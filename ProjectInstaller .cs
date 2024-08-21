using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
[RunInstaller(true)]
public class ProjectInstaller : Installer
{
    private ServiceProcessInstaller processInstaller;
    private ServiceInstaller serviceInstaller;
    public ProjectInstaller()
    {
        processInstaller = new ServiceProcessInstaller();
        serviceInstaller = new ServiceInstaller();
        processInstaller.Account = ServiceAccount.LocalSystem;
        processInstaller.Username = null;
        processInstaller.Password = null;
        serviceInstaller.DisplayName = "Currency Fetcher Service";
        serviceInstaller.StartType = ServiceStartMode.Automatic;
        serviceInstaller.ServiceName = "CurrencyFetcherService";
        Installers.Add(serviceInstaller);
        Installers.Add(processInstaller);
    }
}