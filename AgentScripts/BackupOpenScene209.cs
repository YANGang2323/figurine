using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class BackupOpenScene209 { public static string Run(){ var s=SceneManager.GetActiveScene(); string backup="C:/Users/lomi4/OneDrive/Desktop/figurine 작업내용/rollback-209-backup-20261123/OpenScene-"+s.name+".unity"; if(!EditorSceneManager.SaveScene(s,backup,true))throw new System.Exception("Scene backup failed");return "Saved scene copy: "+backup+"; original="+s.path+"; dirty="+s.isDirty;} }