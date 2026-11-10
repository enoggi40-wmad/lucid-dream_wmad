using UnityEngine;

namespace LucidDream.Core
{
    /// <summary>
    /// 가장 최근의 정화 지점(부활 지점)을 저장하는 정적 레지스트리.
    /// - PurificationPoint가 정화될 때 SetLastCheckpoint 호출
    /// - GameManager가 부활 처리에서 GetLastCheckpoint 사용
    /// </summary>
    public static class CheckpointRegistry
    {
        private static Vector3 lastCheckpoint = Vector3.zero;
        private static bool hasCheckpoint = false;

        public static void SetLastCheckpoint(Vector3 pos)
        {
            lastCheckpoint = pos;
            hasCheckpoint = true;
        }

        public static Vector3 GetLastCheckpoint() => lastCheckpoint;

        public static bool HasCheckpoint => hasCheckpoint;

        public static void Reset()
        {
            lastCheckpoint = Vector3.zero;
            hasCheckpoint = false;
        }
    }
}