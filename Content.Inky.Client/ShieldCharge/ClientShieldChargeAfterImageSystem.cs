namespace Content.Inky.Client.ShieldCharge;

using Content.Inky.Common.ShieldCharge;
using Content.Inky.Shared.ShieldCharge;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

public sealed partial class ClientShieldChargeAfterimageSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> HideContextMenuTag = "HideContextMenu";

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var query1 = EntityQueryEnumerator<SharedActiveShieldChargeComponent, SharedShieldChargeComponent>();
        while (query1.MoveNext(out var uid, out var _, out var chargerComp))
        {
            if (chargerComp.User.HasValue)
            {
                var transform = Transform(chargerComp.User.Value);
                var coordinates = transform.Coordinates;
                var prevAfterimage = chargerComp.LastAfterimagePosition;
                var dist = Vector2.Distance(prevAfterimage, coordinates.Position);
                var dir = (coordinates.Position - prevAfterimage).Normalized();
                var pos = prevAfterimage;
                while (dist >= 0.6f)
                {
                    pos += dir * 0.6f;
                    dist -= 0.6f;
                    SpawnAfterimage(chargerComp.User.Value, new MapCoordinates(pos, transform.MapID), _timing.CurTime);
                }
                chargerComp.LastAfterimagePosition = pos;
            }
        }

        var query2 = EntityQueryEnumerator<ShieldChargeAfterimageComponent>();
        while (query2.MoveNext(out var uid, out var afterimageComp))
        {
            if (TryComp<SpriteComponent>(uid, out var afterimageSprite))
            {
                var alpha = (float) Math.Clamp(0.7 / 1f * (1f - (_timing.CurTime - afterimageComp.TimeCreated).TotalSeconds), 0f, 0.7f);
                if (alpha == 0f)
                {
                    QueueDel(uid);
                    continue;
                }
                _sprite.SetColor((uid, afterimageSprite), afterimageSprite.Color.WithAlpha(alpha));
                _sprite.ForceUpdate(uid);
            }
        }
    }

    private void SpawnAfterimage(EntityUid uid, MapCoordinates pos, TimeSpan timeCreated)
    {
        var xform = Transform(uid);
        var afterimage = Spawn(null, pos);

        var afterimageComp = EnsureComp<ShieldChargeAfterimageComponent>(afterimage);

        if (!TryComp<SpriteComponent>(uid, out var userSprite))
            return;

        _tagSystem.AddTag(afterimage, HideContextMenuTag);

        var afterimageSprite = EnsureComp<SpriteComponent>(afterimage);
        _sprite.CopySprite((uid, userSprite), (afterimage, afterimageSprite));
        _sprite.SetColor((afterimage, afterimageSprite), afterimageSprite.Color.WithAlpha(0.7f));
        afterimageSprite.EnableDirectionOverride = true;
        afterimageSprite.DirectionOverride = xform.LocalRotation.GetCardinalDir();
        afterimageComp.TimeCreated = timeCreated;
    }
}
