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

## Licença: os secrets que o Vinicius cola uma vez

Sem `UNITY_EMAIL` e `UNITY_PASSWORD` os jobs do Unity aparecem como **skipped** e o PR não fica vermelho.

Licença Personal: o GameCI (game-ci/cli) ativa uma vaga Personal pela conta Unity em cada rodada. O `.ulf` do PC (`UNITY_LICENSE`) não serve sozinho: ele é preso à máquina que o pediu e o runner reprova com `Machine bindings don't match` (visto em 2026-10-24); com e-mail e senha cadastrados, o GameCI cai sozinho na ativação pela conta.

1. Em https://github.com/Vvs2705/forge-street/settings/secrets/actions/new crie (botão *New repository secret*, um por vez):
   - `UNITY_EMAIL`: o e-mail da conta Unity logada no Hub;
   - `UNITY_PASSWORD`: a senha dessa conta.
2. A conta precisa estar **sem verificação em duas etapas** (o login automático não responde ao código).
3. Rode o workflow em https://github.com/Vvs2705/forge-street/actions/workflows/unity.yml (*Run workflow* na `main`) ou atualize qualquer PR.

`UNITY_LICENSE` (conteúdo de `C:\ProgramData\Unity\Unity_lic.ulf`, gerado no Hub em *Preferences → Licenses → Adicionar licença → Obter uma licença pessoal gratuita*) pode ficar cadastrada: o GameCI tenta o arquivo primeiro e, no erro de máquina, usa a conta.

## Regra de merge

Com os 4 jobs verdes no PR, o CI cobre o portão do `verify.sh full` (núcleo, EditMode, BuildWindows e autoplay) sem o PC. O `android-emulator` e o `release` continuam só no PC.
