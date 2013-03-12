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
    public interface IThingy
    {
        string TableName { get; }
        int Version { get; }
        string MakeRowKey();
        string EntityType { get; }
    }

    public abstract class AuditedEntityBase<TEntity> : TableEntity where TEntity : AuditedEntityBase<TEntity>
    {
        private static readonly string _entityType;
        static AuditedEntityBase() { _entityType = typeof(TEntity).FullName; }

        public abstract string TableName { get; }
        public abstract string MakeRowKey();
        public string EntityType { get; set; }
        public string AuditRecord { get; set; }

        static readonly CloudTableClient tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
        public AuditedEntityBase()
        {
            table = GetTable();
            EntityType = _entityType;
        }
        protected CloudTable GetTable() {  return GetTable(TableName); }
        protected static CloudTable GetTable(string tableName) { return tableClient.GetTableReference(tableName); }
        protected readonly CloudTable table;
        public int Version { get; set; }

        public async Task Save(string audit)
        {
            this.Version = Version + 1;
            AuditRecord = audit;
            this.RowKey = MakeRowKey();
            await table.AsyncExecute(TableOperation.Insert((TEntity)this));
        }
    }
    public class ProgressionEntity : AuditedEntityBase<ProgressionEntity>, IThingy
    {
        private const string TitleId = "SomeGame";
        private const string _tableName = "Player";
        private const string _rowKey = "Progression";
        private const string _rowKeyEnd = "Progression_~";

        #region Table Stuff
        static ProgressionEntity() { GetTable(_tableName).AsyncCreateIfNotExist().Wait(); }
        public override string TableName { get { return _tableName; } }
        public ProgressionEntity()
        {
        }
        #endregion

        public ProgressionEntity(ulong playerId)
        {
            this.PartitionKey = MakePk(playerId);
        }

        private static string MakePk(ulong playerId)
        {
            return string.Format("{0}_{1:x16}", TitleId, playerId);
        }
        public override string MakeRowKey()
        {
            return MakeRowKey(Version);
        }
        private static string MakeRowKey(int version) { return string.Format("{0}_{1}", _rowKey, version); }


        public static IEnumerable<ProgressionEntity> Get(ulong playerId) 
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, MakePk(playerId));
            string upperRKFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _rowKeyEnd);
            string lowerRkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, _rowKey);
            string combinedRowKeyFilter = TableQuery.CombineFilters(lowerRkFilter, TableOperators.And, upperRKFilter);
            string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, combinedRowKeyFilter);
            return GetTable(_tableName).QueryAll<ProgressionEntity>(combinedFilter);
        }
   
        public static async Task<ProgressionEntity> Get(ulong playerId, int version)
        {
            return await GetTable(_tableName).GetSingle<ProgressionEntity>(MakePk(playerId), MakeRowKey(version));
        }
    }

    public static class AsyncHelpers
    {
        public static IEnumerable<TEntity> QueryAll<TEntity>(this CloudTable table, string filter) where TEntity : AuditedEntityBase<TEntity>, new()
        {
            LogManager.GetCurrentClassLogger().Info("Query Filter: {0}", filter);
            var query = new TableQuery<TEntity>().Where(filter);
            TableQuerySegment<TEntity> currentSegment = null;
            
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                var token = currentSegment != null ? (TableContinuationToken) currentSegment.ContinuationToken : null;
                currentSegment = table.ExecuteQuerySegmented(query, token);
                foreach (var item in currentSegment.Results)
                {
                    yield return item;   
                }
            }
            //Task.Factory.FromAsync<ResultSegment<TEntity>>(, table.EndExecuteQuerySegmented<TEntity>, query, null, null);
        }
        public static async Task<bool> AsyncCreateIfNotExist(this CloudTable table) 
        {
            return await Task.Factory.FromAsync<bool>(table.BeginCreateIfNotExists, table.EndCreateIfNotExists, null);
        }
        public static async Task<TEntity> GetSingle<TEntity>(this CloudTable table, string pk, string rk) where TEntity : AuditedEntityBase<TEntity>, new()
        {
            return (await table.AsyncExecute(TableOperation.Retrieve<TEntity>(pk, rk))).Result as TEntity;
        }
        public static async Task<TableResult> AsyncExecute(this CloudTable table, TableOperation operation) 
        {
            return await Task.Factory.FromAsync<TableOperation, TableResult>(table.BeginExecute, table.EndExecute, operation, null);
        }
    }
}
