using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Configuration;
using System.Net.Mail;
using System.Text;

public class CurrencyFetcher
{
    public static void FetchAndWriteRates()
    {
        string url = GetUrl();
        List<Currency> currencies = FetchCurrencies(url);
        WriteToFile(currencies);
        string filePath = @"C:\Kurlar\kurlar.txt";
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            foreach (var currency in currencies)
            {
                writer.WriteLine("txt");
            }
        }
        WriteToDatabase(currencies);
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            foreach (var currency in currencies)
            {
                writer.WriteLine("db");
            }
        }
    }

    private static string GetUrl()
    {
        var date = DateTime.Now;
        if (date.Date == DateTime.Today)
            return "http://www.tcmb.gov.tr/kurlar/today.xml";
        else
            return string.Format("http://www.tcmb.gov.tr/kurlar/{0}{1}/{2}{1}{0}.xml", date.Year, AddZero(date.Month), AddZero(date.Day));
    }

    private static string AddZero(int number)
    {
        return number < 10 ? "0" + number : number.ToString();
    }

    private static List<Currency> FetchCurrencies(string url)
    {
        XDocument document = XDocument.Load(url);
        var result = document.Descendants("Currency")
            .Where(v => v.Element("ForexBuying") != null && v.Element("ForexBuying").Value.Length > 0)
            .Select(v => new Currency
            {
                Code = v.Attribute("Kod").Value,
                BuyRate = decimal.Parse(v.Element("ForexBuying").Value.Replace('.', ',')),
                BankNoteBuying = v.Element("BanknoteBuying") != null && v.Element("BanknoteBuying").Value.Length > 0 ? decimal.Parse(v.Element("BanknoteBuying").Value.Replace('.', ',')) : 0M,
                SellRate = v.Element("ForexSelling") != null && v.Element("ForexSelling").Value.Length > 0 ? decimal.Parse(v.Element("ForexSelling").Value.Replace('.', ',')) : 0M,
                BankNoteSelling = v.Element("BanknoteSelling") != null && v.Element("BanknoteSelling").Value.Length > 0 ? decimal.Parse(v.Element("BanknoteSelling").Value.Replace('.', ',')) : 0M
            }).ToList();
        return result;
    }


    private static void WriteToFile(List<Currency> currencies)
    {
        try
        {
            string filePath = @"C:\Kurlar\kurlar.txt";
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var currency in currencies)
                {
                    writer.WriteLine($"{DateTime.Now}: {currency.Code} - {currency.BuyRate} - {currency.SellRate} - {currency.BankNoteBuying} - {currency.BankNoteSelling}");
                }
            }
        }
        catch (Exception e)
        {
            string filePath = @"C:\Kurlar\kurlar.txt";
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var currency in currencies)
                {
                    writer.WriteLine(e);
                }
            }

        }
        
    }




    private static void WriteToDatabase(List<Currency> currencies)
    {
        var connectionStrings = new string[]
        {
        ConfigurationManager.ConnectionStrings["SEB"].ConnectionString,
        ConfigurationManager.ConnectionStrings["SEC"].ConnectionString,
        ConfigurationManager.ConnectionStrings["SECTEST"].ConnectionString
        };

        foreach (var connectionString in connectionStrings)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string daycontrolQuery = "IF NOT EXISTS (SELECT 1 FROM sbr_doviz WHERE cast(tarih as date) = CAST(GETDATE() AS DATE)) BEGIN INSERT INTO sbr_doviz (dovizid, tarih, onay) VALUES (@dovizid, @Date, 1) END";
                    using (SqlCommand daycontrolCommand = new SqlCommand(daycontrolQuery, connection))
                    {
                        var dovizid = Guid.NewGuid();
                        daycontrolCommand.Parameters.AddWithValue("@dovizid", dovizid);
                        daycontrolCommand.Parameters.AddWithValue("@Date", DateTime.Today);

                        int rowsAffected = daycontrolCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            foreach (var currency in currencies)
                            {
                                string query = "INSERT INTO sbr_dovizdetay (dovizdetayid, tarih, dovizalis, dovizsatis, dovizkod, efektifalis, efektifsatis, kayitgiristarih) VALUES (@id, @Date, @BuyRate, @SellRate, @Code, @BankNoteBuying, @BankNoteSelling, cast(getdate() as date))";
                                using (SqlCommand command = new SqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@id", currency.Id);
                                    command.Parameters.AddWithValue("@Date", DateTime.Today);
                                    command.Parameters.AddWithValue("@BuyRate", currency.BuyRate);
                                    command.Parameters.AddWithValue("@SellRate", currency.SellRate);
                                    command.Parameters.AddWithValue("@Code", currency.Code);
                                    command.Parameters.AddWithValue("@BankNoteBuying", currency.BankNoteBuying);
                                    command.Parameters.AddWithValue("@BankNoteSelling", currency.BankNoteSelling);

                                    command.ExecuteNonQuery();
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string filePath = @"C:\Kurlar\logs.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"Exception: {ex.Message}");
                }

                string addressFrom = ConfigurationManager.AppSettings["AddressFrom"];
                string displayNameFrom = ConfigurationManager.AppSettings["DisplayNameFrom"];
                string addressTo = ConfigurationManager.AppSettings["AddressTo"];
                string displayNameTo = ConfigurationManager.AppSettings["DisplayNameTo"];
                string smtpClientHost = ConfigurationManager.AppSettings["SmtpClientHost"];
                int smtpClientPort = int.Parse(ConfigurationManager.AppSettings["SmtpClientPort"]);
                string smtpUserName = ConfigurationManager.AppSettings["NetworkCredentialUserName"];
                string smtpPassword = ConfigurationManager.AppSettings["NetworkCredentialPassword"];

                try
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(addressFrom, displayNameFrom);
                    mail.To.Add(new MailAddress(addressTo, displayNameTo));
                    mail.Subject = "Kurlar Alınamamıştır Lütfen Kontrol Ediniz!";
                    mail.Body = "Hata Mesajı:"+ connectionString.Substring(connectionString.IndexOf("Catalog=") + 8, 
                                                connectionString.IndexOf(";", connectionString.IndexOf("Catalog="))-
                                                connectionString.IndexOf("Catalog=")-8) + 
                                                $" {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                    mail.IsBodyHtml = true;

                    SmtpClient smtpClient = new SmtpClient(smtpClientHost, smtpClientPort);
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserName, smtpPassword);
                    smtpClient.EnableSsl = false;  
                    smtpClient.Host= smtpClientHost;

                    smtpClient.Send(mail);
                }
                catch (Exception emailEx)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine($"Failed to send email: {emailEx.Message}");
                    }

                }
            }
        }
    }

}

public class Currency
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public string Code { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public decimal BankNoteBuying { get; set; }
    public decimal BankNoteSelling { get; set; }
}
