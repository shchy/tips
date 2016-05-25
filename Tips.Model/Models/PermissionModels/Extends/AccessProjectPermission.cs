using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.PermissionModels.Extends
{
    public class AccessProjectPermission : Permission, IPermission
    {
        /// <summary>
        /// プロジェクトへのアクセス権限初期化
        /// </summary>
        public AccessProjectPermission()
        {
            // 全体は不許可
            this.All = false;

            // 管理者は許可
            this.Administrator = true;

            // その他は該当なし
            this.Others = new Dictionary<string, bool>();
        }
    }
}
