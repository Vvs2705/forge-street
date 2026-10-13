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

## 8. Leva 9 — ícones dos produtos dos distritos 2–6 (2026-10-13, Blender por primitivas — 0 crédito)
- **Ferramenta:** Blender 5.2.1 LTS headless, `client/tools/itens_blender.py` (48 833 B, SHA-256 `d0a8f383aed713b2dc90b4a2c147432d393e99f833148b52333f24d4c883cbdc`, fim de linha LF). O script importa sem alterar `arte_v05_blender.py` (`Peca`, `vista_item`, `meta`, tons e primitivas), `ui_v05b_blender.py` (`Sub`) e `props_blender.py`. A pipeline é a do `item_espada`: Workbench FLAT, render a 512 px reduzido 4×, contorno `#1E1612` de 3 px e sombra de 4 px a 35%. A folha de QA sai de `client/tools/folha_produtos.py` (Python 3.14 + Pillow 12.2, 4 159 B, SHA-256 `60432e0ea66c4cff4d426ca5a9f8fb6f38922a0ffff62afec3115d4a4f34c9eb`).
- **Entrada:** nenhuma. Sem rede, sem IA generativa, sem Tripo, sem Mixamo e sem asset de terceiros. Os modelos são primitivas e bmesh escritos no script, com os matizes da ART_BIBLE §2–3.1. **Créditos: 0**; o saldo do Tripo não foi tocado.
- **Licença:** própria do estúdio. Autoria: worker da raia ARTE da Leva 9 do ORQUESTRADOR (personas 06 ai-art-director e 31 ai-prop-weapon-artist), a pedido do Vinicius (meta GDD ≥ 90%). Data: 2026-10-13.
- **Comando** (raiz do projeto, Unity fechado): `blender.exe -b --factory-startup --python-exit-code 1 --python client/tools/itens_blender.py`. Gera 41 itens e 82 PNG em 17,1 s de render (19,1 s de parede, ~16 s de CPU, pico de 529 MB de RAM, 1 processo). Depois: `python client/tools/folha_produtos.py`.
- **Saída:** `client/Assets/_FS/Resources/Sprites/<id>/` com `icone.png` e `deitado.png` (128×128 RGBA, ppu 128, pivô no centro) e `meta.json` com `"fonte": "tools/itens_blender.py (procedural, 0 credito)"`. Os `.meta` do Unity (pasta, 2 PNG, `meta.json`) são cópias dos do `item_joia`, com o mesmo import (sem mipmap, ASTC 6×6 no Android, DXT5 no PC) e um GUID novo por arquivo. O script só cria `.meta` que falta: rodar de novo não troca GUID, o que foi conferido re-renderizando `item_grevas`.
- **Reprodutibilidade:** num 2º render completo, 81 dos 82 PNG saíram idênticos bit a bit. `item_cristal/deitado.png` variou 1/255 (ruído de antisserrilhado do Workbench, como na §6). Os SHA abaixo são dos arquivos entregues.
- **Conferência:** `client/Builds/validation_art_produtos/folha.png` (1760×3194, 1 662 663 B, SHA-256 `e74e6fb76cd8ae144738c005569dbde64e1f5596930176745dd9f92ae2baaeea`). Reprovados na 1ª e na 2ª folha e corrigidos: liga celeste (lia como placa de "proibido"), panóplia (as espadas sumiam atrás do escudo), espada de gala e lâmina estelar (tinham a mesma silhueta da espada), cajado e alabarda (pareciam a lança), grevas (pareciam as manoplas), minério rúnico (um caixote) e o brilho da placa. Matizes por família: ART_BIBLE §3.1.

