# API 说明

本文档描述当前 `Jellyfin.PersonalRatings` 插件已经实现的 HTTP 接口。除非另有说明，接口都只面向 **Jellyfin 10.10.7**。

## 通用约定

- 基础前缀：`/Plugins/PersonalRatings`
- 鉴权方式：依赖 Jellyfin 当前登录上下文
- 默认只操作“当前登录用户”的评分数据
- `itemId` 必须是 Jellyfin 条目的 GUID
- 评分取值：
  - `0` 表示未评分
  - `1~5` 表示有效评分
- 标签定义是全局共享维度；标签关系按 `UserId + ItemId + TagId` 存储

## 单条评分接口

### `GET /Plugins/PersonalRatings/rating?itemId=<guid>`

查询当前用户对指定条目的评分状态。

成功返回示例：

```json
{
  "ItemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
  "Score": 4,
  "IsPendingDelete": false,
  "LastPlayedAt": "2026-05-08T03:52:28.0000000+00:00",
  "IsPlayed": true,
  "RatedAt": "2026-05-08T04:12:10.0000000+00:00",
  "UpdatedAt": "2026-05-08T04:12:10.0000000+00:00",
  "CreatedAt": "2026-05-08T04:12:10.0000000+00:00",
  "ItemName": "Example Item",
  "MediaType": "Video",
  "ItemType": "Movie",
  "ProductionYear": 2024,
  "Tags": [
    {
      "Id": 1,
      "Name": "重看",
      "Color": "#d88b2f"
    }
  ]
}
```

可能返回：

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `404 Not Found`

### `POST /Plugins/PersonalRatings/rating`

为当前用户设置单条评分。

请求体：

```json
{
  "itemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
  "score": 5
}
```

可能返回：

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `404 Not Found`

### `DELETE /Plugins/PersonalRatings/rating?itemId=<guid>`

清除当前用户对指定条目的评分。清除后会返回 `Score = 0` 的响应体。

可能返回：

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `404 Not Found`

## 分页查询接口

### `POST /Plugins/PersonalRatings/ratings/query`

用于前台“打分库”浏览页和评分后台页的分页查询。

当前后台页会直接复用这组字段完成标签筛选：

- `tagIds`
- `tagMatchMode`

本轮没有新增标签相关接口，主要是把评分后台里现有的标签筛选和批量标签交互收口到更清晰的使用体验。

请求体字段：

```json
{
  "isRated": true,
  "score": 5,
  "isPlayed": true,
  "isPendingDelete": false,
  "libraryIds": [],
  "mediaTypes": [],
  "tagIds": [1, 2],
  "tagMatchMode": "any",
  "year": 2024,
  "addedAfterUtc": null,
  "addedBeforeUtc": null,
  "ratedAfterUtc": null,
  "ratedBeforeUtc": null,
  "keyword": "sample",
  "sortBy": "updatedAt",
  "sortOrder": "desc",
  "pageNumber": 1,
  "pageSize": 25
}
```

当前支持的主要筛选字段：

- `isRated`
- `score`
- `isPlayed`
- `isPendingDelete`
- `libraryIds`
- `mediaTypes`
- `tagIds`
- `tagMatchMode`
- `year`
- `addedAfterUtc`
- `addedBeforeUtc`
- `ratedAfterUtc`
- `ratedBeforeUtc`
- `keyword`

`tagMatchMode` 当前支持：

- `any`
- `all`

当前支持的主要排序字段：

- `updatedAt`
- `createdAt`
- `ratedAt`
- `score`
- `lastPlayedAt`
- `name`
- `year`
- `dateAdded`

成功返回结构：

```json
{
  "Items": [
    {
      "ItemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
      "Score": 5,
      "IsPendingDelete": false,
      "LastPlayedAt": null,
      "IsPlayed": false,
      "RatedAt": "2026-05-08T04:12:10.0000000+00:00",
      "UpdatedAt": "2026-05-08T04:12:10.0000000+00:00",
      "CreatedAt": "2026-05-08T04:12:10.0000000+00:00",
      "ItemName": "Example Item",
      "MediaType": "Video",
      "ItemType": "Movie",
      "ProductionYear": 2024,
      "Tags": [
        {
          "Id": 1,
          "Name": "重看",
          "Color": "#d88b2f"
        }
      ]
    }
  ],
  "TotalCount": 1,
  "PageNumber": 1,
  "PageSize": 25
}
```

