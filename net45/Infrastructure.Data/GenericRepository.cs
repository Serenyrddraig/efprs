using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Data.Specification;

namespace Infrastructure.Data
{
    /// <summary>
    ///     Generic repository
    /// </summary>
    public partial class GenericRepository : IRepository, IDisposable
    {
        private readonly string _connectionStringName;
        private bool _bDisposed;
        private DbContext _context;
        private IUnitOfWork unitOfWork;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Repository&lt;TEntity&gt;" /> class.
        /// </summary>
        public GenericRepository()
            : this(string.Empty)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenericRepository&lt;TEntity&gt;" /> class.
        /// </summary>
        /// <param name="connectionStringName">Name of the connection string.</param>
        public GenericRepository(string connectionStringName)
        {
            _connectionStringName = connectionStringName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenericRepository&lt;TEntity&gt;" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public GenericRepository(DbContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            _context = context;
        }

        public GenericRepository(ObjectContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            _context = new DbContext(context, true);
        }

        protected DbContext DbContext
        {
            get
            {
                if (_context == null)
                {
                    _context = DbContextManager.GetContext(_connectionStringName);
                }
                return _context;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public TEntity GetByKey<TEntity>(object keyValue) where TEntity : class
        {
            EntityKey key = GetEntityKey<TEntity>(keyValue);

            object originalItem;
            if (((IObjectContextAdapter) DbContext).ObjectContext.TryGetObjectByKey(key, out originalItem))
            {
                return (TEntity) originalItem;
            }
            return default(TEntity);
        }

        public IQueryable<TEntity> GetQuery<TEntity>() where TEntity : class
        {
            /* 
             * From CTP4, I could always safely call this to return an IQueryable on DbContext 
             * then performed any with it without any problem:
             */
            // return DbContext.Set<TEntity>();

            /*
             * but with 4.1 release, when I call GetQuery<TEntity>().AsEnumerable(), there is an exception:
             * ... System.ObjectDisposedException : The ObjectContext instance has been disposed and can no longer be used for operations that require a connection.
             */

            // here is a work around: 
            // - cast DbContext to IObjectContextAdapter then get ObjectContext from it
            // - call CreateQuery<TEntity>(entityName) method on the ObjectContext
            // - perform querying on the returning IQueryable, and it works!
            string entityName = GetEntityName<TEntity>();
            return ((IObjectContextAdapter) DbContext).ObjectContext.CreateQuery<TEntity>(entityName);
        }

        public IQueryable<TEntity> GetQuery<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return GetQuery<TEntity>().Where(predicate);
        }

        public IQueryable<TEntity> GetQuery<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>());
        }

