# Changelog — Forge Street

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões do Forge Street em [SemVer](https://semver.org/lang/pt-BR/). O histórico anterior ao repositório (2026-10-06 → 2026-10-07) foi reconstruído a partir dos documentos de validação.

## [Unreleased]
### Adicionado
- Criativos de UA 9:16 #1, #2 e #8 (GDD §17) gravados do build real, em `client/Builds/creatives/`, com os comandos em `docs/CRIATIVOS.md`.
- Flags de dev `-record` (quadros 1080×1920 com relógio travado), `-buyids` e `-warmup`.
- `client/tools/diario_report.py`: relatório do playtest Camada 0 (1ª venda, upgrades humano × bot, clientes perdidos, andar sem decisão, travada/fome por minuto, portões do GDD) com `--autoteste`.
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

