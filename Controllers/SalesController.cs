using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        SqlTools sqlTools = new SqlTools();
        private readonly IConfiguration _config;
        public SalesController(IConfiguration config)
        {
            _config = config;
            sqlTools = new SqlTools(_config);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Daily")]
        public IActionResult DailySales(int days = 1)
        {
            try
            {
                List<DailySales> salebois = new();
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetSales", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        var param = new SqlParameter();
                        param.ParameterName = "@days";
                        param.Value = days;
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            DailySales dailySales = new();
                            dailySales.FoodType = reader["food_type"].ToString();
                            dailySales.Amount = Convert.ToInt32(reader["amount"]);
                            dailySales.Price = Convert.ToDecimal(reader["price"]);
                            salebois.Add(dailySales);
                        }
                    }
                    return Ok(new { message = "Success", sales = salebois });
                }
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Daily Sales", "Error", "Error retrieving Daily Sales", error: ex.Message, clientIP: Request.HttpContext.Connection.RemoteIpAddress.ToString()) ;
                return NotFound("Could not retrieve orders");
            }
        }
    }
}
