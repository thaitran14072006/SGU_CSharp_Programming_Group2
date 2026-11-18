using System.ComponentModel.DataAnnotations;

namespace MocBakery.Web.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có 3–50 ký tự.")]
    public string Username { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(64, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có 8–64 ký tự.")]
    public string Password { get; set; } = "";
}

public sealed class RegisterForm : LoginForm
{
    [Required(ErrorMessage = "Nhập xác nhận mật khẩu."), Compare(nameof(Password), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string Confirmation { get; set; } = "";
    [Required(ErrorMessage = "Nhập họ tên."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), RegularExpression(@"[0-9]{10}", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số.")]
    public string Phone { get; set; } = "";
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }
}

public sealed class ProfileForm
{
    [Required(ErrorMessage = "Nhập họ tên."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), RegularExpression(@"[0-9]{10}", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số.")]
    public string Phone { get; set; } = "";
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }
}
