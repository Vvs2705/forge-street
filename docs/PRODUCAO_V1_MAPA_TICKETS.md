# Produção v1 — mapa KEEP / UPGRADE / REPLACE / NEW e tickets por Wave

2026-10-09 · Leva 1 do ORQUESTRADOR (4 raias em paralelo, só leitura): núcleo/economia, visual/UI/áudio, plataforma/save/release, produto/UA. Direção: [`DOCUMENTO_MESTRE_PRODUCAO_V1.md`](DOCUMENTO_MESTRE_PRODUCAO_V1.md) ("DM §n"). Estado e decisões: [`PROJETO.md`](PROJETO.md).

Ticket: `ID · título — aceite verificável · esforço P/M/G · depende de · DECISÃO`. `DECISÃO` = precisa de aval do Vinicius (crédito, download, conta, verba, fornecedor, guardrail, direção). Os IDs das decisões P1–P13 estão no `PROJETO.md` §5.

## 1. Validação do Documento Mestre contra o código

| Afirmação do DM | Veredito | Evidência |
|---|---|---|
| `FS.Core` puro, separado da View | **Confere** | `Core/FS.Core.asmdef` (`noEngineReferences: true`, sem referências); testes compilam o núcleo fora do Unity |
| Simulação determinística | **Parcial** | Nenhum sorteio (`Sim.cs:86-87`, VIP por Weyl `:241`, encomendas em rodízio `:941`, teste `Determinismo_MesmaEntradaMesmoResultado`); mas `Tick(float dt)` recebe o `deltaTime` variável (`Game.cs:216-219`) |
| Bot | **Parcial** | 2 perfis (`Bot.cs:18,22`); não usa VIP nem Velocidade, logo não há F2P × pagante |
| Testes | **Confere** | 87/87 em 17 s |
| Mutation testing | **Não confere como ferramenta** | Só "prova vermelha" manual em cópias (`BALANCE.md:315,865,1048,1211`); script fora do repo |
| Data-driven | **Não confere** | `enum Item` com 6 valores e `new int[6]` (`Defs.cs:6`, `Sim.cs:37`), `switch` de custos (`Defs.cs:275-289`), estações no construtor (`Sim.cs:153-163`), cadeia de `if` em `Recompute` (`Sim.cs:718-751`); `const` impede tunar em execução |
| Separação simulação/apresentação (View) | **Confere no núcleo / parcial no §127** | `WorldView.cs` (~1.700 linhas) e `Game.cs` (~1.080) misturam fluxo, HUD, anúncios e diário |
| Pipeline Blender → folha → Unity | **Confere, com limites** | Workbench `flat` ignora emissão; 1 PNG por clipe, sem SpriteAtlas; estações com 1 quadro; ferreiro só Idle/Walking/Cheering; FBX de origem fora do git |
| Proveniência | **Confere, com pendências** | Falta comprovante do plano Tripo (`PROVENIENCIA.md:42`), releitura dos termos Mixamo (`:75`), SHA dos PNG do Lote 4 |
| Telemetria | **Parcial** | Diário local; sem envio remoto nem a taxonomia do §86 |

## 2. Mapa

### 2.1 Núcleo e economia

