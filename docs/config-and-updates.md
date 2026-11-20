# 配置目录、更新服务

现场工控机不走共享文件夹。程序目录会被版本更新换掉，工站自己的配置和日志不能放在里面。

## 启动时配置从哪来

程序旁边只有 `bootstrap.xml`。没有这个文件时，程序按 Windows 的 `%ProgramData%\HostTemplate` 生成一份。`bootstrap.default.xml` 是给现场看的样例，不会在更新时覆盖已经填好的引导文件。

| 字段 | 作用 |
|---|---|
| `ConfigDirectory` | 工站配置目录。默认 `%ProgramData%\HostTemplate\config` |
| `ServiceBaseUrl` | 本线更新服务，例如 `http://10.0.0.8:5080`。留空则完全离线 |
| `StationId` | 向服务要配置和版本时用的工站编号。留空则用 `app.xml` 的工站号 |
| `ServiceToken` | 可选。服务开了令牌时放到请求头 `X-Host-Token` |
| `LogDirectory` | 日志目录。默认 `%ProgramData%\HostTemplate\Logs` |

`app.xml`、`plc.xml`、`api.xml`、`users.xml` 都在配置目录。数据库相对路径也相对这个目录，不再相对程序目录。`log4net.config` 仍跟程序走，日志文件写到 `LogDirectory`。

加载顺序：

1. 配置目录里三份必需文件都在，就直接用。不会自动用服务器覆盖。
2. 目录或必需文件缺失时，向服务要该工站的配置包，写到本地再加载。
3. 服务没有、失败或是空的，就把程序自带的 `templates\`（没有则用代码里的默认值）补进缺失文件。离线也能启动。

手动「获取远端配置」会先弹出确认框，写明将覆盖本地配置。取消则本地文件一个字节都不改。确认后下载、覆盖、再走原来的热加载。

地址表只描述 PLC 地址，不会被抄进 `StationState`。

更新换掉程序目录后，`bootstrap.xml` 可能一起没了。每次读成功都会镜像到 `%ProgramData%\HostTemplate\bootstrap.xml`，下次启动再拷回程序目录。不要把填好的 `bootstrap.xml` 打进安装包；发布目录里只有 `bootstrap.default.xml`。

## 更新

Velopack 负责打包、下载和替换程序。版本列表和「该工站现在该装哪一版」由更新服务给出，程序不扫描共享目录。

- 「程序更新」列出服务上的全部版本。可以选择更新，也可以选旧版本回退。确认后才下载。
- 空闲才重启。空闲的定义在 `StationEngine.IsIdle`：流程字是等待进站（0），`Testing` 为假，没有正在执行的步骤。界面不自己判断。
- 不空闲时提示「已下载，空闲后安装并重启」，等引擎报空闲再调用 Velopack 重启。
- 系统配置里「自动更新」写在 `app.xml`。打开后定期问服务的目标版本（`GET /latest`），只在比当前新时后台下载，仍然等空闲才装。
- 服务返回的目标版本带 `policy`。现在固定是 `newest`（已发布里最新的）。以后按工站做灰度，只改服务选出的 `version` / `feedUrl`，客户端不用改。

当前如果不是 Velopack 安装（例如 `dotnet run`），按钮会说明无法替换文件，不会假装已经更新。窗口标题和右下角版本仍按原来的规则显示（提交号只留 7 位）。

## 服务

后端项目是 `HostTemplate.Backend`（以前叫 UpdateService）。怎么跑、目录和接口见 [src/HostTemplate.Backend/README.md](../src/HostTemplate.Backend/README.md)。GitHub Release 在 Windows 上额外打出 `*-full.nupkg`，把这个文件放进服务的 `data/inbox` 即可，不要把安装包提交进仓库。

Linux 上的 `vpk` 只会打 AppImage，不能给 Windows 工控机用。打包脚本是 `scripts/pack-velopack.ps1`，路径相对仓库根目录。CI 在 `windows-latest` 上这样调用，不提问：

```powershell
pwsh ./scripts/pack-velopack.ps1 -Version 1.0.42 -PublishDir $publishDir -OutputDir $feedDir -NonInteractive
```

`-PublishDir` 是 `-SourceDir` 的别名。本地交互式用法、只打包指定文件、以及不要把工站配置 xml 打进去，写在 [开发手册](开发手册.md) 的打包一节。
