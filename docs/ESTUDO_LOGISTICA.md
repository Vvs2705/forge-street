# Estudo de logística — joalheria sem lingote (Leva 8, evidência 4 de `FASE3_LUXO.md`)

**Data:** 2026-10-07. **Raia:** economia/QA. **Estado:** diagnóstico MEDIDO; A/B MEDIDO numa cópia temporária do core. **Nada implementado no jogo**: nenhuma linha de `client/Assets` mudou. Os números de luxo ficam em [BALANCE.md §11](BALANCE.md) e não entram aqui.

> **Atualização Leva 10 (2026-10-07):** o par Mineiro → Joalheiro 2 foi **implementado** por cima da física da fase 5. O resultado, com o A/B refeito com física, está em §6.
>
> **Atualização Leva 9 (2026-10-07, raia core/economia):** A/B do **2º ajudante de minério isolado** medido em §5. **Nenhuma variante do escopo passa os 4 critérios** (genérico 1/4 bases, dedicado à Fornalha 2 0/4), então **nada de logística foi implementado**. A pendência de arquitetura de §4 está resolvida: a marca de luxo agora é por upgrade (`UpgradeDef.Luxury`, [BALANCE.md §12](BALANCE.md)), sem mudança de comportamento.

## Fonte congelada e método

- **Fonte:** `Sim.cs` / `Defs.cs` / `Bot.cs` de 2026-10-07. SHA-256: `Sim.cs` 8fabe0eb…f6af6f6, `Defs.cs` 5b0f0d92…a6b49, `Bot.cs` 110e0a5d…749519f (lista completa em `%TEMP%\fs_ab\logs\core_freeze_sha256.txt`). Depois do estudo, só as 3 constantes de preço do luxo mudaram em `Defs.cs`; elas não afetam a janela medida.
- **Diagnóstico:** harness `%TEMP%\fs_ab\diag`, compilando o core **real** sem alteração e lendo só o estado público (estações, ajudantes, eventos).
- **A/B:** harness `%TEMP%\fs_ab\ab`, sobre uma **cópia** do core com ganchos desligados por padrão (esteira lateral, fornalha dedicada). Com os ganchos desligados, a cópia reproduz o core real **ao byte**: save aos 41:00 e relatório do controle idênticos ao diagnóstico.
- **Controle idêntico aos 41 min:** a simulação é determinística. Cada variante refaz a mesma partida desde 0:00 (mesmo bot humano, Dt 1/30, mesma entrada) e o harness confere que o save aos 41:00 é igual ao do controle antes de aplicar a variante. Assim, fila, ajudantes em trânsito, timers e o estado interno do bot são clonados exatamente, o que um Save/Load não faria. Estado aos 41:00: os 20 produtivos comprados (produção completa 40:41), quatro baús abertos, saldo 426, preço da joia 80.
- **Sem compras na janela:** os pads de luxo (17–19) ficam fora da partida (`Pads.RemoveAll`). No jogo atual o bot compraria a Fachada aos 46:02, dentro da janela. O luxo não muda a produção (BALANCE §11.1), mas o desvio até o pad adicionaria ruído.
- **Janela:** 41:00–60:00 (19 min). **Fome** = mesma definição de §10.7: segundos com a bancada parada sem os 2 lingotes (`StarveSeconds`) ÷ duração da janela.
- **Robustez:** a simulação não tem semente. Para ter bases independentes, repeti o A/B com outro passo de integração (Dt 1/60, trajetória diferente desde 0:00) e com janelas deslocadas: **A** Dt 1/30, 41–60 · **B** Dt 1/60, 41–60 · **C** Dt 1/30, 45–64 · **D** Dt 1/60, 50–69. Em todas, a produção estava completa e os baús abertos antes da janela (40:41 com Dt 1/30; 40:39 com Dt 1/60).

## 1. Diagnóstico: onde falta lingote (controle, 41:00–60:00)

Controle: ouro/min **1.094,0**, joias/min **7,21**, outras vendas/min **35,74**, bancada de joias sem lingote **56%** da janela. Com a definição de §10.7 (Joalheiro → 60 min), a mesma partida dá **52%**, igual ao documento.

