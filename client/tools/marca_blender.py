"""marca_blender.py - Forge Street C-ART-14 (0 credito): camadas RGBA da marca renderizadas dos modelos do jogo.

Uso (raiz do projeto Forge Street, Unity fechado; docs/ASSETS.md secao 13):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1
      --python client/tools/marca_blender.py -- [--out client/Builds/marca/camadas] [camada ...]
Depois: python client/tools/marca_compor.py (fundo, brilho, faiscas, contorno, texto, folhas).
Saida: <out>/<camada>.png, RGBA de fundo transparente, 2048 px no lado maior (EEVEE, toon de 3 faixas e contorno por
casca invertida = preset `forja` do render_sprites.py; luz-chave quente de baixo-esquerda, preenchimento frio de cima).
  - ferreiro_martelo / ferreiro_moeda: o ferreiro do Lote 1 (Tripo, arte/tripo/ferreiro) posado por script (martelo
    ou moeda erguidos na mao direita, mao esquerda na cintura), quase de frente;
  - bigorna: a bigorna da heraldica do jogo (perfil ANVIL da moeda) com a espada do jogo em brasa sobre o tampo;
  - espada: o item_espada do jogo (arte_v05_blender.espada), grande, para a obra-prima;
  - moedas: pilha de moedas do jogo (arte_v05_blender.moeda);
  - simbolo: a mesma bigorna com o martelo atravessado, para o logo.
O import do FBX do ferreiro leva 5-11 min: a 1a rodada guarda <out>/../cache_ferreiro.blend e as seguintes leem dele.
"""
import argparse, math, os, sys, time
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_sprites as rs
import arte_v05_blender as A
from arte_v05_blender import Peca, R, T, prisma, espada, moeda, ANVIL
from props_blender import box, cyl
from ui_v05b_blender import Sub

ARTE = "C:/Users/VINICIUS/Videos/MEUS PROJETOS/JOGOS NOVOS/03_FORGE_STREET/arte/tripo/"   # fora do git (.gitignore)
FERREIRO = ARTE + "ferreiro/tripo_convert_69868933-1e2e-41d3-9802-1309391a9369.fbx"
LADO = 2048          # px do lado maior de cada camada
CONTORNO_M = 0.006   # casca invertida em m (~10 px a 2048 numa figura de 1,2 m)
A.TONS.update({"brasa": (0xFF7A1F, 0xFF9A3D, 0xFFD166), "brasa_fio": (0xFFD166, 0xFFE9A8, 0xFFF6D8),
               "ferro_i": (0x3C434F, 0x5F6878, 0x8C96A6)})
# pose do ferreiro: giros no MUNDO, em ordem (pai antes do filho). T-pose: braco direito em -X, esquerdo em +X, frente -Y.
# +Y no braco direito levanta; +Y no esquerdo abaixa (regra da mao direita).
POSE = [("mixamorig:RightArm", (0, 1, 0), 55), ("mixamorig:RightArm", (1, 0, 0), -15),
        ("mixamorig:RightForeArm", (0, 1, 0), 45),
        ("mixamorig:LeftArm", (0, 1, 0), 62), ("mixamorig:LeftArm", (1, 0, 0), -10),
        ("mixamorig:LeftForeArm", (0, 1, 0), 65), ("mixamorig:LeftForeArm", (0, 0, 1), 25),
        ("mixamorig:Head", (0, 1, 0), 6)]
YAW = 14.0           # o corpo vira um pouco para a camera-esquerda (3/4 leve)


def marreta(p):
    """Martelo de forja exagerado: cabo de madeira +Z de 0,46 m, cabeca de aco de 0,24 m em X com a boca maior."""
    p.add(cyl(0.026, 0.46, 0.022, segs=10), "madeira_i", at=(0, 0, 0.23), bevel=0.005)
    p.add(cyl(0.032, 0.03, segs=10), "couro2", at=(0, 0, 0.03), bevel=0.006)
    p.add(box(0.2, 0.1, 0.1), "aco", at=(0, 0, 0.47), bevel=0.014)
    p.add(box(0.06, 0.13, 0.13), "aco", at=(0.11, 0, 0.47), bevel=0.016)
    p.add(cyl(0.05, 0.05, 0.03, segs=8), "aco", m=T(-0.12, 0, 0.47) @ R("Y", -90), bevel=0.006)
    p.add(box(0.05, 0.11, 0.11), "couro2", at=(0, 0, 0.47), bevel=0.008)


def espada_brasa(p):
    """O item_espada com a lamina em brasa (acabou de sair da fornalha)."""
    espada(p)
    p.fams = ["brasa" if f == "lamina" else "brasa_fio" if f in ("fio", "sulco") else f for f in p.fams]


def bigorna(p):
    """Bigorna da heraldica do jogo: o perfil ANVIL da moeda e do lingote extrudado (0,8 x 0,51 x 0,2 m), ferro escuro
    com o tampo aco-claro. (O modelo Tripo da estacao foi testado e saiu do icone: bigorna preta sob o pilao de madeira e a
    lanterna vira mancha a 48 px.)"""
    k = 8.0
    p.add(prisma([(x * k, z * k) for x, z in ANVIL], -0.1, 0.1), "ferro_i", bevel=0.012,
          pick=lambda f: "lingote" if f.normal.z > 0.9 else None)


