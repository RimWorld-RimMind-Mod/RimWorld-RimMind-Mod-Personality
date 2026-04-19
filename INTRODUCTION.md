# RimMind - Personality

AI 驱动的性格系统，让每个殖民者拥有独特的内在世界。

## 核心能力

**每日性格评估** - 每天一次，AI 综合分析殖民者的状态、经历、关系，生成个性化的性格 Thoughts。

**动态心情影响** - AI 生成的性格 Thought 会直接影响殖民者的心情值，让心理状态随经历真实变化。

**性格档案可视化** - 在殖民者的生物标签页新增"性格"面板，查看 AI 对其性格的评估摘要。

**玩家可编辑** - 你可以编辑人格描述、工作倾向和社交倾向，AI 评估时会参考你的设定。

**塑造投票** - 对 AI 的人格评估投票，投票历史注入后续 AI 请求，让 AI 逐渐学习你的偏好。

**多种触发方式** - 每日定时、受伤、技能升级、重要事件、亲近者死亡，均可触发人格评估，每种触发方式可独立开关。

## 评估维度

- 当前情绪状态与心情趋势
- 近期正负经历的影响
- 人际关系质量
- 工作与生活的平衡
- 健康状况的影响
- 玩家编辑的人格描述

## 触发机制

| 触发类型 | 设置开关 | 说明 |
|---------|---------|------|
| 每日定时 | enableDailyEval | 每游戏天评估一次（含随机抖动避免同时触发） |
| 受伤/患病 | enableInjuryTrigger | 健康状态剧变时触发 |
| 技能里程碑 | enableSkillTrigger | 技能等级提升时触发 |
| 重要事件 | enableIncidentTrigger | 袭击、收获等事件时触发 |
| 亲近者死亡 | enableDeathTrigger | 社交关系对象死亡时触发 |

事件触发有 1200 tick 冷却期（约 0.02 游戏天），防止连锁触发。

## 心情影响

| AI 强度 | 心情偏移 |
|---------|---------|
| -3 | -10 |
| -2 | -3 |
| -1 | -1 |
| 0 | 0 |
| +1 | +1 |
| +2 | +3 |
| +3 | +10 |

## 设置项

| 设置 | 默认值 | 说明 |
|------|--------|------|
| 启用 AI 人格系统 | 开启 | 总开关 |
| 每日定时评估 | 开启 | 每游戏天评估一次 |
| 受伤触发 | 开启 | 健康剧变时触发 |
| 技能升级触发 | 开启 | 技能提升时触发 |
| 事件触发 | 开启 | 重要事件时触发 |
| 死亡触发 | 开启 | 亲近者死亡时触发 |
| Thought 数量期望值 | 1.0 | Poisson 抽样参数 μ（0→固定1个，越大越多，结果 1~3） |
| Thought 持续时长模式 | AI 决定 | 固定 / AI 决定 |
| 固定时长 | 24 游戏小时 | 固定模式下的时长（1~24 小时） |
| 显示通知 | 开启 | 人格更新时右下角提示 |
| 显示 [RimMind] 前缀 | 开启 | 在心情面板区分 AI 生成和原版 Thought |
| 启用塑造投票 | 开启 | 玩家可对 AI 评估投票 |
| 请求过期时间 | 0.50 游戏天 | 评估请求超时自动取消 |
| 塑造历史保留数量 | 20 | 保留最近 N 次投票记录供 AI 参考 |

## 建议配图

1. 性格档案面板截图（展示 AI 评估和玩家编辑区域）
2. AI 生成的性格 Thought 示例（心情面板中的展示）
3. 塑造投票界面截图

---

# RimMind - Personality (English)

An AI-driven personality system giving each colonist a unique inner world.

## Key Features

**Daily Personality Assessment** - Once per day, AI comprehensively analyzes colonist state, experiences, and relationships to generate personalized personality Thoughts.

**Dynamic Mood Impact** - AI-generated personality thoughts directly affect colonist mood values, allowing mental states to change realistically with experiences.

**Personality Profile Visualization** - Adds a "Personality" panel to the colonist bio tab to view AI's personality assessment summary.

**Player Editable** - You can edit personality description, work tendencies, and social tendencies. AI evaluations reference your input.

**Shaping Vote** - Vote on AI personality assessments. Vote history is injected into future AI requests, helping AI learn your preferences.

**Multiple Triggers** - Daily timer, injury, skill milestone, incidents, death of loved ones can all trigger personality evaluation. Each trigger type can be toggled independently.

## Assessment Dimensions

- Current emotional state and mood trends
- Impact of recent positive/negative experiences
- Quality of interpersonal relationships
- Work-life balance
- Health status effects
- Player-edited personality description

## Trigger Mechanism

| Trigger Type | Setting Switch | Description |
|-------------|---------------|-------------|
| Daily timer | enableDailyEval | Evaluate once per game day (with random jitter to avoid simultaneous triggers) |
| Injury/Illness | enableInjuryTrigger | Trigger on health state changes |
| Skill milestone | enableSkillTrigger | Trigger on skill level up |
| Major incident | enableIncidentTrigger | Trigger on raids, harvests, etc. |
| Death of loved one | enableDeathTrigger | Trigger when a social relation dies |

Event triggers have a 1200 tick cooldown (~0.02 game days) to prevent chain triggering.

## Mood Impact

| AI Intensity | Mood Offset |
|-------------|------------|
| -3 | -10 |
| -2 | -3 |
| -1 | -1 |
| 0 | 0 |
| +1 | +1 |
| +2 | +3 |
| +3 | +10 |

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Enable AI Personality System | On | Master switch |
| Daily Evaluation | On | Evaluate once per game day |
| Injury Trigger | On | Trigger on health changes |
| Skill Level Up Trigger | On | Trigger on skill improvement |
| Incident Trigger | On | Trigger on major events |
| Death Trigger | On | Trigger when loved ones die |
| Thought Count Expectation (μ) | 1.0 | Poisson sampling parameter (0=fixed 1, higher=more, result 1~3) |
| Thought Duration Mode | AI Decides | Fixed / AI Decides |
| Fixed Duration | 24 game hours | Duration in Fixed mode (1~24 hours) |
| Show Notifications | On | Display notification on personality updates |
| Show [RimMind] Prefix | On | Distinguish AI-generated Thoughts from vanilla in mood panel |
| Enable Shaping Vote | On | Players can vote on AI assessments |
| Request Expiry | 0.50 game days | Auto-cancel evaluation requests after timeout |
| Shaping History Limit | 20 | Keep last N vote records for AI reference |

## Suggested Screenshots

1. Personality profile panel (showing AI assessment and player editing area)
2. Example of AI-generated personality thought (in mood panel)
3. Shaping vote interface
