# Brief de Feature — {{nome}}

- **Jogo:** {{...}} · **Épico:** {{...}} · **Ata:** `docs/conselho/{{arquivo}}.md` · **ADR:** `{{...}}`
- **Fantasia do jogador (1 frase):** o que o jogador sente/faz
- **Pilar servido:** {{pilar do GDD}}
- **Referências estudadas (aprender, nunca clonar):** {{jogo → mecânica observada → o que faremos diferente}}

## Comportamento esperado
1. Entrada do jogador → resposta do jogo (com números: tempo, alcance, dano, custo)
2. Estados e transições
3. Casos de borda

## Orçamentos
| Item | Alvo | Medição |
|---|---|---|
| FPS (perfil mobile médio) | 60 (mín. 30 estável) | Profiler / build Android |
| Memória adicional | ≤ {{MB}} | Memory Profiler |
| Polígonos por asset | ≤ {{tris}} | `scripts/check-asset-budget.py` |
| Texturas | ≤ {{px}} / ASTC | import settings |

## Critério de aceite (testável)
- [ ] Teste automatizado: {{EditMode/PlayMode/Vitest}} `{{nome}}`
- [ ] Evidência visual: `production/qa/evidence/{{slug}}-*.png|mp4`
- [ ] Revisão de originalidade aprovada (nomes, arte, UI, textos)
- [ ] Portão de Segurança se tocar rede/backend/auth/pagamento/dados
