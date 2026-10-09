"""ui_v05b_blender.py - Forge Street v0.5b (0 credito): icones dos cartoes de melhoria e props do exterior, modelados por
primitivas no Blender 5.2 headless com a MESMA pipeline de arte_v05_blender.py (Workbench, luz FLAT, contorno #1E1612).

Uso (da raiz do projeto Forge Street; docs/ASSETS.md secao v0.5b):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1
      --python client/tools/ui_v05b_blender.py -- [--out client/Assets/_FS/Resources/Sprites] [icones exterior]
Saida por peca: <out>/<nome>/meta.json + PNG, formato lido por SpriteSheet.cs.
  - melhoria_*: icone 3/4 (clipe `icone`), celula 128 px, contorno 3 px + sombra 4 px a 35% (= item_*): cartoes do menu,
    botao Melhorias e placas de obra. Mochila, botas, fole, fole duplo, martelo de ouro, lupa, vitrine, sino, cesto.
  - arvore, arbusto, poste, canteiro: escala real (ppu 160), camera de 60 graus, clipe `Static`, pivo no pe (= pilar).
Nenhum matiz de item nos props (ART_BIBLE s3): copa verde-oliva apagado, flores creme/amarelo.
"""
import argparse, math, os, sys, time
import bmesh
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arte_v05_blender as A
from arte_v05_blender import Peca, R, T, prisma, espada, martelo, minerio, lingote, vista_item, meta, cam_60, faixa, objeto, tom_cenario, render_ler, gravar
from props_blender import ball, box, cyl, lathe

A.TONS.update({
    "couro_cl": (0x4E2D18, 0x7A4A2A, 0xA86D45), "pano": (0x9E8458, 0xC9B994, 0xE6DCC0), "asa": (0xB8C2D0, 0xEEF1F8, 0xFFFFFF),
    "vidro": (0x8FB4CC, 0xC4E0F0, 0xF2FAFF), "palha": (0x9E7E44, 0xC9A464, 0xE6CC8A), "madeira_e": (0x3E2412, 0x5C3A1E, 0x8A5A32),
    # cenario (sombra = base, so o tampo clareia)
    "copa": (0x3F6B3A, 0x3F6B3A, 0x5E8C4C), "copa2": (0x365E33, 0x365E33, 0x527E44), "tronco": (0x6B4428, 0x6B4428, 0x8A5A32),
    "flor": (0xF2E6C8, 0xF2E6C8, 0xFFF8E8), "flor2": (0xFFD166, 0xFFD166, 0xFFE9A8), "terra": (0x5C4630, 0x5C4630, 0x6E5640),
    "luz": (0xFFD166, 0xFFE9A8, 0xFFF6D8),
})
PPU = A.PPU


class Sub:
    """Constroi uma peca existente (espada, martelo...) dentro de outra, com uma matriz extra na frente de tudo."""

    def __init__(self, p, M):
        self.p, self.M = p, M

    def add(self, t, fam, m=None, at=(0, 0, 0), **kw):
        self.p.add(t, fam, m=self.M @ (m if m is not None else T(*at)), **kw)


# ------------------------------------------------------------------ icones de melhoria (frente = -Y, como os itens)

def mochila(p):
    """Mochila de couro: corpo, aba com 2 correias e fivela de ouro, bolso, saco de dormir creme enrolado em cima."""
    p.add(box(0.34, 0.2, 0.4), "couro_cl", at=(0, 0, 0.2), bevel=0.05)
    p.add(box(0.36, 0.22, 0.17), "couro", at=(0, -0.01, 0.33), bevel=0.04)
    p.add(box(0.22, 0.07, 0.14), "couro", at=(0, -0.12, 0.11), bevel=0.03)
    for x in (-0.09, 0.09):
        p.add(box(0.04, 0.012, 0.2), "couro2", at=(x, -0.125, 0.3), bevel=0.004)
        p.add(box(0.06, 0.016, 0.05), "ouro", at=(x, -0.133, 0.22), bevel=0.006)
    p.add(box(0.06, 0.016, 0.045), "ouro", at=(0, -0.16, 0.13), bevel=0.006)
    p.add(cyl(0.075, 0.42, segs=14), "pano", m=T(0, 0.01, 0.47) @ R("Y", 90), bevel=0.01)
    for x in (-0.12, 0.12):
        p.add(cyl(0.08, 0.025, segs=14), "couro2", m=T(x, 0.01, 0.47) @ R("Y", 90), bevel=0.004)


