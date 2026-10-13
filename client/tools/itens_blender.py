"""itens_blender.py - Forge Street, Leva 9 (Wave F antecipada, 0 credito): icones dos produtos dos distritos 2-6 e dos
intermediarios novos (GDD s7), modelados por primitivas no Blender 5.2 headless com a MESMA pipeline dos item_* atuais
(arte_v05_blender.vista_item: Workbench FLAT, contorno #1E1612 de 3 px, sombra de 4 px a 35%, render a 512 px reduzido 4x).

Uso (da raiz do projeto Forge Street; docs/ASSETS.md s10):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1
      --python client/tools/itens_blender.py -- [--out client/Assets/_FS/Resources/Sprites] [item_elmo item_lanca ...]
Saida por item: <out>/<id>/meta.json + icone.png (3/4: balao, cartao, HUD) + deitado.png (camera de 60: pilha, estoque),
celula 128 px, ppu 128, pivo no centro (= item_espada). Os .meta do Unity saem do item_joia com GUID novo, so quando faltam
(rodar de novo nunca troca um GUID). Modelos em metros, base na origem, frente -Y. Matiz por familia: ART_BIBLE s3.1.
"""
import argparse, math, os, re, sys, time, uuid
import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arte_v05_blender as A
import ui_v05b_blender as U   # Sub e os tons couro_cl, pano, madeira_e, veludo, rubi, safira
from arte_v05_blender import Peca, R, T, basis, prisma, lamina, ANVIL, _h, espada, escudo
from props_blender import ball, box, cyl, lathe
Sub = U.Sub

# familia -> (sombra, base, luz). Matizes novos documentados na ART_BIBLE s3.1
A.TONS.update({
    "aco_esc": (0x3C434F, 0x5F6878, 0x8C96A6), "fenda": (0x15100C, 0x1E1612, 0x2E2420),
    "pele": (0x7A4A2A, 0xA86D45, 0xC99068), "pele2": (0x93613C, 0xBF8A62, 0xDDAA80),
    "cobre": (0x8E4E24, 0xC8743A, 0xF0A26A), "liga": (0x8A5A2A, 0xC98A4A, 0xF2C27E),
    "bronze": (0x6E4A22, 0xA8743A, 0xD9A55E), "marmore": (0x8E8A84, 0xC9C4BA, 0xEDE8DE),
    "vela": (0xC9B994, 0xF2E6C8, 0xFFF8E8), "caixa": (0x3E1F66, 0x5B2E8C, 0x8455C0),
    "real": (0x1F4E8C, 0x2F64B8, 0x6E9CE6), "real_esc": (0x16305A, 0x1F4E8C, 0x2F64B8),
    "arcano": (0x2E1E5A, 0x4B2E83, 0x7454B8), "ciano": (0x1FA8A0, 0x3FE0D0, 0xB8FFF6),
    "cristal": (0x7FD8E8, 0xC4F4FF, 0xFFFFFF), "rocha_r": (0x2A2438, 0x453C5C, 0x635A80),
    "noite": (0x10142E, 0x1B2350, 0x34408A), "estrela": (0xFFE08A, 0xFFF6D8, 0xFFFFFF), "prata": (0x6E7888, 0xA4AEBE, 0xDDE3EC),
})

DEITA_X = basis((0, -1, 0), (0, 0, -1), (1, 0, 0))   # eixo Z local -> +X, frente (-Y) para cima (= item_espada deitado)
FACE_UP = R("X", -90)                                  # peca plana de frente para cima, topo para o norte (= item_escudo)
EM_PE = Matrix()
SPRITES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_FS", "Resources", "Sprites")
GEMA = ((0.001, -0.05), (0.064, 0.0), (0.064, 0.012), (0.04, 0.042), (0.001, 0.042))   # lapidacao do item_joia (mesa em +Z)


# ------------------------------------------------------------------ ajudantes

def mira(n):
    """Rotacao que leva +Z para a direcao n."""
    return Vector((0, 0, 1)).rotation_difference(Vector(n).normalized()).to_matrix().to_4x4()


def tubo(p, fam, pts, r, segs=8):
    """Cilindros de raio r ligando os pontos (arco, corrente, haste curva), com bolinhas nas juntas."""
    pts = [Vector(q) for q in pts]
    for a, b in zip(pts, pts[1:]):
        p.add(cyl(r, (b - a).length, segs=segs), fam, m=T(*((a + b) / 2)) @ mira(b - a), bevel=0)
    for q in pts[1:-1]:
        p.add(ball(r, r, r, u=segs, v=4), fam, at=q, bevel=0)


def arco(c, r, a0, a1, n, rz=None):
    """n+1 pontos de um arco de centro c no plano XZ (graus: 0 = +X, 90 = +Z); rz = raio vertical (elipse)."""
    rz = r if rz is None else rz
    return [Vector(c) + Vector((r * math.cos(math.radians(a)), 0, rz * math.sin(math.radians(a))))
            for a in (a0 + (a1 - a0) * k / n for k in range(n + 1))]


def gema(p, fam, at, s, n=(0, -1, 0)):
    """Gema lapidada (a do item_joia, escala s) com a mesa virada para n (padrao: para a camera)."""
    p.add(lathe([(x * s, z * s) for x, z in GEMA], segs=8), fam, m=T(*at) @ mira(n), bevel=0)


def anel_xz(p, fam, at, r0, r1, esp, segs=20):
    """Aro chato (arruela) no plano XZ, de frente para a camera."""
    p.add(lathe([(r0, -esp / 2), (r1, -esp / 2), (r1, esp / 2), (r0, esp / 2)], segs=segs, closed=True), fam,
          m=T(*at) @ R("X", -90), bevel=0)


def estrela(pontas, r_ext, r_int, z0=0.0, gira=90.0):
    """Poligono [(x, z)] de uma estrela de `pontas` pontas (1a ponta para cima)."""
    return [((r_ext if k % 2 == 0 else r_int) * math.cos(math.radians(gira + 180.0 * k / pontas)),
             z0 + (r_ext if k % 2 == 0 else r_int) * math.sin(math.radians(gira + 180.0 * k / pontas))) for k in range(2 * pontas)]


def tinge(p, mapa):
    """Troca a familia de cor das faces ja adicionadas (como ui_v05b.martelo_ouro)."""
    p.fams = [mapa.get(f, f) for f in p.fams]


def simetrico(meia):
    """Meio contorno [(x, z)] do lado +X, de cima para baixo, espelhado no outro lado."""
    return list(meia) + [(-x, z) for x, z in reversed(meia[1:-1] if meia[-1][0] == 0 else meia[1:])]


# ------------------------------------------------------------------ tier 1 e 2: armas (azul-aco) e armaduras (aco + couro)

def espada_longa(p):
    """Espada longa (Blueprint): lamina ESTREITA de 0,8 m, punho de 2 maos, guarda de ACO larga com pontas viradas e pomo
    de disco de aco (a espada comum: lamina curta e larga, guarda e pomo de ouro)."""
    p.add(cyl(0.05, 0.034, segs=14), "aco", m=T(0, 0, 0.04) @ R("X", 90), bevel=0.006)
    p.add(cyl(0.024, 0.26, 0.027, segs=10), "couro", at=(0, 0, 0.18), bevel=0.004)
    for z in (0.1, 0.18, 0.26):
        p.add(cyl(0.03, 0.014, segs=10), "couro2", at=(0, 0, z), bevel=0.003)
    p.add(box(0.2, 0.042, 0.036), "aco", at=(0, 0, 0.325), bevel=0.008)
    for sx in (-1, 1):
        p.add(box(0.034, 0.042, 0.075), "aco", m=T(sx * 0.115, 0, 0.35) @ R("Y", -sx * 28), bevel=0.008)
    bm, fams = lamina(0.06, 0.05, 0.343, 1.05, 1.16, 0.018, 0.013)
    p.add(bm, "lamina", bevel=0, facefam=fams)
    for sy in (-1, 1):
        p.add(box(0.011, 0.003, 0.55), "sulco", at=(0, sy * 0.0092, 0.66), bevel=0)


