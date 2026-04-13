using System;
using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 离线战报流水：每条为完整一行文案（已含【全局】/【己方回合】/【敌方回合】等前缀）。
    /// 阶段推进用的战报在 <see cref="BattleFlowPacing"/> 中可按【全局】/「无事发生」插入 1s/0.5s 停顿后再继续。
    /// </summary>
    public static class BattleFlowLog
    {
        public const int MaxEntries = 400;

        public struct Entry
        {
            public string Line;
        }

        private static readonly List<Entry> _entries = new List<Entry>(64);

        public static IReadOnlyList<Entry> Entries => _entries;

        public static event Action Changed;

        public static void Clear()
        {
            _entries.Clear();
            Changed?.Invoke();
        }

        public static void Add(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            _entries.Add(new Entry { Line = line.Trim() });
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            Changed?.Invoke();
        }
    }
}
