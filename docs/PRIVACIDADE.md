# Privacidade e loja — Forge Street (G-PROD-04 / G-PLAT-02)

2026-10-12 · Leva 8 do ORQUESTRADOR, raia LOJA/PRIVACIDADE (fichas 48 e 51) · Estado: **PROPOSTO** (nada foi declarado na Play Console).

> **Não é parecer jurídico.** Este documento junta o que o código faz e o que os SDKs declaram, e propõe as respostas da loja. Tudo marcado `<<PREENCHER>>` depende de conta ou cadastro; `<<CONFIRMAR>>` depende do Vinicius ou do jurídico. As decisões estão no §8.

**Evidência usada:** `main` em `0af7d10`; `View/Privacidade.cs`, `View/Servicos.cs`, `View/Game.cs`, `View/SaveStore.cs`, `View/Art.cs` (chaves do PlayerPrefs); lista de permissões do APK 0.6.0 no `release_check` do `client/tools/verify.sh` (`PERMS`); `docs/LEVELPLAY.md` §1, §6 e §7; `client/ProjectSettings/UnityConnectSettings.asset`; tabelas oficiais de Data Safety do LevelPlay e do Unity Ads, consultadas em 2026-10-12 (links no §2.6).

**Portão antes de preencher a Play Console:** rodar `client/tools/verify.sh release-check <o AAB que vai subir>`. Sem `AVISO: permissão fora da lista aprovada` = este inventário vale para aquele AAB. Com aviso, ou com SDK novo/atualizado (LevelPlay, Unity Ads, analytics/crash da decisão P8), revisar o §1 e o §2 antes de enviar. Declaração de loja não se reaproveita de build anterior sem essa conferência.

---

## 1. Inventário de dados

### 1.1 O que o jogo grava (fica no aparelho; nada vai para servidor nosso)

O Forge Street não tem servidor, conta, login nem chat. **Desde 2026-10-23 (P8 = Unity Gaming Services, §1.5)** o jogo manda eventos de analytics (só 18+ que aceitou) e baixa o RemoteConfig, depois da tela de idade; a compra (Unity IAP) só liga com a chave da Play (`docs/IAP.md` §5.1). Revisar o §2 antes do Closed.

| Dado | Onde fica | Quando grava | Conteúdo | Sai do aparelho? |
|---|---|---|---|---|
| Save (`save.txt`, `save.txt.bak`, `.tmp` e a quarentena `save.txt.corrupt-*`) | `persistentDataPath` = `/sdcard/Android/data/br.com.vstack.forgestreet/files/` (pasta do app, apagada na desinstalação) | autosave a cada 5 s e na pausa | progresso: ouro, compras por ID, tempos de jogo, instante do último save, checksum. Sem nome, conta, ID de aparelho ou localização | Não (ver backup do Android no §1.4) |
| Espelho do save (`fs.save`) | PlayerPrefs (`shared_prefs`) | junto do save | o mesmo texto do save | Não |
| Resposta da tela de idade (`fs_privacidade` = `versão\|faixa\|aceite`, ex.: `1\|3\|0`) | PlayerPrefs | na resposta e ao rever em Configurações > Privacidade | **faixa** de idade (não a data de nascimento) e o aceite de anúncio personalizado (só 18+) | Não. A faixa não vai ao SDK: vão só as flags do §1.2 |
| Configurações (`fs_musica`, `fs_efeitos`, `fs_vibra`; `fs_som` legado) | PlayerPrefs | ao mudar | volume e vibração | Não |
| Diário (`diario.csv`) | `persistentDataPath` | **só** em build de desenvolvimento ou com `-playtest`. No release do Android nada é gravado: o release só lê a linha de comando, que o Android não passa | data UTC, sessão aleatória de 8 caracteres por processo, evento, tempo de jogo e 2 parâmetros (versão, modelo do aparelho, SO, faixa de RAM, faixa de fps, faixa de idade respondida…). Sem ID de aparelho, conta ou localização. Teto de 1 MB com rotação | Só pelo cabo, no aparelho de teste (`medir_aparelho.sh`) |

