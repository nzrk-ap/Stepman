using System.IO;
using System.Xml.Serialization;
using Stepman.Models;

namespace Stepman.Services
{
    public class ExportService
    {
        public Task Export(StepData step, string path)
        {
            var fileName = $"{{{step.StepId}}}.xml";
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
            {
                var xmlContent = File.ReadAllText(fullPath);
                var serializer = new XmlSerializer(typeof(SdkMessageProcessingStep));
                using StringReader reader = new(xmlContent);
                var instance = (SdkMessageProcessingStep)serializer.Deserialize(reader);

                instance.FilteringAttributes = string.Join(",", step.Attributes.Select(a => a.LogicalName));

                foreach (var image in step.Images)
                {
                    var target = instance.SdkMessageProcessingStepImages
                        .FirstOrDefault(img => img.SdkMessageProcessingStepImage.SdkMessageProcessingStepImageId == image.ImageId);

                    if (target is not null)
                    {
                        target.SdkMessageProcessingStepImage.Attributes = string.Join(",", image.Attributes.Select(a => a.LogicalName));
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