def elmo(p):
    """Elmo fechado: cupula de aco com crista de cobre em meia-lua, fenda dos olhos escura em volta e respiro (T), faixa de couro com rebites."""
    p.add(lathe([(0.001, 0.0), (0.13, 0.0), (0.142, 0.06), (0.142, 0.17), (0.127, 0.245), (0.092, 0.296), (0.046, 0.319),
                 (0.001, 0.323)], segs=22), "aco", bevel=0)
    p.add(lathe([(0.128, 0.172), (0.146, 0.172), (0.146, 0.204), (0.128, 0.204)], segs=22, closed=True), "fenda", bevel=0)
    p.add(box(0.034, 0.03, 0.1), "fenda", at=(0, -0.136, 0.115), bevel=0.006)
    crista = [(0.155 * math.cos(math.radians(t)), 0.17 + 0.21 * math.sin(math.radians(t))) for t in range(20, 161, 10)]
    crista += [(0.105 * math.cos(math.radians(t)), 0.17 + 0.12 * math.sin(math.radians(t))) for t in range(160, 19, -10)]
    t = prisma(crista, -0.016, 0.016)
    bmesh.ops.transform(t, matrix=R("Z", 90), verts=t.verts)   # crista no plano YZ (de frente para tras)
    p.add(t, "cobre", bevel=0.005)
    p.add(lathe([(0.134, 0.0), (0.154, 0.0), (0.154, 0.048), (0.138, 0.048)], segs=22, closed=True), "couro_cl", bevel=0)
    for k in range(8):
        a = 2 * math.pi * (k + 0.5) / 8
        p.add(ball(0.013, 0.013, 0.013, u=6, v=4), "cobre", at=(0.156 * math.cos(a), 0.156 * math.sin(a), 0.024), bevel=0)


PEITO = [(0.0, 0.33), (0.06, 0.35), (0.09, 0.4), (0.165, 0.4), (0.19, 0.355), (0.152, 0.29), (0.152, 0.15), (0.135, 0.0)]


def peitoral(p):
    """Peitoral (cuirass): placa de aco em forma de tronco com quina no meio, gola enrolada, 2 lamas embaixo, correias
    de couro nos ombros e rebites de cobre. Sem ombreiras (a armadura de gala tem)."""
    p.add(prisma(simetrico(PEITO), -0.05, 0.05), "aco", bevel=0.024)
    p.add(box(0.026, 0.026, 0.29), "aco", m=T(0, -0.05, 0.17) @ R("Z", 45), bevel=0)
    tubo(p, "aco", [(-0.09, -0.05, 0.4), (-0.06, -0.05, 0.35), (0.0, -0.05, 0.33), (0.06, -0.05, 0.35), (0.09, -0.05, 0.4)], 0.014)
    for k, (z, w) in enumerate(((-0.035, 0.29), (-0.085, 0.27))):
        p.add(box(w, 0.1, 0.05), "aco" if k == 0 else "aco_esc", at=(0, 0.005 * k, z), bevel=0.012)
        for sx in (-1, 1):
            p.add(ball(0.013, 0.013, 0.013, u=6, v=4), "cobre", at=(sx * (w / 2 - 0.025), -0.052, z), bevel=0)
    for sx in (-1, 1):
        p.add(box(0.07, 0.13, 0.024), "couro_cl", at=(sx * 0.128, 0, 0.405), bevel=0.006)
        p.add(ball(0.014, 0.014, 0.014, u=6, v=4), "cobre", at=(sx * 0.128, -0.055, 0.37), bevel=0)


def greva(p, M):
    q = Sub(p, M)
    q.add(lathe([(0.001, 0.0), (0.072, 0.0), (0.074, 0.02), (0.048, 0.05), (0.05, 0.1), (0.07, 0.19), (0.068, 0.25), (0.058, 0.285),
                 (0.001, 0.29)], segs=14), "aco", bevel=0)
    q.add(box(0.022, 0.022, 0.2), "aco", m=T(0, -0.055, 0.16) @ R("Z", 45), bevel=0)
    q.add(ball(0.082, 0.07, 0.07, u=12, v=6), "aco", at=(0, -0.012, 0.32), bevel=0)
    for sx in (-1, 1):   # abas do joelho em leque
        q.add(ball(0.06, 0.02, 0.05, u=10, v=5), "aco_esc", m=T(sx * 0.075, -0.004, 0.31) @ R("Y", sx * 25), bevel=0)
    for z in (0.1, 0.22):
        q.add(lathe([(0.055, z - 0.014), (0.068, z - 0.014), (0.068, z + 0.014), (0.055, z + 0.014)], segs=14, closed=True), "couro_cl", bevel=0)
    for k, (y, w, h) in enumerate(((-0.06, 0.13, 0.06), (-0.11, 0.115, 0.05), (-0.155, 0.1, 0.042))):   # pe articulado (escarpe)
        q.add(box(w, 0.06, h), "aco" if k % 2 == 0 else "aco_esc", at=(0, y, h / 2), bevel=0.012)
    q.add(cyl(0.045, 0.07, 0.0, segs=8), "aco", m=T(0, -0.2, 0.021) @ R("X", 90), bevel=0)


def grevas(p):
    """Par de grevas: caneleiras de aco com joelheira em leque, quina na frente, 2 correias de couro e pe articulado
    pontudo; icone meio de lado, com os pes para a direita (a silhueta de pernas separa das manoplas, dedos para cima)."""
    greva(p, T(-0.085, 0.02, 0) @ R("Z", 8))
    greva(p, T(0.085, -0.01, 0) @ R("Z", -8))


def manopla(p, M, sx):
    q = Sub(p, M)
    q.add(cyl(0.072, 0.12, 0.05, segs=12), "aco", at=(0, 0, 0.06), bevel=0.006)
    q.add(lathe([(0.05, 0.11), (0.064, 0.11), (0.064, 0.132), (0.05, 0.132)], segs=12, closed=True), "cobre", bevel=0)
    q.add(box(0.11, 0.05, 0.1), "aco", at=(0, 0, 0.18), bevel=0.016)
    q.add(box(0.116, 0.058, 0.026), "aco_esc", at=(0, 0, 0.235), bevel=0.008)
    for k, x in enumerate((-0.039, -0.013, 0.013, 0.039)):
        h = 0.08 if k in (1, 2) else 0.066
        q.add(box(0.023, 0.044, h), "aco", at=(x, 0.004, 0.25 + h / 2), bevel=0.007)
        q.add(box(0.025, 0.046, 0.012), "aco_esc", at=(x, 0.004, 0.25 + h * 0.55), bevel=0.003)
    q.add(box(0.032, 0.044, 0.08), "aco", m=T(sx * 0.07, -0.004, 0.17) @ R("Y", sx * 38), bevel=0.008)


def manoplas(p):
    """Par de manoplas: punho de aco alargado com faixa de cobre, costas da mao, 4 dedos articulados e polegar."""
    manopla(p, T(-0.1, 0.02, 0) @ R("Y", 10), 1)
    manopla(p, T(0.1, -0.01, 0) @ R("Y", -10), -1)


def lanca(p):
    """Lanca: haste de madeira com empunhadura de couro e conto de aco, ponta em folha azul-aco larga."""
    p.add(cyl(0.026, 0.06, 0.02, segs=8), "aco", at=(0, 0, 0.03), bevel=0.004)
    p.add(cyl(0.027, 0.86, segs=8), "madeira_i", at=(0, 0, 0.48), bevel=0.004)
    p.add(cyl(0.032, 0.16, segs=8), "couro", at=(0, 0, 0.32), bevel=0.004)
    for z in (0.25, 0.39):
        p.add(cyl(0.036, 0.014, segs=8), "couro2", at=(0, 0, z), bevel=0.003)
    p.add(cyl(0.036, 0.1, 0.026, segs=8), "aco", at=(0, 0, 0.93), bevel=0.004)
    bm, fams = lamina(0.044, 0.16, 0.97, 1.09, 1.34, 0.028, 0.014)
    p.add(bm, "lamina", bevel=0, facefam=fams)


MACHADO = [(0.035, 0.07), (0.1, 0.1), (0.165, 0.175), (0.19, 0.09), (0.2, 0.0), (0.19, -0.09), (0.165, -0.175), (0.1, -0.1), (0.035, -0.07)]


def machado_guerra(p):
    """Machado de guerra de 2 gumes (labris): 2 laminas em meia-lua azul-aco com fio claro, olho de aco com espigao,
    cabo curto de madeira com empunhadura de couro (a alabarda: 1 lamina de ouro, espigao e bandeirola numa haste longa)."""
    p.add(cyl(0.03, 0.035, segs=8), "aco", at=(0, 0, 0.018), bevel=0.004)
    p.add(cyl(0.025, 0.62, segs=8), "madeira_i", at=(0, 0, 0.33), bevel=0.004)
    p.add(cyl(0.03, 0.18, segs=8), "couro", at=(0, 0, 0.13), bevel=0.004)
    for z in (0.06, 0.13, 0.2):
        p.add(cyl(0.034, 0.014, segs=8), "couro2", at=(0, 0, z), bevel=0.003)
    zc = 0.56
    p.add(box(0.075, 0.062, 0.15), "aco", at=(0, 0, zc), bevel=0.012)
    p.add(cyl(0.024, 0.1, 0.0, segs=8), "aco", at=(0, 0, zc + 0.125), bevel=0)
    for sx in (-1, 1):
        p.add(prisma([(sx * x, z) for x, z in MACHADO], -0.016, 0.016), "lamina", at=(0, 0, zc), bevel=0.006,
              pick=lambda f, sx=sx: "fio" if f.normal.x * sx > 0.75 else None)


