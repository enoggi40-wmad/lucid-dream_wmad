using UnityEngine;

public class LockerPhoneAnomaly : PurifiableObject
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Renderer phoneRenderer;
    [SerializeField] private Transform vibratingTransform;
    [SerializeField] private Light screenLight;

    [Header("Audio")]
    [SerializeField] private AudioSource ringtoneSource;
    [SerializeField] private AudioSource vibrationSource;
    [SerializeField, Min(0.1f)] private float ringtoneAudibleDistance = 18f;
    [SerializeField, Min(0.1f)] private float vibrationAudibleDistance = 4f;
    [SerializeField, Range(0f, 1f)] private float ringtoneVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float vibrationVolume = 0.35f;

    [Header("Phone Screen")]
    [SerializeField] private int screenMaterialIndex = 1;
    [SerializeField] private Color screenGlowColor = new Color(0.35f, 0.8f, 1f, 1f);
    [SerializeField, Min(0f)] private float minLightIntensity = 0.6f;
    [SerializeField, Min(0f)] private float maxLightIntensity = 2.2f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 3.5f;

    [Header("Locker Rattle")]
    [SerializeField, Min(0f)] private float rattleAmplitude = 0.015f;
    [SerializeField, Min(0.1f)] private float rattleFrequency = 22f;

    private const int SampleRate = 44100;

    private Material screenMaterialInstance;
    private Vector3 baseLocalPosition;
    private float purificationFade = 1f;

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;

        if (phoneRenderer == null)
            phoneRenderer = GetComponentInChildren<Renderer>();

        if (vibratingTransform == null)
            vibratingTransform = transform;

        baseLocalPosition = vibratingTransform.localPosition;

        SetupScreenMaterial();
        SetupLight();
        SetupAudio();
    }

    private void OnEnable()
    {
        purificationFade = 1f;
        PlayAnomalyAudio();
    }

    private void OnDisable()
    {
        StopAnomalyAudio();
    }

    private void Update()
    {
        if (IsPurified)
            return;

        float distance01 = GetDistance01();
        float close01 = GetClose01();

        UpdateAudio(distance01, close01);
        UpdateScreenGlow();
        UpdateRattle(close01);
    }

    protected override void OnPurifyProgressChanged(float progress01)
    {
        purificationFade = 1f - Mathf.Clamp01(progress01);
        UpdateScreenGlow();
        UpdateAudio(GetDistance01(), GetClose01());
    }

    protected override void OnPurifyComplete()
    {
        base.OnPurifyComplete();


        if (WJ_AnomalyManager.Instance != null)
            WJ_AnomalyManager.Instance.RemoveAnomaly(gameObject);


        purificationFade = 0f;
        StopAnomalyAudio();
        UpdateScreenGlow();
        ResetRattle();
    }

    private void SetupScreenMaterial()
    {
        if (phoneRenderer == null)
            return;

        Material[] materials = phoneRenderer.materials;

        if (materials == null || materials.Length == 0)
            return;

        int index = Mathf.Clamp(screenMaterialIndex, 0, materials.Length - 1);
        screenMaterialInstance = materials[index];

        if (screenMaterialInstance != null)
            screenMaterialInstance.EnableKeyword("_EMISSION");
    }

    private void SetupLight()
    {
        if (screenLight != null)
            return;

        GameObject lightObject = new GameObject("PhoneScreenLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.02f, -0.035f);
        screenLight = lightObject.AddComponent<Light>();
        screenLight.type = LightType.Point;
        screenLight.range = 1.5f;
        screenLight.color = screenGlowColor;
        screenLight.shadows = LightShadows.None;
    }

    private void SetupAudio()
    {
        ringtoneSource = SetupAudioSource(ringtoneSource, "RingtoneSource", ringtoneAudibleDistance, ringtoneVolume);
        vibrationSource = SetupAudioSource(vibrationSource, "VibrationSource", vibrationAudibleDistance, vibrationVolume);

        if (ringtoneSource != null)
            ringtoneSource.clip = CreateRingtoneClip();

        if (vibrationSource != null)
            vibrationSource.clip = CreateVibrationClip();
    }

    private AudioSource SetupAudioSource(AudioSource source, string objectName, float maxDistance, float volume)
    {
        if (source == null)
        {
            GameObject sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = maxDistance;
        source.volume = volume;

        return source;
    }

    private void PlayAnomalyAudio()
    {
        if (ringtoneSource != null && ringtoneSource.clip != null && !ringtoneSource.isPlaying)
            ringtoneSource.Play();

        if (vibrationSource != null && vibrationSource.clip != null && !vibrationSource.isPlaying)
            vibrationSource.Play();
    }

    private void StopAnomalyAudio()
    {
        if (ringtoneSource != null)
            ringtoneSource.Stop();

        if (vibrationSource != null)
            vibrationSource.Stop();
    }

    private void UpdateAudio(float distance01, float close01)
    {
        if (ringtoneSource != null)
            ringtoneSource.volume = ringtoneVolume * distance01 * purificationFade;

        if (vibrationSource != null)
            vibrationSource.volume = vibrationVolume * close01 * purificationFade;
    }

    private void UpdateScreenGlow()
    {
        float pulse01 = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        float intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, pulse01) * purificationFade;
        Color glow = screenGlowColor * intensity;

        if (screenMaterialInstance != null)
        {
            screenMaterialInstance.SetColor("_EmissionColor", glow);
            Color screenColor = Color.Lerp(Color.black, screenGlowColor, Mathf.Clamp01(intensity));

            if (screenMaterialInstance.HasProperty("_BaseColor"))
                screenMaterialInstance.SetColor("_BaseColor", screenColor);

            if (screenMaterialInstance.HasProperty("_Color"))
                screenMaterialInstance.SetColor("_Color", screenColor);
        }

        if (screenLight != null)
        {
            screenLight.enabled = intensity > 0.01f;
            screenLight.color = screenGlowColor;
            screenLight.intensity = intensity;
        }
    }

    private void UpdateRattle(float close01)
    {
        if (vibratingTransform == null)
            return;

        float amount = close01 * purificationFade;
        Vector3 offset = new Vector3(
            Mathf.Sin(Time.time * rattleFrequency) * rattleAmplitude * amount,
            Mathf.Sin(Time.time * rattleFrequency * 1.37f) * rattleAmplitude * 0.5f * amount,
            0f
        );

        vibratingTransform.localPosition = baseLocalPosition + offset;
    }

    private void ResetRattle()
    {
        if (vibratingTransform != null)
            vibratingTransform.localPosition = baseLocalPosition;
    }

    private float GetDistance01()
    {
        if (playerCamera == null)
            return 1f;

        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        return Mathf.Clamp01(1f - distance / ringtoneAudibleDistance);
    }

    private float GetClose01()
    {
        if (playerCamera == null)
            return 0f;

        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        return Mathf.Clamp01(1f - distance / vibrationAudibleDistance);
    }

    private static AudioClip CreateRingtoneClip()
    {
        float duration = 2.4f;
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float ringGate = Mathf.Repeat(time, 0.8f) < 0.46f ? 1f : 0f;
            float toneA = Mathf.Sin(2f * Mathf.PI * 880f * time);
            float toneB = Mathf.Sin(2f * Mathf.PI * 1175f * time);
            samples[i] = (toneA * 0.55f + toneB * 0.25f) * ringGate * 0.22f;
        }

        AudioClip clip = AudioClip.Create("GeneratedLockerRingtone", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateVibrationClip()
    {
        float duration = 0.35f;
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float buzz = Mathf.Sin(2f * Mathf.PI * 58f * time);
            float grit = Mathf.Sin(2f * Mathf.PI * 121f * time) * 0.35f;
            samples[i] = (buzz + grit) * 0.18f;
        }

        AudioClip clip = AudioClip.Create("GeneratedLockerVibration", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