| Feature | Onde | Classe | Motivo | DM |
|---|---|---|---|---|
| Tick puro, física, eventos `Ev` | `Sim.cs:291-320`, `Defs.cs:58` | KEEP | Fronteira limpa; `Ev` alimenta missões e analytics | §0, §127 |
| Determinismo sem sorteio | `Sim.cs:241,941` | KEEP | Base da qualidade sem RNG e do piso ECA Digital | §15, §106 |
| Números `const` em `Balance` | `Defs.cs:98-210` | UPGRADE | `Tuning` de instância para RemoteConfig/eventos/varreduras | §88, §126 |
| `enum Item` fixo, 1 insumo por receita | `Defs.cs:6`, `Sim.cs:17,197` | REPLACE | `ItemDef`/`RecipeDef` multi-insumo (tiers 1–5) | §8, §14 |
| `enum Upgrade` (ID salvo = ordem), custo `50×1,3^tier` | `Defs.cs:15-51,271-292` | REPLACE | 120–160 upgrades; estoura `int` no tier 67 | §7 |
| Estações/pads no construtor | `Sim.cs:138,153-189` | REPLACE | `DistrictDef` | §8, §145 |
| `Recompute` com `if` | `Sim.cs:716-752` | UPGRADE | Efeitos declarados no `UpgradeDef` | §126 |
| Contorno de obstáculos (2 aberturas) | `Sim.cs:341,370,429-447` | UPGRADE | Grafo de waypoints por distrito | §145 |
| Ajudantes (4 papéis) | `Sim.cs:32-47` | UPGRADE | `WorkerDef` com nível e especialidade | §22–23 |
| Clientes/paciência/fila de joias | `Sim.cs:799-917` | UPGRADE | `ClientDef` por família | §20 |
| VIP | `Sim.cs:236-282` | KEEP | DM manda preservar | §21 |
| Velocidade por anúncio | `Sim.cs:252-263` | KEEP + correção | Recarga não vai no save | §133 |
| Encomendas (1 tipo, 1 vaga) | `Sim.cs:926-950` | UPGRADE | Base do Common das Orders 2.0 | §18 |
| Baús de marco | `Sim.cs:859-882` | UPGRADE | Embrião de Achievements | §32 |
| Luxo / `ProductionComplete` | `Sim.cs:126-133` | UPGRADE | Critério do Royal Charter | §11 |
| Cofre offline (25%, 2 h, `claimId`) | `Sim.cs:1028-1059` | KEEP regra / UPGRADE | Estender a distritos/managers | §27 |
| Save `chave=valor` | `Sim.cs:1063-1209` | UPGRADE | `v=1` nunca lido; sem `contentVersion`/checksum/migração | §91, §131 |
| Ouro `int` | `Sim.cs:92` | UPGRADE | `long` | §12 |
| Bot / BalanceTests / prova vermelha | `Bot.cs`, `BalanceTests.cs` | UPGRADE | Bot 2.0, econsim e mutantes no repo | §105–107 |
| Quality, worker progression, Orders 2.0, Blueprints, Reputation, Masterworks, Collection, Royal Contracts/Charter, distritos 2–6, managers, Daily/Weekly/Achievements/Eventos/Pass, content validator | — | **NEW** | — | §11–35, §108 |

Distritos: o modelo atual não suporta uma 2ª área sem código novo (a rua lateral foi feita com `if`), mais de 6 itens, receitas multi-insumo, custos por distrito nem teto offline por distrito. Proposta: uma instância de `Sim` por distrito montada de um `DistrictDef`, mais uma camada "Império" com ouro e reputação compartilhados; distritos inativos rendem por fórmula.

### 2.2 Visual, UI e áudio

| Elemento | Onde | Classe | Motivo | DM |
|---|---|---|---|---|
| Player | `Sprites/ferreiro`, `WorldView.cs:790` | UPGRADE | Faltam Carry/Interact e escala (~30 px em 540) | §25, §58–59 |
| Ajudantes | `Sprites/ajudante` | REPLACE | Um modelo tingido; DM pede papéis distintos | §22, §59 |
| Clientes | 17 folhas | UPGRADE | Mapear para famílias; faltam farmer/royal/collector | §20, §57 |
| VIP | coroa em `WorldView.cs:281` | REPLACE | Cliente + chapéu; sem entrada especial | §21 |
| Fornalha | `Sprites/fornalha/Static.png` | REPLACE | 1 quadro; DM pede 6 estados | §54 |
| Bigorna | `Sprites/bigorna` | UPGRADE | Martelo-pilão e ritmo | §55 |
| Esteira | `WorldView.cs:766` | UPGRADE | Pouco movimento, sem som | §56 |
| Balcão, bancadas, depósito | `Sprites/balcao*`, `bancada_*` | KEEP + re-render | Leitura ok | §75 |
| Cenário | `Resources/Textures`, `props_blender.py` | UPGRADE | Sala única e plana; ~60% de piso vazio no início | §51, §67 |
| HUD | `Game.cs:405-432` | UPGRADE | Duas paletas (azul-marinho + creme) | §63–64 |
| Cartões de melhoria | `MenuBar.cs` | UPGRADE | Falta benefício em % e estados | §65 |
| Popups/avisos | `Game.cs:445,704` | REPLACE | Cobrem a oficina | §68, §73 |
| Ícones | `melhoria_*`, `item_*` | UPGRADE | Primitivas | §63 |
| Formas procedurais (balão, anel, placas) | `Art.cs:33-168` | REPLACE até o release | Placeholder | §178 |
| Fonte | `Art.cs:378` (Arial) | REPLACE | — | §63, §179 |
| VFX | `WorldView.cs:1284-1599` | KEEP pool / UPGRADE biblioteca | Pool e teto já existem | §61 |
| Áudio e música | `Art.cs:435-487` (11 senoides) | REPLACE | Sem música, ambiente nem mixer | §70–71 |
| Haptics | `Art.cs:494`, `Game.cs:262` | UPGRADE | Vibra em toda venda; `vibrate(long)` sem amplitude | §72 |
| Câmera | `Game.cs:41,368` | UPGRADE | D2 / P12 | §73 |
| Estados da fornalha, mapa/tela de distrito, shop/pass visual, perfis de qualidade, acessibilidade completa, marca/logo/ícone, store art | — | **NEW** | Ícone do app vazio (`ProjectSettings.asset:305-306`) | §54, §66–69, §76–77, §110–111, §179–180 |
| Presets de criativo | `-record`, `-buyids`, `-vipnow` | UPGRADE | 3 receitas; DM pede 9 | §78 |

