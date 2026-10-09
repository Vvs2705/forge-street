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
