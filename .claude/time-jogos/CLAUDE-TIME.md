# Time de Jogos — regras (carregadas via bloco TIME-JOGOS do CLAUDE.md do jogo)

O usuário traz a ideia; o time decide como estúdio profissional e executa. Idioma: português brasileiro.

## Como o time trabalha
1. **Pedido de feature/mecânica/épico → `/conselho <pedido>`** antes de codar. O Conselho (creative-director, technical-director,
   producer) debate em 2 rodadas paralelas, decide (ADR), e o producer **despacha as raias na mesma execução**, sem pausar.
2. **PR pronto → `/conselho pr <n>`**: técnico (código/testes/perf), criativo (evidência visual), produtor (escopo/DoD).
   Veto ou NÃO AVALIADO → correção (máx. 3 ciclos) → aprovado → `python .claude/scripts/tj_merge_gate.py <n>` (espera CI verde; nunca `gh pr merge --auto`, que no plano gratuito mergeia com CI vermelho).
3. Cadeia de comando: Conselho → chefes de área → especialistas (ver `.claude/docs/agent-roster.md`). Especialista não decide fora do domínio;
   conflito sobe para o chefe comum, depois para o diretor do domínio.
4. Pergunte ao usuário **só** quando: faltar credencial/acesso; for gastar dinheiro novo; publicar para o público;
   empate de **visão do jogo** no Conselho (2–3 opções em ≤ 5 linhas). Todo o resto o time decide e registra.

## Limites inegociáveis
- Hardware: **máx. 3 subagentes em paralelo · 1 Unity Editor/build por vez.**
- Originalidade: estudar concorrentes, **nunca clonar** nome, arte, UI, código ou texto → `ip-originality-reviewer` aprova o que é novo.
- Monetização **só cosmética**, nunca pay-to-win, sem loot box para menores (ECA Digital) → `monetization-compliance`.
- Custo: nenhuma chave de API paga além do Claude Max; backend com **custo zero quando ocioso** (Fly.io descartado).
- Proibido: `gh repo delete`, force push, `git reset --hard` em main, tornar repo público, commitar segredo/.env/.ulf/keystore,
  editar ou apagar `_upstream`.
- Segurança: rede, backend, auth, IAP ou dados pessoais → Portão de Segurança (`/security-review` → `strix-review`).

## Definição de pronto (tudo)
Teste rodado e passando (local e CI) · algo visível tem screenshot/vídeo no caminho de evidência (template
`.claude/time-jogos/templates/EVIDENCIA_QA.md`) · ata + ADR quando houve decisão · sem segredo · originalidade aprovada · PR mergeado com CI verde.
- **Gameplay:** EditMode verde **não basta** — exige teste PlayMode **ou** screenshot **na cena real do jogo** mostrando a mecânica
  (lição F6: testes verdes, mecânica inexistente no jogo; só a foto pegou).
- **FPS** só vale de player com janela ou aparelho; Editor em `-batchmode` mede custo de CPU, nunca FPS → `NÃO AVALIADO`.

## Resolução de caminhos
1. **A convenção do jogo vence.** Leia o `CLAUDE.md` do jogo antes de criar arquivo: se ele proíbe pastas novas, use uma existente
   (ex.: ata/ADR/evidência em `design/`) e registre na ata o caminho escolhido.
2. **`project.yaml` (opcional):** chaves `time_jogos.paths.ata`, `time_jogos.paths.adr`, `time_jogos.paths.evidencia` sobrescrevem os padrões.
3. **Padrões** (só se 1 e 2 não decidirem): ata `docs/conselho/`, ADR `docs/architecture/`, evidência `production/qa/evidence/`.
4. **Docs de referência do time** (`docs/PRATICAS_APROVADAS.md`, `docs/AUTORIZACOES.md`, `docs/engine-reference/**`, director-gates,
   templates): no jogo, leia da cópia instalada `.claude/time-jogos/` (`docs/`, `templates/`) ou `.claude/docs/` (director-gates); se
   ausente, do repo do time `C:\Users\VINICIUS\Videos\MEUS PROJETOS\TIME DE JOGOS\time-jogos` (ou `gh repo clone Vvs2705/time-jogos`).
   Doc não achado → `NÃO AVALIADO (doc ausente)`, nunca conteúdo inventado.

## Unity — pré-voo
1 Editor por vez: `Get-Process Unity -EA SilentlyContinue | Where-Object { $_.Path -like '*\Editor\Unity.exe' }` vazio = livre
(o Hub roda um `unity.exe serve` que casa com `Get-Process Unity` e **não** conta).

## Ferramentas do time
- Orçamento de assets: `python .claude/scripts/tj_asset_budget.py <arquivo|pasta> --perfil mobile` (prefixos hero_/char_/npc_/wpn_/prop_/env_/vfx_/ui_).
- Tripo3D: conta A até acabar, depois B; todo gasto em `docs/LEDGER_TRIPO.md` (`python .claude/scripts/tj_tripo_ledger.py add ...`).
- Assets por IA → Blender batch → Unity: skill `/asset-ia`. Unity: skills oficiais `unity:*` (plugin Unity) e `unity-mcp` quando instalado.
- Templates: `.claude/time-jogos/templates/` (ATA_CONSELHO, ADR, BRIEF_FEATURE, EVIDENCIA_QA, GDD).

Todo subagente responde no PROTOCOLO DE REPORTE: RESUMO · ENTREGAS (paths) · EVIDÊNCIA · RISCOS · RECOMENDAÇÃO.

## Limpeza de lixo de build (sem pedir)
Use `powershell -ExecutionPolicy Bypass -File .claude\scripts\tj_limpar.ps1 -Raiz <repo> [-Simular]`: apaga só Library\Bee, Temp, obj, PerformanceTestRun* e APKs de Builds\testes, nunca algo rastreado pelo git nem fora da raiz. Qualquer outra exclusão continua proibida (deny `Remove-Item -Recurse`).
