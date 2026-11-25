using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TextureFlow.UnityEditor;
public static class LayerRenderSmoke {
 public static void Run(){Material material=null;RenderTexture target=null;Texture2D readback=null;try{
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var request=Directory.GetFiles("Assets/TextureFlow/Exports","flow-request.json",SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).First(f=>{var c=JsonUtility.FromJson<FlowConfig>(File.ReadAllText(f));return c.schemaVersion==2&&c.layers!=null&&c.layers.Length==2;});
  FlowImporter.Import(request);var generated=AssetDatabase.LoadAssetAtPath<Material>(Path.GetDirectoryName(request).Replace('\\','/')+"/Animated.mat");material=new Material(generated);material.SetFloat("_Emission",1);material.SetFloat("_Emission1",1);
  // Exercise the full eight-area shader path while six additional masks are empty.
  material.SetFloat("_LayerCount",8);for(int i=2;i<8;i++){material.SetFloat("_Enabled"+i,1);material.SetTexture("_FlowTex"+i,Texture2D.whiteTexture);material.SetTexture("_Mask"+i,Texture2D.blackTexture);}
  RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.reflectionIntensity=0;
  var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.GetComponent<Renderer>().sharedMaterial=material;quad.transform.localScale=new Vector3(2,2,1);
  var camera=new GameObject("Layer render camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=false;
  target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;readback=new Texture2D(64,64,TextureFormat.RGB24,false);
  Action Render=()=>{camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,64,64),0,0);readback.Apply();RenderTexture.active=null;};
  Render();if(ShaderUtil.ShaderHasError(material.shader))throw new Exception("Shader errors after GPU compilation.");Color left=readback.GetPixel(8,32),right=readback.GetPixel(56,32),middle=readback.GetPixel(32,32);
  if(!(left.r>left.g+.2f&&right.g>right.r+.2f&&middle.maxColorComponent<.2f))throw new Exception("Independent region render mismatch: "+left+" / "+right+" / "+middle);
  File.WriteAllBytes("layer-gpu-preview.png",readback.EncodeToPNG());
  material.SetTexture("_Mask1",material.GetTexture("_Mask"));Render();var overlap=readback.GetPixel(8,32);if(overlap.g<=overlap.r+.2f)throw new Exception("Later area did not cover earlier area in GPU output.");
  material.SetFloat("_Enabled1",0);Render();var disabled=readback.GetPixel(8,32);if(disabled.r<=disabled.g+.2f)throw new Exception("Disabled area still covered the first area.");
  File.WriteAllText("layer-render-result.txt","PASS actual GPU rendering: separate red/green regions, untouched original, overlap order, disabled areas and full eight-area shader compilation. Device: "+SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName);EditorApplication.Exit(0);
 }catch(Exception ex){Debug.LogException(ex);File.WriteAllText("layer-render-result.txt",ex.ToString());EditorApplication.Exit(1);}finally{if(material!=null)UnityEngine.Object.DestroyImmediate(material);if(readback!=null)UnityEngine.Object.DestroyImmediate(readback);if(target!=null)target.Release();}}
}
