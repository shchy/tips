using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels
{
    public class Permission : IPermission
    {
        public IUserPermissions Administrator { get; set; }
        public IUserPermissions All { get; set; }
        public IDictionary<string, IUserPermissions> Others { get; set; }
    }
}
