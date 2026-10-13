# RemoteConfig e Analytics — Forge Street (A-PLAT-08 e A-PLAT-09)

Estado em 2026-10-13: tudo atrás de costura, **sem provedor remoto** (decisão P8 pendente). Hoje o jogo **não manda nada pela rede**: o provedor HTTP do RemoteConfig está com a URL vazia e o de analytics é nulo. `docs/PRIVACIDADE.md` §2 continua valendo como está.

Código: `client/Assets/_FS/Scripts/Core/RemoteConfig.cs` (travas, JSON, núcleo puro), `client/Assets/_FS/Scripts/View/Plataforma.cs` (fontes do config, Telemetria, provedores) e o boot em `Game.Awake`. Testes: `RemoteConfigTests` e `TelemetriaTests` (rodam no `dotnet test`).

## 1. RemoteConfig

### Formato

Um JSON plano. `id` e `versao` identificam o config; o resto são chaves.

```json
{
  "id": "ab_preco_espada",
  "versao": 3,
  "OfflineFactor": 0.3,
  "Price": [0, 0, 12, 25, 16, 60],
  "Encomendas": true,
  "Placements.supply.Ligado": false,
  "Placements.vip.Recarga": 600,
  "AnunciosLigados": true
}
```

- **Chave = nome exato de um campo público do `Tuning`** (`Core/Defs.cs`): `bool`, `int`, `long`, `float` ou array deles. Campo novo no `Tuning` já pode ser mudado pelo config, sem mexer no `RemoteConfig.cs`. Uma flag de IAP ou de Pass, quando existir, entra assim.
- **Placements de anúncio**: `Placements.<id>.<campo>`, com `<id>` em `vip`, `velocidade`, `cofre2x`, `encomenda_bonus`, `encomenda_troca` ou `supply` e `<campo>` em `Ligado`, `Recarga`, `TetoDia` ou `Valor` (o `Id` não muda).
- **Array**: precisa ter o mesmo tamanho do padrão (`Price` tem 6). O config só ajusta o que o build já tem e **nunca cria** item, placement, oferta, baú ou sorteio. Por isso não há como entrar item aleatório pago (V4).
- **Ignorado e registrado**: chave desconhecida, `id` de placement que o build não tem, tipo errado (`"0.3"` entre aspas, `1` num bool) e array de outro tamanho.

### Quando vale

O `Tuning` recebe o config **antes de o Sim nascer**, e o Sim lê o `Tuning` dele. Por isso um config novo vale **na próxima abertura**. Assim nenhum número muda no meio da partida e o save não precisa saber de onde veio o número.

Ordem das fontes (`RemoteConfigLocal.Ler`):

| Origem (`session_info config`) | Quando |
|---|---|
| `rcfile` | `-rcfile caminho`, **só em build de desenvolvimento**. Um caminho relativo é procurado no `persistentDataPath`. Se o arquivo não existir, a origem fica `rcfile_ausente` e valem os padrões (não cai no cache). |
| `cache` | `persistentDataPath/remoteconfig.json`, o último config bom que o provedor baixou. O `-testsession`, o `-autoplay` e o `-record` não leem o cache. |
| `padrao` | Os valores do `Tuning` embutidos no build. |

Um JSON que não abre (erro de sintaxe, número infinito, mais de 64 KB ou mais de 32 níveis) vale como os padrões e gera o aviso `json: nao abre`. Aplicar o mesmo config 2× dá o mesmo `Tuning`, porque os valores são absolutos.

### Travas (todas no núcleo, `RemoteConfig.Aplicar`)

Valor fora da faixa fica **preso** na borda e é registrado. Cada aviso vira uma linha `rc_aviso` no diário, no formato `chave: preso: pedido -> aplicado`.

