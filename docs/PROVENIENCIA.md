# Forge Street — Proveniência e licença de arte

- **Por quê:** ART_BIBLE §10 e ASSETS §4: cada asset gerado entra aqui (ferramenta, plano pago, prompt, data, SHA-256); sem a linha, o asset não entra em build comercial. Modelo de campos: `chronicles-of-existence/docs/arte/PROVENIENCIA.md` §1.
- **Regra:** bloco aberto ANTES de gerar (PIPELINE_ARTE_LICOES §3.1 passo 1) e completado depois (task/URL, saída, SHA-256, créditos).
- **Estado:** 2026-10-06 20:59. **Lotes 1, 2 e 3 completos** (20 modelos, 9 rigs, 1 080 créditos; saldo 21 370). Lote 1 completo: 8 modelos (4 personagens com rig Mixamo + 4 estações) + 7 clips Mixamo; 440 créditos (saldo 22 450 → 22 010).

## 1. Livro-razão de créditos (Tripo, conta do estúdio, plano Max declarado em `ARKANA/design/cenario/TRIPO-STUDIO.md` 30/09)

| Data/hora | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| 2026-10-06 ~14:55 | Abertura de `studio.tripo3d.ai/pt/workspace/generate` no navegador integrado | **não visível** (site sem sessão: botão "Registrar/Entrar") | 0 | BLOQUEADO; operação passou para o Chrome do Vinicius (Claude in Chrome), onde a sessão existe |
| 2026-10-06 15:04 | Chrome: página do gerador aberta, sessão ativa, preço de assinante ("Gerar 45"), Privacidade = Privado | **22 450** | 0 | — |
| 2026-10-06 15:05 | 4 imagens de referência do ferreiro (Nano Banana 2, 1:1, gerador embutido) | 22 450 | 0 (grátis, 20/dia) | candidata #1 escolhida |
| 2026-10-06 15:08 | Modelo HD do ferreiro gerado a partir da #1 (H3.1, 8 000 tris, 2K, remover iluminação, PBR, Ultra, triângulos, privado) | **22 405** | 45 | task `770b6100-04e8-4cf7-b5de-be55fa796353` |
| 2026-10-06 15:11 | Auto Rig humanoide, predefinição de esqueleto Mixamo | **22 385** | 20 | mesma task |
| 2026-10-06 15:12 | Exportar FBX, predefinição Mixamo, textura 2K, esqueleto ligado → `ferreiro_mixamo.zip` | 22 385 | 0 | download no Chrome |
| 2026-10-06 15:57 | Ajudante: 4 imagens (gratis), escolhida #3 (bone e lenco azuis: separa do ferreiro) | 22 385 | 0 | — |
| 2026-10-06 15:59 | Ajudante: modelo HD (mesmas configuracoes do ferreiro); visualizador deu "erro desconhecido", recarregar a pagina resolveu sem nova cobranca | **22 340** | 45 | task `601dd4f0-d3aa-45b3-be90-c4651c8285ef` |
| 2026-10-06 16:02 | Ajudante: Auto Rig Mixamo | **22 320** | 20 | mesma task |
| 2026-10-06 16:03 | Ajudante: export FBX Mixamo 2K → `ajudante_mixamo.zip` | 22 320 | 0 | — |
| 2026-10-06 16:06 | Anao: 4 imagens, escolhida #4; modelo HD | **22 275** | 45 | task `b5be25d3-e981-4f6a-b8ed-addf3845fe38` |
| 2026-10-06 16:07 | Guerreira: 4 imagens, escolhida #3; modelo HD | **22 230** | 45 | task `dc45f87d-4487-4a4c-96f9-50fb37f26bd2` |
| 2026-10-06 16:08 | Fornalha: 4 imagens, escolhida #4; modelo HD | 22 185 | 45 | task `8f59254c-38e0-472d-ba4e-7a84c10a3a92` |
| 2026-10-06 16:10 | Anao: Auto Rig Mixamo (1o clique numa aba de fundo nao disparou; 2o sim, cobrado 1 vez) | 22 165 | 20 | task `b5be25d3-...` |
| 2026-10-06 16:11 | Guerreira: Auto Rig Mixamo | 22 145 | 20 | task `dc45f87d-...` |
| 2026-10-06 16:13 | Exports FBX: anao e guerreira (Mixamo), fornalha (Blender) | 22 146 (visor) | 0 | — |
| 2026-10-06 16:15 | Bigorna #4, Balcao #4, Deposito #3: imagens + modelo HD cada | — | 135 | tasks `99040ea4-8616-4191-a22c-0e925f151739`, `9fbd65e3-4eeb-496d-be28-1a8d16392dbc`, `050d70f3-5d31-4b91-853f-e0f52db7b511` |
| 2026-10-06 16:21 | **Fechamento do Lote 1** (exports FBX Blender das 3 estacoes) | **22 010** | **total 440** | = orcamento aprovado (440) |
| 2026-10-06 ~17:20 | **Abertura dos Lotes 2+3** (aval do Vinicius: "todas as artes estão aprovadas"; orçamento 350 + 290 = 640). Chrome "VAI NESSE"; painel veio com Textura 8K LIGADA ("Gerar 65") e privacidade fora de Privado: corrigir antes de gerar | **22 010** | 0 | teto: saldo final ≥ 21 370 |
| 2026-10-06 17:08 | Mago: imagem #3 de 4 (pernas/botas à mostra); modelo HD; Auto Rig Mixamo apesar do aviso "não pronto para rigging" (túnica larga), esqueleto encaixou | — | 65 | task `ce669e61-1aff-4172-8e15-5a3ad87bfdb3` |
| 2026-10-06 17:14 | Elfa: imagem #2 de 4 (capuz pontudo, arco nas costas); modelo HD + Auto Rig Mixamo | — | 65 | task `583e5c6a-8229-4d3d-95fb-e5edbe0066dc` |
| 2026-10-06 17:16 | Goblin mercador: imagem #2 de 4 (braços livres da mochila); modelo HD (**rig pendente**) | **21 835** | 45 | task `a48609f7-8631-4add-ab99-a4086b6e894a` |
| 2026-10-06 ~17:22 | **PAUSA pedida pelo Vinicius.** Cavaleiro: 4 imagens grátis, escolhida #1 (escudo kite no braço), carregada no painel e NÃO gerada. Downloads pendentes: o Chrome "VAI NESSE" não grava neste PC (export do Mago deu sucesso no backend, arquivo não chegou); exportar pelo perfil Default | **21 835** | total 175 | faltam: rig goblin (20), cavaleiro (65), 2 bancadas (90), Lote 3 (290) |

