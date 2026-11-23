using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class PrepareRollback209 { public static string Run(){ if(EditorApplication.isPlaying) throw new System.Exception("Stop play mode first"); var path=SceneManager.GetActiveScene().path; EditorSceneManager.OpenScene(path,OpenSceneMode.Single); AssetDatabase.DisallowAutoRefresh(); return "Discarded camera preview; auto refresh suspended for rollback.";} }