#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LucidDream.Integration;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// 거울 이상현상(MirrorDelayAnomaly) 동작을 눈으로 확인하기 위한 테스트 씬을 자동 생성한다.
    /// 카메라 + 거울(Quad) + 반사상(Capsule) + 좌우로 움직이는 더미 플레이어(Capsule)를 배치하고,
    /// 불일치 간격을 짧게 세팅해 Play 즉시 효과가 보이도록 한다.
    /// 메뉴: Tools ▸ Lucid Dream ▸ Anomalies ▸ Build Mirror Test Scene
    /// (배치 모드: -executeMethod LucidDream.Integration.EditorTools.MirrorTestSceneBuilder.Build)
    /// </summary>
    public static class MirrorTestSceneBuilder
    {
        private const string ScenePath = "Assets/_Integration/Anomalies/MirrorTest.unity";

        [MenuItem("Tools/Lucid Dream/Anomalies/Build Mirror Test Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 카메라: 거울과 반사상을 정면으로 본다.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.2f, -3f);
                cam.transform.rotation = Quaternion.identity;
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // 거울 평면(Quad). 법선 = 로컬 +Z.
            var mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mirror.name = "TestMirror";
            mirror.transform.position = new Vector3(0f, 1.2f, 1.5f);
            mirror.transform.localScale = new Vector3(2.2f, 2.6f, 1f);
            // 거울 표면이 카메라(-Z)를 향하도록 뒤집어 둔다(시각용. 반사 계산엔 영향 없음).
            mirror.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Object.DestroyImmediate(mirror.GetComponent<Collider>()); // 정화 콜라이더는 테스트에 불필요

            // 반사상(거울 속 '나'). Play 시 MirrorDelayAnomaly가 위치/회전을 제어한다.
            var proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            proxy.name = "ReflectionProxy(거울속의나)";
            proxy.transform.position = new Vector3(0f, 1f, 3f);
            proxy.SetActive(false); // OnEnable에서 켜짐

            // 더미 플레이어(좌우 왕복). 'Player' 태그로 거울이 자동 추적.
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "TestPlayer";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -0.5f);
            player.AddComponent<MirrorTestMover>();

            // 거울에 이상현상 부착 + 참조 배선 + 테스트용 짧은 간격 세팅
            var anomaly = mirror.AddComponent<MirrorDelayAnomaly>();
            var so = new SerializedObject(anomaly);
            SetObj(so, "mirrorPlane", mirror.transform);
            SetVec(so, "planeNormalLocal", Vector3.forward);
            SetObj(so, "reflectionProxy", proxy.transform);
            SetFloat(so, "activateDistance", 50f);
            SetFloat(so, "mismatchInterval", 3f);   // 테스트용: 3초마다
            SetFloat(so, "mismatchDuration", 2.5f);
            SetBool(so, "moreFrequentWhenLowSanity", false);
            SetBool(so, "randomizeBehavior", true);
            // target은 비워두면 Play 시 'Player' 태그로 자동 탐색됨
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[MirrorTestSceneBuilder] 테스트 씬 생성 완료: " + ScenePath +
                      "  (에디터에서 이 씬 열고 Play ▶ 누르면 반사 동작 확인 가능)");
        }

        private static void SetObj(SerializedObject so, string name, Object v)
        {
            var p = so.FindProperty(name); if (p != null) p.objectReferenceValue = v;
        }
        private static void SetVec(SerializedObject so, string name, Vector3 v)
        {
            var p = so.FindProperty(name); if (p != null) p.vector3Value = v;
        }
        private static void SetFloat(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name); if (p != null) p.floatValue = v;
        }
        private static void SetBool(SerializedObject so, string name, bool v)
        {
            var p = so.FindProperty(name); if (p != null) p.boolValue = v;
        }
    }
}
#endif
