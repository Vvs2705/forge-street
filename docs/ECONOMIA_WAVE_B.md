# Economia da Wave B — números iniciais (spec para o bot validar)

2026-10-12 · Raia DESIGN, Leva 5 · Estado: **PROPOSTO**. Nada daqui está implementado nem medido.
Regras de design: [`GDD.md`](GDD.md) §5 e §8–§14. Tickets: B-CORE-01..08 em [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md). Números medidos (base): [`BALANCE.md`](BALANCE.md) §18–§22. Decisões: [`PROJETO.md`](PROJETO.md) V4, V7, V9, V11, P5, P10.

Todo número aqui é **ponto de partida**. Ele só vira APROVADO depois que o econsim (`client/tools/econsim`, perfis do Bot 2.0) passar nos portões do §10 e os resultados forem para o `BALANCE.md`. Se um portão falhar, o ajuste é no knob indicado, não na regra.

## 0. Regras que valem para tudo

1. **Sem sorteio.** Nenhuma fórmula usa RNG. Qualidade = acumulador; Orders = rodízio; recompensas de Blueprint = fila ordenada; workers = personagens fixos.
2. **Só ouro** como moeda (V9). Renome é trilha; Blueprint é posse.
3. **Prêmio que não é venda** (Orders, baús, marcos) entra no ouro mas **fora do `GoldEarned`**, com ID de transação (como hoje, `BALANCE.md` §20).
4. **Escala por taxa:** alvos e prêmios em segundos da taxa online (`RateEma`), como as Encomendas atuais (`Balance.OrderSeconds`, `OrderRewardSeconds`). Custos em ouro do Old Forge.
5. **Inteiros:** pontos de qualidade em centésimos (`int`), para o acumulador não acumular erro de `float` entre plataformas.
6. **Primeiros 10 min intocados:** tudo daqui abre depois das Ferramentas (~10:46 no bot F2P). O `Bot_Primeiros10Minutos_BatemASecao3` não muda.
7. **Categoria nova `Meta`** no `UpgradeDef` para os ralos da Wave B (níveis de estação, níveis e contratações de workers): ficam **fora** da produção completa (`ProductionComplete`) e do teto offline (`CheapestLockedCost`), como o luxo. Assim os portões atuais de 60/90 min continuam valendo e o Charter usa a mesma "produção completa".
8. **Tudo em dados** (`*Def` com ID em string, A-CORE-04) e aprovado pelo Content Validator antes de entrar.

## 1. Quality (B-CORE-03)

### 1.1 Níveis e preço

| Nível | Índice | Multiplicador de preço | Vende? | Quem faz |
|---|---|---|---|---|
| Normal | 0 | 1,0× | sim | qualquer estação |
| Fina | 1 | 1,5× | sim | acumulador ou golpe |
| Superior | 2 | 2,5× | sim | acumulador ou golpe |
| Masterwork | 3 | — | **não** (vai para o Hall) | só golpe do ferreiro numa peça Superior com projeto e condições |

`PriceOf(item, nível) = arredonda(preçoBase × mult)` (meio para cima). Preços resultantes:

| Produto | Normal | Fina | Superior |
|---|---|---|---|
| Espada | 10 | 15 | 25 |
| Escudo | 25 | 38 | 63 |
| Ferramenta | 16 | 24 | 40 |
| Joia (antes da Vitrine de joias) | 60 | 90 | 150 |
| Joia (com Vitrine de joias) | 80 | 120 | 200 |

O VIP paga 3× o `PriceOf` da peça que leva (com a qualidade). O balcão e o VIP tiram **a melhor qualidade primeiro**.

### 1.2 Pontos de qualidade `q` (centésimos), por estação de fabricar

| Fonte (DM §15) | Valor | Onde | Wave |
|---|---|---|---|
| Nível da estação (família) | L1 0 · L2 15 · L3 30 · L4 45 · L5 60 | §1.5 | B |
| Oficial da especialidade da família | 15 (níveis 1–4) · 25 (nível 5) | §2.3 | B |
| Técnica (Blueprint da família) | 20 | §4 | B |
| Mestre de obras no distrito | 10 | §2.3 | B |
| Material: liga | 30 | distrito 2+ | C |
| Modificador de evento | 0–25 | `EventDef` | D |