def maca(p):
    """Maca de cravos: bola azul-aco com 13 cravos claros, colar de aco, cabo de madeira com empunhadura de couro."""
    p.add(cyl(0.03, 0.035, segs=8), "aco", at=(0, 0, 0.018), bevel=0.004)
    p.add(cyl(0.024, 0.46, segs=8), "madeira_i", at=(0, 0, 0.25), bevel=0.004)
    p.add(cyl(0.029, 0.16, segs=8), "couro", at=(0, 0, 0.12), bevel=0.004)
    p.add(cyl(0.04, 0.05, 0.03, segs=10), "aco", at=(0, 0, 0.45), bevel=0.006)
    zc = 0.55
    p.add(ball(0.09, 0.09, 0.09, u=14, v=8), "lamina", at=(0, 0, zc), bevel=0)
    for d in ((1, 0, 0.01), (-1, 0, 0.01), (0.01, 0, 1), (0.01, -1, 0), (0.01, 1, 0), (0.7, 0, 0.7), (-0.7, 0, 0.7), (0.7, 0, -0.7),
              (-0.7, 0, -0.7), (0.5, -0.7, 0.5), (-0.5, -0.7, 0.5), (0.5, -0.7, -0.5), (-0.5, -0.7, -0.5)):
        n = Vector(d).normalized()
        p.add(cyl(0.034, 0.08, 0.0, segs=6), "fio", m=T(*(Vector((0, 0, zc)) + n * 0.11)) @ mira(n), bevel=0)


# ------------------------------------------------------------------ tier 3: ourivesaria (roxo + ouro) e utensilios (cobre)

def colar(p):
    """Colar: corrente grossa de contas de ouro em U aberto em cima, 2 gemas pequenas e pingente com gema roxa grande."""
    pts = [Vector((0.16 * math.sin(math.radians(a)), 0, 0.2 - 0.2 * math.cos(math.radians(a)))) for a in range(-150, 151, 10)]
    for k, q in enumerate(pts):
        p.add(ball(0.022, 0.022, 0.022, u=8, v=5), "ouro_esc" if k % 3 == 1 else "ouro", at=q, bevel=0)
    for a in (-48, 48):
        c = (0.16 * math.sin(math.radians(a)), -0.02, 0.2 - 0.2 * math.cos(math.radians(a)))
        p.add(ball(0.034, 0.02, 0.034, u=10, v=5), "ouro", at=c, bevel=0)
        gema(p, "gema", (c[0], -0.03, c[2]), 0.42)
    p.add(ball(0.02, 0.02, 0.02, u=8, v=5), "ouro", at=(0, 0, -0.035), bevel=0)
    p.add(ball(0.068, 0.026, 0.082, u=12, v=6), "ouro", at=(0, 0.005, -0.11), bevel=0)
    gema(p, "gema", (0, -0.03, -0.11), 0.95)


def broche(p):
    """Broche: flor de 6 petalas de ouro, miolo escuro, gema roxa grande no centro e alfinete de aco atravessado atras
    (o contorno de petalas e o alfinete separam da moeda, que e' um disco liso)."""
    for k in range(6):
        a = 2 * math.pi * k / 6 + math.pi / 2
        p.add(ball(0.07, 0.022, 0.05, u=10, v=5), "ouro", m=T(0.09 * math.cos(a), 0, 0.09 * math.sin(a)) @ R("Y", -math.degrees(a)), bevel=0)
    p.add(cyl(0.07, 0.03, segs=18), "ouro_esc", m=T(0, -0.012, 0) @ R("X", 90), bevel=0.004)
    gema(p, "gema", (0, -0.03, 0), 0.9)
    p.add(cyl(0.009, 0.42, segs=6), "aco", m=T(0, 0.025, 0) @ R("Y", 62), bevel=0)
    p.add(ball(0.02, 0.02, 0.02, u=8, v=5), "aco", at=(0.185, 0.025, 0.1), bevel=0)


def cinzel(p):
    """Cinzel (ferramenta fina): cunha de aco com fio claro, haste quadrada, virola, cabo verde com 2 faixas, cabeca de bater."""
    t = box(0.07, 0.036, 0.09)
    for v in t.verts:
        if v.co.z < 0:
            v.co.y *= 0.12
            v.co.x *= 1.12
    p.add(t, "aco", at=(0, 0, 0.045), bevel=0.004)
    p.add(box(0.08, 0.008, 0.012), "fio", at=(0, 0, 0.004), bevel=0)
    p.add(box(0.034, 0.03, 0.12), "aco", at=(0, 0, 0.15), bevel=0.006)
    p.add(cyl(0.034, 0.024, segs=10), "aco_esc", at=(0, 0, 0.218), bevel=0.004)
    p.add(cyl(0.031, 0.2, 0.035, segs=10), "verde", at=(0, 0, 0.33), bevel=0.006)
    for z in (0.27, 0.39):
        p.add(cyl(0.038, 0.018, segs=10), "verde2", at=(0, 0, z), bevel=0.004)
    p.add(cyl(0.036, 0.02, 0.03, segs=10), "aco", at=(0, 0, 0.44), bevel=0.004)
    p.add(ball(0.036, 0.036, 0.018, u=10, v=5), "aco", at=(0, 0, 0.452), bevel=0)


def _prato(p, at, r=0.048):
    p.add(lathe([(0.001, 0.0), (r, 0.0), (r + 0.006, 0.02), (r * 0.6, 0.026), (0.001, 0.03)], segs=14), "cobre", at=at, bevel=0)


def _vela(p, x, z, h):
    p.add(cyl(0.025, h, segs=10), "vela", at=(x, 0, z + h / 2), bevel=0.004)
    p.add(ball(0.024, 0.022, 0.045, u=8, v=5), "veio", at=(x, -0.004, z + h + 0.04), bevel=0)
    p.add(ball(0.012, 0.012, 0.024, u=8, v=5), "nucleo", at=(x, -0.02, z + h + 0.03), bevel=0)


def castical(p):
    """Castical de 3 velas (candelabro): pe de cobre, coluna com no, braco em U, 3 pratinhos, velas creme e chamas."""
    p.add(lathe([(0.001, 0.0), (0.14, 0.0), (0.145, 0.016), (0.12, 0.032), (0.05, 0.05), (0.035, 0.07), (0.001, 0.075)], segs=20), "cobre", bevel=0)
    p.add(lathe([(0.03, 0.07), (0.022, 0.14), (0.042, 0.17), (0.022, 0.2), (0.025, 0.42), (0.001, 0.425)], segs=12), "cobre", bevel=0)
    tubo(p, "cobre", arco((0, 0, 0.37), 0.15, 180, 360, 10), 0.017)
    for x, z in ((-0.15, 0.37), (0.15, 0.37), (0.0, 0.42)):
        _prato(p, (x, 0, z))
        _vela(p, x, z + 0.025, 0.14 if x == 0 else 0.11)


def caixa_joias(p):
    """Caixa de joias: caixa roxo-escura com cantos e fecho de ouro, tampa aberta para tras com forro de veludo,
    2 gemas roxas e uma perola dentro."""
    p.add(box(0.34, 0.22, 0.12), "caixa", at=(0, 0, 0.06), bevel=0.014)
    for y in (-0.0975, 0.0975):
        p.add(box(0.34, 0.025, 0.05), "caixa", at=(0, y, 0.135), bevel=0.006)
    for x in (-0.1575, 0.1575):
        p.add(box(0.025, 0.22, 0.05), "caixa", at=(x, 0, 0.135), bevel=0.006)
    p.add(box(0.3, 0.18, 0.012), "veludo", at=(0, 0, 0.125), bevel=0)
    p.add(box(0.35, 0.23, 0.026), "ouro", at=(0, 0, 0.013), bevel=0.006)
    for x in (-0.162, 0.162):
        for y in (-0.102, 0.102):
            p.add(box(0.032, 0.032, 0.15), "ouro", at=(x, y, 0.08), bevel=0.006)
    p.add(box(0.05, 0.016, 0.045), "ouro", at=(0, -0.118, 0.12), bevel=0.006)
    M = T(0, 0.11, 0.16) @ R("X", -100) @ T(0, -0.11, 0.025)   # tampa aberta 100 graus na dobradica de tras
    p.add(box(0.34, 0.22, 0.05), "caixa", m=M, bevel=0.014)
    p.add(box(0.3, 0.18, 0.01), "veludo", m=M @ T(0, 0, -0.026), bevel=0)
    p.add(box(0.35, 0.03, 0.056), "ouro", m=M @ T(0, -0.1, 0), bevel=0.006)
    for x, s in ((-0.07, 0.62), (0.075, 0.48)):
        gema(p, "gema", (x, -0.01, 0.15), s, n=(0, -0.5, 1))
    p.add(ball(0.026, 0.026, 0.026, u=10, v=6), "vela", at=(0.01, 0.045, 0.15), bevel=0)


