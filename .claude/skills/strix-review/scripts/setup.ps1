# strix-review -- instalador do arsenal ofensivo (Windows / PowerShell)
# Todas as ferramentas sao open-source e gratuitas. Nenhuma usa API paga.

Write-Host "== strix-review :: setup do arsenal ==" -ForegroundColor Cyan

# --- Go tools (ProjectDiscovery + ffuf) ---
if (Get-Command go -ErrorAction SilentlyContinue) {
    Write-Host "[go] instalando nuclei, httpx, subfinder, ffuf..." -ForegroundColor Yellow
    go install github.com/projectdiscovery/nuclei/v3/cmd/nuclei@latest
    go install github.com/projectdiscovery/httpx/cmd/httpx@latest
    go install github.com/projectdiscovery/subfinder/v2/cmd/subfinder@latest
    go install github.com/ffuf/ffuf/v2@latest
    Write-Host "[go] adicione %USERPROFILE%\go\bin ao PATH se ainda nao estiver" -ForegroundColor DarkGray
} else {
    Write-Host "[go] Go nao encontrado. Instale via 'winget install GoLang.Go' ou https://go.dev/dl/" -ForegroundColor Red
    Write-Host "     Alternativa: baixe os binarios pre-compilados das releases do ProjectDiscovery no GitHub." -ForegroundColor DarkGray
}

# --- Python tools ---
Write-Host "[pip] instalando sqlmap e httpie..." -ForegroundColor Yellow
pip install --upgrade sqlmap-python httpie

# --- Playwright (pode ja existir via scrapling) ---
$hasPlaywright = $false
try { python -c "import playwright" 2>$null; if ($LASTEXITCODE -eq 0) { $hasPlaywright = $true } } catch {}
if ($hasPlaywright) {
    Write-Host "[playwright] ja instalado; garantindo chromium..." -ForegroundColor Yellow
    python -m playwright install chromium
} else {
    Write-Host "[playwright] instalando..." -ForegroundColor Yellow
    pip install playwright
    python -m playwright install chromium
}

# --- Templates do nuclei ---
if (Get-Command nuclei -ErrorAction SilentlyContinue) {
    Write-Host "[nuclei] atualizando templates..." -ForegroundColor Yellow
    nuclei -update-templates
}

# --- jq ---
if (-not (Get-Command jq -ErrorAction SilentlyContinue)) {
    Write-Host "[jq] nao encontrado. Instale: 'winget install jqlang.jq'" -ForegroundColor DarkGray
}

Write-Host "== setup concluido. Verifique: nuclei -version; httpx -version; ffuf -V ==" -ForegroundColor Green
