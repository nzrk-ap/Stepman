namespace Stepman.Models
{
    public class StepAttribute
    {
        public bool IsTracked { get; set; }
        public bool IsEnabled { get; set; }
        public string? LogicalName { get; set; }
        public string? DisplayName { get; set; }
        public string? Type { get; set; }
    }
}
