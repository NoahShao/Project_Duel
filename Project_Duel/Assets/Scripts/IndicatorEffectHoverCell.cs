using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JunzhenDuijue
{
    /// <summary>战斗指示物格：悬浮约 0.5s 后展示 intro 介绍，移出隐藏。</summary>
    public sealed class IndicatorEffectHoverCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string IntroLookupId = string.Empty;

        private Coroutine _delayRoutine;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(IntroLookupId))
                return;
            if (_delayRoutine != null)
                StopCoroutine(_delayRoutine);
            _delayRoutine = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }

            GameUI.HideBattleIndicatorIntroTooltip();
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            _delayRoutine = null;
            GameUI.ShowBattleIndicatorIntroTooltip(IntroLookupId, Input.mousePosition);
        }

        private void OnDisable()
        {
            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }

            GameUI.HideBattleIndicatorIntroTooltip();
        }
    }
}
