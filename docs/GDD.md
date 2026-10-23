# GDD v2 — FORGE STREET
## Tycoon de crafting · retrato · Android primeiro · produto completo

Versão 2.0 · 2026-10-12 · Dono: Vinicius · Raia DESIGN (Leva 5 do ORQUESTRADOR).
Direção: [`DOCUMENTO_MESTRE_PRODUCAO_V1.md`](DOCUMENTO_MESTRE_PRODUCAO_V1.md) ("DM §n"). Estado, decisões e placar: [`PROJETO.md`](PROJETO.md). Tickets: [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md). Números da Wave B: [`ECONOMIA_WAVE_B.md`](ECONOMIA_WAVE_B.md). Números medidos: [`BALANCE.md`](BALANCE.md). Mercado: [`COMPETITIVO.md`](COMPETITIVO.md) e [`BENCHMARK_MERCADO.md`](BENCHMARK_MERCADO.md).

---

## 0. Como ler este documento

- **Para que serve:** é a régua do produto completo. O jogo está "100%" quando os 23 itens do §23 cumprem o critério escrito lá (mesma lista do `PROJETO.md` §8, onde fica o placar).
- **Precedência:** em conflito, vale o Documento Mestre (DM §186) e as decisões vigentes V1–V12 do `PROJETO.md` §4. Este GDD aplica as duas coisas ao design. Onde o DM e uma decisão vigente divergem, a decisão vence e o texto diz isso (ex.: DM §12 "Blueprint Tokens" × V9 "Blueprint é desbloqueio").
- **Estado de cada sistema** (escala do `PROJETO.md`): PROPOSTO → APROVADO → IMPLEMENTADO → INTEGRADO → TESTADO → VALIDADO → LANÇADO. Cada seção traz uma linha "Estado · Wave · tickets".
- **GDD v1:** o GDD do MVP está em [`archive/GDD_v1_MVP.md`](archive/GDD_v1_MVP.md). Referências do tipo "GDD §n" em documentos e código anteriores a 2026-10-12 (ex.: `BALANCE.md`, `Bot_Primeiros10Minutos_BatemASecao3`) apontam para o v1. As janelas da primeira sessão (v1 §3) continuam vigentes e estão no §3.3 daqui.
- **Números:** todo número de design é **hipótese até o bot medir** (`client/tools/econsim`, `BalanceTests`). Números medidos só no `BALANCE.md`.

**Pilares que valem para todos os sistemas**
1. **Simples no momento, fundo na meta** (DM §169): o toque continua sendo andar, carregar e escolher o gargalo; a profundidade fica nos sistemas de dias e semanas.
2. **Determinismo sem sorteio** (V7): nenhum sistema usa RNG. Qualidade por acumulador, encomendas por rodízio, VIP por sequência de Weyl. Mesma entrada, mesmo resultado; o jogador sempre consegue explicar o que aconteceu.
3. **Nada aleatório pago** (V4; ECA Digital, Lei 15.211/2025): baú tem conteúdo fixo e visível antes; "trocar" mostra a próxima opção antes de pagar ou assistir.
4. **Conteúdo em dados + Content Validator** (V6, DM §108, §126): nada de `if (district == 4)`; cada `*Def` passa no validator antes de entrar.
5. **Uma moeda até a Wave E** (V9): só o ouro. Reputation é trilha, Blueprint é posse.
6. **Toda recompensa tem ID de transação** e prêmio que não é venda fica fora do `GoldEarned` (não infla taxa online, cofre nem VIP; `BALANCE.md` §20).
7. **Ativo rende mais que offline e que fundo de distrito** (DM §27, C-CORE-03).

**O que mudou do v1 (resumo):** produto completo em 6 distritos (DM §7) no lugar do MVP de 1 área; "prestige-lite" virou **Royal Charter sem reset** (V5); Gems saem do MVP e entram só na Wave E (V9); Blueprint deixa de ser recurso e vira desbloqueio (V9); interstitial implementado atrás de flag desligada (V10), não "desligado para sempre"; os FS-1…FS-7 foram reconciliados com o DM (§21); metas de retenção têm três níveis (§20, decisão P6).

---

## 1. Visão e promessa

**Definição (DM §193):** Forge Street é um tycoon de crafting medieval-fantástico para mobile no qual uma pequena oficina manual cresce para uma rede de distritos automatizados, com produção visível, gargalos, trabalhadores, contratos, Blueprints, Masterworks, eventos, Season Pass e progressão de longo prazo.

**Hook (DM §80):** "Comece com uma bigorna. Termine comandando um distrito inteiro de artesãos."

**Promessa (DM §194):**
- Gameplay: "Eu construí isso."
- Meta: "Sempre existe algo novo para melhorar, automatizar, colecionar ou desbloquear."
- Negócio: o produto tem conteúdo, retenção e monetização para crescer depois do lançamento.

**Diferencial central:** produção física legível (minério entra, metal funde, item é martelado, cliente leva) e **a mão do ferreiro continua importando** depois da automação: só ele chega ao tier Masterwork (§9, §21 FS-2).

**Posicionamento (do v1 §28, mantido):** para quem gosta de ver uma operação crescer sem ser interrompido, Forge Street é o tycoon de forja em que a sua mão ainda importa e o jogo se ajusta ao seu ritmo, calibrado com bot e com gente de verdade. Inimigo: o idle que interrompe a cada minuto e pune quem está aprendendo (reclamação nº 1 do formato, `COMPETITIVO.md` c.1).

**Fora do escopo da v1.0:** PvP, guildas, mapa mundial, backend próprio grande. A arquitetura não deve impedir leaderboard, amigos, visitar forja e guildas depois (DM §38).

---

## 2. Público e plataforma

| Item | Valor | Fonte |
|---|---|---|
| Gênero | tycoon de crafting / idle arcade híbrido, management casual | DM §168 |
| Público | 13–50+, fãs de idle/tycoon, crafting e fantasia casual. Inclui adolescentes: vale o ECA Digital (nada aleatório pago; menor de 18 sem anúncio personalizado, E-PLAT-01) | v1 §1; `PROJETO.md` V4 |
| Plataforma | Android primeiro (minSdk 26, ARM64, target API 36, 16 KB). iOS depois do Global, sem data | DM §96–98; `PROJETO.md` §1 |
| Orientação | retrato, uma mão | DM §156 |
| Sessão | 3–12 min; várias sessões por dia (meta 2+ entre retidos) | v1 §2, §15 |
| Idiomas | pt-BR, en, es (texto fora do código) | DM §109 |
| Desempenho | 60 fps em aparelho forte; 30/45 estáveis em intermediário por perfil (Battery/Balanced/High) | DM §111–112 |
| Aparelho de marco | POCO F4 só nos 4 gates (Feature Complete, RC, bug Android físico, build de publicação) | V2, DM §2 |
| Lançamento | Internal → Closed → Limited Release → Global (estratégia de publicação, não MVP) | DM §165 |

---

## 3. Core loop e os 5 loops de retenção

Estado: loop base IMPLEMENTADO (v0.6) · meta loops por Wave (B–E).

### 3.1 Core loop (DM §5)

```
COLETAR MINÉRIO → PROCESSAR (fornalha) → FABRICAR (bigorna/bancada) → ABASTECER (balcão)
→ VENDER / ENTREGAR → GANHAR OURO / RENOME / BLUEPRINT → LER O GARGALO
→ UPGRADE / WORKER / AUTOMAÇÃO → NOVA LINHA → ORDER / CONTRACT → DISTRITO
→ AUTOMATIZAR O DISTRITO ANTIGO → EVENTOS / MASTERWORKS / PASS → IMPÉRIO ARTESANAL
```

A decisão central do minuto a minuto é **ler qual etapa trava** (estação "travada" = saída cheia, vermelho; "fome" = sem insumo, cinza) e investir ali. Tensão sem derrota: cliente que cansa ou dá meia-volta (perda visível), a obra-prima que só sai da mão do ferreiro (competir por tempo do avatar) e, se aprovado, o rival na rua (FS-4).

### 3.2 Cinco loops de retenção (DM §6)

| Loop | Conteúdo | Sistemas | Wave |
|---|---|---|---|
| Segundos | coletar, levar, fabricar, vender, golpe de mestre | núcleo, Quality (golpe) | já existe / B |
| Minutos | upgrade, worker, nível de estação, Order | upgrades, Workers, Orders 2.0 | já existe / B |
| Sessão | automação, nova linha, major unlock, Masterwork | Quality, Masterworks, Reputation | B |
| Dias | Blueprint, Masterwork, distrito, daily, evento, login de 7 dias | Blueprints, distritos, Daily, LiveOps | B, C, D |
| Semanas | Season, Pass, Royal Contracts, novos distritos | Pass, Contracts, Charter | C, D |

### 3.3 Primeira sessão (FTUE) — janelas do bot

Tutorial curto, sem explicar sistemas futuros (DM §81): pegar minério → fornalha → lingote → fabricar → vender → comprar upgrade; depois worker, Order e rewarded.

Metas internas (DM §81): 1ª venda < 90 s · upgrade significativo < 3 min · automação < 5–7 min · produto novo < 8–10 min.

Janelas da primeira sessão (v1 §3, **vigentes**; o `Bot_Primeiros10Minutos_BatemASecao3` aceita do piso — 30% do tempo para o Fole, 50% para os demais — até o tempo da tabela; `BALANCE.md` §2):

| Tempo | Marco |
|---|---|
| 0:00–0:45 | oficina; 3 minérios na fornalha; 1º lingote |
| 0:45–1:30 | lingote na bigorna; espada; cliente paga (**1ª venda < 90 s**) |
| 1:30–2:30 | Fole (velocidade da fornalha) |
| 2:30–3:30 | 2ª bigorna |
| 3:30–4:30 | 1º ajudante |
| 4:30–5:30 | Escudos |
| 5:30–6:30 | 1º rewarded opcional (VIP aos ~5:36 no bot, `BALANCE.md` §18.5) |
| ≤ 8:30 | Esteira |
| 9:30–10:00 | razão clara para voltar (teaser do próximo passo) |

**Regra da Wave B:** nenhum sistema novo pode mexer nos primeiros 10 min. Quality, níveis de estação e tipos novos de Order abrem depois das Ferramentas (~10:46 no bot).

### 3.4 Ritmo humano

No POCO F4 a pessoa foi **1,7–3,7× mais lenta** que o bot; 67 clientes de espada cansaram e 471 deram meia-volta (`VALIDACAO_V05.md`; `COMPETITIVO.md` c.6). Alvo humano (PROPOSTO, mede no Closed com `diario_report.py`): mediana ≤ 2× o bot nos marcos Fole, Escudos e Esteira; clientes perdidos nos primeiros 15 min ≤ metade do POCO. Primeira alavanca: FS-1 (§21), provada antes no bot Slow.

### 3.5 Do D0 ao D90 (DM §82–84)

