# 编译与部署说明

本文档面向当前仓库的本地开发、手动部署和首次验证。

## 适用范围

- Jellyfin：**10.10.7**
- .NET SDK：**8.0**
- 插件项目：`src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj`

## 本地编译

在仓库根目录执行：

```bash
dotnet build src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj
```

如果只想生成部署产物：

```bash
dotnet publish src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj -c Release
```

默认发布目录：

```text
src/Jellyfin.Plugin.PersonalRatings/bin/Release/net8.0/publish/
```

## Jellyfin 插件目录

插件部署目录不要硬编码。当前代码通过 Jellyfin `IApplicationPaths.DataPath` 计算路径，规则是：

```text
<Jellyfin DataPath>/plugins/Jellyfin.PersonalRatings/
```

首次初始化后，插件 SQLite 数据库默认位于：

```text
<Jellyfin DataPath>/plugins/Jellyfin.PersonalRatings/personal-ratings.db
```

## 手动部署步骤

1. 停止 Jellyfin，或者确保接下来会重启 Jellyfin。
2. 执行 `dotnet publish`。
3. 创建插件目录：

   ```text
   <Jellyfin DataPath>/plugins/Jellyfin.PersonalRatings/
   ```

4. 将 `publish/` 下的文件复制到上面的插件目录。
5. 启动或重启 Jellyfin。
6. 在 Jellyfin 管理后台确认插件已被识别。

## 首次启动会发生什么

- Jellyfin 加载插件程序集
- 插件注册 Controller、Service、SQLite 连接工厂和 Web 启动过滤器
- `DatabaseInitializationHostedService` 会触发数据库初始化
- 首次访问时会自动创建：
  - `user_item_ratings`
  - `delete_audit_logs`

## Web 端验证建议

部署后建议按下面顺序验证：

1. 打开 Jellyfin Web。
2. 找一个可访问的视频或集合条目。
3. 进入详情页，确认出现“个人评分”面板。
4. 尝试打分、清分、标记待删除。
5. 打开“我的评分库”：

   ```text
   #/configurationpage?name=PersonalRatingsManagePage
   ```

6. 验证分页加载、筛选和批量操作。
7. 使用管理员账号验证物理删除。
8. 确认数据库中出现 `delete_audit_logs` 记录。

## 常见问题

### 1. 详情页没有出现评分面板

先确认：

- 插件已经成功加载
- 当前页面是新的 Jellyfin Web 页签，或已经手动刷新
- 当前条目能被当前用户访问

当前详情页 UI 是通过注入 Jellyfin Web 壳页面实现的，老页签在插件部署前已打开时，通常需要刷新一次。

### 2. 管理页能打开但没有记录

先确认：

- 当前用户是否已经对某些条目打过分
- 查询条件是否过严
- 当前条目是否对该用户可见

### 3. 物理删除返回失败

先确认：

- 当前用户是否为管理员
- 请求体里是否传了 `confirmDelete = true`
- 目标条目是否仍存在
- Jellyfin 本身是否有权删除底层媒体路径

## 升级注意事项

- 当前仓库只验证过 Jellyfin **10.10.7**
- 升级到 10.11.x 之前，应重新核对：
  - `ILibraryManager.DeleteItem(...)`
  - Web 注入点
  - 用户权限读取方式

## 生产前建议

在更广范围部署前，建议先完成以下工作：

- 物理删除权限硬约束收口
- 审计日志可靠性增强
- 自动化测试补齐
- 删除审计查询能力
