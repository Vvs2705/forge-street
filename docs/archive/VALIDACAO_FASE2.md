# Forge Street — continuação e validação da fase 2 (2026-10-07)

## RESUMO

Executado o handoff `PROMPT_CONTINUACAO_CHATGPT.md` (removido em 2026-10-09; está no histórico do git, commit 2df0f0d), passos A–C; o passo D foi convertido em propostas, sem expansão implementada. Custos 2.200 / 5.500 / 9.000 e joias de 80 após a Vitrine estão no núcleo atual. Core e Unity EditMode: 37/37. Windows compilado e fotografado; Android tem portão separado abaixo. Rune Relay não foi alterado.

## ENTREGAS

- `Scripts/Core/Defs.cs`: custos, preço e descrição da Vitrine.
- `Scripts/Core/Sim.cs`: preço centralizado em `PriceOf`, usado nas vendas da fila e diretas.
- `Tests/EditMode/CoreTests.cs`: curva de custos e regressão 60/80 × fila/direta, eventos, receita, outros produtos e save.
- `Tests/EditMode/BalanceTests.cs`: limites medidos, guarda dos primeiros 10 minutos e relatório do bot.
- `Scripts/View/Game.cs`: `-testsession` sem save/diário normal; autoplay também isola. `-shotchest` captura um marco real antes de o bot abrir. HUD usa a versão do aplicativo.
- `Editor/Setup.cs`: versão 0.2.0.
- `BALANCE.md` §10.7 e `AREA2_FASE2.md`: vigente, histórico e desvios 10/11.
- `README.md`, `RENDER_SPRITES.md`: estado atual e pendências.
- `PROXIMOS_EXPERIMENTOS.md`: opções de destino do ouro e A/B de abastecimento; todos os novos preços são propostas.

## TESTES E EVIDÊNCIAS

Pasta: `client/Builds/validation_phase2/`.

| Verificação | Resultado | Evidência |
|---|---|---|
| Baseline dotnet | 36/36 | saída inicial da sessão; medição prévia em `core_before.log/.trx` |
| Core final | 37/37, 0 falhas/ignorados | `core_final.log/.trx` |
| Prova vermelha isolada | teste novo falhou: esperado 80, recebido 60 | `core_red.log/.trx` |
| Cópia restaurada e reconstruída | 1/1 verde; rebuild 0 erros/avisos | `core_restored_verified.log/.trx`, `core_proof_rebuild.log` |
| Integridade da prova | cópia restaurada igual às fontes reais | `core_proof_integrity.txt` |
| Viewcheck dotnet | 0 erros / 0 avisos | saída da sessão; validação Unity adicional no build |
| Unity EditMode | 37/37, 0 falhas/ignorados | `editmode.xml/.log` |
| Unity Windows | Succeeded, errors=0 | `build_win.log`, `../win/ForgeStreet.exe` |
| Autoplay Windows, 60 min | OK; 20 upgrades; primeira venda 19 s; receita 46.377 | `autoplay.log` |
| Unity Android dev ARM64/IL2CPP | Succeeded, errors=0; APK 46.898.468 bytes (46,9 MB / 44,7 MiB), 07/10 às 15:07:54 | `build_android.log`, `../android/ForgeStreet-dev.apk` |

A primeira restauração da cópia ainda executou binário mutado por cache incremental. `core_restored.trx` conserva essa falha; a restauração válida é `core_restored_verified.trx`, após rebuild explícito. A fonte real não recebeu a mutação. `source_and_apk.sha256` registra fontes C# e APK do fechamento. Os logs/XML atuais também foram copiados para `client/Builds/` para substituir os resultados antigos.

### Fotos verificadas

| Foto | Cenário e observação |
|---|---|
| `shots/01_rua_completa.png` | 20 upgrades, rua aberta; joalheiro lilás. Captura inicial com pouca produção. |
| `shots/02_baus_oficina.png` | 20 upgrades, baú “15 espadas!” legível; log registra nobres 5/5 e baús disponíveis. |
| `shots/03_bau_jogo_normal.png` | Bot partindo do estado inicial; baú disponível em 145,5 s, com 2 upgrades. |
| `shots/04_fila_nobres.png` | 20 upgrades, nobres 5/5; baú “10 joias!” separado da fila; “+80” inteiro no enquadramento. |

