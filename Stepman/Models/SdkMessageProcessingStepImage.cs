using System.Xml.Serialization;

namespace Stepman.Models
{
    [XmlRoot(ElementName = "SdkMessageProcessingStepImage")]
    public class SdkMessageProcessingStepImage
    {

        [XmlElement(ElementName = "SdkMessageProcessingStepImageId")]
        public Guid SdkMessageProcessingStepImageId { get; set; }

        [XmlElement(ElementName = "Attributes")]
        public string Attributes { get; set; }

        [XmlElement(ElementName = "EntityAlias")]
        public string EntityAlias { get; set; }

        [XmlElement(ElementName = "ImageType")]
        public int ImageType { get; set; }

        [XmlElement(ElementName = "MessagePropertyName")]
        public string MessagePropertyName { get; set; }

        [XmlElement(ElementName = "IsCustomizable")]
        public int IsCustomizable { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
