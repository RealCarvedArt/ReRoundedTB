using RoundedTB;
using Xunit;

namespace RoundedTB.Tests
{
    // rtb.json is user-editable and loaded on every start, so a corrupt or hostile file must never crash the app (audit finding L1)
    public class SettingsTests
    {
        private const int AutoHideOptions = 3;

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("[1,2]")]
        [InlineData("{\"AutoHide\":\"abc\"}")]
        [InlineData("{\"AutoHide\":1e40}")]
        [InlineData("{\"AutoHide\":99999999999}")]
        [InlineData("{not json")]
        public void CorruptJsonFallsBackToDefaults(string json)
        {
            Assert.Null(Interaction.ParseSettings(json));
        }

        [Fact]
        public void DeeplyNestedJsonFallsBackToDefaults()
        {
            Assert.Null(Interaction.ParseSettings(new string('[', 200) + new string(']', 200)));
        }

        [Fact]
        public void TypeNameIsIgnored()
        {
            // Only plain ints and bools are deserialised; a $type hint must not instantiate anything else
            Types.Settings settings = Interaction.ParseSettings("{\"$type\":\"System.IO.FileInfo, System.IO\",\"AutoHide\":1}");
            Assert.NotNull(settings);
            Assert.Equal(1, settings.AutoHide);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(3)]
        [InlineData(99)]
        [InlineData(int.MinValue)]
        public void OutOfRangeAutoHideResetsToAlwaysShow(int autoHide)
        {
            var settings = new Types.Settings { AutoHide = autoHide };
            settings.Normalize(isWindows11: true, AutoHideOptions);
            Assert.Equal(0, settings.AutoHide);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ValidAutoHideIsKept(int autoHide)
        {
            var settings = new Types.Settings { AutoHide = autoHide };
            settings.Normalize(isWindows11: true, AutoHideOptions);
            Assert.Equal(autoHide, settings.AutoHide);
        }

        [Fact]
        public void ExtremeLayoutValuesAreClampedAndCantOverflowScaling()
        {
            Types.Settings settings = Interaction.ParseSettings(
                "{\"SimpleTaskbarLayout\":{\"CornerRadius\":-7,\"MarginTop\":2147483647,\"MarginLeft\":-2147483648,\"MarginBottom\":5,\"MarginRight\":-3}}");
            settings.Normalize(isWindows11: true, AutoHideOptions);

            Types.SegmentSettings layout = settings.SimpleTaskbarLayout;
            Assert.Equal(0, layout.CornerRadius);
            Assert.Equal(Types.SegmentSettings.MaxMargin, layout.MarginTop);
            Assert.Equal(Types.SegmentSettings.MinMargin, layout.MarginLeft);
            Assert.Equal(5, layout.MarginBottom);
            Assert.Equal(-3, layout.MarginRight); // negative margins are a feature and must survive
            // Region maths multiplies by the monitor scale factor (up to 5x) and converts to int
            System.Convert.ToInt32(layout.MarginTop * 5.0);
            System.Convert.ToInt32(layout.MarginLeft * 5.0);
        }

        [Fact]
        public void MissingLayoutsFromOldConfigsGetDefaults()
        {
            // Configs from R3.1 and earlier have no per-segment layouts
            Types.Settings settings = Interaction.ParseSettings("{\"IsDynamic\":true}");
            settings.Normalize(isWindows11: true, AutoHideOptions);

            Assert.NotNull(settings.SimpleTaskbarLayout);
            Assert.NotNull(settings.DynamicAppListLayout);
            Assert.NotNull(settings.DynamicTrayLayout);
            Assert.NotNull(settings.DynamicWidgetsLayout);
            Assert.Equal(7, settings.DynamicTrayLayout.CornerRadius);
        }

        [Fact]
        public void Windows10DefaultsDifferFromWindows11()
        {
            var settings = new Types.Settings();
            settings.Normalize(isWindows11: false, AutoHideOptions);
            Assert.Equal(16, settings.SimpleTaskbarLayout.CornerRadius);
            Assert.Equal(2, settings.SimpleTaskbarLayout.MarginTop);
        }

        [Theory]
        [InlineData(true, 7, 3, true)]
        [InlineData(false, 16, 2, false)]
        public void DefaultsMatchThePreviousHardCodedValues(bool isWindows11, int radius, int margin, bool fillOnTaskSwitch)
        {
            Types.Settings settings = Types.Settings.CreateDefault(isWindows11);

            foreach (Types.SegmentSettings layout in new[] { settings.SimpleTaskbarLayout, settings.DynamicAppListLayout, settings.DynamicTrayLayout, settings.DynamicWidgetsLayout })
            {
                Assert.Equal(radius, layout.CornerRadius);
                Assert.Equal(margin, layout.MarginTop);
                Assert.Equal(margin, layout.MarginLeft);
                Assert.Equal(margin, layout.MarginBottom);
                Assert.Equal(margin, layout.MarginRight);
            }
            Assert.Equal(isWindows11, settings.IsWindows11);
            Assert.True(settings.FillOnMaximise);
            Assert.Equal(fillOnTaskSwitch, settings.FillOnTaskSwitch);
            Assert.False(settings.IsDynamic);
            Assert.False(settings.IsCentred);
            Assert.False(settings.ShowTray);
            Assert.False(settings.ShowWidgets);
            Assert.False(settings.CompositionCompat);
            Assert.False(settings.IsNotFirstLaunch);
            Assert.False(settings.ShowSegmentsOnHover);
            Assert.False(settings.HasSeenTrayNotice);
            Assert.Equal(0, settings.AutoHide);
        }

        [Fact]
        public void ValidSettingsRoundTripUnchanged()
        {
            const string json = "{\"Version\":3,\"SimpleTaskbarLayout\":{\"CornerRadius\":12,\"MarginTop\":4,\"MarginLeft\":-1,\"MarginBottom\":4,\"MarginRight\":0},\"IsDynamic\":false,\"AutoHide\":1}";
            Types.Settings settings = Interaction.ParseSettings(json);
            settings.Normalize(isWindows11: true, AutoHideOptions);

            Assert.Equal(12, settings.SimpleTaskbarLayout.CornerRadius);
            Assert.Equal(-1, settings.SimpleTaskbarLayout.MarginLeft);
            Assert.Equal(1, settings.AutoHide);
        }
    }
}
