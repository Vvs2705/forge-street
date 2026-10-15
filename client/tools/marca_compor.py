"""marca_compor.py - Forge Street C-ART-14 (0 credito): compoe as 4 variantes de icone, as camadas do icone adaptativo,
o logo e a feature graphic a partir das camadas do marca_blender.py e de um quadro gravado do jogo.

Uso (raiz do projeto; Python 3 + Pillow + numpy; ffmpeg no PATH): python client/tools/marca_compor.py
Entrada: client/Builds/marca/camadas/*.png (marca_blender.py) e o criativo gravado do jogo
         client/Builds/creatives/v06/fs_01_bigorna_rua.mp4 da pasta principal (quadro de 22,5 s).
Saida:
  arte/marca/icone_{a_ferreiro_martelo,b_bigorna_fogo,c_ferreiro_ouro,d_obra_prima}.png  512x512 RGB, sem transparencia
  arte/marca/logo.png (1024 de largura, RGBA), arte/marca/feature_graphic.png (1024x500 RGB), arte/marca/icone_leitura.png
  client/Builds/marca/icone_leitura.png (copia local da folha, fora do git)
  client/Assets/_FS/Art/Marca/icone.png (512, legado/PC), icone_fundo.png e icone_frente.png (432, adaptativo) da ESCOLHIDA
Fonte: Inter (SIL OFL 1.1, Copyright 2020 The Inter Project Authors) que vem com o Blender 5.2 (datafiles/fonts/Inter.woff2),
eixo de peso em Black (900). Nada baixado.
"""
import os, random, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

RAIZ = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
CAM = os.path.join(RAIZ, "client", "Builds", "marca", "camadas")
SAI = os.path.join(RAIZ, "arte", "marca")
UNITY = os.path.join(RAIZ, "client", "Assets", "_FS", "Art", "Marca")
INTER = "C:/Program Files/Blender Foundation/Blender 5.2/5.2/datafiles/fonts/Inter.woff2"
VIDEO = "C:/Users/VINICIUS/Videos/MEUS PROJETOS/JOGOS NOVOS/03_FORGE_STREET/client/Builds/creatives/v06/fs_01_bigorna_rua.mp4"
ESCOLHIDA = "a_ferreiro_martelo"   # ver docs/ASSETS.md s13 (leitura a 48 px, contraste, coerencia)
TINTA = (30, 22, 18)               # #1E1612, contorno dos itens do jogo
K = 2                              # supersample: compoe a 1024 e reduz para 512


def rgb(h):
    return tuple((h >> s) & 255 for s in (16, 8, 0))


def camada(nome):
    im = Image.open(os.path.join(CAM, nome + ".png")).convert("RGBA")
    return im.crop(im.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())


def escala(im, w=None, h=None):
    s = (w / im.width) if w else (h / im.height)
    return im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)


def radial(w, h, cx, cy, r, stops):
    """Gradiente radial: stops [(distancia/r, 0xRRGGBB)] interpolados em sRGB."""
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(xx - cx, yy - cy) / r
    ts = [t for t, _ in stops]
    cs = np.array([rgb(c) for _, c in stops], np.float32)
    out = np.stack([np.interp(d, ts, cs[:, i]) for i in range(3)], axis=2)
    return Image.fromarray(out.astype(np.uint8), "RGB").convert("RGBA")


def contorno(im, r, cor=TINTA):
    """Contorno arredondado de ~r px atras da figura (dilatacao por desfoque + limiar)."""
    pad = r * 2
    base = Image.new("RGBA", (im.width + 2 * pad, im.height + 2 * pad), (0, 0, 0, 0))
    base.alpha_composite(im, (pad, pad))
    a = np.asarray(base.getchannel("A").filter(ImageFilter.GaussianBlur(r * 0.55)), np.float32) / 255
    o = np.clip((a - 0.04) / 0.08, 0, 1)
    sombra = Image.new("RGBA", base.size, cor + (0,))
    sombra.putalpha(Image.fromarray((o * 255).astype(np.uint8)))
    sombra.alpha_composite(base)
    return sombra, pad


def brilho(im, r, cor, forca=1.0):
    """Halo: a silhueta desfocada e tingida (vai atras da figura)."""
    a = np.asarray(im.getchannel("A").filter(ImageFilter.GaussianBlur(r)), np.float32) / 255
    g = Image.new("RGBA", im.size, rgb(cor) + (0,))
    g.putalpha(Image.fromarray((np.clip(a * forca, 0, 1) * 255).astype(np.uint8)))
    return g


