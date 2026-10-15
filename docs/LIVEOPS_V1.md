# LiveOps v1 — calendário, Masterwork Pass e missões

2026-10-12 · Raia LIVEOPS/DESIGN, Leva 6 · Tickets **D-PROD-01** (calendário), **D-PROD-02** (Pass S1/S2), **D-PROD-03** (missões) · Estado: **PROPOSTO**. Nada daqui está implementado nem medido.

Regras de design: [`GDD.md`](GDD.md) §15–§17. Decisões: [`PROJETO.md`](PROJETO.md) V4, V7, V9, V11, V16. Direção: DM §28–§35, §86, §121–§124, §133, §135 ([`DOCUMENTO_MESTRE_PRODUCAO_V1.md`](DOCUMENTO_MESTRE_PRODUCAO_V1.md)). Números da Wave B: [`ECONOMIA_WAVE_B.md`](ECONOMIA_WAVE_B.md). Ritmos medidos: [`BALANCE.md`](BALANCE.md) §20–§22. Referências de mercado: só as do [`BENCHMARK_MERCADO.md`](BENCHMARK_MERCADO.md) §8. Implementação: D-CORE-01..04 em [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md).

Todo número aqui é **ponto de partida** e só vira APROVADO depois que o econsim (`client/tools/econsim`) passar nos portões do §6 e o resultado for para o `BALANCE.md`. Se um portão falhar, o ajuste é no knob indicado, não na regra.

## 0. Regras que valem para tudo

1. **Sem sorteio** (V7). Diárias e semanais saem de um rodízio por dia/semana; eventos têm missões fixas; recompensas são listas fixas, mostradas antes.
2. **Nada aleatório pago** (V4; ECA Digital, Lei 15.211/2025). O tipo de recompensa nem tem "baú sorteado", "chance" ou "roleta" (§5.1).
3. **Só ouro como moeda até a Wave E** (V9). Recompensa de missão, conquista, login e trilha free do Pass: **ouro, pontos de evento, XP do Pass, cosmético fixo ou Renome**. Eventos usam **pontos de evento**, trilha não gastável (V16).
4. **Uma vez só** (D-CORE-01). Toda recompensa tem ID de transação (§5.8) e entra no ouro **fora do `GoldEarned`**, como as encomendas e os baús (`BALANCE.md` §20): não infla a taxa online, o cofre nem o pacote do VIP.
5. **Nada exige anúncio.** Missão que se faz com anúncio também se faz sem (o VIP conta natural e chamado). O "bônus de evento" por rewarded (GDD §17.1) é extra e fica fora de toda conta de fechamento.
6. **Nada pune.** Sem missão negativa (cliente perdido, estação travada), sem reset de login (DM §135), Rush nunca falha (GDD §10).
7. **Primeiros 10 min intocados** (GDD §3.3, regra da Wave B). Diárias, semanais, login, conquistas, eventos e Pass abrem **depois das Ferramentas** (~10:46 no bot F2P, `BALANCE.md` §20). Conquista feita antes disso é creditada quando a tela abre.
8. **Ouro em segundos de renda.** "ouro N s" = N × taxa online (`RateEma`) no crédito, múltiplo de 5, mínimo 25 (o piso das encomendas, `Balance.OrderRewardMin`).
9. **Só jogo ativo conta.** Venda offline e renda de fundo de distrito (manager) não emitem `Ev` e não contam para missão nem pontos. O offline nunca dá pontos de evento nem XP (GDD §5.3).
10. **Tudo em dados** (V6): `EventDef`, `MissionDef`, `PassDef`, `AchievementDef`, `LoginDef` aprovados pelo `ContentValidator` (§5.7). Evento novo entra só com dados (aceite da Wave D, DM §146).

## 1. Jogador de referência e orçamento

**F2P de referência** (o perfil `F2P` do econsim, `BALANCE.md` §22): 2 sessões de 10 min por dia = **20 min de jogo ativo/dia**, todo dia, sem anúncio (GDD §2: sessão de 3–12 min, meta de 2+ sessões entre os retidos).

| Ritmo usado nas contas | Valor | Fonte |
|---|---|---|
| Encomendas entregues | 21 em 60 min (2,8 min cada) → ~7 por dia de 20 min; portão da Wave B ≥ 25 em 60 min → ~8 | `BALANCE.md` §20; `ECONOMIA_WAVE_B.md` §10 |
| Vendas | 2.032 em 60 min (~34/min) | `BALANCE.md` §21 (ouro direto) |
| VIP | 1 a cada 4–6 min (`Balance.VipMin/VipMax`) | `Defs.cs`; `BALANCE.md` §18 |
| Ferramentas · Joalheria | 10:46 · 22:53 (F2P) | `BALANCE.md` §20, §22 |
| Renome 1 · 2 · 4 | 12–25 min · 40–75 min · 3–6 h acumuladas (portões propostos) | `ECONOMIA_WAVE_B.md` §10 |

**Orçamento de ouro de LiveOps** (diárias + semanais + login + Pass + eventos): **≤ 180 s de renda por dia**, ou seja, ≤ 15% da receita de venda de um dia de 20 min (1.200 s). Somado às encomendas (5,0% da receita hoje, portão 3–10%), o ouro que não é venda fica perto de 20%.

| Fonte | Por dia (s de renda) |
|---|---|
| Diárias: 3 × 20 s | 60 |
| Semanais: 4 × 60 s / 7 | 34 |
| Login: (20 + 30 + 40 s) / 7, mais 60 s no dia 7 depois do 4º ciclo | 13–21 |
| Pass free: 1.170 s / 35 dias | 33 |
| Eventos: 135 s por evento / 7 | 19 |
| **Total** | **159–167** |

**Renome de LiveOps** por semana: semanais 20 + login 7 + Pass ~9 + evento 8 ≈ **44** (o Charter do Old Forge pede 400, `ECONOMIA_WAVE_B.md` §5.2). Portão no §6.

## 2. Calendário de 8 semanas + 2 reservas (D-PROD-01)

### 2.1 Linha do tempo

T0 = início da Season 1, data a decidir (§7). Dias contados a partir de T0 em UTC; virada de evento e de temporada às 13:00 UTC (10:00 de Brasília), recomendação a confirmar. Cada evento dura **6 dias** (o Burger Please! faz eventos de 6 dias, `BENCHMARK_MERCADO.md` §8) e o 7º dia fica sem evento (respiro; resumo do que fechou). Nunca há dois eventos ao mesmo tempo.

| Sem. | Dias (T0+) | Temporada / Pass | Evento | Template (DM §29) | Requisito além das Ferramentas | F2P cumpre em |
|---|---|---|---|---|---|---|
| 1 | 0–5 | **S1 Royal Commission** (dias 0–34) | Royal Week | Royal Week | Renome 1 | 12–25 min |
| 2 | 7–12 | S1 | Forge Frenzy | Forge Frenzy | 10 encomendas entregues | ~30 min |
| 3 | 14–19 | S1 | Armorer's Challenge | Armorer's Challenge | Renome 1 | 12–25 min |
| 4 | 21–26 | S1 | Masterwork Hunt "King's Sunblade" | Masterwork Hunt | Renome 1 + `bp_mw_primeira_lamina` (~100 espadas, preview do Hall no D0, GDD §3.5) | D0 |
| 5 | 28–33 | S1 | **Forge Frenzy-B** (reserva) | Forge Frenzy | Joalheria | 22:53 |
| 6 | 35–40 | **S2 Ember Festival** (dias 35–69) | Blacksmith Festival | Blacksmith Festival | 10 encomendas entregues | ~30 min |
| 7 | 42–47 | S2 | Rare Ore Rush | Rare Ore Rush | Renome 1 | 12–25 min |
| 8 | 49–54 | S2 | Merchant Caravan | Merchant Caravan | Renome 1 + Bulk aberto (2 linhas, 5 entregues) | ≤ 25 min |
| 9 | 56–61 | S2 | Masterwork Hunt "Ember Crown" | Masterwork Hunt | Joalheria + Renome 1 | 22:53–25 min |
| 10 | 63–68 | S2 | **Royal Week-B** (reserva) | Royal Week | Renome 1 | 12–25 min |

- **Todo requisito sai do D0/D1:** o pior caso é ~30 min de jogo, e o F2P de referência joga 40 min até o fim do D1. Renome 2 (40–75 min) **não** é requisito de evento: as missões que pedem encomenda Royal contam com o jogador chegando lá durante os 6 dias do evento (2 h de jogo).
- **Encomenda de tipo especial é rara:** o rodízio do Old Forge tem 1 Bulk, 1 Quality, 1 Mixed, 1 Rush e 1 Royal a cada 10 (`ECONOMIA_WAVE_B.md` §3.2). Com ~8 encomendas por dia, um evento pede no máximo **2 de cada tipo especial** (~20–30 encomendas, 3–4 dias); diária nunca pede tipo especial (só a encomenda Event, que tem vaga própria).
- **Temporada de 35 dias** (DM §33 pede 28–35): 4 semanas de evento + a semana 5 com a reserva. O F2P fecha a trilha free por volta do dia 28 (§3.3); a semana 5 é a folga de quem faltou dias.
- **Reservas** (DM §121 "2 eventos backup"): Forge Frenzy-B e Royal Week-B rodam **só com o distrito 1**, missões só sobre eventos do diário que já existem hoje e arte reaproveitada do evento-mãe com paleta trocada. Uso: (a) na semana 5 da temporada, se nada atrasou; (b) no lugar de qualquer evento das semanas 1–4 ou 6–9 cujo conteúdo não ficou pronto. Usada antes, a semana 5 fica sem evento (o Pass não depende dela).
- **Arcane Week** (8º template) fica fora de S1/S2: entra com o distrito 5 (Arcane Foundry, D60, GDD §6).
- S3 em diante: o mesmo formato (5 semanas, 4 eventos + 1 reserva), só com dados novos (DM §124).

### 2.2 Template comum de evento

Todo evento reaproveita este molde (DM §124: trocar missões, arte, modificadores, recompensa e moeda, nunca o sistema).

**Pontos de evento** (V16): trilha não gastável, por evento, que zera no evento seguinte (fica arquivada). Fontes:

| Fonte | Pontos | Nota |
|---|---|---|
| Missões do evento (7–8, fixas) | soma **700** | contam do `event_enter` em diante |
| Encomenda Event (vaga própria, `ECONOMIA_WAVE_B.md` §3.1–3.2) | **15** por entrega (10 no Merchant Caravan) | alvo 150 s de renda, produto do rodízio, qualidade Normal; F2P ~6–8 por dia de 20 min (Orders 2.0, `BALANCE.md` §28.6; era 20 e fechava a trilha numa sessão) |
| Bônus de evento (rewarded, GDD §17.1) | +30, 1× por dia, valor mostrado antes | **fora** da conta de fechamento |

