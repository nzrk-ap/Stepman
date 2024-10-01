namespace Stepman.Models
{
    public class ImageData
    {
        public Guid ImageId { get; set; }

        public List<StepAttribute> Attributes { get; set; } = new List<StepAttribute>();
    }
}
