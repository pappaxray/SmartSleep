using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SmartSleep
{
    class TeamCityConnection
    {
        #region Interface
        public AgentBuildState BuildState { get; private set; } = AgentBuildState.NotRunning;
        public DateTime lastActiveTime;
        #endregion

        public enum AgentBuildState
        {
            Unknown,
            NotRunning,
            Running,
            Pending
        }

        public string TeamCityAgent = "Sargon-0";
        public string AccessToken = "eyJ0eXAiOiAiVENWMiJ9.YVdGcklJMmdhZ3M2YTNBcVFwbTc5UjVSMXFn.YjUxNTQ0NTYtNTJlYy00ZTBkLTg5N2UtYWI1ZDc0MDI0MTRh";

        public float updateIntervalSecs = 10.0f;

        public HttpClient http = new()
        {
            BaseAddress = new Uri("https://builds.tentoncat.com"),
        };
        DateTime lastUpdate;
        bool requestingAgentState = false;

        delegate void OnGetAgentStatusCompleteCB(AgentBuildState buildState);
        class AgentInfo
        {
            public bool Valid = false;
            public int Id = 0;
            public string WebUrl = null;
        }

        class BuildInfo
        {
            public int BuildsPending = 0;
        }

        class AgentState
        {
            public bool Valid = false;
            public AgentBuildState BuildState;
        }

        public void Init()
        {
            lastUpdate = new DateTime();
            lastActiveTime = DateTime.Now;
        }

        static async Task<AgentInfo> GetAgentInfo(TeamCityConnection conn)
        {
            AgentInfo result = new AgentInfo();
            conn.http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", conn.AccessToken);
            using HttpResponseMessage response = await conn.http.GetAsync("app/rest/agents");
            if (HttpStatusCode.OK == response.StatusCode)
            {
                var txtResponse = await response.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(txtResponse);
                var agents = doc.SelectNodes("agents/agent");
                foreach (XmlNode a in agents)
                {
                    var name = a.Attributes.GetNamedItem("name");
                    if (name.Value == conn.TeamCityAgent) // Found our agent
                    {
                        result.Id = int.Parse(a.Attributes.GetNamedItem("id").Value);
                        result.WebUrl = a.Attributes.GetNamedItem("webUrl").Value;
                        result.Valid = true;
                    }
                }
            }
            return result;
        }

        static async Task<BuildInfo> GetBuildInfo(TeamCityConnection conn)
        {
            BuildInfo result = new BuildInfo();
            conn.http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", conn.AccessToken);
            using HttpResponseMessage response = await conn.http.GetAsync("app/rest/buildQueue");
            if (HttpStatusCode.OK == response.StatusCode)
            {
                var txtResponse = await response.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(txtResponse);
                var agents = doc.SelectNodes("builds/build");
                result.BuildsPending = agents.Count;
            }
            return result;
        }

        static async Task<AgentState> GetAgentState(TeamCityConnection conn, int id)
        {
            AgentState result = new AgentState();
            conn.http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", conn.AccessToken);
            using HttpResponseMessage response = await conn.http.GetAsync($"app/rest/agents/id:{id}");
            if (HttpStatusCode.OK == response.StatusCode)
            {
                var txtResponse = await response.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(txtResponse);
                var build = doc.SelectSingleNode("agent/build");
                if (null != build)
                {
                    result.Valid = true;
                    var state = build.Attributes.GetNamedItem("state");
                    if (state.Value == "running") // Found our agent
                    {
                        result.BuildState = AgentBuildState.Running;
                    }
                }
                else
                {
                    result.BuildState = AgentBuildState.NotRunning;
                }
            }
            return result;
        }

        static async Task GetAgentStatus(TeamCityConnection conn, OnGetAgentStatusCompleteCB onComplete)
        {
            var agentInfo = await GetAgentInfo(conn);
            AgentBuildState buildState = AgentBuildState.Unknown;
            if (agentInfo.Valid)
            {
                var agentState = await GetAgentState(conn, agentInfo.Id);
                buildState = agentState.BuildState;
            }

            if (AgentBuildState.Running != buildState)
            {
                var buildInfo = await GetBuildInfo(conn);
                if (buildInfo.BuildsPending > 0)
                {
                    buildState = AgentBuildState.Pending;
                }
            }
            onComplete(buildState);
        }

        public void Update()
        {
            if (!requestingAgentState)
            {
                if ((DateTime.Now - lastUpdate).Seconds > updateIntervalSecs)
                {
                    requestingAgentState = true;
                    Task.Run(() => GetAgentStatus(this, OnGetAgentStatusComplete));
                }
            }
        }

        public bool IsAgentRequired
        {
            get
            {
                return BuildState == AgentBuildState.Running || BuildState == AgentBuildState.Pending;
            }
        }

        void OnGetAgentStatusComplete(AgentBuildState buildState)
        {
            requestingAgentState = false;
            lastUpdate = DateTime.Now;
            BuildState = buildState;
            if (IsAgentRequired)
            {
                lastActiveTime = DateTime.Now;
            }
        }
    }
}