**Data Safety:** nada desta tabela se declara. Dado processado só no aparelho não conta como coleta (Google Play, "Data safety", answer 10787469).

### 1.2 O que o SDK de anúncio coleta (LevelPlay 9.5.1 + Unity Ads)

Peças (`LEVELPLAY.md` §1): Ads Mediation `com.unity.services.levelplay` 9.5.1, SDK nativo `mediation-sdk` 9.6.1, adaptador Unity Ads 5.14.0 + `unity-ads` 4.21.0, `play-services-ads-identifier` 18.1.0. Rede ativa: só a Unity Ads.

- **Quando a tela abre (desde 2026-10-24, GDD "Conflitos" item 1, decidido: movida):** nada antes do 1º toque. Sem resposta (ou com resposta de outra versão do texto), a tela de idade abre **uma vez**, no 1º instante em que um botão de anúncio apareceria: algum placement que o núcleo deixa (`Anuncios.Pode`), o que pela D6 só acontece depois da 1ª melhoria comprada, e só onde o botão é tocável (HUD jogando, ou o "Cofre 2x" no painel do cofre aberto). A regra é a `PedidoIdade` (`View/Privacidade.cs`, pura, `PrivacidadeTests`); o diário registra `consent_ask` (`hud`/`cofre`) quando ela abre e `consent_answer` na resposta. Até a resposta: nenhum botão de anúncio (nem o simulado do dev), nenhum intersticial (veto `consentimento`), SDK parado e **nenhuma rede** (sem analytics remoto, sem busca do RemoteConfig, sem loja real: `Game.Liberar` só roda com resposta, como antes; os eventos esperam na fila da `Telemetria`, até 256, e só vão ao remoto de quem tem 18+ e aceitou). A oficina para enquanto a tela está aberta (o `-bot` segue). Continuam: Configurações > Privacidade (rever, revogar ou responder antes do pedido), `-privacidade [18]` (abre já no Awake, também no `-testsession`) e o `-testsession` sem a tela automática (fotos e smoke mostram os botões do anúncio simulado).
- **Quando o SDK inicia:** só depois da resposta da tela de idade (`Privacidade.Sdk`: sem resposta = `Espera`). Antes dela o `AdsLevelPlay` não toca no LevelPlay (logcat `ADS ad_init espera consentimento`; o `verify.sh android-emulator --migracao` reprova se o SDK iniciar sem resposta). Depois da resposta o SDK inicia para **qualquer** faixa, com estas flags (`Privacidade.Flags`, tabela em `LEVELPLAY.md` §6):

| Faixa | COPPA | Sem ID de publicidade (`is_deviceid_optout`) | Famílias (`Google_Family_Self_Certified_SDKS`) | Personalizado (`SetGDPRConsent`) | Não vender (`SetCCPA`) |
|---|---|---|---|---|---|
| Menos de 13 | sim | sim | sim | não | sim |
| 13 a 17 | não | sim (V21, leitura do ECA Digital) | não | não | sim |
| 18+ que recusou | não | não | não | não | sim |
| 18+ que aceitou | não | não | não | **sim** | não |

- **O que coletam**, pela tabela oficial de cada um (detalhe no §2.2): localização aproximada (do IP), identificadores do aparelho (o ID de publicidade só de quem tem 18+), interações **dentro do anúncio**, diagnóstico do app e, no Unity Ads, "identificadores pessoais" para funcionamento, "histórico de compras" e tempo de uso do app (este só com o *Acquire Optimization* ligado no painel do Unity Ads).
- **O que o jogo não manda:** sem `userId` no `Init`, sem `SetSegment`, sem receita por impressão encaminhada, sem a faixa de idade.

### 1.5 Unity Gaming Services: Analytics e Remote Config (P8, 2026-10-23)

