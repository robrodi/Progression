namespace Progression
{
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// EntityBase type that uses reflection to set a table name and entity type properties.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity</typeparam>
    public abstract class EntityBase<TEntity> : TableEntity where TEntity : ImmutableEntityBase<TEntity>, new()
    {
        protected static readonly string _entityType;
        protected static readonly string _tableName;

        /// <summary>
        /// Creates the registration using reflection.  This only happens once.
        /// </summary>
        static EntityBase()
        {
            _entityType = typeof(TEntity).FullName;
            _tableName = GetTableName();
            EntityRegistry.Register(typeof(TEntity));
        }

        /// <summary>
        /// The name of the table in which this entity will be stored.
        /// </summary>
        public string TableName { get { return _tableName; } }

        /// <summary>
        /// The type of entity which will be stored in the table for resolution later.
        /// </summary>
        public string EntityType { get { return _entityType; } set { } }

        /// <summary>
        /// Uses reflection to retrieve the table name from either the <see cref="TableNameAttribute"/> or the class name itself.
        /// </summary>
        /// <returns>The name of the table.</returns>
        public static string GetTableName()
        {
            var attrs = typeof(TEntity).GetCustomAttributes(typeof(TableNameAttribute), true).FirstOrDefault() as TableNameAttribute;
            return attrs != null ? attrs.Name : typeof(TEntity).Name.Replace("Entity", string.Empty);
        }
    }
}
