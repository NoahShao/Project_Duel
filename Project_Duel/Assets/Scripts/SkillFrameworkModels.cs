using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 数据驱动技能框架：用 JSON（Unity JsonUtility）描述「时机 + 效果链 + 攻击牌型表」，
    /// 由 <see cref="SkillFrameworkExecutor"/> 解释执行。与现有 <see cref="SkillRuleEntry"/> 并存，可按 SkillKey 逐步迁移。
    /// </summary>
    public enum SkillFrameworkEventKind
    {
        None = 0,
        /// <summary>对局开始（双方抽牌后、进入准备阶段前等，由调用方决定精确注入点）</summary>
        GameStart = 1,
        /// <summary>己方回合 · 准备阶段开始</summary>
        PreparationStart = 2,
        /// <summary>己方回合 · 收入阶段结束</summary>
        IncomeEnd = 3,
        /// <summary>己方回合 · 弃牌阶段开始</summary>
        DiscardStart = 4,
        /// <summary>己方回合 · 弃牌阶段结束</summary>
        DiscardEnd = 5,
        /// <summary>己方回合 · 回合结束</summary>
        TurnEnd = 6,
        /// <summary>消耗士气后（需在耗士气代码处手动 Notify）</summary>
        AfterMoraleSpent = 7,
        /// <summary>主将技窗口内玩家点击发动（与 OfflineSkillEngine.TryActivatePrimarySkill 对齐）</summary>
        ManualPrimary = 8,
    }

    public enum SkillFrameworkEffectOp
    {
        None = 0,
        GainMorale = 1,
        /// <summary>直接提高当前士气上限（持续类可配合 Refresh 再算，当前与 Offline 逻辑一致为即时改 SideState.MoraleCap）</summary>
        AddMoraleCap = 2,
        HealSelf = 3,
        DrawSelf = 4,
        AddEffectLayerSelf = 5,
        RemoveEffectLayersAnySelf = 6,
        AddExtraPlayPhases = 7,
        SetPendingBaseDamage = 8,
        AddPendingBonusDamage = 9,
        SetIgnoreDefense = 10,
        AddPostResolveDraw = 11,
        AddPostResolveHeal = 12,
        AddPostResolveMorale = 13,
        AppendCombatNote = 14,
        /// <summary>记入 TriggeredSkillKeysThisTurn，配合 LimitPerTurn</summary>
        MarkTriggeredThisTurn = 15,
    }

    public enum AttackPatternKind
    {
        None = 0,
        SingleCard = 1,
        Pair = 2,
        TwoPair = 3,
        Triple = 4,
        Straight = 5,
        StraightFlush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
    }

    [Serializable]
    public class SkillEffectStep
    {
        public SkillFrameworkEffectOp Op;
        public int I0;
        public int I1;
        public int I2;
        [Tooltip("效果层 Key、备注文案等")]
        public string S0 = string.Empty;
    }

    [Serializable]
    public class SkillTriggerBlock
    {
        public SkillFrameworkEventKind Event = SkillFrameworkEventKind.None;
        /// <summary>0 表示本块不限制；1 表示每回合至多触发一次（配合 MarkTriggeredThisTurn）</summary>
        public int LimitPerTurn;
        public List<SkillEffectStep> Steps = new List<SkillEffectStep>();
    }

    [Serializable]
    public class AttackPatternRow
    {
        public AttackPatternKind Kind = AttackPatternKind.None;
        /// <summary>顺子 / 同花顺最短长度，默认 3</summary>
        public int MinStraightLength = 3;
        public bool RequireAllRed;
        public bool RequireAllBlack;
        /// <summary>顺子类：为 true 时不同花顺匹配（同花顺请用 <see cref="AttackPatternKind.StraightFlush"/> 单独一行）</summary>
        public bool RequireNotFlush;
        /// <summary>仅 SingleCard：有效点数 &gt; 该值时匹配（如 7 表示 8 点及以上）</summary>
        public int MinEffectiveRankExclusive;
        public int BaseDamage;
        public bool Unblockable;
        public int ExtraPlayPhases;
        public int PostDraw;
        public int PostHeal;
        public int PostMorale;
        [Tooltip("匹配成功后写入 PendingCombatNote")]
        public string Note = string.Empty;
        [Tooltip("\u653b\u51fb\u4f24\u5bb3\u7c7b\u578b\uff1bNone \u6309\u901a\u7528\u4f24\u5bb3")]
        public DamageCategory DamageCategory;
        [Tooltip("\u4ec5\u5f53 DamageCategory \u4e3a\u5c5e\u6027\u4f24\u5bb3\u65f6\u6709\u6548")]
        public DamageElement DamageElement;
    }

    [Serializable]
    public class SkillDefinition
    {
        public string SkillKey = string.Empty;
        public string DisplayName = string.Empty;
        public List<SkillTriggerBlock> Triggers = new List<SkillTriggerBlock>();
        public List<AttackPatternRow> AttackPatterns = new List<AttackPatternRow>();
    }

    /// <remarks>
    /// <para>示例（与当前代码中关羽 NO002_0 等价，可粘贴进 SkillFramework.json 的 Definitions）：</para>
    /// <code>
    /// {
    ///   "SkillKey": "NO002_0",
    ///   "DisplayName": "策马斩将",
    ///   "AttackPatterns": [
    ///     { "Kind": 6, "MinStraightLength": 3, "RequireAllRed": true, "BaseDamage": 7, "Unblockable": true, "ExtraPlayPhases": 1, "PostDraw": 3, "Note": "【策马斩将】红色同花顺" },
    ///     { "Kind": 5, "MinStraightLength": 3, "RequireAllRed": true, "RequireNotFlush": true, "BaseDamage": 6, "ExtraPlayPhases": 1, "Note": "【策马斩将】红色顺子" },
    ///     { "Kind": 1, "RequireAllRed": true, "BaseDamage": 3, "Note": "【策马斩将】红色单牌" }
    ///   ]
    /// }
    /// </code>
    /// <para>Kind 对应 <see cref="AttackPatternKind"/> 整数值。若某 SkillKey 出现在此表中，攻击结算将优先走框架，不再执行 OfflineSkillEngine 内同名 case。</para>
    /// </remarks>
    [Serializable]
    public class SkillFrameworkTable
    {
        public List<SkillDefinition> Definitions = new List<SkillDefinition>();
    }
}