| Item (`Resources/Sprites/`) | `icone.png` (bytes, SHA-256) | `deitado.png` (bytes, SHA-256) |
|---|---|---|
| `item_espada_longa` | 7 850 B `ca4a7ac31e9e1b5f4f7e9646f2208179ada9646e68014c32ee2f3da0c7d73dbd` | 4 090 B `b38217e1b478c59680b769589b2ef9564ed2f326d52b8e6751fbdb8fc3e66842` |
| `item_elmo` | 13 692 B `bbbf0ec33e60997bad71cdabbf47615fc05094f808b15190bb0d4ce0b4c037ac` | 14 061 B `9a84857982776279c7048ada114ca4d243227053285939178b941f71534adf4f` |
| `item_peitoral` | 13 192 B `296089d3e0515b57b451c3ca0e3e117d230fd900d6cc776f64218e7d80166e1f` | 11 744 B `97639f475f614f5e2aa4da06b1b47a9b23cc9662747a45cb4c6ff60af05ce6bd` |
| `item_grevas` | 16 867 B `8acea589e8dbd68f20545950dccdaee73c83c56a00f0cf1398c7bd61b3ecacaa` | 17 270 B `135e0e97b69c6bf7ff9a2130371dcba0077e53f75f0947c350715de823dc6517` |
| `item_manoplas` | 19 834 B `142dd78a4d5a984e8aee4db4e0d7f6155fa60ad044ea834a1ef51c0d595368a1` | 17 431 B `be315139fadbce761f678531764a1a63394ea4c2363a81bea93538662ca50d05` |
| `item_lanca` | 5 908 B `cefc5fedf8593a06bcedd2680712244484869f9ca6a1b348be548104c63be2a5` | 3 109 B `15fdd112fb915a75762c10dbf37e1eb5fbaa7151db971f347b1ff11499c7edbe` |
| `item_machado_guerra` | 10 950 B `eea645e0b9a71985680db8036042d8ae2032b0162980512bb17dce1797b28725` | 8 405 B `ebd559589fab345b64806ad5993a11d12f09d55b8d6554fabb01c94ba4c25c79` |
| `item_maca` | 10 027 B `fbb33d9632cead45f4fef93f4bdb9ceb09f8526a141190c43f9eeb6efc0fc731` | 6 633 B `bb5686c25ae13b4cca697dc322fa8acd463c9794315e8a2de40514c57bb416e3` |
| `item_colar` | 12 079 B `a3360bee2fe5c5e4d910c397cc95a86a7e8967a7c123cae3774116531000f7c8` | 12 411 B `c8c375d05990bdf556c331b94d1714615ba2495d7caaf144e1b82502dc5320d6` |
| `item_broche` | 15 522 B `6545c7a43b6cbbe9dc77136906e9b30d846fdb8b2d1b79ee049cc062ddfabc87` | 14 433 B `da3afe83f07e35c72bd7ddcf6c0f26ee68c8ebf3d125035fa4afeb76e0f6eed3` |
| `item_cinzel` | 10 804 B `b4214fad27453dd27ed5f1ee66e34a921cf73c5ffa765d8ff93c9564770d90a4` | 5 064 B `e93b65808f8b3ea7985e699343ba91d9421ae4b741cb1f9d5937febb47a04c32` |
| `item_castical` | 11 290 B `06e5259a8f5a3f9111a4df3115d915c73339ee6251309be55b95bc10d9254d9e` | 15 334 B `5b60d0d01cc2cf95c30c4033bc8c5dabe6ac54da4d711792c0285333cb864ec9` |
| `item_caixa_joias` | 15 901 B `bc96151e3fa6bbd8734255e8ce77495427715399c4555f718c39e2b972ae3bbd` | 11 134 B `c0c0b5ad4a9bae8adac18ecf4de845cb2a5f4f534a5cbeb982cd553086996d16` |
| `item_calice` | 12 918 B `a87da33001644b551d37dbadeb3900ad4d8f63d4778413d691cfd0fa4368deb1` | 13 352 B `0af57012425f740fc870ed0cab4c0a81436148e10c2cf34558a4750d77f20cf5` |
| `item_fivela` | 12 861 B `b2d62cb21dda9bd11e34f8406b147b1d406b05e7eee5a58af8652bbf7fdf063f` | 6 656 B `71e0b6f617c089f49d37f46483760538426b0dffd4e8b52c744febd6cb8dd70c` |
| `item_kit_guarda` | 11 426 B `91324b1138dc0612ffd79be81ea51f86bc3b7ba828184268b0306339ec83b404` | 12 359 B `e97329cf7a89d49b83156b2bf9063f9d68c9976b7c94840b82c38e20eb7b0a6a` |
| `item_kit_batedor` | 12 246 B `756893e1758a0b38c02aa51a18fff0025121b5a740768ff5b8e398d75194eafa` | 13 852 B `ef1aaa27fd87b0fafba028f411b018114b10cece61386562f33b946e750ffcef` |
| `item_alabarda_cerimonial` | 11 411 B `3639017eadd6b8a457ee79de78436669e3ff7c5bb248703de94e4f8da857e862` | 6 154 B `fded1883f21ffd34910f719f39e04522b35f019491a055e27c690297ead79ea7` |
| `item_espada_gala` | 15 033 B `7600a8447c14df32775f77834bf74f08aa34dee3cae97b907f922119e0240548` | 15 682 B `b5cbd3fbf249b3ba6f34f6c1a85e843a939e97a7f56edf5c7abcd944af676dc3` |
| `item_armadura_gala` | 19 678 B `fcc057780b1d009ef920409cf200c219f81b1bb911662f41566a42044db4d63d` | 19 048 B `a4d9a1ee3c05b4a9c3a822efc620f08ffefa9c9bbe024bee9390e1155faaa4f7` |
| `item_insignia_real` | 11 127 B `bfdc991dedbead14c8fa4b8fbb8ba7a5e5c4b9fb42a2dfb357550f1894c29e2f` | 11 239 B `e05cf9b597d8a75398978227c699d8f0e12943792f0bed142134e56640594f9e` |
| `item_lamina_runica` | 10 617 B `c60730de347a6944afe901d1260bdcd82e7ee5d3e7ce97f34d15e435ff7345ea` | 6 581 B `ce7adeba114ee20703ab814c1bffcc2096743ceea688f560a105bcb8c5d38d1c` |
| `item_escudo_runico` | 14 464 B `53f2d4657d7ece6d78bab6c5fb404b8541c6d7d95f9b038b5e1f296e568579fc` | 15 762 B `25402461aea01926ff0e7eabb39374be0da7fc9bba30267eb3ed1483b4e28b8b` |
| `item_amuleto_arcano` | 10 217 B `3df661dea55f1e37b80858eccfe88e6e1c69c99b4c113130d1ef3d6386948ec6` | 9 735 B `514bcbad9783a7a7bc8aff05b0157899723b08d04affbaae41c0eadf6e67e899` |
| `item_cajado_liga` | 7 058 B `479b6daf7c0ea0c5feff4ec04db335b338c6c253bb59d0e9ae56dc150fc74a77` | 5 128 B `220e3050b31b2b788b90a83f873477bd078889d6af6b5f9f1890553c6692f903` |
| `item_anel_cristal` | 10 008 B `0040366873abd17d5c6d171457fbb756c7c030eb0bfc0fda374b8bd29e84056f` | 8 381 B `eccab50dfd5248bb2870d5925e3d6ac308765fad4a8af8034a2afa95d65a48ea` |
| `item_lanterna_arcana` | 10 358 B `c7f5eff7fb5c923a055aa6ac0e07d7d2ba6298adcf3472f8922dcf3806ab9bd4` | 9 557 B `758733727c5f2204f019892c05e050f5daa82eccd860a9d16f9d2df16d8c9275` |
| `item_liga_celeste` | 15 539 B `8806b829bea13223678e3d43a6dc896767687c94295c6c183eea442068329e2a` | 13 063 B `3e64d6c4e40efaf503fbdf50943b1d167ec9838ef2a7e7fe94a467cab68b0867` |
| `item_panoplia_campeao` | 15 967 B `a20f4e173913e55a893e10459e2b795d794ab921424b625ccda55fd389c3b7c4` | 17 324 B `6ccf29c9b28e89b2c2c7288b31123a517fe36f325959ad573cf3d9fa80a508d3` |
| `item_lamina_estelar` | 7 299 B `a6e84cd0e48e281aeced18bf01b83546d1657d927941e2b4d9682635d5c72cfa` | 6 560 B `5c8c13d5222c2bf04629f7918bc7709c14345042ccae2289b9a407aad9758ed8` |
| `item_coroa_reino` | 14 997 B `7b0fc6e8a2c3c66afb9e5695824092e6d4b5753771cb9a7e5ff5c2d240cb089c` | 19 982 B `230dd22887a5c7aa6fae8744637a210e8270c0239e62e427f5d058e2d7d49e23` |
| `item_estatua_bronze` | 10 157 B `dd478a4ca84eea69e3e755b3d638767e6bdd93432b31ffc5dc5612eae2fea86e` | 11 634 B `8b711a5b75f00c7bfd680e85040866e3ca11fbc48c13c2e77fd2444ed8192156` |
| `item_sino_catedral` | 14 414 B `ee93b18ee8d309166b84b30c3c63bbb0ea848e22508f3122478c5f5740d53c0d` | 14 319 B `fbf7bcb51aeff805e4fd6708189cf79af2963c3e44aa0b0ec349963f75467741` |
| `item_relicario_real` | 17 162 B `d9afa71fe067c85f911d951085f196e6020926c6c5c7821f578a98c6abe058aa` | 11 569 B `b62589f1098517f1676f417cd198ebcd4c37d28e2864999ca7f274c5bd32838a` |
| `item_couro` | 15 724 B `8284089199b2c7e1551d2cddc030c734c181e72c9e823d13124ea6d3b7826d9b` | 16 201 B `86775b3327c5baccc66382286273b35550b8223eefe8bbcc19458b93220cc6b6` |
| `item_placa` | 14 985 B `babdf264dff20aba7aab7a1d7525bd52dbbb243312db3169bc35d7bd67733ec2` | 13 226 B `676bfe55f3023cdb77db9f8e3b697b7c619c17215151d76dc61b50d3cefdc60d` |
| `item_rebite` | 13 202 B `2e42600e72b7e1093c912598e1962bd647628f0f276023b6f9a834d945fc6ba2` | 15 010 B `0fbd3180af57ce48b29a06fd531d86a73695c60eb620e1e39b9b7e23c6352f89` |
| `item_gema` | 12 682 B `6a141edb276881f62797f2d0fc0878a1257d83f056ba308ff4ac149e25f40f21` | 13 059 B `4c86e99b32630e4fa60b76f10f9311c9aca3097f0474b61c7063462243c3ecea` |
| `item_liga` | 10 938 B `ffedb5068bc4c9832eb198f813bbf2dc19ba289abc00f8636b4b21b0110376a3` | 10 940 B `bae83a3911a53aab9d109dde294c14348426a5ea8b0abf395dfeb57214feec9f` |
| `item_minerio_runico` | 13 521 B `11396a779a0f51b1298f2d1cf1b2f67ff3b45493db77b3b8e9b6ae554a4c82b4` | 15 466 B `f182d3848c2ef7624bfe2421623d740a066017cda7f30dc2dcae66a99c3a37fe` |
| `item_cristal` | 12 868 B `522654fca8733bda1ca606ab7e515e1ac66ce70f631f6d446777486a02e64b63` | 14 800 B `605fd5a6ab28e7767e0a257d3c50b56486a74d24819520769726f36324926596` |

