# Changelog — Forge Street

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões do Forge Street em [SemVer](https://semver.org/lang/pt-BR/). O histórico anterior ao repositório (2026-10-06 → 2026-10-07) foi reconstruído a partir dos documentos de validação.

## [Não lançado] — em andamento
- **Leva 10:** física (estações e decoração sólidas, interação encostando), duas bocas por estação (entrada só deposita, saída só recolhe), paciência dos clientes proporcional ao tempo de produção; depois o par Mineiro → Joalheiro 2. Origem: playtest do Vinicius no aparelho. Contratos: `docs/FASE5_FISICA_PACIENCIA.md`, `docs/FASE4_MINERIO.md`.
- **Leva 11:** ambientação — paredes, chão texturizado (texturas pintadas geradas no Tripo, grátis), props de cenário modelados no Blender.
- **Próxima:** melhorias do ferreiro e dos itens num menu na barra inferior (sem pisar em pads).

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