- **Projeto:** "Forge Street" no Unity Cloud, organização `vsouz009`, ambiente `production` (`cloudProjectId` 05962639-e02d-4f7c-8293-0ff2089e5144 no `ProjectSettings.asset`). Pacotes `com.unity.services.analytics` 6.3.0 e `com.unity.remote-config` 4.2.5 (`Ugs.cs`).
- **Quando:** nada inicia antes da resposta da tela de idade (`Game.Liberar`). Fora do Editor e do `-testsession`.
- **Analytics:** só com `Privacidade.Flags(...).Consentimento` = 18+ que aceitou. Liga o `AnalyticsIntent` do `EndUserConsent` e manda o evento `fs_evento` (nome, `a`, `b`, segundos de jogo e o `id@versao` do config; o ID de transação nunca). O SDK acrescenta os eventos padrão dele (sessão, aparelho, `newPlayer`) e um **ID de instalação**. Menor de 18 e quem recusou: nada sai, só o diário local.
- **Remote Config:** baixa para todos depois da tela de idade (é funcionamento do jogo, não analytics), com o **ID de instalação** e o IP. O JSON vai ao cache e vale na próxima abertura, com as travas do núcleo.
- **Data Safety (§2.2, a refazer):** "Atividade no app / interações" e "Identificadores do aparelho ou outros IDs" passam a ser coletados também pelo jogo, finalidade "Análise" (analytics) e "Funcionalidade do app" (config); compartilhamento = não (a Unity é operadora); criptografado em trânsito = sim.

### 1.3 Permissões do manifesto mesclado (lista do `release_check`, APK 0.6.0)

| Permissão | Quem traz | Para quê | Efeito na loja |
|---|---|---|---|
| `INTERNET` | Unity / SDK | rede do anúncio | nenhum por si |
| `ACCESS_NETWORK_STATE` | SDK | saber se há rede antes de carregar anúncio | nenhum |
| `VIBRATE` | o jogo (v0.6b) | vibração por importância | nenhum |
| `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED`, `FOREGROUND_SERVICE` | WorkManager (androidx), trazido pelos SDKs | tarefas em segundo plano dos SDKs | nenhum dado novo. Se a Play Console pedir a declaração de serviço em primeiro plano: o jogo não usa; vem do WorkManager `<<CONFIRMAR no console>>` |
| `com.google.android.gms.permission.AD_ID` | `play-services-ads-identifier` 18.1.0 | ler o ID de publicidade | "Dispositivo ou outros IDs" e a declaração "ID de publicidade" (§2.4) |
| `ACCESS_ADSERVICES_TOPICS` | SDK (Privacy Sandbox) | API Topics: interesses do usuário para anúncio | perfilamento; ver §4 (recomendação: remover) |
| `ACCESS_ADSERVICES_ATTRIBUTION` | SDK (Privacy Sandbox) | medir conversão de anúncio sem ID entre apps | medição; ver §4 |
| `br.com.vstack.forgestreet.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` | androidx core | protege receivers internos não exportados (API 33+) | nenhum |

Não há localização, contatos, armazenamento compartilhado, câmera, microfone, telefone nem `QUERY_ALL_PACKAGES`.

### 1.4 Outros caminhos de saída a conferir

- **Serviços da Unity:** os legados do `UnityConnectSettings.asset` (Analytics antigo, Cloud Diagnostics/Crash Reporting, Performance Reporting, Purchasing antigo e Unity Ads legado) continuam `m_Enabled: 0`; o Analytics e o Remote Config do UGS entram pelos pacotes novos (§1.5), e o `cloudProjectId` agora aponta para o projeto Forge Street. **Pendente:** o `ProjectSettings.asset` tem `submitAnalytics: 1` (estatísticas de hardware do motor). Não achei doc do Unity 6 que diga o que ele envia hoje. Ação: desligar ou medir com captura de rede antes do Closed (decisão D-PRIV-8).
- **Backup do Android:** se o manifesto mesclado tiver `android:allowBackup="true"`, o próprio Android pode copiar o save e as configurações para a conta Google do jogador (serviço do sistema, não do jogo). Conferir no AAB (`bundletool dump manifest`) e citar na política (D-PRIV-9).
- **Apps instalados:** o Unity Ads declara "não". Há relato de app com LevelPlay em que a Play exigiu "sim" por causa de `<queries>` no manifesto. Se o relatório de pré-lançamento acusar, comparar os `<queries>` do manifesto mesclado com a tabela do §2.2.

