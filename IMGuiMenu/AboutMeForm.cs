
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProfileApp
{
    public class AboutMeForm : Form
    {
        private readonly MainMenuForm _mainMenu;
        private readonly string _username;
        private readonly string _profileKey;   // which box/profile is open

        private PictureBox _picture;
        private Label _briefLabel;
        private Label _nameLabel;

        public AboutMeForm(MainMenuForm mainMenu, string username, string profileKey)
        {
            Theme.Apply(this);

            _mainMenu = mainMenu;
            _username = username;
            _profileKey = profileKey;

            Text = $"About Me — {_profileKey}";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(820, 540);

            // Load profile info for this (username, profileKey)
            string displayName, brief, photoPath;
            using (var db = new AppDb())
            {
                (displayName, brief, photoPath) = db.GetProfile(_username, _profileKey);
            }

            // Root split (left info, right picture)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // ---------- LEFT ----------
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // info
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // spacer
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // grid

            var title = Theme.H1("About Me");
            _nameLabel = Theme.BodyText(string.IsNullOrWhiteSpace(displayName) ? _profileKey : displayName);
            _briefLabel = Theme.BodyText(brief);

            var btnEditProfile = new Button { Text = "Edit Profile" };
            Theme.StyleSecondaryButton(btnEditProfile);
            btnEditProfile.Click += BtnEditProfile_Click;

            var infoPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true
            };
            infoPanel.Controls.Add(title);
            infoPanel.Controls.Add(_nameLabel);
            infoPanel.Controls.Add(_briefLabel);
            infoPanel.Controls.Add(btnEditProfile);

            // >>> Declare grid BEFORE using it <<<
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Add the four navigation buttons (each uses the same profileKey)

            grid.Controls.Add(CreateInfoButton("Education", () =>
                NavigateTo(new EducationForm(this, _username, _profileKey))), 0, 0);

            grid.Controls.Add(CreateInfoButton("Hobbies", () =>
                NavigateTo(new HobbiesForm(this, _username, _profileKey))), 1, 0);

            grid.Controls.Add(CreateInfoButton("Skills", () =>
                NavigateTo(new SkillsForm(this, _username, _profileKey))), 0, 1);

            grid.Controls.Add(CreateInfoButton("Message about kay Sir Bill", () =>
                NavigateTo(new MessageSirBillForm(this, _username, _profileKey))), 1, 1);


            left.Controls.Add(infoPanel, 0, 0);
            left.Controls.Add(new Panel { Height = 12, Dock = DockStyle.Top }, 0, 1);
            left.Controls.Add(grid, 0, 2);

            // ---------- RIGHT ----------
            var right = new Panel { Dock = DockStyle.Fill };
            _picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.Colors.Surface
            };
            LoadProfileImage(photoPath);
            right.Controls.Add(_picture);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);

            // Top bar with Back to Main Menu
            var back = new Button { Text = "← Back to Main Menu" };
            Theme.StyleSecondaryButton(back);
            back.Click += (_, __) =>
            {
                Hide();
                _mainMenu.Show();
            };

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(24),
                AutoSize = true
            };
            topBar.Controls.Add(back);

            Controls.Add(root);
            Controls.Add(topBar);

            // If user closes About Me, go back to main
            FormClosed += (_, __) => _mainMenu.Show();
        }

        private void NavigateTo(Form page)
        {
            page.Show();
            Hide();
        }

        private Button CreateInfoButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = false,
                Size = new Size(150,80),
                Padding = new Padding(16),
                Margin = new Padding(8)
            };
            Theme.StylePrimaryButton(btn);
            btn.Click += (_, __) => onClick();
            return btn;
        }

        private void LoadProfileImage(string photoPath)
        {
            if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
            {
                try
                {
                    _picture.Image = Image.FromFile(photoPath);
                    return;
                }
                catch { /* fall through to placeholder */ }
            }

            _picture.Image = DetailForm.CreatePlaceholderProfileImage(
                512, 512, DetailForm.GetInitials(_username));
        }

        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            using var db = new AppDb();
            var (displayName, brief, photoPath) = db.GetProfile(_username, _profileKey);

            // Edit brief
            using (var dlgBrief = new EditDialog("Edit Profile Brief", brief))
            {
                if (dlgBrief.ShowDialog(this) == DialogResult.OK)
                    brief = dlgBrief.EditedText;
            }

            // Edit display name
            using (var dlgName = new EditDialog("Edit Display Name",
                       string.IsNullOrWhiteSpace(displayName) ? _profileKey : displayName))
            {
                if (dlgName.ShowDialog(this) == DialogResult.OK)
                    displayName = dlgName.EditedText;
            }

            // Optional: choose photo
            using (var ofd = new OpenFileDialog
            {
                Title = "Select Profile Picture (optional)",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                CheckFileExists = true
            })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    photoPath = ofd.FileName;
            }

            db.SaveProfile(_username, _profileKey, displayName, brief, photoPath);
            _nameLabel.Text = string.IsNullOrWhiteSpace(displayName) ? _profileKey : displayName;
            _briefLabel.Text = brief;
            LoadProfileImage(photoPath);
        }
    }
}
