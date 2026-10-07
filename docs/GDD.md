# GDD 03 — FORGE STREET
## Idle/Tycoon Hybrid / Portrait / Android + iOS

> **Hipótese comercial:** o crescimento visual “uma bigorna → uma rua produtiva inteira” pode gerar UA forte, enquanto rewarded ads e automação criam monetização natural sem exigir backend no MVP.

# 1. VISÃO DO PRODUTO

**Nome:** Forge Street  
**Gênero:** Idle/Tycoon Hybrid / management casual  
**Plataforma:** Android primeiro.  
**Orientação:** Portrait.  
**Público:** 13–50+, idle/tycoon, crafting, fantasy casual.  
**Proposta:** transformar uma oficina minúscula em uma cadeia artesanal viva.  
**Fantasy:** “eu construí a melhor rua de artesãos do reino”.  
**Diferencial:** produção física legível — minério entra, metal funde, item é martelado, cliente leva. A evolução é visível no cenário, não apenas em números.

# 2. GAMEPLAY

## Core mechanic
Controle de um gerente/personagem por joystick simples:
- recolher minério/lingotes;
- depositar em estação;
- retirar produto;
- entregar ao balcão;
- comprar upgrades pisando em pads.

Com progressão:
- contratar trabalhadores;
- automatizar transporte;
- abrir novas bancadas;
- produzir espadas, escudos, ferramentas, joias.

## Core loop
`produzir → vender → melhorar → contratar → automatizar → expandir → nova linha`

## Vitória/derrota
Não há “derrota” tradicional.
Objetivo curto: concluir milestones de expansão.
Fricção:
- filas;
- gargalos;
- estoque;
- velocidade.

## Sessão
3–12 min; progresso offline limitado.

# 3. PRIMEIROS 10 MINUTOS

**0:00–0:45** — oficina vazia. Jogador pega 3 minérios, deposita na fornalha e vê primeiro lingote.

**0:45–1:30** — leva lingote à bigorna; NPC anima marteladas; espada aparece; cliente paga. Primeira satisfação em <90 s.

**1:30–2:30** — compra velocidade da fornalha pisando num pad. Transformação visível.

**2:30–3:30** — abre segunda bigorna; fila reduz.

**3:30–4:30** — primeira contratação de ajudante; automação parcial.

**4:30–5:30** — unlocked escudos; nova matéria-prima.

**5:30–6:30** — rewarded opcional: “trabalhador temporário 3 min” ou “2× receita por 3 min”. O jogador pode ignorar.

**6:30–7:30** — abre corredor lateral; rua começa a aparecer.

**7:30–8:30** — milestone “10 espadas”; baú visual com decoração.

**8:30–9:30** — primeira máquina/esteira simples reduz caminhada.

**9:30–10:00** — teaser da loja de joalheria fechada; razão clara para voltar.

# 4. PRIMEIRO DIA

Conteúdo:
- 3 linhas: espada, escudo, ferramenta;
- 2 matérias-primas;
- 8 estações;
- 6 trabalhadores;
- 25 upgrades;
- 2 áreas;
- 1 “prestige-lite” de distrito não resetável no D1.

Primeiro rewarded aos 5–7 min.
Primeiro IAP após o jogador automatizar a primeira linha:
- Remove Ads;
- Starter Smith Pack.

Retorno:
- cofre offline;
- pedido especial do dia seguinte;
- área 3 em progresso.

# 5. PROGRESSÃO

**Curta:** estação e trabalhador.  
**Média:** linha de produção e nova loja.  
**Longa:** rua → distrito → bairros temáticos.

- D1: forja básica.
- D3: joalheria/armaduras leves.
- D7: segundo distrito.
- D14: pedidos especiais e coleção de blueprints.
- D30: festival de artesanato.
- D60: nova cidade.
- D90: eventos sazonais reutilizáveis.

# 6. ECONOMIA

**Gold:** soft currency.
Fontes:
- vendas;
- offline;
- pedidos;
- rewarded.

Sinks:
- estação;
- worker;
- velocidade;
- capacidade;
- expansão.

**Gems:** hard currency.
Fontes:
- milestones;
- eventos;
- IAP.

Sinks:
- upgrades premium de conveniência;
- cosmetics;
- acelerar desbloqueio sem teto exclusivo.

**Blueprints:** progression resource; gameplay-first.

Inflação:
- curva exponencial controlada por área;
- reset parcial apenas se prestige for validado.

# 7. MONETIZAÇÃO

## Rewarded
- 2× receita 3–5 min;
- worker temporário;
- claim 2× offline;
- pedido especial;
- baú de blueprint.

## Interstitial
Somente após milestone/saída de área; cap forte.
No early game, priorizar rewarded.

## IAP
- Remove Ads;
- Starter Pack;
- gems;
- permanent helper convenience moderada;
- cosmetic forge themes.

Não vender multiplicadores absurdos que invalidem loop.

# 8. RETENÇÃO

D1: cofre offline + próxima loja.  
D3: blueprint e novo produto.  
D7: distrito novo.  
D14: evento de encomendas.  
D30: city expansion.

Daily:
- pedido do dia;
- bônus moderado;
- coleção.

# 9. LIVE OPS

Baixo custo:
- Double Order Weekend;
- Royal Contract;
- Blacksmith Festival;
- Rare Ore Rush;
- Blueprint Hunt.

Eventos reutilizam linhas existentes com metas e visuais.

# 10. CONTEÚDO

MVP:
- 1 área;
- 3 produtos;
- 5 estações;
- 3 workers;
- 15 upgrades.

Lançamento se validado:
- 4 distritos;
- 8–12 produtos;
- 20–30 estações;
- 12 workers visuais;
- 5 eventos;
- 40–60 upgrades;
- 20 cosmetics.

