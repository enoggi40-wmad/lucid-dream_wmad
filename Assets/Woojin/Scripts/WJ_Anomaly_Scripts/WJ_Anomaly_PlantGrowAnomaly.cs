using UnityEngine;

public class WJ_Anomaly_PlantGrowAnomaly : MonoBehaviour
{
    [Header("크기 설정")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 targetScale = new Vector3(3f, 3f, 3f);

    [Header("성장 시간")]
    [SerializeField] private float growDuration = 10f;

    private float timer = 0f;

    private void Start()
    {
        transform.localScale = startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = timer / growDuration;
        t = Mathf.Clamp01(t);

        transform.localScale = Vector3.Lerp(startScale, targetScale, t);
    }
}