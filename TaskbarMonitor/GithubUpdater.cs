using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor
{
    class GithubUpdater
    {
        private string user = "";
        private string project = "";

        public GithubUpdater(string user, string project)
        {
            this.user = user;
            this.project = project;
        }
        public string GetURL()
        {
            return "https://github.com/" + user + "/" + project + "/releases/latest";
        }
        public async Task<bool> CheckForUpdatesAsync(Version version, int fieldCount)
        {
            //https://github.com/leandrosa81/taskbar-monitor/releases

            using (var client = new HttpClient())
            {
                var result = client.GetAsync(GetURL()).Result;
                
                var html = await result.Content.ReadAsStringAsync();
                //<meta property="og:url" content="/leandrosa81/taskbar-monitor/releases/tag/v0.2.0">
                var tag = @"<meta property=""og:url"" content=""/" + this.user + @"/" + this.project + @"/releases/tag/v" + version.ToString(fieldCount) + @"""";
                var ret = html.Contains(tag);
                return !ret;
            }            
        }
    }
}
