"""arte_v05_blender.py - Forge Street v0.5, leva 1 de arte (0 credito): icones de item + moeda, estande modular do
balcao e portoes laterais, modelados por primitivas no Blender 5.2 headless e renderizados com a camera/luz dos props
(render_sprites.py: Workbench, luz FLAT + contorno + cavidade, AA 8, ortografica a 60 graus sobre o chao).

Uso (1 comando regenera tudo; rodar da raiz do projeto Forge Street; docs/ASSETS.md secao v0.5):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1
      --python client/tools/arte_v05_blender.py -- [--out client/Assets/_FS/Resources/Sprites] [--check DIR] [itens balcao portao]
Saida por peca: <out>/<nome>/meta.json + 1 PNG por clipe, formato de render_sprites.py lido por SpriteSheet.cs.
  - itens (item_*, moeda): celula 128 px, ppu 128 (1 celula = 1 unidade, como os sprites de Art.cs), pivo no centro;
    clipes `icone` (3/4 na diagonal: balao, cartao, HUD) e `deitado` (de lado na camera de 60: pilha, bocas, estoque).
    Contorno #1E1612 de 3 px e sombra de 4 px a 35% (render a 512 px e reducao 4x).
  - item_*_em_pe: espada/escudo/martelo deitados no painel inclinado do estande, escala real (ppu 160), pivo no apoio.
  - balcao_*: modulos de 0,85 m (1 vaga) e pontas de 0,2 m (Balance.CounterEnd), ppu 160, pivo no centro do modulo no chao;
    meta.json traz `encaixes` [x0, y0, x1, y1, ...] em m relativos ao pivo, no plano do sprite (onde a View poe o item em pe).
  - pilar, portao_fechado/aberto, porta_servico_fechada/aberta: vistos DE LADO (parede norte-sul) com a mesma camera.
--check DIR grava as montagens de conferencia (balcao direto x montado, 4 e 8 vagas) e o relatorio de emenda.
"""
import argparse, json, math, os, sys, time
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from props_blender import ball, box, cyl, lathe, lin   # primitivas e sRGB -> linear dos props (Leva 11)

# familia -> (sombra, base, luz) #RRGGBB. Itens: matiz de Art.ItemColor (ART_BIBLE s3); estande/portoes: paleta dos props
# (props_blender.PAL), em que a "luz" e' so o tampo (regra dos props: normal.z > 0,55) e a sombra repete a base.
TONS = {
    "lamina": (0x4C88C4, 0x7FC4FF, 0xB0DCFF), "fio": (0xE2F2FF, 0xF4FAFF, 0xFFFFFF), "sulco": (0x3A6E9E, 0x4F86BC, 0x6FA4D8),
    "ouro": (0x9C7520, 0xE0B23A, 0xFFE08A), "couro": (0x3E2412, 0x5C3A1E, 0x7E5230), "couro2": (0x2C190B, 0x3E2412, 0x5C3A1E),
    "lingote": (0x6E7888, 0xA4AEBE, 0xC8D0DC), "lingote_topo": (0xDDE3EC, 0xEEF1F8, 0xFFFFFF), "brilho": (0xFFFFFF,) * 3,
    "rocha": (0x66503B, 0x9C8468, 0xB89C78), "veio": (0xE0561A, 0xFF7A1F, 0xFFB347), "nucleo": (0xFFD166, 0xFFE9A8, 0xFFF6D8),
    "escudo": (0xB4343C, 0xF2545B, 0xFF8E93), "aco": (0x4F5866, 0x8C96A6, 0xD5DCE6), "madeira_i": (0x6B4428, 0x8A5A32, 0xB8824F),
    "verde": (0x2F9A4A, 0x4CD964, 0x9DF0AC), "verde2": (0x1F6E35, 0x2F9A4A, 0x4CD964),
    "gema": (0x6E44B8, 0xB07CF2, 0xE4CCFF), "ouro_esc": (0x7A5A14, 0xB88A22, 0xD9A62E),
    # cenario (sombra = base: so o tampo clareia, como nos props)
    "madeira": (0x8A5A32, 0x8A5A32, 0xB8824F), "madeira2": (0x7E5230, 0x7E5230, 0xAA7646),
    "madeira_esc": (0x5C3A1E, 0x5C3A1E, 0x8A5A32), "tampo": (0x9A6A3E, 0x9A6A3E, 0xC99A62), "tampo2": (0x8E6038, 0x8E6038, 0xBC8E58),
    "painel": (0x8A5A32, 0x8A5A32, 0xB8824F), "rack": (0x5C3A1E, 0x5C3A1E, 0x7A4E2A), "rack2": (0x553519, 0x553519, 0x704726), "painel2": (0x80542F, 0x80542F, 0xAE7C4A),
    "ferro": (0x4F5866, 0x4F5866, 0x8C96A6), "ferro_cl": (0x8C96A6, 0x8C96A6, 0xD5DCE6), "ouro_p": (0xE0B23A, 0xE0B23A, 0xFFE08A),
    "pedra": (0x6E655C, 0x6E655C, 0x8E8478), "pedra2": (0x625A52, 0x625A52, 0x82786C), "pedra_cl": (0x6A6158, 0x6A6158, 0x857B6F),
    "ferro_esc": (0x3C434F, 0x3C434F, 0x5F6878),
}
CONTORNO = (0x1E, 0x16, 0x12)   # #1E1612 (BENCHMARK_VISUAL P0-1)
PPU = 160.0                     # px/m do estande e dos portoes (1 m ~ 95 px no aparelho: ~1,7x de folga)
ITEM, SS = 128, 4               # celula dos itens e supersample do render dos itens
SLOT, PONTA = 0.85, 0.2         # Balance.SlotStep e Balance.CounterEnd
BALCAO_V = (-0.52, 1.16)        # faixa vertical fixa do render do balcao (m no plano da camera): mesmos px em toda montagem
SAIDAS = []                     # PNG gravados


def R(axis, deg):
    return Matrix.Rotation(math.radians(deg), 4, axis)


def T(x, y, z):
    return Matrix.Translation((x, y, z))


def basis(ex, ey, ez):
    """Rotacao 4x4 que leva os eixos locais X, Y, Z para os vetores dados."""
    return Matrix((ex, ey, ez)).transposed().to_4x4()


# ------------------------------------------------------------------ malha por familias

