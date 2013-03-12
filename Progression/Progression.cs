using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progression
{
    [TableName("Player")]
    public class ProgressionEntity : ImmutableEntityBase<ProgressionEntity>
    {
        #region constants
        private const string TitleId = "SomeGame";
        private const string _rowKey = "Progression";
        private const string _rowKeyEnd = "Progression_~";
        #endregion
        #region Table Stuff
        public ProgressionEntity()
        {
        }
        #endregion

        public int Xp { get; set; }

        public ProgressionEntity(ulong playerId)
        {
            this.PartitionKey = MakePk(playerId);
        }

        #region Query Convenience Functions
        public static IEnumerable<ProgressionEntity> Get(CloudTable table, ulong playerId)
        {
            //string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, MakePk(playerId));
            //string upperRKFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _rowKeyEnd);
            //string lowerRkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, _rowKey);
            //string combinedRowKeyFilter = TableQuery.CombineFilters(lowerRkFilter, TableOperators.And, upperRKFilter);
            //string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, combinedRowKeyFilter);
            //return table.QueryAll<ProgressionEntity>(combinedFilter);

            // hax
            return table.QueryAll<ProgressionEntity>(string.Format("(PartitionKey eq '{0}') and ((RowKey gt '{1}') and (RowKey lt '{1}_~'))", MakePk(playerId), _rowKey));
        }

        public static async Task<ProgressionEntity> Get(CloudTable table, ulong playerId, int version)
        {
            return await table.GetSingle<ProgressionEntity>(MakePk(playerId), MakeRowKey(version));
        }
        #endregion 

        #region Keys
        public override string MakeRowKey()
        {
            return MakeRowKey(Version);
        }
        public static string MakePk(ulong playerId)
        {
            return string.Format("{0}_{1:x16}", TitleId, playerId);
        }
        private static string MakeRowKey(int version) { return string.Format("{0}_{1}", _rowKey, version); }
        #endregion
    }
}
