using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 临时替换打出区并调用 <see cref="OfflineSkillEngine.ConfigureAttackSkill"/> 以评估某一出牌+攻击技组合的强度，再恢复原状态。
    /// </summary>
    public static class BattleAttackPreview
    {
        /// <summary>为 true 时跳过技能横幅（如 AI 预览多次调用 ConfigureAttackSkill）。</summary>
        public static bool SuppressSkillBanners { get; set; }

        private struct Snapshot
        {
            public List<PokerCard> PlayedBackup;
            public int PendingBaseDamage;
            public int PendingAttackBonus;
            public bool PendingIgnoreDefenseReduction;
            public int PendingPostResolveDrawToAttacker;
            public int PendingPostResolveHealToAttacker;
            public int PendingPostResolveMoraleToAttacker;
            public string PendingCombatNote;
            public int TotalPlayPhasesThisTurn;
            public int PendingExtraPlayPhasesToGrant;
            public int PendingAttackPatternVariant;
            public SelectedSkillKind PendingAttackSkillKind;
            public int PendingGenericAttackOptionIndex;
            public bool PendingGenericAttackShapeChoicePending;
            public string PendingGenericAttackShapeDisplayName;
            public DamageCategory PendingDamageCategory;
            public DamageElement PendingDamageElement;
            public bool HuBuGuanYouWindowConsumedForCurrentAttack;
        }

        private static Snapshot Capture(BattleState state)
        {
            var snap = new Snapshot
            {
                PlayedBackup = new List<PokerCard>(state.ActiveSide.PlayedThisPhase),
                PendingBaseDamage = state.PendingBaseDamage,
                PendingAttackBonus = state.PendingAttackBonus,
                PendingIgnoreDefenseReduction = state.PendingIgnoreDefenseReduction,
                PendingPostResolveDrawToAttacker = state.PendingPostResolveDrawToAttacker,
                PendingPostResolveHealToAttacker = state.PendingPostResolveHealToAttacker,
                PendingPostResolveMoraleToAttacker = state.PendingPostResolveMoraleToAttacker,
                PendingCombatNote = state.PendingCombatNote ?? string.Empty,
                TotalPlayPhasesThisTurn = state.TotalPlayPhasesThisTurn,
                PendingExtraPlayPhasesToGrant = state.PendingExtraPlayPhasesToGrant,
                PendingAttackPatternVariant = state.PendingAttackPatternVariant,
                PendingAttackSkillKind = state.PendingAttackSkillKind,
                PendingGenericAttackOptionIndex = state.PendingGenericAttackOptionIndex,
                PendingGenericAttackShapeChoicePending = state.PendingGenericAttackShapeChoicePending,
                PendingGenericAttackShapeDisplayName = state.PendingGenericAttackShapeDisplayName ?? string.Empty,
                PendingDamageCategory = state.PendingDamageCategory,
                PendingDamageElement = state.PendingDamageElement,
                HuBuGuanYouWindowConsumedForCurrentAttack = state.HuBuGuanYouWindowConsumedForCurrentAttack,
            };
            return snap;
        }

        private static void Restore(BattleState state, Snapshot snap)
        {
            state.ActiveSide.PlayedThisPhase.Clear();
            state.ActiveSide.PlayedThisPhase.AddRange(snap.PlayedBackup);
            state.PendingBaseDamage = snap.PendingBaseDamage;
            state.PendingAttackBonus = snap.PendingAttackBonus;
            state.PendingIgnoreDefenseReduction = snap.PendingIgnoreDefenseReduction;
            state.PendingPostResolveDrawToAttacker = snap.PendingPostResolveDrawToAttacker;
            state.PendingPostResolveHealToAttacker = snap.PendingPostResolveHealToAttacker;
            state.PendingPostResolveMoraleToAttacker = snap.PendingPostResolveMoraleToAttacker;
            state.PendingCombatNote = snap.PendingCombatNote;
            state.TotalPlayPhasesThisTurn = snap.TotalPlayPhasesThisTurn;
            state.PendingExtraPlayPhasesToGrant = snap.PendingExtraPlayPhasesToGrant;
            state.PendingAttackPatternVariant = snap.PendingAttackPatternVariant;
            state.PendingAttackSkillKind = snap.PendingAttackSkillKind;
            state.PendingGenericAttackOptionIndex = snap.PendingGenericAttackOptionIndex;
            state.PendingGenericAttackShapeChoicePending = snap.PendingGenericAttackShapeChoicePending;
            state.PendingGenericAttackShapeDisplayName = snap.PendingGenericAttackShapeDisplayName;
            state.PendingDamageCategory = snap.PendingDamageCategory;
            state.PendingDamageElement = snap.PendingDamageElement;
            state.HuBuGuanYouWindowConsumedForCurrentAttack = snap.HuBuGuanYouWindowConsumedForCurrentAttack;
        }

        /// <summary>数值越大越好：优先总伤，其次额外阶段、摸牌、恢复等。</summary>
        public static long ScoreAttackChoice(BattleState state, bool attackerIsPlayer, int generalIndex, int skillIndex, List<PokerCard> played)
        {
            if (state == null || played == null || played.Count == 0)
                return long.MinValue;

            bool prevSuppress = SuppressSkillBanners;
            SuppressSkillBanners = true;
            Snapshot snap = Capture(state);

            if (generalIndex < 0)
            {
                var opts = GenericAttackShapes.BuildSortedOptions(played);
                long score = long.MinValue;
                int tp0 = snap.TotalPlayPhasesThisTurn + snap.PendingExtraPlayPhasesToGrant;
                for (int oi = 0; oi < opts.Count; oi++)
                {
                    Restore(state, snap);
                    state.ActiveSide.PlayedThisPhase.Clear();
                    for (int i = 0; i < played.Count; i++)
                        state.ActiveSide.PlayedThisPhase.Add(played[i]);
                    state.PendingAttackPatternVariant = -1;
                    state.PendingAttackSkillKind = SelectedSkillKind.GenericAttack;
                    state.PendingGenericAttackOptionIndex = oi;
                    state.PendingGenericAttackShapeChoicePending = false;
                    OfflineSkillEngine.ConfigureAttackSkill(state, attackerIsPlayer, -1, -1);
                    int tp1 = state.TotalPlayPhasesThisTurn + state.PendingExtraPlayPhasesToGrant;
                    int totalHit = state.PendingBaseDamage + state.PendingAttackBonus;
                    long sc = (long)totalHit * 10000L
                        + (tp1 - tp0) * 800L
                        + state.PendingPostResolveDrawToAttacker * 250L
                        + state.PendingPostResolveHealToAttacker * 180L
                        + state.PendingPostResolveMoraleToAttacker * 150L
                        + (state.PendingIgnoreDefenseReduction ? 600L : 0L);
                    if (sc > score)
                        score = sc;
                }

                Restore(state, snap);
                SuppressSkillBanners = prevSuppress;
                return score;
            }

            state.ActiveSide.PlayedThisPhase.Clear();
            for (int i = 0; i < played.Count; i++)
                state.ActiveSide.PlayedThisPhase.Add(played[i]);

            state.PendingAttackPatternVariant = -1;
            SideState atkSide2 = state.GetSide(attackerIsPlayer);
            if (generalIndex < atkSide2.GeneralCardIds.Count)
            {
                string cid2 = atkSide2.GeneralCardIds[generalIndex] ?? string.Empty;
                if (string.Equals(SkillRuleHelper.MakeSkillKey(cid2, skillIndex), "NO002_0", StringComparison.Ordinal))
                    OfflineSkillEngine.AutoPickCeMaPatternVariant(state, state.ActiveSide.PlayedThisPhase);
            }

            int tp0g = snap.TotalPlayPhasesThisTurn + snap.PendingExtraPlayPhasesToGrant;
            state.PendingAttackSkillKind = SelectedSkillKind.GeneralSkill;
            OfflineSkillEngine.ConfigureAttackSkill(state, attackerIsPlayer, generalIndex, skillIndex);
            int tp1g = state.TotalPlayPhasesThisTurn + state.PendingExtraPlayPhasesToGrant;
            int totalHitG = state.PendingBaseDamage + state.PendingAttackBonus;
            long scoreG = (long)totalHitG * 10000L
                + (tp1g - tp0g) * 800L
                + state.PendingPostResolveDrawToAttacker * 250L
                + state.PendingPostResolveHealToAttacker * 180L
                + state.PendingPostResolveMoraleToAttacker * 150L
                + (state.PendingIgnoreDefenseReduction ? 600L : 0L);

            Restore(state, snap);
            SuppressSkillBanners = prevSuppress;
            return scoreG;
        }

        private static List<List<int>> Combine(int n, int k)
        {
            var result = new List<List<int>>();
            var cur = new List<int>(k);
            void Dfs(int start)
            {
                if (cur.Count == k)
                {
                    result.Add(new List<int>(cur));
                    return;
                }

                for (int i = start; i < n; i++)
                {
                    cur.Add(i);
                    Dfs(i + 1);
                    cur.RemoveAt(cur.Count - 1);
                }
            }

            Dfs(0);
            return result;
        }

        /// <summary>在 <paramref name="attackerSide"/> 的手牌中枚举 1..max 张的所有组合，对每种组合与每种攻击选项打分，选出最优。</summary>
        public static void FindBestPlayAndAttack(
            BattleState state,
            SideState attackerSide,
            bool attackerIsPlayer,
            IReadOnlyList<(int generalIndex, int skillIndex, string skillName)> attackOptions,
            out List<int> chosenHandIndicesAscending,
            out int chosenGeneral,
            out int chosenSkill,
            out string chosenSkillName)
        {
            chosenHandIndicesAscending = new List<int> { Mathf.Max(0, attackerSide.Hand.Count - 1) };
            chosenGeneral = -1;
            chosenSkill = -1;
            chosenSkillName = "\u901a\u7528\u653b\u51fb";

            int n = attackerSide.Hand.Count;
            if (n <= 0)
                return;

            int maxK = Mathf.Min(BattleState.MaxPlayPerPhase, n);
            long bestScore = long.MinValue;
            int bestK = 999;

            // k 从小到大：同分时保留张数更少的组合，避免无必要出满 5 张。
            for (int k = 1; k <= maxK; k++)
            {
                foreach (List<int> comb in Combine(n, k))
                {
                    var played = new List<PokerCard>(k);
                    for (int i = 0; i < comb.Count; i++)
                        played.Add(attackerSide.Hand[comb[i]]);

                    for (int o = 0; o < attackOptions.Count; o++)
                    {
                        (int g, int s, string nm) = attackOptions[o];
                        long sc = ScoreAttackChoice(state, attackerIsPlayer, g, s, played);
                        if (sc > bestScore || (sc == bestScore && k < bestK))
                        {
                            bestScore = sc;
                            bestK = k;
                            chosenHandIndicesAscending = new List<int>(comb);
                            chosenGeneral = g;
                            chosenSkill = s;
                            chosenSkillName = nm;
                        }
                    }
                }
            }
        }
    }
}
