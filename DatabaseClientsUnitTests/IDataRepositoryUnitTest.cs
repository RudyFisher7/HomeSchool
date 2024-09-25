using DataRepositories;
using DataRepositories.Attributes;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data.SqlTypes;
using Utility.JsonSerialization;

namespace IDataRepositoryUnitTests
{
    public class IDataRepositoryUnitTest : IDisposable
    {
        private class MyTestModel
        {
            [JsonProperty("id")]
            [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringGuidConverter))]
            [SqlType(typeof(SqlGuid), SqlTypeEnum.UNIQUEIDENTIFIER, SqlConstraintEnum.PRIMARY_KEY)]
            public Guid Id { get; set; } = Guid.NewGuid();

            [SqlType(typeof(SqlString), SqlTypeEnum.NVARCHAR)]
            public string Name { get; set; } = string.Empty;

            [SqlType(typeof(SqlDouble), SqlTypeEnum.FLOAT)]
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


        private readonly List<IDataRepository> _dataRepositories = new List<IDataRepository>();
        private readonly IConfigurationRoot _configuration;
        private const string _DATABASE_NAME = "TestDatabase";
        private const string _PARTITION_KEY_VALUE = "Test";


        public IDataRepositoryUnitTest()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            _dataRepositories.Add(new CosmosDataRepository(_configuration.GetConnectionString("AzureCosmosDBConnection") ?? throw new ArgumentNullException("AzureCosmosDBConnection")));
            _dataRepositories.Add(new SqlDataRepository(_configuration.GetConnectionString("AzureSqlDBConnection") ?? throw new ArgumentNullException("AzureSqlDBConnection")));

            foreach (var repository in _dataRepositories)
            {
                var wasCreated = repository.CreateDatabaseIfNotExists(_DATABASE_NAME).Result;
                var wasCreated2 = repository.CreateCollectionIfNotExists(_DATABASE_NAME, typeof(MyTestModel), nameof(MyTestModel.Name)).Result;
            }
        }


        public void Dispose()
        {
            foreach (var repository in _dataRepositories)
            {
                repository.DeleteDatabaseIfExists(_DATABASE_NAME);
            }
        }


        [Fact]
        public void PassThroughTest()
        {
            int i = 0;
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


            foreach (var repository in _dataRepositories)
            {
                var insertResult = await repository.CreateSingleItem(_DATABASE_NAME, model, model.Name);

                Assert.True(insertResult.Success, insertResult.Message);
            }
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

            foreach (var repository in _dataRepositories)
            {
                var insertResult = await repository.CreateSingleItem(_DATABASE_NAME, model, model.Name);

                Assert.True(insertResult.Success, insertResult.Message);

                var getResult = await repository.ReadSingleItem<MyTestModel, string>(_DATABASE_NAME, model.Id.ToString(), model.Name);

                Assert.True(getResult.Success, insertResult.Message);
                Assert.NotNull(getResult);
                Assert.Equal(model, getResult.Item);
            }
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

            foreach (var repository in _dataRepositories)
            {
                var insertResult = await repository.CreateSingleItem(_DATABASE_NAME, model, model.Name);

                Assert.True(insertResult.Success, insertResult.Message);
                Assert.NotNull(insertResult.Item);

                model.Value = 0.9d;

                var updateResult = await repository.UpdateSingleItem(_DATABASE_NAME, model.Id.ToString(), model, model.Name);

                Assert.True(updateResult.Success, updateResult.Message);
                Assert.Equal(model, updateResult.Item);
            }
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

            foreach (var repository in _dataRepositories)
            {
                var insertResult = await repository.CreateSingleItem(_DATABASE_NAME, model, model.Name);

                Assert.True(insertResult.Success, insertResult.Message);
                Assert.NotNull(insertResult.Item);

                var deleteResult = await repository.DeleteSingleItem<MyTestModel, string>(_DATABASE_NAME, model.Id.ToString(), model.Name);

                Assert.True(deleteResult.Success, deleteResult.Message);
            }
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

            foreach (var repository in _dataRepositories)
            {
                var upsertResult = await repository.CreateSingleItem(_DATABASE_NAME, model, model.Name);

                Assert.True(upsertResult.Success, upsertResult.Message);
                Assert.NotNull(upsertResult.Item);
            }
        }
    }
}