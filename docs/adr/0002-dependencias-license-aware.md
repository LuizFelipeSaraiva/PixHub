# ADR-0002 — Escolha de dependências _license-aware_

- **Status:** Aceito
- **Data:** 2026-07-23
- **Decisores:** Luiz Felipe

## Contexto

Entre 2024 e 2026, várias bibliotecas .NET amplamente usadas migraram para licenças comerciais/pagas.
Um portfólio open-source não deve depender de libs que exijam licença comercial em produção, e demonstrar
consciência dessa mudança é um sinal de senioridade.

## Decisão

Usar alternativas gratuitas, modernas e (quando possível) baseadas em _source generators_:

| Uso | Evitar (agora pago) | Adotado (gratuito) |
|---|---|---|
| Mediator/CQRS | MediatR | **`Mediator`** (martinothamar, MIT, source-gen) |
| Mapeamento | AutoMapper | **`Mapperly`** (Riok.Mapperly, MIT, source-gen) |
| Bus/Saga | MassTransit **v9** (comercial) | **MassTransit v8** (OSS) — alternativa: Wolverine |
| Assertions | FluentAssertions v8+ | **`Shouldly`** / `AwesomeAssertions` |
| Mock | Moq (SponsorLink) | **`NSubstitute`** |

## Consequências

- **Positivas:** projeto 100% livre; melhor desempenho (source generators evitam reflexão em runtime).
- **Negativas / trade-offs:** MassTransit v8 recebe apenas correções de segurança; `Mediator` e
  `Mapperly` são menos ubíquos que MediatR/AutoMapper (curva de familiaridade em entrevistas).
- **Revisão futura:** migrar para Wolverine caso o MassTransit v8 seja descontinuado.
