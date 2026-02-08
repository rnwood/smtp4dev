using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rnwood.Smtp4dev.Data
{
    /// <summary>
    /// Design-time factory for creating Smtp4devDbContext instances.
    /// This allows EF Core tools to create the context without running the application.
    /// </summary>
    public class Smtp4devDbContextFactory : IDesignTimeDbContextFactory<Smtp4devDbContext>
    {
        public Smtp4devDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Smtp4devDbContext>();
            
            // Use SQLite for design-time operations
            optionsBuilder.UseSqlite("Data Source=smtp4dev.db");
            
            return new Smtp4devDbContext(optionsBuilder.Options);
        }
    }
}
