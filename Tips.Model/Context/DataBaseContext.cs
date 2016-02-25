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

                // todo ↑が解消されれば問題なさそうだけど、EFはIdentityに指定されたIDに値を指定しても勝手に降りなおされてしまう。
                // todo 知らないIDが指定されてたら事前にnull or zero にしておくのがよさそう。
                // todo つまり、Linkだけ削除してしまうと以降の更新でずれる。

                // todo 古いリンクの削除と無所属になったデータの削除？
                // 所属しなくなったTaskLinkの削除
                var notLinkSprintTask =
                    from tLink in db.LinkSprintWithTaskItem.ToArray()
                    from sLink in db.LinkProjectWithSprint.ToArray()
                    where sLink.ProjectId == dbModel.Id
                    where sLink.SprintId == tLink.SprintId
                    where project.Sprints.SelectMany(x => x.Tasks).Select(x=>x.Id).Contains(tLink.TaskItemId) == false
                    select tLink;
                notLinkSprintTask.ForEach(x =>
                {
                    x.IsDeleted = 1;
                    db.LinkSprintWithTaskItem.AddOrUpdate(x);
                });
                db.SaveChanges();

                // 所属しなくなったSprintLinkの削除
                var notLinkProjectSprint =
                    from sLink in db.LinkProjectWithSprint.ToArray()
                    where sLink.ProjectId == dbModel.Id
                    where project.Sprints.Select(x => x.Id).Contains(sLink.SprintId) == false
                    select sLink;
                notLinkProjectSprint.ForEach(x=>
                {
                    x.IsDeleted = 1;
                    db.LinkProjectWithSprint.AddOrUpdate(x);
                });
                db.SaveChanges();


                foreach (var sprint in project.Sprints.Select((v,i)=>new { v,i }))
                {
                    var updatedSprint = sprint.v.ToDbModel();
                    db.Sprints.AddOrUpdate(updatedSprint);
                    db.SaveChanges();

                    var linkPtoS = dbModel.ToDbLink(updatedSprint, sprint.i);
                    db.LinkProjectWithSprint.AddOrUpdate(linkPtoS);
                    db.SaveChanges();

                    foreach (var taskItem in sprint.v.Tasks.Select((v, i) => new { v, i }))
                    {
                        var updatedTaskItem = taskItem.v.ToDbModel();
                        db.TaskItems.AddOrUpdate(updatedTaskItem);
                        db.SaveChanges();

                        var linkStoT = updatedSprint.ToDbLink(updatedTaskItem, taskItem.i);
                        db.LinkSprintWithTaskItem.AddOrUpdate(linkStoT);
                        db.SaveChanges();
                    }
                }
                db.SaveChanges();
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
                db.TaskRecords.Add(model);
                db.SaveChanges();
                var link = model.ToDbLink(taskId);
                db.LinkTaskItemWithRecord.Add(link);
                db.SaveChanges();
            });
        }

        public void AddTaskToUser(IUser user, int taskId)
        {
            this.dbContext.Update(db =>
            {
                var link = user.ToDbLink(taskId);
                db.LinkUserWithTaskItem.AddOrUpdate(link);
                db.SaveChanges();
            });
        }

        public void AddUser(IUser user)
        {
            this.dbContext.Update(db =>
            {
                var model = user.ToDbModel();
                db.Users.AddOrUpdate(model);
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
                         where link.IsDeleted == 0
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
                        where link.IsDeleted == 0
                        where link.ProjectId == p.Id
                        from s in buildedSprints.ToArray()
                        where link.SprintId == s.Id
                        orderby link.Sort ascending
                        select s
                    select p.ToModel(sprints.ToArray());

                return projects.Where(predicate).ToArray();
            });
        }

        public void DeleteProject(IProject project)
        {
            // todo プロジェクトに紐づくすべてのデータをここで削除する必要がある

            // Sprintsを削除
            project.Sprints.ForEach(DeleteSprint);

            this.dbContext.Delete(db =>
            {
                // プロジェクトを削除
                var model = project.ToDbModel();
                db.Projects.Attach(model);
                db.Projects.Remove(model);
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
                        where link.IsDeleted == 0
                        where link.TaskItemId == t.Id
                        from r in db.TaskRecords.ToArray()
                        where r.Id == link.TaskRecordId
                        select r
                    let cx =
                        from link in db.LinkTaskItemWithComment.ToArray()
                        where link.IsDeleted == 0
                        where link.TaskItemId == t.Id
                        from c in db.TaskComments.ToArray()
                        where c.Id == link.TaskCommentId
                        select c
                    let ax =
                        from link in db.LinkUserWithTaskItem.ToArray()
                        where link.IsDeleted == 0
                        where link.TaskItemId == t.Id
                        from u in users
                        where u.Id == link.UserId
                        select u
                    select TaskWithRecord.Create(
                        t.ToModel()
                        , rx.Select(r => r.ToModel(users.FirstOrDefault(u => u.Id == r.UserId))).ToArray()
                        , cx.Select(c => c.ToModel(users.FirstOrDefault(u => u.Id == c.UserId))).ToArray()
                        , ax.FirstOrDefault());

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
        
        private void DeleteSprint(ISprint sprint)
        {
            // todo sprintに紐づくすべてのデータを削除する必要がある

            // プロジェクトとsprintの関係モデルを削除
            var linkProjectWithSprint =
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkProjectWithSprint
                        .Where(link => link.SprintId == sprint.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkProjectWithSprint.ForEach(link =>
                {
                    db.LinkProjectWithSprint.Attach(link);
                });
                db.LinkProjectWithSprint.RemoveRange(linkProjectWithSprint);
            });

            // sprintに紐づくtaskを削除
            sprint.Tasks.ForEach(DeleteTask);

            this.dbContext.Delete(db =>
            {
                var model = sprint.ToDbModel();
                db.Sprints.Attach(model);
                db.Sprints.Remove(model);
            });
        }

        private void DeleteTask(ITaskItem task)
        {
            // todo taskに紐づくすべてのデータを削除する必要がある
            
            // taskのコメントを削除
            var taskComments =
                this.dbContext.Get(db =>
                {
                    var commentIds =
                        db.LinkTaskItemWithComment
                        .Where(link => link.TaskItemId == task.Id)
                        .Select(link => link.TaskCommentId);
                    return
                        db.TaskComments
                        .Where(comment => commentIds.Contains(comment.Id)).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                taskComments.ForEach(comment =>
                {
                    db.TaskComments.Attach(comment);
                });
                db.TaskComments.RemoveRange(taskComments);
            });

            // taskrecordを削除
            var taskRecords =
                this.dbContext.Get(db =>
                {
                    var recordsIds =
                        db.LinkTaskItemWithRecord
                        .Where(link => link.TaskItemId == task.Id)
                        .Select(link => link.TaskRecordId);
                    return
                        db.TaskRecords
                        .Where(record => recordsIds.Contains(record.Id)).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                taskRecords.ForEach(record =>
                {
                    db.TaskRecords.Attach(record);
                });
                db.TaskRecords.RemoveRange(taskRecords);
            });

            // sprintとtaskの関係モデルを削除
            var linkSprintWithTaskItem = 
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkSprintWithTaskItem
                        .Where(link => link.TaskItemId == task.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkSprintWithTaskItem.ForEach(link =>
                {
                    db.LinkSprintWithTaskItem.Attach(link);
                });
                db.LinkSprintWithTaskItem.RemoveRange(linkSprintWithTaskItem);
            });

            // taskとコメントの関係モデルを削除
            var linkTaskItemWithComment =
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkTaskItemWithComment
                        .Where(link => link.TaskItemId == task.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkTaskItemWithComment.ForEach(link =>
                {
                    db.LinkTaskItemWithComment.Attach(link);
                });
                db.LinkTaskItemWithComment.RemoveRange(linkTaskItemWithComment);
            });

            // taskとtaskrecordの関係モデルを削除
            var linkTaskItemWithRecord =
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkTaskItemWithRecord
                        .Where(link => link.TaskItemId == task.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkTaskItemWithRecord.ForEach(link =>
                {
                    db.LinkTaskItemWithRecord.Attach(link);
                });
                db.LinkTaskItemWithRecord.RemoveRange(linkTaskItemWithRecord);
            });

            // ユーザとtaskの関係モデルを削除
            var linkUserWithTaskItem =
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkUserWithTaskItem
                        .Where(link => link.TaskItemId == task.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkUserWithTaskItem.ForEach(link =>
                {
                    db.LinkUserWithTaskItem.Attach(link);
                });
                db.LinkUserWithTaskItem.RemoveRange(linkUserWithTaskItem);
            });

            this.dbContext.Delete(db =>
            {
                // taskを削除
                var model = task.ToDbModel();
                db.TaskItems.Attach(model);
                db.TaskItems.Remove(model);
            });
        }
        
        public void DeleteUser(IUser user)
        {
            // todo Userに紐づくすべてのデータをここで削除する必要がある
            
            // ユーザとtaskの関係モデルを削除
            var linkUserWithTaskItem =
                this.dbContext.Get(db =>
                {
                    return
                        db.LinkUserWithTaskItem
                        .Where(link => link.UserId == user.Id).ToArray();
                });
            this.dbContext.Delete(db =>
            {
                linkUserWithTaskItem.ForEach(link =>
                {
                    db.LinkUserWithTaskItem.Attach(link);
                });
                db.LinkUserWithTaskItem.RemoveRange(linkUserWithTaskItem);
            });

            this.dbContext.Delete(db =>
            {
                // Userを削除
                var model = user.ToDbModel();
                db.Users.Attach(model);
                db.Users.Remove(model);
            });
        }

        public void DeleteTaskRecord(ITaskWithRecord taskWithRecord, int recordId)
        {
            // taskとtaskrecordの関係モデルを削除
            this.dbContext.Delete(db =>
            {
                var links = db.LinkTaskItemWithRecord
                            .Where(x => x.TaskItemId == taskWithRecord.Id
                                        && x.TaskRecordId == recordId);
                links.ForEach(x => db.LinkTaskItemWithRecord.Attach(x));
                db.LinkTaskItemWithRecord.RemoveRange(links);
            });

            // taskrecordを削除
            this.dbContext.Delete(db =>
            {
                var record = taskWithRecord.Records.Where(x => x.Id == recordId).FirstOrNothing();
                record.On(x =>
                {
                    var model = x.ToDbModel();
                    db.TaskRecords.Attach(model);
                    db.TaskRecords.Remove(model);
                });
            });
        }
    }
}
