using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoundedTB
{
    public class Types
    {
        public class Taskbar
        {
            public IntPtr TaskbarHwnd { get; set; } // Handle to the taskbar
            public IntPtr TrayHwnd { get; set; } // Handle to the tray on the taskbar (if present)
            public IntPtr AppListHwnd { get; set; } // Handle to the list of open/pinned apps on the taskbar
            public LocalPInvoke.RECT TaskbarRect { get; set; } // Bounding box for the taskbar
            public LocalPInvoke.RECT TrayRect { get; set; }  // Bounding box for the tray (dynamic)
            public LocalPInvoke.RECT AppListRect { get; set; } // Bounding box for the list of pinned & open apps (dynamic)
            public double ScaleFactor { get; set; } // The scale factor of the monitor the taskbar is on
            public string TaskbarRes { get; set; } // Resolution of the taskbar as text
            public bool Ignored { get; set; } // Specifies if the taskbar should be ignored when applying changes
            public bool TaskbarHidden { get; set; } // Specifies if this taskbar is currently hidden by RTB
            public bool TrayHidden { get; set; } // Specifies if the tray is currently hidden by RTB on this taskbar
            public int AppListWidth { get; set; } // Specifies the width of the app list
        }

        public class Settings
        {
            public int Version {  get; set; }
            public SegmentSettings SimpleTaskbarLayout { get; set; }
            public SegmentSettings DynamicAppListLayout { get; set; }
            public SegmentSettings DynamicTrayLayout { get; set; }
            public SegmentSettings DynamicWidgetsLayout { get; set; }
            public bool IsDynamic { get; set; }
            public bool IsCentred { get; set; }
            public bool IsWindows11 { get; set; }
            public bool ShowTray { get; set; }
            public bool ShowWidgets { get; set; }
            public bool CompositionCompat { get; set; }
            public bool IsNotFirstLaunch { get; set; }
            public bool FillOnMaximise { get; set; }
            public bool FillOnTaskSwitch {  get; set; }
            public bool ShowSegmentsOnHover { get; set; }
            public int AutoHide { get; set; }

            /// <summary>
            /// Makes settings read from rtb.json safe to use. The file is user-editable, so out-of-range values are brought back in range
            /// rather than crashing on every start, and configs from R3.1 and earlier (no per-segment layouts) get default layouts.
            /// </summary>
            public void Normalize(bool isWindows11, int autoHideOptionCount)
            {
                SegmentSettings DefaultLayout() => isWindows11
                    ? new SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 }
                    : new SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 };
                SimpleTaskbarLayout ??= DefaultLayout();
                DynamicAppListLayout ??= DefaultLayout();
                DynamicTrayLayout ??= DefaultLayout();
                DynamicWidgetsLayout ??= DefaultLayout();

                SimpleTaskbarLayout.Clamp();
                DynamicAppListLayout.Clamp();
                DynamicTrayLayout.Clamp();
                DynamicWidgetsLayout.Clamp();
                if (AutoHide < 0 || AutoHide >= autoHideOptionCount)
                {
                    AutoHide = 0;
                }
            }
        }

        public class EffectiveRegion
        {
            public int CornerRadius { get; set; }
            public int Top { get; set; }
            public int Left { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class SegmentSettings
        {
            // Generous bounds (negative margins are a feature) that only exist to stop a hand-edited or corrupt rtb.json from overflowing the region maths
            public const int MaxCornerRadius = 10000;
            public const int MinMargin = -10000;
            public const int MaxMargin = 10000;

            public int CornerRadius { get; set; }
            public int MarginTop { get; set; }
            public int MarginLeft { get; set; }
            public int MarginBottom { get; set; }
            public int MarginRight { get; set; }

            public void Clamp()
            {
                CornerRadius = Math.Clamp(CornerRadius, 0, MaxCornerRadius);
                MarginTop = Math.Clamp(MarginTop, MinMargin, MaxMargin);
                MarginLeft = Math.Clamp(MarginLeft, MinMargin, MaxMargin);
                MarginBottom = Math.Clamp(MarginBottom, MinMargin, MaxMargin);
                MarginRight = Math.Clamp(MarginRight, MinMargin, MaxMargin);
            }
        }

        public enum TrayMode
        {
            Show = 0,
            Hide = 1,
            AutoHide = 2,
        }

        public enum CompositionMode
        {
            None = 0,
            TranslucentTB = 1,
            Legacy = 2,
        }

        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }
    }
}
