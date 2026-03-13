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
using System.Xml;

public class CurrencyFetcher
{
    private static readonly string LogFilePath = ConfigurationManager.AppSettings["LogFilePath"] ?? @"C:\Kurlar\logs.txt";
    private const int RequestTimeoutMs = 30000;

    public static void FetchAndWriteRates()
    {
        NetRuntime.Ensure();

        string url = GetUrl();
        List<Currency> currencies = FetchCurrencies(url);
       // WriteToFile(currencies);
        WriteToDatabase(currencies);

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
        XDocument document = LoadXml(url);
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

    private static XDocument LoadXml(string url)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Timeout = RequestTimeoutMs;
        request.ReadWriteTimeout = RequestTimeoutMs;
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        request.KeepAlive = false;
        request.UserAgent = "rateService";
        request.ConnectionGroupName = Guid.NewGuid().ToString("N");

        try
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
                using (var xmlReader = XmlReader.Create(stream, settings))
                {
                    return XDocument.Load(xmlReader);
                }
            }
        }
        finally
        {
            try { request.ServicePoint.CloseConnectionGroup(request.ConnectionGroupName); } catch { }
        }
    }

    private static void AppendLog(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (StreamWriter writer = new StreamWriter(LogFilePath, true, Encoding.UTF8))
            {
                writer.WriteLine(message);
            }
        }
        catch
        {
            // loglama hatası sessizce yutulsun
        }
    }


    private static void WriteToDatabase(List<Currency> currencies)
    {
        var connectionStringNames = new string[] { "SEB", "SEC", "SECTEST", "SEBTEST", "SecLTT", "SecBG2025" };
        var connectionStrings = new List<string>();

        foreach (var name in connectionStringNames)
        {
            var connStringSetting = ConfigurationManager.ConnectionStrings[name];
            if (connStringSetting != null && !string.IsNullOrEmpty(connStringSetting.ConnectionString))
            {
                connectionStrings.Add(connStringSetting.ConnectionString);
            }
            else
            {
                // Bulunamayan connection string'i logla
                AppendLog($"{DateTime.Now}: Warning - Connection string '{name}' not found in configuration.");
            }
        }

        foreach (var connectionString in connectionStrings)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string insertMasterQuery = "INSERT INTO sbr_doviz (dovizid, tarih, onay) VALUES (@dovizid, @Date, 1)";
                    using (SqlCommand insertMasterCommand = new SqlCommand(insertMasterQuery, connection))
                    {
                        var dovizid = Guid.NewGuid();
                        insertMasterCommand.Parameters.AddWithValue("@dovizid", dovizid);
                        insertMasterCommand.Parameters.AddWithValue("@Date", DateTime.Today);

                        try
                        {
                            insertMasterCommand.ExecuteNonQuery();
                        }
                        catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                        {
                            // Bugünün kaydı zaten varsa (muhtemelen MetalFetcher oluşturdu ya da servis tekrar çalıştı)
                            // devam edip detay kayıtlarını ekleyelim.
                        }

                        foreach (var currency in currencies)
                        {
                            string query = @"IF NOT EXISTS (SELECT 1 FROM sbr_dovizdetay WHERE dovizkod = @Code AND cast(tarih as date) = @Date)
BEGIN
    INSERT INTO sbr_dovizdetay (dovizdetayid, tarih, dovizalis, dovizsatis, dovizkod, efektifalis, efektifsatis, kayitgiristarih)
    VALUES (@id, @Date, @BuyRate, @SellRate, @Code, @BankNoteBuying, @BankNoteSelling, cast(getdate() as date))
END";
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
            catch (Exception ex)
            {
                AppendLog($"{DateTime.Now}: Exception: {ex.Message}");

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
                    mail.Body = "Hata Mesajı:" + connectionString.Substring(connectionString.IndexOf("Catalog=") + 8,
                                                connectionString.IndexOf(";", connectionString.IndexOf("Catalog=")) -
                                                connectionString.IndexOf("Catalog=") - 8) +
                                                $" {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                    mail.IsBodyHtml = true;

                    SmtpClient smtpClient = new SmtpClient(smtpClientHost, smtpClientPort);
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserName, smtpPassword);
                    smtpClient.EnableSsl = false;
                    smtpClient.Host = smtpClientHost;

                    smtpClient.Send(mail);
                }
                catch (Exception emailEx)
                {
                    AppendLog($"{DateTime.Now}: Failed to send email: {emailEx.Message}");

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
