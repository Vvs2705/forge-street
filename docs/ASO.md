# ASO — Forge Street (G-UA-04)

2026-10-12 · Leva 8 do ORQUESTRADOR, raia LOJA/PRIVACIDADE (ficha 51) · Estado: **PROPOSTO** (nada publicado; a conta do Play Console é a decisão P9).

**Régua:** só entra o que existe no build de hoje (`PROJETO.md` §8 e `Defs.cs`). Nada de "em breve" no texto da loja: a política de metadados da Play não aceita promessa enganosa, e a ficha 51 não vende o futuro. O que depende de Wave está na tabela do §6 e só entra quando a Wave fechar.

**O que existe hoje** (2026-10-24, `main`; `PROJETO.md` §8 ~83%):
- 6 distritos no build (Old Forge, Rua dos Armeiros, Bairro Artesão, Arsenal Real, Fundição Arcana, Grande Forja), cada um com uma mecânica nova; 38 produtos vendáveis; estações com níveis II–V; workers, turmas e managers por distrito;
- qualidade Normal/Fina/Superior com o golpe de mestre; 60 obras-primas no Hall; Coleção; encomendas Orders 2.0 (8 tipos); 12 contratos reais; Renome do Império em 10 níveis;
- 41 diárias, 17 semanais, 106 conquistas, login de 7 dias; 10 eventos; Passe de 30 níveis com trilha grátis (premium atrás de flag desligada, fora do texto);
- cofre offline: 25% da taxa, até 2 h; rewarded opcional (VIP, Velocidade e outros), só depois da 1ª melhoria; intersticial atrás de flag desligada (V10); IAP desligado até a chave da Play (fora do texto);
- retrato, uma mão; roda sem internet; textos em pt-BR, en e es (`View/Loc.cs`).

Limites da Play conferidos com `len()` do Python em cada bloco `text` abaixo (títulos e curtas em 2026-10-12: 24–29 de 30 e 70–79 de 80; completas reescritas em 2026-10-24: 1.845, 1.847 e 1.928 de 4.000). Mudou um texto? Conte de novo.

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
Forge Street é um jogo de ferreiro em que a oficina cresce junto com você. Pegue minério, funda lingotes na fornalha, martele espadas na bigorna e venda no balcão para os clientes da rua. Com o ouro, compre melhorias, contrate ajudantes e transforme uma forja pequena numa rua inteira de oficinas.

Ache o gargalo
Cada estação mostra quando trava: fica cinza quando falta insumo e acende o vermelho quando a saída enche. Descubra o que está segurando a produção e invista no lugar certo.

Seis distritos, uma mecânica nova em cada
• Old Forge: espadas, escudos, ferramentas e joias, com esteira e ajudantes
• Rua dos Armeiros: ligas e peças montadas de várias partes
• Bairro Artesão: bancadas que você configura e clientes por família
• Arsenal Real: cotas com prazo para o exército do rei
• Fundição Arcana: peças energizadas nos altares
• Grande Forja: síntese com peças dos outros distritos e lotes de exportação
Mais de 35 produtos, melhorias, níveis de estação e uma equipe de ajudantes e mestres para cada distrito.

Qualidade e obras-primas
Acerte o golpe de mestre na bigorna e a peça sobe de qualidade: Normal, Fina, Superior. Peças perfeitas viram obras-primas para o Hall, e a Coleção guarda cada marco que você alcança.

Encomendas, contratos reais e Renome
Cumpra encomendas de vários tipos, aceite contratos reais e suba o Renome do seu império para abrir os próximos distritos.

Metas, eventos e Passe
Diárias, semanais e conquistas, eventos com trilha própria e um Passe de temporada com trilha grátis.

A forja rende enquanto você está fora
Feche o jogo e volte depois: o cofre junta ouro por até 2 horas.

No seu ritmo
• Jogo em retrato, com uma mão só
• Em português, inglês e espanhol
• Bônus opcional por anúncio: chame um cliente VIP ou acelere a produção
• Funciona sem internet (os bônus por anúncio precisam de conexão)
```

<!-- completa en -->
```text
Forge Street is a blacksmith game where your workshop grows with you. Grab ore, smelt ingots in the furnace, hammer swords on the anvil and sell them at the counter to customers on the street. Spend your gold on upgrades, hire helpers and turn a small forge into a whole street of workshops.

Find the bottleneck
Every station shows when it's stuck: it turns grey when it runs out of input and lights up red when its output is full. Find what's holding production back and invest in the right place.

