# Cloud Save (UGS)

Estado (2026-10-24): **o resolver e os testes existem; o SDK ainda não está ligado.** O jogo segue só com o save local (`SaveStore` + espelho). A flag `Tuning.CloudSave` é `false` por padrão.

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

Testes: `CloudSaveTests` (5 casos: progresso, empate, nuvem ou aparelho que não abre, build mais novo, flag desligada).

## Para ligar (próximo ticket)

1. **Pacotes:** `com.unity.services.cloudsave` e `com.unity.services.authentication`. O Cloud Save exige um jogador: login anônimo (`SignInAnonymouslyAsync`). Login pela Play Games só se o Vinicius pedir a troca de aparelho com conta.
2. **Painel:** ativar o Cloud Save no projeto `305e7176…`, no ambiente `teste` primeiro.
3. **`View/CloudSaveUgs.cs`** (só com `Tuning.CloudSave` ligada e depois da tela de idade, como o analytics):
   - lê a chave `save` (Player Data);
   - chama `CloudSave.Resolver(local, remoto)`;
   - em `Subir`, grava o texto em `save` (`SaveAsync`) no `Save` do jogo, com teto de 1 a cada 60 s e na pausa do app;
   - em `Baixar`, chama `SaveStore.Gravar(remoto)` e renasce a cena.
4. **Privacidade:** o save vai a um servidor, então atualizar a `docs/PRIVACIDADE.md` (§2) e o Data Safety da Play antes de ligar a flag no `production`.
