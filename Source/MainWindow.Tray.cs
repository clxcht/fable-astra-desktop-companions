using System;
using System.Collections.Generic;
using System.Windows;
using Forms=System.Windows.Forms;

namespace FableAstra;

public partial class MainWindow
{
    Forms.NotifyIcon? tray;
    void BuildTray()
    {
        tray=new Forms.NotifyIcon{Text="Fable chan + Astra chan",Icon=System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)??System.Drawing.SystemIcons.Information,Visible=true};
        tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(Reveal);
        var menu=new Forms.ContextMenuStrip();
        menu.Items.Add("Show companions",null,(_,_)=>Dispatcher.Invoke(Reveal));
        menu.Items.Add("Settings…",null,(_,_)=>Dispatcher.Invoke(OpenSettings));
        menu.Items.Add(new Forms.ToolStripSeparator());
        var pause=menu.Items.Add("Pause conversations",null,(_,_)=>Dispatcher.Invoke(TogglePause));
        var next=menu.Items.Add("Next line",null,async(_,_)=>await Advance());
        menu.Items.Add("New conversation",null,(_,_)=>Dispatcher.Invoke(NewTopic));
        menu.Items.Add("Have a tea break",null,(_,_)=>Dispatcher.Invoke(()=>StartConversation("Tea break")));
        var scenes=new Forms.ToolStripMenuItem("Choose a conversation");
        for(int i=0;i<Dialogue.Scenes.Length;i++)
        {
            int sceneIndex=i;var scene=Dialogue.Scenes[i];
            scenes.DropDownItems.Add(scene.Lines[0],null,(_,_)=>Dispatcher.Invoke(()=>StartConversation(scene.Topic,sceneIndex)));
        }
        menu.Items.Add(scenes);
        var mute=menu.Items.Add("Mute voices",null,(_,_)=>Dispatcher.Invoke(()=>{Config.Voice=!Config.Voice;voice?.Stop();Save();}));
        var expressions=new Forms.ToolStripMenuItem("Change poses with conversation"){Checked=Config.ExpressiveSprites};
        expressions.Click+=(_,_)=>Dispatcher.Invoke(()=>{Config.ExpressiveSprites=!Config.ExpressiveSprites;Sprites.Configure(Config);Save();});
        menu.Items.Add(expressions);
        var artMenu=new Forms.ToolStripMenuItem("Resting artwork");
        var artItems=new Dictionary<string,Forms.ToolStripMenuItem>();
        foreach(var(id,label)in new[]{("original","Your original artwork"),("classic","Classic"),("happy","Happy"),("drink","Tea break"),("talk","Talking")})
        {
            var item=new Forms.ToolStripMenuItem(label);item.Click+=(_,_)=>Dispatcher.Invoke(()=>SetArt(id));
            artItems[id]=item;artMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(artMenu);menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Reset to bottom left",null,(_,_)=>Dispatcher.Invoke(()=>{dragged=false;Reveal();}));
        menu.Items.Add("Hide companions",null,(_,_)=>Dispatcher.Invoke(HideCompanions));
        menu.Items.Add("Quit",null,(_,_)=>Dispatcher.Invoke(()=>Application.Current.Shutdown()));
        menu.Opening+=(_,_)=>
        {
            pause.Text=Config.Paused?"Resume conversations":"Pause conversations";
            mute.Text=Config.Voice?"Mute voices":"Enable voices";next.Enabled=!busy;
            expressions.Checked=Config.ExpressiveSprites;
            foreach(var pair in artItems)pair.Value.Checked=pair.Key==Config.Artwork;
            if(desktop is not null)desktop.SuspendRaise=true;
        };
        menu.Closed+=(_,_)=>{if(desktop is not null)desktop.SuspendRaise=false;};
        tray.ContextMenuStrip=menu;UpdateState();
    }
}
