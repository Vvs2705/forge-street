"""loja_screenshots.py - Forge Street, G-UA-03 (2026-10-16): as 8 screenshots da loja do docs/ASO.md §7, em pt/en/es, tiradas do jogo real.

Uso (raiz do projeto; Python 3 + Pillow + numpy; um processo pesado por vez):
  1) build sem a marca "Development Build": Unity -batchmode -quit -executeMethod FS.EditorTools.Setup.BuildWindowsLoja (client/Builds/win_loja)
  2) python client/tools/loja_screenshots.py capturar [--so <preset>] [--lang pt]   # grava cada preset e guarda so o quadro escolhido
  3) python client/tools/loja_screenshots.py compor                                # faixa da legenda + quadro com moldura
Quadros escolhidos: client/Builds/loja/quadros/<lang>/<n>_<preset>.jpg (fora do git; mesmo comando, mesmo quadro: CRIATIVOS.md).
Saida: arte/loja/screenshots/<lang>/<n>_<preset>.jpg (1080 x 1920, JPG 92, 9:16 como a Play pede).
"""
import os, sys, shutil, subprocess, argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import marca_compor as M   # radial (degrade) e vinheta, os mesmos do icone

RAIZ = M.RAIZ
EXE = os.path.join(RAIZ, "client", "Builds", "win_loja", "ForgeStreet.exe")
TMP = os.path.join(RAIZ, "client", "Builds", "loja", "gravacao")
QUADROS = os.path.join(RAIZ, "client", "Builds", "loja", "quadros")
SAI = os.path.join(RAIZ, "arte", "loja", "screenshots")
FONTE = os.path.join(RAIZ, "client", "Assets", "_FS", "Resources", "Fonts", "LilitaOne-Regular.ttf")
LANGS = ["pt", "en", "es"]
FUNDO = [(0.0, 0x34405A), (0.55, 0x222A3B), (1.0, 0x121620)]   # ardosia do icone A3
BRASA = [(0.0, 0x8A4A1E), (0.5, 0x3A2418), (1.0, 0x222A3B)]

# (n, preset, quadro escolhido a 30 fps, legenda pt, en, es) - ASO.md §7
PECAS = [
    (1, "full_district", 449, "DA BIGORNA À OFICINA CHEIA", "FROM ONE ANVIL TO A FULL SHOP", "DE UN YUNQUE A UN TALLER LLENO"),
    (2, "conveyor", 180, "AUTOMATIZE A PRODUÇÃO", "AUTOMATE PRODUCTION", "AUTOMATIZA LA PRODUCCIÓN"),
    (3, "first_worker", 150, "CONTRATE AJUDANTES", "HIRE HELPERS", "CONTRATA AYUDANTES"),
    (4, "huge_queue", 30, "FILA CHEIA? ACHE O GARGALO", "FULL LINE? FIND THE BOTTLENECK", "¿COLA LLENA? BUSCA EL ATASCO"),
    (5, "vip", 420, "CLIENTES VIP PAGAM 3×", "VIP CUSTOMERS PAY 3×", "LOS VIP PAGAN 3×"),
    (6, "order", 510, "CUMPRA ENCOMENDAS", "COMPLETE ORDERS", "CUMPLE PEDIDOS"),
    (7, "district_unlock", 200, "ABRA UMA NOVA RUA", "OPEN A NEW STREET", "ABRE UNA CALLE NUEVA"),
    (8, "offline", 60, "RENDE ATÉ 2 H FORA DO JOGO", "EARNS UP TO 2 H WHILE AWAY", "GANA HASTA 2 H SIN JUGAR"),
]


def capturar(so, langs, candidatos):
    """Roda o preset (o -record ja liga o -testsession) e copia so o quadro escolhido; com --candidatos, 1 a cada 60 para escolher."""
    for n, preset, q, *_ in PECAS:
        if so and preset not in so:
            continue
        for lang in langs:
            pasta = os.path.join(TMP, preset + "_" + lang)
            shutil.rmtree(pasta, ignore_errors=True)
            os.makedirs(pasta)
            log = os.path.join(TMP, preset + "_" + lang + ".log")
            r = subprocess.run([EXE, "-screen-fullscreen", "0", "-screen-width", "540", "-screen-height", "960",
                                "-preset", preset, "-lang", lang, "-record", pasta, "-logFile", log], timeout=900)
            ok = r.returncode == 0 and "PRESET %s ok" % preset in open(log, encoding="utf-8", errors="replace").read()
            dest = os.path.join(QUADROS, lang)
            os.makedirs(dest, exist_ok=True)
            fs = sorted(os.listdir(pasta))
            esc = [q] if not candidatos else list(range(0, len(fs), 60))
            for k in esc:
                src = os.path.join(pasta, "f%05d.jpg" % k)
                if os.path.exists(src):
                    shutil.copy(src, os.path.join(dest, "%d_%s%s.jpg" % (n, preset, "" if not candidatos else "_f%05d" % k)))
            print("%s %s rc=%d ok=%s quadros=%d" % (preset, lang, r.returncode, ok, len(fs)), flush=True)
            shutil.rmtree(pasta, ignore_errors=True)   # ~0,7 MB por quadro: so o escolhido fica


