# Roteiro de playtest por marcos (build Windows)

Para a nota **E** (experiência) do placar do `PROJETO.md` §8 (régua S/E, V31). Sem sessão humana, a nota E de cada linha fica em no máximo 50; uma sessão deste roteiro, jogada por quem não fez a feature, é a evidência que a faz subir. O roteiro **não depende do POCO**: o build Windows sai do CI a cada PR.

## 1. O que baixar (uma vez; sem clone, sem PowerShell digitado)
1. Entre no GitHub com a sua conta e abra o repositório > aba **Actions**.
2. **O jogo:** à esquerda, workflow `unity` > o último run verde da `main` > no fim da página, artifact `build-win` (fica 14 dias). Se não houver artifact, clique em **Run workflow** (branch `main`) e volte em ~25 min. Descompacte o zip numa pasta sua; o jogo é o `ForgeStreet.exe`.
3. **Os saves:** workflow `saves-de-marco` > o último run verde > artifact `saves-de-marco` (fica 30 dias). Descompacte. Dentro vêm os `.save.txt`, o `LEIA.md` (a hora do relógio do bot em cada save, distrito, Renome e ouro) e três atalhos de clique duplo:
   - `usar_save.bat`: arraste um `.save.txt` sobre ele;
   - `do_zero.bat`: o jogo abre como na primeira vez;
   - `voltar.bat`: devolve o save que você tinha antes.

## 2. Ordem dos saves
A **ordem mínima são 4 sessões (cerca de 1 h)**. Os outros Charters são opcionais. Cada linha lista o que observar e marcar **SIM/NÃO** durante a sessão (são as 8 queixas de 2026-11-21 no `PROJETO.md` §0).

| ordem | save | o que você vai ver | observe (SIM/NÃO) |
|---|---|---|---|
| **1** | `00` = `do_zero.bat` | o primeiro minuto do jogo | Entendeu o que fazer nos primeiros 60 s sem ler nada? O joystick respondeu como esperado (andar, parar, virar)? |
| **2** | `01_old_forge_completo` | o Old Forge com a produção inteira, antes do Charter | Algum ajudante ou funcionário ficou parado mais de 5 s sem motivo visível? A esteira fez alguma diferença? A estação mudou de cara ao subir de nível? |
| opcional | `NN_charter_<distrito>` | logo depois de cada Royal Charter: o mapa e o próximo distrito | O distrito novo parece diferente do anterior? Os ajudantes parecem saber o que fazem? |
| **3** | `NN_carta_mais_30min` (ou, se não saiu, o último `NN_charter_…`) | o fim da campanha: a Carta da Grande Forja e 30 min depois (Masterworks, Collection, Events, Pass) | As evoluções finais parecem um avanço ou "como chão"? |
| **4** | `NN_tudo_comprado` (ou `fim_40h`) | o fim de jogo: tudo comprado em todos os distritos | Com tudo comprado, há algo que você queira fazer? O que a tela oferece? |

Nota do gerador (run de 2026-11-21, perfil F2PGolpe): o bot compra tudo nos 6 distritos com **21,5 h** de relógio e 5 Charters, **antes** da Carta da Grande Forja. O gerador segue jogando até a Carta + 30 min, então os dois saves saem, e os números seguem a ordem do relógio (no run de 2026-11-21: 07 tudo comprado com 21,5 h, 08 Charter da Grand Forge, 09 Carta + 30 min com 23,6 h). A sessão 3 (09) vem antes da 4 (07) de propósito: a 4 é o vazio do fim, a última coisa a medir. Isso confirma o P1 "fim de jogo morre" do §7: a sessão 4 deve medir esse vazio, não contorná-lo.

Ao abrir um save, o cofre offline pode pagar até 2 h, como para qualquer jogador que volta.

## 3. A sessão (15 min por save)
1. Feche o jogo. Use o atalho do save (seção 1). O atalho se recusa a rodar com o jogo aberto, porque o autosave regravaria o save.
2. Comece a gravar a tela (Win + Alt + R, ou o OBS) antes de abrir o jogo. Um **vídeo de 30 s ou mais sem corte** é parte da evidência.
3. Jogue 15 min sem ajuda e sem flags de teste, marcando os SIM/NÃO da tabela.
4. Responda as **6 perguntas fixas** (texto curto, ou por áudio):
   1. O que você quer fazer agora?
   2. O que você não entendeu?
   3. O que parece inútil?
   4. O que você faria se tivesse mais 1 h?
   5. O que piorou desde a última vez?
   6. **De 0 a 10, quanto você quer continuar jogando?** (o número compara um build com o outro)
5. Anote o commit do build: o número da versão nas Configurações, ou o nome do run do Actions.
6. No fim, `voltar.bat` devolve o seu save.

## 4. Onde fica a evidência
Mande os SIM/NÃO, as respostas e o vídeo na thread do projeto (ou ponha numa pasta `docs/playtest/<data>-<save>/` só com texto; vídeo não entra no repositório, que é público). O coordenador registra no §8: com 1 sessão sem queixa P0/P1 a linha vai a até 70–80; com 2 sessões em builds diferentes, a 90; queixa aberta segura a linha em 40.

## 5. Gerar os saves na mão (opcional)
`dotnet run -c Release --project client/tools/econsim -- --marcos saves --perfis F2PGolpe --horas 40`. Os atalhos ficam em `client/tools/playtest/`. O gerador relê cada save como o jogo leria e sai 1 se algum não abrir igual.
