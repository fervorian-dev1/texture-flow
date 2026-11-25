using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TextureFlow;

public sealed class EffectLayer : INotifyPropertyChanged
{
 string name="Area",flowName="No moving texture yet";bool enabled=true;
 public string Name {get=>name;set{name=value;Changed();}}
 public string FlowName {get=>flowName;set{flowName=value;Changed();}}
 public bool Enabled {get=>enabled;set{enabled=value;Changed();}}
 public byte[] Mask=[];
 public Raster? Flow;
 public MotionSettings Settings=new();
 public Stack<byte[]> Undo=new(),Redo=new();
 public event PropertyChangedEventHandler? PropertyChanged;
 void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
}

public partial class MainWindow
{
 public const int MaxEffects=8;
 public ObservableCollection<EffectLayer> EffectLayers {get;}=new();
 EffectLayer? activeEffect;
 Stack<byte[]> redo=new();
 bool changingEffects;
 int nextArea=1;
 (EffectLayer layer,int index)? removedEffect;

 void InitializeEffects()
 {
  EffectLayers.CollectionChanged+=(_,e)=>{if(e.OldItems!=null)foreach(EffectLayer l in e.OldItems)l.PropertyChanged-=LayerLabelChanged;if(e.NewItems!=null)foreach(EffectLayer l in e.NewItems)l.PropertyChanged+=LayerLabelChanged;};
  Effects.ItemsSource=EffectLayers;
  ResetEffects();UpdateToolButtons();
  PreviewKeyDown+=EditorShortcut;
 }
 void LayerLabelChanged(object? sender,PropertyChangedEventArgs e){if(ReferenceEquals(sender,activeEffect)&&(e.PropertyName==nameof(EffectLayer.Name)||e.PropertyName==nameof(EffectLayer.FlowName)))UpdateEffectLabels();}
 void ResetEffects()
 {
  string flowName=activeEffect?.FlowName??"Moving texture";changingEffects=true;
  EffectLayers.Clear();nextArea=1;removedEffect=null;
  var first=new EffectLayer{Name="Area "+nextArea++,Mask=mask,Flow=flow,Settings=new MotionSettings(),FlowName=flow==null?"No moving texture yet":flowName};
  EffectLayers.Add(first);activeEffect=first;history=first.Undo;redo=first.Redo;Effects.SelectedItem=first;
  changingEffects=false;
  ApplyEffectControls(first.Settings);UpdateEffectLabels();
 }
 void UpdateEffectLabels()
 {
  if(activeEffect==null)return;
  FlowLabel.Text=activeEffect.FlowName;
  AreaHint.Text=$"Editing {activeEffect.Name}. Other areas are kept.";
 }
 void SaveActiveEffect()
 {
  if(activeEffect==null)return;
  activeEffect.Mask=mask;activeEffect.Flow=flow;activeEffect.Settings=Settings();
 }
 static MotionSettings CopySettings(MotionSettings settings)=>JsonSerializer.Deserialize<MotionSettings>(JsonSerializer.Serialize(settings,Json),Json)!;
 void ApplyEffectControls(MotionSettings c)
 {
  string F(float number)=>number.ToString(System.Globalization.CultureInfo.InvariantCulture);
  Speed.Text=F(c.speed);Angle.Value=c.angle;TileX.Text=F(c.tilingX);TileY.Text=F(c.tilingY);Rotation.Text=F(c.rotation);Emission.Text=F(c.emission);Hdr.Text=F(c.hdr);Tint.Text=c.tint;
  Shader.SelectedIndex=c.shader=="Poiyomi"?1:c.shader=="lilToon"?2:0;
 }
 void ActivateEffect(EffectLayer layer)
 {
  activeEffect=layer;mask=layer.Mask;flow=layer.Flow;history=layer.Undo;redo=layer.Redo;
  polygon.Clear();Outline.Points.Clear();drawing=false;Preview.ReleaseMouseCapture();
  ApplyEffectControls(layer.Settings);UpdateEffectLabels();
 }
 void EffectSelected(object sender,SelectionChangedEventArgs e)
 {
  if(changingEffects||activeEffect==null||Effects.SelectedItem is not EffectLayer selected||ReferenceEquals(selected,activeEffect))return;
  if(busy){changingEffects=true;Effects.SelectedItem=activeEffect;changingEffects=false;return;}
  try{SaveActiveEffect();ActivateEffect(selected);Status.Text="Editing "+selected.Name+". Your other areas are kept.";}
  catch(Exception ex){changingEffects=true;Effects.SelectedItem=activeEffect;changingEffects=false;Status.Text=ex.Message;}
 }
 public EffectLayer AddEffectLayer(bool duplicate=false)
 {
  Check();if(EffectLayers.Count>=MaxEffects)throw new IOException($"This version supports {MaxEffects} independent areas per material. Remove an unused area before adding another.");
  SaveActiveEffect();
  var layer=new EffectLayer{Name="Area "+nextArea++,Mask=duplicate?(byte[])mask.Clone():new byte[image!.W*image.H],Flow=duplicate?flow:null,Settings=duplicate?CopySettings(activeEffect!.Settings):new MotionSettings(),FlowName=duplicate?activeEffect!.FlowName:"No moving texture yet"};
  changingEffects=true;EffectLayers.Add(layer);Effects.SelectedItem=layer;changingEffects=false;ActivateEffect(layer);
  SelectionMode.SelectedIndex=1;ShowMask.IsChecked=true;Steps.SelectedIndex=1;
  Status.Text=$"{layer.Name}: select the next color or part, refine it with the brush, then choose its moving texture. Previous areas are kept.";
  return layer;
 }
 void AddArea(object sender,RoutedEventArgs e){if(busy)return;try{AddEffectLayer();}catch(Exception ex){Status.Text=ex.Message;}}
 void DuplicateArea(object sender,RoutedEventArgs e){if(busy)return;try{AddEffectLayer(true);}catch(Exception ex){Status.Text=ex.Message;}}
 void RemoveArea(object sender,RoutedEventArgs e)
 {
  if(busy||activeEffect==null)return;
  try
  {
   SaveActiveEffect();int index=EffectLayers.IndexOf(activeEffect);removedEffect=(activeEffect,index);changingEffects=true;EffectLayers.RemoveAt(index);
   if(EffectLayers.Count==0)EffectLayers.Add(new EffectLayer{Name="Area "+nextArea++,Mask=new byte[image==null?0:image.W*image.H]});
   var next=EffectLayers[Math.Min(index,EffectLayers.Count-1)];Effects.SelectedItem=next;changingEffects=false;ActivateEffect(next);Status.Text="Area removed. Use Restore removed area to bring it back.";
  }
  catch(Exception ex){changingEffects=false;Status.Text=ex.Message;}
 }
 void RestoreRemovedArea(object sender,RoutedEventArgs e)
 {
  if(busy||removedEffect==null)return;
  if(EffectLayers.Count>=MaxEffects){Status.Text="Remove an unused area before restoring another.";return;}
  SaveActiveEffect();var saved=removedEffect.Value;changingEffects=true;EffectLayers.Insert(Math.Min(saved.index,EffectLayers.Count),saved.layer);Effects.SelectedItem=saved.layer;changingEffects=false;ActivateEffect(saved.layer);removedEffect=null;Status.Text="Removed area restored.";
 }
 void ReorderArea(object sender,RoutedEventArgs e)
 {
  if(busy||activeEffect==null)return;
  int from=EffectLayers.IndexOf(activeEffect),to=from+int.Parse(((Button)sender).Tag.ToString()!);
  if(to<0||to>=EffectLayers.Count)return;
  SaveActiveEffect();changingEffects=true;EffectLayers.Move(from,to);Effects.SelectedItem=activeEffect;changingEffects=false;Status.Text="Order updated. Later areas cover earlier areas where their masks overlap.";
 }
 async void AddMovingArea(object sender,RoutedEventArgs e)
 {
  if(busy)return;var dialog=new Microsoft.Win32.OpenFileDialog{Filter=ProjectTextures.FileFilter};if(dialog.ShowDialog()!=true)return;
  await Work(async()=>{Check();var raster=await Task.Run(()=>Raster.Load(dialog.FileName));AddEffectLayer();SetMovingTexture(raster,Path.GetFileName(dialog.FileName));});
 }
 void SetMovingTexture(Raster raster,string name)
 {
  flow=raster;if(activeEffect!=null){activeEffect.Flow=raster;activeEffect.FlowName=name;}FlowLabel.Text=name;
 }
 void QuickSelectionTool(object sender,RoutedEventArgs e)
 {
  if(busy)return;
  if(polygon.Count>=3)FinishPolygon();else{polygon.Clear();Outline.Points.Clear();}
  Tool.SelectedIndex=int.Parse(((Button)sender).Tag.ToString()!);if(Tool.SelectedIndex<3)SelectionMode.SelectedIndex=1;ShowMask.IsChecked=true;Steps.SelectedIndex=1;
  Status.Text=Tool.SelectedIndex==3?"Paint add: brush imperfections into this area's existing selection.":Tool.SelectedIndex==4?"Paint erase: brush unwanted pixels out of this area.":"Fill color: click matching regions. Add mode keeps the existing selection.";
 }
 void ToolChanged(object sender,SelectionChangedEventArgs e)=>UpdateToolButtons();
 void UpdateToolButtons(){if(Preview==null||QuickFill==null)return;QuickFill.Background=Tool.SelectedIndex==0?Brushes.LightSeaGreen:Brushes.LightGray;QuickAdd.Background=Tool.SelectedIndex==3?Brushes.LightSeaGreen:Brushes.LightGray;QuickErase.Background=Tool.SelectedIndex==4?Brushes.LightSeaGreen:Brushes.LightGray;Preview.Cursor=Tool.SelectedIndex>=3?Cursors.Cross:Cursors.Hand;}
 SelectionOperation CurrentSelectionOperation()
 {
  if((Keyboard.Modifiers&ModifierKeys.Alt)!=0)return SelectionOperation.Subtract;
  if((Keyboard.Modifiers&ModifierKeys.Shift)!=0)return SelectionOperation.Add;
  return (SelectionOperation)Math.Clamp(SelectionMode.SelectedIndex,0,2);
 }
 public void MergeSelection(byte[] selected,SelectionOperation operation){mask=Selection.Combine(mask,selected,operation);if(activeEffect!=null)activeEffect.Mask=mask;}
 void RedoSelection(object sender,RoutedEventArgs e){if(busy)return;if(redo.TryPop(out var next)){history.Push((byte[])mask.Clone());mask=next;TrimSelectionHistory();}}
 void EditorShortcut(object sender,KeyEventArgs e)
 {
  // Preserve ordinary text-field editing shortcuts.
  if(Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase||Keyboard.FocusedElement is ComboBox)return;
  if((Keyboard.Modifiers&ModifierKeys.Control)==0)return;
  if(e.Key==Key.Z){if((Keyboard.Modifiers&ModifierKeys.Shift)!=0)RedoSelection(this,new RoutedEventArgs());else Undo(this,new RoutedEventArgs());e.Handled=true;}
  else if(e.Key==Key.Y){RedoSelection(this,new RoutedEventArgs());e.Handled=true;}
 }
 void TrimSelectionHistory()
 {
  const long budget=256L*1024*1024;
  long Used()=>EffectLayers.Sum(l=>l.Undo.Sum(a=>(long)a.Length)+l.Redo.Sum(a=>(long)a.Length));
  void Oldest(Stack<byte[]> stack){var keep=stack.Reverse().Skip(1).ToArray();stack.Clear();foreach(var item in keep)stack.Push(item);}
  while(Used()>budget){var candidate=EffectLayers.Where(l=>!ReferenceEquals(l,activeEffect)).FirstOrDefault(l=>l.Undo.Count>0||l.Redo.Count>0)??EffectLayers.FirstOrDefault(l=>l.Undo.Count>1||l.Redo.Count>0);if(candidate==null)break;if(candidate.Redo.Count>0)Oldest(candidate.Redo);else Oldest(candidate.Undo);}
 }
}
