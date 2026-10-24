"""Teste de silhueta (Workers, GDD §164): silhuetas pretas dos personagens na escala do jogo (60 px/m) e ampliadas 2x, do 1o quadro do Idle
virado para o sul. uso: python client/tools/silhuetas.py SAIDA.png [folha ...]   (sem folhas: o heroi nos 5 estagios e os workers com folha propria)"""

import json
import os
import sys

from PIL import Image, ImageDraw

SP = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', '_FS', 'Resources', 'Sprites') + '/'
ESC = {
    "ajudante": 1.19,
    "oficial": 1.02,
    "balconista": 1.20,
    "almoxarife": 1.18,
    "mestre_obras": 1.16,
    "vulcana": 1.03,
    "felix": 1.08,
}
NOMES = sys.argv[2:] or [
    "ferreiro",
    "ferreiro_e2",
    "ferreiro_e3",
    "ferreiro_e4",
    "ferreiro_e5",
    "ajudante",
    "oficial",
    "almoxarife",
    "mestre_obras",
    "balconista",
    "vulcana",
    "felix",
]
PXM = 60


def quadro(n):
    m = json.load(open(SP + n + "/meta.json"))
    c = next((c for c in m["clips"] if c["name"] == "Idle"), m["clips"][0])
    f = os.path.splitext(c.get("file") or c["name"])[0]
    im = Image.open(SP + n + "/" + f + ".png").convert("RGBA")
    sz = m["size"]
    row = m["dir_order"].index("S") if m.get("dir_order") else 0
    cel = im.crop((0, row * sz, sz, row * sz + sz))
    metros = sz / (m["ppu"] * 0.76 * ESC.get(n, 1.0))
    px = max(1, round(metros * PXM))
    a = cel.getchannel("A").point(lambda v: 255 if v > 40 else 0)
    preto = Image.new("RGBA", cel.size, (0, 0, 0, 0))
    preto.putalpha(a)
    bg = Image.new("RGBA", cel.size, (0, 0, 0, 0))
    sil = Image.composite(Image.new("RGBA", cel.size, (0, 0, 0, 255)), bg, a)
    return cel.resize((px, px), Image.LANCZOS), sil.resize((px, px), Image.LANCZOS)


cols = []
for n in NOMES:
    cor, sil = quadro(n)
    cols.append((n, cor, sil))
W = max(c[1].width for c in cols) * 2 + 8
H1 = max(c[1].height for c in cols)
Z = 2
folha = Image.new("RGB", (W * len(cols), 14 + H1 * 2 + H1 * Z + 10), (235, 230, 215))
d = ImageDraw.Draw(folha)
for i, (n, cor, sil) in enumerate(cols):
    x = i * W + 4
    d.text((x, 1), n[:12], fill=(0, 0, 0))
    folha.paste(cor, (x, 14), cor)
    folha.paste(sil, (x, 14 + H1), sil)
    z = sil.resize((sil.width * Z, sil.height * Z), Image.NEAREST)
    folha.paste(
        z, (i * W + (W - z.width) // 2 if z.width < W else x, 14 + H1 * 2 + 4), z
    )
folha.save(sys.argv[1])
print(folha.size, [(n, c.width) for n, c, _ in cols])
