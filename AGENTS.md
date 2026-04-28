# AGENTS.md — RimMind-Personality

人格系统，LLM评估小人状态 → 注入人格Thought(最多3槽位)影响心情与行为。

## 项目定位

每日定时(含确定性抖动)或事件触发(受伤/技能/事件/死亡) → ContextEngine(RequestStructured, SchemaRegistry.PersonalityOutput) → AI评估 → `PersonalityThoughtMapper.Apply` 解析 → 写入 `PersonalityProfile`(narrative+identity) → 生成 `Thought_AIPersonality`(最多3槽位) → `MoodOffsetCalculator` 查表影响心情。含玩家塑造投票(强化/抑制/忽略)、AgentIdentity注册、Bio页人格按钮。

依赖: Core(编译期)，Personality上下文被Advisor/Dialogue通过Core系统自动消费。

## 构建

| 项 | 值 |
|----|-----|
| Target | net48, C#9.0, Nullable enable |
| Output | `../1.6/Assemblies/` |
| Assembly | RimMindPersonality |
| Harmony ID | mcocdaa.RimMindPersonality |
| 依赖 | RimMindCore.dll, Krafs.Rimworld.Ref, Lib.Harmony.Ref, Newtonsoft.Json 13.0 |

## 源码结构

```
Source/
├── RimMindPersonalityMod.cs              Mod入口
├── Personality/
│   ├── PersonalityThoughtMapper.cs       核心: AI响应→Thought映射+塑造投票+EvaluationSchema
│   ├── PersonalityResultDto.cs           JSON DTO(无RimWorld依赖)
│   ├── Thought_AIPersonality.cs          自定义Thought(重写Label/MoodOffset/DurationTicks)
│   └── MoodOffsetCalculator.cs           强度→心情偏移查表(-3~+3)
├── Settings/AIPersonalitySettings.cs     13项设置
├── Data/
│   ├── PersonalityProfile.cs             人格档案(IExposable)
│   ├── AIPersonalityWorldComponent.cs    WorldComponent单例
│   └── ShapingRecord.cs                  玩家塑造记录
├── Comps/CompAIPersonality.cs            ThingComp(Tick触发/事件触发/TriggerEventType枚举)
├── UI/BioTabPersonalityPatch.cs + Dialog_PersonalityProfile.cs
├── Patches/                              5个Patch(Injury/Skill/Incident/Death + AddComp)
└── Debug/PersonalityDebugActions.cs
```

## 触发机制

CompAIPersonality.CompTick: `DailyInterval=60000` + `JitterRange=3000`(基于thingIDNumber确定性抖动) + 事件触发(1200tick冷却)。

TriggerEventType枚举: `Injury`(enableInjuryTrigger) / `Skill`(enableSkillTrigger) / `Incident`(enableIncidentTrigger) / `Death`(enableDeathTrigger)

请求参数: `Scenario=Personality, ExcludeKeys=["personality_state"], MaxTokens=600, Temperature=0.8f`

## 心情偏移查表

| intensity | -3 | -2 | -1 | 0 | +1 | +2 | +3 |
|-----------|---:|----|----|---|----|----|----|
| MoodOffset | -10 | -3 | -1 | 0 | +1 | +3 | +10 |

`MoodOffsetCalculator.CalcMoodOffset` 自动clamp到[-3,+3]后查表。

## PersonalityResultDto

```csharp
thoughts[]: {type, label, description, intensity(-3~+3), duration_hours?}
narrative: string
identity?: {motivations[], traits[], core_values[]}
```

## 上下文注入

| Provider | 内容 |
|----------|------|
| personality_profile | 人格档案(描述+工作倾向+社交倾向+AI叙事) |
| personality_state | 当前活跃Thought列表(Slot_0/1/2) |
| personality_shaping | 玩家塑造历史记录 |
| personality_task | TaskInstruction(L0_Static, 0.95, 仅Personality场景) |
| AgentIdentity | 向Core注册identity→motivations/traits/core_values |

## 代码约定

- Thought槽位最多3个(`SlotDefNames[3]`)
- `aiDescription` 存档key为 `"aiDesc"`(非 `"aiDescription"`，修改需向后兼容)
- `AIDecides` 模式: `duration_hours` clamp到[1,24]
- 翻译键前缀: `RimMind.Personality.*`

## 操作边界

### ✅ 必须做
- 新触发类型在 `TriggerEventType` 添加值 + `CompAIPersonality.TriggerEvent` 添加分支
- 新设置项在 `ExposeData` + UI + 翻译键三处同步

### ⚠️ 先询问
- 修改 `MoodTable` 心情偏移值
- 修改每日抖动算法
- 修改Patch触发过滤(如Injury添加严重度过滤)

### 🚫 绝对禁止
- 后台线程调用 `ThoughtMaker.MakeThought`/`TryGainMemory`
- 修改 `"aiDesc"` 存档key(破坏向后兼容)
- 向Core注册Provider用旧API(用 `ContextKeyRegistry.Register`)
