using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 图鉴要展示的卡牌 ID 列表。在 Inspector 里添加 NO001、NO002 等，图鉴只显示列表中的卡牌。
    /// </summary>
    [CreateAssetMenu(fileName = "CompendiumConfig", menuName = "军阵对决/图鉴配置", order = 0)]
    public class CompendiumConfig : ScriptableObject
    {
        [Tooltip("要在图鉴中展示的卡牌 ID 列表，如 NO001、NO002...")]
        public List<string> CardIds = new List<string>();
    }
}
