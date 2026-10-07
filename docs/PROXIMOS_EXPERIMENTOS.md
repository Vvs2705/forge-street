# Próximos experimentos — Forge Street

**Data:** 2026-10-07. **Estado:** PROPOSTO, sem implementação. Este documento cobre apenas o Passo D §§1–2 do handoff. Não abre a Leva 8 nem autoriza expansão, ads/IAP ou publicação.

> **Atualização 2026-10-07 (coordenador):** a opção "Luxo visual" foi implementada na Leva 8 com preços **11.000 / 14.000 / 18.000** (medidos; 6.000/10.000/16.000 compravam a Fachada cedo demais e o teto offline pagava dois luxos de uma vez). Vigente em `FASE3_LUXO.md` e `BALANCE.md` §11. Logística: nenhuma variante deste documento venceu (`ESTUDO_LOGISTICA.md`); próximo A/B planejado = 2º ajudante de minério.

## Base medida

Fonte vigente: [BALANCE.md §10.7](BALANCE.md#107-passo-a--custos-aprovados-e-vitrine-com-joia-80-vigente-2026-10-07), [log depois do ajuste](../client/Builds/validation_phase2/core_after_measure.log) e [log anterior](../client/Builds/validation_phase2/core_before.log). Os logs foram produzidos pelo responsável pelo core; nenhum experimento deste documento foi executado.

| Medida do bot humano, 60 min | Vigente |
|---|---|
| Joalheiro / Lupa / Vitrine | 2.200 / 5.500 / 9.000 ouro |
| Compra dos três | 25:25 / 31:12 / 40:41 |
| Joia depois da Vitrine | 80 ouro; antes, 60 |
| Ouro ganho por vendas / saldo aos 60 min | 46.377 / 21.212 |
| Joias vendidas / joias por minuto após tudo | 269 / 7,2 |
| Ouro/min, janela 55–60 | 1.095 |
| Fome da bancada: Joalheiro→Lupa / Joalheiro→60 min | 33% / 52% |

Os antigos 32:20, 951 ouro/min, 276 joias e 35–53% de fome são **históricos**. O A/B antigo de BALANCE §10.2 não mede nenhuma das variantes abaixo. Para atribuir o efeito do preço 80, usar o A/B atual de custos iguais em §10.7: 947→1.095 ouro/min; comparar 951→1.095 mistura custo e preço.

## 1. Ouro sem destino — opções PROPOSTAS

A direção documental é rua→distrito e certificação sem reset, conforme [GDD §§4–5](GDD.md). Isso é planejamento, não conteúdo já produzido nem retenção validada.

| Opção | Escopo e preços PROPOSTOS | Limite |
|---|---|---|
| **Luxo visual — recomendada** | Três compras únicas depois dos 20 upgrades: **6.000 / 10.000 / 16.000**. Melhorias visíveis de fachada, oficina e joalheria com elementos existentes | Sem aumento de produção, moeda nova ou devolução de ouro. Gasto total proposto: 32.000 |
| Certificação de distrito sem reset | Patamares **12.000 / 18.000 / 26.000**, título/placa e metas de vendas. Primeiro patamar proposto: **250 joias + 2.000 vendas** | Não chamar de reset; não adicionar bônus multiplicativos ou recompensa em ouro |
| Terceira área mínima | **18.000** para abrir + **12.000** para a primeira linha; inicialmente só uma estação e uma loja | Maior escopo de arte, navegação e economia. A renda da nova linha não foi medida |

**Projeção, não medição:** 32.000 / 1.095 equivale a aproximadamente 29 minutos de renda à taxa final observada. Isso não prevê o tempo real de compra: saldo prévio, variação de renda, caminhada e offline alteram o resultado.

**Cuidado com o offline:** o teto vigente após comprar tudo é **18.000**. Um claim que alcance esse teto paga imediatamente os luxos de 6.000 + 10.000 e ainda deixa 2.000. Antes de aprovar preços, comparar início com saldo zero, saldo real aos 60 min e retorno com claim no teto. Novos itens não podem aumentar o teto por acidente ao alterar o último upgrade ou o menor travado. Não mudar a regra offline nesta proposta.

**Gate humano PROPOSTO:** antes de uma terceira área, mostrar as transformações a jogadores e observar se escolhem gastar ouro nelas e se desejam continuar depois dos 20 upgrades. Um bot comprando tudo não prova interesse nem retenção. Luxo sem valor percebido é somente atraso; nesse caso, revisar a hipótese antes de aumentar escopo.

## 2. Joalheria sem lingote — A/B PROPOSTO

| Variante | Configuração inicial PROPOSTA | Hipótese |
|---|---|---|
| **Esteira lateral — primeira candidata** | Buffer de **2 lingotes**; testar **1 lingote a cada 2 / 3 / 4 s**, transferindo somente estoque real da fonte | Reduzir distância e disputa de transporte |
| Segundo Joalheiro | **1 ajudante adicional**, velocidade e capacidade existentes, dedicado à joalheria | Aumentar transporte; não cria lingotes |
| Fornalha lateral | **1 fornalha** com configuração existente, alimentada com minério e destinada à joalheria | Criar oferta local, se produção for o gargalo |

**Controle idêntico:** capturar o mesmo estado aos **41 min**, com os 20 upgrades comprados, e clonar para controle e variantes. Mesmos estoques, posições, filas, saldo, timers, bot, passo de simulação, custos e preço 80. Janela **41–60 min**, sem compras adicionais ou mudanças simultâneas de demanda, receita, recompensa ou offline. Comparar cada variante isoladamente; nunca comparar uma variante atual com o histórico de §10.2.

Antes do A/B, confirmar onde falta oferta: lingotes acumulados nas fontes apontam para transporte; fontes vazias ou fornalhas sem minério exigem resolver produção/alimentação. Uma esteira ou mais um trabalhador não cria estoque.

Registrar fome da bancada, joias/min, ouro/min total, outras vendas/min, estoques de lingote nas fontes, fome/trava das fornalhas e caminhada. Usar a mesma janela e definição de fome para controle e variante: os 52% atuais cobrem Joalheiro→60 min e não são automaticamente o controle da janela 41–60.

**Critérios iniciais PROPOSTOS, não resultados:** fome ≤35%; joias/min ≥15% acima do controle; ouro/min total não cai; outras vendas/min não caem mais de 5%. Se falhar, revisar a distribuição de lingotes e a hipótese, sem compensar aumentando preço da joia.

**Preço/retorno PROPOSTOS:** só precificar o vencedor após medir ganho incremental positivo. Faixa inicial de retorno: **8–12 min**; `custo ≈ Δouro/min × 8…12`, arredondado a múltiplo de 5. Não há preço aprovado ou retorno medido para estas variantes.

## Recomendação e limites

Depois de fechar A–C, priorizar validação humana do luxo e diagnóstico/A/B da esteira. Certificação e terceira área permanecem alternativas. Uma futura implementação precisa preservar save antigo, primeiros 10 min e offline, medir antes/depois e passar pelo portão Unity. Não há testes, builds ou implementação novos decorrentes deste documento.
