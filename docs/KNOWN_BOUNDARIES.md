# 已知边界

本文档记录当前版本已经明确知道、但还没有完全收口的边界与风险。它们不是隐藏问题，发布前应主动评估。

## 1. 版本范围固定在 Jellyfin 10.10.7

当前实现和手动验证都只面向 **Jellyfin 10.10.7**。不应直接把当前结论外推到 10.11.x 或更高版本。

重点受版本影响的点包括：

- `ILibraryManager.DeleteItem(...)`
- Jellyfin Web 壳页面注入方式
- 当前用户权限读取
- 顶栏入口挂载点
- 详情页 DOM 注入点

## 2. 当前只覆盖 Jellyfin Web MVP

已实现的 UI 只覆盖 Jellyfin Web：

- 前台“打分库”浏览页
- 详情页统一操作区
- 评分后台页
- 标签管理页
- 删除审计页

当前不覆盖：

- Android / iOS / TV 原生客户端 UI
- 非 Web 客户端交互适配

## 3. 配置项已接入主流程，但仍保留范围边界

`PluginConfiguration` 中预留了这些配置项：

- `EnableFavoriteSync`
- `FavoriteThreshold`
- `EnableDeleteFeature`
- `RequireAdminForPhysicalDelete`
- `EnableDetailsPageInjection`
- `EnableManagePage`

当前已接线的字段包括：

- `EnableDeleteFeature`
- `EnableDetailsPageInjection`
- `EnableManagePage`

当前语义：

- `EnableDeleteFeature=false` 会隐藏前端删除入口，并阻断后端物理删除接口
- `EnableDetailsPageInjection=false` 会停止详情页注入
- `EnableManagePage=false` 会隐藏前台“打分库”入口、评分后台页、标签管理页和删除审计页入口，并停止相关静态资源暴露

当前仍未处理的字段包括：

- `EnableFavoriteSync`
- `FavoriteThreshold`

## 4. 兼容配置项仍然保留

`PluginConfiguration` 里仍保留了 `RequireAdminForPhysicalDelete` 这个旧字段，但当前代码已经不再用它放开普通用户删除权限。

这意味着：

- 当前实际行为仍然是“物理删除始终要求管理员”
- 该字段现在更偏向兼容旧配置，而不是运行期能力开关

## 5. 审计与删除仍不是单数据库事务

当前实现已经把删除链路改成：

- 删除前先写一条预审计记录
- 预审计写不进去时，直接阻止物理删除
- 删除结果再逐条写最终审计

这样已经避免了“完全没有任何审计就删掉条目”的主要风险。但它仍然不是一个单数据库事务，所以还存在残余边界：

- 如果条目已经被 Jellyfin 删除
- 但最终结果审计或评分清理失败

那么数据库里通常仍会保留预审计记录，但可能需要人工结合日志做后续核对。

## 6. 评分清理只覆盖直接删除的 item id

当前删除成功后，会清理被直接删除条目的评分记录；但如果未来扩展到“目录级删除”“父项带子项级联删除”，当前逻辑还没有同步清理所有后代评分数据。

## 7. `last_played_at` 目前不是持久化同步字段

表结构里有 `last_played_at`，但当前主要依赖 Jellyfin 运行时的用户数据解析：

- `IsPlayed`
- `LastPlayedAt`

这意味着：

- 当前页面和查询能拿到播放状态
- 但插件并没有独立维护一套完整的播放时间同步机制

## 8. 元数据筛选与排序存在内存回退路径

当查询条件依赖 Jellyfin 元数据时，例如：

- `isPlayed`
- `libraryIds`
- `mediaTypes`
- `year`
- `keyword`
- `addedAfterUtc`
- `addedBeforeUtc`
- `name`
- `lastPlayedAt`

当前实现会先取当前用户候选评分记录，再结合 Jellyfin 元数据做补充过滤和排序，而不是全部在 SQLite 中一次完成。

这在 MVP 阶段是可接受的，但在更大数据量下需要进一步优化。

## 9. 前台“打分库”依赖 Jellyfin Web 壳注入

当前主入口虽然已经从配置页转到前台顶栏，但它仍然不是 Jellyfin 内建的原生模块，而是通过注入脚本挂到现有 Web 壳结构上。

当前依赖点包括：

- `.headerTabs`
- `.mainAnimatedPages`
- `#/details`

优点：

- 主入口已经更接近日常浏览路径
- 能复用现有评分查询与详情页能力

代价：

- 仍受 Jellyfin 10.10.7 Web 壳 DOM 结构影响
- 升级到新版本前必须重新回归顶栏注入与页面挂载

## 10. 评分后台页、标签管理页和删除审计页仍然借用 configurationpage 壳

当前 `#/configurationpage?name=PersonalRatingsManagePage`、`#/configurationpage?name=PersonalRatingsTagManagePage` 与 `#/configurationpage?name=PersonalRatingsAuditPage` 仍然保留，但它们现在只作为后台页，不再是产品主入口。

这意味着：

- 日常浏览应走前台“打分库”
- 后台页仍然依赖 Jellyfin 配置页壳结构

## 11. 标签功能目前是一期开口

当前已经实现：

- `tag_definitions`
- `user_item_tags`
- 标签定义 API
- 单条条目标签 API
- 批量加标签 / 移除标签
- `ratings/query` 的 `tagIds` 与 `tagMatchMode`
- 详情页标签交互
- 前台浏览页标签筛选 chips
- 标签管理后台页

当前仍未完全收口：

- 标签定义目前是全局共享，而不是按用户隔离
- 前台卡片与筛选条虽然更接近浏览页，但仍是注入式 MVP 实现
- 标签管理页仍是基础 CRUD 版本，没有更复杂的批量编排与拖拽排序

## 12. 部署后旧页签通常需要刷新

前台“打分库”入口和详情页统一操作区都通过 Web 壳页面注入脚本实现。对于插件安装前已经打开的 Jellyfin Web 页签，通常需要手动刷新一次，才能拿到新注入的前端逻辑。

## 13. 当前已有基础自动化测试，但覆盖率仍有限

仓库里现在已经有正式测试项目，当前主要覆盖：

- 删除链路 service 级回归测试
- 审计查询与删除接口的关键 controller 测试
- 配置开关对页面暴露和注入路径的关键测试

当前仍然没有完整覆盖：

- SQLite Repository 的真实查询与写入行为
- 标签关系查询的真实数据库级测试
- 标签后台页前端交互的端到端自动化
- Web 前端交互的端到端自动化
- 本地 Jellyfin 10.10.7 真实运行时的完整集成回归

所以当前验证仍然建议保留两层：

- `dotnet build` / `dotnet test`
- 本地 Jellyfin 10.10.7 手动联调
