using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MapType1GuardSetup
{
    private const string MapType1Path = "Assets/Map/MapType1.unity";
    private const string GuardName = "\uACBD\uBE44\uC6D0";
    private const string FootprintGuid = "0bbd6cb558556844a848a3e1bb6933cf";

    static MapType1GuardSetup()
    {
        EditorApplication.delayCall += EnsureGuardSetup;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EnsureGuardSetup();
    }

    private static void EnsureGuardSetup()
    {
        if (Application.isPlaying)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != MapType1Path)
            return;

        GameObject guardObject = GameObject.Find(GuardName);
        if (guardObject == null)
            return;

        bool changed = false;

        changed |= RemoveRuntimeRoadBuilder(guardObject);

        GuardPatrolEntity guard = guardObject.GetComponent<GuardPatrolEntity>();
        if (guard == null)
        {
            guard = Undo.AddComponent<GuardPatrolEntity>(guardObject);
            changed = true;
        }

        changed |= RemoveMovingNavMeshSurface(guardObject);

        if (guardObject.GetComponent<NavMeshAgent>() == null)
        {
            NavMeshAgent agent = Undo.AddComponent<NavMeshAgent>(guardObject);
            agent.radius = 0.32f;
            agent.height = 1.65f;
            agent.speed = 1.2f;
            agent.angularSpeed = 240f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.6f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            changed = true;
        }

        Rigidbody rigidbody = guardObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(guardObject);
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            changed = true;
        }

        CapsuleCollider collider = guardObject.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<CapsuleCollider>(guardObject);
            collider.radius = 0.32f;
            collider.height = 1.65f;
            collider.center = new Vector3(0f, 0.82f, 0f);
            changed = true;
        }

        changed |= ConfigureExistingAgent(guardObject.GetComponent<NavMeshAgent>());
        changed |= ConfigureExistingCollider(collider);

        if (collider.isTrigger)
        {
            collider.isTrigger = false;
            changed = true;
        }

        changed |= ConfigureGuard(guard);

        if (changed)
        {
            EditorUtility.SetDirty(guardObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    private static bool RemoveRuntimeRoadBuilder(GameObject guardObject)
    {
        RuntimeRoadNavMeshBuilder builder = guardObject.GetComponent<RuntimeRoadNavMeshBuilder>();
        if (builder == null)
            return false;

        Undo.DestroyObjectImmediate(builder);
        return true;
    }

    private static bool ConfigureGuard(GuardPatrolEntity guard)
    {
        SerializedObject serializedGuard = new SerializedObject(guard);
        bool changed = false;

        changed |= SetStringArray(serializedGuard, "routeObjectNames", GetRouteObjectNames());
        changed |= SetObjectReference(serializedGuard, "footprintPrefab", LoadFootprintPrefab());
        changed |= SetFloat(serializedGuard, "footprintInterval", 1.3f);
        changed |= SetFloat(serializedGuard, "footprintLifetime", 10f);
        changed |= SetFloat(serializedGuard, "footprintPurifyTime", 2f);
        changed |= SetVector3(serializedGuard, "footprintEulerOffset", Vector3.zero);
        changed |= SetInt(serializedGuard, "purifyingLayer", LayerMask.NameToLayer("Purifying"));
        changed |= SetFloat(serializedGuard, "guardWaitAfterDrop", 3f);
        changed |= SetFloat(serializedGuard, "chaseDistance", 8f);
        changed |= SetFloat(serializedGuard, "catchDistance", 0.85f);
        changed |= SetFloat(serializedGuard, "loseSightDuration", 1.2f);
        changed |= SetFloat(serializedGuard, "sightHeight", 1.1f);
        changed |= SetFloat(serializedGuard, "moveSpeed", 1.2f);
        changed |= SetFloat(serializedGuard, "chaseSpeed", 1.7f);
        changed |= SetFloat(serializedGuard, "carryingSpeed", 1.8f);
        changed |= SetFloat(serializedGuard, "carryingSpeedMultiplier", 1.5f);
        changed |= SetFloat(serializedGuard, "carriedPlayerForwardOffset", 0.8f);
        changed |= SetFloat(serializedGuard, "randomPatrolRadius", 5f);
        changed |= SetFloat(serializedGuard, "wallCheckRadius", 0.18f);
        changed |= SetFloat(serializedGuard, "wallCheckDistance", 0.55f);
        changed |= SetFloat(serializedGuard, "stuckRecoverDelay", 1.4f);
        changed |= SetString(serializedGuard, "playerObjectName", "Player_Prefab");
        changed |= SetString(serializedGuard, "aiDestinationObjectName", "AIdestination");

        serializedGuard.ApplyModifiedProperties();
        return changed;
    }

    private static bool ConfigureExistingAgent(NavMeshAgent agent)
    {
        if (agent == null)
            return false;

        bool changed = false;

        if (!Mathf.Approximately(agent.radius, 0.32f))
        {
            agent.radius = 0.32f;
            changed = true;
        }

        if (!Mathf.Approximately(agent.height, 1.65f))
        {
            agent.height = 1.65f;
            changed = true;
        }

        if (!Mathf.Approximately(agent.stoppingDistance, 0.6f))
        {
            agent.stoppingDistance = 0.6f;
            changed = true;
        }

        if (agent.obstacleAvoidanceType != ObstacleAvoidanceType.HighQualityObstacleAvoidance)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            changed = true;
        }

        if (!agent.autoRepath)
        {
            agent.autoRepath = true;
            changed = true;
        }

        return changed;
    }

    private static bool ConfigureExistingCollider(CapsuleCollider collider)
    {
        if (collider == null)
            return false;

        bool changed = false;

        if (!Mathf.Approximately(collider.radius, 0.32f))
        {
            collider.radius = 0.32f;
            changed = true;
        }

        if (!Mathf.Approximately(collider.height, 1.65f))
        {
            collider.height = 1.65f;
            changed = true;
        }

        Vector3 center = new Vector3(0f, 0.82f, 0f);
        if (collider.center != center)
        {
            collider.center = center;
            changed = true;
        }

        if (collider.isTrigger)
        {
            collider.isTrigger = false;
            changed = true;
        }

        return changed;
    }

    private static bool RemoveMovingNavMeshSurface(GameObject guardObject)
    {
        Component surface = guardObject.GetComponent("NavMeshSurface");
        if (surface == null)
            return false;

        Undo.DestroyObjectImmediate(surface);
        return true;
    }

    private static string[] GetRouteObjectNames()
    {
        return new[]
        {
            "\uACFC\uD559\uC2E4 \uBC14\uB2E5 (1)",
            "\uD654\uC7A5\uC2E4 \uBC14\uB2E5",
            "\uBCF5\uB3C4",
            "\uACFC\uD559\uC2E4 \uBC14\uB2E5"
        };
    }

    private static GameObject LoadFootprintPrefab()
    {
        string footprintPath = AssetDatabase.GUIDToAssetPath(FootprintGuid);
        if (string.IsNullOrEmpty(footprintPath))
            return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(footprintPath);
    }

    private static bool SetStringArray(SerializedObject serializedObject, string propertyName, string[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
            return false;

        bool changed = property.arraySize != values.Length;
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element.stringValue != values[i])
            {
                element.stringValue = values[i];
                changed = true;
            }
        }

        return changed;
    }

    private static bool SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        return true;
    }

    private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
            return false;

        property.floatValue = value;
        return true;
    }

    private static bool SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.intValue == value)
            return false;

        property.intValue = value;
        return true;
    }

    private static bool SetVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.vector3Value == value)
            return false;

        property.vector3Value = value;
        return true;
    }

    private static bool SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.stringValue == value)
            return false;

        property.stringValue = value;
        return true;
    }

    private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.boolValue == value)
            return false;

        property.boolValue = value;
        return true;
    }
}
