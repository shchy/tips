using Nancy;
using Nancy.Authentication.Forms;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Context;
using Tips.WebServer.Services;

namespace Tips.WebServer.Modules
{
    public class LoginModule : NancyModule
    {
        public LoginModule(
            IDataBaseContext context
            , IUserMapper mapper)
        {
            Get["/"] = prms =>
            {
                // ログイン情報がある場合ホーム画面にリダイレクト
                var query =
                    from user in this.Context.CurrentUser.ToMaybe()
                    where context.GetUser(x => user.UserName.Equals(x.Id)).Any()
                    select user;

                if (query.IsSomething)
                    return Response.AsRedirect("/home/");

                // ログイン情報がないor不正な場合はログイン画面へ
                return View["Views/Login"];
            };

            Post["/"] = prms =>
            {
                var id = (string)this.Request.Form["userId"];
                var pass = (string)this.Request.Form["password"];
                var redirect = (string)this.Request.Query["returnUrl"] ?? "/home/";

                var findId =
                    (from u in context.GetUser(_ => true)
                     where u.Id == id
                     where u.Password == pass
                     select (mapper as UserValidator).ToGuid(u)).FirstOrNothing();

                if (findId.IsNothing)
                {
                    return Response.AsRedirect("/");
                }

                return
                    this.LoginAndRedirect(findId.Return(), DateTime.Now.AddDays(7), redirect);
            };



            Get["/logout"] = prms =>
            {
                return this.LogoutAndRedirect("/");
            };

            // todo apiへ
            Get["/login/{id}/pass/{pass}"] = prms =>
            {
                var id = (string)prms.id;
                var pass = (string)prms.pass;

                var findId =
                    (from u in context.GetUser(_ => true)
                     where u.Id == id
                     where u.Password == pass
                     select (mapper as UserValidator).ToGuid(u)).FirstOrNothing();

                if (findId.IsNothing)
                {
                    return Response.AsRedirect("/");
                }

                return
                    this.Login(findId.Return(), DateTime.Now.AddDays(7));
            };
        }
    }
}
