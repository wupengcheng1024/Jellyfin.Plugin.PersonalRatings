# 后端契约

## 推荐结构

```text
src/
  Jellyfin.Plugin.PersonalRatings/
    Configuration/
      PluginConfiguration.cs
    Controllers/
      RatingController.cs
      RatingBatchController.cs
      ConfigController.cs
    Data/
      DatabaseInitializer.cs
      SqliteConnectionFactory.cs
      Repositories/
    Models/
      Entities/
      Requests/
      Responses/
    Services/
      RatingService.cs
      DeletionService.cs
      FavoriteSyncService.cs
      JellyfinItemResolver.cs
    Web/
      details-rating.js
      manage-page.html
      manage-page.js
      manage-page.css
    Plugin.cs
    Jellyfin.Plugin.PersonalRatings.csproj
```

## 数据表

### user_item_ratings

```sql
CREATE TABLE IF NOT EXISTS user_item_ratings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id TEXT NOT NULL,
    item_id TEXT NOT NULL,
    score INTEGER NOT NULL DEFAULT 0,
    is_pending_delete INTEGER NOT NULL DEFAULT 0,
    last_played_at TEXT NULL,
    rated_at TEXT NULL,
    updated_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    UNIQUE(user_id, item_id)
);
```

推荐索引：

```sql
CREATE INDEX IF NOT EXISTS idx_ratings_user_score ON user_item_ratings(user_id, score);
CREATE INDEX IF NOT EXISTS idx_ratings_user_pending ON user_item_ratings(user_id, is_pending_delete);
CREATE INDEX IF NOT EXISTS idx_ratings_user_updated ON user_item_ratings(user_id, updated_at DESC);
```

### delete_audit_logs

```sql
CREATE TABLE IF NOT EXISTS delete_audit_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    operator_user_id TEXT NOT NULL,
    item_id TEXT NOT NULL,
    item_name TEXT NULL,
    action TEXT NOT NULL,
    result TEXT NOT NULL,
    message TEXT NULL,
    created_at TEXT NOT NULL
);
```

## 配置项

- `EnableFavoriteSync`
- `FavoriteThreshold`
- `EnableDeleteFeature`
- `RequireAdminForPhysicalDelete`
- `DefaultPageSize`
- `EnableDetailsPageInjection`
- `EnableManagePage`
- `DatabaseFileName`

## API 前缀与接口

统一前缀：`/Plugins/PersonalRatings`

单条评分：

- `GET /rating?itemId=...`
- `POST /rating`
- `DELETE /rating?itemId=...`

查询：

- `POST /ratings/query`

批量：

- `POST /ratings/batch/set-score`
- `POST /ratings/batch/clear-score`
- `POST /ratings/batch/set-pending-delete`
- `POST /ratings/batch/unset-pending-delete`
- `POST /ratings/batch/delete-physical`

## 分层职责

- `IRatingRepository`
  - 单条查询
  - 插入 / 更新
  - 条件分页查询
  - 批量更新评分
  - 批量设置待删除
- `IRatingService`
  - 校验评分范围
  - 设置评分 / 清分 / 查分
  - 分页查询
  - Favorite 同步
- `IDeletionService`
  - 管理员权限校验
  - 物理删除执行
  - 审计日志写入
- `IJellyfinItemResolver`
  - 解析 ItemId
  - 获取名称、类型、路径和播放信息

## 原始来源

- `jellyfin-personal-ratings-md/03-技术设计初稿.md`
- `jellyfin-personal-ratings-md/05-Codex执行提示词.md`
