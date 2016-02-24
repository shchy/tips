using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Context
{
    public interface IDataBaseSource<out Source>
    {
        T Get<T>(Func<Source, T> getter);
        void Update(Action<Source> setter);
        void Delete(Action<Source> deleter);
    }
}
