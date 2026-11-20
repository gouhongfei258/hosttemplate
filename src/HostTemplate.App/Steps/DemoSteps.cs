using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Data;
using HostTemplate.Core.Flow;

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
                return StepResult.Ng(ReviewCode.NG_InStation, $"SN 长度不符：{sn.Length} != {context.App.SnLength}");
            }

            context.State.SerialNumber = sn;

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
                return StepResult.Ng(ReviewCode.NG_TestStart, "未读到 SN，拒绝开始测试");
            }

            if (!context.Trace.Check(new[] { ProcessWords.InStation }, out var error))
            {
                return StepResult.Ng(ReviewCode.NG_TestStart, error);
            }

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
                Value = "12.0",
                Result = passed,
                Remark = "模板示例数据，接入真实仪器后替换",
            });

            try
            {
                await _store.SaveAsync(record, null, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 落库失败不应阻断产线，只报警。
                context.Alarms.Set("测试数据落库失败", true, ex.Message);
                context.Log.Error("测试数据落库失败", ex);
            }

            await Task.Delay(150, context.CancellationToken).ConfigureAwait(false);

            return passed
                ? StepResult.Ok(ReviewCode.OK_TestFinish, $"测试用时 {(context.State.EndTime - context.State.StartTime).TotalMilliseconds:F0}ms")
                : StepResult.Ng(ReviewCode.NG_TestFinish, "示例判定为不良");
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
