# ASO — Forge Street (G-UA-04)

2026-10-12 · Leva 8 do ORQUESTRADOR, raia LOJA/PRIVACIDADE (ficha 51) · Estado: **PROPOSTO** (nada publicado; a conta do Play Console é a decisão P9).

**Régua:** só entra o que existe no build de hoje (`PROJETO.md` §8 e `Defs.cs`). Nada de "em breve" no texto da loja: a política de metadados da Play não aceita promessa enganosa, e a ficha 51 não vende o futuro. O que depende de Wave está na tabela do §6 e só entra quando a Wave fechar.

**O que existe hoje** (conferido em `client/Assets/_FS/Scripts/Core/Defs.cs`, `main` `0af7d10`):
- 1 oficina (Old Forge) + rua lateral com joalheria; 4 produtos: espada, escudo, ferramenta e joia;
- 29 melhorias (26 de produção + 3 de decoração); 6 ajudantes (Ajudante 1–3, Mineiro, Joalheiro 1–2); esteira; balcão de 4 a 8 vagas;
- clientes com paciência, que vão embora; VIP que paga 3× por unidade; encomendas (1 por vez, sem prazo); 4 baús de marco com ouro fixo;
- cofre offline: 25% da taxa, até 2 h; rewarded opcional: Velocidade 2×/3× e "Chamar VIP" (só depois da 1ª melhoria, recarga de 5 min);
- retrato, uma mão; roda sem internet. Sem IAP. Interstitial atrás de flag desligada (V10). Textos do jogo em pt-BR, en e es desde o PR #41 (`View/Loc.cs`, idioma do aparelho ou escolha nas Configurações).

Limites da Play conferidos com `len()` do Python em cada bloco `text` abaixo (2026-10-12): títulos 24–29 de 30, descrições curtas 70–79 de 80, completas 1.384–1.465 de 4.000. Mudou um texto? Conte de novo.

---

## 1. Título (≤ 30)

| Idioma | Principal | Alternativa (teste depois) |
|---|---|---|
| pt-BR | `Forge Street: Tycoon de Forja` | `Forge Street: Ferreiro Idle` |
| en | `Forge Street: Idle Blacksmith` | `Forge Street: Blacksmith Shop` |
| es | `Forge Street: Herrero Tycoon` | `Forge Street: Forja Idle` |

<!-- titulo principal e alternativa, pt-BR / en / es -->
```text
Forge Street: Tycoon de Forja
Forge Street: Ferreiro Idle
Forge Street: Idle Blacksmith
Forge Street: Blacksmith Shop
Forge Street: Herrero Tycoon
Forge Street: Forja Idle
```

- "Forge Street" é a marca e não muda entre idiomas. O sufixo carrega a palavra de busca mais forte de cada língua.
- A Play não aceita no título emoji, CAIXA ALTA fora da marca, nem "melhor", "nº 1", "grátis" ou preço.
- A marca ainda não passou por pesquisa de nome e domínio (ficha 50) `<<CONFIRMAR>>`.

## 2. Descrição curta (≤ 80), 2 variantes para experimento

- A = **transformação** (o gancho do DM §80, sem "distrito", que só existe na Wave C).
- B = **gargalo** (a decisão central do jogo, GDD §3.1).

<!-- curta pt-BR A e B -->
```text
Comece com uma bigorna e transforme a oficina na forja mais movimentada da rua.
Funda minério, martele espadas e ache o gargalo da sua forja medieval.
```

<!-- curta en A e B -->
```text
Start with one anvil and grow your shop into the busiest forge on the street.
Smelt ore, hammer swords and find the bottleneck in your medieval forge.
```

<!-- curta es A e B -->
```text
Empieza con un yunque y convierte tu taller en la forja con más clientes.
Funde mineral, forja espadas y encuentra el atasco de tu forja medieval.
```

**Experimento (Play Console > Experimentos da página da loja):** um atributo por vez (só a descrição curta), por idioma, 50/50, métrica "instaladores retidos" (o padrão da Play), mínimo 7 dias e até a Play declarar o resultado. Só roda com a página no ar (Limited Release ou produção). O Internal e o Closed não têm tráfego de loja. Primeiro o pt-BR, depois en/es. O vencedor fica, e o perdedor vira a alternativa do teste seguinte (título ou screenshot 1, nunca dois ao mesmo tempo).

## 3. Descrição completa (≤ 4.000)

