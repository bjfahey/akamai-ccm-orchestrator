// Copyright 2026 Keyfactor
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Text;
using Keyfactor.Orchestrators.Extensions;

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Jobs
{
    public class CcmJob
    {
        protected long JobHistoryId { get; set; }

        protected JobResult Success(string message = "")
        {
            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                JobHistoryId = JobHistoryId,
                FailureMessage = message
            };
        }

        protected JobResult Failure(string message)
        {
            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                JobHistoryId = JobHistoryId,
                FailureMessage = message
            };
        }

        protected JobResult Warning(string message)
        {
            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Warning,
                JobHistoryId = JobHistoryId,
                FailureMessage = message
            };
        }

        protected string FlattenException(Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine(e.Message);
            if (e.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.AppendLine(FlattenException(e.InnerException));
            }
            sb.AppendLine(e.StackTrace);
            return sb.ToString();
        }
    }
}