| Momento | Novidade obrigatória | Sistemas |
|---|---|---|
| D0 | cadeia completa, 1º worker, Orders, cofre, preview do Hall (1º projeto de Masterwork aos ~100 espadas), teaser do distrito seguinte, 1º rewarded opcional | núcleo, Orders, Blueprints |
| D1 | grande marco de distrito: Royal Charter do Old Forge → Armorer's Row | Charter, Reputation |
| D3 | Blueprint de estação, especialidade de worker, distrito novo | Blueprints, Workers |
| D7 | Royal Contract, evento, marco de coleção | Contracts, LiveOps, Collection |
| D30 | vários distritos, Season, eventos, Masterworks | tudo |
| D60 | Arcane Foundry | distrito 5 |
| D90 | ciclos de endgame no Grand Forge; próxima cidade (pós-lançamento, DM §9) | distrito 6 |

---

## 4. Fantasia de progressão

Estado: PROPOSTO (escada) · personagem/ferramentas em parte IMPLEMENTADO (Mochila, Botas).

```
EU FAÇO TUDO SOZINHO → TENHO UM AJUDANTE → GERENCIO UMA OFICINA → CONTROLO UMA RUA
→ ADMINISTRO VÁRIOS DISTRITOS → ATENDO O REINO → MEU NOME É REFERÊNCIA EM TODO O MAPA
```

Cada degrau muda seis coisas (DM §4): mecânica, economia, visual, som, social e comercial.

| Degrau | Mecânica | Visual / som | Renome (§14) |
|---|---|---|---|
| Sozinho | carregar tudo, 3 itens | oficina vazia, um martelo | Aprendiz |
| Um ajudante | 1º ajudante | 2º personagem andando | — |
| Oficina | linhas, esteira, níveis de estação, Quality | fornalha em overdrive, esteira viva | Ferreiro da rua |
| Rua | joalheria, Hall de obras-primas | fachada, pedestais, rua cheia | Mestre artesão |
| Distritos | Charter, managers, renda de fundo | mapa de distritos | Fornecedor do reino |
| Reino | Royal Contracts, Arsenal | estandartes reais, música "royal" | níveis 6–8 |
| Referência | Grand Forge, exportação, Masterworks lendárias | catedral de forja | níveis 9–10 |

**Personagem (DM §25):** roupa simples → avental → cinto → luvas → ferramenta → uniforme de mestre → visual de guilda/real. Três estágios visuais na Wave B (B-ART-05, ligados ao Renome 0, 3 e 4); o resto entre gameplay e cosmético.

**Ferramentas do jogador (DM §26), sem inventário RPG:** botas = velocidade (Botas, existe); luvas/carrinho = carga (Mochila, existe); martelo = golpe de mestre (Quality, §9); livro-caixa = vagas de Order (Renome 1 e 3, §10).

**Narrativa leve (DM §36–37):** oficina herdada, mercador mentor, guilda, capitão real, mestre artesão e um rival comercial. Sem cutscene longa; falas curtas em marcos (Charter, Masterwork, Renome). O rival aparece em marcos e eventos, sem PvP; a versão "rival na rua" é o FS-4 (§21).

---

## 5. Economia

Estado: ouro IMPLEMENTADO (long, Wave A) · carteira com IDs PROPOSTO (Wave B) · Gems Wave E · tickets A-CORE-06, B-*, E-PLAT-*.

### 5.1 Moedas e trilhas (V9 / P1)

| Recurso | Natureza | Gasta? | Entra | Na HUD |
|---|---|---|---|---|
| Ouro (`gold`) | moeda | sim | já existe | sempre |
| Reputation ("Renome") | trilha monotônica com níveis | **não** | B | selo de nível (não é saldo) |
| Blueprint | posse (desbloqueio) | **não é token** | B | Collection / cartão da receita |
| Pass XP | trilha da temporada | não | D | tela do Pass |
| Pontos de evento | trilha do evento | não | D | tela do evento |
| Gems (`gems`) | moeda premium | sim | **E**, só com mapa fonte → ralo no `BALANCE.md` | só quando relevante (DM §64) |
| Moeda de evento | moeda temporária | sim | decisão (P1 / D-CORE-03); no máximo 1 por vez | durante o evento |

A carteira guarda saldos por ID de moeda desde a Wave B (só `gold` ativo), para a Wave E entrar sem migrar save. Clareza para o jogador (DM §13): **Ouro = crescer · Gems = conveniência/cosmético · Blueprint = descobrir · Renome = quanto o reino te conhece.**

### 5.2 Ouro: fontes e ralos

| Fontes | Wave | Entra no `GoldEarned`? |
|---|---|---|
| Vendas no balcão (inclui VIP 3× e o multiplicador de qualidade) | já / B | sim |
| Encomendas (Orders 2.0) | já / B | não |
| Baús de marco | já | não |
| Cofre offline | já | não (é derivado da taxa) |
| Royal Contracts, diárias, eventos | D | não |
| Renda de fundo dos distritos com manager | C | não |

| Ralos | Wave |
|---|---|
| Estações e pads (construir), upgrades do menu | já |
| Luxo/decoração (sem efeito de produção) | já |
| Níveis de estação (qualidade + velocidade + aparência) | B |
| Contratar e subir nível de workers | B |
| Abrir e construir distritos 2–6; managers | C |
| Ralos de utilidade de evento | D |

**Regras de escala:** custos de cada distrito em "minutos de renda esperada" e prêmios em "segundos de taxa online" (`RateEma`), como as Encomendas já fazem (`BALANCE.md` §20). Upgrade produtivo deve se pagar em 8–12 min de renda (regra de retorno da fase 4, `BALANCE.md` §14). Sintomas que o econsim vigia (DM §107): inflação, zona morta, paywall, distrito sem fim, recurso transbordando.

**Problemas medidos que a economia nova tem de resolver** (`BALANCE.md` §22): zona morta de ~23 dos ~43 min até a produção completa (meta Wave B: ≤ 12 min); ~29 mil de ouro sem destino aos 60 min (meta Wave B: ≤ 10 mil); "Chamar VIP" sem recarga fecha a produção 44% mais cedo (resolvido por V11: recarga de 5 min).

### 5.3 Offline (regra vigente, `BALANCE.md` §5)

`ouro = min(RateEma × 0,25 × min(Δt, 7.200 s), 2 × menor upgrade travado)`; Δt ≤ 0 paga 0; cofre só abre com ≥ 60 s fora; cada resgate tem `claimId`; `SavedAt` nunca recua (`clock_back`). O **teto de 2 h é trava no núcleo, nunca knob**. Managers e premium mexem na **taxa**, não no tempo (recomendação da decisão P2). O offline nunca dá Gem, moeda de evento, Blueprint nem Masterwork.

### 5.4 Exploits que viram teste (DM §133)

Relógio, reabrir o app, offline duplicado, recompensa de anúncio duplicada, restore de IAP, reset de evento, encomenda duplicada. Os de ouro já são testes (A-CORE-02); cada sistema novo traz o seu teste negativo (ver `ECONOMIA_WAVE_B.md` §9).

---

## 6. Distritos 1–6

Estado: Old Forge IMPLEMENTADO (oficina + rua lateral) · 2–6 PROPOSTO · Wave C · tickets C-CORE-01..05, C-ART-13, C-UI-20.

Regra (DM §145): cada distrito tem identidade visual, família de produto, **uma mecânica nova**, major unlock, Orders próprios e Masterworks. Distrito que só muda números não entra. Cada distrito é um `DistrictDef` (uma instância de `Sim` por distrito + camada Império com carteira e Renome compartilhados).

| # | Distrito | Identidade | Família de produto | Mecânica nova | Orders próprios | Masterworks | Major unlock |
|---|---|---|---|---|---|---|---|
| 1 | **Old Forge** | oficina herdada; pedra, madeira, brasa laranja | espada, escudo, ferramenta; joia simples na rua lateral (teaser do tier 3) | cadeia física, gargalo, ajudantes, esteira, **Quality e golpe de mestre** | Common, Bulk, Quality, Rush, Mixed, Blueprint, Royal (Renome 2) | 10 (`ECONOMIA_WAVE_B.md` §6) | Charter → Armorer's Row |
| 2 | **Armorer's Row** | rua de armeiros; ferro escuro, couro, rebites | elmo, peitoral, grevas, manoplas, lança, machado de guerra, maça | **montagem** (assembly): peça de 2–3 componentes numa bancada de montagem; **liga** como material de qualidade | Bulk pesado ("equipar a milícia"), Quality com liga | 10 | carroça de couro do fornecedor; Fundidor (Smelter) |
| 3 | **Artisan Quarter** | bairro de ourives; ouro, cobre, vitrines, lanternas | colar, broche, cinzel (ferramenta fina), castiçal (ornamento), caixa de joias, cálice (utensílio), fivela (acessório) | **margem × volume**: bancada configurável escolhe o produto; clientes ricos com ticket alto, pouca gente e paciência longa | Mixed de 3 componentes, pedidos de clientes ricos | 10 | lapidação de gemas |
| 4 | **Royal Arsenal** | pátio militar; estandartes azul e ouro | kit da guarda, kit do batedor, alabarda cerimonial, espada de gala, armadura de gala, insígnia real | **Royal Contracts** com cotas e prazo-bônus; Renome real | Royal, Contract-linked | 10 | contratos reais permanentes |
| 5 | **Arcane Foundry** | fundição arcana; roxo, ciano, runas, cristal | lâmina rúnica, escudo rúnico, amuleto arcano, cajado de liga, anel de cristal, lanterna arcana, liga celeste | **energização/estabilidade**: a estação arcana tem um medidor determinístico; produzir acima do ritmo estável pausa a estação (decisão de ritmo, sem sorteio) | Quality Superior, Event "Arcane Week" | 10 | minério rúnico e cristal |
| 6 | **Grand Forge** | catedral de forja; todas as paletas | panóplia do campeão, lâmina estelar, coroa do reino, estátua de bronze, sino da catedral, relicário real | **síntese e exportação**: produtos que pedem componentes de outros distritos; o Mensageiro (Courier) leva lotes de exportação | elite contracts | 10 | endgame: ciclos de exportação e Masterworks lendárias |

**Distritos antigos não somem (DM §10):** depois do Charter, um manager assume, a renda vira fundo, o jogador pode voltar, eventos ainda acontecem e os upgrades finais ficam. A renda de fundo é sempre menor que a ativa (invariante testada, C-CORE-03).

**Pós-lançamento (DM §9):** Forge Street → Port City → Mountain Foundry → Royal Capital, reaproveitando os sistemas e trocando recursos, cadeia, layout, arte, clientes e eventos.

---

## 7. Produtos (tier 1–5) e clientes

Estado: 4 produtos IMPLEMENTADO · `ItemDef`/`RecipeDef` PROPOSTO · Wave B (dados) e C (conteúdo) · B-CORE-01, B-CORE-02.

**Meta da v1.0:** 35–45 produtos vendáveis (DM §7). A lista abaixo dá **38**; intermediários (minério, lingote, couro, placa, rebite, gema, liga, minério rúnico, cristal) não contam.

