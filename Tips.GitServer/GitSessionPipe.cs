using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    public class GitSessionPipe : IStartableSessionPipe
    {
        private Process gitProcess;

        public GitSessionPipe(Process gitProcess)
        {
            this.gitProcess = gitProcess;
        }

        public void Start()
        {
            this.gitProcess.Start();

            MessageLoop(this.gitProcess.StandardOutput.BaseStream, OnData);

            if (EndEvent != null)
            {
                EndEvent((uint)this.gitProcess.ExitCode);
            }
        }

        private void MessageLoop(
            Stream stream
            , Action<IEnumerable<byte>> callback)
        {
            var bytes = new byte[1024 * 64];
            while (true)
            {
                var len = stream.Read(bytes, 0, bytes.Length);
                if (len <= 0)
                    break;

                var data = bytes.Length != len
                    ? bytes.Take(len).ToArray()
                    : bytes;
                if (callback != null)
                    callback(data);
            }
        }

        public event Action<uint> EndEvent;
        public event Action<IEnumerable<byte>> OnData;

        public void PushData(IEnumerable<byte> bytes)
        {
            var data = bytes.ToArray();
            this.gitProcess.StandardInput.BaseStream.Write(data, 0, data.Length);
            this.gitProcess.StandardInput.BaseStream.Flush();
        }

        public void EndData(uint code)
        {
            this.gitProcess.StandardInput.BaseStream.Close();
        }

        public void Dispose()
        {

        }
    }
}
