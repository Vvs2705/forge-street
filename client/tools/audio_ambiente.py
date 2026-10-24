"""Ambiente em camadas (AUDIO.md §11): os lacos CC0 do OpenGameArt viram mono 22 050 Hz, sem emenda (as pontas cruzam em 1 s), no nivel
RMS medio pedido (o do laco sintetizado que substituem, para a Mixagem continuar valendo).

uso: python client/tools/audio_ambiente.py <pasta com os arquivos baixados> [--saida client/Assets/_FS/Resources/Audio]
Precisa do ffmpeg no PATH. Grava <saida>/amb_<nome>.wav (16 bits) e imprime duracao, RMS e SHA-256 para o PROVENIENCIA.
"""

import argparse
import array
import hashlib
import math
import os
import subprocess
import wave

TAXA = 22050
CRUZA = 1.0  # s de cruzamento entre o fim e o comeco do laco
# nome: (arquivo baixado, filtro do ffmpeg, RMS medio alvo em dBFS)
MAPA = {
    "fogo": (
        "fire.wav",
        "highpass=f=60",
        -13.4,  # o RMS medio do Sintese.Fogo que substitui
    ),  # PagDev, "Fireplace Sound loop" (CC0)
    "multidao": (
        "crowd_shouting.ogg",
        "lowpass=f=900,highpass=f=120",
        -18.9,  # o RMS medio do Sintese.Rua (camada nova, no nivel da rua)
    ),  # StarNinjas, "Crowd Shouting/Speaking Ambience" (CC0)
}


def decodifica(caminho, filtro):
    cmd = [
        "ffmpeg",
        "-v",
        "error",
        "-i",
        caminho,
        "-af",
        filtro,
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


def laco(x):
    n = int(CRUZA * TAXA)
    corpo = x[: len(x) - n]
    for i in range(
        n
    ):  # as ultimas n amostras entram por cima das primeiras: o fim emenda no comeco
        k = i / n
        corpo[i] = corpo[i] * math.sin(k * math.pi / 2) + x[len(x) - n + i] * math.cos(
            k * math.pi / 2
        )
    return corpo


def rms_db(x):
    return 20 * math.log10(math.sqrt(sum(v * v for v in x) / len(x)) + 1e-12)


def grava(x, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    a = array.array("h", (max(-32767, min(32767, int(round(v * 32767)))) for v in x))
    with wave.open(dest, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(TAXA)
        w.writeframes(a.tobytes())


def main():
    p = argparse.ArgumentParser()
    p.add_argument("fonte")
    p.add_argument("--saida", default="client/Assets/_FS/Resources/Audio")
    a = p.parse_args()
    for nome, (arq, filtro, alvo) in MAPA.items():
        x = laco(decodifica(os.path.join(a.fonte, arq), filtro))
        g = 10 ** ((alvo - rms_db(x)) / 20)
        x = [
            0.95 * math.tanh(v * g / 0.95) for v in x
        ]  # limitador suave com teto de 0,95: o pico nunca clipa
        dest = os.path.join(a.saida, "amb_" + nome + ".wav")
        grava(x, dest)
        sha = hashlib.sha256(open(dest, "rb").read()).hexdigest()
        print(
            "%s <- %s | %.2f s | RMS %.1f dBFS (alvo %.1f) | %s"
            % (nome, arq, len(x) / TAXA, rms_db(x), alvo, sha)
        )


if __name__ == "__main__":
    main()
