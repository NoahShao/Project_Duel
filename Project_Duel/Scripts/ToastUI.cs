using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 简单 Toast 提示，居中显示一段时间后自动消失。
    /// </summary>
    public static class ToastUI
    {
        private static GameObject _root;
        private static TextMeshProUGUI _text;
        private static Coroutine _hideRoutine;

        public static void Show(string message, float duration = 2f)
        {
            if (_root == null)
                Create();
            _text.text = message ?? "";
            _root.SetActive(true);
            if (_hideRoutine != null)
            {
                var go = _root.GetComponent<MonoBehaviour>();
                if (go != null)
                    go.StopCoroutine(_hideRoutine);
            }
            var runner = _root.GetComponent<ToastRunner>();
            if (runner != null)
                _hideRoutine = runner.StartCoroutine(HideAfter(duration));
        }

        private static IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _root.SetActive(false);
            _hideRoutine = null;
        }

        private static void Create()
        {
            _root = new GameObject("ToastUI");
            _root.SetActive(false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(520, 80);
            panel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panel.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null) _text.font = TMPHelper.GetDefaultFont();
            _text.fontSize = 28;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 8);
            textRect.offsetMax = new Vector2(-16, -8);

            _root.AddComponent<ToastRunner>();
        }

        private class ToastRunner : MonoBehaviour { }
    }
}
