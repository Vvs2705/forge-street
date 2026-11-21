# Publicação na Google Play — kit do dia do D-U-N-S

2026-11-21 · Estado: **PRONTO PARA PREENCHER** (nada foi enviado à Play Console). O D-U-N-S está previsto para 2026-11-25 (`RELEASE.md` §3.5).

O objetivo é que, no dia, o Vinicius só copie e cole. Cada campo da Play Console tem aqui o valor pronto, ou um `<<VINICIUS>>` quando o dado só ele tem. Os outros docs continuam valendo como fonte:
- `ASO.md`: os textos da loja e a origem deles;
- `PRIVACIDADE.md`: o inventário de dados (este kit atualiza o §2 dele);
- `IAP.md`: o fluxo de compra;
- `RELEASE.md`: a chave e o AAB.

**Quem fez:** TIME DE JOGOS, plano B (papéis simulados na sessão: release-manager, monetization-compliance e localization-lead). A ata está no §10.

**Menus conferidos** em 2026-11-21 na ajuda oficial da Play (links no §11). Não houve login na Play Console. O console é em português, e os nomes dos menus abaixo são a tradução que ele usa, seguidos do nome em inglês da ajuda. Se algum nome tiver mudado, a ordem dos passos continua a mesma.

---

## 0. O que precisa estar em mãos (antes de abrir o console)

