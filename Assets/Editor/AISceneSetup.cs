using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds the AI customer objects to the open scene: Shopkeeper > Set Up AI Customers
[InitializeOnLoad]
public static class AISceneSetup
{
    // If this file exists after a recompile, setup runs once and enters Play mode (used for one-off automation)
    private const string AutoRunMarker = "Temp/ai_setup_request";

    static AISceneSetup()
    {
        if (!File.Exists(AutoRunMarker)) return;
        File.Delete(AutoRunMarker);
        EditorApplication.delayCall += () =>
        {
            SetUp();
            EditorApplication.isPlaying = true;
        };
    }

    [MenuItem("Shopkeeper/Set Up AI Customers")]
    public static void SetUp()
    {
        if (Object.FindAnyObjectByType<AIServerClient>() == null)
        {
            var ai = new GameObject("AI Systems");
            ai.AddComponent<AIServerClient>();
            ai.AddComponent<ShopManager>();
            ai.AddComponent<PriceSelectUI>();
            Undo.RegisterCreatedObjectUndo(ai, "Add AI Systems");
        }

        if (Object.FindAnyObjectByType<CustomerSpawner>() == null)
        {
            // Door at the bottom-centre of the shop, customers stand between the shelves
            var spawner = new GameObject("Customer Spawner");
            spawner.transform.position = new Vector3(0f, -4.5f, 0f);
            var cs = spawner.AddComponent<CustomerSpawner>();
            var so = new SerializedObject(cs);
            so.FindProperty("standAreaCenter").vector2Value = new Vector2(0f, 6f);
            so.FindProperty("standAreaSize").vector2Value = new Vector2(6f, 2f);
            so.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(spawner, "Add Customer Spawner");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AI] AI customer objects added. Save the scene (Cmd+S) to keep them.");
    }
}
