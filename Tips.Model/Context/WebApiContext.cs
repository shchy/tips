using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private IUser authUser;

        public WebApiContext(string baseUrl, IUser authUser)
        {
            this.baseUrl = baseUrl;
            this.authUser = authUser;
        }

        public void AddProject(IProject project)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IProject> GetProjects(Func<IProject, bool> predicate = null)
        {
            throw new NotImplementedException();
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
;        }

        T Get<T>(string api, Func<string, T> getter)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", this.authUser.Id, this.authUser.Password));
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


                var response = client.GetAsync(api);
                response.Wait();
                response.Result.EnsureSuccessStatusCode();
                var body = response.Result.Content.ReadAsStringAsync();
                body.Wait();
                return getter(body.Result);
            }
        }

        void PostAsJson(string api, Func<object> getPostData)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", this.authUser.Id, this.authUser.Password));
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var model = getPostData();

                var response = client.PostAsJsonAsync(api, model);
                response.Wait();
                response.Result.EnsureSuccessStatusCode();
            }
        }

        private IUser ToUser(JToken u)
        {
            var user = new User
            {
                Id = u["Id"].Value<string>(),
                Name = u["Name"].Value<string>(),
                Role = u["Role"].Value<UserRole>(),
            };
            return user;
        }

    }
}
