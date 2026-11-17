# ADR-0006 — Identity (OAuth2/OIDC) e hardening de borda

- **Status:** Aceito
- **Data:** 2026-11-16
- **Decisores:** Luiz Felipe

## Contexto

A Fase 2 do blueprint pede segurança de nível avançado para o setor financeiro: autenticação/autorização
entre serviços e mitigação do OWASP Top 10. Até aqui (Fase 1) o Accounts.Api estava completamente aberto —
qualquer chamador conseguia abrir/debitar/creditar contas. Era preciso um servidor de autorização
dedicado e um jeito de proteger os serviços de recurso sem acoplá-los entre si.

Alternativas consideradas para o servidor de autorização:
- **IdentityServer** (Duende) — descartado: licença comercial desde a v6 (mesma lógica da ADR-0002).
- **Auth0/Keycloak como serviço externo** — descartado para o portfólio: foge do objetivo de demonstrar a
  implementação do próprio fluxo OAuth2/OIDC em .NET, e adiciona uma dependência operacional externa.
- **OpenIddict** — escolhido: open source (Apache 2.0), mantido ativamente, integra nativamente com
  ASP.NET Core + EF Core, e permite implementar o servidor de autorização como mais um serviço do próprio
  monorepo (consistente com a arquitetura de microsserviços do blueprint).

## Decisão

**Servidor de autorização dedicado (`PixHub.Identity`)**, separado dos serviços de recurso:

- **OpenIddict** configurado como servidor OAuth2, hoje só com o fluxo **`client_credentials`** (M2M) —
  o fluxo interativo (`authorization_code` + PKCE, para usuário final) fica para quando existir um client
  de fato que precise dele.
- **`ClientsSeeder`** (`IHostedService`, idempotente) garante na subida do serviço que o escopo
  `accounts.api` e o client `pixhub-backend-client` existam no Postgres — sem passo manual de setup.
- Tokens **JWT assinados, não criptografados** (`DisableAccessTokenEncryption`) — prioriza
  inspecionabilidade/depuração e validação via JWKS padrão por qualquer serviço de recurso, sem segredo
  compartilhado entre Identity e os demais serviços.
- O **audience do token é o recurso** (`accounts-api`), derivado dos escopos concedidos, não do client —
  um client pode futuramente acumular escopos de múltiplos recursos.

**Serviços de recurso (hoje: `Accounts.Api`) validam o token de forma desacoplada:**

- `AddJwtBearer` com `Authority` apontando para o Identity — a validação de assinatura usa a metadata
  OIDC/JWKS descoberta em runtime, então os dois serviços não compartilham segredo nem precisam ser
  deployados juntos.
- **`ScopeAuthorizationHandler`** (em `PixHub.BuildingBlocks.Api`, reaproveitável por todo serviço de
  recurso futuro) implementa autorização por escopo: o claim `scope` é uma string única separada por
  espaço (RFC 6749), então a política exige policy customizada (`RequireScope`) em vez do
  `RequireClaim` nativo, que só faz igualdade exata.

**Hardening de borda, também em `PixHub.BuildingBlocks.Api` e aplicado nos dois serviços (Identity e
Accounts):**

- **Security headers** escritos à mão (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
  CSP restritiva, HSTS condicional a HTTPS) — ver `SecurityHeadersMiddleware`.
- **Rate limiting nativo** (`Microsoft.AspNetCore.RateLimiting`, sem pacote de terceiros): política
  global (100 req/min por IP) em toda Api, e uma política mais restritiva (10 req/min por IP) só no
  endpoint `/connect/token` — onde o risco principal é força bruta de credenciais de client.
- **PII (CPF)** no domínio de Accounts já nasce com uma projeção mascarada (`Cpf.Masked`, ADR anterior à
  Fase 1) pronta para uso em logging/auditoria — hoje o `LoggingPipelineBehavior` não loga o payload dos
  comandos (só nome do caso de uso + código de erro), então não há PII em log algum neste momento; a
  projeção mascarada fica disponível para quando um log de auditoria granular for implementado.

## Consequências

- **Positivas:** Identity e os serviços de recurso evoluem e deployam de forma independente (só
  compartilham o contrato RFC: issuer, audience, nomes de escopo); `ScopeAuthorizationHandler` e os dois
  middlewares de hardening já nascem reaproveitáveis por qualquer serviço futuro, sem copiar/colar.
- **Negativas / trade-offs:** só o fluxo `client_credentials` está implementado — não há ainda login de
  usuário final (`authorization_code` + PKCE), nem refresh tokens. Em dev/teste, a validação de token é
  feita sem HTTPS (`DisableTransportSecurityRequirement`/`RequireHttpsMetadata = false`) — aceitável só
  porque o tráfego nunca sai da máquina local/CI.
- **Alternativas consideradas:** ver seção Contexto (IdentityServer, serviço externo).
- **Adiado para uma fase/iteração futura** (estava no escopo original da Fase 2, mas não foi necessário
  para o fluxo M2M hoje coberto): `Idempotency-Key` em endpoints de movimentação financeira (vira
  relevante quando existir retry automático entre serviços, na Saga da Fase 4); AWS Secrets
  Manager/KMS para o segredo do client (hoje via configuração/variável de ambiente — suficiente para
  LocalStack não estar provisionado ainda, ver ADR-0005); log de auditoria estruturado usando
  `Cpf.Masked`.

## Mapeamento OWASP Top 10

Ver `docs/owasp-top-10.md` para o mapeamento completo requisito-a-requisito.
