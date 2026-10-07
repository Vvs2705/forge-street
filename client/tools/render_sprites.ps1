<#
render_sprites.ps1 - chama o Blender 5.2 headless com render_sprites.py (ASCII puro; PowerShell 5.1).

Uso:
  .\render_sprites.ps1 -Model <fbx|glb> -Anims <pasta com 1 FBX por clipe> -Out <pasta>
      [-Dirs 4|8] [-Elev 60] [-Fps 12] [-Size 128] [-Scale 0] [-Yaw 0]
      [-Light flat|matcap|studio] [-Aa OFF|FXAA|5|8|11|16|32] [-Shadow] [-SelfAnims]
  .\render_sprites.ps1 -Teste [-TestDir <pasta>]
      gera o humanoide-placeholder do COE em <TestDir>\fbx e renderiza Idle + Run em 4 direcoes
      para <TestDir>\out (padrao: %TEMP%\fs_sprites_test - caminho curto, MAX_PATH).
Saida: exit 0 e linha "SPRITES_OK" no log; exit 1 e linha "ERRO ..." na falha.
#>
param(
    [string]$Model,
    [string]$Anims,
    [string]$Out,
    [int]$Dirs = 4,
    [double]$Elev = 60,
    [double]$Fps = 12,
    [int]$Size = 128,
    [double]$Scale = 0,
    [double]$Yaw = 0,
    [ValidateSet("flat", "matcap", "studio")][string]$Light = "flat",
    [string]$Aa = "8",
    [switch]$Shadow,
    [switch]$SelfAnims,
    [switch]$Teste,
    [string]$TestDir = (Join-Path $env:TEMP "fs_sprites_test"),
    [string]$Blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe",
    [string]$Placeholder = "C:\Users\VINICIUS\Videos\MEUS PROJETOS\chronicles-of-existence\client\tools\placeholder_humanoid.py"
)

$Script = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "render_sprites.py"
if (-not (Test-Path $Blender)) { Write-Host "ERRO Blender nao encontrado: $Blender"; exit 1 }

function Invoke-Blender([string]$Py, [string[]]$ScriptArgs) {
    # "--" dentro do array chega inteiro ao Blender; --python-exit-code 1 devolve exit 1 em excecao/sys.exit
    $all = @("--background", "--factory-startup", "--python-exit-code", "1", "--python", $Py, "--") + $ScriptArgs
    & $Blender @all | Out-Host   # Out-Host: a funcao devolve SO o exit code (saida no pipeline viraria parte do retorno)
    return $LASTEXITCODE
}

if ($Teste) {
    if (-not (Test-Path $Placeholder)) { Write-Host "ERRO placeholder do COE nao encontrado: $Placeholder"; exit 1 }
    $Fbx = Join-Path $TestDir "fbx"
    $AnimDir = Join-Path $TestDir "anims"
    $Out = Join-Path $TestDir "out"
    New-Item -ItemType Directory -Force -Path $Fbx, $AnimDir, $Out | Out-Null
    $code = Invoke-Blender $Placeholder @($Fbx)
    if ($code -ne 0) { Write-Host "ERRO placeholder falhou (exit $code)"; exit $code }
    Copy-Item -Path (Join-Path $Fbx "Idle.fbx"), (Join-Path $Fbx "Run.fbx") -Destination $AnimDir -Force
    $Model = Join-Path $Fbx "Model.fbx"
    $Anims = $AnimDir
    $Dirs = 4
}
if (-not $Model -or -not $Out) { Write-Host "ERRO informe -Model e -Out (ou -Teste)"; exit 1 }

$a = @("--model", $Model, "--out", $Out, "--dirs", $Dirs, "--elev", $Elev, "--fps", $Fps, "--size", $Size,
       "--scale", $Scale, "--yaw", $Yaw, "--light", $Light, "--aa", $Aa)
if ($SelfAnims) { $a += "--self" } elseif ($Anims) { $a += @("--anims", $Anims) }
if ($Shadow) { $a += "--shadow" }

$t0 = Get-Date
$code = Invoke-Blender $Script $a
Write-Host ("render_sprites: exit {0} em {1:n0} s -> {2}" -f $code, ((Get-Date) - $t0).TotalSeconds, $Out)
exit $code
