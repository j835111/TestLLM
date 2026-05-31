using System.Data.Common;
using CrudApi.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrudApi.Tests.Infrastructure;

public sealed class CrudApiApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection connection;

    public CrudApiApplicationFactory(string databaseName)
    {
        connection = new SqliteConnection($"Data Source={databaseName};Mode=Memory;Cache=Shared");
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbConnection>();

            services.AddSingleton(connection);

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options.UseSqlite(serviceProvider.GetRequiredService<DbConnection>()));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            connection.Dispose();
        }
    }
}
