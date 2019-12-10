using System.Collections.Generic;
using TabletDriverLib.Component;
using TabletDriverLib.Interop;
using TabletDriverLib.Interop.Cursor;
using TabletDriverLib.Tablet;

namespace TabletDriverLib.Output
{
    public class AbsoluteMode : OutputMode
    {
        private ICursorHandler CursorHandler { set; get; } = Platform.CursorHandler;
        private Area _display, _tablet;
        private TabletProperties _properties;

        public Area DisplayArea
        {
            set
            {
                _display = value;
                UpdateCache();
            }
            get => _display;
        }

        public Area TabletArea
        {
            set
            {
                _tablet = value;
                UpdateCache();
            }
            get => _tablet;
        }

        public override TabletProperties TabletProperties
        {
            set
            {
                _properties = value;
                UpdateCache();
            }
            get => _properties;
        }

        public bool Clipping { set; get; }
        public bool TipEnabled { set; get; }
        public float TipActivationPressure { set; get; }
        public Dictionary<(BindingType type, int index), MouseButton> Bindings { set; get; } = new Dictionary<(BindingType type, int index), MouseButton>()
        {
            { (BindingType.Tip, 0), MouseButton.None }
        };

        private float scaleX, scaleY, reportXOffset, reportYOffset;

        public void UpdateCache()
        {
            if (DisplayArea != null && TabletArea != null && TabletProperties != null)
            {
                scaleX = (DisplayArea.Width * TabletProperties.Width) / (TabletArea.Width * TabletProperties.MaxX);
                scaleY = (DisplayArea.Height * TabletProperties.Height) / (TabletArea.Height * TabletProperties.MaxY);
                reportXOffset = (TabletProperties.MaxX / TabletProperties.Width) * TabletArea.Position.X;
                reportYOffset = (TabletProperties.MaxY / TabletProperties.Height) * TabletArea.Position.Y;
            }
        }

        public override void Position(ITabletReport report)
        {
            var pos = new Point(
                (scaleX * (report.Position.X - reportXOffset)) + DisplayArea.Position.X,
                (scaleY * (report.Position.Y - reportYOffset)) + DisplayArea.Position.Y
            );

            if (Clipping)
            {
                // X position clipping
                if (pos.X > DisplayArea.Width + DisplayArea.Position.X)
                    pos.X = DisplayArea.Width + DisplayArea.Position.X;
                else if (pos.X < DisplayArea.Position.X)
                    pos.X = DisplayArea.Position.X;
                // Y position clipping
                if (pos.Y > DisplayArea.Height + DisplayArea.Position.Y)
                    pos.Y = DisplayArea.Height + DisplayArea.Position.Y;
                else if (pos.Y < DisplayArea.Position.Y)
                    pos.Y = DisplayArea.Position.Y;
            }

            CursorHandler.SetCursorPosition(pos);
            HandleButton(report);
        }

        public void HandleButton(ITabletReport report)
        {
            if (TipEnabled)
            {
                float pressurePercent = (float)report.Pressure / TabletProperties.MaxPressure * 100f;
                if (pressurePercent >= TipActivationPressure && !CursorHandler.GetMouseButtonState(Bindings[(BindingType.Pen, 0)]))
                    CursorHandler.MouseDown(Bindings[(BindingType.Pen, 0)]);
                else if (pressurePercent < TipActivationPressure && CursorHandler.GetMouseButtonState(Bindings[(BindingType.Pen, 0)]))
                    CursorHandler.MouseUp(Bindings[(BindingType.Pen, 0)]);
            }

            
        }
    }
}