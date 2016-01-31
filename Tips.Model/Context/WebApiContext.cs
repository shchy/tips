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
                        list.ToArray();
                        //JsonConvert.DeserializeObject<List<Project>>(json);
                });
        }

        public IEnumerable<IUser> GetUser(Func<IUser, bool> predicate = null)
        {
            return
                Get("api/users/", json =>
                {
                    return 
                        JsonConvert.DeserializeObject<List<User>>(json);
                });
        }

        public void AddUser(IUser user)
        {
            PostAsJson("api/users/", () =>
            {
                return user;
            });
        }

        public void AddProject(IProject project)
        {
            PostAsJson("api/projects/", () =>
            {
                return project;
            });
        }

        T Get<T>(string api, Func<string, T> getter)
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

        
        T PostAsJson<T>(string api, Func<object> getPostData, Func<string, T> getter = null)
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

    }
}