| estação | fome % | trava % | ocupada % | feitos/min | saída média | saída = 0 | saída ≥ 2 | saída cheia | entrada média |
|---|---|---|---|---|---|---|---|---|---|
| Fornalha | **22** | 0 | 78 | 28,47 lingotes | **0,55** de 4 | 55% | 9% | 0% | 1,03 minério de 6 |
| Fornalha 2 | **22** | 0 | 78 | 28,47 lingotes | **0,77** de 4 | 48% | 17% | 2% | 1,03 minério de 6 |
| Bigorna | 0 | 3 | 97 | 19,42 espadas | 0,72 | 45% | 14% | 3% | 3,62 |
| Bigorna 2 | 100 | 0 | 0 | 0 | 0 | 100% | 0% | 0% | 0 |
| Escudos | 48 | 1 | 52 | 6,84 | 0,35 | 70% | 5% | 1% | 0,84 |
| Ferramentas | 52 | 1 | 47 | 9,37 | 0,65 | 51% | 14% | 2% | 0,56 |
| Joalheria | **56** | 0 | 44 | 7,26 | 0,25 | 76% | 1% | 0% | 0,68 |

Fluxo de lingote (por minuto): fornalhas produzem **56,9** (76% da capacidade de 2 × 37,5). Consumo: Bigorna 19,42 (todos pela esteira principal), Escudos 13,74, Ferramentas 9,37 e Joalheria **14,47** (Joalheiro 11,42 + Ajudante 2 3,05), total 57,0. Capacidade aberta das bancadas, sem a Bigorna 2: 20 + 26,7 + 20 + 33,3 = **100 lingotes/min**. A oferta cobre 57%.

| quem | medido na janela |
|---|---|
| **Minério no depósito** | infinito por desenho (`Kind.Deposit` não tem estoque). **Não falta minério no depósito; falta minério na fornalha** (entrada média 1,03 de 6). |
| **Ajudante 1** (minério) | 57,0 minérios/min = exatamente o que as fornalhas fundem; 14,3 viagens/min com carga cheia (4); **0% sem alvo**: 35% pegando no depósito (0,4 s por minério), 49% andando, 16% descarregando. **Saturado.** |
| Jogador (bot) | não leva minério nem lingote nesta fase (0/min); 30% andando vazio, 26% parado vazio, o resto levando produto ao balcão. |
| **Ajudante 2** (lingote) | 12,1 viagens/min, 117 m/min, 26,2 lingotes/min (Escudos 13,74, Ferramentas 9,37, Joalheria 3,05). **~38% do tempo parado nas fornalhas** esperando lingote (32% com carga parcial, 6% vazio); 0% sem alvo. |
| **Joalheiro** | 10,8 viagens/min, 142 m/min, 11,42 lingotes/min (7,79 da Fornalha 2, 3,63 da Fornalha). Tempo: 19% levando lingote, 26% indo vazio às fornalhas, 13% levando joia à loja, 8% voltando vazio à bancada, 8% nas fornalhas, **12% parado com joia na mão** (espera `WorkerPatience` 1,2 s antes de entregar) e 3% parado com lingote (bancada cheia). |

**Conclusão do diagnóstico:** a falta está **na fonte, não no transporte**. As fornalhas quase nunca acumulam lingote (média abaixo de 1, vazias metade do tempo, cheias no máximo 2%) e passam **22% do tempo sem minério**. Os dois transportadores de lingote esperam na fornalha. O Ajudante 1 está saturado, e a oferta (57/min) fica bem abaixo da capacidade aberta das bancadas (~100/min). A joalheria fica com a sobra: é a bancada mais distante (10,5 m da Fornalha, 7,5 m da Fornalha 2) e disputa com outras três linhas. Pela regra de PROXIMOS_EXPERIMENTOS §2, *fontes vazias / fornalhas sem minério exigem resolver produção/alimentação*. Esteira ou mais um carregador de lingote só redistribuem o que existe.

Observações de passagem, que não são bug e não foram alteradas: (a) a **Bigorna 2** fica 100% ociosa no fim de jogo, porque a esteira principal já abastece a demanda de espadas e o balcão de espadas satura; (b) o Joalheiro passa 12% do tempo esperando a paciência de 1,2 s com joia na mão. Entregar sem esperar é um ajuste de comportamento para o dono do core, **não medido** aqui.

## 2. A/B (cópia do core, variantes isoladas a partir do mesmo estado aos 41:00)

