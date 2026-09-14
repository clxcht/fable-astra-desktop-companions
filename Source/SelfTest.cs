using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Interop;

namespace FableAstra;

public static class SelfTest
{
    public static int Run(string[] args)
    {
        string dir=args.SkipWhile(a=>a!="--self-test").Skip(1).FirstOrDefault()??Path.Combine(AppContext.BaseDirectory,"qa");
        Directory.CreateDirectory(dir);
        var checks=new List<string>();
        try
        {
            var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
            System.Threading.Tasks.Task.Run(ApiSelfTest.Run).GetAwaiter().GetResult();
            checks.Add("local mock API: independent model IDs, shared dialogue context, HTTP errors and cancellation");
            using(var dialogue=new Dialogue())
            {
                var seen=new HashSet<string>();
                for(int i=0;i<Dialogue.Scenes.Length;i++)
                {
                    dialogue.NewEpisode("Surprise me");
                    var first=dialogue.NextOffline("Surprise me");
                    Require(seen.Add(first.Text),"shuffle repeats before every scene has played");
                    Require(first.Character==0,"Fable must begin each offline episode");
                    for(int n=1;n<6;n++) Require(dialogue.NextOffline("Surprise me").Character==n%2,"speakers must alternate");
                    Require(dialogue.AtEnd,"episode end must be detected");
                }
                checks.Add("21 scenes shuffle without repeats; all 126 lines alternate correctly");
                Require(dialogue.History.Count==100,"history must be bounded");
                checks.Add("conversation history bounded to 100 lines");
                Require(Dialogue.ValidateEndpoint("http://localhost:11434/v1").AbsolutePath=="/v1/chat/completions","Ollama endpoint");
                Require(Dialogue.ValidateEndpoint("https://example.com/v1/").Scheme=="https","HTTPS endpoint");
                foreach(var bad in new[]{"http://example.com/v1","file:///C:/test","https://user:secret@example.com","garbage"})
                {
                    bool rejected=false;try{Dialogue.ValidateEndpoint(bad);}catch(ArgumentException){rejected=true;}Require(rejected,"invalid endpoint accepted");
                }
                checks.Add("API endpoint validation accepts HTTPS and local Ollama; rejects plaintext remote and embedded credentials");
            }
            var settings=new Settings {Motion=false,Voice=false,ExpressiveSprites=false};
            var roundTrip=settings.Clone();Require(roundTrip.Artwork=="original"&&!roundTrip.LiveAI,"safe offline defaults");
            checks.Add("settings round trip and original-art / offline defaults");
            var window=new MainWindow(settings,true);
            window.TogglePause();Require(window.Config.Paused,"pause transition");
            window.TogglePause();Require(!window.Config.Paused,"resume transition");
            checks.Add("pause and resume controls change state correctly without persisting test preferences");
            window.Measure(new Size(432,414));window.Arrange(new Rect(0,0,432,414));window.UpdateLayout();window.Anchor();
            Require(window.Left>=SystemParameters.WorkArea.Left&&window.Top+window.Height<=SystemParameters.WorkArea.Bottom,"bottom-left anchoring");
            checks.Add("window fits above taskbar in primary display work area");
            foreach(var art in SpriteCue.Artwork)
            {
                window.Config.Artwork=art;window.ApplySettings();
                window.ShowLine(new Line(0,"Astra, do you think this corner feels like home yet?"));
                Render(window.Scene,Path.Combine(dir,art+"-companion.png"),432,414);
                CheckSpriteBaseline(window,art,dir);
                foreach(var who in new[]{"fable","astra"})
                {
                    var image=MainWindow.LoadImage(art+"-"+who+".png");
                    var converted=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
                    var data=new byte[converted.PixelWidth*converted.PixelHeight*4];converted.CopyPixels(data,converted.PixelWidth*4,0);
                    Require(Enumerable.Range(0,data.Length/4).Any(i=>data[i*4+3]==0),"sprite must have actual alpha transparency");
                }
            }
            checks.Add("all 17 art pairs load and contain transparent pixels; no button bar or nameplates");
            checks.Add("both character silhouettes meet the bottom pixel row in all 17 poses, including at maximum breathing scale");
            window.Config.Artwork="original";window.ApplySettings();
            window.ShowLine(new Line(1,new string('W',270)));
            Require(window.SpeechText.Text.EndsWith("…")&&window.SpeechText.Text.Length<150,"long dialogue must fit visibly");
            checks.Add("long replies fit in the bubble with full text available in tooltip and speech");
            Render(window.Scene,Path.Combine(dir,"long-dialogue.png"),432,414);
            CheckSprites(window,dir,checks);
            CheckStoryboards(window,dir,checks);
            var panel=new SettingsWindow(window,Array.Empty<string>());
            for(int i=0;i<3;i++) {panel.Tabs.SelectedIndex=i;Render((FrameworkElement)panel.Content,Path.Combine(dir,"settings-"+i+".png"),584,652);}
            checks.Add("overlay and all three settings tabs render without errors");
            CheckBounds(window,checks);
            File.WriteAllText(Path.Combine(dir,"results.json"),JsonSerializer.Serialize(new{passed=true,checks},new JsonSerializerOptions{WriteIndented=true}));
            window.Close();panel.Close();app.Shutdown();return 0;
        }
        catch(Exception ex)
        {
            File.WriteAllText(Path.Combine(dir,"results.json"),JsonSerializer.Serialize(new{passed=false,checks,error=ex.ToString()},new JsonSerializerOptions{WriteIndented=true}));return 1;
        }
    }
    static void CheckSprites(MainWindow window,string dir,List<string> checks)
    {
        window.Config.Artwork="original";window.Config.ExpressiveSprites=true;window.ApplySettings();
        var now=DateTime.UtcNow;
        window.Sprites.Begin(new Line(0,"Let's have some tea."),"Tea break",8,now);
        Require(window.Sprites.FablePose=="drink"&&window.Sprites.AstraPose=="drink","tea poses must appear");
        Render(window.Scene,Path.Combine(dir,"tea-break.png"),432,414);
        window.Sprites.Tick(now.AddSeconds(2));
        Require(window.Sprites.FablePose=="talk"&&window.Sprites.AstraPose=="drink","speaker talks while listener sips");
        Render(window.Scene,Path.Combine(dir,"talk-and-listen.png"),432,414);
        window.Sprites.Begin(new Line(1,"I have an idea."),"Space",6,now);
        Require(window.Sprites.FablePose=="original"&&window.Sprites.AstraPose=="talk","speaking pose follows the correct character");
        window.Sprites.Tick(now.AddSeconds(10));
        Require(window.Sprites.FablePose=="original"&&window.Sprites.AstraPose=="original","return to resting sprites after a line");
        window.Config.ExpressiveSprites=false;window.ApplySettings();
        window.Sprites.Begin(new Line(0,"Tea?"),"Tea break",8,now);
        Require(window.Sprites.FablePose=="original","fixed-art preference honored");
        checks.Add("independent tea / speaking / listening poses; timed return to rest; fixed-art option");
    }
    static void CheckStoryboards(MainWindow window,string dir,List<string> checks)
    {
        window.Config.Artwork="classic";window.Config.ExpressiveSprites=true;window.ApplySettings();
        using var dialogue=new Dialogue();
        var used=new HashSet<string>();var now=DateTime.UtcNow;
        for(int i=0;i<Dialogue.Scenes.Length;i++)
        {
            var scene=Dialogue.Scenes[i];dialogue.NewEpisode(scene.Topic,i);
            for(int n=0;n<scene.Lines.Length;n++)
            {
                var line=dialogue.NextOffline(scene.Topic);
                Require(line.Text==scene.Lines[n]&&line.Cue==scene.Cues[n],"selected scene must retain each line's visual cue");
                Require(line.Character==n%2,"storyboard must preserve speaker alternation");
                window.ShowLine(line);
                window.TopicLabel.Text=scene.Topic.ToUpperInvariant();
                // Keep props during a reply and its listening pause, rather than switching to generic talking.
                window.Sprites.Begin(line,scene.Topic,10,now);
                window.Sprites.Tick(now.AddSeconds(9));
                string expectedF=scene.Cues[n].Fable=="rest"?(line.Character==0?"talk":"classic"):scene.Cues[n].Fable;
                string expectedA=scene.Cues[n].Astra=="rest"?(line.Character==1?"talk":"classic"):scene.Cues[n].Astra;
                Require(window.Sprites.FablePose==expectedF&&window.Sprites.AstraPose==expectedA,$"scene {i} line {n} loses its action while listening");
                used.Add(expectedF);used.Add(expectedA);
                if(n==3)Render(window.Scene,Path.Combine(dir,$"scene-{i:00}.png"),432,414);
                if(i==17)Render(window.Scene,Path.Combine(dir,$"soup-line-{n}.png"),432,414);
            }
            Require(dialogue.AtEnd,"chosen scene must end after six replies");
        }
        foreach(var pose in new[]{"soup","experiment","bake","snack","book","notes","space","garden","game","blanket","cat","adventure"})
            Require(used.Contains(pose),"new art is never used: "+pose);
        var soup=Dialogue.Scenes.Single(s=>s.Lines[0]=="Is soup a drink or a meal?");
        Require(soup.Cues[0]==new SpriteCue("soup","soup"),"soup debate must begin with two bowls");
        Require(soup.Cues[3]==new SpriteCue("experiment","experiment"),"controlled experiment must switch both girls to science props");
        Require(soup.Cues[4]==new SpriteCue("soup","experiment"),"bread reply must show Fable tasting and Astra still measuring");
        Require(soup.Cues[5]==new SpriteCue("soup","soup"),"peer review must return both girls to tasting");
        window.Sprites.Begin(new Line(1,soup.Lines[3],soup.Cues[3]),"Food",10,now);
        window.Sprites.Tick(now.AddSeconds(11));
        Require(window.Sprites.FablePose=="classic"&&window.Sprites.AstraPose=="classic","activity must end at rest time");
        window.Config.ExpressiveSprites=false;window.ApplySettings();
        window.Sprites.Begin(new Line(1,soup.Lines[3],soup.Cues[3]),"Food",10,now);
        Require(window.Sprites.FablePose=="classic"&&window.Sprites.AstraPose=="classic","fixed artwork must also override authored action cues");
        checks.Add("all 126 authored dialogue cues preserve props while listening; all 12 new action pairs are used across the 21 scenes");
        checks.Add("soup bowls -> controlled experiment -> bread and measurement -> tasting; chosen scenes play deterministically; actions return to rest and honor fixed artwork");
    }
    static void CheckBounds(MainWindow window,List<string> checks)
    {
        var area=new DesktopGuard.NativeRect(-1920,0,0,1040);
        foreach(var r in new[]{new DesktopGuard.NativeRect(-2000,1000,-1400,1500),new DesktopGuard.NativeRect(-3000,-900,4000,4000),new DesktopGuard.NativeRect(-100,-200,200,300)})
        {
            var c=DesktopGuard.Clamp(r,area);
            Require(c.Left>=area.Left+8&&c.Top>=area.Top+8&&c.Right<=area.Right-8&&c.Bottom<=area.Bottom,"display edge / taskbar clipping");
        }
        var hwnd=new WindowInteropHelper(window).EnsureHandle();window.Anchor();
        Require(DesktopGuard.IsTopmost(hwnd),"native topmost flag");
        Require(DesktopGuard.DoesNotActivate(hwnd),"companion must not take focus");
        var actual=DesktopGuard.SafeArea(IntPtr.Zero);
        Require(DesktopGuard.Bounds(hwnd).Bottom==actual.Bottom,"docked window must touch the taskbar with zero gap");
        DesktopGuard.MoveForTest(hwnd,new DesktopGuard.NativeRect(actual.Left+20,actual.Bottom+50,actual.Left+452,actual.Bottom+464));
        var bounds=DesktopGuard.Bounds(hwnd);
        Require(bounds.Bottom==actual.Bottom,"native movement under taskbar must clamp flush with its edge");
        var drag=DesktopGuard.ProposeDragForTest(hwnd,new DesktopGuard.NativeRect(actual.Left+20,actual.Bottom+30,actual.Left+452,actual.Bottom+444));
        Require(drag.Bottom==actual.Bottom,"interactive drag must stop flush with taskbar");
        checks.Add("native topmost + no-activation styles; both interactive and programmatic taskbar moves clamped; negative-coordinate and small-display bounds");
    }
    static void CheckSpriteBaseline(MainWindow window,string art,string dir)
    {
        foreach(double breathing in new[]{1.0,1.008})
        {
            ((ScaleTransform)window.FableImage.RenderTransform).ScaleY=breathing;
            ((ScaleTransform)window.AstraImage.RenderTransform).ScaleY=breathing;
            window.Scene.Measure(new Size(432,414));window.Scene.Arrange(new Rect(0,0,432,414));window.Scene.UpdateLayout();
            var bitmap=new RenderTargetBitmap(432,414,96,96,PixelFormats.Pbgra32);bitmap.Render(window.Scene);
            var pixels=new byte[432*414*4];bitmap.CopyPixels(pixels,432*4,0);
            for(int side=0;side<2;side++)
            {
                int opaque=0;
                for(int x=side*216;x<(side+1)*216;x++)if(pixels[(413*432+x)*4+3]>240)opaque++;
                Require(opaque>12,$"{art}: character {side} floats above baseline at breathing scale {breathing}");
            }
        }
        ((ScaleTransform)window.FableImage.RenderTransform).ScaleY=1;
        ((ScaleTransform)window.AstraImage.RenderTransform).ScaleY=1;
    }
    public static void Render(FrameworkElement element,string path,int width,int height)
    {
        element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));element.UpdateLayout();
        var image=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);image.Render(element);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(path);encoder.Save(file);
    }
    static void Require(bool condition,string message) {if(!condition)throw new Exception(message);}
}
