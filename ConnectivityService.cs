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
            .Select(_ => MapTo(_connectivity.NetworkAccess));
    }

    private NetworkAccess MapTo(Microsoft.Maui.Networking.NetworkAccess connectivityNetworkAccess)
    {
        return connectivityNetworkAccess switch
        {
            Microsoft.Maui.Networking.NetworkAccess.None => NetworkAccess.NoNetwork,
            Microsoft.Maui.Networking.NetworkAccess.Local => NetworkAccess.Local,
            Microsoft.Maui.Networking.NetworkAccess.ConstrainedInternet => NetworkAccess.ConstrainedInternet,
            Microsoft.Maui.Networking.NetworkAccess.Internet => NetworkAccess.Internet,
            _ => NetworkAccess.Unknown
        };
    }

    public bool IsConnected => _connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;
    public NetworkAccess NetworkAccess => MapTo(_connectivity.NetworkAccess);
    public IObservable<NetworkAccess> ConnectivityChanged { get; }
}