using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 对局主界面：双方武将牌（各 3 张，半露）、中央横向分割线、回合按钮、牌堆与弃牌堆；卡牌点击放大预留接口供双方/联网调用。
    /// </summary>
    public static class GameUI
    {
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;
        private const float CardAspectW = 1016f;
        private const float CardAspectH = 1488f;

        private static GameObject _root;
        private static BattleState _state;
        private static Button _turnButton;
        private static TextMeshProUGUI _turnButtonText;
        private static TextMeshProUGUI _phaseLabel;
        private static TextMeshProUGUI _deckCountTooltip;
        private static GameObject _deckTooltipRoot;
        private static Button _discardButton;
        private static TextMeshProUGUI _discardButtonLabel;
        private static GameObject _discardPopupRoot;
        private static Transform _discardPopupContent;
        private static TextMeshProUGUI _discardPopupTitle;
        private static GameObject _cardEnlargeRoot;
        private static Image _cardEnlargeImage;
        private static List<GameObject> _playerCardRows = new List<GameObject>();
        private static List<GameObject> _opponentCardRows = new List<GameObject>();
        private static GameObject _opponentDeckTooltipRoot;
        private static TextMeshProUGUI _opponentDeckCountTooltip;
        private static Button _opponentDiscardButton;
        private static TextMeshProUGUI _opponentDiscardButtonLabel;
        private static bool _opponentTurnAutoEnd = true;
        private static GameObject _discardPhasePopupRoot;
        private static Transform _discardPhaseContent;
        private static TextMeshProUGUI _discardPhaseTitle;
        private static Button _discardPhaseConfirmBtn;
        private static List<int> _discardPhaseSelectedIndices = new List<int>();
        private static int _discardPhaseNeedCount;
        private static bool _discardPhaseIsPlayer;
        private static GameObject _moralePopupRoot;
        private static Button[] _moraleEffectButtons = new Button[3];
        private static Transform _playerHandContent;
        private static Transform _opponentHandContent;
        private static TextMeshProUGUI _playerHandLabel;
        private static TextMeshProUGUI _opponentHandLabel;
        private static Transform _handHoverOverlay;
        private static GameObject _playedZoneRoot;
        private static Transform _playedZoneContent;

        /// <summary>
        /// 请求展示卡牌放大（武将牌）。双方均可调用，便于后续联网同步。
        /// </summary>
        public static System.Action<string, bool> OnRequestCardEnlarge;
        public static Transform GetRootTransform() => _root != null ? _root.transform : null;
        public static Transform GetHandHoverOverlay() => _handHoverOverlay;

        public static bool IsOpponentTurnAutoEndEnabled() => _opponentTurnAutoEnd;
        public static void NotifyPhaseChanged() { RefreshAllFromState(); }

        public static void Create()
        {
            _root = new GameObject("GameUI");
            _root.SetActive(false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = new Vector2(RefWidth, RefHeight);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(RefWidth, RefHeight);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_root.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.16f, 1f);
            SetFullRect(bg.GetComponent<RectTransform>());

            BuildDivider();
            BuildTurnButton();
            BuildDeckAndDiscardArea();
            BuildHandAndCharacterFrames();
            BuildPlayedZone();
            BuildOpponentGenerals();
            BuildPlayerGenerals();
            BuildMoraleAreas();
            BuildCardEnlargeOverlay();
            BuildDiscardPopup();
            BuildMoralePopup();
            BuildDiscardPhasePopup();
            BuildVictoryDefeatPopups();
            RegisterCardEnlarge();
            if (_phaseLabel != null && _phaseLabel.transform.parent != null)
                _phaseLabel.transform.parent.SetAsLastSibling();
            if (_turnButton != null)
                _turnButton.transform.SetAsLastSibling();
        }

        private static void BuildVictoryDefeatPopups()
        {
            _victoryPopupRoot = new GameObject("VictoryPopup");
            _victoryPopupRoot.transform.SetParent(_root.transform, false);
            _victoryPopupRoot.SetActive(false);
            SetFullRect(_victoryPopupRoot.AddComponent<RectTransform>());
            var vCanvas = _victoryPopupRoot.AddComponent<Canvas>();
            vCanvas.overrideSorting = true;
            vCanvas.sortingOrder = 100;
            _victoryPopupRoot.AddComponent<GraphicRaycaster>();
            var vBg = new GameObject("Bg");
            vBg.transform.SetParent(_victoryPopupRoot.transform, false);
            vBg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            SetFullRect(vBg.GetComponent<RectTransform>());
            var vPanel = new GameObject("Panel");
            vPanel.transform.SetParent(_victoryPopupRoot.transform, false);
            var vpR = vPanel.AddComponent<RectTransform>();
            vpR.anchorMin = vpR.anchorMax = new Vector2(0.5f, 0.5f);
            vpR.sizeDelta = new Vector2(400, 200);
            vPanel.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f, 0.98f);
            var vText = CreateGameText(vPanel.transform, "胜利", 48);
            SetFullRect(vText.GetComponent<RectTransform>());

            _defeatPopupRoot = new GameObject("DefeatPopup");
            _defeatPopupRoot.transform.SetParent(_root.transform, false);
            _defeatPopupRoot.SetActive(false);
            SetFullRect(_defeatPopupRoot.AddComponent<RectTransform>());
            var dCanvas = _defeatPopupRoot.AddComponent<Canvas>();
            dCanvas.overrideSorting = true;
            dCanvas.sortingOrder = 100;
            _defeatPopupRoot.AddComponent<GraphicRaycaster>();
            var dBg = new GameObject("Bg");
            dBg.transform.SetParent(_defeatPopupRoot.transform, false);
            dBg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            SetFullRect(dBg.GetComponent<RectTransform>());
            var dPanel = new GameObject("Panel");
            dPanel.transform.SetParent(_defeatPopupRoot.transform, false);
            var dpR = dPanel.AddComponent<RectTransform>();
            dpR.anchorMin = dpR.anchorMax = new Vector2(0.5f, 0.5f);
            dpR.sizeDelta = new Vector2(400, 200);
            dPanel.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.98f);
            var dText = CreateGameText(dPanel.transform, "失败", 48);
            SetFullRect(dText.GetComponent<RectTransform>());
        }

        /// <summary> 对玩家造成伤害，若血量归零则弹出失败。 </summary>
        public static void ApplyDamageToPlayer(int amount)
        {
            if (_state == null || amount <= 0) return;
            _state.Player.CurrentHp = Mathf.Max(0, _state.Player.CurrentHp - amount);
            RefreshAllFromState();
            if (_state.Player.CurrentHp <= 0) ShowDefeatPopup();
        }

        /// <summary> 对对手造成伤害，若血量归零则弹出胜利。 </summary>
        public static void ApplyDamageToOpponent(int amount)
        {
            if (_state == null || amount <= 0) return;
            _state.Opponent.CurrentHp = Mathf.Max(0, _state.Opponent.CurrentHp - amount);
            RefreshAllFromState();
            if (_state.Opponent.CurrentHp <= 0) ShowVictoryPopup();
        }

        private static void ShowVictoryPopup() { if (_victoryPopupRoot != null) _victoryPopupRoot.SetActive(true); }
        private static void ShowDefeatPopup() { if (_defeatPopupRoot != null) _defeatPopupRoot.SetActive(true); }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 创建游戏内文本（与图鉴等统一）：TextMeshProUGUI + defaultFontAsset + 富文本开启以支持 &lt;u&gt; 下划线等。
        /// </summary>
        private static TextMeshProUGUI CreateGameText(Transform parent, string content, int fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            if (parent == null) return null;
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (t == null) return null;
            t.text = content;
            var font = TMPHelper.GetDefaultFont();
            if (font != null) t.font = font;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = Color.white;
            return t;
        }

        private static void BuildDivider()
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(_root.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 32);
            go.AddComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 1f);

            var labelGo = new GameObject("PhaseLabel");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(200, 28);
            _phaseLabel = CreateGameText(labelGo.transform, "准备阶段", 22);
            SetFullRect(_phaseLabel.GetComponent<RectTransform>());
        }

        private static void BuildTurnButton()
        {
            var go = new GameObject("TurnButton");
            go.transform.SetParent(_root.transform, false);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-40, 0);
            rect.sizeDelta = new Vector2(180, 56);
            go.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.5f, 1f);
            _turnButton = go.AddComponent<Button>();

            _turnButtonText = CreateGameText(go.transform, "你的回合", 26);
            SetFullRect(_turnButtonText.GetComponent<RectTransform>());

            _turnButton.onClick.AddListener(OnEndTurn);
        }

        private static void BuildDeckAndDiscardArea()
        {
            float leftX = 120f;
            float deckY = 140f;
            float deckW = 100f;
            float deckH = 140f;
            float discardY = 40f;
            float discardW = 120f;
            float discardH = 44f;

            var deckGo = new GameObject("Deck");
            deckGo.transform.SetParent(_root.transform, false);
            var deckRect = deckGo.AddComponent<RectTransform>();
            deckRect.anchorMin = new Vector2(0f, 0f);
            deckRect.anchorMax = new Vector2(0f, 0f);
            deckRect.pivot = new Vector2(0.5f, 0.5f);
            deckRect.anchoredPosition = new Vector2(leftX, deckY);
            deckRect.sizeDelta = new Vector2(deckW, deckH);
            var deckImg = deckGo.AddComponent<Image>();
            deckImg.color = new Color(0.2f, 0.22f, 0.3f, 1f);
            var deckBtn = deckGo.AddComponent<Button>();
            deckBtn.transition = Selectable.Transition.None;

            _deckTooltipRoot = new GameObject("DeckTooltip");
            _deckTooltipRoot.transform.SetParent(_root.transform, false);
            _deckTooltipRoot.SetActive(false);
            var ttRect = _deckTooltipRoot.AddComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0f, 0f);
            ttRect.anchorMax = new Vector2(0f, 0f);
            ttRect.pivot = new Vector2(0.5f, 0f);
            ttRect.anchoredPosition = new Vector2(leftX, deckY + deckH * 0.5f + 30f);
            ttRect.sizeDelta = new Vector2(120, 36);
            var ttBg = _deckTooltipRoot.AddComponent<Image>();
            ttBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            _deckCountTooltip = CreateGameText(_deckTooltipRoot.transform, "", 22);
            SetFullRect(_deckCountTooltip.GetComponent<RectTransform>());

            var deckHover = deckGo.AddComponent<DeckTooltipHover>();
            deckHover.TooltipRoot = _deckTooltipRoot;
            deckHover.CountLabel = _deckCountTooltip;
            deckHover.GetCount = () => _state != null ? _state.Player.Deck.Count : 0;

            var discardGo = new GameObject("Discard");
            discardGo.transform.SetParent(_root.transform, false);
            var discardRect = discardGo.AddComponent<RectTransform>();
            discardRect.anchorMin = new Vector2(0f, 0f);
            discardRect.anchorMax = new Vector2(0f, 0f);
            discardRect.pivot = new Vector2(0.5f, 0.5f);
            discardRect.anchoredPosition = new Vector2(leftX, discardY);
            discardRect.sizeDelta = new Vector2(discardW, discardH);
            discardGo.AddComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
            _discardButton = discardGo.AddComponent<Button>();
            _discardButton.onClick.AddListener(() => OpenDiscardPopup(true));

            var discardLabelGo = new GameObject("Label");
            discardLabelGo.transform.SetParent(discardGo.transform, false);
            _discardButtonLabel = CreateGameText(discardLabelGo.transform, "弃牌堆", 20);
            SetFullRect(_discardButtonLabel.GetComponent<RectTransform>());

            float oppDeckY = 140f;
            float oppDiscardY = 40f;
            var oppDeckGo = new GameObject("OpponentDeck");
            oppDeckGo.transform.SetParent(_root.transform, false);
            var oppDeckR = oppDeckGo.AddComponent<RectTransform>();
            oppDeckR.anchorMin = new Vector2(0f, 1f);
            oppDeckR.anchorMax = new Vector2(0f, 1f);
            oppDeckR.pivot = new Vector2(0.5f, 0.5f);
            oppDeckR.anchoredPosition = new Vector2(leftX, -deckY);
            oppDeckR.sizeDelta = new Vector2(deckW, deckH);
            oppDeckGo.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.3f, 1f);
            oppDeckGo.AddComponent<Button>().transition = Selectable.Transition.None;
            _opponentDeckTooltipRoot = new GameObject("OpponentDeckTooltip");
            _opponentDeckTooltipRoot.transform.SetParent(_root.transform, false);
            _opponentDeckTooltipRoot.SetActive(false);
            var oppTtR = _opponentDeckTooltipRoot.AddComponent<RectTransform>();
            oppTtR.anchorMin = new Vector2(0f, 1f);
            oppTtR.anchorMax = new Vector2(0f, 1f);
            oppTtR.pivot = new Vector2(0.5f, 1f);
            oppTtR.anchoredPosition = new Vector2(leftX, -(deckY + deckH * 0.5f + 30f));
            oppTtR.sizeDelta = new Vector2(120, 36);
            _opponentDeckTooltipRoot.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            _opponentDeckCountTooltip = CreateGameText(_opponentDeckTooltipRoot.transform, "", 22);
            SetFullRect(_opponentDeckCountTooltip.GetComponent<RectTransform>());
            var oppDeckHover = oppDeckGo.AddComponent<DeckTooltipHover>();
            oppDeckHover.TooltipRoot = _opponentDeckTooltipRoot;
            oppDeckHover.CountLabel = _opponentDeckCountTooltip;
            oppDeckHover.GetCount = () => _state != null ? _state.Opponent.Deck.Count : 0;
            var oppDiscardGo = new GameObject("OpponentDiscard");
            oppDiscardGo.transform.SetParent(_root.transform, false);
            var oppDiscardR = oppDiscardGo.AddComponent<RectTransform>();
            oppDiscardR.anchorMin = new Vector2(0f, 1f);
            oppDiscardR.anchorMax = new Vector2(0f, 1f);
            oppDiscardR.pivot = new Vector2(0.5f, 0.5f);
            oppDiscardR.anchoredPosition = new Vector2(leftX, -discardY);
            oppDiscardR.sizeDelta = new Vector2(discardW, discardH);
            oppDiscardGo.AddComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
            _opponentDiscardButton = oppDiscardGo.AddComponent<Button>();
            _opponentDiscardButton.onClick.AddListener(() => OpenDiscardPopup(false));
            var oppDiscardLabelGo = new GameObject("Label");
            oppDiscardLabelGo.transform.SetParent(oppDiscardGo.transform, false);
            _opponentDiscardButtonLabel = CreateGameText(oppDiscardLabelGo.transform, "弃牌堆", 20);
            SetFullRect(_opponentDiscardButtonLabel.GetComponent<RectTransform>());
        }

        private static void BuildHandAndCharacterFrames()
        {
            float handW = 420f;
            float handH = 160f;
            float margin = 24f;
            float labelGap = 6f;
            float labelH = 22f;

            var playerLabelGo = new GameObject("PlayerHandLabel");
            playerLabelGo.transform.SetParent(_root.transform, false);
            var plr = playerLabelGo.AddComponent<RectTransform>();
            plr.anchorMin = new Vector2(1f, 0f);
            plr.anchorMax = new Vector2(1f, 0f);
            plr.pivot = new Vector2(0.5f, 0f);
            plr.anchoredPosition = new Vector2(-margin - handW * 0.5f, margin + handH + labelGap + labelH * 0.5f);
            plr.sizeDelta = new Vector2(handW - 16, labelH);
            _playerHandLabel = CreateGameText(playerLabelGo.transform, "手牌上限：6/手牌数量：0", 18);
            SetFullRect(_playerHandLabel.GetComponent<RectTransform>());

            var playerFrame = new GameObject("PlayerHandFrame");
            playerFrame.transform.SetParent(_root.transform, false);
            var pr = playerFrame.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(1f, 0f);
            pr.anchorMax = new Vector2(1f, 0f);
            pr.pivot = new Vector2(1f, 0f);
            pr.anchoredPosition = new Vector2(-margin, margin);
            pr.sizeDelta = new Vector2(handW, handH);
            var pi = playerFrame.AddComponent<Image>();
            pi.color = new Color(0.15f, 0.18f, 0.22f, 0.85f);
            pi.raycastTarget = false;
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(playerFrame.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(8, 8);
            vpRect.offsetMax = new Vector2(-8, -8);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<RectMask2D>();
            var playerContent = new GameObject("PlayerHandContent");
            playerContent.transform.SetParent(viewport.transform, false);
            var pcRect = playerContent.AddComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(1f, 0.5f);
            pcRect.anchorMax = new Vector2(1f, 0.5f);
            pcRect.pivot = new Vector2(1f, 0.5f);
            pcRect.anchoredPosition = Vector2.zero;
            pcRect.sizeDelta = new Vector2(0, handH - 16);
            var phlg = playerContent.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = -36f;
            phlg.childAlignment = TextAnchor.MiddleRight;
            phlg.childControlWidth = phlg.childControlHeight = false;
            phlg.childForceExpandWidth = phlg.childForceExpandHeight = false;
            playerContent.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var psr = playerFrame.AddComponent<RightMouseScrollRect>();
            psr.content = pcRect;
            psr.viewport = vpRect;
            psr.horizontal = true;
            psr.vertical = false;
            _playerHandContent = playerContent.transform;

            var oppLabelGo = new GameObject("OpponentHandLabel");
            oppLabelGo.transform.SetParent(_root.transform, false);
            var olr = oppLabelGo.AddComponent<RectTransform>();
            olr.anchorMin = new Vector2(1f, 1f);
            olr.anchorMax = new Vector2(1f, 1f);
            olr.pivot = new Vector2(0.5f, 1f);
            olr.anchoredPosition = new Vector2(-margin - handW * 0.5f, -margin - handH - labelGap - labelH * 0.5f);
            olr.sizeDelta = new Vector2(handW - 16, labelH);
            _opponentHandLabel = CreateGameText(oppLabelGo.transform, "手牌上限：6/手牌数量：0", 18);
            SetFullRect(_opponentHandLabel.GetComponent<RectTransform>());

            var oppFrame = new GameObject("OpponentHandFrame");
            oppFrame.transform.SetParent(_root.transform, false);
            var or = oppFrame.AddComponent<RectTransform>();
            or.anchorMin = new Vector2(1f, 1f);
            or.anchorMax = new Vector2(1f, 1f);
            or.pivot = new Vector2(1f, 1f);
            or.anchoredPosition = new Vector2(-margin, -margin);
            or.sizeDelta = new Vector2(handW, handH);
            var oi = oppFrame.AddComponent<Image>();
            oi.color = new Color(0.15f, 0.18f, 0.22f, 0.85f);
            oi.raycastTarget = true;
            var oppViewport = new GameObject("Viewport");
            oppViewport.transform.SetParent(oppFrame.transform, false);
            var ovpRect = oppViewport.AddComponent<RectTransform>();
            ovpRect.anchorMin = Vector2.zero;
            ovpRect.anchorMax = Vector2.one;
            ovpRect.offsetMin = new Vector2(8, 8);
            ovpRect.offsetMax = new Vector2(-8, -8);
            oppViewport.AddComponent<Image>().color = Color.clear;
            oppViewport.AddComponent<RectMask2D>();
            var oppContent = new GameObject("OpponentHandContent");
            oppContent.transform.SetParent(oppViewport.transform, false);
            var ocRect = oppContent.AddComponent<RectTransform>();
            ocRect.anchorMin = new Vector2(1f, 0.5f);
            ocRect.anchorMax = new Vector2(1f, 0.5f);
            ocRect.pivot = new Vector2(1f, 0.5f);
            ocRect.anchoredPosition = Vector2.zero;
            ocRect.sizeDelta = new Vector2(0, handH - 16);
            var ohlg = oppContent.AddComponent<HorizontalLayoutGroup>();
            ohlg.spacing = -36f;
            ohlg.childAlignment = TextAnchor.MiddleRight;
            ohlg.childControlWidth = ohlg.childControlHeight = false;
            ohlg.childForceExpandWidth = ohlg.childForceExpandHeight = false;
            oppContent.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var osr = oppFrame.AddComponent<ScrollRect>();
            osr.content = ocRect;
            osr.viewport = ovpRect;
            osr.horizontal = true;
            osr.vertical = false;
            osr.enabled = false;
            var oppCg = oppFrame.AddComponent<CanvasGroup>();
            oppCg.interactable = false;
            oppCg.blocksRaycasts = true;
            _opponentHandContent = oppContent.transform;

            var hoverOverlay = new GameObject("HandHoverOverlay");
            hoverOverlay.transform.SetParent(_root.transform, false);
            var hoverRect = hoverOverlay.AddComponent<RectTransform>();
            SetFullRect(hoverRect);
            hoverOverlay.transform.SetAsLastSibling();
            _handHoverOverlay = hoverOverlay.transform;
        }

        private static void BuildPlayedZone()
        {
            _playedZoneRoot = new GameObject("PlayedZone");
            _playedZoneRoot.transform.SetParent(_root.transform, false);
            var rect = _playedZoneRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.32f);
            rect.anchorMax = new Vector2(1f, 0.56f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = _playedZoneRoot.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f, 0.5f);
            img.raycastTarget = true;
            var content = new GameObject("Content");
            content.transform.SetParent(_playedZoneRoot.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(800, 120);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = false;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            _playedZoneContent = content.transform;
            _playedZoneRoot.AddComponent<PlayedZoneMarker>();
        }

        private static GameObject _playerMoraleRoot;
        private static Image[] _playerMoraleIcons = new Image[2];
        private static GameObject _opponentMoraleRoot;
        private static GameObject _playerHpRoot;
        private static TextMeshProUGUI _playerHpText;
        private static Image _playerHpFill;
        private static GameObject _opponentHpRoot;
        private static TextMeshProUGUI _opponentHpText;
        private static Image _opponentHpFill;
        private static GameObject _victoryPopupRoot;
        private static GameObject _defeatPopupRoot;

        private static void BuildMoraleAreas()
        {
            float leftX = 120f;
            float moraleY = 380f;
            float iconSize = 44f;
            float gap = 12f;
            _playerMoraleRoot = new GameObject("PlayerMorale");
            _playerMoraleRoot.transform.SetParent(_root.transform, false);
            var pr = _playerMoraleRoot.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0f);
            pr.anchorMax = new Vector2(0f, 0f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.anchoredPosition = new Vector2(leftX + iconSize + gap * 0.5f, moraleY);
            pr.sizeDelta = new Vector2(iconSize * 2 + gap, iconSize);
            var pBtn = _playerMoraleRoot.AddComponent<Button>();
            pBtn.onClick.AddListener(OpenMoralePopup);
            for (int i = 0; i < 2; i++)
            {
                var icon = new GameObject("Icon" + i);
                icon.transform.SetParent(_playerMoraleRoot.transform, false);
                var ir = icon.AddComponent<RectTransform>();
                ir.anchorMin = new Vector2(i * 0.5f, 0f);
                ir.anchorMax = new Vector2(i == 0 ? 0.5f : 1f, 1f);
                ir.offsetMin = Vector2.zero;
                ir.offsetMax = Vector2.zero;
                _playerMoraleIcons[i] = icon.AddComponent<Image>();
                _playerMoraleIcons[i].sprite = GetHexagonSprite();
                _playerMoraleIcons[i].color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }
            _opponentMoraleRoot = new GameObject("OpponentMorale");
            _opponentMoraleRoot.transform.SetParent(_root.transform, false);
            var or = _opponentMoraleRoot.AddComponent<RectTransform>();
            or.anchorMin = new Vector2(0f, 1f);
            or.anchorMax = new Vector2(0f, 1f);
            or.pivot = new Vector2(0.5f, 0.5f);
            or.anchoredPosition = new Vector2(leftX + iconSize + gap * 0.5f, -moraleY);
            or.sizeDelta = new Vector2(iconSize * 2 + gap, iconSize);
            for (int i = 0; i < 2; i++)
            {
                var icon = new GameObject("Icon" + i);
                icon.transform.SetParent(_opponentMoraleRoot.transform, false);
                var ir = icon.AddComponent<RectTransform>();
                ir.anchorMin = new Vector2(i * 0.5f, 0f);
                ir.anchorMax = new Vector2(i == 0 ? 0.5f : 1f, 1f);
                ir.offsetMin = Vector2.zero;
                ir.offsetMax = Vector2.zero;
                var oImg = icon.AddComponent<Image>();
                oImg.sprite = GetHexagonSprite();
                oImg.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }
        }

        private static void BuildMoralePopup()
        {
            _moralePopupRoot = new GameObject("MoralePopup");
            _moralePopupRoot.transform.SetParent(_root.transform, false);
            _moralePopupRoot.SetActive(false);
            SetFullRect(_moralePopupRoot.AddComponent<RectTransform>());
            var canvas = _moralePopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 42;
            _moralePopupRoot.AddComponent<GraphicRaycaster>();
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_moralePopupRoot.transform, false);
            var oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0, 0, 0, 0.6f);
            oImg.raycastTarget = true;
            SetFullRect(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Button>().transition = Selectable.Transition.None;
            overlay.GetComponent<Button>().onClick.AddListener(CloseMoralePopup);
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_moralePopupRoot.transform, false);
            var panelR = panel.AddComponent<RectTransform>();
            panelR.anchorMin = new Vector2(0.5f, 0.5f);
            panelR.anchorMax = new Vector2(0.5f, 0.5f);
            panelR.pivot = new Vector2(0.5f, 0.5f);
            panelR.anchoredPosition = Vector2.zero;
            panelR.sizeDelta = new Vector2(400, 320);
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleR = titleGo.AddComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0.5f, 1f);
            titleR.anchorMax = new Vector2(0.5f, 1f);
            titleR.pivot = new Vector2(0.5f, 1f);
            titleR.anchoredPosition = new Vector2(0, -20);
            titleR.sizeDelta = new Vector2(360, 32);
            var titleT = CreateGameText(titleGo.transform, "请选择使用你的士气效果", 26);
            string[] labels = new[] { "摸两张牌", "增加出牌阶段", "一名己方角色翻面" };
            for (int i = 0; i < 3; i++)
            {
                int effectIndex = i;
                var btnGo = new GameObject("Effect" + i);
                btnGo.transform.SetParent(panel.transform, false);
                var br = btnGo.AddComponent<RectTransform>();
                br.anchorMin = new Vector2(0.5f, 1f);
                br.anchorMax = new Vector2(0.5f, 1f);
                br.pivot = new Vector2(0.5f, 1f);
                br.anchoredPosition = new Vector2(0, -72 - i * 56);
                br.sizeDelta = new Vector2(320, 48);
                btnGo.AddComponent<Image>().color = new Color(0.28f, 0.35f, 0.5f, 1f);
                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => OnMoraleEffectClick(effectIndex));
                var tt = CreateGameText(btnGo.transform, labels[i], 22);
                SetFullRect(tt.GetComponent<RectTransform>());
                _moraleEffectButtons[i] = btn;
            }
        }

        private static void OpenMoralePopup()
        {
            if (_state == null || !_state.IsPlayerTurn || _state.Player.Morale <= 0) return;
            _moralePopupRoot.SetActive(true);
            RefreshMoralePopupButtons();
        }

        private static void RefreshMoralePopupButtons()
        {
            if (_state == null) return;
            var used = _state.Player.MoraleUsedThisTurn;
            for (int i = 0; i < 3 && i < _moraleEffectButtons.Length; i++)
            {
                var btn = _moraleEffectButtons[i];
                if (btn != null)
                {
                    btn.interactable = !used[i];
                    btn.GetComponent<Image>().color = used[i] ? new Color(0.35f, 0.35f, 0.38f, 1f) : new Color(0.28f, 0.35f, 0.5f, 1f);
                }
            }
        }

        private static void OnMoraleEffectClick(int effectIndex)
        {
            if (_state == null || !_state.IsPlayerTurn || _state.Player.Morale <= 0) return;
            if (_state.Player.MoraleUsedThisTurn[effectIndex]) return;
            _state.Player.MoraleUsedThisTurn[effectIndex] = true;
            _state.Player.Morale--;
            if (effectIndex == 0)
                BattleState.Draw(_state.Player, 2);
            if (effectIndex == 1)
                _state.TotalPlayPhasesThisTurn++;
            CloseMoralePopup();
            RefreshAllFromState();
            RefreshMoraleIcons();
        }

        private static void RefreshMoraleIcons()
        {
            if (_state == null || _playerMoraleIcons == null) return;
            int m = Mathf.Min(2, _state.Player.Morale);
            for (int i = 0; i < 2; i++)
            {
                if (_playerMoraleIcons[i] != null)
                    _playerMoraleIcons[i].color = i < m ? new Color(0.9f, 0.75f, 0.2f, 1f) : new Color(0.4f, 0.4f, 0.45f, 1f);
            }
            var pBtn = _playerMoraleRoot != null ? _playerMoraleRoot.GetComponent<Button>() : null;
            if (pBtn != null) pBtn.interactable = _state.Player.Morale > 0;
        }

        private static void CloseMoralePopup()
        {
            if (_moralePopupRoot != null) _moralePopupRoot.SetActive(false);
        }

        private static void BuildDiscardPhasePopup()
        {
            _discardPhasePopupRoot = new GameObject("DiscardPhasePopup");
            _discardPhasePopupRoot.transform.SetParent(_root.transform, false);
            _discardPhasePopupRoot.SetActive(false);
            SetFullRect(_discardPhasePopupRoot.AddComponent<RectTransform>());
            var canvas = _discardPhasePopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 45;
            _discardPhasePopupRoot.AddComponent<GraphicRaycaster>();
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_discardPhasePopupRoot.transform, false);
            var ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.6f);
            ovImg.raycastTarget = true;
            SetFullRect(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Button>().transition = Selectable.Transition.None;
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_discardPhasePopupRoot.transform, false);
            var panelR = panel.AddComponent<RectTransform>();
            panelR.anchorMin = new Vector2(0.5f, 0.5f);
            panelR.anchorMax = new Vector2(0.5f, 0.5f);
            panelR.pivot = new Vector2(0.5f, 0.5f);
            panelR.anchoredPosition = Vector2.zero;
            panelR.sizeDelta = new Vector2(1000, 420);
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleR = titleGo.AddComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0.5f, 1f);
            titleR.anchorMax = new Vector2(0.5f, 1f);
            titleR.pivot = new Vector2(0.5f, 1f);
            titleR.anchoredPosition = new Vector2(0, -16);
            titleR.sizeDelta = new Vector2(600, 36);
            _discardPhaseTitle = CreateGameText(titleGo.transform, "请弃置0张牌", 28);
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollR = scrollGo.AddComponent<RectTransform>();
            scrollR.anchorMin = new Vector2(0f, 0f);
            scrollR.anchorMax = new Vector2(1f, 1f);
            scrollR.offsetMin = new Vector2(16, 56);
            scrollR.offsetMax = new Vector2(-16, -56);
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpR = viewport.AddComponent<RectTransform>();
            vpR.anchorMin = Vector2.zero;
            vpR.anchorMax = Vector2.one;
            vpR.offsetMin = Vector2.zero;
            vpR.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentR = content.AddComponent<RectTransform>();
            contentR.anchorMin = new Vector2(0f, 1f);
            contentR.anchorMax = new Vector2(1f, 1f);
            contentR.pivot = new Vector2(0f, 1f);
            contentR.anchoredPosition = Vector2.zero;
            contentR.sizeDelta = new Vector2(0, 120);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content = contentR;
            sr.viewport = vpR;
            sr.horizontal = true;
            sr.vertical = false;
            _discardPhaseContent = content.transform;
            var confirmGo = new GameObject("Confirm");
            confirmGo.transform.SetParent(panel.transform, false);
            var confirmR = confirmGo.AddComponent<RectTransform>();
            confirmR.anchorMin = new Vector2(0.5f, 0f);
            confirmR.anchorMax = new Vector2(0.5f, 0f);
            confirmR.pivot = new Vector2(0.5f, 0f);
            confirmR.anchoredPosition = new Vector2(0, 12);
            confirmR.sizeDelta = new Vector2(160, 40);
            confirmGo.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.5f, 1f);
            _discardPhaseConfirmBtn = confirmGo.AddComponent<Button>();
            _discardPhaseConfirmBtn.onClick.AddListener(OnDiscardPhaseConfirm);
            var ct = CreateGameText(confirmGo.transform, "确认", 22);
            SetFullRect(ct.GetComponent<RectTransform>());
        }

        private static void OpenDiscardPhasePopup(bool isPlayer, int needCount)
        {
            _discardPhaseIsPlayer = isPlayer;
            _discardPhaseNeedCount = needCount;
            _discardPhaseSelectedIndices.Clear();
            var side = isPlayer ? _state.Player : _state.Opponent;
            _discardPhasePopupRoot.SetActive(true);
            if (_discardPhaseTitle != null)
                _discardPhaseTitle.text = "请弃置" + needCount + "张牌";
            foreach (Transform t in _discardPhaseContent)
                Object.Destroy(t.gameObject);
            float cardW = 80f;
            float cardH = cardW * CardAspectH / CardAspectW;
            for (int i = 0; i < side.Hand.Count; i++)
            {
                int index = i;
                var pc = side.Hand[i];
                var item = new GameObject("HandItem");
                item.transform.SetParent(_discardPhaseContent, false);
                var le = item.AddComponent<LayoutElement>();
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;
                var img = item.AddComponent<Image>();
                img.color = new Color(0.22f, 0.26f, 0.32f, 1f);
                var btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => ToggleDiscardPhaseSelection(index, img));
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(item.transform, false);
                var label = CreateGameText(labelGo.transform, pc.DisplayName, 16);
                SetFullRect(label.GetComponent<RectTransform>());
            }
        }

        private static void ToggleDiscardPhaseSelection(int index, Image img)
        {
            if (_discardPhaseSelectedIndices.Contains(index))
            {
                _discardPhaseSelectedIndices.Remove(index);
                img.color = new Color(0.22f, 0.26f, 0.32f, 1f);
            }
            else if (_discardPhaseSelectedIndices.Count < _discardPhaseNeedCount)
            {
                _discardPhaseSelectedIndices.Add(index);
                img.color = new Color(0.4f, 0.6f, 0.9f, 1f);
            }
        }

        private static void OnDiscardPhaseConfirm()
        {
            if (_discardPhaseSelectedIndices.Count != _discardPhaseNeedCount)
            {
                ToastUI.Show("弃牌数量不对，请重新选择");
                return;
            }
            _discardPhasePopupRoot.SetActive(false);
            BattlePhaseManager.NotifyDiscardPhaseDone(_discardPhaseIsPlayer, _discardPhaseSelectedIndices.ToArray());
            RefreshAllFromState();
        }

        private const float CharacterAreaTotalWidth = 788f;

        private static void BuildOpponentGenerals()
        {
            float cardW = 140f;
            float cardH = cardW * CardAspectH / CardAspectW;
            float gap = 24f;
            float visibleH = cardH * 0.5f;
            var area = new GameObject("OpponentCharacterArea");
            area.transform.SetParent(_root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0.5f, 1f - 0.32f);
            areaRect.anchorMax = new Vector2(0.5f, 1f);
            areaRect.pivot = new Vector2(0.5f, 1f);
            areaRect.anchoredPosition = Vector2.zero;
            areaRect.sizeDelta = new Vector2(CharacterAreaTotalWidth, 0f);
            var areaHlg = area.AddComponent<HorizontalLayoutGroup>();
            areaHlg.spacing = 0f;
            areaHlg.childForceExpandWidth = false;
            areaHlg.childControlWidth = true;
            areaHlg.childControlHeight = true;

            var oppSpacer = new GameObject("OpponentDeckColumnSpacer");
            oppSpacer.transform.SetParent(area.transform, false);
            var oppSpacerLe = oppSpacer.AddComponent<LayoutElement>();
            oppSpacerLe.preferredWidth = 240f;
            oppSpacerLe.flexibleWidth = 0;

            _opponentHpRoot = new GameObject("OpponentHpBar");
            _opponentHpRoot.transform.SetParent(area.transform, false);
            var ohpLe = _opponentHpRoot.AddComponent<LayoutElement>();
            ohpLe.preferredWidth = HpBarWidth;
            ohpLe.preferredHeight = HpBarHeight;
            ohpLe.flexibleWidth = 0;
            var ohpBg = new GameObject("HpBg");
            ohpBg.transform.SetParent(_opponentHpRoot.transform, false);
            SetFullRect(ohpBg.AddComponent<RectTransform>());
            var ohpBgImg = ohpBg.AddComponent<Image>();
            ohpBgImg.color = Color.black;
            ohpBgImg.sprite = GetWhiteSprite();
            var ohpOutline = ohpBg.AddComponent<Outline>();
            ohpOutline.effectColor = Color.white;
            ohpOutline.effectDistance = new Vector2(2, 2);
            var ohpFillGo = new GameObject("HpFill");
            ohpFillGo.transform.SetParent(_opponentHpRoot.transform, false);
            SetFullRect(ohpFillGo.AddComponent<RectTransform>());
            var ohpFillImg = ohpFillGo.AddComponent<Image>();
            ohpFillImg.color = new Color(0.1f, 0.5f, 0.2f, 1f);
            ohpFillImg.sprite = GetWhiteSprite();
            ohpFillImg.type = Image.Type.Filled;
            ohpFillImg.fillMethod = Image.FillMethod.Vertical;
            ohpFillImg.fillOrigin = (int)Image.OriginVertical.Top;
            ohpFillImg.fillAmount = 1f;
            ohpFillImg.raycastTarget = false;
            _opponentHpFill = ohpFillImg;
            var ohpTextGo = new GameObject("HpText");
            ohpTextGo.transform.SetParent(_opponentHpRoot.transform, false);
            SetFullRect(ohpTextGo.AddComponent<RectTransform>());
            _opponentHpText = CreateGameText(ohpTextGo.transform, "30/30", 18);

            var container = new GameObject("OpponentGeneralsContainer");
            container.transform.SetParent(area.transform, false);
            var ocLe = container.AddComponent<LayoutElement>();
            ocLe.preferredWidth = 3f * (cardW + SkillButtonExtraWidth) + 2f * gap;
            ocLe.flexibleWidth = 0;
            var ocRect = container.GetComponent<RectTransform>();
            if (ocRect == null) ocRect = container.AddComponent<RectTransform>();
            var frameBg = new GameObject("Frame");
            frameBg.transform.SetParent(container.transform, false);
            var fRect = frameBg.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;
            var fImg = frameBg.AddComponent<Image>();
            fImg.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);
            fImg.raycastTarget = false;
            frameBg.AddComponent<LayoutElement>().ignoreLayout = true;
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = gap;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            for (int i = 0; i < 3; i++)
            {
                var go = CreateGeneralCardRowInContainer(container.transform, cardW, visibleH, cardH, true, i);
                if (go != null)
                    _opponentCardRows.Add(go);
            }
        }

        private const float HpBarWidth = 56f;
        private const float HpBarHeight = 100f;

        private static void BuildPlayerGenerals()
        {
            float cardW = 140f;
            float cardH = cardW * CardAspectH / CardAspectW;
            float gap = 24f;
            float visibleH = cardH * 0.5f;
            var area = new GameObject("PlayerCharacterArea");
            area.transform.SetParent(_root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0.5f, 0f);
            areaRect.anchorMax = new Vector2(0.5f, 0.32f);
            areaRect.pivot = new Vector2(0.5f, 0f);
            areaRect.anchoredPosition = Vector2.zero;
            areaRect.sizeDelta = new Vector2(CharacterAreaTotalWidth, 0f);
            var areaHlg = area.AddComponent<HorizontalLayoutGroup>();
            areaHlg.spacing = 0f;
            areaHlg.childForceExpandWidth = false;
            areaHlg.childControlWidth = true;
            areaHlg.childControlHeight = true;

            var playerSpacer = new GameObject("PlayerDeckColumnSpacer");
            playerSpacer.transform.SetParent(area.transform, false);
            var playerSpacerLe = playerSpacer.AddComponent<LayoutElement>();
            playerSpacerLe.preferredWidth = 240f;
            playerSpacerLe.flexibleWidth = 0;

            _playerHpRoot = new GameObject("PlayerHpBar");
            _playerHpRoot.transform.SetParent(area.transform, false);
            var hpLe = _playerHpRoot.AddComponent<LayoutElement>();
            hpLe.preferredWidth = HpBarWidth;
            hpLe.preferredHeight = HpBarHeight;
            hpLe.flexibleWidth = 0;
            var hpBg = new GameObject("HpBg");
            hpBg.transform.SetParent(_playerHpRoot.transform, false);
            SetFullRect(hpBg.AddComponent<RectTransform>());
            var hpBgImg = hpBg.AddComponent<Image>();
            hpBgImg.color = Color.black;
            hpBgImg.sprite = GetWhiteSprite();
            var hpOutline = hpBg.AddComponent<Outline>();
            hpOutline.effectColor = Color.white;
            hpOutline.effectDistance = new Vector2(2, 2);
            var hpFillGo = new GameObject("HpFill");
            hpFillGo.transform.SetParent(_playerHpRoot.transform, false);
            SetFullRect(hpFillGo.AddComponent<RectTransform>());
            var hpFillImg = hpFillGo.AddComponent<Image>();
            hpFillImg.color = new Color(0.1f, 0.5f, 0.2f, 1f);
            hpFillImg.sprite = GetWhiteSprite();
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Vertical;
            hpFillImg.fillOrigin = (int)Image.OriginVertical.Top;
            hpFillImg.fillAmount = 1f;
            hpFillImg.raycastTarget = false;
            _playerHpFill = hpFillImg;
            var hpTextGo = new GameObject("HpText");
            hpTextGo.transform.SetParent(_playerHpRoot.transform, false);
            SetFullRect(hpTextGo.AddComponent<RectTransform>());
            _playerHpText = CreateGameText(hpTextGo.transform, "30/30", 18);

            var container = new GameObject("PlayerGeneralsContainer");
            container.transform.SetParent(area.transform, false);
            var cLe = container.AddComponent<LayoutElement>();
            cLe.preferredWidth = 3f * (cardW + SkillButtonExtraWidth) + 2f * gap;
            cLe.flexibleWidth = 0;
            var cRect = container.GetComponent<RectTransform>();
            if (cRect == null) cRect = container.AddComponent<RectTransform>();
            var frameBg = new GameObject("Frame");
            frameBg.transform.SetParent(container.transform, false);
            var fRect = frameBg.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;
            var fImg = frameBg.AddComponent<Image>();
            fImg.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);
            fImg.raycastTarget = false;
            frameBg.AddComponent<LayoutElement>().ignoreLayout = true;
            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = gap;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            for (int i = 0; i < 3; i++)
            {
                var go = CreateGeneralCardRowInContainer(container.transform, cardW, visibleH, cardH, false, i);
                if (go != null)
                    _playerCardRows.Add(go);
            }
        }

        private const float SkillButtonHeight = 58f;
        private const float SkillButtonSpacing = 20f;
        private const float SkillToCardGap = 20f;
        private const int MaxSkillButtons = 3;
        private const float CardAreaBorderInset = 4f;
        private const float SkillButtonExtraWidth = 8f;

        private static GameObject CreateGeneralCardRowInContainer(Transform parent, float cardW, float visibleH, float cardH, bool isOpponent, int index)
        {
            try
            {
                return CreateGeneralCardRowInContainerImpl(parent, cardW, visibleH, cardH, isOpponent, index);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("CreateGeneralCardRowInContainer failed: " + e.Message + "\n" + e.StackTrace);
                return null;
            }
        }

        private static GameObject CreateGeneralCardRowInContainerImpl(Transform parent, float cardW, float visibleH, float cardH, bool isOpponent, int index)
        {
            if (parent == null) return null;
            float skillAreaMaxH = MaxSkillButtons * SkillButtonHeight + (MaxSkillButtons - 1) * SkillButtonSpacing;
            float rowH = skillAreaMaxH + SkillToCardGap + visibleH;

            var row = new GameObject((isOpponent ? "Opponent" : "Player") + "General_" + index);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredWidth = cardW + SkillButtonExtraWidth;
            rowLe.preferredHeight = rowH;
            rowLe.flexibleWidth = 0;

            var rowVlg = row.AddComponent<VerticalLayoutGroup>();
            rowVlg.spacing = SkillButtonSpacing;
            rowVlg.childForceExpandHeight = false;
            rowVlg.childControlHeight = true;
            rowVlg.childControlWidth = true;
            rowVlg.childForceExpandWidth = false;

            var skillContainer = new GameObject("SkillButtons");
            skillContainer.transform.SetParent(row.transform, false);
            var skillVlg = skillContainer.AddComponent<VerticalLayoutGroup>();
            skillVlg.spacing = SkillButtonSpacing;
            skillVlg.childForceExpandHeight = false;
            skillVlg.childControlHeight = true;
            skillVlg.childControlWidth = true;
            var skillLe = skillContainer.AddComponent<LayoutElement>();
            skillLe.preferredHeight = 0;
            skillLe.flexibleHeight = 0;
            skillContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            var spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.preferredHeight = SkillToCardGap;
            spacerLe.flexibleHeight = 0;

            var cardPart = new GameObject("CardPart");
            cardPart.transform.SetParent(row.transform, false);
            var cardPartLe = cardPart.AddComponent<LayoutElement>();
            cardPartLe.preferredHeight = visibleH;
            cardPartLe.flexibleHeight = 0;
            var cardPartBorder = cardPart.AddComponent<Image>();
            cardPartBorder.color = new Color(0.18f, 0.18f, 0.22f, 0.98f);
            cardPartBorder.sprite = GetWhiteSprite();
            cardPartBorder.raycastTarget = false;

            var slotBg = new GameObject("SlotBg");
            slotBg.transform.SetParent(cardPart.transform, false);
            var slotBgRect = slotBg.AddComponent<RectTransform>();
            slotBgRect.anchorMin = Vector2.zero;
            slotBgRect.anchorMax = Vector2.one;
            slotBgRect.offsetMin = new Vector2(CardAreaBorderInset, CardAreaBorderInset);
            slotBgRect.offsetMax = new Vector2(-CardAreaBorderInset, -CardAreaBorderInset);
            var slotBgImg = slotBg.AddComponent<Image>();
            slotBgImg.color = new Color(0.38f, 0.4f, 0.46f, 0.98f);
            slotBgImg.sprite = GetWhiteSprite();
            slotBgImg.raycastTarget = false;

            var maskGo = new GameObject("Mask");
            maskGo.transform.SetParent(cardPart.transform, false);
            var maskRect = maskGo.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            maskGo.AddComponent<Image>().color = Color.clear;

            var cardAnchor = new GameObject("CardAnchor");
            cardAnchor.transform.SetParent(maskGo.transform, false);
            var anchorRect = cardAnchor.AddComponent<RectTransform>();
            anchorRect.anchorMin = new Vector2(0f, isOpponent ? 0f : 1f);
            anchorRect.anchorMax = new Vector2(1f, isOpponent ? 0f : 1f);
            anchorRect.pivot = new Vector2(0.5f, isOpponent ? 0f : 1f);
            anchorRect.offsetMin = Vector2.zero;
            anchorRect.offsetMax = Vector2.zero;
            anchorRect.sizeDelta = new Vector2(0, cardH);

            var block = cardPart.AddComponent<Image>();
            block.color = Color.clear;
            var holder = row.AddComponent<GeneralCardHolder>();
            if (holder == null)
            {
                Object.Destroy(row);
                return null;
            }
            holder.CardSlot = cardAnchor.transform;
            holder.CardIndex = index;
            holder.IsPlayer = !isOpponent;
            var skillButtons = new List<Button>(MaxSkillButtons);
            var skillLabels = new List<TextMeshProUGUI>(MaxSkillButtons);
            for (int i = 0; i < MaxSkillButtons; i++)
            {
                int skillIndex = i;
                var btnGo = new GameObject("SkillBtn_" + i);
                btnGo.transform.SetParent(skillContainer.transform, false);
                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = SkillButtonHeight;
                btnLe.preferredWidth = cardW + SkillButtonExtraWidth;
                btnLe.flexibleWidth = 0;
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.4f, 0.4f, 0.45f, 1f);
                btnImg.sprite = GetWhiteSprite();
                var labelT = CreateGameText(btnGo.transform, "", 16);
                var labelRect = labelT != null ? labelT.GetComponent<RectTransform>() : null;
                if (labelRect != null)
                    SetFullRect(labelRect);
                if (labelT != null)
                    labelT.color = new Color(1f, 1f, 1f, 1f);
                skillLabels.Add(labelT);
                var btn = btnGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = btnImg;
                var colors = btn.colors;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.45f, 1f);
                colors.normalColor = new Color(0.25f, 0.5f, 0.9f, 1f);
                colors.highlightedColor = new Color(0.35f, 0.6f, 1f, 1f);
                colors.pressedColor = new Color(0.2f, 0.45f, 0.85f, 1f);
                btn.colors = colors;
                btn.interactable = false;
                btnGo.SetActive(false);
                btn.onClick.AddListener(() => holder.OnSkillButtonClick(skillIndex));
                skillButtons.Add(btn);
            }
            holder.SkillButtons = skillButtons;
            holder.SkillButtonLabels = skillLabels;
            var rowBtn = row.AddComponent<Button>();
            rowBtn.targetGraphic = block;
            rowBtn.onClick.AddListener(() => holder.OnCardClick());
            if (isOpponent)
                block.raycastTarget = true;
            return row;
        }

        private static void BuildCardEnlargeOverlay()
        {
            _cardEnlargeRoot = new GameObject("CardEnlargeOverlay");
            _cardEnlargeRoot.transform.SetParent(_root.transform, false);
            _cardEnlargeRoot.SetActive(false);
            SetFullRect(_cardEnlargeRoot.AddComponent<RectTransform>());
            var canvas = _cardEnlargeRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            _cardEnlargeRoot.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_cardEnlargeRoot.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            bgImg.raycastTarget = true;
            SetFullRect(bg.GetComponent<RectTransform>());
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(HideCardEnlarge);

            var center = new GameObject("Center");
            center.transform.SetParent(_cardEnlargeRoot.transform, false);
            var centerRect = center.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            float w = 320f;
            centerRect.sizeDelta = new Vector2(w, w * CardAspectH / CardAspectW);
            _cardEnlargeImage = center.AddComponent<Image>();
            _cardEnlargeImage.preserveAspect = true;
            _cardEnlargeImage.color = Color.white;
            var centerBtn = center.AddComponent<Button>();
            centerBtn.transition = Selectable.Transition.None;
            centerBtn.onClick.AddListener(HideCardEnlarge);
        }

        private static void BuildDiscardPopup()
        {
            _discardPopupRoot = new GameObject("DiscardPopup");
            _discardPopupRoot.transform.SetParent(_root.transform, false);
            _discardPopupRoot.SetActive(false);
            SetFullRect(_discardPopupRoot.AddComponent<RectTransform>());
            var canvas = _discardPopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;
            _discardPopupRoot.AddComponent<GraphicRaycaster>();

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_discardPopupRoot.transform, false);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.6f);
            overlayImg.raycastTarget = true;
            SetFullRect(overlay.GetComponent<RectTransform>());
            var overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(CloseDiscardPopup);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_discardPopupRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1000, 420);
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(400, 36);
            _discardPopupTitle = CreateGameText(titleGo.transform, "弃牌堆 (0)", 28);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(16, 16);
            scrollRect.offsetMax = new Vector2(-16, -56);
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 120);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = viewportRect;
            sr.horizontal = true;
            sr.vertical = false;

            _discardPopupContent = content.transform;
        }

        private static void RegisterCardEnlarge()
        {
            OnRequestCardEnlarge = (cardId, isPlayerSide) =>
            {
                CompendiumUI.ShowCardDetailByCardId(cardId ?? "");
            };
        }

        public static void RequestCardEnlarge(string cardId, bool isPlayerSide)
        {
            OnRequestCardEnlarge?.Invoke(cardId, isPlayerSide);
        }

        private static void HideCardEnlarge()
        {
            if (_cardEnlargeRoot != null)
                _cardEnlargeRoot.SetActive(false);
        }

        private static void OnEndTurn()
        {
            BattlePhaseManager.EndTurn();
        }

        private static void RefreshTurnButton()
        {
            if (_turnButton == null || _turnButtonText == null) return;
            bool myTurn = _state != null && _state.IsPlayerTurn;
            if (!myTurn)
            {
                _turnButtonText.text = "对手回合";
                _turnButton.interactable = false;
                return;
            }
            if (_state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _turnButtonText.text = "结束主要阶段";
                _turnButton.interactable = true;
                return;
            }
            if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _turnButtonText.text = "结束当前出牌阶段";
                _turnButton.interactable = true;
                return;
            }
            _turnButtonText.text = "你的回合";
            _turnButton.interactable = false;
        }

        private static void OpenDiscardPopup(bool isPlayer)
        {
            if (_discardPopupRoot == null || _state == null) return;
            var pile = isPlayer ? _state.Player.DiscardPile : _state.Opponent.DiscardPile;
            _discardPopupRoot.SetActive(true);
            int n = pile.Count;
            if (_discardPopupTitle != null) _discardPopupTitle.text = "弃牌堆 (" + n + ")";
            foreach (Transform t in _discardPopupContent)
                Object.Destroy(t.gameObject);
            float cardW = 80f;
            float cardH = cardW * CardAspectH / CardAspectW;
            for (int i = 0; i < pile.Count; i++)
            {
                var pc = pile[i];
                var item = new GameObject("DiscardItem");
                item.transform.SetParent(_discardPopupContent, false);
                var le = item.AddComponent<LayoutElement>();
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;
                item.AddComponent<Image>().color = new Color(0.22f, 0.26f, 0.32f, 1f);
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(item.transform, false);
                var label = CreateGameText(labelGo.transform, pc.DisplayName, 16);
                SetFullRect(label.GetComponent<RectTransform>());
            }
        }

        private static void CloseDiscardPopup()
        {
            if (_discardPopupRoot != null)
                _discardPopupRoot.SetActive(false);
        }

        public static void StartGame(DeckData selectedDeck)
        {
            CardTableLoader.Load();
            _cachedPlayerHandCount = -1;
            _cachedOpponentHandCount = -1;
            if (_root == null) Create();
            _state = new BattleState();
            _state.InitFromDecks(selectedDeck, selectedDeck);
            BattlePhaseManager.Bind(_state);
            BattlePhaseManager.OnDiscardMain += OnDiscardPhaseRequest;
            BattlePhaseManager.OnGameStart();
            DeckSelectUI.Hide();
            _root.SetActive(true);
            RefreshAllFromState();
        }

        private static void OnDiscardPhaseRequest(bool isPlayer, int needCount)
        {
            if (isPlayer)
                OpenDiscardPhasePopup(true, needCount);
            else
                BattlePhaseManager.NotifyDiscardPhaseDone(false, null);
        }

        private static void RefreshAllFromState()
        {
            RefreshTurnButton();
            RefreshPhaseLabel();
            RefreshDeckTooltip();
            RefreshDiscardLabel();
            RefreshHpDisplay();
            RefreshGeneralCards();
            RefreshMoraleIcons();
            RefreshHandCards();
            RefreshPlayedCards();
        }

        private static void RefreshHpDisplay()
        {
            if (_state == null) return;
            int pCur = _state.Player.CurrentHp;
            int pMax = _state.Player.MaxHp;
            int oCur = _state.Opponent.CurrentHp;
            int oMax = _state.Opponent.MaxHp;
            if (_playerHpText != null) _playerHpText.text = pCur + "/" + pMax;
            if (_playerHpFill != null) _playerHpFill.fillAmount = pMax > 0 ? Mathf.Clamp01((float)pCur / pMax) : 0f;
            if (_opponentHpText != null) _opponentHpText.text = oCur + "/" + oMax;
            if (_opponentHpFill != null) _opponentHpFill.fillAmount = oMax > 0 ? Mathf.Clamp01((float)oCur / oMax) : 0f;
        }

        private static void RefreshPhaseLabel()
        {
            if (_phaseLabel == null) return;
            if (_state == null)
            {
                _phaseLabel.text = "—";
                return;
            }
            _phaseLabel.text = GetPhaseDisplayName(_state.CurrentPhase);
        }

        private static string GetPhaseDisplayName(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Preparation: return "准备阶段";
                case BattlePhase.Income: return "收入阶段";
                case BattlePhase.Primary: return "主要阶段";
                case BattlePhase.Main: return "出牌阶段";
                case BattlePhase.Discard: return "弃牌阶段";
                case BattlePhase.TurnEnd: return "回合结束";
                default: return phase.ToString();
            }
        }

        private static void RefreshDeckTooltip()
        {
            if (_state == null) return;
            if (_deckCountTooltip != null)
                _deckCountTooltip.text = "牌堆：" + _state.Player.Deck.Count + " 张";
            if (_opponentDeckCountTooltip != null)
                _opponentDeckCountTooltip.text = "牌堆：" + _state.Opponent.Deck.Count + " 张";
        }

        private static void RefreshDiscardLabel()
        {
            if (_state == null) return;
            if (_discardButtonLabel != null)
                _discardButtonLabel.text = "弃牌堆 (" + _state.Player.DiscardPile.Count + ")";
            if (_opponentDiscardButtonLabel != null)
                _opponentDiscardButtonLabel.text = "弃牌堆 (" + _state.Opponent.DiscardPile.Count + ")";
        }

        private static void RefreshGeneralCards()
        {
            if (_state == null) return;
            for (int i = 0; i < _playerCardRows.Count; i++)
            {
                var holder = _playerCardRows[i].GetComponent<GeneralCardHolder>();
                if (holder != null)
                    holder.SetCardId(i < _state.Player.GeneralCardIds.Count ? _state.Player.GeneralCardIds[i] : "");
            }
            for (int i = 0; i < _opponentCardRows.Count; i++)
            {
                var holder = _opponentCardRows[i].GetComponent<GeneralCardHolder>();
                if (holder != null)
                    holder.SetCardId(i < _state.Opponent.GeneralCardIds.Count ? _state.Opponent.GeneralCardIds[i] : "");
            }
        }

        /// <summary> UI Image 无 sprite 时不绘制，需用白图占位才能显示纯色。 </summary>
        public static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(2, 2);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
        private static Sprite _whiteSprite;
        private static Sprite _hexagonSprite;

        /// <summary> 生成正六边形 Sprite（同士气图标大小感），用于士气槽。 </summary>
        public static Sprite GetHexagonSprite()
        {
            if (_hexagonSprite != null) return _hexagonSprite;
            const int size = 64;
            const float cx = size * 0.5f;
            const float cy = size * 0.5f;
            const float r = size * 0.44f;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, Color.clear);
            float[] vx = new float[6], vy = new float[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f - 90f) * Mathf.Deg2Rad;
                vx[i] = cx + r * Mathf.Cos(a);
                vy[i] = cy + r * Mathf.Sin(a);
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (PointInPolygon(x + 0.5f, y + 0.5f, vx, vy))
                        tex.SetPixel(x, y, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _hexagonSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _hexagonSprite;
        }
        private static bool PointInPolygon(float px, float py, float[] vx, float[] vy)
        {
            bool inside = false;
            int n = vx.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((vy[i] > py) != (vy[j] > py)) && (px < (vx[j] - vx[i]) * (py - vy[i]) / (vy[j] - vy[i]) + vx[i]))
                    inside = !inside;
            }
            return inside;
        }
        private static Sprite _semicircleSprite;
        private static Sprite GetSemicircleSprite()
        {
            if (_semicircleSprite != null) return _semicircleSprite;
            const int size = 64;
            const float cx = 0f;
            const float cy = size * 0.5f;
            const float r = size * 0.5f;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float dx = x - cx - 1;
                    float dy = y - cy;
                    bool inRightHalf = x >= cx && (dx * dx + dy * dy) <= r * r;
                    tex.SetPixel(x, y, inRightHalf ? Color.white : Color.clear);
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _semicircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f));
            return _semicircleSprite;
        }

        private const float HandContentWidth = 404f;
        private const float HandCardWidth = 72f;

        private static int _cachedPlayerHandCount = -1;
        private static int _cachedOpponentHandCount = -1;

        private static void RefreshHandCards()
        {
            if (_playerHandContent == null || _state == null) return;
            int playerCount = _state.Player.Hand.Count;
            int oppCount = _opponentHandContent != null ? _state.Opponent.Hand.Count : 0;
            if (_playerHandLabel != null)
                _playerHandLabel.text = "手牌上限：" + _state.HandLimit + "/手牌数量：" + playerCount;
            if (_opponentHandLabel != null)
                _opponentHandLabel.text = "手牌上限：" + _state.HandLimit + "/手牌数量：" + oppCount;
            if (playerCount == _cachedPlayerHandCount && oppCount == _cachedOpponentHandCount)
                return;
            _cachedPlayerHandCount = playerCount;
            _cachedOpponentHandCount = oppCount;
            float cardH = 100f;
            var phlg = _playerHandContent.GetComponent<HorizontalLayoutGroup>();
            if (phlg != null) phlg.spacing = -36f;
            foreach (Transform t in _playerHandContent)
                Object.Destroy(t.gameObject);
            for (int i = 0; i < playerCount; i++)
            {
                int index = i;
                var pc = _state.Player.Hand[i];
                CreateHandCardItem(_playerHandContent, pc, HandCardWidth, cardH, index);
            }
            if (_opponentHandContent != null)
            {
                var ohlg = _opponentHandContent.GetComponent<HorizontalLayoutGroup>();
                if (ohlg != null) ohlg.spacing = -36f;
                foreach (Transform t in _opponentHandContent)
                    Object.Destroy(t.gameObject);
                for (int i = 0; i < oppCount; i++)
                {
                    var pc = _state.Opponent.Hand[i];
                    CreateHandCardItem(_opponentHandContent, pc, HandCardWidth, cardH, i, false);
                }
            }
        }

        private static Sprite _cardBackSprite;
        private static Sprite GetCardBackSprite()
        {
            if (_cardBackSprite != null) return _cardBackSprite;
            _cardBackSprite = Resources.Load<Sprite>("Cards/Back");
            if (_cardBackSprite == null)
            {
                var tex = Resources.Load<Texture2D>("Cards/Back");
                if (tex != null) _cardBackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return _cardBackSprite;
        }

        private static GameObject CreateHandCardItem(Transform parent, PokerCard pc, float w, float h, int handIndex, bool isPlayer = true)
        {
            var go = new GameObject("HandCard_" + handIndex);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            var goImg = go.AddComponent<Image>();
            goImg.color = Color.clear;
            goImg.raycastTarget = isPlayer;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var vRect = visual.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            var border = new GameObject("Border");
            border.transform.SetParent(visual.transform, false);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);
            var borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            borderImg.sprite = GetWhiteSprite();
            var fill = new GameObject("Fill");
            fill.transform.SetParent(visual.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fill.AddComponent<Image>();
            if (isPlayer)
            {
                fillImg.color = Color.white;
                fillImg.sprite = GetWhiteSprite();
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(visual.transform, false);
                var label = CreateGameText(labelGo.transform, pc.DisplayName, 18);
                label.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                SetFullRect(label.GetComponent<RectTransform>());
            }
            else
            {
                fillImg.color = Color.white;
                fillImg.sprite = GetCardBackSprite();
                if (fillImg.sprite == null) fillImg.sprite = GetWhiteSprite();
                fillImg.preserveAspect = true;
            }
            go.AddComponent<HandCardHover>();
            if (isPlayer)
            {
                var drag = go.AddComponent<HandCardDragDrop>();
                drag.HandIndex = handIndex;
            }
            return go;
        }

        private static void RefreshPlayedCards()
        {
            if (_playedZoneContent == null || _state == null) return;
            foreach (Transform t in _playedZoneContent)
                Object.Destroy(t.gameObject);
            float cardW = 72f;
            float cardH = 100f;
            for (int i = 0; i < _state.Player.PlayedThisPhase.Count; i++)
            {
                int playedIndex = i;
                var pc = _state.Player.PlayedThisPhase[i];
                var go = new GameObject("PlayedCard_" + playedIndex);
                go.transform.SetParent(_playedZoneContent, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;
                go.AddComponent<Image>().color = Color.clear;
                go.GetComponent<Image>().raycastTarget = true;
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                var vRect = visual.AddComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.offsetMin = vRect.offsetMax = Vector2.zero;
                var border = new GameObject("Border");
                border.transform.SetParent(visual.transform, false);
                var borderRect = border.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = new Vector2(-2, -2);
                borderRect.offsetMax = new Vector2(2, 2);
                var borderImgP = border.AddComponent<Image>();
                borderImgP.color = new Color(0.4f, 0.4f, 0.45f, 1f);
                borderImgP.sprite = GetWhiteSprite();
                var fill = new GameObject("Fill");
                fill.transform.SetParent(visual.transform, false);
                var fillRect = fill.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(2, 2);
                fillRect.offsetMax = new Vector2(-2, -2);
                var fillImgP = fill.AddComponent<Image>();
                fillImgP.color = Color.white;
                fillImgP.sprite = GetWhiteSprite();
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(visual.transform, false);
                var lbl = CreateGameText(labelGo.transform, pc.DisplayName, 18);
                lbl.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                SetFullRect(lbl.GetComponent<RectTransform>());
                go.AddComponent<HandCardHover>();
                var btn = go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => ReturnPlayedCardToHand(playedIndex));
            }
        }

        private static void ReturnPlayedCardToHand(int playedIndex)
        {
            if (_state == null || playedIndex < 0 || playedIndex >= _state.Player.PlayedThisPhase.Count) return;
            var card = _state.Player.PlayedThisPhase[playedIndex];
            _state.Player.PlayedThisPhase.RemoveAt(playedIndex);
            _state.Player.Hand.Add(card);
            RefreshAllFromState();
        }

        /// <summary> 出牌阶段中，将手牌从手牌区移到已打出区。每段出牌阶段最多打出 MaxPlayPerPhase 张。 </summary>
        public static void MoveHandCardToPlayedZone(int handIndex)
        {
            if (_state == null || !_state.IsPlayerTurn || _state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main) return;
            if (_state.Player.PlayedThisPhase.Count >= BattleState.MaxPlayPerPhase) return;
            if (handIndex < 0 || handIndex >= _state.Player.Hand.Count) return;
            var card = _state.Player.Hand[handIndex];
            _state.Player.Hand.RemoveAt(handIndex);
            _state.Player.PlayedThisPhase.Add(card);
            RefreshAllFromState();
        }

        public static void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }
    }

    public class GeneralCardHolder : MonoBehaviour
    {
        /// <summary> 卡牌预制体实例挂载点，Refresh 时清空并放入 CardView.InstantiateCardSlot(data)。 </summary>
        public Transform CardSlot;
        public int CardIndex;
        public bool IsPlayer;
        /// <summary> 技能按钮（最多 3 个），由 CreateGeneralCardRowInContainer 赋值。 </summary>
        public List<Button> SkillButtons;
        /// <summary> 技能按钮上的名称文本，与 SkillButtons 一一对应。 </summary>
        public List<TextMeshProUGUI> SkillButtonLabels;
        /// <summary> 点击技能按钮时回调：参数为 (卡牌槽索引, 技能索引 0/1/2)。目前仅留接口，暂不做技能逻辑。 </summary>
        public static System.Action<int, int> OnSkillButtonClicked;
        private string _cardId;

        /// <summary> 设置显示的技能按钮数量（0～3）、技能名称，并置灰；后续可用 SetSkillButtonState 单独设高亮。skillNames 可为 null 或长度 3。 </summary>
        public void SetSkillCount(int count, string[] skillNames = null)
        {
            if (SkillButtons == null) return;
            int c = Mathf.Clamp(count, 0, SkillButtons.Count);
            for (int i = 0; i < SkillButtons.Count; i++)
            {
                var btn = SkillButtons[i];
                if (btn == null) continue;
                btn.gameObject.SetActive(i < c);
                if (SkillButtonLabels != null && i < SkillButtonLabels.Count && SkillButtonLabels[i] != null)
                    SkillButtonLabels[i].text = (skillNames != null && i < skillNames.Length) ? (skillNames[i] ?? "") : "";
                SetSkillButtonState(i, false);
            }
        }

        /// <summary> 设置某个技能按钮是否高亮可点。true=高亮可点，false=置灰不可点。 </summary>
        public void SetSkillButtonState(int index, bool enabled)
        {
            if (SkillButtons == null || index < 0 || index >= SkillButtons.Count) return;
            var btn = SkillButtons[index];
            if (btn == null) return;
            btn.interactable = enabled;
            var img = btn.targetGraphic as Image;
            if (img != null)
                img.color = enabled ? new Color(0.25f, 0.5f, 0.9f, 1f) : new Color(0.4f, 0.4f, 0.45f, 1f);
        }

        /// <summary> 技能按钮点击时调用，仅留接口。 </summary>
        public void OnSkillButtonClick(int skillIndex)
        {
            OnSkillButtonClicked?.Invoke(CardIndex, skillIndex);
        }

        public void SetCardId(string cardId)
        {
            _cardId = NormalizeCardId(cardId ?? "");
            if (CardSlot == null) return;
            for (int i = CardSlot.childCount - 1; i >= 0; i--)
                Object.Destroy(CardSlot.GetChild(i).gameObject);
            SetSkillCount(0);
            int skillCount = 0;
            if (!string.IsNullOrEmpty(_cardId))
            {
                var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(_cardId));
                if (data != null)
                    skillCount = data.SkillCount;
                if (data == null)
                {
                    var placeGo = new GameObject("Placeholder");
                    placeGo.transform.SetParent(CardSlot, false);
                    var placeRect = placeGo.AddComponent<RectTransform>();
                    placeRect.anchorMin = Vector2.zero;
                    placeRect.anchorMax = Vector2.one;
                    placeRect.offsetMin = placeRect.offsetMax = Vector2.zero;
                    var placeImg = placeGo.AddComponent<Image>();
                    placeImg.sprite = GameUI.GetWhiteSprite();
                    placeImg.color = new Color(0.3f, 0.32f, 0.38f, 1f);
                    SetSkillCount(0, null);
                    return;
                }
                string[] skillNames = new[] { data.SkillName1 ?? "", data.SkillName2 ?? "", data.SkillName3 ?? "" };
                // 与图鉴完全一致：优先预制体，否则用相同 fallback 结构 + CardView.LoadFaceSprite()，保证同一套加载路径
                var go = CardView.InstantiateCardSlot(data);
                if (go != null)
                {
                    go.transform.SetParent(CardSlot, false);
                    var rect = go.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                    }
                    SetSkillCount(skillCount, skillNames);
                    return;
                }
                var root = new GameObject("Card_" + data.CardId);
                root.transform.SetParent(CardSlot, false);
                var rootRect = root.AddComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
                var face = new GameObject("Face");
                face.transform.SetParent(root.transform, false);
                var faceRect = face.AddComponent<RectTransform>();
                faceRect.anchorMin = new Vector2(0.5f, 0.5f);
                faceRect.anchorMax = new Vector2(0.5f, 0.5f);
                faceRect.pivot = new Vector2(0.5f, 0.5f);
                faceRect.anchoredPosition = Vector2.zero;
                faceRect.sizeDelta = new Vector2(100f, 100f * 1488f / 1016f);
                var img = face.AddComponent<Image>();
                img.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                var view = root.AddComponent<CardView>();
                view.Data = data;
                view.FaceImage = img;
                view.LoadFaceSprite();
                SetSkillCount(skillCount, skillNames);
            }
        }

        /// <summary> 将 "1"、"001"、"NO001" 等统一为 "NO001" 格式，便于加载与表格查询。 </summary>
        private static string NormalizeCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return "";
            string s = cardId.Trim();
            if (s.StartsWith("NO", System.StringComparison.OrdinalIgnoreCase) && s.Length >= 5)
                return s;
            if (int.TryParse(s, out int n) && n >= 1)
                return "NO" + n.ToString("D3");
            return s;
        }

        public void OnCardClick()
        {
            if (!string.IsNullOrEmpty(_cardId))
                GameUI.RequestCardEnlarge(_cardId, IsPlayer);
        }
    }

    public class DeckTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject TooltipRoot;
        public TextMeshProUGUI CountLabel;
        public System.Func<int> GetCount;

        public void OnPointerEnter(PointerEventData eventData) { Show(); }
        public void OnPointerExit(PointerEventData eventData) { Hide(); }
        public void Show()
        {
            if (TooltipRoot != null) TooltipRoot.SetActive(true);
            if (CountLabel != null && GetCount != null) CountLabel.text = "牌堆：" + GetCount() + " 张";
        }
        public void Hide() { if (TooltipRoot != null) TooltipRoot.SetActive(false); }
    }

    /// <summary> 手牌/打出牌悬停时克隆外观到顶层 overlay 显示，不移动原牌、不触发布局，实现全展示且不抽搐。 </summary>
    public class HandCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject _hoverClone;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hoverClone != null) return;
            var overlay = GameUI.GetHandHoverOverlay();
            if (overlay == null) return;
            Transform visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (visual == null) return;
            _hoverClone = Object.Instantiate(visual.gameObject);
            _hoverClone.name = "HoverClone";
            _hoverClone.transform.SetParent(overlay, false);
            var cardRect = transform as RectTransform;
            var cloneRect = _hoverClone.GetComponent<RectTransform>();
            if (cardRect != null && cloneRect != null)
            {
                cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
                cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
                cloneRect.pivot = new Vector2(0.5f, 0.5f);
                cloneRect.position = cardRect.position;
                cloneRect.sizeDelta = cardRect.sizeDelta;
            }
            var cg = _hoverClone.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.ignoreParentGroups = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoverClone != null)
            {
                Object.Destroy(_hoverClone);
                _hoverClone = null;
            }
        }

        private void OnDestroy()
        {
            if (_hoverClone != null) Object.Destroy(_hoverClone);
        }
    }

    /// <summary> 手牌拖到打出区时移入 PlayedThisPhase。仅出牌阶段生效。 </summary>
    public class HandCardDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int HandIndex;
        private GameObject _ghost;
        private RectTransform _rt;
        private Canvas _canvas;

        private void Awake()
        {
            _rt = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rt == null || _canvas == null) return;
            _ghost = new GameObject("HandCardGhost");
            _ghost.transform.SetParent(_canvas.transform, false);
            var ghostRt = _ghost.AddComponent<RectTransform>();
            ghostRt.sizeDelta = _rt.sizeDelta;
            var ghostImg = _ghost.AddComponent<Image>();
            ghostImg.color = Color.white;
            ghostImg.raycastTarget = false;
            var cg = _ghost.AddComponent<CanvasGroup>();
            cg.alpha = 0.9f;
            cg.blocksRaycasts = false;
            ghostRt.position = _rt.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null) (_ghost.transform as RectTransform).position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = eventData.position }, results);
            foreach (var r in results)
            {
                if (r.gameObject.GetComponent<PlayedZoneMarker>() != null)
                {
                    GameUI.MoveHandCardToPlayedZone(HandIndex);
                    return;
                }
            }
        }
    }

    /// <summary> 打出区标记，用于拖放手牌时检测落点。 </summary>
    public class PlayedZoneMarker : MonoBehaviour { }

}