| Tier (DM §14) | Produtos | Distrito |
|---|---|---|
| 1 | espada, escudo, ferramenta, espada longa (via Blueprint) | 1 |
| 2 | elmo, peitoral, grevas, manoplas, lança, machado de guerra, maça | 2 |
| 3 | joia simples (já no distrito 1), colar, broche, cinzel, castiçal, caixa de joias, cálice, fivela | 1 e 3 |
| 4 | kit da guarda, kit do batedor, alabarda cerimonial, espada de gala, armadura de gala, insígnia real | 4 |
| 5 | lâmina rúnica, escudo rúnico, amuleto arcano, cajado de liga, anel de cristal, lanterna arcana, liga celeste | 5 |
| 5+ (síntese) | panóplia do campeão, lâmina estelar, coroa do reino, estátua de bronze, sino da catedral, relicário real | 6 |

Cada produto declara (DM §14): insumos, estações, tempo, preço, posição no rodízio de Orders (substitui o "order weight" do DM, que pressupõe sorteio), visual, desbloqueio, Blueprint e regras de qualidade. Esquema em `ECONOMIA_WAVE_B.md` §8.

**Clientes (DM §20, §57):** famílias viajante, guarda, aventureiro, mercador, nobre, emissário real, colecionador, VIP (e camponês no distrito 2+). Diferem em ticket, paciência, produto, comportamento e recompensa; a silhueta comunica a classe (o pedido não depende só do balão). Paciência vigente: base + k × tempo de produção (`BALANCE.md` §13). O cliente colecionador abre no Renome 2 e só leva Fina ou melhor.

**VIP (DM §21, preservar):** a cada 4–6 min por sequência de Weyl, pacote a 3× por unidade, paciência 90 s + 6 s por unidade (`BALANCE.md` §18.9). "Chamar VIP" por anúncio traz um VIP extra com pacote 2× até 20 e **recarga de 5 min** (V11). Apresentação nova na Wave B (B-ART-11): entrada especial, som, FX e pedido destacado.

---

## 8. Workers e managers

Estado: 6 ajudantes de papel fixo IMPLEMENTADO · `WorkerDef` com nível e especialidade PROPOSTO · Wave B (Old Forge) e C (papéis de distrito) · B-CORE-04, B-ART-09, C-CORE-03.

**Papéis (DM §22):**

| Papel | Faz | No jogo hoje | Entra |
|---|---|---|---|
| Carregador (Porter) | leva minério, lingote, produto | Ajudante 1, 2, 3, Mineiro, Joalheiro, Joalheiro 2 | já |
| Oficial (Smith) | opera uma família de estação: qualidade e um pouco de velocidade | — | B |
| Balconista (Clerk) | atende: paciência e ritmo de atendimento | — | B |
| Almoxarife (Quartermaster) | mais armazenamento nas saídas e no balcão | — | B |
| Mestre de obras (Foreman) | buff de área (qualidade e velocidade) | — | B |
| Fundidor (Smelter) | opera fornalha de liga | — | C (distrito 2) |
| Mensageiro (Courier) | entrega Orders e exportação | — | C (distrito 6) |
| Manager | assume o distrito depois do Charter | — | C |

**Progressão (DM §23):** nível (1–10; 1–5 no Old Forge), velocidade, capacidade, especialidade (espadas, escudos, ferramentas, joias, armaduras, carga, atendimento) e cosmético. No máximo 1–2 traços fortes por worker. **Workers são personagens fixos nomeados em dados**: o jogador escolhe quem contratar; nenhum traço é sorteado. Meta da v1.0: 20–30 workers funcionais (DM §7). Números iniciais no `ECONOMIA_WAVE_B.md` §2.

**Managers (DM §24):** entram com o Charter, automatizam a coleta e geram renda de fundo; o manager sobe a **taxa** do distrito em fundo e do offline, nunca o teto de 2 h (P2). Não elimina o motivo de revisita: eventos, Masterworks e contratos do distrito continuam pedindo o jogador.

---

## 9. Quality

Estado: PROPOSTO · Wave B · B-CORE-03 · decisão P10 (fórmula). Números: `ECONOMIA_WAVE_B.md` §1.

Quatro níveis (DM §15): **Normal · Fina · Superior · Masterwork**.

- **Sem RNG; regra explicável.** Cada estação de fabricar soma "pontos de qualidade" `q` das fontes que o DM lista: nível da estação, worker (oficial da especialidade), Blueprint (técnica da família), material (liga, distrito 2+) e buff (mestre de obras, evento). A parte inteira de `q` é o nível garantido; a parte fracionária enche um **acumulador por estação**: quando ele passa de 1, aquela peça sai um nível acima. Com `q = 0,25`, exatamente 1 peça em cada 4 sai Fina. A UI mostra a barra da estação ("próxima Fina em 2 peças") e o `QualityBreakdown()` lista cada fonte.
- **Preço:** Normal 1×, Fina 1,5×, Superior 2,5× (no balcão e no VIP). Masterwork **não é vendida**.
- **Golpe de mestre (FS-2 reconciliado):** só o ferreiro, parado na boca da bigorna/bancada com o insumo, acerta um anel de 0,8 s e sobe a peça **um nível**. Ajudantes, managers e offline nunca fazem. Recarga de 20 s (knob). Errar não pune.
- **Masterwork:** é o nível acima de Superior e **só a mão do ferreiro chega lá** — golpe de mestre numa peça que já sairia Superior, com o projeto (Blueprint) da Masterwork e as condições dela cumpridas. A peça vai para o **Hall** (§13), não para o balcão. Sem as condições, o golpe em Superior fica Superior (vendida a 2,5×).
- **Automação chega no máximo a Superior.** Peças carregam o nível na pilha, nos ajudantes e no estoque; o balcão vende a melhor primeiro.
- **Save antigo:** tudo Normal, acumuladores em zero.

---

## 10. Orders 2.0

Estado: Encomendas (1 tipo, 1 vaga) IMPLEMENTADO · 8 tipos PROPOSTO · Wave B · B-CORE-05. Números: `ECONOMIA_WAVE_B.md` §3.

| Tipo (DM §18) | O que pede | Prêmio | Abre |
|---|---|---|---|
| Common | N de um produto (= a encomenda atual) | ouro | sempre |
| Bulk | quantidade grande | mais ouro e Renome | 2 linhas e 5 entregues |
| Quality | N de qualidade mínima (Fina; Superior no Renome 3) | ouro alto, Renome | 1ª fonte de qualidade |
| Rush | N com prazo | prazo é **só bônus**: no prazo, prêmio dobrado; fora, prêmio normal | 8 entregues |
| Mixed | 2–3 produtos | ouro, Renome | 3 linhas |
| Royal | quantidade + qualidade Fina | ouro alto e muito Renome; projeto de Masterwork real | Renome 2 |
| Blueprint | N de um produto | um Blueprint (o próximo pendente da lista do distrito) | Renome 1 |
| Event | definido pelo `EventDef` | pontos de evento | evento ativo (Wave D) |

**Regras:**
- **Rodízio determinístico, sem sorteio:** cada distrito tem um ciclo fixo de tipos (Old Forge: `Common, Common, Bulk, Quality, Common, Mixed, Rush, Blueprint, Common, Royal`); tipo ainda travado vira Common. O produto segue o rodízio das linhas abertas, como hoje.
- **Nunca falha, nunca pune.** Sem prazo de falha (B-CORE-05: Rush só com bônus).
- **Vagas:** 1 no começo, 2 no Renome 1, 3 no Renome 3 (o "livro-caixa" do DM §26). Cada unidade vendida conta para no máximo uma encomenda.
- **Prêmio** em segundos de taxa online, fora do `GoldEarned`, com ID de transação.
- **Meta da v1.0:** 50+ `OrderDef` (DM §7). FS-3 (fregueses com nome) entra como encomenda com `clientId`, não como 9º tipo.
- Rewarded opcionais: "bônus da encomenda" e "trocar encomenda" (mostra a próxima do rodízio antes de assistir) — §17.

---

## 11. Royal Contracts e Royal Charter

Estado: PROPOSTO · Charter Wave C (C-CORE-03) · Contracts Wave D (D-CORE-04).

**Royal Contract (DM §19):** objetivo de médio prazo, em paralelo ao loop. Ex.: "Equipar 20 guardas" = 20 espadas + 20 escudos + 20 elmos. Entregar desvia produto do balcão: a decisão é vender agora ou cumprir o contrato.
- Recompensas: ouro, **Blueprint (desbloqueio, não token, V9)**, Renome e, se houver baú, **conteúdo fixo mostrado antes**. A "cosmetic chance/controlada" do DM §19 sai: nada de chance (P5).
- Prazo só como bônus; contrato nunca bloqueia o core loop (o bot fecha todos os distritos sem nenhum contrato).
- Meta da v1.0: 12 Royal Contracts (DM §7). Recompensa 1×, com ID de transação.

**Royal Charter (DM §11):** a "formatura" do distrito, **sem reset** (V5).
- Condições do Old Forge (PROPOSTO): produção completa + Renome 4 + 1 Masterwork registrada. Distritos seguintes: o mesmo padrão com o nível de Renome do distrito; quando os Royal Contracts existirem (Wave D), os Charters do distrito 4 em diante pedem também 1 contrato.
- Concede: libera o próximo distrito, +100 de Renome, insígnia, manager e renda de fundo do distrito, fala de marco. Um Charter por distrito, 1×.
- A troca de distrito é o momento natural de interstitial **se** a flag estiver ligada (§17).

---

## 12. Blueprints

Estado: PROPOSTO · Wave B · B-CORE-07 · V9 (Blueprint é desbloqueio). Lista inicial: `ECONOMIA_WAVE_B.md` §4.

- **Blueprint é posse, não token.** Ter o Blueprint libera uma coisa específica e permanente: receita (produto ou variante), nível de estação, técnica de qualidade, projeto de Masterwork, especialização ou variante cosmética. Não existe saldo nem troca.
- **Fontes (DM §17):** gameplay (marcos de produção), Orders (tipo Blueprint e Royal), Royal Contracts, eventos, Pass, achievements e, só na Wave E, loja como **aceleração**.
- **Regra "receita essencial nunca só paga"** (DM §17; regra do validator): todo Blueprint marcado essencial (o que trava progressão de distrito, Charter, Masterwork exigida ou contrato) tem pelo menos uma fonte grátis alcançável; loja e trilha premium do Pass nunca são a única fonte. O validator falha o build se isso quebrar.
- **Sem sorteio:** a próxima recompensa de Blueprint é a próxima pendente da lista ordenada do distrito, mostrada no cartão da encomenda.
- **Meta da v1.0:** 60+ Blueprints (DM §7).

---

## 13. Masterworks e Collection

Estado: PROPOSTO · Wave B · B-CORE-08 · P10. Lista e marcos: `ECONOMIA_WAVE_B.md` §6–§7.

