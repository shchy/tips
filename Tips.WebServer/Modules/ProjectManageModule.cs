using Microsoft.Practices.Unity;
using Nancy;
using Nancy.Security;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Core.Services;
using Tips.Model.Context;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class ProjectManageModule : NancyModule
    {
        public ProjectManageModule(
            IDataBaseContext context
            , [Dependency("workdaySettingFolder")] string workdaySettingFolder)
            : base("/projectmanage/")
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new[] { UserRole.Admin.ToString() });

            Get["/{id}"] = prms =>
            {
                var id = (int)prms.id;
                return Response.AsRedirect(string.Format("/projectmanage/{0}/setupworkdays", id));
            };

            Get["/{id}/setupworkdays"] = prms =>
            {
                var id = (int)prms.id;

                var view =
                    from p in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    from workdayContext in MyClass.GetWorkdayContext(workdaySettingFolder, p.Id)
                    let workdays = workdayContext.Load()
                    let json = WorkdayModify.ToJsonText(workdays)
                    select View["Views/ProjectManageWorkdays", new { Project = p, Json = json }] as object;

                return view.Return(() => HttpStatusCode.BadRequest);
            };

            // todo api?
            Post["/{id}/setupworkdays"] = prms =>
            {
                var id = (int)prms.id;
                var query =
                    from p in context.GetProjects(x => x.Id == id).Select(p => MyClass.ToWithRecordsProject(context, p)).FirstOrNothing()
                    from workdayContext in MyClass.GetWorkdayContext(workdaySettingFolder, p.Id)
                    let json = this.Request.Body.ToStreamString()
                    let model = WorkdayModify.ToModel(json)
                    let result = Fn.New(()=> { workdayContext.Save(model); return 0; }).ToExceptional()
                    where result.IsRight
                    select Response.AsJson(new { }, HttpStatusCode.OK);
                return query.Return(() => HttpStatusCode.BadRequest);
            };

        }
    }
}
