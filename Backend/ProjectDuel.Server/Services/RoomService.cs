using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectDuel.Server.Domain;
using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.Rules;

namespace ProjectDuel.Server.Services;

public sealed class ConnectedSessionAccessor
{
    public required string SessionId { get; init; }
    public required WebSocket Socket { get; init; }
}

/// <summary>
/// ???????
/// ???????????????????????????/???????
/// </summary>
public sealed class RoomService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, ConnectedSession> _sessions = new();
    private readonly ConcurrentDictionary<string, DuelRoom> _rooms = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly AuthoritativeConfigService _configService;
    private readonly ILogger<RoomService> _logger;

    public RoomService(AuthoritativeConfigService configService, ILogger<RoomService>? logger = null)
    {
        _configService = configService;
        _logger = logger ?? NullLogger<RoomService>.Instance;
    }

    /// <summary>
    /// 为一个新的 WebSocket 连接创建会话。
    /// 会话对象记录玩家名、房间号、已选套牌等联机上下文。
    /// </summary>
    public ConnectedSession RegisterSocket(WebSocket socket)
    {
        var session = new ConnectedSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Socket = socket,
        };
        _sessions[session.SessionId] = session;
        _logger.LogInformation("Socket connected. session={SessionId}", session.SessionId);
        return session;
    }

    /// <summary>
    /// 仅用于测试与调试：获取当前所有房间 ID。
    /// 运行时业务逻辑不依赖此方法。
    /// </summary>
    public IReadOnlyCollection<string> GetDebugRoomIds()
    {
        return _rooms.Keys.ToList();
    }

    /// <summary>
    /// 仅用于测试与调试：按房间号读取当前房间快照对象。
    /// </summary>
    public DuelRoom? GetDebugRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out DuelRoom? room);
        return room;
    }

    /// <summary>
    /// 通过会话 ID 获取当前连接的 Socket 访问器。
    /// Program.cs 的接收循环通过它拿到底层 Socket。
    /// </summary>
    public ConnectedSessionAccessor GetAccessor(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out ConnectedSession? session) || session.Socket == null)
            throw new InvalidOperationException("Session not found: " + sessionId);
        return new ConnectedSessionAccessor { SessionId = sessionId, Socket = session.Socket };
    }

    /// <summary>
    /// 向客户端发送 connected 确认消息。
    /// </summary>
    public Task SendConnectedAsync(ConnectedSession session)
    {
        return SendAsync(session, ServerMessageTypes.Connected, new ConnectedResponse { SessionId = session.SessionId });
    }

    /// <summary>
    /// 注销断开的连接，并更新房间中的在线状态。
    /// 当前实现保留房间对象，后续可继续扩展断线重连。
    /// </summary>
    public async Task UnregisterSocketAsync(string sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out ConnectedSession? session))
            return;

        _logger.LogInformation("Socket disconnected. session={SessionId}, room={RoomId}", sessionId, session.RoomId ?? string.Empty);

        if (string.IsNullOrWhiteSpace(session.RoomId))
            return;
        if (!_rooms.TryGetValue(session.RoomId, out DuelRoom? room))
            return;

        RoomPlayerSlot? slot = room.Players.FirstOrDefault(player => player.SessionId == sessionId);
        if (slot != null)
            slot.IsConnected = false;

        LogRoomState("session_disconnected", room);
        await BroadcastRoomStateAsync(room);
    }

    /// <summary>
    /// 服务器统一消息分发入口。
    /// 所有客户端操作都先经过这里，再路由到对应的房间/对局处理函数。
    /// </summary>
    public async Task HandleClientMessageAsync(string sessionId, ClientEnvelope envelope)
    {
        if (!_sessions.TryGetValue(sessionId, out ConnectedSession? session))
        {
            _logger.LogWarning("Received client message for unknown session. session={SessionId}, type={Type}", sessionId, envelope.Type);
            return;
        }

        _logger.LogInformation(
            "Received client message. session={SessionId}, room={RoomId}, type={Type}, payload={Payload}",
            sessionId,
            session.RoomId ?? string.Empty,
            envelope.Type,
            SummarizePayload(envelope.Payload));

        switch (envelope.Type)
        {
            case ClientMessageTypes.Hello:
                await HandleHelloAsync(session, envelope.Payload.Deserialize<HelloRequest>(JsonOptions));
                break;
            case ClientMessageTypes.CreateRoom:
                await HandleCreateRoomAsync(session, envelope.Payload.Deserialize<CreateRoomRequest>(JsonOptions));
                break;
            case ClientMessageTypes.JoinRoom:
                await HandleJoinRoomAsync(session, envelope.Payload.Deserialize<JoinRoomRequest>(JsonOptions));
                break;
            case ClientMessageTypes.SetReady:
                await HandleSetReadyAsync(session, envelope.Payload.Deserialize<SetReadyRequest>(JsonOptions));
                break;
            case ClientMessageTypes.EndPhase:
                await HandleEndPhaseAsync(session);
                break;
            case ClientMessageTypes.PlayCards:
                await HandlePlayCardsAsync(session, envelope.Payload.Deserialize<PlayCardsRequest>(JsonOptions));
                break;
            case ClientMessageTypes.ActivatePrimarySkill:
                await HandleActivatePrimarySkillAsync(session, envelope.Payload.Deserialize<SelectSkillRequest>(JsonOptions));
                break;
            case ClientMessageTypes.TakeBackPlayedCard:
                await HandleTakeBackPlayedCardAsync(session, envelope.Payload.Deserialize<TakeBackPlayedCardRequest>(JsonOptions));
                break;
            case ClientMessageTypes.SelectAttackSkill:
                await HandleSelectAttackSkillAsync(session, envelope.Payload.Deserialize<SelectSkillRequest>(JsonOptions));
                break;
            case ClientMessageTypes.SelectDefenseSkill:
                await HandleSelectDefenseSkillAsync(session, envelope.Payload.Deserialize<SelectSkillRequest>(JsonOptions));
                break;
            case ClientMessageTypes.UseMorale:
                await HandleUseMoraleAsync(session, envelope.Payload.Deserialize<UseMoraleRequest>(JsonOptions));
                break;
            case ClientMessageTypes.Ping:
                await SendAsync(session, ServerMessageTypes.Pong, new { now = DateTimeOffset.UtcNow });
                break;
            default:
                await SendErrorAsync(session, "unknown_message", "Unknown message type: " + envelope.Type);
                break;
        }
    }

    private Task HandleHelloAsync(ConnectedSession session, HelloRequest? request)
    {
        if (request != null && !string.IsNullOrWhiteSpace(request.PlayerName))
            session.PlayerName = request.PlayerName.Trim();
        _logger.LogInformation("Hello accepted. session={SessionId}, player={PlayerName}", session.SessionId, session.PlayerName);
        return SendConnectedAsync(session);
    }

    private async Task HandleCreateRoomAsync(ConnectedSession session, CreateRoomRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PlayerName) || request.Deck == null || request.Deck.CardIds == null || request.Deck.CardIds.Count != 3)
        {
            await SendErrorAsync(session, "invalid_request", "CreateRoom requires playerName and a complete deck.");
            return;
        }

        session.PlayerName = request.PlayerName.Trim();
        session.Deck = request.Deck;

        var room = new DuelRoom
        {
            RoomId = CreateRoomCode(),
            Status = RoomStatus.Waiting,
            TurnNumber = 0,
            ActiveSeatIndex = 0,
            Phase = DuelPhaseName.Preparation,
            Players = new List<RoomPlayerSlot>
            {
                new()
                {
                    SeatIndex = 0,
                    SessionId = session.SessionId,
                    PlayerName = session.PlayerName,
                    Deck = request.Deck,
                    IsConnected = true,
                    IsReady = false,
                }
            }
        };

        session.RoomId = room.RoomId;
        _rooms[room.RoomId] = room;

        _logger.LogInformation(
            "Room created. room={RoomId}, ownerSession={SessionId}, player={PlayerName}, deck={DeckId}",
            room.RoomId,
            session.SessionId,
            session.PlayerName,
            request.Deck.DeckId);
        LogRoomState("room_created", room);
        await SendAsync(session, ServerMessageTypes.RoomCreated, new RoomCreatedResponse { RoomId = room.RoomId });
        await BroadcastRoomStateAsync(room);
    }

    private async Task HandleJoinRoomAsync(ConnectedSession session, JoinRoomRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RoomId) || string.IsNullOrWhiteSpace(request.PlayerName) || request.Deck == null || request.Deck.CardIds == null || request.Deck.CardIds.Count != 3)
        {
            await SendErrorAsync(session, "invalid_request", "JoinRoom requires roomId, playerName and a complete deck.");
            return;
        }

        string roomId = request.RoomId.Trim().ToUpperInvariant();
        if (!_rooms.TryGetValue(roomId, out DuelRoom? room))
        {
            await SendErrorAsync(session, "room_not_found", "Room not found.");
            return;
        }
        if (room.Players.Count >= 2)
        {
            await SendErrorAsync(session, "room_full", "Room is already full.");
            return;
        }
        if (room.Status != RoomStatus.Waiting && room.Status != RoomStatus.ReadyCheck)
        {
            await SendErrorAsync(session, "room_closed", "Room can no longer accept players.");
            return;
        }

        session.PlayerName = request.PlayerName.Trim();
        session.Deck = request.Deck;
        session.RoomId = roomId;
        room.Players.Add(new RoomPlayerSlot
        {
            SeatIndex = room.Players.Count,
            SessionId = session.SessionId,
            PlayerName = session.PlayerName,
            Deck = request.Deck,
            IsConnected = true,
            IsReady = false,
        });
        room.Status = RoomStatus.ReadyCheck;
        _logger.LogInformation(
            "Room joined. room={RoomId}, session={SessionId}, player={PlayerName}, deck={DeckId}",
            roomId,
            session.SessionId,
            session.PlayerName,
            request.Deck.DeckId);
        LogRoomState("room_joined", room);
        await SendAsync(session, ServerMessageTypes.RoomJoined, new { roomId });
        await BroadcastRoomStateAsync(room);
    }

    private async Task HandleSetReadyAsync(ConnectedSession session, SetReadyRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "SetReady requires isReady.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }

        slot!.IsReady = request.IsReady;
        _logger.LogInformation("Set ready. room={RoomId}, session={SessionId}, player={PlayerName}, isReady={IsReady}", room!.RoomId, session.SessionId, slot.PlayerName, request.IsReady);
        if (room!.Players.Count == 2 && room.Players.All(player => player.IsReady))
        {
            room.Status = RoomStatus.InGame;
            room.MatchState = AuthoritativeBattleEngine.StartMatch(room.Players[0].Deck, room.Players[1].Deck);
            SyncRoomFromMatch(room);
            _logger.LogInformation("Match started. room={RoomId}, turn={TurnNumber}, phase={Phase}, activeSeat={ActiveSeatIndex}", room.RoomId, room.TurnNumber, room.Phase, room.ActiveSeatIndex);
            await BroadcastAsync(room.Players.Select(player => player.SessionId), ServerMessageTypes.MatchStarted, new { roomId = room.RoomId });
        }
        else
        {
            room.Status = room.Players.Count == 2 ? RoomStatus.ReadyCheck : RoomStatus.Waiting;
        }

        LogRoomState("ready_state_changed", room);
        await BroadcastRoomStateAsync(room);
    }

    private async Task HandleEndPhaseAsync(ConnectedSession session)
    {
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        bool canEndCurrentPhase = match!.Phase == DuelPhaseName.Defense
            ? slot!.SeatIndex != match.ActiveSeatIndex
            : slot!.SeatIndex == match.ActiveSeatIndex;
        if (!canEndCurrentPhase)
        {
            await SendErrorAsync(session, "not_your_turn", "Current phase cannot be ended by this player.");
            return;
        }

        AuthoritativeBattleEngine.AdvancePhase(match, _configService);
        SyncRoomFromMatch(room!);
        _logger.LogInformation("End phase succeeded. room={RoomId}, session={SessionId}, newPhase={Phase}, turn={TurnNumber}, activeSeat={ActiveSeatIndex}", room!.RoomId, session.SessionId, room.Phase, room.TurnNumber, room.ActiveSeatIndex);
        await BroadcastRoomStateAsync(room!);
    }

    private async Task HandlePlayCardsAsync(ConnectedSession session, PlayCardsRequest? request)
    {
        if (request == null || request.HandIndices == null || request.HandIndices.Count == 0)
        {
            await SendErrorAsync(session, "invalid_request", "PlayCards requires hand indices.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        if (!AuthoritativeBattleEngine.TryPlayCards(match!, slot!.SeatIndex, request.HandIndices, request.ChaShiCourtChoices, _configService))
        {
            await SendErrorAsync(session, "invalid_play", "Unable to play cards in current state.");
            return;
        }
        _logger.LogInformation("Play cards succeeded. room={RoomId}, session={SessionId}, seat={SeatIndex}, handIndices={HandIndices}", room!.RoomId, session.SessionId, slot.SeatIndex, string.Join(",", request.HandIndices));
        SyncRoomFromMatch(room!);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private async Task HandleActivatePrimarySkillAsync(ConnectedSession session, SelectSkillRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "ActivatePrimarySkill requires a payload.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        string cardId = GetGeneralCardId(match!, slot!.SeatIndex, request.GeneralIndex);
        var rule = _configService.GetRule(cardId, request.SkillIndex);
        if (rule == null)
        {
            await SendErrorAsync(session, "rule_not_found", "Skill rule not found.");
            return;
        }
        if (!AuthoritativeBattleEngine.TryActivatePrimarySkill(match!, slot.SeatIndex, rule.EffectId, rule.Value1, rule.Value2, rule.StringValue1))
        {
            await SendErrorAsync(session, "invalid_primary_skill", "Cannot activate this primary skill now.");
            return;
        }
        _logger.LogInformation("Primary skill activated. room={RoomId}, session={SessionId}, seat={SeatIndex}, generalIndex={GeneralIndex}, skillIndex={SkillIndex}, effectId={EffectId}", room!.RoomId, session.SessionId, slot.SeatIndex, request.GeneralIndex, request.SkillIndex, rule.EffectId);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private async Task HandleTakeBackPlayedCardAsync(ConnectedSession session, TakeBackPlayedCardRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "TakeBackPlayedCard requires a payload.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        if (!AuthoritativeBattleEngine.TryTakeBackPlayedCard(match!, slot!.SeatIndex, request.PlayedIndex))
        {
            await SendErrorAsync(session, "invalid_take_back", "Cannot take back the selected card now.");
            return;
        }
        _logger.LogInformation("Take back played card succeeded. room={RoomId}, session={SessionId}, seat={SeatIndex}, playedIndex={PlayedIndex}", room!.RoomId, session.SessionId, slot.SeatIndex, request.PlayedIndex);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private async Task HandleSelectAttackSkillAsync(ConnectedSession session, SelectSkillRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "SelectAttackSkill requires a payload.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        string cardId = GetGeneralCardId(match!, slot!.SeatIndex, request.GeneralIndex);
        string skillName = _configService.GetRule(cardId, request.SkillIndex)?.SkillName ?? (cardId + "_" + request.SkillIndex);
        if (!AuthoritativeBattleEngine.TrySelectAttackSkill(match!, slot.SeatIndex, request.GeneralIndex, request.SkillIndex, skillName))
        {
            await SendErrorAsync(session, "invalid_attack_skill", "Cannot select this attack skill now.");
            return;
        }
        _logger.LogInformation("Attack skill selected. room={RoomId}, session={SessionId}, seat={SeatIndex}, generalIndex={GeneralIndex}, skillIndex={SkillIndex}, skillName={SkillName}", room!.RoomId, session.SessionId, slot.SeatIndex, request.GeneralIndex, request.SkillIndex, skillName);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private async Task HandleSelectDefenseSkillAsync(ConnectedSession session, SelectSkillRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "SelectDefenseSkill requires a payload.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        string cardId = GetGeneralCardId(match!, slot!.SeatIndex, request.GeneralIndex);
        string skillName = _configService.GetRule(cardId, request.SkillIndex)?.SkillName ?? (cardId + "_" + request.SkillIndex);
        if (!AuthoritativeBattleEngine.TrySelectDefenseSkill(match!, slot.SeatIndex, request.GeneralIndex, request.SkillIndex, skillName))
        {
            await SendErrorAsync(session, "invalid_defense_skill", "Cannot select this defense skill now.");
            return;
        }
        _logger.LogInformation("Defense skill selected. room={RoomId}, session={SessionId}, seat={SeatIndex}, generalIndex={GeneralIndex}, skillIndex={SkillIndex}, skillName={SkillName}", room!.RoomId, session.SessionId, slot.SeatIndex, request.GeneralIndex, request.SkillIndex, skillName);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private async Task HandleUseMoraleAsync(ConnectedSession session, UseMoraleRequest? request)
    {
        if (request == null)
        {
            await SendErrorAsync(session, "invalid_request", "UseMorale requires a payload.");
            return;
        }
        if (!TryGetRoomAndSlot(session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match))
        {
            await SendErrorAsync(session, "room_not_joined", "Join a room first.");
            return;
        }
        if (!AuthoritativeBattleEngine.TryUseMorale(match!, slot!.SeatIndex, request.EffectIndex, request.GeneralIndex, _configService))
        {
            await SendErrorAsync(session, "invalid_morale", "Cannot use morale effect now.");
            return;
        }
        _logger.LogInformation("Use morale succeeded. room={RoomId}, session={SessionId}, seat={SeatIndex}, effectIndex={EffectIndex}, generalIndex={GeneralIndex}", room!.RoomId, session.SessionId, slot.SeatIndex, request.EffectIndex, request.GeneralIndex);
        await BroadcastBattleSnapshotsAsync(room!);
    }

    private bool TryGetRoomAndSlot(ConnectedSession session, out DuelRoom? room, out RoomPlayerSlot? slot)
    {
        room = null;
        slot = null;
        if (string.IsNullOrWhiteSpace(session.RoomId))
            return false;
        if (!_rooms.TryGetValue(session.RoomId, out room))
            return false;
        slot = room.Players.FirstOrDefault(player => player.SessionId == session.SessionId);
        return slot != null;
    }

    private bool TryGetRoomAndSlot(ConnectedSession session, out DuelRoom? room, out RoomPlayerSlot? slot, out AuthoritativeBattleState? match)
    {
        match = null;
        if (!TryGetRoomAndSlot(session, out room, out slot))
            return false;
        if (room!.Status != RoomStatus.InGame || room.MatchState == null)
            return false;
        match = room.MatchState;
        return true;
    }

    private void SyncRoomFromMatch(DuelRoom room)
    {
        if (room.MatchState == null)
            return;
        room.ActiveSeatIndex = room.MatchState.ActiveSeatIndex;
        room.TurnNumber = room.MatchState.TurnNumber;
        room.Phase = room.MatchState.Phase;
    }

    /// <summary>
    /// 广播房间层面的快照。
    /// 该快照包含房间状态、准备状态和当前阶段；若房间已进入对局，还会继续广播战斗快照。
    /// </summary>
    private async Task BroadcastRoomStateAsync(DuelRoom room)
    {
        if (room.MatchState != null)
            SyncRoomFromMatch(room);

        LogRoomState("broadcast_room_snapshot", room);
        var snapshot = new RoomSnapshotResponse
        {
            RoomId = room.RoomId,
            Status = room.Status,
            TurnNumber = room.TurnNumber,
            ActiveSeatIndex = room.ActiveSeatIndex,
            Phase = room.Phase,
            Players = room.Players.Select(player => new PlayerSlotSnapshot
            {
                SeatIndex = player.SeatIndex,
                SessionId = player.SessionId,
                PlayerName = player.PlayerName,
                DeckId = player.Deck.DeckId,
                IsReady = player.IsReady,
                IsConnected = player.IsConnected,
            }).ToList(),
        };

        await BroadcastAsync(room.Players.Select(player => player.SessionId), ServerMessageTypes.RoomSnapshot, snapshot);
        if (room.Status == RoomStatus.InGame && room.MatchState != null)
            await BroadcastBattleSnapshotsAsync(room);
    }

    /// <summary>
    /// 按玩家座位分别构建个性化战斗快照。
    /// 因为每个玩家看到的手牌不同，所以不能简单广播同一份数据。
    /// </summary>
    private async Task BroadcastBattleSnapshotsAsync(DuelRoom room)
    {
        if (room.MatchState == null)
            return;
        _logger.LogInformation("Broadcast battle snapshots. room={RoomId}, phase={Phase}, turn={TurnNumber}, activeSeat={ActiveSeatIndex}", room.RoomId, room.Phase, room.TurnNumber, room.ActiveSeatIndex);
        for (int seatIndex = 0; seatIndex < room.Players.Count; seatIndex++)
        {
            RoomPlayerSlot slot = room.Players[seatIndex];
            if (!_sessions.TryGetValue(slot.SessionId, out ConnectedSession? session))
                continue;
            int opponentSeat = seatIndex == 0 ? 1 : 0;
            RoomPlayerSlot opponent = room.Players[opponentSeat];
            BattleSnapshotResponse snapshot = AuthoritativeBattleEngine.BuildSnapshot(
                room.MatchState,
                seatIndex,
                room.RoomId,
                slot.PlayerName,
                opponent.PlayerName,
                slot.Deck.DeckId,
                opponent.Deck.DeckId);
            await SendAsync(session, ServerMessageTypes.BattleSnapshot, snapshot);
        }
    }

    private async Task BroadcastAsync(IEnumerable<string> sessionIds, string type, object payload)
    {
        foreach (string sessionId in sessionIds.Distinct())
        {
            if (_sessions.TryGetValue(sessionId, out ConnectedSession? session))
                await SendAsync(session, type, payload);
        }
    }

    private async Task SendAsync(ConnectedSession session, string type, object payload)
    {
        if (session.Socket == null || session.Socket.State != WebSocketState.Open)
            return;
        string json = JsonSerializer.Serialize(new ServerEnvelope { Type = type, Payload = payload }, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync();
        try
        {
            await session.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private Task SendErrorAsync(ConnectedSession session, string code, string message)
    {
        _logger.LogWarning("Command rejected. session={SessionId}, room={RoomId}, code={Code}, message={Message}", session.SessionId, session.RoomId ?? string.Empty, code, message);
        return SendAsync(session, ServerMessageTypes.CommandRejected, new ErrorResponse { Code = code, Message = message });
    }

    private void LogRoomState(string action, DuelRoom room)
    {
        _logger.LogInformation(
            "{Action}. room={RoomId}, status={Status}, phase={Phase}, turn={TurnNumber}, activeSeat={ActiveSeatIndex}, players={Players}",
            action,
            room.RoomId,
            room.Status,
            room.Phase,
            room.TurnNumber,
            room.ActiveSeatIndex,
            string.Join(" | ", room.Players.Select(player =>
                player.SeatIndex + ":" + player.PlayerName
                + "/ready=" + player.IsReady
                + "/connected=" + player.IsConnected
                + "/session=" + player.SessionId)));
    }

    private static string SummarizePayload(JsonElement payload)
    {
        string raw = payload.ValueKind == JsonValueKind.Undefined ? "{}" : payload.GetRawText();
        return raw.Length <= 400 ? raw : raw.Substring(0, 400) + "...";
    }

    private static string CreateRoomCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]).ToArray());
    }

    private static string GetGeneralCardId(AuthoritativeBattleState state, int seatIndex, int generalIndex)
    {
        if (seatIndex < 0 || seatIndex >= state.Sides.Length)
            return string.Empty;
        var side = state.Sides[seatIndex];
        if (generalIndex < 0 || generalIndex >= side.GeneralCardIds.Count)
            return string.Empty;
        return side.GeneralCardIds[generalIndex] ?? string.Empty;
    }
}