**Trilha** (igual em todos; o crédito é automático ao cruzar o limiar, com aviso na fila de avisos, A-UI-19):

| Limiar | Recompensa | ID de transação |
|---|---|---|
| 50 | ouro 30 s | `e:{evento}:50` |
| 100 | XP do Pass 50 | `e:{evento}:100` |
| 175 | Renome +3 | `e:{evento}:175` |
| 250 | ouro 45 s | `e:{evento}:250` |
| 350 | XP do Pass 100 | `e:{evento}:350` |
| 450 | cosmético menor do tema (nos Hunts: o projeto da Masterwork, §2.4) | `e:{evento}:450` |
| 575 | ouro 60 s | `e:{evento}:575` |
| 700 | XP do Pass 150 | `e:{evento}:700` |
| 850 | Renome +5 | `e:{evento}:850` |
| 1.000 | XP do Pass 200 + cosmético principal do evento | `e:{evento}:1000` |

Por evento: 135 s de ouro, 8 de Renome, 500 de XP do Pass, 2 cosméticos. **Fechamento:** 700 das missões (todo evento tem a "jogue em 3 dias", `play_day`) + ~20 encomendas Event; o F2P de referência fecha no dia 4–5 de 6, sem anúncio, e 60 min seguidos não fecham (portão no §6; `LiveOpsTests.TrilhaDeEvento_*`).

**Outras regras do template:** os modificadores valem só online e só para quem cumpriu o requisito (quem está no FTUE não vê evento nem modificador); missão de comprar/contratar se completa sozinha se não houver mais o que comprar (`exhaust = AutoComplete`, §5.2); toda missão aceita pelo menos uma família do distrito 1 (o jogador nos distritos 2–6 conta qualquer família da lista no distrito ativo). **Ofertas: nenhuma até a Wave E** (sem IAP, sem moeda premium, V9). Na Wave E, se aprovado, só Event Bundle de cosmético fixo; nunca pontos, XP, ouro, Renome ou Blueprint à venda.

### 2.3 Knobs do `Tuning` que um evento pode tocar

O `Tuning` de instância é o A-CORE-03 (ainda não existe; hoje os números são `const` em `Balance`, `Defs.cs`). Nomes propostos; cada modificador é `Mul` ou `Add`, sempre no lado a favor do jogador e dentro da faixa.

| Knob proposto | Hoje (`Defs.cs`) | Faixa para evento | Lado a favor |
|---|---|---|---|
| `IntervaloCliente[linha]` | `Balance.ClientInterval` (6 / 9 / 8 / 14 s) | ×0,75–1,0 | menor |
| `PacienciaBase` | `Balance.PatienceBase` (30 s) | ×1,0–1,3 | maior |
| `IntervaloVipMin` / `IntervaloVipMax` | `Balance.VipMin` / `VipMax` (240 / 360 s) | ×0,7–1,0 | menor |
| `PrecoEspada`, `PrecoEscudo`, `PrecoFerramenta`, `PrecoJoia` (`PrecoFamilia[f]` na Wave C) | `Balance.Price` (10 / 25 / 16 / 60; 80 com a Vitrine) | ×1,0–1,25 | maior |
| `MulTempoFornalha` | multiplica `FurnaceTime0..2` (4,0 / 2,5 / 1,6 s) | ×0,8–1,0 | menor |
| `MulTempoBancada[família]` | multiplica `HammerTime0/1` (5 / 3 s) e `ShieldTimeMul`/`JewelTimeMul` | ×0,85–1,0 | menor |
| `TempoColeta` | `Balance.GrabTime` (0,4 s) | ×0,7–1,0 | menor |
| `PremioEncomendaSeg` | `Balance.OrderRewardSeconds` (10 s) | ×1,0–1,3 | maior |
| `QualidadeEvento[família]` | — (Wave B; "modificador de evento 0–25", `ECONOMIA_WAVE_B.md` §1.2) | +0–25 centésimos | maior |
| `RecargaGolpe` | — (Wave B; 20 s, `ECONOMIA_WAVE_B.md` §1.4) | ×0,75–1,0 | menor |

**Proibidos a evento:** `OfflineCapSeconds` (trava de 2 h no núcleo, nunca knob), `OfflineFactor`, `OfflineMaxNextUpgrades`, `BoostCooldownSeconds` e a recarga do VIP chamado (V11), tetos e placements de anúncio, e **custo de upgrade** (desconto temporário vira corrida de compra e fura os portões de ritmo do econsim).

### 2.4 Os 10 eventos (os 9 campos do DM §28)

Legenda de gatilho: ✓ = evento do diário que já existe; ✗ = evento novo (lista no §4.6). "auto" = completa sozinha se não houver o que comprar.

#### E1 · Royal Week

| Campo | Valor |
|---|---|
| id | `ev_s1w1_royal_week` |
| início / fim | T0+0d 13:00 UTC → T0+6d 13:00 UTC (S1, semana 1) |
| requisitos | Ferramentas + Renome 1 |
| missões (700) | atender 5 VIPs (`vip_served` ✓) 100 · entregar 2 encomendas Royal (`order_complete` ✗; pede Renome 2 durante o evento) 120 · vender 300 produtos (`product_sold` ✓) 80 · fabricar 40 peças Fina ou melhor (`quality_crafted` ✗) 100 · vender 50 escudos ou peças de armadura (`product_sold` ✓) 80 · comprar 2 melhorias (`upgrade_buy` ✓, auto) 60 · abrir o cofre 2× (`offline_claim` ✓) 60 · cumprir 1 Royal Contract (`contract_complete` ✓; raia H, no lugar dos 10 golpes de mestre: o cartão mostra 8 missões, `BALANCE.md` §36) 100 |
| recompensas | trilha do §2.2; 450: `cos_ev_s1w1_selo` "Selo do Emissário" (emblema do perfil); 1.000: `cos_ev_s1w1_tapete` "Tapete real do balcão" (decoração) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido da corte" |
| visual | estandartes azul e ouro na fachada e na rua, VIP de capa real, música "royal" (GDD §18), fala curta do capitão real no cartão (GDD §4) |
| modificadores | `IntervaloVipMin/Max` ×0,75 (VIP a cada 3–4,5 min) |
| ofertas | nenhuma até a Wave E |

#### E2 · Forge Frenzy

| Campo | Valor |
|---|---|
| id | `ev_s1w2_forge_frenzy` |
| início / fim | T0+7d → T0+13d |
| requisitos | Ferramentas + 10 encomendas entregues |
| missões (700) | vender 500 produtos (`product_sold` ✓) 120 · fabricar 300 lingotes (`product_crafted` ✓) 80 · entregar 8 encomendas (`order_done` ✓) 100 · vender 100 espadas (`product_sold` ✓) 60 · vender 60 ferramentas (`product_sold` ✓) 60 · atender 2 VIPs (`vip_served` ✓) 60 · comprar 3 melhorias (`upgrade_buy` ✓, auto) 80 · jogar em 3 dias do evento (`login_claim` ✗) 140 |
| recompensas | 450: `cos_ev_s1w2_emblema` "Emblema da Chama Dupla"; 1.000: `cos_ev_s1w2_fole` "Fole de ferro batido" (decoração da fornalha) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido do frenesi" |
| visual | fornalha em overdrive (6º estado, GDD §18), multidão na rua, faíscas extras, música "event" |
| modificadores | `IntervaloCliente` ×0,8 em todas as linhas; `PacienciaBase` ×1,15 (mais gente sem mais cliente perdido, coerente com o FS-1) |
| ofertas | nenhuma até a Wave E |

#### E3 · Armorer's Challenge

| Campo | Valor |
|---|---|
| id | `ev_s1w3_armorers_challenge` |
| início / fim | T0+14d → T0+20d |
| requisitos | Ferramentas + Renome 1 |
| missões (700) | vender 150 escudos ou peças de armadura (`product_sold` ✓) 150 · fabricar 100 escudos (`product_crafted` ✓) 80 · entregar 4 encomendas de escudo ou armadura (`order_done` ✓) 100 · fabricar 20 escudos Fina ou melhor (`quality_crafted` ✗) 100 · subir 1 nível da bancada de escudos (`upgrade_buy` categoria `Meta` ✓*, auto) 80 · acertar 5 golpes em escudos (`master_strike` ✗) 90 · vender 300 produtos (`product_sold` ✓) 100 |
| recompensas | 450: `cos_ev_s1w3_emblema` "Emblema do Armeiro"; 1.000: `cos_ev_s1w3_manequim` "Manequim com armadura" (decoração da rua) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido da guilda dos armeiros" |
| visual | paleta de Armorer's Row (ferro escuro, couro, rebites, GDD §6), armaduras em suportes na rua; o rival comercial aparece no cartão com uma fala (DM §37, sem PvP) |
| modificadores | `PrecoEscudo` ×1,2 (e `PrecoFamilia` das armaduras ×1,2 no distrito 2, Wave C); `MulTempoBancada[escudo]` ×0,9 |
| ofertas | nenhuma até a Wave E |

#### E4 · Masterwork Hunt "King's Sunblade"

| Campo | Valor |
|---|---|
| id | `ev_s1w4_hunt_sunblade` |
| início / fim | T0+21d → T0+27d |
| requisitos | Ferramentas + Renome 1 + `bp_mw_primeira_lamina` (marco de 100 espadas; Hall aberto, `ECONOMIA_WAVE_B.md` §4) |
| missões (700) | vender 400 espadas (`product_sold` ✓) 120 · fabricar 10 espadas Superiores (`quality_crafted` ✗; abaixo de q 100 a Superior sai do golpe numa peça Fina) 120 · acertar 15 golpes em espadas (`master_strike` ✗) 100 · entregar 2 encomendas Royal (`order_complete` ✗) 100 · entregar 2 encomendas Quality (`order_complete` ✗) 90 · subir 1 nível das bigornas (`upgrade_buy` `Meta` ✓*, auto) 70 · vender 1.000 produtos (`product_sold` ✓) 100 |
| recompensas | 450: **`bp_mw_sunblade`** (projeto da Lâmina Solar do Rei; fonte grátis permanente = fila Royal 2º, `ECONOMIA_WAVE_B.md` §4; quem já tem recebe ouro 60 s, mesmo ID); 1.000: `cos_ev_s1w4_tocha` "Tocha do Caçador" (decoração do Hall) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido do tesoureiro real" |
| visual | Hall em destaque (pedestais iluminados), faísca dourada (GDD §18), retrato da Sunblade no cartão, música "royal" |
| modificadores | `QualidadeEvento[espada]` +15; `RecargaGolpe` ×0,75 (20 → 15 s) |
| ofertas | nenhuma até a Wave E |

