using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 单张扑克牌（牌堆/弃牌堆/手牌用）：花色 + 点数。
    /// </summary>
    public struct PokerCard
    {
        public string Suit;
        public int Rank;

        public string DisplayName
        {
            get
            {
                string r = Rank switch { 1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => Rank.ToString() };
                return (Suit ?? "") + r;
            }
        }
    }

    /// <summary>
    /// 一方玩家的对局数据：牌堆、手牌、弃牌堆、3 张武将 ID、士气及本回合已用士气效果。
    /// </summary>
    public class SideState
    {
        public List<PokerCard> Deck = new List<PokerCard>();
        public List<PokerCard> Hand = new List<PokerCard>();
        public List<PokerCard> DiscardPile = new List<PokerCard>();
        /// <summary> 本回合出牌阶段中打出的牌（拖到角色区上方），阶段结束或回合结束时收回手牌。 </summary>
        public List<PokerCard> PlayedThisPhase = new List<PokerCard>();
        public List<string> GeneralCardIds = new List<string>();
        /// <summary> 当前士气点数（0/1/2），收入阶段 +1，上限可后续定。 </summary>
        public int Morale;
        /// <summary> 本回合已使用的士气效果：0=摸两张牌，1=增加出牌阶段，2=一名己方角色翻面。 </summary>
        public bool[] MoraleUsedThisTurn = new bool[3];
        /// <summary> 当前血量，为 0 时判负。 </summary>
        public int CurrentHp = 30;
        /// <summary> 血量上限，默认 30。 </summary>
        public int MaxHp = 30;

        public void ResetMoraleUsedThisTurn()
        {
            MoraleUsedThisTurn[0] = MoraleUsedThisTurn[1] = MoraleUsedThisTurn[2] = false;
        }
    }

    /// <summary>
    /// 回合阶段。
    /// </summary>
    public enum BattlePhase
    {
        Preparation,  // 准备阶段
        Income,       // 收入阶段
        Primary,      // 主要阶段
        Main,         // 出牌阶段（本回合可有多段，由 TotalPlayPhasesThisTurn 决定）
        Discard,      // 弃牌阶段
        TurnEnd       // 回合结束
    }

    /// <summary>
    /// 阶段内节点：开始时 → 效果触发时 → 结束时。
    /// </summary>
    public enum PhaseStep
    {
        Start,
        Main,
        End
    }

    /// <summary>
    /// 对局状态：双方独立数据、阶段、回合。与 GameUI 解耦，便于后续联网。
    /// </summary>
    public class BattleState
    {
        public const int DefaultHandLimit = 6;
        public const int MaxMorale = 2;
        /// <summary> 每段出牌阶段最多打出的牌数。 </summary>
        public const int MaxPlayPerPhase = 5;

        public SideState Player = new SideState();
        public SideState Opponent = new SideState();
        public bool IsPlayerTurn = true;
        public int HandLimit = DefaultHandLimit;
        public BattlePhase CurrentPhase = BattlePhase.Preparation;
        public PhaseStep CurrentPhaseStep = PhaseStep.Start;
        /// <summary> 本回合出牌阶段总次数（默认 1，士气「增加出牌阶段」可加）。 </summary>
        public int TotalPlayPhasesThisTurn = 1;
        /// <summary> 当前是第几段出牌阶段（0 基），用于判断是否还有下一段。 </summary>
        public int CurrentPlayPhaseIndex = 0;

        public static string[] Suits = new[] { "红桃", "方片", "黑桃", "梅花" };

        public SideState ActiveSide => IsPlayerTurn ? Player : Opponent;
        public SideState InactiveSide => IsPlayerTurn ? Opponent : Player;

        /// <summary>
        /// 根据套牌生成初始牌堆（52 或 39），并洗牌。
        /// </summary>
        public static List<PokerCard> CreateShuffledDeck(DeckData deck)
        {
            var list = new List<PokerCard>();
            string removed = string.IsNullOrWhiteSpace(deck?.RemovedSuit) ? null : deck.RemovedSuit.Trim();
            foreach (var suit in Suits)
            {
                if (removed != null && suit == removed) continue;
                for (int r = 1; r <= 13; r++)
                    list.Add(new PokerCard { Suit = suit, Rank = r });
            }
            Shuffle(list);
            return list;
        }

        public static void Shuffle(List<PokerCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        /// <summary>
        /// 从牌堆摸 n 张到手牌，返回实际摸到的数量。
        /// </summary>
        public static int Draw(SideState side, int n)
        {
            int count = 0;
            while (n > 0 && side.Deck.Count > 0)
            {
                var card = side.Deck[side.Deck.Count - 1];
                side.Deck.RemoveAt(side.Deck.Count - 1);
                side.Hand.Add(card);
                count++;
                n--;
            }
            return count;
        }

        /// <summary>
        /// 将手牌中指定索引的牌弃入弃牌堆。
        /// </summary>
        public static void DiscardFromHand(SideState side, List<int> handIndices)
        {
            if (handIndices == null || handIndices.Count == 0) return;
            handIndices.Sort((a, b) => b.CompareTo(a));
            foreach (int i in handIndices)
            {
                if (i >= 0 && i < side.Hand.Count)
                {
                    var card = side.Hand[i];
                    side.Hand.RemoveAt(i);
                    side.DiscardPile.Add(card);
                }
            }
        }

        /// <summary>
        /// 初始化双方：各自牌堆、武将 ID；不包含摸 6 张（由阶段驱动在游戏开始时执行）。
        /// </summary>
        public void InitFromDecks(DeckData playerDeck, DeckData opponentDeck)
        {
            Player.Deck = CreateShuffledDeck(playerDeck);
            Player.Hand.Clear();
            Player.DiscardPile.Clear();
            Player.PlayedThisPhase.Clear();
            Player.GeneralCardIds.Clear();
            for (int i = 0; i < playerDeck.CardIds.Count && i < 3; i++)
                Player.GeneralCardIds.Add(playerDeck.CardIds[i]);
            Player.Morale = 0;
            Player.ResetMoraleUsedThisTurn();
            Player.CurrentHp = Player.MaxHp = 30;

            Opponent.Deck = CreateShuffledDeck(opponentDeck);
            Opponent.Hand.Clear();
            Opponent.DiscardPile.Clear();
            Opponent.PlayedThisPhase.Clear();
            Opponent.GeneralCardIds.Clear();
            for (int i = 0; i < opponentDeck.CardIds.Count && i < 3; i++)
                Opponent.GeneralCardIds.Add(opponentDeck.CardIds[i]);
            Opponent.Morale = 0;
            Opponent.ResetMoraleUsedThisTurn();
            Opponent.CurrentHp = Opponent.MaxHp = 30;

            IsPlayerTurn = true;
            CurrentPhase = BattlePhase.Preparation;
            CurrentPhaseStep = PhaseStep.Start;
        }
    }
}
