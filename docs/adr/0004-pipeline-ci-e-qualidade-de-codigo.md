# ADR-0004 — Pipeline de CI e qualidade de código

- **Status:** Aceito
- **Data:** 2026-08-02
- **Decisores:** Luiz Felipe

## Contexto

Concluídas as Fases 0-1 (fundações + `Accounts` Domain/Application), era o primeiro momento natural para
introduzir CI: já existiam 79 testes e convenções de estilo/analisadores (`.editorconfig`,
`Directory.Build.props` com `TreatWarningsAsErrors`) nunca validadas em pipeline. Faltava também decidir
o nível de análise de qualidade adequado a um portfólio, considerando que algumas ferramentas (CodeQL via
GitHub Advanced Security) só são gratuitas em repositórios públicos.

## Decisão

- **GitHub Actions** (`.github/workflows/ci.yml`), disparado em push/PR para `main`.
- **Lint/formatação:** `dotnet format --verify-no-changes` cobre estilo (`.editorconfig`); a análise de
  qualidade (nullable, regras CA) já é feita pelo próprio `dotnet build`, pois
  `EnableNETAnalyzers`/`AnalysisLevel=latest-recommended`/`TreatWarningsAsErrors` fazem qualquer warning
  de analyzer quebrar o build — não há uma ferramenta de lint separada do compilador no ecossistema .NET.
- **Cobertura:** `coverlet.collector` nos projetos de teste + `dotnet test --collect:"XPlat Code
  Coverage"`; `reportgenerator` publica um resumo Markdown no *job summary*/PR e um artefato HTML.
- **Quality Gate:** **SonarCloud** (`dotnet-sonarscanner`) — code smells, duplicação, security hotspots,
  maintainability rating, consumindo o mesmo relatório de cobertura. O passo é condicionado à existência
  do secret `SONAR_TOKEN`, para o CI continuar verde enquanto o projeto não for configurado manualmente no
  SonarCloud.
- **Visibilidade do repositório alterada para pública** (verificado antes: nenhum segredo no histórico) —
  condição habilitadora que libera o tier gratuito do SonarCloud e, futuramente, dos recursos de GitHub
  Advanced Security (CodeQL, secret scanning nativo) sem custo.
- **CodeQL propositalmente fora de escopo por ora** — decisão do autor, não mais uma limitação de custo:
  com o repositório público, o CodeQL já seria gratuito. Fica registrado como possível adição futura
  (Fase 8), quando as demais ferramentas de scan (Trivy, gitleaks) também entrarem.

## Consequências

- **Positivas:** pipeline cobre formatação + lint + build + testes + cobertura + quality gate + validação
  de título de PR (ADR-0003), tudo em tier gratuito por o repositório ser público; SonarCloud é sinal
  reconhecido no mercado; o *gating* por presença de secret evita quebrar o CI para outros contribuidores
  enquanto a conta externa não está configurada.
- **Negativas / trade-offs:** SonarCloud adiciona ~1-2 min ao tempo de CI (setup de Java + instalação de
  ferramentas globais); depende de um passo manual fora do repositório (criar projeto no SonarCloud,
  configurar `SONAR_TOKEN`/`SONAR_PROJECT_KEY`/`SONAR_ORGANIZATION`); tornar o repositório público expõe
  código e histórico integralmente.
- **Alternativas consideradas:** SonarQube self-hosted — descartado por exigir manter um servidor sempre
  acessível pelo runner; manter o repositório privado + GitHub Advanced Security pago — descartado por
  custo desnecessário num portfólio; não usar nenhuma ferramenta de quality gate externa — descartado por
  perder um sinal bem reconhecido em entrevistas técnicas.
- **Revisão futura:** reavaliar a inclusão do CodeQL na Fase 8, junto com Trivy/gitleaks.