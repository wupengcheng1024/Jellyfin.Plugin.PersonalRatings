# 评审与验收清单

## 通用高优先级检查

1. 是否仍只面向 Jellyfin 10.10.7
2. 是否伪造或假设了未知 Jellyfin API
3. 是否保持了 Controller / Service / Repository 边界
4. 是否把 SQLite 作为主存储
5. 是否避免了硬编码 Docker / Linux 路径
6. 是否保持 `UserId + ItemId` 唯一性
7. 是否保证普通用户不能物理删除
8. 是否保证物理删除有审计日志

## 阶段验收点

### 阶段 0

- 插件骨架存在
- 项目能编译
- Jellyfin 能识别插件

### 阶段 1

- SQLite 初始化存在
- `user_item_ratings` 和 `delete_audit_logs` 建表存在
- 单条评分查询、设置、清分接口可用

### 阶段 2

- 列表查询支持分页
- 支持按评分、待删除、关键词等条件筛选

### 阶段 3

- 批量改分可用
- 批量清分可用
- 批量设为待删除 / 取消待删除可用
- 有基本参数校验

### 阶段 4

- 详情页能看到评分 UI
- 修改评分后可即时生效

### 阶段 5

- 管理页能展示分页数据
- 能执行批量改分 / 清分 / 待删除

### 阶段 6

- 管理员可物理删除
- 普通用户不能调用物理删除
- 删除行为有日志

## 残余风险提醒

- Jellyfin 前端注入点可能仍需真实环境二次验证
- 媒体删除 API 与权限上下文需要真实 Jellyfin 10.10.7 环境核验
- 若未运行 `dotnet build` 或真实部署验证，不能把“理论可用”表述成“已验证”

## 原始来源

- `jellyfin-personal-ratings-md/00-README-给Codex的使用说明.md`
- `jellyfin-personal-ratings-md/02-产品需求文档-PRD.md`
- `jellyfin-personal-ratings-md/04-分阶段开发计划.md`
