# Hexagonal Architecture em .NET 9

Projeto de exemplo simples demonstrando **Arquitetura Hexagonal** (também chamada de **Ports & Adapters**) usando .NET 9, ASP.NET Core Web API e xUnit.

O domínio escolhido é propositalmente simples — um cadastro de **Produtos** com criação, consulta e aplicação de desconto — para deixar a estrutura da arquitetura em evidência, sem ruído de regras de negócio complexas.

## O que é Arquitetura Hexagonal

A ideia central (proposta por Alistair Cockburn) é isolar as regras de negócio (o "hexágono") de qualquer detalhe técnico externo — banco de dados, frameworks web, filas, etc. A comunicação entre o núcleo e o mundo exterior acontece sempre através de **portas** (interfaces), nunca de dependências diretas.

- **Portas de entrada (driving/input ports):** interfaces que definem o que a aplicação sabe fazer. São chamadas por quem "aciona" o sistema (uma API REST, um teste, uma CLI).
- **Portas de saída (driven/output ports):** interfaces que definem o que a aplicação precisa do mundo externo (persistir dados, enviar e-mail, chamar outro serviço). São implementadas por adaptadores concretos.
- **Adaptadores:** implementações concretas que conectam o hexágono ao mundo real. Adaptadores **driving** entram no sistema (controllers, consumidores de fila); adaptadores **driven** são chamados pelo sistema (repositórios, clients HTTP).

A regra de dependência é sempre **de fora para dentro**: `Infrastructure` e `WebApi` dependem de `Application`, e `Application` depende de `Domain`. **Nunca o contrário.** `Domain` não conhece nenhuma das outras camadas.

```
                         ┌───────────────────────────────────────┐
                         │              WebApi (driving)          │
                         │     ProductsController (adaptador)     │
                         └───────────────────┬─────────────────────┘
                                             │ chama a porta de entrada
                                             ▼
                         ┌───────────────────────────────────────┐
                         │              Application                │
                         │  Ports/Input  → IProductService (porta) │
                         │  UseCases     → ProductService (caso de │
                         │                 uso, implementa a porta)│
                         │  Ports/Output → IProductRepository      │
                         │                 (porta exigida)         │
                         └───────────────────┬─────────────────────┘
                                             │ é implementada por
                                             ▼
                         ┌───────────────────────────────────────┐
                         │            Infrastructure (driven)      │
                         │   ProductRepository (EF Core/SQLite)    │
                         └───────────────────────────────────────┘

                         ┌───────────────────────────────────────┐
                         │                 Domain                  │
                         │     Product (entidade + invariantes)    │
                         │     DomainException                     │
                         │  (não depende de mais nenhuma camada)   │
                         └───────────────────────────────────────┘
```

## Estrutura do projeto

```
App.sln
src/
  App.Domain/            # núcleo do hexágono
  App.Application/       # casos de uso + portas
  App.Infrastructure/    # adaptadores driven (saída)
  App.WebApi/            # adaptador driving (entrada) + composição/DI
tests/
  App.Application.Tests/ # testes unitários dos casos de uso
```

### `App.Domain` — o núcleo

Contém apenas **entidades, value objects, exceções de domínio e regras de negócio**. Não referencia nenhum outro projeto e não conhece ASP.NET, banco de dados ou DTOs.

- `Entities/Product.cs`: entidade com invariantes protegidas no próprio construtor (nome obrigatório, preço > 0) e comportamento de domínio (`ApplyDiscount`), que valida o percentual antes de aplicar.
- `Exceptions/DomainException.cs`: exceção específica para violação de regra de negócio.

Esta camada poderia ser reaproveitada em qualquer outro contexto (outra API, um worker, um app desktop) sem nenhuma alteração.

### `App.Application` — casos de uso e portas

Orquestra o domínio para realizar casos de uso, mas **não sabe como os dados são persistidos nem como serão expostos**. Referencia apenas `App.Domain`.

- `Ports/Input/IProductService.cs`: **porta de entrada**. Define o que a aplicação oferece (`CreateAsync`, `GetByIdAsync`, `GetAllAsync`, `ApplyDiscountAsync`). É o contrato que qualquer adaptador driving (WebApi, testes, CLI) usa para acionar o sistema.
- `Ports/Output/IProductRepository.cs`: **porta de saída**. Define o que a aplicação precisa do mundo externo para funcionar (persistência). É implementada por um adaptador driven em `Infrastructure`.
- `UseCases/ProductService.cs`: implementação da porta de entrada. Depende apenas da abstração `IProductRepository` (injetada via construtor) — nunca de uma implementação concreta. É aqui que fica a lógica de aplicação (ex.: validar que o produto existe antes de aplicar desconto).
- `DTOs/ProductDto.cs`: objetos de transporte de dados que cruzam a fronteira do hexágono, evitando expor a entidade de domínio diretamente para fora.

### `App.Infrastructure` — adaptadores de saída (driven)

Implementações concretas das portas de saída. Referencia `App.Application` (para conhecer as interfaces que precisa implementar). Usa **EF Core** para persistência.

