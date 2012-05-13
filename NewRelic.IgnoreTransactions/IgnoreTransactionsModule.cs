using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace NewRelic.IgnoreTransactions
{
    public class IgnoreTransactionsModule : IHttpModule
    {
        private HttpApplication _context;
        private IList<string> _urls = null;

        public void Init(HttpApplication context)
        {
            _context = context;
            _context.BeginRequest += BeginRequest;
        }

        IList<string> GetUrls(HttpRequest request)
        {
            if (_urls != null) return _urls;

            var urls = new List<string>();

            var path = _context.Server.MapPath(@"~\NewRelic.IgnoreTransactions.txt");
            if (File.Exists(path))
            using (var reader = File.OpenText(path))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    if (string.IsNullOrEmpty(line) || string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("#"))
                    {
                        // ignore
                    }
                    else
                    {
                        urls.Add(line.Trim());
                    }
                    line = reader.ReadLine();
                }
            }

            _urls = urls;
            return _urls;
        }

        void BeginRequest(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            var request = application.Context.Request;
            var requestUrl = request.Url.ToString();
            foreach (var url in GetUrls(request))
            {
                if (requestUrl.IndexOf(url, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    Api.Agent.NewRelic.IgnoreTransaction();
                    return;
                }
            }
        }

        public void Dispose()
        {
            _context.BeginRequest -= BeginRequest;
            _context = null;
        }
    }
}
