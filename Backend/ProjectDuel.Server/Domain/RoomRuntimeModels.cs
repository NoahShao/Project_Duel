using System.Net.WebSockets;
using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.Rules;

namespace ProjectDuel.Server.Domain;

public sealed class ConnectedSession
{
    public string SessionId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DeckSelectionDto Deck { get; set; } = new();
    public WebSocket? Socket { get; set; }
}

public sealed class RoomPlayerSlot
{
    public int SeatIndex { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DeckSelectionDto Deck { get; set; } = new();
    public bool IsReady { get; set; }
    public bool IsConnected { get; set; } = true;
}

public sealed class DuelRoom
{
    public string RoomId { get; set; } = string.Empty;
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public int TurnNumber { get; set; }
    public int ActiveSeatIndex { get; set; }
    public DuelPhaseName Phase { get; set; } = DuelPhaseName.Preparation;
    public List<RoomPlayerSlot> Players { get; set; } = new();
    public AuthoritativeBattleState? MatchState { get; set; }
}
