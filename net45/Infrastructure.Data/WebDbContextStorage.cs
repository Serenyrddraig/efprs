using System.Data.Entity;
using System.Web;

namespace Infrastructure.Data
{
    public class WebDbContextStorage : IDbContextStorage
    {
        private const string STORAGE_KEY = "HttpContextObjectContextStorageKey";

        public WebDbContextStorage(HttpApplication app)
        {
            app.EndRequest += (sender, args) => { HttpContext.Current.Items.Remove(STORAGE_KEY); };
        }

        public DbContext GetDbContextForKey(string key)
        {
            SimpleDbContextStorage storage = GetSimpleDbContextStorage();
            return storage.GetDbContextForKey(key);
        }

        public void SetDbContextFactoryForKey(string factoryKey, IEFContextFactory<DbContext> context)
        {
            SimpleDbContextStorage storage = GetSimpleDbContextStorage();
            storage.SetDbContextFactoryForKey(factoryKey, context);
        }

        //public IEnumerable<DbContext> GetAllDbContexts()
        //{
        //    SimpleDbContextStorage storage = GetSimpleDbContextStorage();
        //    return storage.GetAllDbContexts();
        //}

        private SimpleDbContextStorage GetSimpleDbContextStorage()
        {
            HttpContext context = HttpContext.Current;
            var storage = context.Items[STORAGE_KEY] as SimpleDbContextStorage;
            if (storage == null)
            {
                storage = new SimpleDbContextStorage();
                context.Items[STORAGE_KEY] = storage;
            }
            return storage;
        }
    }
}