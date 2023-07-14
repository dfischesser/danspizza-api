using System.Text.Json.Serialization;

namespace Pizza
{
    public class FoodItem
    {
        public int? FoodID { get; set; }
        public int? MenuCategoryID { get; set; }
        public string? FoodName { get; set; }
        public decimal? Price { get; set; }
        public int? FoodOrder { get; set; }
        public List<CustomizeOptions>? CustomizeOptions { get; set; } = new List<CustomizeOptions>();
        public int? OrderItemID { get; set; }
        public DateTime? CreatedOn;
        public DateTime? ModifiedOn;
    }
}
