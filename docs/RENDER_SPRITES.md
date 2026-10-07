# RENDER_SPRITES — personagem 3D rigado → folhas de sprite 2D top-down

- **Estado (2026-10-06):** IMPLEMENTADO + TESTADO com o humanoide-placeholder do COE (Idle + Run, 4 direções). **Não integrado**: nada em `Assets/`; a view do jogo (`Art.cs`) continua 100 % procedural. Zero créditos gastos.
- **Arquivos:** `client/tools/render_sprites.py` (Blender 5.2 headless, 198 linhas) · `client/tools/render_sprites.ps1` (wrapper ASCII, PowerShell 5.1) · amostra em `client/Builds/sprites_test/` (Idle.png, Run.png, contact.png, meta.json).

## Como usar
```
.\client\tools\render_sprites.ps1 -Teste [-TestDir D:\tmp\fs]     # placeholder do COE -> <TestDir>\out (Idle + Run, 4 dir)
.\client\tools\render_sprites.ps1 -Model arte\heroi.glb -Anims arte\mixamo\ -Out client\Assets\Resources\Sprites\heroi -Dirs 8 -Scale 95
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python-exit-code 1 --python client\tools\render_sprites.py -- --model M.fbx --anims pasta\ --out saida\
```
Exit 0 + linha `SPRITES_OK` no log; exit 1 + `ERRO ...` na falha (osso faltando, modelo sem armature, pasta sem clipe). Caminhos curtos (MAX_PATH).

| Parâmetro (.ps1 / .py) | Padrão | O que faz |
|---|---|---|
| `-Model` / `--model` | — | FBX ou GLB rigado (1 armature + malhas). GLB traz textura embutida; FBX do Tripo aponta para `.fbm` que não vem |
| `-Anims` / `--anims` | — | pasta com **1 FBX por clipe** (Mixamo "Without Skin" ou placeholder); nome do arquivo = nome do clipe |
| `-SelfAnims` / `--self` | off | usa as actions já dentro do modelo (take do FBX/GLB = clipe; Mixamo chama o take de `mixamo.com` — prefira `--anims`) |
| `-Elev` / `--elev` | 60 | graus da câmera sobre o chão (90 = de cima; 45 = 3/4 clássico) |
| `-Dirs` / `--dirs` | 4 | 4 → S, W, N, E · 8 → S, SW, W, NW, N, NE, E, SE (**horário a partir de S**; S = de frente para a câmera = para baixo na tela) |
| `-Fps` / `--fps` | 12 | amostragem; clipe de 30 fps é reamostrado (Idle 2,5 s → 30 frames; Run 0,67 s → 8) |
| `-Size` / `--size` | 128 | px por célula (quadrada) |
| `-Scale` / `--scale` | 0 | px por metro; 0 = ajusta o personagem à célula (todas as folhas do mesmo personagem compartilham ppu e pivô). Avisa `AVISO recorte` se não couber |
| `-Yaw` / `--yaw` | 0 | graus somados a toda direção, para modelo que não olha para −Y no Blender (= +Z no Unity) |
| `-Light` / `--light` | flat | `flat` cor plana + contorno + cavidade · `matcap` toon 2 tons (`toon_light.exr`) · `studio` sombreado |
| `-Aa` / `--aa` | 8 | antialias do Workbench (`OFF` para pixel art crua) |
| `-Shadow` / `--shadow` | off | auto-sombra do Workbench (não há sombra no chão: sem shadow catcher; desenhe a elipse no Unity) |
| `--src-fps` | 30 | fps dos clipes de origem (Mixamo e placeholder = 30) |

## Saída
- `<Clipe>.png` RGBA transparente: **linha = direção (linha 0 em cima), coluna = frame**; `contact.png` = 1º frame de cada clipe (direção S) para conferir no olho.
- `meta.json`: `size, fps, dirs, dir_order[], row0:"top", elev, yaw, ppu, pivot_px[x,y], pivot[0.5, y/size] (normalizado, origem embaixo-esquerda, como Sprite.Create), clips[] {name, file, frames, loop, duration_s, src_frames}`. `loop` vem do nome (idle|walk|run); o Unity pode sobrescrever.
- Pivô = **origem do modelo** (pé no chão, contrato V02 do COE / `pivot_to_center_bottom` do Tripo). Com câmera oblíqua a sola do pé de apoio fica na linha do pivô e a ponta do pé (ou a perna que balança para a câmera) aparece abaixo dela — correto para 3/4.
- Técnica: **um render por clipe** — cada célula é uma cópia da malha avaliada no frame, girada em Z e deslocada no plano da câmera ortográfica. Sem PIL/numpy, sem PNG temporário.

