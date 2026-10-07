# Fase 3 — melhorias visuais de distrito (Leva 8, 2026-10-07)

Continuação autorizada pelo pedido “CONTINUE DESENVOLVENDO”. Escopo desta leva: três compras de luxo com transformação visível, preservando produção, primeiros 10 minutos e saves antigos; diagnóstico A/B de logística separado, sem implantar variantes não aprovadas pela medição.

## Contrato de integração

Append ao enum `Upgrade`, sem alterar IDs 0–19: `WorkshopFacade` (20), `WorkshopFloor` (21), `JewelryDecor` (22). `Upgrades.ProductionCount = 20`; `Upgrades.Count = 23`.

| Upgrade | Nome | Preço vigente (aprovado 2026-10-07) | Preço inicial para medição (histórico) | Desbloqueio | Pad novo |
|---|---|---:|---:|---|---|
| WorkshopFacade | Fachada nobre | **11.000** | 6.000 | todos os upgrades 0–19 comprados | (6,5; 3,2) |
| WorkshopFloor | Piso de oficina | **14.000** | 10.000 | Fachada nobre e produção completa | (4,5; 3,2) |
| JewelryDecor | Joalheria real | **18.000** | 16.000 | Piso de oficina e produção completa | (14,2; 5,1) |

Os pads são anexados depois do índice 16; nenhuma estação nova. As compras só mudam a decoração: entrada dourada/bandeirolas; piso em ladrilhos; tapetes/adornos da joalheria. Reusar sprites e formas do projeto. O core publica flags em `Bought`, eventos `Ev.Bought` e save no formato existente. View lê as flags ao iniciar e após compra; nenhuma lógica de economia na view.

Bloquear pads de luxo até a produção completa, inclusive quando Vitrine de joias for comprada antes de outros upgrades. Uma compra paga uma vez, preserva rearmamento de pad e save/load. Luxo não entra em `CheapestLockedCost`/teto offline: manter a regra dos 20 upgrades produtivos e o teto de 18.000 após completá-los. Nenhuma alteração de receita, velocidade, clientes, capacidade, recompensa ou moeda.

## Evidência exigida

1. Bot de 90 min: tempos dos 20 produtivos e três luxos, saldo/receita, primeiros 10 min, comparação com baseline e retorno de offline no teto. Medir preços antes de declará-los vigentes (feito: vigentes 11.000 / 14.000 / 18.000).
2. Testes de desbloqueio, pagamento sem repetição, ausência de bônus produtivo, save antigo de 20 flags/17 pads, persistência do luxo e offline preservado; prova vermelha em cópia isolada.
3. Dotnet + Unity EditMode, build Windows, autoplay, fotos antes/depois por área e build Android dev 0.3.0. Preservar save real com `-testsession`.
4. Diagnóstico de fome em `ESTUDO_LOGISTICA.md`, fonte congelada e controle idêntico aos 41 min. Não misturar números do luxo com A/B de logística.

## Evidência medida (raia economia/QA, 2026-10-07)

**1. Bot de 90 min: FECHADA** ([BALANCE.md §11](BALANCE.md), teste `Bot_90Minutos_Luxo`).
- Os 20 produtivos saem no mesmo tick com e sem luxo; produção completa aos **40:41**. Os primeiros 10 min são idênticos (§3 do GDD verde), e sem luxo os 60 min batem §10.7 (46.377 / 21.212).
- **Vigente, 11.000 / 14.000 / 18.000** (aplicado em `Defs.cs` só nas 3 constantes; `CoreTests` de custo e orçamento ajustados):
  - Luxo: Fachada **50:45** (+10,1 min depois da produção completa), Piso **63:46** (+23,1), Joalheria real **79:55** (+39,2). Os três saem dentro dos 90 min, e os intervalos crescem (10,1 / 13,0 / 16,1 min).
  - Sem bônus: da produção completa aos 90 min, ouro/min **1.103,2 × 1.095,8 (+0,7%)** e vendas/min **−0,5%** contra a mesma partida sem luxo. O saldo final difere exatamente pelos 43.000 do preço.
  - Offline: um retorno no teto (18.000) logo depois da produção completa compra **só a Fachada**; o Piso vem 6,6 min depois e a Joalheria real 23,2 min depois. Um save de 60 min sem luxo (21.212) compra só a Fachada na hora; com claim no teto, compra Fachada + Piso.
  - Portão `Bot_90Minutos_Luxo`: Fachada ≤ 66:00, Piso ≤ 83:00, Joalheria real ≤ 90:00, com/sem luxo dentro de ±3%. Prova vermelha refeita numa cópia: luxo com +10% de preço (1,073), fornalha acelerada (1,056) e luxo no teto offline (36.000 × 22.000) ficam vermelhos; a cópia restaurada fica verde.
- **Histórico, 6.000 / 10.000 / 16.000:** Fachada 46:02 (+5,4 min, antes dos ~10 min pedidos), Piso 55:08, Joalheria real 69:41. Um retorno no teto comprava Fachada + Piso em 7 s. A troca foi aprovada pelo coordenador em 2026-10-07 (BALANCE §11.4).

**4. Diagnóstico e A/B de logística: FECHADA** ([ESTUDO_LOGISTICA.md](ESTUDO_LOGISTICA.md)), em fonte congelada com controle idêntico aos 41:00 e cópia temporária do core.
- A fome da joalheria (**56%** na janela 41–60) vem da **fonte**, não do transporte. As fornalhas ficam 22% do tempo sem minério e nunca acumulam lingote (saída média 0,55 / 0,77). O Ajudante 1 está saturado, e a oferta é de 57 lingotes/min para ~100 de capacidade aberta nas bancadas.
- **Nenhuma variante PROPOSTA vence:** esteira lateral e 2º Joalheiro só tiram lingote das outras linhas (outras vendas −5 a −14%), e a fornalha lateral fica 100% sem minério.
- Próximo A/B **aprovado para planejamento**: 2º ajudante de minério, isolado. Não está implementado e não tem preço. Nada de logística entrou em `client/Assets`.

**2 e 3: fora desta raia.** A suíte dotnet com os testes de luxo do núcleo está verde (**43/43**, já com os preços vigentes). A prova vermelha desses testes, Unity EditMode, builds, autoplay e fotos não foram verificados por esta raia. Com os novos preços, o Unity EditMode precisa rodar de novo.

## Pendência técnica

**IDs produtivos ≥ 23.** O core assume que os produtivos são os IDs contíguos 0..19 e os luxos 20..22: `Upgrades.ProductionCount`, `Pad.Current`, `Sim.Buy`, `ProductionComplete` e `CheapestLockedCost` (teto offline). Um upgrade produtivo futuro, como o 2º ajudante de minério se vencer o A/B, teria de ser anexado como ID 23 por causa do save, e então seria tratado como luxo: ficaria bloqueado até a produção completa, fora do teto offline e fora de `ProductionComplete`. Antes de anexar qualquer produtivo, o dono do core precisa de uma marca produtivo/luxo por upgrade (tabela ou flag em `UpgradeDef`), com teste de save antigo de 23 flags.

## Estado

- Comportamento **IMPLEMENTADO e TESTADO (dotnet 43/43)**.
- Preços do luxo **VIGENTES em 11.000 / 14.000 / 18.000**, medidos e com portão de regressão.
- Logística **DIAGNOSTICADA, sem vencedor**; próximo A/B (2º ajudante de minério) em planejamento.
- Portão Unity/arte com a outra raia.
- Interesse do jogador por decoração exige playtest; compras pelo bot não comprovam retenção.
