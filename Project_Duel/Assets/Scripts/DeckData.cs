using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 单个牌组数据：移除的花色 + 从图鉴拖入的卡牌（最多3张）。
    /// </summary>
    [System.Serializable]
    public class DeckData
    {
        public string Id;
        public string DisplayName;
        /// <summary> 从牌组中移除的花色，如 "红桃"；null 表示未选 </summary>
        public string RemovedSuit;
        /// <summary> 从图鉴拖入的卡牌 ID，最多 3 张 </summary>
        public List<string> CardIds = new List<string>();

        public DeckData()
        {
            Id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            DisplayName = "牌组";
        }
    }

    /// <summary> 用于 JsonUtility 序列化牌组列表 </summary>
    [System.Serializable]
    public class DeckListSave
    {
        public DeckData[] Decks;
    }
}
