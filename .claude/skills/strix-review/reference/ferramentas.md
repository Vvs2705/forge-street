# Arsenal ofensivo — todas gratuitas, dirigidas por você

Nenhuma destas ferramentas usa IA nem chama API paga. Elas são os "braços"; você é o cérebro. O Strix embrulha essencialmente este mesmo conjunto (Nuclei, Caido, Playwright etc.) — aqui usamos as versões open-source direto.

| Ferramenta | Para quê | Licença | Como Claude dirige |
|---|---|---|---|
| **nuclei** | CVEs e misconfigs por templates | MIT | roda e lê o output |
| **httpx** | quais hosts respondem + fingerprint | MIT | recon inicial |
| **subfinder** | enumerar subdomínios | MIT | recon inicial |
| **ffuf** | fuzzing de dir/param/valores | MIT | brute controlado |
| **sqlmap** | detectar e explorar SQLi | GPL | prova de SQLi |
| **Playwright** | browser p/ XSS, CSRF, auth | Apache-2.0 | script Python |
| **curl / httpie** | forjar requests HTTP manuais | MIT/curl | ataque manual |
| **jq** | fatiar respostas JSON | MIT | parsing |
| **Caido** *(opcional)* | proxy de interceptação (GUI) | edição free | inspeção manual |

`nuclei`, `httpx`, `subfinder`, `ffuf` são binários em Go (ProjectDiscovery). `sqlmap` é Python. Playwright você provavelmente já tem instalado pelo scrapling.

---

## Instalação
Use o script pronto:
- **Windows**: `scripts/setup.ps1`
- **Linux/Mac**: `scripts/setup.sh`

Se preferir manual, o essencial:
```bash
# Go tools (ProjectDiscovery) — precisa do Go instalado
go install github.com/projectdiscovery/nuclei/v3/cmd/nuclei@latest
go install github.com/projectdiscovery/httpx/cmd/httpx@latest
go install github.com/projectdiscovery/subfinder/v2/cmd/subfinder@latest
go install github.com/ffuf/ffuf/v2@latest

# Python
pip install sqlmap-python httpie
python -m playwright install chromium   # se ainda não tiver

# Templates do nuclei (primeira vez)
nuclei -update-templates
```
Se `go` não estiver instalado, dá pra baixar os binários pré-compilados das releases do GitHub de cada projeto (ProjectDiscovery). Registre o que faltar e siga com o que tiver.

Wordlists (para ffuf): o repositório **SecLists** é o padrão. Se não tiver, `ffuf` aceita qualquer arquivo de palavras, uma por linha.

---

## Receitas rápidas

### Recon encadeado (black-box)
```bash
subfinder -d alvo.com.br -silent -o subs.txt
httpx -l subs.txt -sc -title -tech-detect -o alive.txt
nuclei -l alive.txt -severity medium,high,critical -o nuclei.txt
```

### SQLi (não-destrutivo)
```bash
# Começar leve; nunca dumpe dados reais em produção
sqlmap -u "https://alvo.com.br/api/item?id=1" --batch --level 2 --risk 2 --banner
# Só se confirmado e AUTORIZADO a extrair: limite a prova inócua
# sqlmap ... --current-db   (evite --dump em produção)
```

### Fuzzing de endpoints ocultos
```bash
ffuf -u https://alvo.com.br/FUZZ -w wordlist.txt -mc 200,301,302,401,403
```

### IDOR manual (com curl)
```bash
# Autentique como usuário A e capture o cookie/token; então troque o id
curl -s -H "Authorization: Bearer $TOKEN_A" https://alvo.com.br/api/pedido/1001 | jq .
curl -s -H "Authorization: Bearer $TOKEN_A" https://alvo.com.br/api/pedido/1002 | jq .
# Se 1002 pertence a OUTRO tenant e retorna dados → IDOR
```

### XSS / auth com Playwright (esqueleto Python)
```python
from playwright.sync_api import sync_playwright

PAYLOAD = "<script>window.__xss=1</script>"

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()
    # injeta payload num campo refletido
    page.goto(f"https://alvo.com.br/busca?q={PAYLOAD}")
    executed = page.evaluate("() => window.__xss === 1")
    print("XSS EXECUTOU" if executed else "não executou (só refletiu, talvez)")
    browser.close()
```

### nuclei com foco (tecnologia detectada)
```bash
nuclei -u https://alvo.com.br -tags cve,exposure,misconfig -severity high,critical
```

---

## Regras ao usar o arsenal
- **Escopo/autorização confirmados** antes de qualquer disparo ativo (ver SKILL.md).
- **Produção = payload não-destrutivo.** sqlmap sem `--dump`; ffuf com rate baixo; nada de flood.
- **Rate limit**: adicione `-rate` / `-t` baixos em ferramentas de rede para não derrubar o alvo nem disparar WAF sem querer.
- As ferramentas APONTAM; você CONFIRMA. Todo achado precisa do PoC reproduzível da Fase 4.
