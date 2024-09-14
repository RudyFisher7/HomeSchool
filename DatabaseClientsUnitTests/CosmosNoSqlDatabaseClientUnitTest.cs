using DatabaseClients;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Reflection;
using Utility.JsonSerialization;

namespace DatabaseClientsUnitTests
{
    public class CosmosNoSqlDatabaseClientUnitTest : IDisposable
    {
        private class MyTestModel
        {
            [JsonProperty("id")]
            [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringGuidConverter))]
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = string.Empty;

            public double Value { get; set; }

            public override bool Equals(object? other)
            {
                var result = false;

                if (other != null)
                {
                    if (other is MyTestModel)
                    {
                        var otherModel = other as MyTestModel;

                        result = (
                            Id.Equals(otherModel!.Id)
                            && Name.Equals(otherModel.Name)
                            && Math.Abs(Value - otherModel.Value) < 0.0001d
                        );
                    }
                    else
                    {
                        result = base.Equals(other);
                    }
                }

                return result;
            }
        }


        private readonly IConfigurationRoot _configuration;
        private readonly IDatabaseClient _databaseClient;
        private const string _DATABASE_NAME = "TestDatabase";
        private const string _PARTITION_KEY_VALUE = "Test";


        public CosmosNoSqlDatabaseClientUnitTest()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            _databaseClient = new CosmosNoSqlDatabaseClient(_configuration.GetConnectionString("AzureCosmosDBConnection") ?? throw new ArgumentNullException("AzureCosmosDBConnection"));
            var wasCreated = _databaseClient.CreateDatabaseIfNotExists(_DATABASE_NAME).Result;
            var wasCreated2 = _databaseClient.CreateCollectionIfNotExists(_DATABASE_NAME, typeof(MyTestModel), nameof(MyTestModel.Name)).Result;
        }


        public void Dispose()
        {
            _databaseClient.DeleteDatabase(_DATABASE_NAME);
        }


        [Fact]
        public async Task CreateSingleItemTest()
        {
            var model = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = _PARTITION_KEY_VALUE,
                Value = 1.034d,
            };

            var insertResult = await _databaseClient.CreateSingleItem(_DATABASE_NAME, model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.Created, insertResult.StatusCode);
        }


        [Fact]
        public async Task CreateAndGetSingleItemTest()
        {
            var model = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = _PARTITION_KEY_VALUE,
                Value = 1.034d,
            };

            var insertResult = await _databaseClient.CreateSingleItem(_DATABASE_NAME, model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.Created, insertResult.StatusCode);

            var getResult = await _databaseClient.GetSingleItem<MyTestModel, string>(_DATABASE_NAME, model.Id.ToString(), model.Name);

            Assert.NotNull(getResult);
            Assert.Equal(model, getResult);
        }


        [Fact]
        public async Task CreateAndUpdateSingleItemTest()
        {
            var model = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = _PARTITION_KEY_VALUE,
                Value = 1.034d,
            };

            var insertResult = await _databaseClient.CreateSingleItem(_DATABASE_NAME, model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.Created, insertResult.StatusCode);

            model.Value = 0.9d;

            var updateResult = await _databaseClient.UpdateSingleItem(_DATABASE_NAME, model.Id.ToString(), model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.OK, updateResult.StatusCode);
            Assert.Equal(model, updateResult.Resource);
        }


        [Fact]
        public async Task CreateAndDeleteSingleItemTest()
        {
            var model = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = _PARTITION_KEY_VALUE,
                Value = 1.034d,
            };

            var insertResult = await _databaseClient.CreateSingleItem(_DATABASE_NAME, model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.Created, insertResult.StatusCode);

            var deleteResult = await _databaseClient.DeleteSingleItem<MyTestModel, string>(_DATABASE_NAME, model.Id.ToString(), model.Name);

            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResult.StatusCode);
            Assert.Null(deleteResult.Resource);
        }


        [Fact]
        public async Task UpsertSingleItemTest()
        {
            var model = new MyTestModel()
            {
                Id = Guid.NewGuid(),
                Name = _PARTITION_KEY_VALUE,
                Value = 1.034d,
            };

            var upsertResult = await _databaseClient.CreateSingleItem(_DATABASE_NAME, model, model.Name);

            Assert.Equal(System.Net.HttpStatusCode.Created, upsertResult.StatusCode);
        }
    }
}