# Jellyfin Personal Ratings Agent

## 使命

这个仓库用于开发 **Jellyfin 10.10.7** 的 `Jellyfin.PersonalRatings` 插件，目标是提供：

- 按 Jellyfin 用户隔离的私有评分，范围 `0~5`
- 用评分替代原生 Favorite 的粗粒度收藏
- 评分管理页、筛选、分页与批量操作
- 待删除标记
- 仅管理员可执行的物理删除与审计日志

当前优先服务 **Jellyfin Web 端**，并始终以“先能编译、先稳后全”为第一原则。

## 规范优先级

1. 根目录 `AGENTS.md`
2. `.codex/skills/` 下的项目 Skill 与 references
3. `jellyfin-personal-ratings-md/` 下的原始需求文档

如果第 2 层与第 3 层出现冲突，以原始需求文档为准，并同步回补 Skill / reference。

## 不可违反的约束

1. 目标版本固定为 **Jellyfin 10.10.7**，除非用户明确扩版本。
2. **先保证可编译，再补功能。**
3. **先后端，再前端体验。**
4. 不要伪造未知的 Jellyfin API；不确定时必须放入适配层、解析器或 TODO。
5. 主存储必须是 **SQLite**，不要把 JSON 当主数据源。
6. 不要硬编码 Docker 或 Linux 路径，所有路径通过 Jellyfin 提供的应用路径接口获取。
7. 评分按 Jellyfin 用户维度存储；`0` 表示未评分，`1~5` 为有效评分。
8. `UserId + ItemId` 只能有一条有效评分记录。
9. 待删除只是标签，不等于真实删除。
10. 物理删除必须满足管理员权限，并记录审计日志。
11. C# 局部变量使用显式类型，不使用 `var`。
12. Controller 只做参数接收、基础校验和调用 Service；数据库访问逻辑放 Repository。

## 推荐工作顺序

1. 阶段 0：插件骨架、配置类、目录结构、可识别
2. 阶段 1：SQLite 初始化、数据表、单条评分 API
3. 阶段 2：评分列表分页查询
4. 阶段 3：批量改分、清分、待删除
5. 阶段 4：详情页评分 UI
6. 阶段 5：管理页 UI
7. 阶段 6：物理删除与审计
8. 阶段 7：文档与交付收尾

除非用户明确要求跨阶段推进，否则优先完成当前最小阶段闭环，不要一次性铺满全部功能。

## Skill 路由

- 任务开始、范围校对、阶段判断：读取 `.codex/skills/jellyfin-personal-ratings-project/SKILL.md`
- 后端插件、SQLite、API、Repository / Service：读取 `.codex/skills/jellyfin-personal-ratings-backend/SKILL.md`
- Web 详情页注入、管理页、静态资源：读取 `.codex/skills/jellyfin-personal-ratings-web/SKILL.md`
- 代码评审、验收、发布前检查：读取 `.codex/skills/jellyfin-personal-ratings-review/SKILL.md`

## 验证要求

1. 有 `.csproj` 之后，优先执行定向 `dotnet build`。
2. 如果已存在测试项目，优先跑与改动范围直接相关的测试。
3. 无法验证时，要明确写出原因、当前风险和下一步建议。
4. 不要为了“看起来完整”而跳过版本兼容、权限边界或分页验证。

## 文档与命名约定

1. 面向人的说明默认使用中文。
2. Skill slug、目录名、文件名、类名、接口名继续使用英文或现有工程命名。
3. `jellyfin-personal-ratings-md/` 是原始策划输入；`.codex/skills/` 是面向 Agent 的精简执行层。
4. 当需求发生变化时，先更新原始文档，再同步更新 Skill references。
