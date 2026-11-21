# Agent Roster

Agentes disponíveis, cada um com definição em `.claude/agents/`. O time é **autônomo**: o Conselho (Tier 1) decide e
registra (ata `docs/conselho/`, ADR `docs/architecture/`), o `producer` despacha as raias na mesma execução, e os
chefes de área (Tier 2) despacham os especialistas (Tier 3). O usuário só é chamado para empate de VISÃO do jogo,
dinheiro novo, credencial/conta faltando ou publicação pública. Regras de coordenação: `coordination-rules.md`.

O modelo de cada agente vem da linha `model:` do próprio arquivo (e `effort:`); a tabela oficial é validada por
`scripts/lint_agents.py` (`MODEL_TABLE`). `model-tiers.md` lista os agentes que fixam um modelo.

Limites de execução: **máx. 3 subagentes em paralelo · 1 Unity Editor/build por vez · 1 processo pesado por vez.**

## Cadeia de comando

```
Usuário (só: visão do jogo em empate · dinheiro novo · credencial/conta · publicação pública)
└── Conselho: creative-director · technical-director · producer  (/conselho — 2 rodadas, desempate por domínio)
    └── producer (despachante: ata §5, raias com arquivos exclusivos, ≤ 3 em paralelo, ≤ 1 Unity)
        ├── lead-programmer ──► code review (LP-CODE-REVIEW) antes do technical-director
        ├── game-designer · art-director · audio-director · narrative-director   (visão: creative-director)
        ├── qa-lead ──────────► dono da evidência (production/qa/evidence/)
        ├── release-manager ──► dono do merge/auto-merge, versão, CHANGELOG, tag
        └── localization-lead
            └── especialistas (Tier 3), despachados pelo lead do domínio
Fluxo de PR: especialista → lead-programmer (LP-CODE-REVIEW) → qa-lead (evidência) → /conselho pr (TD · CD · producer:
APROVA / VETA / NÃO AVALIADO, máx. 3 ciclos) → release-manager (`python .claude/scripts/tj_merge_gate.py <n>` — espera CI verde; nunca `--auto`).
```

## Tier 1 -- Leadership Agents (Conselho)
| Agent | Model / effort | Domain | When to Use |
|-------|----------------|--------|-------------|
| `creative-director` | opus / high (sem Bash) | Visão, pilares, tom, identidade | Decisão que mexe na identidade, conflito de pilares, review visual de PR, gates `CD-*`; desempata design/tom |
| `technical-director` | opus / xhigh | Arquitetura, stack, performance, risco técnico | ADR, dependência/pacote, orçamento de frame/memória, review técnico de PR, gates `TD-*`; desempata técnica |
| `producer` | sonnet / high | Escopo, prazo, riscos, **despacho** | Abre ata, consolida decisão, abre raias e despacha; review de escopo de PR, gates `PR-*`; desempata escopo/prazo |

## Tier 2 -- Department Lead Agents
| Agent | Reporta a | Domain | Despacha (Tier 3) |
|-------|-----------|--------|-------------------|
| `lead-programmer` | technical-director | Contratos de código, quebra técnica, **code review** | `gameplay-programmer`, `engine-programmer`, `ai-programmer`, `ui-programmer`, `network-programmer`, `tools-programmer`, `unity-specialist` (+ `unity-*`), `prototyper`; com TD: `multiplayer-backend-engineer`, `mobile-performance-specialist` |
| `game-designer` | creative-director | Sistemas, economia, level, balanceamento | `systems-designer`, `level-designer`, `economy-designer`, `ux-designer`, `prototyper`, `live-ops-designer`; consulta `monetization-compliance`, `analytics-engineer` |
| `art-director` | creative-director | Estilo, art bible, specs, pipeline de assets IA | `technical-artist`, `ai-asset-pipeline-engineer`, `ux-designer` (visual de UI), `unity-shader-specialist`; consulta `ip-originality-reviewer` |
| `audio-director` | creative-director | Identidade sonora, mix, loudness, licença de áudio | `sound-designer`; implementação via `lead-programmer` |
| `narrative-director` | creative-director | História, lore, tom do texto, consistência | `writer`, `world-builder`; dá a fonte ao `localization-lead` |
| `qa-lead` | producer (fluxo) · technical-director (barra) | Estratégia de teste, **evidência**, triagem, sign-off | `qa-tester`, `performance-analyst`, `mobile-performance-specialist` (medição), `accessibility-specialist`, `build-ci-engineer` (portão no CI) |
| `release-manager` | producer | **Merge**, versão, CHANGELOG, RC, conformidade de loja, rollout | `build-ci-engineer`, `devops-engineer`, `community-manager` (rascunho), `monetization-compliance`, `ip-originality-reviewer` |
| `localization-lead` | narrative-director (texto) · producer (cronograma) | i18n, tabelas de strings, pseudo-loc, LQA | `ui-programmer` (via lead-programmer), `writer`, `qa-tester`, `ux-designer`, `tools-programmer` |

Todos os 8 leads: `sonnet / high`.

