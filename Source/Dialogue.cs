using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FableAstra;

public record Line(int Character, string Text, SpriteCue? Cue = null);
public record Episode(string Topic, string[] Lines, string Storyboard)
{
    public SpriteCue[] Cues {get;} = ParseCues(Lines, Storyboard);
    static SpriteCue[] ParseCues(string[] lines, string storyboard)
    {
        var cues=storyboard.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(SpriteCue.Parse).ToArray();
        if(cues.Length!=lines.Length)throw new ArgumentException("Every dialogue line needs an artwork cue.");
        return cues;
    }
}

public sealed class Dialogue : IDisposable
{
    readonly Random random = new();
    readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
    readonly Queue<int> bag = new();
    Episode? episode;
    int index;
    int lastEpisode = -1;
    public readonly List<Line> History = new();
    public bool AtEnd => episode is not null && index >= episode.Lines.Length;
    public string CurrentTopic => episode?.Topic ?? "A little company";
    public static string[] Topics => new[] { "Surprise me" }.Concat(Scenes.Select(e => e.Topic).Distinct()).ToArray();
    public void NewEpisode(string topic, int? sceneIndex = null)
    {
        int chosen;
        var matches = Scenes.Select((e,i)=>(e,i)).Where(x=>x.e.Topic==topic).Select(x=>x.i).ToArray();
        if(sceneIndex is not null)
        {
            if(sceneIndex<0||sceneIndex>=Scenes.Length)throw new ArgumentOutOfRangeException(nameof(sceneIndex));
            chosen=sceneIndex.Value;
        }
        else if(matches.Length > 0) chosen = matches[random.Next(matches.Length)];
        else
        {
            if(bag.Count == 0)
            {
                var values = Enumerable.Range(0, Scenes.Length).OrderBy(_=>random.Next()).ToList();
                if(values[0]==lastEpisode) (values[0],values[1])=(values[1],values[0]);
                foreach(var v in values) bag.Enqueue(v);
            }
            chosen=bag.Dequeue();
        }
        lastEpisode=chosen; episode=Scenes[chosen]; index=0;
    }
    public Line NextOffline(string topic)
    {
        if(episode is null || AtEnd) NewEpisode(topic);
        var line = new Line(index % 2, episode!.Lines[index], episode.Cues[index]);
        index++;
        Remember(line); return line;
    }
    public void Remember(Line line) { History.Add(line); if(History.Count > 100) History.RemoveAt(0); }
    public static Uri ValidateEndpoint(string endpoint)
    {
        if(!Uri.TryCreate(endpoint.Trim().TrimEnd('/')+"/chat/completions",UriKind.Absolute,out var uri)
           || (uri.Scheme!="https" && !(uri.Scheme=="http" && uri.IsLoopback)) || !string.IsNullOrEmpty(uri.UserInfo)
           || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Use an HTTPS API base URL, or http://localhost:11434/v1 for Ollama.");
        return uri;
    }
    public async Task<Line> NextLive(int character, Settings settings, CancellationToken token)
    {
        var model=character==0?settings.Fable:settings.Astra;
        if(string.IsNullOrWhiteSpace(model.Model)) throw new InvalidOperationException("Enter a model ID for both characters in AI connections.");
        var uri=ValidateEndpoint(model.Endpoint);
        var name=character==0?"Fable chan":"Astra chan";
        var other=character==0?"Astra chan":"Fable chan";
        var persona=character==0?"You are warm, imaginative, curious, and gently teasing. You love stories, cozy things, and turning small moments into adventures.":"You are thoughtful, a little mischievous, and fascinated by space, puzzles, and cats. Your humor is dry but affectionate.";
        var transcript=string.Join("\n",History.TakeLast(14).Select(l=>(l.Character==0?"Fable chan":"Astra chan")+": "+l.Text));
        var payload=new {
            model=model.Model,
            messages=new[]{
                new {role="system",content=$"You are {name}, a fictional adult anime desktop companion chatting with your friend {other}. {persona} Reply to your friend's last thought with one or two natural sentences, at most 40 words. Only speak as yourself. Do not include a name prefix, markdown, stage directions, or roleplay a user. Never claim to see the user's screen or files. Topic: {settings.Topic}."},
                new {role="user",content=transcript.Length==0?"Start a friendly conversation with your companion.":"Conversation so far:\n"+transcript+"\nContinue with your next reply."}
            },max_tokens=140,stream=false
        };
        using var request=new HttpRequestMessage(HttpMethod.Post,uri) { Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json") };
        var variable=model.KeyVariable?.Trim();
        if(!string.IsNullOrEmpty(variable))
        {
            var key=Environment.GetEnvironmentVariable(variable) ?? Environment.GetEnvironmentVariable(variable,EnvironmentVariableTarget.User);
            if(!string.IsNullOrWhiteSpace(key)) request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key.Trim());
        }
        using var response=await client.SendAsync(request,token);
        if(!response.IsSuccessStatusCode) throw new InvalidOperationException($"The AI connection returned HTTP {(int)response.StatusCode}. Check the URL, model ID and API key variable.");
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var content=doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
        if(string.IsNullOrEmpty(content)) throw new InvalidOperationException("The model returned an empty reply.");
        if(content.StartsWith(name+":",StringComparison.OrdinalIgnoreCase)) content=content[(name.Length+1)..].Trim();
        if(content.Length>280) { int space=content.LastIndexOf(' ',276); content=content[..(space>180?space:276)]+"…"; }
        var line=new Line(character,content); Remember(line); return line;
    }
    public void Dispose()=>client.Dispose();
    public static readonly Episode[] Scenes = {
        new("Tea break", new[]{"Astra, tea break? I found a mug that matches my hair.","And mine matches a small galaxy. Extremely important selection criteria.","Careful, it's hot. Tiny sips first.","A sensible plan. I'm giving this tea my full scientific attention.","You mean you're enjoying it?","Very much. Please record that as the official finding."}, "drink drink drink drink drink drink"),
        new("A little company", new[]{"Astra, do you think this corner feels like home yet?", "It has you, me, and a suspiciously large taskbar. Promising.", "I'm putting an imaginary kettle on.", "Then I'll calculate the optimal biscuit-to-tea ratio.", "The answer is always one more biscuit.", "Your methods are questionable. Your results are excellent."}, "rest rest drink drink/snack snack snack"),
        new("Space", new[]{"If we could name a tiny planet, what would you call it?", "Crumb. Small, rocky, inexplicably near the keyboard.", "Planet Crumb needs a library and a little bakery.", "And an observatory. We should keep an eye on the space pigeons.", "Space pigeons? You invented those just now.", "Every discovery has to start somewhere."}, "space space book/bake space space space"),
        new("Cozy things", new[]{"Rain on a window is such a good sound.", "A million tiny percussionists with no rehearsal schedule.", "I'd read a book beside it. Something with a secret garden.", "I'd pretend to read while checking whether you reached the good bit.", "You'd spoil the twist with your expression.", "This is why I require a strategically placed mug."}, "blanket blanket book book book book/drink"),
        new("Games", new[]{"Would you rather fight one enormous duck or a hundred tiny dragons?", "Are the dragons open to negotiation?", "They want your snacks and half the desktop.", "Then the duck. Its paperwork will be simpler.", "I would make friends with the dragons.", "I know. I've already ordered a hundred tiny name tags."}, "adventure adventure snack/adventure adventure adventure adventure"),
        new("Creativity", new[]{"I want to write a story where the moon gets lost.", "Lost as in orbit, or lost as in taking the wrong bus?", "The wrong bus. It ends up at a seaside cafe.", "The tides would leave several strongly worded messages.", "But it finally gets a night off.", "All right. Give the moon a hot chocolate. I'll cover its shift."}, "notes/space notes/space book/space book/space book/space book/drink"),
        new("Tiny victories", new[]{"Today's quest: notice one tiny good thing.", "I nominate that very serious curl on top of your head.", "That's my antenna for receiving excellent ideas.", "Interesting. Mine mostly receives snack-related interference.", "Then our next excellent idea is a snack break.", "The network is functioning perfectly."}, "notes happy happy happy/snack snack snack"),
        new("Cats", new[]{"Does your tail have its own opinions?", "Yes. Most of them concern insufficient attention.", "Could it help us choose what to talk about?", "It has voted to knock the topic list off a table.", "A bold editorial direction.", "Very concise. Very difficult to appeal."}, "talk talk talk/notes notes notes happy/notes"),
        new("Puzzles", new[]{"I have a riddle: what gets bigger when you share it?", "A version-control conflict?", "I was going to say happiness.", "Ah. Your answer has better documentation.", "Sometimes a riddle can just be sweet, Astra.", "Understood. Updating my expectations. And accepting the happiness."}, "notes notes happy/notes happy/notes happy happy"),
        new("Food", new[]{"What would a constellation taste like?", "Depends. Orion probably has too much belt-shaped pasta.", "I'd make the little stars out of sugar.", "Then the Milky Way is already halfway to dessert.", "We're opening a cosmic bakery.", "I'll handle the gravity. You handle the frosting."}, "space space bake bake bake bake"),
        new("Quiet moments", new[]{"We don't have to fill every moment with words.", "Agreed. A little quiet has excellent acoustics.", "We can just sit here and keep someone company.", "Like two very opinionated bookmarks.", "Bookmarks that remind you where the nice parts are.", "That's a good job. Let's keep it."}, "blanket blanket blanket book book book"),
        new("Space", new[]{"Would you live in a little house on the moon?", "Only if the windows had a good Earth view.", "I'd grow flowers in the greenhouse.", "I'd label the airlock: please do not let the atmosphere out.", "You'd make a very responsible moon roommate.", "Until you discovered my emergency cheese collection."}, "space space garden garden/notes garden/notes garden/snack"),
        new("Games", new[]{"If life had save points, mine would look like a cozy bench.", "Mine would be a glowing terminal with one reassuring button.", "The button says: you're doing fine.", "And the bench restores your energy without asking questions.", "Let's put them next to each other.", "A very efficient little checkpoint. I approve."}, "game game game game game game"),
        new("Creativity", new[]{"Do you ever get an idea that's too big to start?", "Frequently. I put it in a smaller imaginary box.", "How small?", "Small enough that the first step fits on a sticky note.", "Mine says: write the first sentence.", "Excellent. The enormous idea has been outmaneuvered."}, "notes notes notes notes notes notes"),
        new("Cozy things", new[]{"I think every blanket has a secret personality.", "The fluffy ones are clearly retired clouds.", "The heavy ones are dependable bodyguards.", "And the tiny ones exist solely for cats who reject expensive beds.", "Which kind would you be?", "A retired cloud with very strong opinions about bedtime."}, "blanket blanket blanket blanket/cat blanket blanket"),
        new("Tiny victories", new[]{"I finished an imaginary to-do list.", "An impressive feat. What was on it?", "Say hello. Be kind. Find a better pencil.", "Two accomplished. One ongoing adventure.", "The pencil quest has several side missions.", "Naturally. No good stationery quest is ever linear."}, "notes notes notes notes notes notes"),
        new("Puzzles", new[]{"Quick: invent a completely useless superpower.", "Knowing exactly how many spoons are nearby.", "That would be wonderful at a picnic.", "Then I have failed the assignment spectacularly.", "Mine is making dramatic music whenever I open a cupboard.", "That is also useful. The biscuits deserve an entrance."}, "notes soup soup/snack notes/snack snack snack"),
        new("Food", new[]{"Is soup a drink or a meal?", "It depends whether you're carrying it confidently.", "That can't be the scientific answer.", "Fine. We need two bowls and a controlled experiment.", "And bread. For structural support.", "Peer review will be delicious."}, "soup soup soup experiment soup/experiment soup"),
        new("Cats", new[]{"I saw an imaginary cat sleeping on an imaginary keyboard.", "Excellent security. No one can access the keyboard.", "What if we need to type something?", "We submit a request and wait three to five business naps.", "Can we expedite it with treats?", "That's the only supported authentication method."}, "cat cat cat cat cat cat"),
        new("Quiet moments", new[]{"What's your favorite part of a long day?", "That moment when you decide you've done enough.", "When your shoulders finally drop a little.", "And the unfinished things agree to wait until tomorrow.", "We should remind each other of that.", "Consider it a standing agreement, Fable."}, "blanket blanket blanket blanket blanket blanket"),
        new("A little company", new[]{"Do you think we make a good team?", "You bring the stories. I bring unnecessary star facts.", "No star fact is unnecessary.", "Careful. That's how you get a three-hour lecture about nebulae.", "I'll bring tea and ask very enthusiastic questions.", "Then yes. An exceptionally good team."}, "rest book/space space space drink/space drink")
    };
}
