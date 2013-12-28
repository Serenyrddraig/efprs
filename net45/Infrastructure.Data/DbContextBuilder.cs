using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.IO;
using System.Reflection;

namespace Infrastructure.Data
{
    public partial class DbContextBuilder<T> : DbModelBuilder
    {
        public DbContextBuilder(string connectionStringName, string[] mappingAssemblies, bool recreateDatabaseIfExists,
            bool lazyLoadingEnabled)
        {
            CnStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            ApplyConfiguration(mappingAssemblies, recreateDatabaseIfExists, lazyLoadingEnabled);
        }

        private ConnectionStringSettings CnStringSettings { get; set; }
        private bool RecreateDatabaseIfExists { get; set; }
        private bool LazyLoadingEnabled { get; set; }


        /// <summary>
        ///     Adds mapping classes contained in provided assemblies and register entities as well
        /// </summary>
        /// <param name="mappingAssemblies"></param>
        private void AddConfigurations(string[] mappingAssemblies)
        {
            if (mappingAssemblies == null || mappingAssemblies.Length == 0)
            {
                throw new ArgumentNullException("mappingAssemblies", "You must specify at least one mapping assembly");
            }

            bool hasMappingClass = false;
            foreach (string mappingAssembly in mappingAssemblies)
            {
                Assembly asm = Assembly.LoadFrom(MakeLoadReadyAssemblyName(mappingAssembly));

                foreach (Type type in asm.GetTypes())
                {
                    if (!type.IsAbstract)
                    {
                        if (type.BaseType.IsGenericType && IsMappingClass(type.BaseType))
                        {
                            hasMappingClass = true;

                            // http://areaofinterest.wordpress.com/2010/12/08/dynamically-load-entity-configurations-in-ef-codefirst-ctp5/
                            dynamic configurationInstance = Activator.CreateInstance(type);
                            Configurations.Add(configurationInstance);
                        }
                    }
                }
            }

            if (!hasMappingClass)
            {
                throw new ArgumentException("No mapping class found!");
            }
        }

        /// <summary>
        ///     Determines whether a type is a subclass of entity mapping type
        /// </summary>
        /// <param name="mappingType">Type of the mapping.</param>
        /// <returns>
        ///     <c>true</c> if it is mapping class; otherwise, <c>false</c>.
        /// </returns>
        private bool IsMappingClass(Type mappingType)
        {
            Type baseType = typeof (EntityTypeConfiguration<>);
            if (mappingType.GetGenericTypeDefinition() == baseType)
            {
                return true;
            }
            if ((mappingType.BaseType != null) &&
                !mappingType.BaseType.IsAbstract &&
                mappingType.BaseType.IsGenericType)
            {
                return IsMappingClass(mappingType.BaseType);
            }
            return false;
        }

        /// <summary>
        ///     Ensures the assembly name is qualified
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private static string MakeLoadReadyAssemblyName(string assemblyName)
        {
            string asmFile = (assemblyName.IndexOf(".dll") == -1)
                ? assemblyName.Trim() + ".dll"
                : assemblyName.Trim();

            if (!Path.IsPathRooted(asmFile))
            {
                // If asmFile is not rooted, root it to this assembly's path
                // Assembly.GetExecutingAssembly().Location does not work in xUnit
                string location =
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(@"file:\".Length);
                asmFile = Path.Combine(location, asmFile);
            }
            return asmFile;
        }
    }
}