**Masterworks (DM §16):** peças de coleção com aparência exclusiva, registro no Hall, condição especial, nunca vendidas.
- Como sair uma: golpe de mestre numa peça que já sairia Superior + Blueprint da Masterwork + condição especial (contagem vendida, nível de estação, encomendas de um tipo, nível de oficial, Renome). Nada pago, nada de anúncio como condição.
- **Hall = Galeria (FS-6):** pedestais físicos no distrito (crescimento visível e filmável) e a aba Masterworks do livro. Cada Masterwork entra 1×; a segunda vez sob as mesmas condições sai Superior e é vendida.
- Recompensas: Renome, cosmético (pedestal, estandarte, martelo do ferreiro), achievement e marcos de coleção. Família completa no distrito: +5% fixo no preço daquela família (FS-6).
- Meta da v1.0: 60 Masterworks, 10 por distrito (DM §7). Exemplos do DM: King's Sunblade, Shield of the First Guard, Ember Crown — todas no Old Forge.

**Collection Book (DM §34):** dá valor de coleção ao que já foi produzido.
- Abas: produtos (produto × qualidade), Masterworks, workers, clientes, distritos, Blueprints.
- **Derivada dos contadores** (vendas por item e qualidade, contratações, posses): o save só guarda os marcos já resgatados, com ID de transação.
- Marcos dão Renome e cosméticos (DM §34), nunca ouro direto. Abre no 1º Blueprint de Masterwork (~100 espadas), como preview do D0 (DM §82).

---

## 14. Reputation ("Renome")

Estado: PROPOSTO · Wave B · B-CORE-06. Limiares: `ECONOMIA_WAVE_B.md` §5.

- **Não é moeda** (DM §35, V9): monotônica, nunca gasta, nunca cai. Existe um total e níveis com nome.
- **Fontes:** Orders (por tipo), Royal Contracts, qualidade (peças Superiores vendidas), Masterworks, marcos de coleção, baús de marco, Charter e eventos.
- **Libera (DM §35):** clientes (colecionador), vagas de Order, tipos de Order (Blueprint, Royal), workers (almoxarife, mestre de obras), cosméticos e estágios visuais do herói, falas de história, contratos e o Charter de cada distrito.
- **Níveis:** 0 Aprendiz · 1 Ferreiro da rua · 2 Oficina conhecida · 3 Mestre artesão · 4 Fornecedor do reino (Charter do Old Forge) · 5–10 nos distritos 2–6.
- **Save antigo (0.6):** entra com Renome = encomendas entregues + 5 × baús abertos (retroativo determinístico).
- Na HUD aparece como selo de nível, nunca como saldo ao lado do ouro (DM §64).

---

## 15. Metas diárias, semanais e achievements

Estado: baús de marco IMPLEMENTADO (embrião) · resto PROPOSTO · Wave D · D-CORE-01/02, D-PROD-03.

- **Daily (DM §30):** 3 missões curtas por dia sobre o fluxo de eventos `Ev` (vender 20 produtos, concluir 2 Orders, melhorar estação, coletar o cofre, chamar VIP — esta última sempre com alternativa sem anúncio no mesmo dia). Recompensa até a Wave E: ouro (segundos de taxa), Pass XP e Renome; Gems pequenas só depois da Wave E.
- **Weekly (DM §31):** concluir Orders, produzir famílias, cumprir Contracts, participar de evento.
- **Achievements (DM §32):** crafting, workers, distritos, Orders, Masterworks, automação, economia. Recompensas: Blueprint, insígnia, cosmético, Pass XP (Gems só depois da Wave E).
- **Login de 7 dias (DM §135):** ciclo de 7 dias, **reset não punitivo** (perder um dia não volta ao dia 1), catch-up leve.
- **Volume da v1.0** (D-PROD-03): ≥ 30 modelos de daily, ≥ 12 de weekly, ≥ 60 achievements, cada um ligado a um evento da taxonomia (DM §86).
- **Calendário e ledger** (D-CORE-01): o núcleo recebe `nowUnix`; nenhuma recompensa duplica em save/load, reabertura ou relógio para trás. Trapaça local de relógio aceita até haver hora de servidor (decisão de D-CORE-01).

---

## 16. LiveOps e Pass

Estado: PROPOSTO · Wave D (sistemas) e E (compra do Pass) · D-CORE-03/04, D-PROD-01/02.

**Evento em dados (DM §28):** `EventDef` com id, início/fim, requisitos, missões, recompensas, moeda (ou pontos), visual, modificadores (sobre o `Tuning`) e ofertas. Nunca hardcode de evento no fluxo central; **evento novo entra só com dados** (aceite da Wave D).

**Templates (DM §29), 8:** Forge Frenzy, Royal Week, Rare Ore Rush, Blacksmith Festival, Masterwork Hunt, Merchant Caravan, Armorer's Challenge, Arcane Week.

**Calendário (DM §121–123, D-PROD-01):** 8 semanas pré-planejadas + 2 reservas antes do Global.
- Season 1 — Royal Commission: Royal Week, Forge Frenzy, Armorer's Challenge, Masterwork Hunt "King's Sunblade".
- Season 2 — Ember Festival: Blacksmith Festival, Rare Ore Rush, Merchant Caravan, Masterwork Hunt "Ember Crown".
- Reservas: Forge Frenzy-B e Royal Week-B, rodando só com o distrito 1.

**Moeda de evento:** até decisão (P1 / D-CORE-03), eventos usam **pontos de evento** (trilha não gastável com recompensas por limiar). Se a moeda de evento entrar, é uma por vez (DM §12) e nunca vem do offline.

**Masterwork Pass (DM §33):** temporada de 28–35 dias, trilha free e premium; XP de daily, weekly, Orders e eventos. Premium: cosméticos, decorações, conveniência e (depois da Wave E) Gems. **Nada obrigatório de progressão só no premium** (regra do validator). O bot F2P fecha a trilha free (D-PROD-02). A compra do premium é IAP da Wave E.

---

## 17. Monetização

Estado: 2 rewarded IMPLEMENTADO (LevelPlay 9.5.1; conta aguardando aprovação) · resto PROPOSTO · Wave E · E-PLAT-01..07, E-UI-21.

**Princípio (DM §39):** pagar acelera, amplia opções ou melhora a aparência; nunca torna a experiência grátis deliberadamente ruim. Kill criterion herdado do v1 §26: **rewarded necessário para a progressão normal = cancelar**. O bot F2P, sem anúncio, fecha todo o conteúdo.

**Pré-requisito de toda monetização comercial:** tela de idade + consentimento antes de iniciar qualquer SDK (E-PLAT-01); menor de 18 sem anúncio personalizado; nada de consentimento fixo no release (DM §94).

### 17.1 Rewarded (DM §40), tudo por RemoteConfig

| Placement | O que dá | Recarga / teto (knob) | Estado |
|---|---|---|---|
| Chamar VIP | VIP extra, pacote 2× até 20 | 5 min (V11) | IMPLEMENTADO; recarga na Leva 5 |
| Velocidade ("Forge Rush") | 2× por 60 s; 2º anúncio sobe a 3× | 5 min contados do fim | IMPLEMENTADO |
| Cofre 2× | dobra aquele resgate, uma vez | 1 por resgate | PROPOSTO (D5) |
| Bônus da encomenda | +50% do ouro da encomenda ativa (não Renome, não Blueprint) | 1 por encomenda | PROPOSTO |
| Trocar encomenda | pula para a próxima do rodízio, **mostrada antes** de assistir | 1 por encomenda | PROPOSTO (substitui o "Blueprint Reroll", P5) |
| Supply Drop | entrega fixa de lingotes (60 s de produção da fornalha), mostrada antes | 10 min | PROPOSTO |
| Bônus de evento | pontos de evento extras, valor mostrado antes | por evento | Wave D |

Teto diário total por knob; botões de anúncio só depois da 1ª melhoria (decisão D6 do `BACKLOG_V07.md`). Recompensa entregue 1× (E-PLAT-02); recompensa tardia logada.

### 17.2 Interstitial (DM §41, V10)

Implementado atrás de **feature flag desligada**. Só liga depois de A/B no Limited Release. Regras quando ligado: nunca no tutorial, nunca durante ação, só no estado `Transition` (troca de distrito, volta ao mapa, depois de sequência de marcos), cap por sessão e cooldown, removido pela compra "Remove Ads". Enquanto a flag está desligada a loja **não promete** "sem anúncio forçado" (V10). Banner: nenhum.

### 17.3 IAP (DM §42–47), Wave E

Sem moeda premium antes da Wave E (V9). Tudo compra direta com conteúdo fixo e visível; sem lootbox (DM §45).

| Produto | Conteúdo | Nota |
|---|---|---|
| Remove Ads **ou** Pacote do Mestre | se o interstitial ligar: remove interstitial (rewarded continua opcional). Se continuar desligado: Pacote do Mestre = cofre 2× permanente + skin de oficina | decisão P3 depois do A/B; FS-5 |
| Starter Smith Pack | 1ª compra: Gems, cosmético, boost; alto valor percebido | aparece depois da 1ª automação, nunca no 1º minuto (DM §47) |
| Gem Packs | faixas de preço | Gems com mapa fonte → ralo antes |
| Master Blacksmith Bundle | skin, Gems, consumíveis | — |
| District Bundle | oferta contextual ao abrir distrito | offer engine (DM §46) |
| Event Bundle | durante eventos | — |
| Masterwork Pass | trilha premium da temporada | §16 |

Faixas de preço de referência do mercado (não decididas): No Ads US$ 3,99–9,99 e passes US$ 8,99–9,99 nos similares (`BENCHMARK_MERCADO.md` §2, §8); "Pacote do Mestre" proposto a US$ 2,99–4,99 (`COMPETITIVO.md` FS-5).

**Payer ladder (DM §45):** low (Starter, Remove Ads/Pacote do Mestre), regular (Pass, bundles), high (Gem packs, Master Blacksmith). **Gems (Wave E):** fontes = compra, achievements, Pass, pequena distribuição grátis; ralos = cosméticos, conveniência, bundles, utilidade de evento e "trocar" com a próxima opção visível. **Assinatura:** fora da v1.0 (DM §43). **Shop (DM §44):** Featured, Pass, Gems, Cosmetics, Bundles; não abre sozinha toda sessão; sem dark pattern. **Cosméticos (DM §48–49):** aparecem de verdade no jogo; slots simples de decoração.

**Segurança (DM §93, E-PLAT-04):** recibo, restore, entitlements re-derivados do recibo, compra pendente confirmada só depois do save gravado.

---

## 18. Arte e áudio (direção)

Estado: sprites pré-renderizados IMPLEMENTADO (V3); fornalha em 6 estados e HUD numa paleta (Wave A); resto PROPOSTO · arte contínua desde a Wave A, fechamento na Wave F · Art Bible: [`ART_BIBLE.md`](ART_BIBLE.md); proveniência: [`PROVENIENCIA.md`](PROVENIENCIA.md).

