using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private Menu menuCategories = new Menu();
        private List<MenuCategoryItem> mcList = new List<MenuCategoryItem>();
        private List<FoodItem> foodItems = new List<FoodItem>();
        private List<CustomizeItem> customizeItems = new List<CustomizeItem>();
        SqlTools sqlTools = new SqlTools();

        [HttpGet("Get")]
        public IActionResult GetMenu()
        {
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet dsCategories = new DataSet();
                DataSet dsFood = new DataSet();
                DataSet dsCustomize = new DataSet();

                // Get Data from DB
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();

                    // Get Menu Categories from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetMenuCategories", connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsCategories);
                        }
                    }

                    // Get Food Types from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetFood", connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsFood);
                        }
                    }
                    
                    // Get Customization from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetCustomize", connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsCustomize);
                        }
                    }
                }

                // Load Menu
                // TODO: Add Error Logging for Menu

                // Load Menu Categories
                if (dsCategories.Tables.Count > 0 && dsCategories.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in dsCategories.Tables[0].Rows)
                    {
                        MenuCategoryItem mc = new MenuCategoryItem();
                        mc.MenuCategoryID = Convert.ToInt32(dr["id"]);
                        mc.FoodType = string.IsNullOrEmpty(dr["food_type"].ToString()) ? "" : dr["food_type"].ToString();
                        mcList.Add(mc);
                    }
                }

                // Load Food
                if (dsFood.Tables.Count > 0 && dsFood.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in dsFood.Tables[0].Rows)
                    {
                        FoodItem fi = new FoodItem();
                        fi.FoodID = Convert.ToInt32(dr["id"]);
                        fi.MenuCategoryID = Convert.ToInt32(dr["menu_category_id"]);
                        fi.FoodName = string.IsNullOrEmpty(dr["food"].ToString()) ? "" : dr["food"].ToString();
                        fi.Price = Convert.ToDecimal(dr["price"]);
                        fi.CustomizeOptions = new List<CustomizeOptions>();
                        foodItems.Add(fi);
                    }
                }


                // Load Customization
                if (dsCustomize.Tables.Count > 0 && dsCustomize.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in dsCustomize.Tables[0].Rows)
                    {
                        CustomizeItem c = new CustomizeItem();
                        c.FoodID = Convert.ToInt32(dr["food_id"]);
                        c.CustomizeOption = dr["customize_option"].ToString();
                        c.CustomizeOptionItem = dr["customize_option_item"].ToString();
                        c.Price = Convert.ToDecimal(dr["price"]);
                        c.IsMultiSelect = Convert.ToInt32(dr["is_multi_select"]);
                        customizeItems.Add(c);
                    }
                }

                CustomizeOptions customizeOptions = new CustomizeOptions();
                var customizeOptionsList = new List<CustomizeOptions>();

                foreach (var customizeItem in customizeItems)
                {
                    var foodItem = foodItems.Find(foodItem => foodItem.FoodID == customizeItem.FoodID);
                    var options = new CustomizeOptions();

                    if (foodItem != null)
                    {
                        options = foodItem.CustomizeOptions.Find(customizeOption => customizeOption.OptionName == customizeItem.CustomizeOption);
                        if (options != null)
                        {
                            options.OptionItems.Add(customizeItem);
                        }
                        else
                        {
                            foodItem.CustomizeOptions.Add(
                                new CustomizeOptions() { 
                                    OptionName = customizeItem.CustomizeOption, 
                                    IsMultiSelect = customizeItem.IsMultiSelect,
                                    OptionItems = new List<CustomizeItem>() { customizeItem } 
                                });
                        }
                    }
                }

                // Add Food List to each Menu Category Item
                foreach (MenuCategoryItem mc in mcList)
                {
                    mc.FoodList = foodItems.Where(item => item.MenuCategoryID == mc.MenuCategoryID).ToList<FoodItem>();
                }

                menuCategories.MenuCategoryList = mcList;

                return Ok(menuCategories);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Menu", "Error", "Error Getting Menu", ex.Message);
                return NotFound(menuCategories);
            }
        }
    }
}