<!-- completa pt-BR -->
```text
Forge Street é um jogo de ferreiro em que a oficina cresce junto com você. Pegue minério, funda lingotes na fornalha, martele espadas na bigorna e venda no balcão para os clientes da rua. Com o ouro, compre melhorias, contrate ajudantes e transforme uma forja pequena na mais movimentada da rua.

Ache o gargalo
Cada estação mostra quando trava: a fornalha acende uma lâmpada vermelha quando a saída enche e fica cinza quando falta minério. Descubra o que está segurando a produção e invista no lugar certo.

Construa e automatize
• 4 linhas de produção: espadas, escudos, ferramentas e joias
• 29 melhorias: fornalhas, bigornas, fole, mochila, botas, vitrines e decoração
• 6 ajudantes que carregam minério, lingotes e produtos por você
• Esteira que leva os lingotes da fornalha até a bigorna
• Balcão de até 8 vagas para atender a fila inteira
• Rua lateral com joalheria e clientes nobres

Clientes, encomendas e VIP
Os clientes chegam, esperam na fila e vão embora se a espera passar do limite. Cumpra encomendas para ganhar ouro extra, abra baús nos marcos de venda e atenda clientes VIP, que pagam o triplo.

A forja rende enquanto você está fora
Feche o jogo e volte depois: o cofre junta ouro por até 2 horas.

No seu ritmo
• Jogo em retrato, com uma mão só
• Bônus opcional por anúncio: chame um cliente VIP ou acelere a produção
• Funciona sem internet (os bônus por anúncio precisam de conexão)
```

<!-- completa en -->
```text
Forge Street is a blacksmith game where your workshop grows with you. Grab ore, smelt ingots in the furnace, hammer swords on the anvil and sell them at the counter to customers on the street. Spend your gold on upgrades, hire helpers and turn a small forge into the busiest one on the street.

Find the bottleneck
Every station shows when it's stuck: the furnace lights a red lamp when its output is full and turns grey when it runs out of ore. Find what's holding production back and invest in the right place.

Build and automate
• 4 production lines: swords, shields, tools and jewelry
• 29 upgrades: furnaces, anvils, bellows, backpack, boots, display shelves and decorations
• 6 helpers who carry ore, ingots and goods for you
• A conveyor that moves ingots from the furnace to the anvil
• A counter with up to 8 slots to serve the whole line
• A side street with a jewelry shop and noble customers

Customers, orders and VIPs
Customers arrive, wait in line and leave if the wait gets too long. Complete orders for extra gold, open chests at sales milestones and serve VIP customers, who pay triple.

Your forge earns while you're away
Close the game and come back later: the vault collects gold for up to 2 hours.

At your own pace
• Portrait mode, one-handed play
• Optional ad bonuses: call a VIP customer or speed up production
• Plays offline (ad bonuses need a connection)
```

<!-- completa es -->
```text
Forge Street es un juego de herrero en el que el taller crece contigo. Recoge mineral, funde lingotes en la fragua, forja espadas en el yunque y véndelas en el mostrador a los clientes de la calle. Con el oro, compra mejoras, contrata ayudantes y convierte una forja pequeña en la más concurrida de la calle.

Encuentra el atasco
Cada estación muestra cuándo se traba: la fragua enciende una luz roja cuando la salida se llena y se pone gris cuando falta mineral. Descubre qué frena la producción e invierte en el lugar correcto.

Construye y automatiza
• 4 líneas de producción: espadas, escudos, herramientas y joyas
• 29 mejoras: fraguas, yunques, fuelle, mochila, botas, vitrinas y decoración
• 6 ayudantes que llevan mineral, lingotes y productos por ti
• Una cinta que lleva los lingotes de la fragua al yunque
• Un mostrador de hasta 8 puestos para atender toda la cola
• Una calle lateral con joyería y clientes nobles

Clientes, pedidos y VIP
Los clientes llegan, esperan en la cola y se van si la espera se alarga demasiado. Cumple pedidos para ganar oro extra, abre cofres en los hitos de ventas y atiende a clientes VIP, que pagan el triple.

Tu forja gana mientras no estás
Cierra el juego y vuelve más tarde: la caja fuerte junta oro durante hasta 2 horas.

A tu ritmo
• Juego en vertical, con una sola mano
• Bonus opcional por anuncio: llama a un cliente VIP o acelera la producción
• Funciona sin internet (los bonus por anuncio necesitan conexión)
```

