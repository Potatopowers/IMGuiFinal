using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProfileApp
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;

        public LoginForm()
        {
            // Defer seeding until after UI controls are initialized.
            Text = "Login";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(600, 380);
            MinimumSize = new Size(520, 320);

            lblTitle = new Label
            {
                Text = "Welcome — Please Log In",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true
            };

            var lblUser = new Label { Text = "Username", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Regular) };
            var lblPass = new Label { Text = "Password", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Regular) };

            txtUsername = new TextBox { Width = 260, Font = new Font("Segoe UI", 10) };
            txtPassword = new TextBox { Width = 260, Font = new Font("Segoe UI", 10), UseSystemPasswordChar = true };

            btnLogin = new Button
            {
                Text = "Login",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true
            };
            btnLogin.Click += BtnLogin_Click;

            // Layout
            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(34),
            };
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); // Spacer
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Title
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // User
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Pass
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Button
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); // Spacer

            var lineUser = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            lineUser.Controls.Add(lblUser);
            lineUser.Controls.Add(Box(16));
            lineUser.Controls.Add(txtUsername);

            var linePass = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            linePass.Controls.Add(lblPass);
            linePass.Controls.Add(Box(16));
            linePass.Controls.Add(txtPassword);

            var headerPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            headerPanel.Controls.Add(lblTitle);

            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            buttonPanel.Controls.Add(btnLogin);

            formLayout.Controls.Add(Box(0));            // spacer
            formLayout.Controls.Add(centerWrap(headerPanel));
            formLayout.Controls.Add(centerWrap(lineUser));
            formLayout.Controls.Add(centerWrap(linePass));
            formLayout.Controls.Add(centerWrap(buttonPanel));
            formLayout.Controls.Add(Box(0));            // spacer

            Controls.Add(formLayout);

            AcceptButton = btnLogin; // Enter key submits

            // Optional: seed defaults for an empty username when the form is created.
            // Typically seeding is done after a user provides a username (on login), so we skip it here.
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter both username and password.", "Missing info",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var uname = txtUsername.Text.Trim();

            using (var db = new AppDb())
                db.SeedDefaultsForUserWithProfiles(uname);   // <-- NEW

            var mainMenu = new MainMenuForm(this, uname);
            mainMenu.Show();
            Hide();

        }

        // Helper: returns a blank control to add spacing
        private Control Box(int height) => new Panel { Height = height, Width = 0 };

        // Helper: centers a control horizontally using a wrapper
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
