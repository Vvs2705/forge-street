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

## 5. Estado — IMPLEMENTADO e TESTADO no core (Leva 10, raia core/economia, 2026-10-07)
Feito **por cima da Etapa A** de `FASE5_FISICA_PACIENCIA.md` (física, bocas, parede, paciência), na ordem pedida pelo coordenador. **NÃO COMPILADO no Unity**: dotnet 59/59 e `viewcheck` com 0 erros. Números e varreduras em [BALANCE.md §14](BALANCE.md).

| ID | enum | preço APLICADO | requer | pad | por quê |
|---|---|---|---|---|---|
| 23 | `Miner` | **3.000** (`Balance.MinerCost`) | `Jeweler` | **(2,8; 0,6)**, slot 20 (v0.4.1; era (0,5; 3,5)) | embaixo do Depósito, no lugar das Botas (pad invisível desde o menu); fora da rota Depósito → entrada da Fornalha |
| 24 | `Jeweler2` | **2.400** (`Balance.Jeweler2Cost`) | `Miner` | **(12; 3,5)**, slot 21 | rua lateral, embaixo da Joalheria; longe das bocas (2,35 m), da porta lateral, da fila, do baú, dos pedestais e dos outros pads |
| 14 | `HammerSpeed` (pad) | — | — | (0,5; 9,5) → **(8,4; 3,4)**, mesmo slot 10 | a sugestão (8,4; 7,5) caiu na linha entre as SAÍDAS de Ferramentas e Escudos criadas pela Etapa A; (8,4; 3,4) fica fora de toda linha boca → boca e a ≥ 1,5 m de qualquer estação |

- **Papéis:** o Mineiro é o 2º papel 0, o Joalheiro 2 é o 2º papel 3 (`Sim.HireRole`). Mesma velocidade e carga dos outros ajudantes, e Ajudantes ágeis vale para os dois.
  - Do save, os ajudantes voltam na ordem `0, 1, 2, 3, 0, 3`. Em jogo, a lista segue a ordem da compra.
- **Luxo:** espera os 22 produtivos.
- **Teto offline:** antes do par, 2 × o menor travado (Joalheiro 2 → 4.800). Com a produção completa, 18.000.
- **`Game.DevArgs -buy N`:** compra primeiro os produtivos entre os N primeiros e depois os luxos. Com `-buy 25` tudo é comprado. Com `-buy 23` (o "tudo" antigo), entram os 20 de antes e o luxo é recusado, porque faltam os IDs 23/24.
- **Save antigo de 23 flags (v0.3):** carrega com o par travado, e a produção reabre. O luxo comprado continua comprado.
  - Correção nesta leva: o pagamento parcial de um pad de luxo que fica escondido **volta para o ouro**. Antes ele era zerado no `Load` e o ouro sumia.

### Evidência (bot humano; detalhes em BALANCE §14)
- **A/B da regra de retorno com física**, janela 60–90 min, Dt 1/30 · 1/60:
  - Mineiro sozinho: **+53 · +58** ouro/min.
  - Par: **+296 · +291**.
  - Fome da joalheria: 36% · 33% → **15% · 16%**. Joias/min +25 a +32%; outras vendas +2 a +12%. O par passa nos 4 critérios.
- **Produção completa (22 produtivos):** **42:24** (Dt 1/30) · **42:16** (Dt 1/60), dentro da janela 42–48.
  - Compras: Joalheiro 26:06 → Mineiro 28:41 → Joalheiro 2 30:57 → Lupa 35:41 → Vitrine de joias 42:24.
- **Ouro/min 55–60:** 1.682 (Etapa A sozinha 1.336). Joias/min depois da produção: 14,3. Bancada de joias sem lingote, Joalheiro → 60 min: **12%**.
- **Primeiros 10 min:** o par não os afeta. O pad novo do Martelo tira o pagamento acidental de 15–43 de ouro e adianta a 2ª bigorna em 17 s (2:14 → 1:57). A §3 do GDD continua verde.
- **Luxo (11/14/18 mil):** Fachada **+6,7 min** depois da produção, abaixo dos 8 min. Pela regra do contrato, novos preços foram propostos e **não aplicados** (abaixo).

### PROPOSTAS NÃO APLICADAS (decisão do coordenador)
1. **Luxo 16.000 / 20.000 / 24.000:**
   - Fachada +9,8 min · Piso +22,1 · Joalheria real +37,1 (79:29).
   - Os intervalos crescem, e um retorno no teto compra um luxo só.
   - Alternativa conservadora: 14.000 / 18.000 / 22.000 (Fachada +8,5).
2. **Mineiro pela regra de retorno remedida, ~500:**
   - Com a física, a faixa do contrato (2.400–4.400) não vale mais para o Mineiro sozinho: a regra dá 420–700.
   - O Joalheiro 2 a 2.400 já está dentro da regra remedida (1.860–2.920).
   - Com o Mineiro a ~500, a produção acaba às **40:01**, fora da janela 42–48. As duas metas do contrato não cabem juntas no Mineiro, e o coordenador escolhe uma.
