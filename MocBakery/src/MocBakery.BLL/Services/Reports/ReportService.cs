using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Reports;
namespace MocBakery.BLL.Services.Reports;
public sealed class ReportService(IReportRepository repo,AuthService auth)
{
 public IReadOnlyList<RevenueDay> Revenue(UserSession s,DateTime start,DateTime end){auth.Require(s,owner:true);Range(start,end);return repo.Revenue(start.Date,end.Date.AddDays(1));}
 public IReadOnlyList<ProductSales> Bestsellers(UserSession s,DateTime start,DateTime end){auth.Require(s,owner:true);Range(start,end);return repo.Bestsellers(start.Date,end.Date.AddDays(1));}
 public Dashboard Overview(UserSession s,DateTime start,DateTime end){auth.Require(s,owner:true);Range(start,end);return repo.Overview(start.Date,end.Date.AddDays(1));}
 public IReadOnlyList<LowStockItem> LowStock(UserSession s,int threshold=10){auth.Require(s,owner:true);if(threshold<1)throw new BusinessException("Ngưỡng tồn phải lớn hơn 0.");return repo.LowStock(threshold);}
 public IReadOnlyList<UpcomingOrder> Upcoming(UserSession s,DateTime day){auth.Require(s,owner:true);return repo.Upcoming(day);}
 private static void Range(DateTime start,DateTime end){if(start.Date>end.Date||end.Date>=DateTime.MaxValue.Date)throw new BusinessException("Khoảng báo cáo không hợp lệ.");}
}
