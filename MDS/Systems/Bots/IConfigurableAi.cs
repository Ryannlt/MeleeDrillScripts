using System.Collections.Generic;

namespace MDS.Systems
{
    // An IBotAi exposing named levers, so a bot can be tuned live with 'rc bot cfg'.
    public interface IConfigurableAi
    {
        // Set one lever. False with an error for an unknown name or an unparseable value.
        bool TrySet(string name, string value, out string message);

        // The current levers and values for 'rc bot cfg <target>', each tagged with the gate holding it dormant.
        List<(string name, string value, string inactive)> ListParams();
    }
}
