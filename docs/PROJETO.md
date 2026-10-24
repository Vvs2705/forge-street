# PROJETO — Forge Street (fonte única de verdade)

Atualizado: 2026-10-14 · Dono: Vinicius · Direção: [`DOCUMENTO_MESTRE_PRODUCAO_V1.md`](DOCUMENTO_MESTRE_PRODUCAO_V1.md) (prevalece em conflito com documentos antigos, §186) · Mapa e tickets: [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md).

> O `LEIA_PRIMEIRO_PRODUCAO_V1.md` cita `FORGE_STREET_AUDITORIA_COMPLETA_2026-10-09_v1_0.md`, que não foi encontrada no PC. Por decisão do Vinicius (2026-10-09), o mapeamento seguiu sem ela; o lugar dela é ocupado pelo mapa da Leva 1 (4 raias, só leitura).

Histórico: contratos de fases já entregues (FASE3–7, AREA2, estudos e validações antigas) ficam em `docs/archive/`; em conflito, este arquivo e o `GDD.md` prevalecem.

Estados de um item: PROPOSTO → APROVADO → IMPLEMENTADO → INTEGRADO → TESTADO → VALIDADO → LANÇADO.

## 1. Versão

| Item | Valor | Evidência |
|---|---|---|
| Lançada | **0.5.1** (Release `v0.5.1`): POCO F4 60 fps, p95 16,6 ms, PSS 325–345 MB, 0 crash | `VALIDACAO_V05.md`, "Teste no POCO F4" |
| `main` | **0.6.0** (CHANGELOG `[Unreleased]`): juice, som/vibração, Encomendas, revisão de UX, voltar do Android; testada só no emulador | `VALIDACAO_V06A.md` |
| Engine | Unity 6000.3.23f1 | `client/ProjectSettings/ProjectVersion.txt` |
| Android | IL2CPP ARM64, minSdk 26; o APK 0.6.0 sai com **targetSdk 36** (automático) e **16 KB ok** (7 `.so` com alinhamento 0x4000). Ainda sem AAB, sem chave de release, `versionCode` 1, `BuildOptions.Development` fixo | `ProjectSettings.asset:181-183`, `Setup.cs:61-67,97` |
| Testes do núcleo | 87/87 (79 Core + 7 Balance + 1 Textos), 17 s | `dotnet test client/tools/coretests`, 2026-10-09 |

## 2. Estado real (Leva 1, 2026-10-09)

