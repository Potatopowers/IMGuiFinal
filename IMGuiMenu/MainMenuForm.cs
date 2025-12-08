
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProfileApp
{
    public class MainMenuForm : Form
    {
        private readonly LoginForm _loginForm;
        private readonly string _username;
        private TableLayoutPanel boxesLayout;

        public MainMenuForm(LoginForm loginForm, string username)
        {
            Theme.Apply(this);

            _loginForm = loginForm;
            _username = username;

            Text = "Main Menu";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 560);
            MinimumSize = new Size(720, 480);
            FormClosed += (_, __) => _loginForm.Show();

            var title = Theme.H1($"Hello, {_username}! Choose an option");

            // Create 4 cards
            boxesLayout = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                boxesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));


            string[] keys = { "Box1", "Box2", "Box3", "Box4" };
            for (int i = 0; i < keys.Length; i++)
            {
                var box = CreateMenuBox(keys[i], $"Box {i + 1}");
                boxesLayout.Controls.Add(box, i, 0);
            }


            var btnLogout = new Button { Text = "Logout"};
            Theme.StyleSecondaryButton(btnLogout);
            btnLogout.Click += (_, __) =>
            {
                Hide();
                _loginForm.Show();
            };


            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(24)
            };
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var headerWrap = centerWrap(title);
            container.Controls.Add(headerWrap, 0, 0);

            var centerPanel = new Panel { Dock = DockStyle.Fill };
            centerPanel.Controls.Add(boxesLayout);
            centerPanel.Resize += (_, __) => CenterBoxes();
            container.Controls.Add(centerPanel, 0, 1);

            var logoutWrap = centerWrap(btnLogout);
            container.Controls.Add(logoutWrap, 0, 2);

            Controls.Add(container);
            Shown += (_, __) => CenterBoxes();


        }


        private Button CreateMenuBox(string profileKey, string text)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(160, 160),
                Margin = new Padding(12)
            };
            Theme.StyleCard(btn);

            btn.Click += (_, __) =>
            {
                var about = new AboutMeForm(this, _username, profileKey); // <-- pass profileKey
                about.Show();
                Hide();
            };

            return btn;
        }


        private void CenterBoxes()
        {
            if (boxesLayout.Parent == null) return;
            var parent = boxesLayout.Parent;
            var desiredWidth = boxesLayout.PreferredSize.Width;
            var desiredHeight = boxesLayout.PreferredSize.Height;

            boxesLayout.Size = boxesLayout.PreferredSize;
            boxesLayout.Location = new Point(
                Math.Max(0, (parent.ClientSize.Width - desiredWidth) / 2),
                Math.Max(0, (parent.ClientSize.Height - desiredHeight) / 2)
            );
        }

        private Control centerWrap(Control child)
        {
            var w = new TableLayoutPanel
            {
                ColumnCount = 3,
                Dock = DockStyle.Top,
                AutoSize = true
            };
            w.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            w.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            w.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            w.Controls.Add(child, 1, 0);
            return w;
        }
    }
}
