using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tips.Model.Models;

namespace Tips.Model.Context
{
    public class InMemoryData
    {
        public InMemoryData()
        {
            this.Users = new List<User>();
            this.Projects = new List<Project>();
        }

        public List<User> Users;
        public List<Project> Projects;

    }
    public class InMemoryDataBaseContext : IDataBaseContext
    {
        protected InMemoryData inMemoryData;

        public InMemoryDataBaseContext()
        {
            this.inMemoryData = new InMemoryData();
            var defaultUser = new User { Id = "admin", Password = "admin", Role = UserRole.Admin, Name = "administrator" };
            this.inMemoryData.Users.Add(defaultUser);
        }

        public void AddUser(IUser user)
        {
            if (this.inMemoryData.Users.Any(x=>x.Id == user.Id))
            {
                throw new Exception("mou iruyo");
            }
            inMemoryData.Users.Add(user as User);
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null)
        {
            return
                this.inMemoryData.Users.Where(predicate ?? (_ => true)).ToArray();
        }

        public void AddProject(IProject project)
        {
            if (project.Id == 0)
            {
                (project as Project).Id =
                    inMemoryData.Projects.ToGuards()
                    .When(px => px.Any(), px => px.Max(x => x.Id) + 1)
                    .Return(1);
            }
            if (this.inMemoryData.Projects.Any(x=>x.Id == project.Id))
            {
                throw new Exception("mou iruyo");
            }
            inMemoryData.Projects.Add(project as Project);
        }

        public IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null)
        {
            return
                this.inMemoryData.Projects.Where(predicate ?? (_ => true)).ToArray();
        }
    }
}
