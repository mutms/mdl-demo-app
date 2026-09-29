# Draws app.ico from the geometry of mdl-demo's favicon.svg
# (go/internal/webui/static/favicon.svg): the biohazard trefoil, dark on
# Moodle orange. Windows PowerShell has no SVG renderer, and the shapes are
# simple, so they are redrawn here at each icon size. Run it again after
# changing the favicon:  powershell -File win\MDL_Demo\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$orange = [System.Drawing.ColorTranslator]::FromHtml('#ff8c1a')
$dark = [System.Drawing.ColorTranslator]::FromHtml('#201408')

function New-IconPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $s = $size / 32.0   # favicon.svg has a 32x32 viewBox

    # <rect x="2" y="2" width="28" height="28" rx="6.5" fill="#ff8c1a"/>
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x = 2 * $s; $w = 28 * $s; $d = 2 * 6.5 * $s
    $path.AddArc($x, $x, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $x, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $x + $w - $d, $d, $d, 0, 90)
    $path.AddArc($x, $x + $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush $orange), $path)

    # Three rings, r=6, stroke-width 3, and the centre dot r=2.3.
    $pen = New-Object System.Drawing.Pen $dark, (3 * $s)
    foreach ($c in @(@(16, 9.5), @(10, 20), @(22, 20))) {
        $g.DrawEllipse($pen, ($c[0] - 6) * $s, ($c[1] - 6) * $s, 12 * $s, 12 * $s)
    }
    $g.FillEllipse((New-Object System.Drawing.SolidBrush $dark), (16 - 2.3) * $s, (16 - 2.3) * $s, 4.6 * $s, 4.6 * $s)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    if ($size -ge 256) {
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    } else {
        # Small sizes as a classic DIB: older readers (GDI+) do not decode PNG entries.
        $w = New-Object System.IO.BinaryWriter $ms
        $w.Write([uint32]40); $w.Write([int32]$size); $w.Write([int32]($size * 2))
        $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]0)
        $w.Write([uint32]0); $w.Write([int32]0); $w.Write([int32]0); $w.Write([uint32]0); $w.Write([uint32]0)
        for ($y = $size - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $size; $x++) {
                $p = $bmp.GetPixel($x, $y)
                $w.Write([byte]$p.B); $w.Write([byte]$p.G); $w.Write([byte]$p.R); $w.Write([byte]$p.A)
            }
        }
        # AND mask, all zero: the alpha channel does the transparency.
        $maskRow = [int]([math]::Ceiling($size / 32) * 4)
        $w.Write((New-Object byte[] ($maskRow * $size)))
        $w.Flush()
    }
    $bmp.Dispose()
    return , $ms.ToArray()
}

# ICO: 6-byte header, a 16-byte entry per image, then the image data.
$pngs = foreach ($size in $sizes) { , (New-IconPng $size) }
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($png in $pngs) { $bw.Write($png) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'app.ico'), $out.ToArray())
