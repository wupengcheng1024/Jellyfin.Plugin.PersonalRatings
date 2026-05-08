# TODO

## P0

- 为管理员物理删除链路补自动化回归测试，覆盖 `auditUnavailable`、`notFound`、`deleteFailed` 和评分清理降级场景。
- 增加删除审计查询能力，避免只能直接看 SQLite 表或服务器日志。

## P1

- 为 `EnableDeleteFeature`、`EnableDetailsPageInjection`、`EnableManagePage` 补全真实开关逻辑。
- 评估目录级删除、父子项级联删除时的评分清理策略。
- 评估更稳的 Jellyfin Web 入口，而不只依赖配置页壳。
- 为大数据量场景优化元数据过滤和排序路径。

## P2

- 增加自动化测试项目，至少覆盖 Repository、Service 和关键 Controller。
- 补充 README 中的截图或联调示例。
- 评估 Favorite 同步能力的实现方式。
- 评估审计日志导出能力。
- 为非 Web 客户端预留后续兼容路线。
