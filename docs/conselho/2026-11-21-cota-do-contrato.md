# Ata do Conselho — cota do contrato (2026-11-21)

> Arquivo: `docs/conselho/2026-11-21-cota-do-contrato.md` · Aberta por: `producer` · Estado: **DECIDIDA**
> Caminhos: a convenção do jogo vence (`CLAUDE-TIME.md`, resolução de caminhos 1). As decisões do Forge ficam na tabela V do `PROJETO.md` §4, então o ADR desta ata é a **V30**; a ata fica em `docs/conselho/` (padrão do time).
> Conselho rodado no plano B da skill (o time ainda não estava instalado no repositório): cada diretor como agente lendo o próprio `.md` de papel.

## 0. Entrada
- **Pedido original:** linha do `PROJETO.md` §7: "Contrato conta a mesma peça para a encomenda e para o contrato (sem o seletor de cota do CONTEUDO_V1 §8) | design | `Contratos.cs` | decidir se a cota entra antes do Limited Release".
- **Jogo / repositório:** forge-street · **Branch:** `docs/cota-contrato-adr`

## 1. Enquadramento (producer)
- **Objetivo:** decidir se o seletor de cota 0/25/50/100% por linha (`CONTEUDO_V1.md` §8) entra antes do Limited Release.
- **Opções:** (A) implementar o seletor agora; (B) manter a dupla contagem como decisão registrada e revisitar com dados; (C) outra (C1: contagem exclusiva sem seletor; C2: só o pátio de cotas do Arsenal decide).
- **Restrições:** outra thread mexe na esteira, nos luxos, no tutorial e na IA de pedidos (o caminho `Sim.Sell` → `Encomendas.OnVenda`); sem dotnet no container (CI de ~20 min por rodada); mudança de economia exige econsim no `BALANCE.md`.
- **Critério de pronto:** PR só de docs e comentário, sem linha de Core alterada; GDD e catálogo sem prometer uma decisão que o jogo não tem.

## 2. Rodada 1 — posições independentes

### technical-director — **B**
- Hoje há 3 pontos de venda e cada um chama `Encomendas.OnVenda` e depois `Contratos.OnVenda`: `Sim.Producao.cs` (`Sell`), `Sim.Exportacao.cs` (lote) e `Sim.Cotas.cs` (bloco do pátio). Uma peça conta para um contrato só. O save guarda `ctr=`, `ctr.v=` e `ctr.v2=`, sem campo de cota.
- A custa 2–3 dias e ≥ 3 rodadas de CI: desvio da peça antes do `Stock[]` (o caminho da esteira, em outra thread), mesa do contrato, linha nova de save, seletor na View nos 6 distritos, textos, bot, econsim e mutações. C1 parece barata (~3 linhas), mas muda o ritmo das encomendas e do Charter e quebra testes e o golden do bot. C2 já existe: o bloco de cota é decisão explícita e conta para o contrato.
- **Veto:** A antes do Limited Release ou em paralelo com a thread de pedidos; C1 sem econsim; campo de save novo sem testes de save antigo, lixo e ida e volta; lógica de cota fora do Core.

### creative-director — **B, com ajuste**
- O seletor por linha é microgestão numa tela de 6" e duplica a identidade do Royal Arsenal (o pátio de cotas, §2.4). Reter a peça parece punição ao público casual ("nunca falha, nunca pune", GDD §10–§11).
- A dupla contagem tira o custo de oportunidade: o contrato vira uma barra que enche sozinha. A compensação é de apresentação, não de controle: selo real na peça que conta e a barra pulsando (legível sem som), e uma partida com estandartes ao concluir.
- **Veto:** seletor por linha antes do Limited Release; GDD prometendo uma decisão que o jogo não tem; versão futura em que a peça reservada renda menos sem compensação visível ou em que o contrato vire obrigatório.

### producer — **B**
- A cota não está no critério do GDD §23 item 7 (12 contratos, bot fecha sem cumprir, 1× com ID de transação, baú fixo); o item já está em 90 e não sobe com ela. O que falta para 90% é Play Console e POCO.
- O item 6 ("1 unidade conta para 1 encomenda") não é violado: o contrato não é encomenda.
- **Veto:** A agora (colisão com a thread de pedidos e CI sem ponto); C1 agora (mudança de economia sem pedido de jogador).

## 3. Rodada 2
Dispensada: os três votaram B na Rodada 1, com vetos compatíveis entre si. O ajuste do CD (texto do GDD e do catálogo) entra nesta decisão; o alvo de experiência (selo e barra) vira pendência visual.

## 4. Decisão
- **Consenso:** sim, **B**.
- **ADR:** V30 no `PROJETO.md` §4. A peça vendida conta para a encomenda e para o contrato; o contrato é o reconhecimento da Coroa sobre o que a oficina já produz, não um desvio do balcão. A decisão "vender agora ou cumprir" fica onde já existe: no pátio de cotas do Arsenal (C2) e em aceitar ou abandonar o contrato.
- **Alternativas descartadas:** A (custo, colisão, fora do critério); C1 (economia sem dado).
- **Gatilho para revisitar (Closed / Limited Release):** taxa de contratos aceitos e concluídos baixa, playtest dizendo "nem percebi o contrato", ou mais de 90% concluídos dentro do prazo-bônus (contrato de graça). Nesse caso a menor versão é um interruptor único **"Reservar para a Coroa"** por contrato, com o desvio atrás de uma interface no Core e depois que a thread de pedidos fizer o merge; o seletor por linha segue descartado.

## 5. Despacho
| # | Raia | Dono | Entrega | Arquivos | Depende de |
|---|---|---|---|---|---|
| 1 | ADR e textos | producer (inline) | V30 no §4; §7 como decidido; GDD §11 e `CONTEUDO_V1.md` §8 sem prometer o seletor; comentário do `Contratos.cs` aponta a V30 | `docs/PROJETO.md`, `docs/GDD.md`, `docs/CONTEUDO_V1.md`, `Core/Contratos.cs` (só comentário), `CHANGELOG.md` | — |
| 2 | Selo real na peça que conta e barra pulsando (alvo do CD) | ui-programmer + art-director | pendência: depois da thread de pedidos, com foto em retrato | `View/` | thread de pedidos |

## 7. Revisão pós-código
Preenchida no PR.
