using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<(IReadOnlyList<Beneficiario> Dados, int Total)> ListarAsync(
        int pagina,
        int tamanho,
        StatusBeneficiario? status,
        Guid? planoId,
        CancellationToken cancellationToken)
    {
        var consulta = db.Beneficiarios.AsNoTracking().Include(b => b.Plano).AsQueryable();

        if (status is not null)
        {
            consulta = consulta.Where(b => b.Status == status);
        }

        if (planoId is not null)
        {
            consulta = consulta.Where(b => b.PlanoId == planoId);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var dados = await consulta
            .OrderBy(b => b.DataCadastro)
            .ThenBy(b => b.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);

        return (dados, total);
    }

    public async Task<Beneficiario> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Beneficiarios.Include(b => b.Plano).FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NaoEncontradoException("Beneficiário não encontrado");
    }

    public async Task<Beneficiario> CriarAsync(BeneficiarioRequestDados dados, CancellationToken cancellationToken)
    {
        await GarantirPlanoExisteAsync(dados.PlanoId, cancellationToken);

        var beneficiario = new Beneficiario(dados.NomeCompleto, dados.Cpf, dados.DataNascimento, dados.PlanoId);

        await GarantirCpfDisponivelAsync(beneficiario, cancellationToken);
        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);

        return beneficiario;
    }

    public async Task<Beneficiario> AtualizarAsync(
        Guid id,
        BeneficiarioAtualizacaoDados dados,
        CancellationToken cancellationToken)
    {
        var beneficiario = await ObterAsync(id, cancellationToken);

        var dadosCadastraisMudaram =
            dados.NomeCompleto != beneficiario.NomeCompleto ||
            dados.DataNascimento != beneficiario.DataNascimento ||
            dados.PlanoId != beneficiario.PlanoId;

        if (beneficiario.Status == StatusBeneficiario.INATIVO && dadosCadastraisMudaram)
        {
            throw new ConflitoException("Beneficiário inativo não pode ter dados cadastrais alterados");
        }

        if (dados.PlanoId != beneficiario.PlanoId)
        {
            await GarantirPlanoExisteAsync(dados.PlanoId, cancellationToken);
        }

        beneficiario.DefinirDados(dados.NomeCompleto, dados.DataNascimento);
        beneficiario.DefinirPlano(dados.PlanoId);
        beneficiario.DefinirStatus(dados.Status);

        await SalvarAsync(cancellationToken);

        return beneficiario;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterAsync(id, cancellationToken);

        beneficiario.Excluir();
        await SalvarAsync(cancellationToken);
    }

    private async Task GarantirPlanoExisteAsync(Guid planoId, CancellationToken cancellationToken)
    {
        var existe = await db.Planos.AnyAsync(p => p.Id == planoId, cancellationToken);

        if (!existe)
        {
            throw new NaoProcessavelException(
                "Plano informado não existe",
                [new DetalheErro("plano_id", "inexistente")]);
        }
    }

    private async Task GarantirCpfDisponivelAsync(Beneficiario beneficiario, CancellationToken cancellationToken)
    {
        var emUso = await db.Beneficiarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(b => b.Id != beneficiario.Id && b.Cpf == beneficiario.Cpf, cancellationToken);

        if (emUso)
        {
            throw new ConflitoException(
                "Já existe beneficiário cadastrado com esse CPF",
                [new DetalheErro("cpf", "duplicado")]);
        }
    }

    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            throw new ConflitoException("Já existe beneficiário cadastrado com esse CPF");
        }
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;
}

public sealed record BeneficiarioRequestDados(string? NomeCompleto, string? Cpf, DateOnly? DataNascimento, Guid PlanoId);

public sealed record BeneficiarioAtualizacaoDados(
    string? NomeCompleto,
    DateOnly? DataNascimento,
    Guid PlanoId,
    StatusBeneficiario Status
);