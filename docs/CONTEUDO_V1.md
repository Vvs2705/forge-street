# Conteúdo da v1.0 — catálogo de dados dos 6 distritos

2026-10-13 · Raia DESIGN DE CONTEÚDO (Leva 9 do ORQUESTRADOR) · Waves B/C · Estado: **PROPOSTO**. Nada daqui está implementado nem medido.
Tickets: **C-CORE-01..05** (distritos), **B-CORE-01..08** (Defs), **D-CORE-04** (Royal Contracts) em [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md). Regras de design: [`GDD.md`](GDD.md) §5–§14, §21, §23. Esquemas e números do Old Forge: [`ECONOMIA_WAVE_B.md`](ECONOMIA_WAVE_B.md). Números medidos: [`BALANCE.md`](BALANCE.md) §14–§22. Eventos: [`LIVEOPS_V1.md`](LIVEOPS_V1.md). Decisões: [`PROJETO.md`](PROJETO.md) V4, V5, V7, V9, V22, V24.

**Para que serve.** É a fonte de **dados** que as raias de núcleo vão codificar: uma linha de tabela = um `*Def`. Cada tabela de dados tem um marcador `<!-- tabela: nome -->` logo acima, e o script [`client/tools/conteudo_check.py`](../client/tools/conteudo_check.py) lê essas tabelas e confere as regras do §16 (rode depois de qualquer mudança aqui). Todo número é **hipótese até o econsim medir** (`client/tools/econsim`); os números do Old Forge que já existem no `Defs.cs` não mudam.

---

## 0. Regras que valem para tudo

1. **Sem sorteio** (V7). Toda mecânica nova é uma regra determinística: mesma entrada, mesmo resultado. Rodízio no lugar de peso, acumulador no lugar de chance, sequência fixa no lugar de "drop".
2. **Nada aleatório pago** (V4; ECA Digital, Lei 15.211/2025). Baú de contrato tem conteúdo fixo mostrado antes. Nenhuma Masterwork, Blueprint essencial ou Charter depende de anúncio ou compra.
3. **Só ouro até a Wave E** (V9). Renome é trilha; Blueprint é posse. Nenhuma tabela daqui tem Gem.
4. **Offline com teto de 2 h** (trava do núcleo). Manager e Mensageiro mexem na **taxa**, nunca no tempo; o offline nunca importa item, nunca exporta lote, nunca fecha bloco de cota e nunca dá Masterwork.
5. **Prêmio que não é venda** (encomenda, contrato, marco, coleção) entra fora do `GoldEarned`, com ID de transação. Venda no balcão, pagamento de bloco de cota (§2.4) e lote de exportação (§2.6) **são venda** (entram no `GoldEarned`, na `RateEma` e no offline).
6. **Escala:** custos em minutos de renda esperada; prêmios em segundos da taxa online (`RateEma`), como as encomendas atuais (`BALANCE.md` §20). Mínimos de prêmio em ouro escalam com o multiplicador do distrito `M` (§1).
7. **Primeiros 10 min intocados.** Nada daqui entra no Old Forge antes das Ferramentas (~10:46 no bot F2P).
8. **Convenção de IDs.** Itens **sem prefixo** (V24: `espada`, `machado_guerra`; sprite `item_<id>`; texto `item.<id>.nome`). Melhorias sem prefixo (V22: `fole`, `bigorna2`); a melhoria que abre uma linha tem o **mesmo ID da estação** que ela constrói (padrão atual: `escudos`). Estações e famílias sem prefixo. Os demais `*Def` com prefixo: `rec_`, `wk_`, `ord_`, `ctr_`, `bp_`, `mw_`, `cli_`, `col_`. Distritos: `old_forge`, `armorers_row`, `artisan_quarter`, `royal_arsenal`, `arcane_foundry`, `grand_forge`. Nenhum ID é renomeado depois de publicado.
9. **Nomes.** Coluna pt-BR + en/es curtos (≤ 16 caracteres, a largura do cartão da HUD, `LocTests`). Nos itens e melhorias o pt-BR também cabe em 16. Nome próprio (worker, freguês) é igual nos 3 idiomas. Blueprint não tem nome próprio: a UI mostra "Projeto: " + o nome do que ele libera.
10. **Receitas atuais intocadas** (B-CORE-01): fornalha 1 minério → lingote; bigorna 1 lingote → espada; escudos 2 lingotes → escudo (tempo ×1,5); ferramentas 1 lingote → ferramenta; joalheria 2 lingotes → joia (tempo ×2). O "tempo base" da tabela de receitas é o tempo inicial sem melhoria (= tempo base do tipo × `TimeMul`); para as receitas novas, `TimeMul = tempo base ÷ HammerTime0` (bancadas) ou `÷ FurnaceTime0` (fornalhas).

---

## 1. Escala econômica e curva por distrito

**Âncora (medida, `BALANCE.md` §15, §18.5, §20, §22):** o Old Forge chega à produção completa (26 produtivos, 33.425 de ouro) aos 41:31 no econsim F2P e 47:03 no bot humano, com renda de ~1.600/min (1.584–1.668/min entre 41 e 90 min). A Wave B soma ~138,8 mil de ralo Meta e o luxo, 54 mil.

**Regra de escala (HIPÓTESE, revisar):** cada distrito rende **3× o anterior** na produção completa (`M = 3^(d−1)`) e os custos produtivos seguem a fórmula do Old Forge com base maior: `custo(tier) = CostBase × 1,30^tier`, múltiplo de 5 (até 10 mil) ou 2 algarismos significativos (acima), com **CostBase = 160 × M** e 18 produtivos por distrito, sem exceção de preço. O primeiro produtivo de cada distrito custa ~0,5 min da renda que o jogador já tem quando chega (renda de fundo dos anteriores + o kit inicial), o mesmo ritmo do Fole no Old Forge.

**Modelo usado nas contas (não é medição).** Renda na produção completa: modelo estático de vazão (estações do estado final × 60 / tempo; insumo que falta corta as linhas que dependem dele), calibrado no Old Forge: o modelo dá 1.491/min contra 1.600/min medidos, então toda renda prevista multiplica por 1,073. Os preços dos distritos 2–6 saem do valor relativo de cada produto (valor dos insumos + 0,3 × segundos de cada etapa intermediária + 0,6 × segundos da etapa final; importado = 6) escalado para o modelo bater a renda alvo. Tempo até a produção completa: soma de `custo / renda` com a renda subindo em linha reta do kit inicial até a renda alvo, mais a renda de fundo dos distritos anteriores, multiplicada por 1,42 (o fator que faz o mesmo modelo dar os 41,5 min medidos no Old Forge). **O econsim substitui tudo isso** assim que o C-CORE-05 rodar um distrito em dados.

<!-- tabela: curva -->
| distrito | M | CostBase | Σ produtivos | produção completa (F2P) | renda/min na produção completa | renda/min no Charter (×1,5) | renda de fundo (manager L1) | abertura | Charter (h no distrito) | h acumuladas | missionScale |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `old_forge` | 1 | 50 (fórmula + tabela) | 33.425 | 41:31 F2P / 47:03 bot humano (medido) | 1.600 | 2.400 | 600 | — | 3–6 | 4,5 | 1,00 |
| `armorers_row` | 3 | 480 | 179.665 | ~61 min (modelo) | 4.800 | 7.200 | 1.800 | 140.000 | 3–5 | 8,5 | 0,65 |
| `artisan_quarter` | 9 | 1.440 | 532.355 | ~57 min (modelo) | 14.400 | 21.600 | 5.400 | 430.000 | 4–6 | 13,5 | 1,00 |
| `royal_arsenal` | 27 | 4.320 | 1.600.725 | ~56 min (modelo) | 43.200 | 64.800 | 16.200 | 1.300.000 | 4–6 | 18,5 | 0,83 |
| `arcane_foundry` | 81 | 12.960 | 4.789.000 | ~56 min (modelo) | 129.600 | 194.400 | 48.600 | 3.900.000 | 5–7 | 24,5 | 1,09 |
| `grand_forge` | 243 | 38.880 | 14.501.000 | ~56 min (modelo) | 388.800 | 583.200 | 145.800 | 12.000.000 | 6–8 (Carta) | 31,5 | 0,28 |


- **Renda no Charter:** ×1,5 sobre a produção completa (qualidade Fina/Superior, níveis de estação e workers; HIPÓTESE, o portão da Wave B limita o prêmio de qualidade a ≤ 35% aos 120 min).
- **Renda de fundo:** manager nível 1 = **25% da `RateEma` do distrito no momento em que o jogador saiu dele** (o mesmo fator do offline), 30/33/36/40% nos níveis 2–5. Sempre menor que a renda ativa (invariante C-CORE-03). Fica **fora do `GoldEarned`**: é derivada da taxa, como o cofre offline (não infla VIP nem cofre).
- **Abertura** = 90 min da renda de produção completa do distrito anterior. Quando o Charter sai, o jogador já passou horas no distrito; a abertura é o ralo que absorve o ouro acumulado no rabo do Charter (o problema "ouro sem destino" do `BALANCE.md` §22).
- **Charter (h no distrito):** alvo de tempo ativo do F2P dentro do distrito até o Charter (produção completa + níveis + Masterworks + Renome). Acumulado: Arcane Foundry abre com ~18,5 h de jogo (≈ dia 56 para o F2P de referência de 20 min/dia do `LIVEOPS_V1.md` §1, o "D60" do GDD §3.5) e Grand Forge com ~24,5 h (≈ dia 74; endgame até o "D90").
- **`missionScale`** (campo do `DistrictDef` que o `LIVEOPS_V1.md` §4.1 pede): unidades vendidas por minuto na produção completa ÷ as do Old Forge (modelo). Multiplica os alvos de volume das missões quando o jogador está naquele distrito.

**Ralos por distrito (ouro).** Produtivos: 33.425 / 179.665 / 532.355 / 1.600.725 / 4.789.000 / 14.501.000. Luxo (3 por distrito, 8,75 / 11,25 / 13,75 min de renda, como os 14/18/22 mil do Old Forge): 14.000 / 18.000 / 22.000 · 42.000 / 54.000 / 66.000 · 130.000 / 160.000 / 200.000 · 380.000 / 490.000 / 590.000 · 1.100.000 / 1.500.000 / 1.800.000 · 3.400.000 / 4.400.000 / 5.300.000. Níveis de estação (§5.7): 78.550 / 236.800 / 705.000 / 2.116.000 / 6.360.000 / 19.100.000 (o mesmo peso do Old Forge, ~49 min de renda). Workers: custo dos níveis no §6 (~40 min de renda por especialista, ~18 min por turma).

**Renda por produto na produção completa (modelo calibrado).** Mostra o mix que o modelo assume; é a base dos preços. No Old Forge a joia é ~56% da renda (medido: ~14 joias/min × 80).

<!-- tabela: mix -->
| distrito | produto | un/min (modelo) | preço | ouro/min (calibrado) |
|---|---|---|---|---|
| old_forge | `joia` | 10,4 | 80 | 894 |
| old_forge | `espada` | 25,0 | 10 | 268 |
| old_forge | `escudo` | 8,3 | 25 | 224 |
| old_forge | `ferramenta` | 12,5 | 16 | 215 |
| armorers_row | `peitoral` | 3,5 | 230 | 854 |
| armorers_row | `elmo` | 5,2 | 140 | 780 |
| armorers_row | `grevas` | 5,2 | 130 | 724 |
| armorers_row | `maca` | 5,2 | 120 | 668 |
| armorers_row | `machado_guerra` | 5,2 | 110 | 613 |
| armorers_row | `lanca` | 7,8 | 70 | 585 |
| armorers_row | `manoplas` | 4,4 | 120 | 573 |
| artisan_quarter | `castical` | 7,1 | 290 | 2.223 |
| artisan_quarter | `broche` | 8,6 | 240 | 2.208 |
| artisan_quarter | `cinzel` | 10,7 | 190 | 2.185 |
| artisan_quarter | `caixa_joias` | 4,3 | 450 | 2.070 |
| artisan_quarter | `colar` | 5,4 | 350 | 2.012 |
| artisan_quarter | `calice` | 6,1 | 300 | 1.971 |
| artisan_quarter | `fivela` | 14,3 | 110 | 1.686 |
| royal_arsenal | `alabarda_cerimonial` | 8,8 | 970 | 9.203 |
| royal_arsenal | `kit_batedor` | 11,1 | 710 | 8.449 |
| royal_arsenal | `insignia_real` | 14,1 | 550 | 8.349 |
| royal_arsenal | `armadura_gala` | 3,8 | 1.900 | 7.695 |
| royal_arsenal | `kit_guarda` | 4,2 | 1.200 | 5.355 |
| royal_arsenal | `espada_gala` | 4,8 | 820 | 4.182 |
| arcane_foundry | `anel_cristal` | 22,7 | 1.500 | 36.538 |
| arcane_foundry | `amuleto_arcano` | 13,6 | 1.800 | 26.291 |
| arcane_foundry | `cajado_liga` | 4,6 | 3.100 | 15.363 |
| arcane_foundry | `lamina_runica` | 5,3 | 2.400 | 13.593 |
| arcane_foundry | `escudo_runico` | 4,1 | 3.000 | 13.215 |
| arcane_foundry | `liga_celeste` | 3,7 | 3.300 | 13.083 |
| arcane_foundry | `lanterna_arcana` | 7,4 | 1.500 | 11.894 |
| grand_forge | `sino_catedral` | 5,6 | 23.000 | 137.119 |
| grand_forge | `estatua_bronze` | 4,4 | 24.000 | 113.719 |
| grand_forge | `lamina_estelar` | 1,6 | 21.000 | 36.998 |
| grand_forge | `panoplia_campeao` | 1,2 | 29.000 | 36.495 |
| grand_forge | `coroa_reino` | 1,4 | 22.000 | 32.300 |
| grand_forge | `relicario_real` | 1,4 | 20.000 | 29.363 |


---

## 2. Distritos (`DistrictDef`)

Campos (C-CORE-01): `id`, `index`, `nameKey`, `costBase`, `costGrowth` (1,30), `openCost`, `charter` (condições), `majorUnlock`, `cycle` (rodízio de encomendas, §7.1), `missionScale`, `startKit` (estações já construídas), `stations` (§4), `qualitySources` (§10.2), `hall` (10 pedestais), `music` e `palette` (arte). Uma instância de `Sim` por distrito; carteira, Renome, Blueprints, Collection e contratos ficam na camada Império.

<!-- tabela: distritos -->
| id | # | pt | en | es | mecânica nova | abertura (ouro) | renda alvo (/min) | Charter | major unlock | missionScale |
|---|---|---|---|---|---|---|---|---|---|---|
| `old_forge` | 1 | Old Forge | Old Forge | Vieja Forja | cadeia física, gargalo, ajudantes, esteira, Quality e golpe de mestre (existe / Wave B) | — | 1.600 | produção completa + Renome 4 (400) + 1 Masterwork do distrito | Charter → Armorer's Row (cena: comprar a forja do rival, FS-4) | 1,00 |
| `armorers_row` | 2 | Rua dos Armeiros | Armorer's Row | Calle Armeros | montagem: bancada com 2–3 componentes; liga opcional = q +30 | 140.000 (90 min × renda do d1) | 4.800 | produção completa + Renome 5 (750) + 2 Masterworks do distrito | `carroca_curtidor` (couro) e `fornalha_liga` (Fundidor) | 0,65 |
| `artisan_quarter` | 3 | Bairro Artesão | Artisan Quarter | Barrio Artesano | bancada configurável (margem × volume) e clientes ricos | 430.000 (90 min × renda do d2) | 14.400 | produção completa + Renome 6 (1.250) + 3 Masterworks do distrito | `lapidario` (lapidação de gemas) | 1,00 |
| `royal_arsenal` | 4 | Arsenal Real | Royal Arsenal | Arsenal Real | pátio de cotas: blocos com prêmio ×1,3 e prazo-bônus; Royal Contracts | 1.300.000 (90 min × renda do d3) | 43.200 | produção completa + Renome 7 (2.000) + 3 Masterworks do distrito + (1 Royal Contract **ou** 10 encomendas Royal no distrito) | `mesa_real` (contratos reais permanentes: 2ª vaga de contrato) | 0,83 |
| `arcane_foundry` | 5 | Fundição Arcana | Arcane Foundry | Fundición Arcana | medidor de carga arcana por altar (Estável × Forçado), sem sorteio | 3.900.000 (90 min × renda do d4) | 129.600 | produção completa + Renome 8 (3.000) + 4 Masterworks do distrito + (2 Royal Contracts **ou** 20 encomendas Royal no distrito) | `veio_runico` (minério rúnico; o cristal já vem no kit) | 1,09 |
| `grand_forge` | 6 | Grande Forja | Grand Forge | Gran Forja | síntese com importados (rotas do Mensageiro) e lotes de exportação | 12.000.000 (90 min × renda do d5) | 388.800 | Carta da Grande Forja: produção completa + Renome 9 (4.500) + 5 Masterworks do distrito + (3 Royal Contracts **ou** 30 encomendas Royal no distrito) | `mensageiro` (rotas e exportação); endgame de exportação e Masterworks lendárias | 0,28 |


**Regras comuns a todo distrito**
- **Produção completa** = todos os upgrades `Production` do distrito comprados (Old Forge: os 26 atuais; distritos 2–6: os 18 da tabela). Luxo, Meta (níveis de estação, `bigorna_longa`, `selo_arsenal`, `bencao_forja`) e workers ficam fora, como hoje (`ECONOMIA_WAVE_B.md` §0.7). O 1º luxo de cada distrito exige a produção completa.
- **Kit inicial** (grátis ao abrir): depósito(s) de partida, uma estação de cada etapa da 1ª linha e o balcão; o ferreiro carrega no começo, como no Old Forge. Mochila e Botas são ferramentas do herói e valem em todos os distritos.
- **Charter** (V5, sem reset): 1× por distrito, com ID de transação. Concede +100 de Renome, insígnia, o manager do distrito (§6), a renda de fundo, uma fala de marco e libera o próximo distrito (abertura paga em ouro no mapa). O último (Grand Forge) é a **Carta da Grande Forja**: mesmas concessões, sem próximo distrito.
- **Contrato nunca bloqueia** (GDD §11 × §23 item 7): onde o Charter pede Royal Contracts (distritos 4–6), a mesma condição aceita **10 encomendas Royal entregues no distrito por contrato pedido**. O bot F2P fecha os 6 distritos sem cumprir nenhum contrato.
- **Volta ao distrito antigo:** o manager segue, a renda de fundo segue, eventos e encomendas do distrito seguem; os níveis de estação e Masterworks que faltam continuam ali (DM §10).
- **Hall:** 10 pedestais físicos por distrito (FS-6), em ordem fixa (coluna "pedestal" do §10).

### 2.1 Old Forge (`old_forge`) — existe

Oficina herdada; pedra, madeira, brasa laranja. Ensina a cadeia física, o gargalo, os ajudantes, a esteira, as encomendas e (Wave B) Quality e golpe de mestre. Conteúdo atual intocado; este catálogo só **acrescenta**:
- **Espada longa** (`espada_longa`, 2 lingotes, 7,5 s, 40 de ouro): linha **Meta** na `bigorna_longa` (6.000; exige `martelo_veloz` e o `bp_rec_espada_longa`), fora da produção completa para não mexer nos portões medidos de 41–48 min. Família `espada` (mesma técnica, oficial e nível das bigornas). 20 de ouro por lingote, entre a ferramenta (16) e a joia (40).
- Os 6 ajudantes atuais viram `WorkerDef` nomeados (§6), sem mudar rota nem custo.
- **Charter:** produção completa + Renome 4 + 1 Masterwork (V15). Major unlock: Armorer's Row (o rival do FS-4 só entra depois do A/B no bot; se entrar, "comprar a forja dele" é a cena deste Charter).

```
layout (fileiras de baixo para cima = entrada → saída; já existe)
oficina x 0–9                         | rua lateral x 9–15
[depósito]                            |
[fornalha][fornalha 2]                | [carroça]
[bigorna][bigorna 2][bigorna longa*]  | [joalheria]
[escudos][ferramentas]                | [loja de joias]
[balcão 4→8 vagas]          [baú]     | [Hall*: 10 pedestais]
* novo
```

### 2.2 Armorer's Row (`armorers_row`) — montagem

Rua de armeiros; ferro escuro, couro, rebites, faíscas azuladas da prensa. Família: armaduras (elmo, grevas, manoplas, peitoral) e armas de haste (maça, lança, machado bélico). Kit inicial: depósito de minério, fornalha, prensa, rebitadeira, montagem de elmos, balcão de 6 vagas (estoque 10). CostBase 480; abertura 140.000.

**Mecânica nova — montagem (regra):**
1. Estação `Assembly`: uma entrada por componente da receita, com capacidade de 2× a quantidade da receita (elmo: placas 4, rebites 4). A peça **só começa quando todos os componentes estão na entrada** na quantidade da receita; consome tudo no início; o tempo corre como numa bancada.
2. **Liga** (depois de `fornalha_liga`): toda estação `Assembly` ganha um slot opcional de liga (capacidade 2). Se há ≥ 1 liga no slot quando a peça começa, consome 1 e a peça soma **q +30** (fonte "liga" no `QualityBreakdown()`); sem liga, nada muda. O contador `AlloyPieces` (por produto) conta as peças feitas com liga.
3. Carregadores entregam primeiro o componente com a **menor razão estoque/necessário** na estação de destino; empate pela ordem da receita. Determinístico, e o jogador lê o gargalo (o componente que falta pisca na estação, como a "fome" cinza).
4. A rebitadeira produz **4 rebites por lingote** (`outputCount` 4); a prensa, 1 placa por lingote.

```
[depósito minério]                 [carroça do curtidor*]
[fornalha][fornalha 2]             [fornalha de liga*]
[prensa][prensa 2][rebitadeira][rebitadeira 2]
[mont. elmo][mont. grevas*][mont. manoplas*][mont. peitoral*]
[bigorna pesada*][forja de hastes*][forja de machados*]
[balcão 6 vagas]                   [Hall]
* comprado
```

- **Major unlock:** `carroca_curtidor` (depósito de couro + linha das grevas; libera lança, manoplas, machado e peitoral) e `fornalha_liga` (o Fundidor, `wk_vulcana`).
- **Charter:** produção completa + Renome 5 (750) + 2 Masterworks do distrito.