---

## 2. Data Safety — respostas item a item (Play Console > Conteúdo do app > Segurança dos dados)

### 2.1 Perguntas gerais

| Pergunta | Resposta | Por quê |
|---|---|---|
| O app coleta ou compartilha algum dos tipos de dados exigidos? | **Sim** | pelo SDK de anúncio (§1.2). O jogo em si não coleta |
| Todos os dados coletados são criptografados em trânsito? | **Sim** | LevelPlay e Unity Ads declaram "sim"; o jogo não transmite nada |
| Você oferece uma forma de os usuários pedirem a exclusão dos dados? | **Sim** `<<CONFIRMAR>>` (D-PRIV-7) | o que é do jogo: desinstalar ou "Limpar dados" apaga tudo. O que é do SDK: pedido à Unity pelo canal de privacidade dela, ou por e-mail para nós, que encaminhamos. Os dois SDKs declaram que aceitam pedido de exclusão |
| O app permite criar conta? | **Não** | então não se exige URL de exclusão de conta |
| Revisão de segurança independente (MASA)? | Não | opcional |

### 2.2 Tipos de dados

Regras usadas: o dev responde pelo que o SDK coleta; quando os dois SDKs divergem, vale a **união** (declarar a mais não é infração, declarar a menos é). "Obrigatório" porque o SDK inicia para qualquer resposta da tela de idade: o jogador não consegue usar o app impedindo essa coleta (o que é opcional é a **personalização**, só 18+ com aceite). Processamento efêmero: não, em todos (os SDKs dizem "não").

| Categoria > tipo | Coletado | Compartilhado | Obrigatório? | Finalidades | Origem e nota |
|---|---|---|---|---|---|
| Local > Aproximado | **Sim** | **Sim** | Obrigatório | Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade; Funcionalidade do app | LevelPlay e Unity Ads (derivado do IP) |
| Local > Exato | Não | — | — | — | nenhuma permissão de localização |
| Informações pessoais > IDs de usuário | **Sim** `<<CONFIRMAR>>` | **Sim** | Obrigatório | Funcionalidade do app | o Unity Ads declara "Personal identifiers" para funcionamento. O jogo não tem conta; manter por conservadorismo (D-PRIV-6) |
| Informações pessoais > nome, e-mail, endereço, telefone, raça, crenças, orientação, outras | Não | — | — | — | — |
| Informações financeiras > Histórico de compras | **Sim** `<<CONFIRMAR>>` | **Sim** | Obrigatório | Publicidade ou marketing; Análise | o Unity Ads declara. O jogo não vende nada hoje; manter por conservadorismo e reavaliar com o IAP da Wave E (D-PRIV-6) |
| Informações financeiras > cartão, crédito, outras | Não | — | — | — | — |
| Saúde e condicionamento físico | Não | — | — | — | — |
| Mensagens | Não | — | — | — | sem chat |
| Fotos e vídeos; Arquivos de áudio; Arquivos e documentos; Agenda; Contatos | Não | — | — | — | — |
| Atividade no app > Interações com o app | **Sim** | **Sim** | Obrigatório | Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade | só toques e visualizações **dentro do anúncio**, não no jogo (nota do Unity Ads); o LevelPlay chama de "interação com anúncios" |
| Atividade no app > Outras ações | **Sim** | **Sim** | Obrigatório | Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade | LevelPlay ("other actions: interaction with ads") e Unity Ads (tempo de uso do app, só com *Acquire Optimization* ligado, D-PRIV-11) |
| Atividade no app > Histórico de pesquisa, Apps instalados, Outro conteúdo gerado | Não | — | — | — | ver §1.4 sobre apps instalados |
| Navegação na Web | Não | — | — | — | — |
| Informações e desempenho do app > Registros de falhas | Não | — | — | — | nenhum SDK de crash (Cloud Diagnostics desligado). Muda quando a decisão P8 trouxer crash reporting |
| Informações e desempenho do app > Diagnóstico | **Sim** | **Sim** | Obrigatório | Funcionalidade do app; Análise; Prevenção de fraude, segurança e conformidade; Publicidade ou marketing | o LevelPlay coleta sem compartilhar; o Unity Ads coleta e compartilha: vale a união |
| Informações e desempenho do app > Outros dados de desempenho | Não | — | — | — | — |
| Dispositivo ou outros IDs | **Sim** | **Sim** | Obrigatório | Publicidade ou marketing; Análise; Funcionalidade do app; Prevenção de fraude, segurança e conformidade | ID de publicidade **só de 18+** (menor de 18 = `is_deviceid_optout`), mais IDs do próprio SDK. Continua "sim" mesmo com o opt-out |