| Regra | Chaves | Fonte |
|---|---|---|
| Faixa genérica: entre o padrão do build ÷ 10 e o padrão × 10. Um padrão 0 continua 0 (minério não se vende, Masterwork não se vende, placement sem recarga continua sem recarga). Inteiro arredonda. | todo número | A-PLAT-08 |
| Teto offline ≤ 7.200 s (um evento só encurta) | `OfflineMaxSeconds` | P2, `Balance.OfflineCapSeconds` |
| Fator offline ≤ 1 (o offline nunca rende mais que o online) | `OfflineFactor` | P2 — **revisar** |
| Cofre 2× no máximo dobra | `Placements.cofre2x.Valor` ≤ 1 | P2 |
| O anúncio só pode ficar **mais raro** que no build: recarga nunca abaixo do padrão | `Placements.*.Recarga` (VIP 300 s, Velocidade 300 s, Supply 600 s), `InterstitialCooldown` (180 s) | P14/V11, GDD §17.1 |
| O anúncio só pode ficar **mais raro** que no build: teto e empilhamento nunca acima do padrão | `AnunciosTetoDia` (30), `Placements.*.TetoDia`, `InterstitialMaxSessao` (3), `BoostMax` (3×), `BoostPlayerMax` (1,3×), `VipSummonPackMax` (20) | GDD §17.1–17.2 — **revisar** |
| Liga, mas não desliga | `AnunciosDepoisDaPrimeiraMelhoria` | D6/V18 |
| Intersticial: o remoto sempre pode desligar. Ligar só vale num build com `Game.IntersticialNoBuild = true`; hoje é `false` (o build do A/B no Limited Release muda isso). | `Interstitial` | P3/V10 |
| Chave mestra: com `false`, o `Aplicar` desliga todos os placements e o intersticial, e o `Game` nem inicia o SDK (`SemAnuncio`) | `AnunciosLigados` | DM §130 |

Coerência entre chaves diferentes (por exemplo `VipMin` < `VipMax`) fica com quem escreve o config. Antes de publicar um config que mexe em economia, rode o bot e registre os números no `BALANCE.md`. Hoje o `econsim` não lê JSON (pendência §5).

### Snapshot para A/B (DM §89)

`Resultado.Rotulo` = `id@versao` (`padrao@0` sem config, `sem_id@0` se o JSON não tem `id`). Ele vai:

- no diário, na linha `session_info,config,<rotulo> <origem>` (o `diario_report.py` agrupa por ele);
- em todo evento remoto, no campo `config`.

## 2. Analytics e crash

### Telemetria (o `IAnalytics` do `Game`)

1. **Local** (`Diario`): recebe todo evento na hora. Só grava em build de desenvolvimento ou com `-playtest`; o release no aparelho não grava nada. O arquivo tem teto de 1 MB e, quando passa, fica só a metade mais nova. Isso já tinha entrado na A-PLAT-06: a linha "Diário CSV grava também no release e cresce sem limite" do `PROJETO.md` §7 está resolvida (teste `Diario_PassouDoTeto_FicaACaudaMaisNova` e `Diario_Desligado_NaoGravaNada`).
2. **Remoto** (`IProvedorAnalytics`): **nada sai antes da resposta da tela de idade**. Até lá os eventos esperam numa fila em memória de 256. Se a fila encher, cai o mais velho, e o contador `Descartados` registra quantos.
   - Quando a pessoa responde, ou se já tinha respondido numa abertura anterior, `Game.Liberar` chama `Telemetria.Liberar`: o provedor inicia e a fila é enviada, em ordem.
   - `Liberar` vale uma vez por sessão. Se a resposta mudar nas Configurações, a mudança vale na próxima abertura, igual ao anúncio.
   - **ID de publicidade**: o provedor só pode usar com 18+ que aceitou (`Privacidade.Flags(...).Consentimento`). Menor de 18 nunca usa (V21), e adulto que recusou também não.
   - O evento em si nunca leva ID de aparelho, conta, localização nem ID de publicidade.
   - A busca do RemoteConfig (`IRemoteConfig.Buscar`) espera a mesma resposta. Com isso, nada vai para a rede antes dela.
   - Provedor que lança exceção não derruba o jogo.

### Esquema de evento v1 (`Evento.ParaJson`)

| Campo | Conteúdo |
|---|---|
| `v` | 1 (`Evento.Esquema`; subir quando um campo mudar de sentido) |
| `seq` | ordem na sessão (o evento logo antes de uma exceção é o `seq` anterior: o "last major event" do DM §90) |
| `nome`, `a`, `b` | taxonomia do diário (DM §86), os mesmos nomes e parâmetros do `diario.csv`; `a`/`b` cortados em 120 caracteres |
| `t` | segundos de jogo (`Sim.Time`) |
| `sessao` | 8 caracteres aleatórios por processo (o mesmo do diário) |
| `app` | `Application.version` |
| `config` | `id@versao` do RemoteConfig |

### Exceções (DM §90)

`Application.logMessageReceived` com `LogType.Exception` (não tratada ou `Debug.LogException`) vira o evento `excecao`: `a` = mensagem, `b` = 1ª linha da pilha.

- Teto de 5 por sessão (`Telemetria.ExcecoesMax`), para que um erro em loop não encha o diário nem a cota do provedor.
- O `persistentDataPath` e o `dataPath` viram `~` (no Windows eles têm o nome do usuário).
- Crash nativo (sinal, ANR) e os símbolos por build ficam para o SDK de crash da P8.

