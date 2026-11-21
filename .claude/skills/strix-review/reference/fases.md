# Playbook detalhado — 5 fases

Leia a fase que você está executando. Cada fase assume que a autorização e o escopo já foram confirmados (ver SKILL.md).

## Índice
1. Reconhecimento
2. Priorização
3. Exploração
4. Validação com PoC
5. Relatório + correção

---

## FASE 1 — Reconhecimento

Objetivo: entender a superfície de ataque antes de tocar nela.

### White-box (código local ou repositório)
1. Mapeie a estrutura: onde ficam rotas/endpoints, models, middleware de auth, config.
2. No FastAPI: liste todos os `@app`/`@router` e anote quais têm dependência de auth e quais não têm.
3. Identifique o modelo multi-tenant: como o código separa dados de tenants/usuários diferentes? Toda query filtra por dono?
4. Leia `.env.example`, `settings`, config de CORS, cookies, e deps (`requirements.txt`, `package.json`).
5. Saída desta etapa: uma lista de endpoints + o que cada um espera + como autentica.

### Black-box (URL de app em produção/staging)
Execute em ordem, encadeando a saída de um na entrada do próximo:
```bash
# 1. Subdomínios
subfinder -d alvo.com.br -silent -o subs.txt

# 2. Quais respondem + fingerprint de tecnologia
httpx -l subs.txt -sc -title -tech-detect -o alive.txt

# 3. Vulnerabilidades/misconfigs conhecidas (templates da comunidade)
nuclei -l alive.txt -severity low,medium,high,critical -o nuclei.txt

# 4. Diretórios e endpoints ocultos
ffuf -u https://alvo.com.br/FUZZ -w /caminho/wordlist.txt -mc 200,301,302,401,403 -o ffuf.json
```
Interprete os resultados VOCÊ — o nuclei aponta, mas quem decide o que vale explorar é você.

### API (OpenAPI / Swagger / Postman)
- Se houver spec, enumere cada endpoint declarado (método, parâmetros, auth exigida) em vez de descobrir por crawl.
- Monte a matriz: endpoint × precisa auth? × que role? × que parâmetros aceita?

**Entregável da Fase 1:** mapa da superfície de ataque (endpoints, tecnologias, pontos de auth, superfície multi-tenant).

---

## FASE 2 — Priorização

Ordene por (probabilidade de existir) × (impacto se explorado). Não teste tudo às cegas — ataque como um atacante real: pelo caminho de maior retorno.

Ordem típica para o stack V-STACK (FastAPI + PostgreSQL + Next.js, multi-tenant):
1. **Controle de acesso / IDOR entre tenants** — o risco nº 1 em SaaS multi-tenant. Trocar um ID e ler dado de outro cliente.
2. **Injeção** — SQLi onde houver query crua fora do ORM; command injection; SSTI.
3. **Auth / JWT / sessão** — assinatura não verificada, expiração ignorada, role manipulável.
4. **SSRF / upload / path traversal** — onde o app busca URL do usuário ou aceita arquivo.
5. **Lógica de negócio** — manipulação de preço/pagamento, race condition, bypass de fluxo.

Use `owasp-checklist.md` como bateria concreta de testes por classe.

---

## FASE 3 — Exploração

Para cada alvo priorizado, escolha a ferramenta e ataque. Detalhe de uso em `ferramentas.md`.

### Controle de acesso / IDOR
- Autentique como usuário A, capture um request que acessa um recurso por ID.
- Repita trocando o ID por um que pertence ao usuário/tenant B (ou incremente/decremente).
- Se retornar dado de B → IDOR confirmado. Registre request/resposta.
- Teste também: acessar rota de admin como usuário comum; forçar `role`/`is_admin` em payloads.

### Injeção
- **SQLi**: `sqlmap` no parâmetro suspeito (`--batch --level 2 --risk 2` para começar leve). Confirme com extração de uma linha inócua (ex.: `version()`), nunca dumpe dados reais em produção.
- **Command/SSTI/NoSQL**: payloads manuais via `curl`; procure eco do payload, delay, ou erro que revele execução.

### Client-side / auth (browser)
- Script Playwright (Python) para: injetar payload XSS e ver se executa; testar CSRF (request cross-site sem token); clickjacking (iframe do alvo); e bypass de fluxo de login (pular etapas, reusar token expirado).
- Exemplo de esqueleto em `ferramentas.md`.

### Conhecidas / fuzzing
- `nuclei` com templates específicos da tecnologia detectada.
- `ffuf` para fuzzing de parâmetros e valores (ex.: enumerar IDs válidos, achar parâmetros escondidos).

**Regra de produção:** payload não-destrutivo sempre. Prove que dá pra fazer, sem fazer o dano.

---

## FASE 4 — Validação com PoC

Este é o passo que separa o strix-review de um scanner qualquer. Sem PoC reproduzível, não é achado confirmado.

Para cada candidato a achado, produza:
1. **O gatilho exato**: o request completo (método, URL, headers, corpo) ou o script que dispara a falha.
2. **Passos de reprodução**: numerados, do estado inicial até a evidência, de forma que outra pessoa reproduza sem adivinhar.
3. **Evidência de impacto**: o dado vazado (mascarado/mínimo), o comando que rodou, a sessão assumida, o preço alterado.
4. **CVSS**: score e vetor, quando aplicável, para calibrar severidade.

Se não reproduz de forma consistente → marque como **suspeita**, liste o que falta, e siga. Não infle o relatório.

---

## FASE 5 — Relatório + correção

- Gere o relatório no formato do SKILL.md em `docs/pentest/PENTEST-<AAAA-MM-DD>.md`.
- Coloque CRÍTICO em produção no topo, com plano de correção imediato.
- Ofereça aplicar as correções **uma por vez**. Depois de cada fix: re-rode o teste que confirmou a falha e prove que fechou (fix → re-scan → verde). Só então passe para o próximo.
- Nunca escreva valor de segredo no relatório; referencie `arquivo:linha` e mande rotacionar.
