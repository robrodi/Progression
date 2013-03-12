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
            var result = ProgressionEntity.Get(5UL);
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void InsertAndSimpleGet()
        {
            ulong xuid = (ulong)new Random().Next();
            var entity = new ProgressionEntity(xuid);
            entity.Save().Wait();

            var result = ProgressionEntity.Get(xuid);
            result.Result.Should().NotBeNull();
        }
    }
}
