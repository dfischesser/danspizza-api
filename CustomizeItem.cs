namespace Pizza
{
    public class CustomizeItem
    {
        public int? FoodID { get; set; }
        public int? OptionID { get; set; }
        public string? CustomizeOption { get; set; }
        public int? CustomizeOptionOrder { get; set; }
        public string? CustomizeOptionItem { get; set; }
        public int? CustomizeOptionItemID { get; set; }
        public decimal? Price { get; set; }
        public bool? IsMultiSelect { get; set; }
        public bool? IsDefaultOption { get; set; }
        public int? CustomizeOptionItemOrder { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
