# PRÁTICAS APROVADAS — o que funcionou e o que não se repete

> Levantamento só-leitura de 2026-11-19 sobre ARKANA, Chronicles of Existence (COE), Dungeon Courier (DC),
> Forge Street (FS), JOGOS NOVOS (Rune Relay, Tiny Airport/Little Terminal) e a memória do estúdio
> (`MEUS PROJETOS\Agentes\_memoria\LICOES.md`, 521 linhas, e `PLAYBOOK_ESTUDIO_DUNGEON_COURIER.md`).
> Cada item traz a evidência (arquivo, PR ou commit). Repositórios: `Vvs2705/arkana`, `Vvs2705/chronicles-of-existence`,
> `Vvs2705/dungeon-courier`, `Vvs2705/forge-street`, `Vvs2705/tiny-airport-empire`.

---

## 1. Decisões fixas do ARKANA

| Tema | Decisão | Evidência |
|---|---|---|
| Engine | **Unity 6000.3.23f1** (changeset `09d2ecc7fb28`), única instalada na máquina; nunca mudou desde a criação | `ARKANA/mobile-unity/ProjectSettings/ProjectVersion.txt`; `ARKANA_AUDITORIA_TECNICA_ANDROID.md` §2 |
| Engine é definitiva | "Unity, e só Unity. *Não pretendo mudar mais.*" Nunca propor Unreal, Steam ou Godot | `README.md`; `CHANGELOG.md` 2026-09-09; `Agentes/_memoria/arkana.md:15` |
| Plataforma | **Celular Android**, paisagem obrigatória, toque. Ciclo: testar no **PC** (editor + fotos/build Windows) e depois **APK** no aparelho | `README.md`; `ProjectSettings.asset` (autorotate só paisagem); LICOES 2026-09-28 ("manter build de PC desde o início") |
| Render pipeline | **URP 17.3.0**, Forward, Render Graph ligado (compatibility off), espaço de cor **Linear**, HDR on, MSAA off, SRP Batcher on, um único asset `URP_Base.asset` mutado em runtime pela Config; SSAO serializado mas **desligado**; sombra macia **Low**; pós-processamento (tonemap, bloom, vinheta) ligado | Auditoria §3.4; `Assets/_Arkana/Settings/URP_Base*.asset`; PROJETO bloco 12 |
| Graphics API | Auto (Vulkan primeiro, GLES3 de reserva); multithreaded rendering; Swappy (frame pacing) on | Auditoria Anexo B §3.1 |
| Build Android | IL2CPP Release, **ARM64 só**, minSdk **26**, targetSdk **35**, GameActivity, package `br.com.vstack.arkana`, company `V-STACK`; tudo forçado por `Editor/Build.cs` a cada build | Auditoria §15 e §3.2 |
| Pacotes | `render-pipelines.universal 17.3.0`, `inputsystem 1.14.0` (activeInputHandler = ambos), `cloud.gltfast 6.20.0` (import de .glb no editor), `ugui 2.0.0`, `test-framework 1.5.1` (resolve 1.6.0), `com.coplaydev.unity-mcp` v10.2.0 (Unity ao vivo pelo Claude); módulos androidjni, animation, audio, physics, terrain, particlesystem, uielements, unitywebrequest etc. **Sem rede, anúncio, IAP, analytics ou crash SDK** | `mobile-unity/Packages/manifest.json`; Auditoria §3 |
| Aparelho-régua | **Poco F4** (Snapdragon 870, Adreno 650, 120 Hz) — aparelho do Diretor; não é o jogador final (volta a importar na fase de loja) | `design/PROJETO.md` "DIRETRIZ PERMANENTE" (25/08) |
| Aparelho mínimo | Android 8.0 (API 26), ARM64; "jogável em aparelho fraco com Modo Névoa" (meta de GDD, não medida) | Auditoria §15; `GDD.md` §19.2 |
| FPS alvo | GDD: **60 FPS em aparelho intermediário**; meta operacional "intermediária **45 estável**"; padrão da Config = qualidade Média, limite 60, vSync on. Medido: 34,6 FPS Média (21/09) → chão 42–54, treino 31 (27/09) | `GDD.md` §19.2 e §0 linha 19.2; `PROJETO.md` bloco 12 (gate "Performance no Poco F4") |
| Perfis de qualidade | Baixa 0,70 / 2 cascatas / sombra 2048 · Média 0,85 / 2 / 2048 (padrão) · Alta 1,0 / 4 / 4096 | `Scripts/Menu/Config.cs:81-84` (via Auditoria §3.4) |
| "FPS se mede, não se estima" | Tamanho do APK não é argumento; FPS no aparelho é | `PROJETO.md` "DIRETRIZ PERMANENTE DO DIRETOR (25/08/2026)" |
| Monetização | **Nunca pay-to-win** ("Juramento de Arkana"): poder só no loot da partida; loja e passe **só cosméticos**; mago se desbloqueia com moeda grátis, dinheiro nunca acelera; **nenhuma caixa aleatória paga** (ECA Digital); publicar o Juramento na loja | `design/gdd/GDD.md` §19.1 e §7 |
| Originalidade vs Apex | Apex é referência de ritmo e de regras (ex.: carga da suprema "modelo Apex, adotado por ordem dele"), mas os kits foram **descolados do Apex** em 20/08: cada mago tem identidade mágica própria; **copiar assets de Apex é proibido** (concept arts originais são "a maior força do projeto") | `GDD.md` §1 nota "REVISADO 20/08/2026"; `gdd/DIRECAO.md:135-136`; `gdd/PASSOS_GRAFICOS.md:425-426`; `CHANGELOG.md` 2026-08-21 ("Elenco descolado do Apex") |
| Pilares de jogo | BR de magos em **terceira pessoa** sobre o ombro (o texto antigo do GDD diz "primeira pessoa" — divergência registrada), projéteis (nada hitscan), Sintonia (combo de dupla) e terreno reativo como diferenciais; partida de 18 corpos em trios; ilha do Documento Mestre 4,8 × 4,4 km | `GDD.md` §0 ("Divergências...") |
| Assets complexos | **IA**: Tripo Studio (plano Max, 25.000 créditos/mês, criar no **site**, sem API), Meshy no site para personagem (API proibida para personagem); Higgsfield citado como opção paga para peças orgânicas (plano gratuito bloqueia API) | `design/cenario/TRIPO-STUDIO.md`; `arte/00-LEIA.md`; `PROJETO.md:440`; LICOES 2026-09-25 |
| Assets simples | **Blender por script** headless (`blender.exe -b --python arte/tools/blender/<script>.py`): kit de arquitetura `kit_documento_mestre.py` (21 peças), `otimizar.py` (decimação, escala métrica, pé no chão), `lod1.py`, `cc0_para_unity.py`, `desneon.py` | `arte/00-LEIA.md` "Blender + Claude Code"; `CHANGELOG.md` 2026-10-04 |
| Fluxo validado Tripo | site → exportar GLB 2K → `otimizar.py` → Unity (teste A×B: 293 k → 45 k tris, 117 → 13 MB de textura, visual igual à distância de jogo) | `TRIPO-STUDIO.md` "TESTE A × B"; `PIPELINE_ARTE_LICOES.md` §1.1 |
| Animação | Mixamo (77 clipes, X Bot, FBX for Unity, 30 fps) num Avatar Humanoid por mago (`EsqueletoHumano`), Mecanim; um clipe serve aos 20 magos | `design/pipeline/MIXAMO.md`; `PROJETO.md` bloco 13 |
| Fonte da verdade | `design/` decide (sem engine), `arte/` é matéria-prima, `mobile-unity/` é o produto; implementações nunca cruzam código | `README.md`; `swarm.yaml:7` |
| Portão | Unity Test Framework em `-batchmode` antes de qualquer entrega; **todo teste novo se prova reintroduzindo o defeito** | `swarm.yaml:9-10` |
| Rede | Só depois do "COMBAT & MOVEMENT GATE" passar; plano: Netcode for GameObjects + servidor dedicado em contêiner, sem amarrar fornecedor | `PROJETO.md` bloco 12 e "Lições do desvio" item 6 |
| Remoto Git | `origin = https://github.com/Vvs2705/arkana.git`; em 2026-11-19 `main` está em dia com `origin/main` (0 commits à frente); PR #10 aberto (`perf/arvores-instanciadas`) | `git -C ARKANA remote -v`; `git rev-list --count origin/main..main` = 0 |

