
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
        private PictureBox _picture;
        private Label _briefLabel;

        public AboutMeForm(MainMenuForm mainMenu, string username)
        {
            Theme.Apply(this);

            _mainMenu = mainMenu;
            _username = username;

            Text = "About Me";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(820, 540);

            // Load profile from DB
            string brief, photoPath;
            using (var db = new AppDb())
            {
                (brief, photoPath) = db.GetProfile(_username);
            }

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // LEFT HALF
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // brief info
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // spacer
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 4 buttons grid

            var title = Theme.H1("About Me");

            _briefLabel = Theme.BodyText(brief);

            // Edit profile button (brief + photo path)
            var btnEditProfile = new Button { Text = "Edit Profile" };
            Theme.StyleSecondaryButton(btnEditProfile);
            btnEditProfile.Click += BtnEditProfile_Click;

            var infoPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            infoPanel.Controls.Add(title);
            infoPanel.Controls.Add(_briefLabel);
            infoPanel.Controls.Add(btnEditProfile);

            // 2x2 grid of navigation buttons
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true
            };
            for (int i = 0; i < 2; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 2; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));


            grid.Controls.Add(CreateInfoButton("Education", () => NavigateTo(new EducationForm(this, _username))), 0, 0);
            grid.Controls.Add(CreateInfoButton("Hobbies", () => NavigateTo(new HobbiesForm(this, _username))), 1, 0);
            grid.Controls.Add(CreateInfoButton("Skills", () => NavigateTo(new SkillsForm(this, _username))), 0, 1);
            grid.Controls.Add(CreateInfoButton("Message about kay Sir Bill", () => NavigateTo(new MessageSirBillForm(this, _username))), 1, 1);

            left.Controls.Add(infoPanel, 0, 0);
            left.Controls.Add(new Panel { Height = 12, Dock = DockStyle.Top }, 0, 1);
            left.Controls.Add(grid, 0, 2);

            // RIGHT HALF (profile picture)
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

            FormClosed += (_, __) => _mainMenu.Show();
        }

        private void NavigateTo(Form page)
        {
            page.Show();
            Hide();
        }

        private Button CreateInfoButton(string text, Action onClick)
        {
            var btn = new Button { Text = text, AutoSize = true, Padding = new Padding(16), Margin = new Padding(8) };
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
                catch { /* fall back to placeholder */ }
            }

            _picture.Image = DetailForm.CreatePlaceholderProfileImage(512, 512, DetailForm.GetInitials(_username));
        }

        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            using var db = new AppDb();
            var (brief, photoPath) = db.GetProfile(_username);

            // Edit brief
            using var dlg = new EditDialog("Edit Profile Brief", brief);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                brief = dlg.EditedText;
            }

            // Edit photo path
            using var ofd = new OpenFileDialog
            {
                Title = "Select Profile Picture (optional)",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                CheckFileExists = true
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                photoPath = ofd.FileName;
            }

            db.SaveProfile(_username, brief, photoPath);
            _briefLabel.Text = brief;
            LoadProfileImage(photoPath);
        }
    }
}