## Tier 3 -- Specialist Agents
| Agent | Domain | Despachado por | When to Use |
|-------|--------|----------------|-------------|
| `systems-designer` | Systems design | game-designer | Fórmulas, matrizes de interação, loops |
| `level-designer` | Level design | game-designer | Layout, ritmo, encontros, greybox |
| `economy-designer` | Economy/balance | game-designer | Sinks/faucets, drop, curvas de progressão |
| `gameplay-programmer` | Gameplay code | lead-programmer | Implementação de mecânica, controle, câmera, feel |
| `engine-programmer` | Engine systems | lead-programmer | Sistemas de baixo nível, memória, streaming, nativo |
| `ai-programmer` | AI systems | lead-programmer | Comportamento de NPC/bot, NavMesh, BT/FSM |
| `network-programmer` | Networking | lead-programmer (+ TD) | Netcode, replicação, compensação de lag |
| `multiplayer-backend-engineer` | Backend/multiplayer | technical-director / lead-programmer | Servidor autoritativo, matchmaking, backend com custo zero ocioso (Portão de Segurança) |
| `tools-programmer` | Dev tools | lead-programmer | Ferramentas de editor, validadores, automação |
| `ui-programmer` | UI implementation | lead-programmer | Telas, HUD, binding, safe area, localização no código |
| `mobile-performance-specialist` | Performance mobile | technical-director / qa-lead | FPS/frame time p95, térmico, memória, vitals no aparelho; FPS antes/depois de onda visual |
| `performance-analyst` | Performance | technical-director / qa-lead | Baseline, regressão entre builds, soak |
| `build-ci-engineer` | Build/CI (GameCI) | technical-director / release-manager | Workflows, build reproduzível, cache, secrets, AAB assinado |
| `devops-engineer` | Build/deploy | release-manager | Infra de entrega e automação geral |
| `security-engineer` | Security | technical-director | Portão de Segurança, anti-cheat, validação server-side, segredos |
| `technical-artist` | Tech art | art-director (+ TD em perf) | Shaders, import presets, validador de asset, lookdev, orçamento gráfico |
| `ai-asset-pipeline-engineer` | Assets por IA | art-director | Tripo/Meshy no site → Blender headless → Unity; ledger de créditos, proveniência |
| `sound-designer` | Sound design | audio-director | SFX, ambiência, síntese, import settings |
| `writer` | Dialogue/lore | narrative-director | Falas, descrições, textos de UI/tutorial |
| `world-builder` | World/lore design | narrative-director | Facções, geografia, história, consistência |
| `qa-tester` | Test execution | qa-lead | Casos de teste, execução, capturas, LQA |
| `analytics-engineer` | Telemetry | game-designer / producer | Plano de eventos, funis, privacidade |
| `ux-designer` | UX flows | game-designer / art-director | Fluxos, wireframes, input, clareza |
| `prototyper` | Rapid prototyping | game-designer / lead-programmer | Spike descartável, viabilidade |
| `accessibility-specialist` | Accessibility | qa-lead (consulta do CD) | Contraste, daltonismo, alvos de toque, legendas |
| `live-ops-designer` | Live operations | game-designer | Eventos e temporadas (só cosmético) |
| `community-manager` | Community | release-manager / producer | Rascunho de patch notes e comunicação (publicar = usuário) |
| `ip-originality-reviewer` | Originalidade / IP | art-director, narrative-director, release-manager (bloqueio próprio; recurso ao CD) | Nomes, arte, UI, texto, ficha de loja: nunca clonar; proveniência |
| `monetization-compliance` | Monetização ética / conformidade | game-designer, release-manager (bloqueio próprio; recurso ao CD) | Só cosmético, sem loot box (ECA Digital), Play Billing, Data safety |

## Engine-Specific Agents (use the set matching your engine)

O estúdio usa **Unity 6** (decisão definitiva). Os conjuntos Unreal e Godot permanecem por herança do CCGS e não
entram no roteamento.

### Engine Leads

| Agent | Engine | When to Use |
| ---- | ---- | ---- |
| `unity-specialist` | Unity | MonoBehaviour vs DOTS, Addressables, URP, otimização Unity (despachado pelo lead-programmer) |
| `unreal-specialist` | Unreal Engine 5 | Fora do roteamento (não propor troca de engine) |
| `godot-specialist` | Godot 4 | Fora do roteamento (não propor troca de engine) |

### Unreal Engine Sub-Specialists

| Agent | Subsystem | When to Use |
| ---- | ---- | ---- |
| `ue-gas-specialist` | Gameplay Ability System | Fora do roteamento |
| `ue-blueprint-specialist` | Blueprint Architecture | Fora do roteamento |
| `ue-replication-specialist` | Networking/Replication | Fora do roteamento |
| `ue-umg-specialist` | UMG/CommonUI | Fora do roteamento |

### Unity Sub-Specialists

| Agent | Subsystem | When to Use |
| ---- | ---- | ---- |
| `unity-dots-specialist` | DOTS/ECS | Entities, Jobs, Burst — só com ADR do TD |
| `unity-shader-specialist` | Shaders/VFX | Shader Graph, URP, pós-processamento (via technical-artist) |
| `unity-addressables-specialist` | Asset Management | Grupos Addressable, carregamento assíncrono, memória, Play Asset Delivery |
| `unity-ui-specialist` | UI Toolkit/UGUI | UI Toolkit, UXML/USS, Canvas, binding, input |

### Godot Sub-Specialists

| Agent | Subsystem | When to Use |
| ---- | ---- | ---- |
| `godot-gdscript-specialist` | GDScript | Fora do roteamento |
| `godot-csharp-specialist` | C# / .NET | Fora do roteamento |
| `godot-shader-specialist` | Shaders/Rendering | Fora do roteamento |
| `godot-gdextension-specialist` | GDExtension | Fora do roteamento |
