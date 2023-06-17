namespace Pizza
{
    public class CustomizeOptions
    {
        public string? OptionName { get; set; }
        public List<CustomizeItem>? OptionItems { get; set; } = new List<CustomizeItem>();
        public int? IsMultiSelect { get; set; }
        public int? OrderItemOptionID { get; set; }
    }
}