Visual Robustness Gate (§73) hoje: personagem pouco visível, estado de estação e gargalo fracos sem zoom, UI cobre o jogo, aparência genérica (elenco "party de RPG" com luz chapada), sem identidade de marca. Falta a luz de forja: o render plano mata o calor (§53).

### 2.3 Plataforma, save e release

Validação do artefato `ForgeStreet-0.6.0.apk` (aapt2, `zipalign -c -P 16`, `llvm-readelf`): **targetSdk 36, compileSdk 36, minSdk 26** (o "automático" pegou a API 36 instalada; fixar), **7 `.so` arm64 com alinhamento 0x4000 → 16 KB ok**. Não conferem: AAB (só APK, `Setup.cs:61-67`), assinatura de release (chave de debug; `.gitignore` não ignora `*.keystore`/`*.jks`), `versionCode` (1 em todas as versões), build não-development (`BuildOptions.Development` fixo, `Setup.cs:97`, APK `debuggable=true`), consentimento (COPPA/GDPR false e CCPA true fixos para todos, `Ads.cs:200-204`).

| Item | Onde | Classe | Motivo | DM |
|---|---|---|---|---|
| `Sim.Save/Load` chave=valor tolerante | `Sim.cs:1063-1209` | UPGRADE | Vira o payload do envelope; migração implícita já bem testada (`Save_Antigo*`) | §91, §131 |
| PlayerPrefs como storage | `Game.cs:107,906-911` | REPLACE | Arquivo atômico + `.bak`; hoje save corrompido vira estado inicial e o autosave de 5 s grava por cima, sem volta | §91 |
| Cofre (claimId, teto 2 h) | `Sim.cs:1028` | KEEP + UPGRADE | `SavedAt` que nunca recua: adiantar → resgatar → voltar → adiantar repete o pagamento | §27, §133 |
| Lifecycle (pause/focus/quit/5 s) | `Game.cs:917-923` | KEEP | Formalizar em estados | §128 |
| `Ads.cs` (simulado/real, `Logged`) | `Ads.cs` | UPGRADE | Costura natural de `IAds`: o simulado já é o mock | §40, §129 |
| Consentimento fixo | `Ads.cs:202-204` | REPLACE | Tela de idade + consentimento | §94 |
| Diário CSV | `Game.cs:1070` | UPGRADE | Costura de `IAnalytics`; cobre 12 dos 31 eventos do §86; grava também no release e cresce sem limite | §86–87 |
| `diario_report.py`, `medir_aparelho.sh` | `client/tools` | KEEP + UPGRADE | Nomes novos; PAGE_SIZE e versionCode | §113 |
| `Setup` Apply/BuildWindows/Dev/Emu | `Setup.cs` | KEEP | Funcionam | §99 |
| targetSdk automático / versionCode manual | `Setup.cs:36`, `ProjectSettings.asset:181` | UPGRADE / REPLACE | Fixar 36; versionCode monotônico gerado | §96, §98 |
| `mainTemplate.gradle` + LevelPlay 9.5.1 | `Plugins/Android`, `Packages/manifest.json` | KEEP | 16 KB ok | §97 |
| viewcheck | `client/tools/viewcheck` | UPGRADE | Não compila o caminho `#if UNITY_ANDROID`; depende do Unity local (não roda no GitHub) | §100 |
| CI `nucleo.yml` | `.github/workflows` | UPGRADE | Só `dotnet test` | §99 |
| `ISaveStorage`, `IAds`, `IAnalytics` | `Game.cs:107,909`; `Ads.cs`; diário | NEW (Wave A) | Já têm consumidor e 2 implementações | §129 |
| `IRemoteConfig` + feature flags | — | NEW | Sem costura: `Balance` é `const` → depende do A-CORE-03 | §88, §130 |
| `ICrashReporter`, `IIAP`, attribution | — | NEW junto do 1º SDK real | Interface vazia antes do fornecedor é abstração especulativa | §85, §90, §93 |
| Interstitial com caps, Remove Ads, ILRD | — | NEW (Wave E) | — | §41–42, §95 |
| Game states formais | flags em `Game.cs:189-200` | NEW | Back, save, anúncios | §128 |
| `verify quick/full/android-emulator/release` | comandos soltos no README | NEW | — | §99–103 |

