using Compass;
using Xunit;

namespace Compass.Tests;

public class ProductionToDevelopmentRefreshTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    [InlineData("Prod")]
    [InlineData("prod")]
    public void ProductionEnvironmentNames_AreRejectedAsWriteTargets(string environmentName)
    {
        Assert.True(ProductionToDevelopmentRefresh.IsProductionEnvironmentName(environmentName));
        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionToDevelopmentRefresh.EnsureEnvironmentIsNotProduction(environmentName));
        Assert.Contains("production cannot be updated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    public void NonProductionEnvironmentNames_CanBeWriteTargets(string environmentName)
    {
        Assert.False(ProductionToDevelopmentRefresh.IsProductionEnvironmentName(environmentName));
        ProductionToDevelopmentRefresh.EnsureEnvironmentIsNotProduction(environmentName);
    }

    [Fact]
    public void SameDatabase_IsRejected()
    {
        const string connectionString =
            "Server=tcp:compass-prod.database.windows.net,1433;Initial Catalog=compass;User ID=u;Password=p;";

        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionToDevelopmentRefresh.EnsureDifferentDatabases(connectionString, connectionString));
        Assert.Contains("same database", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DifferentServerOrCatalog_IsAllowed()
    {
        const string production =
            "Server=tcp:compass-prod.database.windows.net,1433;Initial Catalog=compass;User ID=u;Password=p;";
        const string development =
            "Server=tcp:compass-dev.database.windows.net,1433;Initial Catalog=compass;User ID=u;Password=p;";

        ProductionToDevelopmentRefresh.EnsureDifferentDatabases(production, development);
    }

    [Fact]
    public void SameDatabase_IsRejected_WhenOnlyTheServerFormatDiffers()
    {
        const string production =
            "Server=tcp:compass-prod.database.windows.net,1433;Initial Catalog=compass;User ID=u;Password=p;";
        const string sameDatabase =
            "Data Source=compass-prod.database.windows.net;Initial Catalog=compass;User ID=u;Password=p;";

        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionToDevelopmentRefresh.EnsureDifferentDatabases(production, sameDatabase));
        Assert.Contains("same database", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
