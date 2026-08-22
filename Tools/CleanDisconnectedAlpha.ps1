param(
    [Parameter(Mandatory = $true)] [string] $Source,
    [Parameter(Mandatory = $true)] [string] $Destination
)

Add-Type -AssemblyName System.Drawing

$loadedBitmap = [System.Drawing.Bitmap]::FromFile($Source)
$sourceBitmap = $loadedBitmap.Clone(
    [System.Drawing.Rectangle]::new(0, 0, $loadedBitmap.Width, $loadedBitmap.Height),
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$loadedBitmap.Dispose()

$width = $sourceBitmap.Width
$height = $sourceBitmap.Height
$visited = [bool[]]::new($width * $height)
$largestComponent = [System.Collections.Generic.List[int]]::new()
$directions = @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))

# 알파가 연결된 영역을 탐색해 실제 이미지 본체인 가장 큰 성분을 찾습니다.
for ($y = 0; $y -lt $height; $y += 1)
{
    for ($x = 0; $x -lt $width; $x += 1)
    {
        $startIndex = $y * $width + $x
        if ($visited[$startIndex] -eq $true -or $sourceBitmap.GetPixel($x, $y).A -eq 0)
        {
            continue
        }

        $component = [System.Collections.Generic.List[int]]::new()
        $queue = [System.Collections.Generic.Queue[int]]::new()
        $queue.Enqueue($startIndex)
        $visited[$startIndex] = $true

        while ($queue.Count -gt 0)
        {
            $index = $queue.Dequeue()
            $component.Add($index)
            $currentX = $index % $width
            $currentY = [Math]::Floor($index / $width)

            foreach ($direction in $directions)
            {
                $nextX = $currentX + $direction[0]
                $nextY = $currentY + $direction[1]
                if ($nextX -lt 0 -or $nextY -lt 0 -or $nextX -ge $width -or $nextY -ge $height)
                {
                    continue
                }

                $nextIndex = $nextY * $width + $nextX
                if ($visited[$nextIndex] -eq $false -and $sourceBitmap.GetPixel($nextX, $nextY).A -gt 0)
                {
                    $visited[$nextIndex] = $true
                    $queue.Enqueue($nextIndex)
                }
            }
        }

        if ($component.Count -gt $largestComponent.Count)
        {
            $largestComponent = $component
        }
    }
}

# 본체와 연결되지 않은 생성 노이즈를 완전 투명 픽셀로 정리합니다.
$keep = [bool[]]::new($width * $height)
foreach ($index in $largestComponent)
{
    $keep[$index] = $true
}

$output = [System.Drawing.Bitmap]::new($width, $height,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$removedPixelCount = 0
for ($y = 0; $y -lt $height; $y += 1)
{
    for ($x = 0; $x -lt $width; $x += 1)
    {
        $index = $y * $width + $x
        $pixel = $sourceBitmap.GetPixel($x, $y)
        if ($pixel.A -gt 0 -and $keep[$index] -eq $false)
        {
            $output.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            $removedPixelCount += 1
        }
        else
        {
            $output.SetPixel($x, $y, $pixel)
        }
    }
}

$resolvedSource = [System.IO.Path]::GetFullPath($Source)
$resolvedDestination = [System.IO.Path]::GetFullPath($Destination)
$savePath = $resolvedDestination
if ($resolvedSource -eq $resolvedDestination)
{
    $savePath = [System.IO.Path]::Combine(
        [System.IO.Path]::GetDirectoryName($resolvedDestination),
        [System.IO.Path]::GetFileNameWithoutExtension($resolvedDestination) + ".cleaning.png")
}

$output.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
$output.Dispose()
$sourceBitmap.Dispose()

if ($savePath -ne $resolvedDestination)
{
    Move-Item -LiteralPath $savePath -Destination $resolvedDestination -Force
}

Write-Output "Kept $($largestComponent.Count) connected pixels and removed $removedPixelCount stray pixels."
