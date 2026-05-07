---
name: jellyfin-personal-ratings-review
description: "当你在 Jellyfin Personal Ratings 仓库中做代码评审、验收、发布前检查或风险排查时使用：用于优先检查 Jellyfin 10.10.7 兼容性、权限边界、分页与批量行为、管理员删除审计以及当前阶段是否真正闭环。"
metadata:
  short-description: "Jellyfin Personal Ratings 评审与验收 Skill"
---

# Jellyfin Personal Ratings Review Skill

## 概览

这个 Skill 用来做 review、验收和风险检查。默认采用“先找问题”的视角，而不是先写总结。

## 工作流程

1. 先读取 `references/review-checklist.md`。
2. 先判断当前改动属于哪个阶段，再按阶段验收。
3. Review 时优先检查：
   - Jellyfin 10.10.7 兼容性
   - 是否伪造了未知 API
   - 用户隔离是否正确
   - 分页是否真实生效
   - 批量操作是否有参数校验和日志
   - 管理员删除是否有权限校验与审计
4. 如果没有发现问题，也要说明残余风险和未验证项。

## 什么时候读取 reference

- 需要做代码评审、验收判断、发布前核对时，读取 `references/review-checklist.md`

## 约束

- Findings 优先，摘要次之
- 不要因为“看起来差不多”就默认 Jellyfin API 可用
- 如果代码还没达到当前阶段的完成标准，要明确指出缺口
