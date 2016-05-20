using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels
{
    public class Permission : IPermission
    {
        public bool Administrator { get; set; }
        public bool All { get; set; }
        public IDictionary<string, bool> Others { get; set; }
    }
}
