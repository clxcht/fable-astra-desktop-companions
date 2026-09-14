using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace FableAstra;

public sealed class Voice : IDisposable
{
    dynamic? speaker;
    readonly List<(string name, object token, bool female, bool english)> tokens = new();
    public IReadOnlyList<string> Names => tokens.ConvertAll(t => t.name);
    public Voice()
    {
        try
        {
            var type = Type.GetTypeFromProgID("SAPI.SpVoice");
            if (type is null) return;
            speaker = Activator.CreateInstance(type);
            dynamic voices = speaker!.GetVoices();
            for (int i = 0; i < voices.Count; i++)
            {
                dynamic token = voices.Item(i);
                string gender="",language="";
                try { gender=(string)token.GetAttribute("Gender");language=(string)token.GetAttribute("Language"); } catch { }
                bool english=language.Split(';').Any(code=>int.TryParse(code,System.Globalization.NumberStyles.HexNumber,null,out int languageId)&&(languageId&0x3ff)==9);
                tokens.Add(((string)token.GetDescription(), (object)token,gender.Equals("Female",StringComparison.OrdinalIgnoreCase),english));
            }
            Marshal.ReleaseComObject(voices);
        }
        catch { speaker = null; }
    }
    public void Say(string text, int character, Settings settings)
    {
        if (!settings.Voice || speaker is null) return;
        try
        {
            Stop();
            string wanted = character == 0 ? settings.FableVoice : settings.AstraVoice;
            var selected = tokens.Find(t => t.name == wanted);
            if (selected.token is null && tokens.Count > 0)
            {
                var automatic=tokens.Where(t=>t.female&&t.english).ToList();
                if(automatic.Count==0)automatic=tokens.Where(t=>t.female).ToList();
                if(automatic.Count==0)automatic=tokens;
                selected=automatic[Math.Min(character,automatic.Count-1)];
            }
            if (selected.token is not null) speaker.Voice = selected.token;
            speaker.Volume = settings.Volume;
            speaker.Rate = character == 0 ? 0 : 1;
            speaker.Speak(text, 3); // asynchronous; purge any previous utterance
        }
        catch { }
    }
    public void Stop() { try { speaker?.Speak("", 3); } catch { } }
    public void Dispose()
    {
        Stop();
        foreach (var t in tokens) if (Marshal.IsComObject(t.token)) Marshal.ReleaseComObject(t.token);
        if (speaker is not null && Marshal.IsComObject(speaker)) Marshal.ReleaseComObject(speaker);
    }
}
