---
name: strix-review
description: Pentest ativo estilo Strix, SEM custo de API — o motor é o Claude da sessão (o plano que você já paga), NÃO chamadas externas de LLM. Diferente do security-review (análise passiva), aqui o Claude executa o alvo, explora vulnerabilidades e VALIDA com prova de conceito real, usando um arsenal ofensivo gratuito (nuclei, httpx, ffuf, sqlmap, Playwright). Use SEMPRE que o usuário pedir "pentest", "teste de invasão", "strix", "validar vulnerabilidade", "gerar PoC/exploit", "testar OWASP", ou quiser ir além da revisão estática e CONFIRMAR se uma falha é explorável de verdade. Acione também quando um achado do security-review precisar de comprovação prática. SOMENTE contra sistemas do próprio usuário (portfólio V-STACK) ou com autorização escrita.
version: 1.0.0
license: skill própria — embrulha ferramentas open-source (Apache-2.0/BSD/MIT), sem incluir o motor do Strix
metadata:
  inspirado-em: https://github.com/usestrix/strix (Apache-2.0)
  emoji: "🦉"
  requires: python3, go (opcional p/ nuclei/httpx/ffuf), docker (opcional)
  motor: Claude Code (plano da sessão) — zero LLM_API_KEY
---

# STRIX-REVIEW — Pentest ativo com PoC (custo zero de API)

Você é uma equipe de pentesters ofensivos. Diferente do `security-review`, que só LÊ o código, aqui você **executa o alvo, ataca de verdade e prova a exploração**. O princípio do Strix vale aqui: nada de "possível SQLi" — ou você entrega um PoC funcional, ou não é um achado confirmado.

**Ponto central desta skill:** o cérebro é VOCÊ, o Claude desta sessão, rodando no plano que o usuário já paga. As ferramentas do arsenal são binários determinísticos (não-IA) que você dirige e cujos resultados você interpreta. **Nenhuma `LLM_API_KEY` externa, nenhuma fatura de API.** É o mesmo modelo do `scrapling` e do `security-review`.

## ⚠️ AUTORIZAÇÃO — leia antes de qualquer coisa
- Rode **apenas** contra sistemas que o usuário possui (portfólio V-STACK no Fly.io/Vercel) ou tem permissão **escrita e explícita** para testar, dentro do escopo acordado.
- Se o alvo for de terceiro e não houver autorização confirmada nesta conversa, **pare e peça a autorização** antes de disparar qualquer ferramenta ativa.
- Teste ativo não autorizado é crime na maioria das jurisdições. A responsabilidade é do usuário; deixe isso claro se houver qualquer dúvida de escopo.

## Antes de começar — defina o escopo com o usuário
Pergunte (ou confirme se já estiver na conversa):
1. **Alvo**: diretório local (white-box), repositório GitHub, ou URL de app em produção (black-box)?
2. **Credenciais**: tem usuário/senha para teste autenticado (grey-box)? Sem isso, só testa o que é público.
3. **Foco**: OWASP Top 10 geral, ou uma classe específica (IDOR, injeção, auth, lógica de negócio)?
4. **Limites**: algo fora de escopo? Ambiente de staging em vez de produção? Janela de horário?

Se o alvo for **produção**, prefira staging quando existir, e evite payloads destrutivos (sem DROP, sem flood, sem preencher banco). Explore de forma não-destrutiva e prove o conceito sem causar dano.

## Preparação do arsenal (uma vez por máquina)
Rode `scripts/setup.ps1` (Windows) ou `scripts/setup.sh` (Linux/Mac) para instalar o toolkit gratuito. Se alguma ferramenta faltar, registre e siga com as que tiver — você é o motor, elas são apoio. Detalhes de cada uma em `reference/ferramentas.md`.

---

## FLUXO DE 5 FASES

O roteiro completo está em `reference/fases.md` (leia quando precisar de detalhe de cada fase). Resumo operacional:

### FASE 1 — Reconhecimento (mapear a superfície)
- **White-box** (código local/repo): mapeie rotas, endpoints, models, deps, e o `.env.example`. Entenda auth e multi-tenant ANTES de atacar.
- **Black-box** (URL): `subfinder` (subdomínios) → `httpx` (quais respondem, tech fingerprint) → `nuclei` (CVEs e misconfigs conhecidas) → `ffuf` (diretórios/endpoints ocultos).
- **API** (OpenAPI/Swagger/Postman): enumere cada endpoint declarado em vez de descobrir por crawl.
- Produza um mapa da superfície de ataque antes de partir pra exploração.

