using System;

namespace JunzhenDuijue
{
    /// <summary>攻击伤害大类（与生命值失去、失去体力等区分）。</summary>
    public enum DamageCategory : byte
    {
        /// <summary>未指定；攻击结算前若仍为 None 则按 <see cref="DamageCategory.Generic"/> 处理。</summary>
        None = 0,
        /// <summary>通用伤害</summary>
        Generic = 1,
        /// <summary>兵刃伤害</summary>
        Blade = 2,
        /// <summary>属性伤害（须配合 <see cref="DamageElement"/>）</summary>
        Attribute = 3,
        /// <summary>谋略伤害</summary>
        Stratagem = 4,
    }

    /// <summary>属性伤害的元素细分。</summary>
    public enum DamageElement : byte
    {
        None = 0,
        Fire = 1,
        Lightning = 2,
        Poison = 3,
        Flood = 4,
    }

    /// <summary>战报、Toast 等与伤害类型对应的固定中文（勿在业务里拼「基础伤害」等表外用语）。</summary>
    public static class DamageTypeLabels
    {
        public static string CategoryName(DamageCategory c)
        {
            return c switch
            {
                DamageCategory.Generic => "\u901a\u7528\u4f24\u5bb3",
                DamageCategory.Blade => "\u5175\u5203\u4f24\u5bb3",
                DamageCategory.Attribute => "\u5c5e\u6027\u4f24\u5bb3",
                DamageCategory.Stratagem => "\u8c0b\u7565\u4f24\u5bb3",
                _ => "\u901a\u7528\u4f24\u5bb3",
            };
        }

        /// <summary>用于「造成3点火焰伤害」式文案：属性类返回元素名+伤害，否则返回大类名。</summary>
        public static string DamageTypeNameForAmountLine(DamageCategory category, DamageElement element)
        {
            if (category == DamageCategory.Attribute && element != DamageElement.None)
            {
                return element switch
                {
                    DamageElement.Fire => "\u706b\u7130\u4f24\u5bb3",
                    DamageElement.Lightning => "\u96f7\u7535\u4f24\u5bb3",
                    DamageElement.Poison => "\u6bd2\u6027\u4f24\u5bb3",
                    DamageElement.Flood => "\u6c34\u6df9\u4f24\u5bb3",
                    _ => CategoryName(DamageCategory.Attribute),
                };
            }

            DamageCategory c = category == DamageCategory.None ? DamageCategory.Generic : category;
            return CategoryName(c);
        }

        /// <summary>结算摘要：造成最终点数 + 类型名（已含「伤害」后缀）。</summary>
        public static string FormatResolvedDamageLine(int finalAmount, DamageCategory category, DamageElement element)
        {
            string type = DamageTypeNameForAmountLine(category, element);
            return "\u9020\u6210" + finalAmount + "\u70b9" + type;
        }

        /// <summary>攻击伤害预告横幅用：「结算时将造成」+ N 点 + 类型（如 3 点兵刃伤害），避免「3 点伤害，兵刃伤害」重复表述。</summary>
        public static string FormatDeclarePendingDamageClause(int amount, DamageCategory category, DamageElement element)
        {
            DamageCategory c = category == DamageCategory.None ? DamageCategory.Generic : category;
            int a = amount < 0 ? 0 : amount;
            return "\u7ed3\u7b97\u65f6\u5c06\u9020\u6210" + a + "\u70b9" + DamageTypeNameForAmountLine(c, element);
        }
    }
}
