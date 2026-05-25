using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory> { }