---

## 2. O que funcionou

### 2.1 Estrutura de pastas
- **Três camadas por jogo:** `design/` (decisão, sem engine) · `arte/` (matéria-prima e ferramentas) · `mobile-unity/` ou `client/` (produto). Sobreviveu a 3 trocas de engine no ARKANA. — `ARKANA/README.md`.
- **Código do jogo num prefixo próprio:** `Assets/_Arkana/`, `Assets/_COE/`, `Assets/_DC/`, `Assets/_FS/`, `Assets/_TA/`. — CLAUDE.md de cada repo.
- **Módulos com README de 2 linhas** em `Scripts/<Modulo>/` (COE: Core, Character, Combat, Save, UI, Input, Perf...). — `chronicles-of-existence/CLAUDE.md`.
- **Núcleo em C# puro em `Scripts/Core` + `View/` + `Editor/Setup.cs` + `tools/coretests` + `tools/viewcheck`** (DC, FS, Rune Relay, Tiny Airport). — `dungeon-courier/docs/LICOES_PARA_O_ESTUDIO.md` §2.1.
- **`docs/` com fonte única de estado:** `PROJETO.md` com §0/CONTINUAR DAQUI, `GDD.md` com §0 "estado real" (matriz GDD × feito), `MAPA_CODIGO.md`, `BALANCE.md`, `PROVENIENCIA.md`, `ADR-*`. — FS `docs/PROJETO.md`; ARKANA `GDD.md` §0; DC `docs/MAPA_CODIGO.md`.
- **Um repositório por jogo e uma sessão aberta na pasta do repo** (não na raiz `JOGOS NOVOS`). — LICOES 2026-10-08 (sessão na raiz bloqueou Edit/Write nos repos aninhados).