## 2. Registro por asset

### ferreiro
- categoria: Personagem (jogador) — ASSETS.md Lote 1 #1
- estado: **PRONTO em 2026-10-06 15:16** (operador: coordenador, via Chrome do Vinicius): modelo + rig + FBX + prévia aprovada
- ferramenta: Tripo Studio (studio.tripo3d.ai) — Imagem: gerador embutido (GPT Image 2, 1:1); Modelo: HD H3.1 (nome exibido na UI em 2026-10-06: "H3.1 - Máx. qualidade"); Rig: Auto Rig esqueleto Mixamo; Blender 5.2.1 LTS (prévia)
- plano: pago (confirmado em 2026-10-06: Privacidade "Privado" liberada, preço de assinante 45, saldo 22 450)
- comprovante_plano: a preencher pelo Vinicius (nº da fatura; nunca dado de cartão)
- data: 2026-10-06 15:05 (imagem) / 15:08 (modelo)
- entrada: n/a (texto só; imagem gerada dentro do Tripo — nenhum arquivo enviado)
- prompt_imagem (USADO, = assunto + `[S]` + `[I]`; modelo de imagem Nano Banana 2, 1:1, 4 imagens): `young blacksmith hero, broad shoulders, dark brown leather apron to the knees over cream shirt with rolled sleeves, oversized grey gloves, red bandana, big smithing hammer hooked on the belt, friendly round face, short dark hair, stylized low-poly game asset, chibi proportions 3 heads tall, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo, full body front view, T-pose arms horizontal, soft even studio light, plain light grey background, 3D render look, no ground shadow`
- prompt_3d (se o campo de texto do modelo for usado; assunto + `[S][T]`): `young blacksmith hero, broad shoulders, dark brown leather apron to the knees over cream shirt with rolled sleeves, oversized grey gloves, red bandana, big smithing hammer hooked on the belt, friendly round face, short dark hair, stylized low-poly game asset, chibi proportions 3 heads tall, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo, T-pose, arms straight out horizontal, legs slightly apart, neutral face, symmetrical`
- parametros (USADOS, defaults do site em 2026-10-06 coincidiram com a receita COE 30/09): Modelo HD, H3.1, ~8 000 polígonos, triângulos (quad se disponível), textura 2K, "remover iluminação" ligado, 8K DESLIGADO, Gerar em Partes DESLIGADO, **Privacidade = Privado antes de gerar**; Auto Rig humanoide, esqueleto **Mixamo**; export FBX (predefinição Blender) + GLB se houver
- criterio_escolha_imagem (ART_BIBLE §4–5): 3 cabeças (não 2,5), ombros ≈ 1 cabeça, mãos/botas 1,5×, T-pose ≤ 10°, avental até o joelho, bandana vermelha `#C1272D`, martelo grande no cinto, camisa creme, couro `#7A4A2A`, fundo neutro, sem IP
- creditos_previstos: 45 (modelo) + 20 (rig) = 65; teto duro 90; imagens 0 (20 grátis/dia)
- creditos_gastos: **65** (45 modelo + 20 rig; saldo 22 450 → 22 385). Dentro do previsto (65) e do teto (90).
- task_id / url_asset: `770b6100-04e8-4cf7-b5de-be55fa796353` — https://studio.tripo3d.ai/pt/workspace/generate/770b6100-04e8-4cf7-b5de-be55fa796353
- saida_fonte: `arte/tripo/ferreiro/ferreiro_mixamo.zip` (SHA-256 `6fc9ce5f4d8f8a89f1b3f5a629cc3f1aed6aef21e9129e82c8ed5628cc8330b4`, 24 536 000 B), extraído para `tripo_convert_69868933-1e2e-41d3-9802-1309391a9369.fbx` (SHA-256 `6a0b8dfa3567a1c2930d15590d32f8d030f8b471bc97d85514301f62a8256fae`) + `.fbm/` com 5 PNG (basecolor, normal, metallic, roughness, rm)
- validacao_blender (5.2.1, `preview_fbx.py`): 7 164 tris, 1 malha, altura 0,98 m, pé em z=0, Armature com **65 ossos, 65 com prefixo `mixamorig:`** (Hips, LeftUpLeg, LeftLeg, LeftFoot, Spine, Spine1, Spine2, LeftShoulder…), escala do armature 0,01 (padrão FBX cm), 1 material, 6 imagens. Prévias `preview_frente.png` e `preview_topo60.png` aprovadas pelo coordenador (silhueta lê a 60°).
- visibilidade: privada (Privacidade = Privado antes de gerar)
- autor: operador: worker raia 4 do ORQUESTRADOR (personas 29 character-3d + 28 concept-art-lead) sob autorização do Vinicius; sem retoque manual ainda
- licenca: tripo_pago (condicionada ao plano ativo na data; `tripo_free` reprova)
- termos_url: https://www.tripo3d.ai/terms
- termos_versao: Last updated: July 11, 2025 (conforme COE PROVENIENCIA §2, lido em 2026-09-29/10-03; reler no dia)
- termos_consultados_em: 2026-10-06 (não relidos nesta sessão; versão de 2025-07-11 conforme COE)
- g1 / g2 / g3: n/a. Escolha da imagem (faz as vezes do G2): **candidata #1 de 4** — 3 cabeças, rosto redondo simpático, bandana vermelha, martelo pendurado no cinto, avental até o joelho, luvas e botas grandes, pose T ≤ 10°, fundo cinza neutro. Rejeitadas: #2 e #3 braços caídos (pose A ~25°), #4 luvas menores e figura mais magra.
- obs: 2026-10-06 — navegador integrado sem sessão no Tripo; nada gerado. Lição 13 do PIPELINE: UMA página nova por peça, nunca trocar a imagem na página de resultado; foto do painel de créditos antes e depois.

