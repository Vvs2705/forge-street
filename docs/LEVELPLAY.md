# LevelPlay: anúncio premiado (rewarded)

Branch `feat/levelplay`, de `feat/v0.5-carga-balcao-arte` (59be56f). A integração do SDK está pronta e a API está exposta em `client/Assets/_FS/Scripts/View/Ads.cs`. **Nenhum botão do jogo chama o anúncio ainda.** O coordenador liga o VIP e a Velocidade quando o núcleo dessas recompensas existir.

A conta LevelPlay ainda espera o Vinicius responder 2 e-mails de aprovação. Até lá o anúncio real pode não carregar. Por isso o build de desenvolvimento mostra um **anúncio simulado** sempre que não há anúncio real pronto.

## 1. Versões instaladas

| Peça | Versão | Onde está |
|---|---|---|
| Ads Mediation (UPM `com.unity.services.levelplay`) | **9.5.1** (20/08/2026, a mais alta do registro) | `Packages/manifest.json` + `packages-lock.json` |
| `com.unity.services.core` (dependência) | 1.18.0 (o pacote pede ≥ 1.15.1) | lock |
| `com.unity.nuget.newtonsoft-json` (dependência do core) | 3.2.2 | lock |
| SDK nativo Android `com.unity3d.ads-mediation:mediation-sdk` | 9.6.1 | `Assets/LevelPlay/Editor/IronSourceSDKDependencies.xml` |
| `com.google.android.gms:play-services-ads-identifier` | 18.1.0 | idem (é ele que traz a permissão `AD_ID`) |
| Adaptador Unity Ads (`unityads-adapter`) + `com.unity3d.ads:unity-ads` | 5.14.0 + 4.21.0 | `Assets/LevelPlay/Editor/ISUnityAdsAdapterDependencies.xml` |
| Mobile Dependency Resolver (fork do EDM4U) | 1.2.185 | `Assets/MobileDependencyResolver/` |

- No registro da Unity, a tag `latest` aponta para a 9.3.2. É um patch da linha 9.3 publicado em 25/09, depois da 9.5.1. A versão estável mais alta é a 9.5.1, e foi ela que entrou.
- O SDK nativo (9.6.1) e o adaptador do Unity Ads foram escolhidos pelo **instalador do próprio pacote** (`SdkInstaller`: "último SDK compatível" com a faixa `[9.5.0, 10.0[` e adaptador Unity Ads automático). Para trocar, use **Ads Mediation > Network Manager** no editor.
- Só o Unity Ads está instalado como rede. Outras redes entram pelo Network Manager. Cada rede nova acrescenta um XML em `Assets/LevelPlay/Editor/` e linhas no `mainTemplate.gradle`.

### Dependências Android (MDR/Gradle)

O resolvedor (`PlayServicesResolver.ResolveSync(force)`) ligou os 3 templates customizados. As dependências entram pelo Gradle no build, sem AAR copiado para o projeto:

- `Assets/Plugins/Android/mainTemplate.gradle`: as 4 linhas `implementation` entre `// Android Resolver Dependencies Start/End`.
- `Assets/Plugins/Android/settingsTemplate.gradle`: repositório `https://maven.google.com/` + `mavenLocal()`.
- `Assets/Plugins/Android/gradleTemplate.properties`: `android.useAndroidX=true` e `android.enableJetifier=true`.
- `ProjectSettings/AndroidResolverDependencies.xml`: o registro do resolvedor.

Na Unity, ter o arquivo em `Assets/Plugins/Android/` já liga o template. Nada mudou no `ProjectSettings.asset`.

### Como foi instalado (em batch, sem abrir o editor)

O fluxo normal da skill abre o editor, clica **Install** no Package Manager e depois **Import** no aviso do MDR. A Unity da pasta principal estava ocupada, então o mesmo caminho rodou em batch no worktree, com um script temporário que **não foi commitado**:

1. `Client.Add("com.unity.services.levelplay@9.5.1")`. O evento `registeredPackages` disparou o instalador do pacote, que baixou os 2 XMLs e o `LevelPlayVersions.json`.
2. Com `UNITY_THISISABUILDMACHINE=1`, o próprio pacote importa o MDR 1.2.185 em modo batch, pelo caminho que ele já tem para máquinas de build.
3. `Google.VersionHandler.UpdateNow()` e depois `PlayServicesResolver.ResolveSync(true)` com o alvo Android.

