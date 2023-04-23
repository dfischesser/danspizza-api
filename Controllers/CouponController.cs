using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Pizza.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CouponController : ControllerBase
    {

        private readonly ILogger<CouponController> _logger;

        private Coupons coupons = new Coupons();
        private List<Coupon> cp = new List<Coupon>();

        public CouponController(ILogger<CouponController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetCoupons")]
        public IEnumerable<Coupon> Get()
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.DataSource = "FUSER";
            sqlBuilder.UserID = "fuser";
            sqlBuilder.Password = "goverbose";
            sqlBuilder.InitialCatalog = "PizzaDB";
            sqlBuilder.Encrypt = false;
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("dbo.GetTestData", connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(ds, "test_data");
                    }
                }
            }


            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var coupon = new Coupon();
                    if (dr.ToString() != null)
                    {
                        coupon.CouponID = Convert.ToInt32(dr["id"]);
                        coupon.CouponText = string.IsNullOrEmpty(dr["test_data"].ToString()) ? "" : dr["test_data"].ToString();
                    }
                    cp.Add(coupon);
                }
            }
            coupons.CouponList = cp;

            return coupons.CouponList;
        }
    }
}