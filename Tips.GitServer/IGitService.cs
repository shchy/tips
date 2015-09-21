using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    public interface IGitService
    {
        IStartableSessionPipe Exec(string command, string project);
    }

    public interface IStartableSessionPipe : ISessionPipe
    {
        void Start();
    }

    
}
