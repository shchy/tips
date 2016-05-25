using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels
{
    public interface IPermission
    {
        bool Administrator { get; }
        bool All { get; }
        IDictionary<string, bool> Others { get; } // key: userId, value: permissions
    }

    public static class IPermissionExtends
    {
        public static bool IsPermittedUser(this IPermission @this, IUser user)
        {
            // 許可条件に一つでも適合した場合にtrue
            return
                @this.All
                || (user.Role == UserRole.Admin && @this.Administrator)
                || (@this.Others.ContainsKey(user.Id) && @this.Others[user.Id]);
        }
    }
}
