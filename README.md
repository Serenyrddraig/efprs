efprs
=====

Entity Framework POCO, Repository and Specification Pattern

This branch resolves an issue with the original implementation that occurs when attempting to use GenericRepository
based DALs sourced from an IOC (such as Microsoft's Unity) when those DALs operate in a multi-threaded environment
such as WCF. The major change was to cache the DbCompiledModel (the DbContext factory) rather than the DbContext 
itself. In addition, IDisposable was added to some components to resolve issues that occurred in unit testing. A few
breaking changes were introduced principally around former cache features. For example, GetAllContexts() has no
relevant meaning when DbContexts are manufactured per instance.

IOC integration is faciliated by the IEFContextFactory interface. Suggested usage is included in code comments.
