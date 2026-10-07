"""render_sprites.py - Blender headless: personagem 3D rigado + clipes -> folhas de sprite 2D top-down.

Uso (Blender 5.2; render_sprites.ps1 ao lado monta esta linha):
  blender.exe -b --factory-startup --python-exit-code 1 --python render_sprites.py --
      --model Model.fbx --out saida/ (--anims pasta_com_fbx/ | --self) [--elev 60] [--dirs 4|8] [--fps 12]
      [--size 128] [--scale PX_POR_METRO] [--yaw 0] [--light flat|matcap|studio] [--shadow] [--aa 8] [--src-fps 30]
Saida: <Clipe>.png (linha = direcao, coluna = frame; linha 0 em cima), meta.json, contact.png.
Direcoes em sentido horario a partir de S (de frente para a camera = para baixo na tela):
  4 -> S, W, N, E      8 -> S, SW, W, NW, N, NE, E, SE
Pivo = origem do modelo (pe no chao), em px a partir do canto inferior esquerdo da celula.
Tecnica: UM render por clipe. Cada celula e uma copia da malha avaliada no frame (pose aplicada), girada
em Z e transladada no plano da camera (em ORTHO a translacao no plano nao muda a imagem). Sem PIL/numpy.
"""
import argparse, glob, json, math, os, re, sys, time
import bpy
from mathutils import Matrix, Vector