Registrar a King's Sunblade (1.000 espadas vendidas + peça Superior + golpe, `ECONOMIA_WAVE_B.md` §6) **não é missão do evento**: é a conquista `ach_sunblade`. O Hunt acelera o caminho, mas não é a única janela.

#### E5 · Forge Frenzy-B (reserva, só distrito 1)

| Campo | Valor |
|---|---|
| id | `ev_s1w5_forge_frenzy_b` |
| início / fim | T0+28d → T0+34d, ou a semana de um evento que atrasou |
| requisitos | Ferramentas + Joalheria |
| missões (700), só eventos que já existem | vender 400 produtos (`product_sold` ✓) 100 · vender 40 joias (`product_sold` ✓) 100 · fabricar 250 lingotes (`product_crafted` ✓) 80 · entregar 8 encomendas (`order_done` ✓) 120 · vender 80 escudos (`product_sold` ✓) 80 · atender 2 VIPs (`vip_served` ✓) 60 · abrir o cofre 2× (`offline_claim` ✓) 60 · comprar 2 melhorias (`upgrade_buy` ✓, auto) 100 |
| recompensas | 450: `cos_ev_s1w5_emblema` "Emblema da Lua de Forja"; 1.000: `cos_ev_s1w5_lanterna` "Lanterna do Frenesi" (decoração; troca de paleta do pacote do E2, sem modelo novo) |
| pontos de evento | trilha do §2.2 |
| visual | pacote do E2 em versão noturna (lanternas, paleta trocada) |
| modificadores | iguais aos do E2 |
| ofertas | nenhuma até a Wave E |

#### E6 · Blacksmith Festival

| Campo | Valor |
|---|---|
| id | `ev_s2w1_blacksmith_festival` |
| início / fim | T0+35d → T0+41d (abre a Season 2) |
| requisitos | Ferramentas + 10 encomendas entregues |
| missões (700) | fabricar 400 lingotes (`product_crafted` ✓) 100 · vender 400 produtos (`product_sold` ✓) 100 · comprar 3 melhorias (`upgrade_buy` ✓, auto) 80 · acertar 5 golpes de mestre (`master_strike` ✗) 80 · contratar ou subir de nível 1 worker (`worker_hired` ✓ / `worker_upgrade` ✗, auto) 80 · entregar 6 encomendas (`order_done` ✓) 100 · abrir o cofre 2× (`offline_claim` ✓) 60 · atender 3 VIPs (`vip_served` ✓) 100 |
| recompensas | 450: `cos_ev_s2w1_emblema` "Emblema da Bigorna Acesa"; 1.000: `cos_ev_s2w1_guirlanda` "Guirlanda de ferro da fornalha" (cosmético de fornalha, DM §123) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido do festival" |
| visual | fogueiras e bandeirolas laranja, fornalha com brasa decorativa, música "event" |
| modificadores | `MulTempoFornalha` ×0,85; `IntervaloCliente` ×0,9 |
| ofertas | nenhuma até a Wave E |

#### E7 · Rare Ore Rush

| Campo | Valor |
|---|---|
| id | `ev_s2w2_rare_ore_rush` |
| início / fim | T0+42d → T0+48d |
| requisitos | Ferramentas + Renome 1 |
| missões (700) | fabricar 600 lingotes (`product_crafted` ✓) 150 · vender 300 produtos (`product_sold` ✓) 80 · entregar 2 encomendas Bulk (`order_complete` ✗) 100 · comprar 2 melhorias (`upgrade_buy` ✓, auto) 60 · vender 60 ferramentas (`product_sold` ✓) 80 · contratar ou subir de nível 1 worker (`worker_hired` ✓ / `worker_upgrade` ✗, auto) 80 · jogar em 3 dias do evento (`login_claim` ✗) 150 |
| recompensas | 450: `cos_ev_s2w2_emblema` "Emblema do Veio de Brasa"; 1.000: `cos_ev_s2w2_carrinho` "Carrinho de minério de brasa" (decoração do depósito) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido dos mineiros" |
| visual | veios de "minério de brasa" no depósito (só skin do minério; o lingote é o mesmo), brilho rubro na fornalha, música "event" |
| modificadores | `TempoColeta` ×0,7 (0,4 → 0,28 s); `MulTempoFornalha` ×0,9 |
| ofertas | nenhuma até a Wave E |

O "rare ore" do DM §123 entra como skin, não como item: um minério novo pede `ItemDef` e cadeia (Wave C; o distrito 5 já tem minério rúnico). Ver decisão no §7.

#### E8 · Merchant Caravan

| Campo | Valor |
|---|---|
| id | `ev_s2w3_merchant_caravan` |
| início / fim | T0+49d → T0+55d |
| requisitos | Ferramentas + Renome 1 + encomenda Bulk aberta (2 linhas e 5 entregues) |
| missões (700) | entregar 2 encomendas Bulk (`order_complete` ✗) 140 · entregar 2 encomendas Mixed (`order_complete` ✗) 120 · entregar 10 encomendas (`order_done` ✓) 120 · vender 500 produtos (`product_sold` ✓) 100 · vender 80 escudos (`product_sold` ✓) 80 · abrir o cofre 2× (`offline_claim` ✓) 60 · atender 2 VIPs (`vip_served` ✓) 80 |
| recompensas | 450: `cos_ev_s2w3_emblema` "Emblema da Caravana"; 1.000: `cos_ev_s2w3_balanca` "Balança do mercador" (decoração do balcão) |
| pontos de evento | trilha do §2.2; encomenda Event "Lote da caravana" |
| visual | a carroça da rua lateral (já existe no mundo, `Balance.Obstacles` "carroca") vira a do mercador, o mercador mentor (GDD §4) no cartão, toldos coloridos |
| modificadores | `PremioEncomendaSeg` ×1,25 |
| ofertas | nenhuma até a Wave E |

#### E9 · Masterwork Hunt "Ember Crown"

| Campo | Valor |
|---|---|
| id | `ev_s2w4_hunt_ember_crown` |
| início / fim | T0+56d → T0+62d |
| requisitos | Ferramentas + Joalheria + Renome 1 |
| missões (700) | vender 80 joias (`product_sold` ✓) 120 · fabricar 20 joias Fina ou melhor (`quality_crafted` ✗) 120 · acertar 10 golpes em joias (`master_strike` ✗) 100 · entregar 2 encomendas Royal (`order_complete` ✗) 100 · subir 1 nível da bancada de joias (`upgrade_buy` `Meta` ✓*, auto) 70 · vender 800 produtos (`product_sold` ✓) 100 · entregar 3 encomendas de joia (`order_done` ✓) 90 |
| recompensas | 450: **`bp_mw_ember_crown`** (fonte grátis permanente = fila Royal 3º; quem já tem recebe ouro 60 s, mesmo ID); 1.000: `cos_ev_s2w4_vitrine` "Vitrine de brasa" (decoração da joalheria) |
| pontos de evento | trilha do §2.2; encomenda Event "Pedido da coroa" |
| visual | Hall com brasa, coroa em silhueta no cartão, faísca dourada, música "event" |
| modificadores | `QualidadeEvento[joia]` +15; `RecargaGolpe` ×0,75 |
| ofertas | nenhuma até a Wave E |

Registrar a Ember Crown pede Renome 4 (`ECONOMIA_WAVE_B.md` §6): o Hunt prepara o caminho (projeto, peças Fina+, golpes); o registro é a conquista `ach_ember_crown`.

#### E10 · Royal Week-B (reserva, só distrito 1)

| Campo | Valor |
|---|---|
| id | `ev_s2w5_royal_week_b` |
| início / fim | T0+63d → T0+69d, ou a semana de um evento que atrasou |
| requisitos | Ferramentas + Renome 1 |
| missões (700), só eventos que já existem | atender 5 VIPs (`vip_served` ✓) 120 · vender 300 produtos (`product_sold` ✓) 80 · vender 60 escudos (`product_sold` ✓) 80 · vender 100 espadas (`product_sold` ✓) 80 · entregar 6 encomendas (`order_done` ✓) 120 · comprar 2 melhorias (`upgrade_buy` ✓, auto) 80 · abrir o cofre 2× (`offline_claim` ✓) 60 · fabricar 200 lingotes (`product_crafted` ✓) 80 |
| recompensas | 450: `cos_ev_s2w5_emblema` "Emblema do Arauto"; 1.000: `cos_ev_s2w5_flamula` "Flâmula do arauto" (decoração da rua; troca de paleta do pacote do E1) |
| pontos de evento | trilha do §2.2 |
| visual | pacote do E1 com paleta de fim de tarde (dourado e vermelho) |
| modificadores | iguais aos do E1 |
| ofertas | nenhuma até a Wave E |

## 3. Masterwork Pass S1/S2 (D-PROD-02)

### 3.1 Estrutura

| Item | Valor |
|---|---|
| Temporadas | `pass_s1_royal_commission` (T0+0 → T0+35) e `pass_s2_ember_festival` (T0+35 → T0+70); sem intervalo entre elas |
| Duração | 35 dias (DM §33: 28–35) |
| Níveis | 30 |
| XP por nível | **600** fixo → nível 30 = **18.000 XP** |
| Trilhas | free (Wave D) e premium (descrita aqui, **desligada até a Wave E**) |
| Crédito | automático ao subir de nível; ID `p:{pass}:{nível}:f` (free) e `p:{pass}:{nível}:p` (premium) |
| Depois do 30 | o XP não acumula (sem nível bônus na v1) |
| Fim da temporada | nível não alcançado não paga; nada essencial mora no Pass |

### 3.2 Fontes de XP

| Fonte | XP | Teto |
|---|---|---|
| Diária concluída | 100 | 3 por dia |
| Semanal concluída | 350 | 4 por semana |
| Encomenda entregue (qualquer tipo) | 10 | 100 por dia (knob `PassXpEncomendaTetoDia`) |
| Evento (trilha de pontos, §2.2) | 50 / 100 / 150 / 200 nos limiares 100 / 350 / 700 / 1.000 | 500 por evento |
| Conquista | 50–200 (§4.5) | fora da estimativa: é bônus, e ajuda quem entra no meio da temporada |
| Login | 0 | o DM §33 não lista login como fonte |

### 3.3 Estimativa: o F2P fecha a trilha free em ~28 dias

