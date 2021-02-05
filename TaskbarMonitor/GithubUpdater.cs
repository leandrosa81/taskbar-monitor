using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
        public async Task<Version> GetLastestVersionAsync()
        {
            Version latest = null;
            //https://github.com/leandrosa81/taskbar-monitor/releases    
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            using (var client = new HttpClient())
            {
                var result = client.GetAsync(GetURL()).Result;
                
                var html = await result.Content.ReadAsStringAsync();
                //<meta property="og:url" content="/leandrosa81/taskbar-monitor/releases/tag/v0.2.0">
                //var tag = @"<meta property=""og:url"" content=""/" + this.user + @"/" + this.project + @"/releases/tag/v" + version.ToString(fieldCount) + @"""";
                Regex reg = new Regex(@"<meta\sproperty=""og:url""\scontent=""\/" + this.user + @"\/" + this.project + @"\/releases\/tag\/v([\d\.]+)""");
                var ret = reg.Match(html);
                if (ret.Success)
                {
                    latest = new Version(ret.Groups[1].Captures[0].Value);                    
                    return latest;
                }
                return null;
            }            
        }
    }
}
