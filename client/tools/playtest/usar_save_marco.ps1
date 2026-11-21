# Forge Street: troca o save do build Windows por um save de marco (docs/ROTEIRO_PLAYTEST_MARCOS.md).
# Uso: powershell -ExecutionPolicy Bypass -File client\tools\playtest\usar_save_marco.ps1 <arquivo .save.txt>
#      powershell -ExecutionPolicy Bypass -File client\tools\playtest\usar_save_marco.ps1 -Voltar   (devolve o save que você tinha antes)
#      powershell -ExecutionPolicy Bypass -File client\tools\playtest\usar_save_marco.ps1 -DoZero   (guarda o save atual e o jogo abre como primeira vez: marco 00)
# No artifact saves-de-marco vêm junto os atalhos de clique duplo: arraste um .save.txt sobre usar_save.bat; do_zero.bat; voltar.bat.
# Nunca apaga sem guardar: o save atual vai para save.txt.antes-do-marco-<data> na mesma pasta, o .bak do jogo também, e o espelho do
# registro (fs.save) vai para save.txt.original.reg antes de ser limpo; o -Voltar o importa de volta.
param([string]$Save, [switch]$Voltar, [switch]$DoZero)
$ErrorActionPreference = 'Stop'
$dir = Join-Path $env:USERPROFILE 'AppData\LocalLow\V-STACK\Forge Street'
$principal = Join-Path $dir 'save.txt'
if (Get-Process -Name 'ForgeStreet*' -ErrorAction SilentlyContinue) { throw 'Feche o Forge Street antes (o autosave de 5 s regravaria o save).' }
New-Item -ItemType Directory -Force -Path $dir | Out-Null
# Sem save.txt o jogo le o espelho do PlayerPrefs (fs.save; no Windows fica no registro, HKCU\Software\V-STACK\Forge Street): sem
# limpar o espelho, "do zero" abriria o ultimo save gravado (revisao do TD no #185). O registro original vai para save.txt.original.reg.
$prefs = 'HKCU:\Software\V-STACK\Forge Street'
function LimpaEspelho {
    if (-not (Test-Path $prefs)) { return }
    (Get-Item $prefs).Property | Where-Object { $_ -like 'fs.save*' } | ForEach-Object { Remove-ItemProperty -Path $prefs -Name $_ }
}
$original = Join-Path $dir 'save.txt.original'   # o save de antes do PRIMEIRO marco: gravado uma vez so; e' para ele que o -Voltar volta
if (-not $Voltar -and -not (Test-Path $original)) {
    if (Test-Path $principal) { Copy-Item $principal $original } else { Set-Content -Path $original -Value '' -NoNewline }
    if (Test-Path $prefs) { & reg.exe export 'HKCU\Software\V-STACK\Forge Street' "$original.reg" /y | Out-Null }
}
if ($Voltar) {
    if (-not (Test-Path $original)) { throw "Nenhum save original guardado em $dir." }
    $ultimo = Get-Item $original
    if (Test-Path $principal) { Copy-Item $principal "$principal.marco-$(Get-Date -Format yyyyMMdd-HHmmss)" }
    if (Test-Path "$principal.bak") { Rename-Item "$principal.bak" "save.txt.bak.marco-$(Get-Date -Format yyyyMMdd-HHmmss)" }
    if ($ultimo.Length -eq 0) {
        if (Test-Path $principal) { Rename-Item $principal "save.txt.marco-fim-$(Get-Date -Format yyyyMMdd-HHmmss)" }
        Write-Host 'Voce nao tinha save antes: o jogo abre do zero.'
    } else { Copy-Item $ultimo.FullName $principal -Force; Write-Host 'Devolvido: o save que voce tinha antes do primeiro marco.' }
    LimpaEspelho   # o espelho tem o ultimo marco jogado; volta o de antes (ou nenhum)
    if (Test-Path "$original.reg") { & reg.exe import "$original.reg" 2>$null | Out-Null; Rename-Item "$original.reg" "save.txt.original.reg.usado-$(Get-Date -Format yyyyMMdd-HHmmss)" }
    Rename-Item $original "save.txt.original.usado-$(Get-Date -Format yyyyMMdd-HHmmss)"   # a proxima rodada de playtest guarda o original de novo
    exit 0
}
if ($DoZero) {
    $carimbo = Get-Date -Format yyyyMMdd-HHmmss
    if (Test-Path $principal) { Rename-Item $principal "save.txt.antes-do-marco-$carimbo" }
    if (Test-Path "$principal.bak") { Rename-Item "$principal.bak" "save.txt.bak.antes-do-marco-$carimbo" }
    LimpaEspelho   # guardado em save.txt.original.reg na 1a troca
    Write-Host 'Pronto: sem save. O jogo abre como na primeira vez (marco 00). Para voltar ao seu save: voltar.bat'
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
