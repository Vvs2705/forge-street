"""props_blender.py - Blender 5.2 headless: props de cenario do Forge Street modelados por primitivas -> FBX (0 credito).

Uso (render depois com render_sprites.py --dirs 1 --size 256, docs/RENDER_SPRITES.md):
  blender.exe -b --factory-startup --python-exit-code 1 --python props_blender.py -- [--out arte/props] [nome ...]
Saida: <out>/<nome>/<nome>.fbx - 1 malha, origem no chao (z = 0), frente para -Y (= de frente para a camera do render).
Estilo (ART_BIBLE s1, s2, s8): volumes gordos com chanfro leve, cor chapada em 2 tons por material - face de topo
(normal z > 0,55) no tom de luz, o resto no tom base; o Workbench do render_sprites (luz flat) le a cor do material.
Nenhum matiz de item (s3: azul espada, vermelho escudo, verde ferramenta, roxo joia) nos props: nao confundem a pilha.
"""
import argparse, math, os, sys
import bpy, bmesh
from mathutils import Euler, Matrix, Vector

# material -> (base = laterais, luz = topo). Madeira/ferro/ouro/couro/brasa = ART_BIBLE s2; o resto e' derivado
PAL = {
    "madeira": (0x8A5A32, 0xB8824F), "madeira_esc": (0x5C3A1E, 0x8A5A32), "casca": (0x6B4428, 0x8A5A32),
    "cerne": (0xC99A62, 0xE0B98A), "ferro": (0x4F5866, 0x8C96A6), "aco": (0x8C96A6, 0xD5DCE6),
    "ouro": (0xE0B23A, 0xFFE08A), "couro": (0x7A4A2A, 0xA86D45), "juta": (0xB59A6C, 0xD9C399),
    "juta2": (0x9E8458, 0xC2A87A), "terracota": (0xA65A35, 0xCF7E50), "creme": (0xC9B994, 0xE6DCC0),
    "vidro": (0x3F5E5A, 0x6A8F88), "carvao": (0x2E2B2A, 0x4A4644), "agua": (0x2F5F85, 0x4F86B8),
    "chama": (0xFF7A1F, 0xFF7A1F), "nucleo": (0xFFD166, 0xFFD166),
}
EMISSIVE = {"chama", "nucleo"}


