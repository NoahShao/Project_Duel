using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JunzhenDuijue
{
    /// <summary>
    /// <summary>联机大厅界面。</summary>
    /// <summary>负责服务器地址、房间号、套牌选择、准备状态与基础房间信息展示。</summary>
    /// </summary>
    public static class OnlineLobbyUI
    {
        private static GameObject _root;
        private static TMP_InputField _serverInput;
        private static TMP_InputField _roomInput;
        private static TextMeshProUGUI _selectedDeckText;
        private static TextMeshProUGUI _statusText;
        private static TextMeshProUGUI _playerSlot1;
        private static TextMeshProUGUI _playerSlot2;
        private static TextMeshProUGUI _readyButtonText;
        private static DeckData _selectedDeck;
        private static bool _localReady;
        private static bool _eventsBound;

        private static void Trace(string message)
        {
            RuntimeTraceLogger.Trace("OnlineLobbyUI", message);
        }

        /// <summary>
        /// 创建联机大厅界面。
        /// 整个大厅 UI 通过纯代码构建，避免依赖额外场景或预制体。
        /// </summary>
        public static void Create()
        {
            if (_root != null)
                return;
            OnlineClientService.EnsureRunner();
            _root = new GameObject("OnlineLobbyUI");
            _root.SetActive(false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1920, 1080);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_root.transform, false);
            bg.AddComponent<Image>().color = new Color(0.11f, 0.14f, 0.18f, 1f);
            SetFullRect(bg.GetComponent<RectTransform>());

            CreateTitle("联机对战", new Vector2(0.5f, 1f), new Vector2(0, -40), 44);
            string suggestedServerUrl = OnlineClientService.GetSuggestedServerUrl();
            Trace("Create UI. suggestedServerUrl=" + suggestedServerUrl);
            _serverInput = CreateInput(suggestedServerUrl, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(700, 46), suggestedServerUrl);
            _roomInput = CreateInput("输入房间号后加入", new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(380, 46));
            _selectedDeckText = CreateTitle("未选择套牌", new Vector2(0.5f, 1f), new Vector2(0, -245), 24);
            _statusText = CreateTitle("未连接服务器", new Vector2(0.5f, 1f), new Vector2(0, -290), 22);
            _playerSlot1 = CreateTitle("1P: 空位", new Vector2(0.5f, 0.5f), new Vector2(0, 40), 28);
            _playerSlot2 = CreateTitle("2P: 空位", new Vector2(0.5f, 0.5f), new Vector2(0, -10), 28);

            CreateButton("\u9009\u62e9\u5957\u724c", new Vector2(-320, 120), OnSelectDeck);
            CreateButton("\u521b\u5efa\u623f\u95f4", new Vector2(0, 120), async () => await OnCreateRoomAsync());
            CreateButton("\u52a0\u5165\u623f\u95f4", new Vector2(320, 120), async () => await OnJoinRoomAsync());
            CreateButton("\u8fd4\u56de", new Vector2(-320, -200), OnBack);
            var readyBtn = CreateButton("\u51c6\u5907", new Vector2(0, -200), async () => await ToggleReadyAsync());
            _readyButtonText = readyBtn.GetComponentInChildren<TextMeshProUGUI>();
            CreateButton("结束当前阶段", new Vector2(320, -200), async () => await OnlineClientService.EndPhaseAsync());

            BindEvents();
        }

        /// <summary>
        /// 显示联机大厅，并刷新当前已选套牌文本。
        /// </summary>
        public static void Show()
        {
            if (_root == null)
                Create();
            if (!OnlineClientService.IsConnected)
            {
                ResetRoomDisplay();
                SetStatus("未连接服务器");
            }
            RefreshDeckText();
            RefreshReadyButton();
            _root.SetActive(true);
            Trace("Show lobby. connected=" + OnlineClientService.IsConnected + ", roomId=" + OnlineClientService.RoomId);
        }

        public static void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        /// <summary>
        /// 绑定网络客户端事件。
        /// 大厅与战斗 UI 都依赖这里接收服务器回传的房间与战斗快照。
        /// </summary>
        private static void BindEvents()
        {
            if (_eventsBound)
                return;
            _eventsBound = true;
            OnlineClientService.OnConnected += sessionId => SetStatus("已连接，Session=" + sessionId);
            OnlineClientService.OnRoomCreated += roomId =>
            {
                _roomInput.text = roomId;
                SetLocalReady(false);
                SetStatus("房间已创建：" + roomId);
            };
            OnlineClientService.OnRoomJoined += roomId =>
            {
                _roomInput.text = roomId;
                SetLocalReady(false);
                SetStatus("已加入房间：" + roomId);
            };
            OnlineClientService.OnRoomSnapshot += OnRoomSnapshot;
            OnlineClientService.OnBattleSnapshot += OnBattleSnapshot;
            OnlineClientService.OnMatchStarted += roomId => SetStatus("对局开始：" + roomId);
            OnlineClientService.OnError += (code, message) => { SetStatus(code + ": " + message); ToastUI.Show(message); };
            OnlineClientService.OnDisconnected += () =>
            {
                ResetRoomDisplay();
                SetStatus("连接已断开");
            };
        }

        /// <summary>
        /// 创建房间。
        /// 当前本地用户必须先选择一套完整套牌，才允许向服务器发起建房。
        /// </summary>
        private static async Task OnCreateRoomAsync()
        {
            if (!EnsureDeckSelected())
                return;
            Trace("Create room requested. server=" + (_serverInput != null ? _serverInput.text : string.Empty) + ", deck=" + (_selectedDeck != null ? _selectedDeck.Id : "null"));
            await OnlineClientService.CreateRoomAsync(_serverInput.text, _selectedDeck);
        }

        /// <summary>
        /// 加入指定房间号。
        /// 这里同样要求先选择套牌，并由服务器进行房间容量与状态校验。
        /// </summary>
        private static async Task OnJoinRoomAsync()
        {
            if (!EnsureDeckSelected())
                return;
            if (string.IsNullOrWhiteSpace(_roomInput.text))
            {
                ToastUI.Show("请输入房间号");
                return;
            }
            Trace("Join room requested. server=" + (_serverInput != null ? _serverInput.text : string.Empty) + ", roomId=" + (_roomInput != null ? _roomInput.text : string.Empty) + ", deck=" + (_selectedDeck != null ? _selectedDeck.Id : "null"));
            await OnlineClientService.JoinRoomAsync(_serverInput.text, _roomInput.text, _selectedDeck);
        }

        /// <summary>
        /// 在大厅中切换本地玩家准备状态。
        /// 双方都准备后，服务器会把房间推进到 InGame。
        /// </summary>
        private static async Task ToggleReadyAsync()
        {
            _localReady = !_localReady;
            Trace("Toggle ready requested. localReady=" + _localReady + ", roomId=" + OnlineClientService.RoomId);
            await OnlineClientService.SetReadyAsync(_localReady);
            RefreshReadyButton();
        }

        private static void OnSelectDeck()
        {
            DeckSelectUI.ShowForSelection(deck =>
            {
                _selectedDeck = deck;
                Show();
                RefreshDeckText();
            }, Show, "选择联机套牌", "选择套牌");
            Hide();
        }

        private static void OnBack()
        {
            _ = ReturnToMainMenuAsync();
        }

        private static async Task ReturnToMainMenuAsync()
        {
            SetStatus("正在离开房间...");
            Trace("ReturnToMainMenuAsync called. Disconnecting current session.");
            await OnlineClientService.DisconnectAsync();
            Hide();
            MainMenuUI.Show();
        }

        /// <summary>
        /// 收到房间快照后刷新大厅两侧座位与房间状态文本。
        /// </summary>
        private static void OnRoomSnapshot(OnlineRoomSnapshotResponse snapshot)
        {
            if (snapshot == null)
                return;
            Trace("OnRoomSnapshot. roomId=" + snapshot.RoomId + ", status=" + snapshot.Status + ", phase=" + snapshot.Phase + ", turn=" + snapshot.TurnNumber + ", players=" + (snapshot.Players != null ? snapshot.Players.Count.ToString() : "0"));
            _roomInput.text = snapshot.RoomId ?? string.Empty;
            if (_playerSlot1 != null)
                _playerSlot1.text = snapshot.Players.Count > 0 ? FormatPlayer(snapshot.Players[0]) : "1P: 空位";
            if (_playerSlot2 != null)
                _playerSlot2.text = snapshot.Players.Count > 1 ? FormatPlayer(snapshot.Players[1]) : "2P: 空位";
            SyncLocalReady(snapshot);
            SetStatus("房间状态：" + snapshot.Status + " / 阶段：" + snapshot.Phase + " / 回合：" + snapshot.TurnNumber);
        }

        /// <summary>
        /// 收到战斗快照时，说明服务器已经进入对局。
        /// 此时大厅隐藏，战斗主界面接管展示。
        /// </summary>
        private static void OnBattleSnapshot(OnlineBattleSnapshotResponse snapshot)
        {
            if (snapshot == null)
                return;
            Trace("OnBattleSnapshot. roomId=" + snapshot.RoomId + ", phase=" + snapshot.Phase + ", turn=" + snapshot.TurnNumber + ", localSeat=" + snapshot.LocalSeatIndex + ", activeSeat=" + snapshot.ActiveSeatIndex);
            Hide();
            GameUI.StartOnlineGame(snapshot);
        }

        private static string FormatPlayer(OnlinePlayerSlotSnapshot player)
        {
            return (player.SeatIndex + 1) + "P: " + player.PlayerName + (player.IsReady ? " [准备]" : " [未准备]") + (player.IsConnected ? string.Empty : " [离线]");
        }

        private static bool EnsureDeckSelected()
        {
            if (_selectedDeck != null && _selectedDeck.CardIds != null && _selectedDeck.CardIds.Count == 3)
                return true;
            ToastUI.Show("请先选择一套完整的套牌");
            Trace("Deck selection check failed. Selected deck is incomplete or empty.");
            return false;
        }

        private static void RefreshDeckText()
        {
            if (_selectedDeckText != null)
                _selectedDeckText.text = _selectedDeck == null ? "未选择套牌" : ("当前套牌：" + (_selectedDeck.DisplayName ?? _selectedDeck.Id));
        }

        private static void ResetRoomDisplay()
        {
            if (_roomInput != null)
                _roomInput.text = string.Empty;
            if (_playerSlot1 != null)
                _playerSlot1.text = "1P: 空位";
            if (_playerSlot2 != null)
                _playerSlot2.text = "2P: 空位";
            SetLocalReady(false);
        }

        private static void SyncLocalReady(OnlineRoomSnapshotResponse snapshot)
        {
            if (snapshot == null || snapshot.Players == null)
                return;

            string sessionId = OnlineClientService.SessionId;
            if (string.IsNullOrEmpty(sessionId))
                return;

            for (int i = 0; i < snapshot.Players.Count; i++)
            {
                if (snapshot.Players[i] != null && snapshot.Players[i].SessionId == sessionId)
                {
                    SetLocalReady(snapshot.Players[i].IsReady);
                    return;
                }
            }

            SetLocalReady(false);
        }

        private static void SetLocalReady(bool isReady)
        {
            _localReady = isReady;
            RefreshReadyButton();
        }

        private static void RefreshReadyButton()
        {
            if (_readyButtonText != null)
                _readyButtonText.text = _localReady ? "取消准备" : "准备";
        }

        private static void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text ?? string.Empty;
        }

        private static TMP_InputField CreateInput(string placeholder, Vector2 anchor, Vector2 pos, Vector2 size, string initialValue = "")
        {
            var go = new GameObject("Input");
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<Image>().color = new Color(0.2f, 0.24f, 0.3f, 1f);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var input = go.AddComponent<TMP_InputField>();
            var text = CreateTitle(string.Empty, Vector2.zero, Vector2.zero, 24, go.transform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(12, 0, 12, 0);
            var ph = CreateTitle(placeholder, Vector2.zero, Vector2.zero, 20, go.transform);
            ph.color = new Color(1f, 1f, 1f, 0.45f);
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.margin = new Vector4(12, 0, 12, 0);
            input.textViewport = go.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = initialValue ?? string.Empty;
            return input;
        }

        private static Button CreateButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<Image>().color = new Color(0.28f, 0.35f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 52);
            CreateTitle(label, Vector2.zero, Vector2.zero, 24, go.transform);
            return btn;
        }

        private static TextMeshProUGUI CreateTitle(string content, Vector2 anchor, Vector2 pos, int fontSize, Transform parent = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent ?? _root.transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null)
                text.font = TMPHelper.GetDefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            var rect = go.GetComponent<RectTransform>();
            if (parent == null)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = pos;
                rect.sizeDelta = new Vector2(900, 40);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            return text;
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
