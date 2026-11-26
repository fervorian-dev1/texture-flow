using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using TextureFlow.UnityEditor;
public static class VoidLive {
 static double deadline;
 public static void Run(){EditorSceneManager.OpenScene("Assets/VoidTest.unity");deadline=EditorApplication.timeSinceStartup+150;FlowBridge.Publish();File.WriteAllText(FlowBridge.Root+"/live-ready.txt","ready");EditorApplication.update+=Tick;}
 static void Tick(){if(File.Exists(FlowBridge.Root+"/live-stop.txt")){EditorApplication.Exit(0);return;}if(EditorApplication.timeSinceStartup>deadline)EditorApplication.Exit(2);}
}
