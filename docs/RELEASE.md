# Release — Forge Street (pipeline e o que falta do Vinicius)

Atualizado: 2026-10-25 · Estado: **chave de upload real gerada e secrets do GitHub configurados (§3.1, §3.2); nada publicado, nenhuma tag criada.** A conta da Play espera o D-U-N-S (previsão 2026-11-25, §3.5). A subida à Play depende dos passos do §3.3 em diante, que são do Vinicius.

## 1. Como uma versão sai

| Passo | Onde | O que faz |
|---|---|---|
| versão | `client/Assets/_FS/Editor/Setup.cs` (`Setup.Version`, SemVer X.Y.Z) | o `versionName`; o `versionCode` sai dela (`Setup.VersionCode`: X·10000 + Y·100 + Z; 0.6.0 = 600, 1.0.0 = 10000), sempre crescente |
| AAB | `Setup.BuildAndroidRelease` | build não-development, IL2CPP arm64, targetSdk 36, assinado pelas variáveis `FS_KEYSTORE`, `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS`, `FS_KEY_PASS`. Sem elas o build **falha** (nunca cai na chave de debug) |
| conferência | `client/tools/verify.sh release-check <aab>` | targetSdk ≥ 36, versionCode coerente e > 1, não-debuggable, só arm64-v8a, 16 KB, módulo base ≤ 200 MB, assinatura que não é a de debug, permissões da lista aprovada |
| no PC | `client/tools/verify.sh release` | quick + o AAB + o release-check, com as 4 variáveis no ambiente do Vinicius |
| no CI | workflow `release` (`.github/workflows/release.yml`) | o mesmo build com a **chave de upload real** pelos secrets `FS_*` (sem eles, uma keystore de teste gerada na hora, que só prova o pipeline) + release-check + o AAB como artifact. Roda à mão (`workflow_dispatch`) e numa tag `v*`. **Não sobe à Play** |
| símbolos | `Setup.BuildAndroidRelease` (só `#if UNITY_ANDROID`) | a tabela de símbolos do IL2CPP (`DebugSymbolLevel.SymbolTable`) **dentro do AAB** (a Play simboliza o crash nativo sozinha, GDD §164 item 17) e num `.symbols.zip` ao lado, que o workflow `release` guarda no artifact |
| loja | Play Console | o 1º AAB vai à mão (a Play exige); os seguintes podem ir pela conta de serviço (§3.3) |

A tag `v*` (ex.: `v0.7.0`) é o gatilho preparado para a release automática; criar a tag e publicar uma Release do GitHub seguem dependendo do aval do Vinicius.

