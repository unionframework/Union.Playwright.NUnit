using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Core;

/// <summary>
/// Base class for configuring test session dependencies.
/// Creates a DI container and provides factory method for test sessions.
/// </summary>
/// <typeparam name="T">The concrete test session type.</typeparam>
public abstract class TestSessionProvider<T> where T : class, ITestSession
{
    private readonly IHost _testApp;

    /// <summary>
    /// Initializes the DI container with default and custom services.
    /// </summary>
    protected TestSessionProvider()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, services) =>
        {
            // Register framework services
            services.AddScoped<IWeb, Web>();
            services.AddScoped<ITestSession, T>();

            // Register optional settings from configuration
            var settings = context.Configuration
                .GetSection("TestSettings")
                .Get<TestSettings>();

            if (settings != null)
            {
                services.AddSingleton(settings);
            }
            else
            {
                services.AddSingleton(TestSettings.Default);
            }

            // Allow derived class to register custom services
            ConfigureServices(services);
        });

        _testApp = builder.Build();
    }

    /// <summary>
    /// Gets the DI host for advanced scenarios.
    /// </summary>
    protected IHost TestHost => _testApp;

    /// <summary>
    /// Creates a new test session with DI scope but without a browser context.
    /// The browser context should be attached later via ScopedTestSession.SetContext().
    /// This enables Session to be available before ContextOptions() is called.
    /// </summary>
    /// <returns>A scoped test session that should be disposed after the test.</returns>
    public ScopedTestSession CreateTestSession()
    {
        // Create new DI scope for this test
        var scope = _testApp.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        // Resolve the test session (and all its dependencies)
        var session = provider.GetRequiredService<ITestSession>();

        // Register services with the Web routing system
        var web = provider.GetRequiredService<IWeb>();
        foreach (var service in session.GetServices())
        {
            web.RegisterService(service);
        }

        return new ScopedTestSession(session, scope);
    }

    /// <summary>
    /// Override to register custom services for tests.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public abstract void ConfigureServices(IServiceCollection services);
}
