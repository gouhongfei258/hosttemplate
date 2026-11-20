using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Data;
using HostTemplate.Core.Flow.Engine;
using HostTemplate.Core.Flow.Process;

namespace HostTemplate.App.Steps
{
    /// <summary>
    /// 示例工站：把「进站 → 测试开始 → 测试结束 → 出站」四个步骤拆成四个类。
    /// 新项目照抄这个结构，把业务写进对应步骤即可，不需要动引擎。
    /// </summary>
    public static class DemoSteps
    {
        public static IReadOnlyList<IStationStep> Create(TestDataStore store)
        {
            return new List<IStationStep>
            {
                new DemoInStationStep(),
                new DemoTestStartStep(),
                new DemoTestFinishStep(store),
                new DemoOutStationStep(),
            };
        }
    }

    /// <summary>进站：读 SN、校验条码、可在此调用 MES 申请 SN。</summary>
    public sealed class DemoInStationStep : IStationStep
    {
        public ushort Word => ProcessWords.InStation;

        public string Name => "进站";

        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            // 示例页用可选的数据袋。新的一件从这里清空；别的页面不用这一句。
            context.Live.BeginCycle();
            context.State.StartTime = DateTime.Now;
            context.State.Testing = true;

            var sn = context.ReadPlcString(AddressKeys.PlcSerialNumber, 20).Trim();

            // 仿真模式下 PLC 里没有 SN，这里造一个符合长度要求的，便于整条流程跑通。
            if (string.IsNullOrEmpty(sn))
            {
                var length = context.App.SnLength > 0 ? context.App.SnLength : 18;
                var seed = $"DEMO{DateTime.Now:yyMMddHHmmss}";

                sn = seed.Length >= length
                    ? seed.Substring(0, length)
                    : seed.PadRight(length, '0');

                var address = context.Station.GetAddress(AddressKeys.MesSerialNumber);
                if (!string.IsNullOrEmpty(address))
                {
                    context.Plc?.WriteString(address, sn, out _);
                }
            }

            if (context.App.SnLength > 0 && sn.Length != context.App.SnLength)
            {
                context.State.SerialNumber = sn;
                context.Live.RememberSerialNumber(sn);
                var message = $"SN 长度不符：{sn.Length} != {context.App.SnLength}";
                context.PublishVerdict(false, message);
                return StepResult.Ng(ReviewCode.NG_InStation, message);
            }

            context.State.SerialNumber = sn;
            context.Live.RememberSerialNumber(sn);

            // 这里是接 MES 的位置，示例只演示调用方式。
            // var response = await context.Api.PostAsync("ApplySn", new { LineCode = context.App.Line }, context.CancellationToken);

            await Task.Delay(80, context.CancellationToken).ConfigureAwait(false);

            return StepResult.Ok(ReviewCode.OK_InStation, $"SN={sn}");
        }
    }

    /// <summary>测试开始：检查进站是否已过、准备测试资源。</summary>
    public sealed class DemoTestStartStep : IStationStep
    {
        public ushort Word => ProcessWords.TestStart;

        public string Name => "测试开始";

        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (string.IsNullOrEmpty(context.State.SerialNumber))
            {
                const string message = "未读到 SN，拒绝开始测试";
                context.PublishVerdict(false, message);
                return StepResult.Ng(ReviewCode.NG_TestStart, message);
            }

            if (!context.Trace.Check(new[] { ProcessWords.InStation }, out var error))
            {
                context.PublishVerdict(false, error);
                return StepResult.Ng(ReviewCode.NG_TestStart, error);
            }

            // 示例曲线，只喂给 DemoLiveView。别的实时页面不必调用。
            context.PublishPoint("示例电压", 0, 11.8);
            context.PublishPoint("示例电压", 1, 12.1);

            await Task.Delay(120, context.CancellationToken).ConfigureAwait(false);

            return StepResult.Ok(ReviewCode.OK_TestStart, "测试已开始");
        }
    }

    /// <summary>测试结束：采集数据、落库、可在此上传 MES。</summary>
    public sealed class DemoTestFinishStep : IStationStep
    {
        private readonly TestDataStore _store;

        public DemoTestFinishStep(TestDataStore store)
        {
            _store = store;
        }

        public ushort Word => ProcessWords.TestFinish;

        public string Name => "测试结束";

        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            context.State.EndTime = DateTime.Now;
            context.State.Testing = false;

            // 示例判定：SN 里含 "NG" 即判不良，方便现场演示不良分支。
            var passed = context.State.SerialNumber.IndexOf("NG", StringComparison.OrdinalIgnoreCase) < 0;
            var voltage = passed ? 12.0 : 13.6;

            var record = new TestDataRecord
            {
                LineCode = context.App.Line,
                StationCode = context.App.EquId,
                StationName = context.App.WorkStationName,
                SnNumber = context.State.SerialNumber,
                TrayNo = context.State.TrayNo,
                TestType = context.App.Section,
                Result = passed,
                WarningMessage = passed ? string.Empty : "示例不良",
                StartTime = context.State.StartTime,
                EndTime = context.State.EndTime,
            };

            record.Items.Add(new TestDataItemRecord
            {
                ItemName = "示例电压",
                Standard = "11.5 ~ 12.5",
                Value = voltage.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                Result = passed,
                Remark = "模板示例数据，接入真实仪器后替换",
            });

            // 同一批测试项进实时表格。落库失败也不影响操作员看到这一件。
            foreach (var item in record.Items)
            {
                context.PublishItem(item.ItemName, item.Standard, item.Value, item.Result);
            }

            context.PublishPoint("示例电压", 2, 12.0);
            context.PublishPoint("示例电压", 3, voltage);

            try
            {
                await _store.SaveAsync(record, null, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 落库失败不应阻断产线，只报警。产品结论仍然照常给出。
                context.Alarms.Set("测试数据落库失败", true, ex.Message);
                context.Log.Error("测试数据落库失败", ex);
            }

            await Task.Delay(150, context.CancellationToken).ConfigureAwait(false);

            var message = passed
                ? $"测试用时 {(context.State.EndTime - context.State.StartTime).TotalMilliseconds:F0}ms"
                : "示例判定为不良";

            context.PublishVerdict(passed, message);

            return passed
                ? StepResult.Ok(ReviewCode.OK_TestFinish, message)
                : StepResult.Ng(ReviewCode.NG_TestFinish, message);
        }
    }

    /// <summary>出站：校验流程完整性、过站上报、清状态。</summary>
    public sealed class DemoOutStationStep : IStationStep
    {
        public ushort Word => ProcessWords.OutStation;

        public string Name => "出站";

        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (!context.Trace.Check(context.App.RequiredSteps, out var error))
            {
                context.PublishVerdict(false, error);
                return StepResult.Ng(ReviewCode.NG_OutStation, error);
            }

            await Task.Delay(80, context.CancellationToken).ConfigureAwait(false);

            var sn = context.State.SerialNumber;
            context.State.SerialNumber = string.Empty;
            context.State.Testing = false;

            return StepResult.Ok(ReviewCode.OK_OutStation, $"出站 SN={sn}");
        }
    }
}
