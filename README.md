# Forge Street — v0.6 em desenvolvimento (v0.5.1 lançada e testada no POCO F4)

**Estilo:** idle/tycoon arcade em retrato. Você controla o ferreiro com um joystick.

**O que é:** você é o ferreiro de uma rua de forjas medieval e transforma uma bigorna solitária numa rua inteira de produção, com joalheria.

**Como funciona:** pega **minério** no depósito, derrete na **fornalha**, martela o lingote na **bigorna** e leva a espada ao **balcão**, onde os clientes esperam em fila e pagam. Com o ouro, pisa nas **placas do chão** para construir (2ª bigorna, escudos, ferramentas, esteira, 2ª fornalha, corredor, joalheria) e contratar **ajudantes que automatizam cada etapa**; no **menu de baixo** compra as melhorias do ferreiro e das estações (fole, mochila, botas, martelo veloz, vitrine, balcão maior). A produção é física e legível: cada pilha aparece no chão, a estação com saída cheia pisca vermelho ("travada") e a sem insumo fica apagada ("fome"). A decisão é ler qual etapa é o gargalo agora e investir ali. Clientes VIP pagam 3×, encomendas curtas dão direção à sessão e o cofre rende enquanto você está fora.

**Como vai ser jogar:** você começa sozinho carregando tudo no braço e termina comandando uma rua de forjas que trabalha sozinha. Sessões de 3 a 12 minutos.

- **GDD:** `docs/GDD.md` (§3 primeiros 10 min e §18 MVP são o alvo desta v0.1).
- **Balance medido pelo bot:** `docs/BALANCE.md`.
- **Por que este jogo:** `../00_PESQUISA/VEREDITO_VALIDACAO.md` (slot idle disputado com o Underground Inc.; este greybox existe para o teste de criativos e o playtest Camada 0).

## Estado (2026-10-09, v0.5.1 testada no POCO F4)