### 2.4 Documentação

| Arquivo | Classe |
|---|---|
| `DOCUMENTO_MESTRE_PRODUCAO_V1.md`, `PROJETO.md`, `COMPETITIVO.md`, `CRIATIVOS.md`, `LEVELPLAY.md`, `PROVENIENCIA.md`, `RENDER_SPRITES.md`, `BENCHMARK_*.md`, `TESTE_COMPARATIVO.md` | ATUAL |
| `FASE8_VIP_VELOCIDADE.md`, `FASE9_ENCOMENDAS.md`, `VALIDACAO_V06A.md` | ATUAL até a Wave B / Release 0.6 |
| `BALANCE.md` | ATUAL (separar vigente × histórico) |
| `GDD.md`, `ART_BIBLE.md`, `ASSETS.md`, `BACKLOG_V07.md`, `ROTEIRO_TESTE_POCO.md` | SUBSTITUIR (GDD v2, Art Bible v1, assets por Wave, PROJETO + tickets, gate POCO no checklist de release) |
| `AREA2_*`, `FASE3`–`FASE7`, `ESTUDO_LOGISTICA`, `PROXIMOS_EXPERIMENTOS`, `VALIDACAO_APARELHO/FASE2/FASE3/V05` | HISTÓRICO → `docs/archive/` (com `git mv` e links corrigidos) |

## 3. Tickets

### Wave A — Foundation (não depende do POCO)

Correções já identificadas (fora das Waves, `fix/`)
- **FIX-01 · Flags do intent só em build de desenvolvimento** — `Ads.Arg` lia o extra `unity` do intent em qualquer build: no release, `-fakeads` liberava recompensa com anúncio simulado e `-adtest` abria o teste. Aceite: release ignora o intent; dev continua lendo · P · —
- **FIX-02 · Recompensa tardia logada** — recompensa que chega depois da janela de 2 s é descartada sem log (`Ads.cs:107-108`), ao contrário de `LEVELPLAY.md`; watchdog do `_busy` · P · —

Núcleo e save (raias 1 + 3)
- **A-CORE-01 · Envelope de save** (schema, contentVersion, writtenAt, checksum SHA-256) e cadeia de migração — saves antigos de todas as versões carregam; 1 byte alterado = `Corrupt` (não reseta); schema mais novo recusado sem sobrescrever; ida e volta idêntica; o `Load` antigo continua lendo (downgrade) · P/M · —
- **A-PLAT-02 · SaveStore atômico** (`.tmp` → troca → `.bak`; espelho na chave `fs.save`; arquivo corrompido em quarentena; nunca grava estado inicial por cima de save existente) — testes dotnet em pasta temporária; Windows + `-autoplay`; no emulador, 0.6.0 com progresso → instalar por cima → progresso mantido · M · A-CORE-01
- **A-CORE-02 · Exploits do §133 como testes** (relógio para trás/frente, cofre duplicado, encomenda/baú duplicados, boost ao reabrir) + **recarga da Velocidade no save** + **`SavedAt` que nunca recua** (evento `clock_back`) — um teste por ataque, todos verdes; números do bot inalterados · P · A-CORE-01
- **A-CORE-03 · `Tuning` de instância + feature flags** (Orders, VIP, Boost, Baús) — `Tuning` padrão mantém a assinatura do bot de 60 min; teto offline de 7.200 s fica trava, nunca knob · M/G · —
- **A-CORE-04 · IDs estáveis em string** e save por ID (migra o `up=`) — reordenar a tabela num teste não quebra o save · M · A-CORE-01
- **A-CORE-05 · Content validator puro** (IDs únicos, pré-requisitos sem ciclo, unlock alcançável, custo > 0, chaves de texto/analytics, bocas e pads fora dos corpos) — verde no conteúdo atual; 1 teste negativo por regra · M · A-CORE-04
- **A-CORE-06 · Ouro e custos em `long`**, curva sem estouro — custo monotônico até o tier 200 · P · A-CORE-01
- **A-CORE-07 · Bot 2.0 + `client/tools/econsim`** (Perfect, Humanized, Slow, F2P, AdWatcher) — reproduz o BalanceTests; mil rodadas cabem no PC; aponta o "ouro sem destino" depois de ~42 min · M · A-CORE-03
- **A-CORE-08 · Prova vermelha versionada** (`client/tools/mutantes`, Python stdlib, ≥ 10 mutações em economia/encomendas/save/offline, todas mortas) · M · — · DECISÃO só se for Stryker

