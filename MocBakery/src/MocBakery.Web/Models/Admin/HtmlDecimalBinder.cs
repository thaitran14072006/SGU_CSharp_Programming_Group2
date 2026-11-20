using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MocBakery.Web.Models.Admin;

// HTML number controls submit a dot decimal separator regardless of Windows locale.
public sealed class HtmlDecimalBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value=context.ValueProvider.GetValue(context.ModelName);
        if(value==ValueProviderResult.None)return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName,value);
        if(decimal.TryParse(value.FirstValue,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var number))
            context.Result=ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName,"Nhập số hợp lệ.");
        return Task.CompletedTask;
    }
}
