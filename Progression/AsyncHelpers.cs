using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using NLog;
namespace Progression
{
    public static class AsyncHelpers
    {
        public static IEnumerable<TEntity> QueryAll<TEntity>(this CloudTable table, string filter, EntityResolver<TEntity> resolver = null) where TEntity : TableEntity, new()
        {
            LogManager.GetCurrentClassLogger().Info("Query Filter: {0}", filter);
            var query = new TableQuery<TEntity>().Where(filter);
            TableQuerySegment<TEntity> currentSegment = null;

            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                var token = currentSegment != null ? (TableContinuationToken)currentSegment.ContinuationToken : null;

                currentSegment = resolver != null ? table.ExecuteQuerySegmented(query, resolver, token) : table.ExecuteQuerySegmented(query, token);
                foreach (var item in currentSegment.Results)
                    yield return item;
            }
        }
       
        public static async Task<bool> AsyncCreateIfNotExist(this CloudTable table)
        {
            return await Task.Factory.FromAsync<bool>(table.BeginCreateIfNotExists, table.EndCreateIfNotExists, null);
        }
        public static async Task<TEntity> GetSingle<TEntity>(this CloudTable table, string pk, string rk) where TEntity : ImmutableEntityBase<TEntity>, new()
        {
            return (await table.AsyncExecute(TableOperation.Retrieve<TEntity>(pk, rk))).Result as TEntity;
        }
        public static async Task<TableResult> AsyncExecute(this CloudTable table, TableOperation operation)
        {
            return await Task.Factory.FromAsync<TableOperation, TableResult>(table.BeginExecute, table.EndExecute, operation, null);
        }
    }

}
