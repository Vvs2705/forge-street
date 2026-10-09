# Portfólio de jogos V-STACK — descritivo breve (2026-10-09)

Nove jogos mobile (Android primeiro, retrato, F2P), cada um com um GDD próprio. Uma validação multiagente em 2026-10-06 escolheu a ordem (pesquisa de mercado, design, técnica e revisão cruzada). Nenhum foi testado com público ainda: teste de criativos e coorte real decidem.

| # | Jogo | Gênero | O que é | Situação |
|---|---|---|---|---|
| 01 | **Rune Relay** | Puzzle "flow sort" | Orbes de mana coloridos saem das fontes em fila visível; o jogador gira runas-canal, divisores e comportas para cada cor chegar ao receptor certo, com 3 liberações por nível. | **GO, 1º MVP.** v0.1 jogável (30 níveis com solução única, APK). Pausado até o Forge Street fechar. Repo `Vvs2705/rune-relay` |
| 02 | **Skyhold Cargo** | Puzzle espacial "cabe/não cabe" | Arrastar cargas com formato, peso e regras (frágil, perigosa) para dentro do porão de um avião antes da decolagem. | Plano B do puzzle (reusaria o gerador e o solver do Rune Relay) |
| 03 | **Forge Street** | Idle/tycoon arcade | Você é o ferreiro: leva minério à fornalha, lingote à bigorna e espada ao balcão; contrata ajudantes e cresce de uma bigorna para uma rua inteira de forjas e uma joalheria. | **Em desenvolvimento (este repositório).** v0.5.1 testada no POCO F4 a 60 fps; v0.6 pronta no PR #5 |
| 04 | **Beast Haven** | Idle/tycoon + coleção de criaturas | Cuidar de um santuário: alimentar, limpar e colher recursos para atrair e colecionar criaturas. | NO-GO nesta rodada (custo de animação de quadrúpedes; atenção ao ECA Digital com itens aleatórios) |
| 05 | **Scrapstorm** | Survivor-like / ação casual | Runs curtas com um dedo: o mech atira sozinho, você desvia e monta peças de sucata durante a partida. | Depois (mercado já tem "monte seu mech") |
| 06 | **Dungeon Courier** | Ação casual de objetivo | Atravessar salas de masmorra levando um pacote/relíquia que muda as regras, e sair vivo. | NO-GO agora |
| 07 | **Underground Inc.** | Idle/tycoon de mineração | Uma mineradora que começa na superfície e desce camada por camada até cavernas, ruínas, criaturas e minerais impossíveis. | Alternativa para o slot idle (disputa com o Forge Street) |
| 08 | **Mystic Kitchen** | Merge-2 + gestão + narrativa | Reconstruir uma taverna mágica abandonada até virar o maior restaurante fantástico do reino. | **NO-GO** (mercado de merge saturado) |
| 09 | **Tiny Airport Empire** | Idle/tycoon de gestão | Começar numa pista rural e construir uma rede global de aeroportos. | NO-GO nesta rodada (pode virar reskin se o idle validar) |

**Ordem atual:** terminar o Forge Street (slot idle), depois voltar ao Rune Relay (slot puzzle). Guardrails para todos: uma moeda no MVP, offline com teto de 2 h, nada de item aleatório pago (ECA Digital, Lei 15.211/2025), interstitial desligado na v0.1.
