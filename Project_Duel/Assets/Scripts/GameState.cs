using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    public struct PokerCard
    {
        public string Suit;
        public int Rank;
        /// <summary>为 true 表示本张并非牌库手牌，而是角色区「当牌打出」的代理；结算后不进入弃牌堆。</summary>
        public bool PlayedAsGeneral;
        /// <summary>仅在 <see cref="PlayedAsGeneral"/> 时有效：角色在 <see cref="SideState.GeneralCardIds"/> 中的下标。</summary>
        public int GeneralSlotIndex;
        /// <summary>打出区展示用角色名（可选）。</summary>
        public string PlayedRoleDisplayName;

        public string DisplayName
        {
            get
            {
                string rankText = Rank switch
                {
                    1 => "A",
                    11 => "J",
                    12 => "Q",
                    13 => "K",
                    _ => Rank.ToString()
                };
                return (Suit ?? string.Empty) + rankText;
            }
        }
    }

    public sealed class SideState
    {
        public List<PokerCard> Deck = new List<PokerCard>();
        public List<PokerCard> Hand = new List<PokerCard>();
        public List<PokerCard> DiscardPile = new List<PokerCard>();
        public List<PokerCard> PlayedThisPhase = new List<PokerCard>();
        public List<string> GeneralCardIds = new List<string>();
        public List<bool> GeneralFaceUp = new List<bool>();
        public List<int> FaceDownRecoverAfterOwnTurnEnds = new List<int>();
        public HashSet<string> UsedOneShotSkills = new HashSet<string>();
        public HashSet<string> TriggeredSkillKeysThisTurn = new HashSet<string>();
        public Dictionary<string, int> EffectLayers = new Dictionary<string, int>();
        public int Morale;
        public int MoraleCap = 2;
        public bool[] MoraleUsedThisTurn = new bool[3];
        public int CurrentHp = 30;
        public int MaxHp = 30;

        public void ResetMoraleUsedThisTurn()
        {
            MoraleUsedThisTurn[0] = false;
            MoraleUsedThisTurn[1] = false;
            MoraleUsedThisTurn[2] = false;
        }

        public void ResetTriggeredSkillsThisTurn()
        {
            TriggeredSkillKeysThisTurn.Clear();
        }

        public int GetEffectLayerCount(string effectKey)
        {
            if (string.IsNullOrWhiteSpace(effectKey))
                return 0;

            return EffectLayers.TryGetValue(effectKey, out int count) ? Mathf.Max(0, count) : 0;
        }

        public void AddEffectLayers(string effectKey, int count)
        {
            if (string.IsNullOrWhiteSpace(effectKey) || count <= 0)
                return;

            EffectLayers[effectKey] = GetEffectLayerCount(effectKey) + count;
        }

        public int RemoveEffectLayers(string effectKey, int count)
        {
            if (string.IsNullOrWhiteSpace(effectKey) || count <= 0)
                return 0;

            int current = GetEffectLayerCount(effectKey);
            if (current <= 0)
                return 0;

            int removed = Mathf.Min(current, count);
            current -= removed;
            if (current <= 0)
                EffectLayers.Remove(effectKey);
            else
                EffectLayers[effectKey] = current;
            return removed;
        }

        public int RemoveAnyEffectLayers(int maxCount)
        {
            if (maxCount <= 0 || EffectLayers.Count == 0)
                return 0;

            var keys = new List<string>(EffectLayers.Keys);
            int removed = 0;
            for (int i = 0; i < keys.Count && removed < maxCount; i++)
                removed += RemoveEffectLayers(keys[i], maxCount - removed);
            return removed;
        }

        public void EnsureGeneralStateCount(int count)
        {
            while (GeneralFaceUp.Count < count)
                GeneralFaceUp.Add(true);
            while (FaceDownRecoverAfterOwnTurnEnds.Count < count)
                FaceDownRecoverAfterOwnTurnEnds.Add(0);
            if (GeneralFaceUp.Count > count)
                GeneralFaceUp.RemoveRange(count, GeneralFaceUp.Count - count);
            if (FaceDownRecoverAfterOwnTurnEnds.Count > count)
                FaceDownRecoverAfterOwnTurnEnds.RemoveRange(count, FaceDownRecoverAfterOwnTurnEnds.Count - count);
        }

        public int GetFaceUpGeneralCount()
        {
            int count = 0;
            for (int i = 0; i < GeneralFaceUp.Count; i++)
            {
                if (GeneralFaceUp[i])
                    count++;
            }
            return count;
        }

        public bool IsGeneralFaceUp(int index)
        {
            return index >= 0 && index < GeneralFaceUp.Count && GeneralFaceUp[index];
        }

        public bool FlipGeneralFaceDown(int index, bool duringOwnersTurn)
        {
            if (index < 0 || index >= GeneralFaceUp.Count || !GeneralFaceUp[index])
                return false;

            GeneralFaceUp[index] = false;
            FaceDownRecoverAfterOwnTurnEnds[index] = duringOwnersTurn ? 2 : 1;
            return true;
        }

        /// <summary>士气等：将已翻面武将翻回正面，并清除自动翻回计时。</summary>
        public bool UnflipGeneralFromMorale(int index)
        {
            if (index < 0 || index >= GeneralFaceUp.Count || GeneralFaceUp[index])
                return false;

            GeneralFaceUp[index] = true;
            FaceDownRecoverAfterOwnTurnEnds[index] = 0;
            return true;
        }

        public void RecoverGeneralsAtOwnTurnEnd()
        {
            for (int i = 0; i < GeneralFaceUp.Count; i++)
            {
                if (GeneralFaceUp[i] || FaceDownRecoverAfterOwnTurnEnds[i] <= 0)
                    continue;

                FaceDownRecoverAfterOwnTurnEnds[i]--;
                if (FaceDownRecoverAfterOwnTurnEnds[i] <= 0)
                {
                    FaceDownRecoverAfterOwnTurnEnds[i] = 0;
                    GeneralFaceUp[i] = true;
                }
            }
        }

        public void MovePlayedCardsToDiscard()
        {
            for (int i = 0; i < PlayedThisPhase.Count; i++)
            {
                var c = PlayedThisPhase[i];
                if (!c.PlayedAsGeneral)
                    DiscardPile.Add(c);
            }

            PlayedThisPhase.Clear();
        }

        /// <summary>打出区中来自手牌的张数（角色当牌打出不计入，不受 <see cref="BattleState.MaxPlayPerPhase"/> 与手牌张数混用上限）。</summary>
        public int CountNonGeneralCardsInPlayedZone()
        {
            int n = 0;
            for (int i = 0; i < PlayedThisPhase.Count; i++)
            {
                if (!PlayedThisPhase[i].PlayedAsGeneral)
                    n++;
            }

            return n;
        }
    }

    public enum BattlePhase
    {
        Preparation,
        Income,
        Primary,
        Main,
        Defense,
        Resolve,
        Discard,
        TurnEnd
    }

    public static class BattlePhaseDisplay
    {
        public static string ToChinese(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Preparation: return "\u51c6\u5907\u9636\u6bb5";
                case BattlePhase.Income: return "\u6536\u5165\u9636\u6bb5";
                case BattlePhase.Primary: return "\u4e3b\u8981\u9636\u6bb5";
                case BattlePhase.Main: return "\u51fa\u724c\u9636\u6bb5";
                case BattlePhase.Defense: return "\u9632\u5fa1\u9636\u6bb5";
                case BattlePhase.Resolve: return "\u7ed3\u7b97\u9636\u6bb5";
                case BattlePhase.Discard: return "\u5f03\u724c\u9636\u6bb5";
                case BattlePhase.TurnEnd: return "\u56de\u5408\u7ed3\u675f";
                default: return phase.ToString();
            }
        }
    }

    public enum PhaseStep
    {
        Start,
        Main,
        End
    }

    public enum SelectedSkillKind
    {
        None,
        GenericAttack,
        GeneralSkill
    }

    public sealed class BattleState
    {
        public const int DefaultHandLimit = 7;
        public const int DefaultMoraleCap = 2;
        public const int MaxMorale = 2;
        public const int MaxPlayPerPhase = 5;
        /// <summary>通用攻击枚举子集时参与组合的上限（含角色代理牌）；避免张数过大时 2^n 爆炸。</summary>
        public const int MaxCardsEvaluatedForGenericAttack = 12;

        public static readonly string[] Suits = { "红桃", "方片", "黑桃", "梅花" };

        public SideState Player = new SideState();
        public SideState Opponent = new SideState();
        public bool IsPlayerTurn = true;
        public bool PlayerGoesFirst = true;
        public int TurnNumber = 1;
        public int HandLimit = DefaultHandLimit;
        public BattlePhase CurrentPhase = BattlePhase.Preparation;
        public PhaseStep CurrentPhaseStep = PhaseStep.Start;
        public int TotalPlayPhasesThisTurn = 1;
        public int CurrentPlayPhaseIndex;

        public int PendingBaseDamage;
        /// <summary>本次攻击的伤害大类与属性元素（结算、战报用语）。</summary>
        public DamageCategory PendingDamageCategory;
        public DamageElement PendingDamageElement;
        public int PendingAttackBonus;
        public int PendingDefenseReduction;
        public int PendingAttackGeneralIndex = -1;
        public int PendingAttackSkillIndex = -1;
        public int PendingDefenseGeneralIndex = -1;
        public int PendingDefenseSkillIndex = -1;
        public string PendingAttackSkillName = string.Empty;
        public string PendingDefenseSkillName = string.Empty;
        public SelectedSkillKind PendingAttackSkillKind = SelectedSkillKind.None;
        public SelectedSkillKind PendingDefenseSkillKind = SelectedSkillKind.None;
        public bool PendingIgnoreDefenseReduction;
        /// <summary>多牌型攻击技由玩家在二级弹窗选择；当前用于【策马斩将】0=两张红色单牌 1=红色顺子 2=红色同花顺；-1 表示未选或由 AI 自动择优。</summary>
        public int PendingAttackPatternVariant = -1;
        /// <summary>通用攻击：玩家在 <see cref="GenericAttackShapes.BuildSortedOptions"/> 列表中的选项下标；-1 表示未指定（多选项时由 AI 择优或等待玩家选择）。</summary>
        public int PendingGenericAttackOptionIndex = -1;
        public bool PendingGenericAttackShapeChoicePending;
        public string PendingGenericAttackShapeDisplayName = string.Empty;
        public int PendingPostResolveDrawToAttacker;
        public int PendingPostResolveHealToAttacker;
        public int PendingPostResolveMoraleToAttacker;
        /// <summary>攻击技在「结算」时计入的额外出牌阶段数（与是否造成伤害无关）；结算后并入 <see cref="TotalPlayPhasesThisTurn"/>。</summary>
        public int PendingExtraPlayPhasesToGrant;
        public string PendingCombatNote = string.Empty;
        public int PlayPhaseStartPromptMask;
        public bool PlayPhaseStartInitialized;
        public bool DefenseBuffStepDone;
        public bool DefenseSkillLocked;

        /// <summary>已结算「游戏开始时」士气上限/恢复类技能（<c>start_game_gain_morale_and_max</c>），用于与点击触发顺序一致。</summary>
        public readonly HashSet<(bool sideIsPlayer, int generalIndex, int skillIndex)> AppliedGameStartMoraleEffects =
            new HashSet<(bool, int, int)>();

        public SideState ActiveSide => IsPlayerTurn ? Player : Opponent;
        public SideState InactiveSide => IsPlayerTurn ? Opponent : Player;

        /// <summary>
        /// 本局<strong>先手方</strong>是否为己方（<see cref="Player"/>）。与 <see cref="PlayerGoesFirst"/> 同值：
        /// 先手为己方时 true，先手为敌方时 false。开局时与 <see cref="IsPlayerTurn"/> 一致；之后 IsPlayerTurn 会随回合切换，本属性仍表示固定先后手。
        /// </summary>
        public bool InitiativeSideIsPlayer => PlayerGoesFirst;

        public SideState GetSide(bool isPlayer)
        {
            return isPlayer ? Player : Opponent;
        }

        public void ClearPendingCombat()
        {
            PendingBaseDamage = 0;
            PendingDamageCategory = DamageCategory.None;
            PendingDamageElement = DamageElement.None;
            PendingAttackBonus = 0;
            PendingDefenseReduction = 0;
            PendingAttackGeneralIndex = -1;
            PendingAttackSkillIndex = -1;
            PendingDefenseGeneralIndex = -1;
            PendingDefenseSkillIndex = -1;
            PendingAttackSkillName = string.Empty;
            PendingDefenseSkillName = string.Empty;
            PendingAttackSkillKind = SelectedSkillKind.None;
            PendingDefenseSkillKind = SelectedSkillKind.None;
            PendingIgnoreDefenseReduction = false;
            PendingAttackPatternVariant = -1;
            PendingGenericAttackOptionIndex = -1;
            PendingGenericAttackShapeChoicePending = false;
            PendingGenericAttackShapeDisplayName = string.Empty;
            PendingPostResolveDrawToAttacker = 0;
            PendingPostResolveHealToAttacker = 0;
            PendingPostResolveMoraleToAttacker = 0;
            PendingExtraPlayPhasesToGrant = 0;
            PendingCombatNote = string.Empty;
            DefenseBuffStepDone = false;
            DefenseSkillLocked = false;
        }

        public void FinishCurrentPlayPhaseCombat()
        {
            ActiveSide.MovePlayedCardsToDiscard();
            ClearPendingCombat();
        }

        public bool TryFlipGeneral(bool isPlayerSide, int generalIndex)
        {
            var side = GetSide(isPlayerSide);
            return side.FlipGeneralFaceDown(generalIndex, IsPlayerTurn == isPlayerSide);
        }

        public void CompleteCurrentTurn()
        {
            ActiveSide.RecoverGeneralsAtOwnTurnEnd();
            ActiveSide.ResetMoraleUsedThisTurn();
            ActiveSide.ResetTriggeredSkillsThisTurn();
            InactiveSide.ResetTriggeredSkillsThisTurn();
            TotalPlayPhasesThisTurn = 1;
            CurrentPlayPhaseIndex = 0;
            ActiveSide.PlayedThisPhase.Clear();
            ClearPendingCombat();
            PlayPhaseStartPromptMask = 0;
            PlayPhaseStartInitialized = false;
        }

        public static List<PokerCard> CreateShuffledDeck(DeckData deck)
        {
            var list = new List<PokerCard>();
            string removedSuit = string.IsNullOrWhiteSpace(deck?.RemovedSuit) ? null : deck.RemovedSuit.Trim();

            for (int suitIndex = 0; suitIndex < Suits.Length; suitIndex++)
            {
                string suit = Suits[suitIndex];
                if (removedSuit != null && suit == removedSuit)
                    continue;

                for (int rank = 1; rank <= 13; rank++)
                    list.Add(new PokerCard { Suit = suit, Rank = rank });
            }

            Shuffle(list);
            return list;
        }

        public static void Shuffle(List<PokerCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                PokerCard temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        public static int Draw(SideState side, int count)
        {
            int actual = 0;
            while (count > 0)
            {
                if (side.Deck.Count == 0)
                {
                    if (side.DiscardPile.Count == 0)
                        break;
                    ReshuffleDiscardIntoDeck(side);
                }

                if (side.Deck.Count == 0)
                    break;

                PokerCard card = side.Deck[side.Deck.Count - 1];
                side.Deck.RemoveAt(side.Deck.Count - 1);
                side.Hand.Add(card);
                actual++;
                count--;
            }

            return actual;
        }

        private static void ReshuffleDiscardIntoDeck(SideState side)
        {
            if (side == null || side.DiscardPile.Count == 0)
                return;

            side.Deck.AddRange(side.DiscardPile);
            side.DiscardPile.Clear();
            Shuffle(side.Deck);
        }

        public static void DiscardFromHand(SideState side, List<int> handIndices)
        {
            if (handIndices == null || handIndices.Count == 0)
                return;

            handIndices.Sort((left, right) => right.CompareTo(left));
            for (int i = 0; i < handIndices.Count; i++)
            {
                int handIndex = handIndices[i];
                if (handIndex < 0 || handIndex >= side.Hand.Count)
                    continue;

                PokerCard card = side.Hand[handIndex];
                side.Hand.RemoveAt(handIndex);
                side.DiscardPile.Add(card);
            }
        }

        public void InitFromDecks(DeckData playerDeck, DeckData opponentDeck)
        {
            Player.Deck = CreateShuffledDeck(playerDeck);
            Player.Hand.Clear();
            Player.DiscardPile.Clear();
            Player.PlayedThisPhase.Clear();
            Player.GeneralCardIds.Clear();
            for (int i = 0; i < playerDeck.CardIds.Count && i < 3; i++)
                Player.GeneralCardIds.Add(playerDeck.CardIds[i]);
            Player.EnsureGeneralStateCount(Player.GeneralCardIds.Count);
            Player.Morale = 0;
            Player.MoraleCap = DefaultMoraleCap;
            Player.ResetMoraleUsedThisTurn();
            Player.ResetTriggeredSkillsThisTurn();
            Player.EffectLayers.Clear();
            Player.CurrentHp = 30;
            Player.MaxHp = 30;
            Player.UsedOneShotSkills.Clear();
            AppliedGameStartMoraleEffects.Clear();

            Opponent.Deck = CreateShuffledDeck(opponentDeck);
            Opponent.Hand.Clear();
            Opponent.DiscardPile.Clear();
            Opponent.PlayedThisPhase.Clear();
            Opponent.GeneralCardIds.Clear();
            for (int i = 0; i < opponentDeck.CardIds.Count && i < 3; i++)
                Opponent.GeneralCardIds.Add(opponentDeck.CardIds[i]);
            Opponent.EnsureGeneralStateCount(Opponent.GeneralCardIds.Count);
            Opponent.Morale = 0;
            Opponent.MoraleCap = DefaultMoraleCap;
            Opponent.ResetMoraleUsedThisTurn();
            Opponent.ResetTriggeredSkillsThisTurn();
            Opponent.EffectLayers.Clear();
            Opponent.CurrentHp = 30;
            Opponent.MaxHp = 30;
            Opponent.UsedOneShotSkills.Clear();

            IsPlayerTurn = true;
            PlayerGoesFirst = true;
            TurnNumber = 1;
            HandLimit = DefaultHandLimit;
            CurrentPhase = BattlePhase.Preparation;
            CurrentPhaseStep = PhaseStep.Start;
            TotalPlayPhasesThisTurn = 1;
            CurrentPlayPhaseIndex = 0;
            PlayPhaseStartPromptMask = 0;
            PlayPhaseStartInitialized = false;
            ClearPendingCombat();
        }

        /// <summary>整理手牌：红桃→方片→黑桃→梅花→其他，同花色点数升序（A 最小）。</summary>
        public static int SuitOrganizeOrder(string suit)
        {
            string s = (suit ?? string.Empty).Trim();
            if (s == "\u7ea2\u6843") return 0;
            if (s == "\u65b9\u7247" || s == "\u65b9\u5757") return 1;
            if (s == "\u9ed1\u6843") return 2;
            if (s == "\u6885\u82b1") return 3;
            return 4;
        }

        public static int CompareHandCardsOrganize(PokerCard a, PokerCard b)
        {
            int c = SuitOrganizeOrder(a.Suit).CompareTo(SuitOrganizeOrder(b.Suit));
            if (c != 0) return c;
            return a.Rank.CompareTo(b.Rank);
        }

        public static void SortHandOrganize(List<PokerCard> hand)
        {
            if (hand == null || hand.Count < 2)
                return;
            hand.Sort(CompareHandCardsOrganize);
        }

        public static bool HandCardIdentityEquals(PokerCard a, PokerCard b)
        {
            return a.Suit == b.Suit && a.Rank == b.Rank && a.PlayedAsGeneral == b.PlayedAsGeneral
                && a.GeneralSlotIndex == b.GeneralSlotIndex
                && string.Equals(a.PlayedRoleDisplayName ?? "", b.PlayedRoleDisplayName ?? "", StringComparison.Ordinal);
        }
    }
}