def calice(p):
    """Calice: taca de cobre com pe largo, no na haste, borda de ouro e 3 gemas roxas pequenas na copa."""
    p.add(lathe([(0.001, 0.0), (0.1, 0.0), (0.105, 0.016), (0.07, 0.03), (0.03, 0.06), (0.022, 0.12), (0.042, 0.14), (0.022, 0.16),
                 (0.026, 0.19), (0.07, 0.21), (0.11, 0.26), (0.125, 0.32), (0.13, 0.35), (0.116, 0.35), (0.11, 0.32), (0.001, 0.29)],
                segs=22), "cobre", bevel=0)
    p.add(lathe([(0.122, 0.33), (0.136, 0.33), (0.136, 0.358), (0.118, 0.358)], segs=22, closed=True), "ouro", bevel=0)
    p.add(lathe([(0.04, 0.13), (0.048, 0.13), (0.048, 0.15), (0.04, 0.15)], segs=12, closed=True), "ouro", bevel=0)
    for a in (-130, -90, -50):
        n = Vector((math.cos(math.radians(a)), math.sin(math.radians(a)), 0.25))
        gema(p, "gema", (0.112 * n.x, 0.112 * n.y, 0.27), 0.3, n=n)


def fivela(p):
    """Fivela: moldura retangular de cobre com pino de aco, correia de couro com 3 furos e ponta chanfrada."""
    p.add(prisma([(-0.24, 0.0), (-0.2, 0.05), (0.2, 0.05), (0.2, -0.05), (-0.2, -0.05)], 0.0, 0.018), "couro_cl", bevel=0.005)
    for x in (0.06, 0.11, 0.16):
        p.add(cyl(0.011, 0.006, segs=8), "fenda", m=T(x, -0.001, 0) @ R("X", 90), bevel=0)
    xc = -0.06
    for dz in (-0.085, 0.085):
        p.add(box(0.2, 0.034, 0.03), "cobre", at=(xc, -0.012, dz), bevel=0.008)
    for dx in (-0.088, 0.088):
        p.add(box(0.03, 0.034, 0.2), "cobre", at=(xc + dx, -0.012, 0), bevel=0.008)
    p.add(box(0.12, 0.014, 0.016), "aco", at=(xc + 0.02, -0.034, 0), bevel=0.003)
    p.add(cyl(0.014, 0.17, segs=8), "aco", at=(xc - 0.088, -0.03, 0), bevel=0)


# ------------------------------------------------------------------ tier 4: Royal Arsenal (azul-real + ouro)

HEATER = [(-0.16, 0.2), (0.16, 0.2), (0.16, 0.04), (0.135, -0.08), (0.075, -0.17), (0.0, -0.215), (-0.075, -0.17), (-0.135, -0.08), (-0.16, 0.04)]


def kit_guarda(p):
    """Kit da guarda: escudo heater azul-real com aro de ouro e bigorna de ouro (heraldica propria) + lanca atras na diagonal."""
    lanca(Sub(p, T(-0.24, 0.06, -0.3) @ R("Y", 38) @ Matrix.Scale(0.62, 4)))
    p.add(prisma([(x * 1.1, z * 1.1 + 0.003) for x, z in HEATER], 0.0, 0.036), "ouro", bevel=0.008)
    p.add(prisma(HEATER, -0.022, 0.012), "real", bevel=0.014)
    p.add(prisma([(x * 2.0, z * 2.0 + 0.02) for x, z in ANVIL], -0.034, -0.02), "ouro", bevel=0)
    for x in (-0.11, 0.0, 0.11):
        p.add(ball(0.014, 0.012, 0.014, u=6, v=4), "ouro", at=(x, -0.024, 0.165), bevel=0)


def kit_batedor(p):
    """Kit do batedor: arco curto de madeira com empunhadura de couro e corda + aljava azul-real com aros de ouro e 3 flechas."""
    pts = arco((-0.15, 0.05, 0.06), 0.34, -60, 60, 12)
    tubo(p, "madeira_i", pts, 0.018)
    p.add(cyl(0.025, 0.09, segs=8), "couro", at=pts[6], bevel=0.004)
    for q in (pts[0], pts[-1]):
        p.add(ball(0.022, 0.022, 0.022, u=8, v=5), "ouro", at=q, bevel=0)
    tubo(p, "pano", [pts[0], pts[-1]], 0.006, segs=6)
    q = Sub(p, T(-0.07, -0.04, -0.05) @ R("Y", -16))
    q.add(cyl(0.068, 0.32, 0.078, segs=12), "real", at=(0, 0, 0), bevel=0.008)
    for z in (-0.12, 0.13):
        q.add(lathe([(0.068, z - 0.014), (0.086, z - 0.014), (0.086, z + 0.014), (0.068, z + 0.014)], segs=12, closed=True), "ouro", bevel=0)
    for k, x in enumerate((-0.03, 0.0, 0.03)):
        q.add(cyl(0.009, 0.16, segs=6), "madeira_i", at=(x, 0.005 * k, 0.22), bevel=0)
        for s in (-1, 1):
            q.add(box(0.04, 0.006, 0.07), "vela", m=T(x + s * 0.012, 0.005 * k, 0.28) @ R("Y", s * 22), bevel=0)


ALABARDA = [(0.03, 0.09), (0.09, 0.12), (0.17, 0.16), (0.2, 0.06), (0.2, -0.04), (0.17, -0.11), (0.09, -0.06), (0.03, -0.04)]


def alabarda_cerimonial(p):
    """Alabarda cerimonial: haste azul-real com aneis de ouro, lamina de machado de OURO, espigao de aco, gancho atras e
    bandeirola azul-real grande com barra e bigorna de ouro pendurada do outro lado."""
    p.add(cyl(0.03, 0.06, 0.022, segs=8), "ouro", at=(0, 0, 0.03), bevel=0.004)
    p.add(cyl(0.026, 0.86, segs=8), "real_esc", at=(0, 0, 0.46), bevel=0.004)
    for z in (0.18, 0.38):
        p.add(cyl(0.032, 0.03, segs=8), "ouro", at=(0, 0, z), bevel=0.004)
    zc = 0.92
    p.add(cyl(0.036, 0.17, segs=8), "ouro", at=(0, 0, zc), bevel=0.006)
    p.add(prisma([(x * 1.3, z * 1.3) for x, z in ALABARDA], -0.016, 0.016), "ouro", at=(0, 0, zc), bevel=0.006)
    p.add(prisma([(-0.03, 0.06), (-0.16, -0.04), (-0.03, -0.01)], -0.014, 0.014), "ouro", at=(0, 0, zc), bevel=0.004)
    bm, fams = lamina(0.046, 0.06, zc + 0.08, zc + 0.13, zc + 0.36, 0.024, 0.014)
    p.add(bm, "aco", bevel=0, facefam=["prata" if f == "fio" else "aco" for f in fams])
    flag = [(0.0, 0.8), (-0.34, 0.8), (-0.26, 0.64), (-0.34, 0.48), (0.0, 0.48)]
    p.add(prisma(flag, -0.008, 0.008), "real", bevel=0.004)
    p.add(box(0.34, 0.022, 0.03), "ouro", at=(-0.17, -0.002, 0.8), bevel=0.004)
    p.add(prisma([(x * 1.4 - 0.13, z * 1.4 + 0.66) for x, z in ANVIL], -0.016, -0.008), "ouro", bevel=0)


def espada_gala(p):
    """Espada de gala deitada numa ALMOFADA azul-real com 4 borlas de ouro (a silhueta de almofada separa das outras 4
    espadas). A espada: lamina azul-aco com sulco de ouro, guarda com concha e arco de mao (D), punho azul, pomo com safira."""
    p.add(box(0.42, 0.42, 0.12), "real", at=(0, 0, 0.06), bevel=0.05)
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = Vector((sx * 0.2, sy * 0.2, 0.07))
            p.add(ball(0.026, 0.026, 0.026, u=8, v=5), "ouro", at=c, bevel=0)
            d = Vector((sx, sy, -1.0)).normalized()
            p.add(cyl(0.014, 0.08, 0.036, segs=8), "ouro", m=T(*(c + d * 0.06)) @ mira(d), bevel=0)
    espada_cerimonial(Sub(p, R("Z", 28) @ T(-0.255, 0, 0.15) @ DEITA_X @ Matrix.Scale(0.85, 4)))


