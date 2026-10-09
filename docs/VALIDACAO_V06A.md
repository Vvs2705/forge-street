# v0.6a: juice da produção e upgrades visíveis (validação, 2026-10-09)

Escopo: `BENCHMARK_VISUAL.md` P2-1 (partículas da produção), P2-2 (upgrade aparece na estação) e o "MAX" da P1-5. Só View (`WorldView.cs` e uma linha no `Game.cs`); Core e testes não foram tocados. Sem arte nova: os props são os ícones `melhoria_*` que o menu já usa. `bundleVersion` continua 0.5.1. Branch `feat/v0.6-juice`, a partir da 0.5.1 (`b2af75a`).

## O que mudou
| # | Pedido | Como ficou |
|---|---|---|
| P2-1 | Bigorna e bancadas | Enquanto a estação trabalha (`Busy`), uma martelada a cada 0,45 s: 8–10 faíscas (brilho de 4 pontas de 0,26 → 0,08 m, girando) saem do tampo em leque para cima e caem em arco, de `#FFD166` a `#FF7A1F` em 0,3 s; a arte da estação dá punch 1 → 1,06 → 1 em 0,12 s. Ponto de saída medido nos PNG: topo da bigorna (−0,18; 0,45 m do pivô), tampo das bancadas (0; 0,3). |
| P2-1 | Fornalha | 3 baforadas de fumaça por segundo pela chaminé (disco cinza a 50%, 0,2 → 0,6 m, sobe 1 m em 1,1 s e some) e a boca acesa: brilho laranja `#FF7A1F` de 0,7 m sobre a arte, pulsando no mesmo ritmo da poça de luz do chão (que já existia e passou a pulsar ±20%). Parada, a boca apaga. |
| P2-1 | Item pronto | `Ev.Crafted` → o item mais novo da pilha de saída dá pop 0 → 1,15 → 1 em 0,2 s. |
| P1-5 | "MAX" | Vermelho `#FF2D2D`, negrito itálico com contorno escuro, sobre a pilha do ferreiro, quando ele está numa boca de onde pegaria um tipo que já está no teto (depósito, ou saída com produto pronto: a mesma regra do `Sim.CanPick`). Fica enquanto ele insiste e some 0,8 s depois saindo; o pop (0 → 1,25 → 1) só roda quando estava apagado, então não pisca. |
| P2-2 | Upgrades visíveis | Fole: ícone de 0,45 m na parede esquerda de cada fornalha (a direita já tem o fole da arte); Fole duplo troca pelo ícone duplo (0,52 m). Martelo veloz: martelo dourado de 0,5 m encostado no lado direito de cada bigorna e bancada. Mochila: 0,38 m nas costas do ferreiro, atrás da pilha; de frente fica atrás do corpo, de costas na frente, de lado do lado oposto ao olhar. Botas: poeirinha no pé andando (até 3 por segundo). Os props ligam pela flag com o mesmo pop dos luxos (0,6 → 1 em 0,3 s); save e `-buy` mostram prontos. |
| — | Orçamento de efeitos | Faísca, fumaça e poeira só nascem com menos de 40 efeitos vivos (pool de 64), e só com a estação na câmera: venda, moedas da placa e poeira de obra sempre têm vaga. O punch roda sempre (é uma escala). |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | 81/81 |
| `dotnet build client/tools/viewcheck/view -nologo -v q --artifacts-path %TEMP%\vc6` | 0 erros, 0 avisos |
| `Unity.exe -batchmode -nographics -quit -projectPath client -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v06a/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v06a/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310` (igual à v0.5b: a View não mexe na simulação) |
| Bot 20 min: `-bot -speed 20 -shotdelay 62` (janela) | `SHOT t=1240,2 upgrades=18`, 0 exceções no log (`logs/06_bot_20min.log`) |

Fotos em `client/Builds/validation_v06a/shots/` (janela `-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession`; logs em `validation_v06a/logs/`). Cada uma foi aberta e ampliada; os `*z_*_zoom.png` são recortes ampliados das fotos ao lado.

