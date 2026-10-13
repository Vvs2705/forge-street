# Roteiro do `POCO_RELEASE_GATE` (POCO F4)

Checklist do aparelho físico para os marcos (G-PROD-03 / I-PLAT-01; Documento Mestre §2, §113 e §151). Resultado:

```text
POCO_RELEASE_GATE = PASS | FAIL | BLOCKED
```

O POCO F4 (Android 14, 1080×2400) **não** é aparelho de regressão diária: o dia a dia é `dotnet test`, viewcheck, EditMode, build Windows + autoplay, emulador, bot e econsim. Ele entra só nos 4 marcos do §2 e só depois de o PC ficar verde (§2 abaixo). Uma vez PASS, **não repetir sem bug real**.

Referência de desempenho: a 0.5.1 no POCO (2026-10-09, build Development): 60,3–60,5 fps, p95 16,6 ms, 0 quadro acima de 33 ms, PSS 325–345 MB, bateria 38,9 → 40,8 °C, status térmico 0, 0 crash (`VALIDACAO_V05.md`, "Teste no POCO F4"). O roteiro daquele teste está no histórico: `git show 6032e82:docs/ROTEIRO_TESTE_POCO.md`.

## 1. Quando rodar e como dar o veredito

| Marco (DM §2) | Build sob teste | Itens |
|---|---|---|
| Gate 1 — Feature Complete | AAB de release; sem a chave de release (hoje), o APK dev, declarado na tabela | todos |
| Gate 2 — Release Candidate (I-PLAT-01) | AAB de release | todos |
| Gate 3 — bug Android específico | o build com o conserto | só os do bug (toque G10, ciclo de vida G15/G16, GPU G11, vibração G8, voltar G17, hardware) e o que o conserto tocou |
| Gate 4 — build de publicação | o AAB que vai para a loja | mesmo SHA-256 do RC = nada a repetir; build novo = G3, G4, G7, G11 (1 janela), G14 e G17 |

- **PASS:** nenhum FAIL e nenhum BLOCKED. N/A só onde este roteiro já marca (G18, G19 e a parte de distritos do G6); quando o sistema existir, o item deixa de ser N/A.
- **FAIL:** pelo menos 1 item reprovado com bug real e reproduzível (passos, build, esperado × obtido, severidade, evidência em arquivo).
- **BLOCKED:** nenhum FAIL, mas algum item não pôde rodar por dependência externa (conta LevelPlay sem anúncio, chave de release, trilha interna da Play, aparelho).
- **Não repetir sem bug real:** depois do PASS, o POCO só volta no próximo marco ou por bug Android real. Depois de FAIL, repete só os itens reprovados e o que o conserto tocou (G5 inteiro só se o conserto mexeu no laço do jogo). Depois de BLOCKED, roda só o item bloqueado quando a dependência chegar (o resto vale se o SHA-256 do build for o mesmo).
- Nenhum número sem arquivo: toda medida vai para a pasta de evidências (`$G`, abaixo) e entra na tabela do §4.

## 2. Antes do aparelho

**No PC, nesta ordem (o POCO não é o 1º lugar a achar bug):**
1. `bash client/tools/verify.sh full` verde.
2. `bash client/tools/verify.sh android-emulator --migracao <pasta principal>/client/Builds/android/ForgeStreet-emu-0.6.0.apk` verde (smoke + instalar por cima no emulador). Hoje o AVD está sem boot: §6.
3. `bash client/tools/verify.sh release` (o Vinicius define as 4 variáveis `FS_*`; gera e confere o AAB) ou `verify.sh release-check <aab>` num AAB pronto.
4. APK dev do **mesmo commit** (`FS.EditorTools.Setup.BuildAndroidDev` → `client/Builds/android/ForgeStreet-dev.apk`, ARM64): é ele que instala por cima da 0.5.1/0.6.0 (mesma chave de debug) e aceita as flags de dev pelo cabo.
5. Anotar commit (`git rev-parse --short HEAD`), `sha256sum` do AAB e do APK, versionName e versionCode.

**Aparelho, nas mesmas condições da 0.5.1:** cabo USB no PC com "Depuração USB" e "Instalar via USB" ligados (o cabo carrega o aparelho), brilho fixo, nenhum outro app aberto, Wi-Fi ligado. Anotar a temperatura da bateria antes de começar; se estiver acima de 38,9 °C (a partida da 0.5.1), esperar esfriar.

