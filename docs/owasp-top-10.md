# OWASP Top 10 (2021) — mapeamento para o PixHub

Checklist de como cada categoria é tratada no código hoje, e o que fica para fases seguintes do
blueprint (`.claude/PlanoGeral.md`). Ver também ADR-0006 para o raciocínio por trás das decisões de
Identity/hardening.

| # | Categoria | Como é tratado hoje | Onde |
|---|---|---|---|
| A01 | Broken Access Control | JWT Bearer + autorização por escopo (`RequireScope`) em todo endpoint de `Accounts.Api`; nenhuma rota de negócio é anônima (só `/health/*`, que não expõe dado nem ação). | `Accounts.Api/Program.cs`, `ScopeAuthorizationHandler` |
| A02 | Cryptographic Failures | Tokens JWT assinados (não criptografados, por design — ver ADR-0006) com certificado próprio do OpenIddict; segredo do client nunca hardcoded (lido de configuração/variável de ambiente); CPF nasce com projeção mascarada (`Cpf.Masked`) para uso futuro em log/auditoria; HSTS emitido quando a conexão é HTTPS. | `Identity.Api/Program.cs`, `ClientsSeeder`, `Cpf.cs`, `SecurityHeadersMiddleware` |
| A03 | Injection | EF Core (write) e Dapper (read) usam sempre parâmetros tipados/LINQ — nenhuma concatenação de SQL em lugar nenhum do código; `FluentValidation` rejeita entrada inválida antes de qualquer caso de uso rodar. | `Accounts.Infrastructure/Repositories`, `Accounts.Application` (behaviors de validação) |
| A04 | Insecure Design | Value Objects (`Money`, `Cpf`, `AccountId`) só existem via `Create`/factory validando invariantes — estado inválido não é representável; agregado `Account` protege as próprias transições (ex.: não debita conta bloqueada). | `Accounts.Domain` |
| A05 | Security Misconfiguration | `TreatWarningsAsErrors` + analisadores na solução inteira; `ProblemDetails` (RFC 9457) no lugar de stack trace vazando em exceção não tratada; headers de segurança (`nosniff`, `X-Frame-Options`, CSP, Referrer-Policy) em toda resposta. | `Directory.Build.props`, `Program.cs` dos dois serviços, `SecurityHeadersMiddleware` |
| A06 | Vulnerable and Outdated Components | `Directory.Packages.props` (central package management) facilita auditoria/bump de versão num único lugar; escolhas de dependência já avaliam licença/manutenção (ADR-0002). **Pendência conhecida:** o restore atual reporta duas vulnerabilidades (NU1902/NU1903) em `System.Security.Cryptography.Pkcs`/`Xml` 10.0.11, trazidas transitivamente pelo OpenIddict — rastrear bump de versão quando uma release corrigida estiver disponível. | `Directory.Packages.props`, ADR-0002 |
| A07 | Identification and Authentication Failures | Servidor de autorização dedicado (OpenIddict) em vez de autenticação implementada à mão por serviço; rate limiting dedicado no endpoint de token (10 req/min/IP) mitiga força bruta de credenciais de client. **Pendência:** só `client_credentials` (M2M) existe — login de usuário final (`authorization_code` + PKCE, MFA) é fase seguinte. | `Identity.Api`, `RateLimitingExtensions` |
| A08 | Software and Data Integrity Failures | CI (ADR-0004) roda build+testes antes de qualquer merge; Conventional Commits + PR obrigatório evitam push direto não revisado em `main`. | `.github/workflows/ci.yml`, ADR-0003/0004 |
| A09 | Security Logging and Monitoring Failures | Toda execução de caso de uso é logada (início, sucesso/erro, duração) com `[LoggerMessage]` source-gen; falha de negócio sai como `Warning` com código do erro, exceção como `Error`. **Pendência:** não há ainda log de auditoria dedicado (quem fez o quê, quando) nem sink centralizado — fica para a Fase 6 (observabilidade). | `LoggingPipelineBehavior` |
| A10 | Server-Side Request Forgery (SSRF) | Nenhum serviço hoje aceita URL/host controlado pelo cliente para disparar uma requisição de saída — superfície inexistente neste momento. Reavaliar quando `Directory (DICT)`/integrações externas forem implementadas (Fase 5+). | — |

## Observações

- Esta tabela é um checklist vivo: cada nova fase do blueprint deve revisitar as linhas marcadas como
  "pendência" e as categorias com superfície ainda inexistente (A10).
- Os testes de autorização por escopo (`PixHub.Accounts.Api.Tests`) exercitam A01 de ponta a ponta contra
  o pipeline real (sem token → 401; escopo errado → 403; escopo correto → autorizado).