| Foto | Argumentos | Conferido |
|---|---|---|
| `01_oficina_tudo.png` + `01z_upgrades_zoom.png` | `-buy 15 -warmup 70 -gold 900 -bot -shotdelay 9` | Fole duplo nas 2 fornalhas, martelo dourado nas 2 bigornas e nas 2 bancadas, fumaça subindo das 2 chaminés, bocas acesas, faíscas nas bancadas de escudos e ferramentas |
| `02_faiscas_bigorna.png` + `02z_faiscas_zoom.png` | `-buy 15 -warmup 120 -bot -shotdelay 4.2` | faíscas amarelo → laranja sobre a Bigorna 2 e a bancada de escudos (as bigornas paradas, sem faísca) |
| `03_fumaca_fole.png` + `03z_fumaca_fole_zoom.png` | `-buyids 0,2,5 -warmup 20 -px 4.5,3 -shotdelay 2` | Fole simples na parede esquerda, 3 baforadas crescendo acima da chaminé, boca acesa, fornalha trabalhando |
| `04_mochila_botas.png` + `04z_mochila_botas_zoom.png` | `-buyids 4,8 -bot -shotdelay 3.6` | ferreiro de costas na entrada da fornalha: mochila nas costas (rolo e fivelas) atrás da pilha de minério; poeirinha no pé |
| `05_max_pilha.png` | `-hold 3 -px 1.5,2.15 -warmup 65 -shotdelay 1.5` | "MAX" sobre 3 minérios no depósito (teto 3); depois do 1º minuto o rótulo da Fornalha não cruza mais com ele |
| `05b_max_bigorna.png` | `-buyids 2,6 -hold 0,0,3 -px 2.4,9.5 -warmup 65 -shotdelay 1.5` | "MAX" sobre 3 espadas na saída da Bigorna com espada pronta no palete |
| `06_bot_20min.png` | `-bot -speed 20 -shotdelay 62` | fim do bot de 20 min: oficina íntegra, nenhum efeito preso na tela |

Refeito depois de olhar: faíscas de 0,16 m sumiam na foto (7 px a 540 de largura): 0,26 m e leque mais aberto (0,45–0,8 m); fumaça 0,5 → 0,6 m e alfa 45 → 50%; martelo 0,45 → 0,5 m e mais para baixo (batia na lamparina da bigorna); a foto da fumaça com 1 ajudante pegava a fornalha travada (saída cheia): com o Ajudante 2 levando os lingotes ela trabalha.

## Fica de fora / para o aparelho
- Pop do item pronto e punch da martelada sem foto: duram 0,2 s e 0,12 s; conferidos no código (`Juice`, `RefreshStation`), ver no aparelho.
- Mochila de frente (andando para baixo) fica escondida atrás do corpo, como uma mochila de verdade; ela lê andando para cima e de lado.
- A Joalheria também ganha faíscas e martelo dourado (é bancada, e o Martelo veloz acelera a joia também: `HammerTime × JewelTimeMul`). Trocar as faíscas por brilho roxo se o playtest estranhar.
- No POCO: medir se 9 faíscas × 4 bancadas + fumaça pesam no FPS (pool de 64 sprites, sem alocação por quadro) e se o "MAX" de 46 px de referência lê a 1080 × 2400.
---

# v0.6b: configurações com som e vibração (validação, 2026-10-09)

Escopo: `BENCHMARK_VISUAL.md` P2-4 (vibração) e o resto da P1-1 (versão sai da HUD para as configurações). Só View (`Game.cs`, `Art.cs`, uma linha no `WorldView.cs` e a paleta do `MenuBar.cs` pública); Core e testes não foram tocados. Sem `AndroidManifest.xml` próprio. `bundleVersion` continua 0.5.1; o APK de amanhã (`ForgeStreet-dev.apk`, SHA `b2f26113…`) não foi refeito.

