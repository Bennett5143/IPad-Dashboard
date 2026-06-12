using Dashboard.Infrastructure.Status;

namespace Dashboard.Tests.Status;

public class SystemMetricsParserTests
{
    [Fact]
    public void ParseCpuTemperature_ConvertsMillidegrees()
    {
        Assert.Equal(48.312, SystemMetricsParser.ParseCpuTemperature("48312\n")!.Value, 3);
        Assert.Null(SystemMetricsParser.ParseCpuTemperature("kaputt"));
        Assert.Null(SystemMetricsParser.ParseCpuTemperature(null));
    }

    [Fact]
    public void ParseMemInfo_UsesAvailableNotFree()
    {
        const string memInfo =
            """
            MemTotal:        3884376 kB
            MemFree:          123456 kB
            MemAvailable:    2884376 kB
            Buffers:          100000 kB
            """;

        var (used, total) = SystemMetricsParser.ParseMemInfo(memInfo);

        Assert.Equal(3884376 / 1024.0, total!.Value, 1);
        Assert.Equal((3884376 - 2884376) / 1024.0, used!.Value, 1); // Total − Available
    }

    [Fact]
    public void ParseMemInfo_HandlesMissingLines()
    {
        Assert.Equal((null, null), SystemMetricsParser.ParseMemInfo(null));
        Assert.Equal((null, null), SystemMetricsParser.ParseMemInfo("irgendwas anderes"));
    }

    [Fact]
    public void ParseLoadAverage_TakesFirstValue()
    {
        Assert.Equal(0.52, SystemMetricsParser.ParseLoadAverage("0.52 0.58 0.59 1/189 12345"));
        Assert.Null(SystemMetricsParser.ParseLoadAverage(""));
        Assert.Null(SystemMetricsParser.ParseLoadAverage(null));
    }
}