Six districts, a new mechanic in each
• Old Forge: swords, shields, tools and jewelry, with a conveyor and helpers
• Armorer's Row: alloys and goods assembled from several parts
• Artisan Quarter: workbenches you configure and customers by family
• Royal Arsenal: quotas with deadlines for the king's army
• Arcane Foundry: goods energized at the altars
• Grand Forge: synthesis with parts from the other districts and export batches
More than 35 products, upgrades, station levels and a team of helpers and masters for each district.

Quality and Masterworks
Land the master strike on the anvil and the piece goes up in quality: Normal, Fine, Superior. Perfect pieces become Masterworks for the Hall, and the Collection keeps every milestone you reach.

Orders, royal contracts and Renown
Fulfil orders of many kinds, take on royal contracts and raise your empire's Renown to open the next districts.

Goals, events and the Pass
Dailies, weeklies and achievements, events with their own track and a season Pass with a free track.

Your forge earns while you're away
Close the game and come back later: the vault collects gold for up to 2 hours.

At your own pace
• Portrait mode, one-handed play
• In English, Portuguese and Spanish
• Optional ad bonuses: call a VIP customer or speed up production
• Plays offline (ad bonuses need a connection)
```

<!-- completa es -->
```text
Forge Street es un juego de herrero en el que el taller crece contigo. Recoge mineral, funde lingotes en la fragua, forja espadas en el yunque y véndelas en el mostrador a los clientes de la calle. Con el oro, compra mejoras, contrata ayudantes y convierte una forja pequeña en toda una calle de talleres.

Encuentra el atasco
Cada estación muestra cuándo se traba: se pone gris cuando falta material y se enciende en rojo cuando la salida se llena. Descubre qué frena la producción e invierte en el lugar correcto.

Seis distritos, una mecánica nueva en cada uno
• Vieja Forja: espadas, escudos, herramientas y joyas, con cinta y ayudantes
• Calle Armeros: aleaciones y piezas montadas con varias partes
• Barrio Artesano: mesas de trabajo que configuras y clientes por familia
• Arsenal Real: cuotas con plazo para el ejército del rey
• Fundición Arcana: piezas energizadas en los altares
• Gran Forja: síntesis con piezas de los otros distritos y lotes de exportación
Más de 35 productos, mejoras, niveles de estación y un equipo de ayudantes y maestros para cada distrito.

Calidad y obras maestras
Acierta el golpe maestro en el yunque y la pieza sube de calidad: Normal, Fina, Superior. Las piezas perfectas se vuelven obras maestras para el Salón, y la Colección guarda cada hito que alcanzas.

Pedidos, contratos reales y Renombre
Cumple pedidos de muchos tipos, acepta contratos reales y sube el Renombre de tu imperio para abrir los siguientes distritos.

Metas, eventos y Pase
Diarias, semanales y logros, eventos con su propia ruta y un Pase de temporada con ruta gratis.

Tu forja gana mientras no estás
Cierra el juego y vuelve más tarde: la caja fuerte junta oro durante hasta 2 horas.

A tu ritmo
• Juego en vertical, con una sola mano
• En español, portugués e inglés
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
| ~~"6 distritos", mapa~~ | Wave C | **no texto desde 2026-10-24** (6 distritos no build) |
| ~~Obras-primas, qualidade (Fina/Superior), Hall, coleção~~ | Wave B | **no texto desde 2026-10-24** |
| ~~Renome, encomendas de vários tipos~~ (projetos fora do texto) | Wave B | **no texto desde 2026-10-24** |
| ~~Contratos reais, eventos, Passe (trilha grátis), diárias e conquistas~~ | Wave D | **no texto desde 2026-10-24**; o premium do Passe só com a loja ligada |
| ~~"Mais de 35 produtos"~~ | Waves B/C | **no texto desde 2026-10-24** (38 vendáveis) |
| "Sem anúncio forçado" / "sem interstitial" | decisão P3 depois do A/B (V10) | `CRIATIVOS_BANCO_V1.md` |
| Pacotes, Remove Ads / Pacote do Mestre | Wave E | GDD §17.3 |
| ~~"Em português, inglês e espanhol"~~ | localização no build | **no texto desde 2026-10-24** |

## 7. Roteiro das 8 screenshots