## O que mudou
| # | Pedido | Como ficou |
|---|---|---|
| P1-1 | Engrenagem na HUD | Botão à direita da dica, dentro da `AreaSegura`, no estilo da pílula de ouro (borda `#F2D9A0`, fundo `#2A1E14`, engrenagem de 8 dentes desenhada por código). A dica encolheu de 0,385–1 para 0,385–0,875 da faixa e continua quebrando em 2 linhas. No `-record` não há engrenagem e a dica volta à largura inteira. |
| P1-1 | Cartão de configurações | Cartão creme com borda de couro e chanfro (a paleta dos cartões do menu), título "Configurações" em Arial negrito, Som e Vibração como chaves verde (LIGADO) / cinza (DESLIGADO) com bolinha branca e texto com contorno, linha "Forge Street v0.5.1" e FECHAR. O voltar do Android (Esc) fecha o cartão em vez de sair do jogo. |
| — | Comportamento | **Não é pausa.** Como o painel do cofre: o joystick e o WASD ficam travados (o ferreiro para), a oficina, a fila e o boost seguem rodando. Foi o caminho mais simples e igual ao modal que já existia. |
| P1-1 | Versão no canto | Só no build de desenvolvimento (`Debug.isDebugBuild`; o playtest usa). No release ela fica só no cartão. |
| P2-4 | Som | `Ajustes.Som` (PlayerPrefs `fs_som`, 1 = ligado). A 1ª linha do `Sfx.Play` volta sem tocar com o som desligado: nenhum SFX do jogo passa por outro caminho. |
| P2-4 | Vibração | `Ajustes.Pulso(ms)`: `Vibrator` do `currentActivity` + `VibrationEffect.createOneShot(ms, DEFAULT_AMPLITUDE)` (API 26+; abaixo, `vibrate(long)`), em `try/catch`; se falhar uma vez, desliga até reabrir. Venda 15 ms (no máximo 4 por segundo: o VIP vende várias unidades no mesmo quadro), compra (placa ou menu) 25 ms, VIP servido 40 ms, "MAX" 15 ms só quando aparece. PlayerPrefs `fs_vibra`, ligada por padrão no Android. No PC a chave aparece apagada (45%) e não responde. |
| P2-4 | Permissão `VIBRATE` | Ligar a Vibração confirma com `Handheld.Vibrate()`, como o Rune Relay. Essa referência basta para a Unity pôr a permissão no manifesto, sem um `AndroidManifest.xml` próprio disputando o merge com o GameActivity e o LevelPlay. |
| — | Diário | Mudar uma chave grava `settings,som|vibra,0|1` (pelo mesmo `SetSom`/`SetVibra` do toque). As preferências valem também no `-testsession` (não são progresso); o diário não (`Log` não grava no `-testsession`). |
| — | Flag de dev | `-settings` abre o cartão no início; `-settings on|off` liga ou desliga as duas antes, gravando como o toque. A linha `SHOT` do log passou a mostrar `som=` e `vibra=`. |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | 81/81 |
| `dotnet build client/tools/viewcheck/view -nologo --artifacts-path %TEMP%\vc6b` | 0 erros, 0 avisos |
| `Unity.exe … -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v06b/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v06b/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=8 ouro=1310` (igual à v0.6a) |
| Android: método temporário `Setup.BuildAndroidTmp` → `%TEMP%\fsv\test.apk` (log `validation_v06b/build_android_tmp.log`, já removido do `Setup.cs`) | `BuildSummary(Android): result=Succeeded errors=0`; compila o caminho `UNITY_ANDROID` do `Ajustes.Pulso` |
| `aapt2 dump permissions %TEMP%\fsv\test.apk` | `uses-permission: name='android.permission.VIBRATE'` (o APK 0.5.1 não tinha); `mainTemplate.gradle` e `AndroidResolverDependencies.xml` não foram alterados pelo resolver |
| Persistência | `-testsession -settings off` → registro `fs_som=0`, `fs_vibra=0`; a execução seguinte só com `-settings` abre com as duas DESLIGADO e loga `som=False`; a seguinte sem flag mostra a HUD normal e loga `som=False`; `-settings on` restaurou `fs_som=1`, `fs_vibra=1` |
| Mudo | caminho de código: `Sfx.Play` → `if (… !Ajustes.Som …) return` antes do `PlayOneShot` |

Fotos em `client/Builds/validation_v06b/shots/` (janela `-screen-fullscreen 0 -testsession`, `-shotdelay 2`; logs em `validation_v06b/logs/`). Todas foram abertas.

| Foto | Argumentos | Conferido |
|---|---|---|
| `01_hud_540.png`, `03_hud_432.png` + `01z_engrenagem_zoom.png` | 540×960 e 432×960 | engrenagem dentro da faixa, sem encostar na dica nem na pílula de ouro; na 432 o botão fica um pouco mais alto que largo e continua lendo |
| `02_hud_dica_longa_540.png`, `04_hud_dica_longa_432.png` | `-gold 300` | "Toque em Melhorias: Fole (50 de ouro)" em 2 linhas inteiras na dica mais estreita |
| `05_config_540.png`, `06_config_432.png` | `-settings` | cartão no centro, Som LIGADO, Vibração apagada (PC), versão, FECHAR; a HUD, o menu e o botão 2× ficam escurecidos por baixo |
| `07_config_off_540.png` | `-settings off` | Som DESLIGADO (cinza, bolinha à esquerda) diferente da Vibração apagada |
| `08_persistiu_432.png` | `-settings` (execução seguinte) | abriu com Som DESLIGADO: gravou |
| `09_hud_som_off_540.png` | sem flag | HUD igual com o som desligado (`som=False` no log) |
| `10_restaurado_432.png` | `-settings on` | volta ao LIGADO; preferências restauradas no registro |
| `11z_record_sem_engrenagem.png` | `-record … -recordsec 0.2` (topo do quadro 1080×1920) | criativo sem engrenagem e sem versão, dica na largura inteira |

Refeito depois de olhar: a 1ª engrenagem (dentes finos de 0,19 do passo) lia como sol a 50 px. Os dentes ficaram largos e afinando na ponta, com corpo maior. No `-record` a dica deixava um buraco à direita e voltou à largura inteira.

## Fica de fora / para o aparelho
- Vibração não foi sentida: o emulador não vibra e o APK do teste de amanhã é o 0.5.1. Conferir no próximo APK de playtest se 15 ms se sente no POCO (alguns motores ignoram pulsos abaixo de ~20 ms) e se 4 por segundo na venda incomoda.
- A linha `settings` do diário não foi gravada de verdade: rodar sem `-testsession` mexeria no save e no diário reais do PC. Ela sai do mesmo `Log` das outras linhas.
- O voltar do Android com o painel do cofre aberto continua saindo do jogo, como antes. Só o cartão de configurações fecha com o voltar.

