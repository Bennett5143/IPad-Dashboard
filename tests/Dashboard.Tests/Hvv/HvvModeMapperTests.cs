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

    [Theory]
    [InlineData("S3", "RAIL")]
    [InlineData("S5", "RAIL")]
    [InlineData("s1", "REGIONALTRAIN")]
    public void Map_ClassifiesAnSLineAsSBahn_WhateverTheProviderSays(string lineName, string simpleType)
    {
        // geofox meldet die S3/S5 ab Harburg Rathaus als RAIL. Auf einer Hamburger Tafel ist das
        // eine S-Bahn, und der Linienname sagt das eindeutig.
        Assert.Equal(TransportMode.SBahn, HvvModeMapper.Map(simpleType, lineName));
    }

    [Theory]
    [InlineData("RE1", "RAIL", TransportMode.RegionalTrain)]
    [InlineData("RB31", "REGIONALTRAIN", TransportMode.RegionalTrain)]
    [InlineData("Sprinter", "BUS", TransportMode.Bus)]
    [InlineData("142", "BUS", TransportMode.Bus)]
    [InlineData("U3", "UTRAIN", TransportMode.UBahn)]
    public void Map_LeavesEveryOtherLineToTheProvider(
        string lineName, string simpleType, TransportMode expected)
    {
        // „Sprinter" beginnt mit S, aber nicht mit S+Ziffer — ein echter Regionalzug und ein Bus
        // bleiben, was sie sind.
        Assert.Equal(expected, HvvModeMapper.Map(simpleType, lineName));
    }
}
