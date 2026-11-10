using LucidDream.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SchoolSanityHUD : MonoBehaviour
{
    private const int MinDissociationLevel = 1;
    private const int MaxDissociationLevel = 5;

    [Header("Sanity")]
    [SerializeField] private TMP_Text sanityText;
    [SerializeField] private Image sanityFillImage;
    [SerializeField] private string sanityTextFormat = "Sanity : {0}%";

    [Header("Dissociation")]
    [SerializeField] private Image[] dissociationLevelImages;
    [SerializeField] private bool useCurrentLevelAsVisibleCount = true;
    [SerializeField] private bool autoBindStepImages = true;

    [Header("Debug Input")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private float sanityDebugAmount = 10f;
    [SerializeField] private bool createManagersForDebugIfMissing = true;

    private SanityManager subscribedSanityManager;
    private DissociationLevelManager subscribedDissociationManager;

    private void Awake()
    {
        AutoBindReferences();
        ConfigureSanityFillImage();
    }

    private void OnEnable()
    {
        AutoBindReferences();
        ConfigureSanityFillImage();
        TrySubscribeManagers();
        RefreshAll();
    }

    private void Update()
    {
        if (subscribedSanityManager == null || subscribedDissociationManager == null)
        {
            TrySubscribeManagers();
            RefreshAll();
        }

        HandleDebugInput();
    }

    private void OnDisable()
    {
        UnsubscribeManagers();
    }

    private void TrySubscribeManagers()
    {
        SanityManager sanityManager = SanityManager.Instance;
        if (sanityManager == null && createManagersForDebugIfMissing)
            sanityManager = new GameObject("Runtime SanityManager").AddComponent<SanityManager>();

        if (sanityManager != null && subscribedSanityManager != sanityManager)
        {
            if (subscribedSanityManager != null)
                subscribedSanityManager.OnSanityChanged.RemoveListener(UpdateSanityUI);

            subscribedSanityManager = sanityManager;
            subscribedSanityManager.OnSanityChanged.AddListener(UpdateSanityUI);
        }

        DissociationLevelManager dissociationManager = DissociationLevelManager.Instance;
        if (dissociationManager == null && createManagersForDebugIfMissing)
            dissociationManager = new GameObject("Runtime DissociationLevelManager").AddComponent<DissociationLevelManager>();

        if (dissociationManager != null && subscribedDissociationManager != dissociationManager)
        {
            if (subscribedDissociationManager != null)
                subscribedDissociationManager.OnLevelChanged.RemoveListener(UpdateDissociationUI);

            subscribedDissociationManager = dissociationManager;
            subscribedDissociationManager.OnLevelChanged.AddListener(UpdateDissociationUI);
        }
    }

    private void UnsubscribeManagers()
    {
        if (subscribedSanityManager != null)
            subscribedSanityManager.OnSanityChanged.RemoveListener(UpdateSanityUI);

        if (subscribedDissociationManager != null)
            subscribedDissociationManager.OnLevelChanged.RemoveListener(UpdateDissociationUI);

        subscribedSanityManager = null;
        subscribedDissociationManager = null;
    }

    private void AutoBindReferences()
    {
        if (sanityText == null)
            sanityText = FindChildComponentByName<TMP_Text>("Sanity_Text");

        if (autoBindStepImages && (dissociationLevelImages == null || dissociationLevelImages.Length == 0))
        {
            dissociationLevelImages = new Image[MaxDissociationLevel];

            for (int i = 0; i < dissociationLevelImages.Length; i++)
                dissociationLevelImages[i] = FindChildComponentByName<Image>($"Step{i + 1}_Base");
        }
    }

    private void ConfigureSanityFillImage()
    {
        if (sanityFillImage == null)
            return;

        sanityFillImage.type = Image.Type.Filled;
        sanityFillImage.fillMethod = Image.FillMethod.Horizontal;
        sanityFillImage.fillOrigin = 0;
    }

    private T FindChildComponentByName<T>(string targetName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        string normalizedTargetName = NormalizeName(targetName);

        for (int i = 0; i < components.Length; i++)
        {
            if (NormalizeName(components[i].name) == normalizedTargetName)
                return components[i];
        }

        return null;
    }

    private string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace(" ", string.Empty);
    }

    private void RefreshAll()
    {
        if (subscribedSanityManager != null)
        {
            UpdateSanityUI(
                subscribedSanityManager.CurrentSanity,
                subscribedSanityManager.MaxSanity
            );
        }

        if (subscribedDissociationManager != null)
            UpdateDissociationUI(subscribedDissociationManager.Current);
    }

    private void HandleDebugInput()
    {
        if (!enableDebugKeys)
            return;

        if (Input.GetKeyDown(KeyCode.P))
            subscribedDissociationManager?.ForceLevelUp("DebugKey_P");

        if (Input.GetKeyDown(KeyCode.M))
            subscribedDissociationManager?.TemporaryReduce(1);

        if (Input.GetKeyDown(KeyCode.Alpha1))
            subscribedSanityManager?.ApplyDamage(sanityDebugAmount);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            subscribedSanityManager?.Heal(sanityDebugAmount);
    }

    private void UpdateSanityUI(float current, float max)
    {
        float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        int percent = Mathf.RoundToInt(ratio * 100f);

        if (sanityText != null)
            sanityText.text = string.Format(sanityTextFormat, percent);

        if (sanityFillImage != null)
            sanityFillImage.fillAmount = ratio;
    }

    private void UpdateDissociationUI(DissociationLevelManager.LevelData levelData)
    {
        if (dissociationLevelImages == null)
            return;

        int currentLevel = Mathf.Clamp(levelData.level, MinDissociationLevel, MaxDissociationLevel);
        int visibleCount = useCurrentLevelAsVisibleCount ? currentLevel : Mathf.Max(0, currentLevel - 1);

        for (int i = 0; i < dissociationLevelImages.Length; i++)
        {
            if (dissociationLevelImages[i] != null)
                dissociationLevelImages[i].gameObject.SetActive(i < visibleCount);
        }
    }
}