Logs: `client/Builds/validation_levelplay/install_levelplay.log` e `install_levelplay_2.log`.

**Na primeira vez que alguém abrir o projeto no editor com interface**, o pacote vai:

- acrescentar o símbolo `LEVELPLAY_DEPENDENCIES_INSTALLED` aos Scripting Define Symbols do alvo ativo, o que muda o `ProjectSettings.asset`. Em batch ele pula esse passo de propósito. O símbolo só liga as janelas do Network Manager e de Developer Settings, e o build funciona sem ele.
- talvez perguntar sobre o MDR, que já está instalado. Responda **Import** só se ele não aparecer em `Assets/MobileDependencyResolver`.

Commite essa mudança junto da próxima alteração no projeto.

## 2. Chaves (não são segredo: vão dentro do APK)

| O quê | Valor |
|---|---|
| App Key Android, app "Forge Street" (Not live yet, COPPA "Not directed") | `28953c84d` |
| Ad unit `rewarded_vip`, recompensa "VIP" ×1, placement do jogo `vip` | `7dxdva13pxaovoae` |
| Ad unit `rewarded_velocidade`, recompensa "Velocidade" ×1, placement do jogo `velocidade` | `gple3wvhz7xm3qk7` |

Não foram criados placements no painel. Cada recompensa tem a sua ad unit, e o `ShowAd()` sai sem nome de placement. O capping por placement do painel, portanto, não se aplica.

## 3. API (`namespace FS`, assembly `FS` da View)

```csharp
// A-PLAT-06: o Game fala só com a costura IAds (Scripts/View/Servicos.cs); o Ads.Init escolhe a implementação.
public interface IAds
{
    bool Busy { get; }                                               // do Show até a recompensa/falha (estado Ad)
    bool Ready(string placement);                                    // false = esconder o botão
    bool Loaded(string placement);                                   // anúncio já carregado (o simulado, sempre)
    void Show(string placement, Action onReward, Action onFail);     // chama exatamente UM dos dois
}
public sealed class Ads : MonoBehaviour   // fábrica: AdsLevelPlay (aparelho) ou AdsSimulado (Editor, Windows, -fakeads, fallback dev)
{
    public const string AppKey = "28953c84d";
    public const string Vip = "vip", Velocidade = "velocidade";
    public static IAds Init(Action<string, string, string> log);     // log (evento, placement, detalhe) -> IAnalytics
    public static string Arg(string name);                           // flag da linha de comando ou do intent (intent só em build de desenvolvimento)
    public static bool Resposta(out Faixa f, out bool aceitou);      // E-PLAT-01: resposta gravada da tela de idade (false = perguntar)
    public static void Responder(Faixa f, bool aceitou, bool gravar); // grava (fora do -testsession) e inicia o SDK com as flags (seção 6)
}
```

- `Show` chama **exatamente um** dos dois callbacks, sempre na thread principal. `onReward` vem do `OnAdRewarded` do SDK, ou do fim do simulado. `onFail` cobre: placement desconhecido, outro anúncio na tela, sem anúncio, erro de exibição, e "fechou sem recompensa".
- No SDK, o `OnAdRewarded` pode chegar depois do `OnAdClosed`. Por isso a falha por fechamento espera 2 s antes de valer. Se a recompensa chegar depois desses 2 s, ela não paga e fica no log e no diário como `ad_reward_late`. É um caso raro.
- **Uso esperado pelo coordenador:** `botao.SetActive(ads.Ready(Ads.Vip))` e, no toque, `ads.Show(Ads.Vip, DarVip, () => {})` (com `ads` = o `IAds` do `Ads.Init`). A recompensa é aplicada **só** no `onReward`.
- A pré-carga é feita de propósito: as 2 ad units carregam logo depois do init e recarregam ao fechar. Sem isso, o `Ready` nunca ficaria true a tempo de o botão aparecer. Uma falha de carga tenta de novo em 30 s, 60 s, 120 s… até o teto de 300 s.
- Se o init falhar, ele tenta de novo em 10 s, 20 s… até o teto de 300 s.

