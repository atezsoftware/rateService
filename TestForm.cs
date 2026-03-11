using System;
using System.IO;
using System.Windows.Forms;

namespace rateService
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void btnCurrency_Click(object sender, EventArgs e)
        {
            txtLog.AppendText($"[{DateTime.Now}] Kur servisi başlatılıyor...\r\n");
            try
            {
                CurrencyFetcher.FetchAndWriteRates();
                txtLog.AppendText($"[{DateTime.Now}] Kur servisi başarıyla tamamlandı.\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"[{DateTime.Now}] Kur servisi HATA: {ex.Message}\r\n");
            }
            txtLog.AppendText(new string('-', 60) + "\r\n");
        }

        private void btnMetal_Click(object sender, EventArgs e)
        {
            txtLog.AppendText($"[{DateTime.Now}] Metal servisi başlatılıyor...\r\n");
            try
            {
                MetalFetcher.FetchAndWriteMetals();
                txtLog.AppendText($"[{DateTime.Now}] Metal servisi başarıyla tamamlandı.\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"[{DateTime.Now}] Metal servisi HATA: {ex.Message}\r\n");
            }
            txtLog.AppendText(new string('-', 60) + "\r\n");
        }

        private void btnBoth_Click(object sender, EventArgs e)
        {
            btnCurrency_Click(sender, e);
            btnMetal_Click(sender, e);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void btnShowLog_Click(object sender, EventArgs e)
        {
            try
            {
                string kurlarPath = @"C:\Kurlar\kurlar.txt";
                string logsPath = @"C:\Kurlar\logs.txt";

                txtLog.AppendText("=== kurlar.txt ===\r\n");
                if (File.Exists(kurlarPath))
                {
                    string[] lines = File.ReadAllLines(kurlarPath);
                    int start = Math.Max(0, lines.Length - 20);
                    for (int i = start; i < lines.Length; i++)
                        txtLog.AppendText(lines[i] + "\r\n");
                }
                else
                {
                    txtLog.AppendText("Dosya bulunamadı.\r\n");
                }

                txtLog.AppendText("\r\n=== logs.txt ===\r\n");
                if (File.Exists(logsPath))
                {
                    string[] lines = File.ReadAllLines(logsPath);
                    int start = Math.Max(0, lines.Length - 20);
                    for (int i = start; i < lines.Length; i++)
                        txtLog.AppendText(lines[i] + "\r\n");
                }
                else
                {
                    txtLog.AppendText("Dosya bulunamadı.\r\n");
                }
                txtLog.AppendText(new string('-', 60) + "\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Log okuma hatası: {ex.Message}\r\n");
            }
        }
    }
}
