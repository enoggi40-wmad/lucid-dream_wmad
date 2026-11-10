#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LucidDream.Integration;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// 통합 컴포넌트를 씬에 안전하게 붙여주는 에디터 도구 모음.
    /// 씬 파일(.unity YAML)을 직접 손대지 않고, 에디터 메뉴 클릭으로 컴포넌트를 추가한다.
    /// (이 스크립트는 Editor 폴더에 있어 빌드에는 포함되지 않는다.)
    /// </summary>
    public static class AnomalyIntegrationTools
    {
        private const string AnomalyTypePrefix = "WJ_Anomaly"; // Woojin 이상현상 스크립트 접두사

        /// <summary>
        /// 현재 열린 씬에서 Woojin 이상현상 스크립트가 붙은 오브젝트를 모두 찾아
        /// AnomalyThreat(Sanity 침식/정화 연동) 컴포넌트를 자동으로 추가한다.
        /// </summary>
        [MenuItem("Tools/Lucid Dream/Wire Anomalies In Open Scene")]
        private static void WireAnomaliesInScene()
        {
            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // 이상현상 스크립트가 붙은 GameObject(중복 제거)
            var anomalyObjects = allBehaviours
                .Where(mb => mb != null && mb.GetType().Name.StartsWith(AnomalyTypePrefix))
                .Select(mb => mb.gameObject)
                .Distinct()
                .ToList();

            if (anomalyObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Lucid Dream",
                    "열린 씬에서 'WJ_Anomaly*' 이상현상 스크립트를 찾지 못했습니다.", "확인");
                return;
            }

            int added = 0;
            var noCollider = new StringBuilder();

            foreach (var go in anomalyObjects)
            {
                if (go.GetComponent<AnomalyThreat>() == null)
                {
                    Undo.AddComponent<AnomalyThreat>(go);
                    added++;
                }

                // 정화 빔(PurifyController)은 Collider를 레이캐스트로 맞춘다. 없으면 알려준다.
                if (go.GetComponentInChildren<Collider>() == null)
                    noCollider.AppendLine(" - " + go.name);
            }

            if (added > 0)
                EditorSceneManager.MarkSceneDirty(anomalyObjects[0].scene);

            string msg = $"이상현상 오브젝트 {anomalyObjects.Count}개 처리, AnomalyThreat {added}개 추가됨.";
            if (noCollider.Length > 0)
                msg += "\n\n[주의] 아래 오브젝트는 Collider가 없어 정화 빔이 맞지 않습니다(필요 시 추가하세요):\n" + noCollider;

            Debug.Log("[AnomalyIntegrationTools] " + msg.Replace("\n", " "));
            EditorUtility.DisplayDialog("Lucid Dream - Wire Anomalies", msg, "확인");
        }

        /// <summary>
        /// 선택한 오브젝트(들)를 "정화 가능한 이상현상"으로 만든다.
        /// AnomalyThreat + AnomalyPurifiable 를 추가하고, Collider가 없으면 BoxCollider를 추가한다.
        /// </summary>
        [MenuItem("Tools/Lucid Dream/Make Selected Purifiable Anomaly")]
        private static void MakeSelectedPurifiable()
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("Lucid Dream",
                    "Hierarchy에서 이상현상 오브젝트를 먼저 선택하세요.", "확인");
                return;
            }

            int processed = 0;
            foreach (var go in selection)
            {
                if (go.GetComponent<AnomalyThreat>() == null)
                    Undo.AddComponent<AnomalyThreat>(go);

                if (go.GetComponent<PurifiableObject>() == null)
                    Undo.AddComponent<AnomalyPurifiable>(go);

                if (go.GetComponentInChildren<Collider>() == null)
                    Undo.AddComponent<BoxCollider>(go);

                processed++;
            }

            EditorSceneManager.MarkSceneDirty(selection[0].scene);
            string msg = $"선택한 {processed}개 오브젝트를 정화 가능한 이상현상으로 설정했습니다.";
            Debug.Log("[AnomalyIntegrationTools] " + msg);
            EditorUtility.DisplayDialog("Lucid Dream - Make Purifiable", msg, "확인");
        }

        [MenuItem("Tools/Lucid Dream/Make Selected Purifiable Anomaly", validate = true)]
        private static bool MakeSelectedPurifiableValidate() => Selection.gameObjects.Length > 0;
    }
}
#endif
