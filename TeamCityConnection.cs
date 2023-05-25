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
        public AgentBuildState AgentState
        {
            get
            {
                return _buildState;
            }
            
            private set
            {
                if(_buildState != value)
                {
                    _buildState = value;
                    Logger.Log($"Build state changed:{_buildState}");
                }
            }
        }
        AgentBuildState _buildState = AgentBuildState.Initialising;

        public int BuildID
        {
            set
            {
                if (_buildID != value)
                {
                    _buildID = value;
                    Logger.Log($"BuildID:{_buildID}");
                }
            }
            get
            {
                return _buildID;
            }
        }
        public int _buildID = -1;

        public int SecsSinceLastUpdate
        {
            get
            {
                return (DateTime.Now - lastUpdate).Seconds;
            }
        }

        public DateTime lastActiveTime;
        #endregion

        public enum AgentBuildState
        {
            Initialising,
            Requesting,
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

        delegate void OnGetAgentStatusCompleteCB(AgentBuildState buildState, int buildID);
        delegate void OnGetAgentStatusErrorCB(string txt);

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

        class AgentStateRequest
        {
            public bool Valid = false;
            public AgentBuildState State;
            public int BuildID = -1;
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

        static async Task<AgentStateRequest> GetAgentState(TeamCityConnection conn, int id)
        {
            AgentStateRequest result = new AgentStateRequest();
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
                        result.State = AgentBuildState.Running;
                    }
                    else
                    {
                        Logger.Log($"Unexpected agent state:{state.Value}");
                    }

                    var buildIdAtt = build.Attributes.GetNamedItem("number");
                    if (null != buildIdAtt)
                    {
                        result.BuildID = int.Parse(buildIdAtt.Value);
                    }
                }
                else
                {
                    result.State = AgentBuildState.NotRunning;
                }
            }
            return result;
        }

        static async Task GetAgentStatus(TeamCityConnection conn, OnGetAgentStatusCompleteCB onComplete, OnGetAgentStatusErrorCB onError)
        {
            try
            {
                AgentBuildState buildState = AgentBuildState.Requesting;
                int buildID = -1;

                var agentInfo = await GetAgentInfo(conn); // Check agent state to see if it's building
                if (agentInfo.Valid)
                {
                    var agentStateReq = await GetAgentState(conn, agentInfo.Id);
                    buildState = agentStateReq.State;
                    buildID = agentStateReq.BuildID;
                }

                if (AgentBuildState.Running != buildState) // Agent is not running, check build queue to see if there is one pending
                {
                    var buildInfo = await GetBuildInfo(conn);
                    if (buildInfo.BuildsPending > 0)
                    {
                        buildState = AgentBuildState.Pending;
                    }
                }
                onComplete(buildState, buildID);
            }
            catch(Exception e)
            {
                onError(e.Message);
            }
        }

        public void Update()
        {
            if (!requestingAgentState)
            {
                if (SecsSinceLastUpdate > updateIntervalSecs)
                {
                    requestingAgentState = true;
                    Task.Run(() => GetAgentStatus(this, OnGetAgentStatusComplete, OnGetAgentSatusError));
                }
            }
        }

        public bool IsAgentRequired
        {
            get
            {
                return AgentState == AgentBuildState.Running || AgentState == AgentBuildState.Pending;
            }
        }

        void OnGetAgentStatusComplete(AgentBuildState buildState, int buildID)
        {
            requestingAgentState = false;
            lastUpdate = DateTime.Now;
            AgentState = buildState;
            BuildID = buildID;
            if (IsAgentRequired)
            {
                lastActiveTime = DateTime.Now;
            }
        }

        void OnGetAgentSatusError(string err)
        {
            requestingAgentState = false;
            lastUpdate = DateTime.Now;
            Logger.Log($"GetAgentSatusError:{err}");
        }
    }
}