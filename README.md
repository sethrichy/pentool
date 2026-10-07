# Pen Tool for Rhino

A Rhino 8 command for drawing open or closed cubic Bezier paths with an
Illustrator-style Pen workflow.

## First-version interactions

- Click to place a corner anchor.
- Click and drag to place a smooth anchor with aligned handles.
- Click the first anchor to close the path.
- Press Enter to finish an open path or Esc to cancel.

The command creates a Rhino curve from the placed anchors. Build with the .NET
SDK and RhinoCommon 8, then load `src/PenTool/bin/Debug/net7.0/PenTool.rhp` in
Rhino.