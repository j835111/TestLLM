using System.Data.Common;
using CrudApi.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Tests.Infrastructure;

public sealed class SqliteAppDbContextScope : IDisposable
{
    private readonly DbConnection connection;

    public SqliteAppDbContextScope()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        DbContext = new AppDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public AppDbContext DbContext { get; }

    public void Dispose()
    {
        DbContext.Dispose();
        connection.Dispose();
    }
}