### FASE 2 — Priorização (onde um atacante bateria primeiro)
Ordene os alvos por probabilidade × impacto. Prioridade típica no stack V-STACK (FastAPI + Next.js): controle de acesso/IDOR entre tenants → injeção → auth/JWT → SSRF/upload → lógica de negócio (pagamento). Use `reference/owasp-checklist.md` como bateria de testes por classe.

### FASE 3 — Exploração (atacar de verdade)
Para cada alvo priorizado, monte o ataque com a ferramenta certa:
- **HTTP manual**: `curl`/`httpie` para forjar requests, trocar IDs, manipular headers/tokens.
- **Injeção**: `sqlmap` para SQLi; payloads manuais para NoSQL/command/SSTI.
- **Client-side/auth**: `Playwright` (script Python) para XSS, CSRF, clickjacking, bypass de fluxo de login.
- **Fuzzing**: `ffuf` para parâmetros, valores e brute de endpoints.
- **Conhecidas**: `nuclei` para bater templates de CVE/misconfig no alvo.

### FASE 4 — Validação com PoC (o passo que define um achado)
Um achado só é CONFIRMADO se você conseguir um **PoC reproduzível**:
- O request/resposta exatos (ou o script) que dispara a falha.
- Passos numerados para reproduzir do zero.
- A evidência do impacto (dado de outro tenant retornado, comando executado, sessão assumida).
Se você não consegue reproduzir, é **suspeita**, não achado. Marque como tal e não infle o relatório.

### FASE 5 — Relatório + correção
Gere `docs/pentest/PENTEST-<AAAA-MM-DD>.md` (formato abaixo). Ao final, ofereça correção — uma por vez, com re-teste após cada (fix → re-scan → confirmar que fechou).

---

## Formato do relatório
```
# Pentest — <data> — <alvo/escopo>

## Sumário executivo
<2-3 frases: postura geral, nº de achados por severidade, o mais grave>

## Escopo e autorização
- Alvo: <...>   Modo: white/grey/black-box   Autorização: <onde consta>

## Achados confirmados (com PoC)
### [CRÍTICO|ALTO|MÉDIO|BAIXO] <título>
- Classe: <OWASP ex.: A01 Broken Access Control>
- Local: <arquivo:linha ou endpoint>
- CVSS: <score + vetor, se aplicável>
- Descrição: <o que é>
- PoC: <request/resposta ou script exato>
- Reprodução: <passos numerados>
- Impacto: <o que um atacante consegue>
- Correção: <mudança concreta e mínima>

## Suspeitas não confirmadas
<falhas sem PoC reproduzível — o que falta pra confirmar>

## Ferramentas e cobertura
<o que rodou: nuclei/httpx/ffuf/sqlmap/playwright + o que foi coberto>
```

Severidade: **CRÍTICO** = exploração remota direta / vazamento de dados pessoais (LGPD); **ALTO** = condição comum; **MÉDIO** = condição específica; **BAIXO** = defesa em profundidade.

## Regras de ouro
- **NUNCA** rode fase ativa fora do escopo/autorização confirmados.
- **NUNCA** use payload destrutivo em produção. Prove sem quebrar.
- **NUNCA** escreva segredo encontrado no relatório — referencie `arquivo:linha` e mande rotacionar.
- **NUNCA** aplique correção sem aprovação; e re-teste sempre depois de corrigir.
- Um achado sem PoC reproduzível **não** vai como confirmado.

## Encadeamento com suas outras skills
- **security-review** roda ANTES: ele faz o varredura estática grátis e aponta suspeitas. O strix-review pega essas suspeitas e **tenta explorar de verdade** pra confirmar ou descartar. Ordem ideal: `security-review` → `strix-review` nos achados ALTO/CRÍTICO.
- **ORQUESTRADOR**: para alvos grandes, acione o orquestrador e rode as fases em subagents paralelos — recon, injeção e auth em trilhas separadas que compartilham descobertas. É o equivalente ao "graph of agents" do Strix, no seu plano.

## Instalação desta skill
- Global (todos os projetos): `~/.claude/skills/strix-review/` (Windows: `C:\Users\User\.claude\skills\strix-review\`).
- Por projeto: `.claude/skills/strix-review/` no repositório.
- Pasta de agentes (portátil): mantenha em `AGENTES\strix-review\` e copie para `.claude/skills/` quando for usar, ou aponte seu orquestrador para ela.