# v0.6c: encomendas (validação, 2026-10-09)

Escopo: a View das encomendas da FASE9 (`docs/FASE9_ENCOMENDAS.md` §6). O Core veio pronto do coordenador (`f05142f`, merge `3808948` na `feat/v0.6-juice`). Mexi em `Game.cs` e `client/tools/diario_report.py`. `bundleVersion` continua 0.5.1 e o `ForgeStreet-dev.apk` não foi refeito.

## O que mudou
| Pedido | Como ficou |
|---|---|
| Cartão | Pílula de ouro em miniatura (borda `#F2D9A0`, fundo `#2A1E14`) com ícone do item, "Encomenda 3/5", barra fina verde, moeda e "+25". **Canto de baixo à esquerda** (0,02–0,40 da largura, na altura do selo do boost), acima do botão VIP. Sob a pílula de ouro, como pedido, ele cobria o balão do 1º da fila com 8 vagas (foto `validation_v05b/06_fila_cheia_lote4.png`) e as moedas e corações da rua depois do Corredor. Foi o mesmo motivo que tirou o selo do boost do topo. |
| Animação | Entra com o pop do aviso (0,3 → 1,1 → 1 em 0,3 s) quando o Sim tem encomenda (vale para a que volta do save) e pulsa a cada venda que conta. Entregue, mostra o alvo cheio (a última venda e a entrega caem no mesmo tick), cresce 25% e some em 0,35 s. |
| Entregue | Som `upgrade`, aviso "Encomenda entregue! +N" com a moeda (`MakeBanner`, 0,55–0,62 da altura, logo abaixo do "Cliente VIP!", então os dois cabem juntos) e moedas do cartão até o contador pelo `FlyCoins` do baú (o número só sobe quando elas chegam). |
| Diário | `order_new,item,alvo` e `order_done,item,prêmio`. O `diario_report.py` ganhou a seção ENCOMENDAS: recebidas e entregues por testador e minutos de jogo por encomenda (t_jogo / entregues, a conta do bot no `BALANCE.md` §20). |
| `-record` | Sem cartão e sem aviso, como a versão e a engrenagem. O ouro entra direto no número. |
| Flag de dev | `-shotorder`: a foto espera o aviso da 1ª entrega (até 180 s) e sai `-shotdelay` s depois. Com o bot em tempo variável, o momento da entrega muda a cada execução. |

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | 85/85 depois do merge; 86/86 com o teste da revisão (abaixo) |
| `dotnet build client/tools/viewcheck/view -nologo --artifacts-path %TEMP%\vc6c` | 0 erros, 0 avisos |
| `python client/tools/diario_report.py --autoteste` | `autoteste OK`. Prova vermelha: a mesma versão sem contar `order_*` sai com exit 1 no assert da seção ENCOMENDAS |
| `Unity.exe … -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v06c/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v06c/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=9 ouro=1535`. Na v0.6b era 8 / 1310: o prêmio adianta uma compra (o `ouro` é o `GoldEarned`, sem o prêmio) |
| Bot 20 min: `-bot -speed 20 -shotdelay 62` | `SHOT t=1240,1 upgrades=18`, 0 exceções (`logs/06_bot_20min.log`) |
| Diário de verdade | Não gravado: o `-testsession` das fotos não escreve diário. As linhas saem do mesmo `Log` das outras e o relatório foi provado com linhas sintéticas (4 entregues + 1 aberta em 10 min = "1 a cada 2.5 min") |

Fotos em `client/Builds/validation_v06c/shots/` (janela `-screen-fullscreen 0 -testsession`; logs em `validation_v06c/logs/`). Abri todas.

| Foto | Argumentos | Conferido |
|---|---|---|
| `01_encomenda_540.png` + `01z_cartao_zoom.png` | `-bot -speed 10 -shotdelay 9.5` | "Encomenda 3/5" com espada, barra em 60%, "+25". Não encosta no botão VIP nem na versão (build dev); o Depósito fica livre acima |
| `02_encomenda_432.png` | igual, 432×960 | cartão sobre a grama, abaixo do muro; lê igual |
| `03_entregue_540.png` | `-bot -speed 10 -shotorder -shotdelay 0.25` | aviso "Encomenda entregue! +25", cartão em 5/5 crescendo e sumindo, moedas subindo para o contador (ouro 19 antes de elas chegarem) |
| `04_cartao_vip_boost_540.png` | `-bot -boost 1 -stock 5 -speed 2 -shotdelay 26` | cartão "0/5", botão VIP, selo "2× 0:07" e botão "3×" juntos, sem sobrepor (cartão 0,02–0,40, selo 0,69–0,98) |
| `05z_record_sem_cartao.png` | `-record … -recordsec 5 -recordfps 10 -bot -speed 20` (fundo do último quadro, t = 100 s, com a 1ª encomenda ativa) | criativo sem cartão |
| `06_bot_20min.png` | `-bot -speed 20 -shotdelay 62` | fim da bateria de 20 min: "Encomenda 8/30" de ferramenta, "+70", longe da fila |

