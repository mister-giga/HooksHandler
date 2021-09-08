using HooksHandler.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using F = System.IO.File;

namespace HooksHandler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HandleController : ControllerBase
    {
        private readonly ILogger<HandleController> _logger;

        public HandleController(ILogger<HandleController> logger)
        {
            _logger = logger;
        }

        [Route("{target}")]
        public async Task<IActionResult> Push(string target)
        {
            _logger.LogInformation($"pushed {target}");

            var file = Path.Combine(EnvVariables.HandlersPath, target);

            if (!F.Exists(file))
            {
                _logger.LogError($"{file} not found");
                return NotFound();
            }


            var processInfo = new ProcessStartInfo()
            {
                FileName = "sh",
                Arguments = file
            };

            if (HttpContext.Request.Body != null)
            {
                JToken jObject = null;
                using (var stringReader = new StreamReader(HttpContext.Request.Body))
                {
                    var json = await stringReader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogInformation(json);
                        jObject = JToken.Parse(json);
                    }
                }

                if (jObject != null)
                {
                    foreach (var item in JsonHelper.DeserializeAndFlatten(jObject))
                        processInfo.Environment[$"body_{item.Key}"] = item.Value;
                }
            }

            foreach (var item in Request.Headers)
                processInfo.Environment[$"header_{item.Key}"] = item.Value;

            foreach (var item in Request.Query)
                processInfo.Environment[$"query_{item.Key}"] = item.Value;

            var process = Process.Start(processInfo);
            process.WaitForExit();

            return Ok();
        }

        class JsonHelper
        {
            public static Dictionary<string, string> DeserializeAndFlatten(JToken token)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                FillDictionaryFromJToken(dict, token, "");
                return dict;
            }

            private static void FillDictionaryFromJToken(Dictionary<string, string> dict, JToken token, string prefix)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        foreach (JProperty prop in token.Children<JProperty>())
                        {
                            FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                        }
                        break;

                    case JTokenType.Array:
                        int index = 0;
                        foreach (JToken value in token.Children())
                        {
                            FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                            index++;
                        }
                        break;

                    default:
                        dict.Add(prefix, ((JValue)token).Value?.ToString() ?? "");
                        break;
                }
            }

            private static string Join(string prefix, string name)
            {
                return (string.IsNullOrEmpty(prefix) ? name : prefix + "_" + name);
            }
        }
    }
}
