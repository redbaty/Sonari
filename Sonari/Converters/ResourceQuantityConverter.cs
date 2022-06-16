using CliFx.Extensibility;
using k8s.Models;

namespace Sonari.Converters;

public class ResourceQuantityConverter : BindingConverter<ResourceQuantity>
{
    public override ResourceQuantity Convert(string? rawValue)
    {
        return new ResourceQuantity(rawValue);
    }
}