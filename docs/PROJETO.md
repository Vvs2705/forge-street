# PROJETO — Forge Street (fonte única de verdade)

Atualizado: 2026-10-09 · Dono: Vinicius · Direção: [`DOCUMENTO_MESTRE_PRODUCAO_V1.md`](DOCUMENTO_MESTRE_PRODUCAO_V1.md) (prevalece em conflito com documentos antigos, §186) · Mapa e tickets: [`PRODUCAO_V1_MAPA_TICKETS.md`](PRODUCAO_V1_MAPA_TICKETS.md).

> O `LEIA_PRIMEIRO_PRODUCAO_V1.md` cita `FORGE_STREET_AUDITORIA_COMPLETA_2026-10-09_v1_0.md`, que não foi encontrada no PC. Por decisão do Vinicius (2026-10-09), o mapeamento seguiu sem ela; o lugar dela é ocupado pelo mapa da Leva 1 (4 raias, só leitura).

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
- **Anúncios:** LevelPlay 9.5.1 com 2 rewarded (Chamar VIP, Velocidade); conta aguardando aprovação; build de teste usa anúncio simulado. Consentimento fixo para todos (COPPA/GDPR false, CCPA true, `Ads.cs:200-204`): bloqueia release comercial.
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
| V8 | Crédito de IA, download de arquivo, merge, tag e Release só com aval do Vinicius | `CLAUDE.md` |

## 5. Decisões pendentes (Vinicius)

| # | Decisão | Recomendação do time | Bloqueia |
|---|---|---|---|
| P1 | "1 moeda no MVP" × Gold/Gems/Blueprint Tokens/Event Currency (§12) | Carteira com IDs de moeda já na Wave A, só ouro ativo até a Wave E; Blueprint como desbloqueio, Reputation não é moeda; cada moeda entra com mapa fonte → ralo no `BALANCE.md` | B, D, E |
| P2 | Teto offline 2 h × teto ampliável por premium/assinatura (§27, §43) | 2 h como trava no núcleo; managers e premium aumentam a **taxa**, não o tempo; cofre 2× por anúncio dobra o mesmo resgate uma vez | C, E |
| P3 | Interstitial autorizado (§41) × "zero interstitial" do FS-5 e da `CLAUDE.md` | Implementar atrás de feature flag **desligada**; decidir por A/B no Limited Release | E, ASO |
| P4 | "Release 0.6.0 só depois do POCO" × POCO só em marcos | Release `v0.6.0` como pre-release com a evidência do emulador | versões |
| P5 | Rerolls com Gems, "chest; cosmetic chance", offline chest (§12, §19, §40, §82) × nada aleatório pago | Baú com conteúdo fixo mostrado antes; reroll mostra a próxima opção antes de pagar; tirar "chance"; teste negativo no núcleo | B, D, E |
| P6 | Metas de retenção: GDD 26/5 × DM 28/7 (agressivo 32/9) | 26/5 = continuar; 28/7 e 32/9 = metas internas | dashboards |
| P7 | "Não ampliar conteúdo antes de medir com gente" (`BACKLOG_V07.md`) × Waves B/C | Waves seguem; medição roda em paralelo (Closed testers + bot lento) e não bloqueia | B, C |
| P8 | Provedores de analytics / crash / RemoteConfig / attribution e custo | Escolher na Wave A; tudo atrás de interface com mock | A |
| P9 | Conta do Play Console e testadores (Closed exige 12 por 14 dias em conta pessoal) | Abrir o Internal já na Wave A | G |
| P10 | Regra da obra-prima (FS-2: vendida a 3×) × Masterwork não vendida (§16) | Só a mão do ferreiro chega ao tier Masterwork; Masterwork vai para o Hall | B |
| P11 | Downloads: fonte OFL (D8), clipes Mixamo, banco de sons; créditos Tripo por lote de Wave | Aprovar por pacote de Wave | A, B, F |
| P12 | Câmera (D2): oficina inteira 11,4 m × 9 m | Decidir pelo alvo visual 9:16 (ticket A-ART-01) | A |
| P13 | Verba e conta para o Gate M (teste de criativos) e a regra "Rune Relay vai primeiro" | — | G |
| P14 | "Chamar VIP" por anúncio sem recarga: quem assiste todo anúncio fecha a produção em 24 min × 43 do F2P (`BALANCE.md` §22) | Recarga no VIP chamado (ex.: 5 min, como a Velocidade) ou teto por hora, como knob do RemoteConfig | A, E |
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
| "Chamar VIP" sem recarga fura os pisos do §3 | P1 (exploit de economia) | `BALANCE.md` §22 | decisão P14 |
| Impasse de navegação: 1 em 1.000 partidas o `Sim.Steer` alterna entre duas direções a caminho do pad do Mineiro (bot Ideal a 36,36 ticks/s); conferir se ajudantes podem cair nele | P1 se pegar ajudante (progressão travada) | `BALANCE.md` §22 | ticket de núcleo (Wave A) |
| Vibra em toda venda (§72 pede só eventos importantes) | P2 | `Game.cs:262` | PR #19 |
| Sem AAB, chave de release, `versionCode` monotônico nem build não-development; targetSdk automático | antes da loja | `ProjectSettings.asset:181,183`, `Setup.cs:97` | PR #18 (falta a chave real: decisão) |
| viewcheck não compila o caminho `#if UNITY_ANDROID` e só roda com o Unity local | — | `client/tools/viewcheck` | PR #13 (viewcheck Android); Unity no CI = P15 |
| `dotnet test` (NUnit 3.14) aceita tupla `int` × `long`; o EditMode do Unity (NUnit 3.5) não | portão | `FSCore.Tests.csproj` | regra: `verify.sh full` antes de todo PR de jogo |
| 0.6.0 sem aparelho físico | — | `VALIDACAO_V06A.md` | gate de Feature Complete (POCO só em marcos) |
| Diário CSV grava também no release e cresce sem limite | P2 | `Game.cs:1070` | A-PLAT-06 |
| Sem analytics remoto, crash reporting e RemoteConfig | antes de UA | — | A-PLAT-08/09 (decisão P8) |
| Textos no código, sem localização | antes de en/es | DM §109 | — |
| Bandeja mostra o cartão travado primeiro; "Ajudantes ágeis" fora do padrão "Ajudantes rápidos" | P2 | `VALIDACAO_V05.md` | — |
| Fome da Bigorna 2 (2.582 s em 60 min no bot) | P2 | `BACKLOG_V07.md` | — |
| Zona morta ~53% da curva até a produção completa; ouro sem destino depois dela | design | `BALANCE.md` §22 | Waves B (Orders 2.0, Blueprints) e C (distritos) |
| Conta LevelPlay aguardando aprovação | — | `LEVELPLAY.md` | Vinicius |
