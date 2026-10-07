# PROMPT — Continuar o desenvolvimento do Forge Street (handoff do Claude Code para o ChatGPT)

> Cole tudo abaixo da linha no ChatGPT. Se ele tiver acesso ao disco/terminal (Codex, agente local ou desktop), ele trabalha direto na pasta. Se não tiver, ele vai pedir os arquivos listados na seção 10. Estado fotografado em **2026-10-07 ~14:00**.

---

## 0. Quem você é e como trabalhar

Você vai **continuar exatamente de onde outro agente (Claude Code) parou** no desenvolvimento do **Forge Street**, um jogo mobile idle arcade 2D do estúdio do **Vinicius Souza** (V-STACK). Atue como **coordenador técnico + engenheiro de gameplay Unity + designer de economia + QA**, tudo ao mesmo tempo.

Regras do Vinicius, que valem para tudo:

1. **Responda sempre em português do Brasil.** Comentários dentro de `.cs` seguem o estilo do projeto: português **sem acento**, curtos.
2. **Execute sem pedir permissão para o trabalho de rotina.** O Vinicius autorizou explicitamente: "não quero pausas nem explicações, continue fazendo as atividades, está tudo autorizado".
   **Exceções em que você PARA e confirma antes:**
   - autenticação, dados de usuário, **pagamentos (IAP/ads)** e deploy/publicação em loja;
   - gastar **créditos do Tripo3D** (já foram 1 080; nenhum crédito novo sem aval explícito);
   - qualquer operação destrutiva (apagar pasta, sobrescrever save do usuário, `git push --force`).
3. **Nunca leia `.env` nem segredos.** Não digite senhas e não crie contas: o login nos sites (Tripo, Mixamo) é sempre do Vinicius.
4. **Estilo "ponytail" (preguiçoso no bom sentido):**
   - o menor diff que resolve, sem abstração especulativa;
   - reusar o que já existe no projeto antes de criar algo;
   - simplificação deliberada leva um comentário `// ponytail: <motivo e limite>`.
5. **Nada é "pronto" sem prova.** Rode os testes, compile e tire foto do jogo. Se algo falhar, diga que falhou e mostre a saída. Nunca diga que compilou no Unity se só compilou pelo `dotnet`.
6. **Relatório no fim de cada etapa:** RESUMO (3–5 linhas) · ENTREGAS (arquivos e o que mudou) · TESTES (comando e saída) · NÚMEROS (se mexeu em economia) · RISCOS · PRÓXIMO PASSO.
7. **Decisão de economia** (custo, preço, tempo) se toma com o bot medindo. Nunca mude um número sem medir antes e depois, e registre tudo em `docs/BALANCE.md`.

---

## 1. Portfólio e onde as coisas estão

