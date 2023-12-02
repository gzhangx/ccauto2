using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ccAuto2
{
    internal class EventRequester
    {
        public enum RequestTypes
        {
            SamOneImage = 0,
            GameProcessing = 1,
        }
        private readonly object syncObj = new object();
        

        public class RequestAndResult
        {
            public string name;
            public bool pendingRequest;
            public bool inProgress { get; internal set; }
            public byte[] result;
            public AutoResetEvent waitObj = new AutoResetEvent(false);
            public Action<byte[]> callback;

            public bool doRequest(Action<byte[]> callback)
            {
                if (inProgress) return false;
                pendingRequest= true;
                this.callback = callback;
                this.result= null;
                return true;
            }
            public byte[] waitFor(int timeout = 1000)
            {
                if (waitObj.WaitOne(timeout) && result != null)
                {
                    return result;
                }
                return null;
            }
        }
        private List<RequestAndResult> reqestResults = new List<RequestAndResult>();

        public RequestAndResult registerNewEvent(string name)
        {
            lock (syncObj)
            {                
                var req = new RequestAndResult
                {
                    name = name,
                    pendingRequest = false,
                    inProgress = false,
                    result = null,
                    callback = null,
                };
                reqestResults.Add(req);
                return req;
            }
        }

        

        public bool canProcessRequest()
        {
            lock(syncObj)
            {
                foreach (var req in reqestResults)
                {
                    if (req.pendingRequest && !req.inProgress)
                    {
                        req.inProgress = true;
                        return true;
                    }
                }
                return false;
            }
        }
        public void processRequest(byte[] buf)
        {
            lock (syncObj)
            {
                foreach (var res in reqestResults)
                {
                    if (res.pendingRequest)
                    {
                        res.inProgress = false;
                        res.inProgress = false;
                        res.result = buf;
                        res.waitObj.Set();
                        if (res.callback != null) res.callback(buf);
                    }
                }

            }
        }


    }
}
