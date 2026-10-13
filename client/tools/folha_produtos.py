"""folha_produtos.py - folha de contato dos itens de itens_blender.py (Python 3 + Pillow; so QA, o jogo nao depende dela).

Cada item: icone a 128 e a 64 px em fundo claro (balao) e escuro (chao), silhueta preta a 64 px e o `deitado` a 64 px;
embaixo, todos lado a lado a 64 px (fundo escuro) e em silhueta, para cacar pares confundiveis. Os 6 itens atuais, a moeda,
a coroa do VIP e o sino das melhorias entram como referencia (nao podem ser confundidos com os novos).
Uso (da raiz do projeto): python client/tools/folha_produtos.py [--sprites DIR] [--out client/Builds/validation_art_produtos/folha.png]
"""
import argparse, os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from folhas_v05 import BALAO, ESCURO, RES, ROOT, texto

REFS = ["item_minerio", "item_lingote", "item_espada", "item_escudo", "item_ferramenta", "item_joia", "moeda", "coroa", "melhoria_sino"]
FUNDO = (30, 34, 46, 255)


def novos():
    import ast
    src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "itens_blender.py"), encoding="utf-8").read()
    return [k.value for n in ast.walk(ast.parse(src)) if isinstance(n, ast.Assign) and getattr(n.targets[0], "id", "") == "ITENS"
            for k in n.value.keys]   # ponytail: le o catalogo sem importar bpy


def png(sp, nome, clip):
    for d in (sp, os.path.join(RES, "Sprites")):   # --sprites de um render de teste; as referencias vem do jogo
        f = os.path.join(d, nome, clip + ".png")
        if os.path.exists(f):
            return Image.open(f).convert("RGBA")
    return None


def em(im, px, bg):
    t = Image.new("RGBA", (px, px), bg)
    t.alpha_composite(im.resize((px, px), Image.LANCZOS))
    return t


def silhueta(im, px):
    a = im.resize((px, px), Image.LANCZOS).getchannel("A")
    t = Image.new("RGBA", (px, px), BALAO)
    t.paste((0, 0, 0, 255), (0, 0), a)
    return t


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sprites", default=os.path.join(RES, "Sprites"))
    ap.add_argument("--out", default=os.path.join(ROOT, "client", "Builds", "validation_art_produtos", "folha.png"))
    a = ap.parse_args()
    nomes = REFS + novos()
    cols, cw, ch = 6, 290, 236
    rows = (len(nomes) + cols - 1) // cols
    n64 = 12
    r64 = (len(nomes) + n64 - 1) // n64
    W = cols * cw + 20
    H = 50 + rows * ch + 2 * (40 + r64 * 92) + 20
    folha = Image.new("RGBA", (W, H), FUNDO)
    texto(folha, (12, 12), "Forge Street - itens dos distritos 2-6 (itens_blender.py). 9 primeiros = referencia ja no jogo", 20, bold=True)
    for i, nome in enumerate(nomes):
        x, y = 10 + (i % cols) * cw, 50 + (i // cols) * ch
        ic, dt = png(a.sprites, nome, "icone"), png(a.sprites, nome, "deitado")
        texto(folha, (x + 4, y + 2), nome.replace("item_", "") + ("  (ref)" if nome in REFS else ""), 15, (255, 224, 138) if nome in REFS else (238, 241, 248))
        if ic is None:
            texto(folha, (x + 4, y + 40), "SEM ICONE", 18, (242, 84, 91))
            continue
        folha.alpha_composite(em(ic, 128, BALAO), (x, y + 24))
        folha.alpha_composite(em(ic, 128, ESCURO), (x + 136, y + 24))
        for k, im in enumerate((em(ic, 64, BALAO), em(ic, 64, ESCURO), silhueta(ic, 64), em(dt, 64, ESCURO) if dt else None)):
            if im:
                folha.alpha_composite(im, (x + k * 68, y + 160))
    y0 = 50 + rows * ch
    for titulo, f in (("todos a 64 px, fundo escuro", lambda im: em(im, 64, ESCURO)), ("silhueta a 64 px", lambda im: silhueta(im, 64))):
        texto(folha, (12, y0 + 10), titulo, 18, bold=True)
        for i, nome in enumerate(nomes):
            ic = png(a.sprites, nome, "icone")
            x, y = 10 + (i % n64) * (W - 20) // n64, y0 + 40 + (i // n64) * 92
            if ic:
                folha.alpha_composite(f(ic), (x, y))
            texto(folha, (x, y + 66), nome.replace("item_", "")[:17], 11)
        y0 += 40 + r64 * 92
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    folha.convert("RGB").save(a.out, optimize=True)
    print("FOLHA_OK %s %dx%d, %d itens" % (a.out, W, H, len(nomes)))


if __name__ == "__main__":
    main()
