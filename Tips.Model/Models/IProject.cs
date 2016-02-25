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
        double? Value { get; }
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
        IUser Assign { get; }
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
                            Value = t["value"].Value<double?>(),
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

        // todo razorにするまでViewで計算できないからプロパティ追加
        public double TotalValue { get { return Tasks.Where(x => x.Value.HasValue).Sum(x => x.Value.Value); } }
        public double Ev { get { return Tasks.OfType<ITaskWithRecord>().SelectMany(x => x.Records).Sum(x => x.Value); } }
        public double Remaining { get { return this.TotalValue - this.Ev; } }

    }



    public class TaskItem : ITaskItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double? Value { get; set; }
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
            , IEnumerable<ITaskComment> comments
            , IUser assign)
        {
            return new TaskWithRecord
            {
                Id = source.Id,
                Name = source.Name,
                Value = source.Value,
                Records = records,
                Comments = comments,
                Assign = assign,
            };
        }

        public IUser Assign { get; set; }
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

    public class AddUserWithIcon
    {
        public string UserId { get; set; }
        public string Base64BytesByImage { get; set; }
    }



    public interface IGraphPoint
    {
        DateTime Day { get; }
        double Value { get; }
    }

    public interface IGraphModel
    {
        IEnumerable<IGraphPoint> Pv { get; }
        IEnumerable<IGraphPoint> Ev { get; }
        IEnumerable<IGraphPoint> Ac { get; }
    }

    public class GraphPoint : IGraphPoint
    {
        public DateTime Day { get; set; }

        public double Value { get; set; }
    }

    public class GraphModel : IGraphModel
    {
        public IEnumerable<IGraphPoint> Ac { get; set; }

        public IEnumerable<IGraphPoint> Ev { get; set; }

        public IEnumerable<IGraphPoint> Pv { get; set; }
        public GraphModel()
        {
            this.Ac = Enumerable.Empty<IGraphPoint>();
            this.Ev = Enumerable.Empty<IGraphPoint>();
            this.Pv = Enumerable.Empty<IGraphPoint>();
        }
    }

    public static class ModelExtensions
    {
        public static bool IsCompleted(this ISprint @this)
        {
            var tasks = @this.Tasks.ToArray();
            var taskWiths = @this.Tasks.OfType<ITaskWithRecord>().ToArray();
            // 判断できないときはfalse
            if (tasks.Length != taskWiths.Length)
            {
                return false;
            }

            return
                taskWiths.All(t => t.IsCompleted());

        }

        public static bool IsCompleted(this ITaskWithRecord @this)
        {
            return @this.Value.HasValue && @this.Records.Sum(x => x.Value) >= @this.Value.Value;
        }

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
