# 稳定截图与实机验证

## 适用场景

- 用户要求“你自己截图看看”
- UI 看起来和代码结构不一致
- 内置浏览器截图偶发失真、超时或拿到旧静态资源
- 需要验证前台路由、后台页真实加载、详情页返回链路

## 推荐方式

### 1. 先用本地 Chrome + puppeteer-core

优先使用系统 Chrome：

```text
/Applications/Google Chrome.app/Contents/MacOS/Google Chrome
```

临时目录建议：

```text
/tmp/personalratings-browser
```

依赖安装：

```bash
mkdir -p /tmp/personalratings-browser
cd /tmp/personalratings-browser
[ -f package.json ] || npm init -y >/dev/null 2>&1
npm install puppeteer-core@24.9.0 --no-save
```

运行脚本时建议：

```bash
NODE_PATH=/tmp/personalratings-browser/node_modules node /tmp/personalratings-browser/check.js
```

## 登录选择器

当前 Jellyfin 10.10.7 登录页常用选择器：

- 用户名：`#txtManualName`
- 密码：`#txtManualPassword`
- 登录按钮：`button[type="submit"].raised.button-submit`

## 截图输出

统一把截图落到：

```text
/tmp/*.png
```

然后用 `view_image` 检查，不要只看终端文字。

## 常用回归链路

### 前台打分库

1. 打开 `http://<host>:8096/web/#/personalratings`
2. 检查：
   - 顶栏 tabs 是否存在
   - 打分库是否真的激活
   - 列表 / 海报切换是否生效
   - 页面是否有空白或叠层

### 详情页链路

1. `打分库 -> 任意详情页 -> 返回`
2. 检查：
   - 返回后是否仍是 `#/personalratings`
   - tabs 是否还在
   - 背景是否残留详情页

### 后台页链路

1. `打分库 -> 评分后台`
2. 检查：
   - URL 是否进入 `#/configurationpage?name=PersonalRatingsManagePage`
   - 摘要文本是否出现真实数量
   - 状态文本是否从“准备中”变为“列表已刷新”

## 缓存排查

如果页面行为像“旧版本”：

1. 先检查资源 URL 是否带新版本戳
2. 再检查页面里是否有旧 `<link>` / `<script>` 没被替换
3. 不要第一时间归因到 CSS 失效；也可能是旧脚本没更新

## 最低验收输出

至少给出：

- 实际访问的 URL
- 页面摘要 / 状态文本
- 一张真实截图
- 是否是在真实 Jellyfin 10.10.7 页面下验证
