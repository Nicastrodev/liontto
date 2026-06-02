using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LionttoMoveis.Helpers;

public sealed class BrazilianNumberModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var rawValue = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) is not null)
                bindingContext.Result = ModelBindingResult.Success(null);

            return Task.CompletedTask;
        }

        var targetType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;
        var normalized = NormalizarNumero(rawValue);

        try
        {
            object value = targetType == typeof(decimal)
                ? decimal.Parse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture)
                : double.Parse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture);

            bindingContext.Result = ModelBindingResult.Success(value);
        }
        catch
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"Informe um numero valido para {bindingContext.ModelMetadata.GetDisplayName()}.");
        }

        return Task.CompletedTask;
    }

    private static string NormalizarNumero(string valor)
    {
        var texto = valor
            .Trim()
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        if (texto.Contains(','))
            return texto.Replace(".", string.Empty).Replace(',', '.');

        var pontos = texto.Count(c => c == '.');
        if (pontos == 1)
        {
            var partes = texto.Split('.');
            if (partes.Length == 2 && partes[1].Length == 3)
                return string.Concat(partes);
        }

        if (pontos > 1)
            return texto.Replace(".", string.Empty);

        return texto;
    }
}

public sealed class BrazilianNumberModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        return type == typeof(decimal) || type == typeof(double)
            ? new BrazilianNumberModelBinder()
            : null;
    }
}