### 2.3 O que não se declara (§1.1)

Save, espelho, configurações, faixa de idade e diário: ficam no aparelho e nenhum servidor nosso os recebe.

### 2.4 Outras declarações da página "Conteúdo do app"

| Item | Resposta |
|---|---|
| Política de privacidade | URL pública da política (§6) `<<PREENCHER>>` |
| Anúncios: "O app contém anúncios?" | **Sim** (rewarded hoje; interstitial atrás de flag desligada, V10) |
| ID de publicidade: "O app usa o ID de publicidade?" | **Sim**. Finalidades: Publicidade ou marketing; Análise; Prevenção de fraude, segurança e conformidade |
| Acesso ao app | Tudo acessível sem login ou credencial |
| Classificação do conteúdo | questionário IARC (§3.1) |
| Público-alvo e conteúdo | §3.2 |
| App de notícias, de governo, recursos financeiros, saúde | Não |

### 2.5 Quando refazer

Toda permissão nova no `release-check`; SDK de anúncio atualizado ou rede nova no Network Manager (cada rede tem a sua tabela); provedor de analytics, crash, RemoteConfig ou atribuição (P8); IAP (Wave E: "Histórico de compras" passa a ser também do jogo e o IARC ganha "compras no app"); cloud save; e qualquer mudança na tela de idade.

### 2.6 Fontes (consultadas em 2026-10-12)

- LevelPlay, *Google Data Safety Questionnaire*: <https://docs.unity.com/grow/levelplay/platform/legal-resources/google-data-safety-questionnaire>
- Unity Ads, *Google Play data safety section*: <https://docs.unity.com/en-us/grow/ads/privacy/google-data-safety>
- Unity Diagnostics (não usado aqui, para comparar): <https://docs.unity.com/en-us/cloud/developer-data/google-data-safety>
- Google Play, *Data safety section*: <https://support.google.com/googleplay/android-developer/answer/10787469>
- LevelPlay, *Regulation advanced settings* (flags do §1.2): <https://docs.unity.com/en-us/grow/levelplay/sdk/android/regulation-advanced-settings>

---

## 3. Classificação IARC e público-alvo

### 3.1 Questionário IARC (respostas esperadas)

Categoria: **Jogo**. O certificado sai do questionário. A expectativa é a classificação mais baixa em cada sistema (ClassInd "L", PEGI 3, ESRB "E", USK 0), **sem garantia** `<<CONFIRMAR no certificado>>`.

