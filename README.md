# HostTemplate · 上位机最小开发模板

从 BYD DCS 上位机项目（`BydDCS`）抽离出的通用骨架。目标：**新工站从 0 到跑通「进站 → 测试 → 出站」不超过一天**，不再复制一份 5000 行的 `RunLogic.cs`。

## 1. 技术栈

| 项 | 选择 | 说明 |
|---|---|---|
| 运行时 | .NET 8（`net8.0-windows`） | SDK 式 csproj，`x64` |
| UI | WinForms + SunnyUI 3.7.2 | SunnyUI 有 `net8.0-windows7.0` 目标 |
| PLC | HslCommunication 12.8.3（NuGet） | **不要**再用 `using/HslCommunication.dll`，那是 net48 专用编译版 |
| 数据 | EF Core 8.0.1 + Microsoft.Data.Sqlite | 替代 EF6 + SQLite.CodeFirst（不支持 .NET 8） |
| 日志 | log4net 3.3.0 | 落盘 + 事件推送界面（3.0.3 有 CVE-2026-40021，必须 ≥ 3.3.0） |
| 配置 | XML + `XmlSerializer` + `PropertyGrid` | 现场可直接改、可可视化编辑 |
| 测试 | xunit | 引擎/配置/轨迹/落库/鉴权/设备/实时数据共 63 个用例 |

## 2. 目录

```
HostTemplate/
├─ HostTemplate.sln
├─ src/
│  ├─ HostTemplate.Core/          与设备无关的一切
│  │  ├─ Abstractions/            IPlcClient / IApiClient / IDevice / AddressKeys
│  │  ├─ Configuration/           AppConfig / PlcConfig / ApiConfig / ConfigManager / XmlConfigStore
│  │  ├─ Diagnostics/             IAppLogger / Log4NetLogger / AlarmManager
│  │  ├─ Flow/                    StationEngine / IStationStep / StepContext / ProcessTrace
│  │  ├─ Runtime/                 StationContext（取代 static Global）/ StationState / CycleLiveData（可选）
│  │  ├─ Security/                AuthService（PBKDF2）/ UserAccount / UserSession
│  │  └─ Data/                    HostDbContext / TestDataStore / 实体
│  ├─ HostTemplate.Devices/       具体协议实现
│  │  ├─ Hsl/                     HslDeviceBase（HSL 统一封装）/ 各协议客户端 / PlcDeviceCatalog / HslDeviceFactory
│  │  ├─ Plc/                     Siemens7Client（S7）、FakePlcClient（仿真）
│  │  ├─ Api/                     HttpApiClient（端点表驱动）
│  │  └─ Tcp/                     TcpDeviceBase（打印机/扫码枪/仪器基类）
│  └─ HostTemplate.App/           WinForms 主程序
│     ├─ Program.cs               启动装配：单实例 → 日志 → 配置 → 上下文 → 登录 → 设备 → 引擎 → 界面
│     ├─ MainForm.cs              监控台（状态灯 / 流程字 / 实时数据 / 日志 / 报警条）
│     ├─ Forms/                   ConfigForm（配置）/ UserForm（账号）/ LoginForm / ChangePasswordForm
│     ├─ Steps/                   DemoSteps.cs / DemoLiveView.cs（示例页，整页可换）
│     └─ Controls/                StatusLight / UiLogSink
├─ .github/workflows/release.yml  合并到 main 后发布 GitHub Release
└─ tests/HostTemplate.Tests/
```

## 3. 快速开始

```bash
# 无硬件，直接跑仿真：每 3 秒自动推进一次流程字，并自动开始监控
dotnet run --project src/HostTemplate.App -- --simulate --autostart
```

| 参数 | 作用 |
|---|---|
| `--simulate` | 强制启用仿真 PLC，并把自动推进间隔设为 3 秒 |
| `--autostart` | 窗口显示后自动开始监控（不点「开始监控」） |
| `--config` | 直接打开配置编辑页，保存后退出（现场调试用） |

首次启动会在 `bin/.../config/` 下自动生成 `app.xml`、`plc.xml`、`api.xml`，缺文件时回落默认值，不会启动失败。

