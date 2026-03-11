using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace rateService
{
    public partial class Service1 : ServiceBase
    {

        private Timer timer;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer = new Timer();

            string intervalStr = ConfigurationManager.AppSettings["ServiceIntervalMs"];
            double interval;
            if (!double.TryParse(intervalStr, out interval))
            {
                interval = 30000; 
            }

            timer.Interval = interval;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {

            timer.Stop();
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                CurrencyFetcher.FetchAndWriteRates();
            }
            catch (Exception ex)
            {
                LogError("CurrencyFetcher hatası: " + ex.Message);
            }

            try
            {
                MetalFetcher.FetchAndWriteMetals();
            }
            catch (Exception ex)
            {
                LogError("MetalFetcher hatası: " + ex.Message);
            }
        }

        private void LogError(string message)
        {
            try
            {
                string filePath = @"C:\Kurlar\logs.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch { }
        }
    }
}