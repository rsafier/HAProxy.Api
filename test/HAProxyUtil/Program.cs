using HAProxy.Api;
using System;
using ServiceStack.Text;
using ServiceStack;

namespace HAProxyUtil
{
    class Program
    {
        /// <summary>
        /// Outputs JSON object of request.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            JsConfig.DateHandler = DateHandler.ISO8601DateTime;
            if (args.Length < 2)
            {
                Console.WriteLine("   HAProxyUtil <host>:<port> <command> [arg()]   ");
                Console.WriteLine("------------------  COMMANDS  -------------------");
                Console.WriteLine("errors");
                Console.WriteLine("info");
                Console.WriteLine("backend");
                Console.WriteLine("stats");
                Console.WriteLine("drain <backend> <server>");
                Console.WriteLine("disable <backend> <server>");
                Console.WriteLine("enable <backend> <server>");
                Console.WriteLine("setweight <backend> <server> <weight>");
                Console.WriteLine("sendcommand <command> <Y>");
                Environment.Exit(0);
            }
            var hostData = args[0].Split(":");
            var client = new HaProxyClient(hostData[0], hostData[1].ConvertTo<int>());
            switch (args[1].ToLower())
            {
                case "errors":
                    Console.Write(client.ShowErrors().ToJson());
                    break;
                case "info":
                    Console.Write(client.ShowInfo().ToJson());
                    break;
                case "backend":
                    Console.Write(client.ShowBackendServers().ToJson());
                    break;
                case "stats":
                    Console.Write(client.ShowStat().ToJson());
                    break;
                case "drain":
                    Console.Write(client.DrainServer(args[2], args[3]).ToJson());
                    break;
                case "enable":
                    Console.Write(client.EnableServer(args[2], args[3]).ToJson());
                    break;
                case "disable":
                    Console.Write(client.DisableServer(args[2], args[3]).ToJson());
                    break;
                case "setweight":
                    Console.Write(client.SetWeight(args[2], args[3], args[4].ConvertTo<int>()).ToJson());
                    break;
                case "sendcommand":
                    Console.Write(client.SendCommand(args[2], args.Length == 4)); //anything in 4th position will count as true for returning raw string (you gotta parse that yourself)
                    break;
            }
        }
    }
}
