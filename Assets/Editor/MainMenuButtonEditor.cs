using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(MainMenuButton)), CanEditMultipleObjects]
public class MainMenuButtonEditor : ButtonEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Text Outline", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("label"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("normalTextMaterial"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverTextMaterial"));
        serializedObject.ApplyModifiedProperties();
    }
}
