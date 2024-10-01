namespace Stepman.Models
{
    public class StepData
    {
        public Guid StepId { get; set; }

        public List<StepAttribute> Attributes { get; set; } = new List<StepAttribute>();

        public List<ImageData> Images { get; set; } = new List<ImageData>();
    }
}
