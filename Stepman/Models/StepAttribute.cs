namespace Stepman.Models
{
    public class StepAttribute
    {
        public bool IsSelected { get; set; } = false;
        public bool IsManaged { get; set; }
        public string? LogicalName { get; set; }
        public string? DisplayName { get; set; }
        public string? Type { get; set; }
    }
}