def botas(p):
    """Bota de couro de perfil (bico para +X) com sola escura, dobra, fivela de ouro e asinha clara (= velocidade)."""
    p.add(box(0.14, 0.14, 0.32), "couro_cl", at=(-0.04, 0, 0.26), bevel=0.03)
    p.add(box(0.3, 0.14, 0.11), "couro_cl", at=(0.04, 0, 0.085), bevel=0.04)
    p.add(ball(0.08, 0.072, 0.065, u=10, v=6), "couro_cl", at=(0.17, 0, 0.09), bevel=0)
    p.add(box(0.36, 0.15, 0.035), "couro2", at=(0.05, 0, 0.018), bevel=0.01)
    p.add(box(0.07, 0.15, 0.06), "couro2", at=(-0.08, 0, 0.04), bevel=0.01)
    p.add(box(0.18, 0.17, 0.08), "couro", at=(-0.04, 0, 0.42), bevel=0.02)
    p.add(box(0.16, 0.16, 0.035), "couro2", at=(-0.04, 0, 0.2), bevel=0.006)
    p.add(box(0.04, 0.02, 0.05), "ouro", at=(0.02, -0.085, 0.2), bevel=0.006)
    for k, (dz, ln) in enumerate(((0.0, 0.17), (-0.06, 0.14), (-0.12, 0.1))):   # asa: 3 penas em leque para tras (-X)
        p.add(ball(ln, 0.012, 0.035, u=8, v=4), "asa", m=T(-0.16 - ln * 0.6, -0.08, 0.36 + dz) @ R("Y", -18 - 14 * k), bevel=0)


def _fole(p, M):
    """Fole de ferreiro deitado: 2 tabuas em gota, couro sanfonado entre elas, bico de latao (+X), cabos (-X)."""
    poly = [(-0.12 + 0.13 * math.cos(math.radians(a)), 0.13 * math.sin(math.radians(a))) for a in range(90, 271, 15)]
    poly += [(0.17, -0.04), (0.17, 0.04)]
    tab = lambda s: [(x * s, z * s) for x, z in poly]
    q = Sub(p, M @ R("X", 90))   # poligono em XZ extrudado em Y -> tabua no plano XY, espessura em Z
    q.add(prisma(tab(1.0), -0.03, 0.0), "madeira_i", bevel=0.006)
    for k, (y0, y1, s) in enumerate(((0.0, 0.035, 0.93), (0.035, 0.07, 0.86), (0.07, 0.105, 0.93))):
        q.add(prisma(tab(s), -y1, -y0), "couro" if k % 2 else "couro_cl", bevel=0.004)
    Mt = M @ T(0.17, 0, 0.105) @ R("Y", 9) @ T(-0.17, 0, 0)   # tabua de cima aberta 9 graus pelo bico
    Sub(p, Mt @ R("X", 90)).add(prisma(tab(1.0), -0.03, 0.0), "madeira_i", bevel=0.006)
    for a in range(0, 360, 60):
        Sub(p, Mt).add(ball(0.012, 0.012, 0.008, u=6, v=4), "ouro", at=(-0.12 + 0.08 * math.cos(math.radians(a)), 0.08 * math.sin(math.radians(a)), 0.03), bevel=0)
    Sub(p, M).add(cyl(0.032, 0.14, 0.012, segs=10), "ouro", m=T(0.23, 0, 0.06) @ R("Y", 90), bevel=0.004)
    Sub(p, M).add(cyl(0.018, 0.12, segs=8), "madeira_e", m=T(-0.3, 0, -0.015) @ R("Y", 90), bevel=0.004)
    Sub(p, Mt).add(cyl(0.018, 0.12, segs=8), "madeira_e", m=T(-0.3, 0, 0.045) @ R("Y", 90), bevel=0.004)


def fole(p):
    _fole(p, Matrix())


def fole_duplo(p):
    _fole(p, T(-0.06, 0.12, 0.1))
    _fole(p, T(0.06, -0.12, -0.04))


def martelo_ouro(p):
    """O martelo do item ferramenta com a cabeca de ouro e o pano de couro (Martelo veloz)."""
    martelo(p)
    p.fams = [{"aco": "ouro", "verde": "couro_cl", "verde2": "couro"}.get(f, f) for f in p.fams]


def lupa(p):
    """Lupa: aro de ouro, lente clara com brilho, cabo de madeira escura com anel de ouro (cabo para baixo-direita)."""
    m = R("X", -90)
    p.add(lathe([(0.12, -0.02), (0.15, -0.02), (0.15, 0.02), (0.12, 0.02)], segs=28, closed=True), "ouro", m=m, bevel=0)
    p.add(cyl(0.122, 0.016, segs=28), "vidro", m=R("X", 90), bevel=0)
    p.add(box(0.1, 0.004, 0.022), "brilho", m=T(-0.04, -0.012, 0.05) @ R("Y", 40), bevel=0)
    p.add(box(0.04, 0.004, 0.016), "brilho", m=T(0.02, -0.012, 0.075) @ R("Y", 40), bevel=0)
    H = T(0.106, 0, -0.106) @ R("Y", 135)   # cabo saindo do aro a 45 graus para baixo-direita
    p.add(cyl(0.028, 0.05, segs=10), "ouro", m=H @ T(0, 0, 0.065), bevel=0.004)
    p.add(cyl(0.026, 0.2, 0.032, segs=10), "madeira_e", m=H @ T(0, 0, 0.19), bevel=0.006)
    p.add(ball(0.034, 0.034, 0.02, u=8, v=5), "ouro", m=H @ T(0, 0, 0.29), bevel=0)