Raiz: `C:\Users\VINICIUS\Videos\MEUS PROJETOS\JOGOS NOVOS\`

**Pastas:**

| Pasta | Conteúdo |
|---|---|
| `00_PESQUISA/` | Validação de 9 GDDs (2026-10-06). Veredito em `VEREDITO_VALIDACAO.md`; lições de pipeline de arte em `PIPELINE_ARTE_LICOES.md`. |
| `01_RUNE_RELAY/` | Puzzle mobile, **v0.1 completa** (30 níveis gerados por construção com prova de solução única, view v2, 46 testes, APK dev). Está parado; não mexa sem pedido. |
| `03_FORGE_STREET/` | **Projeto ativo**, foco desta continuação. |
| `02_`, `04_`…`09_` | Outros GDDs validados, sem código. Mystic Kitchen é NO-GO. |

**Memória e coordenação do estúdio (fora da raiz):**
- `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\STATUS_jogos-novos-validacao.md`: placar de todas as levas. A última é a **Leva 7, PAUSADA**.
- `...\Agentes\_memoria\forge-street.md`: memória do projeto (decisões e armadilhas).
- `...\Agentes\_memoria\LICOES.md`: retrospectivas.
- Personas do time: `...\Agentes\TIME DE JOGOS\COE_Estudio_Multiagentes_v1_0\AGENTS\*.md`. As usadas até aqui foram 16 gameplay engineer, 11 economy, 43 QA, 32 tech artist, 28/29 arte.

---

## 2. O jogo: Forge Street

**Gênero e loop.** Idle arcade 2D top-down em retrato (estilo "pegue, deposite e venda"). O jogador controla um ferreiro por joystick e percorre a cadeia minério → fornalha → lingote → bigorna/bancadas → produto → balcão → cliente paga. Pisar em pads compra upgrades (ajudantes, velocidade, linhas novas). O GDD está em `03_FORGE_STREET/docs/GDD.md`; a §3 descreve os primeiros 10 minutos e é a régua do balanceamento.

**Stack.** Unity **6000.3.23f1**, Built-in RP, **Input System only** (`activeInputHandler=1`), uGUI criado por código, **sem TextMeshPro**, câmera ortográfica. A arte são sprites pré-renderizados no Blender a partir de modelos do Tripo3D.

**Arquitetura** (`03_FORGE_STREET/client/Assets/_FS/`):

| Caminho | O que é |
|---|---|
| `Scripts/Core/` (asmdef `FS.Core`, `noEngineReferences`, C# 9 puro, determinístico) | `Defs.cs`: enums `Item`, `Kind`, `Upgrade`, `Ev`, `Hint`; `V2`; `Balance` com todos os números; `Upgrades` (custos e nomes); `MilestoneDef`. `Sim.cs`: simulação inteira, com `Tick(dt, inX, inY)`, estações, pads, ajudantes, filas, offline e save/load chave=valor. `Bot.cs`: bot "humano" (0,7 s de reação, 85% de stick) que é o portão de balanceamento. |
| `Scripts/View/` (asmdef `FS`) | `Game.cs`: bootstrap, câmera, HUD, dicas, eventos→SFX/FX, flags de linha de comando, diário CSV, offline. `WorldView.cs`: desenha o mundo (sprites, filas, pilhas, baús, decoração, ordenação por Y). `Art.cs`: paleta, formas procedurais dos itens, helpers. `SpriteSheet.cs`: lê `Resources/Sprites/<nome>/meta.json` e as folhas PNG. `Joystick.cs`, `Dp.cs`, `AreaSegura.cs`. |
| `Editor/` | `Setup.cs` (`FS.EditorTools.Setup.Apply`, `BuildWindows`, `BuildAndroidDev`); `SpriteImport.cs` (AssetPostprocessor: max 4096, sem mipmaps, ASTC 6x6 no Android). |
| `Tests/EditMode/` | `CoreTests.cs` e `BalanceTests.cs`, que rodam também fora do Unity, pelo `dotnet`. |
| `Resources/Sprites/` | 20 folhas, cada uma com `meta.json` e um PNG por clipe. Personagens: ferreiro, ajudante, anao, guerreira, mago, elfa, goblin, cavaleiro, nobre. Estações e objetos: fornalha, bigorna, balcao, deposito, bancada_escudos, bancada_ferramentas, bancada_joalheria, loja_joalheria, bau, arco, carroca. |

**Documentos** (`03_FORGE_STREET/docs/`):

| Arquivo | Conteúdo |
|---|---|
| `GDD.md` | Design do jogo. |
| `BALANCE.md` | Toda medição do bot; §9 é a 2ª área e §10 a fase 2. |
| `ART_BIBLE.md` | Paleta, proporção 3 cabeças, alturas por personagem, silhuetas. |
| `ASSETS.md` | Os 39 assets, prompts e clipes Mixamo. |
| `RENDER_SPRITES.md` | Ferramenta de render. |
| `PROVENIENCIA.md` | Livro-razão de créditos do Tripo, SHA-256 de cada arquivo e tasks. |
| `AREA2_JOALHERIA.md` | Contrato da 2ª área, fase 1. |
| `AREA2_FASE2.md` | Contrato da fase 2, com os desvios registrados na §6. |
| `03_FORGE_STREET/README.md` | Visão geral, comandos e log de decisões. |

---

## 3. Estado EXATO em que o trabalho parou

| Versão | Status |
|---|---|
| v0.1 (oficina, 15 upgrades) | Pronta, testada, APK. |
| Arte Lotes 1+2+3 (Tripo, Mixamo e Blender) | Pronta: 20 modelos e 9 rigs; 1 080 créditos gastos; saldo Tripo **21 370**. |
| **v0.2: 2ª área (corredor lateral + joalheria)** | **Pronta e verificada no Unity:** EditMode 27/27, autoplay OK, fotos `client/Builds/shots_art/10_area2_fechada.png`, `11_corredor.png`, `12_joalheria.png`. APK `client/Builds/android/ForgeStreet-dev.apk` (56 MB, 2026-10-07 13:35). |
| **Fase 2 da 2ª área** (Joalheiro, Lupa, Vitrine de joias, baús de marco) | **Implementada mas NÃO verificada no Unity.** Core verde pelo `dotnet` (**36/36**); a view compila pelo `dotnet` (0 erros e 0 avisos). Ainda não foi compilada nem rodada no Unity, não tem foto nem APK. |

**O que foi interrompido.** O Vinicius pediu "pause agora tudo" no meio do último ajuste. Conferi depois: **o ajuste NÃO chegou a ser aplicado** e o código está limpo e verde. Faltam aplicar duas decisões que o coordenador já tomou:

1. **Custos da fase 2:** de 2 200 / 2 900 / 3 800 para **2 200 / 5 500 / 9 000** (Joalheiro / Lupa / Vitrine de joias). Medido numa cópia: com esses valores tudo é comprado aos **40:41** em vez de 32:20, que era o alvo (~40 min). Proposta em `BALANCE.md` §10.4.
2. **Vitrine de joias com efeito que dê para medir.** Além de fila 3→5 e nobres ×0,7, o **preço da joia sobe de 60 para 80** (`Balance.JewelPriceUp = 80`). Descrição do upgrade: "Nobres pagam 80 e a fila cresce". Medido, a Vitrine hoje rende +0% de ouro, porque a linha é limitada por lingote.

**Já aplicado e aceito:** o marco de espadas é **15** (não 10). Com 10, o bot quebrava a §3 do GDD: a 2ª bigorna caía antes de 1:45.

---

## 4. Desenho atual do jogo (números que você vai mexer)

**Mundo.** 15 × 14 m (retrato).
- **Oficina:** x de 0 a 9 (`Balance.WorkshopW = 9`).
- **Rua lateral:** x de 9 a 15 (`Balance.WorldW = 15`).
- **Rua dos clientes:** no topo (y ≈ 14).
- **Câmera:** mostra 11,4 m de largura (`WorkshopW + 2·Margin`, com `Margin = 1,2`) e reserva os 15% de cima para a HUD. Fica parada no centro da oficina até o Corredor ser comprado; depois segue o X do jogador.

**Estações.** Índice, posição (x; y) e desbloqueio:

| # | Estação | Posição | Desbloqueio |
|---|---|---|---|
| 0 | Depósito | (1,5; 1,5) | — |
| 1 | Fornalha | (1,5; 5,5) | — |
| 2 | Fornalha 2 | (4,5; 5,5) | Furnace2 |
| 3 | Bigorna | (1,5; 9,5) | — |
| 4 | Bigorna 2 | (4,5; 9,5) | Anvil2 |
| 5 | Escudos | (7,5; 9,5) | Shields |
| 6 | Ferramentas | (7,5; 5,5) | Tools |
| 7 | Balcão | (4,5; 13) | — |
| 8 | Joalheria (bancada) | (12; 6,5) | Jewelry |
| 9 | Loja de joias (Counter) | (12; 11,5) | Jewelry |

**Upgrades.** A ordem do enum é a ordem de compra pretendida. **Novos itens vão sempre no fim do enum** por causa do save. Custo pela fórmula `50·1,30^tier`, arredondado a 5, exceto os que estão marcados como "override":

| tier | enum | nome | custo |
|---|---|---|---|
| 0 | FurnaceSpeed1 | Fole | 50 |
| 1 | Anvil2 | 2ª bigorna | 65 |
| 2 | Helper1 | Ajudante | 85 |
| 3 | Shields | Escudos | 110 |
| 4 | PlayerCapacity | Mochila | 145 |
| 5 | Helper2 | Ajudante 2 | 185 |
| 6 | Conveyor | Esteira | 240 |
| 7 | FurnaceSpeed2 | Fole duplo | 315 |
| 8 | PlayerSpeed | Botas | 410 |
| 9 | Tools | Ferramentas | 530 |
| 10 | CounterCapacity | Vitrine | 690 |
| 11 | Furnace2 | 2ª fornalha | 895 |
| 12 | Helper3 | Ajudante 3 | 1 165 |
| 13 | HelperSpeed | Ajudantes ágeis | 1 515 |
| 14 | HammerSpeed | Martelo veloz | 1 970 |
| 15 | SideCorridor | Corredor | **690** (override `Balance.SideCorridorCost`) |
| 16 | Jewelry | Joalheria | **1 515** (override `Balance.JewelryCost`) |
| 17 | Jeweler | Joalheiro | 2 200 (override; manter) |
| 18 | JewelSpeed | Lupa | 2 900 → **mudar para 5 500** |
| 19 | JewelVitrine | Vitrine de joias | 3 800 → **mudar para 9 000** |

Os overrides estão em `Upgrades.Cost(int tier)`. `Sim.CheapestLockedCost()` pega o **menor** custo entre os não comprados; ele define o teto do cofre offline, que é 2× esse valor.

**Pads novos (índices em `Sim.Pads`):**

| # | Pad | Posição |
|---|---|---|
| 12 | Corredor | (8,4; 11,6) |
| 13 | Joalheria | (12; 6,5) |
| 14 | Joalheiro | (14,2; 8,5) |
| 15 | Lupa | (10,2; 6,5) |
| 16 | Vitrine de joias | (14,2; 11,5) |

**Itens.**
- `Item { Ore, Ingot, Sword, Shield, Tool, Jewel }`.
- Preços `{0, 0, 10, 25, 16, 60}`.
- Lingotes por produto `{_, _, 1, 2, 1, 2}`.
- Intervalo base entre clientes `{_, _, 6, 9, 8, 14}` s, multiplicado pela Fama e pela Vitrine.
- A joia tem cor `#B07CF2` e forma procedural `Art.Jewel()`.
- A bancada de joias leva o tempo da bigorna × 2. Com a Lupa, × 0,6 sobre isso.

