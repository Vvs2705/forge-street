# Instruções para o Claude — Forge Street

Este repositório é **só do Forge Street** (GitHub `Vvs2705/forge-street`). Cada chat aberto aqui cuida **apenas deste jogo**.

## Escopo
- Leia e altere apenas arquivos deste repositório. Não mexa nos outros jogos do estúdio, na pasta raiz `JOGOS NOVOS` nem em outros repositórios, a não ser que o Vinicius peça explicitamente neste chat.
- Se o pedido for sobre outro jogo, avise e sugira abrir o chat no grupo daquele jogo no menu lateral.
- Os documentos deste repositório descrevem só o Forge Street (nada de portfólio com os outros jogos).

## Como trabalhar
- Português brasileiro. Código do jogo em `client/Assets/_FS/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity).
- Git: trabalho em branch (`feat/`, `fix/`, `docs/`, `art/`), commits em português com escopo (ver `CONTRIBUTING.md`), entra por Pull Request na `main`. Merge, push na `main`, tags e Releases só com aval do Vinicius neste chat.
- PC com 7,7 GB de RAM: um processo pesado por vez (Unity **ou** emulador **ou** Blender).
- Guardrails do estúdio: 1 moeda no MVP; offline com teto de 2 h; nada de item aleatório pago (ECA Digital, Lei 15.211/2025); interstitial desligado na v0.1; criativos gravados do jogo real; créditos de IA (Tripo etc.) só com aval explícito.
- Memória do estúdio (leitura; atualize só a parte deste jogo): `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\` — `STATUS_jogos-novos-validacao.md` e, se existir, o arquivo deste projeto.

## Situação
- Estado: 0.5.1 lançada (Release `v0.5.1`, testada no POCO F4 a 60 fps); 0.6.0 na `main` — Release só depois do teste no POCO.
- Comece por: `docs/PROJETO.md` (fonte única: estado, decisões, known issues), `docs/DOCUMENTO_MESTRE_PRODUCAO_V1.md` (direção comercial, Waves A→I), `docs/PRODUCAO_V1_MAPA_TICKETS.md` (mapa e tickets); histórico: `README.md` (seção Estado), `docs/BACKLOG_V07.md` (decisões D1–D9), `docs/GDD.md`, `docs/COMPETITIVO.md`, `docs/VALIDACAO_V05.md` (teste no POCO) e `docs/VALIDACAO_V06A.md`.
- Portões antes de qualquer PR de jogo: `dotnet test client/tools/coretests`, `dotnet build client/tools/viewcheck/view` (0 erros), build Windows + `-autoplay`; economia mexida = números do bot em `docs/BALANCE.md`.
- Build Android: use `Setup.BuildAndroidDev`/`BuildAndroidEmu` (já limpam a saída incremental do Gradle e tratam o `mainTemplate.gradle`). Aparelho: `client/tools/medir_aparelho.sh`; roteiro em `docs/ROTEIRO_TESTE_POCO.md`.
- A `main` tem a regra "mudanças só por pull request": nunca dê push direto nela.
