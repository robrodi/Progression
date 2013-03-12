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
            entity.Save().Wait();

            var result = ProgressionEntity.Get(xuid, 1);
            result.Result.Should().NotBeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet_v2()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            entity.Save().Wait();
            entity.Save().Wait();
            entity.Save().Wait();
            entity.Save().Wait();
            entity.Save().Wait();
            entity.Save().Wait();

            for (var i = 1; i <= 2; i++)
            {
                var result = ProgressionEntity.Get(xuid, i).Result;
                result.Should().NotBeNull("v{0} is null", i);
                result.Version.Should().Be(i, "v{0} version should be {0}", i);
            }
        }
    }
}