**Filas.**
- **Balcão principal:** `Queue`, 4 vagas (6 com a Vitrine), paciência 25 s.
- **Loja de joias:** `JewelQueue`, com clientes nobres. Vagas em `Sim.JewelQueueCap` (3, ou 5 com a Vitrine de joias), paciência 40 s. Posição de cada vaga em `JewelSlot(i) = (12 + 0,85·(i−2); 12,5)`, centrada na loja.
- **Regra anti-trava** (corrige um bug real): o cliente que chega com a fila cheia **compra direto se o produto está na vitrine**.

**Ajudantes.**

| Papel | Tarefa |
|---|---|
| 0 | minério → fornalhas |
| 1 | lingotes → bancadas, incluindo a de joias |
| 2 | produto → o balcão que vende aquele item (`Sim.Sells` / `CounterFor`) |
| 3 | Joalheiro: lingote → só a bancada de joias, e joia → loja de joias |

**Baús de marco** (`Balance.Milestones`):

| Marco | Ouro | Onde fica o baú |
|---|---|---|
| 15 espadas | 60 | oficina (2,8; 12,6) |
| 50 vendas | 250 | oficina |
| 10 joias | 600 | rua (10,2; 9,6) |
| 200 vendas | 1 200 | oficina |

- Só um baú aparece por vez em cada lugar, e nunca nasce embaixo do jogador.
- Pisar no baú paga o ouro uma única vez. Esse ouro **não** entra em `GoldEarned`, para não inflar a taxa usada no cofre offline.
- Eventos: `Ev.Milestone` e `Ev.ChestOpened`. Dica: `Hint.OpenChest`, logo depois de `BuyPad`; a view mostra "Abra o baú: {rótulo}".

