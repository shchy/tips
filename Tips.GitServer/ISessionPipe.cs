using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    public interface ISessionPipe : IDisposable
    {
        event Action<IEnumerable<byte>> OnData;
        event Action<uint> EndEvent;
        void PushData(IEnumerable<byte> bytes);
        void EndData(uint code);
    }
}
