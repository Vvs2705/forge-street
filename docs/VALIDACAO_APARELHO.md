# Forge Street — validação em aparelho Android (2026-10-07)

## Aparelho
POCO F4 (Xiaomi, modelo 22021211RG, `munch`), Android 14, tela 1080×2400 (20:9), 440 dpi, painel 120 Hz. Conectado por USB pelo Vinicius; instalação exigiu ativar **"Instalar via USB"** nas Opções do desenvolvedor (MIUI/HyperOS recusa com `INSTALL_FAILED_USER_RESTRICTED` sem isso) e confirmar o aviso no celular.

## Build testado
APK dev 0.3.0 (IL2CPP ARM64), 46 905 964 B, SHA-256 `f3849f14e1fed164b0f1fdd8ab9702dc2b0481a2ae38d66b5a039842b985bc45` (o mesmo de `VALIDACAO_FASE3.md`). Pacote `br.com.vstack.forgestreet`, activity `com.unity3d.player.UnityPlayerGameActivity`.

## Resultados (sessão de ~1 min com o Vinicius jogando)
| Medida | Resultado | Evidência (`client/Builds/validation_device/`) |
|---|---|---|
| Abre e roda | sim, sem crash nem ANR; nenhum erro/exceção do Unity no logcat | `logcat.txt` |
| FPS (SurfaceFlinger, camada `SurfaceView(BLAST)`, 127 quadros) | **60,5 fps médio**, mediana 16,5 ms, p95 16,6 ms, pior 16,7 ms, **0 quadros > 33 ms** (vsync 120 Hz; `Application.targetFrameRate = 60`) | `latency3.txt` |
| Memória | TOTAL PSS **217 MB** (RSS 309 MB), Graphics 65 MB, EGL 41 MB | `meminfo.txt` |
| Temperatura | bateria 40,7 °C (22%, provavelmente carregando pelo USB); throttling de CPU baixo (`thermal-cpufreq` 8–13) | `battery.txt`, `thermal.txt` |
| Tela 20:9 | mundo encaixa na largura, HUD de 2 linhas legível, sobra pequena embaixo | `01_abertura.png`, `02_35s.png`, `03_60s.png`, `contato_device.png` |
| Joystick flutuante | aparece e move o ferreiro | `02_35s.png` |

## Achados para corrigir
- Rótulo do pad "Martelo veloz 1970" encosta no rótulo da estação "Bigorna" (já existia no PC).
- Rótulo "Balcão" × varal da Fachada nobre: corrigido no código depois do APK (rótulo do balcão a +0,8 m); entra no próximo build.

## Não validado ainda
Sessão longa (aquecimento/bateria em 30+ min), suspensão/retomada e cofre offline real, halo do ASTC nas bordas (não perceptível nas capturas), diagonal do joystick a 45°, som, aparelhos fracos (este é um Snapdragon 870 — topo de linha). Playtest humano estruturado continua pendente.