### 2.3 Artisan Quarter (`artisan_quarter`) — margem × volume

Bairro de ourives; ouro, cobre, vitrines, lanternas. Família: fivela, cinzel, castiçal, broche, cálice, colar, caixa de joias. Kit inicial: depósito de cobre, crisol de cobre, 1 bancada de ourives (fivela e cinzel), loja fina de 3 vagas. CostBase 1.440; abertura 430.000.

**Mecânica nova — bancada configurável e clientes ricos (regra):**
1. Estação `Configurable` (`bancada_ourives`): faz **qualquer receita do distrito já liberada**; a receita ativa é escolhida pelo jogador (toque na bancada). Trocar a receita: a bancada termina a peça em curso e fica **parada 4 s** (`troca_rapida`: 1 s; Lúcia: −1 s; mínimo 1 s). Os insumos que a nova receita não usa **ficam na entrada** (até 6 por tipo) e servem na próxima troca; nada se perde.
2. Carregadores só entregam insumos da receita ativa de cada bancada.
3. **Clientes por família com rodízio fixo de pedidos** (§12): mercador e viajante pedem as peças baratas e rápidas (fivela, cinzel, castiçal), chegam mais e esperam pouco; nobre e colecionador pedem as caras (colar, broche, cálice, caixa), chegam a cada 2,5–3× o intervalo e esperam 1,8–2× mais. O mercador leva 2 unidades por visita.
4. **A decisão** é quantas bancadas ficam no volume e quantas na margem. Ouro por segundo de bancada (nível 1) é parecido em todas as receitas (fivela 36,7 · cinzel 47,5 · castiçal 48,3 · broche 48 · cálice 42,9 · colar 43,8 · caixa 45); o que muda é **o metal gasto** (fivela 110 por 1 cobre; caixa 450 por cobre + ouro + gema) e **quem espera** (cliente barato some rápido; o rico espera). O bot escolhe pela regra fixa "receita com mais clientes na fila; empate, maior ouro por insumo".
5. Contador `RichClients` (nobres + colecionadores atendidos no distrito).

```
[minério cobre][minério ouro*][gemas brutas*]
[crisol cobre][crisol cobre 2*][crisol ouro*][crisol ouro 2*]
[lapidário*][lapidário 2*]
[bancada ourives][bancada 2*][bancada 3*]
[loja fina 3→5 vagas]              [Hall]
```

- **Major unlock:** `lapidario` (lapidação de gemas: colar e caixa de joias).
- **Charter:** produção completa + Renome 6 (1.250) + 3 Masterworks do distrito.

### 2.4 Royal Arsenal (`royal_arsenal`) — cotas reais

Pátio militar; estandartes azul e ouro, tambores. Família: kit do batedor, kit da guarda, insígnia real, espada de gala, alabarda de gala, armadura de gala. Kit inicial: depósito de minério, carroça de couro real, fornalha, forja de lâminas, bancada de kits, pátio de cotas (1 faixa), balcão de 4 vagas. CostBase 4.320; abertura 1.300.000.

**Mecânica nova — pátio de cotas (regra):**
1. O `patio_cotas` tem **faixas** (1 no kit; `patio_faixa2`, `patio_faixa3`). Cada faixa recebe um produto escolhido pelo jogador e abre um **bloco** desse produto.
2. Bloco: **10 unidades** no 1º; cada bloco fechado do mesmo produto aumenta o próximo em +2 (teto 30). Carregadores levam a peça ao pátio se houver faixa com aquele produto e bloco aberto com vaga; senão, ao balcão.
3. Ao fechar o bloco, a Coroa paga **1,3 × Σ `PriceOf(peça, qualidade)`** — é venda (entra no `GoldEarned` e na `RateEma`) — e dá **+2 de Renome**. Se o bloco fechar até **180 s** depois de aberto (240 s com `wk_matias`), paga **+25%** e +1 de Renome (**prazo só como bônus**: fora do prazo paga o normal, nunca menos).
4. Trocar o produto de uma faixa só vale para o próximo bloco; as unidades já entregues ficam no bloco em curso.
5. Contadores `QuotaBlocks` (por produto) e `QuotaOnTime` (por produto, no prazo).
6. **Royal Contracts** (§8) nascem aqui na narrativa: a `mesa_real` dá a 2ª vaga de contrato do império, e as encomendas Royal do distrito 4 nunca viram Common.

A renda alvo do §1 é a do balcão; o pátio soma até +30% (+62,5% no prazo) nas unidades que passam por ele. O econsim mede quanto da produção o bot manda ao pátio (portão no §16).

```
[depósito minério][carroça couro real][minério ouro*]
[fornalha][fornalha 2*][fornalha 3*][crisol ouro*]
[forja lâminas][prensa*]
[bancada kits][bancada kits 2*][bancada guarda*]
[mesa gravador*][forja cerimonial*][forja alabardas*][montagem gala*]
[pátio de cotas: faixa 1 | 2* | 3*]  [balcão 4 vagas]  [Hall]
```

- **Major unlock:** `mesa_real` (contratos reais permanentes).
- **Charter:** produção completa + Renome 7 (2.000) + 3 Masterworks + (1 Royal Contract **ou** 10 encomendas Royal no distrito).

### 2.5 Arcane Foundry (`arcane_foundry`) — carga arcana

Fundição arcana; roxo, ciano, runas, cristal, zumbido grave. Família: lanterna arcana, anel de cristal, amuleto arcano, lâmina rúnica, escudo rúnico, cajado de liga, liga celeste. Kit inicial: depósito de minério, depósito de cristal bruto, fornalha, cuba de cristal, altar de lanternas, balcão. CostBase 12.960; abertura 3.900.000.

**Mecânica nova — medidor de carga, sem sorteio (regra):**
1. Toda estação `Arcane` (altares e cadinho) tem um medidor **E** inteiro de 0 a 100 (em centésimos no núcleo: 0–10.000) e um **modo** escolhido pelo jogador: **Estável** (padrão) ou **Forçado**.
2. `E` cai `dreno` por segundo, contínuo: base **3/s**; `condensador` +2; `condensador2` +2; Ísis +1 no nível 1 e +0,5 nos níveis 3, 5, 7 e 9 (máx. +3).
3. **Estável:** a peça só começa se `E + carga ≤ 100`; senão o altar espera (estado "estabilizando", azul, com forma de onda). Soma `carga`. Nunca sobrecarrega.
4. **Forçado:** a peça começa assim que há insumo; soma **2 × carga** e sai **energizada** (q +20, fonte "energizada" no `QualityBreakdown()`). Se `E` passar de 100, a peça termina e o altar entra em **sobrecarga**: pausa de **10 s**, depois `E = 40`. Nada se perde (insumo e peça ficam).
5. Carga por peça: lanterna 15 · anel 12 · amuleto 18 · lâmina 20 · cajado 22 · escudo 25 · liga celeste 30. No kit, Estável e o tempo da lanterna (5 s, dreno 3, carga 15) empatam: a decisão de ritmo aparece desde o 1º minuto.
6. **A decisão:** Estável = vazão; Forçado = qualidade (+20 q) com risco de pausa. Investir em dreno (condensadores, Ísis) deixa o Forçado sem pausa. Contadores `EnergizedPieces` (por produto) e `StableStreak` (por altar: **peças energizadas seguidas sem sobrecarga**; zera na sobrecarga).

```
[depósito minério][cristal bruto][veio rúnico*]
[fornalha][cuba][cuba 2*][cuba 3*][forno rúnico*][forno rúnico 2*][fornalha de liga*]
[altar lanternas][altar anéis*][altar amuletos*][cadinho celeste*]
[altar lâminas*][altar escudos*][altar cajados*]
[balcão]                           [Hall]
```

- **Major unlock:** `veio_runico` (minério rúnico; o cristal já vem no kit para a mecânica existir desde o início — revisar com o GDD, que lista "minério rúnico e cristal").
- **Charter:** produção completa + Renome 8 (3.000) + 4 Masterworks + (2 Royal Contracts **ou** 20 encomendas Royal no distrito).

### 2.6 Grand Forge (`grand_forge`) — síntese e exportação

Catedral de forja; todas as paletas, sinos, vitrais. Família: sino da catedral, estátua brônzea, lâmina estelar, relicário real, coroa do reino, panóplia campeã. Kit inicial: depósito de cobre, crisol de bronze, fundição de sinos, balcão. CostBase 38.880; abertura 12.000.000.

**Mecânica nova — rotas de importação e lotes de exportação (regra):**
1. **Importados:** 4 receitas do distrito pedem produtos de outros distritos (§3.1). Eles chegam ao `cais` pelas **rotas do Mensageiro** (`wk_felix`): capacidade total **6 un/min** (+6 com `cais2`, +2 por nível do Félix; teto 30), uma unidade a cada `60 / capacidade` s, sempre para a rota cuja bancada de destino tem **menos estoque daquele importado** (empate: ordem da tabela §3.1).
2. Cada unidade importada **desconta `PriceOf(item, Normal) × 0,25` da renda de fundo do distrito de origem** naquele minuto (é o ouro que aquela peça daria em fundo), nunca abaixo de 0. Só importa com o jogo aberto; o offline nunca importa.
3. **Exportação:** no `patio_exportacao` o jogador escolhe o produto do lote (ou "o mais estocado"). A cada **90 s** (60 s com `patio_exportacao2`) o Mensageiro parte com até **10 unidades** (20 com `patio_exportacao2`; +2 por nível do Félix), pagando **1,25 × `PriceOf(peça, qualidade)`** — é venda. Parte com o que tiver (≥ 1); vazio não parte. Cada partida = 1 ciclo de exportação (contador `ExportLots`); cada importado consumido conta em `ImportedUnits`.
4. **Endgame:** depois da Carta, a Grand Forge é o lugar dos ciclos de exportação, das Masterworks lendárias (Renome 9–10) e do último contrato (`ctr_campeao`). O motivo para voltar aos distritos antigos é estrutural: as rotas e os contratos pedem peças de lá.

```
[minério cobre][minério][cratera estelar*][cais*]
[crisol bronze][crisol 2*][crisol 3*][fornalha*][fornalha 2*][forja estelar*]
[fundição sinos][fundição estátuas*]
[forja síntese*][banca relicário*][bancada coroas*][banca panóplia*]
[balcão][pátio de exportação*]      [Hall]
```

- **Major unlock:** `mensageiro` (rotas + exportação).
- **Carta da Grande Forja:** produção completa + Renome 9 (4.500) + 5 Masterworks + (3 Royal Contracts **ou** 30 encomendas Royal no distrito).

---

## 3. Itens (`ItemDef`)

38 produtos vendáveis (GDD §7 lista 38) + intermediários e brutos. `Raw` sai de depósito (sem receita); `Intermediate` não vai ao balcão; `Product` tem preço e qualidade. "distritos" = onde o item existe (depósito ou receita). Preço base = Normal; Fina 1,5×, Superior 2,5× (`ECONOMIA_WAVE_B.md` §1.1). Os preços dos distritos 2–6 saem do §1 (arredondados a 2 algarismos significativos).

<!-- tabela: itens -->
| id | tipo | família | tier | distritos | preço base | pt | en | es |
|---|---|---|---|---|---|---|---|---|
| `minerio` | Raw | - | 1 | 1,2,4,5,6 | — | minério | ore | mineral |
| `couro` | Raw | - | 2 | 2,4 | — | couro | leather | cuero |
| `minerio_cobre` | Raw | - | 3 | 3,6 | — | minério de cobre | copper ore | mineral de cobre |
| `minerio_ouro` | Raw | - | 3 | 3,4 | — | minério de ouro | gold ore | mineral de oro |
| `gema_bruta` | Raw | - | 3 | 3 | — | gema bruta | rough gem | gema en bruto |
| `cristal_bruto` | Raw | - | 5 | 5 | — | cristal bruto | raw crystal | cristal bruto |
| `minerio_runico` | Raw | - | 5 | 5 | — | minério rúnico | rune ore | mineral rúnico |
| `pedra_estelar` | Raw | - | 5 | 6 | — | pedra estelar | star stone | piedra estelar |
| `lingote` | Intermediate | - | 1 | 1,2,4,5,6 | — | lingote | ingot | lingote |
| `placa` | Intermediate | - | 2 | 2,4 | — | placa | plate | placa |
| `rebite` | Intermediate | - | 2 | 2 | — | rebite | rivet | remache |
| `liga` | Intermediate | - | 2 | 2,5 | — | liga | alloy | aleación |
| `lingote_cobre` | Intermediate | - | 3 | 3 | — | lingote de cobre | copper ingot | lingote de cobre |
| `lingote_ouro` | Intermediate | - | 3 | 3,4 | — | lingote de ouro | gold ingot | lingote de oro |
| `gema` | Intermediate | - | 3 | 3 | — | gema | gem | gema |
| `lamina` | Intermediate | - | 4 | 4 | — | lâmina | blade | hoja |
| `cristal` | Intermediate | - | 5 | 5 | — | cristal | crystal | cristal |
| `lingote_runico` | Intermediate | - | 5 | 5 | — | lingote rúnico | rune ingot | lingote rúnico |
| `bronze` | Intermediate | - | 5 | 6 | — | barra de bronze | bronze bar | barra de bronce |
| `aco_estelar` | Intermediate | - | 5 | 6 | — | aço estelar | star steel | acero estelar |
| `espada` | Product | espada | 1 | 1 | 10 | espada | sword | espada |
| `escudo` | Product | escudo | 1 | 1 | 25 | escudo | shield | escudo |
| `ferramenta` | Product | ferramenta | 1 | 1 | 16 | ferramenta | tool | herramienta |
| `joia` | Product | joia | 3 | 1 | 60 (80 com vitrine_joias) | joia | jewel | joya |
| `espada_longa` | Product | espada | 1 | 1 | 40 | espada longa | longsword | espada larga |
| `elmo` | Product | elmo | 2 | 2 | 140 | elmo | helm | yelmo |
| `maca` | Product | maca | 2 | 2 | 120 | maça | mace | maza |
| `grevas` | Product | grevas | 2 | 2 | 130 | grevas | greaves | grebas |
| `lanca` | Product | lanca | 2 | 2 | 70 | lança | spear | lanza |
| `manoplas` | Product | manoplas | 2 | 2 | 120 | manoplas | gauntlets | guanteletes |
| `machado_guerra` | Product | machado_guerra | 2 | 2 | 110 | machado bélico | war axe | hacha de guerra |
| `peitoral` | Product | peitoral | 2 | 2 | 230 | peitoral | breastplate | peto |
| `fivela` | Product | fivela | 3 | 3 | 110 | fivela | buckle | hebilla |
| `cinzel` | Product | cinzel | 3 | 3 | 190 | cinzel | chisel | cincel |
| `castical` | Product | castical | 3 | 3 | 290 | castiçal | candlestick | candelabro |
| `broche` | Product | broche | 3 | 3 | 240 | broche | brooch | broche |
| `calice` | Product | calice | 3 | 3 | 300 | cálice | chalice | cáliz |
| `colar` | Product | colar | 3 | 3 | 350 | colar | necklace | collar |
| `caixa_joias` | Product | caixa_joias | 3 | 3 | 450 | caixa de joias | jewel box | joyero |
| `kit_batedor` | Product | kit_batedor | 4 | 4 | 710 | kit do batedor | scout kit | kit explorador |
| `kit_guarda` | Product | kit_guarda | 4 | 4 | 1.200 | kit da guarda | guard kit | kit de guardia |
| `insignia_real` | Product | insignia_real | 4 | 4 | 550 | insígnia real | royal insignia | insignia real |
| `espada_gala` | Product | espada_gala | 4 | 4 | 820 | espada de gala | gala sword | espada de gala |
| `alabarda_cerimonial` | Product | alabarda_cerimonial | 4 | 4 | 970 | alabarda de gala | gala halberd | alabarda de gala |
| `armadura_gala` | Product | armadura_gala | 4 | 4 | 1.900 | armadura de gala | gala armor | armadura de gala |
| `lanterna_arcana` | Product | lanterna_arcana | 5 | 5 | 1.500 | lanterna arcana | arcane lantern | farol arcano |
| `anel_cristal` | Product | anel_cristal | 5 | 5 | 1.500 | anel de cristal | crystal ring | anillo cristal |
| `amuleto_arcano` | Product | amuleto_arcano | 5 | 5 | 1.800 | amuleto arcano | arcane amulet | amuleto arcano |
| `lamina_runica` | Product | lamina_runica | 5 | 5 | 2.400 | lâmina rúnica | rune blade | hoja rúnica |
| `escudo_runico` | Product | escudo_runico | 5 | 5 | 3.000 | escudo rúnico | rune shield | escudo rúnico |
| `cajado_liga` | Product | cajado_liga | 5 | 5 | 3.100 | cajado de liga | alloy staff | báculo aleado |
| `liga_celeste` | Product | liga_celeste | 5 | 5 | 3.300 | liga celeste | celestial alloy | aleación celeste |
| `sino_catedral` | Product | sino_catedral | 5 | 6 | 23.000 | sino da catedral | cathedral bell | campana mayor |
| `estatua_bronze` | Product | estatua_bronze | 5 | 6 | 24.000 | estátua brônzea | bronze statue | estatua bronce |
| `lamina_estelar` | Product | lamina_estelar | 5 | 6 | 21.000 | lâmina estelar | starblade | hoja estelar |
| `relicario_real` | Product | relicario_real | 5 | 6 | 20.000 | relicário real | royal reliquary | relicario real |
| `coroa_reino` | Product | coroa_reino | 5 | 6 | 22.000 | coroa do reino | realm crown | corona del reino |
| `panoplia_campeao` | Product | panoplia_campeao | 5 | 6 | 29.000 | panóplia campeã | champion set | panoplia campeón |


Nomes que mudaram para caber em 16 caracteres (o ID segue o GDD): "alabarda cerimonial" → **alabarda de gala**; "machado de guerra" → **machado bélico**; "estátua de bronze" → **estátua brônzea**; "panóplia do campeão" → **panóplia campeã**; "fragmento estelar" → **pedra estelar**.

### 3.1 Receitas (`RecipeDef`)

Uma receita por item produzido; a mesma receita vale em todos os distritos da coluna (a estação existe lá). `insumos` = `item×quantidade`. Toda receita usa só itens com depósito ou receita **no mesmo distrito**, exceto os importados do Grand Forge (tabela abaixo).