可能返回：

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`

## 批量接口

所有批量接口前缀：

```text
/Plugins/PersonalRatings/ratings/batch
```

### `POST /set-score`

批量设置评分。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ],
  "score": 4
}
```

### `POST /clear-score`

批量清除评分。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ]
}
```

### `POST /set-pending-delete`

批量标记待删除。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ]
}
```

### `POST /unset-pending-delete`

批量取消待删除。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ]
}
```

### `POST /add-tags`

批量为当前用户的条目增加标签。

当前评分后台页会复用这个接口完成“多选条目后批量添加标签”。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ],
  "tagIds": [1, 2]
}
```

### `POST /remove-tags`

批量为当前用户的条目移除标签。

当前评分后台页会复用这个接口完成“多选条目后批量移除标签”。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ],
  "tagIds": [1]
}
```

### `POST /delete-physical`

管理员物理删除接口。

请求体：

```json
{
  "itemIds": [
    "guid-1",
    "guid-2"
  ],
  "confirmDelete": true
}
```

说明：

- 当前实现要求 `confirmDelete = true`
- 当前实现对物理删除执行**硬性管理员限制**
- 删除前会先校验审计写入能力，无法写审计时会阻止真正删除
- 删除结果会返回逐条状态，并写入或尝试补齐 `delete_audit_logs`

成功返回结构：

```json
{
  "Operation": "deletePhysical",
  "RequestedCount": 2,
  "DeletedCount": 1,
  "FailedCount": 1,
  "AttentionCount": 1,
  "Items": [
    {
      "ItemId": "guid-1",
      "ItemName": "Deleted Item",
      "Result": "deleted",
      "AuditStatus": "completed",
      "Message": "The item was deleted from Jellyfin.",
      "SuggestedAction": null
    },
    {
      "ItemId": "guid-2",
      "ItemName": "Missing Item",
      "Result": "notFound",
      "AuditStatus": "completed",
      "Message": "The item no longer exists or cannot be resolved by Jellyfin.",
      "SuggestedAction": "刷新当前列表并确认该条目仍存在于 Jellyfin 中，然后再决定是否重试。"
    }
  ]
}
```

`delete-physical` 的单条结果当前会附带这些额外字段：

- `AuditStatus`
  - `none`
  - `attemptLogged`
  - `completed`
- `SuggestedAction`

当前常见 `Result` 值包括：

- `deleted`
- `deleteFailed`
- `notFound`
- `auditUnavailable`
- `forbidden`

当 `EnableDeleteFeature = false` 时：

- 前端不再显示物理删除入口
- 后端会阻断该接口，并返回 `409 Conflict`

## 批量接口通用响应

除 `delete-physical` 外，其他批量接口当前返回 `BatchOperationResponse`，核心字段包括：

- `Operation`
- `RequestedCount`
- `AffectedCount`
- `Items`

`Items` 中的单条记录当前会包含：

- 评分状态
- 待删除状态
- 元数据摘要
- 当前标签列表

## 标签定义管理接口

### `GET /Plugins/PersonalRatings/tags`

查询标签定义。

查询参数：

- `includeDisabled`

说明：

- 默认 `includeDisabled=false`
- 当前登录用户可读取启用中的标签定义
- `includeDisabled=true` 默认只允许管理员

成功返回示例：

```json
[
  {
    "Id": 1,
    "Name": "重看",
    "Color": "#d88b2f",
    "SortOrder": 10,
    "IsEnabled": true,
    "CreatedAt": "2026-05-09T01:00:00.0000000+00:00",
    "UpdatedAt": "2026-05-09T01:00:00.0000000+00:00"
  }
]
```

### `POST /Plugins/PersonalRatings/tags`

创建标签定义。当前默认只允许管理员。

请求体：

```json
{
  "name": "重看",
  "color": "#d88b2f",
  "sortOrder": 10,
  "isEnabled": true
}
```

### `PUT /Plugins/PersonalRatings/tags/{id}`

更新标签定义。当前默认只允许管理员。

请求体：

```json
{
  "name": "年度候选",
  "color": "#7aa1d2",
  "sortOrder": 20,
  "isEnabled": true
}
```

### `DELETE /Plugins/PersonalRatings/tags/{id}`

删除标签定义。当前默认只允许管理员。

说明：

- 删除标签定义时，会同步清理 `user_item_tags` 中对应关系

## 条目标签接口

### `GET /Plugins/PersonalRatings/item-tags?itemId=<guid>`

查询当前用户对指定条目的标签。

成功返回示例：

```json
{
  "ItemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
  "Tags": [
    {
      "Id": 1,
      "Name": "重看",
      "Color": "#d88b2f"
    }
  ]
}
```

### `PUT /Plugins/PersonalRatings/item-tags`

覆盖写入当前用户对单条条目的标签关系。

请求体：

```json
{
  "itemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
  "tagIds": [1, 3]
}
```

说明：

- 传空数组表示清空该条目的全部标签
- 当前只允许写入已启用的标签定义

## 删除审计查询接口

### `POST /Plugins/PersonalRatings/audit-logs/query`

按分页查询删除审计日志。当前默认只允许管理员访问。

请求体字段：

```json
{
  "createdAfterUtc": null,
  "createdBeforeUtc": null,
  "result": "deleted",
  "keyword": "movie",
  "itemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
  "pageNumber": 1,
  "pageSize": 25
}
```

当前支持的最小筛选字段：

- `createdAfterUtc`
- `createdBeforeUtc`
- `result`
- `keyword`
- `itemId`

成功返回结构：

```json
{
  "Items": [
    {
      "Id": 12,
      "OperatorUserId": "1c9f048a-3115-4b46-9d7f-1f96a5b714d7",
      "ItemId": "c3fbc7d0-415f-f29d-39d3-099f8db52663",
      "ItemName": "Example Item",
      "Action": "deletePhysical",
      "Result": "deleted",
      "Message": "The item was deleted from Jellyfin.",
      "CreatedAt": "2026-05-08T05:12:10.0000000+00:00"
    }
  ],
  "TotalCount": 1,
  "PageNumber": 1,
  "PageSize": 25
}
```

可能返回：

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`

