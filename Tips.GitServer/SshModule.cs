using FxSsh;
using FxSsh.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tips.GitServer
{
    public class SshModule
    {
        private string gitCommandsPath;
        private SshServer server;
        private string projectRootPath;

        public SshModule(string gitCommandsPath, string projectRootPath)
        {
            this.gitCommandsPath = gitCommandsPath;
            this.projectRootPath = projectRootPath;
        }

        public void Start()
        {
            this.server = new SshServer();
            // ServiceRegistered Events When Add Any
            this.server.AddHostKey("ssh-rsa", "<RSAKeyValue><Modulus>xXKzcIH/rzcfv2D7VcvLdxR5S5iw2TTsP65Aa82S4+9ZIqLPTNtuzr76Mz6Cx0yDOhawHlIujtalqaaQzaUvkudCtMcVMnj37OcCYz7XDAYejalCxf/vtJo7U4mnYdCM+nAOQKNIDKLGbLtGuEAGwdi560DOJY2plhnBf1oOI+k=</Modulus><Exponent>AQAB</Exponent><P>37kMr9YiU4cSqHqTJSjBJ/szG2O4n5xSIlPy4MZ4aAN5NALHxfsN0dq1y8NL6GTLMI5qoykvp4Bjrm2ZgU1cDQ==</P><Q>4e84rF+UsFBfQEKJc2pbACWWJjttNW0hccdQZzA3IUxRmd/Z4yEMr1L70TP0XV7dw1RDs1JyU7xnXBIbGy5ETQ==</Q><DP>vP0TbI6VnL3j0xMIrkFJOj8Ho0GQOrTQ5VLJP3wpRqR4hKk8nVBBEl+RZznpK73Jr5D/ICmwqezZSAYpwILbGQ==</DP><DQ>bHdzZtWwRYEgaXJIGL+7lnN1BT/MazTMNJpykEeGgBbqqgvcx/zq4RTezg26SEUuBANlSSbQukCeAoayurbYlQ==</DQ><InverseQ>Irq9vR7CXIVR+r09caYIxIY8BOig+HShN1bXvATERJcjTW2jUgJrUttDGNEx70/hBd7m1NWCZz5YO3RH9Bdf5w==</InverseQ><D>AqxsufxFcW9TDCAmQK4mwVdsoQjRp2jfcULmkM8fl9u40dtxTr6Csv5dz7qfKLWxHTGlDUDabCK2t/DCcZZoA3rsqwLADe4ZerDdg6xiq4MBzNprM8Y0IfNESEdFB9T0T73ONQCsMalUzEvUknC4Ed4Fya34LUHntgQtEhXpDJE=</D></RSAKeyValue>");
            this.server.AddHostKey("ssh-dss", "<DSAKeyValue><P>1fge/S/6Y42F73v/RhtkZQUEgNktLUzjf4zcJPse2JfXGqtg3lMaEiEDbb/2Nm4Q/M7iksZxlIxge0BkP1ul69+Tovl+Gg+hI+OPGaKlD/1dnX4D4i1FiOpnjtyoCTeZysJ5X8nO1lSInDRnRv46gHk4cHSFVKILKY7CVo1sf0s=</P><Q>tb2irBOnujteP+kf5HmH0yXepE8=</Q><G>HJfQK2Sdd5vN47HWVdmRRYhwAhiVoP98l7FZ3FzVxZvE/CurAL2xVrDi9modbLg0UA1JHeOi6h9FTI9ijepnqRscBKFlyIny0wh15sc8EMcZzG/vJVH2M48ps1knt793sxEwjU/SPYxE7UEX5HGa/KyLuP6Ev/0z4WOOIJ62B5w=</G><Y>ebWWjjGiFpfKfaXlrY3SiMorrkvge/UEeHd5E27RS4B/BBlm9OhdNBFq3zzILAa3X8KvDOGlVZgTYqDchp3/SK/Y8HsbqNyTyqDMNvcnHILDqSrWmGMQEA/0kGiPn0nwEcA87F1osCro7Y70SuLNwUOl3xh+gQKdi5V6WU75gaw=</Y><Seed>w0QY1PFWINvz3x2OZlVF6Vi5cCw=</Seed><PgenCounter>UQ==</PgenCounter><X>SFKXJnLYP8UM5tlOCpVQ0S2yE78=</X></DSAKeyValue>");
            
            this.server.ConnectionAccepted += server_ConnectionAccepted;
            this.server.Start();
        }

        void server_ConnectionAccepted(object sender, Session e)
        {
            e.ServiceRegistered += e_ServiceRegistered;
        }

        void e_ServiceRegistered(object sender, SshService e)
        {
            var session = (Session)sender;

            if (e is UserauthService)
            {
                var service = (UserauthService)e;
                service.Userauth += service_Userauth;
            }
            else if (e is ConnectionService)
            {
                var service = (ConnectionService)e;
                service.CommandOpened += service_CommandOpened;
            }
        }

        void service_Userauth(object sender, UserauthArgs e)
        {
            //Console.WriteLine("Client {0} fingerprint: {1}.", e.KeyAlgorithm, e.Fingerprint);

            e.Result = true;
        }

        void service_CommandOpened(object sender, SessionRequestedArgs e)
        {
            var allow = true; // func(e.CommandText, e.AttachedUserauthArgs);

            if (!allow)
                return;

            var parser = new Regex(@"(?<cmd>git-receive-pack|git-upload-pack|git-upload-archive) \'/?(?<proj>.+)\.git\'");
            var match = parser.Match(e.CommandText);
            var command = match.Groups["cmd"].Value;
            var project = match.Groups["proj"].Value;


            var git = 
                new GitService(
                    this.projectRootPath
                    , this.gitCommandsPath);
            var gitPipe = git.Exec(command, project);
            var sshPipe = new SshSettionPipe(e.Channel);

            gitPipe.OnData += (bytes) => sshPipe.PushData(bytes);
            sshPipe.OnData += (bytes) => gitPipe.PushData(bytes);
            gitPipe.EndEvent += code => sshPipe.EndData(code);
            sshPipe.EndEvent += code => gitPipe.EndData(code);
            
            Task.Run((Action)gitPipe.Start);
        }
    }
}
