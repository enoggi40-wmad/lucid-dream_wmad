#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LucidDream.Integration;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// 정신력 회복 관련 오브젝트를 씬에 빠르게 배치하는 에디터 메뉴.
    /// Tools ▸ Lucid Dream ▸ Sanity ▸ ...
    /// </summary>
    public static class SanityToolsExtra
    {
        [MenuItem("Tools/Lucid Dream/Sanity/Create Recovery Zone")]
        public static void CreateRecoveryZone()
        {
            var go = new GameObject("RecoveryZone");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 3f, 4f);
            go.AddComponent<SanityRecoveryZone>();

            var view = SceneView.lastActiveSceneView;
            if (view != null) go.transform.position = view.pivot;

            Undo.RegisterCreatedObjectUndo(go, "Create Recovery Zone");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        [MenuItem("Tools/Lucid Dream/Sanity/Create Stage Recovery Manager")]
        public static void CreateStageRecoveryManager()
        {
            if (Object.FindAnyObjectByType<StageTransitionSanity>() != null)
            {
                EditorUtility.DisplayDialog("Stage Recovery Manager",
                    "씬에 StageTransitionSanity가 이미 존재합니다.", "OK");
                return;
            }
            var go = new GameObject("StageTransitionSanity");
            go.AddComponent<StageTransitionSanity>();
            Undo.RegisterCreatedObjectUndo(go, "Create Stage Recovery Manager");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        /// <summary>
        /// 재사용 프리팹을 _Integration/Prefabs/에 생성한다.
        /// 메뉴 또는 배치 모드(-executeMethod ...SanityToolsExtra.BuildSanityPrefabs)에서 호출.
        /// 씬을 건드리지 않으므로 팀원 작업과 충돌하지 않는다.
        /// </summary>
        [MenuItem("Tools/Lucid Dream/Sanity/Build Sanity Prefabs")]
        public static void BuildSanityPrefabs()
        {
            const string parent = "Assets/_Integration";
            const string dir = parent + "/Prefabs";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(parent, "Prefabs");

            // 회복존 프리팹
            var zone = new GameObject("RecoveryZone");
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 3f, 4f);
            zone.AddComponent<SanityRecoveryZone>();
            PrefabUtility.SaveAsPrefabAsset(zone, dir + "/RecoveryZone.prefab");
            Object.DestroyImmediate(zone);
            Debug.Log("[SanityTools] Created prefab: " + dir + "/RecoveryZone.prefab");

            // 스테이지 전환 회복 부트스트랩 프리팹
            var stage = new GameObject("StageTransitionSanity");
            stage.AddComponent<StageTransitionSanity>();
            PrefabUtility.SaveAsPrefabAsset(stage, dir + "/StageTransitionSanity.prefab");
            Object.DestroyImmediate(stage);
            Debug.Log("[SanityTools] Created prefab: " + dir + "/StageTransitionSanity.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SanityTools] BuildSanityPrefabs done.");
        }
    }
}
#endif
