using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private Menu menuCategories = new Menu();
        private List<MenuCategoryItem> mcList = new List<MenuCategoryItem>();
        private List<FoodItem> foodItems = new List<FoodItem>();

        [HttpGet("Get")]
        public Menu Get()
        {
            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
            DataSet dsCategories = new DataSet();
            DataSet dsFood = new DataSet();

            // Load Data from DB
            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();

                // Load Menu Categories from DB
                using (SqlCommand command = new SqlCommand("dbo.GetMenuCategories", connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(dsCategories);
                    }
                }

                // Load Food Types from DB
                using (SqlCommand command = new SqlCommand("dbo.GetFood", connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(dsFood);
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
                    foodItems.Add(fi);
                }
            }

            // Compile Menu

            // Add Food List to each Menu Category Item
            foreach (MenuCategoryItem mc in mcList)
            {

                mc.FoodList = foodItems.Where(item => item.MenuCategoryID == mc.MenuCategoryID).ToList<FoodItem>();
            }

            menuCategories.MenuCategoryList = mcList;

            return menuCategories;
        }
    }
}
