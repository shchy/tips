using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Core.Services
{
    public interface ITaskToTextFactory
    {
        IEnumerable<ISprint> Make(string text);
        string Make(IEnumerable<ISprint> sprints);
    }

    public class TaskToTextFactory : ITaskToTextFactory
    {
        public TaskToTextFactory()
        {

        }


        public string Make(IEnumerable<ISprint> sprints)
        {
            var items =
                from s in sprints
                select new object[] { s }.Concat(s.Tasks);

            var lines =
                items
                .SelectMany(x => x)
                .Select(x =>
                    x.ToCaseOf()
                    .Match((ISprint a) => ToText(a))
                    .Match((ITaskItem a) => ToText(a))
                    .Return(string.Empty));
            var sb = new StringBuilder();

            lines.ForEach(l => sb.AppendLine(l));

            return sb.ToString();
        }

        private string ToText(ISprint model)
        {
            return
                string.Format(@"# {0} {1} {2} {3}"
                    , model.Id.ToString("[00]")
                    , model.Name
                    , model.Left.HasValue ? model.Left.Value.ToString("@yyyy/MM/dd") : ""
                    , model.Right.HasValue ? model.Right.Value.ToString("@yyyy/MM/dd") : "");

        }
        private string ToText(ITaskItem model)
        {
            return
                string.Format(@"- {0} {1} {2}"
                    , model.Id.ToString("[00]")
                    , model.Name
                    , model.Value.ToString("0pt"));
        }

        public IEnumerable<ISprint> Make(string text)
        {
            var items =
                from line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                let item =
                    from f in new Func<string,object>[] { ToSprint, ToTask }
                    let v = f(line)
                    where v != null
                    select v
                let rslt = item.FirstOrDefault()
                where rslt != null 
                select rslt;

            var result = ToSprintList(items.ToArray()).ToArray();
            return result;
        }

        private IEnumerable<ISprint> ToSprintList(IEnumerable<object> items)
        {
            // もし先頭にTaskが書かれてたら無所属なので除外
            var seekedSprint = items.SkipWhile(x => (x is ISprint) == false).ToArray();
            // 残りが無かったらおしまい
            if (seekedSprint.Any() == false)
            {
                yield break;
            }

            // 今回処理するSprintを取り出し
            var sprint = seekedSprint.First() as Sprint;
            // 次のSprintまでに書かれているTaskを取り出し
            var tasks = seekedSprint.Skip(1).TakeWhile(x => x is ITaskItem).OfType<ITaskItem>().ToArray();
            // Sprintに追加
            tasks.ForEach(sprint.AddTask);
            // 返す
            yield return sprint;

            var tails = items.Where(x => x != sprint).Where(x => tasks.Any(y => y == x) == false).ToArray();
            // 残りを再帰で処理
            var result = ToSprintList(tails);
            foreach (var item in result)
            {
                yield return item;
            }


        }

        // todo あとで別クラスにする
        ITaskItem ToTask(string line)
        {
            if (line.TrimStart().StartsWith("-") == false)
            {
                return null;
            }

            var task = new TaskItem();
            var name = line.TrimStart().TrimStart('-');

            {
                var reg = new Regex(@"(@(\d+)pt|@(\d+))",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var findedList = reg.Matches(line).OfType<Match>().Select(x => x.Value);
                var values = findedList.Select(x=>TryToInt(x.TrimStart('@').Replace("pt", string.Empty))).ToArray();

                if (values.Any())
                    task.Value = values.Max();
                name = 
                    findedList.Aggregate(
                        name
                        , (a, x) => a.Replace(x, string.Empty))
                        .Trim();
            }

            {
                var reg = new Regex(@"[(\d+)]",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var findedList = reg.Matches(line).OfType<Match>().Select(x => x.Value);
                var values = findedList.Select(x=>TryToInt(x.TrimStart('[').TrimEnd(']'))).ToArray();

                if (values.Any())
                    task.Id = values.Max();

                name =
                   findedList.Aggregate(
                       name
                       , (a, x) => a.Replace(x, string.Empty))
                       .Trim();
            }

            task.Name = name;
            return task;
        }


        // todo あとで別クラスにする
        ISprint ToSprint(string line)
        {
            if (line.TrimStart().StartsWith("#") == false)
            {
                return null;
            }

            var model = new Sprint();
            var name = line.TrimStart().TrimStart('#');
            {
                var reg = new Regex(@"(@(\d{4})/(\d{2})/(\d{2})|@(\d{2})/(\d{2}))",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var findedList = reg.Matches(line).OfType<Match>().Select(x => x.Value);

                var dates = findedList.Select(x => TryToDate(x.TrimStart('@'))).Where(x => x != DateTime.MinValue).ToArray();

                model.Left = dates.Any() ? new DateTime?(dates.Min()) : null;
                model.Right = dates.Count() > 1 ? new DateTime?(dates.Max()) : null;


                name =
                findedList.Aggregate(
                    name
                    , (a, x) => a.Replace(x, string.Empty))
                    .Trim();
            }
            {
                var reg = new Regex(@"[(\d+)]",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var findedList = reg.Matches(line).OfType<Match>().Select(x => x.Value);
                var values = findedList.Select(x => TryToInt(x.TrimStart('[').TrimEnd(']'))).ToArray();

                if (values.Any())
                    model.Id = values.Max();

                name =
                findedList.Aggregate(
                    name
                    , (a, x) => a.Replace(x, string.Empty))
                    .Trim();
            }
            model.Name = name;
            return model;
        }


        private int TryToInt(string arg)
        {
            int v = 0;
            int.TryParse(arg, out v);
            return v;
        }

        private DateTime TryToDate(string value)
        {
            var v = DateTime.MinValue;
            if(DateTime.TryParse(value, out v))
            {
                return v;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
    }
}
