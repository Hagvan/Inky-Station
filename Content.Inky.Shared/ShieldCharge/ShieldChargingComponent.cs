using Robust.Shared.GameStates;

namespace Content.Inky.Shared.ShieldCharge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShieldChargingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid ChargeProvider;
}
