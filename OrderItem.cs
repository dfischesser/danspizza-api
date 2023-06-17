using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace Pizza
{
    public class OrderItem
    {
        public int? UserID { get; set; }
        public string? MenuCategory { get; set; }
        public int? MenuCategoryID { get; set; }
        public string? FoodName { get; set; }
        public int? FoodID { get; set; }
        public string? OptionName { get; set; }
        public int? OptionID { get; set; }
        public int? OrderItemID { get; set; }
        public string? OptionItem { get; set; }
        public int? OptionItemID { get; set; }
        public int? IsMultiSelect { get; set; }
        public decimal? Price { get; set; }
        public int? CartItemID { get; set; }
        [JsonIgnore]
        public int? Active { get; set; }
        public int OrderID { get; set; }
    }
}