Plataforma
- **A-PLAT-04 · `client/tools/verify.sh quick|full|android-emulator`** (quick = core + viewcheck + viewcheck Android; full = + EditMode + BuildWindows + autoplay; emulador = BuildAndroidEmu, install, `-bot`, logcat sem exceção, BACK e HOME; um processo pesado por vez) — `quick` sai 0 na `main` · M · —
- **A-PLAT-05 · `BuildAndroidRelease` + `verify release`** (AAB, sem Development, targetSdk 36 fixo, versionCode gerado, keystore só por variável de ambiente, `.gitignore` com `*.keystore`/`*.jks`; checa 0x4000, `zipalign -P 16`, só arm64, não-debuggable, permissões) — reprova o APK 0.6.0 e aprova o AAB · M · A-PLAT-04 · DECISÃO (chave de upload / Play App Signing)
- **A-PLAT-06 · Costuras `IAds`/`IAnalytics`/`ISaveStorage`** + diário na taxonomia §86/§87 (session_end, tutorial_step, rewarded_offer, currency_source/sink, SO, RAM, faixa de fps, build id) + rotação do arquivo — `Game` não chama PlayerPrefs, File nem LevelPlay direto; `diario_report.py --autoteste` lê nomes antigos e novos · M · A-PLAT-02
- **A-PLAT-07 · Game states** (Boot, Loading, Playing, Modal, Ad, Paused, Transition) derivados dos flags atuais — roteiro do voltar da V06A passa em cada estado · M · —
- **A-PLAT-08 · Feature flags + RemoteConfig local** (defaults + JSON de dev) com travas de guardrail (offline ≤ 7.200 s, caps de anúncio) — desligar anúncios/IAP por flag sem rebuild · M · A-CORE-03, A-PLAT-06 · DECISÃO (provedor remoto; P8)
- **A-PLAT-09 · Analytics e crash remotos** + símbolos nativos por build, ligados só depois do consentimento · M · A-PLAT-06, E-PLAT-01 · DECISÃO (P8)
- **A-PLAT-10 · CI ampliado** (dotnet + `diario_report --autoteste`; Unity/viewcheck por GameCI ou runner próprio) · P · A-PLAT-04 · DECISÃO só na parte Unity (licença em secret)

Visual (arte começa antes da Wave F)
- **A-ART-01 · Alvo visual da oficina em 9:16** (montagem sobre a cena atual) — PNG 1080×1920 + 8 respostas do §73; ferreiro ≥ 9% da largura · M · — · DECISÃO (câmera, escala, luz; P12)
- **A-ART-02 · Preset de render v2 "forja"** (toon 2 tons + contorno quente) — contact sheet flat × v2 do ferreiro e da fornalha · M · FBX em `arte/tripo`
- **A-ART-03 · Câmera e escala com fila legível** — fotos `-cam 9` com fila cheia, nada sob a HUD · P · A-ART-01 · DECISÃO (P12)
- **A-ART-04 · Herói com Carry, Interact e Celebrate** — folha com 5 clipes; `WorldView` usa Carry com pilha · M · A-ART-02 · DECISÃO (download Mixamo, P11)
- **A-ART-06 · Fornalha legível em 6 estados** (fria, aquecendo, ativa, sem minério, saída cheia, overdrive), só a partir do que o `Sim` expõe; extrai `View/StationView.cs` — 6 fotos `-shot`, 6/6 acertos em cinza; viewcheck 0 erros; nada no Core · M · —
- **A-ART-07 · Bigorna com martelo-pilão** (pilão, faíscas, som e pop no mesmo compasso) · M · A-ART-02
- **A-UI-15 · Fonte OFL** (Lilita One ou Fredoka) — troca em `Art.Font()`, licença em `PROVENIENCIA.md` · P · — · DECISÃO (download, P11)
- **A-UI-16 · HUD v2 com uma paleta só** (resolve D7) — nenhum `#121622` na HUD; fotos a 540 e 432 · P · —
- **A-UI-17 · Ajustes completos + perfis Battery/Balanced/High** (música, efeitos, vibração, reduzir efeitos) — 3 perfis mudam teto de efeitos, fps e escala sem tirar informação · M · A-AUD-23
- **A-UI-18 · Cartões de melhoria v2** (nome, efeito, custo, ícone, benefício em %; estados) · M · —
- **A-UI-19 · Fila de avisos fora da área de jogo** — nenhum aviso cobre estação na foto do bot aos 20 min · P · —
- **A-AUD-22 · Haptics por importância** — sem vibração na venda comum; vibra em upgrade, VIP, desbloqueio e venda grande · P · —
- **A-AUD-23 · Identidade sonora v1 + AudioMixer** (Music/SFX/Ambience; fogo, metal, esteira, multidão) — nenhuma senoide no release · M · — · DECISÃO (banco de sons ou crédito, P11)

