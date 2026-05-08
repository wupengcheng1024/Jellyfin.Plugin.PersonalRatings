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
  "ProductionYear": 2024
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

用于“我的评分库”管理页的分页查询。

请求体字段：

```json
{
  "isRated": true,
  "score": 5,
  "isPlayed": true,
  "isPendingDelete": false,
  "libraryIds": [],
  "mediaTypes": [],
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
- `year`
- `addedAfterUtc`
- `addedBeforeUtc`
- `ratedAfterUtc`
- `ratedBeforeUtc`
- `keyword`

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
      "ProductionYear": 2024
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

## 批量接口通用响应

除 `delete-physical` 外，其他批量接口当前返回 `BatchOperationResponse`，核心字段包括：

- `Operation`
- `RequestedCount`
- `AffectedCount`
- `Items`

## 插件 Web 资源接口

这些接口主要给 Jellyfin Web 壳页面和插件管理页使用，不建议当作外部业务 API 依赖。

- `GET /Plugins/PersonalRatings/web/details-rating.js`
- `GET /Plugins/PersonalRatings/web/manage-page.js`
- `GET /Plugins/PersonalRatings/web/manage-page.css`

## 插件页面入口

- 配置页：`PersonalRatingsConfigPage`
- 管理页：`PersonalRatingsManagePage`
- 管理页前端路由：`#/configurationpage?name=PersonalRatingsManagePage`

## 当前未提供的接口

以下能力尚未实现独立 API：

- 审计日志查询接口
- 删除审计导出接口
- Favorite 同步接口
- 非 Web 客户端专用接口
