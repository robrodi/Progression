using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
namespace Progression
{
    /// <summary>
    /// Creates an "immutable" entity.  This just means it increments the version property when saved.
    /// </summary>
    /// <remarks>It's up to the subclass to ensure that the rk takes the version into account.</remarks>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class ImmutableEntityBase<TEntity> : EntityBase<TEntity> where TEntity : ImmutableEntityBase<TEntity>, new()
    {
        /// <summary>
        /// A string intended to contain the changes that were made on this version.
        /// </summary>
        public string AuditRecord { get; set; }

        /// <summary>
        /// The version of the record.  Starts @ 1;
        /// </summary>
        public int Version { get; set; }

        public abstract string MakeRowKey();

        /// <summary>
        /// Increment the version and save the entity.
        /// </summary>
        /// <param name="table">The <see cref="CloudTable"/> to which the entity is saved.</param>
        /// <param name="audit">"A message explaining the change</param>
        /// <returns>A task</returns>
        public async Task Save(CloudTable table, string audit)
        {
            this.Version++;
            AuditRecord = audit;
            this.RowKey = MakeRowKey();
            await table.AsyncExecute(TableOperation.Insert((TEntity)this));
        }
    }
}
