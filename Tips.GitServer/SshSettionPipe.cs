using FxSsh.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    class SshSettionPipe : ISessionPipe
    {
        private SessionChannel channel;

        public SshSettionPipe(SessionChannel channel)
        {
            this.channel = channel;
            this.channel.DataReceived += Channel_DataReceived;
            this.channel.CloseReceived += Channel_CloseReceived;
        }

        private void Channel_CloseReceived(object sender, EventArgs e)
        {
            EndEvent(0);
        }

        private void Channel_DataReceived(object sender, byte[] e)
        {
            OnData(e);
        }

        public event Action<uint> EndEvent;
        public event Action<IEnumerable<byte>> OnData;

        public void EndData(uint code)
        {
            this.channel.SendClose(code);
        }

        public void PushData(IEnumerable<byte> bytes)
        {
            this.channel.SendData(bytes.ToArray());
        }

        public void Dispose()
        {
            this.channel.DataReceived -= Channel_DataReceived;
            this.channel.CloseReceived -= Channel_CloseReceived;
        }
    }
}
