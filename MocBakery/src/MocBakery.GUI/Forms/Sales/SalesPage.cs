using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Customers;
using MocBakery.Models.DTOs.Sales;
using MocBakery.GUI.Forms.Customers;

namespace MocBakery.GUI.Forms.Sales;

internal sealed class SalesPage : UserControl
{
    internal SalesPage(AppServices services, UserSession session, bool showInvoices = false)
    {
        var errors = new ErrorProvider { ContainerControl = this };
        Disposed += (_, _) => errors.Dispose();
        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        var sales = new TabPage("Giỏ hàng");
        var invoices = new TabPage("Tra cứu hóa đơn");
        tabs.TabPages.AddRange([sales, invoices]);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new(12) };
        layout.RowStyles.Add(new(SizeType.Absolute, 50));
        layout.RowStyles.Add(new(SizeType.Percent, 55));
        layout.RowStyles.Add(new(SizeType.Absolute, 60));
        layout.RowStyles.Add(new(SizeType.Percent, 45));
        sales.Controls.Add(layout);

        var productGrid = Ui.Grid(("Name", "Sản phẩm"), ("Price", "Đơn giá"), ("Stock", "Tồn có thể bán"));
        var cartGrid = Ui.Grid(("Name", "Sản phẩm"), ("Quantity", "Số lượng"), ("Price", "Đơn giá"), ("Total", "Thành tiền"));
        var quantity = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Value = 1, Width = 90 };
        var cart = new Dictionary<int, CartRow>();
        var total = new Label { AutoSize = true, Text = "Tổng tiền 0 đ" };
        string customerName = "Khách lẻ";
        int? customerId = null;
        var cartActions = Ui.Flow();
        var customer = new Button { Text = customerName, AutoSize = true };
        customer.Click += (_, _) =>
        {
            using var picker = new CustomerPickerForm(services, session);
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedCustomer is not { } selected) return;
            customerId = selected.MaKhachHang;
            customerName = selected.HoTen;
            RefreshCart();
        };
        var top = Ui.Flow();
        top.Controls.Add(Ui.Button("Tải danh sách bánh", async (_, _) => await Reload()));
        top.Controls.Add(new Label { Text = "Số lượng", AutoSize = true });
        top.Controls.Add(quantity);
        top.Controls.Add(Ui.Button("Thêm vào giỏ", async (_, _) => await Run(() =>
        {
            if (productGrid.CurrentRow?.DataBoundItem is not SalesProduct product)
                throw new BusinessException("Chọn sản phẩm trước.");
            var existing = cart.GetValueOrDefault(product.Id);
            long requested = (long)quantity.Value + (existing?.Quantity ?? 0);
            if (requested > product.Stock || requested > int.MaxValue)
                throw new BusinessException("Không đủ tồn có thể bán.");
            cart[product.Id] = new(product.Id, product.Name, (int)requested, product.Price);
            RefreshCart();
            return Task.CompletedTask;
        })));
        cartActions.Controls.Add(customer);
        cartActions.Controls.Add(Ui.Button("Khách lẻ", (_, _) =>
        {
            customerId = null;
            customerName = "Khách lẻ";
            RefreshCart();
        }));
        cartActions.Controls.Add(Ui.Button("Xóa dòng", (_, _) =>
        {
            if (cartGrid.CurrentRow?.DataBoundItem is CartRow row) cart.Remove(row.ProductId);
            RefreshCart();
        }));
        cartActions.Controls.Add(total);
        cartActions.Controls.Add(Ui.Button("Thanh toán", async (_, _) => await Run(async () =>
        {
            if (cart.Count == 0) throw new BusinessException("Giỏ hàng đang trống.");
            if (!Ui.Confirm(this, $"Xác nhận thanh toán {cart.Values.Sum(row => row.Total):N2} đ?")) return;
            var items = cart.Values.Select(row => new CartItem(row.ProductId, row.Quantity, row.Price)).ToArray();
            int? buyer = customerId;
            int invoiceId = await Task.Run(() => services.Sales.Sell(session, buyer, items));
            cart.Clear();
            customerId = null;
            customerName = "Khách lẻ";
            RefreshCart();
            MessageBox.Show(this, $"Đã lập hóa đơn #{invoiceId}.");
            await LoadProducts();
        }), true));
        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(productGrid, 0, 1);
        layout.Controls.Add(cartActions, 0, 2);
        layout.Controls.Add(cartGrid, 0, 3);

        var history = Ui.Grid(("MaHoaDon", "Mã hóa đơn"), ("NgayLap", "Ngày lập"), ("TongTien", "Tổng tiền"),
            ("TienCocTru", "Cọc đã trừ"), ("SoTienThu", "Đã thu"));
        var historyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new(12) };
        historyLayout.RowStyles.Add(new(SizeType.Absolute, 50));
        historyLayout.RowStyles.Add(new(SizeType.Percent, 100));
        invoices.Controls.Add(historyLayout);
        var historyActions = Ui.Flow();
        historyActions.Controls.Add(Ui.Button("Tải hóa đơn", async (_, _) => await LoadHistory()));
        historyActions.Controls.Add(Ui.Button("Xem chi tiết", async (_, _) => await Run(async () =>
        {
            if (history.CurrentRow?.DataBoundItem is not InvoiceSummary selected)
                throw new BusinessException("Chọn hóa đơn trước.");
            var rows = await Task.Run(() => services.Sales.Invoice(session, selected.MaHoaDon));
            if (IsDisposed) return;
            using var dialog = new Form();
            Ui.Style(dialog, $"Hóa đơn #{selected.MaHoaDon}");
            var details = Ui.Grid(("LotId", "Mã lô"), ("Product", "Sản phẩm"), ("Quantity", "Số lượng"),
                ("UnitPrice", "Đơn giá lúc bán"), ("Total", "Thành tiền"));
            details.DataSource = rows.ToList();
            dialog.Controls.Add(details);
            dialog.ShowDialog(this);
        })));
        historyLayout.Controls.Add(historyActions, 0, 0);
        historyLayout.Controls.Add(history, 0, 1);

        tabs.SelectedIndex = showInvoices ? 1 : 0;
        Load += async (_, _) => await LoadSelectedTab();

        tabs.SelectedIndexChanged += async (_, _) => await LoadSelectedTab();

        async Task LoadSelectedTab()
        {
            if (UiSmoke.Active) return;
            if (tabs.SelectedIndex == 0) await Reload();
            else await LoadHistory();
        }

        async Task Run(Func<Task> action) => await Ui.Run(this, errors, new(), action);
        async Task Reload() => await Run(LoadProducts);
        async Task LoadProducts()
        {
            var rows = await Task.Run(() => services.Sales.Products(session));
            if (IsDisposed) return;
            productGrid.DataSource = rows.ToList();
            foreach (var product in rows)
                if (cart.TryGetValue(product.Id, out var row))
                    cart[product.Id] = row with { Name = product.Name, Price = product.Price };
            RefreshCart();
        }
        async Task LoadHistory() => await Run(async () =>
        {
            var rows = await Task.Run(() => services.Sales.Invoices(session));
            if (!IsDisposed) history.DataSource = rows.ToList();
        });
        void RefreshCart()
        {
            cartGrid.DataSource = cart.Values.ToList();
            customer.Text = customerName;
            total.Text = $"Tổng tiền {cart.Values.Sum(row => row.Total):N2} đ";
        }
    }

    private sealed record CartRow(int ProductId, string Name, int Quantity, decimal Price)
    {
        public decimal Total => Quantity * Price;
    }
}
