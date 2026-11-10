using UnityEngine;

public class PurifiableObject : MonoBehaviour
{
    [Header("Purification Settings")]
    [SerializeField] private float requiredPurifyTime = 5f;

    [Header("State")]
    [SerializeField] private float currentPurifyTime = 0f;
    [SerializeField] private bool isPurified = false;

    [Header("Effects")]
    [SerializeField] private Transform effectPoint;
    [SerializeField] private GameObject purifyingEffectPrefab;
    [SerializeField] private GameObject successEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;
    [SerializeField] private AudioSource purifyingAudioSource;

    private GameObject activePurifyingEffectInstance;

    public float RequiredPurifyTime => requiredPurifyTime;
    public float CurrentPurifyTime => currentPurifyTime;
    public float Progress01 => requiredPurifyTime <= 0f ? 1f : Mathf.Clamp01(currentPurifyTime / requiredPurifyTime);
    public bool IsPurified => isPurified;
    public Transform EffectPoint => effectPoint != null ? effectPoint : transform;

    public virtual bool CanBePurified => !isPurified && gameObject.activeInHierarchy;

    public virtual void OnPurifyStart()
    {
        StartPurifyingEffect();

        if (purifyingAudioSource != null && !purifyingAudioSource.isPlaying)
        {
            purifyingAudioSource.Play();
        }
    }

    public virtual void OnPurifyStop()
    {
        StopPurifyingEffect();

        if (purifyingAudioSource != null && purifyingAudioSource.isPlaying)
        {
            purifyingAudioSource.Stop();
        }
    }

    public virtual void AddPurifyTime(float deltaTime)
    {
        if (!CanBePurified || deltaTime <= 0f)
            return;

        currentPurifyTime = Mathf.Clamp(currentPurifyTime + deltaTime, 0f, requiredPurifyTime);
        OnPurifyProgressChanged(Progress01);
    }

    public virtual void ResetPurifyProgress()
    {
        currentPurifyTime = 0f;
        OnPurifyProgressChanged(0f);
    }

    public virtual void CompletePurification()
    {
        if (isPurified)
            return;

        isPurified = true;
        currentPurifyTime = requiredPurifyTime;

        OnPurifyProgressChanged(1f);
        OnPurifyComplete();

        StopPurifyingEffect();

        if (successEffectPrefab != null)
        {
            Transform spawnPoint = effectSpawnPoint != null ? effectSpawnPoint : EffectPoint;
            Instantiate(successEffectPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        if (purifyingAudioSource != null && purifyingAudioSource.isPlaying)
        {
            purifyingAudioSource.Stop();
        }
    }

    protected virtual void OnPurifyProgressChanged(float progress01) { }

    protected virtual void OnPurifyComplete()
    {
        Debug.Log($"{gameObject.name} 정화 완료");
    }

    private void StartPurifyingEffect()
    {
        if (purifyingEffectPrefab == null)
            return;

        if (activePurifyingEffectInstance == null)
        {
            activePurifyingEffectInstance = Instantiate(
                purifyingEffectPrefab,
                EffectPoint.position,
                EffectPoint.rotation,
                EffectPoint
            );
        }
        else
        {
            activePurifyingEffectInstance.SetActive(true);
            activePurifyingEffectInstance.transform.SetPositionAndRotation(EffectPoint.position, EffectPoint.rotation);
        }
    }

    private void StopPurifyingEffect()
    {
        if (activePurifyingEffectInstance != null)
        {
            Destroy(activePurifyingEffectInstance);
            activePurifyingEffectInstance = null;
        }
    }
}