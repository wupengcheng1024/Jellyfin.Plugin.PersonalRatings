# Jellyfin Personal Ratings

`Jellyfin.PersonalRatings` 是一个面向 **Jellyfin 10.10.7** 的自定义插件，用来给当前登录用户提供私有评分、分级收藏、待删除标记和批量管理能力。

当前仓库已经完成阶段 0 ~ 6 的最小闭环：

- 插件骨架、配置类和服务注册
- SQLite 初始化与建表
- 单条评分查询、设置、清除
- 分页查询与批量评分/清分/待删除
- 详情页评分 UI
- “我的评分库”管理页
- 管理员物理删除与删除审计日志

## 当前范围

- 目标 Jellyfin 版本：**10.10.7**
- 目标运行时：**net8.0**
- 主存储：**SQLite**
- 当前 UI 范围：**Jellyfin Web**
- 当前主流程：`看过 -> 评分 -> 筛选 -> 批量处理 -> 管理员物理删除`

## 核心能力

- 当前登录用户可对条目打 `1~5` 分
- 清除评分后回到 `0 = 未评分`
- 每个 `UserId + ItemId` 只保留一条有效评分记录
- 支持待删除标记与取消待删除
- 提供“我的评分库”管理页
- 支持分页、筛选、多选和批量操作
- 只有管理员可以调用物理删除接口
- 物理删除会写入 `delete_audit_logs`

## 目录结构

```text
.
├── AGENTS.md
├── README.md
├── TODO.md
├── docs/
│   ├── API.md
│   ├── DEPLOYMENT.md
│   └── KNOWN_BOUNDARIES.md
├── jellyfin-personal-ratings-md/
│   ├── 00-README-给Codex的使用说明.md
│   ├── 01-项目背景与约束.md
│   ├── 02-产品需求文档-PRD.md
│   ├── 03-技术设计初稿.md
│   ├── 04-分阶段开发计划.md
│   ├── 05-Codex执行提示词.md
│   └── 99-参考资料.md
├── src/
│   └── Jellyfin.Plugin.PersonalRatings/
│       ├── Configuration/
│       ├── Controllers/
│       ├── Data/
│       ├── Infrastructure/
│       ├── Models/
│       ├── Services/
│       ├── Web/
│       ├── Jellyfin.Plugin.PersonalRatings.csproj
│       ├── Plugin.cs
│       └── PluginServiceRegistrator.cs
└── tests/
```

## 快速开始

```bash
dotnet build src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj
dotnet publish src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj -c Release
```

发布产物默认在：

```text
src/Jellyfin.Plugin.PersonalRatings/bin/Release/net8.0/publish/
```

部署步骤、插件目录位置和首次验证清单见 [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)。

## Web 入口

- 插件配置页：`PersonalRatingsConfigPage`
- 管理页：`#/configurationpage?name=PersonalRatingsManagePage`
- 详情页评分 UI：通过插件中间件向 Jellyfin Web 壳页面注入 `details-rating.js`

## 数据存储

SQLite 数据库路径通过 Jellyfin `IApplicationPaths.DataPath` 计算，不硬编码容器或系统路径。

默认数据库文件位置：

```text
<Jellyfin DataPath>/plugins/Jellyfin.PersonalRatings/personal-ratings.db
```

主要数据表：

- `user_item_ratings`
- `delete_audit_logs`

## 文档索引

- 接口说明：[`docs/API.md`](docs/API.md)
- 编译与部署：[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)
- 已知边界：[`docs/KNOWN_BOUNDARIES.md`](docs/KNOWN_BOUNDARIES.md)
- 后续待办：[`TODO.md`](TODO.md)

## 当前交付结论

当前仓库适合作为 **Jellyfin 10.10.7 Web MVP** 继续迭代，但还不应把它视为“所有边界都已经完全收口”的正式稳定版。发布前请先阅读 [`docs/KNOWN_BOUNDARIES.md`](docs/KNOWN_BOUNDARIES.md)。
