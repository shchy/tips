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
        void AddUser(IUser user);
        IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null);
        void AddProject(IProject project);
        IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null);
    }
}
