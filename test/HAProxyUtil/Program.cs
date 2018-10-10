using HAProxy.Api;
using System;
using ServiceStack.Text;
using ServiceStack;

namespace HAProxyUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            JsConfig.DateHandler = DateHandler.ISO8601DateTime;
            if (args.Length < 2)
            {
                Console.WriteLine("HAProxyUtil <host>:<port> <command>");
                Console.WriteLine("command: errors,info,backend,stat");
                Environment.Exit(0);
            }
            var hostData = args[0].Split(":");
            var client = new HaProxyClient(hostData[0], hostData[1].ConvertTo<int>());
            switch(args[1].ToLower())
            {
                case "errors":
                    Console.WriteLine(client.ShowErrors().ToJson());
                    break;
                case "info":
                    Console.WriteLine(client.ShowInfo().ToJson());
                    break;
                case "backend":
                    Console.WriteLine(client.ShowBackendServers().ToJson());
                    break;
                case "stat":
                    Console.WriteLine(client.ShowStat().ToJson());
                    break;
            }
        }
    }
}
