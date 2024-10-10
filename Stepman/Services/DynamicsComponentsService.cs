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
using Stepman.Models;
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
                var solutionId = (Guid)solution["solutionid"];
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
                var stepId = (Guid)entity.GetAttributeValue<AliasedValue>("step.sdkmessageprocessingstepid").Value;
                var stepName = entity.GetAttributeValue<AliasedValue>("step.name").Value.ToString();
                dictionary.Add(stepId, stepName);
            }

            return dictionary;
        }

        public IDictionary<Guid, string> GetStepsImages(Guid stepId)
        {
            var query = new QueryExpression("sdkmessageprocessingstepimage");
            query.ColumnSet.AddColumn("name");
            var link = query.AddLink("sdkmessageprocessingstep", 
                "sdkmessageprocessingstepid",
                "sdkmessageprocessingstepid", JoinOperator.Inner);

            link.LinkCriteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId);

            var result = _organizationService.RetrieveMultiple(query);
            var dictionary = new Dictionary<Guid, string>();

            foreach (var entity in result.Entities)
            {
                var imageId = entity.GetAttributeValue<Guid>("sdkmessageprocessingstepimageid");
                var imageName = entity.GetAttributeValue<string>("name");
                dictionary.Add(imageId, imageName);
            }

            return dictionary;
        }

        public IEnumerable<StepAttribute> GetStepAttributes(Guid pluginStepId)
        {
            var tableName = GetStepTergetTableName(pluginStepId);
            var selectedAttributes = GetSelectedAttributes(pluginStepId);
            var retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = tableName
            };

            // Execute the request
            RetrieveEntityResponse retrieveEntityResponse = (RetrieveEntityResponse)_organizationService.Execute(retrieveEntityRequest);

            // Get the entity metadata
            EntityMetadata entityMetadata = retrieveEntityResponse.EntityMetadata;

            var attributes = new List<StepAttribute>();

            // Loop through the attributes and display their names
            Console.WriteLine($"Attributes for entity '{tableName}':");
            foreach (var attribute in entityMetadata.Attributes)
            {
                if (attribute.DisplayName.UserLocalizedLabel is null)
                    continue;

                attributes.Add(new StepAttribute
                {
                    LogicalName = attribute.LogicalName,
                    DisplayName = attribute.DisplayName.UserLocalizedLabel?.Label,
                    Type = attribute?.AttributeType?.ToString() ?? "",
                    IsTracked = selectedAttributes.Contains(attribute.LogicalName),
                    IsEnabled = !selectedAttributes.Contains(attribute.LogicalName)
                });
            }

            return attributes;
        }

        public IEnumerable<StepAttribute> GetImageAttributes(Guid imageId, Guid pluginStepId)
        {
            var tableName = GetStepTergetTableName(pluginStepId);
            var selectedAttributes = GetSelectedImageAttributes(imageId);
            var retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = tableName
            };

            // Execute the request
            var retrieveEntityResponse = (RetrieveEntityResponse)_organizationService.Execute(retrieveEntityRequest);

            // Get the entity metadata
            var entityMetadata = retrieveEntityResponse.EntityMetadata;

            var attributes = new List<StepAttribute>();

            foreach (var attribute in entityMetadata.Attributes)
            {
                if (attribute.DisplayName.UserLocalizedLabel is null)
                    continue;

                attributes.Add(new StepAttribute
                {
                    LogicalName = attribute.LogicalName,
                    DisplayName = attribute.DisplayName.UserLocalizedLabel?.Label,
                    Type = attribute?.AttributeType?.ToString() ?? "",
                    IsTracked = selectedAttributes.Contains(attribute.LogicalName),
                    IsEnabled = !selectedAttributes.Contains(attribute.LogicalName)
                });
            }

            return attributes;
        }

        public string GetSolutionLogicalName(Guid solutionId)
        {
            var result = _organizationService.Retrieve("solution", solutionId, new ColumnSet("uniquename"));
            return result.GetAttributeValue<string>("uniquename");
        }

        private string GetStepTergetTableName(Guid stepId)
        {
            var query = new QueryExpression("sdkmessagefilter");
            query.ColumnSet.AddColumn("primaryobjecttypecode");
            var link = query.AddLink("sdkmessageprocessingstep", "sdkmessagefilterid", "sdkmessagefilterid");
            link.LinkCriteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId);
            var result = _organizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
            return result.GetAttributeValue<string>("primaryobjecttypecode");
        }

        private IEnumerable<string> GetSelectedAttributes(Guid stepId)
        {
            var query = new QueryExpression("sdkmessageprocessingstep");
            query.ColumnSet.AddColumn("filteringattributes");
            query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId);
            var result = _organizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
            var filteringattributes = result.GetAttributeValue<string>("filteringattributes");
            if (filteringattributes is null)
                return new string[0];

            return filteringattributes.Split(',');
        }

        private IEnumerable<string> GetSelectedImageAttributes(Guid imageId)
        {
            var query = new QueryExpression("sdkmessageprocessingstepimage");
            query.ColumnSet.AddColumn("attributes");
            query.Criteria.AddCondition("sdkmessageprocessingstepimageid", ConditionOperator.Equal, imageId);
            var result = _organizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
            var attributes = result.GetAttributeValue<string>("attributes");
            if (attributes is null)
                return new string[0];
            return attributes.Split(',');
        }
    }
}
