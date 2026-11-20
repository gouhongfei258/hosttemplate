# HostTemplate.Backend

一条产线用的配置和程序包服务。工控机通过 HTTP 拉取本工站配置，并按版本下载 Velopack 安装包。

数据在磁盘上（`data/stations`、`data/packages`）。重启服务不会丢掉配置，也不会丢掉已经发布的版本。

主机侧何时下载、何时等空闲再重启，见 [更新与打包](../../docs/更新与打包.md)。解决方案见 [开发手册](../../docs/开发手册.md)。

## 目录

```
src/HostTemplate.Backend/
├─ Program.cs                 配置绑定、文件库、版本策略、inbox 扫描、控制器
├─ Controllers/               HTTP 接口
├─ Application/               NewestVersionPolicy、inbox 导入、下载地址
├─ Infrastructure/            文件库、Velopack feed、令牌中间件
├─ Models/                    安装包记录，以及 appsettings 的 UpdateService 节
├─ appsettings.json
└─ Properties/launchSettings.json
```

主机和这个服务共用的版本、配置包类型在 `HostTemplate.Contracts`。

## 本机运行

```bash
dotnet run --project src/HostTemplate.Backend --urls http://0.0.0.0:5080
```

`Properties/launchSettings.json` 里的配置文件同样把地址设为 `http://0.0.0.0:5080`。默认没有令牌，本机可以直接调用。数据目录是内容根下的 `data/`（从仓库里用 `dotnet run` 时，内容根是这个项目目录）。

`appsettings.json` 的节名是 `UpdateService`：

| 项 | 含义 |
|---|---|
| `UpdateService:DataRoot` | 数据根目录。相对路径相对内容根 |
| `UpdateService:Token` | 空字符串时不校验。非空时，除 `/api/health` 外都要带请求头 `X-Host-Token`，按固定时间比较 |
| `UpdateService:PublicBaseUrl` | 写进版本信息里的服务地址。空则用当前请求的地址 |
| `UpdateService:PackageId` | Velopack 包名，默认 `HostTemplate` |

## 磁盘

```
data/
├─ stations/{工站编号}/     app.xml、plc.xml、api.xml，可选 users.xml
├─ packages/{版本号}/       {包名}-{版本}-full.nupkg 和 release.json
├─ inbox/                   投放目录。根上或 inbox/{版本号}/ 里的全量 nupkg
│  └─ stations/{工站编号}/  待导入的工站配置
└─ inbox-processed/         已经收进正式目录的投放文件
```

`InboxImportService` 每 5 秒扫描一次 inbox。查询版本或工站配置时也会先收一次。导入成功的文件或目录会移到 `inbox-processed`，文件名前面加上时间戳。

工站编号和版本号只允许字母、数字、`-`、`_`、`.`，长度 1～80，不能是 `.` 或 `..`。

## 投放配置

直接把 `app.xml`、`plc.xml`、`api.xml` 拷进 `data/stations/{工站编号}/`。也可以放到 `data/inbox/stations/{工站编号}/`，三份必需文件都在时会被收进正式目录，可选带上 `users.xml`。

上传 zip：

```bash
curl -X PUT --data-binary @OP010-config.zip http://127.0.0.1:5080/api/stations/OP010/config
```

zip 里要有 `app.xml`、`plc.xml`、`api.xml`。其他条目会被忽略。下载：

```bash
curl -o OP010.zip http://127.0.0.1:5080/api/stations/OP010/config
```

## 投放安装包

每个已发布版本都保留。正式文件在 `data/packages/{版本号}/`。文件名需要匹配 `{包名}-{版本}-full.nupkg`，例如 `HostTemplate-1.0.42-full.nupkg`。

放到 `data/inbox/` 时从文件名取版本。放到 `data/inbox/{版本号}/` 时用目录名作版本；文件名里也能解析出版本时，两者必须一致，否则跳过该文件。同一版本目录里只保留最新写入的一个 nupkg。

上传：

```bash
curl -X PUT --data-binary @HostTemplate-1.0.42-full.nupkg \
  "http://127.0.0.1:5080/api/packages/1.0.42?fileName=HostTemplate-1.0.42-full.nupkg"
```

不传 `fileName`、请求头里也没有 `Content-Disposition` 的文件名时，用 `{PackageId}-{version}-full.nupkg`。文件名里的版本和 URL 里的版本不一致时返回 400。

## 接口

| 方法 | 路径 | 作用 |
|---|---|---|
| GET | `/api/health` | 健康检查，返回 `{ "status": "ok" }`。不校验令牌 |
| GET | `/api/stations/{id}/config` | 下载该工站配置 zip。没有完整配置时 404 |
| PUT | `/api/stations/{id}/config` | 上传配置 zip |
| GET | `/api/stations/{id}/versions` | 全部版本，新的在前。`latest` 是策略选中的目标 |
| GET | `/api/stations/{id}/latest` | 该工站的目标版本。没有已发布版本时 404 |
| PUT | `/api/packages/{version}` | 上传一个全量 nupkg |
| GET | `/api/feeds/{version}/releases.win.json` | 只含这一版的 Velopack feed |
| GET | `/api/feeds/{version}/{file}` | 下载该版安装包。文件名必须和库里的 nupkg 一致 |

JSON 使用 camelCase。`latest` 和版本列表里的每一项有 `stationId`、`version`、`policy`、`feedUrl`、`downloadUrl`、`fileName`、`size`、`publishedAt`。`feedUrl` 以斜杠结尾，形如 `{基地址}/api/feeds/{版本}/`。目标版本的 `policy` 来自 `IVersionPolicy.Name`。列表里其余版本的 `policy` 为空字符串。

当前注册的实现是 `NewestVersionPolicy`，名字 `newest`，取已发布版本里版本号最大的一个。要按工站返回另一个版本，实现 `IVersionPolicy` 并在 `Program.cs` 里替换注册。主机按返回的 `feedUrl` 下载。

开了令牌时，主机 `bootstrap.xml` 的 `ServiceToken` 会放进 `X-Host-Token`。

GitHub Release 上的 `HostTemplate-<版本>-full.nupkg` 就是这里接受的全量包。打包脚本和发布工作流见 [更新与打包](../../docs/更新与打包.md)。不要把 nupkg 提交进仓库。
