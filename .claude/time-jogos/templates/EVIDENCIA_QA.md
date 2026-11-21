# Evidência de QA — {{feature}} ({{AAAA-MM-DD}})

Salvar em `production/qa/evidence/{{AAAA-MM-DD}}-{{slug}}.md` com os arquivos ao lado.

| Campo | Valor |
|---|---|
| Commit / PR | `{{sha}}` / #{{n}} |
| Build | {{web | Unity Editor | Android APK | Windows}} · versão {{...}} |
| Dispositivo / perfil | {{ex.: PC i5 + GTX; perfil mobile médio (Android 12, 4 GB RAM, Adreno 610)}} |
| Comando de teste | `{{comando exato}}` |
| Resultado | {{N passou / M falhou}} — log: `{{arquivo.log}}` |
| FPS medido | média {{}} · p1 {{}} · método {{Profiler, stats overlay}} |
| Screenshot / vídeo | `{{arquivo.png}}` |
| Observado por | {{agente}} — o que foi visto na tela (1–3 linhas) |
| Originalidade | aprovada por `ip-originality-reviewer` em {{data}} / n.a. |
| Segurança | Portão acionado? sim/não — resultado |

## Passos reproduzíveis
1. …

## Defeitos encontrados
| # | Severidade | Descrição | Status |
|---|---|---|---|
