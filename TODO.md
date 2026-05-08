# TODO

## P0

- 把“只有管理员可以物理删除”收口为不可绕开的硬约束，不再依赖可关闭配置。
- 调整删除与审计的落库顺序，降低“条目已删除但审计缺失”的风险。
- 为物理删除补更明确的失败分类与恢复建议。

## P1

- 为 `EnableDeleteFeature`、`EnableDetailsPageInjection`、`EnableManagePage` 补全真实开关逻辑。
- 增加审计日志查询接口或管理页。
- 评估目录级删除、父子项级联删除时的评分清理策略。
- 评估更稳的 Jellyfin Web 入口，而不只依赖配置页壳。
- 为大数据量场景优化元数据过滤和排序路径。

## P2

- 增加自动化测试项目，至少覆盖 Repository、Service 和关键 Controller。
- 补充 README 中的截图或联调示例。
- 评估 Favorite 同步能力的实现方式。
- 评估审计日志导出能力。
- 为非 Web 客户端预留后续兼容路线。
