# AUTORIZAÇÕES JÁ CONCEDIDAS — base para o time de jogos

> Levantamento só-leitura feito em 2026-11-19. União de tudo que o Vinicius já liberou, proibiu ou mandou perguntar
> nos projetos de jogo e na configuração global. Cada linha cita a origem (`arquivo:linha`).
> **Valor de segredo nenhum foi copiado.** Onde um arquivo tem credencial, a linha diz só "contém segredo, não copiado".

Legenda da coluna **Regra**: `allow` = liberado sem perguntar · `deny` = proibido · `ask` = só com aval explícito do Vinicius no chat.

Caminhos abreviados:
- `MP\` = `C:\Users\VINICIUS\Videos\MEUS PROJETOS\`
- `~\` = `C:\Users\VINICIUS\`
- `f1\` = clone raso em `C:\Users\VINICIUS\AppData\Local\Temp\claude\f1-repos\` (repos `Vvs2705/dungeon-courier` e `Vvs2705/forge-street`)

---

## 0. O que existe (e o que não existe) como configuração de permissão

| Arquivo | Estado | Observação |
|---|---|---|
| `~\.claude\settings.json` | existe, **sem bloco `permissions` e sem `hooks`** | só `outputStyle: "Proactive"` e `agentPushNotifEnabled: true` (linhas 2–3) |
| `~\.claude\settings.local.json` | não existe | — |
| `~\.claude\CLAUDE.md` | não existe nesta máquina | a LICOES (`MP\Agentes\_memoria\LICOES.md:487`) descreve 3 camadas: `~/.claude/CLAUDE.md` + `~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md` + CLAUDE.md do jogo |
| `~\.claude\estudio\REGRAS_JOGOS_ESTUDIO.md` e `PROMPT_MESTRE_NOVOS_JOGOS.md` | **não existem nesta máquina** | importados por `@~/.claude/estudio/...` nos CLAUDE.md do DC (`f1\dungeon-courier\CLAUDE.md:6`), Forge (`f1\forge-street\CLAUDE.md:6`) e Tiny Airport (`MP\JOGOS NOVOS\09_TINY_AIRPORT_EMPIRE\CLAUDE.md:6,9`). Provavelmente estão no outro PC. O "mínimo" de fallback está no próprio CLAUDE.md (ver §2). |
| `MP\.claude\settings.local.json` | existe | 43 regras `allow`, quase todas do projeto Strema/scraping (não são de jogo). Listadas no §5 por completude. |
| `MP\ARKANA\.claude\` | só `launch.json` (config morta `npm run dev`, porta 5173) | sem `settings*.json` |
| `chronicles-of-existence\.claude\`, `dungeon-courier`, `forge-street` | **sem `settings.json`** | COE tem `agents/` e `skills/`; DC e FS não têm `.claude/` no GitHub (`gh api .../contents/.claude` = 404) |
| `MP\Agentes\hooks\settings-hooks.json` | modelo pronto, **NÃO aplicado** (`MP\Agentes\hooks\README.md`, "Instalar (manual: nada foi aplicado)") | ver §4 |
| `MP\ARKANA\arte\tools\meshy\.env` | existe, ignorado pelo git | **contém segredo — não copiado** (citado em `ARKANA_AUDITORIA_TECNICA_ANDROID.md`, §15) |
| `~\.android\debug.keystore`, `adbkey` | existem | **contém segredo — não copiado** (mesma fonte) |

Conclusão prática: quase toda autorização de jogo hoje vive **em texto** (CLAUDE.md, docs, memória), não em `permissions` do Claude Code. O time novo deve transformar a parte estável disso em `settings.json` do projeto.

---

## 1. Git, PR e merge

| Permissão | Regra | Origem |
|---|---|---|
| Commit, push, abrir PR e fazer o merge **sem perguntar**, depois de `client/tools/verify.sh full` verde (Forge) | allow | `f1\forge-street\CLAUDE.md:12` (regra do Vinicius de 2026-10-12); `f1\forge-street\docs\PROJETO.md` §4 V8 |
| Push, PR e merge **sem perguntar** ao fechar bloco verificado (COE) | allow | `MP\chronicles-of-existence\CLAUDE.md:51` (2026-10-02) |
| Push direto na `main` (repos com proteção "só por PR": Forge, DC, Tiny Airport) | deny | `f1\forge-street\CLAUDE.md:20`; fallback mínimo em `f1\dungeon-courier\CLAUDE.md:8` |
| Mudança entra por PR na `main`, uma branch e um PR por assunto (`feat/`, `fix/`, `docs/`, `art/`) | allow (fluxo padrão) | `f1\forge-street\CONTRIBUTING.md:3`; `f1\dungeon-courier\docs\LICOES_PARA_O_ESTUDIO.md` §1.2 |
| ARKANA: trabalho entra em `main` só por `git merge --ff-only`; se divergiu, **parar e perguntar** | allow ff-only / ask se divergir | `MP\ARKANA\CLAUDE.md:13-14` |
| `git push --force`, `git filter-branch`, `git reflog expire`, `git gc --prune`, apagar `.git` | deny | `MP\ARKANA\CLAUDE.md:17-18` |
| Apagar branch/worktree só depois de `git merge-base --is-ancestor` provar que está em `main` | allow com prova | `MP\ARKANA\CLAUDE.md:15-16` |
| Criar pasta nova no repo (ARKANA) | deny salvo necessidade real | `MP\ARKANA\CLAUDE.md:5-6` |
| Worktrees aninhados `.claude/worktrees/...` (ARKANA) | deny | `MP\ARKANA\CLAUDE.md:7-9` (na prática existem 1 worktree e 1 zip lá — ver PRATICAS §Falhas) |
| Tags, GitHub Releases | ask | `f1\forge-street\CLAUDE.md:12`; `docs\PROJETO.md` V8 |
| `gh pr merge` — o classificador do Claude Code bloqueou como "Merge Without Review" | ask (ou regra de permissão explícita do Vinicius) | `MP\Agentes\_memoria\LICOES.md:431,439` (lição 2026-10-08) |
| `git push --dry-run` em repo com LFS | deny (sobe objetos LFS de verdade) | `MP\Agentes\_memoria\LICOES.md` entrada 2026-09-23 (esteira Android) |
| `git stash` sem nome em worktrees compartilhados | deny | `MP\Agentes\TIME DE JOGOS\COE_Estudio_Multiagentes_v1_0\01_PROTOCOLO_ORQUESTRACAO.md` §6 |
| `git checkout HEAD -- <pasta>` amplo para limpar temporário | deny | LICOES entrada 2026-10-01 (git sem diff falso) |
| `Bash(git checkout *)` | allow | `MP\.claude\settings.local.json:46` |
| Visibilidade de repositório novo | ask | LICOES entrada 2026-10-09 (gdd-competitivo: "perguntar visibilidade antes de criar repo") |

## 2. Arquivos, limpeza e segredos

| Permissão | Regra | Origem |
|---|---|---|
| Apagar regeneráveis (`.next`, `dist`, `build`, `out`, `target`, `coverage`, `__pycache__`, `.pytest_cache`, `.turbo`, `*.tsbuildinfo`, `*.log`, `*.tmp`) | allow | `MP\ARKANA\CLAUDE.md:21-23` |
| Apagar arquivo de trabalho (mesmo não rastreado) sem commit antes | deny | `MP\ARKANA\CLAUDE.md:24` |
| Limpeza: só o que `git status --ignored` mostra como `!!`; nunca a pasta-mãe de arquivo versionado | allow / deny | LICOES entrada 2026-09-29 (higienização COE) |
| Apagar por curinga (`L*`) | deny — só nome exato | LICOES entrada 2026-11-06 |
| `docker builder prune -af` (limpeza global) | deny — filtrar por label | LICOES entrada 2026-09-29 |
| Ler `.env` / segredos sem ordem explícita | deny | `...\AGENTS\16_ai-unity-gameplay-engineer.md` frontmatter `nao_fazer`; protocolo §6 "Sem secrets" |
| Commitar `.env`, chaves, keystore, arte crua do Tripo/Mixamo, builds, cache do Unity | deny | `f1\forge-street\CONTRIBUTING.md` ("Nunca comitar..."); `MP\ARKANA\.gitignore` (descrito em `ARKANA_AUDITORIA_TECNICA_ANDROID.md` §18) |
| Chave de upload real: guardar fora do git (`%USERPROFILE%/.<jogo>/`) com backup fora do PC | allow (o caminho) / ask (gerar ou usar chave real) | `f1\dungeon-courier\docs\LICOES_PARA_O_ESTUDIO.md:172`; `f1\forge-street\CLAUDE.md:12` |
| Release real sem as variáveis `COE_KEYSTORE*` | deny → `BLOCKED_CREDENTIAL`, não inventar chave | `MP\chronicles-of-existence\.claude\skills\release-check\SKILL.md` passo 1 |
| Commitar binário 3D/textura pesada fora do Git LFS | deny | `MP\ARKANA\swarm.yaml:8` |

## 3. Downloads, contas, créditos pagos e lojas

| Permissão | Regra | Origem |
|---|---|---|
| Download "sempre permitido, em qualquer quantidade" de gerações da conta do Vinicius (Tripo, Mixamo), referências, pacotes e docs de fonte conhecida — **não** cobre fonte não confiável nem executar o baixado | allow (COE) | `MP\chronicles-of-existence\CLAUDE.md:52` (2026-10-03) |
| Download externo (Forge) | ask | `f1\forge-street\CLAUDE.md:12` |
| Baixar no Mixamo com o login Adobe do Vinicius (ARKANA) | ask — "ato do Diretor" (`BLOCKED_MIXAMO_DOWNLOAD`); em 04/10 os 77 clipes foram baixados com a conta dele | `MP\ARKANA\design\PROJETO.md:132,174,335`; bloco 13 (77 clipes) `design\PROJETO.md:80` |
| Gerar no Tripo (gasta crédito) | allow **por lote autorizado** (COE: `docs/arte/PROMPTS_G2.md`, "Quem gera"); Forge: crédito de IA só com aval | `MP\chronicles-of-existence\CLAUDE.md:53`; `f1\forge-street\CLAUDE.md:12-13` |
| Tripo pela **API** | deny (decisão do Diretor 30/09: "sem API", criar no site) | `MP\ARKANA\design\cenario\TRIPO-STUDIO.md:4`; `design\PROJETO.md:238` |
| Meshy: criar **personagem** pela API | deny (o script recusa; rig da API anima como robô) | `MP\ARKANA\arte\00-LEIA.md` ("`tools/meshy/meshy.py` RECUSA criar personagem pela API") |
| Conferir saldo de crédito antes e depois de cada geração e registrar em `PROVENIENCIA.md` | obrigatório | `f1\dungeon-courier\docs\LICOES_PARA_O_ESTUDIO.md:28` |
| Gerar malha de personagem/criatura/local/item-assinatura/VFX sem os portões G1 ficha → G2 concept → G3 licença | deny | `MP\chronicles-of-existence\CLAUDE.md:61` (ADR-0002) |
| Criar conta, aceitar termos (Mixamo, Play, Roblox), pagamento | ask/deny — ato do Vinicius | `f1\forge-street\CLAUDE.md:12`; `MP\Agentes\_memoria\arkana.md:42` (login/publicação no Roblox nunca por agente); `release-check\SKILL.md` ("nunca cria conta, nunca aceita termos da Play") |
| Publicar em loja (Play Console) | ask | `f1\forge-street\CLAUDE.md:12`; `release-check\SKILL.md` ("Nunca publica") |
| Instalar pacote novo no Unity | ask ("sem pacote novo sem pedir; sem SDK/ferramenta sem fase ativa") | `MP\ARKANA\ARKANA_AUDITORIA_TECNICA_ANDROID.md:308` (cita `mobile-unity/ARQUITETURA.md` §5) |
| Pacote no `manifest.json` sem uso no mesmo commit | deny | `MP\chronicles-of-existence\CLAUDE.md:10` |
| Instalador elevado (UAC) | não chega ao agente → vira `.bat` para o Vinicius rodar | LICOES entrada 2026-09-08 |
| `Desktop/FORGE_LIBERAR_DEV.bat` libera ferramentas de dev | existe, rodado pelo Vinicius | LICOES entrada 2026-10-08 (Forge leva 14/15) |

## 4. Aparelho, navegador, MCP e hooks

| Permissão | Regra | Origem |
|---|---|---|
| Usar o celular do Vinicius (Poco F4) por adb | ask — perguntar "pode testar" antes; foto só com o jogo em primeiro plano | `f1\dungeon-courier\docs\LICOES_PARA_O_ESTUDIO.md:188` |
| Rodada de medição "sem dedo" no aparelho | ask — avisar antes; separada da rodada em que ele joga | LICOES entrada 2026-09-24 |
| "Sem pedir teste no celular", navegador, Remote Control (autorização permanente do prompt mestre de jogos novos) | allow **declarado**, mas o PR que o versionava foi fechado sem merge | `gh pr view 19 -R Vvs2705/dungeon-courier` (CLOSED); arquivo alvo `~/.claude/estudio/PROMPT_MESTRE_NOVOS_JOGOS.md` não existe aqui |
| Claude in Chrome com a sessão logada do Vinicius (Tripo, Mixamo, LevelPlay) | allow — usado em várias levas | LICOES entradas 2026-10-06, 10-07, 10-08 |
| JS para clicar botão em fluxo de pagamento | deny (classificador bloqueia) | LICOES entrada 2026-10-22 |
| **MCP Blender** (`mcp-for-blender`, escopo de usuário, `BLENDER_MCP_SAFE_MODE=1`, porta 9876 só localhost); `execute_blender_code` nunca no master, sempre em cópia | allow com restrição | `MP\ARKANA\arte\00-LEIA.md:45-52` |
| **MCP Unity** (`com.coplaydev.unity-mcp` v10.2.0 no manifest do ARKANA): manter só o transporte HTTP, desligar AutoRegister | allow com restrição | `MP\ARKANA\mobile-unity\Packages\manifest.json:3`; LICOES entrada 2026-09-25 |
| `com.coplaydev.unity-mcp` dentro do APK | ask — "decisão dele tirar do build" | `MP\ARKANA\design\PROJETO.md` bloco 12, Dívida |
| Tripo DCC Bridge (Blender 60600 / Unity 60610): um programa por vez; só envia com clique humano | allow (uso), envio = humano | `MP\ARKANA\design\cenario\TRIPO-STUDIO.md` "Pontes"; `MP\JOGOS NOVOS\00_PESQUISA\PIPELINE_ARTE_LICOES.md` §0 |
| Hooks V-STACK: `proteger-segredos` (bloqueia ler/escrever `.env*`, `*.pem`, `*.key`, chaves SSH), `comandos-perigosos` (bloqueia `rm -rf /`, force-push para main/master, `--no-verify`), `proteger-config`, `formatar`, `avisar-debug`, `verificar-ao-parar` (opt-in) | deny (quando aplicados) — **modelo NÃO aplicado** no `~\.claude\settings.json` atual | `MP\Agentes\hooks\settings-hooks.json:3-80`; `MP\Agentes\hooks\README.md`; LICOES:496 relata que já valeram em sessão de 2026-10-17 (provavelmente outro PC) |
| Unity: uma instância por projeto; só o coordenador/Integrator roda Unity e batch mode | deny para workers | `01_PROTOCOLO_ORQUESTRACAO.md` §6; `03_PROMPT_COORDENADOR_MESTRE.md` regra 13 |
| Fechar Unity/Editor de outra sessão | deny — ler a linha de comando de cada `Unity.exe` e esperar build alheio | LICOES entrada 2026-09-30 (leitura COE) |
| Um processo pesado por vez (Unity OU Blender OU emulador) | obrigatório | `f1\dungeon-courier\CLAUDE.md:8`; `LICOES_PARA_O_ESTUDIO.md` §1.4 |

## 5. Produto e monetização (vetos do Vinicius)

| Permissão | Regra | Origem |
|---|---|---|
| Item aleatório pago / loot box / caixa paga | deny | `MP\ARKANA\design\gdd\GDD.md` §19.1 item 4; `f1\forge-street\CLAUDE.md:13`; DC fallback `CLAUDE.md:8` |
| Vender poder (arma, runa, cooldown, loot, reroll, boost, acesso antecipado a mago) | deny — "nunca será pay-to-win" | `MP\ARKANA\design\gdd\GDD.md` §19.1 (linhas ~608-620) e §7 |
| Anúncio com `AD_ID` / personalizado | deny | DC `CLAUDE.md:8`; `LICOES_PARA_O_ESTUDIO.md:153` |
| Interstitial ligado por padrão | deny — atrás de flag desligada até A/B | `f1\forge-street\CLAUDE.md:13`; `docs\PROJETO.md` V10 |
| Criativo que não seja gameplay real | deny | `LICOES_PARA_O_ESTUDIO.md` §8.4 |
| IA generativa em runtime com autoridade sobre estado | deny | `MP\chronicles-of-existence\CLAUDE.md:57` |
| Propor Unreal, Steam ou Godot | deny | `MP\Agentes\_memoria\arkana.md:15`; `ARKANA\README.md` |
| Alterar decisão de GDD em silêncio | deny — vira proposta em `docs/` | `MP\chronicles-of-existence\CLAUDE.md` "O que NÃO fazer" |
| Autenticação, dados de usuário, pagamentos ou deploy | ask ("sempre confirmar antes de executar") | `~\AGENTS.md` "Regra de ouro" |

## 6. Regras `allow` do `MP\.claude\settings.local.json` (Strema/scraping, não são de jogo)

Todas `allow`, linhas 4–46. Agrupadas:

| Padrão | Linhas |
|---|---|
| `WebSearch` | 4 |
| `WebFetch(domain:gestaoink.site)` | 41 |
| `Bash(copy "...ARQUITETURA_STREMA_MVP.md" ...)`, `Bash(Get-ChildItem -Recurse "...Strema\frontend")`, `Bash(Select-Object FullName)`, `Bash(Out-Null)`, `Bash(Write-Output "Diretorios criados.")` | 5-7, 9, 23 |
| `Bash(New-Item -ItemType Directory -Force -Path "...Strema\backend\...")` (14 pastas de Clean Architecture) | 8, 10-22 |
| `Bash(pip --version)`, `Bash(pip install *)`, `Bash(scrapling install *)` | 24-26 |
| `Bash(python analisar.py)`, `crawler.py`, `inspecionar_login.py`, `crawler_auth.py ...`, `debug_login.py ...`, `crawler_spa.py ...`, `crawler_rotas.py ...`, `crawler_sessao.py`, `screenshots_responsivo.py`, `seo_copy_audit.py` | 27-29, 33-40, 43-44 |
| `Bash(python -c ' *)`, `Bash(python -c "...")` (2) | 31-32, 45 |
| `Bash(cd ".../skills/scrapling" && ls references/)`, `Bash(ls ".../memory/" ...)` | 30, 42 |
| `Bash(git checkout *)` | 46 |

`deny` e `ask`: nenhum bloco em nenhum `settings*.json` encontrado.

## 7. Conflitos entre fontes (decidir antes de virar settings)

1. **Download/Mixamo:** COE diz "sempre permitido" (10-03); ARKANA trata como "ato do Diretor" (04/10); Forge pede aval. Sugestão: `allow` para fonte conhecida + conta do Vinicius, `ask` para fonte nova.
2. **Merge:** COE e Forge liberam merge sem perguntar; o classificador bloqueou `gh pr merge` uma vez; ARKANA usa ff-only local. Sugestão: regra explícita `Bash(gh pr merge *)` só depois do portão verde.
3. **Worktrees:** ARKANA proíbe worktree aninhado; Forge/DC/COE usam worktree por raia (em `%TEMP%\wt\<curto>`). Sugestão: worktree fora do repo, caminho curto, nunca em `.claude/worktrees`.
4. **Teste no celular:** DC manda perguntar; o prompt mestre (não versionado aqui) diz "sem pedir". Manter `ask` até o Vinicius confirmar.
