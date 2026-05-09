# 编译与部署说明

本文档面向当前仓库的本地开发、手动部署和首次验证。

## 适用范围

- Jellyfin：**10.10.7**
- .NET SDK：**8.0**
- 插件项目：`src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj`
- 当前 UI 覆盖：**Jellyfin Web MVP**

## 本地编译

在仓库根目录执行：

```bash
dotnet build src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj
dotnet test tests/Jellyfin.Plugin.PersonalRatings.Tests/Jellyfin.Plugin.PersonalRatings.Tests.csproj
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
  - `tag_definitions`
  - `user_item_tags`

## Web 端验证建议

部署后建议按下面顺序验证：

1. 打开 Jellyfin Web。
2. 确认顶栏出现“打分库”入口。
3. 进入：

   ```text
   #/personalratings
   ```

4. 验证前台浏览页能正常加载卡片流。
5. 切换海报视图 / 列表视图，确认页面能正常切换。
6. 试用评分、播放状态、类型、排序、搜索等筛选。
7. 如果已经存在标签定义，确认顶部标签 chips 能加载并参与筛选。
8. 点击任意卡片，确认能跳回原始 Jellyfin 详情页。
9. 在详情页确认出现统一操作区，并验证：
   - 打分
   - 待删除
   - 标签占位交互
10. 打开评分后台：

    ```text
    #/configurationpage?name=PersonalRatingsManagePage
    ```

11. 验证分页加载、批量操作和后台入口仍可用。
12. 使用管理员账号验证物理删除。
13. 打开删除审计页并确认能看到刚才的删除记录：

    ```text
    #/configurationpage?name=PersonalRatingsAuditPage
    ```

14. 确认数据库中出现 `delete_audit_logs` 记录。

## 常见问题

### 1. 顶栏没有出现“打分库”

先确认：

- 插件已经成功加载
- 当前页面是新的 Jellyfin Web 页签，或已经手动刷新
- 当前插件配置里的 `EnableManagePage` 为 `true`

当前前台入口是通过 Jellyfin Web 壳页面注入实现的，老页签在插件部署前已打开时，通常需要刷新一次。

### 2. 详情页没有出现统一操作区

先确认：

- 当前插件配置里的 `EnableDetailsPageInjection` 为 `true`
- 当前页面已经刷新到新资源
- 当前条目能被当前用户访问

### 3. 打分库页面能打开但没有记录

先确认：

- 当前用户是否已经对某些条目打过分，或通过标签交互生成过关系记录
- 查询条件是否过严
- 当前条目是否对该用户可见

### 4. 标签区域为空

先确认：

- 当前是否已经创建启用中的标签定义
- `GET /Plugins/PersonalRatings/tags` 是否正常返回
- 当前用户是否能访问目标条目

### 5. 物理删除返回失败

先确认：

- 当前用户是否为管理员
- 请求体里是否传了 `confirmDelete = true`
- 当前插件配置是否启用了 `EnableDeleteFeature`
- 目标条目是否仍存在
- Jellyfin 本身是否有权删除底层媒体路径

### 6. 打分库入口、评分后台或删除审计页一起消失

先确认：

- 当前插件配置里的 `EnableManagePage` 是否为 `true`
- 当前页面是否已经刷新到新版本前端资源

## 升级注意事项

- 当前仓库只验证过 Jellyfin **10.10.7**
- 升级到 10.11.x 之前，应重新核对：
  - `ILibraryManager.DeleteItem(...)`
  - Web 注入点
  - 顶栏入口挂载点
  - 用户权限读取方式

## 当前验证基线

当前仓库建议至少保留这 4 步作为每次修改后的快速验证：

- `dotnet build src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj`
- `dotnet test tests/Jellyfin.Plugin.PersonalRatings.Tests/Jellyfin.Plugin.PersonalRatings.Tests.csproj`
- 前台“打分库”页手动联调
- 详情页统一操作区、评分后台页和删除审计页手动联调