### ajudante
- categoria: Personagem (ajudante base) — ASSETS.md Lote 1 #2
- estado: PRONTO 2026-10-06 16:03 (operador: coordenador, Chrome do Vinicius)
- prompt_imagem: assunto da linha #2 + `[S]` + `[I]`; Nano Banana 2, 1:1, 4 imagens; escolhida #3 (bone e lenco azuis, pose T limpa; as outras tinham bone marrom igual ao couro do ferreiro)
- parametros: iguais ao ferreiro (HD H3.1, 8 000, 2K, remover iluminacao, PBR, Ultra, triangulos, privado); rig humanoide Mixamo; export FBX predefinicao Mixamo 2K
- creditos: 65 (45 + 20); task `601dd4f0-d3aa-45b3-be90-c4651c8285ef`
- saida_fonte: `arte/tripo/ajudante/ajudante_mixamo.zip` (SHA-256 `62b37a0c1fcd15e3ead150475196c70164f31b51f689cfaf4d18576849633c0d`) → `tripo_convert_50943298-43ce-48cc-8a05-63f887e58856.fbx` (SHA-256 `a4bd647381c29b21a9d0dac75ef59b42698d6172137c7261aa223e1d184d2a8e`)
- licenca: tripo_pago · visibilidade: privada

### clips Mixamo (servem a todos os personagens com esqueleto `mixamorig:`)
- fonte: mixamo.com, conta Adobe do Vinicius (login feito por ele em 2026-10-06), personagem X Bot (padrao, nao enviado nenhum modelo nosso)
- configuracao: FBX Binary, **Without Skin**, 30 fps, keyframe reduction none; andar com **In Place**
- termos: Adobe/Mixamo (uso em jogos permitido pelo FAQ; termos completos a reler antes do lancamento — pendencia do PIPELINE §5)
| arquivo | clip no Mixamo (descricao) | SHA-256 |
|---|---|---|
| `arte/mixamo/Idle.fbx` | Breathing Idle | `befaa0fa9c5684a4e82dd229b04710588b1f32bc026704cc1856cae9c80f4220` |
| `arte/mixamo/Walking.fbx` | Unarmed Walk Forward (Walking Forward), In Place | `87c18ad47c01e126faf31921d316cf0714e7867c3ab29fbedc404ca79d528585` |
| `arte/mixamo/Cheering.fbx` | Cheering (Male Cheering With Two Fists Pump) | `239cecccd1f25fd48e4ae1cbf88d79957402afe32032cb7bd1dbdc4d66c72985` |
| `arte/mixamo/LookingAround.fbx` | Looking Around (Idle Stand Looking Around) | `9c80441200a9e6c3c13b570e65183b7d3faeb6ff0f0dc1a94785a4dab144d930` |
| `arte/mixamo/Angry.fbx` | Angry (Standing Angrily) | `1db40ab4f3621fb0e77ffd95ca886c6b63125783d3a2818c6783d823240b0245` |
| `arte/mixamo/Thankful.fbx` | Thankful | `5462c4529ed06b88caaa877918be24535b2474731489daa027c365b91bfe7e88` |
| `arte/mixamo/Waving.fbx` | Waving | `d8f9444653475b44541eea77beb8e64f3eb3ee9b6ea1366460480e4489b91367` |

### anao · guerreira (personagens, rig Mixamo, export FBX predefinicao Mixamo 2K; 65 creditos cada)
| asset | imagem escolhida | task | zip (SHA-256) | fbx (SHA-256) |
|---|---|---|---|---|
| anao | #4 de 4 (cores chapadas, barba tranca + capacete leem de cima, sem sombra no chao) | `b5be25d3-e981-4f6a-b8ed-addf3845fe38` | `arte/tripo/anao/anao_mixamo.zip` `2927aa2f73d08d30ae02c3601fe509d5dd01c0b0f75753f8b9ffe78838ea157d` | `5aee4aae3b5edf752c1f7955a0a1ec9d371818ba9c769dc2dbd2f421367891da` |
| guerreira | #3 de 4 (rabo de cavalo alto + ombreiras + escudo nas costas = silhueta mais forte) | `dc45f87d-4487-4a4c-96f9-50fb37f26bd2` | `arte/tripo/guerreira/guerreira_mixamo.zip` `91565d08e117eaf752e43771018cf2da2e0e9dbc55adb5bdde4b958fb704db5e` | `777a929bab5f188614b93ec71bfbf02e4f7e5874245d9c8d3ddc97e4803ad644` |

### estacoes (sem rig, export FBX predefinicao Blender 2K; 45 creditos cada)
| asset | imagem escolhida | task | zip (SHA-256) | fbx (SHA-256) |
|---|---|---|---|---|
| fornalha | #4 (lanterna vermelha no canto superior direito = estado "travada" da Art Bible; fole; boca acesa) | `8f59254c-38e0-472d-ba4e-7a84c10a3a92` | `arte/tripo/fornalha/fornalha.zip` `6365e4025487ae26676a2ef395671b5aff1f2799bf54cede62036cf9258cb179` | `c1e55d6f686aca6113a1fa90c4f866981babc3fb54acda90e1e4d2d5af6a5207` |
| bigorna | #4 (martelo-pilao separavel no Blender para animar; lanterna; balde) | `99040ea4-8616-4191-a22c-0e925f151739` | `arte/tripo/bigorna/bigorna.zip` `dce20d76153d99008d143a988a30b4ab465221737c56194d8deb3b7f37309fdd` | `8d34bec714fe442a4b9de26c35f629c7ea1841167272fbd95b39b41695e1a473` |
| balcao | #4 (toldo listrado dominante visto de cima; placa; bandeja de moedas) | `9fbd65e3-4eeb-496d-be28-1a8d16392dbc` | `arte/tripo/balcao/balcao.zip` `249f389985a6b0618e0bbb4d7ff393e14ac80ca016708b2e683084dad3225096` | `7bd03cc5e738e85591e6b430ee4eb27b7e0d52f7160889e7af8de53bc4ab2775` |
| deposito | #3 (carrinho nos trilhos, minerio com brilho laranja) | `050d70f3-5d31-4b91-853f-e0f52db7b511` | `arte/tripo/deposito/deposito.zip` `5342fa912efa2fd1ffe6e8dc742f127bbb9f1fc8bcf60b88d742317424d1b6c6` | `8e991bb10c04b8f0823d69faa07af9a8255e346af8f89aa212646e810458fd55` |