<!-- tabela: receitas -->
| id | saída | qtd | insumos | estação | tempo base (s) | distritos | desbloqueio |
|---|---|---|---|---|---|---|---|
| `rec_lingote` | `lingote` | 1 | minerio×1 | fornalha | 4,0 | 1,2,4,5,6 | inicio |
| `rec_espada` | `espada` | 1 | lingote×1 | bigorna | 5,0 | 1 | inicio |
| `rec_escudo` | `escudo` | 1 | lingote×2 | escudos | 7,5 | 1 | upgrade escudos |
| `rec_ferramenta` | `ferramenta` | 1 | lingote×1 | ferramentas | 5,0 | 1 | upgrade ferramentas |
| `rec_joia` | `joia` | 1 | lingote×2 | joalheria | 10,0 | 1 | upgrade joalheria |
| `rec_espada_longa` | `espada_longa` | 1 | lingote×2 | bigorna_longa | 7,5 | 1 | bp_rec_espada_longa + upgrade bigorna_longa |
| `rec_placa` | `placa` | 1 | lingote×1 | prensa | 3,0 | 2,4 | inicio (d2) / upgrade bancada_guarda (d4) |
| `rec_rebite` | `rebite` | 4 | lingote×1 | rebitadeira | 4,0 | 2 | inicio |
| `rec_liga` | `liga` | 1 | lingote×2 | fornalha_liga | 6,0 | 2,5 | upgrade fornalha_liga (d2) / fornalha_liga_arcana (d5) |
| `rec_elmo` | `elmo` | 1 | placa×2 + rebite×2 | montagem_elmo | 6,0 | 2 | inicio |
| `rec_maca` | `maca` | 1 | lingote×2 + rebite×2 | bigorna_pesada | 6,0 | 2 | upgrade bigorna_pesada |
| `rec_grevas` | `grevas` | 1 | placa×2 + couro×1 | montagem_grevas | 6,0 | 2 | upgrade carroca_curtidor |
| `rec_lanca` | `lanca` | 1 | lingote×1 + couro×1 | forja_hastes | 4,0 | 2 | upgrade forja_hastes |
| `rec_manoplas` | `manoplas` | 1 | placa×1 + rebite×2 + couro×1 | montagem_manoplas | 7,0 | 2 | upgrade montagem_manoplas |
| `rec_machado_guerra` | `machado_guerra` | 1 | lingote×2 + couro×1 | forja_machados | 6,0 | 2 | upgrade forja_machados |
| `rec_peitoral` | `peitoral` | 1 | placa×3 + rebite×4 + couro×1 | montagem_peitoral | 9,0 | 2 | upgrade montagem_peitoral |
| `rec_lingote_cobre` | `lingote_cobre` | 1 | minerio_cobre×1 | crisol_cobre | 3,0 | 3 | inicio |
| `rec_lingote_ouro` | `lingote_ouro` | 1 | minerio_ouro×1 | crisol_ouro | 4,0 | 3,4 | upgrade veio_ouro (d3) / crisol_real (d4) |
| `rec_gema` | `gema` | 1 | gema_bruta×1 | lapidario | 6,0 | 3 | upgrade lapidario |
| `rec_fivela` | `fivela` | 1 | lingote_cobre×1 | bancada_ourives | 3,0 | 3 | inicio |
| `rec_cinzel` | `cinzel` | 1 | lingote_cobre×2 | bancada_ourives | 4,0 | 3 | inicio |
| `rec_castical` | `castical` | 1 | lingote_cobre×3 | bancada_ourives | 6,0 | 3 | upgrade receita_castical |
| `rec_broche` | `broche` | 1 | lingote_ouro×1 + lingote_cobre×1 | bancada_ourives | 5,0 | 3 | upgrade veio_ouro |
| `rec_calice` | `calice` | 1 | lingote_ouro×2 | bancada_ourives | 7,0 | 3 | upgrade receita_calice |
| `rec_colar` | `colar` | 1 | lingote_ouro×1 + gema×1 | bancada_ourives | 8,0 | 3 | upgrade lapidario |
| `rec_caixa_joias` | `caixa_joias` | 1 | lingote_cobre×1 + lingote_ouro×1 + gema×1 | bancada_ourives | 10,0 | 3 | upgrade receita_caixa |
| `rec_lamina` | `lamina` | 1 | lingote×1 | forja_laminas | 3,0 | 4 | inicio |
| `rec_kit_batedor` | `kit_batedor` | 1 | lamina×1 + couro×2 | bancada_kits | 6,0 | 4 | inicio |
| `rec_kit_guarda` | `kit_guarda` | 1 | lamina×1 + placa×2 + couro×1 | bancada_guarda | 8,0 | 4 | upgrade bancada_guarda |
| `rec_insignia_real` | `insignia_real` | 1 | lingote_ouro×1 + couro×1 | mesa_gravador | 5,0 | 4 | upgrade crisol_real |
| `rec_espada_gala` | `espada_gala` | 1 | lamina×1 + lingote_ouro×1 | forja_cerimonial | 7,0 | 4 | upgrade forja_cerimonial |
| `rec_alabarda_cerimonial` | `alabarda_cerimonial` | 1 | lingote×2 + lingote_ouro×1 | forja_alabardas | 8,0 | 4 | upgrade forja_alabardas |
| `rec_armadura_gala` | `armadura_gala` | 1 | placa×3 + lingote_ouro×2 + couro×1 | montagem_gala | 12,0 | 4 | upgrade montagem_gala |
| `rec_cristal` | `cristal` | 1 | cristal_bruto×1 | cuba_cristal | 5,0 | 5 | inicio |
| `rec_lingote_runico` | `lingote_runico` | 1 | minerio_runico×1 | fornalha_runica | 5,0 | 5 | upgrade veio_runico |
| `rec_lanterna_arcana` | `lanterna_arcana` | 1 | lingote×1 + cristal×1 | altar_lanternas | 5,0 | 5 | inicio |
| `rec_anel_cristal` | `anel_cristal` | 1 | cristal×2 | altar_aneis | 4,0 | 5 | upgrade altar_aneis |
| `rec_amuleto_arcano` | `amuleto_arcano` | 1 | lingote_runico×1 + cristal×1 | altar_amuletos | 6,0 | 5 | upgrade veio_runico |
| `rec_lamina_runica` | `lamina_runica` | 1 | lingote_runico×2 + lingote×1 | altar_laminas | 7,0 | 5 | upgrade altar_laminas |
| `rec_escudo_runico` | `escudo_runico` | 1 | lingote_runico×2 + lingote×2 | altar_escudos | 9,0 | 5 | upgrade altar_escudos |
| `rec_cajado_liga` | `cajado_liga` | 1 | liga×1 + cristal×1 + lingote_runico×1 | altar_cajados | 8,0 | 5 | upgrade fornalha_liga_arcana |
| `rec_liga_celeste` | `liga_celeste` | 1 | lingote_runico×1 + cristal×1 + liga×1 | cadinho_celeste | 10,0 | 5 | upgrade cadinho_celeste |
| `rec_bronze` | `bronze` | 1 | minerio_cobre×1 | crisol_bronze | 3,0 | 6 | inicio |
| `rec_aco_estelar` | `aco_estelar` | 1 | lingote×1 + pedra_estelar×1 | forja_estelar | 6,0 | 6 | upgrade cratera_estelar |
| `rec_sino_catedral` | `sino_catedral` | 1 | bronze×8 | fundicao_sinos | 18,0 | 6 | inicio |
| `rec_estatua_bronze` | `estatua_bronze` | 1 | bronze×6 + aco_estelar×1 | fundicao_estatuas | 16,0 | 6 | upgrade cratera_estelar |
| `rec_lamina_estelar` | `lamina_estelar` | 1 | aco_estelar×2 + lamina_runica×1 | forja_sintese | 10,0 | 6 | upgrade forja_sintese |
| `rec_relicario_real` | `relicario_real` | 1 | caixa_joias×1 + lanterna_arcana×1 + bronze×2 | bancada_relicarios | 12,0 | 6 | upgrade bancada_relicarios |
| `rec_coroa_reino` | `coroa_reino` | 1 | colar×1 + insignia_real×1 + anel_cristal×1 | bancada_coroas | 12,0 | 6 | upgrade bancada_coroas |
| `rec_panoplia_campeao` | `panoplia_campeao` | 1 | peitoral×1 + espada_gala×1 + escudo_runico×1 + aco_estelar×1 | bancada_panoplia | 14,0 | 6 | upgrade bancada_panoplia |


**Importados do Grand Forge** (rotas do Mensageiro, §2.6):

<!-- tabela: importacoes -->
| item | origem | preço no balcão de origem | custo na renda de fundo da origem (por unidade) |
|---|---|---|---|
| `peitoral` | armorers_row | 230 | 58 |
| `colar` | artisan_quarter | 350 | 88 |
| `caixa_joias` | artisan_quarter | 450 | 112 |
| `insignia_real` | royal_arsenal | 550 | 138 |
| `espada_gala` | royal_arsenal | 820 | 205 |
| `lanterna_arcana` | arcane_foundry | 1.500 | 375 |
| `anel_cristal` | arcane_foundry | 1.500 | 375 |
| `lamina_runica` | arcane_foundry | 2.400 | 600 |
| `escudo_runico` | arcane_foundry | 3.000 | 750 |


---

## 4. Estações (`StationDef`)

`tipo` = `Kind` do núcleo (Deposit, Furnace, Crafter, Counter) e os novos: `Assembly` (multi-insumo, §2.2), `Configurable` (§2.3), `Quota` (§2.4), `Arcane` (§2.5), `Import`/`Export` (§2.6), `Conveyor`, `Hall`. A "família" agrupa nível de estação, técnica, oficial e Masterwork.

<!-- tabela: estacoes -->
| id | distrito | pt | en | es | tipo | família (nível e qualidade) | função |
|---|---|---|---|---|---|---|---|
| `deposito` | old_forge | Depósito | Ore Pile | Depósito | Deposit | - | minério; existe |
| `fornalha` | old_forge | Fornalha | Furnace | Horno | Furnace | - | minério → lingote; existe (também nos distritos 2, 4, 5, 6) |
| `bigorna` | old_forge | Bigorna | Anvil | Yunque | Crafter | bigornas | espada; existe |
| `escudos` | old_forge | Bancada escudos | Shield Bench | Banco escudos | Crafter | escudos | escudo; existe |
| `ferramentas` | old_forge | Bancada ferram. | Tool Bench | Banco herram. | Crafter | ferramentas | ferramenta; existe |
| `joalheria` | old_forge | Joalheria | Jewelry | Joyería | Crafter | joalheria | joia; existe |
| `bigorna_longa` | old_forge | Bigorna longa | Long Anvil | Yunque largo | Crafter | bigornas | espada longa; NOVA (Meta) |
| `esteira` | old_forge | Esteira | Conveyor | Cinta | Conveyor | - | fornalha A → bigorna A; existe |
| `balcao` | old_forge | Balcão | Counter | Mostrador | Counter | - | 4 → 8 vagas; existe (todo distrito tem um) |
| `loja_joias` | old_forge | Loja de joias | Jewel Shop | Tienda joyas | Counter | - | fila 3 → 5; existe |
| `hall` | old_forge | Hall | Hall | Salón | Hall | - | 10 pedestais por distrito (Wave B); todo distrito tem um |
| `carroca_couro` | armorers_row | Carroça couro | Leather Cart | Carro de cuero | Deposit | - | couro (d2: major unlock; d4: desde o início) |
| `prensa` | armorers_row | Prensa | Press | Prensa | Furnace | - | lingote → placa (d2, d4) |
| `rebitadeira` | armorers_row | Rebitadeira | Riveter | Remachadora | Furnace | - | 1 lingote → 4 rebites |
| `fornalha_liga` | armorers_row | Fornalha liga | Alloy Furnace | Horno aleación | Furnace | - | 2 lingotes → liga; opera com Fundidor (d2, d5) |
| `montagem_elmo` | armorers_row | Montagem elmo | Helm Bench | Banco yelmos | Assembly | montagem | elmo |
| `montagem_grevas` | armorers_row | Montagem grevas | Greave Bench | Banco grebas | Assembly | montagem | grevas |
| `montagem_manoplas` | armorers_row | Mont. manoplas | Gauntlet Bench | Banco guantes | Assembly | montagem | manoplas |
| `montagem_peitoral` | armorers_row | Mont. peitoral | Plate Bench | Banco petos | Assembly | montagem | peitoral |
| `bigorna_pesada` | armorers_row | Bigorna pesada | Heavy Anvil | Yunque pesado | Assembly | forja_armas | maça |
| `forja_hastes` | armorers_row | Forja de hastes | Haft Forge | Forja de astas | Assembly | forja_armas | lança |
| `forja_machados` | armorers_row | Forja machados | Axe Forge | Forja hachas | Assembly | forja_armas | machado bélico |
| `deposito_cobre` | artisan_quarter | Minério cobre | Copper Pile | Pila de cobre | Deposit | - | minério de cobre (d3, d6) |
| `deposito_ouro` | artisan_quarter | Minério ouro | Gold Pile | Pila de oro | Deposit | - | minério de ouro (d3, d4) |
| `deposito_gemas` | artisan_quarter | Gemas brutas | Rough Gems | Gemas brutas | Deposit | - | gema bruta |
| `crisol_cobre` | artisan_quarter | Crisol cobre | Copper Crucible | Crisol cobre | Furnace | - | minério de cobre → lingote |
| `crisol_ouro` | artisan_quarter | Crisol ouro | Gold Crucible | Crisol oro | Furnace | - | minério de ouro → lingote (d3, d4) |
| `lapidario` | artisan_quarter | Lapidário | Gem Cutter | Lapidario | Furnace | - | gema bruta → gema |
| `bancada_ourives` | artisan_quarter | Bancada ourives | Goldsmith Bench | Banco orfebre | Configurable | ourivesaria | qualquer receita do d3 liberada; troca = 4 s |
| `loja_rica` | artisan_quarter | Loja fina | Fine Shop | Tienda fina | Counter | - | balcão do d3: 3 → 5 vagas |
| `forja_laminas` | royal_arsenal | Forja lâminas | Blade Forge | Forja de hojas | Furnace | - | lingote → lâmina |
| `bancada_kits` | royal_arsenal | Bancada kits | Kit Bench | Banco de kits | Assembly | kits | kit do batedor |
| `bancada_guarda` | royal_arsenal | Bancada guarda | Guard Bench | Banco guardia | Assembly | kits | kit da guarda |
| `mesa_gravador` | royal_arsenal | Mesa gravador | Engraver Desk | Mesa grabador | Assembly | cerimonial | insígnia real |
| `forja_cerimonial` | royal_arsenal | Forja cerimonial | Gala Forge | Forja de gala | Assembly | cerimonial | espada de gala |
| `forja_alabardas` | royal_arsenal | Forja alabardas | Halberd Forge | Forja alabarda | Assembly | cerimonial | alabarda de gala |
| `montagem_gala` | royal_arsenal | Montagem gala | Gala Bench | Banco de gala | Assembly | cerimonial | armadura de gala |
| `patio_cotas` | royal_arsenal | Pátio de cotas | Quota Yard | Patio de cupos | Quota | - | blocos de cota (1 → 3 faixas) |
| `deposito_cristal` | arcane_foundry | Cristal bruto | Raw Crystal | Cristal bruto | Deposit | - | cristal bruto |
| `deposito_runico` | arcane_foundry | Veio rúnico | Rune Vein | Veta rúnica | Deposit | - | minério rúnico (major unlock) |
| `cuba_cristal` | arcane_foundry | Cuba de cristal | Crystal Vat | Cuba cristal | Furnace | - | cristal bruto → cristal |
| `fornalha_runica` | arcane_foundry | Forno rúnico | Rune Furnace | Horno rúnico | Furnace | - | minério rúnico → lingote rúnico |
| `altar_lanternas` | arcane_foundry | Altar lanternas | Lantern Altar | Altar faroles | Arcane | altar_cristal | lanterna arcana; medidor |
| `altar_aneis` | arcane_foundry | Altar de anéis | Ring Altar | Altar anillos | Arcane | altar_cristal | anel de cristal; medidor |
| `altar_amuletos` | arcane_foundry | Altar amuletos | Amulet Altar | Altar amuletos | Arcane | altar_cristal | amuleto arcano; medidor |
| `cadinho_celeste` | arcane_foundry | Cadinho celeste | Sky Crucible | Crisol celeste | Arcane | altar_cristal | liga celeste; medidor |
| `altar_laminas` | arcane_foundry | Altar lâminas | Blade Altar | Altar de hojas | Arcane | altar_runa | lâmina rúnica; medidor |
| `altar_escudos` | arcane_foundry | Altar escudos | Shield Altar | Altar escudos | Arcane | altar_runa | escudo rúnico; medidor |
| `altar_cajados` | arcane_foundry | Altar cajados | Staff Altar | Altar báculos | Arcane | altar_runa | cajado de liga; medidor |
| `deposito_estelar` | grand_forge | Cratera estelar | Star Crater | Cráter estelar | Deposit | - | pedra estelar |
| `cais` | grand_forge | Cais | Dock | Muelle | Import | - | chegada das rotas do Mensageiro |
| `crisol_bronze` | grand_forge | Crisol bronze | Bronze Crucible | Crisol bronce | Furnace | - | minério de cobre → barra de bronze |
| `forja_estelar` | grand_forge | Forja estelar | Star Forge | Forja estelar | Furnace | - | lingote + pedra estelar → aço estelar |
| `fundicao_sinos` | grand_forge | Fundição sinos | Bell Foundry | Fundición | Crafter | fundicao | sino da catedral |
| `fundicao_estatuas` | grand_forge | Fundição estátua | Statue Foundry | Fundición est. | Crafter | fundicao | estátua brônzea |
| `forja_sintese` | grand_forge | Forja síntese | Synth Forge | Forja síntesis | Assembly | sintese | lâmina estelar |
| `bancada_relicarios` | grand_forge | Banca relicário | Reliquary Bench | Banco relicario | Assembly | sintese | relicário real |
| `bancada_coroas` | grand_forge | Bancada coroas | Crown Bench | Banco coronas | Assembly | sintese | coroa do reino |
| `bancada_panoplia` | grand_forge | Banca panóplia | Panoply Bench | Banco panoplia | Assembly | sintese | panóplia campeã |
| `patio_exportacao` | grand_forge | Pátio exportação | Export Yard | Patio export. | Export | - | lotes do Mensageiro |


---

## 5. Upgrades por distrito (`UpgradeDef`)

Categoria (`ECONOMIA_WAVE_B.md` §8.7): `Production` (conta na produção completa e no teto offline), `Luxury` (sem efeito), `Meta` (fora dos dois). `tier` = posição na fórmula do distrito. "requer" = pré-requisito (`*produção completa` = todos os `Production` do distrito). Os do menu dizem "(menu)"; os outros são pads no chão, como hoje.

### 5.1 Old Forge — os 29 atuais + 1 (custos vigentes do `Tuning`)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole` | Production | — | Fole | (Loc.cs) | (Loc.cs) | fornalha 4,0 → 2,5 s (menu) | - | 50 |
| `bigorna2` | Production | — | Bigorna 2 | (Loc.cs) | (Loc.cs) | 2ª bigorna (espadas) | - | 65 |
| `ajudante1` | Production | — | Ajudante | (Loc.cs) | (Loc.cs) | minério → fornalhas (wk_ajudante1) | - | 85 |
| `escudos` | Production | — | Escudos | (Loc.cs) | (Loc.cs) | nova linha: escudo (2 lingotes, 25) | - | 110 |
| `mochila` | Production | — | Mochila | (Loc.cs) | (Loc.cs) | ferreiro carrega 3 → 6 (menu; vale em todo distrito) | - | 145 |
| `ajudante2` | Production | — | Ajudante 2 | (Loc.cs) | (Loc.cs) | lingotes → bancadas (wk_ajudante2) | ajudante1 | 185 |
| `esteira` | Production | — | Esteira | (Loc.cs) | (Loc.cs) | fornalha A → bigorna A sem andar | - | 240 |
| `fole_duplo` | Production | — | Fole duplo | (Loc.cs) | (Loc.cs) | fornalha 2,5 → 1,6 s (menu) | fole | 315 |
| `botas` | Production | — | Botas | (Loc.cs) | (Loc.cs) | ferreiro 3,0 → 4,2 m/s (menu; vale em todo distrito) | - | 410 |
| `ferramentas` | Production | — | Ferramentas | (Loc.cs) | (Loc.cs) | nova linha: ferramenta (1 lingote, 16) | escudos | 530 |
| `vitrine` | Production | — | Vitrine | (Loc.cs) | (Loc.cs) | estoque 5 → 10, clientes ×0,7 (menu) | - | 690 |
| `fornalha2` | Production | — | Fornalha 2 | (Loc.cs) | (Loc.cs) | 2ª fornalha | bigorna2 | 895 |
| `ajudante3` | Production | — | Ajudante 3 | (Loc.cs) | (Loc.cs) | bancadas → balcão (wk_ajudante3) | ajudante2 | 1.165 |
| `ajudantes_ageis` | Production | — | Ajudantes ágeis | (Loc.cs) | (Loc.cs) | ajudantes 2,4 → 3,4 m/s, carga 2 → 4 (menu) | ajudante1 | 1.515 |
| `martelo_veloz` | Production | — | Martelo veloz | (Loc.cs) | (Loc.cs) | bancadas 5 → 3 s (menu) | - | 1.970 |
| `corredor` | Production | — | Corredor | (Loc.cs) | (Loc.cs) | abre a rua lateral | - | 690 |
| `joalheria` | Production | — | Joalheria | (Loc.cs) | (Loc.cs) | nova linha: joia (2 lingotes, 60) | corredor | 1.515 |
| `joalheiro1` | Production | — | Joalheiro | (Loc.cs) | (Loc.cs) | lingotes → joalheria, joias → loja (wk_joalheiro1) | joalheria | 2.200 |
| `lupa` | Production | — | Lupa | (Loc.cs) | (Loc.cs) | joalheria ×0,6 (menu) | joalheria | 5.500 |
| `vitrine_joias` | Production | — | Vitrine de joias | (Loc.cs) | (Loc.cs) | joia 60 → 80, fila 3 → 5 (menu) | joalheria | 9.000 |
| `fachada_nobre` | Luxury | — | Fachada nobre | (Loc.cs) | (Loc.cs) | luxo, sem efeito de produção | vitrine_joias | 14.000 |
| `piso_oficina` | Luxury | — | Piso de oficina | (Loc.cs) | (Loc.cs) | luxo | fachada_nobre | 18.000 |
| `joalheria_real` | Luxury | — | Joalheria real | (Loc.cs) | (Loc.cs) | luxo | piso_oficina | 22.000 |
| `mineiro` | Production | — | Mineiro | (Loc.cs) | (Loc.cs) | 2º minério → fornalhas (wk_mineiro) | joalheiro1 | 3.000 |
| `joalheiro2` | Production | — | Joalheiro 2 | (Loc.cs) | (Loc.cs) | 2º joalheiro (wk_joalheiro2) | mineiro | 2.400 |
| `balcao5` | Production | — | Balcão 5 vagas | (Loc.cs) | (Loc.cs) | 4 → 5 vagas (menu) | vitrine | 150 |
| `balcao6` | Production | — | Balcão 6 vagas | (Loc.cs) | (Loc.cs) | 5 → 6 (menu) | balcao5 | 175 |
| `balcao7` | Production | — | Balcão 7 vagas | (Loc.cs) | (Loc.cs) | 6 → 7 (menu) | balcao6 | 200 |
| `balcao8` | Production | — | Balcão 8 vagas | (Loc.cs) | (Loc.cs) | 7 → 8 (menu) | balcao7 | 225 |
| `bigorna_longa` | Meta | — | Bigorna longa | Long Anvil | Yunque largo | nova linha (Meta): espada longa (2 lingotes, 40); fora da produção completa | martelo_veloz + bp_rec_espada_longa | 6.000 |


### 5.2 Armorer's Row (CostBase 480, 1º produtivo 480; renda de fundo ao chegar 600/min)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole_couro` | Production | 0 | Fole de couro | Leather Bellows | Fuelle de cuero | fornalhas 4,0 → 2,5 s (menu) | - | 480 |
| `prensa2` | Production | 1 | Prensa 2 | Press 2 | Prensa 2 | 2ª prensa de placas | - | 625 |
| `ferraz1` | Production | 2 | Ferraz 1 | Ferraz 1 | Ferraz 1 | turma Ferraz, 1º corpo: minério → fornalhas | - | 810 |
| `bigorna_pesada` | Production | 3 | Maças | Maces | Mazas | nova linha: maça (bigorna pesada) | - | 1.055 |
| `ferraz2` | Production | 4 | Ferraz 2 | Ferraz 2 | Ferraz 2 | 2º corpo: lingotes → prensas, rebitadeira, bigorna pesada | ferraz1 | 1.370 |
| `trilho_placas` | Production | 5 | Trilho de placas | Plate Rail | Riel de placas | prensa A → montagem de elmos sem andar | - | 1.780 |
| `carroca_curtidor` | Production | 6 | Carroça curtidor | Tanner's Cart | Carro curtidor | MAJOR: depósito de couro + nova linha grevas | - | 2.315 |
| `forja_hastes` | Production | 7 | Lanças | Spears | Lanzas | nova linha: lança | carroca_curtidor | 3.010 |
| `fornalha_armeiro2` | Production | 8 | Fornalha 2 | Furnace 2 | Horno 2 | 2ª fornalha | - | 3.915 |
| `ferraz3` | Production | 9 | Ferraz 3 | Ferraz 3 | Ferraz 3 | 3º corpo: produtos → balcão | ferraz2 | 5.090 |
| `montagem_manoplas` | Production | 10 | Manoplas | Gauntlets | Guanteletes | nova linha: manoplas | carroca_curtidor | 6.615 |
| `fornalha_liga` | Production | 11 | Fornalha de liga | Alloy Furnace | Horno aleación | MAJOR: Fundidor — 2 lingotes → liga; libera contratar wk_vulcana; slot de liga em montagens e forjas | fornalha_armeiro2 | 8.600 |
| `forja_machados` | Production | 12 | Machados | War Axes | Hachas | nova linha: machado bélico | carroca_curtidor | 11.000 |
| `fole_couro_duplo` | Production | 13 | Fole duplo | Twin Bellows | Fuelle doble | fornalhas 2,5 → 1,6 s (menu) | fole_couro | 15.000 |
| `ferraz4` | Production | 14 | Ferraz 4 | Ferraz 4 | Ferraz 4 | 4º corpo: couro e liga → montagens e forjas | ferraz3 | 19.000 |
| `gabaritos` | Production | 15 | Gabaritos | Jigs | Plantillas | montagens e forjas ×0,6 (menu) | - | 25.000 |
| `montagem_peitoral` | Production | 16 | Peitorais | Breastplates | Petos | nova linha: peitoral | montagem_manoplas | 32.000 |
| `rebitadeira2` | Production | 17 | Rebitadeira 2 | Riveter 2 | Remachadora 2 | 2ª rebitadeira | - | 42.000 |
| `estandartes_armeiro` | Luxury | — | Estandartes | Banners | Estandartes | luxo | *produção completa | 42.000 |
| `rua_calcada` | Luxury | — | Rua calçada | Paved Street | Calle empedrada | luxo | estandartes_armeiro | 54.000 |
| `manequins` | Luxury | — | Manequins | Mannequins | Maniquíes | luxo | rua_calcada | 66.000 |