**Direção visual (DM §51):** *Stylized Medieval Crafting Diorama* — formas robustas, silhuetas grandes, materiais limpos, cores quentes, luz de forja, personagens expressivos, objetos exagerados, VFX fortes. Premium, divertido, reconhecível em 9:16. Nenhum asset é protegido por já existir (DM §50).
- **Pipeline (V3, DM §58, §74):** modelo 3D → rig → animação → preset de render → folha de sprites → atlas → Unity → QA visual → proveniência. Crédito de IA (Tripo etc.) e download só com aval.
- **Paleta (DM §52):** madeira, pedra, ferro, couro, cobre; laranja/amarelo = calor, verde = sucesso, azul = informação, vermelho = gargalo. Cor nunca é o único sinal (estado é forma também).
- **Estações:** fornalha com 6 estados legíveis sem texto (fria, aquecendo, ativa, sem minério, saída cheia, overdrive; DM §54); bigorna com martelo, faísca, impacto, som, progresso e pop (DM §55); esteira como "antes/depois" vivo (DM §56). **Nível de estação muda a aparência** (Wave B).
- **Qualidade legível:** Fina = brilho prata; Superior = brilho dourado; Masterwork = faísca dourada + câmera tremendo + pedestal.
- **Personagens (DM §57–60):** animações mínimas (jogador e worker: idle, walk, carry, interact/work, celebrate; cliente: walk, wait, happy, frustrated, leave; especiais: hammer, smelt, counter); silhueta por papel de worker e por família de cliente.
- **VFX (DM §61–62):** faísca, brasa, calor, impacto; moedas; flash, poeira de obra, feixe de nível; revelação de Blueprint e brilho de Masterwork; pacote de tema por evento. Tudo em pool com teto por perfil. "Juice" = visual + som + vibração + movimento em upgrade, contratação, distrito, Masterwork, VIP, contrato e recompensa do Pass.
- **Códigos distintivos:** bandana vermelha do ferreiro, faísca dourada da obra-prima, "travada" vermelha e "fome" cinza, anel de ouro dos pads, heráldica bigorna + chama.
- **Gates:** Visual Robustness Gate (DM §73, 8 perguntas por tela) e No Placeholder Gate (DM §178) no release.
- **Marca (DM §179–180):** logo de metal/madeira com bigorna/martelo, legível pequeno; tipografia OFL (A-UI-15); 4 variantes de ícone para teste (DM §77).

**Áudio (DM §70–72):** a oficina "respira": camadas de fogo, metal, martelo, esteira, moedas, multidão, carroça e ambiente, que **crescem junto com o negócio**. Músicas: main, workshop, royal, event, arcane (medieval estilizado, sem cansar). Mixer Music/SFX/Ambience (A-AUD-23); nenhuma senoide sintetizada no release. Vibração só em eventos importantes (upgrade, Masterwork, desbloqueio, VIP, venda grande), nunca em toda venda.

---

## 19. UX e HUD

Estado: menu inferior, dicas e cartão de encomenda IMPLEMENTADO · redesign PROPOSTO (A-UI-15..19, C-UI-20, E-UI-21).

- **HUD (DM §64):** ouro; Gems só quando relevante (Wave E); Order ativa (até 3 cartões compactos); objetivo contextual; menu. Renome aparece como selo de nível na tela do distrito/perfil e num aviso ao subir, nunca como saldo. Nada de dez moedas na tela.
- **Uma mão, retrato, hierarquia clara** (DM §156); a UI não parece painel de administração. Avisos numa fila fora da área de jogo (A-UI-19).
- **Cartão de upgrade (DM §65):** nome, efeito, custo, ícone e benefício (ex.: "Fole Reforçado · +25% velocidade da fornalha · 500"). Nível de estação mostra "+0,15 qualidade · 1 em cada 7 sai Fina".
- **Estação:** barra de qualidade (acumulador) e o `QualityBreakdown()` ao tocar; anel do golpe de mestre num lugar fixo, só com o ferreiro parado na boca (não briga com o joystick).
- **Telas novas:** mapa de distritos (progresso, renda, evento, próximo unlock, Masterworks; DM §66), tela de distrito (DM §67), Collection Book e Hall, Shop (cartões claros, sem 15 popups; DM §68), Pass (free/premium, prévia, tema, XP; DM §69).
- **Revelação progressiva:** Orders aos 0:45; Quality e níveis de estação depois das Ferramentas (~11 min); selo de Renome no nível 1 (~15–25 min); Collection no 1º projeto de Masterwork; mapa de distritos no Charter. Não explicar sistema futuro cedo (DM §81).
- **Volta do jogador (DM §136 + FS-7):** um cartão só — boas-vindas, cofre que conta (o que cada linha rendeu e qual estação travou, com "ver"), evento ativo e a Order. Nunca cinco popups.
- **Estados de jogo formais (DM §128):** Boot, Loading, Playing, Modal, Ad, Paused, Transition (A-PLAT-07); o Voltar do Android funciona em todos.
- **Acessibilidade (DM §110–111):** volume de música e efeitos, vibração liga/desliga, cor + forma, reduzir efeitos, texto legível, modo bateria; perfis Battery/Balanced/High sem tirar informação de jogo.

---

## 20. Métricas e KPIs

Estado: diário CSV local IMPLEMENTADO (12 dos 31 eventos do DM §86) · analytics remoto PROPOSTO · A-PLAT-06/09, G-UA-05 · decisões P6 e P8.

**Taxonomia (DM §86):** os 31 eventos (`session_start` … `event_complete`) por `IAnalytics`, só depois do consentimento; contexto de desempenho (DM §87: versão, build, modelo, SO, classe de memória, faixa de fps, duração, crash), sem dado pessoal desnecessário. Ad revenue por impressão (DM §95). Todo A/B com id, variante, snapshot do config, início, fim e métricas (DM §89).

**Retenção (decisão P6 pendente):**

| Nível | D1 | D3 | D7 | D30 | Uso |
|---|---|---|---|---|---|
| Continuar (kill criteria, v1 §15/§26) | ≥ 26% | ≥ 14% | ≥ 5% | ≥ 2% | decide continuar / iterar / cancelar |
| Meta interna base (DM §115) | ≥ 28% | ≥ 14% | ≥ 7% | ≥ 2% | meta do time |
| Meta agressiva (DM §115) | ≥ 32% | — | ≥ 9% | ≥ 3% | aspiração |

Leitura: coorte de 1,5–2k installs pagos, pago × pago; cancelar só depois de 2 iterações; pagante só a partir de 5k installs (v1 §15). Referência de mercado citada no v1: GameAnalytics 2026, D1 mediano ~20% e top-25% ~30%; D7 mediano < 4% e top-25% 6–7%.

**Portões de jogo (não mudam):** tutorial ≥ 90%; 1ª venda < 90 s para ≥ 90%; rewarded opt-in ≥ 45%; 2–5 rewarded/DAU sem queda de retenção; 2+ sessões/dia nos retidos; andar sem decisão < 50%; crash-free > 99,5%.

**Outros grupos (DM §116–118, §139–141):**
- Monetização: opt-in, completion, ads/DAU, ad ARPDAU, payer conversion (≥ 1%), ARPPU, IAP ARPDAU, starter e pass conversion, blended ARPDAU.
- Engajamento e meta: Orders/sessão, upgrades/sessão, progresso de distrito, participação em evento, uso da coleção, tempo entre transformações visíveis, Royal Contracts, progressão do Pass.
- Gameplay: `first_sale`, ritmo de upgrade, densidade de decisão, `walk_no_decision`, `client_left`, conclusão de distrito; % de vendas Fina ou melhor; golpes de mestre/sessão.
- Qualidade técnica: crash-free, ANR-free, tempo de carga, fps, falha de save, falha de compra.
- UA: hook rate, CTR, IPM, CPI (≤ US$ 0,70 promissor; 0,70–1,10 iterar; > 1,10 exige LTV maior — v1 §15), conversão da loja, ROAS D7/D30, fadiga de criativo.
- Diferenciais: a métrica de cada FS em `COMPETITIVO.md` d (ex.: FS-1 = clientes perdidos −50% no bot lento).

---

## 21. Diferenciais FS-1…FS-7 reconciliados com o Documento Mestre

Fonte dos diferenciais: `COMPETITIVO.md` d–e (todos HIPÓTESE até bot, Closed e teste de criativos).

| # | Diferencial | Ponto de atrito com o DM | Reconciliação proposta | Wave / estado |
|---|---|---|---|---|
| FS-1 | Demanda que respira (chegada acompanha a vazão nos ~15 min iniciais) | nenhum; serve à meta "1ª venda < 90 s" e ao bot Slow (DM §81, §106) | Knob do `Tuning` atrás de flag (A-CORE-03). Invariantes: nunca aumenta a chegada, nunca dá ouro, desliga sozinha. Prova: bot Slow com clientes perdidos −50% sem o ouro/min do F2P subir > 5% | B · PROPOSTO |
| FS-2 | Golpe de mestre: obra-prima só do ferreiro, "vendida a 3×" | DM §16: Masterwork **não é vendida**, vai para o Hall | **P10:** o golpe sobe a peça 1 nível (Normal→Fina→Superior, vendidas a 1,5×/2,5×); **só o golpe numa peça que já sairia Superior, com o projeto e as condições, chega ao tier Masterwork**, que vai para o Hall. A "obra-prima a 3×" vira "peça Superior a 2,5×" no dia a dia e "a obra-prima entra no Hall" no marco. Criativo #11 / FS-C-28 troca o fecho. Invariantes: ajudante, manager e offline nunca; ≤ 8% da receita no bot; recarga 20 s | B · PROPOSTO (P10) |
| FS-3 | Clientela fiel: 5 fregueses com nome e 5 corações | DM §20 (famílias), §35 (Renome libera clientes), §18 (8 tipos de Order) | Freguês = `ClientDef` nomeado dentro de uma família (mago, elfa, goblin, cavaleiro, nobre; arte existente). "Encomenda pessoal" = `OrderDef` Common/Quality/Bulk com `clientId`, não um 9º tipo. Corações = afinidade derivada das entregas daquele freguês (trilha, não moeda). A marca na rua (+5% fixo na linha) é recompensa da aba Clientes. Visitas no rodízio, sem sorteio | B/C · PROPOSTO |
| FS-4 | Ferreiro rival na rua, pilotado pelo `Bot.cs`; vencer = comprar a forja dele | DM §37 (rival em marcos/eventos, sem PvP) e §11 (Charter abre o distrito) | O rival é a **camada narrativa do Charter do Old Forge**: superar o rival vira uma das condições do Charter, e "comprar a forja dele" é a cena do major unlock de Armorer's Row. Ele só leva cliente que você não atendeu; nunca rouba ouro. Entra só depois do A/B no bot (≤ 15% dos clientes de quem joga bem, ≤ 35% do Slow) | C · PROPOSTO |
| FS-5 | "Anúncio só quando você pede": zero interstitial e banner para sempre | DM §41 autoriza interstitial; V10 manda implementar atrás de flag desligada | O jogo **cumpre** a promessa enquanto a flag está desligada; a loja **não promete** até o A/B do Limited Release (V10). Banner: nenhum. Se a flag ficar desligada de vez, "Remove Ads" vira "Pacote do Mestre" (cofre 2× permanente + skin; sobe a taxa, não o teto, coerente com P2) | E · APROVADO em parte (V10); final = P3 |
| FS-6 | Galeria de obras-primas: 12 pedestais, +5% por linha completa | DM §16 (Hall) e §34 (Collection) | A Galeria **é** o Hall: pedestais físicos no distrito + aba Masterworks. 10 por distrito (60 na v1.0, DM §7) no lugar de 12. O +5% por família completa vira marco de coleção, fixo e sem sorteio | B · PROPOSTO |
| FS-7 | Cofre que conta (o que cada linha rendeu, qual estação travou, "ver") | DM §136 (volta: boas-vindas, offline, resumo, evento, Order; sem 5 popups) | O cofre que conta **é** o "resumo" do DM §136, num cartão só com a Order e o evento. Pagamento inalterado (25% × até 2 h, `claimId`) | B · PROPOSTO |

