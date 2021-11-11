using System.Collections.Generic;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;

namespace UniversalOrchestratorExtensionSample.Jobs
{
    public class Inventory : IInventoryJobExtension
    {
        public string ExtensionName => "SAMPLETYPE";
        private readonly ILogger _logger = LogHandler.GetClassLogger<Inventory>();

        public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
        {
            _logger.MethodEntry();

            _logger.LogInformation($"Calling Invoke with 0 items.");
            bool invokeResult = submitInventoryUpdate.Invoke(new List<CurrentInventoryItem>());

            _logger.MethodExit();
            return new JobResult
            {
                JobHistoryId = jobConfiguration.JobHistoryId,
                Result = invokeResult == true ? OrchestratorJobStatusJobResult.Success : OrchestratorJobStatusJobResult.Failure
            };
        }
    }   
}
