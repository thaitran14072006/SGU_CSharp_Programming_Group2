using MocBakery.Models.WebCart;
namespace MocBakery.Web.Models;

public sealed record CartViewModel(CartQuote Quote, bool OrderServiceReady = false, Guid RequestKey = default);
public sealed record CartReview(int CustomerId, Guid RequestKey, CartQuote Quote);
