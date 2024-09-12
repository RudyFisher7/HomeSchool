using DatabaseClients;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DatabaseClientsUnitTests
{
    public class CosmosNoSqlDatabaseClientUnitTest
    {
        private readonly IConfigurationRoot _configuration;

        private class MyTestModel
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public double Value { get; set; }
        }

        public CosmosNoSqlDatabaseClientUnitTest()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        [Fact]
        public async Task Test1()
        {
            var client = new CosmosNoSqlDatabaseClient(_configuration.GetConnectionString("AzureCosmosDBConnection") ?? throw new ArgumentNullException("AzureCosmosDBConnection"));

            var wasCreated = await client.CreateDatabaseIfNotExists(typeof(MyTestModel));

            var upsertMe = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Value = 1.034,
            };

            var upsertResult = await client.UpsertSingleItem(upsertMe);

            Assert.Equal(System.Net.HttpStatusCode.Created, upsertResult.StatusCode);
        }
    }
}