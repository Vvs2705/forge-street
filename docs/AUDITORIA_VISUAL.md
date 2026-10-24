# Auditoria visual — programa de arte, etapa 1 (2026-10-16)

Pedido do Vinicius (2026-10-16): "a mesma dinâmica de melhoria de arte que fizemos no Dungeon Courier", em 7 etapas: auditoria, logo, ícone, interface v2, nitidez, recompensas com vida e teste no POCO só depois do "pode testar". Esta é a etapa 1: fotos de todas as telas do build atual (`main` 3be2c29, PC, 540 × 960) e a lista do que parece amador.

Fotos: `arte/auditoria/2026-10-16/folha_1.jpg` (abertura, início, 1ª venda, meio, oficina completa, menu de melhorias, configurações), `folha_2.jpg` (loja, metas diárias, conquistas, contratos, Hall, projetos, Renome) e `folha_3.jpg` (cofre offline, evento, Passe, encomendas, Rua dos Armeiros). Roteiro: `-testsession` + a flag de cada tela (`-privacidade`, `-menu`, `-settings`, `-lojafalsa -loja`, `-metas [conquistas]`, `-contratos`, `-hall`, `-projetos`, `-renomecard`, `-cofre`, `-evento … -eventocard`, `-pass`, `-encomendas`, `-distrito armorers_row`). Faltaram o baú de marco (`-shotchest` não apareceu em 120 s) e o VIP (`-vipnow` pede uma venda antes): refazer com warmup maior.

## O que parece amador (por prioridade)

### P1 — primeira impressão e loja (o que o jogador e a loja veem primeiro)
1. **Sem tela de título nem logo.** O jogo abre direto na pergunta de idade, por cima da oficina escurecida. Não há marca, nome do jogo nem "toque para começar".
2. **Painéis chapados.** Metas, Hall, Projetos, Loja, Passe, Evento e Configurações são retângulos creme lisos com borda fina, sem moldura, textura, cabeçalho ou ornamento. Parecem protótipo.
3. **Fonte do sistema.** Todos os textos estão numa sans genérica (Arial/Inter), sem personalidade. Títulos ("Metas", "Hall", "Loja") são texto simples alinhado à esquerda.
4. **Botões planos.** "RESGATAR", "ACEITAR", "PEGAR", "FECHAR" e o preço da loja são retângulos de cor única (verde ou marrom), sem relevo, brilho, sombra nem efeito de apertar.
5. **Loja vazia.** O "Pacote do Mestre" é texto em lista, sem arte do produto, sem selo de oferta e sem moldura. O preço é um botão verde comum.

### P2 — telas de recompensa e meta (onde o jogador sente o progresso)
6. **Recompensa sem vida.** O cofre offline ("Bem-vindo de volta! +5000") é um cartão com uma moeda; não abre baú, não tem luz, partículas nem moedas voando. O cartão do Renome tem só uma estrela com número.
7. **Recompensas em texto.** O login de 7 dias mostra "+25", "+2 Renome" em caixinhas iguais; o Passe e o evento listam "+25 de ouro", "+3 Renome" em linhas de texto. Falta ícone pintado por tipo (ouro, Renome, XP, projeto, cosmético).
8. **Barras de progresso planas.** Cinza com preenchimento verde/amarelo liso, com o número em letra minúscula por cima.
9. **Texto pequeno demais.** As linhas secundárias ("Encomenda de projeto nº 1", "Qualidade +0,15 · vel. +5%", "Venda 1.000 espadas (0/1.000)") ficam em ~10–12 px na tela de referência: ilegíveis num celular de 6,5".
10. **Selos de estado genéricos.** "NO HALL", "BLOQUEADO" e o ponto vermelho de notificação são pílulas de cor sólida sem desenho.

### P3 — HUD e mundo
11. **HUD de cima.** Ouro e dica em pílulas marrom-escuras lisas; o selo "Oficina conhecida/Mestre artesão" é minúsculo sob o ouro.
12. **Barra de baixo.** "Melhorias", "Metas", "VIP" e "2×" são retângulos marrons com ícone pequeno; não têm a mesma linguagem dos painéis nem destaque de botão principal.
13. **Rótulos do mundo.** Nomes das estações ("Bigorna 2", "Fornalha"), "Só Normal" e "Fina em 3" em letra branca pequena com contorno; na oficina cheia viram ruído.
14. **Personagens e itens pequenos e pouco nítidos.** Ferreiro, ajudantes e clientes ocupam ~60–80 px; as folhas de 128 px por célula ficam borradas quando escaladas (etapa 5: mais pixels por metro).
15. **Chão repetitivo.** O assoalho de tábuas repete o mesmo ladrilho sem variação nem sombra de contato das estações.
16. **Marca d'água "Development Build"** (some no build de release; só registro).

