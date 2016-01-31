using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models
{
    public interface INameable
    {
        string Name { get; }
    }

    public interface IResource : IIdentity<int>, INameable
    {
        /// <summary>
        /// V/H
        /// </summary>
        double Cost { get; }
    }

    public interface IRange<T>
    {
        T Left { get; }
        T Right { get; }
    }

    public interface ITaskItem : IIdentity<int>, INameable
    {
        double Value { get; }
    }

    public interface ITaskRecord : IIdentity<int>
    {
        int TaskId { get; }
        DateTime Day { get; }
        double Value { get; }
        double WorkValue { get; }
        IResource Who { get; }
    }

    public interface IPlan : IIdentity<int>
    {
        DateTime Day { get; }
        double Value { get; }
        IResource Who { get; }
    }

    public interface ISprint : IIdentity<int>, INameable, IRange<DateTime?>
    {
        IEnumerable<ITaskItem> Tasks { get; }
    }

    public interface IProject : IIdentity<int>, INameable
    {
        IEnumerable<ISprint> Sprints { get; set; }
        string Describe { get; }
    }

    [PropertyChanged.ImplementPropertyChanged]
    public partial class Project : IProject
    {
        public string Describe { get; set; }

        public int Id { get; set; }

        public IEnumerable<ISprint> Sprints { get; set; }

        public string Name { get; set; }

        public Project()
        {
            this.Sprints = Enumerable.Empty<ISprint>();
        }

        public static IProject FromJson(string json)
        {
            var model = JObject.Parse(json);

            var project =
                from m in model.ToMaybe()
                let sprints =
                    from n in m["sprints"].Children()
                    let tasks =
                        from t in n["tasks"].Children()
                        select new TaskItem
                        {
                            Id = t["id"].Value<int>(),
                            Name = t["name"].Value<string>(),
                            Value = t["value"].Value<double>(),
                        }
                    let s = new Sprint
                    {
                        Id = n["id"].Value<int>(),
                        Name = n["name"].Value<string>(),
                        Left = n["left"].Value<DateTime?>(),
                        Right = n["right"].Value<DateTime?>(),
                        Tasks = tasks.ToArray(),
                    }
                    select s
                let p = new Project

                {
                    Id = m["id"].Value<int>(),
                    Name = m["name"].Value<string>(),
                    Describe = m["describe"].Value<string>(),
                    Sprints = sprints.ToArray(),
                }
                select p;
            return project.Return();
        }
    }

    public class Sprint : ISprint
    {
        public Sprint()
        {
            Tasks = Enumerable.Empty<ITaskItem>();
        }

        public void AddTask(ITaskItem task)
        {
            this.Tasks = this.Tasks.Concat(task).ToArray(); 
        }

        public int Id { get; set; }

        public DateTime? Left { get; set; }

        public string Name { get; set; }

        public DateTime? Right { get; set; }

        public IEnumerable<ITaskItem> Tasks { get; set; }
    }

    public class TaskItem : ITaskItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double Value { get; set; }
    }

}
