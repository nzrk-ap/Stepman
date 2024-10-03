using System.IO;
using System.Xml.Serialization;
using Stepman.Models;

namespace Stepman.Services
{
    public class ExportService
    {
        public Task Export(StepData step, string path, string taskInfo)
        {
            var fileName = $"{{{step.StepId}}}.xml";

            if (!path.Contains("/SdkMessageProcessingSteps"))
            {
                path = Path.Combine(path, "SdkMessageProcessingSteps");
            }

            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
            {
                var xmlContent = File.ReadAllText(fullPath);
                var serializer = new XmlSerializer(typeof(SdkMessageProcessingStep));
                using StringReader reader = new(xmlContent);
                var instance = (SdkMessageProcessingStep)serializer.Deserialize(reader);

                foreach (var attr in step.Attributes)
                {
                    if (!instance.FilteringAttributes.Contains(attr.LogicalName))
                    {
                        instance.FilteringAttributes += (taskInfo + "=>");
                        instance.FilteringAttributes += "," + attr.LogicalName;
                        instance.FilteringAttributes += (taskInfo + "<=");
                    }
                }

                foreach (var image in step.Images)
                {
                    var target = instance.SdkMessageProcessingStepImages
                        .FirstOrDefault(img => img.SdkMessageProcessingStepImage.SdkMessageProcessingStepImageId == image.ImageId);

                    if (target is not null)
                    {
                        foreach (var attr in image.Attributes)
                        {
                            if (!target.SdkMessageProcessingStepImage.Attributes.Contains(attr.LogicalName))
                            {
                                target.SdkMessageProcessingStepImage.Attributes += (taskInfo + "=>");
                                target.SdkMessageProcessingStepImage.Attributes += "," + attr.LogicalName;
                                target.SdkMessageProcessingStepImage.Attributes += (taskInfo + "<=");
                            }
                        }
                    }
                }

                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, instance);
                    string xmlOutput = writer.ToString();
                    File.WriteAllText(fullPath, xmlOutput);
                };
            }

            return Task.CompletedTask;
        }
    }
}
