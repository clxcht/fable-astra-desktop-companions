using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FableAstra;

public partial class MainWindow : Window
{
    public const double DesignWidth=432,DesignHeight=414;
    public Settings Config {get;private set;}
    readonly Dialogue dialogue=new();
    readonly Voice? voice;
    readonly DispatcherTimer timer=new();
    public SpriteDirector Sprites {get;}
    DesktopGuard? desktop;
    CancellationTokenSource? pending;
    DateTime nextAt=DateTime.UtcNow.AddSeconds(2),bubbleUntil=DateTime.MinValue;
    int liveCharacter,requestVersion;
    bool busy,closing,dragged,liveFailed;
    public bool TestMode {get;}
    SettingsWindow? settingsWindow;
    public string LastError {get;private set;}="";
    public MainWindow(Settings settings,bool testMode=false)
    {
        Config=settings;Config.Topmost=true;TestMode=testMode;
        InitializeComponent();Sprites=new SpriteDirector(FableImage,AstraImage,Config);
        if(!testMode)voice=new Voice();
        ApplySettings();
        SourceInitialized+=(_,_)=>desktop=new DesktopGuard(this,!TestMode);
        Loaded+=(_,_)=>{Anchor();Animate();};
        if(!testMode)
        {
            dialogue.NewEpisode("Tea break");
            BuildTray();timer.Interval=TimeSpan.FromMilliseconds(200);
            timer.Tick+=async (_,_)=>
            {
                var now=DateTime.UtcNow;Sprites.Tick(now);
                if(now>=bubbleUntil)SpeechArea.Visibility=Visibility.Collapsed;
                if(!Config.Paused&&IsVisible&&!busy&&now>=nextAt)await Advance();
            };
            timer.Start();
            SystemEvents.DisplaySettingsChanged+=DisplayChanged;
            SystemEvents.UserPreferenceChanged+=PreferencesChanged;
        }
        Closing+=(_,_)=>Cleanup();
    }
    public void ApplySettings()
    {
        Width=DesignWidth*Config.Scale;Height=DesignHeight*Config.Scale;
        Config.Topmost=true;Topmost=true;
        Sprites.Configure(Config);UpdateState();Animate();
        if(IsLoaded)Anchor();
    }
    public static BitmapImage LoadImage(string name)
    {
        var image=new BitmapImage();image.BeginInit();
        image.UriSource=new Uri(Path.Combine(AppContext.BaseDirectory,"Assets",name));
        image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();return image;
    }
    void Animate()
    {
        foreach(var pair in new[]{(FableImage,0.0),(AstraImage,1.4)})
        {
            var transform=(ScaleTransform)pair.Item1.RenderTransform;
            transform.BeginAnimation(ScaleTransform.ScaleYProperty,null);
            if(Config.Motion&&!TestMode&&!Config.Paused&&IsVisible)
                transform.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1,1.008,TimeSpan.FromSeconds(2.8)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,BeginTime=TimeSpan.FromSeconds(pair.Item2)});
        }
    }
    public void Anchor()
    {
        if(desktop is not null){desktop.KeepVisible(!dragged);return;}
        var area=SystemParameters.WorkArea;
        double scale=Math.Min(Config.Scale,Math.Min((area.Width-24)/DesignWidth,(area.Height-24)/DesignHeight));
        Width=DesignWidth*scale;Height=DesignHeight*scale;
        Left=area.Left+12;Top=area.Bottom-Height;
    }
    void DisplayChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(Anchor);
    void PreferencesChanged(object sender,UserPreferenceChangedEventArgs e)=>Dispatcher.BeginInvoke(Anchor);
    void DragCompanions(object sender,MouseButtonEventArgs e)
    {
        if(e.ChangedButton!=MouseButton.Left)return;
        dragged=true;
        try{DragMove();}catch(InvalidOperationException){}
        finally{desktop?.KeepVisible();}
    }
    public void ShowLine(Line line)
    {
        var now=DateTime.UtcNow;double seconds=SpeechSeconds(line.Text);
        bubbleUntil=now.AddSeconds(seconds);SpeechArea.Visibility=Visibility.Visible;
        SpeakerName.Text=line.Character==0?"FABLE CHAN":"ASTRA CHAN";
        SpeakerName.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(line.Character==0?"#964123":"#634488"));
        SpeechText.Text=FitBubbleText(line.Text);SpeechText.ToolTip=line.Text;
        TopicLabel.Text=Config.LiveAI&&!liveFailed?"AI CONVERSATION":dialogue.CurrentTopic.ToUpperInvariant();
        BubbleTail.Margin=new Thickness(line.Character==0?83:295,0,0,0);
        Sprites.Begin(line,dialogue.CurrentTopic,line.Cue is null?seconds:Math.Max(Config.LineSeconds,seconds)+.6,now);
        if(Config.Motion&&!TestMode)Bubble.BeginAnimation(OpacityProperty,new DoubleAnimation(.35,1,TimeSpan.FromMilliseconds(180)));
        voice?.Say(line.Text,line.Character,Config);UpdateState();
    }
    double SpeechSeconds(string text)=>Math.Max(6,text.Split(' ').Length/2.0+2);
    public static string FitBubbleText(string text)
    {
        bool Fits(string value)
        {
            var probe=new TextBlock{Text=value,FontFamily=new FontFamily("Segoe UI"),FontSize=15,TextWrapping=TextWrapping.Wrap,LineHeight=21,LineStackingStrategy=LineStackingStrategy.BlockLineHeight};
            probe.Measure(new Size(370,double.PositiveInfinity));return probe.DesiredSize.Height<=63;
        }
        if(Fits(text))return text;
        int low=1,high=text.Length;
        while(low<high){int middle=(low+high+1)/2;if(Fits(text[..middle]+"…"))low=middle;else high=middle-1;}
        int count=low,space=text.LastIndexOf(' ',low-1);if(space>count-24)count=space;
        return text[..count].TrimEnd()+"…";
    }
    public async Task Advance()
    {
        if(busy||closing)return;
        busy=true;UpdateState();int version=requestVersion;
        try
        {
            Line line;
            if(Config.LiveAI&&!liveFailed)
            {
                pending=new CancellationTokenSource();
                try{line=await dialogue.NextLive(liveCharacter,Config,pending.Token);}
                catch(OperationCanceledException)when(pending.IsCancellationRequested){return;}
                catch(Exception ex)
                {
                    if(version!=requestVersion)return;
                    liveFailed=true;Sprites.Rest();SpeechArea.Visibility=Visibility.Visible;
                    SpeechText.Text=FitBubbleText("The AI connection is unavailable. Offline banter will continue. Open Settings from the tray icon to check it.");
                    TopicLabel.Text="CONNECTION PAUSED";SpeakerName.Text="COMPANIONS";
                    LastError=ex is TaskCanceledException?"The AI request timed out after 45 seconds.":ex.Message;
                    bubbleUntil=nextAt=DateTime.UtcNow.AddSeconds(10);return;
                }
                liveCharacter=1-liveCharacter;
            }
            else line=dialogue.NextOffline(Config.Topic);
            if(version!=requestVersion)return;
            ShowLine(line);
            double seconds=Math.Max(Config.LineSeconds,SpeechSeconds(line.Text));
            nextAt=DateTime.UtcNow.AddSeconds(seconds+(dialogue.AtEnd&&(!Config.LiveAI||liveFailed)?Config.BreakSeconds:0));
        }
        finally{pending?.Dispose();pending=null;busy=false;UpdateState();}
    }
    void CancelPending(){requestVersion++;pending?.Cancel();voice?.Stop();Sprites.Rest();}
    void UpdateState()
    {
        if(tray is not null)tray.Text="Fable + Astra · "+(Config.Paused?"paused":busy?"thinking":Config.LiveAI&&!liveFailed?"live AI":"offline conversations");
    }
    void Save()
    {
        if(TestMode)return;
        try{Config.Save();}catch(Exception ex){MessageBox.Show("Could not save preferences: "+ex.Message,"Fable + Astra");}
    }
    public void TogglePause()
    {
        Config.Paused=!Config.Paused;CancelPending();UpdateState();Animate();
        nextAt=DateTime.UtcNow.AddSeconds(1);Save();
    }
    public void NewTopic()=>StartConversation(Config.Topic);
    public void StartConversation(string topic, int? sceneIndex = null)
    {
        CancelPending();dialogue.NewEpisode(topic,sceneIndex);dialogue.History.Clear();liveCharacter=0;
        nextAt=DateTime.UtcNow.AddSeconds(1);
        if(Config.Paused&&!Config.LiveAI)ShowLine(dialogue.NextOffline(topic));
    }
    public void SetArt(string art){Config.Artwork=art;Sprites.Configure(Config);Save();}
    public void HideCompanions(){CancelPending();Hide();Animate();}
    public void Reveal(){Show();Anchor();Animate();nextAt=DateTime.UtcNow.AddSeconds(2);}
    public void OpenSettings()
    {
        if(settingsWindow is not null){settingsWindow.Activate();return;}
        settingsWindow=new SettingsWindow(this,voice?.Names.ToArray()??Array.Empty<string>());
        settingsWindow.Closed+=(_,_)=>settingsWindow=null;
        settingsWindow.Show();settingsWindow.Activate();
    }
    public void Commit(Settings settings,bool startup)
    {
        settings.Topmost=true;Startup.Set(startup);settings.Save();CancelPending();Config=settings;
        liveFailed=false;LastError="";ApplySettings();
        dialogue.NewEpisode(Config.Topic);nextAt=DateTime.UtcNow.AddSeconds(2);
    }
    void Cleanup()
    {
        if(closing)return;closing=true;timer.Stop();CancelPending();desktop?.Dispose();
        SystemEvents.DisplaySettingsChanged-=DisplayChanged;SystemEvents.UserPreferenceChanged-=PreferencesChanged;
        tray?.Dispose();voice?.Dispose();dialogue.Dispose();
    }
}
