using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.ArtLab;

public sealed record ItemIconRecipe(string CatalogId, string Material, string Quality, string[] Prefixes, string[] Suffixes, int Seed = 0);
public sealed record ItemIconFrame(IReadOnlyList<uint> Pixels, IReadOnlyList<string> VisibleAffixes, IReadOnlyList<string> SuppressedAffixes);

/// <summary>Deterministic 16×16 inventory compositor. No file IO, random global state, or gameplay mutation during rendering.</summary>
public static class ItemIconRenderer
{
    public const int Size = 16;
    public const int Version = 1;
    private const uint Ink = 0xff101317, Grip = 0xff805336, Trim = 0xffc5c8cc;
    private static readonly Dictionary<string, ItemIconFrame> cache = new(StringComparer.Ordinal);
    private static readonly Queue<string> cacheOrder = new();
    private static readonly object gate = new();
    public static readonly IReadOnlyDictionary<string, uint[]> Materials = new Dictionary<string, uint[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Bone"]=[0xffffedbe,0xffdac69c,0xffaa9272], ["Steel"]=[0xffd9e4ed,0xff909fad,0xff5b687b],
        ["Iron"]=[0xff98a2b2,0xff596274,0xff333b4b], ["Bronze"]=[0xffffce8c,0xffbc8050,0xff805037],
        ["Gold"]=[0xffffed83,0xffe7b334,0xffa47128], ["Mithril"]=[0xffe2fbff,0xff87cee9,0xff4e8ba9],
        ["Glass"]=[0xffdbffff,0xff69cddd,0xff367487], ["Obsidian"]=[0xff9583bd,0xff4e3c6d,0xff292539],
        ["Shadow"]=[0xff8a78df,0xff41336f,0xff24233d], ["Willow"]=[0xffd5bb83,0xff9d7945,0xff624c34],
        ["Silver"]=[0xfff9f8ee,0xffc7cbd0,0xff8b919e], ["Crystal"]=[0xffecdbff,0xffb396ef,0xff7058b3],
        ["Stone"]=[0xffc8bdb0,0xff938a82,0xff635e5e], ["Unknown"]=[0xffc4ced0,0xff828b92,0xff535966],
        ["Strange"]=[0xffaff3d9,0xff4ca9a3,0xff3d676c], ["Celestial"]=[0xffffffd5,0xffead8a0,0xffaa986a],
        ["Wood"]=[0xffd4a86d,0xffa07143,0xff6b452d], ["Leather"]=[0xffc39460,0xff916240,0xff593d30],
        ["Cloth"]=[0xff91a9cc,0xff637ea7,0xff3f526f]
    };
    public static uint StableHash(string value)
    {
        uint hash = 2166136261;
        foreach (char c in value) { hash ^= c; hash = unchecked(hash * 16777619); }
        return hash;
    }
    public static ItemIconRecipe ForItem(Item item)
    {
        var entry = ItemIconCatalog.Resolve(item);
        var mods = item.Modifications ?? [];
        string material = !string.IsNullOrWhiteSpace(item.Material) ? item.Material : mods.FirstOrDefault(m => string.Equals(m.PrefixCategory,"Material", StringComparison.OrdinalIgnoreCase))?.Name ?? entry.DefaultMaterial;
        return new(entry.Id, material, mods.FirstOrDefault(m => string.Equals(m.PrefixCategory,"Quality", StringComparison.OrdinalIgnoreCase))?.Name ?? "",
            mods.Where(m => string.IsNullOrWhiteSpace(m.PrefixCategory) || string.Equals(m.PrefixCategory,"Adjective", StringComparison.OrdinalIgnoreCase)).Select(m => m.Name).ToArray(),
            (item.StatBonuses ?? []).Select(s => s.Name).ToArray());
    }
    public static string Key(ItemIconRecipe recipe) => Version + ":" + JsonSerializer.Serialize(Normalize(recipe));
    private static ItemIconRecipe Normalize(ItemIconRecipe r) => r with
    {
        Material = (r.Material ?? "").Trim().ToLowerInvariant(), Quality = (r.Quality ?? "").Trim().ToLowerInvariant(),
        Prefixes = Canonical(r.Prefixes), Suffixes = Canonical(r.Suffixes)
    };
    private static string[] Canonical(IEnumerable<string>? names) => (names ?? []).Where(n => !string.IsNullOrWhiteSpace(n))
        .Select(n => n.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    public static ItemIconFrame Render(Item item) => Render(ForItem(item), ItemIconCatalog.Resolve(item));
    public static ItemIconFrame Render(ItemIconRecipe recipe, ItemIconCatalog.Entry? custom = null)
    {
        recipe = Normalize(recipe);
        var entry = ItemIconCatalog.Items.FirstOrDefault(e => e.Id == recipe.CatalogId) ?? custom
            ?? new ItemIconCatalog.Entry(recipe.CatalogId, "Unknown item", "wand", 1, "charm", "Unknown");
        string key = Key(recipe) + ":" + entry.Shape + ":" + entry.Name;
        lock (gate)
        {
            if (cache.TryGetValue(key, out var frame)) return frame;
            frame = Compose(recipe, entry);
            if (cache.Count >= 512) cache.Remove(cacheOrder.Dequeue());
            cache[key] = frame; cacheOrder.Enqueue(key);
            return frame;
        }
    }
    private static ItemIconFrame Compose(ItemIconRecipe r, ItemIconCatalog.Entry entry)
    {
        var mask = new char[256];
        var pixels = new uint[256];
        var protectedWear = new bool[256];
        bool writingWear = false;
        string[] shape = ItemIconShapes.All.TryGetValue(entry.Shape, out var rows) ? rows : ItemIconShapes.All["charm"];
        int width = shape.Max(s => s.Length), left = (16-width)/2, top = (16-shape.Length)/2;
        for(int y=0;y<shape.Length;y++) for(int x=0;x<shape[y].Length;x++) if(shape[y][x]!='.') mask[(y+top)*16+x+left]=shape[y][x];
        uint[] palette = Materials.TryGetValue(r.Material, out var p) ? p : Materials["Unknown"];
        bool weapon = entry.Family is "sword" or "dagger" or "mace" or "wand";
        bool soft = entry.Shape is "hood" or "cap" or "shirt" or "coat" or "pants" or "wrap" or "shoe" or "sandals";
        uint variant = StableHash(entry.Id + ":" + r.Seed);
        // Family-specific fittings vary without changing the material's main surface.
        uint fitting = entry.Tier >= 4 ? 0xffd7bd72 : Trim;
        uint grip = entry.Shape == "pants" && r.Material == "cloth" ? 0xff7a6543 : Grip;
        if (entry.Family == "consumable")
        {
            palette = entry.Shape switch
            {
                "apple" => [0xffff9b80,0xffd55251,0xff893d45], "cheese" => Materials["Gold"],
                "bread" => Materials["Wood"], "meat" => [0xffda9270,0xffa15648,0xff693c3c],
                _ => StatPalette(ItemIconCatalog.Affixes.FirstOrDefault(a => a.Kind == "suffix" && entry.Name.Contains(a.Stat, StringComparison.OrdinalIgnoreCase) && a.Stat.Length > 0)?.Stat ?? entry.Name)
            };
        }
        foreach(string prefix in r.Prefixes)
        {
            // Structural changes operate before damage; this prevents later reinforcement from healing wear.
            if (prefix == "short" && weapon) for(int i=0;i<256;i++) if(i/16 < top+2 && IsMain(mask[i])) mask[i]='\0';
            if (prefix is "heavy" or "reinforced" or "sturdy")
            {
                var copy=(char[])mask.Clone();
                for(int y=1;y<15;y++) for(int x=1;x<15;x++) if(copy[y*16+x]=='G' && copy[y*16+x+1]=='\0') mask[y*16+x+1]='T';
                if(prefix=="heavy" && entry.Family is "sword" or "dagger")
                    for(int y=2;y<13;y++)for(int x=2;x<13;x++)if(copy[y*16+x]=='M'&&copy[y*16+x+1]=='\0')mask[y*16+x+1]='S';
                if(prefix=="reinforced" && entry.Family=="chest")
                    foreach(int x in new[]{left+1,left+width-2})for(int y=top;y<top+2;y++)mask[y*16+x]='T';
            }
            if(prefix == "long" && weapon)
            {
                int tip=Array.FindIndex(mask,IsMain);
                if(tip>=17 && tip%16<14) mask[tip-15]='H';
            }
            if(prefix == "light") for(int i=0;i<256;i++) if(mask[i]=='S' && i%2==0) mask[i]='\0';
            if(prefix == "serrated") for(int y=2;y<13;y+=3) for(int x=2;x<13;x++) if(mask[y*16+x]=='M' && mask[y*16+x-1]=='\0') {mask[y*16+x-1]='S';break;}
        }
        for(int i=0;i<256;i++) pixels[i]=mask[i] switch {'H'=>palette[0], 'M'=>palette[1], 'S'=>palette[2], 'G'=>grip, 'T'=>fitting, _=>0};
        if(r.Material == "glass" && entry.Family != "consumable") for(int i=0;i<256;i++) if(mask[i]=='M' && i%3==0) pixels[i]=0;
        if(r.Material == "strange") MarkMain(2,0xffe5847c);
        if(r.Material == "unknown") MarkMain(1,0xffe1dcb7);
        if(entry.Tier > 1) // Small authored-slot variant per catalog row, never random noise over the surface.
        {
            int[] trims=Enumerable.Range(0,256).Where(i=>mask[i]=='T').ToArray();
            if(trims.Length>0) pixels[trims[(int)(variant%(uint)trims.Length)]]=entry.Tier>=4 ? 0xfff5d976 : palette[0];
        }
        var visible = new List<string>(); var suppressed = new List<string>();
        var accents = new List<(string Name,uint Color,string Motif)>();
        foreach(string prefix in r.Prefixes)
        {
            visible.Add(prefix);
            switch(prefix)
            {
                case "hardened": case "brutal": for(int i=0;i<256;i++) if(mask[i]=='S') pixels[i]=0xff323747; break;
                case "keen": case "precise": for(int i=0;i<256;i++) if(mask[i]=='H') pixels[i]=0xfff3fcff; break;
                case "ancient": for(int i=0;i<256;i++) if(mask[i]=='T') pixels[i]=0xffa89968; break;
            }
            if(prefix is not ("heavy" or "short" or "long" or "light" or "serrated" or "reinforced" or "sturdy" or "hardened" or "keen"))
                accents.Add((prefix, PrefixColor(prefix), PrefixMotif(prefix)));
        }
        // Quality owns the wear layer; overlays are applied after all structural prefix operations.
        writingWear = true;
        switch(r.Quality)
        {
            case "broken":
                int tip=Array.FindIndex(mask,IsMain);
                if(tip>=0) for(int i=0;i<256;i++) if(IsMain(mask[i]) && i/16<=tip/16+1) {pixels[i]=0; mask[i]='\0';}
                MarkMain(2,palette[2]); break;
            case "battle scarred": MarkMain(2,Ink); break;
            case "worn": MarkMain(1,palette[2]); FadeGrip(); break;
            case "second hand": MarkMain(1,soft ? 0xffd7b789 : palette[2]); FadeGrip(); break;
            case "preowned": FadeGrip(); break;
            case "like new": case "new": break;
            case "perfect": for(int i=0;i<256;i++) if(mask[i]=='H') pixels[i]=palette[0]; break;
            case "masterwork": for(int i=0;i<256;i++) if(mask[i]=='T') pixels[i]=0xffedd366; break;
            case "heirloom": for(int i=0;i<256;i++) {if(mask[i]=='T') pixels[i]=0xffc5ae6d;if(mask[i]=='G' && i%2==0) pixels[i]=0xff8e4b5a;} break;
            case "cosmic": accents.Insert(0,("cosmic",0xffc493fa,"spark")); break;
        }
        // Outline only opaque components. Alpha remains genuinely transparent outside the silhouette.
        uint[] filled=(uint[])pixels.Clone();
        for(int y=1;y<15;y++) for(int x=1;x<15;x++) if(filled[y*16+x]!=0)
            foreach(int d in new[]{-16,16,-1,1}) if(pixels[y*16+x+d]==0 && mask[y*16+x+d]=='\0') pixels[y*16+x+d]=Ink;
        if(r.Material is "shadow" or "celestial") accents.Insert(0,(r.Material,r.Material=="shadow"?0xff8872d4:0xffe7ce79,"spark"));
        foreach(string suffix in r.Suffixes)
        {
            var art = SuffixStyle(suffix);
            accents.Add((suffix,art.Color,art.Motif));
        }
        // One effect, two local details. Unshown properties remain available in tooltips and the lab recipe.
        int detail=0; bool effect=false;
        foreach(var accent in accents)
        {
            bool fx=accent.Motif is "spark" or "flame" or "speed" or "bolt";
            if(fx ? effect : detail>=2) {suppressed.Add(accent.Name);visible.Remove(accent.Name);continue;}
            if(fx) {effect=true; DrawMotif(12,1,accent.Motif,accent.Color);}
            else
            {
                var (x,y)=Anchor(detail++);
                DrawMotif(x,y,accent.Motif,accent.Color);
            }
            if(!visible.Contains(accent.Name)) visible.Add(accent.Name);
        }
        return new(Array.AsReadOnly(pixels),visible.AsReadOnly(),suppressed.AsReadOnly());

        void FadeGrip() {int i=Array.FindLastIndex(mask,c=>c=='G');if(i>=0){pixels[i]=0xffb29670;protectedWear[i]=true;}}
        void MarkMain(int count,uint color)
        {
            int[] candidates=Enumerable.Range(0,256).Where(i=>mask[i]=='M' && i/16>top+1).ToArray();
            for(int k=0;k<count && candidates.Length>0;k++)
            {
                int index=candidates[(candidates.Length/2+k*3)%candidates.Length];pixels[index]=color;
                if(writingWear)protectedWear[index]=true;
            }
        }
        (int,int) Anchor(int slot)
        {
            if(weapon && entry.Shape is "sword" or "dagger" or "cleaver" or "rapier" or "saber" or "kris" or "needle" or "mace" or "club" or "hammer" or "flail" or "wand" or "polearm")
            {
                int idx=slot==0?Array.FindLastIndex(mask,c=>c=='G'||c=='T'):Array.FindIndex(mask,c=>c=='G');
                return idx<0 ? (7,10) : (Math.Clamp(idx%16,2,12),Math.Clamp(idx/16-(slot==0?0:1),2,12));
            }
            return entry.Family switch {"head"=>(slot==0?7:11,slot==0?4:8), "chest"=>(slot==0?7:3,slot==0?10:4), "legs"=>(slot==0?7:3,slot==0?3:8), "feet"=>(slot==0?10:7,slot==0?7:10), _=>(slot==0?7:4,slot==0?8:5)};
        }
        void DrawMotif(int x,int y,string motif,uint color)
        {
            (int,int)[] dots=motif switch
            {
                "spark"=>[(0,0),(2,2)], "flame"=>[(0,2),(1,1),(1,0)], "speed"=>[(0,0),(1,0),(1,2)], "bolt"=>[(1,0),(0,1),(1,2)],
                "ribbon"=>[(0,0),(0,1),(1,2)], "cross"=>[(0,1),(1,0),(1,1),(2,1),(1,2)],
                "fang"=>[(0,0),(0,1),(2,0),(2,1)], "shell"=>[(0,1),(1,0),(2,1),(1,2)],
                "feather"=>[(0,2),(1,1),(1,0)], "band"=>[(0,0),(1,0)], "rune"=>[(0,0),(1,1),(0,2)],
                "leaf"=>[(0,0),(1,0),(0,1)], _=>[(0,0)]
            };
            foreach(var(dx,dy) in dots) {int xx=x+dx,yy=y+dy;if(xx>=1 && xx<15 && yy>=1 && yy<15 && !protectedWear[yy*16+xx])pixels[yy*16+xx]=color;}
        }
    }
    private static bool IsMain(char c) => c is 'H' or 'M' or 'S';
    private static uint PrefixColor(string n) => n switch
    {
        "flaming" or "acrobatic"=>0xffef963f,"poisonous"=>0xff7ac95e,"enchanted"=>0xffb889e9,"charged" or "blessed"=>0xffead76e,
        "nimble"=>0xff77d8b3,"brutal"=>0xffc2585e,"swift"=>0xff86cfea,"ancient"=>0xffc2ac75,_=>0xffc9d8e5
    };
    private static string PrefixMotif(string n) => n switch
    {
        "flaming"=>"flame","charged"=>"bolt","enchanted"=>"spark","swift"=>"speed","featherweight"=>"feather","nimble" or "acrobatic"=>"ribbon",
        "blessed"=>"cross","poisonous"=>"leaf","ancient" or "precise"=>"rune",_=>"band"
    };
    public static (uint Color,string Motif) SuffixStyle(string name)
    {
        string n=name.ToLowerInvariant();
        bool Has(params string[] words)=>words.Any(n.Contains);
        // Named taxon groups share a readable motif; every animal retains its individual recipe identity.
        if(n.StartsWith("of the "))
        {
            if(Has("dragonfly","damselfly","firefly"))return(0xffb2c27a,"rune");
            if(Has("thunderbird"))return(0xffe5d18a,"bolt");
            if(Has("crab","lobster","abalone","tortoise","armadillo","pangolin","trilobite","nautilus","beetle"))return(0xff86bac5,"shell");
            if(Has("eagle","hawk","falcon","owl","raven","corvid","jay","crane","heron","ibis","jackdaw","kingfisher","magpie","osprey","rook","shrike","sicklebill","swallow","swift","hummingbird"))return(0xffd4c493,"feather");
            if(Has("serpent","viper","cobra","basilisk","python","fer-de","toad","gecko","crocodile","stonefish"))return(0xff90bc64,"rune");
            if(Has("phoenix","dragon","salamander","thunderbird"))return(0xffe79a59,"flame");
            if(Has("squid","octopus","kraken","whale","cuttlefish","otter","starfish","shrimp","sawfish"))return(0xff7bbfcf,"ribbon");
            if(Has("ant","mantis","spider","scorpion","millipede","damselfly","firefly"))return(0xffb2c27a,"rune");
            if(Has("bear","bison","buffalo","auro","ox","gorilla","golem","elephant","mammoth","rhinoceros","hippopotamus","moose","mule"))return(0xffc3a17a,"band");
            if(Has("deer","elk","ram","pronghorn","stallion","kirin"))return(0xffd5c99d,"leaf");
            return(0xffd6ad90,"fang");
        }
        if(Has("blood","killing","feros","feroc","bone"))return(0xffd76570,"band");
        if(Has("agility","fleeting","daylight","rain","breath"))return(0xff77d5b4,n.Contains("fleeting")?"speed":"ribbon");
        if(Has("accuracy"))return(0xff62d9e9,"gem");
        if(Has("precision","technique"))return(0xffb0e6ed,"rune");
        if(Has("intelligence","secret","remembered","moonlight"))return(0xffb89bdc,"gem");
        if(Has("protection","mended","hidden beneath"))return(0xff719dcc,"shell");
        if(Has("rejuv","vitality","lovers","mother","waking","kin"))return(n.Contains("vitality")?0xffe398b8:0xff87c47b,"cross");
        if(Has("strength","power","brother","father","heir"))return(0xffd9b170,"band");
        if(Has("gods","faith","child","debt"))return(0xffe5d18a,"spark");
        if(Has("rust","time","long"))return(0xffb69170,"rune");
        var affix=ItemIconCatalog.Affixes.FirstOrDefault(a=>a.Kind=="suffix" && a.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
        var palette=StatPalette(affix?.Stat??"");
        return(palette[0],"rune");
    }
    private static uint[] StatPalette(string stat)
    {
        string s=stat.ToLowerInvariant();
        uint color=s.Contains("str")||s.Contains("blood")||s.Contains("damage") ? 0xffdc8878 : s.Contains("agi")||s.Contains("silver") ? 0xff7ad5b5 : s.Contains("int")||s.Contains("thought") ? 0xffb99bdf : s.Contains("hit")||s.Contains("aim") ? 0xff78c9e4 : s.Contains("crit")||s.Contains("razor") ? 0xffdcbe79 : 0xff8fb8cc;
        return [color,0xff658b97,0xff3b5665];
    }
    public static void Draw(DrawingContext context,ItemIconFrame frame,Rect bounds)
    {
        double scale=Math.Max(1,Math.Floor(Math.Min(bounds.Width,bounds.Height)/16));
        double ox=Math.Floor(bounds.X+(bounds.Width-16*scale)/2),oy=Math.Floor(bounds.Y+(bounds.Height-16*scale)/2);
        var brushes=new Dictionary<uint,IBrush>();
        for(int i=0;i<256;i++) if(frame.Pixels[i]!=0)
        {
            uint c=frame.Pixels[i];
            if(!brushes.TryGetValue(c,out var brush))brushes[c]=brush=new SolidColorBrush(Color.FromUInt32(c));
            context.FillRectangle(brush,new Rect(ox+i%16*scale,oy+i/16*scale,scale,scale));
        }
    }
}

public sealed class ItemIconControl : Control
{
    public ItemIconFrame? Frame { get; set; }
    public override void Render(DrawingContext context) {base.Render(context);if(Frame!=null)ItemIconRenderer.Draw(context,Frame,new Rect(Bounds.Size));}
}