Produto
- **A-PROD-01 · `PROJETO.md` + Documento Mestre no repo** — este PR · P · —
- **A-PROD-02 · Arquivar os históricos** (§2.4) — `git mv`; 0 links quebrados · P · A-PROD-01
- **A-PROD-03 · Reescrever guardrails** (`CLAUDE.md`, README, GDD) conforme P1–P5 · P · DECISÃO (P1–P5)
- **A-PROD-04 · Issues no GitHub por ticket** (labels `wave:A`–`I`, raia, estado; template com Definition of Ready/Done) · P · A-PROD-01 · DECISÃO (publicar issues)
- **A-PROD-05 · Custos recorrentes** (Unity CI, provedores, Play US$ 25, verba de UA) com valor, fonte e data · P · — · DECISÃO
- **A-PROD-06 · Template "pacote de distrito" em dados** — Old Forge 100% em dados, aprovado pelo validator · M · A-CORE-05
- **A-PROD-07 · Velocidade medida** (tickets/semana por raia após 2 semanas; estimativas refeitas) · P · A-PROD-04

### Wave B — Core/Meta (critério comum: save antigo carrega e o econsim passa)
- **B-CORE-01 · `ItemDef`/`RecipeDef` multi-insumo** — as 4 linhas atuais em dados com assinatura igual; um produto de 2 insumos funciona · G · A-CORE-03, A-CORE-04
- **B-CORE-02 · `ClientDef` por família** (tira o caso especial da joia) · M · B-CORE-01
- **B-CORE-03 · Quality System determinístico** — `QualityBreakdown()` explica o resultado; save antigo = Normal · M/G · B-CORE-01 · DECISÃO (fórmula; P10)
- **B-CORE-04 · `WorkerDef`** (nível, especialidade; Clerk, Foreman, Quartermaster) · G · B-CORE-01
- **B-CORE-05 · Orders 2.0** (Common = encomenda atual; 1 teste por tipo; prêmio com ID de transação) · G · B-CORE-01, B-CORE-03 · DECISÃO (Rush com penalidade? recomendação: só bônus)
- **B-CORE-06 · Reputation** monotônica, não gastável, limiares em dados · P · A-CORE-04
- **B-CORE-07 · Blueprints como desbloqueio** — regra no validator: receita essencial nunca só paga · M · B-CORE-01, A-CORE-05 · DECISÃO (token × desbloqueio; P1)
- **B-CORE-08 · Masterworks (Hall) + Collection** derivada dos contadores · M · B-CORE-03, B-CORE-07
- **B-ART-05 · Herói em 3 estágios visuais** — 3 silhuetas distintas em preto a 30% · M · A-ART-04 · DECISÃO (crédito ou só adereços no Blender)
- **B-ART-08 · Esteira viva** (roletes animados, item circulando) · P · —
- **B-ART-09 · Ajudantes com silhueta por papel** — ≥ 5/7 acertos no teste de silhueta · G · A-ART-02, B-CORE-04 · DECISÃO (crédito)
- **B-ART-10 · Clientes por família** (farmer, royal, collector novos) · M · B-CORE-02 · DECISÃO (~195 créditos)
- **B-ART-11 · VIP com entrada especial** (tapete/carruagem, FX, pedido destacado) · M · B-ART-10, A-AUD-23
- **B-ART-12 · Biblioteca de VFX v1** (coin burst, beam, puff, brasa) pelo pool com teto por perfil · M · A-UI-17
- **B-AUD-24 · 5 músicas + ambiente em camadas** que cresce com o negócio · M · A-AUD-23 · DECISÃO

### Wave C — Distritos
- **C-CORE-01 · `DistrictDef` + uma instância de `Sim` por distrito** — `old_forge` com assinatura igual; um distrito só de dados roda até o fim · G · B-CORE-01, A-CORE-05
- **C-CORE-02 · Camada Império** (carteira compartilhada, troca de distrito, save multi-distrito; save antigo vira 1 distrito) · G · C-CORE-01, A-CORE-01
- **C-CORE-03 · Managers, renda em segundo plano e Royal Charter** — ativo rende mais que fundo em todo distrito; Charter 1×; sem reset · M · C-CORE-02 · DECISÃO (P2)
- **C-CORE-04 · Grafo de waypoints por distrito** — validator prova todas as bocas alcançáveis · M/G · C-CORE-01
- **C-CORE-05 · Conteúdo dos distritos 2–6** (tiers 2–5, curva por distrito, 1 mecânica nova cada) — econsim sem zona morta · G por distrito · C-CORE-01..03, Wave B
- **C-ART-13 · Kit de silhueta por distrito (6)** · G · Wave C · DECISÃO
- **C-ART-14 · Marca, logo e 4 variantes de ícone** — lê a 48 px; `m_Icons` preenchido · M · A-UI-15 · DECISÃO
- **C-UI-20 · Mapa de distritos e tela de distrito** · G · Wave C

