using System.Xml.Serialization;

namespace Stepman.Models
{
    [XmlRoot(ElementName = "SdkMessageProcessingStep")]
    public class SdkMessageProcessingStep
    {

        [XmlElement(ElementName = "SdkMessageId")]
        public string SdkMessageId { get; set; }

        [XmlElement(ElementName = "PluginTypeName")]
        public string PluginTypeName { get; set; }

        [XmlElement(ElementName = "PluginTypeId")]
        public string PluginTypeId { get; set; }

        [XmlElement(ElementName = "PrimaryEntity")]
        public string PrimaryEntity { get; set; }

        [XmlElement(ElementName = "AsyncAutoDelete")]
        public int AsyncAutoDelete { get; set; }

        [XmlElement(ElementName = "Configuration")]
        public string Configuration { get; set; }

        [XmlElement(ElementName = "FilteringAttributes")]
        public string FilteringAttributes { get; set; }

        [XmlElement(ElementName = "InvocationSource")]
        public int InvocationSource { get; set; }

        [XmlElement(ElementName = "Mode")]
        public int Mode { get; set; }

        [XmlElement(ElementName = "Rank")]
        public int Rank { get; set; }

        [XmlElement(ElementName = "EventHandlerTypeCode")]
        public int EventHandlerTypeCode { get; set; }

        [XmlElement(ElementName = "Stage")]
        public int Stage { get; set; }

        [XmlElement(ElementName = "IsCustomizable")]
        public int IsCustomizable { get; set; }

        [XmlElement(ElementName = "IsHidden")]
        public int IsHidden { get; set; }

        [XmlElement(ElementName = "SupportedDeployment")]
        public int SupportedDeployment { get; set; }

        [XmlElement(ElementName = "IntroducedVersion")]
        public double IntroducedVersion { get; set; }

        [XmlElement(ElementName = "SdkMessageProcessingStepImages")]
        public List<SdkMessageProcessingStepImages> SdkMessageProcessingStepImages { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "SdkMessageProcessingStepId")]
        public string SdkMessageProcessingStepId { get; set; }

        [XmlAttribute(AttributeName = "xsi")]
        public string Xsi { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "SdkMessageProcessingStepImages")]
    public class SdkMessageProcessingStepImages
    {

        [XmlElement(ElementName = "SdkMessageProcessingStepImage")]
        public SdkMessageProcessingStepImage SdkMessageProcessingStepImage { get; set; }
    }
}
