# Changelog — Forge Street

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões do Forge Street em [SemVer](https://semver.org/lang/pt-BR/). O histórico anterior ao repositório (2026-10-06 → 2026-10-07) foi reconstruído a partir dos documentos de validação.

## [Unreleased]
### Adicionado
- **Build de release (A-PLAT-05, DM §96–98, §103):** `Setup.BuildAndroidRelease` gera **AAB** sem Development, só ARM64, **targetSdk fixo em 36** e **versionCode = maior·10000 + menor·100 + patch** (0.6.0 = 600, 0.7.0 = 700; o publicado até hoje é 1); assinatura **só por variável de ambiente** (`FS_KEYSTORE`, `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS`, `FS_KEY_PASS`) — sem elas o build falha, nunca cai na chave de debug — e o `finally` devolve o projeto ao estado do Dev. `verify.sh release` (quick + build + checagens) e `verify.sh release-check <aab|apk>`: targetSdk ≥ 36, versionCode esperado, não-debuggable, só arm64, todo `.so` com LOAD align ≥ 0x4000 (16 KB), zipalign, assinatura que não seja de debug, permissões fora da lista aprovada viram aviso. Ferramentas do próprio Unity (bundletool 1.17.2, aapt2, llvm-readelf, jarsigner). `.gitignore` com `*.keystore`/`*.jks`/`*.p12`.
- **`client/tools/verify.sh quick|full|android-emulator` (A-PLAT-04, DM §99–102):** ponto único dos portões. `quick` = núcleo + viewcheck + **viewcheck Android** (`client/tools/viewcheck/android`, compila o caminho `#if UNITY_ANDROID` do `Ads.cs` contra a `Unity.LevelPlay.dll`, que antes só o build Android via); `full` = quick + EditMode + `BuildWindows` + `-autoplay 10`, lendo o resultado desta rodada; `android-emulator` = `BuildAndroidEmu` + instala no AVD + `-bot` + BACK/HOME + logcat sem exceção. Um processo pesado por vez (aborta com Unity aberto neste projeto ou emulador ligado). Logs em `client/Builds/verify/`.
- **Encomendas (v0.6c, FASE9, `BENCHMARK_MERCADO` B6):** uma encomenda por vez (vender N de uma linha aberta, em rodízio; a 1ª é 5 espadas, 45 s depois da 1ª venda), sem prazo nem falha; prêmio de ~10 s da taxa online, fora do `GoldEarned` como o baú (`BALANCE.md` §20). Cartão no canto de baixo à esquerda (ícone, "Encomenda 3/5", barra e prêmio), aviso "Encomenda entregue! +N" e moedas até o contador; diário `order_new`/`order_done` e seção ENCOMENDAS no `diario_report.py` (`docs/FASE9_ENCOMENDAS.md`).
- **Configurações com som e vibração (v0.6b, `BENCHMARK_VISUAL` P2-4/P1-1):** engrenagem na faixa de cima abre um cartão creme com Som e Vibração (gravados no aparelho, `fs_som`/`fs_vibra`), a versão e FECHAR. Vibração curta no Android (venda 15 ms, até 4 por segundo; compra 25 ms; VIP servido 40 ms; "MAX" 15 ms), permissão `VIBRATE` no APK. O número de versão do canto só aparece no build de desenvolvimento. Validação em `docs/VALIDACAO_V06A.md` §v0.6b.
- **Juice da produção e upgrades visíveis (v0.6a, `BENCHMARK_VISUAL` P2-1/P2-2/P1-5):** faíscas e punch na martelada das bigornas e bancadas, fumaça e boca acesa nas fornalhas, pop do item pronto, "MAX" vermelho sobre a pilha do ferreiro no teto, e os upgrades na cena (fole na fornalha, martelo dourado na bancada, mochila nas costas, poeira das botas). Só View; validação em `docs/VALIDACAO_V06A.md`.
- `client/tools/medir_aparelho.sh` (FPS, memória, temperatura, erros e diário pelo cabo); resultado do teste no POCO em `docs/VALIDACAO_V05.md`.
### Corrigido
- **Flags do intent só em build de desenvolvimento:** o `Ads.Arg` lia o extra `unity` do intent também no release, então `-fakeads` dava recompensa com anúncio simulado e `-adtest` abria o teste fora do dev. Agora o release só lê a linha de comando, e o `Game.Arg` passou a usar a mesma função. Recompensa que chega depois da janela de 2 s não paga, como antes, mas agora fica no diário como `ad_reward_late` (antes sumia sem log). Achados da Leva 1 de produção (`docs/PRODUCAO_V1_MAPA_TICKETS.md`, FIX-01/02).
- **Revisão da v0.6:** o voltar do Android fecha o painel do cofre e a fileira de melhorias antes de sair; vibração por `vibrate(long)`, sem reflexão a cada pulso; os sprites de efeito pararam de alocar a cada chamada (90 → 0 B); a encomenda de joia aparece na loja de joias; estação pronta com a saída cheia não solta faísca nem fumaça (`docs/VALIDACAO_V06A.md`, revisão da v0.6).
- Encomenda dimensionada pela renda real da linha (antes pela demanda de clientes: a 2ª pedia 27 espadas no 3º minuto); bot 60 min: 21 entregues, 5,0% da receita (`docs/BALANCE.md` §20).
- Anel de paciência do balão em cache sem alocação por quadro (revisão 3).
- Achados do teste no POCO F4 (2026-10-09): um voltar às vezes valia por dois (fechava a bandeja e saía do jogo) — trava de 0,4 s; reabrir em menos de 1 min mostrava "31 de ouro em 0 min fora" — a abertura segue a regra de ≥ 60 s.
### Alterado
- **Leitura da HUD (v0.6d, revisão de UX):** dica em 1 linha de até 26 caracteres, sem preço ("Compre o Fole em Melhorias", "Pise na placa da Bigorna 2"; teste `TextosTests`); balão do 1º da fila preso na tela (24 px abaixo da HUD) e mini-ícones só do 2º ao 4º; seta da dica maior com contorno; placas creme com borda verde e pulso quando dá para pagar, preço salmão quando não; cartão "Venda 5 espadas" com "3/5" na barra; botões de anúncio a 32 px das bordas com o ▶ dentro e "→3×" com o boost ativo; quantidade do VIP numa pílula só; "+N" de vendas próximas somados; moedas por baixo da dica; compra no aviso da encomenda; chaves e cofre ("Bem-vindo de volta!") no cartão creme; rótulos de 30 px; "Bigorna 2"/"Fornalha 2" como nome único; "Precisa: Balcão 5" no cartão travado. Flag de dev `-cofre N`. Validação em `docs/VALIDACAO_V06A.md` §v0.6d.
- Criativos 9:16 #1, #2 e #8 regravados com a v0.6 (`docs/CRIATIVOS.md`, seção v0.6).
- Experimento B4 (moeda física no balcão) medido no bot e guardado no branch `exp/moeda-fisica`, fora do jogo (`docs/BALANCE.md` §21); backlog e decisões da v0.7 em `docs/BACKLOG_V07.md`.

## [0.5.1] — 2026-10-09
### Adicionado
- **Carga sem bloqueio:** o ferreiro carrega todos os tipos de item ao mesmo tempo, até 3 de cada (6 com a Mochila); ajudantes continuam com um tipo só. Corrige a trava "lingotes na mão + bigornas cheias = não pega espada" (`docs/FASE7_CARGA_BALCAO.md`).
- **Balcão evolutivo 4 → 8 vagas:** 4 evoluções no menu (150/175/200/225; a 1ª exige a Vitrine), estande sem toldo que cresce com as vagas e mostra o estoque em pé nos encaixes.
- **Arte v0.5 (Blender, 0 crédito):** ícones de minério, lingote, espada, escudo, martelo, anel e moeda; estande modular; portão e porta de serviço vistos de lado com pilares (`docs/ASSETS.md` §7).
- **Leitura de venda:** balão grande só no 1º da fila com anel de paciência; mini-ícones nos demais; moedas voando até o contador; quem chega com a fila cheia aparece indo embora.
- **16 clientes:** 10 novos do Lote 4 (Tripo, 650 créditos) e sorteio embaralhado sem repetir em seguida.
- O save guarda a carga da mão do ferreiro e dos ajudantes.
- **Cliente VIP:** ~4–6 min depois da 1ª venda chega um cliente com coroa que paga 3× por unidade (paciência 90 s + 6 s/unidade); aviso, "×3" e pacote "×N" no balão (`docs/FASE8_VIP_VELOCIDADE.md`).
- **Anúncios recompensados (Unity LevelPlay 9.5.1):** "Chamar VIP" (VIP extra com pacote 2×, até 20) e "Velocidade" (2× por 60 s; o 2º anúncio sobe para 3×; recarga de 5 min). Sem fill, o build de teste mostra um anúncio simulado de 5 s (`-fakeads`); diário registra `ad_*`, `vip_*` e `boost_start` (`docs/LEVELPLAY.md`).
- `Setup.BuildAndroidEmu`: APK x86_64 para o emulador do PC (o ARM64 cai na tradução do Android 15); validação em `docs/VALIDACAO_V05.md`.
- `docs/ROTEIRO_TESTE_POCO.md` (teste da v0.5 no aparelho) e `docs/TESTE_COMPARATIVO.md` (protocolo do teste com 4 jogos no emulador).
- `docs/BENCHMARK_MERCADO.md` e `docs/BENCHMARK_VISUAL.md`: análise de 10 jogos similares.
### Corrigido
- **Botão voltar do Android:** não chegava ao jogo (o GameActivity da Unity 6 não passa o voltar ao Input System; testado no emulador). Input "Both" + `Input.GetKeyDown(KeyCode.Escape)`: fecha o painel do cofre e a bandeja de melhorias e, com a tela limpa, sai do jogo. APK reserva só com o Input System para o caso de o "Both" pesar no aparelho (`docs/ROTEIRO_TESTE_POCO.md`, plano B).
- **Build Android:** `Setup.Build` apaga a saída incremental do Gradle antes de cada build (APK com ~12 MB de buracos ou sem `libunity` ao alternar ARM64/x86_64) e o `BuildAndroidEmu` tira o exclude de `/lib/x86_64` do `mainTemplate.gradle` só durante o build.
- `diario_report.py` separa "falhas ao mostrar" de "sem anúncio carregado" (antes somava e mostrava falhas sem nenhum pedido).
- **Ajudante 3 parado no balcão (0.5.1, revisão de código):** pegava espada com a prateleira de espada cheia e esperava no balcão enquanto escudo e ferramenta encalhavam. Agora só busca o que cabe. Jogador parado 15 min: 80 → 209 vendas; produção completa do bot 47:03 → 46:12 (`docs/BALANCE.md` §19).
- Save: a carga dos ajudantes contratados fora da ordem (Joalheiro antes do Ajudante 3) sumia ao reabrir; cada entrada leva o papel (`wk=papel:contagens`, o save da 0.5.0 ainda abre).
- VIP que estava na vaga ao fechar: reabrir não repete o aviso "Cliente VIP!" nem o `vip_arrived` no diário, e guarda o ouro que ele já pagou.
- VIP que esperava ao lado da fila cheia sumia e outro boneco caía do alto ao abrir a vaga; agora ele entra andando de onde estava.
- Voltar de um anúncio real não abre mais o "Seu cofre rendeu"; o jogo para enquanto o anúncio é pedido (o boost não acaba no segundo antes de o anúncio cobrir a tela), com teto de 15 s para um SDK que não responde.
- **Vendas fantasma:** a "compra direta" com fila cheia vendia sem cliente visível (18,7% das vendas aos 10 min, 28,5% aos 45); saiu. Toda venda tem cliente na vaga.
- **Cofre ao voltar de outro app:** antes só a abertura pagava; agora a volta de pausa ≥ 60 s também paga.
- `Steer` preso entre a Loja de joias e um pedestal; teto do cofre derrubado por upgrade ainda não à venda.
### Alterado
- Vitrine: só estoque 5 → 10 e clientes ×0,7 (as vagas passaram para as evoluções do balcão). Produção completa do bot 43:37 → 48:24 (sem a receita invisível).
- Criativos de UA 9:16 #1, #2 e #8 (GDD §17) gravados do build real, em `client/Builds/creatives/`, com os comandos em `docs/CRIATIVOS.md`.
- Flags de dev `-record` (quadros 1080×1920 com relógio travado), `-buyids` e `-warmup`.
- `client/tools/diario_report.py`: relatório do playtest Camada 0 (1ª venda, upgrades humano × bot, clientes perdidos, andar sem decisão, travada/fome por minuto, anúncios/VIP/velocidade, portões do GDD com o opt-in de rewarded ≥ 45%) com `--autoteste`.
### Corrigido
- "Fome" visível: estação parada escurece para cinza frio 0,55 (antes 0,78, quase igual à ativa), como pede a ART_BIBLE §6.

## [0.4.1] — 2026-10-07
### Alterado
- Arte das estações 18% maior (célula 1,65 → 1,95 m): agora cobre o corpo sólido de 1,3 m, sem borda invisível; sombra e rótulos acompanham.
- Pad do Mineiro de (0,5; 3,5) para (2,8; 0,6), embaixo do Depósito: saiu da rota Depósito → Fornalha. Balance inalterado (produção completa 43:37).
- Rótulo "Rua lateral" na parede entre a porta e o arco, não mais em cima da porta.

## [0.4.0] — 2026-10-07
### Adicionado
- **Física** (Leva 10): estações, parede e decoração sólidas (caixa 0,65 × 0,40 m); personagem círculo de 0,25 m que desliza nas quinas; bot e ajudantes contornam as estações.
- **Duas bocas por estação de produção**: entrada só deposita, saída só recolhe; marcadas no chão com anel e seta na cor do item. Depósito e balcões com zona única.
- **Parede direita sólida** com porta lateral (y 5,6–7,4, fechada até o Corredor) e o arco (y 11,6–13,4).
- **Mineiro** (3 000) → **Joalheiro 2** (2 400): mais um ajudante de minério e mais um na joalheria (`docs/FASE4_MINERIO.md`).
- **Ambientação** (Leva 11): chão de pedra, assoalho na frente do balcão, paredes de tijolo, calçamento, tochas e 8 props de cenário.
### Alterado
- **Paciência dos clientes** proporcional ao tempo de produção: 30 s + 3 × produção (espada 57 s, escudo 76,5 s, ferramenta 57 s, joia 84 s). Desistências em 10 min: 27 → 1.
- Pads do Martelo veloz (8,4; 3,4) e da Lupa (14,2; 6,8) fora das linhas de caminhada.
- Produção completa (bot) 40:41 → 43:37; fome da joalheria 52% → 15% (`docs/BALANCE.md` §13–§15).
- **Menu de melhorias** (Leva 12): 9 melhorias do ferreiro e das estações saem dos pads e viram cartões na barra inferior, comprados por toque; botão dourado com a contagem do que dá para comprar; joystick não começa na barra (`docs/FASE6_MENU_MELHORIAS.md`).
- Pads de construção redesenhados como canteiro de obra (anel tracejado e "+").
- Luxo 11 000 / 14 000 / 18 000 → **14 000 / 18 000 / 22 000**.
- Fornalha, Bigorna e Joalheria com entrada à esquerda e saída à direita, como as outras; pad da Esteira para (0,5; 11,2).

## [0.3.0] — 2026-10-07
### Adicionado
- Fase 3 "luxo visual" (destino do ouro): Fachada nobre 11 000, Piso de oficina 14 000, Joalheria real 18 000 — só após os 20 upgrades produtivos, sem bônus de produção (`docs/FASE3_LUXO.md`, `docs/VALIDACAO_FASE3.md`).
- Marca produtivo/luxo por upgrade (`UpgradeDef.Luxury`).
- Validação em aparelho real: POCO F4, 60 fps estáveis, 217 MB PSS (`docs/VALIDACAO_APARELHO.md`).
### Alterado
- Rótulo do balcão abaixo do varal da fachada.

## [0.2.0] — 2026-10-07
### Adicionado
- 2ª área: corredor lateral (690) + joalheria (1 515) com loja de joias e fila de nobres (`docs/AREA2_JOALHERIA.md`).
- Fase 2: Joalheiro, Lupa (5 500), Vitrine de joias (9 000; joia passa de 60 para 80), baús de marco (`docs/AREA2_FASE2.md`, `docs/VALIDACAO_FASE2.md`).
- Arte Lotes 2 e 3 (Tripo3D + Mixamo + Blender → sprites): mago, elfa, goblin, cavaleiro, nobre, bancadas, joalheria, baú, carroça, arco.
### Corrigido
- Trava da fila cheia (cliente compra direto da vitrine quando a fila está cheia).

## [0.1.0] — 2026-10-06
### Adicionado
- Greybox jogável: núcleo C# puro determinístico com bot de balanceamento, 15 upgrades, ajudantes, esteira, offline, save; câmera fixa; APK.
- Arte Lote 1: ferreiro, ajudante, anão, guerreira, fornalha, bigorna, balcão, depósito (sprites pré-renderizados).

