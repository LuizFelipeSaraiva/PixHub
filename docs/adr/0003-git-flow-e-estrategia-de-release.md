# ADR-0003 — Git flow e estratégia de release

- **Status:** Aceito
- **Data:** 2026-08-02
- **Decisores:** Luiz Felipe

## Contexto

Projeto de portfólio individual (sem time), até então com todo o histórico direto em `main`. Falta um
fluxo de branches e uma forma de simular releases que sinalize maturidade profissional sem introduzir
ceremônia desnecessária para um único desenvolvedor.

## Decisão

- **GitHub Flow**: `main` sempre estável/deployável; branches curtas `feat/*`, `fix/*`, `chore/*` por
  tarefa, integradas via Pull Request.
- **Conventional Commits** no título de commits/PRs (`feat:`, `fix:`, `chore:`, `ci:`, `docs:` etc.).
- **Releases simuladas** com tags **SemVer** + **GitHub Releases**, marcando o fim de cada fase do
  blueprint (não a cada PR), com changelog gerado automaticamente a partir dos Conventional Commits.
- **Enforcement em CI**: job `pr-title` no workflow (`amannn/action-semantic-pull-request`) valida o
  título do PR no padrão Conventional Commits antes do merge, protegendo a geração do changelog.

## Consequências

- **Positivas:** baixa ceremônia, adequada a um único contribuidor; sinaliza prática de mercado
  (trunk-based/GitHub Flow é o padrão dominante hoje); changelog confiável porque o formato é garantido
  por gate de CI, não por disciplina manual.
- **Negativas / trade-offs:** sem branches dedicadas de `release`/`hotfix` — se múltiplas fases do
  blueprint precisarem de estabilização simultânea e independente, exige cuidado manual extra; o
  enforcement depende de uma GitHub Action de terceiro (`amannn/action-semantic-pull-request`).
- **Alternativas consideradas:** GitFlow completo (`develop`/`release`/`hotfix`) — descartado por
  overhead desnecessário sem time; trunk-based puro sem PR — descartado por perder o gate de revisão e a
  decoração do PR (SonarCloud, resumo de cobertura — ver ADR-0004).