def espada_cerimonial(p):
    """A espada da almofada (eixo +Z, pomo em z = 0, como a espada do item_espada)."""
    p.add(ball(0.044, 0.038, 0.044, u=10, v=6), "ouro", at=(0, 0, 0.04), bevel=0)
    gema(p, "safira", (0, -0.036, 0.04), 0.34)
    p.add(cyl(0.022, 0.13, 0.026, segs=10), "real_esc", at=(0, 0, 0.14), bevel=0.004)
    for z in (0.1, 0.14, 0.18):
        p.add(cyl(0.028, 0.01, segs=10), "ouro", at=(0, 0, z), bevel=0.002)
    p.add(box(0.16, 0.046, 0.04), "ouro", at=(0, 0, 0.225), bevel=0.008)
    p.add(cyl(0.045, 0.014, segs=16), "ouro_esc", m=T(0, -0.026, 0.225) @ R("X", 90), bevel=0.003)
    p.add(ball(0.024, 0.026, 0.026, u=8, v=5), "ouro", at=(-0.08, 0, 0.225), bevel=0)
    tubo(p, "ouro", [(0.075, 0, 0.225), (0.108, 0, 0.185), (0.114, 0, 0.13), (0.098, 0, 0.08), (0.045, 0, 0.045)], 0.012)
    bm, fams = lamina(0.07, 0.058, 0.245, 0.56, 0.68, 0.02, 0.015)
    p.add(bm, "lamina", bevel=0, facefam=fams)
    for sy in (-1, 1):
        p.add(box(0.012, 0.003, 0.22), "ouro", at=(0, sy * 0.0102, 0.37), bevel=0)
    tubo(p, "ouro", [(0, 0, 0.0), (-0.025, 0, -0.045)], 0.006, segs=6)
    p.add(ball(0.016, 0.016, 0.016, u=8, v=5), "ouro", at=(-0.027, 0, -0.05), bevel=0)
    p.add(cyl(0.013, 0.075, 0.032, segs=8), "real", m=T(-0.03, 0, -0.1) @ R("Y", 180), bevel=0)


def armadura_gala(p):
    """Armadura de gala: o peitoral em OURO com OMBREIRAS redondas, gorjal, faixa azul-real na diagonal e capa azul atras
    (o peitoral do distrito 2 e' de aco e sem ombreiras)."""
    peitoral(p)
    tinge(p, {"aco": "ouro", "aco_esc": "ouro_esc", "cobre": "ouro_esc", "couro_cl": "real_esc"})
    p.add(prisma([(-0.2, 0.4), (0.2, 0.4), (0.26, -0.13), (-0.26, -0.13)], 0.055, 0.075), "real", bevel=0.01)
    for sx in (-1, 1):
        p.add(ball(0.105, 0.088, 0.08, u=12, v=6), "ouro", at=(sx * 0.19, 0.0, 0.37), bevel=0)
        p.add(ball(0.1, 0.08, 0.035, u=12, v=6), "real", at=(sx * 0.2, 0.004, 0.305), bevel=0)
    p.add(ball(0.09, 0.06, 0.035, u=12, v=6), "ouro_esc", at=(0, -0.01, 0.37), bevel=0)
    p.add(box(0.06, 0.012, 0.42), "real", m=T(0, -0.062, 0.18) @ R("Y", 40), bevel=0.004)


def insignia_real(p):
    """Insignia real: estrela de 8 pontas de ouro com centro azul-real e bigorna de ouro, pendurada numa fita azul com
    2 listras de ouro (a fita em pentagono separa do broche)."""
    p.add(prisma([(-0.1, 0.44), (0.1, 0.44), (0.1, 0.29), (0.0, 0.22), (-0.1, 0.29)], -0.008, 0.008), "real", bevel=0.004)
    for x in (-0.045, 0.045):
        p.add(box(0.022, 0.008, 0.15), "ouro", at=(x, -0.007, 0.365), bevel=0)
    anel_xz(p, "ouro", (0, 0, 0.19), 0.016, 0.03, 0.014, segs=12)
    p.add(prisma(estrela(8, 0.165, 0.095), -0.016, 0.016), "ouro", bevel=0.006)
    p.add(cyl(0.078, 0.02, segs=20), "real", m=T(0, -0.02, 0) @ R("X", 90), bevel=0.004)
    anel_xz(p, "ouro", (0, -0.026, 0), 0.074, 0.088, 0.012, segs=20)
    p.add(prisma([(x * 1.3, z * 1.3 + 0.004) for x, z in ANVIL], -0.036, -0.028), "ouro", bevel=0)


# ------------------------------------------------------------------ tier 5: Arcane Foundry (ciano + roxo-noite)

def lamina_runica(p):
    """Lamina runica: lamina CURVA (sabre) ciano com 3 runas roxas, guarda em meia-lua roxa, punho roxo, pomo de cristal."""
    S = [k / 10 for k in range(10)]
    xc = lambda s: 0.1 * s * s
    w = lambda s: 0.08 * (1 - 0.25 * s)
    z = lambda s: 0.25 + 0.44 * s
    poly = [(xc(s) - w(s) / 2, z(s)) for s in S] + [(xc(1.0) + 0.005, z(1.0) + 0.02)] + [(xc(s) + w(s) / 2, z(s)) for s in reversed(S)]
    p.add(prisma(poly, -0.012, 0.012), "ciano", bevel=0.006)
    for s in (0.25, 0.5, 0.75):
        p.add(prisma([(-0.012, 0.0), (0.0, 0.02), (0.012, 0.0), (0.0, -0.02)], -0.016, -0.011), "arcano", at=(xc(s), 0, z(s)), bevel=0)
    tubo(p, "arcano", arco((0, 0, 0.3), 0.1, 205, 335, 8), 0.018)
    p.add(box(0.06, 0.044, 0.05), "arcano", at=(0, 0, 0.22), bevel=0.01)
    p.add(cyl(0.022, 0.13, 0.025, segs=10), "arcano", at=(0, 0, 0.13), bevel=0.004)
    for zz in (0.09, 0.15):
        p.add(cyl(0.028, 0.012, segs=10), "prata", at=(0, 0, zz), bevel=0.002)
    gema(p, "cristal", (0, 0, 0.06), 0.62, n=(0, 0, -1))


HEXA = [(0.0, 0.25), (0.175, 0.13), (0.175, -0.12), (0.0, -0.25), (-0.175, -0.12), (-0.175, 0.13)]


def escudo_runico(p):
    """Escudo runico: hexagono alto roxo-noite com aro de prata, anel runico ciano com 6 marcas e cristal no centro."""
    p.add(prisma([(x * 1.1, z * 1.1) for x, z in HEXA], 0.0, 0.036), "prata", bevel=0.008)
    p.add(prisma(HEXA, -0.024, 0.012), "arcano", bevel=0.016)
    anel_xz(p, "ciano", (0, -0.028, 0), 0.088, 0.11, 0.012, segs=24)
    for k in range(6):
        a = math.radians(60 * k + 30)
        p.add(box(0.016, 0.008, 0.04), "ciano", m=T(0.145 * math.cos(a), -0.028, 0.145 * math.sin(a)) @ R("Y", -math.degrees(a)), bevel=0)
    gema(p, "cristal", (0, -0.03, 0), 0.95)


def amuleto_arcano(p):
    """Amuleto arcano: losango roxo com moldura de prata, gema ciano grande, pingente de cristal e cordao em laco."""
    tubo(p, "couro2", arco((0, 0, 0.26), 0.07, -55, 235, 12), 0.012, segs=6)
    anel_xz(p, "prata", (0, 0, 0.18), 0.014, 0.028, 0.014, segs=12)
    p.add(prisma([(0.0, 0.18), (0.13, 0.0), (0.0, -0.2), (-0.13, 0.0)], 0.0, 0.03), "prata", bevel=0.008)
    p.add(prisma([(0.0, 0.15), (0.105, 0.0), (0.0, -0.165), (-0.105, 0.0)], -0.016, 0.0), "arcano", bevel=0.008)
    gema(p, "ciano", (0, -0.024, 0.0), 0.85)
    for x in (-0.085, 0.085):
        gema(p, "cristal", (x, -0.02, 0.0), 0.22)
    p.add(cyl(0.022, 0.06, 0.0, segs=6), "cristal", m=T(0, -0.005, -0.235) @ R("X", 180), bevel=0)


