using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UniversalOrchestratorExtensionSample.Jobs
{
    public class PropertyReader
    {
        private static readonly ILogger _logger = LogHandler.GetClassLogger<PropertyReader>();

        public static Dictionary<string, object> GetDynamicProperties()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string jsonFile = Path.Combine(assemblyFolder, "DynamicProps.json");
            string fileContent = File.ReadAllText(jsonFile);

            _logger.LogInformation($"The props file should be at {jsonFile}");
            Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);
            foreach (var kvp in result)
            {
                _logger.LogInformation($"Key: {kvp.Key}");
                _logger.LogInformation($"Value: {kvp.Value}");
            }

            return result;
        }
    }
}
