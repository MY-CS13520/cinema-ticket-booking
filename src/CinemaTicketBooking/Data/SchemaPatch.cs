using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Data;

/// <summary>
/// Adds sign-in tables to a database that was created before accounts existed.
/// A brand-new database already has these columns from the EF model.
/// </summary>
internal static class SchemaPatch
{
    public static async Task ApplyAsync(CinemaDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync();

        try
        {
            if (!await ExistsAsync(connection, "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Accounts'"))
            {
                await ExecuteAsync(connection, """
                    CREATE TABLE Accounts (
                        Id INTEGER NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        PasswordSalt TEXT NOT NULL,
                        Role TEXT NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_Accounts_Username ON Accounts (Username);
                    """);
            }

            if (!await ExistsAsync(connection, "SELECT 1 FROM pragma_table_info('Bookings') WHERE name = 'AccountId'"))
                await ExecuteAsync(connection, "ALTER TABLE Bookings ADD COLUMN AccountId INTEGER NULL;");
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private static async Task<bool> ExistsAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return result is not null and not DBNull;
    }

    private static async Task ExecuteAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
