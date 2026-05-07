---
name: jellyfin-personal-ratings-web
description: "当你在 Jellyfin Personal Ratings 仓库中实现或审查 Web 端改动时使用：用于详情页评分注入、管理页静态资源、筛选与批量交互设计，并确保前端只建立在已确认的后端接口与权限边界之上。"
metadata:
  short-description: "Jellyfin Personal Ratings Web 交互 Skill"
---

# Jellyfin Personal Ratings Web Skill

## 概览

这个 Skill 用来处理 Jellyfin Web 端的最小可用交互，重点是：

- 详情页评分组件
- 清除评分
- 待删除切换
- 管理页筛选、分页、多选和批量操作

默认保持轻量，不要在第一版引入超出需求的重型前端工程。

## 工作流程

1. 先读取 `references/web-contract.md`。
   - 确认详情页和管理页最少要展示什么、依赖哪些后端能力
2. 先检查后端接口是否已经存在。
   - 如果评分、查询或批量接口未就绪，先补后端或标出依赖
3. 优先做最小静态资源。
   - 优先 `details-rating.js`
   - 再做 `manage-page.html`、`manage-page.js`、`manage-page.css`
4. 交互必须尊重权限。
   - 普通用户不能看到或触发管理员物理删除
   - UI 状态要能反映当前评分和待删除状态
5. 保持渐进增强。
   - 不依赖不存在的 Jellyfin 前端扩展点
   - 不确定注入点时，用最小原型验证并保留降级空间

## 什么时候读取 reference

- 需要确认详情页元素、管理页筛选项、批量操作和 UI 限制时，读取 `references/web-contract.md`

## 约束

- 第一版只保证 **Jellyfin Web 端**
- 先服务已有后端能力，不要反过来让前端定义后端契约
- 不要把数据库或删除逻辑写到前端脚本里
- 不要为了 UI 完整度破坏“先最小可运行版本”的原则
