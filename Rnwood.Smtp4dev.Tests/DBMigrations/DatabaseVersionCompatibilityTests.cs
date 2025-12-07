using System;
using System.Linq;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;
using System.Reflection;

namespace Rnwood.Smtp4dev.Tests.DBMigrations
{
    public class DatabaseVersionCompatibilityTests : IDisposable
    {
        private readonly SqliteInMemory _sqlLiteForTesting;

        public DatabaseVersionCompatibilityTests()
        {
            _sqlLiteForTesting = new SqliteInMemory();
        }

        [Fact]
        public void ValidateDatabaseVersionCompatibility_InMemoryDatabase_ShouldSkipCheck()
        {
            // Arrange & Act
            using var context = new Smtp4devDbContext(_sqlLiteForTesting.ContextOptions);
            
            // This should not throw an exception since in-memory databases skip the version check
            var validateMethod = typeof(Rnwood.Smtp4dev.Startup).GetMethod(
                "ValidateDatabaseVersionCompatibility", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            // Act & Assert - should not throw
            validateMethod.Should().NotBeNull();
            Action act = () => validateMethod.Invoke(null, new object[] { context });
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateDatabaseVersionCompatibility_CompatibleDatabase_ShouldNotThrow()
        {
            // Arrange - create a database with a known migration
            using var context = new Smtp4devDbContext(_sqlLiteForTesting.ContextOptions);
            
            // Act - validate the database compatibility
            var validateMethod = typeof(Rnwood.Smtp4dev.Startup).GetMethod(
                "ValidateDatabaseVersionCompatibility", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            // Assert - should not throw since the database was created with the current migrations
            validateMethod.Should().NotBeNull();
            Action act = () => validateMethod.Invoke(null, new object[] { context });
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateDatabaseVersionCompatibility_DatabaseWithUnknownMigration_ShouldThrow()
        {
            // Arrange - create a database and simulate it having an unknown migration
            using var context = new Smtp4devDbContext(_sqlLiteForTesting.ContextOptions);
            
            // Add a fake migration to the __EFMigrationsHistory table
            var fakeMigrationId = "99999999999999_FutureVersionMigration";
            context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{fakeMigrationId}', '999.0.0')");

            // Act & Assert
            var validateMethod = typeof(Rnwood.Smtp4dev.Startup).GetMethod(
                "ValidateDatabaseVersionCompatibility", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            validateMethod.Should().NotBeNull();
            Action act = () => validateMethod.Invoke(null, new object[] { context });
            
            var exceptionResult = act.Should().Throw<TargetInvocationException>();
            exceptionResult.Which.InnerException.Should().BeOfType<InvalidOperationException>();
            
            var innerException = exceptionResult.Which.InnerException as InvalidOperationException;
            innerException.Message.Should().Contain("Database version mismatch detected");
            innerException.Message.Should().Contain(fakeMigrationId);
            innerException.Message.Should().Contain("Upgrade to a newer version");
        }

        public void Dispose()
        {
            _sqlLiteForTesting?.Dispose();
        }
    }
}