### Quando o anúncio é real e quando é simulado

| Onde | Anúncio real pronto | Sem anúncio real |
|---|---|---|
| Editor e Windows (qualquer build) | não existe (o SDK não roda) | **simulado** |
| Qualquer build com `-fakeads` | ignorado | **simulado** |
| Android, build de desenvolvimento | **real** | **simulado** (fallback para testar hoje) |
| Android, build de release | **real** | `Ready` = false, `Show` → `onFail` (o botão some) |
| Android, **sem resposta** da tela de idade, ou resposta mudada nesta sessão (seção 6) | o SDK não iniciou / não mostra | `Ready` = false em qualquer build (o botão some) |

O simulado é um painel uGUI preto, com sortingOrder 1000, que bloqueia o toque. Mostra "Anúncio de teste", "Recompensa: VIP x1" e uma contagem de 5 s. Ele pausa o jogo com `Time.timeScale = 0`, como o anúncio real pausa o app, e no fim chama `onReward`.

### Diário (`diario.csv`)

O `Ads.Init` recebe o `log` do `Game`, que grava pelo mesmo `IAnalytics` dos outros eventos. Com `-testsession` nada é gravado, como sempre.

| evento | a | b |
|---|---|---|
| `ad_init` | `ok` / `falha` / `simulado` / `espera` | versão do plugin / código+mensagem / motivo / `consentimento` (SDK esperando a tela de idade) |
| `ad_request` | placement | `real` / `simulado` |
| `ad_shown` | placement | rede que serviu / `simulado` |
| `ad_reward` | placement | `VIP x1 <rede>` / `... simulado` |
| `ad_fail` | placement | `sem_anuncio`, `ocupado`, `fechou_sem_recompensa`, `exibicao <código> <msg>`… |
| `ad_load_fail` | placement | `<código> <msg> retry Ns` |
| `consent_answer` | faixa: `<13` / `13-17` / `18+` | personalizado: `sim` / `nao` (sai do `Game.Responder`, a cada resposta da tela) |

Trecho real, de uma sessão no PC sem `-testsession` (o save e o diário do PC foram restaurados do backup depois): `client/Builds/validation_levelplay/diario_trecho.csv`.

## 4. Como testar

### PC (hoje)

```bash
cd client/Builds
./win/ForgeStreet.exe -testsession -fakeads -adtest vip -shot v.png -shotdelay 2.5        # foto no meio do anúncio
./win/ForgeStreet.exe -testsession -fakeads -adtest velocidade -shot r.png -shotdelay 7   # foto do painel "Recompensa recebida"
```

O `Player.log` (ou o `-logFile`) mostra `ADS ad_init|ad_request|ad_shown|ad_reward ...`. No Windows o simulado sai mesmo sem `-fakeads`.

Tela de idade (E-PLAT-01). O `-testsession` não mostra a tela nem grava a resposta; o `-privacidade` força. O `-shot` exige caminho **absoluto**:

```bash
./win/ForgeStreet.exe -testsession -privacidade -shot "C:/caminho/absoluto/priv_idade.png" -shotdelay 1.5          # passo 1: as 3 faixas
./win/ForgeStreet.exe -testsession -privacidade 18 -shot "C:/caminho/absoluto/priv_consent.png" -shotdelay 1.5     # passo 2: Recusar | Aceitar
./win/ForgeStreet.exe -testsession -settings -shot "C:/caminho/absoluto/config_privacidade.png" -shotdelay 1.5     # botão PRIVACIDADE
```

Sem `-testsession`, a tela aparece na 1ª abertura de verdade. Para ver de novo no PC, use Configurações > PRIVACIDADE ou apague o valor `fs_privacidade` do PlayerPrefs (no registro, em `HKCU\Software\<empresa>\<produto>`).

### Editor

Basta dar Play. O `Ads` está em modo simulado no editor em qualquer alvo, inclusive Android. Os anúncios falsos ("mock ads") do LevelPlay no editor **não** são usados.

### Aparelho (amanhã, POCO, APK de desenvolvimento)

