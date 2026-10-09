# Backlog da v0.7 — Forge Street (proposta, 2026-10-09)

Estado: **PROPOSTO**. Nada daqui está implementado na `feat/v0.6-juice`. A ordem final depende do teste da v0.5.1 no POCO F4 (`ROTEIRO_TESTE_POCO.md`) e do playtest Camada 0. Regra do projeto: não ampliar conteúdo antes de medir com gente.

## 1. Decisões do Vinicius (destravam itens abaixo)
| # | Decisão | Dado que temos | Se "sim" |
|---|---|---|---|
| D1 | Merge do PR #4 (v0.5.1) e depois PR da v0.6 | 0.5.1: 81 testes, 3 revisões de código, emulador; v0.6: 87 testes, 3 revisões, emulador | tag v0.5.1 + Release; PR da v0.6 com base na `main` |
| D2 | Câmera: oficina inteira (11,4 m) ou mais perto (`-cam 9`/`-cam 8`) | fotos `validation_v051/shots/02_cam9.png`, `04_android_cam9_bandeja.png`; a 9 m os personagens ficam ~25% maiores, mas a fila pode ir para baixo da HUD com o ferreiro na forja | mudar `VisibleWidth` e ajustar o clamp vertical para a fila nunca sumir |
| D3 | Input "Both" (voltar funciona) ou reserva só Input System | emulador: toque, joystick e voltar ok com "Both"; FPS só no POCO | se pesar: ponto de entrada Activity em vez de GameActivity e voltar pelo Input System |
| D4 | Moeda física no balcão (B4) | `BALANCE.md` §21: −4% de receita, +45–65 s na produção completa, caminhada à toa 20% → 5–13% (vira ida à pilha) | View da pilha (moedas empilhando, ímã, som), save da pilha, dica "Recolha as moedas" (~1 dia) |
| D5 | Cofre "Pegar 2× ▶" (mais um rewarded) | padrão do gênero (revisão de UX); o opt-in do GDD §15 mede ≥ 45% | botão no popup do cofre + placement `cofre` no LevelPlay |
| D6 | Botões de anúncio só depois da 1ª melhoria (~2 min) | revisão de UX: primeira sessão mais limpa × menos visualizações no começo | esconder VIP/Velocidade até `UpgradesBought > 0` |
| D7 | Uma paleta só na HUD (marrom/creme) | revisão de UX: hoje mistura azul-marinho e creme | trocar a pílula da dica e o botão Melhorias para #2A1E14/#F2D9A0 |
| D8 | Baixar fonte OFL (Lilita One ou Fredoka) para a HUD | `BENCHMARK_VISUAL.md` P1-1; baixar arquivo pede aval | embutir a fonte e trocar Arial negrito |
| D9 | MP4 dos criativos v0.6 (~35 MB) no git ou fora | os antigos estão versionados desde o PR #3 | commitar ou ignorar `creatives/v06/*.mp4` |

## 2. Candidatos sem decisão pendente (ordem sugerida)
| Prio | Item | Por quê | Esforço |
|---|---|---|---|
| 1 | **Corrigir o que o POCO mostrar** (FPS com 16 clientes + juice, leitura a 1080×2400, vibração de 15 ms perceptível?) | é o único dado de aparelho real | ? |
| 2 | **Troca de loja / distrito (B7)** depois da produção completa (~42 min) | loop de longo prazo do gênero; hoje o ouro fica sem destino depois do luxo | G (semana, arte nova) — só depois do playtest |
| 3 | **Encomendas: 2º tipo** ("compre uma melhoria", "atenda o VIP") e encomenda "do Rei" com prêmio maior | variedade barata em cima do sistema da FASE9 | P–M |
| 4 | **Login diário / oferta diária (B10)** | D1/D3 do GDD §8; só faz sentido perto do soft launch | P |
| 5 | **Fome da Bigorna 2** (2 582 s de fome em 60 min no bot) | estação comprada parada parece defeito; a demanda de espada não cobre 2 bigornas no fim | P (estudo) / M (mudança de layout ou da regra dos ajudantes) |
| 6 | **Sobras das revisões**: quantidade do VIP fora da 1ª vaga ainda em texto; "+N" do VIP e da venda juntos; coroa sobre o balão | polimento | P |
| 7 | **Criativos #3–#7** (GDD §17) com o build v0.6 | teste de CPI precisa de 8 | M |

## 3. Medido nesta rodada (referência)
- Bot humano 60 min, v0.6: produção completa 41:31; Encomendas 21 entregues (5,0% da receita); VIP 5,5%.
- Ocioso 15 min depois de 25 min de bot: 209 vendas (era 80 antes da correção do Ajudante 3).
- Android (emulador API 35): Encomendas, vibração (`dumpsys vibrator_manager`), voltar, joystick e `-cam` funcionando; 0 exceção no logcat.