接真实 PLC 时把 `app.xml` 的 `Simulate` 改为 `false`，然后在「系统配置 → PLC 连接」里选型号、填 IP，点「连接测试」通了再保存（详见 7.1）。

## 4. 引擎语义（StationEngine）

- **边沿触发**：流程字与上次处理值不同才分发，PLC 抖动不会重复执行。
- **单飞防重入**：上一步未结束时不重入，且**不丢弃**——下一轮继续尝试。
- **统一回写**：步骤返回 `StepResult`，引擎负责写审查码、记轨迹、统计良率。
- **失败不阻断**：通信失败只置报警并断开，下一轮自动重连（带间隔节流）。
- **地址解耦**：引擎只认 `AddressKeys` 里的键名，换点位只改 `plc.xml`。
- **不管画面**：引擎不填写实时页面。产品结论怎么画由工站自己的控件决定。程序报警走 `AlarmManager`，主界面只负责把报警条亮出来。

## 4.1 主界面

上半部分仍是工站身份：顶栏状态灯，下面是流程字 / SN / 托盘 / 轨迹 / 良品不良计数。中间是两个页签，底栏按钮不动。

| 区域 | 谁负责 |
|---|---|
| 报警条 | 模板。在身份条和页签之间，始终可见。无报警显示「无程序报警」，有报警显示当前故障。新的程序报警，或步骤返回 NG，会自动切到「实时数据」 |
| 实时数据 | **工站整页自己写**。表格、曲线、视觉标注、几种形式拼在一起，都是这个控件里的事。`MainForm` 只把它 Dock 满这一页 |
| 运行日志 | 模板。原来的日志框 |

在 `Program.cs` 里把控件交进去。仓库自带的 `DemoLiveView` 只是让模板能跑起来的示例（大号 OK/NG + 表格 + 一条示例曲线），换掉这一行即可，不用改引擎：

```csharp
var realtimeView = new MyStationView();   // 图像标注、组合画面，或任何 WinForms 控件
Application.Run(new MainForm(context, engine, realtimeView, CreatePlcClient, CreateApiClient));
```

`CycleLiveData` 是可选的数据袋，不是页面契约。示例工站用它推测试项、曲线点和产品结论，并在进站时自己 `BeginCycle()`。不做历史查询页，落库仍走 `TestDataStore`。不用这个袋子的页面完全可以不引用它。

```csharp
context.Live.BeginCycle();
context.PublishItem("电压", "11.5 ~ 12.5", "12.01", true);
context.PublishPoint("电压曲线", t, 12.01);
context.PublishVerdict(true, "合格");   // 本周期一旦 NG，后面的 OK 不会盖掉
```

窗口标题和右下角显示程序版本。

## 5. 新增一个工站步骤

```csharp
public sealed class MyLeakTestStep : IStationStep
{
    public ushort Word => ProcessWords.TestStart;   // 20
    public string Name => "气密性检测";

    public async Task<StepResult> ExecuteAsync(StepContext ctx)
    {
        var ok = await RunLeakDeviceAsync(ctx.CancellationToken);
        return ok
            ? StepResult.Ok(ReviewCode.OK_TestStart)
            : StepResult.Ng(ReviewCode.NG_TestStart, "泄漏量超上限");
    }
}
```

在 `DemoSteps.Create()` 里注册即可，**不需要改引擎**。

## 6. 配置三件套

| 文件 | 内容 | 关键字段 |
|---|---|---|
| `app.xml` | 机器/工站/运行参数 | `EquId`、`PollIntervalMs`、`HeartbeatTicks`、`RequiredSteps`、`Simulate` |
| `plc.xml` | 连接参数 + 地址表 | `Protocol`、`Address`、`Port`、`CpuType`、`Rack`、`Slot`、`StationNo`、`Addresses[]` |
| `api.xml` | 上位系统接口 | `BaseUrl`、`Headers[]`、`Endpoints[]`、`Enabled` |

默认地址表（可在 `ConfigManager.CreateDefaultPlc()` 中调整）：

| 键名 | 方向 | 默认地址 | 类型 |
|---|---|---|---|
| `MES_心跳` | 写 | M100.0 | Bool |
| `PLC_指令` | 读 | MW200 | Word × 2 |
| `PLC_预留bool` | 读 | M110.0 | Bool × 16 |
| `MES_审查结果` | 读写 | MW210 | Word |
| `MES_SN` | 写 | DB1.DBB0 | String(20) |
| `PLC_SN` | 读 | DB1.DBB100 | String(20) |
| `PLC_托盘号` | 读 | MW220 | Word |

