# AGENTS.md — RimMind-Personality

本文件供 AI 编码助手阅读，描述 RimMind-Personality 的架构、代码约定和扩展模式。

## 项目定位

RimMind-Personality 是 RimMind AI 模组套件的人格系统模块。职责：

1. **人格评估**：定时或事件触发时，调用 AI 评估小人人格状态
2. **Thought 生成**：将 AI 评估结果转化为游戏内 Thought（最多 3 个槽位）
3. **心情影响**：Thought 通过 `MoodOffsetCalculator` 影响小人心情
4. **叙事更新**：AI 生成的叙事文本写入 `PersonalityProfile`
5. **玩家塑造**：玩家可通过悬浮窗投票"强化/抑制"人格 Thought
6. **上下文注入**：将人格档案、状态、塑造历史注入 AI Prompt

**依赖关系**：
- 依赖 RimMind-Core 提供的 API 和上下文构建
- 被 RimMind-Advisor 和 RimMind-Dialogue 消费（人格上下文影响决策和对话风格）

## 源码结构

```
Source/
├── RimMindPersonalityMod.cs        Mod 入口，注册 Harmony，初始化设置
├── Personality/
│   ├── PersonalityThoughtMapper.cs  核心：AI 响应 → Thought 映射
│   ├── PersonalityContextBuilder.cs 构建 AI 请求的 User Prompt
│   ├── PersonalityResultDto.cs      AI 响应 JSON DTO
│   ├── EvaluationInstructionHelper.cs 评估指令构建（Poisson 抽样 + JSON 格式）
│   ├── Thought_AIPersonality.cs     自定义 Thought 类型（带 AI 标签和强度）
│   └── MoodOffsetCalculator.cs     强度→心情偏移查表
├── Settings/
│   └── AIPersonalitySettings.cs     模组设置（触发源、持续时间、塑造等）
├── Data/
│   ├── PersonalityProfile.cs        人格档案 + WorldComponent
│   └── ShapingRecord.cs             玩家塑造记录
├── Comps/
│   ├── CompAIPersonality.cs         ThingComp：评估触发 + 事件监听
│   └── CompProperties_AIPersonality.cs
├── UI/
│   ├── BioTabPersonalityPatch.cs    在 Bio 页添加"人格"按钮
│   └── Dialog_PersonalityProfile.cs 人格档案编辑窗口
├── Patches/
│   └── AddCompToHumanlikePatch.cs   为人形种族注入 Comp
└── Debug/
    └── PersonalityDebugActions.cs   Dev 菜单调试动作
```

## 关键类与 API

### CompAIPersonality

挂载到每个殖民者的 ThingComp，负责触发评估：

```csharp
// 常量
DailyInterval = 60000;       // 1 游戏天
EventCooldownTicks = 1200;   // 0.5 天事件冷却

// 核心方法
override void CompTick()     // 检测：每日定时 或 事件触发
void TriggerEvent(string context)  // 外部 Patch 调用，设置待处理事件
bool IsEligible()            // 自由非奴隶殖民者、未死亡、在地图上、有 mood

// System Prompt 构建
static string BuildSystemPrompt()  // 使用 StructuredPromptBuilder
```

**触发条件**：
1. 总开关 `enablePersonality` 开启
2. API 已配置
3. 小人符合资格
4. 每日定时触发（`enableDailyEval`）
5. 或事件触发（受伤/技能/事件/死亡），带冷却

### PersonalityThoughtMapper

核心映射逻辑，将 AI 响应转化为游戏内 Thought：

```csharp
// 主入口
static void Apply(AIResponse response, Pawn pawn)
// 解析 JSON → PersonalityResultDto
// 写入 narrative 到 profile
// 清除旧 Thought
// 创建新 Thought_AIPersonality（最多 3 个槽位）
// 注册塑造投票请求

// 持续时间计算
static int CalcDurationTicks(ThoughtEntryDto entry, AIPersonalitySettings? settings)
// Fixed 模式：thoughtDurationHours * 2500
// AIDecides 模式：entry.duration_hours * 2500（兜底 24h）

// Thought 槽位
static readonly string[] SlotDefNames = {
    "AIPersonality_Slot_0",
    "AIPersonality_Slot_1",
    "AIPersonality_Slot_2"
};

// 工具方法
static void RemoveAllAIPersonalityThoughts(Pawn pawn)
static bool IsAIPersonalityDef(string defName)
```

### Thought_AIPersonality

自定义 Thought 类型，挂到 Pawn 的记忆 Thought 列表：

