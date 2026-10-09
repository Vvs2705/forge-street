# Forge Street — cliente VIP e velocidade por anúncio (contrato, 2026-10-08)

Pedido do Vinicius: "seria interessante aparecer um pedido VIP, que ganhe mais dinheiro quando aquele personagem aparecer, tipo o dobro do valor ou triplo, uma vez a cada 5 minutos mais ou menos, tem isso em alguns jogos, assim como assistir vídeos para ele aparecer mais rápido, ver propaganda para conseguir velocidade 2x ou 3x por 1 min".

Referências do gênero: VIP com trilha de dinheiro (My Perfect Hotel), missão "servir um VIP" (Burger Please!) e boost de velocidade de 3 min (MPH, BP), em [BENCHMARK_MERCADO.md §8](BENCHMARK_MERCADO.md). No GDD, o §3 põe o 1º rewarded opcional em 5:30–6:30 e o §7 pede "pedido especial" e "2× por 3–5 min".

Números: [BALANCE.md §18](BALANCE.md). **Rodada 2** (decisões do coordenador, mesmo dia): recarga do boost e VIP chamado como VIP extra com pacote 2× (§18.8). **Rodada 3** (teste com gente): paciência do VIP pelo tamanho do pacote e teto de 20 no VIP chamado (§18.9). Estado: **só o núcleo, IMPLEMENTADO e TESTADO** (dotnet 78/78; viewcheck 0 erros e 0 avisos, sem tocar na View). **Não compilado no Unity.** A View e o SDK de anúncio são de outras raias. O bot não assiste anúncio.

## 1. Cliente VIP
- **Quando:** o relógio (`Sim.VipIn`) só anda depois da 1ª venda, no tempo de jogo.
  - Quando zera, o Sim sorteia o VIP. O próximo intervalo sai de 4 a 6 min por sorteio determinístico (`Sim.VipInterval(n)`: sequência de Weyl, sem estado além de `VipCount`).
  - O 1º VIP chega 5:14 depois da 1ª venda (5:36 no bot).
  - Se ainda há um VIP ativo, o relógio fica em 0 até ele sair.
- **Onde:** ocupa uma vaga normal do balcão principal.
  - Com a fila cheia, ele espera do lado de fora (`VipActive` = true, fora de `Queue`) e pega a próxima vaga livre antes de quem chega depois. Não some.
  - Medido no bot: espera de 2 s em média para entrar, 9 s no máximo.
- **O que pede:** um produto do balcão já aberto, em rodízio: espada → escudo → ferramenta. A joia não entra, porque é da loja de joias.
- **Pacote:** vale ~7% do ouro de um intervalo médio (300 s × `RateEma`), a 3× o preço, com no mínimo 3 unidades e no máximo a prateleira (`CounterCap`: 5, ou 10 com a Vitrine).
  - Medido no bot: 3 nos primeiros ~25 min (a espada das 20:44 já pede 5); depois dos 30 min, 6–7 escudos ou 10 espadas/ferramentas.
  - Com 3 unidades fixas, o VIP caía para 1–3% da janela depois dos 35 min, porque a joia passa a dominar a renda.
- **Preço:** **3× por unidade**, uma unidade por atendimento (0,4 s).
  - Cada unidade sai como `Ev.Sold` (B = 3× o preço) e conta em `Sales`, `GoldEarned` e nos marcos.
  - O VIP fica na vaga até levar o pacote todo.
- **Paciência** (rodada 3): 90 s + 6 s por unidade do pacote, no tempo de jogo (`Balance.VipPatienceFor`). Ficam 108 s para 3 unidades, 150 s para 10 e 210 s para 20. O cliente normal tem 57–77 s.
  - Se cansar, sai com `Ev.VipLeft` e paga só o que levou.
  - Esse VIP não entra no `ClientsLost`, que continua sendo a métrica dos clientes normais.

## 2. "Chamar o VIP" (anúncio recompensado; decisão do coordenador, rodada 2)
- `Sim.SummonVip()` traz um **VIP extra** agora (ele entra na próxima vaga livre). O relógio do natural **não muda**: nem reinicia, nem adianta.
  - Único efeito colateral: se o natural zerar com o extra ainda na vaga, ele espera o extra sair (no máximo a paciência do extra, até 210 s).
- **Pacote** (rodada 3): 2× o do natural, **até 20**, e passa da prateleira: 6–10 sem a Vitrine, 6–20 com ela.
  - O teto de 20 sai da própria regra, porque o natural já vem cortado na prateleira (no máximo 10).
- **Regra contra abuso** (`Sim.CanSummonVip`): só depois da 1ª venda, sem VIP ativo (esperando vaga ou na fila) e com o VIP natural a mais de 60 s. Fora disso, devolve false e nada muda.
- **Valor medido** (rodada 3, BALANCE §18.9):
  - até os 30 min: **1,1–1,4 min de renda** por anúncio (na rodada 1 eram 0,3–0,5);
  - depois dos 35 min: **0,42 min** (na rodada 2, com teto de 10, eram 0,19).

## 3. Velocidade 2×/3× (anúncio recompensado)
- `Sim.StartBoost()`: 1 anúncio = **2× por 60 s**. Um 2º anúncio com o boost ativo sobe para **3×** e reinicia os 60 s. Devolve o multiplicador novo, ou **0 se recusou** (nada muda).
- **Recarga** (decisão do coordenador, rodada 2): **5 min de jogo, contados do fim** de cada boost (`Sim.BoostCooldown`, s restantes).
  - `Sim.CanBoost` = (boost ativo e abaixo de 3×) ou (sem boost e recarga zerada). No 3×, recusa.
  - Ciclo máximo: 60 s de boost a cada 6 min.