## 7. 配置编辑页

主界面点「系统配置」（或 `--config` 启动）打开，四页：

| 页面 | 控件 | 说明 |
|---|---|---|
| 基础参数 | `PropertyGrid` | 分组显示 `[PC]` `[运行]` `[条码]` `[存储]` `[调试]` `[权限]` |
| PLC 连接 | 自绘表单 + 下拉框 | 先选设备型号，再按型号显示需要的参数，带默认值和说明，可现场试连 |
| PLC 地址表 | `DataGridView` | 增删改、复制、上下移；读写与数据类型是下拉框 |
| MES 接口 | `PropertyGrid` + 双表格 | 服务参数 + 端点表 + 请求头 |

### 7.1 PLC 连接页

`PropertyGrid` 摆不下「选型号 → 参数跟着变」，所以这一页是手搭的。数据源只有一个：`PlcDeviceCatalog`。

**设备型号下拉**（10 项，`PlcDeviceCatalog.All`）：

| 型号（下拉显示） | 协议 | 默认端口 | 显示的行 |
|---|---|---|---|
| 西门子 S7-1200 | `SiemensS7` | 102 | IP / 端口 / 机架 / 插槽 |
| 西门子 S7-1500 | `SiemensS7` | 102 | IP / 端口 / 机架 / 插槽 |
| 西门子 S7-300 | `SiemensS7` | 102 | IP / 端口 / 机架 / 插槽 |
| 西门子 S7-400 | `SiemensS7` | 102 | IP / 端口 / 机架 / 插槽 |
| 西门子 S7-200 Smart | `SiemensS7` | 102 | IP / 端口 |
| Modbus TCP（通用） | `ModbusTcp` | 502 | IP / 端口 / **站号** |
| 汇川 Inovance（AM/H5U） | `InovanceTcp` | 502 | IP / 端口 / **站号** |
| 台达 Delta（DVP/AS） | `DeltaTcp` | 502 | IP / 端口 / **站号** |
| 三菱 MC（Q/FX5U/iQ-R） | `MelsecMc` | 6000 | IP / 端口 |
| 欧姆龙 FINS（NJ/NX/CJ） | `OmronFins` | 9600 | IP / 端口 / **节点号** |

选中型号后这一页做四件事：

1. **换行**：机架/插槽 与 站号 按 `UseRackSlot` / `UseStation` 显示或隐藏，剩下的行重新紧凑排列（隐藏行不留空档）。
2. **填默认值**：端口、机架号、插槽号、站号回落到该型号的默认值。S7-300/400 插槽默认 2，S7-1200/1500 默认 0；欧姆龙节点号默认 0，填 0 时客户端自动取 IP 末段。
3. **给说明**：每个输入框右边一句提示（`默认 102`、`S7-300/400 常用 2`、`从站地址 1 ~ 247`……），下拉框右侧一段该型号的注意事项（例如 S7-1200 要在 TIA 里勾「允许 PUT/GET 通信」）。
4. **换地址写法**：底部提示随型号变 —— S7 是 `M100.0 / MW200 / MD300 / DB1.DBW0`，Modbus 是 `保持寄存器 0/100；线圈 0 或 coil=0；离散输入 dis=0`。

**连接测试**：点「连接测试」会用**当前页面上的参数**（不是已保存的）实际连一次，结果实时显示：

- 连上了且地址表有 `PLC_指令` → 顺手读一次，显示 `读取 MW200 = 1`
- 连上了但读失败 → 显示「已连接，但读取失败：xxx」，说明是地址写错而不是连不上
- 连不上 → 再 `Ping` 一次区分原因：Ping 通提示「检查端口 / 站号 / 机架槽号」，Ping 不通提示「检查网线、网段和 PLC 电源」

测试在 `Task.Run` 里跑，UI 不卡；测完立即 `Dispose` 客户端，不留连接。

### 7.2 三个设计约定

