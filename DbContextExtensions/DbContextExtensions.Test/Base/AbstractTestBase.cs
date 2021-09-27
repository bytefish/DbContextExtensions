using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace DbContextExtensions.Test
{
    /// <summary>
    /// The base class for all Unit Tests, which ensures we have a DI Container and 
    /// provides a default IConfiguration read from the appsettings.json file.
    /// </summary>
    public abstract class AbstractTestBase
    {
        protected ServiceProvider services;

        /// <summary>
        /// Each Test Fixture should set up its Dependency Injection Container. This only has 
        /// to be done once per Fixture, hence we do this within a OneTimeSetUp-attributed 
        /// method.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var serviceCollection = new ServiceCollection();

            // Registers the Test Configuration:
            serviceCollection.AddSingleton(BuildConfiguration());

            // Register all other Services:            
            RegisterDependencies(serviceCollection);

            // And Build the Service Provider:
            services = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Builds the Configuration to used within the Tests.
        /// </summary>
        /// <returns>Configured IConfiguration</returns>
        private IConfiguration BuildConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();

            GetConfiguration(configurationBuilder);

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Configures the <see cref="IConfiguration"/> used in the Tests.
        /// </summary>
        /// <param name="configurationBuilder">Configuration Builder</param>
        protected virtual void GetConfiguration(ConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json");
        }

        /// <summary>
        /// Registers all Service Dependencies for the Unit Test.
        /// </summary>
        /// <param name="services">ServiceCollection to configure</param>
        protected abstract void RegisterDependencies(ServiceCollection services);

        /// <summary>
        /// Resolves a Service from the underlying Service Provider.
        /// </summary>
        /// <typeparam name="T">Type of the Service</typeparam>
        /// <returns>Registered Service</returns>
        protected T GetService<T>()
        {
            return services.GetService<T>();
        }
    }
}