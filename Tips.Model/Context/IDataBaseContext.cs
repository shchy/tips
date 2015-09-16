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
        IEnumerable<IUser> GetUser();
        IEnumerable<IUser> GetUser(Func<IUser,bool> predicate);
        void AddUser(IUser user);
    }

    public class InMemoryDataBaseContext : IDataBaseContext
    {
        private IDictionary<string, IUser> users;

        public InMemoryDataBaseContext()
        {
            this.users = new Dictionary<string,IUser>();
            var defaultUser = new User { Id = "admin", Password = "admin", Role = UserRole.Admin, Name = "administrator" };
            this.users.Add(new KeyValuePair<string, IUser>(defaultUser.Id, defaultUser));
        }

        public void AddUser(IUser user)
        {
            if (this.users.ContainsKey(user.Id))
            {
                throw new Exception("mou iruyo");
            }
            users.Add(user.Id, user);
        }

        public IEnumerable<IUser> GetUser()
        {
            return
                GetUser(_ => true);
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate)
        {
            return
                this.users.Values.Where(predicate).ToArray();
        }
    }

}
