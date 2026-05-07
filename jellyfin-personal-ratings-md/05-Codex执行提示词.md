# Codex 执行提示词

以下内容可直接发给 Codex。

---

你要为 **Jellyfin 10.10.7** 开发一个自定义插件，插件名暂定为：

**Jellyfin.PersonalRatings**

请严格按下面要求执行。

## 一、目标

实现一个“个人评分 + 分级收藏 + 批量管理”的 Jellyfin 插件。

核心功能：

1. 当前登录用户可以对视频条目打 **1~5 分**。
2. 支持清除评分，清除后记为 **0=未评分**。
3. 提供一个“我的评分库”管理页。
4. 管理页支持：
   - 全部已评分
   - 5分 / 4分 / 3分 / 2分 / 1分 / 未评分
   - 待删除
   - 最近评分
   - 已播放未评分
5. 管理页支持筛选、分页、多选和批量操作。
6. 支持待删除标记。
7. 只有管理员可以执行物理删除。
8. 物理删除必须写审计日志。

## 二、技术要求

1. 使用 **C#**。
2. 使用 Jellyfin 官方插件模板风格组织项目。
3. 目标框架：**net8.0**。
4. 第一版只适配 **Jellyfin 10.10.7**。
5. 使用 **SQLite** 作为主存储。
6. 不要用 JSON 文件作为主数据源。
7. 不要硬编码 Docker/Linux 路径。
8. 所有路径通过 Jellyfin 的应用路径接口获取。
9. 先做最小可运行版本，先保证后端正确，再逐步补前端。
10. 不要伪造未知的 Jellyfin API；如果不确定，请封装适配层并标注 TODO。

## 三、代码风格要求

1. 局部变量使用显式类型，不要使用 `var`。
2. 先最小实现，不要过度设计。
3. Controller 只做参数接收、校验和调用 Service。
4. 数据访问逻辑放到 Repository。
5. 关键 public 方法加 XML 注释。
6. 关键异常场景写清楚日志。

## 四、数据表设计

### 表：user_item_ratings

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

### 表：delete_audit_logs

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

## 五、建议目录结构

```text
src/
  Jellyfin.Plugin.PersonalRatings/
    Configuration/
    Controllers/
    Data/
      Repositories/
    Models/
      Entities/
      Requests/
      Responses/
    Services/
    Web/
    Plugin.cs
    PluginConfiguration.cs
    Jellyfin.Plugin.PersonalRatings.csproj
tests/
```

## 六、API 设计

统一前缀：

`/Plugins/PersonalRatings`

### 单条评分接口

- `GET /Plugins/PersonalRatings/rating?itemId=xxx`
- `POST /Plugins/PersonalRatings/rating`
- `DELETE /Plugins/PersonalRatings/rating?itemId=xxx`

### 查询接口

- `POST /Plugins/PersonalRatings/ratings/query`

### 批量接口

- `POST /Plugins/PersonalRatings/ratings/batch/set-score`
- `POST /Plugins/PersonalRatings/ratings/batch/clear-score`
- `POST /Plugins/PersonalRatings/ratings/batch/set-pending-delete`
- `POST /Plugins/PersonalRatings/ratings/batch/unset-pending-delete`
- `POST /Plugins/PersonalRatings/ratings/batch/delete-physical`

## 七、权限要求

- 普通用户：
  - 查看自己的评分
  - 修改自己的评分
  - 清除自己的评分
  - 标记/取消待删除

- 管理员：
  - 拥有上述全部权限
  - 可以调用物理删除接口

## 八、第一阶段交付要求

请先只完成以下内容：

1. 可编译的插件骨架
2. SQLite 初始化
3. `user_item_ratings` 与 `delete_audit_logs` 建表
4. 单条评分查询接口
5. 单条评分设置接口
6. 单条评分清除接口
7. 基础分页查询接口骨架

此阶段先不要把前端做复杂。

## 九、输出要求

请你输出：

1. 完整目录树
2. 所有新增文件代码
3. 关键类说明
4. 当前仍需二次确认的 Jellyfin API 列表
5. 编译与部署说明

## 十、注意事项

1. 先让项目通过编译。
2. 不确定的 Jellyfin API 不要乱写。
3. 避免让插件和 Jellyfin 10.11 的接口耦合。
4. 保持模块边界清晰。
5. 不要试图一次性把所有功能都写满。

---

如果这是第 1 轮，请只完成“后端最小可运行版本”。

