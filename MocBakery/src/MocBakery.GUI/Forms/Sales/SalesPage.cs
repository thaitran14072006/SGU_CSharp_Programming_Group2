using MocBakery.GUI.Forms.Customers;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Customers;
using MocBakery.Models.DTOs.Sales;

namespace MocBakery.GUI.Forms.Sales;

internal sealed class SalesPage : UserControl
{
    private static readonly Color Surface = Color.FromArgb(246, 241, 234);
    private static readonly Color Ink = Color.FromArgb(62, 43, 34);
    private static readonly Color Accent = Color.FromArgb(191, 112, 73);
    private static readonly Color Muted = Color.FromArgb(115, 100, 89);
    private static readonly Color Line = Color.FromArgb(238, 229, 219);
    private readonly AppServices services;
    private readonly UserSession session;
    private readonly ErrorProvider errors;

    internal SalesPage(AppServices services, UserSession session, bool showInvoices = false,
        IReadOnlyList<SalesProduct>? previewProducts = null)
    {
        this.services = services;
        this.session = session;
        errors = new ErrorProvider { ContainerControl = this };
        Disposed += (_, _) => errors.Dispose();
        BackColor = Surface;
        ForeColor = Ink;
        Controls.Add(showInvoices ? CreateInvoices() : CreateCheckout(previewProducts));
    }

    private Control CreateCheckout(IReadOnlyList<SalesProduct>? previewProducts)
    {
        var page = Page("Bán hàng", "Chọn bánh, lập giỏ hàng và thanh toán tại quầy.");
        var split = CreateLayout(2);
        split.ColumnStyles.Add(new(SizeType.Percent, 60));
        split.ColumnStyles.Add(new(SizeType.Percent, 40));
        page.Controls.Add(split, 0, 1);

        var catalog = CreateLayout(1);
        catalog.Margin = new Padding(0, 0, 28, 0);
        var search = SearchBox("Tìm theo tên hoặc mã bánh");
        string? selectedCategory = null;
        var filters = Toolbar();
        var products = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        catalog.Controls.Add(TextLabel("Tìm bánh", 10, color: Muted), 0, 0);
        catalog.Controls.Add(search, 0, 1);
        catalog.Controls.Add(filters, 0, 2);
        catalog.Controls.Add(products, 0, 3);
        catalog.RowStyles.Add(new(SizeType.AutoSize));
        catalog.RowStyles.Add(new(SizeType.AutoSize));
        catalog.RowStyles.Add(new(SizeType.AutoSize));
        catalog.RowStyles.Add(new(SizeType.Percent, 100));
        split.Controls.Add(catalog, 0, 0);

        var order = CreateLayout(1);
        order.BackColor = Color.White;
        order.Padding = new Padding(16);
        order.Margin = new Padding(0, 10, 0, 10);
        Round(order, 13);
        var customer = ActionButton("Chọn khách hàng", false);
        var guest = ActionButton("Khách lẻ", false);
        var buyerActions = Toolbar();
        buyerActions.Controls.AddRange([customer, guest]);
        var cartGrid = Grid(("Name", "Bánh"), ("Quantity", "SL"), ("Total", "Tiền (đ)"));
        cartGrid.Columns["Quantity"]!.FillWeight = 40;
        cartGrid.Columns["Total"]!.DefaultCellStyle.Format = "N0";
        cartGrid.Columns["Total"]!.DefaultCellStyle.ForeColor = Color.FromArgb(51, 118, 91);
        var total = TextLabel("Phải trả   0 đ", 16, true);
        total.Margin = new Padding(0, 3, 0, 5);
        var subtotal = TextLabel("Tạm tính   0 đ", 10);
        var cash = new NumericUpDown { Minimum = 0, Maximum = decimal.MaxValue, ThousandsSeparator = true,
            Width = 138, AccessibleName = "Tiền mặt khách đưa" };
        var cashRow = Toolbar();
        cashRow.Controls.AddRange([TextLabel("Tiền mặt · Khách đưa", 9, color: Muted), cash]);
        var change = TextLabel("Tiền thừa   0 đ", 9, color: Color.FromArgb(51, 118, 91));
        var cartActions = Toolbar();
        var quantity = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Width = 68,
            AccessibleName = "Số lượng" };
        var remove = ActionButton("Xóa dòng", false);
        cartActions.Controls.AddRange([TextLabel("Số lượng", 10), quantity, remove]);
        var pay = ActionButton("Thanh toán & In hóa đơn");
        pay.UseMnemonic = false;
        pay.Dock = DockStyle.Top;
        Round(pay, 8);
        order.Controls.Add(TextLabel("Đơn bán tại quầy", 14, true), 0, 0);
        order.Controls.Add(buyerActions, 0, 1);
        order.Controls.Add(cartGrid, 0, 2);
        order.Controls.Add(cartActions, 0, 3);
        order.Controls.Add(subtotal, 0, 4);
        order.Controls.Add(TextLabel("Giảm giá   0 đ", 10), 0, 5);
        order.Controls.Add(total, 0, 6);
        order.Controls.Add(cashRow, 0, 7);
        order.Controls.Add(change, 0, 8);
        order.Controls.Add(pay, 0, 9);
        order.RowStyles.Add(new(SizeType.AutoSize));
        order.RowStyles.Add(new(SizeType.AutoSize));
        order.RowStyles.Add(new(SizeType.Percent, 100));
        for (int row = 3; row < 10; row++) order.RowStyles.Add(new(SizeType.AutoSize));
        split.Controls.Add(order, 1, 0);

