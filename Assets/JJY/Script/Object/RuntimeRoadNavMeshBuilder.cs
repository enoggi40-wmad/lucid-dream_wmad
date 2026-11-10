using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-200)]
public class RuntimeRoadNavMeshBuilder : MonoBehaviour
{
    private const string SurfaceObjectName = "JJY_MapType1RoadNavMesh";

    [SerializeField] private string roadLayerName = "road";
    [SerializeField] private string[] roadObjectNames =
    {
        "\uACFC\uD559\uC2E4 \uBC14\uB2E5 (1)",
        "\uD654\uC7A5\uC2E4 \uBC14\uB2E5",
        "\uBCF5\uB3C4",
        "\uACFC\uD559\uC2E4 \uBC14\uB2E5"
    };
    [SerializeField] private NavMeshCollectGeometry useGeometry = NavMeshCollectGeometry.RenderMeshes;
    [SerializeField, Min(0f)] private float minRegionArea = 0.5f;

    private void Awake()
    {
        RemoveMovingSurface();
    }

    public void BuildRoadNavMesh()
    {
        int roadLayer = LayerMask.NameToLayer(roadLayerName);

        if (roadLayer < 0)
        {
            Debug.LogWarning($"Layer '{roadLayerName}' does not exist. Road NavMesh was not built.", this);
            return;
        }

        AssignRoadLayer(roadLayer);

        NavMeshSurface surface = ResolveSurface();

        if (surface == null)
            return;

        surface.collectObjects = CollectObjects.All;
        surface.layerMask = 1 << roadLayer;
        surface.useGeometry = useGeometry;
        surface.minRegionArea = minRegionArea;
        surface.BuildNavMesh();
    }

    private NavMeshSurface ResolveSurface()
    {
        GameObject surfaceObject = GameObject.Find(SurfaceObjectName);

        if (surfaceObject == null)
            surfaceObject = new GameObject(SurfaceObjectName);

        surfaceObject.transform.position = Vector3.zero;
        surfaceObject.transform.rotation = Quaternion.identity;
        surfaceObject.transform.localScale = Vector3.one;

        NavMeshSurface resolvedSurface = surfaceObject.GetComponent<NavMeshSurface>();

        if (resolvedSurface == null)
            resolvedSurface = surfaceObject.AddComponent<NavMeshSurface>();

        return resolvedSurface;
    }

    private void RemoveMovingSurface()
    {
        NavMeshSurface movingSurface = GetComponent<NavMeshSurface>();

        if (movingSurface != null)
            Destroy(movingSurface);
    }

    private void AssignRoadLayer(int roadLayer)
    {
        for (int i = 0; i < roadObjectNames.Length; i++)
        {
            if (string.IsNullOrEmpty(roadObjectNames[i]))
                continue;

            GameObject roadObject = GameObject.Find(roadObjectNames[i]);

            if (roadObject != null)
                SetLayerRecursively(roadObject.transform, roadLayer);
        }
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        if (target == null)
            return;

        target.gameObject.layer = layer;

        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursively(target.GetChild(i), layer);
        }
    }
}
