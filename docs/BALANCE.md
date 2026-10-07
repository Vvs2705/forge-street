# BALANCE — Forge Street v0.1 + 2ª área (medido pelo bot, 2026-10-06; 2ª área e correção da fila em 2026-10-07, §9; fase 2 da joalheria e baús de marco, §10)

Tudo aqui saiu de `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` (`BalanceTests`). Nenhuma pessoa jogou ainda: os tempos humanos são **HIPÓTESE** derivada do bot. Mudou um número em `Assets/_FS/Scripts/Core/Defs.cs` (`Balance`), roda o teste e atualiza este arquivo.

**Versão vigente: §10.7 (Passo A, 20 upgrades, custos 2.200/5.500/9.000 e joia 80 com Vitrine).** §1–9 preservam as medições da oficina e da fase 1; tetos offline "tudo comprado" e contagens dessas seções são históricos. Teto atual em §10.5; guardas atuais em §10.6. **Luxo (fase 3, 3 compras sem bônus): vigente 11.000/14.000/18.000, medição e portão em §11** (6.000/10.000/16.000 = histórico, §11.4). **Luxo marcado por upgrade (`UpgradeDef.Luxury`), sem mudança de número: §12.**

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

## 9. 2ª área: corredor lateral + joalheria (contrato `docs/AREA2_JOALHERIA.md`, medido 2026-10-07)

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

## 10. 2ª área, fase 2: Joalheiro, Lupa, Vitrine de joias e baús de marco (contrato `docs/AREA2_FASE2.md`, medido 2026-10-07)

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

Fonte de decisão: handoff `PROMPT_CONTINUACAO_CHATGPT.md` §3 e §6A. Aplicado: Joalheiro **2.200**, Lupa **5.500**, Vitrine de joias **9.000**; descrição "Nobres pagam 80 e a fila cresce"; `Balance.JewelPriceUp = 80`. `Sim.PriceOf(Item)` é usado por `Sell` nos dois caminhos (fila e compra direta), inclusive `GoldEarned` e `Ev.Sold.B`. Antes da Vitrine a joia continua em 60; outras linhas mantêm 10/25/16. Bot e dicas não têm preço de joia fixo e não precisaram mudar.

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

Limites calibrados em §10.6. Estado do Passo A: **núcleo testado por dotnet, 37/37**. Portão local da continuação concluído: Unity EditMode 37/37, Windows/Android sem erros, autoplay e fotos; relatório em `docs/VALIDACAO_FASE2.md`. Aparelho Android e playtest humano continuam pendentes. Bot determinístico continua uma hipótese sobre pessoas: nenhuma medição daqui comprova retenção, CPI ou legibilidade mobile.

## 11. Fase 3: luxo depois dos 20 produtivos (contrato `docs/FASE3_LUXO.md`, medido 2026-10-07)

**Vigente (aprovado pelo coordenador em 2026-10-07):** Fachada nobre **11.000**, Piso de oficina **14.000**, Joalheria real **18.000** (`Balance.WorkshopFacadeCost/WorkshopFloorCost/JewelryDecorCost`), total de 43.000. Os preços do contrato original (6.000 / 10.000 / 16.000) são **históricos** (§11.4): a Fachada saía cedo demais, e um retorno no teto offline pagava dois luxos de uma vez. Esta seção é a evidência 1 do contrato. Diagnóstico e A/B de logística ficam em `docs/ESTUDO_LOGISTICA.md`.

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

**2º ajudante de minério:** o A/B isolado não passou nos 4 critérios. Nada foi implementado, e não há §12 de preço nem `FASE4_MINERIO.md`. Resultado e recomendação em [ESTUDO_LOGISTICA.md §5](ESTUDO_LOGISTICA.md).
