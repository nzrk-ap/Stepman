using System.IO;
using System.Text;
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
                var filteringAttributes = GetTagValue(xmlContent, "FilteringAttributes");
                var imagesAttributes = GetImageAttributes(xmlContent);

                EraseTagValues(ref xmlContent, "FilteringAttributes");
                EraseTagValues(ref xmlContent, "Attributes");

                var serializer = new XmlSerializer(typeof(SdkMessageProcessingStep));
                using StringReader reader = new(xmlContent);
                var obj = serializer.Deserialize(reader);
                var instance = obj as SdkMessageProcessingStep;
                instance.FilteringAttributes = filteringAttributes;

                foreach (var image in instance.SdkMessageProcessingStepImages)
                {
                    var source = imagesAttributes
                        .FirstOrDefault(im => im.Key == image.SdkMessageProcessingStepImage.SdkMessageProcessingStepImageId);
                    image.SdkMessageProcessingStepImage.Attributes = source.Value;
                }

                foreach (var attr in step.Attributes)
                {
                    if (!instance.FilteringAttributes.Contains(attr.LogicalName))
                    {
                        instance.FilteringAttributes = instance.FilteringAttributes.TrimEnd(',');
                        instance.FilteringAttributes += $"<!--{taskInfo}-->";
                        instance.FilteringAttributes += "," + attr.LogicalName;
                        instance.FilteringAttributes += $"<!--{taskInfo}-->";
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
                                target.SdkMessageProcessingStepImage.Attributes = target.SdkMessageProcessingStepImage.Attributes.TrimEnd(',');
                                target.SdkMessageProcessingStepImage.Attributes += $"<!--{taskInfo}-->";
                                target.SdkMessageProcessingStepImage.Attributes += "," + attr.LogicalName;
                                target.SdkMessageProcessingStepImage.Attributes += $"<!--{taskInfo}-->";
                            }
                        }
                    }
                }

                using (var writer = new Utf8StringWriter())
                {
                    serializer.Serialize(writer, instance);
                    string xmlOutput = writer.ToString();
                    xmlOutput = xmlOutput.Replace("&lt;", "<").Replace("&gt;", ">");
                    File.WriteAllText(fullPath, xmlOutput);
                };
            }

            return Task.CompletedTask;
        }

        private static IDictionary<Guid, string> GetImageAttributes(string xmlContent)
        {
            var imagesText = GetTagValue(xmlContent, "SdkMessageProcessingStepImages");
            var imagesArray = imagesText.Split(new string[] { "<SdkMessageProcessingStepImage>" }, StringSplitOptions.None);
            var dictionary = new Dictionary<Guid, string>();
            foreach (var image in imagesArray)
            {
                var imageIdStr = GetTagValue(image, "SdkMessageProcessingStepImageId");
                var imageId = Guid.Parse(imageIdStr);
                var attributesText = GetTagValue(image, "Attributes");
                dictionary.Add(imageId, attributesText);
            }

            return dictionary;
        }

        private static string GetTagValue(string xmlContent, string tagName)
        {
            var begin = xmlContent.IndexOf($"<{tagName}>") + $"<{tagName}>".Length;
            var end = xmlContent.IndexOf($"</{tagName}>");
            if (begin < 0 || end < 0)
                return string.Empty;

            return xmlContent.Substring(begin, end - begin);
        }

        private static void EraseTagValues(ref string xmlContent, string tagName)
        {
            while (true)
            {
                var begin = xmlContent.IndexOf($"<{tagName}>") + $"<{tagName}>".Length;
                var end = xmlContent.IndexOf($"</{tagName}>");
                if ((end - begin) == 0)
                    break;

                xmlContent = xmlContent.Remove(begin, end - begin);
            }
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }
}
