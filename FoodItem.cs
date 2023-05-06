using System.Text.Json.Serialization;

namespace Pizza
{
    public class FoodItem
    {
        public int? FoodID { get; set; }
        public int? MenuCategoryID { get; set; }
        public string? FoodName { get; set; }
        public decimal? Price { get; set; }
    }
}
