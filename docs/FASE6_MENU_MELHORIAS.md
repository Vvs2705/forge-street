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
- Bot: compra do menu quando pode (sem caminhar). Remedir a §3 do GDD (primeiros 10 min): sem caminhada até os pads, upgrades saem mais cedo; se algum marco sair da janela, propor custo (não aplicar sem decisão). Offline: `CheapestLockedCost` inalterado (menu e pad contam igual).

## 3. View (HUD)
- Barra inferior fixa (~14% da altura, área segura respeitada) com botão "Melhorias" que abre uma faixa de cartões rolável na horizontal; cada cartão: ícone simples (forma/cor do item afetado), nome, efeito curto, preço. Estados: comprável (destaque ouro), sem ouro (cinza com preço), travado (cadeado + "requer X"), comprado (some ou "✓").
- Toque compra (`TryBuyMenu`), com som/efeito de compra existente. O joystick NÃO pode começar dentro da barra (como já não começa na faixa de cima: `Joystick.TopBand` → adicionar `BottomBand`).
- A câmera reserva a faixa de baixo como reserva a de cima (o mundo não fica atrás da barra).

## 4. Testes
TryBuyMenu (ouro, pré-requisito, idempotência, evento); upgrade de menu não tem pad visível; save antigo com pago parcial em pad de menu devolve ouro; bot 10/60/90 min remedidos; joystick ignora toque na barra (teste de view se viável, senão foto).
