using Newtonsoft.Json;
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

    //public interface IResource : IIdentity<int>, INameable
    //{
    //    /// <summary>
    //    /// V/H
    //    /// </summary>
    //    double Cost { get; }
    //}

    public interface IRange<T>
    {
        T Left { get; }
        T Right { get; }
    }

    public interface ITaskItem : IIdentity<int>, INameable
    {
        double Value { get; }
    }

    public interface ITaskComment : IIdentity<int>
    {
        DateTime Day { get; }
        string Text { get; }
        IUser Who { get; }
    }

    public interface ITaskRecord : IIdentity<int>
    {
        DateTime Day { get; }
        double Value { get; }
        double WorkValue { get; }
        IUser Who { get; }
    }

    public interface ITaskWithRecord : ITaskItem
    {
        IEnumerable<ITaskComment> Comments { get; }
        IEnumerable<ITaskRecord> Records { get; }
    }

    public interface IPlan : IIdentity<int>
    {
        DateTime Day { get; }
        double Value { get; }
        IUser Who { get; }
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

    [PropertyChanged.ImplementPropertyChanged]
    public class TaskWithRecord : TaskItem, ITaskWithRecord
    {
        [JsonConverter(typeof(ConcreteConverter<List<TaskComment>>))]
        public IEnumerable<ITaskComment> Comments { get; set; }

        [JsonConverter(typeof(ConcreteConverter<List<TaskRecord>>))]
        public IEnumerable<ITaskRecord> Records { get; set; }

        public static ITaskWithRecord Create(ITaskItem source
            , IEnumerable<ITaskRecord> records
            , IEnumerable<ITaskComment> comments)
        {
            return new TaskWithRecord
            {
                Id = source.Id,
                Name = source.Name,
                Value = source.Value,
                Records = records,
                Comments = comments,
            };
        }
    }


    public class TaskRecord : ITaskRecord
    {
        public DateTime Day { get; set; }

        public int Id { get; set; }

        public double Value { get; set; }

        [JsonConverter(typeof(ConcreteConverter<User>))]
        public IUser Who { get; set; }

        public double WorkValue { get; set; }
    }

    [PropertyChanged.ImplementPropertyChanged]
    public class TaskComment : ITaskComment
    {
        public DateTime Day { get; set; }

        public int Id { get; set; }

        public string Text { get; set; }

        [JsonConverter(typeof(ConcreteConverter<User>))]
        public IUser Who { get; set; }
    }


    // todo toFile
    public class ConcreteConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader,
         Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
