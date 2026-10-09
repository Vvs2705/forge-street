# Roteiro do teste da v0.5 no POCO F4 (2026-10-09)

APK: `client/Builds/android/ForgeStreet-dev.apk` (**0.5.1**, ARM64, build de desenvolvimento, 72,5 MB, SHA-256 `77fc31100db98c583eb752f8efb215ffcc8f7f6615ecbe846451fcc0907f4147`; a versão aparece no canto de baixo à esquerda). O save da 0.4.x abre na 0.5.

A 0.5.1 corrige o que a revisão de código achou na 0.5.0: o Ajudante 3 ficava parado no balcão com espada quando a prateleira de espada estava cheia (escudo e ferramenta encalhavam); a carga dos ajudantes sumia ao reabrir se o Joalheiro foi contratado antes do Ajudante 3; o VIP repetia o aviso a cada abertura; o VIP que esperava ao lado da fila cheia "pulava" para outro boneco; e a volta de um anúncio real abria o "Seu cofre rendeu".

## 1. Instalar (cabo USB, "Instalar via USB" ligado nas Opções do desenvolvedor)
```
adb install -r client/Builds/android/ForgeStreet-dev.apk
```

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
| 11 | Ajudantes sozinhos (0.5.1) | Com o Ajudante 3 contratado, solte o celular 2–3 min: ele não pode ficar parado no balcão segurando espada enquanto escudos/ferramentas acumulam na bancada |

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