### 2.2 Arquitetura e testes
- **Lógica pura + MonoBehaviour casca**, dependência explícita, sem God Manager nem event bus global, IDs estáveis em `snake_case`. — `chronicles-of-existence/CLAUDE.md` "Regras de arquitetura"; ARKANA Auditoria §21.
- **Teste provado vermelho** (reintroduzir o defeito) antes de aceitar. — `ARKANA/swarm.yaml:10`; `01_PROTOCOLO_ORQUESTRACAO.md` §4 Quality Gate.
- **Portão local com script único:** ARKANA `portao.ps1` (EditMode+PlayMode, linha final separa passaram/falharam/ignorados); COE `client/tools/verify.ps1 rapido|completo|android`; DC/FS `client/tools/verify.sh quick|full|android`. — COE skill `verify`; FS `CLAUDE.md`; ARKANA PROJETO bloco 12.
- **Veredito pelo XML do `-testResults`**, nunca pelo código de saída (`-runTests` roda sem `-quit`). — `time-de-jogos.md` "Armadilhas"; protocolo §4.
- **Bot + `-autoplay N`** jogando o próprio jogo para balance e portão; `econsim` no FS. — DC playbook §2.3; FS PR #165, #168.
- **Flags de teste no build** (`-testsession`, `-tela`, `-shot`, `-record`, `-bot`, `-lang`, `-tutorial`) que não tocam no save do jogador. — DC playbook §2.6.
- **Fotos antes do PR**: o coordenador olha cada tela. Pegaram defeitos que 169 testes verdes não pegaram (pouso antes do 1º toque, texto sobreposto, joystick tampando o personagem). — DC playbook §1.5; LICOES 2026-10-14.
- **Cena gerada por script e determinística** (`BootstrapSceneBuilder`, `CenaEstavel`): regerar sem mudança deixa o `.unity` igual byte a byte. — COE `CLAUDE.md` "Build e cena"; LICOES 2026-10-01.
- **Save versionado** (`SchemaVersion`, migração com DTO congelado, gravação atômica tmp+replace, teste de save antigo); recompensa idempotente; compra IAP em 2 passos. — COE `CLAUDE.md`; agente `coe-save-reviewer`; FS PR #16, #41 (COE save v3).
- **Telemetria no próprio APK:** `MedidorDeFps`, `BancadaDeCortes` (15 hipóteses de custo de GPU numa partida), linha `ARKANA CUSTO` (CPU/GPU/GC/memória/batches no logcat), automação por intent (`arkana_auto`, `arkana_fps`, `arkana_bancada`). — Auditoria §17, §22; PROJETO bloco 12.

