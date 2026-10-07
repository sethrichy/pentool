using Rhino.PlugIns;

namespace PenTool;

public sealed class PenToolPlugin : PlugIn
{
    public PenToolPlugin()
    {
        Instance = this;
    }

    public static PenToolPlugin? Instance { get; private set; }
}