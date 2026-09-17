using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.ArtLab;

namespace RPGGame.Tests.Unit;

public static class ItemRarityGalleryTests
{
    public static void Run(string output)
    {
        int checks=0;
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
        var groups=ItemRarityPreview.Generate(1717,6);
        Check(groups.Select(g=>g.Name).SequenceEqual(ItemRarityPreview.Order),"Rarity order mismatch.");
        Check(groups.All(g=>g.Recipes.Count==6),"Incorrect number per rarity.");
        Check(JsonSerializer.Serialize(groups)==JsonSerializer.Serialize(ItemRarityPreview.Generate(1717,6)),"Seed not deterministic.");
        Check(JsonSerializer.Serialize(groups)!=JsonSerializer.Serialize(ItemRarityPreview.Generate(1718,6)),"Seed did not change batch.");
        var larger=ItemRarityPreview.Generate(1717,12);
        for(int i=0;i<6;i++)Check(JsonSerializer.Serialize(groups[i].Recipes)==JsonSerializer.Serialize(larger[i].Recipes.Take(6)),"Changing count changed the first six samples.");
        foreach(string family in new[]{"sword","dagger","mace","wand","head","chest","legs","feet"})
        {
            var batch=ItemRarityPreview.Generate(91,6,family,3);
            Check(batch.Count==6,"Missing rarity section.");
            foreach(var group in batch)foreach(var recipe in group.Recipes)
            {
                var entry=ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId);
                Check(entry.Family==family&&entry.Tier==3,"Filter leaked another family or tier.");
                var quality=ItemIconCatalog.Affixes.First(a=>a.Kind=="quality"&&a.Name==recipe.Quality);
                Check(quality.Rarity==group.Name,"Quality rank does not match rarity.");
                if(Enum.TryParse<WeaponType>(family,true,out var type))Check(recipe.Material==ItemMaterialRules.ResolveWeaponMaterial(type,group.Name),"Weapon material ladder mismatch.");
                if(group.Name=="Common")Check(recipe.Prefixes.Length==0&&recipe.Suffixes.Length==0,"Common preview has unexpected affixes.");
                Check(ItemIconRenderer.Render(recipe).Pixels.Any(c=>c!=0),"Preview icon missing.");
            }
        }
        var consumables=ItemRarityPreview.Generate(1,6,"consumable");
        Check(consumables.Count==1&&consumables[0].Name=="Consumables","Consumables assigned invented rarities.");
        Check(consumables[0].Recipes.All(r=>r.Quality==""&&r.Prefixes.Length==0&&r.Suffixes.Length==0),"Consumables have affixes.");
        Check(ItemRarityPreview.Generate(1,6,"consumable",5).Count==0,"Empty filter should return no groups.");
        Check(ItemRarityPreview.Generate(1,100).All(g=>g.Recipes.Count==24),"Preview count cap failed.");
        AppBuilder.Configure<ItemIconLabApp>().UsePlatformDetect().SetupWithoutStarting();
        var window=new ItemRarityGalleryWindow { ShowInTaskbar=false,Position=new PixelPoint(-10000,-10000) };
        window.Show();Dispatcher.UIThread.RunJobs();
        T Field<T>(string name)=>(T)typeof(ItemRarityGalleryWindow).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
        void Click(string name)=>window.GetLogicalDescendants().OfType<Button>().First(b=>b.Content is string s&&s==name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(Field<StackPanel>("rows").Children.Count==6,"UI is not grouped by rarity.");
        Field<ComboBox>("family").SelectedItem="sword";Field<ComboBox>("count").SelectedItem=12;
        Check(Field<IReadOnlyList<ItemRarityPreview.Group>>("groups").Sum(g=>g.Recipes.Count)==72,"Count control failed.");
        Check(Field<IReadOnlyList<ItemRarityPreview.Group>>("groups").SelectMany(g=>g.Recipes).All(r=>ItemIconCatalog.Items.First(e=>e.Id==r.CatalogId).Family=="sword"),"Family selector failed.");
        var card=window.GetLogicalDescendants().OfType<Button>().Last(b=>b.Tag is ItemIconRecipe);
        card.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(Field<ItemIconRecipe>("selected")== (ItemIconRecipe)card.Tag!,"Card selection failed.");
        string before=Field<TextBox>("seed").Text!;Click("Shuffle");Check(Field<TextBox>("seed").Text!=before,"Shuffle did not change seed.");
        Field<ComboBox>("family").SelectedItem="consumable";Field<ComboBox>("tier").SelectedItem="5";
        Check(Field<ItemIconRecipe?>("selected")==null&&Field<StackPanel>("rows").Children.Count==0,"Empty filter retained old selected item.");
        Field<TextBox>("seed").Text="1717";Field<ComboBox>("family").SelectedItem="All gear";Field<ComboBox>("tier").SelectedItem="Any";Field<ComboBox>("count").SelectedItem=6;Click("Generate");
        Dispatcher.UIThread.RunJobs();
        var content=(Control)window.Content!;content.Measure(new Size(1400,1000));content.Arrange(new Rect(0,0,1400,1000));
        Directory.CreateDirectory(output);
        using(var bitmap=new RenderTargetBitmap(new PixelSize(1400,1000),new Vector(96,96))){bitmap.Render(content);bitmap.Save(Path.Combine(output,"rarity-gallery.png"));}
        File.WriteAllText(Path.Combine(output,"rarity-batch.json"),JsonSerializer.Serialize(groups,new JsonSerializerOptions { WriteIndented=true }));
        window.Close();string report=$"PASS: {checks} rarity preview checks (seed, rank pools, material ladders, family/tier filters, counts, consumables, selection, shuffle, empty state).";
        File.WriteAllText(Path.Combine(output,"rarity-verification.txt"),report);Console.WriteLine(report);
    }
}