def vitrine(p):
    """Vitrine: suporte de madeira com 3 espadas em pe e placa de ouro (Vitrine = mais estoque e mais clientes)."""
    p.add(box(0.5, 0.18, 0.07), "madeira_e", at=(0, 0, 0.035), bevel=0.012)
    p.add(box(0.5, 0.04, 0.5), "madeira_i", at=(0, 0.07, 0.3), bevel=0.01)
    p.add(box(0.52, 0.06, 0.05), "madeira_e", at=(0, 0.06, 0.57), bevel=0.01)
    p.add(box(0.18, 0.02, 0.07), "ouro", at=(0, 0.03, 0.57), bevel=0.006)
    for x in (-0.16, 0.0, 0.16):
        espada(Sub(p, T(x, -0.02, 0.07) @ Matrix.Scale(0.78, 4)))


def sino(p):
    """Sino de balcao: cupula de ouro sobre base de madeira, botao no topo (Balcao +1 vaga = mais gente atendida)."""
    p.add(cyl(0.17, 0.05, segs=24), "madeira_e", at=(0, 0, 0.025), bevel=0.012)
    p.add(lathe([(0.15, 0.05), (0.15, 0.065), (0.13, 0.1), (0.09, 0.17), (0.04, 0.2), (0.001, 0.205)], segs=24), "ouro", bevel=0)
    p.add(cyl(0.012, 0.05, segs=8), "aco", at=(0, 0, 0.225), bevel=0)
    p.add(ball(0.03, 0.03, 0.02, u=8, v=5), "aco", at=(0, 0, 0.255), bevel=0)
    p.add(box(0.05, 0.004, 0.014), "brilho", m=T(-0.06, -0.11, 0.13) @ R("Y", 50), bevel=0)


def cesto(p):
    """Cesto de vime com minerio e um lingote por cima (Ajudantes ageis: carregam mais)."""
    p.add(lathe([(0.001, 0.0), (0.13, 0.0), (0.16, 0.08), (0.18, 0.2), (0.155, 0.2), (0.14, 0.09), (0.001, 0.03)], segs=20), "palha", bevel=0)
    for z in (0.05, 0.12, 0.19):
        r = 0.13 + z * 0.27
        p.add(lathe([(r, z - 0.008), (r + 0.015, z - 0.008), (r + 0.015, z + 0.008), (r, z + 0.008)], segs=20, closed=True), "madeira_i", bevel=0)
    for x, y, s in ((-0.06, 0.02, 0.6), (0.06, -0.01, 0.55), (0.0, 0.06, 0.5)):
        minerio(Sub(p, T(x, y, 0.2) @ Matrix.Scale(s, 4)))
    lingote(Sub(p, T(0.0, -0.05, 0.25) @ R("Z", 15) @ Matrix.Scale(0.7, 4)))


ICONES = {   # nome -> (construtor, pose do icone)
    "melhoria_mochila": (mochila, R("X", 14) @ R("Z", 24)),
    "melhoria_botas": (botas, R("X", 10) @ R("Z", 12)),
    "melhoria_fole": (fole, R("X", 55) @ R("Z", -28)),
    "melhoria_fole_duplo": (fole_duplo, R("X", 55) @ R("Z", -28)),
    "melhoria_martelo": (martelo_ouro, R("Y", -40) @ R("Z", -20)),
    "melhoria_lupa": (lupa, R("X", 10) @ R("Z", 14)),
    "melhoria_vitrine": (vitrine, R("X", 16) @ R("Z", 20)),
    "melhoria_sino": (sino, R("X", 22) @ R("Z", 18)),
    "melhoria_cesto": (cesto, R("X", 28) @ R("Z", 22)),
}


def icones(out):
    for nome, (build, pose) in ICONES.items():
        p = Peca()
        build(p)
        vista_item(p, pose, 0.0, os.path.join(out, nome, "icone.png"), (110, 106))
        meta(out, nome, A.ITEM, A.ITEM, (0.5, 0.5), ["icone"])
        print("ICONE %-22s" % nome)


# ------------------------------------------------------------------ exterior (escala real, camera de 60, pivo no pe)

