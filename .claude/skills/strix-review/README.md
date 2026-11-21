# strix-review 🦉

Skill de **pentest ativo com prova de conceito**, inspirada no projeto open-source
[Strix](https://github.com/usestrix/strix) — mas com uma diferença de arquitetura
fundamental: **o motor é o Claude da sua sessão** (o plano que você já paga), não
chamadas externas de LLM. Zero `LLM_API_KEY`, zero fatura de API. Mesmo modelo do
`scrapling` e do `security-review`.

## O que ela faz
Enquanto o `security-review` só LÊ o código (análise passiva), o `strix-review`
**executa o alvo, ataca de verdade e valida com PoC reproduzível**. O Claude é o
pentester; as ferramentas do arsenal (nuclei, httpx, ffuf, sqlmap, Playwright) são
binários gratuitos que ele dirige.

## Conteúdo da pasta
```
strix-review/
├── SKILL.md                    # a skill (fluxo de 5 fases, regras, relatório)
├── README.md                   # este arquivo
├── reference/
│   ├── fases.md                # playbook detalhado das 5 fases
│   ├── owasp-checklist.md      # bateria de testes por classe OWASP
│   └── ferramentas.md          # arsenal: instalação e receitas
└── scripts/
    ├── setup.ps1               # instala o arsenal (Windows)
    └── setup.sh                # instala o arsenal (Linux/Mac)
```

## Como instalar / transportar
1. Copie a pasta `strix-review/` inteira para onde quiser (é autocontida e portátil).
2. Para ativar no Claude Code, coloque em um destes locais:
   - Global: `~/.claude/skills/strix-review/` (Windows: `C:\Users\User\.claude\skills\strix-review\`)
   - Por projeto: `.claude/skills/strix-review/` dentro do repositório
3. Rode uma vez o `scripts/setup.ps1` (ou `.sh`) para instalar as ferramentas.
4. Para levar a outro computador ou outra conta Claude: basta copiar a pasta. Nada
   aqui depende de credencial ou de estado da máquina de origem.

## Encadeamento
`security-review` (varredura estática grátis) → **strix-review** (explora e confirma
os achados ALTO/CRÍTICO) → `ORQUESTRADOR` (roda as fases em subagents paralelos para
alvos grandes).

## ⚠️ Uso autorizado apenas
Ferramenta ofensiva. Rode só contra sistemas que você possui (portfólio V-STACK) ou
com autorização escrita e explícita. Teste ativo não autorizado é crime na maioria
das jurisdições.