**Save** (texto chave=valor no `PlayerPrefs["fs.save"]`):
- Chaves: `t`, `gold`, `up` (20 flags), `ut`, `pads`, `st<i>` (por estação que produz), `stock` (4 números: espada, escudo, ferramenta, joia), `sold`, `ms` (estado dos marcos), `ema`, `saved`, `claim`, `m` (métricas), `px`.
- Lixo vira estado inicial, e tudo é rederivado de `up` em `Recompute()`.
- **Save antigo tem de carregar sempre.** Para isso, enum, estações e pads novos entram **no fim**.

**Últimos números do bot humano** (60 min, custos atuais 2 200 / 2 900 / 3 800):

- **Compras:** Corredor 12:21, Joalheria 21:32, Joalheiro 25:25, Lupa 28:29, Vitrine de joias 32:20 (fim do conteúdo).
- **Ouro por minuto:** ~450 antes da área e ~950–1 000 depois.
- **Bancada de joias sem lingote:** 35% do tempo depois do Joalheiro, 53% depois da Lupa.
- **Primeiros 10 min:** todos os marcos da §3 do GDD continuam dentro.

---

## 5. Como rodar e verificar (Windows, Git Bash)

```bash
# Testes do core sem Unity (rápido; precisa ficar verde sempre)
cd "C:/Users/VINICIUS/Videos/MEUS PROJETOS/JOGOS NOVOS/03_FORGE_STREET/client/tools/coretests" && dotnet test -nologo -v q

# Compilar a VIEW sem abrir o Unity (core das fontes + DLLs do Unity)
dotnet build "C:/Users/VINICIUS/Videos/MEUS PROJETOS/JOGOS NOVOS/03_FORGE_STREET/client/tools/viewcheck/view" -nologo -v q
```

