using TMPro;
using UnityEngine;

public class BlackboardGraffitiAnomaly : PurifiableObject
{
    [System.Serializable]
    private class GraffitiLine
    {
        public string text = "나가";
        public Vector2 normalizedPosition = new Vector2(0.3f, 0.3f);
        public float fontSize = 0.22f;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        [Range(0f, 1f)] public float revealStart = 0f;
    }

    [Header("Board")]
    [SerializeField] private Transform boardTransform;
    [SerializeField] private string boardChildName = "칠판";
    [SerializeField] private bool useMeshBoundsSize = true;
    [SerializeField, Min(0.01f)] private float boardWidth = 2.6f;
    [SerializeField, Min(0.01f)] private float boardHeight = 1.35f;
    [SerializeField, Range(0f, 0.45f)] private float boardPadding = 0.12f;
    [SerializeField] private float surfaceOffset = 0.012f;
    [SerializeField] private Vector3 textLocalEulerAngles = Vector3.zero;
    [SerializeField, Min(0.001f)] private float textLocalScale = 1f;

    [Header("Assigned Text")]
    [SerializeField] private TMP_Text graffitiText;
    [SerializeField] private string fullGraffitiText = "누가 지웠어?\n문 열지 마\n뒤에 있어\n나가지 마\n칠판을 보지 마";

    [Header("Graffiti Timing")]
    [SerializeField, Min(0f)] private float startDelay = 5f;
    [SerializeField, Min(0.1f)] private float revealDuration = 18f;
    [SerializeField, Range(0.01f, 1f)] private float lineRevealSpan = 0.28f;
    [SerializeField, Range(0f, 1f)] private float purifiableRevealThreshold = 0.1f;