# 11. ARTE

Stylized low-poly premium.
Câmera isométrica/3D oblíqua.
Silhuetas grandes, materiais claros.
Produção deve ser filmável.

A forja pode reutilizar conhecimento de assets fantasy do COE, mas IP e identidade visual devem ser próprios.

# 12. ÁUDIO

- minério;
- furnace;
- hammer;
- coin;
- upgrade;
- worker hire;
- customer;
- milestone.

Loop musical leve; som da produção é parte do prazer.

# 13. TECNOLOGIA

Unity/C#.

Módulos:
- `Station`
- `ProductionRecipe`
- `WorkerAgent`
- `QueueSystem`
- `UpgradeSystem`
- `OfflineProgress`
- `EconomyManager`
- `SaveManager`
- `AnalyticsManager`
- `AdsManager`
- `IAPManager`
- `RemoteConfig`
- `LiveOpsManager`
- `Audio/Haptics`

Simulação offline usa timestamp validado e limites anti-clock-cheat básicos.

# 14. ANALYTICS

- tutorial_complete
- station_unlock
- upgrade_buy
- product_crafted
- product_sold
- queue_length
- bottleneck_seconds
- worker_hired
- rewarded_offer/complete
- offline_claim
- district_unlock
- iap_purchase
- currency_source/sink

Métricas:
- D1/D7;
- sessions/day;
- session duration;
- rewarded views/DAU;
- offline return rate;
- upgrade cadence;
- payer conversion;
- ARPDAU.

# 15. KPIs — TARGETS INTERNOS

- tutorial ≥90%;
- primeira venda <90 s para ≥90% dos jogadores;
- D1 ≥28%;
- D3 ≥14%;
- D7 ≥7%;
- D30 ≥2%;
- 2+ sessões/dia entre retidos;
- rewarded opt-in ≥45%;
- 2–5 rewarded/DAU sem queda de retenção;
- payer conversion ≥1%;
- crash-free >99.5%.

UA low-cost Android:
- CPI ≤ ~US$0,70 promissor;
- 0,70–1,10 iterar;
- >1,10 exige LTV bem maior.

# 16. SOFT LAUNCH

A: 6–10 criativos, BR/PH/ID.  
B: 1.000–2.000 installs para loop/retention.  
C: 3.000–5.000 com monetização e pequena amostra CA/AU.

Hipóteses:
- transformação visual reduz CPI;
- rewarded é aceito por acelerar gargalo;
- offline progress traz D1/D3.

# 17. 10 CRIATIVOS UA

1. bigorna vazia → rua lotada em 15 s;
2. fila gigante causada por fornalha lenta → upgrade resolve;
3. “onde investir 100 moedas?” duas estações;
4. trabalhador lento vs automatizado;
5. minério bruto → espada brilhante em cadeia;
6. “não deixe clientes esperando”;
7. antes/depois de contratar 5 workers;
8. erro proposital: 5 bigornas, 1 furnace;
9. rare ore aparece e produção acelera;
10. rua inteira funcionando em loop satisfatório.

# 18. MVP

- 1 área;
- 3 produtos;
- 5 estações;
- 3 workers;
- progress/save;
- offline capped;
- analytics;
- rewarded placeholder/test;
- 8 criativos.

Sem:
- mapa mundial;
- guilda;
- PvP;
- backend;
- 10 cidades.

# 19. V0.1

Core + 15 upgrades + Android.

# 20. V0.2

Se CPI/D1 passarem:
- segunda área;
- ads/IAP;
- remote config;
- orders;
- 1 evento.

# 21. V1.0

Se D7/LTV passarem:
- 4 distritos;
- 8+ produtos;
- live ops;
- iOS;
- localization;
- 3–5 eventos.

# 22. PRODUÇÃO

| Área | Peso |
|---|---|
| Programação | Médio |
| Design | Médio |
| Arte | Médio |
| UI | Baixo–Médio |
| Áudio | Baixo |
| Backend | Baixo |
| Analytics | Médio |
| Conteúdo | Baixo–Médio |

Complexidade: **5/10**.

# 23. REUTILIZAÇÃO

COE:
- crafting concepts;
- fantasy prop pipeline;
- save/domain patterns.

ARKANA:
- mobile input;
- pooling;
- animation;
- profiling.

Compartilhado:
- MobileCore.

# 24. IA

- conceitos de estação;
- icon temp;
- material variants;
- code/test assistance;
- UA storyboards;
- localization;
- balance simulation offline.

Não runtime.

# 25. RISCOS

**Parece cópia de idle arcade.**  
Diferenciar pela cadeia artesanal e decisões de gargalo, não apenas andar coletando dinheiro.

**Rewarded excessivo.**  
Opt-in e cap.

**Offline quebra economia.**  
Cap + remote tuning.

**Conteúdo vira só números maiores.**  
Novas linhas precisam mudar fluxo/layout.

# 26. KILL CRITERIA

## CONTINUAR
- D1 ≥28%;
- D7 ≥7%;
- rewarded opt-in ≥45%;
- ≥2 sessões/dia nos retidos;
- CPI dentro da faixa;
- jogadores identificam gargalos sem tutorial pesado.

## ITERAR
- D1 22–28%;
- D7 4.5–7%;
- CPI até 50% acima;
- boa retenção, mas pouca volta offline;
- muita caminhada sem decisão.

## CANCELAR
- D1 <22% após 2 iterações;
- D7 <4.5%;
- CPI >2× target em 10 criativos;
- loop vira “andar entre pilhas” sem decisão;
- rewarded necessário para progressão normal.
