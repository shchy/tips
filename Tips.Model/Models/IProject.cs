using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models
{
    public interface IProject : IIdentity<int>
    {
        string Name { get; }

    }




    public interface IStoryBoard
    {


    }


}
