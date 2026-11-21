# Roteiro de playtest por marcos (build Windows)

Para a nota **E** (experiência) do placar do `PROJETO.md` §8 (régua S/E, V31). Sem sessão humana, a nota E de cada linha fica em no máximo 50; uma sessão deste roteiro, jogada por quem não fez a feature, é a evidência que a faz subir. O roteiro **não depende do POCO**: o build Windows sai do CI a cada PR.

## 1. O que baixar (GitHub, aba Actions)
1. **O jogo:** workflow `unity`, último run verde da `main` → artifact `build-win` (guardado 3 dias). Descompacte; o jogo é o `ForgeStreet.exe`.
2. **Os saves:** workflow `saves-de-marco`, último run verde da `main` (ou "Run workflow" para gerar agora) → artifact `saves-de-marco` (guardado 30 dias). O `LEIA.md` dentro dele diz em que hora do relógio do bot cada save foi gravado, o distrito ativo, o Renome e o ouro.

| save | o que você vai ver | itens do placar que ele mede |
|---|---|---|
| `01_old_forge_completo` | o Old Forge com a produção inteira, antes do Charter | Core, Workers, Quality, Orders, Arte, Áudio |
| `NN_charter_<distrito>` | logo depois de cada Royal Charter: o mapa e o próximo distrito por abrir | 6 distritos, Reputation, Royal Contracts, Blueprints |
| `NN_carta_mais_30min` | 30 min depois da Carta da Grande Forja | Masterworks, Collection, Events, Pass |
| `NN_tudo_comprado` (ou `fim_40h`) | o fim de jogo: tudo comprado em todos os distritos | o P1 "fim de jogo morre" do §7 |

## 2. Como trocar o save
Feche o jogo. No PowerShell, na pasta do repositório:

```
powershell -ExecutionPolicy Bypass -File client\tools\usar_save_marco.ps1 "<pasta do artifact>\01_old_forge_completo.save.txt"
```

O script guarda o seu save atual (nada é apagado) e põe o de marco no lugar (`%USERPROFILE%\AppData\LocalLow\V-STACK\Forge Street\save.txt`). Para voltar ao seu: `usar_save_marco.ps1 -Voltar`. Ao abrir, o cofre offline pode pagar até 2 h, como para qualquer jogador que volta.

## 3. A sessão (15 min por save)
1. Comece a gravar a tela (Win + Alt + R, ou o OBS) antes de abrir o jogo: **vídeo de 30 s ou mais sem corte** é parte da evidência.
2. Jogue 15 min sem ajuda e sem flags de teste.
3. Responda as **5 perguntas fixas** (texto curto, pode ser por áudio):
   1. O que você quer fazer agora?
   2. O que você não entendeu?
   3. O que parece inútil?
   4. O que você faria se tivesse mais 1 h?
   5. O que piorou desde a última vez?
4. Anote o commit do build (o número da versão nas Configurações, ou o nome do run do Actions).

## 4. Onde fica a evidência
Mande as respostas e o vídeo na thread do projeto (ou ponha numa pasta `docs/playtest/<data>-<save>/` só com texto; vídeo não entra no repositório, que é público). O coordenador registra no §8: com 1 sessão sem queixa P0/P1 a linha vai a até 70–80; com 2 sessões em builds diferentes, a 90; queixa aberta segura a linha em 40.

## 5. Gerar os saves na mão (opcional)
`dotnet run -c Release --project client/tools/econsim -- --marcos saves --perfis F2PGolpe --horas 40`. O gerador relê cada save como o jogo leria e sai 1 se algum não abrir igual.
