@echo off
rem Forge Street: arraste um .save.txt (do artifact saves-de-marco) sobre este arquivo, com o jogo fechado.
if "%~1"=="" (echo Arraste um arquivo .save.txt sobre usar_save.bat.& pause & exit /b 1)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0usar_save_marco.ps1" "%~1"
pause