Variantes, como PROPOSTO em PROXIMOS_EXPERIMENTOS §2:
- **Esteira lateral:** fonte = Fornalha 2 (a mais próxima da rua). A cada 2/3/4 s move 1 lingote **somente se houver na saída** para um buffer de 2; o buffer descarrega na bancada de joias quando cabe (mesma regra da esteira principal: fonte vazia ou buffer cheio zera o timer).
- **2º Joalheiro:** mais um ajudante papel 3, com velocidade e carga atuais (3,4 m/s, 4).
- **Fornalha lateral:** 1 fornalha com a configuração atual (1,6 s, entrada 6, saída 4) em (12; 3,5), 3 m acima da bancada de joias. É dedicada: só o Joalheiro tira lingote dela, e o Ajudante 2 e o jogador não. O minério vem pela logística existente.
- Duas variantes **EXPLORATÓRIAS**, fora da lista PROPOSTA, para testar a hipótese do diagnóstico: **2º ajudante de minério** (mais um papel 0) e **fornalha lateral + 2º ajudante de minério** (combinação, portanto não isolada).

### 2.1 Base A (Dt 1/30, 41–60): tabela completa

| variante | joias/min | Δ | fome joias | ouro/min | Δ | outras vendas/min | Δ | saída média F1/F2/FL | fome F1/F2/FL | trava F1/F2/FL | jogador m/min | ajudantes m/min | critérios |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| controle | 7,21 | — | 56% | 1.094,0 | — | 35,74 | — | 0,55/0,77/– | 22/22/– | 0/0/– | 122 | 467 | — |
| esteira lateral 1/2 s | 10,53 | +46,0% | 36% | 1.265,9 | +15,7% | 30,90 | **−13,5%** | 0,34/0,40/– | 22/22/– | 0/0/– | 121 | 443 | falha (fome, outras) |
| esteira lateral 1/3 s | 9,21 | +27,7% | 44% | 1.200,6 | +9,7% | 33,47 | **−6,3%** | 0,41/0,53/– | 22/22/– | 0/0/– | 120 | 467 | falha (fome, outras) |
| esteira lateral 1/4 s | 8,74 | +21,2% | 48% | 1.173,1 | +7,2% | 33,47 | **−6,3%** | 0,44/0,56/– | 22/22/– | 0/0/– | 121 | 458 | falha (fome, outras) |
| 2º Joalheiro | 9,89 | +37,2% | 41% | 1.239,5 | +13,3% | 32,63 | **−8,7%** | 0,23/0,52/– | 22/22/– | 0/0/– | 120 | 617 | falha (fome, outras) |
| fornalha lateral | 7,21 | 0,0% | 56% | 1.094,0 | 0,0% | 35,74 | 0,0% | 0,55/0,77/**0,00** | 22/22/**100** | 0/0/0 | 122 | 467 | falha (nada muda) |
| EXPL. 2º ajudante de minério | 10,05 | +39,4% | 39% | 1.393,8 | +27,4% | 39,90 | +11,6% | 1,78/1,62/– | **0/0**/– | 6/9/– | 125 | 639 | falha (fome 39%) |
| EXPL. fornalha lateral + 2º aj. minério | 11,42 | +58,4% | **31%** | 1.531,1 | +39,9% | 40,42 | +13,1% | 2,24/1,85/3,47 | 0/1/2 | 13/12/73 | 125 | 648 | **passa os 4** |

Critérios PROPOSTOS: fome ≤ 35%; joias/min ≥ +15% sobre o controle; ouro/min total não cai; outras vendas/min não caem mais de 5%. "Caminhada": metros por minuto do jogador e soma dos ajudantes.

### 2.2 Robustez nas quatro bases (Δ sobre o controle de cada base)