    [Header("Graffiti Visual")]
    [SerializeField] private Color chalkColor = new Color(0.86f, 0.84f, 0.78f, 0.92f);
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] private int sortingOrder = 20;

    [Header("Graffiti Lines")]
    [SerializeField] private GraffitiLine[] graffitiLines =
    {
        new GraffitiLine
        {
            text = "누가 지웠어?",
            normalizedPosition = new Vector2(0.14f, 0.84f),
            fontSize = 0.17f,
            alignment = TextAlignmentOptions.TopLeft,
            revealStart = 0f
        },
        new GraffitiLine
        {
            text = "문 열지 마",
            normalizedPosition = new Vector2(0.82f, 0.82f),
            fontSize = 0.18f,
            alignment = TextAlignmentOptions.TopRight,
            revealStart = 0.14f
        },
        new GraffitiLine
        {
            text = "뒤에 있어",
            normalizedPosition = new Vector2(0.18f, 0.18f),
            fontSize = 0.19f,
            alignment = TextAlignmentOptions.BottomLeft,
            revealStart = 0.32f
        },
        new GraffitiLine
        {
            text = "나가지 마",
            normalizedPosition = new Vector2(0.78f, 0.2f),
            fontSize = 0.2f,
            alignment = TextAlignmentOptions.BottomRight,
            revealStart = 0.48f
        },
        new GraffitiLine
        {
            text = "칠판을 보지 마",
            normalizedPosition = new Vector2(0.5f, 0.53f),
            fontSize = 0.24f,
            alignment = TextAlignmentOptions.Center,
            revealStart = 0.66f
        }
    };

    private TMP_Text[] generatedTexts;
    private float elapsedTime;
    private float reveal01;
    private float purificationFade = 1f;
    private bool isUsingAssignedGraffitiText;
    private float assignedTemplateFontSize = 5f;
    private Vector3 boardLocalCenter = Vector3.zero;

    public override bool CanBePurified => base.CanBePurified && reveal01 >= purifiableRevealThreshold;

    private void Reset()
    {
        boardTransform = FindDeepChild(transform, boardChildName);
    }

    private void Awake()
    {
        if (boardTransform == null)
            boardTransform = FindDeepChild(transform, boardChildName);

        if (boardTransform == null)
            boardTransform = transform;

        ApplyMeshBoundsSize();
        InitializeGraffitiTexts();
        SetGraffitiReveal(0f);
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        reveal01 = 0f;
        purificationFade = 1f;
        SetGraffitiReveal(0f);
    }

    private void Update()
    {
        if (IsPurified)
            return;

        elapsedTime += Time.deltaTime;

        float revealElapsed = elapsedTime - startDelay;
        reveal01 = revealElapsed <= 0f ? 0f : Mathf.Clamp01(revealElapsed / revealDuration);
        SetGraffitiReveal(reveal01);
    }

    protected override void OnPurifyProgressChanged(float progress01)
    {
        purificationFade = 1f - Mathf.Clamp01(progress01);
        SetGraffitiReveal(reveal01);
    }

    protected override void OnPurifyComplete()
    {
        base.OnPurifyComplete();

        if (WJ_AnomalyManager.Instance != null)
            WJ_AnomalyManager.Instance.RemoveAnomaly(gameObject);

        purificationFade = 0f;
        SetGraffitiReveal(0f);
    }

    private void InitializeGraffitiTexts()
    {
        if (graffitiText != null && graffitiLines != null && graffitiLines.Length > 0)
        {
            CreateGraffitiTextsFromTemplate(graffitiText);
            return;
        }

        if (graffitiText != null)
        {
            isUsingAssignedGraffitiText = true;
            ConfigureAssignedText(graffitiText);
            generatedTexts = new[] { graffitiText };
            return;
        }

        isUsingAssignedGraffitiText = false;
        CreateGraffitiTexts();
    }

    private void CreateGraffitiTextsFromTemplate(TMP_Text template)
    {
        isUsingAssignedGraffitiText = false;
        assignedTemplateFontSize = Mathf.Max(0.01f, template.fontSize);

        Vector3 templatePosition = template.transform.localPosition;
        Quaternion templateRotation = template.transform.localRotation;
        Vector3 templateScale = template.transform.localScale;

        Transform root = new GameObject("GeneratedBlackboardGraffiti").transform;
        root.SetParent(boardTransform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        generatedTexts = new TMP_Text[graffitiLines.Length];
        template.gameObject.SetActive(false);

        for (int i = 0; i < graffitiLines.Length; i++)
        {
            GraffitiLine line = graffitiLines[i];
            GameObject textObject = Instantiate(template.gameObject, root);
            textObject.name = "GraffitiText_" + i;
            textObject.SetActive(true);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            ConfigureTemplateText(text, line);
            PositionTemplateText(textObject.transform, line.normalizedPosition, templatePosition.z, templateRotation, templateScale);

            generatedTexts[i] = text;
        }
    }

    private void ConfigureAssignedText(TMP_Text text)
    {
        text.text = fullGraffitiText;
        text.color = chalkColor;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.maxVisibleCharacters = 0;

        if (fontAsset != null)
            text.font = fontAsset;

        Renderer textRenderer = text.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder;
    }

    private void ConfigureTemplateText(TMP_Text text, GraffitiLine line)
    {
        if (text == null)
            return;

        text.text = line.text;
        text.color = chalkColor;
        text.fontSize = assignedTemplateFontSize * Mathf.Max(0.5f, line.fontSize / 0.2f);
        text.alignment = line.alignment;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.maxVisibleCharacters = 0;
        ConfigureTemplateRect(text, line);

        if (fontAsset != null)
            text.font = fontAsset;

        Renderer textRenderer = text.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder;
    }

    private void ConfigureTemplateRect(TMP_Text text, GraffitiLine line)
    {
        RectTransform rectTransform = text.rectTransform;

        if (rectTransform == null)
            return;

        float width = Mathf.Max(4f, line.text.Length * assignedTemplateFontSize * 0.45f);
        float height = Mathf.Max(1.5f, assignedTemplateFontSize * 0.7f);
        rectTransform.sizeDelta = new Vector2(width, height);

        switch (line.alignment)
        {
            case TextAlignmentOptions.TopLeft:
                rectTransform.pivot = new Vector2(0f, 1f);
                break;
            case TextAlignmentOptions.TopRight:
                rectTransform.pivot = new Vector2(1f, 1f);
                break;
            case TextAlignmentOptions.BottomLeft:
                rectTransform.pivot = new Vector2(0f, 0f);
                break;
            case TextAlignmentOptions.BottomRight:
                rectTransform.pivot = new Vector2(1f, 0f);
                break;
            default:
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;
        }
    }

    private void CreateGraffitiTexts()
    {
        if (graffitiLines == null || graffitiLines.Length == 0)
        {
            generatedTexts = new TMP_Text[0];
            return;
        }

        Transform root = new GameObject("GeneratedBlackboardGraffiti").transform;
        root.SetParent(boardTransform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        generatedTexts = new TMP_Text[graffitiLines.Length];

        for (int i = 0; i < graffitiLines.Length; i++)
        {
            GraffitiLine line = graffitiLines[i];
            GameObject textObject = new GameObject("GraffitiText_" + i);
            textObject.transform.SetParent(root, false);

            TMP_Text text = textObject.AddComponent<TextMeshPro>();
            ConfigureText(text, line);
            PositionText(text.transform, line.normalizedPosition);

            generatedTexts[i] = text;
        }
    }

    private void ApplyMeshBoundsSize()
    {
        if (!useMeshBoundsSize || boardTransform == null)
            return;

        if (TryApplyRendererBoundsSize())
            return;

        MeshFilter meshFilter = boardTransform.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Bounds bounds = meshFilter.sharedMesh.bounds;
        boardLocalCenter = bounds.center;
        Vector3 size = bounds.size;

        if (size.x > 0.01f)
            boardWidth = size.x;

        if (size.y > 0.01f)
            boardHeight = size.y;
    }

    private bool TryApplyRendererBoundsSize()
    {
        Renderer[] renderers = boardTransform.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || ShouldIgnoreBoundsRenderer(renderer))
                continue;

            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            for (int j = 0; j < corners.Length; j++)
            {
                Vector3 localCorner = boardTransform.InverseTransformPoint(corners[j]);

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds)
            return false;

        boardLocalCenter = localBounds.center;
        Vector3 size = localBounds.size;

        if (size.x > 0.01f)
            boardWidth = size.x;

        if (size.y > 0.01f)
            boardHeight = size.y;

        return boardWidth > 0.01f && boardHeight > 0.01f;
    }

    private bool ShouldIgnoreBoundsRenderer(Renderer renderer)
    {
        if (renderer.GetComponent<TMP_Text>() != null)
            return true;

        Transform current = renderer.transform;

        while (current != null && current != boardTransform)
        {
            if (current.name.StartsWith("GeneratedBlackboardGraffiti")
                || current.name.StartsWith("GraffitiText_")
                || current.name.EndsWith("_GeneratedOutline")
                || current.name == "Highlight")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ConfigureText(TMP_Text text, GraffitiLine line)
    {
        text.text = line.text;
        text.color = chalkColor;
        text.fontSize = line.fontSize;
        text.alignment = line.alignment;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.maxVisibleCharacters = 0;

        if (fontAsset != null)
            text.font = fontAsset;

        Renderer textRenderer = text.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder;
    }

    private void PositionText(Transform textTransform, Vector2 normalizedPosition)
    {
        Vector2 localPosition = GetBoardLocalPosition(normalizedPosition);

        textTransform.localPosition = new Vector3(localPosition.x, localPosition.y, boardLocalCenter.z + surfaceOffset);
        textTransform.localEulerAngles = textLocalEulerAngles;
        textTransform.localScale = Vector3.one * textLocalScale;
    }

    private void PositionTemplateText(Transform textTransform, Vector2 normalizedPosition, float z, Quaternion rotation, Vector3 scale)
    {
        Vector2 localPosition = GetBoardLocalPosition(normalizedPosition);

        textTransform.localPosition = new Vector3(localPosition.x, localPosition.y, boardLocalCenter.z + z);
        textTransform.localRotation = rotation;
        textTransform.localScale = scale;
    }

    private Vector2 GetBoardLocalPosition(Vector2 normalizedPosition)
    {
        float usableWidth = boardWidth * (1f - boardPadding * 2f);
        float usableHeight = boardHeight * (1f - boardPadding * 2f);
        float x = boardLocalCenter.x + (Mathf.Clamp01(normalizedPosition.x) - 0.5f) * usableWidth;
        float y = boardLocalCenter.y + (Mathf.Clamp01(normalizedPosition.y) - 0.5f) * usableHeight;

        return new Vector2(x, y);
    }

    private void SetGraffitiReveal(float value)
    {
        if (generatedTexts == null || graffitiLines == null)
            return;

        for (int i = 0; i < generatedTexts.Length; i++)
        {
            TMP_Text text = generatedTexts[i];

            if (text == null)
                continue;

            GraffitiLine line = i < graffitiLines.Length ? graffitiLines[i] : null;
            float revealStart = !isUsingAssignedGraffitiText && line != null ? line.revealStart : 0f;
            float revealSpan = isUsingAssignedGraffitiText ? 1f : lineRevealSpan;
            float lineReveal01 = Mathf.Clamp01((value - revealStart) / revealSpan);
            float easedReveal = Mathf.SmoothStep(0f, 1f, lineReveal01);
            Color color = chalkColor;
            color.a *= easedReveal * purificationFade;

            text.color = color;
            text.maxVisibleCharacters = Mathf.CeilToInt(text.text.Length * lineReveal01);
            text.gameObject.SetActive(color.a > 0.001f);
        }
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }
}