**Git Bash, na raiz do repositório** (o `adb` no PATH, como no `medir_aparelho.sh`):
```
export MSYS_NO_PATHCONV=1    # sem isto o Git Bash troca /sdcard por C:/Program Files/Git/sdcard
P=br.com.vstack.forgestreet; ACT=$P/com.unity3d.player.UnityPlayerGameActivity; F=/sdcard/Android/data/$P/files
V=0.7.0; G=playtest/gate_$V; mkdir -p $G      # evidências; playtest/ fica fora do git (dados de pessoa real)
```

**Backup do save que está no POCO** (a instalação limpa da Fase B apaga o save do aparelho):
```
adb shell dumpsys package $P | grep -E 'versionName|versionCode' > $G/antes_versao.txt
adb exec-out run-as $P cat shared_prefs/$P.v2.playerprefs.xml > $G/antes_prefs.xml   # 0.5.1/0.6.0: o save está aqui (só APK dev)
adb exec-out cat $F/save.txt > $G/antes_save.txt                                     # só existe se já houve build v0.7
```

**Instalar o AAB na Fase B:**
- Preferido: trilha de teste interno da Play, a mesma rota do jogador (depende da conta e do testador, decisões P9 e license tester).
- Sem a Play: o bundletool que vem com o Unity, assinando com a chave de upload (o Vinicius roda; as senhas ficam nas variáveis dele):
```
AP="C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Data/PlaybackEngines/AndroidPlayer"; J="$AP/OpenJDK/bin/java.exe"
BT=$(ls "$AP"/Tools/bundletool-all-*.jar); ADBX="$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe"
"$J" -jar "$BT" build-apks --bundle=client/Builds/android/ForgeStreet-$V.aab --output=$G/gate.apks --overwrite \
  --connected-device --adb="$ADBX" --ks="$FS_KEYSTORE" --ks-key-alias="$FS_KEY_ALIAS" --ks-pass=pass:"$FS_KEYSTORE_PASS" --key-pass=pass:"$FS_KEY_PASS"
"$J" -jar "$BT" install-apks --apks=$G/gate.apks --adb="$ADBX"
```
O APK dev (chave de debug) e o release (chave de upload) não instalam um por cima do outro (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`): por isso a Fase A (dev, por cima) vem antes da Fase B (release, limpa).

## 3. Itens do gate

Cada item: **Fazer** · **Medir** · **PASS** · **FAIL / BLOCKED**.

### Fase A — APK dev do candidato por cima do save atual

**G1. Migração por cima da 0.5.1/0.6.0** (§113/§151 "migration"; Save 2.0 e schema 2)
- **Fazer:** com a 0.5.1 ou 0.6.0 instalada e com progresso (o backup do §2 feito), `adb install -r client/Builds/android/ForgeStreet-dev.apk`; abrir pelo ícone; responder a tela de idade; jogar 1 min; HOME. Sem versão antiga no aparelho: `adb install -r <pasta principal>/client/Builds/android/ForgeStreet-0.6.0.apk`, jogar 5 min, HOME e seguir.
- **Medir:**
  ```
  grep -oiE '(^|%0A|>)(up|gold)(=|%3D)[0-9]+' $G/antes_prefs.xml      # up = flags 0/1 das compras, gold = ouro (com ou sem URL encoding)
  adb exec-out cat $F/save.txt > $G/depois_save.txt; grep -E '^(schema|gold|ups)=' $G/depois_save.txt
  adb exec-out cat $F/diario.csv | grep -E 'save_(prefs|corrupt|error|newer)'
  ```
- **PASS:** `schema=2`; o `ups=` novo tem tantas compras quanto os `1` do `up=` antigo; ouro igual ou maior (o cofre pode pagar o tempo fora); a oficina abre com as mesmas estações, ajudantes e vagas do balcão; o diário tem `save_prefs` com `Legacy` e nenhum `save_corrupt`/`save_error`; a tela de idade aparece (quem atualiza nunca respondeu).
- **FAIL:** compra ou ajudante a menos, ouro menor sem compra, `schema=1`, `save_corrupt`, crash na abertura. **BLOCKED:** nenhum APK antigo disponível.

**G2. Câmera a 9 m e leitura a 1080×2400** (A-ART-03, V23; HUD e cartões v2)
- **Fazer:** fila de 8 vagas sem mexer no save (`-testsession` não lê nem grava o save e não mostra a tela de idade):
  ```
  adb shell am force-stop $P
  adb shell "am start -n $ACT -e unity '-testsession -buyids vitrine,balcao5,balcao6,balcao7,balcao8 -warmup 45'"
  ```
  Levar o ferreiro à fileira das fornalhas e depois à do Depósito; abrir a bandeja de Melhorias. Foto em cada: `adb exec-out screencap -p > $G/camera_fornalhas.png` (`_deposito`, `_bandeja`). No fim, `am force-stop` e abrir pelo ícone.
- **PASS:** as 8 vagas e o balão do 1º da fila inteiros abaixo da faixa de cima em todas as fileiras (o 9:20 do POCO é o aspecto em que até o Depósito cabe tudo); bandeja aberta cobre só a fileira do Depósito sem esconder a cabeça do ferreiro; ouro da HUD, dica e textos dos cartões ("4 → 2,5 s", "Faltam N", "Precisa: X") legíveis com o braço estendido; nada sob o recorte da câmera frontal nem sob a barra de gestos.
- **FAIL:** vaga ou balão cortados no POCO, HUD sobre o recorte, texto ilegível.

### Fase B — AAB de release, instalação limpa

Trocar de build: `adb uninstall $P` (apaga o save do aparelho; o backup está em `$G`) e instalar o AAB (§2).

**G3. Instalação limpa** (§113/§151 "clean install")
- **Fazer:** com o AAB instalado, `adb logcat -c` e abrir: `adb shell am start -W -n $ACT | tee $G/abertura.txt`.
- **Medir:** `adb shell dumpsys package $P | grep -E 'versionName|versionCode|targetSdk|flags=' > $G/instalado.txt` e `adb shell getconf PAGE_SIZE >> $G/instalado.txt` (anotar; o 16 KB em si é o `release-check`). `TotalTime` do `am start -W` = abertura a frio (anotar; ainda não há base).
- **PASS:** instala sem erro; versionName, versionCode e targetSdk iguais aos do `release-check`; sem `DEBUGGABLE` nas flags; a 1ª tela é a de idade; depois da resposta, a oficina do zero (sem compras, sem cofre, dica apontando a 1ª ação).
- **FAIL:** erro de instalação, crash na 1ª abertura, progresso de outra instalação, `DEBUGGABLE` no release.

**G4. Tela de idade na 1ª abertura e SDK só depois da resposta** (E-PLAT-01, DM §94)
- **Fazer:** na abertura do G3, ficar 30 s na tela de idade sem responder; apertar voltar (não pode fechar); responder "Menos de 13"; jogar 1 min. Depois, Configurações > Privacidade > "18 ou mais" > Aceitar; `adb shell am force-stop $P` e abrir de novo.
- **Medir:** depois de reabrir (o buffer do logcat guarda as duas aberturas desde o `logcat -c` do G3):
  ```
  adb logcat -d > $G/logcat_idade.txt
  grep -niE 'ADS (ad_init|privacidade)|ironsource|levelplay' $G/logcat_idade.txt | head -40
  ```
- **PASS:** a 1ª linha `ADS` é `ad_init espera consentimento`; nenhuma linha de inicialização do SDK (IronSource/LevelPlay) antes de `ADS privacidade <13 coppa=True gdpr=False ccpa=True aaid_optout=True familias=True` (linha do sistema que só cita o pacote do SDK registrado no manifesto não conta: em dúvida, o coordenador lê); `ADS ad_init ok` só depois (sem rede sai `ad_init falha`, que não reprova este item); nenhum botão de anúncio antes da resposta; nenhuma faixa pré-marcada; Recusar e Aceitar do mesmo tamanho e cor; a mudança em Configurações loga `mudou para 18+ ... sem anuncio ate reabrir` e, depois de reabrir, `ADS privacidade 18+ coppa=False gdpr=True ccpa=False aaid_optout=False familias=False`.
- **FAIL:** SDK iniciando antes da resposta, botão de anúncio antes da resposta, faixa pré-marcada, dá para pular a pergunta na 1ª abertura, flags erradas para a faixa.

**G5. 30 min jogando** (§113 "30 min") — G6 a G13 são medidos dentro desta sessão
- **Fazer:** logo depois do G4 (aparelho com a resposta 18+), o Vinicius joga 30 min seguidos, normalmente (vende, compra, contrata, assiste aos anúncios oferecidos); o coordenador mede pelo cabo sem tocar no aparelho. Logcat da Fase B inteira em arquivo, no formato padrão (o `grep` abaixo depende dele): `adb logcat -c; adb logcat > $G/logcat_faseB.txt & LC=$!` (`kill $LC` no fim do G17). Passos que fecham, reabrem ou tiram o jogo da frente (G7: reabrir na recarga e modo avião; G8 g; G9 a e d) ficam para depois dos 30 min.
- **Medir:** `grep -A1 'FATAL EXCEPTION' $G/logcat_faseB.txt | grep "Process: $P"; grep -E "ANR in $P|>>> $P <<<| Unity +: .*Exception" $G/logcat_faseB.txt` (o mesmo filtro do `verify.sh`)
- **PASS:** 30 min sem crash, ANR, travada > 1 s nem exceção da Unity; nunca fica sem o que fazer.
- **FAIL:** qualquer um dos acima.

**G6. Progressão** (§113 "district progression sample", §151 "representative districts")
- **Fazer:** durante o G5, anotar o relógio de: 1ª venda, Fole, 2ª bigorna, Escudos, Esteira, Corredor (rua lateral) e o que mais chegar em 30 min.
- **Medir:** relógio na mão (o release não grava diário; no APK dev, `python client/tools/diario_report.py`).
- **PASS:** toda compra aparece na cena e funciona (estação nasce, ajudante trabalha, portão abre com o Corredor, a esteira leva o lingote); nenhuma trava de progressão; a dica nunca aponta para algo impossível. Os tempos são dado para comparar com o bot (GDD §3), não critério.
- **FAIL:** compra que não aparece ou não funciona, ajudante parado com trabalho a fazer, dica errada, progressão travada. **N/A (parte):** hoje só existe o Old Forge com a rua lateral; a amostra de distritos vira item cheio com o distrito 2 (Wave C).

**G7. Rewarded** (§113/§151 "rewarded"; D6/V18, P14/V11)
- **Fazer:** antes da 1ª melhoria, conferir que Velocidade e Chamar VIP não aparecem; depois dela, assistir Velocidade (2× por 60 s), de novo (3×), e Chamar VIP; durante a recarga do VIP (5 min, botão cinza com cronômetro), `am force-stop` e reabrir; voltar de um anúncio; ligar o modo avião e tentar de novo.
- **Medir:** `grep -E 'ADS (ad_request|ad_shown|ad_reward|ad_reward_late|ad_fail)' $G/logcat_faseB.txt > $G/anuncios.txt`
- **PASS:** botões só depois da 1ª melhoria; cada anúncio visto até o fim paga 1 vez (`ad_request` → `ad_shown` → `ad_reward` e o efeito na tela: selo 2×/3× ou "VIP a caminho!"); a recarga do VIP continua depois de reabrir; voltar do anúncio não abre o cofre; sem rede o botão some ou avisa "Anúncio indisponível", sem travar.
- **FAIL:** recompensa faltando ou em dobro, `ad_reward_late` num anúncio visto até o fim, botão antes da 1ª melhoria, reabrir zera a recarga. **BLOCKED:** no release não há anúncio simulado; sem a conta LevelPlay aprovada e o POCO como test device (`docs/LEVELPLAY.md`), o botão nunca aparece (o simulado do build dev não vale para o gate).

**G8. Vibração por importância** (A-AUD-22, DM §72)
- **Fazer:** com Vibração ligada: (a) 2 min só vendendo espada e escudo; (b) comprar 1 melhoria; (c) comprar o que abre estação (2ª bigorna, Escudos) ou o Corredor; (d) servir um VIP; (e) entregar uma encomenda; (f) vender joias seguidas; (g) desligar Vibração em Configurações, comprar outra melhoria e reabrir o app.
- **Medir:** na mão; prova com `adb shell dumpsys vibrator_manager > $G/vibra_<passo>.txt` depois de cada passo (lista as vibrações recentes por pacote; o formato muda por versão do Android: contar as entradas de `$P`).
- **PASS:** (a) nenhuma vibração; (b) curta (30 ms); (c) mais forte (60 ms); (d) e (e) média (40 ms); (f) fraca (20 ms), no máximo 1 a cada 1,5 s; (g) nenhuma vibração e a chave continua desligada ao reabrir.
- **FAIL:** venda comum vibra, evento importante não vibra, vibra com a chave desligada, chave que não fica gravada.

**G9. Áudio** (A-AUD-23, DM §70/§71)
- **Fazer:** volume de mídia em ~50%. (a) Configurações: desligar Música (ficam efeitos e ambiente), religar; desligar Efeitos (efeitos e ambiente somem juntos, fica a música); reabrir o app; (b) comparar o som da oficina parada (começo) com a oficina cheia (estações trabalhando e fila); (c) 10 min seguidos com a Música ligada; (d) HOME com som tocando e voltar.
- **Medir:** ouvido (fone opcional). O custo do áudio procedural está no G11–G13; a síntese dos 14 efeitos está no `TotalTime` do G3.
- **PASS:** cada chave isola o seu grupo e fica gravada ao reabrir; a oficina cheia soa claramente mais cheia que a parada (o projeto dá ~9,5 dB); a volta do laço de 28,8 s da música não estala; nos 10 min o Vinicius não desliga a música (parecer dele); fora do jogo, silêncio; ao voltar, o som volta.
- **FAIL:** chave que não isola ou não grava, som em segundo plano, estalo na emenda, som cortado ou distorcido, música que cansa (parecer).

**G10. Toque** (§151 "touch")
- **Fazer:** joystick (arrasto curto e longo, soltar no meio, dedo saindo da tela), cartões e botão de compra da bandeja, botões de anúncio nos cantos, engrenagem, painel do cofre, dois dedos ao mesmo tempo.
- **PASS:** tudo responde no 1º toque; o joystick segue o dedo sem atraso e para ao soltar; tocar na bandeja não anda com o ferreiro; nenhum botão sob o recorte ou a barra de gestos.
- **FAIL:** toque perdido ou duplo, joystick preso, botão inalcançável.

**G11–G13. FPS, memória e temperatura** (§151 "FPS", §113 "thermal"; com o áudio procedural e a esteira)
- **Fazer:** no início do G5, a série de temperatura por minuto (em segundo plano) e 6 janelas de medida a cada ~4,5 min (como na 0.5.1):
  ```
  bash client/tools/medir_aparelho.sh $G/serie --temp-serie 30 > $G/serie.log 2>&1 &   # $G/serie/temperatura.csv: 31 linhas
  for i in 1 2 3 4 5 6; do bash client/tools/medir_aparelho.sh $G/perf_$i 270; done
  ```
  (O `medir_aparelho.sh` também tenta puxar o diário, que o release não grava: ignorar. Desde o I-PLAT-02 o `aparelho.txt` de cada pasta traz versionCode, versionName, targetSdk e `page_size` instalados, e o `temperatura.csv` traz por minuto a bateria, o status do `thermalservice` e o FPS/p95 dos últimos ~2 s.)
- **Medir:** por janela, FPS médio, p95 e quadros > 33 ms (linha `FPS:`), `TOTAL PSS` e temperatura/status térmico.
- **PASS (proposta do QA; ajustar antes do gate, nunca depois):**
  - **G11 FPS:** em todas as janelas, FPS médio ≥ 58, p95 ≤ 17 ms e no máximo 1% dos quadros acima de 33 ms.
  - **G12 Memória:** PSS ≤ 380 MB em todas (325–345 da 0.5.1 + 10%) e a última janela até 30 MB acima da primeira (sem vazamento).
  - **G13 Temperatura:** bateria ≤ 41 °C no fim dos 30 min e status térmico 0.
- **FAIL:** qualquer janela fora. Partida acima de 38,9 °C não é FAIL: esfriar e refazer. A base da 0.5.1 é de build Development; o release tem de ficar igual ou melhor (declarar o tipo de build na tabela).

### Depois dos 30 min

**G14. Save atômico no IL2CPP ARM** (A-PLAT-02: `.tmp` → `File.Replace` → `.bak`)
- **Fazer:** depois do G5 (30 min de autosave a cada 5 s), listar a pasta; depois, 3 vezes: comprar algo, esperar 10 s, `adb shell am force-stop $P` e reabrir.
- **Medir:** `adb exec-out ls -l $F > $G/save_ls.txt; adb exec-out head -4 $F/save.txt`
- **PASS:** `save.txt` e `save.txt.bak` presentes, nenhum `save.txt.tmp` com o app parado, nenhum `save.txt.corrupt-*`; cabeçalho `sha256=`, `schema=2`, `content=<versão>`, `at=`; depois de cada force-stop o progresso volta (perde no máximo os últimos 5 s). Se o `File.Replace` falhar neste sistema de arquivos, o código troca por `Move` sem aviso e o `.bak` aparece igual: o critério é o resultado observável.
- **FAIL:** sem `.bak`, `.tmp` sobrando, `.corrupt-*`, progresso perdido ou de volta ao início.

**G15. Segundo plano e volta** (§151 "background/foreground")
- **Fazer:** (a) HOME 10 s e voltar; (b) recentes → outro app → voltar; (c) bloquear a tela 30 s e desbloquear; (d) HOME, abrir 3–4 apps pesados (câmera, navegador) até o Android matar o jogo, e voltar; (e) puxar a barra de notificações jogando.
- **PASS:** volta no mesmo ponto, sem crash nem tela preta; boost e VIP não correm fora (pausa); som para fora e volta; em (d), reabre com o progresso do último save; menos de 60 s fora não mostra "Bem-vindo de volta!".
- **FAIL:** crash, tela preta, progresso perdido, som fora do jogo, cofre com menos de 60 s.

**G16. Offline (cofre)** (§113/§151 "offline")
- **Fazer:** com renda, fechar o jogo (force-stop ou arrastar dos recentes) por 2 min e abrir; depois por 10 min; depois por menos de 60 s.
- **Medir:** foto do painel (`screencap`) e o ouro antes e depois de confirmar.
- **PASS:** "Bem-vindo de volta!" aparece 1 vez, com valor > 0, e o ouro sobe exatamente o mostrado; com menos de 60 s não aparece; reabrir logo em seguida não paga de novo. Teto de 2 h e relógio adiantado já são provados no núcleo (`ExploitTests`): não repetir no aparelho.
- **FAIL:** sem painel com renda, valor 0, paga em dobro, ouro diferente do mostrado.

**G17. Voltar do Android** (§113/§151 "Back"; com gestos e, se der, com os botões de navegação)
- **Fazer:** voltar em cada estado: tela de idade na 1ª abertura e no passo do consentimento (antes, numa instalação limpa, ou pela revisão em Configurações > Privacidade); "Bem-vindo de volta!" aberto; Configurações aberta; bandeja de Melhorias aberta; tela limpa; durante um anúncio.
- **PASS:** idade na 1ª abertura fica; consentimento volta à idade; painel, Configurações e bandeja fecham só o que está aberto, um por toque (um toque nunca vale por dois); tela limpa grava e sai; durante o anúncio o jogo não faz nada (o anúncio trata).
- **FAIL:** sai com algo aberto, um toque fecha dois níveis, voltar sem efeito com a tela limpa.

### Ainda não existem

**G18. Evento** (§113/§151 "event") — **N/A** até o D-CORE-03 (LiveOps). Quando existir: entrar no evento, pontuar, receber o prêmio, evento terminando com o app fechado.

**G19. IAP** (§113/§151 "IAP test") — **N/A** até a Wave E (E-PLAT-04/05). Quando existir: compra de teste com license tester, compra interrompida, restore numa instalação limpa.

## 4. Resultado (o Vinicius preenche)

Copiar esta seção para o `VALIDACAO_<versão>.md` da versão e preencher lá; este roteiro fica em branco para o próximo marco.

- Data e hora: · Marco (Gate 1–4):
- Build: arquivo · tipo (release/Development) · versionName / versionCode · SHA-256 · commit:
- Aparelho: POCO F4 · Android · temperatura inicial (°C) · brilho · cabo/Wi-Fi:
- Pasta de evidências: `playtest/gate_<versão>/`

| # | Item | Resultado (PASS / FAIL / BLOCKED / N/A) | Número medido | Evidência (`$G/...`) | Observação |
|---|---|---|---|---|---|
| G1 | Migração por cima da 0.5.1/0.6.0 | | compras antes/depois; ouro antes/depois | `antes_prefs.xml`, `depois_save.txt` | |
| G2 | Câmera a 9 m e leitura a 1080×2400 | | | `camera_*.png` | |
| G3 | Instalação limpa | | TotalTime (ms); versionCode; targetSdk | `abertura.txt`, `instalado.txt` | |
| G4 | Idade e SDK só depois da resposta | | | `logcat_idade.txt` | |
| G5 | 30 min jogando | | crashes / ANR / exceções | `logcat_faseB.txt` | |
| G6 | Progressão | | tempos da 1ª venda, Fole, 2ª bigorna, Escudos, Esteira, Corredor | | distritos: N/A até a Wave C |
| G7 | Rewarded | | pedidos / mostrados / recompensas | `anuncios.txt` | |
| G8 | Vibração por importância | | | `vibra_*.txt` | |
| G9 | Áudio | | | | |
| G10 | Toque | | | | |
| G11 | FPS | | médio, p95, % > 33 ms (pior janela) | `perf_*/` | |
| G12 | Memória (PSS) | | 1ª e última janela, pico (MB) | `perf_*/meminfo.txt` | |
| G13 | Temperatura | | início → fim (°C), status térmico | `serie/temperatura.csv` | |
| G14 | Save atômico (save.txt + .bak) | | | `save_ls.txt` | |
| G15 | Segundo plano e volta | | | | |
| G16 | Offline (cofre) | | valor do painel × ouro | | |
| G17 | Voltar do Android | | | | |
| G18 | Evento | N/A | | | sem LiveOps (D-CORE-03) |
| G19 | IAP | N/A | | | Wave E |

```text
POCO_RELEASE_GATE = ______   (PASS | FAIL | BLOCKED)
```

Bugs abertos (um por FAIL): passos · build · esperado × obtido · severidade · evidência.

## 5. Depois do gate

- **PASS:** registrar no `VALIDACAO_<versão>.md` e no `PROJETO.md` §1; o POCO só volta no próximo marco ou por bug Android real.
- **FAIL / BLOCKED:** §1 (repete só o que o bug ou a dependência pede).
- O progresso antigo do POCO fica no backup (`$G/antes_*`); não há restauração automática.

## 6. Emulador: recuperar o AVD `fs_playstore`

**Sintoma (2026-10):** o AVD não sobe nem com boot frio e o log mostra `Can't find service: package` — a partição de dados corrompeu quando o C: encheu. O `verify.sh android-emulator` agora aborta com menos de 6 GB livres para não repetir isso.

**Recuperar (só com aval do Vinicius):** com pelo menos 6 GB livres no C: e nada pesado aberto (Unity, Blender):
```
"$LOCALAPPDATA/Android/Sdk/emulator/emulator.exe" -avd fs_playstore -wipe-data -no-snapshot -crash-report-mode never -no-metrics
```

**O que o `-wipe-data` apaga:** a partição de dados inteira do AVD — apps instalados (o Forge Street e o que tiver sobrado do teste comparativo), saves e diários no `/sdcard` emulado (que mora nessa partição), a conta Google, se houver uma entrada, e as configurações do Android. Os snapshots já tinham sido apagados em 2026-10-10.
**O que fica:** a imagem do sistema (Android 15 Google Play x86_64), a configuração do AVD (1080×2400, RAM), o SDK e tudo no PC (APKs em `client/Builds/android/`, `playtest/`).

**Depois:** o 1º boot frio leva até 10 min. Conta Google só se for usar a Play Store, e quem digita a senha é o Vinicius. Em seguida: `bash client/tools/verify.sh android-emulator --migracao <pasta principal>/client/Builds/android/ForgeStreet-emu-0.6.0.apk` (smoke + instalar por cima, pendente desde o PR #16).
