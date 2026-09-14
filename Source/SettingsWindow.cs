using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;

namespace FableAstra;

public sealed class SettingsWindow : Window
{
    readonly MainWindow owner;
    readonly Settings draft;
    readonly StackPanel appearance=new(), conversation=new(), connections=new();
    readonly CheckBox startup, motion, voices, paused, live, expressions;
    readonly Slider size, volume, lineDelay, breakDelay;
    readonly ComboBox art, topic, fableVoice, astraVoice;
    readonly TextBox fableUrl, fableModel, fableKey, astraUrl, astraModel, astraKey;
    readonly TextBlock error=new() { Foreground=Brushes.Firebrick, TextWrapping=TextWrapping.Wrap, FontSize=12 };
    static readonly Brush Ink=Brush("#302A31"), Muted=Brush("#746A70"), Paper=Brush("#F8F4ED");
    public TabControl Tabs {get;}
    public SettingsWindow(MainWindow companion,string[] voiceNames)
    {
        owner=companion; draft=owner.Config.Clone();
        Title="Fable + Astra · Settings"; Width=640; Height=730;
        MinWidth=560; MinHeight=540; WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=Paper; Foreground=Ink; FontFamily=new FontFamily("Segoe UI"); FontSize=13;
        ShowInTaskbar=true;
        var root=new Grid { Margin=new Thickness(28,24,28,20), Background=Paper };
        root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        Content=root;
        var header=new StackPanel { Margin=new Thickness(0,0,0,22) };
        header.Children.Add(new TextBlock { Text="FABLE CHAN  +  ASTRA CHAN", FontSize=11,FontWeight=FontWeights.Bold,Foreground=Brush("#995537"),Margin=new Thickness(0,0,0,8) });
        header.Children.Add(new TextBlock { Text="A little company.",FontSize=32,FontWeight=FontWeights.SemiBold });
        header.Children.Add(Note("Two personalities. Your own little corner of the desktop."));
        root.Children.Add(header);
        Tabs=new TabControl { Background=Paper, BorderThickness=new Thickness(0),Padding=new Thickness(0,18,0,0) };
        Grid.SetRow(Tabs,1);root.Children.Add(Tabs);
        AddTab("Companions",appearance); AddTab("Conversation",conversation); AddTab("AI connections",connections);

        Section(appearance,"Make yourselves at home");
        startup=Check(appearance,"Start automatically when I sign in to Windows",Startup.Enabled);
        motion=Check(appearance,"Gentle breathing while resting",draft.Motion);
        expressions=Check(appearance,"Act out conversations with matching poses and props",draft.ExpressiveSprites);
        appearance.Children.Add(Note("Always above desktop windows and kept clear of the taskbar, including while dragging. All controls are in the tray icon."));
        size=Range(appearance,"Companion size",.65,1.4,draft.Scale,"×",.05);
        art=Choose(appearance,"Resting artwork",new[]{"Your original artwork","Classic · new illustration","Happy · new illustration","Tea break · new illustration","Talking · new illustration"},Array.IndexOf(new[]{"original","classic","happy","drink","talk"},draft.Artwork));
        var credit=new Border {Background=Brush("#EEE6DD"),CornerRadius=new CornerRadius(8),Padding=new Thickness(15),Margin=new Thickness(0,18,6,12)};
        var creditStack=new StackPanel();credit.Child=creditStack;
        creditStack.Children.Add(new TextBlock {Text="Original art, kept as supplied.",FontWeight=FontWeights.SemiBold});
        creditStack.Children.Add(Note("The source image is bundled unchanged. The default sprites use that drawing with transparent backgrounds and cleaned edges. Artwork credit visible in the source: @thatlev."));
        appearance.Children.Add(credit);

        Section(conversation,"Let them chat");
        paused=Check(conversation,"Pause automatic conversations",draft.Paused);
        topic=Choose(conversation,"Conversation topic",Dialogue.Topics,Math.Max(0,Array.IndexOf(Dialogue.Topics,draft.Topic)));
        lineDelay=Range(conversation,"Time between replies",6,30,draft.LineSeconds," sec",1);
        breakDelay=Range(conversation,"Quiet time between offline conversations",10,300,draft.BreakSeconds," sec",5);
        Section(conversation,"Give them a voice");
        voices=Check(conversation,"Read their lines aloud with Windows voices",draft.Voice);
        volume=Range(conversation,"Voice volume",0,100,draft.Volume,"%",5);
        var options=new[]{"Automatic"}.Concat(voiceNames).ToArray();
        fableVoice=Choose(conversation,"Fable chan's voice",options,Math.Max(0,Array.IndexOf(options,draft.FableVoice)));
        astraVoice=Choose(conversation,"Astra chan's voice",options,Math.Max(0,Array.IndexOf(options,draft.AstraVoice)));
        conversation.Children.Add(Note(voiceNames.Length==0?"No Windows speech voices were detected. Speech bubbles still work.":"Automatic prefers English female voices. If only one matching voice is installed, they share it with slightly different pacing."));
        conversation.Children.Add(Note("Offline mode plays 21 written conversations. Surprise me shuffles the whole collection before repeating."));

        Section(connections,"Connect their minds");
        live=Check(connections,"Use live AI for both companions",draft.LiveAI);
        connections.Children.Add(Note("Leave this off for offline banter. Live mode sends the recent conversation to each character's configured OpenAI-compatible API. Ollama is supported. Use actual model IDs supplied by your provider."));
        Character(connections,"Fable chan",Brush("#A45A34"));
        fableUrl=Field(connections,"API base URL",draft.Fable.Endpoint);
        fableModel=Field(connections,"Model ID",draft.Fable.Model);
        fableKey=Field(connections,"API key environment variable",draft.Fable.KeyVariable);
        Character(connections,"Astra chan",Brush("#785292"));
        astraUrl=Field(connections,"API base URL",draft.Astra.Endpoint);
        astraModel=Field(connections,"Model ID",draft.Astra.Model);
        astraKey=Field(connections,"API key environment variable",draft.Astra.KeyVariable);
        connections.Children.Add(Note("Ollama base URL: http://localhost:11434/v1. For cloud services, use the provider's HTTPS base URL. Put secrets in Windows environment variables, then enter the variable names above. Keys are never stored in preferences."));
        if(!string.IsNullOrEmpty(owner.LastError)) connections.Children.Add(new TextBlock {Text="Last connection issue: "+owner.LastError,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Firebrick,Margin=new Thickness(0,10,8,12)});

        var footer=new Grid {Margin=new Thickness(0,18,0,0)}; Grid.SetRow(footer,2);root.Children.Add(footer);
        footer.ColumnDefinitions.Add(new ColumnDefinition());footer.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        footer.Children.Add(error);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};Grid.SetColumn(buttons,1);footer.Children.Add(buttons);
        var cancel=Button("Cancel",false);cancel.IsCancel=true;cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        var save=Button("Save changes",true);save.IsDefault=true;save.Click+=(_,_)=>Save();buttons.Children.Add(save);
    }
    void Save()
    {
        try
        {
            draft.Topmost=true; draft.Motion=motion.IsChecked==true;draft.ExpressiveSprites=expressions.IsChecked==true;
            draft.Voice=voices.IsChecked==true; draft.Paused=paused.IsChecked==true; draft.LiveAI=live.IsChecked==true;
            draft.Scale=size.Value; draft.Volume=(int)volume.Value;draft.LineSeconds=(int)lineDelay.Value;draft.BreakSeconds=(int)breakDelay.Value;
            draft.Artwork=new[]{"original","classic","happy","drink","talk"}[Math.Max(0,art.SelectedIndex)];
            draft.Topic=topic.SelectedItem?.ToString()??"Surprise me";
            draft.FableVoice=fableVoice.SelectedIndex==0?"":fableVoice.SelectedItem?.ToString()??"";
            draft.AstraVoice=astraVoice.SelectedIndex==0?"":astraVoice.SelectedItem?.ToString()??"";
            draft.Fable=new ModelSettings {Endpoint=fableUrl.Text.Trim(),Model=fableModel.Text.Trim(),KeyVariable=fableKey.Text.Trim()};
            draft.Astra=new ModelSettings {Endpoint=astraUrl.Text.Trim(),Model=astraModel.Text.Trim(),KeyVariable=astraKey.Text.Trim()};
            if(draft.LiveAI)
            {
                Dialogue.ValidateEndpoint(draft.Fable.Endpoint);Dialogue.ValidateEndpoint(draft.Astra.Endpoint);
                if(string.IsNullOrWhiteSpace(draft.Fable.Model)||string.IsNullOrWhiteSpace(draft.Astra.Model)) throw new ArgumentException("Enter a model ID for each companion.");
            }
            owner.Commit(draft,startup.IsChecked==true);Close();
        }
        catch(Exception ex) { error.Text=ex.Message; }
    }
    void AddTab(string title,StackPanel panel)
    {
        panel.Margin=new Thickness(2,16,12,10);
        Tabs.Items.Add(new TabItem {Header=title,Padding=new Thickness(16,9,16,9),Content=new ScrollViewer {Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}});
    }
    static Brush Brush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    static TextBlock Note(string text)=>new() {Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Muted,FontSize=12,LineHeight=18,Margin=new Thickness(0,7,6,6)};
    static void Section(Panel panel,string title)=>panel.Children.Add(new TextBlock {Text=title,FontSize=18,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,10,0,12)});
    static void Character(Panel panel,string name,Brush accent)=>panel.Children.Add(new TextBlock{Text=name,Foreground=accent,FontSize=16,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,22,0,4)});
    static CheckBox Check(Panel panel,string title,bool value)
    {
        var check=new CheckBox {Content=title,IsChecked=value,Margin=new Thickness(0,7,0,7),VerticalContentAlignment=VerticalAlignment.Center};panel.Children.Add(check);return check;
    }
    static Slider Range(Panel panel,string title,double min,double max,double value,string suffix,double tick)
    {
        var label=new TextBlock {Text=title,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,17,0,7)};panel.Children.Add(label);
        var row=new DockPanel();var readout=new TextBlock {Text=value.ToString("0.##")+suffix,Width=60,TextAlignment=TextAlignment.Right,Foreground=Muted};DockPanel.SetDock(readout,Dock.Right);row.Children.Add(readout);
        var slider=new Slider {Minimum=min,Maximum=max,Value=value,TickFrequency=tick,IsSnapToTickEnabled=true,Margin=new Thickness(0,0,14,0)};
        AutomationProperties.SetName(slider,title);
        slider.ValueChanged+=(_,_)=>readout.Text=slider.Value.ToString("0.##")+suffix;row.Children.Add(slider);panel.Children.Add(row);return slider;
    }
    static ComboBox Choose(Panel panel,string title,string[] values,int selected)
    {
        panel.Children.Add(new TextBlock{Text=title,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,16,0,7)});
        var combo=new ComboBox {ItemsSource=values,SelectedIndex=selected,Padding=new Thickness(9,7,9,7),MinHeight=34};AutomationProperties.SetName(combo,title);panel.Children.Add(combo);return combo;
    }
    static TextBox Field(Panel panel,string title,string value)
    {
        panel.Children.Add(new TextBlock{Text=title,FontSize=12,Margin=new Thickness(0,11,0,5)});
        var text=new TextBox{Text=value,Padding=new Thickness(10,7,10,7),MinHeight=34,BorderBrush=Brush("#C8BCB3"),BorderThickness=new Thickness(1)};AutomationProperties.SetName(text,title);panel.Children.Add(text);return text;
    }
    static Button Button(string text,bool primary)=>new() {Content=text,Padding=new Thickness(15,9,15,9),Margin=new Thickness(9,0,0,0),Background=primary?Brush("#382E3C"):Paper,Foreground=primary?Brushes.White:Ink,BorderBrush=Brush("#C8BCB3"),BorderThickness=new Thickness(1),MinWidth=86};
}
