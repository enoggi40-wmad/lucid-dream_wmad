using TMPro;
using UnityEngine;

public class HospitalChartPurifiable : PurifiableObject
{
    [Header("Chart Text")]
    [SerializeField] private TMP_Text chartText;
    [SerializeField] private string abnormalText = "꿹저dsfa다ㅔ@$%뇨뷰엘@#!#";
    [SerializeField] private string normalText = "오늘 환자의 상태는 괜찮았다. 하지만 더 심해지지 않도록 주의를 해야겠다";

    private void Start()
    {
        if (chartText != null && !IsPurified)
        {
            chartText.text = abnormalText;
        }
    }

    protected override void OnPurifyProgressChanged(float progress01)
    {
        if (chartText == null || IsPurified)
            return;

        chartText.text = BuildMixedText(progress01);
    }

    protected override void OnPurifyComplete()
    {
        base.OnPurifyComplete();

        if (chartText != null)
        {
            chartText.text = normalText;
        }
    }

    private string BuildMixedText(float t)
    {
        int maxLength = Mathf.Max(abnormalText.Length, normalText.Length);
        char[] chars = new char[maxLength];

        for (int i = 0; i < maxLength; i++)
        {
            char abnormal = i < abnormalText.Length ? abnormalText[i] : ' ';
            char normal = i < normalText.Length ? normalText[i] : ' ';
            float threshold = maxLength <= 1 ? 1f : (float)i / (maxLength - 1);

            chars[i] = t >= threshold ? normal : abnormal;
        }

        return new string(chars).TrimEnd();
    }
}