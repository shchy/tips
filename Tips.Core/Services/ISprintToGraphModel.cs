using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Core.Services
{
    public interface ISprintToGraphModel
    {
        IGraphModel Make(ISprint sprint);
        IEnumerable<IGraphPoint> Merge(IEnumerable<IGraphPoint> x, IEnumerable<IGraphPoint> y);
    }

    public class SprintToGraphModel : ISprintToGraphModel
    {
        private Func<DateTime, bool> isWorkday;

        public SprintToGraphModel(Func<DateTime,bool> isWorkday)
        {
            this.isWorkday = isWorkday;
        }

        public IGraphModel Make(ISprint sprint)
        {
            // タスクを抽出
            var tasks = sprint.Tasks.OfType<ITaskWithRecord>().ToArray();
            // 無かったら空っぽを返す
            if (tasks.Any()==false)
            {
                return new GraphModel();
            }
            // まずは期間を確定させる。
            // 未設定の場合は登録されているメモやレコードから推測する
            var records = tasks.SelectMany(x => x.Records).ToArray();
            var comments = tasks.SelectMany(x => x.Comments).ToArray();
            var dateList = records.Select(x => x.Day).Concat(comments.Select(x => x.Day)).ToArray();
            var left = sprint.Left ?? (dateList.Any() ? dateList.Min() : DateTime.Now);
            var right = sprint.Right ?? (dateList.Any() ? dateList.Max() : DateTime.Now);

            // 次にPV（計画）
            // 休日を除く日で総Valueを案分する
            var workDays =
                (from i in Enumerable.Range(0, (right - left).Days + 1)
                let day = left.AddDays(i)
                where this.isWorkday(day)
                select day).ToArray();

            // 稼働日が無い場合は先頭日に全部押し付ける
            if(workDays.Any() == false)
            {
                workDays = new[] { left };
            }

            // 計画値の総量
            var pvAll = tasks.Sum(x => x.Value);
            // 1日あたりの量
            var pvAve = pvAll / workDays.Length;
            var pvList = workDays.Select(x => new GraphPoint { Day = x, Value = pvAve }).ToArray();
            var evList = records.Select(x => new GraphPoint { Day = x.Day, Value = x.Value }).ToArray();
            var acList = records.Select(x => new GraphPoint { Day = x.Day, Value = x.WorkValue }).ToArray();

            return new GraphModel
            {
                Pv = pvList,
                Ac = acList,
                Ev = evList,
            };
        }

        public IEnumerable<IGraphPoint> Merge(IEnumerable<IGraphPoint> x, IEnumerable<IGraphPoint> y)
        {
            // 同じ日のものはValueを足して1つにする

            var dx = new Dictionary<Tuple<int,int,int>, double>();
            foreach (var item in x.Concat(y))
            {
                var key = Tuple.Create(item.Day.Year, item.Day.Month, item.Day.Day);
                if (dx.ContainsKey(key) == false)
                {
                    dx[key] = 0;
                }
                dx[key] += item.Value;
            }

            return
                dx
                .Select(a => new GraphPoint { Day = new DateTime(a.Key.Item1, a.Key.Item2, a.Key.Item3), Value = a.Value })
                .ToArray();
        }
    }
}