## Fica de fora
- Som ao chegar encomenda nova: o pop do cartão já chama o olho, e o pedido só falava do som da entrega. Entra se o playtest disser que ninguém viu o cartão.
- Vibração na entrega: o baú não vibra, então a entrega também não.

# Revisão da v0.6 (correções, 2026-10-09)

Achados da revisão de código de `57de892` (v0.6a), `5d992d2` (v0.6b) e `f05142f` (core v0.6c). Todos entraram no mesmo build Windows das fotos da v0.6c. Os portões são os da seção acima: coretests 86/86, viewcheck 0/0, autoplay OK e bot de 20 min sem exceção.

| # | Achado | Correção | Como foi conferido |
|---|---|---|---|
| 1 | Estação pronta com a saída cheia parecia trabalhando (`Busy` com `Progress` 1) | `WorldView.Juice`: `work = Busy && Progress < 1` para faíscas e punch, fumaça, boca acesa e brasa da fornalha. O pulso vermelho da pilha fica sozinho | Código. No Sim de hoje esse estado não aparece em jogo normal (a estação não começa com a saída cheia, e só ela enche a própria saída). Só um save com `Busy=1` e saída cheia chega lá, então a correção é defensiva |
| 2 | Voltar do Android saía do jogo com o painel do cofre ou a fileira de melhorias abertos | `Game.Update`: painel → `ClosePanel()`; configurações → fecha; fileira (`MenuBar.IsOpen`) → fecha; nada aberto → grava e sai. Isso também resolve a pendência da v0.6b ("o voltar com o painel do cofre aberto continua saindo") | Código e viewcheck. O Esc do PC passa pelo mesmo caminho, mas não foi apertado à mão |
| 3 | GC por quadro: `Art.Get`/`Soft` criavam a lambda que captura `inside`/`f` antes de olhar o cache | O cache é conferido antes, num método sem lambda (`Cached`); a lambda foi para `Supersample`/`Gradient`. Corrige todos os chamadores (faísca, fumaça, poeira, coração), sem campo de cache no `WorldView` | Console .NET com o mesmo formato (`GC.GetAllocatedBytesForCurrentThread`, 1000 chamadas): antes 90 B por chamada; "return antes" no mesmo método 24 B; corrigido 0 B |
| 4 | Vibração: `vibrate(VibrationEffect)` montava a assinatura JNI pela classe de runtime (`VibrationEffect$OneShot`), errava a busca e caía na reflexão a cada pulso, com um `AndroidJavaClass` novo por pulso | Sempre `_vib.Call("vibrate", ms)` com `ms` long (assinatura `(J)V` exata). O `Vibrator` fica em cache, e saíram o `SDK_INT` e o `VibrationEffect` | O caminho `UNITY_ANDROID` não compila no viewcheck nem no build Windows. A linha `_vib.Call("vibrate", ms)` já existia no ramo API < 26 compilado pelo APK temporário da v0.6b. Falta sentir no próximo APK de playtest |
| 5 | Encomendas invisíveis na View | v0.6c (seção acima) | idem |
| 6 | `OrderNew`/`OrderDone` saíam sempre no balcão principal | `Sim.Orders`: `CounterFor(item).Pos`, então a encomenda de joia sai na loja de joias. O item é lido antes de `OrderItem = -1` | Teste novo `Encomenda_DeJoia_EventosNaLojaDeJoias`. Falhou antes (`(5, 4.5, 13)` em vez de `(5, 12, 11.5)`) e passa depois; 86/86 |
| 7 | A pop do item da pilha de saída reescrevia a escala de todos os itens a cada quadro | A escala só é escrita durante a pop, inclusive no quadro em que ela acaba (volta a 1) | Código |

Visto de passagem e fora do pedido: `MenuBar.TopPx` cria um `Vector3[4]` por quadro (o `Game.Update` lê a cada quadro).

---

# v0.6 no Android (emulador do PC, 2026-10-09, coordenador)

APK x86_64 da `feat/v0.6-juice` (`Setup.BuildAndroidEmu`) no AVD `fs_playstore` (Android 15, API 35, 1080×2400), sem janela. Flags de dev pelo intent: `adb shell "am start -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity '-bot -speed 4'"`. As aspas simples vão **dentro** das duplas: sem elas o shell do aparelho quebra o extra e o `am` acusa "Unknown option".

