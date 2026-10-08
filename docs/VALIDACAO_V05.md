# Forge Street v0.5, leva 2: integração da arte na View (validação, 2026-10-08)

Escopo: a View passa a usar o que o core (FASE7) e a arte (ASSETS §7) entregaram. Core, testes e `Resources/Sprites` não foram tocados. Contrato: `docs/ASSETS.md` §7 (integração) e `docs/BENCHMARK_VISUAL.md` §3–4.

## O que mudou na View
| # | Pedido | Como ficou |
|---|---|---|
| 1 | Ícones dos itens | `Art.PaintItem`: `icone` no balão e nos cartões do menu; `deitado` na pilha da cabeça, nas pilhas das estações, no estoque da loja de joias, na esteira e nas bocas (0,45 m a 50%). Cor branca, sem `StackBg` e sem achatar o lingote. As máscaras antigas só entram se faltar a folha. |
| 1b | Pilha mista | Do mais grosso (embaixo) ao mais fino (em cima): minério, escudo, joia, ferramenta, lingote, espada. Cada item fica sobre o anterior com 25% de sobreposição; uma coluna acima de 1,6 m encolhe por igual. |
| 2 | Balão grande | Só o 1º da fila (balcão e loja de joias): balão de 1,32 × 1,12 m com ícone de 0,9 m. Anel de paciência na borda (verde → amarelo → vermelho, no sentido horário). Abaixo de 20% ele treme ±3° a 6 Hz. Os outros clientes têm mini-ícone de 0,42 m e uma barrinha de paciência. |
| 3 | Moedas | 3 a 6 sprites `moeda` (uGUI, por cima da HUD) voam do cliente até a moeda da HUD em 0,4 s, em arco. O número só sobe quando elas chegam (`Gold − _pending`), e a moeda e o número pulsam (1 → 1,15 → 1). O "+N" da venda saiu de cima do balão e agora sobe pela frente do estande. |
| 4 | Estande | Ponta esquerda + N módulos + ponta direita, na largura de `CounterHalfX(QueueCap)`. Ordem dos módulos: espadas, escudos, espadas, ferramentas, escudos, espadas, ferramentas, escudos (os N primeiros); uma linha que ainda não foi comprada vira rack de espadas. O estoque fica em pé nos `encaixes`, e o estande remonta com pop quando as vagas mudam. O sprite `balcao` com toldo saiu (o balcão procedural é a reserva). |
| 5 | Portões | `pilar` nas 4 pontas, `portao_*` no vão de cima e `porta_servico_*` no de baixo. A troca para aberto acontece na compra do Corredor. Saíram `Batente`, `Porta` e `arco`. |
| 6 | Fila cheia | Quem recebe `ClientLeft` B = 2 desce da rua de cima e vai embora pela esquerda. |

Calibração feita olhando as fotos: o topo da cabeça na tela fica a ~0,65 m do pé (ferreiro) e a 0,5–0,65 m (clientes). Antes era 0,95 m, e o balão e a pilha flutuavam uns 0,3 m acima da cabeça. Por causa do balão, `Game.ContentTop` passou de `WorldH + 1,6` para `+ 2,1`.

Flags de dev novas (`Game.DevArgs`, aplicadas depois do `-warmup`): `-hold 0,3,3,3,0,0` define a carga do ferreiro por Item (até o teto) e `-stock 10,10,10` define o estoque do balcão (espada, escudo, ferramenta, joia).

## Verificação
| Portão | Resultado |
|---|---|
| `dotnet build client/tools/viewcheck/view -nologo -v q --artifacts-path <tmp>` (core das fontes) | 0 erros, 0 avisos |
| `dotnet test client/tools/coretests --artifacts-path <tmp>` | 72/72 |
| `Unity.exe -batchmode -nographics -quit -projectPath client -executeMethod FS.EditorTools.Setup.BuildWindows -logFile client/Builds/validation_v05/build_win.log` | "Build Finished, Result: Success." |
| `ForgeStreet.exe -batchmode -nographics -autoplay 10 -logFile validation_v05/autoplay.log` | `AUTOPLAY OK venda1=23s upgrades=9 ouro=1320` |

Fotos em `client/Builds/validation_v05/shots/`, todas com `-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession` (salvo indicação) e logs em `validation_v05/logs/`:

| Foto | Argumentos |
|---|---|
| `01_inicio_4vagas.png` | `-warmup 40 -px 4.5,11.2 -shotdelay 2` |
| `01b_inicio_aspecto_aparelho.png` | igual, janela 432 × 960 (20:9, como o POCO) |
| `02_pilha_mista.png` | `-hold 0,3,3,3,0,0 -px 4.5,10.6` |
| `02b_pilha_mista_mochila.png` | `-buyids 3,4,9 -hold 2,6,6,3,2,0 -px 4.5,8.5` (19 itens, coluna encolhida) |
| `03_estande_8vagas_cheio.png` | `-buyids 3,9,10,25,26,27,28 -warmup 25 -stock 10,10,10 -px 4.5,11 -shotdelay 0.6` (moedas no ar) |
| `04_rua_fechada.png` | `-px 8.4,9.5` |
| `05_rua_aberta.png` | `-buyids 15,16 -warmup 30 -px 10.5,9.5` (portões abertos, balão do nobre, cliente da fila cheia passando) |
| `06_venda_moedas.png` | `-warmup 30 -stock 5 -px 4.5,11 -shotdelay 0.7` |
| `07_balcao_4para5_sequencia.png` | quadros de `-warmup 30 -buyids 10,25@30.6 -px 4.5,11 -record <tmp> -recordsec 1.6`: estande alarga com pop e a fila desliza |

## Fica para o aparelho
- Leitura a 1080 × 2400 do mini-ícone (0,42 m ≈ 40 px) e da barrinha de paciência.
- Custo dos sprites a mais: estande de 8 vagas com 34 renderers (10 peças + 24 itens), 36 na pilha e até 30 moedas.
- O PNG com o balcão antigo (`Resources/Sprites/balcao`) e o `arco` não são mais usados, mas continuam no build porque estão em `Resources`.