Tudo sai do jogo real, com os presets de captura que já existem (`View/Captura.cs`, tabela em `CRIATIVOS.md` "Presets de captura"). Cada preset grava quadros 1080×1920 (JPG 95, 9:16, dentro do que a Play aceita). A screenshot é um quadro escolhido. Mesmo comando, mesmos quadros: o número do quadro reproduz a peça.

```bash
cd client/Builds/win
Q="$(cygpath -m "$TEMP")/fs_loja"; W="-screen-fullscreen 0 -screen-width 540 -screen-height 960"
./ForgeStreet.exe $W -preset <nome> -record "$Q/<nome>" -logFile "$Q/<nome>.log"   # -record já liga o -testsession
grep -E "RECORD OK|PRESET <nome> ok" "$Q/<nome>.log"
```

A legenda vai na faixa de cima (fora da HUD), curta para ler na miniatura, com a fonte da marca (decisão P11: **Lilita One**, a dos títulos da interface v2, #87). A peça passa pelo gate §73 e lê a 48 px (G-UA-03).

**Feitas (2026-10-16): `arte/loja/screenshots/<pt|en|es>/<n>_<preset>.jpg`**, 8 × 3 idiomas, 1080 × 1920 JPG. Saem de um build **sem a marca "Development Build"** (`Setup.BuildWindowsLoja`, em `client/Builds/win_loja`) e do `client/tools/loja_screenshots.py`: `capturar` roda cada preset com `-lang` e guarda só o quadro escolhido (os outros ~0,7 MB por quadro são apagados), `compor` põe a legenda na faixa de cima (Lilita One creme com contorno, sobre a ardósia e a brasa do ícone A3) e o quadro do jogo a 86% da largura com aro dourado. Quadros conferidos numa folha de candidatos (1 a cada 2 s): os da tabela, com o VIP em `f00240` (VIP de coroa no balcão com o "×3") e a esteira em `f00180` (lingote na esteira e a venda "+10").

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
- **A segunda sequência ("Ache o gargalo", `CRIATIVOS_BANCO_V1.md` §c) está pronta** (2026-10-23): `arte/loja/screenshots_b/<lang>/` (8 × pt/en/es, 1080 × 1920; `python client/tools/loja_screenshots.py capturar|compor --seq B`). Peças: fila cheia, o gargalo (fornalha cheia com a lâmpada vermelha), o menu com os cartões, o 3×, a encomenda, o cofre, a oficina completa e a forja de perto (no lugar do "sem anúncio forçado", que espera o FS-5/P3). Entra no teste de screenshots depois do de descrição curta.
- Ícone, feature graphic e vídeo: G-UA-03 / C-ART-14, fora deste documento. As 4 variantes de ícone, o logo e a feature graphic estão em `arte/marca/` (ASSETS §13, PROPOSTO). **Vídeo de gameplay:** `arte/loja/video/gameplay.mp4` (35 s, 1080 × 1920, 30 fps, H.264, 7,3 MB), gravado do build sem a marca de desenvolvimento: a esteira com a câmera perto, as fornalhas a 3× e a oficina completa (`loja_screenshots.py video`). **Corte de 30 s** (para o caso de a Play ou um canal pedir até 30 s): `arte/loja/video/gameplay_30s.mp4` (30,0 s, 1080 × 1920, 30 fps, H.264 CRF 23, sem áudio, 7,9 MB, SHA-256 `3e60742e…9e5e80f`), os 30 primeiros segundos do original: tira só 5 s do fim do bloco da oficina completa, que repete; esteira, fornalhas a 3× e a oficina completa (10 s) ficam. Refazer: `ffmpeg -i gameplay.mp4 -t 30 -c:v libx264 -preset slow -crf 23 -pix_fmt yuv420p -r 30 -an -movflags +faststart gameplay_30s.mp4`.

## 8. Decisões do Vinicius

| # | Decisão | Recomendação |
|---|---|---|
| D-ASO-1 | Título principal por idioma | os da coluna "Principal" do §1; a alternativa entra no 2º experimento |
| D-ASO-2 | Categoria | Simulação (§5) |
| D-ASO-3 | Publicar en/es? | Sim, com build a partir do PR #41 (localização pt-BR/en/es); fotos com `-lang` (§3, §7) |
| D-ASO-4 | Marca "Forge Street" sem pesquisa de nome e domínio | pesquisar antes da página pública (ficha 50) |
| D-ASO-5 | E-mail, site e URL da política na ficha | os mesmos do `PRIVACIDADE.md` (D-PRIV-3/4) |