| Conferido | Resultado |
|---|---|
| Encomendas no Android | cartão "Encomenda 3/27" com o bot vendendo, 0 exceção no logcat (`validation_v06emu/01`) |
| Vibração (JNI) | `dumpsys vibrator_manager`: pulsos de 15 ms nas vendas e 25 ms na compra, `usage: TOUCH`, do `br.com.vstack.forgestreet`; sem exceção |
| **Botão voltar** | **não chegava ao jogo**: nem o código antigo (salvar e sair) nem a correção da revisão rodavam. O GameActivity da Unity 6 não passa o voltar ao Input System (nenhum evento de teclado, nem "A"; problema conhecido: fórum Unity 1555368 / UUM-136080). Com o Predictive Back desligado, também não |
| Correção | `activeInputHandler` = 2 (Input System + Input Manager legado) no `Setup`/ProjectSettings e `Input.GetKeyDown(KeyCode.Escape)` no `Game.Update`. Voltar fecha a bandeja (`validation_v06emu/03`→`04`) e o cartão de configurações (`05`→`06`); com a tela limpa, sai do jogo. Predictive Back continua ligado (Android 16) |
| Painel do cofre | apareceu na 1ª volta (`validation_v06emu/02`, antes da correção); na repetição o cofre já tinha pago, então o caminho "voltar fecha o painel" foi provado pelo cartão de configurações (mesmo `if`) |

## Armadilhas do build Android (corrigidas no `Setup.Build`)
- **Empacotamento incremental do Gradle:** depois de trocar de branch o APK saiu com ~12 MB de buracos (84,8 MB, as mesmas entradas comprimidas de 72,4 MB); alternando ARM64/x86_64 saiu APK sem `libunity`/`libil2cpp` (crash `libgame.so not found`). Agora todo build Android apaga `Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build` antes.
- **APK do emulador sem bibliotecas:** o `mainTemplate.gradle` versionado é o do aparelho (o resolver do LevelPlay exclui `/lib/x86_64`). Se o resolver não reescrever (estado guardado de um build x86 anterior), o APK x86_64 sai sem as `.so` nativas. Agora o `BuildAndroidEmu` tira essa linha só durante o build e devolve o arquivo.

---

# v0.6d: leitura e retorno visual (revisão de UX, validação, 2026-10-09)

Escopo: os itens 1–12 da revisão de UX (as 3 decisões do dono ficaram de fora, como pedido). Mexi só na View (`Game.cs`, `WorldView.cs`, `MenuBar.cs`, `Art.cs` e o novo `Textos.cs`) e no teste novo `TextosTests`. No Core mudaram **só 2 textos** em `Defs.cs` ("2ª bigorna" → "Bigorna 2", "2ª fornalha" → "Fornalha 2"), para o nome ser o mesmo da estação em todo lugar. Nenhuma regra mudou. `bundleVersion` continua 0.6.0 e nenhum APK foi refeito. Medidas em px na largura 1080 (a janela de 540 mostra a metade).

