# Forge Street — comparativo competitivo e diferenciais (2026-10-09)

**Pedido do Vinicius:** "avalie todos os GDDs, faça comparativos com outros jogos similares, melhore o arquivo e proponha coisas que não têm em jogos similares e que ajudariam nosso time a ter algo na frente de competitividade".

**Base.** Este documento **não refaz** a pesquisa de 08/10: os 10 jogos, as mecânicas e as fontes estão em [`BENCHMARK_MERCADO.md`](BENCHMARK_MERCADO.md) (loop e mercado) e [`BENCHMARK_VISUAL.md`](BENCHMARK_VISUAL.md) (arte e game feel). Aqui entra o resumo, três checagens novas na web (09/10) e o que ainda ninguém faz. Os dados de jogador vêm do teste no POCO F4 ([`VALIDACAO_V05.md`](VALIDACAO_V05.md), seção "Teste no POCO F4"), do bot ([`BALANCE.md`](BALANCE.md) §19–§21) e do [`BACKLOG_V07.md`](BACKLOG_V07.md).

**Legenda.** **[F]** fato com fonte · **[I]** inferência nossa · **[O]** opinião. "não confirmado" = nenhuma fonte legível confirmou. Status de tudo o que é proposto aqui: **PROPOSTO** (nada implementado). Custo para o nosso time: **P** ≤ 1 semana · **M** 2–4 semanas · **G** ≥ 1 mês.

---

## a) Similares (resumo; detalhe e fontes completas no BENCHMARK_MERCADO §1)

