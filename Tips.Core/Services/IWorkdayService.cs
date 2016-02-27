using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Core.Services
{
    public interface IWorkdayContext
    {
        void Save(IWorkdayModify workdayModify);
        IWorkdayModify Load();
    }

    /// <summary>
    /// Mod Value/Day
    /// </summary>
    public interface IWorkdayModify
    {
        /// <summary>
        /// 土日はデフォルトでお休み
        /// </summary>
        bool IsHolidayWeekEnd { get; }
        /// <summary>
        /// TargetDay
        /// </summary>
        IEnumerable<DateTime> ModifyDays { get; }
    }

    public class WorkdayModify : IWorkdayModify
    {
        public WorkdayModify()
        {
            this.IsHolidayWeekEnd = true;
            this.ModifyDays = new List<DateTime> { DateTime.MinValue };
        }

        public bool IsHolidayWeekEnd { get; set; }

        [JsonConverter(typeof(ConcreteConverter<List<DateTime>>))]
        public IEnumerable<DateTime> ModifyDays { get; set; }

        public static IWorkdayModify New(IWorkdayModify source)
        {
            return new WorkdayModify
            {
                ModifyDays = source.ModifyDays,
                IsHolidayWeekEnd = source.IsHolidayWeekEnd,
            };
        }


        public static string ToJsonText(IWorkdayModify @this)
        {
            return JsonConvert.SerializeObject(@this, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.Indented });
        }

        public static IWorkdayModify ToModel(string jsonText)
        {
            return JsonConvert.DeserializeObject<WorkdayModify>(jsonText);
        }
    }


    public static class WorkdayContextExtension
    {
        public static bool IsWorkday(this IWorkdayContext @this, DateTime day)
        {
            var context = @this.Load();
            // 引数が週末かつ週末が休日設定だったら稼働日じゃない
            var isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
            if (isWeekend && context.IsHolidayWeekEnd)
            {
                return false;
            }

            // Moddays（非稼働日）に設定されてる日だったら稼働日じゃない
            var isWorkDay = context.ModifyDays.FirstOrDefault(x => (x - day).Days == 0) == default(DateTime);
            return isWorkDay;
        }

    }

    public class WorkdayContext : IWorkdayContext
    {
        private string filePath;

        public WorkdayContext(string filePath)
        {
            this.filePath = filePath;
        }

        public IWorkdayModify Load()
        {
            // todo もう少し抽象化する
            if (File.Exists(this.filePath) == false)
            {
                var defaultModel = new WorkdayModify();
                Save(defaultModel);
            }

            using (var fs = new FileStream(this.filePath, FileMode.Open))
            using (var r = new StreamReader(fs))
            {
                var json = r.ReadToEnd();
                var model = WorkdayModify.ToModel(json);
                return model;
            }
        }

        public void Save(IWorkdayModify workdayModify)
        {
            if (Directory.Exists(Path.GetDirectoryName(this.filePath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.filePath));
            }

            using (var fs = new FileStream(this.filePath, FileMode.Create))
            using (var w = new StreamWriter(fs))
            {
                var json = WorkdayModify.ToJsonText(workdayModify);
                w.Write(json);
            }
        }
    }
}
