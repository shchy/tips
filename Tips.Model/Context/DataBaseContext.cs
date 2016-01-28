using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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


                foreach (var sprint in project.Sprints)
                {
                    var updatedSprint = sprint.ToDbModel();
                     db.Sprints.AddOrUpdate(updatedSprint);
                    db.SaveChanges();

                    var linkPtoS = dbModel.ToDbLink(updatedSprint);
                    db.LinkProjectWithSprint.AddOrUpdate(linkPtoS);

                    foreach (var taskItem in sprint.Tasks)
                    {
                        var updatedTaskItem = taskItem.ToDbModel();
                        db.TaskItems.AddOrUpdate(updatedTaskItem);
                        db.SaveChanges();

                        var linkStoT = updatedSprint.ToDbLink(updatedTaskItem);
                        db.LinkSprintWithTaskItem.AddOrUpdate(linkStoT);
                    }
                }
                // todo 古いリンクの削除と無所属になったデータの削除？
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
                         select t
                     select s.BuildModel(tasks.ToArray())).ToArray();

                var projects =
                    from p in db.Projects.ToArray()
                    let sprints =
                        from link in db.LinkProjectWithSprint.ToArray()
                        where link.ProjectId == p.Id
                        from s in buildedSprints.ToArray()
                        where link.SprintId == s.Id
                        select s
                    select p.ToModel(sprints.ToArray());

                return projects.ToArray();
            });
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null)
        {
            return this.dbContext.Get(db =>
            {
                return 
                    db.Users.ToArray();
            });
        }
    }
}
