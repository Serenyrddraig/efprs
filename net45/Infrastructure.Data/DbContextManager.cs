using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Infrastructure.Data
{
    public class DbContextManager
    {
        public static void Init(string[] mappingAssemblies, bool recreateDatabaseIfExist = false, bool lazyLoadingEnabled = true)
        {
            Init(DefaultConnectionStringName, mappingAssemblies, recreateDatabaseIfExist, lazyLoadingEnabled);
        }

        public static void Init(string connectionStringName, string[] mappingAssemblies, bool recreateDatabaseIfExist = false, bool lazyLoadingEnabled = true)
        {
            AddConfiguration(connectionStringName, mappingAssemblies, recreateDatabaseIfExist, lazyLoadingEnabled);
        }       

        public static void InitStorage(IDbContextStorage storage)
        {
            if (storage == null) 
            {
                throw new ArgumentNullException("storage");
            }
            if ((_storage != null) && (_storage != storage))
            {
                throw new ApplicationException("A storage mechanism has already been configured for this application");
            }            
            _storage = storage;
        }

        /// <summary>
        /// The default connection string name used if only one database is being communicated with.
        /// </summary>
        public const string DefaultConnectionStringName = "DefaultDb";  
      

        ///// <summary>
        ///// Used to get the a DbContext associated with a key; i.e., the key 
        ///// associated with an  for a specific database.
        ///// 
        ///// If you're only communicating with one database, you should call <see cref="Current" /> instead,
        ///// although you're certainly welcome to call this if you have the key available.
        ///// </summary>
        public static DbContext GetContext(string key = DefaultConnectionStringName)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = DefaultConnectionStringName;
            }

            if (_storage == null)
            {
                throw new ApplicationException("An IDbContextStorage has not been initialized");
            }

            DbContext context = null;
            lock (_syncLock)
            {
                if (!_dbContextBuilders.ContainsKey(key))
                {
                    throw new ApplicationException("An DbContextBuilder does not exist with a key of " + key);
                }

                context = _storage.GetDbContextForKey(key);

                if (context == null)
                {
                    var bldr = _dbContextBuilders[key];
                    var model = bldr.CompiledModel;  // force model compile                  
                    _storage.SetDbContextFactoryForKey(key, (IEFContextFactory<DbContext>)bldr);
                    context = _storage.GetDbContextForKey(key);
                }
            }
            return context;
        }

        private static void AddConfiguration(string connectionStringName, string[] mappingAssemblies, bool recreateDatabaseIfExists = false, bool lazyLoadingEnabled = true)
        {
            if (string.IsNullOrEmpty(connectionStringName))
            {
                throw new ArgumentNullException("connectionStringName");
            }

            if (mappingAssemblies == null)
            {
                throw new ArgumentNullException("mappingAssemblies");
            }

            lock (_syncLock)
            {
                _dbContextBuilders.Add(connectionStringName,
                    new DbContextBuilder<DbContext>(connectionStringName, mappingAssemblies, recreateDatabaseIfExists, lazyLoadingEnabled));
            }
        }        

        /// <summary>
        /// An application-specific implementation of IDbContextStorage must be setup either thru
        /// <see cref="InitStorage" /> or one of the <see cref="Init" /> overloads. 
        /// </summary>
        protected static IDbContextStorage _storage { get; set; }

        /// <summary>
        /// Maintains a dictionary of db context builders, one per database.  The key is a 
        /// connection string name used to look up the associated database, and used to decorate respective
        /// repositories. If only one database is being used, this dictionary contains a single
        /// factory with a key of <see cref="DefaultConnectionStringName" />.
        /// </summary>
        private static Dictionary<string, DbContextBuilder<DbContext>> _dbContextBuilders = new Dictionary<string, DbContextBuilder<DbContext>>();

        private static object _syncLock = new object();
    }
}
