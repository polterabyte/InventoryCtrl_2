namespace Inventory.API.Services;

public interface IPortConfigurationService
{
    PortConfiguration LoadPortConfiguration();
    string[] GetCorsOrigins();
}
