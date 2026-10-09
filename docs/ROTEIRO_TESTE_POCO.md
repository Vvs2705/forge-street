# Roteiro do teste da v0.5 no POCO F4 (2026-10-09)

APK: `client/Builds/android/ForgeStreet-dev.apk` (**0.5.1**, ARM64, build de desenvolvimento, 72,5 MB, SHA-256 `3ed4260ce9a3924b6dbd47c87f1e2eac7ad7a64d74f1e186b1abcb1187ebe4ac`; a versão aparece no canto de baixo à esquerda). O save da 0.4.x abre na 0.5.

A 0.5.1 corrige o que a revisão de código achou na 0.5.0: o Ajudante 3 ficava parado no balcão com espada quando a prateleira de espada estava cheia (escudo e ferramenta encalhavam); a carga dos ajudantes sumia ao reabrir se o Joalheiro foi contratado antes do Ajudante 3; o VIP repetia o aviso a cada abertura; o VIP que esperava ao lado da fila cheia "pulava" para outro boneco; a volta de um anúncio real abria o "Seu cofre rendeu"; e o botão voltar do Android saía do jogo com o painel do cofre aberto (agora fecha o painel, depois a bandeja de melhorias, e só sai com a tela limpa).

## 1. Instalar (cabo USB, "Instalar via USB" ligado nas Opções do desenvolvedor)
```
adb install -r client/Builds/android/ForgeStreet-dev.apk
```

**Plano B (se o joystick falhar ou o FPS cair):** a 0.5.1 lê o voltar pelo Input Manager legado junto com o Input System ("Both"), que a Unity avisa não ser suportado no Android. No emulador o toque, o arrasto do joystick e o voltar funcionaram, mas o FPS só se mede no POCO. Se o joystick travar, não responder ou o jogo ficar abaixo de ~55 fps, instale o reserva, que é a mesma 0.5.1 só com o Input System, como as versões já medidas a 60 fps (aí o voltar não faz nada):
```
adb install -r client/Builds/android/ForgeStreet-0.5.1-reserva.apk
```
(SHA-256 `55b251b075f23c567a71a6fc9fcdf980ce990b11a8210608624efa893de443ee`.) Anote qual dos dois ficou: decide se a correção do voltar fica assim ou muda para o ponto de entrada Activity.

## 2. O que conferir jogando (10–15 min)
| # | Pedido seu | O que olhar |
|---|---|---|
| 1 | Trava das máquinas cheias | Pegue lingotes até encher a mão com as bigornas cheias e tente pegar as espadas da bigorna: tem que pegar (até 3 de cada tipo; 6 com a Mochila) e vender |
| 2 | Vendas sem ninguém pedindo | Toda moeda tem que sair de um cliente na vaga do balcão (não pode aparecer "+10" numa vaga vazia) |
| 3 | Balcão sem toldo, 4 → 8 | Estande de madeira com as espadas em pé; Melhorias → "Balcão 5 vagas" (exige a Vitrine) → o estande cresce e a fila anda para as vagas novas |
| 4 | Espada que parece espada | Balão grande só no 1º da fila; ícones da pilha na cabeça e das estações |
| 5 | Portão lateral | Fechado antes do Corredor; abre (folhas para a rua) depois |
| 6 | Mais clientes e rotação | 16 clientes diferentes aparecendo sem repetir em seguida |
| 7 | Cliente VIP | ~5 min depois da 1ª venda: aviso "Cliente VIP!", coroa, "×3" e o pacote "×N" caindo; paga 3× |
| 8 | Anúncio → VIP / velocidade | Botões nos cantos da barra de baixo. Sem a conta aprovada, o build de teste mostra um **anúncio simulado de 5 s** e dá a recompensa; velocidade 2× (2º anúncio = 3×) por 1 min, depois recarga de 5 min |
| 9 | Cofre | Saia do jogo por mais de 1 min e volte: "Seu cofre rendeu" |
| 10 | Visual geral | HUD em pílula, cartões claros com preço verde/cinza, placas de obra com ícone, luz, coração na venda, rosto bravo quando a paciência acaba |
| 12 | Botão voltar (0.5.1) | Com a bandeja de Melhorias ou o "Seu cofre rendeu" aberto, o voltar fecha só o que está aberto; com a tela limpa, sai do jogo. Antes ele não fazia nada no Android |
| 11 | Ajudantes sozinhos (0.5.1) | Com o Ajudante 3 contratado, solte o celular 2–3 min: ele não pode ficar parado no balcão segurando espada enquanto escudos/ferramentas acumulam na bancada |

