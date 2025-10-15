using Microsoft.EntityFrameworkCore;
using Tool_DATA_PR.Models.Bkmis_PhoiThep;

namespace Tool_DATA_PR.Context
{
    public class BkDbContext : DbContext
    {
        public BkDbContext(DbContextOptions<BkDbContext> options) : base(options) { }

        public DbSet<BK_PhoiThep> BK_PhoiThep { get; set; }
    }
}