F2P de referência do §1 (3 diárias por dia, 4 semanais por semana, 20 min/dia, todo evento fechado, sem anúncio, sem XP de conquista):

| Fonte | Por dia | Dia 7 | Dia 14 | Dia 21 | Dia 28 |
|---|---|---|---|---|---|
| Diárias (3 × 100) | 300 | 2.100 | 4.200 | 6.300 | 8.400 |
| Semanais (4 × 350 por semana) | 200 | 1.400 | 2.800 | 4.200 | 5.600 |
| Encomendas (8 × 10; portão da Wave B) | 80 | 560 | 1.120 | 1.680 | 2.240 |
| Eventos (500 por semana) | 71 | 500 | 1.000 | 1.500 | 2.000 |
| **XP acumulado** | **651** | **4.560** | **9.120** | **13.680** | **18.240** |
| **Nível** | | 7 | 15 | 22 | **30** |

- **Fecha no dia 27,6** (18.000 / 651) com o portão de encomendas da Wave B (≥ 25 em 60 min → 8 por dia, `ECONOMIA_WAVE_B.md` §10).
- **Com o ritmo medido hoje** (21 em 60 min → 7 por dia, 70 XP, `BALANCE.md` §20): 641 XP/dia, fecha no **dia 28,1**.
- Ritmo de ~1 nível por dia; a semana 5 (dias 28–34) é a folga para quem faltou até ~7 dias de diárias.
- **Casual** (2 de 3 diárias, 3 de 4 semanais, 5 encomendas por dia, 70% de cada evento): ~450 XP/dia → **nível 26 no dia 35**, sem fechar. Fechar o casual pede ~520 XP por nível, e aí o F2P fecharia no dia ~24. Decisão no §7.
- **É conta de planilha, não medida.** O econsim hoje roda sessões de 60 min (`BALANCE.md` §22); medir o Pass pede o bot jogando dias de 20 min com missões (portão no §6).

### 3.4 Nível → recompensa (free × premium)

Free: ouro, Renome e cosmético fixo (§0.3), igual em tipo e valor nas duas temporadas; só o cosmético muda de tema. Premium: só cosmético (C), decoração (D), conveniência (V) e Gems (G, Wave E). Nada de ouro, Renome, XP ou Blueprint no premium (§3.6).

| Nível | XP | Free (S1 e S2) | Premium S1 Royal Commission | Premium S2 Ember Festival |
|---|---|---|---|---|
| 1 | 600 | ouro 30 s | C · Gibão real azul (ferreiro) | C · Gibão chamuscado (ferreiro) |
| 2 | 1.200 | Renome +3 | V · Supply Drop ×1 | V · Supply Drop ×1 |
| 3 | 1.800 | ouro 30 s | D · Flâmula do leão (balcão) | D · Flâmula da fênix (balcão) |
| 4 | 2.400 | emblema do perfil: S1 "Selo da Comissão" · S2 "Selo da Brasa" | V · Trocar encomenda ×1 | V · Trocar encomenda ×1 |
| 5 | 3.000 | Renome +5 | G · Gems (Wave E) | G · Gems (Wave E) |
| 6 | 3.600 | ouro 45 s | C · Martelo de bronze real | C · Martelo rubro |
| 7 | 4.200 | ouro 45 s | V · Velocidade 2× (60 s) ×1 | V · Velocidade 2× (60 s) ×1 |
| 8 | 4.800 | Renome +5 | D · Tapete da corte (oficina) | D · Tapete de cinzas (oficina) |
| 9 | 5.400 | ouro 45 s | V · Supply Drop ×1 | V · Supply Drop ×1 |
| 10 | 6.000 | bandana do ferreiro: S1 "azul-real" · S2 "brasa" | C · Pajem da corte (ajudante) | C · Aprendiz do festival (ajudante) |
| 11 | 6.600 | ouro 60 s | D · Busto do rei (rua) | D · Braseiro de pedra (rua) |
| 12 | 7.200 | Renome +5 | V · Trocar encomenda ×1 | V · Trocar encomenda ×1 |
| 13 | 7.800 | ouro 60 s | C · Moedas com selo real (efeito de venda) | C · Moedas em brasa (efeito de venda) |
| 14 | 8.400 | ouro 60 s | V · Velocidade 2× ×1 | V · Velocidade 2× ×1 |
| 15 | 9.000 | decoração do balcão: S1 "Flâmula real" · S2 "Lanternas de brasa" | G · Gems (Wave E) | G · Gems (Wave E) |
| 16 | 9.600 | ouro 60 s | D · Lanternas azuis (rua) | D · Lanternas de festival (rua) |
| 17 | 10.200 | Renome +8 | C · Arauto (balconista) | C · Mercadora do festival (balconista) |
| 18 | 10.800 | ouro 75 s | V · Supply Drop ×2 | V · Supply Drop ×2 |
| 19 | 11.400 | ouro 75 s | D · Toldo real (fachada) | D · Toldo laranja (fachada) |
| 20 | 12.000 | skin de ajudante: S1 "Tabardo da guarda" · S2 "Avental chamuscado" | C · Reverência (comemoração do ferreiro) | C · Martelo no alto (comemoração) |
| 21 | 12.600 | ouro 75 s | V · Trocar encomenda ×2 | V · Trocar encomenda ×2 |
| 22 | 13.200 | Renome +8 | D · Base de mármore (pedestal do Hall) | D · Base de obsidiana (pedestal do Hall) |
| 23 | 13.800 | ouro 90 s | C · Rastro de pétalas (passos) | C · Rastro de faíscas (passos) |
| 24 | 14.400 | ouro 90 s | V · Velocidade 2× ×2 | V · Velocidade 2× ×2 |
| 25 | 15.000 | moldura do cartão de encomenda: S1 "Brasão" · S2 "Brasas" | G · Gems (Wave E) | G · Gems (Wave E) |
| 26 | 15.600 | ouro 90 s | D · Bigorna com brasão | D · Bigorna rubra |
| 27 | 16.200 | Renome +10 | C · Fornalha de friso dourado (DM §123) | C · Fornalha de brasa (DM §123) |
| 28 | 16.800 | ouro 120 s | V · Supply Drop ×2 | V · Supply Drop ×2 |
| 29 | 17.400 | ouro 120 s | D · Estandarte duplo (rua) | D · Arco de fitas (rua) |
| 30 | 18.000 | estandarte da fachada: S1 "Estandarte da Comissão Real" · S2 "Braseiro do Festival" | C · Armadura de gala do ferreiro | C · Coroa de fogo do festival (visual) |

Totais da trilha free por temporada: **ouro 1.170 s** (17 níveis), **Renome +44** (7 níveis), **6 cosméticos**. Premium por temporada: 17 cosméticos/decorações, 10 conveniências (cargas de conteúdo fixo: Supply Drop = lingotes de 60 s da fornalha, GDD §17.1; Velocidade e Trocar respeitam a recarga e mostram a próxima opção antes), 3 de Gems (valor só com o mapa fonte → ralo da Wave E, V9).

### 3.5 Até a Wave E: Pass só com a trilha free

- Flags: `pass_free` liga na Wave D; `pass_premium` fica **desligada** até a Wave E (sem moeda premium nem IAP, V9).
- Com `pass_premium` desligada, a tela do Pass mostra **só a trilha free**: sem a faixa premium, sem cadeado, sem preço, sem "em breve" (nada de vitrine do que não se pode comprar).
- Todo o resto funciona igual: XP, níveis, recompensas free, fim de temporada.
- Quando a Wave E ligar o premium: a compra é IAP direta (sem moeda intermediária), confirmada só depois do save gravado e com restore (E-PLAT-04). Quem compra no meio da temporada recebe **na hora** o premium dos níveis já alcançados, cada um com o seu ID (`p:{pass}:{n}:p`), sem duplicar. Os cosméticos premium são posse re-derivada do recibo; as conveniências são creditadas 1× pelo ID.
- Preço: decisão do D-PROD-02 para a Wave E. Referência do mercado: passes de US$ 8,99 (Pizza Ready!) e US$ 9,99 (Forge Master) (`BENCHMARK_MERCADO.md` §2, §8).

### 3.6 Regras do Pass

1. **Nada obrigatório só no premium** (DM §33, GDD §16): premium só aceita cosmético, decoração, conveniência e Gems. Nunca Blueprint (essencial ou não), Renome, ouro, XP ou pontos. Regra do validator (§5.4).
2. A trilha free fecha com o F2P de referência dentro de `duração − 7` dias (estático no validator, medido no econsim).
3. Todo nível tem recompensa free (não existe nível "só premium").
4. Cosmético do Pass é sazonal e não volta como recompensa grátis em outra fonte (cosmético em uma fonte só, §5.7).

## 4. Missões (D-PROD-03)

### 4.1 Regras comuns

- **Abrem depois das Ferramentas** (§0.7). O MPH abre as diárias no nível 3 (`BENCHMARK_MERCADO.md` §8); aqui o marco é a compra das Ferramentas.
- **Virada do dia:** diárias e login viram às 04:00 do horário do aparelho (quem joga perto da meia-noite não perde a diária no meio da sessão); semanais viram com a semana do calendário (T0 + 7k, UTC), alinhadas com os eventos. Recomendação; decisão no §7.
- **Rodízio sem sorteio:** cada dia tem 3 diárias, uma de cada pool (A volume, B gestão, C qualidade/Renome/evento): `diária[pool] = pool[(diaLocal × 5 + índice do pool) mod tamanho]`, pulando as travadas, as esgotadas e as que repetem gatilho no mesmo dia. Pool C sem nenhuma aberta (antes da Quality) → sai a próxima do pool A. Com evento ativo e requisito cumprido, o slot C alterna `dly_evento100` e `dly_enc_evento`.
- **Troca grátis:** 1 por dia, mostra antes qual entra (a próxima do rodízio). Sem anúncio.
- **Semanais:** 4 por semana. Com evento ativo, o slot 1 é `wkl_evento350`; os outros saem de `(semana × 3 + k) mod 15` sobre as demais, pulando travadas.
- **Alvo fixo** no Old Forge; nos distritos 2–6 o alvo de volume multiplica pelo `missionScale` do `DistrictDef` (Wave C, calibrar no econsim). Alvo em "s de renda" congela a taxa no momento em que a missão aparece.
- **Recompensa padrão:** diária = XP 100 + ouro 20 s; semanal = XP 350 + ouro 60 s + Renome +5.
- **Missão de comprar/contratar** (`exhaust = Skip`): não aparece se não houver o que comprar.

### 4.2 Diárias (34 modelos)