- `Persistence/AppDbContext.cs`: `DbContext` do EF Core, com `DbSet<Product>`.
- `Persistence/Configurations/ProductConfiguration.cs`: mapeamento (Fluent API) da entidade `Product` — tabela, chave, tamanhos de coluna. `Product` não tem construtor vazio nem setters públicos; o EF Core faz *constructor binding* automaticamente (casa os parâmetros do construtor com as propriedades), então o `Domain` não precisa de nenhum ajuste para ser persistido.
- `Persistence/Repositories/ProductRepository.cs`: implementação de `IProductRepository` usando `AppDbContext`. Poderia ser substituída por Dapper, MongoDB, um client HTTP para outro microsserviço, etc. — **sem alterar uma linha sequer** de `Domain` ou `Application`.
- `DependencyInjection.cs`: extension method `AddPersistence` — é aqui que o provider de banco é escolhido e o `IProductRepository` é registrado no DI.

#### Trocando de banco de dados

O provider é configurado via `appsettings.json` (`Database:Provider` + `ConnectionStrings:Default`) e resolvido em `DependencyInjection.AddPersistence`. Hoje só SQLite está implementado. Para usar outro banco (SQL Server, PostgreSQL, MySQL etc.):

1. Instale o pacote do provider desejado em `App.Infrastructure` (ex.: `dotnet add src/App.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer` ou `Npgsql.EntityFrameworkCore.PostgreSQL`).
2. Adicione um novo `case` no `switch` de `DependencyInjection.AddPersistence` chamando o `Use<Provider>` correspondente (ex.: `options.UseSqlServer(connectionString)`).
3. Ajuste `Database:Provider` e `ConnectionStrings:Default` no `appsettings.json`.

Nenhuma outra camada (`Domain`, `Application`, `WebApi`) precisa mudar.

> **Nota sobre criação de schema**: este projeto usa `Database.EnsureCreated()` (chamado em `Program.cs`) em vez de Migrations, propositalmente — assim o schema é gerado automaticamente a partir do modelo para qualquer provider, sem depender de migrations específicas de um banco. Para um cenário de produção real, o recomendado é usar `dotnet ef migrations` em vez de `EnsureCreated`.

### `App.WebApi` — adaptador de entrada (driving) + composição

Ponto de entrada da aplicação. Referencia `App.Application` (para consumir a porta de entrada) e `App.Infrastructure` (apenas para fazer o *wiring* de DI em `Program.cs` — o controller nunca referencia `Infrastructure` diretamente).

- `Controllers/ProductsController.cs`: adaptador driving. Traduz requisições HTTP em chamadas a `IProductService`. Não conhece a entidade `Product` do domínio nem o repositório concreto.
- `Program.cs`: é onde a "mágica" da inversão de dependência acontece — o *composition root* chama `AddPersistence` para registrar qual adaptador concreto (`ProductRepository`) satisfaz cada porta (`IProductRepository`), e garante a criação do banco (`EnsureCreated`) na inicialização.

### `tests/App.Application.Tests` — testes unitários

Testa `ProductService` (a lógica de aplicação) usando **Moq** para simular a porta `IProductRepository`, sem precisar de banco de dados real ou subir a Web API. Isso é um dos maiores benefícios práticos da arquitetura hexagonal: como as dependências são portas (interfaces), a lógica de negócio é testável em isolamento total.

## Por que separar assim?

- **Testabilidade**: `Domain` e `Application` são testados sem infraestrutura real (sem subir banco, sem subir servidor HTTP).
- **Substituibilidade**: trocar de banco de dados, de framework web, ou de fornecedor de e-mail vira uma troca de adaptador — o núcleo do negócio não muda.
- **Foco no negócio**: a regra de negócio fica isolada de detalhes técnicos, facilitando a leitura e manutenção do que realmente importa: o domínio.

## Pré-requisitos

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

> Este projeto foi criado direcionado ao `net9.0`. Se ao rodar `dotnet build` você receber o erro `NETSDK1045`, é sinal de que o SDK 9 ainda não está instalado na máquina — instale-o pelo link acima (ou baixe outra versão e ajuste o `TargetFramework` nos `.csproj`, se preferir usar o SDK já disponível).

## Como rodar

```bash
# Restaurar e compilar toda a solução
dotnet build

# Rodar a Web API (Swagger disponível em /swagger no ambiente de desenvolvimento)
dotnet run --project src/App.WebApi

# Rodar os testes unitários
dotnet test
```

Ao subir, a Web API cria automaticamente o arquivo SQLite (`app.db`, configurável em `ConnectionStrings:Default` no `appsettings.json`) na pasta de execução, caso ele ainda não exista.

## Exemplos de uso da API

```http
POST /api/products
Content-Type: application/json

{
  "name": "Teclado Mecânico",
  "price": 350.00
}
```

```http
GET /api/products
GET /api/products/{id}

PATCH /api/products/{id}/discount
Content-Type: application/json

{
  "percentage": 10
}
```
