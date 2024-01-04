using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccauto.Marker
{
    public class Env
    {
        private Dictionary<string, String> _envs = new Dictionary<string, String>();
        public Env()
        {
            string[] args = Environment.GetCommandLineArgs();
            // -cfg
            for (int i = 1; i < args.Length; i+=2)
            {
                if (args.Length > i+1)
                {
                    _envs.Add(args[i], args[i+1]);
                }
            }
        }

        public string getEnv(string name) { 
            if (_envs.ContainsKey(name)) { return _envs[name]; }
            return null;
        }
    }
}
