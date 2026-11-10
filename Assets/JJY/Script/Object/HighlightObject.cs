using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class HighlightObject : MonoBehaviour
{
    [Header("Legacy highlight object")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private bool toggleLegacyHighlightObject = false;

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color outlineColor = new Color(1f, 0.78f, 0.08f, 0.9f);
    [SerializeField, Min(0f)] private float outlineWidth = 0.035f;

    private const string OutlineColorProperty = "_OutlineColor";
    private const string OutlineWidthProperty = "_OutlineWidth";

    private readonly List<Renderer> outlineRenderers = new List<Renderer>();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        BuildOutlineRenderers();
        SetGeneratedOutlineVisible(false);
    }

    public void SetHighlighted(bool value)
    {
        if (outlineRenderers.Count > 0)
        {
            UpdateOutlineProperties();
            SetGeneratedOutlineVisible(value);
            return;
        }

        if (toggleLegacyHighlightObject && highlightObject != null && highlightObject != gameObject)
            highlightObject.SetActive(value);
    }

    private void BuildOutlineRenderers()
    {
        if (outlineMaterial == null)
            return;

        Renderer[] renderers = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer source = renderers[i];

            if (source == null || IsLegacyHighlightRenderer(source) || IsGeneratedOutlineRenderer(source))
                continue;

            Renderer outlineRenderer = CreateOutlineRenderer(source);

            if (outlineRenderer == null)
                continue;

            outlineRenderer.gameObject.SetActive(false);
            outlineRenderers.Add(outlineRenderer);
        }

        UpdateOutlineProperties();
    }

    private Renderer CreateOutlineRenderer(Renderer source)
    {
        if (source is SkinnedMeshRenderer skinnedMeshRenderer)
            return CreateSkinnedOutlineRenderer(skinnedMeshRenderer);

        if (source is MeshRenderer meshRenderer)
            return CreateMeshOutlineRenderer(meshRenderer);

        return null;
    }

    private Renderer CreateMeshOutlineRenderer(MeshRenderer source)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();

        if (sourceFilter == null || sourceFilter.sharedMesh == null)
            return null;

        GameObject outlineObject = CreateOutlineObject(source.transform);
        MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();

        outlineFilter.sharedMesh = sourceFilter.sharedMesh;
        ConfigureOutlineRenderer(outlineRenderer);

        return outlineRenderer;
    }

    private Renderer CreateSkinnedOutlineRenderer(SkinnedMeshRenderer source)
    {
        if (source.sharedMesh == null)
            return null;

        GameObject outlineObject = CreateOutlineObject(source.transform);
        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();

        outlineRenderer.sharedMesh = source.sharedMesh;
        outlineRenderer.rootBone = source.rootBone;
        outlineRenderer.bones = source.bones;
        outlineRenderer.localBounds = source.localBounds;
        ConfigureOutlineRenderer(outlineRenderer);

        return outlineRenderer;
    }

    private GameObject CreateOutlineObject(Transform sourceTransform)
    {
        GameObject outlineObject = new GameObject(sourceTransform.name + "_GeneratedOutline");
        Transform outlineTransform = outlineObject.transform;

        outlineTransform.SetParent(sourceTransform, false);
        outlineTransform.localPosition = Vector3.zero;
        outlineTransform.localRotation = Quaternion.identity;
        outlineTransform.localScale = Vector3.one;

        return outlineObject;
    }

    private void ConfigureOutlineRenderer(Renderer outlineRenderer)
    {
        outlineRenderer.sharedMaterial = outlineMaterial;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private bool IsLegacyHighlightRenderer(Renderer renderer)
    {
        if (highlightObject == gameObject)
            return false;

        return highlightObject != null && renderer.transform.IsChildOf(highlightObject.transform);
    }

    private bool IsGeneratedOutlineRenderer(Renderer renderer)
    {
        return renderer.gameObject.name.EndsWith("_GeneratedOutline");
    }

    private void SetGeneratedOutlineVisible(bool value)
    {
        for (int i = 0; i < outlineRenderers.Count; i++)
        {
            if (outlineRenderers[i] != null)
                outlineRenderers[i].gameObject.SetActive(value);
        }
    }

    private void UpdateOutlineProperties()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        propertyBlock.SetColor(OutlineColorProperty, outlineColor);
        propertyBlock.SetFloat(OutlineWidthProperty, outlineWidth);

        for (int i = 0; i < outlineRenderers.Count; i++)
        {
            if (outlineRenderers[i] != null)
                outlineRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
