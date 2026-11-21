# tj_limpar.ps1 — Time de Jogos: apaga SÓ lixo regenerável de build, numa lista fechada.
# Uso: powershell -ExecutionPolicy Bypass -File .claude\scripts\tj_limpar.ps1 -Raiz <repo do jogo> [-Simular]
#
# Apaga (dentro de -Raiz, num projeto Unity = pasta com ProjectSettings\ ao lado):
#   Library\Bee · Temp · obj · Assets\Resources\PerformanceTestRun* (+ .meta) · Builds\testes\*.apk
#   e Assets\Resources (+ .meta) se ficar vazia depois disso.
# Recusa: caminho fora de -Raiz · -Raiz que não é repositório git · qualquer coisa rastreada pelo git ·
#         qualquer caminho com "_upstream" · .claude, .git, Assets (exceto os itens acima), ProjectSettings, Packages.
# Saída: uma linha por item (APAGADO / SIMULADO / RECUSADO <motivo>) e exit 0; exit 2 se -Raiz inválida.
param(
  [Parameter(Mandatory = $true)][string]$Raiz,
  [switch]$Simular
)
$ErrorActionPreference = "Continue"  # git escreve em stderr; no PS 5.1 "Stop" transformaria isso em erro fatal

if (-not (Test-Path -LiteralPath $Raiz -PathType Container)) { Write-Output "ERRO: -Raiz não existe: $Raiz"; exit 2 }
$root = (Resolve-Path -LiteralPath $Raiz).Path.TrimEnd('\', '/')
if ($root -match '_upstream') { Write-Output "ERRO: -Raiz dentro de _upstream é proibido"; exit 2 }
$top = (& git -C $root rev-parse --show-toplevel 2>$null)
if (-not $top) { Write-Output "ERRO: -Raiz não é repositório git: $root"; exit 2 }

function Test-Tracked([string]$full) {
  $rel = $full.Substring($root.Length).TrimStart('\', '/')
  if (-not $rel) { return $true }
  $out = & git -C $root ls-files -- "$rel" 2>$null
  return [bool]$out
}

$alvos = New-Object System.Collections.Generic.List[string]
# exclusão relativa à raiz: a própria raiz pode estar em ...\Temp\...
$projetos = Get-ChildItem -LiteralPath $root -Directory -Recurse -Depth 3 -Filter ProjectSettings -ErrorAction SilentlyContinue |
  Where-Object { ("\" + $_.FullName.Substring($root.Length).TrimStart('\')) -notmatch '\\(Library|Temp|_upstream|node_modules|\.git)\\' } |
  ForEach-Object { $_.Parent.FullName }
foreach ($p in $projetos) {
  # Logs fica de fora: guarda XML/log de evidência de testes
  foreach ($d in @("Library\Bee", "Temp", "obj")) { $x = Join-Path $p $d; if (Test-Path -LiteralPath $x) { $alvos.Add($x) } }
  $res = Join-Path $p "Assets\Resources"
  if (Test-Path -LiteralPath $res) {
    Get-ChildItem -LiteralPath $res -Filter "PerformanceTestRun*" -File -ErrorAction SilentlyContinue | ForEach-Object { $alvos.Add($_.FullName) }
  }
  $bt = Join-Path $p "Builds\testes"
  if (Test-Path -LiteralPath $bt) {
    Get-ChildItem -LiteralPath $bt -Filter "*.apk" -File -ErrorAction SilentlyContinue | ForEach-Object { $alvos.Add($_.FullName) }
  }
}

foreach ($a in $alvos) {
  $full = [IO.Path]::GetFullPath($a)
  if (-not $full.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { Write-Output "RECUSADO fora da raiz: $full"; continue }
  if ($full -match '_upstream') { Write-Output "RECUSADO _upstream: $full"; continue }
  if (Test-Tracked $full) { Write-Output "RECUSADO rastreado pelo git: $full"; continue }
  if ($Simular) { Write-Output "SIMULADO $full"; continue }
  try {
    if (Test-Path -LiteralPath $full -PathType Container) {
      & cmd /c "rmdir /s /q `"$full`"" 2>$null   # robusto com caminhos longos do Bee
      if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop }
    } else { Remove-Item -LiteralPath $full -Force -ErrorAction Stop }
    Write-Output "APAGADO $full"
  } catch { Write-Output "RECUSADO erro ao apagar ($($_.Exception.Message)): $full" }
}

# Assets\Resources vazia e não rastreada -> remove a pasta e o .meta
foreach ($p in $projetos) {
  $res = Join-Path $p "Assets\Resources"
  if ((Test-Path -LiteralPath $res) -and -not (Get-ChildItem -LiteralPath $res -Force | Select-Object -First 1) -and -not (Test-Tracked $res)) {
    if ($Simular) { Write-Output "SIMULADO $res (vazia)" }
    else {
      Remove-Item -LiteralPath $res -Force
      $meta = "$res.meta"
      if ((Test-Path -LiteralPath $meta) -and -not (Test-Tracked $meta)) { Remove-Item -LiteralPath $meta -Force }
      Write-Output "APAGADO $res (vazia) + .meta"
    }
  }
}
if ($alvos.Count -eq 0) { Write-Output "Nada a limpar em $root" }
exit 0
