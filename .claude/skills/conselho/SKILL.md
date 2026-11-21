---
name: conselho
description: "Conselho de Diretores autônomo (creative-director, technical-director, producer). Use SEMPRE que uma feature, épico, mecânica, mudança de stack ou pedido de jogo começar, e no review de todo PR de jogo. Recebe o pedido, abre a ata, roda 2 rodadas de debate em paralelo, decide (ADR) e DESPACHA o trabalho na mesma execução, sem pausar nem perguntar. Gatilhos: 'nova feature', 'quero que o jogo tenha', 'implementa', 'mecânica', 'épico', 'conselho', 'review do PR', '/conselho'."
argument-hint: "[pedido da feature | pr <número>] [--jogo <repo>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Agent, Bash(git *), Bash(gh pr *), Bash(gh run *), Bash(python *), TaskCreate, TaskGet, TaskList, TaskUpdate
model: sonnet
---
# /conselho — Conselho de Diretores (autônomo)

O usuário traz a ideia; o Conselho decide como estúdio profissional e já põe o time para trabalhar.
**Não use AskUserQuestion.** A única saída para o usuário é a escalada de VISÃO DO JOGO (passo 5).

Sem argumento: leia o último pedido do usuário na conversa e use-o como entrada. Se não houver pedido de jogo, diga em 1 linha o uso (`/conselho <pedido>` ou `/conselho pr <n>`) e pare.

## Limites de execução (hardware do notebook, inegociáveis)
- No máximo **3 subagentes em paralelo** (as 3 posições dos diretores ocupam a leva inteira).
- No máximo **1 Unity Editor/build por vez** — raias Unity são serializadas.
- Diretores **não codam**: posicionam, decidem, revisam e vetam.

## Modo A — Antes de codar (pedido de feature/épico)

### 1. Abrir a ata (producer, inline, sem subagente)
- Caminhos de ata/ADR/evidência e docs do time: siga "Resolução de caminhos" do CLAUDE-TIME.md (convenção do jogo e
  `project.yaml` `time_jogos.paths.*` vencem os padrões abaixo; docs ausentes → `.claude/time-jogos/` ou repo do time).
- Slug curto do pedido; arquivo `docs/conselho/AAAA-MM-DD-<slug>.md` a partir de `templates/ATA_CONSELHO.md`
  (no repositório de jogo instalado via `install-time`, o template fica em `.claude/time-jogos/templates/`).
- Preencher §0 (pedido literal) e §1 (objetivo, restrições do jogo — leia `CLAUDE.md`, `docs/PRATICAS_APROVADAS.md` e o GDD por seção — e critério de pronto testável).
- Restrições fixas sempre presentes: originalidade (nunca clonar), monetização só cosmética, custo zero ocioso, orçamento mobile quando o alvo for celular.

### 2. Rodada 1 — em paralelo (3 chamadas `Agent` na MESMA mensagem)
`subagent_type`: `creative-director`, `technical-director`, `producer`. Prompt de cada um:
```
Conselho — Rodada 1. Leia a ata em <caminho> (§0 e §1). Escreva SUA posição em no máximo 1 página:
Riscos (do seu domínio) · Abordagem recomendada · O que você VETARIA e por quê · Critério de aceite que você exige.
Não escreva em arquivos. Devolva só o texto da posição no PROTOCOLO DE REPORTE.
```
Cole as 3 posições na §2 da ata (você escreve o arquivo, não eles).

**Plano B (agente não disponível na sessão** — ex.: sessão aberta fora do repo do jogo, ou chamada via API):
dispare `Agent` com `subagent_type: general-purpose` e o prompt `"Leia .claude/agents/<diretor>.md e assuma esse papel integralmente. <prompt da rodada>"`.
Sem ferramenta `Agent` alguma: escreva cada posição você mesmo seguindo o `.md` do diretor e registre no topo da ata
`Rodadas simuladas inline (sem subagentes)` — nunca omita isso.

### 3. Rodada 2 — réplica, em paralelo (última rodada)
Cada diretor recebe as posições dos outros dois e responde: concorda / discorda (com motivo) / ajuste mínimo para aceitar.
Cole na §3. **Nunca abra uma 3ª rodada.**

### 4. Decidir (producer consolida, inline)
- Consenso → registra.
- Divergência → **desempate por domínio**: design/identidade/tom → `creative-director`; técnica/stack/arquitetura/performance → `technical-director`; escopo/prazo/sequência → `producer`. O dono do domínio decide e a ata registra o voto vencido.
- Gere o ADR em `docs/architecture/adr-NNNN-<slug>.md` (template `templates/ADR.md`): abordagem, alternativas descartadas, critério de aceite testável, orçamentos.
  **Status do ADR:** nasce `Accepted` quando o Conselho decidiu (o technical-director assinou na Rodada 2); fica `Proposed` só se escalou ao usuário.
