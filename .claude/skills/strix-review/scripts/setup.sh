#!/usr/bin/env bash
# strix-review — instalador do arsenal ofensivo (Linux/Mac)
# Todas as ferramentas são open-source e gratuitas. Nenhuma usa API paga.
set -u

echo "== strix-review :: setup do arsenal =="

# --- Go tools (ProjectDiscovery + ffuf) ---
if command -v go >/dev/null 2>&1; then
  echo "[go] instalando nuclei, httpx, subfinder, ffuf..."
  go install github.com/projectdiscovery/nuclei/v3/cmd/nuclei@latest
  go install github.com/projectdiscovery/httpx/cmd/httpx@latest
  go install github.com/projectdiscovery/subfinder/v2/cmd/subfinder@latest
  go install github.com/ffuf/ffuf/v2@latest
  echo "[go] lembre-se de ter \$HOME/go/bin no PATH"
else
  echo "[go] Go não encontrado. Pule ou instale via https://go.dev/dl/"
  echo "     Alternativa: baixe os binários pré-compilados das releases do ProjectDiscovery no GitHub."
fi

# --- Python tools ---
echo "[pip] instalando sqlmap e httpie..."
pip install --upgrade sqlmap-python httpie 2>/dev/null || pip3 install --upgrade sqlmap-python httpie

# --- Playwright (pode já existir via scrapling) ---
if python -c "import playwright" 2>/dev/null; then
  echo "[playwright] já instalado; garantindo chromium..."
  python -m playwright install chromium
else
  echo "[playwright] instalando..."
  pip install playwright && python -m playwright install chromium
fi

# --- Templates do nuclei ---
if command -v nuclei >/dev/null 2>&1; then
  echo "[nuclei] atualizando templates..."
  nuclei -update-templates
fi

# --- jq (parsing JSON) ---
if ! command -v jq >/dev/null 2>&1; then
  echo "[jq] não encontrado. Instale: apt/brew install jq"
fi

echo "== setup concluído. Verifique com: nuclei -version; httpx -version; ffuf -V =="