Notas:
- **Nada de "sem anúncio forçado"** nem "só anúncio opcional" como promessa (V10, FS-5): o texto descreve o bônus por anúncio, que é opcional, e não diz que não existem outros anúncios. Muda só com a decisão P3.
- Os selos "Contém anúncios" e (na Wave E) "Compras no app" a própria Play mostra. Não repetir no texto.
- **en e es vão ao ar junto com o build que tem os textos em en/es** (PR #41 em diante). Página em inglês para um build só em português engana quem instala e vira avaliação ruim: confira a versão do build antes de publicar a página.
- Os nomes do jogo (cofre, encomenda, Velocidade) seguem os do build pt-BR. Na localização, alinhar en/es com os termos que a raia de localização escolher `<<CONFIRMAR>>` ("vault"/"caja fuerte", "orders"/"pedidos").

## 4. Palavras-chave de pesquisa

A Play não tem campo de palavra-chave: ela lê o título, a descrição curta e a completa. As palavras abaixo guiam o texto (2–3 menções naturais das principais, sem lista solta, que a política de metadados proíbe) e servem de base para ferramenta de ASO e campanha.

| Idioma | Principais (já no texto) | Secundárias | Fonte |
|---|---|---|---|
| pt-BR | ferreiro, forja, tycoon, idle, oficina, espadas | jogo de ferreiro, simulador, medieval, fábrica, gerenciar loja, artesanato, magnata, jogo offline, bigorna | DM §120 (forge, blacksmith, tycoon, idle, crafting, medieval, factory, shop) traduzido |
| en | blacksmith, forge, idle, tycoon, workshop, swords | crafting, medieval, factory, shop, smithy, anvil, business simulator, offline game | DM §120 |
| es | herrero, forja, tycoon, idle, taller, espadas | herrería, juego de herrero, simulador, medieval, fábrica, tienda, magnate, juego sin internet, yunque | DM §120 traduzido |

Volume de busca e concorrência por palavra: **a verificar** com ferramenta de ASO antes do Limited Release. Não há número com fonte no repositório.

## 5. Categoria

**Recomendação: Jogos > Simulação.** O jogo é um tycoon de crafting / management casual (GDD §2, DM §168), e a Simulação é onde a Play agrupa tycoon e gerenciamento. "Casual" é mais amplo, mais disputado e descreve menos o loop. Tags da Play Console (até 5): escolher entre as disponíveis algo como Idle, Magnata/Tycoon, Gerenciamento, Medieval e Estilizado `<<CONFIRMAR os nomes exatos na lista do console>>`.

Dados de contato da ficha: e-mail `<<CONFIRMAR: contato@vstack-solutions.com.br>>`, site `<<PREENCHER>>` (o mesmo domínio do `app-ads.txt`) e URL da política `<<PREENCHER>>` (`PRIVACIDADE.md` §6).

## 6. Frases que só entram quando a Wave fechar

| Frase ou recurso | Entra quando | Fonte |
|---|---|---|
| "Termine comandando um distrito inteiro", "6 distritos", mapa | Wave C (C-CORE-01..05) | `PROJETO.md` §8: 6 distritos = 15% |
| Obras-primas / Masterwork, qualidade (Fina/Superior), Hall, coleção | Wave B (B-CORE-03/08) | §8: 0% |
| Projetos (Blueprints), Renome, encomendas de 8 tipos | Wave B | §8 |
| Contratos reais, eventos, temporadas, Pass, diárias e conquistas | Wave D | §8 |
| "35+ produtos" | Waves B/C | §8: 4 produtos |
| "Sem anúncio forçado" / "sem interstitial" | decisão P3 depois do A/B (V10) | `CRIATIVOS_BANCO_V1.md` |
| Pacotes, Remove Ads / Pacote do Mestre | Wave E | GDD §17.3 |
| "Em português, inglês e espanhol" | localização no build | DM §109 |

## 7. Roteiro das 8 screenshots

Tudo sai do jogo real, com os presets de captura que já existem (`View/Captura.cs`, tabela em `CRIATIVOS.md` "Presets de captura"). Cada preset grava quadros 1080×1920 (JPG 95, 9:16, dentro do que a Play aceita). A screenshot é um quadro escolhido. Mesmo comando, mesmos quadros: o número do quadro reproduz a peça.

```bash
cd client/Builds/win
Q="$(cygpath -m "$TEMP")/fs_loja"; W="-screen-fullscreen 0 -screen-width 540 -screen-height 960"
./ForgeStreet.exe $W -preset <nome> -record "$Q/<nome>" -logFile "$Q/<nome>.log"   # -record já liga o -testsession
grep -E "RECORD OK|PRESET <nome> ok" "$Q/<nome>.log"
```

A legenda vai na faixa de cima (fora da HUD), curta para ler na miniatura, com a fonte da marca (decisão P11; hoje o jogo usa Arial). A peça passa pelo gate §73 e lê a 48 px (G-UA-03).

| # | Preset | Quadro sugerido (conferir na revisão) | Legenda pt-BR | en | es |
|---|---|---|---|---|---|
| 1 | `full_district` | último (`f00449`, oficina inteira com os luxos) | DA BIGORNA À OFICINA CHEIA | FROM ONE ANVIL TO A FULL SHOP | DE UN YUNQUE A UN TALLER LLENO |
| 2 | `conveyor` | um quadro com lingote no meio da esteira | AUTOMATIZE A PRODUÇÃO | AUTOMATE PRODUCTION | AUTOMATIZA LA PRODUCCIÓN |
| 3 | `first_worker` | ~5 s depois da contratação (`f00150`), ajudante carregando | CONTRATE AJUDANTES | HIRE HELPERS | CONTRATA AYUDANTES |
| 4 | `huge_queue` | `f00030` (fila 8/8 parada; os 12 primeiros ficam fora) | FILA CHEIA? ACHE O GARGALO | FULL LINE? FIND THE BOTTLENECK | ¿COLA LLENA? BUSCA EL ATASCO |
| 5 | `vip` | VIP no balcão com a pílula "×N" | CLIENTES VIP PAGAM 3× | VIP CUSTOMERS PAY 3× | LOS VIP PAGAN 3× |
| 6 | `order` | ~17 s (`f00510`), "Encomenda entregue! +30" | CUMPRA ENCOMENDAS | COMPLETE ORDERS | CUMPLE PEDIDOS |
| 7 | `district_unlock` | depois da compra do Corredor, câmera na rua lateral (~`f00200`) | ABRA UMA NOVA RUA | OPEN A NEW STREET | ABRE UNA CALLE NUEVA |
| 8 | `offline` | `f00060`, painel "Bem-vindo de volta!" com o cofre real de 2 h | RENDE ATÉ 2 H FORA DO JOGO | EARNS UP TO 2 H WHILE AWAY | GANA HASTA 2 H SIN JUGAR |
| reserva | `start` | ~2 s, oficina com uma bigorna | COMECE COM UMA BIGORNA | START WITH ONE ANVIL | EMPIEZA CON UN YUNQUE |

- Ordem: a 1 e a 2 vendem a transformação e a automação (as que mais aparecem na busca); a 4 é a decisão; a 8 fecha com a volta ao jogo.
- `full_district` e `district_unlock` são **interinos** até a Wave C: mostram a oficina e a rua lateral, nunca um "distrito". A legenda não diz "distrito".
- **en/es:** a HUD sai no idioma do quadro com `-lang en|es` junto do `-preset` (ex.: `-preset vip -lang es`); o `SHOT` do log grava `idioma=`.
- A segunda sequência ("Ache o gargalo", `CRIATIVOS_BANCO_V1.md` §c) fica para o experimento de screenshots depois do de descrição curta.
- Ícone, feature graphic e vídeo: G-UA-03 / C-ART-14, fora deste documento. As 4 variantes de ícone, o logo e a feature graphic estão em `arte/marca/` (ASSETS §13, PROPOSTO). O vídeo ainda falta.

## 8. Decisões do Vinicius

| # | Decisão | Recomendação |
|---|---|---|
| D-ASO-1 | Título principal por idioma | os da coluna "Principal" do §1; a alternativa entra no 2º experimento |
| D-ASO-2 | Categoria | Simulação (§5) |
| D-ASO-3 | Publicar en/es? | Sim, com build a partir do PR #41 (localização pt-BR/en/es); fotos com `-lang` (§3, §7) |
| D-ASO-4 | Marca "Forge Street" sem pesquisa de nome e domínio | pesquisar antes da página pública (ficha 50) |
| D-ASO-5 | E-mail, site e URL da política na ficha | os mesmos do `PRIVACIDADE.md` (D-PRIV-3/4) |
