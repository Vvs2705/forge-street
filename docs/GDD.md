# GDD 03 — FORGE STREET
## Idle/Tycoon Hybrid / Portrait / Android + iOS

> **Revisão 2026-10-09 (comparativo de mercado):** comparativo com 11 similares (os 10 do `BENCHMARK_MERCADO.md` + Hammer & Steel) e os dados do teste no POCO F4 em [`COMPETITIVO.md`](COMPETITIVO.md). O que mudou, sem renumerar nada:
> - **§15 e §26:** o "continuar" passa a D1 ≥ 26% e D7 ≥ 5% (era 28%/7%, acima do top quartil do GameAnalytics 2026), com coorte de 1,5–2k installs, comparação pago × pago e cancelamento só depois de 2 iterações (veredito de 2026-10-06). Pagante só a partir de 5k installs. Os portões que o código usa (1ª venda < 90 s, opt-in ≥ 45%, "andar sem decisão") não mudam.
> - **§3:** os tempos continuam sendo o alvo do **bot**. A pessoa real foi 1,7–3,7× mais lenta, e 67 clientes cansaram. O alvo humano entra em §3.1.
> - **§4/§6/§7/§13:** "prestige-lite não resetável" vira **troca de rua/distrito** (não é prestige); **1 moeda** (ouro) até a v0.2; nada aleatório pago (ECA Digital); o offline não tem anti-cheat sem backend, e o texto agora diz isso.
> - **§2 e §25:** ganham a tensão que faltava ao core e dois riscos novos (tema de fantasia mais estreito; ritmo humano).
> - **Novo §27 (Diferenciais competitivos)** e **§28 (Posicionamento)**; o §17 ganha os criativos 11–13 desses diferenciais. Os 3 primeiros: demanda que acompanha a oficina, golpe de mestre (obra-prima só do ferreiro) e "anúncio só quando você pede".

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

**Tensão (revisão 2026-10-09).** A validação apontou que os gargalos "se resolvem sozinhos" (a estação com fila pede upgrade, decisão óbvia) e que a automação tira a função do avatar. A tensão vem de três fontes, sem derrota punitiva:
- **perda visível:** o cliente cansa (anel de paciência) e quem chega com a fila cheia dá meia-volta (IMPLEMENTADO desde a v0.5.1);
- **mão do mestre:** a obra-prima só sai do ferreiro (§27, FS-2, PROPOSTO), então ficar na bigorna compete com carregar e comprar;
- **concorrência:** um rival do outro lado da rua leva os clientes que você não atende (§27, FS-4, PROPOSTO, depois do playtest).

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

## 3.1 Ritmo humano (revisão 2026-10-09)

Os tempos acima são o alvo do **bot** (`BALANCE.md` §2; o bot tem de chegar antes). No 1º teste com gente (POCO F4, `VALIDACAO_V05.md`), a pessoa foi **1,7–3,7× mais lenta**: Fole 3:21, Escudos 14:45 e Esteira 14:54, contra 2:30 / 5:30 / 8:30. Nesse tempo, 67 clientes de espada cansaram e 471 deram meia-volta com a fila cheia.

Alvo humano (PROPOSTO, a medir no playtest Camada 0 com o `diario_report.py`):
- 1ª venda < 90 s para ≥ 90% (não muda; §15);
- mediana dos testadores ≤ 2× o bot nos marcos Fole, Escudos e Esteira;
- clientes perdidos (cansou + meia-volta) nos primeiros 15 min ≤ metade do que o POCO mostrou.

A primeira alavanca é a "demanda que respira" (§27, FS-1): na largada, a chegada de clientes acompanha a vazão real da oficina. Ela deve ser provada antes num **bot lento** (velocidade e decisão ×0,4).

# 4. PRIMEIRO DIA

Conteúdo:
- 3 linhas: espada, escudo, ferramenta;
- 2 matérias-primas;
- 8 estações;
- 6 trabalhadores;
- 25 upgrades;
- 2 áreas;
- 1 “prestige-lite” de distrito não resetável no D1.

> Revisão 2026-10-09: sem reset, "prestige" não é prestige, é só mais um tier (raia B da validação). O item passa a se chamar **troca de rua/distrito**: depois da produção completa (~42 min no bot, `BACKLOG_V07.md` item 2), a oficina "se forma" e abre uma rua nova com layout novo, como a troca de loja do gênero (`BENCHMARK_MERCADO.md` B7). Prestige com reset só entra se for validado (§6). O FS-4 do §27 propõe que a rua nova seja a forja do rival.

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

