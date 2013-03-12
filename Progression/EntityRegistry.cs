using Microsoft.WindowsAzure.Storage.Table;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Progression
{
    /// <summary>
    /// Registers all known Entity Types 
    /// </summary>
    public static class EntityRegistry
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static object Mutex = new object();
        private static readonly Dictionary<string, Type> types = new Dictionary<string, Type>();

        /// <summary>
        /// Registers a  type to be generated in <see cref="GetAllKnownTypesResolver"/> if a matching &quot;EntityType&quot; column is populated.
        /// </summary>
        /// <param name="t">The type to register.</param>
        public static void Register(Type t)
        {
            // lock + doublecheck
            if (types.ContainsKey(t.FullName)) return;
            lock (Mutex)
            {
                if (types.ContainsKey(t.FullName)) return;
                if (t.IsSubclassOf(typeof(TableEntity)))
                    types.Add(t.FullName, t);
                else
                    throw new ArgumentOutOfRangeException("Type is not a subclass of Table Entity and cannot be resolved");
            }
        }

        /// <summary>
        /// An implementation of the <see cref="EntityResolver<TableEntity>"/> delegate that uses the <see cref="EntityBase<T>.EntityType"/> column to generate an instance of an entity and populate its properties.
        /// </summary>
        /// <param name="partitionKey">The pk.</param>
        /// <param name="rowKey">The rk.</param>
        /// <param name="timestamp">The last updated time.</param>
        /// <param name="properties">The property collection.</param>
        /// <param name="etag">The etag.</param>
        /// <returns>A populated table entity.</returns>
        public static TableEntity GetAllKnownTypesResolver(string partitionKey,
                                                                            string rowKey,
                                                                            DateTimeOffset timestamp,
                                                                            IDictionary<string, EntityProperty> properties,
                                                                            string etag)
        {
            
            EntityProperty typeProperty;
            string shapeType = properties.TryGetValue("EntityType", out typeProperty) ? typeProperty.StringValue : null;
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
            if (string.IsNullOrEmpty(shapeType)) return new TableEntity();

            if (types.ContainsKey(shapeType))
            {
                logger.Debug("ShapeType Found: {0}", shapeType);
                return Activator.CreateInstance(types[shapeType]) as TableEntity;
            }
            else
            {
                logger.Debug("ShapeType Not Found: {0}", shapeType);
                // try register
                var t = Type.GetType(shapeType);
                if (t != null)
                {
                    logger.Debug("ShapeType resolved and registered: {0}", shapeType);
                    Register(t);
                    return Activator.CreateInstance(types[shapeType]) as TableEntity;
                }
                else
                {
                    logger.Warn("Unknown Type {0}.  Returning TableEntity.");
                    return new TableEntity();
                }
            }
        }
    }
}
