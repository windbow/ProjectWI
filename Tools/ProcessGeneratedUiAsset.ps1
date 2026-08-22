param(
    [Parameter(Mandatory = $true)] [string] $Source,
    [Parameter(Mandatory = $true)] [string] $Destination,
    [ValidateSet("Light", "Dark")] [string] $Background = "Light"
)

Add-Type -AssemblyName System.Drawing

$loadedBitmap = [System.Drawing.Bitmap]::FromFile($Source)
$targetWidth = [Math]::Min(512, $loadedBitmap.Width)
$targetHeight = [Math]::Round($loadedBitmap.Height * ($targetWidth / $loadedBitmap.Width))
$sourceBitmap = [System.Drawing.Bitmap]::new($targetWidth, $targetHeight,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($sourceBitmap)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($loadedBitmap, 0, 0, $targetWidth, $targetHeight)
$graphics.Dispose()
$loadedBitmap.Dispose()
$width = $sourceBitmap.Width
$height = $sourceBitmap.Height
$visited = [bool[]]::new($width * $height)
$queue = [System.Collections.Generic.Queue[int]]::new()

function Add-BackgroundPixel([int] $x, [int] $y)
{
    if ($x -lt 0 -or $y -lt 0 -or $x -ge $width -or $y -ge $height)
    {
        return
    }

    $index = $y * $width + $x
    if ($visited[$index] -eq $true)
    {
        return
    }

    $pixel = $sourceBitmap.GetPixel($x, $y)
    $maximum = [Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B))
    $minimum = [Math]::Min($pixel.R, [Math]::Min($pixel.G, $pixel.B))
    $isLightBackground = $Background -eq "Light" -and
        $pixel.R -ge 225 -and $pixel.G -ge 225 -and $pixel.B -ge 225 -and $maximum - $minimum -le 18
    $isDarkBackground = $Background -eq "Dark" -and
        $pixel.R -le 6 -and $pixel.G -le 6 -and $pixel.B -le 6 -and $maximum - $minimum -le 6
    if ($isLightBackground -or $isDarkBackground)
    {
        $visited[$index] = $true
        $queue.Enqueue($index)
    }
}

for ($x = 0; $x -lt $width; $x += 1)
{
    Add-BackgroundPixel $x 0
    Add-BackgroundPixel $x ($height - 1)
}
for ($y = 0; $y -lt $height; $y += 1)
{
    Add-BackgroundPixel 0 $y
    Add-BackgroundPixel ($width - 1) $y
}

while ($queue.Count -gt 0)
{
    $index = $queue.Dequeue()
    $x = $index % $width
    $y = [Math]::Floor($index / $width)
    Add-BackgroundPixel ($x - 1) $y
    Add-BackgroundPixel ($x + 1) $y
    Add-BackgroundPixel $x ($y - 1)
    Add-BackgroundPixel $x ($y + 1)
}

$output = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$minimumX = $width
$minimumY = $height
$maximumX = 0
$maximumY = 0
for ($y = 0; $y -lt $height; $y += 1)
{
    for ($x = 0; $x -lt $width; $x += 1)
    {
        $index = $y * $width + $x
        if ($visited[$index] -eq $true)
        {
            $output.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            continue
        }

        $pixel = $sourceBitmap.GetPixel($x, $y)
        $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $pixel.R, $pixel.G, $pixel.B))
        $minimumX = [Math]::Min($minimumX, $x)
        $minimumY = [Math]::Min($minimumY, $y)
        $maximumX = [Math]::Max($maximumX, $x)
        $maximumY = [Math]::Max($maximumY, $y)
    }
}

$padding = 8
$minimumX = [Math]::Max(0, $minimumX - $padding)
$minimumY = [Math]::Max(0, $minimumY - $padding)
$maximumX = [Math]::Min($width - 1, $maximumX + $padding)
$maximumY = [Math]::Min($height - 1, $maximumY + $padding)
$rectangle = [System.Drawing.Rectangle]::new(
    $minimumX,
    $minimumY,
    $maximumX - $minimumX + 1,
    $maximumY - $minimumY + 1)
$cropped = $output.Clone($rectangle, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cropped.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)

$cropped.Dispose()
$output.Dispose()
$sourceBitmap.Dispose()
