using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;
using Tips.Model.Models.DbModels;

namespace Tips.Model.Context
{
    public interface IDataSource
    {
        DbSet<DbUser> Users { get; }
        DbSet<DbProject> Projects { get; }
        DbSet<DbSprint> Sprints { get; }
        DbSet<DbTaskItem> TaskItems { get; }
        DbSet<DbLinkProjectWithSprint> LinkProjectWithSprint { get; }
        DbSet<DbLinkSprintWithTask> LinkSprintWithTaskItem { get; }
    }

    public class SqliteContext : DbContext, IDataSource
    {
        public SqliteContext(string dbPath)
            : base(new SQLiteConnection { ConnectionString = string.Format("Data Source={0}", dbPath) }, true)
        {
            // memo this.Database.Existsだとファイルが勝手に生成されるため、スキーマが作成済かどうか判断できないのでこうする
            var fileInfo = new FileInfo(dbPath);
            if (fileInfo.Exists == false || fileInfo.Length == 0)
            {
                this.Database.ExecuteSqlCommand(MakeSchema());
            }
        }

        string MakeSchema()
        {
            // SqliteはDDL発行を対応してないらしい
            var sql = string.Empty;
            sql += "create table DbUser(";
            sql += " Id TEXT not null";
            sql += " ,Name TEXT not null";
            sql += " ,Password TEXT not null";
            sql += " ,Role INTEGER";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbProject(";
            sql += " Id INTEGER";
            sql += " ,Name TEXT";
            sql += " ,Describe TEXT";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbSprint(";
            sql += " Id INTEGER";
            sql += " ,Name TEXT";
            sql += " ,Left INTEGER";
            sql += " ,Right INTEGER";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbTaskItem(";
            sql += " Id INTEGER";
            sql += " ,Name TEXT";
            sql += " ,Value REAL";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbLinkProjectWithSprint(";
            sql += " ProjectId INTEGER";
            sql += " ,SprintId INTEGER";
            sql += " ,Sort INTEGER";
            sql += " ,PRIMARY KEY (SprintId)";
            sql += ");";
            sql += "create table DbLinkSprintWithTask(";
            sql += " SprintId INTEGER";
            sql += " ,TaskItemId INTEGER";
            sql += " ,Sort INTEGER";
            sql += " ,PRIMARY KEY (TaskItemId)";
            sql += ");";
            return sql;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 名前から主キーとかを推論させない
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<DbUser>().Property(x => x.Id).IsRequired();
            modelBuilder.Entity<DbUser>().Property(x => x.Name).IsRequired();
            modelBuilder.Entity<DbUser>().Property(x => x.Password).IsRequired();

            //// 主キー設定
            //modelBuilder.Entity<OdakyuTrainRawInput>()
            //    .HasKey(o => new { o.Id, o.Station });

            //// 主キー設定
            //modelBuilder.Entity<Parameters>()
            //    .HasKey(o => new { o.Key });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<DbUser> Users { get; set; }
        public DbSet<DbProject> Projects { get; set; }
        public DbSet<DbSprint> Sprints { get; set; }
        public DbSet<DbTaskItem> TaskItems { get; set; }
        public DbSet<DbLinkProjectWithSprint> LinkProjectWithSprint { get; set; }
        public DbSet<DbLinkSprintWithTask> LinkSprintWithTaskItem { get; set; }

        

        
    }
}
