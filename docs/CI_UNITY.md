# Unity no CI do GitHub (P15)

O workflow `.github/workflows/unity.yml` roda no GitHub, com GameCI (docker `unityci/editor:ubuntu-6000.3.23f1-*`), o que o `client/tools/verify.sh full` faz no PC:

| Job | O que faz | Equivale no `verify.sh` |
|---|---|---|
| `EditMode (Unity)` | testes EditMode do Unity (NUnit 3.5), veredito pelo `result="Passed"` do XML | `run_unity editmode.log -runTests` |
| `build Windows` | `FS.EditorTools.Setup.BuildWindows` na imagem `windows-mono` | `build BuildWindows` |
| `autoplay 10 min` | baixa o build e roda `ForgeStreet.exe -batchmode -nographics -autoplay 10` num runner Windows; exige `AUTOPLAY OK` e saída 0 | o `.exe` com `-autoplay 10` |
| `build Android` | `FS.EditorTools.Setup.BuildAndroidDev` (IL2CPP ARM64) e guarda o APK como artefato por 3 dias | não está no `full`; cobre o `#if UNITY_ANDROID` que o viewcheck android cobre no PC |

O `nucleo.yml` continua com o `dotnet test` do núcleo. O viewcheck (`view`/`android`) não roda no CI: ele compila contra as DLLs do Unity do PC, e o EditMode e o build Android já compilam o mesmo código com o Unity de verdade.

Cada job guarda o `client/Library` em cache (uma chave por alvo). A 1ª rodada importa o projeto inteiro (~20–40 min por job); as seguintes reaproveitam o cache.

## Licença: os 3 secrets que o Vinicius cola uma vez

Sem `UNITY_LICENSE` os jobs do Unity aparecem como **skipped** e o PR não fica vermelho. Para ligar:

1. No PC, com o Unity Hub logado na conta da licença Personal, abra `C:\ProgramData\Unity\Unity_lic.ulf` no Bloco de Notas e copie **todo** o conteúdo (do `<?xml` até o `</root>`).
2. Em https://github.com/Vvs2705/forge-street/settings/secrets/actions/new crie 3 secrets (botão *New repository secret*, um por vez):
   - `UNITY_LICENSE`: o conteúdo do `.ulf` copiado no passo 1;
   - `UNITY_EMAIL`: o e-mail da conta Unity;
   - `UNITY_PASSWORD`: a senha da conta Unity.
3. Rode o workflow em https://github.com/Vvs2705/forge-street/actions/workflows/unity.yml (*Run workflow* na `main`) ou abra/atualize qualquer PR.

Se o `.ulf` não existir nesse caminho, abra o Unity Hub, *Preferences → Licenses*, devolva e ative de novo a licença Personal; o arquivo é recriado. A licença Personal vence de tempos em tempos: se o EditMode começar a falhar com erro de ativação, repita o passo 1 e atualize o `UNITY_LICENSE`.

## Regra de merge

Com os 4 jobs verdes no PR, o CI cobre o portão do `verify.sh full` (núcleo, EditMode, BuildWindows e autoplay) sem o PC. O `android-emulator` e o `release` continuam só no PC.