def lin(h):
    """#RRGGBB sRGB -> RGBA linear (Base Color e cor de viewport sao lineares; o render volta para sRGB)."""
    c = [((h >> s) & 255) / 255 for s in (16, 8, 0)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    base, topo = (name[:-5], True) if name.endswith("_topo") else (name, False)
    col = lin(PAL[base][1 if topo else 0])
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value = col
    p.inputs["Roughness"].default_value = 0.8
    m.diffuse_color = col  # Workbench (render_sprites) le esta
    if base in EMISSIVE:
        p.inputs["Emission Color"].default_value = col
        p.inputs["Emission Strength"].default_value = 2.0
    return m


# ------------------------------------------------------------------ primitivas (bmesh na origem, ja no tamanho final)

def box(sx, sy, sz):
    t = bmesh.new()
    bmesh.ops.create_cube(t, size=1.0)
    bmesh.ops.scale(t, vec=(sx, sy, sz), verts=t.verts)
    return t


def cyl(r1, h, r2=None, segs=12):
    """Cilindro/cone em Z, centrado na origem (z de -h/2 a h/2)."""
    t = bmesh.new()
    bmesh.ops.create_cone(t, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r1 if r2 is None else r2, depth=h)
    return t


def ball(rx, ry, rz, u=10, v=6):
    t = bmesh.new()
    bmesh.ops.create_uvsphere(t, u_segments=u, v_segments=v, radius=1.0)
    bmesh.ops.scale(t, vec=(rx, ry, rz), verts=t.verts)
    return t


def rock(r):
    t = bmesh.new()
    bmesh.ops.create_icosphere(t, subdivisions=1, radius=r)
    return t


def lathe(prof, segs=12, closed=False):
    """Perfil [(r, z)] girado em Z (z absoluto). closed=False tampa as pontas; closed=True = anel de perfil fechado."""
    t = bmesh.new()
    rings = [[t.verts.new((r * math.cos(2 * math.pi * k / segs), r * math.sin(2 * math.pi * k / segs), z)) for k in range(segs)]
             for r, z in prof]
    for i in range(len(rings) if closed else len(rings) - 1):
        a, b = rings[i], rings[(i + 1) % len(rings)]
        for k in range(segs):
            t.faces.new((a[k], a[(k + 1) % segs], b[(k + 1) % segs], b[k]))
    if not closed:
        t.faces.new(list(reversed(rings[0])))
        t.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    return t


class Prop:
    """Uma malha por prop; cada add() funde uma primitiva com chanfro e material de 2 tons."""

    def __init__(self):
        self.bm, self.mats = bmesh.new(), []

    def add(self, t, mat, at=(0, 0, 0), rot=(0, 0, 0), bevel=0.012, cap=None):
        """`cap`: material das faces do eixo Z LOCAL (topo de tora, agua do balde) em vez dos 2 tons."""
        if bevel > 0:
            bmesh.ops.bevel(t, geom=list(t.edges), offset=bevel, offset_type="OFFSET", segments=1, profile=0.5,
                            affect="EDGES", clamp_overlap=True)
        t.normal_update()
        caps = {f for f in t.faces if cap and abs(f.normal.z) > 0.99}
        bmesh.ops.transform(t, matrix=Matrix.Translation(at) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4(), verts=t.verts)
        t.normal_update()
        for f in t.faces:
            name = cap if f in caps else mat + ("_topo" if f.normal.z > 0.55 else "")
            if name not in self.mats:
                self.mats.append(name)
            f.material_index = self.mats.index(name)
        me = bpy.data.meshes.new("tmp")
        t.to_mesh(me)
        t.free()
        self.bm.from_mesh(me)
        bpy.data.meshes.remove(me)


# ------------------------------------------------------------------ props (metros; frente = -Y)

def barril(p):
    p.add(lathe([(0.22, 0.0), (0.26, 0.12), (0.28, 0.37), (0.26, 0.62), (0.22, 0.74)]), "madeira")
    for z, r in ((0.1, 0.268), (0.64, 0.268)):
        p.add(cyl(r, 0.06), "ferro", at=(0, 0, z), bevel=0.008)
    p.add(cyl(0.18, 0.03), "madeira_esc", at=(0, 0, 0.745), bevel=0.008)


def crate(p, s, h, at, rz):
    """Caixote de lado `s` e altura `h`: miolo, 4 pilares, aros em cima/embaixo e travessa diagonal na frente e na direita."""
    x0, y0, z0 = at
    m = Matrix.Translation(at) @ Matrix.Rotation(math.radians(rz), 4, "Z")
    def put(t, mat, local, rot=(0, 0, 0), bevel=0.01):
        p.add(t, mat, at=tuple(m @ Vector(local)), rot=(rot[0], rot[1], rot[2] + rz), bevel=bevel)
    put(box(s - 0.04, s - 0.04, h - 0.04), "madeira", (0, 0, h / 2), bevel=0.015)
    b, e = 0.075, s / 2 - 0.0375
    for sx in (-1, 1):
        for sy in (-1, 1):
            put(box(b, b, h), "madeira_esc", (sx * e, sy * e, h / 2))
    for z in (b / 2, h - b / 2):
        for sy in (-1, 1):
            put(box(s, b, b), "madeira_esc", (0, sy * e, z))
            put(box(b, s, b), "madeira_esc", (sy * e, 0, z))
    d = math.degrees(math.atan2(h - 2 * b, s - 2 * b))
    put(box(math.hypot(s, h) - 2.2 * b, 0.03, 0.06), "madeira_esc", (0, -s / 2 - 0.005, h / 2), rot=(0, -d, 0))
    put(box(0.03, math.hypot(s, h) - 2.2 * b, 0.06), "madeira_esc", (s / 2 + 0.005, 0, h / 2), rot=(d, 0, 0))


def caixote(p):
    crate(p, 0.6, 0.52, (0, 0, 0), 0)
    crate(p, 0.42, 0.36, (0.04, 0.05, 0.52), 18)


def saco(p, at, mat, rz=0):
    x, y, z = at
    p.add(ball(0.19, 0.17, 0.23), mat, at=(x, y, z + 0.21), rot=(0, 0, rz), bevel=0)
    p.add(cyl(0.075, 0.08, 0.045, segs=8), mat, at=(x, y, z + 0.46), bevel=0.006)
    p.add(cyl(0.06, 0.03, segs=8), "couro", at=(x, y, z + 0.45), bevel=0.005)
    p.add(cyl(0.03, 0.06, 0.07, segs=8), mat, at=(x, y, z + 0.52), bevel=0.006)


def sacos(p):
    saco(p, (-0.19, 0.06, 0), "juta")
    saco(p, (0.2, 0.08, 0), "juta2", 20)
    p.add(ball(0.27, 0.17, 0.14), "juta", at=(0.0, -0.2, 0.13), rot=(0, 0, 8), bevel=0)   # deitado na frente
    p.add(cyl(0.05, 0.08, segs=8), "couro", at=(0.27, -0.21, 0.14), rot=(0, 90, 8), bevel=0.005)


def tocha(p):
    """Tocha de parede: a parede fica em y = +0,07 (atras); o braco sai para a camera. Chama ~z 1,2 (z0 + 0,65)."""
    z0, y = 0.55, -0.16
    p.add(box(0.18, 0.04, 0.3), "ferro", at=(0, 0.05, z0 + 0.15))
    p.add(box(0.06, 0.22, 0.06), "ferro", at=(0, -0.05, z0 + 0.08), bevel=0.008)
    p.add(cyl(0.075, 0.05, segs=10), "ferro", at=(0, y, z0 + 0.1), bevel=0.008)
    p.add(cyl(0.042, 0.48, segs=8), "madeira_esc", at=(0, y, z0 + 0.22), bevel=0.008)
    p.add(cyl(0.052, 0.1, 0.1, segs=10), "ferro", at=(0, y, z0 + 0.5), bevel=0.008)
    p.add(ball(0.115, 0.115, 0.12, u=8, v=5), "chama", at=(0, y, z0 + 0.6), bevel=0)
    p.add(cyl(0.115, 0.3, 0.0, segs=8), "chama", at=(0, y, z0 + 0.76), bevel=0)
    p.add(cyl(0.065, 0.2, 0.0, segs=8), "nucleo", at=(0, y - 0.06, z0 + 0.68), bevel=0)


def jarro(p, at, mat, h, rb, rm, rn):
    x, y, z = at
    p.add(lathe([(rb, 0), (rm, h * 0.45), (rn, h * 0.85), (rn * 1.25, h)], segs=10), mat, at=(x, y, z), bevel=0.006)


def garrafa(p, at, h):
    x, y, z = at
    p.add(lathe([(0.045, 0), (0.045, h * 0.6), (0.02, h * 0.8), (0.02, h)], segs=8), "vidro", at=(x, y, z), bevel=0.004)
    p.add(cyl(0.024, 0.03, segs=8), "madeira_esc", at=(x, y, z + h + 0.01), bevel=0.004)


def prateleira(p):
    for sx in (-1, 1):
        p.add(box(0.06, 0.3, 1.27), "madeira_esc", at=(sx * 0.43, 0, 0.635))
    for z in (0.06, 0.46, 0.86, 1.25):
        p.add(box(0.92, 0.3, 0.04), "madeira", at=(0, 0, z))
    for z in (0.28, 0.68, 1.06):
        p.add(box(0.82, 0.03, 0.05), "madeira_esc", at=(0, 0.13, z), bevel=0.006)
    jarro(p, (-0.22, 0, 0.08), "terracota", 0.3, 0.08, 0.13, 0.06)
    jarro(p, (0.06, 0, 0.08), "creme", 0.26, 0.07, 0.11, 0.05)
    p.add(ball(0.11, 0.1, 0.1), "juta", at=(0.28, -0.01, 0.17), bevel=0)
    jarro(p, (-0.26, 0, 0.48), "creme", 0.2, 0.06, 0.09, 0.04)
    jarro(p, (0.0, 0, 0.48), "terracota", 0.12, 0.08, 0.12, 0.1)
    garrafa(p, (0.25, 0, 0.48), 0.2)
    jarro(p, (-0.16, 0, 0.88), "terracota", 0.24, 0.06, 0.1, 0.045)
    garrafa(p, (0.12, 0, 0.88), 0.22)
    garrafa(p, (0.27, 0, 0.88), 0.17)
    jarro(p, (0.0, 0, 1.27), "creme", 0.16, 0.06, 0.08, 0.04)


def espada(p, x):
    p.add(ball(0.035, 0.035, 0.035, u=8, v=4), "ouro", at=(x, 0, 0.1), bevel=0)
    p.add(cyl(0.022, 0.16, segs=8), "couro", at=(x, 0, 0.19), bevel=0.004)
    p.add(box(0.17, 0.04, 0.035), "ouro", at=(x, 0, 0.285), bevel=0.006)
    p.add(box(0.06, 0.018, 0.56), "aco", at=(x, 0, 0.58), bevel=0.004)
    t = cyl(0.043, 0.09, 0.0, segs=4)
    bmesh.ops.rotate(t, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(45), 3, "Z"), verts=t.verts)
    bmesh.ops.scale(t, vec=(1.0, 0.3, 1.0), verts=t.verts)
    p.add(t, "aco", at=(x, 0, 0.905), bevel=0)


