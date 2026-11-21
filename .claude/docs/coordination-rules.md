# Agent Coordination Rules

O time é **autônomo**: decide, registra e segue. Ninguém pergunta "posso escrever este arquivo?" ou "qual opção
prefere?". O usuário só é chamado para: **empate de VISÃO do jogo** no Conselho, **dinheiro novo**, **credencial/conta
faltando**, **publicação pública** (loja, repo público, post). Tudo o mais é decidido e registrado.

1. **Vertical Delegation**: Conselho (`creative-director`, `technical-director`, `producer`) decide; o `producer`
   abre as raias e despacha os chefes de área; cada chefe despacha os especialistas do seu domínio
   (mapa em `agent-roster.md`). Tarefa pequena de um domínio só pode ir direto ao especialista, com o lead
   do domínio como revisor. Não pule o Conselho em decisão de feature/épico/stack.
2. **Horizontal Consultation**: Agentes do mesmo nível podem se consultar, mas não tomam decisão vinculante fora
   do próprio domínio.
3. **Conflict Resolution**: Conflito sobe para o pai comum (especialista → chefe de área → diretor do domínio →
   Conselho). No Conselho, **desempate por domínio**: design/identidade/tom → `creative-director`;
   técnica/stack/arquitetura/performance → `technical-director`; escopo/prazo/sequência → `producer`. A ata registra
   o voto vencido. Máximo **2 rodadas** de debate; nunca uma 3ª.
4. **Change Propagation**: Mudança que afeta vários domínios é propagada pelo `producer` (ata + tarefas).
5. **No Unilateral Cross-Domain Changes**: Cada raia tem **arquivos exclusivos** declarados na ata §5; um dono por
   arquivo. Mexer fora da raia exige nova raia ou delegação explícita do producer.
6. **Read Design Docs by Section**: O brief nomeia as seções necessárias pelo título. Ache o título em GDD, ADR ou
   art bible com Grep (`^#`, modo content) e leia o intervalo com Read (offset/limit); leia inteiro só quando a
   tarefa exigir, e diga por quê. `validate-read.sh` recusa leitura inteira de doc acima de 40 KB.
7. **Nothing Goes Upstream**: Nunca abra/comente issue ou PR em repositório de engine, plugin ou fornecedor, mesmo
   a pedido. Escreva um rascunho (`/bug-report` só gera rascunho) e o usuário envia.
8. **Record Every Decision**: Toda decisão fica escrita onde o próximo agente vai procurar: feature/épico/stack →
   ata `docs/conselho/AAAA-MM-DD-<slug>.md` + ADR `docs/architecture/adr-NNNN-<slug>.md` (o `technical-director`
   move para `Accepted` após a decisão do Conselho); decisão de domínio → documento do dono (GDD, art bible, audio
   bible, lore bible) com data; suposição tomada para destravar → no PR. Sem registro, a decisão não existe.
9. **Evidence Over Claims**: Teste só conta se ficou **vermelho com o defeito reintroduzido**; veredito de teste
   Unity vem do XML do `-testResults`, nunca do exit code; a linha final do portão separa passaram / falharam /
   **ignorados com motivo**; algo visível precisa de screenshot do build em `production/qa/evidence/` (template
   `EVIDENCIA_QA.md`); onda visual termina com **FPS antes/depois** no aparelho. Dono da evidência: `qa-lead`.
10. **Verdict Vocabulary**: Review de PR usa `APROVA` · `VETA` · `NÃO AVALIADO (<motivo>)`. `NÃO AVALIADO` bloqueia como
    veto, mas a correção é produzir a evidência/rodar o CI. Gates do CCGS (`.claude/docs/director-gates/`) usam só as
    palavras da linha **Verdicts** do gate, token na 1ª linha.
11. **Review Chain**: especialista → `lead-programmer` (`LP-CODE-REVIEW`) → `qa-lead` (evidência) → `/conselho pr <n>`
    (TD: código/testes/perf · CD: evidência visual · producer: escopo/DoD), máx. 3 ciclos de correção → `release-manager`
    roda `python .claude/scripts/tj_merge_gate.py <n>` (CI verde obrigatório; nunca `--auto`). Merge bloqueado por permissão não é contornado:
    vira `BLOCKED_PERMISSION` na ata.
12. **Non-Negotiable Limits**: nunca trocar de engine/plataforma por feature distante (Unity 6 é definitiva);
    originalidade (`ip-originality-reviewer`) e monetização só cosmética (`monetization-compliance`) têm poder de
    bloqueio próprio (recurso ao `creative-director`); rede/backend/auth/IAP/dados pessoais → Portão de Segurança
    (`/security-review` → `strix-review`); custo zero ocioso; sem chave de API paga; proibido force push,
    `git reset --hard` em main, `gh repo delete`, repo público, commitar segredo, editar `_upstream`.

## Model Tier Assignment

Leia `.claude/docs/model-tiers.md` sob demanda. Tabela oficial do time (validada por `scripts/lint_agents.py`):
`technical-director` opus/xhigh · `creative-director` opus/high · `producer` e os 8 leads sonnet/high.

**Key point:** whether a skill's `model:` is used
depends on how the skill starts. Typed as `/skill-name`: yes, except a `haiku`
pin in auto mode. Started by Claude through the Skill tool: no. Never tell a
user a `haiku` skill saves money; an `opus` skill typed in a Sonnet session
costs more. The agent-side `model:` is a different mechanism, and is applied.

## Subagents vs Agent Teams

This project uses two distinct multi-agent patterns:

### Subagents (current, always active)
Spawned via the `Agent` tool within a single Claude Code session. Used by `/conselho`, all `team-*` skills and
orchestration skills. Subagents share the session's permission context, run sequentially or in parallel within the
session, and return results to the parent. Um subagente não abre outro: quando o `producer` roda como subagente,
devolve a §5 da ata e os contratos de raia prontos, e a sessão que o chamou despacha sem reescrever.

**When to spawn in parallel**: If two subagents' inputs are independent (neither needs the other's output to begin),
spawn both `Agent` calls in the same message — respecting **máx. 3 em paralelo** e **no máximo 1 raia que abre
Unity** por leva. Example: Rodada 1 do Conselho (3 diretores) ocupa a leva inteira.

### Agent Teams (experimental — opt-in)
Multiple independent Claude Code *sessions* coordinated via a shared task list. Opt-in only — read
`.claude/docs/agent-teams.md` on demand before proposing one.

## Parallel Task Protocol

When an orchestration skill spawns multiple independent agents:

1. Contract first: se duas raias tocam a mesma fronteira, a raia de contrato (interface/schema) roda antes
2. Issue all independent `Agent` calls before waiting for any result (≤ 3, ≤ 1 Unity)
3. Each lane gets the CONTRATO DE RAIA (objetivo, ata/ADR, arquivos exclusivos, não tocar, critério de pronto,
   orçamentos, limites) and returns the PROTOCOLO DE REPORTE (RESUMO · ENTREGAS · EVIDÊNCIA · RISCOS · RECOMENDAÇÃO)
4. Collect all results before proceeding to dependent phases; o integrador confere escopo, paths reais e contradição
   entre raias antes de aceitar — saída reprovada volta ao dono
5. If any agent is BLOCKED, surface it immediately — do not silently skip; use as etiquetas `BLOCKED_HARDWARE`,
   `BLOCKED_CREDENTIAL`, `BLOCKED_LICENSE`, `BLOCKED_PERMISSION`
6. Always produce a partial report if some agents complete and others block
7. Worker só reporta com o artefato existindo — build longo nunca fica em background de worker
