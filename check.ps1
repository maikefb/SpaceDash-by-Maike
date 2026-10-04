param(
    [ValidateSet("Info", "All")]
    [string]$Mod = "All"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "tools\CompileCheck\CompileCheck.csproj"
$selected = @("Info")
$failed = $false

foreach ($name in $selected) {
    Write-Host "== compilando Core + $name =="
    dotnet build $project -nologo -v q -clp:NoSummary "-p:Mod=$name" 2>&1 |
        Where-Object { $_ -match "error|warning CS" } |
        ForEach-Object { $_ -replace [regex]::Escape($PSScriptRoot + "\"), "" } |
        Sort-Object -Unique
    if ($LASTEXITCODE -ne 0) { $failed = $true; Write-Host "FALHOU: $name" } else { Write-Host "OK: $name" }
}

if ($failed) { exit 1 }
