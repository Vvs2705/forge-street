# FORGE STREET
# DOCUMENTO MESTRE DE PRODUÇÃO COMERCIAL AGRESSIVA — V1.0

**Data-base:** 09/10/2026  
**Projeto:** Forge Street  
**Repositório:** `https://github.com/Vvs2705/forge-street`  
**Base técnica auditada:** `main`, v0.6.0  
**Engine:** Unity 6000.3.23f1  
**Plataforma primária:** Android  
**Orientação:** Portrait  
**Estratégia:** produto completo, expansivo, orientado a receita, retenção e escala  
**Público:** programação, game design, arte, UI/UX, áudio, QA, monetização, produto e UA.

---

# 0. DIRETIVA EXECUTIVA

Forge Street não deve mais ser tratado como um MVP pequeno.

A missão oficial passa a ser:

> **transformar a base atual em um jogo mobile comercial completo, visualmente premium, expansivo, com retenção de longo prazo, LiveOps, monetização híbrida e capacidade real de escalar.**

A equipe está autorizada a:

- ampliar sistemas;
- criar novos distritos;
- aprofundar a economia;
- criar novas linhas de produção;
- expandir workers;
- criar blueprints, collections e Masterworks;
- criar LiveOps;
- criar Season Pass;
- adicionar Rewarded Ads;
- adicionar Interstitials controlados;
- implementar IAP;
- criar analytics remoto;
- criar RemoteConfig;
- criar attribution;
- fortalecer save;
- criar cloud/restore quando justificável;
- melhorar radicalmente UI;
- substituir assets;
- refazer personagens;
- melhorar animações;
- melhorar iluminação;
- adicionar VFX;
- melhorar áudio;
- redesenhar telas;
- criar identidade visual e marca mais robustas;
- criar ferramentas internas de conteúdo;
- automatizar release e QA.

Regra central:

> **Preservar as vantagens técnicas que já existem, mas não preservar fraquezas visuais ou de produto por apego ao trabalho anterior.**

Preservar especialmente:

- `FS.Core` puro;
- simulação determinística;
- bot;
- testes;
- mutation testing;
- telemetria;
- proveniência;
- pipeline de criativos;
- separação entre simulação e apresentação.

---

# 1. ESTRATÉGIA AGRESSIVA

“Caminho agressivo” neste projeto significa:

- maior teto de conteúdo;
- maior qualidade visual;
- maior profundidade de progressão;
- monetização mais completa;
- LiveOps desde a arquitetura;
- mais material para UA;
- mais motivos de retorno;
- mais espaço de expansão.

Não significa:

- features aleatórias;
- ignorar testes;
- quebrar performance;
- monetização predatória;
- reescrever sistemas estáveis sem necessidade.

A aposta é:

> **assumir mais risco de produção em troca de um produto capaz de gerar LTV e receita muito superiores a um idle arcade curto.**

---

# 2. POLÍTICA DO POCO F4

O POCO F4 já demonstrou que Forge Street roda bem no aparelho.

Ele não deve continuar sendo usado como dispositivo de regressão diária.

## Rotina de desenvolvimento

Usar:

- Core tests;
- Unity EditMode;
- viewcheck;
- Windows build;
- autoplay;
- Android Emulator;
- CI;
- bot;
- smoke;
- simuladores de resolução;
- profiling no Editor/desktop;
- testadores externos;
- device farm quando apropriado.

## POCO F4 somente em marcos

### Gate 1 — Feature Complete
Quando sistemas da versão candidata estiverem integrados.

### Gate 2 — Release Candidate
Antes de internal/open testing comercial.

### Gate 3 — Bug Android específico
Somente quando bug depender de:
- touch físico;
- lifecycle;
- GPU/driver;
- haptic;
- Android Back;
- hardware.

### Gate 4 — Build de publicação
Validação final curta da build real.

Regra:

> **não reinstalar e testar o jogo no POCO a cada pequena alteração.**

---

# 3. VISÃO FINAL DO PRODUTO

Forge Street deve evoluir de:

> “uma oficina de ferraria”

para:

> **uma rede de distritos artesanais em crescimento, onde o jogador transforma matéria-prima em produtos, resolve gargalos, contrata trabalhadores, automatiza cadeias, cumpre contratos, coleciona projetos e Masterworks, participa de eventos e constrói a maior rede de forjas do reino.**

---

# 4. FANTASIA DE PROGRESSÃO

```text
EU FAÇO TUDO SOZINHO
↓
TENHO UM AJUDANTE
↓
GERENCIO UMA OFICINA
↓
CONTROLO UMA RUA
↓
ADMINISTRO VÁRIOS DISTRITOS
↓
ATENDO O REINO
↓
MEU NOME É REFERÊNCIA EM TODO O MAPA
```

Essa transformação deve ser:

- mecânica;
- econômica;
- visual;
- sonora;
- social;
- comercial.

---

# 5. CORE LOOP FINAL

```text
RECEBER / COLETAR RECURSO
↓
PROCESSAR
↓
FABRICAR
↓
ABASTECER
↓
VENDER / ENTREGAR
↓
GANHAR GOLD / REPUTATION / BLUEPRINT
↓
IDENTIFICAR GARGALO
↓
UPGRADE / WORKER / AUTOMAÇÃO
↓
NOVA LINHA
↓
ORDER / CONTRACT
↓
DISTRICT
↓
AUTOMATIZAR DISTRITO ANTIGO
↓
EVENTOS / MASTERWORKS / PASS
↓
EXPANSÃO DO IMPÉRIO ARTESANAL
```

---

# 6. CINCO LOOPS DE RETENÇÃO

## Loop de segundos
Coletar, levar, fabricar, vender.

## Loop de minutos
Upgrade, worker, estação, Order.

## Loop de sessão
Automatização, nova linha, major unlock.

## Loop de dias
Blueprint, Masterwork, distrito, daily, event.

## Loop de semanas
Season, Pass, Royal Contracts, novas áreas.

---

# 7. META DE CONTEÚDO V1.0

Direção de lançamento completo:

- 6 distritos;
- 35–45 produtos;
- 25–35 estações;
- 20–30 workers/personagens funcionais;
- 120–160 upgrades;
- 60+ Blueprints;
- 60 Masterworks;
- 50+ Orders;
- 12 Royal Contracts;
- 8 templates de LiveOps;
- 1 Season Pass;
- achievements;
- collection book;
- reputation;
- monetização completa;
- store;
- analytics;
- release pipeline;
- arte final;
- áudio final.

O número exato pode ser ajustado, mas o produto não deve sair parecendo um protótipo.

---

# 8. DISTRITOS

## Distrito 1 — Old Forge
Ensina:
- minério;
- furnace;
- lingote;
- espada;
- escudo;
- ferramenta;
- worker;
- esteira;
- Orders.

## Distrito 2 — Armorer's Row
Produtos:
- elmo;
- peitoral;
- grevas;
- lança;
- arma pesada.

Novidades:
- assembly;
- quality;
- bulk orders.

## Distrito 3 — Artisan Quarter
Produtos:
- joias;
- ferramentas finas;
- ornamentos;
- caixas;
- acessórios.

Novidades:
- múltiplos componentes;
- clientes ricos;
- margem versus volume.

