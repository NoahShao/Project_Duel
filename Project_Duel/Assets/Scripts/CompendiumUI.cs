using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary> 挂在图鉴根节点上，显示后下一帧再刷新网格尺寸，避免布局未完成时格子比例错误 </summary>
    internal class CompendiumGridRefresher : MonoBehaviour
    {
        private void OnEnable()
        {
            StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            CompendiumUI.RefreshGridLayout();
        }
    }
    /// <summary>
    /// 图鉴&组牌界面：左侧 70% 翻页图鉴（4×2 卡牌，比例 1016×1488 像素），右侧 30% 牌组列表；
    /// 筛选按钮打开筛选界面；点击卡牌打开详情（遮罩+放大居中）。
    /// </summary>
    public static class CompendiumUI
    {
        private static GameObject _root;
        private static GameObject _leftArea;
        private static GameObject _rightArea;
        private static GameObject _cardGridRoot;       // 当前页的 4×2 卡牌容器
        private static GridLayoutGroup _cardGrid;
        private static int _currentPage;
        private static int _totalPages;
        private static List<string> _cardIds = new List<string>();  // 当前要展示的卡牌 ID（来自 config，后续可被筛选）
        private static ScrollRect _deckScrollRect;
        private static Transform _deckContent;
        private static List<DeckData> _decks = new List<DeckData>();
        private static int _currentEditingDeckIndex = -1;
        private static GameObject _deckEditRoot;
        private static Transform _deckEditCardContent;
        private static ScrollRect _deckEditScrollRect;
        private static Toggle[] _deckEditSuitToggles;
        private static TMP_InputField _deckEditNameInput;
        private static TextMeshProUGUI _deckEditCardCountLabel;
        private const int MaxDecks = 9;
        private const int MaxCardsPerDeck = 3;
        private static readonly string[] DeckSuitOptions = new[] { "红桃", "方片", "黑桃", "梅花" };
        private static GameObject _filterPanelRoot;
        private static TMP_InputField _filterNameInput;
        private static Transform _filterFactionRow;
        private static Transform _filterSuitRow;
        private static Transform _filterRankRow;
        private static Transform _filterExpansionRow;
        private static ScrollRect _filterScrollRect;
        private static RectTransform _filterScrollContent;
        private static GameObject _detailRoot;
        private static Transform _detailParentBeforeGameUI;
        private static bool _detailOpenedFromGameUI;
        private static GameObject _tempDetailViewGo;
        private static Image _detailCardImage;
        private static Image _detailSpecialFormImage;
        private static RectTransform _detailCenterRect;
        private static RectTransform _detailSideRect;
        private static CardView _detailCurrentView;
        private static bool _detailShowSpecialLarge;
        private static Sprite _detailMainSprite;
        private static Sprite _detailSpecialSprite;
        private static Text _detailSkillText;
        private static TextMeshProUGUI[] _detailSkillNames;
        private static TextMeshProUGUI[] _detailSkillDescs;
        private static RectTransform _detailSkillPanelRect;
        /// <summary> tag 介绍弹窗：遮罩 + 居中面板，点击遮罩关闭。 </summary>
        private static GameObject _introModalRoot;
        private static TextMeshProUGUI _introModalText;
        /// <summary> 每个技能下方的 tag 按钮行（从左往右排列）。 </summary>
        private static Transform[] _detailSkillTagRows;
        private const float DetailRefWidth = 1920f;
        private const float DetailCardWidth = 400f;
        /// <summary> 技能名称：48 号字，列宽=5 字，行高=1.5×48；描述：36 号字，列宽=15 字，行高自适应。G/J/M=名，I/L/O=描述。 </summary>
        private const float DetailSkillNameFontSize = 48f;
        private const float DetailSkillDescFontSize = 36f;
        private const float DetailSkillNameWidth = 240f;   // 48 号字五个字列宽
        private const float DetailSkillNameHeight = 72f;   // 1.5 × 48
        private const float DetailSkillDescWidth = 840f;   // 36 号字约 20 字列宽，20 个字一换行
        private const float DetailSkillGapNameDesc = 20f;
        private const float DetailSkillGapDescName = 50f;
        private const float DetailSkillPanelWidth = 860f;
        /// <summary> tag 介绍弹窗：36 号字，第 15 字换行，行宽约 36*15≈540。 </summary>
        private const float IntroModalFontSize = 36f;
        private const int IntroModalCharsPerLine = 15;
        private const float IntroModalPanelPadding = 24f;
        private static float IntroModalTextWidth => IntroModalFontSize * IntroModalCharsPerLine;
        private static float _detailLargeW;
        private static float _detailLargeH;
        private static float _detailSmallW;
        private static float _detailSmallH;
        private static Vector2 _detailSidePosRight;
        private static Vector2 _detailSidePosLeft;
        private const int CardsPerPage = 8;
        private const int Columns = 4;
        private const int Rows = 2;
        private const float GridPaddingHorizontal = 20f;
        private const float GridPaddingTop = 48f;
        private const float GridPaddingBottom = 20f;
        private const float GridSpacing = 16f;
        private const float GridCellSafetyScale = 0.68f;
        public static void Create()
        {
            _cardIds = CardTableLoader.GetCompendiumCardIds();

            _root = new GameObject("CompendiumPanel");
            _root.SetActive(false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1920, 1080); // 与 referenceResolution 一致，确保首帧布局正确
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();
            _root.AddComponent<CompendiumGridRefresher>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_root.transform, false);
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 1f); // 图鉴页面 100% 黑色遮罩
            SetFullRect(bg.GetComponent<RectTransform>());

            var backBtn = CreateBackButton();
            backBtn.transform.SetParent(_root.transform, false);

            var filterBtn = CreateFilterButton();
            filterBtn.transform.SetParent(_root.transform, false);

            BuildLeftArea();
            BuildRightArea();
            BuildFilterPanel();
            BuildCardDetailOverlay();

            _totalPages = Mathf.Max(1, (_cardIds.Count + CardsPerPage - 1) / CardsPerPage);
            _currentPage = 0;
            UpdateGridCellSize();
            RefreshCardGrid();
            UpdatePageLabel();

            BuildDeckEditOverlay();
            RefreshDeckList();
        }

        private static void BuildLeftArea()
        {
            _leftArea = new GameObject("LeftCompendium");
            _leftArea.transform.SetParent(_root.transform, false);
            var leftRect = _leftArea.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.7f, 1f);
            leftRect.offsetMin = new Vector2(24, 24);
            leftRect.offsetMax = new Vector2(-14, -88);
            _leftArea.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 1f);
            _leftArea.AddComponent<RectMask2D>();
            _leftArea.AddComponent<DropZone>().IsCompendiumZone = true;

            var compTitle = CreateText(_leftArea.transform, "卡牌图鉴", 28);
            var compTitleRect = compTitle.GetComponent<RectTransform>();
            compTitleRect.anchorMin = new Vector2(0.5f, 1f);
            compTitleRect.anchorMax = new Vector2(0.5f, 1f);
            compTitleRect.pivot = new Vector2(0.5f, 1f);
            compTitleRect.anchoredPosition = new Vector2(0, -16);
            compTitleRect.sizeDelta = new Vector2(360, 36);

            var paginationRoot = new GameObject("Pagination");
            paginationRoot.transform.SetParent(_leftArea.transform, false);
            var paginationRect = paginationRoot.AddComponent<RectTransform>();
            paginationRect.anchorMin = new Vector2(0f, 0f);
            paginationRect.anchorMax = new Vector2(1f, 1f);
            paginationRect.offsetMin = new Vector2(24, 72);
            paginationRect.offsetMax = new Vector2(-24, -64);

            float btnW = 92f;
            float btnH = 40f;
            float bottomY = 20f;
            var prevBtn = CreateSmallButton(paginationRoot.transform, "上一页", new Vector2(0f, 0f), new Vector2(20 + btnW * 0.5f, bottomY), btnW, btnH);
            prevBtn.onClick.AddListener(PrevPage);
            var nextBtn = CreateSmallButton(paginationRoot.transform, "下一页", new Vector2(1f, 0f), new Vector2(-20 - btnW * 0.5f, bottomY), btnW, btnH);
            nextBtn.onClick.AddListener(NextPage);

            var gridViewport = new GameObject("CardGridViewport");
            gridViewport.transform.SetParent(paginationRoot.transform, false);
            var gridViewportRect = gridViewport.AddComponent<RectTransform>();
            gridViewportRect.anchorMin = new Vector2(0f, 0f);
            gridViewportRect.anchorMax = new Vector2(1f, 1f);
            gridViewportRect.offsetMin = new Vector2(0f, 60f);
            gridViewportRect.offsetMax = new Vector2(0f, -8f);
            gridViewport.AddComponent<RectMask2D>();

            _cardGridRoot = new GameObject("CardGrid");
            _cardGridRoot.transform.SetParent(gridViewport.transform, false);
            var gridRect = _cardGridRoot.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            _cardGrid = _cardGridRoot.AddComponent<GridLayoutGroup>();
            _cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _cardGrid.constraintCount = Columns;
            _cardGrid.spacing = new Vector2(GridSpacing, GridSpacing);
            _cardGrid.padding = new RectOffset((int)GridPaddingHorizontal, (int)GridPaddingHorizontal, (int)GridPaddingTop, (int)GridPaddingBottom);
            _cardGrid.childAlignment = TextAnchor.MiddleCenter;
            _cardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _cardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _cardGrid.cellSize = new Vector2(FallbackCellWidth, FallbackCellHeight);

            var pageLabel = CreateText(paginationRoot.transform, "第 1 / 1 页", 22);
            pageLabel.name = "PageLabel";
            var pageLabelRect = pageLabel.GetComponent<RectTransform>();
            pageLabelRect.anchorMin = new Vector2(0.5f, 0f);
            pageLabelRect.anchorMax = new Vector2(0.5f, 0f);
            pageLabelRect.pivot = new Vector2(0.5f, 0f);
            pageLabelRect.anchoredPosition = new Vector2(0, 6);
            pageLabelRect.sizeDelta = new Vector2(180, 26);
        }

        /// <summary> 卡牌像素尺寸：宽 1016，高 1488（横向×纵向），比例 1016:1488 </summary>
        private const float CardPixelW = 1016f;
        private const float CardPixelH = 1488f;
        private const float FixedGridCellWidth = 240f;
        private const float FixedGridCellHeight = 360f;
        private const float FallbackCellWidth = FixedGridCellWidth;
        private const float FallbackCellHeight = FixedGridCellHeight;

        private static void UpdateGridCellSize()
        {
            if (_cardGrid == null || _cardGridRoot == null) return;
            _cardGrid.cellSize = new Vector2(FixedGridCellWidth, FixedGridCellHeight);
        }

        private static void RefreshCardGrid()
        {
            for (int i = _cardGridRoot.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_cardGridRoot.transform.GetChild(i).gameObject);

            if (_cardIds == null || _cardIds.Count == 0) return;

            int start = _currentPage * CardsPerPage;
            for (int i = 0; i < CardsPerPage; i++)
            {
                int index = start + i;
                if (index >= _cardIds.Count) break;
                string cardId = _cardIds[index];
                if (string.IsNullOrEmpty(cardId) || cardId.Length < 5 || !cardId.StartsWith("NO")) continue;
                if (!int.TryParse(cardId.Substring(2), out int id)) continue;
                var data = CardTableLoader.GetCard(id);
                if (data == null) continue;
                var cardGo = CreateCardSlot(data);
                if (cardGo == null) continue;
                float cardW = FixedGridCellWidth;
                float cardH = FixedGridCellHeight;
                cardGo.transform.SetParent(_cardGridRoot.transform, false);
                cardGo.name = cardId;
                NormalizeCardSlotLayout(cardGo, cardW, cardH);
                cardGo.SetActive(true);
                var view = cardGo.GetComponent<CardView>();
                if (view != null)
                {
                    var btn = cardGo.GetComponent<Button>() ?? cardGo.AddComponent<Button>();
                    btn.targetGraphic = view.FaceImage;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OpenCardDetail(view));
                }
                bool inCurrentDeck = _currentEditingDeckIndex >= 0 && _currentEditingDeckIndex < _decks.Count && _decks[_currentEditingDeckIndex].CardIds.Contains(cardId);
                bool blockedSameName = !inCurrentDeck && CannotAddDueToSameNameRoleTag(cardId);
                if (inCurrentDeck)
                {
                    var overlay = new GameObject("InDeckOverlay");
                    overlay.transform.SetParent(cardGo.transform, false);
                    var overlayRect = overlay.AddComponent<RectTransform>();
                    SetFullRect(overlayRect);
                    var overlayImg = overlay.AddComponent<Image>();
                    overlayImg.color = new Color(0f, 0f, 0f, 1f);
                    overlayImg.raycastTarget = false;
                    var label = CreateText(overlay.transform, "已加入牌组", 14);
                    var labelRect = label.GetComponent<RectTransform>();
                    SetFullRect(labelRect);
                    label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                }
                else if (blockedSameName)
                {
                    var overlay = new GameObject("SameNameBlockOverlay");
                    overlay.transform.SetParent(cardGo.transform, false);
                    var overlayRect = overlay.AddComponent<RectTransform>();
                    SetFullRect(overlayRect);
                    var overlayImg = overlay.AddComponent<Image>();
                    overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
                    overlayImg.raycastTarget = false;
                    var label = CreateText(overlay.transform, "已加入其他同名角色", 12);
                    var labelRect = label.GetComponent<RectTransform>();
                    SetFullRect(labelRect);
                    label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                }
                if (_currentEditingDeckIndex >= 0)
                {
                    var drag = cardGo.GetComponent<CompendiumDragDrop>() ?? cardGo.AddComponent<CompendiumDragDrop>();
                    drag.CardId = cardId;
                    drag.IsDeckCard = false;
                }
            }
            Canvas.ForceUpdateCanvases();
            try
            {
                if (_cardGridRoot != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_cardGridRoot.GetComponent<RectTransform>());
            }
            catch (System.Exception) { }
        }

        /// <summary> 创建单张卡牌节点：优先从预制体 CardPrefabs/CardSlot 实例化，若无则退回代码创建。局内用 CardView.InstantiateCardSlot(data) 即可。 </summary>
        private static GameObject CreateCardSlot(CardData data)
        {
            var root = CardView.InstantiateCardSlot(data);
            if (root == null)
                root = CreateCardViewFallback(data);
            else
                root.name = "Card_" + data.CardId;
            return root;
        }

        private static GameObject CreateCardViewFallback(CardData data)
        {
            var root = new GameObject("Card_" + data.CardId);
            var rootRect = root.AddComponent<RectTransform>();
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.24f, 0.3f, 1f);
            bg.raycastTarget = true;
            var face = new GameObject("Face");
            face.transform.SetParent(root.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.anchorMin = new Vector2(0.5f, 0.5f);
            faceRect.anchorMax = new Vector2(0.5f, 0.5f);
            faceRect.pivot = new Vector2(0.5f, 0.5f);
            faceRect.anchoredPosition = Vector2.zero;
            faceRect.sizeDelta = new Vector2(100f, 100f * CardPixelH / CardPixelW);
            var img = face.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            img.raycastTarget = true;
            var view = root.AddComponent<CardView>();
            view.Data = data;
            view.FaceImage = img;
            view.LoadFaceSprite();
            return root;
        }

        // Older single-card prefabs may use FaceImage instead of Face; force the face rect
        // to fill the card root so it cannot overflow the GridLayout cell.
        private static void NormalizeCardSlotLayout(GameObject cardGo, float cardW, float cardH)
        {
            if (cardGo == null) return;

            var cardRect = cardGo.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(cardW, cardH);
            }

            var faceRect = GetCardFaceRect(cardGo);
            if (faceRect != null)
            {
                SetFullRect(faceRect);
                faceRect.pivot = new Vector2(0.5f, 0.5f);
                faceRect.anchoredPosition = Vector2.zero;
                faceRect.localScale = Vector3.one;
            }
        }

        private static RectTransform GetCardFaceRect(GameObject cardGo)
        {
            if (cardGo == null) return null;

            var view = cardGo.GetComponent<CardView>();
            if (view != null && view.FaceImage != null)
                return view.FaceImage.rectTransform;

            var display = cardGo.GetComponent<CardDisplay>();
            if (display != null && display.FaceImage != null)
                return display.FaceImage.rectTransform;

            var faceTr = cardGo.transform.Find("Face");
            if (faceTr == null)
                faceTr = cardGo.transform.Find("FaceImage");

            return faceTr != null ? faceTr.GetComponent<RectTransform>() : null;
        }

        private static void OpenCardDetail(CardView view)
        {
            if (view == null || view.Data == null || _detailRoot == null) return;
            try
            {
                _detailCurrentView = view;
                _detailShowSpecialLarge = false;
                _detailRoot.transform.SetAsLastSibling();
                _detailRoot.SetActive(true);
                if (_detailCardImage != null)
                {
                    _detailCardImage.sprite = view.FaceSprite;
                    _detailCardImage.enabled = view.FaceSprite != null;
                    _detailCardImage.color = view.FaceSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.25f, 1f);
                    _detailCardImage.preserveAspect = true;
                }
                _detailMainSprite = view.FaceSprite;
                RefreshDetailSkillText(showSpecialSkills: false);
                if (_detailSpecialFormImage != null)
                {
                    if (view.Data.HasSpecialForm && view.Data.SpecialFormId > 0)
                    {
                        string specialResId = CardTableLoader.GetSpecialFormCardId(view.Data.SpecialFormId);
                        _detailSpecialSprite = !string.IsNullOrEmpty(specialResId) ? CardView.LoadCardSprite("Cards/" + specialResId) : null;
                        _detailSpecialFormImage.sprite = _detailSpecialSprite;
                        _detailSpecialFormImage.enabled = true;
                        _detailSpecialFormImage.color = _detailSpecialSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.25f, 1f);
                        _detailSpecialFormImage.preserveAspect = true;
                        ApplyDetailLayout(mainLarge: true);
                    }
                    else
                    {
                        _detailSpecialSprite = null;
                        _detailSpecialFormImage.sprite = null;
                        _detailSpecialFormImage.enabled = false;
                        ApplyDetailLayout(mainLarge: true);
                    }
                }
            }
            catch (System.Exception e) { UnityEngine.Debug.LogException(e); }
        }

        private static void ApplyDetailLayout(bool mainLarge)
        {
            if (_detailCenterRect == null || _detailSideRect == null) return;
            float largeW = _detailLargeW;
            float largeH = _detailLargeH;
            float smallW = _detailSmallW;
            float smallH = _detailSmallH;
            bool hasSpecial = _detailCurrentView?.Data?.HasSpecialForm ?? false;
            float panelW = DetailSkillPanelWidth / DetailRefWidth;
            float halfCardW = (DetailCardWidth * 0.5f) / DetailRefWidth;
            float standardCardCenter = 0.35f;
            float largeCardCenterX;
            if (mainLarge)
            {
                if (hasSpecial)
                {
                    float cardLeft = 0.02f + panelW;
                    largeCardCenterX = cardLeft + halfCardW;
                    _detailCenterRect.anchorMin = new Vector2(largeCardCenterX, 0.5f);
                    _detailCenterRect.anchorMax = new Vector2(largeCardCenterX, 0.5f);
                    _detailSideRect.anchorMin = _detailSidePosRight;
                    _detailSideRect.anchorMax = _detailSidePosRight;
                }
                else
                {
                    largeCardCenterX = standardCardCenter;
                    _detailCenterRect.anchorMin = new Vector2(standardCardCenter, 0.5f);
                    _detailCenterRect.anchorMax = new Vector2(standardCardCenter, 0.5f);
                    _detailSideRect.anchorMin = _detailSidePosRight;
                    _detailSideRect.anchorMax = _detailSidePosRight;
                }
                _detailCenterRect.anchoredPosition = Vector2.zero;
                _detailCenterRect.sizeDelta = new Vector2(largeW, largeH);
                _detailSideRect.anchoredPosition = Vector2.zero;
                _detailSideRect.sizeDelta = new Vector2(smallW, smallH);
            }
            else
            {
                largeCardCenterX = standardCardCenter;
                _detailCenterRect.anchorMin = new Vector2(standardCardCenter, 0.5f);
                _detailCenterRect.anchorMax = new Vector2(standardCardCenter, 0.5f);
                _detailCenterRect.anchoredPosition = Vector2.zero;
                _detailCenterRect.sizeDelta = new Vector2(largeW, largeH);
                _detailSideRect.anchorMin = _detailSidePosLeft;
                _detailSideRect.anchorMax = _detailSidePosLeft;
                _detailSideRect.anchoredPosition = Vector2.zero;
                _detailSideRect.sizeDelta = new Vector2(smallW, smallH);
            }
            float largeCardLeft = largeCardCenterX - halfCardW;
            float largeCardRight = largeCardCenterX + halfCardW;
            if (hasSpecial)
            {
                SetDetailSkillPanelSide(panelOnLeftOfCard: mainLarge, largeCardLeft, largeCardRight, panelW);
                RefreshDetailSkillText(!mainLarge);
            }
            else
            {
                SetDetailSkillPanelSide(panelOnLeftOfCard: false, largeCardLeft, largeCardRight, panelW);
            }
        }

        private static void OnDetailCenterClicked()
        {
            if (_detailCurrentView != null && _detailCurrentView.Data != null && _detailCurrentView.Data.HasSpecialForm && _detailShowSpecialLarge)
            {
                _detailShowSpecialLarge = false;
                _detailCardImage.sprite = _detailMainSprite;
                _detailSpecialFormImage.sprite = _detailSpecialSprite;
                ApplyDetailLayout(mainLarge: true);
            }
            else
                CloseDetailOverlay();
        }

        private static void CloseDetailOverlay()
        {
            if (_detailRoot != null) _detailRoot.SetActive(false);
            if (_detailOpenedFromGameUI && _detailRoot != null && _detailParentBeforeGameUI != null)
            {
                _detailRoot.transform.SetParent(_detailParentBeforeGameUI);
                _detailOpenedFromGameUI = false;
            }
            if (_tempDetailViewGo != null) { Object.Destroy(_tempDetailViewGo); _tempDetailViewGo = null; }
        }

        /// <summary> 由对局界面调用：按 cardId 打开与图鉴一致的卡牌详情（遮罩+技能等），可关闭后回到对局。 </summary>
        public static void ShowCardDetailByCardId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _detailRoot == null) return;
            int id = CardTableLoader.CardIdToNumber(cardId);
            var data = CardTableLoader.GetCard(id);
            if (data == null) return;
            _detailParentBeforeGameUI = _detailRoot.transform.parent;
            _detailOpenedFromGameUI = true;
            _detailRoot.transform.SetParent(GameUI.GetRootTransform(), false);
            var detailRect = _detailRoot.GetComponent<RectTransform>();
            if (detailRect != null)
            {
                detailRect.anchorMin = Vector2.zero;
                detailRect.anchorMax = Vector2.one;
                detailRect.offsetMin = Vector2.zero;
                detailRect.offsetMax = Vector2.zero;
                detailRect.anchoredPosition = Vector2.zero;
                detailRect.localScale = Vector3.one;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailRect);
            }
            _tempDetailViewGo = new GameObject("TempDetailView");
            var face = new GameObject("Face");
            face.transform.SetParent(_tempDetailViewGo.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.sizeDelta = new Vector2(100, 100);
            var faceImg = face.AddComponent<Image>();
            var view = _tempDetailViewGo.AddComponent<CardView>();
            view.Data = data;
            view.FaceImage = faceImg;
            view.LoadFaceSprite();
            _detailCurrentView = view;
            _detailShowSpecialLarge = false;
            _detailRoot.transform.SetAsLastSibling();
            _detailRoot.SetActive(true);
            if (_detailCardImage != null)
            {
                _detailCardImage.sprite = view.FaceSprite;
                _detailCardImage.enabled = view.FaceSprite != null;
                _detailCardImage.color = view.FaceSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.25f, 1f);
                _detailCardImage.preserveAspect = true;
            }
            _detailMainSprite = view.FaceSprite;
            RefreshDetailSkillText(showSpecialSkills: false);
            if (_detailSpecialFormImage != null)
            {
                if (data.HasSpecialForm && data.SpecialFormId > 0)
                {
                    string specialResId = CardTableLoader.GetSpecialFormCardId(data.SpecialFormId);
                    _detailSpecialSprite = CardView.LoadCardSprite("Cards/" + specialResId);
                    _detailSpecialFormImage.sprite = _detailSpecialSprite;
                    _detailSpecialFormImage.enabled = true;
                    _detailSpecialFormImage.color = _detailSpecialSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.25f, 1f);
                    _detailSpecialFormImage.preserveAspect = true;
                    ApplyDetailLayout(mainLarge: true);
                }
                else
                {
                    _detailSpecialSprite = null;
                    _detailSpecialFormImage.sprite = null;
                    _detailSpecialFormImage.enabled = false;
                    ApplyDetailLayout(mainLarge: true);
                }
            }
        }

        private static void AppendSkill(System.Text.StringBuilder sb, string name, string desc)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(desc)) return;
            if (!string.IsNullOrWhiteSpace(name)) sb.AppendLine(name.Trim());
            if (!string.IsNullOrWhiteSpace(desc)) sb.AppendLine(desc.Trim());
            sb.AppendLine();
        }

        private static void RefreshDetailSkillText(bool showSpecialSkills)
        {
            if (_detailCurrentView?.Data == null) return;
            var d = _detailCurrentView.Data;
            if (_detailSkillNames != null && _detailSkillDescs != null)
            {
                string n1, n2, n3, t1, t2, t3;
                if (showSpecialSkills)
                {
                    n1 = d.SpecialSkillName1?.Trim() ?? ""; t1 = d.SpecialSkillDesc1?.Trim() ?? "";
                    n2 = d.SpecialSkillName2?.Trim() ?? ""; t2 = d.SpecialSkillDesc2?.Trim() ?? "";
                    n3 = d.SpecialSkillName3?.Trim() ?? ""; t3 = d.SpecialSkillDesc3?.Trim() ?? "";
                }
                else
                {
                    n1 = d.SkillName1?.Trim() ?? ""; t1 = d.SkillDesc1?.Trim() ?? "";
                    n2 = d.SkillName2?.Trim() ?? ""; t2 = d.SkillDesc2?.Trim() ?? "";
                    n3 = d.SkillName3?.Trim() ?? ""; t3 = d.SkillDesc3?.Trim() ?? "";
                }
                var tags1 = showSpecialSkills ? d.SpecialSkillTags1 : d.SkillTags1;
                var tags2 = showSpecialSkills ? d.SpecialSkillTags2 : d.SkillTags2;
                var tags3 = showSpecialSkills ? d.SpecialSkillTags3 : d.SkillTags3;
                SetDetailSkillBox(_detailSkillNames[0], _detailSkillDescs[0], n1, t1, tags1, 0);
                SetDetailSkillBox(_detailSkillNames[1], _detailSkillDescs[1], n2, t2, tags2, 1);
                SetDetailSkillBox(_detailSkillNames[2], _detailSkillDescs[2], n3, t3, tags3, 2);
                RefreshDetailTagButtons(tags1, tags2, tags3);
                bool anySkill = !(string.IsNullOrWhiteSpace(n1) && string.IsNullOrWhiteSpace(t1) &&
                    string.IsNullOrWhiteSpace(n2) && string.IsNullOrWhiteSpace(t2) &&
                    string.IsNullOrWhiteSpace(n3) && string.IsNullOrWhiteSpace(t3));
                if (_detailSkillPanelRect != null)
                    _detailSkillPanelRect.gameObject.SetActive(anySkill);
                if (_detailSkillNames != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var pair = _detailSkillNames[i].transform.parent; // SkillPair 节点
                        if (pair != null)
                        {
                            string na = (i == 0 ? n1 : i == 1 ? n2 : n3);
                            string ta = (i == 0 ? t1 : i == 1 ? t2 : t3);
                            pair.gameObject.SetActive(!string.IsNullOrWhiteSpace(na) || !string.IsNullOrWhiteSpace(ta));
                        }
                    }
                }
                if (_detailSkillPanelRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_detailSkillPanelRect);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_detailSkillPanelRect);
                }
            }
            else if (_detailSkillText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (showSpecialSkills)
                {
                    AppendSkill(sb, d.SpecialSkillName1, d.SpecialSkillDesc1);
                    AppendSkill(sb, d.SpecialSkillName2, d.SpecialSkillDesc2);
                    AppendSkill(sb, d.SpecialSkillName3, d.SpecialSkillDesc3);
                }
                else
                {
                    AppendSkill(sb, d.SkillName1, d.SkillDesc1);
                    AppendSkill(sb, d.SkillName2, d.SkillDesc2);
                    AppendSkill(sb, d.SkillName3, d.SkillDesc3);
                }
                _detailSkillText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "（无技能配置）";
            }
        }

        private static void SetDetailSkillBox(TextMeshProUGUI nameText, TextMeshProUGUI descText, string name, string desc, List<string> tags, int skillIndex)
        {
            if (nameText != null) { nameText.text = name; nameText.gameObject.SetActive(!string.IsNullOrEmpty(name)); }
            if (descText != null)
            {
                descText.text = WrapTagsWithStyle(desc ?? "", tags);
                descText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            }
        }

        /// <summary> 为三个技能行填充 tag 按钮（从左往右），点击打开介绍弹窗。 </summary>
        private static void RefreshDetailTagButtons(List<string> tags1, List<string> tags2, List<string> tags3)
        {
            if (_detailSkillTagRows == null) return;
            var font = TMPHelper.GetDefaultFont();
            var tagLists = new[] { tags1 ?? new List<string>(), tags2 ?? new List<string>(), tags3 ?? new List<string>() };
            for (int row = 0; row < 3 && row < _detailSkillTagRows.Length; row++)
            {
                var rowTrans = _detailSkillTagRows[row];
                if (rowTrans == null) continue;
                for (int c = rowTrans.childCount - 1; c >= 0; c--)
                    UnityEngine.Object.Destroy(rowTrans.GetChild(c).gameObject);
                var tags = tagLists[row];
                rowTrans.gameObject.SetActive(tags.Count > 0);
                foreach (string tag in tags)
                {
                    if (string.IsNullOrWhiteSpace(tag)) continue;
                    string tagId = tag.Trim();
                    var btn = CreateTagButton(rowTrans, tagId, font);
                    btn.onClick.AddListener(() => ShowIntroModal(tagId));
                }
            }
        }

        private static Button CreateTagButton(Transform parent, string label, TMP_FontAsset font)
        {
            const float btnW = 132f, btnH = 48f;
            var go = new GameObject("TagBtn_" + label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(btnW, btnH);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = btnW;
            le.preferredHeight = btnH;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.4f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            SetFullRect(textRect);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            if (font != null) text.font = font;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return btn;
        }

        /// <summary> 技能描述中 tag 的富文本颜色（攻击技深红、破军技金、防御技/抵御深蓝、韬光深紫，其余黄）。 </summary>
        private static string GetTagDescColorHex(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "yellow";
            switch (tag.Trim())
            {
                case "攻击技": return "#B81414";  // 深红
                case "破军技": return "#E6BF33";  // 金
                case "防御技": return "#2633A6";  // 深蓝
                case "抵御":   return "#2633A6";  // 深蓝
                case "韬光":   return "#73268C";  // 深紫
                default:       return "yellow";
            }
        }

        /// <summary> 描述中：属于 H/K/N（或 T/W/Z）的 tag 加粗+按表设色；红色→明红；若干伤害类型加下划线。 </summary>
        private static string WrapTagsWithStyle(string desc, List<string> tags)
        {
            if (string.IsNullOrEmpty(desc)) return desc;
            string s = desc;
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    string t = tag.Trim();
                    if (string.IsNullOrEmpty(t) || !s.Contains(t)) continue;
                    string color = GetTagDescColorHex(t);
                    s = s.Replace(t, "<b><color=" + color + ">" + t + "</color></b>");
                }
            }
            // 无 tag 但需字色：红色 → 明红
            s = s.Replace("红色", "<color=#FF4444>红色</color>");
            // 伤害类型：加下划线，颜色不变（TextMeshPro 支持 &lt;u&gt;）
            string[] underlinePhrases = new[] { "通用伤害", "兵刃伤害", "属性伤害", "雷电伤害", "火焰伤害", "毒性伤害", "水淹伤害" };
            foreach (var phrase in underlinePhrases)
            {
                if (s.Contains(phrase))
                    s = s.Replace(phrase, "<u>" + phrase + "</u>");
            }
            return s;
        }

        public static CardData GetDetailCurrentCardData() => _detailCurrentView?.Data;

        /// <summary> 当前详情是否在展示特殊形态技能（T/W/Z、U/X/AA）。 </summary>
        public static bool GetDetailShowSpecialFormSkills() => _detailShowSpecialLarge;

        /// <summary> 点击 tag 按钮后：在屏幕中央打开介绍弹窗，展示 intro.xlsx 中对应 id 的内容；弹窗大小随文本；点击遮罩关闭。 </summary>
        public static void ShowIntroModal(string tagId)
        {
            if (_introModalRoot == null || _introModalText == null) return;
            string intro = IntroLoader.GetIntro(tagId ?? "");
            _introModalText.text = string.IsNullOrEmpty(intro) ? "（暂无介绍）" : intro;
            _introModalRoot.transform.SetAsLastSibling();
            _introModalRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_introModalText.rectTransform);
            float preferredH = LayoutUtility.GetPreferredHeight(_introModalText.rectTransform);
            float panelW = IntroModalTextWidth + IntroModalPanelPadding * 2f;
            float panelH = Mathf.Max(80f, preferredH + IntroModalPanelPadding * 2f);
            var panelRect = _introModalText.transform.parent.GetComponent<RectTransform>();
            if (panelRect != null) panelRect.sizeDelta = new Vector2(panelW, panelH);
        }

        public static void HideIntroModal()
        {
            if (_introModalRoot != null) _introModalRoot.SetActive(false);
        }

        /// <summary> 普通角色：技能文本框左侧紧贴卡牌右侧(panelOnLeftOfCard=false)。特殊角色普通状态：技能文本框右侧紧贴卡牌左侧(panelOnLeftOfCard=true)。特殊角色特殊状态：技能文本框左侧紧贴卡牌右侧(panelOnLeftOfCard=false)。 </summary>
        private static void SetDetailSkillPanelSide(bool panelOnLeftOfCard, float largeCardLeft, float largeCardRight, float panelW)
        {
            if (_detailSkillPanelRect == null) return;
            float vMin = 0.22f;
            float vMax = 0.92f;
            if (panelOnLeftOfCard)
            {
                _detailSkillPanelRect.anchorMin = new Vector2(largeCardLeft - panelW, vMin);
                _detailSkillPanelRect.anchorMax = new Vector2(largeCardLeft, vMax);
            }
            else
            {
                _detailSkillPanelRect.anchorMin = new Vector2(largeCardRight, vMin);
                _detailSkillPanelRect.anchorMax = new Vector2(largeCardRight + panelW, vMax);
            }
            _detailSkillPanelRect.offsetMin = Vector2.zero;
            _detailSkillPanelRect.offsetMax = Vector2.zero;
        }

        private static void OnDetailSideClicked()
        {
            if (_detailCurrentView == null || _detailCurrentView.Data == null || !_detailCurrentView.Data.HasSpecialForm) return;
            _detailShowSpecialLarge = !_detailShowSpecialLarge;
            if (_detailShowSpecialLarge)
            {
                _detailCardImage.sprite = _detailSpecialSprite;
                _detailSpecialFormImage.sprite = _detailMainSprite;
                ApplyDetailLayout(mainLarge: false);
            }
            else
            {
                _detailCardImage.sprite = _detailMainSprite;
                _detailSpecialFormImage.sprite = _detailSpecialSprite;
                ApplyDetailLayout(mainLarge: true);
            }
        }

        private static void BuildCardDetailOverlay()
        {
            _detailRoot = new GameObject("CardDetailOverlay");
            _detailRoot.transform.SetParent(_root.transform, false);
            var detailRootRect = _detailRoot.AddComponent<RectTransform>();
            SetFullRect(detailRootRect);
            _detailRoot.SetActive(false);

            // 1) 全屏通用遮罩：黑色、透明度 80%，挡住除放大卡牌外的所有内容，点击遮罩关闭
            var overlayBg = new GameObject("Overlay");
            overlayBg.transform.SetParent(_detailRoot.transform, false);
            var overlayRect = overlayBg.AddComponent<RectTransform>();
            SetFullRect(overlayRect);
            var overlayImg = overlayBg.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 1f); // 100% 黑色遮罩，凸显文字
            overlayImg.raycastTarget = true;
            var overlayBtn = overlayBg.AddComponent<Button>();
            overlayBtn.onClick.AddListener(CloseDetailOverlay);

            // 2) 居中放大的卡牌（在遮罩之上）；有特殊形态时点击居中卡可切回主牌或关闭
            float detailSize = 400f;
            float detailH = detailSize * CardPixelH / CardPixelW;
            _detailLargeW = detailSize;
            _detailLargeH = detailH;
            float specialSize = 280f;
            _detailSmallW = specialSize;
            _detailSmallH = specialSize * CardPixelH / CardPixelW;
            // 觉醒后小图在右侧：右移避免与左侧大图（觉醒前）重叠
            _detailSidePosRight = new Vector2(0.80f, 0.5f);
            float smallHalfW = (280f * 0.5f) / DetailRefWidth;
            float centerCardLeft = 0.35f - (DetailCardWidth * 0.5f) / DetailRefWidth;
            _detailSidePosLeft = new Vector2(centerCardLeft - smallHalfW - 0.02f, 0.5f);

            var centerCard = new GameObject("CenterCard");
            centerCard.transform.SetParent(_detailRoot.transform, false);
            _detailCenterRect = centerCard.AddComponent<RectTransform>();
            _detailCenterRect.anchorMin = new Vector2(0.5f, 0.5f);
            _detailCenterRect.anchorMax = new Vector2(0.5f, 0.5f);
            _detailCenterRect.pivot = new Vector2(0.5f, 0.5f);
            _detailCenterRect.anchoredPosition = Vector2.zero;
            _detailCenterRect.sizeDelta = new Vector2(detailSize, detailH);
            _detailCardImage = centerCard.AddComponent<Image>();
            _detailCardImage.color = Color.white;
            _detailCardImage.raycastTarget = true;
            _detailCardImage.preserveAspect = true;
            var centerBtn = centerCard.AddComponent<Button>();
            centerBtn.onClick.AddListener(OnDetailCenterClicked);

            // 2b) 右侧/左侧小卡（特殊形态）；有特殊形态时显示，点击可切换主牌与特殊形态大小位置
            var specialCard = new GameObject("SpecialFormCard");
            specialCard.transform.SetParent(_detailRoot.transform, false);
            _detailSideRect = specialCard.AddComponent<RectTransform>();
            _detailSideRect.anchorMin = _detailSidePosRight;
            _detailSideRect.anchorMax = _detailSidePosRight;
            _detailSideRect.pivot = new Vector2(0.5f, 0.5f);
            _detailSideRect.anchoredPosition = Vector2.zero;
            _detailSideRect.sizeDelta = new Vector2(_detailSmallW, _detailSmallH);
            _detailSpecialFormImage = specialCard.AddComponent<Image>();
            _detailSpecialFormImage.color = Color.white;
            _detailSpecialFormImage.raycastTarget = true;
            _detailSpecialFormImage.preserveAspect = true;
            _detailSpecialFormImage.enabled = false;
            var sideBtn = specialCard.AddComponent<Button>();
            sideBtn.onClick.AddListener(OnDetailSideClicked);

            // 3) 提示文字放在卡牌正下方，不遮挡卡图
            var closeLabel = CreateText(_detailRoot.transform, "点击任意处关闭", 20);
            closeLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            var closeRect = closeLabel.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0f, 0f);
            closeRect.anchorMax = new Vector2(0f, 0f);
            closeRect.pivot = new Vector2(0f, 0f);
            closeRect.anchoredPosition = new Vector2(24, 24);
            closeRect.sizeDelta = new Vector2(220, 28);

            // 4) 技能区：至多 6 个文本框（1名/2述/3名/4述/5名/6述），左对齐于卡牌大图右侧边缘，整体与卡牌上下居中。1-2/3-4/5-6 间距 20，2-3/4-5 间距 50。
            var skillPanel = new GameObject("DetailSkillPanel");
            skillPanel.transform.SetParent(_detailRoot.transform, false);
            _detailSkillPanelRect = skillPanel.AddComponent<RectTransform>();
            float cardRightDefault = 0.35f + (DetailCardWidth * 0.5f) / DetailRefWidth;
            _detailSkillPanelRect.anchorMin = new Vector2(cardRightDefault, 0.22f);
            _detailSkillPanelRect.anchorMax = new Vector2(cardRightDefault + DetailSkillPanelWidth / DetailRefWidth, 0.92f);
            _detailSkillPanelRect.offsetMin = Vector2.zero;
            _detailSkillPanelRect.offsetMax = Vector2.zero;
            var skillVlg = skillPanel.AddComponent<VerticalLayoutGroup>();
            skillVlg.childAlignment = TextAnchor.MiddleLeft;   // 整体与卡牌上下居中、左侧贴齐卡牌右缘
            skillVlg.childControlHeight = false;
            skillVlg.childControlWidth = false;
            skillVlg.childForceExpandHeight = false;
            skillVlg.childForceExpandWidth = false;
            skillVlg.spacing = DetailSkillGapDescName; // 2-3、4-5 之间 50
            skillVlg.padding = new RectOffset(0, 12, 12, 12);

            _detailSkillNames = new TextMeshProUGUI[3];
            _detailSkillDescs = new TextMeshProUGUI[3];
            _detailSkillTagRows = new Transform[3];
            var font = TMPHelper.GetDefaultFont(); // 技能名称、描述、tag 按钮统一用中文字体
            var textColor = new Color(0.9f, 0.9f, 0.92f, 1f);
            for (int i = 0; i < 3; i++)
            {
                var pair = new GameObject("SkillPair" + (i + 1));
                pair.transform.SetParent(skillPanel.transform, false);
                pair.AddComponent<RectTransform>();
                var pairVlg = pair.AddComponent<VerticalLayoutGroup>();
                pairVlg.spacing = DetailSkillGapNameDesc;
                pairVlg.childAlignment = TextAnchor.UpperLeft;
                pairVlg.childControlHeight = false;
                pairVlg.childControlWidth = false;
                pairVlg.childForceExpandHeight = false;
                pairVlg.childForceExpandWidth = false;
                pair.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var pairLe = pair.AddComponent<LayoutElement>();
                pairLe.preferredWidth = DetailSkillDescWidth;
                pairLe.minWidth = DetailSkillDescWidth;
                pairLe.flexibleWidth = 0f;

                // 技能名称：48 号字，列宽 5 字，行高 1.5×48（G/J/M）
                var nameGo = new GameObject("SkillName" + (i + 1));
                nameGo.transform.SetParent(pair.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(DetailSkillNameWidth, DetailSkillNameHeight);
                var nameLe = nameGo.AddComponent<LayoutElement>();
                nameLe.preferredWidth = DetailSkillNameWidth;
                nameLe.preferredHeight = DetailSkillNameHeight;
                nameLe.flexibleWidth = 0f;
                var nameText = nameGo.AddComponent<TextMeshProUGUI>();
                if (font != null) nameText.font = font;
                nameText.fontSize = DetailSkillNameFontSize;
                nameText.color = textColor;
                nameText.alignment = TextAlignmentOptions.TopLeft;
                nameText.raycastTarget = false;
                nameText.text = "";
                nameText.overflowMode = TextOverflowModes.Overflow;
                _detailSkillNames[i] = nameText;

                // 技能描述：36 号字，固定列宽使第 16 字换行，行高自适应（I/L/O），支持 &lt;u&gt; 下划线
                var descGo = new GameObject("SkillDesc" + (i + 1));
                descGo.transform.SetParent(pair.transform, false);
                var descRect = descGo.AddComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0f, 1f);
                descRect.anchorMax = new Vector2(0f, 1f);
                descRect.pivot = new Vector2(0f, 1f);
                descRect.anchoredPosition = Vector2.zero;
                descRect.sizeDelta = new Vector2(DetailSkillDescWidth, 200f); // 固定宽度，不随父节点被压窄
                var descCsf = descGo.AddComponent<ContentSizeFitter>();
                descCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                descCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                var descLe = descGo.AddComponent<LayoutElement>();
                descLe.preferredWidth = DetailSkillDescWidth;
                descLe.minWidth = DetailSkillDescWidth;  // 禁止被压窄，20 字一换行
                descLe.flexibleWidth = 0f;
                descLe.minHeight = 24f;
                var descText = descGo.AddComponent<TextMeshProUGUI>();
                if (font != null) descText.font = font;
                descText.fontSize = DetailSkillDescFontSize;
                descText.color = textColor;
                descText.alignment = TextAlignmentOptions.TopLeft;
                descText.raycastTarget = true;
                descText.text = "";
                descText.enableWordWrapping = true;
                descText.overflowMode = TextOverflowModes.Overflow;
                _detailSkillDescs[i] = descText;

                // 该技能 tag 按钮行：从左往右排列，点击后在屏幕中央打开介绍弹窗
                var tagRowGo = new GameObject("SkillTagRow" + (i + 1));
                tagRowGo.transform.SetParent(pair.transform, false);
                var tagRowRect = tagRowGo.AddComponent<RectTransform>();
                tagRowRect.anchorMin = new Vector2(0f, 1f);
                tagRowRect.anchorMax = new Vector2(1f, 1f);
                tagRowRect.pivot = new Vector2(0f, 1f);
                tagRowRect.anchoredPosition = Vector2.zero;
                tagRowRect.sizeDelta = new Vector2(0f, 56f);
                var tagRowHlg = tagRowGo.AddComponent<HorizontalLayoutGroup>();
                tagRowHlg.spacing = 12f;
                tagRowHlg.childAlignment = TextAnchor.MiddleLeft;
                tagRowHlg.childControlWidth = false;
                tagRowHlg.childControlHeight = false;
                tagRowHlg.childForceExpandWidth = false;
                tagRowHlg.childForceExpandHeight = false;
                tagRowHlg.padding = new RectOffset(0, 4, 0, 0);
                _detailSkillTagRows[i] = tagRowGo.transform;
            }
            _detailSkillText = null;

            // 5) tag 介绍弹窗：全屏遮罩 + 居中面板，36 号字 15 字换行，点击遮罩关闭
            var modalRoot = new GameObject("IntroModal");
            modalRoot.transform.SetParent(_detailRoot.transform, false);
            _introModalRoot = modalRoot;
            _introModalRoot.SetActive(false);
            var modalRootRect = modalRoot.AddComponent<RectTransform>();
            SetFullRect(modalRootRect);

            var mask = new GameObject("IntroModalMask");
            mask.transform.SetParent(modalRoot.transform, false);
            var maskRect = mask.AddComponent<RectTransform>();
            SetFullRect(maskRect);
            var maskImg = mask.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.6f);
            maskImg.raycastTarget = true;
            var maskBtn = mask.AddComponent<Button>();
            maskBtn.onClick.AddListener(HideIntroModal);

            var panel = new GameObject("IntroModalPanel");
            panel.transform.SetParent(modalRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(IntroModalTextWidth + IntroModalPanelPadding * 2f, 200f);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = Color.white;
            panelImg.raycastTarget = true;

            var textGo = new GameObject("IntroModalText");
            textGo.transform.SetParent(panel.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(IntroModalPanelPadding, IntroModalPanelPadding);
            textRect.offsetMax = new Vector2(-IntroModalPanelPadding, -IntroModalPanelPadding);
            _introModalText = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) _introModalText.font = font;
            _introModalText.fontSize = IntroModalFontSize;
            _introModalText.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            _introModalText.alignment = TextAlignmentOptions.TopLeft;
            _introModalText.enableWordWrapping = true;
            _introModalText.overflowMode = TextOverflowModes.Overflow;
            _introModalText.raycastTarget = false;
            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.preferredWidth = IntroModalTextWidth;
            textLe.flexibleWidth = 0f;
            textLe.flexibleHeight = 0f;
            var textCsf = textGo.AddComponent<ContentSizeFitter>();
            textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private static readonly string[] FilterFactions = new[] { "蜀", "魏", "吴", "群", "汉", "晋" };
        private static readonly string[] FilterSuits = new[] { "红桃", "方片", "黑桃", "梅花", "无" };
        private static readonly string[] FilterRanks = new[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly string[] FilterExpansions = new[] { "基础", "智包", "勇包", "姿包", "阵包", "势包", "命包", "梦包", "sp1三足鼎立" };

        private static void BuildFilterPanel()
        {
            _filterPanelRoot = new GameObject("FilterPanel");
            _filterPanelRoot.transform.SetParent(_root.transform, false);
            var filterRootRect = _filterPanelRoot.AddComponent<RectTransform>();
            SetFullRect(filterRootRect);
            _filterPanelRoot.SetActive(false);

            // 通用遮罩层：透明度 80%（alpha 0.8），挡住后面页面，点击关闭
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_filterPanelRoot.transform, false);
            SetFullRect(overlay.AddComponent<RectTransform>());
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.8f);
            overlayImg.raycastTarget = true;
            var overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.onClick.AddListener(CloseFilterPanel);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_filterPanelRoot.transform, false);
            panel.transform.SetAsLastSibling();
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(920, 680);
            panelRect.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 1f);

            var title = CreateText(panel.transform, "筛选卡牌", 32);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(200, 32);

            // 可滚动区域：标题下方到底部，留出上下左右边距
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(20, 20);
            scrollRect.offsetMax = new Vector2(-20, -70);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 20f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            SetFullRect(viewportRect);
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewportRect;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 600);
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.padding = new RectOffset(0, 0, 0, 24);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            scroll.content = contentRect;
            _filterScrollRect = scroll;
            _filterScrollContent = contentRect;

            const float nameRowH = 30f;
            float rowH = 38f;
            float labelW = 100f;

            // 输入名称行（高度单独缩小）
            var nameRow = new GameObject("NameRow");
            nameRow.transform.SetParent(content.transform, false);
            nameRow.AddComponent<RectTransform>();
            var nameRowLE = nameRow.AddComponent<LayoutElement>();
            nameRowLE.preferredHeight = nameRowH;
            nameRowLE.flexibleWidth = 1;
            var nameHlg = nameRow.AddComponent<HorizontalLayoutGroup>();
            nameHlg.spacing = 10f;
            nameHlg.childAlignment = TextAnchor.MiddleLeft;
            nameHlg.childControlWidth = false;
            nameHlg.childControlHeight = true;
            nameHlg.childForceExpandWidth = false;
            nameHlg.childForceExpandHeight = true;
            var nameLabel = CreateText(nameRow.transform, "输入名称", 22);
            nameLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(labelW, nameRowH);
            var nameLabelLE = nameLabel.AddComponent<LayoutElement>();
            nameLabelLE.preferredWidth = labelW;
            nameLabelLE.preferredHeight = nameRowH;
            var nameInputGo = CreateInputField(nameRow.transform);
            var nameInputRect = nameInputGo.GetComponent<RectTransform>();
            nameInputRect.sizeDelta = new Vector2(260, nameRowH);
            var nameInputLE = nameInputGo.AddComponent<LayoutElement>();
            nameInputLE.preferredWidth = 260f;
            nameInputLE.preferredHeight = nameRowH;
            _filterNameInput = nameInputGo.GetComponent<TMP_InputField>();

            _filterFactionRow = CreateFilterCheckboxSection(content.transform, "势力", FilterFactions, labelW, rowH, 6);
            _filterSuitRow = CreateFilterCheckboxSection(content.transform, "花色", FilterSuits, labelW, rowH, 5);
            _filterRankRow = CreateFilterCheckboxSection(content.transform, "点数", FilterRanks, labelW, rowH, 13);
            _filterExpansionRow = CreateFilterCheckboxSection(content.transform, "所属扩展包", FilterExpansions, labelW, rowH, 9);

            // 确定/取消固定在滚动区域下方
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panel.transform, false);
            var btnRowRect = btnRow.AddComponent<RectTransform>();
            btnRowRect.anchorMin = new Vector2(0.5f, 0f);
            btnRowRect.anchorMax = new Vector2(0.5f, 0f);
            btnRowRect.pivot = new Vector2(0.5f, 0f);
            btnRowRect.anchoredPosition = new Vector2(0, 24);
            btnRowRect.sizeDelta = new Vector2(220, 44);
            var confirmBtn = CreateSmallButton(btnRow.transform, "确定", new Vector2(0.5f, 0.5f), new Vector2(-55, 0), 88, 36);
            confirmBtn.onClick.AddListener(ApplyFilterAndClose);
            var cancelBtn = CreateSmallButton(btnRow.transform, "取消", new Vector2(0.5f, 0.5f), new Vector2(55, 0), 88, 36);
            cancelBtn.onClick.AddListener(CloseFilterPanel);
        }

        /// <summary> 创建筛选区块：标题 + 多行选项（每行最多 maxPerRow 个），扩展包等排在点数多行之后 </summary>
        private static Transform CreateFilterCheckboxSection(Transform contentParent, string sectionLabel, string[] options, float labelW, float rowH, int maxPerRow)
        {
            var section = new GameObject("Section_" + sectionLabel);
            section.transform.SetParent(contentParent, false);
            var sectionLE = section.AddComponent<LayoutElement>();
            sectionLE.flexibleWidth = 1;
            var sectionCsf = section.AddComponent<ContentSizeFitter>();
            sectionCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sectionCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var sectionVlg = section.AddComponent<VerticalLayoutGroup>();
            sectionVlg.spacing = 4f;
            sectionVlg.childAlignment = TextAnchor.UpperLeft;
            sectionVlg.childControlWidth = true;
            sectionVlg.childControlHeight = false;
            sectionVlg.childForceExpandWidth = true;
            sectionVlg.childForceExpandHeight = false;

            var label = CreateText(section.transform, sectionLabel, 22);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(labelW, rowH);
            var labelLE = label.AddComponent<LayoutElement>();
            labelLE.preferredWidth = labelW;
            labelLE.preferredHeight = rowH;

            for (int start = 0; start < options.Length; start += maxPerRow)
            {
                int count = Mathf.Min(maxPerRow, options.Length - start);
                var rowGo = new GameObject("Row");
                rowGo.transform.SetParent(section.transform, false);
                var rowLE = rowGo.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 30f;
                rowLE.flexibleWidth = 1;
                var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = true;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = false;
                for (int i = 0; i < count; i++)
                    CreateFilterToggle(rowGo.transform, options[start + i]);
            }
            return section.transform;
        }

        private static Toggle CreateFilterToggle(Transform parent, string labelText)
        {
            var go = new GameObject("Toggle_" + labelText);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            float labelLen = Mathf.Min(100f, 14f * labelText.Length + 24f);
            rect.sizeDelta = new Vector2(24f + 8f + labelLen, 28f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = rect.sizeDelta.x;
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            var check = new GameObject("Check");
            check.transform.SetParent(go.transform, false);
            var checkRect = check.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(12, 0);
            checkRect.sizeDelta = new Vector2(20, 20);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.3f, 0.7f, 0.4f, 1f);
            toggle.graphic = checkImg;
            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(36, 2);
            labelRect.offsetMax = new Vector2(-4, -2);
            var text = label.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 20;
            text.color = Color.white;
            return toggle;
        }

        private static void CollectSelectedToggles(Transform t, HashSet<string> set)
        {
            var toggle = t.GetComponent<Toggle>();
            if (toggle != null && toggle.isOn)
            {
                var label = t.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label != null && !string.IsNullOrEmpty(label.text))
                    set.Add(label.text.Trim());
                return;
            }
            for (int i = 0; i < t.childCount; i++)
                CollectSelectedToggles(t.GetChild(i), set);
        }

        private static HashSet<string> GetSelectedValuesFromRow(Transform section)
        {
            var set = new HashSet<string>();
            if (section != null) CollectSelectedToggles(section, set);
            return set;
        }

        private static void ApplyFilterAndClose()
        {
            string nameStr = _filterNameInput != null ? (_filterNameInput.text ?? "").Trim() : "";
            var factions = GetSelectedValuesFromRow(_filterFactionRow);
            var suits = GetSelectedValuesFromRow(_filterSuitRow);
            var ranks = GetSelectedValuesFromRow(_filterRankRow);
            var expansions = GetSelectedValuesFromRow(_filterExpansionRow);
            _cardIds = CardTableLoader.GetFilteredCardIds(nameStr, factions, suits, ranks, expansions);
            _totalPages = Mathf.Max(1, (_cardIds.Count + CardsPerPage - 1) / CardsPerPage);
            _currentPage = 0;
            UpdateGridCellSize();
            RefreshCardGrid();
            UpdatePageLabel();
            CloseFilterPanel();
        }

        private static GameObject CreateInputField(Transform parent)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.42f, 0.52f, 1f);
            var input = go.AddComponent<TMP_InputField>();
            var textArea = new GameObject("Text");
            textArea.transform.SetParent(go.transform, false);
            var text = textArea.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 18;
            text.color = Color.white;
            var textRect = textArea.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);
            input.textViewport = textRect;
            input.textComponent = text;
            input.targetGraphic = img;
            return go;
        }

        private static void OpenFilterPanel()
        {
            if (_filterPanelRoot != null)
            {
                _filterPanelRoot.transform.SetAsLastSibling();
                _filterPanelRoot.SetActive(true);
                if (_filterScrollContent != null && _filterScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_filterScrollContent);
                    _filterScrollRect.verticalNormalizedPosition = 1f;
                }
            }
        }

        private static void CloseFilterPanel()
        {
            _filterPanelRoot.SetActive(false);
        }

        private static void BuildRightArea()
        {
            _rightArea = new GameObject("RightDeckList");
            _rightArea.transform.SetParent(_root.transform, false);
            var rightRect = _rightArea.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.7f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.offsetMin = new Vector2(10, 20);
            rightRect.offsetMax = new Vector2(-20, -80);
            _rightArea.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.2f, 1f);

            var deckTitle = CreateText(_rightArea.transform, "我的牌组", 26);
            var deckTitleRect = deckTitle.GetComponent<RectTransform>();
            deckTitleRect.anchorMin = new Vector2(0.5f, 1f);
            deckTitleRect.anchorMax = new Vector2(0.5f, 1f);
            deckTitleRect.pivot = new Vector2(0.5f, 1f);
            deckTitleRect.anchoredPosition = new Vector2(0, -12);
            deckTitleRect.sizeDelta = new Vector2(280, 36);

            CreateDeckScrollView();
            deckTitle.transform.SetAsLastSibling();
        }

        private static void BuildDeckEditOverlay()
        {
            if (_rightArea == null) return;
            _deckEditRoot = new GameObject("DeckEditOverlay");
            _deckEditRoot.transform.SetParent(_rightArea.transform, false);
            var overlayRect = _deckEditRoot.AddComponent<RectTransform>();
            SetFullRect(overlayRect);
            var overlayImg = _deckEditRoot.AddComponent<Image>();
            overlayImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            overlayImg.raycastTarget = true;
            _deckEditRoot.SetActive(false);

            float y = -14f;
            float rowH = 36f;
            float gap = 10f;

            var nameInputGo = CreateInputField(_deckEditRoot.transform);
            var nameInputR = nameInputGo.GetComponent<RectTransform>();
            nameInputR.anchorMin = new Vector2(0f, 1f);
            nameInputR.anchorMax = new Vector2(1f, 1f);
            nameInputR.pivot = new Vector2(0.5f, 1f);
            nameInputR.anchoredPosition = new Vector2(0, y);
            nameInputR.sizeDelta = new Vector2(-24, 32);
            var nameLE = nameInputGo.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 32f;
            _deckEditNameInput = nameInputGo.GetComponent<TMP_InputField>();
            _deckEditNameInput.textComponent.fontSize = 20;
            (_deckEditNameInput.textComponent as TextMeshProUGUI).alignment = TextAlignmentOptions.Center;
            y -= 36f + gap;

            var suitLabel = CreateText(_deckEditRoot.transform, "从牌组中移除花色（单选）", 18);
            var suitLabelRect = suitLabel.GetComponent<RectTransform>();
            suitLabelRect.anchorMin = new Vector2(0f, 1f);
            suitLabelRect.anchorMax = new Vector2(1f, 1f);
            suitLabelRect.pivot = new Vector2(0.5f, 1f);
            suitLabelRect.anchoredPosition = new Vector2(0, y);
            suitLabelRect.sizeDelta = new Vector2(-32, rowH);
            y -= rowH + gap;

            var suitRow = new GameObject("SuitRow");
            suitRow.transform.SetParent(_deckEditRoot.transform, false);
            var suitRowRect = suitRow.AddComponent<RectTransform>();
            suitRowRect.anchorMin = new Vector2(0f, 1f);
            suitRowRect.anchorMax = new Vector2(1f, 1f);
            suitRowRect.pivot = new Vector2(0.5f, 1f);
            suitRowRect.anchoredPosition = new Vector2(0, y);
            suitRowRect.sizeDelta = new Vector2(-32, rowH);
            var suitGroup = suitRow.AddComponent<ToggleGroup>();
            suitGroup.allowSwitchOff = true;
            var suitHlg = suitRow.AddComponent<HorizontalLayoutGroup>();
            suitHlg.spacing = 8f;
            suitHlg.childAlignment = TextAnchor.MiddleLeft;
            suitHlg.childControlWidth = false;
            suitHlg.childControlHeight = true;
            suitHlg.childForceExpandWidth = false;
            suitHlg.childForceExpandHeight = true;
            _deckEditSuitToggles = new Toggle[DeckSuitOptions.Length];
            for (int i = 0; i < DeckSuitOptions.Length; i++)
            {
                var t = CreateDeckSuitToggle(suitRow.transform, DeckSuitOptions[i], suitGroup);
                _deckEditSuitToggles[i] = t;
            }
            y -= rowH + gap + 8f;

            var scrollGo = new GameObject("DeckEditScroll");
            scrollGo.transform.SetParent(_deckEditRoot.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(12, 88);
            scrollRect.offsetMax = new Vector2(-12, -148);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            SetFullRect(viewportRect);
            viewport.AddComponent<Image>().color = new Color(0.15f, 0.17f, 0.22f, 0.98f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<DropZone>().IsDeckZone = true;

            var content = new GameObject("DeckEditContent");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 300);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8f;
            contentVlg.padding = new RectOffset(8, 8, 8, 8);
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            _deckEditScrollRect = scroll;
            _deckEditCardContent = content.transform;

            var countGo = CreateText(_deckEditRoot.transform, "0/3", 18);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(0.5f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.anchoredPosition = new Vector2(0, 52);
            countRect.sizeDelta = new Vector2(80, 24);
            _deckEditCardCountLabel = countGo.GetComponent<TextMeshProUGUI>();

            var backBtn = CreateSmallButton(_deckEditRoot.transform, "返回", new Vector2(0.5f, 0f), new Vector2(0, 24), 120, 40);
            backBtn.onClick.AddListener(CloseDeckEditAndSave);
        }

        private static Toggle CreateDeckSuitToggle(Transform parent, string label, ToggleGroup group)
        {
            var go = new GameObject("Suit_" + label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72, 32);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 72f;
            le.preferredHeight = 32f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            var toggle = go.AddComponent<Toggle>();
            toggle.group = group;
            toggle.targetGraphic = img;
            var colors = toggle.colors;
            colors.normalColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.38f, 0.45f, 1f);
            colors.pressedColor = new Color(0.3f, 0.45f, 0.35f, 1f);
            toggle.colors = colors;
            var check = new GameObject("Check");
            check.transform.SetParent(go.transform, false);
            var checkRect = check.AddComponent<RectTransform>();
            SetFullRect(checkRect);
            checkRect.offsetMin = new Vector2(4, 4);
            checkRect.offsetMax = new Vector2(-4, -4);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.4f, 0.75f, 0.5f, 1f);
            toggle.graphic = checkImg;
            var textGo = CreateText(go.transform, label, 16);
            var textRect = textGo.GetComponent<RectTransform>();
            SetFullRect(textRect);
            return toggle;
        }

        private static void CreateDeckScrollView()
        {
            var scrollGo = new GameObject("DeckScrollRect");
            scrollGo.transform.SetParent(_rightArea.transform, false);
            var scrollRectRect = scrollGo.AddComponent<RectTransform>();
            scrollRectRect.anchorMin = new Vector2(0f, 0f);
            scrollRectRect.anchorMax = new Vector2(1f, 1f);
            scrollRectRect.offsetMin = new Vector2(12, 56);
            scrollRectRect.offsetMax = new Vector2(-12, -52);

            var viewport = new GameObject("DeckViewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 0.95f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("DeckContent");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 400);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _deckScrollRect = scrollGo.AddComponent<ScrollRect>();
            _deckScrollRect.content = contentRect;
            _deckScrollRect.viewport = viewportRect;
            _deckScrollRect.horizontal = false;
            _deckScrollRect.vertical = true;
            _deckScrollRect.scrollSensitivity = 20f;
            _deckContent = content.transform;
        }

        private static void PrevPage()
        {
            _currentPage = Mathf.Max(0, _currentPage - 1);
            RefreshCardGrid();
            UpdatePageLabel();
        }

        private static void NextPage()
        {
            _currentPage = Mathf.Min(_totalPages - 1, _currentPage + 1);
            RefreshCardGrid();
            UpdatePageLabel();
        }

        private static void UpdatePageLabel()
        {
            var paginationRoot = _leftArea.transform.Find("Pagination");
            if (paginationRoot == null) return;
            var label = paginationRoot.Find("PageLabel")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = "第 " + (_currentPage + 1) + " / " + _totalPages + " 页";
        }

        private static void RefreshDeckList()
        {
            foreach (Transform t in _deckContent)
                Object.Destroy(t.gameObject);

            for (int i = 0; i < _decks.Count; i++)
            {
                int index = i;
                var deck = _decks[i];
                var row = CreateDeckButton(deck.DisplayName ?? ("牌组" + (i + 1)), true, () => OpenDeckEdit(index), () => DeleteDeck(index));
                row.transform.SetParent(_deckContent, false);
            }

            if (_decks.Count < MaxDecks)
            {
                var createRow = CreateDeckButton("创建", false, OnCreateDeck, null);
                createRow.transform.SetParent(_deckContent, false);
            }
        }

        private static GameObject CreateDeckButton(string label, bool hasDeleteBtn, System.Action onSelect, System.Action onDelete)
        {
            var row = new GameObject("DeckItem");
            row.AddComponent<RectTransform>();
            row.AddComponent<Image>().color = new Color(0.28f, 0.32f, 0.4f, 1f);
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = row.GetComponent<Image>();

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
            textRect.offsetMax = new Vector2(-14, 0);

            if (hasDeleteBtn && onDelete != null)
            {
                var delBtn = new GameObject("DeleteBtn");
                delBtn.transform.SetParent(row.transform, false);
                var delRect = delBtn.AddComponent<RectTransform>();
                delRect.anchorMin = new Vector2(1f, 1f);
                delRect.anchorMax = new Vector2(1f, 1f);
                delRect.pivot = new Vector2(1f, 1f);
                delRect.anchoredPosition = new Vector2(-8, -8);
                delRect.sizeDelta = new Vector2(36, 36);
                var delImg = delBtn.AddComponent<Image>();
                delImg.color = new Color(0.5f, 0.25f, 0.25f, 1f);
                var delText = CreateText(delBtn.transform, "删除", 14);
                SetFullRect(delText.GetComponent<RectTransform>());
                var delButton = delBtn.AddComponent<Button>();
                delButton.onClick.AddListener(() => { onDelete?.Invoke(); });
                delButton.targetGraphic = delImg;
            }

            if (onSelect != null)
                btn.onClick.AddListener(() => onSelect());
            return row;
        }

        private static void OnCreateDeck()
        {
            var deck = new DeckData();
            _decks.Insert(0, deck);
            deck.DisplayName = "牌组" + _decks.Count;
            SaveDecks();
            RefreshDeckList();
        }

        private static void DeleteDeck(int index)
        {
            if (index < 0 || index >= _decks.Count) return;
            if (_currentEditingDeckIndex == index)
            {
                CloseDeckEditAndSave();
                _currentEditingDeckIndex = -1;
            }
            else if (_currentEditingDeckIndex > index)
                _currentEditingDeckIndex--;
            _decks.RemoveAt(index);
            SaveDecks();
            RefreshDeckList();
        }

        private static void OpenDeckEdit(int index)
        {
            if (index < 0 || index >= _decks.Count) return;
            _currentEditingDeckIndex = index;
            var deck = _decks[index];
            if (_deckEditNameInput != null)
            {
                _deckEditNameInput.text = deck.DisplayName ?? "";
                _deckEditNameInput.onEndEdit.RemoveAllListeners();
                _deckEditNameInput.onEndEdit.AddListener(s =>
                {
                    if (!string.IsNullOrWhiteSpace(s)) deck.DisplayName = s.Trim();
                });
            }
            for (int i = 0; i < _deckEditSuitToggles.Length; i++)
            {
                var t = _deckEditSuitToggles[i];
                t.SetIsOnWithoutNotify(deck.RemovedSuit == DeckSuitOptions[i]);
                var bgImg = t.targetGraphic as Image;
                if (bgImg != null) bgImg.color = t.isOn ? new Color(0.28f, 0.5f, 0.35f, 1f) : new Color(0.25f, 0.28f, 0.35f, 1f);
                t.onValueChanged.RemoveAllListeners();
                int j = i;
                t.onValueChanged.AddListener(ison =>
                {
                    if (ison) deck.RemovedSuit = DeckSuitOptions[j];
                    else if (deck.RemovedSuit == DeckSuitOptions[j]) deck.RemovedSuit = null;
                    var img = _deckEditSuitToggles[j].targetGraphic as Image;
                    if (img != null) img.color = ison ? new Color(0.28f, 0.5f, 0.35f, 1f) : new Color(0.25f, 0.28f, 0.35f, 1f);
                });
            }
            if (_deckEditSuitToggles[0].group != null)
                _deckEditSuitToggles[0].group.allowSwitchOff = true;
            RefreshDeckEditCards();
            UpdateDeckEditCardCountLabel();
            _deckEditRoot.transform.SetAsLastSibling();
            _deckEditRoot.SetActive(true);
            RefreshCardGrid();
        }

        private static void CloseDeckEditAndSave()
        {
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return;
            var deck = _decks[_currentEditingDeckIndex];
            deck.RemovedSuit = null;
            for (int i = 0; i < _deckEditSuitToggles.Length; i++)
            {
                if (_deckEditSuitToggles[i].isOn)
                {
                    deck.RemovedSuit = DeckSuitOptions[i];
                    break;
                }
            }
            _deckEditRoot.SetActive(false);
            _currentEditingDeckIndex = -1;
            SaveDecks();
            RefreshDeckList();
            RefreshCardGrid();
        }

        private static void LoadDecks()
        {
            _decks.Clear();
            if (!AccountManager.IsRegistered()) return;
            var json = AccountManager.LoadUserData("Decks");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var save = JsonUtility.FromJson<DeckListSave>(json);
                if (save?.Decks != null)
                {
                    foreach (var d in save.Decks)
                    {
                        if (d == null) continue;
                        if (d.CardIds == null) d.CardIds = new List<string>();
                        _decks.Add(d);
                    }
                }
            }
            catch (System.Exception e) { UnityEngine.Debug.LogWarning("LoadDecks: " + e.Message); }
        }

        private static void SaveDecks()
        {
            if (!AccountManager.IsRegistered()) return;
            try
            {
                var save = new DeckListSave { Decks = _decks.ToArray() };
                var json = JsonUtility.ToJson(save);
                AccountManager.SaveUserData("Decks", json);
            }
            catch (System.Exception e) { UnityEngine.Debug.LogWarning("SaveDecks: " + e.Message); }
        }

        private static void RefreshDeckEditCards()
        {
            if (_deckEditCardContent == null) return;
            for (int i = _deckEditCardContent.childCount - 1; i >= 0; i--)
                Object.Destroy(_deckEditCardContent.GetChild(i).gameObject);
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return;
            var deck = _decks[_currentEditingDeckIndex];
            foreach (var cardId in deck.CardIds)
            {
                var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cardId));
                if (data == null) continue;
                var cardGo = CreateDraggableDeckCard(cardId, data);
                cardGo.transform.SetParent(_deckEditCardContent, false);
            }
            UpdateDeckEditCardCountLabel();
        }

        private static void UpdateDeckEditCardCountLabel()
        {
            if (_deckEditCardCountLabel == null) return;
            int count = 0;
            if (_currentEditingDeckIndex >= 0 && _currentEditingDeckIndex < _decks.Count)
                count = _decks[_currentEditingDeckIndex].CardIds.Count;
            _deckEditCardCountLabel.text = count + "/3";
        }

        private static GameObject CreateDraggableDeckCard(string cardId, CardData data)
        {
            var root = new GameObject("DeckCard_" + cardId);
            root.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 72);
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.26f, 0.34f, 1f);
            var face = new GameObject("Face");
            face.transform.SetParent(root.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.anchorMin = new Vector2(0f, 0.5f);
            faceRect.anchorMax = new Vector2(0f, 0.5f);
            faceRect.pivot = new Vector2(0f, 0.5f);
            faceRect.anchoredPosition = new Vector2(8, 0);
            faceRect.sizeDelta = new Vector2(50, 50 * CardPixelH / CardPixelW);
            var img = face.AddComponent<Image>();
            var view = root.AddComponent<CardView>();
            view.Data = data;
            view.FaceImage = img;
            view.LoadFaceSprite();
            var drag = root.AddComponent<CompendiumDragDrop>();
            drag.CardId = cardId;
            drag.IsDeckCard = true;
            var label = CreateText(root.transform, data?.RoleName ?? cardId, 16);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(62, 0);
            labelRect.offsetMax = new Vector2(-8, 12);
            return root;
        }

        /// <summary> 具有角色 tag 且非霸业（霸业有特殊逻辑单独处理），用于同名互斥规则 </summary>
        private static bool HasRelevantRoleTag(CardData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.RoleTag)) return false;
            var t = data.RoleTag.Trim();
            return !string.Equals(t, "霸业", System.StringComparison.Ordinal);
        }

        private static bool DeckHasSameRoleName(DeckData deck, string roleName, string excludeCardId)
        {
            if (string.IsNullOrEmpty(roleName) || deck?.CardIds == null) return false;
            foreach (var cid in deck.CardIds)
            {
                if (cid == excludeCardId) continue;
                var c = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
                if (c != null && string.Equals(c.RoleName?.Trim(), roleName.Trim(), System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary> 牌组中是否存在同名角色且涉及“有效角色 tag”（本方或对方有 tag 且非霸业）则不可同组 </summary>
        private static bool CannotAddDueToSameNameRoleTag(string cardId)
        {
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return false;
            var deck = _decks[_currentEditingDeckIndex];
            if (deck.CardIds.Count >= MaxCardsPerDeck || deck.CardIds.Contains(cardId)) return false;
            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cardId));
            if (data == null || string.IsNullOrEmpty(data.RoleName)) return false;
            foreach (var cid in deck.CardIds)
            {
                if (cid == cardId) continue;
                var c = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
                if (c == null || !string.Equals(c.RoleName?.Trim(), data.RoleName.Trim(), System.StringComparison.Ordinal)) continue;
                if (HasRelevantRoleTag(data) || HasRelevantRoleTag(c)) return true;
            }
            return false;
        }

        private static bool CanAddCardToEditingDeck(string cardId)
        {
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return false;
            var deck = _decks[_currentEditingDeckIndex];
            if (deck.CardIds.Count >= MaxCardsPerDeck || deck.CardIds.Contains(cardId)) return false;
            if (CannotAddDueToSameNameRoleTag(cardId)) return false;
            return true;
        }

        public static void TryAddCardToEditingDeck(string cardId)
        {
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return;
            if (!CanAddCardToEditingDeck(cardId)) return;
            var deck = _decks[_currentEditingDeckIndex];
            deck.CardIds.Add(cardId);
            RefreshDeckEditCards();
            RefreshCardGrid();
        }

        public static void TryRemoveCardFromEditingDeck(string cardId)
        {
            if (_currentEditingDeckIndex < 0 || _currentEditingDeckIndex >= _decks.Count) return;
            var deck = _decks[_currentEditingDeckIndex];
            deck.CardIds.Remove(cardId);
            if (_cardIds != null && !_cardIds.Contains(cardId))
                _cardIds.Add(cardId);
            SortCardIdsByNumber();
            RefreshCardGrid();
            RefreshDeckEditCards();
        }

        private static void SortCardIdsByNumber()
        {
            _cardIds.Sort((a, b) =>
            {
                int na = CardTableLoader.CardIdToNumber(a);
                int nb = CardTableLoader.CardIdToNumber(b);
                return na.CompareTo(nb);
            });
        }

        private static Button CreateBackButton()
        {
            var go = new GameObject("BackButton");
            go.AddComponent<Image>().color = new Color(0.3f, 0.35f, 0.45f, 1f);
            var button = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24, -24);
            rect.sizeDelta = new Vector2(120, 44);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "返回";
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            SetFullRect(textGo.GetComponent<RectTransform>());
            button.onClick.AddListener(OnBack);
            return button;
        }

        private static Button CreateFilterButton()
        {
            var go = new GameObject("FilterButton");
            go.AddComponent<Image>().color = new Color(0.3f, 0.35f, 0.45f, 1f);
            var button = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24, -24);
            rect.sizeDelta = new Vector2(120, 44);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "筛选";
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            SetFullRect(textGo.GetComponent<RectTransform>());
            button.onClick.AddListener(OpenFilterPanel);
            return button;
        }

        /// <summary> 从图鉴返回时若已设置则先执行（如从选择套牌页进入组牌时，返回应回到选择套牌页）。 </summary>
        private static System.Action _onBackReturnTo;

        public static void SetReturnTarget(System.Action onBackShow)
        {
            _onBackReturnTo = onBackShow;
        }

        /// <summary> 从本地重新加载牌组数据（不打开图鉴界面）。主菜单点「开始游戏」或进入选择套牌页前调用，确保套牌列表为最新。 </summary>
        public static void EnsureDecksLoaded()
        {
            LoadDecks();
        }

        /// <summary> 获取当前牌组列表（与图鉴右侧顺序一致，从上到下）。 </summary>
        public static System.Collections.Generic.List<DeckData> GetDecks()
        {
            return new System.Collections.Generic.List<DeckData>(_decks);
        }

        private static void OnBack()
        {
            _detailRoot.SetActive(false);
            Hide();
            if (_onBackReturnTo != null)
            {
                var act = _onBackReturnTo;
                _onBackReturnTo = null;
                act?.Invoke();
            }
            else
                MainMenuUI.Show();
        }

        private static GameObject CreateText(Transform parent, string content, int fontSize)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            if (TMPHelper.GetDefaultFont() != null) t.font = TMPHelper.GetDefaultFont();
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            return go;
        }

        private static Button CreateSmallButton(Transform parent, string label, Vector2 anchor, Vector2 pos, float w, float h)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.28f, 0.35f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(w, h);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            SetFullRect(textGo.GetComponent<RectTransform>());
            return btn;
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary> 重新计算网格格子尺寸并刷新卡牌与页码（供 OnEnable 下一帧调用，避免布局未完成时比例错误） </summary>
        public static void RefreshGridLayout()
        {
            UpdateGridCellSize();
            RefreshCardGrid();
            UpdatePageLabel();
        }

        public static void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
                if (_detailRoot != null) _detailRoot.SetActive(false);
                if (_filterPanelRoot != null) _filterPanelRoot.SetActive(false);
                if (_deckEditRoot != null) _deckEditRoot.SetActive(false);
                _currentEditingDeckIndex = -1;
                LoadDecks();
                RefreshDeckList();
                _cardIds = CardTableLoader.GetCompendiumCardIds();
                _totalPages = Mathf.Max(1, (_cardIds.Count + CardsPerPage - 1) / CardsPerPage);
                _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);
                UpdateGridCellSize();
                RefreshCardGrid();
                UpdatePageLabel();
            }
        }

        public static void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }
    }
}
