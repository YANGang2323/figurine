using UnityEditor;
public static class RefreshFloorFill { public static string Run(){AssetDatabase.Refresh(); return "Floor fill source imported";} }