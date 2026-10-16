"""marca_v2.py - Forge Street, programa de arte etapas 2-3 (2026-10-16, 0 credito): logo e icone do app a partir das opcoes
escolhidas pelo Vinicius (A3 = emblema/icone, C2 = logo de titulo), geradas no Tripo (Nano Banana 2, gratis).

Uso (raiz do projeto; Python 3 + Pillow + numpy + OpenCV; ffmpeg no PATH): python client/tools/marca_v2.py
Entrada: arte/marca/v2/fonte_emblema_a3.png e fonte_logo_c2.png (as imagens do Tripo, versionadas).
Saida:
  arte/marca/v2/emblema.png e arte/marca/logo.png (recortes RGBA, fundo transparente)
  arte/marca/icone_loja_512.png (512 RGB, o icone da loja) e arte/marca/v2/icone_leitura.png (folha de leitura)
  arte/marca/feature_graphic.png (1024x500, quadro do jogo + logo novo)
  client/Assets/_FS/Art/Marca/icone.png (512, legado/redondo/PC), icone_fundo.png e icone_frente.png (432, adaptativo)
Recorte: GrabCut (OpenCV) a partir de um retangulo + o brilho acima do fundo (o glow das letras e as faiscas viram alfa).
"""
import os, sys
import numpy as np
import cv2
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import marca_compor as M   # radial, vinheta, mascara_circulo, folha_leitura, o quadro do jogo da feature graphic

RAIZ = M.RAIZ
V2 = os.path.join(RAIZ, "arte", "marca", "v2")
SAI = os.path.join(RAIZ, "arte", "marca")
UNITY = M.UNITY
FUNDO = [(0.0, 0x34405A), (0.55, 0x222A3B), (1.0, 0x121620)]   # ardosia do proprio A3: centro mais claro, borda quase preta
BRASA = [(0.0, 0x8A4A1E), (0.5, 0x3A2418), (1.0, 0x222A3B)]    # brilho quente atras do emblema (a fornalha)


def recorte(caminho, margem, iters=6, ganho_brilho=70.0):
    """RGBA: GrabCut com o retangulo `margem` (fracao da borda) + alfa do brilho acima da cor do fundo (glow e faiscas)."""
    bgr = cv2.imread(caminho, cv2.IMREAD_COLOR)
    h, w = bgr.shape[:2]
    rect = (int(w * margem[0]), int(h * margem[1]), int(w * (1 - 2 * margem[0])), int(h * (1 - 2 * margem[1])))
    mask = np.zeros((h, w), np.uint8)
    bgd, fgd = np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64)
    cv2.grabCut(bgr, mask, rect, bgd, fgd, iters, cv2.GC_INIT_WITH_RECT)
    fg = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 1.0, 0.0).astype(np.float32)
    fg = cv2.morphologyEx(fg, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    fg = cv2.GaussianBlur(fg, (0, 0), 1.2)
    rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB).astype(np.float32)
    borda = np.concatenate([rgb[:8].reshape(-1, 3), rgb[-8:].reshape(-1, 3), rgb[:, :8].reshape(-1, 3), rgb[:, -8:].reshape(-1, 3)])
    fundo = np.median(borda, axis=0)
    luz = rgb.mean(axis=2) - fundo.mean()
    # sombra projetada da imagem original: na faixa externa do recorte (fora do miolo erodido), pixel quase da cor do fundo sai
    miolo = cv2.erode((fg > 0.5).astype(np.uint8), np.ones((15, 15), np.uint8)).astype(np.float32)
    dist = np.linalg.norm(rgb - fundo, axis=2)
    fg = np.where(miolo > 0, fg, fg * np.clip((dist - 22.0) / 26.0, 0, 1))
    brilho = np.clip((luz - 18.0) / ganho_brilho, 0, 1)   # 18 = ruido do degrade do fundo (sem isso o recorte pega a imagem inteira)
    alfa = np.maximum(fg, brilho)
    # onde so o brilho segura o alfa, tira o fundo da cor (des-premultiplica sobre a cor do fundo)
    a = np.maximum(alfa, 1e-3)[..., None]
    cor = np.where(fg[..., None] > 0.5, rgb, np.clip((rgb - fundo * (1 - a)) / a, 0, 255))
    out = np.dstack([cor, alfa * 255]).astype(np.uint8)
    im = Image.fromarray(out, "RGBA")
    return im.crop(im.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox())


def caber(im, caixa):
    s = min(caixa / im.width, caixa / im.height)
    return im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)


