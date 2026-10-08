"""folhas_v05.py - folhas de conferencia da leva 1 de arte v0.5 (Python 3 + Pillow; so QA, o jogo nao depende dela).

Monta, na escala do aparelho (1 m = 95 px, BENCHMARK_VISUAL), cenas falsas com as texturas e o elenco reais:
  v05_itens.png        icones (128 -> 24 px, fundo claro do balao e escuro do chao), deitados, em pe, antes x depois
  v05_balcao_4e8.png   modulos, estande de 4 e de 8 vagas com estoque e fila, prova de emenda (direto x montado)
  v05_portao.png       parede direita antes e depois do Corredor (portao de cima + porta de servico + pilares)
Uso (da raiz do projeto, depois de tools/arte_v05_blender.py com --check):
  python client/tools/folhas_v05.py [--sprites client/Assets/_FS/Resources/Sprites] [--check DIR] [--out client/Builds/sprites_contact]
"""
import argparse, json, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
RES = os.path.join(ROOT, "client", "Assets", "_FS", "Resources")
S = 95.0   # px por metro no aparelho (11,4 m de largura visivel em 1080 px)
CHAR = {"ferreiro": 1.0, "ajudante": 1.19, "guerreira": 1.24, "anao": 1.08, "elfa": 1.03, "goblin": 1.11, "cavaleiro": 0.79,
        "nobre": 0.91, "mago": 1.0}   # WorldView.CharScale / CharPpuMul (0,76)
ITENS = ["item_minerio", "item_lingote", "item_espada", "item_escudo", "item_ferramenta", "item_joia", "moeda"]
BALAO, ESCURO, PRETO = (244, 246, 250, 255), (78, 69, 57, 255), (0, 0, 0, 255)


def fonte(px, bold=False):
    for f in ("arialbd.ttf" if bold else "arial.ttf", "segoeui.ttf"):
        try:
            return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", f), px)
        except OSError:
            pass
    return ImageFont.load_default()


def texto(im, xy, s, px=18, cor=(238, 241, 248), bold=False):
    ImageDraw.Draw(im).text(xy, s, font=fonte(px, bold), fill=cor)


def colar(dst, src, x, y, bg=None):
    if bg:
        t = Image.new("RGBA", src.size, bg)
        t.alpha_composite(src)
        src = t
    dst.alpha_composite(src, (int(round(x)), int(round(y))))


