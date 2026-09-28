using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SpecialPlatformSetup
{
    private const string SessionKey = "SpecialPlatformSetup.Completed";
    private const string ScenePath = "Assets/Scenes/PlatformerLevel.unity";
    private const string MaterialFolder = "Assets/LevelObjects/Materials";
    private const string PrefabFolder = "Assets/LevelObjects/Prefabs";

    static SpecialPlatformSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Tools/Level Objects/Rebuild Special Platforms")]
    private static void RebuildFromMenu()
    {
        BuildAssetsAndScene();
    }

    public static void BuildForAutomation()
    {
        BuildAssetsAndScene();
    }

    private static void RunOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
            EditorApplication.delayCall += RunOnce;
            return;
        }

        SessionState.SetBool(SessionKey, true);

        try {
            BuildAssetsAndScene();
            Debug.Log("Special platform prefabs and PlatformerLevel examples created successfully.");
        }
        catch (Exception exception) {
            SessionState.SetBool(SessionKey, false);
            Debug.LogException(exception);
        }
    }

    private static void BuildAssetsAndScene()
    {
        EnsureFolder(PrefabFolder);

        Material jumpMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/JumpPlatform.mat",
            new Color(0.55f, 0.18f, 0.8f, 1f));
        Material slowMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/SlowPlatform.mat",
            new Color(0.85f, 0.12f, 0.12f, 1f));
        Material fastMaterial = CreateOrUpdateMaterial(
            MaterialFolder + "/FastPlatform.mat",
            new Color(1f, 0.75f, 0.05f, 1f));

        GameObject jumpPrefab = CreateOrUpdatePrefab(
            "JumpPlatform", jumpMaterial, GroundPlatformEffect.EffectType.Jump, 12f);
        GameObject slowPrefab = CreateOrUpdatePrefab(
            "SlowPlatform", slowMaterial, GroundPlatformEffect.EffectType.Slow, 0.5f);
        GameObject fastPrefab = CreateOrUpdatePrefab(
            "FastPlatform", fastMaterial, GroundPlatformEffect.EffectType.Fast, 1.75f);

        AddExamplesToLevel(jumpPrefab, slowPrefab, fastPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Material CreateOrUpdateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) {
                throw new InvalidOperationException("The Universal Render Pipeline/Lit shader was not found.");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = System.IO.Path.GetFileNameWithoutExtension(path);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateOrUpdatePrefab(
        string name,
        Material material,
        GroundPlatformEffect.EffectType effectType,
        float strength)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try {
            platform.name = name;
            platform.transform.localScale = new Vector3(3f, 0.5f, 3f);

            MeshRenderer renderer = platform.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            GroundPlatformEffect effect = platform.AddComponent<GroundPlatformEffect>();
            SerializedObject serializedEffect = new SerializedObject(effect);
            serializedEffect.FindProperty("effectType").enumValueIndex = (int)effectType;
            serializedEffect.FindProperty("strength").floatValue = strength;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(platform, path);
        }
        finally {
            UnityEngine.Object.DestroyImmediate(platform);
        }
    }

    private static void AddExamplesToLevel(
        GameObject jumpPrefab,
        GameObject slowPrefab,
        GameObject fastPrefab)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;

        if (openedForSetup) {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try {
            Transform platforms = FindTransform(scene, "Level", "Platforms");
            if (platforms == null) {
                throw new InvalidOperationException("Could not find Level/Platforms in PlatformerLevel.");
            }

            CreateOrUpdateExample(jumpPrefab, platforms, new Vector3(-3f, -0.25f, -4.5f));
            CreateOrUpdateExample(slowPrefab, platforms, new Vector3(0f, -0.25f, -4.5f));
            CreateOrUpdateExample(fastPrefab, platforms, new Vector3(3f, -0.25f, -4.5f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally {
            if (openedForSetup && scene.IsValid() && scene.isLoaded) {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void CreateOrUpdateExample(GameObject prefab, Transform parent, Vector3 localPosition)
    {
        Transform instance = parent.Find(prefab.name);
        if (instance == null) {
            GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            instance = created.transform;
            instance.SetParent(parent, false);
        }

        instance.name = prefab.name;
        instance.localPosition = localPosition;
        instance.localRotation = Quaternion.identity;
        instance.localScale = Vector3.one;
    }

    private static Transform FindTransform(Scene scene, string rootName, string childName)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) {
            if (root.name != rootName) {
                continue;
            }

            return root.transform.Find(childName);
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
