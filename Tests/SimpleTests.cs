using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Progression;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace Tests
{
    [TestClass]
    public class SimpleTests
    {
        static readonly CloudTableClient tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
        static CloudTable table;
        [TestInitialize]
        public void Init() 
        {
            table = tableClient.GetTableReference("Player");
            table.CreateIfNotExists();
        }

        [TestMethod]
        public void SimpleGet_New()
        {
            var result = ProgressionEntity.Get(table, 5UL, 1);
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet()
        {
            ulong playerId = (ulong)new Random().Next();
            var entity = new ProgressionEntity(playerId);
            entity.Save(table, "Created").Wait();

            var result = ProgressionEntity.Get(table, playerId, 1);
            result.Result.Should().NotBeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet_v2()
        {
            ulong playerId = (ulong)new Random().Next();
            var entity = new ProgressionEntity(playerId);
            const int numEntities = 6;
            for (var i = 0; i < numEntities; i++)
                entity.Save(table, "edit " + i).Wait();

            for (var i = 1; i <= numEntities; i++)
            {
                var result = ProgressionEntity.Get(table, playerId, i).Result;
                result.Should().NotBeNull("v{0} is null", i);
                result.Version.Should().Be(i, "v{0} version should be {0}", i);
            }
        }

        [TestMethod]
        public void InsertAndSimpleGetAll()
        {
            ulong playerId = (ulong)new Random().Next();
            var entity = new ProgressionEntity(playerId);
            const int numEntities = 6;
            for (var i = 0; i < numEntities; i++)
                entity.Save(table, "edit " + i).Wait();

            var result = ProgressionEntity.Get(table, playerId).ToArray();
            result.Length.Should().Be(numEntities);
        }
        [TestMethod]
        public void InsertAndGetAllWithResolver()
        {
            ulong playerId = (ulong)new Random().Next();
            var entity1 = new ProgressionEntity(playerId) { Xp = 12345 };
            var entity2 = new CrapEntity { PartitionKey = entity1.PartitionKey, RowKey = "O Hai" };

            entity1.Save(table, "Created").Wait();
            entity2.Save(table, "Created").Wait();
            var result = table.QueryAll<TableEntity>(string.Format("(PartitionKey eq '{0}')", ProgressionEntity.MakePk(playerId)), EntityRegistry.GetAllKnownTypesResolver).ToArray();
            result.Length.Should().Be(2);
            result.Any(e => e.GetType() == typeof(ProgressionEntity)).Should().BeTrue("Progression not found");
            result.Any(e => e.GetType() == typeof(CrapEntity)).Should().BeTrue("CrapEntity not found");
            var p = result.First(e => e.GetType() == typeof(ProgressionEntity)) as ProgressionEntity;
            p.Version.Should().Be(1);
            p.Xp.Should().Be(12345);
        }

        [TestMethod]
        public void GetTableName()
        {
            ImmutableEntityBase<ProgressionEntity>.GetTableName().Should().Be("Player");
        }

        [TestMethod]
        public void GetTableName_NoAttr() {
            ImmutableEntityBase<CrapEntity>.GetTableName().Should().Be("Crap");
        }
        private class CrapEntity : ImmutableEntityBase<CrapEntity>
        {
            public override string MakeRowKey() { return "crap"; }
        }
    }
}

