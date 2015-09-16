using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models
{
    public interface IIdentity<T>
    {
        T Id { get; }
    }

    public interface IUser : IIdentity<string>
    {
        string Name { get; }
        string Password { get; }
        UserRole Role { get; }
    }

    public enum UserRole
    {
        Admin = 0xFF,
        Normal = 0x00,
    }

    public class User : IUser
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public UserRole Role { get; set; }
    }
}