- **Acelera:**
  - estações (fornalhas, bigornas, bancadas, joalheria) e esteira;
  - ajudantes (andar e mãos);
  - chegada de clientes de todas as linhas: a demanda acompanha.
- **O ferreiro** (andar, mãos e pad) vai a **no máximo 1,3×**, para o joystick não fugir do controle.
- **Não acelera:** paciência dos clientes, atendimento do balcão, relógio do VIP e o próprio boost. Tudo isso corre no tempo de jogo.
- **Offline:** o offline não paga boost. A taxa do cofre (`RateEma`) aprende o ouro por segundo de fábrica (dt × boost). A mesma taxa dimensiona o pacote do VIP.
- **Save:** nem o boost nem a recarga vão no save; os dois somem ao fechar, e reabrir libera um boost.
  - Gravar a recarga seria uma linha, mas exigiria descontar o tempo offline (5 min "de jogo" contra horas com o jogo fechado).
  - Decidi não gravar. Fica para quando o diário mostrar abuso (`// ponytail:` no `Sim`).

## 4. Save
- `vip=<VipIn>,<VipCount>,<produto>,<unidades>`.
- A fila não vai no save. Por isso, o VIP que já estava na vaga volta como pendente, com o que falta do pacote e a paciência cheia (calculada pelo que falta), e pega a 1ª vaga ao reabrir.
- **Save antigo** (sem `vip=`): relógio novo (`VipInterval(0)` = 314 s) e nenhum VIP.
- **Lixo:** número inválido vira o padrão; relógio acima de 6 min fica em 6 min; produto fechado ou fora do balcão é descartado; pacote acima de 20 fica em 20.

## 5. API para a View
| membro | o que é |
|---|---|
| `Ev.VipArrived` (A = item, B = unidades, Pos = vaga) | o VIP entrou na vaga |
| `Ev.VipServed` (A = item, B = ouro total pago, Pos = vaga) | levou o pacote e vai embora feliz |
| `Ev.VipLeft` (A = item, B = unidades que faltaram, Pos = vaga) | cansou |
| `Ev.Sold` com B = 3× o preço | cada unidade do VIP ("+30", moedas) |
| `Client.Vip`, `Client.Pack` (unidades que faltam), `Client.Paid` | o VIP dentro de `Sim.Queue`; `Patience/MaxPatience` como o normal |
| `Sim.VipActive` | há VIP esperando vaga ou na fila |
| `Sim.VipIn` | segundos até o próximo VIP natural (parado antes da 1ª venda) |
| `Sim.CanSummonVip`, `Sim.SummonVip()` | botão "chamar o VIP": mostrar só com `CanSummonVip`; chamar depois do anúncio assistido |
| `Sim.BoostMul` (1, 2, 3), `Sim.BoostLeft` (s) | estado do boost para o selo/cronômetro |
| `Sim.CanBoost`, `Sim.BoostCooldown` (s) | botão de velocidade: ativo só com `CanBoost`; fora disso, mostrar a recarga |
| `Sim.StartBoost()` | chamar depois do anúncio assistido; devolve o novo multiplicador (0 = recusado) |
| `Balance.VipPatienceFor(unidades)`, `Balance.BoostSeconds`, `Balance.BoostMax`, `Balance.BoostCooldownSeconds`, `Balance.VipSummonPackMul` | para barras e textos (a barra do VIP usa `Client.MaxPatience`, que já vem pelo pacote) |

### O que a View precisa desenhar
- **Corpo do VIP:** hoje o VIP já aparece como um cliente normal (mesmo `Queue`, mesmo balão), sem nenhuma mudança na View. Falta a cara de VIP: coroa ou capa e a marca "×3".
- **Pedido:** o balão mostra o produto e "×N" com `Client.Pack`, que cai a cada unidade entregue.
- **Eventos:** `VipArrived` toca som e mostra o aviso "VIP chegou!". `VipServed` mostra "+total" grande com moedas. `VipLeft` mostra "..." e o cliente vai embora. Os três ainda não têm `case` no `Game.HandleEvents`, então ficam mudos até a leva da View.
- **Fila cheia:** o VIP pendente pode aparecer esperando ao lado do balcão (`VipActive && Queue` sem VIP).
- **Botões de anúncio:** "Chamar VIP" (só com `CanSummonVip`) e "Velocidade 2×/3×", com o selo `BoostMul` e o cronômetro `BoostLeft`. O 2º toque durante o boost leva a 3×. Fora de `CanBoost`, o botão mostra a recarga (`BoostCooldown`).
- **Diário:** sugestão de eventos `vip_arrived`, `vip_served`, `vip_left`, `ad_summon_vip` e `ad_boost` para medir o opt-in (GDD §15/§26: ≥ 45%).

## 6. Pendências e riscos (detalhe em BALANCE §18.6)
- **Frequência do boost: RESOLVIDO** com a recarga. O uso máximo (3× toda vez que a recarga deixa, 31 anúncios em 90 min) fecha a produção aos **37:30**. Sem a recarga, eram 26:56.
- **"Chamar o VIP": RESOLVIDO** (rodada 3: VIP extra com pacote 2× até 20). Vale 1,1–1,4 min de renda por anúncio até os 30 min e 0,42 min depois.
- **Paciência do VIP:** 90 s + 6 s por unidade (rodada 3).
  - No bot lento com VIP extra a cada 5 min, 1 de 175 VIPs cansou (faltou 1 unidade de um pacote grande); o maior tempo na vaga foi de 138 s.
  - O playtest com gente decide se basta.
- **Playtest humano:** o "×N" do pacote grande (até 20) é legível? A paciência de 90 s + 6 s por unidade basta? No bot lento (2× a reação) com VIP extra, o maior tempo na vaga foi de 138 s e 1 de 175 VIPs cansou.