1. **改的是副本**：打开时 `XmlConfigStore.Clone` 深拷贝，点「取消」原配置一个字都不动。
2. **保存前校验**：轮询周期、端口范围、CPU 型号、键名重复、地址为空、缺 `PLC_指令`、基地址格式、PLC 页各框是否为数字……出错会自动跳到出错的那一页并列出问题。
3. **保存即生效**：主窗收到结果后走 `ApplyConfigAsync()` —— 停监控 → `ReloadConfig()` 重读三份 XML 并重建地址索引 → 用工厂重建 `IPlcClient` / `IApiClient` → `engine.ReloadAddresses()` → 恢复运行。现场改 IP 或地址表不用重启。

不阻断保存的提醒会单独弹窗：地址表缺键名、开着仿真模式、打开了登录开关。

## 8. HSL 封装层（Devices/Hsl）

HSL 12.x 的公开读写方法几乎都挂在 `DeviceCommunication` 上，S7 / Modbus / 三菱 / 欧姆龙 / 汇川只是「怎么连」不同。所以封装按这个分层做，不是按品牌各写一套。

### 8.1 类的分工

| 类 | 行数 | 作用 |
|---|---|---|
| `HslDeviceBase` | ~550 | 抽象基类：连接守卫 + 串行锁 + 全类型读写（实现 `IPlcClient`） |
| `ModbusClientBase` | ~50 | Modbus 家族共用：线圈 / 离散输入 / 写单寄存器 / 掩码写 |
| `ModbusTcpClient` `InovanceTcpClient` `DeltaTcpClient` | 25~30 | Modbus 家族，只差名字和默认端口 |
| `MelsecMcClient` | ~35 | 三菱 MC，默认 6000 |
| `OmronFinsClient` | ~60 | 欧姆龙 FINS，默认 9600；节点号留 0 时自动取 IP 末段 |
| `Siemens7Client` | ~90 | S7，继承 `HslDeviceBase`，只保留 S7 专有部分（建 `SiemensS7Net`、读 PLC 时钟、读订货号） |
| `PlcDeviceCatalog` | ~215 | 型号元数据：默认端口/机架/插槽/站号、显示哪些行、地址写法、注意事项 |
| `HslDeviceFactory` | ~125 | 按 `plc.xml` 的 `Protocol` 创建客户端 |

配置页的下拉框、行显隐、默认值、说明文字、校验规则全部读 `PlcDeviceCatalog`，**没有第二处硬编码**。新增一个型号只要往 `All` 里加一条。

### 8.2 覆盖的方法

读写（同步，全部 `bool + out`，不抛异常）：

```
Bool / Byte / Bytes / Int16 / UInt16 / Int32 / UInt32 / Int64 / UInt64
Float / Double / String(定长) / 各自数组 / ReadStruct<T>
```

外加几个现场常用的：

```csharp
plc.WaitBool("M30.0", true, 3000, out var elapsed, out _);   // 等气缸到位，带超时
plc.ReadStruct<MyData>("DB1.DBB0", 64, out var data, out _); // 结构体直读
plc.Ping();                                                  // 区分网线问题和 PLC 没开
plc.EnablePersistentConnection();                            // 网关爱断空闲连接时开
plc.RawDevice;                                               // 逃生口：直接拿 HSL 原对象
```

`ModbusClientBase`（`ModbusTcpClient` / `InovanceTcpClient` / `DeltaTcpClient` 的父类）另有 `ReadCoil / ReadDiscrete / WriteOneRegister / WriteMask`。

### 8.3 加一个新品牌（基恩士 MC 为例）

```csharp
public sealed class KeyenceMcClient : HslDeviceBase
{
    public KeyenceMcClient(string ip, int port = 5000)
    {
        Name = "KeyenceMc";
        Address = ip;
        var mc = new KeyenceMcNet(ip, port);   // HSL 里它本身就继承 MelsecMcNet
        Device = mc;
        ConnectAction = () => mc.ConnectServer();
        CloseAction = () => mc.ConnectClose();
    }
}
```

两步接完：

1. `HslDeviceFactory.Create()` 加一个 `case`（协议名建议常量 `ProtocolKeyenceMc`）；
2. `PlcDeviceCatalog.All` 加一条型号，填上默认端口、显示哪些行、地址写法和注意事项 —— 配置页下拉框、行显隐、默认值、校验会自动跟上。

