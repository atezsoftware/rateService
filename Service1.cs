using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            timer.Interval = 1000 * 60 * 30;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {

            timer.Stop();
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {

            CurrencyFetcher.FetchAndWriteRates();
        }
    }
}