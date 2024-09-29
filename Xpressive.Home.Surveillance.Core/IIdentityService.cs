namespace Xpressive.Home.Surveillance.Core;

public interface IIdentityService
{
    string GetPublicKey();
    string GetNonce();
}