## Distrito 4 — Royal Arsenal
Produtos:
- kits militares;
- armas cerimoniais;
- equipamento de guarda.

Novidades:
- Royal Contracts;
- deadlines;
- quotas;
- reputação real.

## Distrito 5 — Arcane Foundry
Recursos:
- minério rúnico;
- cristal;
- ligas especiais.

Novidades:
- energização;
- estabilidade;
- visual premium;
- receitas especiais.

## Distrito 6 — Grand Forge
Síntese:
- múltiplas cadeias;
- exportação;
- elite contracts;
- Masterworks;
- endgame.

---

# 9. EXPANSÃO PÓS-LANÇAMENTO

Arquitetura deve suportar:

```text
FORGE STREET
↓
PORT CITY
↓
MOUNTAIN FOUNDRY
↓
ROYAL CAPITAL
```

Cada cidade pode reutilizar os mesmos sistemas e mudar:
- recursos;
- cadeia;
- layout;
- art direction;
- clientes;
- eventos.

---

# 10. DISTRITOS ANTIGOS NÃO DESAPARECEM

Ao avançar:

- manager assume;
- renda vira background;
- player pode revisitar;
- eventos ainda podem ocorrer;
- upgrades finais permanecem.

Isso produz sensação de rede crescente.

---

# 11. ROYAL CHARTER

Ao dominar um distrito:

o jogador recebe um **Royal Charter**.

Funções:
- libera próximo distrito;
- aumenta reputação;
- concede badge;
- melhora background income;
- marca grande conquista.

Não usar reset destrutivo clássico de prestige.

---


# 12. ECONOMIA

## Gold
Moeda principal.

Fontes:
- vendas;
- Orders;
- Royal Contracts;
- offline;
- eventos.

Sinks:
- stations;
- workers;
- upgrades;
- district unlocks;
- production capacity;
- utility systems.

## Gems
Premium currency.

Fontes:
- compra;
- achievements;
- Pass;
- pequena distribuição gratuita.

Sinks:
- cosmetics;
- rerolls;
- convenience;
- bundles;
- event utility.

## Blueprint Tokens
Progressão de conhecimento.

Fontes:
- Orders;
- Contracts;
- events;
- achievements.

Sinks:
- novas receitas;
- specializations;
- Masterwork unlocks.

## Event Currency
Somente durante evento.

Não operar múltiplas moedas de evento simultaneamente.

---

# 13. REGRA DE CLAREZA ECONÔMICA

O jogador precisa conseguir entender:

```text
Gold = crescer
Gems = premium/conveniência
Blueprint = descobrir
Event Currency = evento temporário
```

Não criar dezenas de recursos apenas para parecer profundo.

---

# 14. PRODUTOS

Produtos são divididos em famílias.

## Tier 1
- espada;
- escudo;
- ferramenta.

## Tier 2
- lança;
- elmo;
- peitoral;
- grevas.

## Tier 3
- joia;
- ferramenta fina;
- ornamento;
- utensílio.

## Tier 4
- equipamento real;
- kit militar;
- item cerimonial.

## Tier 5
- equipamento rúnico;
- item arcano;
- liga especial.

Cada produto precisa declarar:
- inputs;
- stations;
- craft time;
- sell value;
- order weight;
- visual;
- unlock;
- blueprint;
- quality rules.

---

# 15. QUALITY SYSTEM

Adicionar:

```text
Normal
Fine
Superior
Masterwork
```

Qualidade depende de:
- station;
- worker;
- blueprint;
- material;
- buff.

Evitar RNG puro.

O jogador precisa compreender por que conseguiu uma qualidade melhor.

---

# 16. MASTERWORKS

Masterworks são peças de coleção.

Características:
- aparência exclusiva;
- registro no Hall;
- condição especial;
- não vendidas automaticamente;
- usadas como meta de longo prazo.

Exemplos:
- King's Sunblade;
- Shield of the First Guard;
- Ember Crown.

Recompensas:
- reputation;
- cosmetics;
- achievement;
- collection completion.

---

# 17. BLUEPRINTS

Blueprints desbloqueiam:
- produtos;
- melhorias;
- Masterworks;
- variantes.

Fontes:
- gameplay;
- Orders;
- Contracts;
- events;
- Pass;
- achievements.

A loja pode acelerar aquisição, mas não deve tornar receitas essenciais exclusivas de pagamento.

---

# 18. ORDERS 2.0

Tipos:

## Common
Pedido simples.

## Bulk
Grande quantidade.

## Quality
Exige nível de qualidade.

## Rush
Prazo.

## Mixed
Vários produtos.

## Royal
Alta recompensa e reputação.

## Blueprint
Recompensa projeto.

## Event
Ligado a LiveOps.

---

# 19. ROYAL CONTRACTS

Objetivos de médio prazo.

Exemplo:

> Equipar 20 guardas.

Necessita:
- 20 espadas;
- 20 escudos;
- 20 elmos.

Recompensas:
- Gold;
- Blueprint Tokens;
- Reputation;
- chest;
- cosmetic chance/controlada.

Contratos não podem bloquear o core loop.

---

# 20. CLIENTES

Famílias:
- farmer;
- traveler;
- guard;
- adventurer;
- merchant;
- noble;
- royal emissary;
- collector;
- VIP.

Diferenças:
- ticket médio;
- paciência;
- produto;
- raridade;
- comportamento;
- reward.

---

# 21. VIP

Preservar o conceito atual.

Melhorar apresentação:
- entrada especial;
- som;
- FX;
- pedido destacado;
- recompensa clara.

Rewarded “Chamar VIP” precisa parecer evento voluntário.

---

# 22. WORKERS

Papéis:

## Porter
Carrega recurso.

## Smelter
Opera furnace.

## Smith
Produz.

## Clerk
Atende.

## Foreman
Buff de área.

## Quartermaster
Aumenta armazenamento.

## Courier
Move Orders/exportação.

---

# 23. WORKER PROGRESSION

Cada worker:
- level;
- speed;
- capacity;
- specialty;
- cosmetic.

Especializações simples:
- swords;
- shields;
- armor;
- carrying;
- customer service.

No máximo 1–2 traços fortes por worker.

---

# 24. MANAGERS

Ao concluir distrito:
- manager entra;
- automatiza coleta;
- administra produção em background;
- permite renda offline melhor.

Manager não deve eliminar todo motivo de revisita.

---

# 25. PLAYER CHARACTER

O personagem principal precisa mostrar evolução.

Progressão visual:
- roupa simples;
- avental;
- belt;
- luvas;
- ferramenta;
- uniforme de mestre;
- visual de guilda/real.

Parte:
- gameplay.
Parte:
- cosmetics.

---

# 26. PLAYER TOOLS

Ferramentas funcionais leves:
- boots → movimento;
- gloves → carry;
- hammer → interaction;
- cart → capacity;
- ledger → order slots.

Não transformar em inventário RPG complexo.

---

# 27. OFFLINE PROGRESS 2.0

Manter o sistema limitado por:
- taxa;
- cap;
- district;
- manager.

Rewarded:
- dobrar offline.

Premium:
- pode ampliar cap de forma moderada.

Gameplay ativo precisa continuar superior ao offline.

---

# 28. LIVEOPS

Criar infraestrutura data-driven.

