using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Inky.Client.ShieldCharge;

/// <summary>
/// Component for afterimage entities spawned by Sandevistan users.
/// </summary>
[RegisterComponent]
public sealed partial class ShieldChargeAfterimageComponent : Component
{
    [DataField]
    public TimeSpan TimeCreated;

}
