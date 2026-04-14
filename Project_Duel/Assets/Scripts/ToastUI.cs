using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 简单 Toast 提示，居中显示一段时间后自动消失。
    /// 多条请求排队展示，避免技能宣告横幅被同一帧内后续 Toast（如防御宣告）覆盖。
    /// </summary>
    public static class ToastUI
    {
        private struct QueuedToast
        {
            public string Message;
            public float Duration;
            public bool PauseGameWhileVisible;
            public Action OnComplete;
        }

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static TextMeshProUGUI _text;
        private static Coroutine _hideRoutine;
        private static float _savedTimeScale = 1f;
        private static bool _gamePausedForToast;
        private static Action _toastOnComplete;
        private static readonly Queue<QueuedToast> s_queue = new Queue<QueuedToast>();

        /// <summary>技能横幅等 <c>pauseGameWhileVisible</c> 为 true 时，Time.timeScale 被置 0；用于暂停战报续跑与 AI 连点 EndTurn。</summary>
        public static bool IsSkillBannerTimeFreezeActive() => _gamePausedForToast;

        /// <summary>胜负已定时清空队列并关闭 Toast，不执行未完成的 onComplete。</summary>
        public static void CancelAllToastsImmediate()
        {
            s_queue.Clear();
            if (_root != null)
            {
                var runner = _root.GetComponent<ToastRunner>();
                if (_hideRoutine != null && runner != null)
                    runner.StopCoroutine(_hideRoutine);
            }

            _hideRoutine = null;
            _toastOnComplete = null;
            RestoreTimeScaleIfPausedByToast();
            if (_root != null)
                _root.SetActive(false);
        }

        public static void Show(string message, float duration = 2f, bool pauseGameWhileVisible = false, Action onComplete = null)
        {
            if (_root == null)
                Create();

            s_queue.Enqueue(new QueuedToast
            {
                Message = message ?? "",
                Duration = duration,
                PauseGameWhileVisible = pauseGameWhileVisible,
                OnComplete = onComplete,
            });
            TryStartNextQueuedToast();
        }

        private static void TryStartNextQueuedToast()
        {
            if (_hideRoutine != null || _root == null)
                return;
            if (s_queue.Count == 0)
                return;

            QueuedToast item = s_queue.Dequeue();
            RestoreTimeScaleIfPausedByToast();

            _text.text = item.Message;
            // 必须先激活层级再量字，否则首局首次横幅时 TMP 未参与 Canvas 布局，会错误换行且底框尺寸不对。
            _root.SetActive(true);
            ApplyPanelSizeToText();

            _toastOnComplete = item.OnComplete;
            var runner = _root.GetComponent<ToastRunner>();
            if (runner != null)
                _hideRoutine = runner.StartCoroutine(HideAfter(item.Duration, item.PauseGameWhileVisible));
        }

        private static void RestoreTimeScaleIfPausedByToast()
        {
            if (!_gamePausedForToast)
                return;
            Time.timeScale = _savedTimeScale;
            _gamePausedForToast = false;
        }

        private static IEnumerator HideAfter(float seconds, bool pauseGameWhileVisible)
        {
            if (pauseGameWhileVisible)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _gamePausedForToast = true;
            }

            if (pauseGameWhileVisible)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);

            if (pauseGameWhileVisible && _gamePausedForToast)
            {
                Time.timeScale = _savedTimeScale;
                _gamePausedForToast = false;
            }

            if (_root != null)
                _root.SetActive(false);
            _hideRoutine = null;

            Action cb = _toastOnComplete;
            _toastOnComplete = null;
            cb?.Invoke();
            TryStartNextQueuedToast();
            if (!_gamePausedForToast)
                BattlePhaseManager.NotifyToastBannerUnblocked();
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
            _panelRect = panelRect;
            panelRect.sizeDelta = new Vector2(520, 80);
            panel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panel.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null) _text.font = TMPHelper.GetDefaultFont();
            _text.fontSize = 28;
            _text.enableWordWrapping = true;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            _root.AddComponent<ToastRunner>();
        }

        /// <summary>按当前文案测量 TMP 排版，撑开底图（技能横幅等长句不换行裁切问题）。</summary>
        private static void ApplyPanelSizeToText()
        {
            if (_text == null || _panelRect == null)
                return;

            const float maxTextWidth = 1680f;
            const float padX = 40f;
            const float padY = 28f;
            const float minPanelW = 280f;
            const float minPanelH = 72f;

            string t = _text.text ?? string.Empty;
            // ignoreActiveState：避免在层级切换的同一帧内漏更新网格。
            _text.ForceMeshUpdate(true);
            Vector2 pref = MeasurePreferredWrapped(t, maxTextWidth);
            float innerW = Mathf.Max(pref.x, 40f);
            float innerH = Mathf.Max(pref.y, _text.fontSize * 1.25f);
            float panelW = Mathf.Clamp(innerW + padX, minPanelW, maxTextWidth + padX);
            float panelH = Mathf.Max(innerH + padY, minPanelH);
            _panelRect.sizeDelta = new Vector2(panelW, panelH);
            var tr = _text.rectTransform;
            tr.sizeDelta = new Vector2(panelW - padX, panelH - padY);
            Canvas.ForceUpdateCanvases();
            // 内层尺寸更新后 TMP 可能重排，再量一次避免首帧与后续不一致。
            _text.ForceMeshUpdate(true);
            Vector2 pref2 = MeasurePreferredWrapped(t, maxTextWidth);
            float innerW2 = Mathf.Max(pref2.x, 40f);
            float innerH2 = Mathf.Max(pref2.y, _text.fontSize * 1.25f);
            float panelW2 = Mathf.Clamp(innerW2 + padX, minPanelW, maxTextWidth + padX);
            float panelH2 = Mathf.Max(innerH2 + padY, minPanelH);
            if (!Mathf.Approximately(panelW2, panelW) || !Mathf.Approximately(panelH2, panelH))
            {
                _panelRect.sizeDelta = new Vector2(panelW2, panelH2);
                tr.sizeDelta = new Vector2(panelW2 - padX, panelH2 - padY);
            }
        }

        private static Vector2 MeasurePreferredWrapped(string t, float maxTextWidth)
        {
            if (_text == null)
                return Vector2.zero;
            return _text.GetPreferredValues(t, maxTextWidth, 0f);
        }

        private class ToastRunner : MonoBehaviour { }
    }
}