| Tema | Resposta | Base no jogo |
|---|---|---|
| Violência (realista, fantasiosa, contra personagens) | **Não** | espadas e escudos são mercadoria forjada e vendida; não há combate, ninguém se fere |
| Medo, terror | Não | — |
| Sexualidade, nudez | Não | — |
| Linguagem imprópria, humor grosseiro, discriminação | Não | — |
| Drogas, álcool, tabaco | Não | não há taverna nem bebida |
| Jogos de azar (simulados ou com dinheiro) | **Não** | nada aleatório: VIP por sequência de Weyl, encomendas em rodízio, baús de marco com ouro fixo |
| Compras digitais / itens aleatórios pagos (loot box) | **Não** | sem IAP hoje; loot box nunca (V4, ECA Digital art. 20). Refazer o questionário quando o IAP entrar (Wave E) |
| Interação entre usuários, chat, conteúdo gerado por usuário | **Não** | single-player, sem chat (o art. 21 do ECA Digital não se aplica hoje) |
| Compartilha a localização do usuário com outros usuários | Não | — |
| Anúncios | Sim, na declaração "Contém anúncios" da Play Console (o IARC não classifica anúncio) | rewarded opcional |

**Consequência:** classificação livre significa que o jogo é "de acesso provável" por crianças e adolescentes. O guardrail "nada aleatório pago" (V4) vale para sempre, não só até a Wave E.

### 3.2 Público-alvo declarado na Play Console

**Recomendação (D-PRIV-1): faixas 13–15, 16–17 e 18+**, sem "12 ou menos". Coerente com o GDD §2 (público 13–50+) e com a tela de idade:

- A tela é **neutra**: 3 faixas iguais, nenhuma pré-marcada, sem dica de "precisa ter 13". Quem responde "Menos de 13" não é barrado: recebe o tratamento COPPA (contextual, sem ID de publicidade, só redes do programa Famílias). É o caminho que a doc do LevelPlay pede para público misto e idade desconhecida.
- "Seu app pode atrair crianças sem querer?": resposta honesta do Vinicius `<<CONFIRMAR>>`. Arte estilizada e colorida pode fazer o Google achar que sim. Se isso acontecer, o jogo já cumpre o essencial da política Famílias para público misto (tela de idade neutra, sem ID de publicidade de criança, flag de SDK autocertificado). Falta conferir se o LevelPlay e o Unity Ads estão na lista atual de SDKs autocertificados para Famílias `<<CONFIRMAR>>`.
- Alternativa (não recomendada agora): incluir "12 ou menos". Entra no programa Famílias com todas as exigências: todo SDK de anúncio autocertificado, revisão mais lenta e, se o público for **só** criança, tirar a permissão `AD_ID` do manifesto.
- O painel do LevelPlay continua "Not directed" (COPPA no app). O `SetCOPPA(true)` por usuário vale por cima.

---

## 4. `ACCESS_ADSERVICES_*` e `AD_ID`: o que fazer para menores

O manifesto é um só para todos: permissão não varia por usuário. O que varia por usuário são as flags do §1.2, enviadas antes do `Init`.

| Item | Hoje | Recomendação |
|---|---|---|
| `AD_ID` | no manifesto; menor de 18 vai com `is_deviceid_optout` (o SDK não usa o ID de publicidade) | **Manter** com o público 13+: o adulto que aceita precisa dele. Remover (`tools:node="remove"`) só se o app virar exclusivamente infantil. Verificar no aparelho que o opt-out funciona (pendente, §7) |
| `ACCESS_ADSERVICES_TOPICS` | no manifesto para todos | **Remover** com `tools:node="remove"` (D-PRIV-2). Topics é segmentação por interesse: contraria o "nada personalizado" de menor de 18 e, para adulto, só valeria com aceite. Nada garante que o SDK respeita o COPPA/opt-out na API Topics, e quase nada se perde, porque o anúncio já é contextual para a maioria |
| `ACCESS_ADSERVICES_ATTRIBUTION` | no manifesto para todos | **Manter** (medição de conversão, sem perfil no jogo) ou remover junto por simplicidade (D-PRIV-2) |