## 插件功能开关快照接口

### `GET /Plugins/PersonalRatings/features`

返回当前进程内已经生效的插件功能开关快照，主要供 Jellyfin Web 前台入口、详情页和后台页做渐进降级。

成功返回结构：

```json
{
  "IsDeleteFeatureEnabled": true,
  "IsDetailsPageInjectionEnabled": true,
  "IsManagePageEnabled": true
}
```

## 插件 Web 资源接口

这些接口主要给 Jellyfin Web 壳页面和插件前端页面使用，不建议当作外部业务 API 依赖。

当前主要包括：

- `GET /Plugins/PersonalRatings/web/browse-state.js`
- `GET /Plugins/PersonalRatings/web/browse-api.js`
- `GET /Plugins/PersonalRatings/web/browse-render.js`
- `GET /Plugins/PersonalRatings/web/browse-filters.js`
- `GET /Plugins/PersonalRatings/web/details-rating.js`
- `GET /Plugins/PersonalRatings/web/details-api.js`
- `GET /Plugins/PersonalRatings/web/details-panel.js`
- `GET /Plugins/PersonalRatings/web/browse-shell.js`
- `GET /Plugins/PersonalRatings/web/browse-page.css`
- `GET /Plugins/PersonalRatings/web/manage-page.js`
- `GET /Plugins/PersonalRatings/web/manage-page.css`
- `GET /Plugins/PersonalRatings/web/audit-page.js`
- `GET /Plugins/PersonalRatings/web/tag-manage-page.js`
- `GET /Plugins/PersonalRatings/web/tag-manage-page.css`

开关行为：

- 当 `EnableDetailsPageInjection = false` 时，`details-rating.js` 不再注入，且资源接口返回 `404`
- 当 `EnableManagePage = false` 时，前台“打分库”入口脚本、评分后台页、标签管理页和删除审计页相关资源接口都会返回 `404`

## 插件页面入口

- 前台主入口：Jellyfin Web 顶栏“打分库”
- 前台路由：`#/personalratings`
- 配置页：`PersonalRatingsConfigPage`
- 评分后台页：`PersonalRatingsManagePage`
- 评分后台前端路由：`#/configurationpage?name=PersonalRatingsManagePage`
- 评分后台当前支持：批量评分、清分、待删除切换、批量添加标签、批量移除标签
- 标签管理页：`PersonalRatingsTagManagePage`
- 标签管理前端路由：`#/configurationpage?name=PersonalRatingsTagManagePage`
- 删除审计页：`PersonalRatingsAuditPage`
- 删除审计前端路由：`#/configurationpage?name=PersonalRatingsAuditPage`

## 当前未提供的接口

以下能力尚未实现独立 API：

- 删除审计导出接口
- Favorite 同步接口
- 非 Web 客户端专用接口
