namespace LucidDream.Core
{
    /// <summary>
    /// 이상현상 난이도 등급. 하(Easy) / 중(Medium) / 상(Hard).
    ///
    /// 해리성 단계에 따라 "활성 등급"이 정해진다:
    ///  - 단계가 높을수록(해리 심화) Easy(하) 등급이 등장하고 정신력 소모가 빨라진다.
    ///  - 단계가 낮을수록(자각 상태) Hard(상) 등급이 등장하고 정신력 소모가 느려진다.
    /// </summary>
    public enum AnomalyDifficulty
    {
        Easy,    // 하 - 칠판 낙서 등 (해리성 단계가 높을 때 등장)
        Medium,  // 중 - 사물함 휴대폰 등
        Hard,    // 상 - 배회체 등 (해리성 단계가 낮을 때 등장)
    }
}
