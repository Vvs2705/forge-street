# Soak test (estabilidade de memória em sessão longa)

**Critério:**
- sem crescimento de memória acima de **10%** depois dos 10 primeiros minutos;
- **0 exceção**;
- o jogo vivo até o fim.

A memória é o PSS do processo e o heap mono da Unity, comparando a mediana dos minutos 10–14 com a dos 5 últimos.

## Como rodar

```bash
bash client/tools/soak_emulador.sh <pasta de saída> 60
```

- **Pré-requisito:** o APK do emulador já construído (`Setup.BuildAndroidEmu` ou `verify.sh android-emulator`) e nenhum processo pesado aberto.
- **Na partida:** liga o AVD `fs_base` sem Vulkan, instala do zero e abre com `-bot -speed 4 -soak`.
- **A cada minuto:**
  - `dumpsys meminfo` (PSS);
  - a linha `SOAK` que o jogo põe no logcat com `-soak`: fps do minuto real, heap mono, memória reservada da Unity e GCs.
- **No fim:** `soak_relatorio.py` junta tudo em `soak.csv`, conta exceção, ANR e crash do pacote no logcat, e dá APROVADO ou REPROVADO (sai 1).

**Por que não o `-autoplay`:** ele é um laço de simulação sem render, que termina em segundos e não carrega a cena; não mede a memória do jogo. O `-bot` joga o jogo de verdade, com a cena, as views e o áudio.

## Resultado 2026-10-25 (emulador, `main` de 2026-10-24 + `-soak`)

- **Aparelho:** AVD `fs_base` (Pixel 7, Android 15 x86_64, 2 GB), GLES 3 pela GPU do host (RTX 3050), build Development.
- **Partida:** instalação nova, `-bot -speed 4` por 60 min de relógio.
- **Dados minuto a minuto:** [`docs/soak/2026-10-25_emulador_gles.csv`](soak/2026-10-25_emulador_gles.csv).

| min | PSS (MB) | fps | heap mono (MB) | reservado (MB) | GCs acumulados |
|---|---|---|---|---|---|
| 1 | 359 | 51,8 | 23,1 | 274,0 | 38 |
| 10 | 385 | 45,1 | 24,6 | 275,7 | 204 |
| 20 | 391 | 41,9 | 25,4 | 277,7 | 382 |
| 30 | 389 | 28,5 | 25,7 | 277,7 | 511 |
| 40 | 394 | 36,7 | 26,3 | 277,7 | 624 |
| 50 | 386 | 29,0 | 25,5 | 277,7 | 738 |
| 60 | 391 | 34,6 | 25,8 | 277,7 | 853 |

**APROVADO.**
- **Crescimento depois do minuto 10:** PSS +1,9% e heap mono +2,4% (medianas de janela).
- **Pico:** PSS 398 MB.
- **Memória reservada da Unity:** parada em 277,7 MB desde o minuto 20.
- **Exceções:** 0.
- **O jogo:** vivo até o fim.

Os primeiros 10 minutos sobem de 359 para 385 MB: é a partida abrindo conteúdo (melhorias, linhas, clientes). Depois disso a memória fica num patamar.

**Achados (sem correção neste PR):**
1. **O fps cai de ~52 para ~35 ao longo da hora,** com a memória parada.
   - Não é vazamento: a partida a 4× chega ao fim do Old Forge com muito mais estações, clientes e itens na tela do que no minuto 1.
   - O GLES do emulador é tradução pela GPU do host, então o número absoluto não vale para o aparelho.
   - Medir a mesma curva no POCO (Vulkan, Adreno) antes de concluir algo. Se cair lá também, o próximo passo é perfilar o custo por estação e cliente.
2. **~14 GCs por minuto** (853 em 60 min) com o heap mono em ~25 MB: há alocação por quadro.
   - Não pesa na memória, mas cada GC é um soluço em potencial.
   - Candidato a perfil (`Profiler` com Deep Profile) quando o fps do POCO pedir.
3. **O Vulkan do emulador derruba o build Development.**
   - No 1º soak, SIGSEGV no minuto 8 dentro do `vulkan.ranchu.so`, chamado pelo `vk::Image::SetLabel` (a marcação de debug que só o build Development faz no Vulkan), na thread `UnityGfxDeviceWorker`.
   - É bug do driver do emulador, não do jogo. Até o minuto 7 a memória estava estável: PSS 320 → 332 MB.
   - Por isso o script liga o emulador com `-feature -Vulkan` (troque por `EMU_ARGS=...`).
   - O Vulkan de verdade se mede no POCO.
4. **O AVD `fs_playstore` segue sem boot** (ROTEIRO_TESTE_POCO.md §6), então o script usa o `fs_base`.

**No POCO (quando liberado):** o mesmo critério, com o `medir_aparelho.sh --temp-serie 60` e o build `-soak`, para fechar o fps e o térmico no Vulkan real.
