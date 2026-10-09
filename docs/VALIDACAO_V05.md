# Forge Street v0.5, leva 2: integração da arte na View (validação, 2026-10-08)

Escopo: a View passa a usar o que o core (FASE7) e a arte (ASSETS §7) entregaram. Core, testes e `Resources/Sprites` não foram tocados. Contrato: `docs/ASSETS.md` §7 (integração) e `docs/BENCHMARK_VISUAL.md` §3–4.

## O que mudou na View
| # | Pedido | Como ficou |
|---|---|---|
| 1 | Ícones dos itens | `Art.PaintItem`: `icone` no balão e nos cartões do menu; `deitado` na pilha da cabeça, nas pilhas das estações, no estoque da loja de joias, na esteira e nas bocas (0,45 m a 50%). Cor branca, sem `StackBg` e sem achatar o lingote. As máscaras antigas só entram se faltar a folha. |
| 1b | Pilha mista | Do mais grosso (embaixo) ao mais fino (em cima): minério, escudo, joia, ferramenta, lingote, espada. Cada item fica sobre o anterior com 25% de sobreposição; uma coluna acima de 1,6 m encolhe por igual. |
| 2 | Balão grande | Só o 1º da fila (balcão e loja de joias): balão de 1,32 × 1,12 m com ícone de 0,9 m. Anel de paciência na borda (verde → amarelo → vermelho, no sentido horário). Abaixo de 20% ele treme ±3° a 6 Hz. Os outros clientes têm mini-ícone de 0,42 m e uma barrinha de paciência. |
| 3 | Moedas | 3 a 6 sprites `moeda` (uGUI, por cima da HUD) voam do cliente até a moeda da HUD em 0,4 s, em arco. O número só sobe quando elas chegam (`Gold − _pending`), e a moeda e o número pulsam (1 → 1,15 → 1). O "+N" da venda saiu de cima do balão e agora sobe pela frente do estande. |
| 4 | Estande | Ponta esquerda + N módulos + ponta direita, na largura de `CounterHalfX(QueueCap)`. Ordem dos módulos: espadas, escudos, espadas, ferramentas, escudos, espadas, ferramentas, escudos (os N primeiros); uma linha que ainda não foi comprada vira rack de espadas. O estoque fica em pé nos `encaixes`, e o estande remonta com pop quando as vagas mudam. O sprite `balcao` com toldo saiu (o balcão procedural é a reserva). |
| 5 | Portões | `pilar` nas 4 pontas, `portao_*` no vão de cima e `porta_servico_*` no de baixo. A troca para aberto acontece na compra do Corredor. Saíram `Batente`, `Porta` e `arco`. |
| 6 | Fila cheia | Quem recebe `ClientLeft` B = 2 desce da rua de cima e vai embora pela esquerda. |

Calibração feita olhando as fotos: o topo da cabeça na tela fica a ~0,65 m do pé (ferreiro) e a 0,5–0,65 m (clientes). Antes era 0,95 m, e o balão e a pilha flutuavam uns 0,3 m acima da cabeça. Por causa do balão, `Game.ContentTop` passou de `WorldH + 1,6` para `+ 2,1`.

