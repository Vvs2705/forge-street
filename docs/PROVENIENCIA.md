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