**Como remover (ticket para a raia de plataforma; esta raia não toca em `client/Assets`):** um `client/Assets/Plugins/Android/AndroidManifest.xml` customizado com `xmlns:tools` e `<uses-permission android:name="android.permission.ACCESS_ADSERVICES_TOPICS" tools:node="remove"/>`. Também é preciso tirar a permissão do `PERMS` do `verify.sh`: permissão que some não dispara o aviso, mas a lista documenta o aprovado. Conferir com `release-check` e refazer o §1.3 e o Data Safety.

---

## 5. `app-ads.txt` (rascunho)

Fica na raiz do domínio informado no campo "Site" da conta de desenvolvedor da Play Console, em `https://<<PREENCHER: domínio>>/app-ads.txt`. As linhas saem **do painel** (LevelPlay e Unity Ads), copiadas como estão. Nenhum ID abaixo foi inventado: todos estão como `<<PREENCHER>>`.

```text
# app-ads.txt — Forge Street (br.com.vstack.forgestreet) — RASCUNHO, não publicar com marcadores
# Formato IAB: <domínio do sistema de anúncio>, <ID da conta do publisher>, <DIRECT|RESELLER>, <ID da autoridade de certificação (opcional)>
# Fonte das linhas: painel LevelPlay (lista de app-ads.txt das redes ativas) <<CONFIRMAR o caminho do menu no painel>>

# LevelPlay / ironSource Ads (mediação)
<<PREENCHER: linha(s) do painel LevelPlay para a conta da VStack>>

# Unity Ads (única rede ativa hoje)
unity3d.com, <<PREENCHER: ID da organização/publisher no painel Unity Ads>>, DIRECT, <<PREENCHER: ID de certificação que o painel mostrar>>

# Revendedores das redes ativas: colar as linhas RESELLER que o painel LevelPlay listar
<<PREENCHER>>
```

- Cada rede nova no Network Manager acrescenta as linhas dela.
- O domínio precisa bater com o "Site" da ficha de desenvolvedor e do app. As redes leem o arquivo pelo link da loja: domínio errado = inventário "não autorizado" e eCPM menor.
- Não é obrigatório para publicar, mas o DM §149 pede "quando aplicável", e com anúncio é.

---

## 6. Política de privacidade: onde fica e como o jogo liga a ela

- Textos prontos para revisão: [`politica/privacidade.pt-BR.md`](politica/privacidade.pt-BR.md), [`.en.md`](politica/privacidade.en.md), [`.es.md`](politica/privacidade.es.md).
- **URL pública** `<<PREENCHER>>` (D-PRIV-3): página HTML simples no domínio do §5, por exemplo `https://<<domínio>>/forgestreet/privacidade` com as 3 línguas. A Play exige URL ativa, pública, sem login e que não seja PDF.
- **Link dentro do jogo** (pendente, ticket da raia de View): botão "Política de privacidade" em Configurações > Privacidade (`Application.OpenURL`). Um botão novo não muda o `Privacidade.Texto`, então não sobe a `Privacidade.Versao`. Já mudar o texto da tela (revisão jurídica) **sobe** a versão, e a pergunta volta para todo mundo.
- A política entra também na ficha da loja (campo obrigatório) e no painel do LevelPlay, se ele pedir.

---

## 7. Checklist ECA Digital / LGPD-criança deste release

