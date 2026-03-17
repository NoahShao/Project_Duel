using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 挂在技能描述 Text 上：悬浮 0.5s 后按 intro.xlsx 的 A列 id 对应 B列介绍在弹窗中展示；弹窗跟随鼠标；鼠标移出描述则隐藏弹窗。
    /// 当该技能有多个 tag（如 Cards.xlsx 的 H/K/N 或 T/W/Z 列「防御技|主动技」）时，根据鼠标悬停位置判断：
    /// 悬停在描述中「防御技」三字上则显示 intro.xlsx 中 id=防御技 的介绍，悬停在「主动技」上则显示 id=主动技 的介绍，依此类推。
    /// </summary>
    public class SkillDescHoverHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public int SkillIndex;

        private Coroutine _delayRoutine;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_delayRoutine != null) StopCoroutine(_delayRoutine);
            _delayRoutine = StartCoroutine(ShowIntroAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }
            CompendiumUI.HideIntroModal();
        }

        private IEnumerator ShowIntroAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            _delayRoutine = null;

            var data = CompendiumUI.GetDetailCurrentCardData();
            if (data == null) yield break;

            bool useSpecial = CompendiumUI.GetDetailShowSpecialFormSkills();
            var tags = useSpecial
                ? (SkillIndex == 0 ? data.SpecialSkillTags1 : SkillIndex == 1 ? data.SpecialSkillTags2 : data.SpecialSkillTags3)
                : (SkillIndex == 0 ? data.SkillTags1 : SkillIndex == 1 ? data.SkillTags2 : data.SkillTags3);
            if (tags == null || tags.Count == 0) yield break;

            var text = GetComponent<TextMeshProUGUI>();
            if (text == null) yield break;

            // 对比文本内容：检测鼠标当前悬停在哪一段文字上，该段文字对应哪个标签（强制技/持续技/中毒/攻心等），只显示该标签的介绍
            string tagAtCursor = GetTagUnderMouseTMP(text, tags);
            if (string.IsNullOrEmpty(tagAtCursor)) yield break;

            string intro = IntroLoader.GetIntro(tagAtCursor);
            if (string.IsNullOrEmpty(intro)) yield break;

            CompendiumUI.ShowIntroModal(tagAtCursor);
        }

        private static string GetPlainText(string rich)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            string s = rich;
            while (true)
            {
                int start = s.IndexOf('<');
                if (start < 0) break;
                int end = s.IndexOf('>', start);
                if (end < 0) break;
                s = s.Remove(start, end - start + 1);
            }
            return s;
        }

        /// <summary>
        /// 用 TMP_TextUtilities 做「坐标→字符」检测，返回该位置对应的 tag，支持 &lt;u&gt; 等富文本。
        /// </summary>
        private static string GetTagUnderMouseTMP(TextMeshProUGUI text, List<string> tags)
        {
            if (text == null || text.rectTransform == null || tags == null || tags.Count == 0) return null;

            Camera cam = null;
            var canvas = text.canvas;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
                cam = canvas.worldCamera;

            int charIndex = TMP_TextUtilities.FindIntersectingCharacter(text, Input.mousePosition, cam, true);
            if (charIndex < 0 || charIndex >= text.textInfo.characterCount) return null;

            string plain = GetPlainText(text.text);
            if (charIndex >= plain.Length) return null;

            string bestTag = null;
            int bestLen = 0;
            int bestStart = -1;

            for (int t = 0; t < tags.Count; t++)
            {
                string tag = (tags[t] ?? "").Trim();
                if (string.IsNullOrEmpty(tag)) continue;

                int start = 0;
                while (start < plain.Length)
                {
                    int idx = plain.IndexOf(tag, start, System.StringComparison.Ordinal);
                    if (idx < 0) break;

                    int end = idx + tag.Length;
                    if (end > plain.Length) break;

                    if (charIndex >= idx && charIndex < end)
                    {
                        bool better = tag.Length > bestLen || (tag.Length == bestLen && idx > bestStart);
                        if (better)
                        {
                            bestTag = tag;
                            bestLen = tag.Length;
                            bestStart = idx;
                        }
                    }

                    start = idx + tag.Length;
                }
            }

            return bestTag;
        }
    }
}
