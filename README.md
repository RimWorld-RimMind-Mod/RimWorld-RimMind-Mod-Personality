<div align="center">

# RimMind-Personality 🎭
### Big-Five Psychological Profiles & Probabilistic State-Transition Reflection for RimWorld 1.6

**English** | [简体中文](README_zh.md)

<p>
  <a href="https://rimworldgame.com/"><img src="https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg" alt="RimWorld 1.6"></a>
  <a href="https://github.com/mcocdaa/RimWorld-RimMind-Mod-Core"><img src="https://img.shields.io/badge/Dependency-RimMind--Core-blue.svg" alt="Dependency: RimMind-Core"></a>
  <a href="#"><img src="https://img.shields.io/badge/Unit%20Tests-9%2B%20Passing-success.svg" alt="Unit Tests"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
</p>

<p><em>Infuse colonists with multidimensional psychological depth, dawn soliloquies, and organic mood reflections.</em></p>

</div>

---

## 📖 Overview

**RimMind-Personality** models colonists with nuanced Big-Five personality dimensions (Openness, Conscientiousness, Extraversion, Agreeableness, Neuroticism). Instead of rigid timer loops, personality reflections occur dynamically through **probabilistic state transitions**.

### Key Highlights
- **Big-Five Cognitive Profiling**: Maps vanilla RimWorld traits (e.g., Bloodlust, Slothful, Optimist) onto continuous psychometric axes, guiding tone and decision rationale.
- **Probabilistic State-Transition Engine**: Colonists reflect on life during meaningful environmental moments:
  - 🌅 **Sunrise (06:00 Dawn)**: Morning reflection as daylight returns (chance: 30%).
  - 🌌 **Skygazing / Solitude**: Philosophical or poetic musings (chance: 40%).
  - 🍷 **Shared Recreation**: Social warmth or witty cynicism (chance: 25%).
  - 💔 **Traumatic Mood Swings**: Severe mental break risks trigger desperate internal rationales (chance: 50%).
- **Thought Injection**: AI-generated psychological reflections manifest directly as RimWorld `ThoughtDef` entries, influencing mood and mental break thresholds.

---

## 🎮 In-Game Showcase

![RimMind-Personality Showcase](docs/images/showcase.jpg)
*Dawn soliloquy in action: A colonist standing in the morning light triggers an introspective thought bubble reflecting their Big-Five traits.*

---

## 🏛️ Architecture & State-Transition Pipeline

```mermaid
flowchart TD
    Env["Environmental Tick (06:00 Dawn / Skygaze / Break Risk)"] --> Transition["State Transition Filter"]
    Transition --> Dice{"Probabilistic Roll & Cooldown Check"}
    Dice -- Success --> CorePrompt["Context Build via RimMind-Core"]
    Dice -- Suppressed --> Idle["Idle until Next Natural State"]
    CorePrompt --> LLM["LLM Generates Thought / Monologue"]
    LLM --> Inject["Inject ThoughtDef into Colonist Mind"]
    Inject --> Advisor["Forward Thought to RimMind-Advisor"]
```

---

## 🛠️ Installation & Load Order

```text
1. Harmony
2. Core (Vanilla RimWorld)
3. RimMind-Core
4. RimMind-Personality
```

---

## 🧪 Developer Guide & Testing

Run unit tests directly:

```powershell
dotnet test RimMind-Personality/Tests/RimMindPersonality.Tests.csproj -c Release
```

---

## 📜 License

Licensed under the [MIT License](LICENSE).
