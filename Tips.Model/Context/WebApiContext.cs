using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;
using Tips.Model.Models.PermissionModels;

namespace Tips.Model.Context
{
    public class WebApiContext : IDataBaseContext
    {
        private string baseUrl;
        private Func<IUser> getAuthUser;

        public WebApiContext(string baseUrl, Func<IUser> getAuthUser)
        {
            this.baseUrl = baseUrl;
            this.getAuthUser = getAuthUser;

        }

        public IUser AuthUser(IUser authUser)
        {
            return
                PostAsJson("/api/login/", () =>
                {
                    return authUser;
                },
                json =>
                {
                    return
                        JsonConvert.DeserializeObject<User>(json);
                });
        }


        public IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null)
        {
            return
                Get("api/projects/", json =>
                {
                    var query = JArray.Parse(json);
                    var list =
                        from q in query.Children()
                        select Project.FromJson(q.ToString());
                    return
                        list.Where(predicate).ToArray();
                    //JsonConvert.DeserializeObject<List<Project>>(json);
                });
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null)
        {
            return
                Get("api/users/", json =>
                {
                    return 
                        JsonConvert.DeserializeObject<List<User>>(json)
                        .Where(predicate).ToArray();
                });
        }


        public IEnumerable<ITaskWithRecord> GetTaskRecords(Func<ITaskWithRecord, bool> predicate = null)
        {
            return
                Get("api/tasks/", json =>
                {
                    //Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                    //serializer.Converters.Add(new DTOJsonConverter());
                    //Interfaces.IEntity entity = serializer.Deserialize(jsonReader);

                    return
                        JsonConvert.DeserializeObject<List<TaskWithRecord>>(json)
                        .Where(predicate)
                        .ToArray();
                });
        }

        public void AddUser(IUser user)
        {
            PostAsJson("api/users/", () =>
            {
                return user;
            });
        }


        public void AddUserIcon(IUser user, byte[] iconImage)
        {
            PostAsJson("api/users/withIcon/", () =>
            {
                var base64String = 
                    Convert.ToBase64String(
                        iconImage
                        , 0, iconImage.Length);

                return new AddUserWithIcon
                {
                    UserId = user.Id,
                    Base64BytesByImage = base64String,
                };
            });
        }

        public void AddProject(IProject project, IUser user)
        {
            PostAsJson("api/projects/", () =>
            {
                return project;
            });
        }

        public void AddTaskComment(ITaskComment comment, int taskId)
        {
            PostAsJson("api/task/comment/", () =>
            {
                //return new { Comment = comment, TaskId = taskId };
                return new { comment, taskId };
            });
        }

        public void AddTaskRecord(ITaskRecord record, int taskId)
        {
            PostAsJson("api/task/record/", () =>
            {
                return new { record, taskId };
            });
        }



        T Get<T>(string api, Func<string, T> getter)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(this.baseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    SetAuth(client);

                    var response = client.GetAsync(api);
                    response.Wait();
                    response.Result.EnsureSuccessStatusCode();
                    var body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    return getter(body.Result);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        
        T PostAsJson<T>(string api, Func<object> getPostData, Func<string, T> getter = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(this.baseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    SetAuth(client);

                    var model = getPostData();

                    var json =
                        JsonConvert.SerializeObject(
                            model,
                            Formatting.Indented,
                            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
                          );
                    var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = client.PostAsync(api, content);
                    response.Wait();
                    response.Result.EnsureSuccessStatusCode();

                    if (getter == null)
                    {
                        return default(T);
                    }

                    var body = response.Result.Content.ReadAsStringAsync();
                    body.Wait();
                    return getter(body.Result);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void SetAuth(HttpClient client)
        {
            var authUser = getAuthUser();
            if (authUser == null)
            {
                return;
            }
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", authUser.Id, authUser.Password));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        }

        void PostAsJson(string api, Func<object> getPostData )
        {
            PostAsJson<object>(api, getPostData, null);
        }

        public void AddTaskToUser(IUser user, int taskId)
        {
            throw new NotImplementedException();
        }

        public void DeleteProject(IProject project)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(IUser user)
        {
            throw new NotImplementedException();
        }

        public void DeleteTaskRecord(ITaskWithRecord taskWithRecord, int recordId)
        {
            throw new NotImplementedException();
        }

        public IPermission GetDeleteTaskRecordPermission(Tuple<int, int> taskAndRecord)
        {
            throw new NotImplementedException();
        }

        public IPermission GetDeleteProjectPermission()
        {
            throw new NotImplementedException();
        }

        public IPermission GetDeleteUserPermission()
        {
            throw new NotImplementedException();
        }

        public void UpdateTask(ITaskItem task)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IUser> GetUserOfProject(int projectId)
        {
            throw new NotImplementedException();
        }

        public IProject GetProjectFromTask(int taskId)
        {
            throw new NotImplementedException();
        }

        public void AddProjectMember(IUser user, int projectId)
        {
            throw new NotImplementedException();
        }

        public void DeleteProjectMember(IUser user, int projectId)
        {
            throw new NotImplementedException();
        }

        public IPermission GetAddProjectMemberPermission()
        {
            throw new NotImplementedException();
        }

        public IPermission GetDeleteProjectMemberPermission()
        {
            throw new NotImplementedException();
        }

        public IPermission GetAccessProjectPermission(int projectId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IProject> GetProjectBelongUser(IUser user)
        {
            throw new NotImplementedException();
        }
    }
}
