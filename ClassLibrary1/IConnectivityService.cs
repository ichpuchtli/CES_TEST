namespace CES_TEST;

public interface IConnectivityService
{
    public bool IsConnected { get; }
    public InternetAccess InternetAccess { get; }
    
    IObservable<InternetAccess> ConnectivityChanged { get; }
}