DIRS = {1: ["S"], 4: ["S", "W", "N", "E"], 8: ["S", "SW", "W", "NW", "N", "NE", "E", "SE"]}
LIGHT = {"flat": ("FLAT", None), "matcap": ("MATCAP", "toon_light.exr"), "studio": ("STUDIO", "Default")}
BONE_RE = re.compile(r'pose\.bones\["([^"]+)"\]')
LOOP_RE = re.compile(r"idle|walk|run", re.I)  # ponytail: laco por nome; o Unity pode sobrescrever


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--model", required=True, help="FBX/GLB rigado (ou o FBX de um clipe do placeholder)")
    p.add_argument("--out", required=True)
    p.add_argument("--anims", help="pasta com 1 FBX por clipe (Mixamo Without Skin ou placeholder)")
    p.add_argument("--self", dest="self_", action="store_true", help="usar as actions ja no modelo")
    p.add_argument("--elev", type=float, default=60.0, help="graus da camera sobre o chao (90 = de cima)")
    p.add_argument("--dirs", type=int, default=4, choices=(1, 4, 8))
    p.add_argument("--fps", type=float, default=12.0)
    p.add_argument("--size", type=int, default=128, help="px por celula")
    p.add_argument("--scale", type=float, default=0.0, help="px por metro (0 = ajustar ao personagem)")
    p.add_argument("--yaw", type=float, default=0.0, help="graus somados a toda direcao (modelo que nao olha -Y)")
    p.add_argument("--light", default="flat", choices=tuple(LIGHT))
    p.add_argument("--shadow", action="store_true", help="auto-sombra do Workbench (nao ha sombra no chao)")
    p.add_argument("--aa", default="8", help="OFF|FXAA|5|8|11|16|32")
    p.add_argument("--src-fps", type=int, default=30, help="fps dos clipes de origem")
    p.add_argument("--max-frames", type=int, default=32, help="teto de quadros por clipe (largura da folha)")
    return p.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def import_file(path):
    before = set(bpy.data.objects)
    (bpy.ops.import_scene.gltf if path.lower().endswith((".glb", ".gltf")) else bpy.ops.import_scene.fbx)(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def action_bones(act):  # API em camadas do 5.x (action.fcurves nao existe mais)
    paths = " ".join(fc.data_path for ly in act.layers for st in ly.strips for cb in st.channelbags for fc in cb.fcurves)
    return set(BONE_RE.findall(paths))


def set_action(arm, act):
    if arm is None or act is None:  # prop estatico: nada a animar
        bpy.context.view_layer.update()
        return
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = act
    if act.slots:
        ad.action_slot = act.slots[0]
    bpy.context.view_layer.update()


def bone_order(arm):
    out = []

    def walk(b):
        out.append(b.name)
        for c in b.children:
            walk(c)
    for b in arm.data.bones:
        if b.parent is None:
            walk(b)
    return out


def retarget(src, tgt, name):
    """Leva o clipe do armature `src` para `tgt` pelo DELTA de rotacao de cada osso em relacao ao proprio repouso
    (no mundo), e nao pela rotacao local crua. Copiar a action so funciona com repouso/roll identicos; o rig
    Mixamo do Tripo e o X Bot do Mixamo diferem (bracos subiam acima da cabeca, 2026-10-06). O quadril tambem
    leva o deslocamento, escalado pela razao das alturas do quadril."""
    sact = src.animation_data.action
    s, e = (int(round(x)) for x in sact.frame_range)
    tb, sb = tgt.data.bones, src.data.bones
    order = bone_order(tgt)
    found = [n for n in order if n in sb]
    if len(found) < len(order) // 2:
        die("armature nao bate no clipe %s: so %d de %d osso(s) do modelo existem no clipe (faltam p.ex. %s)" % (
            name, len(found), len(order), ", ".join([n for n in order if n not in sb][:8])))
    Ws, Wt = src.matrix_world, tgt.matrix_world
    qWt_inv, Wt3_inv = Wt.to_quaternion().inverted(), Wt.inverted().to_3x3()
    root = order[0]
    hs = (Ws @ sb[root].head_local).z if root in sb else 0.0
    k = (Wt @ tb[root].head_local).z / hs if hs > 1e-6 else 1.0
    ad = tgt.animation_data or tgt.animation_data_create()
    ad.action = bpy.data.actions.new(name)
    for pb in tgt.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for f in range(s, e + 1):
        bpy.context.scene.frame_set(f)
        pose = {}
        for n in order:
            b, pb = tb[n], tgt.pose.bones[n]
            attach = pose[b.parent.name] @ b.parent.matrix_local.inverted() @ b.matrix_local if b.parent else b.matrix_local.copy()
            m = attach
            if n in sb:
                srest, spose = Ws @ sb[n].matrix_local, Ws @ src.pose.bones[n].matrix
                d = spose.to_quaternion() @ srest.to_quaternion().inverted()
                rot = qWt_inv @ d @ (Wt @ b.matrix_local).to_quaternion()
                head = attach.translation
                if not b.parent:
                    head = head + Wt3_inv @ ((spose.translation - srest.translation) * k)
                m = Matrix.Translation(head) @ rot.to_matrix().to_4x4()
            pose[n] = m
            pb.matrix_basis = attach.inverted() @ m
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if not b.parent:
                pb.keyframe_insert("location", frame=f)
    return ad.action


def load_clips(a, arm):
    """[(nome, action)] vindos de --anims (1 FBX = 1 clipe, retargetado) ou das actions do proprio modelo (--self)."""
    clips = [(act.name.split("|")[-1], act) for act in bpy.data.actions if action_bones(act)] if a.self_ else []
    for f in [] if a.self_ else sorted(glob.glob(os.path.join(a.anims or "", "*.fbx"))):
        if os.path.abspath(f) == os.path.abspath(a.model):
            continue
        objs = import_file(f)
        srcs = [o for o in objs if o.type == "ARMATURE" and o.animation_data and o.animation_data.action]
        name = os.path.splitext(os.path.basename(f))[0]
        if srcs:
            clips.append((name, retarget(srcs[0], arm, name)))
        else:
            print("AVISO sem animacao: " + f)
        bpy.data.batch_remove(objs)
    if not clips:
        die("nenhum clipe: use --anims <pasta com FBX> ou --self")
    return clips


def sample_frames(act, a):
    s, e = act.frame_range
    span = max(e - s, 1.0)  # em clipe em laco a pose do fim == inicio: amostra [s, e)
    # ponytail: teto de quadros por clipe (folha <= max_frames*size px; 4096 e o teto seguro de textura no celular);
    # clipe longo (Breathing Idle ~4 s) fica com fps menor, gravado por clipe no meta.json
    n = max(1, min(a.max_frames, int(round(span / a.src_fps * a.fps))))
    return [s + i * span / n for i in range(n)]


def eval_meshes(meshes, fr):
    bpy.context.scene.frame_set(int(fr), subframe=fr - int(fr))
    dg = bpy.context.evaluated_depsgraph_get()
    return [ob.evaluated_get(dg) for ob in meshes]


def bounds(arm, meshes, clips, e):
    """Raio horizontal maximo e extremos verticais na tela (valem para qualquer giro em Z) em todos os frames,
    pelos vertices reais: a bound_box combina z do topo com o raio da mao e inflava a celula em ~40%."""
    rho, vmin, vmax = 0.0, 0.0, 0.0
    for _, act, frames in clips:
        set_action(arm, act)
        for fr in frames:
            for ev in eval_meshes(meshes, fr):
                for v in ev.data.vertices:
                    p = ev.matrix_world @ v.co
                    r = math.hypot(p.x, p.y)
                    rho, vmin, vmax = max(rho, r), min(vmin, p.z * math.cos(e) - r * math.sin(e)), max(vmax, p.z * math.cos(e) + r * math.sin(e))
    return rho, vmin, vmax


def place(meshes, fr, theta, shift, keep):
    for ev in eval_meshes(meshes, fr):
        cp = bpy.data.objects.new("cell", bpy.data.meshes.new_from_object(ev))
        cp.matrix_world = Matrix.Translation(shift) @ Matrix.Rotation(theta, 4, "Z") @ ev.matrix_world
        bpy.context.scene.collection.objects.link(cp)
        keep.append(cp)


def render(cols, rows, cell, size, path, keep):
    sc = bpy.context.scene
    sc.camera.data.ortho_scale = cols * cell
    sc.render.resolution_x, sc.render.resolution_y, sc.render.filepath = cols * size, rows * size, path
    bpy.ops.render.render(write_still=True)
    bpy.data.batch_remove([cp.data for cp in keep] + keep)
    keep.clear()


def main():
    a = parse_args()
    out = os.path.abspath(a.out)
    os.makedirs(out, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.fps = a.src_fps  # o importador FBX converte tempo -> frame com o fps da cena
    model = import_file(os.path.abspath(a.model))
    arms, meshes = [o for o in model if o.type == "ARMATURE"], [o for o in model if o.type == "MESH"]
    if len(arms) > 1 or not meshes:
        die("modelo precisa de malha(s) e no maximo 1 armature; achei %d armature(s) e %d malha(s)" % (len(arms), len(meshes)))
    arm = arms[0] if arms else None
    # sem armature = prop estatico (estacao): 1 quadro "Static", mesma camera e escala dos personagens
    clips = [(n, act, sample_frames(act, a)) for n, act in load_clips(a, arm)] if arm else [("Static", None, [0.0])]

    e = math.radians(a.elev)
    fwd, up, right = Vector((0, math.cos(e), -math.sin(e))), Vector((0, math.sin(e), math.cos(e))), Vector((1, 0, 0))
    rho, vmin, vmax = bounds(arm, meshes, clips, e)
    extent = max(2 * rho, vmax - vmin) * 1.1
    ppu = a.scale or a.size / extent
    cell = a.size / ppu  # metros por celula
    if extent > cell:
        print("AVISO recorte: personagem ocupa %.2f m e a celula so %.2f m (--scale menor ou --size maior)" % (extent, cell))
    vc = (vmin + vmax) / 2.0  # centro vertical do personagem na tela; o pivo (origem) fica abaixo dele
    pivot_y = a.size / 2.0 - vc * ppu

    sc.render.engine, sc.render.film_transparent = "BLENDER_WORKBENCH", True
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform, sc.display.render_aa = "Standard", a.aa
    sh = sc.display.shading
    sh.light, studio = LIGHT[a.light]
    if studio:
        sh.studio_light = studio
    sh.color_type, sh.show_object_outline, sh.show_cavity, sh.show_shadows = "TEXTURE", True, True, a.shadow
    cam = bpy.data.objects.new("SpriteCam", bpy.data.cameras.new("SpriteCam"))
    cam.data.type, cam.data.sensor_fit, cam.data.clip_end = "ORTHO", "HORIZONTAL", 1000.0
    cam.location, cam.rotation_euler = -fwd * 50.0, (math.pi / 2 - e, 0.0, 0.0)
    sc.collection.objects.link(cam)
    sc.camera = cam
    for o in model:
        o.hide_render = True

    meta = {"size": a.size, "fps": a.fps, "dirs": a.dirs, "dir_order": DIRS[a.dirs], "row0": "top", "elev": a.elev,
            "yaw": a.yaw, "ppu": round(ppu, 2), "pivot_px": [a.size / 2.0, round(pivot_y, 1)],
            "pivot": [0.5, round(pivot_y / a.size, 4)], "light": a.light, "src_fps": a.src_fps, "clips": []}  # lista: JsonUtility
    keep = []
    for name, act, frames in clips:
        t0 = time.time()
        set_action(arm, act)
        for d in range(a.dirs):
            for i, fr in enumerate(frames):
                shift = right * ((i + 0.5 - len(frames) / 2.0) * cell) + up * ((a.dirs / 2.0 - d - 0.5) * cell - vc)
                place(meshes, fr, math.radians(a.yaw - d * 360.0 / a.dirs), shift, keep)
        render(len(frames), a.dirs, cell, a.size, os.path.join(out, name + ".png"), keep)
        loop = bool(LOOP_RE.search(name))
        span = act.frame_range[1] - act.frame_range[0] if act else 0.0
        dur = max(span, 1.0) / a.src_fps
        meta["clips"].append({"name": name, "file": name + ".png", "frames": len(frames), "loop": loop,
                              "fps": round(len(frames) / dur, 3), "duration_s": round(dur, 3),
                              "src_frames": int(round(span))})
        print("CLIP %-20s frames=%2d dirs=%d folha=%dx%d px loop=%s t=%.1fs" % (
            name, len(frames), a.dirs, len(frames) * a.size, a.dirs * a.size, loop, time.time() - t0))
    for j, (name, act, frames) in enumerate(clips):  # contact.png: 1o frame de cada clipe, direcao S
        set_action(arm, act)
        place(meshes, frames[0], math.radians(a.yaw), right * ((j + 0.5 - len(clips) / 2.0) * cell) - up * vc, keep)
    render(len(clips), 1, cell, a.size, os.path.join(out, "contact.png"), keep)
    with open(os.path.join(out, "meta.json"), "w") as f:
        json.dump(meta, f, indent=1)
    print("SPRITES_OK %s clipes=%d ppu=%.1f pivo=(%d,%d)px" % (out, len(clips), ppu, a.size / 2, round(pivot_y)))


main()
