# RimMind Personality contract tests

The retained suite is organized around three stable public boundaries:

- `PersonalityEvaluationContracts` — evaluation math, DTO/request behavior, context policy, and malformed-response isolation.
- `PersonalityThoughtInjectionContracts` — owned Thought slots, trigger eligibility, shaping actions, and bounded shaping history.
- `PersonalityPersistenceContracts` — settings defaults, complete Scribe field mapping, legacy save keys, and malformed/partial save recovery.

The compact suite contains 8 Facts and no Theory rows, below the Personality budget of 40 discovered tests.

## Cutover handoff

The active compile entry should be `Contracts/**/*.cs` plus:

```xml
<Compile Include="..\..\RimMind-Core\TestSupport\ContractCaseRunner.cs"
         Link="Support\ContractCaseRunner.cs" />
```

Retain the pure production links for Thought projection/parsing, trigger policy,
profile and Thought persistence, settings, mood calculation, DTOs, shaping
actions, request construction and scenario policy. Request-envelope tests use
the real Core Domain/Application projects; the Verse persistence recorder
captures full labels, defaults and collection modes.
The legacy compile categories superseded at cutover are evaluation/duration/mood/parser tests, Thought/trigger/shaping tests, and profile/identity/context tests outside `Contracts/`.

## Retired legacy tests

Files outside `Contracts/` are retained on disk but excluded from compilation.
Their behavior mapping is recorded in the root contract mapping document.
Deletion requires explicit owner approval for each exact file path; directories are never deleted.