| # | ID | Missão | Pool | Abre | Gatilho no diário |
|---|---|---|---|---|---|
| 1 | `dly_vende50` | Vender 50 produtos | A | Ferramentas | `product_sold` ✓ |
| 2 | `dly_espadas30` | Vender 30 espadas | A | Ferramentas | `product_sold` (a = espada) ✓ |
| 3 | `dly_escudos20` | Vender 20 escudos | A | Ferramentas | `product_sold` (escudo) ✓ |
| 4 | `dly_ferram20` | Vender 20 ferramentas | A | Ferramentas | `product_sold` (ferramenta) ✓ |
| 5 | `dly_joias10` | Vender 10 joias | A | Joalheria | `product_sold` (joia) ✓ |
| 6 | `dly_lingotes100` | Fabricar 100 lingotes | A | Ferramentas | `product_crafted` (lingote) ✓ |
| 7 | `dly_forja_espadas40` | Fabricar 40 espadas | A | Ferramentas | `product_crafted` (espada) ✓ |
| 8 | `dly_forja_escudos20` | Fabricar 20 escudos | A | Ferramentas | `product_crafted` (escudo) ✓ |
| 9 | `dly_forja_ferram60` | Fabricar 60 ferramentas | A | Ferramentas | `product_crafted` (ferramenta) ✓ |
| 10 | `dly_forja_joias10` | Fabricar 10 joias | A | Joalheria | `product_crafted` (joia) ✓ |
| 11 | `dly_vende150` | Vender 150 produtos | A | Renome 1 | `product_sold` ✓ |
| 12 | `dly_vende300` | Vender 300 produtos | A | Renome 2 | `product_sold` ✓ |
| 13 | `dly_renda5min` | Ganhar em vendas 300 s de renda | A | Ferramentas | `currency_source` (venda, vip) ✓ |
| 14 | `dly_enc2` | Entregar 2 encomendas (DM §30) | B | Ferramentas | `order_done` ✓ |
| 15 | `dly_enc3` | Entregar 3 encomendas | B | Renome 1 | `order_done` ✓ |
| 16 | `dly_melhoria1` | Comprar 1 melhoria (DM §30 "melhorar station") | B | Ferramentas | `upgrade_buy` ✓ |
| 17 | `dly_melhoria2` | Comprar 2 melhorias | B | Ferramentas | `upgrade_buy` ✓ |
| 18 | `dly_nivel_estacao` | Subir 1 nível de estação | B | estação L2 à venda | `upgrade_buy` (categoria `Meta`) ✓* |
| 19 | `dly_gasta10min` | Gastar em melhorias 600 s de renda | B | Ferramentas | `currency_sink` ✓ |
| 20 | `dly_cofre` | Abrir o cofre (DM §30 "coletar offline") | B | Ferramentas | `offline_claim` ✓ |
| 21 | `dly_vip1` | Atender 1 VIP, natural ou chamado (DM §30 "chamar VIP", com caminho sem anúncio) | B | Ferramentas | `vip_served` ✓ |
| 22 | `dly_vip2` | Atender 2 VIPs | B | Renome 1 | `vip_served` ✓ |
| 23 | `dly_contrata` | Contratar 1 worker | B | vaga de worker à venda | `worker_hired` ✓ |
| 24 | `dly_worker_nivel` | Subir 1 nível de worker | B | Wave B (níveis) | `worker_upgrade` ✗ |
| 25 | `dly_fina_escudos10` | Fabricar 10 escudos Fina ou melhor | C | 1ª fonte de qualidade nos escudos | `quality_crafted` (escudo) ✗ |
| 26 | `dly_fina_joias5` | Fabricar 5 joias Fina ou melhor | C | 1ª fonte de qualidade nas joias | `quality_crafted` (joia) ✗ |
| 27 | `dly_golpe_superior1` | Acertar 1 golpe que faz Superior | C | Quality aberta | `master_strike` (superior) ✗ |
| 28 | `dly_fina30` | Fabricar 30 peças Fina ou melhor | C | Renome 2 | `quality_crafted` ✗ |
| 29 | `dly_fina10` | Fabricar 10 peças Fina ou melhor | C | 1ª fonte de qualidade | `quality_crafted` ✗ |
| 30 | `dly_superior3` | Fabricar 3 peças Superiores (golpe numa peça Fina) | C | Quality aberta | `quality_crafted` ✗ |
| 31 | `dly_golpe3` | Acertar 3 golpes de mestre | C | Quality aberta | `master_strike` ✗ |
| 32 | `dly_renome5` | Ganhar 5 de Renome | C | Renome 1 | `reputation_gain` ✗ |
| 33 | `dly_evento100` | Ganhar 100 pontos de evento | C | evento ativo + requisito | `event_points` ✗ |
| 34 | `dly_enc_evento` | Entregar 1 encomenda de evento | C | evento ativo + requisito | `order_complete` (Event) ✗ |

Pools: A tem 13, B tem 11, C tem 10. Logo depois das Ferramentas já há ≥ 3 abertas em A e em B. ✓* = o evento existe, mas o filtro pede o `category` no `UpgradeDef` (B-CORE-01). Nenhuma diária pede encomenda de tipo especial (Bulk, Quality, Rush, Mixed, Royal): com 1 de cada a cada 10 no rodízio, não fecha em 15 min. Elas ficam nas semanais.

### 4.3 Semanais (16 modelos)

| # | ID | Missão | Abre | Gatilho no diário |
|---|---|---|---|---|
| 1 | `wkl_enc25` | Entregar 25 encomendas (DM §31) | Ferramentas | `order_done` ✓ |
| 2 | `wkl_vende2000` | Vender 2.000 produtos | Ferramentas | `product_sold` ✓ |
| 3 | `wkl_lingote1500` | Fabricar 1.500 lingotes | Ferramentas | `product_crafted` ✓ |
| 4 | `wkl_familias3` | Vender 150 de cada uma de 3 famílias (DM §31 "produzir famílias") | Ferramentas | `product_sold` (itens distintos) ✓ |
| 5 | `wkl_melhoria10` | Comprar 10 melhorias ou níveis | Ferramentas | `upgrade_buy` ✓ |
| 6 | `wkl_vip15` | Atender 15 VIPs | Ferramentas | `vip_served` ✓ |
| 7 | `wkl_cofre5` | Abrir o cofre 5× | Ferramentas | `offline_claim` ✓ |
| 8 | `wkl_joias100` | Vender 100 joias | Joalheria | `product_sold` (joia) ✓ |
| 9 | `wkl_enc_premium5` | Entregar 5 encomendas Quality, Royal ou Mixed | Quality aberto | `order_complete` ✗ |
| 10 | `wkl_bulk3` | Entregar 3 encomendas Bulk | Bulk aberto | `order_complete` ✗ |
| 11 | `wkl_royal2` | Entregar 2 encomendas Royal | Renome 2 | `order_complete` ✗ |
| 12 | `wkl_superior20` | Fabricar 20 peças Superiores | Quality aberta | `quality_crafted` ✗ |
| 13 | `wkl_golpe50` | Acertar 50 golpes de mestre | Quality aberta | `master_strike` ✗ |
| 14 | `wkl_contrato1` | Cumprir 1 Royal Contract (DM §31) | Royal Contracts (Wave D) | `contract_complete` ✓ |
| 15 | `wkl_diarias15` | Concluir 15 diárias | Ferramentas | `mission_complete` ✗ |
| 16 | `wkl_evento350` | Fazer 350 pontos no evento da semana (DM §31 "participar de evento") | evento ativo + requisito | `event_points` ✗ |

Encomendas de tipo especial: ~8 por dia dão ~56 por semana, ~5 de cada tipo (rodízio do `ECONOMIA_WAVE_B.md` §3.2), então 2–5 por semana cabem.

### 4.4 Login de 7 dias

| Dia do ciclo | Recompensa | ID |
|---|---|---|
| 1 | ouro 20 s | `l:{ciclo}:1` |
| 2 | Renome +2 | `l:{ciclo}:2` |
| 3 | ouro 30 s | `l:{ciclo}:3` |
| 4 | Renome +2 | `l:{ciclo}:4` |
| 5 | ouro 40 s | `l:{ciclo}:5` |
| 6 | Renome +3 | `l:{ciclo}:6` |
| 7 | decoração fixa do ciclo: c1 `cos_login_sino` "Sino do balcão" · c2 `cos_login_placa` "Placa entalhada da forja" · c3 `cos_login_barril` "Barril de ferramentas" · c4 `cos_login_lanterna` "Lanterna de ferro"; do ciclo 5 em diante, ouro 60 s (mostrado antes) | `l:{ciclo}:7` |

- **Sem reset punitivo** (DM §135): o dia do ciclo avança 1 por dia de calendário com sessão; dia perdido só pausa, nunca volta ao dia 1. Depois do 7, começa o ciclo seguinte.
- **Catch-up:** nenhum além de não resetar (como nada se perde, não há o que recuperar).
- Abre no **D1** (o 1º dia de calendário depois da instalação, já com as Ferramentas): dá ao D1 uma razão de voltar sem mexer no D0.
- Aparece **dentro do cartão da volta** (GDD §19, DM §136), nunca como popup próprio.
- Sem "2× por anúncio" (o MPH tem, `BENCHMARK_MERCADO.md` §8, mas não está no GDD §17.1).

### 4.5 Conquistas (77)

Todas visíveis (nenhuma escondida), progresso **derivado dos contadores do núcleo** (como a Collection, GDD §13): save antigo recebe o retroativo quando a tela abre. Recompensas: ouro, XP do Pass, Renome, cosmético fixo (insígnias). Conquista disparada por Renome não dá Renome (sem laço). Quando o sistema de origem já dá Renome ou cosmético (Masterwork, marco de coleção, Charter), a conquista dá só XP ou insígnia própria, para não duplicar.

