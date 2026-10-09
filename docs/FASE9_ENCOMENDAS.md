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

## 6. View (v0.6c)
- **Cartão** no estilo da pílula de ouro (borda `#F2D9A0`, fundo `#2A1E14`): ícone do item (`item_*`), "Encomenda 3/5", barra fina verde e o prêmio com a moeda. Fica no **canto de baixo à esquerda**, acima do botão VIP, espelho do selo do boost. O pedido era sob a pílula de ouro, mas ali ele cobre o balão do 1º da fila com 8 vagas (`validation_v05b/shots/06_fila_cheia_lote4.png`) e, depois do Corredor, as moedas e os corações da rua. Foi o mesmo motivo que levou o selo do boost para baixo.
- Entra pulando (0,3 → 1,1 → 1) quando o Sim tem encomenda (vale para a que volta do save) e pulsa a cada venda que conta. Entregue, mostra o 5/5, cresce e some em 0,35 s.
- **Entregue:** som `upgrade`, aviso "Encomenda entregue! +N" com a moeda (estilo do "Cliente VIP!", logo abaixo dele, então os dois cabem juntos) e moedas voando do cartão até o contador. O número só sobe quando elas chegam, como no baú.
- A fileira de melhorias aberta cobre o cartão, como cobre o selo do boost. No criativo (`-record`) não há cartão nem aviso: o ouro entra direto no número.
- **Diário:** `order_new,item,alvo` e `order_done,item,prêmio`. O `client/tools/diario_report.py` mostra recebidas e entregues por testador e os minutos de jogo por encomenda (t_jogo acumulado / entregues, a conta do bot no `BALANCE.md` §20: 3,7).
- Flag de dev `-shotorder`: a foto espera o aviso da 1ª entrega e sai `-shotdelay` s depois. Validação em `docs/VALIDACAO_V06A.md` §v0.6c.
