using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FableAstra;

public sealed class SpriteDirector
{
    readonly Image fable,astra;
    readonly Dictionary<string,BitmapImage> images=new();
    public string FablePose {get;private set;}="";
    public string AstraPose {get;private set;}="";
    Settings settings;
    int speaker;
    bool speaking,sipping;
    DateTime sipUntil,restAt;
    public SpriteDirector(Image fableImage,Image astraImage,Settings config)
    {fable=fableImage;astra=astraImage;settings=config;Rest();}
    public void Configure(Settings config){settings=config;Rest();}
    public void Rest(){speaking=false;sipping=false;Set(0,settings.Artwork);Set(1,settings.Artwork);}
    void Set(int character,string pose)
    {
        if((character==0?FablePose:AstraPose)==pose)return;
        string file=pose+(character==0?"-fable.png":"-astra.png");
        if(!images.TryGetValue(file,out var bitmap)){bitmap=MainWindow.LoadImage(file);images[file]=bitmap;}
        if(character==0){fable.Source=bitmap;FablePose=pose;}else{astra.Source=bitmap;AstraPose=pose;}
    }
    public void Begin(Line line,string topic,double seconds,DateTime now)
    {
        if(!settings.ExpressiveSprites){Rest();return;}
        speaker=line.Character;speaking=true;restAt=now.AddSeconds(seconds);
        if(line.Cue is not null)
        {
            // Authored actions stay in their pose for the full reply, including listening.
            sipping=false;
            Set(0,Resolve(line.Cue.Fable,0));Set(1,Resolve(line.Cue.Astra,1));
            return;
        }
        bool tea=Regex.IsMatch(line.Text,@"\b(tea|coffee|kettle|mug|mugs|cocoa|sip|sipping|hot chocolate)\b",RegexOptions.IgnoreCase)||topic=="Tea break";
        bool happy=Regex.IsMatch(line.Text,@"\b(laugh|laughing|funny|joke|excellent|delicious|happiness|smile|wonderful)\b",RegexOptions.IgnoreCase);
        sipping=tea;sipUntil=now.AddSeconds(1.8);
        Set(speaker,tea?"drink":happy?"happy":"talk");
        Set(1-speaker,tea?"drink":settings.Artwork);
    }
    string Resolve(string pose,int character)=>pose=="rest"?(character==speaker?"talk":settings.Artwork):pose;
    public void Tick(DateTime now)
    {
        if(!speaking)return;
        if(now>=restAt){Rest();return;}
        if(sipping&&now>=sipUntil){sipping=false;Set(speaker,"talk");}
    }
    public void Preview(string pose){speaking=false;sipping=false;Set(0,pose);Set(1,pose);}
}
