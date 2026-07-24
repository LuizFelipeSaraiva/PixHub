# ADR-0001 — Stack e arquitetura da plataforma

- **Status:** Aceito
- **Data:** 2026-07-23
- **Decisores:** Luiz Felipe (autor do portfólio)

## Contexto

Projeto de portfólio para vaga Sênior Backend .NET/AWS no setor financeiro. Precisa cobrir, com
profundidade, um conjunto amplo de requisitos (microsserviços, DDD, Saga, ORMs, AWS, IaC, CI/CD,
observabilidade, segurança, feature flags). O domínio precisa justificar naturalmente esses padrões.

## Decisão

- **Plataforma:** .NET 10 (LTS) / C# 14.
- **Domínio:** conta digital + Pix (pagamentos instantâneos), que exige fluxos distribuídos com
  compensação (Saga) e contabilidade (ledger).
- **Arquitetura:** monorepo de microsserviços, cada um em **Clean Architecture** (Domain →
  Application → Infrastructure → Api/Worker), com **DDD tático** (agregados, VOs, domain events) e
  **SOLID** verificados por testes de arquitetura (NetArchTest).
- **CQRS:** EF Core no _write side_; **Dapper** no _read side_ (queries otimizadas).
- **Mensageria/Saga:** RabbitMQ + **MassTransit v8** (Saga orquestrada) e **AWS SNS/SQS** (Saga
  coreografada), com **Outbox/Inbox** transacional e consumidores idempotentes.
- **AWS:** desenvolvimento 100% local via **LocalStack**; **Terraform** modular pronto para AWS real
  (ECS Fargate e EKS).

## Consequências

- **Positivas:** cobre todos os requisitos da vaga com um fio condutor coeso e realista; executável
  offline (LocalStack) sem custo; cada fase é entregável isoladamente.
- **Negativas / trade-offs:** escopo grande, entregue de forma incremental (fases 0–9); alguns
  serviços de infra (Docker/Terraform/AWS CLI) precisam ser instalados para as fases posteriores.
- **Alternativas consideradas:** domínios de cartões/investimentos (descartados por menor aderência a
  “maior banco do Brasil” e ao Pix); Kafka como espinha dorsal (mantido como possível evolução);
  Wolverine no lugar de MassTransit (ver ADR-0002).
