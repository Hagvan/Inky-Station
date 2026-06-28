using Content.Inky.Common.ShieldCharge;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Inky.Shared.ShieldCharge;

public sealed partial class SharedShieldChargeSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedShieldChargeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SharedShieldChargeComponent, ShieldChargeEvent>(OnAction);

        SubscribeLocalEvent<SharedShieldChargeComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<SharedShieldChargeComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<SharedShieldChargeComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<SharedShieldChargeComponent, UseInHandEvent>(OnUseInHand);

        SubscribeAllEvent<ShieldBashRequestEvent>(OnShieldBashRequest);

        if (_net.IsClient)
            SubscribeLocalEvent<ShieldChargingComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<ShieldChargingComponent> ent, ref StartCollideEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var other = args.OtherEntity;

        if (TryComp<SharedShieldChargeComponent>(ent.Comp.ChargeProvider, out var chargeComp) && HasComp<InputMoverComponent>(other))
        {
            RaisePredictiveEvent(new ShieldBashRequestEvent(GetNetEntity(other), GetNetEntity(ent.Comp.ChargeProvider)));
        }
    }

    private void OnUseInHand(Entity<SharedShieldChargeComponent> ent, ref UseInHandEvent args)
    {
        StartCharge(ent.Owner, ent.Comp);
    }

    private void OnShieldBashRequest(ShieldBashRequestEvent ev)
    {
        var target = GetEntity(ev.Target);
        var chargeProvider = GetEntity(ev.ChargeProvider);
        if (TryComp<SharedShieldChargeComponent>(chargeProvider, out var chargeComp) &&
            chargeComp.User.HasValue &&
            TryComp<InputMoverComponent>(chargeComp.User.Value, out var inputMoverComponent))
        {
            if (_net.IsServer)
                _stamina.TakeStaminaDamage(target, chargeComp.ChargeVelocity * 8f * (float) (_timing.CurTime - chargeComp.ChargeStartTime).TotalSeconds);
            inputMoverComponent.CanMove = true;
            Dirty(chargeComp.User.Value, inputMoverComponent);
            EndCharge(chargeProvider, chargeComp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SharedActiveShieldChargeComponent, SharedShieldChargeComponent>();
        while (query.MoveNext(out var uid, out var _, out var chargerComp))
        {
            if (chargerComp.IsCharging)
            {
                if (TryComp<TransformComponent>(chargerComp.User, out var transformComponent)
                    && TryComp<PhysicsComponent>(chargerComp.User, out var physicsComponent))
                {
                    var desiredAngle = transformComponent.LocalRotation;
                    if (!desiredAngle.EqualsApprox(chargerComp.ChargeDirection))
                    {
                        var delta = Angle.ShortestDistance(chargerComp.ChargeDirection, desiredAngle);
                        var maxTurn = Angle.FromDegrees(chargerComp.TurnRate * (_timing.CurTime - chargerComp.PrevTime).TotalSeconds);

                        if (delta > maxTurn)
                        {
                            delta = maxTurn;
                        }
                        else if (delta <= -maxTurn)
                        {
                            delta = -maxTurn;
                        }

                        chargerComp.ChargeDirection += delta;
                    }

                    if (_timing.CurTime > chargerComp.ChargeStartTime + chargerComp.GracePeriod
                        && physicsComponent.LinearVelocity.Length() < 1f
                        || _timing.CurTime > chargerComp.ChargeStartTime + chargerComp.Duration)
                    {
                        EndCharge(uid, chargerComp);
                        continue;
                    }

                    chargerComp.PrevTime = _timing.CurTime;

                    Dirty(uid, chargerComp);
                    _physics.SetLinearVelocity(chargerComp.User.Value, chargerComp.ChargeDirection.ToWorldVec() * chargerComp.ChargeVelocity);
                }
            }
        }
    }

    private void OnDrop(Entity<SharedShieldChargeComponent> ent, ref DroppedEvent args)
    {
        EndCharge(ent.Owner, ent.Comp);
        ent.Comp.User = null;
    }

    private void OnUnequip(Entity<SharedShieldChargeComponent> ent, ref GotUnequippedHandEvent args)
    {
        EndCharge(ent.Owner, ent.Comp);
        ent.Comp.User = null;
    }

    private void OnEquip(Entity<SharedShieldChargeComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.User = args.User;
    }

    private void OnAction(Entity<SharedShieldChargeComponent> ent, ref ShieldChargeEvent args)
    {
        StartCharge(ent.Owner, ent.Comp);
    }

    private void OnGetActions(EntityUid uid, SharedShieldChargeComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ShieldChargeActionEntity, component.ShieldChargeAction);
    }


    private void StartCharge(EntityUid uid, SharedShieldChargeComponent component)
    {
        if (TryComp<CombatModeComponent>(component.User, out var comp) && comp.IsInCombatMode)
        {
            if (TryComp<InputMoverComponent>(component.User, out var inputMoverComponent))
            {
                inputMoverComponent.CanMove = false;
                Dirty(component.User.Value, inputMoverComponent);
                component.IsCharging = true;
                component.ChargeStartTime = _timing.CurTime;
                component.PrevTime = _timing.CurTime;
                if (TryComp<TransformComponent>(component.User, out var transformComponent))
                    component.ChargeDirection = transformComponent.LocalRotation;
                var chargingComp = EnsureComp<ShieldChargingComponent>(component.User.Value);
                chargingComp.ChargeProvider = uid;
                EnsureComp<SharedActiveShieldChargeComponent>(uid);
                Dirty(uid, component);
            }
        }
    }

    private void EndCharge(EntityUid uid, SharedShieldChargeComponent component)
    {
        if (TryComp<InputMoverComponent>(component.User, out var inputMoverComponent))
        {
            inputMoverComponent.CanMove = true;
            Dirty(component.User.Value, inputMoverComponent);
        }
        component.IsCharging = false;
        RemComp<SharedActiveShieldChargeComponent>(uid);
        if (component.User.HasValue)
        {
            RemComp<ShieldChargingComponent>(component.User.Value);
        }
        Dirty(uid, component);
    }
}

[Serializable, NetSerializable]
public sealed class ShieldBashRequestEvent : EntityEventArgs
{
    public NetEntity Target;
    public NetEntity ChargeProvider;

    public ShieldBashRequestEvent(NetEntity target, NetEntity chargeProvider)
    {
        Target = target;
        ChargeProvider = chargeProvider;
    }
}
