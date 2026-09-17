using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Compact comparison app; rarity is shown on the card frame, never baked into the sprite.</summary>
public sealed class ItemRarityGalleryWindow : Window
{
    private readonly ComboBox family = new() { MinWidth=130 }, count = new() { MinWidth=80 }, tier = new() { MinWidth=90 };
    private readonly TextBox seed = new() { Text="1717", Width=100 };
    private readonly StackPanel rows = new() { Spacing=10 };
    private readonly TextBlock status = new() { FontSize=12, Foreground=Brush.Parse("#97A7BA") };
    private readonly TextBlock selectedTitle = new() { FontSize=15, FontWeight=FontWeight.SemiBold, TextWrapping=TextWrapping.Wrap, MaxHeight=48 };
    private readonly TextBlock selectedDetails = new() { FontSize=12, TextWrapping=TextWrapping.Wrap, MaxHeight=48, Foreground=Brush.Parse("#AEBBCB") };
    private readonly ItemIconControl selectedIcon = new() { Width=64, Height=64 };
    private readonly ScrollViewer scroll;
    private ItemIconRecipe? selected;
    private IReadOnlyList<ItemRarityPreview.Group> groups = [];
    private bool ready;

    public ItemRarityGalleryWindow()
    {
        Title="Dungeon Fighter — Rarity Gallery"; Width=1400;Height=1000;MinWidth=980;MinHeight=680;
        Background=Brush.Parse("#141A22");
        var root = new Grid { RowDefinitions=new RowDefinitions("Auto,*,Auto"), Margin=new Thickness(24) };
        var header = new StackPanel { Spacing=10, Margin=new Thickness(0,0,0,16) };
        header.Children.Add(new TextBlock { Text="THE LOOT TABLE", FontSize=26, FontWeight=FontWeight.Bold });
        header.Children.Add(new TextBlock { Text="Random item variations, side by side. Select a card to inspect its affixes.", Foreground=Brush.Parse("#AEBBCB") });
        var toolbar = new WrapPanel { Orientation=Orientation.Horizontal };
        family.ItemsSource=new[]{"All gear","sword","dagger","mace","wand","head","chest","legs","feet","consumable"};family.SelectedIndex=0;
        count.ItemsSource=new[]{6,12,24};count.SelectedIndex=0;
        tier.ItemsSource=new[]{"Any","1","2","3","4","5"};tier.SelectedIndex=0;
        AddField("Family",family);AddField("Per rarity",count);AddField("Item tier",tier);AddField("Seed",seed);
        toolbar.Children.Add(MakeButton("Generate",()=>Regenerate(),true));
        toolbar.Children.Add(MakeButton("Shuffle",()=>{seed.Text=Random.Shared.Next().ToString(CultureInfo.InvariantCulture);Regenerate();}));
        toolbar.Children.Add(MakeButton("Affix editor",()=>new ItemIconLabWindow().Show(this)));
        toolbar.Children.Add(MakeButton("Save batch",async()=>await SaveBatch()));
        header.Children.Add(toolbar);header.Children.Add(status);root.Children.Add(header);
        scroll=new ScrollViewer { Content=rows, HorizontalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll,1);root.Children.Add(scroll);
        var footer=new Grid { ColumnDefinitions=new ColumnDefinitions("80,*,Auto"), Margin=new Thickness(0,16,0,0) };
        footer.Children.Add(selectedIcon);
        var description=new StackPanel { Spacing=4, Margin=new Thickness(0,0,16,0) };description.Children.Add(selectedTitle);description.Children.Add(selectedDetails);Grid.SetColumn(description,1);footer.Children.Add(description);
        var export=MakeButton("Save selected PNG",async()=>await SaveSelected());Grid.SetColumn(export,2);footer.Children.Add(export);Grid.SetRow(footer,2);root.Children.Add(footer);
        Content=new Border { Background=Background, Child=root };
        family.SelectionChanged+=(_,_)=>{if(ready)Regenerate();};count.SelectionChanged+=(_,_)=>{if(ready)Regenerate();};tier.SelectionChanged+=(_,_)=>{if(ready)Regenerate();};
        seed.KeyDown+=(_,e)=>{if(e.Key==global::Avalonia.Input.Key.Enter)Regenerate();};
        ready=true;Regenerate();
        void AddField(string name,Control control)
        {
            var field=new StackPanel { Spacing=4, Margin=new Thickness(0,0,14,0) };
            field.Children.Add(new TextBlock { Text=name, FontSize=11, Foreground=Brush.Parse("#AEBBCB") });field.Children.Add(control);toolbar.Children.Add(field);
        }
    }
    private static Button MakeButton(string text,System.Action action,bool primary=false)
    {
        var b=new Button { Content=text,Margin=new Thickness(0,16,8,0),Padding=new Thickness(14,8),VerticalAlignment=VerticalAlignment.Bottom };
        if(primary){b.Background=Brush.Parse("#CBE888");b.Foreground=Brush.Parse("#18201C");}
        b.Click+=(_,_)=>action();return b;
    }
    private void Regenerate()
    {
        int value=int.TryParse(seed.Text,out int number)?number:unchecked((int)ItemIconRenderer.StableHash(seed.Text??""));
        int? itemTier=int.TryParse(tier.SelectedItem as string,out int parsed)?parsed:null;
        groups=ItemRarityPreview.Generate(value,(int)(count.SelectedItem??6),family.SelectedItem as string??"All gear",itemTier);
        rows.Children.Clear();selected=null;selectedIcon.Frame=null;selectedIcon.InvalidateVisual();selectedTitle.Text="Select an item";selectedDetails.Text="";
        foreach(var group in groups)
        {
            var grid=new Grid { ColumnDefinitions=new ColumnDefinitions("108,*") };
            var label=new StackPanel { Spacing=6, Margin=new Thickness(0,14,10,0) };
            label.Children.Add(new TextBlock { Text=group.Name.ToUpperInvariant(),FontSize=12,FontWeight=FontWeight.Bold,Foreground=Brush.Parse(group.Color) });
            label.Children.Add(new TextBlock { Text=group.Recipes.Count+" items",FontSize=11,Foreground=Brush.Parse("#8797A9") });grid.Children.Add(label);
            var cards=new WrapPanel { Orientation=Orientation.Horizontal };Grid.SetColumn(cards,1);grid.Children.Add(cards);
            foreach(var recipe in group.Recipes)
            {
                var entry=ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId);
                var contents=new StackPanel { Width=158, Spacing=3 };
                contents.Children.Add(new ItemIconControl { Frame=ItemIconRenderer.Render(recipe),Width=48,Height=48,HorizontalAlignment=HorizontalAlignment.Center });
                contents.Children.Add(new TextBlock { Text=entry.Name,FontSize=12,FontWeight=FontWeight.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis });
                contents.Children.Add(new TextBlock { Text=recipe.Material+" · "+recipe.Quality,FontSize=10,Foreground=Brush.Parse("#AEBBCB"),TextTrimming=TextTrimming.CharacterEllipsis });
                var button=new Button { Content=contents,Tag=recipe,Background=Brush.Parse("#202A36"),BorderBrush=Brush.Parse(group.Color),BorderThickness=new Thickness(0,2,0,0),Padding=new Thickness(9,6),Margin=new Thickness(0,0,8,4) };
                ToolTip.SetTip(button,FullName(recipe));button.Click+=(_,_)=>Select(recipe,group.Name);cards.Children.Add(button);
            }
            rows.Children.Add(grid);
        }
        status.Text=groups.Count==0 ? "No items match this family and tier." : $"{groups.Sum(g=>g.Recipes.Count)} items · seed {value} · equal samples per rarity, not drop probabilities";
        if(groups.Count>0)Select(groups[0].Recipes[0],groups[0].Name);
        scroll.Offset=new Vector(0,0);
    }
    private void Select(ItemIconRecipe recipe,string rarity)
    {
        selected=recipe;var frame=ItemIconRenderer.Render(recipe);selectedIcon.Frame=frame;selectedIcon.InvalidateVisual();
        selectedTitle.Text=rarity+" / "+FullName(recipe);
        selectedDetails.Text=frame.SuppressedAffixes.Count>0?"Additional affixes retained in the item recipe: "+string.Join(", ",frame.SuppressedAffixes):"All selected visual accents are shown. Pixel size: 16 × 16.";
    }
    private static string FullName(ItemIconRecipe recipe)
    {
        var entry=ItemIconCatalog.Items.First(e=>e.Id==recipe.CatalogId);
        return string.Join(" ",new[]{recipe.Quality}.Concat(recipe.Prefixes).Concat(new[]{recipe.Material,entry.Name}).Concat(recipe.Suffixes).Where(s=>s.Length>0));
    }
    private async Task SaveSelected()
    {
        if(selected==null)return;
        var recipe=selected;
        try
        {
            var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title="Save item sprite",SuggestedFileName="item.png",DefaultExtension="png" });
            if(file==null)return;
            await using var stream=await file.OpenWriteAsync();stream.SetLength(0);using var image=ItemIconExport.Bitmap(recipe,1);image.Save(stream);status.Text="Saved "+file.Name;
        }
        catch(Exception ex){status.Text="Could not save: "+ex.Message;}
    }
    private async Task SaveBatch()
    {
        var snapshot=groups;
        try
        {
            var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title="Save rarity batch",SuggestedFileName="rarity-batch.json",DefaultExtension="json" });
            if(file==null)return;
            await using var stream=await file.OpenWriteAsync();stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream,new { version=ItemIconRenderer.Version,groups=snapshot },new JsonSerializerOptions { WriteIndented=true });status.Text="Saved "+file.Name;
        }
        catch(Exception ex){status.Text="Could not save: "+ex.Message;}
    }
}
