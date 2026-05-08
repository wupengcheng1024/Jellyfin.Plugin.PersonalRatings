# 已知边界

本文档记录当前版本已经明确知道、但还没有完全收口的边界与风险。它们不是隐藏问题，发布前应主动评估。

## 1. 版本范围固定在 Jellyfin 10.10.7

当前实现和手动验证都只面向 **Jellyfin 10.10.7**。不应直接把当前结论外推到 10.11.x 或更高版本。

重点受版本影响的点包括：

- `ILibraryManager.DeleteItem(...)`
- Jellyfin Web 壳页面注入方式
- 当前用户权限读取
- 详情页 DOM 注入点

## 2. 当前只覆盖 Jellyfin Web MVP

已实现的 UI 只覆盖 Jellyfin Web：

- 详情页评分面板
- “我的评分库”管理页

当前不覆盖：

- Android / iOS / TV 原生客户端 UI
- 非 Web 客户端交互适配

## 3. 配置项已经预留，但仍有部分未真正接线

`PluginConfiguration` 中预留了这些配置项：

- `EnableFavoriteSync`
- `FavoriteThreshold`
- `EnableDeleteFeature`
- `RequireAdminForPhysicalDelete`
- `EnableDetailsPageInjection`
- `EnableManagePage`

其中有些字段当前只是预留，并没有完整接到所有运行路径。尤其是：

- `EnableDeleteFeature`
- `EnableDetailsPageInjection`
- `EnableManagePage`

当前不应把它们视为“已经完全生效的开关”。

## 4. 物理删除权限仍需进一步收口

项目需求要求“只有管理员可以物理删除”。当前实现虽然默认开启管理员校验，但删除服务里仍保留了 `RequireAdminForPhysicalDelete` 这个可配置入口。

这意味着：

- 生产环境应保持 `RequireAdminForPhysicalDelete = true`
- 更稳妥的后续方向是把管理员限制做成不可绕开的硬约束

## 5. 审计写入与删除结果不是单事务闭环

当前实现会尝试为每个删除结果写入 `delete_audit_logs`，但删除、评分清理和审计入库不是同一个原子事务。

这带来的风险是：

- 如果条目已经被 Jellyfin 删除
- 但后续评分清理或审计写入失败

那么接口层可能返回失败，同时数据库中不一定完整留下对应审计记录。

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

## 9. 管理页仍然借用 Jellyfin 配置页壳

当前“我的评分库”页面是通过：

```text
#/configurationpage?name=PersonalRatingsManagePage
```

挂进 Jellyfin Web 的配置页壳里，而不是一个完全独立的新路由模块。

优点：

- 兼容性较稳
- 插件集成成本低

代价：

- 页面入口仍偏“插件页面”而不是“产品主页”
- 部分前端行为还依赖 Jellyfin 当前壳结构

## 10. 部署后旧页签通常需要刷新

详情页评分 UI 是通过 Web 壳页面注入脚本实现的。对于插件安装前已经打开的 Jellyfin Web 页签，通常需要手动刷新一次，才能拿到新注入的前端逻辑。

## 11. 当前还没有自动化测试项目

仓库里已经预留了 `tests/` 目录，但目前没有正式测试项目。当前验证主要依赖：

- `dotnet build`
- 本地 Jellyfin 10.10.7 手动联调

这足以支撑 MVP 开发，但还不足以支撑长期稳定维护。