| variante | joias/min Δ (A/B/C/D) | fome joias | ouro/min Δ | outras vendas Δ | passa? |
|---|---|---|---|---|---|
| controle (valores) | 7,21 / 7,79 / 7,21 / 7,16 | 56 / 53 / 56 / 56% | 1.094 / 1.126 / 1.090 / 1.090 | 35,7 / 35,3 / 35,5 / 36,2 | — |
| esteira 1/2 s | +46 / +32 / +51 / +46% | 36 / 38 / 34 / 37% | +16 / +11 / +18 / +16% | **−13,5 / −10,3 / −14,1 / −14,3%** | não (0/4) |
| esteira 1/3 s | +28 / +20 / +25 / +29% | 44 / 44 / 45 / 45% | +10 / +7 / +9 / +10% | **−6,3 / −6,9 / −5,3 / −8,3%** | não (0/4) |
| esteira 1/4 s | +21 / +14 / +21 / +19% | 48 / 47 / 47 / 49% | +7 / +5 / +8 / +7% | −6,3 / −4,8 / −5,6 / −6,1% | não (0/4) |
| 2º Joalheiro | +37 / +28 / +29 / +32% | 41 / 40 / 44 / 43% | +13 / +10 / +10 / +11% | **−8,7 / −7,9 / −8,2 / −9,0%** | não (0/4) |
| fornalha lateral | 0 / 0 / 0 / 0% | = controle | 0 | 0 | não (0/4) |
| EXPL. 2º aj. minério | +39 / +40 / +39 / +49% | 39 / 34 / 39 / 35% | +27 / +29 / +28 / +34% | +11,6 / +11,8 / +11,3 / +9,8% | no limite (fome) |
| EXPL. fornalha lateral + 2º aj. minério | +58 / +51 / +61 / +67% | **31 / 29 / 30 / 28%** | +40 / +38 / +43 / +44% | +13,1 / +13,7 / +17,2 / +14,4% | **sim (4/4)** |

Saídas completas: `%TEMP%\fs_ab\logs\ab_base_A.txt`, `ab_detalhe_base_A.txt` (fluxo e ocupação por variante) e `ab_bases_B_C_D.txt`.

### 2.3 Leitura

- **Nenhuma das três variantes PROPOSTAS vence.** Esteira lateral e 2º Joalheiro aumentam joias e ouro só porque **tiram lingote das outras linhas**: as fornalhas produzem os mesmos 56,9/min, a fome delas não muda (22%), e Escudos e Ferramentas passam fome. O ouro sobe porque um lingote vale 40 na joia (80 ÷ 2) e de 10 a 16 nas outras linhas. É troca de mix, não ganho de oferta, e o critério "outras vendas não caem mais de 5%" barra isso, como deve. Nenhuma chega à fome ≤ 35% de forma estável (a melhor, 1/2 s, fica entre 34% e 38%).
- **A fornalha lateral, como especificada, não faz nada:** 100% sem minério. O Ajudante 1 já está saturado e nunca escolhe a fornalha mais distante, e o bot não leva minério nessa fase. Qualquer nova fornalha alimentada pela logística atual só dividiria os mesmos 57 minérios/min.
- **A hipótese do diagnóstico se confirma nas exploratórias:** com um 2º ajudante de minério, as fornalhas param de passar fome (22% → 0%), passam a acumular lingote (saída média 1,6–1,8) e **todas** as linhas vendem mais (+10 a +12% nas outras vendas, +27 a +34% de ouro). A fome da joalheria só cai para 34–39%, porque o gargalo vira o transporte até a rua. A combinação **fornalha lateral + 2º ajudante de minério** passa os quatro critérios nas quatro bases, mas é combinação e não variante isolada.

## 3. Vencedor e preço

**Sem vencedor** entre as variantes PROPOSTAS. Nenhum preço sugerido para elas.

**Decisão do coordenador (2026-10-07):** o próximo A/B **aprovado para planejamento** é o **2º ajudante de minério, isolado**. Ele NÃO está implementado no jogo e não tem preço aprovado. Antes de virar upgrade, depende da pendência técnica dos IDs produtivos ≥ 23 (`FASE3_LUXO.md`, "Pendência técnica"). A troca dos preços do luxo para 11.000 / 14.000 / 18.000 não altera este estudo: o A/B roda sem os pads de luxo, e a produção até 41:00 é a mesma.

Para o próximo A/B (HIPÓTESE, não preço): pela regra `custo ≈ Δouro/min × 8…12`, o 2º ajudante de minério (Δ +300 a +365 ouro/min) daria **~2.400–4.400**, e a combinação (Δ +425 a +483) daria **~3.400–5.800**. Antes de precificar, medir isoladamente: (1) 2º ajudante de minério; (2) fornalha lateral **com alimentação própria** (por exemplo, o ajudante de minério dedicado a ela), comparada com (1). Usar o mesmo controle aos 41:00, os mesmos critérios e as quatro bases.

## 4. Riscos e limites

