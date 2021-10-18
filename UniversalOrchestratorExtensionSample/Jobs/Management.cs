using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.PKI.X509;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace UniversalOrchestratorExtensionSample.Jobs
{
    public class Management : IManagementJobExtension
    {
        public string ExtensionName => Constants.ExtensionName;
        private readonly ILogger _logger = LogHandler.GetClassLogger<Management>();

        public JobResult ProcessJob(ManagementJobConfiguration jobConfiguration)
        {
            try
            {
                _logger.MethodEntry();

                CertificateStore store = jobConfiguration.CertificateStoreDetails;
                ManagementJobCertificate mgmtCert = jobConfiguration.JobCertificate;

                _logger.LogTrace($"Thumbprint: {mgmtCert.Thumbprint}");
                _logger.LogTrace($"Alias: {mgmtCert.Alias}");

                _logger.LogTrace("Begin JobProperties");
                if (jobConfiguration.JobProperties != null)
                {
                    foreach (var kvp in jobConfiguration.JobProperties)
                    {
                        _logger.LogTrace($"{kvp.Key}: {kvp.Value}");
                    }
                }
                _logger.LogTrace("End JobProperties");

                string storePath = store.StorePath;

                switch (jobConfiguration.OperationType)
                {
                    case CertStoreOperationType.Add:

                        string alias = GetThumbprint(mgmtCert);
                        string filePath = GetFullFilePath(storePath, alias);
                        AddCert(mgmtCert, filePath);
                        EntryParameterHelper.AddOrUpdateRecord(storePath, alias, jobConfiguration.JobProperties);
                        break;
                    case CertStoreOperationType.Remove:
                        string removeAlias = string.IsNullOrWhiteSpace(mgmtCert.Alias) ? mgmtCert.Thumbprint : mgmtCert.Alias;
                        RemoveCert(GetFullFilePath(storePath, removeAlias));
                        break;
                    default:
                        string errorMessage = $"Unknown Type: {jobConfiguration.OperationType}";
                        _logger.LogError(errorMessage);
                        throw new Exception(errorMessage);
                }

                _logger.MethodEntry();
                return new JobResult()
                {
                    Result = OrchestratorJobStatusJobResult.Success
                };
            }
            catch (Exception e)
            {
                string errorMessage = LogHandler.FlattenException(e);
                _logger.LogError(errorMessage);
                throw;
            }
        }

        private string GetThumbprint(ManagementJobCertificate mgmtCert)
        {
            if (!string.IsNullOrWhiteSpace(mgmtCert.Alias))
            {
                return mgmtCert.Alias;
            }

            X509Certificate2 cert = GetX509(mgmtCert);
            string thumbprint = cert.Thumbprint;

            _logger.LogInformation($"The thumbprint is {thumbprint}");
            return thumbprint;
        }

        private X509Certificate2 GetX509(ManagementJobCertificate mgmtCert)
        {
            _logger.LogInformation($"Content: {mgmtCert.Contents}");
            _logger.LogInformation($"Password: {mgmtCert.PrivateKeyPassword}");

            string password = mgmtCert.PrivateKeyPassword;
            return new X509Certificate2(Convert.FromBase64String(mgmtCert.Contents), password, X509KeyStorageFlags.Exportable);
        }

        private string GetPemString(ManagementJobCertificate mgmtCert)
        {
            _logger.MethodEntry();
            _logger.LogInformation($"Content: {mgmtCert.Contents}");
            _logger.LogInformation($"Password: {mgmtCert.PrivateKeyPassword}");

            string password = mgmtCert.PrivateKeyPassword;
            X509Certificate2 cert = GetX509(mgmtCert);
            string pemString = CertificateConverterFactory.FromX509Certificate2(cert, password).ToPEM(includeHeaders: true, password: password);

            _logger.MethodExit();
            return pemString;
        }

        private string GetFullFilePath(string storePath, string fileName)
        {
            return Path.Combine(storePath, fileName.EnsureEndsWith(".pem"));
        }

        private void AddCert(ManagementJobCertificate mgmtCert, string fullPath)
        {
            _logger.MethodEntry();

            string pemString = GetPemString(mgmtCert);
            File.WriteAllText(fullPath, pemString);

            _logger.MethodExit();
        }

        private void RemoveCert(string fullPath)
        {
            _logger.MethodEntry();

            File.Delete(fullPath);

            _logger.MethodExit();
        }
    }
}
