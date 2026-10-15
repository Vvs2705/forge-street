# Brief comum — Leva dos distritos 3 e 4 (C-CORE-06/07), 2026-10-15

Você é UMA raia de núcleo do Forge Street (tycoon de crafting em Unity; núcleo `FS.Core` em C# puro, testável com `dotnet`).
Coordenador: a sessão principal. Português brasileiro em código, commits e docs. Leia ANTES de codar:

1. `CLAUDE.md` (regras do estúdio e guardrails: sem sorteio, nada aleatório pago, só ouro até a Wave E, offline teto 2 h).
2. `docs/PROJETO.md` §4 (V22–V29) e §9 ("Como o coordenador trabalha").
3. `docs/CONTEUDO_V1.md`: §0, §1 (curva), §2 (regras comuns + a seção do SEU distrito), §3/§3.1 (itens e receitas, linhas do seu distrito), §4 (estações), §5 (o upgrade do seu distrito + §5.7 níveis), §6 (workers), §7 (encomendas), §8 (contratos), §9 (Blueprints), §10 (Masterworks e Collection), §11 (Renome), §12 (clientes), §14–§16 (testes negativos e portões).
4. O padrão da Rua dos Armeiros, que você vai repetir: commits `5da9e73` (#61, distritos e Império), `a8e693b` (#66, Row afinada), `c701da0` (#71, liga) — `git show --stat <c>` e leia o diff do `Distritos.cs`, `EncomendasDefs.cs`, `ProjetosDefs.cs`, `MasterworksDefs.cs`, `ContratosDefs.cs`, `Imperio.cs`, `ContentValidator.cs`, `Equipe.cs`, `econsim/Program.cs`, `View/Loc.cs`, `View/Textos.cs`, testes `RowTests.cs`/`LigaTests.cs`, `mutantes.py`. E `docs/BALANCE.md` §31, §33, §34, §35 (formato das medições que você vai escrever).

## Base e branch

- Sua worktree deve conter o commit `59e401d` ("Sim, Station e Bot como classes parciais", branch local `chore/sim-parcial`). Confira: `git merge-base --is-ancestor chore/sim-parcial HEAD || git merge chore/sim-parcial`.
- Crie seu branch: `git switch -c <seu branch>` (nome no seu prompt). Commits pequenos, em português com escopo (`feat(fs-core): …`, `feat(econsim): …`, `test: …`, `docs: …`), terminando com a linha `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. **Sem push, sem PR, sem CHANGELOG, sem mexer no `docs/PROJETO.md`** (o coordenador integra).
- Antes das mutações e das medições finais: `git fetch -q origin && git merge origin/main` (a `main` pode mudar no meio da raia; a outra raia pode entrar antes de você).

## Ambiente (Windows, Git Bash, PC de 7,7 GB e 16 núcleos)

- Só `dotnet` (sem Unity, sem emulador, sem Blender). A outra raia roda em paralelo: no máximo um `dotnet test`/`econsim` pesado seu por vez.
- Copie as 3 DLLs para a sua worktree antes do 1º `verify.sh quick`: `mkdir -p client/Library/ScriptAssemblies && cp /c/fsv/client/Library/ScriptAssemblies/{Unity.InputSystem,UnityEngine.UI,Unity.LevelPlay}.dll client/Library/ScriptAssemblies/` (a pasta `client/Library` é ignorada pelo git).
- Portão da raia: `client/tools/verify.sh quick` verde (núcleo + viewcheck PC e Android). O coordenador roda o `full` (Unity) na integração.
- Se Edit/Write recusarem um caminho, edite por Bash/python (heredoc).
- **NUnit 3.5 do Unity**: o `dotnet test` (NUnit 3.14) aceita o que o Unity reprova. Em `Assert.AreEqual` use tipos exatos dos dois lados (`5L` com `long`, `5f` com `float`, nunca tupla `(int,long)` × `(int,int)`); nada de APIs do NUnit 3.6+ (`Assert.Multiple`, `Is.EqualTo(...).Within` com tupla etc.).
- Todo `.cs` novo em `client/Assets` precisa do `.meta` (2 linhas: `fileFormatVersion: 2` e `guid: <32 hex novos>`; gere com `python -c "import uuid;print(uuid.uuid4().hex)"`).

## O que é seu (para não colidir com a outra raia)

- **Arquivos próprios** (crie): os dados do distrito num arquivo só ou poucos (ex.: `Core/Distritos.<Nome>.cs` com `static partial class Distritos`, e no mesmo arquivo ou ao lado `static partial class OrderDefs` / `ProjetosDefs` / `MasterworksDefs` / `ContratosDefs` / `WorkerDefs` — todas já são `partial`); a mecânica nova em `Core/Sim.<Mecanica>.cs` (`public sealed partial class Sim`, e `partial class Station` se precisar de campos), o bot da mecânica em `Core/Bot.<Mecanica>.cs`; testes em `Tests/EditMode/<Nome>Tests.cs`.
- **Arquivos compartilhados** (a outra raia também mexe; edição mínima e local, num bloco contíguo com comentário de cabeçalho `// ---- <distrito_id> (C-CORE-0X)`): `Distritos.All`, `OrderDefs.Distritos`, `ProjetosDefs.Distritos`, `Imperio.Managers` (o manager do §6), enum `Kind` em `Defs.cs` (se a mecânica pedir um tipo novo de estação), `ContentValidator.cs` (regras da mecânica), `Sim.cs` (só os GANCHOS de 1–3 linhas que chamam o seu arquivo), `Equipe.cs`, `View/Loc.cs` e `View/Textos.cs` (textos pt/en/es do distrito num bloco só), `View/Game.cs`/`WorldView.cs`/`Captura.cs` (UI mínima da mecânica), `econsim/Program.cs`, `mutantes.py` (seu bloco no fim da lista), `docs/BALANCE.md` (SUA seção, número no prompt).

## O que entregar (o padrão da Row, num branch só)

1. **Dados do distrito** (CONTEUDO_V1): `DistrictDef` com itens, receitas, estações e pads (layout do §2.x; colunas de 3 m, fileiras de baixo para cima = entrada → saída como a Row; rótulos sem encostar — há o teste `Layout_…` da Row para copiar), os 18 produtivos do §5.x + 3 luxos + níveis de estação do §5.7, workers do §6 (turma de carregadores como a Ferraz + especialistas), manager do distrito em `Imperio.Managers`, `Abertura`, `Charter`, `FundoFator`, `Ajuste` (preço, intervalo de cliente, custo, vagas e estoque do balcão).
2. **A mecânica nova** do §2.x do seu distrito, determinística (V7), em arquivo próprio, com contadores do catálogo e linhas de save **opcionais** (só gravadas quando diferentes do padrão; save antigo = padrão; teste de ida e volta). Bot com a regra fixa (a do catálogo ou a sua, documentada).
3. **Encomendas** do distrito (§7, ciclo §7.1, freguês com nome §12), **projetos** (§9, essenciais com fonte grátis permanente), **obras-primas** (§10.1, as 10, Superior alcançável §10.2 — o validator confere) e **marcos da Collection** (§10.3), **contratos** do distrito (§8: abrem com o distrito no build, V28), textos pt/en/es (≤ 16 caracteres onde o `LocTests` cobra).
4. **econsim** `--distrito <id>` medindo o distrito como o jogador chega (o Império com os anteriores no Charter) e o que a mecânica pede.
5. **Portões** (CONTEUDO_V1 §16 e BALANCE §35.3): produção completa do F2P 55–70 min; maior sequência morta do F2P ≤ 15 min; renda nos 10 min depois da produção ±25% da renda alvo do §1; ouro de encomendas 4–12% da venda (chegando com o Renome do Império: `--renome` adequado); Charter do distrito alcançável pelo Ideal dentro da coluna "Charter (h no distrito)" do §1 (rodadas de 300–420 min) e provado por teste do caminho real (como `LigaTests.Charter_DaRow_…`); o portão específico da mecânica. Se um número do catálogo não fecha o portão, ajuste pelos knobs do `Ajuste` (preço × k, custo × k, como a V28) e registre o desvio.
6. **Old Forge e Rua dos Armeiros idênticos ao byte**: extraia a `main` com `git archive origin/main client/Assets/_FS/Scripts/Core client/tools/econsim` num diretório temporário (no seu scratchpad), rode lá e aqui `--perfis F2P --minutos 60 --partidas 8 --anuncio-seg 30`, `--perfis Ideal,F2P,Slow,AdWatcher,AdWatcherE --minutos 60 --partidas 2 --anuncio-seg 30` e `--distrito armorers_row --perfis Ideal,F2P,Slow,AdWatcher --minutos 150 --partidas 2 --anuncio-seg 30 --gerente 0`, e compare os CSVs com `cmp` (mova cada `econsim.csv` para um nome próprio). Qualquer diferença tem de ser explicada (o ideal: nenhuma).
7. **Testes** no `coretests` (um arquivo seu; os negativos do §14 da sua mecânica; o Charter pelo caminho real; validator com defeitos injetados; textos nos 3 idiomas) e **prova vermelha**: mutações versionadas no `mutantes.py` com o prefixo do seu prompt, `python client/tools/mutantes/mutantes.py --so <prefixo>` com **todas mortas**.
8. **Docs**: a SUA seção do `docs/BALANCE.md` no formato da §35 (Vigente com a lista de arquivos; decisões e desvios numerados "revisar"; tabelas antes → depois; portões; testes e prova vermelha; Old Forge e Row ao byte com os SHA-256). Atualize o `CONTEUDO_V1.md` só se mudar um número do catálogo (marque "medido").
9. **View**: a UI mínima da mecânica (toque na estação abre a escolha; ou botões no painel), compilando no viewcheck; arte emprestada de folhas existentes pelo campo `arte:` do `StationDef` (ponytail, como a Row); flags de foto no `Captura.cs` (`-distrito <id> -testsession` + o que a sua mecânica precisar) para o coordenador fotografar.

## Relatório final (≤ 60 linhas)

Branch e commits; o que entrou; números (produção completa, renda, zona morta, encomendas, Charter, portão da mecânica) antes → depois; Old Forge/Row ao byte (sim/não, SHA); testes (quantos) e mutações (N mortas / N); desvios do catálogo e decisões por padrão; o que ficou de fora e por quê; arquivos compartilhados que você tocou (para o coordenador resolver conflitos).
