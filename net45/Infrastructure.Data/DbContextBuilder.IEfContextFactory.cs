using System;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace Infrastructure.Data
{
    public partial class DbContextBuilder<T> : IEFContextFactory<T> where T : DbContext
    {
        private Lazy<DbCompiledModel> _compiledModel;

        public DbContextBuilder(ConnectionStringSettings connectionStringSettings, string[] mappingAssemblies,
            bool recreateDatabaseIfExists, bool lazyLoadingEnabled)
        {
            CnStringSettings = connectionStringSettings;
            ApplyConfiguration(mappingAssemblies, recreateDatabaseIfExists, lazyLoadingEnabled);
        }

        protected DbProviderFactory Factory { get; private set; }

        // If cached in IOC, there's a chance that multiple threads will attempt
        // to access the CompiledModel concurrently, hence Lazy<T>

        public DbCompiledModel CompiledModel
        {
            get { return _compiledModel.Value; }
        }


        T IEFContextFactory<T>.Create()
        {
            return (T) new DbContext(Create(), true);
        }

        private void ApplyConfiguration(string[] mappingAssemblies, bool recreateDatabaseIfExists,
            bool lazyLoadingEnabled)
        {
            Factory = DbProviderFactories.GetFactory(CnStringSettings.ProviderName);
            RecreateDatabaseIfExists = recreateDatabaseIfExists;
            LazyLoadingEnabled = lazyLoadingEnabled;

            AddConfigurations(mappingAssemblies);
            _compiledModel = new Lazy<DbCompiledModel>(ModelInitialization);
        }

        private DbCompiledModel ModelInitialization()
        {
            DbConnection cn = Factory.CreateConnection();
            cn.ConnectionString = CnStringSettings.ConnectionString;

            DbModel dbModel = Build(cn);
            DbCompiledModel compiled = dbModel.Compile();
            var context = compiled.CreateObjectContext<ObjectContext>(cn);
            if (!context.DatabaseExists())
            {
                context.CreateDatabase();
            }
            else if (RecreateDatabaseIfExists)
            {
                context.DeleteDatabase();
                context.CreateDatabase();
            }
            return compiled;
        }

        private ObjectContext Create()
        {
            DbConnection cn = Factory.CreateConnection();
            cn.ConnectionString = CnStringSettings.ConnectionString;
            var ctx = CompiledModel.CreateObjectContext<ObjectContext>(cn);
            ctx.ContextOptions.LazyLoadingEnabled = LazyLoadingEnabled;
            return ctx;
        }
    }
}