| Item | Estado | Evidência |
|---|---|---|
| Nada aleatório pago (loot box) | OK | sem IAP; baús de marco com ouro fixo (`Defs.cs` `Milestones`); núcleo sem RNG |
| Menor de 18 sem anúncio personalizado | OK | `Privacidade.Flags`, `PrivacidadeTests` |
| Menor de 18 sem ID de publicidade | OK no código / **não verificado no aparelho** | `is_deviceid_optout` antes do `Init`. Falta prova de rede ou do logcat do `IntegrationHelper` no POCO |
| Tela de idade neutra, sem pré-seleção, obrigatória antes do 1º anúncio, revogável | OK | `TelaPrivacidade` (abre no 1º botão de anúncio, `PedidoIdade`); Configurações > Privacidade |
| SDK só depois da resposta | OK no emulador / POCO pendente | `verify.sh android-emulator --migracao` (`ADS ad_init ok` ausente antes da resposta); item da tela de idade no `ROTEIRO_TESTE_POCO.md` |
| Sem consentimento fixo no release | OK | E-PLAT-01 acabou com o hardcode (DM §94) |
| Sem interação entre usuários | OK | single-player |
| Interstitial desligado | OK | flag `Interstitial` desligada (V10) |
| O jogo não coleta dado pessoal | OK | §1.1 |
| Política com URL e link no jogo | **Pendente** | §6 |
| Data Safety, IARC e público-alvo preenchidos | **Pendente** | §2, §3 (depende da conta, P9) |
| Topics removido para não perfilar menor | **Pendente** (D-PRIV-2) | §4 |
| A autodeclaração de faixa basta para o ECA Digital? | **Em aberto** (D-PRIV-10) | o texto oficial da lei não foi conferido nesta leva |

---

## 8. Decisões do Vinicius

| # | Decisão | Recomendação | Bloqueia |
|---|---|---|---|
| D-PRIV-1 | Público-alvo na Play Console | 13–15, 16–17 e 18+, sem "12 ou menos" (§3.2); responder com honestidade se o app pode atrair crianças | ficha da loja, Internal (P9) |
| D-PRIV-2 | Tirar `ACCESS_ADSERVICES_TOPICS` (e `ATTRIBUTION`?) do manifesto | Tirar Topics; manter Attribution (§4) | Data Safety final |
| D-PRIV-3 | Domínio e hospedagem da política e do `app-ads.txt`; "Site" da conta de desenvolvedor | o domínio da VStack `<<CONFIRMAR>>`, página HTML com as 3 línguas | URL da política, §5 |
| D-PRIV-4 | Dados do controlador: razão social, CNPJ, endereço, e-mail (`contato@vstack-solutions.com.br` `<<CONFIRMAR>>`) e encarregado (DPO) ou canal LGPD | o jurídico diz se precisa de encarregado (porte da empresa × dado de adolescente) | política |
| D-PRIV-5 | Revisão jurídica da política e do texto da tela de idade | revisar antes do Closed. Mudar o texto da tela = `Privacidade.Versao` + 1 | publicação |
| D-PRIV-6 | Declarar "IDs de usuário" e "Histórico de compras", que o Unity Ads declara e o jogo não tem | declarar (conservador) e reavaliar com o IAP | Data Safety |
| D-PRIV-7 | "Usuários podem pedir exclusão": sim, com e-mail + canal da Unity | Sim (§2.1) | Data Safety |
| D-PRIV-8 | `submitAnalytics: 1` (estatísticas de hardware da Unity) | desligar no `Setup.cs`, ou medir com captura de rede, antes do Closed | Data Safety |
| D-PRIV-9 | Backup automático do Android (`allowBackup`) | conferir no AAB. Manter ligado (o jogador recupera o save ao trocar de celular) e citar na política | política |
| D-PRIV-10 | ECA Digital: a autodeclaração de faixa basta, ou a lei pede aferição de idade ou supervisão parental para este tipo de app? | consulta jurídica com o texto oficial da Lei 15.211/2025 | Limited Release (Brasil) |
| D-PRIV-11 | *Acquire Optimization* no painel do Unity Ads | deixar desligado (menos dado: sem "tempo de uso") `<<CONFIRMAR o estado atual>>` | Data Safety |
| D-PRIV-12 | GDPR/CCPA por região × para todos | manter para todos (como hoje: mais protetivo, sem detecção de região) | — |

Fora desta raia, mas ligado a ela: **P8** (cada provedor de analytics, crash, RemoteConfig ou atribuição muda o §2) e **P9** (conta do Play Console: sem ela nada do §2 e do §3 é preenchido).
