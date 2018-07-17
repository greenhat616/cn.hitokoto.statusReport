using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace cn.hitokoto.statusReport.DbModels {
    public class StatisticsContext : DbContext {
        static string workDir = System.AppDomain.CurrentDomain.BaseDirectory; // 获取程序目录
        static string databaseDir = workDir + "/database";
        static string DBFile = databaseDir + "/Statistics.db";

        public DbSet<status> Status { get; set; }
        public DbSet<log> Log { get; set; }
        public DbSet<buffer> Buffer { get; set; }

        protected override void OnModelCreating (ModelBuilder modelBuilder) {
            modelBuilder.Entity<status>()
                .HasKey(c => c.id);
            modelBuilder.Entity<log>()
                .HasKey(c => c.id);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // optionsBuilder.UseSqlite(@"Data Source=Statistics.db");
            optionsBuilder.UseSqlite(@"Data Source=" + DBFile);
        }
    }
    public class log {
        public int id { get; set; }

        public string identification { get; set; } // 标识
        public string type { get; set; } // 时间类型， 例如: down, up
        public long ts { get; set; } // 时间戳
    }

    public class status {
        public int id { get; set; }
        public string identification { get; set; } // 标识
        public uint totol { get; set; } // 总计的 Tick 数
        public uint up { get; set; } // 正常的 Tick 数
        public uint down { get; set; } // 故障的 Tick 数
    }

    public class buffer { // 缓存目前的状态数组
        public int id { get; set; }
        public string identification { get; set; } // 标识
        public string startTS { get; set; } // 触发时间
    }

}