Um evento deve definir:
- id;
- start/end;
- requirements;
- missions;
- rewards;
- currency;
- visual;
- modifiers;
- offers.

Nunca hardcode evento específico no fluxo central.

---

# 29. TEMPLATES DE LIVEOPS

- Forge Frenzy;
- Royal Week;
- Rare Ore Rush;
- Blacksmith Festival;
- Masterwork Hunt;
- Merchant Caravan;
- Armorer's Challenge;
- Arcane Week.

---

# 30. DAILY MISSIONS

Curtas:
- vender 20 produtos;
- concluir 2 Orders;
- melhorar station;
- coletar offline;
- chamar VIP.

Recompensa:
- Gold;
- Gems pequenas;
- Pass XP;
- Tokens.

---

# 31. WEEKLY MISSIONS

Objetivos maiores:
- concluir Orders;
- produzir famílias;
- cumprir Contracts;
- participar de evento.

---

# 32. ACHIEVEMENTS

Categorias:
- crafting;
- workers;
- districts;
- Orders;
- Masterworks;
- automation;
- economy.

Recompensas:
- Gems;
- badge;
- cosmetic;
- Pass XP.

---

# 33. MASTERWORK PASS

Season Pass:
- 28–35 dias;
- free track;
- premium track.

XP:
- daily;
- weekly;
- Orders;
- events.

Rewards premium:
- cosmetics;
- Gems;
- decorations;
- convenience.

Não colocar item obrigatório de progressão apenas no premium.

---

# 34. COLLECTION BOOK

Abas:
- products;
- Masterworks;
- workers;
- clients;
- districts;
- blueprints.

Objetivo:
dar valor de coleção ao conteúdo já produzido.

---

# 35. REPUTATION

Reputation não é moeda.

Ganha por:
- Royal Contracts;
- qualidade;
- milestones;
- events.

Desbloqueia:
- clientes;
- districts;
- contracts;
- cosmetics;
- story beats.

---

# 36. CAMPAIGN LIGHT

Narrativa leve:
- oficina herdada;
- comerciante mentor;
- guilda;
- capitão real;
- mestre artesão;
- rival.

Sem cutscenes longas.

Narrativa serve para:
- contexto;
- objetivos;
- identidade.

---

# 37. RIVAL

Rival comercial pode:
- aparecer em milestones;
- competir em eventos;
- provocar desafio;
- servir de referência de progresso.

Não exige PvP real no lançamento.

---

# 38. SOCIAL FUTURO

Arquitetura não deve impedir:
- leaderboard;
- friends;
- visit forge;
- guilds.

Mas não é blocker da v1.0.

---


# 39. MONETIZAÇÃO — PRINCÍPIO

Forge Street é PvE/tycoon.

Pode monetizar conveniência de forma mais agressiva do que um jogo competitivo.

Mas:

> **pagamento acelera, amplia opções ou melhora aparência; não transforma a experiência gratuita em uma versão deliberadamente ruim.**

---

# 40. REWARDED ADS

Placements recomendados:

## VIP
Chamar cliente especial.

## Forge Rush
Aumentar produção por tempo limitado.

## Offline x2
Dobrar recompensa offline.

## Order Boost
Acelerar ou bonificar Order.

## Blueprint Reroll
Trocar opção.

## Supply Drop
Pequena entrega de insumo.

## Event Bonus
Recompensa extra.

Tudo controlado por RemoteConfig.

---

# 41. INTERSTITIAL ADS

Autorizados.

Regras:
- nenhum durante tutorial inicial;
- nenhum no meio de ação;
- apenas em transições naturais;
- cap por sessão;
- cooldown;
- removidos por No Ads.

Candidatos:
- retorno ao mapa;
- mudança de distrito;
- após sequência de milestones.

---

# 42. IAP

## Remove Ads
Remove interstitial.
Rewarded continua opcional.

## Starter Smith Pack
Primeira compra:
- Gems;
- cosmetic;
- boost;
- alto valor percebido.

## Gem Packs
Faixas de preço.

## Master Blacksmith Bundle
- skin;
- Gems;
- Tokens;
- consumables.

## District Bundle
Oferta contextual ao desbloquear área.

## Event Bundle
Durante eventos.

## Masterwork Pass
Pass sazonal.

---

# 43. SUBSCRIPTION

Não obrigatória na v1.0.

Pode virar:
> Forge Guild Membership

Benefícios possíveis:
- daily Gems;
- offline cap extra;
- cosmetic mensal;
- extra Order slot.

Só implementar se existir valor recorrente real.

---

# 44. SHOP

Seções:
- Featured;
- Pass;
- Gems;
- Cosmetics;
- Bundles.

Não abrir automaticamente toda sessão.

Não usar dark patterns.

---

# 45. PAYER LADDER

Criar produtos para:
- low spender;
- regular spender;
- high spender.

Sem lootbox.

Usar:
- compra direta;
- bundles transparentes;
- Pass.

---

# 46. OFFER ENGINE

Gatilhos:
- district unlock;
- first purchase opportunity;
- event;
- Pass milestone;
- low premium currency.

Tudo configurável remotamente.

---

# 47. FIRST PURCHASE

Não mostrar no primeiro minuto.

Fluxo:
```text
entender loop
↓
primeiro upgrade
↓
primeira automação
↓
rewarded opcional
↓
starter pack
```

---

# 48. COSMÉTICOS

Categorias:
- player outfit;
- worker skins;
- forge skin;
- furnace skin;
- counter theme;
- district decoration;
- profile badge;
- VFX discretos.

Cosmético deve aparecer de verdade no gameplay.

---

# 49. DECORAÇÃO

Adicionar slots simples de decoração.

Fontes:
- Pass;
- events;
- achievements;
- store.

Não criar editor complexo.

---

# 50. ARTE — AUTORIZAÇÃO DE REDESIGN

A partir deste documento:

> **nenhum asset visual existente é protegido apenas por já estar pronto.**

Se uma peça:
- parece provisória;
- é genérica;
- tem leitura ruim;
- parece barata;
- reduz contraste;
- prejudica anúncio;
- destoa da identidade;

ela pode e deve ser substituída.

---

# 51. DIREÇÃO VISUAL FINAL

Proposta:

> **Stylized Medieval Crafting Diorama**

Características:
- chunky shapes;
- silhuetas grandes;
- materiais limpos;
- textura controlada;
- cores quentes;
- iluminação de forja;
- personagens expressivos;
- objetos exagerados;
- cenário legível;
- VFX fortes.

O objetivo é parecer:
- premium;
- divertido;
- reconhecível em 9:16.

---

# 52. PALETA

Base:
- madeira;
- pedra;
- ferro;
- couro;
- cobre.

Acentos:
- laranja/amarelo para calor;
- verde para sucesso;
- azul para informação;
- vermelho para gargalo.

Cor nunca é o único sinal.

---

# 53. ILUMINAÇÃO

A forja precisa vender calor.

Usar:
- furnace glow;
- contraste quente/frio;
- highlights;
- reflexo metálico simplificado;
- sombras suaves;
- partículas de brasa.

Evitar pós-processamento caro.

---

# 54. FORNALHA

Deve ter estados visuais claros:

```text
fria
aquecendo
ativa
sem minério
saída cheia
overdrive
```