class Folha:
    """Pasta Resources/Sprites/<nome>: meta.json + PNG do clipe (Static, icone, deitado ou a 1a celula de um personagem)."""

    def __init__(self, base, nome, clip=None):
        self.m = json.load(open(os.path.join(base, nome, "meta.json")))
        c = clip or self.m["clips"][0]["name"]
        im = Image.open(os.path.join(base, nome, c + ".png")).convert("RGBA")
        if self.m["dirs"] > 1:   # personagem: linha 0 (S), quadro 0
            n = next(k["frames"] for k in self.m["clips"] if k["name"] == c)
            im = im.crop((0, 0, im.width // n, im.height // self.m["dirs"]))
        self.im, self.ppu, self.piv = im, self.m["ppu"], self.m["pivot"]


# ------------------------------------------------------------------ cena falsa (mesma montagem da WorldView)

class Cena:
    def __init__(self, x0, x1, y0, y1, s=S):
        self.x0, self.x1, self.y0, self.y1, self.s = x0, x1, y0, y1, s
        self.im = Image.new("RGBA", (int((x1 - x0) * s), int((y1 - y0) * s)), (18, 22, 34, 255))
        self.sprites = []

    def px(self, x, y):
        return (x - self.x0) * self.s, (self.y1 - y) * self.s

    def chao(self, nome, a, b, tinta=1.0, gira=False):
        """Textura repetida (Art.Ground: 1 repeticao a cada 2 m) no retangulo de mundo a-b; gira = parede vertical."""
        tex = Image.open(os.path.join(RES, "Textures", nome + ".png")).convert("RGBA")
        if gira:
            tex = tex.rotate(90, expand=True)
        n = max(1, int(round(2 * self.s)))
        tex = tex.resize((n, n), Image.LANCZOS)
        if tinta != 1.0:
            tex = Image.fromarray(np.clip(np.asarray(tex).astype(float) * [tinta, tinta, tinta, 1], 0, 255).astype(np.uint8))
        (ax, ay), (bx, by) = self.px(a[0], b[1]), self.px(b[0], a[1])
        w, h = int(round(bx - ax)), int(round(by - ay))
        if w <= 0 or h <= 0:
            return
        tile = Image.new("RGBA", (w, h))
        for i in range(0, w, n):
            for j in range(0, h, n):
                tile.paste(tex, (i, j))
        self.im.alpha_composite(tile, (int(round(ax)), int(round(ay))))

    def sprite(self, f, x, y, ordem=None, ppu_mul=1.0, dx_px=0.0, dy_px=0.0):
        self.sprites.append((ordem if ordem is not None else -y, f, x, y, ppu_mul, dx_px, dy_px))

    def desenhar(self):
        for _, f, x, y, mul, dx, dy in sorted(self.sprites, key=lambda t: t[0]):
            k = self.s / (f.ppu * mul)
            im = f.im.resize((max(1, int(round(f.im.width * k))), max(1, int(round(f.im.height * k)))), Image.LANCZOS)
            px, py = self.px(x, y)
            colar(self.im, im, px - f.piv[0] * im.width + dx, py - (1 - f.piv[1]) * im.height - dy)
        self.sprites = []
        return self.im


def oficina(c, rua_aberta):
    """Chaos e parede direita como a WorldView.BuildScenery (sem a porta antiga: os sprites novos entram no lugar)."""
    c.chao("rua", (c.x0 - 1, c.y0 - 1), (c.x1 + 1, c.y1 + 1), 0.42)
    c.chao("rua", (c.x0 - 1, 14), (c.x1 + 1, 16.6), 0.8)
    c.chao("rua", (9, 0), (15, 14), 0.8 if rua_aberta else 0.35)
    c.chao("piso_oficina", (0, 0), (9, 12))
    c.chao("madeira", (0, 12), (9, 14))
    for a, b in ((0, 5.6), (7.4, 11.6), (13.4, 14)):
        c.chao("parede", (9.0, a), (9.6, b), gira=True)


# ------------------------------------------------------------------ itens

def mascaras_antigas():
    """As formas procedurais de Art.cs (antes): triangulo, losango, cruz, pedra, barra, anel com gema."""
    n = 64
    y, x = np.mgrid[0:n, 0:n]
    x, y = (x + 0.5) / n * 2 - 1, -((y + 0.5) / n * 2 - 1)
    a = np.arctan2(y, x)
    rr = 0.86 + 0.1 * np.sin(a * 3 + 0.7) + 0.06 * np.cos(a * 5)
    cy = y + 0.15
    d = x * x + cy * cy
    f = {
        "item_minerio": x * x + y * y <= rr * rr,
        "item_lingote": (np.abs(x) < 0.98) & (np.abs(y) < 0.98),
        "item_espada": (y > -0.8) & (np.abs(x) < (0.9 - y) * 0.58),
        "item_escudo": np.abs(x) + np.abs(y) < 0.98,
        "item_ferramenta": ((np.abs(x) < 0.3) | (np.abs(y) < 0.3)) & (np.abs(x) < 0.9) & (np.abs(y) < 0.9),
        "item_joia": ((d <= 0.68 ** 2) & (d >= 0.38 ** 2)) | (np.abs(x) / 0.4 + np.abs(y - 0.6) / 0.34 < 1),
    }
    cor = {"item_minerio": (0x9C, 0x84, 0x68), "item_lingote": (0xC8, 0xD0, 0xDC), "item_espada": (0x7F, 0xC4, 0xFF),
           "item_escudo": (0xF2, 0x54, 0x5B), "item_ferramenta": (0x4C, 0xD9, 0x64), "item_joia": (0xB0, 0x7C, 0xF2)}
    out = {}
    for k, m in f.items():
        arr = np.zeros((n, n, 4), np.uint8)
        arr[m] = cor[k] + (255,)
        out[k] = Image.fromarray(arr)
    return out


def balao(w, h, icone):
    b = Image.new("RGBA", (w + 12, h + 22), (0, 0, 0, 0))
    d = ImageDraw.Draw(b)
    d.polygon([(w // 2 - 8, h), (w // 2 + 8, h), (w // 2, h + 16)], fill=BALAO, outline=(30, 22, 18, 255))
    d.rounded_rectangle((2, 2, w + 2, h + 2), radius=int(min(w, h) * 0.22), fill=BALAO, outline=(30, 22, 18, 255), width=3)
    i = icone.resize((int(min(w, h) * 0.82),) * 2, Image.LANCZOS)
    colar(b, i, 2 + (w - i.width) / 2, 2 + (h - i.height) / 2)
    return b


def folha_itens(sp, out):
    W, linha = 1500, 150
    im = Image.new("RGBA", (W, 120 + linha * len(ITENS) + 520), (40, 44, 56, 255))
    texto(im, (16, 10), "Forge Street v0.5 - icones de item (Blender procedural, 0 credito)", 26, bold=True)
    texto(im, (16, 44), "por linha: icone 128 px no balao e no chao | icone 96/64/48/32/24 px claro e preto | deitado (pilha/estoque) 64/48/32 px no chao", 16)
    texto(im, (16, 66), "contorno #1E1612 3 px + sombra 4 px 35%; 1 m = 95 px no aparelho (pilha 0,5 m = 48 px; balao grande ~85 px)", 16)
    y = 100
    for n in ITENS:
        ic, de = Image.open(os.path.join(sp, n, "icone.png")).convert("RGBA"), Image.open(os.path.join(sp, n, "deitado.png")).convert("RGBA")
        texto(im, (16, y + 60), n, 16, bold=True)
        x = 170
        for bg in (BALAO, ESCURO):
            colar(im, ic, x, y, bg)
            x += 136
        for px in (96, 64, 48, 32, 24):
            r = ic.resize((px, px), Image.LANCZOS)
            colar(im, r, x, y + (128 - px) // 2 - 22, BALAO)
            colar(im, r, x, y + (128 - px) // 2 + 30, PRETO)
            x += px + 10
        x += 20
        for px in (64, 48, 32):
            colar(im, de.resize((px, px), Image.LANCZOS), x, y + (128 - px) // 2, ESCURO)
            x += px + 10
        y += linha
    # aceite: 24 px sobre preto, ampliado 4x sem filtro (o que o olho ve num celular a ~1 cm)
    texto(im, (16, y + 4), "Aceite (BENCHMARK_VISUAL): a 24 px sobre preto, 5 de 5 reconheciveis. Fileira real e ampliada 4x sem filtro:", 18, bold=True)
    fil = Image.new("RGBA", (len(ITENS) * 30 + 6, 32), PRETO)
    for i, n in enumerate(ITENS):
        colar(fil, Image.open(os.path.join(sp, n, "icone.png")).convert("RGBA").resize((24, 24), Image.LANCZOS), 6 + i * 30, 4)
    colar(im, fil, 16, y + 36)
    colar(im, fil.resize((fil.width * 4, fil.height * 4), Image.NEAREST), 260, y + 36)
    y += 180
    # antes x depois no balao do pedido (tamanho de hoje: 0,6 x 0,5 m com item de 0,3 m; proposta: 1,3 x 1,1 m)
    texto(im, (16, y), "Antes (Art.cs, balao de hoje 57x47 px, item 28 px)  x  depois (balao 1,3 x 1,1 m = 124x104 px, icone ~85 px)", 18, bold=True)
    old = mascaras_antigas()
    x = 16
    for n in ITENS[:6]:
        colar(im, balao(57, 47, old[n]), x, y + 40)
        colar(im, balao(124, 104, Image.open(os.path.join(sp, n, "icone.png")).convert("RGBA")), x + 66, y + 40)
        x += 222
    y += 190
    # em pe no estande, na escala do aparelho e 2x
    texto(im, (16, y), "Em pe no estande (item_*_em_pe, escala real 160 px/m): no aparelho (95 px/m) e 2x", 18, bold=True)
    x = 16
    for n in ("item_espada_em_pe", "item_escudo_em_pe", "item_ferramenta_em_pe"):
        f = Folha(sp, n)
        for k in (S / f.ppu, 2 * S / f.ppu):
            r = f.im.resize((int(f.im.width * k), int(f.im.height * k)), Image.LANCZOS)
            colar(im, r, x, y + 36, (110, 72, 40, 255))
            x += r.width + 14
        x += 30
    im.save(os.path.join(out, "v05_itens.png"))


# ------------------------------------------------------------------ balcao

def estande(c, sp, n, cheio):
    """Monta o balcao de n vagas centrado em (4,5; 13) como a View deve fazer: ponta + n modulos + ponta, itens nos encaixes."""
    tipos = (["balcao_meio", "balcao_escudos", "balcao_meio", "balcao_ferramentas"] * 2)[:n]
    larg = n * 0.85 + 0.4
    xl = 4.5 - larg / 2
    mods = [("balcao_ponta_esq", xl + 0.1)] + [(t, xl + 0.2 + 0.425 + 0.85 * i) for i, t in enumerate(tipos)] + [("balcao_ponta_dir", xl + larg - 0.1)]
    item = {"balcao_meio": "item_espada_em_pe", "balcao_escudos": "item_escudo_em_pe", "balcao_ferramentas": "item_ferramenta_em_pe"}
    k = 0
    for t, x in mods:
        f = Folha(sp, t)
        c.sprite(f, x, 13.0, ordem=-13.0)
        enc = f.m.get("encaixes", [])
        for j in range(0, len(enc), 2):
            k += 1
            if k % 5 != 0 or cheio:   # 1 encaixe em 5 vazio: estoque baixo le como rack vazio
                c.sprite(Folha(sp, item[t]), x + enc[j], 13.0, ordem=-12.99, dy_px=enc[j + 1] * c.s)
    return [4.5 + 0.85 * (i - (n - 1) / 2) for i in range(n)]


def fila(c, sp, slots):
    elenco = ["guerreira", "anao", "mago", "elfa", "goblin", "cavaleiro", "guerreira", "anao"]
    pedidos = ["item_espada", "item_escudo", "item_espada", "item_ferramenta", "item_espada", "item_escudo", "item_ferramenta", "item_espada"]
    for i, x in enumerate(slots):
        f = Folha(os.path.join(RES, "Sprites"), elenco[i], "Idle")
        c.sprite(f, x, 14.0, ppu_mul=0.76 * CHAR[elenco[i]])
    c.desenhar()
    for i, x in enumerate(slots):   # balao grande so no 1o da fila; mini-icone nos outros (BENCHMARK_VISUAL P0-2)
        ic = Image.open(os.path.join(sp, pedidos[i], "icone.png")).convert("RGBA")
        px, py = c.px(x, 14.0 + 0.8)   # acima da cabeca na folha (a HeadY 0,95 da View vale no plano do sprite)
        if i == 0:
            b = balao(int(1.3 * c.s), int(1.1 * c.s), ic)
            colar(c.im, b, px - b.width / 2, py - b.height)
        else:
            r = ic.resize((int(0.42 * c.s),) * 2, Image.LANCZOS)
            colar(c.im, r, px - r.width / 2, py - r.height)


def folha_balcao(sp, chk, out):
    blocos = []
    # modulos soltos (1:1, 160 px/m)
    mods = ["balcao_ponta_esq", "balcao_meio", "balcao_escudos", "balcao_ferramentas", "balcao_ponta_dir"]
    fs = [Folha(sp, m) for m in mods]
    strip = Image.new("RGBA", (sum(max(f.im.width, 120) for f in fs) + 40 * len(fs), fs[0].im.height + 30), (40, 44, 56, 255))
    x = 10
    for m, f in zip(mods, fs):
        colar(strip, f.im, x, 24, ESCURO)
        texto(strip, (x, 2), m.replace("balcao_", "") + " (%d px)" % f.im.width, 14)
        x += max(f.im.width, 120) + 40
    blocos.append(("Modulos (1:1, 160 px/m; meio = 0,85 m = 1 vaga = 136 px; pontas 0,2 m = Balance.CounterEnd)", strip))
    for n, cheio in ((4, False), (8, True)):
        c = Cena(0.0, 9.0, 11.3, 17.0)
        oficina(c, False)
        slots = estande(c, sp, n, cheio)
        fila(c, sp, slots)
        blocos.append(("Estande de %d vagas no aparelho (1 m = 95 px), estoque %s, fila com balao grande no 1o" % (n, "cheio" if cheio else "com encaixes vazios"), c.im))
    c = Cena(1.9, 7.1, 11.9, 16.6, s=2 * S)
    oficina(c, False)
    fila(c, sp, estande(c, sp, 4, False))
    blocos.append(("Zoom 2x do estande de 4 vagas", c.im))
    if chk and os.path.exists(os.path.join(chk, "emenda.txt")):
        d = Image.open(os.path.join(chk, "balcao_8_direto.png")).convert("RGBA")
        m = Image.open(os.path.join(chk, "balcao_8_montado.png")).convert("RGBA")
        diff = np.abs(np.asarray(d).astype(int) - np.asarray(m).astype(int)).max(axis=2)
        dm = Image.fromarray(np.clip(diff * 20, 0, 255).astype(np.uint8)).convert("RGBA")
        prova = Image.new("RGBA", (d.width, d.height * 3 + 20), (40, 44, 56, 255))
        colar(prova, d, 0, 0, ESCURO)
        colar(prova, m, 0, d.height + 10, ESCURO)
        colar(prova, dm, 0, 2 * d.height + 20)
        rel = open(os.path.join(chk, "emenda.txt")).read().strip().replace("\n", "  |  ")
        blocos.append(("Emenda: render direto (cima) x montado dos modulos (meio) x diferenca x20 (baixo). " + rel, prova))
    W = max(b.width for _, b in blocos) + 32
    H = sum(b.height + 44 for _, b in blocos) + 60
    im = Image.new("RGBA", (W, H), (40, 44, 56, 255))
    texto(im, (16, 10), "Forge Street v0.5 - balcao sem toldo, estande modular por vaga (4 -> 8)", 26, bold=True)
    y = 56
    for t, b in blocos:
        texto(im, (16, y), t, 15, bold=True)
        colar(im, b, 16, y + 24)
        y += b.height + 44
    im.save(os.path.join(out, "v05_balcao_4e8.png"))


# ------------------------------------------------------------------ portoes

def parede(c, sp, aberto):
    pil = Folha(sp, "pilar")
    for y in (5.6 - 0.23, 7.4 + 0.23, 11.6 - 0.23, 13.4 + 0.23):
        c.sprite(pil, 9.3, y)
    c.sprite(Folha(sp, "portao_aberto" if aberto else "portao_fechado"), 9.3, 12.5, ordem=-99)
    c.sprite(Folha(sp, "porta_servico_aberta" if aberto else "porta_servico_fechada"), 9.3, 6.5, ordem=-99)
    c.sprite(Folha(os.path.join(RES, "Sprites"), "ferreiro", "Idle"), 8.2, 6.2, ppu_mul=0.76)
    c.sprite(Folha(sp, "balcao_ponta_dir"), 8.1 - 0.1, 13.0, ordem=-13.0)   # ponta direita do estande de 8 vagas
    c.sprite(Folha(sp, "balcao_meio"), 8.1 - 0.2 - 0.425, 13.0, ordem=-13.0)


def folha_portao(sp, out):
    painel = []
    for aberto in (False, True):
        c = Cena(6.4, 11.6, 4.3, 15.4)
        oficina(c, aberto)
        parede(c, sp, aberto)
        painel.append(c.desenhar())
    zoom = []
    for aberto in (False, True):
        c = Cena(8.0, 11.0, 10.4, 15.2, s=2 * S)
        oficina(c, aberto)
        parede(c, sp, aberto)
        zoom.append(c.desenhar())
    for aberto in (False, True):
        c = Cena(8.0, 11.0, 4.6, 9.0, s=2 * S)
        oficina(c, aberto)
        parede(c, sp, aberto)
        zoom.append(c.desenhar())
    W = painel[0].width * 2 + zoom[0].width * 2 + 120
    H = max(painel[0].height, zoom[0].height + zoom[2].height + 40) + 110
    im = Image.new("RGBA", (W, H), (40, 44, 56, 255))
    texto(im, (16, 10), "Forge Street v0.5 - parede direita vista DE LADO (camera de 60): antes x depois do Corredor", 24, bold=True)
    texto(im, (16, 44), "pilar de pedra nas 4 pontas das aberturas; portao com tranca em y 11,6-13,4 (abre para a rua); porta de servico em y 5,6-7,4. 1 m = 95 px; zoom 2x a direita", 15)
    x = 16
    for t, p in zip(("antes (fechado, rua escura)", "depois (aberto, rua acesa)"), painel):
        texto(im, (x, 76), t, 16, bold=True)
        colar(im, p, x, 100)
        x += p.width + 30
    for i, z in enumerate(zoom):
        zx = x + (i % 2) * (z.width + 20)
        zy = 100 + (i // 2) * (zoom[0].height + 30)
        colar(im, z, zx, zy)
    im.save(os.path.join(out, "v05_portao.png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sprites", default=os.path.join(RES, "Sprites"))
    ap.add_argument("--check", default="")
    ap.add_argument("--out", default=os.path.join(ROOT, "client", "Builds", "sprites_contact"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    folha_itens(a.sprites, a.out)
    folha_balcao(a.sprites, a.check, a.out)
    folha_portao(a.sprites, a.out)
    print("FOLHAS_OK " + a.out)


if __name__ == "__main__":
    main()
