using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ReloadBackedUpScene209 { public static string Run(){var s=SceneManager.GetActiveScene();var b="C:/Users/lomi4/OneDrive/Desktop/figurine 작업내용/rollback-209-backup-20261123/OpenScene-"+s.name+".unity";if(!System.IO.File.Exists(b))throw new System.Exception("Backup missing");EditorSceneManager.OpenScene(s.path,OpenSceneMode.Single);return "Reloaded only after verifying saved scene backup. Auto refresh unchanged.";} }