Fotos e revisão não justificaram mudar tamanho/rótulo do baú, posição da fila ou tinta do joalheiro. Partes da outra área fora do enquadramento decorrem da câmera horizontal. Cor dos ajudantes em movimento ainda exige playtest.

Janela oculta causou `Failed to capture screen shot` em `shot01.log`; a captura com janela renderizada passou (`shot01_visible.log`). Todas as fotos usam `-testsession`, sem `-reset`: save e diário normais não são lidos nem gravados pelo jogo.

O “+80” fotografado não identifica especificamente uma venda direta na extremidade direita. Fila/direta e preço 60/80 foram verificados no núcleo; não há captura específica dos quatro casos visuais.

## NÚMEROS

Mesmo bot humano, 60 min, 30 ticks/s:

| Medida | Antes | Vigente |
|---|---:|---:|
| Último upgrade | 32:20 | 40:41 |
| Receita online | 43.844 | 46.377 |
| Ouro/min nos minutos 55–60 | 951 | 1.095 |
| Joias vendidas | 276 | 269 |
| Ouro restante | 26.479 | 21.212 |

A/B somente do preço, mantendo os novos custos: 947 → 1.095 ouro/min (+15,6%), +2.800 ouro em 60 min, mesmas 269 joias e último upgrade em 40:41. Os primeiros 10 min permanecem dentro da §3. Marco de 15 espadas e compatibilidade de saves antigos preservados.

## RISCOS

- Não há aparelho Android conectado por ADB; toque, suspensão/retomada, halos ASTC, FPS, memória e temperatura não foram validados em aparelho.
- Não foi executado PlayMode; o projeto possui testes EditMode de núcleo, além do smoke/render no player Windows.
- Fome de lingotes e ouro sem destino persistem; a bancada ficou 52% do período Joalheiro→60 min sem lingotes. O teto offline com tudo comprado é 18.000.
- Bot e foto não substituem sessões humanas nem provam retenção/viabilidade comercial.

## PRÓXIMO PASSO

Validar o APK em Android real e realizar o playtest. As propostas de luxo visual e esteira estão em `PROXIMOS_EXPERIMENTOS.md`; preços/retornos futuros precisam de bot A/B e avaliação humana antes de implementação. Nenhuma publicação, integração de pagamentos ou geração paga de arte foi executada.

## Comandos usados (Windows PowerShell)

```powershell
$taskProject = 'C:\Users\VINICIUS\Videos\MEUS PROJETOS\JOGOS NOVOS\03_FORGE_STREET'
$taskClient = Join-Path $taskProject 'client'
$taskEvidence = Join-Path $taskClient 'Builds\validation_phase2'
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
dotnet test "$taskClient\tools\coretests" -nologo -v q
dotnet build "$taskClient\tools\viewcheck\view" -nologo -v q

# Uma instancia Unity por vez; aguarde o encerramento antes da etapa seguinte.
$taskArgs = @('-batchmode','-nographics','-projectPath',('"'+$taskClient+'"'),'-runTests','-testPlatform','EditMode','-testResults',('"'+$taskEvidence+'\editmode.xml"'),'-logFile',('"'+$taskEvidence+'\editmode.log"'))
Start-Process $taskUnity -ArgumentList $taskArgs -WindowStyle Hidden -Wait

# Repetir para BuildAndroidDev e build_android.log depois de BuildWindows.
$taskArgs = @('-batchmode','-nographics','-quit','-projectPath',('"'+$taskClient+'"'),'-executeMethod','FS.EditorTools.Setup.BuildWindows','-logFile',('"'+$taskEvidence+'\build_win.log"'))
Start-Process $taskUnity -ArgumentList $taskArgs -WindowStyle Hidden -Wait

& "$taskClient\Builds\win\ForgeStreet.exe" -batchmode -nographics -autoplay 60 -logFile "$taskEvidence\autoplay.log"
# Capture com janela renderizada; aguarde sair antes de iniciar outro player.
& "$taskClient\Builds\win\ForgeStreet.exe" -screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession -bot -speed 8 -shot "$taskEvidence\shots\03_bau_jogo_normal.png" -shotchest -logFile "$taskEvidence\shot03.log"
```
