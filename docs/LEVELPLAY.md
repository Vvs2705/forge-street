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
public sealed class Ads : MonoBehaviour
{
    public const string AppKey = "28953c84d";
    public const string Vip = "vip", Velocidade = "velocidade";
    public static event Action<string, string, string> Logged;      // (evento, placement, detalhe) -> diário
    public static void Init();                                       // idempotente; Game.Awake chama
    public static bool Live { get; }                                 // SDK real ativo (aparelho, sem -fakeads)
    public static bool Ready(string placement);                      // false = esconder o botão
    public static bool Real(string placement);                       // há anúncio REAL carregado (diagnóstico)
    public static void Show(string placement, Action onReward, Action onFail);
    public static string Arg(string name);                           // flag da linha de comando ou do intent Android
}
```

- `Show` chama **exatamente um** dos dois callbacks, sempre na thread principal. `onReward` vem do `OnAdRewarded` do SDK, ou do fim do simulado. `onFail` cobre: placement desconhecido, outro anúncio na tela, sem anúncio, erro de exibição, e "fechou sem recompensa".
- No SDK, o `OnAdRewarded` pode chegar depois do `OnAdClosed`. Por isso a falha por fechamento espera 2 s antes de valer. Se a recompensa chegar depois desses 2 s, ela é ignorada e fica só no log (`ADS ...`). É um caso raro.
- **Uso esperado pelo coordenador:** `botao.SetActive(Ads.Ready(Ads.Vip))` e, no toque, `Ads.Show(Ads.Vip, DarVip, () => {})`. A recompensa é aplicada **só** no `onReward`.
- A pré-carga é feita de propósito: as 2 ad units carregam logo depois do init e recarregam ao fechar. Sem isso, o `Ready` nunca ficaria true a tempo de o botão aparecer. Uma falha de carga tenta de novo em 30 s, 60 s, 120 s… até o teto de 300 s.
- Se o init falhar, ele tenta de novo em 10 s, 20 s… até o teto de 300 s.

### Quando o anúncio é real e quando é simulado

| Onde | Anúncio real pronto | Sem anúncio real |
|---|---|---|
| Editor e Windows (qualquer build) | não existe (o SDK não roda) | **simulado** |
| Qualquer build com `-fakeads` | ignorado | **simulado** |
| Android, build de desenvolvimento | **real** | **simulado** (fallback para testar hoje) |
| Android, build de release | **real** | `Ready` = false, `Show` → `onFail` (o botão some) |

O simulado é um painel uGUI preto, com sortingOrder 1000, que bloqueia o toque. Mostra "Anúncio de teste", "Recompensa: VIP x1" e uma contagem de 5 s. Ele pausa o jogo com `Time.timeScale = 0`, como o anúncio real pausa o app, e no fim chama `onReward`.

### Diário (`diario.csv`)

O `Game` assina `Ads.Logged` e grava pelo mesmo `Log()` dos outros eventos. Com `-testsession` nada é gravado, como sempre.

| evento | a | b |
|---|---|---|
| `ad_init` | `ok` / `falha` / `simulado` | versão do plugin / código+mensagem / motivo |
| `ad_request` | placement | `real` / `simulado` |
| `ad_shown` | placement | rede que serviu / `simulado` |
| `ad_reward` | placement | `VIP x1 <rede>` / `... simulado` |
| `ad_fail` | placement | `sem_anuncio`, `ocupado`, `fechou_sem_recompensa`, `exibicao <código> <msg>`… |
| `ad_load_fail` | placement | `<código> <msg> retry Ns` |

Trecho real, de uma sessão no PC sem `-testsession` (o save e o diário do PC foram restaurados do backup depois): `client/Builds/validation_levelplay/diario_trecho.csv`.

## 4. Como testar

### PC (hoje)

```bash
cd client/Builds
./win/ForgeStreet.exe -testsession -fakeads -adtest vip -shot v.png -shotdelay 2.5        # foto no meio do anúncio
./win/ForgeStreet.exe -testsession -fakeads -adtest velocidade -shot r.png -shotdelay 7   # foto do painel "Recompensa recebida"
```

O `Player.log` (ou o `-logFile`) mostra `ADS ad_init|ad_request|ad_shown|ad_reward ...`. No Windows o simulado sai mesmo sem `-fakeads`.

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

## 6. Privacidade (configuração conservadora; não é parecer jurídico)

Configurado no código, **antes** do `LevelPlay.Init` (`Ads.StartSdk`):

| Chamada | Valor | Por quê |
|---|---|---|
| `LevelPlayPrivacySettings.SetCOPPA` | `false` | Bate com o painel: "Not directed" (o jogo não é para menores de 13 anos). |
| `LevelPlayPrivacySettings.SetGDPRConsent` | `false` | Ainda não existe tela de consentimento. Sem consentimento, as redes servem anúncio **não personalizado** onde o GDPR vale. |
| `LevelPlayPrivacySettings.SetCCPA` | `true` | "Não vender/compartilhar" dados pessoais, o mais restrito. |

- Não passamos `userId` no `Init`, não há segmentação (`SetSegment`) e não há ILRD encaminhado para nenhuma plataforma. Nenhum dado do jogo sai para o SDK.
- O `play-services-ads-identifier` 18.1.0 traz a permissão `com.google.android.gms.permission.AD_ID` pelo merge de manifest. É o que a skill exige para API 33+, e o projeto usa a API mais nova instalada. Ver na seção 7 se ela aparece no APK.
- **Efeito na receita:** `GDPRConsent=false` e `CCPA=true` valendo para **todos** os jogadores reduzem a personalização e o eCPM. É de propósito, até haver decisão.

**Falta decidir (Vinicius + jurídico):**

1. **LGPD (público principal no Brasil).** Não existe API específica de LGPD no LevelPlay. É preciso definir a base legal para a publicidade, por exemplo consentimento numa tela de primeira abertura ou legítimo interesse com opção de saída. Depois disso, ligar `SetGDPRConsent(true)` só para quem consentir. Uma CMP é opcional.
2. **Região:** aplicar GDPR e CCPA só onde valem, em vez de para todos, o que recupera eCPM.
3. **Google Play:**
   - **Política de privacidade** com URL pública. É obrigatória com SDK de anúncio e com a permissão `AD_ID`.
   - **Formulário Data safety**: declarar "Device or other IDs" (ID de publicidade), coleta pelo SDK de anúncios e compartilhamento com terceiros.
   - Responder **"Contém anúncios = sim"**.
4. **COPPA/Famílias:** se o público-alvo da Play incluir menores de 13 anos, a configuração acima deixa de valer. Seria preciso `SetCOPPA(true)` e o programa Famílias.

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
- **Consentimento/LGPD** (seção 6).
- O app está **"Not live yet"** no painel. Na publicação, o app precisa estar ligado à loja (package `br.com.vstack.forgestreet`). O AppId ainda é provisório e "não muda nunca" depois de publicado (veja o `Setup.cs`).
- **Ligar os botões:** a API está pronta, mas ninguém chama o `Ads.Show` além do `-adtest`. O núcleo do VIP e da Velocidade está na `feat/v0.5-carga-balcao-arte` (93f5273), fora desta branch. O coordenador integra as duas e liga os botões.
- **Rede única (Unity Ads):** com uma rede só, o fill e o eCPM dependem só dela. Outras redes entram pelo Network Manager depois que houver tráfego.
- **Jetifier ligado** pelo resolvedor: deixa o build Gradle um pouco mais lento. Pode ser desligado se nenhuma dependência usar support library antiga.
- **Antes do release:** remover `-adtestsuite`/`ValidateIntegration` não é necessário, porque os dois só rodam com `Debug.isDebugBuild`. Mas o checklist de produção da skill pede: Test Suite OK no aparelho, testar com o modo avião e testar em mais de um aparelho.