## 9. Lote 5A — 7 workers + 4 clientes (Leva 9, raia ARTE TRIPO, 2026-10-13)
- **Aval:** Vinicius, 2026-10-13, neste ciclo do ORQUESTRADOR ("criar o que for preciso", "tudo está autorizado"). Orçamento 800 créditos (11 × 65 = 715 + folga). Saldo visto pelo coordenador às ~05:30 do relógio dele: 17 285. Teto: saldo final ≥ **16 485**.
- **Operador:** worker da raia ARTE TRIPO (persona 06 ai-art-director) pelo Chrome do Vinicius (Claude in Chrome, aba própria, fechada no fim), sessão já logada; nenhuma senha digitada. Horas abaixo = relógio deste PC.
- **Receita (conferida no painel antes da 1ª geração e o preço "Gerar 45" / "Auto Rig 20" conferido em cada clique):** imagem no gerador do site (Nano Banana 2, 1:1, 4 imagens, grátis: contador "Free 548/1000") → Modelo HD **H3.1 - Máx. qualidade**, qualidade de malha Ultra, "Completar com IA" desligado, textura ligada **2K** (não 4K/8K), remover iluminação, PBR, triângulos, 8 000 polígonos, Gerar em Partes e Textura 8K desligados, **Privacidade = Privado** → Auto Rig humanoide, predefinição de esqueleto **Mixamo** → Exportar FBX, predefinição **Mixamo**, textura 2k, esqueleto ligado (0 crédito). Uma página nova por peça (o "Gerar 3D" de cada imagem abre o painel de modelo limpo).
- **Prompts:** assunto + `[S]` + `[I]` do cabeçalho do `ASSETS.md`, separados por vírgula; texto exato do assunto em `ASSETS.md` §11.1.

