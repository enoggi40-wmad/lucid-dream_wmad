using UnityEngine;

namespace LucidDream.Integration
{
    /// <summary>
    /// 테스트 전용: 거울 반사 동작을 눈으로 확인하기 위해 좌우로 왕복 이동하는 더미 플레이어.
    /// MirrorTest 씬에서 'Player' 태그 오브젝트에 붙는다. (실제 게임 로직과 무관)
    /// </summary>
    public class MirrorTestMover : MonoBehaviour
    {
        [SerializeField] private float range = 1.8f;   // 좌우 이동 폭(m)
        [SerializeField] private float speed = 1.4f;   // 왕복 속도
        [SerializeField] private float turnDeg = 35f;  // 좌우로 몸을 트는 각도

        private Vector3 start;

        private void Start() => start = transform.position;

        private void Update()
        {
            float s = Mathf.Sin(Time.time * speed);
            transform.position = start + new Vector3(s * range, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, s * turnDeg, 0f);
        }
    }
}