Máximo no Old Forge: 60 + 25 + 20 + 10 = **115** → toda peça Fina e 15% Superior. A fornalha não tem qualidade (lingote é intermediário).

### 1.3 Regra do acumulador

```
q    = soma das fontes da estação (centésimos)        // QualityBreakdown() devolve a lista
base = q / 100 ; frac = q % 100
acc[estação] += frac
nível = base
se acc[estação] >= 100: nível += 1 ; acc[estação] -= 100
nível = min(nível, Superior)                           // a automação para em Superior
se golpe válido nesta peça:
    nível = (nível == Superior e há Masterwork pendente da família) ? Masterwork : min(nível + 1, Superior)
```

- Com `q = 25`: exatamente 1 Fina a cada 4 peças. Com `q = 115`: Fina sempre, Superior a cada ~6,7 peças.
- A UI mostra a barra `acc` da estação ("próxima Fina em 2 peças"; "próxima Superior agora").
- "Masterwork pendente" = a primeira, na ordem da lista do §6, cujo projeto o jogador tem e cujas condições estão cumpridas, ainda não registrada.
- **Save:** `acc` por estação; save antigo = 0 e tudo Normal.

### 1.4 Golpe de mestre (FS-2 reconciliado, decisão P10)

| Knob | Valor inicial | Nota |
|---|---|---|
| Janela do anel | 0,8 s | FS-2 |
| Recarga | 20 s | sobe para 30–45 s se o portão de 8% falhar |
| Quem | só o ferreiro, parado na boca de entrada de uma estação de fabricar, com o insumo na mão | ajudante, manager e offline nunca |
| Efeito | a próxima peça daquela estação sobe 1 nível; Superior + Masterwork pendente = Masterwork | errar ou ignorar não pune |

Perfis do bot (determinísticos, por contador): Perfect acerta todo golpe disponível e espera a "próxima Superior" para tentar a Masterwork; F2P acerta 1 em 2; Slow 1 em 4; AdWatcher = F2P.

### 1.5 Níveis de estação (ralo novo, categoria `Meta`, no menu)

Cada nível: **+15 de qualidade, +5% de velocidade das estações da família e troca de aparência** (`BENCHMARK_MERCADO.md` B1: estação com níveis é o padrão do gênero). Cadeia L2 → L5.

| Família | L2 | L3 | L4 | L5 | Abre L2 | L4–L5 exigem | Total |
|---|---|---|---|---|---|---|---|
| Bigornas (espada) | 500 | 1.500 | 4.000 | 9.000 | Ferramentas comprado | `bp_bigorna_mestra` | 15.000 |
| Bancada de escudos | 650 | 1.900 | 5.000 | 11.000 | Ferramentas comprado | `bp_bancada_mestra` | 18.550 |
| Bancada de ferramentas | 500 | 1.500 | 4.000 | 9.000 | Ferramentas comprado | `bp_bancada_mestra` | 15.000 |
| Bancada de joias | 1.000 | 3.000 | 8.000 | 18.000 | Joalheria comprado | `bp_bancada_mestra` | 30.000 |
| **Soma** | | | | | | | **78.550** |

Leitura pretendida: L2–L3 (500–3.000) caem na zona morta dos 15–25 min, quando a renda é 400–600/min e os custos produtivos estão em 1–3 mil (`BALANCE.md` §22); L4–L5 absorvem o ouro depois da produção completa.

## 2. Workers (B-CORE-04)

### 2.1 Os 6 ajudantes atuais viram Carregadores com nível

| Ajudante atual | Papel | Especialidade |
|---|---|---|
| Ajudante (minério → fornalhas), Mineiro | Carregador | carga |
| Ajudante 2 (fornalhas → bancadas) | Carregador | carga |
| Ajudante 3 (bancadas → balcão) | Carregador | carga |
| Joalheiro, Joalheiro 2 | Carregador | joias (só a linha de joias, como hoje) |

### 2.2 Nível dos Carregadores (categoria `Meta`)

| Nível | Custo para chegar | Efeito acumulado |
|---|---|---|
| 1 | (contratação atual) | — |
| 2 | 250 | velocidade +6% |
| 3 | 400 | +12%; carga +1 |
| 4 | 640 | +18% |
| 5 | 1.025 | +24%; carga +2 |

`custo(L) = 250 × 1,6^(L−2)`, múltiplo de 5. Soma por ajudante: 2.315; os 6: **13.890**. Multiplica por cima do "Ajudantes ágeis" (2,4 → 3,4 m/s; carga 2 → 4). Níveis 6–10 entram com o distrito 2.

### 2.3 Papéis novos no Old Forge (WorkerDef fixos e nomeados; nomes provisórios)

`custo do nível L (2–5) = contratação / 4 × 1,6^(L−2)`, múltiplo de 5.

| Papel | WorkerDef | Contratação | Requisito | Nível 1 | Níveis 2–5 | Níveis 2–5 (custo) |
|---|---|---|---|---|---|---|
| Oficial (Smith) | Bruna (espadas), Tomé (escudos), Iara (ferramentas) | 1.200 cada | estação da família no nível 2 | q +15 na família; velocidade da família +5% | +3% velocidade por nível; q +25 no nível 5 | 300 / 480 / 770 / 1.230 |
| Oficial (Smith) | Joaquim (joias) | 2.400 | bancada de joias no nível 2 | idem | idem | 600 / 960 / 1.535 / 2.460 |
| Balconista (Clerk) | Celeste | 1.500 | Balcão 6 vagas | paciência dos clientes +10%; atendimento 0,40 → 0,32 s | +5% paciência por nível (nível 5: +30%) | 375 / 600 / 960 / 1.535 |
| Almoxarife (Quartermaster) | Baltazar | 2.500 | Renome 1 | saída da fornalha 4 → 6; saída das bancadas 3 → 5 | estoque do balcão +2 por produto nos níveis 3 e 5 | 625 / 1.000 / 1.600 / 2.560 |
| Mestre de obras (Foreman) | Otávio | 4.000 | Renome 2 | q +10 em todas as estações de fabricar do distrito; velocidade +5% | +2% velocidade por nível (nível 5: +13%) | 1.000 / 1.600 / 2.560 / 4.095 |

Soma dos papéis novos (contratar + nível 5): oficiais 6.000 + 13.895 · balconista 4.970 · almoxarife 8.285 · mestre de obras 13.255 = **46.405**. Com os Carregadores: **60.295**. Com os níveis de estação: **~138,8 mil** de ralo novo no Old Forge (o luxo atual soma 54 mil).

Regras: 1–2 traços fortes por worker (DM §23); um oficial por família; nenhum traço sorteado; Fundidor e Mensageiro só na Wave C.

## 3. Orders 2.0 (B-CORE-05)

### 3.1 Os 8 tipos

`N = alvo × RateEma / (linhas abertas × PriceOf(item, Normal))`, preso no intervalo da coluna. Prêmio = segundos × `RateEma`, múltiplo de 5, nunca abaixo do mínimo.

| Tipo | Alvo (s de renda) | N mín–máx | Qualidade mínima | Prazo (só bônus) | Ouro (s; mínimo) | Renome | Outro prêmio | Abre quando |
|---|---|---|---|---|---|---|---|---|
| Common | 150 | 3–30 | — | — | 10 s; 25 | +1 | — | sempre (= Encomenda atual) |
| Bulk | 400 | 8–80 | — | — | 32 s; 60 | +3 | — | 2 linhas abertas e 5 entregues |
| Quality | 90 | 3–20 | Fina (Superior no Renome 3) | — | 18 s; 40 (Superior: 30 s; 75) | +3 | — | alguma estação com q > 0 |
| Rush | 150 | 3–30 | — | 240 s: prêmio dobra | 10 s (+10 s); 25 | +1 (+1) | — | 8 entregues |
| Mixed | 70 por produto, 2 produtos (3 com 4 linhas) | 2–15 cada | — | — | 25 s; 60 | +3 | — | 3 linhas abertas |
| Royal | 200 | 5–40 | Fina | — | 45 s; 150 | +10 | próximo Blueprint da fila Royal (§4) | Renome 2 |
| Blueprint | 150 | 3–30 | — | — | 5 s; 25 | +2 | próximo Blueprint da fila de encomenda (§4) | Renome 1 e Blueprint pendente |
| Event | do `EventDef` | — | do `EventDef` | do `EventDef` | pontos de evento | do `EventDef` | do `EventDef` | evento ativo (Wave D; flag) |

Rush nunca falha: depois de 240 s (tempo de jogo, o offline não conta) vira prêmio normal.

### 3.2 Rodízio determinístico

- Ciclo do Old Forge (10 posições): `Common, Common, Bulk, Quality, Common, Mixed, Rush, Blueprint, Common, Royal`. Posição = `OrderCount % 10`.
- Tipo ainda travado → **Common** na mesma posição.
- Produto: `linhas[OrderCount % linhas]` (regra atual). Mixed: as próximas 2–3 linhas a partir dali. Quality/Royal: a primeira linha nessa ordem que tenha q > 0 (se nenhuma, vira Common).
- Event fica fora do ciclo: ocupa uma vaga própria enquanto o evento está ativo.
- "Trocar encomenda" (rewarded, GDD §17) avança o ciclo uma posição e **mostra antes** qual vem.

### 3.3 Vagas

1 vaga no começo → 2 no Renome 1 → 3 no Renome 3. Vagas puxam do mesmo ciclo; se o produto escolhido pelo rodízio já está numa vaga aberta, pula para a próxima linha. Cada unidade vendida conta para **no máximo uma** encomenda (a mais antiga que aceita aquela peça).

### 3.4 Save

`ord=` vira uma lista de vagas `tipo,item(s),alvo,progresso,prêmio,prazo` + `OrderCount` e relógio. Save da 0.6: a encomenda atual vira a vaga 1 como Common; o ciclo continua de `OrderCount`.

## 4. Blueprints (B-CORE-07)

Blueprint = posse permanente que libera uma coisa. Nada pago na Wave B; a regra "essencial nunca só paga" entra no validator agora, para a Wave E não conseguir quebrá-la.

| ID | Libera | Fonte grátis | Essencial |
|---|---|---|---|
| `bp_tecnica_espada` | Técnica: espadas q +20 | fila de encomenda Blueprint (1º) | não |
| `bp_tecnica_escudo` | Técnica: escudos q +20 | fila de encomenda Blueprint (2º) | não |
| `bp_bigorna_mestra` | Bigornas nível 4–5 | fila de encomenda Blueprint (3º) | **sim** (Superior → Masterwork → Charter) |
| `bp_mw_gume_brasa` | projeto: Gume de Brasa | fila de encomenda Blueprint (4º) | não |
| `bp_mw_primeira_guarda` | projeto: Escudo da Primeira Guarda | fila de encomenda Blueprint (5º) | não |
| `bp_mw_tenaz_eterna` | projeto: Tenaz Eterna | fila de encomenda Blueprint (6º) | não |
| `bp_bancada_mestra` | bancadas (escudo, ferramenta, joia) nível 4–5 | fila Royal (1º) | **sim** |
| `bp_mw_sunblade` | projeto: Lâmina Solar do Rei | fila Royal (2º) | não |
| `bp_mw_ember_crown` | projeto: Coroa de Brasa | fila Royal (3º) | não |
| `bp_mw_primeira_lamina` | projeto: Primeira Lâmina | marco: 100 espadas vendidas | **sim** (1ª Masterwork alcançável; abre o Hall) |
| `bp_tecnica_ferramenta` | Técnica: ferramentas q +20 | marco: 150 ferramentas vendidas | não |
| `bp_tecnica_joia` | Técnica: joias q +20 | marco: 50 joias vendidas | não |
| `bp_mw_broquel` | projeto: Broquel de Carvalho e Ferro | marco: 300 escudos vendidos | não |
| `bp_mw_martelo_fundador` | projeto: Martelo do Fundador | marco: 300 ferramentas vendidas | não |
| `bp_mw_anel_mercador` | projeto: Anel do Mercador | marco: 100 joias Fina ou melhor vendidas | não |
| `bp_var_espada_larga` | variante visual da espada (cosmético) | marco: 500 espadas vendidas | não |
| `bp_mw_escudo_brasao` | projeto: Escudo do Brasão | Renome 3 | não |
| `bp_var_escudo_brasao` | variante visual do escudo (cosmético) | Renome 3 | não |

18 Blueprints no Old Forge (meta da v1.0: 60+). Fila esgotada → a encomenda Blueprint não aparece (vira Common no ciclo); Royal com fila esgotada paga só ouro e Renome.

## 5. Reputation (B-CORE-06)

### 5.1 Fontes

| Fonte | Renome | Wave |
|---|---|---|
| Encomenda Common / Rush (+bônus) / Blueprint | +1 / +1 (+1) / +2 | B |
| Encomenda Bulk / Quality / Mixed | +3 cada | B |
| Encomenda Royal | +10 | B |
| Baú de marco aberto | +5 | B |
| Peças Superiores vendidas | +1 a cada 10 | B |
| Masterwork registrada | +25 a +50 (§6) | B |
| Marco de coleção | +5 / +10 / +50 (§7; era +10 / +20, ver `BALANCE.md` §30.6) | B |
| Royal Charter | +100 | C |
| Royal Contract | +15 a +40 | D |
| Evento | por limiar do `EventDef` | D |

### 5.2 Níveis

| Nível | Nome | Total | Libera |
|---|---|---|---|
| 0 | Aprendiz | 0 | — |
| 1 | Ferreiro da rua | 20 | 2ª vaga de encomenda; encomendas Blueprint; Almoxarife |
| 2 | Oficina conhecida | 70 | encomendas Royal; Mestre de obras; cliente colecionador (só Fina ou melhor) |
| 3 | Mestre artesão | 180 | 3ª vaga; Quality pede Superior; Hall na rua (pedestais); avental de mestre (herói estágio 2); `bp_mw_escudo_brasao`, `bp_var_escudo_brasao` |
| 4 | Fornecedor do reino | 400 | condição do Royal Charter do Old Forge (Wave C); uniforme de mestre (herói estágio 3) |
| 5–10 | (distritos 2–6) | 750 / 1.250 / 2.000 / 3.000 / 4.500 / 6.500 | Charters e conteúdo da Wave C (calibrar no econsim) |

Save da 0.6: Renome inicial = encomendas entregues + 5 × baús abertos.

## 6. Masterworks — as 10 primeiras (Old Forge, B-CORE-08)

Base de todas: **peça que sairia Superior + golpe de mestre + projeto (Blueprint)**. A coluna "condição" soma algo a isso. Nenhuma condição pede anúncio ou compra.