| # | ID | Conquista | Categoria | Gatilho | Recompensa |
|---|---|---|---|---|---|
| 1 | `ach_primeira_forja` | Fabricar a 1ª espada | Crafting | `product_crafted` ✓ | ouro 30 s |
| 2 | `ach_primeira_venda` | Fazer a 1ª venda | Crafting | `first_sale` ✓ | ouro 30 s |
| 3 | `ach_vende100` | Vender 100 produtos | Crafting | `product_sold` ✓ | XP 50 |
| 4 | `ach_vende1k` | Vender 1.000 produtos | Crafting | `product_sold` ✓ | XP 100 + Renome +5 |
| 5 | `ach_vende10k` | Vender 10.000 produtos | Crafting | `product_sold` ✓ | XP 200 + Renome +10 |
| 6 | `ach_vende100k` | Vender 100.000 produtos | Crafting | `product_sold` ✓ | insígnia "Mão de Ouro" |
| 7 | `ach_espadas500` | Fabricar 500 espadas | Crafting | `product_crafted` ✓ | XP 100 |
| 8 | `ach_escudos500` | Fabricar 500 escudos | Crafting | `product_crafted` ✓ | XP 100 |
| 9 | `ach_ferram500` | Fabricar 500 ferramentas | Crafting | `product_crafted` ✓ | XP 100 |
| 10 | `ach_joias200` | Fabricar 200 joias | Crafting | `product_crafted` ✓ | XP 100 |
| 11 | `ach_lingotes5k` | Fabricar 5.000 lingotes | Crafting | `product_crafted` ✓ | XP 100 + Renome +5 |
| 12 | `ach_quatro_linhas` | Vender os 4 produtos do Old Forge | Crafting | `product_sold` (distintos) ✓ | Renome +5 |
| 13 | `ach_primeira_fina` | 1ª peça Fina | Quality | `quality_crafted` ✗ | ouro 30 s |
| 14 | `ach_primeira_superior` | 1ª peça Superior | Quality | `quality_crafted` ✗ | XP 50 + Renome +3 |
| 15 | `ach_fina100` | 100 peças Fina ou melhor | Quality | `quality_crafted` ✗ | XP 100 |
| 16 | `ach_superior1k` | 1.000 peças Superiores | Quality | `quality_crafted` ✗ | insígnia "Olho de Mestre" |
| 17 | `ach_golpe1` | 1º golpe de mestre | Quality | `master_strike` ✗ | ouro 30 s |
| 18 | `ach_golpe100` | 100 golpes de mestre | Quality | `master_strike` ✗ | XP 100 + Renome +5 |
| 19 | `ach_golpe_superior50` | 50 golpes que fazem Superior | Quality | `master_strike` ✗ | XP 150 |
| 20 | `ach_mw1` | 1ª Masterwork registrada | Masterworks | `masterwork_unlock` ✗ | XP 100 |
| 21 | `ach_mw5` | 5 Masterworks | Masterworks | `masterwork_unlock` ✗ | XP 150 |
| 22 | `ach_mw10_oldforge` | As 10 Masterworks do Old Forge | Masterworks | `masterwork_unlock` ✗ | XP 200 + insígnia "Hall Completo" |
| 23 | `ach_familia_espada` | Família espada completa (3) | Masterworks | `masterwork_unlock` ✗ | XP 100 |
| 24 | `ach_sunblade` | Registrar a King's Sunblade | Masterworks | `masterwork_unlock` ✗ | insígnia "Sol do Rei" |
| 25 | `ach_ember_crown` | Registrar a Ember Crown | Masterworks | `masterwork_unlock` ✗ | insígnia "Coroa de Brasa" |
| 26 | `ach_mw30` | 30 Masterworks | Masterworks | `masterwork_unlock` ✗ | XP 200 + Renome +10 |
| 27 | `ach_mw60` | 60 Masterworks | Masterworks | `masterwork_unlock` ✗ | insígnia "Lenda da Forja" |
| 28 | `ach_bp1` | 1º Blueprint | Blueprints | `blueprint_unlock` ✗ | ouro 30 s |
| 29 | `ach_bp10` | 10 Blueprints | Blueprints | `blueprint_unlock` ✗ | XP 100 + Renome +5 |
| 30 | `ach_bp18` | Os 18 Blueprints do Old Forge | Blueprints | `blueprint_unlock` ✗ | XP 200 + insígnia "Arquivista" |
| 31 | `ach_enc1` | 1ª encomenda | Orders | `order_done` ✓ | ouro 30 s |
| 32 | `ach_enc50` | 50 encomendas | Orders | `order_done` ✓ | XP 100 |
| 33 | `ach_enc250` | 250 encomendas | Orders | `order_done` ✓ | XP 150 + Renome +5 |
| 34 | `ach_enc1000` | 1.000 encomendas | Orders | `order_done` ✓ | XP 200 + insígnia "Livro-Caixa Cheio" |
| 35 | `ach_enc_royal1` | 1ª encomenda Royal | Orders | `order_complete` ✗ | XP 100 |
| 36 | `ach_enc_tipos7` | Uma encomenda de cada tipo (Common, Bulk, Quality, Rush, Mixed, Royal, Blueprint) | Orders | `order_complete` ✗ | XP 150 + Renome +10 |
| 37 | `ach_rush_prazo10` | 10 Rush no prazo | Orders | `order_complete` (rush no prazo) ✗ | XP 100 |
| 38 | `ach_bulk25` | 25 encomendas Bulk | Orders | `order_complete` ✗ | XP 100 |
| 39 | `ach_ajudante1` | 1º ajudante | Workers | `worker_hired` ✓ | ouro 30 s |
| 40 | `ach_ajudantes6` | Os 6 ajudantes do Old Forge | Workers | `worker_hired` ✓ | XP 100 + Renome +5 |
| 41 | `ach_oficial1` | 1º oficial (Smith) | Workers | `worker_hired` (id do `WorkerDef`) ✓ | XP 50 |
| 42 | `ach_worker_n5` | 1 worker no nível 5 | Workers | `worker_upgrade` ✗ | XP 100 |
| 43 | `ach_workers13` | Os 13 workers do Old Forge | Workers | `worker_hired` ✓ | XP 150 + Renome +10 |
| 44 | `ach_workers13_n5` | Os 13 no nível 5 | Workers | `worker_upgrade` ✗ | insígnia "Mestre de Ofício" |
| 45 | `ach_workers20` | 20 workers contratados | Workers | `worker_hired` ✓ | XP 200 |
| 46 | `ach_esteira` | Comprar a Esteira | Automation | `upgrade_buy` (Conveyor) ✓ | ouro 45 s |
| 47 | `ach_joalheria` | Abrir a Joalheria | Automation | `upgrade_buy` (Jewelry) ✓ | XP 50 |
| 48 | `ach_producao_completa` | Produção completa do Old Forge | Automation | `upgrade_buy` (produtivos) ✓ | XP 200 + Renome +10 |
| 49 | `ach_balcao8` | Balcão com 8 vagas | Automation | `upgrade_buy` ✓ | XP 50 |
| 50 | `ach_mineiro` | Contratar o Mineiro | Automation | `upgrade_buy` (Miner) ✓ | XP 50 |
| 51 | `ach_estacao_n3` | 1ª estação no nível 3 | Automation | `upgrade_buy` (`Meta`) ✓* | XP 50 |
| 52 | `ach_estacoes_n5` | Todas as estações do Old Forge no nível 5 | Automation | `upgrade_buy` (`Meta`) ✓* | XP 200 + insígnia "Oficina de Ferro" |
| 53 | `ach_charter1` | Royal Charter do Old Forge | Districts | `district_unlock` ✗ | XP 200 (a insígnia é do próprio Charter, GDD §11) |
| 54 | `ach_elmos100` | Vender 100 elmos (Armorer's Row) | Districts | `product_sold` (elmo) ✓ | XP 100 |
| 55 | `ach_distritos3` | 3 distritos abertos | Districts | `district_unlock` ✗ | XP 200 + Renome +10 |
| 56 | `ach_distritos6` | 6 distritos abertos | Districts | `district_unlock` ✗ | insígnia "Senhor das Ruas" |
| 57 | `ach_contrato1` | 1º Royal Contract | Districts | `contract_complete` ✓ | XP 150 |
| 58 | `ach_contratos12` | Os 12 Royal Contracts | Districts | `contract_complete` ✓ | insígnia "Arsenal do Reino" |
| 59 | `ach_ouro10k` | Ganhar 10 mil de ouro em vendas | Economy | `currency_source` ✓ | XP 50 |
| 60 | `ach_ouro1m` | Ganhar 1 milhão de ouro em vendas | Economy | `currency_source` ✓ | XP 200 + Renome +10 |
| 61 | `ach_gasto50k` | Gastar 50 mil em melhorias | Economy | `currency_sink` ✓ | XP 100 |
| 62 | `ach_luxo1` | 1º luxo | Economy | `upgrade_buy` (`Luxury`) ✓ | XP 50 |
| 63 | `ach_luxo3` | Os 3 luxos | Economy | `upgrade_buy` (`Luxury`) ✓ | insígnia "Oficina Nobre" |
| 64 | `ach_cofre30` | Abrir o cofre 30× | Economy | `offline_claim` ✓ | XP 100 |
| 65 | `ach_baus4` | Abrir os 4 baús de marco | Economy | `chest_opened` ✓ | XP 50 |
| 66 | `ach_vip10` | Atender 10 VIPs | Economy | `vip_served` ✓ | XP 50 |
| 67 | `ach_vip100` | Atender 100 VIPs | Economy | `vip_served` ✓ | XP 150 + Renome +5 |
| 68 | `ach_renome1` | Renome nível 1 | Renome | `reputation_gain` ✗ | XP 50 |
| 69 | `ach_renome4` | Renome nível 4 | Renome | `reputation_gain` ✗ | insígnia "Fornecedor do Reino" |
| 70 | `ach_colecao_produtos` | Aba Produtos 100% no Old Forge | Renome | `collection_milestone` ✗ | XP 100 |
| 71 | `ach_colecao_clientes` | Aba Clientes 100% | Renome | `collection_milestone` ✗ | XP 100 |
| 72 | `ach_evento1` | Fechar a trilha de 1 evento | LiveOps | `event_complete` ✗ | XP 100 |
| 73 | `ach_eventos4` | Entrar em 4 eventos | LiveOps | `event_enter` ✗ | XP 150 |
| 74 | `ach_passe_free` | Fechar a trilha free de um Pass | LiveOps | `pass_level` ✗ | insígnia "Veterano de Temporada" |
| 75 | `ach_login7` | Completar um ciclo de login | LiveOps | `login_claim` ✗ | XP 50 |
| 76 | `ach_diarias30` | Concluir 30 diárias | LiveOps | `mission_complete` ✗ | XP 100 |
| 77 | `ach_semanais10` | Concluir 10 semanais | LiveOps | `mission_complete` ✗ | XP 150 |

Por categoria (DM §32 + Quality, Blueprints, Renome e LiveOps): Crafting 12 · Quality 7 · Masterworks 8 · Blueprints 3 · Orders 8 · Workers 7 · Automation 7 · Districts 6 · Economy 9 · Renome/Coleção 4 · LiveOps 6. Gatilho já existente: **38**; evento novo: **39**. Conquista cujo conteúdo ainda não existe no build (ex.: `ach_mw60` com 10 Masterworks, `ach_elmos100` antes do distrito 2) fica fora do build pelo `requiresContent` (§5.5). O DM §32 lista Gems e o GDD §15 lista Blueprint como recompensa de conquista: nenhuma das duas aqui (V9; Blueprint pode entrar depois como fonte extra, nunca única).

### 4.6 Eventos do diário: os que já existem e os que faltam

**Já existem e são usados** (`Game.cs`, `Servicos.cs`): `product_sold` (item, preço), `product_crafted` (item, estação), `order_done` (item, ouro), `upgrade_buy` (upgrade, preço), `offline_claim` (ouro, s), `vip_served` (item, ouro), `worker_hired` (índice), `chest_opened` (baú, ouro), `first_sale`, `currency_source` / `currency_sink` (origem/destino, ouro por minuto). Equivalências com o DM §86: `order_new`/`order_done` = `order_start`/`order_complete` sem o tipo; `ad_reward` = `rewarded_complete`.

**Faltam** (cada um nasce com o ticket do sistema; o núcleo emite o `Ev`, o `Game.cs` loga, o `diario_report.py` aprende a ler):

| Evento novo | Parâmetros (a, b) | Taxonomia DM §86? | Ticket | Usado por |
|---|---|---|---|---|
| `order_complete` | tipo (Common…Event; `rush_prazo` no bônus), item | sim | B-CORE-05 | 1 diária, 3 semanais, 4 conquistas, 5 eventos |
| `quality_crafted` | item, nível (fina, superior) | não | B-CORE-03 | 5 diárias, 1 semanal, 4 conquistas, 4 eventos |
| `master_strike` | item, nível resultante | não | B-CORE-03 | 2 diárias, 1 semanal, 3 conquistas, 5 eventos |
| `worker_upgrade` | worker, nível | sim | B-CORE-04 | 1 diária, 2 conquistas, 2 eventos |
| `reputation_gain` | fonte, total | não | B-CORE-06 | 1 diária, 2 conquistas |
| `blueprint_unlock` | Blueprint, fonte | sim | B-CORE-07 | 3 conquistas |
| `masterwork_unlock` | Masterwork, família | sim | B-CORE-08 | 8 conquistas |
| `collection_milestone` | aba, marco | não | B-CORE-08 | 2 conquistas |
| `district_unlock` | distrito, via (charter) | sim | C-CORE-01..03 | 3 conquistas |
| `contract_complete` (+ `contract_accept`, `contract_abandon`; nome do código, #64) | contrato, distrito | sim | D-CORE-04 | 1 semanal, 2 conquistas, 1 missão da Royal Week |
| `event_enter` / `event_complete` | evento, limiar | sim | D-CORE-03 | 2 conquistas; telemetria |
| `event_points` | evento, total | não | D-CORE-03 | 1 diária, 1 semanal |
| `login_claim` | dia do ciclo, ciclo | não | D-CORE-02 | 1 conquista, 2 eventos ("jogar em 3 dias") |
| `mission_complete` | tipo (daily, weekly, event), id | não | D-CORE-02 | 1 semanal, 2 conquistas |
| `achievement_unlock` | id, categoria | não | D-CORE-02 | telemetria |
| `pass_level` (+ `pass_view` do §86) | temporada, nível | `pass_view` sim | D-CORE-04 | 1 conquista; telemetria |

Também precisa: o `category` (`Production`, `Meta`, `Luxury`) no `UpgradeDef` (B-CORE-01, `ECONOMIA_WAVE_B.md` §8.7) para filtrar `upgrade_buy` sem evento novo; e as novas origens no `currency_source`: `diaria`, `semanal`, `login`, `passe`, `evento`, `conquista`. Recomendação: no Orders 2.0, emitir `order_complete` e manter `order_done` por uma versão, para o `diario_report.py` não quebrar.

## 5. Esquema de dados e validator

Mesmo formato do `ECONOMIA_WAVE_B.md` §8. Campos comuns: `id` (string estável com prefixo `ev_`, `dly_`, `wkl_`, `evm_`, `ach_`, `pass_`, `cos_`), `nameKey`, `iconId`, `analyticsKey`. O validator segue o padrão do `ContentValidator` (A-CORE-05): cada regra é uma função curta sobre os **dados recebidos**; o `ContentValidatorTests` injeta uma cópia quebrada e a regra fica vermelha.

### 5.1 `RewardDef` (embutido nos outros)

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `kind` | enum `Gold`, `Reputation`, `PassXp`, `EventPoints`, `Cosmetic`, `Blueprint`, `Convenience`, `Gems` | **não existe** `Chest`, `Random`, `Chance`: "nada aleatório" é estrutural | `Gems` exige a moeda `gems` ativa (V9); `Convenience` e `Gems` só na trilha premium |
| `goldSeconds`, `goldMin` | float, long | ouro = s × `RateEma`, múltiplo de 5, mín. 25 | > 0; entra no orçamento do §1 |
| `amount` | int | Renome, XP, pontos, cargas | > 0 |
| `refId` | string | cosmético, Blueprint ou conveniência | existe |
| `fallback` | `RewardDef`? | quando a posse já existe (Blueprint de evento) | obrigatório em `Blueprint`; usa o mesmo ID de transação |

### 5.2 `MissionDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `dly_`, `wkl_`, `evm_` | único |
| `kind` | enum `Daily`, `Weekly`, `Event` | | — |
| `pool` | enum `A`, `B`, `C` | só `Daily` | pools A e B com ≥ 3 abertas logo depois das Ferramentas |
| `trigger` | (`Ev`, filtro) | filtro: item, família, tipo de encomenda, nível de qualidade, categoria de upgrade, origem de ouro | o `Ev` existe no núcleo, ou a missão tem `feature` com a flag desligada; **proibidos**: `client_left`, `bottleneck`, `ad_*`, `boost_start`, `rewarded_*` (sem punição, sem anúncio obrigatório) |
| `count` | enum `Count`, `SumSeconds`, `DistinctItems`, `DistinctDays` | | — |
| `target` / `targetSeconds` | int / float | s × `RateEma` congelado quando a missão aparece | > 0; `Daily` ≤ ritmo do F2P em 15 min no estágio do `unlock` (tabela do econsim) |
| `unlock` | condição | mesma linguagem do `RecipeDef.unlock` | alcançável; implica "Ferramentas comprado" |
| `exhaust` | enum `Skip`, `AutoComplete` | | obrigatório quando o gatilho é compra ou contratação; `Event` ⇒ `AutoComplete` |
| `families` | string[] | missão de volume | `Event` ⇒ inclui ≥ 1 família do distrito 1 |
| `reward` | `RewardDef[]` | | `Daily`/`Weekly`: só `PassXp`, `Gold`, `Reputation`; `Event`: só `EventPoints` |
| `feature` | string? | flag do sistema de que depende | flag existe |

### 5.3 `EventDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `ev_s1w1_royal_week` | único **para sempre** (reprise = id novo; o ID de transação usa o id) |
| `template` | enum dos 8 do DM §29 | | — |
| `seasonId` | string | | temporada existe; janela dentro dela |
| `startDay`, `durationDays` | int, int | dias desde T0 (UTC); 6 | janelas de eventos não se sobrepõem |
| `requirements` | condição[] | | alcançáveis pelo F2P em ≤ 40 min de jogo (D0/D1 do F2P de referência); implicam Ferramentas |
| `minDistrict` | int | 1 nas reservas e em S1/S2 | reserva (`isReserve`) ⇒ só gatilhos que já existem e só knobs do distrito 1 |
| `missions` | `MissionDef.id[]` | `kind = Event` | soma dos pontos ≥ 70% do último limiar; ≤ 2 encomendas de cada tipo especial |
| `track` | (limiar, `RewardDef[]`)[] | §2.2 | limiares crescentes; `Blueprint` só com fonte grátis permanente e `fallback` |
| `eventOrder` | `OrderDef.id` | tipo `Event`, pontos por entrega | `OrderDef` existe |
| `rewardedBonus` | (pontos, por dia) | | fora da conta de fechamento |
| `visual` | `themeId` | paleta, fachada, música, pacote de VFX, fala | assets existem (No Placeholder Gate na release) |
| `modifiers` | (knob, `Mul`/`Add`, valor, escopo)[] | §2.3 | knob existe no `Tuning`; dentro da faixa; no lado a favor; nunca knob proibido; vale só online e para quem entrou |
| `offers` | `OfferDef.id[]` | | vazio enquanto a flag `iap` estiver desligada; oferta nunca vende pontos, XP, ouro, Renome nem Blueprint; conteúdo fixo |
| `isReserve` | bool | | — |

### 5.4 `PassDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id`, `seasonId` | string | `pass_s1_royal_commission` | único |
| `startDay`, `durationDays` | int | 0 e 35 | `durationDays` em 28–35; temporadas sem sobreposição |
| `levels` | int | 30 | ≥ 1 |
| `xpPerLevel` | int[] | 600 em todos | XP acumulado estritamente crescente |
| `free` | `RewardDef` por nível | | um por nível; kinds ⊂ {`Gold`, `Reputation`, `Cosmetic`} |
| `premium` | `RewardDef` por nível | | kinds ⊂ {`Cosmetic`, `Convenience`, `Gems`}: **nada de progressão só no premium** |
| `premiumFlag` | string | `pass_premium` | desligada até a Wave E |
| `xpRules` | diária 100, semanal 350, encomenda 10, teto 100/dia | eventos dão XP pela própria trilha | > 0 |
| `themeId` | string | | existe |

Regra de fechamento (estática): `xpF2PDia × (durationDays − 7) ≥ XP total`, com `xpF2PDia = 3 × diária + 4 × semanal / 7 + min(teto, encomendasF2PDia × encomenda) + Σ XP dos eventos da temporada / durationDays`. S1/S2: 651 × 28 = 18.228 ≥ 18.000. O econsim confirma (§6).

### 5.5 `AchievementDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `ach_` | único |
| `category` | enum `Crafting`, `Quality`, `Masterworks`, `Blueprints`, `Orders`, `Workers`, `Automation`, `Districts`, `Economy`, `Reputation`, `LiveOps` | | — |
| `trigger` | (`Ev`, filtro) | igual ao `MissionDef` | mesmas proibições |
| `counter` | contador do núcleo | vendas por item, encomendas por tipo, Masterworks… | existe; save antigo recebe o retroativo |
| `target` | long | | > 0; maior que o do `prev` |
| `prev` | `AchievementDef.id`? | escada | sem ciclo |
| `requiresContent` | condição sobre dados | ex.: 60 `MasterworkDef` | sem o conteúdo, a conquista fica fora do build |
| `reward` | `RewardDef[]` | | kinds ⊂ {`Gold`, `PassXp`, `Reputation`, `Cosmetic`}; gatilho `reputation_gain` ⇒ sem `Reputation` |

### 5.6 `LoginDef`

| Campo | Tipo | Validator |
|---|---|---|
| `cycleDays` | int (7) | — |
| `days` | `RewardDef[7]` | kinds ⊂ {`Gold`, `Reputation`, `Cosmetic`} |
| `day7Cosmetics` | `cos_` id por ciclo, depois `fallback` ouro | cada cosmético único |

### 5.7 Regras cruzadas (novas funções no `ContentValidator`)

1. **IDs únicos** por tipo e **IDs de transação únicos por construção** (prefixo + dono + índice; dois dados nunca geram o mesmo ID).
2. **Recompensa duplicada impossível:** cada `cos_` aparece em **uma** fonte só, contando Pass, evento, login, conquista, Masterwork, marco de coleção, nível de Renome e Charter (ex.: `ach_charter1` não pode repetir a insígnia do Charter).
3. **Nada aleatório:** o enum de recompensa não tem tipo aleatório; oferta só com conteúdo fixo e mostrado antes.
4. **Nada pago progride:** premium e ofertas sem `Gold`, `Reputation`, `PassXp`, `EventPoints` nem `Blueprint`.
5. **Essencial nunca só pago:** reaproveita a regra do `BlueprintDef` (`ECONOMIA_WAVE_B.md` §8.5); evento e Pass também não podem ser a única fonte de **nenhum** Blueprint.
6. **Missão alcançável:** `unlock` no grafo de condições a partir do início; alvo dentro do ritmo; `exhaust` declarado.
7. **Sem anúncio obrigatório:** gatilhos proibidos (§5.2); bônus de anúncio fora das contas de fechamento.
8. **Orçamento de ouro estático:** soma dos s de renda por dia de calendário (diárias + semanais + login + Pass + eventos) ≤ 180.
9. **Calendário:** eventos sem sobreposição e dentro da temporada; temporadas contíguas; reserva só com conteúdo do distrito 1.
10. **FTUE:** nada de LiveOps abre antes das Ferramentas.

### 5.8 Calendário e ledger (D-CORE-01)

- O núcleo recebe `nowUnix` e deriva `diaCal = ⌊(nowUnix − T0) / 86.400⌋` (eventos, temporadas, semanais) e `diaLocal` (diárias e login, com a virada às 04:00 do aparelho). O save guarda o **máximo** de cada um: com o relógio para trás nada novo abre e nada antigo reabre (o `clock_back` já é logado, A-CORE-02).
- Relógio para frente: aceito até haver hora de servidor (decisão do D-CORE-01). Quem adianta o relógio recebe as diárias antes, mas, ao voltar à hora certa, fica sem diária nova até o dia real passar o máximo gravado.
- **Ledger** = conjunto de IDs de transação no save. Crédito = (ID fora do ledger → aplica → grava o ID) no mesmo passo do núcleo e na mesma gravação do save. Repetir, recarregar ou reabrir não paga de novo.

| Recompensa | ID de transação |
|---|---|
| Diária | `d:{diaLocal}:{slot}` |
| Semanal | `w:{semana}:{slot}` |
| Login | `l:{ciclo}:{dia}` |
| Conquista | `a:{achId}` |
| Missão de evento | `em:{eventId}:{missionId}` |
| Limiar de evento | `e:{eventId}:{limiar}` |
| Pass | `p:{passId}:{nível}:{f|p}` |

- Limpeza: IDs de temporadas encerradas há mais de 35 dias saem do ledger (o dia máximo monotônico impede reabrir a janela).

**Estado no save** (o que sobrevive a matar o app):

| Estado | Campo proposto |
|---|---|
| Relógio | `cal.maxDiaCal`, `cal.maxDiaLocal` |
| Diárias | `dly.dia`, `dly.ids[3]`, `dly.prog[3]`, `dly.troca` |
| Semanais | `wkl.semana`, `wkl.ids[4]`, `wkl.prog[4]` |
| Login | `login.ciclo`, `login.dia`, `login.ultimoDia` |
| Evento | `evt.id`, `evt.entrou`, `evt.pontos`, `evt.prog[]` |
| Pass | `pass.id`, `pass.xp`, `pass.xpEncHoje`, `pass.diaEnc` |
| Conquistas | derivadas dos contadores (o save guarda só o ledger) |
| Ledger | `led` (IDs de transação) |

Save antigo (0.6/0.7): tudo vazio; conquistas recebem o retroativo pelos contadores na 1ª abertura da tela.

### 5.9 Testes negativos (`coretests`)

1. Crédito 1×: concluir, salvar, recarregar, concluir de novo → paga uma vez (diária, semanal, login, conquista, limiar, nível do Pass).
2. Matar o app entre o crédito e a gravação → ao reabrir, ou pagou e o ID está no ledger, ou não pagou e o ID não está (nunca meio termo).
3. Relógio para trás (DM §133 "clock change"): nenhuma diária, semana, evento ou login novo; janela antiga não reabre.
4. Reset de evento (DM §133): reiniciar o evento com o mesmo id não paga limiar já pago.
5. Login: pular 3 dias → o próximo login paga o dia seguinte do ciclo, não o dia 1.
6. Primeiros 10 min do bot F2P iguais tick a tick com qualquer evento ativo (o requisito tira o modificador do FTUE).
7. Cofre aberto durante o evento paga igual ao de fora do evento com a mesma taxa base (modificador só online).
8. Validator: cada regra do §5.7 vermelha com um defeito injetado numa cópia (cosmético repetido, Blueprint só em premium, missão com `client_left`, janelas sobrepostas, premium com `Reputation`, evento com `OfflineCapSeconds`).

## 6. Portões do econsim

| Medida | Portão | Knob se falhar |
|---|---|---|
| Trilha free do Pass, F2P de referência | fecha entre o dia 26 e o 30 da temporada de 35 | XP por nível |
| Trilha de evento, F2P sem anúncio | fecha no dia 4–5 de 6 | pontos das missões / da encomenda Event |
| Diária | cada uma em ≤ 15 min de jogo no estágio em que abre | alvo |
| Ouro de LiveOps | ≤ 15% da receita de venda do F2P por dia | s de renda das recompensas |
| Renome de LiveOps | ≤ 25% do Renome do F2P em 28 dias | Renome das semanais e dos eventos |
| Modificador de evento | ouro/min do F2P ≤ +15% durante o evento; cofre igual | faixa do knob |
| Primeiros 10 min | iguais tick a tick com evento ativo | requisito do evento |
| Casual (2/3 diárias, 3/4 semanais) | informativo: nível no dia 35 (conta: 26) | XP por nível (decisão §7) |

Pré-requisito: o econsim precisa jogar **dias** (2 sessões de 10 min com intervalo, cofre, virada de dia) e não só sessões de 60 min. Isso entra no D-CORE-02.

## 7. Decisões pendentes (Vinicius)

| # | Decisão | Recomendação | Trava |
|---|---|---|---|
| 1 | T0 (data da Season 1) e horário das viradas | eventos e temporadas às 13:00 UTC (10:00 de Brasília); diárias e login às 04:00 do aparelho | D-PROD-01 (DECISÃO "datas") |
| 2 | Temporada de 35 dias com a reserva na semana 5 | sim: o F2P fecha no dia ~28 e a semana 5 é folga | D-PROD-01/02 |
| 3 | Casual (~450 XP/dia) fecha ou não a trilha free | não fechar (nível ~26): 600 XP por nível mantém o F2P em ~28 dias como pede o D-PROD-02; 520 fecharia o casual e levaria o F2P para ~24 | D-PROD-02 |
| 4 | Masterwork Hunt dá o projeto da Masterwork no limiar 450 | sim, como fonte extra (a fila Royal continua grátis e permanente; quem já tem recebe ouro) | D-PROD-01 |
| 5 | Premium sem ouro, Renome, XP e Blueprint (mais restrito que o DM §33, que só proíbe o "obrigatório") | sim: o validator fica simples e não existe "pagar para subir o Pass" | D-CORE-04, Wave E |
| 6 | Conveniências do premium (Supply Drop, Velocidade, Trocar) como "cargas" | decidir junto das Gems na Wave E (é um recurso novo, V9) | Wave E |
| 7 | XP de encomenda com teto de 100 por dia | sim (knob): segura o ritmo de ~28 dias para quem joga muito | D-CORE-04 |
| 8 | XP de conquista no Pass (DM §32) | sim, fora da estimativa: ajuda quem entra no meio da temporada | D-CORE-02 |
| 9 | "Rare ore" (DM §123) como skin, não como item novo | skin agora; minério de verdade só com `ItemDef` na Wave C | D-PROD-01 |
| 10 | Eventos de 6 dias + 1 de respiro | sim (referência: eventos de 6 dias no Burger Please!, `BENCHMARK_MERCADO.md` §8) | D-PROD-01 |
| 11 | Preço do premium | Wave E; referência de mercado US$ 8,99–9,99 (`BENCHMARK_MERCADO.md` §8) | D-PROD-02 (DECISÃO "preço") |
| 12 | Moeda de evento (P1 / D-CORE-03) | continuar com pontos (V16) | D-CORE-03 |

**Custo de arte** (para o pacote de Wave, P11): cosméticos free do Pass 12 (6 por temporada); eventos 18 (14 nos 8 eventos principais + 4 nas reservas, feitos por troca de paleta do evento-mãe ou ícone 2D); login 4; insígnias de conquista 15 (ícones 2D); premium 34 (só na Wave E/F). Nada disso pede crédito de IA sem aval.

## 8. Ordem de implementação

1. **D-CORE-01** calendário (`diaCal`, `diaLocal`, máximos monotônicos) + ledger com os IDs do §5.8.
2. **D-CORE-02** `MissionDef`, `AchievementDef`, `LoginDef` sobre o fluxo de `Ev`, com os eventos novos do §4.6 que já tiverem sistema (os de Wave B entram com os B-CORE); econsim jogando dias.
3. **D-CORE-03** `EventDef` com modificadores sobre o `Tuning` (depende do A-CORE-03); provar "evento novo só com dados" num PR que só adiciona a reserva Forge Frenzy-B.
4. **D-CORE-04** `PassDef` com `pass_premium` desligada; regras do validator do §5.7.
5. Rodar os portões do §6, registrar no `BALANCE.md` e pedir aprovação dos números.
