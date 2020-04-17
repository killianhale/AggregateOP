using System.Threading;

namespace AggregateOP
{
    public interface IProjectionProcessor
    {
        void Start();
        void Stop();
    }
}