def fundo_icone(L):
    f = M.radial(L, L, L * 0.5, L * 0.46, L * 0.75, FUNDO)
    b = M.radial(L, L, L * 0.5, L * 0.5, L * 0.42, BRASA)
    b.putalpha(Image.fromarray((np.clip(1 - np.hypot(*np.mgrid[-1:1:L * 1j, -1:1:L * 1j]) / 0.9, 0, 1) * 160).astype(np.uint8)))
    f = f.convert("RGBA")
    f.alpha_composite(b)
    return f


def icone(emb, S=512):
    """Legado/redondo/PC e loja: o emblema ocupa ~84% do quadrado sobre o fundo ardosia com a brasa atras."""
    L = S * M.K
    f = fundo_icone(L)
    e = caber(emb, round(L * 0.84))
    f.alpha_composite(e, ((L - e.width) // 2, (L - e.height) // 2))
    M.vinheta(f, 0.30)
    return f.convert("RGB").resize((S, S), Image.LANCZOS)


def adaptativo(emb, L=432):
    """Camadas do adaptativo (108 dp; 72 dp visiveis, circulo seguro de 66 dp = 264 px em 432): o fundo sangra ate a borda e o
    emblema cabe numa caixa de 236 px: as pontas da fita (a ~3/4 da altura e na largura toda) ficam dentro do circulo de 132 px de raio
    (0,5 B na horizontal e 0,25 B abaixo do centro: B x 0,559 <= 132)."""
    T = L * M.K
    f = fundo_icone(T).convert("RGB").resize((L, L), Image.LANCZOS)
    fr = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    e = caber(emb, round(T * 236 / 432))
    fr.alpha_composite(e, ((T - e.width) // 2, (T - e.height) // 2))
    return f, fr.resize((L, L), Image.LANCZOS)


def feature_graphic(logo):
    """1024x500: o mesmo quadro do jogo da C-ART-14 sob um veu escuro, com o logo novo no centro."""
    q = os.path.join(RAIZ, "client", "Builds", "marca", "fonte", "fs01_t22.5.png")
    if not os.path.exists(q):
        import subprocess
        os.makedirs(os.path.dirname(q), exist_ok=True)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", "22.5", "-i", M.VIDEO, "-frames:v", "1", q], check=True)
    jogo = Image.open(q).convert("RGBA").crop((0, 290, 1000, 290 + 488)).resize((1024, 500), Image.LANCZOS)
    w, h = jogo.size
    yy, xx = np.mgrid[0:h, 0:w]
    a = np.clip(0.95 - np.hypot((xx - w * 0.5) / (w * 0.55), (yy - h * 0.5) / (h * 0.9)) * 0.5, 0.42, 0.88)
    veu = Image.new("RGBA", (w, h), (16, 12, 10, 0))
    veu.putalpha(Image.fromarray((a * 255).astype(np.uint8)))
    jogo.alpha_composite(veu)
    lg = caber(logo, 430)
    jogo.alpha_composite(lg, ((w - lg.width) // 2, (h - lg.height) // 2))
    return jogo.convert("RGB")


def main():
    for p in (V2, SAI, UNITY):
        os.makedirs(p, exist_ok=True)
    emb = recorte(os.path.join(V2, "fonte_emblema_a3.png"), (0.10, 0.08), ganho_brilho=90.0)
    logo = recorte(os.path.join(V2, "fonte_logo_c2.png"), (0.20, 0.12))
    emb.save(os.path.join(V2, "emblema.png"), optimize=True)
    caber(logo, 1024).save(os.path.join(SAI, "logo.png"), optimize=True)
    ic = icone(emb)
    fundo, frente = adaptativo(emb)
    ic.save(os.path.join(UNITY, "icone.png"), optimize=True)
    ic.save(os.path.join(SAI, "icone_loja_512.png"), optimize=True)
    fundo.save(os.path.join(UNITY, "icone_fundo.png"), optimize=True)
    frente.save(os.path.join(UNITY, "icone_frente.png"), optimize=True)
    M.folha_leitura({"emblema_a3": ic}, {"emblema_a3": (fundo, frente)}).save(os.path.join(V2, "icone_leitura.png"), optimize=True)
    feature_graphic(logo).save(os.path.join(SAI, "feature_graphic.png"), optimize=True)
    print("MARCA_V2_OK emblema=%dx%d logo=%dx%d" % (emb.width, emb.height, logo.width, logo.height))


if __name__ == "__main__":
    main()
