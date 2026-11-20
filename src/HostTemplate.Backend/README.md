# HostTemplate.Backend

一条产线用的配置和程序包服务。工控机之间没有共享文件夹，主机通过 HTTP 拉取本工站配置，并按版本下载 Velopack 安装包。

项目以前叫 `HostTemplate.UpdateService`。HTTP 路径、`UpdateService` 配置节、磁盘目录和令牌头都没改。

数据都在磁盘上（`data/stations`、`data/packages`）。重启服务不会丢配置，也不会丢已经发布的版本。

## 目录

```
src/HostTemplate.Backend/
├─ Program.cs                 启动：配置绑定、文件库、版本策略、inbox 扫描、控制器
├─ Controllers/               HTTP 接口
├─ Application/               版本策略、inbox 导入、给客户端的下载地址
├─ Infrastructure/            文件库、Velopack feed、令牌中间件
├─ Models/                    安装包记录，以及 appsettings 的 UpdateService 节
└─ appsettings.json
```

主机和这个服务共用的版本、配置包类型在 `HostTemplate.Contracts`。

## 本机运行

```bash
dotnet run --project src/HostTemplate.Backend --urls http://0.0.0.0:5080
```

默认没有令牌，本机可以直接调。数据目录是程序工作目录下的 `data/`。

`appsettings.json` 的节名仍是 `UpdateService`：

| 项 | 含义 |
|---|---|
| `UpdateService:DataRoot` | 数据根目录，相对路径相对内容根 |
| `UpdateService:Token` | 非空时，除 `/api/health` 外都要带请求头 `X-Host-Token` |
| `UpdateService:PublicBaseUrl` | 写进版本信息里的服务地址。留空则用当前请求的地址 |
| `UpdateService:PackageId` | Velopack 包名，默认 `HostTemplate` |

## 工站配置

正式目录：`data/stations/{工站编号}/`，里面放 `app.xml`、`plc.xml`、`api.xml`，可选 `users.xml`。直接拷文件进去即可。

也可以丢到 `data/inbox/stations/{工站编号}/`，几秒内（或下一次查询时）会收进正式目录。

上传：

```bash
curl -X PUT --data-binary @OP010-config.zip http://127.0.0.1:5080/api/stations/OP010/config
```

zip 里要有 `app.xml`、`plc.xml`、`api.xml`。下载：

```bash
curl -o OP010.zip http://127.0.0.1:5080/api/stations/OP010/config
```

## 程序版本

每个已发布版本都留着，不只留最新的。正式目录：`data/packages/{版本号}/*.nupkg`。

投放：把 GitHub Release 上的 `HostTemplate-<版本>-full.nupkg` 放进 `data/inbox/`。文件名需要是 `{包名}-{版本}-full.nupkg`。也可以放进 `data/inbox/{版本号}/`。

上传：

```bash
curl -X PUT --data-binary @HostTemplate-1.0.42-full.nupkg \
  "http://127.0.0.1:5080/api/packages/1.0.42?fileName=HostTemplate-1.0.42-full.nupkg"
```

接口：

| 方法 | 路径 | 作用 |
|---|---|---|
| GET | `/api/health` | 健康检查，不校验令牌 |
| GET | `/api/stations/{id}/config` | 下载该工站配置 zip |
| PUT | `/api/stations/{id}/config` | 上传配置 zip |
| GET | `/api/stations/{id}/versions` | 全部版本。`latest` 是当前策略选中的目标 |
| GET | `/api/stations/{id}/latest` | 该工站的目标版本。现在的策略是 `newest` |
| PUT | `/api/packages/{version}` | 上传一个全量 nupkg |
| GET | `/api/feeds/{version}/releases.win.json` | 只含这一版的 Velopack feed |
| GET | `/api/feeds/{version}/{file}` | 下载该版安装包 |

`latest` 的 JSON 里有 `version`、`policy`、`feedUrl`、`downloadUrl`。以后若要按工站灰度，换掉 `Application/IVersionPolicy` 的实现，让它按 `stationId` 返回另一版即可，主机不用改。

开了令牌时，主机 `bootstrap.xml` 的 `ServiceToken` 会放进 `X-Host-Token`。Velopack 下载 feed 和 nupkg 时也会带这个头。

## 和 GitHub Release 的关系

`.github/workflows/release.yml` 在推到 `main` 后，于 Windows 运行器上测试、`dotnet publish`，再用 `scripts/pack-velopack.ps1` 调用 `vpk pack`（`--delta None`，每个版本都是全量包，方便回退）。Release 上同时挂着 zip 和 `*-full.nupkg`。

不要把 nupkg 提交进仓库。把 Release 里的 nupkg 拷到这台服务的 `data/inbox`，或用上面的 PUT。

Linux 上的 `vpk` 打出来的是 AppImage，不能给 Windows 工控机用。打包请在 Windows 上跑，或等这条 GitHub Action。相对路径的用法见 [开发手册](../../docs/开发手册.md)。
