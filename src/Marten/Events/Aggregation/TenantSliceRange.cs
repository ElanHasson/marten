using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events.Daemon;

namespace Marten.Events.Aggregation
{
    internal class TenantSliceRange<TDoc, TId>: EventRangeGroup
    {
        private readonly DocumentStore _store;
        private readonly AggregationRuntime<TDoc, TId> _runtime;

        public TenantSliceRange(DocumentStore store, AggregationRuntime<TDoc, TId> runtime, EventRange range,
            IReadOnlyList<TenantSliceGroup<TDoc, TId>> groups, CancellationToken projectionCancellation) : base(range, projectionCancellation)
        {
            _store = store;
            _runtime = runtime;
            Groups = groups;
        }

        public IReadOnlyList<TenantSliceGroup<TDoc, TId>> Groups { get; }


        protected override void reset()
        {
            foreach (var group in Groups) group.Reset();
        }

        public override void Dispose()
        {
            foreach (var group in Groups) group.Dispose();
        }

        public override string ToString()
        {
            return $"Aggregate for {Range}, {Groups.Count} slices";
        }

        public override Task ConfigureUpdateBatch(IProjectionAgent projectionAgent, ProjectionUpdateBatch batch)
        {
            foreach (var @group in Groups)
            {
                @group.Start(projectionAgent, batch.Queue, _runtime, _store, Cancellation);
            }

            return Task.WhenAll(Groups.Select(x => x.Complete()).ToArray());
        }
    }
}