### 5.3 Artisan Quarter (CostBase 1.440; 1º produtivo 1.440; fundo ao chegar 2.400/min)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole_ourives` | Production | 0 | Fole de ourives | Jeweler Bellows | Fuelle orfebre | crisóis ×0,625 (menu) | - | 1.440 |
| `bancada_ourives2` | Production | 1 | Bancada 2 | Bench 2 | Banco 2 | 2ª bancada de ourives (configurável) | - | 1.870 |
| `dourado1` | Production | 2 | Dourado 1 | Dourado 1 | Dourado 1 | turma Dourado, 1º corpo: minérios → crisóis | - | 2.435 |
| `receita_castical` | Production | 3 | Castiçais | Candlesticks | Candelabros | receita castiçal nas bancadas | - | 3.165 |
| `veio_ouro` | Production | 4 | Veio de ouro | Gold Vein | Veta de oro | depósito de minério de ouro + crisol de ouro; receita broche | - | 4.115 |
| `dourado2` | Production | 5 | Dourado 2 | Dourado 2 | Dourado 2 | 2º corpo: lingotes → bancadas | dourado1 | 5.345 |
| `vitrine_luxo` | Production | 6 | Vitrine de luxo | Luxury Display | Vitrina de lujo | loja 3 → 5 vagas; clientes ricos ×0,7 no intervalo (menu) | - | 6.950 |
| `receita_calice` | Production | 7 | Cálices | Chalices | Cálices | receita cálice | veio_ouro | 9.035 |
| `crisol_cobre2` | Production | 8 | Crisol 2 | Crucible 2 | Crisol 2 | 2º crisol de cobre | - | 12.000 |
| `dourado3` | Production | 9 | Dourado 3 | Dourado 3 | Dourado 3 | 3º corpo: produtos → loja | dourado2 | 15.000 |
| `lapidario` | Production | 10 | Lapidário | Gem Cutter | Lapidario | MAJOR: lapidação — depósito de gemas brutas + lapidário; receita colar | veio_ouro | 20.000 |
| `bancada_ourives3` | Production | 11 | Bancada 3 | Bench 3 | Banco 3 | 3ª bancada de ourives | bancada_ourives2 | 26.000 |
| `receita_caixa` | Production | 12 | Caixas de joias | Jewel Boxes | Joyeros | receita caixa de joias | lapidario | 34.000 |
| `fole_ourives_duplo` | Production | 13 | Fole duplo | Twin Bellows | Fuelle doble | crisóis e lapidário ×0,64 (menu) | fole_ourives | 44.000 |
| `lupa_ourives` | Production | 14 | Lupa de ourives | Jeweler Loupe | Lupa orfebre | bancadas ×0,6 (menu) | - | 57.000 |
| `lapidario2` | Production | 15 | Lapidário 2 | Gem Cutter 2 | Lapidario 2 | 2º lapidário | lapidario | 74.000 |
| `crisol_ouro2` | Production | 16 | Crisol de ouro 2 | Gold Crucible 2 | Crisol de oro 2 | 2º crisol de ouro | veio_ouro | 96.000 |
| `troca_rapida` | Production | 17 | Troca rápida | Quick Swap | Cambio rápido | troca de receita na bancada 4 → 1 s (menu) | bancada_ourives3 | 120.000 |
| `fachada_dourada` | Luxury | — | Fachada dourada | Gilded Front | Fachada dorada | luxo | *produção completa | 130.000 |
| `lanternas_rua` | Luxury | — | Lanternas | Lanterns | Faroles | luxo | fachada_dourada | 160.000 |
| `fonte_bairro` | Luxury | — | Fonte do bairro | Fountain | Fuente | luxo | lanternas_rua | 200.000 |


### 5.4 Royal Arsenal (CostBase 4.320; 1º produtivo 4.320; fundo ao chegar 7.800/min)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole_real` | Production | 0 | Fole real | Royal Bellows | Fuelle real | fornalha e crisol ×0,625 (menu) | - | 4.320 |
| `bancada_kits2` | Production | 1 | Bancada kits 2 | Kit Bench 2 | Banco kits 2 | 2ª bancada de kits do batedor | - | 5.615 |
| `cadete1` | Production | 2 | Cadete 1 | Cadet 1 | Cadete 1 | turma Cadetes, 1º corpo: minério → fornalhas | - | 7.300 |
| `bancada_guarda` | Production | 3 | Kits da guarda | Guard Kits | Kits guardia | prensa + nova linha kit da guarda | - | 9.490 |
| `cadete2` | Production | 4 | Cadete 2 | Cadet 2 | Cadete 2 | 2º corpo: lingotes → forja de lâminas e prensa | cadete1 | 12.000 |
| `patio_faixa2` | Production | 5 | Faixa do pátio 2 | Yard Lane 2 | Carril patio 2 | pátio de cotas: 2 produtos ao mesmo tempo | - | 16.000 |
| `crisol_real` | Production | 6 | Crisol real | Royal Crucible | Crisol real | depósito de ouro + crisol + nova linha insígnia real (mesa do gravador) | - | 21.000 |
| `forja_cerimonial` | Production | 7 | Espadas de gala | Gala Swords | Espadas de gala | nova linha: espada de gala | crisol_real | 27.000 |
| `fornalha_real2` | Production | 8 | Fornalha 2 | Furnace 2 | Horno 2 | 2ª fornalha | - | 35.000 |
| `cadete3` | Production | 9 | Cadete 3 | Cadet 3 | Cadete 3 | 3º corpo: produtos → pátio e balcão | cadete2 | 46.000 |
| `forja_alabardas` | Production | 10 | Alabardas | Halberds | Alabardas | nova linha: alabarda de gala | crisol_real | 60.000 |
| `mesa_real` | Production | 11 | Mesa real | Royal Desk | Mesa real | MAJOR: contratos permanentes — 2ª vaga de Royal Contract no império; encomenda Royal do d4 nunca vira Common | patio_faixa2 | 77.000 |
| `montagem_gala` | Production | 12 | Armaduras gala | Gala Armor | Armaduras gala | nova linha: armadura de gala | forja_cerimonial | 100.000 |
| `fole_real_duplo` | Production | 13 | Fole duplo | Twin Bellows | Fuelle doble | ×0,64 (menu) | fole_real | 130.000 |
| `cadete4` | Production | 14 | Cadete 4 | Cadet 4 | Cadete 4 | 4º corpo: couro e ouro → bancadas | cadete3 | 170.000 |
| `ferragens_reais` | Production | 15 | Ferragens reais | Royal Tools | Herrajes reales | bancadas e forjas ×0,6 (menu) | - | 220.000 |
| `patio_faixa3` | Production | 16 | Faixa do pátio 3 | Yard Lane 3 | Carril patio 3 | 3ª faixa do pátio | patio_faixa2 | 290.000 |
| `fornalha_real3` | Production | 17 | Fornalha 3 | Furnace 3 | Horno 3 | 3ª fornalha | fornalha_real2 | 370.000 |
| `flamulas_reais` | Luxury | — | Flâmulas reais | Royal Pennants | Pendones reales | luxo | *produção completa | 380.000 |
| `portao_real` | Luxury | — | Portão real | Royal Gate | Portón real | luxo | flamulas_reais | 490.000 |
| `torre_relogio` | Luxury | — | Torre do relógio | Clock Tower | Torre del reloj | luxo | portao_real | 590.000 |
| `selo_arsenal` | Meta | — | Selo do Arsenal | Arsenal Seal | Sello Arsenal | q +25 em toda estação de fabricar do distrito (Meta) | produção completa | 110.000 |


### 5.5 Arcane Foundry (CostBase 12.960; 1º produtivo 13.000; fundo ao chegar 24.000/min)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole_arcano` | Production | 0 | Fole arcano | Arcane Bellows | Fuelle arcano | fornalha, cuba e forno rúnico ×0,625 (menu) | - | 13.000 |
| `altar_aneis` | Production | 1 | Anéis de cristal | Crystal Rings | Anillos cristal | nova linha: anel de cristal | - | 17.000 |
| `runa1` | Production | 2 | Aprendiz 1 | Apprentice 1 | Aprendiz 1 | turma Aprendizes, 1º corpo: minério e cristal bruto → fornalha e cuba | - | 22.000 |
| `cuba2` | Production | 3 | Cuba 2 | Vat 2 | Cuba 2 | 2ª cuba de cristal | - | 28.000 |
| `runa2` | Production | 4 | Aprendiz 2 | Apprentice 2 | Aprendiz 2 | 2º corpo: lingotes e cristais → altares | runa1 | 37.000 |
| `condensador` | Production | 5 | Condensador | Condenser | Condensador | dreno de todos os altares +2/s (menu) | - | 48.000 |
| `veio_runico` | Production | 6 | Veio rúnico | Rune Vein | Veta rúnica | MAJOR: depósito de minério rúnico + forno rúnico + nova linha amuleto | - | 63.000 |
| `altar_laminas` | Production | 7 | Lâminas rúnicas | Rune Blades | Hojas rúnicas | nova linha: lâmina rúnica | veio_runico | 81.000 |
| `fornalha_runica2` | Production | 8 | Forno rúnico 2 | Rune Furnace 2 | Horno rúnico 2 | 2º forno rúnico | veio_runico | 110.000 |
| `runa3` | Production | 9 | Aprendiz 3 | Apprentice 3 | Aprendiz 3 | 3º corpo: produtos → balcão | runa2 | 140.000 |
| `fornalha_liga_arcana` | Production | 10 | Liga arcana | Arcane Alloy | Aleación arcana | fornalha de liga + nova linha cajado de liga | veio_runico | 180.000 |
| `altar_escudos` | Production | 11 | Escudos rúnicos | Rune Shields | Escudos rúnicos | nova linha: escudo rúnico | altar_laminas | 230.000 |
| `fole_arcano_duplo` | Production | 12 | Fole duplo | Twin Bellows | Fuelle doble | ×0,64 (menu) | fole_arcano | 300.000 |
| `cadinho_celeste` | Production | 13 | Liga celeste | Celestial Alloy | Aleación celeste | nova linha: liga celeste | fornalha_liga_arcana | 390.000 |
| `runa4` | Production | 14 | Aprendiz 4 | Apprentice 4 | Aprendiz 4 | 4º corpo: rúnico e liga → altares | runa3 | 510.000 |
| `condensador2` | Production | 15 | Condensador 2 | Condenser 2 | Condensador 2 | dreno +2/s (menu) | condensador | 660.000 |
| `foco_cristal` | Production | 16 | Foco de cristal | Crystal Focus | Foco de cristal | altares ×0,6 (menu) | - | 860.000 |
| `cuba3` | Production | 17 | Cuba 3 | Vat 3 | Cuba 3 | 3ª cuba | cuba2 | 1.100.000 |
| `vitrais` | Luxury | — | Vitrais | Stained Glass | Vitrales | luxo | *produção completa | 1.100.000 |
| `runas_piso` | Luxury | — | Runas no piso | Floor Runes | Runas del suelo | luxo | vitrais | 1.500.000 |
| `observatorio` | Luxury | — | Observatório | Observatory | Observatorio | luxo | runas_piso | 1.800.000 |


### 5.6 Grand Forge (CostBase 38.880; 1º produtivo 39.000; fundo ao chegar 72.600/min)

<!-- tabela: upgrades -->
| id | categoria | tier | pt | en | es | efeito | requer | custo |
|---|---|---|---|---|---|---|---|---|
| `fole_grande` | Production | 0 | Fole da catedral | Great Bellows | Fuelle mayor | crisóis e fornalhas ×0,625 (menu) | - | 39.000 |
| `crisol_bronze2` | Production | 1 | Crisol 2 | Crucible 2 | Crisol 2 | 2º crisol de bronze | - | 51.000 |
| `estrela1` | Production | 2 | Estrela 1 | Star 1 | Estrella 1 | turma Estrela, 1º corpo: minérios → crisóis e fornalha | - | 66.000 |
| `cratera_estelar` | Production | 3 | Cratera estelar | Star Crater | Cráter estelar | depósito de pedra estelar + fornalha + forja estelar + nova linha estátua | - | 85.000 |
| `estrela2` | Production | 4 | Estrela 2 | Star 2 | Estrella 2 | 2º corpo: bronze e aço → fundições e forjas | estrela1 | 110.000 |
| `mensageiro` | Production | 5 | Mensageiro | Courier | Mensajero | MAJOR: contrata wk_felix — rotas de importação (6 un/min) e pátio de exportação (lote 10 a cada 90 s) | - | 140.000 |
| `forja_sintese` | Production | 6 | Lâmina estelar | Starblades | Hojas estelares | nova linha: lâmina estelar | mensageiro + cratera_estelar | 190.000 |
| `fornalha_grande2` | Production | 7 | Fornalha 2 | Furnace 2 | Horno 2 | 2ª fornalha | cratera_estelar | 240.000 |
| `estrela3` | Production | 8 | Estrela 3 | Star 3 | Estrella 3 | 3º corpo: produtos → balcão e pátio de exportação | estrela2 | 320.000 |
| `bancada_relicarios` | Production | 9 | Relicários | Reliquaries | Relicarios | nova linha: relicário real | mensageiro | 410.000 |
| `cais2` | Production | 10 | Cais 2 | Dock 2 | Muelle 2 | rotas de importação +6 un/min | mensageiro | 540.000 |
| `bancada_coroas` | Production | 11 | Coroas | Crowns | Coronas | nova linha: coroa do reino | mensageiro | 700.000 |
| `fole_grande_duplo` | Production | 12 | Fole duplo | Twin Bellows | Fuelle doble | ×0,64 (menu) | fole_grande | 910.000 |
| `estrela4` | Production | 13 | Estrela 4 | Star 4 | Estrella 4 | 4º corpo: importados → bancadas | estrela3 | 1.200.000 |
| `bancada_panoplia` | Production | 14 | Panóplias | Panoplies | Panoplias | nova linha: panóplia campeã | cais2 | 1.500.000 |
| `patio_exportacao2` | Production | 15 | Exportação 2 | Export 2 | Exportación 2 | lote 10 → 20; partida 90 → 60 s | mensageiro | 2.000.000 |
| `martelos_catedral` | Production | 16 | Martelos mestres | Master Hammers | Mazos maestros | fundições, forjas e bancadas ×0,6 (menu) | - | 2.600.000 |
| `crisol_bronze3` | Production | 17 | Crisol 3 | Crucible 3 | Crisol 3 | 3º crisol de bronze | crisol_bronze2 | 3.400.000 |
| `vitral_forja` | Luxury | — | Vitral da forja | Forge Window | Vitral de forja | luxo | *produção completa | 3.400.000 |
| `praca_sinos` | Luxury | — | Praça dos sinos | Bell Square | Plaza campanas | luxo | vitral_forja | 4.400.000 |
| `estatua_heroi` | Luxury | — | Estátua do herói | Hero Statue | Estatua héroe | luxo | praca_sinos | 5.300.000 |
| `bencao_forja` | Meta | — | Bênção da forja | Forge Blessing | Bendición forja | q +25 em toda estação de fabricar do distrito (Meta) | produção completa | 970.000 |


### 5.7 Níveis de estação (Meta)

Cada nível: **+15 de qualidade, +5% de velocidade das estações da família e troca de aparência** (`ECONOMIA_WAVE_B.md` §1.5). L2 abre quando a 1ª estação da família existe (no Old Forge, só depois das Ferramentas, regra dos primeiros 10 min); L4–L5 exigem o Blueprint de mestre do distrito. Old Forge: números do `ECONOMIA_WAVE_B.md` §1.5 (a `bigorna_longa` é da família `bigornas`). Distritos 2–6: total = 78.550 × M (o mesmo peso de ~49 min de renda), dividido entre as famílias, cada uma na proporção 1/30, 1/10, 4/15, 3/5 (a do Old Forge). IDs: `nivel_<família>` + o nível (`nivel_montagem3`).

<!-- tabela: niveis -->
| id | distrito | família | L2 | L3 | L4 | L5 | L4–L5 exigem | total |
|---|---|---|---|---|---|---|---|---|
| `nivel_bigornas` | old_forge | bigornas | 500 | 1.500 | 4.000 | 9.000 | `bp_bigorna_mestra` | 15.000 |
| `nivel_escudos` | old_forge | escudos | 650 | 1.900 | 5.000 | 11.000 | `bp_bancada_mestra` | 18.550 |
| `nivel_ferramentas` | old_forge | ferramentas | 500 | 1.500 | 4.000 | 9.000 | `bp_bancada_mestra` | 15.000 |
| `nivel_joalheria` | old_forge | joalheria | 1.000 | 3.000 | 8.000 | 18.000 | `bp_bancada_mestra` | 30.000 |
| `nivel_montagem` | armorers_row | montagem | 4.300 | 13.000 | 35.000 | 78.000 | `bp_mestra_armorers` | 130.300 |
| `nivel_forja_armas` | armorers_row | forja_armas | 3.500 | 11.000 | 28.000 | 64.000 | `bp_mestra_armorers` | 106.500 |
| `nivel_ourivesaria` | artisan_quarter | ourivesaria | 24.000 | 71.000 | 190.000 | 420.000 | `bp_mestra_artisan` | 705.000 |
| `nivel_kits` | royal_arsenal | kits | 32.000 | 95.000 | 250.000 | 570.000 | `bp_mestra_arsenal` | 947.000 |
| `nivel_cerimonial` | royal_arsenal | cerimonial | 39.000 | 120.000 | 310.000 | 700.000 | `bp_mestra_arsenal` | 1.169.000 |
| `nivel_altar_cristal` | arcane_foundry | altar_cristal | 110.000 | 320.000 | 850.000 | 1.900.000 | `bp_mestra_arcane` | 3.180.000 |
| `nivel_altar_runa` | arcane_foundry | altar_runa | 110.000 | 320.000 | 850.000 | 1.900.000 | `bp_mestra_arcane` | 3.180.000 |
| `nivel_sintese` | grand_forge | sintese | 350.000 | 1.000.000 | 2.800.000 | 6.300.000 | `bp_mestra_grand` | 10.450.000 |
| `nivel_fundicao` | grand_forge | fundicao | 290.000 | 860.000 | 2.300.000 | 5.200.000 | `bp_mestra_grand` | 8.650.000 |


---

## 6. Workers (`WorkerDef`)

29 workers fixos e nomeados; nenhum traço sorteado; ≤ 2 traços fortes cada. Os 8 papéis do GDD §8 aparecem: Porter, Smith, Clerk, Quartermaster, Foreman, Smelter, Courier e Manager.
- **Turma** (campo novo `crew`, revisar): nos distritos 2–6 os carregadores são **um** `WorkerDef` com N corpos; cada upgrade `ferraz1..4` (etc.) adiciona um corpo com a rota escrita no efeito. O nível vale para a turma inteira. É o que deixa 6 distritos caberem em 20–30 workers.
- **Custo de nível:** `custoBase × 1,6^(L−2)` (managers: `× 2,0^(L−2)`), múltiplo de 5 ou 2 algarismos significativos. Os 6 carregadores do Old Forge vão aos níveis 6–10 quando a Armorer's Row abre (`ECONOMIA_WAVE_B.md` §2.2).
- **Manager:** vem com o Charter do distrito (contratação 0); níveis 2–5 custam 20/40/80/160 min da renda alvo do distrito e sobem a renda de fundo (§1).
- **Especialidade nova `Arcane`** no enum (Ísis; revisar). `Smelter` usa `Armor` (a liga melhora armaduras).

