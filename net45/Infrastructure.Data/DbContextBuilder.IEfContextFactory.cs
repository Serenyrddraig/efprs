using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{

    public partial class DbContextBuilder<T> : IEFContextFactory<T> where T : DbContext
    {
        public DbContextBuilder(ConnectionStringSettings connectionStringSettings, string[] mappingAssemblies, bool recreateDatabaseIfExists, bool lazyLoadingEnabled)
        {
            CnStringSettings = connectionStringSettings;
            ApplyConfiguration(mappingAssemblies, recreateDatabaseIfExists, lazyLoadingEnabled);
        }

        private void ApplyConfiguration(string[] mappingAssemblies, bool recreateDatabaseIfExists, bool lazyLoadingEnabled)
        {
            Factory = DbProviderFactories.GetFactory(CnStringSettings.ProviderName);
            RecreateDatabaseIfExists = recreateDatabaseIfExists;
            LazyLoadingEnabled = lazyLoadingEnabled;

            AddConfigurations(mappingAssemblies);
            _compiledModel = new Lazy<DbCompiledModel>(ModelInitialization);
        }

        protected DbProviderFactory Factory { get; private set; }

        // If cached in IOC, there's a chance that multiple threads will attempt
        // to access the CompiledModel concurrently, hence Lazy<T>
        private Lazy<DbCompiledModel> _compiledModel;
        
        private  DbCompiledModel ModelInitialization()
        {
            var cn = Factory.CreateConnection();
            cn.ConnectionString = CnStringSettings.ConnectionString;

            var dbModel = this.Build(cn);
            var compiled = dbModel.Compile();
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

        public DbCompiledModel CompiledModel
        {
            get 
            {
                return _compiledModel.Value;
            }
        }

        private ObjectContext Create()
        {
            var cn = Factory.CreateConnection();
            cn.ConnectionString = CnStringSettings.ConnectionString;
            var ctx = CompiledModel.CreateObjectContext<ObjectContext>(cn);
            ctx.ContextOptions.LazyLoadingEnabled = LazyLoadingEnabled;
            return ctx;
        }


        T IEFContextFactory<T>.Create()
        {
            return (T)new DbContext(Create(), true);
        }
    }
}
