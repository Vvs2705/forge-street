"""A-ART-02: folha de contato atual x forja (ferreiro Walking 4 dir + fornalha) a partir de <pasta>/{atual,forja}.
Uso: python montar_folha.py <pasta>   (Pillow; gera folha_contato.png, folha_contato_cinza.png, folha_jogo_1x.png, folha_jogo_x4.png)
A fornalha entra na MESMA escala de mundo do ferreiro (ppu do meta.json), como no jogo."""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

D = sys.argv[1]
CHAO = (74, 65, 58, 255)  # pedra quente da oficina (ART_BIBLE #4A413A)
FONT = ImageFont.load_default(size=22)


def load(preset, nome):
    pasta = os.path.join(D, preset, nome)
    m = json.load(open(os.path.join(pasta, "meta.json")))
    return Image.open(os.path.join(pasta, m["clips"][0]["file"])).convert("RGBA"), m


def bloco(preset, k=1.0):
    """Ferreiro (linhas S,W,N,E x quadros) + fornalha a direita, tudo multiplicado por k."""
    fer, mf = load(preset, "ferreiro")
    forn, mo = load(preset, "fornalha")
    forn = forn.resize((round(forn.width * mf["ppu"] / mo["ppu"]),) * 2, Image.LANCZOS)  # escala de mundo
    w, h = fer.width + forn.width, max(fer.height, forn.height)
    b = Image.new("RGBA", (w, h), CHAO)
    b.alpha_composite(fer, (0, 0))
    b.alpha_composite(forn, (fer.width, (h - forn.height) // 2))
    return b if k == 1.0 else b.resize((round(w * k), round(h * k)), Image.LANCZOS)


def altura_px(preset):  # altura do ferreiro (alfa) na 1a celula S
    fer, m = load(preset, "ferreiro")
    box = fer.crop((0, 0, m["size"], m["size"])).getchannel("A").point(lambda a: 255 if a > 32 else 0).getbbox()
    return box[3] - box[1]


def empilha(blocos, rotulos, pad=36):
    w, h = max(b.width for b in blocos), sum(b.height + pad for b in blocos)
    f = Image.new("RGBA", (w, h), (18, 22, 34, 255))
    y = 0
    for b, r in zip(blocos, rotulos):
        ImageDraw.Draw(f).text((8, y + 6), r, fill=(238, 241, 248, 255), font=FONT)
        f.alpha_composite(b, (0, y + pad))
        y += b.height + pad
    return f


ROT = {"atual": "ATUAL - Workbench flat + contorno + cavidade", "forja": "FORJA - EEVEE toon 3 faixas + contorno + chave quente + brasa"}
f = empilha([bloco(p) for p in ROT], list(ROT.values()))
f.save(os.path.join(D, "folha_contato.png"))
f.convert("L").save(os.path.join(D, "folha_contato_cinza.png"))
# tamanho real do jogo: ferreiro com 32 e 56 px de altura (a celula de 128 tem ~h px)
h = altura_px("atual")
alvos = (32, 56)
linhas = [empilha([bloco(p, a / h) for p in ROT], ["%s %d px" % (p, a) for p in ROT], pad=28) for a in alvos]
w = max(l.width for l in linhas)
j = Image.new("RGBA", (w, sum(l.height for l in linhas)), (18, 22, 34, 255))
y = 0
for l in linhas:
    j.alpha_composite(l, (0, y))
    y += l.height
j.save(os.path.join(D, "folha_jogo_1x.png"))
j.resize((j.width * 4, j.height * 4), Image.NEAREST).save(os.path.join(D, "folha_jogo_x4.png"))
print("ok altura_ferreiro_celula=%dpx alvos=%s" % (h, alvos))