Prompts usados: os da tabela de `ASSETS.md` (assunto + `[S]`/`[P]` + `[I]`), Nano Banana 2, 1:1, 4 imagens gratis por asset. Parametros 3D iguais aos do ferreiro. Todos privados, plano pago, operador: coordenador pelo Chrome do Vinicius.

## 3. Lotes 2+3 (2026-10-06, 17:05–20:59; pausa 17:22–20:35 a pedido do Vinicius)

- Operador: coordenador pelo Chrome do Vinicius ("VAI NESSE" até a pausa; "PC de casa" na retomada = este PC, downloads em `Downloads`). Receita conferida antes de gerar: HD H3.1, 8 000 polígonos, 2K, Ultra, remover iluminação, PBR, triângulos, 8K DESLIGADO, Privado. Imagens: Nano Banana 2, 1:1, 4 grátis por peça. Personagens: Auto Rig humanoide Mixamo (20) + export FBX predefinição Mixamo 2K; peças: export FBX predefinição Blender 2K.
- Créditos: 22 010 → **21 370 = 640** (Lote 2 = 350, Lote 3 = 290), exatamente o orçado. Licença tripo_pago, visibilidade privada.
- Armadilhas: o painel veio com 8K ligado, textura 4K e 2 000 000 polígonos ("Gerar 65"); clique por ref em aba de fundo não dispara; o Chrome "VAI NESSE" não gravava neste PC (export ok no backend, arquivo sumia); o 1º export da elfa se perdeu ao navegar 10 s depois (refeito, conferir `Downloads` antes de sair da página).

| asset | categoria | imagem escolhida | task | créditos | zip (SHA-256) | fbx (SHA-256) |
|---|---|---|---|---|---|---|
| mago | Personagem (cliente) — Lote 2 #18 | #3 de 4 (pernas/botas à mostra; #1/#2 túnica até o chão). Rig com aviso "não pronto" (túnica larga), esqueleto encaixou | `ce669e61-1aff-4172-8e15-5a3ad87bfdb3` | 65 | `arte/tripo/mago/mago_mixamo.zip` `6b15daeed65c7a3f57a1ac9d7a5eeb6ddfbbb2b1095879a2da878e945bd6e455` | `59e6d3a30974e29f8905f96d8f01464f5fc890359f53b12284fc2ffba0b6f2de` |
| elfa | Personagem (cliente) — Lote 2 #19 | #2 de 4 (capuz pontudo, arco nas costas, pose T limpa) | `583e5c6a-8229-4d3d-95fb-e5edbe0066dc` | 65 | `arte/tripo/elfa/elfa_mixamo.zip` `02c832621de12b42ae42a133e75ed7846a783fbecc9d981ae67876728127c2c5` | `1f35a1e2ab217c06c455eb22a754ab87f8393705bc9b8347a2fc42ec7f7a1e7d` |
| goblin | Personagem (cliente) — Lote 2 #20 | #2 de 4 (braços livres da mochila, pernas à mostra) | `a48609f7-8631-4add-ab99-a4086b6e894a` | 65 | `arte/tripo/goblin/goblin_mixamo.zip` `519ae64248d92c3f01275dc0de035893d2199b9c755bd11059609b3204f76cc8` | `07512f96ccfbe10b32fa7c7ba59f31ce9c89c366bca47ee8e2f32a244187b8de` |
| cavaleiro | Personagem (cliente) — Lote 2 #21 | #1 de 4 (único com escudo kite no braço; pluma vermelha) | `8fdcccbd-e511-4c06-a2b5-82260c84ff17` | 65 | `arte/tripo/cavaleiro/cavaleiro_mixamo.zip` `adaed13bb4ee92217a6931f50f00f0882a7fce150c28f1eb745fe3a9ad9c3ef2` | `59928fa19146dc2ae2ee97b5dc4e667a0c8d8aab69a6be665cbd53cd5b6130f0` |
| bancada_escudos | Estação — Lote 2 #22 | #4 de 4 (3 rodelas no suporte, lanterna sup. dir.) | `7b2eeae3-8620-4686-a47b-dda03dc86f3f` | 45 | `arte/tripo/bancada_escudos/bancada_escudos.zip` `266bfd529e9044ae707d5955d23d780f71ff070f8f76c0dc180d1f13b4ebf580` | `4387b6660d6b16d424582986fc8388d216fb39d4a1fd151a320a6794668c3489` |
| bancada_ferramentas | Estação — Lote 2 #23 | #4 de 4 (mesma composição da de escudos) | `28f304a5-229a-4256-abf9-7e0f329092f4` | 45 | `arte/tripo/bancada_ferramentas/bancada_ferramentas.zip` `c3a681c6a0a6ad2fcf6962c77c78ddf606eca6d7607210593be370589ad380cf` | `027585847d92e13f5449e6a39497c4216e78a24a8bb4ffcb9ecaf369aa436705` |
| bancada_joalheria | Estação — Lote 3 #31 | #2 de 4 (bandeja de gemas + lupa leem de cima) | `64205eb1-4e8f-4c9c-9d08-f2983b3c6b32` | 45 | `arte/tripo/bancada_joalheria/bancada_joalheria.zip` `5eec493fc6aa587945eb2d919b001bebb8cadda94f1e0af2558ffd2ad7faa3c2` | `3af9b33bec11a98b57aaf311d5cb98b7cb4081f1bf85fbcd054636594a3978fd` |
| loja_joalheria | Estação — Lote 3 #32 | #1 de 4 (barraca com toldo roxo, estilo do balcão). 1º clique "erro desconhecido no servidor" sem cobrança | `ce91e2b3-89b3-483c-8f58-fe4774260481` | 45 | `arte/tripo/loja_joalheria/loja_joalheria.zip` `59b19a19611876e7b064115d6345f487a1d52d4e8e826d26c9aed5df0853e939` | `eca058de116799af046aaea3f97714b7da595d5c4e90113a604979414315b829` |
| bau | Prop — Lote 3 #33 | #4 de 4 (cores chapadas, tampa aberta com ouro) | `bbd646a0-57d2-4b0e-845f-a915bb6454bb` | 45 | `arte/tripo/bau/bau.zip` `38572ae90029d4278b48bce30eb95bdc5e61a8fea896c7bebea56baae71aa8eb` | `b40f1007af2eb6df2be9d649157eafd3ece686bbc4e748b98f5e1edbf6fba184` |
| carroca | Prop — Lote 3 #34 | #3 de 4 (barris + sacos + lona dobrada em cima) | `148da6c7-fe33-4560-a17f-401bfb9b99ff` | 45 | `arte/tripo/carroca/carroca.zip` `0b2026d51a85f4e3a14e9ed91270f799362e640f3ce864381fe8e9909417390b` | `5488a86b946416983add1c857ae0312eb505a7e5937578ff74c8917a79ff2ac1` |
| arco | Ambiente — Lote 3 #35 | #1 de 4 (arco robusto; #4 tinha telhado que esconde a rua de cima) | `e3c9eed0-cd99-4703-8de7-7e3134b90997` | 45 | `arte/tripo/arco/arco.zip` `2c589331ec2f9ceab07a081b3eab1a0f57b8c1001f91eb6516301118c8742fb3` | `fbc720fec3478316d87a8cfeeff3774f0852e4bf7a3057197366179e52ca5294` |
| nobre | Personagem (cliente 2ª área) — Lote 3 #36 | #3 de 4 (pena grande no chapéu, monóculo, caixinha de anel) | `d8980357-6fbb-4172-8c26-e3cc085a5887` | 65 | `arte/tripo/nobre/nobre_mixamo.zip` `26ad2a7ddb6ee81203122469b2540ca61de52c506ad8cea9621e39c80c286b4f` | `cb54e757d0b34dd688024063474a37ca4ebfc7e9a261e9eb0835fd30f32c5314` |

