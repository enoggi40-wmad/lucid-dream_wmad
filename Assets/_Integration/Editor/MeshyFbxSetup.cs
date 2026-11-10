#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// Meshy FBX(텍스처 내장형)를 Unity에서 올바르게 세팅한다.
    ///  1) 탄젠트 Calculate Mikktspace + 노멀 Import  (격자/체스판 음영 아티팩트 방지)
    ///  2) 내장 텍스처를 Textures/ 폴더로 추출
    ///  3) 노멀맵 텍스처를 Normal Map 타입으로 자동 지정 (평균색/이름 판정)
    ///  4) URP/Lit 머티리얼 생성 후 모델 머티리얼 슬롯에 리맵
    ///
    /// 메뉴: Tools ▸ Lucid Dream ▸ Meshy ▸ Setup Meshy FBX (Models folder)
    /// 또는 배치: -executeMethod LucidDream.Integration.EditorTools.MeshyFbxSetup.SetupAll
    /// </summary>
    public static class MeshyFbxSetup
    {
        const string ROOT = "Assets/_Integration/Anomalies/Models";

        [MenuItem("Tools/Lucid Dream/Meshy/Setup Meshy FBX (Models folder)")]
        public static void SetupAll()
        {
            var report = new System.Text.StringBuilder("[MeshyFbxSetup]\n");
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { ROOT }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                report.AppendLine(Setup(path));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(report.ToString());
        }

        static string Setup(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return fbxPath + " : ModelImporter 아님";

            // 0) 재실행 대비: 기존 머티리얼 리맵 제거 → 임베디드 머티리얼 복원
            foreach (var kv in importer.GetExternalObjectMap().ToList())
                if (kv.Key.type == typeof(Material))
                    importer.RemoveRemap(kv.Key);

            // 1) 메시 임포트 설정 (탄젠트 = 격자 아티팩트의 핵심)
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

            // 2) 내장 텍스처 추출
            string dir = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
            string texDir = dir + "/Textures";
            if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder(dir, "Textures");
            importer.ExtractTextures(texDir);
            AssetDatabase.Refresh();

            var texPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { texDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == texDir)
                .ToList();

            // 3) 분류: 노멀맵 = 파랑 우세, 베이스컬러 = 노멀 아닌 것 중 최고 해상도(메인 아틀라스)
            string normalP = null;
            foreach (var tp in texPaths)
            {
                string low = Path.GetFileNameWithoutExtension(tp).ToLowerInvariant();
                if (low.Contains("normal") || Classify(tp) == "normal") { normalP = tp; break; }
            }
            if (normalP == null)
                foreach (var tp in texPaths) if (Classify(tp) == "normal") { normalP = tp; break; }

            // 메탈릭/러프니스 맵을 base로 잘못 잡는 사고를 피하려고, base는 무조건 최고 해상도로 고른다.
            string baseP = texPaths.Where(p => p != normalP).OrderByDescending(Pixels).FirstOrDefault();
            SetNormalType(normalP);

            // 4) URP/Lit 머티리얼
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh) { name = Path.GetFileNameWithoutExtension(fbxPath) + "_Mat" };
            AssignTex(mat, "_BaseMap", baseP); AssignTex(mat, "_MainTex", baseP);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (normalP != null && mat.HasProperty("_BumpMap")) { AssignTex(mat, "_BumpMap", normalP); mat.EnableKeyword("_NORMALMAP"); }
            // 메탈릭 맵은 분류 신뢰도가 낮아 일부러 비운다(잘못 넣으면 새까매짐). 비금속·중간 거칠기로 둔다.
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            string matDir = dir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
            string matPath = matDir + "/" + mat.name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);

            // 5) 모델 머티리얼 슬롯 → URP 머티리얼로 리맵
            var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath).OfType<Material>().ToList();
            foreach (var em in reps)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), em.name), mat);
            importer.SaveAndReimport();

            return $"{Path.GetFileName(fbxPath)} : base={Name(baseP)} normal={Name(normalP)} slots={reps.Count}";
        }

        static long Pixels(string assetPath) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath); return t == null ? 0 : (long)t.width * t.height; }
        static long FileLen(string assetPath) { try { return new FileInfo(assetPath).Length; } catch { return 0; } }
        static string Name(string p) => p == null ? "-" : Path.GetFileName(p);

        static void AssignTex(Material m, string prop, string p)
        {
            if (p == null || !m.HasProperty(prop)) return;
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t != null) m.SetTexture(prop, t);
        }

        static void SetNormalType(string p)
        {
            if (p == null) return;
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        // 평균색으로 노멀맵/그레이스케일/컬러 판정
        static string Classify(string assetPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(assetPath);
                var t = new Texture2D(2, 2);
                if (!t.LoadImage(bytes)) { Object.DestroyImmediate(t); return "color"; }
                var px = t.GetPixels32();
                long r = 0, g = 0, b = 0; int n = 0; int step = Mathf.Max(1, px.Length / 4096);
                for (int i = 0; i < px.Length; i += step) { r += px[i].r; g += px[i].g; b += px[i].b; n++; }
                Object.DestroyImmediate(t);
                float ar = r / (float)n / 255f, ag = g / (float)n / 255f, ab = b / (float)n / 255f;
                bool gray = Mathf.Abs(ar - ag) < 0.06f && Mathf.Abs(ag - ab) < 0.06f;
                bool normalish = ab > 0.5f && ab > ar + 0.08f && ab > ag + 0.08f &&
                                 Mathf.Abs(ar - ag) < 0.18f && ar > 0.30f && ar < 0.70f;
                if (normalish) return "normal";
                if (gray) return "gray";
                return "color";
            }
            catch { return "color"; }
        }
    }
}
#endif
