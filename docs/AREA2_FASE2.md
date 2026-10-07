# Forge Street — 2ª área, fase 2: joalheiro, upgrades da joalheria e baús de marco (contrato, 2026-10-07)

Motivo (BALANCE.md §9.4, bot humano com a v0.2): tudo comprado aos 28:24 e ouro sobrando sem destino; bancada de joias 36% do tempo sem lingote (fica a ~10 m das fornalhas); GDD §3 7:30 pede "milestone 10 espadas; baú visual com decoração" e o sprite `bau` já existe (`client/Builds/sprites_area2/bau`, o coordenador move para `Resources/Sprites/bau`).

## 1. Upgrades novos (ANEXADOS ao fim do enum, depois de `Jewelry`; `Upgrades.Count = 20`)
| enum | requer | nome | desc | custo (override, como Corredor/Joalheria) |
|---|---|---|---|---|
| `Jeweler` | `Jewelry` | "Joalheiro" | "Ajudante leva lingotes à joalheria e joias à loja" | 2 200 |
| `JewelSpeed` | `Jewelry` | "Lupa" | "Joalheria mais rápida" | 5 500 |
| `JewelVitrine` | `Jewelry` | "Vitrine de joias" | "Nobres pagam 80 e a fila cresce" | 9 000 |
Custos vigentes aprovados no handoff (§3/§6A) e aplicados em 2026-10-07. Bot humano: tudo comprado aos 40:41; medição antes/depois e histórico em BALANCE.md §10.7. Mudanças futuras de custo exigem nova medição e decisão do coordenador.

## 2. Regras
- `Jeweler`: ajudante novo com papel 3 (`Carrier.Role = 3`), sai do `HireSpot` ou de um ponto na rua lateral; busca lingote em qualquer fornalha e leva SÓ à `JewelBench`; quando a bancada tem joia pronta e ele está vazio, leva joia à `JewelShop`. Usa velocidade/carga de ajudante (HelperSpeed vale para ele).
- `JewelSpeed`: tempo da bancada de joias × 0,6.
- `JewelVitrine`: `JewelQueueCap` 3 → 5, intervalo dos nobres × 0,7 e preço da joia 60 → 80 (`Balance.JewelPriceUp`), tanto por fila como por compra direta.
- Pads: `Jeweler` em (14,2; 8,5), `JewelSpeed` em (10,2; 6,5), `JewelVitrine` em (14,2; 11,5) (todos na rua lateral, fora da linha bancada→loja x≈12).

## 3. Baús de marco (milestones)
- Lista fixa em `Balance` (ordem = ordem de exibição): (a) 15 espadas vendidas → 60 ouro (original: 10; desvio §6.4); (b) 50 vendas totais → 250; (c) 10 joias vendidas → 600; (d) 200 vendas totais → 1 200.
- Ao bater o marco, `Sim` emite `Ev.Milestone` (A = índice do marco, B = ouro) e o baú do marco fica "disponível" numa posição fixa (oficina: (2,8; 12,6); rua lateral: (10,2; 9,6) para o de joias, desvio §6.2). O jogador pisa no baú → ganha o ouro (uma vez), `Ev.Milestone` com B negativo? NÃO: use um `Ev` novo `ChestOpened` (A = índice, B = ouro). Só um baú disponível por vez na tela por local; o próximo aparece quando o anterior é aberto e o jogador sai do local.
- Save: `ms=` com os marcos batidos/abertos (ex.: "2,1,0,0" = aberto, disponível, não batido). Save antigo sem `ms=` carrega com nenhum marco.
- Bot: abre baú disponível quando passa perto ou quando não tem nada melhor (não precisa ser ótimo).

## 4. View
- Joalheiro: sprite `ajudante` com tinta roxa `#B07CF2` 30% (como os outros papéis).
- Baú: sprite `bau` (estático) com brilho pulsante (escala 1 ↔ 1,08) enquanto disponível; ao abrir, moedas + "+ouro" como venda, e o baú some. Rótulo do marco acima ("15 espadas!").
- Pads novos aparecem como os outros.

