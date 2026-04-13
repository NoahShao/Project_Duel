using System;
using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// Unity ??????????????????
    /// </summary>
    public static class OnlineClientMessageTypes
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

    /// <summary>
    /// ??????? Unity ???????????
    /// </summary>
    public static class OnlineServerMessageTypes
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

    public enum OnlineDuelPhaseName
    {
        Preparation,
        Income,
        Primary,
        Main,
        Defense,
        Resolve,
        Discard,
        TurnEnd,
    }

    public enum OnlineRoomStatus
    {
        Waiting,
        ReadyCheck,
        InGame,
        Finished,
    }

    [Serializable] public class OnlineDeckSelectionDto { public string DeckId = string.Empty; public string DisplayName = string.Empty; public string RemovedSuit = string.Empty; public List<string> CardIds = new List<string>(); public static OnlineDeckSelectionDto FromDeckData(DeckData deck){ return new OnlineDeckSelectionDto{ DeckId = deck?.Id ?? string.Empty, DisplayName = deck?.DisplayName ?? string.Empty, RemovedSuit = deck?.RemovedSuit ?? string.Empty, CardIds = deck?.CardIds != null ? new List<string>(deck.CardIds) : new List<string>()}; } }
    [Serializable] public class OnlineHelloRequest { public string PlayerName = string.Empty; }
    [Serializable] public class OnlineCreateRoomRequest { public string PlayerName = string.Empty; public OnlineDeckSelectionDto Deck = new OnlineDeckSelectionDto(); }
    [Serializable] public class OnlineJoinRoomRequest { public string RoomId = string.Empty; public string PlayerName = string.Empty; public OnlineDeckSelectionDto Deck = new OnlineDeckSelectionDto(); }
    [Serializable] public class OnlineSetReadyRequest { public bool IsReady; }
    [Serializable] public class OnlinePlayCardsRequest { public List<int> HandIndices = new List<int>(); }
    [Serializable] public class OnlineTakeBackPlayedCardRequest { public int PlayedIndex; }
    [Serializable] public class OnlineSelectSkillRequest { public int GeneralIndex; public int SkillIndex; }
    [Serializable] public class OnlineUseMoraleRequest { public int EffectIndex; public int GeneralIndex = -1; public bool HasGeneralIndex; }
    [Serializable] public class OnlineConnectedResponse { public string SessionId = string.Empty; }
    [Serializable] public class OnlineRoomCreatedResponse { public string RoomId = string.Empty; }
    [Serializable] public class OnlineRoomJoinedResponse { public string RoomId = string.Empty; }
    [Serializable] public class OnlineMatchStartedResponse { public string RoomId = string.Empty; }
    [Serializable] public class OnlineErrorResponse { public string Code = string.Empty; public string Message = string.Empty; }
    [Serializable] public class OnlinePlayerSlotSnapshot { public int SeatIndex; public string SessionId = string.Empty; public string PlayerName = string.Empty; public string DeckId = string.Empty; public bool IsReady; public bool IsConnected; }
    [Serializable] public class OnlineRoomSnapshotResponse { public string RoomId = string.Empty; public OnlineRoomStatus Status; public int TurnNumber; public int ActiveSeatIndex; public OnlineDuelPhaseName Phase; public List<OnlinePlayerSlotSnapshot> Players = new List<OnlinePlayerSlotSnapshot>(); }
    [Serializable] public class OnlineBattleCardDto { public string Suit = string.Empty; public int Rank; public string DisplayName = string.Empty; }
    [Serializable] public class OnlineBattleSideSnapshot { public int SeatIndex; public string PlayerName = string.Empty; public string DeckId = string.Empty; public int DeckCount; public int HandCount; public int DiscardCount; public int CurrentHp; public int MaxHp; public int Morale; public int MoraleCap = 2; public List<bool> MoraleUsedThisTurn = new List<bool>(); public List<string> GeneralCardIds = new List<string>(); public List<bool> GeneralFaceUp = new List<bool>(); public List<OnlineBattleCardDto> DiscardTopPreview = new List<OnlineBattleCardDto>(); public List<OnlineBattleCardDto> DiscardCards = new List<OnlineBattleCardDto>(); }
    [Serializable] public class OnlineBattleSnapshotResponse { public string RoomId = string.Empty; public int LocalSeatIndex; public int ActiveSeatIndex; public int TurnNumber; public OnlineDuelPhaseName Phase; public int HandLimit; public int TotalPlayPhasesThisTurn; public int CurrentPlayPhaseIndex; public string PendingAttackSkillName = string.Empty; public string PendingDefenseSkillName = string.Empty; public OnlineBattleSideSnapshot Self = new OnlineBattleSideSnapshot(); public OnlineBattleSideSnapshot Opponent = new OnlineBattleSideSnapshot(); public List<OnlineBattleCardDto> SelfHand = new List<OnlineBattleCardDto>(); public List<OnlineBattleCardDto> PlayedCards = new List<OnlineBattleCardDto>(); }
    [Serializable] public class OnlinePongResponse { public string Now = string.Empty; }
    [Serializable] public class EmptyPayload { }
}