Flags de dev novas (`Game.DevArgs`, aplicadas depois do `-warmup`): `-hold 0,3,3,3,0,0` define a carga do ferreiro por Item (até o teto) e `-stock 10,10,10` define o estoque do balcão (espada, escudo, ferramenta, joia).

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet build client/tools/viewcheck/view -nologo -v q --artifacts-path <tmp>` (core das fontes) | 0 erros, 0 avisos |
| `dotnet test client/tools/coretests --artifacts-path <tmp>` | 72/72 |
| `Unity.exe -batchmode -nographics -quit -projectPath client -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v05/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v05/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=9 ouro=1320` |

Fotos em `client/Builds/validation_v05/shots/`, todas com `-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession` (salvo indicação) e logs em `validation_v05/logs/`:

| Foto | Argumentos |
|---|---|
| `01_inicio_4vagas.png` | `-warmup 40 -px 4.5,11.2 -shotdelay 2` |
| `01b_inicio_aspecto_aparelho.png` | igual, janela 432 × 960 (20:9, como o POCO) |
| `02_pilha_mista.png` | `-hold 0,3,3,3,0,0 -px 4.5,10.6` |
| `02b_pilha_mista_mochila.png` | `-buyids 3,4,9 -hold 2,6,6,3,2,0 -px 4.5,8.5` (19 itens, coluna encolhida) |
| `03_estande_8vagas_cheio.png` | `-buyids 3,9,10,25,26,27,28 -warmup 25 -stock 10,10,10 -px 4.5,11 -shotdelay 0.6` (moedas no ar) |
| `04_rua_fechada.png` | `-px 8.4,9.5` |
| `05_rua_aberta.png` | `-buyids 15,16 -warmup 30 -px 10.5,9.5` (portões abertos, balão do nobre, cliente da fila cheia passando) |
| `06_venda_moedas.png` | `-warmup 30 -stock 5 -px 4.5,11 -shotdelay 0.7` |
| `07_balcao_4para5_sequencia.png` | quadros de `-warmup 30 -buyids 10,25@30.6 -px 4.5,11 -record <tmp> -recordsec 1.6`: estande alarga com pop e a fila desliza |

## Fica para o aparelho
- Leitura a 1080 × 2400 do mini-ícone (0,42 m ≈ 40 px) e da barrinha de paciência.
- Custo dos sprites a mais: estande de 8 vagas com 34 renderers (10 peças + 24 itens), 36 na pilha e até 30 moedas.
- O PNG com o balcão antigo (`Resources/Sprites/balcao`) e o `arco` não são mais usados, mas continuam no build porque estão em `Resources`.

---

# v0.5b: resto do pacote visual do benchmark (validação, 2026-10-08)

Escopo: os itens do `BENCHMARK_VISUAL.md` §5 que a leva 2 deixou de fora (#6 HUD, #8 bocas e rótulos, #9 placas de obra, #10 cartões, #11 ambiente) e o juice da venda e da paciência. Só View (`Art.cs`, `Game.cs`, `MenuBar.cs`, `WorldView.cs`) e sprites novos (`docs/ASSETS.md` §8, `PROVENIENCIA.md` §6b). Core e testes não foram tocados (o núcleo VIP/velocidade da outra raia já estava no disco e compila com esta View).

## O que mudou
| # | Pedido | Como ficou |
|---|---|---|
| 6 | HUD | Faixa de cima de 0,15 → 0,075 da tela, sem painel cheio: pílula de ouro (borda clara, fundo `#2A1E14`, moeda 3D saindo pela esquerda, número de 62 px com contorno e sombra) e pílula da dica (negrito, contorno, até 2 linhas com best-fit). O número **rola** até o valor novo em ~0,3 s (sobe na venda, desce na compra). Versão saiu do título: `v0.x` de 20 px a 30% no canto de baixo à esquerda (criativo `-record`: nenhuma). Vinheta de bordas a 30%. Todo painel uGUI agora é 9-fatias (canto redondo de verdade). |
| 6b | Dica no mundo | Seta dourada com contorno, quicando 0,15 m sobre o alvo da dica (placa da vez, boca onde depositar/recolher, baú, depósito); some com o jogador em cima. Sem alvo no mundo (Melhorias, cliente esperando): sem seta, o badge pulsa. Texto do pad: "Pise na placa: …". |
| 10 | Cartões | Creme `#FFF1D6` com borda de couro e chanfro de 8 px, nome, ícone renderizado grande, efeito em número com seta verde ("3 → 6", "4 → 2,5 s", "4 → 5 vagas", lidos do `Balance`), botão de preço **verde** (dá para comprar) ou **cinza** (não dá), "requer X" no cinza quando travado. Mochila ≠ Botas: 9 ícones novos no Blender (`melhoria_*`). Botão Melhorias com martelo de ouro e **bolinha vermelha** com o número (pulsa). |
| 9 | Placas de obra | Placa de 1,2 m no chão (fundo escuro, borda tracejada branca que pulsa em ouro quando dá para pagar, enchimento de ouro pelo que já foi pago) com o **ícone do que nasce ali** (bigorna, fornalha, bancadas, portão, ajudante, lingote da esteira; estrela no luxo) e preço com moeda dentro. Nome só perto ou no 1º minuto. Pagando: moedas voam da cabeça do ferreiro para a placa (1 a cada 0,06 s). Concluiu: poeira e a estação nasce com pop 0 → 1,1 → 1 em 0,35 s; ajudante contratado também solta poeira. |
| 8 | Bocas e rótulos | Anel com "▶" virou placa no chão: **grelha de ferro** na entrada (item que entra a 55%; balcão = moeda) e **palete de madeira** na saída (a pilha fica em cima). Barra de progresso em cápsula 1,2 × 0,16 m com o ícone do produto na ponta. Nome da estação só a até 2,6 m do jogador ou no 1º minuto de jogo, em negrito com contorno, e embaixo da barra em quem produz (em cima batia no preço da placa da Esteira). |
| 11 | Ambiente | Chão da oficina lavado de luz quente (laje ~`#493F36` → ~`#7A6A58`, sobreposição alfa, sem shader novo), ruas com luz fria (a lateral só depois do Corredor), tampo claro na borda de dentro das paredes, pilares nos 4 cantos e no meio da parede esquerda, poça de luz quente pulsando nas fornalhas que trabalham, tochas e postes com brilho de queda suave. Exterior: grama procedural no lugar do calçamento a 42%, 7 árvores, 4 arbustos, 3 canteiros e 3 postes (Blender, `ui_v05b_blender.py`). |
| juice | Venda e paciência | Na venda o balão estoura (1 → 1,25 → 0 em 0,15 s) e volta com sobra no próximo; sobe um coração de 0,5 m com 6 brilhos. Abaixo de 20% de paciência, rosto bravo de ~0,5 m (48 px) no canto do balão, pulsando. "+N" flutuante com contorno. |
| — | Lote 4 | `CharScale` dos 10 clientes novos (valores do coordenador, ASSETS §9). |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet build client/tools/viewcheck/view -nologo -v q --artifacts-path <tmp>` (core das fontes, com o VIP/velocidade) | 0 erros, 0 avisos |
| `Unity.exe -batchmode -nographics -quit -projectPath client -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v05b/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v05b/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310` |

Fotos em `client/Builds/validation_v05b/shots/` (janela `-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession`, salvo indicação; logs em `validation_v05b/logs/`). Cada uma foi aberta e ampliada; o que não leu bem foi refeito (abaixo).

| Foto | Argumentos | Conferido |
|---|---|---|
| `01_inicio.png` | `-warmup 40 -px 4.5,11.2 -shotdelay 2` | HUD fina, placas com ícone e preço, grelha/palete, chão claro, pilares, rótulos do 1º minuto |
| `01b_inicio_20x9.png` | igual, janela 432 × 960 (20:9) | exterior aparece: árvores, canteiros, postes, grama |
| `02_menu_aberto.png` | `-gold 200 -menu -warmup 20 -px 4.5,8 -shotdelay 2` | cartões verdes (Fole, Mochila), travados "requer …", badge 2 |
| `02b_menu_20x9.png` | igual, 432 × 960 | 3 cartões visíveis, texto cabe |
| `03_placa_pagando.png` | `-gold 300 -px 4.5,9.5 -shotdelay 0.45` | placa enchendo de ouro, 35 restantes, moedas no ar |
| `03b_obra_pop.png` | `-gold 300 -px 4.5,9.5 -shotdelay 1.15` | poeira, Bigorna 2 nascendo, "2ª bigorna!" com contorno |
| `04_oficina_meio_luz.png` | `-buy 12 -warmup 70 -gold 900 -bot -shotdelay 9` | estações trabalhando, poça de luz nas fornalhas, só o rótulo perto, seta da dica no balcão, rosto bravo |
| `05_rua_lateral.png` | `-buyids 15,16 -warmup 30 -px 10.5,9.5 -shotdelay 2` | rua lateral com luz fria, placa do Joalheiro, grelha da loja de joias |
| `06_fila_cheia_lote4.png` | `-buyids 3,9,10,25,26,27,28 -warmup 50 -px 4.5,10.5 -shotdelay 2` | 8 clientes do elenco novo lado a lado, proporção coerente com o `CharScale` |
| `07_venda_coracao.png` | `-warmup 30 -stock 5 -px 4.5,11 -shotdelay 0.35` | coração subindo do balão, moedas indo para a HUD, "+10" |
| `08_cliente_bravo.png` | `-warmup 57 -px 4.5,10.5 -shotdelay 1.5` | rosto bravo no canto do balão (anel vermelho) |

Refeito depois de olhar: ícone da placa pequeno (a arte ocupa 62–77% da célula: caixa dividida pela fração medida nos PNG); preço da placa da Esteira em cima do nome "Bigorna" (nome de quem produz foi para baixo da barra); rótulo a meio-alfa lia como defeito (fade de 0,6 → 0,25 m); "requer Balcão 5 vagas" vazava do botão (best-fit com `Truncate`); poça de luz da fornalha sumia sob o sprite (queda `(1−d)²` → smoothstep invertido, 4 m a 30%); pílula da dica deixava ver o poste atrás (80% → 92%); moedas da placa quase invisíveis (arco 1,1 → 1,6 m, 0,44 m).

## Fica de fora / para o aparelho
- Fonte display OFL (Lilita One/Fredoka): não há arquivo no projeto e baixar fonte pede aval; ficou Arial negrito + `Outline` + `Shadow`.
- Câmera deslizando até a próxima placa liberada e "MAX" na pilha: não feitos (mexem na câmera/pilha que a raia do VIP vai tocar).
- Custo no aparelho: +120 renderers no início e até ~190 com tudo comprado (bocas ~9 por estação, barras 3, exterior 40, brilhos 11, placas 4 por placa visível) e até 64 efeitos; vinheta + 3 lavagens de luz = ~2 telas de overdraw alfa a mais. Medir FPS no POCO F4 com o APK.
- Leitura a 1080 × 2400: preço da placa (34 px de referência ≈ 38 px), nome de estação (28 px) e o rosto bravo (0,5 m ≈ 48 px).

---

# v0.5c: VIP e velocidade na tela (validação, 2026-10-08)

Escopo: a View do núcleo FASE8 (`docs/FASE8_VIP_VELOCIDADE.md` §5) com o wrapper de anúncio da raia LevelPlay (`Ads.Ready`/`Ads.Show`, placements `vip` e `velocidade`, simulado de 5 s no Windows). Só View (`Art.cs`, `Game.cs`, `WorldView.cs`) e 1 sprite novo (`coroa`, `tools/ui_v05b_blender.py`). Core, testes e `Ads.cs` não foram tocados.

## O que mudou
| # | Pedido | Como ficou |
|---|---|---|
| 1 | VIP na tela | **Coroa** de ouro de 0,46 m "vestida" na cabeça de qualquer VIP (na vaga, na fila, esperando ou indo embora). 1º da fila: balão com **moldura de ouro**, corpo creme, **selo "×3"** dourado no canto de cima e **"×N"** grande do pacote no canto de baixo, que cai a cada unidade entregue (cada unidade estoura o balão com coração, como a venda normal). VIP fora do 1º lugar: mini-ícone sobe 0,16 m e "×N" ao lado. **Fila cheia:** o VIP pendente desce da rua e espera de coroa, 0,95 m à direita da última vaga, olhando em volta, com "VIP" em cima; quando abre vaga ele entra andando de onde estava. |
| 1b | Eventos | `VipArrived`: aviso "Cliente VIP!" com coroa (pop, 2 s), som novo `vip` e anel de brilhos na vaga. `VipServed`: "+total" de 64 px pela frente do estande, 2 corações, anel de brilhos e 12 moedas de enfeite voando para a HUD (o ouro já entrou unidade por unidade). `VipLeft`: "..." vermelho e som de saída; ele vai embora pela esquerda, de coroa. |
| 2 | Botões | Cantos da barra de baixo, fora da fila e do joystick: **"Chamar VIP"** (coroa + selo de vídeo) só com `Sim.CanSummonVip && Ads.Ready(Ads.Vip)`; **"Velocidade"** (avanço rápido + selo de vídeo) com `Sim.CanBoost && Ads.Ready(Ads.Velocidade)`, mostrando o próximo multiplicador ("2×", ou "3×" com o boost ativo). Sem boost e em recarga: cinza com o tempo (`Sim.BoostCooldown`, "4:42"). No 3× o botão some (o selo diz tudo). **Selo do boost** logo acima do botão de velocidade: avanço rápido, "2×"/"3×" e cronômetro (`Sim.BoostLeft`), pulsando. Toque: `Ads.Show(placement, onReward: Sim.SummonVip()` (+ aviso "VIP a caminho!") `/ Sim.StartBoost()` (+ "Velocidade 2×!" no ferreiro)`, onFail:` aviso curto "Anúncio indisponível")`. A base dos botões sobe 2,6% para a marca "Development Build" do APK de teste não cobrir a legenda; a versão foi para cima do botão VIP. |
| 3 | Diário | `vip_arrived` a=item b=unidades, `vip_served` a=item b=ouro, `vip_left` a=item b=faltaram, `boost_start` a=multiplicador b=60 (nomes e campos do `client/tools/diario_report.py`; os `ad_*` continuam saindo do `Ads.Logged`). |
| 4 | Flags | `-vipnow`: chama o VIP depois do `-warmup` (fila cheia = ele espera ao lado; marca a 1ª venda se ainda não houve, cheat de dev); se ainda não pode, tenta a cada quadro. `-boost N`: N anúncios de velocidade **antes** do warmup (`-boost 1 -warmup 75` = foto da recarga). |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet build client/tools/viewcheck/view -nologo -v q --artifacts-path <tmp>` (core das fontes) | 0 erros, 0 avisos |
| `dotnet test client/tools/coretests --artifacts-path <tmp>` | 78/78 |
| `Unity.exe ... -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v05c/build_win.log` | "Build Finished, Result: Success." (com o `SpriteImport` em DXT5 no Windows) |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v05c/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310` |
| Borda dos sprites com DXT5 | comparada ampliada com a foto da v0.5b (estação, placa, ferreiro, itens): sem borrão visível |

Fotos em `client/Builds/validation_v05c/shots/` (janela `-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession`, salvo indicação; logs em `validation_v05c/logs/`). Todas abertas e ampliadas.

| Foto | Argumentos | Conferido |
|---|---|---|
| `01_botoes_visiveis.png` | `-warmup 30 -stock 5 -px 4.5,11 -shotdelay 2` | "VIP" e "2×" nos cantos, com selo de vídeo |
| `02_vip_primeiro.png` | `-warmup 3 -vipnow -px 4.5,10.5 -shotdelay 3` | coroa, moldura de ouro, "×3" e "×6" |
| `02b_aviso_vip.png` | igual, `-shotdelay 1.2` | aviso "Cliente VIP!" com coroa |
| `03_vip_na_fila.png` | `-warmup 22 -vipnow ...` | VIP na 4ª vaga: coroa e "×6" ao lado do mini-ícone |
| `04_vip_esperando.png` | `-warmup 40 -vipnow ...` | fila 4/4, VIP de coroa esperando à direita com "VIP" |
| `05_boost_ativo.png` | `-boost 1 -warmup 12 -stock 5 ...` | selo "2× 0:45", botão oferecendo "3×" |
| `05b_boost_3x.png` | `-boost 2 -warmup 12 -stock 5 -buyids 10,25,26,27,28 ...` | selo "3× 0:45", botão de velocidade some, fila de 8 vagas sem bater no selo |
| `06_recarga.png` | `-boost 1 -warmup 75 ...` | botão cinza "4:42" sem selo de vídeo |
| `07_anuncio_simulado.png` | `-warmup 30 -stock 5 -adtest velocidade -shotdelay 2.5` | painel "Anúncio de teste", recompensa Velocidade x1, contagem |
| `08a_vip_pacote_caindo.png` | `-warmup 3 -vipnow -stock 6 ... -shotdelay 2.9` | "×1" depois de 5 unidades (estoque preso em 5 sem a Vitrine) |
| `08b_vip_servido.png` | `-buyids 10 -warmup 3 -vipnow -stock 8 ... -shotdelay 2.5` | "+180", corações, moedas indo para a HUD |
| `09_botoes_boost_20x9.png` | `-boost 1 -warmup 30 -stock 5 -buyids 10,25,26,27,28`, janela 432 × 960 | selo, "VIP", "3×" e badge legíveis em 20:9 |

Refeito depois de olhar: moldura de ouro própria (o trilho dourado sumia sob o anel cheio de paciência); coroa 0,40 → 0,46 m; "play" do selo de vídeo escuro (vermelho em branco lembrava marca); aviso "Cliente VIP!" não aparecia (o 1º quadro tem `unscaledDeltaTime` de segundos: passo limitado a 0,1 s); a marca "Development Build" cobria a legenda do botão de velocidade (base subiu); o selo do boost no topo batia no balão do 1º da fila com 8 vagas (foi para cima do botão); "×3"/"×1" ficavam soltos quando a fila esvaziava (rótulos desligados com o balão); "+180" subia para baixo da faixa da HUD (agora sai pela frente do estande); ripas das bocas sem ordem fixa (placa 0, ripas 1, item 2).

## Fica de fora / para o aparelho
- Diário não conferido em arquivo: `-testsession` (todas as fotos e o smoke) não grava diário de propósito, e uma sessão normal mexeria no save real do PC. Os nomes e campos batem com o `diario_report.py`.
- Aviso "Anúncio indisponível" sem foto: no Windows o simulado sempre tem anúncio. Conferir no aparelho sem rede.
- O VIP pendente não mostra o item (o Core não expõe o `_vipWant`); ele aparece só com coroa e "VIP".
- No aparelho: medir se o "×N" de pacote grande (até 20) lê a 1080 × 2400 (44 px de referência) e se o selo de 0,56 m com "×3" de 30 px lê.

## v0.5 no emulador do PC (2026-10-09, coordenador)
- AVD `fs_playstore` (Android 15 Google Play x86_64, 1080×2400, 2 GB). Ligar SEMPRE com `-crash-report-mode never -no-metrics` (senão, depois de um `adb emu kill`, ele para num diálogo de relatório de erro com envio automático marcado).
- O APK de aparelho (ARM64) **cai** no emulador: `berberis: Cannot process signal 11` (tradutor ARM→x86 do Android 15 com IL2CPP). Não é bug do jogo. Para o PC: `FS.EditorTools.Setup.BuildAndroidEmu` → `Builds/android/ForgeStreet-emu.apk` (x86_64 nativo, 73,4 MB); depois dele, `git checkout client/Assets/Plugins/Android/mainTemplate.gradle client/ProjectSettings/AndroidResolverDependencies.xml` (o resolver do LevelPlay exclui as libs ARM64).
- Resultado: abre, LevelPlay inicializa (`ad_init ok 9.5.1`), sem anúncio real (`509 Mediation No fill` — conta ainda não aprovada / sem test device) → build de desenvolvimento cai no **anúncio simulado**; toque em "2×" → anúncio de teste 5 s → recompensa → selo "2× 0:56" e botão oferecendo "3×". Fotos `client/Builds/validation_emulador/` (01 queda ARM64, 02 abertura x86_64, 03 anúncio, 04 boost).
- Diário no Android lido pelo `diario_report.py` (seção de anúncios conta pedidos/mostrados/recompensas/falhas). No Git Bash, `adb pull /sdcard/...` precisa de `MSYS_NO_PATHCONV=1` (senão o caminho vira `C:/Program Files/Git/sdcard/...`).
- Desempenho do emulador NÃO vale para o POCO (CPU do PC); FPS e memória só no aparelho.


---

# v0.5.1: correções da revisão de código (2026-10-09, coordenador)

Um revisor leu o diff `main...feat/v0.5` (Core + View + `diario_report.py`) procurando bugs que o jogador veria no POCO. Sem crash, save corrompido ou pagamento em dobro. Seis achados, todos corrigidos:

| # | Achado | Correção | Prova |
|---|---|---|---|
| 1 | **Ajudante 3 parado no balcão** com espada e a prateleira de espada cheia; escudo e ferramenta encalhavam (sem a compra direta, o estoque só desce com cliente) | `Sim.Fits` no `IsSourceFor`/`PickSource`: só busca o que cabe (regra do `JewelerSource`) | `Ajudante3_NaoBuscaProdutoComPrateleiraCheia` e `Ocioso_15Min_AjudantesVendemSozinhos` (parado 15 min: 80 → 209 vendas), vermelhos na 0.5.0; `BALANCE.md` §19 |
| 2 | Carga dos ajudantes sumia ao reabrir se o Joalheiro/Mineiro/Joalheiro 2 veio antes do Ajudante 3 (save na ordem da compra, load na do `HireBy`) | `wk=papel:contagens`; load vai para o próximo ajudante vazio do papel; save da 0.5.0 segue pela posição | `Save_CargaDosAjudantes_ContratadosForaDeOrdem` (vermelho na 0.5.0) + caso "save da 0.5.0" no teste de save |
| 3 | VIP que esperava ao lado da fila cheia sumia e outro boneco caía do alto ao abrir a vaga | `RefreshVipWaiting` depois do `RefreshQueue` (o `BindClient` acha o `_vipWait`) | viewcheck; leitura do fluxo |
| 4 | Voltar de anúncio real de 60 s+ abria "Seu cofre rendeu" | `OnApplicationPause(true)` guarda `Ads.Busy`; a volta de anúncio não paga cofre | viewcheck; conferir no POCO com anúncio real |
| 5 | VIP restaurado do save repetia aviso, som e `vip_arrived` a cada abertura e perdia o ouro já pago | `vip=` ganha 2 campos (estava na vaga, já pagou); save de 4 campos ainda abre | `Save_RelogioEVipPendente_…` estendido |
| 6 | Recompensa de velocidade podia sumir se o boost acabasse no segundo antes de o anúncio real cobrir a tela | Jogo parado enquanto `Ads.Busy` (o simulado já parava pelo `timeScale`), teto de 15 s para SDK que não responde | viewcheck |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | **81/81** (3 testes novos, 2 estendidos) |
| viewcheck | 0 erros, 0 avisos |
| Build Windows + `-autoplay 10` | Success; `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310` |
| Bot no build a 20× por 20 min (`-bot -speed 20 -vipnow`) | 19 upgrades, 0 exceções (`validation_v051/bot20.log`, foto `validation_v051/shots/01_bot_20min.png`) |
| Build Android ARM64 | Success; `ForgeStreet-dev.apk` **0.5.1**, 72 504 619 B, SHA-256 `3ed4260c…e4ac` (refeito com a flag `-cam` e o voltar do Android que chega ao jogo) |

Fica para o aparelho: #4 e #6 só aparecem com anúncio real (conta LevelPlay aprovada). A paciência do VIP restaurado continua cheia ao reabrir (decisão da FASE8 §4).

## Flag de câmera para o playtest (0.5.1)
- `-cam W`: largura visível em metros (6–11,4; padrão 11,4 = oficina inteira). Abaixo do padrão a câmera segue o jogador pelos clamps que já existiam. Experimento P2-3 do `BENCHMARK_VISUAL.md`, não muda o padrão.
- No build de desenvolvimento o `Game.Arg` passa a ler também o extra `unity` do intent (o `Ads.Arg` já lia), então todas as flags de dev valem no aparelho via `adb shell am start ... -e unity "..."`. Release: só a linha de comando.
- Verificação: viewcheck 0/0; build Windows Success; `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310`; fotos `validation_v051/shots/02_cam9.png` e `02_cam8.png` (personagens ~25% e ~40% maiores; a 8 m corta uma coluna de estações e o balão do 1º da fila encosta na borda esquerda).

## Voltar do Android (0.5.1, achado da revisão da v0.6)
- O voltar (= Esc) salvava e saía mesmo com o "Seu cofre rendeu" aberto, que aparece em quase toda abertura depois de tempo fora. Agora fecha o painel, depois a bandeja de melhorias, e só sai com a tela limpa (`MenuBar.IsOpen`).
- viewcheck 0/0; build Windows Success; `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310`; APK ARM64 72 506 143 B, SHA-256 `e9cb5c0f…852e`.
- **Armadilha do build:** depois de trocar de branch, o empacotamento incremental do Gradle deixou ~12 MB de buracos no APK (84,8 MB com as mesmas entradas comprimidas de 72,4 MB). Apagar `client/Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build` antes do build Android resolve.

## O voltar não chegava ao jogo no Android (teste no emulador, 2026-10-09)
- No emulador (API 35), nem o código antigo (salvar e sair) nem a correção acima rodavam: o GameActivity da Unity 6 não entrega o voltar ao Input System (nenhum evento de teclado; problema conhecido, fórum Unity 1555368 / UUM-136080). O Predictive Back desligado também não resolve.
- Correção: `activeInputHandler` = 2 (Input System + Input Manager legado, no `Setup` e no ProjectSettings) e `Input.GetKeyDown(KeyCode.Escape)` no `Game.Update`. No emulador: voltar fecha a bandeja (`validation_v051/shots/04`→`05`) e, com a tela limpa, sai do jogo.
- O `Setup.Build` agora apaga a saída incremental do Gradle antes de todo build Android e o `BuildAndroidEmu` tira o exclude de `/lib/x86_64` do `mainTemplate.gradle` durante o build (detalhes em `VALIDACAO_V06A.md` §Android).
- Build Windows Success, `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310`; APK ARM64 72 509 035 B (7 bibliotecas), SHA-256 `3ed4260c…e4ac`.