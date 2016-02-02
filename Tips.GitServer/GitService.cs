using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    public class GitService : IGitService
    {
        private string gitCommandsPath;
        private string projectRootPath;

        public GitService(string projectRootPath, string gitCommandsPath)
        {
            this.projectRootPath = projectRootPath;
            this.gitCommandsPath = gitCommandsPath;
        }

        public IStartableSessionPipe Exec(string command, string project)
        {
            var args = 
                Path.Combine(this.projectRootPath, project + ".git");

            var startInfo = 
                new ProcessStartInfo(
                    Path.Combine(this.gitCommandsPath, command + ".exe"), args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var process = new Process();
            process.StartInfo = startInfo;

            return new GitSessionPipe(process);
        }
    }
}
