@echo off
rem Forge Street: guarda o seu save e o jogo abre como na primeira vez (marco 00 do roteiro).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0usar_save_marco.ps1" -DoZero
pause