**Guardrails (revisão 2026-10-09, veredito da validação):**
- **1 moeda (ouro) até a v0.2.** Gems e Blueprints só entram quando existir ralo para elas no mapa de fontes e ralos. Fonte sem ralo é inflação anunciada.
- Prêmios fora do `GoldEarned` (baú, Encomenda) continuam fora da base do offline e do VIP (`BALANCE.md` §20).
- O offline nunca dá gem nem moeda de evento.
- **Nada aleatório pago** (baú, roleta, gacha): ECA Digital, Lei 15.211/2025, em vigor desde 17/03/2026. O VIP (sequência de Weyl) e as Encomendas (rodízio) já são determinísticos. Qualquer "baú" tem conteúdo fixo e visível.
- Toda concessão tem id de transação (já vale no cofre: `Sim.ApplyOffline(elapsed, claimId)`).

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

**Revisão 2026-10-09.**
- **Interstitial:** desligado na v0.1 (veredito). A proposta FS-5 do §27 é mantê-lo desligado **para sempre**, como promessa de marca ("anúncio só quando você pede"), porque é a reclamação nº 1 nos líderes (`COMPETITIVO.md` c). Nesse caso, "Remove Ads" dá lugar a um "Pacote do Mestre" (cofre 2× permanente + skin). **Decisão do Vinicius.**
- **"Baú de blueprint" por anúncio:** só com conteúdo fixo e visível antes de assistir.
- **Rewarded já no jogo (v0.5.1):** "Chamar VIP" e "Velocidade 2×/3×". Na fila: cofre 2× (decisão D5 do `BACKLOG_V07.md`, não confundir com o FS-5 do §27).
- **Gate de dinheiro da fase B = IAA de rewarded** (opt-in, impressões/DAU, ARPDAU). Conversão de pagante só a partir de 5k installs.

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

> Revisão 2026-10-09: sem backend não existe timestamp validado. Save local não é anti-cheat; no máximo detecta relógio que voltou. A regra vigente (`BALANCE.md` §5) aceita a trapaça local com teto: 25% da taxa online × até 2 h, teto de 2× o upgrade mais barato ainda travado, Δt ≤ 0 paga 0, o cofre só abre com ≥ 60 s fora e cada claim tem id de transação. É o que o gênero faz sem servidor.

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
- D1 ≥26% (era ≥28%; revisão 2026-10-09);
- D3 ≥14%;
- D7 ≥5% (era ≥7%; revisão 2026-10-09);
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

**Como ler estes números (revisão 2026-10-09, veredito da validação).**
- O alvo antigo de D1 ≥ 28% / D7 ≥ 7% ficava acima do top quartil do GameAnalytics 2026 (D1 mediano ~20%, top-25% ~30%; D7 mediano < 4%, top-25% 6–7%). O "continuar" passa a **D1 ≥ 26% e D7 ≥ 5%**.
- **Amostra:** coorte de 1,5–2k installs pagos, comparando pago com pago. Com 1k installs, a margem do D7 é ±1,7 pp; com 2k, ±1,2 pp.
- **Pagante:** só se mede a partir de 5k installs. Antes disso, o gate de dinheiro é o IAA de rewarded (opt-in, impressões/DAU, ARPDAU).
- Os portões que o `diario_report.py` e os testes usam (1ª venda < 90 s para ≥ 90%, opt-in ≥ 45%, andar sem decisão) **não mudam**.
- **Diferenciais (§27):** cada um tem a sua métrica no `COMPETITIVO.md` d (ex.: FS-1 "demanda que respira" = clientes perdidos −50% no bot lento).

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

Revisão 2026-10-09 (criativos dos diferenciais, §27; entram no mesmo teste e não substituem os 10):

11. golpe de mestre: anel de tempo, acerto perfeito, faísca dourada e a obra-prima vendida a 3×;
12. "o goblin voltou": um freguês com nome volta, enche o 5º coração e deixa a marca na rua;
13. rival do outro lado da rua com fila enorme → a sua oficina vira o jogo → placa "VENDIDO" na forja dele.

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

**Tema de fantasia com apelo mais estreito** (revisão 2026-10-09).  
Os quatro líderes do formato usam temas do cotidiano. No tema forja, Forge & Fortune (Supercent) tem 1K+ instalações e Hammer & Steel tem 70 avaliações. Mitigação: o teste de criativos decide; incluir 2 criativos que vendem o **ofício** (a obra-prima, o freguês que volta) e não a fantasia.

**Ritmo humano bem abaixo do bot** (revisão 2026-10-09).  
A pessoa foi 1,7–3,7× mais lenta no POCO F4, e o começo pune quem ainda não sabe produzir (67 clientes cansaram). Mitigação: §3.1 e a "demanda que respira" (§27, FS-1), provadas antes num bot lento.

**Avatar sem função depois da automação** (revisão 2026-10-09, raia B).  
Mitigação: obra-prima só do ferreiro (§27, FS-2).

# 26. KILL CRITERIA

