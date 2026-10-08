# Forge Street — carga do ferreiro, balcão evolutivo e venda só com cliente (contrato, 2026-10-08)

Pedidos do Vinicius, jogando a v0.4.1 no POCO F4:
1. "Eu peguei as barras e as máquinas estavam cheias, aí não consegui depois pegar os itens da bigorna para vender, isso trava o jogo. O personagem principal pode pegar item da mesma quantidade de todos os itens sem bloqueio, apenas os NPC precisam ter essa regra."
2. "O balcão pode ser evolutivo, para atender mais cliente: começar com tamanho que possa atender simultânea 4 pessoas e cresce de tamanho para até 8, cada evolução aumentar mais um cliente no balcão."
3. "Aconteceram várias vendas no balcão mesmo sem ter ninguém pedindo nada, aconteceu algum erro no processo."
4. Benchmark (A3, `docs/BENCHMARK_MERCADO.md`): o save não gravava o que estava na mão.

Números: [BALANCE.md §17](BALANCE.md). Estado: **core IMPLEMENTADO e TESTADO** (dotnet 72/72, viewcheck 0 erros/0 avisos); **não compilado no Unity** (portão do coordenador na leva 2).

## 1. Carga do ferreiro sem bloqueio (Etapa A)
- `Carrier.Held[6]`: quantos de cada `Item` na mão. O **ferreiro** leva todos os tipos ao mesmo tempo, até `Player.Cap` **de cada tipo** (3; 6 com a Mochila). Os **ajudantes** continuam com um tipo por vez, só o do papel (`Sim.CanPick`).
- `Carrier.Count` = total na mão; `Carrier.Item` = o mais adiantado na cadeia (joia > ferramenta > escudo > espada > lingote > minério). Para o ajudante é o único tipo.
- Bocas sem mudança de regra: a entrada recebe só o `InItem` dela, a saída entrega o `OutItem` até o teto daquele tipo, o depósito dá minério até o teto de minério e o balcão recebe, um por vez, os produtos que vende. Os tempos por item e as regras de pad não mudam.
- **Dica:** produto que o balcão aceita → "leve ao balcão"; senão, o produto pronto que cabe na mão, começando pelo que a fila pede; depois o insumo na mão. Com lingote na mão e espada pronta, a dica é pegar a espada (destrava a bigorna).
- **Bot:** entrega só o que o balcão aceita (`Sim.Deliverable`); insumo sem destino não o prende mais; recolhe o produto que a fila pede primeiro (`Sim.Wanted`, +12 no placar).

## 2. Balcão evolutivo 4 → 8 (Etapa B)
- **4 evoluções no MENU, anexadas no fim do enum** (save por índice): `Counter5` (25), `Counter6` (26), `Counter7` (27), `Counter8` (28). São produtivas: entram em `ProductionComplete` e o luxo passa a esperar **26** produtivos.
  - Em cadeia: o Balcão 5 exige a **Vitrine** e cada uma das outras exige a anterior.
- **Preços: 150 / 175 / 200 / 225.**
  - Ganho medido de 4 → 8 vagas: +89 ouro/min comprando aos 15 min. Retorno da soma (750): 8,4 min.
  - O requisito da Vitrine põe as vagas onde elas rendem (13:45–14:43 no bot).
  - Sem esse requisito, a preço de regra, elas caíam nos primeiros 5 min (onde rendem 0) e a Esteira saía da janela da §3: 10:23–11:13 contra o limite de 8:30.
- **A Vitrine não mexe mais na fila.** Ficou com estoque 5 → 10 e clientes ×0,7, e a descrição passou a ser "Mais estoque e mais clientes".
- **Geometria:**
  - **Vagas:** `QueueCap` vagas lado a lado em cima do balcão, centradas nele, com 0,85 m entre elas (4 vagas: x 3,2–5,8; 8 vagas: x 1,5–7,5).
  - **Ponto de saída:** a vaga `QueueCap`, logo depois da última, é onde aparece quem chega com a fila cheia e vai embora.
  - **Corpo sólido:** meia-largura `Balance.CounterHalfX(vagas)` = 0,425 × vagas + 0,2 (ponta). Com 4 vagas, x 2,6–6,4; com 8, x 0,9–8,1. A profundidade não muda.
  - **Boca:** continua no meio da face de baixo, em (4,5; 12,35).
  - **Folgas conferidas:**
    - pads (Esteira, Corredor), bocas, baús e rotas bancada → balcão ficam fora do corpo;
    - sobram 0,9 m até a parede do arco.
  - **Baú da oficina:** (2,8; 12,6) → **(2,2; 12,0)**, porque o balcão de 4 vagas o cobria.