Cada um precisa ser legível sem texto.

---

# 55. BIGORNA

Feedback:
- martelo;
- sparks;
- impact;
- sound;
- progress;
- product pop.

Crafting precisa ser satisfatório mesmo quando o jogador apenas observa worker.

---

# 56. ESTEIRA

A esteira é um grande “antes/depois”.

Antes:
> manual.

Depois:
> sistema vivo.

Dar:
- movimento claro;
- som mecânico;
- itens circulando;
- partículas leves.

---

# 57. CLIENTES

Silhuetas distintas:
- farmer;
- traveler;
- guard;
- adventurer;
- merchant;
- noble;
- royal;
- VIP.

Pedido não pode depender só do balão.

O cliente precisa comunicar classe/fantasia.

---

# 58. PERSONAGENS

Manter estratégia de sprites pré-renderizados, mas melhorar livremente.

Autorizado:
- novo modelo;
- novo rig;
- nova roupa;
- nova pose;
- mais frames;
- novo render preset;
- novo atlas;
- nova luz;
- nova escala.

---

# 59. ANIMAÇÕES

Set mínimo funcional:

Player:
- idle;
- walk;
- carry;
- interact;
- celebrate.

Worker:
- idle;
- walk;
- carry;
- work;
- celebrate.

Client:
- walk;
- wait;
- happy;
- frustrated;
- leave.

Especializados:
- hammer;
- smelt;
- counter.

---

# 60. EXPRESSIVIDADE

Em sprite pequeno:
- pose;
- braço;
- cabeça;
- timing;
- exaggeration.

Eventos precisam de emoção:
- VIP;
- Masterwork;
- unlock;
- Order completed.

---

# 61. VFX LIBRARY

## Forge
- spark;
- ember;
- heat;
- impact.

## Economy
- coin burst;
- currency fly.

## Upgrade
- flash;
- construction puff;
- level-up beam.

## Rare
- Blueprint reveal;
- Masterwork glow.

## LiveOps
- theme package.

Poolar efeitos.

---

# 62. JUICE STANDARD

Ações premium:

```text
visual
+
sound
+
haptic
+
motion
```

Aplicar a:
- upgrade;
- hire;
- district unlock;
- Masterwork;
- VIP;
- Contract complete;
- Pass reward.

---

# 63. UI — REDESIGN AUTORIZADO

Pode:
- substituir fonte;
- reconstruir HUD;
- refazer card;
- melhorar iconografia;
- mudar paleta;
- adicionar animação;
- reorganizar menus.

Objetivo:
> **parecer produto final, não ferramenta de debug bonita.**

---

# 64. HUD

Mostrar prioritariamente:
- Gold;
- Gems quando relevante;
- Order ativo;
- objetivo contextual;
- menu.

Não mostrar dez moedas/badges simultaneamente.

---

# 65. UPGRADE CARDS

Cada card:

```text
nome
efeito
custo
ícone
benefício
```

Exemplo:

> **Fole Reforçado**  
> +25% Furnace Speed  
> 500 Gold

---

# 66. MAPA DE DISTRITOS

Criar mapa.

Cada distrito mostra:
- progresso;
- renda;
- event;
- major unlock;
- Masterworks.

Visualiza expansão do negócio.

---

# 67. TELA DE DISTRITO

Elementos:
- art;
- completion;
- income;
- next unlock;
- active event;
- collection.

---

# 68. SHOP VISUAL

Cards claros:
- visual grande;
- preço;
- conteúdo;
- vantagem;
- tag.

Sem 15 popups concorrentes.

---

# 69. PASS VISUAL

Mostrar:
- free;
- premium;
- reward preview;
- season theme;
- XP progress.

---

# 70. AUDIO DIRECTION

Forge Street deve soar vivo.

Camadas:
- fire;
- metal;
- hammer;
- conveyor;
- coins;
- crowd;
- cart;
- ambience.

Quando o negócio cresce, o áudio também cresce.

---

# 71. MUSIC

Tracks:
- main;
- workshop;
- royal;
- event;
- arcane.

Medieval estilizado, não música cansativa.

---

# 72. HAPTICS

Usar em:
- upgrade;
- Masterwork;
- unlock;
- VIP;
- major sale.

Não vibrar a cada pequena venda.

---

# 73. VISUAL ROBUSTNESS GATE

Toda área/tela deve responder:

1. entende em screenshot?
2. entende em vídeo 9:16?
3. player está visível?
4. gargalo está visível?
5. estado da station está claro?
6. UI cobre gameplay?
7. parece genérico?
8. parece Forge Street?

Se falhar:
> refazer.

---

# 74. ART PIPELINE

```text
concept
↓
reference
↓
3D source/model
↓
cleanup
↓
rig
↓
animation
↓
render preset
↓
sprite sheet
↓
atlas
↓
Unity import
↓
visual QA
↓
provenance
```

---

# 75. ART QUALITY TIERS

## Tier A
Hero/marketing:
- main character;
- VIP;
- royal;
- icon;
- Masterwork;
- key district.

## Tier B
Gameplay:
- workers;
- customers;
- stations.

## Tier C
Environment:
- props;
- clutter.

Investir mais em A/B.

---

# 76. STORE ART

Produzir durante desenvolvimento:
- icon variants;
- screenshots;
- feature graphic;
- portrait video;
- store video.

---

# 77. ICON TEST

Variantes:
- character + hammer;
- anvil + fire;
- character + gold;
- Masterwork close-up.

Testar.

---

# 78. CREATIVE PRESETS

Automação de captura precisa oferecer:
- start;
- huge queue;
- first worker;
- conveyor;
- VIP;
- full district;
- Masterwork;
- Royal Contract;
- district unlock.

---

# 79. UA CREATIVE FAMILIES

- transformation;
- bottleneck;
- fail/fix;
- satisfying chain;
- upgrade choice;
- worker automation;
- VIP;
- Masterwork;
- district unlock;
- Royal Contract.

---

# 80. PRINCIPAL HOOK

> **Comece com uma bigorna. Termine comandando um distrito inteiro de artesãos.**

---


# 81. TUTORIAL E PRIMEIRA SESSÃO

Tutorial precisa continuar curto.

Ordem:
1. pegar minério;
2. alimentar furnace;
3. pegar lingote;
4. fabricar;
5. vender;
6. comprar upgrade.

Depois:
- worker;
- Order;
- rewarded.

Não explicar systems futuros cedo.

Meta inicial:
- primeira venda <90 s;
- upgrade significativo <3 min;
- automação <5–7 min;
- novo produto <8–10 min.

Esses números são targets internos e podem ser recalibrados.

---

# 82. FIRST DAY

D0 deve incluir:
- uma cadeia completa;
- primeiro worker;
- Orders;
- offline chest;
- preview de collection;
- teaser de distrito seguinte;
- primeiro rewarded opcional.

Razão para voltar:
- nova receita;
- blueprint;
- Order;
- progresso offline;
- event teaser.

---

# 83. D1–D7

## D1
Grande marco de distrito.

## D3
Blueprint / novo distrito / worker specialty.

## D7
Royal Contract / event / collection milestone.

A cada retorno importante precisa existir novidade.

---

# 84. D30–D90

## D30
Múltiplos distritos, Season, events, Masterworks.