```csharp
public class Thought_AIPersonality : Thought_Memory
{
    public string aiLabel;           // AI 生成的标签（<=8字）
    public string aiDescription;     // AI 生成的描述（<=20字）
    public int aiIntensity;          // 强度 -3~+3
    public int customDurationTicks;  // 自定义持续时间（-1=使用默认）

    override int DurationTicks       // customDurationTicks > 0 时使用
    override string LabelCap         // [RimMind] 前缀（可配置）
    override string Description      // aiDescription
    override float MoodOffset()      // 委托 MoodOffsetCalculator
    override void ExposeData()       // 序列化全部字段
}
```

### MoodOffsetCalculator

强度到心情偏移的查表映射：

```csharp
static class MoodOffsetCalculator
{
    static readonly float[] MoodTable = { -10, -3, -1, 0, +1, +3, +10 };
    // intensity: -3  -2  -1  0  +1  +2  +3

    static float CalcMoodOffset(int intensity);  // 自动 clamp 到 [-3, +3]
}
```

### PersonalityResultDto（JSON DTO）

```csharp
public class PersonalityResultDto
{
    public ThoughtEntryDto[] thoughts { get; set; } = Array.Empty<ThoughtEntryDto>();
    public string narrative { get; set; } = "";
}

public class ThoughtEntryDto
{
    public string type { get; set; }           // "state" 或 "behavior"
    public string label { get; set; }          // <=8 字
    public string description { get; set; }    // <=20 字
    public int intensity { get; set; }         // -3 ~ +3
    public int? duration_hours { get; set; }   // 可选，仅 AIDecides 模式
}
```

### PersonalityProfile

```csharp
public class PersonalityProfile : IExposable
{
    // 玩家可编辑
    public string description;       // 人格描述
    public string workTendencies;    // 工作倾向
    public string socialTendencies;  // 社交倾向

    // AI 生成（只读）
    public string aiNarrative;       // AI 叙事文本
    public bool rimTalkSynced;       // RimTalk 同步标记
    public int lastNarrativeUpdateTick;

    // 塑造历史
    public List<ShapingRecord> playerShapingHistory;

    // 方法
    bool IsEmpty { get; }
    void AddShapingRecord(ShapingRecord record, int maxCount);
    void ExposeData();
}
```

### AIPersonalityWorldComponent

```csharp
public class AIPersonalityWorldComponent : WorldComponent
{
    static AIPersonalityWorldComponent? Instance;

    PersonalityProfile GetOrCreate(Pawn pawn);
    bool TryGet(Pawn pawn, out PersonalityProfile? profile);
    void Remove(Pawn pawn);
    override void ExposeData();

    // 内部：Dictionary<int, PersonalityProfile> _profiles（以 thingIDNumber 为键）
}
```

### ShapingRecord

```csharp
public class ShapingRecord : IExposable
{
    public string thoughtLabel;  // Thought 标签
    public string action;       // "reinforce" / "suppress" / "ignored"
    public int tick;            // 时间戳
}
```

## AI Prompt 结构

### System Prompt

使用 `StructuredPromptBuilder` 链式构建：

```
[角色] 你是 RimWorld 殖民者 {name} 的人格内核。
[目标] 根据当前状态评估心理状态和行为倾向。
[流程] 1.分析状态 2.生成Thought 3.撰写叙事
[约束] 1~3个Thought，intensity范围-3~+3
[输出] JSON格式
[示例] ...
[兜底] 生成一个中性Thought
```

### User Prompt

```
{RimMindAPI.BuildFullPawnPrompt(pawn, excludeProviders: ["personality_state"])}
{EvaluationInstructionHelper.Append(basePrompt, eventContext, targetCount, aiDecidesDuration)}
```

`EvaluationInstructionHelper` 追加：
- 触发原因（每日/受伤/技能/事件/死亡）
- JSON 格式模板（含 type/label/description/intensity/duration_hours）
- Poisson 抽样决定目标 Thought 数量（mu=1.5，clamp 到 [1,3]）

## 玩家塑造系统

1. AI 评估后，每个 Thought 通过 `RimMindAPI.RegisterPendingRequest` 注册投票请求
2. 悬浮窗显示选项："强化" / "抑制" / "忽略"
3. 选择后写入 `ShapingRecord`，追加到 `PersonalityProfile.playerShapingHistory`
4. 塑造历史通过 `personality_shaping` Provider 注入下次评估的 Prompt

## 上下文注入

Personality 向 Core 注册三个 Provider：

| Provider | 优先级 | 内容 |
|----------|--------|------|
| personality_profile | PriorityKeyState | 人格档案（描述+工作倾向+社交倾向+AI叙事） |
| personality_state | PriorityKeyState | 当前活跃的人格 Thought 列表 |
| personality_shaping | PriorityMemory | 玩家塑造历史记录 |

