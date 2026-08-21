using System.Text.RegularExpressions;

namespace Desafio.Api.Dominio;

public static partial class CpfValidador
{
    public static bool EhValido(string? cpf)
    {
        if (cpf is null || !FormatoDoCpf().IsMatch(cpf))
        {
            return false;
        }

        var digitos = cpf.Select(c => c - '0').ToList();

        if (digitos.Distinct().Count() == 1)
        {
            return false;
        }

        return digitos[9] == CalcularDigito(digitos, 10) && digitos[10] == CalcularDigito(digitos, 11);
    }

    private static int CalcularDigito(IReadOnlyList<int> digitos, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < pesoInicial - 1; i++)
        {
            soma += digitos[i] * (pesoInicial - i);
        }

        var resto = soma * 10 % 11;

        return resto == 10 ? 0 : resto;
    }

    [GeneratedRegex("^[0-9]{11}$")]
    private static partial Regex FormatoDoCpf();
}