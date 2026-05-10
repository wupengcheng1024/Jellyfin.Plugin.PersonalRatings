---
name: jellyfin-personal-ratings-project
description: "当你在 Jellyfin Personal Ratings 仓库中开始新任务、做需求校对、决定当前阶段或拆解范围时使用：用于先确认项目目标、MVP 边界、版本限制、交付顺序和全局工程规范，再把任务路由到后端、Web 或评审 Skill。"
metadata:
  short-description: "Jellyfin Personal Ratings 项目总控 Skill"
---

# Jellyfin Personal Ratings Project Skill

## 概览

这个 Skill 用来做项目级对齐。它不负责直接实现某个具体模块，而是负责在动手前先把以下问题定清楚：

- 当前任务属于哪个阶段
- 需求是否仍在 MVP 范围内
- 是优先做后端、Web，还是先做评审
- 哪些约束是本次改动不能碰的

## 工作流程

1. 先读取 `references/project-baseline.md`。
   - 用来确认产品目标、业务边界、核心规则和明确不做的事情
2. 再读取 `references/delivery-rules.md`。
   - 用来确认推荐目录结构、阶段顺序、代码规范和执行优先级
3. 判断当前任务所在阶段。
   - 如果仍处于阶段 0~3，优先后端闭环
   - 如果用户要求详情页或管理页交互，转到 Web Skill
   - 如果用户要求 review、验收、风险排查，转到 Review Skill
4. 对大任务先收缩范围。
   - 优先完成当前阶段的最小闭环
   - 不要把“后面会做”的功能提前塞进当前改动
5. 如果需求和现有文档冲突，回到 `jellyfin-personal-ratings-md/` 核对原始文件。

## 什么时候读取 reference

- 需要确认目标、边界、角色权限、评分和删除规则时，读取 `references/project-baseline.md`
- 需要确认阶段顺序、目录结构、技术方向、执行要求时，读取 `references/delivery-rules.md`

## 路由规则

- 后端插件、数据层、API、Jellyfin 适配：转到 `../jellyfin-personal-ratings-backend/SKILL.md`
- Web 注入、详情页、管理页、静态资源：转到 `../jellyfin-personal-ratings-web/SKILL.md`
- 代码评审、发布前检查、验收：转到 `../jellyfin-personal-ratings-review/SKILL.md`
- 真实页面截图、LAN Jellyfin 页面验证、缓存排查：转到 `../jellyfin-personal-ratings-browser-qa/SKILL.md`
- 部署到 50、容器重启、插件日志确认：转到 `../jellyfin-personal-ratings-deploy-50/SKILL.md`

## 约束

- 默认只面向 **Jellyfin 10.10.7**。
- 默认优先 **后端稳定性**，不要一开始堆很重的前端工程。
- 默认所有面向人的说明使用中文；slug、代码和系统命名保持英文。
- 原始需求文档在 `jellyfin-personal-ratings-md/` 下，Skill 和 references 是它们的精简执行版，不是新的独立需求来源。
