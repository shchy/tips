using Haskellable.Code.Monads.Maybe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Boot
{
    class Program
    {
        const string DEFAULT_URL = @"http://localhost:1234";

        static void Main(string[] args)
        {
            var uri = TryToUri(args.FirstOrNothing());

            Console.WriteLine("start up uri is {0}", uri);

            using (var host = new Nancy.Hosting.Self.NancyHost(new Uri(uri), new Bootstrapper()))
            {
                var result = Fn.New(() => { host.Start(); return 0; }).ToExceptional();
                result
                    .OnLeft(ex => Console.WriteLine(ex.Message))
                    .OnRight(_ => Console.WriteLine("Enter to exit."))
                    .OnRight(_ => Console.ReadLine());
                host.Stop();
            }
        }

        private static string TryToUri(IMaybe<string> maybe)
        {
            return maybe.Return(DEFAULT_URL);
        }
    }
}