Unity em modo batch, rodando a partir de `03_FORGE_STREET`, com `U="/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"` e `P="$(pwd -W)/client"`:

```bash
"$U" -batchmode -nographics -projectPath "$P" -runTests -testPlatform EditMode -testResults "$P/Builds/editmode.xml" -logFile "$P/Builds/editmode.log"
"$U" -batchmode -nographics -quit -projectPath "$P" -executeMethod FS.EditorTools.Setup.BuildWindows -logFile "$P/Builds/build_win.log"
"$U" -batchmode -nographics -quit -projectPath "$P" -executeMethod FS.EditorTools.Setup.BuildAndroidDev -logFile "$P/Builds/build_android.log"
```

Smoke test e fotos com o jogo compilado, a partir de `client/Builds`:

```bash
./win/ForgeStreet.exe -batchmode -nographics -autoplay 3 -logFile autoplay.log      # espera "AUTOPLAY OK"
W="-screen-fullscreen 0 -screen-width 540 -screen-height 960"
./win/ForgeStreet.exe $W -reset -bot -speed 8 -shot "$(pwd -W)/shots_art/X.png" -shotdelay 45 -logFile x.log
```

**Flags de linha de comando:**

| Flag | Efeito |
|---|---|
| `-reset` | Apaga o save. |
| `-bot` | O bot pilota. |
| `-speed N` | Velocidade da simulação (até 50). |
| `-shot arquivo.png` | Tira foto e sai. |
| `-shotdelay s` | Espera antes da foto. |
| `-autoplay min` | Smoke test sem render. |
| `-buy N` | Compra os N primeiros upgrades (dev). |
| `-gold G` | Define o ouro (dev). |
| `-px x,y` | Posiciona o jogador (dev). |

**Armadilhas conhecidas:**
- Depois de editar `.cs`, o Unity em batch só pega a mudança num build novo, porque compila no início. Não edite durante um build.
- Na primeira abertura em batch, o Unity reimporta as pastas de sprites novas.
- O render no Blender pode travar se o PC suspender: rode cada peça com `timeout 900`.

---

## 6. PRÓXIMOS PASSOS, em ordem (faça um de cada vez, provando cada um)

