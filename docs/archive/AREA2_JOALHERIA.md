# Forge Street — 2ª área: corredor lateral + joalheria (contrato v0.2, 2026-10-07)

Fonte: GDD §3 (6:30 "abre corredor lateral; rua começa a aparecer", 9:30 "teaser da loja de joalheria fechada"), §5 (D3 joalheria), §20 (v0.2 = segunda área). Arte pronta (Lote 3): `nobre`, `bancada_joalheria`, `loja_joalheria`, `arco`, `carroca` (sprites em `client/Assets/_FS/Resources/Sprites/<nome>/`).

## 1. Mundo
- `Balance.WorkshopW = 9f` (oficina de hoje, x 0–9) e `Balance.WorldW = 15f` (mapa inteiro; x 9–15 = rua lateral). `WorldH = 14f` não muda.
- Antes de comprar o Corredor o jogador fica preso em x ≤ `WorkshopW − 0,3` (como hoje). Depois, em x ≤ `WorldW − 0,3`.
- A câmera continua mostrando `WorkshopW + 2·Margin` de largura (11,4 m). Antes do Corredor: parada no centro da oficina (igual a hoje). Depois: segue o X do jogador, presa ao mapa inteiro com `Margin` de folga.

## 2. Upgrades novos (no FIM do enum: saves antigos com 15 flags continuam válidos)
| enum | tier | custo | requer | nome | desc |
|---|---|---|---|---|---|
| `SideCorridor` | 15 | **690** | — | "Corredor" | "Abre a rua lateral" |
| `Jewelry` | 16 | **1 515** | `SideCorridor` | "Joalheria" | "Nova linha: 2 lingotes, paga 60" |
`Upgrades.Count = 17`.

Custos (decisão do coordenador, 2026-10-07; antes eram 2 560 / 3 325 pela fórmula): os dois ficam no fim do enum por causa do save, mas o preço vem de `Balance.SideCorridorCost`/`Balance.JewelryCost`, fora da fórmula por tier (override em `Upgrades.Cost(int tier)`). `Sim.CheapestLockedCost` (teto do cofre offline) pega o MENOR custo entre os não comprados. Medição em `docs/BALANCE.md` §9.

## 3. Item e linha
- `Item.Jewel = 5` (anel; matiz roxo `#B07CF2`, ART_BIBLE). `Balance.Price[5] = 60`, `IngotsPer[5] = 2`, `ItemName[5] = "joia"`, `ClientInterval[5] = 14f` (× Fama × Vitrine, como as outras linhas).
- `Stock` passa a ter 6 posições. Teto de estoque da joia = `CounterCap` (o mesmo das outras).
- Tempo da bancada de joias = tempo da bigorna × `Balance.JewelTimeMul = 2f`.

## 4. Estações e pads (ANEXADOS ao fim das listas: índices antigos e o save não mudam)
| campo `Sim` | Kind | pos | desbloqueio | nome |
|---|---|---|---|---|
| `JewelBench` | Crafter (Ingot ×2 → Jewel) | (12,0; 6,5) | `Jewelry` | "Joalheria" |
| `JewelShop` | Counter (só vende Jewel) | (12,0; 11,5) | `Jewelry` | "Loja de joias" |
Pads novos: `SideCorridor` em (8,4; 11,6) (lado direito da oficina, fora das linhas de caminhada); `Jewelry` em (12,0; 6,5) (sobre a bancada, como Escudos/Ferramentas).

## 5. Balcões e filas
- `Sim.Sells(Station counter, Item i)`: o `Counter` de hoje vende Sword/Shield/Tool; o `JewelShop` vende só Jewel. Tudo que hoje assume "o balcão" (Action, IsDestFor/PickDest do ajudante papel 2, Bot.Target, Need, dica) passa a usar "o balcão que vende este item".
- `Queue` continua sendo a fila do balcão principal. Nova `JewelQueue` (List<Client>) com `Balance.JewelQueueCap = 3`, paciência `Balance.JewelPatience = 40f`, slots `JewelSlot(i) = (JewelShop.X + 0,85·i, JewelShop.Y + 1,0)`, timer de atendimento próprio (`ServeTime`).
- Eventos de cliente da joalheria usam os mesmos `Ev` (ClientArrived/Sold/ClientLeft) com `A = (int)Item.Jewel`; a posição é o `JewelSlot`.

## 6. Save
- `up=` com 17 caracteres; `stock=` com 4 números (espada, escudo, ferramenta, joia); linha `st8` para a bancada de joias (não existe `st9`: a loja de joias, estação 9, é `Counter` e não tem linha `st`; o estoque dela vai no 4º número do `stock=`); `pads=` com os pads novos no fim. Save antigo (15 flags, 3 estoques) carrega sem erro com a 2ª área fechada.

## 7. View
- Chão da rua lateral (x 9–15) com cor própria (pedra fria da ART_BIBLE §2). Antes do Corredor: rua escurecida + rótulo "Rua lateral" e o `arco` em (9,0; 12,6) visível na borda (teaser). Depois do Corredor e antes da Joalheria: `loja_joalheria` aparece escurecida com rótulo "Joalheria — fechada" (teaser §3 9:30). Depois: bancada e loja normais.
- Decoração estática: `arco` (entrada do corredor), `carroca` em (14,2; 3,0).
- Clientes da `JewelQueue` usam o sprite `nobre` (mesma máquina de estados dos clientes de hoje).
- Item Jewel: forma própria (anel/gema) na cor `#B07CF2` em `Art.ItemSprite`/`ItemColor`.
- Dica: carregando joia → "Leve joias à loja de joias".
