# Forge Street — fase 4: Mineiro + 2º Joalheiro (contrato, Leva 10, 2026-10-07)

## Por quê
`ESTUDO_LOGISTICA.md` §5 (A/B em 4 bases, estado clonado na produção completa): a joalheria passa fome por falta de lingote na fonte E, resolvida a fonte, por transporte até a rua. Sozinho, o 2º ajudante de minério falha na fome (39%); o PAR "ajudante de minério + 2º joalheiro" passou nos 4 critérios nas 4 bases com folga (fome 17–20%, joias/min +77–93%, ouro/min +48–53%, outras vendas +3–8%). Decisão do coordenador: implementar o PAR, em sequência (o 2º Joalheiro exige o Mineiro).

## 1. Upgrades novos (PRODUTIVOS, anexados ao fim do enum depois de `JewelryDecor`; marcados produtivos pela marca `Luxury = false`)
| ID | enum | nome | desc | requer | pad | preço |
|---|---|---|---|---|---|---|
| 23 | `Miner` | "Mineiro" | "Mais um ajudante leva minério às fornalhas" | `Jeweler` | (0,5; 3,5) — parede esquerda entre Depósito e Fornalha (pré-avaliado na Leva 9) | medir: faixa da regra de retorno 8–12 min ≈ 2 400–4 400 |
| 24 | `Jeweler2` | "Joalheiro 2" | "Mais um ajudante na joalheria" | `Miner` | escolher na rua lateral, livre de pads/estações/fila/baú/decoração da Joalheria real (documentar) | medir: ≈ 1 750–3 100 |
- `Mineiro` = +1 ajudante papel 0 (mesma velocidade/carga, HelperSpeed vale). `Joalheiro 2` = +1 ajudante papel 3.
- Luxo continua exigindo TODOS os produtivos (agora 22).

## 2. Alvos de medição (bot humano)
- Fim da produção (22 produtivos) entre **42 e 48 min**; primeiros 10 min intactos (§3 do GDD); teto offline coerente (2× o produtivo mais caro com tudo comprado).
- Remedir o luxo (§11): com ~+50% de ouro/min os intervalos encurtam; se a Fachada sair < 8 min depois da produção completa, PROPOR novos preços de luxo (não aplicar sem decisão).

## 3. Ajustes que entram junto
- Pad `HammerSpeed` (Martelo veloz) sai de (0,5; 9,5): o rótulo encosta no rótulo da Bigorna no aparelho (`VALIDACAO_APARELHO.md`). Nova posição livre e fora das linhas de caminhada (sugestão: (8,4; 7,5), entre Ferramentas e Escudos, x + 0,9); documentar e conferir que os primeiros 10 min não mudam.
- `Game.DevArgs -buy N`: comprar os produtivos primeiro e os luxos depois (hoje compra na ordem do enum e o luxo recusa antes de os produtivos 23/24 existirem).

## 4. Testes
Mineiro cria ajudante papel 0 extra; Joalheiro 2 cria papel 3 extra; requisitos; luxo espera os 22 produtivos; save antigo (23 flags) carrega; bot 60/90 min com limites medido + 30%; prova vermelha em cópia.
