"""folha_mochila.py - antes x depois da mochila do ferreiro (Python 3 + Pillow; so QA, o jogo nao depende dela).

Antes = folha `ferreiro` + icone `melhoria_mochila` solto nas costas, como a WorldView fazia (0,38 m, 0,33 m acima do pe,
0,14 m para o lado das costas em W/E, atras do corpo em S). Depois = folha `ferreiro_mochila` (mochila presa ao Spine2 no render).
A: de perto (2x, sem filtro), parado (Idle q0) e andando (Walking q4), S/W/N/E. B: tamanho de jogo, 540 px de largura = 9 m
visiveis (60 px/m; Enquadramento), e a mesma faixa 2x sem filtro.
Uso (da raiz do projeto): python client/tools/folha_mochila.py [--sprites DIR] [--out client/Builds/sprites_contact/mochila_antes_depois.png]
"""
import argparse, json, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from folhas_v05 import RES, ROOT, texto

GAME_PPU = 109.87 * 0.76   # px da folha por m do mundo no jogo (meta.ppu x WorldView.CharScale("ferreiro"))
PACK_S, PACK_Y, PACK_DX = 0.38, 0.33, 0.14   # WorldView (prop antigo)
DIRS = "SWNE"
CHAO, FUNDO = (110, 96, 82, 255), (30, 34, 46, 255)   # oficina com a luz quente (~#6E6052) / fundo da folha


def folha(sp, nome):
    d = os.path.join(sp, nome)
    m = json.load(open(os.path.join(d, "meta.json")))
    return m, {c["name"]: Image.open(os.path.join(d, c["file"])).convert("RGBA") for c in m["clips"]}


def celula(m, clips, clip, q, d):
    s = m["size"]
    return clips[clip].crop((q * s, d * s, q * s + s, d * s + s)), (m["pivot_px"][0], s - m["pivot_px"][1])   # pivo de cima


def antes(m, clips, icone, clip, q, d):
    """Celula de 192 px com o pe em (96, 140): corpo + icone solto (ordem e lado do RefreshCarrier antigo)."""
    c, (px, py) = celula(m, clips, clip, q, d)
    out = Image.new("RGBA", (192, 192))
    n = round(PACK_S * GAME_PPU)
    ic = icone.resize((n, n), Image.LANCZOS)
    dx = {1: PACK_DX, 3: -PACK_DX}.get(d, 0.0)
    ix, iy = round(96 + dx * GAME_PPU - n / 2), round(140 - PACK_Y * GAME_PPU - n / 2)
    if d == 0:
        out.alpha_composite(ic, (ix, iy))
    out.alpha_composite(c, (round(96 - px), round(140 - py)))
    if d != 0:
        out.alpha_composite(ic, (ix, iy))
    return out


def depois(m, clips, clip, q, d):
    c, (px, py) = celula(m, clips, clip, q, d)
    out = Image.new("RGBA", (192, 192))
    out.alpha_composite(c, (round(96 - px), round(140 - py)))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sprites", default=os.path.join(RES, "Sprites"))
    ap.add_argument("--out", default=os.path.join(ROOT, "client", "Builds", "sprites_contact", "mochila_antes_depois.png"))
    a = ap.parse_args()
    mo, co = folha(os.path.join(RES, "Sprites"), "ferreiro")
    mn, cn = folha(a.sprites, "ferreiro_mochila")
    icone = Image.open(os.path.join(RES, "Sprites", "melhoria_mochila", "icone.png")).convert("RGBA")
    poses = [("parado", "Idle", 0), ("andando", "Walking", 4)]
    linhas = [(f"antes {t}", lambda d, c=c, q=q: antes(mo, co, icone, c, q, d)) for t, c, q in poses] + \
             [(f"depois {t}", lambda d, c=c, q=q: depois(mn, cn, c, q, d)) for t, c, q in poses]
    linhas = [linhas[0], linhas[2], linhas[1], linhas[3]]
    Z, L = 2, 150   # zoom de A, coluna dos rotulos
    W = L + 4 * 192 * Z
    k = 60.0 / GAME_PPU   # B: 60 px/m
    g = round(192 * k)
    bh = 4 * g + 40
    im = Image.new("RGBA", (max(W, 540 * 3 + 60), 70 + 4 * 192 * Z + 60 + bh * 2 + 40), FUNDO)
    texto(im, (16, 14), "Mochila do ferreiro: antes (icone solto nas costas) x depois (mochila no rig, Spine2)", 26, bold=True)
    texto(im, (16, 44), "A: de perto, 2x sem filtro (S, W, N, E)   B: tamanho de jogo, 540 px = 9 m (60 px/m), e 2x sem filtro", 18)
    y0 = 70
    for i, (nome, f) in enumerate(linhas):
        texto(im, (12, y0 + i * 192 * Z + 170), nome, 22, bold=True)
        for d in range(4):
            t = Image.new("RGBA", (192, 192), CHAO)
            t.alpha_composite(f(d))
            im.alpha_composite(t.resize((192 * Z, 192 * Z), Image.NEAREST), (L + d * 192 * Z, y0 + i * 192 * Z))
            if i == 0:
                texto(im, (L + d * 192 * Z + 8, y0 + 6), DIRS[d], 22, cor=(255, 230, 140), bold=True)
    yb = y0 + 4 * 192 * Z + 30
    texto(im, (16, yb), "B: tamanho de jogo (faixa de 540 px; linhas = antes parado, depois parado, antes andando, depois andando)", 18)
    faixa = Image.new("RGBA", (540, 4 * g), CHAO)
    for i, (_, f) in enumerate(linhas):
        for d in range(4):
            faixa.alpha_composite(f(d).resize((g, g), Image.LANCZOS), (round(20 + d * 130), i * g))
    im.alpha_composite(faixa, (16, yb + 30))
    im.alpha_composite(faixa.resize((1080, 8 * g), Image.NEAREST), (16 + 540 + 30, yb + 30))
    im = im.crop((0, 0, im.width, yb + 30 + 8 * g + 20))
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    im.convert("RGB").save(a.out)
    print("FOLHA_OK", a.out, im.size)


if __name__ == "__main__":
    main()
