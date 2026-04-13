using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JunzhenDuijue
{
    /// <summary>
    /// 主界面：标题 + 开始游戏 / 图鉴&组牌 / 退出游戏；首次进入显示注册弹窗；右上角显示用户名。
    /// </summary>
    public static class MainMenuUI
    {
        private static GameObject _root;
        private static GameObject _regRoot;
        private static TMP_InputField _regUsername;
        private static TMP_InputField _regPassword;
        private static TMP_InputField _regConfirm;
        private static TextMeshProUGUI _regErrorText;
        private static TextMeshProUGUI _usernameLabel;

        public static void Create()
        {
            _root = new GameObject("MainMenu");
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1920, 1080); // 与 referenceResolution 一致，确保首帧布局正确
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_root.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.18f, 0.22f, 1f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root.transform, false);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "军阵对决";
            if (TMPHelper.GetDefaultFont() != null) titleText.font = TMPHelper.GetDefaultFont();
            titleText.fontSize = 72;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.85f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(600, 100);
            titleRect.anchoredPosition = Vector2.zero;

            float buttonWidth = 320f;
            float buttonHeight = 56f;
            float startY = 0.45f;

            // 开始游戏
            var btn1 = CreateButton("开始游戏", startY, buttonWidth, buttonHeight);
            btn1.onClick.AddListener(OnStartGame);

            // 联机对战
            var btn2 = CreateButton("联机对战", startY - 0.12f, buttonWidth, buttonHeight);
            btn2.onClick.AddListener(OnOpenOnlineGame);

            // 图鉴&组牌
            var btn3 = CreateButton("图鉴&组牌", startY - 0.24f, buttonWidth, buttonHeight);
            btn3.onClick.AddListener(OnOpenCompendium);

            // 退出游戏
            var btn4 = CreateButton("退出游戏", startY - 0.36f, buttonWidth, buttonHeight);
            btn4.onClick.AddListener(OnExitGame);

            // 右上角用户名
            var usernameGo = new GameObject("UsernameLabel");
            usernameGo.transform.SetParent(_root.transform, false);
            _usernameLabel = usernameGo.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null) _usernameLabel.font = TMPHelper.GetDefaultFont();
            _usernameLabel.fontSize = 24;
            _usernameLabel.alignment = TextAlignmentOptions.MidlineRight;
            _usernameLabel.color = Color.white;
            var usernameRect = usernameGo.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(1f, 1f);
            usernameRect.anchorMax = new Vector2(1f, 1f);
            usernameRect.pivot = new Vector2(1f, 1f);
            usernameRect.anchoredPosition = new Vector2(-24, -24);
            usernameRect.sizeDelta = new Vector2(320, 36);

            BuildRegistrationPopup();
            RefreshUsernameLabel();
            if (!AccountManager.IsRegistered())
                ShowRegistrationPopup();
        }

        private static Button CreateButton(string label, float anchorY, float width, float height)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(_root.transform, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.35f, 0.5f, 1f);
            var button = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void OnStartGame()
        {
            Hide();
            DeckSelectUI.Show();
        }

        private static void OnOpenOnlineGame()
        {
            Hide();
            OnlineLobbyUI.Show();
        }

        private static void OnOpenCompendium()
        {
            Hide();
            CompendiumUI.Show();
        }

        private static void OnExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void BuildRegistrationPopup()
        {
            _regRoot = new GameObject("RegistrationPopup");
            _regRoot.transform.SetParent(_root.transform, false);
            var rootRect = _regRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 100% 黑色遮罩
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_regRoot.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 1f);
            overlayImg.raycastTarget = true;

            // 居中面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_regRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(420, 340);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "注册";
            if (TMPHelper.GetDefaultFont() != null) titleText.font = TMPHelper.GetDefaultFont();
            titleText.fontSize = 32;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            var titleR = titleGo.GetComponent<RectTransform>();
            titleR.anchorMin = new Vector2(0.5f, 1f);
            titleR.anchorMax = new Vector2(0.5f, 1f);
            titleR.pivot = new Vector2(0.5f, 1f);
            titleR.anchoredPosition = new Vector2(0, -20);
            titleR.sizeDelta = new Vector2(200, 40);

            float rowH = 38f;
            float gap = 14f;

            float y = -72f;
            var userRow = CreateRegRow(panel.transform, "用户名", rowH, 372f, out var userInput);
            SetRegRowPos(userRow.GetComponent<RectTransform>(), ref y, rowH);
            _regUsername = userInput.GetComponent<TMP_InputField>();
            y -= gap;

            var passRow = CreateRegRow(panel.transform, "密码", rowH, 372f, out var passInput);
            SetRegRowPos(passRow.GetComponent<RectTransform>(), ref y, rowH);
            _regPassword = passInput.GetComponent<TMP_InputField>();
            _regPassword.contentType = TMP_InputField.ContentType.Password;
            y -= gap;

            var confirmRow = CreateRegRow(panel.transform, "确认密码", rowH, 372f, out var confirmInput);
            SetRegRowPos(confirmRow.GetComponent<RectTransform>(), ref y, rowH);
            _regConfirm = confirmInput.GetComponent<TMP_InputField>();
            _regConfirm.contentType = TMP_InputField.ContentType.Password;
            y -= 12f;

            var errGo = new GameObject("ErrorText");
            errGo.transform.SetParent(panel.transform, false);
            _regErrorText = errGo.AddComponent<TextMeshProUGUI>();
            _regErrorText.text = "密码不相同";
            if (TMPHelper.GetDefaultFont() != null) _regErrorText.font = TMPHelper.GetDefaultFont();
            _regErrorText.fontSize = 18;
            _regErrorText.color = Color.red;
            _regErrorText.alignment = TextAlignmentOptions.Center;
            var errRect = errGo.GetComponent<RectTransform>();
            errRect.anchorMin = new Vector2(0.5f, 1f);
            errRect.anchorMax = new Vector2(0.5f, 1f);
            errRect.pivot = new Vector2(0.5f, 1f);
            errRect.anchoredPosition = new Vector2(0, y - 12f);
            errRect.sizeDelta = new Vector2(360, 24);
            errGo.SetActive(false);

            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panel.transform, false);
            var btnRowRect = btnRow.AddComponent<RectTransform>();
            btnRowRect.anchorMin = new Vector2(0.5f, 0f);
            btnRowRect.anchorMax = new Vector2(0.5f, 0f);
            btnRowRect.pivot = new Vector2(0.5f, 0f);
            btnRowRect.anchoredPosition = new Vector2(0, 36);
            btnRowRect.sizeDelta = new Vector2(260, 44);
            var btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 24f;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childControlHeight = false;

            var saveBtn = CreateButton("保存", 0.5f, 100, 40);
            saveBtn.transform.SetParent(btnRow.transform, false);
            var saveLE = saveBtn.gameObject.AddComponent<LayoutElement>();
            saveLE.preferredWidth = 100f;
            saveLE.preferredHeight = 40f;
            saveBtn.onClick.AddListener(OnRegistrationSave);

            var cancelBtn = CreateButton("取消", 0.5f, 100, 40);
            cancelBtn.transform.SetParent(btnRow.transform, false);
            var cancelLE = cancelBtn.gameObject.AddComponent<LayoutElement>();
            cancelLE.preferredWidth = 100f;
            cancelLE.preferredHeight = 40f;
            cancelBtn.onClick.AddListener(OnExitGame);

            _regRoot.SetActive(false);
        }

        private static void SetRegRowPos(RectTransform rowRect, ref float y, float rowH)
        {
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0, y - rowH * 0.5f);
            y -= rowH;
        }

        private static GameObject CreateRegRow(Transform parent, string labelText, float rowH, float rowW, out GameObject inputGo)
        {
            var row = new GameObject("Row_" + labelText);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(rowW, rowH);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var label = new GameObject("Label");
            label.transform.SetParent(row.transform, false);
            var labelT = label.AddComponent<TextMeshProUGUI>();
            labelT.text = labelText;
            if (TMPHelper.GetDefaultFont() != null) labelT.font = TMPHelper.GetDefaultFont();
            labelT.fontSize = 20;
            labelT.color = Color.white;
            var labelR = label.GetComponent<RectTransform>();
            var labelLE = label.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 80f;
            labelLE.preferredHeight = rowH;

            inputGo = new GameObject("InputField");
            inputGo.transform.SetParent(row.transform, false);
            var inputR = inputGo.AddComponent<RectTransform>();
            var inputLE = inputGo.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1f;
            inputLE.preferredHeight = rowH;
            inputGo.AddComponent<Image>().color = new Color(0.3f, 0.35f, 0.45f, 1f);
            var input = inputGo.AddComponent<TMP_InputField>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            if (TMPHelper.GetDefaultFont() != null) text.font = TMPHelper.GetDefaultFont();
            text.fontSize = 20;
            text.color = Color.white;
            var textR = textGo.GetComponent<RectTransform>();
            textR.anchorMin = Vector2.zero;
            textR.anchorMax = Vector2.one;
            textR.offsetMin = new Vector2(8, 4);
            textR.offsetMax = new Vector2(-8, -4);
            input.textViewport = textR;
            input.textComponent = text;
            input.targetGraphic = inputGo.GetComponent<Image>();
            return row;
        }

        private static void OnRegistrationSave()
        {
            if (_regErrorText != null) _regErrorText.gameObject.SetActive(false);
            string user = (_regUsername != null ? _regUsername.text : "").Trim();
            string pass = _regPassword != null ? _regPassword.text : "";
            string confirm = _regConfirm != null ? _regConfirm.text : "";
            if (pass != confirm)
            {
                if (_regErrorText != null)
                {
                    _regErrorText.text = "密码不相同";
                    _regErrorText.gameObject.SetActive(true);
                }
                return;
            }
            if (string.IsNullOrEmpty(user))
            {
                if (_regErrorText != null)
                {
                    _regErrorText.text = "请输入用户名";
                    _regErrorText.gameObject.SetActive(true);
                }
                return;
            }
            AccountManager.SetCurrentUser(user);
            CloseRegistrationPopup();
            RefreshUsernameLabel();
        }

        private static void ShowRegistrationPopup()
        {
            if (_regRoot != null)
            {
                _regRoot.SetActive(true);
                if (_regErrorText != null) _regErrorText.gameObject.SetActive(false);
            }
        }

        private static void CloseRegistrationPopup()
        {
            if (_regRoot != null) _regRoot.SetActive(false);
        }

        private static void RefreshUsernameLabel()
        {
            if (_usernameLabel != null)
            {
                var name = AccountManager.GetCurrentUsername();
                _usernameLabel.text = string.IsNullOrEmpty(name) ? "" : name;
                _usernameLabel.gameObject.SetActive(!string.IsNullOrEmpty(name));
            }
        }

        public static void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
                RefreshUsernameLabel();
                if (!AccountManager.IsRegistered())
                    ShowRegistrationPopup();
            }
        }

        public static void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }
    }
}
