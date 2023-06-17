using System.Text.Json.Serialization;

namespace Pizza
{
    public class Food
    {
        public int? FoodID { get; set; }
        public int? MenuCategoryID { get; set; }
        public string? FoodName { get; set; }
        public decimal? Price { get; set; }
        public List<CustomizeOptions>? CustomizeOptions { get; set; }
        [JsonIgnore]
        public int? CartItemID { get; set; }
        [JsonIgnore]
        public int? Active { get; set; }
        [JsonIgnore]
        public int? OrderID { get; set; }
    }
}
