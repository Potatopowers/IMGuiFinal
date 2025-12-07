
using System.Drawing;
using System.Windows.Forms;

namespace ProfileApp
{
    public static class Theme
    {
        // Replace these with your Figma color tokens
        public static class Colors
        {
            public static Color Background = ColorTranslator.FromHtml("#F7F8FA");
            public static Color Surface = ColorTranslator.FromHtml("#FFFFFF");
            public static Color Primary = ColorTranslator.FromHtml("#4F46E5"); // Indigo 600
            public static Color PrimaryDark = ColorTranslator.FromHtml("#3730A3"); // Indigo 800
            public static Color Accent = ColorTranslator.FromHtml("#10B981"); // Emerald 500
            public static Color Border = ColorTranslator.FromHtml("#E5E7EB"); // Gray 200
            public static Color TextPrimary = ColorTranslator.FromHtml("#111827"); // Gray 900
            public static Color TextMuted = ColorTranslator.FromHtml("#6B7280"); // Gray 500
            public static Color Danger = ColorTranslator.FromHtml("#EF4444"); // Red 500
        }

        // Replace these sizes/weights with your Figma type scale

        public static class TypeScale
        {
            public static Font Display() => new Font("Segoe UI", 24, FontStyle.Bold);
            public static Font Headline() => new Font("Segoe UI", 18, FontStyle.Bold);
            public static Font Title() => new Font("Segoe UI", 14, FontStyle.Bold);   // was SemiBold
            public static Font Body() => new Font("Segoe UI", 11, FontStyle.Regular);
            public static Font Label() => new Font("Segoe UI", 10, FontStyle.Regular);
            public static Font Button() => new Font("Segoe UI", 11, FontStyle.Bold);
        }


        public static void Apply(Form form)
        {
            form.BackColor = Colors.Background;
            form.ForeColor = Colors.TextPrimary;
        }

        // Standard “card” button style matching Figma components
        public static void StylePrimaryButton(Button b)
        {
            b.ForeColor = Color.White;
            b.BackColor = Colors.Primary;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = TypeScale.Button();
            b.Padding = new Padding(14, 10, 14, 10);
        }

        public static void StyleSecondaryButton(Button b)
        {
            b.ForeColor = Colors.TextPrimary;
            b.BackColor = Colors.Surface;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Colors.Border;
            b.FlatAppearance.BorderSize = 1;
            b.Font = TypeScale.Button();
            b.Padding = new Padding(14, 10, 14, 10);
        }

        public static void StyleCard(Button b)
        {
            b.BackColor = Colors.Surface;
            b.ForeColor = Colors.TextPrimary;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Colors.Border;
            b.FlatAppearance.BorderSize = 1;
            b.Font = TypeScale.Title();
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
