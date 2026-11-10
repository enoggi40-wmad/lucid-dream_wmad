#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LucidDream.Core;
using LucidDream.Integration;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// 신규 이상현상(거꾸로 시계 / 책상 위의 손 / 거울 속 지연)을 선택 오브젝트에 한 번에 배선한다.
    /// 연출 컴포넌트 + AnomalyThreat(Sanity·단계 연동) + AnomalyPurifiable(정화 연동) + Collider 추가,
    /// 난이도 등급까지 설정한다. (Editor 전용, 빌드 미포함)
    /// </summary>
    public static class NewAnomalyTools
    {
        [MenuItem("Tools/Lucid Dream/Anomalies/Make Reversed Clock (selected)")]
        private static void MakeClock() => Apply<ReversedClockAnomaly>("거꾸로 걸린 시계", AnomalyDifficulty.Medium);

        [MenuItem("Tools/Lucid Dream/Anomalies/Make Desk Hand (selected)")]
        private static void MakeHand() => Apply<DeskHandAnomaly>("책상 위의 손", AnomalyDifficulty.Hard);

        [MenuItem("Tools/Lucid Dream/Anomalies/Make Mirror Delay (selected)")]
        private static void MakeMirror() => Apply<MirrorDelayAnomaly>("거울 속 반사(다른 행동)", AnomalyDifficulty.Hard);

        private static void Apply<T>(string label, AnomalyDifficulty diff) where T : MonoBehaviour
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                EditorUtility.DisplayDialog("Lucid Dream",
                    "Hierarchy에서 이상현상으로 만들 오브젝트를 먼저 선택하세요.", "확인");
                return;
            }

            int n = 0;
            foreach (var go in sel)
            {
                if (go.GetComponent<T>() == null) Undo.AddComponent<T>(go);

                var threat = go.GetComponent<AnomalyThreat>();
                if (threat == null) threat = Undo.AddComponent<AnomalyThreat>(go);
                SetDifficulty(threat, diff);

                if (go.GetComponent<PurifiableObject>() == null)
                    Undo.AddComponent<AnomalyPurifiable>(go);

                if (go.GetComponentInChildren<Collider>() == null)
                    Undo.AddComponent<BoxCollider>(go);

                n++;
            }

            EditorSceneManager.MarkSceneDirty(sel[0].scene);
            string msg = $"선택한 {n}개 오브젝트를 '{label}' 이상현상으로 배선했습니다. (난이도: {diff})";
            Debug.Log("[NewAnomalyTools] " + msg);
            EditorUtility.DisplayDialog("Lucid Dream - Anomaly", msg, "확인");
        }

        private static void SetDifficulty(AnomalyThreat threat, AnomalyDifficulty diff)
        {
            var so = new SerializedObject(threat);
            var p = so.FindProperty("difficulty");
            if (p != null)
            {
                p.enumValueIndex = (int)diff; // Easy=0, Medium=1, Hard=2
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif
