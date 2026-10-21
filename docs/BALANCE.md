# BALANCE — Forge Street v0.1 + 2ª área (medido pelo bot, 2026-10-06; 2ª área e correção da fila em 2026-10-07, §9; fase 2 da joalheria e baús de marco, §10)

Tudo aqui saiu de `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` (`BalanceTests`). Nenhuma pessoa jogou ainda: os tempos humanos são **HIPÓTESE** derivada do bot. Mudou um número em `Assets/_FS/Scripts/Core/Defs.cs` (`Balance`), roda o teste e atualiza este arquivo.

**Versão vigente: §14 (fase 4 por cima da fase 5: Mineiro 3.000 e Joalheiro 2 2.400, 22 produtivos) sobre §13 (fase 5, Etapa A: física, duas bocas por estação, parede lateral com porta e arco, paciência por item).** Luxo segue 11.000/14.000/18.000 (nova proposta em §14.6, não aplicada). Antes da física: §10.7 (Passo A, 20 upgrades, custos 2.200/5.500/9.000 e joia 80 com Vitrine). §1–9 preservam as medições da oficina e da fase 1; tetos offline "tudo comprado" e contagens dessas seções são históricos. Teto atual em §10.5; guardas atuais em §10.6. **Luxo (fase 3, 3 compras sem bônus): vigente 11.000/14.000/18.000, medição e portão em §11** (6.000/10.000/16.000 = histórico, §11.4). **Luxo marcado por upgrade (`UpgradeDef.Luxury`), sem mudança de número: §12.**

## 1. Fórmula de custo dos 17 upgrades

`custo(tier) = 50 × 1,30^tier`, arredondado ao múltiplo de 5. O tier é a posição no enum `Upgrade` (ordem de compra pretendida). Pré-requisitos só impedem pular a cadeia (Fole duplo ← Fole; Ajudante 2 ← 1; Ajudante 3 ← 2; Ajudantes ágeis ← Ajudante 1; Ferramentas ← Escudos; 2ª fornalha ← 2ª bigorna).

| tier | upgrade | efeito no fluxo | custo | acumulado |
|---|---|---|---|---|
| 0 | Fole | fornalha 4,0 → 2,5 s | 50 | 50 |
| 1 | 2ª bigorna | 2ª estação de espadas | 65 | 115 |
| 2 | Ajudante | carrega minério → fornalhas | 85 | 200 |
| 3 | Escudos | 2ª linha (2 lingotes, paga 25) | 110 | 310 |
| 4 | Mochila | jogador carrega 6 | 145 | 455 |
| 5 | Ajudante 2 | carrega lingotes → bancadas | 185 | 640 |
| 6 | Esteira | fornalha A → bigorna A sem andar (1 lingote/s) | 240 | 880 |
| 7 | Fole duplo | fornalha 2,5 → 1,6 s | 315 | 1195 |
| 8 | Botas | jogador 3,0 → 4,2 m/s | 410 | 1605 |
| 9 | Ferramentas | 3ª linha (1 lingote, paga 16) | 530 | 2135 |
| 10 | Vitrine | estoque 5 → 10, fila 4 → 6, clientes 30% mais frequentes | 690 | 2825 |
| 11 | 2ª fornalha | dobra lingotes | 895 | 3720 |
| 12 | Ajudante 3 | carrega produtos → balcão | 1165 | 4885 |
| 13 | Ajudantes ágeis | 2,4 → 3,4 m/s, carga 2 → 4 | 1515 | 6400 |
| 14 | Martelo veloz | bancadas 5 → 3 s | 1970 | 8370 |
| 15 | Corredor | abre a rua lateral (x 9–15) | **690** (fora da fórmula, §9) | 9060 |
| 16 | Joalheria (← Corredor) | 4ª linha: 2 lingotes → joia, paga 60, loja e fila próprias | **1515** (fora da fórmula, §9) | 10575 |

Teste `Curva_DeCusto_Dos17Upgrades`: 17 ids únicos, razão entre tiers consecutivos em [1,15; 1,5] nos 15 do v0.1 (Corredor/Joalheria = 690/1.515, fora da fórmula), preço múltiplo de 5, todo pad visível quando chega a vez dele.

## 2. §3 do GDD × bot

> Desde a fase 2 (§10) os baús de marco adiantam o meio da curva (ajudante 3:16 → 2:31, esteira 8:13 → 6:31); a tabela abaixo é a medição de antes dos baús. A vigente está em §10.3.

Bot "humano" = `new Bot()` (0,7 s parado quando o alvo muda, 85% de stick). Bot ideal = `Bot.Ideal()` (limite inferior de tempo). Janela aceita pelo teste: do piso (30% para o fole, 50% para os demais) até o tempo do GDD. **HIPÓTESE:** uma pessoa é 1,3–2× mais lenta que o bot humano, mais no 1º minuto (aprender o joystick); o `upgrade_buy` do `diario.csv` calibra.

| marco (GDD §3) | GDD | bot humano | bot ideal | fração do GDD |
|---|---|---|---|---|
| 1ª venda | <1:30 | **0:19** | 0:17 | 21% |
| Fole (velocidade da fornalha) | 1:30–2:30 | **0:53** | 0:52 | 35% |
| 2ª bigorna | 2:30–3:30 | **1:59** | 1:56 | 57% |
| Ajudante 1 | 3:30–4:30 | **3:16** | 3:11 | 73% |
| Escudos | 4:30–5:30 | **4:19** | 4:17 | 78% |
| Mochila | — | 5:34 | 5:34 | — |
| Ajudante 2 | — | 7:09 | 6:57 | — |
| Esteira | 8:30 | **8:13** | 7:57 | 97% |
| Fole duplo | — | 9:26 | 9:17 | — |
| comprados em 10 min | — | 8 de 15 | 8 de 15 | sobra razão para voltar |

A fração sobe de 35% para 97%: o bot é muito mais rápido que uma pessoa no começo (não erra o dedo) e quase igual quando a oficina já anda sozinha. Por isso o teste aceita o fole cedo e aperta a esteira.

## 3. Minuto a minuto (bot humano)

```
t=1:00  gold=10  earned=60    upgrades=1 sales=6   lost=0  away=0  walk=16s  ema=0.72/s
t=2:00  gold=5   earned=120   upgrades=2 sales=12  lost=3  away=2  walk=35s  ema=0.96/s
t=3:00  gold=65  earned=180   upgrades=2 sales=18  lost=5  away=3  walk=60s  ema=0.99/s
t=4:00  gold=60  earned=260   upgrades=3 sales=26  lost=7  away=5  walk=79s  ema=1.24/s
t=5:00  gold=60  earned=370   upgrades=4 sales=37  lost=7  away=10 walk=98s  ema=1.57/s
t=6:00  gold=50  earned=505   upgrades=5 sales=43  lost=13 away=19 walk=118s ema=1.93/s
t=7:00  gold=175 earned=630   upgrades=5 sales=51  lost=16 away=30 walk=140s ema=1.99/s
t=8:00  gold=185 earned=825   upgrades=6 sales=66  lost=20 away=34 walk=157s ema=2.95/s
t=9:00  gold=230 earned=1110  upgrades=7 sales=84  lost=23 away=39 walk=175s ema=4.04/s
t=10:00 gold=160 earned=1355  upgrades=8 sales=101 lost=25 away=48 walk=193s ema=4.11/s
```
`lost` = cansou na fila (25 s); `away` = fila cheia, nem entrou; `walk` = andar de mãos vazias sem pad pagável; `ema` = ouro/s online (média móvel de 60 s, base do offline).

Renda: ~60 ouro/min nos 3 primeiros minutos (1 pessoa carregando 3 de cada vez), 130/min aos 5–6 min (ajudante + escudos), 250–285/min aos 8–10 (esteira + 2 ajudantes + fama). Cadência de compra ≈ 1 a cada 70 s do minuto 1 ao 9.

## 4. Gargalos por estação (10 min, bot humano)

| estação | fome (entrada vazia) | travada (saída cheia) | feitos |
|---|---|---|---|
| Fornalha | 131 s | 115 s | 143 lingotes |
| Bigorna | 249 s | 3 s | 69 espadas |
| Bigorna 2 | 415 s | 0 s | 13 espadas |
| Escudos | 151 s | 0 s | 25 escudos |

Leitura: a fornalha fica **travada** (ninguém tira os lingotes) 20% do tempo e as bigornas com **fome** 40–70%: o gargalo do meio do jogo é **carregar lingotes**, não produzi-los. É o sinal que o jogador precisa ler (saída da fornalha piscando vermelho, bigorna apagada): a compra certa ali é Ajudante 2 ou Esteira, não Fole duplo. A 2ª bigorna sozinha rende pouco (13 espadas) até chegar a Esteira: candidata a trocar de tier com a Mochila se o playtest confirmar.

- **Andar sem decisão:** 193 s de 600 s (32%) no bot humano; 51% no ideal (ele replaneja a cada tick e vai e volta). Kill criterion do GDD §26 ("andar entre pilhas") só se mede com gente; o teste exige <50%.
- **Clientes:** 101 vendas, 25 cansaram, 48 não couberam na fila (cap 4). Um terço da demanda não é atendida: fricção visível ("não deixe clientes esperando", criativo #6). Se no playtest parecer fracasso e não pressão, baixar `FamePer10Sales` para 0,96 ou subir `Patience`.
- **Primeira venda** 0:19 (bot) com o 1º cliente chegando aos 8 s: para gente, a estimativa é 45–75 s (GDD §15 pede <90 s para ≥90%).

## 5. Offline (decisão do coordenador, 2026-10-06)

`ouro = min( EMA(ouro/s) × 0,25 × min(Δt, 7200 s) , 2 × preço do upgrade mais barato ainda travado )`; "mais barato" = o **menor** custo entre os não comprados (desde a 2ª área os custos não crescem mais com o tier); tudo comprado → 2× o último, a Joalheria (3.030; eram 3.940 com 15); claim idempotente por `SavedAt`; Δt ≤ 0 = 0.

Medido: depois de 10 min do bot (EMA 4,11/s, 8 upgrades comprados, próximo travado = Botas 410), 8 h fora rendem **820 ouro** (pela taxa seriam 7.390; na regra anterior de 50% sem teto relativo eram 16.337, que compravam os 7 upgrades restantes de uma vez). O cofre agora paga no máximo o próximo upgrade e mais um pouco: a razão de voltar continua sendo jogar. Testes: `Offline_Teto2h_25Porcento_NegativoZero_Idempotente` e `Offline_NuncaPagaMaisQue2xOUpgradeMaisBaratoTravado` (provado vermelho removendo o teto relativo).

## 6. Varredura de custo (base × crescimento), bot humano, 10 min

(Medida em 2026-10-06, antes da correção da fila da §9; a correção muda a esteira em 5 s, não a escolha da curva.)

Feita com `Balance.CostBase/CostGrowth` temporariamente estáticos (o código voltou para `const`). Linhas = base; colunas = fole / 2ª bigorna / ajudante / escudos / mochila / ajudante 2 / esteira.

```
base growth | fole  anvil2 helper shield mochila helper2 esteira | upg earned
  40  1,25  |  0:48   1:32   2:28   3:19    4:23    5:07    5:59 |   9   1210
  40  1,35  |  0:48   1:37   2:49   3:50    5:09    6:40    7:56 |   8   1430
  50  1,25  |  0:53   1:54   3:12   4:12    5:16    6:28    7:08 |   9   1595
  50  1,30  |  0:53   1:59   3:16   4:19    5:34    7:09    8:18 |   8   1385   <- escolhido
  50  1,35  |  0:53   1:59   3:20   4:38    6:11    7:43    9:15 |   7   1275
  60  1,25  |  0:58   2:19   3:41   4:59    6:11    7:36    8:39 |   8   1285
  60  1,30  |  0:58   2:19   3:45   5:03    6:36    8:07    9:33 |   7   1200
  70  1,25  |  1:21   2:35   4:18   5:38    6:58    8:52    9:56 |   7   1065
  80  1,30  |  1:27   3:12   5:00   6:49    8:38   --:--   --:-- |   5    855
```
O fole chega a ~8 vendas em qualquer curva (o 1º minuto é limitado por carregar 3 de cada vez); o que a base muda é o resto da curva. 50/1,30 é a única que fecha os cinco marcos da §3 abaixo do tempo do GDD com a esteira em ~8:30.

## 7. Histórico do que a medição mudou (mesma sessão)

1. **Pads nas diagonais engoliam 100% do ouro** (140 na esteira, 490 no martelo; 0 upgrades em 10 min). Pads foram para as paredes e passaram a cobrar só parado ou após 0,5 s em cima; rearmam ao sair. Depois disso o bot comprou a 2ª fornalha (tier 11) por acidente atravessando o pad no local da estação: a regra do "parado" fechou isso.
2. **Fila FIFO estrita derrubou a renda de 2,8 para 1,0 ouro/s** depois dos escudos (cliente de escudo na frente sem escudo = ninguém compra espada). Balcão passou a atender o primeiro que dá.
3. **Demanda fixa (cliente de espada a cada 6 s) prendia a renda em 100 ouro/min:** bigornas com fome 75% do tempo e a 2ª bigorna inútil. Entrou a fama; a bigorna ficou mais lenta (5 s) que a fornalha com fole para a §3 fazer sentido.
4. **Base 30 era 2× rápida demais; base 80, 2× lenta.** A varredura fechou em 50/1,30.
5. **(2026-10-07) Trava da fila aos 12 min**, achada ao rodar o bot por 45 min (ver §9.1). Corrigida com a compra direta na vitrine.

## 8. Como repetir

```bash
dotnet test client/tools/coretests --logger "console;verbosity=detailed"
```
`Bot_Primeiros10Minutos_BatemASecao3` imprime os dois relatórios (ideal e humano); `Offline_DepoisDe10Min…` imprime o cofre; `Bot_45Minutos_SegundaArea_CorredorEJoalheria` imprime os 45 min e a linha de joias (§9). No build: `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile autoplay.log`.

## 9. 2ª área: corredor lateral + joalheria (contrato `docs/archive/AREA2_JOALHERIA.md`, medido 2026-10-07)

**Vigente (decisão do coordenador, 2026-10-07):** Corredor **690**, Joalheria **1.515** (requer Corredor). Os dois ficam no fim do enum (tiers 15/16) por causa do save antigo, mas o preço sai de `Balance.SideCorridorCost`/`JewelryCost`, fora da fórmula por tier (`Upgrades.Cost`). Por isso `Sim.CheapestLockedCost` (teto do cofre offline) passou a pegar o **menor** custo entre os não comprados. Linha: joia = 2 lingotes, paga 60, bancada 2× o tempo da bigorna (10 s; 6 s com Martelo veloz), nobre a cada 14 s × fama × vitrine, fila de 3, paciência 40 s.

### 9.1 Achado antes de medir: trava da fila (pré-existente no v0.1)

Com o código de 2026-10-06, o bot humano **para de ganhar aos 12 min e fica parado até os 45** (ouro congelado em 450, sem chegar nem às Ferramentas). Estado: jogador com 1 espada na mão na frente do balcão com 5/5 espadas; fila (4) cheia de clientes de escudo; escudos prontos na bancada mas ninguém para levar (a mão está ocupada e não existe "largar"); todo cliente de espada encontra a fila cheia e vai embora. Na fama mínima os timers ficam em fase (escudo 3,6 s = 1,5 × espada 2,4 s) e toda vaga aberta por um cliente cansado é ocupada por outro cliente de escudo: a trava é permanente. Uma pessoa cai no mesmo estado (pega 3 espadas de uma vez com 1 vaga no balcão).

**Correção (Sim.Clients), aprovada pelo coordenador em 2026-10-07:** cliente que chega com a fila cheia mas acha o produto na vitrine compra direto (mesmo preço, mesmo evento `Sold`, na posição da vaga depois da última). Sem estoque, continua "sem vaga". Teste `FilaCheia_ProdutoNaVitrine_ClienteCompraDireto_NaoTrava` (provado vermelho tirando a regra; o teste de 45 min também fica vermelho). Efeito nos 10 primeiros minutos: esteira 8:18 → 8:13, fole duplo 9:22 → 9:26, ganho em 10 min 1.385 → 1.355; todos os marcos da §3 continuam dentro (§2/§3/§4 já atualizados).

### 9.2 Bot humano, 45 min (custos vigentes 690 / 1.515)

Ordem depois dos 10 min: Botas 10:47 · Ferramentas 12:04 · Vitrine 13:37 · **Corredor 15:18** · 2ª fornalha 17:17 · Ajudante 3 20:01 · Ajudantes ágeis 23:23 · **Joalheria 26:02** · Martelo veloz 28:24 (= tudo comprado).

```
t=15:00 gold=582   earned=3407  upgrades=11 sales=242  ema=7.00/s
t=20:00 gold=1173  earned=5583  upgrades=13 sales=390  ema=7.44/s
t=25:00 gold=915   earned=8005  upgrades=15 sales=559  ema=9.12/s
t=30:00 gold=1621  earned=12196 upgrades=17 sales=783  ema=16.33/s joias=20
t=35:00 gold=6174  earned=16749 upgrades=17 sales=1000 ema=15.38/s joias=49
t=40:00 gold=10754 earned=21329 upgrades=17 sales=1225 ema=15.64/s joias=77
t=45:00 gold=15006 earned=25581 upgrades=17 sales=1445 ema=15.55/s joias=98
```

| ouro/min (janelas de 5 min) | medido |
|---|---|
| antes do Corredor (10–15) | 410 |
| antes da Joalheria (21–26) | 516 |
| depois da Joalheria (27–32) | **925 (+79%)** |
| fim (40–45) | 850 |

- **Joias:** 99 feitas, **98 vendidas** (5.880 ouro = 35% do ganho depois da Joalheria; 5,2 joias/min), 13 nobres cansaram, 172 nem entraram (fila de 3 cheia). A bancada passa **36% do tempo aberta com fome** (405 s de 1.138 s): a linha é limitada por oferta (fica a 10 m das fornalhas e disputa lingote com 4 bancadas).
- **Retorno:** +409 ouro/min sobre 1.515 → a Joalheria se paga em ~4 min; Corredor + Joalheria (2.205) em ~5–6 min.
- **Fim de conteúdo:** aos 28:24 não há mais nada para comprar; o ouro acumula até 15.006 aos 45 min.
- **GDD §3 × medido:** o GDD põe o corredor aos 6:30 e o teaser da joalheria aos 9:30; o bot compra aos 15:18 / 26:02 (uma pessoa: estimativa 20–35 min). Nenhum preço medido bate o 6:30 (ver §9.3).
- **Offline:** teto "2× o menor travado"; tudo comprado → 2× o último (Joalheria) = 3.030. Depois de 10 min de bot nada muda (820 ouro; menor travado = Botas 410).
- **Teste (portão de regressão, folgado, medido + ~30%):** Corredor ≤ 20:00, Joalheria ≤ 34:00, > 3,6 joias/min depois da Joalheria (piso 70% de 5,2) e ouro/min depois > antes.

### 9.3 Histórico: custos do contrato original (2.560 / 3.325) e varredura

Com o preço da fórmula (tiers 15/16 = 2.560 / 3.325) o bot comprava o Corredor aos 28:38 e a Joalheria aos 33:28 (os dois mais caros ficam por último: o bot compra sempre o pad pagável mais barato), 51 joias em 45 min, ouro/min 699 → 890 (+27%), tudo comprado aos 33:28. A varredura abaixo (cópia do core com `Cost(15)`/`Cost(16)` sobrescritos, bot humano, 45 min) levou à decisão de 690 / 1.515.

```
corredor joalheria | t_corr  t_joal | joias  ganho_45min | ouro/min 5 antes / 5 depois da joalheria | tudo comprado
    2560       3325 |  28:38  33:28 |    51      24211   |    699 / 890                             | 33:28   <- contrato original
     240        895 |   9:05  18:43 |    86      24640   |    435 / 441                             | 27:32
     410       1165 |  11:44  22:05 |    93      24847   |    451 / 598                             | 28:11
     690       1515 |  15:18  26:02 |    98      25581   |    516 / 925                             | 28:24   <- vigente
     895       1970 |  17:48  29:53 |    71      24421   |    644 / 903                             | 29:53
```
Leitura: abrir a rua cedo (240/895) quase não muda a renda: sem Ajudante 3 e Martelo veloz a joalheria fica com fome e a joia sai a 10 s. 690/1.515 dá o maior ganho em 45 min e o maior salto de renda.

### 9.4 Próximos passos (registrados, NÃO feitos — decisão do coordenador 2026-10-07)

1. **Bancada de joias com fome (36% do tempo aberta):** alimentar melhor a joalheria (ex.: esteira/fornalha na rua lateral, ou ajudante dedicado). Medir com o mesmo `Bot_45Minutos…`.
2. **Ralo de ouro depois do último upgrade (28:24):** o ouro acumula sem uso (15.006 aos 45 min). Próxima área ou upgrades da joalheria.

## 10. 2ª área, fase 2: Joalheiro, Lupa, Vitrine de joias e baús de marco (contrato `docs/archive/AREA2_FASE2.md`, medido 2026-10-07)

**Vigente (Passo A do handoff, aplicado e medido em 2026-10-07):** Joalheiro **2.200**, Lupa **5.500**, Vitrine de joias **9.000** (tiers 17–19, todos ← Joalheria, fora da fórmula como Corredor/Joalheria: `Balance.JewelerCost/JewelSpeedCost/JewelVitrineCost`). Joalheiro = ajudante papel 3 (lingote de qualquer fornalha → só a bancada de joias; vazio e joia pronta → loja de joias; Ajudantes ágeis valem para ele). Lupa: bancada de joias × 0,6 (10 s → 6 s; com Martelo veloz 6 s → 3,6 s). Vitrine de joias: fila de nobres 3 → 5, intervalo × 0,7 (soma com a Vitrine) e preço da joia **60 → 80**, na fila e na compra direta. Medição vigente em **§10.7**; §10.1/§10.2/§10.4 preservam o histórico anterior ao Passo A.
**Baús** (`Balance.Milestones`): **15 espadas → 60** (desvio: o contrato dizia 10, ver §10.3), 50 vendas → 250, 10 joias → 600, 200 vendas → 1.200. Ouro de baú vai para `Gold` mas **não** para `GoldEarned` (não infla a taxa online nem o cofre offline).

### 10.1 Histórico antes do Passo A: bot humano, 60 min (`Bot_60Minutos_Fase2`, custos originais e joia 60)

Compras: Fole 0:53 · 2ª bigorna 1:59 · Ajudante 2:31 · Escudos 3:39 · Mochila 5:08 · Ajudante 2 6:26 · Esteira 6:31 · Fole duplo 7:42 · Botas 9:01 · Ferramentas 10:20 · Vitrine 11:54 · **Corredor 12:21** · 2ª fornalha 12:52 · Ajudante 3 15:29 · Ajudantes ágeis 18:55 · **Joalheria 21:32** · Martelo veloz 23:57 · **Joalheiro 25:25** · **Lupa 28:29** · **Vitrine de joias 32:20** (= tudo comprado). Nenhuma compra acidental (o bot só comprou o pad que mirava; o pad da Lupa em (10,2; 6,5) fica perto da linha fornalha → joalheria, mas atravessar sem parar não paga).

Baús (bateu / abriu): 15 espadas 2:23 / 2:24 · 50 vendas 6:18 / 6:20 · 200 vendas 12:17 / 12:18 · 10 joias 23:54 / 24:02. Total 2.110 ouro de baú em 60 min.

```
t=5:00  gold=140   earned=390   upgrades=4  sales=39   ema=1.53/s  joias=0
t=10:00 gold=385   earned=1680  upgrades=9  sales=126  ema=5.93/s  joias=0
t=15:00 gold=1014  earned=3914  upgrades=13 sales=281  ema=7.55/s  joias=0
t=20:00 gold=601   earned=6181  upgrades=15 sales=436  ema=8.56/s  joias=0
t=25:00 gold=1745  earned=10210 upgrades=17 sales=657  ema=16.07/s joias=17
t=30:00 gold=1631  earned=15196 upgrades=19 sales=881  ema=17.20/s joias=55
t=35:00 gold=2682  earned=20047 upgrades=20 sales=1100 ema=16.37/s joias=94
t=40:00 gold=7297  earned=24662 upgrades=20 sales=1319 ema=14.27/s joias=126
t=45:00 gold=12275 earned=29640 upgrades=20 sales=1538 ema=16.86/s joias=167
t=50:00 gold=16947 earned=34312 upgrades=20 sales=1746 ema=15.31/s joias=203
t=55:00 gold=21721 earned=39086 upgrades=20 sales=1961 ema=16.63/s joias=240
t=60:00 gold=26479 earned=43844 upgrades=20 sales=2180 ema=15.61/s joias=276
```

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| medido | 78 | 258 | 446 | 453 | 805 | 997 | 970 | 923 | 995 | 934 | 954 | 951 |

- **Joias/min no jogo corrido:** Joalheria → Joalheiro **5,7** · Joalheiro → Lupa **6,5** · depois de tudo **7,4** (276 joias em 60 min).
- **Bancada de joias sem lingote no jogo corrido:** antes do Joalheiro **7%** (21:32–25:25: a bancada ainda faz joia em 10 s, sem Martelo veloz, e quase não come) · Joalheiro → Lupa **35%** · Joalheiro → 60 min **53%** (com a Lupa a bancada come 0,55 lingote/s e volta a esperar). No jogo corrido os upgrades se misturam; a comparação limpa é a §10.2.
- **Fim de conteúdo histórico aos 32:20** (antes da fase 2: 28:24). Ficava antes dos ~40 min pedidos: a proposta de §10.4 ainda não estava aplicada nesta medição. Ouro parado no fim histórico: 26.479 aos 60 min. Vigente: §10.7.

### 10.2 Histórico antes do Passo A: A/B isolado (cópia do core no scratchpad; janela 26:00–60:00; upgrade "nunca" = não comprado)

| cenário | joias/min | ouro/min | bancada de joias sem lingote | nobres sem vaga/min | outras vendas/min |
|---|---|---|---|---|---|
| só a Joalheria (fase 1) | 4,4 | 859 | **52%** | 9,4 | 39,1 |
| + Joalheiro (25:25) | **6,8 (+55%)** | 931 (+8%) | **32%** | 8,4 | 35,9 |
| + Lupa (28:29) | 7,3 | 956 (+3%) | 54% | 7,9 | 36,0 |
| + Vitrine de joias (32:20) | 7,4 | 957 (+0%) | 54% | **11,9** | 36,0 |

Leitura: o **Joalheiro** resolve o que a §9.4 apontou (fome 52% → 32%, +55% joias, +72 ouro/min: se paga em ~30 min); as outras linhas vendem 8% menos porque o lingote vai para a joia (30 ouro/lingote contra 10–16 nas outras). A **Lupa** quase não rende (+3%): o limite volta a ser lingote. A **Vitrine de joias não rende nada medível** (+0% ouro): a linha é limitada por oferta, e mais nobres só aumentam os que vão embora sem vaga (7,9 → 11,9/min). É ralo de ouro sem retorno visível para o jogador (ver riscos).

### 10.3 Primeiros 10 min com baús e o desvio "15 espadas" (§3 do GDD)

Bot humano: fole 0:53 (35% do GDD) · 2ª bigorna 1:59 (57%) · ajudante 2:31 (56%) · escudos 3:39 (66%) · esteira 6:31 (77%) · 9 de 20 upgrades em 10 min (eram 8) · andar sem decisão 30%. Todos dentro das janelas do `Bot_Primeiros10Minutos_BatemASecao3`. Offline depois de 10 min: 1.060 ouro (2× Ferramentas 530; eram 820).

Com o baú do contrato (10 espadas → 60) o teste da §3 fica **vermelho**: 10 espadas saem aos 1:35, o bot abre aos 1:36 e compra a 2ª bigorna aos **1:39**, abaixo do piso de 1:45 (50% de 3:30). Varredura (cópia do core, 10 min):

```
espadas ouro | abre o bau | fole  bigorna2 ajudante escudos esteira | §3
  10    60   |   1:36     | 0:53   1:39     2:26     3:30    6:41   | FALHA (bigorna2 < 1:45)
  10    30   |   1:36     | 0:53   1:39     2:51     3:55    6:49   | FALHA (o bot ja tinha ~40 guardados)
  12    60   |   2:15     | 0:53   1:59     2:23     3:26    6:38   | ok, ajudante a 8 s do piso (2:15)
  15    60   |   2:24     | 0:53   1:59     2:31     3:39    6:31   | ok   <- aplicado
  20    60   |   3:23     | 0:53   1:59     3:16     3:51    7:01   | ok
```
Baixar o ouro não resolve (o baú chega quando o bot já guardou quase os 65); resolve o baú chegar depois da 2ª bigorna. 15 espadas é o menor número com folga nos dois pisos. Obs.: o GDD §3 põe o "milestone 10 espadas" aos 7:30; com qualquer limiar em espadas o 1º baú cai bem antes (o de 50 vendas cai aos 6:18, perto da janela do GDD).

### 10.4 Histórico da varredura de custo (cópia do core, bot humano, 60 min, joia 60) — proposta aceita no Passo A (§10.7)

```
joalheiro  lupa vitrine | joalheiro lupa  vitrine | tudo comprado | ganho 60 min  joias | ouro/min 35-40 55-60
     2200  2900    3800 |   25:25   28:29  32:20  |    32:20      |    43844       276  |   923   951   <- contrato original (historico)
     3300  4350    5700 |   26:43   31:10  36:55  |    36:55      |    43884       277  |   962   936
     4400  5800    7600 |   27:53   34:05  42:02  |    42:02      |    43620       269  |   954   998
     2200  5500    9000 |   25:25   31:12  40:41  |    40:41      |    43577       269  |   970   947   <- proposta
     2200  6000   10000 |   25:25   31:44  42:09  |    42:09      |    43732       275  |   950   943
     3000  6000    9000 |   26:24   32:35  42:01  |    42:01      |    43693       267  |   967   982
     4000  7000   10000 |   27:34   34:47  45:26  |    45:26      |    43595       269  |   925   957
```
**Proposta histórica, agora aplicada: Joalheiro 2.200 / Lupa 5.500 / Vitrine de joias 9.000** → tudo comprado aos 40:41, o Joalheiro (o que conserta a fome) continua aos 25:25, ganho em 60 min próximo (43,6 mil × 43,8 mil). Nesta varredura a joia pagava 60; mudar somente o custo da Lupa e da Vitrine quase não muda a renda (§10.2). No Passo A também entrou o preço 80 da Vitrine, com efeito isolado medido em §10.7.

### 10.5 Offline com os 3 novos

`CheapestLockedCost` continua "o menor travado": com o v0.1 e a Joalheria comprados, o menor travado é o Joalheiro (teto 2 × 2.200 = 4.400). Tudo comprado → 2 × o último do enum (Vitrine de joias): **18.000** (histórico: 7.600 com o contrato original; 3.030 antes da fase 2). O preço 80 entra na taxa online, preservando os mesmos tetos. Ouro sem destino depois de tudo comprado continua pendente. Teste: `Offline_NuncaPagaMaisQue2xOUpgradeMaisBaratoTravado`.

### 10.6 Portão de regressão (`Bot_60Minutos_Fase2`, folgado: medido + ~30%)

Vigente depois do Passo A: todos os 20 comprados em 60 min; Joalheiro ≤ **33:10**, Lupa ≤ **40:40**, Vitrine de joias ≤ **53:00** (tempos medidos × 1,3, arredondados para cima em 10 s). Baús até 3:10 / 8:20 / 31:20 / 16:00; joias/min depois de tudo > antes do Joalheiro e > **5,0** (~70% de 7,2); ouro/min 55–60 > **766** (~70% de 1.095). Histórico antes do Passo A: 33:00 / 37:00 / 42:00, pisos 5,2 e 665. O teste de 45 min e o guarda dos primeiros 10 min continuam verdes na suíte final; medição vigente e evidências em §10.7.

Como repetir: `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` (o `Bot_60Minutos_Fase2` imprime os números vigentes da §10.7). As medições históricas §10.2 e §10.4 rodaram numa cópia do core (custos como `static`), como na §9.3.

### 10.7 Passo A — custos aprovados e Vitrine com joia 80 (vigente, 2026-10-07)

Fonte de decisão: handoff `PROMPT_CONTINUACAO_CHATGPT.md` (removido em 2026-10-09; está no histórico do git, commit 2df0f0d) §3 e §6A. Aplicado: Joalheiro **2.200**, Lupa **5.500**, Vitrine de joias **9.000**; descrição "Nobres pagam 80 e a fila cresce"; `Balance.JewelPriceUp = 80`. `Sim.PriceOf(Item)` é usado por `Sell` nos dois caminhos (fila e compra direta), inclusive `GoldEarned` e `Ev.Sold.B`. Antes da Vitrine a joia continua em 60; outras linhas mantêm 10/25/16. Bot e dicas não têm preço de joia fixo e não precisaram mudar.

#### Antes/depois: mesmo bot humano, 60 min, 30 ticks/s

| cenário | Joalheiro | Lupa | Vitrine / tudo comprado | receita online em 60 min | ouro restante | joias vendidas | ouro/min 55–60 |
|---|---|---|---|---|---|---|---|
| antes: 2.200 / 2.900 / 3.800, joia 60 | 25:25 | 28:29 | 32:20 | 43.844 | 26.479 | 276 | 951 |
| somente novos custos, joia 60 (cópia isolada) | 25:25 | 31:12 | 40:41 | 43.577 | 18.412 | 269 | 947 |
| vigente: novos custos, joia 80 com Vitrine | 25:25 | 31:12 | **40:41** | **46.377** | **21.212** | **269** | **1.095** |

O A/B isolado mantém os novos custos e altera somente o pagamento da joia: mesma sequência de vendas/compras, **+2.800 ouro em 60 min (+6,4%)** e renda final **947 → 1.095 (+15,6%)**. A Vitrine deixa de ter retorno nulo: cada joia vendida depois dela rende +20 ouro. O fim de conteúdo é igual nos dois cenários porque a regra do preço só entra depois de comprá-la. Comparar 951 → 1.095 mistura custo e preço; para atribuir o efeito use o A/B de 947 → 1.095.

Compras até o Joalheiro mantidas: Fole 0:53, 2ª bigorna 1:59, Ajudante 2:31, Escudos 3:39, Mochila 5:08, Ajudante 2 6:26, Esteira 6:31, Fole duplo 7:42, Botas 9:01, Ferramentas 10:20, Vitrine 11:54, Corredor 12:21, 2ª fornalha 12:52, Ajudante 3 15:29, Ajudantes ágeis 18:55, Joalheria 21:32, Martelo veloz 23:57 e Joalheiro 25:25. Baús abertos na ordem dos marcos: **2:24 / 6:20 / 24:02 / 12:18**, total 2.110 ouro; os primeiros 10 min e o cofre depois de 10 min continuam iguais à §10.3.

```
t=5:00  gold=140   earned=390   upgrades=4  sales=39   ema=1.53/s  joias=0
t=10:00 gold=385   earned=1680  upgrades=9  sales=126  ema=5.93/s  joias=0
t=15:00 gold=1014  earned=3914  upgrades=13 sales=281  ema=7.55/s  joias=0
t=20:00 gold=601   earned=6181  upgrades=15 sales=436  ema=8.56/s  joias=0
t=25:00 gold=1745  earned=10210 upgrades=17 sales=657  ema=16.07/s joias=17
t=30:00 gold=4386  earned=15051 upgrades=18 sales=880  ema=15.47/s joias=52
t=35:00 gold=3638  earned=19803 upgrades=19 sales=1096 ema=15.55/s joias=88
t=40:00 gold=8492  earned=24657 upgrades=19 sales=1311 ema=15.83/s joias=126
t=45:00 gold=4873  earned=30038 upgrades=20 sales=1523 ema=19.91/s joias=162
t=50:00 gold=10290 earned=35455 upgrades=20 sales=1739 ema=17.03/s joias=197
t=55:00 gold=15736 earned=40901 upgrades=20 sales=1957 ema=17.91/s joias=232
t=60:00 gold=21212 earned=46377 upgrades=20 sales=2167 ema=17.51/s joias=269
```

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| vigente | 78 | 258 | 446 | 453 | 805 | 968 | 950 | 970 | 1.076 | 1.083 | 1.089 | 1.095 |

Joias/min no jogo corrido: Joalheria → Joalheiro **5,7**, Joalheiro → Lupa **7,0**, depois de tudo **7,2**. Bancada de joias sem lingote: antes do Joalheiro **7%**, Joalheiro → Lupa **33%**, Joalheiro → 60 min **52%**. A nova renda resolve o retorno do upgrade, mas não a oferta de lingotes nem o ouro sem destino depois de 40:41. Tudo comprado agora permite teto offline de **18.000**, portanto um ralo posterior continua pendente.

#### Evidências do núcleo e prova vermelha

Todos os resultados ficam em `client/Builds/validation_phase2/`:

- `core_before.log` + `core_before.trx`: bot 60 min antes, **1/1 aprovado**.
- `core_after_measure.log` + `.trx`: bot 60 min vigente e teste novo, **2/2 aprovados**.
- `core_costs_only.log` + `.trx`: bot com novos custos e joia 60 na cópia isolada, **1/1 aprovado**; controle A/B do preço.
- `core_final.log` + `core_final.trx`: suíte das fontes reais, **37/37 aprovados, zero falhas**; inclui os guardas de 10/45/60 min, custos, offline, save e o teste novo.
- `core_red.log` + `core_red.trx`: retirado somente o pagamento 80 em `client/tools/phase2_core_proof_20261007/Core/Sim.cs`; `VitrineDeJoias_Preco60Antes80Depois_FilaEDireta_EventoEReceita` fica **vermelho**, esperado `(80, 80, 1, 1, 0)`, recebido `(60, 60, 1, 1, 0)` (ouro, receita, vendas, vendas de joia, estoque).
- `core_proof_rebuild.log`, `core_restored_verified.log` + `.trx`: cópia restaurada por backup, rebuild explícito **0 erros / 0 avisos**, teste novo **1/1 aprovado**. `core_proof_integrity.txt` registra SHA-256 igual entre a cópia restaurada e o `Sim.cs` real. A fonte real não foi mutada.

O teste novo exercita **60/80 × fila/compra direta**, preço no evento, ouro/receita, uma venda por item consumido, localização do evento, preservação da fila na compra direta, outros produtos e preço rederivado do save. A primeira tentativa de restauração (`core_restored.log/.trx`) ainda executou o binário mutado por cache incremental: copiar o backup preservou timestamp antigo. O rebuild explícito acima resolveu; só `core_restored_verified` é a evidência verde de restauração.

Comando final: `dotnet test client/tools/coretests -nologo --logger "console;verbosity=detailed" --logger "trx;LogFileName=core_final.trx" --results-directory client/Builds/validation_phase2`.

Limites calibrados em §10.6. Estado do Passo A: **núcleo testado por dotnet, 37/37**. Portão local da continuação concluído: Unity EditMode 37/37, Windows/Android sem erros, autoplay e fotos; relatório em `docs/archive/VALIDACAO_FASE2.md`. Aparelho Android e playtest humano continuam pendentes. Bot determinístico continua uma hipótese sobre pessoas: nenhuma medição daqui comprova retenção, CPI ou legibilidade mobile.

## 11. Fase 3: luxo depois dos 20 produtivos (contrato `docs/archive/FASE3_LUXO.md`, medido 2026-10-07)

**Vigente (aprovado pelo coordenador em 2026-10-07):** Fachada nobre **11.000**, Piso de oficina **14.000**, Joalheria real **18.000** (`Balance.WorkshopFacadeCost/WorkshopFloorCost/JewelryDecorCost`), total de 43.000. Os preços do contrato original (6.000 / 10.000 / 16.000) são **históricos** (§11.4): a Fachada saía cedo demais, e um retorno no teto offline pagava dois luxos de uma vez. Esta seção é a evidência 1 do contrato. Diagnóstico e A/B de logística ficam em `docs/archive/ESTUDO_LOGISTICA.md`.

Método: `Bot_90Minutos_Luxo` (`BalanceTests.cs`) joga o bot humano (0,7 s de reação, 85% de stick, 30 ticks/s) **duas vezes no mesmo passo**: com os pads de luxo e sem eles (`Pads.RemoveAll` dos pads 17–19 só na partida de controle). Sem RNG: a diferença entre as duas partidas é só o luxo. Retornos offline: harness em `%TEMP%\fs_ab\lux` contra o core real (só leitura). Varredura de preço numa cópia (`%TEMP%\fs_ab\ab\core`, custos de luxo `static`), que reproduz a partida real ao tick (as linhas 6.000/10.000/16.000 e 11.000/14.000/18.000 de §11.3 são iguais às medições reais).

### 11.1 Vigente: bot humano, 90 min, com × sem luxo (11.000 / 14.000 / 18.000)

- **20 produtivos no mesmo tick nas duas partidas** (Fole 0:53 … Vitrine de joias 40:41 = produção completa, iguais a §10.7). Primeiros 10 min idênticos (fole 0:53, 2ª bigorna 1:59, ajudante 2:31, escudos 3:39, esteira 6:31; receita 1.680): a §3 do GDD continua batendo (`Bot_Primeiros10Minutos_BatemASecao3` verde).
- **Sem luxo aos 60 min = §10.7 exatamente** (receita 46.377, saldo 21.212, 2.167 vendas, 269 joias). Com luxo aos 60 min: receita 46.234, saldo 10.069, 2.171 vendas, 266 joias. O `Bot_60Minutos_Fase2` agora vê a Fachada aos 50:45, dentro dos seus 60 min: joias 266, ouro/min 55–60 = 1.096, fome Joalheiro→60 min 53%. Os guardas de §10.6 continuam verdes.
- **Luxo:** Fachada **50:45** (+10,1 min depois da produção completa) · Piso **63:46** (+23,1; 13,0 min depois da Fachada) · Joalheria real **79:55** (+39,2; 16,1 min depois do Piso). Os intervalos crescem, e os três saem dentro dos 90 min, com 10 min de folga.
- As partidas são idênticas até o minuto 50; a 1ª diferença de receita/vendas aparece no minuto 51, logo depois da Fachada.

| janela (min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 | 60–65 | 65–70 | 70–75 | 75–80 | 80–85 | 85–90 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ouro/min com luxo | 78 | 258 | 446 | 453 | 805 | 968 | 950 | 970 | 1.076 | 1.083 | 1.059 | 1.096 | 1.087 | 1.117 | 1.134 | 1.088 | 1.132 | 1.121 |
| ouro/min sem luxo | 78 | 258 | 446 | 453 | 805 | 968 | 950 | 970 | 1.076 | 1.083 | 1.089 | 1.095 | 1.106 | 1.106 | 1.085 | 1.101 | 1.060 | 1.121 |
| saldo no fim, com luxo | 140 | 385 | 1.014 | 601 | 1.745 | 4.386 | 3.638 | 8.492 | 4.873 | 10.290 | 4.587 | 10.069 | 1.506 | 7.094 | 12.768 | 209 | 5.872 | 11.481 |
| saldo no fim, sem luxo | 140 | 385 | 1.014 | 601 | 1.745 | 4.386 | 3.638 | 8.492 | 4.873 | 10.290 | 15.736 | 21.212 | 26.746 | 32.276 | 37.704 | 43.212 | 48.514 | 54.120 |
| vendas/min com luxo | 7,8 | 17,4 | 31,0 | 31,0 | 44,2 | 44,6 | 43,2 | 43,0 | 42,4 | 43,2 | 43,6 | 42,8 | 42,2 | 41,8 | 43,4 | 43,0 | 44,4 | 43,2 |
| vendas/min sem luxo | 7,8 | 17,4 | 31,0 | 31,0 | 44,2 | 44,6 | 43,2 | 43,0 | 42,4 | 43,2 | 43,6 | 42,0 | 42,6 | 43,2 | 43,8 | 43,8 | 43,4 | 44,0 |

**Luxo não altera produção:** da produção completa aos 90 min, ouro/min **1.103,2 × 1.095,8 (+0,7%)** e vendas/min **43,06 × 43,27 (−0,5%)**. As janelas curtas oscilam (80–90: +3,3% de ouro, +0,2% de vendas) porque o desvio do bot até o pad desloca as entregas; o efeito é caótico e sem sinal. Na soma, o luxo fica com +361 de receita e −10 vendas em 90 min (492 × 484 joias). Aos 90 min: receita 79.646 × 79.285; saldo **11.481 × 54.120** = exatamente −43.000 (o preço, cobrado uma vez) + 361 de receita. Teto offline igual nas duas partidas (18.000).

### 11.2 Vigente: retorno com claim offline (Save → Load → `ApplyOffline` → bot continua)

| cenário | saldo na volta | compras imediatas (≤ 10 s) | depois |
|---|---|---|---|
| A. sai na produção completa (40:41, saldo 41, taxa 14,46/s) e volta depois de 8 h | 41 + **18.000 (teto)** | **só a Fachada** (sobram 7.359) | Piso +6,6 min, Joalheria real +23,2 min |
| A2. mesma saída, 30 min fora | 41 + 6.507 | nenhuma | Fachada +4,0, Piso +16,7, Joalheria real +33,1 min |
| B. save de 60 min **sem** luxo (jogador da versão anterior atualiza), sem claim | 21.212 | Fachada | Piso +3,7, Joalheria real +19,8 min |
| C. B + claim no teto | 39.212 | Fachada + Piso | Joalheria real +3,7 min |
| D. versão atual: sai aos 60 min (Fachada já comprada, saldo 10.069) e volta no teto | 28.069 | Piso | Joalheria real +3,9 min |

O teto (2 × 9.000) é atingido com ~83 min fora logo depois da produção completa (taxa 14,46/s × 25%) ou ~69 min fora aos 60 min (17,51/s). Com os preços vigentes, um retorno no teto partindo de saldo ~0 paga **um** luxo, como pedia PROXIMOS_EXPERIMENTOS. Saldo acumulado + teto (cenários C e D) ainda paga dois; isso é inevitável sem mexer na regra offline, que está fora de escopo.

### 11.3 Varredura de preço do luxo (cópia do core, mesmo bot, 90 min)

```
precos            | fachada        piso           joalheria real | receita 90 | volta no teto apos producao: imediatas / 3o | save 60 sem luxo: +teto / sem claim
 6000/10000/16000 | 46:02 (+5,4)   55:08 (+14,5)  69:41 (+29,0)  |   79432    | 2 / +13,0 min                              | 3 / 2   <- historico (contrato original)
10000/13000/16000 | 49:42 (+9,0)   61:55 (+21,2)  76:19 (+35,6)  |   79727    | 1 / +19,1 min                              | 3 / 1
11000/13000/16000 | 50:45 (+10,1)  62:43 (+22,0)  77:13 (+36,5)  |   79300    | 1 / +20,2 min                              | 2 / 1   <- alternativa conservadora
11000/14000/18000 | 50:45 (+10,1)  63:46 (+23,1)  79:55 (+39,2)  |   79646    | 1 / +23,2 min                              | 2 / 1   <- VIGENTE
12000/14000/16000 | 51:34 (+10,9)  64:08 (+23,5)  78:40 (+38,0)  |   79689    | 1 / +22,0 min                              | 2 / 1
12000/15000/18000 | 51:34 (+10,9)  64:59 (+24,3)  81:30 (+40,8)  |   79840    | 1 / +24,8 min                              | 2 / 1
11000/15000/20000 | 50:45 (+10,1)  64:29 (+23,8)  82:34 (+41,9)  |   80133    | 1 / +25,5 min                              | 2 / 1
13000/16000/20000 | 52:40 (+12,0)  66:58 (+26,3)  84:53 (+44,2)  |   79780    | 1 / +28,7 min                              | 2 / 1
```
Tempos entre parênteses: minutos depois da produção completa (40:41). A receita em 90 min fica entre 79,3 e 80,1 mil em todas as linhas: o preço do luxo não mexe na produção, e a variação vem do desvio do bot.

### 11.4 Histórico: contrato original 6.000 / 10.000 / 16.000 (medido antes da decisão)

- Fachada **46:02** (+5,4 min depois da produção completa), Piso **55:08** (+14,5), Joalheria real **69:41** (+29,0).
- Da produção completa aos 90 min, ouro/min 1.098,8 × 1.095,8 (+0,3%) e vendas/min −0,3%. Saldo aos 90 min 22.267 × 54.120 (−32.000 + 147). Aos 60 min com luxo: receita 46.584, saldo 5.419.
- Offline: um retorno no teto logo depois da produção completa comprava **Fachada + Piso em 7 s**. O save de 60 min sem luxo comprava os dois na hora, e os três com o claim no teto.
- Motivo da troca: a Fachada saía antes de ~10 min depois da produção completa (critério do contrato), e o teto pagava dois dos três momentos de luxo de uma vez. A decisão é do coordenador, em 2026-10-07.
- O bot compra porque tem ouro, e isso não mostra interesse por decoração. O gate humano de PROXIMOS_EXPERIMENTOS §1 continua pendente.

### 11.5 Portão de regressão (`Bot_90Minutos_Luxo`, folgado: medido + ~30%)

- 20 produtivos comprados e **no mesmo tick** com e sem luxo.
- Fachada entre a produção completa e **66:00** (50:45 × 1,3); Piso depois da Fachada e até **83:00** (63:46 × 1,3); Joalheria real depois do Piso e até **90:00** (79:55 × 1,3 passaria de 90; vale o limite do contrato).
- Partidas idênticas (receita e vendas) em todo minuto até a Fachada.
- Da produção completa aos 90 min, ouro/min e vendas/min com/sem luxo dentro de **±3%** (medido +0,7% / −0,5%).
- Saldo final = saldo sem luxo + Δreceita − 43.000 (cobra uma vez, sem devolução); 23 × 20 compras; teto offline igual.
- Histórico com 6.000/10.000/16.000: limites 60:00 / 71:50 / 90:00.

**Prova vermelha** (cópia isolada `%TEMP%\fs_ab\red`, só o teste novo, refeita com os preços vigentes; logs `v2_*.log` em `%TEMP%\fs_ab\logs`):

| mutação no `Sim.cs` da cópia | resultado vigente (histórico 6/10/16) |
|---|---|
| Fachada dá +10% no preço de todos os itens (`PriceOf`) | **vermelho**: ouro/min com/sem = 1,073 (1,102); limite 0,97–1,03 |
| Fachada acelera a fornalha ×0,8 (`Recompute`) | **vermelho**: 1,056 (1,062) |
| luxo entra no teto offline (`CheapestLockedCost` até `Upgrades.Count`) | **vermelho**: teto 36.000 × 22.000 (32.000 × 12.000) |
| cópia restaurada (Sim/Defs/Bot iguais byte a byte ao real), rebuild sem cache | verde 1/1 |

Como repetir: `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` (o teste imprime compras, janelas de 5 min e a comparação). Suíte final: **43/43**.

## 12. Marca de luxo por upgrade (Leva 9, 2026-10-07): nenhum número muda

**O que mudou no core:** o luxo deixou de ser "ID ≥ 20" (`Upgrades.ProductionCount`) e passou a ser uma marca por upgrade, `UpgradeDef.Luxury`, consultada por `Upgrades.IsLuxury(u)`. Hoje só a Fachada nobre, o Piso de oficina e a Joalheria real têm a marca. Ela é lida em:
- `Pad.Current`: o pad de luxo só aparece com a produção completa;
- `Sim.Buy`: a compra direta de luxo exige a produção completa e o pré-requisito;
- `Sim.ProductionComplete`: todos os upgrades **sem** a marca comprados;
- `Sim.CheapestLockedCost`: o teto offline só considera produtivos.

`ProductionCount` (20) continua sendo a **contagem** de produtivos. Um teste garante que ela é igual ao número de definições sem a marca. Os IDs não precisam mais ser contíguos, então um produtivo novo pode ser anexado depois dos luxos (ID 23+) sem quebrar save antigo.

**Regra do teto offline com tudo comprado:** passa de "2× o último produtivo" para "**2× o produtivo mais caro**". O valor é o mesmo (Vitrine de joias 9.000 → **18.000**), mas não cai se um produtivo mais barato for anexado ao fim do enum. Antes de a produção ficar completa, a regra continua 2× o **menor** produtivo travado (§10.5).

**Evidência de comportamento idêntico (`%TEMP%\fs_ab9\logs\`):**
- `ident_old.txt` × `ident_new.txt`: o mesmo programa compilado contra o core da v0.3.0 e contra o novo. As DLLs são diferentes e só a nova contém `IsLuxury`. A saída tem 416 linhas, **idênticas byte a byte** (SHA c1c68b56…5d05), e cobre:
  - bot humano em 90 min, com e sem pads de luxo, Dt 1/30 e 1/60;
  - por minuto: saldo, receita, vendas, compras, `ProductionComplete`, `CheapestLockedCost`, teto offline, pad mais barato visível e o upgrade à venda em cada um dos 20 pads;
  - a cada 15 min: Save → Load → claim de 8 h e o save recarregado;
  - a matriz de compra direta: cada upgrade sozinho e "todos menos u".
- Suíte `dotnet test client/tools/coretests`: **44/44**, os 43 de antes inalterados mais o teste novo. Saves antigos de 15, 17 e 20 flags continuam verdes.

**Teste novo** `CoreTests.Marca_ProdutivoAnexadoDepoisDosLuxos_ETratadoComoProdutivo`: troca, só durante o teste, a definição do ID 22 por uma produtiva que requer o Joalheiro, e restaura no `finally`. O core tem de tratá-la como produtiva:
- o pad fica visível antes da produção completa;
- a compra direta é aceita;
- o upgrade entra em `ProductionComplete` e no teto offline como menor travado (18.000 × 9.000);
- o luxo passa a esperá-lo.

**Prova vermelha** (cópia isolada `%TEMP%\fs_ab9\red`, rebuild sem cache a cada mutação; logs `etapa1_red_M*.txt`):

| mutação no `Sim.cs` da cópia | resultado |
|---|---|
| M1: `Sim.cs` da v0.3.0 (regra por ID em tudo) | **vermelho**: "pad visível antes da produção completa", esperado 22, recebido −1 |
| M2: só `ProductionComplete` volta a `i < ProductionCount` | **vermelho**: "falta o produtivo anexado", esperado False, recebido True |
| M3: só `CheapestLockedCost` volta a `i < ProductionCount` | **vermelho**: "entra no teto offline como menor travado", esperado 18.000, recebido 9.000 |
| restaurado pela cópia de backup (SHA b0332cde… igual ao real) | verde **44/44** |

**2º ajudante de minério:** o A/B isolado não passou nos 4 critérios. Nada foi implementado, e não há §12 de preço nem `FASE4_MINERIO.md`. Resultado e recomendação em [ESTUDO_LOGISTICA.md §5](archive/ESTUDO_LOGISTICA.md).


## 13. Fase 5, Etapa A: física, duas bocas, parede lateral e paciência por item (contrato `docs/archive/FASE5_FISICA_PACIENCIA.md`, medido 2026-10-07)

**Medição da Etapa A sozinha**, numa cópia do core final sem o par da fase 4 (`%TEMP%\fs_l10\stageA2`, gerada por `make_stage_a.py`: sem Mineiro/Joalheiro 2 e com o pad do Martelo veloz em (0,5; 9,5)). O jogo vigente é A + B (§14). Origem: playtest do Vinicius no POCO F4. Ele achou a paciência curta, viu o personagem atravessar ou parar em cima das estações e pediu para abastecer sem retirar. Os números de §10.7/§11 passam a ser **históricos** (antes da física). Nenhum preço mudou nesta etapa.

### 13.1 O que mudou no core
- **Paciência por item:** `Balance.PatienceFor(item) = PatienceBase + PatiencePerSecond × (FurnaceTime0 × lingotes + HammerTime0 × multiplicador da bancada)`, com **base 30 s e k = 3** (varredura em §13.2).
  - Valores: espada **57 s**, escudo **76,5 s**, ferramenta **57 s**, joia **84 s**. Antes eram 25 s no balcão e 40 s para os nobres.
  - `Client.MaxPatience` recebe o valor do item. `Balance.Patience`/`JewelPatience` saíram.
- **Corpos sólidos:** toda estação é uma caixa `Balance.StationHalf` = ±(0,65; 0,40) m em `Station.Pos` (`Station.Body`).
  - Estação travada não é sólida, porque o pad de compra fica no lugar dela. A loja de joias é sólida mesmo fechada, porque a view desenha a fachada.
  - Decoração e parede ficam em `Balance.Obstacles`, com a mesma geometria da view:
    - carroça (14,2; 3,0) ±(0,65; 0,40);
    - **parede direita da oficina** em x 9,0–9,6 (`Balance.SideWallX0/X1`), sólida em y 0–5,6, 7,4–11,6 e 13,4–14. As aberturas `Balance.SideWallOpenings` = {5,6, 7,4, 11,6, 13,4} são a **porta lateral** (na altura da joalheria) e o **arco**. As pontas dos segmentos fazem de batente, e o arco não tem mais poste solto: os de (8,4 e 9,6; 12,6) ficavam dentro da passagem;
    - 4 pedestais da Joalheria real ±(0,2; 0,1), só com `JewelryDecor`;
    - 2 postes da Fachada nobre (0,15 e 8,85; 14,1) ±(0,16; 0,06), só com `WorkshopFacade`.
  - `Sim.Solids` guarda os corpos ativos e é refeito no `Recompute`.
- **Personagens:** círculo de raio `Balance.CharRadius` = 0,25 m.
  - `Sim.Collide` empurra para fora pela normal da caixa. O resto do passo vira deslize na face e contorno da quina; centro dentro sai pelo lado mais perto, e no empate por baixo. Vale todo tick para o jogador, inclusive parado (estação comprada embaixo dele o empurra), e para os ajudantes.
  - Ajudantes e bot usam `Sim.Via` e `Sim.Steer`. `Via` cruza a parede pelo meio da abertura de menor caminho (regra de 2 aberturas, sem pathfinding). `Steer` contorna o corpo encostado pelas quinas, comparando o caminho quina a quina até o alvo ficar visível.
  - O jogador humano não tem desvio automático: empurrar de frente para.
  - Pads e baús continuam pisáveis.
- **Duas bocas (FASE5 §2b):**
  - Cada estação que produz tem `InAt` (ENTRADA, só deposita) e `OutAt` (SAÍDA, só recolhe), em lados opostos, no ponto em que o personagem encosta no meio da face.
  - Zona = até `Balance.MouthRadius` = 0,4 m do ponto. Depósito e balcões têm zona única (`InAt = OutAt`).
  - Ajudantes buscando vão à SAÍDA da fonte; entregando, à ENTRADA do destino (`Balance.WorkerReach` = 0,3 m). O bot faz o mesmo.
  - A esteira continua ligando a saída da Fornalha à entrada da Bigorna, sem zona. `Sim.StationAt(p, out bool outZone)` substitui o raio de 0,9 m do centro.
- **Pad da Lupa** (10,2; 6,5) → **(14,2; 6,8)**, na coluna de pads da rua. Na boca da porta lateral, o bot pagou 81 de ouro nele sem querer (harness de §14.1). O índice do pad não muda, então o save fica igual.

| estação | ENTRADA | SAÍDA | motivo do lado |
|---|---|---|---|
| Depósito (1,5; 1,5) | zona única acima (1,5; 2,15) | — | lado das fornalhas |
| Fornalha (1,5; 5,5) | abaixo (1,5; 4,85) | acima (1,5; 6,15) | à esquerda a zona encostava no pad do Fole (0,5; 6,5); em pé o fluxo sobe até a esteira |
| Fornalha 2 (4,5; 5,5) | esquerda (3,6; 5,5) | direita (5,4; 5,5) | padrão (pilhas da view) |
| Bigorna (1,5; 9,5) | abaixo (1,5; 8,85) | acima (1,5; 10,15) | à esquerda caía no pad antigo do Martelo veloz (0,5; 9,5); a esteira chega por baixo |
| Bigorna 2 (4,5; 9,5) | esquerda (3,6; 9,5) | direita (5,4; 9,5) | padrão |
| Escudos (7,5; 9,5) | esquerda (6,6; 9,5) | direita (8,4; 9,5) | padrão |
| Ferramentas (7,5; 5,5) | esquerda (6,6; 5,5) | direita (8,4; 5,5) | padrão |
| Balcão (4,5; 13) | zona única abaixo (4,5; 12,35) | — | a fila fica acima (y 14) |
| Joalheria (12; 6,5) | abaixo (12; 5,85) | acima (12; 7,15) | à esquerda caía no pad antigo da Lupa (10,2; 6,5); a saída aponta para a loja |
| Loja de joias (12; 11,5) | zona única abaixo (12; 10,85) | — | a fila fica acima (y 12,5) |

Testes que conferem a geometria:
- `Bocas_Tabela_LadosOpostos_ForaDeCorposEPads`: boca a 0,25 m da face, fora de qualquer corpo, a ≥ 1,0 m (0,4 + 0,6) de todo pad que não seja o da própria estação, e sem zona sobreposta à de outra estação. A folga mais curta é saída da Fornalha × pad do Fole: 1,06 m.
- `Fila_VagasEPadsEBaus_ForaDosCorpos`: vagas de clientes, pads, baús e `HireSpot` fora dos corpos. A vaga de compra direta do balcão (9,6; 14,0) fica na quina da parede, mas não dentro dela.

### 13.2 Varredura de paciência (cópia `%TEMP%\fs_l10\sweep\coreA2` com `PatienceBase/PatiencePerSecond` estáticos, core da Etapa A, bot humano)
Legenda: "desist." = cansou na fila (`ClientsLost`); "sem vaga" = fila cheia, nem entrou (`ClientsTurnedAway`); "cheia" = % do tempo com a fila do balcão no teto, que mede a "parede". A linha "antes" é o core da v0.3 (`orig.txt`); as outras são `a2_<base>_<k>.txt`.

```
paciencia (espada)          | 10 min: desist. sem vaga cheia vendas ganho esteira | 60 min: desist. sem vaga cheia  ganho  producao
antes: 25/40 s, sem fisica  |           21      41     35%   126   1680   6:31   |           305    2661   68%   46234   40:41
base 25 k 0 (25 s)          |           26      43     39%   110   1475   7:06   |           325    2574   67%   51512   39:43   <- so a fisica
base 20 k 1 (29 s)          |           16      53     48%   111   1500   7:07   |           151    2772   71%   52883   38:57
base 25 k 1 (34 s)          |           12      57     50%   108   1485   7:17   |            82    2840   72%   51351   39:23
base 20 k 2 (38 s)          |            5      65     57%   108   1470   7:08   |            43    2887   76%   53396   39:10
base 25 k 2 (43 s)          |            3      66     56%   108   1485   7:17   |            23    2880   73%   52830   39:06
base 30 k 2 (48 s)          |            4      66     54%   109   1465   7:12   |            10    2924   75%   52637   39:11
base 30 k 3 (57 s)          |            1      68     57%   111   1470   7:12   |             5    2914   74%   53219   39:07   <- VIGENTE
base 30 k 4 (66 s)          |            0      69     57%   111   1470   7:12   |             0    2907   74%   52402   39:02
```
Leitura:
- **A desistência cai 96% no início** (26 → 1 em 10 min, contra só a física com 25 s) e quase some no fim (325 → 5 em 60 min).
- **A "parede" não depende do valor escolhido.** Toda paciência que corta bem a desistência põe a fila cheia em 54–57% dos primeiros 10 min (39% com 25 s): quem não desiste ocupa a vaga, e o cliente seguinte vira "sem vaga". O total de não atendidos fica em 66–69 em todas as linhas, porque a oferta é o limite. A paciência só troca "cansou na fila" por "nem entrou".
- **Escolha: base 30 / k 3**, o ponto de partida do contrato. É o menor par redondo com desistência ≈ 0 em 10 e 60 min. Se o playtest achar a fila cheia demais, a alavanca é a capacidade da fila (`QueueCap`), não a paciência.
- A primeira varredura, feita antes da parede e da correção do desvio (`%TEMP%\fs_l10\sweep\new_*.txt`), deu a mesma leitura: 27 → 1 desistências e fila cheia de 38% → 59%.

### 13.3 Primeiros 10 min (bot humano; §3 do GDD)
| marco | antes (§10.3) | Etapa A | GDD | fração do GDD |
|---|---|---|---|---|
| 1ª venda | 0:19 | **0:21** | < 1:30 | 23% |
| Fole | 0:53 | **1:04** | 2:30 | 43% |
| 2ª bigorna | 1:59 | **2:14** | 3:30 | 64% |
| Ajudante | 2:31 | **2:38** | 4:30 | 59% |
| Escudos | 3:39 | **3:49** | 5:30 | 69% |
| Esteira | 6:31 | **7:12** | 8:30 | 85% |
| comprados / receita em 10 min | 9 / 1.680 | **9 / 1.470** | — | — |
| desistiram / sem vaga / fila máx. | 21 / 41 / 4 | **1 / 68 / 4** | — | — |
| andar sem decisão | 30% | **33%** | < 50% | — |

- **Custo da física:** contornar a estação entre a entrada e a saída custa caminhada no começo (fole +11 s, esteira +41 s, receita −13%). A §3 continua batendo com folga (`Bot_Primeiros10Minutos_BatemASecao3` verde).
- **Bot ideal:** fole 0:52, 2ª bigorna 2:00, esteira 7:05. Offline depois de 10 min: 1.060 de ouro (2 × Ferramentas 530, igual a §10.3).
- **Achado:** o bot humano deixa **15 de ouro** no pad antigo do Martelo veloz (0,5; 9,5), que fica no corredor entre a Bigorna e a parede. Na 1ª medição, antes da correção do desvio, eram 85. A troca de lugar do pad é da Etapa B (§14.3).

### 13.4 Bot humano, 45, 60 e 90 min (Etapa A sozinha)
- **Compras:** Fole 1:04 · 2ª bigorna 2:14 · Ajudante 2:38 · Escudos 3:49 · Mochila 5:05 · Ajudante 2 6:45 · Esteira 7:12 · Fole duplo 8:13 · Botas 9:41 · Ferramentas 11:00 · Vitrine 12:31 · **Corredor 13:02** · 2ª fornalha 13:33 · Ajudante 3 16:06 · Ajudantes ágeis 19:20 · **Joalheria 21:57** · Martelo veloz 24:23 · **Joalheiro 26:00** · **Lupa 31:02** · **Vitrine de joias 39:16 = produção completa** (antes 40:41).
- **Luxo:** Fachada **47:43 (+8,4 min)** · Piso **58:24 (+19,1)** · Joalheria real **72:00 (+32,7)**. Antes: +10,1 / +23,1 / +39,2.
  - Com e sem luxo, de 40 a 90 min: ouro/min +2,1%, vendas/min −0,9%. Não há bônus; o desvio do bot até o pad e os pedestais sólidos explicam a diferença.
  - Saldo aos 90 min: 24.620 com luxo × 66.221 sem.
- **Baús:** abertos aos 2:31 / 7:08 / 24:33 / 12:59. **45 min:** 9,2 joias/min; ouro/min 493 → 970 com a Joalheria.

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| antes (§10.7) | 78 | 258 | 446 | 453 | 805 | 968 | 950 | 970 | 1.076 | 1.083 | 1.089 | 1.095 |
| **Etapa A** | 83 | 211 | 424 | 468 | 751 | 1.078 | 1.098 | 1.135 | 1.298 | 1.291 | 1.324 | 1.336 |

- **Joalheria:**
  - joias/min: Joalheria → Joalheiro 5,8 · Joalheiro → Lupa 8,8 · depois da produção 10,3 (antes 5,7 / 7,0 / 7,2).
  - Bancada de joias sem lingote, Joalheiro → 60 min: **33%** (antes 52%).
  - Nobres que cansaram em 45 min: 0.
- **Efeito da parede** (A/B na mesma cópia, sem os 3 segmentos e sem `Via`; log `stageA2_nowall.log`):
  - fome 31% → **33%** com parede;
  - joias em 60 min 384 → 368 (−4%);
  - produção completa 39:06 → 39:16;
  - ouro/min 55–60: 1.287 × 1.336, dentro do ruído do desvio.
  - A porta lateral segura o custo: a parede inteira, sem porta, não foi medida.
- **O fim de jogo rende +20%** (1.291–1.336 contra 1.095), e só a física já faz isso: a linha "base 25 k 0" da §13.2 dá 51,5 mil em 60 min contra 46,2 mil. A causa não foi isolada. **HIPÓTESE:** com entrada e saída separadas, abastecer não espera o recolhimento.
- A fome da joalheria que motivou o par Mineiro/Joalheiro 2 era de 56% na janela 41–60 (`ESTUDO_LOGISTICA.md`) e **mudou** para 33–36%. A Etapa B remede por cima desta (§14.1).

### 13.5 Testes novos e prova vermelha
- **Novos em `CoreTests`:**
  - Etapa A (8): `Paciencia_PorItem_ProporcionalAProducaoInicial`, `Fisica_EmpurrandoContraQualquerCorpo_NuncaEntra`, `Fisica_DeslizaNaQuina_EAjudanteContornaEstacaoNoCaminho`, `Bocas_EntradaSoDeposita_SaidaSoRecolhe`, `Bocas_Tabela_LadosOpostos_ForaDeCorposEPads`, `Fila_VagasEPadsEBaus_ForaDosCorpos`, `Ajudantes_CompletamCiclos_10Min_SemTravar`, `Save_JogadorDentroDeUmCorpo_SaiPelaBorda`.
  - Parede: `Parede_SoPassaPelaPortaEPeloArco_AjudanteUsaAAbertura`.
- **Testes antigos ajustados ao contrato novo:**
  - a cadeia, a mochila e a loja de joias interagem nas bocas (`InAt`/`OutAt`), não mais no centro;
  - o teste do Corredor anda pela faixa da porta lateral (y 6,5);
  - os testes de paciência leem `PatienceFor`;
  - `Luxo_NaoMudaProducao…` iguala a geometria das duas partidas antes de comparar ao tick, porque pedestais e postes agora são corpos.
- **Os portões dos testes de 45/60/90 min** foram remedidos por cima de A + B, em §14.4. A Etapa A sozinha passou nos portões que valiam para ela (6/6 em `stageA2_final.log`).
- **Correção achada na Etapa B e aplicada aqui:**
  - A 1ª versão do `Steer` escolhia a quina só pela 1ª perna. Com Dt 1/60, o bot ficava preso na quina da Fornalha: 0 vendas em 10 min.
  - A versão final soma o caminho quina a quina, e com isso o bot joga normalmente com Dt 1/20, 1/30, 1/45 e 1/60.
  - A prova vermelha está junto com a da Etapa B (§14.5).

## 14. Fase 4 por cima da Etapa A: Mineiro + Joalheiro 2, Martelo veloz e `-buy` (contrato `docs/archive/FASE4_MINERIO.md`, medido 2026-10-07)

**Vigente.** Mineiro (ID 23, ← Joalheiro) = 2º ajudante papel 0. Joalheiro 2 (ID 24, ← Mineiro) = 2º ajudante papel 3. Os dois são produtivos (`Luxury = false`), anexados ao fim do enum, e os pads 20–21 ficam no fim da lista. Preços em `Balance.MinerCost` / `Balance.Jeweler2Cost`. O luxo agora espera **22** produtivos.

### 14.1 A/B da regra de retorno com física (harness `%TEMP%\fs_l10\sweepb`, cópia do core final com preços estáticos; sem luxo; janela 60–90 min; logs `final/L_<modo>_<dt>.txt`)
| modo | base | ouro/min 60–90 | Δ | joias/min | fome da joalheria | outras vendas/min |
|---|---|---|---|---|---|---|
| controle (sem o par) | Dt 1/30 · 1/60 | 1.326 · 1.355 | — | 10,67 · 11,17 | 36% · 33% | 32,93 · 32,10 |
| + Mineiro | | 1.379 · 1.413 | **+53 · +58** | 11,27 · 11,57 | 32% · 31% | 33,17 · 34,27 |
| + Mineiro + Joalheiro 2 | | 1.622 · 1.646 | **+296 · +291** (Joalheiro 2 sobre o Mineiro: +243 · +233) | 14,13 · 14,00 | **15% · 16%** | 33,70 · 36,07 |

- **O par continua passando nos 4 critérios** de `ESTUDO_LOGISTICA.md` §5:
  - fome ≤ 35%: 15–16%;
  - joias/min +25 a +32%;
  - ouro/min +21 a +22%;
  - outras vendas sem queda: +2 a +12%.
- **O Mineiro sozinho quase não rende com a física:** +53 a +58 ouro/min, contra +300 a +365 no estudo de antes. O Joalheiro 2 é que carrega o ganho.
- **Regra de retorno remedida** (`custo ≈ Δ × 8…12`):
  - Mineiro **420–700**;
  - Joalheiro 2 **1.860–2.920**;
  - par 2.330–3.550.
  - A faixa do contrato (2.400–4.400 / 1.750–3.100) foi calculada antes da física.

### 14.2 Varredura de preço (bot humano, 90 min, com luxo 11/14/18 mil; `final/grid_<mineiro>_<joalheiro2>.txt`)
```
mineiro j2   | mineiro joalh.2 lupa  vitrine = producao | fachada (+min) | ouro/min 41-60 60-90 | fome 60-90 | receita 90
  500  2400  |  26:26   28:34  33:19  40:01             | 46:55 (+6,9)   | 1621  1623            | 16%        | 109080   <- regra de retorno (Mineiro)
 1000  2400  |  26:53   29:08  33:46  40:29             | 47:08 (+6,6)   | 1632  1646            | 13%        | 109810
 1500  2400  |  27:22   29:40  34:16  41:12             | 47:59 (+6,8)   | 1576  1654            | 13%        | 108490
 2400  1750  |  28:09   29:47  34:33  41:14             | 48:05 (+6,8)   | 1614  1616            | 15%        | 108227
 2400  2400  |  28:09   30:21  35:03  41:45             | 48:40 (+6,9)   | 1575  1632            | 14%        | 107902
 2400  3100  |  28:09   30:58  35:47  42:35             | 49:17 (+6,7)   | 1587  1630            | 14%        | 107864
 3000  1750  |  28:41   30:27  35:06  41:49             | 48:30 (+6,7)   | 1603  1607            | 16%        | 107608
 3000  2400  |  28:41   30:57  35:41  42:24             | 49:08 (+6,7)   | 1625  1638            | 14%        | 108807   <- VIGENTE
 3000  3100  |  28:41   31:36  36:20  43:08             | 49:56 (+6,8)   | 1564  1645            | 13%        | 107642
 3600  1750  |  29:24   31:01  35:48  42:36             | 49:21 (+6,7)   | 1619  1642            | 14%        | 108442
 3600  2400  |  29:24   31:37  36:20  43:08             | 50:07 (+7,0)   | 1588  1663            | 12%        | 108503
 3600  3100  |  29:24   32:20  37:01  43:39             | 50:22 (+6,7)   | 1583  1619            | 16%        | 107077
 4400  1750  |  30:11   31:47  36:31  43:05             | 50:02 (+7,0)   | 1573  1628            | 15%        | 107272
 4400  2400  |  30:11   32:22  37:05  43:49             | 50:31 (+6,7)   | 1593  1606            | 17%        | 106694
 4400  3100  |  30:11   33:05  37:46  44:24             | 51:13 (+6,8)   | 1546  1640            | 14%        | 107150
```
- A receita em 90 min quase não muda (106,7–109,8 mil): o preço só desloca o tempo.
- **Pela faixa do contrato, a janela 42–48 min só é atingida com o par custando ≥ ~5.400.** Com a faixa remedida (§14.1), a produção acaba em ~40:00, fora da janela.
- **Escolha (APLICADA): Mineiro 3.000 / Joalheiro 2 2.400.**
  - Produção completa **42:24** (Dt 1/30) e **42:16** (Dt 1/60), dentro de 42–48 nas duas bases.
  - É o par mais barato dentro das faixas do contrato que fecha a janela, com o Joalheiro 2 dentro da própria regra remedida (≈ 10 min).
  - O Mineiro fica fora da regra remedida: sozinho, ele se paga em 52–57 min. Ele vale como porta do Joalheiro 2, e o par se paga em ~18 min. A outra opção está em PROPOSTAS (`FASE4_MINERIO.md`).
- **Primeiros 10 min:** o par só aparece depois do Joalheiro (~26 min) e não os afeta. O que mudou neles foi o pad do Martelo veloz (§14.3).

### 14.3 Pads: Martelo veloz e o par
- **Martelo veloz** (0,5; 9,5) → **(8,4; 3,4)**, com o mesmo índice 10 (save igual). Medição de pagamento acidental (ouro que o bot pôs num pad que não mirava), 90 min, Dt 1/30 · 1/60:

| posição do Martelo veloz | acidental | 2ª bigorna / esteira | receita 10 min |
|---|---|---|---|
| (0,5; 9,5) antiga | **15 · 43** (1:17 e 8:15) | 2:14 / 7:12 | 1.470 |
| (8,4; 7,5) sugestão do contrato | 0 · 0 | 1:57 / 7:03 | 1.485 |
| **(8,4; 3,4) aplicada** | 0 · 0 | 1:57 / 7:03 | 1.485 |

- **Por que não a sugestão:** com as bocas da Etapa A, a sugestão fica exatamente na linha entre as SAÍDAS de Ferramentas (8,4; 5,5) e de Escudos (8,4; 9,5). O teste `Pad_MarteloVeloz_LongeDaBigornaDasBocasEDasLinhas` exige que o pad fique fora de toda linha boca → boca e a ≥ 1,5 m de qualquer estação (o rótulo do pad ficava a 1,0 m do da Bigorna).
- **Os primeiros 10 min mudam, e a mudança é pela saída do pad do corredor da Bigorna:** o bot deixa de pagar sem querer e compra a 2ª bigorna 17 s antes. Fora isso nada muda: fole 1:04, ajudante 2:38, escudos 3:45. A §3 continua batendo.
- **Mineiro (0,5; 3,5)**, na parede esquerda entre o Depósito e a Fornalha. **Joalheiro 2 (12; 3,5)**, na rua lateral embaixo da Joalheria, longe das bocas, da fila, do baú, dos pedestais e da porta. Os dois ficam a ≥ 1,68 m de toda boca. Pagamento acidental medido: 0.

### 14.4 Vigente: bot humano 10/45/60/90 min (`BalanceTests`, `b_final.log`)
- **10 min:** 1ª venda 0:21 · fole 1:04 · 2ª bigorna 1:57 · ajudante 2:38 · escudos 3:45 · esteira 7:03 · 9 upgrades · receita 1.485 · desistiram 0 / sem vaga 71 · andar sem decisão 34%. Offline depois de 10 min: 1.060.
- **Compras:** … Joalheria 21:56 · Martelo veloz 24:27 · **Joalheiro 26:06 · Mineiro 28:41 · Joalheiro 2 30:57** · Lupa 35:41 · **Vitrine de joias 42:24 = produção completa (22 produtivos)**. Baús: 2:32 / 6:59 / 24:52 / 13:03.
- **Joalheria:**
  - joias/min: Joalheria → Joalheiro 5,3 · Joalheiro → Lupa 9,5 · depois da produção **14,3**. Na Etapa A eram 10,3, e antes da física 7,2.
  - Bancada de joias sem lingote, Joalheiro → 60 min: **12%** (Etapa A 33%, antes da física 52%).
  - 45 min: 10,5 joias/min; ouro/min 496 → 935 com a Joalheria.

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Etapa A (§13.4) | 83 | 211 | 424 | 468 | 751 | 1.078 | 1.098 | 1.135 | 1.298 | 1.291 | 1.324 | 1.336 |
| **A + B (vigente)** | 78 | 219 | 442 | 463 | 729 | 1.093 | 1.155 | 1.298 | 1.492 | 1.655 | 1.621 | 1.682 |

- **Luxo (11/14/18 mil):**
  - Fachada **49:08 (+6,7 min)** · Piso 57:37 (+15,2) · Joalheria real 68:41 (+26,3).
  - Com e sem luxo, de 43 a 90 min: ouro/min +1,4%, vendas/min +0,6% (sem bônus).
  - Receita aos 90 min: 108.807 com luxo × 107.763 sem. Saldo: 35.242 × 77.198.
  - **A Fachada sai menos de 8 min depois da produção completa.** Pela regra do contrato, novos preços de luxo são PROPOSTOS em §14.6 e **não foram aplicados**.
- **Teto offline:**
  - antes do par, 2 × Joalheiro 2 = 4.800 (o menor travado);
  - com a produção completa, 18.000 (2 × Vitrine de joias);
  - um retorno no teto logo depois da produção completa compra **um** luxo.
- **Portões remedidos** (medido × 1,3, arredondado para cima em 10 s; pisos ~70%):
  - `Bot_45Minutos…`: Corredor ≤ 17:10, Joalheria ≤ 28:40, joias/min > 7,3.
  - `Bot_60Minutos_Fase2`:
    - tempos: Joalheiro ≤ 34:00, **Mineiro ≤ 37:20, Joalheiro 2 ≤ 40:20**, Lupa ≤ 46:30, Vitrine de joias ≤ 55:10;
    - baús: ≤ 3:20 / 9:10 / 32:20 / 17:00;
    - joias/min > 10,0;
    - **bancada de joias sem lingote ≤ 35%** (critério da fase 4, medido 12%);
    - ouro/min 55–60 > 1.177.
  - `Bot_90Minutos_Luxo`: Fachada ≤ 64:00, Piso ≤ 75:00, Joalheria real ≤ 90:00, com/sem luxo dentro de ±3%. A baseline sem luxo agora remove os pads por `IsLuxury` (a regra antiga "ID ≥ ProductionCount" tirava o par junto).

### 14.5 Testes e prova vermelha (Etapas A + B)
- **Novos da Etapa B em `CoreTests` (6):** `Requisitos_Joalheiro_Mineiro_Joalheiro2_ProdutivosAnexados`, `Luxo_EsperaOs22Produtivos_InclusiveMineiroEJoalheiro2`, `Mineiro_Papel0Extra_SoCarregaMinerioAsFornalhas`, `Joalheiro2_Papel3Extra_AbasteceAJoalheriaELevaJoias`, `Save_Antigo23Flags_CarregaComOParNovoTravadoEDevolveParcialDoLuxo`, `Pad_MarteloVeloz_LongeDaBigornaDasBocasEDasLinhas`.
- **Ajustados para 25 upgrades / 22 produtivos:** curva de custo, luxo, offline, saves de 15/17/20 flags (`up=` com 25 dígitos), contagem de ajudantes e de pads.
- **Suíte: 59/59** (44 de antes + 15 novos). `viewcheck`: 0 erros, 0 avisos.
- **Achado de save corrigido:** um save da v0.3 com pagamento parcial na Joalheria real perdia o ouro ao carregar, porque o par novo esconde o pad de luxo e o `Load` zerava o parcial. Agora o parcial de pad escondido volta para o ouro (`Load`, com teto em `int.MaxValue`).

Prova vermelha: cópia isolada `%TEMP%\fs_l10\red`, só os 15 novos, rebuild sem cache a cada mutação, logs `logs/<id>.log`, resumo em `logs/summary_final.txt`. Cada um dos 15 ficou vermelho em pelo menos uma mutação.

| mutação na cópia | testes vermelhos |
|---|---|
| A1 paciência antiga (25/40 s) | Paciencia_PorItem |
| A2 `Collide` não empurra | EmpurrandoContraQualquerCorpo, DeslizaNaQuina, Ajudantes_CompletamCiclos, Parede, Save_JogadorDentro |
| A3 `Steer` em linha reta | DeslizaNaQuina, Ajudantes_CompletamCiclos, Joalheiro2 |
| A4 boca única (entrada recolhe) | Bocas_EntradaSoDeposita |
| A5 saída no ponto da entrada | Bocas_Tabela, Bocas_EntradaSoDeposita, Ajudantes_CompletamCiclos, Mineiro, Joalheiro2 |
| A6 vaga do balcão dentro do corpo | Fila_VagasEPadsEBaus |
| A7 `Load` sem `Collide` | Save_JogadorDentro |
| A8 ajudante mira o centro da estação | Ajudantes_CompletamCiclos, Mineiro, Joalheiro2 |
| W1 parede sem o 1º segmento | Parede |
| W2 `Via` sempre direto | Parede (o ajudante prende na parede contra a borda do mundo) |
| B1 Mineiro com papel 1 | Mineiro, Joalheiro2 |
| B2 Joalheiro 2 com papel 2 | Joalheiro2, Ajudantes_CompletamCiclos |
| B3 Joalheiro 2 exige o Joalheiro | Requisitos, Save_Antigo23Flags |
| B4 Joalheiro 2 marcado como luxo | Luxo_EsperaOs22, Requisitos, Joalheiro2 |
| B5 `Load` zera o parcial escondido | Save_Antigo23Flags |
| B6 Martelo veloz no lugar antigo | Pad_MarteloVeloz |
| B7 contratação antiga (um por papel) | Mineiro, Joalheiro2 |
| restaurado pela cópia de backup (SHA = core real) | verde **59/59** |

### 14.6 PROPOSTO, não aplicado
- **Luxo** (a Fachada sai +6,7 min depois da produção, abaixo dos 8 min). Mesmo bot, 90 min, com Mineiro 3.000 / Joalheiro 2 2.400; arquivos `final/lux_*` e `final/off_*`:

```
precos (F / P / J)  | fachada        piso           joalheria real | volta no teto apos a producao: luxos em 10 s
11000/14000/18000   | 49:08 (+6,7)   57:37 (+15,2)  68:41 (+26,3)  | 1   <- vigente
12000/15000/18000   | 49:42 (+7,3)   59:01 (+16,6)  70:12 (+27,8)  | 1
13000/16000/20000   | 50:24 (+8,0)   60:20 (+17,9)  72:45 (+30,4)  | 1
14000/18000/22000   | 50:55 (+8,5)   62:27 (+20,0)  75:55 (+33,5)  | 1   <- alternativa conservadora
15000/18000/22000   | 51:35 (+9,2)   62:33 (+20,1)  76:09 (+33,8)  | 1
16000/20000/24000   | 52:14 (+9,8)   64:30 (+22,1)  79:29 (+37,1)  | 1   <- PROPOSTA
```
  - **Proposta: 16.000 / 20.000 / 24.000.** Ela repete a forma aprovada em §11: Fachada ~+10 min, intervalos crescentes de 9,8 / 12,3 / 15,0 min e Joalheria real aos ~80 min, dentro dos 90. Um retorno no teto (18.000) compra um luxo só.
  - **Alternativa: 14.000 / 18.000 / 22.000**, com a Fachada em +8,5 min.
- **Preço do Mineiro pela regra de retorno remedida:** ~500 (Δ +53 a +58), com o Joalheiro 2 em 2.400. A produção completa cai para **40:01**, fora da janela 42–48. O coordenador decide entre a regra de retorno e a janela.

## 15. Leva 11: menu inferior de melhorias, bocas laterais e luxo 14/18/22 mil (contrato `docs/archive/FASE6_MENU_MELHORIAS.md`, medido 2026-10-07)

**Vigente.** Os 9 upgrades do personagem e dos itens são comprados por toque no menu inferior (`UpgradeDef.InMenu`, `Sim.TryBuyMenu`), sem pad e sem dreno de 1 s: Fole, Fole duplo, Mochila, Botas, Vitrine, Ajudantes ágeis, Martelo veloz, Lupa e Vitrine de joias. As construções continuam pads. Fornalha, Bigorna e Joalheria passaram a ter entrada à esquerda e saída à direita. Os números de §14.4 viram **históricos**.

Harness: cópia do core em `%TEMP%\claude\…\scratchpad\l11` (`h/Program.cs`; preços de luxo e do par como `static` só na cópia), bot humano, 90 min, Dt 1/30 · 1/60. A base "antes" reproduz §14.4 ao segundo (produção 42:24, Fachada 49:08), o que valida o harness.

### 15.1 O que mudou no core
- **Menu:** `Pad.Current` devolve −1 para upgrade do menu. Os 8 pads desses upgrades (slots 0, 6, 7, 8, 10, 11, 15 e 16) ficam na lista, porque o save é por slot, e ficam invisíveis para sempre. No `Load`, o parcial pago neles volta para o `Gold`, com teto em `int.MaxValue` (mesmo laço do parcial do luxo escondido).
- **Política de compra** (dica e bot): a compra mais barata que o ouro já paga, comparando o preço do menu com o restante do pad. No empate fica o menu, que não exige andar (`Sim.CheapestAffordableMenu`).
  - O bot compra o upgrade do menu parado: ele lê a tela por `Reaction` (0,7 s, como em toda troca de alvo) e toca.
  - A compra acontece depois do `Tick`, para o `Ev.Bought` ficar em `Events` igual ao do pad.
- **`WalkNoDecision` continua contando só pad pagável:** comprar no menu não exige andar.
- **Teto offline (`CheapestLockedCost`):** não muda, porque menu e pad contam igual.
- **Bocas** (as outras estações não mudam; tabela completa em §13.1):

| estação | ENTRADA | SAÍDA | antes (§13.1) |
|---|---|---|---|
| Fornalha (1,5; 5,5) | esquerda (0,6; 5,5) | direita (2,4; 5,5) | abaixo (1,5; 4,85) / acima (1,5; 6,15) |
| Bigorna (1,5; 9,5) | esquerda (0,6; 9,5) | direita (2,4; 9,5) | abaixo (1,5; 8,85) / acima (1,5; 10,15) |
| Joalheria (12; 6,5) | esquerda (11,1; 6,5) | direita (12,9; 6,5) | abaixo (12; 5,85) / acima (12; 7,15) |

- **Distâncias conferidas** (`Bocas_Laterais_…` e `Bocas_Tabela_…`):
  - boca × pad que fica no mundo: ≥ 1,5 m. A menor é a entrada da Bigorna × pad da Esteira, 1,70 m. Antes era a saída da Fornalha × pad do Fole, 1,06 m;
  - boca × baú, × vaga das filas e × ponto de contratação: ≥ 2 m;
  - boca × parede e obstáculos: fora de corpo. A entrada da Joalheria fica a 1,5 m da face da parede, na frente da porta lateral;
  - zonas de bocas vizinhas: a saída da Fornalha e a entrada da Fornalha 2 ficam a 1,2 m, como a Fornalha 2 e as Ferramentas.
- **Colisão achada e ajustada: pad da Esteira (0,5; 7,8) → (0,5; 11,2)**, no mesmo slot 9.
  - Com as entradas da Fornalha e da Bigorna na parede esquerda, o pad ficou a 0,10 m do corredor entre elas. O bot humano passou por cima 5 vezes em 90 min, com até 0,43 s em cima (o limite de cobrança é `PadDwell` 0,5 s). Antes era 1 vez.
  - Um humano mais lento ou que pare ali pagaria sem querer.
  - Candidatos medidos:

| posição | travessias (1/30) | máx. em cima | pagamento acidental (1/30 · 1/60) |
|---|---|---|---|
| (0,5; 7,8) antiga | 5 | 0,43 s | 0 · 0 |
| (2,9; 7,6) | 29 | 1,17 s | **145** · 0 |
| (0,5; 11,0) · (0,5; 12,0) · (1,2; 11,2) | 0 | — | 0 · 0 |
| **(0,5; 11,2) aplicada** | **0** | — | **0 · 0** |

  - Escolha: o ponto mais perto da Bigorna, a ponta da esteira, mantendo a mesma folga de antes até a entrada (1,70 m).
  - Mineiro (0,5; 3,5): fica a 0,62 m da linha Depósito → entrada da Fornalha (raio do pad 0,6). Nenhuma travessia medida.
- **Bug achado e corrigido (`Sim.Via`):**
  - Sintoma: quem chegava ao ponto de passagem da porta com x 9,29999 (arredondamento) mirava o próprio lugar, o `Steer` devolvia direção zero e ele travava no vão para sempre.
  - Com a entrada nova da Joalheria, o Joalheiro travou aos 30 s em `Joalheiro2_Papel3Extra_…`.
  - Correção: `Via` devolve o alvo quando o personagem já está no meio do vão (|x − 9,3| < 0,01).
  - A mesma rotina serve ajudantes e bot (raiz, não sintoma).

### 15.2 Primeiros 10 min (bot humano; §3 do GDD)
| marco | antes (§14.4) | Leva 11 (Dt 1/30) | Dt 1/60 | GDD | fração do GDD |
|---|---|---|---|---|---|
| 1ª venda | 0:21 | **0:22** | 0:22 | < 1:30 | 24% |
| Fole (menu) | 1:04 | **1:07** | 1:07 | 2:30 | 45% |
| 2ª bigorna | 1:57 | **2:17** | 2:17 | 3:30 | 65% |
| Ajudante | 2:38 | **2:59** | 3:00 | 4:30 | 66% |
| Escudos | 3:45 | **4:00** | 4:01 | 5:30 | 73% |
| Esteira | 7:03 | **7:20** | 7:11 | 8:30 | 86% |
| comprados / receita em 10 min | 9 / 1.485 | **9 / 1.460** | 9 / 1.450 | — | — |
| desistiram / sem vaga | 0 / 71 | **1 / 65** | 0 / 68 | — | — |
| andar sem decisão | 34% | **34%** | 33% | < 50% | — |

- **A §3 continua verde** (`Bot_Primeiros10Minutos_BatemASecao3`). A Esteira tem 70 s de folga.
- **Bot ideal:** fole 0:56 · 2ª bigorna 2:10 · esteira 7:30 (antes 0:52 / 2:00 / 7:05).
- **Offline depois de 10 min:** 1.060, igual.
- **Contra a hipótese do contrato, os upgrades NÃO saíram mais cedo.** O A/B isolado (mesmo harness, antes da mudança do pad da Esteira) separa os dois efeitos:

| variante | 2ª bigorna | esteira | receita 10 min | produção completa (1/30 · 1/60) |
|---|---|---|---|---|
| antes (§14.4) | 1:57 | 7:03 | 1.485 | 42:24 · 42:16 |
| só o menu (bocas antigas) | 1:55 | 6:59 | 1.565 | **42:03 · 41:25** |
| menu + bocas laterais | 2:17 | 7:22 | 1.450 | **43:41 · 43:23** |
| final (Esteira em (0,5; 11,2)) | 2:17 | 7:20 | 1.460 | **43:37 · 43:29** |

  - **O menu sozinho adianta** a produção completa em 21–51 s.
  - **As bocas laterais atrasam** ~1:40–2:00. O laço do começo fica mais longo: a ida da saída da Fornalha à entrada da Bigorna passa de 2,7 m em linha reta para 4,4 m na diagonal, e do Depósito à entrada da Fornalha é preciso contornar a quina.
  - A variante "só bocas, sem menu" não é uma configuração real: os pads do Fole e do Martelo continuam no mundo e o bot paga 101–103 de ouro sem querer. Ela não entra na tabela.

### 15.3 Bot humano, 45/60/90 min (vigente)
- **Compras (Dt 1/30):** Fole 1:07\* · 2ª bigorna 2:17 · Ajudante 2:59 · Escudos 4:00 · Mochila 5:27\* · Ajudante 2 6:52 · Esteira 7:20 · Fole duplo 8:17\* · Botas 9:38\* · Ferramentas 10:55 · Vitrine 12:40\* · Corredor 13:26 · 2ª fornalha 13:49 · Ajudante 3 16:41 · Ajudantes ágeis 20:20\* · Joalheria 23:01 · Martelo veloz 25:21\* · Joalheiro 27:10 · Mineiro 30:04 · Joalheiro 2 32:16 · Lupa 36:50\* · **Vitrine de joias 43:37\* = produção completa**. (\* = menu.)
- **Dt 1/60:** produção completa **43:29**.
- **Janela 42–48:** a produção completa fica dentro dela nas duas bases. Mineiro 3.000 / Joalheiro 2 2.400 ficam e **não há proposta** de preço do par.
- **Baús:** 2:53 / 7:18 / 25:46 / 13:23.
- **Pagamento acidental em pad:** 0 nas duas bases.

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| antes (§14.4) | 78 | 219 | 442 | 463 | 729 | 1.093 | 1.155 | 1.298 | 1.492 | 1.655 | 1.621 | 1.682 |
| **Leva 11** | 67 | 225 | 384 | 410 | 658 | 986 | 1.162 | 1.245 | 1.439 | 1.583 | 1.631 | 1.623 |

| métrica (Dt 1/30 · 1/60) | antes (§14.4) | Leva 11 |
|---|---|---|
| ouro/min 41–60 | 1.625 · 1.636 | 1.584 · 1.574 |
| ouro/min 60–90 | 1.638 · 1.668 | 1.629 · 1.613 (−0,5% · −3,3%) |
| joias/min 60–90 | 14,3 · 14,7 | 14,0 · 13,9 |
| bancada de joias sem lingote, Joalheiro → 60 min | 12% · 12% | 16% · 15% |
| bancada de joias sem lingote, 60–90 min | 14% · 12% | 16% · 17% |
| receita em 90 min (luxo vigente de cada época) | 108.807 · 110.082 | 105.956 · 105.607 |

- **Fome da joalheria:** 15–17%, longe do critério da fase 4 (≤ 35%). **HIPÓTESE:** a pequena alta vem do atraso de ~1 min na Joalheria e no par.
- **45 min:** Corredor 13:26 · Joalheria 23:01 · 10,1 joias/min · ouro/min 490 → 968 com a Joalheria (+98%).

### 15.4 Luxo: varredura e preço APLICADO (14.000 / 18.000 / 22.000)
Com o menu e as bocas, a Fachada a 11.000 sai **+6,9 · +6,8 min** depois da produção completa, abaixo dos 8 min. Pela autorização do coordenador, apliquei preço novo pela regra: Fachada entre +8 e +12 min, intervalos crescentes, partindo de 14.000 / 18.000 / 22.000.

Varredura no core final, bot humano, 90 min (arquivos `logs/final/lux/<F>_<P>_<J>_<dt>.txt`):
```
precos (F / P / J)  | Dt 1/30: fachada  piso           joalheria real  | Dt 1/60: fachada  piso           joalheria real
11000/14000/18000   | 50:34 (+6,9)  59:00 (+15,4)  70:06 (+26,5)      | 50:19 (+6,8)  58:56 (+15,5)  69:53 (+26,4)   <- antigo
12000/15000/18000   | 51:05 (+7,5)  60:13 (+16,6)  71:16 (+27,7)      | 50:58 (+7,5)  60:17 (+16,8)  71:24 (+27,9)
13000/16000/20000   | 51:44 (+8,1)  61:43 (+18,1)  74:13 (+30,6)      | 51:35 (+8,1)  61:31 (+18,0)  73:54 (+30,4)
13000/17000/22000   | 51:44 (+8,1)  62:15 (+18,6)  75:50 (+32,2)      | 51:35 (+8,1)  62:07 (+18,6)  75:57 (+32,5)
14000/18000/22000   | 52:20 (+8,7)  63:22 (+19,7)  76:56 (+33,3)      | 52:12 (+8,7)  63:18 (+19,8)  76:53 (+33,4)   <- APLICADO
14000/19000/24000   | 52:20 (+8,7)  63:57 (+20,3)  78:42 (+35,1)      | 52:12 (+8,7)  63:55 (+20,4)  78:46 (+35,3)
15000/18000/22000   | 52:58 (+9,4)  63:55 (+20,3)  77:29 (+33,9)      | 52:49 (+9,3)  63:48 (+20,3)  77:30 (+34,0)
15000/19000/24000   | 52:58 (+9,4)  64:35 (+21,0)  79:31 (+35,9)      | 52:49 (+9,3)  64:25 (+20,9)  79:10 (+35,7)
16000/20000/24000   | 53:35 (+10,0) 66:00 (+22,4)  81:03 (+37,4)      | 53:28 (+10,0) 66:05 (+22,6)  80:54 (+37,4)
```
- **Aplicado: 14.000 / 18.000 / 22.000** (`Balance.WorkshopFacadeCost/WorkshopFloorCost/JewelryDecorCost`).
  - É o ponto de partida pedido e já cumpre a regra nas duas bases.
  - Fachada **+8,7 min** depois da produção completa.
  - Intervalos crescentes: 8,7 → 11,0 → 13,6 min (Dt 1/60: 8,7 → 11,1 → 13,6).
  - Joalheria real aos **76:56 · 76:53**, dentro dos 90 min.
- 16.000 / 20.000 / 24.000 (+10,0) também cabe na regra. Fica como alternativa se o playtest pedir o luxo mais espaçado.
- **Retorno no teto logo depois da produção completa** (Save → Load → `ApplyOffline` 18.000 → bot por 10 s): compra **1** luxo nas duas bases (Fachada 14.000; o troco não paga o Piso).
- **Com × sem luxo, de 44 a 90 min** (`Bot_90Minutos_Luxo`): ouro/min +0,5%, vendas/min +0,1%. Não há bônus.
  - Receita aos 90 min: 105.956 com luxo × 105.587 sem.
  - Saldo aos 90 min: 21.391 × 75.022.
- Isto substitui a proposta de §14.6 (16/20/24 mil) e a alternativa dela.

### 15.5 Portões remedidos (medido × 1,3, arredondado para cima em 10 s; pisos ~70%)
- **`Bot_45Minutos…`:**
  - Corredor ≤ 17:30;
  - Joalheria ≤ 30:00;
  - joias/min > 7,0;
  - a Joalheria aumenta a renda (medido +98%).
- **`Bot_60Minutos_Fase2`:**
  - tempos: Joalheiro ≤ 35:20, Mineiro ≤ 39:10, Joalheiro 2 ≤ 42:00, Lupa ≤ 48:00, Vitrine de joias ≤ 56:50;
  - baús: ≤ 3:50 / 9:30 / 33:30 / 17:30;
  - joias/min depois da produção > 9,5 (medido 13,6);
  - bancada de joias sem lingote ≤ 35% (medido 16%);
  - ouro/min 55–60 > 1.136 (medido 1.623).
- **`Bot_90Minutos_Luxo`:** Fachada ≤ 68:10, Piso ≤ 82:30, Joalheria real ≤ 90:00, com e sem luxo dentro de ±3%.
- **`Bot_Primeiros10Minutos_BatemASecao3`:** sem mudança (janelas do GDD).

### 15.6 Testes e prova vermelha
- **Novos em `CoreTests` (7):**
  - `Menu_OsNoveDoContrato_SemPadVisivel_PadsContinuamNaLista`
  - `TryBuyMenu_Ouro_PreRequisito_Idempotente_PadRecusado_Evento`
  - `TryBuyMenu_AplicaOMesmoEfeitoDaCompraPeloPad`
  - `Save_Antigo_ParcialEmPadQueVirouMenu_VoltaParaOOuro`
  - `Dica_BuyMenu_QuandoOMaisBaratoPagavelEDoMenu_BuyPadQuandoEDoPad`
  - `Bot_CompraNoMenuSemAndar_MesmaPoliticaDoPad`
  - `Bocas_Laterais_FornalhaBigornaJoalheria_LongeDosPadsQueFicam`
  - Mais uma asserção de regressão do `Via` em `Parede_…`.
- **Ajustados ao contrato novo:**
  - o teste de pad usa a cadeia da contratação e o pad da 2ª bigorna (Fole e Mochila saíram do mundo);
  - curva de custo: todo upgrade fica à venda no pad OU no menu;
  - dicas: `BuyMenu` para o Fole;
  - Lupa: à venda no menu;
  - saves de 15 e 20 flags: o parcial das Botas (30) e da Vitrine de joias (450) volta ao ouro;
  - tabela das bocas;
  - luxo: 14/18/22 mil, e o ralo é lido de `Upgrades.Cost`.
- **Suíte: 66/66.** `viewcheck`: 0 erros, 0 avisos.
- **Prova vermelha:** cópia isolada `scratchpad\l11\red` (`red.py`), rebuild `--no-incremental` a cada mutação, logs em `red/logs`. Cada teste novo ficou vermelho em pelo menos uma mutação. Restaurado: verde, e o SHA-256 da cópia é igual ao dos 5 arquivos reais.

| mutação na cópia | testes vermelhos |
|---|---|
| M1 `Pad.Current` mostra upgrade do menu | Menu_OsNove, Save_Antigo_Parcial |
| M2 `TryBuyMenu` aceita upgrade de pad | TryBuyMenu_Ouro, Dica_BuyMenu, Bot_CompraNoMenu |
| M3 `TryBuyMenu` ignora pré-requisito | TryBuyMenu_Ouro |
| M4 `TryBuyMenu` não cobra | TryBuyMenu_Ouro, TryBuyMenu_AplicaOMesmoEfeito, Bot_CompraNoMenu |
| M5 `TryBuyMenu` marca o flag sem `Buy`/`Recompute` | TryBuyMenu_AplicaOMesmoEfeito |
| M6 `Ev.Bought` do menu na posição do pad antigo | TryBuyMenu_Ouro |
| M7 dica sem `BuyMenu` | Dica_BuyMenu |
| M8 empate menu × pad vai ao pad | Dica_BuyMenu |
| M9 bot ignora o menu | Bot_CompraNoMenu |
| M10 bot compra antes do `Tick` (o evento some) | Bot_CompraNoMenu |
| M11 Fornalha com entrada embaixo | Bocas_Laterais |
| M12 Joalheria com entrada embaixo | Bocas_Laterais |
| M13 Esteira em (0,5; 7,8) | Bocas_Laterais |
| M14 `Load` não devolve o parcial escondido | Save_Antigo_Parcial |
| M15 `Load` sem teto `int.MaxValue` | Save_Antigo_Parcial |
| M16 `Via` sem a correção do vão | Parede, Joalheiro2_Papel3Extra |
| M17 Lupa sem a marca `menu` | Menu_OsNove, Save_Antigo_Parcial, TryBuyMenu_AplicaOMesmoEfeito, Bocas_Laterais |
| restaurado (SHA = core real) | verde |

## 16. v0.4.1 — ajustes visuais e pad do Mineiro (coordenador, 2026-10-07)
- **Pad do Mineiro** (0,5; 3,5) → **(2,8; 0,6)**, slot 20, no lugar das Botas (pad do menu, invisível; `Sim` ignora pad invisível na busca por posição). Motivo: a 0,62 m da linha Depósito → entrada da Fornalha, o humano pisaria nele de passagem. Bot: produção completa **43:37** (igual), Fachada +8,8 min, ouro/min 44–90 1.626; dotnet 66/66.
- **Arte das estações** de célula 1,65 m para 1,95 m: com 1,65 a peça tinha 0,9–1,24 m de largura e o corpo sólido 1,3 m (o ferreiro batia numa borda invisível). Só view; nenhum número muda.

## 17. FASE7: carga mista do ferreiro, balcão evolutivo 4 → 8 e venda só com cliente na vaga (contrato `docs/archive/FASE7_CARGA_BALCAO.md`, medido 2026-10-08)

**Vigente.** Origem: playtest do Vinicius na v0.4.1 (POCO F4): a trava dos lingotes com a bigorna cheia, o pedido do balcão evolutivo e "vendas sem ninguém pedindo". Os números de §15/§16 viram **históricos**. Harness: `scratchpad/fs7/h` (fontes reais do core; preços do balcão mudados em runtime), `h2` (vendas por caminho, compila contra o core antigo e o novo) e `red` (mutações). Bot humano, Dt 1/30 (portão) e 1/60.

### 17.1 O que mudou no core
- **Carga:** `Carrier.Held[6]`.
  - O ferreiro leva todos os tipos, até `Cap` de cada um; o ajudante, um tipo por vez.
  - A regra nova sozinha não muda nenhum tick do bot antigo em 90 min: ele nunca passava por uma boca recolhível carregando outra coisa.
  - O estado da trava (insumo na mão sem nenhuma entrada com espaço) apareceu **0 s** em 90 min de bot.
- **Balcão:** `Counter5..Counter8` no menu, em cadeia (o 1º exige a Vitrine), +1 vaga cada.
  - O corpo cresce com as vagas: meia-largura `CounterHalfX(n)` = 0,425·n + 0,2.
  - As vagas ficam centradas no balcão. A Vitrine perdeu a fila e ficou com estoque e clientes ×0,7.
  - Baú da oficina (2,8; 12,6) → (2,2; 12,0). Nos 10 min, a diferença entre posições do baú (1.380–1.480 de receita) é ruído do bot: no Dt 1/60 dá 1.450 nas duas.
- **Venda:** a compra direta da vitrine saiu (§17.2).
- **Bot:**
  - só leva ao balcão o produto que ele aceita (`Sim.Deliverable`);
  - insumo sem destino não o prende;
  - produto que a fila pede ganha +12 (`Sim.Wanted`).
  - A dica segue a mesma ordem.
- **`Sim.Steer`:** considera todos os corpos encostados e descarta rota cuja quina não cabe o personagem (§17.5).
- **Teto do cofre:** o mais barato **à venda** (§17.5).
- **Save:** grava a carga na mão (`hold=`, `wk=`).

### 17.2 Etapa C: vendas sem cliente visível (causa medida)
`Ev.Sold` só sai de `Sim.Sell`, chamado em dois lugares: o atendimento da fila e a compra direta (fila cheia + produto no estoque). A compra direta solta o "+10" na vaga depois da última, sem cliente desenhado.

| bot humano | v0.4.1, 10 min | v0.4.1, 45 min | FASE7, 10 min | FASE7, 45 min |
|---|---|---|---|---|
| Dt 1/30: vendas (fila / **direta**) | 107 (87 / **20 = 18,7%**) | 1.544 (945 / **377 = 28,5%**; joias 221 / 1) | 90 (90 / **0**) | 1.212 (1.047 / **0**; joias 165 / 0) |
| Dt 1/60: vendas (fila / **direta**) | 106 (90 / **16 = 15,1%**) | 1.561 (944 / **400 = 29,8%**) | 78 (78 / **0**) | 1.275 (1.102 / **0**) |
| fila_cheia \| cansou (1/30) | 65 \| 1 | 1.882 \| 1 | 79 \| 2 | 2.094 \| 2 |
| receita (1/30) | 1.460 | 32.723 | 1.320 | 27.075 |

- **Outras fontes descartadas:**
  - `Ev.Offline` não tem `case` no `HandleEvents`: o cofre aparece só no painel.
  - `Ev.Deposited` do ajudante só toca som.
  - `Ev.ChestOpened` solta "+ouro" no baú, mas são só 2–3 aberturas no jogo inteiro.
- **Sem a compra direta e sem mudar mais nada, o bot TRAVAVA aos ~10 min** (renda 0 até os 90 min).
  - Estado aos 15:00: o bot no balcão com 2 espadas, estoque de espada 5/5, fila = 4 clientes de escudo e 3 escudos prontos na bancada.
  - Os clientes de espada nunca acham vaga, porque os tempos ficam em fase. É a trava da §9.1, que a compra direta mascarava.
  - Ela se desfaz pela paciência, pela vaga a mais e pela carga mista, mais a regra do bot/dica "não espere num balcão que não aceita o que está na mão; busque o que a fila pede". Teste: `FilaCheiaDeEscudo_…_DestravaPelaPacienciaPelasVagasEPelaCargaMista`.

### 17.3 Ganho das vagas e preço
- **Com a compra direta (antes da Etapa C) as vagas valiam zero.** 4 → 8 dadas de graça no minuto T, janela de 10 min: Δ entre −50 e +18 ouro/min, média −2,5. A `fila_cheia` quase não caía (479 → 474 aos 10 min).
  - A Vitrine antiga (fila 6) e a nova (fila 4) deram a mesma receita em 90 min: 106.215 × 105.834 (Dt 1/30) e 107.041 × 105.941 (Dt 1/60).
  - A economia é limitada pela produção: no fim chegam ~110 clientes/min contra ~50 vendas/min. Quem não entrava comprava direto do estoque.
- **Sem a compra direta, as vagas rendem no meio do jogo.** Médias de 5 passos (Dt 1/30, 1/40, 1/45, 1/50, 1/60), janela de 15 min, vagas dadas no minuto T:

| T | ouro/min com 4 vagas | 5 | 6 | 7 | 8 | **4 → 8** | fila_cheia \| cansou (4 → 8 vagas) |
|---|---|---|---|---|---|---|---|
| 5 | 303 | −6 | +4 | +1 | −3 | **−4** | 535 \| 1 → 546 \| 4 |
| 10 | 357 | −10 | −5 | +36 | +4 | **+25** | 804 \| 0 → 790 \| 0 |
| 15 | 425 | +33 | +1 | +35 | +20 | **+89** | 1.000 \| 1 → 952 \| 1 |
| 20 | 636 | +5 | +32 | −2 | +22 | **+56** | 982 \| 2 → 965 \| 1 |
| 25 | 900 | +18 | +33 | −11 | +10 | **+50** | 944 \| 2 → 919 \| 2 |
| 30 | 1.142 | −10 | 0 | +9 | +11 | **+9** | 897 \| 1 → 872 \| 2 |
| 40 | 1.431 | −13 | +28 | −14 | +1 | **+2** | 949 \| 0 → 927 \| 0 |

  - **Mais vagas não trocam "fila_cheia" por "cansou"** (nota do benchmark): `cansou` fica em 0–4 em toda linha, porque a paciência por item (57–84 s) segura quem entra. A `fila_cheia` cai pouco (−2% a −5%), já que a produção continua sendo o limite.
  - O ganho por vaga tem ruído de ±15 e não se separa. **A regra de retorno foi aplicada à soma.** Na faixa 15–25 min, 4 → 8 rendem ~65 ouro/min; comprando aos ~15 min, +89/min.
- **Preço APLICADO: 150 / 175 / 200 / 225** (soma 750, retorno de 8,4 min sobre +89/min). **O Balcão 5 exige a Vitrine.**

| preços (5/6/7/8) | requisito do Balcão 5 | Esteira (1/30 · 1/60) | vagas compradas (1/30) | produção completa (1/30 · 1/60) |
|---|---|---|---|---|
| sem as vagas (4 sempre) | — | 7:28 · 7:30 | — | 49:01 · 47:52 (os 22 antigos) |
| 160 / 160 / 160 / 160 | nenhum | **11:12 · 11:13** | 6:40–8:32 | 51:09 · 49:44 |
| 120 / 150 / 180 / 210 | nenhum | **10:23 · 10:33** | 5:08–9:27 | 47:20 · 47:52 |
| 120 / 150 / 180 / 210 | Vitrine | 7:28 · 7:30 | 13:41–14:43 | 49:52 · 48:02 |
| **150 / 175 / 200 / 225** | **Vitrine** | **7:28 · 7:30** | **13:45–14:43** | **48:24 · 47:32** |
| 200 / 250 / 300 / 350 | Vitrine | 7:28 · 7:30 | 13:46–14:48 | 51:10 · 48:32 |
| 300 / 350 / 400 / 450 | Vitrine | 7:28 · 7:30 | 14:05–14:51 | 50:37 · 50:02 |

  - **Sem requisito**, o preço da regra põe as vagas nos primeiros 5–9 min, onde rendem 0, e a Esteira sai da janela da §3 (≤ 8:30).
  - **Com a Vitrine**, elas caem logo depois dela (13:28), onde começam a render, e a §3 fica idêntica à base.
  - **No jogo inteiro**, as vagas se pagam:
    - ouro/min 15–25 min: +85/+91 (Dt 1/30) e +19/+87 (Dt 1/60) contra a base sem vagas;
    - receita em 90 min: 95.926 → 98.151 (Dt 1/30) e 98.215 → 99.992 (Dt 1/60), já descontados os 750.

### 17.4 Bot humano antes (v0.4.1) × depois (FASE7), Dt 1/30 (`BalanceTests`)
| marco | v0.4.1 | FASE7 | janela |
|---|---|---|---|
| 1ª venda | 0:22 | 0:22 | < 1:30 |
| Fole · 2ª bigorna · Ajudante · Escudos | 1:07 · 2:17 · 2:59 · 4:00 | 1:07 · 2:17 · 2:59 · 4:00 | §3 |
| Esteira | 7:20 | **7:28** (Dt 1/60 7:30) | ≤ 8:30 |
| receita · compras em 10 min | 1.460 · 9 | **1.320** · 9 (Dt 1/60: 1.450 → 1.170) | — |
| Vitrine · Balcão 5/6/7/8 | 12:40 · — | 13:28 · 13:45 / 14:05 / 14:28 / 14:43 | — |
| Corredor · Joalheria | 13:26 · 23:01 | 14:46 · 26:44 | — |
| Joalheiro · Mineiro · Joalheiro 2 · Lupa | 27:10 · 30:05 · 32:20 · 36:49 | 31:42 · 34:35 · 36:47 · 41:21 | — |
| **produção completa** | **43:37** (22 produtivos) | **48:24** (26; Dt 1/60 47:32) | 42–48 |
| Fachada · Piso · Joalheria real | 52:25 · 63:32 · 76:54 | 56:57 (+8,6) · 68:09 (+19,8) · 82:08 (+33,7) | Fachada +8…12 |
| receita · vendas em 90 min | 106.006 · 3.775 | 98.151 · 3.059 | — |
| **fila_cheia \| cansou** em 10 / 45 / 60 / 90 min | 65\|1 · 1.882\|1 · 2.758\|1 · 4.519\|1 | 79\|2 · 2.094\|2 · 3.084\|2 · 5.091\|2 | — |
| fome da joalheria (Joalheiro → 60 min) | 14% | **9%** | ≤ 35% |
| andar sem decisão (10 min · 45 min) | 34% · 30% | 34% · 23% | < 50% |
| cofre ofline depois de 10 min | 1.060 | 1.060 | — |
| baús abertos | 2:53 / 7:18 / 25:46 / 13:23 | 2:53 / 7:26 / 28:57 / 14:42 | — |

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| v0.4.1 | 67 | 225 | 384 | 410 | 658 | 986 | 1.147 | 1.292 | 1.372 | 1.617 | 1.598 | 1.661 |
| **FASE7** | 65 | 199 | 375 | 424 | 394 | 551 | 992 | 1.138 | 1.274 | 1.397 | 1.656 | 1.551 |

- **O custo é da Etapa C, não das vagas:**
  - Sem a compra direta, 28–30% das vendas do balcão (que eram clientes invisíveis) viram "fila_cheia". A receita cai 7% em 90 min e as vendas, 19%.
  - O vale de 20–30 min é a fila entupida de clientes de escudo/ferramenta antes do Ajudante 3 e da Joalheria.
  - As vagas devolvem parte disso: sem elas, a produção sairia aos 49:01.
- **A produção completa sai 1:24 depois da janela 42–48 no Dt 1/30** (47:32 no Dt 1/60, dentro). **Não repreçei nada fora da FASE7.** Se o coordenador quiser voltar a ~44 min, as alavancas medidas são o preço das vagas (já no mínimo da regra) ou a paciência e a fila. Fica a decisão.
- A Fachada sai +8,6 min depois da produção, dentro da regra de +8…12 min, sem mexer no luxo.

### 17.5 Achados corrigidos no caminho
- **Bolso da Loja de joias (`Sim.Steer`):**
  - Com a Joalheria real, a loja (x 11,35–12,65; y 11,1–11,9) e os 2 pedestais da frente (10,95 e 13,05; 10,9) deixam vãos de 0,1–0,2 m dos dois lados da boca.
  - O `Steer` olhava só o 1º corpo encostado e escolhia a quina do vão. Aos 81 min (Dt 1/60, com a carga mista), o bot ficou 9 min parado em (11,40; 10,79): ouro/min 85–90 1.354 e 64 "cansou".
  - **Correção:** todos os corpos encostados entram na conta, e a rota cuja quina não cabe o personagem (`Sim.Fits`) é descartada. Nada muda antes desse ponto: as partidas são idênticas ao tick até ali. Depois: 85–90 min = 1.625 ouro/min e 0 "cansou".
  - **Teste:** `Steer_BocaDaLojaDeJoias_EntreLojaEPedestais_SaiDoVao`. A geometria dos pedestais (duplicada em `WorldView.Pedestals`) não mudou.
- **Teto do cofre (`CheapestLockedCost`):**
  - O Balcão 5 (150, exige a Vitrine) virou o "menor travado" e derrubou o cofre dos 10 min de 1.060 para 300.
  - **Agora o teto conta só o que está à venda (pré-requisito comprado)**, e os 10 min voltam a 1.060.
  - Efeito colateral único: na janela do par, o teto passa de 4.800 (2 × Joalheiro 2, que exige o Mineiro e não está à venda) para **6.000** (2 × Mineiro). Produção completa: 18.000, igual.

### 17.6 Portões remedidos (medido × 1,3, arredondado para cima em 10 s; pisos ~70%)
- **`Bot_45Minutos…`:** Corredor ≤ 19:20; Joalheria ≤ 34:50; joias/min > 6,3 (medido 9,0). A Joalheria aumenta a renda (371 → 792, +113%).
- **`Bot_60Minutos_Fase2`:**
  - tempos: Joalheiro ≤ 41:20; Mineiro ≤ 45:00; Joalheiro 2 ≤ 47:50; Lupa ≤ 53:50; Vitrine de joias dentro dos 60 min;
  - **as 4 vagas até 19:10** (medido 14:43);
  - baús: ≤ 3:50 / 9:40 / 37:40 / 19:10;
  - joias/min depois da produção > 10,3 (medido 14,8);
  - fome ≤ 35% (medido 9%);
  - ouro/min 55–60 > 1.085 (medido 1.551).
- **`Bot_90Minutos_Luxo`:** Fachada ≤ 74:10; Piso ≤ 88:40; Joalheria real ≤ 90:00. Com e sem luxo dentro de ±3%; medido −1,1% em ouro/min e +1,1% em vendas/min.
- **`Bot_Primeiros10Minutos_BatemASecao3`:** sem mudança.

### 17.7 Testes e prova vermelha
- **Suíte: 72/72** (66 − 1 substituído + 7 novos). `viewcheck` com o core das fontes: 0 erros, 0 avisos.
- **Novos (7):**
  - `Carga_LingotesNoTeto_BigornaCheia_PegaEspadasEVende` (a trava do Vinicius)
  - `Venda_SempreDeClienteNaVaga_SemCompraDireta`
  - `FilaCheiaDeEscudo_EspadaNoEstoque_DestravaPelaPacienciaPelasVagasEPelaCargaMista`
  - `Balcao_4a8Vagas_EmCadeiaNoMenu_ExigeVitrine_VitrineSoEstoqueEClientes`
  - `Balcao_VagasCentradas_CorpoCresce_SemEngolirPadsBocasERotas` (4..8 vagas)
  - `Save_CargaDaMao_JogadorMistaEAjudantes_IdaEVolta_SaveAntigoMaosVazias`
  - `Steer_BocaDaLojaDeJoias_EntreLojaEPedestais_SaiDoVao`
  - O antigo `FilaCheia_ProdutoNaVitrine_ClienteCompraDireto_NaoTrava` foi trocado pelos dois de venda/trava.
- **Reescritos ao contrato novo:**
  - `Capacidade_TetoPorTipo_JogadorMistura_AjudanteNao` (antes: "nunca mistura");
  - a dica (lingote na mão + espada pronta = pegar a espada);
  - bocas (a saída recolhe com minério na mão);
  - fila/Vitrine (a fila cresce no balcão);
  - a compra de joia com a fila cheia;
  - luxo e cofre com as vagas e com o teto "à venda";
  - saves de 25 → 29 flags;
  - ciclo dos ajudantes com o jogador parado: > 250 vendas em 10 min, medido 288. Antes era > 300, mas a compra direta contava.
- **Prova vermelha A (core antigo, sem adaptar):** os 2 testes novos que só usam API antiga foram copiados literalmente para uma cópia isolada com o core da v0.4.1 (SHA = HEAD), em `scratchpad/fs7/redold`, com `dotnet build --no-incremental && dotnet test --no-build`:
  - `Carga_LingotesNoTeto…` vermelho: "recolhe as espadas com os lingotes na mao — Expected True, But was False";
  - `Venda_SempreDeCliente…` vermelho: "toda venda tem um cliente que saiu da vaga — Expected 0, But was 4";
  - os mesmos 2 contra o core novo: verdes.
- **Prova vermelha B (mutações):** cópia isolada do core novo (`scratchpad/fs7/red/red.py`), rebuild limpo a cada mutação, resumo em `red/summary.txt`. Tabela abaixo.

| mutação na cópia | testes vermelhos |
|---|---|
| A1 ferreiro com pilha homogênea (`CanPick`) | Carga_LingotesNoTeto, Capacidade_TetoPorTipo, Dica_SegueOEstado, FilaCheiaDeEscudo (rodada à mão; a automática não leu a saída) |
| A2 dica de produto pronto só de mãos vazias | Dica_SegueOEstado, FilaCheiaDeEscudo |
| C1 compra direta de volta | Venda_SempreDeClienteNaVaga, VitrineDeJoias_Preco60Antes80Depois |
| C2 dica "leve ao balcão" com o estoque cheio | FilaCheiaDeEscudo |
| C3 bot espera no balcão com o estoque cheio | **Bot_45Minutos** (o bot trava aos ~10 min) |
| B1 Vitrine volta a dar fila (+2) | Balcao_4a8Vagas, Balcao_VagasCentradas, Fila_Cheia |
| B2 evoluções não somam vaga | Balcao_4a8Vagas, Balcao_VagasCentradas, FilaCheiaDeEscudo, Fila_Cheia |
| B3 vagas à direita do balcão (antigo) | Balcao_VagasCentradas |
| B4 corpo do balcão fixo (1,3 m) | Balcao_VagasCentradas |
| B5 Balcão 5 sem requisito | Balcao_4a8Vagas, Offline_IgnoraLuxo |
| B6 baú da oficina no lugar antigo | Balcao_VagasCentradas |
| B7 teto ofline ignora pré-requisito (antigo) | Luxo_EsperaOs26Produtivos, Offline_IgnoraLuxo |
| S1 save sem `hold=` | Save_CargaDaMao |
| S2 `Load` do ajudante aceita qualquer tipo | Save_CargaDaMao |
| S3 `Load` sem teto por tipo do ferreiro | Save_CargaDaMao |
| P1 `Steer` sem a checagem de quina que cabe | Steer_BocaDaLojaDeJoias |
| restaurado (SHA da cópia = fontes reais) | verde, 72/72 |

## 18. FASE8: cliente VIP e velocidade 2×/3× por anúncio (contrato `docs/FASE8_VIP_VELOCIDADE.md`, medido 2026-10-08)

**Vigente.** Origem: pedido do Vinicius (VIP que paga 2× ou 3× a cada ~5 min, vídeo para ele vir antes, propaganda para 2× ou 3× por 1 min). Os números de §17.4 viram **históricos**.

Harnesses em `scratchpad/fs8`:
- `h`: fontes reais do core; `base` = VIP desligado (`VipIn = 1e9`).
- `sweep.py`: cópias com outra regra de pacote ou paciência.
- `h/Lock.cs`: um anúncio só, em passo travado.
- `red.py`: mutações.

Bot humano, Dt 1/30 (portão). As médias usam 5 passos (1/30, 1/40, 1/45, 1/50, 1/60), porque o bot é caótico: um passo sozinho varia ±5% no fim. **Validação do harness:** com o VIP desligado e sem boost, o core novo reproduz §17.4 ao tick (Esteira 7:28, produção 48:24, receita 98.151). Logo, boost em 1× e VIP ausente não mudam nada.

### 18.1 O que mudou no core
- **VIP:**
  - **Relógio** (`VipIn`): só depois da 1ª venda e no tempo de jogo. O intervalo é de 4–6 min (`VipInterval(n)`, sequência de Weyl).
  - **Vaga:** o VIP espera fora da fila cheia e pega a próxima vaga antes de quem chega.
  - **Pedido:** produto em rodízio (espada → escudo → ferramenta). O pacote é calculado pela renda (§18.2), a 3× por unidade, uma unidade por atendimento.
  - **Paciência:** 120 s. O VIP que cansa fica fora do `ClientsLost`.
  - **Eventos:** `VipArrived`, `VipServed`, `VipLeft`. Cada unidade é um `Sold` com B = 3× o preço.
- **`SummonVip`:** só depois da 1ª venda, sem VIP ativo e com o natural a > 60 s. Na rodada 1, trazia o VIP já e reiniciava o relógio. **Rodada 2 (§18.8):** traz um VIP extra, com 2× o pacote, e não mexe no relógio.
- **Boost (`StartBoost`):** 2× por 60 s; o 2º anúncio sobe para 3× e reinicia os 60 s. **Rodada 2 (§18.8):** recarga de 5 min contada do fim do boost (`CanBoost`, `BoostCooldown`).
  - **Acelera:** estações, esteira, ajudantes e chegada de clientes.
  - **Ferreiro:** no máximo 1,3×.
  - **No tempo de jogo:** paciência, atendimento, relógio do VIP e o próprio boost.
- **Taxa online (`RateEma`):** passa a aprender o ouro por segundo de fábrica (dt × boost). Sem boost é idêntica.
  - Congelar a média durante o boost (1ª versão) deixava a taxa velha para quem renova o boost sem parar: o pacote do VIP ficava em 3 e o cofre ficava na taxa de antes.
- **Save:** `vip=` guarda o relógio, os VIPs sorteados e o VIP pendente; o da vaga volta pendente. O boost não vai.
- **Bot:** sem mudança. Ele não assiste anúncio e trata o VIP como um cliente (`Wanted`).

### 18.2 Pacote do VIP (varredura em cópias do core, bot humano, 90 min, Dt 1/30; o % é do ouro da janela de 5 min)
| regra do pacote | VIP % da receita (1/30 · 1/60) | janelas 5–15 min | 15–30 | 30–90 | produção completa (1/30 · 1/60) |
|---|---|---|---|---|---|
| fixo 3 | 2,6 · 2,6 | 9–12 | 5–9 | **0–5** | 48:32 · 48:26 |
| fixo 5 | 4,4 · 4,1 | **15–19** | 8–12 | 0–6 | 47:39 · 43:32 |
| `Player.Cap` (3; 6 com a Mochila) | 5,0 · 5,0 | **17–22** | 8–12 | 0–6 | 45:04 · 45:37 |
| renda 5% | 4,8 · 4,8 | 9–12 | 5–9 | 3–5 | 48:04 · 47:54 |
| **renda 7% (APLICADO)** | **5,6 · 5,8** | **9–12** | **7–8** | **3–7** | **47:03 · 48:06** |
| renda 10% | 6,6 · 6,7 | 9–12 | 6–9 | 4–10 | 45:26 · 46:10 |

- **Fórmula:** pacote = clamp(round(7% × 300 s × `RateEma` / (3 × preço)), 3, `CounterCap`).
- **Por que a renda:**
  - Com tamanho fixo, o VIP some da conta depois dos 35 min, quando a joia, que ele não pede, domina a renda.
  - Fixo maior estoura o alvo no começo.
  - A regra de 7% fica na faixa 5–10% em quase todas as janelas. As exceções são 3–4%, quando o sorteio cai na borda de uma janela.
- **Pacotes medidos (Dt 1/30):**
  - 5:36 esp 3 · 10:05 esc 3 · 15:47 fer 3 · 20:44 esp 5 · 24:54 esc 3 · 30:19 fer 6 · 34:59 esp 10 · 40:53 esc 6 · 46:02 fer 10;
  - depois, espada e ferramenta 10 e escudo 7.

### 18.3 Paciência do VIP (pacote 7%, 90 min, Dt 1/30 + 1/60; "lento" = bot com 1,5 s de reação e 60% de stick)
| paciência | VIPs que cansaram (bot · lento) | maior tempo na vaga até levar (bot · lento) |
|---|---|---|
| 60 s | 11 de 36 · 13 de 35 | 60 s · 58 s (no teto) |
| 90 s | 0 · 1 | 76 s · 89 s |
| **120 s (APLICADO)** | **0 · 0** | **76 s · 98 s** |
| 180 s | 0 · 0 | igual a 120 |

- Na média, a espera por vaga é de 2–3 s (máximo 9–13 s) e o VIP leva o pacote em ~40 s.

### 18.4 Bot A/B, rodada 1 (HISTÓRICO: sem recarga e com o chamado reiniciando o relógio; vigente em §18.8; médias de 5 passos, 90 min)
| cenário | anúncios | receita 10 / 45 / 90 min | ouro/min 0–10 / 10–30 / 30–60 / 60–90 | Esteira · Corredor · Joalheria · produção · Fachada |
|---|---|---|---|---|
| sem VIP (base, = FASE7) | 0 | 1.239 / 27.059 / 98.114 | 124 / 436 / 1.338 / 1.601 | 7:30 · 15:25 · 26:53 · 48:17 · 57:06 |
| **(a) VIP natural (vigente)** | 0 | 1.361 / 28.980 / 103.341 | 136 / 473 / 1.421 / 1.663 | 7:08 · 15:09 · 25:53 · **46:41** · 55:11 |
| (b) a + `SummonVip` a cada 5 min | 18 | 1.530 / 30.470 / 106.778 | 153 / 495 / 1.471 / 1.707 | 7:14 · 14:52 · 25:24 · 45:44 · 54:02 |
| (c) a + boost 2× a cada 5 min | 18 | 1.493 / 39.005 / 127.856 | 149 / 665 / 1.796 / 1.972 | 6:28 · 13:46 · 22:34 · **40:46** · 47:55 |
| (d) a + boost 3× (2 anúncios) a cada 5 min | 36 | 1.463 / 47.129 / 149.601 | 146 / 787 / 2.135 / 2.278 | 6:31 · 13:16 · 21:13 · 37:37 · 43:50 |
| (e) abuso: 2× renovado sem parar | 90 | 3.789 / 89.070 / 233.066 | 379 / 1.864 / 3.198 / 3.203 | **4:49** · 8:49 · 14:50 · **26:56** · 31:19 |

- **O VIP natural se paga sozinho:**
  - +5,3% de receita em 90 min e produção 1:36 mais cedo na média (46:41). No portão (Dt 1/30) ela volta para dentro da janela de 42–48 (47:03); no Dt 1/60 dá 48:06.
  - A Esteira sai 22 s mais cedo, ainda dentro da §3.
  - O VIP é 5,7% da receita.
- **Por anúncio, contra (a):**
  - `SummonVip`: **+191 ouro** (+3,3% com 18 anúncios);
  - boost 2×: **+1.362** (+24%, produção −5:55);
  - boost 3×: **+1.285 por anúncio** (+45%, produção −9:04).
- **Um anúncio só no minuto T** (passo travado: partidas idênticas até T; ganho nos 10 min seguintes, em minutos da renda daquele momento; médias de 5 passos):

| faixa de T | renda (ouro/min) | `SummonVip` | boost 2× (1 anúncio) | boost 3× (2 anúncios) |
|---|---|---|---|---|
| 5–10 min | 205–402 | 117 (0,46 min) | 260 (0,82 min) | 310 (0,92 min) |
| 15–30 min | 412–1.073 | 169 (0,34 min) | 851 (1,53 min) | 1.409 (2,73 min) |
| 35–85 min | 1.214–1.707 | 281 (0,19 min) | 1.502 (0,95 min) | 2.971 (1,89 min) |

- **Preço implícito do anúncio:**
  - Boost: ~**1 min de renda** por anúncio. O 2º anúncio (2× → 3×) vale o mesmo que o 1º depois dos 15 min e quase nada antes, quando o ferreiro, a 1,3×, é o limite.
  - `SummonVip`: **0,2–0,5 min**, porque o relógio reinicia e o anúncio só adianta a fila de VIPs. Além disso, só o excedente de 2× sobre o preço normal é ganho de verdade, já que a produção é o limite.

### 18.5 Vigente: bot humano FASE7 × FASE8, Dt 1/30 (`BalanceTests`)
| marco | FASE7 | FASE8 | janela |
|---|---|---|---|
| 1ª venda | 0:22 | 0:22 | < 1:30 |
| Fole · 2ª bigorna · Ajudante · Escudos | 1:07 · 2:17 · 2:59 · 4:00 | 1:07 · 2:17 · 2:59 · 4:00 | §3 |
| 1º VIP | — | **5:36** (3 espadas, +90) | §3: rewarded opcional em 5:30–6:30 |
| Esteira | 7:28 | **7:17** | ≤ 8:30 |
| receita · compras em 10 min | 1.320 · 9 | 1.310 · 8 (as Botas saem aos 10:00) | — |
| Vitrine · Balcão 5 / 8 | 13:28 · 13:45 / 14:43 | 13:33 · 14:03 / 15:14 | — |
| Corredor · Joalheria | 14:46 · 26:44 | 15:52 · 26:10 | — |
| Joalheiro · Mineiro · Joalheiro 2 · Lupa | 31:42 · 34:35 · 36:47 · 41:21 | 31:01 · 33:55 · 35:54 · 40:33 | — |
| **produção completa** | 48:24 (Dt 1/60 47:32) | **47:03** (Dt 1/60 48:06) | 42–48 |
| Fachada · Piso · Joalheria real | 56:57 (+8,6) · 68:09 · 82:08 | 55:38 (+8,6) · 66:24 (+19,3) · 79:41 (+32,6) | Fachada +8…12 |
| receita 90 min | 98.151 | 102.661 (+4,6%) | — |
| fila_cheia \| cansou (normais) em 90 min | 5.091 \| 2 | 5.226 \| 20 | — |
| VIP em 60 min | — | 11 sorteados, 11 levaram, 0 cansaram; 3.177 ouro = 6,0% | — |
| fome da joalheria · andar sem decisão (10 · 45 min) | 9% · 34% · 23% | 9% · 29% · 19% | ≤ 35% · < 50% |
| cofre ofline depois de 10 min | 1.060 | 820 | — |
| baús abertos | 2:53 / 7:26 / 28:57 / 14:42 | 2:53 / 7:15 / 28:06 / 15:49 | — |

| ouro/min (janela de 5 min) | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FASE7 | 65 | 199 | 375 | 424 | 394 | 551 | 992 | 1.138 | 1.274 | 1.397 | 1.656 | 1.551 |
| **FASE8** | 65 | 197 | 374 | 414 | 419 | 579 | 1.061 | 1.204 | 1.360 | 1.551 | 1.674 | 1.668 |

- **Cofre de 10 min, 1.060 → 820:** o VIP das 5:36 adianta a Esteira, e as Botas (410) caem 1 tick depois dos 10:00. O teto passa a 2 × 410. É só ordem de compra.
- **cansou 2 → 20:** são clientes normais. O VIP segura o estoque do produto dele por ~40 s. São +18 em 90 min, contra ~3.000 vendas.
- **Portões:** os de §17.6 ficaram como estavam e continuam folgados. Não foram afrouxados.
  - **Novo, em `Bot_60Minutos_Fase2`:** ≥ 8 VIPs levaram o pacote (medido 11), ≤ 1 cansou (medido 0) e VIP entre 4,2% e 10% da receita (medido 6,0%).

### 18.6 Riscos e propostas da rodada 1 (as duas primeiras foram decididas pelo coordenador e aplicadas em §18.8)
- **Boost sem trava de frequência no core:**
  - O contrato pede "2º anúncio sobe para 3×". Renovado sem parar (cenário e), o jogo roda ~1,7× mais rápido: produção aos 26:56 e Esteira aos 4:49, perto do piso da §3 (4:15).
  - **Proposta:** a raia do SDK limita a 1 boost (até 2 anúncios) a cada 5 min. Nessa cadência, a produção sai aos 40:46 (cenário c).
- **`SummonVip` fraco** (0,2–0,5 min de renda contra ~1 min do boost). Propostas:
  - (1) o VIP chamado não reinicia o relógio e vira um VIP extra (+1 VIP inteiro, ~0,23 min de renda marginal);
  - (2) o VIP chamado traz o pacote 2–3× maior;
  - (3) trocar o botão por "maleta de ouro" (o rewarded mais assistido no MPH, BENCHMARK §8).
  - Decisão do coordenador.
- **Pacote pela taxa online:** é estável no bot (`RateEma` = média de 60 s). Num humano que para de jogar por um tempo, a taxa cai e o VIP seguinte vem com o mínimo de 3. É aceitável: o VIP acompanha o ritmo de quem joga.

### 18.7 Testes e prova vermelha, rodada 1 (a rodada 2 está em §18.8)
- **Suíte: 78/78** (72 + 6 novos). `viewcheck` (core das fontes, `--artifacts-path` temporário): 0 erros, 0 avisos, **sem tocar na View**.
- **Novos (6):**
  - `Vip_SoDepoisDa1aVenda_ChegaEntre4e6Min_EsperaAVagaSemSumir`
  - `Vip_PacoteA3xPorUnidade_UmPorAtendimento_TamanhoPelaRenda_ProdutoAberto`
  - `Vip_CansaPelaPaciencia_PagaSoOQueLevou_ForaDoClientsLost`
  - `SummonVip_SoSemVipAtivoEComONaturalAMaisDe60s_ReiniciaORelogio` (reescrito na rodada 2 como `SummonVip_VipExtra_PacoteDobrado_NaoMexeNoRelogio_…`)
  - `Boost_2xPor60s_2oAnuncioSobePara3x_TetoEExpira_OQueAcelera`
  - `Save_RelogioEVipPendente_IdaEVolta_BoostNaoVai_SaveAntigo`
  - Mais o portão do VIP em `Bot_60Minutos_Fase2`.
- **Prova vermelha (mutações):**
  - Os testes novos usam API nova, então não há prova contra o core antigo.
  - Rodei 30 mutações numa cópia isolada (`scratchpad/fs8/red.py`), com rebuild limpo a cada uma, rodando os 6 testes novos + `Bot_60Minutos_Fase2`. Resumo em `red_summary.txt`.
  - **Todas ficaram vermelhas**, e cada teste novo cai em pelo menos uma.

| mutação na cópia | testes vermelhos |
|---|---|
| V1 relógio anda antes da 1ª venda | Vip_SoDepoisDa1aVenda |
| V2 fila cheia: o VIP some | Vip_SoDepoisDa1aVenda, **Bot_60Minutos_Fase2** |
| V3 VIP paga 1× | Vip_Pacote, Vip_Cansa, **Bot_60Minutos_Fase2** |
| V4 VIP sai depois da 1ª unidade | Vip_Pacote, Vip_Cansa, Save_Relogio, **Bot_60Minutos_Fase2** |
| V5 pacote fixo em 3 | Vip_Pacote, **Bot_60Minutos_Fase2** (VIP < 4,2%) |
| V6 pacote sem o teto da prateleira | Vip_Pacote |
| V7 paciência do VIP = a do normal | Vip_SoDepoisDa1aVenda, Vip_Cansa, Save_Relogio, **Bot_60Minutos_Fase2** |
| V8 VIP cansado no ClientsLost | Vip_Cansa |
| V9 sempre espada · V10 joia no rodízio do balcão | Vip_Pacote · Vip_Pacote |
| S1 sem a regra dos 60 s · S2 com VIP ativo · S3 sem reiniciar o relógio · S4 antes da 1ª venda | SummonVip (as 4) |
| B1 2º anúncio não sobe · B2 não reinicia os 60 s · B3 sem teto 3× · B4 ferreiro sem o 1,3× · B5 estações · B6 ajudantes · B7 chegada sem boost · B8 paciência em tempo de fábrica · B9 não expira · B10 cofre aprende o boost | Boost (as 10) |
| L1 save sem `vip=` · L2 VIP da vaga fora do save · L3 load aceita produto fechado · L4 load sem teto do relógio · L5 boost no save | Save_Relogio (as 5) |
| L6 relógio inicial em 0 (save antigo com VIP imediato) | Save_Relogio, Vip_Pacote, Vip_SoDepoisDa1aVenda |
| restaurado (SHA da cópia = fontes reais `d3bd9d44a7`) | verde, 7/7 |

### 18.8 Rodada 2: decisões do coordenador aplicadas (vigente para a recarga do boost; a paciência do VIP e o teto do VIP chamado mudaram em §18.9)

**Decisões:**
- **Recarga do boost:** 5 min de jogo, contados do **fim** de cada boost.
  - `Sim.CanBoost` = (boost ativo e `BoostMul` < 3) ou (sem boost e recarga zerada). `Sim.BoostCooldown` dá os segundos restantes.
  - O 2º anúncio durante o boost continua subindo para 3× e reiniciando os 60 s.
  - `StartBoost` recusado devolve 0 e não muda nada.
- **Recarga fora do save** (decisão minha, autorizada pelo coordenador): reabrir libera um boost. Gravar exigiria descontar o tempo offline. `// ponytail:` no `Sim`.
- **VIP chamado = VIP EXTRA:** não reinicia nem adianta o relógio natural, e traz 2× o pacote do natural até a prateleira (`Balance.VipSummonPackMul` = 2).
  - A regra de uso não muda.
  - Se o natural zerar com o extra na vaga, ele espera o extra sair (≤ 120 s).

O VIP natural e o bot sem anúncio **não mudam** (cenário a idêntico ao tick; `BalanceTests` iguais aos de §18.5).

**Bot A/B, rodada 2** (médias de 5 passos, 90 min; os anúncios saem na marca de 5 min, assim que a recarga ou a regra do VIP deixa):

| cenário | anúncios | receita 10 / 45 / 90 min | ouro/min 0–10 / 10–30 / 30–60 / 60–90 | Esteira · Corredor · Joalheria · produção · Fachada |
|---|---|---|---|---|
| sem VIP (base) | 0 | 1.239 / 27.059 / 98.114 | 124 / 436 / 1.338 / 1.601 | 7:30 · 15:25 · 26:53 · 48:17 · 57:06 |
| **(a) VIP natural** | 0 | 1.361 / 28.980 / 103.341 | 136 / 473 / 1.421 / 1.663 | 7:08 · 15:09 · 25:53 · 46:41 · 55:11 |
| **(b) a + VIP extra a cada 5 min** | 18 | 1.637 / 34.128 / 111.949 | 164 / 584 / 1.559 / 1.729 | 7:14 · 15:28 · 24:05 · **43:16** · 51:30 |
| **(c) a + boost 2× (com recarga)** | 15 | 1.493 / 38.705 / 125.404 | 149 / 688 / 1.759 / 1.913 | 6:28 · 13:47 · 23:07 · **41:12** · 48:13 |
| (d) a + boost 3× (2 anúncios, com recarga) | 30 | 1.463 / 44.847 / 144.525 | 146 / 795 / 2.052 / 2.187 | 6:31 · 13:45 · 21:54 · 38:36 · 45:20 |
| (e) máximo: 3× toda vez que a recarga deixa | 31 | 1.950 / 47.160 / 143.834 | 195 / 780 / 2.025 / 2.184 | 6:24 · 12:43 · 20:36 · **37:30** · 44:07 |

- **Ganho por anúncio contra (a), em ouro e em minutos da renda média de (a) (1.148 ouro/min):**

| anúncio | rodada 2 | rodada 1 | efeito no jogo |
|---|---|---|---|
| VIP extra | **+478 = 0,42 min** | +191 = 0,17 min | +8,3% de receita; produção −3:25 |
| boost 2× | **+1.471 = 1,28 min** | — | +21%; produção −5:29 |
| boost 3× | +1.373 = 1,20 min | — | — |
| máximo | +1.306 = 1,14 min | — | — |

- **A recarga segura o abuso:** o uso máximo fecha a produção aos 37:30, contra 26:56 sem recarga (§18.4, cenário e). A Esteira sai aos 6:24, longe do piso de 4:15 da §3.
- **VIP extra no bot** (Dt 1/30): 35 sorteados (17 naturais + 18 extras), 34 entraram, 34 levaram, **0 cansaram**; o maior tempo na vaga foi de 103 s.
  - No bot lento (5 passos, 170 VIPs): 0 cansaram, com máximo de **116 s** contra os 120 s de paciência. Ver os riscos abaixo.
- **Um anúncio só no minuto T** (passo travado, ganho nos 10 min seguintes, em minutos da renda daquele momento, médias de 5 passos). O boost não muda contra §18.4: um boost isolado não depende da recarga.

| faixa de T | renda (ouro/min) | VIP extra (pacote 2×) | boost 2× (1 anúncio) | boost 3× (2 anúncios) |
|---|---|---|---|---|
| 5–10 min | 205–402 | 270 (**1,12 min**; rodada 1: 0,46) | 260 (0,82 min) | 310 (0,92 min) |
| 15–30 min | 412–1.073 | 629 (**1,31 min**; rodada 1: 0,34) | 851 (1,53 min) | 1.409 (2,73 min) |
| 35–85 min | 1.214–1.707 | 295 (**0,19 min**; rodada 1: 0,19) | 1.502 (0,95 min) | 2.971 (1,89 min) |

- **Preço implícito do anúncio:**
  - Boost: ~1 min de renda por anúncio no jogo todo.
  - VIP extra: ~1 min até os 30 min. Depois cai para 0,19 min, porque o pacote bate na prateleira (10 unidades = 300–750 ouro, contra renda de 1.200–1.700/min).

**Riscos e propostas NÃO aplicados:**
- **VIP extra no fim do jogo:**
  - O teto da prateleira (10) segura o pacote, e o anúncio vale 0,19 min depois dos 35 min.
  - Proposta: teto do chamado = 2 prateleiras (20). Fora deste pedido.
- **Paciência × pacote 2×:**
  - No bot lento, o VIP de 10 unidades ficou até 116 s na vaga (paciência de 120 s).
  - Um humano mais lento pode perder o VIP extra.
  - Proposta: paciência do VIP = 90 s + 6 s por unidade (108–150 s). Medir no playtest antes.
- **Recarga fora do save:** fechar e reabrir libera um boost. O custo é um reinício do app por anúncio, então o abuso é improvável. Medir no diário.

**Testes, rodada 2:**
- **Suíte: 78/78.** `viewcheck`: 0 erros, 0 avisos, sem tocar na View.
- **Reescritos:**
  - `SummonVip_VipExtra_PacoteDobrado_NaoMexeNoRelogio_SoSemVipAtivoEComONaturalAMaisDe60s`: o relógio fica em 200 s, o pacote é 6 e o natural chega no horário de antes;
  - `Boost_…` ganhou a recarga: 0 durante o boost, 299 s 1 s depois do fim, recusa no 3× e na recarga, libera em 300 s;
  - `Vip_Pacote…` cobre o pacote do chamado (6, 8 e 10 com a Vitrine; 5 sem ela);
  - `Save_…` cobre a recarga fora do save e o pendente com o pacote do chamado.
  - Os testes do VIP natural passaram a sortear pelo relógio (`NaturalVip`), não pelo anúncio.
- **Prova vermelha, rodada 2** (`scratchpad/fs8/red2.py`, 38 mutações, rebuild limpo a cada uma, resumo em `red2_summary.txt`):
  - as 30 da rodada 1 foram reapontadas para o código novo, e S3 virou "reinicia o relógio" (a regra antiga);
  - entram 8 novas;
  - **todas ficaram vermelhas**, e a cópia restaurada (SHA = fontes reais) ficou verde, 7/7.

| mutação nova (rodada 2) | testes vermelhos |
|---|---|
| S3 chamado reinicia o relógio (regra da rodada 1) · S7 chamado zera o relógio natural | SummonVip |
| S5 chamado com o pacote do natural | SummonVip, Vip_Pacote, Save_Relogio |
| S6 chamado sem o teto da prateleira | Vip_Pacote, Save_Relogio |
| C1 sem recarga · C2 recarga contada do início · C3 recarga não anda · C4 recusa no 3× reinicia os 60 s | Boost (as 4) |
| L7 recarga vai no save | Save_Relogio |
| as 30 da rodada 1, reapontadas | vermelhas como em §18.7 |
| restaurado (SHA da cópia = fontes reais `126186909d`) | verde, 7/7 |

### 18.9 Rodada 3: paciência do VIP pelo pacote e teto de 20 no VIP chamado (VIGENTE)

**Decisões do coordenador** (o teste de amanhã é com gente, mais lenta que o bot):
1. **Paciência do VIP** (natural e chamado) = **90 s + 6 s por unidade do pacote** (`Balance.VipPatienceFor`): 3 = 108 s, 10 = 150 s, 20 = 210 s. Antes: 120 s fixos.
   - O VIP que volta do save calcula a paciência pelo que falta do pacote.
2. **Teto do VIP chamado = 20** = 2× o natural, sem corte na prateleira. O natural continua até a prateleira.
   - Como o natural já vem cortado na prateleira (máximo 10), o teto de 20 sai da própria regra. `// ponytail:` no `Sim`: uma prateleira maior que 10 pede o `Math.Min` de volta.
   - O `Load` aceita VIP pendente com até 20 unidades.

**Bot** (médias de 5 passos, 90 min; entre parênteses, a rodada 2):

| cenário | anúncios | receita 90 min | produção completa | VIPs · cansaram · maior tempo na vaga |
|---|---|---|---|---|
| (a) VIP natural | 0 | 103.341 (igual) | 46:41 (igual) | 90 · 0 · 77 s |
| **(b) a + VIP extra a cada 5 min** | 18 | **116.299** (111.949) | **41:47** (43:16) | 175 · 0 · 129 s |
| (c) a + boost 2× com recarga | 15 | 125.404 (igual) | 41:12 (igual) | 90 · 0 · 82 s |
| bot lento, só o natural | 0 | 95.909 (igual) | 50:59 (igual) | 87 · 0 · 98 s |
| **bot lento + VIP extra** | 18 | 110.121 (109.534) | 45:21 (44:40) | 175 · **1** (faltou 1 unidade de um pacote grande, Dt 1/45) · 138 s |

- **VIP natural:** a curva não muda. O bot não chega perto da paciência: o maior tempo na vaga foi de 77 s, contra 108–150 s.
  - `BalanceTests` iguais aos de §18.5: portão de 60 min com 11 levaram, 0 cansaram, 6,0%.
- **VIP extra, por anúncio:**
  - contra (a): **+720 ouro = 0,63 min de renda** (rodada 2: +478 = 0,42; rodada 1: +191 = 0,17);
  - em passo travado, um anúncio no minuto T, ganho nos 10 min seguintes:

| faixa de T | pacote médio | ganho por anúncio (min de renda) | rodada 2 |
|---|---|---|---|
| 5–10 min | 7 | 1,13 | 1,12 |
| 15–30 min | 12 | 1,43 | 1,31 |
| 35–85 min | 18,5 | **0,42** | 0,19 |

- **O boost não muda:** 1,28 min de renda por anúncio no A/B; 0,8–1,5 min isolado.

**Testes, rodada 3:**
- **Suíte: 78/78.** `viewcheck`: 0 erros, 0 avisos.
- **Ajustados:**
  - `Vip_SoDepoisDa1aVenda…`: contrato da paciência, 108 / 150 / 210 s;
  - `Vip_Pacote…`: chamado 6 / 8 / 20 com a Vitrine e 6 / 10 sem ela;
  - `Vip_Cansa…`: cansa aos 108 s;
  - `Save_…`: o pendente do chamado (6) passa da prateleira de 5, a paciência vem do que falta e o lixo é cortado em 20.
- **Prova vermelha** (`scratchpad/fs8/red3.py`, 8 mutações só nas linhas novas, rebuild limpo, resumo em `red3_summary.txt`): **todas vermelhas**; a cópia restaurada (SHA = fontes reais `763de4c361`) ficou verde, 7/7.

| mutação | testes vermelhos |
|---|---|
| P1 paciência fixa em 120 s (regra antiga) | Vip_SoDepoisDa1aVenda, Vip_Cansa |
| P2 sem os 6 s por unidade · P4 3 s por unidade | Vip_SoDepoisDa1aVenda |
| P3 paciência do cliente normal | Vip_SoDepoisDa1aVenda, Vip_Cansa, Save_Relogio, **Bot_60Minutos_Fase2** |
| X1 chamado cortado na prateleira (regra antiga) | Vip_Pacote, Save_Relogio |
| X2 chamado com o pacote do natural | Vip_Pacote, SummonVip, Save_Relogio |
| L8 load corta o pendente na prateleira · L9 load sem o teto de 20 | Save_Relogio |

## 19. v0.5.1: Ajudante 3 parado no balcão (revisão de código, 2026-10-09)

**Achado.** Sem a compra direta (FASE7), o estoque do balcão só desce com cliente na vaga. O Ajudante 3 (papel 2, bancadas → balcão) escolhia a fonte sem olhar a prateleira: pegava espada com a prateleira de espada cheia e ficava parado no balcão, enquanto escudo e ferramenta encalhavam. O revisor mediu 68% do tempo parado no bot de 45 min. Na v0.4.1 a compra direta esvaziava a prateleira, então isso não aparecia.

**Correção.** `Sim.Fits`: o Ajudante 3 só busca na bancada cujo produto ainda cabe (`Stock + na mão < CounterCap`), mesma regra do `JewelerSource`. Vale para o `IsSourceFor` e o `PickSource`.

| Medida (Dt 1/30) | v0.5.0 | v0.5.1 |
|---|---|---|
| Bot 25 min + **parado 15 min**: vendas · clientes perdidos (`Ocioso_15Min_AjudantesVendemSozinhos`) | 80 · 107 | **209 · 59** |
| Bot humano 60 min (`Bot_60Minutos_Fase2`): produção completa · vendas · perdidos | 47:03 · 1 789 · 12 | **46:12 · 1 825 · 7** |
| Ouro/min 25–30 min (`Bot_60Minutos_Fase2`) | 579 | 771 |

A fome da Bigorna 2 (2 481 s → 2 582 s em 60 min) já existia e não muda: com o jogador entregando lingote na Bigorna 1, a 2 fica sem entrada. Fica para o estudo de logística.

Portão novo: `Ocioso_15Min_AjudantesVendemSozinhos` (≥ 150 vendas parado). Prova vermelha: com o `Sim.cs` da 0.5.0 dá 80 e falha.

## 20. v0.6c: Encomendas (FASE9, 2026-10-09)

Regra em `docs/FASE9_ENCOMENDAS.md`. Uma encomenda por vez (vender N de uma linha aberta, em rodízio), sem prazo; a 1ª é 5 espadas, 45 s depois da 1ª venda; a seguinte vem 20 s depois da entrega. N = ~150 s da renda real daquela linha (`OrderSeconds × RateEma / (linhas abertas × preço)`, entre 3 e 30; vigente desde o teste no emulador, ver abaixo). Prêmio = `OrderRewardSeconds` × taxa online (mínimo 25, múltiplo de 5), pago como o baú: **fora do `GoldEarned`** (não infla a taxa online, o cofre offline nem o pacote do VIP).

**Varredura do prêmio** (bot humano 60 min, `Bot_60Minutos_Fase2`, Dt 1/30):

| Prêmio | Entregues | Prêmio % da receita | min/encomenda | Produção completa |
|---|---|---|---|---|
| sem encomenda (0.5.1) | — | — | — | 46:12 |
| 8 s | 17 | 3,6% | 3,5 | 43:03 |
| 10 s, N pela demanda | 16 | 4,2% | 3,7 | 42:51 |
| **10 s, N pela renda (vigente)** | **21** | **5,0%** | **2,8** | **41:31** |
| 15 s | 17 | 6,6% | 3,5 | 41:56 |

O ganho de ritmo vem mais do prêmio mínimo (25) no começo, quando cada 25 de ouro adianta uma compra de 50–110, do que do tamanho do prêmio no fim; por isso 8 s e 10 s dão quase o mesmo tempo. Com 10 s, o 1º terço anda assim (bot humano): Bigorna 2 2:17 → 1:55, Ajudante 1 2:59 → 2:56, Escudos 4:00 → 3:49, Ferramentas 11:40 → 10:46, Ajudante 3 19:01 → 17:19. Todos ainda antes dos tempos do GDD §3 (o bot tem de chegar antes). A produção completa volta para perto da v0.4.1 (43:37). O VIP continua em 5,5% e o luxo não muda o ouro por minuto (-0,7%, `Bot_90Minutos_Luxo`).

Portão: `Bot_60Minutos_Fase2` exige o prêmio entre 3% e 10% da receita. `Bot_90Minutos_Luxo` passa a somar o `OrderGold` na conta exata do saldo.

**N pela renda (2026-10-09, teste no emulador).** Com N pela demanda (`OrderSeconds / ClientInterval`), a 2ª encomenda já pedia **27 espadas por +25** no 3º minuto: no começo o gargalo é a produção do ferreiro, não o cliente. Agora N segue a renda real dividida entre as linhas abertas, então cresce junto com a oficina (5 → ~8 → ~15 espadas no começo; teto de 30 no fim). As encomendas ficam mais curtas (2,8 min), saem mais vezes (21) e o prêmio sobe para 5,0% da receita; produção completa 41:31.

## 21. Experimento B4: moeda física no balcão (2026-10-09, branch `exp/moeda-fisica`, NÃO entra no jogo)

Pergunta do benchmark (B4): moedas empilhando ao lado do balcão e o ferreiro recolhendo ao passar, como em My Perfect Hotel e Pizza Ready. Implementado atrás de uma flag (`Sim.PhysicalCoins`), com a pilha 1,2 m à direita da boca do balcão (e da loja de joias), raio de coleta 0,7 m; só o ferreiro recolhe. O bot vai à pilha quando ela completa a próxima compra (placa ou menu) ou passa de um limite. Bot humano, 60 min, Dt 1/30, v0.6 (com Encomendas):

| Variante | 1ª venda | Melhorias 1/5/10/15 | Produção completa | Andar sem decisão (sem a ida à pilha) | Ida à pilha | Vendas | Ganho |
|---|---|---|---|---|---|---|---|
| ouro direto (jogo atual) | 0:22 | 1:07 / 4:58 / 10:29 / 13:24 | 41:31 | 20% (20%) | 0% | 2 032 | 60 754 |
| moeda física, recolhe com 30 | 0:22 | 1:09 / 4:53 / 10:29 / 13:31 | 42:36 | 30% (5%) | 24% | 1 903 | 58 519 |
| moeda física, recolhe com 60 | 0:22 | 1:08 / 4:51 / 10:18 / 13:34 | 42:16 | 29% (6%) | 22% | 1 880 | 58 542 |
| moeda física, recolhe com 120 | 0:22 | 1:08 / 4:49 / 10:43 / 14:12 | 42:33 | 28% (13%) | 14% | 1 897 | 58 158 |

**Leitura.** A moeda física custa pouco no ritmo: produção completa +45 a +65 s (~2%), vendas −6 a −7%, receita −4%; a 1ª venda não muda. O "andar sem decisão" bruto sobe 8–10 p.p. (passa do portão de +5 p.p. do benchmark), mas porque a ida à pilha conta como "sem decisão": tirando ela, a caminhada à toa **cai** de 20% para 5–13%. Ou seja, o tempo que hoje é deslocamento vazio vira o ritual de recolher, que é o que o gênero faz. O bot não mede o que importa (se recolher é gostoso ou chato); isso é do playtest.

**Decisão do Vinicius.** Se quiser, entra na v0.7 com a pilha física na View (moedas empilhando, ímã ao passar, som), a pilha no save e uma dica "Recolha as moedas"; se não, o branch fica só como registro.

## 22. Bot 2.0 e econsim: 4 perfis, 60 min (2026-10-09, A-CORE-07)

`client/tools/econsim` roda N partidas × perfis do Bot 2.0 (`BotProfile`: Ideal, F2P = o humano de sempre, Slow = reação 1,5 s e 60% de stick do §18.3, AdWatcher = F2P que chama o VIP sempre que pode e a Velocidade 2× quando a recarga deixa) e grava um CSV por minuto. Determinístico; 1 partida de 60 min ≈ 0,7 s; 1.000 partidas em 42 s. Nenhuma regra mudou: com N = 1 o F2P reproduz o portão (produção completa 41:31).

| Perfil | 1ª venda | Fole | Esteira | Corredor | Ajudante 3 | Joalheria | Produção completa | Zona morta até a produção (min; maior sequência) | Ouro depois da produção | Luxos | Minutos sem destino | VIPs (anúncios VIP/boost) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ideal | 0:18 | 0:56 | 6:48 | 15:31 | 18:24 | 24:56 | 43:53 | 23 (4 a partir do min 34) | 28 467 | 1/3 | 0 | 11 (0/0) |
| F2P | 0:22 | 1:07 | 6:58 | 13:42 | 16:51 | 22:53 | **41:31** | 21 (5 a partir do min 36) | 32 449 | 2/3 | 0 | 11 (0/0) |
| Slow | 0:38 | 0:54 | 8:19 | 16:12 | 19:24 | 26:16 | 45:15 | 24 (5 a partir do min 40) | 25 939 | 1/3 | 0 | 11 (0/0) |
| AdWatcher | 0:20 | 0:28 | 3:56 | 7:53 | 10:19 | 12:29 | **24:46** | 8 (3 a partir do min 21) | 87 248 | 3/3 | 14 | 55 (45/10) |

Robustez (250 passos de tick por perfil, 1.000 partidas): produção completa média Ideal 43:47, F2P 43:15 (p50 42:57, p90 45:08 — o 41:31 do portão fica no percentil ~3), Slow 45:20, AdWatcher 24:22; zona morta 22,7–23,7 min sem anúncio × 7,6 com.

**Achados (decisão do Vinicius; nada aplicado):**
1. **"Chamar VIP" não tem recarga.** O AdWatcher chama ~47 VIPs por hora e fecha a produção aos 24 min (−44%), furando os pisos do §3 (Esteira 3:56 < 4:15; Fole 0:28 < 0:45). O Sim não conta o tempo do anúncio (47 × ~30 s ≈ 24 min por hora). Proposta: recarga no VIP chamado (ex.: 5 min, como a Velocidade) ou teto por hora — entra no RemoteConfig da Wave A/E.
2. **Zona morta é metade da curva.** O F2P passa ~23 dos ~43 min sem poder comprar nada (platô aos 15–25 min: renda 400–600/min × custos de 1–3 mil). O Slow, 2× mais lento, chega só +2 min depois: depois dos ~15 min a curva é dos ajudantes, e a ação do jogador pesa pouco. É o espaço das Orders 2.0 e dos Blueprints (Wave B).
3. **Ouro sem destino depois da produção completa** (BACKLOG_V07 item 2): ~29 mil até os 60 min e ~84 mil até os 90; só os 54 mil de luxo absorvem, e o luxo acaba por volta dos 72 min. É o argumento econômico dos distritos (Wave C).

**QA:** 1 em 1.000 partidas travou — o Ideal a 36,36 ticks/s parou aos 31 min em (8,4; 6,1) a caminho do pad do Mineiro, com o `Sim.Steer` alternando entre (0,71; 0,70) e (−0,71; −0,70). Impasse de navegação no núcleo; conferir se os ajudantes podem cair nele (ticket para a raia de núcleo).

### 22.1 A-CORE-03, P14 e D6 (2026-10-12)

Com o `Tuning` padrão o econsim é idêntico ao byte; F2P, Ideal e Slow não mudam (não usam anúncio). **P14:** recarga de 5 min no "Chamar VIP", contada de quando o chamado sai. **D6:** Velocidade e "Chamar VIP" só depois da 1ª melhoria.

| AdWatcher | Fole | Esteira | Corredor | Joalheria | Produção completa | Zona morta | Ouro depois da produção | VIPs (VIP/boost) |
|---|---|---|---|---|---|---|---|---|
| antes (main) | 0:28 | 3:56 | 7:53 | 12:29 | 24:46 | 8 | 89 439 | 58 (48/10) |
| P14 | 0:28 | 5:46 | 11:43 | 18:32 | 34:40 | 15 | 54 126 | 21 (10/10) |
| P14 + D6 | **1:07** | 6:07 | 11:36 | 18:44 | **34:06** | 16 | 54 414 | 21 (10/10) |

Pisos do §3 (média de 250 passos de tick, AdWatcher com D6): Fole 1:02 (piso 0:45, 0% abaixo); 2ª bigorna 1:38 (1:45, 85% abaixo); Ajudante 2:05 (2:15, 100%); Escudos 2:33 (2:45, 100%); Esteira 6:04 (4:15, 0%). Sobram 7–12 s nos três marcos do meio, mas o `Sim` não conta o tempo de assistir (~30 s por anúncio, 2 anúncios até os Escudos): em tempo real os pisos são cumpridos. **Decisão por padrão (V19):** o econsim passa a contar o tempo do anúncio antes de mexer em regra de jogo. O achado 1 do §22 fica resolvido. Os números do Ideal no §22 mudaram com a correção do impasse de navegação (44:05 em vez de 43:53).

### 22.2 V19: econsim em tempo de relógio (2026-10-12)

Cada anúncio assistido gasta 30 s de relógio com o jogo parado (`--anuncio-seg`); `--anuncio-seg 0` reproduz o §22.1 byte a byte. 250 partidas, AdWatcher, pisos do §3 sem → com o tempo do anúncio:

| Marco (piso) | sem o tempo do anúncio | com 30 s por anúncio |
|---|---|---|
| Fole (0:45) | 1:02 (0% abaixo) | 1:02 (0%) |
| 2ª bigorna (1:45) | 1:38 (85%) | **2:38 (0%)** |
| Ajudante (2:15) | 2:05 (100%) | **3:05 (0%)** |
| Escudos (2:45) | 2:33 (100%) | **3:33 (0%)** |
| Esteira (4:15) | 6:04 (0%) | 7:04 (0%) |
| Produção completa | 34:51 | **40:57** de relógio (F2P 43:15) |

18,6 anúncios por hora (9 VIP, 9 boost), ~9 min de anúncio por hora. Em relógio, os anúncios custam ~40 s antes da 2ª bigorna e devolvem 2:18 até a produção completa. **V19 fechado:** nenhuma regra de jogo muda.

## 23. B-CORE-03: Quality, níveis de estação e golpe de mestre (2026-10-13)

Regra em `ECONOMIA_WAVE_B.md` §1 e GDD §9. **Vigente:** preço Normal 1× / Fina 1,5× / Superior 2,5× (`Tuning.QualidadePct` = 100/150/250, inteiro e meio para cima; Masterwork 0 = não vende); +15 de qualidade (`NivelEstacaoQ`) e +5% de velocidade (`NivelEstacaoVelocidade`) por nível de estação acima do 1; golpe de mestre com janela de 0,8 s e recarga de 20 s (`GolpeJanela`/`GolpeRecarga`), só o bot Ideal golpeia. Níveis II e III das 4 famílias (L4–L5 exigem projeto: entram com o B-CORE-07), categoria Meta (fora da produção completa e do teto offline), revelados com as Ferramentas. **Desvio do §1.5:** custo = a metade (bigornas 250/750, escudos 325/950, ferramentas 250/750, joalheria 500/1.500; soma 5.275), porque com os números do §1.5 o retorno medido passou do portão.

### 23.1 econsim, 60 min, 1 partida por perfil (`--perfis Ideal,F2P,Slow,AdWatcher --minutos 60`, anúncio = 30 s)

| perfil | | 1ª venda | Fole | Esteira | Corredor | Ajud.3 | Joalheria | Produção | Fachada | zona morta (min; maior seq.) | pós-produção | luxo | saldo final |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ideal | antes | 0:18 | 0:56 | 6:48 | 15:31 | 18:24 | 24:56 | 44:05 | 51:57 | 24 (5 a partir do min 39) | 28.786 | 1/3 | 14.836 |
| | **depois** | 0:18 | 0:56 | 6:48 | 15:41 | 25:19 | 29:45 | **47:56** | 54:51 | **20** (4 a partir do min 43) | 24.952 | 1/3 | 10.999 |
| F2P | antes | 0:22 | 1:07 | 6:58 | 13:42 | 16:51 | 22:53 | 41:31 | 49:28 | 21 (5 a partir do min 36) | 32.449 | 2/3 | 504 |
| | **depois** | 0:22 | 1:07 | 6:58 | 14:30 | 23:14 | 28:46 | **47:02** | 53:52 | **21** (5 a partir do min 42) | 26.379 | 1/3 | 12.545 |
| Slow | antes | 0:38 | 0:54 | 8:19 | 16:12 | 19:24 | 26:16 | 45:15 | 53:28 | 24 (5 a partir do min 40) | 25.939 | 1/3 | 11.951 |
| | **depois** | 0:38 | 0:54 | 8:19 | 16:14 | 26:02 | 31:04 | **49:35** | 56:43 | **23** (4 a partir do min 45) | 20.912 | 1/3 | 6.919 |
| AdWatcher | antes | 0:22 | 1:07 | 7:07 | 13:36 | 16:21 | 22:14 | 40:06 | 48:04 | 20 (5 a partir do min 35) | 37.003 | 2/3 | 5.058 |
| | **depois** | 0:22 | 1:07 | 7:07 | 13:47 | 22:06 | 26:55 | **44:32** | 51:20 | **18** (5 a partir do min 39) | 33.919 | 2/3 | 1.958 |

| perfil | prêmio de qualidade (% da receita, 60 / 120 min) | golpe de mestre (% da receita; golpes em 60 min) | níveis de estação (comprados; gasto; o 1º) |
|---|---|---|---|
| Ideal | 13,1% / 13,7% | 1,1% (37) | 8/8; 5.275; 11:33 |
| F2P | 12,1% / 12,7% | 0 (não golpeia) | 8/8; 5.275; 10:52 |
| Slow | 12,0% / 12,7% | 0 | 8/8; 5.275; 12:00 |
| AdWatcher | 12,2% / 12,7% | 0 | 8/8; 5.275; 10:07 (relógio) |

Pisos do §3 sem mudança (0% abaixo em todos os perfis). Média de 4 partidas por perfil (passos 30–52,5 ticks/s): produção completa F2P 42:35 → 47:00 (Ideal 43:32 → 47:13, Slow 44:28 → 49:26, AdWatcher 41:47 → 44:41); zona morta 22 → 20,3 (Ideal 23,5 → 20,5, Slow 22,5 → 22,3, AdWatcher 21,8 → 19,5).

### 23.2 Portões (ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido | |
|---|---|---|---|
| Primeiros 10 min | iguais, tick a tick | iguais (`QualidadeTests.Revelacao_PrimeirosDezMinutos_IguaisSemQualidade`: log e save do F2P idênticos com e sem qualidade) | ok |
| Produção completa, F2P | 41–55 min | 47:02 (4 partidas: 47:00) | ok |
| Zona morta até a produção, F2P | não piora (meta da Wave B: ≤ 12) | 21 → 21 (4 partidas: 22 → 20,3) | ok (a meta de 12 é das Orders 2.0 e Blueprints) |
| Prêmio de qualidade | 5–20% aos 60 min; ≤ 35% aos 120 | 12,1% / 12,7% (F2P) | ok |
| Golpe de mestre | ≤ 8% (F2P); ≤ 12% (Perfect) | 0% (F2P não golpeia) / 1,1% (Ideal) | ok |
| Retorno medido dos níveis II–III | ≤ 20 min de renda | 19,5 min (metade do §1.5); 39 min com o §1.5 | ok com o desvio |

**Retorno (A/B, 90 min, 4 partidas, ouro/min na janela 60–90 com a produção completa nas duas):** F2P 1.686 → 1.955 (+270/min, +16%), Ideal 1.727 → 1.993 (+266/min). Os 8 níveis custam 5.275 = 19,5 min do ganho; com o §1.5 (10.550) seriam 39 min.

### 23.3 Varredura que decidiu o preço (4 partidas, F2P)

| Variante | Produção completa | Zona morta | Prêmio de qualidade | Retorno dos níveis |
|---|---|---|---|---|
| sem níveis (= main) | 42:35 | 22 | 0 | — |
| §1.5 (500/1.500…), o bot compra o mais barato (1 partida) | 57:26 | 31 | 10,5% | 39 min |
| §1.5 e o Meta só quando o próximo produtivo está a > 120 s de renda (regra testada e descartada) | 50:01 | 21 | 9,1% | 39 min |
| **metade do §1.5, o mais barato (vigente)** | **47:00** | **20,3** | **12,1%** | **19,5 min** |

Com o §1.5 o bot compra Bigornas II, Ferramentas II e Escudos II logo depois das Ferramentas (são as compras mais baratas) e o resto da cadeia (L3, joalheria) atrasa a Joalheria em 12 min: a produção completa passa do portão de 55 min e a zona morta cresce. A metade passa em todos; a regra "Meta só na espera" ficou de fora (o preço resolve sem regra nova).

### 23.4 BalanceTests (bot humano, Dt 1/30): portões "medido + 30%" passam sem mudar

Corredor 14:30, Joalheria 28:46, 10,2 joias/min, ouro/min 392 → 533 → 1.007 → 1.546 (min 40–45). Joalheiro 34:15, Lupa 41:40, Vitrine de joias 47:02 = produção completa; joias/min 6,8 → 10,2 → 15,2; bancada de joias sem lingote 13%; baús 2:50 / 6:56 / 30:48 / 14:25; encomendas 20 (5,0% da receita); VIP 6,0%. 90 min: Fachada 53:52 (+6,8), Piso 62:43, Joalheria real 73:37; ouro/min 48–90 com × sem luxo 1.947 × 1.963 (−0,8%). Níveis do F2P: Bigornas II 10:52, Ferramentas II 11:22, Escudos II 12:01, Bigornas III 15:58, Ferramentas III 17:22, Escudos III 20:58, Joalheria II 29:23, Joalheria III 30:49.

### 23.5 Testes e prova vermelha

`QualidadeTests` (22): q = 25 → exatamente 1 Fina a cada 4; q = 115 → toda Fina, Superior = ⌊0,15 n⌋, dois Sims iguais; `QualityBreakdown` explica 100% das peças de 40 min do bot (refaz o acumulador peça a peça); fornalha sem qualidade; preço por nível; balcão vende a melhor primeiro; VIP 3× com a qualidade; golpe (só parado na boca, revelação, recarga de 20 s, errar não pune, vale uma peça); ajudante na boca, cofre offline e bots F2P/Slow/AdWatcher nunca golpeiam; Masterwork sem projeto fica Superior; com projeto vai ao Hall, nunca à pilha, ao balcão nem ao `GoldEarned` (conservação de peças em 30 min do Ideal); níveis (custo, +15, +5%, revelação, cartão oculto); Meta fora da produção completa e do teto offline; save antigo (schemas 1–3) = tudo Normal; save/load no meio do acumulador continua igual; lote maior que o total é cortado. `ContentValidatorTests`: preço fora de ordem, Masterwork vendável, família desconhecida, nível fora da cadeia. `mutantes.py`: 15 mutações novas de qualidade (acumulador sem resto e com limiar errado, preço, balcão que vende a pior, golpe pelo ajudante/andando/antes da revelação/sem recarga/no erro, Masterwork sem projeto e no balcão, Meta na produção, nível sem velocidade, save do acumulador, validator), todas mortas.

## 24. E-PLAT-02/03: rewarded por placement e intersticial atrás de flag (2026-10-13)

**Vigente** (`Tuning.Placements`, `Core/Anuncios.cs`): Chamar VIP (pacote 2×, recarga 300 s contada da saída do chamado) e Velocidade (60 s de 2×, recarga 300 s do fim do boost) com os números de antes; Cofre 2× (1 por resgate); bônus da encomenda (+50% do prêmio, 1 por encomenda); trocar encomenda (a próxima do rodízio, mostrada antes, teto 5/dia); Supply Drop (60 s das fornalhas, recarga 600 s). Teto diário total `AnunciosTetoDia` = 30. Intersticial: flag `Interstitial` desligada (V10).

### 24.1 econsim, 60 min, anúncio = 30 s de relógio (V19)

Perfis sem os placements novos (`--perfis Ideal,F2P,Slow,AdWatcher --partidas 1`): **CSV idêntico ao byte** ao da `main` (6ea0201 + #51) — a migração do VIP/Velocidade para o `Tuning.Placements` não mudou nada.

Perfil novo `AdWatcherE` (o AdWatcher assistindo também Supply e bônus, guloso, pelo núcleo), média de 3 partidas:

| perfil | Esteira | Ajud.3 | Joalheria | Produção | Fachada | zona morta | pós-produção | anúncios (VIP/Vel./Supply/bônus) |
|---|---|---|---|---|---|---|---|---|
| AdWatcher | 7:05 | 22:15 | 26:50 | **44:36** | 51:19 | 18,3 | 34.265 | 9/9/0/0 |
| AdWatcherE | 8:04 | 25:42 | 31:18 | **51:36** | 58:07 | 25,7 | 17.755 | 6,7/6/4/13 (teto 30) |

**Achado:** assistir tudo atrasa a produção completa em 7 min. Não é o teto diário (com `AnunciosTetoDia=99` a produção fica em 52:07). O bônus da encomenda (+50% de um prêmio de ~25–60 ouro no começo) vale menos que os 30 s de relógio do anúncio, e é o placement mais visto (13 de 30). Varredura de valores (3 partidas): Supply 180 s → 50:00; bônus +100% → 50:25; os dois → 49:23; Supply 300 s + bônus +100% → 48:29 — nenhum empata com o AdWatcher. **Decisão:** os valores ficam; o jogador vê o valor antes de assistir (chips "+N" e "DOBRAR +N"), e o Orders 2.0 (B-CORE-05) recalibra o bônus com piso em segundos de renda (meta: AdWatcherE não mais lento que o AdWatcher). Pisos do §3: 0% abaixo nos 5 perfis.

## 25. B-CORE-04: WorkerDef, os papéis do Old Forge e o bot que contrata (2026-10-13)

**Vigente** (`Core/Equipe.cs`, `ECONOMIA_WAVE_B.md` §2, `CONTEUDO_V1.md` §6): os 13 workers do Old Forge em dados (`WorkerDefs.All`, IDs `wk_*`). Os 6 ajudantes viram `WorkerDef` Porter (o upgrade de sempre contrata; nível 1 = o ajudante de antes, bit a bit) e sobem até o nível 5 por 250/400/640/1.025 (+6% de passo por nível, carga +1 nos níveis 3 e 5). Equipe nova, contratada em ouro no cartão do menu, depois das Ferramentas: oficiais Bruna/Tomé/Iara (1.200; exigem Bigornas/Escudos/Ferramentas II) e Joaquim (2.400; Joalheria II) com q +15 (+25 no nível 5) e +5% de velocidade (+3%/nível) na família; Celeste (balconista, 1.500; Balcão 6) com paciência +10% (+5%/nível) e atendimento 0,40 → 0,32 s no balcão principal; Baltazar (almoxarife, 2.500; Renome 1) com saídas 4 → 6 (fornalha) e 3 → 5 (bancadas) e estoque do balcão +1 por produto a cada nível acima do 1; Otávio (mestre de obras, 4.000; Renome 2) com q +10 e +5% de velocidade (+2%/nível) em toda bancada. Níveis e contratação são categoria **Meta** (V14): fora da produção completa e do teto offline. **Desvio do §2.3 (revisar):** o almoxarife ganha +1 de estoque em todo nível (o §2.3 dava +2 só nos níveis 3 e 5, e os níveis 2 e 4 não fariam nada); mesmo total no 3 (+2) e no 5 (+4).

**Bot (`Bot.WorkerSegundos`, padrão 0):** o mais barato à venda, só **depois da produção completa** (antes, só o que custa até N s da renda online; 0 = nenhum), e nunca à frente de melhoria (menu ou pad) igual ou mais barata. A varredura (§25.2) mostrou que todo worker comprado antes da produção completa a atrasa mais do que devolve.

### 25.1 Perfil sem os workers novos = `main` ao byte

`econsim --tuning Workers=false` (flag da Equipe desligada desde o início) contra o econsim da `main` (d0d78f0, #51–#53), 5 perfis: **CSV idêntico ao byte** em 60 min × 1 partida (md5 `56884df1…`), 90 min × 4 (`becb80df…`) e 120 min × 4 (`0ec710e4…`). Com a flag ligada e o bot que nunca compra worker (`--worker-seg -1`) também (60 × 1). No `coretests`: `EquipeTests.Bot_SemOsWorkersNovos_IdenticoAoDeAntes_E_ComEles_SoDepoisDaProducao` (log minuto a minuto e save iguais em 50 min; com o bot padrão, a curva produtiva e os níveis de estação no mesmo tick). Todos os `BalanceTests` passam sem mudar os portões.

### 25.2 Varredura do `WorkerSegundos` (90 min, 4 partidas, média; F2P / AdWatcherE)

| bot | 1ª compra de worker | Produção completa | Fachada | receita 0–60 | receita 0–90 | renda/min 80–90 | saldo aos 60 |
|---|---|---|---|---|---|---|---|
| sem workers (`main`) | — | 47:04 / 50:43 | 54:01 / 57:22 | 60.294 / 52.533 | 118.954 / 111.473 | 1.943 / 1.962 | 12.719 / 5.398 |
| **0 (vigente)** | 47:09 / 50:49 | **47:04 / 50:43** | 73:43 / 76:59 | **61.332 / 53.067** | **133.364 / 124.070** | 2.464 / 2.474 | **726 / 374** |
| 10 s | 35:07 (F2P) | 47:59 | 73:58 | 60.600 / 52.814 | 132.514 / 123.511 | 2.436 / 2.477 | 840 / 272 |
| 20 s | 30:10 | 50:26 | 74:33 | 59.196 / 48.197 | 131.236 / 117.676 | 2.462 / 2.430 | 837 / 563 |
| 30 s | 15:50 | 52:38 | 75:23 | 57.430 / 46.757 | 128.971 / 116.692 | 2.445 / 2.455 | 320 / 630 |

Quanto antes o bot contrata, menor a receita aos 60 e aos 90 min: os 250–1.200 de um nível ou de um oficial na zona morta empurram a Joalheria e o Joalheiro mais do que o +6%/+15 devolve. Com 0 a curva produtiva é a de antes e a receita é a maior.

### 25.3 Vigente, 90 min, 4 partidas (`main` → branch)

| perfil | Produção completa | Fachada | pós-produção | saldo final | min sem destino | prêmio de qualidade | workers (contratados; níveis; gasto) |
|---|---|---|---|---|---|---|---|
| Ideal | 47:13 = | 54:04 → 73:17 | 89.481 → 57.890 | 35.506 → 3.916 | 17,8 → 2 | 13,5% → 18,0% | 6; 48; 47.040 |
| F2P | 47:04 = | 54:01 → 73:43 | 88.150 → 56.430 | 34.247 → 5.512 | 17,3 → 1,3 | 12,6% → 17,1% | 6; 48; 47.040 |
| Slow | 49:26 = | 56:36 → 76:35 | 82.100 → 49.061 | 28.172 → 17.134 | 14,3 → 0 | 12,6% → 16,8% | 6; 48; 47.040 |
| AdWatcher | 44:39 = | 51:17 → 69:33 | 98.899 → 69.715 | 44.965 → 15.781 | 21 → 6,3 | 12,5% → 17,4% | 6; 48; 47.040 |
| AdWatcherE | 50:43 = | 57:22 → 76:59 | 81.188 → 47.664 | 27.250 → 15.725 | 13,5 → 0 | 12,5% → 16,8% | 6; 48; 47.040 |

Aos 120 min (4 partidas): prêmio de qualidade F2P 12,7% → 18,6%; minutos sem destino 47,3 → 31,3 (o bot fecha os 6 contratáveis no nível 5 por volta dos 88 min; o Otávio pede Renome 2, que o bot sem metas não alcança em 2 h). Bot humano (`BalanceTests`, Dt 1/30), 90 min: 6 contratados, 47.040 gastos, renda 80–90 2.429/min, saldo aos 60 min 186, receita 133.088 × 118.605 sem workers. 60 min × 1 partida: marcos, zona morta e pisos idênticos à `main`; workers F2P 4 contratados, 37 níveis, 27.705 gastos, 1ª compra 47:07.

### 25.4 Portões (ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido (F2P) | |
|---|---|---|---|
| Primeiros 10 min | iguais, tick a tick | iguais (worker só depois das Ferramentas e, no bot, da produção completa) | ok |
| Produção completa | 41–55 min | 47:04, igual à `main` | ok |
| Zona morta até a produção | não piora (meta ≤ 12) | 20,5 = | ok (a meta de 12 fica para Orders 2.0 e Blueprints: worker antes da produção não a reduz, §25.2) |
| Ouro sem destino aos 60 min | ≤ 10 mil | 12.719 → **726** | **ok** (antes falhava) |
| Prêmio de qualidade | 5–20% aos 60–90; ≤ 35% aos 120 | 17,1% (90) / 18,6% (120) | ok |
| Pisos do §3 | 0% abaixo | 0% abaixo nos 5 perfis | ok |

**Retorno (achado, revisar):** os 47.040 dos 6 contratáveis + níveis sobem a renda de 1.943 para 2.464/min (+27%, janela 80–90): ~90 min para pagar, contra ≤ 40 min do portão dos níveis de estação L4–L5. Como ralo depois da produção completa funciona (receita 0–90 +12%, saldo aos 60 de 12,7 mil para 0,7 mil), mas pelo preço o worker não compensa antes dela. Knob se a Wave B quiser worker na zona morta: metade do preço dos níveis (como a V26 fez nas estações) ou efeito maior no nível 1; nada aplicado. A Fachada vai de 54:01 para 73:43 porque os workers (todos < 14 mil) disputam o ouro com o luxo; o `BalanceTests.Bot_90Minutos_Luxo` passou a rodar com `Workers = false` (isola o luxo sobre a curva produtiva; os portões dele seguem os mesmos).

### 25.5 Testes e prova vermelha

`EquipeTests` (20): golden dos 13 IDs; migração (mesmo upgrade, rota e ordem; nível 1 = passo e carga de antes; do save na mesma ordem); efeito neutro sem ninguém; revelação nas Ferramentas e flag desligada; compra (cobra na hora, sem ouro e no máximo nada muda, 250/400/640/1.025); requisitos por upgrade e Renome 1/2; cada papel (passo e carga por nível, oficial só na família e +25 no nível 5, mestre de obras em toda bancada e nunca na fornalha, balconista: paciência ×1,1/×1,3 e 5 → 7 vendas em 2 s, almoxarife: saídas 6/5 e estoque por nível); save de ida e volta (pilha de 6 na fornalha volta inteira), save antigo sem `wkn=` (schema 3 e o `wk=` do schema 2), lixo no `wkn=`; metas (`worker_hired` por papel, `worker_upgrade` por nível, ralo, retroativo); validator (ID, prefixo, 3 traços, custo que não sobe ou fora do múltiplo de 5, distrito/família/upgrade que o build não tem, papel da Wave C, 2º oficial da família, upgrade que já contrata, `wk_` do Renome sem worker); textos do cartão nos 3 idiomas em todo nível; `-equipe`; bot (idêntico sem os novos; 90 min com renda maior e saldo aos 60 ≤ 10 mil). `mutantes.py`: 12 mutações novas (`wk-*`: passo no nível 1, carga, oficial fora da família, mestre de obras na fornalha, atendimento sem balconista, compra sem cobrar, Renome ignorado, revelação, save sem nível, load cortando a pilha, validator de custo, bot antes da produção), todas mortas.

## 26. Blueprints (B-CORE-07) (2026-10-13)

Regra em `GDD.md` §12, `ECONOMIA_WAVE_B.md` §4/§8.5 e `CONTEUDO_V1.md` §9. **Vigente:** 19 `BlueprintDef` do Old Forge (os 18 do §4 + `bp_rec_espada_longa`; 3 essenciais), posse (nunca saldo) no `Sim.Projetos`. Fontes ativas: marco de venda (com "Fina ou melhor"), produção completa, Renome e conquista resgatada; as filas de encomenda (B-CORE-05), contrato, evento, Pass e loja são ganchos. Níveis IV–V das 4 famílias (Meta, menu) exigem o projeto de mestre; a bigorna longa (pad Meta, 6.000, exige o Martelo veloz) e a espada longa (2 lingotes, 7,5 s, 40 de ouro, família das bigornas) exigem `bp_rec_espada_longa`; técnica = q +20 (`Tuning.TecnicaQ`) na família. O bot não mudou: ele compra pelo menu/pad o que o projeto libera, como qualquer melhoria.

**Desvios do catálogo (decididos por padrão, revisar):** (1) `bp_bigorna_mestra` e `bp_bancada_mestra` ganham o marco "produção completa" como 2ª fonte grátis (o padrão dos `bp_mestra_*` dos distritos 2–6): sem ele os níveis IV–V esperavam as filas do B-CORE-05 e nenhum essencial era alcançável no F2P. (2) Custo dos IV–V = **1/5 do §1.5 no IV e 1/6 no V** (bigornas 800/1.500, escudos 1.000/1.835, ferramentas 800/1.500, joalheria 1.600/3.000; soma 12.035), não a metade da V26: com a metade (34.000) o retorno medido foi 104 min (portão ≤ 40). O IV continua acima do III em toda família.

"Antes" = a `main` com os workers (67d4f4e, #54: o bot contrata e sobe worker só depois da produção completa); "depois" = este branch sobre ela.

### 26.1 econsim, 60 min, 1 partida por perfil (anúncio = 30 s), antes → depois

| perfil | Produção | zona morta (min) | prêmio de qualidade | projetos aos 60 | IV–V aos 60 (1º) | bigorna longa | worker (1ª compra; gasto aos 60) |
|---|---|---|---|---|---|---|---|
| Ideal | 47:56 → **50:28** | 20 → 22 | 13,4% → 16,9% | 10/19 | 3/8 (54:24) | 45:35 | 48:07 → 50:30; 25.830 → 19.390 |
| F2P | 47:02 → **49:55** | 21 → 22 | 12,4% → 16,3% | 9/19 | 3/8 (53:48) | 45:05 | 47:07 → 50:04; 27.705 → 19.390 |
| Slow | 49:35 → **52:29** | 23 → 25 | 12,1% → 15,9% | 9/19 | 3/8 (56:30) | 47:27 | 49:47 → 52:37; 21.370 → 13.890 |
| AdWatcher | 44:32 → **46:38** | 18 → 19 | 13,0% → 16,8% | 9/19 | 5/8 (50:34) | 42:29 | 44:35 → 46:47; 36.295 → 25.830 |

Pisos do §3 iguais (0% abaixo em todos os perfis; os primeiros 10 min não mudam, nada de projeto antes das Ferramentas). Depois da produção completa os níveis IV–V e os workers disputam o mesmo ouro (o bot compra o mais barato).

### 26.2 4 partidas por perfil, 120 min (passos 30–52,5 ticks/s)

| perfil | Produção | Fachada | zona morta | ouro/min 111–120 (renda final) | saldo aos 120 | min sem destino | prêmio de qualidade 120 | espada longa (% da receita) |
|---|---|---|---|---|---|---|---|---|
| Ideal | 47:13 → 49:59 | 73:17 → 77:20 | 20,5 → 22,5 | 2.509 → **3.083** | 83.523 → 97.512 | 32 → 30,5 | 19,7% → 29,3% | 6,9% |
| F2P | 47:04 → **49:28** | 73:43 → 77:36 | 20,5 → 21,8 | 2.448 → **2.959** (+21%) | 81.130 → 94.161 | 31,3 → 30,3 | 18,6% → 28,4% | 6,4% |
| Slow | 49:26 → 52:10 | 76:35 → 80:43 | 22,3 → 24,3 | 2.440 → 3.018 | 73.371 → 85.557 | 28,8 → 27,8 | 18,5% → 28,2% | 7,0% |
| AdWatcher | 44:39 → 46:53 | 69:33 → 73:48 | 19,5 → 20,8 | 2.493 → 3.094 | 96.709 → 113.182 | 36,3 → 34,3 | 18,8% → 28,7% | 7,3% |

Os 6 workers e os 48 níveis (47.040) entram nos dois; os IV–V 8/8 (12.035) no "depois", a partir de 53:30 (F2P).

Projetos aos 120 min (os 4 perfis): 10 de 19 — técnica das ferramentas (150 vendidas) e das joias (50), os dois de mestre (produção completa), receita da espada longa (300 espadas), o visual da espada (500), e os projetos das Masterworks com marco (Primeira Lâmina, Broquel, Martelo do Fundador, Anel do Mercador). Os 9 que faltam só têm fila de encomenda/Royal (B-CORE-05), Renome 3 (o F2P chega lá em 1,5–3 h) ou evento. Nenhum veio de fonte paga.

**Por que a produção completa atrasou 2,4 min:** o marco das 300 espadas (~40 min) abre a bigorna longa; o bot compra a de 6.000 aos ~45 min (é o pad mais barato a venda), antes da Vitrine de joias (9.000), o último produtivo. Ela fica fora da produção completa (Meta), mas o ouro gasto nela atrasa o resto; a zona morta sobe 1,3 min pelo mesmo motivo. Fica dentro do portão (41–55 min). Se incomodar: o knob é `Tuning.BigornaLongaCusto` ou pedir a Vitrine de joias como requisito do pad.

### 26.3 Retorno dos níveis IV–V (A/B: IV–V a 10⁹, que o bot nunca compra, × vigente; ouro/min, 4 partidas)

| janela | F2P sem → com | ganho | retorno (12.035 / ganho) | Ideal sem → com | ganho | retorno |
|---|---|---|---|---|---|---|
| min 91–120 (com os workers, vigente) | 2.684 → 3.030 | +346/min | **34,8 min** | 2.720 → 3.093 | +373/min | 32,3 min |
| min 91–120 (`main` antes do #54, sem workers) | 2.130 → 2.439 | +309/min | 38,9 min | 2.167 → 2.511 | +344/min | 35,0 min |

Com os workers a janela 61–90 ainda pega os IV–V sendo comprados (o 1º aos 53:30, o último perto dos 70 min), então o retorno se lê na 91–120.

**Varredura que decidiu o preço** (medida antes do #54, sem workers; F2P, janela 91–120; o ganho dos IV–V fica em ~310–335/min qualquer que seja o preço: só o retorno muda):

| IV–V | soma | retorno | Fachada F2P | saldo aos 120 |
|---|---|---|---|---|
| metade do §1.5 (V26) | 34.000 | 104 min | 69:26 | 89.840 |
| 1/4 do §1.5 | 17.000 | 53 min | 62:05 | 108.129 |
| 1/5 do §1.5 | 13.600 | 41 min (no limite) | 60:40 | 112.509 |
| **1/5 no IV, 1/6 no V (vigente)** | **12.035** | **36–39 min** | 60:13 | 113.042 |

Abaixo de 1/5 o IV fica mais barato que o III (bigornas 665 < 750). O ralo dos IV–V é pequeno perto do luxo (54 mil): o ouro sem destino depois da produção é dos distritos (Wave C).

### 26.4 Portões (ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido | |
|---|---|---|---|
| Primeiros 10 min | iguais | iguais (pisos do §3 sem mudança; `QualidadeTests.Revelacao_PrimeirosDezMinutos_IguaisSemQualidade`) | ok |
| Produção completa, F2P | 41–55 min | 49:55 (4 partidas: 49:28) | ok (+2,4 min, §26.2) |
| Zona morta, F2P | não piora (meta ≤ 12 é das Orders 2.0) | 21 → 22 (4 partidas: 20,5 → 21,8) | piora 1,3 min pela bigorna longa (§26.2) |
| Prêmio de qualidade | 5–20% aos 60; ≤ 35% aos 120 | 16,3% / 28,4% | ok |
| Golpe de mestre | ≤ 8% F2P; ≤ 12% Perfect | 0% / 1,1% | ok |
| Retorno L4–L5 | ≤ 40 min | 34,8 min (F2P; Ideal 32,3) | ok com o desvio de preço |
| AdWatcher, produção completa | ≥ 0,75 × F2P | 46:38 × 49:55 | ok |
| Produção completa nunca presa a Blueprint | nenhum produtivo exige projeto; essenciais grátis no F2P | `ProjetosTests.Bot_F2P_ChegaAosEssenciais_EUsaOsProjetos` (os 3 aos 60 min, nada pago) | ok |

`BalanceTests` (bot humano, Dt 1/30, portões "medido + 30%") passam sem mudar nenhum número.

**Robustez (1.000 partidas, 250 passos de tick por perfil, 90 min, com os workers):** todas chegam à produção completa e à Fachada, sem impasse com o corpo novo da bigorna longa (5,0; 1,5). Produção completa média F2P 49:38 (Ideal 50:42, Slow 52:42, AdWatcher 46:50); os 10 projetos de marco/produção em todas; IV–V 8/8 a partir de 53:34 (F2P); espada longa 6,8% da receita aos 90 min.

### 26.5 Testes e prova vermelha

`ProjetosTests` (12): catálogo (19, 3 essenciais, fila #1–#7 sem buraco, nada pago); posse idempotente (o mesmo marco em dois Ticks, o gancho e a transação não duplicam; o save grava ID e origem, nunca saldo); ganchos (só fonte listada, pago só com ID de transação, a mesma transação não paga 2x); fontes (marco de venda, Fina ou melhor, produção completa, Renome 3, conquista só resgatada); filas (próximo mostrado antes, sem sorteio, o que veio por outra fonte sai da fila, esgota); desbloqueio dos IV–V (menu, compra direta e cartão) e da bigorna longa (pad, linha, venda a 40, nível pela família); técnica (+20 na família toda, 1 Fina a cada 5, determinística); save (ida e volta com origem e transação, save antigo sem `bp=` vazio e o retroativo dos marcos no 1º Tick, ID desconhecido mantido); validator (essencial só pago, essencial só de contrato, só evento, Pass premium, ID repetido/fora do formato, marco inalcançável, posição de fila repetida, Renome fora da tabela, conquista e distrito inexistentes, alvo inexistente, upgrade com projeto errado e nível IV sem projeto); bot F2P com os 3 essenciais e o nível IV aos 60 min; textos do painel nos 3 idiomas. `mutantes.py`: 17 mutações novas `bp-*` (posse que duplica, transação que repete, pago sem transação, fonte não listada, Fina que conta Normal, conquista sem resgate, save sem transação, load que perde a posse, menu/compra/pad sem projeto, técnica zerada ou pela estação, nível pela estação, validator do essencial pago, contrato como permanente, mestre sem a produção completa) e a `eco-preco-ferramenta` atualizada para o preço de 7 itens: 18 mortas, 0 vivas.
## 27. D-CORE-03/04: Eventos e Pass (2026-10-13)

**Vigente** (`Core/LiveOpsDefs.cs`, `Core/LiveOps.cs`): os 10 `EventDef` do `LIVEOPS_V1.md` §2.4 (8 do calendário + 2 reservas), 6 dias às 13:00 UTC a partir do T0 = 2026-11-02 (padrão do time, revisar: DECISÃO "datas" do D-PROD-01), um evento por vez; trilha comum (135 s de ouro, Renome +8, 500 XP do Passe, 2 cosméticos; nos Hunts o projeto da Masterwork com fallback de 60 s); modificadores aplicados pelo `RemoteConfig.Valores` (as mesmas travas: evento nunca fura guardrail) e devolvidos no fim. Pass S1/S2: 30 níveis × 600 XP, 35 dias emendados, só a trilha free (ouro 1.170 s, Renome +44, 6 cosméticos); premium só no dado (V20). XP do Passe = `Metas.PassXp` (diárias 100, semanais 350, conquistas; retroativo 1×) + encomenda entregue 10 (teto 100 por dia local) + XP das trilhas dos eventos. Ouro de limiar e de nível fora do `GoldEarned`.

Rodam neste build: **Forge Frenzy, Armorer's Challenge, Forge Frenzy-B, Blacksmith Festival e Royal Week-B**. Esperam o sistema deles (o calendário pula a semana, nunca é erro): Royal Week e os dois Hunts (`order_complete` Royal/Quality do B-CORE-05; `QualidadeEvento` não é knob; `bp_mw_primeira_lamina` do B-CORE-07), Rare Ore Rush (Bulk; `GrabTime` ainda é const do `Balance`) e Merchant Caravan (Bulk/Mixed).

### 27.1 Evento desligado

econsim padrão (5 perfis × 60 min, anúncio = 30 s): **CSV idêntico ao byte** ao da `main` (67d4f4e, com o B-CORE-04) e a mesma saída. O bot sem relógio não roda o LiveOps (save sem `ev.*`/`ps.*`); com evento ativo, os 10 primeiros minutos do F2P são iguais tick a tick (as Ferramentas são requisito de todo evento; `LiveOpsTests`).

### 27.2 Evento ativo (`econsim --evento <id>`, 1 partida por perfil, comparada com a mesma partida sem o evento)

| evento | modificadores | F2P entra | renda/min do F2P depois da entrada (sem → com) | Δ F2P | Δ nos 5 perfis | pontos do F2P (missões) |
|---|---|---|---|---|---|---|
| Forge Frenzy | `ClientInterval` ×0,8; `PatienceBase` ×1,15 | 29:11 (10 encomendas) | 1.661 → 1.756 | +5,7% | +0,8 a +5,7% | 560 (7/8) |
| Armorer's Challenge | `Price[escudo]` ×1,2; `ShieldTimeMul` ×0,9 | 14:26 (Renome 1) | 1.282 → 1.457 | +13,7% | +10,4 a +16,3% | 610 (6/7) |
| Forge Frenzy-B | os do Forge Frenzy | 28:47 (Joalheria) | 1.632 → 1.683 | +3,1% | +1,8 a +4,0% | 640 (7/8) |
| Blacksmith Festival | `FurnaceTime0..2` ×0,85; `ClientInterval` ×0,9 | 29:11 | 1.661 → 1.765 | +6,2% | +6,2 a +12,9% | 560 (6/8) |
| Royal Week-B | `VipMin`/`VipMax` ×0,75 | 14:26 | 1.282 → 1.370 | +6,9% | −1,9 a +8,9% | 640 (7/8) |

- Portão "ouro/min do F2P ≤ +15% durante o evento" (`LIVEOPS_V1.md` §6): **passa** nos 5 (o maior é o Armorer's, +13,7% no F2P). **Revisar:** no Armorer's o Ideal (+16,3%) e o AdWatcherE (+16,0%) passam de 15% (preço do escudo ×1,2 e bancada ×0,9 somam; os workers do B-CORE-04, comprados depois da produção completa, aumentam o efeito); knob se o portão valer para todo perfil: `Price[escudo]` ×1,15. Cofre aberto durante o evento paga igual (teste).
- **Achado:** 30 min depois da entrada o F2P já fez 560–640 dos 700 pontos das missões (só o "jogar em 3 dias" segura). Sem a encomenda Event (+20 por entrega, gancho do Orders 2.0) a trilha para em 700 de 1.000: os limiares 850 (Renome +5) e 1.000 (XP 200 + cosmético principal) ficam fora de alcance até o B-CORE-05, e o portão "trilha de evento fecha no dia 4–5" ainda não se mede. Se o Orders 2.0 confirmar o ritmo, o knob é o alvo das missões de volume (o meio de jogo vende ~34/min).
- Ouro dos limiares numa sessão: 1.050–3.600 (são segundos de renda: com a taxa do meio de jogo, 135 s valem ~2,2 min de venda).

### 27.3 Pass: o F2P fecha a trilha free?

Conta estática do `LIVEOPS_V1.md` §5.4 com o conteúdo real (`LiveOpsDefs.XpDiaF2P`: o menor XP das diárias e das semanais dos `MetasDefs`, encomendas no teto do dia, XP das trilhas da temporada / 35), conferida pelo validator e pelo `LiveOpsTests.Fechamento_*`:

| temporada | desenho (8 encomendas/dia, os 5 eventos) | hoje (encomendas medidas no econsim: 6,0–7,0 por dia de 20 min; só os eventos que rodam) |
|---|---|---|
| S1 Royal Commission | 651 XP/dia → **dia 27,6** | 603–613 XP/dia → dia 29,4–29,9 |
| S2 Ember Festival | 651 XP/dia → **dia 27,6** | 589–599 XP/dia → dia 30,1–30,6 |

- Portão "fecha entre o dia 26 e o 30": o desenho passa nas duas; hoje a S1 passa e a **S2 passa do 30** porque só 2 dos 5 eventos dela rodam (volta ao 27,6 quando o Orders 2.0 liberar Rare Ore Rush, Merchant Caravan e Ember Crown). Knob se preciso: `PassDef.XpNivel`.
- Numa sessão de 60 min com evento, o Passe ganha 250 XP (100 de encomendas, no teto, + 150 da trilha do evento).
- Orçamento de ouro de LiveOps por dia (validator): metas 115,7 s + Pass 33,4 s + o evento mais caro 27,9 s (Hunt com o fallback do projeto) = **177 s ≤ 180** (14,7% dos 1.200 s de venda de um dia de 20 min; portão ≤ 15%).

### 27.4 Testes e prova vermelha

`LiveOpsTests` (20): calendário às 13:00 UTC e temporadas emendadas; os 10 eventos com os 9 campos, missões somando 700 e as reservas prontas; **evento novo só com dados** (um `EventDef` injetado na folga do dia 6 passa no validator, entra, aplica o knob, paga os limiares e devolve o knob); requisito (Ferramentas, Renome, encomendas) e os 10 primeiros minutos do bot iguais tick a tick com um evento forte ativo; modificadores pelo RemoteConfig sobre o config remoto e de volta no fim; evento com knob proibido preso pelas travas (offline ≤ 7.200 s) e recusado pelo validator; cofre igual; missão e limiar 1× com save/load, matar antes de gravar e reset do evento; relógio para trás; compra/contratação que completa sozinha (`worker_any` com o B-CORE-04); qualidade e golpe de mestre de verdade; Pass (retroativo 1×, espera até a temporada, teto de 100 XP/dia, RESGATAR 1×, nível 30, premium nunca paga, fim de temporada paga o alcançado); save do bot sem `ev.*`/`ps.*`; fechamento 26–30; 29 defeitos injetados no validator; textos nos 3 idiomas. `mutantes.py`: 12 mutações novas (`liveops-*`), todas mortas; o trecho do `qual-nivel-sem-velocidade` foi atualizado para o `Equipe.Ritmo` do #54 (estava desatualizado na `main`) e segue morto.

## 28. B-CORE-05: Orders 2.0 (2026-10-13)

Regra em `ECONOMIA_WAVE_B.md` §3 e GDD §10; catálogo no `CONTEUDO_V1.md` §7. **Vigente** (`Core/Encomendas.cs`, `Core/EncomendasDefs.cs`): 8 tipos em dados (`OrderDef`, IDs `ord_*`): Common (= a encomenda de antes), Bulk (2 linhas e 5 entregues), Quality (linha que já faz Fina; variante Superior no Renome 3), Rush (8 entregues; prazo de 240 s **só de bônus**: no prazo o prêmio dobra e dá +1 de Renome, fora dele paga o normal), Mixed (3 linhas; 2 produtos, 3 com 4 linhas), Royal (Renome 2, Fina; o próximo projeto da fila Royal do `Projetos`), Blueprint (Renome 1 e projeto pendente na fila de encomenda do `Projetos`, B-CORE-07: `ProximoDaFila` no cartão, `GanhaDaFila` com o ID de transação na entrega) e Event (vaga própria enquanto o evento do `LiveOps` está ativo, a `OrderDef` do `EventDef.Encomenda`; só pontos na trilha do evento, §28.6). Os 5 fregueses do Old Forge (FS-3) entram na 7ª, 14ª… Common (N e item fixos, +1 coração). Rodízio do Old Forge `Common, Common, Bulk, Quality, Common, Mixed, Rush, Blueprint, Common, Royal` na posição `Abertas % 10` (abertas + entregues + trocadas), tipo travado vira Common; produto = `linhas[idx % linhas]`, pulando o que já está noutra vaga. Vagas 1 → 2 (Renome 1) → 3 (Renome 3). **Tudo o que é novo só depois das Ferramentas** (tipos, vagas extras, fregueses): até lá o jogo é o de antes, tick a tick. Prêmio em segundos da taxa online × a escala dos knobs da Common (`OrderSeconds / 150`, `OrderRewardSeconds / 10`), múltiplo de 5, fora do `GoldEarned`; Renome pelo tipo (+1 a +10). Uma peça vendida conta para uma vaga só (a mais antiga que a aceita). "Bônus da encomenda" (anúncio) = máx(50% do prêmio, **60 s de renda**) (`Tuning.EncomendaBonusMinSeg`).

**Decisões e desvios (revisar):**
1. **Rush com penalidade? Não:** só bônus (recomendação do ticket); o validator recusa bônus sem prazo e não existe campo de penalidade.
2. **Prêmio de Bulk, Quality, Mixed e Royal = 70% dos segundos do catálogo** (32/18/30/25/45 → 22/13/21/18/32; mínimos iguais; os fregueses Quality/Bulk acompanham). Com o catálogo, o ouro de encomendas foi a 13,6% da venda aos 60 min no F2P (portão 4–12%); com 80%, 11,9% no F2P e 12,6% no Ideal; com 70%, 11,3% (4 partidas, antes do #55; com as filas do `Projetos` ligadas, 9,3%). Common, Rush e Blueprint ficam como o catálogo (a Common não pode mudar: é a dos 10 primeiros minutos).
3. **N de Quality e Royal × fração que a automação já faz no nível mínimo** (q / 100; Fina com Bigornas II = 0,15): o "alvo em s de renda" conta só as peças que servem. Sem isso a 1ª Quality pedia 12 Finas (~80 espadas, ~10 min com a vaga presa); com isso, o mínimo de 3.
4. **A variante Superior do Quality só abre com a automação fazendo Superior (q > 100);** o F2P não golpeia, e uma encomenda que só fecha com o golpe prenderia a vaga ("nunca pune"). Abre sozinha quando nível de estação, técnica e oficial passarem de q 100 (níveis IV–V do #55).
5. **Limiares 2–4 do Renome mantidos (70/180/400):** com o Orders 2.0 eles caem dentro dos portões (abaixo), então a recalibração é "não mexer". Ressalva: o bot não resgata metas (semanais +5, conquistas +5/+10, login +2/+3); o jogador real chega um pouco antes.

### 28.1 econsim antes × depois, 3 partidas × 360 min (`--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 360 --partidas 3`, anúncio = 30 s)

Antes = `main` 5a06394 (com o #54 WorkerDef e o #55 Blueprints; o econsim com a tabela de Renome deste branch); depois = este branch com a `main` mesclada e as filas de encomenda ligadas ao `Projetos`. Renome 1/2/3/4 = relógio em que o nível chegou (h:mm; "—" = não chegou em 6 h).

| perfil | | Produção | zona morta | Renome 1 | Renome 2 | Renome 3 | Renome 4 | Renome em 6 h | encomendas 60 min / 6 h | ouro de encomendas (% da venda, 60 min / 6 h) | renda/min 0–60 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Ideal | antes | 50:43 | 23,3 | 0:15 | 2:07 | 4:35 | — | 243 | 18 / 98 | 4,1% / 4,5% | 1.031 |
| | **depois** | **48:20** | 20,3 | 0:15 | **0:56** | **2:03** | **3:33** | 762 | 30 / 209 | 9,3% / 15,3% | 1.100 |
| F2P | antes | 49:18 | 22,3 | 0:14 | 2:19 | 5:10 | — | 211 | 20 / 101 | 4,4% / 4,5% | 1.071 |
| | **depois** | **48:28** | 21,7 | 0:14 | **1:03** | **2:12** | **3:46** | 711 | 29 / 201 | 9,2% / 14,6% | 1.084 |
| Slow | antes | 52:17 | 24,3 | 0:15 | 2:24 | 5:17 | — | 207 | 19 / 99 | 3,9% / 4,5% | 964 |
| | **depois** | **51:13** | 22,7 | 0:15 | **1:00** | **2:10** | **3:41** | 736 | 29,7 / 212 | 8,4% / 15,5% | 984 |
| AdWatcher | antes | 46:30 | 19,3 | 0:14 | 2:19 | 5:09 | — | 214 | 18,3 / 102 | 4,4% / 4,6% | 1.212 |
| | **depois** | **44:29** | 18 | 0:13 | **0:58** | **2:08** | **3:44** | 709 | 29,3 / 194 | 9,3% / 14,1% | 1.275 |
| AdWatcherE | antes | 54:05 | 26,7 | 0:19 | 2:33 | 5:25 | — | 202 | 15,7 / 96 | 6,4% / 4,6% | 880 |
| | **depois** | **45:01** | 21 | 0:16 | **1:05** | **2:05** | **3:39** | 725 | 24,3 / 202 | 26,2%* / 16,0% | 1.107 |

\* o AdWatcherE soma no prêmio o bônus que ele assiste (14–16 dos 30 anúncios do dia). Entregues por tipo em 6 h (F2P): Common 95, Bulk 22, Quality 24, Rush 19, Mixed 19, Royal 16, Blueprint 5 (a fila de encomenda do Old Forge acaba: o resto dos projetos dela sai pelos marcos; Event só com evento ativo, §28.6). Pisos do §3: 0% abaixo nos 5 perfis, iguais ao antes. Pós-produção F2P em 6 h: 969 mil → 1,12 mi.

### 28.2 8 partidas × 60 min (`--minutos 60 --partidas 8`; antes = `main` 5a06394)

| perfil | Produção (antes → depois) | zona morta | ouro de encomendas aos 60 min | encomendas em 60 min | saldo aos 60 min |
|---|---|---|---|---|---|
| Ideal | 50:29 → **48:44** | 23,3 → 21,3 | 4,4% → 9,2% | 19,1 → 29,4 | 567 → 807 |
| F2P | 50:04 → **47:14** | 22,4 → 19,6 | 4,3% → 9,3% | 18,8 → 30,8 | 651 → 808 |
| Slow | 52:28 → **50:42** | 24,4 → 22,6 | 3,8% → 9,0% | 18,5 → 30,3 | 516 → 444 |
| AdWatcher | 46:45 → **44:35** | 20,4 → 18 | 4,4% → 8,9% | 18,1 → 29 | 635 → 1.160 |
| AdWatcherE | 53:36 → **43:19** | 25,8 → 18 | 5,9% → 28,4%* | 15,3 → 25 | 659 → 924 |

Prêmio de qualidade aos 60 min (F2P) 16,2% → 18,5% (a técnica das encomendas Blueprint soma q +20).

### 28.3 AdWatcher × AdWatcherE (achado do §24)

8 partidas × 60 min, Orders 2.0, varrendo o piso do bônus (`--tuning "EncomendaBonusMinSeg=N"`), medido sobre a `main` 432e638 (antes do #54 e do #55); o AdWatcher não assiste o bônus (42:36 em todas):

| piso do bônus | AdWatcherE, produção | diferença para o AdWatcher |
|---|---|---|
| antes (Orders 1, só 50% do prêmio) | 50:50 | +6:16 (mais lento) |
| 0 s (Orders 2.0, só 50% do prêmio) | 49:12 | +6:36 |
| 45 s | 43:18 | +0:42 |
| **60 s (vigente)** | **41:11** | **−1:25** |
| 75 s | 39:21 | −3:15 |
| 90 s | 37:49 | −4:47 |

60 s é o menor piso em que o AdWatcherE não fica mais lento que o AdWatcher (o anúncio custa 30 s de relógio e devolve 60 s de renda). Com a `main` 5a06394 mesclada: 43:19 × 44:35 (−1:16; §28.2). Pisos do §3 do AdWatcherE: 0% abaixo.

### 28.4 Portões (ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido (F2P) | |
|---|---|---|---|
| Primeiros 10 min | iguais, tick a tick | iguais até as Ferramentas (10:30): golden `EncomendasTests.PrimeirosDezMinutos_IguaisAosDeAntes_GoldenDoMain`, gerado pelo código de antes (mesmos eventos de encomenda e compra no mesmo tick, mesmo ouro, vendas, posição e Renome) | ok |
| Produção completa | 41–55 min | 47:14 (8 partidas; antes 50:04) | ok |
| Zona morta até a produção | ≤ 12 min (meta) | 22,4 → 19,6 | melhora; a meta é da Wave C |
| Ouro sem destino aos 60 min | ≤ 10 mil | 808 | ok |
| Ouro de Orders | 4–12% da receita aos 60 min | 9,3% (Ideal 9,2%, Slow 9,0%) | ok com o desvio 2 |
| Encomendas entregues em 60 min | ≥ 25 | 30,8 (antes 18,8) | ok |
| Prêmio de qualidade aos 60 min | 5–20% | 18,5% | ok |
| Renome 1 / 2 | 12–25 min / 40–75 min | 0:14 / 0:48–1:03 (antes 2:19 h) | ok |
| Renome 3 / 4 | 1,5–3 h / 3–6 h | 2:12 / 3:46 (antes 5:10 / não chegava em 6 h) | ok |
| AdWatcher × F2P | ≥ 0,75 × F2P | 44:35 × 47:14 | ok |
| AdWatcherE × AdWatcher | não mais lento (§24) | 43:19 × 44:35 | ok |
| Pisos do §3 | 0% abaixo | 0% abaixo nos 5 perfis | ok |

`BalanceTests.Bot_60Minutos_Fase2` passou a cobrar o portão da Wave B (4–12% e ≥ 25 entregues; era 3–10% com 1 vaga e 1 tipo).

### 28.5 Testes e prova vermelha

`EncomendasTests` (19): um por tipo (Common igual à de antes; Bulk só com 2 linhas e 5 entregues; Quality só com linha que faz Fina, só a Fina conta, N pela fração; Rush no prazo 2× e +1, fora dele o normal e a vaga continua; Mixed 2 e 3 produtos, cada peça no seu; Royal no Renome 2 com o projeto da fila Royal do `Projetos` pelo ID de transação; Blueprint no Renome 1 só com projeto pendente na fila do `Projetos`, Common com a fila esgotada; Event em vaga própria no Forge Frenzy do `LiveOps`, +20 pontos na trilha, sai quando o evento acaba) e o freguês (7ª Common, o próximo que dá, coração no save); rodízio sem sorteio (golden do ciclo e de 20 posições; dois bots iguais abrem a mesma sequência); vagas 1/2/3 pelo Renome e só depois das Ferramentas, enchendo pelo relógio sem repetir produto; 1 peça para 1 vaga (a mais antiga que aceita); prêmio 1× (entregar 2× paga 1×, nem depois de save/load); save (3 vagas com Mixed, Rush no prazo, bônus e projeto; ida e volta idêntica; espelho `ord=`), save do schema 3 de antes (gerado pelo código da `main`: vira Common igual na posição entregues + trocadas e o rodízio segue), lixo e ID desconhecido; os 10 primeiros minutos do bot iguais ao golden da `main`; o bot F2P entrega os 7 tipos que abrem sem evento em 100 min; validator (item inexistente, Masterwork como alvo, bônus sem prazo, ID fora do formato, Mixed de 1 produto, Event com ouro, Blueprint sem fila, fila inexistente, distrito que o build não tem, N invertido, prêmio fora do múltiplo de 5, ID repetido, ciclo sem Common, com Event ou com tipo sem `OrderDef`). `AnunciosTests`: bônus com piso de 60 s; trocar com 2 vagas (só a principal, no lugar dela). `LocTests`: título e barra do cartão nos 3 idiomas. `mutantes.py`: 22 mutações novas do Orders 2.0 (`enc-*`, com `enc-bp-nao-ganha` da fila do `Projetos`, e as `ord-*`/`save-ord-*`/`ad-troca-proxima` reescritas no código novo), todas mortas; com `ad-paga-2x` e duas do Renome, 27/27 mortas.

### 28.6 Orders 2.0 no LiveOps (#56): Royal Week e Merchant Caravan rodam; a trilha de evento fecha no dia 4–5

Ligações: a entrega chama `LiveOps.OnEncomenda(tipo)` (gatilho `order_complete` com filtro `common|bulk|quality|mixed|rush|royal|event|blueprint`, agora em `LiveOpsDefs.Gatilhos`; `Futuros` vazio) e, na encomenda Event, `LiveOps.PontosEncomenda` (pontos no contador do evento, somados em `PontosDe`, no save pelo `ev.c`); o XP do Passe continua só no `Ev.OrderDone` (10 por entrega, teto de 100 por dia; a Event é uma entrega, não dobra). `Requer` do evento confere o ID no `OrderDefs`/`ProjetosDefs`; o validator cobra que `EventDef.Encomenda` seja a `OrderDef` Event daquele evento. Rodam **7 dos 10**: Royal Week e Merchant Caravan entraram; os 2 Hunts esperavam o knob `QualidadeEvento` (entraram com o B-CORE-08, §30.6) e o Rare Ore Rush o `GrabTime` (consts, não knobs).

**Trilha de evento (portão `LIVEOPS_V1` §2.2/§6: fecha no dia 4–5 de 6, nunca numa sessão).** Com +20 por encomenda Event o F2P chegava a 995 de 1.000 numa sessão de 60 min do Merchant Caravan. Correção:
- **Toda trilha de 3+ dias tem a missão "jogue em 3 dias do evento"** (`play_day` 3, 100 pontos, como a do Forge Frenzy): entrou em Royal Week, Armorer's Challenge, Forge Frenzy-B, Blacksmith Festival, Merchant Caravan e Royal Week-B (e nos 2 Hunts, que ainda não rodam), no lugar da `upgrade_buy` nos eventos de 8 missões e tirando os 100 das missões de volume nos de 7 (a soma segue 700). O validator recusa evento de 3+ dias sem ela.
- **Encomenda Event: 20 → 15 pontos; 10 no Merchant Caravan** (recalibrado na §30.6 para o F2P com 3 vagas: Caravan 12, Royal Week-B 15, Hunts 17/20; o único em que toda missão sai de encomenda e venda). **Desvio do pedido (10 em todos), revisar:** com 10, o F2P de referência só fecha no dia 6 (Forge Frenzy, Forge Frenzy-B, Blacksmith Festival, Royal Week-B) ou não fecha (Royal Week 470 pontos em 60 min e 990 no dia 6, Armorer's Challenge); com 15 em todos, o Merchant Caravan fecha no dia 3.
- **As reservas ganharam a encomenda Event** (`ord_ev_forge_frenzy_b`, `ord_ev_royal_week_b`): sem ela a trilha parava em 700.

`LiveOpsTests.TrilhaDeEvento_*` (bot F2P, 30 Hz; o jogador já jogou 20 min por dia desde o T0, até 2 h; a Royal Week é o D0 e começa do zero): pontos em **60 min seguidos** no 1º dia e o dia em que o **F2P de referência** (`LIVEOPS_V1` §1: 2 sessões de 10 min por dia, cofre entre elas, sem anúncio e sem golpe de mestre) fecha a trilha:

| evento | 60 min seguidos | referência fecha no dia |
|---|---|---|
| Royal Week | 590 | 5 |
| Forge Frenzy | 815 | 4 |
| Armorer's Challenge | 795 | 5 |
| Forge Frenzy-B | 795 | 4 |
| Blacksmith Festival | 745 | 4 |
| Merchant Caravan | 790 | 4 |
| Royal Week-B | 795 | 4 |

Ressalva: o dia de fechamento mexe ±1–2 com a taxa de passo do bot (a 10 Hz o Merchant Caravan fecha no dia 6); o teste fixa 30 Hz, a do `BalanceTests`.

`econsim --evento <id> --minutos 60 --partidas 4` (5 perfis; comparado com a mesma partida sem o evento), depois da correção:

| evento | F2P entra | renda/min do F2P depois da entrada (sem → com) | Δ F2P | Δ nos 5 perfis | pontos do F2P (missões) | XP do Passe na sessão (F2P) |
|---|---|---|---|---|---|---|
| Royal Week (`VipMin`/`VipMax` ×0,75) | 14:11 | 1.441 → 1.474 | +2,3% | +0,4 a +7,2% | 568 (4/8) | 250 |
| Merchant Caravan (`OrderRewardSeconds` ×1,25) | 14:11 | 1.441 → 1.483 | +2,9% | +2,5 a +7,5% | 710 (6/8) | 325 |

- Portão "ouro/min do F2P ≤ +15% durante o evento": **passa** nos dois (e nos 5 perfis). Antes da correção eram +8,2% e +5,0%: o ouro dos limiares que a trilha deixou de pagar na sessão comprava melhoria mais cedo.
- Pass com o Orders 2.0 (eventos prontos, encomendas medidas): S1 657 XP/dia → **dia 27,4**; S2 643 → **dia 28,0** (antes 29,9 e 30,6). Não mudou com a correção: a trilha de cada evento ainda dá 500 XP, só mais tarde.
- Testes: `LiveOpsTests.OrdersDois_RoyalWeekRoda_*` (a Common não conta como Royal, 2 Royal fecham a `royal2`, a Event abre sozinha com a `OrderDef` do evento e dá +15, XP do Passe 10 por entrega com o teto contando a Event, pontos no save), `OrdersDois_MerchantCaravanRoda_*` (Bulk/Mixed por tipo, `order_done` conta todas, prêmio ×1,25 pelo RemoteConfig e de volta no fim), `TrilhaDeEvento_*` (a tabela acima), a lista dos eventos prontos e a Royal Week no calendário; o validator com a `Encomenda` trocada e sem a `play_day`. `mutantes.py`: `enc-evento-sem-pontos`, `enc-evento-sem-tipo`, `enc-evento-pontos-20` e `liveops-sem-play-day` mortas (e a `liveops-teto-encomenda` também pelo teste novo).

### 28.7 Vaga sem cartão repetido (coordenador, 2026-10-13)

Achado nas fotos (`-buy 37 -renome 180 -warmup 600 -encomendas`): com uma Mixed ocupando escudo+joia+ferramenta e uma Common de espada, a Common nova repetia a espada ("Venda 30 espadas" duas vezes na tela). Agora, com todas as linhas ocupadas, a vaga repete primeiro um item que só está dentro de uma Mixed. Teste `TodasAsLinhasOcupadas_NaoRepeteOCartaoDeUmItemSo` (vermelho no código anterior: t=465, vagas 6 e 8). Com a variedade nova o jogador de referência fechava o Royal Week-B no dia 3: a encomenda Event dele foi para 10 pontos (como o Merchant Caravan). F2P, 8 × 60 min: produção completa 47:14 (igual), zona morta 19,6.

## 29. E-PLAT-04/05: compras in-app (núcleo + loja falsa) e o Pacote do Mestre (2026-10-13)

**Quem não compra: nada muda.** O `Sim` só ganhou o `Compras` (ledger e posse re-derivada do recibo) e as linhas `iap.*` no save, que só aparecem com compra; o bot não compra. econsim `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30`: **CSV idêntico ao byte** ao da `main` ef8b4c2 (#56; SHA-256 `5d0ffc58…fac0ed4` nos dois); o resumo difere só na coluna s/partida (tempo de máquina). O mesmo deu idêntico contra a d0d78f0 e a 67d4f4e, a cada merge.

**Quem compra** (Pacote do Mestre, `docs/IAP.md` §1: Velocidade, Lingotes e Trocar sem o vídeo, com as mesmas recargas e teto): flag nova `--pacote 1` no econsim (Velocidade e Supply sem os 30 s do vídeo; VIP e bônus seguem pagando o tempo). Média de 3 partidas, anúncio = 30 s, sobre a ef8b4c2:

| perfil | Esteira | Joalheria | Produção | 1a compra de worker | pisos do §3 abaixo |
|---|---|---|---|---|---|
| AdWatcher | 7:05 | 26:50 | 46:30 | 46:40 | 0% |
| AdWatcher com o pacote | 6:35 | 24:50 | **43:00** | 43:10 | 0% (2a bigorna 2:09, Ajudante 2:36, Escudos 3:04) |
| AdWatcherE | 8:04 | 31:18 | 54:05 | 54:16 | 0% |
| AdWatcherE com o pacote | 7:04 | 27:58 | **49:05** | 49:16 | 0% |

O pacote poupa o tempo dos vídeos (3,5–5 min na produção completa de 60 min) e não dá recurso que o F2P com anúncio não tenha: mesmos placements, recompensas, recarga e teto diário (30); não liga o premium do Passe. Nenhum piso do §3 cai (o anúncio só abre depois da 1a melhoria, D6). Referência: AdWatcher com `--anuncio-seg 0` (VIP **e** Velocidade de graça, limite superior que o pacote não alcança) fecha a produção em 39:30 e fica até 11 s abaixo de 3 pisos (2a bigorna 1:39, Ajudante 2:06, Escudos 2:34); por isso o VIP ficou de fora do `sem_video` (revisar com a P3).

## 30. B-CORE-08: Masterworks, Hall e Collection (2026-10-13)

**Vigente** (`Core/MasterworksDefs.cs`, `Core/Masterworks.cs`): as 10 `MasterworkDef` do Old Forge (`ECONOMIA_WAVE_B.md` §6 = `CONTEUDO_V1.md` §10.1, sem mudança; as 50 dos distritos 2–6 entram com a Wave C, no mesmo formato). Golpe de mestre numa peça que **já sairia Superior**, com o projeto e a condição da 1ª pendente da família, vira a Masterwork: vai ao Hall 1× (nunca pilha, balcão nem `GoldEarned`), credita o Renome dela pelo ID `mw:{id}`; a 2ª vez sai Superior e vende. Ajudante e cofre offline nunca fazem (não golpeiam). Família de produto no `ItemDef.Family` (a espada longa é da espada). Hall completo numa família = +5% no preço de venda dela (FS-6). Collection derivada dos contadores (produto × Normal/Fina/Superior, Masterworks, equipe contratada/nível máximo, projetos): 9 marcos do Old Forge pagam Renome (e 1 cosmético) 1× por `col:{id}`, com retroativo; abre com o 1º projeto de Masterwork depois das Ferramentas. Os Hunts dão o projeto da Masterwork pelo `Projetos.Ganha` (origem `Event`; quem já tem recebe os 60 s). Bot Ideal: com Masterwork pendente e q ≥ 100 na bancada, só golpeia a peça que já sairia Superior (`ECONOMIA_WAVE_B.md` §1.4).

**Decididos por padrão (revisar):** (1) marcos da Collection = os degraus do Old Forge do `ECONOMIA_WAVE_B.md` §7 (50%/100% de cada aba: produtos 8/15, equipe 13/26, projetos 10/19) mais os do `CONTEUDO_V1.md` §10.3 que já cabem (`col_mw_5`, `col_mw_10`, `col_workers_25` = 15 de 58); os de % do catálogo da v1.0 acima disso esperam o conteúdo (o validator recusa marco inalcançável); `col_mw_1` ("abre o Hall") não é marco, é o próprio Hall. Abas Clientes (FS-3) e Distritos (Charter) esperam o sistema delas. (2) Marco paga **Renome e cosmético, nunca ouro** (GDD §13, `CONTEUDO_V1.md` §10.3), embora o ticket aceitasse ouro em s de renda; nenhum marco do catálogo dá Blueprint. (3) +5% da família completa como bônus permanente do Hall (GDD §13/FS-6). (4) Condição `OrdersOfType` lê o contador `order_complete` do Orders 2.0 (na ordem do `OrderType` dele): até o merge, a Tenaz Eterna não sai. (5) Produto × qualidade sai do `solds=` e do `bpq=`; o único contador novo é Superior por item (`colq=`): save antigo conta a Superior de antes como Fina.

"Antes" = `main` (f578df6, #57); "depois" = este branch. "Sem o Renome da coleção" = este branch com os 9 marcos a Renome 0 (A/B só no econsim, não entra no jogo).

### 30.1 60 min (5 perfis × 1 partida, anúncio = 30 s)

**CSV idêntico ao byte** ao da `main`: até 60 min a Collection paga 1–3 marcos (Renome 10–30), mas nada muda no ouro nem nas compras (os 10 primeiros minutos e os pisos do §3 iguais).

### 30.2 120 min, 4 partidas por perfil, antes → depois

| perfil | Produção | Fachada | zona morta | ouro/min 111–120 | prêmio de qualidade aos 120 | golpe de mestre |
|---|---|---|---|---|---|---|
| Ideal | 49:59 (igual) | 77:20 → 81:17 | 22,5 (igual) | 3.083 → 3.292 (+6,8%) | 29,3% → 31,5% | 1,1% → 0,9% |
| F2P | 49:28 (igual) | 77:36 → 81:24 | 21,8 (igual) | 2.959 → 3.235 (+9,3%) | 28,4% → 30,8% | 0% |
| Slow | 52:10 (igual) | 80:43 → 84:43 | 24,3 (igual) | 3.017 → 3.318 (+10,0%) | 28,2% → 30,5% | 0% |
| AdWatcher | 46:53 (igual) | 73:48 → 77:32 | 20,8 (igual) | 3.093 → 3.367 (+8,9%) | 28,7% → 31,2% | 0% |

### 30.3 240 min, 4 partidas por perfil: Hall, Collection e Renome (sem o Renome da coleção → depois)

| perfil | Masterworks no Hall (a 1ª) | marcos pagos (Renome) | Renome aos 240 | Renome 2 | Renome 3 |
|---|---|---|---|---|---|
| Ideal | 2 (1:50:10 → **1:23:45**): Martelo do Fundador, Anel do Mercador | 5,3 (65) | 211 → 285 | 1:49:46 → 0:58:53 | 3:17:33 → 1:47:58 |
| F2P | 0 (não golpeia) | 5 (60) | 135 → 212 | 2:18:40 → **0:59:24** | — → **3:11:21** |
| Slow | 0 | 5 (60) | 130 → 206 | 2:25:22 → 1:02:24 | — → 3:17:15 |
| AdWatcher | 0 | 5 (60) | 133 → 211 | 2:23:11 → 1:01:35 | — → 3:11:12 |

- **A Masterwork em si não mexe na renda (~0):** com os marcos a Renome 0, F2P, Slow e AdWatcher saem **idênticos ao byte** à `main` em 240 min; o Ideal fica +0,45% (o Renome +55 das 2 obras adianta o Renome 2). As 2 obras tiram 2 peças Superiores da venda (~0,04% da receita) e a espera pela Superior baixa o golpe de 1,1% para 0,9% da receita aos 120.
- **O Renome da coleção sim:** os marcos (+60 no F2P) trazem o Renome 2 de 2:18 para **0:59** (portão 40–75 min) e o mestre de obras (Otávio, Renome 2) é contratado mais cedo: ouro/min +7–10% na janela 91–120 e a Fachada +3,8 min (o ouro vai ao Otávio e aos níveis dele antes do luxo). Renda 1–240: +1,5% (Ideal) a +2,4% (F2P).
- Só 2 das 10 saem na `main`: espada e escudo não chegam a q 100 sem a técnica (filas de encomenda do Orders 2.0: bigorna 60 + oficial 25 + mestre 10 = 95), então a Primeira Lâmina (o "preview do Hall" do D0) espera o `bp_tecnica_espada` (fila Blueprint #1, Renome 1); Sunblade e Ember Crown esperam a fila Royal ou o Hunt; Tenaz Eterna, as encomendas Mixed; Escudo do Brasão, o Tomé nível 5 e o projeto do Renome 3.

### 30.4 Portões (ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido | |
|---|---|---|---|
| 1ª Masterwork, Perfect | ≤ 2,5 h | 1:23:45 (4 partidas) | ok |
| 1ª Masterwork, F2P | ≤ 4 h | o bot F2P não golpeia (B-CORE-03: só o Ideal) | **não se mede (revisar:** perfil F2P que acerta 1 em 2) |
| Renome 2, F2P | 40–75 min | 0:59:24 (antes 2:18:40) | ok (passou a valer) |
| Renome 3, F2P | 1,5–3 h | 3:11 (antes > 4 h) | 11 min acima; o Renome por tipo do Orders 2.0 traz para dentro |
| Prêmio de qualidade aos 120 | ≤ 35% | 30,8% (F2P) | ok |
| Golpe de mestre | ≤ 12% Perfect | 0,9% | ok |
| Produção completa, zona morta, 10 primeiros minutos | iguais | iguais | ok |
| Masterwork nunca vendida nem no `GoldEarned` | — | `MasterworksTests` (golpe, bot de 45 min: receita = soma das vendas) | ok |

Knob se o Renome correr demais depois do Orders 2.0: o Renome dos marcos em `MasterworksDefs.Marcos` (10/20 por aba até o merge; **5/10 desde a §30.6**).

### 30.5 Testes e prova vermelha

`MasterworksTests` (16): catálogo das 10 com pedestal, projeto, família e Renome do §6 e o validator verde; o golpe numa Superior com o projeto vai ao Hall 1× (Renome +25 por `mw:`, nunca pilha, balcão nem `GoldEarned`; a 2ª vez sai Superior); sem projeto ou sem condição fica Superior, com a condição (Bigornas V) sai; a espada longa faz a obra da espada; ajudantes e cofre offline nunca fazem; família completa = +5% só na família (meio para cima), Masterwork a 0; save (ida e volta com a data, save estável, depois do load não sai de novo, sem `hall=` o ledger repara, save antigo vazio, ID desconhecido mantido); Collection (produto × qualidade pelos contadores, fechada antes das Ferramentas ou sem projeto, os marcos pagam 1× com retroativo, save/load não paga de novo, `colq=`); venda real conta a Superior, abas Equipe e Projetos; Hunts (o projeto não trava mais o calendário; o Hunt só roda com o projeto da Primeira Lâmina, dá o da Sunblade com origem `Event` sem o fallback e paga 60 s a quem já tinha); conquistas de Masterwork; 27 defeitos injetados no validator (ID, família de outro distrito, projeto errado ou só de evento, pedestal, condição de anúncio/IAP/placement, alvo e nível inexistentes, Superior inalcançável, marco inalcançável/sem prêmio/repetido); bot (o Ideal registra, F2P e Slow nunca; o Ideal espera a peça Superior); textos nos 3 idiomas. `MetasTests.Eventos_TodoGatilhoContaNoJogo` faz uma obra-prima de verdade. `mutantes.py`: 16 mutações novas `mw-*`/`col-*` e a `qual-masterwork-sem-projeto` com o gancho ligado: **17 mortas, 0 vivas**.

### 30.6 Merge com o Orders 2.0 (#60, 2026-10-14): a Collection acelera o Renome e a trilha de evento depende das vagas

"Antes" = `main` ddff35d (#60); "depois" = este branch mesclado. Mesmas condições da §28.1 (`--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 360 --partidas 3 --anuncio-seg 30`) e da §30.1 (`--minutos 60 --partidas 1`). O que o merge ligou: a condição `OrdersOfType` lê o `OrderType` de verdade (`MasterworksDefs.Tipo`, `Enum.TryParse`; a lista provisória saiu), `Requer` com `OrderDef` só precisa existir (`LiveOps.Requisitos`; o Blueprint continua requisito do jogador), e **`QualidadeEvento` virou knob** (`Tuning.QualidadeEvento[item]`, padrão 0, trava 0–25 no RemoteConfig; padrão 0 só sai do 0 com trava absoluta, senão o `Prende` prendia o +15 do Hunt em 0): os 2 Hunts entraram no calendário (**9 dos 10** rodam; o Rare Ore Rush espera o `GrabTime`).

**CSV de 60 min:** F2P, Slow, AdWatcher e AdWatcherE **idênticos ao byte** aos da `main`; o Ideal muda só nos minutos 57–60 (a 1ª Masterwork aos ~57 min: Renome +25, nada no ouro).

**Renome (360 min × 3, relógio em que o nível chegou).** O F2P paga os 7 marcos do Old Forge **todos antes das 2 h** (produtos 50% 31:35, projetos 50% 49:42, equipe 50% 56:50, `col_workers_25` 58:38, produtos 100% 1:12, equipe 100% 1:15, projetos 100% 1:57) e o Renome dos marcos realimenta o do Orders 2.0 (Renome 2 mais cedo → Royal e 2ª vaga mais cedo → mais encomendas → mais Renome): com os degraus do `ECONOMIA_WAVE_B.md` §7 a 10/20 (+100 ao F2P) o Renome 3 e o 4 furavam o piso. Knob da §30.4 aplicado: **degraus do §7 pela metade (5/10; `col_workers_25` segue o +10 do catálogo)** — desvio do §7, revisar.

| perfil | Renome 1 / 2 / 3 / 4, `main` (§28.1) | degraus 10/20 | **degraus 5/10 (vigente)** | Collection ao F2P em 6 h | Hall (Ideal) |
|---|---|---|---|---|---|
| Ideal | 0:15 / 0:56 / 2:03 / 3:33 | 0:16 / 0:48 / 1:16 / 1:37 | 0:16 / 0:51 / 1:20 / 1:46 | 9 marcos (+125) | 10 obras, a 1ª 1:14:36; Renome das obras 320 |
| F2P | 0:14 / 1:03 / 2:12 / 3:46 | 0:14 / 0:49 / **1:22** / **2:48** | 0:14 / 0:51 / **1:41** / **3:11** | 7 marcos (+55) | — |
| Slow | 0:15 / 1:00 / 2:10 / 3:41 | 0:16 / 0:49 / 1:21 / 2:45 | 0:16 / 0:51 / 1:41 / 3:09 | 7 (+55) | — |
| AdWatcher | 0:13 / 0:58 / 2:08 / 3:44 | 0:14 / 0:44 / 1:18 / 2:47 | 0:14 / 0:49 / 1:38 / 3:09 | 7 (+55) | — |
| AdWatcherE | 0:16 / 1:05 / 2:05 / 3:39 | 0:17 / 0:50 / 1:28 / 2:51 | 0:17 / 0:51 / 1:44 / 3:16 | 6,7 (+52) | — |

Portões (ECONOMIA_WAVE_B §10, F2P): Renome 2 40–75 min → **0:51** ok; Renome 3 1,5–3 h → **1:41** ok (10/20 dava 1:22); Renome 4 3–6 h → **3:11** ok (10/20 dava 2:48). Produção completa e zona morta iguais à §28.1; ouro de encomendas 9,9% / 15,3% e entregas 29,3 / 207 (§28.1: 9,2% / 14,6%; 29 / 201 — a 3ª vaga mais cedo), renda/min 0–60 igual (1.085).

**Trilha de evento (`LiveOpsTests.TrilhaDeEvento_*`, o F2P de referência da §28.6).** Com o merge, 4 trilhas saíam do portão "dia 4–5" (Caravan nunca fechava). Sonda por missão: em todas as trilhas **as 7 missões fecham no dia 3**; o que resta até os 1.000 é só a encomenda Event, e ela rende **3–5 entregas por dia com 3 vagas** (`Encomendas.OnVenda`: a peça vai à vaga **mais antiga** que a aceita; a Event reabre no fim da lista e passa fome atrás das 3 normais) contra o dobro com 2. A `main` calibrou os pontos (§28.6/§28.7) com o save de referência a 175 de Renome, **5 abaixo do limiar 180**: 2 vagas nos dias 1–2 e 3 dali em diante. Qualquer fonte nova de Renome (a Collection; o jogador real que resgata metas, §28 ressalva 5) vira a referência em "3 vagas desde o 1º dia" e a trilha escorrega 1–3 dias. Recalibração dos pontos da encomenda Event para a referência a 3 vagas (`EncomendasDefs`): **Merchant Caravan 10 → 12** (15 fecha no dia 3; 10 nunca), **Royal Week-B 10 → 15** (10 não fecha em 6 dias), **Hunt da Sunblade 17** (20 fecha no dia 3; 15 no dia 6) e **Hunt da Ember Crown 20** (15 não fecha: os 100 pontos de `master_strike` são inalcançáveis sem golpe); os outros 5 ficam em 15.

| evento | `main` (§28.6, 2 vagas) | merge com 10/20 | **vigente** (60 min seguidos / dia; pontos da Event) |
|---|---|---|---|
| Royal Week | 590 / 5 | 605 / 6 | 605 / **5** (15) |
| Forge Frenzy | 815 / 4 | 800 / 4 | 785 / **4** (15) |
| Armorer's Challenge | 795 / 5 | 750 / 6 | 810 / **5** (15) |
| Hunt da Sunblade | não rodava | 665 / 6 | 840 / **4** (17) |
| Forge Frenzy-B | 795 / 4 | 780 / 4 | 765 / **4** (15) |
| Blacksmith Festival | 745 / 4 | 745 / 4 | 700 / **5** (15) |
| Merchant Caravan | 790 / 4 | 660 / 7 | 780 / **5** (12) |
| Hunt da Ember Crown | não rodava | 785 / 5 | 820 / **4** (20) |
| Royal Week-B | 795 / 4 | 720 / 6 | 810 / **4** (15) |

Ressalvas: (1) o dia de fechamento continua caótico (±1 com a taxa de passo e com qualquer mudança de estado: a 5/10 com os pontos antigos a Royal Week-B dava 7 e com 10/20 dava 6); (2) **a trilha é função de `VagasMax`** — decisão para o coordenador: deixar como está (calibrado para 3 vagas, o estado do jogador real a partir de ~1:40) ou dar à encomenda Event a conta própria na venda (`OnVenda` contar a peça para a Event além da vaga normal), o que a tira das vagas mas muda o portão "nunca numa sessão" e exige reafinar os pontos dos 9 eventos; (3) a Collection do Old Forge é conteúdo de 0–2 h: qualquer Renome dela cai na janela Renome 2 → 3, então o knob certo é o tamanho dela, não a hora.

**Testes e mutações do merge:** `MasterworksTests.Hunts_RodamNoCalendario_*` (os 2 Hunts sem `Falta`; `OrderDef` no `Requer` só precisa existir; o Hunt entra só com o projeto da Primeira Lâmina, +15 só na espada e o knob volta no fim; RemoteConfig prende `QualidadeEvento` em 25) e `Colecao_*` (+5/+10); `LiveOpsTests` (9 eventos rodam; a tabela acima); `EncomendasTests.Bot_F2P_EntregaOsTiposNovos` soma a Collection e o Hall na conta do Renome. `mutantes.py`: `mw-hunt-sem-requisito` com o `Requisitos` novo; novas `enc-caravan-pontos-15` e `enc-sunblade-pontos-20` (as duas medidas direto: dia 3 → a trilha reprova; **mortas**); as 16 `mw-*`/`col-*` da §30.5 seguem válidas (o `Requisitos` e o `Marcos` são os únicos trechos que o merge tocou).

## 31. C-CORE-01/02: `DistrictDef` e Império (2026-10-14)

**Vigente** (`Core/Distritos.cs`, `Core/Imperio.cs`, `Core/Sim.cs`, `Core/ContentValidator.cs`; branch `feat/distritos`): cada distrito é um `DistrictDef` em dados (itens, receitas, upgrades, workers, projetos, estações, pads, obstáculos, baús, início do ferreiro, ponto de contratação, largura, parede lateral, esteira, `Ajuste` do `Tuning`, abertura, Charter, fator de fundo) e uma instância de `Sim` (`new Sim(distrito: d)`). **Old Forge = as tabelas de sempre:** o layout que era constante no construtor do `Sim` saiu para o `DistrictDef` sem mudar nenhum número (IDs sem prefixo `dst_`, como o `CONTEUDO_V1.md` §2: `old_forge`, `armorers_row`). **Armorer's Row só de dados** (§2.2/§3/§5.2): 12 itens, 10 receitas (montagem = bancada que só começa com todos os componentes; rebitadeira 4 por lingote), 17 produtivos + 3 luxos (`fornalha_liga`, a liga e o Fundidor ficam para o C-CORE-05), turma Ferraz (4 Porters até o campo `crew`), 16 estações, 12 pads, balcão de 6 vagas, preços e intervalos do §1 pelo `Ajuste`. **Império** (`Imperio.cs`): carteira por ID de moeda, só `ouro` (V9) — o saldo mora no `Gold` do distrito ativo e passa inteiro ao trocar; Charter 1× sem reset (V5/V15: produção completa + Renome do Império + Masterworks do distrito; +100 de Renome com ID `charter:<id>`); abertura paga em ouro no mapa; **renda de fundo** = `FundoFator` (25%, manager nível 1 do §1) × a `RateEma` do distrito congelada quando o jogador saiu, só com o Charter e fora de foco, online a cada passo e offline com o teto de 2 h do núcleo (P2: a taxa, nunca o tempo), fora do `GoldEarned`; relógio para trás e claim repetido não pagam (`claimId` por distrito). **Save:** o payload da raiz como sempre (carteira no `gold=`) e, só depois do 1º Charter, as linhas opcionais `imp=` (ativo), `charter=` e `dist.<id>=` (payload do distrito em base64); Império de 1 distrito grava o texto do `Sim.Save` **ao byte**; save antigo (schema 3) vira 1 distrito sem perda; `dist.<id>` de build mais novo volta no save. Validator (`ContentValidator.Mapa/Distrito/Layout/Alcancavel/Charter`): ID único em `[a-z0-9_]`, índice = posição, 1º distrito de graça e os outros com abertura > 0, cada um liberado pelo Charter de exatamente um anterior; estação com ID único, aberta por upgrade do distrito, depósito dá item bruto da tabela, pads vendem upgrades do distrito, esteira liga quem faz a quem usa, ferreiro chega a toda boca e pad (kit e tudo comprado), Charter com Renome igual ao limiar do nível que libera `charter_<id>`, fundo em (0, 1); e as regras de conteúdo de sempre (requisitos alcançáveis, luxo depois da produção, custos, nomes ≤ 16, receitas, bocas, pisáveis) sobre o mundo do distrito.

View (`Game.cs`, `Textos.cs`, `Art.cs`, `MenuBar.cs`, `WorldView.cs`, `EsteiraView.cs`, `Servicos.cs`, `Loc.cs`): o jogo roda o Império (`_imp`); mundo, menu, dica, anúncios e cartões pelo distrito ativo; Renome, metas, eventos, projetos e compras pela raiz (o Old Forge, nesta leva). Mapa mínimo (C-UI-20): botão MAPA sob a engrenagem quando o Charter está pronto ou há outro distrito; uma linha por distrito (aqui / o que falta para o Charter / aberto / abrir por N / fechado) e um botão com a próxima ação (Charter → Abrir → Viajar); trocar = a cena renasce com o mesmo Império. Dev/fotos: `-distrito armorers_row` (só com `-testsession`), `-botwarmup S`, `-mapa`.

### 31.1 Old Forge idêntico ao byte (`main` ddff35d, #60 × este branch com a `main` mesclada)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `c7e639a9…bbe58` (idêntico) |
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `75950063…07261` (idêntico) |

F2P ×8: produção completa 47:14, zona morta 19,6, saldo 808 — os números do §28.2 "depois". Portões (GDD §3.3, `ECONOMIA_WAVE_B.md` §10): primeiros 10 min tick a tick, produção completa 41–55 e pisos do §3 — **iguais à `main`** (CSV idêntico). Testes: `OldForge_LayoutEmDados_IgualAoConstrutorDeAntes`, `OldForge_ImperioDeUmDistrito_SaveIgualAoByte`, `SaveReal_Schema3_ViraOldForgeSemPerda` (save real do bot aos 25 min, schema 3, sem perda), `Loc_ChavesELarguras_DaRow` (chaves e larguras da Row nos 3 idiomas).

### 31.2 Armorer's Row, primeiro corte (`--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30`)

Como o jogador chega (`Imperio.Chegada`): Old Forge em produção completa a 2.400/min (fundo 600/min), Charter forçado (o econsim não mede as condições do V15), 140.000 da abertura pagos. 17 produtivos = 171.065 de ouro.

| perfil | 1ª venda | produção completa | zona morta (min; maior seq.) | renda/min nos 10 min após a produção | renda/min nos últimos 10 min | fundo/min | luxo | saldo final |
|---|---|---|---|---|---|---|---|---|
| Ideal | 1:09 | 100:33 | 84 (26 a partir do min 81) | 1.102 | 1.150 | 599 | 1 | 22.875 |
| F2P | 1:22 | 90:53 | 74,5 (19 a partir do min 71) | 1.257 | 1.489 | 599 | 2 | 7.390 |
| Slow | 0:57 | 112:08 | 96 (28 a partir do min 84) | 874 | 977 | 599 | 0 | 36.112 |
| AdWatcher | 1:41 | 99:13 | 83 (23 a partir do min 74) | 1.338 | 1.177 | 519 | 1 | 29.880 |

Mix (F2P, % da receita): elmo 20,2 · maça 15,1 · grevas 21,5 · lança 12,4 · manoplas 10,3 · machado 9,7 · peitoral 10,8 (o modelo do §1 previa o peitoral na frente; a montagem de 3 componentes é a que mais espera).

**Leitura.** O critério do C-CORE-01 passa: um distrito só de dados **roda até o fim** (o bot fecha os 17 produtivos nos 4 perfis, vende os 7 produtos, o fundo entra a 25% da taxa do Old Forge e fica fora do `GoldEarned`; `Row_BotF2P_ChegaAProducaoCompleta`). A economia da Row, não: produção completa em 91–112 min contra ~61 do modelo do `CONTEUDO_V1.md` §1, renda de 0,9–1,5 mil/min contra 4.800 (¼ do alvo) e zona morta de 75–96 min (análogo do portão ≤ 12 do §10: o bot passa mais da metade do tempo esperando ouro, porque o fundo de 600/min paga sozinho a maior parte dos 171 mil). Causas prováveis, a medir no C-CORE-05 (os knobs estão todos no `DistrictDef.Ajuste` e na tabela de upgrades, sem tocar o Old Forge): (1) intervalos de cliente do §1 "mix" (11,5 s por produto = 5 un/min por linha) travam a renda muito abaixo da vazão das bancadas; (2) a montagem de 2–3 componentes com um só carregador por papel (turma Ferraz de 4) deixa as bancadas com fome; (3) sem oficial, níveis de estação e liga o prêmio de qualidade é zero. Hipótese para o próximo corte: intervalo ÷ 2 e Ferraz 2/3 mais cedo (tier 2/4) e medir de novo; alvo do §10 para o distrito = zona morta ≤ 12 min e produção dentro da janela do §1 (3–5 h de Charter).

Mutações (DistritosTests, `dotnet test --filter DistritosTests`): 7 provadas, todas mortas — `imp-troca-sem-saldo` (Trocar sem passar o saldo: 2 testes caem), `imp-offline-sem-claim` (ApplyOffline sem o claimId por distrito), `imp-fundo-sem-charter` (fundo sem Charter), `imp-charter-2x` (Charter concedido 2×), `imp-save-sem-ativo` (save sem a linha `imp=`), `loc-chave-faltando` (`item.couro` fora do Loc), `view-estacao-sem-arte` (`Station.Arte` não chega do `StationDef`; assert novo em `Row_Kit_BalcaoDe6EOsNumerosDoDistrito`).

## 32. D-CORE-04: Royal Contracts (2026-10-14)

Regra em GDD §11 e CONTEUDO_V1 §8 (os 12 contratos), V25 (contrato nunca bloqueia). **Vigente** (`Core/Contratos.cs`, `Core/ContratosDefs.cs`): os 12 `ContractDef` em dados (IDs `ctr_*`, distrito de origem, etapas item × N × qualidade mínima, prazo-bônus em minutos de jogo, ouro em s da taxa, Renome, Blueprint); o requisito de Renome é o da tabela do `RenomeDefs` (ronda 3, milícia 5, dote 6, coroação 7, vanguarda 8, campeão 9); só `ctr_ronda_vila` é do Old Forge — os outros 11 abrem com o distrito deles e ficam dormentes até a Wave C (o validator não cobra item de distrito fora do build). Os contratos são do **Império**: moram no Sim da raiz (o Old Forge, como Renome e metas nesta leva) e o `Sim.Contratos` de outro distrito devolve os da raiz pelo `Heroi` (C-CORE-02, #61): a venda de qualquer distrito conta pelo ID do item, o prazo anda no tick do distrito ativo, a conclusão paga na raiz (o ouro vai à carteira pelo `Imperio.Fundo`) e o aviso chega ao distrito ativo; só o payload da raiz grava `ctr=`/`ctr.v=`. Oferta sem sorteio: o 1º da lista não concluído é o "próximo"; vira oferta depois das Ferramentas, com o Renome, com o distrito no build e com a flag `Tuning.Contratos`. **1 ativo por vez** (ponytail: a `mesa_real` do distrito 4 dá a 2ª vaga com o C-CORE-05). Progresso por etapa pelo caminho do Orders 2.0 (`Sim.Sell` → `Contratos.OnVenda`): só o item da etapa atual, no nível mínimo ou melhor, cada peça conta 1 (ponytail: a peça vendida conta para o contrato e para a encomenda; o seletor de cota 0/25/50/100% do catálogo, que desvia a peça à mesa do contrato, é o upgrade quando a decisão "vender ou cumprir" precisar de peso). Conclusão paga **1×** pelo ID `ctr:{id}` no ledger das metas (`Metas.Paga`: ouro = s × `RateEma` de agora, múltiplo de 5, piso das encomendas, **fora do `GoldEarned`**; Renome) e o Blueprint pelo `Projetos.Ganha` (fonte `Contract`, mesmo ID; os `bp_var_*` do catálogo são do "império" e entram no `ProjetosDefs` com a Wave C — até lá o gancho é no-op e o validator só cobra o Blueprint que existe no build). Prazo só de bônus (no prazo: ouro ×1,5 e Renome +5; fora: o normal; nunca falha nem expira). Abandonar não pune (nada pago, o progresso recomeça). `Ev.ContractDone` → diário `contract_complete`, gatilho `contract_complete` nas metas (`Metas.Eventos`) e nas missões de evento (`LiveOpsDefs.Gatilhos`; a Royal Week de hoje não tem a missão: só o gancho, V16). Save: linhas `ctr=` (concluídos, na ordem; ID de um build mais novo fica) e `ctr.v=` (o ativo), só quando existem; save antigo carrega sem contrato; nada lê relógio (o prazo anda com o `dt`): relógio para trás não reabre nem paga.

**Decisões e desvios (revisar):**
1. **Etapas sequenciais** (espada → escudo → ferramenta), não paralelas: o cartão mostra "etapa x/N" e a decisão é uma por vez.
2. **Baú cosmético (`cos_ctr_*`) fora:** não existe cosmético de oficina no núcleo; fica só o ID no catálogo (V9: se houver baú, conteúdo fixo mostrado antes).
3. **Prêmio do contrato no mesmo caminho das metas e dos eventos** (`Metas.Paga`, ledger `mt.led`), não num ledger próprio: o `Renome.Credita("ctr:…")` previsto no B-CORE-06 fica sem uso (o ledger das metas já garante 1× e o ouro).
4. **Nome do gatilho:** `contract_complete` (o brief), não `royal_contract_complete` (LIVEOPS_V1 §4.6): acertar o doc ou renomear antes de existir `wkl_contrato1`/`ach_contrato1`.
5. **O ronda é "dinheiro fácil" no Old Forge:** com o Renome 3 (1 h 41 no F2P) o bot vende as 60 peças em ~8 min (prazo-bônus de 90 min sempre batido); o contrato só pesa como decisão com o seletor de cota (desvio 1 acima) ou com N maiores. Fica para quando houver playtest do sistema.

### 32.1 F2P 8 × 60 min (`--perfis F2P --minutos 60 --partidas 8`): antes = `--tuning Contratos=false`, depois = flag ligada

Medido no branch com a `main` mesclada (#61 distritos e Império, #62 mochila, #63 Masterworks e Collection). CSV **idêntico ao byte** (`cmp`): o ronda pede Renome 3, que o F2P alcança à 1 h 41. Produção completa 47:14, zona morta 19,6 min, ouro de encomendas 9,9% da venda, Renome 1 14:35, Renome 2 48:00, saldo aos 60 min 619, contratos 0/0. Pisos do §3: 0% abaixo.

### 32.2 5 perfis × 3 partidas × 240 min (`--minutos 240 --partidas 3`): antes (flag desligada) → depois

| perfil | Produção | Renome 3 (= aceite) | 1ª conclusão | Renome 4 (antes → depois) | Renome em 4 h | ouro do contrato (% da venda em 4 h) | pós-produção (antes → depois) |
|---|---|---|---|---|---|---|---|
| Ideal | 48:20 | 1:20:10 | 1:27:50 | 1:46:12 → **1:43:58** | 928 → 949 | 25.903 (3,8%) | 684.377 → 712.919 |
| F2P | 48:28 | 1:40:54 | 1:48:42 | 3:11:00 → **3:01:24** | 514 → 534 | 25.487 (3,9%) | 652.346 → 677.833 |
| Slow | 51:13 | 1:41:16 | 1:49:18 | 3:09:21 → **3:01:03** | 517 → 537 | 24.997 (3,8%) | 647.058 → 672.054 |
| AdWatcher | 44:29 | 1:38:00 | 1:46:51 | 3:08:39 → **2:58:46** | 523 → 543 | 24.133 (3,5%) | 697.146 → 721.279 |
| AdWatcherE | 45:01 | 1:44:23 | 1:53:32 | 3:15:58 → **3:05:49** | 502 → 522 | 26.805 (4,0%) | 674.735 → 701.540 |

Tudo o mais (produção completa, Renome 1–3, encomendas, prêmio de qualidade, projetos, níveis IV–V, pisos do §3 a 0% abaixo) é idêntico antes e depois: o contrato entra só depois do Renome 3 e nunca bloqueia (o bot fecha o Old Forge sem ele; com a flag desligada o jogo é o de antes). O contrato paga uma vez 450 s da taxa (300 × 1,5 no prazo) = ~25 mil à 1 h 49 no F2P (uma encomenda Royal paga 32 s), +20 de Renome (15 + 5): o Renome 4 chega ~10 min antes. Antes do merge do #63 (sem os marcos de Renome da Collection) o aceite era às 2 h 13 e a conclusão às 2 h 20 no F2P, com o mesmo ouro (3,8%) e o mesmo adianto do Renome 4 (3:46 → 3:37).

### 32.3 Portões (GDD §3, ECONOMIA_WAVE_B §10)

| Medida | Portão | Medido (F2P) | |
|---|---|---|---|
| Primeiros 10 min | iguais, tick a tick | CSV de 60 min idêntico ao byte com a flag ligada; golden `EncomendasTests.PrimeirosDezMinutos` verde | ok |
| Produção completa | 41–55 min | 47:14 (8 × 60), 48:28 (3 × 240) — iguais ao antes | ok |
| Ouro sem destino aos 60 min | ≤ 10 mil | 619 | ok |
| Renome 1 / 2 / 3 / 4 | 12–25 min / 40–75 min / 1,5–3 h / 3–6 h | 14:18 / 50:47 / 1:40:54 / 3:01:24 (antes 3:11:00) | ok |
| Contrato nunca bloqueia (V25, GDD §23 #7) | bot fecha o distrito sem contrato | produção completa igual com a flag desligada; validator: melhoria nunca exige Blueprint só de contrato | ok |
| Recompensa 1× com ID de transação | `ctr:{id}` | `ContratosTests`: entregar 2×, save/load, relógio para trás e ledger `Metas.Paga` = −1 | ok |

### 32.4 Testes e prova vermelha

`ContratosTests` (15): catálogo e validator limpo, oferta determinística (Ferramentas, Renome, 1 vaga, flag, distrito), progresso por etapa e qualidade mínima, conclusão 1× (ouro da taxa fora do `GoldEarned`, Renome, Blueprint pela fonte `Contract`, ledger), gancho no-op sem o Blueprint no build, prazo-bônus, abandono, save ida e volta / antigo / lixo / ID desconhecido, relógio para trás, Império (venda na Rua dos Armeiros conta no contrato da raiz pelo ID, prazo no tick do ativo, aviso no ativo, save só na raiz), missão `contract_complete` de evento, bot F2P (aceita no Renome 3 e cumpre vendendo; sem a flag a partida de 60 min é idêntica) e validator com 17 defeitos injetados. `MetasTests.Eventos_TodoGatilhoContaNoJogo` ganhou Renome 3 no bot (o gatilho novo conta de verdade). LocTests cobra `ctr.<id>` e as larguras.

**Prova vermelha** (`python client/tools/mutantes/mutantes.py --so ctr-…`, sobre o branch com o #61/#62 mesclados; os arquivos mutados não mudam com o #63): **16 mutações `ctr-*`, todas mortas** (0 vivas, 0 inválidas, 2.102 s): contrato concluído volta à oferta, prazo-bônus nunca paga, qualquer item conta, qualidade mínima ignorada, oferta sem o Renome, 2 ativos, abandono que pune, save sem o progresso da etapa, prazo parado, sem o Blueprint, flag ignorada, contrato antes das Ferramentas, etapa que fecha com N + 1, contrato de outro distrito do Império que não é o da raiz, e o validator sem a checagem de item e de Blueprint essencial só por contrato.

## 33. C-CORE-05: Rua dos Armeiros afinada (2026-10-14)

**Vigente** (`Core/Distritos.cs`, `Core/EncomendasDefs.cs`, `Core/Encomendas.cs`, `Core/ContentValidator.cs`, `Core/MasterworksDefs.cs`, `Core/Contratos.cs`, `Core/ContratosDefs.cs`, `View/Loc.cs`, `View/Textos.cs`; testes em `Tests/EditMode/RowTests.cs`; branch `feat/row-conteudo`). Só a Armorer's Row mudou; o Old Forge é o de sempre, ao byte (§33.4).

- **Economia (desvio do catálogo, revisar):** preços do `CONTEUDO_V1.md` §3 × 3,5 (elmo 490, maça 420, grevas 455, lança 245, manoplas 420, machado 385, peitoral 805; a relação entre os produtos é a do catálogo) e os 17 produtivos do §5.2 × 1,55 (745 … 65.000; múltiplo de 5 até 10 mil, 2 algarismos acima, a regra do §1). Luxo (42/54/66 mil), intervalos de cliente, balcão de 6 vagas e estoque 10 como estavam.
- **Encomendas da Row em dados** (§7): `ord_common/bulk/quality/rush/mixed/royal/blueprint_armorers_row` e o freguês `ord_reg_brutus` (Sgt. Brutus, 30 lanças), com o ciclo da Row (Common, Bulk, Quality, Common, Bulk, Mixed, Rush, Blueprint, Common, Royal). O `Encomendas` lê as `OrderDefs` do `Sim.Distrito` (antes, `OrderDefs.OldForge` fixo: a Row rodava as do Old Forge com os itens dela). "Abre: distrito aberto" = sem mínimo de linhas nem de entregues (Mixed pede 3 linhas). Quality e Royal (Fina) e Blueprint (fila da Row vazia) viram Common enquanto a Row não tem fonte de qualidade nem projeto. Mesmo desvio do Old Forge (§28): Bulk, Quality, Mixed, Royal e o Brutus pagam 70% dos segundos do §7.2 (45/18/25/45/32 → 32/13/18/32/22). O validator confere o item fixo no mundo do distrito da encomenda.
- **Obras-primas da Row: nenhuma entrou.** O validator do distrito passou a conferir as Masterworks e os marcos da Collection do Hall DELE (`ContentValidator.Distrito` → `MasterworksDefs.Validar` com o mundo do distrito; obra declarada noutro distrito é recusada). Das 10 do §10.1, 8 o núcleo consegue escrever e todas caem em **"Superior inalcançável"**: a Row não tem fonte de qualidade (q máximo 0 na `montagem` e na `forja_armas`). O §10.2 chega a 140/110 com níveis 60 + técnica 20 + **liga 30** (+ Vulcana 30 na montagem); sem a liga para em 80, abaixo dos 100. Além disso: `mw_peitoral_bastiao` e `mw_machado_rei` pedem nível de estação (o §5.7 da Row não existe), `mw_maca_juiz` pede a `wk_vulcana` (não existe) e `mw_manoplas_vivas`/`mw_elmo_liga` pedem `AlloyPieces` (condição que o núcleo não tem). Os marcos da Collection da Row também esperam: a Collection só abre com um projeto de Masterwork, e a Row não tem nenhum. **O Charter da Row (2 obras) segue travado até a liga** (C-CORE-05, mecânica: fornalha de liga, slot opcional nas montagens, `AlloyPieces`) e os níveis/Blueprints da Row. O caminho em si funciona no Sim da Row (`Masterwork_DaRow_PeloCaminhoDeSempre`: obra e projeto do catálogo injetados, q 200 pelo modificador de evento no lugar da liga).
- **Royal Contracts (#64) com a Row no build:** o `ctr_vinte_guardas` (espada + escudo + elmo) e o `ctr_milicia_norte` (lança + maça + grevas, Renome 5) abrem — item da etapa vale se é produto de algum distrito do build (`ContratosDefs.NoBuild`, também no validator). Semântica "no build", não "aberto pelo jogador": depois do Ronda, o Old Forge já vê o Vinte Guardas e a etapa do elmo espera a Row (nada bloqueia; o "aberto" pede o Império na raiz). O cartão do contrato traduz o item pelo ID ("20 elmos"; antes "20 elmo", ou o item errado com a Row ativa na View).
- **Layout:** depósito da Row de y 1,5 para 1,0. O rótulo dele (1,35 m acima, `WorldView.RefreshStation`) ficava a 0,11 m do da Fornalha (1,04 m abaixo dela); agora 0,61 m (a caixa do rótulo tem 0,37 m). Descer as fornalhas poria o pad da Fornalha 2 (rótulo 0,82 m acima) sob o rótulo da Prensa 2. Validator de alcance verde (kit e tudo comprado).

### 33.1 Por que o preço e não a demanda: a Row é presa no transporte

Diagnóstico (bot F2P chegando como o jogador, 100 min, preços do catálogo): a cada 10 min as fornalhas ficam ~400 s **travadas** (lingote pronto que ninguém leva) enquanto prensas, rebitadeira, montagens e forjas passam ~500 de 600 s **com fome**; a fila do balcão vive cheia (6/6) de quem espera o produto que falta e ~700 clientes por 10 min dão meia-volta. A vazão é ~12 peças/min: lingote, placa, rebite e couro vão de mão em mão (4 Ferraz com carga 2 a 2,4 m/s, montagem de 2–3 componentes, couro na carroça do canto). O modelo do §1 supõe a vazão das estações (~38 un/min na produção completa); 38/12 ≈ 3,2.

Varredura (F2P × 8, 150 min, preços do catálogo; renda/min nos 10 min depois da produção / últimos 10 min): base 1.356 / 1.372; intervalo de cliente × 0,5 → 1.206 / 1.421; × 2 → 1.227 / 1.364; fila de 8 → 1.280 / 1.371; paciência 1 s/s (era 3) → 1.261 / 1.487; Ferraz 4 no papel 1 (intermediários) → 1.497 / 1.559; turma subindo de nível antes da produção (`--worker-seg 60`) → 1.302 / 1.300. Demanda, fila e paciência não mexem; um carregador a mais rende +10%. Sobram preço e custo (os knobs do `Ajuste`): só o preço (× 3,5) fecha a produção aos 39 min; só o custo não sobe a renda. Grade preço × custo (F2P × 16): × 3,5 com custo × 1,5 / 1,55 / 1,6 / 1,65 → 55,4 / 57,0 / 59,5 / 61,4 min, renda 4,6–5,0 mil, maior sequência morta 13–15. Ficou × 1,55. A correção estrutural (turma Ferraz com carga maior ou um "ajudantes ágeis" da Row, o campo `crew` do §6) fica para a leva da liga: se entrar, a renda sobe e o preço volta para perto do catálogo.

### 33.2 Antes (§31.2) → depois (`--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30`)

17 produtivos: 171.065 → 264.705 de ouro.

| perfil | 1ª venda | produção completa | zona morta (min; maior seq.) | renda/min nos 10 min após a produção | renda/min nos últimos 10 min | luxo | saldo final | min sem destino |
|---|---|---|---|---|---|---|---|---|
| Ideal | 1:09 → 1:14 | 100:33 → **65:45** | 84 (26) → 51 (14) | 1.102 → 4.558 | 1.150 → 3.536 | 1 → 3 | 22.875 → 221.370 | 0 → 43,5 |
| F2P | 1:22 → 1:32 | 90:53 → **57:23** | 74,5 (19) → 42,5 (12) | 1.257 → 5.765 | 1.489 → 5.138 | 2 → 3 | 7.390 → 397.172 | 0 → 62,5 |
| Slow | 0:57 → 1:00 | 112:08 → **73:26** | 96 (28) → 58 (19) | 874 → 3.066 | 977 → 2.942 | 0 → 3 | 36.112 → 90.087 | 0 → 21,5 |
| AdWatcher | 1:41 → 1:56 | 99:13 → **56:01** | 83 (23) → 41,5 (12) | 1.338 → 4.821 | 1.177 → 5.717 | 1 → 3 | 29.880 → 357.095 | 0 → 59 |

F2P × 8 (`--perfis F2P --minutos 150 --partidas 8`; antes = `main` 55b59ee e 905676d, iguais): produção completa **94:31 → 58:49**; zona morta 78,1 (maior sequência 24) → 44,3 (**14**); renda/min nos 10 min após a produção 1.356 → **5.059** e nos últimos 10 min 1.372 → **4.633**; luxo 1,4 → 3; saldo aos 150 min 26.157 → 341.612; minutos sem destino 0 → 57,3.

Mix (F2P, % da receita): elmo 20,6 · maça 11,6 · grevas 21,5 · lança 11,4 · manoplas 8,7 · machado 13,9 · peitoral 12,2.

**Leitura contra o alvo (§1: ~61 min e 4.800/min).** F2P em 57–59 min (janela pedida 55–70) e renda 4,6–5,1 mil/min (−4% a +6% do alvo; aceite ±25%). Ideal mais lento que o F2P (65:45), como já era no §31.2 (provável: o golpe de mestre para o ferreiro na boca da bancada, e na Row ele só sobe Normal → Fina). **Zona morta:** o total (42–44 min) não se compara com o do Old Forge: com 17 compras numa produção de ~58 min, pelo menos ~41 minutos ficam sem compra por construção (o Old Forge tem bem mais compras: balcões, níveis de estação, workers). O que mede a espera é a maior sequência: F2P **14 min** (≤ 15, GDD §3; era 24), Ideal 14, AdWatcher 12; o Slow, 19 (a ~3 mil/min, os últimos produtivos de 50–65 mil pedem ~20 min cada). A Rebitadeira 2 (65 mil, a última) passa a partida com fome: é o retrato do transporte saturado do §33.1. **Achado (ouro sem destino):** depois da produção completa e dos 3 luxos a Row não tem ralo (níveis de estação do §5.7 = 236.800, liga, Vulcana e o Charter, que pede obras), e o saldo passa de 300 mil aos 150 min. Fecha com a leva da liga/níveis; a abertura do Artisan Quarter (430 mil) é o ralo seguinte quando ele existir.

**Portões (GDD §3, `ECONOMIA_WAVE_B.md` §10), quando aplicáveis à Row:** FTUE (1ª venda < 90 s, upgrade < 3 min, automação < 5–7 min, produto novo < 8–10 min) é da 1ª sessão no Old Forge e lá está igual (§33.4); na Row, 1ª venda 1:32 (a cadeia tem 4 etapas e o depósito ficou 0,5 m mais longe), Fole de couro 1:15, Ferraz 1 2:48 e Maças 3:38 (bot F2P, 1 partida). Retorno de upgrade 8–12 min: a grade de custo × 1,55 mantém a curva 1,30 do §5.2. **Ouro de encomendas** (bot F2P × 16, 150 min; o econsim `--distrito` não imprime): **5,9%** da venda com a vaga única de hoje (o Renome do Sim da Row começa em 0) e **12,1%** com as 3 vagas do Renome do Império (Row recebida com Renome 400, como ficará quando o Renome subir ao Império, raia F); com os segundos do catálogo eram ~7,6% e ~15,3% (portão 4–12%).

### 33.3 Testes e prova vermelha

`RowTests` (6): `Encomendas_DaRow_RodizioEItensDaRow` (1ª encomenda = Common da Row com mínimo 75; os 10 lugares do rodízio na ordem da Row, todos com OrderDef da Row e itens de linhas abertas da Row; Bulk com N mínimo 12; `TipoAberto(Bulk)` sem entregues; a 7ª Common é o Brutus com 30 lanças; a vaga volta do save pelo ID), `Encomendas_OldForge_AsDeSempre`, `Contratos_DaRow_AbremEOCartaoTraduzOElmo` (Vinte Guardas abre depois do Ronda; Milícia do Norte no Renome 5; "Pede: 20 espadas + 20 escudos + 20 elmos" com o Old Forge ou a Row ativa, "20 yelmos" em es), `Masterwork_DaRow_PeloCaminhoDeSempre` (golpe numa Superior da montagem vai ao Hall da Row 1×, credita o Renome pelo ID, conta no `Sim.Masterworks` que o Charter lê, nunca na pilha nem no `GoldEarned`; a 2ª sai Superior), `Validator_ObrasDaRow_EsperamALiga` (as 8 do catálogo injetadas: "Superior inalcançável" em todas, nível de estação e Vulcana inalcançáveis, Bulk/lanças/Renome 6 aceitos, q máximo 0; obra de outro distrito recusada no mundo da Row) e `Layout_RotuloDoDepositoNaoEncostaNoDaEstacaoDeCima`. Ajustados: `Row_Kit_BalcaoDe6EOsNumerosDoDistrito` (745 / 65.000 de custo; 490 / 805 de preço) `Validator_ConteudoReal_Limpo_EDefeitosInjetados_Acusa` (o distrito inexistente agora é o `artisan_quarter`) e, do #64, `Oferta_SemSorteio_…` (o Vinte Guardas abre) e o `Validator_DefeitosInjetados_Acusa` dos contratos (o item inexistente agora é o `cinzel`). Chave `cli.cli_reg_brutus` nos 3 idiomas (LocTests).

Mutações (`RowTests`, `DistritosTests`, `EncomendasTests`, `MasterworksTests`, `ContentValidatorTests`, `LocTests`, `ContratosTests`; rodadas depois do merge com a `main` 905676d): **12 provadas, todas mortas** — `row-enc-oldforge-fixo` (o rodízio volta a ler o Old Forge), `row-tipoaberto-oldforge`, `row-ciclo-do-oldforge`, `row-brutus-no-oldforge` (4 testes caem), `row-validator-item-no-oldforge` (o item fixo conferido no mundo do Old Forge: 3), `row-validator-sem-obras` (o validator do distrito sem as obras dele), `row-mw-distrito-de-outro` (2), `row-preco-do-catalogo` (2: o bot F2P nem fecha a produção em 150 min), `row-custo-do-catalogo`, `row-deposito-em-1-5` (o rótulo volta a encostar), `row-contrato-so-na-raiz` (7: os contratos da Row não abrem e o validator recusa o elmo) e `row-cartao-pelo-indice` (volta o "20 elmo").

### 33.4 Old Forge idêntico ao byte (`main` 905676d, #64 × este branch com a `main` mesclada)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…8d3a96` (idêntico, `cmp`) |
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico, `cmp`) |

F2P × 8: produção completa 47:14, zona morta 19,6, os números do §31.1. A `main` foi extraída com `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário e as duas rodadas comparadas com `cmp`.

## 34. C-CORE-03: managers e Império (2026-10-14)

**Vigente** (`Core/Imperio.cs`, `Core/Sim.cs`, `Core/Encomendas.cs`, `Core/Masterworks.cs`, `Core/Bot.cs`, `View/Game.cs`, `View/Loc.cs`, `tools/econsim`; branch `feat/managers-imperio`). **Managers** (GDD §8, `CONTEUDO_V1.md` §1 e §6, P2): o manager vem com o Charter do distrito (nível 1, contratação 0) e sobe aos níveis 2–5 por ouro da carteira; cada nível sobe a **taxa** da renda de fundo do distrito, online (fora de foco) e offline, **nunca o teto de 2 h** (`Imperio.ApplyOffline` segue com `min(Δt, OfflineCap)`). Dados em `Imperio.Managers` (um `WorkerDef` com papel `Manager` por distrito: Mestre Anselmo no Old Forge, Dona Gertrudes na Row; os dos distritos 3–6 entram com o `DistrictDef` deles) e `Imperio.FundoNivel` = ×1 / 1,2 / 1,32 / 1,44 / 1,6 sobre o `DistrictDef.FundoFator` (o nível 1, 25%): **25 / 30 / 33 / 36 / 40%** da `RateEma` congelada. Fração sempre < 1: o distrito ativo rende a taxa inteira, então rende mais que ele mesmo em fundo em todo nível (invariante testado para todo distrito do build). Compra com **ID de transação = (distrito, nível)**: `SobeManager(id, nivel)` só aceita o próximo nível, 1×, com o Charter e o ouro (toque duplo, pular nível e o mesmo nível depois de save/load recusam). Save: linha opcional `mgr=id:nível` (só acima do 1) no bloco do Império, schema 3; save sem ela = nível 1; lixo fica de fora, nível acima de 5 corta, distrito de build mais novo volta no save. Bot (`Bot.Manager`, só no econsim `--distrito`): depois da produção completa do distrito ativo, sobe o manager mais barato que a carteira paga (`--gerente 0` = nunca, o econsim de antes).

**Camadas do Império:** o `Sim.Renome` de um distrito **é** o da raiz (propriedade: encomenda, Superior vendida, obra-prima e marco da coleção do distrito sobem o mesmo Renome, e o nível dele libera vagas, encomendas e projetos no distrito); o payload do distrito não grava mais `rn=` e um payload antigo com `rn=` não pisa no Renome do Império. Venda, VIP, encomenda (`order_done` e o tipo pelo `OnEncomenda`), golpe de mestre e cofre do distrito contam **também** nas metas (diárias, semanais, conquistas), no evento e no XP do Pass da raiz, 1× (`Sim.Emit`, `Sim.Strike`, `Encomendas.Entregar`), com o item traduzido pelo ID (o que o Old Forge não tem conta no total, sem filtro de item: o elmo da Row, handle 2, não vira espada). O contador do próprio distrito segue (o Hall dele lê). Fora desta leva: compra, contratação, qualidade e obra-prima do distrito nas metas da raiz (índice de upgrade, worker, estação e Hall não traduzem; entram com a meta/missão que os peça por distrito). Correções que vieram junto: o Charter conta a obra-prima pelo Hall (`max(contador, Hall.Count)`: o contador da sessão não vai no save e o Charter travava depois de reabrir) e o Hall de cada distrito só readota do ledger (agora o do Império) a obra-prima dele.

Mapa (C-UI-20): na linha de cada distrito com Charter, o manager ("Mestre Anselmo, gerente nv 2/5: rende 30% fora (nv 3: 64 mil)") e, com ouro, o botão "Gerente nv N"; o painel continua com **uma ação por vez**, agora por prioridade (Charter → Abrir → Gerente → Viajar; antes, na Row com o Charter pronto, o botão mostrava "Viajar" ao Old Forge). Dev/fotos: `-gerente N` (manager do Old Forge no nível N, sem gastar ouro; precisa do Charter, `-distrito armorers_row -testsession`).

### 34.1 Fundo por nível (HIPÓTESE de custo: 20/40/80/160 min da renda alvo, `CONTEUDO_V1.md` §6)

| nível | fração da taxa | Old Forge a 2.400/min (fundo/min) | custo | retorno (min de fundo) | Row a 7.200/min no Charter (fundo/min) | custo | retorno (min de fundo) |
|---|---|---|---|---|---|---|---|
| 1 (Charter) | 25% | 600 | — | — | 1.800 | — | — |
| 2 | 30% | 720 | 32.000 | 267 | 2.160 | 96.000 | 267 |
| 3 | 33% | 792 | 64.000 | 889 | 2.376 | 190.000 | 880 |
| 4 | 36% | 864 | 130.000 | 1.806 | 2.592 | 380.000 | 1.759 |
| 5 | 40% | 960 | 260.000 | 2.708 | 2.880 | 770.000 | 2.674 |

Offline com o manager no nível 5: 10 h fora pagam 40% × 2.400/min × 120 min = **115.200** (o mesmo que 2 h; `Manager_Offline_TetoDeDuasHorasIntocado`).

### 34.2 Old Forge idêntico ao byte (`main` 55b59ee × este branch)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…3fdb3` (idêntico) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…d3a96` (idêntico) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `9cf20606…ee28f` (idêntico: as camadas não mudam a economia da Row) |

F2P ×8: produção completa 47:14, zona morta 19,6, saldo 619 — os da `main`. Portões (GDD §3.3, `ECONOMIA_WAVE_B.md` §10): primeiros 10 min tick a tick, produção completa 41–55 e pisos do §3 — **iguais à `main`** (CSV idêntico).

### 34.3 Row com o manager do Old Forge e o Império (`--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --partidas 2 --anuncio-seg 30`)

| perfil | min | manager do Old Forge (nível no fim; 1ª subida) | fundo/min (média) com × sem | saldo final com × sem | Renome do Império no fim | vendas nas metas do Império / vendas |
|---|---|---|---|---|---|---|
| Ideal | 150 | 2; min 135 | 612 × 599 | 12.997 × 22.875 | 28 | 1.105 / 1.105 |
| F2P | 150 | 2; min 119 | 624 × 599 | 34.024 × 7.390 | 65 | 1.509 / 1.509 |
| Slow | 150 | 1,5; min 146 | 601 × 599 | 20.389 × 36.112 | 55 | 1.057 / 1.057 |
| AdWatcher | 150 | 2; min 129 | 534 × 519 | 22.142 × 29.880 | 53 | 1.233 / 1.233 |
| Ideal | 300 | 3,5; min 135 | 688 × 600 | 53.253 × 184.236 | 96 | 2.235 / 2.235 |
| F2P | 300 | 4; min 119 | 707 × 600 | 78.169 × 288.579 | 142 | 3.143 / 3.143 |
| Slow | 300 | 3; min 149 | 678 × 600 | 79.691 × 128.214 | 141 | 2.319 / 2.319 |
| AdWatcher | 300 | 4; min 129 | 601 × 520 | 8.003 × 190.802 | 131 | 2.578 / 2.578 |

Produção completa, zona morta e renda até a produção: as do §31.2 (o manager só entra depois dela). **Leitura.** (1) O manager é o ralo do rabo da Row: aos 300 min o saldo parado cai de 128–289 mil para 8–80 mil e o fundo do Old Forge sobe de 600 para 680–710/min (nível 3–4); o luxo da Row sai igual (3) aos 300 min, mais tarde aos 150 (o bot sobe o manager antes de juntar para o luxo mais caro). (2) O retorno é lento de propósito (267 min de fundo no nível 2, ~45 h no 5): o manager é progressão de longo prazo, não atalho; HIPÓTESE a confirmar no POCO (se o jogador não sentir o nível, baratear o 2 ou mostrar o ganho por hora no mapa). (3) Camadas: com a Row ativa as metas do Império contam **cada** venda dela 1× (vendas nas metas = vendas) e o Renome do Império sobe 28–65 em 150 min e 96–142 em 300 min só com as encomendas da Row (antes da leva isso ficava no Renome do distrito, que ninguém lia).

Mutações (`dotnet test --filter "ImperioTests|DistritosTests|MasterworksTests"`): **18 provadas, todas mortas** — `mgr-fundo-sem-nivel`, `mgr-teto-pelo-nivel` (o nível estica o teto offline), `mgr-compra-qualquer-nivel`, `mgr-sem-pagar`, `mgr-sem-charter`, `mgr-save-sem-linha`, `mgr-load-sem-teto`, `mgr-bot-antes-da-producao`, `imp-sem-repasse`, `imp-venda-2x`, `imp-sem-traducao`, `imp-cofre-fora`, `imp-golpe-fora`, `imp-encomenda-tipo-fora`, `imp-renome-do-distrito`, `imp-renome-load-distrito`, `imp-hall-ledger-alheio`, `imp-charter-contador`.

## 35. C-CORE-05: liga na Rua dos Armeiros (2026-10-15)

**Vigente** (`Core/Distritos.cs`, `Core/Sim.cs`, `Core/Equipe.cs`, `Core/ProjetosDefs.cs`, `Core/MasterworksDefs.cs`, `Core/Masterworks.cs`, `Core/EncomendasDefs.cs`, `Core/ContentValidator.cs`, `Core/Defs.cs`, `View/Textos.cs`, `View/Loc.cs`, `View/Game.cs`, `View/WorldView.cs`, `View/Captura.cs`, `tools/econsim`; testes em `Tests/EditMode/LigaTests.cs` e `RowTests.cs`; branch `feat/row-liga`). Só a Armorer's Row mudou; o Old Forge é o de sempre, ao byte (§35.5).

- **Liga** (`CONTEUDO_V1.md` §2.2 regra 2, §3, §5.2): item `liga` (intermediário, handle 12), receita da `fornalha_liga` (2 lingotes → 1 liga em 6 s; o Fole de couro também a acelera) e o upgrade `fornalha_liga` de volta ao lugar dele (produtivo, tier 11, 8.600 × 1,55 = **13.000**, pede a Fornalha 2; os 18 produtivos do §5.2 = 277.705). Toda montagem e forja ganha o **slot opcional** (insumo com `Qty` 0 na receita: a peça não espera por ele). Com liga no slot, o `QualityBreakdown` mostra **Material +30** (`Tuning.LigaQ`) e a peça leva a liga quando sai; `Sim.PecasComLiga` (o `AlloyPieces` do catálogo) conta por produto e vai no save (linha opcional `liga=id:n`, só com alguma; save antigo = 0).
- **Vulcana** (`wk_vulcana`, Smelter, §6): contratável em ouro (7.200) depois da fornalha de liga, níveis 2–10 do catálogo (1.800 … 77.000), como a equipe do Old Forge (Meta, o bot compra depois da produção completa). Opera a fornalha de liga (6 → 4 s, +3% por nível) e dá **q +15 nas montagens** (+25 no nível 5, +30 no 10), nunca nas forjas. Posto na View: em cima da fornalha de liga.
- **Níveis de estação da Row** (§5.7): montagem e forja de armas II–V (4.300/13.000/35.000/78.000 e 3.500/11.000/28.000/64.000 = 236.800), Meta, no menu; IV–V com o `bp_mestra_armorers`.
- **Blueprints da Row** (§9): os 14, com as filas da Row (encomenda Blueprint #1–#5, Royal #1–#3); essenciais com fonte grátis permanente (`bp_mestra_armorers`: Royal #1 ou produção completa; `bp_mw_elmo_vigia`: 100 elmos; `bp_mw_peitoral_bastiao`: fila #3 ou 150 peitorais). O validator do distrito agora confere os projetos dele (`ContentValidator.Distrito` → `Blueprints`: essencial nunca só pago).
- **Obras-primas da Row** (§10.1): as **10** no Hall da Row, com a condição nova `PecasComLiga` (AlloyPieces). Superior alcançável (§10.2, `QMaximo` com a liga e o fundidor): montagem 60 + 20 + 30 + 30 = **140**, forja de armas 60 + 20 + 30 = **110**. **Marcos da Collection da Row**: produtos 11/21 (+5/+10), workers 5/10 (+5/+10), projetos 7/14 (+5/+10) e as 10 obras = `cos_col_estandarte_armorers_row` (o `col_mw_distrito` do §10.3).
- **Encomendas**: com qualidade e projetos na Row, a Quality, a Royal e a Blueprint da Row abrem (antes viravam Common); entrou a variante Superior do Quality da Row (`ord_quality_armorers_row_superior`, Renome 5, 21 s; 225).
- **View**: painéis de projetos e do Hall pelo distrito ativo (antes a raiz: as obras e os projetos da Row ficavam invisíveis); o log do `Ev.Blueprint` pelo Sim que emitiu (com o da raiz, o índice da Row apontava para outro projeto); nomes de item nos projetos e no Hall pela tabela do Sim do painel (o peitoral, handle 8, saía cru). `-equipe`, `-masterwork` e `-projetos` valem no distrito ativo.

**Decisões e desvios (revisar):**
1. **Níveis II da Row pedem a Rebitadeira 2** (o último produtivo), não "a 1ª estação da família" do §5.7. Com o catálogo e o Renome do Império na chegada (≥ 500: 3 vagas e a encomenda Royal), o projeto de mestre vinha antes da produção e o bot comprava os IV–V (205 mil) no meio dela: produção completa do F2P **56:25 → 76:04** (× 16, janela 55–70). Com o II depois da produção: **58:46**, e os níveis viram o ralo do rabo da Row (o pedido do §33), com a Vulcana e o luxo. Medido também o II pedindo a fornalha de liga: quase o mesmo do catálogo no Slow (85:45 contra 88:37, × 2).
2. **A liga sai do slot com a peça, não quando ela começa** (§2.2): a peça em andamento não precisa de estado no save, e o que a barra mostra é o que a peça leva. O slot usa a entrada da estação (4–8), não a capacidade 2 do catálogo (só muda o estoque transitório de liga).
3. **`bp_mw_peitoral_rival` sem a 2ª fonte** (Contract: `ctr_vinte_guardas`): o contrato é do Império e dá o `bp_var_guardas` na raiz; fica a fila Royal #3.
4. **Vulcana com a folha do oficial** até a arte dela existir.
5. **Ferraz 4 continua no papel 0** (minério e couro). O catálogo diz "couro e liga → montagens e forjas"; medido no papel 1 (intermediários, `--renome 500`, × 8): a liga chega a ~8% das peças (5,2% no papel 0) e a renda depois da produção sobe 5.114 → 6.742/min no F2P (+32%, fora dos ±25% do alvo): pede reafinar o preço da Row (× 3,5), fica para o coordenador.

### 35.1 Antes → depois, Renome 0 na chegada (o corte do §31.2/§33.2/§34.3): `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30`

Antes = `main` a8e693b (#66) com o econsim deste branch (as colunas novas). Min sem destino conta pad, menu e equipe (contratar ou subir worker).

| perfil | produção completa | zona morta (min; maior seq.) | renda/min 10 min após a produção | renda/min últimos 10 min | luxo | saldo final | min sem destino | prêmio de qualidade | níveis (comprados; gasto) | Vulcana (nível; contratada) |
|---|---|---|---|---|---|---|---|---|---|---|
| Ideal | 65:45 → 70:43 | 51 (14) → 55,5 (19) | 4.558 → 4.339 | 3.897 → 5.130 | 2,5 → 2 | 121.737 → 28.738 | 24,5 → **0** | 14,1% → 27,4% | — → 6,5; 126.800 | — → 9; 79:37 |
| F2P | 57:23 → 61:29 | 42,5 (12) → 45,5 (11) | 5.828 → 5.265 | 5.152 → 7.239 | 3 → 3 | 198.918 → 5.355 | 47,5 → **1** | 0% → 16,8% | — → 7,5; 197.800 | — → 10; 68:48 |
| Slow | 73:26 → 72:39 | 58 (19) → 56 (16) | 3.066 → 4.498 | 4.307 → 4.290 | 3 → 1,5 | 52.231 → 52.757 | 10 → **0** | 0% → 14,2% | — → 6; 94.800 | — → 9; 80:46 |
| AdWatcher | 56:01 → 60:33 | 41,5 (12) → 45,5 (13) | 4.821 → 4.147 | 6.745 → 7.579 | 3 → 2,5 | 174.990 → 33.956 | 43 → **0** | 0% → 15,3% | — → 7; 158.800 | — → 9,5; 69:56 |

F2P, Slow e Ideal × 8 (`--perfis F2P,Slow,Ideal --minutos 150 --partidas 8`): produção completa 58:49 → **63:04** / 73:18 → 77:34 / 67:24 → 71:16; maior sequência morta 14 → 13 / 20 → 20 / 16 → 19; renda nos últimos 10 min 4.901 → **6.851** / 3.819 → 4.456 / 3.583 → 4.984; saldo aos 150 min 156.034 → 32.055 / 51.680 → 30.542 / 87.157 → 39.241; **min sem destino 42,4 → 0,4** / 13,6 → 0 / 19,5 → 0; prêmio de qualidade 0 → 16,5% / 0 → 12,1% / 14,3 → 27,3%; a Vulcana contratada em 8/8 (nível 9,4 / 8,6 / 9).

### 35.2 Chegada com o Renome do Império (`--renome 500`, o Charter do Old Forge: 3 vagas de encomenda): F2P × 16 × 150 min

| | produção completa | zona morta (maior seq.) | renda/min 10 min após / últimos 10 | saldo | min sem destino | **ouro de encomendas (% da venda)** | prêmio de qualidade | níveis | Vulcana |
|---|---|---|---|---|---|---|---|---|---|
| antes | 56:25 | 41,6 (14) | 4.753 / 4.883 | 169.461 | 47,6 | **12,1%** | 0% | — | — |
| depois | 58:46 | 43 (15) | 5.146 / 7.441 | 50.883 | **3,8** | **11,3%** | 21,1% | 7,6; 207.550 | 16/16; nível 9,9; 65:42 |
| catálogo (II com a 1ª estação) | 76:04 | 53,8 (15) | 6.352 / 7.143 | 49.111 | 7,9 | 11,3% | 26,1% | 7,8; 222.175 | 16/16 |

`--renome 400` (o corte do §33.2) dá os mesmos números (o Renome 5, 750, não é alcançado na chegada em nenhum dos dois). **Ouro de encomendas com 3 vagas: 12,1% → 11,3%**, dentro do portão de 4–12%: o knob da Row (os 70% dos segundos do §7.2) fica como está. A Quality, a Royal e a Blueprint da Row abrem com a qualidade e os projetos e pagam mais que a Common, mas a liga e os níveis sobem a venda junto.

**Diagnóstico da liga e da Rebitadeira 2** (F2P, 1 partida, `--renome 500`, 150 min, contadores de estação): a liga chega a **77 de 1.485 peças (5,2%)** — a fornalha de liga passa ~7.000 s com fome enquanto as fornalhas de minério ficam ~5.900 s travadas (lingote pronto que ninguém leva): o transporte de intermediários é um Ferraz só (o papel 1), o retrato do §33.1. A **Rebitadeira 2** fez **4 ciclos** em 150 min (a 1ª, 345): não é transporte, é demanda — os 1.390 rebites que elmos, maças, manoplas e peitorais pediram cabem numa rebitadeira (4 por lingote; ~9 rebites/min contra ~150/min de capacidade com o Fole duplo). Com o Ferraz 4 no papel 1 a 2ª faz 0 ciclos. Agora ela é a porta dos níveis da Row (desvio 1); torná-la útil pede mudar o catálogo (decisão do coordenador: por exemplo, a 2ª rebitadeira virar a 2ª fornalha de liga).

### 35.3 O Charter da Row (2 obras): Ideal e F2P × 8 × 300 min, `--renome 500`

O bot F2P não golpeia (o golpe de mestre é do perfil Ideal, `Bot.Strike`); a prova do F2P é o teste `LigaTests.Charter_DaRow_DuasObrasPeloCaminhoDeSempre` (a Row de verdade, sem conteúdo injetado: produção completa, montagem V, a Vulcana e a liga dão q 105; os projetos pelos marcos de venda; golpe numa Superior com liga = Elmo do Primeiro Vigia e Peitoral do Bastião no Hall da Row; o Charter sai 1×).

| perfil | obras no Hall (1ª; 2ª) | Charter no fim | prêmio de qualidade | ouro de encomendas | Renome do Império no fim | saldo aos 300 min (antes → depois) | min sem destino (antes → depois) |
|---|---|---|---|---|---|---|---|
| Ideal | 0 → **2,9 (195:05; 210:19)** | renome 8/8 → **pronto 8/8** | 14,2% → 38,1% | 8,8% → 8,0% | 669 → 886 | 525.379 → 540.159 | 177,3 → 122,3 |
| F2P | 0 → 0 (não golpeia) | renome 6/8, masterwork 2/8 → masterwork 8/8 | 0% → 28,7% | 12,6% → 10,8% | 740 → 821 | 884.463 → 1.036.417 | 197,4 → 152,6 |

**Leitura.** (1) O Charter da Row era inalcançável (0 obras no build) e agora fecha no Ideal às **3:30 de Row** (2ª obra aos 210 min), dentro da coluna "Charter (h no distrito)" do `CONTEUDO_V1.md` §1 (3–5 h); o Renome 5 (750) do Império vem com as encomendas, as obras e os marcos da Collection da Row. (2) Ralo: depois da produção completa a Row tem o luxo (162 mil), os níveis (236.800) e a Vulcana (~210 mil): até os 150 min o saldo parado cai de 156–199 mil para 5–51 mil e os minutos sem destino de 42–48 para 0–4. Depois de ~2 h 30 de Row tudo isso está comprado e a renda (7–7,6 mil/min) volta a acumular: o ralo seguinte é a abertura do Artisan Quarter (430 mil), quando ele existir. (3) Renda: o prêmio de qualidade (níveis, Vulcana e liga) leva a renda dos últimos 10 min a 6,9–7,4 mil/min no F2P, a "renda no Charter" do §1 (7.200).

**Portões (GDD §3, `CONTEUDO_V1.md` §16), na Row:** produção completa do F2P 63:04 (× 8, Renome 0) e 58:46 (× 16, Renome 500), janela 55–70 — ok; maior sequência morta do F2P 13 e 15 (≤ 15) — ok; renda nos 10 min depois da produção 4,6–5,3 mil/min contra 4.800 (−4% a +10%; aceite ±25%) — ok; ouro de encomendas 11,3% (4–12%) — ok; nenhuma obra pede anúncio ou compra e todo projeto essencial tem fonte grátis permanente (validator) — ok; Charter por distrito 3:30 de Row no Ideal (3–5 h) — ok. FTUE e os 10 primeiros minutos são do Old Forge: iguais (§35.5).

### 35.4 Testes e prova vermelha

`LigaTests` (11): `FornalhaDeLiga_DoisLingotesUmaLigaEmSeisSegundos` (tier 11, 13.000, pede a Fornalha 2, pad no lugar da estação, 6 s e 3,75 s com o Fole), `Montagem_LigaOpcional_Q30EConta` (todo produto da Row tem o slot, insumo não; a peça não espera a liga; Material +30; a liga sai com a peça; 1 Fina a cada 4 com liga), `PecasComLiga_SaveEOldForgeIntocado`, `Vulcana_ContrataEmOuro_FornalhaEMontagens` (7.200 depois da fornalha; 6 → 4 s; q 15/25/30 nas montagens e 0 nas forjas; nível 10 no save), `Niveis_DaRow_IIaVComOProjetoDeMestre`, `Filas_DaRow_TecnicasEMestre`, `Charter_DaRow_DuasObrasPeloCaminhoDeSempre`, `Obras_CondicoesDeLigaEVulcana`, `Fotos_EquipeNaRow` (o `-equipe` na Row), `Validator_SlotDeLigaEFundidor` e `Textos_DaLigaNosTresIdiomas` (obras, condição "com liga", projetos, marcos e o cartão da Vulcana em todo nível nos 3 idiomas). `RowTests.Validator_ObrasDaRow_PassamComALiga` substitui o `…EsperamALiga` (as 10 passam; sem a liga as 4 da forja voltam a "Superior inalcançável"; sem a Vulcana, a Maça do Juiz). Ajustados: o rodízio da Row (a Blueprint abre), os 18 produtivos, o Hall da Row, o distrito inexistente dos projetos (agora o `artisan_quarter`) e a regra do insumo com 0.

**Prova vermelha** (`python client/tools/mutantes/mutantes.py --so liga-… --so row-…`, versionadas no `mutantes.py`): **17 mutações, todas mortas** (0 vivas, 0 inválidas, 2.823 s) — `liga-sem-q` (a liga não soma: o Charter e a Fina a cada 4 caem), `liga-nao-consome` (qualidade sem gastar a liga), `liga-sem-contador` (PecasComLiga parado), `liga-sem-slot` (montagem e forja sem o slot: o validator recusa as 10 obras, 8 testes), `liga-save-sem-linha`, `liga-load-ignora`, `liga-vulcana-sem-nivel` (sem +25/+30), `liga-vulcana-sem-ritmo` (a fornalha de liga não acelera), `liga-vulcana-toda-familia` (q nas forjas também), `liga-cond-sem-valor` (AlloyPieces nunca anda), `liga-validator-slot`, `liga-validator-fundidor`, `liga-qmax-sem-liga` (as obras da Row voltam a "Superior inalcançável"), `row-niveis-na-producao` (o desvio 1 desfeito), `row-sem-projetos`, `row-vulcana-sem-requisito` e `row-texto-item-do-oldforge` (o peitoral com o nome de outro item).

### 35.5 Old Forge idêntico ao byte (`main` a8e693b, #66 × este branch)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico, `cmp`) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…d3a96` (idêntico, `cmp`) |

F2P × 8: produção completa 47:14, zona morta 19,6, saldo 619 — os do §34.2. A `main` foi extraída com `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário; cada `econsim.csv` foi movido para um nome próprio e comparado com `cmp`.

## 36. Metas de sistemas: contrato, Charter, Collection, projetos, Renome, qualidade, golpe, evento e Passe (raia H, 2026-10-15)

**Vigente** (`Core/MetasDefs.cs`, `Core/Metas.cs`, `Core/LiveOpsDefs.cs`, `Core/LiveOps.cs`, `Core/Sim.cs`, `Core/ContentValidator.cs`, `View/Textos.cs`, `View/Loc.cs`, `View/PainelMetas.cs`; branch `feat/metas-sistemas`). As metas do `LIVEOPS_V1.md` §4.3–§4.5 marcadas ✗ cujo sistema já existe entraram em dados: **3 semanais** (`wkl_contrato1`, `wkl_superior20`, `wkl_golpe50`, recompensa padrão XP 350 + 60 s + Renome 5; só entram no rodízio com um contrato a cumprir / com a Quality aberta, `Metas.Livre`) e **21 conquistas** (Districts: `ach_charter1`, `ach_elmos100`, `ach_contrato1` XP 150, `ach_contratos12` insígnia "Arsenal do Reino"; Quality: `ach_primeira_fina`, `ach_primeira_superior`, `ach_fina100`, `ach_superior1k`, `ach_golpe1`, `ach_golpe100`; Blueprints: `ach_bp1`, `ach_bp10`, `ach_bp18`; Automation: `ach_estacao_n3`, `ach_estacoes_n5`; Renome/Collection: `ach_renome1`, `ach_renome4`, `ach_colecao_produtos`; LiveOps: `ach_evento1`, `ach_eventos4`, `ach_passe_free`), com as recompensas do catálogo (só ouro em s de renda, XP do Passe, Renome e insígnia = posse pelo ID; V9). Gatilhos: **contados** (`mt.c`) — `quality_crafted`/`quality_superior` pelo `Ev.Graded`, `master_strike` pelo `Sim.Strike` (o do distrito também na raiz), `event_enter` pelo `LiveOps.Entra`, `contract_complete` (já contava desde o #64; o retroativo agora semeia pelo `ctr=`) e a venda de item que a raiz não tem pelo ID (`product_sold.elmo`, C-CORE-03: o elmo da Rua dos Armeiros chega à raiz sem virar espada); **derivados** do estado do núcleo (`Metas.Valor`, retroativos, nada novo no save) — Charter pelo `charter:<distrito>` e marco da Collection pelo `col:<marco>` no ledger do Renome, projetos possuídos, nível do Renome, famílias com nível de estação ≥ N, trilha de evento fechada (o último limiar pago, `e:{evento}:1000`) e trilha free do Passe fechada (o nível 30 resgatado, `p:{passe}:30:f`). `MetaDef.Ref` = filtro por ID (item de outro distrito, distrito do Charter, marco). Conquista de Renome não dá Renome; a de Charter, Collection e Masterwork dá só XP ou insígnia (a fonte já deu o Renome).

**Royal Week:** a missão `contrato1` (`contract_complete`, 1, 100 pontos de evento, V16) entrou no lugar dos 10 golpes de mestre (o cartão do evento mostra 8 linhas: `LiveOpsDefs.MaxMissoes`, agora regra do validator). O golpe segue no Armorer's Challenge e nos 2 Hunts.

**Validator (`ContentValidator.Missoes`):** alvo de conquista dentro do conteúdo do build (`requiresContent`: 12 contratos, 1 Charter por distrito, 19 projetos, nível 10 do Renome, 4 famílias com nível V, 10 eventos, 2 Passes), filtro de item/nível nos gatilhos novos, `Ref` só onde vale e que exista (produto de algum distrito, distrito com Charter, marco da Collection), Renome que não dá Renome. **Decisão por padrão (revisar):** os 9 contratos dormentes (distritos 3–6) contam como conteúdo, então `ach_contratos12` aparece "x/12" até a Wave C; para escondê-la, a régua vira `ContratosDefs.NoBuild`.

**Ficaram de fora (registrado):** as 10 diárias do pool C (qualidade, golpe, Renome, evento: mudam o slot C de todo jogador com a Quality aberta; ticket próprio com o golden do rodízio), `wkl_evento350` (regra "slot 1 do evento"), `dly_golpe_superior1`/`ach_golpe_superior50` (o golpe não sabe o nível com que a peça saiu), `ach_workers20` (contratação do distrito não sobe à raiz), `ach_distritos3/6`, `ach_mw30/60` e `ach_colecao_clientes` (conteúdo ou aba que não existe), e nenhuma de manager (o catálogo não lista).

### 36.1 econsim antes (`main` a8e693b) → depois (este branch), `cmp` dos CSVs

| rodada | resultado |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | **idêntico** (`9f3f548e…28da3fdb3`): produção completa 47:14, zona morta 19,6, Renome 1 14:35, Renome 2 48:00, saldo 619 |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | **idêntico** (`3acf0f6f…08d3a96`) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | **idêntico** (`68a9e33d…5c91b64625`) |
| `--evento royal_week --minutos 60 --partidas 4 --anuncio-seg 30` | só o Ideal (o único perfil que golpeia) muda: pontos das missões 660 (5/8) → 549 (4/8), ouro dos limiares 2.699 → 1.393, renda/min depois da entrada +7,4% → +6,0%; F2P, Slow, AdWatcher e AdWatcherE idênticos (F2P 571 pontos, 4/8, +1,7%) |

O bot não resgata metas, então o ouro, o Renome e o XP das conquistas novas não entram em nenhum CSV; os 10 primeiros minutos e os portões do GDD §3 / `ECONOMIA_WAVE_B.md` §10 seguem os da §32.3 (Renome 1/2/3/4 do F2P: 14:18 / 50:47 / 1:40:54 / 3:01:24).

### 36.2 Pass, Renome e ouro (conta, não medida: o jogador real que resgata)

- **Pass:** a conta estática (`LiveOpsDefs.XpDiaF2P`: o menor XP das diárias e das semanais, encomendas, trilhas) não muda (as 3 semanais novas dão os 350 de sempre): S1 651 XP/dia → **dia 27,6** (desenho) e 671 → 26,8 com as encomendas medidas; S2 651 → 27,6 / 657 → 27,4 (portão 26–30 e "~28–31"). As 21 conquistas somam **1.650 XP uma vez** (2,75 níveis), fora da estimativa como o `LIVEOPS_V1.md` §3.2 pede (bônus para quem entra no meio da temporada); quem já tinha os sistemas recebe parte pelo retroativo (Renome, projetos, nível de estação) na 1ª abertura.
- **Renome:** só `ach_primeira_superior` (+3), `ach_golpe100` (+5) e `ach_bp10` (+5) dão Renome (+13 uma vez); as semanais novas dão os +5 de sempre. HIPÓTESE (o bot não resgata): até o Renome 4 (400) o F2P real alcança no máximo o `ach_bp10` (+5, ~1,3% do limiar) — o F2P não golpeia e só faz Superior pela automação perto do nível IV —, então os portões Renome 2/3/4 (40–75 min / 1,5–3 h / 3–6 h) não se mexem de forma mensurável. As vagas de encomenda (limiar 180, §30.6) também não: nada novo paga Renome antes do Renome 3 no F2P.
- **Ouro:** 3 conquistas com ouro (30 s cada, uma vez) e as semanais com os 60 s de sempre: o orçamento do validator (diárias 3 × 20 s + semanais 4 × 60 s / 7 + login) é o mesmo, **177 s ≤ 180** com Pass e evento (§27.3).

### 36.3 Trilha de evento (`LiveOpsTests.TrilhaDeEvento_*`, o F2P de referência da §28.6)

Tabela **idêntica** à vigente da §30.6 (60 min seguidos / dia em que fecha): Royal Week 605 / 5, Forge Frenzy 785 / 4, Armorer's Challenge 810 / 5, Hunt da Sunblade 840 / 4, Forge Frenzy-B 765 / 4, Blacksmith Festival 700 / 5, Merchant Caravan 780 / 5, Hunt da Ember Crown 820 / 4, Royal Week-B 810 / 4. Sonda da Royal Week (a referência começa no D0, do zero): Renome 3 no dia 5 (sessão da manhã, Renome 181), o ronda cumprido na sessão da tarde do dia 5 junto com as 2 Royal, trilha em 1.120 nessa sessão (1.020 sem o contrato: fecharia no mesmo dia). Os 10 golpes nunca contavam para a referência (não golpeia); o contrato passa a contar só no fim — o portão "dia 4–5, nunca numa sessão de 60 min" fica igual.

### 36.4 Testes e prova vermelha

`MetasTests` (+5): `ContratoEElmo_VendidosNaRua_ContamNaRaiz_PagamUmaVez` (contrato da raiz cumprido com elmos vendidos na Row pelo Império: `ach_contrato1` paga XP 150 sem ouro 1×, `ach_contratos12` 1/12, `ach_elmos100` pelo ID e sem virar espada, save do Império leva `product_sold.elmo`), `SemanalDeContrato_SoComUmACumprir_FechaPeloContrato_UmaVez` (25 semanas sem contrato a cumprir: nunca; com o ronda aberto entra, fecha pelo `Ev.ContractDone`, paga 1×, save/load), `QualidadeEGolpe_PeloCaminhoReal_PagamUmaVez_SemanaisSoComAQualityAberta` (Superior pela automação = `Ev.Graded`, golpe de verdade, Fina não é Superior, as semanais de Superior e golpe só com a Quality aberta), `Derivadas_ProjetosEstacaoRenomeColecaoECharter_RetroativasUmaVez` (9 projetos antes da 1ª abertura = retroativo; nível II não é o 3; Renome 4 fecha as 2 sem dar Renome; outro marco da Collection não conta; o Charter do Old Forge pelo `Imperio.Charter`) e `SaveAntigo_ContratoConcluido_ConquistaPeloRetroativo`; `Eventos_TodoGatilhoContaNoJogo` cobre os 11 gatilhos novos pelo caminho real (sonda em `Metas.Valor`); golden do rodízio semanal (20 modelos) e o validator com 14 defeitos novos. `LiveOpsTests`: `Conquistas_EventoEPasse_PeloLedger_UmaVez` (999 pontos não fecham; reabrir o evento não conta outra entrada; nível 30 alcançado sem resgatar não fecha) e o validator com 9 missões. `ContratosTests`: a Royal Week de verdade paga 100 pontos pelo contrato. `ImperioTests`: o golpe da Row conta nas metas da raiz. 478 testes no `coretests`.

**Prova vermelha** (`python client/tools/mutantes/mutantes.py --so meta-…`, versionadas no `mutantes.py`): **20 mutações `meta-*`, todas mortas** (0 vivas, 0 inválidas, 3.333 s): Fina/Superior que não conta (`Ev.Graded`), Fina que conta como Superior, golpe que não conta, elmo da Row sem o ID, Charter lido no ledger errado, qualquer marco da Collection fecha Produtos 100%, projeto contado a mais, nível de Renome pelo total, qualquer nível de estação vale o pedido, trilha de evento "fechada" no 1º limiar, Passe "fechado" no nível 1, entrada em evento que não conta, semanal de contrato sem contrato a cumprir, semanais de Superior/golpe antes da Quality, save antigo sem o retroativo do contrato, validator sem o teto do conteúdo / sem a regra do laço de Renome / sem conferir o ID, a Royal Week de volta aos golpes e o validator sem o teto de 8 missões.

## 37. C-CORE-06: Artisan Quarter — bancada configurável e clientes por família (2026-10-15)

**Vigente** (`Core/Distritos.ArtisanQuarter.cs`, `Core/Sim.Bancada.cs`, `Core/Sim.Clientes.cs`, `Core/Bot.Bancada.cs`; ganchos em `Core/Sim.cs`, `Core/Bot.cs`, `Core/Defs.cs`, `Core/Distritos.cs`, `Core/Encomendas.cs`, `Core/EncomendasDefs.cs`, `Core/ProjetosDefs.cs`, `Core/Projetos.cs`, `Core/MasterworksDefs.cs`, `Core/Masterworks.cs`, `Core/Equipe.cs`, `Core/Imperio.cs`, `Core/ContentValidator.cs`; `View/Loc.cs`, `View/Textos.cs`, `View/Game.cs`, `View/WorldView.cs`, `View/Captura.cs`; `tools/econsim`; testes em `Tests/EditMode/ArtisanTests.cs`; branch `feat/artisan-quarter`). Só o Quarter entrou e o Charter da Row passou a liberá-lo; o Old Forge e a Row são os da `main` 3be2c29 (já com o transporte da §39), ao byte (§37.6).

- **Distrito 3 em dados** (`CONTEUDO_V1.md` §2.3, §3, §3.1, §4, §5.3, §5.7, §6): 13 itens (handles 0/1 = minério e lingote de **cobre**, 2–8 = fivela, cinzel, castiçal, broche, cálice, colar, caixa de joias, depois ouro e gemas), 10 receitas, 13 estações (kit: depósito de cobre, crisol de cobre, 1 bancada de ourives, loja fina de 3 vagas), 10 pads, 18 produtivos + 3 luxos + ourivesaria II–V (IV–V com o `bp_mestra_artisan`), turma Dourado (3 Porters, um por corpo, até o campo `crew`) e a Lúcia (Smith, 22.000), manager Valentim (`Imperio.Managers`), abertura 430.000, Charter = produção completa + Renome 6 (1.250) + 3 obras, sem próximo no build. Rótulos `est.artisan_quarter.deposito/fornalha/balcao` (Minério cobre, Crisol cobre, Loja fina): os IDs `deposito/fornalha/balcao` seguem os papéis fixos do `Sim`.
- **Bancada configurável** (§2.3 regras 1–2; `Sim.Bancada`): estação cujas receitas do distrito têm a mesma `Station` (as 7 da ourivesaria). A receita ativa é o `Recipe` de sempre; `RecipeDef.Abre` = o upgrade que libera cada uma (fivela e cinzel do kit). Toque na barra da bancada (View) ou o bot pedem a receita (`Sim.Configura`); a troca termina a peça em curso, **espera a saída esvaziar** e para `TempoTroca` = 4 s (troca rápida 1 s; o oficial da família −1 s; mínimo 1 s). Os insumos que a nova receita não usa ficam guardados na entrada (`Station.Guardados`, até o `InCap` 6 = 2 × o maior insumo das 7) e voltam quando uma receita os usa: nada se perde. Carregadores e o ferreiro só entregam o insumo da receita ativa (o `Accepts` de sempre); o Dourado dos lingotes só pega o que cabe numa entrada aberta e só espera numa fonte que trabalha: as regras do transporte da Row (§39), que o Quarter ganha de graça (3 insumos feitos em estação: lingote de cobre, lingote de ouro e gema). O gancho próprio que o branch tinha para isso (não buscar o que nenhuma receita ativa usa) saiu no merge do #76: a regra 1 da §39 já cobre. Linha aberta, encomenda (`Fracao`), Hall e validator contam a bancada por toda receita liberada (`Sim.Faz`). Save: linha opcional `ofcN=receita,próxima,parada,guardados` (só fora do padrão).
- **Clientes por família** (§12; `Sim.Clientes`, genérico: o Arsenal usa depois): `ClientDef` com unidades por visita, paciência ×, intervalo ×, qualidade mínima, `Rico` e o rodízio fixo de pedidos (n-ésimo cliente da família → n-ésimo produto aberto da lista; sem lista = todo produto). `DistrictDef.Clientes` null = o relógio por produto de sempre (Old Forge e Row). No Quarter: viajante (fivela, castiçal), mercador (2 por visita, paciência × 0,8, intervalo × 1,3), nobre (colar, broche, cálice, caixa; × 1,8 / × 2,5) e colecionador (qualquer produto, Fina ou melhor, × 2 / × 3, Renome 2). Família com qualidade mínima só chega quando a automação já faz esse nível (senão prende a vaga). O mercador leva 1 por atendimento, como o VIP. `ClientesRicos` (o RichClients do §2.3 regra 5: nobres + colecionadores atendidos) e o rodízio vão no save (`ricos=`, `fam=`, opcionais).
- **Bot** (§2.3 regra 4, refinada; `Bot.Bancada`): para cada bancada sem troca pendente, falta(p) = unidades que a fila quer de p − estoque − saída + 1 de cada OUTRA bancada em p; pede a receita liberada de maior falta (empate: maior preço por insumo) quando ela passa a da atual em `Histerese` = 3. Toca sem andar (como o menu). `PoliticaBancada.Volume/Margem` = as políticas puras do portão (§37.3).
- **Conteúdo do distrito**: encomendas (§7: os 7 tipos com Mixed × 2, a variante Superior do Quality no Renome 6 e a Condessa Amara, `ord_reg_amara`: 4 colares Fina), os 14 projetos (§9; essenciais com fonte grátis permanente: mestre = Royal #1 ou produção completa; fivela do viajante = 200 fivelas; colar da aurora = fila #3 ou 200 colares; broche da condessa = Royal #2 ou 300 broches), as 10 obras (§10.1; condições novas `ClientesRicos` e `Feitos` = GemsCut, pelas gemas que os lapidários fizeram, `Station.Crafted` no save), Superior alcançável (§10.2: 60 + 20 + Lúcia 25 = 105; +15 da lapidação no colar e na caixa), marcos da Collection (11/21 produtos, 4/8 workers, 7/14 projetos, o estandarte das 10 obras) e os contratos `ctr_obras_castelo` e `ctr_dote_princesa` abertos com o Quarter no build (V28; o cartão mostra a qualidade mínima comum 1×: "Pede: 10 colares + 10 caixas de joias + 20 broches · Fina+").
- **Chegada** (`Imperio.Chegada`, dev e econsim): a cadeia inteira de Charters até o distrito (Old Forge → Row → Quarter), cada anterior em produção completa e a renda de Charter do modelo do §1 (Old Forge 2.400/min, × 3 por distrito: a Row a 7.200) → fundo 600 + 1.800 = **2.400/min** ao chegar (§5.3). Para a Row dá o de antes, passo a passo (§37.6).
- **Validator**: a bancada (receita de fábrica do kit; `Abre` = produtivo do distrito; receitas do tipo da bancada e de produto) e as famílias (ID, unidades, paciência, intervalo, qualidade até Superior, pedido = produto do distrito com cliente, a 1ª família aberta desde o kit pedindo o produto do 1º cliente); as regras de receitas, Hall, projetos e encomendas contam a bancada por toda receita; técnica pode mirar um produto; handles 0/1 do distrito = o bruto do depósito do kit e o lingote que a fornalha do kit faz dele (antes: os IDs `minerio`/`lingote`).
- **View**: painel da bancada no toque da barra ("Receita: fivela", "Depois: colar (troca de 4 s)", botão "Fazer: cinzel" = próxima receita liberada em rodízio); a estação é refeita quando a receita muda (pilhas, bocas e ícone do item novo). Arte emprestada: bancada = a joalheria do Old Forge. Fotos: `-distrito artisan_quarter -testsession -buy 25 -receitas fivela,colar,caixa_joias` (+ `-botwarmup S`).

**Decisões e desvios (revisar):**
1. **Preços do §3 × 3 e produtivos do §5.3 × 1,5** (2.160 … 180.000; 794.565 no total): como na Row (V28), o Quarter é preso no transporte (3 Dourado com carga 2 levando minério, lingote e peça; a bancada parada na troca), ~12 peças/min contra ~56 do modelo do §1. A relação entre os produtos é a do catálogo.
2. **Intervalo da linha único, 12 s, nas 7** (o catálogo deriva um por linha do modelo: fivela 4,2 … caixa 14): assim o × 1,3 / × 2,5 / × 3 das famílias vale como o §12 diz (os ricos chegam 2,5–3× mais espaçados). Com o intervalo por linha, o nobre (4 produtos no rodízio) chegava a cada ~26 s e os ricos davam 20% da receita; com 12 s, 28–29%.
3. **Encomendas**: Bulk, Quality, Mixed, Royal e a Amara a 70% dos segundos do §7.2 (o desvio do Old Forge e da Row) e o prêmio de toda encomenda do Quarter × 0,85 (`Tuning.OrderRewardSeconds` 8,5 no `Ajuste`): com 7 linhas o N cai no mínimo e o ouro de encomendas ia a 12,9% da venda.
4. **A troca espera a saída esvaziar** (o catálogo só fala da peça em curso): a pilha de saída é de um produto só.
5. **Bot**: "mais clientes na fila" virou falta = fila − estoque − as outras bancadas, com histerese 3 (varredura F2P × 8: histerese 1 = 615 trocas/h, 57:10, 15,5 mil/min; 3 = 212/h, 58:07, 16,4 mil; 6 = 95/h, 60:36, 14,3 mil).
6. **Lúcia** é Smith: q +15/+25 e o passo da família (a regra de sempre) e −1 s na troca; o catálogo dá só a qualidade e a troca.
7. **Fole de ourives acelera também o lapidário** (é fornalha; o catálogo põe o lapidário só no fole duplo). **Ourivesaria II pede o Fole de ourives** (o 1º produtivo; o "1ª estação da família" do §5.7 é do kit e o validator pede requisito produtivo): ao contrário da Row, a janela da produção completa fecha sem adiar os níveis.
8. **Técnica da lapidação = técnica de produto** (+15, `Tuning.TecnicaItemQ`, no colar e na caixa); a de família segue +20.
9. **`bp_mw_caixa_rainha` sem a 2ª fonte** (Contract: `ctr_dote_princesa`), como o peitoral do rival da Row: o contrato é do Império e dá o `bp_var_dote` na raiz.
10. **Dourado**: 3 Porters (rotas minério, lingote, peça) até o `crew`; carga +1 nos níveis 3 e 5 (o 8 do catálogo espera a regra das turmas).
11. **Reafinado depois do transporte da `main` (#76, §39).** Com as regras de transporte o Quarter andou mais rápido (F2P 58:07 → 57:06, renda 10 min após a produção 16,4 → 16,7 mil, encomendas 11,1% → 11,2%) e o Charter do Ideal caiu de 4:27 h para **3:54 h** (Renome 6 aos 224 min), abaixo dos 4–6 h. Dois knobs no `Ajuste`, nenhum na mecânica: os **níveis da ourivesaria II–V × ~1,5** (36.000 / 105.000 / 290.000 / 630.000; o §5.7 dá 24.000 / 71.000 / 190.000 / 420.000) e o **alvo das encomendas do Quarter × 1,2** (`Tuning.OrderSeconds` 180; o padrão é 150: encomendas maiores, menos por hora, o Renome anda mais devagar e o ouro de encomendas cai de 11,2% para 9,9%). Resultado: produção do F2P 60:30, Charter do Ideal 4:13 h (§37.1, §37.4).

### 37.1 Catálogo → afinado → vigente (`--distrito artisan_quarter --perfis F2P,Slow,Ideal,AdWatcher --minutos 150 --partidas 8 --anuncio-seg 30 --renome 850`)

Renome 850 = o do Charter da Row (750 + 100). "Catálogo" = preços, custos e intervalos do `CONTEUDO_V1.md` (o resto como o afinado), sobre a `main` fe195d6; "afinado" = o mesmo branch antes do merge do #76; **vigente** = depois do merge (`main` 3be2c29, transporte da §39) e da decisão 11.

| perfil | produção completa | zona morta (min; maior seq.) | renda/min 10 min após a produção | renda/min últimos 10 min | saldo final | prêmio de qualidade | ouro de encomendas | trocas/h | ricos atendidos | meia-volta (fila cheia) |
|---|---|---|---|---|---|---|---|---|---|---|
| F2P | 94:21 → 58:07 → **60:30** | 76,1 (17) → 39,6 (11) → 41,9 (**9**) | 4.192 → 16.386 → **15.973** | 3.779 → 13.473 → 17.898 | 23.261 → 91.137 → 63.892 | 19,6% → 23,7% → 23,7% | 11,0% → 11,1% → **9,9%** | 205 → 212 → 242 | 344 → 532 → 609 | 6.919 → 3.333 → 3.104 |
| Slow | 114:44 → 69:20 → 72:44 | 96,3 (21) → 49,9 (15) → 53,1 (15) | 3.228 → 10.681 → 12.902 | 3.126 → 13.877 → 17.553 | 15.203 → 28.760 → 80.109 | 17,9% → 23,0% → 23,0% | 8,5% → 10,4% → 9,7% | 159 → 178 → 199 | 247 → 442 → 497 | 7.098 → 3.527 → 3.378 |
| Ideal | 101:11 → 61:01 → 61:12 | 82,8 (18) → 42,6 (12) → 42,8 (11) | 3.321 → 13.571 → 16.954 | 3.557 → 14.339 → 17.208 | 15.057 → 79.749 → 68.165 | 29,1% → 32,2% → 30,8% | 9,0% → 9,5% → 8,8% | 168 → 184 → 214 | 253 → 433 → 529 | 7.085 → 3.562 → 3.350 |
| AdWatcher | — → 61:23 → 60:58 | — → 42,9 (11) → 42,6 (14) | — → 13.116 → 15.803 | — → 12.795 → 17.477 | — → 66.135 → 101.335 | — → 23,0% → 23,0% | — → 9,7% → 8,1% | — → 195 → 219 | — → 422 → 493 | — → 3.638 → 3.447 |

Mix (F2P, % da receita; catálogo → afinado → vigente): fivela 19,2 → 16,5 → 16,5 · cinzel 16,8 → 15,6 → 15,3 · castiçal 44,9 → 39,9 → 40,1 · broche 3,6 → 5,0 → 5,2 · cálice 4,3 → 6,5 → 6,1 · colar 5,0 → 7,8 → 8,4 · caixa 6,0 → 8,6 → 8,3 (ricos 18,9% → 27,9% → 28,0%). Fundo 2,2–2,6 mil/min; Lúcia contratada 8/8 (~67 min, nível 8,9 aos 150). Só o merge, sem a decisão 11 (`main` 3be2c29, níveis e alvo de antes): F2P 57:06, maior sequência 9, renda 16.731 / 19.396, encomendas 11,2%; Ideal 58:36 e 10,4%. O Slow passa dos 70 min (72:44): o portão é do F2P.

**Varredura** (F2P × 4–8, 150 min): com os intervalos por linha do catálogo, preço × 3,5 e custo × 1,5 / 1,6 / 1,75 = 54:30 / 67:46 / 76:07 (renda 14,7 / 13,4 / 16,0 mil); intervalos × 3 em todas as linhas não mudam a renda (a demanda sobra: meia-volta 7.000 → 1.300); só as linhas ricas × 0,75 levam os ricos a 47% e a renda a +70% (24 mil/min com × 3,5): a margem rende mais por insumo transportado, e o que segura o rico é a demanda. Com o intervalo único de 12 s: × 2,5 / 1,4 = 62:01 e 11,5 mil; × 3 / 1,5 = 58:09 e 13,4 mil, encomendas 12,9% → 11,1% com o prêmio × 0,85.

### 37.2 Chegada com Renome 0 (`--perfis F2P --minutos 150 --partidas 8`, sem `--renome`)

Produção completa 64:47 (antes do merge 62:34); maior sequência morta 13; renda 13.374 / 17.801 por min; **ouro de encomendas 5,4%** (vaga única) e 9,9% com as 3 vagas do Renome 850 — os dois no portão de 4–12%.

### 37.3 Portão da mecânica: margem × volume (`--bancadas regra|volume|margem`, F2P e Ideal × 8 × 150 min, `--renome 850`)

| política | produção completa (F2P / Ideal) | renda/min 10 min após (F2P / Ideal) | renda/min últimos 10 (F2P / Ideal) | trocas/h (F2P) | ricos atendidos (F2P) |
|---|---|---|---|---|---|
| regra (a do bot) | 60:30 / 61:12 | 15.973 / 16.954 | 17.898 / 17.208 | 242 | 609 |
| só volume (fivela, cinzel, castiçal) | 143:23 (7/8) / 127:21 | 3.793 / 5.447 | 4.095 / 5.078 | 30 | 41 |
| só margem (colar, broche, cálice, caixa) | não fecha / não fecha | — | 549 / 1.153 | 8 | 85 |

**Portão (decidido por padrão, revisar): nenhuma política pura se sustenta e a mistura rende ≥ 2× a melhor delas** — passa (17,2–17,9 mil contra 4,1–5,1 mil; antes do merge 13,5–14,3 contra 3,7–4,2). Só volume deixa nobres e colecionadores (paciência × 1,8–2) presos nas 3 vagas da loja; só margem deixa viajantes e mercadores esperando o que não sai e o ouro e as gemas não dão vazão. A decisão de quantas bancadas ficam em cada lado é o que faz a renda. **Teste negativo do §14** (troca de receita não perde insumo): `ArtisanTests.Troca_NaoPerdeInsumo`.

### 37.4 O Charter do Quarter (3 obras): Ideal e F2P × 8 × 420 min, `--renome 850`

| perfil | obras no Hall (1ª; 2ª) | Renome do Charter (min) | Charter pronto (min) | prêmio de qualidade | ouro de encomendas | Renome do Império no fim | saldo final | min sem destino |
|---|---|---|---|---|---|---|---|---|
| Ideal | 6 (239:12; 246:31) | 246 (8/8) | **253 (8/8)** | 37,1% | 8,5% | 1.634 | 3.726.977 | 178,5 |
| F2P | 0 (não golpeia) | 307 (8/8) | — (falta a obra 8/8) | 31,2% | 9,3% | 1.370 | 3.810.655 | 181,1 |

O Ideal fecha o Charter às **4:13 h de Quarter** (antes do merge 4:27 h; só o merge, sem a decisão 11, 3:54 h), dentro da coluna "Charter (h no distrito)" do §1 (4–6 h): Renome 6 com as encomendas, as obras e os marcos; a ourivesaria V e a Lúcia 10 já compradas. O F2P não golpeia (o golpe é do perfil Ideal); a prova dele é o `ArtisanTests.Charter_DoQuarter_TresObrasPeloCaminhoDeSempre` (o Quarter de verdade: produção completa, ourivesaria V, Lúcia 5 e a técnica da fila #1 = q 105; os 3 projetos essenciais pelos marcos de venda; RichClients 100; golpe numa Superior da bancada em cada receita = fivela do viajante, colar da aurora e broche da condessa; o Charter sai 1×). **Achado (ouro sem destino):** depois de ~4 h tudo está comprado e a renda (17–19 mil/min) acumula 3,7–3,8 milhões: o ralo seguinte é a abertura do Royal Arsenal (1,3 milhão), quando ele existir.

### 37.5 Portões (GDD §3, `CONTEUDO_V1.md` §16, §35.3)

Produção completa do F2P 60:30 (× 8, Renome 850) e 64:47 (Renome 0), janela 55–70 — ok; maior sequência morta do F2P 9 e 13 (≤ 15) — ok; renda nos 10 min depois da produção 16,0 mil (+11%) e 13,4 mil (−7%) contra 14.400 (aceite ±25%) — ok; ouro de encomendas 9,9% e 5,4% (4–12%) — ok; Charter 4:13 h de Quarter no Ideal (4–6 h) — ok; nenhuma obra pede anúncio ou compra e todo projeto essencial tem fonte grátis permanente (validator) — ok; margem × volume (§37.3) — ok. FTUE e os 10 primeiros minutos são do Old Forge: iguais (§37.6).

### 37.6 Old Forge e Row idênticos ao byte (`main` 3be2c29 × este branch)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico, `cmp`) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…08d3a96` (idêntico, `cmp`) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `2590bba6…be815fd` (idêntico, `cmp`: o Charter da Row apontar para o Quarter e a chegada pela cadeia não mexem na Row) |

A `main` foi extraída com `git archive 3be2c29 client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário; cada `econsim.csv` foi movido para um nome próprio e comparado com `cmp`. A Row mudou de propósito no #76 (antes, contra a fe195d6: `f7c3f5c2…a2a0eca`, também idêntico); o Old Forge é o mesmo nas duas `main`. A decisão 11 só mexe no `Ajuste` do Quarter.

### 37.7 Testes e prova vermelha

`ArtisanTests` (19): `Kit_DoQuarter_NumerosDoDistrito`, `Chegada_CadeiaDeCharters_FundoDosDois`, `Bancada_FazQualquerReceitaLiberada`, `Troca_TerminaAPecaEsperaASaidaEParaQuatroSegundos`, `Troca_NaoPerdeInsumo` (§14), `Carregadores_SoEntregamOInsumoDaReceitaAtiva`, `OldForgeERow_SemBancadaNemFamilia`, `Familias_RodizioFixoUnidadesPacienciaIntervalo`, `Colecionador_FinaOuMelhor_RichClientsNoSave`, `Bot_ReceitaComMaisClientesNaFila`, `BotF2P_ChegaAProducaoCompleta`, `Encomendas_DoQuarter_RodizioEAmara`, `Contratos_DoQuarter_AbremNoBuild`, `Charter_DoQuarter_TresObrasPeloCaminhoDeSempre`, `Obras_RicosGemasELapidacao`, `Validator_BancadaEFamilias_DefeitosInjetados`, `Layout_RotuloDoDepositoNaoEncostaNoDaEstacaoDeCima` e `Textos_DoQuarterNosTresIdiomas`, `Fotos_ReceitasNoQuarter`. Ajustados: o "distrito/item inexistente" dos validators de encomendas, projetos e contratos (era `artisan_quarter`/`cinzel`; agora `bairro_fantasma`/`item_fantasma`), a lista de distritos do `Validator_MapaReal_SemViolacao` e o título do freguês de outro distrito no `LocTests` (com o item dele); o kit confere também os níveis e o alvo das encomendas da decisão 11. 513 testes no `coretests` (com os da §39).

**Prova vermelha** (`python client/tools/mutantes/mutantes.py --so aq-…`, versionadas no `mutantes.py`): **36 mutações `aq-*`, todas mortas** (0 vivas, 0 inválidas, 5.224 s, de novo sobre a `main` 3be2c29): troca que joga fora a entrada, sem parada, sem esperar a saída, nunca aplicada pelo `Produce`, troca rápida e Lúcia ignoradas; toda receita liberada desde o kit; linha só pela receita ativa; entrada de 4; save/load sem a linha `ofcN=`; família sem rodízio, sem o intervalo ou a paciência dela; mercador com 1 por visita ou saindo depois da 1ª peça; colecionador levando Normal, chegando antes do Renome 2 ou sem Fina na automação; RichClients que não conta ou não vai no save; bot sem histerese ou sem o desempate por ouro; técnica de produto zerada; condições RichClients e GemsCut paradas; chegada só com a Row; validator sem a regra da bancada ou da família; vitrine de luxo sem as vagas; preços, custos, níveis e alvo de encomenda do catálogo; o Charter da Row sem o Quarter; o rótulo do depósito do Quarter com o do Old Forge. A `aq-carregador-sem-destino` saiu com o gancho dela (decisão 11 e o bullet da bancada: a regra 1 da §39 cobre, com as `rowp-*`).

## 38. C-CORE-07: Royal Arsenal e o pátio de cotas (2026-10-16)

**Vigente** (`Core/Distritos.RoyalArsenal.cs` — `DistrictDef`, famílias de cliente, encomendas, projetos, obras e marcos do distrito —, `Core/Sim.Cotas.cs`, `Core/Bot.Cotas.cs`; ganchos em `Core/Sim.cs`, `Core/Bot.cs`, `Core/Defs.cs`, `Core/Distritos.cs`, `Core/Distritos.ArtisanQuarter.cs` (o Charter aponta para o Arsenal), `Core/Imperio.cs`, `Core/Encomendas.cs`, `Core/EncomendasDefs.cs`, `Core/Contratos.cs`, `Core/Masterworks.cs`, `Core/MasterworksDefs.cs`, `Core/Projetos.cs`, `Core/ProjetosDefs.cs`, `Core/ContentValidator.cs`, `View/Loc.cs`, `View/Textos.cs`, `View/WorldView.cs`, `View/Game.cs`, `View/PainelMetas.cs`, `View/Captura.cs`, `tools/econsim`; testes em `Tests/EditMode/ArsenalTests.cs`; branch `feat/royal-arsenal`). Old Forge, Row e Quarter ao byte (§38.5).

- **Distrito 4** (`CONTEUDO_V1.md` §2.4, §3, §3.1, §4, §5.4, §5.7, §6, §12): 13 itens (6 produtos de tier 4, lâmina, placa, lingote e minério de ouro, couro), 10 receitas, 18 estações (kit: depósito, carroça de couro, fornalha, forja de lâminas, bancada de kits, pátio de cotas com 1 faixa, balcão de 4 vagas com estoque 10), 18 produtivos + 3 luxos + níveis kits/cerimonial II–V (IV–V com o `bp_mestra_arsenal`) + `selo_arsenal` (Meta, q +25 em toda estação de produto: `DistrictDef.Selo`, `Tuning.SeloQ`). Turma Cadetes (4 Porters, rotas 0/1/2/0, níveis 2–5 de 6.750 … 28.000) e o Intendente Matias (`wk_matias`, Quartermaster, 68.000 depois da 2ª faixa; "Matias" no cartão: o nome do catálogo passa de 16). Capitã Leonor em `Imperio.Managers`. Abertura 1.300.000. Charter = produção completa + Renome 7 (2.000) + 3 obras do distrito + **1 Royal Contract OU 10 encomendas Royal entregues no distrito** (`CharterDef.Contratos`, `Sim.RoyalEntregues`, linha opcional `cotar=`; contrato nunca bloqueia); `Charter.Proximo = null` (o último do build). **`Indice = 3`, `Distritos.All` = Old Forge, Row, Quarter, Arsenal; Row → Quarter → Arsenal** (`ArtisanQuarter.Charter.Proximo = royal_arsenal`). A chegada é a genérica da `main` (`Imperio.Chegada`: a cadeia inteira em produção completa, renda 2.400 × 3^índice de cada um; fundo de chegada dos 3 distritos ≈ 7.800/min, 25%).
- **Famílias de cliente** (§12, em dados no `DistrictDef.Clientes`, a mecânica genérica do Quarter, `Sim.Clientes.cs`): guarda (kit do batedor, kit da guarda; 1 un, ×1,0, ×1,0; a 1ª, sempre aberta), nobre (insígnia, alabarda de gala; 1 un, paciência ×1,8, intervalo ×2,5; rico), **emissário real (espada de gala, armadura de gala, insígnia; 2 un, ×2,0, ×2,0, qualidade mínima Fina)** e o colecionador (o de todo distrito: qualquer produto na Fina, Renome 2, rico), como o Quarter declara. Tibério é o freguês pessoal (`ord_reg_tiberio`, 6 insígnias Fina).
- **Pátio de cotas** (§2.4 regras 1–6; `Kind.Quota`, `Sim.Cotas.cs`): 1 faixa + `patio_faixa2`/`patio_faixa3`; o jogador escolhe o produto da faixa (o toque na linha da faixa, embaixo do pátio, cicla as linhas abertas; `Sim.EscolheCota`); bloco de 10 no 1º, +2 por bloco fechado do mesmo produto, teto 30. Carregador leva ao pátio quando há faixa daquele produto com vaga, senão ao balcão; o ferreiro entrega onde encostar. Bloco cheio fecha na hora: a Coroa paga **130% da soma dos `PriceOf(peça, nível)`, arredondado para cima** (nunca menos que 1,3×) e é **venda** (`GoldEarned`, `RateEma`, vendas, Renome das Superiores, projetos, Collection, encomenda, contrato, `Ev.Sold` por peça somando o pago) com +2 de Renome; até 180 s da 1ª peça do bloco (240 com o almoxarife contratado), +25% e +1 de Renome — prazo só de bônus. Trocar o produto com peça no bloco vale no próximo. Contadores `CotaBlocos`/`CotaNoPrazo` (QuotaBlocks/QuotaOnTime). O offline nunca fecha bloco nem anda o prazo. Save: linhas opcionais `cota=`, `cotab=`, `cotap=`, `cotar=` (só no distrito com pátio e fora do padrão; save sem elas = pátio vazio). `Ev.QuotaBlock` → aviso "Coroa pagou X!". Validator: no máximo 1 pátio; obra e projeto de cota só onde há pátio.
- **Mesa real** (major unlock): 2ª vaga de Royal Contract do Império (`Contratos.MesaReal` pelo Recompute do Arsenal, `Ativo2`, linha `ctr.v2=`; a peça conta para um contrato só; quando o 1º sai, o 2º sobe; o painel mostra o 2º na linha 1) e a encomenda Royal do Arsenal nunca vira Common (`ord_royal_royal_arsenal_mesa` pede Normal com a mesa, `OrderDef.Requer`; com Fina na linha vale a do catálogo).
- **Encomendas, projetos, obras, contratos:** as 7 do §7.2 + a variante da mesa + Tibério, ciclo com Royal ×3; 14 projetos (`bp_var_estandarte_real` pelo marco "50 blocos de cota", `BlueprintSource.Cotas`); 10 obras com as condições novas `CotaBlocos`, `CotaNoPrazo` e `Contratos` (ContractsDone; textos `hall.c.10`–`12`, depois dos 8–9 do Quarter); Superior alcançável = níveis 60 + técnica 20 + selo 25 = **105**; 7 marcos da Collection; `ctr_guarda_real`, `ctr_batedores` e `ctr_coroacao` entram no build (na lista, depois dos contratos do Quarter).
- **View:** linhas das faixas embaixo do pátio (toque troca), textos pt/en/es do distrito (os do ouro — item, estação e dicas — são os do Quarter, mesmo ID), `-cotas` (fotos: `-distrito royal_arsenal -testsession -cotas`); o pedido de contrato que não cabe (~60 colunas) cai para o nome curto com a qualidade 1× no fim ("Pede: 10 espadas + 10 alabardas + 5 armaduras · Fina+"), junto com a regra do Quarter (a mesma qualidade em todas as etapas sai 1× no fim).

**Decisões e desvios (revisar):**
1. **Preços × 2,4 (como a Row) e produtivos × 1,35** (a Row: × 2,4 e × 1,1 depois do V28): o Arsenal é preso no transporte (com o catálogo, produção completa do F2P aos 145 min e renda de ~12 mil/min). Antes das famílias o fator era × 2,8; com as famílias e o bot pela procura delas (F2P × 16, ferramenta de varredura), × 2,8 fechava a produção do F2P em 54:38 (portão 55–70) com renda de 51,4 mil/min; × 2,52 → 59:03 e 46,0 mil; **× 2,4 → 60:36 e 41,3 mil**; × 2,38 → 61:45 e 42,8 mil. Ficou × 2,4, o fator da Row. Luxo, níveis e selo como no catálogo.
2. **Transporte:** a regra é a da `main` (`Sim.Transporte.cs`: o papel 1 com 2+ insumos só busca o que alguma estação recebe) e o Arsenal soma só a prioridade pelo que as estações esperam (`DistrictDef.InsumoPorEspera`, +10 por estação parada sem o insumo; os outros distritos sem ela). Medido com as famílias (F2P × 16 × 150 min): sem ela, sem ela (a ferramenta de varredura, a mesma régua do desvio 1), a produção completa do F2P vai de 60:36 a 65:56, a renda nos 10 min depois de 41,3 a **31,3 mil/min (−28%, fora dos ±25%)** e o pátio de 67,6% a 60,8% das peças (o ouro volta a parar no crisol); fica..
3. **Regra do bot no pátio** (`Bot.Cotas.cs`): as faixas ficam com as linhas de **menor procura no balcão** (`Sim.Procura`: com famílias, cada uma divide o relógio dela pelos produtos que pede agora — Unidades / (intervalo × soma dos intervalos do rodízio); sem famílias, 1 / intervalo), uma linha sempre só para o balcão, re-escolhendo (no próximo bloco) quando a ordem muda. Com o critério antigo (maior intervalo do produto) e as famílias, o pátio ia a **74% das peças** (o kit da guarda no pátio deixava metade dos guardas sem o que pedir); pela procura das famílias, 68%. Antes das famílias: as 3 primeiras linhas fixas → 80%; só a sobra → 3%; maior intervalo → 47–50%.
4. **O prazo conta da 1ª peça do bloco** (não da escolha do produto): faixa parada não perde o prazo.
5. **A peça vendida à Coroa conta para a encomenda** (sem isso a vaga da linha que vai ao pátio nunca andava).
6. Encomendas com o mesmo desvio do Old Forge e da Row (70% dos segundos de Bulk/Quality/Mixed/Royal/freguês); com o catálogo, 12,8% da venda.
7. `bp_mw_grande_selo` sem a 2ª fonte (Contract), como o `bp_mw_peitoral_rival` da Row; o estandarte real aponta para a alabarda de gala (o catálogo não nomeia o item).
8. **Achado — o pátio no jogo longo:** aos 150 min o pátio leva 63–69% das peças nos 4 perfis (portão 30–70%); aos 420 min, ~75% no Ideal e no F2P. Não é a demanda (intervalo de cliente × 0,5 a × 1,0 não muda: 73–77%): as bancadas do balcão (guarda, gravador, cerimonial) passam ~87% do tempo com fome e as fornalhas e o crisol 47–67% travados (lingote pronto que ninguém leva) — o Arsenal é preso no transporte, e o pátio (sem teto de estoque) fica com o que os cadetes levam. Mexer pede decisão de catálogo (mais um cadete ou uma vaga a mais no balcão).

### 38.1 Antes (catálogo) → depois, `--distrito royal_arsenal --minutos 150 --partidas 16 --anuncio-seg 30 --renome 1350` (o Renome de quem fecha o Charter do Quarter, 1.250 + 100)

| | produção completa | maior seq. morta | renda/min 10 min após | renda/min últimos 10 | ouro de encomendas | peças pelo pátio (ouro) | blocos (no prazo) |
|---|---|---|---|---|---|---|---|
| catálogo, antes das famílias (F2P × 2) | 145:22 | 28 | — | 11.856 | 0,1% | 64% (68%) | 38,5 (10,5) |
| × 2,8 com as famílias, regra antiga (F2P) | 54:37 | 11 | 42.046 | 72.763 | 7,3% | 74,3% (83,7%) | 49,4 (6,2) |
| **vigente: F2P** | **61:19** | **13** | **38.145** | 63.393 | **6,2%** | **68,0%** (76,9%) | 49,3 (18,6) |
| vigente: Slow | 69:32 | 17 | 34.464 | 64.979 | 6,9% | 67,2% (75,7%) | 44,6 (12,5) |
| vigente: Ideal | 57:43 | 13 | 42.587 | 70.800 | 6,2% | 68,6% (78,3%) | 48,9 (17,6) |
| vigente: AdWatcher | 62:49 | 13 | 46.463 | 71.506 | 7,2% | 63,5% (68,0%) | 49,4 (24,4) |

Mix do F2P (% da receita): kit do batedor 8 · kit da guarda 10 · insígnia 7 · espada de gala 4 · alabarda 39 · armadura 31 (o pátio fica com espada, armadura e alabarda, as de menor procura; insígnia e kits no balcão).

### 38.2 Charter do Arsenal: Ideal e F2P × 8 × 420 min

Ideal: produção 57:24, 1ª e 2ª obras aos 159 e 179 min, 3ª aos 203:30, Renome 7 aos 295 min (8/8), **Charter pronto aos 323 min (7/8; 5 h 23, coluna 4–6 h)**; em 1 de 8 faltou o contrato (nem Royal Contract nem 10 Royal; 12,9 Royal em média). O F2P não golpeia: em 420 min falta Renome em 6/8 e obra em 2/8 (a prova é `ArsenalTests.Charter_PeloCaminhoReal_ContratoOuDezRoyal`, com 1 contrato e com 10 Royal). **Achado (de antes):** depois de ~2 h 30 de Arsenal tudo está comprado e o saldo passa de 18 milhões aos 420 min (~250 min sem destino); o ralo seguinte é a abertura da Arcane Foundry (3,9 mi) quando ela existir.

### 38.3 Portões (CONTEUDO_V1 §16, BALANCE §35.3), F2P × 16 × 150 min com a chegada real

Produção completa do F2P 61:19 (55–70) — ok; maior sequência morta 13 (≤ 15) — ok; renda nos 10 min depois da produção 38,1 mil contra 43,2 mil (−12%; ±25%) — ok; ouro de encomendas 6,2% (4–12%; 6,2–7,2% nos 4 perfis) — ok; Charter no Ideal 5 h 23 (4–6 h) — ok; pátio com 68,0% das peças (30–70%; 63,5–68,6% nos 4 perfis) — ok, perto do teto (achado 8).

### 38.4 Testes e prova vermelha

`ArsenalTests` (17): kit, números, índice 3, Quarter → Arsenal e as famílias do §12; bloco 10/+2/30; bloco pago a 1,3× para cima e é venda (GoldEarned, `Ev.Sold` somando o pago, encomenda, Renome); **fora do prazo nunca menos que 1,3×** (soma não redonda; Matias 240 s); **o offline nunca fecha bloco nem anda o prazo**; carregador ao pátio ou ao balcão; troca no próximo bloco; save de ida e volta (linhas opcionais, lixo); transporte com destino e prioridade; regra do bot pela procura das famílias (`Sim.Procura`); selo; mesa real (2ª vaga, a peça conta 1×, o 2º sobe, save do Império); Royal nunca Common e contando só no Charter que pede contrato; **Charter pelo caminho real** (3 obras pelo golpe; 9 Royal não bastam, a 10ª ou 1 contrato fecha; 1×); validator com defeitos (2 pátios, sem o selo, obra/projeto de cota sem pátio, encomenda com upgrade que não existe); layout; textos nos 3 idiomas. `ArtisanTests.Kit_DoQuarter_NumerosDoDistrito` passa a esperar o Quarter → Arsenal; `DistritosTests.Validator_MapaReal_SemViolacao` com os 4 distritos. 530 no `coretests`; `verify.sh quick` verde. Prova vermelha (`python client/tools/mutantes/mutantes.py --so ra-…`, versionadas): **38 mutações `ra-*`, todas mortas** (0 vivas, 0 inválidas; 5.443 s; depois do merge da #79 os 38 trechos batem 1× e a `ra-quarter-sem-arsenal`, a única no arquivo que ela mexeu, morre de novo), com as 5 novas das famílias e da procura (`ra-quarter-sem-arsenal`, `ra-procura-sem-familia`, `ra-procura-sem-rodizio`, `ra-emissario-sem-fina`, `ra-sem-familias`) e os trechos de `ra-bot-mais-procura` e `ra-preco-do-catalogo` refeitos..

### 38.5 Old Forge, Row e Quarter idênticos ao byte (`main` cb21fe7 × este branch; o mesmo com a 2e6f6a9)

O Arsenal dá os mesmos CSVs antes e depois do merge da arte do Quarter (#79): os números de 38.1 e 38.2 valem para a `main` cb21fe7.

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…8da3fdb3` (idêntico) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…608d3a96` (idêntico) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `2590bba6…5be815fd` (idêntico) |
| `--distrito artisan_quarter --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `71f2d9b7…d7208184` (idêntico) |

## 39. Rua dos Armeiros: transporte de insumos, Ferraz 4 e Rebitadeira 2 (raia ROW-PENDÊNCIAS, fim do C-CORE-05, 2026-10-15)

**Vigente** (`Core/Sim.Transporte.cs` novo, `Core/Sim.cs` — 3 ganchos de 1 linha —, `Core/Distritos.cs`, `View/Loc.cs`, `tools/econsim/Program.cs`; testes em `Tests/EditMode/RowPendenciasTests.cs`, `DistritosTests.cs`, `LigaTests.cs`; mutações `rowp-*` no `mutantes.py`; branch `feat/row-pendencias`). Só a Armorer's Row mudou; o Old Forge é o de sempre, ao byte (§39.5).

- **O defeito** (medido com a tabela nova do econsim e um diagnóstico por ajudante, F2P, 150 min): o Ferraz do papel 1 passava **30–70% do tempo parado com placas na mão** — toda montagem aberta com a entrada de placas cheia, esperando rebite — e até **40 min seguidos na boca de uma estação parada** (a fornalha de liga sem lingote: o alvo continuava "válido" por ser o alvo). Por isso as fornalhas ficavam ~6.000 s travadas, a liga chegava a ~4% das peças (0% no Ideal) e um carregador a mais rendia pouco. Não é demanda nem capacidade das estações (§33.1): era o carregador preso.
- **Transporte** (`Sim.Transporte.cs`, só em distrito com 2+ insumos feitos em estação; o Old Forge tem um, o lingote, e fica como estava): (1) o papel 1 só pega o que cabe nas entradas abertas que usam o insumo e não vai à fonte do que não cabe em lugar nenhum; (2) só espera na fonte alvo enquanto ela trabalha (parada e sem saída = escolhe outra).
- **Ferraz 4 no papel 1** (lingote, placa, rebite e liga), com o Ferraz 2. Save de antes (o 4º no papel 0) carrega: os papéis saem do `WorkerDef`, a mão do 4º sem dono no papel novo fica de fora (no máximo 2–4 brutos).
- **Rebitadeira: 4 rebites em 32 s** (o catálogo: 4 s). Com 4 s uma rebitadeira dá ~150 rebites/min com o Fole duplo e a Row pede ~12: a 2ª nunca fazia falta, com transporte consertado ou não. Com 32 s a 2ª (o último produtivo, a porta dos níveis II pela V29) faz ~1/3 dos ciclos depois da produção, sem ela a Row faz ~12% menos peças, e o lingote que sobra vai à fornalha de liga (liga em ~25% das peças).
- **Preço da Row × 2,4 do catálogo** (era × 3,5; elmo 335, maça 290, grevas 310, lança 170, manoplas 290, machado 265, peitoral 550) e **produtivos × 1,1** (era × 1,55; 530 … 46.000, Σ 198.230; a fornalha de liga 9.460). Luxo, níveis de estação, Vulcana, intervalos de cliente e balcão como estavam.
- **Textos**: "Nova linha: maça, paga N" das 5 linhas novas mostrava o preço do catálogo (1/3,5 do jogo desde o #66); agora o do jogo, nos 3 idiomas (teste). O Ferraz 4 "leva lingotes, placas e rebites".
- **econsim** (`--distrito`): tabela nova no fim — ciclos, s com fome e s travada por estação, % das peças com liga e os minutos em que o Renome do Charter e o Charter ficam prontos. O CSV não muda.

**Decisões e desvios (revisar):**
1. **Ferraz 4 no papel 1, não "couro e liga → montagens e forjas"** (§5.2/§6). Medido (F2P × 16, `--renome 500`, preços de antes): papel 0 (o de antes) 5.146/min depois da produção e liga em 3,7% das peças; "couro e liga" (papel novo, só medição) 5.066 e 2,7% — o bruto sobra (fornalhas travadas), não falta; papel 1 6.666 e 4,2%; papel 1 + transporte 7.979 e 6,9%.
2. **Regras de transporte só com 2+ insumos.** No Old Forge o mesmo defeito existe em tese (ajudante esperando na fornalha parada), mas lá esperar com lingote na mão não prende outro tipo e o jogo é o medido da Wave B: fica ao byte. Os distritos 3–6 que tiverem 2+ insumos ganham as regras de graça — **as raias do Artisan Quarter e do Royal Arsenal medem de novo depois do merge**.
3. **Rebitadeira 2 útil pelo tempo do rebite (32 s), não por receita nova nem estação nova.** Medido (F2P × 16, transporte consertado, preços × 3,5; ciclos da Rebitadeira 2 / renda depois da produção / sem a Rebitadeira 2):

| rebite | Rebitadeira 2 (% dos ciclos de rebite) | renda/min 10 min após a produção | sem a Rebitadeira 2 (peças em 150 min) | liga |
|---|---|---|---|---|
| 4 s (catálogo) | 52 (9%) | 7.979 | −3% | 6,9% |
| 1 lingote → 2 rebites, 4 s | 135 (15%) | 6.775 | — | 16,5% |
| 2ª fornalha de liga no lugar da Rebitadeira 2 (só medição) | 4 ciclos dela | 7.646 | — | 12,0% |
| 16 s | 94 (18%) | 7.595 | −6% | 16,4% |
| 24 s | 115 (22%) | 7.123 | −9% | 20,2% |
| **32 s** | **127 (25%)** | 7.304 | **−12%** | **25,8%** |

   A 2ª fornalha de liga não serve (a de liga passa fome de lingote, não de capacidade). Nenhum ID muda: `rebitadeira2` segue a 2ª rebitadeira, no mesmo lugar e com o mesmo efeito. Custo: a 1ª venda da Row sai ~10 s mais tarde (1:32 → 1:43 no F2P; o FTUE é do Old Forge) e, com o preço final, a Row faz ~11% menos peças em 300 min que com o rebite de 4 s (3.611 × 4.060 no Ideal), com a liga em 16% delas em vez de 2%.
4. **Preço × 2,4 e custo × 1,1** pela grade (F2P × 16, `--renome 500`; produção completa / renda nos 10 min depois): 2,2/1,0 58:09 / 4.255; 2,3/1,0 57:26 / 4.398; 2,4/1,0 55:49 / 4.687; **2,4/1,1 58:58 / 4.636**; 2,4/1,2 63:14 / 4.762; 2,5/1,1 61:05 / 5.110; 2,6/1,1 56:27 / 5.135; 2,6/1,2 59:59 / 5.279 (e com Renome 0, × 8: 2,3/1,0 61:33 / 4.186; 2,4/1,1 62:09 / 4.847; 2,5/1,1 64:16 / 4.540). Ficou 2,4/1,1: as duas janelas no meio, com Renome 0 e 500. É a promessa da V28: com o transporte consertado o preço volta para perto do catálogo (× 3,5 → × 2,4) e o custo quase ao catálogo (× 1,55 → × 1,1).
5. **Charter do Ideal em 2:31 h de Row, abaixo da janela de 3–5 h do §1 (decidido por padrão, coordenador 2026-10-16, revisar):** a janela é do F2P real; o Ideal é o teto (golpe perfeito) e só agora passou a fazer liga; o F2P do bot não golpeia e não fecha obras. Na `main` o Ideal fazia **0% de peças com liga** (o mesmo carregador preso) e o Charter caía às 3:36 h; com a liga chegando, o Renome 5 vem às 2:31 h (as encomendas e as Superiores sobem com as peças) junto com a 2ª obra (2:31). O F2P (que no bot não golpeia) chega ao Renome 5 às **3:24 h** (era 3:43) — a coluna "Charter (h no distrito)" do §1 é do F2P. Medidas as saídas (Ideal × 8, 300 min): Charter com 3 obras 2:43 h; Renome 6 (1.000) 4:31 h em 6/8 e 2/8 sem Charter em 5 h; rebite de 4 s (a liga volta a ~2% e a Rebitadeira 2 a não fazer falta) 2:55 h. O catálogo fica (2 obras, Renome 5); a medir de novo com o golpe do F2P real no POCO.
6. **Slow** com renda depois da produção de 3,3–3,9 mil/min (−19% a −31% do alvo; o portão é o F2P), maior sequência morta 12–14 (era 16–20).

### 39.1 Antes → depois, Renome 0 na chegada: `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30`

Antes = `main` fe195d6 (#72) com o econsim deste branch (as colunas novas). Peças = produtos feitos em 150 min.

| perfil | 1ª venda | produção completa | zona morta (min; maior seq.) | renda/min 10 min após a produção | renda/min últimos 10 min | saldo final | prêmio de qualidade | encomendas (% da venda) | peças (com liga) | Rebitadeira 2 (ciclos) | fornalha travada (s) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Ideal | 1:14 → 1:32 | 70:43 → **58:42** | 55,5 (19) → 42 (10) | 4.339 → 4.564 | 5.130 → 6.175 | 28.738 → 30.977 | 27,4% → 30,0% | 2,9% → 5,1% | 1.031 (0,0%) → 1.428 (25,0%) | 0 → 103 | 6.602 → 4.031 |
| F2P | 1:32 → 1:43 | 61:29 → **61:35** | 45,5 (11) → 46 (10) | 5.265 → 4.583 | 7.239 → 5.992 | 5.355 → 41.038 | 16,8% → 22,3% | 4,3% → 6,7% | 1.545 (2,8%) → 1.643 (24,2%) | 0 → 143 | 5.945 → 3.913 |
| Slow | 1:00 → 1:27 | 72:39 → **67:02** | 56 (16) → 49,5 (12) | 4.498 → 3.324 | 4.290 → 5.530 | 52.757 → 28.747 | 14,2% → 19,6% | 4,4% → 6,6% | 1.060 (0,9%) → 1.474 (14,4%) | 0 → 129 | 6.828 → 5.137 |
| AdWatcher | 1:56 → 2:00 | 60:33 → **58:50** | 45,5 (13) → 43,5 (10) | 4.147 → 4.533 | 7.579 → 6.688 | 33.956 → 27.981 | 15,3% → 22,3% | 4,7% → 6,2% | 1.284 (0,8%) → 1.614 (26,7%) | 0 → 134 | 6.440 → 3.575 |

F2P, Slow e Ideal × 8 (`--perfis F2P,Slow,Ideal --minutos 150 --partidas 8`): produção completa 63:04 → **62:39** / 77:34 → 68:47 / 71:16 → 57:41; maior sequência morta 13 → 12 / 20 → 14 / 19 → 11; renda nos 10 min depois da produção 4.631 → **4.571** / 3.731 → 3.873 / 4.157 → 4.731; peças com liga 2,6% → **25,3%** / 2,0% → 16,2% / 0,0% → 20,9%; Rebitadeira 2 1 → 136 ciclos / 0 → 119 / 0 → 120.

### 39.2 Chegada com o Renome do Império (`--renome 500`): F2P × 16 × 150 min

| | produção completa | zona morta (maior seq.) | renda/min 10 min após / últimos 10 | saldo | min sem destino | ouro de encomendas (% da venda) | prêmio de qualidade | peças (com liga) | Rebitadeira 2 / Rebitadeira (ciclos) | fornalha travada (s) |
|---|---|---|---|---|---|---|---|---|---|---|
| antes | 58:46 | 43 (15) | 5.146 / 7.441 | 50.883 | 3,8 | **11,3%** | 21,1% | 1.432 (3,7%) | 1 / 392 | 6.068 |
| depois | **58:18** | 42,8 (**13**) | **4.710** / 6.303 | 31.904 | 0 | **11,0%** | 24,4% | 1.639 (**24,2%**) | 136 / 404 | 3.600 |

### 39.3 O Charter da Row: Ideal e F2P × 8 × 300 min, `--renome 500`

| perfil | obras no Hall (1ª; 2ª) | Renome 5 (relógio) | Charter pronto (relógio) | prêmio de qualidade | ouro de encomendas | peças (com liga) | saldo aos 300 min | min sem destino |
|---|---|---|---|---|---|---|---|---|
| Ideal | 2,9 (195:05; 210:19) → 4,5 (129:11; 150:44) | 207 → 151 min | 216 → **151 min** (8/8) | 38,1% → 37,0% | 8,0% → 10,3% | 2.048 (0,0%) → 3.611 (15,9%) | 540.159 → 971.688 | 122,3 → 152,3 |
| F2P | 0 → 0 (não golpeia) | 223 → **204 min** | — (masterwork 8/8) | 28,7% → 31,5% | 10,8% → 11,5% | 3.062 (2,0%) → 3.651 (21,4%) | 1.036.417 → 824.220 | 152,6 → 143,3 |

O Charter continua provado pelo caminho real no `LigaTests.Charter_DaRow_DuasObrasPeloCaminhoDeSempre` (verde, sem mudança). Depois de ~2 h 30 de Row tudo está comprado e o saldo acumula (~0,8–1 milhão aos 300 min): o ralo seguinte é a abertura do Artisan Quarter (430 mil), como no §35.3.

**Portões (GDD §3, `CONTEUDO_V1.md` §16), na Row:** produção completa do F2P 58:18 (× 16, Renome 500), 62:39 (× 8, Renome 0) e 61:35 (× 2), janela 55–70 — ok; maior sequência morta do F2P 13 / 12 / 10 (≤ 15) — ok; renda nos 10 min depois da produção 4.710 / 4.571 / 4.583 contra 4.800 (−2% a −5%; aceite ±25%) — ok; ouro de encomendas 11,0% com Renome 500 (4–12%) — ok; Charter do Ideal 2:31 h de Row (3–5 h) — **abaixo, desvio 5 (decidido por padrão: a janela é do F2P real)**; F2P no Renome do Charter às 3:24 h. FTUE e os 10 primeiros minutos são do Old Forge: iguais (§39.5).

### 39.4 Testes e prova vermelha

`RowPendenciasTests` (5): `Transporte_PapelUm_SoPegaOQueCabe_OldForgeComoAntes` (montagens com placas cheias: o Ferraz 2 encostado na prensa com 4 placas não pega placa e vai ao rebite; com 1 vaga, pega 1; no Old Forge, bigorna cheia, a mão cheia de lingote como antes), `Transporte_PapelUm_NaoEsperaNaFonteParada_OldForgeComoAntes` (alvo na fornalha de liga parada: troca e leva o rebite; no Old Forge o alvo parado segura o ajudante, como antes), `Ferraz4_NosInsumos_SaveAntigoComPapel0Carrega` (papéis 0/1/2/1; save com o 4º no papel 0 e couro na mão carrega sem ID desconhecido), `Bot_F2P_Rebitadeira2FazFalta_ELigaChega` (bot chegando como o jogador: nos 30 min depois da produção a Rebitadeira 2 faz 40 ciclos e a 1ª 81; liga em 318 de 845 peças; o teste cobra ≥ 1/4 e ≥ 10%) e `Textos_NovaLinha_PagaOPrecoDoJogo` (as 5 linhas × 3 idiomas). Ajustados: `Row_Kit_BalcaoDe6EOsNumerosDoDistrito` (530 / 46.000; 335 / 550), `Row_Rebitadeira_QuatroRebitesPorLingote` (32 s), `FornalhaDeLiga_DoisLingotesUmaLigaEmSeisSegundos` (9.460). 494 testes no `coretests`; `verify.sh quick` verde.

**Prova vermelha** (`python client/tools/mutantes/mutantes.py --so rowp-…`, versionadas no `mutantes.py`): **12 mutações `rowp-*`, todas mortas** (0 vivas, 0 inválidas; 5 em 2026-10-15 e 7 em 2026-10-16 depois do merge da `main` aab8312, 1.438 s) — `rowp-ferraz4-papel0` (o Ferraz 4 de volta ao bruto), `rowp-pega-a-mao-cheia`, `rowp-fonte-sem-destino` (2 testes caem), `rowp-espera-fonte-parada` (2), `rowp-regras-no-oldforge` (as regras no Old Forge: 5 testes, goldens dele), `rowp-sem-gancho-boca`, `rowp-sem-gancho-escolha` (2), `rowp-sem-gancho-alvo` (os 3 ganchos do `Sim.cs`), `rowp-rebite-catalogo` (rebite em 4 s: 2), `rowp-preco-x3-5` (2), `rowp-custo-x1-55` (2) e `rowp-texto-preco-catalogo`.

### 39.5 Old Forge idêntico ao byte (`main` fe195d6, #72 × este branch)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico, `cmp`) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…08d3a96` (idêntico, `cmp`) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | muda de propósito: `f7c3f5c2…2a0eca` (`main`) → `2590bba6…be815fd` (este branch); a 1ª diferença é no minuto 1 (o rebite de 32 s) |

F2P × 8: produção completa 47:14, zona morta 19,6, saldo 619 — os do §35.5. A `main` foi extraída com `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário; cada `econsim.csv` foi movido para um nome próprio e comparado com `cmp`.

## 40. C-CORE-08: Arcane Foundry e o medidor de carga dos altares (2026-10-16)

**Vigente** (`Core/Distritos.ArcaneFoundry.cs` — `DistrictDef`, famílias de cliente, encomendas, projetos, obras e marcos do distrito —, `Core/Sim.Arcano.cs`, `Core/Bot.Arcano.cs`; ganchos em `Core/Sim.cs`, `Core/Bot.cs`, `Core/Defs.cs` (o bloco do Tuning e `QualitySource.Energizada`), `Core/Distritos.cs`, `Core/Distritos.RoyalArsenal.cs` (o Charter aponta para a Foundry), `Core/Imperio.cs`, `Core/Equipe.cs`, `Core/EncomendasDefs.cs`, `Core/Masterworks.cs`, `Core/MasterworksDefs.cs`, `Core/Projetos.cs`, `Core/ProjetosDefs.cs`, `Core/ContentValidator.cs`, `View/Loc.cs`, `View/Textos.cs`, `View/WorldView.cs`, `View/Game.cs`, `View/Captura.cs`, `tools/econsim`; testes em `Tests/EditMode/ArcaneTests.cs`; branch `feat/arcane-foundry`). Old Forge, Row, Quarter e Arsenal ao byte (§40.6).

- **Distrito 5** (`CONTEUDO_V1.md` §2.5, §3, §3.1, §4, §5.5, §5.7, §6, §12): 14 itens (7 produtos de tier 5; cristal, lingote rúnico, liga; cristal bruto e minério rúnico), 11 receitas, 18 estações (kit: depósito, cristal bruto, fornalha, cuba de cristal, altar de lanternas, balcão de 4 vagas com estoque 10), 18 produtivos + 3 luxos + níveis `altar_cristal`/`altar_runa` II–V (IV–V com o `bp_mestra_arcane`). Turma Aprendizes (4 Porters, rotas 0/1/2/1, níveis 2–5 de 20.000 … 83.000) e a Ísis (`wk_isis`, Smith de especialidade `Arcane`, 190.000 depois das lâminas rúnicas). Mestra Sibila em `Imperio.Managers`. Abertura 3.900.000. Charter = produção completa + Renome 8 (3.000) + 4 obras do distrito + **2 Royal Contracts OU 20 encomendas Royal no distrito**; `Charter.Proximo = null` (o último do build). **`Indice = 4`; Arsenal → Foundry** (`RoyalArsenal.Charter.Proximo = arcane_foundry`). Chegada genérica (`Imperio.Chegada`: fundo de chegada dos 4 distritos ≈ 24.000/min).
- **Famílias de cliente** (§12, dados no `DistrictDef.Clientes`, a mecânica do Quarter): viajante (lanterna, anel; 1 un, ×1,0, ×1,0), aventureiro (lâmina rúnica, escudo rúnico, anel; paciência ×0,9), nobre (amuleto, liga celeste, cajado; ×1,8, ×2,5, rico) e o colecionador (Fina, Renome 2). Zélia é a freguesa pessoal (`ord_reg_zelia`, 3 cajados Fina; "Zélia" no cartão).
- **Medidor de carga** (§2.5 regras 1–6; `Sim.Arcano.cs`): altar = estação cujo produto tem carga no `Tuning.CargaArcana` (o Ajuste da Foundry; null nos outros distritos: tudo no-op). Os altares e o cadinho são bancadas (`Kind.Crafter`: golpe, dica e bot de sempre) das famílias `altar_cristal`/`altar_runa`. E em centésimos (0–10.000), dreno contínuo no tempo das estações (base 3/s; `condensador` e `condensador2` +2 cada; Ísis +1 no nível 1 e +0,5 nos 3, 5, 7 e 9). **Estável** (padrão): a peça só começa com E + carga ≤ 100 (senão "estabilizando") e soma a carga — nunca sobrecarrega. **Forçado**: começa com insumo, soma 2 × carga e sai **energizada** (q +20, fonte nova `QualitySource.Energizada` no `QualityBreakdown`); se E passou de 100 ao somar, a peça termina normalmente e o altar pausa 10 s, depois E = 40; o insumo que esperava fica na entrada. Contadores `PecasEnergizadas` (EnergizedPieces por produto) e `Station.Sequencia` (StableStreak por altar). Modo do jogador: toque na barra de qualidade do altar abre o painel com FORÇAR/ESTABILIZAR (vale na próxima peça). Save: linhas opcionais `arcN=` (o altar fora do padrão) e `arce=` (os contadores); save sem elas = medidor vazio e Estável. O offline nunca mexe no medidor. Validator: carga só em produto, de 1 a 100 e do tamanho da tabela; dreno base positivo; sobrecarga devolve E abaixo do teto; a oficial arcana sem família e uma só; obras e projeto de energizadas/sequência só onde há altar; Superior alcançável = níveis 60 + técnica 20 + energizada 20 + Ísis 25 = **125**.
- **Encomendas, projetos, obras, contratos:** as 7 do §7.2 + a variante Superior do Quality (Renome 8) + Zélia; 14 projetos (`bp_var_aura` pelo marco "500 peças energizadas", `BlueprintSource.Energizadas`); 10 obras com as condições novas `PecasEnergizadas` e `SequenciaEstavel` (textos `hall.c.energizadas`/`hall.c.sequencia`: chave com nome, não número, para a raia da Grand Forge não colidir); 7 marcos da Collection; `ctr_torre_magos` e `ctr_vanguarda` entram no build.
- **View:** a linha do medidor embaixo de cada altar ("Estável · 45/100", "Forçado · 82/100", "Estabilizando · 95/100" em azul, "Sobrecarga! 7 s" em vermelho), o painel do toque, os textos pt/en/es do distrito (os da liga e da fornalha de liga são os da Row, mesmo ID) e `-arcano` (fotos: `-distrito arcane_foundry -testsession -arcano`, os 4 estados do medidor). Arte emprestada (`arte:`): cubas e fornos rúnicos = o crisol do Quarter, altares e cadinho = a mesa do gravador do Arsenal, depósitos = gemas e ouro do Quarter; sem `item_lingote_runico` e `item_cristal_bruto` (a cor de reserva).

**Decisões e desvios (revisar):**
1. **Preços × 2,6 e custos como no catálogo** (presa no transporte, como a Row e o Arsenal; lição 1): com o catálogo, produção completa do F2P aos 116 min e renda de ~48 mil/min (§40.1). Varredura (F2P × 8, antes dos desvios 2–3): × 3,0 e custos × 1,5 → 74:36 e 152 mil; × 2,8 e × 1,1 → 66:38 e 139 mil; × 2,4 → 64:12 e 117 mil; **× 2,6 → 62:05 e 128 mil**. Luxo, níveis, cargas, dreno, pausa e q +20 como no catálogo.
2. **Rodízio com Royal × 2** (a 2ª posição: Common → Royal) e a Royal pedindo **240 s** de renda (catálogo: × 1 e 200 s). Com o catálogo o Ideal entregava 13,6 Royal em 420 min (as 20 do Charter, ~10 h; os contratos não andam no bot: os da lista que vêm antes pedem peças de outros distritos). Royal × 2 com 200 s: Charter aos 289 min (4 h 49, abaixo da coluna 5–7 h); com 240 s, **302 min**. Quality continua × 3.
3. **Encomendas:** Bulk, Mixed, Royal e a Zélia a 70% dos segundos do §7.2 (o desvio do Old Forge, da Row e do Arsenal) e o **Quality a 13 s** (o do Old Forge e do Arsenal; catálogo 25 s; a Superior 21 s). Com o Quality a 18 s o ouro de encomendas ia a 12,6% da venda (portão 4–12%), por causa do Quality × 3.
4. **Balcão de 4 vagas e estoque 10** (o §2.5 não diz; o do Arsenal).
5. **`InsumoPorEspera`** (a prioridade do Arsenal: o papel 1 leva o insumo que mais estações esperam): sem ela, F2P × 16, produção 65:47 (contra 62:18), renda nos 10 min depois 115,8 mil (contra 126,6) e nos últimos 10, 150,1 mil (contra 164,5).
6. **Regra do bot** (`Bot.Arcano.cs`): "Forçado enquanto a próxima peça não sobrecarrega" (E + 2 × carga ≤ 100), decidida também com o altar trabalhando (até a próxima peça começar E só cai): nunca sobrecarrega. A 1ª versão só decidia com o altar parado e a peça emendada sobrecarregava (Ideal: 4,5 por 150 min).
7. **Ísis:** Smith de especialidade `Arcane` sem família (`Station` null): oficial de todo altar das duas famílias (`Equipe.Oficial`); como a Lúcia, o passo do oficial (+5%, +3%/nível) também vale. Dreno +0,5 nos níveis 3/5/7/9 pela conta inteira (nível − 1) / 2.
8. **StableStreak** zera só na sobrecarga; a peça Estável não conta nem zera. A sobrecarga é marcada quando a peça começa (E passou de 100 ao somar 2 × carga); durante a pausa E fica parado e vira 40 no fim. Dreno e pausa andam no tempo das estações (o boost acelera).
9. `bp_var_aura` conta as peças energizadas **feitas** (o contador do núcleo), não as vendidas; `bp_mw_amuleto_mare` sem a fonte Event (o `ev_arcane_week` ainda não é evento no build), como o Arsenal tirou a de contrato.
10. **Achado — o medidor quase não morde:** 5 dos 7 altares passam 78–86% do tempo com fome (F2P × 16, 9.000 s: lanternas 7.018 s, lâminas 7.629 s, cajados 7.775 s) enquanto cubas e fornos rúnicos ficam 33–75% travados (cuba 2: 6.764 s) — presa no transporte. Com fome entre as peças E volta a 0: o Estável quase nunca espera (com o dreno base em 1,5/s o Estável dá os mesmos números, ±0,2%) e o Forçado puro sobrecarrega 2,8 vezes por hora, quase sempre por cima da fome. O Forçado vira q +20 quase de graça: dentro do portão na renda depois da produção (§40.2), mas o Ideal aos 150 min rende 24% mais no Forçado (as obras de StableStreak e EnergizedPieces só saem nele, e mais Superiores). Medido com as cargas × 2 (o empate do kit cai): o portão passa nas 4 medidas (F2P e Ideal: 1,15 e 1,11 depois da produção; 1,10 e 1,11 nos últimos 10), mas o Forçado puro (380 sobrecargas/h) ainda rende mais que a regra que nunca sobrecarrega. O medidor só vira decisão com os altares alimentados (mais um carregador de insumo, ou entrada maior): decisão de catálogo.
11. **Achado:** tudo comprado em ~2 h 30 de Foundry (aos 420 min, 262–274 min sem destino e saldo de 48–55 milhões); o ralo seguinte é a abertura da Grand Forge (12 mi).

### 40.1 Catálogo → vigente, `--distrito arcane_foundry --minutos 150 --partidas 16 --anuncio-seg 30 --renome 2100` (o Renome de quem fecha o Charter do Arsenal, 2.000 + 100)

| | produção completa | maior seq. morta | renda/min 10 min após | renda/min últimos 10 | ouro de encomendas | prêmio de qualidade | peças energizadas (sobrecargas) |
|---|---|---|---|---|---|---|---|
| catálogo: F2P | 115:49 | 19 | 47.831 | 60.440 | 17,0% | 23,5% | 99,8% (0) |
| catálogo: Ideal | 80:46 | 17 | 41.911 | 59.238 | 17,8% | 28,1% | 99,8% (0) |
| **vigente: F2P** | **62:18** | **12** | **126.575** | 164.499 | **11,5%** | 30,4% | 99,7% (0) |
| vigente: Slow | 60:58 | 11 | 111.633 | 149.172 | 11,0% | 29,4% | 99,7% (0) |
| vigente: Ideal | 46:29 | 9 | 117.591 | 181.958 | 11,8% | 35,0% | 99,7% (0) |
| vigente: AdWatcher | 61:29 | 10 | 137.191 | 193.493 | 10,5% | 30,9% | 99,7% (0) |

Mix do F2P (% da receita): lanterna 22 · anel 31 · amuleto 8 · lâmina rúnica 12 · escudo rúnico 16 · cajado 6 · liga celeste 7. O "catálogo" é uma cópia do núcleo com os preços, as encomendas e o rodízio do §3/§7 (a regra do bot e o resto, os de agora).

### 40.2 Portão da mecânica: nenhum modo domina (`--arcano estavel|forcado|regra`, F2P e Ideal × 16 × 150 min, `--renome 2100`)

| política | produção completa (F2P / Ideal) | renda/min 10 min após (F2P / Ideal) | renda/min últimos 10 (F2P / Ideal) | sobrecargas/h | obras no Hall aos 150 (Ideal) |
|---|---|---|---|---|---|
| só Estável | 63:30 / 49:09 | 116.520 / 108.946 | 153.466 / 154.073 | 0 | 0,1 |
| só Forçado | 61:11 / 46:33 | 122.081 / 119.401 | 166.570 / 190.801 | 2,8 / 2,9 | 2,5 |
| regra (a do bot) | 62:18 / 46:29 | 126.575 / 117.591 | 164.499 / 181.958 | 0 | 2,8 |

**Portão (CONTEUDO_V1 §16): renda do Forçado entre 0,9× e 1,15× a do Estável, na renda dos portões (os 10 min depois da produção completa):** F2P **1,05×**, Ideal **1,10×** — passa. Nos últimos 10 min: F2P 1,09×, Ideal 1,24× (achado 10). **Testes negativos do §14:** `ArcaneTests.Estavel_SoComecaComEspaco_ENuncaSobrecarrega` (10 min com o altar mais rápido que o dreno: nenhuma sobrecarga, E ≤ 100) e `ArcaneTests.Forcado_SobrecarregadoNaoPerdePeca` (2 insumos, 2 peças, pausa de 10 s e E = 40); sem sorteio: `SemSorteio_EOfflineNaoMexeNoMedidor` (2 partidas do bot, o mesmo save).

### 40.3 O Charter da Foundry (4 obras): Ideal e F2P × 8 × 420 min, `--renome 2100`

Ideal: produção 45:07; 1ª e 2ª obras aos 91 e 101 min, 3ª aos 126; 9 obras no fim; Renome 8 aos 282 min (8/8); **Charter pronto aos 302 min (8/8; 5 h 02, coluna 5–7 h)**, pela 20ª Royal (26,1 no fim). O F2P não golpeia: falta Renome em 8/8 (2.850 aos 420 min), como no Arsenal; a prova é `ArcaneTests.Charter_PeloCaminhoReal_DoisContratosOuVinteRoyal` (4 obras pelo golpe; 1 contrato ou 19 Royal não bastam, o 2º contrato ou a 20ª Royal fecha; 1×). Com o catálogo (Royal × 1): Renome aos 324 min, 15 Royal em 420 min — falta o contrato em 8/8.

### 40.4 Portões (CONTEUDO_V1 §16, BALANCE §35.3), F2P × 16 × 150 min com a chegada real

Produção completa do F2P 62:18 (55–70) — ok; maior sequência morta 12 (≤ 15) — ok; renda nos 10 min depois 126,6 mil contra 129,6 mil (−2%; ±25%) — ok; ouro de encomendas 11,5% (4–12%; 10,5–11,8% nos 4 perfis) — ok, perto do teto; Charter no Ideal 5 h 02 (5–7 h) — ok, perto do piso; medidor 1,05× / 1,10× (0,9–1,15×) — ok.

### 40.5 Testes e prova vermelha

`ArcaneTests` (17): kit, números, carga por peça, encadeamento Arsenal → Foundry e as famílias do §12; os outros distritos sem altar; dreno (base, condensadores, Ísis por nível); empate no kit (uma lanterna a cada 5 s, sem esperar o medidor); **Estável nunca sobrecarrega** e só começa com espaço; **Forçado sobrecarregado não perde peça** (pausa, E = 40); energizada q +20, EnergizedPieces, StableStreak (a Estável não conta nem zera; a sobrecarga zera), troca de modo na próxima peça e o marco das 500; **sem sorteio** e o offline sem mexer no medidor; save de ida e volta (linhas opcionais, lixo, peça em curso); Ísis em todo altar; regra do bot (e 20 min sem sobrecarga); encomendas (rodízio, Royal 240 s, Zélia, Royal no Charter); contratos no build; **Charter pelo caminho real**; validator com defeitos; layout; textos nos 3 idiomas. `ArsenalTests.Kit_…` espera Arsenal → Foundry; `DistritosTests.Validator_MapaReal_SemViolacao` confere o mapa pela cadeia dos Charters (sem fixar posição); `EquipeTests.Validator_…` troca o distrito inexistente para `fundicao_fantasma`. 547 no `coretests`. **Prova vermelha** (`python client/tools/mutantes/mutantes.py --so af-…`, versionadas no `mutantes.py`): **45 mutações `af-*`, todas mortas** (0 vivas, 0 inválidas; a rodada parada em 20/45 na pausa de 2026-10-16 foi refeita inteira em 2026-10-21 depois do merge da `main` 25185ea, 6.804 s) — medidor e modos (`af-estavel-sem-espaco`, `af-estavel-carga-dobrada`, `af-forcado-carga-simples`, `af-energiza-sempre`, `af-carga-trocada`, `af-troca-na-peca-em-curso`), sobrecarga e pausa (`af-sem-sobrecarga`, `af-sobrecarga-perde-peca`, `af-sobrecarga-perde-insumo`, `af-sobrecarga-sem-pausa`, `af-pausa-nao-segura`, `af-pausa-sem-reset`), contadores e qualidade (`af-sequencia-nao-zera`, `af-estavel-zera-sequencia`, `af-energizada-sem-q`, `af-breakdown-sem-energizada`, `af-energizada-nao-conta`, `af-qmax-sem-arcano`), dreno e offline (`af-dreno-parado`, `af-dreno-sem-condensador`, `af-dreno-sem-isis`, `af-offline-drena`), save (`af-save-sem-medidor`, `af-save-sem-arce`, `af-load-ignora`, `af-load-energizada-parado`), Ísis (`af-isis-sem-altar`), validator (`af-validator-carga-limite`, `-carga-insumo`, `-dreno`, `-isis`, `-energizadas`, `-sequencia`), obras e marco (`af-mw-energizadas-parada`, `af-mw-sequencia-parada`, `af-marco-energizadas`), bot (`af-bot-sem-altar`, `af-bot-sempre-forcado`, `af-bot-so-parado`) e dados do distrito (`af-arsenal-sem-foundry`: 7 testes caem; `af-charter-um-contrato`, `af-sem-familias`, `af-ciclo-do-catalogo`, `af-sem-insumo-por-espera`, `af-sem-manager`).

### 40.6 Old Forge, Row, Quarter e Arsenal idênticos ao byte (`main` 25185ea × este branch; antes, 33e1bd6 e e6ee033)

Remedido em 2026-10-21 depois do `git merge origin/main` (#92–#96: screenshots da loja, docs e `mutantes.py`; o núcleo e o econsim da `main` não mudaram desde a 33e1bd6): os 5 CSVs dão o mesmo SHA dos dois lados e os da Foundry não mudam, então os números de 40.1–40.3 valem para a `main` 25185ea. A `main` foi extraída com `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` numa pasta curta (`%TEMP%\wt`: o caminho do scratchpad estoura o MAX_PATH no `dotnet run`).

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico, `cmp`) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…08d3a96` (idêntico) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `2590bba6…5be815fd` (idêntico) |
| `--distrito artisan_quarter --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `71f2d9b7…d7208184` (idêntico) |
| `--distrito royal_arsenal --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `05bbb554…0b306933` (idêntico) |
## 41. C-CORE-09: Grand Forge — rotas de importação e lotes de exportação do Mensageiro (2026-10-16)

**Vigente** (`Core/Distritos.GrandForge.cs` — `DistrictDef`, famílias de cliente, encomendas, projetos, obras e marcos do distrito —, `Core/Sim.Exportacao.cs`; ganchos em `Core/Sim.cs`, `Core/Sim.Cotas.cs` (o pátio de exportação no `CotaAceita`/`Destino`), `Core/Defs.cs`, `Core/Distritos.cs`, `Core/Distritos.RoyalArsenal.cs` (o Charter aponta para a Grand Forge), `Core/Imperio.cs`, `Core/EncomendasDefs.cs`, `Core/Masterworks.cs`, `Core/MasterworksDefs.cs`, `Core/Projetos.cs`, `Core/ProjetosDefs.cs`, `Core/RenomeDefs.cs`, `Core/ContentValidator.cs`, `Core/Equipe.cs` (doc), `View/Loc.cs`, `View/Textos.cs`, `View/WorldView.cs`, `View/Game.cs`, `View/Captura.cs`, `tools/econsim`; testes em `Tests/EditMode/GrandForgeTests.cs`; branch `feat/grand-forge`). Old Forge, Row, Quarter e Arsenal ao byte (§41.5). Sem `Bot.Exportacao.cs`: a regra do bot é o padrão do jogo (lote no automático, desvio 6).

- **Distrito 6** (`CONTEUDO_V1.md` §2.6, §3, §3.1, §4, §5.6, §5.7, §6, §12): 17 itens (handles 0/1 = minério de cobre e barra de bronze, como o Quarter; 2–7 = sino, estátua, lâmina estelar, relicário, coroa, panóplia; ferro, lingote, pedra e aço estelar; no fim os 5 importados, insumo aqui e produto na origem, com o mesmo ID), 9 receitas, 18 estações (kit: depósito de cobre, crisol de bronze, fundição de sinos, balcão de 4 vagas com estoque 10; o **cais** `Kind.Import` e o **pátio de exportação** `Kind.Export` abrem com o `mensageiro`), 18 produtivos + 3 luxos + síntese e fundição II–V (IV–V com o `bp_mestra_grand`) + `bencao_forja` (Meta, q +25: `DistrictDef.Selo`). Turma Estrela (4 Porters, rotas 0/1/2/**0**, níveis 2–5 de 61.000 … 250.000) e o **Félix** (`wk_felix`, Courier: contratado pelo upgrade `mensageiro`, não carrega; níveis 2–10 = 150.000 × 1,6^(L−2) … 6.400.000). Grão-mestre Aurélio em `Imperio.Managers` ("Aurélio": o nome passa de 16). Abertura 12.000.000. **Carta da Grande Forja** = produção completa + Renome 9 (**4.200**, desvio 10) + 5 obras + 3 Royal Contracts **OU 30 encomendas Royal** no distrito; `Charter.Proximo = null` (o último). **`Indice = 4` provisório, `RoyalArsenal.Charter.Proximo = grand_forge`** (com a Arcane Foundry na `main`: índice 5 e Foundry → Grand Forge). Famílias do §12 em dados: mercador (sino, estátua; 2 un, ×0,8, ×1,3; a 1ª), nobre (relicário, estátua, coroa; rico, ×1,8, ×2,5), emissário (coroa, panóplia, lâmina estelar; 2 un, ×2,0, ×2,0, Fina) e o colecionador. Embaixador Kael (`ord_reg_kael`, 6 sinos; "Kael" no cartão).
- **Rotas** (§2.6 regras 1–2; `Sim.Exportacao.cs`): capacidade `Tuning.ImportCap` 6 un/min com o mensageiro, +6 com o `cais2`, +2 por nível do Félix acima do 1, teto 30; uma unidade a cada 60/capacidade s (tempo de jogo), sempre para a rota (`DistrictDef.Importados`, na ordem do §3.1) cuja bancada de destino tem **menos daquele importado na entrada**; empate em rodízio (desvio 3); sem entrada com espaço o Mensageiro espera e a unidade sai assim que abre. Cada unidade chama `Imperio.Importou`: `PriceOf(item, Normal)` da origem × 25% vira dívida da origem, paga do fundo que ela credita no minuto (`Imperio.Fundo`, janela de 60 s); o que passa do minuto é perdoado — **nunca abaixo de 0** (a carteira nunca perde ouro pelo fundo). Só com o jogo aberto: o offline e o cofre nunca importam. `ImportedUnits` conta na entrega.
- **Lote** (regra 3): o jogador escolhe o produto (`EscolheLote`; −1 = **"o mais estocado"** no balcão, o padrão; o toque na linha do lote cicla automático → cada linha aberta → automático); o carregador de produto e o ferreiro levam ao pátio quando o lote aceita (o produto e vaga), senão ao balcão (`CotaAceita`/`Destino` do Arsenal ganharam o pátio de exportação). A cada 90 s (60 com o `patio_exportacao2`) o Mensageiro parte com o que tiver, até 10 peças (20; +2 por nível do Félix acima do 1), pagando **125% da soma dos `PriceOf(peça, nível)`, para cima** — é **venda** (`GoldEarned`, `RateEma`, vendas, Renome das Superiores, projetos, Collection, encomenda, contrato, `Ev.Sold` por peça somando o pago) e `Ev.ExportLot` ("Mensageiro levou X!"). **Vazio não parte nem conta.** `ExportLots` conta. Save: linhas opcionais `gf=` (produto, relógios, contadores) e `gfl=`/`gflq=` (o lote); save sem elas = padrão; distrito sem cais nem pátio: tudo no-op.
- **Encomendas, projetos, obras, contratos:** as 7 do §7.2 + a Royal Superior (Renome 9) + Kael, ciclo com Royal ×3 e Mixed ×2; 14 projetos (`bp_var_sino_ouro` pelo marco "100 lotes", `BlueprintSource.Lotes`); 10 obras com as condições novas `Importados` (ImportedUnits), `Lotes` (ExportLots) e `Charters` (DistrictCharters: os Charters do Império, `Sim.Imp`); Superior = níveis 60 + técnica 20 + bênção 25 = **105**; 7 marcos da Collection; `ctr_sinos_catedral` e `ctr_campeao` entram no build (abrem com a Grand Forge e o Renome).
- **Validator:** no máximo 1 cais e 1 pátio de exportação; rota sem cais / cais sem rota; importado fora da tabela, vendido aqui, que nenhuma receita usa ou de distrito que não o vende; o Mensageiro sem upgrade ou sem cais; obra de importados/lotes e o marco dos lotes só onde há cais/pátio; `Charters` até os outros distritos do build; o importado conta como produzido no fecho das receitas.
- **View:** uma linha embaixo do cais ("Rotas: 6/min · 123 chegaram") e uma embaixo do pátio de exportação ("Lote: 7/10 auto: sinos da catedral · 0:42"; o toque troca o produto), aviso da partida, o cartão do Félix ("Rotas +2/min · lote +2"), o Félix ao lado do pátio, `-exportacao` (fotos: `-distrito grand_forge -testsession -exportacao`). Arte emprestada (crisol e depósito de cobre do Quarter, forjas e bancadas do Arsenal, a carroça no cais, o pátio de cotas no de exportação).

**Decisões e desvios (revisar):**
1. **Importados: só os 5 que já existem** (peitoral da Row; colar e caixa do Quarter; insígnia e espada de gala do Arsenal). Os 4 da Arcane Foundry (lanterna, anel, lâmina e escudo rúnicos) esperam o distrito 5: até lá a lâmina estelar é só aço ×2 (sem rota) e relicário, coroa e panóplia não pedem o insumo arcano. Com a Foundry: 4 linhas no `Importados` e 4 insumos nas receitas (e remedir).
2. **O Mensageiro entrega na entrada da bancada** (o cais é a chegada, sem pilha nem carregador) e a **Estrela 4 leva brutos** (papel 0), não "importados → bancadas": com um carregador de bruto só, o cobre ganhava sempre e o ferro e a pedra estelar nunca saíam (fornalhas de ferro e forja estelar com fome 85–99% do tempo, lâmina/panóplia ~0).
3. **Empate das rotas em rodízio** (a partir de `ImportedUnits % rotas`), não "sempre a 1ª da tabela": com a ordem fixa o colar ganhava todo empate.
4. **"+2 por nível do Félix" = por nível acima do 1** (contratado = 6 un/min e lote 10, como o §5.6 diz).
5. **Desconto por janela de 60 s** do `Imperio.Fundo` (a dívida da origem é paga do crédito do minuto; o resto é perdoado na virada).
6. **Regra do bot no lote = o automático** ("o mais estocado" no balcão, empate pela ordem dos itens): o bot não escolhe nada (por isso não há `Bot.Exportacao.cs`). Alternativas (a linha de menor procura, como o pátio de cotas; o produto mais caro) não foram medidas.
7. **Trilho fornalha → forja estelar com a cratera** (o efeito Conveyor, como o trilho de placas da Row): sem ele o lingote parava na fornalha (fome da forja estelar 95%; lâmina, estátua e panóplia quase não saíam). Achado: a **Fornalha 2** (`fornalha_grande2`) fica parada (a forja só precisa de uma; saída cheia > 98% do tempo).
8. **Preços × 0,85 e produtivos × 0,9** (13.136.000; o catálogo: 14.501.000) — o contrário dos outros distritos: a coroa sai só de importados (o Mensageiro não depende do transporte) e o lote paga ×1,25; com o catálogo a renda depois da produção ia a +23% da alvo. Luxo, níveis e bênção como no catálogo.
9. **Encomendas: alvo × 1/3 e prêmio × 0,35** (`OrderSeconds` 50, `OrderRewardSeconds` 3,5), além dos 70% de Bulk/Quality/Mixed/Royal/Kael do Old Forge, Row e Arsenal: com 6 linhas e 3 lentas (relicário, lâmina, panóplia) a encomenda de 20 peças prendia a vaga, saíam ~6 por hora e as 30 Royal da Carta passavam das 9 h; com N menor sai o triplo no mesmo ouro (4,6–5,8% da venda).
10. **Renome 9: 4.500 → 4.200** (`RenomeDefs`; o §11 manda calibrar os limiares 5–10): com 4.500 a Carta do Ideal saía em 2 de 8 rodadas de 480 min (Renome de ~4.300 no fim); 4.100 piora (a Royal Superior abre antes e as Royal ficam mais lentas).
11. **Coroa dos Seis: DistrictCharters ≥ 4** provisório (= o índice da Grand Forge, os distritos antes dela; 5 com a Foundry; `GrandForgeTests.Kit` confere contra o índice).
12. Sem o "+30 do aço estelar" do §10.2 (não é regra do §2.6; a q máxima já é 105). `bp_mw_coroa_granforja` sem a 2ª fonte (Contract), como a Row e o Arsenal. A Royal Superior paga 1,6× a Fina (67 s; 58.320 de mínimo), como as variantes Superiores.
13. **Achado — o relicário quase não sai no bot** (0,2–1% da receita): o bronze vai às fundições (entradas de 16 e 12) antes do relicário (entrada de 4) — o placar de destino é por peças de espaço. Medido o placar "espaço em receitas" (espaço ÷ o que a receita pede): não mudou a mesa (o relicário seguiu < 1%) e saiu. O humano leva; no bot a coroa (só importados) fica com ~44% da receita.
14. **Achado — sem destino:** depois de ~2 h 30 tudo está comprado e o saldo passa de 200 mi aos 480 min (o último distrito: o endgame de exportação e obras lendárias).
15. **Achado — contratos no econsim:** a lista oferece o 1º não concluído (o `ctr_ronda_vila`, do Old Forge), que nunca anda na Grand Forge; e o `ctr_torre_magos` (Foundry) dorme e segura os da Grand Forge até a Foundry entrar. A Carta sai pelas 30 Royal (contrato nunca bloqueia).

### 41.1 Catálogo → afinado (`--distrito grand_forge --perfis F2P --minutos 150 --partidas 16 --anuncio-seg 30 --renome 3100 --fundo-extra 48600`: a chegada da cadeia inteira — o Renome do Charter da Foundry, 3.000 + 100, e o fundo dela, 48.600/min, que a `main` ainda não tem)

| | produção completa | maior seq. morta | renda/min 10 min após | renda/min últimos 10 | ouro de encomendas | peças pelo lote (ouro) | lotes | importados | Royal |
|---|---|---|---|---|---|---|---|---|---|
| catálogo, transporte de antes (Estrela 4 no papel 1, sem trilho; F2P × 4) | 65:08 | 8 | 345.794 | 459.325 | 2,9% | 66,4% (68,4%) | 124,8 | 902 | — |
| catálogo, estrutura vigente | 60:43 | 11 | 477.190 (+23%) | 616.783 | 8,5% | 48,3% (49,5%) | 106,6 | 1.766 | 7,9 |
| **vigente: F2P** | **58:08** | **11** | **402.518 (+3,5%)** | 517.752 | **5,8%** | **48,4% (49,4%)** | 108,4 | 1.828 | 14,6 |
| vigente: Slow | 61:27 | 13 | 333.906 | 447.606 | 4,6% | 50,8% (52,1%) | 105,6 | 1.710 | 9,4 |
| vigente: Ideal | 48:06 | 11 | 294.730 | 474.851 | 5,1% | 45,6% (43,2%) | 98,6 | 1.664 | 11,4 |
| vigente: AdWatcher | 56:47 | 10 | 403.214 | 583.738 | 4,7% | 49,7% (46,7%) | 101,4 | 1.856 | 11,4 |

Mix do F2P (% da receita): sino 22,7 · estátua 21,7 · lâmina estelar 5,5 · relicário 0,8 · coroa 43,5 · panóplia 5,8. Importados do F2P: peitoral 99 · colar 802 · caixa 26 · insígnia 802 · espada de gala 99. **Sem a Foundry** (a chegada de hoje: `--renome 2100`, sem `--fundo-extra`; fundo ~24.000/min): F2P × 16 em 72:17 com maior sequência morta 16 e renda 369.618 — fora da janela 55–70 e do teto de 15 até a Foundry entrar (remedir no merge dela).

### 41.2 A Carta da Grande Forja: Ideal × 8 × 480 min, a chegada da cadeia inteira

Ideal: produção 48:19, 3ª obra aos 183 min e 5ª aos 335 min, 35,8 Royal entregues (30 pedidas), **Renome 9 (4.200) aos 389 min e Carta pronta aos 390 min em 6 de 8 (6 h 30; coluna 6–8 h)**; nas outras 2 faltou Renome aos 480 min. Com o Renome 9 do catálogo (4.500): 2 de 8 (453 min). Com o catálogo inteiro: 0 de 8 (Renome 3.971, 23 Royal e 4,3 obras aos 480 min). O F2P não golpeia (sem obras): a prova é `GrandForgeTests.Carta_PeloCaminhoReal_TresContratosOuTrintaRoyal`.

### 41.3 Portões (CONTEUDO_V1 §16, BALANCE §35.3), F2P × 16 × 150 min com a chegada da cadeia inteira

Produção completa do F2P 58:08 (55–70) — ok; maior sequência morta 11 (≤ 15) — ok; renda nos 10 min depois da produção 402,5 mil contra 388,8 mil (+3,5%; ±25%) — ok; ouro de encomendas 5,8% (4–12%; 4,6–5,8% nos 4 perfis) — ok; Carta no Ideal 6 h 30 (6–8 h, 6 de 8) — ok; **importação: o fundo da origem nunca chega a 0 com as rotas no máximo** — a maior fração do fundo de uma origem pedida num minuto foi 45,8% (Row) / 55,6% (Quarter) / 33,5% (Arsenal) com `--tuning ImportCap=30` (rotas no teto desde o mensageiro, F2P e Ideal × 8 × 150 min) e 45,8% / 44,3% / 24,7% no Ideal × 8 × 480 min (o Félix no 10: 30 un/min) — ok (< 100%); **lote vazio não conta ciclo** — `GrandForgeTests.Lote_VazioNaoParteNemConta` — ok.

### 41.4 Testes e prova vermelha

`GrandForgeTests` (16): kit, números, o último do mapa (sem índice fixo: o anterior libera a Grand Forge), os importados do §3.1 e o Félix; o mensageiro contrata o Félix que não carrega e o trilho da cratera; rotas 6 / +6 / +2 por nível / teto 30; uma a cada 60/capacidade s para quem tem menos, empate em rodízio, a espera sem destino; **o desconto no fundo da origem, nunca abaixo de 0 e perdoado no minuto seguinte** (§14); **o offline nunca importa nem exporta** (§14); o lote paga 1,25× para cima e é venda (`Ev.Sold` somando o pago, encomenda); **lote vazio não parte nem conta** (§14); tamanho, período e produto do lote (o automático = o mais estocado); carregador ao lote ou ao balcão; save de ida e volta (linhas opcionais, lixo); ciclo com a Royal primeiro, Kael e os contratos no build; **a Carta pelo caminho real** (5 obras pelo golpe; 29 Royal não bastam, a 30ª fecha; ou 3 contratos; 1×); validator com defeitos; layout; textos nos 3 idiomas. Ajustados: `ArsenalTests.Kit` (o Arsenal libera o próximo do mapa), `DistritosTests.Validator_MapaReal_SemViolacao` (os 5 distritos), `EquipeTests.Validator_DefeitosInjetados_Acusa` (o papel sem regra agora é o Manager; o Courier ganhou regra) e `RenomeTests.Niveis_PorLimiar` (4.200). 546 no `coretests` (todos verdes depois da `main` 33e1bd6). Prova vermelha (`python client/tools/mutantes/mutantes.py --so gf-…`, versionadas): **42 mutações `gf-*`** — 43 de 43 mortas com a `ra-carregador-balcao` (§41.6) — e o trecho da `ra-carregador-balcao` refeito (o `Destino` ganhou o pátio de exportação).

### 41.5 Old Forge, Row, Quarter e Arsenal idênticos ao byte (`main` 36ec5c1, e6ee033 e 33e1bd6 × este branch; o `Core` e o `econsim` da `main` não mudaram de 36ec5c1 a 33e1bd6)

| econsim | SHA-256 do CSV (os dois lados) |
|---|---|
| `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30` | `9f3f548e…28da3fdb3` (idêntico) |
| `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` | `3acf0f6f…608d3a96` (idêntico) |
| `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `2590bba6…5be815fd` (idêntico) |
| `--distrito artisan_quarter --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `71f2d9b7…d7208184` (idêntico) |
| `--distrito royal_arsenal --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0` | `05bbb554…0b306933` (idêntico) |

### 41.6 Prova vermelha (`python client/tools/mutantes/mutantes.py --so <id>`)

**43 de 43 mortas**: as 42 `gf-*` (rotas: cais 2, Félix, teto, intervalo, rodízio, menos estoque, espera; desconto: sem, sem os 25%, abaixo de 0, sem perdão; offline; o Félix que carrega; lote: venda, 1,25×, contador, vazio, pátio 2, nível, encomenda, só o produto, o automático; carregador e ferreiro ao lote; save e load; validator: rotas, importado vendido aqui, origem, Courier, obras de importados e de Charters, marco dos lotes; obras zeradas; a Carta com 1 contrato; o Arsenal sem a Grand Forge; o trilho; preço, custo e Renome 9 do catálogo; o cartão do Félix) e a `ra-carregador-balcao` com o trecho novo. A `gf-carregador-ignora-lote` e a `gf-save-sem-gf` sobreviveram à 1ª rodada e morreram depois de 2 asserções no `GrandForgeTests` (o ferreiro com o balcão cheio entrega ao lote; o save grava só o produto do lote). 24 rodaram antes da `main` 33e1bd6 e 19 depois (o `Core` e os `Tests` da `main` são os mesmos). Fora da raia: na `main` 33e1bd6 os trechos da `fila-trava-na-frente`, `wk-load-depois-da-pilha` e `bp-tecnica-zero` não acham mais o código (não rodadas).
