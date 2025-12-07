
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace ProfileApp
{
    public class DetailForm : Form
    {
        protected readonly AboutMeForm _aboutMe;
        protected readonly string _username;
        protected readonly string _section;
        private Label _bodyLabel;

        public DetailForm(AboutMeForm aboutMe, string username, string pageTitle, string sectionKey)
        {
            Theme.Apply(this);

            _aboutMe = aboutMe;
            _username = username;
            _section = sectionKey;

            Text = pageTitle;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(820, 540);

            string body;
            string photoPath;
            using (var db = new AppDb())
            {
                body = db.GetSection(_username, _section);
                photoPath = db.GetProfile(_username).PhotoPath;
            }

            // Root split
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(24)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // LEFT
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titleLabel = Theme.H1(pageTitle);

            _bodyLabel = Theme.BodyText(body);

            var infoPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true
            };
            infoPanel.Controls.Add(titleLabel);
            infoPanel.Controls.Add(_bodyLabel);

            left.Controls.Add(infoPanel, 0, 0);

            // RIGHT
            var right = new Panel { Dock = DockStyle.Fill };
            var picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.Colors.Surface
            };
            if (!string.IsNullOrWhiteSpace(photoPath) && System.IO.File.Exists(photoPath))
            {
                try { picture.Image = Image.FromFile(photoPath); }
                catch { picture.Image = CreatePlaceholderProfileImage(512, 512, GetInitials(_username)); }
            }
            else
            {
                picture.Image = CreatePlaceholderProfileImage(512, 512, GetInitials(_username));
            }
            right.Controls.Add(picture);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);

            // Top bar: Back + Edit
            var btnBack = new Button { Text = "← Back to About Me" };
            Theme.StyleSecondaryButton(btnBack);
            btnBack.Click += (_, __) => { Hide(); _aboutMe.Show(); };

            var btnEdit = new Button { Text = "Edit" };
            Theme.StylePrimaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(24),
                AutoSize = true
            };
            topBar.Controls.Add(btnBack);
            topBar.Controls.Add(btnEdit);

            Controls.Add(root);
            Controls.Add(topBar);

            FormClosed += (_, __) => _aboutMe.Show();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            using var db = new AppDb();
            var currentBody = db.GetSection(_username, _section);

            using var dlg = new EditDialog($"Edit {_section}", currentBody);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                db.SaveSection(_username, _section, dlg.EditedText);
                _bodyLabel.Text = dlg.EditedText;
            }
        }

        // Helpers (same as before)
        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper();
            return new string(name.Trim().Take(2).ToArray()).ToUpper();
        }

        public static Image CreatePlaceholderProfileImage(int width, int height, string initials)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(245, 245, 245));

            var center = new Point(width / 2, height / 2);
            int radius = Math.Min(width, height) / 3;

            using var brush = new SolidBrush(Theme.Colors.Primary);
            g.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            using var pen = new Pen(Color.FromArgb(60, 60, 60), 4);
            g.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            using var font = new Font("Segoe UI", radius / 2.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            var textSize = g.MeasureString(initials, font);
            g.DrawString(initials, font, Brushes.White,
                center.X - textSize.Width / 2, center.Y - textSize.Height / 2);

            return bmp;
        }
    }
}