## 4. Texturas de ambiente (2026-10-07, imagem grátis do Tripo — 0 crédito)
- Ferramenta: Tripo Studio, gerador de imagem embutido (Nano Banana 2, 1:1, 4 imagens grátis), conta do estúdio (plano pago), Chrome "PC de casa". Uso: texturas repetidas do chão/rua/parede/assoalho (pedido do Vinicius: "essa cor sólida cinza não passa credibilidade").
- Processamento: `arte/texturas/fonte/*_src.png` (1024², originais) → mistura com cópia deslocada pela metade (sem emenda) → brilho 0,62–0,75, saturação 0,80–0,90 → 256² em `client/Assets/_FS/Resources/Textures/`.
| textura | prompt (resumo) | escolhida |
|---|---|---|
| `piso_oficina` | lajes de pedra quente, vista de cima, juntas escuras finas, pintado à mão, baixo contraste | #3 de 4 |
| `rua` | calçamento medieval, pedras arredondadas cinza-azuladas | #1 de 4 |
| `parede` | parede de oficina, blocos de pedra cinza quente + tijolos vermelho-marrom, vista frontal | #4 de 4 |
| `madeira` | assoalho de tábuas marrom quente com pregos | #2 de 4 |

## 5. Props de cenário (2026-10-07, Blender por primitivas — 0 crédito)
- `client/tools/props_blender.py` (Blender 5.2 headless) modela por primitivas/bmesh com materiais chapados da paleta da ART_BIBLE §2; FBX em `arte/props/<nome>/<nome>.fbx`; sprites estáticos (`render_sprites.py --dirs 1 --size 256`) em `client/Assets/_FS/Resources/Sprites/<nome>/`.
- Peças: barril, caixote, sacos, tocha, prateleira, suporte_armas, lenha, balde. Autoria: raia de ambiente (personas 30/31/32) sob coordenação; licença: própria do estúdio (sem asset de terceiros).

## 6. Leva 1 de arte v0.5 (2026-10-08, Blender por primitivas — 0 crédito)
- **Ferramenta:** Blender 5.2.1 LTS headless, `client/tools/arte_v05_blender.py` (SHA-256 `a8a083b806315535ed19a00cc745cb5614fbeefb413e36a48373d15661b00dcc`), que importa as primitivas de `props_blender.py`. O `main()` deste passou para `if __name__ == "__main__":`, sem mudança de saída (SHA-256 novo `37242dcb2ec3ea3047d9c7b15e60bacdb0993a18eaad5846a410f2f82d7d9bfa`). Contorno `#1E1612`, sombra, redução 4×, recorte dos módulos e sombra de contato rodam em numpy dentro do Blender. Script das folhas de QA: `client/tools/folhas_v05.py` (Python 3.14 + Pillow 12.2, SHA-256 `6b8bd31797d85cd5acd0f7d976ad84e0d421e580a6518b920b1a613438bc534e`).
- **Entrada:** nenhuma. Sem rede, sem IA generativa, sem Tripo/Mixamo, sem asset de terceiros. As texturas e personagens das folhas são os já registrados nas §2–4, usados só na conferência. Créditos: **0** (saldo Tripo intocado).
- **Licença:** própria do estúdio. Autoria: worker da raia de arte (personas blender-3d + ai-game-development-director) sob o ORQUESTRADOR, a pedido do Vinicius (2026-10-08).
- **Comando:** `blender.exe -b --factory-startup --python-exit-code 1 --python client/tools/arte_v05_blender.py -- --check <tmp>`, depois `python client/tools/folhas_v05.py --check <tmp>` (`ASSETS.md` §7).
- **Reprodutibilidade:** um 2º render deu 26 de 27 PNG idênticos bit a bit. `porta_servico_aberta` variou até 5/255 (ruído de antisserrilhado do Workbench). Os SHA abaixo são dos arquivos entregues.

