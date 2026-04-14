using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>被动同节点待点技能：强制技与非强制技高亮区分。</summary>
    public enum PassiveNodeSkillHighlightKind
    {
        None = 0,
        Mandatory = 1,
        Optional = 2,
        /// <summary>PVE 敌方武将：可点击查看技能说明（不可代为发动）。</summary>
        OpponentInfo = 3
    }

    /// <summary>
    /// 闂佽娴烽弫鎼佸箠閹剧粯鍎戞い鎺嶈兌閳绘棃鏌ゆ慨鎰偓鏍р枍閺囥垺鈷掗柛顐亝缁舵稓绱掗崣妯哄祮鐎规洩绲挎禒锕傛倷椤掑倸骞嶆繝娈垮枟缁绘劗绮婚幋锔藉剭闁靛繈鍊栭崑瀣偓鍏夊亾閻庯綆鍓涢ˇ鈺呮⒑?3 闁诲孩顔栭崰姘垔鐎靛憡顫曟繝闈涱儏绾偓婵犵數濮村ú锕傛嚐閻斿吋鐓ユ繛鎴烇供濡插爼鏌嶈閸忔稑顪冮幒妤佸殘闁瑰鍋涚欢鐐淬亜閺冨倻鍙撴い鎾卞灩鐟欙箓鏌熺€涙绠撻柣搴☆煼閺屾盯骞橀悷鎵闂佹悶鍊曢柊锝嗕繆鐎涙ɑ濯撮柛娑橈功椤︺儵姊洪崨濠呭闁绘锕﹂埀顒€鐏氱划鎾诲蓟閿曞偆鏁婇悶娑掆偓鍏呭婵炶揪绲介幖顐β烽崒鐐寸厱妞ゆ劑鍨洪ˉ娆戠磼濡も偓閼活垶顢氶敐澶婄妞ゅ繐瀚烽弶娲⒑閸撴彃浜滄俊顐㈩嚟濡叉劙鏌嗗鍛攨闂佹寧绋戠€氼剟路閸岀偞鐓熼柟閭﹀枟閸も偓闂佹悶鍊ら崳锝咁嚕閵娾晛绠氱憸瀣閿濆憘娑㈠级閸噮浼€闂佸搫妫岄崑鎾绘⒑鐟欏嫪鑸柛搴ょ簿閵囨劕鈽夊▎鎴犳嚌濠电偞鍨堕敋闁藉啰鍠栭弻?闂備浇澹堟ご绋款潖閼姐倗绀婂璺烘湰鐎氼剟鏌涢幇鍏哥凹闁哄棗绻橀弻?
    /// </summary>
    public static class GameUI
    {
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;
        private const float CardAspectW = 1016f;
        private const float CardAspectH = 1488f;

        private static GameObject _root;
        private static Transform _gameUiBackgroundTransform;
        private static BattleState _state;
        private static bool _isOnlineMode;
        private static string _localPlayerDisplayName = "你";
        private static string _opponentPlayerDisplayName = "对手";
        private static Button _turnButton;
        private static TextMeshProUGUI _turnButtonText;
        private static Button _endPassiveNodeButton;
        private static TextMeshProUGUI _endPassiveNodeButtonText;
        private static Dictionary<(int generalIndex, int skillIndex), GameStartSkillLineEntry> _gameStartMandatoryPending;
        private static Dictionary<(int generalIndex, int skillIndex), GameStartSkillLineEntry> _gameStartOptionalPending;
        private static Action _gameStartPassiveNodeOnComplete;
        private static Button _genericAttackButton;
        private static TextMeshProUGUI _genericAttackButtonText;
        private static TextMeshProUGUI _phaseLabel;
        private static TextMeshProUGUI _deckCountTooltip;
        private static GameObject _deckTooltipRoot;
        private static Button _discardButton;
        private static TextMeshProUGUI _discardButtonLabel;
        private static GameObject _discardPopupRoot;
        private static RectTransform _discardPopupPanelRt;
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
        private static GameObject _jushuiPopupRoot;
        private static Transform _jushuiContent;
        private static TextMeshProUGUI _jushuiTitle;
        private static TextMeshProUGUI _jushuiSubtitle;
        private static Button _jushuiConfirmBtn;
        private static Button _jushuiCancelBtn;
        private static int _jushuiGeneralIndex;
        private static int _jushuiSkillIndex;
        private static int _jushuiMaxPick;
        private static readonly List<int> _jushuiSelectedOrder = new List<int>();
        private static readonly HashSet<int> _jushuiSelectedSet = new HashSet<int>();
        private static readonly List<PokerCard> _jushuiSnapshot = new List<PokerCard>();
        private static GameObject _moralePopupRoot;
        private static Button[] _moraleEffectButtons = new Button[3];
        private static Transform _playerHandContent;
        private static Transform _opponentHandContent;
        private static TextMeshProUGUI _playerHandLabel;
        private static TextMeshProUGUI _opponentHandLabel;
        private static Transform _handHoverOverlay;
        private static RectTransform _playerHandDockRt;
        private static RectTransform _playerHandLabelRt;
        private static RectTransform _playerHandFrameRt;
        private static RectTransform _playerHandViewportRt;
        private static GameObject _playerHandSortButtonGo;
        private static bool _playerHandExpanded;
        private static GameObject _playerHandOutsideDismissOverlayGo;
        private static RightMouseScrollRect _playerHandRightMouseScroll;
        private const float HandDockMargin = 24f;
        private const float CompactHandW = 420f;
        private const float CompactHandH = 160f;
        private const float ExpandedHandOuterH = 280f;
        private static float ExpandedHandOuterW => Mathf.Min(1520f, RefWidth - 48f);
        private static GameObject _playedZoneRoot;
        private static Transform _playedZoneContent;
        private static GameObject _choicePopupRoot;
        private static TextMeshProUGUI _choicePopupTitle;
        private static Transform _choicePopupContent;
        private static GameObject _attackPatternPopupRoot;
        private static TextMeshProUGUI _attackPatternTitle;
        private static Transform _attackPatternContent;
        private static bool _genericAttackShapePopupAfterPendingConfigure;
        private static GameObject _battleFlowLogDock;
        private static Transform _battleFlowLogListContent;
        private static GameObject _battleFlowLogModalRoot;
        private static TextMeshProUGUI _battleFlowLogModalTitle;
        private static TextMeshProUGUI _battleFlowLogModalBody;
        private static Button _battleFlowLogAllButton;
        private static ScrollRect _battleFlowLogModalScroll;
        private static ScrollRect _battleFlowLogDockScroll;
        private static RectTransform _battleFlowLogModalContentRt;

        private const int BattleFlowLogSideVisibleMax = 3;

        /// <summary>
        /// 闂佽崵濮村ú顓㈠绩闁秵鍎戦柟娈垮枙濞岊亪鏌￠崶鈺佇炲ù鐘崇洴閺屾稑螖鐎ｎ剛锛熸繝鈷€鍕疄鐎殿喓鍔戦獮鍡氼槹濞存粣缍侀弻銊モ槈濡厧顤€濡炪倖甯為崰鎾寸閹间礁骞㈡繛鍡楁禋閺夋椽姊洪幐搴ｂ槈妞わ富鍙冮崺鈧い鎺嗗亾闁稿﹦顭堥锝嗙節濮橆剚顥濋梺鑽ゅ枔婢ф绱為崱娑欑厱婵炲棙锚閻忋儵鏌ｉ妸褍鏋涢柟顔荤矙閳ワ箓骞掗弮鍌ゆХ濠电偞鎸婚懝楣冨Φ濡壈濮虫い鎺戝鐟欙箓骞栨潏鍓ф偧闁轰線绠栭弻銈嗘綇閵婏腹鎷荤紓鍌氱Т椤﹂潧鐣烽妷褌娌紓浣靛灩婵￠亶姊?
        /// </summary>
        public static System.Action<string, bool> OnRequestCardEnlarge;
        public static Transform GetRootTransform() => _root != null ? _root.transform : null;
        public static Transform GetHandHoverOverlay() => _handHoverOverlay;
        public static bool IsPlayerHandExpanded() => _playerHandExpanded;

        /// <summary>屏幕坐标是否落在打出区矩形内（拖拽松手时与射线结果配合判定）。</summary>
        public static bool IsScreenPointOverPlayedZone(Vector2 screenPosition)
        {
            if (_playedZoneRoot == null)
                return false;
            var rt = _playedZoneRoot.transform as RectTransform;
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition, null);
        }

        public static void CollapsePlayerHandIfExpanded()
        {
            if (!_playerHandExpanded)
                return;
            SetPlayerHandExpanded(false);
        }

        private static void Trace(string message)
        {
            RuntimeTraceLogger.Trace("GameUI", message);
        }

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
            _gameUiBackgroundTransform = bg.transform;

            BuildDivider();
            BuildTurnButton();
            BuildEndPassiveNodeButton();
            BuildGenericAttackButton();
            BuildDeckAndDiscardArea();
            BuildHandAndCharacterFrames();
            BuildHandExpandOutsideDismissOverlay();
            BuildPlayedZone();
            BuildOpponentGenerals();
            BuildPlayerGenerals();
            BuildMoraleAreas();
            BuildCardEnlargeOverlay();
            BuildDiscardPopup();
            BuildMoralePopup();
            BuildChoicePopup();
            BuildAttackPatternPopup();
            BuildDiscardPhasePopup();
            BuildJushuiDuanqiaoPopup();
            BuildBattleFlowLogUI();
            BattleFlowLog.Changed += RefreshBattleFlowLogPanel;
            BuildVictoryDefeatPopups();
            RegisterCardEnlarge();
            GeneralCardHolder.OnSkillButtonClicked = OnGeneralSkillClicked;
            if (_phaseLabel != null && _phaseLabel.transform.parent != null)
                _phaseLabel.transform.parent.SetAsLastSibling();
            if (_battleFlowLogDock != null)
                _battleFlowLogDock.transform.SetAsLastSibling();
            if (_battleFlowLogModalRoot != null)
                _battleFlowLogModalRoot.transform.SetAsLastSibling();
            if (_turnButton != null)
                _turnButton.transform.SetAsLastSibling();
            if (_endPassiveNodeButton != null)
                _endPassiveNodeButton.transform.SetAsLastSibling();
            if (_genericAttackButton != null)
                _genericAttackButton.transform.SetAsLastSibling();
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
            var vText = CreateGameText(vPanel.transform, "\u80dc\u5229", 48);
            var vTextRect = vText.GetComponent<RectTransform>();
            vTextRect.anchorMin = vTextRect.anchorMax = new Vector2(0.5f, 1f);
            vTextRect.pivot = new Vector2(0.5f, 1f);
            vTextRect.anchoredPosition = new Vector2(0, -36);
            vTextRect.sizeDelta = new Vector2(320, 60);
            CreateResultPopupButton(vPanel.transform, new Vector2(0, -56), "\u8fd4\u56de\u4e3b\u83dc\u5355");

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
            var dText = CreateGameText(dPanel.transform, "\u5931\u8d25", 48);
            var dTextRect = dText.GetComponent<RectTransform>();
            dTextRect.anchorMin = dTextRect.anchorMax = new Vector2(0.5f, 1f);
            dTextRect.pivot = new Vector2(0.5f, 1f);
            dTextRect.anchoredPosition = new Vector2(0, -36);
            dTextRect.sizeDelta = new Vector2(320, 60);
            CreateResultPopupButton(dPanel.transform, new Vector2(0, -56), "\u8fd4\u56de\u4e3b\u83dc\u5355");
        }

        /// <summary> 闂佽閰ｅ褍锕㈡潏鈺傛殰闁惧繐鍘滈崑鎾绘偡閹殿喖鐓熼梺閫炲苯澧板┑顔炬暬瀹曠懓顫濋鐑嗗殼闂侀潧鐗嗛幏瀣船濞差亝鐓ユ繛鎴烆焽閻掔兘鎮楀銉ョ伄闁诡喕鍗抽崺鈧い鎺戝閻撳倿鐓崶銊﹀暗缂佸妞藉濠氬幢濮橆厾銆愰梺鍓茬厛閸ㄤ即顢氶敐澶嬪仭闁哄瀵ч惁鏃€绻濋姀鈥冲姸缂侇喖澧介幉鎾晝閸屾?</summary>
        public static void ApplyDamageToPlayer(int amount)
        {
            if (_state == null || amount <= 0) return;
            _state.Player.CurrentHp = Mathf.Max(0, _state.Player.CurrentHp - amount);
            CheckImmediateGameOverAfterHpChange();
        }

        /// <summary> 闂佽娴烽弫鎼佸箠閹炬椿鏁嬫い鏇楀亾妤犵偛绉堕埀顒婄秵閸犳帡宕戦幘缁樺亹闁告劘灏欐禍锝嗙箾鐎靛壊鍎犻柛濠冩礋椤㈡瑦绻濋崶銊︽珫閻庡厜鍋撻柛鎰电厛娴犙囨煟閻愬鈻撻柛瀣崌濮婃椽骞撻幒鏃傤唶缂傚倸绉撮敃顏勵潖鐠恒劍宕夐柛婵嗗娴犙囨倵濞堝灝鏋ら柟铏崌瀹曠敻顢橀姀鈥虫畱濠电姴锕ら崯顐︽倿濞差亝鐓?</summary>
        public static void ApplyDamageToOpponent(int amount)
        {
            if (_state == null || amount <= 0) return;
            _state.Opponent.CurrentHp = Mathf.Max(0, _state.Opponent.CurrentHp - amount);
            CheckImmediateGameOverAfterHpChange();
        }

        /// <summary>任意路径修改生命后调用：任一方生命≤0 时立刻弹出胜负（先判己方失败）。</summary>
        public static void CheckImmediateGameOverAfterHpChange()
        {
            if (_state == null)
                return;
            RefreshAllFromState();
            if (_state.Player.CurrentHp <= 0)
            {
                ShowDefeatPopup();
                return;
            }

            if (_state.Opponent.CurrentHp <= 0)
                ShowVictoryPopup();
        }

        private static void ShowVictoryPopup()
        {
            CollapsePlayerHandIfExpanded();
            if (_victoryPopupRoot != null) _victoryPopupRoot.SetActive(true);
        }

        private static void ShowDefeatPopup()
        {
            CollapsePlayerHandIfExpanded();
            if (_defeatPopupRoot != null) _defeatPopupRoot.SetActive(true);
        }

        private static void CreateResultPopupButton(Transform parent, Vector2 anchoredPosition, string label)
        {
            var buttonGo = new GameObject("Btn_" + label);
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(180, 46);
            buttonGo.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.34f, 1f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(OnResultPopupReturnToMenu);

            var text = CreateGameText(buttonGo.transform, label, 24);
            SetFullRect(text.GetComponent<RectTransform>());
        }

        private static void OnResultPopupReturnToMenu()
        {
            _ = ReturnToMainMenuAsync();
        }

        private static async Task ReturnToMainMenuAsync()
        {
            if (_victoryPopupRoot != null)
                _victoryPopupRoot.SetActive(false);
            if (_defeatPopupRoot != null)
                _defeatPopupRoot.SetActive(false);

            if (_isOnlineMode)
                await OnlineClientService.DisconnectAsync();

            Hide();
            OnlineLobbyUI.Hide();
            MainMenuUI.Show();
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 闂備礁鎲＄敮妤冪矙閹寸姷纾介柟鍓у仺閳ь剚甯″畷銊︾節閸屾粈绨介梻浣告啞閸旀洟骞婅箛娑樺嚑闁告劦鍠栫€氬銇勯幒鎴姛缂佲偓閸曨偀妲堥柟鐐▕椤庢绻涚€涙﹫鑰块柡灞芥捣閳ь剟娼ч幗婊堝箹閼测晝纾藉ù锝呮惈閺嬪秶绱掗弮鈧幐鎶藉极瀹ュ棗绶炴繛鎴炴皑閻涱櫄extMeshProUGUI + defaultFontAsset + 闂佽閰ｅ褔寮甸鍕嚑闁告劦鍠栫€氬銇勯幒鍡椾壕缂備浇椴搁崹鍨暦閵夆晩鏁嶆繛鍡楁捣闂夊秹姊虹悰鈥充壕濡炪倖鐗楃粙鎴︽儊?&lt;u&gt; 濠电偞鍨堕幐鎼侇敄閸涙潙鍨傛繝闈涚墢濡垶鎮归幁鎺戝闁圭厧銈搁弻?
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
            labelRect.sizeDelta = new Vector2(620, 28);
            _phaseLabel = CreateGameText(labelGo.transform, "\u51c6\u5907\u9636\u6bb5", 22);
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

            _turnButtonText = CreateGameText(go.transform, "\u4f60\u7684\u56de\u5408", 26);
            SetFullRect(_turnButtonText.GetComponent<RectTransform>());

            _turnButton.onClick.AddListener(OnEndTurn);
        }

        private static void BuildEndPassiveNodeButton()
        {
            var go = new GameObject("EndPassiveNodeButton");
            go.transform.SetParent(_root.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-240, 0);
            rect.sizeDelta = new Vector2(180, 56);
            go.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.22f, 1f);
            _endPassiveNodeButton = go.AddComponent<Button>();
            _endPassiveNodeButtonText = CreateGameText(go.transform, "\u7ed3\u675f\u5f53\u524d\u8282\u70b9", 22);
            if (_endPassiveNodeButtonText != null)
                SetFullRect(_endPassiveNodeButtonText.GetComponent<RectTransform>());
            _endPassiveNodeButton.onClick.AddListener(OnEndPassiveNodeButtonClicked);
            go.SetActive(false);
        }

        /// <summary>游戏开始同节点：强制技与非强制技均需己方点击触发（强制技顺序自选）；未点的非强制技可点「结束当前节点」放弃。</summary>
        public static void BeginGameStartPassiveNode(List<GameStartSkillLineEntry> mandatory, List<GameStartSkillLineEntry> optional, Action onComplete)
        {
            _gameStartMandatoryPending = null;
            _gameStartOptionalPending = null;
            _gameStartPassiveNodeOnComplete = null;

            int mc = mandatory?.Count ?? 0;
            int oc = optional?.Count ?? 0;
            if (mc == 0 && oc == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (mc > 0)
            {
                _gameStartMandatoryPending = new Dictionary<(int, int), GameStartSkillLineEntry>(mc);
                for (int i = 0; i < mc; i++)
                {
                    GameStartSkillLineEntry e = mandatory[i];
                    _gameStartMandatoryPending[(e.GeneralIndex, e.SkillIndex)] = e;
                }
            }

            if (oc > 0)
            {
                _gameStartOptionalPending = new Dictionary<(int, int), GameStartSkillLineEntry>(oc);
                for (int i = 0; i < oc; i++)
                {
                    GameStartSkillLineEntry e = optional[i];
                    _gameStartOptionalPending[(e.GeneralIndex, e.SkillIndex)] = e;
                }
            }

            _gameStartPassiveNodeOnComplete = onComplete;
            RefreshEndPassiveNodeButtonVisibility();
            NotifyPhaseChanged();
        }

        /// <summary>仅非强制技时使用；强制技列表为空。</summary>
        public static void BeginGameStartOptionalNode(List<GameStartSkillLineEntry> optional, Action onComplete)
        {
            BeginGameStartPassiveNode(null, optional, onComplete);
        }

        public static bool IsGameStartMandatoryPendingFor(bool isPlayerSide, int generalIndex, int skillIndex)
        {
            return isPlayerSide
                && _gameStartMandatoryPending != null
                && _gameStartMandatoryPending.ContainsKey((generalIndex, skillIndex));
        }

        public static bool IsGameStartOptionalPendingFor(bool isPlayerSide, int generalIndex, int skillIndex)
        {
            return isPlayerSide
                && _gameStartOptionalPending != null
                && _gameStartOptionalPending.ContainsKey((generalIndex, skillIndex));
        }

        public static bool IsGameStartSkillPendingFor(bool isPlayerSide, int generalIndex, int skillIndex)
        {
            return IsGameStartMandatoryPendingFor(isPlayerSide, generalIndex, skillIndex)
                || IsGameStartOptionalPendingFor(isPlayerSide, generalIndex, skillIndex);
        }

        private static void RefreshEndPassiveNodeButtonVisibility()
        {
            bool show = (_gameStartMandatoryPending != null && _gameStartMandatoryPending.Count > 0)
                || (_gameStartOptionalPending != null && _gameStartOptionalPending.Count > 0);
            if (_endPassiveNodeButton != null)
                _endPassiveNodeButton.gameObject.SetActive(show);
        }

        private static void OnEndPassiveNodeButtonClicked()
        {
            if (_gameStartMandatoryPending != null && _gameStartMandatoryPending.Count > 0)
            {
                ToastUI.Show("\u6709\u9700\u8981\u89e6\u53d1\u7684\u5f3a\u5236\u6280", 2f);
                return;
            }

            Action cb = _gameStartPassiveNodeOnComplete;
            _gameStartPassiveNodeOnComplete = null;
            _gameStartMandatoryPending = null;
            _gameStartOptionalPending = null;
            RefreshEndPassiveNodeButtonVisibility();
            cb?.Invoke();
        }

        private static void OnGameStartPassiveNodeAfterOneSkillResolved()
        {
            bool noMandatory = _gameStartMandatoryPending == null || _gameStartMandatoryPending.Count == 0;
            bool noOptional = _gameStartOptionalPending == null || _gameStartOptionalPending.Count == 0;
            if (noMandatory && noOptional)
            {
                Action done = _gameStartPassiveNodeOnComplete;
                _gameStartPassiveNodeOnComplete = null;
                _gameStartMandatoryPending = null;
                _gameStartOptionalPending = null;
                RefreshEndPassiveNodeButtonVisibility();
                NotifyPhaseChanged();
                done?.Invoke();
            }
            else
            {
                RefreshEndPassiveNodeButtonVisibility();
                NotifyPhaseChanged();
            }
        }

        private static bool TryConsumeGameStartPassiveNodeClick(bool isPlayerSide, int generalIndex, int skillIndex)
        {
            if (!isPlayerSide)
                return false;
            if (_state != null
                && (generalIndex < 0 || generalIndex >= _state.Player.GeneralFaceUp.Count || !_state.Player.IsGeneralFaceUp(generalIndex)))
            {
                var gsData = generalIndex >= 0 && generalIndex < _state.Player.GeneralCardIds.Count
                    ? CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(_state.Player.GeneralCardIds[generalIndex]))
                    : null;
                if (gsData == null || !SkillHasTag(gsData, skillIndex, "\u6301\u7eed\u6280"))
                    return false;
            }

            var key = (generalIndex, skillIndex);
            GameStartSkillLineEntry entry;

            if (_gameStartMandatoryPending != null && _gameStartMandatoryPending.TryGetValue(key, out entry))
            {
                _gameStartMandatoryPending.Remove(key);
                SkillEffectBanner.Show(
                    entry.SideIsPlayer,
                    false,
                    entry.RoleDisplayName,
                    entry.SkillDisplayName,
                    GameStartSkillNodeFlow.FormatGameStartMoraleSkillBannerSubtext(entry),
                    () => BattleFlowPacing.AddLogThenContinue(entry.FlowLine, () =>
                    {
                        if (_state != null)
                            OfflineSkillEngine.ApplyResolvedGameStartMoraleSkill(_state, entry);
                        NotifyPhaseChanged();
                        OnGameStartPassiveNodeAfterOneSkillResolved();
                    }));
                RefreshEndPassiveNodeButtonVisibility();
                NotifyPhaseChanged();
                return true;
            }

            if (_gameStartOptionalPending != null && _gameStartOptionalPending.TryGetValue(key, out entry))
            {
                _gameStartOptionalPending.Remove(key);
                SkillEffectBanner.Show(
                    entry.SideIsPlayer,
                    false,
                    entry.RoleDisplayName,
                    entry.SkillDisplayName,
                    GameStartSkillNodeFlow.FormatGameStartMoraleSkillBannerSubtext(entry),
                    () => BattleFlowPacing.AddLogThenContinue(entry.FlowLine, () =>
                    {
                        if (_state != null)
                            OfflineSkillEngine.ApplyResolvedGameStartMoraleSkill(_state, entry);
                        NotifyPhaseChanged();
                        OnGameStartPassiveNodeAfterOneSkillResolved();
                    }));
                RefreshEndPassiveNodeButtonVisibility();
                NotifyPhaseChanged();
                return true;
            }

            return false;
        }

        private static void BuildGenericAttackButton()
        {
            var go = new GameObject("GenericAttackButton");
            go.transform.SetParent(_root.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-40f, -76f);
            rect.sizeDelta = new Vector2(180f, 48f);
            go.AddComponent<Image>().color = new Color(0.32f, 0.42f, 0.55f, 1f);
            _genericAttackButton = go.AddComponent<Button>();
            _genericAttackButtonText = CreateGameText(go.transform, "\u901a\u7528\u653b\u51fb", 22);
            if (_genericAttackButtonText != null)
                SetFullRect(_genericAttackButtonText.GetComponent<RectTransform>());
            _genericAttackButton.onClick.AddListener(OnGenericAttackButtonClicked);
            go.SetActive(false);
        }

        private static void OnGenericAttackButtonClicked()
        {
            if (_state == null)
                return;
            if (_state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main || !_state.IsPlayerTurn)
                return;
            if (_state.ActiveSide.PlayedThisPhase.Count <= 0)
                return;
            if (_state.PendingAttackSkillKind != SelectedSkillKind.None)
                return;

            if (_isOnlineMode)
            {
                var opts = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
                if (opts.Count > 1)
                    _state.PendingGenericAttackOptionIndex = GenericAttackShapes.PickBestOptionIndex(opts);
                _ = OnlineClientService.SelectAttackSkillAsync(-1, -1);
                return;
            }

            CloseChoicePopup();
            CloseAttackPatternPopup();
            _state.PendingAttackPatternVariant = -1;
            _state.PendingGenericAttackOptionIndex = -1;
            var options = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
            if (options.Count > 0)
            {
                OpenGenericAttackShapePopup(openedAfterConfigurePending: false);
                return;
            }

            BattlePhaseManager.NotifyAttackSkillSelected(true, -1, -1, "\u901a\u7528\u653b\u51fb");
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
            _discardButtonLabel = CreateGameText(discardLabelGo.transform, "\u5f03\u724c\u5806", 20);
            SetFullRect(_discardButtonLabel.GetComponent<RectTransform>());

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
            _opponentDiscardButtonLabel = CreateGameText(oppDiscardLabelGo.transform, "\u5f03\u724c\u5806", 20);
            SetFullRect(_opponentDiscardButtonLabel.GetComponent<RectTransform>());
        }

        private static void BuildHandAndCharacterFrames()
        {
            float handW = CompactHandW;
            float handH = CompactHandH;
            float margin = HandDockMargin;
            float labelGap = 6f;
            float labelH = 22f;

            var dockGo = new GameObject("PlayerHandDock");
            dockGo.transform.SetParent(_root.transform, false);
            _playerHandDockRt = dockGo.AddComponent<RectTransform>();

            var playerLabelGo = new GameObject("PlayerHandLabel");
            playerLabelGo.transform.SetParent(dockGo.transform, false);
            var plr = playerLabelGo.AddComponent<RectTransform>();
            _playerHandLabelRt = plr;
            var labelClickPad = new GameObject("HandLabelClickPad");
            labelClickPad.transform.SetParent(playerLabelGo.transform, false);
            var padRt = labelClickPad.AddComponent<RectTransform>();
            SetFullRect(padRt);
            var padImg = labelClickPad.AddComponent<Image>();
            padImg.color = Color.clear;
            padImg.raycastTarget = true;
            labelClickPad.AddComponent<PlayerHandExpandAreaClick>();
            _playerHandLabel = CreateGameText(playerLabelGo.transform, "手牌上限：6/手牌数量：0", 18);
            SetFullRect(_playerHandLabel.GetComponent<RectTransform>());
            _playerHandLabel.raycastTarget = false;

            var playerFrame = new GameObject("PlayerHandFrame");
            playerFrame.transform.SetParent(dockGo.transform, false);
            var pr = playerFrame.AddComponent<RectTransform>();
            _playerHandFrameRt = pr;
            var pi = playerFrame.AddComponent<Image>();
            pi.color = new Color(0.15f, 0.18f, 0.22f, 0.85f);
            pi.raycastTarget = true;
            playerFrame.AddComponent<PlayerHandExpandAreaClick>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(playerFrame.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            _playerHandViewportRt = vpRect;
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(8, 8);
            vpRect.offsetMax = new Vector2(-8, -8);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<PlayerHandExpandAreaClick>();
            var sortGo = new GameObject("HandOrganizeButton");
            sortGo.transform.SetParent(playerFrame.transform, false);
            var sortRt = sortGo.AddComponent<RectTransform>();
            sortRt.anchorMin = sortRt.anchorMax = new Vector2(1f, 1f);
            sortRt.pivot = new Vector2(1f, 1f);
            sortRt.anchoredPosition = new Vector2(-6f, -6f);
            sortRt.sizeDelta = new Vector2(58f, 30f);
            var sortImg = sortGo.AddComponent<Image>();
            sortImg.sprite = GetWhiteSprite();
            sortImg.color = new Color(0.26f, 0.46f, 0.68f, 0.95f);
            var sortBtn = sortGo.AddComponent<Button>();
            sortBtn.targetGraphic = sortImg;
            sortBtn.onClick.AddListener(OnPlayerHandOrganizeClicked);
            var sortLbl = CreateGameText(sortGo.transform, "\u6574\u7406", 15);
            if (sortLbl != null)
            {
                SetFullRect(sortLbl.GetComponent<RectTransform>());
                sortLbl.alignment = TextAlignmentOptions.Center;
            }
            sortGo.SetActive(false);
            _playerHandSortButtonGo = sortGo;

            var playerContent = new GameObject("PlayerHandContent");
            playerContent.transform.SetParent(viewport.transform, false);
            var pcRect = playerContent.AddComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0f, 0.5f);
            pcRect.anchorMax = new Vector2(0f, 0.5f);
            pcRect.pivot = new Vector2(0f, 0.5f);
            pcRect.anchoredPosition = Vector2.zero;
            pcRect.sizeDelta = new Vector2(0, handH - 16);
            var psr = playerFrame.AddComponent<RightMouseScrollRect>();
            psr.content = pcRect;
            psr.viewport = vpRect;
            psr.horizontal = true;
            psr.vertical = false;
            _playerHandRightMouseScroll = psr;
            _playerHandRightMouseScroll.enabled = false;
            _playerHandContent = playerContent.transform;

            ApplyPlayerHandDockLayout(false);

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
            ocRect.anchorMin = new Vector2(0f, 0.5f);
            ocRect.anchorMax = new Vector2(0f, 0.5f);
            ocRect.pivot = new Vector2(0f, 0.5f);
            ocRect.anchoredPosition = Vector2.zero;
            ocRect.sizeDelta = new Vector2(0, handH - 16);
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

        /// <summary>全屏透明层：仅在手牌展开时启用，点击手牌区域外收起（无视觉遮罩）。</summary>
        private static void BuildHandExpandOutsideDismissOverlay()
        {
            var go = new GameObject("HandExpandOutsideDismissOverlay");
            go.transform.SetParent(_root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            SetFullRect(rt);
            var img = go.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(CollapsePlayerHandIfExpanded);
            go.SetActive(false);
            _playerHandOutsideDismissOverlayGo = go;
        }

        /// <summary>点击手牌区标签/空白：仅负责展开；收起需点击手牌区域外。</summary>
        public static void TryExpandPlayerHandFromUiClick()
        {
            if (_state == null)
                return;
            if (_playerHandExpanded)
                return;
            SetPlayerHandExpanded(true);
        }

        private static void SetPlayerHandExpanded(bool expanded)
        {
            if (_playerHandDockRt == null)
                return;
            _playerHandExpanded = expanded;
            if (!expanded && _playerHandOutsideDismissOverlayGo != null)
                _playerHandOutsideDismissOverlayGo.SetActive(false);
            if (_playerHandRightMouseScroll != null)
                _playerHandRightMouseScroll.enabled = expanded;
            ApplyPlayerHandDockLayout(expanded);
            if (_playerHandSortButtonGo != null)
                _playerHandSortButtonGo.SetActive(expanded);
            RefreshHandCards();
            if (expanded && _root != null)
            {
                _playerHandDockRt.SetAsLastSibling();
                if (_handHoverOverlay != null)
                    _handHoverOverlay.SetAsLastSibling();
                if (_playerHandOutsideDismissOverlayGo != null && _gameUiBackgroundTransform != null)
                {
                    _playerHandOutsideDismissOverlayGo.SetActive(true);
                    _playerHandOutsideDismissOverlayGo.transform.SetSiblingIndex(_gameUiBackgroundTransform.GetSiblingIndex() + 1);
                }
            }
        }

        private static void ApplyPlayerHandDockLayout(bool expanded)
        {
            if (_playerHandDockRt == null)
                return;
            float labelGap = 6f;
            float labelH = 22f;
            float margin = HandDockMargin;
            if (!expanded)
            {
                float w = CompactHandW;
                float totalH = labelGap + labelH + CompactHandH;
                _playerHandDockRt.anchorMin = new Vector2(1f, 0f);
                _playerHandDockRt.anchorMax = new Vector2(1f, 0f);
                _playerHandDockRt.pivot = new Vector2(1f, 0f);
                _playerHandDockRt.anchoredPosition = new Vector2(-margin, margin);
                _playerHandDockRt.sizeDelta = new Vector2(w, totalH);
                if (_playerHandLabelRt != null)
                {
                    _playerHandLabelRt.anchorMin = new Vector2(0f, 1f);
                    _playerHandLabelRt.anchorMax = new Vector2(1f, 1f);
                    _playerHandLabelRt.pivot = new Vector2(0.5f, 1f);
                    _playerHandLabelRt.offsetMax = new Vector2(-8f, 0f);
                    _playerHandLabelRt.offsetMin = new Vector2(8f, -(labelGap + labelH));
                }

                if (_playerHandFrameRt != null)
                {
                    _playerHandFrameRt.anchorMin = new Vector2(0f, 0f);
                    _playerHandFrameRt.anchorMax = new Vector2(1f, 0f);
                    _playerHandFrameRt.pivot = new Vector2(0.5f, 0f);
                    _playerHandFrameRt.offsetMin = Vector2.zero;
                    _playerHandFrameRt.offsetMax = new Vector2(0f, CompactHandH);
                }

                SyncPlayerHandContentHeight(false);
            }
            else
            {
                float w = ExpandedHandOuterW;
                float totalH = labelGap + labelH + ExpandedHandOuterH;
                _playerHandDockRt.anchorMin = new Vector2(0.5f, 0f);
                _playerHandDockRt.anchorMax = new Vector2(0.5f, 0f);
                _playerHandDockRt.pivot = new Vector2(0.5f, 0f);
                _playerHandDockRt.anchoredPosition = new Vector2(0f, 48f);
                _playerHandDockRt.sizeDelta = new Vector2(w, totalH);
                if (_playerHandLabelRt != null)
                {
                    _playerHandLabelRt.anchorMin = new Vector2(0f, 1f);
                    _playerHandLabelRt.anchorMax = new Vector2(1f, 1f);
                    _playerHandLabelRt.pivot = new Vector2(0.5f, 1f);
                    _playerHandLabelRt.offsetMax = new Vector2(-8f, 0f);
                    _playerHandLabelRt.offsetMin = new Vector2(8f, -(labelGap + labelH));
                }

                if (_playerHandFrameRt != null)
                {
                    _playerHandFrameRt.anchorMin = new Vector2(0f, 0f);
                    _playerHandFrameRt.anchorMax = new Vector2(1f, 0f);
                    _playerHandFrameRt.pivot = new Vector2(0.5f, 0f);
                    _playerHandFrameRt.offsetMin = Vector2.zero;
                    _playerHandFrameRt.offsetMax = new Vector2(0f, ExpandedHandOuterH);
                }

                SyncPlayerHandContentHeight(true);
            }
        }

        private static void SyncPlayerHandContentHeight(bool expanded)
        {
            if (_playerHandContent == null)
                return;
            var pc = _playerHandContent as RectTransform;
            if (pc == null)
                return;
            float inner = expanded ? ExpandedHandOuterH - 16f : CompactHandH - 16f;
            pc.sizeDelta = new Vector2(pc.sizeDelta.x, inner);
        }

        private static void BuildPlayedZone()
        {
            _playedZoneRoot = new GameObject("PlayedZone");
            _playedZoneRoot.transform.SetParent(_root.transform, false);
            var rect = _playedZoneRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float generalCenterOffsetX = (CharacterSideColumnWidth + HpBarWidth + HpToGeneralGap + GeneralContainerWidth * 0.5f) - CharacterAreaTotalWidth * 0.5f;
            rect.anchoredPosition = new Vector2(CharacterAreaOffsetX + generalCenterOffsetX, 0f);
            rect.sizeDelta = new Vector2(GeneralContainerWidth + 72f, 156f);
            var img = _playedZoneRoot.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.17f, 0.88f);
            img.sprite = GetWhiteSprite();
            img.raycastTarget = true;
            var outline = _playedZoneRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.49f, 0.58f, 0.9f);
            outline.effectDistance = new Vector2(2f, 2f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_playedZoneRoot.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);
            titleRect.sizeDelta = new Vector2(200f, 28f);
            var titleText = CreateGameText(titleGo.transform, "\u51fa\u724c\u533a\u57df", 20);
            if (titleText != null)
                SetFullRect(titleText.GetComponent<RectTransform>());

            var content = new GameObject("Content");
            content.transform.SetParent(_playedZoneRoot.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.offsetMin = new Vector2(18f, 16f);
            cRect.offsetMax = new Vector2(-18f, -40f);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = false;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            _playedZoneContent = content.transform;
            _playedZoneRoot.AddComponent<PlayedZoneMarker>();
        }

        /// <summary>多格模式最多显示的槽位数（基础 2 格在左，其余为上限增加部分，从右往左隐藏）。</summary>
        private const int MoraleIconSlotMax = 6;
        /// <summary>士气上限大于该值时隐藏多格，仅保留最左一格并显示 X/Y。</summary>
        private const int MoraleCompactCapThreshold = 5;

        private static GameObject _playerMoraleRoot;
        private static Image[] _playerMoraleIcons = new Image[MoraleIconSlotMax];
        private static TextMeshProUGUI _playerMoraleCompactLabel;
        private static GameObject _opponentMoraleRoot;
        private static Image[] _opponentMoraleIcons = new Image[MoraleIconSlotMax];
        private static TextMeshProUGUI _opponentMoraleCompactLabel;
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
            pr.pivot = new Vector2(0f, 0.5f);
            pr.anchoredPosition = new Vector2(leftX, moraleY);
            var pBtn = _playerMoraleRoot.AddComponent<Button>();
            pBtn.onClick.AddListener(OpenMoralePopup);
            var phlg = _playerMoraleRoot.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = gap;
            phlg.childAlignment = TextAnchor.MiddleLeft;
            phlg.childControlWidth = true;
            phlg.childControlHeight = true;
            phlg.childForceExpandWidth = false;
            phlg.childForceExpandHeight = false;
            var pFitter = _playerMoraleRoot.AddComponent<ContentSizeFitter>();
            pFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < MoraleIconSlotMax; i++)
            {
                var icon = new GameObject("Icon" + i);
                icon.transform.SetParent(_playerMoraleRoot.transform, false);
                var le = icon.AddComponent<LayoutElement>();
                le.preferredWidth = iconSize;
                le.preferredHeight = iconSize;
                le.minWidth = iconSize;
                le.minHeight = iconSize;
                _playerMoraleIcons[i] = icon.AddComponent<Image>();
                _playerMoraleIcons[i].sprite = GetHexagonSprite();
                _playerMoraleIcons[i].color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }

            _playerMoraleCompactLabel = CreateMoraleCompactRatioLabel(_playerMoraleIcons[0].transform);
            if (_playerMoraleCompactLabel != null)
                _playerMoraleCompactLabel.gameObject.SetActive(false);

            _opponentMoraleRoot = new GameObject("OpponentMorale");
            _opponentMoraleRoot.transform.SetParent(_root.transform, false);
            var or = _opponentMoraleRoot.AddComponent<RectTransform>();
            or.anchorMin = new Vector2(0f, 1f);
            or.anchorMax = new Vector2(0f, 1f);
            or.pivot = new Vector2(0f, 0.5f);
            or.anchoredPosition = new Vector2(leftX, -moraleY);
            var ohlg = _opponentMoraleRoot.AddComponent<HorizontalLayoutGroup>();
            ohlg.spacing = gap;
            ohlg.childAlignment = TextAnchor.MiddleLeft;
            ohlg.childControlWidth = true;
            ohlg.childControlHeight = true;
            ohlg.childForceExpandWidth = false;
            ohlg.childForceExpandHeight = false;
            var oFitter = _opponentMoraleRoot.AddComponent<ContentSizeFitter>();
            oFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < MoraleIconSlotMax; i++)
            {
                var icon = new GameObject("Icon" + i);
                icon.transform.SetParent(_opponentMoraleRoot.transform, false);
                var le = icon.AddComponent<LayoutElement>();
                le.preferredWidth = iconSize;
                le.preferredHeight = iconSize;
                le.minWidth = iconSize;
                le.minHeight = iconSize;
                _opponentMoraleIcons[i] = icon.AddComponent<Image>();
                _opponentMoraleIcons[i].sprite = GetHexagonSprite();
                _opponentMoraleIcons[i].color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }

            _opponentMoraleCompactLabel = CreateMoraleCompactRatioLabel(_opponentMoraleIcons[0].transform);
            if (_opponentMoraleCompactLabel != null)
                _opponentMoraleCompactLabel.gameObject.SetActive(false);
        }

        /// <summary>叠在最左六边形上的 X/Y 文本（紧凑模式）。</summary>
        private static TextMeshProUGUI CreateMoraleCompactRatioLabel(Transform parent)
        {
            TextMeshProUGUI t = CreateGameText(parent, "0/6", 20);
            if (t == null)
                return null;
            t.raycastTarget = false;
            t.fontStyle = FontStyles.Bold;
            // 勿用 TMP outlineWidth：在部分字体/材质未就绪时会触发 SetOutlineThickness 空引用。
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            SetFullRect(t.GetComponent<RectTransform>());
            return t;
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
            var titleT = CreateGameText(titleGo.transform, "\u8bf7\u9009\u62e9\u58eb\u6c14\u6548\u679c", 26);
            string[] labels = new[] { "\u6478\u4e24\u5f20\u724c", "\u589e\u52a0\u51fa\u724c\u9636\u6bb5", "\u5df1\u65b9\u89d2\u8272\u7ffb\u9762\u6216\u7ffb\u56de" };
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
            CollapsePlayerHandIfExpanded();
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
                    bool canUse = !used[i];
                    if (i == 2 && BuildAllPlayerGeneralIndicesForMoraleFlip().Count == 0)
                        canUse = false;
                    btn.interactable = canUse;
                    btn.GetComponent<Image>().color = canUse ? new Color(0.28f, 0.35f, 0.5f, 1f) : new Color(0.35f, 0.35f, 0.38f, 1f);
                }
            }
        }

        private static void OnMoraleEffectClick(int effectIndex)
        {
            if (_state == null || !_state.IsPlayerTurn || _state.Player.Morale <= 0) return;
            if (_state.Player.MoraleUsedThisTurn[effectIndex]) return;

            if (_isOnlineMode)
            {
                if (effectIndex == 2)
                {
                    var allIdx = BuildAllPlayerGeneralIndicesForMoraleFlip();
                    if (allIdx.Count == 0)
                    {
                        ToastUI.Show("\u65e0\u53ef\u9009\u89d2\u8272");
                        return;
                    }

                    var labels = new string[allIdx.Count];
                    for (int i = 0; i < allIdx.Count; i++)
                    {
                        int gi = allIdx[i];
                        labels[i] = GetGeneralDisplayName(true, gi) + (_state.Player.IsGeneralFaceUp(gi)
                            ? "\uff08\u7ffb\u9762\uff09"
                            : "\uff08\u7ffb\u56de\uff09");
                    }

                    OpenChoicePopup("\u9009\u62e9\u89d2\u8272\uff1a\u672a\u7ffb\u9762\u5219\u7ffb\u9762\uff0c\u5df2\u7ffb\u9762\u5219\u7ffb\u56de", labels, selectedIndex =>
                    {
                        if (selectedIndex < 0 || selectedIndex >= allIdx.Count)
                            return;
                        CloseMoralePopup();
                        _ = OnlineClientService.UseMoraleAsync(effectIndex, allIdx[selectedIndex]);
                    });
                    return;
                }
                CloseMoralePopup();
                _ = OnlineClientService.UseMoraleAsync(effectIndex, null);
                return;
            }

            if (effectIndex == 2)
            {
                var allIdx = BuildAllPlayerGeneralIndicesForMoraleFlip();
                if (allIdx.Count == 0)
                {
                    ToastUI.Show("\u65e0\u53ef\u9009\u89d2\u8272");
                    return;
                }

                var labels = new string[allIdx.Count];
                for (int i = 0; i < allIdx.Count; i++)
                {
                    int gi = allIdx[i];
                    labels[i] = GetGeneralDisplayName(true, gi) + (_state.Player.IsGeneralFaceUp(gi)
                        ? "\uff08\u7ffb\u9762\uff09"
                        : "\uff08\u7ffb\u56de\uff09");
                }

                OpenChoicePopup("\u9009\u62e9\u89d2\u8272\uff1a\u672a\u7ffb\u9762\u5219\u7ffb\u9762\uff0c\u5df2\u7ffb\u9762\u5219\u7ffb\u56de", labels, selectedIndex =>
                {
                    if (selectedIndex < 0 || selectedIndex >= allIdx.Count)
                        return;
                    int gi = allIdx[selectedIndex];
                    bool ok = _state.Player.IsGeneralFaceUp(gi)
                        ? _state.TryFlipGeneral(true, gi)
                        : _state.Player.UnflipGeneralFromMorale(gi);
                    if (!ok)
                    {
                        ToastUI.Show("\u65e0\u6cd5\u5b8c\u6210\u7ffb\u9762");
                        return;
                    }

                    _state.Player.MoraleUsedThisTurn[2] = true;
                    _state.Player.Morale--;
                    OfflineSkillEngine.ApplyRenDeWhenMoraleSpent(_state, true);
                    CloseMoralePopup();
                    RefreshAllFromState();
                    RefreshMoraleIcons();
                });
                return;
            }

            _state.Player.MoraleUsedThisTurn[effectIndex] = true;
            _state.Player.Morale--;
            OfflineSkillEngine.ApplyRenDeWhenMoraleSpent(_state, true);
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
            if (_state == null || _playerMoraleIcons == null)
                return;

            RefreshMoraleIconRow(_state.Player.Morale, _state.Player.MoraleCap, _playerMoraleIcons, _playerMoraleRoot, _playerMoraleCompactLabel);
            RefreshMoraleIconRow(_state.Opponent.Morale, _state.Opponent.MoraleCap, _opponentMoraleIcons, _opponentMoraleRoot, _opponentMoraleCompactLabel);

            var pBtn = _playerMoraleRoot != null ? _playerMoraleRoot.GetComponent<Button>() : null;
            if (pBtn != null)
                pBtn.interactable = _state.Player.Morale > 0;
        }

        /// <summary>
        /// 基础 2 个槽在索引 0、1（左侧），士气上限超出 2 的部分依次向右增加槽位；士气减少时从右侧槽先变灰（仅显示前 morale 格为金色）。
        /// 当士气上限 &gt; <see cref="MoraleCompactCapThreshold"/> 时只显示最左一格，并在其上显示 X/Y（当前士气/上限）；上限减回 5 及以下时恢复多格显示。
        /// </summary>
        private static void RefreshMoraleIconRow(int morale, int moraleCap, Image[] icons, GameObject root, TextMeshProUGUI compactLabel)
        {
            if (icons == null)
                return;

            int capRaw = Mathf.Max(1, moraleCap);
            int m = Mathf.Clamp(morale, 0, capRaw);
            var gold = new Color(0.9f, 0.75f, 0.2f, 1f);
            var grey = new Color(0.4f, 0.4f, 0.45f, 1f);

            if (capRaw > MoraleCompactCapThreshold)
            {
                if (compactLabel != null)
                {
                    compactLabel.gameObject.SetActive(true);
                    compactLabel.text = m + "/" + capRaw;
                }

                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] == null)
                        continue;
                    icons[i].gameObject.SetActive(i == 0);
                    if (i == 0)
                        icons[i].color = m > 0 ? gold : grey;
                }
            }
            else
            {
                if (compactLabel != null)
                    compactLabel.gameObject.SetActive(false);

                int cap = Mathf.Min(capRaw, MoraleIconSlotMax);
                m = Mathf.Clamp(morale, 0, cap);
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] == null)
                        continue;
                    bool show = i < cap;
                    icons[i].gameObject.SetActive(show);
                    if (show)
                        icons[i].color = i < m ? gold : grey;
                }
            }

            if (root != null)
            {
                var rt = root.GetComponent<RectTransform>();
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        private static void CloseMoralePopup()
        {
            if (_moralePopupRoot != null) _moralePopupRoot.SetActive(false);
        }

        private static void BuildChoicePopup()
        {
            _choicePopupRoot = new GameObject("ChoicePopup");
            _choicePopupRoot.transform.SetParent(_root.transform, false);
            _choicePopupRoot.SetActive(false);
            SetFullRect(_choicePopupRoot.AddComponent<RectTransform>());
            var canvas = _choicePopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 46;
            _choicePopupRoot.AddComponent<GraphicRaycaster>();

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_choicePopupRoot.transform, false);
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            SetFullRect(overlay.GetComponent<RectTransform>());
            var overlayButton = overlay.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(CloseChoicePopup);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_choicePopupRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760, 460);
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -18f);
            titleRect.sizeDelta = new Vector2(620, 40);
            _choicePopupTitle = CreateGameText(titleGo.transform, "\u8bf7\u9009\u62e9", 28);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(panel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(32, 32);
            contentRect.offsetMax = new Vector2(-32, -72);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            _choicePopupContent = contentGo.transform;
        }

        private static void OpenChoicePopup(string title, string[] labels, System.Action<int> onSelect, string cancelLabel = "\u53d6\u6d88")
        {
            if (_choicePopupRoot == null)
                return;

            CollapsePlayerHandIfExpanded();
            _choicePopupRoot.SetActive(true);
            if (_choicePopupTitle != null)
                _choicePopupTitle.text = title ?? "\u8bf7\u9009\u62e9";

            foreach (Transform child in _choicePopupContent)
                UnityEngine.Object.Destroy(child.gameObject);

            for (int i = 0; i < labels.Length; i++)
            {
                int optionIndex = i;
                var buttonGo = new GameObject("Option_" + i);
                buttonGo.transform.SetParent(_choicePopupContent, false);
                var layout = buttonGo.AddComponent<LayoutElement>();
                layout.preferredHeight = 54f;
                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(0.25f, 0.5f, 0.9f, 1f);
                image.sprite = GetWhiteSprite();
                var button = buttonGo.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() =>
                {
                    CloseChoicePopup();
                    onSelect?.Invoke(optionIndex);
                });
                var text = CreateGameText(buttonGo.transform, labels[i], 22);
                if (text != null)
                    SetFullRect(text.GetComponent<RectTransform>());
            }

            var cancelGo = new GameObject("Cancel");
            cancelGo.transform.SetParent(_choicePopupContent, false);
            var cancelLayout = cancelGo.AddComponent<LayoutElement>();
            cancelLayout.preferredHeight = 54f;
            var cancelImage = cancelGo.AddComponent<Image>();
            cancelImage.color = new Color(0.38f, 0.38f, 0.42f, 1f);
            cancelImage.sprite = GetWhiteSprite();
            var cancelButton = cancelGo.AddComponent<Button>();
            cancelButton.targetGraphic = cancelImage;
            cancelButton.onClick.AddListener(CloseChoicePopup);
            var cancelText = CreateGameText(cancelGo.transform, cancelLabel, 22);
            if (cancelText != null)
                SetFullRect(cancelText.GetComponent<RectTransform>());
        }

        private static void CloseChoicePopup()
        {
            if (_choicePopupRoot != null)
                _choicePopupRoot.SetActive(false);
        }

        private static void BuildAttackPatternPopup()
        {
            _attackPatternPopupRoot = new GameObject("AttackPatternPopup");
            _attackPatternPopupRoot.transform.SetParent(_root.transform, false);
            _attackPatternPopupRoot.SetActive(false);
            SetFullRect(_attackPatternPopupRoot.AddComponent<RectTransform>());
            var canvas = _attackPatternPopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            _attackPatternPopupRoot.AddComponent<GraphicRaycaster>();

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_attackPatternPopupRoot.transform, false);
            var oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0, 0, 0, 0.55f);
            SetFullRect(overlay.GetComponent<RectTransform>());
            var overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(() =>
            {
                CloseAttackPatternPopup();
                OpenPlayerAttackSkillFirstMenu();
            });

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_attackPatternPopupRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(820, 560);
            panel.AddComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 0.99f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -14f);
            titleRect.sizeDelta = new Vector2(760, 44f);
            _attackPatternTitle = CreateGameText(titleGo.transform, "\u9009\u62e9\u724c\u578b", 26);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(panel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(28, 24);
            contentRect.offsetMax = new Vector2(-28, -68);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            _attackPatternContent = contentGo.transform;
        }

        private static void CloseAttackPatternPopup()
        {
            if (_attackPatternPopupRoot != null)
                _attackPatternPopupRoot.SetActive(false);
        }

        /// <summary>通用攻击：展示可申报牌型与效果，由玩家点选一项（含仅一种牌型时也弹窗确认）。<paramref name="openedAfterConfigurePending"/> 为 true 时表示技能回退后挂起，返回时需 <see cref="BattlePhaseManager.ResetAfterGenericShapePopupCancel"/>。</summary>
        public static void OpenGenericAttackShapePopup(bool openedAfterConfigurePending)
        {
            _genericAttackShapePopupAfterPendingConfigure = openedAfterConfigurePending;
            if (_attackPatternPopupRoot == null || _state == null || _attackPatternContent == null)
                return;

            var options = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
            if (options.Count <= 0)
                return;

            foreach (Transform child in _attackPatternContent)
                UnityEngine.Object.Destroy(child.gameObject);

            if (_attackPatternTitle != null)
                _attackPatternTitle.text = "\u9009\u62e9\u901a\u7528\u653b\u51fb\u724c\u578b";

            for (int i = 0; i < options.Count; i++)
            {
                int idx = i;
                GenericAttackOption opt = options[i];
                string desc = GenericAttackShapes.GetShapeEffectDescriptionForUi(opt.Kind);

                var row = new GameObject("GenericShapeRow_" + i);
                row.transform.SetParent(_attackPatternContent, false);
                var rowV = row.AddComponent<VerticalLayoutGroup>();
                rowV.spacing = 8f;
                rowV.childAlignment = TextAnchor.UpperCenter;
                rowV.childControlWidth = true;
                rowV.childControlHeight = true;
                rowV.childForceExpandWidth = true;
                rowV.childForceExpandHeight = false;
                rowV.padding = new RectOffset(0, 0, 0, 0);

                var rowLe = row.AddComponent<LayoutElement>();
                rowLe.minHeight = 96f;
                rowLe.preferredHeight = 108f;
                rowLe.flexibleHeight = 0f;

                var buttonGo = new GameObject("GenericShapeBtn_" + i);
                buttonGo.transform.SetParent(row.transform, false);
                var btnLe = buttonGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 50f;
                btnLe.minHeight = 50f;
                btnLe.flexibleHeight = 0f;
                var img = buttonGo.AddComponent<Image>();
                img.sprite = GetWhiteSprite();
                img.color = new Color(0.22f, 0.48f, 0.82f, 1f);
                var btn = buttonGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() =>
                {
                    if (_state == null)
                        return;
                    CloseAttackPatternPopup();
                    _genericAttackShapePopupAfterPendingConfigure = false;
                    if (_isOnlineMode)
                    {
                        _state.PendingGenericAttackOptionIndex = idx;
                        _ = OnlineClientService.SelectAttackSkillAsync(-1, -1);
                        return;
                    }

                    BattlePhaseManager.CompletePlayerGenericAttackShapePick(idx);
                });

                var btnText = CreateGameText(buttonGo.transform, "\u3010" + opt.DisplayName + "\u3011", 20);
                if (btnText != null)
                    SetFullRect(btnText.GetComponent<RectTransform>());

                var descGo = new GameObject("Desc_" + i);
                descGo.transform.SetParent(row.transform, false);
                var descLe = descGo.AddComponent<LayoutElement>();
                descLe.minHeight = 40f;
                descLe.preferredHeight = 44f;
                descLe.flexibleHeight = 0f;
                var descT = CreateGameText(descGo.transform, desc, 16, TextAlignmentOptions.TopLeft);
                if (descT != null)
                {
                    descT.color = new Color(0.88f, 0.9f, 0.94f, 1f);
                    var drt = descT.GetComponent<RectTransform>();
                    drt.anchorMin = Vector2.zero;
                    drt.anchorMax = Vector2.one;
                    drt.offsetMin = new Vector2(8f, 0f);
                    drt.offsetMax = new Vector2(-8f, 0f);
                }
            }

            var backGo = new GameObject("Back");
            backGo.transform.SetParent(_attackPatternContent, false);
            var backLe = backGo.AddComponent<LayoutElement>();
            backLe.preferredHeight = 48f;
            backLe.minHeight = 48f;
            var backImg = backGo.AddComponent<Image>();
            backImg.sprite = GetWhiteSprite();
            backImg.color = new Color(0.4f, 0.4f, 0.44f, 1f);
            var backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.onClick.AddListener(() =>
            {
                CloseAttackPatternPopup();
                bool afterPending = _genericAttackShapePopupAfterPendingConfigure;
                _genericAttackShapePopupAfterPendingConfigure = false;
                if (afterPending)
                    BattlePhaseManager.ResetAfterGenericShapePopupCancel();
                OpenPlayerAttackSkillFirstMenu();
            });
            var backTxt = CreateGameText(backGo.transform, "\u8fd4\u56de\u4e0a\u4e00\u7ea7", 20);
            if (backTxt != null)
                SetFullRect(backTxt.GetComponent<RectTransform>());

            CollapsePlayerHandIfExpanded();
            _attackPatternPopupRoot.SetActive(true);
        }

        private static string GetPlayerGeneralSkillRuleText(int generalIndex, int skillIndex)
        {
            if (_state == null || generalIndex < 0 || skillIndex < 0 || skillIndex > 2)
                return string.Empty;

            SideState side = _state.Player;
            if (generalIndex >= side.GeneralCardIds.Count)
                return string.Empty;

            CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[generalIndex]));
            if (data == null)
                return string.Empty;

            return skillIndex switch
            {
                0 => data.SkillDesc1 ?? string.Empty,
                1 => data.SkillDesc2 ?? string.Empty,
                2 => data.SkillDesc3 ?? string.Empty,
                _ => string.Empty,
            };
        }

        /// <summary>【策马斩将】二级确认：每行按钮下为对应牌型说明（与卡表 <see cref="CardData.SkillDesc1"/> 一致）。</summary>
        private static void OpenCeMaAttackPatternPopup(int generalIndex, int skillIndex, string skillName)
        {
            if (_attackPatternPopupRoot == null || _state == null || _attackPatternContent == null)
                return;

            var cards = _state.ActiveSide.PlayedThisPhase;
            bool can0 = OfflineSkillEngine.CeMaTwoRedSinglesMatches(cards);
            bool can1 = OfflineSkillEngine.CeMaRedStraightMatches(cards);
            bool can2 = OfflineSkillEngine.CeMaRedStraightFlushMatches(cards);

            foreach (Transform child in _attackPatternContent)
                UnityEngine.Object.Destroy(child.gameObject);

            if (_attackPatternTitle != null)
                _attackPatternTitle.text = "\u9009\u62e9\u3010\u7b56\u9a6c\u65a9\u5c06\u3011\u724c\u578b";

            string[] btnLabels =
            {
                "\u4e24\u5f20\u7ea2\u8272\u5355\u724c",
                "\u7ea2\u8272\u987a\u5b50\uff08\u56db\u5f20\uff09",
                "\u7ea2\u8272\u540c\u82b1\u987a\uff08\u56db\u5f20\uff09",
            };

            string[] variantDescLines =
            {
                "\u4e24\u5f20\u7ea2\u8272\u5355\u724c\uff1a\u4f60\u9020\u62103\u70b9\u5175\u5203\u4f24\u5bb3\u3002",
                "\u7ea2\u8272\u987a\u5b50\uff1a\u4f60\u9020\u62106\u70b9\u5175\u5203\u4f24\u5bb3\uff0c\u5e76\u4e14\u672c\u56de\u5408\u4e2d\u4f60\u83b7\u5f97\u4e00\u4e2a\u989d\u5916\u7684\u51fa\u724c\u9636\u6bb5\u3002",
                "\u7ea2\u8272\u540c\u82b1\u987a\uff1a\u4f60\u9020\u62107\u70b9\u5175\u5203\u4f24\u5bb3\uff0c\u5e76\u4e14\u672c\u56de\u5408\u4e2d\u4f60\u83b7\u5f97\u4e00\u4e2a\u989d\u5916\u7684\u51fa\u724c\u9636\u6bb5\uff0c\u7136\u540e\u4f60\u6478\u4e09\u5f20\u724c\u3002",
            };

            for (int i = 0; i < 3; i++)
            {
                int variant = i;
                bool enabled = i switch
                {
                    0 => can0,
                    1 => can1,
                    2 => can2,
                    _ => false,
                };

                var row = new GameObject("PatternRow_" + i);
                row.transform.SetParent(_attackPatternContent, false);
                var rowV = row.AddComponent<VerticalLayoutGroup>();
                rowV.spacing = 8f;
                rowV.childAlignment = TextAnchor.UpperCenter;
                rowV.childControlWidth = true;
                rowV.childControlHeight = true;
                rowV.childForceExpandWidth = true;
                rowV.childForceExpandHeight = false;
                rowV.padding = new RectOffset(0, 0, 0, 0);

                var rowLe = row.AddComponent<LayoutElement>();
                rowLe.minHeight = 120f;
                rowLe.preferredHeight = 128f;
                rowLe.flexibleHeight = 0f;

                var buttonGo = new GameObject("PatternBtn_" + i);
                buttonGo.transform.SetParent(row.transform, false);
                var btnLe = buttonGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 50f;
                btnLe.minHeight = 50f;
                btnLe.flexibleHeight = 0f;
                var img = buttonGo.AddComponent<Image>();
                img.sprite = GetWhiteSprite();
                img.color = enabled ? new Color(0.22f, 0.48f, 0.82f, 1f) : new Color(0.32f, 0.32f, 0.36f, 0.85f);
                var btn = buttonGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.interactable = enabled;
                btn.onClick.AddListener(() =>
                {
                    if (_state == null)
                        return;
                    _state.PendingAttackPatternVariant = variant;
                    CloseAttackPatternPopup();
                    if (_isOnlineMode)
                        _ = OnlineClientService.SelectAttackSkillAsync(generalIndex, skillIndex);
                    else
                        BattlePhaseManager.NotifyAttackSkillSelected(true, generalIndex, skillIndex, skillName);
                });

                var btnText = CreateGameText(buttonGo.transform, btnLabels[i], 20);
                if (btnText != null)
                    SetFullRect(btnText.GetComponent<RectTransform>());

                var descGo = new GameObject("PatternDesc_" + i);
                descGo.transform.SetParent(row.transform, false);
                var descLe = descGo.AddComponent<LayoutElement>();
                descLe.minHeight = 36f;
                descLe.preferredHeight = -1f;
                descLe.flexibleHeight = 0f;
                var descT = CreateGameText(descGo.transform, variantDescLines[i], 14, TextAlignmentOptions.TopLeft);
                if (descT != null)
                {
                    descT.enableWordWrapping = true;
                    descT.color = new Color(0.82f, 0.86f, 0.9f, 1f);
                    var dr = descT.GetComponent<RectTransform>();
                    dr.anchorMin = new Vector2(0f, 1f);
                    dr.anchorMax = new Vector2(1f, 1f);
                    dr.pivot = new Vector2(0.5f, 1f);
                    dr.sizeDelta = new Vector2(0f, 72f);
                }
            }

            var backGo = new GameObject("Back");
            backGo.transform.SetParent(_attackPatternContent, false);
            var backLe = backGo.AddComponent<LayoutElement>();
            backLe.preferredHeight = 48f;
            backLe.minHeight = 48f;
            var backImg = backGo.AddComponent<Image>();
            backImg.sprite = GetWhiteSprite();
            backImg.color = new Color(0.4f, 0.4f, 0.44f, 1f);
            var backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.onClick.AddListener(() =>
            {
                CloseAttackPatternPopup();
                OpenPlayerAttackSkillFirstMenu();
            });
            var backTxt = CreateGameText(backGo.transform, "\u8fd4\u56de\u4e0a\u4e00\u7ea7", 20);
            if (backTxt != null)
                SetFullRect(backTxt.GetComponent<RectTransform>());

            CollapsePlayerHandIfExpanded();
            _attackPatternPopupRoot.SetActive(true);
        }

        private static bool AttackSkillNeedsPatternSubPopup(string cardId, int skillIndex)
        {
            return string.Equals(SkillRuleHelper.MakeSkillKey(cardId ?? string.Empty, skillIndex), "NO002_0", System.StringComparison.Ordinal);
        }

        private static void CommitAttackSkillAfterOptionalPatternPopup(int generalIndex, int skillIndex, string skillName)
        {
            if (_state == null)
                return;

            if (_isOnlineMode)
            {
                if (generalIndex < 0)
                {
                    var og = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
                    if (og.Count > 1)
                        _state.PendingGenericAttackOptionIndex = GenericAttackShapes.PickBestOptionIndex(og);
                    _ = OnlineClientService.SelectAttackSkillAsync(-1, -1);
                    return;
                }

                var oside = _state.Player;
                if (generalIndex >= oside.GeneralCardIds.Count)
                {
                    _ = OnlineClientService.SelectAttackSkillAsync(-1, -1);
                    return;
                }

                string ocardId = oside.GeneralCardIds[generalIndex] ?? string.Empty;
                if (!AttackSkillNeedsPatternSubPopup(ocardId, skillIndex))
                {
                    _ = OnlineClientService.SelectAttackSkillAsync(generalIndex, skillIndex);
                    return;
                }

                CloseChoicePopup();
                OpenCeMaAttackPatternPopup(generalIndex, skillIndex, skillName);
                return;
            }

            if (generalIndex < 0)
            {
                _state.PendingAttackPatternVariant = -1;
                _state.PendingGenericAttackOptionIndex = -1;
                var gOpts = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
                if (gOpts.Count > 0)
                {
                    CloseChoicePopup();
                    OpenGenericAttackShapePopup(openedAfterConfigurePending: false);
                    return;
                }

                BattlePhaseManager.NotifyAttackSkillSelected(true, generalIndex, skillIndex, skillName);
                return;
            }

            var side = _state.Player;
            if (generalIndex >= side.GeneralCardIds.Count)
            {
                _state.PendingAttackPatternVariant = -1;
                BattlePhaseManager.NotifyAttackSkillSelected(true, generalIndex, skillIndex, skillName);
                return;
            }

            string cardId = side.GeneralCardIds[generalIndex] ?? string.Empty;
            if (!AttackSkillNeedsPatternSubPopup(cardId, skillIndex))
            {
                _state.PendingAttackPatternVariant = -1;
                BattlePhaseManager.NotifyAttackSkillSelected(true, generalIndex, skillIndex, skillName);
                return;
            }

            CloseChoicePopup();
            OpenCeMaAttackPatternPopup(generalIndex, skillIndex, skillName);
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
            _discardPhaseTitle = CreateGameText(titleGo.transform, "\u8bf7\u5f03\u7f6e0\u5f20\u724c", 28);
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
            var ct = CreateGameText(confirmGo.transform, "\u786e\u8ba4", 22);
            SetFullRect(ct.GetComponent<RectTransform>());
        }

        private static void BuildBattleFlowLogUI()
        {
            _battleFlowLogDock = new GameObject("BattleFlowLogDock");
            _battleFlowLogDock.transform.SetParent(_root.transform, false);
            var dockRect = _battleFlowLogDock.AddComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(0f, 0.5f);
            dockRect.anchorMax = new Vector2(0f, 0.5f);
            dockRect.pivot = new Vector2(0f, 0.5f);
            dockRect.anchoredPosition = new Vector2(14f, 0f);
            dockRect.sizeDelta = new Vector2(248f, 198f);
            var dockImg = _battleFlowLogDock.AddComponent<Image>();
            dockImg.color = new Color(0.14f, 0.16f, 0.2f, 0.92f);
            dockImg.raycastTarget = true;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_battleFlowLogDock.transform, false);
            var titleR = titleGo.AddComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0f, 1f);
            titleR.anchorMax = new Vector2(1f, 1f);
            titleR.pivot = new Vector2(0.5f, 1f);
            titleR.anchoredPosition = new Vector2(0, -6f);
            titleR.sizeDelta = new Vector2(0, 32f);
            CreateGameText(titleGo.transform, "\u6218\u62a5", 17);

            var allBtnGo = new GameObject("AllBtn");
            allBtnGo.transform.SetParent(_battleFlowLogDock.transform, false);
            var allR = allBtnGo.AddComponent<RectTransform>();
            allR.anchorMin = new Vector2(1f, 1f);
            allR.anchorMax = new Vector2(1f, 1f);
            allR.pivot = new Vector2(1f, 1f);
            allR.anchoredPosition = new Vector2(-6f, -4f);
            allR.sizeDelta = new Vector2(56f, 28f);
            allBtnGo.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.5f, 1f);
            _battleFlowLogAllButton = allBtnGo.AddComponent<Button>();
            _battleFlowLogAllButton.onClick.AddListener(OpenBattleFlowLogAllModal);
            CreateGameText(allBtnGo.transform, "\u5168\u90e8", 16);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_battleFlowLogDock.transform, false);
            var scrollR = scrollGo.AddComponent<RectTransform>();
            scrollR.anchorMin = Vector2.zero;
            scrollR.anchorMax = Vector2.one;
            scrollR.offsetMin = new Vector2(6f, 8f);
            scrollR.offsetMax = new Vector2(-6f, -40f);

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpR = viewport.AddComponent<RectTransform>();
            SetFullRect(vpR);
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 1f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentR = content.AddComponent<RectTransform>();
            contentR.anchorMin = new Vector2(0f, 1f);
            contentR.anchorMax = new Vector2(1f, 1f);
            contentR.pivot = new Vector2(0.5f, 1f);
            contentR.anchoredPosition = Vector2.zero;
            contentR.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.content = contentR;
            sr.viewport = vpR;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            _battleFlowLogListContent = content.transform;
            _battleFlowLogDockScroll = sr;

            _battleFlowLogModalRoot = new GameObject("BattleFlowLogModal");
            _battleFlowLogModalRoot.transform.SetParent(_root.transform, false);
            _battleFlowLogModalRoot.SetActive(false);
            SetFullRect(_battleFlowLogModalRoot.AddComponent<RectTransform>());
            var modalCanvas = _battleFlowLogModalRoot.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 95;
            _battleFlowLogModalRoot.AddComponent<GraphicRaycaster>();

            var modalBg = new GameObject("Bg");
            modalBg.transform.SetParent(_battleFlowLogModalRoot.transform, false);
            var bgImg = modalBg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.65f);
            bgImg.raycastTarget = true;
            SetFullRect(modalBg.transform as RectTransform);
            var bgBtn = modalBg.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(CloseBattleFlowLogModal);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_battleFlowLogModalRoot.transform, false);
            var pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(760f, 560f);
            panel.AddComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 0.98f);

            var modalTitleGo = new GameObject("ModalTitle");
            modalTitleGo.transform.SetParent(panel.transform, false);
            var mtr = modalTitleGo.AddComponent<RectTransform>();
            mtr.anchorMin = new Vector2(0f, 1f);
            mtr.anchorMax = new Vector2(1f, 1f);
            mtr.pivot = new Vector2(0f, 1f);
            mtr.anchoredPosition = new Vector2(16f, -12f);
            mtr.sizeDelta = new Vector2(-120f, 36f);
            _battleFlowLogModalTitle = CreateGameText(modalTitleGo.transform, "\u6218\u62a5\u8be6\u60c5", 20, TextAlignmentOptions.Left);
            var mTitleTmpR = _battleFlowLogModalTitle.GetComponent<RectTransform>();
            mTitleTmpR.anchorMin = Vector2.zero;
            mTitleTmpR.anchorMax = Vector2.one;
            mTitleTmpR.offsetMin = Vector2.zero;
            mTitleTmpR.offsetMax = Vector2.zero;

            var closeGo = new GameObject("Close");
            closeGo.transform.SetParent(panel.transform, false);
            var cr = closeGo.AddComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(1f, 1f);
            cr.pivot = new Vector2(1f, 1f);
            cr.anchoredPosition = new Vector2(-10f, -8f);
            cr.sizeDelta = new Vector2(72f, 32f);
            closeGo.AddComponent<Image>().color = new Color(0.4f, 0.35f, 0.35f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseBattleFlowLogModal);
            CreateGameText(closeGo.transform, "\u5173\u95ed", 18);

            var mScrollGo = new GameObject("ModalScroll");
            mScrollGo.transform.SetParent(panel.transform, false);
            var msR = mScrollGo.AddComponent<RectTransform>();
            msR.anchorMin = Vector2.zero;
            msR.anchorMax = Vector2.one;
            msR.offsetMin = new Vector2(16f, 16f);
            msR.offsetMax = new Vector2(-14f, -50f);

            var mVp = new GameObject("Viewport");
            mVp.transform.SetParent(mScrollGo.transform, false);
            var mVpR = mVp.AddComponent<RectTransform>();
            SetFullRect(mVpR);
            mVp.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 1f);
            mVp.AddComponent<Mask>().showMaskGraphic = false;

            var mContent = new GameObject("Content");
            mContent.transform.SetParent(mVp.transform, false);
            var mcR = mContent.AddComponent<RectTransform>();
            mcR.anchorMin = new Vector2(0f, 1f);
            mcR.anchorMax = new Vector2(1f, 1f);
            mcR.pivot = new Vector2(0.5f, 1f);
            mcR.anchoredPosition = Vector2.zero;
            mcR.sizeDelta = new Vector2(0f, 200f);
            _battleFlowLogModalContentRt = mcR;

            _battleFlowLogModalBody = CreateGameText(mContent.transform, "", 16, TextAlignmentOptions.TopLeft);
            _battleFlowLogModalBody.enableWordWrapping = true;
            _battleFlowLogModalBody.overflowMode = TextOverflowModes.Overflow;
            _battleFlowLogModalBody.raycastTarget = true;

            var msRect = mScrollGo.AddComponent<ScrollRect>();
            msRect.content = mcR;
            msRect.viewport = mVpR;
            msRect.horizontal = false;
            msRect.vertical = true;
            msRect.scrollSensitivity = 28f;
            msRect.movementType = ScrollRect.MovementType.Clamped;
            msRect.inertia = true;
            _battleFlowLogModalScroll = msRect;
        }

        private static void LayoutBattleFlowLogModalContent()
        {
            if (_battleFlowLogModalBody == null || _battleFlowLogModalScroll == null || _battleFlowLogModalContentRt == null)
                return;

            Canvas.ForceUpdateCanvases();
            RectTransform vpRt = _battleFlowLogModalScroll.viewport;
            LayoutRebuilder.ForceRebuildLayoutImmediate(vpRt);

            const float pad = 12f;
            float viewW = vpRt.rect.width;
            if (viewW < 8f)
                viewW = 700f;

            float textW = Mathf.Max(40f, viewW - pad * 2f);
            string bodyText = _battleFlowLogModalBody.text ?? string.Empty;
            var pref = _battleFlowLogModalBody.GetPreferredValues(bodyText, textW, 0f);
            float textH = Mathf.Max(pref.y, 20f);

            RectTransform tmpRt = _battleFlowLogModalBody.rectTransform;
            tmpRt.anchorMin = new Vector2(0f, 1f);
            tmpRt.anchorMax = new Vector2(0f, 1f);
            tmpRt.pivot = new Vector2(0f, 1f);
            tmpRt.anchoredPosition = new Vector2(pad, -pad);
            tmpRt.sizeDelta = new Vector2(textW, textH);

            float contentH = textH + pad * 2f;
            _battleFlowLogModalContentRt.sizeDelta = new Vector2(0f, contentH);

            _battleFlowLogModalScroll.verticalNormalizedPosition = 0f;
        }

        private static void RefreshBattleFlowLogPanel()
        {
            if (_battleFlowLogListContent == null)
                return;

            foreach (Transform t in _battleFlowLogListContent)
                UnityEngine.Object.Destroy(t.gameObject);

            var entries = BattleFlowLog.Entries;
            int start = System.Math.Max(0, entries.Count - BattleFlowLogSideVisibleMax);
            for (int i = start; i < entries.Count; i++)
            {
                BattleFlowLog.Entry e = entries[i];
                var row = new GameObject("Row" + i);
                row.transform.SetParent(_battleFlowLogListContent, false);
                float rowH = string.IsNullOrEmpty(e.Line) ? 10f : 42f;
                if (e.ExtraTopMargin)
                    rowH += 18f;
                var le = row.AddComponent<LayoutElement>();
                le.preferredHeight = rowH;
                le.minHeight = rowH;
                var img = row.AddComponent<Image>();
                img.color = new Color(0.2f, 0.24f, 0.3f, 0.95f);
                img.raycastTarget = false;
                string line = e.Line ?? string.Empty;
                var txt = CreateGameText(row.transform, line, 13, TextAlignmentOptions.Left);
                txt.enableWordWrapping = true;
                txt.overflowMode = TextOverflowModes.Ellipsis;
                txt.raycastTarget = false;
                if (string.IsNullOrEmpty(line))
                    txt.text = " ";
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(6f, 4f);
                tr.offsetMax = new Vector2(-6f, -4f);
            }

            Canvas.ForceUpdateCanvases();
            if (_battleFlowLogDockScroll != null && entries.Count > 0)
                _battleFlowLogDockScroll.verticalNormalizedPosition = 0f;
        }

        private static void OpenBattleFlowLogAllModal()
        {
            var entries = BattleFlowLog.Entries;
            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                var en = entries[i];
                if (en.ExtraTopMargin)
                    sb.AppendLine();
                sb.AppendLine(en.Line ?? string.Empty);
            }

            _battleFlowLogModalTitle.text = "\u5168\u90e8\u6218\u62a5";
            _battleFlowLogModalBody.text = sb.Length == 0 ? "\u6682\u65e0\u8bb0\u5f55" : sb.ToString();
            _battleFlowLogModalRoot.transform.SetAsLastSibling();
            CollapsePlayerHandIfExpanded();
            _battleFlowLogModalRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutBattleFlowLogModalContent();
        }

        private static void CloseBattleFlowLogModal()
        {
            if (_battleFlowLogModalRoot != null)
                _battleFlowLogModalRoot.SetActive(false);
        }

        private static void OpenDiscardPhasePopup(bool isPlayer, int needCount, bool preserveSelection = false)
        {
            _discardPhaseIsPlayer = isPlayer;
            _discardPhaseNeedCount = needCount;
            if (!preserveSelection)
                _discardPhaseSelectedIndices.Clear();
            var side = isPlayer ? _state.Player : _state.Opponent;
            CollapsePlayerHandIfExpanded();
            _discardPhasePopupRoot.SetActive(true);
            if (_discardPhaseTitle != null)
                _discardPhaseTitle.text = "请弃置" + needCount + "张牌";
            foreach (Transform t in _discardPhaseContent)
                UnityEngine.Object.Destroy(t.gameObject);
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

            if (preserveSelection && _discardPhaseContent != null)
            {
                for (int i = 0; i < _discardPhaseContent.childCount; i++)
                {
                    if (!_discardPhaseSelectedIndices.Contains(i))
                        continue;
                    var ch = _discardPhaseContent.GetChild(i);
                    var hi = ch != null ? ch.GetComponent<Image>() : null;
                    if (hi != null)
                        hi.color = new Color(0.4f, 0.6f, 0.9f, 1f);
                }
            }
        }

        private static void OnPlayerHandOrganizeClicked()
        {
            if (_state == null || !_playerHandExpanded)
                return;
            if (_isOnlineMode)
            {
                ToastUI.Show("\u8054\u673a\u5c40\u4e0d\u652f\u6301\u6574\u7406\u624b\u724c", 2.5f);
                return;
            }
            if (!_state.IsPlayerTurn)
                return;

            bool discardPopupPlayer = _discardPhasePopupRoot != null && _discardPhasePopupRoot.activeSelf && _discardPhaseIsPlayer;
            List<PokerCard> savedPick = null;
            if (discardPopupPlayer && _discardPhaseSelectedIndices.Count > 0)
            {
                var h = _state.Player.Hand;
                savedPick = new List<PokerCard>();
                foreach (int si in _discardPhaseSelectedIndices.OrderBy(x => x))
                {
                    if (si >= 0 && si < h.Count)
                        savedPick.Add(h[si]);
                }
            }

            BattleState.SortHandOrganize(_state.Player.Hand);

            if (savedPick != null && savedPick.Count > 0)
            {
                var used = new HashSet<int>();
                _discardPhaseSelectedIndices.Clear();
                foreach (var card in savedPick)
                {
                    int found = -1;
                    for (int i = 0; i < _state.Player.Hand.Count; i++)
                    {
                        if (used.Contains(i))
                            continue;
                        if (BattleState.HandCardIdentityEquals(_state.Player.Hand[i], card))
                        {
                            found = i;
                            break;
                        }
                    }
                    if (found >= 0)
                    {
                        used.Add(found);
                        _discardPhaseSelectedIndices.Add(found);
                    }
                }
            }

            if (discardPopupPlayer)
            {
                RefreshHandCards();
                FinishPlayerHandOrganizeVisualRefresh();
                OpenDiscardPhasePopup(true, _discardPhaseNeedCount, preserveSelection: true);
            }
            else
            {
                RefreshAllFromState();
                FinishPlayerHandOrganizeVisualRefresh();
            }
        }

        /// <summary>整理手牌后强制本帧完成布局与视口计算，避免 Destroy 延迟导致子物体数量错乱或滚动区未更新。</summary>
        private static void FinishPlayerHandOrganizeVisualRefresh()
        {
            if (!_playerHandExpanded || _playerHandContent == null)
                return;
            Canvas.ForceUpdateCanvases();
            if (_playerHandContent is RectTransform hrt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(hrt);
            if (_playerHandViewportRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_playerHandViewportRt);
            Canvas.ForceUpdateCanvases();
            if (_playerHandRightMouseScroll != null)
                _playerHandRightMouseScroll.ClampToBounds(true);
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
                ToastUI.Show("\u8bf7\u9009\u62e9\u4e0e\u8981\u6c42\u6570\u91cf\u4e00\u81f4\u7684\u724c");
                return;
            }
            _discardPhasePopupRoot.SetActive(false);
            BattlePhaseManager.NotifyDiscardPhaseDone(_discardPhaseIsPlayer, _discardPhaseSelectedIndices.ToArray());
            RefreshAllFromState();
        }

        private static void BuildJushuiDuanqiaoPopup()
        {
            _jushuiPopupRoot = new GameObject("JushuiDuanqiaoPopup");
            _jushuiPopupRoot.transform.SetParent(_root.transform, false);
            _jushuiPopupRoot.SetActive(false);
            SetFullRect(_jushuiPopupRoot.AddComponent<RectTransform>());
            var canvas = _jushuiPopupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 46;
            _jushuiPopupRoot.AddComponent<GraphicRaycaster>();
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_jushuiPopupRoot.transform, false);
            var ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.6f);
            ovImg.raycastTarget = true;
            SetFullRect(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Button>().transition = Selectable.Transition.None;
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_jushuiPopupRoot.transform, false);
            var panelR = panel.AddComponent<RectTransform>();
            panelR.anchorMin = new Vector2(0.5f, 0.5f);
            panelR.anchorMax = new Vector2(0.5f, 0.5f);
            panelR.pivot = new Vector2(0.5f, 0.5f);
            panelR.anchoredPosition = Vector2.zero;
            panelR.sizeDelta = new Vector2(1000, 480);
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleR = titleGo.AddComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0.5f, 1f);
            titleR.anchorMax = new Vector2(0.5f, 1f);
            titleR.pivot = new Vector2(0.5f, 1f);
            titleR.anchoredPosition = new Vector2(0, -12);
            titleR.sizeDelta = new Vector2(920, 36);
            _jushuiTitle = CreateGameText(titleGo.transform, "\u636e\u6c34\u65ad\u6865", 28);
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(panel.transform, false);
            var subR = subGo.AddComponent<RectTransform>();
            subR.anchorMin = new Vector2(0.5f, 1f);
            subR.anchorMax = new Vector2(0.5f, 1f);
            subR.pivot = new Vector2(0.5f, 1f);
            subR.anchoredPosition = new Vector2(0, -48);
            subR.sizeDelta = new Vector2(920, 28);
            _jushuiSubtitle = CreateGameText(subGo.transform, "", 20);
            _jushuiSubtitle.enableWordWrapping = false;
            _jushuiSubtitle.overflowMode = TextOverflowModes.Overflow;
            _jushuiSubtitle.alignment = TextAlignmentOptions.Center;
            SetFullRect(_jushuiSubtitle.GetComponent<RectTransform>());
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollR = scrollGo.AddComponent<RectTransform>();
            scrollR.anchorMin = new Vector2(0f, 0f);
            scrollR.anchorMax = new Vector2(1f, 1f);
            scrollR.offsetMin = new Vector2(16, 64);
            scrollR.offsetMax = new Vector2(-16, -88);
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
            _jushuiContent = content.transform;
            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(panel.transform, false);
            var btnRowR = btnRow.AddComponent<RectTransform>();
            btnRowR.anchorMin = new Vector2(0.5f, 0f);
            btnRowR.anchorMax = new Vector2(0.5f, 0f);
            btnRowR.pivot = new Vector2(0.5f, 0f);
            btnRowR.anchoredPosition = new Vector2(0, 12);
            btnRowR.sizeDelta = new Vector2(360, 44);
            var cancelGo = new GameObject("Cancel");
            cancelGo.transform.SetParent(btnRow.transform, false);
            var cancelR = cancelGo.AddComponent<RectTransform>();
            cancelR.anchorMin = new Vector2(0f, 0f);
            cancelR.anchorMax = new Vector2(0.5f, 1f);
            cancelR.offsetMin = new Vector2(0, 0);
            cancelR.offsetMax = new Vector2(-6, 0);
            cancelGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f, 1f);
            _jushuiCancelBtn = cancelGo.AddComponent<Button>();
            _jushuiCancelBtn.onClick.AddListener(OnJushuiDuanqiaoCancel);
            CreateGameText(cancelGo.transform, "\u53d6\u6d88", 22);
            var confirmGo = new GameObject("Confirm");
            confirmGo.transform.SetParent(btnRow.transform, false);
            var confirmR = confirmGo.AddComponent<RectTransform>();
            confirmR.anchorMin = new Vector2(0.5f, 0f);
            confirmR.anchorMax = new Vector2(1f, 1f);
            confirmR.offsetMin = new Vector2(6, 0);
            confirmR.offsetMax = new Vector2(0, 0);
            confirmGo.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.5f, 1f);
            _jushuiConfirmBtn = confirmGo.AddComponent<Button>();
            _jushuiConfirmBtn.onClick.AddListener(OnJushuiDuanqiaoConfirm);
            CreateGameText(confirmGo.transform, "\u786e\u5b9a", 22);
        }

        private static bool IsJushuiDuanqiaoSkill(CardData data, int skillIndex)
        {
            if (data == null || string.IsNullOrEmpty(data.CardId))
                return false;
            var r = SkillRuleLoader.GetRule(data.CardId, skillIndex);
            return r != null && string.Equals(r.EffectId, OfflineSkillEngine.PrimaryRecoverDiscardExtraPhaseEffectId, StringComparison.Ordinal);
        }

        private static string FlowTurnBracketForBattleLog()
        {
            if (_state == null)
                return string.Empty;
            return _state.IsPlayerTurn ? "\u3010\u5df1\u65b9\u56de\u5408\u3011" : "\u3010\u654c\u65b9\u56de\u5408\u3011";
        }

        private static void OpenJushuiDuanqiaoPopup(int generalIndex, int skillIndex)
        {
            if (_state == null || _jushuiPopupRoot == null)
                return;

            CollapsePlayerHandIfExpanded();
            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(_state.Player.GeneralCardIds[generalIndex]));
            var rule = data != null ? SkillRuleLoader.GetRule(data.CardId, skillIndex) : null;
            _jushuiMaxPick = rule != null && rule.Value2 > 0 ? rule.Value2 : 4;
            _jushuiGeneralIndex = generalIndex;
            _jushuiSkillIndex = skillIndex;
            _jushuiSelectedOrder.Clear();
            _jushuiSelectedSet.Clear();
            _jushuiSnapshot.Clear();
            _jushuiSnapshot.AddRange(_state.Player.DiscardPile);

            if (_jushuiTitle != null)
                _jushuiTitle.text = rule != null && !string.IsNullOrEmpty(rule.SkillName) ? rule.SkillName : "\u636e\u6c34\u65ad\u6865";
            if (_jushuiSubtitle != null)
                _jushuiSubtitle.text = "\u4ece\u4f60\u7684\u5f03\u724c\u5806\u4e2d\u83b7\u5f97\u81f3\u591a" + _jushuiMaxPick + "\u5f20\u724c";

            foreach (Transform t in _jushuiContent)
                UnityEngine.Object.Destroy(t.gameObject);

            float cardW = 80f;
            float cardH = cardW * CardAspectH / CardAspectW;
            for (int i = 0; i < _jushuiSnapshot.Count; i++)
            {
                int index = i;
                var pc = _jushuiSnapshot[i];
                var item = new GameObject("DiscardItem");
                item.transform.SetParent(_jushuiContent, false);
                var le = item.AddComponent<LayoutElement>();
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;
                var img = item.AddComponent<Image>();
                img.color = new Color(0.22f, 0.26f, 0.32f, 1f);
                var btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => ToggleJushuiDuanqiaoSelection(index, img));
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(item.transform, false);
                var label = CreateGameText(labelGo.transform, pc.DisplayName, 16);
                SetFullRect(label.GetComponent<RectTransform>());
            }

            _jushuiPopupRoot.transform.SetAsLastSibling();
            _jushuiPopupRoot.SetActive(true);
        }

        private static void ToggleJushuiDuanqiaoSelection(int index, Image img)
        {
            if (_jushuiSelectedSet.Contains(index))
            {
                _jushuiSelectedSet.Remove(index);
                _jushuiSelectedOrder.Remove(index);
                img.color = new Color(0.22f, 0.26f, 0.32f, 1f);
            }
            else if (_jushuiSelectedOrder.Count >= _jushuiMaxPick)
            {
                ToastUI.Show("\u53ea\u80fd\u81f3\u591a\u9009\u62e9\u56db\u5f20\u724c");
            }
            else
            {
                _jushuiSelectedSet.Add(index);
                _jushuiSelectedOrder.Add(index);
                img.color = new Color(0.4f, 0.6f, 0.9f, 1f);
            }
        }

        private static void OnJushuiDuanqiaoCancel()
        {
            if (_jushuiPopupRoot != null)
                _jushuiPopupRoot.SetActive(false);
        }

        private static void OnJushuiDuanqiaoConfirm()
        {
            if (_state == null)
                return;

            var side = _state.Player;
            int gi = _jushuiGeneralIndex;
            int sk = _jushuiSkillIndex;
            if (gi < 0 || gi >= side.GeneralCardIds.Count || !side.IsGeneralFaceUp(gi))
            {
                OnJushuiDuanqiaoCancel();
                return;
            }

            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[gi]));
            if (data == null || !IsJushuiDuanqiaoSkill(data, sk))
            {
                OnJushuiDuanqiaoCancel();
                return;
            }

            string skillKey = "True_" + gi + "_" + sk;
            if (side.UsedOneShotSkills.Contains(skillKey))
            {
                ToastUI.Show("\u8be5\u7834\u519b\u6280\u672c\u5c40\u5df2\u4f7f\u7528");
                OnJushuiDuanqiaoCancel();
                return;
            }

            if (_jushuiSnapshot.Count != side.DiscardPile.Count)
            {
                ToastUI.Show("\u5f03\u724c\u5806\u5df2\u53d8\u52a8\uff0c\u8bf7\u91cd\u8bd5");
                OnJushuiDuanqiaoCancel();
                return;
            }

            for (int i = 0; i < _jushuiSnapshot.Count; i++)
            {
                var a = _jushuiSnapshot[i];
                var b = side.DiscardPile[i];
                if (a.Suit != b.Suit || a.Rank != b.Rank)
                {
                    ToastUI.Show("\u5f03\u724c\u5806\u5df2\u53d8\u52a8\uff0c\u8bf7\u91cd\u8bd5");
                    OnJushuiDuanqiaoCancel();
                    return;
                }
            }

            var picked = new List<PokerCard>();
            foreach (int idx in _jushuiSelectedOrder)
            {
                if (idx >= 0 && idx < _jushuiSnapshot.Count)
                    picked.Add(_jushuiSnapshot[idx]);
            }

            foreach (int idx in _jushuiSelectedOrder.Distinct().OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < side.DiscardPile.Count)
                    side.DiscardPile.RemoveAt(idx);
            }

            foreach (var c in picked)
                side.Hand.Add(c);

            var suitKinds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in picked)
            {
                if (!string.IsNullOrEmpty(c.Suit))
                    suitKinds.Add(c.Suit);
            }

            int x = suitKinds.Count;
            if (x > 0)
            {
                side.RemoveAnyEffectLayers(x);
                side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + x);
                _state.TotalPlayPhasesThisTurn += x;
            }

            side.UsedOneShotSkills.Add(skillKey);

            string skillDisplay = GetSkillName(data, sk);
            string roleName = GetGeneralDisplayName(true, gi);
            string campActor = _state.IsPlayerTurn ? "\u5df1\u65b9" : "\u654c\u65b9";
            string pileOwner = _state.IsPlayerTurn ? "\u5df1\u65b9" : "\u654c\u65b9";
            string cardPart = picked.Count == 0
                ? "\u65e0"
                : string.Join("\u3001", picked.Select(p => p.DisplayName));

            var banner = new StringBuilder();
            banner.Append(FlowTurnBracketForBattleLog());
            banner.Append(campActor).Append("\u89d2\u8272\u3010").Append(roleName).Append("\u3011\u4f7f\u7528\u6280\u80fd\u3010").Append(skillDisplay).Append("\u3011\uff0c\u4ece");
            banner.Append(pileOwner).Append("\u5f03\u724c\u5806\u4e2d\u56de\u6536\u4e86").Append(picked.Count).Append("\u5f20\u724c\uff0c\u5206\u522b\u4e3a\uff1a");
            banner.Append(cardPart);
            banner.Append("\uff0c\u5e76\u6062\u590d\u4e86").Append(x).Append("\u70b9\u751f\u547d\uff0c\u672c\u56de\u5408\u4e2d\u83b7\u5f97\u4e86").Append(x).Append("\u4e2a\u51fa\u724c\u9636\u6bb5");

            string line = banner.ToString();
            SkillEffectBanner.ShowRawLine(line);
            BattleFlowLog.Add(line);

            if (_jushuiPopupRoot != null)
                _jushuiPopupRoot.SetActive(false);

            RefreshAllFromState();
        }

        private const float GeneralCardWidth = 140f;
        private const float GeneralGap = 36f;
        private const float SkillButtonHeight = 58f;
        private const float SkillButtonSpacing = 20f;
        private const float SkillToCardGap = 16f;
        private const int MaxSkillButtons = 3;
        private const float CardAreaBorderInset = 4f;
        private const float SkillButtonWidth = 68f;
        private const float RoleHeaderHeight = 30f;
        private const float RoleHeaderGap = 10f;
        private const float CharacterAreaOffsetX = -110f;
        private const float CharacterSideColumnWidth = 220f;
        private const float HpBarWidth = 56f;
        private const float HpBarHeight = 100f;
        private const float HpToGeneralGap = 30f;
        private const float GeneralRowWidth = SkillButtonWidth + SkillToCardGap + GeneralCardWidth;
        private const float GeneralContainerWidth = 3f * GeneralRowWidth + 2f * GeneralGap;
        private const float CharacterAreaTotalWidth = CharacterSideColumnWidth + HpBarWidth + HpToGeneralGap + GeneralContainerWidth;

        private static void BuildOpponentGenerals()
        {
            float cardW = GeneralCardWidth;
            float cardH = cardW * CardAspectH / CardAspectW;
            float gap = GeneralGap;
            float visibleH = cardH;
            var area = new GameObject("OpponentCharacterArea");
            area.transform.SetParent(_root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0.5f, 1f - 0.32f);
            areaRect.anchorMax = new Vector2(0.5f, 1f);
            areaRect.pivot = new Vector2(0.5f, 1f);
            areaRect.anchoredPosition = new Vector2(CharacterAreaOffsetX, 0f);
            areaRect.sizeDelta = new Vector2(CharacterAreaTotalWidth, 0f);
            var areaHlg = area.AddComponent<HorizontalLayoutGroup>();
            areaHlg.spacing = 0f;
            areaHlg.childAlignment = TextAnchor.LowerLeft;
            areaHlg.childForceExpandWidth = false;
            areaHlg.childControlWidth = true;
            areaHlg.childControlHeight = true;

            var oppSpacer = new GameObject("OpponentDeckColumnSpacer");
            oppSpacer.transform.SetParent(area.transform, false);
            oppSpacer.AddComponent<RectTransform>();
            var oppSpacerLe = oppSpacer.AddComponent<LayoutElement>();
            oppSpacerLe.preferredWidth = CharacterSideColumnWidth;
            oppSpacerLe.flexibleWidth = 0;

            _opponentHpRoot = new GameObject("OpponentHpBar");
            _opponentHpRoot.transform.SetParent(area.transform, false);
            _opponentHpRoot.AddComponent<RectTransform>();
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

            var oppHpGap = new GameObject("OpponentHpGap");
            oppHpGap.transform.SetParent(area.transform, false);
            oppHpGap.AddComponent<RectTransform>();
            var oppHpGapLe = oppHpGap.AddComponent<LayoutElement>();
            oppHpGapLe.preferredWidth = HpToGeneralGap;
            oppHpGapLe.flexibleWidth = 0;

            var container = new GameObject("OpponentGeneralsContainer");
            container.transform.SetParent(area.transform, false);
            var ocLe = container.AddComponent<LayoutElement>();
            ocLe.preferredWidth = GeneralContainerWidth;
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

        private static void BuildPlayerGenerals()
        {
            float cardW = GeneralCardWidth;
            float cardH = cardW * CardAspectH / CardAspectW;
            float gap = GeneralGap;
            float visibleH = cardH;
            var area = new GameObject("PlayerCharacterArea");
            area.transform.SetParent(_root.transform, false);
            var areaRect = area.AddComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0.5f, 0f);
            areaRect.anchorMax = new Vector2(0.5f, 0.32f);
            areaRect.pivot = new Vector2(0.5f, 0f);
            areaRect.anchoredPosition = new Vector2(CharacterAreaOffsetX, 0f);
            areaRect.sizeDelta = new Vector2(CharacterAreaTotalWidth, 0f);
            var areaHlg = area.AddComponent<HorizontalLayoutGroup>();
            areaHlg.spacing = 0f;
            areaHlg.childAlignment = TextAnchor.UpperLeft;
            areaHlg.childForceExpandWidth = false;
            areaHlg.childControlWidth = true;
            areaHlg.childControlHeight = true;

            var playerSpacer = new GameObject("PlayerDeckColumnSpacer");
            playerSpacer.transform.SetParent(area.transform, false);
            playerSpacer.AddComponent<RectTransform>();
            var playerSpacerLe = playerSpacer.AddComponent<LayoutElement>();
            playerSpacerLe.preferredWidth = CharacterSideColumnWidth;
            playerSpacerLe.flexibleWidth = 0;

            _playerHpRoot = new GameObject("PlayerHpBar");
            _playerHpRoot.transform.SetParent(area.transform, false);
            _playerHpRoot.AddComponent<RectTransform>();
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

            var hpGap = new GameObject("PlayerHpGap");
            hpGap.transform.SetParent(area.transform, false);
            hpGap.AddComponent<RectTransform>();
            var hpGapLe = hpGap.AddComponent<LayoutElement>();
            hpGapLe.preferredWidth = HpToGeneralGap;
            hpGapLe.flexibleWidth = 0;

            var container = new GameObject("PlayerGeneralsContainer");
            container.transform.SetParent(area.transform, false);
            var cLe = container.AddComponent<LayoutElement>();
            cLe.preferredWidth = GeneralContainerWidth;
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
            float mainAreaH = Mathf.Max(skillAreaMaxH, visibleH);
            float rowH = RoleHeaderHeight + RoleHeaderGap + mainAreaH;

            var row = new GameObject((isOpponent ? "Opponent" : "Player") + "General_" + index);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredWidth = GeneralRowWidth;
            rowLe.preferredHeight = rowH;
            rowLe.flexibleWidth = 0;

            var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = SkillToCardGap;
            rowHlg.padding = new RectOffset(0, 0, Mathf.RoundToInt(RoleHeaderHeight + RoleHeaderGap), 0);
            rowHlg.childAlignment = TextAnchor.LowerLeft;
            rowHlg.childForceExpandHeight = false;
            rowHlg.childControlHeight = true;
            rowHlg.childControlWidth = true;
            rowHlg.childForceExpandWidth = false;

            var header = new GameObject("RoleHeader");
            header.transform.SetParent(row.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, RoleHeaderHeight);
            var headerImage = header.AddComponent<Image>();
            headerImage.color = isOpponent ? new Color(0.46f, 0.25f, 0.24f, 0.96f) : new Color(0.22f, 0.36f, 0.54f, 0.96f);
            headerImage.sprite = GetWhiteSprite();
            headerImage.raycastTarget = false;
            var headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.ignoreLayout = true;
            var headerText = CreateGameText(header.transform, "\u6b66\u5c06" + (index + 1), 18);
            if (headerText != null)
            {
                var headerTextRect = headerText.GetComponent<RectTransform>();
                SetFullRect(headerTextRect);
                headerTextRect.offsetMin = new Vector2(8f, 2f);
                headerTextRect.offsetMax = new Vector2(-8f, -2f);
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.enableAutoSizing = true;
                headerText.fontSizeMin = 12f;
                headerText.fontSizeMax = 18f;
                headerText.enableWordWrapping = false;
                headerText.overflowMode = TextOverflowModes.Ellipsis;
                headerText.raycastTarget = false;
            }

            var skillContainer = new GameObject("SkillButtons");
            skillContainer.transform.SetParent(row.transform, false);
            var skillVlg = skillContainer.AddComponent<VerticalLayoutGroup>();
            skillVlg.spacing = SkillButtonSpacing;
            skillVlg.childAlignment = TextAnchor.MiddleCenter;
            skillVlg.childForceExpandHeight = false;
            skillVlg.childControlHeight = true;
            skillVlg.childControlWidth = true;
            skillVlg.childForceExpandWidth = false;
            var skillLe = skillContainer.AddComponent<LayoutElement>();
            skillLe.preferredWidth = SkillButtonWidth;
            skillLe.preferredHeight = mainAreaH;
            skillLe.flexibleWidth = 0;
            skillLe.flexibleHeight = 0;

            var cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(row.transform, false);
            cardContainer.AddComponent<RectTransform>();
            var cardContainerLe = cardContainer.AddComponent<LayoutElement>();
            cardContainerLe.preferredWidth = cardW;
            cardContainerLe.preferredHeight = mainAreaH;
            cardContainerLe.flexibleWidth = 0;
            cardContainerLe.flexibleHeight = 0;

            var cardPart = new GameObject("CardPart");
            cardPart.transform.SetParent(cardContainer.transform, false);
            var cardPartRect = cardPart.AddComponent<RectTransform>();
            cardPartRect.anchorMin = new Vector2(0f, 0.5f);
            cardPartRect.anchorMax = new Vector2(1f, 0.5f);
            cardPartRect.pivot = new Vector2(0.5f, 0.5f);
            cardPartRect.anchoredPosition = Vector2.zero;
            cardPartRect.sizeDelta = new Vector2(0f, visibleH);
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

            var cardAnchor = new GameObject("CardAnchor");
            cardAnchor.transform.SetParent(cardPart.transform, false);
            var anchorRect = cardAnchor.AddComponent<RectTransform>();
            anchorRect.anchorMin = new Vector2(0.5f, 0.5f);
            anchorRect.anchorMax = new Vector2(0.5f, 0.5f);
            anchorRect.pivot = new Vector2(0.5f, 0.5f);
            anchorRect.anchoredPosition = Vector2.zero;
            float maxAnchorWidth = cardW - CardAreaBorderInset * 2f;
            float maxAnchorHeight = visibleH - CardAreaBorderInset * 2f;
            float anchorWidth = maxAnchorWidth;
            float anchorHeight = anchorWidth * CardAspectH / CardAspectW;
            if (anchorHeight > maxAnchorHeight)
            {
                anchorHeight = maxAnchorHeight;
                anchorWidth = anchorHeight * CardAspectW / CardAspectH;
            }
            anchorRect.sizeDelta = new Vector2(anchorWidth, anchorHeight);

            var block = cardPartBorder;
            block.color = Color.clear;
            var holder = row.AddComponent<GeneralCardHolder>();
            if (holder == null)
            {
                UnityEngine.Object.Destroy(row);
                return null;
            }
            holder.CardSlot = cardAnchor.transform;
            holder.CardIndex = index;
            holder.IsPlayer = !isOpponent;
            holder.RoleHeaderLabel = headerText;

            var faceDownOverlay = new GameObject("FaceDownOverlay");
            faceDownOverlay.transform.SetParent(cardPart.transform, false);
            var faceDownRect = faceDownOverlay.AddComponent<RectTransform>();
            faceDownRect.anchorMin = Vector2.zero;
            faceDownRect.anchorMax = Vector2.one;
            faceDownRect.offsetMin = Vector2.zero;
            faceDownRect.offsetMax = Vector2.zero;
            var faceDownImage = faceDownOverlay.AddComponent<Image>();
            faceDownImage.color = new Color(0f, 0f, 0f, 0.8f);
            faceDownImage.raycastTarget = false;
            var faceDownText = CreateGameText(faceDownOverlay.transform, "\u5df2\u7ffb\u9762", 22);
            if (faceDownText != null)
            {
                faceDownText.color = Color.white;
                faceDownText.alignment = TextAlignmentOptions.Center;
                SetFullRect(faceDownText.GetComponent<RectTransform>());
            }

            faceDownOverlay.SetActive(false);
            holder.FaceDownOverlay = faceDownOverlay;

            if (!isOpponent)
            {
                var dragHit = new GameObject("GeneralDragHit");
                dragHit.transform.SetParent(cardPart.transform, false);
                var dRect = dragHit.AddComponent<RectTransform>();
                dRect.anchorMin = Vector2.zero;
                dRect.anchorMax = Vector2.one;
                dRect.offsetMin = new Vector2(CardAreaBorderInset, CardAreaBorderInset);
                dRect.offsetMax = new Vector2(-CardAreaBorderInset, -CardAreaBorderInset);
                var dImg = dragHit.AddComponent<Image>();
                dImg.color = Color.clear;
                dImg.raycastTarget = true;
                dragHit.AddComponent<GeneralCardDragToPlayedZone>();
            }

            var skillButtons = new List<Button>(MaxSkillButtons);
            var skillLabels = new List<TextMeshProUGUI>(MaxSkillButtons);
            for (int i = 0; i < MaxSkillButtons; i++)
            {
                int skillIndex = i;
                var btnGo = new GameObject("SkillBtn_" + i);
                btnGo.transform.SetParent(skillContainer.transform, false);
                btnGo.AddComponent<RectTransform>();
                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.preferredHeight = SkillButtonHeight;
                btnLe.preferredWidth = SkillButtonWidth;
                btnLe.flexibleWidth = 0;
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.4f, 0.4f, 0.45f, 1f);
                btnImg.sprite = GetWhiteSprite();
                var labelT = CreateGameText(btnGo.transform, "", 15);
                var labelRect = labelT != null ? labelT.GetComponent<RectTransform>() : null;
                if (labelRect != null)
                {
                    SetFullRect(labelRect);
                    labelRect.offsetMin = new Vector2(6f, 4f);
                    labelRect.offsetMax = new Vector2(-6f, -4f);
                }
                if (labelT != null)
                {
                    labelT.color = new Color(1f, 1f, 1f, 1f);
                    labelT.enableAutoSizing = true;
                    labelT.fontSizeMin = 11f;
                    labelT.fontSizeMax = 15f;
                    labelT.enableWordWrapping = true;
                    labelT.overflowMode = TextOverflowModes.Ellipsis;
                }
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
            panelRect.sizeDelta = new Vector2(1000f, 280f);
            _discardPopupPanelRt = panelRect;
            panel.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.26f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(400, 36);
            _discardPopupTitle = CreateGameText(titleGo.transform, "弃牌堆(0)", 28);

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
            CollapsePlayerHandIfExpanded();
            OnRequestCardEnlarge?.Invoke(cardId, isPlayerSide);
        }

        private static void HideCardEnlarge()
        {
            if (_cardEnlargeRoot != null)
                _cardEnlargeRoot.SetActive(false);
        }

        private static void OnEndTurn()
        {
            if (_isOnlineMode)
            {
                _ = OnlineClientService.EndPhaseAsync();
                return;
            }
            BattlePhaseManager.EndTurn();
        }

        private static void RefreshTurnButton()
        {
            if (_turnButton == null || _turnButtonText == null) return;
            Debug.Log("[GameUI.RefreshTurnButton] Phase=" + (_state?.CurrentPhase.ToString() ?? "null") +
                       " Step=" + (_state?.CurrentPhaseStep.ToString() ?? "null") +
                       " IsPlayerTurn=" + (_state?.IsPlayerTurn.ToString() ?? "null"));
            // 闂傚倸鍊搁崯顖濄亹閸愵亙鐒婂ù鐘差儐閳锋捇鏌ら崨濠庡晱闁哥偘绮欓弻銊モ槈濞嗗繐鎽靛┑陇灏畷鐢稿焵椤掑倹鍤€闁绘锕濠氭偄閸忕厧鈧兘鏌ｉ悢鍛婄凡婵?缂傚倸鍊烽悞锕傚箰鐠囧樊鐒芥い鎰堕檮閳锋捇鏌熺紒妯虹瑨闁?
            if (_state != null && _state.CurrentPhase == BattlePhase.Defense && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                bool playerIsDefender = !_state.IsPlayerTurn;
                if (playerIsDefender)
                {
                    _turnButtonText.text = "\u7ed3\u675f\u9632\u5fa1";
                    _turnButton.interactable = true;
                    Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 结束防御 (玩家防守中)");
                }
                else
                {
                    _turnButtonText.text = "\u5bf9\u624b\u9632\u5fa1\u4e2d";
                    _turnButton.interactable = false;
                    Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 对手防御中 (等待对手响应)");
                }
                return;
            }
            bool myTurn = _state != null && _state.IsPlayerTurn;
            if (!myTurn)
            {
                _turnButtonText.text = "\u5bf9\u624b\u56de\u5408";
                _turnButton.interactable = false;
                Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 对手回合 (按钮禁用)");
                return;
            }
            if (_state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _turnButtonText.text = "\u7ed3\u675f\u4e3b\u8981\u9636\u6bb5";
                _turnButton.interactable = true;
                Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 结束主要阶段");
                return;
            }
            if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                bool hasMorePlayPhase = _state.CurrentPlayPhaseIndex < _state.TotalPlayPhasesThisTurn - 1;
                _turnButtonText.text = hasMorePlayPhase ? "\u7ed3\u675f\u5f53\u524d\u51fa\u724c\u9636\u6bb5" : "\u7ed3\u675f\u51fa\u724c";
                _turnButton.interactable = true;
                Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: " + _turnButtonText.text);
                return;
            }
            if (_isOnlineMode)
            {
                _turnButtonText.text = "\u7ed3\u675f\u5f53\u524d\u9636\u6bb5";
                _turnButton.interactable = true;
                Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 结束当前阶段 (联机)");
                return;
            }
            _turnButtonText.text = "\u4f60\u7684\u56de\u5408";
            _turnButton.interactable = false;
            Debug.Log("[GameUI.RefreshTurnButton] >>> 显示: 你的回合 (按钮禁用)");
        }

        private static void RefreshGenericAttackButton()
        {
            if (_genericAttackButton == null)
                return;
            bool show = _state != null
                && _state.CurrentPhase == BattlePhase.Main
                && _state.CurrentPhaseStep == PhaseStep.Main
                && _state.IsPlayerTurn
                && _state.ActiveSide.PlayedThisPhase.Count > 0
                && _state.PendingAttackSkillKind == SelectedSkillKind.None;
            _genericAttackButton.gameObject.SetActive(show);
        }

        private static void OpenDiscardPopup(bool isPlayer)
        {
            if (_discardPopupRoot == null || _state == null) return;
            CollapsePlayerHandIfExpanded();
            var pile = isPlayer ? _state.Player.DiscardPile : _state.Opponent.DiscardPile;
            _discardPopupRoot.SetActive(true);
            int n = pile.Count;
            if (_discardPopupTitle != null)
            {
                string owner = isPlayer ? "\u4f60\u7684" : "\u5bf9\u624b\u7684";
                _discardPopupTitle.text = owner + "\u5f03\u724c\u5806 (" + n + ")";
            }
            foreach (Transform t in _discardPopupContent)
                UnityEngine.Object.Destroy(t.gameObject);
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

            const float scrollTopInset = 56f;
            const float scrollBottomInset = 16f;
            const float hlgVerticalPad = 16f;
            float rowContentH = cardH + hlgVerticalPad;
            if (_discardPopupContent is RectTransform contentRt)
                contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, rowContentH);
            if (_discardPopupPanelRt != null)
            {
                float panelH = scrollTopInset + rowContentH + scrollBottomInset;
                panelH = Mathf.Clamp(panelH, 200f, RefHeight * 0.85f);
                _discardPopupPanelRt.sizeDelta = new Vector2(_discardPopupPanelRt.sizeDelta.x, panelH);
            }

            Canvas.ForceUpdateCanvases();
            if (_discardPopupContent is RectTransform cr)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cr);
        }

        private static void CloseDiscardPopup()
        {
            if (_discardPopupRoot != null)
                _discardPopupRoot.SetActive(false);
        }

        public static void StartGame(DeckData selectedDeck)
        {
            _isOnlineMode = false;
            _localPlayerDisplayName = "你";
            _opponentPlayerDisplayName = "对手";
            CardTableLoader.Load();
            _cachedPlayerHandCount = -1;
            _cachedOpponentHandCount = -1;
            if (_root == null) Create();

            DeckData opponentDeck = ChooseOpponentDeck(selectedDeck);
            _state = new BattleState();
            _state.InitFromDecks(selectedDeck, opponentDeck);
            _state.PlayerGoesFirst = UnityEngine.Random.value >= 0.5f;
            _state.IsPlayerTurn = _state.PlayerGoesFirst;
            BattlePhaseManager.Bind(_state);
            BattlePhaseManager.OnDiscardMain -= OnDiscardPhaseRequest;
            BattlePhaseManager.OnAttackSelectionRequested -= OnAttackSelectionRequested;
            BattlePhaseManager.OnDiscardMain += OnDiscardPhaseRequest;
            BattlePhaseManager.OnAttackSelectionRequested += OnAttackSelectionRequested;
            BattlePhaseManager.OnGameStart();
            if (_state != null && !_state.IsPlayerTurn && IsOpponentTurnAutoEndEnabled())
            {
                while (_state != null && !_state.IsPlayerTurn && (_state.CurrentPhase == BattlePhase.Primary || _state.CurrentPhase == BattlePhase.Main))
                    BattlePhaseManager.EndTurn();
            }
            DeckSelectUI.Hide();
            _root.SetActive(true);
            RefreshAllFromState();
        }

        public static void StartOnlineGame(OnlineBattleSnapshotResponse snapshot)
        {
            _isOnlineMode = true;
            CardTableLoader.Load();
            _cachedPlayerHandCount = -1;
            _cachedOpponentHandCount = -1;
            if (_root == null) Create();
            _root.SetActive(true);
            Trace("StartOnlineGame. " + DescribeOnlineSnapshot(snapshot));
            ApplyOnlineBattleSnapshot(snapshot);
        }

        private static void ApplyOnlineBattleSnapshot(OnlineBattleSnapshotResponse snapshot)
        {
            if (snapshot == null)
                return;
            _localPlayerDisplayName = NormalizeDisplayName(snapshot.Self != null ? snapshot.Self.PlayerName : string.Empty, "你");
            _opponentPlayerDisplayName = NormalizeDisplayName(snapshot.Opponent != null ? snapshot.Opponent.PlayerName : string.Empty, "对手");
            Trace("ApplyOnlineBattleSnapshot. " + DescribeOnlineSnapshot(snapshot));
            _state = BuildBattleStateFromOnlineSnapshot(snapshot);
            RefreshAllFromState();
            Trace("Online state refreshed. phase=" + _state.CurrentPhase + ", playerHp=" + _state.Player.CurrentHp + ", opponentHp=" + _state.Opponent.CurrentHp + ", playerHand=" + _state.Player.Hand.Count + ", opponentHand=" + _state.Opponent.Hand.Count);
            if (_state.Player.CurrentHp <= 0) ShowDefeatPopup();
            if (_state.Opponent.CurrentHp <= 0) ShowVictoryPopup();
        }

        private static string NormalizeDisplayName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static BattleState BuildBattleStateFromOnlineSnapshot(OnlineBattleSnapshotResponse snapshot)
        {
            var state = new BattleState();
            state.HandLimit = snapshot.HandLimit;
            state.TotalPlayPhasesThisTurn = snapshot.TotalPlayPhasesThisTurn;
            state.CurrentPlayPhaseIndex = snapshot.CurrentPlayPhaseIndex;
            state.CurrentPhase = MapOnlinePhase(snapshot.Phase);
            // 闂備礁鎼悧鍡欑矓鐎涙ɑ鍙忛柣鏃囨閸楁碍銇勯弽銊х畱缂侇喗鎹囬弻锟犲醇濠靛洦鍎撻悗瑙勬礃椤ㄥ牓骞忕€ｎ噮鏁婇柣鎾冲闁款參姊洪崫鍕妞わ箓娼ц灒濞达絿纭堕弸鏃傜棯閺夋妲虹紒鈧径瀣閻忕偟顭堟晶鍙夌節閳ь剚绗熼埀顒€顕ｉ悽鍓叉晢闁逞屽墮閿曘垻鈧潧鎽滈惌澶愭煠婵劕鈧洖鈻撻悩缁樼厽妞ゎ偒鍓欐俊鎸庣箾?Start/Main/End 闂佽瀛╃粙鎺楀礉閺嶎収鏁嗛柣鎰儗濞堜粙鏌曢崼婵嗘殜闁?
            // 闂佽楠哥粻宥夊垂濞差亜鏄ユ繛鎴炴皑閸楁碍銇勯弽銊х煂闁糕晝濮撮埥澶愬箻椤栨矮澹曢梻浣告惈鐎氼喗鎱ㄩ幘顔藉剭闁绘顕х粈?Main闂備焦瀵х粙鎴︽儔婵傜鐏虫俊顖濆吹閳瑰秵绻濋棃娑欐悙闁绘繃鐩弻鐔兼偪椤栨侗妫勯梺鎼炲€ら崣鍐箖瑜忔禒锕傛倻閳轰椒澹曟繛杈剧悼閹虫捁鐏囬梻浣哄帶閻ゅ洦鎱ㄩ妶鍥ｅ亾閸偆鐭掗柡宀嬬畵椤㈡瑩宕楅悡搴紖缂傚倸鍊烽悞锕傚箰鐠囧樊鐒芥い鎰堕檮閳锋捇鏌ら崨濠庡晱闁哥偘绮欓弻鐔虹矙濞嗙偓鈻堥梺鐟版啞婵炲﹤鐣烽敐澶樻晬婵炲棙鍔曢弨顓熺箾鐎涙鐭嬮悽顖氭喘閸┾偓?
            state.CurrentPhaseStep = PhaseStep.Main;
            state.IsPlayerTurn = snapshot.ActiveSeatIndex == snapshot.LocalSeatIndex;
            state.TurnNumber = snapshot.TurnNumber;
            if (snapshot.TurnNumber <= 1)
                state.PlayerGoesFirst = state.IsPlayerTurn;
            state.PendingAttackSkillName = snapshot.PendingAttackSkillName ?? string.Empty;
            state.PendingDefenseSkillName = snapshot.PendingDefenseSkillName ?? string.Empty;

            FillLocalSide(state.Player, snapshot.Self, snapshot.SelfHand, true);
            FillLocalSide(state.Opponent, snapshot.Opponent, null, false);

            var activeSide = state.IsPlayerTurn ? state.Player : state.Opponent;
            activeSide.PlayedThisPhase.Clear();
            for (int i = 0; i < snapshot.PlayedCards.Count; i++)
            {
                activeSide.PlayedThisPhase.Add(new PokerCard
                {
                    Suit = snapshot.PlayedCards[i].Suit ?? string.Empty,
                    Rank = snapshot.PlayedCards[i].Rank,
                });
            }

            return state;
        }

        private static void FillLocalSide(SideState target, OnlineBattleSideSnapshot side, System.Collections.Generic.List<OnlineBattleCardDto> explicitHand, bool revealHand)
        {
            target.Deck.Clear();
            target.Hand.Clear();
            target.DiscardPile.Clear();
            target.PlayedThisPhase.Clear();
            target.GeneralCardIds.Clear();
            target.GeneralFaceUp.Clear();
            target.FaceDownRecoverAfterOwnTurnEnds.Clear();
            target.Morale = side.Morale;
            target.MoraleCap = side.MoraleCap > 0 ? side.MoraleCap : BattleState.DefaultMoraleCap;
            target.CurrentHp = side.CurrentHp;
            target.MaxHp = side.MaxHp;
            target.MoraleUsedThisTurn = new bool[3];
            for (int i = 0; i < side.MoraleUsedThisTurn.Count && i < 3; i++)
                target.MoraleUsedThisTurn[i] = side.MoraleUsedThisTurn[i];

            for (int i = 0; i < side.DeckCount; i++)
                target.Deck.Add(new PokerCard { Suit = string.Empty, Rank = 0 });

            if (revealHand && explicitHand != null)
            {
                for (int i = 0; i < explicitHand.Count; i++)
                    target.Hand.Add(new PokerCard { Suit = explicitHand[i].Suit ?? string.Empty, Rank = explicitHand[i].Rank });
            }
            else
            {
                for (int i = 0; i < side.HandCount; i++)
                    target.Hand.Add(new PokerCard { Suit = string.Empty, Rank = 0 });
            }

            if (side.DiscardCards != null)
            {
                for (int i = 0; i < side.DiscardCards.Count; i++)
                    target.DiscardPile.Add(new PokerCard { Suit = side.DiscardCards[i].Suit ?? string.Empty, Rank = side.DiscardCards[i].Rank });
            }

            if (side.GeneralCardIds != null)
                target.GeneralCardIds.AddRange(side.GeneralCardIds);
            if (side.GeneralFaceUp != null)
            {
                target.GeneralFaceUp.AddRange(side.GeneralFaceUp);
                for (int i = 0; i < side.GeneralFaceUp.Count; i++)
                    target.FaceDownRecoverAfterOwnTurnEnds.Add(side.GeneralFaceUp[i] ? 0 : 1);
            }
        }

        private static BattlePhase MapOnlinePhase(OnlineDuelPhaseName phase)
        {
            switch (phase)
            {
                case OnlineDuelPhaseName.Preparation: return BattlePhase.Preparation;
                case OnlineDuelPhaseName.Income: return BattlePhase.Income;
                case OnlineDuelPhaseName.Primary: return BattlePhase.Primary;
                case OnlineDuelPhaseName.Main: return BattlePhase.Main;
                case OnlineDuelPhaseName.Defense: return BattlePhase.Defense;
                case OnlineDuelPhaseName.Resolve: return BattlePhase.Resolve;
                case OnlineDuelPhaseName.Discard: return BattlePhase.Discard;
                case OnlineDuelPhaseName.TurnEnd: return BattlePhase.TurnEnd;
                default: return BattlePhase.Preparation;
            }
        }

        private static string DescribeOnlineSnapshot(OnlineBattleSnapshotResponse snapshot)
        {
            if (snapshot == null)
                return "snapshot=null";

            int selfHand = snapshot.SelfHand != null ? snapshot.SelfHand.Count : 0;
            int played = snapshot.PlayedCards != null ? snapshot.PlayedCards.Count : 0;
            string selfHp = snapshot.Self != null ? snapshot.Self.CurrentHp.ToString() : "?";
            string opponentHp = snapshot.Opponent != null ? snapshot.Opponent.CurrentHp.ToString() : "?";
            return "roomId=" + snapshot.RoomId
                + ", phase=" + snapshot.Phase
                + ", turn=" + snapshot.TurnNumber
                + ", localSeat=" + snapshot.LocalSeatIndex
                + ", activeSeat=" + snapshot.ActiveSeatIndex
                + ", selfHp=" + selfHp
                + ", opponentHp=" + opponentHp
                + ", selfHand=" + selfHand
                + ", played=" + played;
        }

        private static bool IsDeckComplete(DeckData deck)
        {
            return deck != null && deck.CardIds != null && deck.CardIds.Count == 3;
        }

        private static DeckData ChooseOpponentDeck(DeckData selectedDeck)
        {
            var decks = CompendiumUI.GetDecks();
            if (decks != null)
            {
                for (int i = 0; i < decks.Count; i++)
                {
                    var deck = decks[i];
                    if (deck == null || ReferenceEquals(deck, selectedDeck) || !IsDeckComplete(deck))
                        continue;
                    if (deck.Id == selectedDeck?.Id)
                        continue;
                    return deck;
                }
            }
            return selectedDeck;
        }

        private static void OnAttackSelectionRequested(bool isPlayerAttacker)
        {
            if (_state == null || !isPlayerAttacker)
                return;

            OpenPlayerAttackSkillFirstMenu();
        }

        /// <summary>
        /// 第一层：展示通用攻击与各翻面武将的攻击技；选通用攻击或【策马斩将】后进入第二层（牌型与效果列表）。
        /// </summary>
        private static void OpenPlayerAttackSkillFirstMenu()
        {
            if (_state == null || !_state.IsPlayerTurn)
                return;
            if (_state.ActiveSide.PlayedThisPhase.Count <= 0)
                return;

            CloseAttackPatternPopup();
            _state.PendingAttackPatternVariant = -1;

            var labels = new List<string>();
            var attackChoices = new List<(int generalIndex, int skillIndex, string skillName)>();
            labels.Add("\u901a\u7528\u653b\u51fb");
            attackChoices.Add((-1, -1, "\u901a\u7528\u653b\u51fb"));

            var side = _state.Player;
            for (int generalIndex = 0; generalIndex < side.GeneralCardIds.Count; generalIndex++)
            {
                if (!side.IsGeneralFaceUp(generalIndex))
                    continue;

                var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[generalIndex]));
                if (data == null)
                    continue;

                for (int skillIndex = 0; skillIndex < 3; skillIndex++)
                {
                    if (!SkillHasTag(data, skillIndex, "攻击技"))
                        continue;
                    string skillName = GetSkillName(data, skillIndex);
                    labels.Add(GetGeneralDisplayName(true, generalIndex) + " - " + skillName);
                    attackChoices.Add((generalIndex, skillIndex, skillName));
                }
            }

            OpenChoicePopup("\u8bf7\u9009\u62e9\u53ef\u7528\u7684\u653b\u51fb\u6280", labels.ToArray(), choiceIndex =>
            {
                if (choiceIndex < 0 || choiceIndex >= attackChoices.Count)
                    return;
                var choice = attackChoices[choiceIndex];
                CommitAttackSkillAfterOptionalPatternPopup(choice.generalIndex, choice.skillIndex, choice.skillName);
            }, "\u53d6\u6d88");
        }

        private static void OnGeneralSkillClicked(bool isPlayerSide, int generalIndex, int skillIndex)
        {
            if (_state == null)
                return;
            if (TryConsumeGameStartPassiveNodeClick(isPlayerSide, generalIndex, skillIndex))
                return;
            if (!isPlayerSide)
            {
                var oside = _state.Opponent;
                if (generalIndex < 0 || generalIndex >= oside.GeneralCardIds.Count)
                    return;
                if (!oside.IsGeneralFaceUp(generalIndex))
                {
                    ToastUI.Show("\u5df2\u7ffb\u9762\u89d2\u8272\u4e0d\u53ef\u67e5\u770b\u6280\u80fd\u8bf4\u660e");
                    return;
                }

                var odata = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(oside.GeneralCardIds[generalIndex]));
                if (odata == null)
                    return;
                string oNm = GetSkillName(odata, skillIndex);
                if (string.IsNullOrWhiteSpace(oNm))
                    return;
                string oDesc = skillIndex switch
                {
                    0 => odata.SkillDesc1 ?? string.Empty,
                    1 => odata.SkillDesc2 ?? string.Empty,
                    2 => odata.SkillDesc3 ?? string.Empty,
                    _ => string.Empty
                };
                string line = "\u3010" + oNm + "\u3011\n" + oDesc.Trim();
                ToastUI.Show(string.IsNullOrWhiteSpace(oDesc) ? oNm : line, 5.5f);
                return;
            }

            var side = _state.Player;
            if (generalIndex < 0 || generalIndex >= side.GeneralCardIds.Count)
                return;

            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[generalIndex]));
            if (data == null)
                return;

            if (!side.IsGeneralFaceUp(generalIndex) && !SkillHasTag(data, skillIndex, "\u6301\u7eed\u6280"))
            {
                ToastUI.Show("\u5df2\u7ffb\u9762\u89d2\u8272\u4e0d\u53ef\u4f7f\u7528\u6216\u89e6\u53d1\u975e\u6301\u7eed\u6280");
                return;
            }

            string skillName = GetSkillName(data, skillIndex);
            if (_isOnlineMode)
            {
                if (_state.CurrentPhase == BattlePhase.Main && _state.IsPlayerTurn && _state.ActiveSide.PlayedThisPhase.Count > 0 && SkillHasTag(data, skillIndex, "攻击技"))
                {
                    OpenPlayerAttackSkillFirstMenu();
                    return;
                }
                if (_state.CurrentPhase == BattlePhase.Defense && !_state.IsPlayerTurn && SkillHasTag(data, skillIndex, "防御技"))
                {
                    _ = OnlineClientService.SelectDefenseSkillAsync(generalIndex, skillIndex);
                    return;
                }
                if (_state.CurrentPhase == BattlePhase.Primary && _state.IsPlayerTurn && (SkillHasTag(data, skillIndex, "主动技") || SkillHasTag(data, skillIndex, "破军技")))
                {
                    _ = OnlineClientService.ActivatePrimarySkillAsync(generalIndex, skillIndex);
                }
                return;
            }

            bool phaseOkPrimary = _state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main && _state.IsPlayerTurn;
            bool phaseOkPlayMain = _state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main && _state.IsPlayerTurn;
            bool isPrimaryOrPojunSkill = SkillHasTag(data, skillIndex, "\u4e3b\u52a8\u6280") || SkillHasTag(data, skillIndex, "\u7834\u519b\u6280");
            bool jushui = IsJushuiDuanqiaoSkill(data, skillIndex);
            bool canUsePrimaryWindowSkill = (phaseOkPrimary && isPrimaryOrPojunSkill) || (phaseOkPlayMain && jushui);
            if (phaseOkPrimary || phaseOkPlayMain)
            {
                if (!canUsePrimaryWindowSkill)
                {
                    if (phaseOkPrimary)
                        return;
                }
                else
                {
                    string skillKey = "True_" + generalIndex + "_" + skillIndex;
                    if (SkillHasTag(data, skillIndex, "\u7834\u519b\u6280"))
                    {
                        if (side.UsedOneShotSkills.Contains(skillKey))
                        {
                            ToastUI.Show("\u8be5\u7834\u519b\u6280\u672c\u5c40\u5df2\u4f7f\u7528");
                            return;
                        }
                    }

                    if (jushui)
                    {
                        OpenJushuiDuanqiaoPopup(generalIndex, skillIndex);
                        return;
                    }

                    if (!OfflineSkillEngine.TryActivatePrimarySkill(_state, true, generalIndex, skillIndex, out string primMsg))
                    {
                        ToastUI.Show(string.IsNullOrEmpty(primMsg) ? "\u65e0\u6cd5\u53d1\u52a8\u8be5\u6280\u80fd" : primMsg);
                        return;
                    }

                    if (SkillHasTag(data, skillIndex, "\u7834\u519b\u6280"))
                        side.UsedOneShotSkills.Add(skillKey);
                    RefreshGeneralSkillStates();
                    return;
                }
            }

            if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main && _state.IsPlayerTurn && _state.ActiveSide.PlayedThisPhase.Count > 0)
            {
                if (!SkillHasTag(data, skillIndex, "攻击技"))
                    return;
                OpenPlayerAttackSkillFirstMenu();
                return;
            }

            if (_state.CurrentPhase == BattlePhase.Defense && _state.CurrentPhaseStep == PhaseStep.Main && !_state.IsPlayerTurn)
            {
                if (!SkillHasTag(data, skillIndex, "防御技"))
                    return;
                BattlePhaseManager.NotifyDefenseSkillSelected(true, generalIndex, skillIndex, skillName);
            }
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
            RefreshGenericAttackButton();
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
                _phaseLabel.text = "--";
                return;
            }
            string phaseName = GetPhaseDisplayName(_state.CurrentPhase);
            bool isLocalActive = _state.IsPlayerTurn;
            string activePlayerName = isLocalActive ? NormalizeDisplayName(_localPlayerDisplayName, "你") : NormalizeDisplayName(_opponentPlayerDisplayName, "对手");
            string activeSide = isLocalActive ? "你方" : "对方";
            _phaseLabel.text = "当前行动方：" + activePlayerName + "（" + activeSide + "）  阶段：" + phaseName;
        }

        private static string GetPhaseDisplayName(BattlePhase phase) => BattlePhaseDisplay.ToChinese(phase);

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
                _discardButtonLabel.text = "弃牌堆(" + _state.Player.DiscardPile.Count + ")";
            if (_opponentDiscardButtonLabel != null)
                _opponentDiscardButtonLabel.text = "弃牌堆(" + _state.Opponent.DiscardPile.Count + ")";
        }

        private static void RefreshGeneralCards()
        {
            if (_state == null) return;
            for (int i = 0; i < _playerCardRows.Count; i++)
            {
                var holder = _playerCardRows[i].GetComponent<GeneralCardHolder>();
                if (holder != null)
                {
                    holder.SetCardId(i < _state.Player.GeneralCardIds.Count ? _state.Player.GeneralCardIds[i] : "");
                    holder.SetFaceDown(! _state.Player.IsGeneralFaceUp(i));
                }
            }
            for (int i = 0; i < _opponentCardRows.Count; i++)
            {
                var holder = _opponentCardRows[i].GetComponent<GeneralCardHolder>();
                if (holder != null)
                {
                    holder.SetCardId(i < _state.Opponent.GeneralCardIds.Count ? _state.Opponent.GeneralCardIds[i] : "");
                    holder.SetFaceDown(! _state.Opponent.IsGeneralFaceUp(i));
                }
            }

            RefreshGeneralSkillStates();
        }

        private static List<int> GetFaceUpGeneralIndices(bool isPlayer)
        {
            var result = new List<int>();
            if (_state == null)
                return result;

            var side = isPlayer ? _state.Player : _state.Opponent;
            for (int i = 0; i < side.GeneralCardIds.Count; i++)
            {
                if (side.IsGeneralFaceUp(i))
                    result.Add(i);
            }
            return result;
        }

        /// <summary>士气第三项：所有有角色的槽位均可选（未翻面则翻面，已翻面则翻回）。</summary>
        private static List<int> BuildAllPlayerGeneralIndicesForMoraleFlip()
        {
            var result = new List<int>();
            if (_state == null)
                return result;
            for (int i = 0; i < _state.Player.GeneralCardIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(_state.Player.GeneralCardIds[i]))
                    result.Add(i);
            }
            return result;
        }

        private static string GetGeneralDisplayName(bool isPlayer, int generalIndex)
        {
            if (_state == null)
                return "武将" + (generalIndex + 1);

            var side = isPlayer ? _state.Player : _state.Opponent;
            if (generalIndex < 0 || generalIndex >= side.GeneralCardIds.Count)
                return "武将" + (generalIndex + 1);

            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[generalIndex]));
            return data?.RoleName ?? ("武将" + (generalIndex + 1));
        }

        public static string GetGeneralDisplayNameForBattleLog(bool isPlayer, int generalIndex) =>
            GetGeneralDisplayName(isPlayer, generalIndex);

        private static string GetSkillName(CardData data, int skillIndex)
        {
            return skillIndex switch
            {
                0 => data?.SkillName1 ?? string.Empty,
                1 => data?.SkillName2 ?? string.Empty,
                2 => data?.SkillName3 ?? string.Empty,
                _ => string.Empty
            };
        }

        private static bool SkillHasTag(CardData data, int skillIndex, string tag)
        {
            if (data == null || string.IsNullOrWhiteSpace(tag))
                return false;

            if (!string.IsNullOrWhiteSpace(data.CardId) && SkillRuleLoader.HasTag(data.CardId, skillIndex, tag))
                return true;

            List<string> tags = skillIndex switch
            {
                0 => data.SkillTags1,
                1 => data.SkillTags2,
                2 => data.SkillTags3,
                _ => null
            };

            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void RefreshGeneralSkillStates()
        {
            if (_state == null)
                return;

            RefreshGeneralSkillStatesForSide(_playerCardRows, true);
            RefreshGeneralSkillStatesForSide(_opponentCardRows, false);
        }

        private static void RefreshGeneralSkillStatesForSide(List<GameObject> rows, bool isPlayerSide)
        {
            for (int cardIndex = 0; cardIndex < rows.Count; cardIndex++)
            {
                var holder = rows[cardIndex] != null ? rows[cardIndex].GetComponent<GeneralCardHolder>() : null;
                if (holder == null)
                    continue;

                var side = isPlayerSide ? _state.Player : _state.Opponent;
                var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(holder.CurrentCardId));
                bool isFaceUp = side.IsGeneralFaceUp(cardIndex);

                for (int skillIndex = 0; skillIndex < 3; skillIndex++)
                {
                    var passiveHi = PassiveNodeSkillHighlightKind.None;
                    bool allowSkillWhileFlipped = isFaceUp || (data != null && SkillHasTag(data, skillIndex, "\u6301\u7eed\u6280"));

                    if (IsGameStartMandatoryPendingFor(isPlayerSide, cardIndex, skillIndex) && allowSkillWhileFlipped)
                    {
                        holder.SetSkillButtonState(skillIndex, true, PassiveNodeSkillHighlightKind.Mandatory);
                        continue;
                    }

                    if (IsGameStartOptionalPendingFor(isPlayerSide, cardIndex, skillIndex) && allowSkillWhileFlipped)
                    {
                        holder.SetSkillButtonState(skillIndex, true, PassiveNodeSkillHighlightKind.Optional);
                        continue;
                    }

                    bool enabled = false;
                    if (data != null && isPlayerSide && allowSkillWhileFlipped)
                    {
                        if (_state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main && _state.IsPlayerTurn)
                            enabled = SkillHasTag(data, skillIndex, "\u4e3b\u52a8\u6280") || SkillHasTag(data, skillIndex, "\u7834\u519b\u6280");

                        if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main && _state.IsPlayerTurn)
                        {
                            if (IsJushuiDuanqiaoSkill(data, skillIndex))
                                enabled = true;
                            else if (_state.ActiveSide.PlayedThisPhase.Count > 0)
                                enabled = enabled || SkillHasTag(data, skillIndex, "\u653b\u51fb\u6280");
                        }

                        if (_state.CurrentPhase == BattlePhase.Defense && _state.CurrentPhaseStep == PhaseStep.Main && !_state.IsPlayerTurn)
                            enabled = enabled || SkillHasTag(data, skillIndex, "\u9632\u5fa1\u6280");
                    }

                    string skillKey = (isPlayerSide ? "True" : "False") + "_" + cardIndex + "_" + skillIndex;
                    bool pjUsed = data != null && SkillHasTag(data, skillIndex, "\u7834\u519b\u6280") && side.UsedOneShotSkills.Contains(skillKey);
                    if (pjUsed)
                        enabled = false;

                    if (!pjUsed && !isPlayerSide && data != null && isFaceUp)
                    {
                        string nmView = GetSkillName(data, skillIndex) ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(nmView))
                        {
                            enabled = true;
                            passiveHi = PassiveNodeSkillHighlightKind.OpponentInfo;
                        }
                    }

                    if (holder.SkillButtonLabels != null && skillIndex < holder.SkillButtonLabels.Count && holder.SkillButtonLabels[skillIndex] != null)
                    {
                        string nm = GetSkillName(data, skillIndex) ?? string.Empty;
                        holder.SkillButtonLabels[skillIndex].text = pjUsed
                            ? nm + "\n<size=10><color=#999999>\u5df2\u4f7f\u7528</color></size>"
                            : nm;
                    }

                    holder.SetSkillButtonState(skillIndex, enabled, passiveHi);
                }
            }
        }

        /// <summary> UI Image 闂?sprite 闂備礁鎼崯顖氾耿閸︻厾绠斿璺虹灱绾惧吋淇婇姘儓闁绘挻鍨块弻銊モ槈濡偐鍔┑鐐存尭閻栧ジ骞冩禒瀣╃憸搴敂閵堝鐓曢柕澶涚畱閸斻倗鈧娲滄晶妤呭箯閸涱収鍚嬮柛鈩冾殢閺嬧偓闂備胶鍘ч悿鍥ㄦ叏閵堝憘鐔稿緞瀹€鈧惌鍡涙煣韫囨洘鍤€闁搞倝浜堕弻銈夋偡閹殿喗鍕鹃梺?</summary>
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

        /// <summary> 闂備焦鐪归崹濠氬窗閹版澘鍨傛慨姗嗗墻濞间即鏌ㄥ┑鍡樺櫣鐟滄澘顑夊鍫曞醇閵忊€虫畬闂?Sprite闂備焦瀵х粙鎴︽偋閸℃稑绠栭柟鎯ь嚟閻濊埖銇勯幇鍓佹偧婵絾绮撻弻娑㈠Ψ瑜嶆禍楣冩煟閺嶇數绋荤€垫澘瀚ˇ鏌ユ煟椤垵澧存鐐叉喘閹倝宕掑鍜佹Т闂備焦瀵х粙鎴︽儔婵傜鏋侀柕鍫濇椤╂煡骞栫划瑙勵潐缂佽鲸绮嶉幈銊ノ熺紒妯锋嫻闁撅箑閰ｉ弻?</summary>
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

        private const float HandContentWidthCompact = 404f;
        private const float HandCardWidthCompact = 72f;
        private const float HandBaseSpacing = -8f;
        private const float HandMinVisibleWidth = 24f;
        private const float HandCardStartX = 0f;
        private const float HandCardWidthExpanded = 112f;

        private static float HandCardWidthNow => _playerHandExpanded ? HandCardWidthExpanded : HandCardWidthCompact;
        private static float HandCardHeightNow => HandCardWidthNow * CardAspectH / CardAspectW;
        private static float HandCardHeightCompact => HandCardWidthCompact * CardAspectH / CardAspectW;

        private static float GetPlayerHandLayoutContentWidth()
        {
            if (!_playerHandExpanded)
                return HandContentWidthCompact;
            return Mathf.Max(HandContentWidthCompact, ExpandedHandOuterW - 36f);
        }

        private static int _cachedPlayerHandCount = -1;
        private static int _cachedOpponentHandCount = -1;

        private static void RefreshHandCards()
        {
            if (_playerHandContent == null || _state == null) return;
            ClearHandHoverOverlayClones();
            int playerCount = _state.Player.Hand.Count;
            int oppCount = _opponentHandContent != null ? _state.Opponent.Hand.Count : 0;
            if (_playerHandLabel != null)
                _playerHandLabel.text = "手牌上限：" + _state.HandLimit + "/手牌数量：" + playerCount;
            if (_opponentHandLabel != null)
                _opponentHandLabel.text = "手牌上限：" + _state.HandLimit + "/手牌数量：" + oppCount;
            _cachedPlayerHandCount = playerCount;
            _cachedOpponentHandCount = oppCount;
            ClearRuntimeChildren(_playerHandContent);
            for (int i = 0; i < playerCount; i++)
            {
                int index = i;
                var pc = _state.Player.Hand[i];
                CreateHandCardItem(_playerHandContent, pc, HandCardWidthNow, HandCardHeightNow, index);
            }
            ApplyHandLayout(_playerHandContent, playerCount, true);
            if (_opponentHandContent != null)
            {
                ClearRuntimeChildren(_opponentHandContent);
                for (int i = 0; i < oppCount; i++)
                {
                    var pc = _state.Opponent.Hand[i];
                    CreateHandCardItem(_opponentHandContent, pc, HandCardWidthCompact, HandCardHeightCompact, i, false);
                }
                ApplyHandLayout(_opponentHandContent, oppCount, false);
            }
        }

        private static void ClearRuntimeChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static void ClearHandHoverOverlayClones()
        {
            if (_handHoverOverlay == null)
                return;
            for (int i = _handHoverOverlay.childCount - 1; i >= 0; i--)
            {
                Transform c = _handHoverOverlay.GetChild(i);
                if (c == null)
                    continue;
                c.SetParent(null, false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        private static void ApplyHandLayout(Transform handContent, int cardCount, bool isPlayerHand)
        {
            if (handContent == null)
                return;

            var rect = handContent as RectTransform;
            if (rect != null)
            {
                float layoutCap = isPlayerHand ? GetPlayerHandLayoutContentWidth() : HandContentWidthCompact;
                float cardW = isPlayerHand ? HandCardWidthNow : HandCardWidthCompact;
                float cardH = isPlayerHand ? HandCardHeightNow : HandCardHeightCompact;
                float step = GetHandCardStep(cardCount, layoutCap, cardW);
                float width = GetHandContentLayoutWidth(cardCount, step, cardW);
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                rect.anchoredPosition = Vector2.zero;

                for (int i = 0; i < handContent.childCount; i++)
                {
                    var childRect = handContent.GetChild(i) as RectTransform;
                    if (childRect == null)
                        continue;

                    childRect.anchorMin = new Vector2(0f, 0.5f);
                    childRect.anchorMax = new Vector2(0f, 0.5f);
                    childRect.pivot = new Vector2(0f, 0.5f);
                    childRect.sizeDelta = new Vector2(cardW, cardH);
                    childRect.anchoredPosition = new Vector2(HandCardStartX + i * step, 0f);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Canvas.ForceUpdateCanvases();
                var dragScroller = rect.GetComponentInParent<RightMouseScrollRect>();
                if (dragScroller != null)
                    dragScroller.ClampToBounds(true);
            }
        }

        private static float GetHandCardStep(int cardCount, float layoutWidthCap, float cardWidth)
        {
            if (cardCount <= 1)
                return 0f;

            float baseStep = cardWidth + HandBaseSpacing;
            float maxStep = (layoutWidthCap - HandCardStartX - cardWidth) / (cardCount - 1);
            return Mathf.Max(HandMinVisibleWidth, Mathf.Min(baseStep, maxStep));
        }

        private static float GetHandContentLayoutWidth(int cardCount, float step, float cardWidth)
        {
            if (cardCount <= 0)
                return 0f;

            return HandCardStartX + cardWidth + (cardCount - 1) * step;
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
            var goImg = go.AddComponent<Image>();
            goImg.color = Color.clear;
            bool playerInteract = isPlayer && _playerHandExpanded;
            goImg.raycastTarget = playerInteract;
            var goRect = go.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0f, 0.5f);
            goRect.anchorMax = new Vector2(0f, 0.5f);
            goRect.pivot = new Vector2(0f, 0.5f);
            goRect.sizeDelta = new Vector2(w, h);
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
            var borderImg = border.AddComponent<Image>();
            borderRect.offsetMin = new Vector2(-2f, -2f);
            borderRect.offsetMax = new Vector2(2f, 2f);
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
                int labelSize = _playerHandExpanded ? 24 : 18;
                var label = CreateGameText(labelGo.transform, pc.DisplayName, labelSize);
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
            if (playerInteract)
            {
                var drag = go.AddComponent<HandCardDragDrop>();
                drag.HandIndex = handIndex;
            }

            return go;
        }

        private static void RefreshPlayedCards()
        {
            if (_playedZoneContent == null || _state == null) return;
            ClearRuntimeChildren(_playedZoneContent);

            var playedCards = _state.ActiveSide.PlayedThisPhase;
            bool canReturnToHand = _state.IsPlayerTurn && _state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main;
            float cardW = HandCardWidthCompact;
            float cardH = HandCardHeightCompact;
            for (int i = 0; i < playedCards.Count; i++)
            {
                int playedIndex = i;
                var pc = playedCards[i];
                var go = new GameObject("PlayedCard_" + playedIndex);
                go.transform.SetParent(_playedZoneContent, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;
                var rootImage = go.AddComponent<Image>();
                rootImage.color = Color.clear;
                rootImage.raycastTarget = canReturnToHand;
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
                fillImg.color = Color.white;
                fillImg.sprite = GetWhiteSprite();
                string roleForStrip = null;
                if (pc.PlayedAsGeneral)
                {
                    roleForStrip = !string.IsNullOrWhiteSpace(pc.PlayedRoleDisplayName)
                        ? pc.PlayedRoleDisplayName.Trim()
                        : GetGeneralDisplayName(_state.IsPlayerTurn, pc.GeneralSlotIndex);
                    if (string.IsNullOrEmpty(roleForStrip))
                        roleForStrip = null;
                }

                if (roleForStrip != null)
                {
                    var roleGo = new GameObject("RoleLabel");
                    roleGo.transform.SetParent(visual.transform, false);
                    var roleRt = roleGo.AddComponent<RectTransform>();
                    roleRt.anchorMin = new Vector2(0.03f, 0.4f);
                    roleRt.anchorMax = new Vector2(0.97f, 0.92f);
                    roleRt.offsetMin = Vector2.zero;
                    roleRt.offsetMax = Vector2.zero;
                    var roleLbl = CreateGameText(roleGo.transform, roleForStrip, 14, TextAlignmentOptions.Center);
                    roleLbl.color = new Color(0.12f, 0.12f, 0.18f, 1f);
                    roleLbl.enableWordWrapping = false;
                    roleLbl.overflowMode = TextOverflowModes.Ellipsis;
                    roleLbl.enableAutoSizing = true;
                    roleLbl.fontSizeMin = 8f;
                    roleLbl.fontSizeMax = 15f;
                    roleLbl.alignment = TextAlignmentOptions.Center;
                    SetFullRect(roleLbl.GetComponent<RectTransform>());

                    var rankGo = new GameObject("RankLabel");
                    rankGo.transform.SetParent(visual.transform, false);
                    var rankRt = rankGo.AddComponent<RectTransform>();
                    rankRt.anchorMin = new Vector2(0.05f, 0.06f);
                    rankRt.anchorMax = new Vector2(0.95f, 0.36f);
                    rankRt.offsetMin = Vector2.zero;
                    rankRt.offsetMax = Vector2.zero;
                    var rankLbl = CreateGameText(rankGo.transform, pc.DisplayName, 16, TextAlignmentOptions.Center);
                    rankLbl.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                    rankLbl.enableWordWrapping = false;
                    rankLbl.overflowMode = TextOverflowModes.Overflow;
                    SetFullRect(rankLbl.GetComponent<RectTransform>());
                }
                else
                {
                    var labelGo = new GameObject("Label");
                    labelGo.transform.SetParent(visual.transform, false);
                    var lbl = CreateGameText(labelGo.transform, pc.DisplayName, 18, TextAlignmentOptions.Center);
                    lbl.color = new Color(0.15f, 0.15f, 0.2f, 1f);
                    lbl.enableWordWrapping = false;
                    lbl.alignment = TextAlignmentOptions.Center;
                    SetFullRect(lbl.GetComponent<RectTransform>());
                }
                go.AddComponent<HandCardHover>();
                if (canReturnToHand)
                {
                    var btn = go.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.onClick.AddListener(() => ReturnPlayedCardToHand(playedIndex));
                }
            }
        }

        private static void ReturnPlayedCardToHand(int playedIndex)
        {
            if (_state == null || !_state.IsPlayerTurn) return;
            if (_isOnlineMode)
            {
                _ = OnlineClientService.TakeBackPlayedCardAsync(playedIndex);
                return;
            }
            if (playedIndex < 0 || playedIndex >= _state.Player.PlayedThisPhase.Count) return;
            var card = _state.Player.PlayedThisPhase[playedIndex];
            _state.Player.PlayedThisPhase.RemoveAt(playedIndex);
            if (!card.PlayedAsGeneral)
                _state.Player.Hand.Add(card);
            RefreshAllFromState();
        }

        /// <summary> 闂備礁鎲￠崹鐓庘枍閿濆棙娅犳繝闈涱儐閳锋捇鏌ら崨濠庡晱闁哥偠妫勯埥澶愬箼閸愩劌浼庣紓浣介哺缁诲嫭绂嶉幖浣稿耿婵☆垯璀﹂弶褰掓⒑缂佹﹩娈斿┑鍌涙⒒閸掓帡鎮╃拠鑼吋闁诲函缍嗛崜娆撀烽崒鐐寸厱闁哄稁鍓氶鐘崇濞戙垺鐓曢柟鐑樻尰缁舵煡鏌涢幋顖滅瘈妤犵偛绉归獮姗€宕橀崣澶屾闂備礁鎲￠悧妤佺娴犲鐒垫い鎺嗗亾闁稿﹤缍婇幆渚€鍨鹃弬銉︾亖闂傚倸鐗婄粙鎴澬掓径鎰厽婵°倐鍋撻柣顓炲€歌灒濞达絿纭堕弸鏃堟煕椤垵浜濆ù鐘趁…鍧楁嚋閻㈡鏆℃繝鈷€宥囩暤鐎?MaxPlayPerPhase 闁诲孩顔栭崰姘叏娴兼潙鐒?</summary>
        public static void MoveHandCardToPlayedZone(int handIndex)
        {
            if (_state == null || !_state.IsPlayerTurn || _state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main) return;
            if (_isOnlineMode)
            {
                _ = OnlineClientService.PlayCardAsync(handIndex);
                return;
            }
            if (_state.Player.CountNonGeneralCardsInPlayedZone() >= BattleState.MaxPlayPerPhase) return;
            if (handIndex < 0 || handIndex >= _state.Player.Hand.Count) return;
            var card = _state.Player.Hand[handIndex];
            _state.Player.Hand.RemoveAt(handIndex);
            _state.Player.PlayedThisPhase.Add(card);
            RefreshAllFromState();
        }

        /// <summary>出牌阶段将未翻面己方角色按表格花色点数当牌打入打出区；结算攻击后该武将翻面。</summary>
        public static bool TryMovePlayerGeneralToPlayedZone(int generalIndex)
        {
            if (_state == null || _isOnlineMode)
                return false;
            if (!_state.IsPlayerTurn || _state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main)
            {
                ToastUI.Show("\u4ec5\u51fa\u724c\u9636\u6bb5\u53ef\u5c06\u89d2\u8272\u6253\u5165\u6253\u51fa\u533a");
                return false;
            }

            if (generalIndex < 0 || generalIndex >= _state.Player.GeneralCardIds.Count)
                return false;
            if (!_state.Player.IsGeneralFaceUp(generalIndex))
            {
                ToastUI.Show("\u5df2\u7ffb\u9762\u7684\u89d2\u8272\u4e0d\u53ef\u5f53\u4f5c\u724c\u6253\u51fa");
                return false;
            }

            string cid = _state.Player.GeneralCardIds[generalIndex] ?? string.Empty;
            if (!CardTableLoader.TryGetGeneralAsPokerCard(cid, out PokerCard pc))
            {
                ToastUI.Show("\u89d2\u8272\u5361\u8868\u683c\u7f3a\u5c11\u6709\u6548\u82b1\u8272/\u70b9\u6570\uff08D/E\u5217\uff09");
                return false;
            }

            for (int pi = 0; pi < _state.Player.PlayedThisPhase.Count; pi++)
            {
                var existing = _state.Player.PlayedThisPhase[pi];
                if (existing.PlayedAsGeneral && existing.GeneralSlotIndex == generalIndex)
                {
                    ToastUI.Show("\u8be5\u89d2\u8272\u5df2\u5728\u6253\u51fa\u533a");
                    return false;
                }
            }

            var roleData = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
            string roleName = roleData != null ? (roleData.RoleName ?? string.Empty).Trim() : string.Empty;
            pc.PlayedAsGeneral = true;
            pc.GeneralSlotIndex = generalIndex;
            pc.PlayedRoleDisplayName = roleName;

            _state.Player.PlayedThisPhase.Add(pc);
            RefreshAllFromState();
            return true;
        }

        public static void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }
    }

    public class GeneralCardHolder : MonoBehaviour
    {
        public Transform CardSlot;
        public int CardIndex;
        public bool IsPlayer;
        public TextMeshProUGUI RoleHeaderLabel;
        public List<Button> SkillButtons;
        public List<TextMeshProUGUI> SkillButtonLabels;
        public GameObject FaceDownOverlay;
        public static System.Action<bool, int, int> OnSkillButtonClicked;
        public string CurrentCardId => _cardId;
        private string _cardId;

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

            var rect = transform as RectTransform;
            if (rect != null)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        public void SetSkillButtonState(int index, bool enabled, PassiveNodeSkillHighlightKind passiveHighlight = PassiveNodeSkillHighlightKind.None)
        {
            if (SkillButtons == null || index < 0 || index >= SkillButtons.Count) return;
            var btn = SkillButtons[index];
            if (btn == null) return;
            btn.interactable = enabled;
            var img = btn.targetGraphic as Image;
            if (img != null)
            {
                if (enabled && passiveHighlight == PassiveNodeSkillHighlightKind.Mandatory)
                    img.color = new Color(0.12f, 0.62f, 0.32f, 1f);
                else if (enabled && passiveHighlight == PassiveNodeSkillHighlightKind.Optional)
                    img.color = new Color(0.25f, 0.5f, 0.9f, 1f);
                else if (enabled && passiveHighlight == PassiveNodeSkillHighlightKind.OpponentInfo)
                    img.color = new Color(0.34f, 0.44f, 0.54f, 1f);
                else if (enabled)
                    img.color = new Color(0.25f, 0.5f, 0.9f, 1f);
                else
                    img.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }
        }

        public void SetFaceDown(bool isFaceDown)
        {
            if (FaceDownOverlay != null)
                FaceDownOverlay.SetActive(isFaceDown);
        }

        public void OnSkillButtonClick(int skillIndex)
        {
            OnSkillButtonClicked?.Invoke(IsPlayer, CardIndex, skillIndex);
        }

        public void SetCardId(string cardId)
        {
            _cardId = NormalizeCardId(cardId ?? "");
            var data = !string.IsNullOrEmpty(_cardId) ? CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(_cardId)) : null;
            UpdateRoleHeader(data);
            if (CardSlot == null) return;
            for (int i = CardSlot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(CardSlot.GetChild(i).gameObject);
            SetSkillCount(0);
            int skillCount = 0;
            if (!string.IsNullOrEmpty(_cardId))
            {
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
                    var slotView = go.GetComponent<CardView>();
                    if (slotView != null && slotView.FaceImage != null)
                    {
                        slotView.FaceImage.color = Color.white;
                        slotView.FaceImage.preserveAspect = true;
                        slotView.LoadFaceSprite();
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

        private void UpdateRoleHeader(CardData data)
        {
            if (RoleHeaderLabel == null)
                return;

            string prefix = "武将" + (CardIndex + 1);
            string roleName = data != null ? (data.RoleName ?? string.Empty).Trim() : string.Empty;
            RoleHeaderLabel.text = string.IsNullOrEmpty(roleName) ? prefix : (prefix + "  " + roleName);
        }

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

    /// <summary>对手牌：收起时不接收射线；展开后悬停时在顶层克隆牌面便于辨认，拖入打出区由 <see cref="HandCardDragDrop"/> 处理。</summary>
    public class HandCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject _hoverClone;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsUnderPlayerHandContent() && !GameUI.IsPlayerHandExpanded())
                return;

            if (_hoverClone != null) return;
            var overlay = GameUI.GetHandHoverOverlay();
            if (overlay == null) return;
            Transform visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (visual == null) return;
            _hoverClone = UnityEngine.Object.Instantiate(visual.gameObject);
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
                if (IsUnderPlayerHandContent())
                    cloneRect.localScale = new Vector3(1.08f, 1.08f, 1f);
            }
            var cg = _hoverClone.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.ignoreParentGroups = true;
        }

        private bool IsUnderPlayerHandContent()
        {
            Transform t = transform;
            while (t != null)
            {
                if (t.name == "PlayerHandContent")
                    return true;
                t = t.parent;
            }

            return false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoverClone != null)
            {
                UnityEngine.Object.Destroy(_hoverClone);
                _hoverClone = null;
            }
        }

        private void OnDestroy()
        {
            if (_hoverClone != null) UnityEngine.Object.Destroy(_hoverClone);
        }
    }

    /// <summary>出牌阶段将己方未翻面角色牌拖入打出区，当作扑克牌打出。</summary>
    public class GeneralCardDragToPlayedZone : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GeneralCardHolder _holder;
        private GameObject _ghost;
        private RectTransform _rt;
        private Canvas _canvas;

        private void Awake()
        {
            _rt = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _holder = GetComponentInParent<GeneralCardHolder>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rt == null || _canvas == null || _holder == null)
                return;
            _ghost = new GameObject("GeneralCardGhost");
            _ghost.transform.SetParent(_canvas.transform, false);
            var ghostRt = _ghost.AddComponent<RectTransform>();
            ghostRt.sizeDelta = _rt.sizeDelta;
            var ghostImg = _ghost.AddComponent<Image>();
            ghostImg.color = Color.white;
            ghostImg.raycastTarget = false;
            var cg = _ghost.AddComponent<CanvasGroup>();
            cg.alpha = 0.85f;
            cg.blocksRaycasts = false;
            ghostRt.position = _rt.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null)
                (_ghost.transform as RectTransform).position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }

            if (_holder == null)
                return;

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = eventData.position }, results);
            bool overPlayed = false;
            foreach (var r in results)
            {
                if (r.gameObject.GetComponentInParent<PlayedZoneMarker>() != null)
                {
                    overPlayed = true;
                    break;
                }
            }

            if (!overPlayed && GameUI.IsScreenPointOverPlayedZone(eventData.position))
                overPlayed = true;
            if (overPlayed)
                GameUI.TryMovePlayerGeneralToPlayedZone(_holder.CardIndex);
        }

        private void OnDestroy()
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
        }
    }

    /// <summary> 闂備礁缍婂褔顢栭崶銊︽珷婵犻潧顑呯粻顖炴煛瀹擃喖鍟伴崢鎺楁⒑閻熸壆浠涢柟鍛婃倐瀹曠敻顢橀姀鐘殿槺闂佺粯鍨惰摫妞は佸懐纾煎璺哄瘨閸ゆ瑩鏌?PlayedThisPhase闂備線娼уΛ妤呭磹妞嬪骸鍨濋幖娣妼缁€鍕煣韫囨洘鍤€妞ゃ倕鍊垮濠氬礃椤撶偟鍘銈嗘煥閻倿骞冩禒瀣亜闁告稑锕ラ悵锟犳⒑?</summary>
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
            if (!GameUI.IsPlayerHandExpanded())
                return;
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
            bool overPlayed = false;
            foreach (var r in results)
            {
                if (r.gameObject.GetComponentInParent<PlayedZoneMarker>() != null)
                {
                    overPlayed = true;
                    break;
                }
            }

            if (!overPlayed && GameUI.IsScreenPointOverPlayedZone(eventData.position))
                overPlayed = true;
            if (overPlayed)
                GameUI.MoveHandCardToPlayedZone(HandIndex);
        }

        private void OnDestroy()
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
        }
    }

    /// <summary> 闂備胶鎳撻悘姘跺箰閹间礁鍚规い鎾卞灩缁€宀勬煟閹寸儐鐒介柛銊ャ偢閹鎷呯粙璺ㄧ泿缂備浇椴哥换鍫ュ箖娴犲惟闁靛牆娲╂竟姗€姊鸿ぐ鎺撴暠闁绘顨婂顒佺瑹閳ь剟鐛鍥ｅ亾閿濆骸澧い銈呭€块弻锟犲礃椤撶偟鍘銈冨劚閹冲酣婀侀柣搴秵閸樹粙宕曟导瀛樼厽闁归偊鍓涢悾顓㈡煃?</summary>
    public class PlayedZoneMarker : MonoBehaviour { }

    /// <summary>点击己方手牌区空白或边框时展开手牌（已展开时无操作）。</summary>
    public class PlayerHandExpandAreaClick : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            GameUI.TryExpandPlayerHandFromUiClick();
        }
    }

}
