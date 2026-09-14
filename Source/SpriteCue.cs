using System;
using System.Linq;

namespace FableAstra;

// A scene supplies one explicit cue per reply. A slash lets the girls do different things.
public record SpriteCue(string Fable, string Astra)
{
    public static readonly string[] Artwork = { "original", "classic", "happy", "drink", "talk", "soup", "experiment", "bake", "snack", "book", "notes", "space", "garden", "game", "blanket", "cat", "adventure" };
    public static SpriteCue Parse(string value)
    {
        var parts=value.Split('/');
        if(parts.Length is <1 or >2 || parts.Any(p=>p!="rest"&&!Artwork.Contains(p)))
            throw new ArgumentException("Unknown conversation artwork cue: "+value);
        return new(parts[0],parts.Length==2?parts[1]:parts[0]);
    }
}