| Arquivo (`client/Assets/_FS/Resources/Sprites/`) | px | bytes | SHA-256 |
|---|---|---|---|
| `item_minerio/icone.png` | 128×128 | 14 923 | `5a2863efdb0fba2bb04803352f3a4a13f2485ec304cf3e0bb972fd806a651857` |
| `item_minerio/deitado.png` | 128×128 | 15 831 | `c955dd138c75e9b8d17dbb39893a7583b5353d4432f45da110d87b23bde091d0` |
| `item_lingote/icone.png` | 128×128 | 9 278 | `a466f5da6dbc6297d5e4cbda69543cdbb2b7907659a5fa88b5fd862e6d044974` |
| `item_lingote/deitado.png` | 128×128 | 9 152 | `e3790d20fd68ebbdb94b35903a1b43cb9fb9c448e978bbef3acd7497d5d558e9` |
| `item_espada/icone.png` | 128×128 | 10 441 | `f074a2de70eeaeab2d7e1b7fe40011567ad46e723577853123c9d7b5e5ada27b` |
| `item_espada/deitado.png` | 128×128 | 5 100 | `d58391920e71dadee10cc8515708a29ac98f807be0b73ee8557214364c9b55fd` |
| `item_escudo/icone.png` | 128×128 | 16 794 | `162f768f728dee56c7355ae1cb7a8c35a6f7dac907be10e176d984df0bdfd841` |
| `item_escudo/deitado.png` | 128×128 | 17 367 | `f1b50581b5903ad52e103caedf76b162a6f2ee038743f20fcf16dab31676dc6f` |
| `item_ferramenta/icone.png` | 128×128 | 11 472 | `3b40adcdcb08215231b2b0a0661f32503d7eaf585178e0a3c26f69be036020aa` |
| `item_ferramenta/deitado.png` | 128×128 | 7 290 | `b87f647a5e307df6e5833f1724ad906ba3dcdd5fac31c4045496144b98bf8801` |
| `item_joia/icone.png` | 128×128 | 14 176 | `0b56013438cdb4a07940a42629699cd0cfce2187181977c02399a292e60d85a0` |
| `item_joia/deitado.png` | 128×128 | 12 908 | `a02d77b58a7063d6a64b5f95144c8216ee9127b0978f0fb85b1770c55cbae267` |
| `moeda/icone.png` | 128×128 | 15 213 | `5858ed9421f6c33bc58670bf8f69c515d51c6a435d6f8ca265cfbe66a3c39b8e` |
| `moeda/deitado.png` | 128×128 | 16 110 | `a0dcff4f890bc208332eda75caab84237c26460ac1a3b50eab6043a73fd36ed5` |
| `item_espada_em_pe/Static.png` | 36×106 | 4 067 | `1f26ca1c2caec719e2f147531f340cde84d983a04fff2cec0659a7e8c665ce9b` |
| `item_escudo_em_pe/Static.png` | 72×70 | 7 380 | `7170ab33d7485684d80a1bea3272f07c1a0b95e81bd001cbea0d332307689843` |
| `item_ferramenta_em_pe/Static.png` | 60×84 | 3 999 | `8b5596f0a385eaed60c131b0116b954c65ceaf6fb4c8f6bc08949e13cbade970` |
| `balcao_ponta_esq/Static.png` | 64×269 | 15 144 | `33ab066c9c6cecdd5400200ccdbc7ac4b94d61887fb6c29b814ea0eb18fa76e6` |
| `balcao_meio/Static.png` | 136×269 | 37 624 | `25b227d8dd819f83a71902bf6d509c9532a5c7f14b08057070dda3d93411b2eb` |
| `balcao_escudos/Static.png` | 136×269 | 38 102 | `611c28615ee59d0e4a31c5754d5947304871d71a5ae79c54b3b44a0578453747` |
| `balcao_ferramentas/Static.png` | 136×269 | 38 026 | `80046b8062deaa50c1be6fb2fdeb3db28894cb68b6b959b14ecd89c3a3d28045` |
| `balcao_ponta_dir/Static.png` | 64×269 | 15 560 | `336c00d528eae787dceabffe2b8f9bad1d364500d0c56678196714cc8e0e2301` |
| `pilar/Static.png` | 142×207 | 32 652 | `1f76b759d7f713453aafd9d26fac6dc47c2d405015bb421e82d25bed710af304` |
| `portao_fechado/Static.png` | 104×380 | 47 574 | `34678e05894307818903e70af7cdd5eac9a3668772f61d86c617a50a160d7e22` |
| `portao_aberto/Static.png` | 336×591 | 129 788 | `c088e0f11d7ed3c23d25d06627b29e7391f4bb1b61688f67af70720d62fea4bd` |
| `porta_servico_fechada/Static.png` | 62×356 | 29 248 | `37f8b585de583833fb4c83e5c05a2511e735c84d4528d57d0599676fb9fba167` |
| `porta_servico_aberta/Static.png` | 308×567 | 104 271 | `fcd72e83c26ae651ae5e71aa58716f8e4713103014b2ddbf44d0127d041b1a8c` |

Folhas (`client/Builds/sprites_contact/`, só QA): `v05_itens.png` 1500×1690, 584 604 B, `02d623c415e162423f8968f803bfe85e55d4bb0faaaa5e7ff9932761f1dea2cc` · `v05_balcao_4e8.png` 1248×3381, 1 999 630 B, `ca76ca1098eae1cb131bc03929ef9b1ba9378e973698a210dabaa51110accff3` · `v05_portao.png` 2246×1897, 3 069 158 B, `8d886db72fda422d68feb6f3fb74284726040be542999f9d6727f6cd28650ecf`.

## 6b. Leva v0.5b: ícones de melhoria e exterior (2026-10-08, Blender por primitivas — 0 crédito)
- Ferramenta: Blender 5.2 headless, `client/tools/ui_v05b_blender.py` (importa `arte_v05_blender.py`), primitivas + Workbench FLAT. Nenhum gerador externo, nenhum crédito, nenhum asset de terceiros.
- Saída em `client/Assets/_FS/Resources/Sprites/<nome>/` (PNG + `meta.json`):

