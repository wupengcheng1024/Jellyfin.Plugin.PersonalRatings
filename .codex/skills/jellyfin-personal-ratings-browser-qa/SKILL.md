---
name: jellyfin-personal-ratings-browser-qa
description: "当你在 Jellyfin Personal Ratings 仓库中需要对真实 Jellyfin Web 页面做稳定截图、实机交互验证、前台/后台路由回归或缓存排查时使用：优先处理 10.10.7 Web 壳下的真实页面，而不是只看静态代码或不稳定的浏览器快照。"
metadata:
  short-description: "Jellyfin Personal Ratings 实机截图与 Web 验证 Skill"
---

# Jellyfin Personal Ratings Browser QA Skill

## 概览

这个 Skill 用来做 **Jellyfin 10.10.7 Web** 的真实页面验证，重点是：

- 稳定截图
- 真实路由回归
- 前台 / 后台页面联调
- 浏览器缓存与旧静态资源排查

当用户说“你自己截图看看”“你去页面上测一测”“这个 UI 看着不对”时，优先使用这个 Skill。

## 工作流程

1. 先读取 `references/stable-screenshot.md`。
   - 确认可复用的截图方式、临时目录、登录方式和回归清单
2. 先用当前可用浏览器快速确认问题是否可复现。
   - 轻量查看、点点页面时，可先用内置浏览器
3. 一旦需要**稳定截图**或页面状态不可信，切到本地 Chrome + `puppeteer-core`。
   - 不要只依赖内置浏览器的截图结果
4. 截图前先确认目标链路。
   - 例如：`#/personalratings`
   - 例如：`打分库 -> 详情页 -> 返回`
   - 例如：`打分库 -> 评分后台`
5. 截图后用 `view_image` 检查结果，再决定是否改代码。
6. 如果页面像“没更新”，优先排查静态资源缓存。
   - 先看当前页面是否加载到最新 `css/js`
   - 必要时补资源版本戳，而不是要求用户频繁强刷

## 什么时候读取 reference

- 需要真实截图、稳定复现或页面联调时，读取 `references/stable-screenshot.md`

## 约束

- 只面向 **Jellyfin 10.10.7 Web**
- 不要把截图成功当成逻辑成功；要同时看路由、摘要文本、状态文本和实际页面结构
- 不要把用户账号密码写进仓库文件
- 如果真实页面没验证到，就明确说明“只完成了代码层验证”
