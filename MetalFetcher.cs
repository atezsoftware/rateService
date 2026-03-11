using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

public class MetalFetcher
{
    private const string BorsaUrl = "https://www.borsaistanbul.com/daily-bulletin.php?op=fetchBultenVerileri&lang=tr";

    public static void FetchAndWriteMetals()
    {
        List<Currency> metals = FetchMetals();
        if (metals != null && metals.Count > 0)
        {
           // WriteToFile(metals);
            WriteToDatabase(metals);
        }
    }

    private static List<Currency> FetchMetals()
    {
        string xml;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BorsaUrl);
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
        {
            xml = reader.ReadToEnd();
        }
        XDocument document = XDocument.Parse(xml);

        // Tarih kontrolü
        var gun1 = document.Descendants("gun1").FirstOrDefault();
        if (gun1 == null) return null;

        int gun = int.Parse(gun1.Element("gun").Value);
        int ay = int.Parse(gun1.Element("ay").Value);
        int yil = int.Parse(gun1.Element("yil").Value);
        var xmlDate = new DateTime(yil, ay, gun);

        var metals = new List<Currency>();

        // ALTIN - kapanis değeri
        var altinElement = document.Descendants("ALTIN").FirstOrDefault();
        if (altinElement != null)
        {
            var seansYtl = altinElement.Element("SEANSytl");
            if (seansYtl != null)
            {
                var kapanisElement = seansYtl.Element("kapanis");
                if (kapanisElement != null && kapanisElement.Value.Length > 0)
                {
                    decimal kapanis = Math.Round(ParseTurkishDecimal(kapanisElement.Value) / 1000M, 4);
                    metals.Add(new Currency
                    {
                        Code = "ALT",
                        Date = xmlDate,
                        BuyRate = kapanis,
                        SellRate = 0M,
                        BankNoteBuying = 0M,
                        BankNoteSelling = 0M
                    });
                }
            }
        }

        // GUMUS - onceki_kapanis değeri
        var gumusElement = document.Descendants("GUMUS").FirstOrDefault();
        if (gumusElement != null)
        {
            var seansYtl = gumusElement.Element("SEANSytl");
            if (seansYtl != null)
            {
                var oncekiKapanisElement = seansYtl.Element("onceki_kapanis");
                if (oncekiKapanisElement != null && oncekiKapanisElement.Value.Length > 0)
                {
                    decimal oncekiKapanis = Math.Round(ParseTurkishDecimal(oncekiKapanisElement.Value) / 1000M, 4);
                    metals.Add(new Currency
                    {
                        Code = "GMS",
                        Date = xmlDate,
                        BuyRate = oncekiKapanis,
                        SellRate = 0M,
                        BankNoteBuying = 0M,
                        BankNoteSelling = 0M
                    });
                }
            }
        }

        return metals;
    }

    private static decimal ParseTurkishDecimal(string value)
    {
        // "7.415.000,00" -> "7415000,00" -> "7415000.00"
        return decimal.Parse(value.Replace(".", "").Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static SqlParameter CreateDecimalParam(string name, decimal value)
    {
        var param = new SqlParameter(name, SqlDbType.Decimal);
        param.Precision = 18;
        param.Scale = 4;
        param.Value = value;
        return param;
    }

    private static void WriteToFile(List<Currency> metals)
    {
        try
        {
            string filePath = @"C:\Kurlar\kurlar.txt";
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var metal in metals)
                {
                    writer.WriteLine($"{DateTime.Now}: {metal.Code} - {metal.BuyRate}");
                }
            }
        }
        catch (Exception e)
        {
            string filePath = @"C:\Kurlar\logs.txt";
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: Metal WriteToFile Error - {e.Message}");
            }
        }
    }

    private static void WriteToDatabase(List<Currency> metals)
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
                string filePath = @"C:\Kurlar\logs.txt";
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine($"{DateTime.Now}: Warning - Connection string '{name}' not found in configuration.");
                    }
                }
                catch { }
            }
        }

        foreach (var connectionString in connectionStrings)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Gün kaydı yoksa oluştur (bugünün tarihine göre)
                    string daycontrolQuery = "IF NOT EXISTS (SELECT 1 FROM sbr_doviz WHERE cast(tarih as date) = @Date) BEGIN INSERT INTO sbr_doviz (dovizid, tarih, onay) VALUES (@dovizid, @Date, 1) END";
                    using (SqlCommand daycontrolCommand = new SqlCommand(daycontrolQuery, connection))
                    {
                        daycontrolCommand.Parameters.AddWithValue("@dovizid", Guid.NewGuid());
                        daycontrolCommand.Parameters.AddWithValue("@Date", DateTime.Today);
                        daycontrolCommand.ExecuteNonQuery();
                    }

                    // Her metal için bugün kaydı yoksa ekle
                    foreach (var metal in metals)
                    {
                        string query = @"IF NOT EXISTS (SELECT 1 FROM sbr_dovizdetay WHERE dovizkod = @Code AND cast(tarih as date) = @Date)
                            BEGIN
                                INSERT INTO sbr_dovizdetay (dovizdetayid, tarih, dovizalis, dovizsatis, dovizkod, efektifalis, efektifsatis, kayitgiristarih) 
                                VALUES (@id, @Date, @BuyRate, @SellRate, @Code, @BankNoteBuying, @BankNoteSelling, cast(getdate() as date))
                            END";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@id", metal.Id);
                            command.Parameters.AddWithValue("@Date", DateTime.Today);
                            command.Parameters.AddWithValue("@Code", metal.Code);

                            command.Parameters.Add(CreateDecimalParam("@BuyRate", metal.BuyRate));
                            command.Parameters.Add(CreateDecimalParam("@SellRate", metal.SellRate));
                            command.Parameters.Add(CreateDecimalParam("@BankNoteBuying", metal.BankNoteBuying));
                            command.Parameters.Add(CreateDecimalParam("@BankNoteSelling", metal.BankNoteSelling));

                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string filePath = @"C:\Kurlar\logs.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"Metal Exception: {ex.Message}");
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
                    mail.Subject = "Metal Fiyatları Alınamamıştır Lütfen Kontrol Ediniz!";
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
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine($"Failed to send email: {emailEx.Message}");
                    }
                }
            }
        }
    }
}
