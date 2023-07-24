using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Security.Claims;
using System.Web;
using System.Buffers.Text;
using System.Text;
using System.Diagnostics;
using RandomDataGenerator.Randomizers;
using RandomDataGenerator.FieldOptions;
using System.Reflection.Metadata.Ecma335;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        SqlTools sqlTools;
        private readonly IConfiguration _config;
        public UserController(IConfiguration config)
        {
            _config = config;
            sqlTools = new SqlTools(_config);
        }

        [HttpGet]
        [Route("Live")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLive()
        {
            try
            {
                HttpClient client = new();
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                var json = await client.GetStringAsync("https://www.youtube.com/@fineplus2points");

                bool isLive = json.Contains("hqdefault_live");

                return Ok(new { message = isLive });
            } 
            catch (Exception ex) 
            {
                sqlTools.Logamuffin("Account", "Error", "Error Checking Live", error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return NotFound(new { message = "Error Checking Live" });
            }
        }

        [HttpPost]
        [Route("OrderPage")]
        [Authorize]
        public IActionResult OrderPage([FromBody] int page)
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            try
            {
                var allOrders = new AllOrders();
                var orders = new List<Order>();
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    GetUserOrders(allOrders, currentUser, connection, page);
                }
                return Ok(allOrders.pastOrders);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Order Page " + page + " for user " + currentUser.Email, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return NotFound(new { message = "Error Getting Order Page " + page + " for user " + currentUser.Email });
            }
        }

        [HttpGet]
        [Route("Account")]
        [Authorize]
        public IActionResult Account()
        {
            Account account = new Account();
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

                DataSet ds = new DataSet();
                var orderItems = new List<OrderItem>();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    var OrderIDs = new List<int>();
                    var customizeIDs = new List<int>();
                    var orders = new List<Order>();
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetUserById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@id";
                        param.Value = currentUser.UserID;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                account.UserID = Convert.ToInt32(currentUser.UserID);
                                account.Role = currentUser.Role;
                                account.FirstName = string.IsNullOrEmpty(reader["first_name"].ToString()) ? "" : reader["first_name"].ToString();
                                account.LastName = string.IsNullOrEmpty(reader["last_name"].ToString()) ? "" : reader["last_name"].ToString();
                                account.Email = string.IsNullOrEmpty(reader["email"].ToString()) ? "" : reader["email"].ToString();
                                account.Phone = string.IsNullOrEmpty(reader["phone"].ToString()) ? "" : reader["phone"].ToString();
                                account.Address1 = string.IsNullOrEmpty(reader["address1"].ToString()) ? "" : reader["address1"].ToString();
                                account.Address2 = string.IsNullOrEmpty(reader["address2"].ToString()) ? "" : reader["address2"].ToString();
                                account.City = string.IsNullOrEmpty(reader["city"].ToString()) ? "" : reader["city"].ToString();
                                account.State = string.IsNullOrEmpty(reader["state"].ToString()) ? "" : reader["state"].ToString();
                                account.Zip = string.IsNullOrEmpty(reader["zip"].ToString()) ? "" : reader["zip"].ToString();
                                
                            }
                        }
                    }

                    AllOrders allOrders = new AllOrders();

                    GetUserOrders(allOrders, currentUser, connection, 1);

                    account.ActiveOrders = allOrders.activeOrders.OrderByDescending(x => x.Created).ToList();
                    account.PastOrders = allOrders.pastOrders.OrderByDescending(x => x.Created).ToList();
                    account.OrderCount = allOrders.OrderCount;

                }
                sqlTools.Logamuffin("Account (GetUserByEmail)", "System", "Retrieved Account for " + account.Email + ". User ID: " + account.UserID, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return Ok(account);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Account for user " + currentUser.Email, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return NotFound(account);
            }
        }

        public static void FillOrders(List<Order> orders, SqlConnection connection)
        {
            foreach (var item in orders)
            {
                using (SqlCommand command = new SqlCommand("dbo.GetOrderItems", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@order_id";
                    param.Value = item.OrderID;
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);
                    // TODO: Add Error Logging for Users
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var food = new FoodItem();
                            food.FoodID = Convert.ToInt32(reader["food_id"]);
                            food.FoodName = reader["food_name"].ToString();
                            food.OrderItemID = Convert.ToInt32(reader["id"]);
                            food.Price = 0;
                            item.FoodItems.Add(food);
                        }
                    }
                }

                foreach (var foodItem in item.FoodItems)
                {
                    using (SqlCommand command = new SqlCommand("dbo.GetOrderItemOptions", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@order_item_id";
                        param.Value = foodItem.OrderItemID;
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var option = new CustomizeOptions();
                                option.OptionName = reader["option_name"].ToString();
                                option.OrderItemOptionID = Convert.ToInt32(reader["id"]);
                                foodItem.CustomizeOptions.Add(option);
                            }
                        }
                    }

                    foreach (var option in foodItem.CustomizeOptions)
                    {
                        using (SqlCommand command = new SqlCommand("dbo.GetOrderItemOptionItems", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@order_item_option_id";
                            param.Value = option.OrderItemOptionID;
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);
                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var customizeItem = new CustomizeItem();
                                    customizeItem.CustomizeOptionItem = reader["order_item_option_item_name"].ToString();
                                    customizeItem.Price = Convert.ToDecimal(reader["price"]);
                                    item.totalPrice += Convert.ToDecimal(reader["price"]);
                                    foodItem.Price += Convert.ToDecimal(reader["price"]);
                                    option.OptionItems.Add(customizeItem);

                                }
                            }
                        }
                    }
                }
            }
        }
        private void GetUserOrders(AllOrders allOrders, UserModel currentUser, SqlConnection connection, int page)
        {

            var dsOrders = new DataSet();
            try
            {
                using (SqlCommand command = new SqlCommand("dbo.GetUserOrders", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@user_id";
                    param.Value = Convert.ToInt32(currentUser.UserID);
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);

                    param = new SqlParameter();
                    param.ParameterName = "@page";
                    param.Value = page;
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);

                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = command;
                    adapter.Fill(dsOrders);

                    if (dsOrders.Tables.Count > 2)
                    {
                        if (dsOrders.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[0].Rows)
                            {
                                var order = new Order();
                                order.OrderID = Convert.ToInt32(row["id"]);
                                order.Created = Convert.ToDateTime(row["created_on"]);
                                order.totalPrice = 0;
                                allOrders.activeOrders.Add(order);
                            }
                        }

                        if (dsOrders.Tables[1].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[1].Rows)
                            {
                                var order = new Order();
                                order.OrderID = Convert.ToInt32(row["id"]);
                                order.Created = Convert.ToDateTime(row["created_on"]);
                                order.totalPrice = 0;

                                allOrders.pastOrders.Add(order);
                            }
                        }

                        if (dsOrders.Tables[2].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[2].Rows)
                            {
                                allOrders.OrderCount = Convert.ToInt32(row["past_order_count"]);
                            }
                        }
                    }
                }
                FillOrders(allOrders.activeOrders, connection);
                FillOrders(allOrders.pastOrders, connection);

            } 
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Orders for user " + currentUser.Email, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
            }
        }

        private string GetRoad(int num)
        {
            string road = "";
            switch (num)
            {
                case 1:
                    road = "Street";
                    break;
                case 2:
                    road = "Avenue";
                    break;
                case 3:
                    road = "Drive";
                    break;
                case 4:
                    road = "Court";
                    break;
                case 5:
                    road = "Lane";
                    break;
                case 6:
                    road = "Terrace";
                    break;
                case 7:
                    road = "Way";
                    break;
                default:
                    road = "Road";
                    break;
            }
            return road;
        }

        private int GetRandomInt(int min, int max)
        {
            var randomInt = RandomizerFactory.GetRandomizer(new FieldOptionsInteger() { Min = min, Max = max });
            return randomInt.Generate() ?? 0;
        }

        [HttpPost]
        [Route("CreateRandom")]
        [AllowAnonymous]
        public IActionResult CreateRandomUser([FromBody] int role)
        {
            SqlTools sqlTools = new SqlTools(_config);
            var clientIP = Request.HttpContext.Connection.RemoteIpAddress;
            
            string emailer = "";
            try
            {
                string regex = @"^(\+1){1}(\s[1-9]{3}){2}(\s[1-9]{4}){1}";
                List<string> states = new List<string>() { "AK", "AL", "AR", "AS", "AZ", "CA", "CO", "CT", "DC", "DE", "FL", "GA", "GU", "HI", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "MA", "MD", "ME", "MI", "MN", "MO", "MS", "MT", "NC", "ND", "NE", "NH", "NJ", "NM", "NV", "NY", "OH", "OK", "OR", "PA", "PR", "RI", "SC", "SD", "TN", "TX", "UT", "VA", "VI", "VT", "WA", "WI", "WV", "WY" };
                var randomfName = RandomizerFactory.GetRandomizer(new FieldOptionsFirstName());
                string firstName = randomfName.Generate();

                var randomlName = RandomizerFactory.GetRandomizer(new FieldOptionsLastName());
                string lastName = randomlName.Generate();

                var randomPhone = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { UseNullValues = false, Pattern = regex });
                string phone = randomPhone.Generate().Replace("\t", " ");

                int intyboi = GetRandomInt(1000, 9999);

                int addrNum = GetRandomInt(1, 999);

                int roadyboi = GetRandomInt(1, 7);

                var randomAddr = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Max = 1 });
                string addr2 = randomAddr.Generate();
                addr2 = char.ToUpper(addr2[0]) + addr2[1..];
                addr2 += " " + GetRoad(roadyboi);

                var randomCity = RandomizerFactory.GetRandomizer(new FieldOptionsCity());
                var cityboi = randomCity.Generate();


                int stater = GetRandomInt(1, 50);

                int zipper = GetRandomInt(10501, 99950);

                emailer = firstName + "." + lastName + intyboi.ToString() + "@danspizza.dev";

                var randomPass = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 2, Max = 2 });
                string pass = randomPass.Generate().Replace(" ", "");
                pass += GetRandomInt(10, 99).ToString().Replace(" ", "");

                Account account = new Account();
                account.FirstName = firstName;
                account.LastName = lastName;
                account.Address1 = addrNum.ToString();
                account.Address2 = addr2;
                account.City = cityboi;
                account.State = states[stater];
                account.Zip = zipper.ToString();

                string hash = BCrypt.Net.BCrypt.HashPassword(pass);
                int id = 0;


                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.CreateRandomUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@first_name";
                        param.Value = firstName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@last_name";
                        param.Value = lastName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@email";
                        param.Value = emailer;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@password";
                        param.Value = hash;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@role_id";
                        param.Value = role;
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@phone";
                        param.Value = phone;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@address1";
                        param.Value = addrNum.ToString();
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@address2";
                        param.Value = addr2;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@city";
                        param.Value = cityboi;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@state";
                        param.Value = states[stater];
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@zip";
                        param.Value = zipper;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                id = Convert.ToInt32(reader["user_id"]);
                            }
                        }
                    }
                }

                Token token = new Token();

                token = sqlTools.TryLogin(new UserLogin { Id = id, Password = pass});


                if (token != null)
                {
                    CookieOptions cookieOptions = new()
                    {
                        Expires = DateTimeOffset.Now.AddDays(15),
                        MaxAge = TimeSpan.FromDays(15),
                        HttpOnly = true
                    };
                    var response = new HttpResponseMessage();

                    HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                }

                sqlTools.Logamuffin("CreateRandom", "System", "Created Random account Account. UserID " + id, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return Ok( new { message = "Login Success", email = emailer, password = pass });
                
                
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account for user ", error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return new NotFoundObjectResult(new { message = "Error Creating Account for userID " + emailer});
            }
        }

        [HttpPost]
        [Route("Create")]
        [AllowAnonymous]
        public IActionResult CreateUser([FromBody] UserLogin userLogin)
        {
            SqlTools sqlTools = new SqlTools(_config);
            Boolean uniqueEmail = true;
            int userID = 0;
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetUserByEmail", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@email";
                        param.Value = userLogin.Email;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["email"].ToString() == userLogin.Email)
                                {
                                    uniqueEmail = false;
                                }
                            }
                        }
                    }
                    if (uniqueEmail)
                    {
                        string hash = BCrypt.Net.BCrypt.HashPassword(userLogin.Password);
                        using (SqlCommand command = new SqlCommand("dbo.CreateUser", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@email";
                            param.Value = userLogin.Email;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@password";
                            param.Value = hash;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    userID = Convert.ToInt32(reader["user_id"]);
                                }
                            }
                        }

                        Token token = new Token();

                        token = sqlTools.TryLogin(userLogin);


                        if (token != null)
                        {
                            CookieOptions cookieOptions = new()
                            {
                                Expires = DateTimeOffset.Now.AddDays(15),
                                MaxAge = TimeSpan.FromDays(15),
                                HttpOnly = true
                            };
                            var response = new HttpResponseMessage();

                            HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                        }

                        return Ok(new { message = "Login Success" });
                    }
                    else
                    {
                        sqlTools.Logamuffin("Create", "Error", "Email already exists for user " + userLogin.Email, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                        return new NotFoundObjectResult(new { message = "Email already exists" });
                    }
                }
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account for user " + userLogin.Email, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return new NotFoundObjectResult(new { message = "Error Creating Account for user " + userLogin.Email });
            }
        }

        [HttpPost]
        [Route("CreateStep2")]
        [Authorize]
        public IActionResult CreateStep2([FromBody] object step2details)
        {
            SqlTools sqlTools = new SqlTools(_config);
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            string? email = "";
            string? role = "";

            Account user = JsonConvert.DeserializeObject<Account>(step2details.ToString());
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.CreateUserStep2", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@id";
                        param.Value = Convert.ToInt32(currentUser.UserID);
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@first_name";
                        param.Value = user.FirstName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@last_name";
                        param.Value = user.LastName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@phone";
                        param.Value = user.Phone;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@street_number";
                        param.Value = user.Address1;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@street_name";
                        param.Value = user.Address2;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@city";
                        param.Value = user.City;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@state";
                        param.Value = user.State;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@zip";
                        param.Value = user.Zip;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            email = reader["email"].ToString();
                            role = reader["role"].ToString();
                        }
                    }
                }

                Token token = new();

                token = sqlTools.Step2Token(new UserModel { UserID = currentUser.UserID, UserFirstName = user.FirstName, Role = role, Email = email});

                if (token != null)
                {
                    CookieOptions cookieOptions = new()
                    {
                        Expires = DateTimeOffset.Now.AddDays(15),
                        MaxAge = TimeSpan.FromDays(15),
                        HttpOnly = true
                    };
                    var response = new HttpResponseMessage();

                    HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                }

                return Ok(new { message = "Login Success" });


            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account Step 2 for user " + currentUser.UserFirstName, error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString());
                return new NotFoundObjectResult(new { message = "Error Creating Account Step 2 for user " + currentUser.UserFirstName });
            }
        }

        
        
    }
}

