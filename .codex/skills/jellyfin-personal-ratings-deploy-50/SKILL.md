---
name: jellyfin-personal-ratings-deploy-50
description: "当你在 Jellyfin Personal Ratings 仓库中需要把插件部署到局域网服务器 50 的 Jellyfin Docker 容器时使用：优先走本地 publish + 远端拉取的稳定流程，规避 scp 经常被重置的问题，并在重启后检查容器健康、插件加载和数据库初始化日志。"
metadata:
  short-description: "Jellyfin Personal Ratings 部署到 50 服务器 Skill"
---

# Jellyfin Personal Ratings Deploy 50 Skill

## 概览

这个 Skill 专门用于把当前插件部署到：

- SSH 别名：`50`
- Jellyfin 容器：`jellyfin`

当用户说“部署到 50”“帮我重启 50 上的 Jellyfin 插件”时，优先使用这个 Skill。

## 工作流程

1. 先读取 `references/server-50-deploy.md`。
2. 本地先执行：
   - `dotnet build`
   - `dotnet test`
   - `dotnet publish -c Release`
3. 不要优先依赖 `scp`。
   - 如果 `scp` 容易 `Connection reset by peer`，改用本地临时 HTTP 服务，让 `50` 自己 `curl` 拉取
4. 在远端覆盖前先备份插件目录。
5. 覆盖插件发布文件后，重启 `jellyfin` 容器。
6. 重启后检查：
   - 容器是否恢复到 `healthy`
   - 插件是否从正式插件目录加载
   - SQLite 初始化日志是否正常
7. 本地临时 HTTP 服务用完后关闭。

## 什么时候读取 reference

- 需要部署、回滚、重启或查看 50 上插件状态时，读取 `references/server-50-deploy.md`

## 约束

- 这个 Skill 只针对当前约定的 **50 服务器**
- 不要把备份目录放进 Jellyfin `plugins/` 根目录，避免被 Jellyfin 误扫成插件
- 不要在未通过本地 `build/test` 时直接部署
- 部署完成后要明确回报：插件目录、备份目录、容器状态、关键日志结论