def simbolo(p):
    """Bigorna + martelo de 1,5x atravessado atras (cabeca acima do tampo, cabo saindo atras do calcanhar)."""
    bigorna(p)
    marreta(Sub(p, T(0.5, 0.16, -0.15) @ R("Y", -45) @ Matrix.Scale(1.5, 4)))


# ------------------------------------------------------------------ cena

_toon = set()


def toon(obs):
    """Toon de 3 faixas do preset forja em cada material (1 vez por material: os tons sao compartilhados entre pecas).
    Familia brasa* = emissiva: a cor sai sem luz, um pouco acima de 1 (laranja quente sem estourar no branco)."""
    for o in obs:
        for m in o.data.materials:
            if m and m.name not in _toon:
                _toon.add(m.name)
                if m.name.startswith("brasa"):
                    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
                    rs.emit(m, tuple(bsdf.inputs["Base Color"].default_value), 1.25)
                else:
                    rs.toon(m, False)


def contorno(obs):
    line = bpy.data.materials.get("Contorno")
    if line is None:
        line = bpy.data.materials.new("Contorno")
        line.use_backface_culling = True
        rs.emit(line, (*rs.OUTLINE_RGB, 1.0))
    for ob in obs:
        n = len(ob.data.materials)
        ob.data.materials.append(line)
        sol = ob.modifiers.new("Contorno", "SOLIDIFY")
        k = sum(ob.matrix_world.to_scale()) / 3
        sol.thickness, sol.offset, sol.use_flip_normals, sol.thickness_clamp = CONTORNO_M / k, 1.0, True, 0.0
        sol.material_offset, sol.use_rim = n, False


def peca_obj(build, M=Matrix()):
    p = Peca()
    build(p)
    ob = A.objeto(p, M, lambda n: 1)   # tom base: as faixas saem do toon
    toon([ob])
    return ob


def cena():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.fps = 30
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples, sc.eevee.use_shadows = 16, False
    sc.render.film_transparent = True
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform = "Standard"


