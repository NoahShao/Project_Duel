using System;
using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// ???????????
    /// ????????????????????? bytes ?????????
    /// </summary>
    [Serializable]
    public class CardTableBinary
    {
        public int MaxId;
        public List<CardData> Cards = new List<CardData>();
    }

    /// <summary>
    /// ????/???????
    /// </summary>
    [Serializable]
    public class IntroConfigEntry
    {
        public string Id = string.Empty;
        public string Content = string.Empty;
    }

    /// <summary>
    /// ?????????????
    /// </summary>
    [Serializable]
    public class IntroTableBinary
    {
        public List<IntroConfigEntry> Entries = new List<IntroConfigEntry>();
    }

    /// <summary>
    /// ???????????????
    /// ?????????????????????????????
    /// </summary>
    public static class CompiledConfigNames
    {
        public const string CardsResourcePath = "Config/Cards";
        public const string IntroResourcePath = "Config/Intro";
        public const string SkillRulesResourcePath = "Config/SkillRules";
        public const string CardsBinaryFileName = "Cards.bytes";
        public const string IntroBinaryFileName = "Intro.bytes";
        public const string SkillRulesBinaryFileName = "SkillRules.bytes";
    }
}
