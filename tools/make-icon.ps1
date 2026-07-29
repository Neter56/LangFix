# Renders the LangFix tray glyph (see TrayIconFactory) into a multi-resolution app.ico.
param([string]$OutputPath = "$PSScriptRoot\..\src\LangFix\app.ico")

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-LangFixBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.ScaleTransform($size / 32.0, $size / 32.0)

    $bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 99, 177))
    $g.FillEllipse($bg, 0, 0, 31, 31)

    $font = New-Object System.Drawing.Font('Segoe UI', 15, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('A', $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.PointF(1, 1)))
    $g.DrawString([string][char]0x05D0, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.PointF(14, 12)))

    $font.Dispose(); $bg.Dispose(); $g.Dispose()
    return $bmp
}

function ConvertTo-IconDib([System.Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # BITMAPINFOHEADER - height is doubled to account for the (unused) AND mask.
    $bw.Write([int]40); $bw.Write([int]$s); $bw.Write([int]($s * 2))
    $bw.Write([int16]1); $bw.Write([int16]32)
    $bw.Write([int]0); $bw.Write([int]($s * $s * 4))
    $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)

    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }

    $maskStride = [int][Math]::Ceiling($s / 32.0) * 4
    $bw.Write((New-Object byte[] ($maskStride * $s)))
    $bw.Flush()
    # Leading comma stops PowerShell from unrolling the byte[] into the pipeline.
    return , $ms.ToArray()
}

$entries = @()
foreach ($size in 16, 24, 32, 48, 64, 128) {
    $bmp = New-LangFixBitmap $size
    [byte[]]$dib = ConvertTo-IconDib $bmp
    $entries += , @{ Size = $size; Data = $dib }
    $bmp.Dispose()
}

$bmp = New-LangFixBitmap 256
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$entries += , @{ Size = 256; Data = [byte[]]$ms.ToArray() }
$bmp.Dispose()

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($out)
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]$e.Data.Length); $w.Write([int]$offset)
    $offset += $e.Data.Length
}
foreach ($e in $entries) { $w.Write([byte[]]$e.Data) }
$w.Flush()

if ($out.Length -ne $offset) {
    throw "Icon assembly mismatch: wrote $($out.Length) bytes, expected $offset."
}

$resolved = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.File]::WriteAllBytes($resolved, $out.ToArray())
Write-Output "Wrote $resolved ($($out.Length) bytes, $($entries.Count) sizes)"
