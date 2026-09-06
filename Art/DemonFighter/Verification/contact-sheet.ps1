Add-Type -AssemblyName System.Drawing
$assetRoot = Join-Path $PSScriptRoot '../../../GameData/Visuals/DemonFighter'
$catalog = Get-Content (Join-Path $assetRoot 'enemies.json') -Raw | ConvertFrom-Json
$families = @($catalog.PSObject.Properties.Value | Sort-Object -Unique)
$sheet = [System.Drawing.Bitmap]::new(1800, [int]([Math]::Ceiling($families.Count / 6.0) * 280))
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(18,20,20))
$font = [System.Drawing.Font]::new('Consolas',14)
for ($i=0; $i -lt $families.Count; $i++) {
    $img = [System.Drawing.Image]::FromFile((Join-Path $assetRoot ($families[$i] + '-layer.png')))
    $x = ($i % 6) * 300
    $y = [Math]::Floor($i / 6) * 280
    $g.DrawImage($img,[System.Drawing.Rectangle]::new($x,$y,300,250),[System.Drawing.Rectangle]::new(800,50,650,760),[System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString($families[$i],$font,[System.Drawing.Brushes]::YellowGreen,$x+8,$y+253)
    $img.Dispose()
}
$sheet.Save((Join-Path $PSScriptRoot 'enemy-families.png'))
$font.Dispose(); $g.Dispose(); $sheet.Dispose()
