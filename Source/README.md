# Personality runtime map

RimMind-Personality turns daily and event triggers into bounded AI-authored
Thoughts and a persistent personality profile. The module has one request path;
Core-facing providers expose its state to other RimMind scenarios.

## Reading order

1. `RimMindPersonalityMod.cs` — composition, Harmony setup, and Core extension registration.
2. `Comps/CompAIPersonality.cs` — eligibility, trigger cooldown, request dispatch, and completion boundary.
3. `Personality/PersonalityRequestBuilder.cs` — stable request envelope.
4. `Personality/PersonalityThoughtMapper.cs` — response application, profile update, and shaping vote entry.
5. `Data/PersonalityProfile.cs` — persistent per-game profile ownership and cleanup.
6. `Personality/*Policy.cs`, `MoodOffsetCalculator.cs`, and `ShapingAction.cs` — pure decisions and value mapping.
7. `Personality/PersonalityProviderRegistrar.cs` — context and Agent Identity output through Core public APIs.
8. `Patches/`, `UI/`, `Debug/`, and `../Tests/README.md` — game adapters, diagnostics, and compact contracts.

## Main flow

```text
daily tick or event patch
  -> CompAIPersonality
  -> PersonalityRequestBuilder
  -> RimMindAPI.RequestStructured
  -> PersonalityThoughtMapper
  -> PersonalityProfile + Thought_AIPersonality
  -> PersonalityProviderRegistrar consumers
```

## Boundaries

- `RimMindPersonalityMod` composes the module; it contains no provider body or widget layout.
- `CompAIPersonality` owns the request lifecycle and validates the original request state before applying a result.
- `PersonalityThoughtMapper` owns response side effects; policy classes keep parsing and decision rules testable.
- `AIPersonalityWorldComponent` owns persistent profiles and removes profiles for absent pawns.
- Providers read Personality state and format Core context. They do not trigger evaluation or mutate Thoughts.
- Verse and Unity side effects stay on the main thread.

## Focused verification

```powershell
dotnet test RimMind-Personality/Tests/RimMindPersonality.Tests.csproj -c Release
dotnet build RimMind-Personality/Source/RimMindPersonality.csproj -c Release
```

The permanent suite contains eight aggregate Facts. The game Autotester remains
separate and is currently blocked by missing resources.