`BuildFullPawnPrompt` 时排除 `personality_state`（避免评估时看到自己的当前状态）。

## 数据流

```
CompAIPersonality.CompTick()
    │
    ├── 检查触发条件（每日/事件）
    │       ▼
    ├── PersonalityContextBuilder.BuildEvaluationPrompt()
    │       ▼
    ├── RimMindAPI.RequestAsync()
    │       ▼
    ├── AI 生成响应
    │       ▼
    ├── PersonalityThoughtMapper.Apply()
    │       ├── 解析 JSON → PersonalityResultDto
    │       ├── 写入 narrative → PersonalityProfile
    │       ├── 清除旧 Thought
    │       ├── 创建新 Thought_AIPersonality（1~3 个槽位）
    │       └── 注册塑造投票请求
    │       ▼
    └── Thought_AIPersonality 挂到 Pawn
            └── MoodOffsetCalculator.CalcMoodOffset() → 影响心情
```

## 设置项

| 设置 | 默认值 | 说明 |
|------|--------|------|
| enablePersonality | true | 总开关 |
| showNotifications | true | 显示通知 |
| enableDailyEval | true | 每日定时评估 |
| enableInjuryTrigger | true | 受伤触发 |
| enableSkillTrigger | true | 技能升级触发 |
| enableIncidentTrigger | true | 事件触发 |
| enableDeathTrigger | true | 死亡触发 |
| thoughtCountMu | 1.5 | Poisson 抽样参数 |
| thoughtDurationHours | 24 | 固定持续时间（小时） |
| durationMode | Fixed | 持续时间模式（Fixed/AIDecides） |
| showLabelPrefix | true | [RimMind] 前缀 |
| enableShapingVote | true | 玩家塑造投票 |
| requestExpireTicks | 30000 | 请求过期 |
| shapingHistoryMaxCount | 50 | 塑造历史上限 |

## 代码约定

### 命名空间

- `RimMind.Personality` — 核心逻辑（Mod 入口、Thought 映射、Prompt、DTO、心情计算）
- `RimMind.Personality.Data` — 数据模型（Profile、WorldComponent、ShapingRecord）
- `RimMind.Personality.Comps` — ThingComp
- `RimMind.Personality.UI` — 界面（BioTab 补丁、对话框）
- `RimMind.Personality.Patches` — Harmony 补丁
- `RimMind.Personality.Debug` — 调试动作

### ThoughtDef 定义

在 `Defs/ThoughtDefs/Thoughts_AIPersonality.xml` 中定义 3 个槽位：

```xml
<ThoughtDef>
  <defName>AIPersonality_Slot_0</defName>
  <!-- Slot_1, Slot_2 同理 -->
  <durationDays>1</durationDays>
  <stages>
    <li>
      <label>AI人格</label>
      <description>AI生成的人格状态</description>
    </li>
  </stages>
</ThoughtDef>
```

运行时通过 `Thought_AIPersonality` 覆盖 Label/Description/MoodOffset。

### 序列化

```csharp
// ThingComp
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Values.Look(ref _lastEventTick, "lastEventTick", 0);
}

// WorldComponent
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Collections.Look(ref _profiles, "profiles", LookMode.Value, LookMode.Deep);
}
```

### Harmony

- Harmony ID：`mcocdaa.RimMindPersonality`
- 使用 Postfix 动态注入 CompProperties

### 构建

- 目标框架：`net48`
- C# 语言版本：9.0
- RimWorld 版本：1.6
- 输出路径：`../1.6/Assemblies/`

## 调试

Dev 菜单（需开启开发模式）→ RimMind Personality：

- **Force Evaluate Selected** — 强制对选中 Pawn 发起 AI 评估
- **Show Personality State** — 输出人格状态到日志
- **Clear Personality Thoughts** — 清除选中 Pawn 的人格 Thought
- **List Enabled Pawns** — 列出启用人格系统的殖民者
- **Reset Personality Profile** — 重置选中 Pawn 的人格档案

## 注意事项

1. **Thought 槽位限制**：最多 3 个，超出时覆盖最早的
2. **Poisson 抽样**：`thoughtCountMu` 控制平均 Thought 数量，结果 clamp 到 [1,3]
3. **AIDecides 模式**：AI 可为每个 Thought 指定不同持续时间
4. **塑造历史**：`shapingHistoryMaxCount` 控制历史长度，超出时移除最旧记录
5. **排除 personality_state**：评估时排除当前状态，避免 AI 简单复制已有 Thought
6. **CalcDurationTicks null safety**：settings 参数为 nullable，null 时兜底返回 1 小时
