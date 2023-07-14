using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;
using CsvHelper;
using System.Globalization;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EditMenuController : ControllerBase
    {
        SqlTools sqlTools = new SqlTools();

        [HttpGet]
        [Route("LoadDB")]
        public IActionResult LoadDB()
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            List<FoodItem> foodItems = new List<FoodItem>();
            List<CustomizeOptions> customizeOptions = new List<CustomizeOptions>();
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet dsCategories = new DataSet();
                DataSet dsFood = new DataSet();
                DataSet dsCustomize = new DataSet();
                List<LoadDB> rec = new List<LoadDB>();

                //Read csv file
                using (var reader = new StreamReader("batch.csv"))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<LoadDB>();
                    rec = records.ToList();
                }

                //string constring = "Data Source=FUSER;Initial Catalog=PizzaLoad;User ID=fuser;Password=goverbose;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();

                    foreach (var record in rec)
                    {
                        if (!String.IsNullOrWhiteSpace(record.food_type))
                        {
                            using (SqlCommand command = new SqlCommand("dbo.LoadMenu", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                SqlParameter param = new SqlParameter();
                                param.ParameterName = "@food_type";
                                param.Value = record.food_type;
                                param.DbType = DbType.String;
                                command.Parameters.Add(param);
                                // TODO: Add Error Logging for Users
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    int foodIndex = 1;
                    int foodOrder = 0;
                    foreach (var record in rec)
                    {
                        if (!String.IsNullOrWhiteSpace(record.menu_category_id))
                        {
                            if (Convert.ToInt32(record.menu_category_id) == foodIndex)
                            {
                                foodOrder++;
                            }
                            else
                            {
                                foodOrder = 1;
                                foodIndex++;
                            }
                            using (SqlCommand command = new SqlCommand("dbo.LoadFood", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                SqlParameter param = new SqlParameter();
                                param.ParameterName = "@food";
                                param.Value = record.food;
                                param.DbType = DbType.String;
                                command.Parameters.Add(param);

                                param = new SqlParameter();
                                param.ParameterName = "@menu_category_id";
                                param.Value = Convert.ToInt32(record.menu_category_id);
                                param.DbType = DbType.Int32;
                                command.Parameters.Add(param);

                                param = new SqlParameter();
                                param.ParameterName = "@food_order";
                                param.Value = foodOrder;
                                param.DbType = DbType.Int32;
                                command.Parameters.Add(param);

                                // TODO: Add Error Logging for Users
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    int optionOrder = 1;
                    int optionItemOrder = 1;
                    string currentOption = "";
                    var options = new List<string>();
                    foreach (var record in rec)
                    {
                        if (!String.IsNullOrWhiteSpace(record.customize_option))
                        {
                            if (currentOption != record.customize_option)
                            {
                                optionItemOrder = 0;
                            }
                            optionItemOrder++;
                            if (!options.Contains(record.customize_option))
                            {
                                options.Add(record.customize_option);

                                using (SqlCommand command = new SqlCommand("dbo.LoadOption", connection))
                                {
                                    command.CommandType = CommandType.StoredProcedure;

                                    SqlParameter param = new SqlParameter();
                                    param.ParameterName = "@customize_option";
                                    param.Value = record.customize_option;
                                    param.DbType = DbType.String;
                                    command.Parameters.Add(param);

                                    param = new SqlParameter();
                                    param.ParameterName = "@option_order";
                                    param.Value = optionOrder;
                                    param.DbType = DbType.Int32;
                                    command.Parameters.Add(param);

                                    param = new SqlParameter();
                                    param.ParameterName = "@is_multi_select";
                                    param.Value = Convert.ToInt32(record.is_multi_select);
                                    param.DbType = DbType.Int32;
                                    command.Parameters.Add(param);

                                    param = new SqlParameter();
                                    param.ParameterName = "@is_default_option";
                                    param.Value = Convert.ToInt32(record.is_default_option);
                                    param.DbType = DbType.Int32;
                                    command.Parameters.Add(param);

                                    // TODO: Add Error Logging for Users
                                    command.ExecuteNonQuery();
                                }
                                optionOrder++;
                            }
                            currentOption = record.customize_option;
                        }

                        using (SqlCommand command = new SqlCommand("dbo.LoadOptionItem", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;

                            var param = new SqlParameter();
                            param.ParameterName = "@food_id";
                            param.Value = Convert.ToInt32(record.food_id);
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@customize_option_id";
                            param.Value = options.IndexOf(record.customize_option) + 1;
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@customize_option_item_order";
                            param.Value = optionItemOrder;
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@customize_option_item";
                            param.Value = record.customize_option_item;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@price";
                            param.Value = record.price;
                            param.DbType = DbType.Decimal;
                            command.Parameters.Add(param);

                            // TODO: Add Error Logging for Users
                            command.ExecuteNonQuery();
                        }
                    }
                }





                    //    FoodItem foodItem = new FoodItem();

                    //    if (dsFood.Tables.Count > 0 && dsFood.Tables[0].Rows.Count > 0)
                    //    {
                    //        foreach (DataRow foodRow in dsFood.Tables[0].Rows)
                    //        {
                    //            foodItem = new FoodItem();
                    //            foodItem.FoodID = Convert.ToInt32(foodRow["id"]);
                    //            foodItem.Price = Convert.ToDecimal(foodRow["price"]);
                    //            foodItem.MenuCategoryID = Convert.ToInt32(foodRow["menu_category_id"]);
                    //            foodItem.FoodName = foodRow["food"].ToString();
                    //            foodItem.FoodOrder = Convert.ToInt32(foodRow["food_order"]);
                    //            foodItem.CreatedOn = Convert.ToDateTime(foodRow["created_on"]);
                    //            foodItem.ModifiedOn = Convert.ToDateTime(foodRow["modified_on"]);
                    //            foodItems.Add(foodItem);
                    //        }
                    //    }


                    //    //Get Customization from DB
                    //    using (SqlCommand command = new SqlCommand("dbo.GetCustomizeOptions", connection))
                    //    {
                    //        using (SqlDataAdapter da = new SqlDataAdapter())
                    //        {
                    //            da.SelectCommand = command;
                    //            da.Fill(dsCustomize);
                    //        }
                    //    }

                    //    CustomizeOptions customizeOption = new CustomizeOptions();

                    //    if (dsCustomize.Tables.Count > 0 && dsCustomize.Tables[0].Rows.Count > 0)
                    //    {
                    //        foreach (DataRow optionRow in dsCustomize.Tables[0].Rows)
                    //        {
                    //            customizeOption = new CustomizeOptions();
                    //            customizeOption.OptionID = Convert.ToInt32(optionRow["id"]);
                    //            customizeOption.OptionOrder = Convert.ToInt32(optionRow["option_order"]);
                    //            customizeOption.IsMultiSelect = Convert.ToBoolean(optionRow["is_multi_select"]);
                    //            customizeOption.IsDefaultOption = Convert.ToBoolean(optionRow["is_default_option"]);
                    //            customizeOption.OptionName = optionRow["customize_option"].ToString();
                    //            customizeOption.CreatedOn = Convert.ToDateTime(optionRow["created_on"]);
                    //            customizeOption.ModifiedOn = Convert.ToDateTime(optionRow["modified_on"]);
                    //            customizeOptions.Add(customizeOption);
                    //        }
                    //    }
                    //}
                    //return Ok(new { foodItems, customizeOptions });
                    return Ok();

            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Menu", "Error", "Error Loading Menu: ", ex.Message);
                return NotFound();
            }
        }

        [HttpGet]
        [Authorize]
        [Route("Food/Get")]
        public IActionResult GetMenu()
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            List<FoodItem> foodItems = new List<FoodItem>();
            List<CustomizeOptions> customizeOptions = new List<CustomizeOptions>();
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
                    //using (SqlCommand command = new SqlCommand("dbo.GetMenuCategories", connection))
                    //{
                    //    using (SqlDataAdapter da = new SqlDataAdapter())
                    //    {
                    //        da.SelectCommand = command;
                    //        da.Fill(dsCategories);
                    //    }
                    //}

                    // Get Food Types from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetFood", connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsFood);
                        }
                    }

                    FoodItem foodItem = new FoodItem();

                    if (dsFood.Tables.Count > 0 && dsFood.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow foodRow in dsFood.Tables[0].Rows)
                        {
                            foodItem = new FoodItem();
                            foodItem.FoodID = Convert.ToInt32(foodRow["id"]);
                            foodItem.MenuCategoryID = Convert.ToInt32(foodRow["menu_category_id"]);
                            foodItem.FoodName = foodRow["food"].ToString();
                            foodItem.FoodOrder = Convert.ToInt32(foodRow["food_order"]);
                            foodItem.CreatedOn = Convert.ToDateTime(foodRow["created_on"]);
                            foodItem.ModifiedOn = Convert.ToDateTime(foodRow["modified_on"]);
                            foodItems.Add(foodItem);
                        }
                    }


                    //Get Customization from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetCustomizeOptions", connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsCustomize);
                        }
                    }

                    CustomizeOptions customizeOption = new CustomizeOptions();

                    if (dsCustomize.Tables.Count > 0 && dsCustomize.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow optionRow in dsCustomize.Tables[0].Rows)
                        {
                            customizeOption = new CustomizeOptions();
                            customizeOption.OptionID = Convert.ToInt32(optionRow["id"]);
                            customizeOption.OptionOrder = Convert.ToInt32(optionRow["option_order"]);
                            customizeOption.IsMultiSelect = Convert.ToBoolean(optionRow["is_multi_select"]);
                            customizeOption.IsDefaultOption = Convert.ToBoolean(optionRow["is_default_option"]);
                            customizeOption.OptionName = optionRow["customize_option"].ToString();
                            customizeOption.CreatedOn = Convert.ToDateTime(optionRow["created_on"]);
                            customizeOption.ModifiedOn = Convert.ToDateTime(optionRow["modified_on"]);
                            customizeOptions.Add(customizeOption);
                        }
                    }
                }
                return Ok(new { foodItems, customizeOptions });

            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Menu", "Error", "Error Getting Food Items for User: " + currentUser.Email, ex.Message);
                return NotFound(foodItems);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("OptionItems/Get")]
        public IActionResult GetOptionItemsForFood([FromBody] int foodID)
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            List<CustomizeItem> customizeItems = new List<CustomizeItem>();
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet dsOptionsItems = new DataSet();

                // Get Data from DB
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();

                    //Get Customization from DB
                    using (SqlCommand command = new SqlCommand("dbo.GetOptionItemsForFoodID", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@food_id";
                        param.Value = foodID;
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        using (SqlDataAdapter da = new SqlDataAdapter())
                        {
                            da.SelectCommand = command;
                            da.Fill(dsOptionsItems);
                        }
                    }

                    CustomizeItem customizeItem = new CustomizeItem();

                    if (dsOptionsItems.Tables.Count > 0 && dsOptionsItems.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow optionRow in dsOptionsItems.Tables[0].Rows)
                        {
                            customizeItem = new CustomizeItem();
                            customizeItem.CustomizeOptionItemID = Convert.ToInt32(optionRow["option_item_id"]);
                            customizeItem.CustomizeOption = optionRow["customize_option"].ToString();
                            customizeItem.OptionID = Convert.ToInt32(optionRow["option_id"]);
                            customizeItem.CustomizeOptionItem = optionRow["customize_option_item"].ToString();
                            customizeItem.CustomizeOptionItemOrder = Convert.ToInt32(optionRow["customize_option_item_order"]);
                            customizeItem.Price = Convert.ToDecimal(optionRow["price"]);
                            customizeItem.CreatedOn = Convert.ToDateTime(optionRow["created_on"]);
                            customizeItem.ModifiedOn = Convert.ToDateTime(optionRow["modified_on"]);
                            customizeItems.Add(customizeItem);
                        }
                    }
                }
                return Ok(customizeItems);

            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Menu", "Error", "Error Getting Option Items for food " + foodID + ". User: " + currentUser.Email, ex.Message);
                return NotFound(customizeItems);
            }
        }
    }
}
