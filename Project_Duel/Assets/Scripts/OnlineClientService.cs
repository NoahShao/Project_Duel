using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// Unity ???????
    /// ?? WebSocket ????????????????????????? UI?
    /// </summary>
    public static class OnlineClientService
    {
        public const string DefaultServerUrl = "ws://127.0.0.1:5057/ws";
        private const string DefaultWebSocketPath = "/ws";
        private const string LastServerUrlKey = "Online.LastServerUrl";
        /// <summary>
        /// 网络线程收到的消息需要回到 Unity 主线程执行 UI 回调，
        /// 因此这里用一个无锁队列缓存待执行动作。
        /// </summary>
        private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();
        private static readonly Regex JsonFieldNameRegex = new Regex("\"([a-z][A-Za-z0-9_]*)\"\\s*:", RegexOptions.Compiled);
        private static ClientWebSocket _socket;
        private static CancellationTokenSource _cts;
        private static Runner _runner;

        public static string SessionId { get; private set; } = string.Empty;
        public static string RoomId { get; private set; } = string.Empty;
        public static string ServerUrl { get; private set; } = DefaultServerUrl;
        public static bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        public static event Action<string> OnConnected;
        public static event Action<string> OnRoomCreated;
        public static event Action<string> OnRoomJoined;
        public static event Action<OnlineRoomSnapshotResponse> OnRoomSnapshot;
        public static event Action<OnlineBattleSnapshotResponse> OnBattleSnapshot;
        public static event Action<string> OnMatchStarted;
        public static event Action<string, string> OnError;
        public static event Action OnDisconnected;

        private static void Trace(string message)
        {
            RuntimeTraceLogger.Trace("OnlineClientService", message);
        }

        public static void EnsureRunner()
        {
            if (_runner != null)
                return;
            var go = new GameObject("OnlineClientServiceRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        /// <summary>
        /// 建立到权威服务器的 WebSocket 连接。
        /// 如果当前已经连到同一个地址，则直接复用现有连接。
        /// </summary>
        public static async Task<bool> ConnectAsync(string serverUrl)
        {
            EnsureRunner();
            Trace("ConnectAsync requested. input=" + (serverUrl ?? string.Empty) + ", currentState=" + (_socket != null ? _socket.State.ToString() : "null"));
            string normalizedServerUrl;
            try
            {
                normalizedServerUrl = NormalizeServerUrl(serverUrl);
                Trace("Normalized server url=" + normalizedServerUrl);
            }
            catch (Exception ex)
            {
                Trace("NormalizeServerUrl failed: " + ex.Message);
                Enqueue(() => OnError?.Invoke("invalid_server_url", ex.Message));
                return false;
            }

            if (IsConnected && string.Equals(ServerUrl, normalizedServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                Trace("Reusing existing websocket connection for " + normalizedServerUrl);
                return true;
            }

            await DisconnectAsync();
            _socket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            try
            {
                await _socket.ConnectAsync(new Uri(normalizedServerUrl), _cts.Token);
                ServerUrl = normalizedServerUrl;
                RememberServerUrl(normalizedServerUrl);
                Trace("WebSocket connected. server=" + normalizedServerUrl);
                _ = Task.Run(ReceiveLoopAsync);
                await SendPayloadAsync(OnlineClientMessageTypes.Hello, new OnlineHelloRequest { PlayerName = AccountManager.GetCurrentUsername() });
                Trace("Hello sent. user=" + AccountManager.GetCurrentUsername());
                return true;
            }
            catch (Exception ex)
            {
                Trace("ConnectAsync failed: " + ex);
                Enqueue(() => OnError?.Invoke("connect_failed", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 主动断开当前 WebSocket 连接，并重置本地会话状态。
        /// </summary>
        public static async Task DisconnectAsync()
        {
            Trace("DisconnectAsync requested. state=" + (_socket != null ? _socket.State.ToString() : "null"));
            try
            {
                _cts?.Cancel();
                if (_socket != null)
                {
                    if (_socket.State == WebSocketState.Open)
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    _socket.Dispose();
                }
            }
            catch { }
            finally
            {
                _socket = null;
                _cts?.Dispose();
                _cts = null;
                SessionId = string.Empty;
                RoomId = string.Empty;
                Trace("Disconnected and cleared local session state.");
                Enqueue(() => OnDisconnected?.Invoke());
            }
        }

        public static Task CreateRoomAsync(string serverUrl, DeckData deck)
        {
            return RunConnectedAsync(serverUrl, () => SendPayloadAsync(OnlineClientMessageTypes.CreateRoom, new OnlineCreateRoomRequest
            {
                PlayerName = AccountManager.GetCurrentUsername(),
                Deck = OnlineDeckSelectionDto.FromDeckData(deck),
            }));
        }

        public static Task JoinRoomAsync(string serverUrl, string roomId, DeckData deck)
        {
            return RunConnectedAsync(serverUrl, () => SendPayloadAsync(OnlineClientMessageTypes.JoinRoom, new OnlineJoinRoomRequest
            {
                RoomId = roomId ?? string.Empty,
                PlayerName = AccountManager.GetCurrentUsername(),
                Deck = OnlineDeckSelectionDto.FromDeckData(deck),
            }));
        }

        public static string GetSuggestedServerUrl()
        {
            string savedUrl = string.Empty;
            try
            {
                savedUrl = PlayerPrefs.GetString(LastServerUrlKey, string.Empty);
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(savedUrl))
            {
                try
                {
                    return NormalizeServerUrl(savedUrl);
                }
                catch { }
            }

            if (TryGetLocalIpv4(out string ipAddress))
                return "ws://" + ipAddress + ":5057" + DefaultWebSocketPath;

            return DefaultServerUrl;
        }

        public static string NormalizeServerUrl(string serverUrl)
        {
            string value = (serverUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
                return DefaultServerUrl;

            value = value.Replace('\\', '/');

            if (value.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
                value = "ws://" + value.Substring(3).TrimStart('/');
            else if (value.StartsWith("wss:", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                value = "wss://" + value.Substring(4).TrimStart('/');
            else if (value.StartsWith("http:", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                value = "http://" + value.Substring(5).TrimStart('/');
            else if (value.StartsWith("https:", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                value = "https://" + value.Substring(6).TrimStart('/');
            else if (!value.Contains("://"))
                value = "ws://" + value.TrimStart('/');

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                throw new UriFormatException("Server URL is invalid.");

            string scheme;
            if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase))
                scheme = "ws";
            else if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
                scheme = "wss";
            else
                throw new UriFormatException("Server URL must use ws, wss, http or https.");

            var builder = new UriBuilder(uri)
            {
                Scheme = scheme,
                Path = NormalizeWebSocketPath(uri.AbsolutePath),
            };
            builder.Port = uri.IsDefaultPort ? -1 : uri.Port;
            return builder.Uri.ToString();
        }

        private static string NormalizeWebSocketPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
                return DefaultWebSocketPath;

            return string.Equals(path, DefaultWebSocketPath, StringComparison.OrdinalIgnoreCase)
                ? DefaultWebSocketPath
                : path;
        }

        private static void RememberServerUrl(string serverUrl)
        {
            try
            {
                PlayerPrefs.SetString(LastServerUrlKey, serverUrl ?? string.Empty);
                PlayerPrefs.Save();
            }
            catch { }
        }

        private static bool TryGetLocalIpv4(out string ipAddress)
        {
            try
            {
                foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                        continue;

                    string candidate = address.ToString();
                    if (candidate.StartsWith("169.254.", StringComparison.Ordinal))
                        continue;

                    ipAddress = candidate;
                    return true;
                }
            }
            catch { }

            ipAddress = string.Empty;
            return false;
        }

        public static Task SetReadyAsync(bool isReady) => SendPayloadAsync(OnlineClientMessageTypes.SetReady, new OnlineSetReadyRequest { IsReady = isReady });
        public static Task EndPhaseAsync() => SendPayloadAsync(OnlineClientMessageTypes.EndPhase, new EmptyPayload());
        public static Task PlayCardsAsync(System.Collections.Generic.IReadOnlyList<int> handIndices, System.Collections.Generic.IReadOnlyList<(int handIndex, bool useAsTen)> chaShiCourtChoices = null)
        {
            var req = new OnlinePlayCardsRequest { HandIndices = new System.Collections.Generic.List<int>(handIndices) };
            if (chaShiCourtChoices != null && chaShiCourtChoices.Count > 0)
            {
                for (int i = 0; i < chaShiCourtChoices.Count; i++)
                {
                    (int hi, bool ut) = chaShiCourtChoices[i];
                    req.ChaShiCourtChoices.Add(new ChaShiCourtChoiceWire { HandIndex = hi, UseAsTen = ut });
                }
            }

            return SendPayloadAsync(OnlineClientMessageTypes.PlayCards, req);
        }

        public static Task PlayCardAsync(int handIndex) => PlayCardsAsync(new System.Collections.Generic.List<int> { handIndex });
        public static Task TakeBackPlayedCardAsync(int playedIndex) => SendPayloadAsync(OnlineClientMessageTypes.TakeBackPlayedCard, new OnlineTakeBackPlayedCardRequest { PlayedIndex = playedIndex });
        public static Task ActivatePrimarySkillAsync(int generalIndex, int skillIndex) => SendPayloadAsync(OnlineClientMessageTypes.ActivatePrimarySkill, new OnlineSelectSkillRequest { GeneralIndex = generalIndex, SkillIndex = skillIndex });
        public static Task SelectAttackSkillAsync(int generalIndex, int skillIndex) => SendPayloadAsync(OnlineClientMessageTypes.SelectAttackSkill, new OnlineSelectSkillRequest { GeneralIndex = generalIndex, SkillIndex = skillIndex });
        public static Task SelectDefenseSkillAsync(int generalIndex, int skillIndex) => SendPayloadAsync(OnlineClientMessageTypes.SelectDefenseSkill, new OnlineSelectSkillRequest { GeneralIndex = generalIndex, SkillIndex = skillIndex });
        public static Task UseMoraleAsync(int effectIndex, int? generalIndex)
        {
            return SendPayloadAsync(OnlineClientMessageTypes.UseMorale, new OnlineUseMoraleRequest
            {
                EffectIndex = effectIndex,
                HasGeneralIndex = generalIndex.HasValue,
                GeneralIndex = generalIndex ?? -1,
            });
        }

        /// <summary>
        /// 确保已连接后再执行指定网络请求。
        /// 这是建房、加入房间等入口的统一封装。
        /// </summary>
        private static async Task RunConnectedAsync(string serverUrl, Func<Task> afterConnect)
        {
            Trace("RunConnectedAsync start. server=" + (serverUrl ?? string.Empty));
            bool ok = await ConnectAsync(serverUrl);
            if (!ok)
            {
                Trace("RunConnectedAsync aborted because ConnectAsync returned false.");
                return;
            }
            Trace("RunConnectedAsync executing post-connect action.");
            await afterConnect();
        }

        /// <summary>
        /// 向服务器发送统一格式的消息信封。
        /// 这里约定消息格式为 { type, payload }。
        /// </summary>
        private static async Task SendPayloadAsync<T>(string type, T payload)
        {
            if (!IsConnected)
            {
                Trace("Skip sending message because websocket is not connected. type=" + type);
                return;
            }
            string payloadJson = JsonUtility.ToJson(payload ?? Activator.CreateInstance<T>());
            string json = "{\"type\":\"" + EscapeJson(type) + "\",\"payload\":" + payloadJson + "}";
            Trace("Sending websocket message. type=" + type + ", json=" + Abbreviate(json));
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
        }

        /// <summary>
        /// 后台接收循环。
        /// 持续读取服务器消息，并分发到对应的本地事件。
        /// </summary>
        private static async Task ReceiveLoopAsync()
        {
            byte[] buffer = new byte[16 * 1024];
            while (_socket != null && _socket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts?.Token ?? CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    int count = result.Count;
                    while (!result.EndOfMessage)
                    {
                        if (count >= buffer.Length)
                            throw new InvalidOperationException("Incoming websocket message too large.");
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), _cts?.Token ?? CancellationToken.None);
                        count += result.Count;
                    }

                    string json = Encoding.UTF8.GetString(buffer, 0, count);
                    Trace("Received websocket message: " + Abbreviate(json));
                    DispatchEnvelope(json);
                }
                catch (Exception ex)
                {
                    Trace("ReceiveLoopAsync failed: " + ex);
                    Enqueue(() => OnError?.Invoke("receive_failed", ex.Message));
                    break;
                }
            }

            Trace("ReceiveLoopAsync exiting and triggering disconnect.");
            await DisconnectAsync();
        }

        /// <summary>
        /// 对服务器发回的 JSON 字符串做最小解析，
        /// 再根据消息类型转成具体 DTO 并抛给大厅/战斗 UI。
        /// </summary>
        private static void DispatchEnvelope(string json)
        {
            if (!TryExtractStringField(json, "type", out string type))
            {
                Trace("DispatchEnvelope ignored message without type field.");
                return;
            }
            if (!TryExtractObjectField(json, "payload", out string payloadJson))
                payloadJson = "{}";

            Trace("DispatchEnvelope type=" + type + ", payload=" + Abbreviate(payloadJson));

            switch (type)
            {
                case OnlineServerMessageTypes.Connected:
                    {
                        var response = DeserializePayload<OnlineConnectedResponse>(payloadJson);
                        SessionId = response != null ? response.SessionId : string.Empty;
                        Trace("Connected response applied. sessionId=" + SessionId);
                        Enqueue(() => OnConnected?.Invoke(SessionId));
                        break;
                    }
                case OnlineServerMessageTypes.RoomCreated:
                    {
                        var response = DeserializePayload<OnlineRoomCreatedResponse>(payloadJson);
                        RoomId = response != null ? response.RoomId : string.Empty;
                        Trace("Room created response applied. roomId=" + RoomId);
                        Enqueue(() => OnRoomCreated?.Invoke(RoomId));
                        break;
                    }
                case OnlineServerMessageTypes.RoomJoined:
                    {
                        var response = DeserializePayload<OnlineRoomJoinedResponse>(payloadJson);
                        RoomId = response != null ? response.RoomId : string.Empty;
                        Trace("Room joined response applied. roomId=" + RoomId);
                        Enqueue(() => OnRoomJoined?.Invoke(RoomId));
                        break;
                    }
                case OnlineServerMessageTypes.RoomSnapshot:
                    {
                        var snapshot = DeserializePayload<OnlineRoomSnapshotResponse>(payloadJson);
                        if (snapshot != null)
                        {
                            Trace("Room snapshot applied. " + DescribeRoomSnapshot(snapshot));
                            Enqueue(() => OnRoomSnapshot?.Invoke(snapshot));
                        }
                        break;
                    }
                case OnlineServerMessageTypes.BattleSnapshot:
                    {
                        var snapshot = DeserializePayload<OnlineBattleSnapshotResponse>(payloadJson);
                        if (snapshot != null)
                        {
                            Trace("Battle snapshot applied. " + DescribeBattleSnapshot(snapshot));
                            Enqueue(() => OnBattleSnapshot?.Invoke(snapshot));
                        }
                        break;
                    }
                case OnlineServerMessageTypes.MatchStarted:
                    {
                        var response = DeserializePayload<OnlineMatchStartedResponse>(payloadJson);
                        Trace("Match started response applied. roomId=" + (response != null ? response.RoomId : string.Empty));
                        Enqueue(() => OnMatchStarted?.Invoke(response != null ? response.RoomId : string.Empty));
                        break;
                    }
                case OnlineServerMessageTypes.CommandRejected:
                    {
                        var error = DeserializePayload<OnlineErrorResponse>(payloadJson);
                        Trace("Command rejected. code=" + (error != null ? error.Code : "unknown") + ", message=" + (error != null ? error.Message : "Unknown error"));
                        Enqueue(() => OnError?.Invoke(error != null ? error.Code : "unknown", error != null ? error.Message : "Unknown error"));
                        break;
                    }
                case OnlineServerMessageTypes.Pong:
                    Trace("Received pong message.");
                    break;
            }
        }

        private static T DeserializePayload<T>(string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
                return JsonUtility.FromJson<T>("{}");

            string normalizedJson = JsonFieldNameRegex.Replace(payloadJson, match =>
            {
                string fieldName = match.Groups[1].Value;
                return "\"" + char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1) + "\":";
            });
            return JsonUtility.FromJson<T>(normalizedJson);
        }

        /// <summary>
        /// 从简单 JSON 文本中提取字符串字段。
        /// 这里故意不用 System.Text.Json，避免 Unity 侧额外程序集依赖。
        /// </summary>
        private static bool TryExtractStringField(string json, string fieldName, out string value)
        {
            value = string.Empty;
            int fieldIndex = json.IndexOf("\"" + fieldName + "\"", StringComparison.Ordinal);
            if (fieldIndex < 0)
                return false;
            int colonIndex = json.IndexOf(':', fieldIndex);
            if (colonIndex < 0)
                return false;
            int firstQuote = json.IndexOf('"', colonIndex + 1);
            if (firstQuote < 0)
                return false;
            int index = firstQuote + 1;
            var sb = new StringBuilder();
            bool escaped = false;
            while (index < json.Length)
            {
                char ch = json[index++];
                if (escaped)
                {
                    sb.Append(ch);
                    escaped = false;
                    continue;
                }
                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (ch == '"')
                {
                    value = sb.ToString();
                    return true;
                }
                sb.Append(ch);
            }
            return false;
        }

        /// <summary>
        /// 从简单 JSON 文本中提取对象/数组字段的原始 JSON 子串。
        /// 该方法配合 JsonUtility 反序列化 payload。
        /// </summary>
        private static bool TryExtractObjectField(string json, string fieldName, out string objectJson)
        {
            objectJson = string.Empty;
            int fieldIndex = json.IndexOf("\"" + fieldName + "\"", StringComparison.Ordinal);
            if (fieldIndex < 0)
                return false;
            int colonIndex = json.IndexOf(':', fieldIndex);
            if (colonIndex < 0)
                return false;
            int index = colonIndex + 1;
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
            if (index >= json.Length)
                return false;
            char start = json[index];
            if (start != '{' && start != '[')
                return false;
            char end = start == '{' ? '}' : ']';
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            int startIndex = index;
            while (index < json.Length)
            {
                char ch = json[index++];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (ch == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (ch == '"')
                        inString = false;
                    continue;
                }
                if (ch == '"')
                {
                    inString = true;
                    continue;
                }
                if (ch == start)
                    depth++;
                else if (ch == end)
                {
                    depth--;
                    if (depth == 0)
                    {
                        objectJson = json.Substring(startIndex, index - startIndex);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 对消息类型等简单字符串做 JSON 转义，
        /// 防止拼接信封时产生非法 JSON。
        /// </summary>
        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Abbreviate(string value, int maxLength = 800)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value ?? string.Empty;
            return value.Substring(0, maxLength) + "...";
        }

        private static string DescribeRoomSnapshot(OnlineRoomSnapshotResponse snapshot)
        {
            int playerCount = snapshot != null && snapshot.Players != null ? snapshot.Players.Count : 0;
            return "roomId=" + (snapshot != null ? snapshot.RoomId : string.Empty)
                + ", status=" + (snapshot != null ? snapshot.Status.ToString() : "null")
                + ", phase=" + (snapshot != null ? snapshot.Phase.ToString() : "null")
                + ", turn=" + (snapshot != null ? snapshot.TurnNumber.ToString() : "0")
                + ", players=" + playerCount;
        }

        private static string DescribeBattleSnapshot(OnlineBattleSnapshotResponse snapshot)
        {
            if (snapshot == null)
                return "null";

            int selfHandCount = snapshot.SelfHand != null ? snapshot.SelfHand.Count : 0;
            int playedCount = snapshot.PlayedCards != null ? snapshot.PlayedCards.Count : 0;
            string selfHp = snapshot.Self != null ? snapshot.Self.CurrentHp.ToString() : "?";
            string opponentHp = snapshot.Opponent != null ? snapshot.Opponent.CurrentHp.ToString() : "?";
            return "roomId=" + snapshot.RoomId
                + ", phase=" + snapshot.Phase
                + ", turn=" + snapshot.TurnNumber
                + ", localSeat=" + snapshot.LocalSeatIndex
                + ", activeSeat=" + snapshot.ActiveSeatIndex
                + ", selfHp=" + selfHp
                + ", opponentHp=" + opponentHp
                + ", selfHand=" + selfHandCount
                + ", played=" + playedCount;
        }

        private static void Enqueue(Action action)
        {
            if (action != null)
                MainThreadActions.Enqueue(action);
        }

        /// <summary>
        /// ???????????? Unity ??????
        /// </summary>
        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                while (MainThreadActions.TryDequeue(out Action action))
                    action?.Invoke();
            }
        }
    }
}