| Arquivo | px | Bytes | SHA-256 |
|---|---|---|---|
| `melhoria_mochila/icone.png` | 128×128 | 13 471 | `ab4fbf3e0ee640fb50ca4a13215c0387c87a403eaca7f12445fdec661e557f75` |
| `melhoria_botas/icone.png` | 128×128 | 10 908 | `a46bdac76229fd2c5121687aaf253934b757a04f6091e0aa53eba32d8fdd9051` |
| `melhoria_fole/icone.png` | 128×128 | 12 473 | `4c8a93d11b76b70fb47ff2ad0303b1eed9840b1139c6fb829c205af859647523` |
| `melhoria_fole_duplo/icone.png` | 128×128 | 16 327 | `a69f26c5bf3a4b32bc4059af23b7c5040c6755ca23df3f7e74540c5da0293d69` |
| `melhoria_martelo/icone.png` | 128×128 | 11 112 | `55ecbaae621f6025f5a81d09e8cbd4fc31b216a5f02bf4552836beaacf4db886` |
| `melhoria_lupa/icone.png` | 128×128 | 12 698 | `936e49c24c2857ddede5552f3364c02c903d805c0d08a959353627371f65a734` |
| `melhoria_vitrine/icone.png` | 128×128 | 14 406 | `acd364f9b56cb9760dedfef31b1d9be1f2ba74cc10fa98e438e0cd567731febf` |
| `melhoria_sino/icone.png` | 128×128 | 14 608 | `3854f4aa0890352cbff00226ff3a76e203c19a9763788b6d4a330cc3118eb570` |
| `melhoria_cesto/icone.png` | 128×128 | 19 847 | `fddf218debc91e30fefa9300a117bed8543bbdb3eee93fe8d40fc74f018fd66d` |
| `coroa/icone.png` (v0.5c, mesmo script) | 128×128 | 19 016 | `c3cdd312261844a52ef74ccd5127808ed0bd2c33868cbf02d023cf62b8ffafac` |
| `arvore/Static.png` | 268×239 | 61 884 | `3a3d721a31afac4f63d711cfebc9c639f24a08cf5e28bc7a9c4815b624456b21` |
| `arbusto/Static.png` | 170×119 | 23 391 | `8fa1b74db8f5ac9e187085b95124442a04f8117cc64807b9b1ae74b1b7063c10` |
| `poste/Static.png` | 60×225 | 13 210 | `682db448620f4f2384b07fe55659a2eba6c1c88246ea8b03131f3460026d88a8` |
| `canteiro/Static.png` | 206×96 | 26 834 | `2c320d11fbcd93ce2c29186c2e5b179a01041fd324063eca41b45bcc0d77f933` |

## 7. Lote 4 — 10 clientes novos (aberto 2026-10-08 ~13:30)
- **Aval:** Vinicius, 2026-10-08 (AskUserQuestion: "10 novos ≈ 650 créditos"). Teto: saldo final ≥ **20 720** (21 370 − 650).
- **Receita (conferir no painel antes de CADA geração):** imagem gerada dentro do site (grátis) → Modelo HD H3.1, 8 000 polígonos, triângulos, textura 2K (NÃO 8K/4K), remover iluminação, **Privado** → Auto Rig humanoide com esqueleto **Mixamo** → Exportar FBX (predefinição Mixamo). Uma página nova por peça.
- **Prompts:** assunto + `[S][T]` + `[I]` de `docs/ASSETS.md` (cabeçalho).

| Data/hora | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| 2026-10-08 13:30 | Abertura do Lote 4, antes de qualquer geração | **21 370** | 0 | Chrome "PC de casa" |

| 2026-10-08 13:35 | **PAUSA pedida pelo Vinicius** antes da 1ª geração (só abri o gerador) | **21 370** | 0 | nada gerado; retomar pelo 1º cliente (Bárbaro) |
| 2026-10-08 19:49 | Minerador: imagem #2 de 4 (Nano Banana 2); Modelo HD H3.1 (Ultra, 2K, 8 000, triângulos, remover iluminação, PBR, 8K off, Privado) | **21 325** | 45 | task `436ef357-6978-4855-bac7-0eebde1cfd72` |
| 2026-10-08 19:51 | Bardo: imagem #2 de 4; Modelo HD (mesma receita) | **21 280** | 45 | task `9cf89ab0-e6b5-4a62-b2ec-dfdbdda5b106` |
| 2026-10-08 19:53 | Alquimista: imagem #4 de 4; Modelo HD | **21 235** | 45 | task `5417da22-fc46-464f-8e66-399ca1b6369a` |
| 2026-10-08 19:54 | Caçadora: imagem #2 de 4; Modelo HD | **21 190** | 45 | task `8becbadd-b5dd-48c7-9c12-c639b2de04f1` |
| 2026-10-08 19:55 | Monge: imagem #4 de 4 (calça visível sob a túnica); Modelo HD | **21 145** | 45 | task `57ee5871-858f-46c7-aaa5-08ce22d54321` |
| 2026-10-08 19:57 | Ladina: imagem #2 de 4 (capa curta); Modelo HD | **21 100** | 45 | task `0b039fdf-d6f5-422f-bea8-2f4c39c4247c` |
| 2026-10-08 19:58 | Paladina: imagem #2 de 4; Modelo HD | **21 055** | 45 | task `549e54a6-ce51-47a7-91de-62bd38ea0501` |
| 2026-10-08 19:59 | Orc: imagem #4 de 4 (pose A, braços mais abertos); Modelo HD | **21 010** | 45 | task `aeccd363-82fe-491a-bff5-fd636f14bacb` |
| 2026-10-08 20:00 | Bárbaro: imagem #4 de 4 (única em pose T); Modelo HD | **20 965** | 45 | task `7a9bd6e6-60b9-4354-9155-6709b133c73e` |
| 2026-10-08 20:01 | Pirata: imagem #2 de 4 (sem gancho); Modelo HD — **10 modelos, 450 créditos** | **20 920** | 45 | task `63adc3d5-bff2-4435-9931-989d4da8b3bd` |
| 2026-10-08 20:03–20:10 | Auto Rig humanoide, predefinição de esqueleto **Mixamo**, nos 10 (Minerador, Bardo, Alquimista, Monge, Ladina, Paladina, Orc, Bárbaro, Pirata, Caçadora), cada um pela URL `workspace/rigging/<task>` | **20 720** | 200 | saldo final = teto do aval (21 370 − 650). Orc veio de imagem em pose A (o painel recomenda T; rig aceitou) |
| 2026-10-08 20:10 | **Lote 4 fechado na geração: 10 modelos + 10 rigs = 650 créditos.** Pendente: export FBX (predefinição Mixamo, 2K) e render dos sprites | **20 720** | 0 | |

