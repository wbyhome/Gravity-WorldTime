using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
namespace WorldTime;
public sealed partial class RulerWindow {
 void BuildNativeMenu(Point screen){
  if(ModalBusy)return;NativePointerLeft();if(openMenu!=null)openMenu.IsOpen=false;var menu=new ContextMenu();openMenu=menu;menu.Closed+=(_,_)=>{if(ReferenceEquals(openMenu,menu))openMenu=null;SyncMotion();};
  MenuItem Item(ItemsControl parent,string key,Action action,bool? check=null){var item=new MenuItem{Header=host.T(key)};if(check.HasValue){item.IsCheckable=true;item.IsChecked=check.Value;}item.Click+=(_,_)=>action();parent.Items.Add(item);return item;}
  MenuItem Sub(ItemsControl parent,string key){var item=new MenuItem{Header=host.T(key)};parent.Items.Add(item);return item;}
  Point local=Desktop.Hosted?new Point((screen.X-Desktop.Position.X)/Desktop.Scale,(screen.Y-Desktop.Position.Y)/Desktop.Scale):root.PointFromScreen(screen);var cluster=ClusterAt(local);
  if(cluster!=null){var person=cluster.Members[0];Item(menu,"Contact details",()=>{flyoutPoint=screen;OpenCluster(cluster,PersonButtons[person.Id]);});var edit=Sub(menu,"Edit contact");Item(edit,"Choose avatar",()=>ChooseAvatar(AvatarId(person)));if(person.Self)Item(edit,"Contact name",()=>EditDisplayName());if(person.Row is Row row){Item(edit,"Contact name",()=>Rename(row));Item(edit,"City routine",()=>ShowTransient(new RoutineEditor(host,row)));Item(edit,"Change city or time zone",()=>ShowTransient(new RulerContactDialog(host,this,null,row)));Item(edit,"Remove",()=>ConfirmRemove(row));}menu.Items.Add(new Separator());}
  int? reference=new Rect(BandOrigin,ScaleTop,SkyWidth,40).Contains(local)?RulerAxis.ReferenceMinute(local.X-BandOrigin,SkyWidth):null;
  Item(menu,"Add contact",()=>ShowTransient(new RulerContactDialog(host,this,reference)));
  Item(menu,"Adjust transparency…",OpenAppearance);Item(menu,"Pin on top",()=>SetPinned(!ActualPinned),ActualPinned);
  var size=Sub(menu,"Display size");Item(size,"Compact",()=>SetPreset(560,true),Compact);Item(size,"Standard",()=>SetPreset(720,false),!Compact&&Math.Abs(Width-720)<1);Item(size,"Wide",()=>SetPreset(1000,false),!Compact&&Math.Abs(Width-1000)<1);Item(size,"Custom width…",()=>ShowTransient(new RulerWidthDialog(host,this)));
  var more=Sub(menu,"More settings");var animation=Sub(more,"Animation effects");OpenMotionSettings(animation,(parent,key,action,check)=>{Item(parent,key,action,check);});Item(more,"My display name…",()=>EditDisplayName());Item(more,"Manage contacts",()=>host.Manager.Reveal());Item(more,host.S.Twelve?"24-hour clock":"12-hour clock",()=>{host.S.Twelve=!host.S.Twelve;host.Sync();host.Save();});Item(more,"Nature accents",()=>SetNature(!NatureEnabled),NatureEnabled);Item(more,"Show dates",()=>SetDates(!ShowDates),ShowDates);Item(more,"Choose my avatar",()=>ChooseAvatar("$self"));Item(more,"Language",()=>ShowTransient(new RulerAppearanceDialog(host,this)));Item(more,"Ruler help",()=>MessageBox.Show(host.T("Drag the left end to move. Drag the right end to resize. Right-click for settings.")+"\n\n"+host.T("The date belongs to the representative. Open the group for each person's date.")+"\n"+host.T("The two edge hours repeat the clock cycle; this is not a global 26-hour timeline."),"WorldTime"));
  if(!Desktop.DesktopHosted&&!ActualPinned)Item(menu,"Return to desktop",()=>SetPinned(false));menu.Items.Add(new Separator());Item(menu,"Reset ruler position",ResetPosition);Item(menu,"Hide ruler",Conceal);Item(menu,"Exit WorldTime",host.Quit);
  var area=DockScreen.AtPoint(screen);menu.Placement=PlacementMode.AbsolutePoint;menu.HorizontalOffset=screen.X/area.Scale;menu.VerticalOffset=screen.Y/area.Scale;menu.IsOpen=true;SyncMotion();
 }
}
