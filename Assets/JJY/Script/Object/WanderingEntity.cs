using UnityEngine;

public class WanderingEntity : MonoBehaviour
{
    [Header("Deprecated")]
    [SerializeField] private bool hideThisObject = true;

    private void Awake()
    {
        if (hideThisObject)
            HideLegacyObject();

        enabled = false;
    }

    private void HideLegacyObject()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
