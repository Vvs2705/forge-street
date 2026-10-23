"""Clareia uma folha de estacao renderizada escura demais (luz flat: a cor e' a da textura do Tripo).

uso: python client/tools/clarear_sprite.py <Static.png> [--v 0.62] [--sat 1.35] [--brilho x,y,raio,cor_hex,forca]

Em HSV: V^v (sobe os escuros sem lavar o claro) e saturacao x sat; alfa intacto. --brilho soma um brilho radial em "screen"
centrado em (x, y) px, so onde o modelo existe (o fogo que a textura nao trouxe). Usado na forja estelar da Grand Forge
(lum media 64 -> ~100; as outras estacoes ficam em 110-150). ponytail: ajuste de pos-render; se o modelo mudar, re-renderizar.
"""

import argparse
import math

from PIL import Image, ImageChops, ImageStat


def clarear(im, v, sat):
    a = im.split()[3]
    h, s, val = im.convert("RGB").convert("HSV").split()
    val = val.point([round(255 * (i / 255) ** v) for i in range(256)])
    s = s.point([min(255, round(i * sat)) for i in range(256)])
    out = Image.merge("HSV", (h, s, val)).convert("RGB").convert("RGBA")
    out.putalpha(a)
    return out


def brilho(im, x, y, raio, cor, forca):
    w, h = im.size
    r, g, b = (int(cor[i : i + 2], 16) for i in (0, 2, 4))
    camada = Image.new("RGB", (w, h), (0, 0, 0))
    px = camada.load()
    for j in range(max(0, y - raio), min(h, y + raio + 1)):
        for i in range(max(0, x - raio), min(w, x + raio + 1)):
            d = math.hypot(i - x, j - y) / raio
            if d < 1:
                k = forca * (1 - d) ** 2
                px[i, j] = (round(r * k), round(g * k), round(b * k))
    rgb = ImageChops.screen(im.convert("RGB"), camada).convert("RGBA")
    rgb.putalpha(im.split()[3])
    return rgb


def lum(im):
    mask = im.split()[3].point(lambda v: 255 if v > 128 else 0)
    return ImageStat.Stat(im.convert("L"), mask=mask).mean[0]


def main():
    p = argparse.ArgumentParser()
    p.add_argument("png")
    p.add_argument("--v", type=float, default=0.62)
    p.add_argument("--sat", type=float, default=1.35)
    p.add_argument("--brilho", help="x,y,raio,cor_hex,forca")
    p.add_argument("--saida", help="padrao: sobrescreve o png")
    a = p.parse_args()
    im = Image.open(a.png).convert("RGBA")
    antes = lum(im)
    im = clarear(im, a.v, a.sat)
    if a.brilho:
        x, y, raio, cor, forca = a.brilho.split(",")
        im = brilho(im, int(x), int(y), int(raio), cor, float(forca))
    im.save(a.saida or a.png)
    print("lum media %.0f -> %.0f" % (antes, lum(im)))


if __name__ == "__main__":
    main()
