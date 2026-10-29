using System;
using System.Drawing;
using Forms=System.Windows.Forms;
namespace WorldTime;
public sealed class DesktopTray : IDisposable {
 readonly Host host;
 readonly Forms.NotifyIcon icon;
 string key="";
 public bool Visible=>icon.Visible;
 public DesktopTray(Host host){this.host=host;icon=new Forms.NotifyIcon{Icon=SystemIcons.Application,Text="Gravity WorldTime",Visible=true};icon.MouseClick+=(_,e)=>{if(e.Button==Forms.MouseButtons.Left)host.Manager.Reveal();};Refresh();}
 public void Refresh(){
  string next=host.Language+host.Ruler?.DisplayMode+host.S.Layer+host.Ruler?.ActualPinned+host.Ruler?.Desktop.DesktopHosted;if(next==key)return;key=next;
  var menu=new Forms.ContextMenuStrip();
  void Item(string label,Action action,string? mode=null){var item=new Forms.ToolStripMenuItem(host.T(label));item.Click+=(_,_)=>action();if(mode!=null){item.CheckOnClick=false;item.Checked=host.S.Layer==mode;}menu.Items.Add(item);}
  Item("Time ruler (preview)",host.ShowRuler);if(host.RulerPreviewOnly){var modes=new Forms.ToolStripMenuItem(host.T("Display mode"));foreach(var mode in new[]{WorldTimeView.Ruler,WorldTimeView.Ring}){var selectedMode=mode;var option=new Forms.ToolStripMenuItem(host.T(mode==WorldTimeView.Ring?"Circular ring":"Horizontal ruler")){Checked=host.Ruler?.DisplayMode==mode};option.Click+=(_,_)=>{var r=host.EnsureRuler();if(!r.SetDisplayMode(selectedMode))System.Windows.MessageBox.Show(host.T("Could not switch display mode. The previous view was restored."),"WorldTime");};modes.DropDownItems.Add(option);}menu.Items.Add(modes);var pin=new Forms.ToolStripMenuItem(host.T("Pin on top")){Checked=host.Ruler?.ActualPinned==true};pin.Click+=(_,_)=>{var r=host.EnsureRuler();r.SetPinned(!r.ActualPinned);};menu.Items.Add(pin);if(host.Ruler?.Desktop.DesktopHosted!=true&&host.Ruler?.ActualPinned!=true)Item("Return to desktop",()=>host.EnsureRuler().SetPinned(false));}if(!host.RulerPreviewOnly){Item("Expand all clocks",host.ExpandAll);Item("Collapse all to edge",host.CollapseAll);}menu.Items.Add(new Forms.ToolStripSeparator());
  Item(host.RulerPreviewOnly?"Restore ruler":"Recover clocks (pin on top)",host.RecoverClocks);Item("Manage cities",()=>host.Manager.Reveal());Item("Add city",()=>{if(host.RulerPreviewOnly)host.EnsureRuler().AddContact();else host.Add(host.S.Rows.Count>0?host.S.Rows[^1]:null);});Item("Settings",()=>{if(host.RulerPreviewOnly)host.EnsureRuler().OpenAppearance();else host.Settings();});menu.Items.Add(new Forms.ToolStripSeparator());
  if(!host.RulerPreviewOnly){Item("On desktop",()=>host.SetLayer("normal"),"normal");Item("Pin on top",()=>host.SetLayer("top"),"top");}menu.Items.Add(new Forms.ToolStripSeparator());Item("Exit WorldTime",host.Quit);
  var previous=icon.ContextMenuStrip;icon.ContextMenuStrip=menu;previous?.Dispose();
 }
 public void Dispose(){icon.Visible=false;var menu=icon.ContextMenuStrip;icon.Dispose();menu?.Dispose();}
}