### Passo A — Aplicar as duas decisões pendentes (core + docs)
1. Em `Defs.cs`: `JewelSpeedCost = 5500`, `JewelVitrineCost = 9000`, `Balance.JewelPriceUp = 80`. Troque a descrição de `JewelVitrine` para "Nobres pagam 80 e a fila cresce".
2. Em `Sim.cs`: o preço da joia no atendimento, e na compra direta da vitrine, passa a ser 80 quando `Bought[JewelVitrine]`. Ache onde se usa `Balance.Price[...]` e crie um helper `PriceOf(Item)` se ajudar. Confira a dica e o bot.
3. Testes:
   - um teste novo: com a Vitrine de joias, o nobre paga 80, e sem ela, 60;
   - ajuste a curva de custos;
   - rode o `Bot_60Minutos_Fase2`, meça de novo e coloque os limites em "medido + 30%".
4. `BALANCE.md`: §10 com os números novos como vigentes e a medição antiga mantida como histórico. `AREA2_FASE2.md` §6: registre os desvios 10 (custos) e 11 (preço 80).
5. Prove vermelho: quebre a regra numa cópia em pasta temporária, veja o teste falhar e restaure.

### Passo B — Portão no Unity da fase 2 (o que falta para dizer "pronto")
1. EditMode no Unity (espere ~37 testes verdes), BuildWindows e autoplay.
2. Fotos:
   - `-reset -buy 20 -gold 0 -px 14,13`: rua lateral completa, Joalheiro roxo, pads da fase 2;
   - `-reset -buy 20 -px 3,12 -speed 8 -shotdelay 30`: baús na oficina;
   - `-reset -bot -speed 8 -shotdelay 120`: baú de marco aparecendo num jogo normal;
   - `-reset -buy 20 -px 12.5,9.5 -speed 8 -shotdelay 60`: fila de 5 nobres sem sair do chão.
3. Confira os riscos que a raia da view apontou (e corrija só o que a foto mostrar):
   - a tinta roxa do Joalheiro se confunde com o azul do ajudante de produtos (`Lerp(branco, #B07CF2, 0,3)` em `WorldView`)?
   - o tamanho do baú (`ChestCell = 1,1 m`) e a altura do rótulo (+0,85 m) estão bons?
   - a fila de joias se sobrepõe ao baú da rua?
   - o "+60/+80" da venda direta sai cortado na borda da câmera?
4. Gere o `BuildAndroidDev` e anote o tamanho do APK.

### Passo C — Documentação e memória
- README do Forge Street: seção "v0.2 — fase 2" com o que entrou, os números do bot e as fotos.
- `STATUS_jogos-novos-validacao.md`: feche a Leva 7 e abra a Leva 8 se for continuar.
- `forge-street.md` (memória): decisões novas e armadilhas.
- `LICOES.md`: retrospectiva curta (o que deu certo e o que deu errado).

### Passo D — Backlog, depois de A–C, na ordem que fizer mais sentido (medindo)
1. **Ouro sem destino depois de ~40 min.** Opções:
   - "prestige-lite" de distrito (GDD §4: "1 prestige-lite de distrito não resetável");
   - **3ª área** (GDD §5: D7 segundo distrito);
   - upgrades de luxo.
   Proponha com números antes de implementar.
2. **Bancada de joias sem lingote 35–53% do tempo.** Opções: esteira até a joalheria, 2º joalheiro, ou fornalha na rua lateral. Meça com A/B, como foi feito em `BALANCE.md` §10.2.
3. **GDD §20 (v0.2)** também lista ads/IAP, remote config, encomendas e 1 evento. **Ads e IAP envolvem pagamento: confirme com o Vinicius antes.** Se ele aprovar, o caminho é LevelPlay/Unity IAP; o plugin oficial da Unity para Claude tem skills para isso, mas você pode seguir a documentação da Unity.
4. **Teste em aparelho Android real:**
   - joystick a 45° pode alternar entre as direções S e W, porque não há histerese;
   - possível halo do ASTC 6x6 nas bordas transparentes;
   - legibilidade dos rótulos em 6,5";
   - 60 fps.
5. **Tutorial dos primeiros 90 s e o diário de playtest** (o `Game.cs` já grava `diario.csv` e existe `01_RUNE_RELAY/client/tools/diario_report.py` como modelo).

---

## 7. Pipeline de arte (se precisar de arte nova)