**Ordem sugerida:** FS-1 e FS-2 (com a Quality) na Wave B; FS-7 junto do redesign da volta; FS-6 com os Masterworks; FS-3 depois do `ClientDef`; FS-4 com o Charter na Wave C; FS-5 decidido pelo A/B do Limited Release.

---

## 22. Decisões abertas de que este GDD depende

As decisões estão no `PROJETO.md` §5; aqui só o que trava design:

| # | Decisão | Recomendação deste GDD | Trava |
|---|---|---|---|
| P10 | Golpe de mestre × Masterwork não vendida | §9 e §21 FS-2: Masterwork = Superior + golpe + projeto, vai para o Hall | B-CORE-03, B-CORE-08 |
| P2 | Teto offline × premium/assinatura | trava de 2 h; manager e premium mexem na taxa | C-CORE-03, E |
| P5 | Rerolls, "chest", "cosmetic chance" | baú fixo e visível; trocar mostra a próxima antes; sem "chance" | B (Orders), D, E |
| P6 | Metas de retenção | três níveis do §20 | dashboards |
| P1 (resto) | Moeda de evento | pontos de evento até decidir | D-CORE-03 |
| — | Fórmula e números da Wave B | os do `ECONOMIA_WAVE_B.md`, validados pelo bot antes de aprovar | B-CORE-03..08 |
| — | FS-3 e FS-4 entram? | FS-3 na B/C com arte existente; FS-4 só depois do A/B no bot | B, C |

---

## 23. Definição de pronto (100%)

Forge Street está **completo** (DM §164) quando os 23 itens abaixo cumprem o critério — a mesma lista do `PROJETO.md` §8, onde fica o placar em %. "Pronto" = pronto para Release Candidate (Wave H), não "começado". Completo não significa "nunca mais atualizar": o live game continua. Além dos 23, o RC exige Feature Complete (DM §163) e `POCO_RELEASE_GATE = PASS` (DM §151).

| # | Item (§164) | Critério verificável de 100% |
|---|---|---|
| 1 | **Core polido** | `verify.sh full` verde; 0 known issue P0/P1 aberto no `PROJETO.md` §7; núcleo determinístico com passo fixo (mesma entrada de toques → mesma partida, teste de replay); 1.000 partidas do econsim por perfil sem deadlock; 1ª venda < 90 s em ≥ 90% e andar sem decisão < 50% no diário do Closed |
| 2 | **6 distritos** | 6 `DistrictDef` aprovados pelo validator; cada um com kit visual próprio, família de produto, 1 mecânica nova com teste no `coretests`, Orders próprios, 10 Masterworks e Charter; bot F2P conclui os 6 sem zona morta maior que a meta do econsim; `grep` por `district ==` no código = 0 |
| 3 | **35–45 produtos** | entre 35 e 45 `ItemDef` vendáveis, cada um com `RecipeDef`, preço, ícone, texto em pt-BR/en/es e chave de analytics; validator verde; o bot vende cada um pelo menos 1× |
| 4 | **Workers** | 20–30 `WorkerDef` com papel, nível, especialidade (≤ 2 traços) e cosmético; os 8 papéis do §8 com teste no núcleo; teste de silhueta ≥ 5/7 (B-ART-09); nenhum traço sorteado |
| 5 | **Quality** | 4 níveis em dados; teste de determinismo (q = 0,25 → exatamente 1 Fina a cada 4); `QualityBreakdown()` explica 100% das peças; Masterwork só pelo golpe do ferreiro (teste negativo com ajudante e offline); prêmio de qualidade e golpe dentro das faixas do `BALANCE.md` |
| 6 | **Orders** | 8 tipos em dados, 1 teste por tipo; ≥ 50 `OrderDef`; rodízio sem RNG (teste negativo); Rush sem penalidade; 1 unidade conta para 1 encomenda; prêmio com ID de transação, fora do `GoldEarned` |
| 7 | **Royal Contracts** | 12 contratos em dados; bot fecha todos os distritos sem cumprir nenhum (não bloqueiam o core); recompensa 1× com ID de transação; baú só de conteúdo fixo |
| 8 | **Blueprints** | ≥ 60 `BlueprintDef`; regra do validator "essencial nunca só paga" com teste negativo; todo essencial tem fonte grátis alcançável no bot F2P; nenhum saldo de Blueprint no save (é posse) |
| 9 | **Masterworks** | 60 `MasterworkDef` (10 por distrito); bot Perfect registra todas; nenhuma vendida nem no `GoldEarned`; todas visíveis no Hall; nenhuma condição exige anúncio ou compra (validator) |
| 10 | **Collection** | 6 abas derivadas dos contadores; marcos 1× com ID de transação; save antigo recupera a coleção pelos contadores; ≥ 30% abrem a coleção sem dica no Closed (métrica do FS-6) |
| 11 | **Reputation** | monotônica (teste: nenhum caminho reduz); sem API de gasto; 10 níveis em dados; cada nível libera algo visível; save antigo recebe o retroativo |
| 12 | **Daily / Weekly / Achievements** | ≥ 30 / ≥ 12 / ≥ 60 modelos, cada um ligado a um evento do DM §86; login de 7 dias não punitivo; nenhuma recompensa duplica em save/load, reabertura ou relógio para trás (testes) |
| 13 | **Events (LiveOps)** | 8 templates; um evento novo entra com diff só de dados (prova num PR); calendário de 8 semanas + 2 reservas; `event_enter`/`event_complete` na telemetria; reset de evento não duplica recompensa |
| 14 | **Pass** | temporada configurável de 28–35 dias em dados; bot F2P fecha a trilha free; validator: nada obrigatório só no premium; compra do premium com restore |
| 15 | **Ads** | consentimento e idade antes do SDK (E-PLAT-01); placements por RemoteConfig com recarga no núcleo (VIP 5 min, Velocidade 5 min); interstitial atrás de flag com os 5 vetos testados; recompensa 1×; receita por impressão no analytics |
| 16 | **IAP** | Unity IAP com restore em instalação limpa; compra pendente confirmada só depois do save; mocks de compra interrompida, duplicada e restaurada verdes; entitlements do recibo; nenhum item aleatório pago (teste/validator) |
| 17 | **Analytics** | os 31 eventos do DM §86 por `IAnalytics`, só depois do consentimento; dicionário de analytics publicado; dashboards de retenção, monetização e UA (G-UA-05); crash reporting com símbolos por build |
| 18 | **RemoteConfig** | os knobs do DM §88 vindo do `Tuning`; flags do DM §130 desligam feature sem build; travas de guardrail testadas (offline ≤ 7.200 s, caps de anúncio); snapshot de config em todo A/B |
| 19 | **Save** | Save 2.0 (envelope, checksum, atômico, `.bak`, migração) + migração testada de todas as versões publicadas (H-PLAT-02) + `contentVersion`; save de compra protegido; cloud save conforme decisão |
| 20 | **Release pipeline** | `verify release` verde no AAB (API 36, ARM64, 16 KB, assinatura, `versionCode` crescente, não-development, privacidade); release automatizada tag → AAB → trilha interna (G-PLAT-03); Unity no CI (P15) |
| 21 | **Arte final** | auditoria do No Placeholder Gate (DM §178) com 0 item; Visual Robustness Gate (DM §73) 8/8 em cada tela; brand system (logo, tipografia, cores, iconografia); proveniência completa |
| 22 | **Áudio final** | nenhuma senoide sintetizada no release; mixer Music/SFX/Ambience; 5 músicas; ambiente em camadas que cresce com o negócio; vibração só nos eventos importantes; ajustes de som/vibração |
| 23 | **Store assets** | 4 ícones testados, 2 × 8 screenshots, feature graphic e vídeo, cada peça aprovada no gate §73; ASO em pt-BR/en/es; criativos dos presets de captura (DM §78) |

---

## Lições do estúdio aplicadas a Forge Street (do Dungeon Courier, 2026-10-16)

Estado: PROPOSTO · nada desta seção foi implementado nem medido · não altera as seções 0–23 (os pontos em que o GDD diverge estão em "Conflitos", para decisão).

