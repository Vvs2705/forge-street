# Benchmark de mercado e loop — Forge Street v0.4.1

**Data:** 2026-10-08 · **Raia:** benchmark de mercado e loop (ORQUESTRADOR; personas Market Research + Game Development Director) · **Pedido:** "faça uma análise agora de jogos similares e veja o que estamos perdendo para esses jogos" / "compare com outros jogos e note que nosso jogo ainda está bem cru".

**Método.** Dez jogos reais (seis da família idle arcade / tycoon e quatro de forja), pesquisados na web em 08/10/2026 só por leitura pública: lojas, desconstruções, guias e reviews. Os fatos que sustentam a recomendação foram conferidos numa segunda leitura direta: Burger Please! e My Perfect Hotel (arpubrothers), Pizza Ready! (LevelWinner) e My Mini Mart (PocketGamer.biz/wnhub). O estado do Forge Street saiu do código (`client/Assets/_FS/Scripts/Core/Sim.cs`, `Defs.cs`), do `README.md`, do `docs/GDD.md` e do `docs/BALANCE.md`.

**Legenda.** **[F]** fato com fonte · **[I]** inferência nossa · **[O]** opinião/recomendação. "não confirmado" = nenhuma fonte legível confirmou. Instalações e notas são da Play Store (EUA) quando a página abriu. Quando não abriu, vêm do ApkCombo e a linha diz isso. A maior parte das mecânicas vem de guias de 2022–2024, e as versões atuais podem ter mudado.

Arte e visual ficam com a outra raia (`docs/BENCHMARK_VISUAL.md`). Aqui só entram quando fazem parte do loop.

---

## 0. Resumo em 6 linhas

1. **[F]** Os líderes do formato (My Perfect Hotel, Burger Please!, Pizza Ready!, My Mini Mart) têm 100M+ instalações cada. Todos usam o mesmo esqueleto: carga limitada que sobe por upgrade, **dinheiro físico recolhido à mão**, caixa/recepção que **ganha atendentes e um 2º canal de venda** (drive-thru), um **escritório de RH com níveis** (velocidade/capacidade/contratação), **troca de loja/mapa** depois de ~30–40 min, rewarded em todo canto e interstitial pesado.
2. **[F]** Contra a trava de "mãos cheias" a resposta do gênero é **uma lixeira**: Pizza Ready! (lixo ocupa a mão até ir para a lixeira), My Mini Mart (lixeira grátis na zona de $95, "para descartar recursos sobrando") e My Perfect Hotel (lixeiras para descartar o que se carrega, indício não verificado). Nenhum dos líderes deixa o jogador sem um lugar para largar a pilha.
3. **[F]** O balcão cresce por **pontos de atendimento em paralelo**, não por fila mais longa: recepcionistas 1→3 (My Perfect Hotel), caixa + drive-thru (Burger Please!, Pizza Ready!), e o nível da estação dá velocidade, receita e **assentos** (Burger Please!).
4. **[F]** O Forge Street tem a cadeia mais legível do grupo, mas não tem: lixeira/largar, balcão com níveis, estação com níveis, dinheiro físico, escritório com níveis, missões, rewarded, troca de mapa, skins, diárias ou passe.
5. **[F]/[I]** No tema forja **não há idle arcade popular**. O mais próximo é o *Forge & Fortune* da própria Supercent, com 1K+ instalações. O tema é espaço livre, mas também é risco: os hits do formato usam temas do cotidiano.
6. **[O]** Para a v0.5, priorizar: (1) lixeira + salvar a pilha, (2) balcão em 3 níveis 4→6→8, (3) níveis por estação com troca de aparência + escritório, (4) rewarded de mentira (opt-in medido, sem SDK), (5) dinheiro físico no balcão e 1º minuto construindo, em A/B no bot.

---

## 1. Jogos analisados

