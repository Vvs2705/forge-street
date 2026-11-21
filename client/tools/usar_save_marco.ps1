# Forge Street: troca o save do build Windows por um save de marco (docs/ROTEIRO_PLAYTEST_MARCOS.md).
# Uso: powershell -ExecutionPolicy Bypass -File client\tools\usar_save_marco.ps1 <arquivo .save.txt>
#      powershell -ExecutionPolicy Bypass -File client\tools\usar_save_marco.ps1 -Voltar   (devolve o save que você tinha antes)
# Nunca apaga nada: o save atual vai para save.txt.antes-do-marco-<data> na mesma pasta, e o .bak do jogo também é guardado.
param([string]$Save, [switch]$Voltar)
$ErrorActionPreference = 'Stop'
$dir = Join-Path $env:USERPROFILE 'AppData\LocalLow\V-STACK\Forge Street'
$principal = Join-Path $dir 'save.txt'
if (Get-Process -Name 'ForgeStreet*' -ErrorAction SilentlyContinue) { throw 'Feche o Forge Street antes (o autosave de 5 s regravaria o save).' }
New-Item -ItemType Directory -Force -Path $dir | Out-Null
if ($Voltar) {
    $ultimo = Get-ChildItem $dir -Filter 'save.txt.antes-do-marco-*' | Sort-Object Name | Select-Object -Last 1
    if (-not $ultimo) { throw "Nenhum save guardado em $dir." }
    if (Test-Path $principal) { Copy-Item $principal "$principal.marco-$(Get-Date -Format yyyyMMdd-HHmmss)" }
    Copy-Item $ultimo.FullName $principal -Force
    if (Test-Path "$principal.bak") { Rename-Item "$principal.bak" "save.txt.bak.marco-$(Get-Date -Format yyyyMMdd-HHmmss)" }
    Write-Host "Devolvido: $($ultimo.Name) -> save.txt"
    exit 0
}
if (-not $Save -or -not (Test-Path $Save)) { throw 'Passe o caminho de um .save.txt (do artifact saves-de-marco).' }
$primeira = Get-Content $Save -TotalCount 1
if ($primeira -notlike 'sha256=*') { throw "$Save não parece um save do Forge Street (falta o envelope sha256=)." }
$carimbo = Get-Date -Format yyyyMMdd-HHmmss
if (Test-Path $principal) { Copy-Item $principal "$principal.antes-do-marco-$carimbo" }
if (Test-Path "$principal.bak") { Rename-Item "$principal.bak" "save.txt.bak.antes-do-marco-$carimbo" }   # senão o jogo poderia cair no .bak antigo
Copy-Item $Save $principal -Force
Write-Host "Pronto: $(Split-Path $Save -Leaf) -> $principal"
Write-Host 'Abra o ForgeStreet.exe. Para voltar ao seu save: usar_save_marco.ps1 -Voltar'