- **Arquitetura (RESOLVIDA na Leva 9):** qualquer vencedor vira upgrade produtivo novo, com ID ≥ 23, depois dos luxos. O core assumia produtivos contíguos 0..19 (`Pad.Current`, `Buy`, `ProductionComplete`, `CheapestLockedCost`). Agora a marca é por upgrade (`UpgradeDef.Luxury` / `Upgrades.IsLuxury`); o teste `Marca_ProdutivoAnexadoDepoisDosLuxos_ETratadoComoProdutivo` prova que um produtivo anexado depois dos luxos é tratado como produtivo ([BALANCE.md §12](BALANCE.md)).
- **Teto offline:** um 21º produtivo de ~3.000 derrubaria o teto de 18.000 para ~6.000 até ser comprado (regra "2× o menor travado"). É efeito de desenho a decidir, não bug.
- O bot humano não leva minério no fim de jogo. Uma pessoa pode levar, e isso mudaria o tamanho da fome. É HIPÓTESE até o playtest.
- As posições escolhidas (fonte da esteira = Fornalha 2; fornalha lateral em (12; 3,5)) são minhas e não constam do PROXIMOS_EXPERIMENTOS. A fornalha lateral não depende da posição enquanto o minério vier do Ajudante 1 saturado.
- Simulação determinística: "bases" = passo e janela diferentes, não sementes aleatórias.

## 5. A/B do 2º ajudante de minério, isolado (Leva 9, 2026-10-07)

**Estado:** MEDIDO. **Nenhuma variante do escopo passa os 4 critérios.** Por isso nada de logística entrou em `client/Assets`, nenhum upgrade novo existe e nenhum preço foi aprovado. `docs/FASE4_MINERIO.md` não foi criado, porque o contrato só existiria com um vencedor.

### 5.1 Método

- **Fonte:** core real depois da marca de luxo da Leva 9 ([BALANCE.md §12](BALANCE.md)). SHA-256: `Sim.cs` b0332cde…bc818ea, `Defs.cs` da2ba2e0…23943b1, `Bot.cs` 110e0a5d…749519f. A marca não muda a partida: em 90 min, com e sem luxo e com Dt 1/30 e 1/60, o core antigo e o novo produzem a mesma saída byte a byte (BALANCE §12).
- **Cópia:** `%TEMP%\fs_ab9\ab\core`, com ganchos **desligados por padrão**: `Carrier.Only` (destino fixo de um papel 0) e `Sim.JewelerNoWait` (só nas exploratórias). Com os ganchos desligados, o controle da cópia repete o controle de §2 ao centésimo: 7,21 joias/min, 56%, 1.094,0 ouro/min e 35,74 outras vendas/min na base A.
- **Mesmo método de §0:** cada variante refaz a partida desde 0:00 com o bot humano e sem os pads de luxo. O harness confere que o save no início da janela é igual ao do controle (SHA impresso no log) antes de aplicar a variante. As quatro bases são as mesmas de §0: **A** Dt 1/30, 41–60 · **B** Dt 1/60, 41–60 · **C** Dt 1/30, 45–64 · **D** Dt 1/60, 50–69.
- **Variantes:**
  - **genérico:** +1 ajudante papel 0 nascendo no `HireSpot`, com a velocidade e a carga atuais dos ajudantes (3,4 m/s, 4).
  - **dedicado à Fornalha 2:** igual ao genérico, mas só entrega na Fornalha 2. Com a fornalha cheia, ele espera.
- **Regra de decisão, registrada antes de medir:** a variante passa se cumprir os 4 critérios de PROXIMOS_EXPERIMENTOS §2 na base A **e** em todas as bases de robustez. Os critérios são fome da joalheria ≤ 35%, joias/min ≥ +15%, ouro/min total sem queda e outras vendas/min sem queda maior que 5%.

### 5.2 Resultado (Δ sobre o controle da mesma base)