## 5. Testes (raia core)
Joalheiro abastece a bancada e leva joia à loja; Lupa muda o tempo; Vitrine de joias muda cap, intervalo e preço (60/80 nos caminhos fila/direta, evento, receita e save); marco dispara uma vez, baú paga uma vez, save/load preserva marcos; save antigo carrega. Bot 60 min: registrar tempos de compra e ouro/min por janela em BALANCE.md §10.7. Passo A: dotnet 37/37 (`client/Builds/validation_phase2/core_final.trx`); prova vermelha na cópia isolada (`core_red.trx`) e restauração verde (`core_restored_verified.trx`). Esta evidência não substitui o portão Unity/aparelho.

## 6. Desvios registrados (2026-10-07, depois da implementação na raia core)

**Decididos pelo coordenador** (a view mediu sobreposição):
1. `Sim.JewelSlot(i)` centra a fila na loja: X = `JewelShop.X + 0,85·(i − 2)`, Y = `JewelShop.Y + 1,0` (i 0..4 = x 10,3..13,7; antes i = 4 caía em 15,4, fora do chão).
2. Baú de joias (`Balance.StreetChest`) sai de (14,2; 13,0) para **(10,2; 9,6)**.
3. Nova dica `Hint.OpenChest` (anexada ao FIM do enum `Hint`), `HintArg` = índice do baú: o 1º disponível na ordem de `Balance.Milestones`; prioridade logo DEPOIS de `BuyPad` (antes de tudo o resto).

**Da raia core** (medidos; nenhum nome do contrato mudou):
4. **Marco (a): 10 → 15 espadas** (rótulo "15 espadas!"). Com 10 o baú abre aos 1:36 e a 2ª bigorna cai para 1:39, abaixo do piso da §3 (1:45) e o `Bot_Primeiros10Minutos_BatemASecao3` fica vermelho; baixar o ouro não resolve. Varredura em BALANCE.md §10.3.
5. **Save ganha também `sold=`** (vendas por item: espada, escudo, ferramenta, joia), além de `ms=`: sem ele os marcos "15 espadas"/"10 joias" recomeçariam do zero a cada load. Save antigo sem `sold=` conta do zero; sem `ms=`, nenhum marco (como o contrato pede) — logo um save antigo com ≥ 50/200 vendas ganha os baús de vendas totais no 1º tick (um por vez).
6. **Baú novo não nasce embaixo do jogador:** o próximo do mesmo lugar só aparece (e só emite `Milestone`) quando o jogador sai de cima; sem isso abria no tick seguinte, sem ser visto.
7. **Ouro de baú não entra em `GoldEarned`** (vai para `Gold`): não infla a taxa online (`RateEma`) nem o cofre offline.
8. Fila de nobres: `Sim.JewelQueueCap` (campo da instância, 3 ou 5) passa a valer na simulação; `Balance.JewelQueueCap = 3` continua como base e entrou `Balance.JewelQueueCapUp = 5`.
9. Joalheiro nasce no `HireSpot` (o `Ev.Hired` sai de lá, como os outros). A lista `Workers` cresce na ordem da compra; o papel vem do upgrade (Ajudante 1/2/3 = 0/1/2, Joalheiro = 3), não da posição na lista.
10. **Custos aprovados e aplicados: 2.200 / 5.500 / 9.000**. Histórico: 2.200 / 2.900 / 3.800 terminava aos 32:20; agora tudo comprado aos 40:41. A proposta de BALANCE.md §10.4 foi aceita no handoff e executada no Passo A; resultados vigentes em §10.7.
11. **Vitrine aumenta o preço da joia de 60 para 80**, além da fila/intervalo: `Balance.JewelPriceUp = 80`; descrição "Nobres pagam 80 e a fila cresce". `Sim.PriceOf` decide pelo upgrade e `Sell` atende ambos os caminhos (fila/direta), atualizando ouro, receita e `Ev.Sold.B` pelo mesmo preço. Outras linhas mantêm seus preços; save rederiva a regra de `up`. A/B com os mesmos custos: ouro/min 55–60 947 → 1.095 (+15,6%); +2.800 ouro em 60 min, 269 joias em ambos.
