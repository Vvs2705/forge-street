# Release — Forge Street (pipeline e o que falta do Vinicius)

Atualizado: 2026-10-24 · Estado: **pipeline sem segredo pronto no repositório; nada publicado, nenhuma tag criada.** O AAB de verdade (chave de upload real) e a subida à Play dependem dos passos do §3, que são do Vinicius.

## 1. Como uma versão sai

| Passo | Onde | O que faz |
|---|---|---|
| versão | `client/Assets/_FS/Editor/Setup.cs` (`Setup.Version`, SemVer X.Y.Z) | o `versionName`; o `versionCode` sai dela (`Setup.VersionCode`: X·10000 + Y·100 + Z; 0.6.0 = 600, 1.0.0 = 10000), sempre crescente |
| AAB | `Setup.BuildAndroidRelease` | build não-development, IL2CPP arm64, targetSdk 36, assinado pelas variáveis `FS_KEYSTORE`, `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS`, `FS_KEY_PASS`. Sem elas o build **falha** (nunca cai na chave de debug) |
| conferência | `client/tools/verify.sh release-check <aab>` | targetSdk ≥ 36, versionCode coerente e > 1, não-debuggable, só arm64-v8a, 16 KB, módulo base ≤ 200 MB, assinatura que não é a de debug, permissões da lista aprovada |
| no PC | `client/tools/verify.sh release` | quick + o AAB + o release-check, com as 4 variáveis no ambiente do Vinicius |
| no CI | workflow `release` (`.github/workflows/release.yml`) | o mesmo build com uma **keystore de teste gerada na hora** (descartável, só prova que o pipeline funciona) + release-check + o AAB como artifact. Roda à mão (`workflow_dispatch`) e numa tag `v*`. **Não sobe à Play** |
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
1. No PC, fora do repositório (regra do estúdio: segredo em `%USERPROFILE%\.forgestreet\`):
   ```bash
   keytool -genkeypair -v -keystore "$USERPROFILE/.forgestreet/upload.jks" -alias forgestreet-upload -keyalg RSA -keysize 4096 -validity 10000
   ```
   O `keytool` vem com o JDK do Unity: `.../Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe`.
2. **Backup fora do PC** do `.jks` e das duas senhas (GDD §15: perder a chave de upload obriga a pedir reset ao suporte da Play).
3. Variáveis do usuário no Windows: `FS_KEYSTORE` (caminho do `.jks`), `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS` (`forgestreet-upload`), `FS_KEY_PASS`. Com elas, `client/tools/verify.sh release` gera e confere o AAB no PC.

### 3.2 Secrets do GitHub para o AAB real no CI (opcional)
Em Settings > Secrets and variables > Actions do repositório:
- `FS_KEYSTORE_B64`: o `.jks` em base64 (`base64 -w0 upload.jks`);
- `FS_KEYSTORE_PASS`, `FS_KEY_ALIAS`, `FS_KEY_PASS`.

Com eles, o workflow `release` usa a chave real em vez da de teste (o job detecta os secrets; sem eles, segue com a de teste e avisa).

### 3.3 Play Console (conta e app)
1. Conta de desenvolvedor e perfil de pagamento (P9; a conta está sendo aberta em outro projeto).
2. Criar o app `br.com.vstack.forgestreet` e ativar o **Play App Signing** (a Play guarda a chave do app; a do §3.1 é só a de upload).
3. Subir o 1º AAB **à mão** numa faixa de teste interno (a Play não aceita o 1º pela API).
4. Produtos do IAP (`IAP.md` §5.1: `iap_pacote_mestre` e os dois do Pass) e a chave RSA da licença no `LojaUnity`.

### 3.4 Conta de serviço para subir pelo CI (depois do 1º upload)
1. Google Cloud: criar uma conta de serviço no projeto ligado à Play e gerar a chave JSON.
2. Play Console > Usuários e permissões: convidar o e-mail da conta de serviço com "Lançar apps em faixas de teste" só para este app.
3. Secret `PLAY_SERVICE_ACCOUNT_JSON` no GitHub. O passo de upload para a faixa interna entra no workflow `release` quando esse secret existir (ainda não implementado: sem ele nada sobe).

## 4. Regras
- Tag, Release do GitHub, publicação na loja e a chave real: só com o aval do Vinicius (`PROJETO.md` §9).
- Nenhuma senha, keystore ou JSON de conta de serviço no git nem em log. O `Setup` nunca imprime o valor das variáveis.
- A keystore de teste do CI é gerada a cada execução e jogada fora; AAB assinado com ela **nunca** vai à Play (a assinatura não bate com a de upload).