| # | ID | Nome | Família | Projeto (fonte) | Condição | Renome | Recompensa visual |
|---|---|---|---|---|---|---|---|
| 1 | `mw_primeira_lamina` | Primeira Lâmina | espada | marco 100 espadas | — | +25 | 1º pedestal; abre o Hall |
| 2 | `mw_gume_brasa` | Gume de Brasa | espada | encomenda Blueprint | Bigornas nível 5 | +30 | pedestal |
| 3 | `mw_sunblade` | Lâmina Solar do Rei (King's Sunblade) | espada | fila Royal | 1.000 espadas vendidas | +50 | pedestal dourado + estandarte na fachada |
| 4 | `mw_primeira_guarda` | Escudo da Primeira Guarda (Shield of the First Guard) | escudo | encomenda Blueprint | 10 encomendas de escudo entregues | +30 | pedestal |
| 5 | `mw_broquel` | Broquel de Carvalho e Ferro | escudo | marco 300 escudos | Bancada de escudos nível 5 | +25 | pedestal |
| 6 | `mw_escudo_brasao` | Escudo do Brasão | escudo | Renome 3 | oficial de escudos nível 5 | +30 | brasão na fachada |
| 7 | `mw_martelo_fundador` | Martelo do Fundador | ferramenta | marco 300 ferramentas | Bancada de ferramentas nível 5 | +25 | martelo cosmético do ferreiro |
| 8 | `mw_tenaz_eterna` | Tenaz Eterna | ferramenta | encomenda Blueprint | 5 encomendas Mixed entregues | +25 | pedestal |
| 9 | `mw_ember_crown` | Coroa de Brasa (Ember Crown) | joia | fila Royal | Renome 4 | +50 | coroa no centro do Hall |
| 10 | `mw_anel_mercador` | Anel do Mercador | joia | marco 100 joias Fina+ | — | +30 | pedestal |

Alcance (regra do validator): o q máximo da família no distrito passa de 100 (espada, escudo, ferramenta e joia chegam a 115 com nível 5 + técnica + oficial nível 5 + mestre de obras). Famílias completas: espada (3), escudo (3), ferramenta (2), joia (2); cada família completa = **+5% fixo no preço daquela família** (FS-6).

## 7. Collection Book (B-CORE-08)

Derivada dos contadores; o save guarda só os marcos resgatados (ID de transação).

| Aba | Entrada | Total no Old Forge | Marcos |
|---|---|---|---|
| Produtos | produto × qualidade vendida (Normal, Fina, Superior) | 4 × 3 = 12 | 50%: +5 Renome · 100%: +10 Renome |
| Masterworks | Masterwork registrada | 10 | 1ª: abre o Hall · 5: +20 Renome · 10: +50 Renome + estandarte; família completa: +5% no preço dela |
| Workers | contratado; nível 5 | 13 workers (6 Carregadores + 4 oficiais + balconista, almoxarife, mestre de obras) × 2 = 26 | 50%: +5 · 100%: +10 e insígnia |
| Clientes | família atendida ≥ 1 vez (viajante, guarda, aventureiro, mercador, nobre, VIP, colecionador) | 7 (+5 fregueses se o FS-3 entrar) | 100%: +10 |
| Blueprints | Blueprint obtido | 18 | 50%: +5 · 100%: +10 |
| Distritos | Charter obtido | Wave C | — |

Marcos dão Renome e cosmético, nunca ouro direto (DM §34). A aba abre no 1º projeto de Masterwork (`bp_mw_primeira_lamina`, ~100 espadas).

## 8. Esquemas de dados

Campos comuns a todos: `id` (string estável, prefixo `item_`, `rec_`, `wk_`, `ord_`, `bp_`, `mw_`), `nameKey` (localização), `iconId`, `analyticsKey`, `district`. Regras comuns do validator: ID único, chaves de texto/ícone/analytics presentes (DM §108).

### 8.1 `ItemDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `item_espada` | único |
| `kind` | enum `Raw`, `Intermediate`, `Product` | minério/lingote/produto | — |
| `family` | string | `espada`, `elmo`… (agrupa qualidade, oficial, Masterwork) | família de produto tem ≥ 1 receita |
| `tier` | int 1–5 | DM §14 | 1–5 |
| `basePrice` | long | 0 se não vende | `Product` ⇒ > 0 |
| `hasQuality` | bool | intermediário = false | `Product` ⇒ true |
| `clientFamilies` | string[] | quem pede | famílias existem |
| `patienceBaseSeconds` | float? | vazio = regra derivada (`BALANCE.md` §13) | > 0 |

### 8.2 `RecipeDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `rec_espada` | único |
| `output` | `ItemDef.id` | | existe |
| `outputCount` | int | | ≥ 1 |
| `inputs` | lista (`ItemDef.id`, int) | multi-insumo (montagem no distrito 2) | cada insumo é produzido por alguém (sem ciclo) |
| `stationFamily` | string | `bigorna`, `bancada_escudo`… | estação existe no distrito |
| `craftSeconds` | float | tempo base | > 0 |
| `unlock` | condição (`upgrade`, `blueprint`, `district`, `reputation`) | | alcançável a partir do início |
| `techniqueBlueprint` | `BlueprintDef.id`? | técnica de qualidade da família | existe |
| `rotationIndex` | int | posição no rodízio de Orders (no lugar do "order weight") | sem peso nem probabilidade |

### 8.3 `WorkerDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `wk_bruna` | único |
| `role` | enum `Porter`, `Smith`, `Clerk`, `Quartermaster`, `Foreman`, `Smelter`, `Courier`, `Manager` | DM §22, §24 | — |
| `specialty` | enum `None`, `Swords`, `Shields`, `Tools`, `Jewels`, `Armor`, `Carrying`, `Service` | | ≤ 2 traços fortes |
| `hireCost` | long | | > 0, múltiplo de 5 |
| `hireUnlock` | condição | | alcançável |
| `maxLevel` | int | 5 no Old Forge, 10 depois | — |
| `levelCostBase`, `levelCostGrowth` | long, float | `base × growth^(L−2)` | custo crescente |
| `speedPerLevel` | float | | ≥ 0 |
| `capacityAtLevels` | int[] | níveis que dão +1 de carga | — |
| `qualityBonus` | int[] por faixa de nível (centésimos) | | ≥ 0 |
| `area` | enum `Station`, `District` | alcance do efeito | — |
| `skinId` | string | cosmético | existe |

### 8.4 `OrderDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `ord_common_oldforge` | único |
| `type` | enum `Common`, `Bulk`, `Quality`, `Rush`, `Mixed`, `Royal`, `Blueprint`, `Event` | DM §18 | — |
| `targetSeconds` | float | alvo em segundos de renda da linha | > 0 |
| `targetMin`, `targetMax` | int | | `min ≤ max` |
| `products` | int | 1 (Mixed: 2–3) | — |
| `minQuality` | int 0–2 | | ≤ Superior (Masterwork nunca é alvo) |
| `bonusDeadlineSeconds` | float | 0 = sem prazo; **só bônus** | não existe campo de penalidade |
| `rewardSeconds`, `rewardMin` | float, long | ouro | > 0 |
| `bonusRewardSeconds` | float | prêmio extra no prazo | ≥ 0 |
| `reputation`, `bonusReputation` | int | | ≥ 0 |
| `blueprintQueue` | string? | `order_blueprint` ou `royal` | fila existe |
| `unlock` | condição | | alcançável |
| `clientId` | string? | freguês nomeado (FS-3) | existe |
| `eventId` | string? | só `Event` | existe |
| `cycle` (no `DistrictDef`) | lista de `type` | rodízio do distrito | contém `Common`; sem peso, sem RNG |

### 8.5 `BlueprintDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `bp_bigorna_mestra` | único |
| `unlocks` | lista (`Recipe`, `StationLevel`, `Technique`, `Masterwork`, `Variant`, `Specialization`; alvo) | o que a posse libera | alvo existe |
| `sources` | lista (`OrderBlueprint`, `OrderRoyal`, `Milestone`, `Reputation`, `Achievement`, `Contract`, `Event`, `PassFree`, `PassPremium`, `Shop`; referência) | ordem da fila = ordem na lista do distrito | nenhuma fonte "aleatória" |
| `essential` | bool | trava distrito, Charter, Masterwork exigida ou contrato | `essential` ⇒ ≥ 1 fonte fora de `Shop`/`PassPremium`, alcançável sem anúncio |
| `queueOrder` | int | posição na fila da fonte | sem repetição na mesma fila |

### 8.6 `MasterworkDef`

| Campo | Tipo | Nota | Validator |
|---|---|---|---|
| `id` | string | `mw_sunblade` | único |
| `family` | string | família do produto | família existe |
| `recipe` | `RecipeDef.id` | peça base | existe |
| `blueprint` | `BlueprintDef.id` | projeto | existe; fonte grátis |
| `conditions` | lista (`Counter`, `StationLevel`, `ReputationTier`, `OrdersOfType`, `WorkerLevel`; ref; valor) | condição especial | nenhuma condição de anúncio ou compra |
| `reputation` | int | | > 0 |
| `cosmeticId`, `achievementId` | string? | | existe |
| `pedestal` | (distrito, índice) | lugar no Hall | único por distrito |
| `listOrder` | int | ordem de "pendente" | único por família |

Validator: q máximo da família no distrito > 100 (Superior alcançável); toda Masterwork tem pedestal.

### 8.7 Auxiliares pequenos

| Def | Campos |
|---|---|
| `ReputationTierDef` | `index`, `nameKey`, `threshold` (crescente), `unlocks` (lista de condições liberadas) |
| `CollectionTabDef` | `tab`, `entryRule` (contador que gera as entradas), `milestones` (percentual ou contagem → Renome/cosmético) |
| `UpgradeDef` (existente) | ganha `category` (`Production`, `Meta`, `Luxury`) e `effects` declarados (A-CORE-03); a marca `Luxury` atual vira `category` |

## 9. Invariantes e testes negativos (`coretests`)

1. Mesma entrada → mesma sequência de níveis de qualidade (dois `Sim` em passo igual).
2. `q = 25` → exatamente 1 Fina a cada 4 peças; `q = 115` → toda peça Fina ou melhor.
3. Ajudante, manager e offline nunca produzem Masterwork; golpe respeita a recarga.
4. Masterwork nunca é vendida, nunca entra no `GoldEarned`; a 2ª vez sob as mesmas condições sai Superior.
5. Renome nunca diminui; não existe método de gasto.
6. Orders: rodízio sem RNG (mesmo `OrderCount` → mesmo tipo e produto); tipo travado vira Common; 1 unidade conta para 1 encomenda; Rush depois do prazo paga o normal, nunca menos.
7. Todo prêmio (encomenda, marco, Masterwork, coleção) tem ID de transação e não duplica em save/load nem reabertura.
8. Validator: Blueprint essencial só com fonte `Shop` → erro; `OrderDef` com qualidade mínima Masterwork → erro; Masterwork inalcançável → erro; worker com 3 traços → erro.
9. Save da 0.6 → tudo Normal, acumuladores 0, Renome retroativo, encomenda atual vira vaga 1 Common, sem Blueprint.
10. Primeiros 10 min do bot F2P iguais aos da 0.6, tick a tick.

## 10. Portões do bot (econsim, perfis F2P, Perfect, Slow, AdWatcher)

| Medida | Hoje (`BALANCE.md` §20–22) | Portão da Wave B | Knob se falhar |
|---|---|---|---|
| Primeiros 10 min | janelas do GDD §3.3 | iguais, tick a tick | abrir mais tarde |
| Produção completa (26 produtivos), F2P | 41:31 | 41–55 min | preço L2/L3 |
| Zona morta até a produção, F2P | ~23 min | **≤ 12 min** | preço L2/L3, nível 2 dos Carregadores |
| Ouro sem destino aos 60 min, F2P | ~29 mil | **≤ 10 mil** | preço L4/L5 |
| Prêmio de qualidade (ouro acima do Normal) | — | 5–20% da receita aos 60 min; ≤ 35% aos 120 | valores de q, multiplicadores |
| Golpe de mestre | — | ≤ 8% da receita (F2P); ≤ 12% (Perfect) | recarga 20 → 45 s |
| Retorno medido (A/B com e sem) | regra 8–12 min | L2–L3 ≤ 20 min de renda; L4–L5 ≤ 40 min | preço do nível |
| Ouro de Orders | 5,0% (portão 3–10%) | 4–12% da receita aos 60 min | segundos de prêmio |
| Encomendas entregues em 60 min, F2P | 21 | ≥ 25 | alvos em segundos |
| Renome nível 1 / 2 | — | 12–25 min / 40–75 min (F2P) | limiares |
| Renome nível 3 / 4 | — | 1,5–3 h / 3–6 h acumuladas (F2P) | limiares |
| 1ª Masterwork | — | Perfect ≤ 2,5 h; F2P ≤ 4 h acumuladas | custos L4/L5 |
| AdWatcher, produção completa | 24:46 (−44%) | ≥ 0,75 × F2P (com a recarga do VIP, V11) | recarga |
| Deadlock em 1.000 partidas por perfil | **0** (2026-10-24: 5 perfis × 1.000 partidas × 60 min, `BALANCE.md` §43; o Steer da Wave A dava 1) | 0 | — |
| Ajudantes ociosos (`Ocioso_15Min`) | ≥ 150 vendas parado | ≥ 150 | — |

## 11. Ordem de implementação

1. **B-CORE-01** `ItemDef`/`RecipeDef` (as 4 linhas atuais em dados, assinatura do bot igual) + `UpgradeDef.category`.
2. **B-CORE-06** Renome (pequeno; destrava vagas e tipos).
3. **B-CORE-03** Quality (acumulador, preços, estoque/carga por nível, golpe, níveis de estação).
4. **B-CORE-04** `WorkerDef` (níveis dos Carregadores + 4 papéis).
5. **B-CORE-05** Orders 2.0 (8 tipos, rodízio, vagas).
6. **B-CORE-07** Blueprints + regra do validator.
7. **B-CORE-08** Masterworks (Hall) + Collection.
8. Rodar o econsim (§10), registrar no `BALANCE.md`, propor ajustes e pedir aprovação dos números.
