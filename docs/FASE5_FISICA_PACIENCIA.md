# Forge Street — física dos objetos + paciência dos clientes (contrato, PRIORIDADE, 2026-10-07)

Origem: playtest do Vinicius no POCO F4 (APK 0.3.0):
1. "O tempo de cada personagem aguardar seu pedido é muito curto, com base no tempo de produção."
2. "Precisa existir física nos objetos: ele atravessa tudo ou fica sobre objetos, e isso não faz sentido."

## 1. Paciência proporcional à produção
- Hoje: `Balance.Patience = 25 s` (balcão) e `JewelPatience = 40 s` (nobres). No início, sozinho, o jogador leva ~15–20 s por espada (minério → fornalha 4 s → bigorna 5 s + caminhada) e a fila tem até 4 clientes: o 3º/4º desiste antes de ser atendido.
- Novo: paciência POR ITEM, derivada do tempo de produção da linha no início do jogo, com folga para a fila: `paciência(item) = PatienceBase + PatiencePerSecond × tempo_de_producao_inicial(item)`, onde `tempo_de_producao_inicial` = fornalha inicial × lingotes por item + tempo da bancada inicial. Ponto de partida para medir: base 30 s + 3 × produção → espada ≈ 30 + 3×9 = 57 s; escudo ≈ 30 + 3×(8 + 7,5) ≈ 77 s; ferramenta ≈ 30 + 3×9 = 57 s; joia ≈ 30 + 3×(8 + 10) = 84 s. Calibrar com o bot: clientes que desistem/min caem bastante SEM a fila virar parede (medir `ClientsTurnedAway`, `ClientsLost`, `MaxQueue`) e os primeiros 10 min continuam na §3 do GDD.
- A barra de paciência na view já é proporcional (`Patience/MaxPatience`): nada a mudar além de ler `MaxPatience` do cliente.

## 2. Física: estações e objetos sólidos
- Cada estação ganha um corpo sólido (caixa alinhada aos eixos, aproximando a base do sprite; ponto de partida meia-largura 0,65 m × meia-altura 0,40 m centrada em `Station.Pos`; balcão/loja podem ser mais largos). Decoração sólida: carroça (14,2; 3,0), postes do arco (9,0 ± 0,6; 12,6), pedestais da Joalheria real e postes da Fachada nobre quando comprados — definir no core (lista de obstáculos estáticos em `Balance`/`Sim`) para a view e a simulação usarem a MESMA geometria.
- Jogador, ajudantes e bot são círculos (raio ~0,25 m) e não podem entrar nos corpos: resolver por empurrão para fora do obstáculo mais deslizamento tangencial, para não travar em quina. O clamp do mundo continua.
- Interação passa a ser "encostar": distância do personagem ao CORPO da estação ≤ ~0,35 m (ou raio equivalente a partir do centro), em vez de ficar dentro de 0,9 m do centro. Ajudantes param na borda (o `reach` do `MoveTowards` precisa ser maior que o corpo, senão empurram para sempre). Pads e baús continuam sendo pisados (chão).
- Fila de clientes: as vagas não podem cair dentro de corpos.
- Medir efeito em balance (caminhada muda): primeiros 10 min, ouro/min, fim da produção.

## 2b. Duas bocas por estação de produção (pedido do Vinicius, 2026-10-07)
"Cada local de criação tenha dois acessos, um de colocar o item e outro de retirar, para que seja possível abastecer ainda mais sem retirar o que já foi feito, garantindo mais opções de estratégia."
- Toda estação que PRODUZ (fornalhas, bigornas, bancadas de escudos/ferramentas/joias) tem uma **zona de ENTRADA** e uma **zona de SAÍDA**, em lados opostos do corpo sólido, alinhadas com as pilhas que a view já desenha (`InPile` de um lado, `OutPile` do outro; hoje `OutPile` fica a +1,05 m em x). Na ENTRADA o personagem só DEPOSITA (nunca recolhe); na SAÍDA só RECOLHE (nunca deposita).
- Depósito de minério e balcões continuam com uma zona só (o balcão recebe; o depósito entrega).
- Ajudantes vão à zona certa: buscando produto → SAÍDA da fonte; entregando insumo → ENTRADA do destino. O bot idem. A esteira continua ligando a saída da Fornalha à entrada da Bigorna (sem zona).
- As zonas não podem cair dentro de outro corpo nem de um pad (pad pisado por acidente gasta ouro): conferir as posições de todas as estações (oficina e rua lateral) e ajustar o lado (esquerda/direita ou cima/baixo) por estação se colidir; documentar a tabela final no core para a view.
- View: marcar as duas bocas no chão (seta/placa pequena "entra"/"sai" ou cor da pilha) — o coordenador faz.
- Medir: caminhada muda; primeiros 10 min, ouro/min, ajudantes sem travar.

## 3. View
- A ordenação por Y já desenha quem está atrás/na frente; conferir na foto que o personagem encostado na estação aparece na frente ou atrás de forma coerente (pé abaixo da base = na frente).
- Barra/rótulo nada muda; apenas conferir.

## 4. Testes
Paciência por item (valores e fila); personagem não entra no corpo de nenhuma estação/obstáculo após N ticks empurrando contra; desliza na quina; interação funciona encostado; ajudantes completam ciclos (sem travar) em 10 min; save antigo carrega; bot 10/60/90 min remedidos (limites medido + 30%).
