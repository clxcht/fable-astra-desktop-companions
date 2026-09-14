using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace FableAstra;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if(args.Contains("--enable-startup")) { Startup.Set(true); return 0; }
        if(args.Contains("--disable-startup")) { Startup.Set(false); return 0; }
        if(args.Contains("--self-test")) return SelfTest.Run(args);
        using var mutex=new Mutex(true,"Local\\FableAstraCompanions",out bool first);
        using var reveal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\FableAstraReveal");
        if(!first) { reveal.Set(); return 0; }
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        app.DispatcherUnhandledException+=(_,e)=>
        {
            Directory.CreateDirectory(Settings.DataDir);
            File.AppendAllText(Path.Combine(Settings.DataDir,"error.log"),DateTime.Now+" "+e.Exception+Environment.NewLine);
            MessageBox.Show("The companions encountered an error. Details were saved in "+Settings.DataDir,"Fable + Astra");
            app.Shutdown(1);e.Handled=true;
        };
        var window=new MainWindow(Settings.Load());app.MainWindow=window;
        var registered=ThreadPool.RegisterWaitForSingleObject(reveal,(_,_)=>app.Dispatcher.BeginInvoke(window.Reveal),null,Timeout.Infinite,false);
        window.Show();int result=app.Run();registered.Unregister(null);return result;
    }
}
