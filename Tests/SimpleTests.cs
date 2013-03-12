using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Progression;
using FluentAssertions;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
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
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            entity.Save(table, "Created").Wait();

            var result = ProgressionEntity.Get(table, xuid, 1);
            result.Result.Should().NotBeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet_v2()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            const int numEntities = 6;
            for (var i = 0; i < numEntities; i++)
                entity.Save(table, "edit " + i).Wait();

            for (var i = 1; i <= numEntities; i++)
            {
                var result = ProgressionEntity.Get(table, xuid, i).Result;
                result.Should().NotBeNull("v{0} is null", i);
                result.Version.Should().Be(i, "v{0} version should be {0}", i);
            }
        }

        [TestMethod]
        public void InsertAndSimpleGetAll()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            const int numEntities = 6;
            for (var i = 0; i < numEntities; i++)
                entity.Save(table, "edit " + i).Wait();


            var result = ProgressionEntity.Get(table, xuid).ToArray();
            result.Length.Should().Be(numEntities);
        }
    }
}