## D60
Conteúdo avançado/Arcane.

## D90
Endgame cycles + próxima cidade/expansão.

---

# 85. ANALYTICS DE PRODUÇÃO

Adicionar stack remota antes de UA real.

Stack enxuta:
- GameAnalytics ou equivalente;
- Crash reporting;
- RemoteConfig;
- AppsFlyer OU Adjust quando attribution for necessária.

Não usar dois sistemas de attribution ao mesmo tempo.

---

# 86. EVENT TAXONOMY

Eventos mínimos:

```text
session_start
session_end
tutorial_step
tutorial_complete
first_sale
product_crafted
product_sold
upgrade_buy
worker_hired
worker_upgrade
station_unlock
district_unlock
order_start
order_complete
royal_contract_start
royal_contract_complete
masterwork_unlock
blueprint_unlock
offline_claim
rewarded_offer
rewarded_start
rewarded_complete
interstitial_show
iap_view
iap_purchase
pass_view
pass_purchase
currency_source
currency_sink
event_enter
event_complete
```

---

# 87. PERFORMANCE ANALYTICS

Registrar:
- app version;
- build id;
- device model;
- OS;
- memory class;
- fps bucket;
- session duration;
- crashes.

Não registrar informação pessoal desnecessária.

---

# 88. REMOTE CONFIG

Knobs importantes:
- costs;
- station speed;
- client rate;
- queue size;
- VIP reward;
- rewarded cooldown;
- offline cap;
- Orders reward;
- event schedule;
- interstitial cap;
- offer configuration.

Objetivo:
tunar economia sem rebuild.

---

# 89. EXPERIMENT SYSTEM

Todo A/B precisa:
- id;
- variant;
- config snapshot;
- start;
- end;
- metrics.

Não rodar muitos experimentos simultâneos com coortes pequenas.

---

# 90. CRASH REPORTING

Antes de teste comercial.

Contexto:
- version;
- scene;
- progression;
- district;
- last major event.

---

# 91. SAVE 2.0

Antes de IAP completo:

```text
SaveEnvelope
{
  schemaVersion,
  contentVersion,
  payload,
  writtenAt,
  checksum
}
```

Adicionar:
- backup;
- migration;
- corruption recovery;
- atomic replace;
- rollback.

---

# 92. CLOUD SAVE

Pode entrar na v1.0 se custo e stack permitirem.

Benefícios:
- troca de aparelho;
- recuperação;
- compra protegida.

Evitar backend custom enorme.

---

# 93. IAP SECURITY

Premium currency e entitlements não devem depender apenas de PlayerPrefs.

Implementar:
- receipt;
- restore;
- store ownership;
- server/service validation quando aplicável.

---

# 94. PRIVACY / CONSENT

Antes de inicializar monetização comercial:
- GDPR;
- US privacy;
- COPPA setting coerente com target audience;
- Data Safety;
- consent/CMP quando necessário.

LevelPlay deve receber sinalização correta.

Não manter hardcode de consentimento provisório na release.

---

# 95. AD REVENUE

Integrar impression-level revenue.

Enviar:
- placement;
- network;
- revenue;
- currency;
- session;
- progression.

Permite calcular:
- Ad ARPDAU;
- LTV;
- ROAS.

---

# 96. TARGET API

A release atual precisa validar:

> Android 16 / API 36 ou superior, conforme exigência vigente do Google Play para novos apps/updates desde 31/08/2026.

Verificar no AAB final.

---

# 97. 16 KB PAGE SIZE

Release gate deve verificar compatibilidade de 64-bit com page size 16 KB.

Não confiar apenas na versão do Unity.

Automatizar.

---

# 98. SIGNING

Release:
- AAB;
- ARM64;
- upload key fora do repo;
- secrets protegidos;
- versionCode crescente;
- non-development.

---

# 99. BUILD MODES

Criar uma entrada única:

```text
verify quick
verify full
verify android-emulator
verify release
```

POCO fica fora do fluxo cotidiano.

---

# 100. VERIFY QUICK

- core tests;
- fast view compile;
- content validator.

---

# 101. VERIFY FULL

- core;
- Unity tests;
- viewcheck;
- bot;
- save;
- event data;
- economy sanity.

---

# 102. VERIFY ANDROID-EMULATOR

- APK;
- install;
- boot;
- touch smoke;
- Back;
- background/foreground;
- mocked ads;
- basic progression.

---

# 103. VERIFY RELEASE

Checar:
- full tests;
- AAB;
- API 36;
- ARM64;
- 16 KB;
- versionCode;
- signing;
- non-development;
- privacy config;
- analytics;
- ads;
- IAP;
- clean settings.

---

# 104. QA AUTOMATION

Automatizar:
- full progression;
- every worker;
- every station;
- every product;
- every district;
- every Order type;
- every event;
- save/load;
- offline;
- ads mocked;
- IAP mocked;
- RemoteConfig variants.

---

# 105. MUTATION TESTING

Preservar e expandir para:
- economy;
- Orders;
- save;
- entitlement;
- events.

Mutation suite pesada:
- nightly/full;
- não necessariamente todo commit.

---

# 106. BOT 2.0

Perfis:

## Perfect
Ótimo.

## Humanized
Pausas, erro leve, rotas menos perfeitas.

## Slow
Teste de acessibilidade/pacing.

## F2P Economy
Sem compras.

## Payer Economy
Simula conveniência.

Bots simulam economia, não diversão.

---

# 107. ECONOMY SIMULATION

Executar milhares de simulações offline.

Perfis:
- casual;
- engaged;
- F2P;
- payer.

Detectar:
- inflation;
- dead zone;
- paywall;
- district completion;
- resource overflow.

---

# 108. CONTENT VALIDATOR

Todo conteúdo precisa:
- ID único;
- recipe válida;
- unlock válido;
- reward válido;
- localization;
- icon;
- analytics key.

---

# 109. LOCALIZATION

Preparar desde já.

Idiomas iniciais candidatos:
- pt-BR;
- en;
- es.

Não colocar texto em código.

---

# 110. ACCESSIBILITY

Adicionar:
- music/sfx controls;
- vibration toggle;
- color + shape;
- reduce effects;
- text readability;
- battery mode.

---

# 111. QUALITY SETTINGS

Perfis:
- Battery;
- Balanced;
- High.

Podem variar:
- particles;
- shadows;
- animation rate;
- render scale.

Não remover informação crítica de gameplay.

---

# 112. PERFORMANCE

Meta:
- 60 fps em aparelho forte;
- 30/45 estáveis em mid-range conforme profile.

Mais importante:
- frame pacing;
- thermal;
- battery.

---

# 113. POLICY: POCO FINAL ONLY

POCO RC:

- clean install;
- migration;
- 30 min;
- district progression sample;
- event;
- rewarded;
- IAP test;
- offline;
- background;
- Back;
- thermal.

Depois:

> não repetir sem bug específico.

---

# 114. EXTERNAL DEVICE COVERAGE

Usar:
- Google Play pre-launch;
- device farm;
- closed testers;
- outros Androids.

Assim o POCO pessoal deixa de ser gargalo.

---

# 115. RETENTION KPIs

Targets internos base:

```text
D1 >= 28%
D3 >= 14%
D7 >= 7%
D30 >= 2%
```