- **Núcleo `FS.Core`:** C# puro (`noEngineReferences`), sem sorteio nenhum (VIP por sequência de Weyl, encomendas em rodízio). Passo de tempo variável (`Tick(dt)` com o `deltaTime` do aparelho): mil rodadas iguais dão o mesmo resultado, mas uma sessão real não é reproduzível. `Sim.cs` é um monólito de ~1.200 linhas.
- **Conteúdo:** números centralizados em `Defs.cs`, comportamento em `enum`/`if` (6 itens fixos, estações no construtor, cadeia de 1 insumo). Não é data-driven no sentido do §126.
- **Economia:** produção completa do bot humano 41:31; a pessoa real foi 1,7–3,7× mais lenta. Offline 25% da taxa, teto 2 h.
- **Bot:** 2 perfis (humano e ideal); não usa VIP nem velocidade.
- **Mutation testing:** só "prova vermelha" manual (mutações em cópias do núcleo, `BALANCE.md`); o script ficou fora do repo.
- **View:** separada do núcleo, mas `WorldView.cs` (~1.700 linhas) e `Game.cs` (~1.080) concentram tudo. Fonte Arial (`Art.cs:378`), áudio de 11 senoides sintetizadas, sem música, ícone do app vazio, fornalha com 1 quadro só.
- **Anúncios:** LevelPlay 9.5.1 com 2 rewarded (Chamar VIP, Velocidade) na conta da V-STACK desde 2026-10-14 (#67; só a rede ironSource ligada até conectar o Unity Ads no painel); build de teste usa anúncio simulado. Consentimento por idade na 1ª abertura (E-PLAT-01): o SDK só inicia depois da resposta.
- **Save:** chave=valor em PlayerPrefs (`fs.save`), autosave a cada 5 s; migração implícita pelo parser tolerante, bem testada.
- **Analytics:** só local (`diario.csv` + `client/tools/diario_report.py`); cobre 12 dos 31 eventos do DM §86.
- **Arte:** sprites pré-renderizados (Tripo → Mixamo → Blender Workbench → folhas PNG); proveniência em `PROVENIENCIA.md`; saldo Tripo 20.720 créditos.
- **UA:** criativos #1, #2 e #8 gravados do build real; nenhum teste pago.

## 3. Direção

Tycoon de crafting completo (Documento Mestre §193), organizado em Waves A → I (§184). POCO F4 só em marcos (§2): Feature Complete, Release Candidate, bug Android físico específico e build de publicação.

## 4. Decisões vigentes

| # | Decisão | Fonte |
|---|---|---|
| V1 | Produto comercial completo, sem reduzir ambição; eficiência por dados, ferramentas e templates | DM §0, §170 |
| V2 | POCO só nos 4 gates | DM §2 |
| V3 | Sprites pré-renderizados (melhorar livremente) | DM §58 |
| V4 | Sem lootbox; nada aleatório pago (ECA Digital, Lei 15.211/2025) | DM §45; `CLAUDE.md` |
| V5 | Sem prestige destrutivo (Royal Charter) | DM §11 |
| V6 | Conteúdo em definições + Content Validator | DM §108, §126 |
| V7 | Preservar `FS.Core`, determinismo, bot, testes, proveniência e pipeline de criativos | LEIA_PRIMEIRO; DM §0 |
| V8 | Commit, push, PR e merge sempre autorizados depois do `verify.sh full` (regra do Vinicius, 2026-10-12); tag, Release, loja, crédito de IA, download, conta, pagamento e chave real só com aval | `CLAUDE.md` |
| V9 | **P1:** só o ouro ativo até a Wave E; carteira com IDs de moeda; Blueprint é desbloqueio; Reputation não é moeda; Gems na Wave E com mapa fonte → ralo | Vinicius, 2026-10-12 |
| V10 | **P3:** interstitial implementado atrás de feature flag **desligada**; liga só depois de A/B no Limited Release; a loja não promete "sem anúncio forçado" até lá | Vinicius, 2026-10-12 |
| V11 | **P14:** "Chamar VIP" por anúncio com recarga de 5 min (knob de RemoteConfig), como a Velocidade | Vinicius, 2026-10-12 |
| V12 | Desenvolvimento contínuo até o 100% (DM §164), em levas; placar no §8 | Vinicius, 2026-10-12 |
| V13 | **P10 (decidido por padrão):** golpe de mestre sobe a peça 1 nível (Normal → Fina → Superior); só o golpe numa peça que já sairia Superior, com o projeto e as condições, vira Masterwork, que vai para o Hall e nunca é vendida; ajudantes e offline nunca fazem (GDD v2 §9, §13, §21) | padrão do time, 2026-10-12 — revisar |
| V14 | Categoria `Meta`: níveis de estação e de worker ficam fora da "produção completa" e do teto offline, como o luxo (GDD v2 §8–9) | padrão do time, 2026-10-12 — revisar |
| V15 | Charter do Old Forge = produção completa + Renome 400 + 1 Masterwork; o rival (FS-4) só entra depois de A/B no bot (GDD v2 §11, §21) | padrão do time, 2026-10-12 — revisar |
| V16 | Eventos usam só pontos de evento (não moeda) até a Wave E, coerente com V9 (GDD v2 §16) | padrão do time, 2026-10-12 — revisar |
| V17 | Clientes nomeados (FS-3) entram como clientes marcados dentro das famílias; corações são trilha, não moeda (GDD v2 §7, §21) | padrão do time, 2026-10-12 — revisar |
| V18 | **D6 (decidido por padrão):** Velocidade e "Chamar VIP" só depois da 1ª melhoria (knob `Tuning.AnunciosDepoisDaPrimeiraMelhoria`) | padrão do time, 2026-10-12 — revisar |
| V19 | Pisos do §3 com anúncio: medir no econsim contando ~30 s por anúncio antes de mexer em regra (o bot sem esse tempo fica 7–12 s abaixo em 3 marcos) | padrão do time, 2026-10-12 — revisar |
| V20 | **LiveOps (decidido por padrão, `LIVEOPS_V1.md` §7):** eventos às 13:00 UTC e diárias às 04:00 do aparelho; temporada de 35 dias com a semana 5 de folga; Pass de 30 níveis × 600 XP (F2P fecha a trilha free em ~28 dias); projeto da Masterwork no Hunt como fonte extra (a grátis permanente continua); premium só cosmético, conveniência e Gems (nunca ouro, Renome, XP ou Blueprint) e preço na Wave E; teto de 100 XP/dia de encomendas; minério raro só como skin; eventos de 6 dias | padrão do time, 2026-10-12 — revisar |
| V21 | **Privacidade (decidido por padrão):** 13–17 anos também sem ID de publicidade (`is_deviceid_optout`), além de nada personalizado — leitura conservadora do ECA Digital; custa receita nessa faixa (uma linha em `Privacidade.Flags`) | padrão do time, 2026-10-12 — revisar |
| V22 | **IDs (decidido por padrão):** IDs de melhoria sem prefixo (`fole`, não `up_fole`; os outros `*Def` da Wave B terão prefixo); espelho `up=` mantido no save só para downgrade à 0.1–0.6; IDs nunca são renomeados depois de publicados | padrão do time, 2026-10-12 — revisar |
| V23 | **P12 / D2 (decidido por padrão, A-ART-03):** câmera com 9 m na largura (era 11,4). Ferreiro a 9,9% da largura da tela pela altura (0,89 m no Idle S); 9 m é a menor largura em que a fila de 8 vagas cabe na horizontal com o ferreiro na fornalha da esquerda. A câmera fica o mais alto possível (fila e balão na base da HUD) com 1,5 m sob o pé do ferreiro: da fileira das fornalhas para cima a fila cabe inteira em 9:16, 9:20 e 10:16; no Depósito só o 9:20 (POCO) cabe tudo, e nos outros o balão fica preso abaixo da HUD. A bandeja aberta cobre só a fileira do Depósito. `-cam 11.4` volta à câmera antiga (`View/Enquadramento.cs`) | padrão do time, 2026-10-12 — revisar |
| V24 | **IDs de item (decidido por padrão, B-CORE-01, revisar):** itens também sem prefixo (`espada`, `machado_guerra`, não `item_espada`), como as melhorias (V22); o sprite continua `item_<id>` e o texto `item.<id>.nome`. Vale para o save (schema 3: `stocks=`/`solds=`/`holds=`/`wks=`); nunca renomear. Os outros `*Def` da Wave B seguem a V22 (com prefixo). |
| V25 | **Catálogo da v1.0 (decidido por padrão, revisar):** [`CONTEUDO_V1.md`](CONTEUDO_V1.md) é a fonte dos dados das Waves B–D (6 distritos, 38 vendáveis, 50 receitas, 29 workers, 61 encomendas, 12 contratos, 101 Blueprints, 60 Masterworks). Escala ×3 de renda por distrito ancorada no Old Forge medido; renda de fundo 25–40% da taxa; Charters 4–6 aceitam N contratos **ou** 10×N encomendas Royal (contrato nunca bloqueia); as demais em `CONTEUDO_V1.md` §15. Os números dos distritos 2–6 são modelo, o econsim confirma quando os `DistrictDef` existirem. |
| V26 | **Qualidade (decidido por padrão, B-CORE-03, revisar):** custo dos níveis de estação = metade do `ECONOMIA_WAVE_B.md` §1.5 (bigornas 250/750, escudos 325/950, ferramentas 250/750, joalheria 500/1.500): com o §1.5 o retorno medido foi 39 min (portão ≤ 20) e a produção completa do F2P 57 min; com a metade, retorno 19,5 min e os 3 portões passam (`BALANCE.md` §23). Knob `Tuning.NivelEstacaoCusto`. "Com o insumo" do golpe = a bancada tem a próxima peça (trabalhando ou com a receita na entrada). Níveis IV–V entram com os projetos (B-CORE-07). |
| V27 | **Collection e encomenda Event (decidido por padrão, B-CORE-08, revisar):** degraus da Collection a +5/+10 de Renome (o §7 pedia +10/+20, que furavam os pisos do Renome 3 e 4 junto com o Orders 2.0; `BALANCE.md` §30.6). **Encomenda Event (decidido: à parte, 2026-10-24):** conta à parte das vagas normais (a peça vendida anda a normal mais antiga que a aceita e também a do evento; o rodízio de produto de cada lado não olha o outro) e dá 17 pontos nos 10 eventos (eram Caravan 12, Royal Week-B 15, Hunts 17/20, afinados para 3 vagas disputando); o F2P de referência fecha os 10 no dia 4–5 (`BALANCE.md` §44). Hall e Collection são por distrito (`DistrictDef`); desde o #71 os painéis do Hall e de projetos mostram o distrito ativo. | padrão do time, 2026-10-14 — revisar |
| V28 | **Rua dos Armeiros (decidido por padrão, C-CORE-05, revisar):** preços do catálogo × 3,5 e produtivos × 1,55, porque o gargalo da Row é o transporte (4 Ferraz com carga 2), não a demanda; se a turma ganhar carga maior, os preços voltam para perto do catálogo. Contratos de distrito abrem com o distrito no build (não "aberto pelo jogador"); nada bloqueia. Managers sobem a renda de fundo dos distritos fora de foco, não o cofre offline do distrito ativo; botão do mapa na ordem Charter → Abrir → Gerente → Viajar (`BALANCE.md` §33–§34). | padrão do time, 2026-10-14 — revisar |
| V29 | **Marca e liga (decidido por padrão, C-ART-14 e C-CORE-05, revisar):** ~~ícone do app = variante A (ferreiro + martelo); logo em Inter Black~~ — **substituído pela escolha do Vinicius (2026-10-16, #86): ícone = emblema A3, logo de título = C2** (`client/tools/marca_v2.py`; fontes da UI = Lilita One + Nunito, #87). Na Row, o nível II das montagens/forjas pede a Rebitadeira 2 (o §5.7 pedia a 1ª estação da família; com ele a produção completa do F2P ia a 76 min) e a liga entra por slot opcional (a peça não espera por ela). | padrão do time, 2026-10-15 — revisar |

## 5. Decisões pendentes (Vinicius)

| # | Decisão | Recomendação do time | Bloqueia |
|---|---|---|---|
| P2 | Teto offline 2 h × teto ampliável por premium/assinatura (§27, §43) | 2 h como trava no núcleo; managers e premium aumentam a **taxa**, não o tempo; cofre 2× por anúncio dobra o mesmo resgate uma vez | C, E |
| P4 | "Release 0.6.0 só depois do POCO" × POCO só em marcos | Release `v0.6.0` como pre-release com a evidência do emulador | versões |
| P5 | Rerolls com Gems, "chest; cosmetic chance", offline chest (§12, §19, §40, §82) × nada aleatório pago | Baú com conteúdo fixo mostrado antes; reroll mostra a próxima opção antes de pagar; tirar "chance"; teste negativo no núcleo | B, D, E |
| P6 | Metas de retenção: GDD 26/5 × DM 28/7 (agressivo 32/9) | 26/5 = continuar; 28/7 e 32/9 = metas internas | dashboards |
| P7 | "Não ampliar conteúdo antes de medir com gente" (`BACKLOG_V07.md`) × Waves B/C | Waves seguem; medição roda em paralelo (Closed testers + bot lento) e não bloqueia | B, C |
| P8 | Provedores de analytics / crash / RemoteConfig / attribution e custo | Escolher na Wave A; tudo atrás de interface com mock | A |
| P9 | Conta do Play Console e testadores (Closed exige 12 por 14 dias em conta pessoal) | **Decidido 2026-10-24:** conta de organização da V-STACK (a do Dungeon Courier), esperando o D-U-N-S (previsão 2026-11-25); sem conta pessoal. `RELEASE.md` §3.5 | G |
| P11 | Downloads: fonte OFL (D8), clipes Mixamo, banco de sons; créditos Tripo por lote de Wave | Aprovar por pacote de Wave | A, B, F |
| P13 | Verba e conta para o Gate M (teste de criativos) e a regra "Rune Relay vai primeiro" | — | G |
| P15 | Unity no CI (GameCI com licença em secret, ou runner próprio) | Sim: o EditMode do Unity pega o que o `dotnet test` não pega (19 falhas de tupla int × long no #20) | A |

Do `BACKLOG_V07.md`: D1 e D9 estão feitos; D3 resolvida pelo dado (input "Both" sem custo de FPS); D5 e D6 viram knobs (DM §40, §47); D7 entra no redesign de HUD (§63); D4 (moeda física) fica no branch `exp/moeda-fisica` até a Wave B.

## 6. Marcos (DM §162)

| Versão | Waves | Saída |
|---|---|---|
| v0.7 | A + B | `verify full` verde; Save 2.0 migra o save da 0.6; serviços atrás de interfaces; Quality, Orders 2.0, Blueprints, Reputation, Masterworks e Collection em dados; bot fecha o ciclo; AAB no Internal |
| v0.8 | C | Distritos 2–6, cada um com identidade, família de produto, mecânica nova, Orders e Masterworks; validator e simulador de economia verdes |
| v0.9 | D + E + integração de F | Evento sem código; Pass configurável; recompensa 1×; IAP com restore; privacidade validada; Closed ativo |
| v1.0 RC | F + G + H + I | Feature Complete (§163); sem placeholder (§178); `verify release` (API 36, ARM64, 16 KB, assinatura); `POCO_RELEASE_GATE = PASS` |

## 7. Known issues

| Problema | Prioridade (§132) | Fonte | Correção |
|---|---|---|---|
| Save corrompido vira estado inicial e o autosave de 5 s grava por cima, sem volta | **P0** (perda de save) | `Sim.cs:1061`, `Game.cs:36,234,909` | PR #16 (Save 2.0) |
| Save grava `v=1` e nunca lê; sem envelope, checksum, backup nem migração explícita | P0 antes de IAP | `Sim.cs:1063-1209` | PR #16 |
| Release lia flags do extra do intent: `-fakeads` dava recompensa com anúncio simulado | P1 (exploit de anúncio) | `Ads.cs:46,103,125-140` | PR #12 |
| Recompensa que chega depois da janela de 2 s é descartada sem log | P1 (ad reward fail) | `Ads.cs:107-108,266-271` | PR #12 (`ad_reward_late`) |
| Recarga da Velocidade não vai no save: reabrir o app libera outro boost | P1 (exploit de anúncio) | `Sim.cs:117-119,252-263` | PR #20 |
| Ciclo de relógio (adiantar → resgatar → voltar → adiantar) repete o cofre | P1 (exploit de economia) | `Sim.cs:1028-1039`, `Game.cs:909` | PR #20 (`SavedAt` monotônico, `clock_back`) |
| Custo de upgrade estoura `int` a partir do tier 67 e volta para 5; ouro em `int` | P1 com 120+ upgrades / distritos | `Defs.cs:290-291`, `Sim.cs:92` | PR #20 (`long`, HUD "1,23 mi") |
| "Chamar VIP" sem recarga fura os pisos do §3 | P1 (exploit de economia) | `BALANCE.md` §22 | decidido (V11): recarga de 5 min — Leva 5 |
| ~~Impasse de navegação: 1 em 1.000 partidas o `Sim.Steer` alterna entre duas direções a caminho do pad do Mineiro~~ **fechado**: `Carrier.MoveTo` conta o tempo sem progresso e o `Steer` dá a quina como contornada (bot e ajudantes; 0 impasses em 1.000 partidas) | — | `BALANCE.md` §22.1, CHANGELOG | testes `Steer_QuinaDeFrente_*` e `Ajudantes_CicloLongo_*` |
| Vibra em toda venda (§72 pede só eventos importantes) | P2 | `Game.cs:262` | PR #19 |
| Sem AAB, chave de release, `versionCode` monotônico nem build não-development; targetSdk automático | antes da loja | `ProjectSettings.asset:181,183`, `Setup.cs:97` | PR #18 (falta a chave real: decisão) |
| ~~viewcheck não compila o caminho `#if UNITY_ANDROID` e só roda com o Unity local~~ | — | `client/tools/viewcheck` | resolvido: viewcheck Android no #13 e Unity no CI (GameCI) no #122 |
| `dotnet test` (NUnit 3.14) aceita tupla `int` × `long`; o EditMode do Unity (NUnit 3.5) não | portão | `FSCore.Tests.csproj` | regra: `verify.sh full` antes de todo PR de jogo |
| 0.6.0 sem aparelho físico | — | `VALIDACAO_V06A.md` | gate de Feature Complete (POCO só em marcos) |
| ~~Diário CSV grava também no release e cresce sem limite~~ | — | `Servicos.cs` | resolvido (A-PLAT-06: teto de 1 MB, desligado no release; conferido no #53) |
| ~~Sem analytics remoto, crash reporting e RemoteConfig~~ | — | `Ugs.cs` | resolvido no #112 (UGS Analytics e Remote Config; a exceção vai como evento `excecao`, A-PLAT-09) |
| ~~Textos no código, sem localização~~ | — | `Loc.cs` | resolvido: pt/en/es em chaves (`Loc.T`, 1.227 chaves; `LocTests`) |
| Bandeja mostra o cartão travado primeiro; "Ajudantes ágeis" fora do padrão "Ajudantes rápidos" | P2 | `VALIDACAO_V05.md` | a ordem está resolvida (`MenuBar`: disponível, sem ouro, travado); falta só o nome do upgrade (texto, `up.ajudantes_ageis.nome`) |
| Fome da Bigorna 2 (2.582 s em 60 min no bot) | P2 | `BACKLOG_V07.md` | — |
| Zona morta ~53% da curva até a produção completa; ouro sem destino depois dela | design | `BALANCE.md` §22 | Waves B (Orders 2.0, Blueprints) e C (distritos) |
| ~~**Barulheira:** muitos sons ao mesmo tempo (Vinicius no POCO, 2026-10-13)~~ | — | `Som.cs` | resolvido no #59 (categorias, orçamento de vozes, distância, ducking); falta ouvir no POCO |
| ~~**Mochila parece item solto** colado no ferreiro (Vinicius no POCO, 2026-10-13)~~ | — | `WorldView.cs` | resolvido no #62 (folha `ferreiro_mochila`, mochila presa às costas); falta ver no POCO |
| ~~Conta LevelPlay aguardando aprovação~~ | — | `LEVELPLAY.md` | resolvido no #67 (conta da V-STACK); falta conectar o Unity Ads no painel e criar as ad units dos outros 4 placements (Vinicius) |
| ~~Rua dos Armeiros: rótulo "Depósito" encosta no "Fornalha"~~ | — | `Distritos.cs` | resolvido no #66 (depósito 0,5 m mais baixo) |
| ~~Rua dos Armeiros sem obras-primas; Charter dela travado~~ | — | `BALANCE.md` §35 | resolvido no #71 (liga, Vulcana, 10 obras; bot Ideal faz o Charter em ~3:30 h de Row) |
| Row: a liga chega a só ~5% das peças (um Ferraz leva todos os intermediários); Rebitadeira 2 quase sem demanda; Vulcana e fornalha de liga com arte emprestada | P2 | `BALANCE.md` §35 | Ferraz 4 nos intermediários (+32% de renda, reafinar o preço × 3,5); arte da Row (C-ART-13) |
| Encomenda Event disputa as vagas com as normais: a trilha do evento depende de quantas vagas o jogador tem (calibrada para 3) | design | `BALANCE.md` §30.6 | decisão: contar a Event à parte e reafinar os 9 eventos |
| ~~Contrato EM BREVE da Row mostra o elmo pelo ID; rótulo "Achievements" pequeno~~ | — | `PainelMetas.cs` | resolvidos no #66 e no #70 (abas largas, Projetos e Hall no rodapé) |
| Contrato conta a mesma peça para a encomenda e para o contrato (sem o seletor de cota do CONTEUDO_V1 §8) | design | `Contratos.cs` | decidir se a cota entra antes do Limited Release |

## 8. Progresso rumo ao 100% (definição de jogo completo, DM §164)

Estimativa honesta do coordenador, atualizada a cada leva (última: 2026-10-24, ~22:00 UTC: #149–#158, com #123 das músicas). 100% = o item do §164 pronto para Release Candidate (Wave H), não "começado". Só conta o que está na `main`; branches pendentes no §9.

| Item (§164) | % | Onde está | Próximo passo |
|---|---|---|---|
| Core polido | 84 | loop completo, Save 2.0 schema 3 por ID, itens/receitas em dados, exploits fechados, ouro long, validator, `Tuning`/flags, RemoteConfig com travas; feedback do POCO resolvido no código (#59, #62); transporte de insumos (#76); replay determinístico (#99); impasse do `Steer` e recarga de 5 min do VIP fechados (§7); `Sim` em 7 parciais com o econsim ao byte e as mutações remapeadas, 0 deadlock em 1.000 partidas × 5 perfis e 60/60 mutações mortas (#127, `BALANCE.md` §43); linha de base no emulador: abertura a frio 2,0 s, PSS 265 MB (#148) | ouvir e ver no POCO |
| 6 distritos | 92 | os 6 no build com arte própria: Old Forge; Row (#66, #71, #75, #76); Quarter (#78, #79); Arsenal (#83, #84); **Arcane Foundry** (#97, 11 estações do Tripo no #107); **Grand Forge** (#98, 10 estações no #102, forja estelar clareada no #104, importados da Foundry nas rotas e na síntese no #110); managers e Império (#65) | conferir com jogador real; Carta do Ideal em 8 de 8 aos 373 min (remedido em 2026-10-24, `BALANCE.md` §41.7; Renome 9 fica em 4.200) |
| 35–45 produtos | 90 | 38 de 38 vendáveis em jogo nos 6 distritos; os 58 itens com ícone (5 insumos novos no #105); textos pt/en/es (1.227 chaves) | chave de analytics por produto conferida no painel do UGS |
| Workers | 92 | Old Forge (13), Ferraz e Vulcana (Row), Dourado e Lúcia (Quarter), Cadetes e Matias (Arsenal), Aprendizes e Ísis (Foundry), Estrela e Félix (Grand Forge); managers 2–5 dos 6; arte própria da Vulcana (#107), do Félix (#119) e do herói nos 5 estágios pelo Renome (#115, #134); teste de silhueta: os personagens de cada distrito se distinguem (#153) | conferir com jogador real |
| Quality | 92 | 4 níveis, níveis II–V, golpe, técnica, oficiais, Masterwork; liga na Row, lapidação no Quarter (#78), peça energizada na Foundry (#97) e bênção da forja na Grand Forge (#98) | conferir com jogador real |
| Orders | 90 | Orders 2.0 (#60); encomendas dos 6 distritos (Amara, Tibério, Zélia, Kael; a Royal nunca vira Common); V27: a encomenda Event tem vaga própria e não disputa as normais (#131, `BALANCE.md` §44) | conferir com jogador real |
| Royal Contracts | 90 | os 12 em dados abrem no build (os da Foundry e da Grand Forge entram com elas); 2ª vaga pela `mesa_real`; Charters do Arsenal, da Foundry e a Carta aceitam contrato ou Royal; no distrito ativo a oferta é o 1º contrato aberto dele (#136): Foundry e Grand Forge passam a fechar contratos no bot | calibrar com jogador real |
| Blueprints | 88 | 19 do Old Forge + 14 por distrito nos outros 5 (#83, #97, #98) | conferir a fila com jogador real |
| Masterworks | 92 | 60 de 60 nos 6 distritos, Hall por distrito; bot F2P que golpeia mede a 1ª obra aos 80 min (portão 4 h, #106) | calibrar o acerto "1 em 2" do bot com jogador real |
| Collection | 88 | marcos dos 6 distritos; `collection_view`/`hall_view` no diário e no UGS para a métrica "≥ 30% abrem a coleção" (#139) | medir no Closed |
| Reputation | 85 | Renome 10 níveis do Império (#65); o 8 e o 9 abrem a Foundry e a Grand Forge; teste de que nunca cai (#100); F2P chega ao 2/3/4 em 0:51 / 1:41 / 3:11 | calibrar 5–10 com jogador real (Renome 9 = knob da Carta) |
| Daily / Weekly / Achievements | 100 | 42 diárias (10 do pool C: #118 e `dly_golpe_superior1` no #124, `BALANCE.md` §42 e §45), 17 semanais, 108 conquistas (3/6 distritos e 30/60 obras no #100; `ach_workers20` derivada do Império), login de 7 dias; retroativo | — |
| Events (LiveOps) | 89 | 10 `EventDef`, os 10 rodando (Rare Ore Rush no #100); `event_enter`/`event_complete` chegam ao UGS Analytics (#112); Event à parte e 17 pontos nos 10 eventos, que fecham a trilha no dia 4–5 do F2P (#131) | reafinar com jogador real |
| Pass | 88 | 30 níveis, temporada de 35 dias, trilha free; premium como produto da loja (`iap_pass_s1_royal_commission`/`s2_ember_festival`, R$ 44,90 definitivo, Vinicius 2026-10-24) e coluna premium no Passe, tudo atrás da flag `PassePremium` desligada (#113, #117, #121); bot F2P fecha a trilha free no dia 23 de 35 e o validator recusa requisito que só o premium dá (#126); cada conquista dá metade do XP: o bot F2P fecha a trilha free no dia 28, dentro da faixa 26–30 (#150) | produto na Play Console; reafinar com jogador real |
| Ads | 74 | 6 placements por config, teto diário, ID de transação, intersticial atrás de flag, `ad_impression` (#52); tela de idade só quando o 1º botão de anúncio apareceria, nada antes do 1º toque (#125, GDD Conflito 1) | conta da V-STACK com VIP e Velocidade (#67); ad units dos outros 4 placements e Unity Ads no painel (Vinicius); A/B do intersticial no Limited Release |
| IAP | 75 | Unity IAP 5.4.4 com a `LojaUnity` (Google Play Billing 9), estados, recibos, restore e Pacote do Mestre; desligada até a chave da Play (#58, #111; `IAP.md` §5.1) | Play Console: perfil de pagamento, AAB numa faixa de teste, produto `iap_pacote_mestre` e chave RSA (Vinicius) |
| Analytics | 90 | UGS Analytics só para 18+ com aceite, evento `fs_evento` com 5 parâmetros ativo no Event Manager de production (#112; projeto Unity Cloud 305e7176-73fa-4e2d-8095-3340d3986b75 na org empresarial 4674267143380); os 31 eventos do DM §86 com o nome da taxonomia e o dicionário publicado (`REMOTECONFIG_ANALYTICS.md` §2, #137); retenção D1/D7 e os funis do DM §86 no `diario_report.py` (#157); AAB com a tabela de símbolos do IL2CPP para o crash nativo (#158); painéis FS Retencao e FS Monetizacao no UGS (`REMOTECONFIG_ANALYTICS.md` §2) | `transaction` e `adImpression` padrão do UGS (receita no FS Monetizacao) |
| RemoteConfig | 88 | UGS Remote Config (chave JSON `fs_config`) com as travas do núcleo (#112); `econsim --rc config.json` (#114); as 6 flags do DM §130 desligam pelo remoto, com teste (`DistritosNovos` nova, #141); snapshot `id@versao#hash` em todo evento, no `session_info` e no `econsim --rc` (#151) | testar as flags pelo painel |
| Save | 92 | Save 2.0 + schema 3; multi-distrito; linhas opcionais da bancada, das famílias, das cotas, do medidor dos altares e das rotas e lotes (#78, #83, #97, #98) | cloud save (opcional) |
| Release pipeline | 74 | AAB assinado por env, `verify release`, 16 KB, API 36; Unity no CI do GitHub (EditMode, builds Windows/Android, autoplay; #130, #135); `release-check` portável com o teto de 200 MB (#145); workflow `release` gera o AAB não-development e roda o check, com gatilho por tag `v*` pronto (#146); passo a passo em `RELEASE.md` (#147); símbolos do IL2CPP no AAB e `.symbols.zip` no artifact (#158) | chave de upload real e secrets (Vinicius); subir à Play com a conta de serviço |
| Arte final | 89 | estações dos 6 distritos com arte própria (#75, #79, #84, #102, #107); ícones de todos os itens (#105); programa de arte: interface v2 (#87), recompensa com vida (#88), cabeçalho dos painéis (#91), HUD v2, selos em fita, placas nos rótulos e chão (#103); arte e selo nos cartões da loja (#116); herói em 3 estágios (#115); No Placeholder Gate com 0 falta em 14 rodadas, grama CC0 (#132) e balcão com folha nos 5 distritos novos (#133); herói nos estágios 4 e 5 (#134); Visual Robustness Gate nas 15 telas, mapa dos distritos corrigido (#149); teste de silhueta (#153) | ver no POCO |
| Áudio final | 80 | 23 efeitos CC0 da Kenney no lugar dos sintetizados, no mesmo nível (#109, #132; `AUDIO.md` §9, `PROVENIENCIA.md` §19 e §23); **5 músicas CC0 do RandomMind** (tema, oficina, royal, arcane e evento) por distrito e por evento, no nível da antiga (`AUDIO.md` §10, `PROVENIENCIA.md` §24); 3 grupos, mix com prioridade, orçamento de vozes e distância (#59); **ambiente em camadas**: lareira gravada CC0 e a multidão abafada com fila ≥ 3 ou evento (#155, `AUDIO.md` §11) | ouvir no POCO |
| Store assets | 84 | marca v2 (#86); 8 screenshots × pt/en/es (#92) e a 2ª sequência "Ache o gargalo" (#108); vídeo de gameplay de 35 s 9:16 (#108); 37 criativos, ASO pt/en/es; descrições completas pt/en/es com o jogo de hoje (#142); corte de 30 s do vídeo (#152) | teste de ícones na Play Console |
| **Média** | **~87** (87,0) | | |

## 9. Retomada (atualizada em 2026-10-24)

Para um chat novo continuar sem perder nada. Leia este §9, o §8 e as decisões V9–V29 (§4).

**Meta e autorização (Vinicius, 2026-10-13):** levar o GDD a ≥ 90% (placar do §8) antes de qualquer teste no POCO ou publicação; "tudo autorizado": commit, push, PR e merge sem perguntar (sempre depois do `verify.sh full`), Tripo3D para o que for preciso (saldo visto: **16 570** créditos; registrar no `PROVENIENCIA.md`), créditos de IA. Seguem com aval explícito: tag/Release, publicação na loja, chave de assinatura real, criação de conta, pagamento. POCO F4 só no fim (30–60 fps basta).

**Vinicius, 2026-10-15:** seguir com programação e desenvolvimento até a etapa de criar a conta no Google Play (já em andamento em outro projeto); o coordenador pede o POCO quando precisar dele.

**Feito em 2026-10-13/15:** #51 Lote 5A, #52 anúncios por config, #53 RemoteConfig/Analytics, #54 WorkerDef, #55 Blueprints, #56 Eventos+Pass, #58 IAP no núcleo, #59 mix de áudio, #60 Orders 2.0, #61 distritos e Império, #62 mochila, #63 Masterworks/Hall/Collection, #64 Royal Contracts, #65 managers e Império, #66 Rua dos Armeiros afinada, #67 LevelPlay na conta da V-STACK (outra sessão), #68 docs, #69 marca e ícone, #70 metas de sistemas, #71 liga na Rua dos Armeiros.

**Feedback do Vinicius no POCO (2026-10-13):** "barulheira" (#59) e "mochila solta" (#62) corrigidos no código; falta ouvir e ver no aparelho. Perguntar o que mais incomodou nos primeiros 5 min.

**Retomada de 2026-10-16 (madrugada, Vinicius dormindo; parada por limite de uso ~06:10).** Na `main`: #75 arte das estações da Row, #76 Row-pendências (transporte de insumos, Ferraz 4, Rebitadeira 2, preço ×2,4), #77 auditoria visual, #78 **Artisan Quarter** (bancada configurável, clientes por família), #79 arte do Quarter + 6 ícones de insumo. Tripo: saldo 15 670 (900 gastos: 20 estações).

**Pendente (branches no GitHub):**
- ~~`feat/royal-arsenal`~~: na `main` pelo #83 (pátio de cotas, famílias, índice 3) e a arte das estações pelo #84.
- ~~`art/nitidez-personagens`~~ (etapa 5): na `main` pelo #81 depois de medir o APK (91,5 → 121,7 MB, 61% do limite de 200 MB; alternativa registrada no `ASSETS.md` §16).

**Programa de arte (pedido de 2026-10-16, `docs/AUDITORIA_VISUAL.md`):** etapas 1–6 feitas: auditoria (#77), nitidez (#81), marca v2 com a escolha do Vinicius, A3 no ícone e C2 no logo (#86), interface v2 (#87) e recompensa com vida (#88), sempre com build Windows e fotos antes/depois. Sobra da auditoria para uma leva de arte: HUD de cima e barra de baixo, fita nos títulos, selos de estado, rótulos do mundo, chão. Etapa 7: POCO **só com "pode testar"**, depois PR final.

**Sessão de 2026-10-23 (worktree `C:/fsv2`, em paralelo com a da Grand Forge).** Na `main`: #99, o replay determinístico. O impasse do `Steer` e a recarga do VIP já estavam fechados. **#100 mesclado** com o `verify.sh full` verde (573 testes, com a Grand Forge mesclada): 10º evento (o `GrabTime` virou knob; +10,8% de ouro/min do F2P, `BALANCE.md` §27.4), `ach_distritos3/6` e `ach_mw30/60` pelo ledger do Renome (retroativo), Renome do Império que nunca cai e migração dos saves das 3 versões publicadas. Os merges desta sessão saem pela API do GitHub na nuvem quando o filtro do PC barra (aval permanente do Vinicius). As `OrderDef` já são 60 (≥ 50). Item da Foundry: §40.5 e os 3 laços do `ArcaneTests` já estavam prontos, e as 3 mutações do #94 morreram (`mutantes.py --so`: `fila-trava-na-frente`, `wk-load-depois-da-pilha` e `bp-tecnica-zero`, 3/3 em 629 s). Arte das estações da Foundry no Tripo (~7 modelos, ~315 créditos): liberada pelo aval permanente. **Travas novas:** Unity = arquivo `C:/fsv2/.unity.lock`, Tripo = `C:/fsv2/.tripo.lock` (a raiz do C: não aceita apagar pasta sem elevação; a `C:/fsv_unity.lock` está obsoleta).

**CONTINUAR DAQUI (2026-10-24, ~22:00 UTC).** Placar **~87%** (§8, 2002/23). Na `main` desde o parágrafo de baixo: #123 5 músicas · #149 Visual Robustness Gate · #150 XP do Passe (trilha free no dia 28) · #151 snapshot `id@versao#hash` · #152 vídeo de 30 s · #153 silhuetas · #154 conserto do `verify.sh` · #155 ambiente em camadas · #157 D1/D7 e funis · #158 símbolos do IL2CPP. Fila da sessão no PC (um PR cada): IAP com loja falsa (compra, restore, pendente, recibo repetido), anúncio simulado nos 6 placements no autoplay, Renome 5–10 medido no bot, captura de exceção para o diário e o `fs_evento`, Blueprints e Coleção por dia no bot. O que falta para 90% depende quase todo do Vinicius: Ads (painel do LevelPlay), IAP e produto do Pass (Play Console, conta da empresa depois do D-U-N-S, previsto para 2026-11-25), chave de upload e secrets do `release`, projeto UGS na conta empresarial nova, e o POCO (só com "pode testar").

**CONTINUAR DAQUI (2026-10-24, ~20:40 UTC).** Placar **~85%** (§8). Na `main` desde o fim de 2026-10-23: #121 preço do Pass · #124 golpe que faz Superior · #125 tela de idade no 1º anúncio · #126 bot fecha a trilha free · #127 `Sim` em partes · #128 placar · #130 e #135 Unity no CI do GitHub · #131 V27 · #132 No Placeholder Gate · #133 balcão · #134 herói 4–5 · #136 contrato no distrito ativo · #137 analytics na taxonomia · #139 coleção medida · #140 placar · #141 flag `DistritosNovos` · #142 ASO · #144 roteiro G3/G4 · #145–#147 pipeline de release · #148 linha de base no emulador. **Portão:** os 4 jobs do `unity.yml` (EditMode, builds Windows/Android, autoplay) valem como o `verify.sh full`; o PC fica para o .exe com janela. Em aberto: #123 (5 músicas, outra sessão) e #149 (Visual Robustness Gate). Fila da sessão no PC: XP do Passe para o bot fechar entre os dias 26 e 30, snapshot do `fs_config` em todo evento de analytics, corte de 30 s do vídeo. Dependem do Vinicius: Play Console (IAP e produto do Pass), painel do LevelPlay (Ads), chave de upload e secrets do `release` (`RELEASE.md`), projeto UGS na conta empresarial nova, POCO só com "pode testar".

**CONTINUAR DAQUI (2026-10-23, fim do dia; o Vinicius segue num chat novo).** Placar **~82%** (§8). Na `main` hoje, todos com `verify.sh full` e CI verdes: #99 replay determinístico · #100 10º evento, conquistas de 3/6 distritos e 30/60 obras, Renome que nunca cai, migração dos saves publicados · #101 docs · #102 arte da Grand Forge · #103 HUD v2 · #104 forja estelar clareada · #105 ícones de 5 insumos · #106 bot F2P que golpeia · #107 arte da Foundry e Vulcana (Tripo) · #108 2ª sequência de screenshots e vídeo · #109 15 sons CC0 · #110 importados da Foundry na Grand Forge · #111 Unity IAP 5.4.4 (desligado até a chave da Play) · #112 UGS Analytics e Remote Config · #113/#117 Pass premium atrás da flag · #114 `econsim --rc` · #115 herói em 3 estágios · #116 arte nos cartões da loja · #118 diárias do pool C · #119 Félix. **Nenhum PR aberto e nenhum branch em andamento.** Tripo: saldo 14.595.

Fila para chegar a 90% (um PR por item; ver o plano em https://claude.ai/artifact/1PQAo9PBHazpwQLdNn6K6y):
1. ~~**Core:** `Sim` em partes (refatoração, econsim ao byte e mutações vivas); 1.000 partidas do econsim por perfil sem deadlock.~~ #127 (2026-10-24).
2. **Ads:** ~~tela de idade movida para quando o 1º botão de anúncio aparece (GDD, Conflito 1); consentimento antes do SDK testado~~ (#125; G3/G4 do `ROTEIRO_TESTE_POCO.md` ajustados). Ad units dos outros 4 placements e Unity Ads no painel do LevelPlay (Vinicius).
3. ~~**Events:** V27, encomenda Event contada à parte das vagas, e reafinar os 10 eventos com o bot~~ (#131, `BALANCE.md` §44: 17 pontos nos 10, que fecham no dia 4–5 do F2P).
4. ~~**Pass:** teste de que o bot F2P fecha a trilha free e validator "nada obrigatório só no premium".~~ #126 (2026-10-24); preço do premium definitivo em R$ 44,90 no #121.
5. **Áudio:** ~~5 músicas~~ (feat/musicas, `AUDIO.md` §10) e ~~fanfarras~~ (#132); falta o ambiente em camadas real.
6. ~~**Arte:** estágios 4 e 5 do herói (Renome 5 e 7, Blender); auditoria do No Placeholder Gate.~~ #134 e #132 (2026-10-24); Visual Robustness Gate no #149.
7. ~~**Metas:** `dly_golpe_superior1`~~ (#124, com `ach_golpe_superior50`; `BALANCE.md` §45).
8. **Release pipeline:** ~~Unity no CI (P15) e release tag → AAB~~ (#130, #135, #145, #146); → faixa interna precisa da chave real e da conta de serviço (Vinicius, `RELEASE.md`).
9. **Dependem do Vinicius:** Play Console (perfil de pagamento, AAB em faixa de teste, `iap_pacote_mestre`, chave RSA; `IAP.md` §5.1), teste de ícones na Play; depois o POCO F4 (só com "pode testar").

**Como as sessões de 2026-10-23 trabalharam:** duas sessões Remote Control no PC (worktrees `C:/fsv` e `C:/fsv2`), travas por arquivo (`C:/fsv2/.unity.lock`, `C:/fsv2/.tripo.lock`), um PR por item com `verify.sh full`; os merges saíram pela API do GitHub na nuvem (o filtro do Claude Code no PC barra merge, Tripo e pacotes de terceiros quando o aval não é digitado na própria conversa; aval permanente do Vinicius de 2026-10-23 para merge, Tripo, navegador e downloads; só compra nova depende dele). Conflitos entre PRs paralelos foram quase sempre só no topo do CHANGELOG (manter as duas entradas).

**Leva dos distritos 5 e 6 (concluída em 2026-10-21: #97 e #98):** raias `feat/arcane-foundry` (C-CORE-08, `Sim.Arcano.cs`, BALANCE §40, mutações af-) e `feat/grand-forge` (C-CORE-09, `Sim.Exportacao.cs`, BALANCE §41, mutações gf-), brief no scratchpad do coordenador com as lições da leva 3–4 (transporte, Charter cedo, chaves de texto duplicadas, merge da `main` no fim). Integrar a Foundry primeiro (índice 4) e a Grand Forge depois (índice 5); depois a arte das estações deles no Tripo. Fora delas: 4 mutações antigas quebradas no `mutantes.py` (`enc-rodizio-repete-produto`, `enc-validator-item`, `ad-inter-tutorial`, `liveops-limiar-2x`) num PR à parte.

**Fila das próximas levas (histórica; a vigente está no CONTINUAR DAQUI acima):**
1. ~~Distritos 3 e 4 + pendências da Row~~ (#76, #78, #83) e ~~a arte deles~~ (#75, #79, #84).
2. ~~Integrar os distritos 5 e 6~~ (#97, #98); importados da Foundry na Grand Forge e a Carta; a arte das estações deles (Tripo, aval por lote); Vulcana e herói.
3. Leva de arte da HUD (sobras da auditoria); ~~as mutações quebradas~~ (#90, #91, #94).
4. Áudio real: decidir a fonte com o Vinicius (gerado por IA com licença comercial do plano, ou banco CC0); depois 5 músicas + banco de efeitos.
5. Store: 2 × 8 screenshots pelos presets, vídeo, feature graphic com quadro novo; IAP com o pacote Unity IAP (aval de download).
6. POCO (o coordenador pede ao Vinicius quando precisar) e loja: a conta Google Play está em andamento em outro projeto.

**Como o coordenador trabalha (o que funcionou):**
- Levas de até 4 agentes em worktree isolada (`isolation: worktree`), só `dotnet` (sem Unity/emulador; Blender só headless). Cada prompt aponta o brief comum, diz qual arquivo a raia é DONA, o número da seção do `BALANCE.md`, copiar as 3 DLLs para `client/Library/ScriptAssemblies`, NUnit 3.5 com tuplas de tipo exato, `.meta` com GUID novo, sem CHANGELOG, sem push; ao fim, `git merge origin/main` ANTES das mutações e do econsim (a `main` muda no meio da raia).
- Agente de raia não consegue usar Edit/Write fora da worktree da sessão: editar por Bash/python. Prova ao byte do Old Forge: `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário + `cmp` dos CSVs.
- Integração na worktree `C:/fsv` (tem `client/Library` próprio): `git checkout -q -- .` (o Unity reescreve `ProjectSettings`), remover a worktree do agente (o branch não pode estar em duas), `git switch <branch>`, CHANGELOG (entrada no topo de `### Adicionado`), `client/tools/verify.sh full`, fotos com `-testsession` e `-shot` de caminho ABSOLUTO (idioma: `-lang en|es`), PR no modelo do repo, CI "testes do nucleo (dotnet)", `gh pr merge --squash --delete-branch -R Vvs2705/forge-street` rodado FORA do repositório, `git checkout --detach origin/main` em `C:/fsv`.
- Olhar as fotos antes do PR: nesta leva elas mostraram o texto do painel do mapa ilegível, o bônus do Hall em 4 linhas minúsculas e a mochila sumindo na Row (`Bought` por enum em vez de `Sim.Has`).
- PC de 7,7 GB: um processo pesado por vez (Unity, Blender, emulador). Raia com Blender cria `C:/fsv_blender.lock` (vira PASTA: a raiz do C: não aceita arquivo sem elevação) e o coordenador espera `^blender\.exe` sair do `tasklist` antes do Unity (o `mcp-for-blender.exe` fica sempre aberto: não confundir). Disco C: ~22 GB livres em 2026-10-14; remover worktrees de agente depois de integrar.
- AVD `fs_playstore` sem boot (precisa de `-wipe-data`); não é necessário até o gate final no POCO.
