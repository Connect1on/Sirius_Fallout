using System.Numerics;
using Content.Shared.Forensics;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class SpawnEntitiesBehavior : IThresholdBehavior
    {
        /// <progamermove>
        /// Entities spawned on reaching this threshold, from a min to a max.
        /// if this build fails, don't show up to the req line on RMC tommorow -pierow
        [DataField]
        public Dictionary<string, MinMax> Spawn = new();

        [DataField("offset")]
        public float Offset { get; set; } = 0.5f;

        [DataField("transferForensics")]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var tSys = system.EntityManager.System<TransformSystem>();
            var position = tSys.GetMapCoordinates(owner);

            var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

            var stackMultiplier = 1;
            if (system.EntityManager.TryGetComponent<StackComponent>(owner, out var stack))
                stackMultiplier = stack.Count;

            foreach (var (entityId, minMax) in Spawn)
            {
                var count = minMax.Min >= minMax.Max
                    ? minMax.Min
                    : system.Random.Next(minMax.Min, minMax.Max + 1);

                count *= stackMultiplier;

                if (count == 0)
                    continue;

                for (var i = 0; i < count; i++)
                {
                    EntityUid spawned;

                    if (SpawnInContainer)
                    {
                        spawned = system.EntityManager.SpawnNextToOrDrop(entityId, owner);
                    }
                    else
                    {
                        spawned = system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));
                    }

                    TransferForensics(spawned, system, owner);
                }
            }
        }

        public void TransferForensics(EntityUid spawned, DestructibleSystem system, EntityUid owner)
        {
            if (!DoTransferForensics ||
                !system.EntityManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
                return;

            var comp = system.EntityManager.EnsureComponent<ForensicsComponent>(spawned);
            comp.DNAs = forensicsComponent.DNAs;

            if (system.Random.NextFloat() >= 0.4f)
                return;

            comp.Fingerprints = forensicsComponent.Fingerprints;
            comp.Fibers = forensicsComponent.Fibers;
        }
    }
}