Targets agressivos desejáveis:

```text
D1 >= 32%
D7 >= 9%
D30 >= 3%
```

Não tratar como garantia universal.

---

# 116. MONETIZATION KPIs

- rewarded opt-in;
- completion;
- ads/DAU;
- ad ARPDAU;
- payer conversion;
- ARPPU;
- IAP ARPDAU;
- starter conversion;
- pass conversion;
- blended ARPDAU.

---

# 117. ENGAGEMENT KPIs

- sessions/day;
- session duration;
- Orders/session;
- upgrades/session;
- district progress;
- event participation;
- collection use;
- time between visible transformations.

---

# 118. UA KPIs

- hook rate;
- CTR;
- IPM;
- CPI;
- store conversion;
- D7 ROAS;
- D30 ROAS;
- creative fatigue.

---

# 119. CREATIVE SCALE

Quando criativo vence:
- variar opening;
- variar failure;
- variar payoff;
- manter hook.

Não depender de um único vídeo.

---

# 120. ASO

Testar:
- icon;
- screenshots;
- short description;
- feature art.

Keywords candidatas:
- forge;
- blacksmith;
- tycoon;
- idle;
- crafting;
- medieval;
- factory;
- shop.

Validar antes da store.

---

# 121. CONTENT CALENDAR

Antes do lançamento global:
- 8 semanas de LiveOps pré-planejadas;
- 1 Season;
- 2 eventos backup.

---

# 122. SEASON 1 — ROYAL COMMISSION

Conteúdo:
- royal cosmetics;
- contracts;
- Masterwork;
- Pass;
- event.

---

# 123. SEASON 2 — EMBER FESTIVAL

Conteúdo:
- fire visual;
- rare ore;
- furnace cosmetics;
- themed Orders.

---

# 124. EVENT PRODUCTION

Cada evento deve reutilizar template.

Não criar sistema novo toda semana.

Trocar:
- missions;
- art;
- modifiers;
- reward;
- currency.

---

# 125. CONTENT TOOLS

Criar Unity Editor tools:

- Product Editor;
- Order Editor;
- Blueprint Editor;
- Event Editor;
- District Validator;
- Economy Preview.

---

# 126. DATA-DRIVEN RULE

Evitar lógica como:

```csharp
if (district == 4)
```

espalhada.

Usar definitions/configs.

---

# 127. ARQUITETURA — EVOLUÇÃO

Preservar `FS.Core`.

Extrair View conforme necessidade:

```text
Game
├── SaveCoordinator
├── AnalyticsCoordinator
├── MonetizationCoordinator
├── SessionCoordinator
└── UIFlow
```

```text
WorldView
├── StationView
├── WorkerView
├── ClientView
├── VfxView
└── DistrictView
```

Sem big-bang refactor.

---

# 128. GAME STATES

Formalizar:

```text
Boot
Loading
Playing
Modal
Ad
Paused
Transition
```

Ajuda:
- lifecycle;
- Back;
- save;
- ads.

---

# 129. SERVICE INTERFACES

Criar:

```text
IAnalytics
IAds
IIAP
IRemoteConfig
ICrashReporter
ISaveStorage
```

Dependências externas atrás de interfaces.

---

# 130. FEATURE FLAGS

Para:
- Pass;
- districts;
- events;
- offers;
- ads;
- IAP.

Permite desligar feature sem nova build.

---

# 131. CONTENT VERSIONING

Save registra:
- schemaVersion;
- contentVersion.

RemoteConfig deve ser compatível.

---

# 132. BUG PRIORITY

P0:
- crash;
- save loss;
- purchase loss;
- progression stuck.

P1:
- economy exploit;
- ad reward fail;
- major UI.

P2:
- cosmetic.

---

# 133. ECONOMY EXPLOIT TESTS

Testar:
- clock change;
- relaunch;
- duplicate offline;
- duplicate ad reward;
- IAP restore;
- event reset;
- Order duplication.

---

# 134. PUSH NOTIFICATION

Pós-consent:
- offline reward;
- event;
- season ending;
- special Order.

Sem spam.

---

# 135. DAILY REWARD

7-day cycle.

Não resetar de forma punitiva.

Catch-up leve pode existir.

---

# 136. RETURNING PLAYER

Após ausência:
- welcome;
- offline reward;
- summary;
- current event;
- Order.

Não abrir cinco popups.

---

# 137. BUSINESS MODEL

```text
IAA
+
IAP
+
MASTERWORK PASS
+
COSMETICS
+
LIVEOPS
```

---

# 138. KPI DE ARTE

Medir também:
- CPI;
- CTR;
- store conversion;
- tutorial completion.

Arte precisa servir:
- gameplay;
- marketing;
- percepção premium.

---

# 139. KPI DE GAMEPLAY

- first_sale;
- upgrade timing;
- decision density;
- walk_no_decision;
- client_left;
- district completion.

---

# 140. KPI DE META

- collection engagement;
- Orders/day;
- Royal Contracts;
- Pass progression;
- event participation.

---

# 141. KPI DE QUALIDADE

- crash-free;
- ANR-free;
- load time;
- fps;
- save failure;
- purchase failure.

---


# 142. IMPLEMENTATION WAVES

A produção deve ser organizada em Waves.

Isso permite ambição alta sem perder coordenação.

---

# 143. WAVE A — FOUNDATION HARDENING

Objetivo:
preparar a base para produto completo.

Implementar:
- service interfaces;
- Analytics;
- Crash Reporting;
- RemoteConfig;
- feature flags;
- Save 2.0;
- content versioning;
- content validator;
- build modes.

Não depende de POCO.

## Acceptance
- core tests verdes;
- save migration test;
- mock services;
- clean build;
- no regression.

---

# 144. WAVE B — CORE EXPANSION

Implementar:
- Quality System;
- worker progression;
- worker specialties;
- Orders 2.0;
- Blueprints;
- Reputation;
- Masterworks;
- collection book.

## Acceptance
- todos data-driven;
- bot completa ciclo;
- save/load preserva;
- nenhum deadlock;
- economy simulator passa.

---

# 145. WAVE C — DISTRICTS

Criar:
- District 2;
- District 3;
- District 4;
- District 5;
- District 6.

Cada distrito precisa:
- visual identity;
- nova família de produto;
- uma nova mecânica;
- major unlock;
- Orders próprios;
- Masterworks.

Não aceitar distrito que só muda números.

---

# 146. WAVE D — META / LIVEOPS

Implementar:
- Daily;
- Weekly;
- Achievements;
- Event System;
- Masterwork Pass;
- Royal Contracts;
- event calendar;
- season data.

## Acceptance
- evento pode ser criado sem código;
- Pass configurável;
- resets corretos;
- save seguro;
- rewards sem duplicação.

---

# 147. WAVE E — MONETIZATION

Implementar:
- Rewarded placements;
- Interstitial;
- Remove Ads;
- Gems;
- Starter Pack;
- Bundles;
- Pass purchase;
- receipt/restore;
- impression revenue.

## Acceptance
- reward uma vez;
- IAP restore;
- no ad during action;
- privacy validada;
- no currency duplication.

---

# 148. WAVE F — PREMIUM ART / AUDIO

