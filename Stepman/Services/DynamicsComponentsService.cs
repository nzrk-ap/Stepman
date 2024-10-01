using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static Stepman.MainWindow;

namespace Stepman.Services
{
    internal sealed class DynamicsComponentsService
    {
        private readonly IOrganizationService _organizationService;

        public DynamicsComponentsService(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        public IDictionary<Guid, string> GetStepSolutions()
        { 
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("friendlyname", "version", "solutionid"),
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) },
                Distinct = true
            };

            query.Criteria.AddCondition("uniquename", ConditionOperator.Like, "%HCMLMS%");
            var components = query.AddLink("solutioncomponent", "solutionid", "solutionid", JoinOperator.Inner);
            components.LinkCriteria.AddCondition("componenttype", ConditionOperator.In, new object[] { 92, 93 });

            var solutions = _organizationService.RetrieveMultiple(query);

            var dictionary = new Dictionary<Guid, string>();

            // Display the retrieved solutions
            foreach (Entity solution in solutions.Entities)
            {
                Console.WriteLine($"Solution ID: {solution["solutionid"]}");
                Console.WriteLine($"Friendly Name: {solution["friendlyname"]}");
                Console.WriteLine($"Version: {solution["version"]}");
                Console.WriteLine();

                // Add the solution to the dictionary
                var solutionId = (Guid) solution["solutionid"];
                var friendlyName = solution["friendlyname"].ToString() + " " + solution["version"];
                dictionary.Add(solutionId, friendlyName);
            }

            return dictionary;
        }

        public IDictionary<Guid, string> GetSolutionSteps(Guid solutionId)
        {
            var query_componenttype = 92;

            // Instantiate QueryExpression query
            var query = new QueryExpression("solutioncomponent")
            {
                TopCount = 50
            };

            // Add conditions to query.Criteria
            query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
            query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, query_componenttype);

            // Add link-entity step
            var step = query.AddLink("sdkmessageprocessingstep", "objectid", "sdkmessageprocessingstepid");
            step.EntityAlias = "step";

            // Add columns to step.Columns
            step.Columns.AddColumns("name", "sdkmessageprocessingstepid");

            var results = _organizationService.RetrieveMultiple(query);

            var dictionary = new Dictionary<Guid, string>();

            foreach (var entity in results.Entities)
            {
                var stepId = (Guid) entity.GetAttributeValue<AliasedValue>("step.sdkmessageprocessingstepid").Value;
                var stepName = entity.GetAttributeValue<AliasedValue>("step.name").Value.ToString();
                dictionary.Add(stepId, stepName);
            }

            return dictionary;
        }

        public IEnumerable<StepAttribute> GetStepAttributes(string tableName, Guid pluginStepId)
        {
            var retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = tableName
            };

            // Execute the request
            RetrieveEntityResponse retrieveEntityResponse = (RetrieveEntityResponse) _organizationService.Execute(retrieveEntityRequest);

            // Get the entity metadata
            EntityMetadata entityMetadata = retrieveEntityResponse.EntityMetadata;

            var attributes = new List<StepAttribute>();

            // Loop through the attributes and display their names
            Console.WriteLine($"Attributes for entity '{tableName}':");
            foreach (var attribute in entityMetadata.Attributes)
            {
                attributes.Add(new StepAttribute
                {
                    LogicalName = attribute.LogicalName,
                    DisplayName = attribute.DisplayName.UserLocalizedLabel.Label,
                    Type = attribute?.AttributeType?.ToString() ?? "",
                    Checked = false
                });
            }

            return attributes;
        }

        public IEnumerable<string> GetStepSelectedAttributes()
        {
            return new List<string>();
        }
    }
}
