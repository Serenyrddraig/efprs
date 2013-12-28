namespace Infrastructure.Data
{
    /// <summary>
    ///     IEFContextFactory provides an interface to a cached compiled model, enabling
    ///     on demand creation of a ObjectContext or DbContext and thus injection (more likely service location)
    ///     for the GenericRepository non-default constructor.
    ///     A container registration routine (e.g. a UnityContainerExtension) could register compiled model instances
    ///     using connection string names to disambiguate as follows:
    ///     private static void RegisterContextFactory(IUnityContainer container, string ConnectionString, IEnumerable
    ///     <string>
    ///         assemblies)
    ///         {
    ///         var bldr = new DbContextBuilder
    ///         <DbContext>
    ///             (ConnectionString, assemblies.ToArray(),
    ///             false, false);
    ///             var model = bldr.CompiledModel; // Force model compile now instead of on first access
    ///             container.RegisterInstance(typeof(IEFContextFactory
    ///             <DbContext>
    ///                 ), conString,
    ///                 bldr as IEFContextFactory
    ///                 <DbContext>
    ///                     , new ContainerControlledLifetimeManager());
    ///                     }
    ///                     Repository construction could then access the factory for a repository unique context
    ///                     var factory = container.Resolve<IEFContextFactory
    ///                     <DbContext>
    ///                         >(ConnectionString);
    ///                         var repository =  new GenericRepository(factory.Create());
    /// </summary>
    /// <typeparam name="T">Object Context or DbContext</typeparam>
    public interface IEFContextFactory<T> where T : class
    {
        T Create();
    }
}