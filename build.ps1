param(
    [ValidateSet("Info", "All")]
    [string]$Mod = "All"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$modsDir = $root; while ((Split-Path $modsDir -Leaf) -ne "Mods") { $modsDir = Split-Path $modsDir -Parent }
$targets = @{ Info = "SpaceDash" }
$marker = ".maike-lcd-build"

$selected = if ($Mod -eq "All") { $targets.Keys } else { @($Mod) }

foreach ($name in $selected) {
    $dest = Join-Path $modsDir $targets[$name]
    # Monta em staging e troca por rename no fim: o jogo nunca ve a arvore pela metade.
    $stage = "$dest.build"

    if ((Test-Path $dest) -and -not (Test-Path (Join-Path $dest $marker))) {
        throw "Pasta '$dest' existe e nao foi gerada por este build. Remova ou renomeie manualmente."
    }
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }

    New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item -Recurse -Force (Join-Path $root "Core\*") $stage
    Copy-Item -Recurse -Force (Join-Path $root "$name\*") $stage

    # O jogo compila cada pasta de primeiro nivel de Data\Scripts como um assembly separado;
    # Core e o mod precisam ficar juntos numa unica pasta.
    $scripts = Join-Path $stage "Data\Scripts"
    $merged = Join-Path $scripts $targets[$name]
    New-Item -ItemType Directory -Path $merged | Out-Null
    Move-Item (Join-Path $scripts "Core") $merged
    Move-Item (Join-Path $scripts $name) $merged
    Set-Content -Path (Join-Path $stage $marker) -Value (Get-Date -Format o)

    if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
    Move-Item $stage $dest

    $cs = (Get-ChildItem -Recurse -Filter *.cs $dest | Measure-Object).Count
    Write-Host "$($targets[$name]) <- Core + $name  ($cs arquivos .cs)"
}
