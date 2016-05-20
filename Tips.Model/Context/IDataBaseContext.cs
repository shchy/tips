using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;
using Tips.Model.Models.PermissionModels;

namespace Tips.Model.Context
{
    public interface IDataBaseContext
    {
        IUser AuthUser(IUser authUser);
        void AddUser(IUser user);
        void AddUserIcon(IUser user, byte[] iconImage);
        void AddProject(IProject project, IUser user);
        void AddTaskComment(ITaskComment comment, int taskId);
        void AddTaskRecord(ITaskRecord record, int taskId);
        void AddTaskToUser(IUser user, int taskId);
        void AddProjectMember(IUser user, int projectId);

        IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null);
        IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null);
        IEnumerable<ITaskWithRecord> GetTaskRecords(Func<ITaskWithRecord, bool> predicate = null);
        
        void DeleteProject(IProject project);
        void DeleteUser(IUser user);
        void DeleteTaskRecord(ITaskWithRecord taskWithRecord, int recordId);
        void DeleteProjectMember(IUser user, int projectId);

        IPermission GetDeleteTaskRecordPermission(Tuple<int, int> taskAndRecord);
        IPermission GetDeleteProjectPermission();
        IPermission GetDeleteUserPermission();
        IPermission GetAddProjectMemberPermission();
        IPermission GetDeleteProjectMemberPermission();
        IPermission GetAccessProjectPermission(int projectId);

        IEnumerable<IUser> GetUserOfProject(int projectId);
        IProject GetProjectFromTask(int taskId);
        IEnumerable<IProject> GetProjectBelongUser(IUser user);

        void UpdateTask(ITaskItem task);
    }
}
