using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Flow;
using HostTemplate.Core.Runtime;
using HostTemplate.Devices.Plc;
using Xunit;

namespace HostTemplate.Tests
{
    public class EngineTests : IDisposable
    {
        private readonly string _tempDirectory;

        public EngineTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HostTemplateEngineTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // 忽略清理异常。
            }
        }

        [Fact]
        public async Task Engine_ShouldDispatchStepWhenWordChanges()
        {
            var context = CreateContext();
            var fake = new FakePlcClient("MW200");
            context.PlcClient = fake;

            var step = new RecordingStep(ProcessWords.InStation);

            using (var engine = new StationEngine(context, new IStationStep[] { step }))
            {
                await engine.StartAsync();

                fake.SetWords("MW200", ProcessWords.InStation, 0);

                Assert.True(step.Done.Wait(TimeSpan.FromSeconds(3)), "步骤未被触发");
                await engine.StopAsync();
            }

            Assert.Equal(1, step.Count);
            Assert.Equal(ProcessWords.InStation, context.State.ProcessWord);
            Assert.Contains(ProcessWords.InStation, context.Trace.Words);
        }

        [Fact]
        public async Task Engine_ShouldIgnoreSameWordTwice()
        {
            var context = CreateContext();
            var fake = new FakePlcClient("MW200");
            context.PlcClient = fake;

            var step = new RecordingStep(ProcessWords.TestStart);

            using (var engine = new StationEngine(context, new IStationStep[] { step }))
            {
                await engine.StartAsync();

                fake.SetWords("MW200", ProcessWords.TestStart, 0);
                Assert.True(step.Done.Wait(TimeSpan.FromSeconds(3)));

                // 流程字不变时不应重复执行。
                await Task.Delay(300);
                await engine.StopAsync();
            }

            Assert.Equal(1, step.Count);
        }

        [Fact]
        public void Engine_ShouldRejectDuplicateWordRegistration()
        {
            var context = CreateContext();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                new StationEngine(context, new IStationStep[]
                {
                    new RecordingStep(ProcessWords.InStation),
                    new RecordingStep(ProcessWords.InStation),
                }));

            Assert.Contains("注册了多个步骤", exception.Message);
        }

        [Fact]
        public async Task Engine_ShouldLeaveRealtimeDataToTheStation()
        {
            var context = CreateContext();
            var fake = new FakePlcClient("MW200");
            context.PlcClient = fake;
            context.Live.PublishItem("旧项目", "1", "9", true);
            context.Live.PublishPoint("旧曲线", 0, 1);
            context.Live.PublishVerdict(true, "旧结论");

            var step = new RecordingStep(ProcessWords.InStation)
            {
                ResultToReturn = StepResult.Ng(ReviewCode.NG_InStation, "长度不符"),
            };

            using (var engine = new StationEngine(context, new IStationStep[] { step }))
            {
                await engine.StartAsync();
                fake.SetWords("MW200", ProcessWords.InStation, 0);
                Assert.True(await WaitUntil(() => step.Count > 0 && context.State.NgCount == 1));
                await engine.StopAsync();
            }

            var snapshot = context.Live.Snapshot();
            Assert.Equal(ProductVerdict.Ok, snapshot.Verdict);
            Assert.Equal("旧结论", snapshot.Reason);
            Assert.Equal("旧项目", snapshot.Rows[0].ItemName);
            Assert.Single(snapshot.Series);
            Assert.Equal(1, context.State.NgCount);
        }

        [Fact]
        public async Task Engine_ShouldReportErrorWhenCommandAddressMissing()
        {
            var context = CreateContext(manager => manager.Plc.Addresses.Clear());
            context.PlcClient = new FakePlcClient("MW200");

            using (var engine = new StationEngine(context, Array.Empty<IStationStep>()))
            {
                await engine.StartAsync();

                Assert.Equal(LogicState.系统异常, context.State.RunStatus);
                Assert.False(engine.IsRunning);
            }
        }

        private StationContext CreateContext(Action<ConfigManager> configure = null)
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);
            manager.App.PollIntervalMs = 20;
            manager.App.HeartbeatTicks = 2;

            configure?.Invoke(manager);

            return new StationContext(manager, new CollectingLogger(), _tempDirectory);
        }

        private sealed class RecordingStep : IStationStep
        {
            private int _count;

            public RecordingStep(ushort word)
            {
                Word = word;
            }

            public ushort Word { get; }

            public string Name => "测试步骤";

            public ManualResetEventSlim Done { get; } = new ManualResetEventSlim(false);

            public int Count => _count;

            public Action<StepContext> BeforeComplete { get; set; }

            public StepResult ResultToReturn { get; set; }

            public Task<StepResult> ExecuteAsync(StepContext context)
            {
                Interlocked.Increment(ref _count);
                BeforeComplete?.Invoke(context);
                Done.Set();
                return Task.FromResult(ResultToReturn ?? StepResult.Ok(ReviewCode.OK_InStation));
            }
        }

        private static async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 3000)
        {
            var start = Environment.TickCount;

            while (unchecked(Environment.TickCount - start) < timeoutMs)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        private sealed class CollectingLogger : IAppLogger
        {
            public event EventHandler<LogEventArgs> Written;

            public List<string> Messages { get; } = new List<string>();

            public void Debug(string message) => Add(LogLevel.Debug, message);

            public void Info(string message) => Add(LogLevel.Info, message);

            public void Warn(string message) => Add(LogLevel.Warn, message);

            public void Error(string message) => Add(LogLevel.Error, message);

            public void Error(string message, Exception exception) => Add(LogLevel.Error, message + exception?.Message);

            private void Add(LogLevel level, string message)
            {
                Messages.Add(message);
                Written?.Invoke(this, new LogEventArgs(level, message, DateTime.Now));
            }
        }
    }
}
