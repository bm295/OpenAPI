using TaskApi.Application.Abstractions;

namespace TaskApi.Tests.Architecture;

public sealed class StorageBoundaryTests
{
    [Fact]
    public void StoreContract_DoesNotExposeMutableCollections()
    {
        var contract = typeof(IOpenBankingStore);

        Assert.Empty(contract.GetProperties());
        Assert.DoesNotContain(
            contract.GetMethods(),
            method => IsMutableDictionary(method.ReturnType) ||
                method.GetParameters().Any(parameter => IsMutableDictionary(parameter.ParameterType)));
    }

    private static bool IsMutableDictionary(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>);
}
