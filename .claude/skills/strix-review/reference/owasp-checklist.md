# Checklist OWASP — bateria de testes por classe

Cobre o OWASP Top 10 e as classes que o Strix ataca. Para cada item: o que testar, como, e o sinal de que está vulnerável. Use como bateria durante a Fase 3.

---

## A01 — Broken Access Control (o nº 1 em SaaS multi-tenant)
- **IDOR**: pegue um endpoint `GET /recurso/{id}`; troque o id por um de outro tenant/usuário. Vulnerável se retorna o dado do outro.
- **Escalada horizontal**: usuário A altera/deleta recurso do usuário B.
- **Escalada vertical**: usuário comum acessa rota de admin; ou envia `role: admin` / `is_staff: true` no corpo e o backend confia.
- **Filtro de tenant ausente**: query busca por id sem `WHERE tenant_id = <do requisitante>`.
- **Sinal**: status 200 + dado que não deveria pertencer ao requisitante.

## A02 — Cryptographic Failures / segredos
- Segredos hardcoded no código; `.env` versionado; chave de API em resposta/log.
- Cookies sem `Secure`/`HttpOnly`/`SameSite`.
- Dado sensível trafegando sem TLS ou em URL (querystring).

## A03 — Injection
- **SQLi**: parâmetros que chegam a query crua (fora do ORM). Teste `'`, `" OR 1=1--`, e confirme com sqlmap. Em produção: só prova inócua (`version()`), sem dump.
- **NoSQL**: `{"$gt": ""}` / operadores em campos de filtro.
- **Command injection**: `; id`, `| whoami`, backticks em campos que viram chamada de sistema.
- **SSTI**: `{{7*7}}`, `${7*7}` — se renderiza `49`, template injection.
- **Sinal**: erro de banco, eco do payload avaliado, delay controlável (`SLEEP`).

## A04 — Insecure Design / lógica de negócio
- Manipulação de preço/quantidade (valor negativo, desconto repetido, alterar total no request).
- Race condition (disparar N requests simultâneos para burlar limite/estoque/saldo).
- Bypass de workflow (pular etapa de pagamento/confirmação e chegar direto no estado final).

## A05 — Security Misconfiguration
- `DEBUG=True` em produção (stack trace exposto).
- CORS `*` com credenciais habilitadas.
- Headers de segurança ausentes (CSP, X-Frame-Options, HSTS).
- Endpoints de admin/docs (`/docs`, `/admin`, `/actuator`) expostos sem auth.

## A06 — Vulnerable & Outdated Components
- Cruze `pip-audit`/`npm audit` com o contexto: só reporte CVE com caminho de exploração plausível no app.
- `nuclei` para CVEs conhecidas na tecnologia detectada.

## A07 — Identification & Authentication Failures
- **JWT**: assinatura não verificada (`alg: none`), segredo fraco, expiração ignorada, token reutilizável após logout.
- Login sem rate limit (credential stuffing / brute force).
- Reset de senha com token previsível ou sem expiração.
- Session fixation: sessão não rotaciona após login.

## A08 — Software & Data Integrity
- Desserialização insegura (pickle/yaml.load em dado de usuário).
- Upload que aceita executável/serializado sem validar tipo real.

## A09 — Logging & Monitoring / LGPD
- Dados pessoais (CPF, telefone, endereço, e-mail) em logs, URLs ou respostas além do necessário.
- Ausência de trilha de auditoria em ações sensíveis (alteração de permissão, acesso a dado de terceiro).
- Retenção de dado pessoal sem propósito.

## A10 — SSRF
- Campo que recebe URL do usuário (webhook, importar de link, avatar por URL): aponte para `http://169.254.169.254/` (metadata cloud), `http://localhost:<porta interna>`, ou um endereço interno.
- **Sinal**: o servidor busca o recurso e devolve conteúdo interno / responde diferente para host interno vs externo.

---

## Extras que o Strix cobre
- **XSS** (stored/reflected/DOM): injete `<script>` / handlers; confirme execução via Playwright, não só reflexo no HTML.
- **CSRF**: request que muda estado sem token anti-CSRF e aceito cross-site.
- **Clickjacking**: alvo carregável em iframe sem `X-Frame-Options`/CSP `frame-ancestors`.
- **Prototype pollution** (Node/JS): `__proto__` em JSON que vira propriedade global.
- **XXE**: parser de XML que resolve entidades externas.
- **Mass assignment**: enviar campos extras (`is_admin`, `balance`) que o model aceita sem allowlist.