## Plano (etapas 2–7, ordem proposta)
- **2. Logo:** 12 opções geradas no Tripo (Nano Banana 2, grátis), em `arte/tripo/logo_opcoes/` (fora do git) e na folha enviada ao Vinicius; **aplicar só depois da escolha dele**.
- **3. Ícone do app:** adaptativo (frente 432 px com margem segura + fundo), legado e redondo nos PlayerSettings, e o 512 da loja, a partir do logo escolhido ou do mascote (o ferreiro do ícone atual, V29).
- **4. Interface v2:** fonte OFL com personalidade para títulos e botões (ex.: Cinzel Decorative/Alegreya SC nos títulos, Nunito/Baloo nos textos; decidir pela leitura a 48 px) em `Resources/Fonts`; moldura 9-slice dos painéis e botões com relevo, brilho e escala ao apertar (gerados por código quando der, pintados em `Resources/UI` quando não); ícones pintados por tipo de recompensa; fundo temático atrás dos painéis; HUD e barra de baixo na mesma linguagem.
- **5. Nitidez:** re-render dos personagens com mais ppu (teto 4096 px por folha) e escala de exibição no `meta.json`; item na mão gerado separado e preso ao osso no Blender.
- **6. Recompensa com vida:** baú/cofre que abre de verdade, luz, partículas e moedas voando no cofre offline, no Renome, nos marcos e no Charter.
- **7.** Build Windows e fotos antes/depois ao fim de cada etapa; POCO só com o "pode testar".

## Teste de silhueta dos personagens (Workers, 2026-10-24)

`python client/tools/silhuetas.py arte/auditoria/2026-10-24/silhuetas.png`: o 1º quadro do Idle virado para o sul de cada folha, em cor, em silhueta preta na escala do jogo (60 px/m, a tela de 540 px com 9 m de largura; a escala de cada folha é a do `WorldView.CharScale`) e ampliada 2×. Entram o herói nos 5 estágios e as 7 folhas de worker do build (os Porters de todos os distritos usam a `ajudante`, tingida pela linha; os Smiths, a `oficial`).

| Quem divide a tela | Silhuetas | Resultado |
|---|---|---|
| Old Forge: herói, ajudantes, oficiais (Bruna, Iara, Joaquim, Tomé), Baltazar (almoxarife), Otávio (mestre de obras), Celeste (balconista) | herói maior (92 px) com boné ou chapéu; ajudante pequeno e redondo; oficial com a ferramenta na diagonal; almoxarife com a caixa quadrada nas costas; mestre de obras com o martelo erguido; balconista baixa com a vareta | **distintas** |
| Rua dos Armeiros: herói, Ferraz (ajudante), Vulcana | Vulcana com a ferramenta na diagonal e o capacete | **distintas** |
| Bairro Artesão: herói, Dourado (ajudante), Lúcia (oficial) | | **distintas** |
| Arsenal Real: herói, cadetes (ajudante), Matias (almoxarife) | | **distintas** |
| Fundição Arcana: herói, aprendizes (ajudante), Ísis (oficial) | | **distintas** |
| Grande Forja: herói, Estrela (ajudante), Félix | Félix com o caixote nas costas e a pena | **distintas** |

Parecidas, mas nunca na mesma tela: a `oficial` e a Vulcana (ferramenta na diagonal; a Vulcana só existe na Row, onde não há oficial) e a `almoxarife` e o Félix (volume quadrado nas costas; Arsenal ou Old Forge × Grande Forja). **Herói:** os estágios 1, 2 e 3 têm silhueta própria (boné, chapéu largo, pena); o 4 e o 5 têm a mesma (pena e as ferramentas no cinto), e a leitura do 5 vem da cor (capa branca e vermelha) e da coroa. Como são o mesmo personagem em momentos diferentes, nunca lado a lado, fica como está. Se o POCO mostrar que a subida para o 5 passa despercebida, o acessório que muda a silhueta é a capa aberta nas costas (Blender, 0 crédito).
