using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExchangeRateDemo
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            string appId = "9b20961d611645749fd22169408be7c2"; 
            
            var currencyPairs = new List<string> { "USD/ILS", "GBP/EUR", "EUR/JPY", "EUR/USD" };

            var ratesData = await GetExchangeRates(appId, currencyPairs);
            SaveRatesToCsv(ratesData, "ExchangeRates.csv");

            // Update every 10 minutes (600,000 milliseconds)
            var timer = new System.Threading.Timer(async _ =>
            {
                ratesData = await GetExchangeRates(appId, currencyPairs);
                SaveRatesToCsv(ratesData, "ExchangeRates.csv");
                Console.WriteLine($"{DateTime.UtcNow}: Rates updated.");
            }, null, 0, 600000);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task<List<RateData>> GetExchangeRates(string appId, List<string> pairs)
        {
            try
            {
                string baseUrl = "https://openexchangerates.org/api/latest.json";
                string uri = $"{baseUrl}?app_id={appId}";

                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<ExchangeRateResult>(responseBody);
                var ratesData = pairs.Select(pair =>
                {
                    var parts = pair.Split('/');
                    var baseCurrency = parts[0];
                    var targetCurrency = parts[1];
                    var value = result.Rates.ContainsKey(targetCurrency) ? result.Rates[targetCurrency] : 0;
                    return new RateData { Pair = pair, Value = value, Date = DateTime.UtcNow };
                }).ToList();

                return ratesData;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request exception: {e.Message}");
                return new List<RateData>();
            }
        }

        static void SaveRatesToCsv(List<RateData> ratesData, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Pair,Value,Date");
                foreach (var rate in ratesData)
                {
                    writer.WriteLine($"{rate.Pair},{rate.Value.ToString(CultureInfo.InvariantCulture)},{rate.Date:yyyy-MM-dd HH:mm:ss}");
                }
            }
            Console.WriteLine("Rates saved to CSV.");
        }
    }

    public class ExchangeRateResult
    {
        public Dictionary<string, decimal> Rates { get; set; }
    }

    public class RateData
    {
        public string Pair { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }
}