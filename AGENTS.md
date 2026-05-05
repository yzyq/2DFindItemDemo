# 🧠 AGENTS.md — Unity Hidden Object Demo Architecture

> 本文件用于指导 AI / Codex / 开发者理解本 Unity 工程结构  
> 核心目标：  
> **稳定找物玩法架构 + UI驱动 + 事件通信 + 可扩展交互系统**

---

# 🎯 一、项目定位

本项目是：

> 📱 竖屏找物（Hidden Object）Unity Demo Framework

核心玩法：

- 在场景中隐藏多个目标物体
- 玩家通过点击/触摸进行识别与收集
- UI展示任务进度与反馈
- 支持动画 / 特效 / Spine表现扩展

---

# 🚨 二、核心技术约束（强制）

## 1. 异步规范

❌ 禁止使用：

- StartCoroutine
- yield return

✅ 必须使用：

- UniTask

```csharp
await UniTask.Delay(100);