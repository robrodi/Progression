using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
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
    }
    public class ProgressionEntity : TableEntity, IThingy
    {
        public ProgressionEntity()
        {
        }
        const string TitleId = "SomeGame";
        private const string _tableName = "Player";
        private const string _rowKey = "Progression";
        static readonly CloudTableClient tableClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient();
        static readonly CloudTable table = tableClient.GetTableReference(_tableName);
        static ProgressionEntity() 
        {
            table.AsyncCreateIfNotExist().Wait();
        }
        public string TableName { get { return _tableName; }}
        public ProgressionEntity(ulong playerId)
        {
            this.PartitionKey = MakePk(playerId);
            this.RowKey = _rowKey;
        }

        private static string MakePk(ulong playerId)
        {
            return string.Format("{0}_{1:x16}", TitleId, playerId);
        }
        public async Task Save() 
        {
            await table.AsyncExecute(TableOperation.Insert(this));
        }
        public static async Task<ProgressionEntity> Get(ulong playerId)
        {
            return (await table.AsyncExecute(TableOperation.Retrieve<ProgressionEntity>(MakePk(playerId), _rowKey))).Result as ProgressionEntity;
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
