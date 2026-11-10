using UnityEngine;

public class WJ_Anomaly_FloatingObject : MonoBehaviour
{
    [Header("떠오르는 설정")]
    [SerializeField] private float floatHeight = 2f;
    [SerializeField] private float riseDuration = 3f;

    [Header("둥둥 효과 설정")]
    [SerializeField] private float bobAmount = 0.2f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("자전 설정")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 60f, 0f);

    private Vector3 startPosition;
    private Vector3 floatPosition;

    private float timer = 0f;
    private bool isRising = true;

    private void Start()
    {
        startPosition = transform.position;
        floatPosition = startPosition + Vector3.up * floatHeight;
    }

    private void Update()
    {
        if (isRising)
        {
            RiseUp();
        }
        else
        {
            BobUpAndDown();
        }

        RotateSelf();
    }

    private void RiseUp()
    {
        timer += Time.deltaTime;

        float t = timer / riseDuration;
        t = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(startPosition, floatPosition, t);

        if (t >= 1f)
        {
            isRising = false;
        }
    }

    private void BobUpAndDown()
    {
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = floatPosition + new Vector3(0f, yOffset, 0f);
    }

    private void RotateSelf()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}