def colar(fundo, im, x, y):
    """alpha_composite que aceita figura saindo da tela."""
    tmp = Image.new("RGBA", fundo.size, (0, 0, 0, 0))
    tmp.paste(im, (int(x), int(y)), im)
    fundo.alpha_composite(tmp)


def figura(fundo, im, cx, topo, r_cont, halo=None):
    """Cola a figura com contorno (e halo) com o centro horizontal em cx e o topo da figura em `topo`."""
    fig, pad = contorno(im, r_cont)
    if halo:
        colar(fundo, brilho(fig, *halo), cx - fig.width / 2, topo - pad)
    colar(fundo, fig, cx - fig.width / 2, topo - pad)


def faiscas(tam, origem, n, raio, ang, seed, comp=(14, 40), larg=(3, 6)):
    """Faiscas: tracos curtos saindo de `origem` num leque (graus, 0 = para cima), nucleo amarelo + halo laranja.
    Semente fixa: a mesma folha a cada rodada."""
    rnd = random.Random(seed)
    lay = Image.new("RGBA", tam, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    for _ in range(n):
        a = np.radians(rnd.uniform(*ang))
        dist = rnd.uniform(raio * 0.25, raio)
        x0, y0 = origem[0] + np.sin(a) * dist, origem[1] - np.cos(a) * dist
        ln = rnd.uniform(*comp)
        x1, y1 = x0 + np.sin(a) * ln, y0 - np.cos(a) * ln
        w = rnd.uniform(*larg)
        d.line([(x0, y0), (x1, y1)], fill=rgb(0xFFE9A8) + (255,), width=int(round(w)))
        d.ellipse([x1 - w * 0.6, y1 - w * 0.6, x1 + w * 0.6, y1 + w * 0.6], fill=rgb(0xFFF6D8) + (255,))
    halo = brilho(lay, 6, 0xFF7A1F, 2.2)
    halo.alpha_composite(lay)
    return halo


def estrela(d, c, r, cor, fino=0.22):
    """Brilho de 4 pontas (o 'brilho' da Art.cs e da faisca dourada da obra-prima)."""
    x, y = c
    pts = []
    for i in range(8):
        rr = r if i % 2 == 0 else r * fino
        a = np.radians(i * 45)
        pts.append((x + np.sin(a) * rr, y - np.cos(a) * rr))
    d.polygon(pts, fill=cor)


def vinheta(im, forca=0.45):
    w, h = im.size
    v = radial(w, h, w / 2, h / 2, w * 0.75, [(0.0, 0x000000), (0.62, 0x000000), (1.0, 0x000000)])
    yy, xx = np.mgrid[0:h, 0:w]
    a = np.clip((np.hypot(xx - w / 2, yy - h / 2) / (w * 0.72) - 0.55) / 0.45, 0, 1) * forca
    v.putalpha(Image.fromarray((a * 255).astype(np.uint8)))
    im.alpha_composite(v)


# ------------------------------------------------------------------ icones (desenho na janela S x S; a tela pode ser maior)

def ponta(fig):
    """Centro do objeto erguido: media das colunas opacas nos 6% de cima da figura."""
    a = np.asarray(fig.getchannel("A")) > 128
    y0 = int(np.argmax(a.any(axis=1)))
    faixa = a[y0:y0 + max(2, int(fig.height * 0.06))]
    return float(np.nonzero(faixa.any(axis=0))[0].mean()), y0 + faixa.shape[0] / 2


def _ferreiro(nome, fundo_stops, S, tela):
    """Ferreiro do peito para cima com o objeto erguido (martelo ou moeda) no alto a esquerda, dentro do circulo seguro
    do adaptativo; o corpo desce ate a borda da tela (sangria)."""
    o = (tela - S) / 2
    fundo = radial(tela, tela, o + S * 0.5, o + S * 0.4, S * 0.78, fundo_stops)
    fig = escala(camada(nome), h=S * 1.42)          # corpo inteiro a 1,42 S: do topo do objeto ao peito na janela
    fig = fig.crop((0, 0, fig.width, min(fig.height, int(tela - o + S * 0.1))))
    frente = Image.new("RGBA", (tela, tela), (0, 0, 0, 0))
    x0, y0 = o + S * 0.53 - fig.width / 2, o + S * 0.11
    figura(frente, fig, o + S * 0.53, y0, 7 * K, halo=(20 * K, 0xFFD166, 1.3))
    px, py = ponta(fig)
    colar(frente, faiscas((tela, tela), (x0 + px, y0 + py), 16, S * 0.2, (-85, 85), 7, (8 * K, 18 * K), (2 * K, 4 * K)), 0, 0)
    return fundo, frente


def icone_a(S, tela):
    return _ferreiro("ferreiro_martelo", [(0, 0xFFD58A), (0.35, 0xFF9A3D), (0.75, 0xC1452D), (1.15, 0x4A1A0E)], S, tela)


def icone_c(S, tela):
    fundo, frente = _ferreiro("ferreiro_moeda", [(0, 0x6E9CE6), (0.45, 0x2F64B8), (0.9, 0x1F3566), (1.2, 0x121622)], S, tela)
    o = (tela - S) / 2
    m = escala(camada("moedas"), w=S * 0.56)
    figura(frente, m, o + S * 0.7, o + S * 0.97 - m.height * 0.8, 5 * K)
    return fundo, frente


def icone_b(S, tela):
    """Bigorna da heraldica com a espada em brasa, chama atras e faiscas saindo da lamina."""
    o = (tela - S) / 2
    fundo = radial(tela, tela, o + S * 0.5, o + S * 0.55, S * 0.75,
                   [(0, 0x6A4A3A), (0.45, 0x3E2A22), (0.85, 0x22181A), (1.3, 0x121012)])
    b = escala(camada("bigorna"), w=S * 0.84)
    frente = Image.new("RGBA", (tela, tela), (0, 0, 0, 0))
    ch = chama(int(S * 0.8), int(S * 0.62))
    colar(frente, brilho(ch, 18 * K, 0xFF7A1F, 1.5), o + S * 0.1, o + S * 0.06)
    colar(frente, ch, o + S * 0.1, o + S * 0.06)
    y0 = o + S * 0.93 - b.height
    figura(frente, b, o + S * 0.5, y0, 6 * K)
    colar(frente, faiscas((tela, tela), (o + S * 0.42, y0 + b.height * 0.22), 24, S * 0.3, (-70, 70), 11, (10 * K, 24 * K), (2 * K, 5 * K)), 0, 0)
    return fundo, frente


def icone_d(S, tela):
    o = (tela - S) / 2
    fundo = radial(tela, tela, o + S * 0.5, o + S * 0.5, S * 0.75,
                   [(0, 0xC2405E), (0.4, 0x962A48), (0.8, 0x4A1020), (1.2, 0x1E0810)])
    raios = Image.new("RGBA", (tela, tela), (0, 0, 0, 0))
    d = ImageDraw.Draw(raios)
    c = (o + S * 0.5, o + S * 0.5)
    for i in range(12):   # raios dourados (leque fixo)
        a0, a1 = np.radians(i * 30 - 6), np.radians(i * 30 + 6)
        d.polygon([c, (c[0] + np.sin(a0) * tela, c[1] - np.cos(a0) * tela), (c[0] + np.sin(a1) * tela, c[1] - np.cos(a1) * tela)],
                  fill=rgb(0xFFD166) + (46,))
    fundo.alpha_composite(raios.filter(ImageFilter.GaussianBlur(3 * K)))
    e = escala(camada("espada"), h=S * 0.8)   # a diagonal inteira dentro do circulo seguro
    frente = Image.new("RGBA", (tela, tela), (0, 0, 0, 0))
    figura(frente, e, o + S * 0.5, o + S * 0.1, 7 * K, halo=(26 * K, 0xFFD166, 1.6))
    est = Image.new("RGBA", (tela, tela), (0, 0, 0, 0))
    d = ImageDraw.Draw(est)
    for (x, y, r) in ((0.78, 0.2, 0.09), (0.24, 0.72, 0.065), (0.7, 0.66, 0.045), (0.3, 0.28, 0.04)):
        estrela(d, (o + S * x, o + S * y), S * r, rgb(0xFFF6D8) + (255,))
    halo = brilho(est, 5 * K, 0xFFD166, 2.0)
    halo.alpha_composite(est)
    frente.alpha_composite(halo)
    return fundo, frente


VARIANTES = {"a_ferreiro_martelo": icone_a, "b_bigorna_fogo": icone_b, "c_ferreiro_ouro": icone_c, "d_obra_prima": icone_d}


def icone(build, S=512):
    fundo, frente = build(S * K, S * K)
    vinheta(fundo, 0.35)
    fundo.alpha_composite(frente)
    return fundo.convert("RGB").resize((S, S), Image.LANCZOS)


def adaptativo(build, L=432):
    """Camadas do icone adaptativo do Android (108 dp; a mascara mostra os 72 dp do meio, o circulo seguro tem 66 dp):
    a mesma composicao com a janela S = 2/3 da camada; o fundo e o corpo continuam ate a borda (sangria)."""
    tela = L * K
    fundo, frente = build(round(tela * 72 / 108), tela)
    return fundo.convert("RGB").resize((L, L), Image.LANCZOS), frente.resize((L, L), Image.LANCZOS)


def mascara_circulo(im):
    m = Image.new("L", (im.width * 4, im.height * 4), 0)
    ImageDraw.Draw(m).ellipse([0, 0, m.width - 1, m.height - 1], fill=255)
    out = im.convert("RGBA")
    out.putalpha(m.resize(im.size, Image.LANCZOS))
    return out


def fonte(px, peso="Black"):
    f = ImageFont.truetype(INTER, px)
    f.set_variation_by_name(peso)
    return f


def folha_leitura(icones, adapt):
    """Cada variante a 512, 192, 96, 48 e 36 px em fundo claro e escuro, mais o adaptativo em circulo a 96 e 48."""
    tams = [512, 192, 96, 48, 36]
    lw = 300
    larg = lw + 2 * (sum(tams) + 24 * len(tams)) + 2 * (96 + 48 + 48) + 40
    alt = 40 + len(icones) * (512 + 40) + 60
    sheet = Image.new("RGB", (larg, alt), (128, 128, 128))
    d = ImageDraw.Draw(sheet)
    meia = (larg - lw) // 2
    d.rectangle([lw, 0, lw + meia, alt], fill=(242, 242, 242))
    d.rectangle([lw + meia, 0, larg, alt], fill=(18, 18, 18))
    f, fp = fonte(26, "Bold"), fonte(18, "Medium")
    d.text((lw + 10, 8), "fundo claro (#F2F2F2)", font=fp, fill=(40, 40, 40))
    d.text((lw + meia + 10, 8), "fundo escuro (#121212)", font=fp, fill=(220, 220, 220))
    for i, (nome, im) in enumerate(icones.items()):
        y0 = 40 + i * (512 + 40)
        d.text((10, y0 + 10), nome, font=f, fill=(255, 255, 255))
        if nome == ESCOLHIDA:
            d.text((10, y0 + 44), "ESCOLHIDA", font=f, fill=(255, 209, 102))
        d.text((10, y0 + 84), "512 192 96 48 36 px\n+ adaptativo em\ncirculo a 96 e 48", font=fp, fill=(235, 235, 235))
        for k, x0 in enumerate((lw + 12, lw + meia + 12)):
            x = x0
            for t in tams:
                sheet.paste(im.resize((t, t), Image.LANCZOS), (x, y0 + (512 - t) // 2))
                x += t + 24
            bg, fg = adapt[nome]
            ad = bg.convert("RGBA")
            ad.alpha_composite(fg)
            ad = ad.crop((72, 72, 360, 360))   # os 72 dp visiveis
            for t in (96, 48):
                c = mascara_circulo(ad.resize((t, t), Image.LANCZOS))
                sheet.paste(c, (x, y0 + (512 - t) // 2), c)
                x += t + 24
    return sheet


# ------------------------------------------------------------------ logo e feature graphic

def texto_metal(txt, px, cima, baixo, contorno_px, extrusao, track=0):
    """Texto Inter Black com degrade vertical (cima -> baixo), contorno escuro e extrusao para baixo (volume de metal)."""
    f = fonte(px)
    letras = [(c, f.getbbox(c)) for c in txt]
    w = sum(b[2] - b[0] if c != " " else px // 3 for c, b in letras) + track * (len(txt) - 1)
    asc, desc = f.getmetrics()
    pad = contorno_px + extrusao + 4
    W, H = w + 2 * pad, asc + desc + 2 * pad
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    x = pad
    for c, b in letras:
        if c == " ":
            x += px // 3 + track
            continue
        md.text((x - b[0], pad), c, font=f, fill=255)
        x += b[2] - b[0] + track
    bb = mask.getbbox()
    yy = np.linspace(0, 1, H)[:, None]
    t = np.clip((yy * H - bb[1]) / max(1, bb[3] - bb[1]), 0, 1)
    c0, c1 = np.array(rgb(cima), np.float32), np.array(rgb(baixo), np.float32)
    grad = (c0 * (1 - t[..., None]) + c1 * t[..., None]) * np.ones((1, W, 1))
    face = Image.fromarray(grad.astype(np.uint8), "RGB").convert("RGBA")
    face.putalpha(mask)
    borda = mask.filter(ImageFilter.MaxFilter(2 * contorno_px + 1))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for k in range(extrusao, 0, -1):   # extrusao: o contorno repetido para baixo, bronze escuro
        lay = Image.new("RGBA", (W, H), rgb(0x3E2412) + (0,))
        lay.putalpha(borda)
        colar(out, lay, 0, k)
    lay = Image.new("RGBA", (W, H), TINTA + (0,))
    lay.putalpha(borda)
    out.alpha_composite(lay)
    out.alpha_composite(face)
    # risco de brilho no terco de cima das letras
    hl = Image.new("RGBA", (W, H), (255, 255, 255, 0))
    band = Image.new("L", (W, H), 0)
    ImageDraw.Draw(band).rectangle([0, bb[1] + (bb[3] - bb[1]) * 0.08, W, bb[1] + (bb[3] - bb[1]) * 0.2], fill=90)
    hl.putalpha(Image.fromarray(np.minimum(np.asarray(band), np.asarray(mask)).astype(np.uint8)))
    out.alpha_composite(hl)
    return out.crop(out.getbbox())


def tabua(w, h):
    """Tabua de madeira do jogo (ART_BIBLE s2: #8A5A32 base, #5C3A1E sombra, #B8824F luz) com 2 rebites de ferro."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = h // 5
    d.rounded_rectangle([0, 0, w - 1, h - 1], r, fill=TINTA + (255,))
    d.rounded_rectangle([5, 5, w - 6, h - 6], r - 4, fill=rgb(0x5C3A1E) + (255,))
    d.rounded_rectangle([5, 5, w - 6, h - 12], r - 4, fill=rgb(0x8A5A32) + (255,))
    d.rounded_rectangle([12, 10, w - 13, 10 + h // 7], r // 2, fill=rgb(0xB8824F) + (255,))
    for y in (h * 0.42, h * 0.68):   # veios
        d.line([(30, y), (w * 0.3, y + 3), (w * 0.55, y - 2), (w - 30, y + 2)], fill=rgb(0x7A4E2A) + (255,), width=3)
    for x in (h * 0.45, w - h * 0.45):
        d.ellipse([x - 10, h / 2 - 10, x + 10, h / 2 + 10], fill=rgb(0x4F5866) + (255,))
        d.ellipse([x - 6, h / 2 - 8, x + 4, h / 2 + 2], fill=rgb(0xD5DCE6) + (255,))
    return im


def chama(w, h):
    """Chama da heraldica (bigorna + chama, ART_BIBLE s1): 3 linguas arredondadas em 3 camadas (vermelho, laranja, amarelo)."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    base = h * 0.86

    def lingua(cx, lw, lh, curva, cor):
        n = 28
        esq, dir_ = [], []
        for i in range(n + 1):
            t = i / n
            hw = lw / 2 * np.sin(np.pi * (0.5 + 0.5 * t)) ** 0.7
            x = cx + curva * lw * t * t
            esq.append((x - hw, base - t * lh))
            dir_.append((x + hw, base - t * lh))
        fundo_ = [(cx + lw / 2 * np.cos(a), base + lw * 0.32 * np.sin(a)) for a in np.linspace(0, np.pi, 14)]
        d.polygon(esq + dir_[::-1] + fundo_, fill=rgb(cor) + (255,))
    for cor, k in ((0xC1272D, 1.0), (0xFF7A1F, 0.74), (0xFFD166, 0.46)):
        lingua(w * 0.3, w * 0.34 * k, h * 0.5 * k, -0.35, cor)
        lingua(w * 0.7, w * 0.34 * k, h * 0.56 * k, 0.35, cor)
        lingua(w * 0.5, w * 0.46 * k, h * 0.8 * k, 0.12, cor)
    return im


def emblema(alt):
    """Simbolo: bigorna + martelo (render) na frente da chama, com contorno; `alt` = altura total em px."""
    s = escala(camada("simbolo"), h=int(alt * 0.72))
    ch = chama(int(s.width * 0.92), int(alt * 0.72))
    W, H = max(s.width, ch.width) + 60, alt + 60
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    cx = W / 2
    colar(out, brilho(ch, alt // 22, 0xFF7A1F, 1.4), cx - ch.width / 2 - s.width * 0.04, 30)
    colar(out, ch, cx - ch.width / 2 - s.width * 0.04, 30)
    fig, pad = contorno(s, max(4, alt // 70))
    colar(out, fig, cx - fig.width / 2, H - 30 - s.height - pad)
    return out.crop(out.getbbox())


def wordmark(larg):
    forge = texto_metal("FORGE", 260, 0xFFE08A, 0xB8862A, 12, 10, track=6)
    forge = escala(forge, w=larg)
    street = texto_metal("STREET", 120, 0xEEF1F8, 0x8C96A6, 8, 5, track=26)
    street = escala(street, w=int(larg * 0.62))
    t = tabua(int(larg * 0.86), int(street.height * 1.55))
    W, H = larg, forge.height + int(t.height * 0.86)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    colar(out, t, (W - t.width) / 2, H - t.height)
    colar(out, street, (W - street.width) / 2, H - t.height + (t.height - street.height) / 2 - 3)
    colar(out, forge, (W - forge.width) / 2, 0)
    return out


def logo():
    """Vertical: simbolo em cima, FORGE em ouro e STREET na tabua; 1024 de largura, fundo transparente."""
    em = emblema(470)
    wm = wordmark(1000)
    W, H = 1024, em.height + wm.height - 40
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    colar(out, em, (W - em.width) / 2, 0)
    colar(out, wm, (W - wm.width) / 2, em.height - 40)
    return out


def feature_graphic():
    """1024x500: faixa de um quadro gravado do jogo (oficina cheia, 22,5 s do criativo fs_01) com o logo na horizontal
    (simbolo a esquerda, wordmark a direita) sobre um veu escuro no meio."""
    q = os.path.join(RAIZ, "client", "Builds", "marca", "fonte", "fs01_t22.5.png")
    if not os.path.exists(q):
        os.makedirs(os.path.dirname(q), exist_ok=True)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", "22.5", "-i", VIDEO, "-frames:v", "1", q], check=True)
    jogo = Image.open(q).convert("RGBA").crop((0, 290, 1000, 290 + 488)).resize((1024, 500), Image.LANCZOS)   # sem a seta e o rotulo da rua lateral
    w, h = jogo.size
    yy, xx = np.mgrid[0:h, 0:w]
    a = np.clip(0.95 - np.hypot((xx - w * 0.5) / (w * 0.55), (yy - h * 0.5) / (h * 0.9)) * 0.5, 0.38, 0.86)
    veu = Image.new("RGBA", (w, h), (20, 12, 8, 0))
    veu.putalpha(Image.fromarray((a * 255).astype(np.uint8)))
    jogo.alpha_composite(veu)
    em = emblema(330)
    wm = wordmark(560)
    x0 = (w - (em.width + 24 + wm.width)) / 2
    colar(jogo, em, x0, (h - em.height) / 2)
    colar(jogo, wm, x0 + em.width + 24, (h - wm.height) / 2 + 10)
    return jogo.convert("RGB")


def main():
    for p in (SAI, UNITY, os.path.join(RAIZ, "client", "Builds", "marca")):
        os.makedirs(p, exist_ok=True)
    icones, adapt = {}, {}
    for nome, build in VARIANTES.items():
        icones[nome] = icone(build)
        icones[nome].save(os.path.join(SAI, "icone_%s.png" % nome), optimize=True)
        adapt[nome] = adaptativo(build)
        print("ICONE %s" % nome)
    sheet = folha_leitura(icones, adapt)
    sheet.save(os.path.join(SAI, "icone_leitura.png"), optimize=True)
    sheet.save(os.path.join(RAIZ, "client", "Builds", "marca", "icone_leitura.png"))
    icones[ESCOLHIDA].save(os.path.join(UNITY, "icone.png"), optimize=True)
    adapt[ESCOLHIDA][0].save(os.path.join(UNITY, "icone_fundo.png"), optimize=True)
    adapt[ESCOLHIDA][1].save(os.path.join(UNITY, "icone_frente.png"), optimize=True)
    logo().save(os.path.join(SAI, "logo.png"), optimize=True)
    feature_graphic().save(os.path.join(SAI, "feature_graphic.png"), optimize=True)
    print("MARCA_COMPOR_OK escolhida=%s -> %s" % (ESCOLHIDA, SAI))


if __name__ == "__main__":
    main()
