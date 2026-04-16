using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 指示物条数据与布局参数（自右下向左上、每行至多 4 个）；具体 UI 在 <see cref="GameUI"/> 中构建。
    /// </summary>
    public static class BattleIndicatorStrip
    {
        public const int MaxCellsPerRow = 4;
        public const float CellHeight = 28f;
        public const float CellPadX = 4f;
        public const float CellPadY = 2f;

        public static string DisplayNameForEffectKey(string effectKey)
        {
            if (string.IsNullOrEmpty(effectKey))
                return string.Empty;
            if (string.Equals(effectKey, OfflineSkillEngine.ResistEffectKey, StringComparison.Ordinal))
                return "\u62b5\u5fa1";
            return effectKey;
        }

        /// <summary>intro.xlsx A 列 id：与图鉴 tag 一致时显示介绍；未知 key 则用显示名或原 key。</summary>
        public static string IntroLookupIdForEffectKey(string effectKey)
        {
            if (string.IsNullOrEmpty(effectKey))
                return string.Empty;
            if (string.Equals(effectKey, OfflineSkillEngine.ResistEffectKey, StringComparison.Ordinal))
                return "\u62b5\u5fa1";
            return effectKey;
        }

        public static List<(string key, int count)> ListIndicatorEntries(SideState side)
        {
            var list = new List<(string key, int count)>();
            if (side?.EffectLayers == null)
                return list;
            foreach (var kv in side.EffectLayers)
            {
                if (kv.Value <= 0)
                    continue;
                list.Add((kv.Key, kv.Value));
            }

            list.Sort((a, b) =>
            {
                bool ar = string.Equals(a.key, OfflineSkillEngine.ResistEffectKey, StringComparison.Ordinal);
                bool br = string.Equals(b.key, OfflineSkillEngine.ResistEffectKey, StringComparison.Ordinal);
                if (ar != br)
                    return ar ? -1 : 1;
                return string.Compare(DisplayNameForEffectKey(a.key), DisplayNameForEffectKey(b.key), StringComparison.Ordinal);
            });
            return list;
        }

        public static int ComputeStripHeight(int entryCount)
        {
            if (entryCount <= 0)
                return 0;
            int rows = (entryCount + MaxCellsPerRow - 1) / MaxCellsPerRow;
            return Mathf.RoundToInt(rows * (CellHeight + CellPadY) + 4f);
        }
    }
}