**Modelos no Tripo3D** (conta do Vinicius, plano pago, saldo 21 370 créditos):
1. Gere a imagem DENTRO do site: Nano Banana 2, 1:1, 4 imagens grátis. Prompt = assunto + sufixos de estilo (`docs/ASSETS.md`).
2. Escolha a imagem pela leitura vista de 60° de cima.
3. Gere o "Modelo HD" com a receita: H3.1, 8 000 polígonos, textura **2K**, Ultra, remover iluminação, PBR, triângulos, **8K DESLIGADO**, **Privado**. Custa 45 créditos.
4. Personagem: Auto Rig humanoide com esqueleto Mixamo (20 créditos) e export FBX com predefinição Mixamo 2K. Objeto: export FBX com predefinição Blender 2K.
5. **Cuidado:** o painel às vezes volta com 8K ligado, textura 4K e 2 000 000 de polígonos ("Gerar 65"). Confira a receita antes de cada geração.

**Clipes Mixamo** (em `arte/mixamo/`): Idle, Walking, Cheering, LookingAround, Angry, Thankful, Waving. Configuração: FBX Binary, Without Skin, 30 fps, In Place.

**Sprites** — `client/tools/render_sprites.py`, Blender 5.2 headless:

```bash
"/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/render_sprites.py -- --model <fbx> --anims <pasta de clipes> --out client/Assets/_FS/Resources/Sprites/<nome>
```

- Objetos e estações: `--dirs 1 --size 256`; dá para ajustar com `--yaw` e `--elev`.
- Saída: `meta.json` com os campos `size`, `fps`, `dirs`, `dir_order` S/W/N/E, `ppu`, `pivot` e `clips[]`, mais um PNG por clipe.
- O script faz retarget por delta de repouso; aplicar o clipe cru levanta os braços.

**No jogo:**
- Escala de personagem em `WorldView.CharScale(nome)` (alturas na ART_BIBLE §4).
- Estações usam `StationArt` e `StationCell`; decoração usa `StaticArt`.

**Proveniência:** toda geração entra em `docs/PROVENIENCIA.md` (prompt, imagem escolhida, task, créditos, SHA-256). Sem essa linha, o asset não entra num build comercial.

---

## 8. Restrições técnicas que não podem quebrar

- O core não pode referenciar UnityEngine e precisa continuar determinístico: os testes comparam execuções.
- Sem TextMeshPro e sem Input Manager antigo.
- Nada de arte de área inexistente dentro de `Resources/`, porque `Resources` vai inteiro para o APK. Arte guardada fica em `client/Builds/sprites_area2/`.
- Os 10 primeiros minutos têm de continuar batendo a §3 do GDD. O teste `Bot_Primeiros10Minutos_BatemASecao3` é o guarda.
- Compatibilidade de save: enum, estações e pads só se anexam no fim, e todo save antigo tem um teste de carga.

---

## 9. Primeira resposta que eu espero de você

1. Confirme que entendeu, em 5 linhas no máximo.
2. Se tem acesso ao disco, rode o `dotnet test` (seção 5) e me mostre a saída. Esperado: 36/36.
3. Execute o **Passo A** inteiro e entregue o relatório no formato da regra 6.
4. Siga para o Passo B sem esperar, a não ser que algo dê vermelho, que bata numa exceção da regra 2, ou que você não tenha como rodar o Unity. Nesse último caso, me dê os comandos exatos para eu rodar e me mandar as saídas e fotos.

---

## 10. Se você NÃO tiver acesso ao disco

Peça que eu cole ou envie estes arquivos, nesta ordem, e trabalhe por diffs que eu aplico:
1. `client/Assets/_FS/Scripts/Core/Defs.cs`, `Sim.cs`, `Bot.cs`
2. `client/Assets/_FS/Tests/EditMode/CoreTests.cs`, `BalanceTests.cs`
3. `client/Assets/_FS/Scripts/View/WorldView.cs`, `Game.cs`, `Art.cs`
4. `docs/AREA2_FASE2.md`, `docs/BALANCE.md` (§9 e §10), `docs/AREA2_JOALHERIA.md`, `README.md`

Entregue cada mudança como um diff unificado por arquivo, mais os comandos de verificação da seção 5 que eu devo rodar.
