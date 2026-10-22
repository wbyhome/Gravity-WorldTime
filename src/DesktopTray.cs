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
  string next=host.Language+host.S.Layer;if(next==key)return;key=next;
  var menu=new Forms.ContextMenuStrip();
  void Item(string label,Action action,string? mode=null){var item=new Forms.ToolStripMenuItem(host.T(label));item.Click+=(_,_)=>action();if(mode!=null){item.CheckOnClick=false;item.Checked=host.S.Layer==mode;}menu.Items.Add(item);}
  Item("Expand all clocks",host.ExpandAll);Item("Collapse all to edge",host.CollapseAll);menu.Items.Add(new Forms.ToolStripSeparator());
  Item("Recover clocks (pin on top)",host.RecoverClocks);Item("Manage cities",()=>host.Manager.Reveal());Item("Add city",()=>host.Add(host.S.Rows.Count>0?host.S.Rows[^1]:null));Item("Settings",()=>host.Settings());menu.Items.Add(new Forms.ToolStripSeparator());
  Item("On desktop",()=>host.SetLayer("normal"),"normal");Item("Pin on top",()=>host.SetLayer("top"),"top");menu.Items.Add(new Forms.ToolStripSeparator());Item("Exit WorldTime",host.Quit);
  var previous=icon.ContextMenuStrip;icon.ContextMenuStrip=menu;previous?.Dispose();
 }
 public void Dispose(){icon.Visible=false;var menu=icon.ContextMenuStrip;icon.Dispose();menu?.Dispose();}
}