<!-- tabela: workers -->
| id | nome | papel | especialidade | distrito | corpos | contratação | requisito | nível máx. | custo dos níveis 2/3/4/5 (… 10) | traço 1 | traço 2 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `wk_ajudante1` | Pipo | Porter | Carrying | old_forge | 1 | upgrade ajudante1 (85) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível | carga +1 nos níveis 3 e 5 |
| `wk_ajudante2` | Lia | Porter | Carrying | old_forge | 1 | upgrade ajudante2 (185) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível | carga +1 nos níveis 3 e 5 |
| `wk_ajudante3` | Caio | Porter | Carrying | old_forge | 1 | upgrade ajudante3 (1.165) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível | carga +1 nos níveis 3 e 5 |
| `wk_mineiro` | Brás | Porter | Carrying | old_forge | 1 | upgrade mineiro (3.000) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível | carga +1 nos níveis 3 e 5 |
| `wk_joalheiro1` | Nina | Porter | Jewels | old_forge | 1 | upgrade joalheiro1 (2.200) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível (só a linha de joias) | carga +1 nos níveis 3 e 5 |
| `wk_joalheiro2` | Teo | Porter | Jewels | old_forge | 1 | upgrade joalheiro2 (2.400) | - | 5 (10 com o d2) | 250 / 400 / 640 / 1.025 | velocidade +6%/nível (só a linha de joias) | carga +1 nos níveis 3 e 5 |
| `wk_bruna` | Bruna | Smith | Swords | old_forge | 1 | 1.200 | nivel_bigornas2 | 5 | 300 / 480 / 770 / 1.230 | q +15 nas bigornas (nível 5: +25) | velocidade da família +5% (+3%/nível) |
| `wk_tome` | Tomé | Smith | Shields | old_forge | 1 | 1.200 | nivel_escudos2 | 5 | 300 / 480 / 770 / 1.230 | q +15 nos escudos (nível 5: +25) | velocidade da família +5% (+3%/nível) |
| `wk_iara` | Iara | Smith | Tools | old_forge | 1 | 1.200 | nivel_ferramentas2 | 5 | 300 / 480 / 770 / 1.230 | q +15 nas ferramentas (nível 5: +25) | velocidade da família +5% (+3%/nível) |
| `wk_joaquim` | Joaquim | Smith | Jewels | old_forge | 1 | 2.400 | nivel_joalheria2 | 5 | 600 / 960 / 1.535 / 2.460 | q +15 na joalheria (nível 5: +25) | velocidade da família +5% (+3%/nível) |
| `wk_celeste` | Celeste | Clerk | Service | old_forge | 1 | 1.500 | balcao6 | 5 | 375 / 600 / 960 / 1.535 | paciência +10% (+5%/nível; nível 5: +30%) | atendimento 0,40 → 0,32 s |
| `wk_baltazar` | Baltazar | Quartermaster | None | old_forge | 1 | 2.500 | Renome 1 | 5 | 625 / 1.000 / 1.600 / 2.560 | saídas: fornalha 4 → 6, bancadas 3 → 5 | estoque do balcão +2 por produto nos níveis 3 e 5 |
| `wk_otavio` | Otávio | Foreman | None | old_forge | 1 | 4.000 | Renome 2 | 5 | 1.000 / 1.600 / 2.560 / 4.095 | q +10 em toda estação de fabricar do distrito | velocidade +5% (+2%/nível) |
| `wk_ferraz` | Turma Ferraz | Porter | Carrying | armorers_row | 4 | upgrades ferraz1–4 | - | 10 | 750 / 1.200 / 1.920 / 3.070 … L10 32.000 | velocidade +6%/nível | carga +1 nos níveis 3, 5 e 8 |
| `wk_vulcana` | Vulcana | Smelter | Armor | armorers_row | 1 | 7.200 | fornalha_liga | 10 | 1.800 / 2.880 / 4.610 / 7.375 … L10 77.000 | fornalha de liga 6 → 4 s (+3% velocidade/nível) | q +15 nas montagens (nível 5: +25; nível 10: +30) |
| `wk_dourado` | Família Dourado | Porter | Carrying | artisan_quarter | 3 | upgrades dourado1–3 | - | 10 | 2.250 / 3.600 / 5.760 / 9.215 … L10 97.000 | velocidade +6%/nível | carga +1 nos níveis 3, 5 e 8 |
| `wk_lucia` | Lúcia | Smith | Jewels | artisan_quarter | 1 | 22.000 | nivel_ourivesaria2 | 10 | 5.500 / 8.800 / 14.000 / 23.000 … L10 240.000 | q +15 nas bancadas de ourives (nível 5: +25) | troca de receita −1 s (mínimo 1 s) |
| `wk_cadetes` | Cadetes da Guarda | Porter | Carrying | royal_arsenal | 4 | upgrades cadete1–4 | - | 10 | 6.750 / 11.000 / 17.000 / 28.000 … L10 290.000 | velocidade +6%/nível | carga +1 nos níveis 3, 5 e 8 |
| `wk_matias` | Intendente Matias | Quartermaster | None | royal_arsenal | 1 | 68.000 | patio_faixa2 | 10 | 17.000 / 27.000 / 44.000 / 70.000 … L10 730.000 | prazo-bônus dos blocos de cota 180 → 240 s | saídas das bancadas e forjas 3 → 5 |
| `wk_aprendizes` | Aprendizes de Runa | Porter | Carrying | arcane_foundry | 4 | upgrades runa1–4 | - | 10 | 20.000 / 32.000 / 52.000 / 83.000 … L10 870.000 | velocidade +6%/nível | carga +1 nos níveis 3, 5 e 8 |
| `wk_isis` | Ísis | Smith | Arcane | arcane_foundry | 1 | 190.000 | altar_laminas | 10 | 48.000 / 76.000 / 120.000 / 190.000 … L10 2.000.000 | dreno dos altares +1/s (+0,5/s a cada 2 níveis) | q +15 nos altares (nível 5: +25) |
| `wk_estrela` | Turma Estrela | Porter | Carrying | grand_forge | 4 | upgrades estrela1–4 | - | 10 | 61.000 / 97.000 / 160.000 / 250.000 … L10 2.600.000 | velocidade +6%/nível | carga +1 nos níveis 3, 5 e 8 |
| `wk_felix` | Félix | Courier | Carrying | grand_forge | 1 | upgrade mensageiro | - | 10 | 150.000 / 240.000 / 380.000 / 610.000 … L10 6.400.000 | rotas de importação +2 un/min por nível | lote de exportação +2 por nível |
| `wk_anselmo` | Mestre Anselmo | Manager | None | old_forge | 1 | Charter do Old Forge | Charter | 5 | 32.000 / 64.000 / 130.000 / 260.000 | renda de fundo 25% → 30/33/36/40% da taxa do distrito | coleta o cofre do distrito sozinho |
| `wk_gertrudes` | Dona Gertrudes | Manager | None | armorers_row | 1 | Charter da Armorer's Row | Charter | 5 | 96.000 / 190.000 / 380.000 / 770.000 | renda de fundo 25% → 30/33/36/40% | coleta o cofre do distrito sozinho |
| `wk_valentim` | Valentim | Manager | None | artisan_quarter | 1 | Charter do Artisan Quarter | Charter | 5 | 290.000 / 580.000 / 1.200.000 / 2.300.000 | renda de fundo 25% → 30/33/36/40% | coleta o cofre do distrito sozinho |
| `wk_leonor` | Capitã Leonor | Manager | None | royal_arsenal | 1 | Charter do Royal Arsenal | Charter | 5 | 860.000 / 1.700.000 / 3.400.000 / 6.900.000 | renda de fundo 25% → 30/33/36/40% | coleta o cofre do distrito sozinho |
| `wk_sibila` | Mestra Sibila | Manager | None | arcane_foundry | 1 | Charter da Arcane Foundry | Charter | 5 | 2.600.000 / 5.200.000 / 10.000.000 / 21.000.000 | renda de fundo 25% → 30/33/36/40% | coleta o cofre do distrito sozinho |
| `wk_aurelio` | Grão-mestre Aurélio | Manager | None | grand_forge | 1 | Carta da Grande Forja | Carta | 5 | 7.800.000 / 16.000.000 / 31.000.000 / 62.000.000 | renda de fundo 25% → 30/33/36/40% | coleta o cofre do distrito sozinho |


---

## 7. Encomendas (`OrderDef`)

61 `OrderDef`: os 7 tipos fixos × 6 distritos (42), 9 encomendas `Event` (uma por evento do `LIVEOPS_V1.md`, mais a Arcane Week) e 10 encomendas pessoais dos fregueses (FS-3). Regras do `ECONOMIA_WAVE_B.md` §3 valem em todos os distritos: `N = alvo × RateEma / (linhas abertas × PriceOf)` preso no intervalo; prêmio = segundos × `RateEma`, múltiplo de 5, nunca abaixo do mínimo (que escala com `M`); Rush nunca falha; 1 unidade conta para 1 encomenda; vagas do império (1 → 2 no Renome 1 → 3 no Renome 3) valem no distrito ativo.

### 7.1 Rodízio por distrito (sem peso, sem sorteio)

Posição = `OrderCount % 10` do distrito; tipo travado vira Common. Cada distrito puxa o tipo da sua mecânica: Bulk ×2 na Row ("equipar a milícia"), Mixed ×2 no Quarter (3 produtos), Royal ×3 no Arsenal, Quality ×3 na Foundry, Royal ×3 + Mixed ×2 na Grand Forge ("elite").

<!-- tabela: ciclos -->
| distrito | ciclo (posição = OrderCount % 10) |
|---|---|
| old_forge | Common, Common, Bulk, Quality, Common, Mixed, Rush, Blueprint, Common, Royal |
| armorers_row | Common, Bulk, Quality, Common, Bulk, Mixed, Rush, Blueprint, Common, Royal |
| artisan_quarter | Common, Mixed, Quality, Common, Mixed, Royal, Rush, Blueprint, Common, Bulk |
| royal_arsenal | Royal, Common, Bulk, Royal, Quality, Mixed, Royal, Blueprint, Rush, Common |
| arcane_foundry | Quality, Common, Quality, Bulk, Mixed, Rush, Quality, Blueprint, Common, Royal |
| grand_forge | Royal, Mixed, Bulk, Quality, Royal, Common, Mixed, Blueprint, Rush, Royal |


### 7.2 Tabela

"rodízio das linhas" = o produto segue `linhas[OrderCount % linhas]` (regra atual). Encomenda pessoal: a cada 7ª Common do distrito, a vaga vira a encomenda do próximo freguês daquele distrito (rodízio entre eles), com o produto favorito; dá o prêmio do tipo e +1 coração (afinidade, trilha; V17). `Event`: vaga própria enquanto o evento está ativo, 20 pontos de evento por entrega (`LIVEOPS_V1.md` §2.2).

<!-- tabela: orders -->
| id | tipo | distrito | pt | en | es | produto | alvo (s de renda) | N | qualidade mín. | prazo (só bônus) | ouro (s; mín.) | Renome | outro prêmio | abre | cliente |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `ord_common_old_forge` | Common | old_forge | Pedido da rua | Street Order | Pedido de calle | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 25 | +1 | — | sempre | — |
| `ord_bulk_old_forge` | Bulk | old_forge | Pedido grande | Big Order | Pedido grande | rodízio das linhas | 400 | 8–80 | — | — | 32 s; 60 | +3 | — | 2 linhas e 5 entregues | — |
| `ord_quality_old_forge` | Quality | old_forge | Peça fina | Fine Piece | Pieza fina | rodízio das linhas | 90 | 3–20 | Fina (Superior no Renome 3) | — | 18 s; 40 | +3 | — | alguma estação com q > 0 | — |
| `ord_rush_old_forge` | Rush | old_forge | Pedido urgente | Rush Order | Pedido urgente | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 25 | +1 (+1) | — | 8 entregues | — |
| `ord_mixed_old_forge` | Mixed | old_forge | Pedido variado | Mixed Order | Pedido variado | rodízio das linhas | 70/produto | 2–15 cada | — | — | 25 s; 60 | +3 | — | 3 linhas | — |
| `ord_royal_old_forge` | Royal | old_forge | Pedido real | Royal Order | Pedido real | rodízio das linhas | 200 | 5–40 | Fina | — | 45 s; 150 | +10 | próximo da fila Royal | Renome 2 | — |
| `ord_blueprint_old_forge` | Blueprint | old_forge | Troca por projeto | Blueprint Deal | Trato de plano | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 25 | +2 | próximo da fila de encomenda | Renome 1 e Blueprint pendente | — |
| `ord_common_armorers_row` | Common | armorers_row | Pedido do quartel | Barracks Order | Pedido cuartel | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 75 | +1 | — | distrito aberto | — |
| `ord_bulk_armorers_row` | Bulk | armorers_row | Equipar a milícia | Arm the Militia | Armar milicia | rodízio das linhas | 600 | 12–120 | — | — | 45 s; 180 | +4 | — | distrito aberto | — |
| `ord_quality_armorers_row` | Quality | armorers_row | Peça com liga | Alloy Piece | Pieza aleada | rodízio das linhas | 90 | 3–20 | Fina (Superior no Renome 5) | — | 18 s; 120 | +3 | — | distrito aberto | — |
| `ord_rush_armorers_row` | Rush | armorers_row | Marcha ao alvorecer | Dawn March | Marcha al alba | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 75 | +1 (+1) | — | distrito aberto | — |
| `ord_mixed_armorers_row` | Mixed | armorers_row | Kit do soldado | Soldier Kit | Kit de soldado | rodízio das linhas | 70/produto | 2–15 cada | — | — | 25 s; 180 | +3 | — | distrito aberto | — |
| `ord_royal_armorers_row` | Royal | armorers_row | Pedido do capitão | Captain's Order | Orden capitán | rodízio das linhas | 200 | 5–40 | Fina | — | 45 s; 450 | +10 | próximo da fila Royal | distrito aberto | — |
| `ord_blueprint_armorers_row` | Blueprint | armorers_row | Desenhos do armeiro | Armorer Plans | Planos armero | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 75 | +2 | próximo da fila de encomenda | distrito aberto | — |
| `ord_common_artisan_quarter` | Common | artisan_quarter | Pedido da vitrine | Display Order | Pedido vitrina | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 225 | +1 | — | distrito aberto | — |
| `ord_bulk_artisan_quarter` | Bulk | artisan_quarter | Lote do mercador | Merchant Lot | Lote mercader | rodízio das linhas | 400 | 8–80 | — | — | 32 s; 540 | +3 | — | distrito aberto | — |
| `ord_quality_artisan_quarter` | Quality | artisan_quarter | Peça de coleção | Collector Piece | Pieza colección | rodízio das linhas | 90 | 3–20 | Fina (Superior no Renome 6) | — | 25 s; 360 | +4 | — | distrito aberto | — |
| `ord_rush_artisan_quarter` | Rush | artisan_quarter | Presente urgente | Rush Gift | Regalo urgente | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 225 | +1 (+1) | — | distrito aberto | — |
| `ord_mixed_artisan_quarter` | Mixed | artisan_quarter | Enxoval da condessa | Countess Set | Ajuar condesa | rodízio das linhas | 70/produto × 3 | 2–10 cada | — | — | 30 s; 540 | +4 | — | distrito aberto | — |
| `ord_royal_artisan_quarter` | Royal | artisan_quarter | Joia da corte | Court Jewel | Joya de corte | rodízio das linhas | 200 | 5–40 | Fina | — | 45 s; 1.350 | +10 | próximo da fila Royal | distrito aberto | — |
| `ord_blueprint_artisan_quarter` | Blueprint | artisan_quarter | Moldes do ourives | Goldsmith Molds | Moldes orfebre | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 225 | +2 | próximo da fila de encomenda | distrito aberto | — |
| `ord_common_royal_arsenal` | Common | royal_arsenal | Pedido da guarda | Guard Order | Pedido guardia | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 675 | +1 | — | distrito aberto | — |
| `ord_bulk_royal_arsenal` | Bulk | royal_arsenal | Paiol da fronteira | Border Supply | Arsenal frontera | rodízio das linhas | 400 | 8–80 | — | — | 32 s; 1.620 | +3 | — | distrito aberto | — |
| `ord_quality_royal_arsenal` | Quality | royal_arsenal | Inspeção real | Royal Inspection | Inspección real | rodízio das linhas | 90 | 3–20 | Fina | — | 18 s; 1.080 | +3 | — | distrito aberto | — |
| `ord_rush_royal_arsenal` | Rush | royal_arsenal | Troca da guarda | Guard Change | Cambio guardia | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 675 | +1 (+1) | — | distrito aberto | — |
| `ord_mixed_royal_arsenal` | Mixed | royal_arsenal | Enxoval do cadete | Cadet Outfit | Equipo cadete | rodízio das linhas | 70/produto | 2–15 cada | — | — | 25 s; 1.620 | +3 | — | distrito aberto | — |
| `ord_royal_royal_arsenal` | Royal | royal_arsenal | Requisição da Coroa | Crown Request | Pedido Corona | rodízio das linhas | 250 | 6–50 | Fina | — | 50 s; 4.050 | +12 | próximo da fila Royal; com mesa_real nunca vira Common | distrito aberto | — |
| `ord_blueprint_royal_arsenal` | Blueprint | royal_arsenal | Planos do Arsenal | Arsenal Plans | Planos Arsenal | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 675 | +2 | próximo da fila de encomenda | distrito aberto | — |
| `ord_common_arcane_foundry` | Common | arcane_foundry | Pedido do aprendiz | Student Order | Pedido aprendiz | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 2.025 | +1 | — | distrito aberto | — |
| `ord_bulk_arcane_foundry` | Bulk | arcane_foundry | Estoque da Academia | Academy Stock | Stock academia | rodízio das linhas | 400 | 8–80 | — | — | 32 s; 4.860 | +3 | — | distrito aberto | — |
| `ord_quality_arcane_foundry` | Quality | arcane_foundry | Peça energizada | Charged Piece | Pieza cargada | rodízio das linhas | 90 | 3–20 | Fina (Superior no Renome 8) | — | 25 s; 3.240 | +4 | — | distrito aberto | — |
| `ord_rush_arcane_foundry` | Rush | arcane_foundry | Pedido do eclipse | Eclipse Order | Pedido eclipse | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 2.025 | +1 (+1) | — | distrito aberto | — |
| `ord_mixed_arcane_foundry` | Mixed | arcane_foundry | Kit do mago | Mage Kit | Kit de mago | rodízio das linhas | 70/produto | 2–15 cada | — | — | 25 s; 4.860 | +3 | — | distrito aberto | — |
| `ord_royal_arcane_foundry` | Royal | arcane_foundry | Pedido do Arquimago | Archmage Order | Orden archimago | rodízio das linhas | 200 | 5–40 | Fina | — | 45 s; 12.150 | +10 | próximo da fila Royal | distrito aberto | — |
| `ord_blueprint_arcane_foundry` | Blueprint | arcane_foundry | Grimório | Grimoire | Grimorio | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 2.025 | +2 | próximo da fila de encomenda | distrito aberto | — |
| `ord_common_grand_forge` | Common | grand_forge | Pedido da catedral | Cathedral Order | Pedido catedral | rodízio das linhas | 150 | 3–30 | — | — | 10 s; 6.075 | +1 | — | distrito aberto | — |
| `ord_bulk_grand_forge` | Bulk | grand_forge | Lote do porto | Harbor Lot | Lote del puerto | rodízio das linhas | 400 | 8–80 | — | — | 32 s; 14.580 | +3 | — | distrito aberto | — |
| `ord_quality_grand_forge` | Quality | grand_forge | Peça impecável | Flawless Piece | Pieza impecable | rodízio das linhas | 90 | 3–20 | Fina | — | 18 s; 9.720 | +3 | — | distrito aberto | — |
| `ord_rush_grand_forge` | Rush | grand_forge | Festa da coroa | Crown Feast | Fiesta corona | rodízio das linhas | 150 | 3–30 | — | 240 s: prêmio ×2 | 10 (+10) s; 6.075 | +1 (+1) | — | distrito aberto | — |
| `ord_mixed_grand_forge` | Mixed | grand_forge | Presente ao rei | King's Gift | Regalo al rey | rodízio das linhas | 70/produto | 2–15 cada | — | — | 25 s; 14.580 | +3 | — | distrito aberto | — |
| `ord_royal_grand_forge` | Royal | grand_forge | Encomenda de elite | Elite Order | Pedido de élite | rodízio das linhas | 300 | 3–20 | Fina (Superior no Renome 9) | — | 60 s; 36.450 | +15 | próximo da fila Royal | distrito aberto | — |
| `ord_blueprint_grand_forge` | Blueprint | grand_forge | Arquivos da Forja | Forge Archives | Archivos forja | rodízio das linhas | 150 | 3–30 | — | — | 5 s; 6.075 | +2 | próximo da fila de encomenda | distrito aberto | — |
| `ord_ev_royal_week` | Event | distrito ativo | Pedido da corte | Court Order | Pedido de corte | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s1w1_royal_week ativo | — |
| `ord_ev_forge_frenzy` | Event | distrito ativo | Pedido do frenesi | Frenzy Order | Pedido frenesí | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s1w2_forge_frenzy ativo | — |
| `ord_ev_armorers_challenge` | Event | distrito ativo | Pedido da guilda dos armeiros | Guild Order | Pedido gremial | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s1w3_armorers_challenge ativo | — |
| `ord_ev_hunt_sunblade` | Event | distrito ativo | Pedido do tesoureiro real | Treasurer Order | Pedido tesoro | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s1w4_hunt_sunblade ativo | — |
| `ord_ev_blacksmith_festival` | Event | distrito ativo | Pedido do festival | Festival Order | Pedido festival | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s2w1_blacksmith_festival ativo | — |
| `ord_ev_rare_ore_rush` | Event | distrito ativo | Pedido dos mineiros | Miners' Order | Pedido mineros | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s2w2_rare_ore_rush ativo | — |
| `ord_ev_merchant_caravan` | Event | distrito ativo | Lote da caravana | Caravan Lot | Lote caravana | rodízio das linhas | 150 | 3–30 | — | — | 10 pontos de evento | — | — | ev_s2w3_merchant_caravan ativo | — |
| `ord_ev_hunt_ember_crown` | Event | distrito ativo | Pedido da coroa | Crown Order | Pedido corona | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s2w4_hunt_ember_crown ativo | — |
| `ord_ev_arcane_week` | Event | distrito ativo | Pedido da Torre | Tower Order | Pedido de Torre | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_arcane_week ativo | — |
| `ord_ev_forge_frenzy_b` | Event | distrito ativo | Pedido do frenesi noturno | Night Frenzy | Frenesí nocturno | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s1w5_forge_frenzy_b ativo | — |
| `ord_ev_royal_week_b` | Event | distrito ativo | Pedido real da tarde | Evening Court | Pedido real | rodízio das linhas | 150 | 3–30 | — | — | 15 pontos de evento | — | — | ev_s2w5_royal_week_b ativo | — |
| `ord_reg_orbo` | Quality | old_forge | Encomenda de Mestre Orbo | Regular's Order | Pedido habitual | ferramenta | — (N fixo) | 5 | Fina | — | 18 s; 40 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_orbo` |
| `ord_reg_lirien` | Common | old_forge | Encomenda de Lírien | Regular's Order | Pedido habitual | joia | — (N fixo) | 8 | — | — | 10 s; 25 | +1 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_lirien` |
| `ord_reg_grunk` | Bulk | old_forge | Encomenda de Grunk | Regular's Order | Pedido habitual | espada | — (N fixo) | 40 | — | — | 32 s; 60 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_grunk` |
| `ord_reg_bento` | Common | old_forge | Encomenda de Sir Bento | Regular's Order | Pedido habitual | escudo | — (N fixo) | 10 | — | — | 10 s; 25 | +1 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_bento` |
| `ord_reg_ines` | Quality | old_forge | Encomenda de Dona Inês | Regular's Order | Pedido habitual | joia | — (N fixo) | 4 | Fina | — | 18 s; 40 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_ines` |
| `ord_reg_brutus` | Bulk | armorers_row | Encomenda de Sargento Brutus | Regular's Order | Pedido habitual | lanca | — (N fixo) | 30 | — | — | 32 s; 180 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_brutus` |
| `ord_reg_amara` | Quality | artisan_quarter | Encomenda de Condessa Amara | Regular's Order | Pedido habitual | colar | — (N fixo) | 4 | Fina | — | 18 s; 360 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_amara` |
| `ord_reg_tiberio` | Quality | royal_arsenal | Encomenda de Arauto Tibério | Regular's Order | Pedido habitual | insignia_real | — (N fixo) | 6 | Fina | — | 18 s; 1.080 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_tiberio` |
| `ord_reg_zelia` | Quality | arcane_foundry | Encomenda de Arquimaga Zélia | Regular's Order | Pedido habitual | cajado_liga | — (N fixo) | 3 | Fina | — | 18 s; 3.240 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_zelia` |
| `ord_reg_kael` | Bulk | grand_forge | Encomenda de Embaixador Kael | Regular's Order | Pedido habitual | sino_catedral | — (N fixo) | 6 | — | — | 32 s; 14.580 | +3 e +1 coração | — | a cada 7ª Common do distrito (rodízio dos fregueses) | `cli_reg_kael` |


