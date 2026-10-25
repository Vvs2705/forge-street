# Cloud Save (UGS)

Estado (2026-10-24): **o SDK está ligado no código, atrás da flag `Tuning.CloudSave`, que é `false` por padrão.** Com a flag desligada, o jogo segue só com o save local (`SaveStore` + espelho), como antes. Ligar = `"CloudSave":true` no `fs_config` do Remote Config, testando primeiro no ambiente `teste` (`-ugsenv teste`) e só então no `production`, depois da revisão de privacidade abaixo.

## Regra (`FS.Core.CloudSave.Resolver(local, remoto)`)

O jogo compara o texto inteiro do envelope (`SaveEnvelope`, Save 2.0) do aparelho com o da nuvem e escolhe uma de três ações: `Subir`, `Baixar` ou `Nada`.

| Situação | Ação |
|---|---|
| Algum dos dois é de um build mais novo (`NewerSchema`) | `Nada`. Não sobe por cima e não baixa o que este build não lê. |
| A nuvem está vazia, corrompida (soma errada) ou sem cabeçalho (legado) | `Subir`, se o local abre. Se nenhum dos dois abre, `Nada`. |
| O aparelho está vazio (instalação nova) ou com save que não abre | `Baixar` |
| Os dois textos são idênticos | `Nada` |
| Os dois abrem e diferem | Vence o **maior progresso** (`Renome.Total` do Império, que nunca cai e já conta os Charters). No empate, vence o **mais novo** (`at=` do envelope): `Baixar` se for o da nuvem, `Subir` se for o local. |

- **"Baixa só se for mais novo"** vale no empate. No conflito, o progresso manda: o jogador nunca perde Renome por ter jogado offline num aparelho velho.
- **O que sobe é o arquivo inteiro** (envelope com SHA-256), nunca um pedaço. Quem baixa grava pelo mesmo caminho do save local (`.bak` antes) e recarrega a cena.

Testes: `CloudSaveTests` (8 casos). O resolver: progresso, empate, nuvem ou aparelho que não abre, build mais novo, flag desligada. O `SincroniaNuvem` com uma nuvem falsa: 1ª vez sobe e depois baixa o de mais progresso; offline e travado nunca lançam nem seguram a abertura (timeout); teto de 1 envio por 60 s, com nova tentativa depois de falhar.

## Como está ligado

- **Pacotes:** `com.unity.services.cloudsave` 3.4.2 e `com.unity.services.authentication` 3.7.4. O Authentication já vinha como dependência do Remote Config; agora está declarado no manifest.
- **`NuvemUgs`** (`View/Ugs.cs`):
  - chave `save` do Player Data, com o envelope inteiro (~2,5 KB; o teto do UGS por chave é bem maior);
  - login **anônimo** (`SignInAnonymouslyAsync`): o jogador do UGS é um ID aleatório deste aparelho, sem conta, e-mail ou nome.
- **`SincroniaNuvem`** (núcleo):
  - nada lança e nada espera mais que 8 s, então sem rede o jogo segue no local e registra `cloud_save,nada,<erro>`;
  - `Abrir` roda 1× por sessão; `Subir` roda depois de cada save gravado, no máximo 1 a cada 60 s.
- **`Game`:**
  - o `AbrirNuvem` roda no `Liberar` (resposta da tela de idade), só com a flag ligada, **só 18+ com aceite** (`Privacidade.Flags(...).Consentimento`; menor de 18 nunca, nem com aceite) e fora do Editor e do `-testsession`;
  - em `Baixar`, grava o texto da nuvem pelo `Salvamento.GravarTexto` (arquivo + espelho) e renasce a cena. O `Game` velho para de gravar (`_headless`).
  - O diário registra `cloud_save,<subir|baixar|nada>,<erro ou vazio>`.
- **Falta no painel, antes de ligar a flag:** ativar o Cloud Save e o Authentication (anônimo) no projeto `305e7176…`, primeiro no ambiente `teste`.

## Rascunho para a privacidade (NÃO publicado; revisar com o Vinicius antes de ligar a flag no `production`)

**`docs/PRIVACIDADE.md` §2 (o que sai do aparelho), item novo:**

> **Progresso salvo na nuvem (só para quem tem 18 anos ou mais e aceitou na tela de idade).** Uma cópia do seu progresso no jogo (o mesmo arquivo de save do aparelho: ouro, melhorias, distritos, Renome e compras já entregues) é guardada nos servidores da Unity (Unity Gaming Services, Cloud Save), ligada a um identificador anônimo criado neste aparelho. Ela serve para recuperar o progresso se você reinstalar o jogo. Não pedimos conta, e-mail nem nome. Para apagar a cópia, peça pelo e-mail de contato da loja; ela também some se você recusar na tela de idade e reinstalar.

**Data Safety da Play (formulário), o que muda:**

| Pergunta | Resposta com o Cloud Save ligado |
|---|---|
| Tipo de dado coletado | **Atividade no app > Outras ações do usuário no app** (o progresso do jogo). Já declarado para o analytics, só acrescentar a finalidade abaixo. |
| Identificadores | **IDs do dispositivo ou outros**: o ID anônimo do jogador do UGS (Authentication). |
| Finalidade | **Funcionalidade do app** (backup e restauração do progresso). |
| Compartilhado com terceiros? | Não. A Unity é processadora (prestadora de serviço), o que a Play não conta como compartilhamento. |
| Opcional? | Sim: só de 18+ com aceite. Quem recusa joga só com o save local. |
| Criptografado em trânsito? | Sim (HTTPS do UGS). |
| O usuário pode pedir exclusão? | Sim, pelo e-mail de contato (exclusão manual no painel do UGS, Player Management). |
