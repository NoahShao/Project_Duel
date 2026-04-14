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
            /// <summary>为 true 时在本行前增加额外上边距（用于区分回合）。</summary>
            public bool ExtraTopMargin;
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

            _entries.Add(new Entry { Line = line.Trim(), ExtraTopMargin = false });
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            Changed?.Invoke();
        }

        /// <summary>新回合开始时的标记行，与上一回合内容拉开间距。</summary>
        public static void AddRoundBeginMarker(int roundNumber)
        {
            if (roundNumber < 1)
                return;

            _entries.Add(new Entry { Line = string.Empty, ExtraTopMargin = true });
            _entries.Add(new Entry { Line = "\u3010\u56de\u5408" + roundNumber + "\u3011", ExtraTopMargin = false });
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            Changed?.Invoke();
        }
    }
}