- Stack/engine vêm do jogo (`project.yaml` → `engine`, ou o CLAUDE.md do jogo): Unity, web (Vite/TS) ou outra. Os diretores avaliam pela engine do jogo, não presumem Unity.
- Se a decisão tocar rede, backend, auth, IAP ou dados pessoais → marque **Portão de Segurança obrigatório** no ADR.

### 5. Escalar ao usuário — SÓ se o empate for de VISÃO DO JOGO
(fantasia central, gênero, público, tom, monetização nova, mudança de pilar). Escreva no máximo 5 linhas:
`Opção A … / Opção B … / (Opção C …) · Recomendação do Conselho: X porque …`.
Enquanto espera, **despache tudo o que não depende dessa escolha**. Qualquer outra decisão o Conselho toma sozinho.

### 6. Despachar IMEDIATAMENTE (mesma execução, sem pausa)
- `producer` preenche a §5 da ata: raias com dono, entrega, arquivos exclusivos e dependência.
- Crie a branch `feat/<slug>` se ainda não existir (`git switch -c`).
- Crie as tarefas (TaskCreate) e dispare os chefes de área/especialistas com `Agent`, até 3 em paralelo, respeitando dependências (contratos → dados → código → arte/áudio → testes → docs).
- Cada raia recebe o CONTRATO DE RAIA abaixo e devolve o PROTOCOLO DE REPORTE.
- Ao terminar as raias: rode os testes do projeto, salve evidência (`templates/EVIDENCIA_QA.md`) e abra o PR (`gh pr create --draft`, corpo com links da ata e do ADR). Em seguida, execute o **Modo B** no PR.

#### Contrato de raia
```
Objetivo: · Ata/ADR: <caminhos> · Escopo da raia: · Arquivos sob sua responsabilidade: · Não tocar: ·
Critério de pronto: · Orçamentos: · Limites: máx. 1 Unity por vez; sem segredos; sem clonar outros jogos ·
Saída: PROTOCOLO DE REPORTE (RESUMO · ENTREGAS com paths · RISCOS · RECOMENDAÇÃO prosseguir/revisar/bloquear)
```

## Modo B — Depois de codar (`/conselho pr <n>`)
1. Colete: diff (`gh pr diff <n>`), checks (`gh pr checks <n>`), evidência em `production/qa/evidence/`, ata e ADR.
2. Antes dos diretores: `lead-programmer` faz o LP-CODE-REVIEW do diff (o technical-director exige esse parecer). Depois, em paralelo:
   - `technical-director`: código, arquitetura, testes, performance → APROVA / VETA (com lista do que corrigir).
   - `creative-director`: só evidência visual (screenshot/vídeo da build) contra brief e ADR → APROVA / VETA.
   - `producer`: escopo, critério de pronto, checklist da Definição de Pronto → APROVA / VETA.
   - Vocabulário de veredito: `APROVA` · `VETA` · `NÃO AVALIADO (<motivo: sem evidência visual | CI não rodou | ata ausente>)`.
     `NÃO AVALIADO` bloqueia o merge como um veto, mas a correção é produzir a evidência/rodar o CI, não reescrever código.
     A saída sempre lista, por diretor, qual veredito saiu — um diretor que não rodou aparece como `NÃO AVALIADO (não executado)`.
3. Algum VETA ou NÃO AVALIADO → despache a correção para o dono da raia, rode testes, atualize evidência e repita. **Máximo 3 ciclos**; no 4º, registre bloqueio na ata e reporte ao usuário com o motivo exato.
4. Todos APROVAM → `python .claude/scripts/tj_merge_gate.py <n>`: espera o CI, recusa se algum check não estiver verde
   ou se não houver check nenhum (NÃO AVALIADO), e só então faz `gh pr ready` + `gh pr merge --squash --delete-branch`.
   **Nunca use `gh pr merge --auto`:** em repositório privado no plano gratuito não existe proteção de branch e o `--auto`
   mergeia na hora, até com CI vermelho (defeito D11 da validação F6). Registre o veredito na §7 da ata.

## Definição de pronto (checar antes de aprovar)
- Teste rodado e passando (local e CI) · evidência visual salva para algo visível · ata + ADR existentes
- Sem segredo commitado · Portão de Segurança (`/security-review` → `strix-review`) quando tocar rede/backend/auth/pagamento/dados
- `ip-originality-reviewer` aprovou nomes, arte, UI e textos novos · PR mergeado com CI verde

## Saída final ao usuário (máx. 8 linhas)
Decisão em 1 linha · raias despachadas · PR e status do CI · evidência · pendências que dependem do usuário (se houver).
