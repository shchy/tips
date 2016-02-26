using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels
{
    public class UserPermissions : IUserPermissions
    {
        public bool IsEnableDelete { get; set; }
    }
}
