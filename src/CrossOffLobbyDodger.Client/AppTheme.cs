using System.Drawing;

namespace CrossOff.LobbyDodger;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(15, 17, 17);
    public static readonly Color Surface = Color.FromArgb(24, 27, 27);
    public static readonly Color SurfaceHover = Color.FromArgb(31, 36, 35);
    public static readonly Color Border = Color.FromArgb(58, 65, 63);
    public static readonly Color Text = Color.FromArgb(238, 242, 240);
    public static readonly Color MutedText = Color.FromArgb(166, 176, 171);
    public static readonly Color Emerald = Color.FromArgb(81, 211, 150);
    public static readonly Color EmeraldSurface = Color.FromArgb(25, 62, 47);
    public static readonly Color Warning = Color.FromArgb(244, 194, 92);
    public static readonly Color Danger = Color.FromArgb(241, 104, 104);

    public static Font UiFont(float size = 9.5f, FontStyle style = FontStyle.Regular)
    {
        FontFamily family = SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif;
        return new Font(family, size, style);
    }

    public static void StyleButton(Button button, bool primary = false, AppGlyph? glyph = null)
    {
        button.AutoSize = false;
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.ForeColor = primary ? Color.FromArgb(9, 24, 17) : Text;
        button.BackColor = primary ? Emerald : SurfaceHover;
        button.FlatAppearance.BorderColor = primary ? Emerald : Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary
            ? Color.FromArgb(106, 225, 169)
            : Color.FromArgb(39, 45, 43);
        button.FlatAppearance.MouseDownBackColor = primary
            ? Color.FromArgb(63, 188, 130)
            : Color.FromArgb(20, 23, 23);

        if (glyph is not null)
        {
            Image? previous = button.Image;
            button.Image = AppGlyphs.Create(glyph.Value, button.ForeColor);
            previous?.Dispose();
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
        }
    }

    public static void StyleModeOption(RadioButton option)
    {
        option.Appearance = Appearance.Button;
        option.AutoCheck = true;
        option.FlatStyle = FlatStyle.Flat;
        option.UseVisualStyleBackColor = false;
        option.Cursor = Cursors.Hand;
        option.TextAlign = ContentAlignment.MiddleLeft;
        option.Padding = new Padding(12, 5, 12, 5);
        option.ForeColor = Text;
        option.BackColor = Surface;
        option.FlatAppearance.BorderColor = Border;
        option.FlatAppearance.BorderSize = 1;
        option.FlatAppearance.MouseOverBackColor = SurfaceHover;
        option.FlatAppearance.MouseDownBackColor = EmeraldSurface;
        option.FlatAppearance.CheckedBackColor = EmeraldSurface;
    }

    public static void RefreshModeOption(RadioButton option)
    {
        option.ForeColor = Text;
        option.BackColor = option.Checked ? EmeraldSurface : Surface;
        option.FlatAppearance.BorderColor = option.Checked ? Emerald : Border;
    }
}
