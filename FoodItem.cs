using System.Text.Json.Serialization;

namespace Pizza
{
    public class FoodItem
    {
        public int? FoodID { get; set; }

        [JsonIgnore]
        public int? MenuCategoryID { get; set; }
        public string? FoodName { get; set; }
    }
}
