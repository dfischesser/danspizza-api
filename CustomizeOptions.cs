namespace Pizza
{
    public class CustomizeOptions
    {
        public int? OptionID { get; set; }
        public string? OptionName { get; set; }
        public List<CustomizeItem>? OptionItems { get; set; } = new List<CustomizeItem>();
        public bool? IsMultiSelect { get; set; }
        public bool? IsDefaultOption { get; set; }
        public int? OrderItemOptionID { get; set; }
        public int? OptionOrder { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