def cajado_liga(p):
    """Cajado de liga: haste de liga (bronze-ouro) com empunhadura roxa, garra em C segurando uma esfera ciano."""
    p.add(cyl(0.026, 0.07, 0.02, segs=8), "liga", at=(0, 0, 0.035), bevel=0.004)
    p.add(cyl(0.026, 0.8, segs=8), "liga", at=(0, 0, 0.43), bevel=0.004)
    p.add(cyl(0.032, 0.18, segs=8), "arcano", at=(0, 0, 0.42), bevel=0.004)
    for z in (0.33, 0.51):
        p.add(cyl(0.036, 0.014, segs=8), "prata", at=(0, 0, z), bevel=0.003)
    c = (0, 0, 0.96)
    pts = arco(c, 0.135, 130, 410, 14)
    tubo(p, "liga", pts, 0.024)
    for q in (pts[0], pts[-1]):
        p.add(cyl(0.024, 0.06, 0.0, segs=6), "liga", m=T(*q) @ mira(q - Vector(c)), bevel=0)
    p.add(ball(0.105, 0.105, 0.105, u=16, v=8), "ciano", at=c, bevel=0)
    p.add(box(0.05, 0.004, 0.018), "brilho", m=T(-0.04, -0.105, 1.0) @ R("Y", 40), bevel=0)


def anel_cristal(p):
    """Anel de cristal: aro de PRATA com um cristal ciano-claro ALTO e pontudo (a joia: aro de ouro e gema roxa larga)."""
    rr, r = 0.1, 0.024
    prof = [(rr + r * math.cos(2 * math.pi * k / 10), r * math.sin(2 * math.pi * k / 10)) for k in range(10)]
    p.add(lathe(prof, segs=28, closed=True), "prata", m=R("X", -90), bevel=0)
    zb = rr + r * 0.5
    p.add(lathe([(0.03, 0.0), (0.05, 0.022), (0.044, 0.04), (0.001, 0.04)], segs=12), "arcano", at=(0, 0, zb), bevel=0)
    p.add(cyl(0.04, 0.15, segs=6), "cristal", at=(0, 0, zb + 0.035 + 0.075), bevel=0)
    p.add(cyl(0.04, 0.07, 0.0, segs=6), "cristal", at=(0, 0, zb + 0.035 + 0.15 + 0.035), bevel=0)
    for k in range(4):
        a = 2 * math.pi * (k + 0.5) / 4
        p.add(box(0.014, 0.014, 0.05), "prata", m=T(0.045 * math.cos(a), 0.045 * math.sin(a), zb + 0.05) @ R("Z", math.degrees(a)), bevel=0)


def lanterna_arcana(p):
    """Lanterna arcana: base e telhado roxos, 4 colunas de prata, cristal ciano aceso dentro, argola de prata no topo."""
    p.add(box(0.19, 0.19, 0.035), "arcano", at=(0, 0, 0.018), bevel=0.008)
    p.add(box(0.17, 0.17, 0.025), "arcano", at=(0, 0, 0.27), bevel=0.006)
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.add(cyl(0.013, 0.24, segs=8), "prata", at=(sx * 0.075, sy * 0.075, 0.15), bevel=0)
    p.add(cyl(0.05, 0.13, segs=6), "ciano", at=(0, 0, 0.13), bevel=0)
    for s in (1, -1):
        p.add(cyl(0.05, 0.05, 0.0, segs=6), "cristal", m=T(0, 0, 0.13 + s * 0.09) @ R("X", 0 if s > 0 else 180), bevel=0)
    p.add(cyl(0.135, 0.09, 0.02, segs=4), "arcano", m=T(0, 0, 0.325) @ R("Z", 45), bevel=0.006)
    anel_xz(p, "prata", (0, 0, 0.405), 0.03, 0.048, 0.016, segs=14)


def liga_celeste(p):
    """Liga celeste: barra sextavada de cristal claro, anel de ouro inclinado em volta da cintura e 2 estrelinhas (a liga comum: 3 gomos)."""
    p.add(cyl(0.065, 0.34, segs=6), "cristal", m=T(0, 0, 0.065) @ R("Y", 90) @ R("Z", 30), bevel=0.008)
    for x in (-0.13, 0.13):
        p.add(cyl(0.07, 0.025, segs=6), "ciano", m=T(x, 0, 0.065) @ R("Y", 90) @ R("Z", 30), bevel=0.004)
    p.add(lathe([(0.125, -0.012), (0.145, -0.012), (0.145, 0.012), (0.125, 0.012)], segs=32, closed=True), "ouro",
          m=T(0, 0, 0.065) @ R("Z", 20) @ R("Y", 90) @ R("X", 25), bevel=0)
    for at, s in (((0.13, -0.06, 0.2), 1.0), ((-0.15, -0.04, 0.15), 0.7)):
        p.add(prisma(estrela(4, 0.04 * s, 0.012 * s), -0.004, 0.004), "estrela", at=at, bevel=0)


# ------------------------------------------------------------------ 5+: Grand Forge (ouro + bronze, sintese)

def panoplia_campeao(p):
    """Panoplia do campeao: 2 espadas cruzadas em X atras de um escudo redondo de ouro com campo vinho (o kit da guarda:
    escudo heater azul + 1 lanca)."""
    for sx in (-1, 1):
        espada(Sub(p, T(sx * 0.286, 0.05, -0.341) @ R("Y", -sx * 40) @ Matrix.Scale(1.35, 4)))
    escudo(Sub(p, T(0, -0.03, 0) @ Matrix.Scale(0.82, 4)))
    tinge(p, {"escudo": "veludo", "aco": "ouro_esc"})


def lamina_estelar(p):
    """Lamina estelar: lamina azul-noite com fio claro e 3 estrelinhas, guarda em ESTRELA de 4 pontas de ouro, pomo estrela."""
    p.add(prisma(estrela(4, 0.065, 0.022), -0.018, 0.018), "ouro", at=(0, 0, 0.04), bevel=0.004)
    p.add(cyl(0.022, 0.13, 0.025, segs=10), "noite", at=(0, 0, 0.14), bevel=0.004)
    for z in (0.1, 0.14, 0.18):
        p.add(cyl(0.028, 0.01, segs=10), "ouro", at=(0, 0, z), bevel=0.002)
    p.add(prisma([(x * 1.7 if abs(x) > 0.02 else x, z) for x, z in estrela(4, 0.1, 0.03)], -0.024, 0.024), "ouro", at=(0, 0, 0.235), bevel=0.006)
    bm, fams = lamina(0.11, 0.085, 0.27, 0.6, 0.74, 0.024, 0.02)
    p.add(bm, "noite", bevel=0, facefam=["estrela" if f == "fio" else "noite" for f in fams])
    for z, s in ((0.36, 1.0), (0.46, 0.8), (0.55, 0.65)):
        p.add(prisma(estrela(4, 0.022 * s, 0.007 * s), -0.004, 0.004), "estrela", at=(0, -0.012, z), bevel=0)


def coroa_reino(p):
    """Coroa do reino: aro de ouro com gemas, 2 ARCOS cruzados fechando uma cupula de veludo azul-real e globo com
    estrela no topo (a coroa do VIP: 5 pontas abertas e veludo vinho)."""
    p.add(lathe([(0.15, 0.0), (0.172, 0.0), (0.172, 0.085), (0.15, 0.085)], segs=30, closed=True), "ouro", bevel=0)
    p.add(ball(0.148, 0.148, 0.15, u=16, v=8), "real", at=(0, 0, 0.07), bevel=0)
    pts = arco((0, 0, 0.075), 0.158, 0, 180, 12, rz=0.17)
    tubo(p, "ouro", pts, 0.018)
    tubo(p, "ouro", [R("Z", 90) @ q for q in pts], 0.018)
    p.add(ball(0.045, 0.045, 0.045, u=12, v=6), "ouro", at=(0, 0, 0.29), bevel=0)
    p.add(prisma(estrela(4, 0.065, 0.02), -0.01, 0.01), "ouro", at=(0, 0, 0.37), bevel=0.003)
    for k in range(4):   # floroes entre os arcos
        a = math.pi / 4 + math.pi / 2 * k
        x, y = 0.162 * math.cos(a), 0.162 * math.sin(a)
        p.add(cyl(0.04, 0.075, 0.0, segs=4), "ouro", m=T(x, y, 0.12) @ R("Z", math.degrees(a) + 45), bevel=0)
        p.add(ball(0.018, 0.018, 0.018, u=8, v=5), "ouro", at=(x * 1.02, y * 1.02, 0.165), bevel=0)
    for k in range(8):
        a = 2 * math.pi * k / 8 - math.pi / 2
        p.add(ball(0.024, 0.012, 0.026, u=8, v=5), "safira" if k % 2 else "rubi",
              m=T(0.174 * math.cos(a), 0.174 * math.sin(a), 0.043) @ R("Z", math.degrees(a) + 90), bevel=0)


