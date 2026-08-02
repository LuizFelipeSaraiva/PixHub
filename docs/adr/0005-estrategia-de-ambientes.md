# ADR-0005 — Estratégia de ambientes (dev/staging/prod)

- **Status:** Aceito
- **Data:** 2026-08-02
- **Decisores:** Luiz Felipe

## Contexto

O blueprint prevê, como diferencial da vaga (Fase 8), estratégias avançadas de deploy — Blue/Green,
Canário, A/B — que pressupõem a existência de múltiplos ambientes. Um portfólio precisa demonstrar essa
prática de forma crível, mas sem gerar custo real de AWS de forma contínua nem depender de uma conta AWS
sempre ativa só para existir.

## Decisão

- **`dev`/`staging`** modelados como **workspaces Terraform separados**, rodando permanentemente contra
  **LocalStack** (custo zero) — cobre o fluxo normal de desenvolvimento e o dia a dia do portfólio.
- **GitHub Environments** configurados no workflow de deploy (Fase 8), com **approval manual obrigatório**
  para `prod` — demonstra o gate de promoção entre ambientes mesmo sem uma conta AWS real por trás.
- **`prod` real (AWS free-tier)** reservado como **deploy on-demand**: workflow `workflow_dispatch` que
  roda `terraform apply` em `envs/aws`, acionado manualmente horas antes de uma demonstração (ex.
  entrevista) — não ao vivo durante a chamada, já que provisionar a stack completa leva 15-30+ minutos.
- **Workflow espelho de auto-destroy** (TTL agendado via cron, ou disparo manual) obrigatório junto ao
  deploy on-demand, para evitar custo residual: nem todos os recursos do blueprint são cobertos pelo AWS
  Free Tier (ECS Fargate, ALB e NAT Gateway cobram por hora; o control plane do EKS tem custo fixo).

## Consequências

- **Positivas:** demonstra o fluxo de promoção entre ambientes e as estratégias de deploy do diferencial
  da vaga sem custo AWS recorrente; preserva a opção de mostrar operação numa conta AWS real quando for
  útil, sem deixar infraestrutura ociosa gerando cobrança.
- **Negativas / trade-offs:** o ambiente `prod` real não fica disponível permanentemente — precisa ser
  acionado com antecedência antes de qualquer demonstração; exige disciplina operacional extra para
  garantir que o auto-destroy realmente execute (risco de cobrança surpresa se falhar silenciosamente).
- **Alternativas consideradas:** três ambientes reais permanentes na AWS — descartado por custo constante
  desnecessário num portfólio; manter tudo apenas em LocalStack, sem nunca tocar AWS real — descartado por
  perder o selo de realismo de operar de fato numa conta de nuvem.
- **Revisão futura:** ao implementar a Fase 8, avaliar se o auto-destroy deve ser TTL fixo (ex. 4h) ou
  disparo manual explícito ao fim da demonstração.