| Item | Onde está | Situação |
|---|---|---|
| Número D-U-N-S e os dados da empresa iguais aos do pedido (razão social, CNPJ, endereço, telefone) | e-mail da CIAL | `<<VINICIUS>>` previsto para 25/11 |
| Conta de organização da Play (dona `vinicius@vstack-solutions.com.br`, parada em "Perfil para pagamentos") | Play Console | já existe (`RELEASE.md` §3.5) |
| AAB assinado com a chave de upload real | workflow `release` à mão (`gh workflow run release.yml --ref main`) ou `client/tools/verify.sh release` no PC | pipeline pronto (#178). Gerar no dia, a partir da `main` |
| SHA-1 do certificado de upload | `RELEASE.md` §3.1: `75:20:35:E8:BD:07:A5:59:A7:0C:16:4C:FE:9C:36:B3:1D:CD:D6:83` | pronto |
| URL pública da política de privacidade | §5 deste doc | **falta hospedar** (decisão D-PUB-1) |
| Ícone 512 × 512 | `arte/marca/icone_loja_512.png` (512 × 512, RGB, 298 KB) | pronto |
| Feature graphic 1024 × 500 | `arte/marca/feature_graphic.png` (1024 × 500, RGB, 613 KB) | pronto |
| Screenshots de celular | `arte/loja/screenshots/<pt\|en\|es>/1…8_*.jpg` (8 por idioma, 1080 × 1920) | prontas |
| Vídeo | `arte/loja/video/gameplay_30s.mp4` | a Play só aceita **link do YouTube**: subir como "Não listado" é publicação externa, então `<<VINICIUS>>` decide (opcional) |

## 1. Ordem do dia (passo a passo)

Cada passo diz onde clicar e qual seção deste doc tem o valor. Os passos 1 a 3 são só do Vinicius, porque envolvem a conta, os dados da empresa e o banco. Do passo 4 em diante, a sessão pode acompanhar pela tela dele se ele pedir.

1. **Perfil para pagamentos.** Retomar o cadastro onde parou e informar o D-U-N-S e os dados da empresa, iguais aos do pedido na CIAL. Esperar a verificação da organização: o Google pode pedir documento, e isso leva de horas a dias.
2. **Criar o app.** Página inicial > **Criar app**.
   - Nome: `Forge Street: Tycoon de Forja`.
   - Idioma padrão: Português (Brasil) – pt-BR.
   - App ou jogo: **Jogo**.
   - Gratuito ou pago: **Gratuito**. Essa escolha não pode ser desfeita depois, e o jogo é gratuito com compras opcionais.
   - Marcar as duas declarações: Políticas do programa e leis de exportação dos EUA.
3. **Play App Signing.** Fica ativo por padrão na criação. Confirmar em Testar e lançar > Configuração > **Integridade do app** > Assinatura do app que a chave do app é gerada pelo Google. A chave de upload é a nossa, a do SHA-1 acima.
4. **Ficha da loja principal** (Expandir com a Play > Presença na loja > **Ficha da loja principal**; em inglês, *Grow users > Store presence > Main store listing*). Valores no §2.
   - Depois, adicionar as traduções `en-US` e `es-419` em **Gerenciar traduções > Adicionar suas próprias traduções**. Também no §2.
5. **Configurações da loja** (Presença na loja > Configurações da loja). Valores no §2.4: categoria, tags e contato.
6. **Conteúdo do app** (Monitorar e melhorar > Política e programas > **Conteúdo do app**). Preencher nesta ordem, porque a Play pede os anúncios e o acesso antes do público-alvo:
   1. Política de privacidade: §5.
   2. Anúncios: §4.1.
   3. Acesso ao app: §4.1.
   4. Público-alvo e conteúdo: §4.2.
   5. Classificação do conteúdo (IARC): §4.3.
   6. Segurança dos dados: §4.4.
   7. ID de publicidade: §4.1.
   8. Apps governamentais, recursos financeiros, saúde e notícias: todos **Não**.
7. **Teste interno.** Testar e lançar > Testes > **Teste interno** > Criar nova versão.
   - Subir o AAB e colar as notas da versão do §7.
   - Testadores: lista de e-mails `<<VINICIUS>>` (no mínimo ele mesmo).
   - A conta de organização **não** precisa do teste fechado de 14 dias com 12 testadores. Essa exigência é só da conta pessoal, e foi por isso que ela foi descartada.
8. **Produtos únicos (IAP).** Monetizar com o Google Play > Produtos > **Produtos únicos** > Criar produto único. Os 3 produtos estão no §3.
   - O app precisa estar com o AAB que pede `com.android.vending.BILLING`, e o nosso pede (`verify.sh`, `PERMS`).
9. **Chave RSA da licença.** Monetizar com o Google Play > **Configuração da monetização** > Licenciamento.
   - Copiar a chave pública (base64) e mandar na thread. É chave pública, não é segredo.
   - Com ela, a sessão gera o `GooglePlayTangle.cs` (Unity > Services > In-App Purchasing > Receipt Validation Obfuscator) e abre o PR que liga a `LojaUnity` (`IAP.md` §5.1).
10. **Testadores de licença.** Configurações (do console, não do app) > **Teste de licença**: adicionar os mesmos e-mails do teste interno, para comprar sem cobrança.
11. **Depois do 1º upload:** a conta de serviço para o CI subir os próximos AABs (`RELEASE.md` §3.4) e o `app-ads.txt` no site (§6).

O que **não** se faz no dia, porque cada um pede um aval próprio do Vinicius: produção ou teste aberto, criar a tag `v*` ou a Release do GitHub, e o vídeo no YouTube.

## 2. Ficha da loja

Os textos são os do `ASO.md` §1–3, contados com `len()`: os títulos têm de 24 a 29 caracteres (limite 30), as curtas de 70 a 79 (limite 80) e as completas de 1.845 a 1.928 (limite 4.000). Eles descrevem só o que existe no build. O Passe premium e o Pacote do Mestre ficam fora do texto, porque a Play mostra sozinha o selo "Compras no app".

### 2.1 pt-BR (idioma padrão)

| Campo | Valor |
|---|---|
| Nome do app (≤ 30) | `Forge Street: Tycoon de Forja` |
| Descrição curta (≤ 80) | `Comece com uma bigorna e transforme a oficina na forja mais movimentada da rua.` (variante A; a B vai para o experimento, `ASO.md` §2) |
| Descrição completa (≤ 4.000) | o bloco "completa pt-BR" do `ASO.md` §3, copiado como está |
| Screenshots de celular | `arte/loja/screenshots/pt/` de 1 a 8, na ordem dos nomes |
| Ícone | `arte/marca/icone_loja_512.png` |
| Recurso gráfico | `arte/marca/feature_graphic.png` |
| Vídeo (opcional) | vazio, ou o link do YouTube se o Vinicius subir o `gameplay_30s.mp4` |

### 2.2 en-US

Título `Forge Street: Idle Blacksmith`. Curta: `Start with one anvil and grow your shop into the busiest forge on the street.` Completa: o bloco "completa en" do `ASO.md` §3. Screenshots de `arte/loja/screenshots/en/` (1 a 8). O ícone e o recurso gráfico são os mesmos.

### 2.3 es-419 (Espanhol – América Latina)

Título `Forge Street: Herrero Tycoon`. Curta: `Empieza con un yunque y convierte tu taller en la forja con más clientes.` Completa: o bloco "completa es" do `ASO.md` §3. Screenshots de `arte/loja/screenshots/es/` (1 a 8).
- O texto usa "tú" e vocabulário neutro, então serve também para `es-ES`. Adicionar `es-ES` com o mesmo texto é opcional.

**Revisão da localization-lead:** os 3 textos batem com as strings do build (`View/Loc.cs`), como "cofre/vault/caja fuerte", "Passe/Pass/Pase", "Renome/Renown/Renombre" e "obras-primas/Masterworks/obras maestras". Não aparece termo que o jogo não usa. Fica pendente o `<<CONFIRMAR>>` do `ASO.md` §3 sobre "orders/pedidos", que é o mesmo termo do build.

### 2.4 Configurações da loja

| Campo | Valor |
|---|---|
| App ou jogo / Categoria | Jogo / **Simulação** (`ASO.md` §5, D-ASO-2) |
| Tags (até 5) | escolher na lista do console as mais próximas de: Idle, Magnata, Gerenciamento, Medieval, Estilizado |
| E-mail de contato (obrigatório, público) | `<<VINICIUS: contato@vstack-solutions.com.br?>>` |
| Telefone | vazio (opcional; se preencher, fica público) |
| Site | o domínio da D-PUB-1 (o mesmo do `app-ads.txt`) |
| Inscrever-se em ofertas e notícias | a critério |

## 3. Produtos únicos (IAP)

Para cada produto: **Criar produto único**, preencher ID, nome e descrição, depois as **opções de compra**.
- Cada produto precisa de pelo menos uma opção de compra, do tipo **Comprar**. A ajuda diz que não dá para criar produto sem ela, e o "preço padrão" antigo acabou.
- ID da opção de compra: `padrao`.
- Preço: informar em BRL e deixar a Play converter para os outros países (Definir preços > Editar preços em massa).
- Por fim, **Ativar**.

O **ID do produto não pode ser alterado nem reaproveitado** depois de criado, então copie sem digitar à mão. Os IDs são os do `Compras.cs`, com letras minúsculas, números e `_`, como a Play exige. O nome deve ter até 55 caracteres (25 recomendados) e a descrição até 200, pelos limites da página antiga da ajuda. Todos os textos abaixo estão dentro dos dois.

| ID do produto | Tipo no jogo | Preço base (BRL) | Ativar no dia? |
|---|---|---|---|
| `iap_pass_s1_royal_commission` | não-consumível (o jogo nunca o consome) | **R$ 44,90** (definitivo, Vinicius 2026-10-24) | **Sim**. Só aparece no jogo com a flag `PassePremium` ligada pelo Remote Config |
| `iap_pass_s2_ember_festival` | não-consumível | **R$ 44,90** | Sim, pela mesma flag |
| `iap_pacote_mestre` | não-consumível | R$ 14,90 é **placeholder** (P3 / Wave E) | **Não**: criar como rascunho e não ativar até a D-PUB-2 |

**Textos por idioma.** O nome é igual ao do `Loc.cs` (`iap.<id>.nome`). A descrição lista o conteúdo fixo, o mesmo que o cartão do jogo mostra antes de pagar (V4: nada aleatório).

`iap_pass_s1_royal_commission`
| | Nome | Descrição |
|---|---|---|
| pt-BR | `Passe Comissão Real` | `Libera a trilha premium da temporada Comissão Real do Passe. Você recebe na hora as recompensas dos níveis que já alcançou. Compra única, sem sorteio.` |
| en-US | `Royal Commission Pass` | `Unlocks the premium track of the Royal Commission Pass season. You get the rewards for the levels you've already reached right away. One-time purchase, no random rewards.` |
| es-419 | `Pase Comisión Real` | `Desbloquea la ruta premium de la temporada Comisión Real del Pase. Recibes al instante las recompensas de los niveles que ya alcanzaste. Compra única, sin sorteos.` |

`iap_pass_s2_ember_festival`
| | Nome | Descrição |
|---|---|---|
| pt-BR | `Passe Festival da Brasa` | `Libera a trilha premium da temporada Festival da Brasa do Passe. Você recebe na hora as recompensas dos níveis que já alcançou. Compra única, sem sorteio.` |
| en-US | `Ember Festival Pass` | `Unlocks the premium track of the Ember Festival Pass season. You get the rewards for the levels you've already reached right away. One-time purchase, no random rewards.` |
| es-419 | `Pase Festival de Brasas` | `Desbloquea la ruta premium de la temporada Festival de Brasas del Pase. Recibes al instante las recompensas de los niveles que ya alcanzaste. Compra única, sin sorteos.` |

`iap_pacote_mestre` (rascunho, não ativar)
| | Nome | Descrição |
|---|---|---|
| pt-BR | `Pacote do Mestre` | `Use Velocidade, Lingotes e Trocar encomenda sem assistir ao vídeo, com as mesmas recargas e limites. Compra única, sem sorteio.` |
| en-US | `Master's Pack` | `Use Speed, Ingots and Swap order without watching the video, with the same cooldowns and limits. One-time purchase, no random rewards.` |
| es-419 | `Paquete del Maestro` | `Usa Velocidad, Lingotes y Cambiar pedido sin ver el video, con las mismas recargas y límites. Compra única, sin sorteos.` |

"Recebe na hora as recompensas dos níveis que já alcançou" é o comportamento testado em `LiveOpsTests.PassePremium_AtrasDaFlag_PagaOsAlcancadosUmaVez_SemProgressao`. A trilha premium não dá progressão (V20: sem ouro, Renome, XP nem Blueprint pagos), e a trilha free fecha sem pagar (#126).

**Por que o Pacote do Mestre espera (monetization-compliance):** o conteúdo dele tem duas posses.
- `sem_video` é real: Velocidade, Lingotes e Trocar sem o vídeo.
- `remove_ads` veta o intersticial, que está **desligado** (V10, flag `Interstitial` falsa até o A/B do Limited Release, P3). Hoje o cartão do jogo lista "Sem anúncio entre telas", uma vantagem que o comprador não percebe porque esse anúncio não existe. Isso é o risco de "descrição enganosa" da política de metadados da Play e do CDC.
- A descrição acima só promete o que existe. Mesmo assim, o preço é placeholder e a linha do cartão continua no build. Por isso fica como rascunho até a D-PUB-2.

## 4. Conteúdo do app

### 4.1 Declarações simples

| Declaração | Resposta | Base |
|---|---|---|
| Anúncios: "O app contém anúncios?" | **Sim** | rewarded opcional pelo LevelPlay + Unity Ads; intersticial atrás de flag desligada |
| Acesso ao app | **Todas as funcionalidades estão disponíveis sem acesso especial** | sem login, sem conta |
| ID de publicidade: "O app usa o ID de publicidade?" | **Sim**. Finalidades: Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade | `AD_ID` no manifesto, usado só para 18+ (`PRIVACIDADE.md` §4) |
| Apps governamentais / Recursos financeiros / Saúde / Notícias | Não / Não tem / Não / Não | — |
| Declaração de serviço em primeiro plano (se o console pedir) | o jogo não usa. A permissão `FOREGROUND_SERVICE` vem do WorkManager dos SDKs | `PRIVACIDADE.md` §1.3. Se o console exigir um tipo, responder com o que o relatório do console mostrar e avisar na thread |

### 4.2 Público-alvo e conteúdo

| Pergunta | Resposta | Base |
|---|---|---|
| Faixas etárias do público-alvo | **13–15, 16–17 e 18+** (sem "12 ou menos" nem "Menores de 5 / 6–8 / 9–12") | decisão do Vinicius, 2026-10-24 (13+); `PRIVACIDADE.md` §3.2 |
| O app pode atrair crianças sem querer? | **Não** `<<VINICIUS confirma>>` | igual ao "não direcionado a crianças" respondido no LevelPlay e no Unity Cloud (COPPA). A tela de idade trata quem tem menos de 13 do mesmo jeito (contextual, sem ID de publicidade) |
| Programa Famílias | não participar | consequência da faixa 13+ |

Se o Google discordar e marcar que o app atrai crianças, o jogo já cumpre o essencial para público misto, como diz o `PRIVACIDADE.md` §3.2. Nesse caso, responder na thread antes de mudar qualquer resposta.

### 4.3 Classificação do conteúdo (questionário IARC)

Conteúdo do app > Classificação do conteúdo > **Iniciar**.
- E-mail do IARC: o mesmo de contato.
- Categoria: **Jogo**.
- Depois de responder, **Enviar**.

| Tema | Resposta | Base |
|---|---|---|
| Violência (qualquer tipo), sangue | **Não** | espadas e escudos são mercadoria forjada e vendida. Não há combate |
| Medo / terror | Não | — |
| Sexualidade, nudez | Não | — |
| Linguagem imprópria, humor grosseiro, discriminação | Não | — |
| Drogas, álcool, tabaco | Não | — |
| Jogos de azar simulados ou reais | **Não** | nada aleatório. Os baús de marco dão ouro fixo e as encomendas seguem um rodízio |
| Interação entre usuários / chat / conteúdo gerado | **Não** | single-player |
| Compartilha a localização com outros usuários | Não | — |
| Compras digitais | **Sim** | o AAB tem Billing e os produtos do §3. Isso mudou desde o `PRIVACIDADE.md` §3.1, que ainda dizia "não" |
| Itens aleatórios pagos (loot box) | **Não** | estrutural: `Compras.TipoConteudo` só tem `Posse` e `Moeda`, e o validator recusa sorteio (V4, ECA Digital) |
| Apps de navegação na Web / redes sociais | Não | — |

Esperado: a classificação mais baixa em cada sistema (ClassInd L, PEGI 3, ESRB E), com o aviso "Compras no app". Conferir no certificado que sai.

### 4.4 Segurança dos dados (Data safety)

Conteúdo do app > Segurança dos dados > **Iniciar**. Este é o **formulário vigente**: ele substitui o `PRIVACIDADE.md` §2.2, que é de 12/10, antes do UGS, do `client_error` e do Billing.

Como as respostas foram montadas:
- Vale a **união** do que o jogo e cada SDK declaram, porque declarar a mais não é infração e declarar a menos é.
- **"Obrigatório"** quando o SDK de anúncio coleta de todo jogador depois da tela de idade.
- **"Opcional"** quando só sai de quem tem 18+ e aceitou, e o jogo funciona igual sem isso. É a regra da ajuda da Play: só é opcional se qualquer usuário pode recusar e seguir usando o app.
- A Unity, como operadora do UGS, não conta como compartilhamento. As redes de anúncio contam.
- O que fica só no aparelho (save, configurações, faixa de idade, diário) não se declara.

**Perguntas gerais**
| Pergunta | Resposta |
|---|---|
| O app coleta ou compartilha algum dos tipos de dados exigidos? | **Sim** |
| Todos os dados do usuário coletados são criptografados em trânsito? | **Sim** (LevelPlay, Unity Ads e UGS usam HTTPS) |
| Quais métodos de login o app oferece? / Contas | **O app não permite que os usuários criem uma conta**. O login anônimo do UGS só existe com a flag `CloudSave`, que está desligada (`CLOUD_SAVE.md`) |
| Os usuários podem pedir a exclusão dos dados? | **Sim**: pelo e-mail de contato, que encaminha à Unity. Apagar os dados do app ou desinstalar remove o que fica no aparelho |

**Tipos de dados**
| Categoria > tipo | Coletado | Compartilhado | Obrigatório / Opcional | Finalidades | Quem |
|---|---|---|---|---|---|
| Local > Local aproximado | Sim | Sim | Obrigatório | Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade; Funcionalidade do app | LevelPlay, Unity Ads (pelo IP) |
| Informações pessoais > IDs de usuário | Sim | Sim | Obrigatório | Funcionalidade do app | o Unity Ads declara "identificadores pessoais". Mantido por cautela (D-PRIV-6) |
| Informações financeiras > Histórico de compras | Sim | Sim | Obrigatório | Funcionalidade do app; Publicidade ou marketing; Análise | o jogo guarda recibo e ID de transação no aparelho, a biblioteca da Play processa a compra, o UGS recebe o `transaction` de 18+ com aceite e o Unity Ads declara esse tipo |
| Atividade no app > Interações com o app | Sim | Sim | Obrigatório | Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade | SDKs de anúncio (interação com o anúncio) e UGS Analytics (`fs_evento`, só 18+ com aceite) |
| Atividade no app > Outras ações do usuário | Sim | Sim | Obrigatório | Publicidade ou marketing; Análise | LevelPlay (interação com anúncios) e Unity Ads (tempo de uso, se o *Acquire Optimization* estiver ligado) |
| Informações e desempenho do app > Registros de falhas | Sim | Não | **Opcional** | Análise; Funcionalidade do app | `client_error` vai ao UGS só de 18+ com aceite |
| Informações e desempenho do app > Diagnóstico | Sim | Sim | Obrigatório | Funcionalidade do app; Análise; Prevenção de fraude, segurança e conformidade; Publicidade ou marketing | LevelPlay, Unity Ads e os eventos padrão do UGS |
| Dispositivo ou outros IDs | Sim | Sim | Obrigatório | Publicidade ou marketing; Análise; Funcionalidade do app; Prevenção de fraude, segurança e conformidade | ID de publicidade **só de 18+**, IDs dos SDKs e o **ID de instalação** do UGS (Remote Config para todos depois da tela de idade) |
| Local exato; nome, e-mail, endereço, telefone e outros dados pessoais; dados de pagamento (cartão); saúde; mensagens; fotos, vídeos, áudio e arquivos; agenda; contatos; histórico de pesquisa; apps instalados; navegação na Web; outros dados de desempenho | Não | — | — | — | — |

Perguntas por tipo:
- "Os dados são processados de forma temporária?" **Não** em todos. Uso para publicidade não pode ser efêmero.
- Para "Registros de falhas", a escolha é "Os usuários podem escolher se esses dados são coletados" = Opcional.

**Quando refazer** (`PRIVACIDADE.md` §2.5):
- antes de ligar a flag `CloudSave` (entra o login anônimo e o save na nuvem; `CLOUD_SAVE.md` tem o rascunho);
- rede de anúncio nova;
- permissão nova no `release-check`;
- validação de recibo em servidor.

Antes de enviar, rode `client/tools/verify.sh release-check <o AAB>`. Se aparecer `AVISO: permissão fora da lista aprovada`, este formulário **não** vale para aquele AAB.

## 5. Política de privacidade

- Textos atualizados neste PR: [`politica/privacidade.pt-BR.md`](politica/privacidade.pt-BR.md), [`.en.md`](politica/privacidade.en.md) e [`.es.md`](politica/privacidade.es.md). Eles cobrem UGS Analytics, Remote Config, relatório de erros e compras, e batem com o §4.4.
- A **página pronta para hospedar** é [`politica/index.html`](politica/index.html): um arquivo só, com as 3 línguas, sem script externo, legível no celular. É só subir e usar a URL.
- A Play exige uma URL pública em HTTPS, sem login, que não seja PDF. Exige também **o link dentro do app**: "You must link to a privacy policy on your store listing and within your app" (ajuda "Prepare your app for review"). **O jogo ainda não tem esse link** (D-PUB-3).
- Antes de publicar: preencher os `<<CONFIRMAR>>` com os dados do D-U-N-S (razão social, CNPJ, endereço) e o e-mail; depois, a revisão jurídica (D-PRIV-5). A página tem um aviso de rascunho no topo para não ir ao ar sem isso.

## 6. `app-ads.txt`

O arquivo fica na raiz do domínio do campo "Site" (D-PUB-1). O rascunho está no `PRIVACIDADE.md` §5. Hoje, as linhas saem:
- do painel do LevelPlay (app `28bbd582d`);
- do Unity Ads da org empresarial `4674267143380` (Game ID `800410373`).

As duas são copiadas do painel, nunca escritas à mão. A Play lê o arquivo pelo site da ficha, então ele só faz efeito depois que a ficha tiver o site.

## 7. Notas da versão (1º teste interno)

```text
<pt-BR>
Primeira versão de teste do Forge Street: os 6 distritos, encomendas, contratos, obras-primas, metas, eventos e o Passe.
</pt-BR>
<en-US>
First test build of Forge Street: all 6 districts, orders, contracts, Masterworks, goals, events and the Pass.
</en-US>
<es-419>
Primera versión de prueba de Forge Street: los 6 distritos, pedidos, contratos, obras maestras, metas, eventos y el Pase.
</es-419>
```

## 8. Checklist curto do Vinicius (o dia do D-U-N-S)

1. [ ] Perfil para pagamentos com o D-U-N-S e os dados da empresa (§1 passo 1).
2. [ ] Decidir o site/domínio da política e do `app-ads.txt` e mandar na thread (D-PUB-1). A sessão sobe a página e abre o PR do link no jogo.
3. [ ] Criar o app "Forge Street: Tycoon de Forja", Jogo, Gratuito (§1 passo 2).
4. [ ] Colar a ficha pt/en/es e as configurações (§2).
5. [ ] Conteúdo do app (§4), na ordem do passo 6.
6. [ ] Subir o AAB no teste interno (§1 passo 7).
7. [ ] Criar os 2 Passes ativos e o Pacote do Mestre em rascunho (§3).
8. [ ] Antes de a política ir ao ar: fechar a D-PRIV-2 (Topics) e a D-PRIV-8 (`submitAnalytics`), §9. São PRs pequenos que a sessão faz com o seu ok.
9. [ ] Mandar na thread a chave RSA pública (§1 passo 9) e adicionar os testadores de licença.

## 9. Decisões abertas

| # | Decisão | Recomendação | Bloqueia |
|---|---|---|---|
| D-PUB-1 | Onde hospedar a política e o `app-ads.txt` (o "Site" da ficha) | `https://vstack-solutions.com.br/forgestreet/privacidade` com o `index.html` deste PR, e o `app-ads.txt` na raiz do mesmo domínio. Não usar GitHub Pages: o repositório é privado e torná-lo público é proibido pela regra do estúdio. A nuvem não conseguiu abrir o site (proxy 403), então não foi verificado se ele existe | URL da política, link no jogo, `app-ads.txt` |
| D-PUB-2 | Pacote do Mestre: preço e conteúdo | não ativar no lançamento. Decidir junto com a P3 (A/B do intersticial). Se for ativar antes, tirar `remove_ads` do conteúdo ou a linha "Sem anúncio entre telas" do cartão | ativar `iap_pacote_mestre` |
| D-PUB-3 | Link da política dentro do jogo | botão "Política de privacidade" em Configurações > Privacidade (`Application.OpenURL`), num PR pequeno assim que a D-PUB-1 der a URL. Não sobe a `Privacidade.Versao` (`PRIVACIDADE.md` §6) | aprovação da revisão da Play |
| D-PUB-4 | Vídeo no YouTube (não listado) | opcional. A ficha vai ao ar sem ele | — |
| D-PRIV-2 | Tirar `ACCESS_ADSERVICES_TOPICS` (segmentação por interesse) do manifesto | tirar, com `tools:node="remove"` (`PRIVACIDADE.md` §4). Enquanto estiver lá, a política não pode garantir "sem personalização para menores": as frases têm `<<CONFIRMAR: D-PRIV-2>>` | página pública da política; envio do Data safety |
| D-PRIV-8 | `submitAnalytics: 1` (estatísticas de hardware do motor Unity, de todos) | desligar no `Setup.cs` ou medir com captura de rede | página pública da política; envio do Data safety |
| D-PRIV-5 | Revisão jurídica da política | antes do teste aberto ou da produção. O teste interno não tem página pública | página pública |

## 10. Ata do TIME DE JOGOS (plano B)

O time não estava instalado no repo nesta sessão. Os papéis foram simulados pela sessão a partir dos `.md` de `Vvs2705/time-jogos`.

**Triagem (producer):** SEGUIR COM AJUSTES. Pelos 7 eixos:
- Utilidade: alta. Sem o kit, o dia do D-U-N-S vira horas de leitura de 5 docs.
- Funcionalidade: os docs que existiam dariam respostas **erradas** em 3 pontos:
  - o IARC dizia "sem compras", e o AAB tem Billing;
  - o Data safety não tinha UGS, `client_error` nem histórico de compras do jogo;
  - a política dizia que "nada sai do aparelho" fora o anúncio, e o UGS Analytics manda eventos.
- Viabilidade: só docs, sem custo e sem login.
- Necessidade: é agora, a 4 dias do número.
- Ajustes: atualizar no lugar e não duplicar. `ASO.md` e `IAP.md` continuam sendo a fonte.
- Melhorias: uma página HTML pronta para hospedar e os textos dos produtos nos 3 idiomas.

**release-manager:** ordem do console conferida na ajuda oficial (§11). No teste interno não há exigência de 14 dias para conta de organização. Produção fica fora do dia.

**monetization-compliance:**
- Passe a R$ 44,90 OK: conteúdo fixo e visível, sem progressão paga, trilha free fecha sem pagar.
- Pacote do Mestre **vetado para ativação** até a D-PUB-2, pelo `remove_ads` sem efeito visível.
- Loot box ausente por estrutura.

**localization-lead:** títulos e curtas dentro dos limites e com os termos do build. Os nomes dos produtos são os do `Loc.cs`.

**Conselho do PR:** o veredito vai no PR e no relatório da thread.

## 11. Fontes (ajuda da Play Console, lidas em 2026-11-21)

- Visão geral dos produtos únicos e das opções de compra: <https://support.google.com/googleplay/android-developer/answer/16430488>
- Criar produto no app (página antiga: IDs, limites de nome e descrição, permissão BILLING): <https://support.google.com/googleplay/android-developer/answer/1153481>
- Preparar o app para revisão (Conteúdo do app, política dentro do app): <https://support.google.com/googleplay/android-developer/answer/9859455>
- Seção Segurança dos dados (opcional, efêmero, no aparelho): <https://support.google.com/googleplay/android-developer/answer/10787469>
- Classificação do conteúdo (IARC): <https://support.google.com/googleplay/android-developer/answer/9859655>
