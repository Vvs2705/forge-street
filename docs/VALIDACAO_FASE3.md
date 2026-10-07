# Forge Street — validação da fase 3 / v0.3.0 (2026-10-07)

## RESUMO
Fase 3 "luxo visual" (contrato `FASE3_LUXO.md`) fechada no portão local. O Codex abriu a leva e implementou o core (15:12); o Claude retomou às ~15:35, verificou tudo, completou a view (decorações), a medição de 90 min, o diagnóstico/A-B de logística, decidiu os preços e rodou o portão Unity. Sem aparelho Android, PlayMode ou playtest humano.

## VERIFICAÇÃO DO QUE O CODEX DEIXOU (ao retomar)
| Item | Achado |
|---|---|
| Fase 2 (Passos A–C do handoff) | confirmada: custos 2 200/5 500/9 000, `PriceOf` → joia 80 com a Vitrine, EditMode 37/37, APK 0.2.0 (SHA conferido = `source_and_apk.sha256`), fotos 01–04 ok |
| Fase 3 | core já mudado DEPOIS do portão da fase 2 (Defs/Sim/testes 15:12–15:15, versão 0.3.0), mas STATUS/VALIDACAO_FASE2 diziam "Leva 8 não aberta"; view sem nada; sem medição — corrigido no STATUS e completado aqui |

## ENTREGAS
- View (`WorldView.cs`, `Art.cs`): Fachada nobre (soleira + postes dourados + varal de 18 bandeirolas nas cores dos itens), Piso de oficina (ladrilhos 1 m numa textura única), Joalheria real (tapete roxo com borda dourada + 4 pedestais de gema); aparecem pela flag (save/compra) com pop de 0,3 s.
- Economia (`BalanceTests.Bot_90Minutos_Luxo`, `BALANCE.md` §11, `FASE3_LUXO.md`): preços vigentes **11 000 / 14 000 / 18 000** (decisão do coordenador sobre a medição; 6 000/10 000/16 000 viraram histórico).
- `ESTUDO_LOGISTICA.md`: fome da joalheria = falta de lingote na fonte; nenhuma variante proposta vence; próximo A/B = 2º ajudante de minério.

## TESTES E EVIDÊNCIAS (`client/Builds/validation_phase3/`)
| Verificação | Resultado |
|---|---|
| `dotnet test client/tools/coretests` | 43/43 |
| viewcheck (`client/tools/viewcheck/view`) | 0 erros / 0 avisos |
| Unity EditMode | 43/43 (`editmode.xml`) |
| Unity Windows | Succeeded, 0 erros (`build_win.log`) |
| Autoplay 3 min | OK (`autoplay.log`) |
| Unity Android dev (IL2CPP ARM64) | Succeeded, 0 erros; APK 46 905 964 B, 07/10 15:59, SHA-256 `f3849f14e1fed164b0f1fdd8ab9702dc2b0481a2ae38d66b5a039842b985bc45` |
| Fotos (`-testsession`, save real intocado) | `20_fachada_antes`, `21_fachada_depois`, `22_piso`, `23_joalheria_real`, `24_compra_ao_vivo` |

## NÚMEROS (bot humano, 90 min)
Produção completa 40:41 (igual com e sem luxo) → Fachada 50:45 · Piso 63:46 · Joalheria real 79:55. Ouro/min com × sem luxo +0,7%, vendas −0,5% (ruído; sem bônus). Retorno com cofre no teto (18 000) após a produção completa: compra só a Fachada.

## RISCOS
- Rótulo "Balcão" encosta no varal da fachada; rua de cima muito cheia no fim de jogo (só a foto/playtest decide se incomoda).
- Bot comprar luxo não prova interesse humano: validar com pessoas antes de mais conteúdo cosmético.
- ~~IDs ≥ 20 são tratados como luxo (`ProductionCount`): marcar produtivo/luxo por upgrade antes de anexar um produtivo novo.~~ **RESOLVIDA na Leva 9:** a marca agora é por upgrade (`UpgradeDef.Luxury`, BALANCE §12). Na Leva 10 os produtivos 23/24 (Mineiro, Joalheiro 2) entraram depois dos luxos (BALANCE §14).
- Android real (toque, ASTC, FPS, memória) ainda não validado.

## PRÓXIMO PASSO
APK 0.3.0 em aparelho + playtest; A/B do 2º ajudante de minério (`ESTUDO_LOGISTICA.md`).
