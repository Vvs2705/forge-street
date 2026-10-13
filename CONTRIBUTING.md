# Como trabalhamos

- **Branch padrão:** `main` sempre compila e passa nos testes. Trabalho novo em `feat/<assunto>`, `fix/<assunto>`, `docs/<assunto>` ou `art/<assunto>` e entra por **Pull Request**.
- **Commits:** em português, no imperativo, com prefixo de escopo: `feat(fs-core): …`, `fix(fs-view): …`, `art(fs): …`, `docs: …`, `test: …`, `chore: …`. Um assunto por commit.
- **Portão para o PR entrar:**
  1. `dotnet test client/tools/coretests` verde;
  2. `dotnet build client/tools/viewcheck/view` com 0 erros;
  3. Unity EditMode verde e build Windows/Android quando o PR mexe em jogo;
  4. mudança de economia vem com medição do bot em `docs/BALANCE.md` (antes/depois);
  5. fotos em `client/Builds/validation_*/shots/` quando muda o visual.
- **Saves antigos:** melhorias vão no save por ID estável (`ups=`, schema 2; nunca renomeie um ID — o golden do `ContentValidatorTests` falha). Itens vão por ID desde o schema 3 (`stocks=`/`solds=`/`holds=`/`wks=`; nunca renomeie um ID de item); estações (`stN=`) e baús (`ms=`) ainda vão por índice: esses só se anexam no FIM.
- **Versão:** SemVer no `PlayerSettings.bundleVersion` (`Editor/Setup.cs`); cada versão vira tag `vX.Y.Z` e Release com notas e APK.
- **Nunca** comitar `.env`, chaves, arte-fonte crua do Tripo/Mixamo, builds ou cache do Unity (ver `.gitignore`).
