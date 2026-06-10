namespace Dashboard.Tests.Hvv;

public class HvvModeMapperTests
{
    [Theory]
    [InlineData("BUS", TransportMode.Bus)]
    [InlineData("STRAIN", TransportMode.SBahn)]
    [InlineData("UTRAIN", TransportMode.UBahn)]
    [InlineData("FERRY", TransportMode.Ferry)]
    [InlineData("AKN", TransportMode.RegionalTrain)]
    [InlineData("ZUG", TransportMode.Other)]
    [InlineData(null, TransportMode.Other)]
    public void Map_TranslatesSimpleType(string? simpleType, TransportMode expected)
    {
        Assert.Equal(expected, HvvModeMapper.Map(simpleType));
    }
}
