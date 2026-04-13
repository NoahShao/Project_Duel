using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 从 .xlsx 表格读取的卡牌一行数据。
    /// 表格列：id、角色名称、势力、花色、点数、技能名称一、技能一tag、技能描述一、技能名称二、技能二tag、技能描述二、技能名称三、技能三tag、技能描述三、
    /// 角色tag、是否有特殊形态、特殊形态id 等。
    /// </summary>
    [System.Serializable]
    public class CardData
    {
        public int Id;
        public string RoleName = "";           // 角色名称
        public string Faction = "";             // 势力
        public string Suit = "";                // 花色
        public string Rank = "";                // 点数
        public string ExpansionPack = "";       // 所属扩展/所属扩展包
        public string SkillName1 = "";
        public string SkillDesc1 = "";
        public List<string> SkillTags1 = new List<string>();  // 技能一tag，多个用 | 分隔，如 强制技|持续技
        public string SkillName2 = "";
        public string SkillDesc2 = "";
        public List<string> SkillTags2 = new List<string>();
        public string SkillName3 = "";
        public string SkillDesc3 = "";
        public List<string> SkillTags3 = new List<string>();

        /// <summary> 特殊形态时的技能：T/W/Z 列 tag，U/X/AA 列描述 </summary>
        public string SpecialSkillName1 = "";
        public List<string> SpecialSkillTags1 = new List<string>();
        public string SpecialSkillDesc1 = "";
        public string SpecialSkillName2 = "";
        public List<string> SpecialSkillTags2 = new List<string>();
        public string SpecialSkillDesc2 = "";
        public string SpecialSkillName3 = "";
        public List<string> SpecialSkillTags3 = new List<string>();
        public string SpecialSkillDesc3 = "";

        /// <summary> 角色 tag 唯一，仅填 sp / ex / 霸业 等，不填强制技|持续技；不填视为无 </summary>
        public string RoleTag = "";
        /// <summary> 是否有特殊形态；为 true 时点击卡牌在右侧展示特殊形态 </summary>
        public bool HasSpecialForm;
        /// <summary> 特殊形态卡牌 id，对应表格中的 id；0 表示未填 </summary>
        public int SpecialFormId;

        /// <summary> 卡牌展示用 ID，如 NO001 </summary>
        public string CardId => "NO" + Id.ToString("D3");

        /// <summary> 该角色具有的技能数量（1～3，按技能名称是否填写）。 </summary>
        public int SkillCount =>
            (string.IsNullOrWhiteSpace(SkillName1) ? 0 : 1) +
            (string.IsNullOrWhiteSpace(SkillName2) ? 0 : 1) +
            (string.IsNullOrWhiteSpace(SkillName3) ? 0 : 1);
    }
}