| variante | base | joias/min (Δ) | fome joias | ouro/min (Δ abs / %) | outras vendas/min (Δ) | critérios |
|---|---|---|---|---|---|---|
| controle | A / B / C / D | 7,21 / 7,79 / 7,21 / 7,16 | 56,26 / 53,34 / 55,99 / 56,47% | 1.094,0 / 1.125,6 / 1.090,4 / 1.090,1 | 35,74 / 35,26 / 35,47 / 36,16 | — |
| **genérico** | A | 10,05 (+39,4%) | **39,46%** | 1.393,8 (+300 / +27,4%) | 39,90 (+11,6%) | falha (fome) |
| | B | 10,89 (+39,9%) | 34,30% | 1.455,1 (+329 / +29,3%) | 39,42 (+11,8%) | passa |
| | C | 10,00 (+38,7%) | **39,45%** | 1.390,2 (+300 / +27,5%) | 39,47 (+11,3%) | falha (fome) |
| | D | 10,68 (+49,3%) | **35,04%** | 1.455,4 (+365 / +33,5%) | 39,68 (+9,8%) | falha (fome, por 0,04 pp) |
| **dedicado à Fornalha 2** | A | 10,16 (+40,9%) | **38,70%** | 1.391,1 (+297 / +27,2%) | 38,79 (+8,5%) | falha (fome) |
| | B | 10,74 (+37,8%) | **35,88%** | 1.444,3 (+319 / +28,3%) | 39,05 (+10,7%) | falha (fome) |
| | C | 10,32 (+43,1%) | **37,60%** | 1.403,3 (+313 / +28,7%) | 39,05 (+10,1%) | falha (fome) |
| | D | 10,42 (+45,6%) | **37,19%** | 1.412,7 (+323 / +29,6%) | 38,11 (+5,4%) | falha (fome) |

**Genérico: passa em 1 de 4 bases. Dedicado: passa em 0 de 4.** Os dois falham só na fome. Os outros três critérios passam com folga em todas as bases.

**Por que a fome não cai abaixo de 35% (detalhe da base A, `ab9_detalhe_base_A.txt`):**
- **A fonte deixou de ser o limite.**
  - As fornalhas passam de 22% para **0%** de fome e ficam 91–94% ocupadas. Produzem 67,5 lingotes/min, dos 75 possíveis.
  - A saída média sobe de 0,55/0,77 para 1,78/1,62. O minério entregue sobe de 57,0 para 68,1/min.
  - Os dois papéis 0 somados ficam 33% do tempo parados com minério, esperando vaga na fornalha. No dedicado são 71%: ele espera a Fornalha 2, que trava 11% do tempo.
- **O gargalo passou a ser o transporte até a rua.**
  - A joalheria recebe 20,3 lingotes/min (Ajudante 2 13,1 + Joalheiro 7,2), para 33,3 de capacidade. Daí os 39% de fome.
  - O Joalheiro gasta 19% do tempo parado com joia na mão (paciência de 1,2 s) e 20% levando joia à loja. No controle eram 12% e 13%.
  - O Ajudante 2 leva lingote à joalheria 25% do tempo; no controle eram 11%.
- **Dedicar à Fornalha 2 não ajuda.** A Fornalha 2 é a fonte principal do Joalheiro, mas o Ajudante 1 já cobre a outra fornalha. Fixar o destino só faz o novo ajudante esperar mais.

### 5.3 Sondagens EXPLORATÓRIAS (fora do escopo, só para a recomendação; não são candidatas desta leva)

| variante EXPLORATÓRIA | fome joias A / B / C / D | joias/min Δ | ouro/min Δ | outras vendas Δ | passa? |
|---|---|---|---|---|---|
| + aj. minério + Joalheiro entrega a joia sem esperar a paciência (gancho `JewelerNoWait`) | 37,00 / 33,29 / 35,91 / 32,21% | +43 a +57% | +330 a +380 (+30 a +35%) | +6,1 a +9,8% | 2/4 |
| + aj. minério + 2º Joalheiro (papel 3, mesma velocidade/carga) | **16,97 / 17,49 / 20,29 / 16,68%** | +77 a +93% | +520 a +583 (+48 a +53%) | +3,1 a +8,5% | **4/4** |

- **Mineiro + 2º Joalheiro:** passa com folga. Isolado, o 2º Joalheiro tirava lingote das outras linhas (§2: outras vendas −8 a −9%). Com o 2º ajudante de minério sobra lingote na fonte, então ele não tira mais. O que ele acrescenta sobre o Mineiro sozinho é de **+218 a +258 ouro/min**.
- **Entregar sem esperar:** ganha só 2 a 4 pp de fome e não resolve sozinho.

### 5.4 Recomendação

