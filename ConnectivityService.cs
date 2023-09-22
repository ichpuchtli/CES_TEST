using System.Reactive.Linq;

namespace CES_TEST;

public class ConnectivityService : IConnectivityService
{
    private readonly IConnectivity _connectivity;

    public ConnectivityService(IConnectivity connectivity)
    {
        _connectivity = connectivity;

        ConnectivityChanged = Observable
            .FromEventPattern<EventHandler<ConnectivityChangedEventArgs>, ConnectivityChangedEventArgs>(
                h => _connectivity.ConnectivityChanged += h,
                h => _connectivity.ConnectivityChanged -= h)
            .Do(status => System.Diagnostics.Debug.WriteLine("Connectivity Change: ", status.EventArgs.ToString()))
            .Select(_ => MapTo(_connectivity.NetworkAccess));
    }

    private InternetAccess MapTo(NetworkAccess connectivityNetworkAccess)
    {
        return connectivityNetworkAccess switch
        {
            NetworkAccess.None => InternetAccess.NoNetwork,
            NetworkAccess.Local => InternetAccess.Local,
            NetworkAccess.ConstrainedInternet => InternetAccess.ConstrainedInternet,
            NetworkAccess.Internet => InternetAccess.Internet,
            _ => InternetAccess.Unknown
        };
    }

    public bool IsConnected => _connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;
    public InternetAccess InternetAccess => MapTo(_connectivity.NetworkAccess);
    public IObservable<InternetAccess> ConnectivityChanged { get; }
}