## 2b. Experimento: câmera mais perto (5 min, depois do roteiro acima)
O benchmark visual (P2-3) aponta a câmera longe como causa de fundo do "cru": hoje a tela mostra 11,4 m de largura e o ferreiro tem ~50 px. O build de teste aceita `-cam W` (largura em metros) pelo cabo; abaixo de 11,4 a câmera segue o ferreiro. Mesmo save, só muda o enquadramento:
```
adb shell am force-stop br.com.vstack.forgestreet
adb shell "am start -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity '-cam 9'"
```
As aspas simples vão dentro das duplas (sem isso o shell do celular quebra o extra e o `am` acusa "Unknown option"). Jogar 2–3 min com 9 e com 8 e dizer qual prefere (personagens maiores × ver a oficina toda; o balão do 1º da fila pode encostar na borda e, com o ferreiro lá embaixo na forja, a fila fica sob a barra de cima). Testado no emulador: `client/Builds/validation_v051/shots/04_android_cam9_bandeja.png`. Abrir pelo ícone volta ao normal (11,4). Fotos do PC: `client/Builds/validation_v051/shots/02_cam9.png` e `02_cam8.png`.

## 2c. Parte 2 (opcional): v0.6.0 por cima
Depois do roteiro da 0.5.1, dá para ver a próxima versão no mesmo save: `adb install -r client/Builds/android/ForgeStreet-0.6.0.apk` (SHA-256 `088761b4…3957`; branch `feat/v0.6-juice`, ainda sem PR). O que olhar:
| # | O que | Onde |
|---|---|---|
| a | Faíscas na bigorna, fumaça e fole na fornalha, martelo dourado e mochila quando compra as melhorias | estações e ferreiro |
| b | "MAX" quando a mão está cheia daquele tipo | em cima da pilha |
| c | Engrenagem no canto de cima: Som e Vibração liga/desliga (vibração curta na venda e na compra) | barra de cima |
| d | **Encomendas**: cartão no canto de baixo à esquerda ("Encomenda 3/8 +25"); entregue = banner e moedas | ~45 s depois da 1ª venda |
Para voltar à 0.5.1: `adb install -r -d client/Builds/android/ForgeStreet-dev.apk` (o save da 0.6 abre na 0.5.1; a encomenda em andamento é ignorada).

## 3. Medir (o coordenador faz pelo cabo)
FPS (SurfaceFlinger), memória (`dumpsys meminfo br.com.vstack.forgestreet`), temperatura e erros no logcat. Comparar com a v0.4.1 (60 fps, 252 MB).

## 4. Puxar o diário e ler
```
adb pull /sdcard/Android/data/br.com.vstack.forgestreet/files/diario.csv playtest/poco01.csv
python client/tools/diario_report.py playtest/
```
(No Git Bash: `MSYS_NO_PATHCONV=1 adb pull ...`.) O relatório mostra 1ª venda, upgrades × bot, clientes perdidos, anúncios/VIP/velocidade e os portões do GDD.

## 5. LevelPlay de verdade (opcional, do seu lado)
1. Responder os 2 e-mails da ironSource (aprovação da conta).
2. Cadastrar o POCO como test device: o ID de publicidade aparece no logcat do build de teste (`ValidateIntegration`) ou em Configurações → Google → Anúncios; no painel: Unity LevelPlay → Settings → Test devices → Add test device. Detalhes em `docs/LEVELPLAY.md`.
Enquanto isso não acontece, o build de teste usa o anúncio simulado — dá para testar o fluxo inteiro.