## O que mudou
| # | Item | Como ficou |
|---|---|---|
| 1 | Dica | 1 linha em negrito, ~32 px. A caixa do texto tem 48 px de altura, então 2 linhas não cabem e o best-fit encolhe até caber em uma. Os textos foram para `View/Textos.cs` (sem UnityEngine), com no máximo 26 caracteres e sem preço: "Compre o Fole em Melhorias", "Pise na placa da Bigorna 2", "Pegue as espadas prontas", "Cliente quer espada!". Nome comprido cai para a forma curta ("Pise na placa: Ferramentas", "Placa: Piso de oficina", "Compre a Mochila"). A dica só é remontada quando muda; antes era uma string nova a cada 0,25 s. |
| 1 | Teste | `TextosTests.Dicas_CabemNumaLinha` monta a dica de cada upgrade (placa e menu), item e baú e falha se alguma passar de 26. Prova vermelha: com "Pegue o minério no depósito" (27) ele sai `Expected: less than or equal to 26`. O `FSCore.Tests.csproj` compila o `Textos.cs` e o `FS.Tests.asmdef` passou a referenciar `FS`. |
| 2 | Balões da fila | O balão do 1º fica preso na tela pelo anel: topo ≥ 24 px abaixo da faixa da HUD e x entre 48 e 1032 (`WorldView.OnScreen`; a base da HUD vem do `Game.Fit`). Os mini-ícones e a paciência aparecem só do 2º ao 4º da fila e só para quem está dentro da tela. |
| 3 | Seta da dica | 0,95 m (~80 px; antes 42 px), contorno de 4 px `#2A1E14` e quique de 12 px a 1,5 Hz. Subiu para 1,5 m acima do alvo: a 1,25 m a ponta caía em cima do nome da placa. |
| 4 | Placas | Placa creme `#E9D8B4` a 90% (antes `#2A2420`, que sumia no chão escuro). Quando dá para pagar: borda verde `#5BD16B` e a placa pulsa de 1 a 1,05. Quando não dá: borda apagada e preço `#FF8A7A`. O ícone de quem não dá para pagar ficou a 75% (com 55% sumia no creme). O preço só é reescrito quando muda; antes era um `int.ToString()` por placa a cada quadro. |
| 5 | Cartão da encomenda | 420 × 104 px, a 32 px da borda da área segura e 24 px acima do botão VIP. Título "Venda 5 espadas" (`Textos.Venda`) numa linha, com best-fit para "Venda 30 ferramentas". A conta "3/5" fica em cima da barra de 20 px. |
| 6 | Versão | Só no build de desenvolvimento, pequena (18 px, contorno de 1,5 px), logo abaixo da pílula de ouro. Fica na folga de 24 px que o balão respeita; embaixo ela batia no cartão e no VIP. |
| 7 | Botões de anúncio | 140 px de largura a 32 px da borda da área segura (antes 2%, ~22 px). A faixa de cima também passou para 32 px. O selo ▶ tem 44 × 32 e fica 8 px para dentro do canto (antes saía pela borda), e o ícone foi para a esquerda dele. Com o boost ativo, o botão mostra "→3×" (o próximo nível). |
| 8 | VIP | O balão mostra a quantidade uma vez só: pílula de 40 px, "×N" `#2A1E14` no `#FFD34D`, na borda de baixo à esquerda (longe do rabicho e da coroa). Saiu o selo "×3" do preço. |
| 9 | Poluição | O "+N" de vendas a menos de 0,4 s do último soma num número só (`WorldView.SaleFloat`). Nasce no máximo 1 a cada 0,4 s e cada um vive 1,1 s, então nunca passam de 3 na tela. As moedas voando viraram filhas da faixa de cima, entre a moeda e a dica: passam por cima da pílula de ouro, onde chegam, e por baixo da dica e de todo o resto da HUD (painéis, avisos, botões). A compra ("Fole!", "Ajudantes ágeis!") usa o aviso do "Encomenda entregue!", com o ícone da melhoria, em vez do "+nome!" verde no mundo. |
| 10 | Configurações | LIGADO: texto `#1F3B12` de 30 px no verde `#5BD16B`. DESLIGADO: branco no `#8C7B6B`. O rótulo fica `#3B2A1A` nos dois. Só a Vibração do PC continua apagada. |
| 11 | Cofre | Cartão creme das configurações com "Bem-vindo de volta!", "+23" de 64 px com a moeda, "Seu cofre guardou isso pra você" em 26 px e PEGAR. Saiu a frase técnica dos 25% e do teto. Flag de dev `-cofre N`: mostra o painel com N de ouro, sem dar o ouro. |
| 12 | Rótulos | Estações e placas com 30 px e contorno de 3 px `#2A1E14`. "Bigorna 2" e "Fornalha 2" são o mesmo nome na placa, no menu, na dica e na estação. "Rua lateral" tem 28 px, cor `#E8D9B8`, e fica dentro de x ≤ 1032. O cartão travado do menu diz "Precisa: Vitrine" / "Precisa: Balcão 5" em 1 linha de 22 px, sem best-fit (antes era "requer Balcão 5 vagas" em 2 linhas). |