## Resultado do teste (placeholder 1,10 m, PC, 2026-10-06)
Idle 30 frames × 4 dir = 3840×512 px em 3,2 s (inclui aquecer o render) · Run 8 × 4 = 1024×512 em 0,2 s · total do `.ps1` 6–7 s (5 s é gerar o placeholder). Ajuste automático: ppu 99 px/m, pivô (64, 34) px, boneco com 65–100 px de altura na célula, margem mínima 5 px, 0 px encostando na borda (medido célula a célula). Prova vermelha: clipe com `mixamorig:Hips` → `ERRO armature nao bate ... 2 osso(s) faltando`, exit 1.

## Integração atual (2026-10-07)
Os 20 conjuntos já estão integrados em `Assets/_FS/Resources/Sprites/`. `Scripts/View/SpriteSheet.cs` lê `meta.json` e mantém o cache; `WorldView.cs` escolhe clipes/estados e preserva a reserva procedural. `Editor/SpriteImport.cs` importa com máximo de 4096, sem mipmaps, ASTC 6×6 no Android. Os halos de compressão ainda precisam de inspeção em aparelho real.

## Exemplo histórico de consumo (a implementação vigente é SpriteSheet.cs)
`Resources/Sprites/<nome>/<Clipe>.png` + `meta.json`. Exemplo simplificado:
```csharp
var tex = Resources.Load<Texture2D>("Sprites/heroi/Run"); var meta = JsonUtility.FromJson<SpriteMeta>(Resources.Load<TextAsset>("Sprites/heroi/meta").text);
Rect cel = new Rect(i * meta.size, (meta.dirs - 1 - d) * meta.size, meta.size, meta.size);        // linha 0 esta no TOPO do PNG
Sprite s = Sprite.Create(tex, cel, new Vector2(meta.pivot[0], meta.pivot[1]), meta.ppu);          // ppu: 1 m do Blender = 1 unidade
int frame = loop ? (int)(t * meta.fps) % n : Mathf.Min((int)(t * meta.fps), n - 1);
int d = (Mathf.RoundToInt(-Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg / (360f / meta.dirs)) % meta.dirs + meta.dirs) % meta.dirs;  // v = velocidade XY
```
Cache os `Sprite` por (clipe, d, i) como o `Art.Get` já faz; parado → último `d`. Para 1 m ≈ 95 px na tela de 1080 use `-Scale 95` (ou deixe o ppu automático e escale o GameObject).

## Receita histórica do primeiro personagem (etapa já executada)
1. Tripo Studio (site, privado): modelo HD → **Auto Rig esqueleto Mixamo** → exportar **GLB** (textura dentro) ou FBX + `_basecolor` copiado para `<fbx>.fbm/`.
2. Mixamo: clipes **"Without Skin", 30 fps, In Place**; 1 FBX por clipe na pasta `-Anims`.
3. Rodar e **olhar o `contact.png`**. Se der `ERRO armature nao bate`, é prefixo/nomenclatura (`mixamorig:` × rig do Tripo) — a verificar com o 1º modelo; o script lista os ossos faltantes. Se o personagem sair de costas, `-Yaw 180`.
4. Só então importar para `Assets/Resources/Sprites/` e escrever o consumidor acima (owner: frontend/view do Forge).

## Limites
- Retarget vigente por delta de rotação sobre o repouso no mundo (`render_sprites.py:retarget`); aplicar a action bruta por nome levantava os braços. Conferir poses e pés na folha de cada personagem.
- Workbench ignora nós de material: só cor base/textura da imagem ligada ao Base Color; sem normal map, emissão ou transparência de material.
- Clipes do placeholder têm braços em T (lição COE #1): as vistas S/N parecem uma cruz — defeito do clipe, não do render.
- Sem sombra de chão, sem atlas único (1 PNG por clipe; Idle de 30 frames = 3840 px de largura — abaixar `-Fps` ou aceitar). Memória: 8 dir × 30 frames × malha de 8 k tris = 2 M tris por render, ok.
