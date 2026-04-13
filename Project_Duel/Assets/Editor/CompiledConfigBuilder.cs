using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JunzhenDuijue.Editor
{
    public static class CompiledConfigBuilder
    {
        private const string CardsXlsxAssetPath = "Assets/StreamingAssets/Cards.xlsx";
        private const string IntroXlsxAssetPath = "Assets/StreamingAssets/Intro.xlsx";
        private const string SkillRulesSourceAssetPath = "Assets/StreamingAssets/SkillRules.json";
        private const string ResourcesConfigDirectory = "Assets/Resources/Config";

        [MenuItem("Tools/Project Duel/Rebuild Compiled Configs")]
        public static void RebuildAll()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Config"));
            BuildCardsConfig();
            BuildIntroConfig();
            BuildSkillRulesConfig();
            AssetDatabase.Refresh();
            Debug.Log("[CompiledConfigBuilder] Rebuilt compiled configs.");
        }

        public static void BuildCardsConfig()
        {
            if (!TryReadCardsFromXlsx(out List<CardData> cards, out string error))
            {
                Debug.LogError("[CompiledConfigBuilder] " + error);
                return;
            }

            int maxId = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].Id > maxId)
                    maxId = cards[i].Id;
            }

            var table = new CardTableBinary
            {
                MaxId = maxId,
                Cards = cards
            };
            WriteJsonBytes(Path.Combine(ResourcesConfigDirectory, CompiledConfigNames.CardsBinaryFileName), table);
        }

        public static void BuildIntroConfig()
        {
            string assetPath = IntroXlsxAssetPath;
            if (!File.Exists(GetAbsolutePath(assetPath)))
            {
                Debug.LogError("[CompiledConfigBuilder] Missing xlsx: " + assetPath);
                return;
            }

            byte[] bytes = File.ReadAllBytes(GetAbsolutePath(assetPath));
            var entries = new List<IntroConfigEntry>();
            if (!IntroXlsxParser.Parse(bytes, entries))
            {
                Debug.LogError("[CompiledConfigBuilder] Failed to parse Intro.xlsx");
                return;
            }

            var table = new IntroTableBinary
            {
                Entries = entries
            };
            WriteJsonBytes(Path.Combine(ResourcesConfigDirectory, CompiledConfigNames.IntroBinaryFileName), table);
        }

        public static void BuildSkillRulesConfig()
        {
            SkillRuleTableBinary table;
            string sourceAbsolutePath = GetAbsolutePath(SkillRulesSourceAssetPath);
            if (!File.Exists(sourceAbsolutePath))
            {
                if (!TryBuildSkillRuleTemplate(out table, out string error))
                {
                    Debug.LogError("[CompiledConfigBuilder] " + error);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(sourceAbsolutePath) ?? string.Empty);
                File.WriteAllText(sourceAbsolutePath, JsonUtility.ToJson(table, true), System.Text.Encoding.UTF8);
                AssetDatabase.ImportAsset(SkillRulesSourceAssetPath, ImportAssetOptions.ForceUpdate);
                Debug.LogWarning("[CompiledConfigBuilder] SkillRules.json missing, template created at " + SkillRulesSourceAssetPath);
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(sourceAbsolutePath, System.Text.Encoding.UTF8);
                    table = JsonUtility.FromJson<SkillRuleTableBinary>(json);
                    if (table == null)
                        throw new IOException("SkillRules.json is empty or invalid JSON.");
                    if (table.Entries == null)
                        table.Entries = new List<SkillRuleEntry>();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[CompiledConfigBuilder] Failed to read SkillRules.json: " + e.Message);
                    return;
                }
            }

            WriteJsonBytes(Path.Combine(ResourcesConfigDirectory, CompiledConfigNames.SkillRulesBinaryFileName), table);
        }

        private static bool TryReadCardsFromXlsx(out List<CardData> cards, out string error)
        {
            cards = new List<CardData>();
            error = string.Empty;
            string assetPath = CardsXlsxAssetPath;
            if (!File.Exists(GetAbsolutePath(assetPath)))
            {
                error = "Missing xlsx: " + assetPath;
                return false;
            }

            byte[] bytes = File.ReadAllBytes(GetAbsolutePath(assetPath));
            if (!CardTableLoader.ValidateTableLayout(bytes, out string validateError))
            {
                error = "Cards.xlsx invalid: " + validateError;
                return false;
            }

            if (!XlsxParser.Parse(bytes, cards))
            {
                error = "Failed to parse Cards.xlsx";
                return false;
            }

            return true;
        }

        private static bool TryBuildSkillRuleTemplate(out SkillRuleTableBinary table, out string error)
        {
            table = new SkillRuleTableBinary();
            error = string.Empty;
            if (!TryReadCardsFromXlsx(out List<CardData> cards, out error))
                return false;

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;
                AppendSkill(table.Entries, card, 0, card.SkillName1, card.SkillTags1);
                AppendSkill(table.Entries, card, 1, card.SkillName2, card.SkillTags2);
                AppendSkill(table.Entries, card, 2, card.SkillName3, card.SkillTags3);
            }

            return true;
        }

        private static void AppendSkill(List<SkillRuleEntry> entries, CardData card, int skillIndex, string skillName, List<string> tags)
        {
            if (entries == null || card == null || string.IsNullOrWhiteSpace(skillName))
                return;

            var safeTags = tags != null ? new List<string>(tags) : new List<string>();
            entries.Add(new SkillRuleEntry
            {
                SkillKey = SkillRuleHelper.MakeSkillKey(card.CardId, skillIndex),
                CardId = card.CardId,
                SkillIndex = skillIndex,
                SkillName = skillName,
                Tags = safeTags,
                TriggerHint = SkillRuleHelper.GuessTriggerHint(safeTags),
                AllowOnOpponentTurn = SkillRuleHelper.GuessAllowOnOpponentTurn(safeTags),
                EffectId = SkillRuleHelper.GuessEffectId(safeTags),
                Value1 = 0,
                Value2 = 0,
                StringValue1 = string.Empty,
            });
        }

        private static void WriteJsonBytes<T>(string assetPath, T data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GetAbsolutePath(assetPath)) ?? string.Empty);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllBytes(GetAbsolutePath(assetPath), System.Text.Encoding.UTF8.GetBytes(json));
        }

        private static string GetAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    public class CompiledConfigAssetPostprocessor : AssetPostprocessor
    {
        private static bool _isBuilding;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (_isBuilding)
                return;

            bool shouldRebuild = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i].Replace('\\', '/');
                if (path == CompiledConfigBuilderReflection.CardsXlsx ||
                    path == CompiledConfigBuilderReflection.IntroXlsx ||
                    path == CompiledConfigBuilderReflection.SkillRulesJson)
                {
                    shouldRebuild = true;
                    break;
                }
            }

            if (!shouldRebuild)
                return;

            _isBuilding = true;
            try
            {
                CompiledConfigBuilder.RebuildAll();
            }
            finally
            {
                _isBuilding = false;
            }
        }
    }

    internal static class CompiledConfigBuilderReflection
    {
        public const string CardsXlsx = "Assets/StreamingAssets/Cards.xlsx";
        public const string IntroXlsx = "Assets/StreamingAssets/Intro.xlsx";
        public const string SkillRulesJson = "Assets/StreamingAssets/SkillRules.json";
    }
}
