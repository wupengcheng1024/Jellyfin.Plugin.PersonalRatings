# Jellyfin Personal Ratings

`Jellyfin.PersonalRatings` 是一个面向 **Jellyfin 10.10.7** 的自定义插件，用来给当前登录用户提供私有评分、分级收藏、标签、待删除标记和批量管理能力。

当前仓库已经完成阶段 0 ~ 6 的最小闭环，并在 v2 中把“打分库”主入口前移到 Jellyfin Web 顶栏。

## 当前范围

- 目标 Jellyfin 版本：**10.10.7**
- 目标运行时：**net8.0**
- 主存储：**SQLite**
- 当前 UI 范围：**Jellyfin Web MVP**
- 当前主流程：`浏览打分库 -> 评分/打标签 -> 筛选 -> 批量处理 -> 管理员物理删除`

## 核心能力

- 当前登录用户可对条目打 `1~5` 分
- 清除评分后回到 `0 = 未评分`
- 每个 `UserId + ItemId` 只保留一条有效评分记录
- 支持待删除标记与取消待删除
- 支持全局标签定义与用户条目标签关系
- 提供前台“打分库”浏览页，支持海报视图 / 列表视图切换
- 提供评分后台页、标签管理页与删除审计页
- 支持分页、筛选、多选和批量操作
- 只有管理员可以调用物理删除接口
- 物理删除会写入 `delete_audit_logs`
- 管理员可通过审计查询 API 和简易审计页查看删除记录

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
dotnet test tests/Jellyfin.Plugin.PersonalRatings.Tests/Jellyfin.Plugin.PersonalRatings.Tests.csproj
dotnet publish src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj -c Release
```

发布产物默认在：

```text
src/Jellyfin.Plugin.PersonalRatings/bin/Release/net8.0/publish/
```

部署步骤、插件目录位置和首次验证清单见 [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)。

## Web 入口

- 前台主入口：Jellyfin Web 顶栏“打分库”
- 前台路由：`#/personalratings`
- 配置页：`PersonalRatingsConfigPage`
- 评分后台页：`#/configurationpage?name=PersonalRatingsManagePage`
- 标签管理页：`#/configurationpage?name=PersonalRatingsTagManagePage`
- 删除审计页：`#/configurationpage?name=PersonalRatingsAuditPage`
- 详情页统一操作区：通过插件中间件向 Jellyfin Web 壳页面注入 `details-rating.js`

当前入口语义：

- “打分库”是主浏览入口，面向日常评分、筛选和卡片浏览
- `configurationpage` 仅保留评分后台、标签管理和删除审计等后台用途，不再作为主入口

当前配置开关行为：

- `EnableDeleteFeature=false` 时，前端隐藏物理删除入口，后端 `delete-physical` 会直接阻断
- `EnableDetailsPageInjection=false` 时，不再注入 `details-rating.js`
- `EnableManagePage=false` 时，不再注入前台“打分库”入口，也不再暴露评分后台页、标签管理页、删除审计页及其相关前端资源
- `RequireAdminForPhysicalDelete` 仅保留为兼容字段，不会重新放开普通用户物理删除

## 数据存储

SQLite 数据库路径通过 Jellyfin `IApplicationPaths.DataPath` 计算，不硬编码容器或系统路径。

默认数据库文件位置：

```text
<Jellyfin DataPath>/plugins/Jellyfin.PersonalRatings/personal-ratings.db
```

主要数据表：

- `user_item_ratings`
- `delete_audit_logs`
- `tag_definitions`
- `user_item_tags`

## 当前 API 覆盖

- 单条评分查询 / 设置 / 清除
- `ratings/query` 分页查询
- 批量评分 / 清分 / 待删除 / 物理删除
- 批量添加标签 / 移除标签
- 标签定义管理 API
- 单条条目标签查询 / 覆盖写入 API
- 删除审计分页查询 API
- 功能开关快照 API

## 本轮前端结构

当前前端仍保持轻量原生脚本，但已经按职责拆分为多个模块：

- 浏览页壳与路由初始化
- 浏览页 API 访问层
- 浏览页状态管理
- 浏览页结果渲染
- 浏览页筛选条与标签筛选
- 详情页 API 访问层
- 详情页统一操作区渲染与交互

## 文档索引

- 接口说明：[`docs/API.md`](docs/API.md)
- 编译与部署：[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)
- 已知边界：[`docs/KNOWN_BOUNDARIES.md`](docs/KNOWN_BOUNDARIES.md)
- 后续待办：[`TODO.md`](TODO.md)

## 当前交付结论

当前仓库适合作为 **Jellyfin 10.10.7 Web MVP** 继续迭代，当前主入口已经切到前台“打分库”，并补上了标签一期的表结构与基础 API。

同时需要明确：

- 只支持 **Jellyfin 10.10.7**
- 只覆盖 **Web MVP**
- 大数据量下，部分元数据筛选和排序仍存在内存回退路径

发布前请先阅读 [`docs/KNOWN_BOUNDARIES.md`](docs/KNOWN_BOUNDARIES.md)。
