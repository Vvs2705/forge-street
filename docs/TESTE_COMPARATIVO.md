# Teste comparativo — Forge Street × 4 jogos similares (protocolo)

**Pedido do Vinicius (2026-10-08):** "instalar um emulador, baixar uma versão de jogo similar ao nosso, fazer um teste de jogo e comparar o nosso com o deles". Aval dele para baixar a imagem Android com Google Play e instalar pela Play Store: **Forge & Fortune**, **Burger Please!**, **My Perfect Hotel**, **Pizza Ready!**. Base de mercado e visual: `BENCHMARK_MERCADO.md`, `BENCHMARK_VISUAL.md` (feitos só com loja/vídeo; este teste é jogando).

## Ambiente
- AVD `fs_playstore`: Android 15 (API 35) Google Play, x86_64 com tradução ARM64 (`abilist = x86_64,arm64-v8a`), tela 1080×2400 (igual ao POCO F4), 2 GB de RAM (o PC tem 7,7 GB: rodar SEM Unity/Blender abertos; `Desktop/FORGE_LIBERAR_DEV.bat`).
- Conta Google: o Vinicius entra (o agente não digita senha). Instalação só pela Play Store; nada de APK de site paralelo.
- O nosso: APK dev da `feat/v0.5-carga-balcao-arte` instalado por `adb install -r`. O emulador NÃO mede desempenho real (CPU do PC + tradução ARM): FPS/memória daqui não comparam com o POCO; servem só entre os jogos no mesmo emulador.

## Roteiro por jogo (instalação limpa, 10 min de jogo)
1. Gravar a tela em blocos de 3 min: `adb shell screenrecord --time-limit 180 /sdcard/<jogo>_<n>.mp4` (4 blocos), puxar com `adb pull`.
2. Jogar seguindo o que o jogo pede (tutorial, setas, mão), como jogador novo. Recusar rastreamento/consentimento opcional; **não comprar nada**; não assistir anúncio a menos que seja a única forma de seguir (anotar).
3. Fotos de tela (`screencap`) nos marcos: abertura, 1º dinheiro, 1º upgrade, 1ª automação (funcionário), 1º anúncio oferecido, 10:00.

## O que medir (mesma planilha para os 5)
| Medida | Como |
|---|---|
| Tempo até a 1ª ação útil e até o 1º dinheiro | cronômetro no vídeo |
| Tutorial: forma (seta, mão, texto), passos, quando termina | vídeo |
| Ritmo: tempo até 1º, 3º e 5º upgrade; 1º funcionário/automação | vídeo |
| Regra de carga: 1 tipo ou misto? teto inicial? como evita mãos cheias | jogando |
| Caixa/balcão: atende quantos ao mesmo tempo, como evolui, dinheiro físico? | jogando |
| Feedback de venda/compra: moedas, números, som, vibração, partículas | vídeo + ouvido |
| Leitura: tamanho dos ícones de pedido e da pilha (px a 1080 de largura) | foto ampliada |
| Anúncios: 1º interstitial (minuto), rewarded oferecidos (o quê, onde, recompensa) | vídeo |
| VIP/eventos especiais e boosts (velocidade, dinheiro) | jogando |
| Meta: missões, nível do jogador, áreas novas, coleções | jogando |
| Tamanho instalado e RAM em jogo (`dumpsys meminfo`) | adb |

## Entrega
`docs/COMPARATIVO_JOGOS.md`: tabela lado a lado (5 colunas), 5–8 diferenças que mais pesam contra o Forge Street, cada uma com a correção proposta, esforço e se vira tarefa na próxima leva; quadros lado a lado (abertura, 1º dinheiro, minuto 10) em `client/Builds/comparativo/` (fora do APK). Vídeos dos outros jogos ficam só no PC (não vão para o repositório público: são obra de terceiros).
