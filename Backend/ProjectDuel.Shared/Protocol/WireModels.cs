using System.Text.Json;

namespace ProjectDuel.Shared.Protocol;

public sealed class ClientEnvelope
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}

public sealed class ServerEnvelope
{
    public string Type { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

public sealed class DeckSelectionDto
{
    public string DeckId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RemovedSuit { get; set; } = string.Empty;
    public List<string> CardIds { get; set; } = new();
}

public sealed class HelloRequest
{
    public string PlayerName { get; set; } = string.Empty;
}

public sealed class CreateRoomRequest
{
    public string PlayerName { get; set; } = string.Empty;
    public DeckSelectionDto Deck { get; set; } = new();
}

public sealed class JoinRoomRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DeckSelectionDto Deck { get; set; } = new();
}

public sealed class SetReadyRequest
{
    public bool IsReady { get; set; }
}

public sealed class EndPhaseRequest
{
}

public sealed class PlayCardsRequest
{
    public List<int> HandIndices { get; set; } = new();
}

public sealed class TakeBackPlayedCardRequest
{
    public int PlayedIndex { get; set; }
}

public sealed class SelectSkillRequest
{
    public int GeneralIndex { get; set; }
    public int SkillIndex { get; set; }
}

public sealed class UseMoraleRequest
{
    public int EffectIndex { get; set; }
    public int? GeneralIndex { get; set; }
}

public sealed class ConnectedResponse
{
    public string SessionId { get; set; } = string.Empty;
}

public sealed class RoomCreatedResponse
{
    public string RoomId { get; set; } = string.Empty;
}

public sealed class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