### Wave D — Meta / LiveOps
- **D-CORE-01 · Calendário + ledger de transações** (núcleo recebe `nowUnix`) — nenhuma recompensa duplica em save/load, reabertura ou relógio para trás · M · A-CORE-01, A-CORE-04 · DECISÃO (aceitar trapaça local de relógio até haver hora de servidor)
- **D-CORE-02 · Missões Daily/Weekly/Achievements** sobre o fluxo de `Ev`; reset não pune (§135) · M · D-CORE-01
- **D-CORE-03 · `EventDef` com modificadores sobre o `Tuning`** — evento novo entra só com dados · M · A-CORE-03, D-CORE-02 · DECISÃO (moeda de evento; P1)
- **D-CORE-04 · Pass e Royal Contracts em dados** — validator: nada obrigatório só no premium · M/G · D-CORE-01..03, B-CORE-05 · DECISÃO (Gems; P1)
- **D-PROD-01 · Calendário LiveOps de 8 semanas** (S1 Royal Commission: Royal Week, Forge Frenzy, Armorer's Challenge, Masterwork Hunt "King's Sunblade"; S2 Ember Festival: Blacksmith Festival, Rare Ore Rush, Merchant Caravan, Masterwork Hunt "Ember Crown"; reservas Forge Frenzy-B e Royal Week-B só com o distrito 1) — cada evento com os 9 campos do §28 · M · D-CORE-03 · DECISÃO (datas)
- **D-PROD-02 · Spec do Pass S1/S2** — bot F2P fecha a trilha free · M · D-PROD-01 · DECISÃO (preço)
- **D-PROD-03 · Daily, weekly, login de 7 dias, achievements** (≥ 30 / ≥ 12 / ≥ 60, cada um ligado a um evento do §86) · P · taxonomia

### Wave E — Monetização
Bloqueio: nenhuma monetização nova vai para a loja antes do E-PLAT-01 (consentimento/público) e do A-PLAT-05 (release real).
- **E-PLAT-01 · Idade + consentimento na 1ª abertura** (substitui `Ads.cs:202-204`; menor de 18 = sem anúncio personalizado; revogável) — nenhum SDK inicia antes da resposta · M · A-PLAT-02 · DECISÃO (público-alvo na Play Console; ECA Digital)
- **E-PLAT-02 · Rewarded por config** (cofre 2× do D5 + placements do §40) com cooldown no núcleo e `rewarded_offer` — testes de chamada dupla e de recompensa tardia · M · A-PLAT-08 · DECISÃO (ad units novas no painel)
- **E-PLAT-03 · Interstitial atrás de flag desligada** (só em Transition, nunca no tutorial nem durante ação, cap por sessão + cooldown) — teste dos 5 vetos · M · A-PLAT-07, A-PLAT-08 · DECISÃO (P3)
- **E-PLAT-04 · Unity IAP** (compra pendente confirmada só depois do save gravado; restore; entitlements re-derivados do recibo; loja falsa em dev) — mocks de compra interrompida, duplicada e restaurada · G · A-PLAT-02, E-PLAT-01 · DECISÃO
- **E-PLAT-05 · Remove Ads ou Pacote do Mestre** (não-consumível, sem 2ª moeda) — restore em instalação limpa · P · E-PLAT-03/04 · DECISÃO (P1)
- **E-PLAT-06 · Impression-level revenue do LevelPlay → `IAnalytics`** · P · A-PLAT-06
- **E-PLAT-07 · Exploits de compra e anúncio** (§133: IAP restore, recompensa duplicada) em dotnet · M · A-CORE-02, E-PLAT-04
- **E-UI-21 · Shop e Pass visual** (cartões com visual, preço e tag; trilha free/premium) · G · Waves D e E

### Wave F — Arte/áudio premium (fechamento)
- **F-ART-25 · Store art, 9 presets de criativo (`-preset <nome>`) e auditoria "sem placeholder"** (§178) · G · tudo acima

### Wave G — Comercial
- **G-PROD-01 · Testadores + device farm no lugar do POCO** — ≥ 12 testadores no Closed por 14 dias; pre-launch report de 1 AAB; matriz de ≥ 5 aparelhos (Android 10–16) · M · AAB · DECISÃO (P9)
- **G-PROD-02 · Plano Internal → Closed → Limited → Global** (entrada, saída com crash-free ≥ 99,5%, ANR, D1/D7, geo, duração) · P · G-PROD-01 · DECISÃO
- **G-PROD-03 · Checklist `POCO_RELEASE_GATE`** (os itens do §113/§151, PASS/FAIL/BLOCKED, usa `medir_aparelho.sh`) · P · A-PROD-02
- **G-PROD-04 · Pacote de privacidade** (política, Data Safety, IARC, LGPD/GDPR, app-ads.txt; nenhum consentimento global no release) · M · DECISÃO (ECA Digital, classificação etária)
- **G-UA-01 · Banco de 30+ criativos em 10 famílias** (transformação, gargalo, fail/fix, cadeia, escolha, automação, VIP, Masterwork, distrito, Royal Contract) — cada um com hook 0–2 s, preset e métrica, ou "bloqueado por Wave X" · P · —
- **G-UA-02 · 9 presets de captura (§78)** — `-preset` em 2 execuções gera os mesmos quadros · M · B/C para 4 deles
- **G-UA-03 · 4 ícones, 2×8 screenshots, feature graphic, vídeo** — gate §73 por peça · M · C-ART-14 · DECISÃO
- **G-UA-04 · ASO pt-BR/en/es** (2 short descriptions para experimento) · P · P3 · DECISÃO
- **G-UA-05 · Dicionário de analytics + dashboards** (§86, §116, §118, §141) · M · interface de analytics · DECISÃO (P6, P8)
- **G-UA-06 · Gate M (teste de criativos)** — CPI, IPM e hook rate por criativo e geo · M · G-UA-01 · DECISÃO (verba, P13)

- **G-PLAT-01 · Attribution** (AppsFlyer **ou** Adjust, só depois do consentimento) · M · E-PLAT-01 · DECISÃO
- **G-PLAT-02 · Política de privacidade com URL, Data Safety a partir do manifest mesclado, app-ads.txt, revisão de `ACCESS_ADSERVICES_*`** · P · DECISÃO
- **G-PLAT-03 · Release automatizada** (tag → AAB → manifesto do artefato com SHA do commit, SHA-256 e símbolos → trilha interna) · G · A-PLAT-05 · DECISÃO

### Wave H — QA / Release Candidate
- **H-PLAT-01 · Matriz de emuladores** (API 26/30/35/36, imagem 16 KB com `getconf PAGE_SIZE` = 16384, tablet sw600dp) · M · A-PLAT-04/05
- **H-PLAT-02 · Soak de 60 min + migração de saves de todas as versões publicadas + checklist de freeze** · M · —
- **H-PLAT-03 · Pre-launch report + closed testing** · P · G-PROD-01 · DECISÃO

### Wave I — POCO final
- **I-PLAT-01 · Roteiro `POCO_RELEASE_GATE`** (§151) sobre o AAB de release → PASS / FAIL / BLOCKED · P · Wave H · DECISÃO (license tester)
- **I-PLAT-02 · `medir_aparelho.sh` ampliado** (versionCode/targetSdk instalados, PAGE_SIZE, temperatura em série por 30 min; pull do diário sem `debuggable`) · P · —

## 4. Esforço e ordem

Esforço grosso (1 pessoa + agentes; não há velocidade medida ainda — refazer no A-PROD-07): A 2–3 sem · B 4–6 · C 10–15 · D 3–4 · E 3–4 · F 8–12 (contínua desde a semana 2) · G 2–3 + esperas externas · H 2–3 · I ≤ 1. Caminho crítico ~6–9 meses até a v1.0 RC.

Maior risco de produção: um aprovador e um PC de 7,7 GB que roda um processo pesado por vez, contra as 6 raias paralelas do §152. Eficiência sem cortar ambição: conteúdo em dados verificado só com `dotnet` (sem Unity), builds no CI, template de distrito, Masterworks como kit base + acessório + material, aprovação por pacote de Wave.

Primeiras 4 semanas:
1. A-PROD-01..04 + pacote único de decisões P1–P13; A-CORE-01 (save) antes de qualquer dado novo; A-CORE-03/04/05; `verify quick/full`; G-UA-01; abrir o Internal (o relógio de 14 dias do Closed é calendário).
2. Interfaces de serviço com mocks + provedor escolhido; Bot 2.0/econsim; herói + forja (A-ART-01/02/06).
3. Quality/Masterwork, Orders 2.0, Blueprints, Reputation; schema de evento e taxonomia fechados.
4. Distrito 2 em greybox 100% em dados; AAB v0.7-alpha no Internal; medir velocidade e refazer estimativas.