        IReadOnlyList<SalesProduct> inventory = [];
        var cart = new Dictionary<int, CartRow>();
        bool syncingQuantity = false;
        int? customerId = null;
        search.TextChanged += (_, _) => FilterProducts();
        products.ClientSizeChanged += (_, _) => SizeCards();
        cash.ValueChanged += (_, _) => RefreshTotal();

        customer.Click += (_, _) =>
        {
            using var picker = new CustomerPickerForm(services, session);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedCustomer is not { } selected) return;
            customerId = selected.MaKhachHang;
            customer.Text = selected.HoTen;
        };
        guest.Click += (_, _) => ClearCustomer();
        cartGrid.CurrentCellChanged += (_, _) => SyncQuantity();
        quantity.ValueChanged += (_, _) =>
        {
            if (syncingQuantity || cartGrid.CurrentRow?.DataBoundItem is not CartRow row) return;
            row.Quantity = (int)quantity.Value;
            cartGrid.Refresh();
            RefreshTotal();
        };
        remove.Click += (_, _) =>
        {
            if (cartGrid.CurrentRow?.DataBoundItem is CartRow row) cart.Remove(row.ProductId);
            RefreshCart();
        };
        pay.Click += async (_, _) => await Run(async () =>
        {
            // Reading Value validates any text still being edited in the quantity box.
            _ = quantity.Value;
            if (cart.Count == 0) throw new BusinessException("Giỏ hàng đang trống.");
            decimal amount = cart.Values.Sum(row => row.Total);
            if (cash.Value > 0 && cash.Value < amount)
                throw new BusinessException("Tiền khách đưa chưa đủ.");
            if (!Ui.Confirm(this, $"Xác nhận thanh toán {Money(amount)}?")) return;
            var items = cart.Values.Select(row => new CartItem(row.ProductId, row.Quantity, row.Price)).ToArray();
            var receipt = cart.Values.Select(row => $"{row.Name} × {row.Quantity}    {Money(row.Total)}").ToArray();
            int? buyer = customerId;
            int id = await Task.Run(() => services.Sales.Sell(session, buyer, items));
            if (IsDisposed) return;
            cart.Clear();
            cash.Value = 0;
            ClearCustomer();
            RefreshCart();
            await LoadProducts();
            if (!IsDisposed && Ui.Confirm(this, $"Đã lập hóa đơn {InvoiceCode(id)}. In hóa đơn?"))
                PrintReceipt(id, receipt, amount);
        });

        Load += async (_, _) =>
        {
            if (previewProducts is not null) SetProducts(previewProducts);
            else if (!UiSmoke.Active) await Run(LoadProducts);
        };
        RefreshCart();
        return page;

        async Task LoadProducts()
        {
            var rows = await Task.Run(() => services.Sales.Products(session));
            if (!IsDisposed) SetProducts(rows);
        }

        void SetProducts(IReadOnlyList<SalesProduct> rows)
        {
            inventory = rows;
            foreach (var product in inventory)
                if (cart.TryGetValue(product.Id, out var row))
                    cart[product.Id] = row with { Name = product.Name, Price = product.Price };
            var categories = inventory.Select(item => item.Category).Where(name => name.Length > 0).Distinct().ToArray();
            if (selectedCategory is not null && !categories.Contains(selectedCategory)) selectedCategory = null;
            RefreshFilters(categories);
            FilterProducts();
            RefreshCart();
        }

        void RefreshFilters(string[] categories)
        {
            while (filters.Controls.Count > 0) filters.Controls[0].Dispose();
            foreach (string? name in categories.Prepend(null))
            {
                var link = new LinkLabel { Text = name ?? "Tất cả", AutoSize = true,
                    LinkColor = Accent, ActiveLinkColor = Ink,
                    LinkBehavior = name == selectedCategory ? LinkBehavior.AlwaysUnderline : LinkBehavior.NeverUnderline,
                    Margin = new Padding(0, 6, 10, 5) };
                link.LinkClicked += (_, _) =>
                {
                    selectedCategory = name;
                    foreach (LinkLabel filter in filters.Controls.OfType<LinkLabel>())
                        filter.LinkBehavior = filter == link ? LinkBehavior.AlwaysUnderline : LinkBehavior.NeverUnderline;
                    FilterProducts();
                };
                filters.Controls.Add(link);
            }
            var refresh = new LinkLabel { Text = "Làm mới", AutoSize = true, LinkColor = Muted,
                LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 6, 0, 5) };
            refresh.LinkClicked += async (_, _) => await Run(LoadProducts);
            filters.Controls.Add(refresh);
        }

        void FilterProducts()
        {
            string key = search.Text.Trim();
            string? selected = selectedCategory;
            var matches = inventory.Where(product => (selected is null || product.Category == selected) &&
                (Contains(product.Name, key) || product.Id.ToString().Contains(key))).ToList();
            products.SuspendLayout();
            while (products.Controls.Count > 0) products.Controls[0].Dispose();
            foreach (var product in matches)
            {
                var card = new ProductCard(product);
                card.Click += async (_, _) => await Run(() =>
                {
                    SetQuantity(product, (long)(cart.GetValueOrDefault(product.Id)?.Quantity ?? 0) + 1);
                    return Task.CompletedTask;
                });
                products.Controls.Add(card);
            }
            if (matches.Count == 0) products.Controls.Add(TextLabel("Không tìm thấy sản phẩm.", 10, color: Muted));
            SizeCards();
            products.ResumeLayout();
        }

        void SizeCards()
        {
            int width = products.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            int columns = Math.Clamp(width / 162, 1, 3);
            foreach (Button card in products.Controls.OfType<Button>())
                card.Width = Math.Max(90, width / columns - 10);
        }

        void ClearCustomer()
        {
            customerId = null;
            customer.Text = "Chọn khách hàng";
        }

        void SetQuantity(SalesProduct? product, long requested)
        {
            if (product is null || requested > product.Stock || requested > int.MaxValue)
                throw new BusinessException("Không đủ tồn có thể bán.");
            cart[product.Id] = new(product.Id, product.Name, (int)requested, product.Price);
            RefreshCart(product.Id);
        }

        void RefreshCart(int? selectedId = null)
        {
            cartGrid.DataSource = cart.Values.ToList();
            if (selectedId is int id)
                foreach (DataGridViewRow row in cartGrid.Rows)
                    if (row.DataBoundItem is CartRow item && item.ProductId == id)
                        cartGrid.CurrentCell = row.Cells[0];
            SyncQuantity();
            RefreshTotal();
        }

        void SyncQuantity()
        {
            syncingQuantity = true;
            try
            {
                var row = cartGrid.CurrentRow?.DataBoundItem as CartRow;
                long stock = row is null ? 0 : inventory.FirstOrDefault(product => product.Id == row.ProductId)?.Stock ?? 0;
                quantity.Enabled = row is not null;
                quantity.Maximum = Math.Clamp(stock, row?.Quantity ?? 1, int.MaxValue);
                quantity.Value = row?.Quantity ?? 1;
            }
            finally
            {
                syncingQuantity = false;
            }
        }

        void RefreshTotal()
        {
            decimal amount = cart.Values.Sum(row => row.Total);
            subtotal.Text = $"Tạm tính   {Money(amount)}";
            total.Text = $"Phải trả   {Money(amount)}";
            change.Text = $"Tiền thừa   {Money(Math.Max(0, cash.Value - amount))}";
            pay.Enabled = cart.Count > 0;
        }
    }

    private Control CreateInvoices()
    {
        var page = Page("Hóa đơn", "Tìm kiếm và xem lịch sử hóa đơn bán hàng.");
        var content = CreateLayout(1);
        content.RowStyles.Add(new(SizeType.AutoSize));
        content.RowStyles.Add(new(SizeType.Percent, 100));
        content.RowStyles.Add(new(SizeType.AutoSize));
        page.Controls.Add(content, 0, 1);

        var search = SearchBox("Mã hóa đơn, tên hoặc số điện thoại");
        search.Width = 300;
        search.Dock = DockStyle.None;
        var period = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        period.Items.AddRange(["Tất cả ngày", "Hôm nay", "7 ngày gần nhất"]);
        period.SelectedIndex = 0;
        var reload = ActionButton("Làm mới", false);
        var open = ActionButton("Xem chi tiết");
        var actions = Toolbar();
        var searchField = CreateLayout(1);
        searchField.AutoSize = true;
        searchField.Dock = DockStyle.None;
        searchField.Controls.Add(TextLabel("Mã hóa đơn / SĐT", 10, color: Muted), 0, 0);
        searchField.Controls.Add(search, 0, 1);
        var periodField = CreateLayout(1);
        periodField.AutoSize = true;
        periodField.Dock = DockStyle.None;
        periodField.Controls.Add(TextLabel("Bộ lọc", 10, color: Muted), 0, 0);
        periodField.Controls.Add(period, 0, 1);
        reload.Margin = new Padding(0, 27, 6, 4);
        open.Margin = new Padding(0, 27, 6, 4);
        actions.Controls.AddRange([searchField, periodField, reload, open]);
        content.Controls.Add(actions, 0, 0);

        var history = Grid(("Code", "Mã hóa đơn"), ("NgayLap", "Giờ lập"), ("CustomerName", "Khách hàng"),
            ("TongTien", "Tổng tiền (đ)"), ("TienCocTru", "Cọc (đ)"), ("SoTienThu", "Đã thu (đ)"));
        history.Columns["NgayLap"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        foreach (string key in new[] { "TongTien", "TienCocTru", "SoTienThu" })
            history.Columns[key]!.DefaultCellStyle.Format = "N0";
        content.Controls.Add(history, 0, 1);
        var count = TextLabel("0 hóa đơn", 10, color: Muted);
        count.Margin = new Padding(0, 10, 0, 0);
        content.Controls.Add(count, 0, 2);

        IReadOnlyList<InvoiceSummary> invoices = [];
        reload.Click += async (_, _) => await LoadInvoices();
        open.Click += async (_, _) => await ShowDetails();
        history.CellDoubleClick += async (_, args) => { if (args.RowIndex >= 0) await ShowDetails(); };
        search.TextChanged += (_, _) => Filter();
        period.SelectedIndexChanged += (_, _) => Filter();
        Load += async (_, _) => { if (!UiSmoke.Active) await LoadInvoices(); };
        return page;

        async Task LoadInvoices() => await Run(async () =>
        {
            var rows = await Task.Run(() => services.Sales.Invoices(session));
            if (IsDisposed) return;
            invoices = rows;
            Filter();
        });

        void Filter()
        {
            string key = search.Text.Trim();
            DateTime? from = period.SelectedIndex switch { 1 => DateTime.Today, 2 => DateTime.Today.AddDays(-6), _ => null };
            var rows = invoices.Where(invoice => (from is null || invoice.NgayLap >= from) &&
                (Contains(InvoiceCode(invoice.MaHoaDon), key) || invoice.MaHoaDon.ToString().Contains(key) ||
                 Contains(invoice.CustomerName, key) || invoice.Phone.Contains(key)))
                .Select(invoice => new InvoiceRow(InvoiceCode(invoice.MaHoaDon), invoice)).ToList();
            history.DataSource = rows;
            count.Text = $"{rows.Count:N0} hóa đơn";
        }

        async Task ShowDetails() => await Run(async () =>
        {
            if (history.CurrentRow?.DataBoundItem is not InvoiceRow selected)
                throw new BusinessException("Chọn hóa đơn trước.");
            var rows = await Task.Run(() => services.Sales.Invoice(session, selected.Invoice.MaHoaDon));
            if (IsDisposed) return;
            using var dialog = new Form();
            Ui.Style(dialog, "Chi tiết hóa đơn");
            var detailPage = Page("Chi tiết hóa đơn", "Xem mặt hàng đã bán và số tiền thanh toán.");
            var receipt = CreateLayout(1);
            receipt.BackColor = Color.White;
            receipt.Padding = new Padding(18);
            receipt.RowStyles.Add(new(SizeType.AutoSize));
            receipt.RowStyles.Add(new(SizeType.AutoSize));
            receipt.RowStyles.Add(new(SizeType.Percent, 100));
            receipt.RowStyles.Add(new(SizeType.AutoSize));
            receipt.Controls.Add(TextLabel($"HÓA ĐƠN {selected.Code}", 18, true), 0, 0);
            receipt.Controls.Add(TextLabel($"{selected.CustomerName}  ·  {selected.NgayLap:dd/MM/yyyy HH:mm}",
                10, color: Muted), 0, 1);
            var details = Grid(("LotId", "Mã lô"), ("Product", "Bánh"), ("Quantity", "SL"),
                ("UnitPrice", "Đơn giá lúc bán"), ("Total", "Thành tiền"));
            foreach (string key in new[] { "UnitPrice", "Total" }) details.Columns[key]!.DefaultCellStyle.Format = "N0";
            details.DataSource = rows.ToList();
            receipt.Controls.Add(details, 0, 2);
            var total = TextLabel($"Tổng tiền   {Money(selected.TongTien)}", 15, true, Accent);
            total.Anchor = AnchorStyles.Right;
            receipt.Controls.Add(total, 0, 3);
            detailPage.Controls.Add(receipt, 0, 1);
            dialog.Controls.Add(detailPage);
            dialog.ShowDialog(this);
        });
    }

    private Task Run(Func<Task> action) => Ui.Run(this, errors, new(), action);

    private void PrintReceipt(int id, IReadOnlyList<string> lines, decimal total)
    {
        using var document = new System.Drawing.Printing.PrintDocument { DocumentName = $"Hóa đơn {InvoiceCode(id)}" };
        int nextLine = 0;
        document.BeginPrint += (_, _) => nextLine = 0;
        document.PrintPage += (_, page) =>
        {
            if (page.Graphics is not { } graphics) return;
            using var heading = new Font("Segoe UI", 14, FontStyle.Bold);
            using var text = new Font("Segoe UI", 10);
            var bounds = page.MarginBounds;
            float y = bounds.Top;
            graphics.DrawString($"MỘC BAKERY · {InvoiceCode(id)}", heading, Brushes.Black, bounds.Left, y);
            y += 34;
            while (nextLine < lines.Count && y + 48 <= bounds.Bottom)
            {
                graphics.DrawString(lines[nextLine++], text, Brushes.Black,
                    new RectangleF(bounds.Left, y, bounds.Width, 24));
                y += 24;
            }
            page.HasMorePages = nextLine < lines.Count && y > bounds.Top + 34;
            if (!page.HasMorePages)
                graphics.DrawString($"Tổng tiền   {Money(total)}", heading, Brushes.Black, bounds.Left, y + 6);
        };
        using var printer = new PrintDialog { Document = document, UseEXDialog = true };
        if (printer.ShowDialog(this) == DialogResult.OK) document.Print();
    }

    private static void Round(Control control, int radius)
    {
        control.SizeChanged += (_, _) =>
        {
            if (control.Width <= 2 * radius || control.Height <= 2 * radius) return;
            using var path = RoundedPath(0, 0, control.Width, control.Height, radius);
            var previous = control.Region;
            control.Region = new Region(path);
            previous?.Dispose();
        };
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(int x, int y, int width, int height, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string Money(decimal value) => $"{value:N0} đ";
    private static string InvoiceCode(int id) => $"HD{id:D3}";
    private static bool Contains(string value, string key) =>
        System.Globalization.CultureInfo.GetCultureInfo("vi-VN").CompareInfo.IndexOf(value, key,
            System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;

    private static TableLayoutPanel Page(string title, string description)
    {
        var page = CreateLayout(1);
        page.Padding = new Padding(26, 18, 26, 20);
        page.RowStyles.Add(new(SizeType.AutoSize));
        page.RowStyles.Add(new(SizeType.Percent, 100));
        var header = CreateLayout(2);
        header.AutoSize = true;
        header.ColumnStyles.Add(new(SizeType.Percent, 100));
        header.ColumnStyles.Add(new(SizeType.AutoSize));
        header.Margin = new Padding(0, 0, 0, 14);
        header.Controls.Add(TextLabel($"Cửa hàng Mộc / {title}", 9, color: Muted), 0, 0);
        header.Controls.Add(TextLabel(DateTime.Today.ToString("dd/MM/yyyy"), 10, color: Muted), 1, 0);
        header.Controls.Add(TextLabel(title, 20, true), 0, 1);
        var caption = TextLabel(description, 10, color: Muted);
        header.Controls.Add(caption, 0, 2);
        header.SetColumnSpan(caption, 2);
        page.Controls.Add(header, 0, 0);
        return page;
    }

    private static TableLayoutPanel CreateLayout(int columns) => new()
    {
        Dock = DockStyle.Fill, ColumnCount = columns, Margin = Padding.Empty
    };

    private static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = new Padding(0, 6, 0, 6)
    };

    private static Label TextLabel(string text, float size, bool bold = false, Color? color = null) => new()
    {
        Text = text, AutoSize = true, ForeColor = color ?? Ink,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        Anchor = AnchorStyles.Left, Margin = new Padding(0, 3, 0, 3)
    };

    private static TextBox SearchBox(string placeholder) => new()
    {
        Dock = DockStyle.Top, PlaceholderText = placeholder,
        Font = new Font("Segoe UI", 12), Margin = new Padding(0, 2, 10, 4)
    };

    private static Button ActionButton(string text, bool primary = true)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Padding = new Padding(8, 0, 8, 0),
            MinimumSize = new Size(0, 32), Margin = new Padding(0, 0, 6, 4),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : Ink
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Line;
        return button;
    }

    private static DataGridView Grid(params (string Key, string Title)[] columns)
    {
        var grid = Ui.Grid(columns);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Line;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
        grid.DefaultCellStyle.ForeColor = Ink;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 247, 243);
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Line;
        return grid;
    }

    private sealed record CartRow(int ProductId, string Name, int InitialQuantity, decimal Price)
    {
        public int Quantity { get; set; } = InitialQuantity;
        public decimal Total => Quantity * Price;
    }

    private sealed class ProductCard : Button
    {
        private readonly SalesProduct product;
        private readonly Image? photo;

        internal ProductCard(SalesProduct product)
        {
            this.product = product;
            photo = LoadPhoto(product);
            Height = 148;
            Margin = new Padding(0, 0, 10, 10);
            Text = product.Name;
            AccessibleDescription = $"{Money(product.Price)}, tồn {product.Stock}";
            Enabled = product.Stock > 0;
            BackColor = Color.White;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 249, 241);
            Round(this, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ClientSize.Width <= 20 || ClientSize.Height <= 20) return;
            using var background = new SolidBrush(Enabled && ClientRectangle.Contains(PointToClient(MousePosition))
                ? Color.FromArgb(255, 249, 241) : Color.White);
            e.Graphics.FillRectangle(background, 1, 1, Width - 2, Height - 2);
            int tint = product.Category.Aggregate(0, (sum, letter) => (sum + letter) % 3);
            using var tag = new SolidBrush(tint switch
            {
                1 => Color.FromArgb(234, 216, 202),
                2 => Color.FromArgb(247, 216, 213),
                _ => Color.FromArgb(242, 220, 199)
            });
            using var bold = new Font(Font.FontFamily, 9, FontStyle.Bold);
            using var metadata = new Font(Font.FontFamily, 8);
            int width = Width - 20;
            int banner = Height * 42 / 100;
            int lineHeight = (Height - banner - 20) / 3;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var bannerPath = RoundedPath(10, 10, width, banner, 8);
            e.Graphics.FillPath(tag, bannerPath);
            if (photo is not null)
            {
                var state = e.Graphics.Save();
                e.Graphics.SetClip(bannerPath);
                float scale = Math.Max((float)width / photo.Width, (float)banner / photo.Height);
                float imageWidth = photo.Width * scale, imageHeight = photo.Height * scale;
                e.Graphics.DrawImage(photo, 10 + (width - imageWidth) / 2, 10 + (banner - imageHeight) / 2, imageWidth, imageHeight);
                e.Graphics.Restore(state);
            }
            else TextRenderer.DrawText(e.Graphics, product.Category, bold, new Rectangle(14, 10, width - 8, banner),
                Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            Draw(product.Name, bold, Ink, 0);
            Draw(Money(product.Price), bold, Accent, 1);
            Draw(Enabled ? $"Còn {product.Stock:N0} · Nhấn để thêm" : "Hết hàng", metadata, Muted, 2);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3));

            void Draw(string text, Font font, Color color, int row) =>
                TextRenderer.DrawText(e.Graphics, text, font, new Rectangle(10, banner + 14 + lineHeight * row, width, lineHeight),
                    color, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static Image? LoadPhoto(SalesProduct product)
        {
            try
            {
                if (product.Photo is { Length: > 0 })
                {
                    using var stream = new MemoryStream(product.Photo);
                    using var original = Image.FromStream(stream);
                    return new Bitmap(original);
                }
            }
            catch (ArgumentException) { }
            string name = product.Name.ToLowerInvariant();
            string? file = name.Contains("cookie") || name.Contains("bánh quy") ? "cookie"
                : name.Contains("muffin") ? "muffin"
                : name.Contains("croissant") || name.Contains("sừng bò") ? "croissant"
                : name.Contains("tiramisu") ? "tiramisu" : null;
            string path = Path.Combine(AppContext.BaseDirectory, "Images", "Bakery", file + ".jpg");
            if (file is null || !File.Exists(path)) return null;
            using var source = Image.FromFile(path);
            return new Bitmap(source);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) photo?.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed record InvoiceRow(string Code, InvoiceSummary Invoice)
    {
        public DateTime NgayLap => Invoice.NgayLap;
        public string CustomerName => Invoice.CustomerName;
        public decimal TongTien => Invoice.TongTien;
        public decimal TienCocTru => Invoice.TienCocTru;
        public decimal SoTienThu => Invoice.SoTienThu;
    }
}
