# ADR-[NNNN]: [Título]

<!-- Títulos `## Status`, `## Date`, `## Last Verified`, `## ADR Dependencies` e as linhas `**Layer**` / `**Depends On**`
     ficam em inglês de propósito: os scripts `.claude/scripts/adr-status.sh` e `adr-dep-graph.sh` leem esses rótulos. -->

## Status

[Accepted | Proposed | Deprecated | Superseded by ADR-XXXX]

> **Regra do status:** o ADR **nasce `Accepted` quando o Conselho decide** — o `technical-director` assina na Rodada 2 do
> `/conselho` e isso é a aceitação. Fica `Proposed` **só** quando a pauta foi escalada ao usuário (empate de visão, dinheiro novo,
> credencial, publicação) e ainda não há resposta; nesse caso só o usuário (ou o TD com a confirmação dele registrada) muda para `Accepted`.
> Histórias que dependem de ADR `Proposed` ficam `Blocked`; épicos exigem `Accepted`.
> Fora do Conselho, nenhum outro agente ou skill muda este campo sozinho: quem acha que o ADR está pronto escala ao TD.

## Date

[AAAA-MM-DD da decisão]

## Last Verified

[AAAA-MM-DD — última vez em que o ADR foi conferido contra a versão atual da engine e do design. Atualize mesmo sem mudança.]

## Decisores

[Quem participou: ex. Conselho (CD, TD, producer) — ata `<caminho da ata>`]

## Resumo

[2 frases: que problema este ADR resolve e o que foi decidido. Nomeie o sistema, o problema e a abordagem escolhida —
uma skill que varre 20 ADRs usa este resumo para decidir se lê o resto.]

## Compatibilidade com a engine

| Campo | Valor |
|-------|-------|
| **Engine** | [ex.: Unity 6000.3.23f1] |
| **Domínio** | [Física / Render / UI / Áudio / Navegação / Animação / Rede / Core / Input] |
| **Layer** | [Foundation / Core / Feature / Presentation] |
| **Risco de conhecimento** | [BAIXO / MÉDIO / ALTO — do VERSION.md em `docs/engine-reference/`] |
| **Referências consultadas** | [docs de engine-reference lidos, ex. `docs/engine-reference/unity/modules/physics.md`] |
| **APIs pós-corte usadas** | [APIs de versões após o corte do modelo de que a decisão depende, ou "Nenhuma"] |
| **Verificação exigida** | [Comportamentos a testar antes de entregar, ou "Nenhuma"] |

> Risco MÉDIO/ALTO → revalidar este ADR se a engine for atualizada (marcar `Superseded` e escrever outro).
> **`Layer` é a camada do próprio ADR**, não a do épico que o cita, e é outra taxonomia que `Domínio`. Vários pontos
> (`/create-stories`, `/create-epics`, `/architecture-decision`, `/story-readiness`, `gate-pre-production.md`) tratam ADR
> `Foundation` como crítico; linha ausente conta como crítico. Use só `Foundation / Core / Feature / Presentation`.

## ADR Dependencies

| Campo | Valor |
|-------|-------|
| **Depends On** | [ADR-NNNN (precisa estar Accepted antes), ou "None"] |
| **Habilita** | [ADR-NNNN que esta decisão destrava, ou "None"] |
| **Bloqueia** | [Épico/história que não começa até este ADR estar Accepted, ou "None"] |
| **Ordem** | [Restrição de sequência não capturada acima] |

## Contexto

### Problema
[O que estamos resolvendo? Por que decidir agora?]

### Estado atual
[Como o sistema funciona hoje? O que está errado?]

### Restrições
- [Técnicas (ex.: 1 Unity por vez, notebook 8 GB)]
- [Prazo]
- [Recursos / custo zero ocioso]
- [Compatibilidade (Android API 36, 16 KB, ARM64)]

### Requisitos
- [Precisa suportar X]
- [Precisa caber no orçamento Y (ms / MB / tris)]
- [Precisa integrar com Z]

## Decisão

[A decisão técnica, com detalhe suficiente para alguém implementar.]

### Diagrama
[ASCII ou descrição da arquitetura resultante]

### Interfaces-chave
[Contratos de API / interfaces que a decisão cria]

### Diretrizes de implementação
[Regras para quem implementa — escreva como "deve / nunca deve" para que `/create-control-manifest` e `/dev-story` extraiam.]

## Alternativas consideradas

### Alternativa 1: [Nome]
- **Descrição**: [Como funcionaria]
- **Prós**: [Vantagens]
- **Contras**: [Desvantagens]
- **Esforço estimado**: [Relativo à escolhida]
- **Motivo da rejeição**: [Por que não]

### Alternativa 2: [Nome]
- **Descrição**:
- **Prós**:
- **Contras**:
- **Esforço estimado**:
- **Motivo da rejeição**:

## Consequências

### Positivas
- [Bons resultados]

### Negativas
- [Custos e trade-offs aceitos]

### Neutras
- [Mudanças nem boas nem ruins]

## Riscos
- [O que pode dar errado — e a mitigação de cada um]

## Requisitos do GDD atendidos

<!-- OBRIGATÓRIO: todo ADR rastreia ao menos um requisito do GDD, ou declara que é fundacional. Auditado por /architecture-review. -->

| Sistema do GDD | Requisito | Como este ADR atende |
|----------------|-----------|----------------------|
| [sistema].md | [regra, fórmula ou limite de performance do GDD] | [como a decisão satisfaz] |

> Fundacional sem requisito direto: "Fundacional — sem requisito de GDD. Habilita: [sistemas do GDD que destrava/restringe]".

## Impacto de performance
- **CPU**: [impacto esperado]
- **Memória**: [impacto esperado]
- **Carregamento**: [impacto esperado]
- **Rede**: [se aplicável]

## Plano de migração
[Se muda código existente, como sair daqui e chegar lá]

**Plano de rollback**: [Como reverter se a decisão se provar errada]

## Critério de validação
[Como saberemos que acertamos? Métrica ou teste — gameplay exige PlayMode ou foto na cena real, não só EditMode.]

## Portão de Segurança
[Sim/Não — rede, backend, auth, IAP ou dados pessoais → `/security-review` → `strix-review`]

## Relacionados
- [ADRs relacionados — indique se substitui, contradiz ou depende]
- [Docs de design]
- [Arquivos de código, quando implementado]

---

## Variante: ADR curto (feature)

Para mecânica pequena da camada `Feature`/`Presentation` (sem dependência de outro ADR, reversível em < 1 dia). Copie só isto:

```markdown
# ADR-[NNNN]: [Título]

## Status
Accepted   <!-- nasce Accepted quando o Conselho decide (TD assina na Rodada 2); Proposed só se escalado ao usuário -->

## Date
[AAAA-MM-DD]

## Resumo
[2 frases: problema e decisão.]

| Campo | Valor |
|-------|-------|
| **Layer** | Feature |
| **Engine** | [ex.: Unity 6000.3.23f1] |

## ADR Dependencies
| Campo | Valor |
|-------|-------|
| **Depends On** | None |

## Decisão
[O que será feito, onde mora a lógica (núcleo testável × casca), contrato principal.]

## Alternativa descartada
[1 alternativa + motivo.]

## Critério de aceite
[Teste provado vermelho + PlayMode ou foto na cena real do jogo; orçamento (ms/MB/tris) se tocar hot path.]

## Rollback
[Como reverter.]
```
Se a feature crescer (nova dependência, mudança de Foundation/Core, custo de reversão alto) → promova ao ADR completo acima.
