namespace Convocation_Management_System.Web.UI.Models
{
    public class SSLCommerzInitRequest
    {
        public decimal total_amount { get; set; }
        public string currency { get; set; } = "BDT";
        public string tran_id { get; set; } = "";
        public string success_url { get; set; } = "";
        public string fail_url { get; set; } = "";
        public string cancel_url { get; set; } = "";
        public string ipn_url { get; set; } = "";
        public string product_name { get; set; } = "";
        public string product_category { get; set; } = "Convocation";
        public string product_profile { get; set; } = "general";

        public string cus_name { get; set; } = "";
        public string cus_email { get; set; } = "";
        public string cus_add1 { get; set; } = "Dhaka";
        public string cus_city { get; set; } = "Dhaka";
        public string cus_country { get; set; } = "Bangladesh";
        public string cus_phone { get; set; } = "01700000000";

        public string shipping_method { get; set; } = "NO";
        public string num_of_item { get; set; } = "1";

        public string value_a { get; set; } = "";
        public string value_b { get; set; } = "";
        public string value_c { get; set; } = "";
        public string value_d { get; set; } = "";
    }
}