### Eventos novos no diário

| Evento | a | b |
|---|---|---|
| `session_info` | `config` | `id@versao origem` |
| `rc_aviso` | `chave: preso: pedido -> aplicado` ou `chave: motivo` | — |
| `rc_fetch` | `ok` / `invalido` / `falha` | bytes ou erro (só com a URL preenchida) |
| `ad_init` | `desligado` | `remoteconfig` (chave mestra) |
| `excecao` | mensagem sem caminho privado | 1ª linha da pilha |

## 3. Como plugar o provedor quando a P8 for decidida

**RemoteConfig, opção A (sem SDK):** publique o JSON acima num endereço HTTPS estático (CDN ou bucket) e preencha `RemoteConfigHttp.Url`.

- Não há mais nada a fazer no código: a busca usa timeout de 5 s, nunca bloqueia o boot, guarda no cache só o JSON que abre e o config vale na próxima abertura.
- Antes de publicar, atualize o `PRIVACIDADE.md` §2 (passa a haver tráfego: IP e data) e a Data Safety.

**RemoteConfig, opção B (SDK do fornecedor: Firebase, Unity Remote Config, GameAnalytics…):** escreva uma classe `IRemoteConfig`.

- `Ler` devolve os valores **já ativados** como o JSON plano deste documento (uma chave por campo). Isso pode vir do cache do SDK ou do `RemoteConfigLocal.Ler`.
- `Buscar` faz o fetch assíncrono e ativa o resultado para a próxima abertura. Grave com `RemoteConfigLocal.Guardar(json)` para manter o cache local como reserva.
- No `Game.Awake`, troque `new RemoteConfigHttp(...)`.
- As travas continuam valendo, porque estão no núcleo e não no fornecedor.

**Analytics e crash:** escreva uma classe `IProvedorAnalytics`.

- `Iniciar(idPublicidade)` configura o SDK. Com `false`: sem ID de publicidade e sem sinal de personalização de anúncio. Cada SDK tem a sua opção para isso; confira na documentação do fornecedor escolhido.
- `Enviar(e)` mapeia `nome` / `a` / `b` / `t` / `config` para o evento do SDK. O SDK faz o lote e o reenvio.
- Troque `new ProvedorNulo(...)` no `Game.Awake`.
- O `Iniciar` só é chamado depois da tela de idade. **Não inicialize o SDK antes disso** (nada de auto-init no manifest).

Checklist da troca:

- `verify.sh release-check` sem permissão nova inesperada;
- `PRIVACIDADE.md` §1–§2 e a Data Safety ("Registros de falhas" e "Diagnóstico" passam a ser "Sim");
- desligar o `submitAnalytics` do Unity (D-PRIV-8);
- upload de símbolos nativos por build;
- registrar a decisão P8 e o custo no `PROJETO.md`.

## 4. Testar (build de desenvolvimento)

- `-rcfile C:/caminho/rc.json`. No Android, empurre o arquivo para o `persistentDataPath` e passe o nome: `adb shell am start ... -e unity "-rcfile rc.json"`.
  - No diário: `session_info,config,<id@versao> rcfile` e uma linha `rc_aviso` para cada valor preso.
  - Com `{"id":"sem_ads","versao":1,"AnunciosLigados":false}`: nenhum botão de anúncio e `ad_init,desligado,remoteconfig`.
- `-analyticslog`: o Player.log mostra `ANALYTICS {"iniciar":...}` e um `ANALYTICS {json}` por evento, mas só depois da resposta da tela de idade. Num PC sem resposta gravada (`fs_privacidade` vazio no PlayerPrefs), `-testsession -analyticslog` mostra a fila saindo inteira no toque da resposta.

O `Aplicar` usa reflexão nos campos públicos do `Tuning` e do `AdPlacementDef`. No IL2CPP isso funciona porque o Sim lê todos esses campos e porque o stripping padrão (`managedStrippingLevel` vazio) não remove código do jogo. Se o stripping subir para High, rode o `-rcfile` no aparelho antes de publicar.

## 5. Pendências

- `econsim --rc arquivo.json`: rodar o bot com o config antes de publicar (portões do §3 do GDD).
- Experimentos (DM §89): o `id@versao` é o snapshot. Variante, início e fim vêm do fornecedor da P8.
- Faixas marcadas **revisar**: `OfflineFactor` ≤ 1 e os tetos de anúncio "nunca acima do build" (`BoostMax`, `BoostPlayerMax`, `VipSummonPackMax`).