class Peca:
    """Uma malha; cada add() funde uma primitiva com a familia de cor de cada face (o tom sai na hora da vista)."""

    def __init__(self):
        self.bm, self.fams = bmesh.new(), []

    def add(self, t, fam, m=None, at=(0, 0, 0), bevel=0.01, facefam=None, pick=None):
        if bevel > 0:
            bmesh.ops.bevel(t, geom=list(t.edges), offset=bevel, offset_type="OFFSET", segments=1, profile=0.5,
                            affect="EDGES", clamp_overlap=True)
        bmesh.ops.transform(t, matrix=m if m is not None else T(*at), verts=t.verts)
        t.normal_update()
        for i, f in enumerate(t.faces):
            name = facefam[i] if facefam else (pick(f) if pick else None) or fam
            if name not in self.fams:
                self.fams.append(name)
            f.material_index = self.fams.index(name)
        me = bpy.data.meshes.new("tmp")
        t.to_mesh(me)
        t.free()
        self.bm.from_mesh(me)
        bpy.data.meshes.remove(me)

    def verts(self, m):
        return [m @ v.co for v in self.bm.verts]


def prisma(poly, y0, y1):
    """Poligono [(x, z)] extrudado em Y de y0 a y1 (aceita concavo: o Blender triangula a n-gon)."""
    t = bmesh.new()
    a = [t.verts.new((x, y0, z)) for x, z in poly]
    b = [t.verts.new((x, y1, z)) for x, z in poly]
    t.faces.new(a)
    t.faces.new(list(reversed(b)))
    n = len(poly)
    for i in range(n):
        t.faces.new((a[i], b[i], b[(i + 1) % n], a[(i + 1) % n]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    return t


def lamina(w0, w1, z0, z1, z2, t, e):
    """Lamina de secao hexagonal (fio chanfrado nas duas bordas) de z0 a z1, afinando ate a ponta em z2.
    Devolve (bmesh, familia por face): chanfros = 'fio' (claro), faces largas = 'lamina'."""
    bm = bmesh.new()

    def sec(w, z):
        pts = ((-w / 2, 0), (-w / 2 + e, -t / 2), (w / 2 - e, -t / 2), (w / 2, 0), (w / 2 - e, t / 2), (-w / 2 + e, t / 2))
        return [bm.verts.new((x, y, z)) for x, y in pts]
    s0, s1, tip = sec(w0, z0), sec(w1, z1), bm.verts.new((0, 0, z2))
    kind = ["fio", "lamina", "fio", "fio", "lamina", "fio"]
    fams = []
    for k in range(6):
        bm.faces.new((s0[k], s0[(k + 1) % 6], s1[(k + 1) % 6], s1[k]))
        fams.append(kind[k])
    for k in range(6):
        bm.faces.new((s1[k], s1[(k + 1) % 6], tip))
        fams.append(kind[k])
    bm.faces.new(list(reversed(s0)))
    fams.append("lamina")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm, fams


# ------------------------------------------------------------------ itens (metros; frente = -Y)

def espada(p):
    """Eixo +Z, pomo em z = 0. Lamina 0,40 m azul-aco com fio claro; guarda dourada de 0,155 m (39% da lamina,
    BENCHMARK_VISUAL s4.1); punho de couro com tiras; pomo de ouro. Total 0,65 m."""
    p.add(ball(0.04, 0.034, 0.04, u=10, v=6), "ouro", at=(0, 0, 0.04), bevel=0)
    p.add(cyl(0.022, 0.13, 0.026, segs=10), "couro", at=(0, 0, 0.14), bevel=0.004)
    for z in (0.105, 0.145, 0.185):
        p.add(cyl(0.028, 0.012, segs=10), "couro2", at=(0, 0, z), bevel=0.003)
    p.add(box(0.11, 0.046, 0.04), "ouro", at=(0, 0, 0.225), bevel=0.008)
    for sx in (-1, 1):
        p.add(ball(0.024, 0.026, 0.026, u=8, v=5), "ouro", at=(sx * 0.058, 0, 0.225), bevel=0)
    bm, fams = lamina(0.072, 0.062, 0.245, 0.55, 0.65, 0.02, 0.015)
    p.add(bm, "lamina", bevel=0, facefam=fams)
    for sy in (-1, 1):
        p.add(box(0.012, 0.003, 0.2), "sulco", at=(0, sy * 0.0102, 0.36), bevel=0)


def escudo(p):
    """Escudo redondo de 0,39 m: face abaulada vermelha (#F2545B), aro de aco, umbo de ouro, 8 rebites. Face -Y, centro na origem."""
    m = R("X", -90)   # eixo do torno (Z) -> +Y; z negativo do perfil = frente
    p.add(lathe([(0.001, -0.036), (0.09, -0.03), (0.168, -0.012), (0.168, 0.02), (0.001, 0.02)], segs=28), "escudo", m=m, bevel=0)
    p.add(lathe([(0.162, -0.024), (0.192, -0.024), (0.194, 0.022), (0.164, 0.022)], segs=28, closed=True), "aco", m=m, bevel=0)
    p.add(ball(0.06, 0.034, 0.06, u=12, v=6), "ouro", at=(0, -0.032, 0), bevel=0)
    p.add(lathe([(0.068, -0.03), (0.08, -0.03), (0.08, -0.018), (0.068, -0.018)], segs=20, closed=True), "ouro", m=m, bevel=0)
    for k in range(8):
        a = 2 * math.pi * (k + 0.5) / 8
        p.add(ball(0.011, 0.008, 0.011, u=6, v=4), "aco", at=(0.178 * math.cos(a), -0.027, 0.178 * math.sin(a)), bevel=0)


def martelo(p):
    """Martelo de forja: cabo +Z de 0,42 m com pano verde (#4CD964), cabeca de aco em X com faixa verde no olho."""
    p.add(cyl(0.024, 0.42, 0.02, segs=10), "madeira_i", at=(0, 0, 0.21), bevel=0.004)
    p.add(cyl(0.03, 0.03, segs=10), "madeira_i", at=(0, 0, 0.015), bevel=0.006)
    p.add(cyl(0.034, 0.2, segs=10), "verde", at=(0, 0, 0.15), bevel=0.006)
    for z in (0.055, 0.15, 0.245):
        p.add(cyl(0.037, 0.016, segs=10), "verde2", at=(0, 0, z), bevel=0.004)
    zc = 0.425
    p.add(box(0.16, 0.074, 0.078), "aco", at=(0, 0, zc), bevel=0.01)
    p.add(box(0.05, 0.094, 0.098), "aco", at=(0.095, 0, zc), bevel=0.012)
    t = cyl(0.046, 0.075, 0.016, segs=4)
    bmesh.ops.rotate(t, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(45), 3, "Z"), verts=t.verts)
    bmesh.ops.scale(t, vec=(1.0, 0.85, 1.0), verts=t.verts)
    p.add(t, "aco", m=T(-0.115, 0, zc) @ R("Y", -90), bevel=0.004)
    p.add(box(0.04, 0.084, 0.088), "verde", at=(0, 0, zc), bevel=0.006)


def anel(p):
    """Anel de ouro (aro 0,1 m) com gema roxa grande (#B07CF2, cintura 0,128 m) no topo; aro no plano XZ, gema para +Z."""
    rr, r = 0.1, 0.026
    prof = [(rr + r * math.cos(2 * math.pi * k / 10), r * math.sin(2 * math.pi * k / 10)) for k in range(10)]
    p.add(lathe(prof, segs=28, closed=True), "ouro", m=R("X", -90), bevel=0)
    zb, g = rr + r * 0.55, 1.3   # g: escala da gema e do engaste (gema grande, BENCHMARK_VISUAL)
    p.add(lathe([(x * g, z * g) for x, z in ((0.02, 0.0), (0.05, 0.028), (0.06, 0.04), (0.052, 0.046), (0.03, 0.03), (0.01, 0.01))],
                segs=10, closed=True), "ouro", at=(0, 0, zb), bevel=0)
    zg = zb + 0.046 * g
    p.add(lathe([(x * g, z * g) for x, z in ((0.001, -0.05), (0.064, 0.0), (0.064, 0.012), (0.04, 0.042), (0.001, 0.042))], segs=8),
          "gema", at=(0, 0, zg), bevel=0)
    for k in range(4):   # garras
        a = 2 * math.pi * (k + 0.5) / 4
        p.add(ball(0.013, 0.013, 0.019, u=6, v=4), "ouro", at=(0.08 * math.cos(a), 0.08 * math.sin(a), zg + 0.01), bevel=0)
    p.add(box(0.03, 0.015, 0.004), "brilho", m=T(-0.016, -0.016, zg + 0.042 * g + 0.001) @ R("Z", 40), bevel=0)


ANVIL = [(-0.052, 0.018), (-0.03, 0.032), (0.046, 0.032), (0.046, 0.012), (0.016, 0.0), (0.02, -0.02), (0.036, -0.032),
         (-0.026, -0.032), (-0.012, -0.02), (-0.008, 0.0), (-0.03, 0.012)]


def _h(v):
    """Ruido fixo por vertice (sem random: render reproduzivel)."""
    x = math.sin(v.x * 12.9898 + v.y * 78.233 + v.z * 37.719) * 43758.5453
    return x - math.floor(x)


def minerio(p):
    """Pedra facetada de ~0,3 m com veios laranja (#FF7A1F) atravessando e 3 cristais brilhantes."""
    t = bmesh.new()
    bmesh.ops.create_icosphere(t, subdivisions=2, radius=1.0)
    for v in t.verts:
        k = 0.82 + 0.3 * _h(v.co)
        v.co = Vector((v.co.x * 0.17 * k, v.co.y * 0.15 * k, max(v.co.z * 0.15 * k, -0.09)))
    t.normal_update()
    nv = Vector((0.75, -0.35, 0.55)).normalized()

    def veio(f):
        c = f.calc_center_median()
        return "veio" if abs(c.dot(nv) - 0.015) < 0.012 else None   # 1 veio: o marrom domina (Art.ItemColor[0])
    p.add(t, "rocha", bevel=0, pick=veio)
    for d, ln, r in (((-0.62, -0.25, 0.74), 0.12, 0.045), ((-0.22, -0.35, 0.91), 0.14, 0.05), ((0.08, -0.15, 0.98), 0.1, 0.04),
                     ((0.5, -0.3, 0.81), 0.11, 0.042), ((0.8, -0.35, 0.48), 0.08, 0.035)):   # cristais para cima: aparecem na silhueta
        n = Vector(d).normalized()
        q = n.to_track_quat("Z", "Y").to_matrix().to_4x4()
        base = Vector((n.x * 0.13, n.y * 0.115, n.z * 0.11))
        p.add(cyl(r, ln, 0.0, segs=5), "veio", m=T(*(base + n * (ln * 0.25))) @ q, bevel=0,
              pick=lambda f: "nucleo" if f.normal.z > 0.35 else None)


def lingote(p):
    """Barra trapezoidal chanfrada 0,34 x 0,15 m, tampo claro (#EEF1F8) com dois riscos de brilho."""
    t = box(0.34, 0.15, 0.09)
    for v in t.verts:
        if v.co.z > 0:
            v.co.x *= 0.8
            v.co.y *= 0.66
    p.add(t, "lingote", at=(0, 0, 0.045), bevel=0.012, pick=lambda f: "lingote_topo" if f.normal.z > 0.9 else None)
    for x, ln in ((-0.045, 0.1), (0.035, 0.035)):
        p.add(box(ln, 0.01, 0.004), "brilho", m=T(x, -0.026, 0.0905) @ R("Z", -18), bevel=0)
    p.add(prisma([(x * 0.85, z * 0.85) for x, z in ANVIL], 0.0, 0.004), "lingote", m=T(0.055, 0.012, 0.089) @ R("X", 90), bevel=0)


def moeda(p):
    """Moeda de ouro de 0,2 m com aro alto e bigorna em relevo (heraldica da ART_BIBLE s1)."""
    p.add(cyl(0.1, 0.024, segs=28), "ouro", m=R("X", 90), bevel=0.004)
    p.add(lathe([(0.084, -0.018), (0.101, -0.018), (0.101, 0.018), (0.084, 0.018)], segs=28, closed=True), "ouro", m=R("X", -90), bevel=0)
    p.add(cyl(0.085, 0.004, segs=28), "ouro_esc", m=T(0, -0.0125, 0) @ R("X", 90), bevel=0)
    p.add(prisma([(x * 1.25, z * 1.25) for x, z in ANVIL], -0.02, -0.012), "ouro", bevel=0)


# vistas: (construtor, pose do icone, pose deitada). Icone visto de frente (elevacao 0); "deitado" pela camera de 60 do jogo.
ITENS = {
    "item_minerio": (minerio, R("X", 22) @ R("Z", 18), Matrix()),
    "item_lingote": (lingote, R("X", 32) @ R("Z", -24), R("Z", -8)),
    "item_espada": (espada, R("Y", 45) @ R("Z", 18), basis((0, -1, 0), (0, 0, -1), (1, 0, 0))),   # ponta +X, face larga para cima
    "item_escudo": (escudo, R("X", 12) @ R("Z", 22), R("X", -90)),
    "item_ferramenta": (martelo, R("Y", -40) @ R("Z", -20), basis((0, 1, 0), (0, 0, 1), (1, 0, 0))),   # diagonal oposta a espada
    "item_joia": (anel, R("X", 28) @ R("Z", 26), R("Z", 180) @ R("X", 60)),                             # deitado, gema no fundo
    "moeda": (moeda, R("X", 12) @ R("Z", 28), R("X", -90)),
}


# ------------------------------------------------------------------ objeto, tons e render

_mats = {}


def mat(fam, tone):
    key = "%s:%d" % (fam, tone)
    if key not in _mats:
        col = lin(TONS[fam][tone])
        m = bpy.data.materials.new(key)
        if m.node_tree is None:
            m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")   # por tipo, nunca por nome
        bsdf.inputs["Base Color"].default_value = col
        m.diffuse_color = col   # o Workbench le esta
        _mats[key] = m
    return _mats[key]


def tom_item(right, up):
    """Cel shading do item relativo a vista: face voltada para a camera = base; inclinada para cima-esquerda = luz;
    para baixo-direita = sombra (luz do lado da camera, alta a esquerda)."""
    L = (right.cross(up) * 0.8 + up * 0.2 - right * 0.55).normalized()
    return lambda n: 2 if n.dot(L) > 0.9 else 0 if n.dot(L) < 0.5 else 1


def tom_cenario(right, up):
    return lambda n: 2 if n.z > 0.55 else 1   # regra dos props (props_blender.Prop.add)


def objeto(peca, M, tom):
    bm = peca.bm.copy()
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    bm.normal_update()
    keys = []
    for f in bm.faces:
        k = (peca.fams[f.material_index], tom(f.normal))
        if k not in keys:
            keys.append(k)
        f.material_index = keys.index(k)
    me = bpy.data.meshes.new("v")
    bm.to_mesh(me)
    bm.free()
    for fam, tone in keys:
        me.materials.append(mat(fam, tone))
    ob = bpy.data.objects.new("v", me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def cena():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine, sc.render.film_transparent = "BLENDER_WORKBENCH", True   # = render_sprites.main()
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform, sc.display.render_aa = "Standard", "8"
    sh = sc.display.shading
    sh.light, sh.color_type, sh.show_object_outline, sh.show_cavity, sh.show_shadows = "FLAT", "MATERIAL", True, True, False
    sc.render.resolution_percentage = 100
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type, cam.data.sensor_fit, cam.data.clip_end = "ORTHO", "HORIZONTAL", 1000.0
    sc.collection.objects.link(cam)
    sc.camera = cam


def cam_60(elev=60.0):
    """Eixos (direita, cima) da camera ortografica olhando para o norte, `elev` graus sobre o chao (render_sprites.main)."""
    e = math.radians(elev)
    return Vector((1, 0, 0)), Vector((0, math.sin(e), math.cos(e)))


def faixa(pts, right, up):
    us, vs = [p.dot(right) for p in pts], [p.dot(up) for p in pts]
    return min(us), max(us), min(vs), max(vs)


def render(objs, right, up, center_uv, ppu, w, h, path):
    """O ponto (u, v) = center_uv do plano da camera cai no centro da imagem w x h, a `ppu` px/m."""
    sc = bpy.context.scene
    back = right.cross(up)
    c = right * center_uv[0] + up * center_uv[1]
    sc.camera.matrix_world = Matrix.Translation(c + back * 50.0) @ basis(right, up, back)
    sc.camera.data.ortho_scale = w / ppu
    sc.render.resolution_x, sc.render.resolution_y, sc.render.filepath = w, h, path
    bpy.ops.render.render(write_still=True)
    for o in objs:
        me = o.data
        bpy.data.objects.remove(o)
        bpy.data.meshes.remove(me)


# ------------------------------------------------------------------ pos-producao (numpy do Blender, sem PIL)

def ler(path):
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    a = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(h, w, 4)[::-1].copy()   # linha 0 em cima


def gravar(a, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    h, w, _ = a.shape
    img = bpy.data.images.new("o", w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(np.clip(a[::-1], 0, 1), dtype=np.float32).ravel())
    img.filepath_raw, img.file_format = path, "PNG"
    img.save()
    bpy.data.images.remove(img)
    SAIDAS.append(path)


def render_ler(objs, right, up, center_uv, ppu, w, h, tmp):
    render(objs, right, up, center_uv, ppu, w, h, tmp)
    a = ler(tmp)
    os.remove(tmp)
    return a


def sobre(top, bot):
    """Composicao alfa: top sobre bot (RGBA reto)."""
    ta, ba = top[..., 3:4], bot[..., 3:4]
    oa = ta + ba * (1 - ta)
    rgb = (top[..., :3] * ta + bot[..., :3] * ba * (1 - ta)) / np.maximum(oa, 1e-6)
    return np.concatenate([rgb, oa], axis=2)


def dilatar(a, r):
    """Alfa dilatado por um disco de raio r px (borda antisserrilhada)."""
    n = int(math.ceil(r))
    h, w = a.shape
    pad = np.pad(a, n)
    o = np.zeros_like(a)
    for dy in range(-n, n + 1):
        for dx in range(-n, n + 1):
            k = min(1.0, r + 0.5 - math.hypot(dx, dy))
            if k > 0:
                o = np.maximum(o, pad[n + dy:n + dy + h, n + dx:n + dx + w] * k)
    return o


def com_contorno(rgba, r, sombra_dy=0, sombra_a=0.0):
    """Contorno #1E1612 de r px atras do item e, se pedido, sombra da silhueta deslocada `sombra_dy` px para baixo."""
    o = dilatar(rgba[..., 3], r)
    cor = np.array([c / 255 for c in CONTORNO], np.float32)
    out = sobre(rgba, np.concatenate([np.broadcast_to(cor, o.shape + (3,)), o[..., None]], axis=2))
    if sombra_a > 0:
        s = np.zeros_like(o)
        s[sombra_dy:] = out[:-sombra_dy, :, 3] * sombra_a
        out = sobre(out, np.concatenate([np.zeros(o.shape + (3,), np.float32), s[..., None]], axis=2))
    return out


def reduzir(a, k):
    h, w, _ = a.shape
    pre = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)
    m = pre[:h // k * k, :w // k * k].reshape(h // k, k, w // k, k, 4).mean(axis=(1, 3))
    return np.concatenate([m[..., :3] / np.maximum(m[..., 3:4], 1e-6), m[..., 3:4]], axis=2)


def borrar(a, sigma):
    """Gaussiana separavel (sombra de contato do estande)."""
    r = int(3 * sigma)
    g = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    g /= g.sum()
    a = np.apply_along_axis(lambda m: np.convolve(m, g, mode="same"), 0, a)
    return np.apply_along_axis(lambda m: np.convolve(m, g, mode="same"), 1, a)


def meta(out, nome, size, ppu, pivot, clips, extra=None):
    m = {"size": int(size), "fps": 12.0, "dirs": 1, "dir_order": ["S"], "row0": "top", "elev": 60.0, "yaw": 0.0,
         "ppu": round(ppu, 3), "pivot": [round(pivot[0], 4), round(pivot[1], 4)], "light": "flat", "src_fps": 30,
         "fonte": "tools/arte_v05_blender.py (procedural, 0 credito)",
         "clips": [{"name": c, "file": c + ".png", "frames": 1, "loop": False, "fps": 30.0, "duration_s": 0.033,
                    "src_frames": 0} for c in clips]}
    m.update(extra or {})
    with open(os.path.join(out, nome, "meta.json"), "w") as f:
        json.dump(m, f, indent=1)


# ------------------------------------------------------------------ itens

def vista_item(peca, M, elev, path, box_px):
    """Pose M cabendo em box_px (px finais) no centro de uma celula ITEM x ITEM; contorno 3 px + sombra 4 px a 35%."""
    right, up = cam_60(elev)
    u0, u1, v0, v1 = faixa(peca.verts(M), right, up)
    ppu = SS * min(box_px[0] / (u1 - u0), box_px[1] / (v1 - v0))
    n = ITEM * SS
    # o centro da imagem fica 2 px abaixo do centro do item: a sombra de 4 px desce
    a = render_ler([objeto(peca, M, tom_item(right, up))], right, up, ((u0 + u1) / 2, (v0 + v1) / 2 - 2 * SS / ppu),
                   ppu, n, n, path + ".raw.png")
    gravar(reduzir(com_contorno(a, 3 * SS, 4 * SS, 0.35), SS), path)


def itens(out):
    for nome, (build, icon, flat) in ITENS.items():
        p = Peca()
        build(p)
        vista_item(p, icon, 0.0, os.path.join(out, nome, "icone.png"), (110, 106))
        vista_item(p, flat, 60.0, os.path.join(out, nome, "deitado.png"), (112, 100))
        meta(out, nome, ITEM, ITEM, (0.5, 0.5), ["icone", "deitado"])
        print("ITEM %-16s icone + deitado" % nome)


# em pe no estande: o item deitado no painel inclinado do balcao (37,5 graus da vertical), pivo no apoio (borda de baixo)
RACK_D = Vector((0, 0.36, 0.47)).normalized()        # sobe o painel (para o fundo)
RACK_N = Vector((0, -RACK_D.z, RACK_D.y))            # normal do painel (para cima/sul, para a camera)
PAINEL_P0 = Vector((0, 0.0, 0.95))                   # pe do painel (sobre o tampo), em coordenadas do modulo
APOIO = PAINEL_P0 + RACK_D * 0.04                     # onde a borda de baixo do item encosta (sobre a prateleira)


def no_painel(peca, eixo, frente):
    """Pose que deita o item no painel: eixo local -> sobe o painel, frente local -> normal; borda de baixo/costas na origem."""
    a, f = Vector(eixo).normalized(), Vector(frente).normalized()
    M = basis(RACK_N, RACK_D, RACK_N.cross(RACK_D)) @ basis(f, a, f.cross(a)).transposed()
    pts = peca.verts(M)
    lo_d, lo_n = min(q.dot(RACK_D) for q in pts), min(q.dot(RACK_N) for q in pts)
    return T(*(-(RACK_D * lo_d) - RACK_N * lo_n)) @ M


EM_PE = {"item_espada_em_pe": espada, "item_escudo_em_pe": escudo, "item_ferramenta_em_pe": martelo}


def em_pe(out):
    right, up = cam_60()
    for nome, build in EM_PE.items():
        p = Peca()
        build(p)
        M = no_painel(p, (0, 0, 1), (0, -1, 0))
        u0, u1, v0, v1 = faixa(p.verts(M), right, up)
        mg = 0.03
        w = int(math.ceil((max(-u0, u1) + mg) * 2 * PPU))
        w += w % 2
        h = int(math.ceil((v1 - v0 + 2 * mg) * PPU))
        piv = (0.5, (mg - v0) * PPU / h)
        path = os.path.join(out, nome, "Static.png")
        a = render_ler([objeto(p, M, tom_item(right, up))], right, up, (0.0, v0 - mg + h / PPU / 2), PPU * SS, w * SS, h * SS,
                       path + ".raw.png")
        gravar(reduzir(com_contorno(a, 2 * SS), SS), path)
        meta(out, nome, w, PPU, piv, ["Static"], {"apoio": "pivo = borda de baixo do item encostada no painel do estande"})
        print("EM_PE %-22s %dx%d px pivo=(%.3f, %.3f)" % (nome, w, h, piv[0], piv[1]))


# ------------------------------------------------------------------ estande do balcao (modulos por vaga)

ENCAIXES = {"balcao_meio": [-0.315, -0.105, 0.105, 0.315], "balcao_escudos": [-0.21, 0.21], "balcao_ferramentas": [-0.27, 0.0, 0.27]}


def corpo_balcao(p, mods):
    """Balcao continuo: tampo de 2 tabuas, faixa de ferro, rodape; frente de tabuas POR MODULO (0,85 = 5 x 0,17, sempre o
    mesmo padrao, para qualquer modulo encaixar em qualquer posicao); postes com cantoneiras nas juntas (metade em cada)."""
    x0, x1 = mods[0][1], mods[-1][2]
    juntas = [m[1] for m in mods[1:]]
    L, xc = x1 - x0, (x0 + x1) / 2
    p.add(box(L, 0.62, 0.78), "madeira_esc", at=(xc, 0.02, 0.47), bevel=0)
    p.add(box(L + 0.02, 0.05, 0.08), "madeira_esc", at=(xc, -0.335, 0.04))
    p.add(box(L, 0.04, 0.08), "madeira_esc", at=(xc, -0.33, 0.82))
    p.add(box(L + 0.06, 0.37, 0.06), "tampo", at=(xc, -0.17, 0.89), bevel=0.012)
    p.add(box(L + 0.06, 0.34, 0.06), "tampo2", at=(xc, 0.19, 0.89), bevel=0.012)
    p.add(box(L, 0.012, 0.045), "ferro", at=(xc, -0.348, 0.6), bevel=0.003)
    for _, a, b in mods:
        n = max(1, int(round((b - a) / 0.17)))
        w = (b - a) / n
        for k in range(n):
            x = a + w * (k + 0.5)
            p.add(box(w - 0.008, 0.03, 0.70), "painel" if k % 2 == 0 else "painel2", at=(x, -0.325, 0.43), bevel=0.006)
            p.add(ball(0.011, 0.008, 0.011, u=6, v=4), "ferro_cl", at=(x, -0.356, 0.6), bevel=0)
    for bx in juntas:
        p.add(box(0.08, 0.05, 0.78), "madeira_esc", at=(bx, -0.34, 0.47), bevel=0.008)
        for z in (0.14, 0.78):
            p.add(box(0.1, 0.012, 0.07), "ferro", at=(bx, -0.368, z), bevel=0.003)


def painel(p, x0, x1, juntas):
    """Painel inclinado de expor (37,5 graus da vertical) na metade de tras do tampo, continuo, com prateleira de apoio
    embaixo e travessa no topo; postes de apoio atras, nas juntas."""
    L, xc = x1 - x0, (x0 + x1) / 2
    ln = math.hypot(0.36, 0.47)
    base = Vector((xc, PAINEL_P0.y, PAINEL_P0.z))
    for i in range(3):
        c = base + RACK_D * ((i + 0.5) / 3 * ln) - RACK_N * 0.02
        p.add(box(L, 0.04, ln / 3 - 0.008), "rack" if i % 2 == 0 else "rack2", m=T(*c) @ R("X", -37.5), bevel=0.006)
    c = base + RACK_N * 0.012
    p.add(box(L, 0.05, 0.05), "madeira_esc", at=(xc, c.y - 0.01, c.z), bevel=0.008)
    top = base + RACK_D * ln
    p.add(box(L, 0.06, 0.06), "madeira_esc", at=(xc, top.y + 0.01, top.z + 0.01), bevel=0.008)
    for bx in juntas:
        p.add(box(0.05, 0.05, 0.5), "madeira_esc", at=(bx, 0.33, 1.17), bevel=0.006)


def acessorios(p, tipo, xc):
    """O que muda entre modulos: copos de ferro + presilhas (espadas), argolas (escudos), trilho com pinos (ferramentas)."""
    pe = PAINEL_P0 + RACK_N * 0.035
    for dx in ENCAIXES[tipo]:
        if tipo == "balcao_meio":
            p.add(box(0.07, 0.04, 0.022), "ferro", at=(xc + dx, pe.y, pe.z + 0.005), bevel=0.004)
            q = PAINEL_P0 + RACK_D * 0.5 + RACK_N * 0.004
            p.add(box(0.014, 0.01, 0.06), "ferro", m=T(xc + dx, q.y, q.z) @ R("X", -37.5), bevel=0)
        elif tipo == "balcao_escudos":
            q = PAINEL_P0 + RACK_D * 0.46 + RACK_N * 0.02
            p.add(lathe([(0.03, -0.006), (0.042, -0.006), (0.042, 0.006), (0.03, 0.006)], segs=12, closed=True), "ferro",
                  m=T(xc + dx, q.y, q.z) @ R("X", -127.5), bevel=0)
            p.add(box(0.12, 0.04, 0.022), "ferro", at=(xc + dx, pe.y, pe.z + 0.005), bevel=0.004)
        else:
            q = PAINEL_P0 + RACK_D * 0.42 + RACK_N * 0.03
            p.add(cyl(0.012, 0.07, segs=8), "ferro_cl", m=T(xc + dx, q.y, q.z) @ R("X", -127.5), bevel=0)
    if tipo == "balcao_ferramentas":
        q = PAINEL_P0 + RACK_D * 0.42 + RACK_N * 0.012
        p.add(box(SLOT - 0.2, 0.025, 0.025), "ferro", at=(xc, q.y, q.z), bevel=0.004)


def ponta(p, x, lado):
    """Ponta do balcao: postes de canto (frente baixo, fundo alto) com pomo dourado, cantoneiras e tampa lateral do painel."""
    for y, hh in ((-0.33, 1.02), (0.33, 1.5)):
        p.add(box(0.1, 0.1, hh), "madeira_esc", at=(x, y, hh / 2), bevel=0.01)
        p.add(ball(0.045, 0.045, 0.045, u=10, v=6), "ouro_p", at=(x, y, hh + 0.04), bevel=0)
    for z in (0.14, 0.78):
        p.add(box(0.12, 0.014, 0.08), "ferro", at=(x, -0.388, z), bevel=0.003)
        p.add(box(0.014, 0.12, 0.08), "ferro", at=(x + lado * 0.058, -0.33, z), bevel=0.003)
    t = prisma([(0.0, 0.95), (0.36, 1.42), (0.36, 0.95)], -0.025, 0.025)
    bmesh.ops.transform(t, matrix=R("Z", 90), verts=t.verts)   # perfil da rampa no plano YZ, espessura em X
    p.add(t, "madeira_esc", at=(x - lado * 0.075, 0, 0), bevel=0.005)


def balcao_montagem(tipos):
    """['balcao_ponta_esq', 'balcao_meio', ..., 'balcao_ponta_dir'] -> (Peca, [(nome, x0, x1)]), x da esquerda = 0."""
    p, mods, x = Peca(), [], 0.0
    for t in tipos:
        w = PONTA if "ponta" in t else SLOT
        mods.append((t, x, x + w))
        x += w
    corpo_balcao(p, mods)
    painel(p, 0.1, x - 0.1, [m[1] for m in mods[1:]])
    for t, a, b in mods:
        if t in ENCAIXES:
            acessorios(p, t, (a + b) / 2)
    ponta(p, 0.05, -1)
    ponta(p, x - 0.05, 1)
    return p, mods


def render_balcao(p, total, tmp):
    """Balcao inteiro na camera de 60 a PPU, x = 0 em px inteiro, faixa vertical fixa, + sombra de contato baked."""
    right, up = cam_60()
    _, _, v0, v1 = faixa(p.verts(Matrix()), right, up)
    if v0 < BALCAO_V[0] + 0.15 or v1 > BALCAO_V[1] - 0.02:
        print("AVISO balcao fora da faixa vertical: %.2f..%.2f" % (v0, v1))
    mg = 32   # px de margem nas pontas (beiral do tampo, contorno, sombra)
    w, h = int(round(total * PPU)) + 2 * mg, int(round((BALCAO_V[1] - BALCAO_V[0]) * PPU))
    a = render_ler([objeto(p, Matrix(), tom_cenario(right, up))], right, up, (total / 2, BALCAO_V[0] + h / PPU / 2), PPU, w, h, tmp)
    # sombra de contato: pegada no chao projetada, borrada, 35%; 3 px para a direita (luz-chave de baixo-esquerda, ART_BIBLE s7)
    s = np.zeros(a.shape[:2], np.float32)
    row = lambda yy: int(round(h - (yy * up.y - BALCAO_V[0]) * PPU))
    s[row(0.37):row(-0.42), mg - 5:mg + int(round(total * PPU)) + 11] = 1.0
    s = borrar(s, 7.0) * 0.35
    a = sobre(a, np.concatenate([np.zeros(a.shape[:2] + (3,), np.float32), s[..., None]], axis=2))
    return a, mg, -BALCAO_V[0] * PPU


def balcao(out, check):
    ref = ["balcao_ponta_esq"] + ["balcao_meio"] * 3 + ["balcao_escudos"] * 3 + ["balcao_ferramentas"] * 3 + ["balcao_ponta_dir"]
    p, mods = balcao_montagem(ref)
    img, mg, piv_px = render_balcao(p, mods[-1][2], os.path.join(out, "_balcao.raw.png"))
    h = img.shape[0]
    _, up = cam_60()
    pick = {"balcao_ponta_esq": 0, "balcao_meio": 2, "balcao_escudos": 5, "balcao_ferramentas": 8, "balcao_ponta_dir": len(ref) - 1}
    for nome, i in pick.items():
        t, a, b = mods[i]
        c0 = 0 if t == "balcao_ponta_esq" else mg + int(round(a * PPU))
        c1 = mg + int(round(b * PPU)) + (mg if t == "balcao_ponta_dir" else 0)
        crop = img[:, c0:c1]
        gravar(crop, os.path.join(out, nome, "Static.png"))
        cx = mg + (a + b) / 2 * PPU - c0
        extra = {"largura_m": round(b - a, 3), "junta_px": [mg + int(round(a * PPU)) - c0, mg + int(round(b * PPU)) - c0]}
        if t in ENCAIXES:
            extra["encaixes"] = [round(c, 4) for dx in ENCAIXES[t] for c in (dx, APOIO.dot(up))]
        meta(out, nome, crop.shape[1], PPU, (cx / crop.shape[1], piv_px / h), ["Static"], extra)
        print("BALCAO %-20s %dx%d px largura=%.2f m" % (nome, crop.shape[1], h, b - a))
    if not check:
        return
    os.makedirs(check, exist_ok=True)
    rel = []
    for n in (4, 8):
        mid = (["balcao_meio", "balcao_escudos", "balcao_meio", "balcao_ferramentas"] * 2)[:n]
        tipos = ["balcao_ponta_esq"] + mid + ["balcao_ponta_dir"]
        q, ms = balcao_montagem(tipos)
        direto, mg2, _ = render_balcao(q, ms[-1][2], os.path.join(check, "_d.raw.png"))
        mont = np.zeros_like(direto)
        for t, a, b in ms:
            c = ler(os.path.join(out, t, "Static.png"))
            x = 0 if t == "balcao_ponta_esq" else mg2 + int(round(a * PPU))
            mont[:, x:x + c.shape[1]] = c
        d = np.abs(direto - mont).max(axis=2)
        rel.append("%d vagas (%s): dif max %.1f/255, p99,9 %.1f/255, px com dif > 4/255: %d de %d" % (
            n, " ".join(t.replace("balcao_", "") for t in tipos), d.max() * 255, np.percentile(d, 99.9) * 255,
            int((d > 4 / 255).sum()), d.size))
        gravar(direto, os.path.join(check, "balcao_%d_direto.png" % n))
        gravar(mont, os.path.join(check, "balcao_%d_montado.png" % n))
    with open(os.path.join(check, "emenda.txt"), "w") as f:
        f.write("\n".join(rel) + "\n")
    print("EMENDA " + " | ".join(rel))


# ------------------------------------------------------------------ portoes laterais (parede norte-sul, vistos de lado)
# Mundo: parede ao longo de Y (x 9,0-9,6), oficina em -X, rua lateral em +X. Origem = centro do vao na linha central da
# parede (x 9,3), no chao. O modelo ja nasce girado (ao longo de Y) e a camera e' a de 60 do jogo, olhando para o norte.

def pilar(p):
    """Batente de pedra 0,72 (atravessa a parede) x 0,46 m, 5 fiadas de cantaria alternada, capitel e topo em piramide."""
    for i in range(5):
        dx, dy = (0.72, 0.46) if i % 2 == 0 else (0.66, 0.4)
        p.add(box(dx, dy, 0.288), "pedra" if i % 2 == 0 else "pedra2", at=(0, 0, 0.15 + 0.3 * i), bevel=0.02)
    p.add(box(0.8, 0.54, 0.1), "pedra_cl", at=(0, 0, 1.55), bevel=0.02)
    t = cyl(0.36, 0.14, 0.04, segs=4)
    bmesh.ops.rotate(t, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(45), 3, "Z"), verts=t.verts)
    bmesh.ops.scale(t, vec=(1.05, 0.72, 1.0), verts=t.verts)
    p.add(t, "pedra_cl", at=(0, 0, 1.67), bevel=0.01)


def folha(p, M, larg, alt, esp, tiras, tampas):
    """Folha de tabuas verticais (local: largura em +Y a partir da dobradica, espessura em X centrada) com tiras de ferro
    cravejadas nas duas faces e tampas de ferro por cima das tabuas (o que se ve de lado); M leva o local ao mundo."""
    n = max(3, int(round(larg / 0.15)))
    w = larg / n
    for i in range(n):
        hh = alt - (0.035 if i % 2 else 0.0)
        p.add(box(esp, w - 0.01, hh), "madeira" if i % 2 == 0 else "madeira2", m=M @ T(0, w * (i + 0.5), hh / 2), bevel=0.008)
    for z in tiras:
        for sx in (-1, 1):
            p.add(box(0.014, larg - 0.04, 0.06), "ferro", m=M @ T(sx * (esp / 2 + 0.007), larg / 2, z), bevel=0.003)
            for k in range(4):
                p.add(ball(0.012, 0.012, 0.012, u=6, v=4), "ferro_cl", m=M @ T(sx * (esp / 2 + 0.016), 0.08 + k * (larg - 0.16) / 3, z), bevel=0)
    for f in tampas:
        p.add(box(esp + 0.04, 0.1, 0.035), "ferro_esc", m=M @ T(0, larg * f, alt + 0.012), bevel=0.004)
        p.add(ball(0.016, 0.016, 0.012, u=6, v=4), "ferro", m=M @ T(0, larg * f, alt + 0.035), bevel=0)


def portao(p, aberto, servico=False):
    """Portao da rua lateral (2 folhas de 0,9 x 1,5 m, 0,3 de espessura, tranca) ou porta de servico (1,2 m, 0,2, trinco).
    Fechado: folhas no meio da parede (de lado, o tampo das tabuas com as tampas de ferro preenche o vao entre os pilares).
    Aberto: folhas giradas 150 graus na face de fora (x 0,3), rentes ao muro do lado da rua; o vao fica livre."""
    alt, esp = (1.2, 0.2) if servico else (1.5, 0.3)   # fechado: folhas no meio da parede, grossas (de lado so o tampo aparece)
    tiras = (0.25, 0.9) if servico else (0.3, 0.75, 1.2)
    tampas = (0.3, 0.75) if servico else (0.25, 0.75)
    for s in (-1, 1):   # folha sul (s = -1) e norte (s = +1), dobradica em y = s * 0,9
        if aberto:   # gira 150 graus na face de fora (x 0,3) e fica rente ao muro, do lado da rua
            M = T(0.3, s * 0.9, 0.05) @ R("Z", 150.0 * s) @ T(-esp / 2, 0, 0)
        else:
            M = T(0, s * 0.9, 0.05)
        M = M @ (R("Z", 180) if s > 0 else Matrix())   # a folha cresce da dobradica para o centro do vao
        folha(p, M, 0.895, alt, esp, tiras, tampas)
        for z in (tiras[0], tiras[-1]):
            p.add(cyl(0.022, 0.1, segs=8), "ferro", at=(0.31 if aberto else esp / 2 + 0.01, s * 0.9, 0.05 + z), bevel=0.004)
        if not aberto:   # argola de puxar, do lado da oficina
            p.add(lathe([(0.04, -0.007), (0.054, -0.007), (0.054, 0.007), (0.04, 0.007)], segs=12, closed=True), "ferro",
                  m=T(-esp / 2 - 0.02, s * 0.12, alt * 0.55) @ R("Y", 90), bevel=0)
    if not aberto and not servico:   # tranca: viga atravessada do lado da oficina, em suportes de ferro
        xb = -esp / 2 - 0.07
        p.add(box(0.12, 2.0, 0.13), "madeira_esc", at=(xb, 0, 0.8), bevel=0.012)
        for y in (-0.75, -0.2, 0.2, 0.75):
            for z in (0.88, 0.72):
                p.add(box(0.16, 0.05, 0.03), "ferro", at=(xb + 0.02, y, z), bevel=0.004)
    if not aberto and servico:   # trinco de ferro atravessando as duas folhas
        p.add(box(0.03, 0.5, 0.04), "ferro", at=(-esp / 2 - 0.02, 0, 0.7), bevel=0.004)
        p.add(box(0.04, 0.05, 0.08), "ferro_cl", at=(-esp / 2 - 0.03, 0.18, 0.7), bevel=0.004)


PORTOES = {
    "pilar": pilar,
    "portao_fechado": lambda p: portao(p, False),
    "portao_aberto": lambda p: portao(p, True),
    "porta_servico_fechada": lambda p: portao(p, False, True),
    "porta_servico_aberta": lambda p: portao(p, True, True),
}


def portoes(out):
    right, up = cam_60()
    for nome, build in PORTOES.items():
        p = Peca()
        build(p)
        u0, u1, v0, v1 = faixa(p.verts(Matrix()), right, up)
        mg = 0.04
        w = int(math.ceil((max(-u0, u1) + mg) * 2 * PPU))
        w += w % 2
        h = int(math.ceil((v1 - v0 + 2 * mg) * PPU))
        path = os.path.join(out, nome, "Static.png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        a = render_ler([objeto(p, Matrix(), tom_cenario(right, up))], right, up, (0.0, v0 - mg + h / PPU / 2), PPU, w, h,
                       path + ".raw.png")
        gravar(a, path)
        meta(out, nome, w, PPU, (0.5, (mg - v0) * PPU / h), ["Static"],
             {"pivo": "pilar: centro da base; portao/porta: centro do vao na linha central da parede (x 9,3), no chao"})
        print("PORTAO %-22s %dx%d px" % (nome, w, h))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_FS", "Resources", "Sprites"))
    ap.add_argument("--check", default="", help="pasta das montagens de conferencia do balcao (4 e 8 vagas)")
    ap.add_argument("grupos", nargs="*", default=["itens", "balcao", "portao"])
    a = ap.parse_args(argv)
    bad = [g for g in a.grupos if g not in ("itens", "balcao", "portao")]
    if bad:
        print("ERRO grupo desconhecido: %s (existem: itens balcao portao)" % ", ".join(bad))
        sys.exit(1)
    out = os.path.abspath(a.out)
    cena()
    t0 = time.time()
    if "itens" in a.grupos:
        itens(out)
        em_pe(out)
    if "balcao" in a.grupos:
        balcao(out, os.path.abspath(a.check) if a.check else "")
    if "portao" in a.grupos:
        portoes(out)
    print("ARTE_V05_OK %d PNG em %.1f s" % (len(SAIDAS), time.time() - t0))


if __name__ == "__main__":
    main()
