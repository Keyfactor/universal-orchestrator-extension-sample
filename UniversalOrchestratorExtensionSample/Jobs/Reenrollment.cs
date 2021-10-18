using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.PKI.X509;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace UniversalOrchestratorExtensionSample.Jobs
{
    public class Reenrollment : IReenrollmentJobExtension
    {
        private readonly ILogger _logger = LogHandler.GetClassLogger<Reenrollment>();
        public string ExtensionName => Constants.ExtensionName;

        public JobResult ProcessJob(ReenrollmentJobConfiguration jobConfiguration, SubmitReenrollmentCSR submitReenrollmentUpdate)
        {
            _logger.LogInformation("Performing Reenrollment.");

            _logger.LogInformation("Begin writing properties.");
            foreach (var property in jobConfiguration.JobProperties)
            {
                _logger.LogInformation($"Key: {property.Key}");
                _logger.LogInformation($"Value: {property.Value}");
                _logger.LogInformation("");
            }
            _logger.LogInformation("End writing properties.");

            string subjectText;
            object subjectTextObject;
            if (!jobConfiguration.JobProperties.TryGetValue("StringEntryParam", out subjectTextObject))
            {
                _logger.LogInformation("StringEntryParam property not found. Falling back to default subject text.");
                subjectTextObject = "CN=Name";
            }
            if (jobConfiguration.JobProperties.ContainsKey("subjectText"))
            {
                subjectText = Convert.ToString(jobConfiguration.JobProperties["subjectText"]);
            }
            else
            {
                subjectText = Convert.ToString(subjectTextObject);
            }
            _logger.LogInformation($"Subject Text: {subjectText}");

            string csrText = string.Empty;
            using (RequestGenerator generator = new RequestGenerator("RSA", 2048))
            {
                generator.Subject = subjectText;
                csrText = Convert.ToBase64String(generator.CreatePKCS10Request());
            }

            X509Certificate2 cert = submitReenrollmentUpdate.Invoke(csrText);

            if (cert != null)
            {
                string alias = jobConfiguration.JobProperties.ContainsKey("Alias") ? Convert.ToString(jobConfiguration.JobProperties["Alias"]) : cert.Thumbprint;
                string filePath = Path.Combine(jobConfiguration.CertificateStoreDetails.StorePath, alias + ".pem");
                AddCert(cert, filePath);
                EntryParameterHelper.AddOrUpdateRecord(jobConfiguration.CertificateStoreDetails.StorePath, alias, jobConfiguration.JobProperties);
            }
            return new JobResult
            {
                JobHistoryId = jobConfiguration.JobHistoryId,
                Result = Keyfactor.Orchestrators.Common.Enums.OrchestratorJobStatusJobResult.Success,
            };
        }

        private void AddCert(X509Certificate2 mgmtCert, string fullPath)
        {
            _logger.MethodEntry();

            string pemString = GetPemString(mgmtCert);
            File.WriteAllText(fullPath, pemString);

            _logger.MethodExit();
        }

        private string GetPemString(X509Certificate2 mgmtCert)
        {
            _logger.MethodEntry();
            string pemString = CertificateConverterFactory.FromX509Certificate2(mgmtCert).ToPEM(includeHeaders: true);

            _logger.MethodExit();
            return pemString;
        }
    }
}
