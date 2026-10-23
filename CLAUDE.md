# Instruções para o Claude — Forge Street

Este repositório é **só do Forge Street** (GitHub `Vvs2705/forge-street`). Cada chat aberto aqui cuida **apenas deste jogo**.

Regras comuns dos jogos do estúdio (escopo, git, guardrails, processo, arte, testes), editadas num lugar só:
@~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md

Se o arquivo acima não carregar (outro PC), o mínimo é: só este jogo; mudança entra por PR na `main`; nada de item aleatório pago; anúncio sem `AD_ID`; um processo pesado por vez.

## Deste jogo
- Código do jogo em `client/Assets/_FS/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity).
- Git: trabalho em branch (`feat/`, `fix/`, `docs/`, `art/`), commits em português com escopo (ver `CONTRIBUTING.md`), entra por Pull Request na `main`. **Regra (Vinicius, 2026-10-12): commit, push, PR e merge estão sempre autorizados — não pergunte, faça**, depois que o PR passar no `client/tools/verify.sh full` (a `main` só aceita mudança por PR: crie o PR e faça o merge você mesmo). Continuam só com aval dele neste chat: tags e Releases, publicação em loja, crédito de IA, download externo, criação de conta, pagamento e chave de assinatura real.
- Guardrails do estúdio: só o ouro ativo até a Wave E (carteira com IDs de moeda; Blueprint é desbloqueio, não token; Gems entram na Wave E com mapa fonte → ralo no `BALANCE.md`) — decisão P1 de 2026-10-12; offline com teto de 2 h (managers e premium mexem na taxa, não no tempo); nada de item aleatório pago (ECA Digital, Lei 15.211/2025: baú com conteúdo fixo e visível, reroll mostra a próxima opção); interstitial só atrás de feature flag **desligada** por padrão, ligada depois de A/B no Limited Release (P3); "Chamar VIP" com recarga de 5 min (P14); criativos gravados do jogo real; créditos de IA (Tripo etc.) só com aval explícito.

## Situação
- Estado: 0.5.1 lançada (Release `v0.5.1`); `main` com a Wave A, itens/receitas em dados, qualidade e golpe de mestre, Renome, metas diárias/conquistas, localização pt/en/es, Orders 2.0, Blueprints, Masterworks/Collection, Royal Contracts, eventos e Pass, IAP no núcleo, os 6 distritos com Império (Old Forge, Rua dos Armeiros, Artisan Quarter, Royal Arsenal, Arcane Foundry e Grand Forge) e o catálogo `CONTEUDO_V1.md`. Meta do Vinicius (2026-10-13): GDD ≥ 90% antes de POCO e loja (placar em `docs/PROJETO.md` §8, ~72% depois do #98). **Retomada de chat: `docs/PROJETO.md` §9** (branches pendentes, fila de levas, como o coordenador trabalha). POCO só no fim.
- Comece por: `docs/PROJETO.md` (fonte única: estado, decisões, known issues, placar §8), `docs/GDD.md` (GDD v2 — a régua do 100%, §23 = definição de pronto), `docs/ECONOMIA_WAVE_B.md` (números da Wave B), `docs/CONTEUDO_V1.md` (catálogo da v1.0: distritos, itens, receitas, workers, encomendas, contratos, Blueprints, Masterworks; `client/tools/conteudo_check.py`), `docs/DOCUMENTO_MESTRE_PRODUCAO_V1.md` (direção comercial, Waves A→I), `docs/PRODUCAO_V1_MAPA_TICKETS.md` (tickets); histórico: `docs/archive/GDD_v1_MVP.md`, `README.md` (seção Estado), `docs/BACKLOG_V07.md`, `docs/COMPETITIVO.md`, `docs/VALIDACAO_V05.md` e `docs/VALIDACAO_V06A.md`.
- Portões antes de qualquer PR de jogo: `client/tools/verify.sh full` (núcleo + viewcheck PC/Android + **EditMode no Unity** + BuildWindows + `-autoplay`; o `dotnet test` sozinho não pega a comparação de tupla int × long do NUnit 3.5 do Unity); economia mexida = números do bot em `docs/BALANCE.md` (`client/tools/econsim`). Worktree nova não tem `client/Library`: copie `Unity.InputSystem.dll`, `UnityEngine.UI.dll` e `Unity.LevelPlay.dll` de `client/Library/ScriptAssemblies` da pasta principal.
- Build Android: use `Setup.BuildAndroidDev`/`BuildAndroidEmu` (já limpam a saída incremental do Gradle e tratam o `mainTemplate.gradle`). Aparelho: `client/tools/medir_aparelho.sh`; roteiro em `docs/ROTEIRO_TESTE_POCO.md`.
- A `main` tem a regra "mudanças só por pull request": nunca dê push direto nela.