Executar:
- player redesign se necessário;
- worker redesign;
- customer readability;
- station pass;
- district pass;
- UI pass;
- VFX pass;
- lighting;
- audio;
- branding;
- store assets.

Arte começa antes desta Wave.

Esta Wave é o fechamento.

## Acceptance
- no placeholders;
- screenshot readability;
- 9:16 readability;
- consistent scale;
- final audio;
- brand system.

---

# 149. WAVE G — COMMERCIAL STACK

Implementar:
- attribution;
- store listing;
- analytics dashboards;
- ASO;
- UA capture system;
- localization;
- privacy documents;
- app-ads.txt quando aplicável;
- release automation.

## Acceptance
- analytics real;
- campaign attribution;
- release AAB;
- API compliance;
- store material pronto.

---

# 150. WAVE H — QA / RELEASE CANDIDATE

Executar:
- full automated suite;
- emulator matrix;
- economy simulation;
- soak;
- full content;
- purchase mocks;
- event calendar;
- migration tests.

Depois:
> RC freeze.

---

# 151. WAVE I — POCO FINAL

Somente agora.

Testar:
- clean install;
- migration;
- 30 min;
- representative districts;
- event;
- rewarded;
- IAP test;
- offline;
- background/foreground;
- Back;
- touch;
- thermal;
- FPS.

Resultado:

```text
POCO_RELEASE_GATE = PASS | FAIL | BLOCKED
```

Não repetir sem bug real.

---

# 152. TRABALHO EM PARALELO

## Gameplay
- systems;
- districts;
- workers;
- production.

## Product/Game Design
- economy;
- pacing;
- retention;
- LiveOps;
- offers.

## Art
- characters;
- stations;
- environments;
- UI;
- VFX.

## Platform
- analytics;
- monetization;
- release.

## QA
- automation;
- economy;
- save.

## UA
- creatives;
- store;
- hooks.

---

# 153. TIME DE PROGRAMAÇÃO — DIRETIVA

Priorizar:
- data-driven;
- tooling;
- tests;
- extensibility.

Não priorizar:
- arquitetura perfeita abstrata;
- micro-refactor sem valor.

---

# 154. TIME DE GAME DESIGN — DIRETIVA

Responsável por:
- decision density;
- pacing;
- novelty;
- economy;
- retention;
- event;
- monetization value.

Não apenas preços.

---

# 155. TIME DE ARTE — DIRETIVA

Não esperar “fim”.

Prioridade:
1. hero player;
2. forge;
3. workers;
4. customers;
5. district silhouette;
6. UI;
7. VFX;
8. store art.

Se algo atual estiver abaixo do padrão:
> substituir.

---

# 156. TIME DE UI/UX — DIRETIVA

Priorizar:
- one-hand;
- hierarchy;
- contextual info;
- clean store;
- readable upgrade.

UI não deve parecer painel de administração.

---

# 157. TIME DE ÁUDIO — DIRETIVA

Construir identidade sonora da oficina.

Não limitar-se a:
- click;
- coin.

A fábrica precisa “respirar”.

---

# 158. TIME DE QA — DIRETIVA

Automação primeiro.

POCO:
> fim.

Foco:
- save;
- economy;
- content;
- monetization;
- event;
- build.

---

# 159. TIME DE UA — DIRETIVA

Produção começa antes do release.

Meta:
- 30+ ideias de criativos;
- 10 famílias;
- icon variants;
- screenshot variants.

---

# 160. DOCUMENTAÇÃO VIVA

Manter:
- GDD;
- Economy Spec;
- Art Bible;
- Provenance;
- LiveOps Calendar;
- Monetization Spec;
- Analytics Dictionary;
- Release Checklist.

---

# 161. SINGLE SOURCE OF TRUTH

Criar/adotar:

`docs/PROJETO.md`

ou equivalente.

Deve conter:
- versão atual;
- estado;
- decisões;
- próximos marcos;
- known issues.

Documentos históricos vão para archive.

Não deixar agente/time confundir passado com estado atual.

---

# 162. VERSION PLAN

Sugestão:

## v0.7
Foundation + meta systems.

## v0.8
District expansion.

## v0.9
LiveOps + monetization + final art integration.

## v1.0 RC
Feature Complete + Store Ready.

Versões podem mudar.

Waves permanecem.

---

# 163. FEATURE COMPLETE

Não chamar Feature Complete se faltar:
- district;
- meta;
- monetization;
- LiveOps;
- analytics;
- save;
- art;
- audio;
- localization mínima.

---

# 164. DEFINIÇÃO DE JOGO COMPLETO

Para este projeto, “completo” significa:

- core polido;
- 6 districts;
- 35–45 products;
- workers;
- Quality;
- Orders;
- Contracts;
- Blueprints;
- Masterworks;
- Collection;
- Reputation;
- Daily/Weekly;
- Events;
- Pass;
- Ads;
- IAP;
- analytics;
- RemoteConfig;
- save;
- release pipeline;
- final art;
- final audio;
- store assets.

Não significa:
> nunca mais atualizar.

Live game continua depois.

---

# 165. LANÇAMENTO

Distribuição pode ocorrer em etapas sem alterar a ambição de produto.

## Internal
Time/QA.

## Closed
External testers.

## Limited Release
Poucos mercados para operação.

## Global
Após estabilidade.

Isso é estratégia de publicação, não “MVP”.

---

# 166. AGGRESSIVE BUSINESS STRATEGY

O produto deve chegar ao mercado com capacidade de:

```text
ACQUIRE
↓
RETAIN
↓
MONETIZE
↓
RE-ENGAGE
↓
SCALE
```

Não lançar só para descobrir depois como monetizar.

---

# 167. RISCO ASSUMIDO

Aceitamos:
- mais conteúdo;
- mais arte;
- maior investimento;
- LiveOps;
- monetização completa;
- mais complexidade.

Em troca buscamos:
- LTV maior;
- D30 melhor;
- mais payer conversion;
- mais ad revenue;
- mais UA creatives;
- maior percepção de valor;
- mais longevidade.

---

# 168. MAIOR APOSTA DE PRODUTO

Forge Street deixa de ser apenas:
> idle arcade.

Passa a ser:

> **tycoon de crafting com automação, coleção, contratos e eventos.**

A simplicidade permanece na interação local.

A profundidade aparece no longo prazo.

---

# 169. MAIOR RISCO DE DESIGN

Profundidade demais na tela.

Mitigação:

> **simple moment-to-moment, deep meta.**

---

# 170. MAIOR RISCO DE PRODUÇÃO

Scope.

Mitigação:
- Waves;
- data-driven;
- tooling;
- reusable event templates;
- automation.

Não reduzir ambição.

Aumentar eficiência.

---

# 171. MAIOR RISCO VISUAL

Parecer:
- asset-store;
- genérico;
- protótipo.

Mitigação:
- art direction;
- hero character;
- consistent render;
- lighting;
- UI;
- VFX;
- brand.

---

# 172. MAIOR RISCO COMERCIAL

Tema de forja não converter.

Mitigação:
- transformation;
- satisfying crafting;
- character;
- automation;
- fantasy;
- stronger visuals.

UA mede isso.

---

# 173. MAIOR RISCO DE RETENÇÃO

Conteúdo virar apenas:
> números maiores.

