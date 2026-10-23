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
| V27 | **Collection e encomenda Event (decidido por padrão, B-CORE-08, revisar):** degraus da Collection a +5/+10 de Renome (o §7 pedia +10/+20, que furavam os pisos do Renome 3 e 4 junto com o Orders 2.0; `BALANCE.md` §30.6). A encomenda Event disputa as vagas com as normais e os pontos dela foram calibrados para o jogador com 3 vagas (Caravan 12, Royal Week-B 15, Hunts 17/20); a alternativa é contá-la à parte e reafinar os 9 eventos. Hall e Collection são por distrito (`DistrictDef`); desde o #71 os painéis do Hall e de projetos mostram o distrito ativo. | padrão do time, 2026-10-14 — revisar |
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
| P9 | Conta do Play Console e testadores (Closed exige 12 por 14 dias em conta pessoal) | Abrir o Internal já na Wave A | G |
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
| viewcheck não compila o caminho `#if UNITY_ANDROID` e só roda com o Unity local | — | `client/tools/viewcheck` | PR #13 (viewcheck Android); Unity no CI = P15 |
| `dotnet test` (NUnit 3.14) aceita tupla `int` × `long`; o EditMode do Unity (NUnit 3.5) não | portão | `FSCore.Tests.csproj` | regra: `verify.sh full` antes de todo PR de jogo |
| 0.6.0 sem aparelho físico | — | `VALIDACAO_V06A.md` | gate de Feature Complete (POCO só em marcos) |
| ~~Diário CSV grava também no release e cresce sem limite~~ | — | `Servicos.cs` | resolvido (A-PLAT-06: teto de 1 MB, desligado no release; conferido no #53) |
| Sem analytics remoto, crash reporting e RemoteConfig | antes de UA | — | A-PLAT-08/09 (decisão P8) |
| Textos no código, sem localização | antes de en/es | DM §109 | — |
| Bandeja mostra o cartão travado primeiro; "Ajudantes ágeis" fora do padrão "Ajudantes rápidos" | P2 | `VALIDACAO_V05.md` | — |
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

Estimativa honesta do coordenador, atualizada a cada leva (última: 2026-10-23, depois do #99 e do #100). 100% = o item do §164 pronto para Release Candidate (Wave H), não "começado". Só conta o que está na `main`; branches pendentes no §9.

| Item (§164) | % | Onde está | Próximo passo |
|---|---|---|---|
| Core polido | 74 | loop completo, Save 2.0 schema 3 por ID, itens/receitas em dados, exploits fechados, ouro long, validator, `Tuning`/flags, RemoteConfig com travas; feedback do POCO resolvido no código (#59, #62); regra de transporte de insumos para distritos com 2+ insumos (#76); replay determinístico (mesmos toques = mesma partida, #99); impasse do `Steer` e recarga de 5 min do VIP fechados (§7) | ouvir e ver no POCO; `Sim` em partes |
| 6 distritos | 82 | os 6 no build: Old Forge; Rua dos Armeiros (#66, #71, #76); Artisan Quarter (#78) e Royal Arsenal (#83) com arte própria (#75, #79, #84); **Arcane Foundry** com o medidor de carga dos altares (#97) e **Grand Forge** com as rotas e os lotes do Mensageiro (#98), a Foundry ainda com arte emprestada e a Grand Forge com as 10 estações próprias (#102); managers e Império (#65) | arte das estações da Foundry (Tripo); importados da Foundry na Grand Forge e a Carta (`BALANCE.md` §41, desvios 1 e 16) |
| 35–45 produtos | 85 | 38 de 38 vendáveis do catálogo em jogo: Old Forge 5, Row 7, Quarter 7, Arsenal 6, Foundry 7 (#97), Grand Forge 6 (#98) | ícones próprios dos insumos novos (cristal bruto, lingote rúnico; os da Grand Forge) |
| Workers | 86 | Old Forge (13), Ferraz e Vulcana (Row), Dourado e Lúcia (Quarter), Cadetes e Matias (Arsenal), Aprendizes e Ísis (Foundry), Estrela e Félix, o Mensageiro (Grand Forge); managers 2–5 dos 6 (Sibila e Aurélio) | arte da Vulcana e do herói; Félix com folha própria |
| Quality | 92 | 4 níveis, níveis II–V, golpe, técnica, oficiais, Masterwork; liga na Row, lapidação no Quarter (#78), peça energizada na Foundry (#97) e bênção da forja na Grand Forge (#98) | conferir com jogador real |
| Orders | 86 | Orders 2.0 (#60); encomendas dos 6 distritos (Amara, Tibério, Zélia, Kael; a Royal nunca vira Common) | V27 (Event e vagas) |
| Royal Contracts | 84 | os 12 em dados abrem no build (os da Foundry e da Grand Forge entram com elas); 2ª vaga pela `mesa_real`; Charters do Arsenal, da Foundry e a Carta aceitam contrato ou Royal | contratos que andam no bot (hoje a lista oferece o 1º não concluído; §41 achado 15) |
| Blueprints | 88 | 19 do Old Forge + 14 por distrito nos outros 5 (#83, #97, #98) | conferir a fila com jogador real |
| Masterworks | 86 | 60 de 60 nos 6 distritos, Hall por distrito; condições novas de peças energizadas, sequência estável, importados, lotes e Charters | bot F2P que golpeia (portão "1ª em até 4 h") |
| Collection | 86 | marcos dos 6 distritos | conferir com jogador real |
| Reputation | 83 | Renome 10 níveis do Império (todo distrito conta, #65); o 8 e o 9 abrem o conteúdo da Foundry e da Grand Forge (Renome 9 em 4.200, desvio); F2P chega ao 2/3/4 em 0:51 / 1:41 / 3:11 (`BALANCE.md` §30.6) | calibrar 5–10 com a Carta (§41 achado 16) |
| Daily / Weekly / Achievements | 95 | 32 diárias, 17 semanais, 106 conquistas, login de 7 dias; derivadas do estado com retroativo (#70); conquistas de 3/6 distritos e 30/60 obras (#100) | pool C das diárias |
| Events (LiveOps) | 82 | 10 `EventDef`, os 10 rodando com o Rare Ore Rush (#100); pontos de todo distrito (#65); missão de contrato na Royal Week (#70) | V27 (Event disputa vagas); `event_enter`/`event_complete` na telemetria |
| Pass | 50 | 30 níveis × 600 XP, temporada de 35 dias, trilha free com RESGATAR; premium no dado, desligado (#56) | premium na Wave E (preço = decisão) |
| Ads | 70 | 6 placements por config, teto diário, ID de transação, intersticial atrás de flag, `ad_impression` (#52) | conta da V-STACK com VIP e Velocidade (#67); ad units dos outros 4 placements e Unity Ads no painel (Vinicius); A/B do intersticial no Limited Release |
| IAP | 35 | núcleo com estados e recibos, loja falsa, Pacote do Mestre, restaurar compras (#58) | pacote Unity IAP (aval de download) |
| Analytics | 45 | `Telemetria` com fila até o consentimento, esquema v1, exceções (#53) | provedor remoto (P8) |
| RemoteConfig | 47 | JSON local com travas no núcleo, chave mestra dos anúncios, cache, HTTP desligado (#53); trava do `QualidadeEvento` (#63) | provedor remoto (P8) |
| Save | 92 | Save 2.0 + schema 3; multi-distrito; linhas opcionais da bancada, das famílias, das cotas, do medidor dos altares e das rotas e lotes (#78, #83, #97, #98) | cloud save (opcional) |
| Release pipeline | 55 | AAB assinado por env, `verify release`, 16 KB, API 36 | chave real (aval), Unity no CI, release automatizada |
| Arte final | 72 | estações do Old Forge, da Row (#75), do Quarter (#79) e do Arsenal (#84), ícones de insumo (#79), elenco 1,5× mais nítido (#81); **programa de arte** (`AUDITORIA_VISUAL.md`): interface v2 com fontes OFL, molduras, botões com relevo, barras, fundo dos menus e ícones de prêmio pintados (#87), cofre e baú com vida (#88) | Vulcana e herói; HUD de cima e barra de baixo no estilo novo, fita nos títulos, selos de estado (itens 10–13 da auditoria); chão menos repetitivo |
| Áudio final | 38 | 3 grupos, 15 efeitos sintetizados, mix com prioridade, orçamento de vozes e distância (#59) | ouvir no POCO; banco de sons real + 5 músicas |
| Store assets | 62 | marca v2 (#86): ícone A3 (adaptativo, legado, redondo, 512 da loja), logo C2 na abertura e na feature graphic; **8 screenshots × pt/en/es** com a interface v2 (#92); 37 criativos, ASO pt/en/es | vídeo; 2ª sequência de screenshots e teste de ícones (G-UA-03, na Play Console) |
| **Média** | **~73** | | |

## 9. Retomada (atualizada em 2026-10-21)

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

**CONTINUAR DAQUI (2026-10-21).** Na `main` desde a pausa de 2026-10-16: #93 (docs da pausa), #94 (mais 3 mutações antigas voltam a morrer), #97 **Arcane Foundry** (distrito 5, índice 4: medidor de carga dos altares; 45/45 mutações `af-`) e #98 **Grand Forge** (distrito 6, índice 5: rotas e lotes do Mensageiro; Foundry → Grand Forge; 87/87 mutações `af-` e `gf-` remedidas depois do merge da Foundry). Os 6 distritos estão no build; distritos 1–5 ao byte; placar ~72%. Próximos, nesta ordem:
1. **Grand Forge: importados da Foundry e a Carta** (`BALANCE.md` §41, desvios 1 e 16): pôr a lanterna arcana, o anel de cristal e a lâmina e o escudo rúnicos nas rotas e nas receitas (lâmina estelar, relicário, coroa e panóplia; `CONTEUDO_V1.md` §3.1) e recalibrar a Carta do Ideal (hoje 3 de 8 em 480 min porque o bot sobe a Mestra Sibila; com `--gerente 0`, 6 de 8).
2. **Arte das estações da Foundry (Tripo):** a Grand Forge ganhou as 10 dela no #102 (450 créditos, `ASSETS.md` §19, `PROVENIENCIA.md` §17). Falta a Foundry, que ainda não tem imagens: gerar no Nano Banana 2 (grátis) e os modelos HD a 45 cada (aval permanente do Vinicius de 2026-10-23 para os créditos existentes; comprar crédito continua pedindo aval). Os downloads do Chrome caem em `Downloads`; um export às vezes diz "exported successfully" sem baixar: conferir o zip. Paleta do distrito na ART_BIBLE.
3. Leva de arte da HUD (sobras da auditoria: HUD de cima, barra de baixo, fita nos títulos, selos de estado, rótulos do mundo, chão).
4. Áudio real (decidir a fonte), Store (vídeo), IAP (aval de download); POCO só com "pode testar".

**Leva dos distritos 5 e 6 (concluída em 2026-10-21: #97 e #98):** raias `feat/arcane-foundry` (C-CORE-08, `Sim.Arcano.cs`, BALANCE §40, mutações af-) e `feat/grand-forge` (C-CORE-09, `Sim.Exportacao.cs`, BALANCE §41, mutações gf-), brief no scratchpad do coordenador com as lições da leva 3–4 (transporte, Charter cedo, chaves de texto duplicadas, merge da `main` no fim). Integrar a Foundry primeiro (índice 4) e a Grand Forge depois (índice 5); depois a arte das estações deles no Tripo. Fora delas: 4 mutações antigas quebradas no `mutantes.py` (`enc-rodizio-repete-produto`, `enc-validator-item`, `ad-inter-tutorial`, `liveops-limiar-2x`) num PR à parte.

**Fila das próximas levas (ordem):**
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
