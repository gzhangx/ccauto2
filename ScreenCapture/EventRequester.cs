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

        private readonly AutoResetEvent[] monitors = new AutoResetEvent[]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false),
        };

        public class RequestAndResult
        {
            public RequestTypes RequestTypes;
            public bool inProgress;
            public byte[] result;
            public AutoResetEvent waitObj;
            public Action<byte[]> callback;
        }
        RequestAndResult[] reqestResults = new RequestAndResult[2];

        public bool doRequest(RequestTypes req, Action<byte[]> cb)
        {
            lock (syncObj)
            {
                if (reqestResults[(int)req] != null) return false;                
                reqestResults[(int)req] = new RequestAndResult
                {
                    RequestTypes = req,
                    inProgress = false,
                    waitObj= monitors[(int)req],
                    result = null,
                    callback= cb,
                };
                return true;
            }
        }

        public byte[] waitFor(RequestTypes req, int timeout = 1000)
        {
            RequestAndResult res = null;
            lock (syncObj)
            {
                res = reqestResults[(int)req];
                if (res == null) return null;
            }
            if (res.waitObj.WaitOne(timeout) && res.result != null)
            {
                reqestResults[(int)req] = null;
                return res.result;
            }
            return null;
        }

        public bool canProcessRequest()
        {
            lock(syncObj)
            {
                foreach (var req in reqestResults)
                {
                    if (req != null && req.result == null && !req.inProgress)
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
                    if (res != null)
                    {
                        res.result = buf;
                        res.waitObj.Set();
                        if (res.callback != null) res.callback(buf);
                    }
                }

            }
        }


    }
}
