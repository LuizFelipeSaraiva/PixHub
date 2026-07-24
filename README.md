# PixHub — Plataforma de Pagamentos Pix (.NET 10)

> Projeto de portfólio backend **.NET 10 / C# 14** que demonstra, em nível avançado, arquitetura de
> microsserviços para o setor financeiro: **Clean Architecture + DDD + SOLID**, **CQRS** (EF Core no
> write, Dapper no read), **Saga orquestrada e coreografada**, mensageria (**RabbitMQ/MassTransit +
> AWS SQS/SNS**), segurança (**OAuth2/OIDC + OWASP**), **feature flags de produção**, observabilidade
> (**OpenTelemetry**), **IaC (Terraform)**, containers (**Docker/Kubernetes**) e **CI/CD** — tudo
> executável localmente via **LocalStack** e pronto para AWS real.

## Domínio

Conta digital + **Pix**. Uma transferência instantânea debita uma conta, credita outra, registra no
**ledger** (partidas dobradas), passa por **limites/antifraude** e dispara **notificações** — um fluxo
distribuído com **compensação**, que justifica microsserviços e Saga de verdade (não “de brinquedo”).

## Arquitetura (bounded contexts)

| Serviço | Responsabilidade | Persistência | Destaque |
|---|---|---|---|
| **Identity** | OAuth2/OIDC, JWT, clients M2M | PostgreSQL | Segurança / autenticação |
| **Accounts** | Contas, saldo, ciclo de vida | PostgreSQL (EF write + Dapper read) | Clean Arch/DDD/CQRS de referência |
| **Payments** | Transferência Pix (orquestração) | PostgreSQL | Saga **orquestrada** (MassTransit) |
| **Ledger** | Partidas dobradas, journal imutável | DynamoDB (NoSQL) | Append-only + idempotência |
| **Risk** | Limites + antifraude | DynamoDB | AWS **Lambda** + feature flags |
| **Notifications** | Notificações | — | Saga **coreografada** (SNS→SQS), Lambda |
| **Directory (DICT)** | Resolve chave Pix → conta | DynamoDB + Redis | Cache / lookup rápido |
| **Gateway/BFF** | Borda: auth, rate limit | — | YARP local + AWS API Gateway |

Cada serviço segue **Clean Architecture** (Domain → Application → Infrastructure → Api/Worker), com DI
nativa, **Outbox/Inbox** transacional, Polly (resiliência), health checks e tracing distribuído.

## Requisitos da vaga → onde são demonstrados

| Requisito | Onde | Status |
|---|---|---|
| OO + C#/ASP.NET Core | Todos os serviços (.NET 10 / C# 14) | 🟡 em construção |
| Injeção de dependência | `IServiceCollection` em todos os serviços | 🟡 |
| **Dapper + EF Core** | EF no write, Dapper no read (Accounts/Payments) | ⬜ Fase 1 |
| Clean Arch, DDD, SOLID | 4 camadas + NetArchTest | 🟡 domínio Accounts + BuildingBlocks prontos |
| Microsserviços | 8 serviços + Gateway | ⬜ |
| **Saga coreografada × orquestrada** | Payments + Onboarding | ⬜ Fases 4–5 |
| Feature Toggle (produção) | OpenFeature + Unleash | ⬜ Fase 6 |
| REST + AWS API Gateway | OpenAPI + Terraform API GW | ⬜ Fases 1,7 |
| Segurança (auth, OWASP) | OpenIddict + hardening + ASVS | ⬜ Fase 2 |
| Testes xUnit + mock | xUnit v3 + NSubstitute + Stryker | 🟡 38 testes de domínio (verde) |
| AWS (EC2/ECS/EKS/Fargate/S3/SQS/SNS/Lambda/API GW/CloudWatch) | LocalStack + Terraform | ⬜ Fases 5,7 |
| Mensageria (SQS/Kafka/RabbitMQ) | RabbitMQ+MassTransit + SQS/SNS | ⬜ Fases 3–5 |
| Relacional + NoSQL | PostgreSQL + DynamoDB + Redis | ⬜ |
| Docker / Kubernetes | Dockerfiles + Helm/EKS | ⬜ Fase 7 |
| IaC (Terraform) | Módulos + LocalStack/AWS | ⬜ Fase 7 |
| CI/CD + Git | GitHub Actions + CodePipeline | ⬜ Fase 8 |
| _Dif.:_ Monitoramento | OTel → Grafana/Jaeger/X-Ray | ⬜ Fase 6 |
| _Dif.:_ Canário/Blue-Green/A-B | CodeDeploy / Argo / flags | ⬜ Fase 8 |

## Decisões de dependência _license-aware_

Várias libs .NET consagradas migraram para licença comercial (2024–2026). Este projeto usa
alternativas **gratuitas e modernas** (sinal de julgamento sênior): `Mediator` (não MediatR),
`Mapperly` (não AutoMapper), **MassTransit v8** (não v9), `Shouldly`/`NSubstitute` (não FluentAssertions v8/Moq).
Ver [ADR-0002](docs/adr/0002-dependencias-license-aware.md).

## Como rodar

**Pré-requisitos:** .NET SDK 10.0.203+. Para fases de infra: Docker, Terraform e AWS CLI (LocalStack).

```bash
dotnet build PixHub.slnx -c Release   # compila a solução
dotnet test  PixHub.slnx              # roda os testes (unit/arch)
```

O plano de implementação completo e faseado está em `docs/` e nas ADRs.

---
_Status: Fase 0 (fundações) concluída. Fase 1 (Accounts) em andamento — domínio DDD + 38 testes unitários
verdes. Próximo: camadas Application (CQRS) → Infrastructure (EF Core + Dapper) → API. Cada fase é
entregável e demonstrável._