### Arquivos do Lote 4 (export + sprites, 2026-10-08 20:15–21:20)
- **Export (coordenador):** FBX predefinição Mixamo 2K, `arte/tripo/<cliente>/tripo_convert_<uuid>.fbx` + `.fbm/` com 5 PNG (`fs_cliente_<cliente>_{basecolor,normal,metallic,roughness,rm}.PNG`). Raw art fora do git (`.gitignore`). SHA-256 calculado pelo worker de render em 2026-10-08.
- **Render (worker blender-3d, 0 crédito, sem rede):** Blender 5.2.1 LTS headless, `client/tools/render_sprites.py` sem mudança, com os parâmetros dos 6 clientes atuais (padrões do script: elev 60°, 4 direções S/W/N/E, 12 fps, célula 128 px, ppu automático por modelo, luz flat + contorno + cavidade, AA 8, `--max-frames 32`, retarget por delta de repouso). Clipes: `client/Builds/anims_sets/guerreira/` = Angry, Idle, LookingAround, Thankful, Walking, Waving (SHA iguais aos de `arte/mixamo/`, §2; sem Cheering, como os clientes atuais). Licença: tripo_pago + Mixamo (§2), visibilidade privada.
- **Comando** (raiz do projeto, um cliente por vez; `contact.png` sai de `Resources`, que vai inteiro para o APK):
```
timeout 900 "/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/render_sprites.py -- --model arte/tripo/<cliente>/<FBX> --anims client/Builds/anims_sets/guerreira --out client/Assets/_FS/Resources/Sprites/<cliente>
mv client/Assets/_FS/Resources/Sprites/<cliente>/contact.png client/Builds/sprites_contact/<cliente>.png
```

| Cliente | FBX (`arte/tripo/<cliente>/`) | Bytes | SHA-256 do FBX | Saída `Resources/Sprites/<cliente>/` (ppu · pivô y · PNG) |
|---|---|---|---|---|
| minerador | `tripo_convert_f4f0ab5c-0ca0-4009-b200-742ecaae11ac.fbx` | 13 067 776 | `1497785125384efe6f701c8e276f0bc74921d6aafb163b3940af78c79e92b82a` | ppu 116.31 · 35.1 px · 6 PNG, 7 350 386 B |
| bardo | `tripo_convert_cbabe90d-f1e3-4a95-9c52-1f4b905e61ff.fbx` | 13 344 048 | `66ad0344a17d530422d6a0436e6949976dd67eda457036a12d568c24bcb6161c` | ppu 114.51 · 31.2 px · 6 PNG, 6 057 809 B |
| alquimista | `tripo_convert_14b06e65-661a-4385-8151-29d50dc22d53.fbx` | 12 503 472 | `e7e4e02e334753a4aa52070d35a24c0d1b94310ab6e53c0b6ed68bc50e054750` | ppu 122.01 · 36.5 px · 6 PNG, 7 130 823 B |
| monge | `tripo_convert_d60f40e4-7c40-4cb6-8995-450cd89f98e2.fbx` | 12 390 304 | `2a9e3a6e633e00a4b0366cda4cb325bdaef15b94e4e0041c280330b8fa51cb03` | ppu 134.32 · 35.2 px · 6 PNG, 7 383 369 B |
| ladina | `tripo_convert_5768a4dc-f123-43b3-b598-014e0a9dd859.fbx` | 11 090 640 | `fa301a0b18293339eb28edda9731ba30acdd23dd43ba6ce001c373cbb654a659` | ppu 124.93 · 39.2 px · 6 PNG, 6 174 615 B |
| paladina | `tripo_convert_4b15b22d-2b4b-4a08-b863-6bfaad8f50c6.fbx` | 16 913 440 | `f5efa0e3e3292a742bdda04c429421f57867e63d7f3d90568759d90e711f1b7b` | ppu 123.74 · 35.4 px · 6 PNG, 7 652 300 B |
| orc | `tripo_convert_41acb365-a32b-48db-8e2a-09925cfa8fa9.fbx` | 10 850 992 | `0625db07111f703d3a0cc26d5fe4764ecc847966b1af92c5d1a405dc18b810e3` | ppu 119.79 · 37.9 px · 6 PNG, 7 066 647 B |
| barbaro | `tripo_convert_3f6ba9bf-de36-473a-8fb9-5a6436e65978.fbx` | 11 919 376 | `834bd4f8bdb52d7e695a6b49dc64a376e7809926c9c4be26af80b69f05b8a789` | ppu 127.26 · 39.3 px · 6 PNG, 7 201 546 B |
| pirata | `tripo_convert_9d7f80f0-4050-4af3-aad9-09a8d524ae0f.fbx` | 13 066 672 | `fe226759183f2d09a7fc694996df6e9aa45b63d21f376ec49beefbae6c8b051b` | ppu 106.90 · 32.1 px · 6 PNG, 6 744 714 B |
| cacadora | `tripo_convert_f9f8969a-f957-43fc-801b-b6cf6fe1e175.fbx` | 11 579 248 | `871e5036c408287934496b99d7317091fd5684fd4f687c3fd26360668044eeef` | ppu 123.04 · 38.7 px · 6 PNG, 6 731 609 B |

Saída por cliente: `meta.json` + Angry/Idle/LookingAround/Thankful (32 quadros, 4096×512), Walking (16, 2048×512), Waving (6, 768×512). Total dos 60 PNG: 69 493 818 B. Os SHA dos PNG não foram registrados (o AA do Workbench varia alguns níveis entre renders, §6). Conferência: `client/Builds/sprites_contact/v05_clientes_lote4.png` (2694×3215, 1 930 713 B, SHA-256 `332409aa1ad3d0466be6dae3ab5aa4719a05f806b62ba4fa68db4a250c9661fb`) e `<cliente>.png` (1º quadro de cada clipe, direção S). CharScale proposto: `ASSETS.md` §9.