| Data/hora | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| 2026-10-13 02:56 | Abertura do gerador; painel conferido (HD, H3.1, 2K, 8 000, triângulos, Ultra, Privado, "Gerar 45") | **17 285** | 0 | igual ao visto pelo coordenador |
| 02:56–02:59 | 11 lotes de 4 imagens (Nano Banana 2, 1:1), um por personagem | 17 285 | 0 (grátis) | escolhas no registro abaixo |
| 03:02 | Oficial: imagem #2; Modelo HD | **17 240** | 45 | task `51f6881e-202d-4d73-86d6-962e7f120a0b` |
| 03:03 | Balconista: imagem #2; Modelo HD | **17 195** | 45 | task `019365ae-1ad8-4372-9927-6e81f982a2a0` |
| 03:04 | Almoxarife: imagem #4; Modelo HD | **17 150** | 45 | task `5b05e23a-b609-4fb5-96f7-7df574e6647b` |
| 03:04 | Mestre de obras: imagem #3; Modelo HD | **17 105** | 45 | task `d4b9599d-31b2-483b-aac1-45d49156a022` |
| 03:05 | Fundidor: imagem #2; Modelo HD | **17 060** | 45 | task `a3979882-e9c8-4d9e-aa52-f6de2210f6de` |
| 03:06 | Mensageiro: imagem #2; Modelo HD | **17 015** | 45 | task `cec47b2d-584b-483c-876c-209cce428a1d` |
| 03:06 | Gerente: imagem #4; Modelo HD | **16 970** | 45 | task `75eb6a69-ace7-488b-b06b-81de8521d0a7` |
| 03:07 | Camponês: imagem #3; Modelo HD | **16 925** | 45 | task `63c70b4b-b016-43c3-9cc9-2beed7f53d18` |
| 03:08 | Guarda real: imagem #1; Modelo HD | **16 880** | 45 | task `b0710f62-656b-422d-9400-d313a32466b1` |
| 03:09 | Emissário: imagem #2; Modelo HD | **16 835** | 45 | task `27e8d107-fd07-4cff-b1b4-04a6c0dbd586` |
| 03:09 | Colecionador: imagem #3; Modelo HD — **11 modelos, 495 créditos** | **16 790** | 45 | task `1416e02d-f683-4f05-8100-66521774e0c7` |
| 03:10 | Oficial: Auto Rig humanoide Mixamo (URL `workspace/rigging/<task>`) | **16 770** | 20 | |
| 03:11 | Balconista: Auto Rig Mixamo | **16 750** | 20 | |
| 03:11 | Almoxarife: Auto Rig Mixamo | **16 730** | 20 | |
| 03:12 | Mestre de obras: Auto Rig Mixamo (1º clique caiu fora do botão depois que a janela do Chrome encolheu; não disparou nem cobrou) | **16 710** | 20 | |
| 03:13 | Fundidor: Auto Rig Mixamo (imagem em pose A leve; o rig aceitou) | **16 690** | 20 | |
| 03:13 | Mensageiro: Auto Rig Mixamo | **16 670** | 20 | |
| 03:14 | Gerente: Auto Rig Mixamo | **16 650** | 20 | |
| 03:14 | Camponês: Auto Rig Mixamo | **16 630** | 20 | |
| 03:15 | Guarda real: Auto Rig Mixamo | **16 610** | 20 | |
| 03:15 | Emissário: Auto Rig Mixamo | **16 590** | 20 | |
| 03:15 | Colecionador: Auto Rig Mixamo — **11 rigs, 220 créditos** | **16 570** | 20 | |
| 03:16–03:27 | Exportar FBX (Mixamo, 2k, esqueleto) dos 11 → `Downloads/<id>_mixamo.zip` → movidos para `arte/tripo/<id>/` | **16 570** | 0 | aba fechada às 03:28 |
| 03:28 | **Lote 5A fechado na geração: 11 modelos + 11 rigs = 715 créditos** (17 285 → 16 570; teto 16 485 respeitado, sobra 85 do orçamento) | **16 570** | **715** | |

### Registro por asset (licença tripo_pago, visibilidade privada, plano pago; termos como no §2)
Imagem de origem = `tripo-data.rg1.data.tripo3d.com/generate-image/20261013/<lote>/image_<n>.png` (URL assinada; registrado só o caminho). Faces = contagem exibida no visualizador do Tripo.