        public IEnumerable<TEntity> Get<TEntity, TOrderBy>(Expression<Func<TEntity, TOrderBy>> orderBy, int pageIndex,
            int pageSize, SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery<TEntity>().OrderBy(orderBy).Skip((pageIndex - 1)*pageSize).Take(pageSize).AsEnumerable();
            }
            return
                GetQuery<TEntity>()
                    .OrderByDescending(orderBy)
                    .Skip((pageIndex - 1)*pageSize)
                    .Take(pageSize)
                    .AsEnumerable();
        }

        public IEnumerable<TEntity> Get<TEntity, TOrderBy>(Expression<Func<TEntity, bool>> criteria,
            Expression<Func<TEntity, TOrderBy>> orderBy, int pageIndex, int pageSize,
            SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return GetQuery(criteria).OrderBy(orderBy).Skip((pageIndex - 1)*pageSize).Take(pageSize).AsEnumerable();
            }
            return
                GetQuery(criteria)
                    .OrderByDescending(orderBy)
                    .Skip((pageIndex - 1)*pageSize)
                    .Take(pageSize)
                    .AsEnumerable();
        }

        public IEnumerable<TEntity> Get<TEntity, TOrderBy>(ISpecification<TEntity> specification,
            Expression<Func<TEntity, TOrderBy>> orderBy, int pageIndex, int pageSize,
            SortOrder sortOrder = SortOrder.Ascending) where TEntity : class
        {
            if (sortOrder == SortOrder.Ascending)
            {
                return
                    specification.SatisfyingEntitiesFrom(GetQuery<TEntity>())
                        .OrderBy(orderBy)
                        .Skip((pageIndex - 1)*pageSize)
                        .Take(pageSize)
                        .AsEnumerable();
            }
            return
                specification.SatisfyingEntitiesFrom(GetQuery<TEntity>())
                    .OrderByDescending(orderBy)
                    .Skip((pageIndex - 1)*pageSize)
                    .Take(pageSize)
                    .AsEnumerable();
        }

        public TEntity Single<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Single<TEntity>(criteria);
        }

        public TEntity Single<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntityFrom(GetQuery<TEntity>());
        }

        public TEntity First<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return GetQuery<TEntity>().First(predicate);
        }

        public TEntity First<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).First();
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            DbContext.Set<TEntity>().Add(entity);
        }

        public void Attach<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            DbContext.Set<TEntity>().Attach(entity);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            DbContext.Set<TEntity>().Remove(entity);
        }

        public void Delete<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            IEnumerable<TEntity> records = Find(criteria);

            foreach (TEntity record in records)
            {
                Delete(record);
            }
        }

        public void Delete<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            IEnumerable<TEntity> records = Find(criteria);
            foreach (TEntity record in records)
            {
                Delete(record);
            }
        }

        public IEnumerable<TEntity> GetAll<TEntity>() where TEntity : class
        {
            return GetQuery<TEntity>().AsEnumerable();
        }

        public void Update<TEntity>(TEntity entity) where TEntity : class
        {
            string fqen = GetEntityName<TEntity>();

            object originalItem;
            EntityKey key = ((IObjectContextAdapter) DbContext).ObjectContext.CreateEntityKey(fqen, entity);
            if (((IObjectContextAdapter) DbContext).ObjectContext.TryGetObjectByKey(key, out originalItem))
            {
                ((IObjectContextAdapter) DbContext).ObjectContext.ApplyCurrentValues(key.EntitySetName, entity);
            }
        }

        public IEnumerable<TEntity> Find<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Where(criteria);
        }

        public TEntity FindOne<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Where(criteria).FirstOrDefault();
        }

        public TEntity FindOne<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntityFrom(GetQuery<TEntity>());
        }

        public IEnumerable<TEntity> Find<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).AsEnumerable();
        }

        public int Count<TEntity>() where TEntity : class
        {
            return GetQuery<TEntity>().Count();
        }

        public int Count<TEntity>(Expression<Func<TEntity, bool>> criteria) where TEntity : class
        {
            return GetQuery<TEntity>().Count(criteria);
        }

        public int Count<TEntity>(ISpecification<TEntity> criteria) where TEntity : class
        {
            return criteria.SatisfyingEntitiesFrom(GetQuery<TEntity>()).Count();
        }

        public IUnitOfWork UnitOfWork
        {
            get
            {
                if (unitOfWork == null)
                {
                    unitOfWork = new UnitOfWork(DbContext);
                }
                return unitOfWork;
            }
        }

        public TEntity Save<TEntity>(TEntity entity) where TEntity : class
        {
            Add(entity);
            DbContext.SaveChanges();
            return entity;
        }

        private EntityKey GetEntityKey<TEntity>(object keyValue) where TEntity : class
        {
            string entitySetName = GetEntityName<TEntity>();
            ObjectSet<TEntity> objectSet = ((IObjectContextAdapter) DbContext).ObjectContext.CreateObjectSet<TEntity>();
            string keyPropertyName = objectSet.EntitySet.ElementType.KeyMembers[0].ToString();
            var entityKey = new EntityKey(entitySetName, new[] {new EntityKeyMember(keyPropertyName, keyValue)});
            return entityKey;
        }

        private string GetEntityName<TEntity>() where TEntity : class
        {
            // Thanks to Kamyar Paykhan -  http://huyrua.wordpress.com/2011/04/13/entity-framework-4-poco-repository-and-specification-pattern-upgraded-to-ef-4-1/#comment-688
            string entitySetName = ((IObjectContextAdapter) DbContext).ObjectContext
                .MetadataWorkspace
                .GetEntityContainer(((IObjectContextAdapter) DbContext).ObjectContext.DefaultContainerName,
                    DataSpace.CSpace)
                .BaseEntitySets.Where(bes => bes.ElementType.Name == typeof (TEntity).Name).First().Name;
            return string.Format("{0}.{1}", ((IObjectContextAdapter) DbContext).ObjectContext.DefaultContainerName,
                entitySetName);
        }

        protected void Dispose(bool bDisposing)
        {
            if (!_bDisposed)
            {
                if (bDisposing)
                {
                    if (null != _context)
                    {
                        _context.Dispose();
                    }
                }
                _bDisposed = true;
            }
        }

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}