def arvore(p):
    """Arvore baixa de 2,3 m: tronco, copa de 4 bolhas facetadas verde-oliva (nao o verde do item)."""
    p.add(cyl(0.13, 0.9, 0.09, segs=7), "tronco", at=(0, 0, 0.45), bevel=0.02)
    for x, y, z, r, f in ((0.0, 0.0, 1.45, 0.62, "copa"), (-0.38, 0.1, 1.15, 0.42, "copa2"), (0.4, 0.05, 1.2, 0.44, "copa2"),
                          (0.05, -0.05, 1.95, 0.4, "copa")):
        t = bmesh.new()
        bmesh.ops.create_icosphere(t, subdivisions=1, radius=r)
        p.add(t, f, at=(x, y, z), bevel=0)


def arbusto(p):
    for x, y, r in ((-0.22, 0.0, 0.3), (0.2, 0.05, 0.28), (0.0, -0.08, 0.34)):
        t = bmesh.new()
        bmesh.ops.create_icosphere(t, subdivisions=1, radius=r)
        bmesh.ops.scale(t, vec=(1.0, 1.0, 0.8), verts=t.verts)
        p.add(t, "copa2" if x else "copa", at=(x, y, r * 0.7), bevel=0)
    for x, y, z in ((-0.2, -0.22, 0.42), (0.15, -0.25, 0.36), (0.02, -0.3, 0.5)):
        p.add(ball(0.05, 0.05, 0.05, u=6, v=4), "flor2", at=(x, y, z), bevel=0)


def poste(p):
    """Poste de ferro de 2,2 m com lanterna de vidro amarelo (a View poe o brilho quente em cima)."""
    p.add(cyl(0.16, 0.12, 0.12, segs=8), "pedra", at=(0, 0, 0.06), bevel=0.02)
    p.add(cyl(0.045, 1.9, 0.035, segs=8), "ferro", at=(0, 0, 1.05), bevel=0.01)
    p.add(box(0.2, 0.2, 0.05), "ferro", at=(0, 0, 2.0), bevel=0.01)
    p.add(box(0.15, 0.15, 0.22), "luz", at=(0, 0, 2.13), bevel=0.01)
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.add(box(0.025, 0.025, 0.24), "ferro", at=(sx * 0.08, sy * 0.08, 2.13), bevel=0)
    p.add(cyl(0.14, 0.1, 0.02, segs=4), "ferro", m=T(0, 0, 2.29) @ R("Z", 45), bevel=0.01)


def canteiro(p):
    """Canteiro de madeira 1,2 x 0,45 m com terra e flores creme/amarelas."""
    p.add(box(1.2, 0.45, 0.3), "madeira", at=(0, 0, 0.15), bevel=0.02)
    p.add(box(1.1, 0.36, 0.04), "terra", at=(0, 0, 0.29), bevel=0)
    for k in range(7):
        x = -0.48 + k * 0.16
        t = bmesh.new()
        bmesh.ops.create_icosphere(t, subdivisions=1, radius=0.1)
        p.add(t, "copa2", at=(x, 0.04 * (k % 2), 0.36), bevel=0)
        p.add(ball(0.045, 0.045, 0.035, u=6, v=4), "flor" if k % 2 else "flor2", at=(x + 0.02, -0.06, 0.44), bevel=0)


EXTERIOR = {"arvore": arvore, "arbusto": arbusto, "poste": poste, "canteiro": canteiro}


def exterior(out):
    """= arte_v05_blender.portoes: camera de 60, ppu 160, pivo no centro da base, sem contorno (como os props)."""
    right, up = cam_60()
    for nome, build in EXTERIOR.items():
        p = Peca()
        build(p)
        u0, u1, v0, v1 = faixa(p.verts(Matrix()), right, up)
        mg = 0.04
        w = int(math.ceil((max(-u0, u1) + mg) * 2 * PPU))
        w += w % 2
        h = int(math.ceil((v1 - v0 + 2 * mg) * PPU))
        path = os.path.join(out, nome, "Static.png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        a = render_ler([objeto(p, Matrix(), tom_cenario(right, up))], right, up, (0.0, v0 - mg + h / PPU / 2), PPU, w, h, path + ".raw.png")
        gravar(a, path)
        meta(out, nome, w, PPU, (0.5, (mg - v0) * PPU / h), ["Static"], {"pivo": "centro da base, no chao"})
        print("EXTERIOR %-12s %dx%d px" % (nome, w, h))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_FS", "Resources", "Sprites"))
    ap.add_argument("grupos", nargs="*", default=["icones", "exterior"])
    a = ap.parse_args(argv)
    out = os.path.abspath(a.out)
    A.cena()
    t0 = time.time()
    if "icones" in a.grupos:
        icones(out)
    if "exterior" in a.grupos:
        exterior(out)
    print("UI_V05B_OK %d PNG em %.1f s" % (len(A.SAIDAS), time.time() - t0))


if __name__ == "__main__":
    main()