Mitigação:
- districts diferentes;
- mechanic novelty;
- Masterworks;
- collections;
- events;
- Contracts.

---

# 174. MAIOR RISCO DE MONETIZAÇÃO

Ads reduzirem retenção.

Mitigação:
- Rewarded-first;
- RemoteConfig;
- No Ads;
- caps;
- IAP value.

---

# 175. AUTORIZAÇÃO DE REDESIGN

Explicitamente permitido:

- novo HUD;
- nova fonte;
- novos cards;
- novos sprites;
- novos modelos;
- novos workers;
- novos clientes;
- nova iluminação;
- novo layout;
- novas animações;
- novos VFX;
- nova logo;
- novo icon;
- novo áudio.

Condição:
> qualidade, clareza e performance.

---

# 176. AUTORIZAÇÃO DE REFATOR

Refatorar se:
- desbloqueia feature;
- permite data-driven;
- permite tests;
- reduz duplicação crítica;
- permite LiveOps.

Não refatorar apenas para “ficar bonito”.

---

# 177. AUTORIZAÇÃO DE REMOÇÃO

Se uma feature:
- confunde;
- polui;
- não retém;
- piora pacing;
- não monetiza;
- quebra identidade;

ela pode ser removida.

Nada é intocável exceto:
- estabilidade;
- core loop;
- qualidade.

---

# 178. NO PLACEHOLDER GATE

Release não pode conter:
- debug text;
- temp icon;
- generic Unity material;
- mock item;
- test sprite;
- missing audio;
- placeholder store art.

---

# 179. BRAND SYSTEM

Criar:
- logo;
- typography;
- colors;
- iconography;
- tone;
- marketing guide.

Forge Street precisa ser reconhecível fora do jogo.

---

# 180. LOGO

Direção:
- metal/wood;
- bigorna/martelo;
- simples;
- legível pequeno;
- não usar lettering medieval ilegível.

---

# 181. SUCCESS — GAMEPLAY

O jogador começa:
> carregando minério.

Semanas depois:
> administra múltiplos districts, persegue Masterworks e completa Royal Contracts.

E ainda gosta de observar a cadeia funcionar.

---

# 182. SUCCESS — BUSINESS

Só dados confirmam.

Mas a arquitetura precisa permitir maximizar:
- D1;
- D7;
- D30;
- ARPDAU;
- LTV;
- payer conversion;
- ad revenue;
- creative throughput.

---

# 183. REFERÊNCIAS ATUAIS DE RELEASE

## Google Play API
A partir de 31/08/2026, novos apps e updates mobile submetidos ao Google Play precisam mirar Android 16 / API 36 ou superior, salvo exceções/extensões aplicáveis.

Fonte:
`https://support.google.com/googleplay/android-developer/answer/11926878?hl=pt-BR`

## 16 KB
Android Developers documenta requisitos de compatibilidade de page size 16 KB para apps 64-bit relevantes e enforcement adicional anunciado para 01/02/2027.

Fonte:
`https://developer.android.com/guide/practices/page-sizes`

## LevelPlay privacy
Unity fornece APIs/configuração para GDPR, CCPA/COPPA e impression-level revenue. O produto continua responsável pelo consentimento e configuração corretos.

Fontes:
`https://docs.unity.com/en-us/grow/levelplay/sdk/unity/regulations-index`
`https://docs.unity.com/en-us/grow/levelplay/platform/legal-resources`

## LiveOps
Unity Gaming Report 2025 reforça o uso amplo de daily missions/rewards, achievements/challenges, ads/IAP e LiveOps por equipes mobile.

Fonte:
`https://unity.com/resources/gaming-report-2025`

---

# 184. EXECUTION ORDER

```text
WAVE A — FOUNDATION
↓
WAVE B — CORE/META
↓
WAVE C — DISTRICTS
↓
WAVE D — LIVEOPS
↓
WAVE E — MONETIZATION
↓
WAVE F — PREMIUM ART/AUDIO
↓
WAVE G — COMMERCIAL STACK
↓
WAVE H — QA/RC
↓
WAVE I — POCO FINAL
↓
STORE
```

Arte/UX começam antes de Wave F.

Wave F é o fechamento final.

---

# 185. HANDOFF PARA O TIME

Antes de implementar:

1. ler este documento;
2. ler auditoria;
3. ler GDD atual;
4. ler Balance;
5. ler Art Bible;
6. ler Proveniência;
7. ler backlog;
8. mapear estado real do código;
9. classificar cada feature como:
   - KEEP
   - UPGRADE
   - REPLACE
   - NEW
10. criar tickets por Wave.

---

# 186. REGRA DE EXECUÇÃO

Ao encontrar uma divergência entre este documento e código antigo:

- validar intenção;
- preservar compatibilidade quando valiosa;
- priorizar direção comercial atual.

Não implementar cegamente documentação histórica.

---

# 187. REGRA DE QUALIDADE

Toda feature precisa passar quatro perguntas:

1. melhora o jogo?
2. melhora retenção?
3. melhora monetização ou valor percebido?
4. pode ser mantida?

Se nenhuma:
> não entra.

---

# 188. REGRA DE ARTE

Toda peça precisa passar:

1. é clara?
2. é bonita?
3. é coerente?
4. vende bem em screenshot/video?
5. roda bem?

Se não:
> iterar/substituir.

---

# 189. REGRA DE MONETIZAÇÃO

Toda oferta precisa responder:

> “qual valor real recebe o jogador?”

Não usar frustração artificial como principal argumento de venda.

---

# 190. REGRA DE LIVEOPS

Todo evento precisa reutilizar infraestrutura.

Se cada evento exige semanas de programação:
> framework falhou.

---

# 191. REGRA DE PERFORMANCE

Performance não é razão para deixar o jogo feio antecipadamente.

Primeiro:
- construir target visual;
- medir;
- criar quality scaling.

Não sacrificar identidade sem profiling.

---

# 192. REGRA DE POCO

Repetindo por importância:

> **POCO F4 não é ferramenta de teste cotidiano.**

É:
> **aparelho de confirmação final de marcos.**

---

# 193. DEFINIÇÃO FINAL DO PRODUTO

> **Forge Street é um tycoon de crafting medieval/fantástico para mobile no qual uma pequena oficina manual cresce para uma rede de distritos automatizados, com produção visível, gargalos, trabalhadores, contratos, Blueprints, Masterworks, eventos, Season Pass e progressão de longo prazo.**

---

# 194. PROMESSA FINAL

Gameplay:

> **“Eu construí isso.”**

Meta:

> **“Sempre existe algo novo para melhorar, automatizar, colecionar ou desbloquear.”**

Negócio:

> **“O produto possui conteúdo, retenção e monetização suficientes para continuar crescendo depois do lançamento.”**

---

# 195. DIRETIVA FINAL

A equipe não deve buscar a versão mais segura de Forge Street.

Deve buscar:

> **a versão comercialmente mais forte que conseguimos sustentar com qualidade.**

Estamos autorizados a correr risco em:

- conteúdo;
- arte;
- profundidade;
- LiveOps;
- monetização;
- marketing.

Mas o risco deve comprar:
- maior LTV;
- maior retenção;
- melhor conversão;
- melhor percepção de valor;
- maior capacidade de escala.

Esse é o objetivo.
