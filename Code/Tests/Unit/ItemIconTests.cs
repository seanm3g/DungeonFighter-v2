using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.LogicalTree;
using RPGGame.UI.Avalonia.ArtLab;

namespace RPGGame.Tests.Unit;

public static class ItemIconTests
{
    public static void Run(string output)
    {
        int checks=0,renders=0;
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
        void Valid(ItemIconRecipe recipe)
        {
            var frame=ItemIconRenderer.Render(recipe);renders++;
            Check(frame.Pixels.Count==256,"Wrong native dimensions.");
            Check(frame.Pixels.Count(c=>c!=0)>5,"Empty icon: "+recipe.CatalogId);
            Check(frame.Pixels.Any(c=>c==0),"Missing alpha: "+recipe.CatalogId);
            Check(frame.Pixels.All(c=>c==0||(c>>24)==255),"Unexpected soft alpha.");
        }
        Check(ItemIconCatalog.Items.Select(e=>e.Id).Distinct().Count()==ItemIconCatalog.Items.Count,"Duplicate catalog identity.");
        foreach(var shape in ItemIconShapes.All)
        {
            Check(shape.Value.Length<=14&&shape.Value.All(row=>row.Length<=14),"Shape exceeds safe outline margin: "+shape.Key);
            Check(shape.Value.All(row=>row.All(c=>".HMSGT".Contains(c))),"Invalid component mask: "+shape.Key);
        }
        foreach(var entry in ItemIconCatalog.Items)
        {
            Check(ItemIconShapes.All.ContainsKey(entry.Shape),"Unmapped silhouette: "+entry.Name);
            var recipe=new ItemIconRecipe(entry.Id,entry.DefaultMaterial,"",[],[]);
            Valid(recipe);
            foreach(string material in ItemIconRenderer.Materials.Keys)Valid(recipe with{Material=material});
            foreach(var affix in ItemIconCatalog.Affixes.Where(a=>a.Kind!="material"))
                Valid(affix.Kind switch{"quality"=>recipe with{Quality=affix.Name},"adjective"=>recipe with{Prefixes=[affix.Name]},_=>recipe with{Suffixes=[affix.Name]}});
        }
        var sword=ItemIconCatalog.Items.First(e=>e.Name=="Sword"&&e.Family=="sword");
        var baseline=new ItemIconRecipe(sword.Id,"Steel","",[],[]);
        Check(!ItemIconRenderer.Render(baseline).Pixels.SequenceEqual(ItemIconRenderer.Render(baseline with{Material="Gold"}).Pixels),"Material did not change pixels.");
        Check(!ItemIconRenderer.Render(baseline).Pixels.SequenceEqual(ItemIconRenderer.Render(baseline with{Suffixes=["of Accuracy"]}).Pixels),"Suffix did not change pixels.");
        Check(!ItemIconRenderer.Render(baseline).Pixels.SequenceEqual(ItemIconRenderer.Render(baseline with{Quality="Broken"}).Pixels),"Quality did not change pixels.");
        var many=baseline with{Quality="Broken",Prefixes=ItemIconCatalog.Names("adjective").ToArray(),Suffixes=ItemIconCatalog.Names("suffix").ToArray()};
        Valid(many);Check(ItemIconRenderer.Render(many).SuppressedAffixes.Count>0,"Effect budget not applied.");
        var reversed=many with{Prefixes=many.Prefixes.Reverse().ToArray(),Suffixes=many.Suffixes.Reverse().ToArray()};
        Check(ItemIconRenderer.Key(many)==ItemIconRenderer.Key(reversed),"Affix ordering changed cache identity.");
        Check(ItemIconRenderer.Render(many).Pixels.SequenceEqual(ItemIconRenderer.Render(reversed).Pixels),"Non-deterministic ordering.");
        var scar=ItemIconRenderer.Render(baseline with{Quality="Battle Scarred"});
        var scarPlus=ItemIconRenderer.Render(baseline with{Quality="Battle Scarred",Prefixes=["Reinforced"],Suffixes=["of Accuracy"]});
        var plain=ItemIconRenderer.Render(baseline);
        var scars=Enumerable.Range(0,256).Where(i=>scar.Pixels[i]==0xff101317&&plain.Pixels[i]!=0xff101317&&plain.Pixels[i]!=0).ToArray();
        Check(scars.Length>0&&scars.All(i=>scarPlus.Pixels[i]==scar.Pixels[i]),"Adding affixes removed existing blade scars.");
        Check(ItemIconCatalog.Resolve(new FeetItem("Masterwork Mithril Full Plate Boots of the Fox",5)).Name=="Full Plate Boots","Longest-name resolution failed.");
        var live=new WeaponItem("Sword",sword.Tier,weaponType:WeaponType.Sword){Material="Mithril",Modifications=[new Modification{Name="Worn",PrefixCategory="Quality"},new Modification{Name="Flaming",PrefixCategory="Adjective"}],StatBonuses=[new StatBonus{Name="of the Fox"}]};
        var copy=JsonSerializer.Serialize(live);var resolved=ItemIconRenderer.ForItem(live);ItemIconRenderer.Render(live);
        Check(resolved.Material=="Mithril"&&resolved.Quality=="Worn"&&resolved.Prefixes.Contains("Flaming")&&resolved.Suffixes.Contains("of the Fox"),"Structured item recipe failed.");
        Check(copy==JsonSerializer.Serialize(live),"Rendering mutated gameplay data.");
        live.Material="Bone";Check(ItemIconRenderer.Key(resolved)!=ItemIconRenderer.Key(ItemIconRenderer.ForItem(live)),"Material mutation did not invalidate cache.");
        Valid(new("modded:item","not-a-material","unknown-quality",["unknown-prefix"],["unknown-suffix"]));
        var random=new Random(1717);var entries=ItemIconCatalog.Items.ToArray();var mats=ItemIconRenderer.Materials.Keys.ToArray();var qs=ItemIconCatalog.Names("quality").ToArray();var ps=ItemIconCatalog.Names("adjective").ToArray();var ss=ItemIconCatalog.Names("suffix").ToArray();
        var samples=new List<ItemIconRecipe>();
        for(int i=0;i<4096;i++)
        {
            var recipe=new ItemIconRecipe(entries[random.Next(entries.Length)].Id,mats[random.Next(mats.Length)],qs[random.Next(qs.Length)],Enumerable.Range(0,random.Next(4)).Select(_=>ps[random.Next(ps.Length)]).ToArray(),Enumerable.Range(0,random.Next(6)).Select(_=>ss[random.Next(ss.Length)]).ToArray(),i);
            Valid(recipe);var before=ItemIconRenderer.Render(recipe).Pixels.ToArray();
            Check(before.SequenceEqual(ItemIconRenderer.Render(recipe).Pixels),"Non-deterministic seeded icon.");
            if(i<48)samples.Add(recipe);
        }
        // Exercise eviction: old recipes must re-render identically after more than 512 other keys.
        var snapshot=ItemIconRenderer.Render(baseline).Pixels.ToArray();
        for(int i=0;i<600;i++)ItemIconRenderer.Render(baseline with{Seed=i+10000});
        Check(snapshot.SequenceEqual(ItemIconRenderer.Render(baseline).Pixels),"Eviction changed pixels.");
        AppBuilder.Configure<ItemIconLabApp>().UsePlatformDetect().SetupWithoutStarting();
        var canvas=new RPGGame.UI.Avalonia.GameCanvasControl(true,40,30);
        canvas.AddItemIcon(2,3,live);canvas.AddItemIcon(8,9,live);Check(canvas.ItemIconCount==2,"Canvas icon registration failed.");
        canvas.ClearTextInArea(0,2,6,3);Check(canvas.ItemIconCount==1,"Panel clear left stale icon.");
        canvas.ClearTextInRange(9,9);Check(canvas.ItemIconCount==0,"Row clear left stale icon.");
        canvas.AddItemIcon(1,1,live);canvas.Clear();Check(canvas.ItemIconCount==0,"Full clear left stale icon.");
        Directory.CreateDirectory(output);
        var all=entries.Select(e=>new ItemIconRecipe(e.Id,e.DefaultMaterial,"",[],[])).ToArray();
        SaveSheet(Path.Combine(output,"catalog-atlas.png"),all,24,16,false);
        SaveSheet(Path.Combine(output,"catalog-review.png"),all,12,96,true);
        SaveSheet(Path.Combine(output,"permutations-review.png"),samples,8,128,true);
        var affixRecipes=ItemIconCatalog.Affixes.Select(a=>a.Kind switch{"material"=>baseline with{Material=a.Name},"quality"=>baseline with{Quality=a.Name},"adjective"=>baseline with{Prefixes=[a.Name]},_=>baseline with{Suffixes=[a.Name]}}).ToArray();
        SaveSheet(Path.Combine(output,"affixes-review.png"),affixRecipes,10,112,true,true);
        var manifest=new {version=ItemIconRenderer.Version,size=16,columns=24,entries=entries.Select((e,i)=>new{e.Id,e.Name,e.Family,e.Tier,e.Shape,source=new[]{i%24*16,i/24*16,16,16},recipe=all[i]})};
        File.WriteAllText(Path.Combine(output,"catalog-atlas.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
        File.WriteAllText(Path.Combine(output,"permutations.json"),JsonSerializer.Serialize(samples,new JsonSerializerOptions{WriteIndented=true}));
        using(var native=ItemIconExport.Bitmap(baseline,1))native.Save(Path.Combine(output,"native-sword.png"));
        using(var window=newWindowCapture())window.Save(Path.Combine(output,"lab-review.png"));
        string report=$"PASS: {checks:N0} checks; {renders:N0} renders; {entries.Length} catalog entries; {ItemIconCatalog.Affixes.Count} affix mappings; 4,096 mixed permutations. Exhaustive single-affix × item coverage; combinatorial space sampled, not exhaustively enumerated.";
        File.WriteAllText(Path.Combine(output,"verification.txt"),report);Console.WriteLine(report);

        RenderTargetBitmap newWindowCapture()
        {
            var lab=new ItemIconLabWindow{ShowInTaskbar=false,Position=new PixelPoint(-10000,-10000)};
            lab.Show();global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            T Field<T>(string name)=>(T)typeof(ItemIconLabWindow).GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(lab)!;
            void Click(string label)
            {
                var button=lab.GetLogicalDescendants().OfType<Button>().First(b=>b.Content is string s&&s==label);
                button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }
            Click("Generate 24 permutations");
            Check(Field<WrapPanel>("gallery").Children.Count==24,"Random batch size failed.");
            var first=(Button)Field<WrapPanel>("gallery").Children[0];first.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            var selected=Field<ItemIconRecipe>("current");
            Check(selected.CatalogId==Field<List<ItemIconCatalog.Entry>>("entries")[Field<ComboBox>("item").SelectedIndex].Id,"Selecting a card did not synchronize selectors.");
            Field<CheckBox>("lockItem").IsChecked=true;Field<CheckBox>("lockMaterial").IsChecked=true;Field<CheckBox>("lockQuality").IsChecked=true;Field<CheckBox>("lockAffixes").IsChecked=true;
            Click("Next batch");
            foreach(var card in Field<WrapPanel>("gallery").Children.Cast<Button>())
            {
                card.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));var value=Field<ItemIconRecipe>("current");
                Check(value.CatalogId==selected.CatalogId&&value.Material==selected.Material&&value.Quality==selected.Quality&&value.Prefixes.SequenceEqual(selected.Prefixes)&&value.Suffixes.SequenceEqual(selected.Suffixes),"Locked choice changed during randomization.");
            }
            foreach(var cb in new[]{"lockItem","lockMaterial","lockQuality","lockAffixes"})Field<CheckBox>(cb).IsChecked=false;
            Field<ComboBox>("family").SelectedItem="feet";Click("Browse catalog");Check(Field<WrapPanel>("gallery").Children.Count==50,"Family catalog filter failed.");
            Click("Add prefix");Click("Add suffix");Check(Field<ItemIconRecipe>("current").Prefixes.Length>0&&Field<ItemIconRecipe>("current").Suffixes.Length>0,"Affix controls failed.");
            Click("Clear affixes");Check(Field<ItemIconRecipe>("current").Prefixes.Length==0&&Field<ItemIconRecipe>("current").Suffixes.Length==0,"Clear affixes failed.");
            Field<ComboBox>("family").SelectedItem="All";Click("Generate 24 permutations");
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var content=(Control)lab.Content!;
            content.Measure(new Size(1240,900));content.Arrange(new Rect(0,0,1240,900));
            var image=new RenderTargetBitmap(new PixelSize(1240,900),new Vector(96,96));image.Render(content);
            lab.Close();return image;
        }
    }
    public static void SaveSheet(string path,IReadOnlyList<ItemIconRecipe> recipes,int columns,int cell,bool labels,bool affixLabels=false)
    {
        int width=columns*cell,height=(int)Math.Ceiling(recipes.Count/(double)columns)*cell;
        var control=new Sheet{Recipes=recipes,Columns=columns,Cell=cell,Labels=labels,AffixLabels=affixLabels,Width=width,Height=height};
        control.Measure(new Size(width,height));control.Arrange(new Rect(0,0,width,height));
        using var bitmap=new RenderTargetBitmap(new PixelSize(width,height),new Vector(96,96));bitmap.Render(control);bitmap.Save(path);
    }
    private sealed class Sheet:Control
    {
        public IReadOnlyList<ItemIconRecipe> Recipes=[];public int Columns,Cell;public bool Labels,AffixLabels;
        public override void Render(DrawingContext context)
        {
            if(Labels)context.FillRectangle(Brush.Parse("#1c222b"),new Rect(Bounds.Size));
            for(int i=0;i<Recipes.Count;i++)
            {
                int x=i%Columns*Cell,y=i/Columns*Cell;var recipe=Recipes[i];
                ItemIconRenderer.Draw(context,ItemIconRenderer.Render(recipe),new Rect(x,y,Cell,Labels?Cell-30:Cell));
                if(!Labels)continue;
                string label=AffixLabels ? string.Join(" ",recipe.Prefixes.Concat(recipe.Suffixes)) : ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId).Name;
                if(AffixLabels&&label.Length==0)label=recipe.Quality.Length>0?recipe.Quality:recipe.Material;
                var text=new FormattedText(label,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Arial"),10,Brushes.White){MaxTextWidth=Cell-8,MaxTextHeight=28,Trimming=TextTrimming.CharacterEllipsis};
                context.DrawText(text,new Point(x+4,y+Cell-28));
            }
        }
    }
}
