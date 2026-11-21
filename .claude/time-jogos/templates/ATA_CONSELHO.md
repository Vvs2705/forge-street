# Ata do Conselho — {{FEATURE}} ({{AAAA-MM-DD}})

> Arquivo: `docs/conselho/{{AAAA-MM-DD}}-{{feature-slug}}.md` · Aberta por: `producer`
> Estado: ABERTA | RODADA-1 | RODADA-2 | DECIDIDA | ESCALADA-AO-USUARIO | DESPACHADA

## 0. Entrada (copiada do pedido, sem reescrever)
- **Pedido original:** "{{texto do usuário ou issue}}"
- **Jogo / repositório:** {{ARKANA | dungeon-courier | ...}} · **Branch alvo:** `feat/{{slug}}`

## 1. Enquadramento (producer, máx. 10 linhas)
- **Objetivo:** {{uma frase verificável}}
- **Restrições:** plataforma, orçamento de FPS/memória, prazo, custo zero ocioso, monetização só cosmética, originalidade
- **Critério de pronto (testável):** {{ex.: teste EditMode X passa; build Android abre; 60 FPS no perfil médio; screenshot em production/qa/evidence/}}
- **Fora de escopo:** {{...}}

## 2. Rodada 1 — posições independentes (em paralelo, máx. 1 página cada)
### creative-director
- Riscos · Abordagem · Veto (o que reprovaria)
### technical-director
- Riscos · Abordagem · Veto
### producer
- Riscos · Abordagem · Veto

## 3. Rodada 2 — réplicas (cada diretor responde aos outros dois; última rodada)
### creative-director → TD / PR
### technical-director → CD / PR
### producer → CD / TD

## 4. Decisão
- **Consenso?** sim / não
- **Desempate por domínio aplicado:** design/identidade → creative · técnica/stack/performance → technical · escopo/prazo → producer
- **Escalar ao usuário?** só se o empate for de VISÃO DO JOGO → 2–3 opções em ≤ 5 linhas (seção 6)
- **ADR:** `docs/architecture/adr-{{NNNN}}-{{slug}}.md` (abordagem, alternativas descartadas, critério de aceite)

## 5. Despacho imediato (preenchido pelo producer no mesmo turno da decisão)
| # | Raia | Agente dono | Entrega | Arquivos (raia exclusiva) | Depende de |
|---|---|---|---|---|---|
| 1 | | | | | — |

Regra de despacho: no máximo 3 subagentes em paralelo · 1 Unity Editor/build por vez · cada raia devolve o PROTOCOLO DE REPORTE.

## 6. Escalada ao usuário (só visão do jogo)
> Opção A … · Opção B … · (Opção C …) · Recomendação do Conselho: …

## 7. Revisão pós-código (PR #{{n}})
| Diretor | Evidência avaliada | Veredito | Ciclo (1–3) |
|---|---|---|---|
| technical-director | código, testes, perf | APROVA / VETA | |
| creative-director | screenshot/vídeo vs brief e ADR | APROVA / VETA | |
| producer | escopo, critério de pronto, checklist §9 | APROVA / VETA | |
