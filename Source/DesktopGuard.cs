using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FableAstra;

/// <summary>All bounds here are native screen pixels, including mixed-DPI displays.</summary>
public sealed class DesktopGuard : IDisposable
{
    readonly Window owner;
    readonly IntPtr hwnd;
    readonly HwndSource source;
    readonly DispatcherTimer timer;
    readonly WinEventProc foregroundCallback;
    IntPtr foregroundHook;
    bool disposed;
    public bool SuspendRaise {get;set;}
    public DesktopGuard(Window window,bool watch=true)
    {
        owner=window;hwnd=new WindowInteropHelper(window).Handle;
        source=HwndSource.FromHwnd(hwnd)!;source.AddHook(Hook);
        var styles=GetStyle(hwnd).ToInt64();
        SetStyle(hwnd,new IntPtr(styles|0x08000000L|0x80L)); // no activation; tool window
        foregroundCallback=(_,_,_,_,_,_,_)=>
        {
            if(!disposed)owner.Dispatcher.BeginInvoke(()=> { if(!disposed)KeepVisible(); });
        };
        timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};
        timer.Tick+=(_,_)=>KeepVisible();
        if(watch)
        {
            foregroundHook=SetWinEventHook(3,3,IntPtr.Zero,foregroundCallback,0,0,0);
            timer.Start();
        }
    }
    public void KeepVisible(bool dock=false)
    {
        if(disposed||(!owner.IsVisible&&!dock))return;
        if(IsIconic(hwnd))ShowWindow(hwnd,4); // restore without taking focus
        if(!GetWindowRect(hwnd,out var bounds))return;
        var area=SafeArea(dock?IntPtr.Zero:MonitorFromRect(ref bounds,2));
        var clamped=Clamp(bounds,area,dock);
        uint flags=0x10|0x200; // NOACTIVATE | NOOWNERZORDER
        if(SuspendRaise)flags|=4;
        SetWindowPos(hwnd,new IntPtr(-1),clamped.Left,clamped.Top,clamped.Width,clamped.Height,flags);
    }
    IntPtr Hook(IntPtr handle,int message,IntPtr wparam,IntPtr lparam,ref bool handled)
    {
        if(message==0x21) {handled=true;return new IntPtr(3);} // MA_NOACTIVATE
        if(message==0x216) // WM_MOVING: clamp every drag frame, not only on release
        {
            var r=Marshal.PtrToStructure<NativeRect>(lparam);
            var fixedRect=Clamp(r,SafeArea(MonitorFromRect(ref r,2)));
            Marshal.StructureToPtr(fixedRect,lparam,false);handled=true;return new IntPtr(1);
        }
        if(message==0x46) // WINDOWPOSCHANGING also covers programmatic moves
        {
            var pos=Marshal.PtrToStructure<WindowPos>(lparam);
            if((pos.Flags&3)!=3 && GetWindowRect(hwnd,out var current))
            {
                int x=(pos.Flags&2)!=0?current.Left:pos.X,y=(pos.Flags&2)!=0?current.Top:pos.Y;
                int w=(pos.Flags&1)!=0?current.Width:pos.Width,h=(pos.Flags&1)!=0?current.Height:pos.Height;
                if(w>0&&h>0)
                {
                    var r=new NativeRect(x,y,x+w,y+h);
                    var fixedRect=Clamp(r,SafeArea(MonitorFromRect(ref r,2)));
                    pos.X=fixedRect.Left;pos.Y=fixedRect.Top;pos.Width=fixedRect.Width;pos.Height=fixedRect.Height;
                    pos.Flags&=~3u;
                }
            }
            if(!SuspendRaise && (pos.Flags&4)==0)pos.InsertAfter=new IntPtr(-1);
            Marshal.StructureToPtr(pos,lparam,false);
        }
        if(message==0x2E0||message==0x7E||message==0x1A)
            owner.Dispatcher.BeginInvoke(()=> {if(!disposed)KeepVisible();});
        return IntPtr.Zero;
    }
    public static NativeRect Clamp(NativeRect rect,NativeRect area,bool dock=false)
    {
        const int gap=8;
        // Keep side/top breathing room, but let the sprite baseline meet the taskbar.
        int availableW=Math.Max(1,area.Width-gap*2),availableH=Math.Max(1,area.Height-gap);
        double shrink=Math.Min(1,Math.Min((double)availableW/Math.Max(1,rect.Width),(double)availableH/Math.Max(1,rect.Height)));
        int w=Math.Max(1,(int)Math.Floor(rect.Width*shrink)),h=Math.Max(1,(int)Math.Floor(rect.Height*shrink));
        int x=dock?area.Left+gap:Math.Clamp(rect.Left,area.Left+gap,Math.Max(area.Left+gap,area.Right-gap-w));
        int y=dock?area.Bottom-h:Math.Clamp(rect.Top,area.Top+gap,Math.Max(area.Top+gap,area.Bottom-h));
        return new NativeRect(x,y,x+w,y+h);
    }
    public static NativeRect SafeArea(IntPtr monitor)
    {
        if(monitor==IntPtr.Zero)monitor=MonitorFromPoint(new NativePoint(0,0),1);
        var info=new MonitorInfo {Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(monitor,ref info))throw new InvalidOperationException("Cannot read the display work area.");
        var work=info.Work;
        for(uint edge=0;edge<4;edge++)
        {
            var data=new AppBarData {Size=(uint)Marshal.SizeOf<AppBarData>(),Edge=edge,Rect=info.Monitor};
            var bar=SHAppBarMessage(11,ref data); // GETAUTOHIDEBAREX, per monitor
            if(bar==UIntPtr.Zero)continue;
            int reserve=48;
            if(GetWindowRect((IntPtr)bar,out var barRect))reserve=edge==0||edge==2?barRect.Width:barRect.Height;
            reserve=Math.Clamp(reserve,32,160);
            if(edge==0)work.Left=Math.Max(work.Left,info.Monitor.Left+reserve);
            if(edge==1)work.Top=Math.Max(work.Top,info.Monitor.Top+reserve);
            if(edge==2)work.Right=Math.Min(work.Right,info.Monitor.Right-reserve);
            if(edge==3)work.Bottom=Math.Min(work.Bottom,info.Monitor.Bottom-reserve);
        }
        return work;
    }
    public static NativeRect Bounds(IntPtr window) {GetWindowRect(window,out var r);return r;}
    public static bool IsTopmost(IntPtr window)=>(GetStyle(window).ToInt64()&8)!=0;
    public static bool DoesNotActivate(IntPtr window)=>(GetStyle(window).ToInt64()&0x08000000)!=0;
    public static void MoveForTest(IntPtr window,NativeRect rect)=>SetWindowPos(window,IntPtr.Zero,rect.Left,rect.Top,rect.Width,rect.Height,0x14);
    public static NativeRect ProposeDragForTest(IntPtr window,NativeRect rect)
    {
        var pointer=Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
        try{Marshal.StructureToPtr(rect,pointer,false);SendMessage(window,0x216,IntPtr.Zero,pointer);return Marshal.PtrToStructure<NativeRect>(pointer);}
        finally{Marshal.FreeHGlobal(pointer);}
    }
    public void Dispose()
    {
        disposed=true;timer.Stop();source.RemoveHook(Hook);
        if(foregroundHook!=IntPtr.Zero)UnhookWinEvent(foregroundHook);
    }
    [StructLayout(LayoutKind.Sequential)] public struct NativeRect
    {
        public int Left,Top,Right,Bottom;
        public int Width=>Right-Left;public int Height=>Bottom-Top;
        public NativeRect(int l,int t,int r,int b){Left=l;Top=t;Right=r;Bottom=b;}
    }
    [StructLayout(LayoutKind.Sequential)] struct NativePoint {public int X,Y;public NativePoint(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [StructLayout(LayoutKind.Sequential)] struct WindowPos {public IntPtr Hwnd,InsertAfter;public int X,Y,Width,Height;public uint Flags;}
    [StructLayout(LayoutKind.Sequential)] struct AppBarData {public uint Size;public IntPtr Hwnd;public uint Callback,Edge;public NativeRect Rect;public IntPtr Param;}
    delegate void WinEventProc(IntPtr hook,uint evt,IntPtr hwnd,int objectId,int childId,uint thread,uint time);
    static IntPtr GetStyle(IntPtr window)=>IntPtr.Size==8?GetWindowLongPtr(window,-20):new IntPtr(GetWindowLong(window,-20));
    static void SetStyle(IntPtr window,IntPtr style){if(IntPtr.Size==8)SetWindowLongPtr(window,-20,style);else SetWindowLong(window,-20,style.ToInt32());}
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref NativeRect rect,uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW")] static extern int SetWindowLong(IntPtr hwnd,int index,int value);
    [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr dll,WinEventProc callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
    [DllImport("shell32.dll")] static extern UIntPtr SHAppBarMessage(uint message,ref AppBarData data);
}