| asset | papel | imagem escolhida (lote/arquivo) e por quê | task | faces | créditos | zip (bytes · SHA-256) | fbx (bytes · SHA-256) |
|---|---|---|---|---|---|---|---|
| oficial | worker Oficial (Smith) | `236131c9-94f9-48e2-ba12-e29d007a739a/image_1.png` (#2 de 4): braços na horizontal, faixa verde com pontas, tenaz atrás do corpo; #1/#3 braços em A, #4 avental escuro igual ao do ferreiro | `51f6881e-202d-4d73-86d6-962e7f120a0b` | 7 772 | 65 | `arte/tripo/oficial/oficial_mixamo.zip` 22 418 427 · `9e6543baa2f62134af311f6d92531799b9c4fdb24e7c1be7b2068fdda0686daf` | `tripo_convert_c43966ac-bef7-46bc-8d99-7cf4f86acc0e.fbx` 11 558 160 · `318bcdd5e1ae26582d5684647f094a29ee09e57d08e4e1c65e0841fcb715aba5` |
| balconista | worker Balconista (Clerk) | `e6308455-a4d6-4db1-a3be-00751d414811/image_1.png` (#2 de 4): pose T frontal, livro na mão estendida, bolsa de moedas, pena, calça e botas; #3 abraça o livro (braços fechados), #4 de 3/4 | `019365ae-1ad8-4372-9927-6e81f982a2a0` | 7 450 | 65 | `arte/tripo/balconista/balconista_mixamo.zip` 23 771 987 · `6c85f26d55811ec855d27e9fb2b77f929818da1333de3b7c81256ae9ce55b181` | `tripo_convert_a15da9fe-7596-42a5-a325-83d9ff4cb2fb.fbx` 12 213 360 · `9dfc002c377b58e65dd598927c91c654e51df3c95f2a45f4fdf8082b199261df` |
| almoxarife | worker Almoxarife (Quartermaster) | `ea6bfd02-44fa-426b-b21d-b1e16a974a61/image_3.png` (#4 de 4): o mais atarracado, caixote aparece atrás do ombro, argola de chaves grande; #1 braços em A, #3 caixote quase invisível | `5b05e23a-b609-4fb5-96f7-7df574e6647b` | 7 431 | 65 | `arte/tripo/almoxarife/almoxarife_mixamo.zip` 20 698 266 · `96a8f71b18891df027d489930e174216b9b42f37846ac53cdcf377f7bd398a8c` | `tripo_convert_ac920960-0e1f-4403-8279-2b575e7e3560.fbx` 10 677 952 · `442dded685d07ca07404684838986e98d8155ebd414947922d7ac51b69fa98a1` |
| mestre_obras | worker Mestre de obras (Foreman) | `b7c2db67-5884-44b9-ac31-a5822ccf2066/image_2.png` (#3 de 4): malho atrás de um ombro e corda no outro, túnica laranja chapada, bigode, pose T limpa | `d4b9599d-31b2-483b-aac1-45d49156a022` | 7 305 | 65 | `arte/tripo/mestre_obras/mestre_obras_mixamo.zip` 22 505 299 · `ea1dc881e2013b0fac06b05e0a94b20abc8ff958dea47edb12eddf791f9bafe4` | `tripo_convert_2fe1da5c-869f-4fbe-8423-d7a8a9093886.fbx` 11 578 912 · `10d570c3c37a5bbc1638f28bce84eda8e0a2d89aa7033d54f589949e48239b61` |
| fundidor | worker Fundidor (Smelter, Wave C) | `1ba74c44-5cf3-46f9-9f4b-a591a71d0a25/image_1.png` (#2 de 4): manoplas marrons contra o casaco cinza, viseira erguida, tenaz atrás, fuligem; #1/#4 luvas da cor do casaco, #3 casaco esverdeado | `a3979882-e9c8-4d9e-aa52-f6de2210f6de` | 7 124 | 65 | `arte/tripo/fundidor/fundidor_mixamo.zip` 26 266 846 · `bb38527cced325bc0c21586116e6c9c893329f37c1f7df204eb4dbe1f83def31` | `tripo_convert_51f05180-5552-4cf2-b437-bc209015beac.fbx` 13 486 096 · `2cbf6b8750072e80ec9c05ed5843667df8cc6cd80aaaaa24f9c5a4fd213144ed` |
| mensageiro | worker Mensageiro (Courier, Wave C) | `0b2c0097-55a1-4e61-b5f9-3ab132bbee42/image_1.png` (#2 de 4): pose T horizontal, boné azul com pena, sacola atravessada, pergaminho na mão; #1 braços em A | `cec47b2d-584b-483c-876c-209cce428a1d` | 7 682 | 65 | `arte/tripo/mensageiro/mensageiro_mixamo.zip` 23 249 484 · `ced5e2d5e7922e9c369ca4d775570423c37181c4bda325a6e76d2fdc476921f1` | `tripo_convert_7d72b0ba-7a6d-4663-b314-337fa5174a66.fbx` 11 960 384 · `483be957ddde937d7e0493c3de2ce15eeaca8856338efe167b8850c6e40b4a2e` |
| gerente | worker Gerente (manager) | `44faab6e-771a-4a8d-a39a-3f07b9863e8d/image_3.png` (#4 de 4): livro deitado na mão estendida (fica no osso da mão), casaca aberta com as pernas à mostra; #1 livro pendurado no quadril (iria para a perna), #2 braços em A, #3 livro nas duas mãos | `75eb6a69-ace7-488b-b06b-81de8521d0a7` | 7 422 | 65 | `arte/tripo/gerente/gerente_mixamo.zip` 24 815 671 · `2235c1eefdfec2b0847524ed2596c75d62670d1a2b7bb9b7998ef538cc7a6715` | `tripo_convert_126c8793-1f7c-486a-8c42-a162465c7962.fbx` 12 754 912 · `419fa91d49454dcb93467204cf13d7e7f0d9086690b743a68da8f8700e63efbe` |
| campones | cliente Camponês (distrito 2+) | `de5079d7-e6f8-4a7d-9295-da4ba1b930c1/image_2.png` (#3 de 4): rosto redondo sorridente, chapéu de palha largo, forcado atrás, pose T | `63c70b4b-b016-43c3-9cc9-2beed7f53d18` | 7 504 | 65 | `arte/tripo/campones/campones_mixamo.zip` 22 619 108 · `bb6ed9dd08d6ac2618b1c276d4bb76aee50341059746d85445069cbf11f9b81f` | `tripo_convert_836c7883-4549-4feb-9515-91b30f5b72f5.fbx` 11 638 432 · `e1358e42e8495c04d545a10ce80cc35e45b5180ccd00486471a96c07a8e18c42` |
| guarda_real | cliente Guarda real | `d9365ca8-bba2-4631-9154-f35f3f10f2cf/image_0.png` (#1 de 4): braços mais horizontais, lança atrás do corpo, escudo redondo no antebraço esquerdo, elmo de abas; #2 braços em A | `b0710f62-656b-422d-9400-d313a32466b1` | 7 604 | 65 | `arte/tripo/guarda_real/guarda_real_mixamo.zip` 32 277 966 · `664fc022b94b2ab2b45e8ec51a38d21961d45f34d2f2a90432d1cb73c544c092` | `tripo_convert_cf4ac22a-728f-4f0d-aaf1-b3742020255a.fbx` 16 493 424 · `6ebc75c8cea6664b35912796bde9fe90481ef5a4fc3b856d03b458afb414640a` |
| emissario | cliente Emissário real | `c2c328d6-4857-49b0-b498-34899ea7c16c/image_1.png` (#2 de 4): capa curta até o quadril com brasão bigorna + chama (heráldica própria), pergaminho com selo vermelho na mão, simétrico; #3/#4 capa caída de um lado até a coxa | `27e8d107-fd07-4cff-b1b4-04a6c0dbd586` | 7 843 | 65 | `arte/tripo/emissario/emissario_mixamo.zip` 28 899 567 · `0d9ed42c10bd43df88a9b881d4561fa973f684bd2a9a7bde02647cf244b16679` | `tripo_convert_d6352d29-0965-48d8-9e53-75622e11cf03.fbx` 14 801 520 · `edb0bbe179424701275973b97191c86beb8bac68c06ea23577c1d2b5eee9ff96` |
| colecionador | cliente Colecionador (Renome 2) | `9249366e-c7e1-4219-88ef-e42a31b463a2/image_2.png` (#3 de 4): casaca roxa com acabamento dourado, anéis e lupa visíveis, cartola com pena, pose T | `1416e02d-f683-4f05-8100-66521774e0c7` | 7 447 | 65 | `arte/tripo/colecionador/colecionador_mixamo.zip` 25 969 214 · `5ac4115f4b4ae322516aa25bac48e147fefbbe35cf0fe35f86357065e3dc48b1` | `tripo_convert_a352f357-a9a0-41ed-8448-c3f5ae73c6e2.fbx` 13 336 160 · `be1d313dbe72948f62516dbc97bdcbe5a92b091738b37d3b44f50ccbba0c4537` |

Hashes: zip e FBX em `arte/tripo/<id>/` (fora do git pelo `.gitignore`, como os lotes anteriores); cada `.fbm/` traz 5 PNG `<id>_mixamo_{basecolor,normal,metallic,roughness,rm}.PNG`.

### Arquivos do Lote 5A (sprites, 0 crédito, sem rede)
- **Render:** Blender 5.2 headless, `client/tools/render_sprites.py` sem mudança, um personagem por vez (antes de cada um, `tasklist` sem `Unity.exe`/`blender.exe`). Padrões do script (elev 60°, 4 direções S/W/N/E, 12 fps, célula 128 px, ppu automático por modelo, luz flat + contorno + cavidade, AA 8, `--max-frames 32`, retarget por delta de repouso). Clipes: workers = `client/Builds/anims_sets/ajudante/` (Idle, LookingAround, Walking: os do ajudante); clientes = `client/Builds/anims_sets/guerreira/` (Angry, Idle, LookingAround, Thankful, Walking, Waving: os do Lote 4). SHA dos clipes = `arte/mixamo/` (§2).
- **Comando** (raiz do projeto; `contact.png` sai de `Resources` para `client/Builds/sprites_contact/<id>.png`):
```
timeout 900 "/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/render_sprites.py -- --model arte/tripo/<id>/<FBX> --anims client/Builds/anims_sets/<ajudante|guerreira> --out client/Assets/_FS/Resources/Sprites/<id>
```
- `.meta` de cada PNG, do `meta.json` e da pasta: cópia dos do minerador/ajudante (TextureImporter com máximo 4096, ASTC no Android, DXT5 no Windows), só com **GUID novo** por arquivo.

| Id | FBX (`arte/tripo/<id>/`) | anims | ppu · pivô y | `Resources/Sprites/<id>/` |
|---|---|---|---|---|
| oficial | `tripo_convert_c43966ac-bef7-46bc-8d99-7cf4f86acc0e.fbx` | ajudante | ppu 121.33 · 40.5 px | 3 PNG + `meta.json`, 3 763 300 B |
| balconista | `tripo_convert_a15da9fe-7596-42a5-a325-83d9ff4cb2fb.fbx` | ajudante | ppu 113.29 · 36.2 px | 3 PNG + `meta.json`, 3 343 354 B |
| almoxarife | `tripo_convert_ac920960-0e1f-4403-8279-2b575e7e3560.fbx` | ajudante | ppu 122.54 · 40.5 px | 3 PNG + `meta.json`, 3 954 981 B |
| mestre_obras | `tripo_convert_2fe1da5c-869f-4fbe-8423-d7a8a9093886.fbx` | ajudante | ppu 113.55 · 36.5 px | 3 PNG + `meta.json`, 4 097 432 B |
| fundidor | `tripo_convert_51f05180-5552-4cf2-b437-bc209015beac.fbx` | ajudante | ppu 124.62 · 34.2 px | 3 PNG + `meta.json`, 3 993 967 B |
| mensageiro | `tripo_convert_7d72b0ba-7a6d-4663-b314-337fa5174a66.fbx` | ajudante | ppu 125.13 · 36.4 px | 3 PNG + `meta.json`, 3 507 364 B |
| gerente | `tripo_convert_126c8793-1f7c-486a-8c42-a162465c7962.fbx` | ajudante | ppu 125.33 · 38.8 px | 3 PNG + `meta.json`, 3 539 292 B |
| campones | `tripo_convert_836c7883-4549-4feb-9515-91b30f5b72f5.fbx` | guerreira | ppu 89.22 · 35.9 px | 6 PNG + `meta.json`, 6 251 791 B |
| guarda_real | `tripo_convert_cf4ac22a-728f-4f0d-aaf1-b3742020255a.fbx` | guerreira | ppu 105.67 · 35.1 px | 6 PNG + `meta.json`, 6 607 247 B |
| emissario | `tripo_convert_d6352d29-0965-48d8-9e53-75622e11cf03.fbx` | guerreira | ppu 98.02 · 28.3 px | 6 PNG + `meta.json`, 6 459 918 B |
| colecionador | `tripo_convert_a352f357-a9a0-41ed-8448-c3f5ae73c6e2.fbx` | guerreira | ppu 123.47 · 33.6 px | 6 PNG + `meta.json`, 6 877 158 B |

Total: 45 PNG, 52 395 804 B. Workers: Idle e LookingAround 32 quadros (4096×512) + Walking 16 (2048×512). Clientes: Angry, Idle, LookingAround e Thankful 32 + Walking 16 + Waving 6 (768×512). Os SHA abaixo são dos arquivos entregues (o AA do Workbench pode variar alguns níveis num re-render, §6).

| Arquivo (`client/Assets/_FS/Resources/Sprites/`) | px | bytes | SHA-256 |
|---|---|---|---|
| `oficial/Idle.png` | 4096×512 | 1 476 676 | `3c4b95599b90fa24dda0a798bed4159591f4fa926bc90417017ad12a23a06bb2` |
| `oficial/LookingAround.png` | 4096×512 | 1 505 682 | `95ea6056e97febd7a2addd17582ce0b0eb9e987e179d8f43c4ef7174bf8673ce` |
| `oficial/Walking.png` | 2048×512 | 780 942 | `d5d59e382a6709aaf5810ee9baaf141397f3ca1151afe9cf83f857b46ff17030` |
| `balconista/Idle.png` | 4096×512 | 1 302 742 | `b9d661c8bb05229f5b3cca0338bc1e01c36b03b2c1c3386388e8bc8e795ee54f` |
| `balconista/LookingAround.png` | 4096×512 | 1 350 609 | `5fad567ba9498bdfd554a0206f5b3f19717d1e56a6273724733495e745ae7d7d` |
| `balconista/Walking.png` | 2048×512 | 690 003 | `bd36db03239ab5e24fbbf0cdd53dc194353f3c53f63d066c70999a75ee95ab8a` |
| `almoxarife/Idle.png` | 4096×512 | 1 524 593 | `8be52ad961228c4c3a63c34c710838250fe2869c21f952909682ae983c2fccb5` |
| `almoxarife/LookingAround.png` | 4096×512 | 1 613 158 | `bcc1dfeba6961cf7ec4a991e234e81b94b6cefcbd733f096420b86ffc5bed2f7` |
| `almoxarife/Walking.png` | 2048×512 | 817 230 | `f943df650d2993506aa0ec0df7da4cc08493471da063acbcd83bd462d03532c8` |
| `mestre_obras/Idle.png` | 4096×512 | 1 610 129 | `ee09a441808626bc271aef7d182d137bc584b7f7acfc39595e70550edbef4175` |
| `mestre_obras/LookingAround.png` | 4096×512 | 1 659 736 | `24b726f624a5b650792f97d578d37d11fa38a10344b313252aabf57fc35b62c6` |
| `mestre_obras/Walking.png` | 2048×512 | 827 567 | `5c60f3f17ee42b1605ab51785d018efb43e38a90562b900a38a1d5a574245109` |
| `fundidor/Idle.png` | 4096×512 | 1 555 674 | `b58cf405384326255a6e8e8103e2abfd14953d39572c8e173dfdec2a2ac10dca` |
| `fundidor/LookingAround.png` | 4096×512 | 1 610 278 | `7456aa6005a87b4c88f046b947ebdebfc7d4678083912693d1ee8dd02f4b8c1e` |
| `fundidor/Walking.png` | 2048×512 | 828 015 | `a99685cf97c8f9d33a1ae56542590d36edd3b43d7bd7ed4158c7ce89ba7108e9` |
| `mensageiro/Idle.png` | 4096×512 | 1 378 268 | `9b2f1b3ef504688a48cda81b5731243accb06c333b001176a7426958b6f1a4bd` |
| `mensageiro/LookingAround.png` | 4096×512 | 1 415 388 | `e4d0158cba4103ab78e567d7c2e6b4c2f873e6d585e716d351228b0a5ec6c962` |
| `mensageiro/Walking.png` | 2048×512 | 713 708 | `7c77c418de269e09ac678faeadafab2b8bd999e37180b792bead31596e989868` |
| `gerente/Idle.png` | 4096×512 | 1 386 527 | `734bb2a06735d7137f2f0ba1e27b9bde0a78e84ed12d1f3ecdadd147a58c6c9b` |
| `gerente/LookingAround.png` | 4096×512 | 1 423 365 | `bcef37fc693e7a55175f413c5dc27b55eb0b263360072f4058fdd04c27bd4051` |
| `gerente/Walking.png` | 2048×512 | 729 400 | `174c63b55173a1421c1b549e17308b582afa4e62698576c752cb7a62fa2fe32d` |
| `campones/Angry.png` | 4096×512 | 1 320 283 | `0ca2987aab592c964775d0699ec6bc35d93f03ae74bd2eac73b1afa3cac855b3` |
| `campones/Idle.png` | 4096×512 | 1 323 572 | `61eae78c259cabd36c054e8a9aac6abca4eaeb55c8d3ad9b49211d32abbf634a` |
| `campones/LookingAround.png` | 4096×512 | 1 360 930 | `c08edf5d25265549d26c93e732215cd7140449493bb2bd543318da6fafecc241` |
| `campones/Thankful.png` | 4096×512 | 1 303 470 | `474ef2de35cc66a3faa541dca9460bcc4b400392cc023a0a242e3c9129f5fa29` |
| `campones/Walking.png` | 2048×512 | 691 600 | `ca308a018a89c8bbb183df0c7baf76e52d1331bcfb3d29447449bcf21e275193` |
| `campones/Waving.png` | 768×512 | 251 936 | `0d4376ba00d79881095a9d1be0f53ccf7250474d915ededaad16ab5541a1a13e` |
| `guarda_real/Angry.png` | 4096×512 | 1 407 555 | `eefa6c102c184984f6eee14531f88aa3bbbaf1d4cace62d80a9caf336a610425` |
| `guarda_real/Idle.png` | 4096×512 | 1 391 719 | `0cf405e1d28a54ad0e559ce3e11b082b6649b595e4a65f926f2fcd757e2a57da` |
| `guarda_real/LookingAround.png` | 4096×512 | 1 432 430 | `94b6cc6f80e1aa63eb91711f2cbee19a2dd5683e82b3161c58f38f6a0b66b68b` |
| `guarda_real/Thankful.png` | 4096×512 | 1 376 621 | `ec25fc0bc3522f90626d145baa9ba206dabeb97260ac2afcee12985aaa04b715` |
| `guarda_real/Walking.png` | 2048×512 | 724 912 | `6028ec0b6c419f0ae2a6ae58f14707e6e1ad58d2f0b1a7e890b0eba0e74789a5` |
| `guarda_real/Waving.png` | 768×512 | 274 010 | `487586844024acac35dc62d8645307459e7949496a41ea7baad69359bba6e6f1` |
| `emissario/Angry.png` | 4096×512 | 1 378 429 | `f506beebccb91e0bcad7f0f1bca0648f44b8029233d68a19e42225004f6ad359` |
| `emissario/Idle.png` | 4096×512 | 1 347 807 | `ff2ccc152b35a264e044b9899f4b854ecc306a6bf5137ad414a227ee4d8dca75` |
| `emissario/LookingAround.png` | 4096×512 | 1 422 362 | `bd031fc7e6038243a2e43344624181ee3f5507acf01ea9667a6c3d12cce52c7a` |
| `emissario/Thankful.png` | 4096×512 | 1 350 760 | `a7a3c311ac04537b34eadd43224bb43e9150b8b052f4405a950744dff846b97a` |
| `emissario/Walking.png` | 2048×512 | 693 658 | `a9708411524f7e5fd2e436b1c5223ef08e3277415fe054cb0b0fac95f0b0d21f` |
| `emissario/Waving.png` | 768×512 | 266 902 | `c0e4d83df56ca500621ae6fb8535b41dd59458b62a7135eeb020202f5d340aa1` |
| `colecionador/Angry.png` | 4096×512 | 1 461 025 | `179bcddeb72166fe0e53221bfa6efdddd3e9b678d3501ce3d7be2aaca98dbe89` |
| `colecionador/Idle.png` | 4096×512 | 1 448 867 | `1c0c693995dc4d514c6f2d72144773e06922ee885b749420fb8644c54d7ae061` |
| `colecionador/LookingAround.png` | 4096×512 | 1 486 787 | `eafeb27bdc6b3400fe1c53270a754105291c46eb2f643e5966c42eb8055d6b25` |
| `colecionador/Thankful.png` | 4096×512 | 1 453 054 | `7a7dee65e88b8cab93c7b089a337eabe81cf2709ce97be733c7af4ebcd9a1bec` |
| `colecionador/Walking.png` | 2048×512 | 736 811 | `d12e36b3ee301b2b8654eb148865fe157a5cca27c7231a4216af259510564f84` |
| `colecionador/Waving.png` | 768×512 | 290 614 | `855f3359314adf87f13ff31d58430075551b1e9b561be0bb351ccc725bded3c0` |

Folhas de conferência (`client/Builds/sprites_contact/`, só QA, fora do APK):

| Arquivo | px | bytes | SHA-256 |
|---|---|---|---|
| `v09_lote5.png` | 2270×2990 | 2 369 190 | `4ba4f17707913ba5c9b4c51de780d8abb0791490760065e858c25f355a39dbf0` |
| `oficial.png` | 384×128 | 37 342 | `40c1fb00967a425ad80766bccc3ae50d65799309e89da17fe321b2ed83964698` |
| `balconista.png` | 384×128 | 33 614 | `8eb5095f359d26931fb5e5f02e3bcff938490548d915a6b1e4718a18a290e756` |
| `almoxarife.png` | 384×128 | 40 185 | `e3b22198d2b06c7790917a75eeb731c18086461c137bfd74b03da01b4d01ec58` |
| `mestre_obras.png` | 384×128 | 40 702 | `c3800c1c7c23b5f74e4b79756feb25b18e499eba28c1133b4a7ddaba45949e7a` |
| `fundidor.png` | 384×128 | 37 071 | `d8ccc3ca143cf9c4365fef3291ff41c197375fbf4792ca3f85924c5e443f32e7` |
| `mensageiro.png` | 384×128 | 35 559 | `ffb21c46372219b2d6070589a28c73d2aa1ce19d26e6e3f198f575e807468663` |
| `gerente.png` | 384×128 | 36 395 | `e2224f0342290bac60a8e4ed27bb767eb30ea40c7ff43261b7523d4ce8642ec0` |
| `campones.png` | 768×128 | 62 484 | `4fa88ad4265ab3a905b48748a448019f94ad5d5075465db951da36ee3619debb` |
| `guarda_real.png` | 768×128 | 71 047 | `4bab9773b9cdf5fc873b2ef4144a245eb754cfe87ed128e2ccd9cb29f4b46195` |
| `emissario.png` | 768×128 | 70 701 | `0f55dae48b44d0073e20ecfff3dcf8571c8683a51fd21d4315c37dd33d1982cf` |
| `colecionador.png` | 768×128 | 74 908 | `07a74d268de7e2482fc0ba98ad8382febd2cd96ddc3404b6f9c12a28fedc42c7` |