| Item | Estado |
|---|---|
| Versão no aparelho | **0.5.1** (Release `v0.5.1`, PR #4 na `main`): testada pelo Vinicius no POCO F4 em 2026-10-09 — **60 fps** (p95 16,6 ms, 0 travada), PSS 325–345 MB, 0 crash, bateria ≤ 41 °C. Parecer: "promissor" (`docs/VALIDACAO_V05.md`, seção "Teste no POCO F4") |
| Próxima versão | **0.6.0** já na `main` (PR #5; falta o teste no POCO para a Release): juice, configurações som/vibração, Encomendas (missões curtas), revisão de UX, voltar do Android |
| Núcleo C# puro (`FS.Core`) | simulação por tick, física, 6 papéis de ajudante, balcão 4→8, VIP, velocidade por anúncio, cofre offline, save; **81 testes** (87 na v0.6) |
| Balance (bot) | produção completa 46:12 (41:31 na v0.6); pessoa real foi 1,7–3,7× mais lenta que o bot no 1º teste (`docs/BALANCE.md`) |
| Anúncios | Unity LevelPlay 9.5.1 integrado (rewarded "Chamar VIP" e "Velocidade"); conta aguardando aprovação, build de teste usa anúncio simulado |
| Próximas decisões | `docs/BACKLOG_V07.md` (9 decisões D1–D9) |

## Como rodar

```bash
client/tools/verify.sh quick   # ou full, android-emulator, release, release-check <arquivo.aab|apk>
```
Ponto de entrada único dos portões (Git Bash). `quick`: núcleo + viewcheck do PC e do Android (compila os `#if UNITY_ANDROID`), sem Unity, ~30 s. `full`: quick + EditMode + `Setup.BuildWindows` + `-autoplay 10`, com veredito do XML/log desta rodada. `android-emulator`: `Setup.BuildAndroidEmu`, liga o AVD `fs_playstore`, instala, abre com `-bot -speed 4`, aperta voltar e início e confere o logcat. Sai ≠ 0 se algo falhar; logs em `client/Builds/verify/`.
`release`: quick + `Setup.BuildAndroidRelease` (AAB sem Development, ARM64, target 36, versionCode = maior×10000 + menor×100 + patch, 0.6.0 = 600) + `release-check`. A chave de upload vem só de `FS_KEYSTORE` (caminho com `/`), `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS` e `FS_KEY_PASS`; sem elas o build falha e nunca assina com a chave de debug. Keystore fora do repositório (`*.keystore`, `*.jks` e `*.p12` estão no `.gitignore`).
`release-check <arquivo.aab|apk>`: só as checagens, sem Unity (targetSdk ≥ 36, versionCode, não-debuggable, só arm64-v8a, LOAD align ≥ 0x4000 em todo `.so`, alinhamento do zip, assinatura sem chave de debug e permissões fora da lista aprovada).
Um processo pesado por vez: aborta se o Unity estiver aberto neste projeto ou se o emulador estiver ligado. Numa worktree, copie `UnityEngine.UI.dll`, `Unity.InputSystem.dll` e `Unity.LevelPlay.dll` de `client/Library/ScriptAssemblies` da pasta principal. Os comandos avulsos abaixo continuam valendo.

```bash
dotnet test client/tools/coretests
```
Núcleo + testes em ~1 s, sem abrir o Unity. `--logger "console;verbosity=detailed"` mostra o relatório do bot minuto a minuto.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics -projectPath "$(pwd -W)/client" -runTests -testPlatform EditMode -testResults "$(pwd -W)/client/Builds/editmode.xml" -logFile "$(pwd -W)/client/Builds/editmode.log"
```
Mesmos testes do núcleo dentro do Unity. O veredito vem do XML, não do código de saída.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "$(pwd -W)/client" -executeMethod FS.EditorTools.Setup.BuildWindows -logFile "$(pwd -W)/client/Builds/build_win.log"
```
Build de Windows (janela retrato 540×960). Para Android, troque o método por `FS.EditorTools.Setup.BuildAndroidDev` (IL2CPP ARM64, API 26+). `Setup.Apply` sozinho só aplica settings e cria a cena vazia.

```bash
client/Builds/win/ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile autoplay.log
```
Smoke no jogo compilado: o bot joga 10 min sem render, loga `AUTOPLAY t=… gold=… upgrades=…` por minuto e termina em `AUTOPLAY OK` (sai 0) se a 1ª venda veio em <90 s e houve upgrade.

Flags de dev do executável:
- `-shot foto.png [-shotdelay 3] [-bot]`: tira uma foto e sai; com `-bot` o bot pilota o jogador enquanto espera.
- `-speed N`: simulação N× mais rápida (até 50).
- `-reset`: apaga o save (`save.txt` e `save.txt.bak` em `persistentDataPath` e o espelho `PlayerPrefs["fs.save"]`; os `save.txt.corrupt-*` da quarentena ficam).
- `-testsession`: estado novo em memória; não lê, apaga ou grava o save nem o diário normal. Use nas fotos de QA, sem `-reset`. O autoplay também isola automaticamente.
- `-shotchest`: com `-shot`, espera um baú real ficar disponível (até 180 s), para fotografá-lo antes de o bot abrir.
- `-menu`: abre a fileira de melhorias (fotos).
- `-buy N` / `-gold G` / `-px x,y`: compra os N primeiros upgrades (produtivos antes do luxo), põe G de ouro, põe o ferreiro em (x, y).
- `-record pasta [-recordsec S] [-recordfps F]`: grava quadros JPG 1080×1920 (janela 540×960, supersample ×2) com o relógio do jogo travado em 1/F s por quadro e sai; liga `-testsession` sozinha. A janela precisa estar visível. Receita: `docs/CRIATIVOS.md`.
- `-buyids 1,5,0@300`: compra esses ids do enum `Upgrade` fora da ordem do `-buy`; `id@t` compra aos t s de jogo, cobrando o preço.
- `-warmup S`: simula S s antes do 1º quadro (jogador parado, sem bot).

Controles: toque e arraste em qualquer lugar entre a faixa de cima e a barra de melhorias (joystick flutuante); no PC também WASD/setas; Esc salva e sai. Pads de construção cobram quando você **para** em cima (ou depois de 0,5 s andando por cima); atravessar correndo não gasta. Melhorias: toque em **Melhorias** na barra de baixo e no cartão (dourado = dá para comprar). Estações e paredes são sólidas: encoste na **boca de entrada** (seta para dentro) para depositar e na **boca de saída** (seta para fora) para recolher.

Diário de playtest: `persistentDataPath/diario.csv` com `utc,sessao,evento,t_jogo,a,b`. Eventos (GDD §14): `session_start, first_sale, product_crafted, product_sold, upgrade_buy, worker_hired, station_unlock, client_left(cansou|fila_cheia), bottleneck, offline_claim` e, por minuto, `queue_length, bottleneck_seconds(travada,fome), walk_no_decision`.

### Playtest (Camada 0)
Colete um `diario.csv` por testador (um arquivo = uma pessoa): Android `adb pull /sdcard/Android/data/br.com.vstack.forgestreet/files/diario.csv playtest/testador01.csv`; Windows `%USERPROFILE%/AppData/LocalLow/V-STACK/Forge Street/diario.csv`.
`python client/tools/diario_report.py playtest/` imprime a 1ª venda, cada `upgrade_buy` humano × bot (BALANCE §15.3) com a razão, `client_left` por motivo, andar sem decisão e travada/fome por minuto, a duração das sessões e o bloco PORTÕES (GDD §3/§15/§26, README).
O que o diário não mede sai como MANUAL: pergunte a cada testador qual estação era o gargalo. Linha truncada é contada e ignorada. `--autoteste` prova o cálculo. Lacunas conhecidas: sem evento de dica, de tutorial nem de fim de sessão (o "escudos sem dica" não sai do diário).

Ao abrir no Editor, a cena `Assets/_FS/Scenes/Main.unity` é vazia de propósito: o `Game` nasce sozinho.

## Arte e animação (decisões do coordenador, 2026-10-06, com base em COE/ARKANA)

- **Rota: sprites pré-renderizados no Blender**, não cena 3D no Unity. Modelos do Tripo (imagem gerada dentro do site → 3D ~8k tris → Auto Rig esqueleto Mixamo) + clips do Mixamo ("Without Skin, 30 fps, In Place") → Blender headless renderiza em folhas (câmera 60°, 4 direções, 12 fps, 128 px) → `Resources/Sprites`. Sim, Game, câmera, HUD e joystick ficam intactos; só a `WorldView` troca formas por sprites (~120 linhas). ≤10 draw calls, ~12 MB de atlas, zero skinning no aparelho. 3D no Unity só se o criativo exigir (e aí com URP + validador do COE).
- **Martelo-pilão mecânico na bigorna** (0 rig, 0 crédito) em vez de NPC martelando: o Sim já processa a bigorna sozinho e combina com a fantasia de automação.
- **Proporção 3 cabeças**, matizes dos itens herdados do `Art.cs` (HUD e playtest não mudam de leitura), estados "travada" (lâmpada vermelha + pilha transborda) e "fome" (dessaturada) visíveis. Docs: `docs/ART_BIBLE.md`, `docs/ASSETS.md` (39 assets, prompts prontos, clips Mixamo), `docs/RENDER_SPRITES.md` (ferramenta `client/tools/render_sprites.py`), lições em `../00_PESQUISA/PIPELINE_ARTE_LICOES.md`.
- **Créditos Tripo** (45/modelo + 20/rig): Lote 1 (criativo + playtest: ferreiro, ajudante, 2 clientes, fornalha, bigorna, balcão, depósito) = 440; Lote 2 (resto do MVP) = 350; Lote 3 (2ª área) = 290. Total 1 080 (20 modelos, 9 rigs, ~18 h de operador). **Nenhum crédito é gasto sem aprovação do Vinicius.** Só conta paga (Tripo grátis é uso não comercial); criar `docs/PROVENIENCIA.md` na 1ª geração (prompt, data, SHA).
- **Lote 1 no jogo (2026-10-06):** 8 folhas em `client/Assets/_FS/Resources/Sprites/<nome>/` (meta.json + PNG por clipe; contact sheets em `Builds/sprites_contact/`). `SpriteSheet.cs` lê o meta; `WorldView` usa sprite quando a folha existe e cai no procedural quando não. Estado → clipe: andando = Walking; ferreiro comprou upgrade = Cheering; ajudante parado = LookingAround; cliente esperando: paciência ≥50% Idle, <50% LookingAround, <20% Angry; atendido = Thankful → Waving → sai. Escala por personagem (`CharScale`) iguala a §4 da ART_BIBLE, já que cada modelo do Tripo sai com altura diferente. Import: `Editor/SpriteImport.cs` (max 4096, sem mips, ASTC 6×6 no Android). Fotos: `client/Builds/shots_art/`.
- **Histórico dos Lotes 2+3 (2026-10-06/07, 640 créditos, saldo 21 370):** clientes mago, elfa, goblin e cavaleiro + bancadas de escudos e ferramentas entram no jogo (`ClientArt` e `StationArt` na `WorldView`). Lote 3 (nobre, bancada e fachada da joalheria, baú, carroça, arco) renderizado em `client/Builds/sprites_area2/`, FORA de `Resources` até a 2ª área existir (não pesa no APK). Escala por personagem medida com o adereço (mochila do goblin 1,30 m, pluma do cavaleiro 1,55 m); mago fica em 1 porque de 60° a aba esconde o corpo. Fotos: `client/Builds/shots_art/05_lote2.png`, `06_lote2_tarde.png`.
- Lições caras herdadas: nunca levar modelo cru do site ao Unity (293k tris/117 MB por peça no COE); uma página nova por peça no Tripo e foto do painel (225 créditos perdidos trocando imagem na página de resultado); clips do placeholder causavam pose T, não o rig; medir performance frio e só no aparelho.

## Histórico v0.2 — fase 1: corredor lateral + joalheria (antes dos baús, 2026-10-07)

- Contrato e decisões: `docs/AREA2_JOALHERIA.md`; números do bot: `docs/BALANCE.md` §9.
- Mundo 15 m de largura (oficina x 0–9, rua lateral x 9–15). Pad **Corredor (690)** abre a rua e a câmera passa a seguir o X do jogador; pad **Joalheria (1 515)** abre a bancada de joias (2 lingotes → anel, 2× o tempo da bigorna) e a loja de joias com fila própria de nobres (cap 3, paciência 40 s, paga 60).
- Bot humano: Corredor 15:18, Joalheria 26:02, ouro/min 516 → 925 (+79%). Tudo comprado aos 28:24 (falta ralo de ouro: próximo passo).
- Arte do Lote 3 em uso: `nobre`, `bancada_joalheria`, `loja_joalheria`, `arco` (entrada/teaser na borda), `carroca` (re-render −45°). `baú` segue fora (milestones não existem).
- Dev: `-buy N` compra os N primeiros upgrades, `-gold G`, `-px x,y` (fotos dos estados da área).
- Fotos: `client/Builds/shots_art/10_area2_fechada.png`, `11_corredor.png`, `12_joalheria.png`.

## v0.2 — fase 2: Joalheiro, Lupa, Vitrine de joias e baús (2026-10-07)

20 upgrades ao todo. Joalheiro: 2.200; Lupa: 5.500; Vitrine de joias: 9.000, fila de 5 nobres e joias de 80 ouro (60 antes da compra). Quatro marcos com recompensa única: 15 espadas, 50 vendas, 10 joias e 200 vendas. Saves de 15/17 upgrades continuam cobertos pelos testes.

Bot humano, 60 min: Joalheiro 25:25, Lupa 31:12, Vitrine 40:41; receita online 46.377, renda final 1.095 ouro/min, 269 joias e 21.212 ouro disponível. São medidas do bot, ainda não calibradas com pessoas. Custos e histórico em `docs/BALANCE.md` §10.7.

Evidências da continuação: `client/Builds/validation_phase2/`. Core 37/37, prova vermelha isolada e restauração verde, EditMode Unity 37/37, viewcheck 0 erros/0 avisos. Builds Windows/Android `Succeeded`, zero erros; autoplay Windows 60 min OK. APK dev 0.2.0: **46.898.468 bytes (46,9 MB)**, gerado em 07/10 às 15:07:54. Fotos em `shots/` e relatório/comandos em `docs/VALIDACAO_FASE2.md`. Android real e métricas humanas continuam sem validação.

## v0.6 — juice, configurações, encomendas e leitura (2026-10-09, branch `feat/v0.6-juice`, APK de teste `ForgeStreet-0.6.0.apk`)
- **Juice:** faíscas e punch na bigorna, fumaça e boca acesa na fornalha, pop do item pronto, "MAX" na pilha, melhorias visíveis (fole, martelo dourado, mochila, poeira das botas) (`docs/VALIDACAO_V06A.md`).
- **Configurações:** engrenagem na HUD com Som e Vibração (pulsos curtos na venda/compra/VIP, provados no Android por `dumpsys`).
- **Encomendas** (missões curtas, `docs/FASE9_ENCOMENDAS.md`): uma por vez, "Venda N de uma linha", N pela renda real, prêmio ~10 s da taxa online fora do `GoldEarned`; bot 60 min: 21 entregues, 5,0% da receita.
- **Leitura (revisão de UX):** dica numa linha, placas que mostram quando dá para comprar, seta-guia maior, balões dentro da tela, cofre "Bem-vindo de volta!", rótulos maiores.
- **Android:** voltar do Android chega ao jogo (Input "Both"; o GameActivity da Unity 6 não passa o voltar ao Input System) e o build Android limpa a saída incremental do Gradle. Experimento B4 (moeda física) medido no bot: `docs/BALANCE.md` §21, branch `exp/moeda-fisica`.
- Próximas escolhas em `docs/BACKLOG_V07.md`.

## v0.5 — feedback do POCO F4 + análise de jogos similares (2026-10-08, em andamento)

Pedidos do Vinicius jogando a v0.4.1 e o que virou (detalhes em `docs/FASE7_CARGA_BALCAO.md`, `docs/FASE8_VIP_VELOCIDADE.md`, `docs/BENCHMARK_*.md`):
- **"Peguei as barras, as máquinas estavam cheias e não consegui pegar os itens da bigorna"** → o ferreiro carrega todos os tipos ao mesmo tempo (até 3 de cada; 6 com a Mochila); ajudantes seguem com um tipo.
- **"Vendas no balcão sem ninguém pedindo"** → era a compra direta com fila cheia (18,7% das vendas aos 10 min): saiu; toda venda tem cliente na vaga.
- **Balcão sem toldo, estande que atende 4 e cresce até 8 (+1 por evolução)** → Balcão 5–8 no menu (150/175/200/225), estande modular com o estoque em pé.
- **"A espada é um triângulo azul"; "o portão lateral está errado"** → ícones renderizados no Blender (espada, escudo, martelo, anel, lingote, minério, moeda), balão grande no 1º da fila, moedas voando ao contador, portões vistos de lado.
- **"Crie mais NPCs e rotacione"** → 10 clientes novos (Tripo, Lote 4, 650 créditos) e sorteio embaralhado de 16.
- **Cliente VIP (~5 min, paga mais) e anúncio para chamar o VIP / velocidade 2×–3× por 1 min** → VIP paga 3× por unidade; velocidade com recarga; anúncios pelo Unity LevelPlay (em integração; anúncio simulado com `-fakeads`).
- Cofre também paga ao voltar de outro app (≥ 60 s fora).
- Teste comparativo com 4 jogos no emulador: protocolo em `docs/TESTE_COMPARATIVO.md`.
- **v0.6a (branch `feat/v0.6-juice`, ainda 0.5.1 no build):** faíscas na martelada, fumaça na fornalha, pop do item pronto, "MAX" na pilha cheia e upgrades visíveis na cena (fole, martelo dourado, mochila, poeira das botas). Validação: `docs/VALIDACAO_V06A.md`.

## v0.4.0 — playtest no aparelho: física, duas bocas, paciência, ambiente e menu (2026-10-07)

Pedidos do Vinicius depois de jogar a v0.3.0 num POCO F4:
- **"O tempo de cada personagem aguardar é muito curto"** → paciência por item = 30 s + 3 × tempo de produção (espada 57 s, escudo 76,5 s, ferramenta 57 s, joia 84 s). Desistências em 10 min: 27 → 1 (`docs/FASE5_FISICA_PACIENCIA.md`).
- **"Precisa existir física; ele atravessa tudo"** → estações, paredes e decoração sólidas; o ferreiro desliza nas quinas; bot e ajudantes contornam.
- **"Cada local de criação com dois acessos"** → entrada (só deposita) à esquerda e saída (só recolhe) à direita de cada estação de produção, marcadas no chão com anel e seta na cor do item.
- **"Chão, paredes, coisas estéticas"** → pedra, assoalho, tijolo, calçamento, tochas e props; parede direita com porta lateral (fechada até o Corredor) e o arco.
- **"Melhorias no menu da barra abaixo"** → 9 melhorias (Fole, Fole duplo, Mochila, Botas, Vitrine, Ajudantes ágeis, Martelo veloz, Lupa, Vitrine de joias) saem dos pads e viram cartões tocáveis na barra inferior (`docs/FASE6_MENU_MELHORIAS.md`). Construções continuam pads.
- Fase 4: **Mineiro 3 000 → Joalheiro 2 2 400** (`docs/FASE4_MINERIO.md`); fome da joalheria 52% → 15%.
- Luxo repreçado para **14 000 / 18 000 / 22 000** (Fachada sai +8,7 min depois da produção completa).

## v0.3.0 — fase 3: luxo visual, destino do ouro (2026-10-07)

- Contrato e evidência: `docs/FASE3_LUXO.md`; números do bot 90 min: `docs/BALANCE.md` §11; validação: `docs/VALIDACAO_FASE3.md` (fotos em `client/Builds/validation_phase3/shots/`).
- Três compras únicas, liberadas só depois dos 20 upgrades produtivos, sem nenhum bônus de produção: **Fachada nobre 11 000** (soleira/postes dourados + varal de bandeirolas na entrada), **Piso de oficina 14 000** (ladrilhos), **Joalheria real 18 000** (tapete roxo com borda dourada + 4 pedestais de gema). Bot: produção completa 40:41 → Fachada 50:45, Piso 63:46, Joalheria real 79:55; ouro/min idêntico (±1%). Voltar com o cofre no teto (18 000) compra só a Fachada.
- Preços medidos: 6 000/10 000/16 000 (proposta inicial) compravam a Fachada 5 min após completar a produção e o teto offline pagava dois luxos de uma vez.
- Logística da joalheria (fome ~52–56%): diagnóstico em `docs/ESTUDO_LOGISTICA.md` — falta lingote NA FONTE (fornalhas 22% sem minério; Ajudante 1 saturado), não transporte; esteira lateral, 2º joalheiro e fornalha lateral perdem no A/B. Próximo A/B planejado: 2º ajudante de minério (não implementado).
- Pendência técnica: o core trata IDs ≥ 20 como luxo (`ProductionCount`); antes de anexar um upgrade produtivo novo, marcar produtivo/luxo por upgrade.

## Decisões (log)
- **2026-10-07 — fase 2:** Joalheiro 2.200 / Lupa 5.500 / Vitrine de joias 9.000. A Vitrine também aumenta o preço da joia de 60 para 80, tanto na fila quanto na venda direta; descrição "Nobres pagam 80 e a fila cresce". Marco de 15 espadas preservado. Medições e históricos em `docs/BALANCE.md` §10.7.
- **2026-10-07 — sessões de QA sem persistência:** `-testsession` protege o save e o diário existentes. Versão do build e da HUD: 0.2.0.
- **2026-10-07 — cliente compra direto da vitrine quando a fila está cheia** (raia core): a fila cheia de clientes de escudo com o jogador segurando espada travava o jogo para sempre aos ~12 min (também acontece com gente). Sem estoque, continua indo embora.
- **2026-10-07 — Corredor 690 / Joalheria 1 515 fora da fórmula por tier** (coordenador): ficam no fim do enum pelo save; com a fórmula (2 560 / 3 325) a área só abria aos 28–33 min. `CheapestLockedCost` passou a pegar o menor travado.

- **2026-10-06 — 2D top-down procedural, não 3D low-poly** (GDD §11). A raia C deu CLI 5/10 ao Forge por depender de arte 3D e ajuste no aparelho; o greybox prova loop e criativo com zero asset. Arte própria vem depois do gate de criativos.
- **5 tipos de estação** (depósito, fornalha, bancada, balcão, pad): escudos e ferramentas são **bancadas com receita** (escudo = 2 lingotes, 25 ouro; ferramenta = 1 lingote, 16), não tipos novos. O enum `Upgrade` está na ordem de compra pretendida; o custo sai só do tier.
- **Pads pagam parado ou após 0,5 s em cima e rearmam só quando o jogador sai de cima.** Motivo medido: na 1ª versão o bot perdeu 100% do ouro atravessando pads nas diagonais (140 no pad da esteira, 490 no do martelo) e comprou a 2ª fornalha (tier 11) por acidente. Também: comprar o Fole não engole o troco no Fole duplo.
- **Balcão atende o primeiro da fila que dá para atender**, não só o da frente. A versão FIFO estrita travava a fila num cliente de escudo e derrubou a renda em 60% depois da 2ª linha (69 clientes perdidos em 10 min).
- **Demanda cresce com "fama" (vendas), não com upgrades** (`0,94^(vendas/10)`, piso 0,4). Sem isso a renda ficava presa em 100 ouro/min e a 2ª bigorna não valia o preço (bigornas com fome 75% do tempo). Bigorna (5 s) mais lenta que fornalha com fole (2,5 s) para a "fila reduzir" na 2ª bigorna, como manda a §3.
- **Pilha homogênea** (um tipo de item por vez na cabeça): legível e simplifica ajudantes (cada um carrega um tipo: minério, lingote, produto).
- **2026-10-06 — câmera fixa com a oficina inteira visível** (coordenador, após as fotos): com 7,6 m visíveis o mundo de 9 m cortava uma coluna de cada lado em qualquer clamp. `VisibleWidth = mundo + 2×margem`; os clamps colapsam no centro. Ler todos os gargalos de uma vez é o que o greybox quer provar. Câmera que segue volta se o mundo crescer (2ª área).
- **2026-10-06 — offline = 25% da taxa × até 2 h, nunca mais que 2× o upgrade mais barato travado** (coordenador; `docs/BALANCE.md` §5). Antes, 8 h fora após 10 min de jogo pagavam 16.337 ouro e compravam tudo.
- **Offline** (raia B §5 + decisão do coordenador): taxa online medida (média móvel de 60 s) × **0,25** × min(Δt, 2 h), e **nunca mais que 2× o preço do upgrade mais barato ainda travado** (tudo comprado: 2× o último), para o cofre não comprar o resto do jogo sozinho; claim idempotente pelo `SavedAt` do save (id igual ou mais antigo = 0); relógio que voltou = 0; não existe gem para dar.
- **Câmera reserva a faixa da HUD** (15% de cima) e dá 1,2 m de folga além do conteúdo: pad/rótulo nunca cortado na borda, balcão/fila/"+10" nunca embaixo do painel. A dica contextual é a 2ª linha do painel de cima, nunca sobre o mundo.
- **Save em texto chave=valor** no PlayerPrefs, sem JsonUtility no Core: lixo vira estado inicial e tudo é re-derivado dos 15 flags (`Recompute`).
- **Sem TextMeshPro**: rótulos do mundo (nome da estação, preço do pad, "+10") são `Text` uGUI na HUD que seguem o ponto do mundo. Fonte embutida.
- **Bot "humano"** (0,7 s de reação quando o alvo muda, 85% de stick) é o portão de balance; o bot ideal é só limite inferior. A relação humano/bot é **HIPÓTESE**: o diário do playtest calibra.
- Métrica "andar sem decisão" = andar de mãos vazias sem nenhum pad pagável (puro deslocamento). Carregar é trabalho.

## Próximos passos

1. **Testar a v0.5.1 no POCO F4 (2026-10-09, `docs/ROTEIRO_TESTE_POCO.md`; parte 2 opcional com a 0.6.0):** carga mista e a trava das máquinas cheias, balcão 4→8 e estande, balão/moedas/ícones legíveis a 1080×2400, VIP e velocidade (anúncio simulado ou LevelPlay de teste), cofre na volta de outro app; FPS e memória com 16 clientes.
   Teste comparativo no emulador com 4 jogos similares (`docs/TESTE_COMPARATIVO.md`) quando o Vinicius entrar na conta Google.
2. **Playtest Camada 0** (10–20 pessoas, teste interno do Play) lendo `diario.csv` com `python client/tools/diario_report.py playtest/` (seção Playtest abaixo): `first_sale` <90 s em ≥90% (GDD §15); `upgrade_buy` × a §3 (calibra o fator humano do bot); `walk_no_decision` (kill criterion "andar entre pilhas"); `client_left`. Hipótese de passagem: ≥70% compram a 2ª linha (escudos) sem dica e ≥50% dizem qual estação era o gargalo.
3. **8 criativos 9:16** (GDD §17). **3 gravados** do build real: #1 bigorna vazia → oficina lotada, #2 fila gigante → fole resolve, #8 erro proposital (`client/Builds/creatives/`, `docs/CRIATIVOS.md`). Faltam 5; #3 "onde investir 100 moedas?" é o próximo.
4. **Decisão do slot idle** pelo playtest e criativos do Forge; Underground continua como alternativa.
5. **Backlog proposto:** A/B do 2º ajudante de minério (`docs/ESTUDO_LOGISTICA.md`); validar com pessoas se o luxo é desejado (bot comprar não prova interesse). Não ampliar conteúdo antes de medir. IAP e publicação exigem autorização específica; anúncios recompensados (LevelPlay) autorizados em 2026-10-08.


## Repositório e licença
- Mudanças: `CHANGELOG.md` e Pull Requests; versões como tags `vX.Y.Z` com APK em Releases; regras em `CONTRIBUTING.md`.
- Fora do repositório: arte-fonte crua do Tripo3D/Mixamo (licença e tamanho; SHA-256 em `docs/PROVENIENCIA.md`), builds e cache do Unity.
- © V-STACK / Vinicius Souza. **Todos os direitos reservados.** Código e arte visíveis para acompanhamento, sem licença de uso, cópia ou redistribuição.
