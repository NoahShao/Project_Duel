using System;

namespace JunzhenDuijue
{
    /// <summary>
    /// 技能效果横幅：模板「己方/敌方玩家使用/触发角色【XX】的技能【XX】，结果」。
    /// </summary>
    public static class SkillEffectBanner
    {
        public const float DefaultDuration = 3f;

        public static void Show(bool sideIsPlayer, bool useActiveVerb, string roleDisplayName, string skillDisplayName, string outcome, Action onComplete = null)
        {
            if (BattleAttackPreview.SuppressSkillBanners)
            {
                onComplete?.Invoke();
                return;
            }

            string camp = sideIsPlayer ? "\u5df1\u65b9" : "\u654c\u65b9";
            string verb = useActiveVerb ? "\u4f7f\u7528" : "\u89e6\u53d1";
            string role = string.IsNullOrWhiteSpace(roleDisplayName) ? "\u6b66\u5c06" : roleDisplayName.Trim();
            string sk = string.IsNullOrWhiteSpace(skillDisplayName) ? "\u6280\u80fd" : skillDisplayName.Trim();
            string oc = outcome ?? string.Empty;
            string line = camp + "\u73a9\u5bb6" + verb + "\u89d2\u8272\u3010" + role + "\u3011\u7684\u6280\u80fd\u3010" + sk + "\u3011\uff0c" + oc;
            ToastUI.Show(line, DefaultDuration, pauseGameWhileVisible: true, onComplete);
        }

        /// <summary>整句技能战报横幅（如【据水断桥】长文案），走 Toast 自适应换行与面板尺寸。</summary>
        public static void ShowRawLine(string fullLine, Action onComplete = null)
        {
            if (BattleAttackPreview.SuppressSkillBanners)
            {
                onComplete?.Invoke();
                return;
            }

            ToastUI.Show(fullLine ?? string.Empty, DefaultDuration, pauseGameWhileVisible: true, onComplete);
        }

        public static string GetRoleNameFromCardId(string cardId)
        {
            var card = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cardId));
            return card != null && !string.IsNullOrWhiteSpace(card.RoleName) ? card.RoleName.Trim() : string.Empty;
        }
    }
}