def legenda(d, texto, cx, y, larg):
    """Lilita One creme com contorno escuro e sombra, centrada; desce o tamanho ate caber em `larg` (1 ou 2 linhas)."""
    for tam in range(112, 60, -4):
        f = ImageFont.truetype(FONTE, tam)
        linhas = [texto] if d.textlength(texto, font=f) <= larg else quebra(d, texto, f, larg)
        if linhas and all(d.textlength(l, font=f) <= larg for l in linhas) and len(linhas) <= 2:
            break
    alt = tam * 1.08
    y0 = y - alt * len(linhas) / 2
    for i, l in enumerate(linhas):
        w = d.textlength(l, font=f)
        p = (cx - w / 2, y0 + i * alt)
        d.text((p[0], p[1] + 9), l, font=f, fill=(0, 0, 0, 150), stroke_width=9, stroke_fill=(0, 0, 0, 150))
        d.text(p, l, font=f, fill=(255, 236, 196, 255), stroke_width=8, stroke_fill=(30, 22, 18, 255))


def quebra(d, texto, f, larg):
    ps = texto.split()
    for k in range(len(ps) - 1, 0, -1):
        a, b = " ".join(ps[:k]), " ".join(ps[k:])
        if d.textlength(a, font=f) <= larg and d.textlength(b, font=f) <= larg:
            return [a, b]
    return None


def compor_uma(quadro, texto):
    W, H, TOPO = 1080, 1920, 340
    c = M.radial(W, H, W * 0.5, H * 0.3, H * 0.9, FUNDO).convert("RGBA")
    b = M.radial(W, H, W * 0.5, TOPO * 0.5, W * 0.7, BRASA).convert("RGBA")   # brasa so na faixa da legenda
    yy = np.linspace(0, 1, H)[:, None] * np.ones((1, W))
    b.putalpha(Image.fromarray((np.clip(1 - yy / 0.35, 0, 1) * 170).astype(np.uint8)))
    c.alpha_composite(b)
    # quadro do jogo: 86% da largura, canto de 36 px, aro dourado de 6 px e sombra
    q = Image.open(quadro).convert("RGBA")
    s = min(W * 0.86 / q.width, (H - TOPO - 50) / q.height)
    q = q.resize((round(q.width * s), round(q.height * s)), Image.LANCZOS)
    x, y = (W - q.width) // 2, TOPO + 10
    m = Image.new("L", q.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, q.width - 1, q.height - 1), 36, fill=255)
    sombra = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(sombra).rounded_rectangle((x - 6, y + 4, x + q.width + 6, y + q.height + 18), 42, fill=(0, 0, 0, 170))
    c.alpha_composite(sombra.filter(ImageFilter.GaussianBlur(14)))
    aro = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(aro).rounded_rectangle((x - 6, y - 6, x + q.width + 5, y + q.height + 5), 42, fill=(224, 178, 58, 255))
    c.alpha_composite(aro)
    c.paste(q, (x, y), m)
    d = ImageDraw.Draw(c)
    legenda(d, texto, W / 2, TOPO / 2 + 4, W - 110)
    M.vinheta(c, 0.25)
    return c.convert("RGB")


def compor():
    for n, preset, q, pt, en, es in PECAS:
        for lang, texto in zip(LANGS, (pt, en, es)):
            src = os.path.join(QUADROS, lang, "%d_%s.jpg" % (n, preset))
            if not os.path.exists(src):
                print("falta", src)
                continue
            out = os.path.join(SAI, lang)
            os.makedirs(out, exist_ok=True)
            compor_uma(src, texto).save(os.path.join(out, "%d_%s.jpg" % (n, preset)), quality=92, optimize=True)
    print("COMPOR_OK")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("acao", choices=["capturar", "compor"])
    ap.add_argument("--so", action="append")
    ap.add_argument("--lang", action="append")
    ap.add_argument("--candidatos", action="store_true", help="1 quadro a cada 60 (2 s) para escolher o da peca")
    a = ap.parse_args()
    if a.acao == "capturar":
        capturar(a.so, a.lang or LANGS, a.candidatos)
    else:
        compor()
