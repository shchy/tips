using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Context
{
    public class DataBaseSource<Context> : IDataBaseSource<Context>
        where Context : DbContext
    {
        private Func<Context> contextFactory;

        public DataBaseSource(Func<Context> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// select系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getter"></param>
        /// <returns></returns>
        public T Get<T>(Func<Context, T> getter)
        {
            try
            {
                using (var db = contextFactory())
                {
                    return
                        getter(db);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// 更新系
        /// </summary>
        /// <param name="setter"></param>
        public void Update(Action<Context> setter)
        {
            try
            {
                using (var db = contextFactory())
                {
                    setter(db);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
