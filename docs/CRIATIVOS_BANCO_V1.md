# Banco de criativos UA v1 — Forge Street (G-UA-01)

2026-10-09 · Leva 4 do ORQUESTRADOR, raia UA. Ticket G-UA-01 (`PRODUCAO_V1_MAPA_TICKETS.md`, Wave G; "DM §n" = `DOCUMENTO_MESTRE_PRODUCAO_V1.md`, os dois no branch `docs/producao-v1`): 30+ criativos em 10 famílias (DM §79, §159), cada um com gancho de 0–2 s, preset de captura, métrica e disponibilidade. Os criativos já gravados (#1, #2 e #8) e o pipeline estão em [`CRIATIVOS.md`](CRIATIVOS.md). A lista original de 13 está no GDD §17, e as hipóteses do teste no GDD §16.

**Conta:** 37 criativos · **22 prontos** com o build atual (v0.6 + PRs abertos) · **15 bloqueados**: Wave A 1 · B 7 · C 4 · D 1 · G 2.

**Regras do banco** (guardrail "criativos gravados do jogo real", `CLAUDE.md`):
- Só entra gameplay do build com `-record` (1080×1920, relógio travado, mesmo comando = mesmos quadros). Trecho acelerado leva "VÍDEO ACELERADO" e os textos ficam na faixa y 1250–1620. Todo vídeo fecha com o cartão "FORGE STREET" e não leva selo de loja (`CRIATIVOS.md` v0.6).
- Estado montado com `-buyids` precisa ser alcançável no jogo (pré-requisitos na ordem certa). Compra `id@t` só vale se o ouro do log cobrir o preço no instante t (ver requisito 2 em a).
- Os vídeos saem mudos porque o `-record` não grava som. Som só depois do A-AUD-23 e de uma fonte licenciada (P11).
- Nada de recurso futuro: o que depende de Wave fica BLOQUEADO. A frase "sem anúncio forçado" só entra depois da decisão FS-5/P3 do Vinicius. Um VIP por vídeo, nunca chamado em sequência (P14).
- 15 s = corte para Reels, Shorts e TikTok; 30 s = variante longa.

**Notação dos presets.** `R` = `./ForgeStreet.exe $W -px 4.5,8.2 -record <pasta>` (com `$W`, `ENC` e os filtros do `CRIATIVOS.md`, seção "Reproduzir"). `P26` = `0,…,19,23,…,28` (os 26 produtivos). Ids (`Defs.cs`): 0 Fole · 1 Bigorna 2 · 2 Ajudante · 3 Escudos · 4 Mochila · 5 Ajudante 2 · 6 Esteira · 7 Fole duplo · 8 Botas · 9 Ferramentas · 10 Vitrine · 11 Fornalha 2 · 12 Ajudante 3 · 13 Ajudantes ágeis · 14 Martelo veloz · 15 Corredor · 16 Joalheria · 17 Joalheiro · 18 Lupa · 19 Vitrine de joias · 20–22 luxos · 23 Mineiro · 24 Joalheiro 2 · 25–28 Balcão 5–8. Os valores de `t` e de `-warmup` são calibrados pelo `RECORD OK t=… fila=… ouro=…` do log. O `-bot` compra sempre a melhoria mais barata que já dá para pagar (`Bot.cs`), então "o bot compra X" quer dizer que X é a mais barata que falta.

**Métricas** (DM §118): hook rate = visualizações de 3 s ÷ impressões; CTR = cliques ÷ impressões; IPM = instalações por mil impressões; CPI = gasto ÷ instalações. Faixas de CPI só as do GDD §15 (≤ US$0,70 promissor; 0,70–1,10 iterar; > 1,10 exige LTV maior). O repo não tem fonte para meta de hook rate nem de IPM, então a 1ª linha de base sai do Gate M (G-UA-06).

## Banco

| ID | Família | Gancho 0–2 s (tela · texto) | Arco 2–15 s | Payoff / CTA | Dur. | Preset de captura | Métrica | Disponibilidade |
|---|---|---|---|---|---|---|---|---|
| FS-C-01 | Transformação | Oficina com 1 bigorna apagada · "COMECEI COM UMA BIGORNA VAZIA..."¹ | Time-lapse do bot, "MINUTO 0→45" | "...E A OFICINA LOTOU!" com os 26 produtivos | 30 s (26,3) | Gravado: `fs1a`/`fs1b`/`fs1c` do `CRIATIVOS.md` v0.6 | CPI e IPM (hipótese do §16: a transformação reduz o CPI) | PRONTO (gravado, #1) |
| FS-C-02 | Transformação | Oficina completa com luxos · "ISSO AQUI..." → corte seco para o começo · "...COMEÇOU ASSIM" | Mesmo time-lapse, 2× mais curto | Fachada nobre e piso de oficina | 15 s | `fs1a` + `fs1b` do #1 + `R -buy 29 -warmup 90 -recordsec 4` | Hook rate × FS-C-01 (mesmo corpo com outra abertura, DM §119) | PRONTO |
| FS-C-03 | Transformação | Fila 4/4 e um cliente dando meia-volta · "MEU BALCÃO SÓ ATENDIA 4" | Menu aberto; compra Balcão 5→8 (150/175/200/225); o estande cresce a cada vaga (4×) | "8 DE UMA VEZ": fila de 8 andando | 15 s | `R -menu -buyids 0,1,2,3,5,6,7,9,10,12,14,25@t1,26@t2,27@t3,28@t4 -warmup 120` | CTR | PRONTO |
| FS-C-04 | Transformação | Ferreiro aprendiz · "ELE ERA ASSIM..." | Os 3 estágios do herói (B-ART-05) | Ferreiro mestre na forja cheia | 15 s | Novo preset `hero` (estágio por flag) | Hook rate | BLOQUEADO Wave B (B-ART-05, A-ART-04) |
| FS-C-05 | Gargalo | Fila 4/4, bigornas com fome · "FILA CHEIA? A FORNALHA NÃO DÁ CONTA" | Compra Fole + Fole duplo (740 → 385); 4× | "FILA ZERADA!" a 1× | 30 s (23,0) | Gravado: `-buyids 1,2,5,6,12,13,10,14,0@294.5,7@296 -warmup 290 -menu` | Hold de 3 s, CTR, CPI | PRONTO (gravado, #2) |
| FS-C-06 | Gargalo | Cliente chega com a fila cheia e vai embora; anel de paciência esvaziando · "ELES ESTÃO INDO EMBORA!" | Compra Fornalha 2 e Balcão 5–6; as meias-voltas param (4×) | "NINGUÉM SAIU": 1× sem meia-volta | 15 s | `R -buyids 1,2,3,5,9,10,12,11@t1,25@t2,26@t3 -warmup 240` | Hook rate, CTR (GDD §17 #6; 471 meias-voltas no POCO, `COMPETITIVO.md` c.6) | PRONTO |
| FS-C-07 | Gargalo | Close da fornalha com lâmpada vermelha e lingotes tombando · "A FORNALHA TRAVOU" | Ninguém tira os lingotes; compra Ajudante 2 e Esteira; bigornas acendem (4×) | "DESTRAVOU!": fumaça e faíscas a 1× | 15 s | `R -cam 7 -px <ao lado da fornalha> -buyids 0,2,7,5@t1,6@t2 -warmup 120` (conferir "saída cheia" na linha `SHOT`) | Hook rate | PRONTO (PR A-ART-06) |
| FS-C-08 | Gargalo | Fornalha cinza com minério fantasma no anel · "FORNALHA COM FOME" | 2 fornalhas rápidas para 1 ajudante de minério; compra Ajudantes ágeis (carga 2→4) | As duas fornalhas acesas a 1× | 15 s | `R -buyids 0,1,2,3,5,6,7,11,12,14,13@t -warmup 180` (conferir "sem minério" na linha `SHOT`) | Hook rate | PRONTO (PR A-ART-06) |
| FS-C-09 | Fail/fix | 4 bancadas e 1 fornalha sem fole · "O QUE ELE FEZ DE ERRADO?" | "BANCADAS APAGADAS... E A FILA QUASE NÃO ANDA" | "ACHOU O ERRO? COMENTA AÍ!" | 15 s (14,1) | Gravado: `-buyids 1,2,3,5,9,12,10,14 -warmup 120` | Hook rate + comentários por mil impressões | PRONTO (gravado, #8) |
| FS-C-10 | Fail/fix | Mesmo quadro do #8 · "A RESPOSTA:" → "FALTOU O FOLE" | Compra Fole e Fole duplo no menu com o ouro do log; bancadas acendem (4×) | "BANCADAS ACESAS" | 15 s | Preset do #8 + `-menu` e `0@t1,7@t2` | CTR (publicar como resposta aos comentários do FS-C-09) | PRONTO |
| FS-C-11 | Fail/fix | Ferreiro com "MAX" vermelho sobre a pilha e bancadas esperando · "FAZENDO TUDO SOZINHO?" | O bot carrega tudo e contrata Ajudante, depois Ajudante 2 (os mais baratos que faltam) | Ferreiro de mãos livres, balcão abastecido · "AGORA SIM" | 15 s | `R -bot -buyids 0,1,3,4,9 -hold 0,3,3,3,0,0`; cortar nos `Hired` do log | Hook rate | PRONTO |
| FS-C-12 | Cadeia satisfatória | Câmera perto, o ferreiro pega minério · "MINÉRIO..." | "...LINGOTE..." fornalha acende; "...ESPADA!" faíscas e pop na bigorna; balcão e moedas voando | "+10" e cartão | 15 s | `R -bot -cam 6.5 -recordsec 20` (save novo, 1×) | Hook rate, IPM (GDD §17 #5) | PRONTO |
| FS-C-13 | Cadeia satisfatória | Oficina completa: 4 linhas, ajudantes, fila de 8 · "SÓ OLHA ISSO" | Plano único a 1×, sem corte, quase sem texto | Cartão | 30 s | `R -buy 29 -warmup 120 -recordsec 30` | IPM (GDD §17 #10) | PRONTO |
| FS-C-14 | Cadeia satisfatória | Fornalhas em overdrive (boca 1,4×, faíscas em estrela), botão "→3×" · "VELOCIDADE 3×" | Produção 3× **no jogo** (rótulo "3× NO JOGO", sem aceleração de vídeo) | "+N" somados no balcão | 15 s | `R -buy 29 -boost 2 -warmup 20 -recordsec 15` (o boost dura 60 s) | CTR | PRONTO (PR A-ART-06)² |
| FS-C-15 | Cadeia satisfatória | Martelo-pilão em close, com som de metal e brasa | Ciclo completo em ASMR | Peça pronta e moeda | 15 s | Novo preset `audio` (o `-record` grava mudo) | Hook rate | BLOQUEADO Wave A (A-AUD-23, A-ART-07) + G-UA-02 |
| FS-C-16 | Escolha de upgrade | Tela dividida, duas oficinas iguais com a fornalha travada · "AJUDANTE 2 OU ESTEIRA?" | Esquerda compra Ajudante 2 (185), direita Esteira (240), no mesmo segundo; 4× por 90 s de jogo | Contador de ouro da HUD de cada lado · "QUAL VOCÊ ESCOLHERIA?"³ | 15 s | 2× `R -buyids 0,1,2,3,5@t -warmup t` / `…,6@t`; ffmpeg `hstack` | Hook rate + comentários | PRONTO |
| FS-C-17 | Escolha de upgrade | Menu aberto com Mochila, Botas e Fole duplo · "QUAL VOCÊ COMPRA PRIMEIRO?" | 3 cortes curtos de 3 execuções, um por compra, com o efeito visível na cena (mochila nas costas, poeira das botas, fole na fornalha) | "COMENTA A SUA" | 15 s | 3× `R -menu -bot -buyids 0,1,2,3 …` + `4@t` / `8@t` / `7@t` | Comentários, CTR | PRONTO |
| FS-C-18 | Escolha de upgrade | "QUALIDADE OU VELOCIDADE?" | A mesma linha com e sem o upgrade de qualidade | Peça superior × mais peças | 15 s | Split (como o FS-C-16) | CTR | BLOQUEADO Wave B (B-CORE-03 Quality, B-CORE-07 Blueprints) |
| FS-C-19 | Automação com worker | "ANTES": o bot faz tudo sozinho, indo e voltando | "DEPOIS": o mesmo estado com 3 ajudantes; o ferreiro fica parado | Balcão cheio e moedas · "CONTRATE AJUDANTES" | 15 s | `R -bot -buyids 0,1,3` + `R -buyids 0,1,2,3,5,12 -warmup 60` (rótulos ANTES/DEPOIS) | IPM (GDD §17 #4) | PRONTO |
| FS-C-20 | Automação com worker | Ferreiro sozinho no minuto 0 · "CONTRATEI 6 AJUDANTES" | Time-lapse com um selo a cada contratação ("AJUDANTE 1"... "JOALHEIRO 2") | Os 6 ajudantes trabalhando | 30 s | Reusa o `fs1b` do #1; marcadores pelos `Hired` do log do bot | IPM, CPI (GDD §17 #7) | PRONTO |
| FS-C-21 | Automação com worker | Ferreiro indo e voltando entre fornalha e bigorna (câmera perto) · "ELE NÃO PRECISA MAIS ANDAR" | O bot compra a Esteira (a mais barata que falta); os lingotes deslizam | Ferreiro livre para o balcão | 15 s | `R -bot -cam 8 -buyids 0,1,2,3,4,5 -recordsec 90` | Hook rate | PRONTO |
| FS-C-22 | Automação com worker | Cartão "Bem-vindo de volta!" · "FUI DORMIR..." | O ouro real do cofre (até 2 h, 25%) | "...E A OFICINA TRABALHOU" | 15 s | Novo preset `offline` (o `-cofre N` mostra um N arbitrário) | CTR, IPM | BLOQUEADO Wave G (G-UA-02 `offline`) |
| FS-C-23 | Automação com worker | Ajudante com silhueta de papel · "ELE SUBIU DE NÍVEL" | Nível e especialidade mudam o ritmo | Linha mais rápida | 15 s | Novo preset `worker` | IPM | BLOQUEADO Wave B (B-CORE-04, B-ART-09) |
| FS-C-24 | VIP | Aviso "Cliente VIP!" e cliente de coroa na fila · "ESTE CLIENTE PAGA 3×" | Pílula "×N", o pacote saindo do balcão | Moedas ×3 · "VIP SERVIDO" | 15 s | `R -buyids 1,2,3,5,6,10,12,14 -warmup 200 -vipnow -recordsec 30` | CTR, IPM | PRONTO⁴ |
| FS-C-25 | VIP | VIP esperando ao lado da fila cheia, anel de paciência descendo · "VAI DAR TEMPO?" | O bot corre com a carga; a fila anda | VIP servido no limite (evento `VipServed`) | 15 s | `R -bot -buyids 1,2,3,5,9,10,12 -warmup 200 -vipnow` (conferir fila 4/4 no `RECORD OK`) | Hook rate | PRONTO⁴ |
| FS-C-26 | VIP | Oficina fraca, VIP com o anel quase vazio · "PERDI O VIP..." | Ele cansa e vai embora (evento real `VipLeft`) | "NÃO DEIXE ISSO ACONTECER" | 15 s | `R -bot -vipnow -recordsec 150` (save novo, 1 bigorna; o VIP vem depois da 1ª venda real); 4× na espera. Conferir `VipLeft` no log: se ele for servido, o take vira FS-C-25 | Hook rate | PRONTO⁴ |
| FS-C-27 | VIP | Tapete ou carruagem na entrada do VIP | Entrada especial com som e FX | Pedido destacado e recompensa | 15 s | Preset `VIP` (a) | CTR | BLOQUEADO Wave B (B-ART-11, B-ART-10, A-AUD-23) |
| FS-C-28 | Masterwork | Anel de tempo na bigorna · "SÓ O MESTRE CONSEGUE" | Acerto perfeito, faísca dourada, câmera tremendo | Obra-prima (GDD §17 #11) | 15 s | Preset `Masterwork` (a) | IPM × FS-C-01 (`COMPETITIVO.md` FS-2) | BLOQUEADO Wave B (FS-2, B-CORE-03/08; decisão P10) |
| FS-C-29 | Masterwork | Peça brilhando no pedestal · "KING'S SUNBLADE" | Como ela foi feita | Registro no Hall | 15 s | Preset `Masterwork` | IPM | BLOQUEADO Wave B (B-CORE-08) + arte Wave F |
| FS-C-30 | Masterwork | Pedestais vazios na rua | As obras-primas aparecendo uma a uma | Galeria completa (FS-6) | 30 s | Preset `Masterwork` + time-lapse | IPM | BLOQUEADO Wave B (FS-6 depende do FS-2) |
| FS-C-31 | Distrito | Portão fechado da rua lateral · "TEM UMA RUA INTEIRA AQUI ATRÁS" | Compra Corredor (690) e Joalheria; os nobres chegam à loja de joias | "NOVA RUA ABERTA" (nunca "distrito") | 15 s | `R -buyids 0,…,14,15@t1,16@t2 -warmup 200` | CPI (proxy do gancho principal, DM §80) | PRONTO |
| FS-C-32 | Distrito | Old Forge completa · "O DISTRITO FICOU PEQUENO" | Royal Charter, mapa, Armorer's Row | Distrito novo abrindo | 15 s | Preset `district unlock` (a) | CPI | BLOQUEADO Wave C (C-CORE-01/02/03, C-UI-20, C-ART-13) |
| FS-C-33 | Distrito | Uma bigorna · "COMECE COM UMA BIGORNA..." | Time-lapse até o 6º distrito | "...TERMINE COMANDANDO UM DISTRITO INTEIRO" (DM §80) | 30 s | `full district` + `district unlock` | CPI | BLOQUEADO Wave C (C-CORE-05) + Wave F (§148) |
| FS-C-34 | Distrito | Forja rival do outro lado da rua com fila enorme | A sua oficina vira o jogo | Placa "VENDIDO" na forja dele (GDD §17 #13) | 15 s | Novo preset `rival` | Hook rate (`COMPETITIVO.md` FS-4) | BLOQUEADO Wave C (troca de rua) + FS-4 só depois do playtest |
| FS-C-35 | Royal Contract | Cartão "Venda 15 espadas" com a barra · "PEDIDO GRANDE" | Encomenda real enchendo, sem prazo nem falha | "Encomenda entregue! +N" (proxy honesto, sem "Royal") | 15 s | Novo preset `order` (o `-record` esconde o cartão e o aviso, `Game.cs:703`) | CTR | BLOQUEADO Wave G (G-UA-02 `order`) |
| FS-C-36 | Royal Contract | "EQUIPAR 20 GUARDAS" | Espadas, escudos e ferramentas enchendo o contrato | Recompensa do contrato | 15 s | Preset `Royal Contract` (a) | CTR | BLOQUEADO Wave D (D-CORE-04, B-CORE-05) |
| FS-C-37 | Royal Contract | Emissário real com pedido de 20 elmos (DM §19) | Linha de elmos de Armorer's Row | Baú de conteúdo fixo e visível | 30 s | Preset `Royal Contract` | IPM | BLOQUEADO Wave C (elmo) + Wave D |

¹ "VAZIA" quer dizer sem trabalho: a oficina já nasce com fornalha e depósito (`BENCHMARK_MERCADO.md` §3). Para um teste estrito, use a variante de texto "COMECEI COM UMA BIGORNA".
² FS-C-14 e os do VIP mostram o botão ▶ do rewarded (HUD real, opt-in). O vídeo não diz "grátis" nem "sem anúncio".
³ As duas metades são execuções reais, e o placar é o contador de ouro da própria HUD. Se a diferença ficar pequena, o vídeo termina na pergunta, sem inventar vencedor.
⁴ Um VIP por vídeo (P14). O `-vipnow` só vale com uma venda real no warmup, porque ele marca a 1ª venda quando ainda não houve (`Game.cs:959-963`).

## a) Presets do §78 → flags de hoje (spec do G-UA-02)

| Preset §78 | Com as flags de hoje | O que falta no `-preset <nome>` | Depende de |
|---|---|---|---|
| `start` | `R` com save novo (`-testsession`); `-bot` para andar (#1 `fs1a`) | Só o alias | — |
| `huge queue` | Fila 4/4: preset do #2. Fila 8/8: `-buyids 10,25,26,27,28 -warmup N` com produção fraca; conferir `fila=` no `RECORD OK` | Alias + checagem "fila = QueueCap no 1º quadro" | — |
| `first worker` | `-buyids 2@t` (Ajudante, 85) ou `-bot` partindo de um estado sem ajudante | Alias que começa a gravar no 1º `Hired` | — |
| `conveyor` | `-bot -buyids 0,1,2,3,4,5` (a Esteira passa a ser a mais barata) ou `6@t` | Alias + `-cam` seguindo o ferreiro | Esteira viva: B-ART-08 (Wave B), opcional |
| `VIP` | `-warmup N -vipnow` | Alias; recusar se o warmup não teve venda real (hoje o `-vipnow` marca a 1ª venda) | Entrada especial: B-ART-11 (Wave B), opcional |
| `full district` | Oficina: `-buy 29 -warmup 120` (área 1 + rua lateral + luxos) | Distrito de verdade | **Wave C** (C-CORE-01/02) |
| `Masterwork` | — | O bot acerta o anel num t fixo; obra-prima visível | **Wave B** (FS-2, B-CORE-03/08, P10) |
| `Royal Contract` | — (o `-shotorder` só fotografa a encomenda) | `-contract <id> -progress n` | **Wave D** (D-CORE-04) |
| `district unlock` | Interino: `-buyids …,15@t,16@t` (rua lateral) | Troca de distrito com transição | **Wave C** (C-CORE-02, C-UI-20) |

Presets fora do §78 que o banco pede:

| Preset | Hoje | Falta | Depende de |
|---|---|---|---|
| `order` | O `-record` esconde o cartão e o aviso da encomenda (`Game.cs:703`) | Manter cartão e aviso no `-record` | — |
| `offline` | O `-cofre N` mostra o painel com um N qualquer, sem dar o ouro | `-offline S`: roda o `Sim.ApplyOffline` real com S ≤ 7.200 s e mostra o cofre | — |
| `audio` | O `-record` grava mudo | Capturar o AudioListener em WAV no mesmo relógio do quadro | Só vale depois do A-AUD-23 (Wave A) |
| `split` | ffmpeg `hstack` de 2 execuções | Nada no jogo | — |
| `hero`, `worker`, `rival` | — | Estágio do herói, nível do ajudante, rival na rua | Waves B, B e C |

Requisitos comuns (aceite do G-UA-02):
1. **2 execuções = mesmos quadros** (SHA dos JPG). A simulação já é determinística com o relógio travado, mas a View sorteia com `UnityEngine.Random` sem semente nas faíscas e moedas (`WorldView.cs:1312-1321`, `:1510`, na `main`). O preset precisa chamar `Random.InitState` com uma semente fixa.
2. **Nenhuma compra de graça.** O `TimedBuys` zera o ouro quando falta (`Game.cs:975`) e não confere pré-requisito (`:974`). No preset, faltar ouro ou pré-requisito é erro e o jogo sai com 1.
3. O log ganha `PRESET <nome> ok`, além do `RECORD OK` com fila, ouro e o estado da fornalha.
4. Os textos continuam na faixa y 1250–1620; com `-menu`, em y 1250/1400.

**Faltam implementar:** `huge queue` (checagem), `first worker`, `conveyor`, `VIP` (trava da venda real), `order`, `offline` e o alias `-preset` com os requisitos 1–3, sem depender de Wave. Ficam para as Waves: `audio` (A), `Masterwork`, `hero` e `worker` (B), `full district`, `district unlock` e `rival` (C), `Royal Contract` (D).

## b) Os 8 do teste de CPI (GDD §16 A), em ordem de produção

Uma família por criativo, todos prontos. Os 3 primeiros já existem; os outros 5 vêm em ordem de custo de captura (1 execução antes de 2).

| # | ID | Família | O que fazer |
|---|---|---|---|
| 1 | FS-C-01 | Transformação | Já gravado; é o controle da hipótese "transformação reduz CPI" |
| 2 | FS-C-05 | Gargalo | Já gravado |
| 3 | FS-C-09 | Fail/fix | Já gravado (mede comentários) |
| 4 | FS-C-24 | VIP | 1 execução com `-vipnow` |
| 5 | FS-C-12 | Cadeia satisfatória | 1 execução, `-bot -cam 6.5`, 1× |
| 6 | FS-C-31 | Distrito (rua lateral) | 1 execução com `15@t,16@t` |
| 7 | FS-C-16 | Escolha de upgrade | 2 execuções + `hstack` |
| 8 | FS-C-19 | Automação com worker | 2 gravações (antes/depois) |

Reservas: FS-C-02 (variante de abertura do vencedor, §119), FS-C-10 (resposta ao FS-C-09) e FS-C-06. O vencedor ganha 2–3 variações de abertura, falha e payoff, mantendo o gancho (DM §119).

## c) Loja: 3 ícones e 2 sequências de 8 screenshots (DM §76–77)

Descrição em texto, sem arte. Tudo sai do jogo real: render do modelo do jogo no Blender (0 crédito) ou `-shot` 1080×1920 com as mesmas flags dos criativos. Crédito de IA só com aval do Vinicius. Cada peça passa pelo gate §73 e lê a 48 px (aceite do C-ART-14).

**Ícones** (o §77 lista 4 variantes; o close-up de Masterwork espera a Wave B):
- **I-1 Ferreiro + martelo.** Busto 3/4 do ferreiro com a bandana vermelha (código distintivo, GDD §28), martelo erguido e faísca dourada no impacto. Fundo em degradê de brasa laranja para o marrom da HUD (#2A1E14). Sem texto.
- **I-2 Bigorna + fogo.** Bigorna em close com um lingote incandescente; atrás, a boca da fornalha no estado overdrive (PR A-ART-06). Sem personagem: testa se "forja" sozinha lê a 48 px.
- **I-3 Ferreiro + ouro.** Ferreiro sorrindo com uma pilha de moedas (ícone de moeda da arte v0.5) e uma espada pronta no balcão. Testa a leitura de "tycoon".

**Sequência A, "Da bigorna à rua"** (transformação). Legenda curta em pt-BR na faixa de cima:
1. Oficina completa com luxos (`-buy 29 -warmup 120`): "MONTE A MELHOR FORJA DA RUA"
2. Começo, save novo: "COMECE COM UMA BIGORNA"
3. Cadeia em close (`-cam 7 -bot`): "MINÉRIO → LINGOTE → ESPADA"
4. Ajudantes carregando (`-buyids 0,1,2,3,5,12`): "CONTRATE AJUDANTES"
5. Esteira com lingotes: "AUTOMATIZE A PRODUÇÃO"
6. Rua lateral com joalheria e nobres: "ABRA UMA NOVA RUA"
7. Cliente VIP no balcão: "CLIENTES VIP PAGAM 3×"
8. Balcão de 8 vagas cheio: "ATENDA 8 DE UMA VEZ"

**Sequência B, "Ache o gargalo"** (decisão):
1. Fila cheia com cliente indo embora: "A FILA LOTOU. E AGORA?"
2. Fornalha com saída cheia e lâmpada vermelha: "ACHE O GARGALO"
3. Menu aberto com 3 cartões e preços: "ESCOLHA A MELHORIA CERTA"
4. Fornalhas em overdrive, botão "→3×": "ACELERE QUANDO QUISER"
5. Encomenda (`-shot … -shotorder`; no `-shot` o cartão aparece): "CUMPRA ENCOMENDAS"
6. Cofre "Bem-vindo de volta!" com o ouro real: "SUA OFICINA RENDE ATÉ 2 H FORA" (depende do preset `offline`)
7. Oficina completa a 1×: "SUA EQUIPE TRABALHA POR VOCÊ"
8. "SEM ANÚNCIO FORÇADO" só se o Vinicius fechar o FS-5/P3; até lá, entra a forja vista de cima com a fila de 8.

As duas sequências vão para um teste de página da loja (DM §120), como o FS-5 do `COMPETITIVO.md` já propõe para a frase.
