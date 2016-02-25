using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels
{
    public interface IPermission
    {
        IUserPermissions Administrator { get; }
        IUserPermissions All { get; }
        IDictionary<string, IUserPermissions> Others { get; } // key: userId, value: permissions
    }

    public static class IPermissionExtends
    {
        public static bool IsPermittedDelete(this IPermission @this, IUser user)
        {
            // 許可条件に一つでも適合した場合にtrue
            return
                @this.All.IsEnableDelete
                || (user.Role == UserRole.Admin && @this.Administrator.IsEnableDelete)
                || (@this.Others.ContainsKey(user.Id) && @this.Others[user.Id].IsEnableDelete);
        }
    }
}
