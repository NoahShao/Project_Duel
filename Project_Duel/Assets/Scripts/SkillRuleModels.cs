using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// ?????????
    /// ????????????????????????????????
    /// </summary>
    [Serializable]
    public class SkillRuleEntry
    {
        public string SkillKey = string.Empty;
        public string CardId = string.Empty;
        public int SkillIndex;
        public string SkillName = string.Empty;
        public List<string> Tags = new List<string>();
        public string TriggerHint = string.Empty;
        public bool AllowOnOpponentTurn;
        public string EffectId = string.Empty;
        public int Value1;
        public int Value2;
        public string StringValue1 = string.Empty;
    }

    /// <summary>
    /// ??????????
    /// </summary>
    [Serializable]
    public class SkillRuleTableBinary
    {
        public List<SkillRuleEntry> Entries = new List<SkillRuleEntry>();
    }

    /// <summary>
    /// ??????????????????????????????????
    /// ????????????????????
    /// </summary>
    public static class SkillRuleHelper
    {
        public static string MakeSkillKey(string cardId, int skillIndex)
        {
            return (cardId ?? string.Empty) + "_" + skillIndex;
        }

        /// <summary>解析 <see cref="MakeSkillKey"/> 生成的键，如 NO002_0。</summary>
        public static bool TryParseSkillKey(string skillKey, out string cardId, out int skillIndex)
        {
            cardId = string.Empty;
            skillIndex = 0;
            if (string.IsNullOrEmpty(skillKey))
                return false;

            int u = skillKey.LastIndexOf('_');
            if (u <= 0 || u >= skillKey.Length - 1)
                return false;

            cardId = skillKey.Substring(0, u);
            return int.TryParse(skillKey.Substring(u + 1), out skillIndex);
        }

        public static string GuessTriggerHint(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "Manual";
            if (tags.Contains("攻击技"))
                return "PlayPhaseAttack";
            if (tags.Contains("防御技"))
                return "DefensePhase";
            if (tags.Contains("主动技") || tags.Contains("破军技"))
                return "PrimaryPhase";
            if (tags.Contains("强制技") || tags.Contains("持续技"))
                return "TriggeredPassive";
            return "Manual";
        }

        public static bool GuessAllowOnOpponentTurn(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return false;
            return tags.Contains("防御技") || tags.Contains("持续技") || tags.Contains("强制技");
        }

        public static string GuessEffectId(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "manual_placeholder";
            if (tags.Contains("攻击技"))
                return "attack_from_played_cards";
            if (tags.Contains("防御技"))
                return "reduce_damage_flat_1";
            if (tags.Contains("抵御"))
                return "grant_resist_layer";
            if (tags.Contains("主动技") || tags.Contains("破军技"))
                return "manual_primary_effect";
            if (tags.Contains("强制技") || tags.Contains("持续技"))
                return "triggered_passive_effect";
            return "manual_placeholder";
        }
    }

    /// <summary>
    /// ???????????
    /// ??????? Resources/Config/SkillRules.bytes ???????????
    /// </summary>
    public static class SkillRuleLoader
    {
        private static Dictionary<string, SkillRuleEntry> _byKey;

        public static bool Load()
        {
            _byKey = new Dictionary<string, SkillRuleEntry>();
            var textAsset = Resources.Load<TextAsset>(CompiledConfigNames.SkillRulesResourcePath);
            if (textAsset == null || textAsset.bytes == null || textAsset.bytes.Length == 0)
                return false;

            try
            {
                string json = System.Text.Encoding.UTF8.GetString(textAsset.bytes);
                var table = JsonUtility.FromJson<SkillRuleTableBinary>(json);
                if (table == null || table.Entries == null)
                    return false;
                for (int i = 0; i < table.Entries.Count; i++)
                {
                    var entry = table.Entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.SkillKey))
                        continue;
                    _byKey[entry.SkillKey] = entry;
                }
                return _byKey.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SkillRuleLoader] Failed to load skill rules: " + e.Message);
                _byKey = new Dictionary<string, SkillRuleEntry>();
                return false;
            }
        }

        public static SkillRuleEntry GetRule(string cardId, int skillIndex)
        {
            if (_byKey == null)
                Load();
            if (_byKey == null)
                return null;
            _byKey.TryGetValue(SkillRuleHelper.MakeSkillKey(cardId, skillIndex), out var rule);
            return rule;
        }

        public static bool HasTag(string cardId, int skillIndex, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;
            var rule = GetRule(cardId, skillIndex);
            if (rule == null || rule.Tags == null)
                return false;
            for (int i = 0; i < rule.Tags.Count; i++)
            {
                if (string.Equals(rule.Tags[i], tag, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static IReadOnlyList<string> GetTags(string cardId, int skillIndex)
        {
            var rule = GetRule(cardId, skillIndex);
            return rule != null && rule.Tags != null ? rule.Tags : Array.Empty<string>();
        }
    }
}
