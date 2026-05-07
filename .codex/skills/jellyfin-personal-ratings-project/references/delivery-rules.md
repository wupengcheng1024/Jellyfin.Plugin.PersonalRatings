# 交付与工程规则

## 技术方向

- 后端插件负责数据存储、权限校验、查询和删除接口
- Web 前端负责详情页评分 UI 与“我的评分库”管理页
- 主存储使用 SQLite
- 只适配 Jellyfin 10.10.7

## 推荐目录

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

## 阶段顺序

1. 阶段 0：插件骨架与可识别
2. 阶段 1：SQLite 初始化、数据表、单条评分 API
3. 阶段 2：评分管理列表分页查询
4. 阶段 3：批量改分、清分、待删除
5. 阶段 4：详情页评分交互
6. 阶段 5：管理页 UI
7. 阶段 6：物理删除与审计
8. 阶段 7：README、部署说明、TODO 和收尾

## 全局编码要求

1. 先保证编译通过，再逐步补全功能
2. 不要伪造未知的 Jellyfin API
3. 不确定 API 时，用适配层、解析器或 TODO 隔离风险
4. C# 局部变量显式类型，不使用 `var`
5. Controller 保持轻量
6. Repository 负责数据访问
7. 关键 public 方法补 XML 注释
8. 关键异常、批量操作和物理删除要写日志

## 执行方式

- 默认先做最小可运行版本
- 默认优先后端闭环，再补 Web 端体验
- 默认优先视频类条目，其他媒体类型后续扩展
- 每轮交付都要说明：已完成、仍是 TODO、需要二次核对的 Jellyfin API

## 原始来源

- `jellyfin-personal-ratings-md/00-README-给Codex的使用说明.md`
- `jellyfin-personal-ratings-md/03-技术设计初稿.md`
- `jellyfin-personal-ratings-md/04-分阶段开发计划.md`
- `jellyfin-personal-ratings-md/05-Codex执行提示词.md`