| Jogo | Estúdio | Play: instalações · nota | O que faz bem | Fonte |
|---|---|---|---|---|
| My Perfect Hotel | Redux Games / SayGames | 100M+ · 4,5 (via ApkCombo) | 1º minuto sem tutorial; recepção que ganha atendentes; um hotel novo com moeda própria a cada ~40 min | [SayGames](https://say.games/news/my-perfect-hotel-hits-fifty-million-downloads/) · [arpubrothers](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/) |
| Burger Please! | Supercent | 100M+ · 4,2–4,4 (fontes divergem) | Caixa + drive-thru; estação sobe de nível; "formatura" no nível 11 para outra loja | [ApkCombo](https://apkcombo.com/burger-please/io.supercent.burgeridle/) · [arpubrothers](https://arpubrothers.com/blog/burger-please-deconstruction-of-the-game/) |
| Pizza Ready! | Supercent | 100M+ · 4,4 (2,91M aval.) | Cadeia em etapas, lixeira, dinheiro físico recolhido pelo jogador | [Play](https://play.google.com/store/apps/details?id=io.supercent.pizzaidle) · [LevelWinner](https://www.levelwinner.com/pizza-ready-guide-tips-tricks-strategies/) |
| My Mini Mart | Supersonic | 100M+ · 3,9 (704K aval.) | Nenhum trecho do 1º ciclo passa de ~2 s; mercado novo aos 30–40 min | [Play](https://play.google.com/store/apps/details?id=com.KisekiGames.smart) · [PocketGamer.biz](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) |
| Idle Lumber Chopper Empire Inc | Supercent | 10M+ · 4,2 (70,5K aval.) | Cadeia industrial tora → serraria → caminhão (a mais parecida com a nossa) | [Play](https://play.google.com/store/apps/details?id=dasi.prs2.lumberchopper) |
| Eatventure | Lessmore | 50M+ · 4,7 (421K aval.) | Meta longa (60 cidades) e **só rewarded**, com nota 4,7 | [Play](https://play.google.com/store/apps/details?id=com.hwqgrhhjfd.idlefastfood) |
| Shop Titans | Kabam | 5M+ · 4,2 (182K aval.) | Loja de forja com livro de coleção, clientes heróis e guilda | [Play](https://play.google.com/store/apps/details?id=com.ripostegames.shopr) |
| Forge Master | Lessmore (não confirmado em 2ª fonte) | 1M+ · 4,5 (22,1K aval.) | A bigorna muda de era a cada upgrade | [Play](https://play.google.com/store/apps/details?id=com.hariwn.legendofcivilizations) |
| Blade Forge 3D | Kwalee | 10M+ · 4,4 (149K aval.) | Satisfação de martelar, temperar e testar a lâmina (hyper-casual, sem loja) | [Play](https://play.google.com/store/apps/details?id=com.kwalee.bladeforge3d) |
| Forge & Fortune | Supercent | 1K+ · 2,9 (23 aval.) | Idle arcade de forja quase igual ao nosso, **sem tração** | [Play](https://play.google.com/store/apps/details?id=com.NokoGames.ForgeFortune) |
| *Novo (09/10):* Hammer & Steel: idle business | Global Advertising Network LTD | iOS, 70 avaliações, nota 3,5; versões de 03/2025 a 11/2025 | Oficina medieval com salas, encomendas e facções; vende DLC de mina e fazenda por US$1,99 | [App Store](https://apps.apple.com/us/app/-/id6742078527) |

**Leitura de mercado [F]/[I].** O formato tem quatro líderes com 100M+ cada, todos com temas do cotidiano (hotel, hambúrguer, pizza, mercado). No tema forja, os dois arcade idle encontrados (Forge & Fortune e Hammer & Steel) têm poucas avaliações. Existem duas leituras para isso (BENCHMARK §9): sobra espaço no tema, ou forja de fantasia atrai um público mais estreito. Só o teste de criativos decide entre as duas. Receita dos líderes: só há números de terceiros (ex.: My Perfect Hotel US$22,1M, AppMagic, abr/2025, citado em `00_PESQUISA/validacao/A_mercado_ua.md`). Nenhuma decisão aqui depende de receita.

---

## b) Matriz de recursos (o que cada um tem × Forge Street)

Fonte de cada célula: BENCHMARK_MERCADO §2–§8. **v0.6** = o que está na `main` hoje. **GDD** = planejado. "—" = não tem ou não se aplica; "?" = não confirmado.

| Recurso | MPH | Burger Please! | Pizza Ready! | My Mini Mart | Eatventure | Shop Titans | Forge & Fortune | **Forge Street** |
|---|---|---|---|---|---|---|---|---|
| Avatar carrega a produção (joystick) | sim | sim | sim | sim | — (toque) | — | sim | **sim (v0.6)** |
| Cadeia física em etapas legíveis | lavanderia → quarto | máquina → caixa | forno → embalagem → balcão | canteiro → prateleira | — | — | mina → forja → rack | **minério → lingote → peça → balcão, com "travada"/"fome" visíveis (v0.6)** |
| Estação/balcão com níveis | sim | sim (dá assentos) | sim | sim | sim | sim | ? | **balcão 4→8 e melhorias em níveis (v0.5.1)** |
| Ajudantes que automatizam | sim | RH | HR + Manager | sim | sim | NPCs | ? | **6 papéis (v0.6)** |
| Cliente VIP | sim (trilha de dinheiro) | missão VIP | ? | — | — | clientes heróis | ? | **VIP 3× a cada 4–6 min + "Chamar VIP" por anúncio (v0.5.1)** |
| Missões curtas | diárias | sim | ? | — | sim | metas diárias | ? | **Encomendas, uma por vez (v0.6)** |
| Offline com teto + 2× por anúncio | ? | sim | sim | ? | teto 3 h | ? | ? | **25% × até 2 h, id de transação; 2× ainda não (D5 do backlog)** |
| Interstitial | muito pesado | < 1/min | 15 s a cada 1–2 min (reviews) | ~1/min | **nenhum** | nenhum | ? | **nenhum (decisão da v0.1)** |
| Troca de loja/mapa | 4 hotéis | lojas novas | mapa de lojas | mercado novo | 60 cidades | expansão | sala nova | **planejado (GDD §5 "rua → distrito", backlog B7)** |
| Coleção | — | skins | uniformes | — | equipamento | **livro de coleção** | ? | planejado (GDD §5 "blueprints") |
| Clientes com nome que voltam | — | — | — | — | — | heróis com classe | — | **— (proposto: FS-3)** |
| Ação de habilidade do avatar (timing) | — | — | — | — | — | — | ? | **— (proposto: FS-2)** |
| Concorrente visível na rua | — | — | — | — | — | — | — | **— (proposto: FS-4)** |
| Ritmo calibrado para pessoa real | ? | ? | ? | ? | ? | ? | ? | **bot + diário medem; ajuste proposto em FS-1** |

---

## c) O que os jogadores reclamam nos líderes (oportunidades)

1. **Anúncio forçado é a reclamação nº 1 do formato [F].** No My Perfect Hotel, uma desconstrução contou 528 vídeos em 19 sessões ([arpubrothers](https://arpubrothers.com/blog/my-perfect-hotel-arcade-idle-deconstruction/)). No Burger Please!, um resumo de reviews da App Store (julho de 2026) diz que os anúncios aparecem a cada poucos segundos, e alguns jogadores dizem que o progresso depende de assistir ([chrome-stats](https://chrome-stats.com/d/id1668713081), resumo de terceiro, não confirmado na loja). Pizza Ready!, My Mini Mart e Lumber Chopper: ~1 interstitial por minuto, segundo reviews (BENCHMARK §8). **Oportunidade:** o Eatventure prova que só rewarded segura nota 4,7. Os líderes não podem copiar isso sem perder a receita de interstitial (ver FS-5).
2. **Perder o que se carregava [F].** É a principal queixa no Forge & Fortune: o minério e as espadas somem ao sair ou durante um anúncio ([Play](https://play.google.com/store/apps/details?id=com.NokoGames.ForgeFortune)). **Já resolvemos:** o save guarda a carga da mão e dos ajudantes (v0.5.1).
3. **Funcionários que se amontoam ou ficam parados [F].** No My Mini Mart eles se juntam numa estação e só saem com a pilha cheia ([App Store](https://apps.apple.com/us/app/my-mini-mart/id1592004814)). **Já resolvemos em parte:** a correção do Ajudante 3 (`BALANCE.md` §19). Ainda falta a fome da Bigorna 2 (2 582 s de fome em 60 min no bot).
4. **Monotonia: "depois de um tempo é só andar" [I].** A raia B da validação disse isso do nosso próprio GDD: os gargalos se resolvem sozinhos, e a automação tira a função do avatar (`00_PESQUISA/validacao/B_design_economia.md`). O gênero inteiro tem esse problema; nenhum líder dá ao avatar um papel que a automação não cobre.
5. **Tema e clientes sem rosto [F]/[I].** A Supercent usa manequins cinza sem rosto (BENCHMARK_VISUAL §2). Nossos 16 clientes do Tripo + Mixamo têm personalidade (Thankful, Waving, Angry), mas hoje são intercambiáveis.
6. **O começo pede mais do que uma pessoa entrega [F] (dado nosso).** No POCO F4, a pessoa foi **1,7–3,7× mais lenta que o bot** (Fole 3:21, Escudos 14:45, Esteira 14:54, contra 2:30 / 5:30 / 8:30 do GDD §3). Nesse tempo, **67 clientes de espada cansaram** e **471 deram meia-volta** com a fila cheia. Os líderes não publicam esse dado; nós o medimos.

---

## d) Diferenciais propostos

Critério: o que os similares **não têm ou fazem mal**, que seja **barato para nós** e **difícil de copiar** (porque depende do nosso núcleo `FS.Core`, do bot e do diário, ou porque vai contra o modelo de receita dos líderes).

### FS-1. Demanda que respira (o movimento acompanha a sua oficina) — custo **P**
- **Por que importa:** é o problema medido nº 1 (item 6 acima). O cliente que cansa ou dá meia-volta no 1º quarto de hora é punição sem decisão: a pessoa ainda não tem como produzir mais rápido.
- **Como funciona:** nos primeiros ~15 min (ou até a Esteira), o intervalo entre clientes de cada linha segue a vazão real do jogador. O `Sim` já mede `RateEma` e já usa essa medida para dimensionar as Encomendas (`FASE9`), então o mesmo princípio vale para a fila. Quem chega com a fila cheia continua indo embora (o sinal visual do gargalo fica), mas a chegada não passa de ~1,2× do que a oficina entrega. Depois disso a regra atual volta e a fila longa vira pressão de verdade, que é o momento de comprar o balcão 5–8.
- **O que os outros fazem:** fluxo fixo de clientes, até onde dá para ver nos guias (**não confirmado**: nenhum líder publica a regra).
- **Risco:** o jogo pode ficar "mole" para quem joga rápido. Mitigação: a regra só reduz a chegada, nunca dá ouro extra, e desliga sozinha.
- **Validar barato:** criar no `Bot.cs` um **bot lento** (velocidade e decisão ×0,4, o pior caso do POCO) e medir em A/B: clientes que cansaram + meia-volta caem ≥ 50% sem o ouro/min do bot normal subir mais de 5%. Depois, 5 testadores com o `diario_report.py`.

### FS-2. Golpe de mestre: a obra-prima que só o ferreiro faz — custo **M**
- **Por que importa:** responde à crítica central da raia B ("a automação tira a função do avatar; o joystick fica inútil"). Também dá um gancho de anúncio que nenhum arcade idle mostra: o acerto perfeito na bigorna, com faíscas e câmera tremendo.
- **Como funciona:** quando o **próprio ferreiro** fica parado na boca da bigorna com um lingote, aparece um anel de tempo (0,8 s). Um toque no botão "Martelar!" dentro da janela faz uma **obra-prima** (peça brilhante). Errar ou ignorar não pune: sai a peça normal. A obra-prima vale 3× no balcão, completa a Encomenda na hora ou vai para a Galeria (FS-6). Ajudantes **nunca** fazem obra-prima, então o avatar continua importante depois da automação.
- **O que os outros fazem:** o Blade Forge 3D tem a satisfação de martelar, mas é hyper-casual, sem loja nem cadeia. Os arcade idle (Burger Please!, Pizza Ready!, MPH) dão ao avatar só a função de carregar.
- **Risco:** conflito com o joystick (hoje o toque em qualquer lugar é joystick). Mitigação: o botão aparece só com o ferreiro parado na boca, num lugar fixo acima da barra de melhorias. O ganho pode virar obrigação ("tenho que ficar na bigorna"), então o teto é de 1 obra-prima a cada 20 s.
- **Validar barato:** (1) criativo 9:16 "acerto perfeito" contra o #1 atual (IPM/CTR no mesmo teste de criativos); (2) playtest: ≥ 50% dos testadores usam o golpe ≥ 2× por sessão sem que ninguém o chame de chato; (3) no bot, a obra-prima não passa de 8% da receita.

### FS-3. Clientela fiel: cinco fregueses com nome que voltam — custo **M**
- **Por que importa:** dá motivo para voltar no D3/D7 (o GDD §8 só tem "cofre + próxima loja"), e é o que separa o nosso elenco dos manequins sem rosto dos líderes. A arte já existe: mago, elfa, goblin, cavaleiro e nobre (Lotes 2–4).
- **Como funciona:** cinco fregueses fixos aparecem como **Encomenda pessoal** (o 2º tipo de encomenda que o `BACKLOG_V07` já prevê): "O Goblin quer 6 ferramentas". Cada entrega enche 1 de 5 corações. No 5º, o freguês deixa uma marca permanente na rua (o estandarte do cavaleiro, a lanterna do mago) com um bônus pequeno e fixo (+5% no preço daquela linha) e uma fala curta. A ordem e o momento das visitas são determinísticos, como o VIP (sequência de Weyl). Nada é sorteado e nada é pago.
- **O que os outros fazem:** o Shop Titans tem heróis por classe, mas é uma loja de menus, não idle arcade. Nos quatro líderes não aparece cliente com nome (BENCHMARK_VISUAL §2).
- **Risco:** texto e localização (5 × ~6 falas). A ideia de "relação" pode passar despercebida se o coração for pequeno na tela.
- **Validar barato:** criativo "o goblin voltou" contra o controle; no playtest, quantos testadores dizem o nome de um freguês sem a gente perguntar. Retenção só no soft launch (D3 dos que completaram ≥ 1 freguês × dos que não completaram).

### FS-4. O ferreiro rival do outro lado da rua — custo **G**
- **Por que importa:** dá ao core a **tensão** que falta (raia B: "sem falha, só números"), sem derrota punitiva. Também dá enredo à troca de loja (backlog B7), que hoje é só "mapa novo".
- **Como funciona:** do outro lado da rua abre uma forja rival, pilotada pelo nosso próprio `Bot.cs` com uma oficina menor. O cliente que chega escolhe a fila mais curta. Quando a sua oficina supera a dele (ex.: 3 min seguidos com fila menor e mais vendas), você **compra a forja dele**: ela vira a sua 2ª loja/distrito, com um rival novo e mais forte no próximo bairro. O rival nunca rouba ouro nem destrói nada: só "leva" os clientes que você não atendeu.
- **O que os outros fazem:** uma busca em 09/10 não achou nenhum idle arcade mobile com loja rival disputando clientes. A busca não é censo; o achado mais próximo é um jogo de PC fora do gênero.
- **Risco:** escopo (2ª simulação, arte de fachada, balance); frustração se o rival parecer "roubar". Performance: 2ª oficina na tela.
- **Validar barato:** antes de qualquer arte, rodar no bot um A/B "rival na rua" (o rival fica com ≤ 15% dos clientes de quem joga bem e ≤ 35% do bot lento). Depois, um criativo "roube a clientela do rival" no teste de criativos. Só entra depois do playtest Camada 0.

### FS-5. "Anúncio só quando você pede" como promessa de marca — custo **P**
- **Por que importa:** ataca a reclamação nº 1 do formato (item 1). É **contraposicionamento**: os líderes ganham com o interstitial e não podem abrir mão dele sem perder receita. O Eatventure, só com rewarded, tem nota 4,7.
- **Como funciona:** zero interstitial e zero banner, para sempre, escrito na loja ("sem anúncio forçado"). Todo anúncio é opt-in, com o prêmio escrito no botão (já é assim no VIP e na Velocidade), recarga visível e um teto diário. No lugar do "Remove Ads", que não teria o que remover, entra um "Pacote do Mestre" (US$2,99–4,99): cofre 2× permanente + uma skin de oficina. É conveniência moderada, como o GDD §7 já admite, e nada aleatório.
- **Risco:** ARPDAU mais baixo que o dos líderes. Por isso o gate de dinheiro da fase B é IAA de rewarded (opt-in, impressões/DAU, ARPDAU), não pagante (veredito, §2 da raia B).
- **Validar barato:** teste de página da loja (custom store listing) com e sem a frase "sem anúncio forçado" e medir a conversão loja → install. Opt-in de rewarded ≥ 45% (GDD §15), já medido pelo `diario_report.py`.

### FS-6. Galeria de obras-primas na rua — custo **M** (depende do FS-2)
- **Por que importa:** coleção é a meta que o tema pede (Shop Titans tem livro de coleção; um review do Blade Forge 3D pede uma sala de armas). E coleção à mostra na rua é crescimento visível, filmável.
- **Como funciona:** cada **tipo** de obra-prima (3 por linha × 4 linhas = 12) ganha um pedestal na rua ao ser feito pela 1ª vez. Completar os 3 de uma linha dá +5% fixo no preço dela. A ordem de desbloqueio é fixa (sem sorteio), e o catálogo mostra a silhueta do que falta.
- **Risco:** 12 sprites novos (Blender, 0 crédito, como a arte v0.5).
- **Validar barato:** playtest: ≥ 30% abrem o catálogo sem a gente pedir. Criativo "a galeria completa" como variação do #10 do GDD §17.

### FS-7. O cofre que conta o que aconteceu — custo **P**
- **Por que importa:** nos líderes a volta é um pop-up com um número e "2× por anúncio" (BENCHMARK §8). Contar o que os ajudantes fizeram ensina o gargalo (a decisão central do jogo) no melhor momento de atenção, que é a volta.
- **Como funciona:** o cartão "Bem-vindo de volta!" ganha 3 linhas: quanto cada linha rendeu (estimativa pela participação de cada uma no `RateEma`, dita como estimativa), qual estação ficou mais tempo travada ou com fome antes de você sair e um botão "ver" que leva a câmera até ela. O pagamento não muda: 25% × até 2 h, com id de transação (`Sim.ApplyOffline`).
- **Risco:** texto demais no cartão. Mitigação: 3 linhas, ícones, nada de tabela.
- **Validar barato:** diário: % das voltas em que o jogador compra a melhoria da estação apontada em até 60 s (alvo ≥ 30%).

---

## e) Os 3 que eu faria primeiro

1. **FS-1 — Demanda que respira (P).** É o único com dado de jogador real por trás (67 cansaram, 471 meias-voltas, ritmo 1,7–3,7× mais lento) e protege o KPI que mais pesa no kill criteria, o D1. Cabe na v0.7 e se prova no bot antes de chegar ao jogador.
2. **FS-2 — Golpe de mestre (M).** Conserta o defeito de design que a validação apontou (o avatar perde a função) e cria o criativo que nenhum concorrente pode filmar. Também é a base do FS-6 e da Encomenda do Rei.
3. **FS-5 — "Anúncio só quando você pede" (P).** Custa uma decisão, não código (o jogo já não tem interstitial). Transforma a política da v0.1 em vantagem permanente de marca contra a reclamação nº 1 do gênero. **A decisão do modelo de negócio é do Vinicius.**

Logo depois: **FS-3** (a arte já existe; é o melhor gancho de D3/D7). **FS-4** é o diferencial mais forte e mais difícil de copiar, mas é G: entra junto com a troca de loja (B7), depois do playtest.

## Posicionamento (uma frase)

Para quem gosta de ver uma operação crescer sem ser interrompido, **Forge Street** é o idle arcade de forja em que **a sua mão ainda importa** (a obra-prima sai só do ferreiro) e **o jogo se ajusta ao seu ritmo**, porque foi calibrado com bot e com gente de verdade, sem anúncio forçado.

## Lacunas desta revisão

- A novidade de FS-2, FS-3 e FS-4 vem de buscas e guias, não de um censo da loja. Um concorrente pequeno pode ter algo parecido.
- Nenhum dos diferenciais foi medido. Todos são **HIPÓTESE** até o bot, o playtest Camada 0 e o teste de criativos.
- Os números das lojas são os de 08/10 (BENCHMARK_MERCADO). O resumo de reviews do Burger Please! vem de terceiro.
