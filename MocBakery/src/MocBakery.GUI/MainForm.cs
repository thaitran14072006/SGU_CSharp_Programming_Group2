using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Sales;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;

namespace MocBakery.GUI;

internal sealed class MainForm : Form
{
    private readonly Panel body = new() { Dock = DockStyle.Fill, BackColor = Ui.Cream };
    private readonly List<Button> navigation = [];
    internal bool SignedOut { get; private set; }

    internal MainForm(AppServices services, UserSession session)
    {
        Ui.Style(this, "BMS Desktop");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        root.ColumnStyles.Add(new(SizeType.Absolute, 190));
        root.ColumnStyles.Add(new(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(body, 1, 0);

        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(12, 14, 12, 14), BackColor = Color.White, Margin = Padding.Empty };
        sidebar.RowStyles.Add(new(SizeType.AutoSize));
        sidebar.RowStyles.Add(new(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new(SizeType.AutoSize));
        root.Controls.Add(sidebar, 0, 0);

        var brand = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Margin = new Padding(5, 0, 0, 16) };
        brand.Controls.Add(new Label { AutoSize = true, Text = "MỘC",
            ForeColor = Ui.Brown, Font = new Font("Segoe UI", 15, FontStyle.Bold),
            Margin = Padding.Empty });
        brand.Controls.Add(new Label { AutoSize = true, Text = "BAKERY MANAGEMENT",
            ForeColor = Ui.Brown, Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Margin = Padding.Empty });
        sidebar.Controls.Add(brand, 0, 0);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        sidebar.Controls.Add(nav, 0, 1);
        var overview = AddNav(nav, "Tổng quan", () => Placeholder("Tổng quan"));
        var sales = AddNav(nav, "Bán hàng", () => ShowPage(new SalesPage(services, session)));
        AddNav(nav, "Hóa đơn", () => ShowPage(new SalesPage(services, session, showInvoices: true)));
        AddNav(nav, "Đặt bánh", () => Placeholder("Đặt bánh"));
        AddNav(nav, "Khách hàng", () => ShowPage(new CustomerPage(services, session)));
        if (session.VaiTro == Roles.ChuTiem)
            AddNav(nav, "Tài khoản", () => ShowPage(new AccountPage(services, session)));

        var footer = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false,
            FlowDirection = FlowDirection.TopDown, Margin = Padding.Empty };
        footer.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(155, 0),
            Text = $"{session.HoTen}\n{(session.VaiTro == Roles.ChuTiem ? "Chủ tiệm" : "Nhân viên")}" });
        var changePassword = new LinkLabel { Text = "Đổi mật khẩu", AutoSize = true, LinkColor = Ui.Brown };
        changePassword.LinkClicked += (_, _) =>
        {
            using var dialog = new ChangePasswordForm(services, session);
            dialog.ShowDialog(this);
        };
        footer.Controls.Add(changePassword);
        var logout = new LinkLabel { Text = "Đăng xuất", AutoSize = true, LinkColor = Ui.Brown };
        logout.LinkClicked += (_, _) =>
        {
            if (!Ui.Confirm(this, "Đăng xuất khỏi phiên làm việc?")) return;
            services.Auth.Logout(session);
            SignedOut = true;
            Close();
        };
        footer.Controls.Add(logout);
        sidebar.Controls.Add(footer, 0, 2);
        if (session.VaiTro == Roles.ChuTiem)
        {
            SelectNav(overview);
            Placeholder("Tổng quan");
        }
        else
        {
            SelectNav(sales);
            ShowPage(new SalesPage(services, session));
        }
    }

    private Button AddNav(FlowLayoutPanel nav, string title, Action action)
    {
        var button = new Button
        {
            Text = title, Width = 155, Height = 34, TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Ui.Brown,
            Padding = new Padding(8, 0, 0, 0), Margin = new Padding(0, 0, 0, 3)
        };
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

    private void SelectNav(Button button)
    {
        foreach (var item in navigation)
            item.BackColor = item == button ? Ui.Selection : Color.White;
    }

    internal void ShowPage(Control page)
    {
        while (body.Controls.Count > 0) body.Controls[0].Dispose();
        page.Dock = DockStyle.Fill;
        body.Controls.Add(page);
    }

    private void Placeholder(string title)
    {
        var panel = Ui.Flow(true);
        panel.Controls.Add(Ui.Title(title));
        panel.Controls.Add(new Label { Text = "Module này chưa được tích hợp.", AutoSize = true });
        ShowPage(panel);
    }
}
