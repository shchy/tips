using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Model.Context
{
    public interface IDataBaseContext
    {
        IUser AuthUser(IUser authUser);
        void AddUser(IUser user);
        void AddUserIcon(IUser user, byte[] iconImage);
        void AddProject(IProject project);
        void AddTaskComment(ITaskComment comment, int taskId);
        void AddTaskRecord(ITaskRecord record, int taskId);
        void AddTaskToUser(IUser user, int taskId);

        IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null);
        IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null);
        IEnumerable<ITaskWithRecord> GetTaskRecords(Func<ITaskWithRecord, bool> predicate = null);
        
        void DeleteProject(IProject project);
        void DeleteUser(IUser user);
    }
}