---

## 8. Royal Contracts (12)

Regra (GDD §11, Wave D): objetivo de médio prazo do império, em paralelo ao loop. **1 vaga** de contrato ativo (2 com a `mesa_real`); a lista é fixa e ordenada; o próximo disponível é o 1º da lista cujo "abre" foi cumprido (sem sorteio).
- **Entrega por cota:** em qualquer distrito, cada linha cujo produto o contrato pede ganha um seletor de cota **0 / 25 / 50 / 100%**. Acumulador inteiro por linha (como a qualidade): soma a cota a cada peça pronta; ao passar de 100, aquela peça vai à mesa do contrato em vez do balcão. A decisão é vender agora ou cumprir.
- **Prazo-bônus** em minutos de jogo ativo desde o aceite: no prazo, ouro +50% e Renome +5; fora, o prêmio normal. Nunca falha, nunca expira.
- **Ouro** em segundos da **taxa do império** (`RateEma` do distrito ativo + soma das rendas de fundo) no momento da entrega, fora do `GoldEarned`, ID de transação `ctr:{id}`. Renome +15 a +40 (`ECONOMIA_WAVE_B.md` §5.1). Blueprint (posse; V9) e baú de **conteúdo fixo mostrado antes**. Recompensa 1×.

<!-- tabela: contratos -->
| id | pt | en | es | abre | pede | qualidade mín. | prazo-bônus | ouro (s da taxa do império) | Renome | Blueprint | baú fixo (mostrado antes) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `ctr_ronda_vila` | Armar a ronda da vila | Village Watch | Ronda del pueblo | Renome 3 | espada×30 + escudo×20 + ferramenta×10 | Normal | 90 min ativos | 300 s | +15 | `bp_var_ronda` | cos_ctr_ronda (flâmula da ronda) |
| `ctr_vinte_guardas` | Equipar 20 guardas | Arm 20 Guards | Armar guardias | armorers_row aberto | espada×20 + escudo×20 + elmo×20 | Normal | 120 min ativos | 400 s | +20 | `bp_var_guardas` | cos_ctr_guardas (cabide de armas) |
| `ctr_milicia_norte` | Milícia do norte | North Militia | Milicia norteña | armorers_row + Renome 5 | lanca×30 + maca×20 + grevas×20 | Normal | 120 min ativos | 450 s | +20 | `bp_var_milicia` | cos_ctr_milicia (tambor da milícia) |
| `ctr_obras_castelo` | Obras do castelo | Castle Works | Obras castillo | artisan_quarter aberto | ferramenta×60 + cinzel×20 + castical×10 | Normal | 150 min ativos | 500 s | +25 | `bp_var_obras` | cos_ctr_obras (andaime decorativo) |
| `ctr_dote_princesa` | Dote da princesa | Princess Dowry | Dote princesa | artisan_quarter + Renome 6 | colar×10 + caixa_joias×10 + broche×20 | Fina | 150 min ativos | 600 s | +30 | `bp_var_dote` | cos_ctr_dote (baú do dote) |
| `ctr_guarda_real` | Guarda real | Royal Guard | Guardia real | royal_arsenal aberto | kit_guarda×20 + elmo×20 + insignia_real×10 | Normal | 150 min ativos | 600 s | +30 | `bp_var_guarda_real` | cos_ctr_guarda (guarita real) |
| `ctr_batedores` | Batedores da fronteira | Border Scouts | Exploradores | royal_arsenal aberto | kit_batedor×30 + lanca×30 | Normal | 120 min ativos | 500 s | +25 | `bp_var_batedores` | cos_ctr_batedores (mapa na parede) |
| `ctr_coroacao` | Desfile da coroação | Coronation | Coronación | royal_arsenal + Renome 7 | espada_gala×10 + alabarda_cerimonial×10 + armadura_gala×5 | Fina | 180 min ativos | 750 s | +35 | `bp_var_coroacao` | cos_ctr_coroacao (tapete real) |
| `ctr_torre_magos` | Torre dos magos | Mage Tower | Torre de magos | arcane_foundry aberto | lanterna_arcana×20 + anel_cristal×20 + cajado_liga×10 | Normal | 180 min ativos | 700 s | +35 | `bp_var_torre` | cos_ctr_torre (esfera flutuante) |
| `ctr_vanguarda` | Vanguarda rúnica | Rune Vanguard | Vanguardia runa | arcane_foundry + Renome 8 | lamina_runica×20 + escudo_runico×20 + peitoral×20 | Fina | 240 min ativos | 900 s | +40 | `bp_var_vanguarda` | cos_ctr_vanguarda (estandarte rúnico) |
| `ctr_sinos_catedral` | Sinos da catedral | Cathedral Bells | Campanas | grand_forge aberto | sino_catedral×6 + estatua_bronze×2 + relicario_real×5 | Normal | 240 min ativos | 900 s | +40 | `bp_var_sinos` | cos_ctr_sinos (carrilhão) |
| `ctr_campeao` | Campeão do reino | Realm Champion | Campeón reino | grand_forge + Renome 9 | panoplia_campeao×5 + lamina_estelar×5 + coroa_reino×1 | Fina | 300 min ativos | 1200 s | +40 | `bp_var_campeao` | cos_ctr_campeao (trono do campeão) |


---

## 9. Blueprints (`BlueprintDef`)

101 Blueprints (meta ≥ 60), **25 essenciais**. Fontes: `OrderBlueprint: <distrito> #n` (n-ésimo da fila de encomenda Blueprint do distrito), `OrderRoyal: <distrito> #n` (fila Royal), `Milestone`, `Reputation`, `Contract`, `Event`, `Achievement`, `PassFree`; pagas (`Shop`, `PassPremium`) **não aparecem** (o Pass premium nunca dá Blueprint, V20; loja só na Wave E e nunca como única fonte).
- **Essencial** = trava distrito, Charter ou Masterwork exigida: o Blueprint de mestre de cada distrito (L4–L5) e os projetos das K primeiras Masterworks de cada distrito (K = Masterworks que o Charter pede: 1, 2, 3, 3, 4, 5). Todo essencial tem uma fonte grátis **permanente** (fila de encomenda, fila Royal, marco ou Renome); evento conta como fonte extra, nunca como a única.
- **Filas:** a fila de encomenda Blueprint e a fila Royal de cada distrito são a ordem dos `#n`; fila esgotada → a encomenda Blueprint vira Common e a Royal paga só ouro e Renome (`ECONOMIA_WAVE_B.md` §4). Old Forge: os 18 do `ECONOMIA_WAVE_B.md` §4 + `bp_rec_espada_longa` (7º da fila de encomenda).

<!-- tabela: blueprints -->
| id | libera | distrito | fontes | essencial | fonte grátis |
|---|---|---|---|---|---|
| `bp_tecnica_espada` | Technique: bigornas q +20 | old_forge | OrderBlueprint: old_forge #1 | não | OrderBlueprint: old_forge #1 |
| `bp_tecnica_escudo` | Technique: escudos q +20 | old_forge | OrderBlueprint: old_forge #2 | não | OrderBlueprint: old_forge #2 |
| `bp_bigorna_mestra` | StationLevel: bigornas L4–L5 | old_forge | OrderBlueprint: old_forge #3 | **sim** | OrderBlueprint: old_forge #3 |
| `bp_bancada_mestra` | StationLevel: escudos, ferramentas, joalheria L4–L5 | old_forge | OrderRoyal: old_forge #1 | **sim** | OrderRoyal: old_forge #1 |
| `bp_tecnica_ferramenta` | Technique: ferramentas q +20 | old_forge | Milestone: 150 ferramentas vendidas | não | Milestone: 150 ferramentas vendidas |
| `bp_tecnica_joia` | Technique: joalheria q +20 | old_forge | Milestone: 50 joias vendidas | não | Milestone: 50 joias vendidas |
| `bp_var_espada_larga` | Variant: espada (cosmético) | old_forge | Milestone: 500 espadas vendidas | não | Milestone: 500 espadas vendidas |
| `bp_var_escudo_brasao` | Variant: escudo (cosmético) | old_forge | Reputation: 3 | não | Reputation: 3 |
| `bp_rec_espada_longa` | Recipe: espada_longa (com a bigorna_longa) | old_forge | OrderBlueprint: old_forge #7; Milestone: 300 espadas vendidas | não | OrderBlueprint: old_forge #7 |
| `bp_tecnica_montagem` | Technique: montagem q +20 | armorers_row | OrderBlueprint: armorers_row #1 | não | OrderBlueprint: armorers_row #1 |
| `bp_tecnica_forja_armas` | Technique: forja_armas q +20 | armorers_row | OrderBlueprint: armorers_row #2 | não | OrderBlueprint: armorers_row #2 |
| `bp_mestra_armorers` | StationLevel: montagem, forja_armas L4–L5 | armorers_row | OrderRoyal: armorers_row #1; Milestone: produção completa da armorers_row | **sim** | OrderRoyal: armorers_row #1 |
| `bp_var_elmo_crista` | Variant: elmo com crista (cosmético) | armorers_row | Milestone: 500 elmos vendidos | não | Milestone: 500 elmos vendidos |
| `bp_tecnica_ourivesaria` | Technique: ourivesaria q +20 | artisan_quarter | OrderBlueprint: artisan_quarter #1 | não | OrderBlueprint: artisan_quarter #1 |
| `bp_tecnica_lapidacao` | Technique: colar e caixa_joias q +15 | artisan_quarter | OrderBlueprint: artisan_quarter #2 | não | OrderBlueprint: artisan_quarter #2 |
| `bp_mestra_artisan` | StationLevel: ourivesaria L4–L5 | artisan_quarter | OrderRoyal: artisan_quarter #1; Milestone: produção completa do artisan_quarter | **sim** | OrderRoyal: artisan_quarter #1 |
| `bp_var_vitrine_cristal` | Variant: vitrine de cristal (cosmético) | artisan_quarter | Milestone: 300 fivelas vendidas | não | Milestone: 300 fivelas vendidas |
| `bp_tecnica_kits` | Technique: kits q +20 | royal_arsenal | OrderBlueprint: royal_arsenal #1 | não | OrderBlueprint: royal_arsenal #1 |
| `bp_tecnica_cerimonial` | Technique: cerimonial q +20 | royal_arsenal | OrderBlueprint: royal_arsenal #2 | não | OrderBlueprint: royal_arsenal #2 |
| `bp_mestra_arsenal` | StationLevel: kits, cerimonial L4–L5 | royal_arsenal | OrderRoyal: royal_arsenal #1; Milestone: produção completa do royal_arsenal | **sim** | OrderRoyal: royal_arsenal #1 |
| `bp_var_estandarte_real` | Variant: estandarte real (cosmético) | royal_arsenal | Milestone: 50 blocos de cota fechados | não | Milestone: 50 blocos de cota fechados |
| `bp_tecnica_cristal` | Technique: altar_cristal q +20 | arcane_foundry | OrderBlueprint: arcane_foundry #1 | não | OrderBlueprint: arcane_foundry #1 |
| `bp_tecnica_runa` | Technique: altar_runa q +20 | arcane_foundry | OrderBlueprint: arcane_foundry #2 | não | OrderBlueprint: arcane_foundry #2 |
| `bp_mestra_arcane` | StationLevel: altar_cristal, altar_runa L4–L5 | arcane_foundry | OrderRoyal: arcane_foundry #1; Milestone: produção completa da arcane_foundry | **sim** | OrderRoyal: arcane_foundry #1 |
| `bp_var_aura` | Variant: aura das lanternas (cosmético) | arcane_foundry | Milestone: 500 peças energizadas vendidas | não | Milestone: 500 peças energizadas vendidas |
| `bp_tecnica_sintese` | Technique: sintese q +20 | grand_forge | OrderBlueprint: grand_forge #1 | não | OrderBlueprint: grand_forge #1 |
| `bp_tecnica_fundicao` | Technique: fundicao q +20 | grand_forge | OrderBlueprint: grand_forge #2 | não | OrderBlueprint: grand_forge #2 |
| `bp_mestra_grand` | StationLevel: sintese, fundicao L4–L5 | grand_forge | OrderRoyal: grand_forge #1; Milestone: produção completa da grand_forge | **sim** | OrderRoyal: grand_forge #1 |
| `bp_var_sino_ouro` | Variant: sino dourado (cosmético) | grand_forge | Milestone: 100 lotes de exportação | não | Milestone: 100 lotes de exportação |
| `bp_mw_primeira_lamina` | Masterwork: mw_primeira_lamina | old_forge | Milestone: 100 espadas vendidas | **sim** | Milestone: 100 espadas vendidas |
| `bp_mw_gume_brasa` | Masterwork: mw_gume_brasa | old_forge | OrderBlueprint: old_forge #4 | não | OrderBlueprint: old_forge #4 |
| `bp_mw_sunblade` | Masterwork: mw_sunblade | old_forge | OrderRoyal: old_forge #2; Event: ev_s1w4_hunt_sunblade | não | OrderRoyal: old_forge #2 |
| `bp_mw_primeira_guarda` | Masterwork: mw_primeira_guarda | old_forge | OrderBlueprint: old_forge #5 | não | OrderBlueprint: old_forge #5 |
| `bp_mw_broquel` | Masterwork: mw_broquel | old_forge | Milestone: 300 escudos vendidos | não | Milestone: 300 escudos vendidos |
| `bp_mw_escudo_brasao` | Masterwork: mw_escudo_brasao | old_forge | Reputation: 3 | não | Reputation: 3 |
| `bp_mw_martelo_fundador` | Masterwork: mw_martelo_fundador | old_forge | Milestone: 300 ferramentas vendidas | não | Milestone: 300 ferramentas vendidas |
| `bp_mw_tenaz_eterna` | Masterwork: mw_tenaz_eterna | old_forge | OrderBlueprint: old_forge #6 | não | OrderBlueprint: old_forge #6 |
| `bp_mw_ember_crown` | Masterwork: mw_ember_crown | old_forge | OrderRoyal: old_forge #3; Event: ev_s2w4_hunt_ember_crown | não | OrderRoyal: old_forge #3 |
| `bp_mw_anel_mercador` | Masterwork: mw_anel_mercador | old_forge | Milestone: 100 joias Fina ou melhor | não | Milestone: 100 joias Fina ou melhor |
| `bp_mw_elmo_vigia` | Masterwork: mw_elmo_vigia | armorers_row | Milestone: 100 elmos vendidos | **sim** | Milestone: 100 elmos vendidos |
| `bp_mw_peitoral_bastiao` | Masterwork: mw_peitoral_bastiao | armorers_row | OrderBlueprint: armorers_row #3; Milestone: 150 peitorais vendidos | **sim** | OrderBlueprint: armorers_row #3 |
| `bp_mw_grevas_marcha` | Masterwork: mw_grevas_marcha | armorers_row | OrderBlueprint: armorers_row #4 | não | OrderBlueprint: armorers_row #4 |
| `bp_mw_manoplas_vivas` | Masterwork: mw_manoplas_vivas | armorers_row | Milestone: 200 manoplas vendidas | não | Milestone: 200 manoplas vendidas |
| `bp_mw_lanca_milicia` | Masterwork: mw_lanca_milicia | armorers_row | OrderRoyal: armorers_row #2 | não | OrderRoyal: armorers_row #2 |
| `bp_mw_machado_rei` | Masterwork: mw_machado_rei | armorers_row | OrderBlueprint: armorers_row #5 | não | OrderBlueprint: armorers_row #5 |
| `bp_mw_maca_juiz` | Masterwork: mw_maca_juiz | armorers_row | Milestone: 300 maças vendidas | não | Milestone: 300 maças vendidas |
| `bp_mw_elmo_liga` | Masterwork: mw_elmo_liga | armorers_row | Reputation: 5 | não | Reputation: 5 |
| `bp_mw_peitoral_rival` | Masterwork: mw_peitoral_rival | armorers_row | OrderRoyal: armorers_row #3; Contract: ctr_vinte_guardas | não | OrderRoyal: armorers_row #3 |
| `bp_mw_lanca_estandarte` | Masterwork: mw_lanca_estandarte | armorers_row | Milestone: 400 lanças vendidas | não | Milestone: 400 lanças vendidas |
| `bp_mw_fivela_viajante` | Masterwork: mw_fivela_viajante | artisan_quarter | Milestone: 200 fivelas vendidas | **sim** | Milestone: 200 fivelas vendidas |
| `bp_mw_colar_aurora` | Masterwork: mw_colar_aurora | artisan_quarter | OrderBlueprint: artisan_quarter #3; Milestone: 200 colares vendidos | **sim** | OrderBlueprint: artisan_quarter #3 |
| `bp_mw_broche_condessa` | Masterwork: mw_broche_condessa | artisan_quarter | OrderRoyal: artisan_quarter #2; Milestone: 300 broches vendidos | **sim** | OrderRoyal: artisan_quarter #2 |
| `bp_mw_cinzel_mestre` | Masterwork: mw_cinzel_mestre | artisan_quarter | Milestone: 300 cinzéis vendidos | não | Milestone: 300 cinzéis vendidos |
| `bp_mw_castical_vigilia` | Masterwork: mw_castical_vigilia | artisan_quarter | OrderBlueprint: artisan_quarter #4 | não | OrderBlueprint: artisan_quarter #4 |
| `bp_mw_caixa_segredos` | Masterwork: mw_caixa_segredos | artisan_quarter | OrderBlueprint: artisan_quarter #5 | não | OrderBlueprint: artisan_quarter #5 |
| `bp_mw_calice_banquete` | Masterwork: mw_calice_banquete | artisan_quarter | Milestone: 200 cálices vendidos | não | Milestone: 200 cálices vendidos |
| `bp_mw_colar_lagrimas` | Masterwork: mw_colar_lagrimas | artisan_quarter | OrderRoyal: artisan_quarter #3 | não | OrderRoyal: artisan_quarter #3 |
| `bp_mw_broche_guilda` | Masterwork: mw_broche_guilda | artisan_quarter | Reputation: 6 | não | Reputation: 6 |
| `bp_mw_caixa_rainha` | Masterwork: mw_caixa_rainha | artisan_quarter | OrderRoyal: artisan_quarter #4; Contract: ctr_dote_princesa | não | OrderRoyal: artisan_quarter #4 |
| `bp_mw_kit_capitao` | Masterwork: mw_kit_capitao | royal_arsenal | Milestone: 100 kits da guarda vendidos | **sim** | Milestone: 100 kits da guarda vendidos |
| `bp_mw_kit_patrulha` | Masterwork: mw_kit_patrulha | royal_arsenal | OrderBlueprint: royal_arsenal #3; Milestone: 300 kits do batedor vendidos | **sim** | OrderBlueprint: royal_arsenal #3 |
| `bp_mw_alabarda_coroacao` | Masterwork: mw_alabarda_coroacao | royal_arsenal | OrderRoyal: royal_arsenal #2 | **sim** | OrderRoyal: royal_arsenal #2 |
| `bp_mw_espada_gala_rei` | Masterwork: mw_espada_gala_rei | royal_arsenal | OrderBlueprint: royal_arsenal #4 | não | OrderBlueprint: royal_arsenal #4 |
| `bp_mw_armadura_desfile` | Masterwork: mw_armadura_desfile | royal_arsenal | OrderRoyal: royal_arsenal #3 | não | OrderRoyal: royal_arsenal #3 |
| `bp_mw_insignia_leao` | Masterwork: mw_insignia_leao | royal_arsenal | Milestone: 300 insígnias vendidas | não | Milestone: 300 insígnias vendidas |
| `bp_mw_kit_cerco` | Masterwork: mw_kit_cerco | royal_arsenal | OrderBlueprint: royal_arsenal #5 | não | OrderBlueprint: royal_arsenal #5 |
| `bp_mw_alabarda_sentinela` | Masterwork: mw_alabarda_sentinela | royal_arsenal | Milestone: 200 alabardas vendidas | não | Milestone: 200 alabardas vendidas |
| `bp_mw_rainha_guerreira` | Masterwork: mw_rainha_guerreira | royal_arsenal | Reputation: 7 | não | Reputation: 7 |
| `bp_mw_grande_selo` | Masterwork: mw_grande_selo | royal_arsenal | OrderRoyal: royal_arsenal #4; Contract: ctr_guarda_real | não | OrderRoyal: royal_arsenal #4 |
| `bp_mw_lanterna_eterna` | Masterwork: mw_lanterna_eterna | arcane_foundry | Milestone: 150 lanternas vendidas | **sim** | Milestone: 150 lanternas vendidas |
| `bp_mw_anel_estrelas` | Masterwork: mw_anel_estrelas | arcane_foundry | OrderBlueprint: arcane_foundry #3; Milestone: 300 anéis vendidos | **sim** | OrderBlueprint: arcane_foundry #3 |
| `bp_mw_amuleto_oraculo` | Masterwork: mw_amuleto_oraculo | arcane_foundry | OrderRoyal: arcane_foundry #2; Milestone: 200 amuletos vendidos | **sim** | OrderRoyal: arcane_foundry #2 |
| `bp_mw_lamina_tempestade` | Masterwork: mw_lamina_tempestade | arcane_foundry | OrderBlueprint: arcane_foundry #4; Milestone: 150 lâminas rúnicas vendidas | **sim** | OrderBlueprint: arcane_foundry #4 |
| `bp_mw_escudo_runas` | Masterwork: mw_escudo_runas | arcane_foundry | OrderRoyal: arcane_foundry #3 | não | OrderRoyal: arcane_foundry #3 |
| `bp_mw_cajado_arcano` | Masterwork: mw_cajado_arcano | arcane_foundry | Milestone: 150 cajados vendidos | não | Milestone: 150 cajados vendidos |
| `bp_mw_liga_primordial` | Masterwork: mw_liga_primordial | arcane_foundry | OrderBlueprint: arcane_foundry #5 | não | OrderBlueprint: arcane_foundry #5 |
| `bp_mw_lamina_silencio` | Masterwork: mw_lamina_silencio | arcane_foundry | Milestone: 300 lâminas rúnicas vendidas | não | Milestone: 300 lâminas rúnicas vendidas |
| `bp_mw_escudo_aurora` | Masterwork: mw_escudo_aurora | arcane_foundry | Reputation: 8 | não | Reputation: 8 |
| `bp_mw_amuleto_mare` | Masterwork: mw_amuleto_mare | arcane_foundry | OrderRoyal: arcane_foundry #4; Event: ev_arcane_week | não | OrderRoyal: arcane_foundry #4 |
| `bp_mw_sino_primeiro` | Masterwork: mw_sino_primeiro | grand_forge | Milestone: 30 sinos vendidos | **sim** | Milestone: 30 sinos vendidos |
| `bp_mw_estatua_fundador` | Masterwork: mw_estatua_fundador | grand_forge | OrderBlueprint: grand_forge #3; Milestone: 30 estátuas vendidas | **sim** | OrderBlueprint: grand_forge #3 |
| `bp_mw_lamina_cometa` | Masterwork: mw_lamina_cometa | grand_forge | OrderRoyal: grand_forge #2 | **sim** | OrderRoyal: grand_forge #2 |
| `bp_mw_relicario_luz` | Masterwork: mw_relicario_luz | grand_forge | OrderBlueprint: grand_forge #4; Milestone: 30 relicários vendidos | **sim** | OrderBlueprint: grand_forge #4 |
| `bp_mw_coroa_seis` | Masterwork: mw_coroa_seis | grand_forge | OrderRoyal: grand_forge #3 | **sim** | OrderRoyal: grand_forge #3 |
| `bp_mw_panoplia_lenda` | Masterwork: mw_panoplia_lenda | grand_forge | OrderBlueprint: grand_forge #5 | não | OrderBlueprint: grand_forge #5 |
| `bp_mw_sino_tempo` | Masterwork: mw_sino_tempo | grand_forge | Milestone: 200 sinos vendidos | não | Milestone: 200 sinos vendidos |
| `bp_mw_rainha_forjadora` | Masterwork: mw_rainha_forjadora | grand_forge | Reputation: 9 | não | Reputation: 9 |
| `bp_mw_lamina_estelar_rei` | Masterwork: mw_lamina_estelar_rei | grand_forge | OrderRoyal: grand_forge #4 | não | OrderRoyal: grand_forge #4 |
| `bp_mw_coroa_granforja` | Masterwork: mw_coroa_granforja | grand_forge | Reputation: 10; Contract: ctr_campeao | não | Reputation: 10 |
| `bp_var_ronda` | Variant: estilo "Armar a ronda da vila" (cosmético) | império | Contract: ctr_ronda_vila | não | Contract: ctr_ronda_vila |
| `bp_var_guardas` | Variant: estilo "Equipar 20 guardas" (cosmético) | império | Contract: ctr_vinte_guardas | não | Contract: ctr_vinte_guardas |
| `bp_var_milicia` | Variant: estilo "Milícia do norte" (cosmético) | império | Contract: ctr_milicia_norte | não | Contract: ctr_milicia_norte |
| `bp_var_obras` | Variant: estilo "Obras do castelo" (cosmético) | império | Contract: ctr_obras_castelo | não | Contract: ctr_obras_castelo |
| `bp_var_dote` | Variant: estilo "Dote da princesa" (cosmético) | império | Contract: ctr_dote_princesa | não | Contract: ctr_dote_princesa |
| `bp_var_guarda_real` | Variant: estilo "Guarda real" (cosmético) | império | Contract: ctr_guarda_real | não | Contract: ctr_guarda_real |
| `bp_var_batedores` | Variant: estilo "Batedores da fronteira" (cosmético) | império | Contract: ctr_batedores | não | Contract: ctr_batedores |
| `bp_var_coroacao` | Variant: estilo "Desfile da coroação" (cosmético) | império | Contract: ctr_coroacao | não | Contract: ctr_coroacao |
| `bp_var_torre` | Variant: estilo "Torre dos magos" (cosmético) | império | Contract: ctr_torre_magos | não | Contract: ctr_torre_magos |
| `bp_var_vanguarda` | Variant: estilo "Vanguarda rúnica" (cosmético) | império | Contract: ctr_vanguarda | não | Contract: ctr_vanguarda |
| `bp_var_sinos` | Variant: estilo "Sinos da catedral" (cosmético) | império | Contract: ctr_sinos_catedral | não | Contract: ctr_sinos_catedral |
| `bp_var_campeao` | Variant: estilo "Campeão do reino" (cosmético) | império | Contract: ctr_campeao | não | Contract: ctr_campeao |