O APK de desenvolvimento sai de `FS.EditorTools.Setup.BuildAndroidDev`. No Android, as flags vão no extra `unity` do intent:

```bash
adb install -r client/Builds/android/ForgeStreet-dev.apk
# Test Suite do LevelPlay (só em build de desenvolvimento):
adb shell am start -S -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity "-adtestsuite"
# anúncio de teste no início (espera até 15 s o real carregar; sem real, mostra o simulado):
adb shell am start -S -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity "-adtest vip"
# forçar o simulado:
adb shell am start -S -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity "-fakeads -adtest velocidade"
adb logcat -s Unity:V IntegrationHelper:V
```

A activity de entrada é `com.unity3d.player.UnityPlayerGameActivity`, segundo o `aapt dump badging` do APK de hoje. Se o Application Entry Point mudar, ela vira `com.unity3d.player.UnityPlayerActivity`.

Em build de desenvolvimento, o `Ads` também:

- liga `LevelPlay.SetAdaptersDebug(true)`;
- chama `LevelPlay.ValidateIntegration()` depois do init. O relatório sai no logcat. Confira se o Unity Ads aparece **VERIFIED**. No fim do relatório sai o **GAID** do aparelho, que é o ID usado para cadastrar o test device (seção 5).

A Test Suite (`is_test_suite` antes do `Init` + `LaunchTestSuite()` no `OnInitSuccess`) **só** liga com a flag `-adtestsuite` e só em build de desenvolvimento. Nada disso vai para o release, que não tem `Debug.isDebugBuild`.

Tela de idade no aparelho: instale limpo (ou **Configurações > Apps > Forge Street > Limpar dados**) e abra sem `-testsession`. Antes da resposta o logcat mostra `ADS ad_init espera consentimento` e nada do LevelPlay. Depois dela, `ADS privacidade <faixa> coppa=… gdpr=… ccpa=… aaid_optout=… familias=…` e, logo em seguida, `ADS ad_init ok`. Para forçar a tela por cima de uma resposta gravada: `-e unity "-privacidade"` (ou `"-privacidade 18"`).

O que conferir no aparelho:

1. o logcat mostra `ADS ad_init ok 9.5.1`;
2. aparecem `ad_load_fail` com o código, ou `Real(...)` vira true;
3. `-adtest vip` mostra o anúncio real, e o diário recebe `ad_shown vip UnityAds` e `ad_reward vip VIP x1 UnityAds`;
4. fechar o anúncio antes do fim gera `ad_fail vip fechou_sem_recompensa`;
5. com o modo avião ligado, o jogo segue, e em build de desenvolvimento cai no simulado.

## 5. Cadastrar o POCO como test device (o Vinicius faz, no painel)

1. Pegue o GAID do POCO de uma destas formas:
   - no **logcat** do `ValidateIntegration`, no fim do relatório (veja a seção 4);
   - no aparelho, em **Configurações > Google > Anúncios** (o nome varia no HyperOS).
   - **Não** toque em "Excluir ID de publicidade": isso zera o ID.
2. No painel LevelPlay: **Unity LevelPlay > Settings > Test devices**. Escolha o app **Forge Street**, clique em **Add test device**, preencha **Device Name** (ex.: "POCO Vinicius"), **Advertising ID** (o GAID) e o OS **Android**, e clique em **Save**.
3. Na mesma página, escolha a rede (**Unity Ads**) e as ad units, e clique em **Test ads** no fim da linha. Assim o POCO recebe anúncio de teste só daquela rede. Pela documentação, a configuração da rede volta ao normal em até 1 hora.
4. Opcional, para a rede ironSource Ads: **ironSource Ads > Setup > Testing** e adicione o mesmo aparelho. O teste começa sozinho em todas as ad units, com uma chave para ligar e desligar por aparelho.

