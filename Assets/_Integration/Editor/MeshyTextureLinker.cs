#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LucidDream.Integration.EditorTools
{
    /// <summary>
    /// Meshy AI에서 받은 모델이 Unity에서 하얗게 나오는 문제를 자동 해결한다.
    /// FBX 옆에 딸려온 텍스처(_texture/_normal/_metallic/_roughness/_emission)를 찾아
    /// URP/Lit 머티리얼을 만들고, 모델의 머티리얼 슬롯을 그 머티리얼로 리맵한다.
    ///
    /// 사용법: Project 창에서 FBX(모델) 하나 이상 선택 →
    ///   Tools ▸ Lucid Dream ▸ Meshy ▸ Auto-Link Textures (Selected Models)
    /// </summary>
    public static class MeshyTextureLinker
    {
        [MenuItem("Tools/Lucid Dream/Meshy/Auto-Link Textures (Selected Models)")]
        private static void LinkSelected()
        {
            var models = Selection.objects
                .Select(AssetDatabase.GetAssetPath)
                .Where(p => !string.IsNullOrEmpty(p) &&
                            (p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                             p.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase)))
                .Distinct().ToList();

            if (models.Count == 0)
            {
                EditorUtility.DisplayDialog("Meshy Texture Linker",
                    "Project 창에서 FBX/OBJ 모델을 하나 이상 선택하세요.", "확인");
                return;
            }

            int ok = 0; var report = new System.Text.StringBuilder();
            foreach (var path in models)
            {
                string msg = LinkOne(path);
                report.AppendLine("• " + Path.GetFileName(path) + " : " + msg);
                if (msg.StartsWith("OK")) ok++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MeshyTextureLinker]\n" + report);
            EditorUtility.DisplayDialog("Meshy Texture Linker",
                $"{ok}/{models.Count} 모델 처리 완료.\n\n" + report, "확인");
        }

        private static string LinkOne(string fbxPath)
        {
            string dir = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
            string stem = Path.GetFileNameWithoutExtension(fbxPath);
            string prefix = stem.EndsWith("_texture") ? stem.Substring(0, stem.Length - 8) : stem;

            // 같은 폴더에서 이 모델에 속하는 텍스처 수집
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });
            string baseTexP = null, normalP = null, metalP = null, roughP = null, emisP = null;
            foreach (var g in texGuids)
            {
                string tp = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetDirectoryName(tp).Replace('\\', '/') != dir) continue;       // 직속 폴더만
                string fn = Path.GetFileNameWithoutExtension(tp);
                if (!fn.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                string low = fn.ToLowerInvariant();
                if (low.Contains("normal")) normalP = tp;
                else if (low.Contains("metallic")) metalP = tp;
                else if (low.Contains("rough")) roughP = tp;
                else if (low.Contains("emiss")) emisP = tp;
                else baseTexP = baseTexP ?? tp; // 접미사 없는 것 = 베이스 컬러
            }

            if (baseTexP == null && normalP == null && metalP == null)
                return "건너뜀 — 옆에 Meshy 텍스처(_texture/_normal/...)가 없음. (텍스처 같이 임포트했는지 확인, 또는 GLB로 받기)";

            // 노멀맵 텍스처 타입 보정
            EnsureNormalType(normalP);

            // URP/Lit 머티리얼 생성
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh) { name = prefix + "_Mat" };

            var baseTex = Load(baseTexP);
            if (baseTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            var nrm = Load(normalP);
            if (nrm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", nrm);
                mat.EnableKeyword("_NORMALMAP");
            }
            var met = Load(metalP);
            if (met != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", met);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);
            }
            var emis = Load(emisP);
            if (emis != null && mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", emis);
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // 머티리얼 에셋 저장
            string matPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + prefix + "_Mat.mat");
            AssetDatabase.CreateAsset(mat, matPath);

            // 모델의 머티리얼 슬롯을 새 머티리얼로 리맵
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return "실패 — ModelImporter 아님";

            var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            var embedded = reps.OfType<Material>().ToList();
            if (embedded.Count == 0)
            {
                // 임베디드 머티리얼이 없으면 외부 머티리얼 모드로 전환 후 재시도
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
                embedded = reps.OfType<Material>().ToList();
            }
            foreach (var em in embedded)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), em.name), mat);

            importer.SaveAndReimport();
            return $"OK — 머티리얼 연결(base:{(baseTex != null ? "Y" : "-")} normal:{(nrm != null ? "Y" : "-")} metal:{(met != null ? "Y" : "-")})";
        }

        private static Texture2D Load(string p) =>
            string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(p);

        private static void EnsureNormalType(string p)
        {
            if (string.IsNullOrEmpty(p)) return;
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }
    }
}
#endif
