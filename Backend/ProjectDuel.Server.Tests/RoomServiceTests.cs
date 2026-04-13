using System.Net.WebSockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectDuel.Server.Options;
using ProjectDuel.Server.Services;
using ProjectDuel.Shared.Protocol;
using Xunit;

namespace ProjectDuel.Server.Tests;

public class RoomServiceTests
{
    private static AuthoritativeConfigService BuildConfig()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ConfigOptions
        {
            SkillRulesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Project_Duel", "Assets", "StreamingAssets", "SkillRules.json"))
        });
        var service = new AuthoritativeConfigService(NullLogger<AuthoritativeConfigService>.Instance, options);
        service.Load();
        return service;
    }

    private static DeckSelectionDto Deck(string id, params string[] cards) => new()
    {
        DeckId = id,
        DisplayName = id,
        RemovedSuit = string.Empty,
        CardIds = cards.ToList(),
    };

    [Fact]
    public async Task CreateAndJoinRoom_FlowsToReadyCheck()
    {
        var roomService = new RoomService(BuildConfig());
        var session1 = roomService.RegisterSocket(new ClientWebSocket());
        var session2 = roomService.RegisterSocket(new ClientWebSocket());

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.CreateRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
            {
                PlayerName = "Alice",
                Deck = Deck("A", "NO001", "NO002", "NO003"),
            })
        });

        string roomId = roomService.GetDebugRoomIds().Single();

        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.JoinRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = "Bob",
                Deck = Deck("B", "NO004", "NO005", "NO006"),
            })
        });

        var room = roomService.GetDebugRoom(roomId);
        Assert.NotNull(room);
        Assert.Equal(2, room!.Players.Count);
        Assert.Equal(RoomStatus.ReadyCheck, room.Status);
    }

    [Fact]
    public async Task BothReady_StartsMatch()
    {
        var roomService = new RoomService(BuildConfig());
        var session1 = roomService.RegisterSocket(new ClientWebSocket());
        var session2 = roomService.RegisterSocket(new ClientWebSocket());

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.CreateRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
            {
                PlayerName = "Alice",
                Deck = Deck("A", "NO001", "NO002", "NO003"),
            })
        });
        string roomId = roomService.GetDebugRoomIds().Single();
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.JoinRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = "Bob",
                Deck = Deck("B", "NO004", "NO005", "NO006"),
            })
        });
        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });

        var room = roomService.GetDebugRoom(roomId);
        Assert.NotNull(room);
        Assert.Equal(RoomStatus.InGame, room!.Status);
        Assert.NotNull(room.MatchState);
        Assert.Equal(1, room.MatchState!.TurnNumber);
    }

    [Fact]
    public async Task PlayCardsCommand_MovesCardsIntoPlayedArea()
    {
        var roomService = new RoomService(BuildConfig());
        var session1 = roomService.RegisterSocket(new ClientWebSocket());
        var session2 = roomService.RegisterSocket(new ClientWebSocket());

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.CreateRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
            {
                PlayerName = "Alice",
                Deck = Deck("A", "NO001", "NO002", "NO003"),
            })
        });
        string roomId = roomService.GetDebugRoomIds().Single();
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.JoinRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = "Bob",
                Deck = Deck("B", "NO004", "NO005", "NO006"),
            })
        });
        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });

        var room = roomService.GetDebugRoom(roomId)!;
        room.MatchState!.Phase = DuelPhaseName.Main;
        int before = room.MatchState.Sides[0].Hand.Count;

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.PlayCards,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new PlayCardsRequest
            {
                HandIndices = new List<int> { 0 }
            })
        });

        Assert.Equal(before - 1, room.MatchState.Sides[0].Hand.Count);
        Assert.Single(room.MatchState.Sides[0].PlayedThisPhase);
    }

    [Fact]
    public async Task UseMoraleCommand_ConsumesMoraleAndChangesState()
    {
        var roomService = new RoomService(BuildConfig());
        var session1 = roomService.RegisterSocket(new ClientWebSocket());
        var session2 = roomService.RegisterSocket(new ClientWebSocket());

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.CreateRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
            {
                PlayerName = "Alice",
                Deck = Deck("A", "NO001", "NO002", "NO003"),
            })
        });
        string roomId = roomService.GetDebugRoomIds().Single();
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.JoinRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = "Bob",
                Deck = Deck("B", "NO004", "NO005", "NO006"),
            })
        });
        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });

        var room = roomService.GetDebugRoom(roomId)!;
        room.MatchState!.Phase = DuelPhaseName.Primary;
        room.MatchState.Sides[0].Morale = 1;
        int before = room.MatchState.Sides[0].Hand.Count;

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.UseMorale,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new UseMoraleRequest
            {
                EffectIndex = 0,
                GeneralIndex = null
            })
        });

        Assert.Equal(0, room.MatchState.Sides[0].Morale);
        Assert.Equal(before + 2, room.MatchState.Sides[0].Hand.Count);
        Assert.True(room.MatchState.Sides[0].MoraleUsedThisTurn[0]);
    }

    [Fact]
    public async Task DefensePhase_AllowsDefenderToEndPhase()
    {
        var roomService = new RoomService(BuildConfig());
        var session1 = roomService.RegisterSocket(new ClientWebSocket());
        var session2 = roomService.RegisterSocket(new ClientWebSocket());

        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.CreateRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
            {
                PlayerName = "Alice",
                Deck = Deck("A", "NO001", "NO002", "NO003"),
            })
        });
        string roomId = roomService.GetDebugRoomIds().Single();
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.JoinRoom,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = "Bob",
                Deck = Deck("B", "NO004", "NO005", "NO006"),
            })
        });
        await roomService.HandleClientMessageAsync(session1.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });
        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.SetReady,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new SetReadyRequest { IsReady = true })
        });

        var room = roomService.GetDebugRoom(roomId)!;
        room.MatchState!.Phase = DuelPhaseName.Defense;
        room.MatchState.ActiveSeatIndex = 0;

        await roomService.HandleClientMessageAsync(session2.SessionId, new ClientEnvelope
        {
            Type = ClientMessageTypes.EndPhase,
            Payload = System.Text.Json.JsonSerializer.SerializeToElement(new EndPhaseRequest())
        });

        Assert.Equal(DuelPhaseName.Resolve, room.MatchState.Phase);
        Assert.Equal(0, room.MatchState.ActiveSeatIndex);
    }
}