O Dungeon Courier (DC, repositório `Vvs2705/dungeon-courier`) chegou à 1.4.1-beta com o GDD inteiro feito e juntou o que funcionou e o que custou caro no playbook [`LICOES_PARA_O_ESTUDIO.md`](https://github.com/Vvs2705/dungeon-courier/blob/main/docs/LICOES_PARA_O_ESTUDIO.md). A cópia fica na memória do estúdio, `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\PLAYBOOK_ESTUDIO_DUNGEON_COURIER.md`, ao lado de `forge-street.md` e `STATUS_forge-producao-v1.md`. Boa parte da base do DC **veio do Forge**: `Setup.cs`, núcleo com bot e `-autoplay`, `render_sprites.py`, `Joystick`, LevelPlay e `-record`. Por isso esta seção trata só do que o DC **acrescentou** e o Forge ainda não tem. Cada item segue o formato **o que fazer neste jogo** · **por quê** (o que aconteceu no DC) · **reaproveitar** (caminho no repositório do DC, salvo indicação).

**O Forge já tem (conferido no repositório em 2026-10-16; não refazer):**
- **Processo e portão:** placar GDD × feito (`PROJETO.md` §8 e §23 daqui) e retomada (`PROJETO.md` §9); núcleo C# puro, bot, `econsim`, `-autoplay`, mutantes e `verify.sh full`.
- **Ferramentas de teste e captura:** `-testsession`, `-shot`, `-record`/`-preset` (`Captura.cs`), `-lang` e `-bot`; diário com `diario_report.py`.
- **Interface e marca:** textos em pt/en/es; interface v2 com fontes OFL (Lilita One e Nunito), molduras e botões com relevo (#87); marca v2 pelo gerador grátis (#86) e ícones de prêmio pintados; ícone adaptativo (`Setup.cs`, `icone_fundo`/`icone_frente`); 20:9 preenchido pelo exterior (`Enquadramento.cs`); `AreaSegura` e `Dp`.
- **Loja e criativos:** screenshots da loja tiradas do jogo real (`loja_screenshots.py`, #92); criativos por `-record` + ffmpeg (`CRIATIVOS.md`).
- **Build e Android:** elenco a 1,5× px/m (#81, APK medido); AAB assinado com a chave lida de variável de ambiente; worktree de integração em caminho curto (`C:/fsv`, que faz o papel do `subst R:` do DC).
- **Privacidade:** regras do LevelPlay aplicadas antes do `Init`, com tela de idade (E-PLAT-01).

### Processo
- **O que fazer:** nada novo de estrutura. Só um acréscimo: a cada pausa, um bloco curto **"CONTINUAR DAQUI"** no topo do `PROJETO.md` §9, com o estado, o que falta e os comandos exatos, e o histórico abaixo dele. Tire também fotos de cada passo do tour guiado (abaixo) antes do PR.
- **Por quê:** no DC, o Vinicius retoma com um "vamos continuar" e um bloco curto basta. As fotos acharam um passo do tutorial preso sem alvo e o controle tampando o personagem, coisas que nenhum dos 169 testes verdes via. **Reaproveitar:** rotina, sem arquivo.

### Código e testes
1. **Tour sem matar cliente.** Nos passos de explicação do tour (§Onboarding), o núcleo pausa, como o `Hud.Blocking` do DC: a paciência dos clientes, o relógio do VIP e o prazo das encomendas Rush param. **Por quê:** no Forge, cliente que cansa é perda visível (§3.1), e um tutorial que congela só a tela gera `client_left` no FTUE. Teste no `coretests`: N segundos bloqueado não mudam `client_left` nem o relógio do VIP. **Reaproveitar:** `client/Assets/_DC/Scripts/View/Tour.cs` (o `Game` segura a `Sim` enquanto o cartão está aberto).
2. **Flag `-tutorial`.** Liga o tour no `-testsession` sem gravar os passos vistos; com o `-bot`, o ENTENDI se aperta sozinho em 2 s, para fotografar e gravar cada passo. **Reaproveitar:** a mesma lógica em `Tour.cs`.
3. **Unity IAP de verdade.** O Forge tem a interface `Loja` e a loja falsa, mas não o pacote. O DC já integrou o Unity IAP 5.4.3 com a mesma regra do §17 (concede, grava o save, só então confirma) e restaurar compras. **Reaproveitar:** `client/Assets/_DC/Scripts/View/LojaIap.cs` e `docs/MONETIZACAO.md`. O download do pacote continua pedindo o aval do Vinicius.

### Arte e animação
Hoje o ferreiro tem só `Idle`, `Walking` e `Cheering`; workers têm `Idle`, `LookingAround` e `Walking`; clientes têm 6 clipes. O §18 pede *carry*, *work*, *hammer*, *smelt* e *counter*. Todos saem de clipes já no disco (esqueleto `mixamorig:`, 0 crédito):

| Quem (GDD) | Falta | Clipe a reaproveitar | Item separado, preso ao osso |
|---|---|---|---|
| Ferreiro: golpe de mestre (§9) | martelar | DC `arte/mixamo/_blender/SkelAttack.fbx` (golpe de cima para baixo, impacto no quadro 6 de 12 = 0,5 s, cabe no anel de 0,8 s) ou `_blender/Smash.fbx` | martelo de mão = a função `martelo()` do `arte_v05_blender.py` (a mesma do `item_ferramenta`) em `mixamorig:RightHand`; o martelo do cinto continua decoração |
| Ferreiro e Porters (Pipo, Lia, Caio, Brás, Turmas) | carregar a pilha, pegar da saída | DC `arte/mixamo/Hold.fbx` com `--hold` (braços da carga sobre o `Walking.fbx` do Forge); pegar: ARKANA `mixamo-pegar.fbx` ou DC `_blender/Grab.fbx` (0,5 s, termina no Hold) | a pilha é o sprite do item na âncora da mão (o `meta.json` do `--hold` grava a âncora por quadro) |
| Ferreiro na fornalha; Vulcana (Smelter) | jogar minério, despejar liga | DC `_blender/PickThrow.fbx` (solta no quadro 7 de 12) | tenaz da Vulcana em `mixamorig:RightHand` (primitivas no Blender) |
| Worker com a estação em "fome" | sinal de forma, não só cor (§18) | DC `_blender/Sleep.fbx` (cochila sentado, laço) + `_blender/Wake.fbx` (acorda em 0,4 s) quando o insumo chega | — |
| Celeste (Clerk) | chamar o próximo, anunciar o VIP | DC `_blender/Call.fbx` (mão na boca e aceno) | o livro fundido atrapalha (ver Conflitos) |
| Ísis, Aprendizes de Runa, Mestra Sibila (Arcane Foundry) | energizar o altar | ARKANA `mixamo-standing-2h-cast-spell-01.fbx` e `mixamo-standing-1h-cast-spell-01.fbx` | cajado separado; se o cajado deitar na mão, faça o clipe no Blender como o `NecroCast` do DC |
| Félix (Courier, distrito 6) | levar o lote de exportação | os do courier do DC: `arte/mixamo/courier/CarryRun.fbx` (sprint do ARKANA) e `CarryIdle.fbx`, com o `Hold.fbx` | caixote na âncora da mão |
| Clientes (todas as famílias) | "cansou e foi embora" legível | DC `_blender/Shrug.fbx` (dá de ombros de frente) antes do `Walking` de saída; `Cheering.fbx` para o VIP satisfeito | — |
| Guarda real (cliente), Cadetes da Guarda | sentinela no pátio de cotas | DC `_blender/GuardIdle.fbx` (pé no chão, laço) | lança e escudo separados |
| 29 workers parados ao mesmo tempo | todos respiram em sincronia | ARKANA `mixamo-standing-idle-02.fbx`, `-03`, `-04` alternados com o Breathing Idle | — |

Caminhos de origem: DC = `06_DUNGEON_COURIER/arte/mixamo/` (tabela e SHA em `docs/PROVENIENCIA.md` §3 do DC); ARKANA = `ARKANA/mobile-unity/Assets/_Arkana/Resources/`. São cópias, sem mudar a origem (aval do Vinicius de 2026-10-09). O retarget é por delta de rotação sobre o repouso, nunca uma action crua entre esqueletos.

- **Trocar o `render_sprites.py` pelo do DC.** **Por quê:** o do Forge (400 linhas, um `--attach`) não tem o que o elenco acima pede. O do DC tem:
  - `--hold`, com a âncora da mão por quadro;
  - `--attach item:Osso:pos:rot:escala` repetível;
  - `--align-arms`, para o orc e o fundidor, que vieram em pose A;
  - `--mirror`, `--outline`, `--hd` e `--escala`;
  - `.blend` como cache do FBX (o import leva mais de 10 min);
  - `timeout` em todo render (no DC, um render travou 15 h com o PC suspenso).

  **Reaproveitar:** `client/tools/render_sprites.py`, `render_sprites.ps1`, `folhas.py`, e `client/tools/anim_blender.py` para o que nem o disco tem: martelada com o tempo do anel, "despejar liga". Atualize o `RENDER_SPRITES.md`.
- **Variantes por troca de cor, 0 crédito.** Os 29 `WorkerDef` (`CONTEUDO_V1.md` §6) cabem em uns 10 modelos:
  - os 6 managers (Anselmo, Gertrudes, Valentim, Leonor, Sibila, Aurélio) saem do `gerente`;
  - os Smiths Bruna, Tomé, Iara, Joaquim e Lúcia saem do `oficial`;
  - as Turmas (Ferraz, Dourado, Cadetes, Aprendizes, Estrela) saem do `ajudante`, cada uma na cor do seu distrito (§6);
  - o rival do FS-4 é o ferreiro com bandana e avental de outra cor;
  - os 3 estágios do herói (B-ART-05): o estágio 3 é o uniforme de mestre recolorido, com o martelo de cabeça de ouro (o `martelo_ouro` do `ui_v05b_blender.py`) na mão.

  A silhueta continua dizendo o papel (teste ≥ 5/7, B-ART-09) e a cor diz quem é. **Por quê:** o DC fez roupas e variantes sem gastar crédito. Com a luz chapada do render, troca-se direto na folha, sem Blender. **Reaproveitar:** `client/tools/roupas_cor.py` (`recolor`, por faixa de matiz, saturação e brilho) e `roupas_2d.py`.
- **Retratos pintados, 0 crédito.** Para:
  - o cartão do worker (hoje `MenuBar.Retrato` é o 1º quadro do Idle);
  - os 5 fregueses do FS-3 (mago, elfa, goblin, cavaleiro, nobre);
  - o mercador mentor, o capitão real e o rival (falas de marco do §4).

  **Como:** gerador grátis (Nano Banana 2), no estilo do logo, sobre azul-marinho liso, recortados por `client/tools/ui_assets.py`. **Por quê:** no DC, o retrato pintado ficou muito mais vistoso que o render 3D recortado. Personagem que só fala em marco precisa só do retrato, sem modelo 3D.
- **Custo estimado no Tripo** (saldo visto: 15 670, `PROJETO.md` §9; no DC: modelo HD 45, Auto Rig 20, imagem grátis):

  | Lote | Créditos |
  |---|---|
  | Elenco por cor + clipes do disco | 0 |
  | Itens de mão (martelo, tenaz, cajado, lança) por primitivas no Blender, como os 41 itens do `itens_blender.py` | 0 (45 cada se for pelo Tripo) |
  | Vulcana e Ísis, se o recolor do `fundidor` e do `oficial` não passar no teste de silhueta | 2 × 65 = 130 |
  | Retratos e ícones | 0 |

  **Teto previsto:** 130 a 310 créditos. Confira o saldo antes e depois de cada geração e registre no `PROVENIENCIA.md`.

### Interface e marca
- **O que fazer:** o Forge já tem quase tudo (ver lista acima). Faltam três pontos:
  - **Medalhão claro atrás do ícone pintado** (ícones de prêmio, retratos novos). O disco escuro fica só para silhueta procedural.
  - **Conferir o ícone adaptativo nas máscaras** círculo, squircle e quadrado com `tools/icone_app.py`, e no AAB com `aapt2 dump badging` (o ícone tem de ser o `.xml` adaptativo).
  - **Conferir o ícone 512 da loja:** deve ser o mesmo do aparelho.
- **Por quê:** no DC, o ícone pintado ficou legível sobre o medalhão claro; o disco escuro só funciona para silhueta procedural. Além disso, o launcher mostra só o meio 2/3 do ícone e corta em círculo.
- **Reaproveitar:** `client/Assets/_DC/Scripts/View/Art.cs` (medalhão, `Letreiro`) e `client/tools/icone_app.py`.

### Onboarding
**Tour guiado (obrigatório, pedido do Vinicius no DC).**
- **Como funciona:** a tela congela e só o alvo fica aceso (véu com buraco, contorno dourado e seta). Um cartão diz o que é, quando usar e por quê. Nos passos de ação, só o alvo responde. Cada passo aparece uma vez; há "Pular tutorial", e "REVER TUTORIAL" nas Configurações. Fica desligado no `-testsession`. Passo de botão se cancela se a tela mudar.
- **Relação com a dica:** a pílula de dica de 1 linha continua como lembrete depois do tour.
- **Revelação progressiva:** os gatilhos seguem o §19 e a regra da Wave B (nada novo nos primeiros 10 min).
- **Telemetria:** o `tutorial_step` do diário passa a contar os passos do tour.

| # | Passo (tipo) | Quando | O que diz · por quê |
|---|---|---|---|
| 0 | cartão de contexto (retrato + balão) | 1ª abertura, antes do 1º toque | ver abaixo · contexto em 1 balão, sem cutscene (§4) |
| 1 | ação: alvo = Depósito | logo depois | "Arraste para andar. Pegue minério." · o verbo (andar e carregar) vem antes de qualquer menu |
| 2 | ação: alvo = Fornalha | ferreiro com minério | "3 minérios viram 1 lingote." · 1º lingote antes de 0:45 (§3.3) |
| 3 | ação: alvo = Bigorna | 1º lingote pronto | "Na bigorna, o lingote vira espada." |
| 4 | ação: alvo = Balcão | 1ª espada pronta | "O cliente paga no balcão." · 1ª venda < 90 s |
| 5 | ação: alvo = pad do Fole (anel de ouro) | ouro ≥ custo do Fole | "Anel de ouro = melhoria. Pise para comprar." |
| 6 | explicação: alvo = a estação parada | 1ª vez que uma estação fica cinza ("fome") ou vermelha ("travada") | "Cinza: falta insumo. Vermelho: saída cheia. Invista onde trava." · é a decisão central do §3.1; ensina no 1º gargalo real, não antes |
| 7 | explicação: alvo = cartão da encomenda | 1ª Order (~0:45) | "Encomenda nunca falha; o prêmio é extra." (§10) |
| 8 | ação: alvo = pad do 1º ajudante | 3:30–4:30 | "O ajudante carrega por você." · 1º degrau da automação (§4) |
| 9 | explicação: alvo = botão Chamar VIP | quando o 1º botão de anúncio aparece (depois da 1ª melhoria, D6) | "Opcional: traz um VIP extra; volta em 5 min." · o rewarded nunca é necessário (§17) |
| 10 | ação: alvo = anel do golpe | 1ª peça na bigorna depois das Ferramentas (~10:46) | "Parado na bigorna, acerte o anel: a peça sobe 1 nível. Errar não pune." (§9) |
| 11 | explicação | Renome 1; 1º projeto de Masterwork; 1º Charter | um passo para cada sistema no momento em que ele abre: selo de Renome (não se gasta), Hall/Collection, mapa de distritos |

- **Cartão de contexto no lugar da cutscene.** Quem fala é o mercador mentor do §4, com o retrato pintado e um balão no 1º cartão do tour. Fala proposta: *"Esta forja era do seu avô. A rua precisa de espadas: acenda a fornalha."*
  - **Por quê:** no DC, uma cutscene somada ao tutorial virou parede de texto e atrasou o 1º toque. O cartão do personagem que pede a missão resolveu e fecha um ciclo com a cena de entrega. No Forge, a cena de fecho é o Charter: o mesmo mentor volta e entrega o distrito. Se o mentor for o Mestre Anselmo, ele vira o manager do Old Forge.
  - **Reaproveitar:** `Tour.cs` (campo `Fala`: retrato + balão acima do cartão).
- **Joystick invisível por padrão.** Hoje o `Joystick.cs` do Forge desenha uma base de 132 dp e um miolo de 50 dp onde o dedo toca. O ferreiro tem cerca de 9,9% da largura da tela (`Enquadramento.cs`), e a base cobre ele e a pilha que ele carrega. O controle continua o mesmo, sem desenho, e "Controle na tela" nas Configurações traz o desenho de volta. O anel do golpe já fica num lugar fixo (§19).
  - **Por quê:** no DC, o joystick desenhado tampava o courier (1.4.1-beta).
  - **Reaproveitar:** campo `Visivel` do `client/Assets/_DC/Scripts/View/Joystick.cs` (`_base.gameObject.SetActive(Visivel)`).

### Monetização e compliance
Confronto do §17 com os guardrails:
- **Nada aleatório pago:** cumpre. Baú com conteúdo fixo, "trocar" mostra a próxima, sem "chance" (V4, P5).
- **Rewarded opcional:** cumpre. O kill criterion já existe e o bot F2P fecha tudo sem anúncio.
- **Interstitial:** cumpre. Fica atrás de flag desligada (V10).
- **Offline:** cumpre. O teto de 2 h é trava no núcleo.
- **1 moeda:** cumpre até a Wave E, e lá entram as Gems (ver Conflitos).
- **Anúncio não personalizado e sem `AD_ID`:** **diverge** do DC (ver Conflitos).

O que acrescentar:
- **Política de privacidade pública.** No Forge ainda está `<<PREENCHER>>` (`PRIVACIDADE.md` §6). O DC resolveu em uma página: GitHub Pages, branch `gh-pages`, um `index.html` com todos os idiomas, citando exatamente as redes usadas (LevelPlay, Unity Ads, ironSource Ads). **Reaproveitar:** o `index.html` da branch `gh-pages` do DC (https://vvs2705.github.io/dungeon-courier/) como modelo, com 3 idiomas, mais o link em Configurações > Privacidade.
- **Unity Ads no LevelPlay.** O painel pede a chave da Monetization Stats API e o core ID da organização Unity. Para o DC isso já foi feito na conta da V-STACK; conferir no painel se ligar o Unity Ads no app do Forge é só ativar. **Reaproveitar:** `docs/MONETIZACAO.md` §4 do DC.

### Play e lançamento
- **Conta de organização desde já.**
  - **O que fazer:** publicar na conta de **organização** da V-STACK, a mesma do DC, que espera o D-U-N-S (previsão 2026-11-25; `docs/LOJA.md` §1 do DC). Não abrir conta pessoal.
  - **Por quê:** o `PROJETO.md` P9 planeja o Closed com 12 testadores por 14 dias "em conta pessoal". Essa exigência é das contas pessoais novas; confirmar na conta de organização quando ela abrir. No DC, tudo ficou pronto e só a conta falta, por isso o D-U-N-S tem de ser pedido cedo, e ele já foi.
- **Chave de upload.** O `FS_KEYSTORE` lido de variável de ambiente está certo. Falta o **backup fora do PC** do `.jks` e das senhas: perder a chave obriga a pedir reset ao suporte.
- **Criativos automáticos.**
  - **O que fazer:** trocar a receita manual de ffmpeg do `CRIATIVOS.md` pelo `criativos.py` do DC, que monta os vídeos 9:16 com legenda e CTA em cada idioma (no DC, 13 vídeos × 4 idiomas, refeitos a cada mudança de arte). O `loja_assets.py` do DC gera a feature graphic 1024×500 de um quadro real, que é o item 5 da fila do `PROJETO.md` §9.
  - **Reaproveitar:** `client/tools/criativos.py` e `client/tools/loja_assets.py`.

### Android
- **vSync desligado.**
  - **O que fazer:** acrescentar `QualitySettings.vSyncCount = 0;` ao lado do `Application.targetFrameRate = 60;` no `Game.Awake` (`Game.cs`).
  - **Por quê:** no Forge, a qualidade padrão do Android é Medium (`QualitySettings.asset`: `m_PerPlatformDefaultQuality` Android 2, `vSyncCount: 1`), a mesma situação do DC antes do ajuste, quando o POCO F4 foi de 28–36 para 55 fps. Medir no gate do POCO.
- **Memória do aparelho.** O APK foi a 121,7 MB com as folhas a 1,5×, e o DC subiu de 67,7 para 83 MiB pelo mesmo motivo. Meça a memória (PSS) no aparelho, não só o tamanho do arquivo.
- **adb no Git Bash:** `adb shell "am start -n <pkg>/<act> -e unity '-a -b'"`, com aspas por dentro; sem elas, só o 1º argumento chega ao jogo.
- **Aparelho:** pergunte "pode testar?" antes de usar o POCO e tire foto só com o jogo em primeiro plano. Meça o desempenho também num aparelho original: o POCO de 4 anos se limita depois de 1 min.

### Conflitos com o GDD atual
1. **§17 e E-PLAT-01: tela de idade na 1ª abertura, anúncio personalizado para 18+ que aceitar, `AD_ID` mantido.** No DC, o anúncio é não personalizado para todos, sem `AD_ID` e sem tela.
   - **A favor do Forge:** eCPM maior com adultos.
   - **Contra:** é uma tela antes do 1º toque (contra a "1ª venda < 90 s" do §3.3); a declaração de ID de publicidade fica = sim; o consentimento tem de ser versionado; a verificação no POCO ainda está pendente (`PRIVACIDADE.md` §7).
   - **Recomendação (decisão do Vinicius):** se a tela ficar, mova-a para o momento em que o 1º botão de anúncio aparece (depois da 1ª melhoria). **Aplicado em 2026-10-23** (`Privacidade.AbreTela`; o bot faz a 1ª venda aos 23 s e a tela abre aos 67 s): a tela fica e sai do 1º toque. Se não, copie o modelo do DC (`AnunciosLevelPlay.cs` + `Setup.SemIdDePublicidade`) e apague a tela.
2. **§5.1 e §17.3: Gems na Wave E** (Gem Packs, Starter com Gems, Master Blacksmith Bundle) criam a 2ª moeda. O DC fechou o GDD inteiro com 1 moeda e IAP de conteúdo fixo (remover anúncios, pacote inicial, roupas, pacote de evento). **Recomendação:** v1.0 só com ouro e venda de conteúdo fixo (Pacote do Mestre, skins, Pass). Gems só se o `econsim` mostrar um ralo de conveniência que o ouro não cobre.
3. **§18 / `ASSETS.md`: item fundido na mão** do balconista (livro), do gerente (livro), do mensageiro e do emissário (pergaminho). A regra do estúdio é item separado preso ao osso. **Recomendação:**
   - nos já gerados, usar só clipes que não giram a mão (Idle, Walking, LookingAround);
   - para `Call`, martelada etc., apagar o item fundido no Blender pela cor da textura (como o `saco_ladrao.py` do DC, 0 crédito) e prender o item separado;
   - gerar os personagens novos (Vulcana, Ísis) já sem item na mão.
4. **§3.3 e §19: o FTUE é só a pílula de dica de 1 linha.** O estúdio agora exige tour guiado. **Recomendação:** o tour do §Onboarding complementa a dica, com os mesmos gatilhos e sem explicar sistema futuro cedo.
5. **§18, "nenhuma senoide sintetizada no release":** não é conflito. O Forge decidiu áudio real, e a lição do `audio_gen.py` do DC não se aplica aqui.

### Primeiros passos recomendados
1. `QualitySettings.vSyncCount = 0` no `Game.Awake` (1 linha; medir no gate do POCO).
2. Joystick invisível por padrão + "Controle na tela" nas Configurações (campo `Visivel` do DC).
3. Tour guiado: copiar `Tour.cs`, trocar os passos pela tabela acima, pausar a paciência no passo de explicação, criar o `-tutorial` e fotografar cada passo.
4. Cartão de contexto com o retrato pintado do mentor, no lugar do 1º passo.
5. Trazer `render_sprites.py` e `anim_blender.py` do DC e renderizar a martelada do golpe de mestre, o carry e o cochilo da "fome" com clipes do disco (0 crédito).
6. Elenco por cor (managers, Smiths, Turmas, rival) com `roupas_cor.recolor` antes de qualquer lote no Tripo.
7. Política pública no `gh-pages` e backup da chave de upload.
8. Levar ao Vinicius as decisões dos Conflitos 1 (tela de idade × não personalizado para todos) e 2 (Gems na Wave E).