## 3. Toda venda tem cliente visível (Etapa C)
- **Causa confirmada:** `Sim.Clients` tinha uma "compra direta". O cliente que chegava com a fila cheia e achava o produto no estoque comprava sem entrar na fila; o "+10" aparecia numa vaga vazia, sem cliente desenhado.
  - No bot da v0.4.1, foram **15–19% das vendas do balcão em 10 min e 28–30% em 45 min**.
  - Descartados: cofre ofline (só o painel; `Ev.Offline` não tem `case` na view), ajudante (`Ev.Deposited` só toca som) e baú (2–3 aberturas no jogo todo).
- **Correção:** a compra direta saiu. Fila cheia = o cliente vai embora (`ClientLeft` B = 2, na vaga `QueueCap`).
- **A trava da §9** (fila cheia de quem quer escudo, estoque cheio de espada) se desfaz de outro jeito:
  - pela paciência por item;
  - por uma vaga a mais;
  - pela carga mista (o ferreiro recolhe o escudo por cima das espadas);
  - e, para o bot, pela regra "não espere num balcão que não aceita o que está na mão".
- **Achado no caminho** (`Sim.Steer`, rotina do bot e dos ajudantes):
  - Na boca da Loja de joias com a Joalheria real, os pedestais deixam vãos de 0,1–0,2 m e o bot ficava preso ali (aos 81 min).
  - Agora o `Steer` considera todos os corpos encostados e descarta rota cuja quina não cabe o personagem (`Sim.Fits`).

## 4. Carga no save
- `hold=` guarda o ferreiro, com 6 números por tipo. `wk=` guarda os ajudantes, na ordem de contratação, separados por `;`.
- No `Load`, cada tipo é cortado no teto; o ajudante fica só com o 1º tipo válido para o papel. Save antigo sem essas chaves volta de mãos vazias, como antes.
- **Teto do cofre ofline:** agora usa o upgrade mais barato **à venda**, isto é, com o pré-requisito comprado. Sem isso o Balcão 5 derrubava o cofre dos 10 min de 1.060 para 300. Na janela do par, o teto passa de 4.800 (2 × Joalheiro 2, que exige o Mineiro) para 6.000 (2 × Mineiro).

## 5. API para a view
| membro | o que é |
|---|---|
| `Carrier.Held[i]` (int[6]) | quantos de cada `Item` na mão (desenhar a pilha mista) |
| `Carrier.Count`, `Carrier.Item`, `Carrier.Has(i)` | total, item mais adiantado, tem ou não |
| `Sim.QueueCap` | vagas do balcão agora (4..8) |
| `Sim.Counter.Body` (`Box`: `Pos` = centro, `Half.X × 2` = largura) e `Sim.Counter.Half` | corpo sólido do balcão, que cresce com as vagas |
| `Balance.CounterHalfX(vagas)`, `Balance.SlotStep` (0,85), `Balance.CounterEnd` (0,2), `Balance.QueueCap0` (4), `Balance.QueueCapMax` (8) | geometria para pré-montar os 5 estandes |
| `Sim.ClientSlot(i)` | vaga i (0..QueueCap−1); `ClientSlot(QueueCap)` = onde aparece quem vai embora com a fila cheia |
| `Upgrade.Counter5..Counter8`, `Upgrades.All[u].Name/Desc` | cartões "Balcão N vagas" no menu (`InMenu`) |
| `Sim.Deliverable(c)`, `Sim.Wanted(item)` | (opcional) produto que o balcão aceita agora; produto que a fila pede sem estoque |

### O que a view precisa desenhar na leva 2
- **Estande do balcão por nível**, com a largura de `Counter.Body`, centrado em `Counter.Pos`: os módulos da raia de arte (`balcao_ponta_esq`, `balcao_meio`, `balcao_ponta_dir`). Hoje o sprite antigo tem ~1,95 m e o corpo já tem 3,8–7,2 m: **o ferreiro bate numa borda invisível até a leva 2.**
- **Clientes:** reposicionar quando `QueueCap` muda (as vagas se recentram, ~0,43 m por nível).
- **Pilha mista:** feita no mínimo, com os ícones antigos empilhados; o passo encolhe acima de 6 itens. A arte nova dos itens é da outra raia.
- **Opcional:** o cliente que vai embora com a fila cheia (`ClientLeft` B = 2) hoje é invisível; pode aparecer andando de volta.

## 6. Pendências
- Playtest humano: a fila cheia de quem quer escudo/ferramenta é legível? O bot precisa da regra "a fila pede"; o humano depende da dica e do balão do cliente.
- Sem a compra direta, a economia ficou mais lenta (−7% de receita em 90 min; produção completa 43:37 → 48:24), um pouco fora da janela 42–48 no Dt 1/30 (47:32 no Dt 1/60). Decisão do coordenador: aceitar ou repreçar.
- Save da v0.4.1 com a Vitrine volta com 4 vagas (antes 6). Não perde nada, mas o jogador vê a fila "encolher" até comprar o Balcão 5 (150).
