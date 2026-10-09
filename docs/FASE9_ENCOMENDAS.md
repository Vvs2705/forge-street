# FASE9: Encomendas (missões curtas)

**Origem:** `BENCHMARK_MERCADO.md` B6 ("missões curtas: direção de sessão e motivo para voltar"), GDD §8 ("pedido do dia, bônus moderado") e §9 ("Royal Contract"). Versão preguiçosa: **uma encomenda por vez**, um tipo só.

## 1. Regra
- **Encomenda** = vender N de uma linha aberta (espada, escudo, ferramenta, joia), em rodízio pela contagem de entregues (`OrderCount % linhas abertas`). Só conta venda feita **depois** que ela apareceu (cliente normal ou unidade de VIP).
- A 1ª é **5 espadas**, `OrderFirstDelay` = 45 s depois da 1ª venda (o relógio não anda antes dela). Entregue, a próxima vem `OrderGap` = 20 s depois.
- **Sem prazo e sem falha** (jogo tranquilo): a encomenda espera o jogador.
- Sem compras pelo offline (o cofre não vende nada), então o offline não mexe na encomenda.

## 2. Números (medidos em `BALANCE.md` §20)
- N a partir da 2ª: `OrderSeconds / ClientInterval(linha)` = ~150 s de clientes daquela linha, entre `OrderMin` 3 e `OrderMax` 30.
- Prêmio: `OrderRewardSeconds` = 10 s da taxa online (`RateEma`), mínimo 25, múltiplo de 5. Pago **fora do `GoldEarned`**, como o baú.
- Bot humano 60 min: 16 entregues, prêmio = 4,2% da receita, uma a cada 3,7 min; produção completa 46:12 → 42:51.

## 3. Save
`ord=item,alvo,progresso,prêmio,entregues,relógio`. Save antigo sem `ord=` começa do zero; lixo vira o padrão; item de linha fechada não volta (a contagem fica); progresso no teto do alvo.

## 4. Eventos e diário
`Ev.OrderNew` (A = item, B = alvo) e `Ev.OrderDone` (A = item, B = prêmio), anexados ao fim do enum. Diário: `order_new`, `order_done`.

## 5. Fica de fora (ponytail)
- Outros tipos ("comprar uma melhoria", "atender o VIP", "abrir área"): se o playtest pedir.
- Várias encomendas ao mesmo tempo (3 cartões, como o My Perfect Hotel): uma basta para dar direção; mais cartões cobrem a fila.
- Prazo e encomenda "do Rei" com prêmio grande: evento de live ops (GDD §9), depois.
- Pular encomenda por anúncio.
