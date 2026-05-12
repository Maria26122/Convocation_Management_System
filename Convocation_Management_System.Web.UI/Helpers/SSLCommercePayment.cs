using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO;

namespace Convocation_Management_System.Web.UI.Helpers
{
    public class SSLCommercePayment
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private string _storeId;
        private string _storePassword;
        private bool _storeTestMode;
        private string _sslCommerzUrl = "https://securepay.sslcommerz.com/";
        private string _submitUrl = "gwprocess/v4/api.php";
        private string _validationUrl = "validator/api/validationserverAPI.php";
        private string _checkingUrl = "validator/api/merchantTransIDvalidationAPI.php";

        public SSLCommercePayment(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            _storeId = _configuration["SSLCommerz:StoreId"];
            _storePassword = _configuration["SSLCommerz:StorePassword"];
            _storeTestMode = bool.Parse(_configuration["SSLCommerz:StoreTestMode"]);

            SetSSLCzTestMode(_storeTestMode);
        }

        public string InitiateTransaction(NameValueCollection postData, bool getGatewayList = false)
        {
            postData.Add("store_id", _storeId);
            postData.Add("store_passwd", _storePassword);
            var response = SendPost(postData).Result;

            try
            {
                var resp = JsonSerializer.Deserialize<SSLCommerzInitResponse>(response);
                if (resp.status == "SUCCESS")
                {
                    return resp.GatewayPageURL.ToString();
                }
                else
                {
                    throw new Exception("Unable to get data from SSLCommerz. Please contact your manager!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public bool OrderValidate(string merchantTrxID, string merchantTrxAmount, string merchantTrxCurrency, Microsoft.AspNetCore.Http.HttpRequest req)
        {
            bool hashVerified = ipnHashVerify(req);
            if (hashVerified)
            {
                string json = string.Empty;
                string encodedValID = Uri.EscapeDataString(req.Form["val_id"]);
                string encodedStoreID = Uri.EscapeDataString(_storeId);
                string encodedStorePassword = Uri.EscapeDataString(_storePassword);

                string validateUrl = $"{_sslCommerzUrl}{_validationUrl}?val_id={encodedValID}&store_id={encodedStoreID}&store_passwd={encodedStorePassword}&v=1&format=json";
                var response = _httpClient.GetAsync(validateUrl).Result;

                using (var reader = new StreamReader(response.Content.ReadAsStream()))
                {
                    json = reader.ReadToEnd();
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var resp = JsonSerializer.Deserialize<SSLCommerzValidatorResponse>(json);

                    if (resp.status == "VALID" || resp.status == "VALIDATED")
                    {
                        if (merchantTrxCurrency == "BDT")
                        {
                            if (merchantTrxID == resp.tran_id && Math.Abs(Convert.ToDecimal(merchantTrxAmount) - Convert.ToDecimal(resp.amount)) < 1)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (merchantTrxID == resp.tran_id && Math.Abs(Convert.ToDecimal(merchantTrxAmount) - Convert.ToDecimal(resp.currency_amount)) < 1)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void SetSSLCzTestMode(bool mode)
        {
            if (mode)
            {
                _storeId = "testbox";
                _storePassword = "qwerty";
                _sslCommerzUrl = "https://sandbox.sslcommerz.com/";
            }
        }

        private async Task<string> SendPost(NameValueCollection postData)
        {
            var content = new FormUrlEncodedContent(postData.AllKeys.ToDictionary(k => k, k => postData[k]));
            var response = await _httpClient.PostAsync($"{_sslCommerzUrl}{_submitUrl}", content);
            return await response.Content.ReadAsStringAsync();
        }

        public bool ipnHashVerify(Microsoft.AspNetCore.Http.HttpRequest req)
        {
            if (req.Form["verify_sign"] != "" && req.Form["verify_key"] != "")
            {
                string verifyKey = req.Form["verify_key"];
                if (verifyKey != "")
                {
                    List<string> keyList = verifyKey.Split(',').ToList();
                    List<KeyValuePair<string, string>> dataArray = new List<KeyValuePair<string, string>>();

                    foreach (string k in keyList)
                    {
                        dataArray.Add(new KeyValuePair<string, string>(k, req.Form[k]));
                    }

                    string hashedPassword = MD5(_storePassword);
                    dataArray.Add(new KeyValuePair<string, string>("store_passwd", hashedPassword));

                    dataArray.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));

                    string hashString = string.Join("&", dataArray.Select(kv => $"{kv.Key}={kv.Value}"));

                    string generatedHash = MD5(hashString);
                    return generatedHash == req.Form["verify_sign"];
                }
            }
            return false;
        }

        private string MD5(string input)
        {
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public class SSLCommerzInitResponse
        {
            public string status { get; set; }
            public string failedreason { get; set; }
            public string sessionkey { get; set; }
            public string GatewayPageURL { get; set; }
        }

        public class SSLCommerzValidatorResponse
        {
            public string status { get; set; }
            public string tran_id { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
            public string currency_amount { get; set; }
        }
    }
}