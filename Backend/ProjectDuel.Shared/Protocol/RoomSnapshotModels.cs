namespace ProjectDuel.Shared.Protocol;

public sealed class RoomSnapshotResponse
{
    public string RoomId { get; set; } = string.Empty;
    public RoomStatus Status { get; set; }
    public int TurnNumber { get; set; }
    public int ActiveSeatIndex { get; set; }
    public DuelPhaseName Phase { get; set; }
    public List<PlayerSlotSnapshot> Players { get; set; } = new();
}

public sealed class PlayerSlotSnapshot
{
    public int SeatIndex { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string DeckId { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsConnected { get; set; }
}
