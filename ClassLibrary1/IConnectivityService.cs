namespace CES_TEST;

public interface IConnectivityService
{
    public bool IsConnected { get; }
    public NetworkAccess NetworkAccess { get; }
    
    IObservable<NetworkAccess> ConnectivityChanged { get; }
}