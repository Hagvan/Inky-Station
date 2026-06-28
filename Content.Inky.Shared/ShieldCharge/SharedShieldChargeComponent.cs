using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Inky.Common.ShieldCharge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SharedShieldChargeComponent : Component
{
    // Start-end controls
    [DataField, AutoNetworkedField]
    public bool IsCharging = false;
    [DataField, AutoNetworkedField]
    public TimeSpan ChargeStartTime;
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2.5f);
    [DataField, AutoNetworkedField]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(0.3f);

    // Charge characteristics
    [DataField, AutoNetworkedField]
    public float TurnRate = 110f;

    // Technical stuff - update method
    [DataField, AutoNetworkedField]
    public TimeSpan PrevTime;
    [DataField, AutoNetworkedField]
    public Angle ChargeDirection;


    // Technical stuff - actions
    [DataField, AutoNetworkedField]
    public EntProtoId ShieldChargeAction = "ActionShieldCharge";
    [DataField, AutoNetworkedField]
    public EntityUid? ShieldChargeActionEntity;
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public float ChargeVelocity = 24f;

    // Only relevant for client
    public Vector2 LastAfterimagePosition;
}
