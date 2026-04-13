using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 选择套牌页：主菜单点击「开始游戏」后进入。展示牌组列表（与图鉴右侧顺序一致），单选后点「开始游戏」进入对局；「组牌」进入图鉴。
    /// </summary>
    public static class DeckSelectUI
    {
        private static GameObject _root;
        private static Transform _listContent;
        private static int _selectedDeckIndex = -1;
        private static List<GameObject> _deckRows = new List<GameObject>();
        private static Color _rowNormal = new Color(0.28f, 0.32f, 0.4f, 1f);
        private static Color _rowSelected = new Color(0.2f, 0.45f, 0.6f, 1f);
        private static TextMeshProUGUI _titleText;
        private static TextMeshProUGUI _confirmButtonText;
        private static System.Action<DeckData> _confirmSelectionOverride;
        private static System.Action _backOverride;
        private static string _titleOverride;
        private static string _confirmLabelOverride;

        public static void Create()
        {
            _root = new GameObject("DeckSelectPanel");
            _root.SetActive(false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1920, 1080);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_root.transform, false);
            bg.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);
            SetFullRect(bg.GetComponent<RectTransform>());

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root.transform, false);
            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "选择套牌开始游戏";
            if (TMPHelper.GetDefaultFont() != null) _titleText.font = TMPHelper.GetDefaultFont();
            _titleText.fontSize = 42;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color = Color.white;
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -50);
            titleRect.sizeDelta = new Vector2(600, 56);

            BuildDeckListArea();
            BuildBottomBar();
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void BuildDeckListArea()
        {
            var scrollGo = new GameObject("DeckScroll");
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.2f, 0.18f);
            scrollRect.anchorMax = new Vector2(0.8f, 0.72f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 400);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content = contentRect;
            sr.viewport = viewportRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 20f;

            _listContent = content.transform;
        }

        private static void BuildBottomBar()
        {
            float barY = 70f;
            float btnH = 48f;
            float btnW = 160f;
            float gap = 24f;

            var backBtn = CreateBarButton("返回", -btnW - gap, barY, btnW, btnH);
            backBtn.onClick.AddListener(OnBack);

            var compendiumBtn = CreateBarButton("组牌", 0, barY, btnW, btnH);
            compendiumBtn.onClick.AddListener(OnOpenCompendium);

            var startBtn = CreateBarButton("开始游戏", btnW + gap, barY, btnW, btnH);
            startBtn.onClick.AddListener(OnStartGame);
            _confirmButtonText = startBtn.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static Button CreateBarButton(string label, float x, float y, float w, float h)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<Image>().color = new Color(0.28f, 0.35f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            SetFullRect(textGo.GetComponent<RectTransform>());
            return btn;
        }

        private static bool IsDeckComplete(DeckData deck)
        {
            return deck != null && deck.CardIds != null && deck.CardIds.Count == 3;
        }

        private static void RefreshDeckList()
        {
            foreach (var row in _deckRows)
            {
                if (row != null)
                    Object.Destroy(row);
            }
            _deckRows.Clear();

            var decks = CompendiumUI.GetDecks();
            for (int i = 0; i < decks.Count; i++)
            {
                if (_selectedDeckIndex == i && !IsDeckComplete(decks[i]))
                    _selectedDeckIndex = -1;
            }
            for (int i = 0; i < decks.Count; i++)
            {
                int index = i;
                var deck = decks[i];
                var row = CreateDeckRow(deck, deck.DisplayName ?? ("牌组" + (i + 1)), index);
                row.transform.SetParent(_listContent, false);
                _deckRows.Add(row);
            }
        }

        private static GameObject CreateDeckRow(DeckData deck, string label, int index)
        {
            bool complete = IsDeckComplete(deck);
            var row = new GameObject("DeckRow_" + index);
            row.AddComponent<RectTransform>();
            var img = row.AddComponent<Image>();
            img.color = complete && index == _selectedDeckIndex ? _rowSelected : _rowNormal;
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = complete;
            if (complete)
                btn.onClick.AddListener(() => SelectDeck(index));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(row.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14, 0);
            textRect.offsetMax = new Vector2(complete ? -14 : -140, 0);

            if (!complete)
            {
                var warnGo = new GameObject("IncompleteLabel");
                warnGo.transform.SetParent(row.transform, false);
                var warnText = warnGo.AddComponent<TextMeshProUGUI>();
                warnText.text = "套牌不完整";
                if (TMPHelper.GetDefaultFont() != null) warnText.font = TMPHelper.GetDefaultFont();
                warnText.fontSize = 20;
                warnText.alignment = TextAlignmentOptions.MidlineRight;
                warnText.color = new Color(1f, 0.35f, 0.35f, 1f);
                var warnRect = warnGo.GetComponent<RectTransform>();
                warnRect.anchorMin = new Vector2(1f, 0f);
                warnRect.anchorMax = new Vector2(1f, 1f);
                warnRect.pivot = new Vector2(1f, 0.5f);
                warnRect.anchoredPosition = Vector2.zero;
                warnRect.offsetMin = new Vector2(-130, 0);
                warnRect.offsetMax = new Vector2(-14, 0);
            }
            return row;
        }

        private static void SelectDeck(int index)
        {
            if (index == _selectedDeckIndex) return;
            for (int i = 0; i < _deckRows.Count; i++)
            {
                var img = _deckRows[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == index ? _rowSelected : _rowNormal;
            }
            _selectedDeckIndex = index;
        }

        public static void Show()
        {
            _confirmSelectionOverride = null;
            _backOverride = null;
            _titleOverride = null;
            _confirmLabelOverride = null;
            OpenInternal();
        }

        public static void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public static void ShowForSelection(System.Action<DeckData> onConfirm, System.Action onBackOverride = null, string title = "选择联机套牌", string confirmLabel = "选择套牌")
        {
            _confirmSelectionOverride = onConfirm;
            _backOverride = onBackOverride;
            _titleOverride = title;
            _confirmLabelOverride = confirmLabel;
            OpenInternal();
        }

        private static void OpenInternal()
        {
            if (_root == null)
                Create();
            CompendiumUI.EnsureDecksLoaded();
            _root.SetActive(true);
            ApplyModeTexts();
            var decks = CompendiumUI.GetDecks();
            if (_selectedDeckIndex >= decks.Count)
                _selectedDeckIndex = -1;
            RefreshDeckList();
        }

        private static void ApplyModeTexts()
        {
            if (_titleText != null)
                _titleText.text = string.IsNullOrWhiteSpace(_titleOverride) ? "选择套牌开始游戏" : _titleOverride;
            if (_confirmButtonText != null)
                _confirmButtonText.text = string.IsNullOrWhiteSpace(_confirmLabelOverride) ? "开始游戏" : _confirmLabelOverride;
        }

        private static void OnBack()
        {
            Hide();
            if (_backOverride != null)
            {
                _backOverride.Invoke();
                return;
            }
            MainMenuUI.Show();
        }

        private static void OnOpenCompendium()
        {
            CompendiumUI.SetReturnTarget(Show);
            Hide();
            CompendiumUI.Show();
        }

        private static void OnStartGame()
        {
            if (_selectedDeckIndex < 0 || _selectedDeckIndex >= _deckRows.Count)
            {
                ToastUI.Show("请选择一套牌以开始游戏");
                return;
            }
            var decks = CompendiumUI.GetDecks();
            if (_selectedDeckIndex >= decks.Count)
            {
                ToastUI.Show("请选择一套牌以开始游戏");
                return;
            }
            var selectedDeck = decks[_selectedDeckIndex];
            if (!IsDeckComplete(selectedDeck))
            {
                ToastUI.Show("请选择一套完整的套牌（需包含 3 张卡牌）以开始游戏");
                return;
            }
            Hide();
            if (_confirmSelectionOverride != null)
            {
                _confirmSelectionOverride.Invoke(selectedDeck);
                return;
            }
            EnterGame(selectedDeck);
        }

        private static void EnterGame(DeckData selectedDeck)
        {
            GameUI.StartGame(selectedDeck);
        }
    }
}
