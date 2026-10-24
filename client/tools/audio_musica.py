"""As 5 musicas (CC0, RandomMind no OpenGameArt; docs/AUDIO.md §10, PROVENIENCIA.md §24) no lugar da musica sintetizada: decodifica,
corta o silencio das pontas, normaliza o RMS medio da faixa ao da Sintese.Musica (-18 dBFS, para o MusicaVol 0,18 continuar dando os
-33 dB do AUDIO.md §2) com um joelho suave nos picos e grava Resources/Musica/<trilha>.ogg (Vorbis estereo 44 100 Hz).

uso: python client/tools/audio_musica.py <pasta com os 5 mp3 baixados> [--saida client/Assets/_FS/Resources/Musica]
Precisa do ffmpeg (com libvorbis) no PATH. Imprime o SHA-256 da fonte e do .ogg, a duracao, o RMS e o pico para o PROVENIENCIA.
"""

import argparse
import array
import hashlib
import math
import os
import subprocess

TAXA = 44100
ALVO = -18.0  # dBFS: RMS medio da Sintese.Musica (0,18 x -18 = -33 dB, AUDIO.md §2)
JOELHO, TETO = 0.7, 0.95  # acima de JOELHO o pico entra no tanh e nunca passa de TETO
# trilha (Mixagem.Trilhas): arquivo baixado da pagina do OpenGameArt (versao completa, nao o loop: toca 1 volta e pausa)
MAPA = {
    "main": "The_Old_Tower_Inn.mp3",
    "workshop": "Market_Day.mp3",
    "royal": "Kings_Feast_0.mp3",
    "arcane": "Rising_Moon_0.mp3",
    "event": "Rejoicing_0.mp3",
}


def decodifica(caminho):
    cmd = ["ffmpeg", "-v", "error", "-i", caminho, "-ac", "2", "-ar", str(TAXA), "-f", "s16le", "-"]
    a = array.array("h")
    a.frombytes(subprocess.run(cmd, check=True, capture_output=True).stdout)
    return [v / 32768.0 for v in a]


def corta(x, limiar=0.003):
    """Silencio das pontas fora (amostras intercaladas: corta em pares para nao trocar os canais)."""
    i = next((k for k, v in enumerate(x) if abs(v) > limiar), 0) & ~1
    j = next((k for k in range(len(x) - 1, -1, -1) if abs(x[k]) > limiar), len(x) - 1) | 1
    return x[i : j + 1]


def rms(x):
    return 10 * math.log10(max(sum(v * v for v in x) / max(1, len(x)), 1e-12))


def joelho(v):
    a = abs(v)
    if a <= JOELHO:
        return v
    return math.copysign(JOELHO + (TETO - JOELHO) * math.tanh((a - JOELHO) / (TETO - JOELHO)), v)


def normaliza(x):
    """Ganho ate o RMS medio ficar no ALVO depois do joelho (3 passes: o joelho tira um pouco de energia)."""
    g, y = ALVO - rms(x), x
    for _ in range(3):
        y = [joelho(v * 10 ** (g / 20)) for v in x]
        g += ALVO - rms(y)
    return y


def fades(x, s=0.02):
    """20 ms de entrada e saida: sem estalo no corte."""
    n = int(TAXA * s) * 2
    for k in range(min(n, len(x) // 2)):
        w = (k // 2) / (n // 2)
        x[k] *= w
        x[-1 - k] *= w
    return x


def grava(x, caminho):
    os.makedirs(os.path.dirname(caminho), exist_ok=True)
    pcm = array.array("h", (max(-32767, min(32767, round(v * 32767))) for v in x))
    cmd = ["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(TAXA), "-ac", "2", "-i", "-",
           "-map_metadata", "-1", "-c:a", "libvorbis", "-q:a", "4", caminho]
    subprocess.run(cmd, input=pcm.tobytes(), check=True)


def sha(caminho):
    return hashlib.sha256(open(caminho, "rb").read()).hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument("fonte")
    p.add_argument("--saida", default="client/Assets/_FS/Resources/Musica")
    a = p.parse_args()
    for trilha, arquivo in MAPA.items():
        orig = os.path.join(a.fonte, arquivo)
        dest = os.path.join(a.saida, trilha + ".ogg")
        grava(fades(normaliza(corta(decodifica(orig)))), dest)
        y = decodifica(dest)  # mede o que o jogo vai tocar (depois do Vorbis)
        print("%s <- %s (%d bytes, %s) | %.1f s | RMS %.1f dBFS (alvo %.1f) | pico %.2f | %s.ogg %d bytes, %s"
              % (trilha, arquivo, os.path.getsize(orig), sha(orig), len(y) / 2 / TAXA, rms(y), ALVO,
                 max(abs(v) for v in y), trilha, os.path.getsize(dest), sha(dest)))


if __name__ == "__main__":
    main()
