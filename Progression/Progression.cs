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

    public abstract class EBase<TEntity> : TableEntity where TEntity : EBase<TEntity>
    {
        public abstract string TableName { get; }
        public abstract string MakeRowKey();
        public string EntityType { get; set; }

        private static readonly string _entityType;
        static EBase() { _entityType = typeof(TEntity).FullName; }
        protected Logger logger = LogManager.GetCurrentClassLogger();
        static readonly CloudTableClient tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
        public EBase()
        {
            table = GetTable();
            EntityType = _entityType;
        }
        protected CloudTable GetTable() {  return GetTable(TableName); }
        protected static CloudTable GetTable(string tableName) { return tableClient.GetTableReference(tableName); }
        protected readonly CloudTable table;
        public int Version { get; set; }

        public async Task Save()
        {

            this.Version = Version + 1;
            this.RowKey = MakeRowKey();
            await table.AsyncExecute(TableOperation.Insert((TEntity)this));
        }
    }
    public class ProgressionEntity : EBase<ProgressionEntity>, IThingy
    {
        #region Table Stuff
        static ProgressionEntity()
        {
            GetTable(_tableName).AsyncCreateIfNotExist().Wait();
        }
        const string TitleId = "SomeGame";
        private const string _tableName = "Player";
        private const string _rowKey = "Progression";
        public override string TableName { get { return _tableName; } }
        public string WTF { get; set; }
        public ProgressionEntity()
        {
        }
#endregion
        public ProgressionEntity(ulong playerId)
        {
            this.PartitionKey = MakePk(playerId);
            logger.Info("x  Version: {0}", Version);
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

        
        public static async Task<ProgressionEntity> Get(ulong playerId, int version)
        {
            LogManager.GetCurrentClassLogger().Info("Getting {0} : {1}", MakePk(playerId), MakeRowKey(version));
            return (await GetTable(_tableName).AsyncExecute(TableOperation.Retrieve<ProgressionEntity>(MakePk(playerId), MakeRowKey(version)))).Result as ProgressionEntity;
        }
       
    }
    public static class WTF
    {
        public static async Task<bool> AsyncCreateIfNotExist(this CloudTable table) 
        {
            return await Task.Factory.FromAsync<bool>(table.BeginCreateIfNotExists, table.EndCreateIfNotExists, null);
        }
        public static async Task<TableResult> AsyncExecute(this CloudTable table, TableOperation operation) 
        {
            return await Task.Factory.FromAsync<TableOperation, TableResult>(table.BeginExecute, table.EndExecute, operation, null);
        }
    }
}
