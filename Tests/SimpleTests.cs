using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Progression;
using FluentAssertions;
namespace Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]
        public void SimpleGet_New()
        {
            var result = ProgressionEntity.Get(5UL, 1);
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            entity.Save("Created").Wait();

            var result = ProgressionEntity.Get(xuid, 1);
            result.Result.Should().NotBeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet_v2()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            const int numEntities = 6;
            for (var i = 0; i < numEntities; i++)
                entity.Save("edit " + i).Wait();

            for (var i = 1; i <= numEntities; i++)
            {
                var result = ProgressionEntity.Get(xuid, i).Result;
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
                entity.Save("edit " + i).Wait();
            

            var result = ProgressionEntity.Get(xuid).ToArray();
            result.Length.Should().Be(numEntities);
        }
    }
}
