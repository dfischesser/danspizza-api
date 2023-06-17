namespace Pizza
{
    public class Customize
    {
        public int? FoodID { get; set; }
        public string? CustomizeOption { get; set; }
        public List<CustomizeOptionItems>? CustomizeOptions { get; set; }
    }
    public class CustomizeOptionItems
    {
        public string? OptionItem { get; set; }
        public decimal? OptionPrice { get; set; }
    }
}
