param(
    [ValidateSet("Info", "All")]
    [string]$Mod = "All"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$modsDir = Split-Path $root -Parent
$targets = @{ Info = "SpaceDash" }
$marker = ".maike-lcd-build"

$selected = if ($Mod -eq "All") { $targets.Keys } else { @($Mod) }

foreach ($name in $selected) {
    $dest = Join-Path $modsDir $targets[$name]

    if (Test-Path $dest) {
        if (-not (Test-Path (Join-Path $dest $marker))) {
            throw "Pasta '$dest' existe e nao foi gerada por este build. Remova ou renomeie manualmente."
        }
        Remove-Item -Recurse -Force $dest
    }

    New-Item -ItemType Directory -Path $dest | Out-Null
    Copy-Item -Recurse -Force (Join-Path $root "Core\*") $dest
    Copy-Item -Recurse -Force (Join-Path $root "$name\*") $dest

    # O jogo compila cada pasta de primeiro nivel de Data\Scripts como um assembly separado;
    # Core e o mod precisam ficar juntos numa unica pasta.
    $scripts = Join-Path $dest "Data\Scripts"
    $merged = Join-Path $scripts $targets[$name]
    New-Item -ItemType Directory -Path $merged | Out-Null
    Move-Item (Join-Path $scripts "Core") $merged
    Move-Item (Join-Path $scripts $name) $merged
    Set-Content -Path (Join-Path $dest $marker) -Value (Get-Date -Format o)

    $cs = (Get-ChildItem -Recurse -Filter *.cs $dest | Measure-Object).Count
    Write-Host "$($targets[$name]) <- Core + $name  ($cs arquivos .cs)"
}