Fonte: [Integration testing (Unity Docs)](https://docs.unity.com/en-us/grow/levelplay/sdk/android/integration-testing) e [Integration helper (Unity Docs)](https://docs.unity.com/en-us/grow/levelplay/sdk/android/integration-helper). Os nomes dos menus podem variar no painel ao vivo.

## 6. Privacidade: tela de idade + consentimento (E-PLAT-01; não é parecer jurídico)

Até a 0.6 o `AdsLevelPlay` fixava COPPA false, GDPR false e CCPA true para todo mundo. Agora o SDK **só inicia depois da resposta** da tela de idade, e as flags saem dessa resposta. As regras estão num lugar só, `View/Privacidade.cs` (puro, testado no `dotnet test`: `PrivacidadeTests`). A tela (`TelaPrivacidade`, no mesmo arquivo) e a gravação (`Ads.Responder`) só consomem essas regras.

### Fluxo

1. **1ª abertura** (sem resposta, ou resposta com versão de texto antiga): o `AdsLevelPlay` **não toca no LevelPlay** (nem os eventos, nem o `SetAdaptersDebug`), loga `ad_init espera consentimento`, e o `Ready` é false (os botões de anúncio somem, inclusive o simulado do dev). O jogo funciona, e a oficina fica parada enquanto a tela está aberta (o `-bot` continua jogando por baixo).
2. **Passo 1, "Qual é a sua idade?"**: 3 botões iguais, um embaixo do outro, na ordem da idade ("Menos de 13", "13 a 17", "18 ou mais"), nenhum pré-marcado. Não tem como fechar sem responder (o voltar do Android não faz nada aqui).
3. **"Menos de 13" e "13 a 17"** fecham a tela: nada é perguntado sobre personalização, porque a resposta é sempre não.
4. **"18 ou mais"** vai ao passo 2, **"Anúncios personalizados?"**: Recusar e Aceitar lado a lado, com o mesmo tamanho, a mesma cor (face `HudEdge`, texto `HudFace`) e a mesma fonte. VOLTAR (ou o voltar do Android) volta para a idade.
5. A resposta vai para o PlayerPrefs (`fs_privacidade` = `versão|faixa|aceite`, ex.: `1|3|0`) e para o `Ads.Responder`, que aplica as flags e chama o `LevelPlay.Init` na mesma sessão. O diário recebe o `consent_answer`.
6. **Rever ou revogar:** Configurações > **PRIVACIDADE** abre a mesma tela com "Resposta atual: …" no rodapé, e CANCELAR fecha sem mudar nada.
   - **Resposta diferente com o SDK já iniciado:** o GDPR e o CCPA são reaplicados na hora (a API aceita depois do Init). Mesmo assim, o anúncio já carregado veio com a resposta antiga, e COPPA, ID de publicidade e Famílias só valem antes do Init. Por isso, **nenhum anúncio é mostrado até o jogo ser reaberto** (`Ready` e `Loaded` = false, e o logcat diz `ADS privacidade mudou ... sem anuncio ate reabrir`).
   - **Na próxima abertura**, tudo sai da resposta nova. **Revogar exige reabrir o jogo**, e a tela avisa isso: "A mudança vale por completo quando você abrir o jogo de novo".
7. **Versão do texto:** `Privacidade.Versao` (hoje 1). Uma resposta gravada com outra versão não vale, e a tela pergunta de novo. O `PrivacidadeTests.Versao_TravaOTexto` trava o par (versão, hash FNV-1a do texto): se alguém mudar o texto sem subir a versão, o teste falha.
8. **Editor, Windows e `-fakeads`:** a tela aparece igual e grava igual, mas o anúncio é o simulado, que nunca sai do aparelho. `-testsession` não mostra a tela nem grava a resposta; `-privacidade [18]` força a tela (ver seção 4).

### O que vai ao LevelPlay (antes do `LevelPlay.Init`, nesta ordem)

| Faixa | Aceitou? | `SetMetaData("is_deviceid_optout","true")` | `SetMetaData("Google_Family_Self_Certified_SDKS","true")` | `LevelPlayPrivacySettings.SetCOPPA` | `SetGDPRConsent` | `SetCCPA` (do_not_sell) | Anúncio |
|---|---|---|---|---|---|---|---|
| sem resposta | — | — | — | — | — | — | **SDK não inicia** |
| Menos de 13 | (não perguntado) | **sim** | **sim** | **true** | false | **true** | não personalizado, child-directed, sem AAID |
| 13 a 17 | (não perguntado) | **sim** | não | false | false | **true** | não personalizado, sem AAID |
| 18 ou mais | Recusar | não | não | false | false | **true** | não personalizado |
| 18 ou mais | Aceitar | não | não | false | **true** | false | personalizado |

- Regra única (`Privacidade.Flags`):
  - personalizado só com 18+ **e** aceite;
  - do_not_sell = não personalizado;
  - COPPA e Famílias só para menor de 13;
  - sem ID de publicidade (AAID) para todo menor de 18.

  Um "aceite" gravado para menor nunca conta, e o `Gravar` salva 0.
- **13 a 17 sem AAID:** é a nossa leitura do "sem perfilamento de adolescente para publicidade" do ECA Digital (Lei 15.211/2025). Sem o AAID, a rede não liga o adolescente a um perfil de anúncio. Isso custa eCPM nessa faixa. Para voltar a mandar o AAID aos 13 a 17, basta trocar `f != Faixa.Adulto` por `crianca` no `SemId` do `Privacidade.Flags` e atualizar a tabela do teste. **É decisão do Vinicius e do jurídico.**
- **Fontes (doc oficial Unity LevelPlay, "Regulation advanced settings", SDK Android 9.4+/9.5+):**
  - `LevelPlayPrivacySettings.SetCOPPA(true)` "antes de inicializar o SDK" (9.4.0+);
  - `SetCCPA` (9.4.0+);
  - `SetGDPRConsent` (9.5.0+; substitui o `SetConsent`);
  - para apps de público misto, para crianças **ou usuários de idade desconhecida**: `setMetaData("is_deviceid_optout","true")` + `setCOPPA(true)` + `setMetaData("Google_Family_Self_Certified_SDKS","true")` antes do Init.

  Link: <https://docs.unity.com/en-us/grow/levelplay/sdk/android/regulation-advanced-settings>. Assinaturas conferidas no pacote 9.5.1 (`Runtime/Api/LevelPlayPrivacySettings.cs` e `LevelPlay.SetMetaData`).
- **Quanto à escolha do público-alvo na Play Console:** a tela funciona para qualquer escolha. Com "13+" (não direcionado a crianças), quem disser "menos de 13" recebe o tratamento COPPA em vez de ser barrado. Com público misto que inclui crianças, o caminho já é o que a doc pede para o público misto. O painel do LevelPlay continua "Not directed", e o `SetCOPPA(true)` por usuário vale por cima. Se a Play Console marcar o app como **só para crianças**, o manifesto precisa **remover** a permissão `AD_ID` (`tools:node="remove"`): não foi feito, porque depende da decisão.
- Continua igual: sem `userId` no `Init`, sem `SetSegment` e sem ILRD encaminhado. A faixa de idade **não** vai ao SDK; vão só as flags acima.

### Falta para a loja (não feito aqui: tickets G-PLAT-02 e G-PROD-04)

Proposta das respostas (Data Safety, IARC, público-alvo, `ACCESS_ADSERVICES_*`, `app-ads.txt`) e decisões pendentes: [`PRIVACIDADE.md`](PRIVACIDADE.md); rascunho da política em [`politica/`](politica/).

1. **Política de privacidade com URL pública.** É obrigatória com SDK de anúncio e com a permissão `AD_ID`. Deve citar o LevelPlay e o Unity Ads, o ID de publicidade, a tela de idade e como revogar (Configurações > Privacidade), e também ficar acessível de dentro do jogo. Hoje o jogo não tem link: entra junto com a URL.
2. **Formulário Data safety** a partir do manifesto mesclado. Declarar "Device or other IDs" (ID de publicidade), coleta e compartilhamento pelo SDK de anúncios, as permissões `ACCESS_ADSERVICES_*` e o que é opcional.
3. **Classificação IARC** e **público-alvo e conteúdo** na Play Console. Isso decide se o app entra no programa Famílias. Se entrar: só redes autocertificadas, revisar o `AD_ID` e a declaração de anúncios.
4. **"Contém anúncios" = sim** e **app-ads.txt** no domínio do desenvolvedor.
5. **Base legal LGPD** registrada na política, por exemplo consentimento para personalização e legítimo interesse para o anúncio não personalizado. A revisão jurídica do texto da tela muda o `Privacidade.Versao`, o que faz a pergunta voltar para todo mundo.
6. **Região (opcional):** hoje GDPR e CCPA saem da resposta para todos, em qualquer país. Aplicar só onde valem recuperaria eCPM, mas exige detecção de região.

## 7. Tamanho do APK (Android dev, ARM64, IL2CPP)

| Build | Bytes | MiB |
|---|---|---|
| Antes (59be56f, sem SDK) | 47.514.214 | 45,31 |
| Depois (LevelPlay 9.5.1 + SDK 9.6.1 + Unity Ads 4.21.0) | 52.773.667 | 50,33 |
| **Diferença** | **+5.259.453 (+11,1%)** | **+5,02** |

A maior parte do ganho está no código Java/Kotlin: os `classes*.dex` passam de 4 para 5 arquivos e somam **+4,06 MB comprimidos** (3.086.530 → 7.143.700 bytes). O resto vem de recursos e de outros arquivos: o APK tem 99 arquivos a mais.
Logs: `client/Builds/validation_levelplay/build_android_antes.log` e `build_android_depois.log` (não versionados: `*.log` está no `.gitignore`).

**Permissões que o merge de manifest trouxe** (`aapt dump badging`). Antes eram só `INTERNET` e a do receiver dinâmico:

- `ACCESS_NETWORK_STATE`
- `com.google.android.gms.permission.AD_ID`
- `ACCESS_ADSERVICES_TOPICS` e `ACCESS_ADSERVICES_ATTRIBUTION` (Privacy Sandbox)
- `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED` e `FOREGROUND_SERVICE` (do WorkManager que vem com os SDKs)

Todas entram no formulário Data safety e na revisão da Play. Para tirar alguma, por exemplo os Topics do Privacy Sandbox, é preciso um manifest com `tools:node="remove"`. **Não foi feito:** é decisão do Vinicius.

**Lição do worktree:** a `Library` copiada da pasta principal leva o estado incremental do Gradle com caminhos absolutos da pasta original. O 1º build Android falhou com `DexingNoClasspathTransform ... is located outside the root directory`. A correção é apagar `client/Library/Bee/Android/Prj/IL2CPP/Gradle` depois do robocopy. A Unity gera essa pasta de novo.

## 8. Pendências e riscos

- **Aprovação da conta**, que depende dos 2 e-mails do Vinicius. Até lá, espere `ad_load_fail` no aparelho. O build de desenvolvimento cai no simulado, e o de release esconde o botão.
- O **test device** é cadastrado pelo Vinicius (seção 5).
- **Privacidade:** a tela de idade e o consentimento estão feitos (seção 6). Faltam a política de privacidade com URL, o Data safety, o IARC e a decisão do público-alvo na Play Console (G-PLAT-02, G-PROD-04). Também falta conferir no POCO, pelo logcat, que nada do SDK roda antes da resposta: um `ContentProvider` de rede que se inicie sozinho apareceria aí.
- O app está **"Not live yet"** no painel. Na publicação, o app precisa estar ligado à loja (package `br.com.vstack.forgestreet`). O AppId ainda é provisório e "não muda nunca" depois de publicado (veja o `Setup.cs`).
- **Ligar os botões:** a API está pronta, mas ninguém chama o `Ads.Show` além do `-adtest`. O núcleo do VIP e da Velocidade está na `feat/v0.5-carga-balcao-arte` (93f5273), fora desta branch. O coordenador integra as duas e liga os botões.
- **Rede única (Unity Ads):** com uma rede só, o fill e o eCPM dependem só dela. Outras redes entram pelo Network Manager depois que houver tráfego.
- **Jetifier ligado** pelo resolvedor: deixa o build Gradle um pouco mais lento. Pode ser desligado se nenhuma dependência usar support library antiga.
- **Antes do release:** remover `-adtestsuite`/`ValidateIntegration` não é necessário, porque os dois só rodam com `Debug.isDebugBuild`. Mas o checklist de produção da skill pede: Test Suite OK no aparelho, testar com o modo avião e testar em mais de um aparelho.
