using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace TextureFlow.UnityEditor {
 [Serializable] public class FlowMotion {public float speed=.1f,angle=90,tilingX=1,tilingY=1,rotation=0,emission=2,hdr=0;public string tint="#FFFFFF",shader="TextureFlow fallback";}
 [Serializable] public class FlowLayerConfig {public string name,maskFile,flowFile;public bool enabled=true;public FlowMotion settings;}
 [Serializable] public class FlowConfig : FlowMotion {public int schemaVersion=1,activeLayer;public FlowLayerConfig[] layers;}
 [InitializeOnLoad] public static class FlowImporter {
  static bool queued;static double nextScan;
  static FlowImporter(){EditorApplication.update+=Tick;Queue();}
  static void Tick(){if(EditorApplication.timeSinceStartup<nextScan)return;nextScan=EditorApplication.timeSinceStartup+2;if(Directory.Exists("Assets/TextureFlow/Exports")){bool pending=Directory.GetFiles("Assets/TextureFlow/Exports","flow-request.json",SearchOption.AllDirectories).Any(f=>!File.Exists(Path.Combine(Path.GetDirectoryName(f),"flow-result.json"))||File.GetLastWriteTimeUtc(f)>File.GetLastWriteTimeUtc(Path.Combine(Path.GetDirectoryName(f),"flow-result.json")));if(pending){AssetDatabase.Refresh();Queue();}}}
  public static void Queue(){if(queued)return;queued=true;EditorApplication.delayCall+=Process;}
  static void Process(){queued=false;if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}if(!Directory.Exists("Assets/TextureFlow/Exports"))return;foreach(string f in Directory.GetFiles("Assets/TextureFlow/Exports","flow-request.json",SearchOption.AllDirectories)){string result=Path.Combine(Path.GetDirectoryName(f),"flow-result.json");if(File.Exists(result)&&File.GetLastWriteTimeUtc(result)>=File.GetLastWriteTimeUtc(f))continue;try{string message=Import(f);File.WriteAllText(result,JsonUtility.ToJson(new Result{success=true,message=message},true));}catch(Exception ex){Debug.LogError("Texture Flow: "+ex);File.WriteAllText(result,JsonUtility.ToJson(new Result{success=false,message=ex.Message},true));}}AssetDatabase.SaveAssets();}
  [Serializable] class Result {public bool success;public string message;}
  static string AssetPath(string folder,string name){if(string.IsNullOrEmpty(name)||Path.IsPathRooted(name))throw new Exception("Invalid area texture path.");string full=Path.GetFullPath(Path.Combine(folder,name));string root=Path.GetFullPath(folder)+Path.DirectorySeparatorChar;if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Area textures must remain inside the export.");return (folder+"/"+name).Replace('\\','/');}
  static Texture2D Texture(string folder,string file,bool linear,bool repeat){string path=AssetPath(folder,file);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Texture not imported: "+path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!linear;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  static void Require(Material m,params string[] names){foreach(string n in names)if(!m.HasProperty(n))throw new Exception("Shader version lacks "+n+". Use TextureFlow fallback.");}
  static void Validate(FlowMotion c){bool Range(float f,float min,float max){return !float.IsNaN(f)&&!float.IsInfinity(f)&&f>=min&&f<=max;}if(c==null||!Range(c.speed,.01f,100)||!Range(c.angle,0,360)||!Range(c.tilingX,.01f,1000)||!Range(c.tilingY,.01f,1000)||!Range(c.rotation,0,360)||!Range(c.emission,0,100)||!Range(c.hdr,-10,16)||!ColorUtility.TryParseHtmlString(c.tint,out var unused))throw new Exception("An area has invalid motion or tint settings.");}
  public static string Import(string request){
   var c=JsonUtility.FromJson<FlowConfig>(File.ReadAllText(request));if(c==null||(c.schemaVersion!=1&&c.schemaVersion!=2))throw new Exception("Unsupported export schema.");
   FlowLayerConfig[] layers;
   if(c.schemaVersion==1)layers=new[]{new FlowLayerConfig{name="Area 1",enabled=true,maskFile="mask.png",flowFile="flow.png",settings=c}};
   else {if(c.layers==null||c.layers.Length<1||c.layers.Length>8)throw new Exception("Exports support one to eight independent areas.");layers=c.layers.Where(l=>l.enabled).ToArray();}
   if(layers.Length==0)throw new Exception("There are no enabled areas to import.");
   string dir=Path.GetDirectoryName(request).Replace('\\','/');var original=Texture(dir,"base.png",false,false);Texture(dir,"mask.png",true,false);Texture(dir,"cutout.png",false,false);
   var shader=Shader.Find("TextureFlow/Masked Scroll");if(shader==null)throw new Exception("Texture Flow shader is missing.");var material=new Material(shader);Texture2D firstFlow=null,firstMask=null;
   try {
    material.SetTexture("_MainTex",original);material.SetFloat("_LayerCount",layers.Length);
    for(int i=0;i<8;i++)material.SetFloat("_Enabled"+i,i<layers.Length?1:0);
    for(int i=0;i<layers.Length;i++){
     var layer=layers[i];Validate(layer.settings);var settings=layer.settings;var flow=Texture(dir,layer.flowFile,false,true);var mask=Texture(dir,layer.maskFile,true,false);if(mask.width!=original.width||mask.height!=original.height)throw new Exception("Area mask dimensions do not match the original.");
     if(i==0){firstFlow=flow;firstMask=mask;}string suffix=i==0?"":i.ToString();ColorUtility.TryParseHtmlString(settings.tint,out var tint);float angle=settings.angle*Mathf.Deg2Rad;
     material.SetTexture("_FlowTex"+suffix,flow);material.SetTexture("_Mask"+suffix,mask);material.SetTextureScale("_FlowTex"+suffix,new Vector2(settings.tilingX,settings.tilingY));material.SetVector("_Velocity"+suffix,new Vector4(Mathf.Cos(angle)*settings.speed,Mathf.Sin(angle)*settings.speed,0,0));material.SetFloat("_Rotation"+suffix,settings.rotation*Mathf.Deg2Rad);material.SetColor("_Tint"+suffix,tint);material.SetFloat("_Emission"+suffix,settings.emission*Mathf.Pow(2,settings.hdr));
    }
    string path=AssetPath(dir,"Animated.mat");var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null)AssetDatabase.CreateAsset(material,path);else{EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);material=existing;}EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
   }catch{if(material!=null&&!AssetDatabase.Contains(material))UnityEngine.Object.DestroyImmediate(material);throw;}
   string message="Created material with "+layers.Length+" independent animated area(s).";
   if(c.shader!="TextureFlow fallback"){
    if(layers.Length>1)message+=" Multiple areas use Texture Flow's shader; native shader export is single-area only.";
    else message+=" "+Native(dir,c.shader,layers[0].settings,original,firstFlow,firstMask);
   }
   return message;
  }
  static string Native(string dir,string name,FlowMotion c,Texture2D original,Texture2D flow,Texture2D mask){Material native=null;try{
   Shader shader=name=="Poiyomi"?(Shader.Find(".poiyomi/Poiyomi Toon")??Shader.Find("Poiyomi/Poiyomi Toon")):Shader.Find("lilToon");if(shader==null)throw new Exception(name+" is not installed.");if(c.rotation!=0)throw new Exception("Native rotation mapping is not validated.");native=new Material(shader);Require(native,"_MainTex","_EmissionMap","_EmissionColor");native.SetTexture("_MainTex",original);native.SetTexture("_EmissionMap",flow);native.SetTextureScale("_EmissionMap",new Vector2(c.tilingX,c.tilingY));ColorUtility.TryParseHtmlString(c.tint,out var tint);float angle=c.angle*Mathf.Deg2Rad,strength=c.emission*Mathf.Pow(2,c.hdr);var velocity=new Vector4(Mathf.Cos(angle)*c.speed,Mathf.Sin(angle)*c.speed,0,0);
   if(name=="Poiyomi"){Require(native,"_EnableEmission","_EmissionMask","_EmissionMapPan","_EmissionStrength","_EmissionReplace0");native.SetFloat("_EnableEmission",1);native.EnableKeyword("_EMISSION");native.SetTexture("_EmissionMask",mask);native.SetVector("_EmissionMapPan",-velocity);native.SetColor("_EmissionColor",tint);native.SetFloat("_EmissionStrength",strength);native.SetFloat("_EmissionReplace0",1);}
   else {Require(native,"_UseEmission","_EmissionBlendMask","_EmissionMap_ScrollRotate");native.SetFloat("_UseEmission",1);native.SetTexture("_EmissionBlendMask",mask);native.SetVector("_EmissionMap_ScrollRotate",-velocity);native.SetColor("_EmissionColor",tint*Mathf.Max(strength,0));if(native.HasProperty("_EmissionBlendMode"))native.SetFloat("_EmissionBlendMode",0);}
   string path=AssetPath(dir,"Native.mat");var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null)AssetDatabase.CreateAsset(native,path);else{EditorUtility.CopySerialized(native,existing);UnityEngine.Object.DestroyImmediate(native);}AssetDatabase.SaveAssets();return "Native material created; verify it with your shader version.";
  }catch(Exception ex){if(native!=null&&!AssetDatabase.Contains(native))UnityEngine.Object.DestroyImmediate(native);return ex.Message+" Animated.mat uses the included shader.";}}
 }
 public class FlowPostprocessor : AssetPostprocessor {static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] from){if(imported.Any(p=>p.EndsWith("flow-request.json",StringComparison.Ordinal)))FlowImporter.Queue();}}
 public class FlowWindow : EditorWindow {
  Material generated;Renderer target;int slot;
  [MenuItem("Tools/Texture Flow/Apply Animated Material")] public static void Open(){GetWindow<FlowWindow>("Texture Flow");}
  void OnGUI(){GUILayout.Label("Texture Flow: apply to one avatar slot",EditorStyles.boldLabel);EditorGUILayout.HelpBox("Animated.mat contains all enabled areas. Choose the avatar Renderer and slot. Undo is supported. PC only.",MessageType.Info);generated=(Material)EditorGUILayout.ObjectField("Generated material",generated,typeof(Material),false);target=(Renderer)EditorGUILayout.ObjectField("Avatar renderer",target,typeof(Renderer),true);slot=EditorGUILayout.IntField("Material slot",slot);if(target!=null){var mats=target.sharedMaterials;for(int i=0;i<mats.Length;i++)EditorGUILayout.LabelField(i+": "+(mats[i]==null?"Empty":mats[i].name));}using(new EditorGUI.DisabledScope(generated==null||target==null||slot<0||slot>=target.sharedMaterials.Length)){if(GUILayout.Button("Apply to this slot")){Undo.RecordObject(target,"Apply Texture Flow material");var mats=target.sharedMaterials;mats[slot]=generated;target.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(target);EditorUtility.SetDirty(target);}}if(GUILayout.Button("Use selected material"))generated=Selection.activeObject as Material;if(GUILayout.Button("Process pending exports"))FlowImporter.Queue();}
 }
}
