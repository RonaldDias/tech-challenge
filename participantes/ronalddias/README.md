# Entrega — ronalddias

## 1. Resumo da entrega

Corrigi os defeitos do módulo de Beneficiários e implementei os endpoints que faltavam
(GET por id, PUT, DELETE), paginação e filtros combináveis na listagem, seguindo o padrão
já estabelecido pelo módulo de Planos (camadas, tratamento de erro, exclusão lógica).
Adicionei validação de CPF com dígito verificador, índice único no banco para garantir
unicidade sob concorrência, e um teste de concorrência real disparando duas requisições
simultâneas. No frontend, implementei a listagem de Beneficiários (com filtros, paginação
e tratamento de erro/loading) e o formulário de cadastro/edição, seguindo o padrão do bloco
de Planos.

Não consegui: (1) validar o frontend rodando de ponta a ponta contra a API, nem (2) publicar
as imagens no Docker Hub — ambos por falha do ambiente local (WSL/Docker) na reta final do
prazo, detalhado na seção 2.5.

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

**1. Rota do controller com acento causava 404 em todos os endpoints de Beneficiários**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** o atributo de rota estava `[Route("beneficiários")]`, com acento.
  Como a URL é comparada caractere a caractere, nenhuma requisição para `/beneficiarios`
  (sem acento, como toda a spec e os testes chamam) batia com essa rota.
- **Como percebi:** depois de corrigir vários outros pontos, a suíte de testes continuava
  devolvendo exatamente as mesmas 13 falhas de antes, todas como `404`. Isso não fazia
  sentido para os defeitos que eu já tinha corrigido, então fui direto conferir a declaração
  da rota linha a linha.
- **Como corrigi:** removi o acento, `[Route("beneficiarios")]`.
- **O que quebraria em produção:** o módulo inteiro de Beneficiários ficaria inacessível.
  Pior: como o build compila normalmente (acento é um caractere válido em uma string), isso
  não aparece em nenhuma etapa de compilação — só em runtime, ao chamar a rota. Um erro
  silencioso desse tipo poderia ir para produção sem ninguém perceber até um cliente
  reportar que a funcionalidade "não existe".

**2. `plano_id` inexistente devolvia `500` em vez de `422`**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
  (versão original, antes da minha correção)
- **O que estava errado:** o código original não validava se o `plano_id` enviado
  correspondia a um plano existente antes de tentar inserir o beneficiário. O banco tem uma
  foreign key para `Planos`, então a inserção estourava uma violação de constraint, que
  subia como exceção não tratada e virava `500` — com o middleware global escondendo os
  detalhes internos (o que é o comportamento correto do middleware, mas o código de negócio
  deveria ter barrado isso antes, com uma mensagem útil).
- **Como percebi:** o teste `Criar_com_plano_inexistente_deve_devolver_422` apontava
  `Expected: UnprocessableEntity, Actual: InternalServerError`.
- **Como corrigi:** adicionei `GarantirPlanoExisteAsync` no `BeneficiarioServico`, que
  consulta se o plano existe (usando o filtro de exclusão lógica já ativo, então plano
  excluído também conta como inexistente automaticamente) antes de qualquer tentativa de
  inserir, lançando `NaoProcessavelException` (422) com o campo e motivo recusado.
- **O que quebraria em produção:** um cliente da API mandando um `plano_id` errado (erro de
  digitação, plano descontinuado, etc.) receberia um erro genérico de servidor em vez de uma
  mensagem clara indicando que o problema é o `plano_id`. Isso dificulta debugging para quem
  integra com a API e pode mascarar um bug real de outra causa atrás de um 500 genérico.

