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
    public static class EntityRegistry
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static object Mutex = new object();
        private static readonly Dictionary<string, Type> types = new Dictionary<string, Type>();

        public static void Register(Type t) 
        {
            // lock + doublecheck
            if (types.ContainsKey(t.FullName)) return;
            lock (Mutex)
            {
                if (types.ContainsKey(t.FullName)) return;
                types.Add(t.FullName, t);
            }
        }
        public static TableEntity GetAllKnownTypesResolver(string partitionKey, 
                                                                            string rowKey, 
                                                                            DateTimeOffset timestamp,
                                                                            IDictionary<string, EntityProperty> properties,
                                                                            string etag) 
        {
            string shapeType = properties["EntityType"].StringValue;
            TableEntity result = ResolveType(shapeType);

            result.PartitionKey = partitionKey;
            result.RowKey = rowKey;
            result.Timestamp = timestamp;
            result.ETag = etag;
            result.ReadEntity(properties, null);

            return result;
        }

        private static TableEntity ResolveType(string shapeType)
        {
            TableEntity result = null;
            if (types.ContainsKey(shapeType))
            {
                logger.Info("ShapeType Found: {0}", shapeType);
                result = Activator.CreateInstance(types[shapeType]) as TableEntity;
            }
            else
            {
                logger.Info("ShapeType Not Found: {0}", shapeType);
                // try register
                var t = Type.GetType(shapeType);
                if (t != null)
                {
                    logger.Info("ShapeType resolved and registered: {0}", shapeType);
                    if (t.IsSubclassOf(typeof(TableEntity)))
                    {
                        Register(t);
                        result = Activator.CreateInstance(types[shapeType]) as TableEntity;
                    }
                    else throw new ArgumentOutOfRangeException("Type is not a subclass of Table Entity and cannot be resolved");
                }
                else
                    result = new TableEntity();
            }
            return result;
        }
    }
    /// <summary>
    /// EntityBase type that uses reflection to set a table name and entity type properties.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity</typeparam>
    public abstract class EntityBase<TEntity> : TableEntity where TEntity : ImmutableEntityBase<TEntity>, new()
    {
        protected static readonly string _entityType;
        protected static readonly string _tableName;
        static EntityBase()
        { 
            _entityType = typeof(TEntity).FullName;
            _tableName = GetTableName();
            EntityRegistry.Register(typeof(TEntity));
        }

        public string TableName { get { return _tableName; } }
        public string EntityType { get { return _entityType; } set { } }

        public static string GetTableName()
        {
            var attrs = typeof(TEntity).GetCustomAttributes(typeof(TableNameAttribute), true).FirstOrDefault() as TableNameAttribute;
            return attrs != null ? attrs.Name : typeof(TEntity).Name.Replace("Entity", string.Empty);
        }
    }

    public abstract class ImmutableEntityBase<TEntity> : EntityBase<TEntity> where TEntity : ImmutableEntityBase<TEntity>, new()
    {
        public string AuditRecord { get; set; }
        public int Version { get; set; }

        public abstract string MakeRowKey();

        public async Task Save(CloudTable table, string audit)
        {
            this.Version++;
            AuditRecord = audit;
            this.RowKey = MakeRowKey();
            await table.AsyncExecute(TableOperation.Insert((TEntity)this));
        }
    }

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

        public static string MakePk(ulong playerId)
        {
            return string.Format("{0}_{1:x16}", TitleId, playerId);
        }
        public override string MakeRowKey()
        {
            return MakeRowKey(Version);
        }
        private static string MakeRowKey(int version) { return string.Format("{0}_{1}", _rowKey, version); }

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
    }

}