def estatua_bronze(p):
    """Estatua de bronze: ferreiro de bronze erguendo o martelo, sobre pedestal de marmore com degrau."""
    p.add(box(0.27, 0.2, 0.1), "marmore", at=(0, 0, 0.05), bevel=0.012)
    p.add(box(0.22, 0.165, 0.035), "marmore", at=(0, 0, 0.117), bevel=0.008)
    z0 = 0.135
    for sx in (-1, 1):
        p.add(cyl(0.032, 0.16, 0.028, segs=10), "bronze", at=(sx * 0.036, 0, z0 + 0.08), bevel=0.004)
        p.add(box(0.05, 0.075, 0.03), "bronze", at=(sx * 0.038, -0.012, z0 + 0.015), bevel=0.008)
    p.add(box(0.13, 0.085, 0.16), "bronze", at=(0, 0, z0 + 0.235), bevel=0.03)
    p.add(prisma([(-0.075, 0.0), (0.075, 0.0), (0.06, -0.12), (-0.06, -0.12)], -0.05, -0.04), "bronze", at=(0, 0, z0 + 0.28), bevel=0.004)
    p.add(ball(0.052, 0.05, 0.056, u=12, v=6), "bronze", at=(0, 0, z0 + 0.37), bevel=0)
    tubo(p, "bronze", [(-0.072, 0, z0 + 0.29), (-0.095, 0, z0 + 0.21), (-0.088, -0.01, z0 + 0.14)], 0.024)
    tubo(p, "bronze", [(0.07, 0, z0 + 0.29), (0.13, 0, z0 + 0.35), (0.15, -0.01, z0 + 0.45)], 0.024)
    mao, d = Vector((0.15, -0.01, z0 + 0.45)), Vector((-0.94, 0, 0.34))   # martelo erguido sobre a cabeca
    p.add(cyl(0.013, 0.14, segs=8), "bronze", m=T(*(mao + d * 0.06)) @ R("Y", -70), bevel=0)
    p.add(box(0.09, 0.055, 0.055), "bronze", m=T(*(mao + d * 0.13)) @ R("Y", -70), bevel=0.008)


def sino_catedral(p):
    """Sino da catedral: sino de bronze de boca larga com 2 faixas de ouro, badalo a mostra, pendurado numa viga de
    madeira com ferragens (o sino de balcao das melhorias: cupula baixa sobre base de madeira)."""
    p.add(lathe([(0.001, 0.0), (0.205, 0.0), (0.218, 0.024), (0.19, 0.05), (0.156, 0.11), (0.136, 0.2), (0.13, 0.27), (0.11, 0.31),
                 (0.06, 0.332), (0.001, 0.336)], segs=24), "bronze", bevel=0)
    p.add(lathe([(0.168, 0.06), (0.186, 0.06), (0.18, 0.088), (0.162, 0.088)], segs=24, closed=True), "ouro", bevel=0)
    p.add(lathe([(0.128, 0.24), (0.142, 0.24), (0.14, 0.262), (0.126, 0.262)], segs=24, closed=True), "ouro", bevel=0)
    p.add(cyl(0.01, 0.06, segs=6), "aco_esc", at=(0, 0, -0.01), bevel=0)
    p.add(ball(0.045, 0.045, 0.045, u=10, v=6), "aco_esc", at=(0, -0.02, -0.04), bevel=0)
    anel_xz(p, "bronze", (0, 0, 0.37), 0.022, 0.042, 0.02, segs=12)
    p.add(box(0.52, 0.085, 0.085), "madeira_e", at=(0, 0, 0.43), bevel=0.016)
    for x in (-0.06, 0.06):
        p.add(box(0.03, 0.095, 0.095), "aco_esc", at=(x, 0, 0.43), bevel=0.006)
    for sx in (-1, 1):
        p.add(cyl(0.022, 0.05, segs=8), "aco_esc", m=T(sx * 0.28, 0, 0.43) @ R("Y", 90), bevel=0)


