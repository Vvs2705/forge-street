---
name: asset-ia
description: "Pipeline ponta a ponta de asset 3D para o Unity 6 mobile: escolher fonte (bpy custo zero, CC0, Tripo3D com 2 contas + ledger, Higgsfield só manual), quarentena, limpeza/decimate/LOD0-2 em Blender batch, orçamento por prefixo, proveniência/licença, import no Unity e promoção. Gatilhos: '/asset-ia', 'gerar asset', 'modelo 3D com IA', 'Tripo', 'processar modelo', 'fazer LOD', 'decimar', 'asset pesado', 'importar FBX/GLB', 'asset para o ARKANA'."
argument-hint: "<asset_id com prefixo hero_/char_/npc_/wpn_/prop_/env_/vfx_/ui_> [--fonte bpy|cc0|tripo] [--in <arquivo>] [--tris <alvo LOD0>] [--altura <m>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Agent, Bash(python *), Bash(blender *), Bash(*blender.exe*), Bash(git *)
model: sonnet
---
# /asset-ia — do pedido ao prefab medido

Dono: agente `ai-asset-pipeline-engineer` (leia `.claude/agents/ai-asset-pipeline-engineer.md` para as regras completas).
Evidência do batch testado: `production/qa/evidence/2026-11-19-blender-batch-test.md`; pipeline Tripo→Blender→Unity validado:
`production/qa/evidence/2026-11-19-asset-tripo-pipeline.md`.

Sem argumento: pegue o último pedido de asset da conversa. Sem `asset_id` com prefixo válido → proponha um (ex.: `prop_barril_runico`)
e siga; nome sem prefixo cai no orçamento `default` do script e é aviso no relatório.

## Regras que não se negociam
- **Créditos**: Tripo só dentro do lote aprovado na ata/raia; conta **A até zerar, depois B**; cada gasto registrado no mesmo turno:
  `python .claude/scripts/tj_tripo_ledger.py add --conta <A|B> --creditos <n|?> --task <id> --asset <asset_id> --nota "<resumo>"`.
  Fora do lote ou contas zeradas → parar e perguntar ao usuário (dinheiro).
- **Credenciais**: login do Tripo/Higgsfield é do usuário, no navegador/add-on. Nunca pedir, ler, gravar ou ecoar.
- **Higgsfield**: só manual, com confirmação explícita do usuário por uso; nunca em loop.
- **Quarentena**: nada gerado por IA entra em `Assets/` antes do passo 7.
- **Hardware**: 1 Unity Editor por vez; 1 Blender batch pesado por vez.

## Passos

### 1. Contrato do asset
Escreva em `production/assets/quarantine/<asset_id>/CONTRATO.md`: uso, alvo de tris do LOD0 (≤ teto do prefixo em
`tj_asset_budget.py`), altura (m), pivô (`base`/`center`/`keep`), materiais (≤ 2), textura máx., estilo (link do style guide).

### 2. Fonte (a mais barata que atende)
| Fonte | Quando | Ação |
|---|---|---|
| `bpy` | forma simples (caixa, barril, rocha, módulo) | script procedural em `tools/blender/` → GLB em `source/` |
| `cc0` | Poly Haven ou similar CC0 | baixar para `source/`, salvar URL + licença no `provenance.json` |
| `tripo` | personagem, criatura, arma detalhada | plano de créditos → gerar pelo **site** `studio.tripo3d.ai` (Chrome já logado pelo usuário; não pela API) → baixar para `source/` → ledger |

### 3. Proveniência
Crie `production/assets/quarantine/<asset_id>/provenance.json` com: `asset_id`, `ferramenta`, `conta` (A/B ou `-`), `task_id`,
`data`, `prompt_ou_referencia`, `plano_e_termos` (plano da conta + data dos termos consultados), `licenca`
(`propria`|`CC0`|`CC-BY-4.0`|`a_confirmar`), `creditos`, `contribuicao_humana`, `sha256_fonte`, `aprovacoes: []`.
Termos não confirmados → `licenca: "a_confirmar"` (bloqueia o passo 8).

### 4. Inspeção da fonte
```
python .claude/scripts/tj_asset_budget.py production/assets/quarantine/<asset_id>/source --perfil mobile
```
FAIL na fonte bruta de IA é esperado. `NAO_AVALIADO` em `.fbx` → seguir para o batch, que mede.

### 5. Batch Blender (limpeza + LOD + export)
```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup -P tools/blender/tj_process_asset.py -- ^
  --in production/assets/quarantine/<asset_id>/source/<arquivo> ^
  --out production/assets/quarantine/<asset_id>/work ^
  --target-tris <alvo> --name <asset_id> --height <m> --pivot base [--max-tex 1024]
```
`--max-tex` (padrão 1024) reduz texturas maiores antes do export (Tripo entrega 4096²).
Exit: `0` ok · `2` entrada/argumento (inclui saída = pasta da fonte) · `3` fora de escopo (rigado/sem malha → fluxo manual com
animador) · `4` falha de export. Saídas: `<asset_id>.fbx` (LOD0–2), `<asset_id>_LOD0..2.glb`, `<asset_id>_report.json`.

### 6. Validação
```
python .claude/scripts/tj_asset_budget.py production/assets/quarantine/<asset_id>/work --perfil mobile
```
Exigir PASS em todas as GLBs (o FBX sai `NAO_AVALIADO` por design; vale o report). O script **não mede textura embutida no GLB**:
confira tamanho/quantidade de texturas no `<asset_id>_report.json` do Blender. Ler `avisos` do report: cada aviso é resolvido
ou justificado na evidência. Conferir dimensões contra o contrato, UV presente, ≤ 2 materiais, LOD1 ≈ 50% e LOD2 ≈ 25%.
Se o budget **não rodar** (Python ausente, caminho errado), o resultado é `NÃO AVALIADO`, nunca PASS.

### 7. Unity (Editor livre — 1 por vez)
**Caminho padrão: os 3 GLB via glTFast** (pacote `com.unity.cloud.gltfast`) numa pasta de staging — importam com material PBR e
textura; **montar o prefab com LODGroup no Unity** (script de Editor) com os GLB `_LOD0/1/2`, transições 60/30/10% e culled ~3%;
aplicar import settings do agente (§4.6: Read/Write off, materiais do projeto `M_<asset_id>`, ASTC no override Android); prefab;
FBX dá LODGroup automático, mas a textura chegou **branca** no Unity mesmo com `embed_textures` (defeito aberto) → textura via FBX
= `NÃO AVALIADO`; label Addressables `cat:<prefixo>`. Screenshots: 4 ângulos + em cena → `quarantine/<asset_id>/qa/`.
Editor indisponível → `NÃO AVALIADO (Editor ocupado)` e o fluxo para aqui.

### 8. Revisões e promoção
- `art-director`: estilo. `ip-originality-reviewer`: veredito sobre origem/semelhança/licença (`APROVADO` obrigatório).
- Promover para `Assets/Art/<Categoria>/<asset_id>/`; linha em `docs/REGISTRO_ASSETS.md`.
- Evidência: `production/qa/evidence/AAAA-MM-DD-asset-<asset_id>.md` com comandos, saídas, tris por LOD, créditos (conta/saldo),
  screenshots e vereditos.

## Saída ao usuário (máx. 6 linhas)
asset_id e status (PROMOVIDO / EM QUARENTENA / BLOQUEADO) · tris LOD0/1/2 · budget PASS/FAIL/NÃO AVALIADO · créditos gastos
(conta A/B, saldo ou `?`) · licença · pendências que dependem do usuário (créditos, login, decisão jurídica).
