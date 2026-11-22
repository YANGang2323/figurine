using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DialogueChoiceAuthoring
{
    private const string ChoicePath = "Assets/Prefab/univ/Choice.prefab";
    private const string FontPath = "Assets/Fonts/KOPUBWORLD/KoPubWorld Batang Light SDF.asset";
    private const string PromptPath = "Assets/Prefab/univ/Stage Save Prompt.prefab";

    [MenuItem("Figurine/Apply Dialog Bar Choices")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Edit dialogue UI outside Play Mode.");

        AuthorChoicePrefab();
        AuthorBarPrefab("Assets/Prefab/univ/Dialog bar.prefab", false);
        AuthorBarPrefab("Assets/Prefab/univ/DialogUI 1.prefab", true);
        var managerRoot = PrefabUtility.LoadPrefabContents("Assets/Prefab/univ/DialogueManager.prefab");
        try
        {
            var manager = managerRoot.GetComponent<DialogueManager>();
            manager.defaultDialogueFont = Font;
            manager.defaultSpeakerFont = Font;
            PrefabUtility.SaveAsPrefabAsset(managerRoot, "Assets/Prefab/univ/DialogueManager.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(managerRoot); }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PromptPath) == null)
        {
            var root = new GameObject("Stage Save Prompt", typeof(StageSavePrompt));
            try { PrefabUtility.SaveAsPrefabAsset(root, PromptPath); }
            finally { Object.DestroyImmediate(root); }
        }

        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scene" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool loaded = scene.IsValid() && scene.isLoaded;
                if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                if (ConfigureScene(scene))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                if (!loaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
    }

    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    private static void AuthorChoicePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(ChoicePath);
        try
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
            var raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Object.DestroyImmediate(raycaster);
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null) Object.DestroyImmediate(scaler);
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null) Object.DestroyImmediate(canvas);
            var view = root.GetComponent<DialogueBarChoices>() ?? root.AddComponent<DialogueBarChoices>();
            var layout = root.GetComponent<VerticalLayoutGroup>() ?? root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var buttons = new Button[3];
            var labels = new TMP_Text[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                var row = new GameObject("ChoiceButton" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.layer = 5;
                row.transform.SetParent(root.transform, false);
                row.GetComponent<Image>().color = Color.clear;
                row.GetComponent<LayoutElement>().preferredWidth = 420f;
                row.GetComponent<LayoutElement>().preferredHeight = 49f;
                var label = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.gameObject.layer = 5;
                label.transform.SetParent(row.transform, false);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.font = Font;
                label.fontSize = 36;
                label.fontSizeMax = 36;
                label.fontSizeMin = 26;
                label.enableAutoSizing = true;
                label.alignment = TextAlignmentOptions.TopLeft;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                label.text = "> 선택지 " + (i + 1);
                buttons[i] = row.GetComponent<Button>();
                buttons[i].targetGraphic = label;
                buttons[i].transition = Selectable.Transition.ColorTint;
                var colors = buttons[i].colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.85f, 0.58f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.82f, 0.65f, 0.4f);
                colors.fadeDuration = 0.1f;
                buttons[i].colors = colors;
                labels[i] = label;
            }
            SetArray(view, "buttons", buttons);
            SetArray(view, "labels", labels);
            SetArray(view, "hiddenWhileChoosing", new GameObject[0]);
            PositionView(view);
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, ChoicePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AuthorBarPrefab(string path, bool fullUI)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bar = fullUI ? root.transform.Find("Dialog bar") : root.transform;
            var view = EnsureView(bar);
            var hidden = bar.Cast<Transform>().Where(t => t != view.transform &&
                (t.name == "Dialogue TMP_Text" || t.name == "Speaker TMP_Text" || t.name == "Q"))
                .Select(t => t.gameObject).ToArray();
            SetArray(view, "hiddenWhileChoosing", hidden);
            foreach (var text in bar.GetComponentsInChildren<TMP_Text>(true)) text.font = Font;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static bool ConfigureScene(Scene scene)
    {
        var managers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DialogueManager>(true)).ToArray();
        if (managers.Length == 0) return false;
        foreach (var manager in managers)
        {
            if (manager.panel == null || manager.dialogueText == null || manager.speakerText == null) continue;
            var oldPanel = manager.choicePanel;
            var view = EnsureView(manager.panel.transform);
            SetArray(view, "hiddenWhileChoosing", new[]
            {
                manager.dialogueText.gameObject, manager.speakerText.gameObject,
                manager.panel.transform.Find("Q")?.gameObject
            }.Where(o => o != null).ToArray());
            manager.choicePanel = view.gameObject;
            manager.choiceButtons = view.Buttons;
            manager.defaultDialogueFont = Font;
            manager.defaultSpeakerFont = Font;
            manager.dialogueText.font = Font;
            manager.speakerText.font = Font;
            Set(manager, "barChoices", view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            if (oldPanel != null && oldPanel != view.gameObject && !oldPanel.transform.IsChildOf(manager.panel.transform))
            {
                var oldRoot = oldPanel.transform.root.gameObject;
                // Delete only the obsolete choice Canvas, never a shared gameplay/UI root.
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(oldRoot) == ChoicePath)
                    Undo.DestroyObjectImmediate(oldRoot);
                else oldPanel.SetActive(false);
            }
        }
        // Old Choice instances may have lost their child references when their source was migrated.
        foreach (var root in scene.GetRootGameObjects())
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == ChoicePath)
                Undo.DestroyObjectImmediate(root);

        var prompt = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StageSavePrompt>(true)).FirstOrDefault();
        if (prompt != null) ConfigurePrompt(prompt, managers[0]);
        return true;
    }

    public static void ConfigurePrompt(StageSavePrompt prompt, DialogueManager manager)
    {
        foreach (Transform child in prompt.transform.Cast<Transform>().ToArray())
            Undo.DestroyObjectImmediate(child.gameObject);
        var raycaster = prompt.GetComponent<GraphicRaycaster>();
        if (raycaster != null) Undo.DestroyObjectImmediate(raycaster);
        var scaler = prompt.GetComponent<CanvasScaler>();
        if (scaler != null) Undo.DestroyObjectImmediate(scaler);
        var canvas = prompt.GetComponent<Canvas>();
        if (canvas != null) Undo.DestroyObjectImmediate(canvas);
        Set(prompt, "dialogueManager", manager);
        prompt.gameObject.SetActive(false);
    }

    private static DialogueBarChoices EnsureView(Transform bar)
    {
        var view = bar.GetComponentInChildren<DialogueBarChoices>(true);
        if (view == null)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ChoicePath), bar);
            view = root.GetComponent<DialogueBarChoices>();
        }
        PositionView(view);
        view.gameObject.SetActive(false);
        return view;
    }

    private static void PositionView(DialogueBarChoices view)
    {
        var rect = (RectTransform)view.transform;
        var body = view.transform.parent?.Find("Dialogue TMP_Text") as RectTransform;
        rect.anchorMin = body != null ? body.anchorMin : new Vector2(0, 1);
        rect.anchorMax = body != null ? body.anchorMax : new Vector2(0, 1);
        rect.pivot = body != null ? body.pivot : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = body != null ? body.anchoredPosition : new Vector2(665.86f, -206.52f);
        rect.sizeDelta = body != null ? body.sizeDelta : new Vector2(1124.43f, 167.05f);
        rect.localScale = Vector3.one;
    }

    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var serialized = new SerializedObject(target);
        var array = serialized.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedProperties();
    }
}
