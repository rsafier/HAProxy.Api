using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace HAProxy.Api
{
    public class Backend
    {
        public string Name { get; set; }
    }
    [DataContract]
    public class BackendServer
    {
        [DataMember(Name = "srv_name")]
        public string Name { get; set; }
        [DataMember(Name = "srv_id")]
        public string Id { get; set; }
        [DataMember(Name = "be_name")]
        public string BackendName { get; set; }
        [DataMember(Name = "be_id")]
        public string BackendId { get; set; }
        [DataMember(Name = "srv_addr")]
        public string Ip { get; set; }
        [DataMember(Name = "srv_op_state")]
        public OperationalState OperationalState { get; set; }
        [DataMember(Name = "srv_admin_state")]
        public AdministrativeState AdministrativeState { get; set; }
        [DataMember(Name = "srv_uweight")]
        public int CurrentWeight { get; set; }
        [DataMember(Name = "srv_iweight")]
        public int InitialWeight { get; set; }
        [DataMember(Name = "srv_time_since_last_change")]
        public int TimeSinceLastChange { get; set; }
        [DataMember(Name = "srv_check_status")]
        public int CheckStatus { get; set; }
        [DataMember(Name = "srv_check_result")]
        public CheckResult CheckResult { get; set; }
        [DataMember(Name = "srv_check_health")]
        public int CheckHealthCounter { get; set; }

        [DataMember(Name = "srv_check_state")]
        public CheckState CheckState { get; set; }
        [DataMember(Name = "srv_agent_state")]
        public CheckState AgentState { get; set; }
        [DataMember(Name = "bk_f_forced_id")]
        public int BackendForcedId { get; set; }
        [DataMember(Name = "srv_f_forced_id")]
        public int ServerForcedId { get; set; }
        public TimeSpan GetTimeSinceLastChange() => TimeSpan.FromSeconds(TimeSinceLastChange);
    }
    public enum CheckState
    {
        CHK_ST_INPROGRESS = 0x0001,
        CHK_ST_CONFIGURED = 0x0002,
        CHK_ST_ENABLED = 0x0004,
        CHK_ST_PAUSED = 0x0008,
        CHK_ST_AGENT = 0x0010
    }
    public enum CheckResult
    {
        CHK_RES_UNKNOWN = 0,
        CHK_RES_NEUTRAL,
        CHK_RES_FAILED,
        CHK_RES_PASSED,
        CHK_RES_CONDPASS,
    };

    public enum AdministrativeState
    {
        SRV_ADMF_FMAINT = 0x01,
        SRV_ADMF_IMAINT = 0x02,
        SRV_ADMF_MAINT = 0x03,
        SRV_ADMF_CMAINT = 0x04,
        SRV_ADMF_FDRAIN = 0x08,
        SRV_ADMF_IDRAIN = 0x10,
        SRV_ADMF_DRAIN = 0x18,
    };
    public enum OperationalState
    {
        SRV_ST_STOPPED = 0,
        SRV_ST_STARTING,
        SRV_ST_RUNNING,
        SRV_ST_STOPPING,
    };

    public class ShowInfoParser
    {
        public ShowInfoResponse Parse(string rawShowInfoResult)
        {
            var propertyDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(rawShowInfoResult))
            {
                var matchResult = Regex.Matches(rawShowInfoResult, @"(?<key>[\w-]+): (?<value>.+)$", RegexOptions.Multiline);

                if (matchResult.Count != 0)
                {
                    foreach (Match match in matchResult)
                    {
                        if (!match.Success) continue;
                        propertyDictionary[match.Groups["key"].Value] = match.Groups["value"].Value;
                    }
                }
            }
            return GetResult(rawShowInfoResult, propertyDictionary);
        }


        protected virtual ShowInfoResponse GetResult(string raw, Dictionary<string, string> properties)
        {
            if (string.IsNullOrWhiteSpace(raw) || properties == null || !properties.Any())
                return new ShowInfoResponse()
                {
                    Raw = raw
                };
            return new ShowInfoResponse()
            {
                Name = GetValue<string>(properties, "Name"),
                Version = GetValue<string>(properties, "Version"),
                MaxConnections = GetValue<int>(properties, "Maxconn"),
                ReleaseDate = GetDateTime(properties, "Release_date"),
                MaxSockets = GetValue<int>(properties, "Maxsock"),
                Uptime = TimeSpan.FromSeconds(GetValue<int>(properties, "Uptime_sec")),
                Node = GetValue<string>(properties, "node"),
                UlimitN = GetValue<int>(properties, "Ulimit-n")
            };
        }

        private T GetValue<T>(Dictionary<string, string> properties, string propertyName)
        {
            if (!properties.ContainsKey(propertyName))
                return default(T);
            return (T)Convert.ChangeType(properties[propertyName], typeof(T));
        }

        private DateTime? GetDateTime(Dictionary<string, string> properties, string propertyName)
        {
            if (!properties.ContainsKey(propertyName))
                return null;
            return DateTime.ParseExact(properties[propertyName], "yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
    }

    public class ShowInfoResponse
    {
        public string Raw { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int MaxConnections { get; set; }
        public int MaxSockets { get; set; }
        public TimeSpan Uptime { get; set; }
        public string Node { get; set; }
        public int UlimitN { get; set; }
    }

    public class ShowErrorResponse
    {
        public string Raw { get; set; }
        public DateTime? CapturedOn { get; set; }
        public long? TotalEvents { get; set; }
    }

    public class ShowErrorParser
    {
        public ShowErrorResponse Parse(string rawShowErrorResult)
        {
            var result = new ShowErrorResponse()
            {
                Raw = rawShowErrorResult,
            };
            if (!string.IsNullOrEmpty(rawShowErrorResult))
            {
                var match = Regex.Match(rawShowErrorResult, @"Total events captured on \[(?<date>.+)\].+: (?<total>\d+)",
                    RegexOptions.Multiline);

                if (match.Success)
                {
                    result.CapturedOn = DateTime.ParseExact(match.Groups["date"].Value, @"dd/MMM/yyyy:HH:mm:ss.fff",
                        CultureInfo.InvariantCulture);
                    result.TotalEvents = Int32.Parse(match.Groups["total"].Value);
                }

            }
            return result;
        }
    }
    [DataContract()]
    public class Stats //TODO: type mappings are not correct, didn't take time to make accurate, for initial purpose of logging source doesn't matter.
    {
        [DataMember()] public string pxname { get; set; }
        [DataMember()] public string svname { get; set; }
        [DataMember()] public string qcur { get; set; }
        [DataMember()] public string qmax { get; set; }
        [DataMember()] public string scur { get; set; }
        [DataMember()] public string smax { get; set; }
        [DataMember()] public string slim { get; set; }
        [DataMember()] public string stot { get; set; }
        [DataMember()] public string bin { get; set; }
        [DataMember()] public string bout { get; set; }
        [DataMember()] public string dreq { get; set; }
        [DataMember()] public string dresp { get; set; }
        [DataMember()] public string ereq { get; set; }
        [DataMember()] public string econ { get; set; }
        [DataMember()] public string eresp { get; set; }
        [DataMember()] public string wretr { get; set; }
        [DataMember()] public string wredis { get; set; }
        [DataMember()] public string status { get; set; }
        [DataMember()] public string weight { get; set; }
        [DataMember()] public string act { get; set; }
        [DataMember()] public string bck { get; set; }
        [DataMember()] public string chkfail { get; set; }
        [DataMember()] public string chkdown { get; set; }
        [DataMember()] public string lastchg { get; set; }
        [DataMember()] public string downtime { get; set; }
        [DataMember()] public string qlimit { get; set; }
        [DataMember()] public string pid { get; set; }
        [DataMember()] public string iid { get; set; }
        [DataMember()] public string sid { get; set; }
        [DataMember()] public string throttle { get; set; }
        [DataMember()] public string lbtot { get; set; }
        [DataMember()] public string tracked { get; set; }
        [DataMember()] public string type { get; set; }
        [DataMember()] public string rate { get; set; }
        [DataMember()] public string rate_lim { get; set; }
        [DataMember()] public string rate_max { get; set; }
        [DataMember()] public string check_status { get; set; }
        [DataMember()] public string check_code { get; set; }
        [DataMember()] public string check_duration { get; set; }
        [DataMember()] public string hrsp_1xx { get; set; }
        [DataMember()] public string hrsp_2xx { get; set; }
        [DataMember()] public string hrsp_3xx { get; set; }
        [DataMember()] public string hrsp_4xx { get; set; }
        [DataMember()] public string hrsp_5xx { get; set; }
        [DataMember()] public string hrsp_other { get; set; }
        [DataMember()] public string hanafail { get; set; }
        [DataMember()] public string req_rate { get; set; }
        [DataMember()] public string req_rate_max { get; set; }
        [DataMember()] public string req_tot { get; set; }
        [DataMember()] public string cli_abrt { get; set; }
        [DataMember()] public string srv_abrt { get; set; }
        [DataMember()] public string comp_in { get; set; }
        [DataMember()] public string comp_out { get; set; }
        [DataMember()] public string comp_byp { get; set; }
        [DataMember()] public string comp_rsp { get; set; }
        [DataMember()] public string lastsess { get; set; }
        [DataMember()] public string last_chk { get; set; }
        [DataMember()] public string last_agt { get; set; }
        [DataMember()] public string qtime { get; set; }
        [DataMember()] public string ctime { get; set; }
        [DataMember()] public string rtime { get; set; }
        [DataMember()] public string ttime { get; set; }
        [DataMember()] public string agent_status { get; set; }
        [DataMember()] public string agent_code { get; set; }
        [DataMember()] public string agent_duration { get; set; }
        [DataMember()] public string check_desc { get; set; }
        [DataMember()] public string agent_desc { get; set; }
        [DataMember()] public string check_rise { get; set; }
        [DataMember()] public string check_fall { get; set; }
        [DataMember()] public string check_health { get; set; }
        [DataMember()] public string agent_rise { get; set; }
        [DataMember()] public string agent_fall { get; set; }
        [DataMember()] public string agent_health { get; set; }
        [DataMember()] public string addr { get; set; }
        [DataMember()] public string cookie { get; set; }
        [DataMember()] public string mode { get; set; }
        [DataMember()] public string algo { get; set; }
        [DataMember()] public string conn_rate { get; set; }
        [DataMember()] public string conn_rate_max { get; set; }
        [DataMember()] public string conn_tot { get; set; }
        [DataMember()] public string intercepted { get; set; }
        [DataMember()] public string dcon { get; set; }
        [DataMember()] public string dses { get; set; }
    }
}

