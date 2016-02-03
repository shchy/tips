using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;
using Tips.Model.Models.DbModels;

namespace Tips.Model.Context
{
    public class DataBaseContext<TSource>
        : IDataBaseContext
        where TSource : DbContext, IDataSource

    {
        private IDataBaseSource<TSource> dbContext;
        // todo パスはコンストラクタでもらう
        string saveLocalFolder = "./content/img/userIcons/";


        public DataBaseContext(IDataBaseSource<TSource> dbContext)
        {
            this.dbContext = dbContext;
        }

        public void AddProject(IProject project)
        {
            this.dbContext.Update(db =>
            {
                // update project
                var dbModel = project.ToDbModel();
                db.Projects.AddOrUpdate(dbModel);
                db.SaveChanges();

                // todo 他プロジェクトのタスクIDを指定されたら困る
                // todo ViewModel側でIDのマッピングする様にして直接IDをText入力させない様にするのが正しそう

                // todo 古いリンクの削除と無所属になったデータの削除？
                // 所属しなくなったTaskLinkの削除
                var notLinkSprintTask =
                    from tLink in db.LinkSprintWithTaskItem.ToArray()
                    from sLink in db.LinkProjectWithSprint.ToArray()
                    where sLink.ProjectId == dbModel.Id
                    where sLink.SprintId == tLink.SprintId
                    where project.Sprints.SelectMany(x => x.Tasks).Select(x=>x.Id).Contains(tLink.TaskItemId) == false
                    select tLink;
                notLinkSprintTask.ForEach(x => db.LinkSprintWithTaskItem.Remove(x));

                // 所属しなくなったSprintLinkの削除
                var notLinkProjectSprint =
                    from sLink in db.LinkProjectWithSprint.ToArray()
                    where sLink.ProjectId == dbModel.Id
                    where project.Sprints.Select(x => x.Id).Contains(sLink.SprintId) == false
                    select sLink;
                notLinkProjectSprint.ForEach(x=>db.LinkProjectWithSprint.Remove(x));


                foreach (var sprint in project.Sprints.Select((v,i)=>new { v,i }))
                {
                    var updatedSprint = sprint.v.ToDbModel();
                    db.Sprints.AddOrUpdate(updatedSprint);
                    db.SaveChanges();

                    var linkPtoS = dbModel.ToDbLink(updatedSprint, sprint.i);
                    db.LinkProjectWithSprint.AddOrUpdate(linkPtoS);

                    foreach (var taskItem in sprint.v.Tasks.Select((v, i) => new { v, i }))
                    {
                        var updatedTaskItem = taskItem.v.ToDbModel();
                        db.TaskItems.AddOrUpdate(updatedTaskItem);
                        db.SaveChanges();

                        var linkStoT = updatedSprint.ToDbLink(updatedTaskItem, taskItem.i);
                        db.LinkSprintWithTaskItem.AddOrUpdate(linkStoT);
                    }
                }
            });
        }

        public void AddTaskComment(ITaskComment comment, int taskId)
        {
            this.dbContext.Update(db =>
            {
                var model = comment.ToDbModel();
                db.TaskComments.Add(model);
                db.SaveChanges();
                var link = model.ToDbLink(taskId);
                db.LinkTaskItemWithComment.Add(link);
                db.SaveChanges();
            });
        }

        public void AddTaskRecord(ITaskRecord record, int taskId)
        {
            this.dbContext.Update(db =>
            {
                var model = record.ToDbModel();
                db.TaskRecords.Add(record.ToDbModel());
                db.SaveChanges();
                var link = model.ToDbLink(taskId);
                db.LinkTaskItemWithRecord.Add(link);
                db.SaveChanges();
            });
        }

        public void AddUser(IUser user)
        {
            this.dbContext.Update(db =>
            {
                var model = user.ToDbModel();
                db.Users.Add(model);
            });
        }

        public void AddUserIcon(IUser user, byte[] iconImage)
        {
            // todo imgフォルダへの保存は別クラス化
            var converter = new ImageConverter();
            var img = converter.ConvertFrom(iconImage) as Image;
            var savePath = Path.Combine(saveLocalFolder, user.Id + ".png");
            if (Directory.Exists(saveLocalFolder) == false)
            {
                Directory.CreateDirectory(saveLocalFolder);
            }
            img.Save(savePath);
        }

        public IUser AuthUser(IUser authUser)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null)
        {
            return this.dbContext.Get(db =>
            {
                var buildedSprints =
                    (from s in db.Sprints.ToArray()
                     let tasks = 
                         from link in db.LinkSprintWithTaskItem.ToArray()
                         where link.SprintId == s.Id
                         from t in db.TaskItems.ToArray()
                         where link.TaskItemId == t.Id
                         orderby link.Sort ascending
                         select t
                     select s.BuildModel(tasks.ToArray())).ToArray();

                var projects =
                    from p in db.Projects.ToArray()
                    let sprints =
                        from link in db.LinkProjectWithSprint.ToArray()
                        where link.ProjectId == p.Id
                        from s in buildedSprints.ToArray()
                        where link.SprintId == s.Id
                        orderby link.Sort ascending
                        select s
                    select p.ToModel(sprints.ToArray());

                return projects.Where(predicate).ToArray();
            });
        }

        public IEnumerable<ITaskWithRecord> GetTaskRecords(Func<ITaskWithRecord, bool> predicate = null)
        {
            return this.dbContext.Get(db =>
            {
                var users = db.Users.ToArray();
                var query =
                    from t in db.TaskItems.ToArray()
                    let rx =
                        from link in db.LinkTaskItemWithRecord.ToArray()
                        where link.TaskItemId == t.Id
                        from r in db.TaskRecords.ToArray()
                        where r.Id == link.TaskRecordId
                        select r
                    let cx =
                        from link in db.LinkTaskItemWithComment.ToArray()
                        where link.TaskItemId == t.Id
                        from c in db.TaskComments.ToArray()
                        where c.Id == link.TaskCommentId
                        select c
                    select TaskWithRecord.Create(
                        t.ToModel()
                        , rx.Select(r => r.ToModel(users.FirstOrDefault(u => u.Id == r.UserId))).ToArray()
                        , cx.Select(c => c.ToModel(users.FirstOrDefault(u => u.Id == c.UserId))).ToArray());

                return query.Where(predicate).ToArray();
            });
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null)
        {
            return this.dbContext.Get(db =>
            {
                return 
                    db.Users.Where(predicate).ToArray();
            });
        }
    }
}
