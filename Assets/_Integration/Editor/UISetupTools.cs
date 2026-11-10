#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LucidDream.Core;
using LucidDream.Integration;
using LucidDream.UI;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// 타이틀 / 인게임 UI를 메뉴 클릭 한 번으로 자동 생성하고 모든 참조를 배선하는 에디터 도구.
    ///
    ///  - Tools ▸ Lucid Dream ▸ UI ▸ Build Title Scene UI   : 타이틀 화면 (시작/설정/종료 + 설정 패널)
    ///  - Tools ▸ Lucid Dream ▸ UI ▸ Build In-Game UI        : HUD(정신력 바·해리성 단계·정화 카운터)
    ///                                                          + 일시정지 메뉴 + 설정 패널 + 클리어/게임오버 패널
    ///                                                          + GameFlowManager / AnomalyDirector 매니저 생성
    ///
    /// 생성 후에도 모든 오브젝트는 일반 UGUI 오브젝트이므로 자유롭게 디자인을 수정하면 된다.
    /// </summary>
    public static class UISetupTools
    {
        private const string KoreanFontPath = "Assets/JJY/Resource/Font/Hakgyoansim_SangjangR SDF.asset";
        private const string GeneratedFolder = "Assets/_Integration/UI/Generated";

        private static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.07f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.13f, 0.20f, 1f);
        private static readonly Color TextColor = new Color(0.91f, 0.90f, 0.94f, 1f);

        private static TMP_FontAsset koreanFont;

        // ────────────────────────────────────────────────────────
        //  메뉴: 타이틀 UI
        // ────────────────────────────────────────────────────────

        [MenuItem("Tools/Lucid Dream/UI/Build Title Scene UI")]
        private static void BuildTitleUI()
        {
            LoadFont();
            EnsureEventSystem();

            var canvas = CreateCanvas("TitleUI_Canvas");
            var canvasGO = canvas.gameObject;

            // 배경
            var bg = CreateImage(canvasGO.transform, "Background", new Color(0.03f, 0.03f, 0.06f, 1f));
            Stretch(bg.rectTransform);

            // 타이틀 텍스트
            var title = CreateText(canvasGO.transform, "TitleText", "LUCID DREAM", 110, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1200, 160));
            title.characterSpacing = 18f;

            var tagline = CreateText(canvasGO.transform, "TaglineText",
                "이상한 게 있습니다. 찾아서 정화하세요.", 30, TextAlignmentOptions.Center);
            SetRect(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1200, 50));
            tagline.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.65f);

            // 버튼 묶음
            var buttonRoot = CreateUIObject(canvasGO.transform, "Buttons");
            SetRect((RectTransform)buttonRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(340, 280));
            var layout = buttonRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var startButton = CreateButton(buttonRoot.transform, "StartButton", "게임 시작");
            var settingsButton = CreateButton(buttonRoot.transform, "SettingsButton", "설  정");
            var quitButton = CreateButton(buttonRoot.transform, "QuitButton", "종  료");

            // 설정 패널 (버튼들 위에 그려지도록 마지막에 생성)
            var settingsPanel = BuildSettingsPanel(canvasGO.transform);

            // TitleScreenUI 배선
            var titleUI = canvasGO.AddComponent<TitleScreenUI>();
            var so = new SerializedObject(titleUI);
            so.FindProperty("gameSceneName").stringValue = "School";
            so.FindProperty("startButton").objectReferenceValue = startButton;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("quitButton").objectReferenceValue = quitButton;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();

            settingsPanel.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Selection.activeGameObject = canvasGO;
            EditorUtility.DisplayDialog("Lucid Dream - Title UI",
                "타이틀 UI 생성 완료.\n\n- 시작 버튼은 'School' 씬을 로드합니다 (TitleScreenUI에서 변경 가능).\n- 타이틀 씬과 School 씬을 File ▸ Build Profiles의 Scene List에 등록하세요.", "확인");
        }

        // ────────────────────────────────────────────────────────
        //  메뉴: 인게임 UI
        // ────────────────────────────────────────────────────────

        [MenuItem("Tools/Lucid Dream/UI/Build In-Game UI")]
        private static void BuildInGameUI()
        {
            LoadFont();
            EnsureEventSystem();
            EnsureCoreManagers(out string managerReport);

            var canvas = CreateCanvas("GameUI_Canvas");
            var canvasGO = canvas.gameObject;

            // 1) 비네트 (정신력이 낮을수록 화면 가장자리가 어두워짐)
            var vignette = CreateImage(canvasGO.transform, "Vignette", new Color(0f, 0f, 0f, 0f));
            Stretch(vignette.rectTransform);
            vignette.sprite = GetOrCreateVignetteSprite();
            vignette.raycastTarget = false;

            // 2) 정신력 바 (좌측 하단)
            var sanityRoot = CreateUIObject(canvasGO.transform, "SanityBar");
            SetRect((RectTransform)sanityRoot.transform, Vector2.zero, Vector2.zero, new Vector2(280, 80), new Vector2(420, 34));
            var sanityGroup = sanityRoot.AddComponent<CanvasGroup>();

            var barBG = CreateImage(sanityRoot.transform, "BarBackground", new Color(0f, 0f, 0f, 0.55f));
            Stretch(barBG.rectTransform);

            var barFill = CreateImage(sanityRoot.transform, "BarFill", Color.white);
            Stretch(barFill.rectTransform, 3f);
            barFill.sprite = BuiltinSprite("UISprite.psd");
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = 1f;

            var sanityLabel = CreateText(sanityRoot.transform, "Label", "정신력", 20, TextAlignmentOptions.BottomLeft);
            SetRect(sanityLabel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(105, 56), new Vector2(200, 30));

            // 3) 해리성 단계 텍스트 (정신력 바 오른쪽)
            var levelNumber = CreateText(canvasGO.transform, "DissociationLevelNumber", "Lv.1", 30, TextAlignmentOptions.Left);
            SetRect(levelNumber.rectTransform, Vector2.zero, Vector2.zero, new Vector2(560, 80), new Vector2(110, 40));

            var levelName = CreateText(canvasGO.transform, "DissociationLevelName", "자각 (Awareness)", 22, TextAlignmentOptions.Left);
            SetRect(levelName.rectTransform, Vector2.zero, Vector2.zero, new Vector2(745, 80), new Vector2(280, 40));

            // 4) 이상현상 정화 카운터 (우측 상단)
            var counter = CreateText(canvasGO.transform, "AnomalyCounter", "정화  0 / 0", 32, TextAlignmentOptions.Right);
            SetRect(counter.rectTransform, Vector2.one, Vector2.one, new Vector2(-170, -60), new Vector2(300, 50));

            // 5) 중앙 알림
            var notification = CreateText(canvasGO.transform, "Notification", "", 40, TextAlignmentOptions.Center);
            SetRect(notification.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1100, 60));

            // 6) 결과 패널 (클리어 / 게임 오버)
            var clearPanel = BuildResultPanel(canvasGO.transform, "ClearPanel",
                "꿈이 정화되었습니다", new Color(0.55f, 0.85f, 0.75f, 1f),
                out Button clearRestart, out Button clearTitle);

            var gameOverPanel = BuildResultPanel(canvasGO.transform, "GameOverPanel",
                "의식이 악몽에 잠식되었습니다", new Color(0.85f, 0.35f, 0.35f, 1f),
                out Button overRestart, out Button overTitle);

            // 7) 일시정지 패널
            var pausePanel = CreateUIObject(canvasGO.transform, "PausePanel");
            Stretch((RectTransform)pausePanel.transform);
            var pauseDim = CreateImage(pausePanel.transform, "Dim", new Color(0f, 0f, 0f, 0.6f));
            Stretch(pauseDim.rectTransform);

            var pauseBox = CreateImage(pausePanel.transform, "Box", PanelColor);
            SetRect(pauseBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 400));

            var pauseTitle = CreateText(pauseBox.transform, "Title", "일시정지", 44, TextAlignmentOptions.Center);
            SetRect(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(380, 60));

            var pauseButtons = CreateUIObject(pauseBox.transform, "Buttons");
            SetRect((RectTransform)pauseButtons.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(340, 260));
            var pauseLayout = pauseButtons.AddComponent<VerticalLayoutGroup>();
            pauseLayout.spacing = 16f;
            pauseLayout.childForceExpandHeight = false;
            pauseLayout.childControlHeight = false;
            pauseLayout.childAlignment = TextAnchor.MiddleCenter;

            var resumeButton = CreateButton(pauseButtons.transform, "ResumeButton", "계속하기");
            var pauseSettingsButton = CreateButton(pauseButtons.transform, "SettingsButton", "설  정");
            var pauseTitleButton = CreateButton(pauseButtons.transform, "TitleButton", "타이틀로 나가기");

            // 8) 설정 패널 (모든 패널 위)
            var settingsPanel = BuildSettingsPanel(canvasGO.transform);

            // ── 컴포넌트 배선 ──

            var sanityUI = canvasGO.AddComponent<SanityUI>();
            var so = new SerializedObject(sanityUI);
            so.FindProperty("sanityFill").objectReferenceValue = barFill;
            so.FindProperty("sanityBarGroup").objectReferenceValue = sanityGroup;
            so.FindProperty("levelNameText").objectReferenceValue = levelName;
            so.FindProperty("levelNumberText").objectReferenceValue = levelNumber;
            so.FindProperty("vignetteImage").objectReferenceValue = vignette;
            var gradientProp = so.FindProperty("sanityColorGradient");
            if (gradientProp != null)
                gradientProp.gradientValue = MakeSanityGradient();
            so.ApplyModifiedPropertiesWithoutUndo();

            var hud = canvasGO.AddComponent<GameHUD>();
            so = new SerializedObject(hud);
            so.FindProperty("anomalyCounterText").objectReferenceValue = counter;
            so.FindProperty("notificationText").objectReferenceValue = notification;
            so.ApplyModifiedPropertiesWithoutUndo();

            var pauseUI = canvasGO.AddComponent<PauseMenuUI>();
            so = new SerializedObject(pauseUI);
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("settingsButton").objectReferenceValue = pauseSettingsButton;
            so.FindProperty("titleButton").objectReferenceValue = pauseTitleButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            var resultUI = canvasGO.AddComponent<GameResultUI>();
            so = new SerializedObject(resultUI);
            so.FindProperty("clearPanel").objectReferenceValue = clearPanel;
            so.FindProperty("clearRestartButton").objectReferenceValue = clearRestart;
            so.FindProperty("clearTitleButton").objectReferenceValue = clearTitle;
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            so.FindProperty("gameOverRestartButton").objectReferenceValue = overRestart;
            so.FindProperty("gameOverTitleButton").objectReferenceValue = overTitle;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 패널 초기 상태는 닫힘
            clearPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            pausePanel.SetActive(false);
            settingsPanel.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Selection.activeGameObject = canvasGO;
            EditorUtility.DisplayDialog("Lucid Dream - In-Game UI",
                "인게임 UI 생성 완료.\n\n" + managerReport +
                "\nESC = 일시정지, 모든 이상현상 정화 = 클리어입니다.", "확인");
        }

        // ────────────────────────────────────────────────────────
        //  매니저 보장 (GameFlowManager / AnomalyDirector / 코어 시스템)
        // ────────────────────────────────────────────────────────

        private static void EnsureCoreManagers(out string report)
        {
            var sb = new System.Text.StringBuilder();

            if (Object.FindAnyObjectByType<GameFlowManager>(FindObjectsInactive.Include) == null)
            {
                new GameObject("GameFlowManager", typeof(GameFlowManager));
                sb.AppendLine("- GameFlowManager 생성됨");
            }

            if (Object.FindAnyObjectByType<AnomalyDirector>(FindObjectsInactive.Include) == null)
            {
                new GameObject("AnomalyDirector", typeof(AnomalyDirector));
                sb.AppendLine("- AnomalyDirector 생성됨 (씬의 AnomalyPurifiable 자동 수집)");
            }

            // 코어 시스템은 플레이어 위치 기반 동작(Respawn)이 있어 플레이어에 붙이는 것이 정석.
            var player = GameObject.FindGameObjectWithTag("Player");

            if (Object.FindAnyObjectByType<SanityManager>(FindObjectsInactive.Include) == null)
            {
                if (player != null)
                {
                    player.AddComponent<SanityManager>();
                    sb.AppendLine("- SanityManager를 Player에 추가함");
                }
                else
                {
                    new GameObject("SanityManager (Player에 옮겨주세요)", typeof(SanityManager));
                    sb.AppendLine("- [주의] Player 태그 오브젝트가 없어 SanityManager를 빈 오브젝트로 생성. 플레이어에 옮기세요.");
                }
            }

            if (Object.FindAnyObjectByType<DissociationLevelManager>(FindObjectsInactive.Include) == null)
            {
                new GameObject("DissociationLevelManager", typeof(DissociationLevelManager));
                sb.AppendLine("- DissociationLevelManager 생성됨");
            }

            if (Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include) == null)
            {
                var gm = new GameObject("GameManager", typeof(GameManager)).GetComponent<GameManager>();
                if (player != null)
                {
                    var so = new SerializedObject(gm);
                    so.FindProperty("player").objectReferenceValue = player.transform;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                sb.AppendLine("- GameManager(체크포인트 부활) 생성됨");
            }

            report = sb.Length > 0 ? sb.ToString() : "- 필요한 매니저가 모두 이미 존재합니다.\n";
        }

        // ────────────────────────────────────────────────────────
        //  설정 패널 빌더 (타이틀/일시정지 공용)
        // ────────────────────────────────────────────────────────

        private static SettingsPanelUI BuildSettingsPanel(Transform canvasParent)
        {
            var root = CreateUIObject(canvasParent, "SettingsPanel");
            Stretch((RectTransform)root.transform);

            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.rectTransform);

            var box = CreateImage(root.transform, "Box", PanelColor);
            SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 660));

            var title = CreateText(box.transform, "Title", "설  정", 44, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(400, 60));

            float y = -140f;
            const float rowH = 84f;

            // 전체 볼륨
            var volumeSlider = CreateLabeledSlider(box.transform, "MasterVolumeRow", "전체 볼륨", y, out TMP_Text volumeValue);
            y -= rowH;

            // 마우스 감도
            var sensitivitySlider = CreateLabeledSlider(box.transform, "SensitivityRow", "마우스 감도", y, out TMP_Text sensitivityValue);
            y -= rowH;

            // 전체 화면
            var fullscreenToggle = CreateLabeledToggle(box.transform, "FullscreenRow", "전체 화면", y);
            y -= rowH;

            // 해상도
            var resolutionDropdown = CreateLabeledDropdown(box.transform, "ResolutionRow", "해상도", y);
            y -= rowH;

            // 그래픽 품질
            var qualityDropdown = CreateLabeledDropdown(box.transform, "QualityRow", "그래픽 품질", y);

            // 닫기
            var closeButton = CreateButton(box.transform, "CloseButton", "닫  기");
            var closeRT = (RectTransform)closeButton.transform;
            SetRect(closeRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(280, 64));

            var panelUI = root.AddComponent<SettingsPanelUI>();
            var so = new SerializedObject(panelUI);
            so.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("masterVolumeValueText").objectReferenceValue = volumeValue;
            so.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
            so.FindProperty("sensitivityValueText").objectReferenceValue = sensitivityValue;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
            so.FindProperty("resolutionDropdown").objectReferenceValue = resolutionDropdown;
            so.FindProperty("qualityDropdown").objectReferenceValue = qualityDropdown;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panelUI;
        }

        private static Slider CreateLabeledSlider(Transform parent, string name, string label, float y, out TMP_Text valueText)
        {
            var row = CreateRow(parent, name, label, y);

            var sliderGO = DefaultControls.CreateSlider(GetLegacyResources());
            sliderGO.name = "Slider";
            sliderGO.transform.SetParent(row.transform, false);
            var rt = (RectTransform)sliderGO.transform;
            SetRect(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(420, 0), new Vector2(260, 28));

            valueText = CreateText(row.transform, "Value", "100%", 24, TextAlignmentOptions.Left);
            SetRect(valueText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(620, 0), new Vector2(110, 36));

            return sliderGO.GetComponent<Slider>();
        }

        private static Toggle CreateLabeledToggle(Transform parent, string name, string label, float y)
        {
            var row = CreateRow(parent, name, label, y);

            var toggleGO = DefaultControls.CreateToggle(GetLegacyResources());
            toggleGO.name = "Toggle";
            toggleGO.transform.SetParent(row.transform, false);
            var rt = (RectTransform)toggleGO.transform;
            SetRect(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(310, 0), new Vector2(40, 40));

            // 기본 토글의 레거시 라벨은 제거하고 체크박스를 키운다.
            var legacyLabel = toggleGO.transform.Find("Label");
            if (legacyLabel != null) Object.DestroyImmediate(legacyLabel.gameObject);

            var bgRT = toggleGO.transform.Find("Background") as RectTransform;
            if (bgRT != null)
            {
                bgRT.sizeDelta = new Vector2(36, 36);
                var checkRT = bgRT.Find("Checkmark") as RectTransform;
                if (checkRT != null) checkRT.sizeDelta = new Vector2(28, 28);
            }

            return toggleGO.GetComponent<Toggle>();
        }

        private static TMP_Dropdown CreateLabeledDropdown(Transform parent, string name, string label, float y)
        {
            var row = CreateRow(parent, name, label, y);

            var dropdownGO = TMP_DefaultControls.CreateDropdown(GetTMPResources());
            dropdownGO.name = "Dropdown";
            dropdownGO.transform.SetParent(row.transform, false);
            var rt = (RectTransform)dropdownGO.transform;
            SetRect(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(470, 0), new Vector2(360, 46));

            // 드롭다운 내부의 모든 TMP 텍스트에 한국어 폰트 적용
            foreach (var text in dropdownGO.GetComponentsInChildren<TMP_Text>(true))
                ApplyFont(text);

            return dropdownGO.GetComponent<TMP_Dropdown>();
        }

        private static GameObject CreateRow(Transform parent, string name, string label, float y)
        {
            var row = CreateUIObject(parent, name);
            var rt = (RectTransform)row.transform;
            SetRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(700, 60));

            var labelText = CreateText(row.transform, "Label", label, 28, TextAlignmentOptions.Left);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150, 0), new Vector2(260, 44));

            return row;
        }

        // ────────────────────────────────────────────────────────
        //  결과 패널 빌더
        // ────────────────────────────────────────────────────────

        private static GameObject BuildResultPanel(Transform canvasParent, string name, string message, Color accent,
            out Button restartButton, out Button titleButton)
        {
            var root = CreateUIObject(canvasParent, name);
            Stretch((RectTransform)root.transform);

            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.78f));
            Stretch(dim.rectTransform);

            var messageText = CreateText(root.transform, "Message", message, 56, TextAlignmentOptions.Center);
            SetRect(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1400, 90));
            messageText.color = accent;

            var buttons = CreateUIObject(root.transform, "Buttons");
            SetRect((RectTransform)buttons.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(720, 70));
            var layout = buttons.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            restartButton = CreateButton(buttons.transform, "RestartButton", "다시 하기");
            titleButton = CreateButton(buttons.transform, "TitleButton", "타이틀로");

            return root;
        }

        // ────────────────────────────────────────────────────────
        //  공통 UI 생성 헬퍼
        // ────────────────────────────────────────────────────────

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static GameObject CreateUIObject(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = CreateUIObject(parent, name);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment)
        {
            var go = CreateUIObject(parent, name);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            ApplyFont(text);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var go = TMP_DefaultControls.CreateButton(GetTMPResources());
            go.name = name;
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(320, 64);

            var image = go.GetComponent<Image>();
            image.color = ButtonColor;

            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = label;
                text.fontSize = 30;
                text.color = TextColor;
                ApplyFont(text);
            }

            return go.GetComponent<Button>();
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        // ────────────────────────────────────────────────────────
        //  리소스 (폰트 / 내장 스프라이트 / 비네트)
        // ────────────────────────────────────────────────────────

        private static void LoadFont()
        {
            koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (koreanFont == null)
                Debug.LogWarning($"[UISetupTools] 한국어 TMP 폰트를 찾지 못했습니다: {KoreanFontPath}\nTMP 기본 폰트로 생성됩니다(한글이 □로 보일 수 있음).");
        }

        private static void ApplyFont(TMP_Text text)
        {
            if (koreanFont != null)
                text.font = koreanFont;
        }

        private static Sprite BuiltinSprite(string fileName)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + fileName);
        }

        private static DefaultControls.Resources GetLegacyResources()
        {
            return new DefaultControls.Resources
            {
                standard = BuiltinSprite("UISprite.psd"),
                background = BuiltinSprite("Background.psd"),
                inputField = BuiltinSprite("InputFieldBackground.psd"),
                knob = BuiltinSprite("Knob.psd"),
                checkmark = BuiltinSprite("Checkmark.psd"),
                dropdown = BuiltinSprite("DropdownArrow.psd"),
                mask = BuiltinSprite("UIMask.psd"),
            };
        }

        private static TMP_DefaultControls.Resources GetTMPResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = BuiltinSprite("UISprite.psd"),
                background = BuiltinSprite("Background.psd"),
                inputField = BuiltinSprite("InputFieldBackground.psd"),
                knob = BuiltinSprite("Knob.psd"),
                checkmark = BuiltinSprite("Checkmark.psd"),
                dropdown = BuiltinSprite("DropdownArrow.psd"),
                mask = BuiltinSprite("UIMask.psd"),
            };
        }

        private static Gradient MakeSanityGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 0.20f, 0.25f), 0f),   // 낮음: 붉은색
                    new GradientColorKey(new Color(0.90f, 0.75f, 0.30f), 0.5f), // 중간: 노란색
                    new GradientColorKey(new Color(0.45f, 0.85f, 0.70f), 1f),   // 높음: 청록색
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        /// <summary>화면 가장자리로 갈수록 불투명해지는 비네트 스프라이트를 생성(최초 1회)하고 로드한다.</summary>
        private static Sprite GetOrCreateVignetteSprite()
        {
            string path = GeneratedFolder + "/Vignette.png";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists(GeneratedFolder))
                Directory.CreateDirectory(GeneratedFolder);

            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int yPix = 0; yPix < size; yPix++)
            {
                for (int xPix = 0; xPix < size; xPix++)
                {
                    float dist = Vector2.Distance(new Vector2(xPix, yPix), center) / maxDist;
                    // 중심부는 투명, 가장자리는 검게. 0.45 지점부터 서서히 어두워진다.
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.15f, dist));
                    tex.SetPixel(xPix, yPix, new Color(0f, 0f, 0f, alpha));
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