Alocação: nenhum texto novo por quadro. A dica, o preço da placa e a pílula do VIP só montam string quando o valor muda. A escala da placa só é escrita enquanto ela pulsa. `OnScreen`/`InView` são só contas de struct.

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | 87/87 (86 + `TextosTests`) |
| `dotnet build client/tools/viewcheck/view -nologo --artifacts-path %TEMP%\vc6d` | 0 erros, 0 avisos |
| `python client/tools/diario_report.py --autoteste` | `autoteste OK` |
| `Unity.exe … -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v06d/build_win.log` | "Build Finished, Result: Success." (compila também o `FS.Tests` com a referência nova) |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10` | `AUTOPLAY OK venda1=23s upgrades=9 ouro=1580`, igual à 0.6.0 (`validation_v06emu/autoplay060b.log`) |
| Bot 20 min: `-bot -speed 20 -shotdelay 62` | `SHOT t=1240,6 upgrades=18`, 0 exceções (`logs/14_bot_20min.log`) |

Fotos em `client/Builds/validation_v06d/shots/` (janela `-screen-fullscreen 0 -screen-width 540|432 -screen-height 960 -testsession`; logs em `validation_v06d/logs/`). `a*` = antes (exe da `2751a66`), o resto = depois. Abri todas as fotos. Os `*z_*` são recortes ampliados.

| Antes | Depois | Argumentos | Conferido |
|---|---|---|---|
| `a01_inicio_540` | `01_inicio_540`, `01z_hud_seta_zoom` | `-shotdelay 2` | dica em 1 linha; seta grande com contorno sobre o Depósito; versão sob a pílula de ouro; botão 2× a 32 px da borda, com o ▶ dentro |
| `a03_placas_540` | `02_placas_540`, `02z_placas_zoom` | `-gold 90` | Bigorna 2 (65) e Ajudante (85) com borda verde e nome amarelo; Escudos, Esteira e Corredor com preço salmão; "Compre o Fole em Melhorias" |
| `a02_dica_longa_432` | `03_dica_432` | `-gold 300` (432×960) | a dica que antes tinha 2 linhas ("Toque em Melhorias: Fole (50 de ouro)") virou 1 linha |
| `a04_fila_cheia_540` | `04_fila_cheia_540`, `05_fila_cheia_432` | `-buyids 3,9,10,25,26,27,28 -warmup 50 -px 4.5,10.5` | 8 na fila: balão do 1º ~13 px (540) abaixo da HUD e mini-ícones só no 2º, 3º e 4º |
| — | `05b_fila_cam7_540` | igual + `-px 8,12 -cam 7` | o 1º da fila fora da tela pela esquerda: o balão fica preso na borda (x ≈ 50 px na largura 1080); o 2º e o 3º estão fora da tela ou na borda (sem mini); o mini do 4º fica atrás do balão preso; do 5º em diante, nenhum |
| `a05_cartao_vip_boost_540` | `06_cartao_vip_boost_540`, `06b_…_432`, `06z_cartao_botoes_zoom` | `-bot -boost 1 -stock 5 -speed 2 -shotdelay 26` | "Venda 5 espadas", "0/5" na barra, 24 px acima do VIP; selo do boost espelhado à direita; botão "→3×" |
| — | `07_vip_pilula_540`, `07z_pilula_vip_zoom` | `-vipnow -shotdelay 4` | VIP como 1º: moldura de ouro e só a pílula "×6" |
| `validation_v06emu/02_android_cofre_painel_antes_da_correcao` | `08_cofre_540` | `-cofre 23` | cartão creme, "Bem-vindo de volta!", moeda + "+23", subtítulo, PEGAR |
| `a07_bandeja_540` | `09_bandeja_540`, `09z_trava_zoom` | `-menu -gold 120` | "Precisa: Vitrine" em 1 linha no Balcão 5 vagas |
| — | `10_compra_aviso_540` | `-gold 200 -bot -shotdelay 1.2` | o bot comprou o Fole: aviso "Fole!" com o ícone do fole, no lugar do "+Fole!" verde no mundo |
| `a06_config_540` | `11_config_on_540`, `12_config_off_540`, `12b_config_on_432` | `-settings on` / `off` / `on` | LIGADO com texto escuro no verde, DESLIGADO branco no `#8C7B6B`, rótulo "Som" escuro nos dois; preferências restauradas (`som=True`) |
| — | `13_moedas_dica_540` | `-warmup 30 -stock 6 -shotdelay 0.35` | moedas da venda chegando por cima da pílula de ouro; "+10" somado |
| — | `14_bot_20min` | `-bot -speed 20 -shotdelay 62` | fim dos 20 min: "Venda 12 escudos 3/12 +60", aviso "Encomenda entregue! +65", dois "+N" na tela, oficina íntegra |

Refeito depois de olhar: a seta a 1,25 m tocava o nome da placa (subiu para 1,5 m); o ícone da placa sem dinheiro a 55% sumia no creme (75%); a versão a 45% sem contorno não lia na pedra (80% + contorno); a pílula do VIP com 54 px e texto de 40 px cobria o ícone, e no canto de baixo à direita brigava com a coroa (40 px, texto de 30 px, borda de baixo à esquerda).

## Fica de fora
- As 3 decisões do dono.
- A tag "×N" do VIP que não é o 1º da fila continua texto, sem pílula: lá ela é o único número.
- O VIP servido ainda mostra o "+N" somado das unidades e o "+N" grande do `VipServed`. O número aparece 2 vezes, mas em 1 só lugar cada, e antes eram N flutuantes.
- Não há foto de uma moeda cruzando a dica. A ordem vem da árvore: `Topo/Moedas` fica antes de `PilulaDica`.
- Com o balão preso 24 px abaixo da HUD, a coroa do VIP (desenhada por cima do balão desde a v0.5c) entra mais no balão. Ela continua lendo.
- O rótulo "Joalheria — fechada" ainda pode sair pela direita com a câmera seguindo o jogador (fora da lista; só a "Rua lateral" foi pedida).

## Sessão longa no Android (emulador, 2026-10-09 10:09–10:19)
APK x86_64 da 0.6.0 (`ForgeStreet-emu-0.6.0.apk`, com juice, configurações, Encomendas e a revisão de UX), bot a 4× por 10 min reais, gravando diário:
- **Memória:** PSS 319 → 336 → 341 → 340 → 342 → 344 MB a cada 2 min: sobe ~10 MB acima da 0.5.1 (322–333 MB; sprites do juice e o pool de efeitos) e estabiliza a partir do 4º minuto, sem vazamento.
- **Erros:** 0 exceção da Unity; nenhum FATAL/ANR do jogo.
- **Diário → `diario_report.py`:** Encomendas 14 recebidas / 13 entregues (1 a cada 3,2 min de jogo), 8 VIPs (8 atendidos), andar sem decisão 13%, portões do GDD ok exceto os 2 que só pessoa mede. A referência do relatório para as Encomendas foi atualizada para o vigente (21 entregues, 1 a cada 2,8 min).