不需要改配置页代码，也不需要动 `HslDeviceBase`。

### 8.4 为什么没包异步

HSL 的 `XxxAsync` 在多数设备上只是同步包装，而主循环是 50ms 定时轮询，异步反而多一层状态机。需要时直接调 `RawDevice` 或用 `Task.Run` 包一层。

## 9. 登录与权限（已实现，默认关闭）

`app.xml` 的 `[权限] 01.启用登录` 默认 `false`。关闭时 `StationContext.Session` 是**匿名管理员**，所有权限判断放行，行为与没有登录完全一致。

打开后启动弹 `LoginForm`，账号存 `config/users.xml`：

- 密码用 PBKDF2-SHA256（10 万次迭代 + 16 字节随机盐），只存派生值，比对走 `CryptographicOperations.FixedTimeEquals`
- 首次自动生成 `admin / admin123`，并标记 `MustChangePassword`，登录后强制改密（可「稍后再说」）
- 角色 `操作员 < 工程师 < 管理员`，判断用 `Session.HasRole(UserRole.工程师)`
- 账号管理 API：`AuthService.Upsert / Remove / SetEnabled / ChangePassword`

业务代码里要卡权限时只写一行，不用关心开关状态：

```csharp
if (!context.Session.HasRole(UserRole.工程师)) { /* 拒绝 */ }
```

### 账号管理页

主界面「账号管理」打开 `UserForm`：

- 列表显示账号 / 姓名 / 角色 / 状态 / 首次登录 / 上次登录，双击行即编辑
- 新增、编辑、删除、刷新；编辑时密码留空表示不改，填了就是重置（下次登录强制改密）
- **防锁死**：最后一个启用的管理员不能被停用、降级或删除，`AuthService` 直接拒绝并提示
- 未启用登录时也能进来提前建账号，页面右上角标明当前开关状态

## 10. 从原项目迁移时要注意的三处

1. **HslCommunication 换包**：原 `using/HslCommunication.dll` 只编译了 net48，.NET 8 引用会在运行期抛 `BadImageFormatException`。必须用 NuGet `HslCommunication 12.8.3`。
2. **数据层重写**：EF6、`SQLite.CodeFirst`、`System.Data.SQLite.EF6` 均不支持 .NET 8。已换成 EF Core + `EnsureCreated()`；原表结构可按 `Entities.cs` 逐个补字段。
3. **`Global` 改名 `StationContext`**：叫 `AppContext` 会与 `System.AppContext` 冲突。所有模块通过构造函数注入，不再有静态状态。

## 11. 尚未包含（后续阶段）

- 历史数据查询页、日志查询页、返修换绑页（主界面只有当前周期，见 4.1）
- 打印（Zebra ZPL）、扫码枪、视觉、指纹
- Velopack 打包与在线更新
- 串口类 PLC（HSL 的 `SerialBase` 家族，接法与 8.3 同构）
- 已在模板里的 PLC 品牌：西门子 S7 / Modbus TCP / 汇川 / 台达 / 三菱 MC / 欧姆龙 FINS；其余品牌按 8.3 两步接入

## 12. 合并后的 GitHub Release

`.github/workflows/release.yml` 在推送到 `main`（PR 合并的常规结果）时，于 `windows-latest` 上用 .NET 8 跑测试，再 `dotnet publish` 到运行器临时目录。校验版本后打成 zip，创建 GitHub Release（标签 `v1.0.<run_number>`，说明里带短提交号），并把 zip 挂到该 Release。不往仓库提交二进制，也不在项目里留 `release/` 或 `publish/` 目录。

版本不提交回仓库。`Directory.Build.props` 固定主次版本 `1.0`，补丁默认 `0`，所以本机 `dotnet build` 的文件版本 / 程序集版本仍是 `1.0.0`。CI 用 `github.run_number` 作为补丁，并把 7 位提交号写入 `InformationalVersion`（例如 `1.0.42+abcdef0`）。标签只用 `v1.0.42`，不加 `+` 后面的提交号。在 git 工作副本里本地编译时，SDK 还会把完整提交号附在 `1.0.0+...` 上，数值版本不变。主界面标题和右下角显示的就是这个版本。
