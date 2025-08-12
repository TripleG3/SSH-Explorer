param(
    [string]$Base = 'C:\Users\micha\OneDrive\Pictures\From Cory\Logo',
    [int]$Max = 500
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Base)) {
    Write-Host 'BASE_NOT_FOUND'
    exit 2
}

Add-Type -AssemblyName System.Drawing -ErrorAction SilentlyContinue | Out-Null

$files = Get-ChildItem -LiteralPath $Base -Recurse -File -Include *.png,*.jpg,*.jpeg |
    Select-Object -First $Max |
    ForEach-Object {
        $p = $_.FullName
        $w = $null
        $h = $null
        try {
            $fs = [System.IO.File]::Open($p, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            try {
                $img = [System.Drawing.Image]::FromStream($fs, $false, $false)
                $w = $img.Width
                $h = $img.Height
                $img.Dispose()
            } finally {
                $fs.Dispose()
            }
        } catch {
            # ignore failures on non-image or locked files
        }
        [PSCustomObject]@{
            Path   = $p
            Name   = $_.Name
            Dir    = $_.DirectoryName
            Width  = $w
            Height = $h
            SizeMB = [math]::Round(($_.Length / 1MB), 2)
            Ratio  = if ($w -and $h -and $h -ne 0) { [math]::Round(($w / $h), 3) } else { $null }
        }
    }

$backgrounds = $files |
    Where-Object { $_.Width -ge 1600 -and $_.Height -ge 900 -and $_.Ratio -ge 1.5 } |
    Sort-Object @{Expression = { $_.Width * $_.Height }; Descending = $true}, SizeMB -Descending |
    Select-Object -First 10

$icons = $files |
    Where-Object { $_.Width -le 512 -and $_.Height -le 512 } |
    Sort-Object @{Expression = { [math]::Max($_.Width, $_.Height) }; Descending = $true}, SizeMB |
    Select-Object -First 30

# Highlight likely pin/unpin icons
$pinIcons = $icons | Where-Object { $_.Name -match 'pin|pushpin|tack' }
$unpinIcons = $icons | Where-Object { $_.Name -match 'unpin|un-pin|un_pin|un pin' }

[PSCustomObject]@{
    Backgrounds = $backgrounds
    Icons       = $icons
    PinIcons    = $pinIcons
    UnpinIcons  = $unpinIcons
} | ConvertTo-Json -Depth 6
