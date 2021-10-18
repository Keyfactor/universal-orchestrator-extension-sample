using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;

namespace UniversalOrchestratorExtensionSample.Jobs
{
    public class Inventory : IInventoryJobExtension
    {
        public string ExtensionName => Constants.ExtensionName;
        private readonly ILogger _logger = LogHandler.GetClassLogger<Inventory>();

        public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
        {
            _logger.MethodEntry();

            List<CurrentInventoryItem> results = new List<CurrentInventoryItem>();
            Dictionary<string, X509Certificate2> certs = Helpers.ReadFiles(jobConfiguration.CertificateStoreDetails.StorePath, "*.cer", "*.pem");
            _logger.LogInformation($"Found {certs.Count()} certificates in {jobConfiguration.CertificateStoreDetails.StorePath}");

            Dictionary<string, object> returnProperties = jobConfiguration.JobProperties;
            try
            {
                returnProperties = PropertyReader.GetDynamicProperties();
            }
            catch
            {
                _logger.LogWarning("Failed to get dynamic properties. Falling back to job properties.");
            }

            // Get all certs in the path. Consider them new for now. The status will be updated later.
            Dictionary<string, CurrentInventoryItem> thumbprint2Item = new Dictionary<string, CurrentInventoryItem>();
            EntryParameterFile entryParameterFile = EntryParameterHelper.GetParameterFile(jobConfiguration.CertificateStoreDetails.StorePath);
            foreach (KeyValuePair<string, X509Certificate2> cert in certs)
            {
                _logger.LogInformation($"Processing certificate with thumbprint {cert.Value.Thumbprint}.");
                _logger.LogInformation($"Certificate's HasPrivateKey Value: {cert.Value.HasPrivateKey}.");
                if (thumbprint2Item.ContainsKey(cert.Key))
                {
                    _logger.LogWarning($"Certificate with thumbprint {cert.Value.Thumbprint} has already been processed.");
                    continue;
                }

                Dictionary<string, object> entryParameters = new Dictionary<string, object>();
                if (entryParameterFile != null && entryParameterFile.Thumbprint2EntryParameters.TryGetValue(cert.Key, out entryParameters))
                {
                    _logger.LogInformation("Logging entry parameters.");
                    foreach (var kvp in entryParameters)
                    {
                        _logger.LogInformation($"{kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No entry parameter file or no entry parameters for {cert.Key}");
                }

                _logger.LogWarning($"Unknown Status Test");
                CurrentInventoryItem item = new CurrentInventoryItem
                {
                    Alias = cert.Key,
                    ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                    PrivateKeyEntry = cert.Value.HasPrivateKey,
                    Certificates = new List<string> { Convert.ToBase64String(cert.Value.GetRawCertData()) },
                    Parameters = entryParameters,
                };

                thumbprint2Item.Add(cert.Key, item);
                results.Add(item);
            }

            foreach (PreviousInventoryItem previousItem in jobConfiguration.LastInventory)
            {
                if (thumbprint2Item.TryGetValue(previousItem.Alias, out CurrentInventoryItem newItem))
                {
                    _logger.LogInformation($"Certificate with thumbprint {previousItem.Alias} was found in the previous inventory and current. Setting as Unchanged.");
                    newItem.ItemStatus = OrchestratorInventoryItemStatus.Unchanged;
                }
                else
                {
                    _logger.LogInformation($"Certificate with thumbprint {previousItem.Alias} was found in the previous inventory and NOT current. Setting as Deleted.");
                    CurrentInventoryItem deletion = new CurrentInventoryItem
                    {
                        Alias = previousItem.Alias,
                        // Without this, an error occurs due to it being null.
                        Certificates = new List<string>(),
                        ItemStatus = OrchestratorInventoryItemStatus.Deleted,
                    };
                    results.Add(deletion);
                }
            }

            _logger.LogInformation($"Calling Invoke with {results.Count} items.");
            bool invokeResult = submitInventoryUpdate.Invoke(results);

            _logger.MethodExit();
            return new JobResult
            {
                JobHistoryId = jobConfiguration.JobHistoryId,
                Result = invokeResult == true ? OrchestratorJobStatusJobResult.Success : OrchestratorJobStatusJobResult.Failure
            };
        }
    }

    public static class Helpers
    {
        public static Dictionary<string, X509Certificate2> ReadFiles(string directory, params string[] searchPatterns)
        {
            Dictionary<string, X509Certificate2> result = new Dictionary<string, X509Certificate2>();
            List<string> filePaths = new List<string>();
            foreach (string searchPattern in searchPatterns)
            {
                filePaths.AddRange(Directory.GetFiles(directory, searchPattern));
            }

            foreach (string filePath in filePaths)
            {
                try
                {
                    string filename = filePath.Substring(directory.Length + 1);
                    string alias = filename.Substring(0, filename.Length - 4);
                    result.Add(alias, new X509Certificate2(filePath));
                }
                catch { }
            }

            return result;
        }
    }
}
