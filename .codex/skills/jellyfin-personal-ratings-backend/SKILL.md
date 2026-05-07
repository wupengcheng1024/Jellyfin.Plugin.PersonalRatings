---
name: jellyfin-personal-ratings-backend
description: "当你在 Jellyfin Personal Ratings 仓库中实现或审查后端插件改动时使用：用于搭建 Jellyfin 10.10.7 插件骨架、SQLite 数据层、配置、Controller / Service / Repository 边界、评分查询与批量接口，并在不确定 Jellyfin API 时通过适配层隔离风险。"
metadata:
  short-description: "Jellyfin Personal Ratings 后端插件 Skill"
---

# Jellyfin Personal Ratings Backend Skill

## 概览

这个 Skill 只关注后端插件实现。适用场景包括：

- 创建或补齐插件骨架
- 设计配置类和插件入口
- 建立 SQLite 初始化、表结构和 Repository
- 实现评分、分页查询、批量操作和删除服务
- 审查 Jellyfin API 适配风险

## 工作流程

1. 先读取 `references/backend-contract.md`。
   - 确认表结构、配置项、API 前缀、推荐目录和服务职责
2. 再读取 `references/backend-workflow.md`。
   - 确认编码方式、实现顺序、验证方式和常见风险
3. 先找当前阶段最小闭环。
   - 阶段 0~1：先做可编译骨架、配置、数据库初始化、单条评分
   - 阶段 2~3：再扩分页查询、批量操作
   - 阶段 6：最后补物理删除与审计
4. 不确定 Jellyfin API 时，不要猜。
   - 优先引入 `Resolver`、`Adapter` 或 `Service` 隔离
   - 在代码与交付说明中标出需要二次核对的点
5. 保持分层边界稳定。
   - Controller 负责入参和权限入口
   - Service 负责业务规则
   - Repository 负责 SQL 与数据持久化

## 什么时候读取 reference

- 需要查表结构、配置项、接口设计、职责分层时，读取 `references/backend-contract.md`
- 需要确认实现顺序、代码风格、验证动作、风险处理时，读取 `references/backend-workflow.md`

## 约束

- 目标框架按文档约束走 `net8.0`
- 默认只适配 **Jellyfin 10.10.7**
- 不要把数据库逻辑写进 Controller 或前端脚本
- 不要硬编码 Docker / Linux 路径
- SQLite 是主存储，不要退化成 JSON 主存储
- C# 局部变量使用显式类型，不使用 `var`