def suporte_armas(p):
    p.add(box(1.0, 0.34, 0.08), "madeira_esc", at=(0, 0.02, 0.04))
    for sx in (-1, 1):
        p.add(box(0.07, 0.07, 1.06), "madeira", at=(sx * 0.46, 0.06, 0.53))
    p.add(box(1.0, 0.07, 0.07), "madeira", at=(0, 0.06, 0.96))
    p.add(box(0.98, 0.1, 0.05), "madeira", at=(0, 0.0, 0.34), bevel=0.01)
    for x in (-0.3, -0.1, 0.1):
        espada(p, x)
    sh = (0.31, -0.13, 0.31)
    p.add(cyl(0.26, 0.04, segs=14), "ferro", at=sh, rot=(78, 0, 0), bevel=0.008)
    p.add(cyl(0.23, 0.05, segs=14), "madeira", at=(sh[0], sh[1] - 0.006, sh[2]), rot=(78, 0, 0), bevel=0.008)
    p.add(ball(0.065, 0.03, 0.065, u=10, v=5), "ouro", at=(sh[0], sh[1] - 0.04, sh[2] + 0.005), rot=(-12, 0, 0), bevel=0)


def lenha(p):
    rows = ((-0.24, -0.08, 0.08, 0.24), (-0.16, 0.0, 0.16), (-0.08, 0.08), (0.0,))
    k = 0
    for i, xs in enumerate(rows):
        for x in xs:
            jit = ((k * 37) % 7 - 3) / 3.0   # ponytail: variacao fixa por tora, sem random (render reproduzivel)
            p.add(cyl(0.072, 0.56 + 0.03 * jit, segs=9), "casca", at=(x, 0.02 * jit, 0.072 + i * 0.128),
                  rot=(90, 0, 5 * jit), bevel=0.01, cap="cerne")
            k += 1
    for (x, y, r) in ((0.5, -0.05, 0.1), (0.62, 0.08, 0.085), (0.43, 0.12, 0.08), (0.58, -0.14, 0.07), (0.53, 0.03, 0.08)):
        p.add(rock(r), "carvao", at=(x, y, r * 0.7 + (0.08 if (x, y) == (0.53, 0.03) else 0)), bevel=0)