1. **Não implementar o 2º ajudante de minério sozinho como resposta à fome da joalheria.** Ele falha no critério principal em 3 de 4 bases, incluindo a janela 41–60.
2. **Valor econômico, que é uma decisão de critério e não desta raia:** é a maior alavanca de fim de jogo medida até aqui (genérico: +27 a +34% de ouro/min) e todas as linhas vendem mais (outras vendas +10 a +12%). Se o coordenador quiser um 21º produtivo por ganho econômico, sem exigir a fome, a regra de retorno dá **~2.400–4.400** (Δ +300 a +365 × 8…12). É **HIPÓTESE**: o fim da produção, os 60/90 min e o offline não foram medidos com ele.
3. **Próximo A/B PROPOSTO:** o par **Mineiro → 2º Joalheiro**, com o 2º Joalheiro **exigindo** o Mineiro, porque sem ele o 2º Joalheiro tira lingote das outras linhas.
   - **Preços pela regra de retorno (HIPÓTESE):** Mineiro ~2.400–4.400; 2º Joalheiro ~1.750–3.100 (Δ +218 a +258 × 8…12).
   - **O que medir:** fim da produção em 42–48 min, primeiros 10 min intactos e o teto offline (enquanto houver um produtivo de ~3.000 travado, o "menor travado" cai para ~6.000).
   - **Luxo:** com +50% de ouro/min, os intervalos medidos em BALANCE §11, com preços de 11/14/18 mil, encurtam. É preciso medir de novo.
4. **Já resolvido para qualquer opção:** a marca por upgrade da Leva 9 permite anexar esses produtivos como IDs 23+ sem quebrar o bloqueio do luxo, `ProductionComplete` ou o teto offline. Depois da produção completa, o teto segue em 18.000 (o produtivo mais caro).

**Logs** (`%TEMP%\fs_ab9\logs\`): `ab9_todas_bases.txt` (tabela das 4 bases, com as exploratórias), `ab9_detalhe_base_A.txt` (fluxo e ocupação por variante) e `core_sha256.txt` (SHA da fonte e da cópia). Para repetir: `dotnet build -c Release %TEMP%\fs_ab9\ab` e depois `dotnet %TEMP%\fs_ab9\ab\bin\Release\net10.0\AB.dll <dtDen> <inicioMin> <fimMin> [detail]`.

## 6. Resultado da implementação (Leva 10, 2026-10-07, raia core/economia)

**Estado:** o par **Mineiro → Joalheiro 2** está IMPLEMENTADO e TESTADO no core (`docs/FASE4_MINERIO.md` §5, [BALANCE.md §14](BALANCE.md)). Mineiro 3.000, Joalheiro 2 2.400, IDs 23/24. Foi feito por cima da física da fase 5 (`docs/FASE5_FISICA_PACIENCIA.md`: corpos sólidos, duas bocas por estação, parede lateral com porta e arco, paciência por item). **NÃO COMPILADO no Unity.**

**A premissa do estudo mudou com a física.** Na mesma partida do bot humano, a bancada de joias sem lingote (Joalheiro → 60 min) foi de:
- 52% antes da física (§1, janela 41–60: 56%);
- **33%** com a física, sem o par ([BALANCE §13.4](BALANCE.md));
- **12%** com o par.

A parede lateral sozinha custa +2 pp de fome (31% → 33%).

**A/B de §5 refeito com física** (mesmo método de controle × variante, sem luxo, janela 60–90, bases Dt 1/30 e Dt 1/60; harness `%TEMP%\fs_l10\sweepb`):

| variante | ouro/min Δ | joias/min | fome da joalheria | outras vendas/min | 4 critérios |
|---|---|---|---|---|---|
| controle | — (1.326 · 1.355) | 10,67 · 11,17 | 36% · 33% | 32,93 · 32,10 | — |
| só Mineiro | +53 · +58 | 11,27 · 11,57 | 32% · 31% | +0,7% · +6,8% | não (joias/min +6% e +4%; meta ≥ +15%) |
| Mineiro + Joalheiro 2 | +296 · +291 | 14,13 · 14,00 (+32% · +25%) | **15% · 16%** | +2,3% · +12,4% | **sim nas 2 bases** |

**Leitura:**
- Com a física, o 2º ajudante de minério sozinho quase não rende. O ganho dele no estudo de antes (+27 a +34% de ouro/min) vinha das fornalhas sem minério, e com bocas separadas elas já se alimentam melhor.
- O par continua vencendo, e quase todo o ganho vem do Joalheiro 2: +233 a +243 ouro/min sobre o Mineiro.
- A regra de retorno remedida dá Mineiro 420–700 e Joalheiro 2 1.860–2.920. O preço aplicado do Mineiro (3.000) segue a faixa do contrato e a janela de fim de produção em 42–48 min. Proposta alternativa e decisão pendente em `FASE4_MINERIO.md` §5.
