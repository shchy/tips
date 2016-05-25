﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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
        DbSet<DbTaskComment> TaskComments { get; }
        DbSet<DbTaskRecord> TaskRecords { get; }
        DbSet<DbLinkTaskItemWithRecord> LinkTaskItemWithRecord { get; }
        DbSet<DbLinkTaskItemWithComment> LinkTaskItemWithComment { get; }
        DbSet<DbLinkProjectWithSprint> LinkProjectWithSprint { get; }
        DbSet<DbLinkSprintWithTask> LinkSprintWithTaskItem { get; }
        DbSet<DbLinkUserWithTaskItem> LinkUserWithTaskItem { get; }
        DbSet<DbLinkProjectWithUser> LinkProjectWithUser { get; }
        DbSet<SchemaInfo> SchemaInfo { get; set; }
        
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
            else
            {
                var sql = string.Empty;

                //sql += "create table DbLinkUserWithTaskItem(";
                //sql += " UserId TEXT";
                //sql += " ,TaskItemId INTEGER";
                //sql += " ,IsDeleted INTEGER";
                //sql += " ,PRIMARY KEY (TaskItemId)";
                //sql += " ,FOREIGN KEY(UserId)REFERENCES DbUser(Id)";
                //sql += " ,FOREIGN KEY(TaskItemId)REFERENCES DbTaskItem(Id)";
                //sql += ");";
                //this.Database.ExecuteSqlCommand(sql);
                //this.SaveChanges();

                //this.Database.ExecuteSqlCommand("CREATE TABLE DbTaskStatusMaster( Id INTEGER, Name TEXT, PRIMARY KEY (Id));");
                //this.Database.ExecuteSqlCommand("ALTER TABLE DbTaskItem RENAME TO DbTaskItemTemp;");
                //this.Database.ExecuteSqlCommand("CREATE TABLE DbTaskItem( Id INTEGER, Name TEXT, Value REAL, StatusCode INTEGER, PRIMARY KEY (Id), FOREIGN KEY(StatusCode)REFERENCES DbTaskStatusMaster(Id));");
                //this.Database.ExecuteSqlCommand("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(0, 'Backlog');");
                //this.Database.ExecuteSqlCommand("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(1, 'Ready');");
                //this.Database.ExecuteSqlCommand("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(2, 'In Progress');");
                //this.Database.ExecuteSqlCommand("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(3, 'Done');");
                //this.Database.ExecuteSqlCommand("INSERT INTO DbTaskItem SELECT NULL AS Id, DbTaskItemTemp.Name AS Name, DbTaskItemTemp.Value AS Value, 0 AS StatusCode FROM DbTaskItemTemp;");
                //this.Database.ExecuteSqlCommand("DROP TABLE DbTaskItemTemp;");

                //this.Database.ExecuteSqlCommand(sql);
                //this.SaveChanges();
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
            sql += " ,IconFile TEXT";
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
            sql += "create table DbTaskComment(";
            sql += " Id INTEGER";
            sql += " ,Day INTEGER";
            sql += " ,UserId TEXT";
            sql += " ,Text TEXT";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbTaskRecord(";
            sql += " Id INTEGER";
            sql += " ,Day INTEGER";
            sql += " ,UserId TEXT";
            sql += " ,Value REAL";
            sql += " ,WorkValue REAL";
            sql += " ,PRIMARY KEY (Id)";
            sql += ");";
            sql += "create table DbLinkProjectWithSprint(";
            sql += " ProjectId INTEGER";
            sql += " ,SprintId INTEGER";
            sql += " ,Sort INTEGER";
            sql += " ,IsDeleted INTEGER";
            sql += " ,PRIMARY KEY (SprintId)";
            sql += " ,FOREIGN KEY(ProjectId)REFERENCES DbProject(Id)";
            sql += " ,FOREIGN KEY(SprintId)REFERENCES DbSprint(Id)";
            sql += ");";
            sql += "create table DbLinkSprintWithTask(";
            sql += " SprintId INTEGER";
            sql += " ,TaskItemId INTEGER";
            sql += " ,Sort INTEGER";
            sql += " ,IsDeleted INTEGER";
            sql += " ,PRIMARY KEY (TaskItemId)";
            sql += " ,FOREIGN KEY(SprintId)REFERENCES DbSprint(Id)";
            sql += " ,FOREIGN KEY(TaskItemId)REFERENCES DbTaskItem(Id)";
            sql += ");";
            sql += "create table DbLinkTaskItemWithRecord(";
            sql += " TaskItemId INTEGER";
            sql += " ,TaskRecordId INTEGER";
            sql += " ,IsDeleted INTEGER";
            sql += " ,PRIMARY KEY (TaskRecordId)";
            sql += " ,FOREIGN KEY(TaskItemId)REFERENCES DbTaskItem(Id)";
            sql += " ,FOREIGN KEY(TaskRecordId)REFERENCES DbTaskRecord(Id)";
            sql += ");";
            sql += "create table DbLinkTaskItemWithComment(";
            sql += " TaskItemId INTEGER";
            sql += " ,TaskCommentId INTEGER";
            sql += " ,IsDeleted INTEGER";
            sql += " ,PRIMARY KEY (TaskCommentId)";
            sql += " ,FOREIGN KEY(TaskItemId)REFERENCES DbTaskItem(Id)";
            sql += " ,FOREIGN KEY(TaskCommentId)REFERENCES DbTaskComment(Id)";
            sql += ");";
            sql += "create table DbLinkUserWithTaskItem(";
            sql += " UserId TEXT";
            sql += " ,TaskItemId INTEGER";
            sql += " ,IsDeleted INTEGER";
            sql += " ,PRIMARY KEY (TaskItemId)";
            sql += " ,FOREIGN KEY(UserId)REFERENCES DbUser(Id)";
            sql += " ,FOREIGN KEY(TaskItemId)REFERENCES DbTaskItem(Id)";
            sql += ");";
            sql += "create table SchemaInfo(";
            sql += " Id INTEGER";
            sql += " ,Version INTEGER";
            sql += " ,PRIMARY KEY (Id)";
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
        public DbSet<DbTaskComment> TaskComments { get; set; }
        public DbSet<DbTaskRecord> TaskRecords { get; set; }
        public DbSet<DbLinkProjectWithSprint> LinkProjectWithSprint { get; set; }
        public DbSet<DbLinkSprintWithTask> LinkSprintWithTaskItem { get; set; }
        public DbSet<DbLinkTaskItemWithRecord> LinkTaskItemWithRecord { get; set; }
        public DbSet<DbLinkTaskItemWithComment> LinkTaskItemWithComment { get; set; }
        public DbSet<DbLinkUserWithTaskItem> LinkUserWithTaskItem { get; set; }
        public DbSet<DbLinkProjectWithUser> LinkProjectWithUser { get; set; }
        public DbSet<SchemaInfo> SchemaInfo { get; set; }

    }

    public class SqliteContextHelper
    {

        public SqliteContextHelper()
        {
            Migrations = new Dictionary<int, IList<string>>();

            MigrationVersion1();
            MigrationVersion2();
        }

        public Dictionary<int, IList<string>> Migrations { get; set; }

        private void MigrationVersion1()
        {
            var steps = new List<string>();

            steps.Add("CREATE TABLE DbTaskStatusMaster( Id INTEGER, Name TEXT, PRIMARY KEY (Id));");
            steps.Add("ALTER TABLE DbTaskItem RENAME TO DbTaskItemTemp;");
            steps.Add("CREATE TABLE DbTaskItem( Id INTEGER, Name TEXT, Value REAL, StatusCode INTEGER, PRIMARY KEY (Id), FOREIGN KEY(StatusCode)REFERENCES DbTaskStatusMaster(Id));");
            steps.Add("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(0, 'Backlog');");
            steps.Add("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(1, 'Ready');");
            steps.Add("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(2, 'In Progress');");
            steps.Add("INSERT INTO DbTaskStatusMaster (Id, Name) VALUES(3, 'Done');");
            steps.Add("INSERT INTO DbTaskItem SELECT NULL AS Id, DbTaskItemTemp.Name AS Name, DbTaskItemTemp.Value AS Value, 0 AS StatusCode FROM DbTaskItemTemp;");
            steps.Add("DROP TABLE DbTaskItemTemp;");

            // キーはVersionと対応
            Migrations.Add(1, steps);
        }
        
        private void MigrationVersion2()
        {
            var steps = new List<string>();

            steps.Add("CREATE TABLE DbLinkProjectWithUser( Id INTEGER, ProjectId INTEGER, UserId TEXT, PRIMARY KEY (Id), FOREIGN KEY(ProjectId)REFERENCES DbProject(Id), FOREIGN KEY(UserId)REFERENCES DbUser(Id));");

            // キーはVersionと対応
            Migrations.Add(2, steps);
        }
    }

    public class SqliteContextInitializer : IDataBaseContextInitializer
    {
        // tips DBのバージョン変更があるたびにインクリメント
        public static int RequiredDatabaseVersion = 2;
        private SqliteContext sqliteContext;

        public SqliteContextInitializer(SqliteContext sqliteContext)
        {
            this.sqliteContext = sqliteContext;
        }

        public void Initialize()
        {
            // SchemaInfoの有無チェック
            MakeSchemaInfo();

            // DBのマイグレーション
            var currentVersion = 0;
            if (sqliteContext.SchemaInfo.Count() > 0)
                currentVersion = sqliteContext.SchemaInfo.Max(x => x.Version);
            SqliteContextHelper mmSqliteHelper = new SqliteContextHelper();
            while (currentVersion < RequiredDatabaseVersion)
            {
                currentVersion++;
                foreach (var migration in mmSqliteHelper.Migrations[currentVersion])
                {
                    sqliteContext.Database.ExecuteSqlCommand(migration);
                }
                sqliteContext.SchemaInfo.Add(new SchemaInfo() { Version = currentVersion });
                sqliteContext.SaveChanges();
            }
        }

        /// <summary>
        /// SchemaInfoテーブルを作成する
        /// </summary>
        private void MakeSchemaInfo()
        {
            var cursor = sqliteContext.Database.SqlQuery(typeof(int), @"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'SchemaInfo'");
            var iterator = cursor.GetEnumerator();
            if (iterator.MoveNext() && ((int)iterator.Current <= 0))
            {
                var sql = string.Empty;
                sql += "create table SchemaInfo(";
                sql += " Id INTEGER";
                sql += " ,Version INTEGER";
                sql += " ,PRIMARY KEY (Id)";
                sql += ");";
                sqliteContext.Database.ExecuteSqlCommand(sql);
                sqliteContext.SaveChanges();
            }

            // iteratorのインスタンスが生き残っているとマイグレーション中のDROP TABLEでtable locked errorが発生するためここで必ずメモリを掃除する
            cursor = null;
            iterator = null;
            GC.Collect();
        }
    }

    //public partial class UserDetailsMigration : DbMigration
    //{

    //    public override void Up()
    //    {
    //    }

    //    public override void Down()
    //    {
    //    }
    //}
}
