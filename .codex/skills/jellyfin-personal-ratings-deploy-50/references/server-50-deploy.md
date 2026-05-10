# 50 服务器部署流程

## 固定环境

- SSH 别名：`50`
- 容器名：`jellyfin`
- 插件目录：

```text
/home/docker_data/jellyfin/config/plugins/Jellyfin.Plugin.PersonalRatings
```

- 备份根目录：

```text
/home/docker_data/jellyfin/plugin-backups
```

## 推荐顺序

### 1. 本地构建

```bash
dotnet build src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj
dotnet test tests/Jellyfin.Plugin.PersonalRatings.Tests/Jellyfin.Plugin.PersonalRatings.Tests.csproj
dotnet publish src/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.csproj -c Release
```

发布目录：

```text
src/Jellyfin.Plugin.PersonalRatings/bin/Release/net8.0/publish
```

### 2. 如果 scp 不稳，改用 HTTP 拉取

本机起临时 HTTP 服务：

```bash
python3 -m http.server 18080 --directory src/Jellyfin.Plugin.PersonalRatings/bin/Release/net8.0/publish
```

如果端口被占用，先看：

```bash
lsof -nP -iTCP:18080 -sTCP:LISTEN
```

部署完记得关闭。

### 3. 远端备份 + 覆盖 + 重启

关键原则：

- 先备份
- 备份目录放到 `plugin-backups/`
- 不要把备份留在 `plugins/` 目录下

远端常用命令模板：

```bash
ssh 50 'set -e; \
plugin_dir=/home/docker_data/jellyfin/config/plugins/Jellyfin.Plugin.PersonalRatings; \
backup_root=/home/docker_data/jellyfin/plugin-backups; \
timestamp=$(date +%Y%m%d-%H%M%S); \
mkdir -p "$backup_root"; \
backup_dir="$backup_root/Jellyfin.Plugin.PersonalRatings.bak-$timestamp"; \
mkdir -p "$backup_dir"; \
cp -a "$plugin_dir/." "$backup_dir/"; \
curl -fsSL http://<local-ip>:18080/Jellyfin.Plugin.PersonalRatings.dll -o "$plugin_dir/Jellyfin.Plugin.PersonalRatings.dll"; \
curl -fsSL http://<local-ip>:18080/Jellyfin.Plugin.PersonalRatings.deps.json -o "$plugin_dir/Jellyfin.Plugin.PersonalRatings.deps.json"; \
curl -fsSL http://<local-ip>:18080/Jellyfin.Plugin.PersonalRatings.pdb -o "$plugin_dir/Jellyfin.Plugin.PersonalRatings.pdb"; \
docker restart jellyfin >/dev/null'
```

本机 IP 不确定时先查：

```bash
ipconfig getifaddr en0
```

## 部署后验证

### 容器状态

```bash
ssh 50 'docker ps --filter name=jellyfin --format "table {{.Names}}\t{{.Status}}"'
```

### 关键日志

```bash
ssh 50 'docker logs --tail 80 jellyfin | tail -n 40'
```

至少确认：

- 插件从 `/config/plugins/Jellyfin.Plugin.PersonalRatings/Jellyfin.Plugin.PersonalRatings.dll` 加载
- SQLite 初始化成功
- 容器最终回到 `healthy`

## 已知经验

- `scp` 在这台机器上偶发 `Connection reset by peer`
- HTTP 拉取方式通常更稳
- 之前把备份放进 `plugins/` 下面时，Jellyfin 会把备份误识别成插件；后续不要再这样做