def luzes():
    sc = bpy.context.scene
    for name, (pos, k) in (("Chave", rs.KEY), ("Preenchimento", rs.FILL)):
        sun = bpy.data.objects.new(name, bpy.data.lights.new(name, "SUN"))
        sun.data.energy, sun.data.use_shadow = k * math.pi, False
        sun.rotation_euler = (-Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type, cam.data.clip_end = "ORTHO", 1000.0
    sc.collection.objects.link(cam)
    sc.camera = cam


def ferreiro(cache):
    """Importa o FBX (ou le o cache .blend) e devolve (armature, malhas)."""
    if os.path.exists(cache):
        with bpy.data.libraries.load(cache) as (src, dst):
            dst.objects = list(src.objects)
        objs = [o for o in dst.objects if o]
        for o in objs:
            bpy.context.scene.collection.objects.link(o)
    else:
        t0 = time.time()
        objs = rs.import_file(FERREIRO)
        os.makedirs(os.path.dirname(cache), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=cache, copy=True)
        print("CACHE ferreiro %.0f s -> %s" % (time.time() - t0, cache))
    arm = next(o for o in objs if o.type == "ARMATURE")
    return arm, [o for o in objs if o.type == "MESH"]


def posar(arm):
    if arm.animation_data:   # o take do FBX sobrescreveria a pose no render
        arm.animation_data_clear()
    arm.rotation_euler.z += math.radians(YAW)
    bpy.context.view_layer.update()
    inv = arm.matrix_world.inverted().to_3x3()
    for nome, eixo, graus in POSE:
        pb = arm.pose.bones[nome]
        M = pb.matrix.copy()
        ax = (inv @ (Matrix.Rotation(math.radians(YAW), 3, "Z") @ Vector(eixo))).normalized()
        h = M.translation.copy()
        pb.matrix = Matrix.Translation(h) @ Matrix.Rotation(math.radians(graus), 4, ax) @ Matrix.Translation(-h) @ M
        bpy.context.view_layer.update()


def mao(arm):
    """(posicao do punho, direcao da mao) da mao direita no mundo."""
    pb = arm.pose.bones["mixamorig:RightHand"]
    W = arm.matrix_world
    return W @ pb.head, ((W @ pb.tail) - (W @ pb.head)).normalized()


def enquadrar(obs, path, elev, yaw=0.0, lado=LADO, margem=1.06):
    """Camera ortografica `elev` graus sobre o horizonte, girada `yaw` em Z, enquadrando os vertices de `obs`."""
    sc = bpy.context.scene
    e, y = math.radians(elev), math.radians(yaw)
    rot = Matrix.Rotation(y, 3, "Z") @ Matrix.Rotation(math.pi / 2 - e, 3, "X")
    right, up, back = rot @ Vector((1, 0, 0)), rot @ Vector((0, 1, 0)), rot @ Vector((0, 0, 1))
    dg = bpy.context.evaluated_depsgraph_get()
    pts = [o.evaluated_get(dg).matrix_world @ v.co for o in obs for v in o.evaluated_get(dg).data.vertices]
    us, vs = [p.dot(right) for p in pts], [p.dot(up) for p in pts]
    w, h = max(us) - min(us), max(vs) - min(vs)
    c = right * (max(us) + min(us)) / 2 + up * (max(vs) + min(vs)) / 2
    sc.camera.matrix_world = Matrix.Translation(c + back * 50) @ rot.to_4x4()
    sc.camera.data.ortho_scale = max(w, h) * margem
    sc.camera.data.sensor_fit = "HORIZONTAL" if w >= h else "VERTICAL"
    sc.render.resolution_x = int(round(lado * min(1.0, w / h)))
    sc.render.resolution_y = int(round(lado * min(1.0, h / w)))
    for o in bpy.data.objects:
        o.hide_render = o.type == "MESH" and o not in obs
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("CAMADA %-18s %dx%d" % (os.path.basename(path), sc.render.resolution_x, sc.render.resolution_y))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Builds", "marca", "camadas"))
    todas = ["ferreiro_martelo", "ferreiro_moeda", "bigorna", "espada", "moedas", "simbolo"]
    ap.add_argument("camadas", nargs="*", default=todas)
    a = ap.parse_args(argv)
    out = os.path.abspath(a.out)
    os.makedirs(out, exist_ok=True)
    t0 = time.time()
    cena()
    if {"ferreiro_martelo", "ferreiro_moeda"} & set(a.camadas):
        arm, corpo = ferreiro(os.path.join(os.path.dirname(out), "cache_ferreiro.blend"))
    luzes()
    if {"ferreiro_martelo", "ferreiro_moeda"} & set(a.camadas):
        toon(corpo)
        contorno(corpo)
        posar(arm)
        punho, dmao = mao(arm)
        cabo = Vector((-0.22, 0.12, 1.0)).normalized()
        grip = punho + dmao * 0.06
        # o cabo passa pelo punho; 0,1 m do cabo fica abaixo da mao
        hx = Matrix.Rotation(math.radians(YAW), 3, "Z") @ Vector((1, 0, 0))
        hx = (hx - cabo * hx.dot(cabo)).normalized()   # cabeca do martelo na horizontal da tela: silhueta em T
        M = Matrix.Translation(grip - cabo * 0.1) @ A.basis(hx, cabo.cross(hx), cabo)
        martelo = peca_obj(marreta, M)
        contorno([martelo])
        M2 = Matrix.Translation(grip + cabo * 0.15) @ R("Z", -32) @ R("X", -10) @ Matrix.Scale(1.6, 4)   # face para a chave: ouro claro
        mo = peca_obj(moeda, M2)
        contorno([mo])
        if "ferreiro_martelo" in a.camadas:
            enquadrar(corpo + [martelo], os.path.join(out, "ferreiro_martelo.png"), 8)
        if "ferreiro_moeda" in a.camadas:
            enquadrar(corpo + [mo], os.path.join(out, "ferreiro_moeda.png"), 8)
    if "bigorna" in a.camadas:
        # a bigorna da heraldica (moeda/lingote) com a espada em brasa deitada no tampo (z 0,256), meio de lado, cabo sobre o bico
        big = peca_obj(bigorna)
        esp = peca_obj(espada_brasa, Matrix.Translation((-0.47, -0.02, 0.285)) @ R("Z", -8) @ R("X", 65) @ R("Y", 90))
        contorno([big, esp])
        enquadrar([big, esp], os.path.join(out, "bigorna.png"), 34, yaw=-25)
    if "espada" in a.camadas:
        ob = peca_obj(espada, R("Y", 40) @ R("Z", 20))
        contorno([ob])
        enquadrar([ob], os.path.join(out, "espada.png"), 0)
    if "moedas" in a.camadas:
        obs = []
        pilha = [(0, 0, 0, 0), (0.02, 0.01, 0.026, 25), (-0.01, -0.01, 0.052, 50), (0.24, 0.05, 0, 10), (0.25, 0.04, 0.026, 70),
                 (-0.23, 0.04, 0, 40), (0.12, -0.12, 0, 15), (-0.12, -0.13, 0, 80), (0.0, -0.06, 0.08, 5)]
        for x, y, z, rz in pilha:
            obs.append(peca_obj(moeda, Matrix.Translation((x, y, z + 0.012)) @ R("Z", rz) @ R("X", -90)))
        for x, z, rx in ((-0.1, 0.17, -62), (0.14, 0.2, -70)):   # 2 em pe, encostadas
            obs.append(peca_obj(moeda, Matrix.Translation((x, 0.06, z * 0.6)) @ R("X", rx)))
        contorno(obs)
        enquadrar(obs, os.path.join(out, "moedas.png"), 30)
    if "simbolo" in a.camadas:
        ob = peca_obj(simbolo)
        contorno([ob])
        enquadrar([ob], os.path.join(out, "simbolo.png"), 14, yaw=-28)
    print("MARCA_BLENDER_OK %d camada(s) em %.1f s -> %s" % (len(a.camadas), time.time() - t0, out))


if __name__ == "__main__":
    main()