def relicario_real(p):
    """Relicario real: casinha de ouro com telhado de 2 aguas azul-real, cumeeira de ouro com bolinhas, 2 paineis azuis
    na frente, rubi no meio e pezinhos de ouro."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.add(ball(0.025, 0.025, 0.025, u=8, v=5), "ouro", at=(sx * 0.13, sy * 0.075, 0.022), bevel=0)
    p.add(box(0.3, 0.18, 0.17), "ouro", at=(0, 0, 0.12), bevel=0.012)
    for x in (-0.07, 0.07):
        p.add(box(0.1, 0.012, 0.11), "real", at=(x, -0.092, 0.12), bevel=0.004)
    gema(p, "rubi", (0, -0.094, 0.125), 0.36)
    t = prisma([(-0.122, 0.0), (0.122, 0.0), (0.0, 0.13)], -0.175, 0.175)
    bmesh.ops.transform(t, matrix=R("Z", 90), verts=t.verts)   # perfil do telhado no plano YZ, cumeeira ao longo de X
    p.add(t, "real", at=(0, 0, 0.205), bevel=0.008)
    p.add(box(0.36, 0.035, 0.03), "ouro", at=(0, 0, 0.338), bevel=0.006)
    for x in (-0.12, -0.04, 0.04, 0.12):
        p.add(ball(0.018, 0.018, 0.02, u=8, v=5), "ouro", at=(x, 0, 0.365), bevel=0)
    for sx in (-1, 1):
        p.add(ball(0.03, 0.03, 0.034, u=10, v=6), "ouro", at=(sx * 0.185, 0, 0.35), bevel=0)


# ------------------------------------------------------------------ intermediarios novos

COURO = [(0.0, 0.2), (0.06, 0.18), (0.1, 0.23), (0.165, 0.2), (0.14, 0.12), (0.17, 0.04), (0.16, -0.06), (0.21, -0.15),
         (0.13, -0.155), (0.08, -0.2), (0.0, -0.18)]


def couro(p):
    """Couro: pele curtida aberta (contorno com 4 patas), couro-claro com miolo mais claro."""
    p.add(prisma(simetrico(COURO), -0.012, 0.012), "pele", bevel=0.008)
    p.add(prisma([(x * 0.7, z * 0.7 + 0.005) for x, z in simetrico(COURO)], -0.018, -0.008), "pele2", bevel=0.004)


def placa(p):
    """Placa: 2 chapas de aco grossas (uma atras, deslocada) com 4 furos de rebite."""
    for x, y, z, ry in ((0.03, 0.04, 0.035, 8), (0.0, 0.0, 0.0, -6)):
        q = Sub(p, T(x, y, z) @ R("Y", ry))
        q.add(box(0.3, 0.035, 0.24), "aco", bevel=0.014)
        for hx in (-0.115, 0.115):
            for hz in (-0.085, 0.085):
                q.add(cyl(0.017, 0.012, segs=10), "fenda", m=T(hx, -0.0145, hz) @ R("X", 90), bevel=0)


def _rebite(p, M):
    q = Sub(p, M)
    q.add(cyl(0.024, 0.1, segs=10), "aco_esc", at=(0, 0, 0.05), bevel=0.004)
    q.add(lathe([(0.001, 0.098), (0.055, 0.098), (0.053, 0.114), (0.038, 0.136), (0.001, 0.144)], segs=14), "aco", bevel=0)


def rebite(p):
    """Rebites: punhado de 3 rebites de ferro (cabeca abaulada clara, haste escura), 2 em pe e 1 deitado na frente."""
    _rebite(p, T(-0.07, 0.03, 0.0))
    _rebite(p, T(0.065, 0.07, 0.0) @ Matrix.Scale(0.9, 4))
    _rebite(p, T(0.07, -0.09, 0.055) @ R("Z", -15) @ R("Y", -90))


def gema_bruta(p):
    """Gema: pedra lapidada roxa grande (a do anel, sem aro), mesa e pavilhao facetados."""
    p.add(lathe([(x * 2.3, z * 2.3) for x, z in GEMA], segs=8), "gema", bevel=0)
    p.add(box(0.07, 0.03, 0.004), "brilho", m=T(-0.04, -0.04, 0.097) @ R("Z", 40), bevel=0)


def liga(p):
    """Liga: barra de 3 GOMOS de bronze com tampo de ouro (o lingote: barra lisa prata)."""
    for x in (-0.118, 0.0, 0.118):
        t = box(0.112, 0.15, 0.09)
        for v in t.verts:
            if v.co.z > 0:
                v.co.x *= 0.72
                v.co.y *= 0.7
        p.add(t, "liga", at=(x, 0, 0.045), bevel=0.012, pick=lambda f: "ouro" if f.normal.z > 0.9 else None)


def minerio_runico(p):
    """Minerio runico: rocha roxo-ardosia com veio ciano e 3 cristais ciano (o minerio: pedra baixa marrom
    com veio laranja)."""
    t = bmesh.new()
    bmesh.ops.create_icosphere(t, subdivisions=2, radius=1.0)
    for v in t.verts:
        k = 0.8 + 0.35 * _h(v.co)
        v.co = Vector((v.co.x * 0.155 * k, v.co.y * 0.13 * k, max(v.co.z * 0.18 * k, -0.08)))
    t.normal_update()
    nv = Vector((-0.6, -0.5, 0.6)).normalized()
    p.add(t, "rocha_r", bevel=0, pick=lambda f: "ciano" if abs(f.calc_center_median().dot(nv) + 0.01) < 0.01 else None)
    for d, ln, r in (((-0.55, -0.3, 0.78), 0.15, 0.04), ((0.12, -0.35, 0.93), 0.2, 0.048), ((0.62, -0.25, 0.74), 0.13, 0.036)):
        n = Vector(d).normalized()
        base = Vector((n.x * 0.1, n.y * 0.09, n.z * 0.15))
        p.add(cyl(r, ln, segs=6), "ciano", m=T(*(base + n * (ln * 0.3))) @ mira(n), bevel=0)
        p.add(cyl(r, r * 1.6, 0.0, segs=6), "cristal", m=T(*(base + n * (ln * 0.8 + r * 0.8))) @ mira(n), bevel=0)


def cristal(p):
    """Cristal: aglomerado de 5 prismas sextavados claros com ponta, numa lasca de rocha pequena."""
    p.add(ball(0.11, 0.085, 0.04, u=8, v=4), "rocha_r", at=(0, 0, 0.025), bevel=0)
    for d, ln, r in (((0.02, 0, 1), 0.24, 0.052), ((-0.55, 0.05, 0.83), 0.17, 0.04), ((0.58, -0.1, 0.8), 0.19, 0.042),
                     ((0.2, -0.62, 0.76), 0.12, 0.032), ((-0.25, 0.55, 0.8), 0.14, 0.034)):
        n = Vector(d).normalized()
        base = Vector((n.x * 0.03, n.y * 0.03, 0.04))
        p.add(cyl(r, ln, segs=6), "cristal", m=T(*(base + n * (ln / 2))) @ mira(n), bevel=0)
        p.add(cyl(r, r * 1.7, 0.0, segs=6), "cristal", m=T(*(base + n * (ln + r * 0.85))) @ mira(n), bevel=0)


# ------------------------------------------------------------------ catalogo: id -> (construtor, pose do icone, pose deitada)

ITENS = {
    # tier 1 (Blueprint)
    "item_espada_longa": (espada_longa, R("Y", -45) @ R("Z", 18), DEITA_X),
    # tier 2 - Armorer's Row
    "item_elmo": (elmo, R("X", 12) @ R("Z", 25), EM_PE),
    "item_peitoral": (peitoral, R("X", 8) @ R("Z", 20), FACE_UP),
    "item_grevas": (grevas, R("X", 10) @ R("Z", 50), EM_PE),
    "item_manoplas": (manoplas, R("X", 8) @ R("Z", 12), FACE_UP),
    "item_lanca": (lanca, R("Y", 45) @ R("Z", 18), DEITA_X),
    "item_machado_guerra": (machado_guerra, R("Y", 30) @ R("Z", 18), DEITA_X),
    "item_maca": (maca, R("Y", 35) @ R("Z", 18), DEITA_X),
    # tier 3 - Artisan Quarter
    "item_colar": (colar, R("X", 8) @ R("Z", 15), FACE_UP),
    "item_broche": (broche, R("X", 12) @ R("Z", 18), FACE_UP),
    "item_cinzel": (cinzel, R("Y", 40) @ R("Z", 20), DEITA_X),
    "item_castical": (castical, R("X", 10) @ R("Z", 15), EM_PE),
    "item_caixa_joias": (caixa_joias, R("X", 25) @ R("Z", 25), EM_PE),
    "item_calice": (calice, R("X", 15) @ R("Z", 10), EM_PE),
    "item_fivela": (fivela, R("Y", 20) @ R("X", 12) @ R("Z", 15), FACE_UP),
    # tier 4 - Royal Arsenal
    "item_kit_guarda": (kit_guarda, R("X", 8) @ R("Z", 18), FACE_UP),
    "item_kit_batedor": (kit_batedor, R("X", 8) @ R("Z", 15), FACE_UP),
    "item_alabarda_cerimonial": (alabarda_cerimonial, R("Y", -35) @ R("Z", 18), DEITA_X),
    "item_espada_gala": (espada_gala, R("X", 42) @ R("Z", 10), EM_PE),
    "item_armadura_gala": (armadura_gala, R("X", 8) @ R("Z", 18), FACE_UP),
    "item_insignia_real": (insignia_real, R("X", 8) @ R("Z", 15), FACE_UP),
    # tier 5 - Arcane Foundry
    "item_lamina_runica": (lamina_runica, R("Y", 40) @ R("Z", 18), DEITA_X),
    "item_escudo_runico": (escudo_runico, R("X", 12) @ R("Z", 22), FACE_UP),
    "item_amuleto_arcano": (amuleto_arcano, R("X", 10) @ R("Z", 18), FACE_UP),
    "item_cajado_liga": (cajado_liga, R("Y", -15) @ R("Z", 18), DEITA_X),
    "item_anel_cristal": (anel_cristal, R("X", 28) @ R("Z", 26), R("Z", 180) @ R("X", 60)),
    "item_lanterna_arcana": (lanterna_arcana, R("X", 12) @ R("Z", 25), EM_PE),
    "item_liga_celeste": (liga_celeste, R("X", 32) @ R("Z", -24), R("Z", -8)),
    # 5+ - Grand Forge
    "item_panoplia_campeao": (panoplia_campeao, R("X", 8) @ R("Z", 12), FACE_UP),
    "item_lamina_estelar": (lamina_estelar, R("Y", 12) @ R("Z", 15), DEITA_X),
    "item_coroa_reino": (coroa_reino, R("X", 18) @ R("Z", 8), EM_PE),
    "item_estatua_bronze": (estatua_bronze, R("X", 6) @ R("Z", 20), EM_PE),
    "item_sino_catedral": (sino_catedral, R("X", 10) @ R("Z", 15), EM_PE),
    "item_relicario_real": (relicario_real, R("X", 15) @ R("Z", 25), EM_PE),
    # intermediarios
    "item_couro": (couro, R("X", 20) @ R("Z", 15), FACE_UP),
    "item_placa": (placa, R("X", 15) @ R("Z", 25), FACE_UP),
    "item_rebite": (rebite, R("X", 15) @ R("Z", 15), EM_PE),
    "item_gema": (gema_bruta, R("X", 25) @ R("Z", 20), R("X", 40)),
    "item_liga": (liga, R("X", 32) @ R("Z", -24), R("Z", -8)),
    "item_minerio_runico": (minerio_runico, R("X", 15) @ R("Z", 18), EM_PE),
    "item_cristal": (cristal, R("X", 10) @ R("Z", 15), EM_PE),
}


def metas_unity(out, nome):
    """.meta do Unity copiados do item_joia (mesmo import: sem mipmap, ASTC 6x6 no Android) com GUID novo; nunca sobrescreve."""
    for rel, modelo in (("", "item_joia"), ("/icone.png", "item_joia/icone.png"), ("/deitado.png", "item_joia/deitado.png"),
                        ("/meta.json", "item_joia/meta.json")):
        dst = os.path.join(out, nome + rel + ".meta")
        if not os.path.exists(dst):
            with open(os.path.join(SPRITES, modelo + ".meta"), "rb") as f:
                txt = re.sub(rb"guid: [0-9a-f]{32}", b"guid: " + uuid.uuid4().hex.encode(), f.read())
            with open(dst, "wb") as f:
                f.write(txt)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=SPRITES)
    ap.add_argument("itens", nargs="*", default=list(ITENS))
    a = ap.parse_args(argv)
    bad = [n for n in a.itens if n not in ITENS]
    if bad:
        print("ERRO item desconhecido: %s" % ", ".join(bad))
        sys.exit(1)
    out = os.path.abspath(a.out)
    A.cena()
    t0 = time.time()
    for nome in a.itens:
        build, icon, flat = ITENS[nome]
        p = Peca()
        build(p)
        A.vista_item(p, icon, 0.0, os.path.join(out, nome, "icone.png"), (110, 106))
        A.vista_item(p, flat, 60.0, os.path.join(out, nome, "deitado.png"), (112, 100))
        A.meta(out, nome, A.ITEM, A.ITEM, (0.5, 0.5), ["icone", "deitado"], {"fonte": "tools/itens_blender.py (procedural, 0 credito)"})
        metas_unity(out, nome)
        print("ITEM %-26s icone + deitado" % nome)
    print("ITENS_OK %d itens, %d PNG em %.1f s" % (len(a.itens), len(A.SAIDAS), time.time() - t0))


if __name__ == "__main__":
    main()
