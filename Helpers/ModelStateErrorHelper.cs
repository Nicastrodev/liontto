using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;

namespace LionttoMoveis.Helpers
{
    public static class ModelStateErrorHelper
    {
        private static readonly Dictionary<string, string> NomeCampos = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Nome"] = "nome",
            ["Telefone"] = "telefone",
            ["Email"] = "e-mail",
            ["Endereco"] = "endereco",
            ["ClienteId"] = "cliente",
            ["DataEntregaPrevista"] = "data de entrega prevista",
            ["ProdIds"] = "produto",
            ["ProdQtds"] = "quantidade do produto",
            ["ProdPers"] = "personalizacao",
            ["MatIds"] = "material",
            ["MatQtds"] = "quantidade do material",
            ["Unidade"] = "unidade",
            ["Quantidade"] = "quantidade",
            ["QuantidadeMinima"] = "quantidade minima",
            ["PrecoUnitario"] = "preco unitario",
            ["PrecoBase"] = "preco base",
            ["TempoProducaoDias"] = "tempo de producao",
            ["Descricao_"] = "descricao",
            ["Observacoes"] = "observacoes",
            ["Tipo"] = "tipo de movimentacao"
        };

        public static string? ObterPrimeiroErroAmigavel(ModelStateDictionary modelState)
        {
            foreach (var entrada in modelState)
            {
                foreach (var erro in entrada.Value.Errors)
                {
                    var mensagem = erro.ErrorMessage?.Trim();
                    if (!string.IsNullOrWhiteSpace(mensagem))
                    {
                        if (EhMensagemTecnica(mensagem))
                            return $"Preencha o campo {ResolverNomeCampo(entrada.Key)}.";

                        return mensagem;
                    }

                    if (erro.Exception is not null)
                        return $"Preencha o campo {ResolverNomeCampo(entrada.Key)}.";
                }
            }

            return null;
        }

        private static bool EhMensagemTecnica(string mensagem)
        {
            var texto = mensagem.ToLowerInvariant();

            if (texto.Contains("the value") && (texto.Contains("is invalid") || texto.Contains("is not valid")))
                return true;

            if (texto.Contains("could not be converted"))
                return true;

            if (texto.Contains("field is required"))
                return true;

            return false;
        }

        private static string ResolverNomeCampo(string? chave)
        {
            if (string.IsNullOrWhiteSpace(chave))
                return "obrigatorio";

            var semIndice = Regex.Replace(chave, @"\[\d+\]", string.Empty);
            var campo = semIndice.Split('.').LastOrDefault() ?? semIndice;

            if (NomeCampos.TryGetValue(campo, out var nome))
                return nome;

            var humanizado = Regex.Replace(campo, "([a-z])([A-Z])", "$1 $2")
                .Replace("_", " ")
                .Trim();

            return string.IsNullOrWhiteSpace(humanizado)
                ? "obrigatorio"
                : humanizado.ToLowerInvariant();
        }
    }
}
