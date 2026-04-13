namespace ProjectDuel.Shared.Protocol;

public static class ClientMessageTypes
{
    public const string Hello = "hello";
    public const string CreateRoom = "create_room";
    public const string JoinRoom = "join_room";
    public const string SetReady = "set_ready";
    public const string StartMatch = "start_match";
    public const string EndPhase = "end_phase";
    public const string PlayCards = "play_cards";
    public const string TakeBackPlayedCard = "take_back_played_card";
    public const string ActivatePrimarySkill = "activate_primary_skill";
    public const string SelectAttackSkill = "select_attack_skill";
    public const string SelectDefenseSkill = "select_defense_skill";
    public const string UseMorale = "use_morale";
    public const string Ping = "ping";
}

public static class ServerMessageTypes
{
    public const string Connected = "connected";
    public const string RoomCreated = "room_created";
    public const string RoomJoined = "room_joined";
    public const string RoomSnapshot = "room_snapshot";
    public const string BattleSnapshot = "battle_snapshot";
    public const string MatchStarted = "match_started";
    public const string CommandRejected = "command_rejected";
    public const string Pong = "pong";
}
