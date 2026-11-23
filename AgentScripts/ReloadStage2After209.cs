using UnityEditor;
using UnityEditor.SceneManagement;
public static class ReloadStage2After209 { public static string Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scene/Stage2.unity",OpenSceneMode.Single);return "Refreshed once and opened restored Stage2";} }