### 2.3 Pipeline de assets
- **Tripo no site (não API)**: imagem grátis no gerador embutido → escolher pela leitura no ângulo do jogo → HD (45 créditos) → Auto Rig Mixamo (20) → FBX predefinição Mixamo (65 ossos `mixamorig:`). Saldo conferido antes/depois e registrado em `PROVENIENCIA.md`. — DC playbook §4.1, §1.7; LICOES 2026-10-07.
- **Item separado do personagem** (arma, escudo, cajado gerados à parte e presos ao osso no Blender). Regra do Vinicius. — DC playbook §4.2; LICOES 2026-10-09.
- **Blender headless conserta em vez de gerar de novo** (0 crédito): ajuste de forma, recolor por faixa, LOD1, decimação. — DC playbook §4.6-4.7; ARKANA `arte/tools/blender/`.
- **Retarget por delta de rotação sobre o repouso**, nunca action crua entre esqueletos. — DC playbook §4.3; LICOES 2026-10-06.
- **Guardar o original, derivar a versão de jogo**; originais grandes fora do git com backup; o que é imagem de UI entra no git. — `ARKANA/arte/00-LEIA.md`; DC playbook §1.6.
- **Git LFS para binário** e `.meta` sempre no commit. — COE `CLAUDE.md`; ARKANA `.gitattributes`.
- **Portões de originalidade G1 ficha → G2 concept → G3 licença/proveniência** antes de qualquer malha (COE ADR-0002), revisor `coe-art-pipeline-reviewer`. — COE `CLAUDE.md:61`.
- **Validador de arte V01–V27** (escala, pés no chão, frente +Z, ossos, T-pose, tris, LOD, LFS); UNKNOWN nunca vira PASS. — `PIPELINE_ARTE_LICOES.md` §1.4.
- **Cor medida, não escolhida pelo nome**; corrigir pelo ganho de luz medido na captura. — LICOES 2026-09-25, 2026-10-01.
- **Áudio sintetizado** (ARKANA 48 timbres, DC `audio_gen.py`): sem licença a rastrear. — Auditoria §3; DC playbook §2.9.

### 2.4 Nomes e commits
- Conventional commits **em português, com escopo do jogo**: `feat(dc-tutorial)`, `fix(fs-view)`, `perf(world)`, `docs(gdd)`, `art(fs)`. — `forge-street/CONTRIBUTING.md`; `git log` do ARKANA.
- Branches `feat/`, `fix/`, `docs/`, `art/`, `perf/`, `chore/`. — FS CONTRIBUTING; ARKANA `perf/arvores-instanciadas`.
- SemVer em `PlayerSettings.bundleVersion` via `Editor/Setup.cs`; tag `vX.Y.Z` + Release (pré-release com APK/AAB/zip). — FS CONTRIBUTING; DC playbook §8.5.
- IDs de conteúdo nunca renomeados depois de publicados (golden test no `ContentValidatorTests`). — FS CONTRIBUTING; FS PROJETO V22/V24.
- Etiquetas de bloqueio padronizadas: `BLOCKED_HARDWARE`, `BLOCKED_CREDENTIAL`, `BLOCKED_LICENSE`, `BLOCKED_MIXAMO_DOWNLOAD`. — COE skill `techdebt`; ARKANA PROJETO bloco 12.

