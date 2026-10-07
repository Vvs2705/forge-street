# Como trabalhamos

- **Branch padrão:** `main` sempre compila e passa nos testes. Trabalho novo em `feat/<assunto>`, `fix/<assunto>`, `docs/<assunto>` ou `art/<assunto>` e entra por **Pull Request**.
- **Commits:** em português, no imperativo, com prefixo de escopo: `feat(fs-core): …`, `fix(fs-view): …`, `art(fs): …`, `docs: …`, `test: …`, `chore: …`. Um assunto por commit.
- **Portão para o PR entrar:**
  1. `dotnet test client/tools/coretests` verde;
  2. `dotnet build client/tools/viewcheck/view` com 0 erros;
  3. Unity EditMode verde e build Windows/Android quando o PR mexe em jogo;
  4. mudança de economia vem com medição do bot em `docs/BALANCE.md` (antes/depois);
  5. fotos em `client/Builds/validation_*/shots/` quando muda o visual.
- **Saves antigos:** enum, estações e pads só se anexam no FIM (o save é por índice).
- **Versão:** SemVer no `PlayerSettings.bundleVersion` (`Editor/Setup.cs`); cada versão vira tag `vX.Y.Z` e Release com notas e APK.
- **Nunca** comitar `.env`, chaves, arte-fonte crua do Tripo/Mixamo, builds ou cache do Unity (ver `.gitignore`).
