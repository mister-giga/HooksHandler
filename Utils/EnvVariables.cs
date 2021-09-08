using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HooksHandler.Utils
{
    public static class EnvVariables
    {
        public static string HandlersPath => Environment.GetEnvironmentVariable("HANDLERS_PATH") ?? "/root";
    }
}
