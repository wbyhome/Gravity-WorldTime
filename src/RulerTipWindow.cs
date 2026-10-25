using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WorldTime;
// Only non-interactive tooltip HWNDs get mouse transparency. Never the ruler itself.
public sealed class RulerTipWindow : Window {
 const long PassiveFlags=0x08000000|0x20;
 public bool Passive {get{var h=new WindowInteropHelper(this).Handle;return h!=IntPtr.Zero&&(GetWindowLongPtr(h,-20).ToInt64()&PassiveFlags)==PassiveFlags;}}
 protected override void OnSourceInitialized(EventArgs e){base.OnSourceInitialized(e);var h=new WindowInteropHelper(this).Handle;var style=GetWindowLongPtr(h,-20).ToInt64();SetWindowLongPtr(h,-20,new IntPtr(style|PassiveFlags));if(!Passive)throw new InvalidOperationException("Read-only tooltip could not become mouse transparent.");}
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)]static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
}