## 2. O que já está pronto (sem segredo)
- AAB assinado por variável de ambiente, versionCode pelo SemVer, targetSdk 36, 16 KB (#18 e seguintes).
- `release-check` portável: roda no PC (ferramentas do Unity) e no Linux do CI (`RC_JAVA`, `RC_JARSIGNER`, `RC_BUNDLETOOL`, `RC_READELF`), com o teto de 200 MB do módulo base.
- Unity no CI com GameCI (EditMode, build Windows, autoplay, build Android de dev: `docs/CI_UNITY.md`).

## 3. O que falta do Vinicius (passo a passo, quando ele decidir)

Nada aqui é pedido agora; é o roteiro para quando chegar a hora da loja.

### 3.1 Chave de upload real (uma vez)
**Feita em 2026-10-25.** O Vinicius rodou o `gerar_chave_upload.ps1`, que fica fora do repositório, em `%USERPROFILE%\.forgestreet\`.
- **Arquivo:** `%USERPROFILE%\.forgestreet\upload.jks`, alias `forgestreet-upload`, RSA 4096, 10000 dias, `CN=Vstack Solutions, O=Vstack Solutions, C=BR`.
- **Certificado (público):**
  - **SHA-1** `75:20:35:E8:BD:07:A5:59:A7:0C:16:4C:FE:9C:36:B3:1D:CD:D6:83` (é o que a Play Console mostra como certificado da chave de upload);
  - **SHA-256** `EA:CB:C5:EA:CE:32:05:EA:52:7D:1C:10:16:58:98:F8:EC:DC:FD:13:E3:73:C0:A9:97:9D:41:EB:11:F6:60:9E`.
- **Senhas:** só no PC (`upload-senhas.txt` ao lado do `.jks`), com backup em outra pasta do PC e uma cópia fora do PC. As 4 variáveis de usuário `FS_*` estão criadas.
- **O keystore é PKCS12** (padrão do keytool do JDK novo). Ele ignora o `-keypass`, então a senha da chave é a **mesma** da keystore: `FS_KEY_PASS` = `FS_KEYSTORE_PASS`.
  - Com senhas diferentes, o Gradle falha no `signReleaseBundle` com "Get Key failed: Given final block not properly padded".
  - O `corrigir_senha_chave.ps1` acertou isso em 2026-10-25.
- **Prova:**
  - o `verify.sh release` no PC gerou o `ForgeStreet-0.6.0.aab` (124,4 MB no módulo base), com todo o release-check OK;
  - o `keytool -printcert -jarfile` no AAB deu o SHA-1 acima.

O passo a passo original, para uma chave nova:

1. No PC, fora do repositório (regra do estúdio: segredo em `%USERPROFILE%\.forgestreet\`):
   ```bash
   keytool -genkeypair -v -keystore "$USERPROFILE/.forgestreet/upload.jks" -alias forgestreet-upload -keyalg RSA -keysize 4096 -validity 10000
   ```
   O `keytool` vem com o JDK do Unity: `.../Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe`.
2. **Backup fora do PC** do `.jks` e das duas senhas (GDD §15: perder a chave de upload obriga a pedir reset ao suporte da Play).
3. Variáveis do usuário no Windows: `FS_KEYSTORE` (caminho do `.jks`), `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS` (`forgestreet-upload`), `FS_KEY_PASS`. Com elas, `client/tools/verify.sh release` gera e confere o AAB no PC.

### 3.2 Secrets do GitHub para o AAB real no CI (opcional)
**Configurados em 2026-10-25:** `FS_KEYSTORE_B64`, `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS` e `FS_KEY_PASS`. O workflow `release` disparado à mão (`gh workflow run release.yml --ref main`) usa a chave real; o resumo do run diz "chave de upload". Nada de tag, Release ou Play.

Em Settings > Secrets and variables > Actions do repositório:
- `FS_KEYSTORE_B64`: o `.jks` em base64 (`base64 -w0 upload.jks`);
- `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS`, `FS_KEY_PASS`.

Com eles, o workflow `release` usa a chave real em vez da de teste (o job detecta os secrets; sem eles, segue com a de teste e avisa).

### 3.3 Play Console (conta e app)
1. Conta de desenvolvedor e perfil de pagamento (P9). **Espera o D-U-N-S, previsão 2026-11-25** (ver §3.5). É a mesma conta de **organização** da V-STACK aberta para o Dungeon Courier: um só cadastro hospeda os dois jogos, então o Forge Street não abre conta própria.
2. Criar o app `br.com.vstack.forgestreet` e ativar o **Play App Signing** (a Play guarda a chave do app; a do §3.1 é só a de upload).
3. Subir o 1º AAB **à mão** numa faixa de teste interno (a Play não aceita o 1º pela API).
4. Produtos do IAP (`IAP.md` §5.1: `iap_pacote_mestre` e os dois do Pass) e a chave RSA da licença no `LojaUnity`.

### 3.4 Conta de serviço para subir pelo CI (depois do 1º upload)
1. Google Cloud: criar uma conta de serviço no projeto ligado à Play e gerar a chave JSON.
2. Play Console > Usuários e permissões: convidar o e-mail da conta de serviço com "Lançar apps em faixas de teste" só para este app.
3. Secret `PLAY_SERVICE_ACCOUNT_JSON` no GitHub. O passo de upload para a faixa interna entra no workflow `release` quando esse secret existir (ainda não implementado: sem ele nada sobe).

### 3.5 D-U-N-S e conta de organização (decisão de 2026-10-24)
O processo foi registrado no Dungeon Courier (`docs/LOJA.md` §1 e `docs/LICOES_PARA_O_ESTUDIO.md` §8 do repo `dungeon-courier`):
- **Conta de organização** ("Uma empresa ou corporação") na Play Console, dona `vinicius@vstack-solutions.com.br` (o dono não muda depois), com verificação em duas etapas. O cadastro já passou de Tipo, Identidade e Nome do desenvolvedor e **parou em "Perfil para pagamentos"**, que pede o D-U-N-S. Os dados da organização são os mesmos do pedido do D-U-N-S.
- **D-U-N-S grátis pela CIAL** (pedido para cadastro Apple/Google, ~30 dias úteis), feito em 2026-10-15, **previsão 2026-11-25**. O plano pago (Basic, ~R$ 1.700/ano, 2 dias úteis) foi descartado.
- **Conta pessoal descartada** pelo Vinicius (2026-10-24): ela exige teste fechado com 12+ testadores por 14 dias antes da produção e deixaria o app fora do nome da empresa. Tudo (Play Console, perfil de pagamentos, produtos) é cadastrado em nome da V-STACK.

**Enquanto o D-U-N-S não sai**, nada do §3.3 anda, e o resto segue: GDD até 90%, AAB pelo workflow `release` com a chave de teste, página da loja e criativos, teste no POCO, e a chave de upload do §3.1 (gerar e guardar com cópia fora do PC). **Quando o número chegar (~2026-11-25):** retomar o cadastro em "Perfil para pagamentos" na mesma conta, criar o app `br.com.vstack.forgestreet` e seguir o §3.3 em ordem.

## 4. Regras
- Tag, Release do GitHub, publicação na loja e a chave real: só com o aval do Vinicius (`PROJETO.md` §9).
- Nenhuma senha, keystore ou JSON de conta de serviço no git nem em log. O `Setup` nunca imprime o valor das variáveis.
- A keystore de teste do CI é gerada a cada execução e jogada fora; AAB assinado com ela **nunca** vai à Play (a assinatura não bate com a de upload).
