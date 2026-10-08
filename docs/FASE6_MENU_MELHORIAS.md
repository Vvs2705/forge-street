# Forge Street — menu inferior de melhorias (contrato, 2026-10-07)

Pedido do Vinicius: "Os itens de melhoria do personagem principal precisam estar no menu na barra abaixo, sem precisar ficar em cima de círculos — apenas as melhorias, dos itens, ou compra de itens da 'loja' que vão ser upados assim."

## 1. Divisão (decisão do coordenador; o Vinicius pode ajustar)
| Vai para o MENU (toque, compra na hora) | Continua PAD no mundo (construção no lugar) |
|---|---|
| Fole, Fole duplo (fornalha mais rápida) | 2ª bigorna, Escudos, Ferramentas, 2ª fornalha |
| Mochila (carga do ferreiro) | Esteira |
| Botas (velocidade do ferreiro) | Ajudante 1/2/3 (contratar no ponto de contratação) |
| Martelo veloz (bancadas mais rápidas) | Corredor, Joalheria, Joalheiro (e Mineiro/Joalheiro 2 da fase 4) |
| Vitrine (balcão) | Luxo (Fachada nobre, Piso de oficina, Joalheria real) |
| Ajudantes ágeis | |
| Lupa, Vitrine de joias | |

## 2. Core
- Marca por upgrade `UpgradeDef.InMenu` (como `Luxury`). Upgrades do menu NÃO têm pad visível (`Pad.Current` devolve −1), mas os pads continuam na lista (o save é por índice); no `Load`, o que estava pago parcialmente num pad que virou menu volta para o `Gold`.
- `Sim.TryBuyMenu(Upgrade u)`: compra na hora se `InMenu`, não comprado, pré-requisito ok e `Gold >= Cost`; emite `Ev.Bought` (posição = jogador). Nada de dreno de 1 s.
- Dica: `Hint.BuyMenu` (HintArg = upgrade) quando o mais barato pagável é do menu; mantém `Hint.BuyPad` para pads.
- **Bocas laterais** (coordenador, foto `Builds/validation_portas/shots/42_bocas.png`): Fornalha, Bigorna e Joalheria ficaram com entrada embaixo e saída em cima porque à esquerda caíam nos pads do Fole, do Martelo veloz e da Lupa. A boca de cima fica ATRÁS do sprite da estação e a de baixo embaixo da barra de progresso: não lê. Esses 3 pads somem com o menu, então as 3 estações passam a entrada à ESQUERDA e saída à DIREITA, como as outras e como as pilhas da view (`InPile` −1,05 / `OutPile` +1,05). Conferir a distância das bocas aos pads que ficam (Esteira, Mineiro, Joalheiro 2, Corredor, luxo) e às vagas/baús; a esteira continua ligando Fornalha → Bigorna sem zona.
- Bot: compra do menu quando pode (sem caminhar). Remedir a §3 do GDD (primeiros 10 min): sem caminhada até os pads, upgrades saem mais cedo; se algum marco sair da janela, propor custo (não aplicar sem decisão). Offline: `CheapestLockedCost` inalterado (menu e pad contam igual).

## 3. View (HUD)
- Barra inferior fixa (~14% da altura, área segura respeitada) com botão "Melhorias" que abre uma faixa de cartões rolável na horizontal; cada cartão: ícone simples (forma/cor do item afetado), nome, efeito curto, preço. Estados: comprável (destaque ouro), sem ouro (cinza com preço), travado (cadeado + "requer X"), comprado (some ou "✓").
- Toque compra (`TryBuyMenu`), com som/efeito de compra existente. O joystick NÃO pode começar dentro da barra (como já não começa na faixa de cima: `Joystick.TopBand` → adicionar `BottomBand`).
- A câmera reserva a faixa de baixo como reserva a de cima (o mundo não fica atrás da barra).

## 4. Testes
TryBuyMenu (ouro, pré-requisito, idempotência, evento); upgrade de menu não tem pad visível; save antigo com pago parcial em pad de menu devolve ouro; bot 10/60/90 min remedidos; joystick ignora toque na barra (teste de view se viável, senão foto).

## 5. Estado do CORE: IMPLEMENTADO e TESTADO (Leva 11, raia core/economia, 2026-10-07)
- **Arquivos:** `Core/Defs.cs`, `Core/Sim.cs`, `Core/Bot.cs`, `Tests/EditMode/CoreTests.cs`, `Tests/EditMode/BalanceTests.cs`.
- **Testes:** dotnet 66/66 e `viewcheck` com 0 erros e 0 avisos, já com a `MenuBar.cs` do coordenador. **NÃO COMPILADO no Unity.**
- **Onde estão os números:** [BALANCE.md §15](BALANCE.md).

