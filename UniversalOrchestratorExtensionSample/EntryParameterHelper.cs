using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace UniversalOrchestratorExtensionSample
{
    public class EntryParameterFile
    {
        public Dictionary<string, Dictionary<string, object>> Thumbprint2EntryParameters { get; set; } = new Dictionary<string, Dictionary<string, object>>();
    }

    public static class EntryParameterHelper
    {
        private const string _fileName = "EntryParameters.json";

        public static void AddOrUpdateRecord(string storeDirectory, string thumbprint, Dictionary<string, object> entryParameters)
        {
            EntryParameterFile entryParameterFile = GetParameterFile(storeDirectory);
            entryParameterFile.Thumbprint2EntryParameters[thumbprint] = entryParameters;
            SetParameterFile(storeDirectory, entryParameterFile);
        }

        private static string GetParameterFilePath(string storeDirectory)
        {
            return Path.Combine(storeDirectory, _fileName);
        }

        public static EntryParameterFile GetParameterFile(string storeDirectory)
        {
            CreateEntryParameterFileIfNotExists(storeDirectory);
            string fileContent = File.ReadAllText(GetParameterFilePath(storeDirectory));
            return JsonConvert.DeserializeObject<EntryParameterFile>(fileContent);
        }

        private static void CreateEntryParameterFileIfNotExists(string storeDirectory)
        {
            string fullPath = GetParameterFilePath(storeDirectory);
            if (!File.Exists(fullPath))
            {
                SetParameterFile(storeDirectory, new EntryParameterFile());
            }
        }

        private static void SetParameterFile(string storeDirectory, EntryParameterFile parameterFile)
        {
            File.WriteAllText(GetParameterFilePath(storeDirectory), JsonConvert.SerializeObject(parameterFile));
        }
    }
}
