
using System.Drawing;
using System.Windows.Forms;

namespace ProfileApp
{
    public static class Theme
    {
        // Make sure these are 6-digit hex codes (#RRGGBB). Do NOT use #AARRGGBB.
        public static class Colors
        {
            public static Color Background = ColorTranslator.FromHtml("#F7F8FA");
            public static Color Surface = ColorTranslator.FromHtml("#FFFFFF");
            public static Color Primary = ColorTranslator.FromHtml("#4F46E5"); // Indigo 600
            public static Color PrimaryDark = ColorTranslator.FromHtml("#3730A3"); // Indigo 800
            public static Color Accent = ColorTranslator.FromHtml("#10B981"); // Emerald 500
            public static Color Border = ColorTranslator.FromHtml("#E5E7EB"); // Gray 200
            public static Color TextPrimary = ColorTranslator.FromHtml("#111827"); // Gray 900 (dark)
            public static Color TextMuted = ColorTranslator.FromHtml("#6B7280"); // Gray 500
            public static Color Danger = ColorTranslator.FromHtml("#EF4444"); // Red 500
        }

        public static class TypeScale
        {
            // Use WinForms-supported styles only (Regular, Bold, Italic, etc.).
            public static Font Display() => new Font("Segoe UI", 24, FontStyle.Bold);
            public static Font Headline() => new Font("Segoe UI", 18, FontStyle.Bold);
            public static Font Title() => new Font("Segoe UI", 14, FontStyle.Bold);
            public static Font Body() => new Font("Segoe UI", 11, FontStyle.Regular);
            public static Font Label() => new Font("Segoe UI", 10, FontStyle.Regular);
            public static Font Button() => new Font("Segoe UI", 11, FontStyle.Bold);
        }

        public static void Apply(Form form)
        {
            form.BackColor = Colors.Background;
            form.ForeColor = Colors.TextPrimary;
        }

        public static void StylePrimaryButton(Button b)
        {
            b.UseVisualStyleBackColor = false;        // critical
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;

            b.BackColor = Colors.Primary;
            b.ForeColor = Color.White;                // ensure contrast
            b.Font = TypeScale.Button();
            b.Padding = new Padding(14, 10, 14, 10);
            b.TextAlign = ContentAlignment.MiddleCenter;

        }

        public static void StyleSecondaryButton(Button b)
        {
            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;

            b.BackColor = Colors.PrimaryDark;       // darker fill
            b.ForeColor = Color.White;              // white text always crisp
            b.Font = TypeScale.Button();

            b.TextAlign = ContentAlignment.MiddleCenter;
            b.AutoSize = false;
            b.Size = new Size(80, 30);
        }

        public static void StyleCard(Button b)
        {
            b.UseVisualStyleBackColor = false;        // critical
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Colors.Border;
            b.FlatAppearance.BorderSize = 1;

            b.BackColor = Colors.Surface;
            b.ForeColor = Colors.TextPrimary;
            b.Font = TypeScale.Title();
            b.TextAlign = ContentAlignment.MiddleCenter;
        }

        public static Label H1(string text) => new Label
        {
            Text = text,
            Font = TypeScale.Headline(),
            ForeColor = Colors.TextPrimary,
            AutoSize = true
        };

        public static Label BodyText(string text) => new Label
        {
            Text = text,
            Font = TypeScale.Body(),
            ForeColor = Colors.TextMuted,
            AutoSize = true
        };
    }
}