**3. Unicidade de CPF garantida só por validação em memória, não por constraint no banco**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Infraestrutura/AppDbContext.cs`
- **O que estava errado:** não havia índice único na coluna `Cpf`. A checagem de duplicidade
  dependia inteiramente de uma consulta prévia ("já existe esse CPF?") antes do insert.
- **Como percebi:** ao ler a SPEC.md (§4.1), que exige explicitamente unicidade garantida
  mesmo sob requisições simultâneas.
- **Como corrigi:** adicionei `entidade.HasIndex(b => b.Cpf).IsUnique()` no `AppDbContext`,
  gerei a migration correspondente, e escrevi um teste
  (`Criar_simultaneamente_com_mesmo_cpf_deve_garantir_unicidade`) que dispara duas
  requisições de criação com o mesmo CPF via `Task.WhenAll` (sem aguardar uma terminar
  antes de iniciar a outra) e confirma que uma recebe `201` e a outra `409`.
- **O que quebraria em produção:** sob carga real, duas requisições podem ser processadas
  por threads/processos diferentes que consultam o banco quase ao mesmo tempo — as duas
  veem "CPF livre" antes de qualquer uma delas inserir, e as duas inserem. Sem constraint no
  banco, dois beneficiários com o mesmo CPF coexistiriam, violando uma regra de negócio
  central do sistema.

### 2.2 Pontos em que a especificação não definiu o comportamento

Não identifiquei lacunas além das inconsistências documentadas em 2.3.

### 2.3 Inconsistências que percebi

**1. Tamanho padrão de página**

- **A spec diz:** "quando ausente, 10" (§3).
- **O teste espera:** `Listar_sem_informar_tamanho_deve_devolver_20_itens_por_pagina`, ou
  seja, 20.
- **Segui:** o teste (valor padrão 20).
- **Por quê:** a suíte pública é o contrato que a avaliação automática também exercita, e o
  próprio README do desafio trata "suíte verde" como o esperado. Como as duas fontes divergem
  e não há como agradar as duas, priorizei manter compatibilidade com o teste público.

**2. Beneficiário `INATIVO` e alteração de dados cadastrais**

- **A spec diz:** um beneficiário `INATIVO` é um registro congelado — dados cadastrais não
  podem ser alterados, e a tentativa deve responder `409` (§2.3).
- **O teste espera:** `Atualizar_dados_de_beneficiario_inativo_deve_devolver_200` atualiza
  `nome_completo` e `data_nascimento` de um beneficiário `INATIVO` e espera `200`.
- **Segui:** a spec. Implementei o bloqueio (`409`) quando dados cadastrais mudam com o
  beneficiário `INATIVO`, deixando esse teste específico vermelho de propósito.
- **Por quê:** "registro congelado" é uma regra de negócio explícita e não-trivial — o tipo
  de regra que, se ignorada, teria consequência real (alguém editando dados de um
  beneficiário que deveria estar bloqueado). Segui o texto normativo da spec em vez do teste
  porque a spec é mais explícita e específica sobre essa regra do que o teste é sobre o
  motivo de esperar `200`.

### 2.4 Decisões técnicas

- **Paginação com `OrderBy(DataCadastro).ThenBy(Id)`:** a spec exige estabilidade ao
  percorrer páginas. Ordenar só por `DataCadastro` deixaria o desempate indefinido entre
  registros criados no mesmo instante; adicionar `Id` como critério secundário garante
  ordem 100% determinística.
- **`IgnoreQueryFilters()` na checagem de CPF disponível:** como o filtro global de exclusão
  lógica esconde beneficiários excluídos por padrão, a checagem de duplicidade de CPF
  precisa desligar esse filtro explicitamente — porque a spec exige que o CPF de um
  beneficiário excluído continue "ocupado".
- **Separação `Request`/`RequestDados`:** os contratos HTTP (`Api/Contratos`) e os DTOs
  internos usados pelo `BeneficiarioServico` (`Aplicacao`) são tipos diferentes, mesmo com
  conteúdo parecido — para que mudanças no formato JSON não obriguem mudanças na camada de
  regra de negócio, e vice-versa. Segue o mesmo espírito do `PlanoServico`, que já recebe
  parâmetros soltos em vez do record de contrato.

### 2.5 O que ficou de fora

- **Validação end-to-end do frontend rodando contra a API real:** o frontend foi
  implementado (listagem com filtros/paginação, formulário de cadastro/edição, tratamento
  de erro visível, estados de carregamento/vazio), mas o ambiente de desenvolvimento
  (WSL) começou a apresentar falhas de I/O e travamentos (`SIGBUS`, corrupção de
  sistema de arquivos) na reta final do prazo, impedindo terminar a validação manual
  completa de todos os fluxos (criar, editar, excluir, filtrar) rodando ao vivo.
- **Publicação das imagens no Docker Hub:** pelo mesmo motivo — o Docker dentro do WSL
  travou de forma consistente (erros `SIGBUS`/`read-only file system` durante o build da
  imagem) e não foi possível gerar e publicar as imagens `linux/amd64`/`linux/arm64` a
  tempo. O `docker-compose.yml` desta entrega documenta o formato pretendido, mas referencia
  imagens que não existem publicamente — portanto não passa em `./verificar.sh`.

## 3. Uso de IA

**Nível de uso:** intenso.

### 3.1 Ferramentas

- Claude (Anthropic), via chat — usado durante toda a sessão de desenvolvimento, em estilo
  de par de programação: eu implementava/colava no editor, rodava build/testes, e discutia
  cada resultado antes do próximo passo.

### 3.2 Os 3 prompts que mais influenciaram o resultado

**Prompt 1**
os 13 testes falhando, cole a saída do dotnet test

- **O que aceitei:** o agrupamento das 13 falhas em causas raiz (POST com defeitos, GET sem
  envelope, endpoints não implementados), que guiou a ordem de correção.
- **O que descartei:** nada — foi diagnóstico inicial, não código.

**Prompt 2**
[pedido para seguir o padrão do PlanoServico ao implementar BeneficiarioServico]

- **O que aceitei:** a estrutura geral (serviço separado do controller, exceções de domínio,
  Include para evitar N+1, índice único + query filter no DbContext).
- **O que descartei/ajustei:** precisei corrigir manualmente, depois de identificar via
  build/teste: o registro faltante no DI (`AddScoped<BeneficiarioServico>`), o acento na
  rota, e o bind incorreto do parâmetro `plano_id` (camelCase vs. snake_case) — nenhum desses
  três veio certo da IA de primeira; foram encontrados por mim rodando testes e comparando
  resultado esperado vs. obtido.

  ### 3.3 O que fiz sem IA

- Rodei e interpretei toda a saída de `dotnet build`/`dotnet test` para decidir o que ainda
  faltava corrigir a cada iteração — a IA não tinha acesso direto ao meu ambiente.
- Resolvi sozinho, com orientação, os problemas de ambiente (extensão C# corrompida no WSL,
  versão do Node.js desatualizada para `ng serve`, configuração de CORS `localhost` vs.
  `127.0.0.1`).
- Encontrei o bug do parâmetro `plano_id` combinando a matemática do resultado do teste
  (4 esperado, 7 obtido — batendo com "filtro de status aplicado, filtro de plano ignorado")
  antes mesmo de olhar o código.
- Tomei as duas decisões de spec × teste divergentes (seção 2.3) e decidi ativamente seguir
  a spec no caso do beneficiário `INATIVO`, mesmo isso significando deixar um teste público
  vermelho.

### 3.4 O que ainda não domino

- A fórmula exata do módulo 11 usada em `CalcularDigito` (cálculo do dígito verificador do
  CPF) eu sei explicar o que ela faz e por que existe, mas não eu que a derivei — é o
  algoritmo padrão definido pela Receita Federal, que eu segui sem questionar a matemática
  por trás dos pesos decrescentes.

## 4. Perguntas de compreensão

### 4.1 Concorrência

Se duas requisições simultâneas tentarem criar beneficiários com o mesmo CPF, a garantia de
unicidade não vem de nenhuma verificação em C# — vem de um índice único no PostgreSQL,
declarado em `AppDbContext.cs` como `entidade.HasIndex(b => b.Cpf).IsUnique()`. A checagem
que o `BeneficiarioServico` faz antes de inserir (`GarantirCpfDisponivelAsync`) existe só
para dar uma resposta de erro mais rápida e clara no caso comum (sem concorrência real), mas
ela sozinha tem uma janela de corrida: as duas requisições podem consultar o banco "ao mesmo
tempo", ambas verem que o CPF está livre, e ambas tentarem inserir. Quando isso acontece, o
banco deixa a primeira inserção passar e recusa a segunda por violar a constraint única
(código Postgres `23505`); o método `SalvarAsync` captura essa exceção especificamente e a
converte em `ConflitoException` (409), em vez de deixar vazar como erro 500. Escrevi o teste
`Criar_simultaneamente_com_mesmo_cpf_deve_garantir_unicidade`, que dispara duas requisições
via `Task.WhenAll` sem aguardar uma terminar antes de iniciar a outra, e confirma que exatamente
uma resposta é `201` e a outra `409` — nunca as duas `201`.

### 4.2 Um defeito que você corrigiu

O `BeneficiariosController.cs` original tinha a rota declarada como `[Route("beneficiários")]`,
com acento — meu próprio erro de digitação ao reescrever o controller, na verdade, não um
defeito do código base original, mas ilustra bem o ponto. O compilador não acusa nada,
porque um "á" é um caractere perfeitamente válido dentro de uma string C#; o problema só
aparece em runtime, quando uma requisição para `/beneficiarios` (sem acento, como a spec e
qualquer cliente real chamariam) não encontra correspondência com a rota registrada, e o
ASP.NET devolve 404 padrão para absolutamente todos os endpoints daquele controller. Descobri
isso porque, depois de corrigir vários outros defeitos de negócio, a suíte continuava
devolvendo exatamente as mesmas 13 falhas — o que não fazia sentido, e me fez conferir a
declaração da rota literalmente caractere por caractere. Em produção, esse tipo de defeito é
particularmente perigoso porque não aparece em nenhuma etapa de build ou análise estática:
o serviço sobe normalmente, o `/health` responde, o Swagger até lista a rota — só que ela
nunca é alcançável por ninguém de fora. Um erro assim só seria percebido quando um cliente
reportasse "essa funcionalidade não existe", o que poderia levar dias ou semanas dependendo
do volume de uso daquele endpoint específico.

### 4.3 O trecho mais complexo

O trecho mais complexo do projeto foi o cálculo do dígito verificador do CPF, em
`Dominio/CpfValidador.cs`:

```csharp
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
```

O CPF tem 11 dígitos: 9 dígitos "base" e 2 dígitos verificadores, calculados a partir dos
outros. Essa função recebe a lista de dígitos já conhecidos e um `pesoInicial` (10 para
calcular o primeiro dígito verificador, 11 para o segundo, já usando o primeiro verificador
recém-calculado). O laço `for` percorre cada um dos dígitos "base" disponíveis e multiplica
cada um por um peso decrescente — o primeiro dígito é multiplicado por `pesoInicial` (10 ou
11), o segundo por `pesoInicial - 1`, e assim sucessivamente — somando tudo em `soma`. Essa
soma ponderada é então multiplicada por 10 e dividida por 11, com `%` pegando só o resto
dessa divisão. A regra do módulo 11 diz que, se esse resto for 10, o dígito verificador na
prática vira 0 (porque um único dígito não pode valer "10"); fora esse caso especial, o resto
já é o próprio dígito verificador. A função é chamada duas vezes em `EhValido` — uma para
recalcular o 10º dígito a partir dos 9 primeiros, outra para recalcular o 11º a partir dos
10 primeiros (9 originais + o 10º recém-validado) — e o CPF só é considerado válido se os
dois dígitos recalculados baterem exatamente com os que vieram na entrada.