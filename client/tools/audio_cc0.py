"""Efeitos reais CC0 (Kenney) no lugar dos sintetizados: decodifica, mono 22 050 Hz, corta o silencio e normaliza ao RMS da
janela de 50 ms mais forte do som que substitui (docs/AUDIO.md §2), para a Mixagem continuar valendo sem recalibrar.

uso: python client/tools/audio_cc0.py <pasta com os zips da Kenney extraidos> [--saida client/Assets/_FS/Resources/Audio]
Precisa do ffmpeg no PATH. Grava <saida>/<som>/<som>_<k>.wav (16 bits) e imprime o SHA-256 de cada um para o PROVENIENCIA.
"""

import argparse
import array
import hashlib
import math
import os
import subprocess
import wave

TAXA = 22050
# som da Sintese: (alvo em dBFS = RMS 50 ms do clip sintetizado, AUDIO.md §2; arquivos CC0 da Kenney, um por variacao)
MAPA = {
    "hammer": (
        -11.2,
        ["kenney_impact-sounds/Audio/impactMetal_heavy_00%d.ogg" % k for k in range(3)],
    ),
    "martelo": (
        -11.2,
        [
            "kenney_impact-sounds/Audio/impactMetal_medium_00%d.ogg" % k
            for k in range(3)
        ],
    ),
    "ore": (
        -14.1,
        ["kenney_impact-sounds/Audio/impactMining_00%d.ogg" % k for k in range(2)],
    ),
    "drop": (
        -12.2,
        ["kenney_impact-sounds/Audio/impactWood_light_00%d.ogg" % k for k in range(2)],
    ),
    "esteira": (
        -15.2,
        ["kenney_impact-sounds/Audio/impactPlate_light_00%d.ogg" % k for k in range(2)],
    ),
    "click": (-19.0, ["kenney_interface-sounds/Audio/click_002.ogg"]),
    "coin": (
        -13.5,
        [
            "kenney_rpg-audio/Audio/handleCoins.ogg",
            "kenney_rpg-audio/Audio/handleCoins2.ogg",
        ],
    ),
    # No Placeholder Gate (2026-10-24): os 8 que ainda tocavam a Sintese
    "furnace": (-11.0, ["kenney_impact-sounds/Audio/impactPlate_heavy_000.ogg"]),
    "client": (-11.7, ["kenney_impact-sounds/Audio/impactBell_heavy_002.ogg"]),
    "leave": (-12.0, ["kenney_rpg-audio/Audio/doorClose_2.ogg"]),
    "upgrade": (-12.2, ["kenney_music-jingles/Audio/Pizzicato jingles/jingles_PIZZI10.ogg"]),
    "hire": (-12.1, ["kenney_music-jingles/Audio/Pizzicato jingles/jingles_PIZZI04.ogg"]),
    "order": (-11.6, ["kenney_music-jingles/Audio/Pizzicato jingles/jingles_PIZZI01.ogg"]),
    "vip": (-10.7, ["kenney_music-jingles/Audio/Steel jingles/jingles_STEEL00.ogg"]),
    "offline": (-12.7, ["kenney_music-jingles/Audio/Steel jingles/jingles_STEEL04.ogg"]),
}


def decodifica(caminho):
    cmd = [
        "ffmpeg",
        "-v",
        "error",
        "-i",
        caminho,
        "-ac",
        "1",
        "-ar",
        str(TAXA),
        "-f",
        "s16le",
        "-",
    ]
    raw = subprocess.run(cmd, check=True, capture_output=True).stdout
    a = array.array("h")
    a.frombytes(raw)
    return [v / 32768.0 for v in a]


def corta(x, limiar=0.01):
    i = next((k for k, v in enumerate(x) if abs(v) > limiar), 0)
    j = next((k for k in range(len(x) - 1, -1, -1) if abs(x[k]) > limiar), len(x) - 1)
    return x[max(0, i - 40) : j + 1]


def rms50(x):
    """RMS da janela de 50 ms mais forte (passo de 5 ms), em dBFS."""
    n, passo = int(TAXA * 0.05), int(TAXA * 0.005)
    melhor = 0.0
    for i in range(0, max(1, len(x) - n + 1), passo):
        j = x[i : i + n]
        melhor = max(melhor, sum(v * v for v in j) / len(j))
    return 10 * math.log10(max(melhor, 1e-12))


def limita(x, g):
    """Ganho g com limitador suave (tanh): o transiente do impacto satura em vez de clipar."""
    return [0.98 * math.tanh(v * g / 0.98) for v in x]


def normaliza(x, alvo):
    """Busca (bissecao em dB) o ganho que poe o RMS 50 ms no alvo depois do limitador; o teto e' +24 dB."""
    lo, hi = -40.0, 24.0
    for _ in range(30):
        mid = (lo + hi) / 2
        if rms50(limita(x, 10 ** (mid / 20))) < alvo:
            lo = mid
        else:
            hi = mid
    return limita(x, 10 ** (lo / 20))


def grava(x, caminho):
    os.makedirs(os.path.dirname(caminho), exist_ok=True)
    pcm = array.array("h", (max(-32767, min(32767, round(v * 32767))) for v in x))
    with wave.open(caminho, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(TAXA)
        w.writeframes(pcm.tobytes())


def main():
    p = argparse.ArgumentParser()
    p.add_argument("fonte")
    p.add_argument("--saida", default="client/Assets/_FS/Resources/Audio")
    a = p.parse_args()
    for som, (alvo, arquivos) in MAPA.items():
        for k, rel in enumerate(arquivos):
            x = corta(decodifica(os.path.join(a.fonte, rel)))
            x = normaliza(x, alvo)
            dest = os.path.join(a.saida, som, "%s_%d.wav" % (som, k))
            grava(x, dest)
            sha = hashlib.sha256(open(dest, "rb").read()).hexdigest()
            print(
                "%s %s <- %s | %.2f s | %.1f dBFS (alvo %.1f) | %s"
                % (som, k, rel, len(x) / TAXA, rms50(x), alvo, sha)
            )


if __name__ == "__main__":
    main()