---

## 10. Masterworks (`MasterworkDef`) e Collection

### 10.1 As 60 Masterworks

Base de todas: **peça que já sairia Superior + golpe de mestre do ferreiro + projeto (Blueprint)**; a coluna "condição especial" soma algo a isso. Nenhuma condição pede anúncio, compra ou Gem. Tipos de condição: `Counter:item>=n` (vendidos), `StationLevel:família>=n`, `ReputationTier>=n`, `OrdersOfType:Tipo@distrito>=n`, `OrdersOfProduct:item>=n`, `WorkerLevel:wk>=n`, `AlloyPieces:item>=n` (§2.2), `RichClients:distrito>=n` (§2.3), `GemsCut>=n` (gemas lapidadas), `QualitySold:item@Nível>=n`, `QuotaBlocks:item>=n` e `QuotaOnTime:item>=n` (§2.4), `ContractsDone>=n`, `EnergizedPieces:item>=n` e `StableStreak:estação>=n` (§2.5), `ImportedUnits>=n`, `ExportLots>=n`, `DistrictCharters>=n` (§2.6). As Old Forge são as do `ECONOMIA_WAVE_B.md` §6 sem mudança. Renome por distrito: 320 / 390 / 435 / 500 / 550 / 700.

<!-- tabela: masterworks -->
| id | pt | en | es | distrito | família | receita | projeto | condição especial | Renome | recompensa visual | pedestal |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `mw_primeira_lamina` | Primeira Lâmina | First Blade | Primera Hoja | old_forge | espada | `rec_espada` | `bp_mw_primeira_lamina` | — | +25 | 1º pedestal; abre o Hall | old_forge#1 |
| `mw_gume_brasa` | Gume de Brasa | Ember Edge | Filo de Brasa | old_forge | espada | `rec_espada` | `bp_mw_gume_brasa` | StationLevel:bigornas>=5 | +30 | pedestal | old_forge#2 |
| `mw_sunblade` | Lâmina Solar do Rei | King's Sunblade | Hoja Solar Real | old_forge | espada | `rec_espada` | `bp_mw_sunblade` | Counter:espada>=1000 | +50 | pedestal dourado + estandarte | old_forge#3 |
| `mw_primeira_guarda` | Escudo da Primeira Guarda | First Guard | Primera Guardia | old_forge | escudo | `rec_escudo` | `bp_mw_primeira_guarda` | OrdersOfProduct:escudo>=10 | +30 | pedestal | old_forge#4 |
| `mw_broquel` | Broquel de Carvalho e Ferro | Oak Buckler | Broquel de Roble | old_forge | escudo | `rec_escudo` | `bp_mw_broquel` | StationLevel:escudos>=5 | +25 | pedestal | old_forge#5 |
| `mw_escudo_brasao` | Escudo do Brasão | Crest Shield | Escudo Blasón | old_forge | escudo | `rec_escudo` | `bp_mw_escudo_brasao` | WorkerLevel:wk_tome>=5 | +30 | brasão na fachada | old_forge#6 |
| `mw_martelo_fundador` | Martelo do Fundador | Founder's Hammer | Mazo Fundador | old_forge | ferramenta | `rec_ferramenta` | `bp_mw_martelo_fundador` | StationLevel:ferramentas>=5 | +25 | martelo cosmético do ferreiro | old_forge#7 |
| `mw_tenaz_eterna` | Tenaz Eterna | Eternal Tongs | Tenaza Eterna | old_forge | ferramenta | `rec_ferramenta` | `bp_mw_tenaz_eterna` | OrdersOfType:Mixed@old_forge>=5 | +25 | pedestal | old_forge#8 |
| `mw_ember_crown` | Coroa de Brasa | Ember Crown | Corona de Brasa | old_forge | joia | `rec_joia` | `bp_mw_ember_crown` | ReputationTier>=4 | +50 | coroa no centro do Hall | old_forge#9 |
| `mw_anel_mercador` | Anel do Mercador | Merchant's Ring | Anillo Mercader | old_forge | joia | `rec_joia` | `bp_mw_anel_mercador` | — | +30 | pedestal | old_forge#10 |
| `mw_elmo_vigia` | Elmo do Primeiro Vigia | Watcher's Helm | Yelmo del Vigía | armorers_row | elmo | `rec_elmo` | `bp_mw_elmo_vigia` | — | +30 | 1º pedestal da Row; abre o Hall do distrito | armorers_row#1 |
| `mw_peitoral_bastiao` | Peitoral do Bastião | Bastion Plate | Peto Bastión | armorers_row | peitoral | `rec_peitoral` | `bp_mw_peitoral_bastiao` | StationLevel:montagem>=5 | +35 | pedestal | armorers_row#2 |
| `mw_grevas_marcha` | Grevas da Longa Marcha | March Greaves | Grebas Marcha | armorers_row | grevas | `rec_grevas` | `bp_mw_grevas_marcha` | OrdersOfType:Bulk@armorers_row>=10 | +35 | pedestal | armorers_row#3 |
| `mw_manoplas_vivas` | Manoplas de Ferro Vivo | Livesteel Fists | Manoplas Vivas | armorers_row | manoplas | `rec_manoplas` | `bp_mw_manoplas_vivas` | AlloyPieces:manoplas>=50 | +35 | luvas cosméticas do ferreiro | armorers_row#4 |
| `mw_lanca_milicia` | Lança da Milícia | Militia Spear | Lanza Miliciana | armorers_row | lanca | `rec_lanca` | `bp_mw_lanca_milicia` | Counter:lanca>=1000 | +40 | pedestal + estandarte da milícia | armorers_row#5 |
| `mw_machado_rei` | Machado do Rei Lenhador | Woodking Axe | Hacha Leñadora | armorers_row | machado_guerra | `rec_machado_guerra` | `bp_mw_machado_rei` | StationLevel:forja_armas>=5 | +35 | pedestal | armorers_row#6 |
| `mw_maca_juiz` | Maça do Juiz | Judge's Mace | Maza del Juez | armorers_row | maca | `rec_maca` | `bp_mw_maca_juiz` | WorkerLevel:wk_vulcana>=5 | +35 | pedestal | armorers_row#7 |
| `mw_elmo_liga` | Elmo de Liga Pura | Pure Alloy Helm | Yelmo Aleado | armorers_row | elmo | `rec_elmo` | `bp_mw_elmo_liga` | AlloyPieces:elmo>=200 | +40 | elmo cosmético do ferreiro | armorers_row#8 |
| `mw_peitoral_rival` | Peitoral do Rival Vencido | Rival's Plate | Peto del Rival | armorers_row | peitoral | `rec_peitoral` | `bp_mw_peitoral_rival` | ReputationTier>=6 | +50 | estandarte do rival na fachada | armorers_row#9 |
| `mw_lanca_estandarte` | Lança do Estandarte | Banner Spear | Lanza Estandarte | armorers_row | lanca | `rec_lanca` | `bp_mw_lanca_estandarte` | OrdersOfType:Royal@armorers_row>=10 | +55 | lança-estandarte na entrada | armorers_row#10 |
| `mw_fivela_viajante` | Fivela do Viajante | Wayfarer Buckle | Hebilla Viajera | artisan_quarter | fivela | `rec_fivela` | `bp_mw_fivela_viajante` | — | +35 | 1º pedestal do Quarter; abre o Hall do distrito | artisan_quarter#1 |
| `mw_colar_aurora` | Colar da Aurora | Dawn Necklace | Collar Aurora | artisan_quarter | colar | `rec_colar` | `bp_mw_colar_aurora` | StationLevel:ourivesaria>=5 | +40 | pedestal | artisan_quarter#2 |
| `mw_broche_condessa` | Broche da Condessa | Countess Brooch | Broche Condesa | artisan_quarter | broche | `rec_broche` | `bp_mw_broche_condessa` | RichClients:artisan_quarter>=100 | +40 | pedestal | artisan_quarter#3 |
| `mw_cinzel_mestre` | Cinzel do Mestre Ourives | Goldsmith Chisel | Cincel Orfebre | artisan_quarter | cinzel | `rec_cinzel` | `bp_mw_cinzel_mestre` | WorkerLevel:wk_lucia>=5 | +40 | cinzel cosmético do ferreiro | artisan_quarter#4 |
| `mw_castical_vigilia` | Castiçal da Vigília | Vigil Candle | Candil Vigilia | artisan_quarter | castical | `rec_castical` | `bp_mw_castical_vigilia` | Counter:castical>=800 | +40 | pedestal + luz na vitrine | artisan_quarter#5 |
| `mw_caixa_segredos` | Caixa dos Mil Segredos | Secret Box | Caja Secretos | artisan_quarter | caixa_joias | `rec_caixa_joias` | `bp_mw_caixa_segredos` | OrdersOfType:Mixed@artisan_quarter>=10 | +45 | pedestal | artisan_quarter#6 |
| `mw_calice_banquete` | Cálice do Grande Banquete | Feast Chalice | Cáliz Banquete | artisan_quarter | calice | `rec_calice` | `bp_mw_calice_banquete` | Counter:calice>=600 | +40 | mesa de banquete na rua | artisan_quarter#7 |
| `mw_colar_lagrimas` | Colar de Lágrimas de Rubi | Ruby Tears | Lágrimas Rubí | artisan_quarter | colar | `rec_colar` | `bp_mw_colar_lagrimas` | GemsCut>=1000 | +45 | pedestal | artisan_quarter#8 |
| `mw_broche_guilda` | Broche da Guilda | Guild Brooch | Broche Gremial | artisan_quarter | broche | `rec_broche` | `bp_mw_broche_guilda` | ReputationTier>=7 | +50 | brasão da guilda na fachada | artisan_quarter#9 |
| `mw_caixa_rainha` | Porta-Joias da Rainha | Queen's Casket | Joyero Reina | artisan_quarter | caixa_joias | `rec_caixa_joias` | `bp_mw_caixa_rainha` | QualitySold:caixa_joias@Superior>=50 | +60 | porta-joias no centro do Hall | artisan_quarter#10 |
| `mw_kit_capitao` | Kit do Capitão da Guarda | Captain's Kit | Kit Capitán | royal_arsenal | kit_guarda | `rec_kit_guarda` | `bp_mw_kit_capitao` | — | +40 | 1º pedestal do Arsenal; abre o Hall do distrito | royal_arsenal#1 |
| `mw_kit_patrulha` | Kit da Patrulha Fantasma | Ghost Patrol Kit | Kit Fantasma | royal_arsenal | kit_batedor | `rec_kit_batedor` | `bp_mw_kit_patrulha` | QuotaBlocks:kit_batedor>=20 | +45 | pedestal | royal_arsenal#2 |
| `mw_alabarda_coroacao` | Alabarda da Coroação | Crown Halberd | Alabarda Real | royal_arsenal | alabarda_cerimonial | `rec_alabarda_cerimonial` | `bp_mw_alabarda_coroacao` | StationLevel:cerimonial>=5 | +45 | pedestal | royal_arsenal#3 |
| `mw_espada_gala_rei` | Espada de Gala do Rei | Royal Gala Blade | Espada Gala Rey | royal_arsenal | espada_gala | `rec_espada_gala` | `bp_mw_espada_gala_rei` | QuotaOnTime:espada_gala>=10 | +45 | pedestal dourado | royal_arsenal#4 |
| `mw_armadura_desfile` | Armadura do Desfile | Parade Armor | Armadura Desfile | royal_arsenal | armadura_gala | `rec_armadura_gala` | `bp_mw_armadura_desfile` | Counter:armadura_gala>=300 | +50 | manequim de gala no pátio | royal_arsenal#5 |
| `mw_insignia_leao` | Insígnia do Leão Dourado | Golden Lion | León Dorado | royal_arsenal | insignia_real | `rec_insignia_real` | `bp_mw_insignia_leao` | WorkerLevel:wk_matias>=5 | +45 | leão dourado no portão | royal_arsenal#6 |
| `mw_kit_cerco` | Kit do Cerco de Pedra Alta | Siege Kit | Kit de Asedio | royal_arsenal | kit_guarda | `rec_kit_guarda` | `bp_mw_kit_cerco` | OrdersOfType:Royal@royal_arsenal>=15 | +50 | pedestal | royal_arsenal#7 |
| `mw_alabarda_sentinela` | Alabarda da Sentinela | Sentinel Halberd | Alabarda Vigía | royal_arsenal | alabarda_cerimonial | `rec_alabarda_cerimonial` | `bp_mw_alabarda_sentinela` | QuotaBlocks:alabarda_cerimonial>=30 | +50 | pedestal | royal_arsenal#8 |
| `mw_rainha_guerreira` | Armadura da Rainha Guerreira | Warrior Queen | Reina Guerrera | royal_arsenal | armadura_gala | `rec_armadura_gala` | `bp_mw_rainha_guerreira` | ReputationTier>=8 | +60 | estátua da rainha no pátio | royal_arsenal#9 |
| `mw_grande_selo` | Grande Selo do Reino | Great Seal | Gran Sello | royal_arsenal | insignia_real | `rec_insignia_real` | `bp_mw_grande_selo` | ContractsDone>=3 | +70 | selo no centro do Hall | royal_arsenal#10 |
| `mw_lanterna_eterna` | Lanterna da Chama Eterna | Eternal Lantern | Farol Eterno | arcane_foundry | lanterna_arcana | `rec_lanterna_arcana` | `bp_mw_lanterna_eterna` | — | +45 | 1º pedestal da Foundry; abre o Hall do distrito | arcane_foundry#1 |
| `mw_anel_estrelas` | Anel das Estrelas Frias | Cold Star Ring | Anillo Estelar | arcane_foundry | anel_cristal | `rec_anel_cristal` | `bp_mw_anel_estrelas` | StableStreak:altar_aneis>=100 | +50 | pedestal | arcane_foundry#2 |
| `mw_amuleto_oraculo` | Amuleto do Oráculo | Oracle Amulet | Amuleto Oráculo | arcane_foundry | amuleto_arcano | `rec_amuleto_arcano` | `bp_mw_amuleto_oraculo` | EnergizedPieces:amuleto_arcano>=100 | +50 | pedestal | arcane_foundry#3 |
| `mw_lamina_tempestade` | Lâmina da Tempestade | Storm Blade | Hoja Tormenta | arcane_foundry | lamina_runica | `rec_lamina_runica` | `bp_mw_lamina_tempestade` | StationLevel:altar_runa>=5 | +50 | raios no telhado | arcane_foundry#4 |
| `mw_escudo_runas` | Escudo das Mil Runas | Thousand Runes | Mil Runas | arcane_foundry | escudo_runico | `rec_escudo_runico` | `bp_mw_escudo_runas` | Counter:escudo_runico>=500 | +55 | pedestal | arcane_foundry#5 |
| `mw_cajado_arcano` | Cajado da Arquimaga | Archmage Staff | Báculo Arcano | arcane_foundry | cajado_liga | `rec_cajado_liga` | `bp_mw_cajado_arcano` | WorkerLevel:wk_isis>=5 | +55 | cajado cosmético do ferreiro | arcane_foundry#6 |
| `mw_liga_primordial` | Lingote Celeste Primordial | Primal Alloy | Aleación Prima | arcane_foundry | liga_celeste | `rec_liga_celeste` | `bp_mw_liga_primordial` | EnergizedPieces:liga_celeste>=200 | +55 | pedestal | arcane_foundry#7 |
| `mw_lamina_silencio` | Lâmina do Silêncio | Silent Blade | Hoja Silente | arcane_foundry | lamina_runica | `rec_lamina_runica` | `bp_mw_lamina_silencio` | StableStreak:altar_laminas>=300 | +60 | pedestal | arcane_foundry#8 |
| `mw_escudo_aurora` | Escudo da Aurora Boreal | Aurora Shield | Escudo Boreal | arcane_foundry | escudo_runico | `rec_escudo_runico` | `bp_mw_escudo_aurora` | ReputationTier>=9 | +70 | aurora no céu do distrito | arcane_foundry#9 |
| `mw_amuleto_mare` | Amuleto da Maré | Tide Amulet | Amuleto Marea | arcane_foundry | amuleto_arcano | `rec_amuleto_arcano` | `bp_mw_amuleto_mare` | OrdersOfType:Quality@arcane_foundry>=20 | +60 | fonte de maré no pátio | arcane_foundry#10 |
| `mw_sino_primeiro` | Primeiro Sino da Catedral | First Bell | Primera Campana | grand_forge | sino_catedral | `rec_sino_catedral` | `bp_mw_sino_primeiro` | — | +50 | 1º pedestal da Grand Forge; abre o Hall do distrito | grand_forge#1 |
| `mw_estatua_fundador` | Estátua do Ferreiro Fundador | Founder Statue | Estatua Fundador | grand_forge | estatua_bronze | `rec_estatua_bronze` | `bp_mw_estatua_fundador` | StationLevel:fundicao>=5 | +60 | estátua na praça | grand_forge#2 |
| `mw_lamina_cometa` | Lâmina do Cometa | Comet Blade | Hoja Cometa | grand_forge | lamina_estelar | `rec_lamina_estelar` | `bp_mw_lamina_cometa` | ImportedUnits>=500 | +60 | pedestal | grand_forge#3 |
| `mw_relicario_luz` | Relicário da Luz Antiga | Ancient Light | Luz Antigua | grand_forge | relicario_real | `rec_relicario_real` | `bp_mw_relicario_luz` | StationLevel:sintese>=5 | +60 | pedestal | grand_forge#4 |
| `mw_coroa_seis` | Coroa dos Seis Distritos | Six-Ward Crown | Corona Seis | grand_forge | coroa_reino | `rec_coroa_reino` | `bp_mw_coroa_seis` | DistrictCharters>=5 | +70 | coroa no mapa de distritos | grand_forge#5 |
| `mw_panoplia_lenda` | Panóplia da Lenda | Legend Panoply | Panoplia Leyenda | grand_forge | panoplia_campeao | `rec_panoplia_campeao` | `bp_mw_panoplia_lenda` | ExportLots>=200 | +70 | panóplia no altar da catedral | grand_forge#6 |
| `mw_sino_tempo` | Sino que Marca o Tempo | Time Bell | Campana Tiempo | grand_forge | sino_catedral | `rec_sino_catedral` | `bp_mw_sino_tempo` | Counter:sino_catedral>=500 | +70 | relógio da catedral | grand_forge#7 |
| `mw_rainha_forjadora` | Estátua da Rainha Forjadora | Forge Queen | Reina Forjadora | grand_forge | estatua_bronze | `rec_estatua_bronze` | `bp_mw_rainha_forjadora` | ReputationTier>=9 | +80 | estátua na escadaria | grand_forge#8 |
| `mw_lamina_estelar_rei` | Lâmina Estelar do Rei | Starblade Royal | Hoja Estelar Rey | grand_forge | lamina_estelar | `rec_lamina_estelar` | `bp_mw_lamina_estelar_rei` | WorkerLevel:wk_felix>=10 | +80 | cometa sobre a forja | grand_forge#9 |
| `mw_coroa_granforja` | Coroa da Grande Forja | Greatforge Crown | Corona Granforja | grand_forge | coroa_reino | `rec_coroa_reino` | `bp_mw_coroa_granforja` | ReputationTier>=10 | +100 | coroa no topo da catedral (lendária) | grand_forge#10 |