| # | Jogo | Estúdio / publisher | Lançamento | Play: instalações · nota | Por que é similar | Fontes |
|---|---|---|---|---|---|---|
| 1 | **My Perfect Hotel** | Redux Games / SayGames | Android 13/04/2022 (soft), global ago/2022 | 100M+ · 4,5 (~1,85M aval., via ApkCombo) | Idle arcade de carregar e entregar; a recepção faz o papel do balcão; cadeia lavanderia → quarto | [SayGames](https://say.games/news/my-perfect-hotel-hits-fifty-million-downloads/) · [arpubrothers](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) · [ApkCombo](https://apkcombo.com/my-perfect-hotel/com.master.hotelmaster/) |
| 2 | **Burger Please!** | Supercent | iOS 09/02/2023 | 100M+ · 4,2–4,4 (fontes divergem) | Produzir → carregar → caixa e drive-thru; RH; troca de loja | [ApkCombo](https://apkcombo.com/burger-please/io.supercent.burgeridle/) · [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) · [mwm.ai](https://mwm.ai/apps/burger-please/1668713081) |
| 3 | **Pizza Ready!** | Supercent | jun–jul/2023 (Play 04/07/2023) | 100M+ · 4,4 (2,91M aval.) | Cadeia em etapas (forno → embalagem → balcão/drive-thru), lixeira, carga de 1 item no início | [Play](https://play.google.com/store/apps/details?id=io.supercent.pizzaidle) · [LevelWinner](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/) · [Gamigion](https://www.gamigion.com/pizza-ready-by-supercent-comprehensive-breakdown/) |
| 4 | **My Mini Mart** | Supersonic Studios (pacote `com.KisekiGames.smart`) | 12/12/2021 | 100M+ · 3,9 (704K aval.) | Cadeia canteiro → (galinha/liquidificador) → prateleira → caixa; lixeira; caixa contratável | [Play](https://play.google.com/store/apps/details?id=com.KisekiGames.smart) · [PocketGamer.biz](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) |
| 5 | **Idle Lumber Chopper Empire Inc** | Supercent | 10/09/2024 | 10M+ · 4,2 (70,5K aval.) | Cadeia industrial (tora → serraria → caminhão), a mais parecida com minério → lingote → espada | [Play](https://play.google.com/store/apps/details?id=dasi.prs2.lumberchopper) · [App Store](https://apps.apple.com/us/app/idle-lumber-chopper-empire-inc/id6738272884) |
| 6 | **Eatventure** | Lessmore GmbH | 28/02/2022 | 50M+ · 4,7 (421K aval.) | Idle de toque (sem carregar), mas é a referência de meta: cidades, equipamento, eventos e só rewarded | [Play](https://play.google.com/store/apps/details?id=com.hwqgrhhjfd.idlefastfood) · [LevelWinner](https://www.levelwinner.com/eatventure-beginners-guide-tips-tricks-strategies/) · [Fandom](https://eatventure-games.fandom.com/wiki/Cities) |
| 7 | **Shop Titans** | Kabam (comprou a Riposte em 2019) | jun/2019 | 5M+ · 4,2 (182K aval.) | Loja de crafting/forja com balcão, prateleiras e clientes heróis | [Play](https://play.google.com/store/apps/details?id=com.ripostegames.shopr) · [PocketGamer](https://www.pocketgamer.com/shop-titans/guide/) · [mobi.gg](https://mobi.gg/en/tips/shop-titans-beginner-guide/) |
| 8 | **Forge Master** | Lessmore GmbH, segundo a Play (não confirmado em 2ª fonte) | ~ago/2025 (não confirmado) | 1M+ · 4,5 (22,1K aval.) | Forja idle: cada upgrade da bigorna libera uma era | [Play](https://play.google.com/store/apps/details?id=com.hariwn.legendofcivilizations) · [App Store](https://apps.apple.com/us/app/-/id6746636289) |
| 9 | **Blade Forge 3D** | Kwalee | não confirmado | 10M+ · 4,4 (149K aval.) | Hyper-casual de forja (forjar, temperar, testar), aposta na satisfação do martelar | [Play](https://play.google.com/store/apps/details?id=com.kwalee.bladeforge3d) |
| 10 | **Forge & Fortune** | Supercent (pacote `com.NokoGames.ForgeFortune`) | iOS dez/2025 (não confirmado) | 1K+ · 2,9 (23 aval.) | **Idle arcade de forja quase igual ao nosso** (minerar → forjar → expor em racks → vender), mas sem tração | [Play](https://play.google.com/store/apps/details?id=com.NokoGames.ForgeFortune) |

Menção extra: **Idle Weapon Shop** (Hello Games Team), 5M+ · 4,6. Loja de armas com clientes misteriosos à noite e roupas do lojista que atraem clientes. Não confirmamos se usa joystick. [Play](https://play.google.com/store/apps/details?id=com.hg.idleweaponshoptycoon.android)

---

## 2. Tabela comparativa (o que cada um tem × Forge Street)

| Sistema | MPH | Burger Please! | Pizza Ready! | My Mini Mart | Shop Titans / Forge Master | **Forge Street v0.4.1** |
|---|---|---|---|---|---|---|
| Carga inicial do jogador | não confirmado; sobe com tokens | **2** itens; sobe com Upgrade Player, skins e boost | **1** item; sobe no Manager Office (5 níveis) + luvas temporárias | ~3 (review); 8 em upgrade (pergunta de usuário) | — | **3** → 6 (Mochila, 1 nível) |
| Mistura de tipos na pilha | não confirmado | não confirmado | **um tipo por vez**: lixo bloqueia pegar pizza | não confirmado | — | **um tipo por vez** |
| Válvula de "mãos cheias" | lixeiras (indício) | não confirmado | **lixeira** | **lixeira** (grátis na zona de $95) | baú com limite por material (ST) | **nenhuma** |
| Dinheiro | **pilha na recepção, recolhida à mão** | recolhido no caixa/drive-thru (não explícito) | **só o jogador recolhe** | caixa enche, jogador recolhe; ladrão se acumular | — | **cai direto no HUD** |
| Balcão/caixa evolui | 1 → 3 recepcionistas automáticos | caixa $100 + upgrade $100; **drive-thru**; nível de estação dá **assentos** | balcão + embalagem + **drive-thru** (capacidade/velocidade); fábrica com esteira | caixa (funcionário) liberado depois do liquidificador | upgrade do balcão (ST) | fila 4 → 6 e estoque 5 → 10 (Vitrine, 1 nível); balcão sem níveis |
| Estações com níveis | quartos sobem de nível (decoração) | **workstation level-up** (+estrelas, velocidade, receita, assentos) | upgrades de capacidade/velocidade | upgrades de máquinas | prédio de artesão sobe de nível (ST); **bigorna muda de era** (FM) | estações **binárias** (compra ou não); upgrades globais de 1 nível |
| RH / escritório | upgrades de velocidade | **RH**: velocidade, capacidade, contratar (até 10) | **HR** + **Manager**, 3 × 5 níveis | ajudante, caixa, repositor | — | 6 ajudantes de papel fixo; "Ajudantes ágeis" (1 nível) |
| Expansão | Hotel 1 → Beach → Mountain → Rising Sun, **moeda por hotel** | formatura no nível 11 → nova loja | mapa de lojas + viagem rápida | **novo mercado aos 30–40 min** | expansão de loja (ST); eras (FM) | 2 áreas no mesmo mapa + 3 luxos |
| Meta | Hotel Pass, login diário, missões diárias, pet, evento | Burger Funds (passe), skins com bônus, missões, evento de 6 dias | uniforme por loja, skins por 3 anúncios, passe US$8,99 | moeda premium (coupons) | guilda, coleção, eventos (ST); PvP, clãs, passe (FM) | 4 baús de marco; dica contextual |
| Rewarded | VIP, +50% velocidade 3 min, **maleta de dinheiro**, recursos, x2 no nível | Money Pocket, Capacity Up, Speed Up, x2, contratar, skins, robô de limpeza | x2 offline, upgrades por anúncio, ícones flutuantes, skins | coupons por anúncio | NPC opcional (ST) | **nenhum** |
| Interstitial | muito frequente (528 vídeos em 19 sessões) | < 1/min; ~30 na 1ª sessão; banner | pausa de 15 s a cada 1–2 min (reviews) | ~1/min (reviews); pop-up removível por IAP | ST/FM: sem anúncios marcados | nenhum |
| IAP | No Ads ~US$10, gemas, pet | No Ads US$3,99, VIP semanal/mensal | No Ads US$3,99, semanal US$5,99, Pro US$19,99 | Remove Ads US$4,99 | gemas, assinatura, passe US$9,99 | nenhum |
| Offline | não confirmado | recompensa offline, **x2 por anúncio** | com teto, **x2 por anúncio** | não confirmado | FM: teto de 4 h (guia de fã) | 25% × até 2 h, teto 2× o upgrade travado; **sem x2** |

Fontes da tabela: as mesmas da §1, mais [Pizza Ready App Store](https://apps.apple.com/us/app/pizza-ready/id6450917563), [Burger Please App Store](https://apps.apple.com/us/app/burger-please/id1668713081), [MPH App Store](https://apps.apple.com/us/app/my-perfect-hotel/id1635760774), [My Mini Mart App Store](https://apps.apple.com/us/app/my-mini-mart/id1592004814), [wnhub (My Mini Mart, RU)](https://wnhub.io/ru/news/game-design/item-40717), [PocketGamer: dicas de My Mini Mart](https://www.pocketgamer.com/my-mini-mart/tips-and-tricks/), [Clashiverse (Forge Master)](https://clashiverse.com/forge-master-beginner-guide/) e [Shop Titans 3.7.0](https://playshoptitans.com/news/version-3-7-0-release-notes).

---

## 3. Primeiros 60 segundos (onboarding)

| Jogo | O que acontece | Fonte |
|---|---|---|
| My Perfect Hotel | **[F]** Não tem tutorial. O jogador começa com $50 e atende o 1º hóspede na recepção. Abre o quarto 1 por $30 e o quarto 2 por $30. A 1ª contratação aparece aos 1:46 e o 1º rewarded (maleta de $100) aos 2:19. | [arpubrothers](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) |
| Burger Please! | **[F]** O jogador recolhe $180 fora da loja e paga $25 de entrada. Depois compra mesa ($5), máquina ($50) e caixa ($100). Duas burgers rendem $10, e limpar a mesa rende $6. Setas vermelhas guiam. | [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) |
| Pizza Ready! | **[F]** Não tem botão de play; só setas iniciais. **Não confirmado:** a Supercent teria redesenhado o FTUE por causa de setas fora da tela e 8–9 s de tempo morto, mas só vimos o resumo de busca do post. | [Gamigion](https://www.gamigion.com/pizza-ready-by-supercent-comprehensive-breakdown/) · [Supercent blog](https://medium.com/supercent-blog/redesigning-the-ftue-of-pizza-ready-11eda193fa2e) |
| My Mini Mart | **[F]** A 1ª compra é o **caixa**, depois a prateleira de tomate e só então o canteiro. No 1º ciclo, nenhum deslocamento passa de ~2 s, e o autor chama de erro comum correr mais de 5 s. A câmera faz pan até cada ponto novo. | [PocketGamer.biz](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) |
| **Forge Street** | **[F]** Oficina **já construída** (depósito, fornalha, bigorna, balcão). A dica no topo manda pegar minério. 1ª venda aos 0:21 no bot; para humanos a estimativa é 45–75 s. 1ª compra (Fole) aos 1:04. | `docs/BALANCE.md` §2, §15 |

**[I]** Nos líderes, o 1º minuto é **construir**: cada moeda vira um objeto novo na tela (entrada, mesa, máquina, caixa). No Forge Street o 1º minuto é **operar** uma oficina que já está pronta. A sensação de "construí isso" só chega com o Fole, e ele é invisível porque fica no menu. Isso contribui para o "cru" que o Vinicius sentiu, e também contradiz o criativo #1 ("comecei com uma bigorna vazia").

---

## 4. Regra de carga e a trava de "mãos cheias"

**Estado do Forge Street [F]:** `CanPick` recusa pegar um item de tipo diferente do que está na mão. A capacidade é 3 (6 com a Mochila). **Não existe largar.** O `BALANCE.md` §9 já registrou uma trava de 45 min: "a mão está ocupada e não existe 'largar'". O remendo vigente é o cliente comprar direto da vitrine quando a fila está cheia. Ele resolve a fila, mas não a mão: com espadas na mão e o estoque de espadas no teto (5/5), o jogador não pode recolher escudos nem depositar nada até alguém comprar uma espada. O `Save` grava ouro, estações, estoque e posição, mas **não grava a pilha da mão nem a dos ajudantes**.

**O que os similares fazem [F]:**
- **Pizza Ready!:** jogador e equipe começam com 1 item, um tipo por vez. Segurando lixo, o jogador não pega pizza até jogá-lo na **lixeira**. A capacidade sobe no Manager Office (jogador) e no HR Office (equipe), 5 níveis cada, e as luvas dão capacidade temporária. [LevelWinner](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/)
- **My Mini Mart:** a **lixeira** serve para descartar recursos desnecessários. Ela aparece **de graça junto com a zona de $95, não no começo**, e o autor critica dar a lixeira desde o início porque ela polui a tela antes de existir o dilema. [PocketGamer.biz](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) · [wnhub](https://wnhub.io/ru/news/game-design/item-40717) · [Gamezebo](https://www.gamezebo.com/2022/01/25/my-mini-mart-strategy-guide-stock-the-shelves-with-these-hints-tips-and-cheats/)
- **My Perfect Hotel:** lixeiras para descartar o que se carrega (pratos, brinquedos, chinelos), porque um item pego só pode ser largado no lugar dele. *Indício:* veio do resumo de busca, e a página deu 403. [AppGamer](https://www.appgamer.com/my-perfect-hotel/answers/66177-what-is-purpose-of-rubbish-bins-they-dont-seen-to-do)
- **Burger Please!:** começa com 2 itens e sobe com Upgrade Player, skins (algumas dão capacidade) e boost Capacity Up (rewarded). Mistura e lixeira: não confirmado. [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/)
- **Forge & Fortune (forja, Supercent):** a principal reclamação nos reviews é perder o minério trazido da mina, as espadas feitas e o dinheiro não coletado ao sair do jogo ou durante um anúncio. [Play](https://play.google.com/store/apps/details?id=com.NokoGames.ForgeFortune)
- **Shop Titans:** baú de material com limite por tipo de recurso. A regra é "não deixar um material lotar e travar os outros". [PocketGamer](https://www.pocketgamer.com/shop-titans/guide/)

**Leitura [I]:** a pilha de um tipo só é o padrão do gênero e deve ficar, porque é legível e simplifica os ajudantes. O que falta no Forge Street é a **válvula**: uma lixeira (no tema, um "barril de sucata" ou um "caldeirão de refugo") que esvazia a mão. Seguindo a lição do My Mini Mart, ela só aparece quando o dilema existe, isto é, quando abre a 2ª linha (Escudos), que é o primeiro momento com dois tipos de produto no chão.

---

## 5. Estações e caixa/balcão evolutivos

**Estado do Forge Street [F]:** um balcão único com fila de 4 clientes (6 com a Vitrine), estoque de 5 por produto (10 com a Vitrine) e **um atendimento por vez** (`Serve` com timer único). O sprite do balcão não muda. Medido no bot, nos 10 primeiros minutos: **65–71 clientes "sem vaga"** e **fila no teto em 54–57% do tempo** (`BALANCE.md` §13–§15). As estações são compradas inteiras (2ª bigorna, 2ª fornalha) e os upgrades de velocidade são globais, de 1 nível (Fole/Fole duplo, Martelo veloz).

**O que os similares fazem [F]:**
- **My Perfect Hotel:** no começo o jogador atende a recepção pessoalmente. O recepcionista automático sai aos 9:26 da 1ª sessão (custa $140, ou o rewarded de $150), e na sessão 3 já há **3 recepcionistas**. O dinheiro fica empilhado e **só o jogador recolhe**. Os quartos sobem de nível com decoração. [arpubrothers](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/)
- **Burger Please!:** caixa por $100 e upgrade do caixa por $100 na 1ª sessão. O **drive-thru** abre na 1ª sessão e ganha upgrade na 4ª. O *level-up* de estação dá estrelas, velocidade, receita e **assentos**. [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) · [appmatch wiki](https://games.appmatch.jp/gamewiki/burgerplease/1668713081-3/)
- **Pizza Ready!:** o 1º funcionário fica no balcão e o 2º leva pizza do forno até ele. Depois vêm a embalagem, o take-out e o **drive-thru** com upgrades de capacidade e velocidade. Uma atualização recente pôs uma fábrica com esteira e empacotador automático. Quem recolhe o dinheiro é sempre o jogador. [LevelWinner](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/) · [Play](https://play.google.com/store/apps/details?id=io.supercent.pizzaidle)
- **My Mini Mart:** o jogador precisa ir ao caixa atender até contratar o **caixa**, um item caro que vem depois do liquidificador. Dinheiro acumulado por muito tempo atrai um **ladrão**, que o jogador pega com uma rede. [wnhub](https://wnhub.io/ru/news/game-design/item-40717) · [PocketGamer](https://www.pocketgamer.com/my-mini-mart/tips-and-tricks/)
- **Shop Titans:** upgrade do balcão, racks e prateleiras. A expansão da loja libera mais prateleiras. [mobi.gg](https://mobi.gg/en/tips/shop-titans-beginner-guide/) · [PocketGamer](https://www.pocketgamer.com/shop-titans/guide/)
- **Forge Master:** cada upgrade da bigorna muda a era dos itens (Pedra → Medieval → Moderna → Espacial → Quântica). [Play](https://play.google.com/store/apps/details?id=com.hariwn.legendofcivilizations)
- **Mudança de aparência ao subir de nível:** **não confirmado** em fonte escrita para os três líderes. Só vale o que se vê nos vídeos e nas lojas, e isso fica com a raia visual.

**Leitura [I] para o balcão 4 → 8 que o Vinicius quer:** nos similares, a capacidade de atendimento cresce em **pontos de venda paralelos** (mais um atendente ou um segundo canal), comprados como **níveis da estação**, e não só alongando a fila. Uma fila de 8 com um atendimento só aumenta a espera. Dois ou três pontos atendem mais gente por minuto. **Cuidado [F]:** o `BALANCE.md` §15 mostra que hoje a **oferta** é o limite. Mais vagas sem mais produção trocam "sem vaga" por "cansou na fila". Os níveis do balcão devem chegar junto com o aumento de produção (2ª bigorna, Escudos) e ser medidos no bot.

---

## 6. Funcionários e automação

- **[F] Padrão do trio:** Burger Please! (RH: velocidade, capacidade, contratar até 10; Upgrade Player: velocidade, capacidade, receita) e Pizza Ready! (HR + Manager, 3 upgrades × 5 níveis, 1º nível grátis, contratação paga uma vez). Contratar também sai por rewarded: o 2º funcionário do Burger Please! exige anúncio. [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) · [LevelWinner](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/)
- **[F] Papéis:** My Perfect Hotel tem faxineiro, recepcionista, loader (nível 7), garçom e bartender, e cada amenidade nova pede um funcionário. My Mini Mart tem ajudante, caixa e repositor. Pizza Ready! tem robôs que precisam de recarga a cada 3 min, com gema ou anúncio. [arpubrothers MPH](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) · [PocketGamer](https://www.pocketgamer.com/my-mini-mart/tips-and-tricks/) · [Gamigion](https://www.gamigion.com/pizza-ready-by-supercent-comprehensive-breakdown/)
- **[F] Problema conhecido:** no My Mini Mart os funcionários se amontoam numa estação e só saem com a pilha cheia. [App Store](https://apps.apple.com/us/app/my-mini-mart/id1592004814)
- **Forge Street [F]:** 6 ajudantes de papel fixo (minério, lingote, produto, joalheiro ×2, mineiro), comprados em pads. Um upgrade de 1 nível ("Ajudantes ágeis": velocidade e carga 4). **[I]** O papel está à altura do gênero, mas falta **profundidade de níveis**: os líderes vendem 5 níveis de velocidade/capacidade, e esse é o ralo de ouro barato que o README diz que falta ("Tudo comprado aos 28:24").

---

## 7. Expansão de áreas e mapas

- **[F]** Burger Please!: no nível 11 o jogador "se forma" e abre a loja seguinte (Shake Burger → Burger Queen → Sandy's na França, com moeda azul → On n On). My Perfect Hotel: Hotel 1 (até o nível 8) → Beach (11) → Mountain (13) → Rising Sun, **cada um com sua moeda**. My Mini Mart: **novo mercado aos 30–40 min**, que limpa a tela e reaproveita os recursos. Pizza Ready!: mapa com viagem rápida, e cada loja libera um uniforme. Eatventure: 60 cidades de ~7 restaurantes, e depois da 60ª volta à 1. [arpubrothers BP](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) · [arpubrothers MPH](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) · [PocketGamer.biz](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) · [Fandom](https://eatventure-games.fandom.com/wiki/Cities)
- **Forge Street [F]:** a oficina e a joalheria ficam no mesmo mapa (Corredor 690, Joalheria 1 515). Depois dos 22 produtivos (~40 min no bot) só restam 3 luxos sem efeito.
- **[I]** O ritmo dos líderes (troca de cenário em 30–40 min) coincide com o momento em que o Forge Street fica sem destino para o ouro. A "rua → distrito" do GDD §5 é exatamente a troca de loja do gênero, e ainda não existe.

---

## 8. Meta, monetização e retenção

**Meta [F]:**
- **Passes:** Burger Funds (grátis e premium), Hotel Pass por hotel, passe de US$8,99 no Pizza Ready! e Battle Progress Pass de US$9,99 no Forge Master.
- **Skins com bônus:** no Burger Please! algumas skins dão velocidade ou capacidade. No Pizza Ready! cada loja libera um uniforme, e outras fantasias saem por 3 anúncios.
- **Missões:** MPH tem missões diárias a partir do nível 3 (limpar 5 quartos, servir um VIP). Burger Please! tem missões como servir um VIP 3 vezes.
- **Eventos:** evento Hawaii de 6 dias (BP), evento Barbie (MPH) e eventos semanais com ranking (Eatventure).
- **Coleção:** livro de coleção e guilda (Shop Titans).

Fontes: [arpubrothers BP](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) · [arpubrothers MPH](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) · [LevelWinner PR](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/) · [PocketGamer Eatventure](https://www.pocketgamer.com/eatventure/tips-and-tricks/) · [PocketGamer ST](https://www.pocketgamer.com/shop-titans/guide/)

**Rewarded — onde e o quê [F]:**
- Maleta/bolsa de dinheiro: o mais assistido no MPH, e o Money Pocket do BP.
- Boost de 3 min: velocidade +50% (MPH), velocidade/capacidade (BP), luvas de capacidade (PR).
- Cliente VIP que deixa uma trilha de dinheiro (MPH).
- **x2 no offline** (BP, PR, Eatventure) e x2 ao subir de nível (MPH, BP).
- Contratar ou fazer upgrade por anúncio no lugar de dinheiro (BP, PR).
- Skins (BP, PR) e recursos em falta (MPH).
- Eatventure: boost de 5 min que acumula até 1 h, e "Investors" que pagam por anúncio.

**Interstitial [F]:** é pesado nos líderes. MPH: 528 vídeos em 19 sessões. BP: menos de 1 por minuto, ~30 na 1ª sessão. PR: pausa de 15 s a cada 1–2 min, segundo reviews. My Mini Mart e Lumber Chopper: ~1 por minuto, segundo reviews, com muitas reclamações. **Não confirmado:** a Pizza Ready! teria adiado o 1º anúncio para ~3 min (só vimos o resumo de busca). Contraexemplo: o **Eatventure só tem rewarded** e nota 4,7.

**IAP [F]:** No Ads de US$3,99 a ~9,99, VIP semanal/mensal, starter/semanal e passe. Blade Forge 3D: Remove Ads de US$2,99 elogiado. Idle Weapon Shop: criticado por não vender a remoção definitiva dos anúncios.

**Retenção [F]:**
- Offline com teto e x2 por anúncio (BP, PR; Eatventure com teto de 3 h; Forge Master de 4 h, segundo guia de fã).
- Login diário a partir do nível 4 em ciclo de 8 dias, com x2 por anúncio (MPH).
- Metas diárias que dão tickets de roleta (Shop Titans).
- Ladrão do dinheiro acumulado como gancho de atenção (My Mini Mart).

**Forge Street [F]:** o offline existe e é bem calibrado (`BALANCE.md` §5), mas o resto desta seção **não existe**. O GDD §7–§8 planeja quase tudo isso, e o §18 pede um "rewarded placeholder/test" no MVP.

---

## 9. O tema forja no mercado

- **[F]** Nenhum idle arcade de forja com 1M+ instalações apareceu em cerca de 20 buscas na Play. O *Forge & Fortune*, da Supercent (dona de Burger Please! e Pizza Ready!), é quase o nosso jogo e tem **1K+** instalações e nota 2,9. [Play](https://play.google.com/store/apps/details?id=com.NokoGames.ForgeFortune)
- **[I]** Isso tem duas leituras e não dá para separar com dado público. (a) Há espaço no tema. (b) A Supercent testou e não escalou, o que confirma o alerta do `00_PESQUISA/validacao/A_mercado_ua.md`: os hits do formato usam temas do cotidiano, e forja de fantasia tende a ter apelo mais estreito. O teste de criativos (GDD §16–§17) continua sendo o juiz.
- **[F]** O que o tema faz bem nos outros formatos: bigorna que muda de era (Forge Master), livro de coleção (Shop Titans), clientes especiais e eventos de muitos clientes (Customer Frenzy no Shop Titans, clientes noturnos no Idle Weapon Shop). Um review do Blade Forge 3D pede uma sala de coleção de armas. [Play Blade Forge](https://play.google.com/store/apps/details?id=com.kwalee.bladeforge3d) · [Shop Titans 3.7.0](https://playshoptitans.com/news/version-3-7-0-release-notes)

---

## 10. O que estamos perdendo

Esforço: **P** ≤ 1 dia de core + view · **M** 2–4 dias · **G** ≥ 1 semana ou arte nova. "v0.5" = cabe na próxima versão sem abrir área nova. Impacto é **hipótese [I]** até ser medido no bot e no playtest.

### 10.A Erros e travas (o que está quebrado ou trava o loop)

| ID | Prio | O que falta | Evidência (jogo) | Impacto esperado | Esforço | Quando |
|---|---|---|---|---|---|---|
| A1 | **P0** | **Largar a pilha / lixeira.** Hoje a mão cheia de um tipo não tem saída (trava registrada no `BALANCE.md` §9) | Pizza Ready! (lixeira), My Mini Mart (lixeira grátis na zona de $95), MPH (lixeiras, indício) | Elimina a trava de mãos cheias. Sem isso, qualquer pessoa no playtest pode ficar parada | **P** | **v0.5** |
| A2 | **P0** | **Balcão estreito:** fila 4/6, 1 atendimento por vez. São 65–71 clientes "sem vaga" em 10 min, com fila no teto 54–57% do tempo | MPH (1→3 recepcionistas), BP (caixa + drive-thru + nível dá assentos), PR (drive-thru com capacidade/velocidade) | Converte demanda perdida em venda **se** a produção acompanhar. Também dá a sensação de "minha loja cresceu" | **M** | **v0.5** |
| A3 | P1 | **A pilha da mão e a dos ajudantes não são salvas** (`Sim.Save` não grava `Player.Count/Item`) | Forge & Fortune: a reclamação nº 1 é perder o carregado e o dinheiro ao sair ou no anúncio | Evita sensação de roubo e é obrigatório antes de ter anúncio | **P** | **v0.5** |
| A4 | P2 | **Andar sem decisão 34%** (bot) e deslocamentos longos no 1º ciclo (depósito embaixo, balcão em cima) | My Mini Mart: ≤ 2 s por trecho no 1º ciclo; > 5 s é "erro comum" | Medir antes de mexer. Se algum trecho do 1º ciclo passar de 3 s, aproximar | P (medir) / M (layout) | v0.5 só a medição |

### 10.B Falta de conteúdo e sistemas

| ID | Prio | O que falta | Evidência (jogo) | Impacto esperado | Esforço | Quando |
|---|---|---|---|---|---|---|
| B1 | **P0** | **Estações com níveis** (Fornalha, Bigorna, Bancadas, Balcão Nv1–3 ou 1–5: velocidade/capacidade e, no balcão, vagas), com **mudança de aparência** a cada nível | BP (workstation level-up), Shop Titans (prédio de artesão sobe de nível), Forge Master (bigorna muda de era), MPH (quartos sobem de nível) | É o "crescimento visível" que o GDD promete e o que mais faz o jogo parecer menos cru. Também é o ralo de ouro que falta depois dos ~40 min | **M** (core) + arte da raia visual | **v0.5** (Fornalha, Bigorna, Balcão) |
| B2 | P1 | **Escritório com níveis:** jogador (velocidade, capacidade, receita) e ajudantes (velocidade, capacidade, contratar), 5 níveis cada, no menu inferior que já existe | BP (RH + Upgrade Player), PR (HR + Manager, 3 × 5 níveis) | Ralo de ouro barato, decisão por gargalo, cadência de compras | **P** (números + menu) | **v0.5** |
| B3 | P1 | **Rewarded (opt-in), primeiro como placeholder sem SDK:** maleta de ouro, boost de 3 min ("luvas de forja": velocidade/capacidade), **x2 no cofre offline**, ajudante temporário, cliente VIP | Todos os 5 arcade; Eatventure prova que só rewarded segura nota 4,7 | Mede o opt-in (kill criterion ≥ 45%, GDD §15/§26) antes de integrar o SDK, que depende de autorização | **P** (placeholder) / M (SDK) | **v0.5** placeholder; SDK depois |
| B4 | P1 | **Dinheiro físico:** moedas empilham no balcão e o jogador (ou um ajudante de caixa) recolhe; um gancho tipo "ladrão" se acumular | MPH e Pizza Ready! (só o jogador recolhe), My Mini Mart (ladrão) | Satisfação ("pilha de ouro"), motivo para circular, gancho natural para a maleta. **Risco:** aumenta a caminhada (A4) | **M** | v0.5 como **A/B no bot** |
| B5 | P1 | **1º minuto construindo:** começar com a oficina vazia e pads baratos (Bigorna, Balcão) pagos com ouro inicial | BP (entrada $25 → mesa $5 → máquina $50 → caixa $100), MPH (quartos de $30), My Mini Mart (caixa primeiro) | Transformação visível desde 0:00, coerente com o criativo #1. **Risco:** atrasa a 1ª venda (hoje 0:21 no bot; teto do GDD < 90 s) | **P–M** | v0.5 como **A/B no bot** |
| B6 | P1 | **Missões curtas** (1–3 tarefas visíveis com recompensa, além da dica e dos 4 baús) | MPH (missões diárias a partir do nível 3), BP (missões VIP), Shop Titans (metas diárias) | Direção de sessão e motivo para voltar; reaproveita os eventos do diário | **M** | v0.5 versão mínima, ou v0.6 |
| B7 | P1 | **Troca de loja/mapa ("formatura")** depois da produção completa: nova rua/distrito com layout novo | BP (nível 11 → nova loja), MPH (4 hotéis com moedas próprias), My Mini Mart (novo mercado aos 30–40 min), PR (mapa de lojas), Eatventure (60 cidades) | É o loop de longo prazo do gênero; hoje o ouro fica sem destino aos ~40 min | **G** | depois do playtest (v0.6+) |
| B8 | P2 | **Cliente VIP / encomenda especial** (nobre que paga 3× um item específico; noite de muitos clientes) | MPH (VIP com trilha de dinheiro), BP (missão VIP), Idle Weapon Shop (clientes noturnos), Shop Titans (Customer Frenzy) | Variedade sem conteúdo novo de produção; casa com rewarded (B3) | **M** | v0.6 |
| B9 | P2 | **Skins/uniformes do ferreiro com bônus pequeno** | BP (skins com velocidade/capacidade), PR (uniforme por loja, 3 anúncios), Idle Weapon Shop (roupas atraem clientes) | Meta cosmética e destino para rewarded/IAP | **M** (arte) | depois |
| B10 | P2 | **Login diário e ofertas diárias** | MPH (do nível 4, ciclo de 8 dias, x2 por anúncio), Shop Titans (metas diárias → roleta) | D1/D3, segundo o GDD §8; só faz sentido com soft launch | **P** | antes do soft launch |
| B11 | P2 | **Passe, eventos, moeda premium** | Burger Funds, Hotel Pass, passe US$8,99 (PR), eventos de 6 dias (BP), eventos semanais com ranking (Eatventure) | Monetização e live ops (GDD §9, §20–21) | **G** | v1.0 |
| B12 | P2 | **Coleção/catálogo de armas** (diferencial do tema) | Shop Titans (livro de coleção), review do Blade Forge 3D pedindo sala de coleção | Retenção temática; dá sentido às linhas novas | **M** | depois |
| B13 | P2 | **Política de anúncios e IAP:** No Ads US$2,99–4,99; interstitial só depois de vários minutos e com teto | Reclamações no MPH, BP, PR, My Mini Mart e Lumber; Remove Ads barato elogiado no Blade Forge 3D; Eatventure só com rewarded | Não copiar a cadência de interstitial dos líderes (< 1/min) | P (decisão) | decisão antes do SDK |

**[O] O que não copiar:**
- A cadência de interstitial dos líderes, que gera reclamações em massa nos reviews.
- Robôs que só funcionam com anúncio a cada 3 min (Pizza Ready!), que tornariam o rewarded obrigatório (kill criterion do GDD §26).
- Lixeira desde o 1º segundo, que polui a tela antes de existir o dilema (lição do My Mini Mart).

---

## 11. Top 5 recomendações para a v0.5

1. **Lixeira + salvar a pilha (A1 + A3), esforço P.**
   - Um barril de sucata perto do balcão e entre as bigornas esvazia a mão inteira em 1 toque de presença, sem devolver ouro (para não virar fonte de renda).
   - Aparece quando os **Escudos** abrem.
   - O `Save` passa a gravar item e quantidade do jogador e dos ajudantes.
   - **Teste:** o cenário de trava do `BALANCE.md` §9 (espadas na mão, estoque 5/5, fila de escudos) precisa destravar em menos de 5 s de jogo.
2. **Balcão em 3 níveis (A2 + B1), esforço M.**
   - Nv1: 4 vagas e 1 ponto de atendimento. Nv2: 6 vagas e um 2º ponto (2º caixeiro ou "janela da rua"). Nv3: **8 vagas** e um 3º ponto.
   - Cada nível troca a aparência do balcão.
   - Preços medidos no bot. A meta é "sem vaga" cair ≥ 50% sem subir "cansou na fila".
   - O Nv2 só fica disponível depois da 2ª bigorna.
3. **Níveis por estação + escritório (B1 + B2), esforço M.**
   - Fornalha e Bigorna em 3 níveis visuais (velocidade/capacidade); Fole e Martelo veloz viram os níveis 2–3 delas.
   - "Escritório" no menu inferior: Mochila, Botas e Ajudantes ágeis passam a ter 5 níveis.
   - Recalibrar a §3 do GDD no bot. Os pads continuam só para construir coisas novas.
4. **Rewarded placeholder (B3), esforço P, sem SDK e sem anúncio real.**
   - 4 ofertas opt-in com o selo "em teste": maleta de ouro, luvas de forja (velocidade/capacidade, 3 min), cofre x2 e ajudante temporário.
   - Registrar `rewarded_offer`/`rewarded_complete` no diário para medir o opt-in do GDD §15. A integração real (LevelPlay etc.) espera a autorização do Vinicius.
5. **A/B no bot: dinheiro físico no balcão (B4) e 1º minuto construindo (B5).**
   - Só entram se a 1ª venda continuar < 90 s e "andar sem decisão" não piorar mais de 5 p.p.
   - Os dois são os sinais de "jogo de verdade" mais visíveis dos líderes, mas mexem no ritmo, então precisam de medição antes.

Fora do top 5, mas logo depois: missões curtas (B6) e a troca de loja/distrito (B7, depois do playtest).

---

## 12. Lacunas da pesquisa (não confirmado)

- **Regra de mistura na pilha:** só o Pizza Ready! tem fonte (um tipo por vez). Burger Please!, MPH, My Mini Mart e Lumber Chopper: não confirmado.
- **Carga inicial:** a do MPH não foi confirmada; a do My Mini Mart (3 → 8) vem de review e de pergunta de usuário.
- **Lixeiras do MPH:** só resumo de busca (página 403).
- **Mudança visual das estações ao subir de nível** nos três líderes: não confirmado em fonte escrita (fica para a raia visual, com vídeo).
- **Lojas:** a Play Store não abriu para parte dos jogos. MPH e Burger Please! usam ApkCombo. A nota do Burger Please! diverge (4,2 × 4,4).
- **Datas de lançamento no Android** de Burger Please! e Pizza Ready! (só iOS). Lançamento do Blade Forge 3D e do Forge & Fortune (o deste só pelo AppGoblin, via resumo de busca).
- **Forge Master:** estúdio (Lessmore, segundo a Play) sem 2ª fonte; o guia do Clashiverse não diz o estúdio.
- **Pizza Ready!:** o atraso do 1º anúncio para ~3 min e o redesenho do FTUE (setas fora da tela, 8–9 s mortos) vêm só de resumo de busca.
- **Offline do MPH**, notificações push e login diário de BP/PR: não confirmados.
- **Receita:** números de AppMagic e Sensor Tower citados por terceiros, sem acesso à fonte primária. Não usamos receita para decidir nada aqui.
- **Mercado forja:** "nenhum idle arcade de forja popular" vem de cerca de 20 buscas na Play. Não é um censo.

---

## Fontes gerais do gênero

- Homa Games, "Arcade Idle: a new hybridcasual genre" (06/09/2021): RV resolve dor do jogador, IAP é a versão permanente do RV, rewarded interstitial. https://www.homagames.com/blog/arcade-idle-a-new-hybridcasual-genre-enters-the-game
- Supersonic, "How to ideate and build a profitable idle arcade game" (24/05/2022): "no resource left behind", recursos em cadeia, câmera que mostra a área travada. https://supersonic.com/?p=82671
- Udonis, "Arcade idle": lista de títulos por estúdio; D1 ~50% e D0 ~25 min (SensorTower/Homa, dado promocional). https://www.blog.udonis.co/mobile-marketing/mobile-games/arcade-idle
- PocketGamer.biz, desconstrução do My Mini Mart (Azur Games, 2023). https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/ · versão RU: https://wnhub.io/ru/news/game-design/item-40717
- Gamigion, Pizza Ready! (análise de IAP e downloads). https://www.gamigion.com/pizza/
- Estado do Forge Street: `README.md`, `docs/GDD.md` §2–§9 e §14–§17, `docs/BALANCE.md` §5, §9 e §13–§15, `client/Assets/_FS/Scripts/Core/Sim.cs` (`CanPick`, `Clients`, `Save`) e `Defs.cs` (capacidades e upgrades).
