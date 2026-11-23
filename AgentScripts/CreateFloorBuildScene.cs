using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class CreateFloorBuildScene {
 public static string Run(){
 var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 try{
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Floor";floor.transform.position=new Vector3(0,-0.5f,0);floor.transform.localScale=new Vector3(20,1,20);
 var camera=new GameObject("Floor check camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1.8f,-8);camera.orthographic=true;camera.orthographicSize=2.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
 var fill=new GameObject("Cut face").AddComponent<FloorCutFaceFill>();var s=new SerializedObject(fill);s.FindProperty("viewCamera").objectReferenceValue=camera;s.FindProperty("floor").objectReferenceValue=floor.GetComponent<Collider>();s.FindProperty("faceColor").colorValue=new Color(0.2f,0.4f,0.6f,1);s.ApplyModifiedProperties();
 var type=Type.GetType("FloorFillBuildProbe, Assembly-CSharp");if(type==null)throw new Exception("Probe not compiled yet");var probe=new GameObject("Build probe").AddComponent(type);s=new SerializedObject(probe);s.FindProperty("testCamera").objectReferenceValue=camera;s.FindProperty("fill").objectReferenceValue=fill;s.ApplyModifiedProperties();
 if(!EditorSceneManager.SaveScene(scene,"Assets/Temp/FloorFillBuildCheck/Check.unity"))throw new Exception("Save failed");return "Created isolated build validation scene";
 }finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
 }
}