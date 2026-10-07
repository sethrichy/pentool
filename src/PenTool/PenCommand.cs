using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace PenTool;

public sealed class PenCommand : Command
{
    public override string EnglishName => "PenTool";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var anchors = new List<Anchor>();

        while (true)
        {
            var getter = new AnchorGetter(
                anchors,
                doc.ModelAbsoluteTolerance,
                anchors.Count == 0
                    ? "Place the first anchor"
                    : "Place an anchor, click the first anchor to close, or press Enter to finish");
            getter.AcceptNothing(true);
            var result = getter.Get();

            if (result == GetResult.Nothing)
                break;
            if (result != GetResult.Point)
                return Result.Cancel;

            var anchor = getter.GetAnchor();
            if (anchors.Count > 1 && anchor.Point.DistanceTo(anchors[0].Point) <= doc.ModelAbsoluteTolerance)
            {
                anchors.Add(anchor with { Point = anchors[0].Point });
                break;
            }

            anchors.Add(anchor);
        }

        if (anchors.Count < 2)
            return Result.Nothing;

        var curve = BuildCurve(anchors, doc.ModelAbsoluteTolerance);
        if (curve is null)
            return Result.Failure;

        var curveId = doc.Objects.AddCurve(curve);
        if (curveId == Guid.Empty)
            return Result.Failure;

        doc.Objects.FindId(curveId)?.Select(true);
        doc.Views.Redraw();
        RhinoApp.RunScript("_PointsOn", false);
        return Result.Success;
    }

    private static Curve? BuildCurve(IReadOnlyList<Anchor> anchors, double tolerance)
    {
        var segmentCount = anchors.Count - 1;
        var segments = new List<Curve>(segmentCount);

        for (var index = 0; index < segmentCount; index++)
        {
            var start = anchors[index];
            var end = anchors[index + 1];
            var bezier = new BezierCurve(new[]
            {
                start.Point,
                start.Point + start.Handle,
                end.Point - end.Handle,
                end.Point
            });
            segments.Add(bezier.ToNurbsCurve());
        }

        return Curve.JoinCurves(segments, tolerance).FirstOrDefault();
    }

    private sealed record Anchor(Point3d Point, Vector3d Handle);

    private sealed class AnchorGetter : GetPoint
    {
        private readonly IReadOnlyList<Anchor> _anchors;
        private readonly double _tolerance;
        private Point3d _mouseDownPoint;
        private bool _hasMouseDown;

        public AnchorGetter(IReadOnlyList<Anchor> anchors, double tolerance, string prompt)
        {
            _anchors = anchors;
            _tolerance = tolerance;
            SetCommandPrompt(prompt);
            if (anchors.Count > 1)
                AddSnapPoint(anchors[0].Point);
        }

        public Anchor GetAnchor()
        {
            var point = Point();
            if (!_hasMouseDown)
                return new Anchor(point, Vector3d.Zero);

            var dragEnd = _hasMouseMove ? _lastMousePoint : point;
            var handle = dragEnd - _mouseDownPoint;
            return handle.Length > _tolerance
                ? new Anchor(_mouseDownPoint, handle)
                : new Anchor(point, Vector3d.Zero);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);
            var cursorPoint = e.CurrentPoint;
            if (_anchors.Count > 1 && cursorPoint.DistanceTo(_anchors[0].Point) <= _tolerance)
                cursorPoint = _anchors[0].Point;

            var point = _hasMouseDown ? _mouseDownPoint : cursorPoint;
            var handle = _hasMouseDown ? cursorPoint - _mouseDownPoint : Vector3d.Zero;
            if (_anchors.Count == 0)
            {
                DrawHandles(e, point, handle);
                return;
            }

            foreach (var anchor in _anchors)
                DrawHandles(e, anchor.Point, anchor.Handle);
            DrawHandles(e, point, handle);

            var previewAnchors = _anchors.Append(new Anchor(point, handle)).ToArray();
            var preview = BuildCurve(previewAnchors, _tolerance);
            if (preview is not null)
                e.Display.DrawCurve(preview, System.Drawing.Color.DarkCyan, 2);
        }

        private static void DrawHandles(GetPointDrawEventArgs e, Point3d point, Vector3d handle)
        {
            if (handle.IsTiny())
                return;

            var incoming = point - handle;
            var outgoing = point + handle;
            var color = System.Drawing.Color.FromArgb(255, 90, 155, 190);
            e.Display.DrawLine(incoming, outgoing, color, 1);
            e.Display.DrawPoint(point, color);
            e.Display.DrawPoint(incoming, color);
            e.Display.DrawPoint(outgoing, color);
        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!e.LeftButtonDown)
                return;

            _mouseDownPoint = e.Point;
            _lastMousePoint = e.Point;
            _hasMouseDown = true;
            _hasMouseMove = false;
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_hasMouseDown)
                return;

            _lastMousePoint = e.Point;
            _hasMouseMove = true;
        }

        private Point3d _lastMousePoint;
        private bool _hasMouseMove;

    }
}