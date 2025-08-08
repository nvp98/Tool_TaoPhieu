using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tool_DATA_PR.Models;

namespace Tool_DATA_PR.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Tbl_Kip> Tbl_Kip { get; set; }
        public DbSet<Tbl_XeGoong> Tbl_XeGoong { get; set; }
        public DbSet<Tbl_BM_16_Phieu> Tbl_BM_16_Phieu { get; set; }
        public DbSet<Tbl_BM_16_GangLong> Tbl_BM_16_GangLong { get; set; }

    }
}