### API final para a view
| membro | o que é |
|---|---|
| `UpgradeDef.InMenu` (bool) | `true` nos 9 do menu: Fole, Fole duplo, Mochila, Botas, Vitrine, Ajudantes ágeis, Martelo veloz, Lupa, Vitrine de joias. Construtor: parâmetro nomeado `menu: true` |
| `Sim.TryBuyMenu(Upgrade u) : bool` | compra na hora se `InMenu`, não comprado, `Requires` comprado e `Gold >= Cost`. Desconta o ouro, aplica pelo mesmo `Buy` do pad e emite `Ev.Bought` (A = upgrade, B = preço, `Pos` = jogador). Senão devolve false e nada muda |
| `Sim.MenuAvailable(int u) : bool` | (extra, opcional) o upgrade está à venda no menu agora, com ou sem ouro: `InMenu`, não comprado e pré-requisito comprado. Serve para os estados do cartão "travado" e "comprado" |
| `Sim.CheapestAffordableMenu() : int` | (extra, opcional) upgrade do menu que é a compra mais barata pagável agora, ou −1. No empate com pad, o menu ganha. É a regra da dica e do bot |
| `Hint.BuyMenu` (anexado ao fim do enum `Hint`) | dica com a mesma prioridade do `BuyPad`. `HintArg` = índice do upgrade (`Upgrades.All[arg].Name`, `Upgrades.Cost(arg)`) |
| `Pad.Current(sim)` | devolve −1 para upgrade do menu: os 8 pads do menu (slots 0, 6, 7, 8, 10, 11, 15, 16) ficam na lista e nunca aparecem |
| `Station.InAt` / `OutAt` | Fornalha (0,6; 5,5)/(2,4; 5,5), Bigorna (0,6; 9,5)/(2,4; 9,5), Joalheria (11,1; 6,5)/(12,9; 6,5): entrada à esquerda, saída à direita |
| pad da Esteira (slot 9) | **(0,5; 11,2)**, antes (0,5; 7,8). A view lê `Pad.Pos`, então não precisa mudar nada |

- **Nota para a view:** fora do `Tick`, o `Ev.Bought` do `TryBuyMenu` entra em `Sim.Events` na hora, e o próximo `Tick` limpa a lista. Toque o som e o efeito logo depois de `TryBuyMenu` devolver true, ou leia `Events` antes do próximo `Tick`.
- **Nomes do contrato preservados**, sem renomeação: `UpgradeDef.InMenu`, `Sim.TryBuyMenu`, `Hint.BuyMenu`, `Upgrades.All/Cost/Count`, `UpgradeDef.Requires/Name/Desc`, `Sim.Bought/Gold`.

### Decisões e medições (detalhes em BALANCE §15)
- **Bot:** mesma política do pad, a compra mais barata pagável. Upgrade do menu ele compra parado: espera `Reaction` (0,7 s) e toca, sem andar.
- **Save antigo:** o parcial pago num pad que virou menu volta para o `Gold` no `Load` (teto `int.MaxValue`). Exemplos: Botas 30 no save da v0.1, Vitrine de joias 450 no save de 20 flags.
- **Bocas laterais:**
  - menor distância boca × pad que fica: 1,70 m;
  - baús, filas e contratação: ≥ 2 m;
  - a esteira continua Fornalha → Bigorna, sem zona.
- **Colisão ajustada:** com as entradas na parede esquerda, o pad da Esteira ficou no corredor entre elas, a 0,10 m da linha. O bot humano passou 5× por cima, com até 0,43 s (o dwell é 0,5 s). Ele foi para (0,5; 11,2): 0 travessias e 0 pagamento acidental.
- **Bug corrigido em `Sim.Via`:** o ajudante travava no meio da porta lateral com x 9,29999. Apareceu com a entrada nova da Joalheria.
- **Primeiros 10 min (§3 do GDD):** verdes. Fole 1:07 · 2ª bigorna 2:17 · Ajudante 2:59 · Escudos 4:00 · Esteira 7:20 (antes 1:04 / 1:57 / 2:38 / 3:45 / 7:03).
  - O menu sozinho adianta 21–51 s, mas as bocas laterais alongam o laço do começo e o saldo é um pouco mais lento. Esse custo é da decisão das bocas (legibilidade), não do menu.
- **Produção completa:** **43:37** (Dt 1/30) · **43:29** (Dt 1/60), dentro da janela 42–48. O par Mineiro 3.000 / Joalheiro 2 2.400 fica e não há proposta.
- **Luxo APLICADO 14.000 / 18.000 / 22.000** (autorizado pelo coordenador):
  - com 11.000, a Fachada saía +6,9 min depois da produção completa;
  - agora: Fachada +8,7 · Piso +19,7 · Joalheria real +33,3 (76:56), com intervalos crescentes;
  - um retorno no teto offline compra 1 luxo.
- **Ouro/min 60–90:** 1.629 · 1.613 (antes 1.638 · 1.668).
- **Fome da joalheria:** 15–17% (antes 12–14%; o critério é ≤ 35%).

### Pendências
- **Validar no aparelho:** a barra, o toque e o joystick fora da faixa de baixo (`BottomBand`).
- **Ainda sem medição de humano:** o tempo do humano para abrir o menu e tocar. O bot usa 0,7 s.
- ~~**Mineiro (0,5; 3,5):** fica a 0,62 m da linha Depósito → entrada da Fornalha.~~ **RESOLVIDO na v0.4.1:** pad do Mineiro em (2,8; 0,6), embaixo do Depósito; produção completa inalterada (43:37).

## 6. Estado da VIEW (coordenador, 2026-10-07)
- `View/MenuBar.cs`: barra fixa (8,5% da altura) com o botão "Melhorias (N)" — dourado quando há N compráveis — e fileira rolável (17%) de cartões ordenados por preço: ícone sobre disco escuro, nome, efeito, preço. Comprável = cartão dourado; sem ouro = escuro com preço em vermelho; travado = apagado com "requer X"; comprado = some.
- Toque → `Sim.TryBuyMenu`; se true, o `Game.Bought` toca o som, solta "+nome!" e grava o diário (o `Ev.Bought` fora do `Tick` não chega ao `HandleEvents`).
- `Joystick.BottomPx` = topo da barra (ou da fileira aberta): toque abaixo não abre o joystick. Câmera reserva `MenuBar.Band` embaixo. Painel modal fica por cima da barra.
- Dica `Hint.BuyMenu`: "Toque em Melhorias: X (N de ouro)". Flag de foto `-menu` abre a fileira.
- Fotos: `client/Builds/validation_menu/shots/`.

