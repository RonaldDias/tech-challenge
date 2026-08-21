using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
public class BeneficiariosController(BeneficiarioServico servico) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BeneficiariosPaginadosResponse>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        [FromQuery] StatusBeneficiario? status = null,
        [FromQuery(Name = "plano_id")] Guid? planoId = null,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || tamanho is < 1 or > 100)
        {
            return BadRequest(new ErroResponse(
                "parametros_invalidos",
                "Parâmetros de paginação inválidos",
                [new DetalheErro("pagina_ou_tamanho", "fora_do_intervalo")]
            ));
        }

        var (dados, total) = await servico.ListarAsync(pagina, tamanho, status, planoId, cancellationToken);

        return Ok(new BeneficiariosPaginadosResponse(
            dados.Select(BeneficiarioResponse.De).ToList(),
            pagina,
            tamanho,
            total
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BeneficiarioResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await servico.ObterAsync(id, cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpPost]
    public async Task<ActionResult<BeneficiarioResponse>> Criar(
        BeneficiarioRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiario = await servico.CriarAsync(
            new BeneficiarioRequestDados(request.NomeCompleto, request.Cpf, request.DataNascimento, request.PlanoId),
            cancellationToken);

        return CreatedAtAction(
            nameof(Obter),
            new { id = beneficiario.Id },
            BeneficiarioResponse.De(beneficiario)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BeneficiarioResponse>> Atualizar(
        Guid id,
        BeneficiarioAtualizacaoRequest request,
        CancellationToken cancellationToken)
    {
        var beneficiario = await servico.AtualizarAsync(
            id,
            new BeneficiarioAtualizacaoDados(request.NomeCompleto, request.DataNascimento, request.PlanoId, request.Status),
            cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await servico.ExcluirAsync(id, cancellationToken);

        return NoContent();
    }
}