### 10.2 Superior alcançável (regra do validator: q máximo da família > 100)

<!-- tabela: qmax -->
| distrito | família | fontes no máximo | q máximo |
|---|---|---|---|
| old_forge | bigornas, escudos, ferramentas, joalheria | nível 60 + técnica 20 + oficial 25 + mestre de obras 10 | 115 |
| armorers_row | montagem | nível 60 + técnica 20 + liga 30 + Vulcana 30 (nível 10) | 140 |
| armorers_row | forja_armas | nível 60 + técnica 20 + liga 30 | 110 |
| artisan_quarter | ourivesaria | nível 60 + técnica 20 + Lúcia 25 (+ lapidação 15 em colar e caixa) | 105 |
| royal_arsenal | kits, cerimonial | nível 60 + técnica 20 + selo_arsenal 25 | 105 |
| arcane_foundry | altar_cristal, altar_runa | nível 60 + técnica 20 + energizada 20 + Ísis 25 | 125 |
| grand_forge | sintese, fundicao | nível 60 + técnica 20 + bencao_forja 25 (+ aço estelar 30 nas receitas com aço) | 105 |


### 10.3 Marcos da Collection

Seis abas derivadas dos contadores (GDD §13); o save guarda só os marcos resgatados (`col:{id}`). Marcos dão Renome e cosmético, **nunca ouro**. A aba abre no 1º projeto de Masterwork (~100 espadas). Os do Old Forge (`ECONOMIA_WAVE_B.md` §7) viram os primeiros degraus destes.

<!-- tabela: colecao -->
| id | aba | marco | recompensa |
|---|---|---|---|
| col_produtos_25 | Produtos | 25% das 114 entradas (38 produtos × Normal, Fina, Superior) | Renome +10 |
| col_produtos_50 | Produtos | 50% | Renome +20 |
| col_produtos_75 | Produtos | 75% | Renome +30 |
| col_produtos_100 | Produtos | 100% | Renome +50 + cos_col_mostruario (mostruário na oficina) |
| col_mw_1 | Masterworks | 1ª Masterwork registrada | abre o Hall (D1) |
| col_mw_5 | Masterworks | 5 | Renome +20 |
| col_mw_10 | Masterworks | 10 | Renome +50 + estandarte do Old Forge |
| col_mw_20 | Masterworks | 20 | Renome +50 |
| col_mw_30 | Masterworks | 30 | Renome +60 |
| col_mw_45 | Masterworks | 45 | Renome +80 |
| col_mw_60 | Masterworks | 60 | Renome +120 + cos_col_martelo_lendario |
| col_mw_distrito | Masterworks | as 10 de um distrito (1× por distrito) | estandarte do distrito no mapa |
| col_mw_familia | Masterworks | todas as Masterworks de uma família (1× por família) | +5% fixo no preço da família no distrito (FS-6) |
| col_workers_25 | Workers | 25% das 58 entradas (29 workers × contratado, nível máximo) | Renome +10 |
| col_workers_50 | Workers | 50% | Renome +20 |
| col_workers_75 | Workers | 75% | Renome +30 |
| col_workers_100 | Workers | 100% | Renome +40 + insígnia de mestre de oficina |
| col_clientes_50 | Clientes | 50% das 33 entradas (23 presenças + 10 fregueses) | Renome +10 |
| col_clientes_100 | Clientes | 100% | Renome +30 + cos_col_placa_fregueses |
| col_bp_25 | Blueprints | 25% dos 101 | Renome +10 |
| col_bp_50 | Blueprints | 50% | Renome +20 |
| col_bp_75 | Blueprints | 75% | Renome +30 |
| col_bp_100 | Blueprints | 100% | Renome +50 + cos_col_mesa_projetos |
| col_distritos_3 | Distritos | 3 Charters | Renome +20 |
| col_distritos_6 | Distritos | 6 (5 Charters + a Carta da Grande Forja) | Renome +50 + estandarte do império |


---

## 11. Renome (10 níveis)

Monotônico, sem gasto, nunca cai. Limiares do `ECONOMIA_WAVE_B.md` §5.2 (0–4 inalterados; 5–10 calibrar no econsim). Cada nível libera algo visível.

<!-- tabela: renome -->
| nível | pt | en | es | total | libera |
|---|---|---|---|---|---|
| 0 | Aprendiz | Apprentice | Aprendiz | 0 | — |
| 1 | Ferreiro da rua | Street Smith | Herrero de calle | 20 | 2ª vaga de encomenda; encomendas Blueprint; wk_baltazar |
| 2 | Oficina conhecida | Known Workshop | Taller conocido | 70 | encomendas Royal; wk_otavio; cliente colecionador |
| 3 | Mestre artesão | Master Artisan | Maestro artesano | 180 | 3ª vaga; Quality pede Superior no Old Forge; Hall na rua; herói estágio 2; bp_mw_escudo_brasao; Royal Contracts (Wave D: ctr_ronda_vila) |
| 4 | Fornecedor do reino | Realm Supplier | Proveedor real | 400 | Charter do Old Forge; herói estágio 3 |
| 5 | Armeiro da Coroa | Crown Armorer | Armero corona | 750 | Charter da Armorer's Row; ctr_milicia_norte; Quality da Row pede Superior; bp_mw_elmo_liga; herói estágio 4 (cinto) |
| 6 | Ourives da corte | Court Goldsmith | Orfebre de corte | 1.250 | Charter do Artisan Quarter; ctr_dote_princesa; Quality do Quarter pede Superior; bp_mw_broche_guilda |
| 7 | Fornecedor real | Royal Purveyor | Proveedor regio | 2.000 | Charter do Royal Arsenal; ctr_coroacao; bp_mw_rainha_guerreira; herói estágio 5 (uniforme real) |
| 8 | Mestre arcano | Arcane Master | Maestro arcano | 3.000 | Charter da Arcane Foundry; ctr_vanguarda; Quality da Foundry pede Superior; bp_mw_escudo_aurora |
| 9 | Grão-mestre | Grandmaster | Gran maestro | 4.500 | Carta da Grande Forja; ctr_campeao; Royal da Grand Forge pede Superior; bp_mw_rainha_forjadora |
| 10 | Lenda do reino | Realm Legend | Leyenda real | 6.500 | bp_mw_coroa_granforja (lendária); estátua do ferreiro na praça; título no perfil |


**Orçamento (estimativa para o econsim):** Masterworks 2.895 no total; Charters 600 (5 × 100 + a Carta); contratos ~355; encomendas ~25/h de jogo (Royal ×3 no Arsenal e na Grand Forge); Collection 810; LiveOps ~44/semana (`LIVEOPS_V1.md` §1). Com ~30 h de jogo, o total passa de 6.500 (Renome 10) no endgame; cada Charter fica preso mais pelas Masterworks do distrito que pelo Renome. Portões no §16.

---

## 12. Clientes

Famílias (DM §20): diferem em unidades por visita, paciência, intervalo e qualidade mínima. Paciência = `PatienceFor(item)` (regra atual, `BALANCE.md` §13) × "paciência ×"; intervalo = o da linha × "intervalo ×". O pedido de cada cliente segue o **rodízio fixo** da coluna "pede" (n-ésimo cliente daquela família no distrito → n-ésimo produto aberto da lista). Silhueta por família (arte, C-ART-13).

<!-- tabela: clientes_familias -->
| id | pt | en | es | unidades por visita | paciência × | intervalo × | qualidade mín. | abre |
|---|---|---|---|---|---|---|---|---|
| viajante | viajante | traveler | viajero | 1 | 1,0 | 1,0 | — | sempre |
| guarda | guarda | guard | guardia | 1 | 1,0 | 1,0 | — | sempre |
| aventureiro | aventureiro | adventurer | aventurero | 1 | 0,9 | 1,0 | — | sempre |
| mercador | mercador | merchant | mercader | 2 | 0,8 | 1,3 | — | sempre |
| nobre | nobre | noble | noble | 1 | 1,8 | 2,5 | — | sempre (no d1: só a loja de joias) |
| campones | camponês | farmer | campesino | 1 | 0,8 | 0,7 | — | distrito 2+ |
| emissario | emissário real | royal envoy | emisario real | 2 | 2,0 | 2,0 | Fina | distrito 4+ |
| colecionador | colecionador | collector | coleccionista | 1 | 2,0 | 3,0 | Fina | Renome 2 |
| vip | VIP | VIP | VIP | pacote | 90 s + 6 s/un | 4–6 min (Weyl) | — | 1ª venda (regra atual) |


**Presença por distrito** (`ClientDef`; colecionador e VIP valem em todo distrito):

<!-- tabela: clientes -->
| id | família | distrito | pede (rodízio fixo) |
|---|---|---|---|
| `cli_viajante_old_forge` | viajante | old_forge | espada, ferramenta |
| `cli_guarda_old_forge` | guarda | old_forge | espada, escudo |
| `cli_aventureiro_old_forge` | aventureiro | old_forge | espada, escudo, ferramenta, espada_longa |
| `cli_mercador_old_forge` | mercador | old_forge | ferramenta, escudo |
| `cli_nobre_old_forge` | nobre | old_forge | joia |
| `cli_campones_armorers_row` | campones | armorers_row | lanca, maca |
| `cli_guarda_armorers_row` | guarda | armorers_row | elmo, grevas, lanca |
| `cli_aventureiro_armorers_row` | aventureiro | armorers_row | manoplas, machado_guerra, maca |
| `cli_mercador_armorers_row` | mercador | armorers_row | elmo, grevas, peitoral |
| `cli_viajante_artisan_quarter` | viajante | artisan_quarter | fivela, castical |
| `cli_mercador_artisan_quarter` | mercador | artisan_quarter | fivela, cinzel, castical |
| `cli_nobre_artisan_quarter` | nobre | artisan_quarter | colar, broche, calice, caixa_joias |
| `cli_guarda_royal_arsenal` | guarda | royal_arsenal | kit_batedor, kit_guarda |
| `cli_nobre_royal_arsenal` | nobre | royal_arsenal | insignia_real, alabarda_cerimonial |
| `cli_emissario_royal_arsenal` | emissario | royal_arsenal | espada_gala, armadura_gala, insignia_real |
| `cli_viajante_arcane_foundry` | viajante | arcane_foundry | lanterna_arcana, anel_cristal |
| `cli_aventureiro_arcane_foundry` | aventureiro | arcane_foundry | lamina_runica, escudo_runico, anel_cristal |
| `cli_nobre_arcane_foundry` | nobre | arcane_foundry | amuleto_arcano, liga_celeste, cajado_liga |
| `cli_mercador_grand_forge` | mercador | grand_forge | sino_catedral, estatua_bronze |
| `cli_nobre_grand_forge` | nobre | grand_forge | relicario_real, estatua_bronze, coroa_reino |
| `cli_emissario_grand_forge` | emissario | grand_forge | coroa_reino, panoplia_campeao, lamina_estelar |
| `cli_colecionador` | colecionador | todos | qualquer produto do distrito ativo (Fina ou melhor) |
| `cli_vip` | vip | todos | qualquer produto do balcão (regra atual, BALANCE §18.9) |


**Fregueses com nome** (FS-3, V17; os 5 do Old Forge usam arte existente — mago, elfa, goblin, cavaleiro, nobre):

<!-- tabela: fregueses -->
| id | nome | família | distrito | favorito | encomenda pessoal |
|---|---|---|---|---|---|
| `cli_reg_orbo` | Mestre Orbo | aventureiro | old_forge | `ferramenta` | `ord_reg_orbo` |
| `cli_reg_lirien` | Lírien | viajante | old_forge | `joia` | `ord_reg_lirien` |
| `cli_reg_grunk` | Grunk | mercador | old_forge | `espada` | `ord_reg_grunk` |
| `cli_reg_bento` | Sir Bento | guarda | old_forge | `escudo` | `ord_reg_bento` |
| `cli_reg_ines` | Dona Inês | nobre | old_forge | `joia` | `ord_reg_ines` |
| `cli_reg_brutus` | Sargento Brutus | guarda | armorers_row | `lanca` | `ord_reg_brutus` |
| `cli_reg_amara` | Condessa Amara | nobre | artisan_quarter | `colar` | `ord_reg_amara` |
| `cli_reg_tiberio` | Arauto Tibério | emissario | royal_arsenal | `insignia_real` | `ord_reg_tiberio` |
| `cli_reg_zelia` | Arquimaga Zélia | colecionador | arcane_foundry | `cajado_liga` | `ord_reg_zelia` |
| `cli_reg_kael` | Embaixador Kael | mercador | grand_forge | `sino_catedral` | `ord_reg_kael` |


---

## 13. Contagens

<!-- tabela: contagens -->
| categoria | neste documento | meta (GDD §23 / DM §7) |
|---|---|---|
| Distritos (`DistrictDef`) | 6 | 6 |
| Produtos vendáveis (`ItemDef` Product) | 38 | 35–45 |
| Insumos (`ItemDef` Raw + Intermediate) | 20 | — |
| Receitas (`RecipeDef`) | 50 | 1 por item produzido |
| Estações (`StationDef`): de produção · de apoio | 44 · 16 | 25–35 (DM §7) — ver decisão 8 do §15 |
| Upgrades (`UpgradeDef` produção + luxo + Meta de linha/distrito) | 137 | 120–160 |
| Níveis de estação (`UpgradeDef` Meta, 4 por família) | 52 | — |
| Workers (`WorkerDef`) | 29 | 20–30 |
| Encomendas (`OrderDef`) | 61 | ≥ 50 |
| Royal Contracts | 12 | 12 |
| Blueprints (`BlueprintDef`) · essenciais | 101 · 25 | ≥ 60 |
| Masterworks (`MasterworkDef`) | 60 | 60 (10 por distrito) |
| Marcos da Collection | 25 | — |
| Níveis de Renome | 10 (+ o 0) | 10 |
| Clientes: famílias · presenças · fregueses | 9 · 23 · 10 | — |


---

## 14. O que o núcleo precisa ter para ler isto (por ticket)

| Ticket | O que entra | Daqui |
|---|---|---|
| B-CORE-01 (em andamento na branch `feat/itemdef-receitas`) | `ItemDef`/`RecipeDef` com ID sem prefixo | §3, §3.1 (acrescentar `outputCount` para o rebite) |
| B-CORE-03 / 04 / 05 / 07 / 08 | Quality, `WorkerDef`, Orders, Blueprints, Masterworks do Old Forge | §5.7, §6, §7, §9, §10 (só as linhas `old_forge`) |
| C-CORE-01 | `DistrictDef` + `Sim` por distrito + `StationDef` com `Assembly` | §2, §4, §5 |
| C-CORE-02 | Império: carteira, Renome, vagas de encomenda, troca de distrito | §0, §11 |
| C-CORE-03 | Manager, renda de fundo, Charter/Carta | §1, §2, §6 |
| C-CORE-05 | Mecânicas dos distritos 2–6 (uma por distrito, com teste no `coretests`) | §2.2–§2.6 |
| D-CORE-04 | Royal Contracts e cota | §8 |

**Testes negativos sugeridos (um por mecânica):** montagem não começa com componente faltando; peça sem liga não ganha +30; troca de receita não perde insumo; bloco de cota fora do prazo nunca paga menos que 1,3×; altar Estável nunca sobrecarrega; Forçado sobrecarregado não perde peça; importação nunca deixa a renda de fundo negativa e nunca acontece offline; lote vazio não conta ciclo; contrato nunca bloqueia o Charter (bot F2P fecha os 6 sem contrato); renda de fundo < renda ativa em todo distrito; nenhum ID se repete; o validator falha com Blueprint essencial só pago.

---

## 15. Decisões tomadas por padrão (revisar)

1. **Escala ×3 por distrito** (renda alvo na produção completa 1.600 → 4.800 → 14.400 → 43.200 → 129.600 → 388.800/min). Alternativa: ×2,5 (endgame menos inflado) ou ×4 (distrito novo mais "explosivo").
2. **CostBase = 160 × M, crescimento 1,30, 18 produtivos sem exceção** nos distritos 2–6 (o Old Forge mantém a tabela atual).
3. **Abertura = 90 min** da renda de produção completa do distrito anterior (140.000 … 12.000.000).
4. **Renda de fundo = 25% da taxa** com o manager nível 1 (o fator do offline), 40% no nível 5; os níveis custam 20/40/80/160 min da renda do distrito.
5. **Contrato nunca bloqueia:** os Charters 4–6 aceitam "N contratos **ou** 10×N encomendas Royal" (concilia GDD §11 com §23 item 7).
6. **Espada longa como linha Meta** (`bigorna_longa`, fora da produção completa) para não mexer nos portões medidos do Old Forge.
7. **Turma de carregadores = 1 `WorkerDef` com N corpos** (campo `crew`), para caber em 20–30 workers (29).
8. **Uma estação por produto** nos distritos novos (padrão do núcleo), exceto a bancada configurável do Quarter: dá 44 estações de produção, acima das 25–35 do DM §7. Fundir (ex.: uma "bancada de montagem" para 4 armaduras) reduz para ~30 se a arte pedir.
9. **Qualidade de distrito por upgrade Meta** onde não há worker de qualidade (`selo_arsenal`, `bencao_forja`, q +25).
10. **Unidades por visita** por família de cliente (mercador e emissário levam 2) e multiplicadores de paciência e intervalo por família — regra nova.
11. **Especialidade `Arcane`** nova no enum.
12. **Nomes provisórios** de workers, managers e fregueses; os 6 ajudantes atuais ganharam nome (Pipo, Lia, Caio, Brás, Nina, Teo).
13. **Ouro de contrato em segundos da "taxa do império"** (ativo + fundo).
14. **Cristal desde o kit** da Arcane Foundry; o major unlock é o minério rúnico.
15. **Mochila e Botas valem em todos os distritos** (ferramentas do herói, DM §26).
16. **Grand Forge sem Charter "de saída":** a Carta da Grande Forja tem as mesmas concessões e marca o endgame.
17. **Tier 5 para os produtos de síntese** (o campo `tier` é 1–5; o GDD chama de "5+").
18. **IDs:** `ord_<tipo>_<distrito>` (o `ECONOMIA_WAVE_B.md` §8.4 escrevia `ord_common_oldforge`), `rec_<saída>`, upgrade de linha = ID da estação.

---

## 16. Checagem e portões

**`client/tools/conteudo_check.py`** (Python stdlib): `python client/tools/conteudo_check.py` (padrão: este arquivo). Lê as tabelas marcadas e falha (saída 1) se: algum ID repete dentro do seu tipo ou entre os tipos com prefixo; alguma receita usa insumo sem depósito ou receita no mesmo distrito (fora os importados do Grand Forge, que precisam ser produzidos no distrito de origem); as contagens saem das faixas (35–45 vendáveis, 20–30 workers, ≥ 50 encomendas, 12 contratos, ≥ 60 Blueprints, 60 Masterworks com 10 por distrito); Blueprint essencial sem fonte grátis permanente; Masterwork com condição de anúncio/compra ou com projeto, receita ou distrito inexistente; nome en/es com mais de 16 caracteres; produto sem receita. `--selftest` quebra uma cópia em memória (essencial só pago, essencial só de evento, Masterwork com anúncio, ID repetido, insumo sem produtor, poucos vendáveis, nome longo, projeto inexistente) e exige o erro de cada uma.

**Portões do econsim (quando o C-CORE-05 rodar um distrito em dados):**

| Medida | Portão | Knob se falhar |
|---|---|---|
| Produção completa por distrito (F2P) | 45–75 min (modelo: ~61/57/56/56/56 min) | CostBase do distrito |
| Zona morta até a produção completa | ≤ 12 min por distrito (meta da Wave B) | preço dos níveis L2–L3 e dos 1ºs níveis de turma |
| Renda na produção completa | ±20% da alvo do §1 | preços do distrito |
| Renda de fundo | < renda ativa do mesmo distrito, sempre | fator do manager |
| Charter por distrito (h no distrito, F2P) | dentro da coluna do §1 | limiares de Renome 5–10, condições das Masterworks |
| Bot F2P sem contrato | fecha os 6 distritos | "ou 10×N Royal" |
| Pátio de cotas | 30–70% da produção do Arsenal passa pelo pátio no bot | multiplicador 1,3 / prazo |
| Forçado × Estável (Foundry) | nenhum dos dois domina: renda do Forçado entre 0,9× e 1,15× a do Estável | carga por peça, pausa |
| Importação | o fundo da origem nunca chega a 0 com as rotas no máximo | custo 0,25 / capacidade |
| Masterworks | bot Perfect registra as 60; nenhuma pede anúncio | condições |
