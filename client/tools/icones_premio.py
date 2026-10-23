"""icones_premio.py - Forge Street, interface v2 (programa de arte, etapa 4, 2026-10-16, 0 credito): icones pintados por tipo de premio
(Renome, XP do Passe, cosmetico, projeto), gerados no Tripo (Nano Banana 2, gratis) e recortados com o GrabCut da marca v2.

Uso (raiz do projeto; Python 3 + Pillow + numpy + OpenCV): python client/tools/icones_premio.py
Entrada: arte/ui/premios/fonte_<tipo>.png (as imagens escolhidas, versionadas; opcoes.jpg = as 16 geradas).
Saida: client/Assets/_FS/Resources/UI/premio_<tipo>.png (256 x 256 RGBA, o objeto cabe em 240), lidos por Art.IconePremio.
"""
import os, sys
import numpy as np
import cv2
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import marca_v2 as V   # recorte (GrabCut + brilho como alfa) e caber

RAIZ = V.RAIZ
FONTE = os.path.join(RAIZ, "arte", "ui", "premios")
SAI = os.path.join(RAIZ, "client", "Assets", "_FS", "Resources", "UI")
TIPOS = ["renome", "passxp", "cosmetico", "blueprint"]
# auditoria item 5 (loja vazia): a arte do cartao de cada produto, pelo ID do produto (arte/ui/loja/fonte_<id>.png -> Resources/UI/<id>.png)
LOJA = ["iap_pacote_mestre", "iap_pass_s1_royal_commission", "iap_pass_s2_ember_festival"]
FONTE_LOJA = os.path.join(RAIZ, "arte", "ui", "loja")
SEM_HALO = {"passxp": 16}   # o brilho azul da estrela vinha com o fundo escuro (halo sujo no creme): fica o corpo claro + o contorno


def sem_halo(im, contorno, lum_min=90):
    """Alfa so no corpo claro (fechado e com os buracos tapados) mais `contorno` px em volta: tira o halo escuro do brilho."""
    a = np.array(im).astype(np.float32)
    corpo = ((a[..., :3].mean(axis=2) > lum_min) & (a[..., 3] > 128)).astype(np.uint8)
    corpo = cv2.morphologyEx(corpo, cv2.MORPH_CLOSE, np.ones((25, 25), np.uint8))
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * contorno + 1, 2 * contorno + 1))
    fica = cv2.GaussianBlur(cv2.dilate(corpo, k).astype(np.float32), (0, 0), 1.5)
    a[..., 3] *= fica
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def main():
    for fonte, t, nome in [(FONTE, t, "premio_" + t) for t in TIPOS] + [(FONTE_LOJA, t, t) for t in LOJA]:
        ob = V.recorte(os.path.join(fonte, "fonte_%s.png" % t), (0.06, 0.06))
        if t in SEM_HALO:
            ob = sem_halo(ob, SEM_HALO[t])
            ob = ob.crop(ob.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox())
        e = V.caber(ob, 240)
        out = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        out.alpha_composite(e, ((256 - e.width) // 2, (256 - e.height) // 2))
        out.save(os.path.join(SAI, nome + ".png"), optimize=True)
        print("%s %dx%d" % (nome, ob.width, ob.height))


if __name__ == "__main__":
    main()
