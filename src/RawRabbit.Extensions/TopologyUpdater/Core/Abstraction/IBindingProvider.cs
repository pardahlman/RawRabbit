using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core.Abstraction
{
    public interface IBindingProvider
    {
        Task<List<Binding>> GetBindingsAsync(string exchangeName);
    }
}
