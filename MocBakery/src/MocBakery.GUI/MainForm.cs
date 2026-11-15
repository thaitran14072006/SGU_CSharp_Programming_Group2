using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Inventory;
using MocBakery.GUI.Forms.Orders;
using MocBakery.GUI.Forms.Reports;
using MocBakery.GUI.Forms.Sales;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;

namespace MocBakery.GUI;

internal sealed class MainForm : Form
{
    private static readonly Color CaptionColor = Color.FromArgb(91, 59, 44);
    private static readonly Color ActiveColor = Color.FromArgb(244, 231, 219);
    private static readonly Color Muted = Color.FromArgb(117, 103, 91);
    private readonly Panel body = new() { Dock = DockStyle.Fill, BackColor = Ui.Cream, Margin = Padding.Empty };
    private readonly List<Button> navigation = [];
    private readonly AppServices services;
    private readonly UserSession session;
    internal bool SignedOut { get; private set; }

    internal MainForm(AppServices services, UserSession session)
    {
        this.services = services;
        this.session = session;
        Ui.Style(this, "BMS Desktop");
        FormBorderStyle = FormBorderStyle.None;
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        root.ColumnStyles.Add(new(SizeType.Absolute, 205));
        root.ColumnStyles.Add(new(SizeType.Percent, 100));
        root.RowStyles.Add(new(SizeType.Absolute, 34));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        Controls.Add(root);

        var caption = new Panel { Dock = DockStyle.Fill, BackColor = CaptionColor, Margin = Padding.Empty };
        var captionText = new Label { Text = "BMS Desktop", Dock = DockStyle.Fill, ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0),
            Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        caption.Controls.Add(captionText);
        caption.Controls.Add(CaptionButton("−", "Thu nhỏ", () => WindowState = FormWindowState.Minimized));
        caption.Controls.Add(CaptionButton("□", "Phóng to hoặc khôi phục", ToggleMaximize));
        caption.Controls.Add(CaptionButton("×", "Đóng", Close));
        Point? drag = null;
        captionText.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) drag = e.Location; };
        captionText.MouseUp += (_, _) => drag = null;
        captionText.MouseMove += (_, e) =>
        {
            if (drag is Point start && e.Button == MouseButtons.Left && WindowState == FormWindowState.Normal)
                Location = new Point(Left + e.X - start.X, Top + e.Y - start.Y);
        };
        captionText.DoubleClick += (_, _) => ToggleMaximize();
        root.Controls.Add(caption, 0, 0);
        root.SetColumnSpan(caption, 2);
        root.Controls.Add(body, 1, 1);

        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(13, 20, 13, 14), BackColor = Color.White, Margin = Padding.Empty };
        sidebar.RowStyles.Add(new(SizeType.AutoSize));
        sidebar.RowStyles.Add(new(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new(SizeType.AutoSize));
        root.Controls.Add(sidebar, 0, 1);

        var brand = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false,
            FlowDirection = FlowDirection.TopDown, Margin = new Padding(12, 0, 0, 12) };
        brand.Controls.Add(SideLabel("MỘC", 22, true, CaptionColor));
        brand.Controls.Add(SideLabel("BAKERY MANAGEMENT", 7, true, Muted));
        sidebar.Controls.Add(brand, 0, 0);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        sidebar.Controls.Add(nav, 0, 1);
        bool owner = session.VaiTro == Roles.ChuTiem;
        Button? orders = null;
        Button? sales = null;
        Button? overview = owner ? AddNav(nav, "Tổng quan", OpenOverview) : null;
        sales = AddNav(nav, "Bán hàng", () => ShowPage(new SalesPage(services, session)));
        AddNav(nav, "Quản lý bánh", () => ShowPage(new ProductPage(services, session)));
        AddNav(nav, "Hóa đơn", () => ShowPage(new SalesPage(services, session, showInvoices: true)));
        orders = AddNav(nav, "Đặt bánh theo yêu cầu", () => ShowPage(new OrderPage(services, session)));
        AddNav(nav, "Khách hàng", () => ShowPage(new CustomerPage(services, session)));
        AddNav(nav, "Tồn kho và hạn dùng", () => ShowPage(new InventoryPage(services, session)));
        AddNav(nav, "Loại bánh", () => ShowPage(new CategoryPage(services, session)));
        if (owner)
        {
            AddNav(nav, "Tài khoản", () => ShowPage(new AccountPage(services, session)));
            AddNav(nav, "Báo cáo thống kê", () => ShowPage(new ReportsPage(services, session)));
        }

        var footer = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false,
            FlowDirection = FlowDirection.TopDown, Margin = new Padding(12, 0, 0, 0) };
        var name = SideLabel(session.HoTen, 9, true, CaptionColor);
        name.MaximumSize = new Size(150, 0);
        footer.Controls.Add(name);
        footer.Controls.Add(SideLabel(owner ? "Chủ tiệm / Quản lý" : "Thu ngân / Nhân viên", 8, false, Muted));
        var password = new LinkLabel { Text = "Đổi mật khẩu", AutoSize = true, LinkColor = CaptionColor };
        password.LinkClicked += (_, _) =>
        {
            using var dialog = new ChangePasswordForm(services, session);
            dialog.ShowDialog(this);
        };
        footer.Controls.Add(password);
        var logout = new LinkLabel { Text = "Đăng xuất", AutoSize = true, LinkColor = CaptionColor };
        logout.LinkClicked += (_, _) =>
        {
            if (!Ui.Confirm(this, "Đăng xuất khỏi phiên làm việc?")) return;
            services.Auth.Logout(session);
            SignedOut = true;
            Close();
        };
        footer.Controls.Add(logout);
        sidebar.Controls.Add(footer, 0, 2);
        if (overview is not null)
        {
            SelectNav(overview);
            OpenOverview();
        }
        else
        {
            SelectNav(sales);
            ShowPage(new SalesPage(services, session));
        }

        void OpenOverview() => ShowPage(new OverviewPage(this.services, this.session,
            () => sales?.PerformClick(), id =>
            {
                if (orders is not null) SelectNav(orders);
                ShowPage(new OrderPage(this.services, this.session, id));
            }));
    }

    private void ToggleMaximize()
    {
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }

    private static Button CaptionButton(string text, string name, Action action)
    {
        var button = new Button { Text = text, AccessibleName = name, Dock = DockStyle.Right,
            Width = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = CaptionColor };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private static Label SideLabel(string text, float size, bool bold, Color color) => new()
    {
        Text = text, AutoSize = true, ForeColor = color,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
    };

    private Button AddNav(FlowLayoutPanel nav, string title, Action action)
    {
        var button = new Button { Text = "▪   " + title, AccessibleName = title, Width = 176, Height = 33,
            TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, BackColor = Color.White,
            ForeColor = Muted, Font = new Font("Segoe UI", 8), Margin = new Padding(0, 0, 0, 5),
            Padding = new Padding(8, 0, 0, 0) };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) =>
        {
            SelectNav(button);
            action();
        };
        navigation.Add(button);
        nav.Controls.Add(button);
        return button;
    }

    private void SelectNav(Button selected)
    {
        foreach (var item in navigation)
        {
            item.BackColor = item == selected ? ActiveColor : Color.White;
            item.ForeColor = item == selected ? CaptionColor : Muted;
        }
    }

    internal void ShowPage(Control page)
    {
        while (body.Controls.Count > 0) body.Controls[0].Dispose();
        page.Dock = DockStyle.Fill;
        body.Controls.Add(page);
    }
}
