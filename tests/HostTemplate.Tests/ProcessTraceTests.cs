using System.Collections.Generic;
using HostTemplate.Core.Flow;
using Xunit;

namespace HostTemplate.Tests
{
    public class ProcessTraceTests
    {
        [Fact]
        public void Add_ShouldDeduplicateTail()
        {
            var trace = new ProcessTrace();

            trace.Add(10);
            trace.Add(10);
            trace.Add(20);
            trace.Add(20);
            trace.Add(30);

            Assert.Equal(new List<ushort> { 10, 20, 30 }, trace.Words);
            Assert.Equal("10-20-30", trace.Text);
        }

        [Fact]
        public void Check_ShouldPassWhenAllRequiredPresent()
        {
            var trace = new ProcessTrace();
            trace.Add(10);
            trace.Add(20);
            trace.Add(30);

            Assert.True(trace.Check(new ushort[] { 10, 20, 30 }, out var error));
            Assert.Equal(string.Empty, error);
        }

        [Fact]
        public void Check_ShouldReportMissingSteps()
        {
            var trace = new ProcessTrace();
            trace.Add(10);

            Assert.False(trace.Check(new ushort[] { 10, 20, 30 }, out var error));
            Assert.Contains("20", error);
            Assert.Contains("30", error);
        }

        [Fact]
        public void Check_ShouldPassWhenRequiredIsNullOrEmpty()
        {
            var trace = new ProcessTrace();

            Assert.True(trace.Check(null, out _));
            Assert.True(trace.Check(new ushort[0], out _));
        }

        [Fact]
        public void Clear_ShouldEmptyTrace()
        {
            var trace = new ProcessTrace();
            trace.Add(10);
            trace.Add(20);

            trace.Clear();

            Assert.Empty(trace.Words);
            Assert.Equal(string.Empty, trace.Text);
        }
    }
}
