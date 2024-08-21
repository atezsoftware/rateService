using System.ComponentModel;
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
        // Service will run under system account
        processInstaller.Account = ServiceAccount.LocalSystem;
        processInstaller.Username = null;
        processInstaller.Password = null;
        // Service information
        serviceInstaller.DisplayName = "Currency Fetcher Service";
        serviceInstaller.StartType = ServiceStartMode.Automatic;
        serviceInstaller.ServiceName = "CurrencyFetcherService";
        Installers.Add(serviceInstaller);
        Installers.Add(processInstaller);
    }
}