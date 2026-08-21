namespace Desafio.Api.Dominio;

public enum StatusBeneficiario
{
    ATIVO,
    INATIVO
}

public class Beneficiario
{

    private Beneficiario()
    { 
    }

    public Beneficiario(string? nomeCompleto, string? cpf, DateOnly? dataNascimento, Guid planoId)
    {
        Id = Guid.NewGuid();
        DefinirCpf(cpf);
        DefinirDados(nomeCompleto, dataNascimento);
        PlanoId = planoId;
        DataCadastro = DateTime.UtcNow;
    }
    public Guid Id { get; private set; }

    public string NomeCompleto { get; private set; } = null!;

    public string Cpf { get; private set; } = null!;

    public DateOnly DataNascimento { get; private set; }

    public StatusBeneficiario Status { get; private set; }

    public Guid PlanoId { get; private set; }

    public Plano? Plano { get; private set; }

    public DateTime DataCadastro { get; private set; }

    public DateTime? ExcluidoEm { get; private set; }

    public void DefinirDados(string? nomeCompleto, DateOnly? dataNascimento)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;

        var detalhes = new List<DetalheErro>();

        if (nomeCompleto.Length == 0)
        {
            detalhes.Add(new DetalheErro("nome_completo", "obrigatorio"));
        }
        else if (nomeCompleto.Length is < 3 or > 120)
        {
            detalhes.Add(new DetalheErro("nome_completo", "tamanho_invalido"));
        }

        if (dataNascimento is null)
        {
            detalhes.Add(new DetalheErro("data_nascimento", "obrigatorio"));
        }
        else if (dataNascimento.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(new DetalheErro("data_nascimento", "deve_ser_informada"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException("Dados do beneficiário inválido", detalhes);
        }

        NomeCompleto = nomeCompleto;
        DataNascimento = dataNascimento!.Value;
    }

    public void DefinirPlano(Guid planoId) => PlanoId = planoId;

    public void DefinirStatus(StatusBeneficiario status) => Status = status;

    public void Excluir() => ExcluidoEm = DateTime.UtcNow;

    private void DefinirCpf(string? cpf)
    {
        cpf = cpf?.Trim() ?? string.Empty;

        if (!CpfValidador.EhValido(cpf))
        {
            throw new ValidacaoException(
                "Dados do beneficiário invalidos",
                [new DetalheErro("cpf", "invalido")]
            );
        }

        Cpf = cpf;
    }
}