### 2.5 CI
- **CI sem Unity sempre** (dotnet test do núcleo; validadores Python de dados) — `dungeon-courier/.github/workflows/nucleo.yml`, `forge-street/.github/workflows/nucleo.yml`, `chronicles-of-existence/.github/workflows/validadores.yml` (o cabeçalho avisa: "verde NÃO quer dizer jogo testado").
- **Unity no CI com GameCI** quando há licença em secret: `forge-street/.github/workflows/unity.yml` (unity-test-runner v4.4.0 + unity-builder v6, build Windows e Android) e `release.yml` (AAB assinado por tag `v*`, secrets `FS_KEYSTORE_*`). — FS PR #122, #130, #135, #146, #178.
- **PR template com evidências** (coretests, viewcheck, EditMode/build, números do bot, fotos). — `forge-street/.github/pull_request_template.md`.
- ARKANA **não tem CI** (só portão local) — Auditoria §18.

### 2.6 Branching e review
- **Um assunto por PR, merge com portão verde**; CHANGELOG em `[Unreleased]` mantido pelo coordenador (evita conflito entre PRs paralelos). — DC playbook §1.2; LICOES 2026-10-09.
- **Dono por arquivo nas raias paralelas** + contrato primeiro (interface C#/schema/API documentada no topo do `Sim.cs`) antes de despachar: merges sem colisão. — LICOES 2026-09-08, 2026-09-21, 2026-10-14; protocolo §6.
- **Máximo 4 raias por leva**; só o coordenador roda Unity; workers entregam código + teste + PROTOCOLO DE REPORTE. — `03_PROMPT_COORDENADOR_MESTRE.md` regras 12-13.
- **Revisores só-leitura especializados** (COE: arquitetura, arte, mobile-perf, QA-regressão, save, UI/input). — `chronicles-of-existence/.claude/agents/`.
- **Revisão no repo é feita pelo próprio coordenador** (todos os PRs vistos têm 0 reviews/comentários humanos); o "review" real foram as fotos, o XML de testes e o Revisor Cruzado. — `gh pr view` em DC#19, FS#138/#154/#172, COE#21, ARKANA#8.
- **Memória de retomada** (`docs/PROJETO.md` §0/§9 ou `STATUS_<jogo>.md` com CONTINUAR DAQUI) a cada pausa; skill `handoff` no COE. — DC playbook §1.3; COE `.claude/skills/handoff`.
- **Contrato da versão antes de codar** (`CONTRATO_Vx.md` + matriz GDD × feito). — DC playbook §1.1; Tiny Airport `CLAUDE.md`.

### 2.7 Android
- `vSyncCount = 0` + `targetFrameRate = 60` no início (Poco foi de 28–36 para 55 fps no DC). — DC playbook §9.1.
- APK de worktree via `subst R: <client>` (caminho longo quebra o CMake do Gradle). — DC playbook §9.2.
- Ler logcat com `adb logcat -d` depois do build (o Unity reinicia o adb). — Auditoria §20.
- AAB IL2CPP ARM64 com bibliotecas alinhadas a 16 KB, targetSdk 35/36, símbolos IL2CPP. — COE PR #21; FS PR #158; DC playbook §8.3.
- Anúncio não personalizado, sem `AD_ID`; LevelPlay + Unity Ads; política pública no GitHub Pages. — DC playbook §7.
- D-U-N-S e conta de **organização** da Play pedidos cedo (DC e FS esperam o D-U-N-S, previsão 2026-11-25). — DC playbook §8.1; FS PR #156.

---

## 3. O que deu errado e não se repete

| # | Falha | O que aconteceu | Evidência | Regra que ficou |
|---|---|---|---|---|
| F1 | **Trocar de engine/plataforma por feature distante** | 27/08–09/09: ARKANA foi para Unreal/Steam por rede/anticheat/Nanite; 13 dias e ~45 GB por uma ilha sem partida. Antes já tinha passado por Phaser/Capacitor → Roblox → Godot | `design/PROJETO.md` "LIÇÕES DO DESVIO STEAM / UNREAL"; `CHANGELOG.md` 2026-09-09; LICOES 2026-09-09 | Uma pessoa, uma engine (Unity) para todos os jogos; o portão é a medição pendente mais antiga |
| F2 | **Portão que mentia por omissão** | "620 testes, 0 falhas" escondia 85 fotos puladas sem GPU e **PlayMode 0 de 9** desde 25/09; o jogo saía **sem pós-processamento** e com a Sintonia invisível | `PROJETO.md` bloco 12 "ACHADOS QUE ESTAVAM ESCONDIDOS"; Auditoria P-16 | Linha final separa passaram/falharam/ignorados com motivo; rodar o portão ANTES de mexer |
| F3 | **Ondas visuais sem medir FPS** | 60 FPS (12/09) → 16–20 FPS no chão (21/09) depois de 12 ondas; SSAO custava ~12 ms | `PROJETO.md` 21/09; Auditoria §17; commit `9749474` | Toda onda visual termina com rodada no aparelho; FPS se mede |
| F4 | **Personagens pela API da Meshy** | 18 magos "parecem robôs": API sem marcação de articulações + fusão de clipes duplicou ações (lançar magia tocava "cair") | `PROJETO.md` "A leva 7 FOI DESFEITA" (27/08); `arte/00-LEIA.md` | Personagem só no site, um por um; teste que cobra "clipe X ≠ clipe Y"; `fundir_animacoes.py` proibido para personagem |
| F5 | **Modelo importado virado / animação sem laço** | glTF olha +Z e o Godot anda −Z: personagem correu de costas desde 21/08; animação sem laço = patinação | `CHANGELOG.md` 2026-08-25 e 08-26; LICOES:182 | Conferir eixo e loop na importação, com foto |
| F6 | **Trabalho só no disco** | 93 commits e ~504 MB de LFS nunca enviados (09/09–21/09) | Auditoria P-01 (hoje resolvido: 0 à frente) | Push ao fim de cada bloco verificado |
| F7 | **`git push --dry-run` com LFS** | o hook pre-push subiu 729 MB antes da decisão | LICOES 2026-09-23 | Em repo com LFS, dry-run de push = push |
| F8 | **Build regravando arquivos versionados** | `Main.unity` com fileIDs novos e URP asset alterado: 22 commits de ruído ("Main.unity regravada pelo build") | `PROJETO.md` bloco 12; Auditoria §18 | `build_apk.ps1` sai 6 se sujar arquivo versionado; geradores determinísticos |
| F9 | **`Build.cs` sobrescreve ProjectSettings** | ajuste manual (ABI, API gráfica, dev build) desfeito a cada build; só ARM64 impede AVD x86_64 | Auditoria P-04, P-05 | Variante de build com `try/finally` devolvendo o asset byte a byte; mudar o script, não o inspector |
| F10 | **Sem build de PC no início** | fogo amigo (partida se vencia sozinha em 24 s) só apareceu ao abrir o jogo, com 126 testes verdes | LICOES 2026-09-28 | Build Windows desde o 1º dia; toda leva termina com o jogo ABERTO e fotografado |
| F11 | **NUnit diferente** | `dotnet test` (NUnit 3.14) aceita tupla int × long; Unity (3.5) não: 105/105 no dotnet, 19 falhas no Unity | FS `docs/PROJETO.md` §7 e P15; LICOES 2026-10-09 | `verify.sh full` (EditMode no Unity) antes de todo PR de jogo |
| F12 | **Script de portão quebrado por PR** | comentário no meio da linha do `verify.sh` com `set -u` quebrou todos os modos | FS PR #154 (corrige #145) | Ao mexer em script de portão, rodar `quick` antes do PR |
| F13 | **PR duplicado / raias sobre o mesmo achado** | #138 fechado em favor do #136 (mesmo achado 15) | FS PR #138 | Um dono por achado; checar PRs abertos antes de despachar |
| F14 | **Branch sem PR acumulando correção** | `fix/rc-flags-painel` foi substituída pelo #172; worktrees órfãs com merge parado e WIP sem commit | FS PR #172; LICOES 2026-10-14 | Toda branch vira PR; worker lê o estado da worktree antes de agir |
| F15 | **Unity sobrescreve `Packages/manifest.json`** | ao criar/abrir projeto em batch entram Purchasing/Analytics | DC playbook §10; LICOES 2026-10-06, 10-09 | Conferir o manifest logo após criar; nada no manifest sem uso |
| F16 | **Excluir caminho de plataforma sem confirmar** | a regra "mobile fora de escopo" levou a T002 a apagar o toque; depois o usuário declarou mobile | LICOES 2026-09-29 (COE técnico pré-arte) | Confirmar plataforma com o Vinicius antes de apagar |
| F17 | **Spec externa tratada como verdade** | doc externo dizia engine/modo/rede "não definidos", tudo já estava no repo | LICOES 2026-09-24 (noite) | Spec externa passa por conta, diff contra `design/` e FPS no aparelho |
| F18 | **Tripo: receita do painel mudou sozinha** | voltou com 8K, textura 4K e 2 M polígonos (65 em vez de 45 por peça); 225 créditos perdidos num erro de upload | LICOES 2026-10-07; `PROJETO.md:232` | Conferir a receita no 1º modelo da sessão; saldo antes/depois |
| F19 | **Render headless travou 15 h** | PC suspendeu no meio | DC playbook §4.4; LICOES 2026-10-07 | `timeout` em todo render |
| F20 | **Dois processos pesados juntos** | Unity + emulador/Blender em 7,7–8 GB de RAM travam ou matam o Blender | DC playbook §1.4; Auditoria P-03 | Um processo pesado por vez |
| F21 | **Worker devolve com tarefa em background** | build longo em `run_in_background` morreu quando o worker encerrou | LICOES 2026-09-08 (noite) | Worker só reporta com o artefato existindo; build longo fica com o coordenador |
| F22 | **Arte crua no worktree** | quase se perdeu quando o worktree some | DC playbook §1.6; LICOES 2026-10-09 | Arte crua no checkout principal + drive privado |
| F23 | **Worktrees dentro do repo** | ARKANA proíbe, mas tem `.claude/worktrees/arkana-code-recovery-upgrade-6f1841` e um `auditoria-tecnica-completa-282af1.zip` de **1,1 GB** lá dentro; COE tem `.claude/worktrees/coe-baseline-validation-b75470` | `ls ARKANA/.claude/worktrees`; `find chronicles-of-existence/.claude` | Worktree fora do repo, em `%TEMP%\wt\<curto>` |
| F24 | **Docs desatualizados depois de migração** | `design/pipeline/ANDROID.md` com método/path errados, `swarm.yaml` com paths `Assets/Scripts`, `.claude/launch.json` com `npm run dev` morto | Auditoria P-15 | Varredura de docs no fim de cada migração |
| F25 | **Arquivos de regra comuns fora do repo** | CLAUDE.md de DC/FS/Tiny importam `~/.claude/estudio/*.md`, que não existem nesta máquina; o PR que versionaria o prompt mestre (DC #19) foi fechado | `gh pr list -R Vvs2705/dungeon-courier` (#19 CLOSED) | Regras do estúdio versionadas num repo (o novo `time-jogos`) |
| F26 | **Edição de CRLF/heredoc** | heredoc Bash come barra invertida em Python; travessão em `.ps1` sem BOM quebra o PowerShell 5.1; LF misturado com CRLF | LICOES 2026-10-04, 2026-10-03 | Scripts em arquivo no scratchpad; `.ps1` só ASCII fora de comentário |

---

## 4. Pendências e riscos que o time herda (ARKANA)

- Gate de combate e movimento **não passou** (04/10): strafe sem animação, câmera sem histerese, performance não medida desde 27/09. A rede não começa antes disso. — `PROJETO.md` bloco 12.
- Boot da IlhaMestre num quadro de vários segundos (~11 s no Poco); 9,8 M triângulos / 4.400 batches no chão medidos no emulador (bloco 13), corte de árvores em andamento (PR #10). — `PROJETO.md` blocos 13–14; `git log`.
- Sem Development Build, sem crash report, keystore de debug, versionCode 1 fixo. — Auditoria P-07, P-13, P-14.
- `VooVisualTests` vermelho intermitente (1 em ~15). — `PROJETO.md` bloco 12.