def balde(p):
    p.add(lathe([(0.13, 0.0), (0.165, 0.28)], segs=12), "madeira", cap="agua")
    p.add(lathe([(0.14, 0.27), (0.172, 0.27), (0.172, 0.31), (0.14, 0.31)], segs=12, closed=True), "madeira_esc", bevel=0.005)
    for z, r1, r2 in ((0.06, 0.141, 0.145), (0.21, 0.161, 0.165)):
        p.add(cyl(r1 + 0.008, 0.035, r2 + 0.008), "ferro", at=(0, 0, z), bevel=0.005)
    n = 7
    for i in range(n):   # alca: arco de ferro sobre a boca
        a0, a1 = math.pi * i / n, math.pi * (i + 1) / n
        x0, z0, x1, z1 = 0.17 * math.cos(a0), 0.31 + 0.17 * math.sin(a0), 0.17 * math.cos(a1), 0.31 + 0.17 * math.sin(a1)
        p.add(box(math.hypot(x1 - x0, z1 - z0) + 0.012, 0.018, 0.018), "ferro", at=((x0 + x1) / 2, 0, (z0 + z1) / 2),
              rot=(0, -math.degrees(math.atan2(z1 - z0, x1 - x0)), 0), bevel=0)


PROPS = {"barril": barril, "caixote": caixote, "sacos": sacos, "tocha": tocha, "prateleira": prateleira,
         "suporte_armas": suporte_armas, "lenha": lenha, "balde": balde}


def export(name, p, out):
    me = bpy.data.meshes.new(name)
    p.bm.to_mesh(me)
    p.bm.free()
    for m in p.mats:
        me.materials.append(material(m))
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    path = os.path.join(out, name, name + ".fbx")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, object_types={"MESH"}, mesh_smooth_type="FACE",
                             add_leaf_bones=False, bake_anim=False)
    lo, hi = [min(v.co[i] for v in me.vertices) for i in range(3)], [max(v.co[i] for v in me.vertices) for i in range(3)]
    print("PROP %-14s tris=%4d mats=%d caixa=%.2f x %.2f x %.2f m -> %s" % (
        name, sum(len(f.vertices) - 2 for f in me.polygons), len(p.mats), hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2], path))
    bpy.data.objects.remove(ob)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "arte", "props"))
    ap.add_argument("names", nargs="*")
    a = ap.parse_args(argv)
    bad = [n for n in a.names if n not in PROPS]
    if bad:
        print("ERRO prop desconhecido: %s (existem: %s)" % (", ".join(bad), ", ".join(PROPS)))
        sys.exit(1)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name in a.names or PROPS:
        p = Prop()
        PROPS[name](p)
        export(name, p, os.path.abspath(a.out))
    print("PROPS_OK")


main()
