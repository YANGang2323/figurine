using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Explicit authoring operation: never modifies hierarchy on entering Play Mode.
public static class SceneHierarchyAuthoring
{
    [MenuItem("Figurine/Prepare Saved Scene Hierarchies")]
    public static void PrepareAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Stop Play Mode before editing scene hierarchies.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scene" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var existing = SceneManager.GetSceneByPath(path);
                bool alreadyOpen = existing.IsValid() && existing.isLoaded;
                var scene = alreadyOpen ? existing : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                Prepare(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    public static void Prepare(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var cameras = roots.SelectMany(r => r.GetComponentsInChildren<CameraPivotRotator>(true)).ToArray();
        if ((cameras.Length > 0 || roots.Any(r => r.GetComponentInChildren<Game>(true) != null)) &&
            !roots.Any(r => r.GetComponentInChildren<DataManager>(true) != null))
            new GameObject("DataManager", typeof(DataManager));
        if (scene.name == "Stage1" || scene.name == "Stage2")
        {
            var first = MapRoot(scene, scene.name == "Stage1" ? "MapRoot" : "MapRoot First");
            var second = scene.name == "Stage2" ? MapRoot(scene, "MapRoot Second") : null;
            // Keep world coordinates intact, including prefab instances and collider boundaries.
            foreach (var root in roots)
            {
                if (root == first.gameObject || root == second?.gameObject || IsInfrastructure(root)) continue;
                if (scene.name == "Stage2" && root.name == "Unvisible Wall")
                {
                    foreach (var child in root.transform.Cast<Transform>().ToArray())
                        ParentGeometry(child, first, second);
                }
                else ParentGeometry(root.transform, first, second);
            }
            if (second != null)
            {
                foreach (var entrance in scene.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Stage2IntroTrigger>(true)).ToArray())
                    Undo.SetTransformParent(entrance.transform, first, "Author corridor entrance");
            }
            foreach (var camera in cameras)
            {
                if (camera.stage3CameraMode || camera.player == null) continue;
                Set(camera, "mapRoot", second == null || camera.player.position.x > 150f ? first : second);
            }
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/MapoPeacefull/MapoPeacefull SDF.asset");
        foreach (var camera in cameras)
        {
            var serialized = new SerializedObject(camera);
            if (serialized.FindProperty("tutorialHint").objectReferenceValue != null) continue;
            var hint = CanvasRoot("View Tutorial Hint - " + camera.name, 100);
            var label = Label(hint.transform, "Q와 E 키로 시야를 전환해 보세요", new Vector2(0, -60), new Vector2(750, 70), font);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
            Set(camera, "tutorialHint", hint);
            Set(camera, "tutorialHintLabel", label);
            hint.SetActive(false);
        }

        if (roots.Any(r => r.GetComponentInChildren<Game>(true) != null) &&
            !scene.GetRootGameObjects().Any(r => r.GetComponent<StageSavePrompt>() != null))
        {
            var prompt = new GameObject("Stage Save Prompt", typeof(StageSavePrompt));
            prompt.SetActive(false);
        }

        foreach (var menu in roots.SelectMany(r => r.GetComponentsInChildren<MainMenuManager>(true)))
        {
            var serialized = new SerializedObject(menu);
            if (serialized.FindProperty("exitPopup").objectReferenceValue != null) continue;
            var popup = ExitFeedbackPopup.BuildForEditor(
                serialized.FindProperty("exitFont").objectReferenceValue as TMP_FontAsset,
                serialized.FindProperty("exitTextBox").objectReferenceValue as Sprite,
                serialized.FindProperty("exitSurveyQr").objectReferenceValue as Sprite);
            Set(menu, "exitPopup", popup);
            popup.gameObject.SetActive(false);
        }
        DialogueChoiceAuthoring.ConfigureScene(scene);
    }

    private static bool IsInfrastructure(GameObject root)
    {
        if (root.name.StartsWith("MapRoot")) return true;
        if (root.GetComponentInChildren<PlayerMovement>(true) != null ||
            root.GetComponentInChildren<Camera>(true) != null ||
            root.GetComponentInChildren<Canvas>(true) != null) return true;
        // Global managers and input/UI systems must stay outside the rotating map.
        return root.GetComponentInChildren<Renderer>(true) == null &&
               root.GetComponentInChildren<Collider>(true) == null &&
               root.GetComponentsInChildren<MonoBehaviour>(true).Length > 0;
    }

    private static Transform MapRoot(Scene scene, string name)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
        if (existing != null) return existing.transform;
        var root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Author map root");
        return root.transform;
    }

    private static void ParentGeometry(Transform item, Transform first, Transform second)
    {
        var renderer = item.GetComponentInChildren<Renderer>(true);
        var collider = item.GetComponentInChildren<Collider>(true);
        var location = renderer != null ? renderer.bounds.center : collider != null ? collider.transform.position : item.position;
        Undo.SetTransformParent(item, second == null || location.x > 150f ? first : second, "Author map hierarchy");
    }

    private static GameObject CanvasRoot(string name, int order)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = order;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return root;
    }

    private static TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        var label = new GameObject("Label", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = 34;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string text, Vector2 position, TMP_FontAsset font)
    {
        var button = new GameObject(text, typeof(Image), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
        var rect = (RectTransform)button.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(240, 70);
        button.targetGraphic = button.GetComponent<Image>();
        button.image.color = new Color(0.24f, 0.19f, 0.16f);
        Label(rect, text, Vector2.zero, rect.sizeDelta, font);
        return button;
    }

    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }
}
