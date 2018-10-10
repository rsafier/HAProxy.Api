using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Text;

namespace HAProxy.Api
{
    public class HaProxyClient
    {
        private readonly string _host;
        private readonly int _port;

        public HaProxyClient(string host, int port)
        {
            _host = host;
            _port = port;
        }


        public string SendCommand(string command, bool readAnswer = true)
        {
            using (var client = new TcpClient(_host, _port))
            using (var stream = client.GetStream())
            {
                var bytes = (Encoding.ASCII.GetBytes(command + "\n"));
                stream.Write(bytes, 0, bytes.Length);

                stream.ReadTimeout = 500;
                string result = null;
                if (readAnswer)
                {
                    byte[] data = new byte[1024];
                    using (var ms = new MemoryStream())
                    {
                        int numBytesRead;
                        while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, numBytesRead);
                        }
                        result = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                    }
                }
                return result;
            }
        }


        private string ShowInfoRaw()
        {
            return SendCommand("show info", true);
        }

        public ShowInfoResponse ShowInfo()
        {
            return new ShowInfoParser().Parse(ShowInfoRaw());
        }

        public List<Stats> ShowStat()
        {
            var x =  SendCommand("show stat", true);
            var y = ParseResponse<Stats>(x,trimComma:true);
            return y;
        }

        private string ShowErrorsRaw()
        {
            return SendCommand("show errors", true);
        }

        public ShowErrorResponse ShowErrors()
        {
            var response = ShowErrorsRaw();

            return new ShowErrorParser().Parse(response);
        }

        public IEnumerable<Backend> ShowBackends()
        {
            var resp = SendCommand("show backend");
            return ParseResponse<Backend>(resp);
        }

        public IEnumerable<BackendServer> ShowBackendServers(string backend = null)
        {
            var resp = SendCommand("show servers state " + backend);
            return ParseResponse<BackendServer>(resp);
        }

        public BackendServer DisableServer(string backend, string server)
        {
            SendCommand($"disable server {backend}/{server}");
            return ShowBackendServer(backend, server);
        }


        public BackendServer SetWeight(string backend, string server, int weight)
        {
            SendCommand($"set server {backend}/{server} weight {weight}", false);
            return ShowBackendServer(backend, server);
        }

        public BackendServer DrainServer(string backend, string server)
        {
            SendCommand($"set server {backend}/{server} state drain", false);
            return ShowBackendServer(backend, server);
        }


        public BackendServer ShowBackendServer(string backend, string server)
        {
            return ShowBackendServers(backend)
                    .FirstOrDefault(
                        x =>
                            string.Equals(x.Name, server, StringComparison.OrdinalIgnoreCase));

        }
        public BackendServer EnableServer(string backend, string server)
        {
            SendCommand($"enable server {backend}/{server}");
            var state =
                ShowBackendServers()
                    .FirstOrDefault(
                        x =>
                            string.Equals(backend, x.BackendName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.Name, server, StringComparison.OrdinalIgnoreCase));
            return state;
        }
        private List<T> ParseResponse<T>(string raw, char delimeter = ' ', bool trimComma = false)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<T>();
            }
            var indexOfSharp = raw.IndexOf("#", StringComparison.Ordinal);

            if (indexOfSharp == 0 && raw.Length == 1)
            {
                return new List<T>();
            }

            if (indexOfSharp >= 0)
            {
                raw = raw.Substring(indexOfSharp + 1);
            }
            raw = raw.Trim().Replace(" ", ",");
            var lines = raw.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            CsvReader<T>.Headers = lines[0].Split(',').Where(t=>t != string.Empty).ToList();
            if (trimComma)
            {
                var result = new List<T>();
                for(int i = 1;i<lines.Length;i++)
                {
                    CsvConfig<T>.OmitHeaders = true;
                    result.Add((T)CsvReader<T>.ReadObjectRow(lines[i]));
                }
                return result;
            }
            var data = raw.FromCsv<List<T>>();
            return data;
        }

    }
}