## CONTINUAR
- D1 ≥26% (era ≥28%; revisão 2026-10-09);
- D7 ≥5% (era ≥7%; revisão 2026-10-09);
- rewarded opt-in ≥45%;
- ≥2 sessões/dia nos retidos;
- CPI dentro da faixa;
- jogadores identificam gargalos sem tutorial pesado.

## ITERAR
- D1 22–26%;
- D7 4.5–5%;
- CPI até 50% acima;
- boa retenção, mas pouca volta offline;
- muita caminhada sem decisão.

## CANCELAR
- D1 <22% após 2 iterações;
- D7 <4.5%;
- CPI >2× target em 10 criativos;
- loop vira “andar entre pilhas” sem decisão;
- rewarded necessário para progressão normal.

> Revisão 2026-10-09: todos os cortes valem para uma coorte de 1,5–2k installs pagos (pago × pago) e só depois de 2 iterações. Um corte de criativo (CPI > 2× em 10 criativos) também vale para os criativos dos diferenciais do §27: se nem o golpe de mestre nem o freguês fiel baixarem o CPI, o problema é o tema, não o loop.

# 27. DIFERENCIAIS COMPETITIVOS (revisão 2026-10-09)

Resumo. O detalhe de cada um (por que, como, custo, risco e validação) e as fontes estão em [`COMPETITIVO.md`](COMPETITIVO.md) d–e. Status de todos: **PROPOSTO**. Custo: P ≤ 1 semana · M 2–4 semanas · G ≥ 1 mês.

| # | Diferencial | O que os similares fazem | Custo | Prova barata |
|---|---|---|---|---|
| FS-1 | **Demanda que respira:** nos primeiros ~15 min, a chegada de clientes acompanha a vazão real da oficina (`RateEma`, como as Encomendas) | fluxo fixo (não confirmado) | P | bot lento ×0,4: clientes perdidos −50% sem o ouro/min subir > 5% |
| FS-2 | **Golpe de mestre:** parado na bigorna, o ferreiro acerta um anel de 0,8 s e faz uma obra-prima (3× no balcão, completa a encomenda). Ajudantes nunca fazem | o avatar só carrega | M | criativo "acerto perfeito" × controle; ≥ 50% usam ≥ 2× por sessão |
| FS-3 | **Clientela fiel:** 5 fregueses com nome (mago, elfa, goblin, cavaleiro, nobre; a arte já existe) com encomendas pessoais e 5 corações; no fim, uma marca na rua com +5% fixo | clientes sem rosto | M | criativo "o goblin voltou"; testador lembra o nome |
| FS-4 | **Ferreiro rival na rua,** pilotado pelo `Bot.cs`: leva os clientes que você não atende; quando você o supera, compra a forja dele, que vira a próxima rua (§4) | nenhum encontrado | G | A/B no bot antes de qualquer arte |
| FS-5 | **Anúncio só quando você pede:** zero interstitial e zero banner, para sempre; "Pacote do Mestre" no lugar do Remove Ads | interstitial pesado | P | página da loja com e sem a frase; opt-in ≥ 45% |
| FS-6 | **Galeria de obras-primas:** 12 pedestais na rua, ordem fixa, +5% por linha completa | livro de coleção em menu (Shop Titans) | M | ≥ 30% abrem o catálogo sozinhos |
| FS-7 | **Cofre que conta:** a volta mostra o que cada linha rendeu e qual estação travou, com "ver" | pop-up com número e 2× | P | ≥ 30% compram a melhoria apontada em 60 s |

**Ordem recomendada:** FS-1 → FS-2 → FS-5 (decisão do Vinicius), depois FS-3. O FS-4 entra junto com a troca de rua (§4), depois do playtest Camada 0.

**Invariantes de design** (cada uma vira teste negativo no `coretests`, se o diferencial entrar):
- FS-1 nunca **aumenta** a chegada de clientes nem dá ouro.
- A obra-prima (FS-2) não sai de ajudante nem do offline, e não passa de 8% da receita no bot.
- Fregueses (FS-3), rival (FS-4) e galeria (FS-6) não sorteiam nada e não vendem nada.
- Toda recompensa nova tem id de transação.

# 28. POSICIONAMENTO (revisão 2026-10-09)

Para quem gosta de ver uma operação crescer sem ser interrompido, **Forge Street** é o idle arcade de forja em que **a sua mão ainda importa** (a obra-prima só sai do ferreiro) e **o jogo se ajusta ao seu ritmo**, porque foi calibrado com bot e com gente de verdade, sem anúncio forçado.

- **Inimigo:** o idle que interrompe a cada minuto e pune quem ainda está aprendendo.
- **Prova:** bot + diário de playtest (`BALANCE.md`, `diario_report.py`), 60 fps no POCO F4, nenhum interstitial.
- **Códigos distintivos:** bandana vermelha do ferreiro, faísca dourada da obra-prima, estados "travada" (vermelho) e "fome" (cinza) nas estações.
