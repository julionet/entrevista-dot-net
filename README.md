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
- `Ports/Input/IAuthService.cs`: **porta de entrada**. Define `RegisterAsync` e `LoginAsync`.
- `Ports/Output/IProductRepository.cs`: **porta de saída**. Define o que a aplicação precisa do mundo externo para funcionar (persistência). É implementada por um adaptador driven em `Infrastructure`.
- `Ports/Output/IUserRepository.cs`, `IPasswordHasher.cs`, `IJwtTokenGenerator.cs`: **portas de saída** usadas por `AuthService` — persistência de usuário, geração/verificação de hash de senha e geração do token, respectivamente. A Application não sabe (nem precisa saber) qual algoritmo de hash ou formato de token é usado por trás dessas portas.
- `UseCases/ProductService.cs`: implementação da porta de entrada. Depende apenas da abstração `IProductRepository` (injetada via construtor) — nunca de uma implementação concreta. É aqui que fica a lógica de aplicação (ex.: validar que o produto existe antes de aplicar desconto).
- `UseCases/AuthService.cs`: implementação de `IAuthService`. No registro, valida que o e-mail ainda não existe e persiste o usuário com a senha já hasheada (nunca em texto puro). No login, busca o usuário, valida a senha e o status `IsActive`, e gera o token via `IJwtTokenGenerator`. Mensagens de erro de login são propositalmente genéricas ("Email ou senha inválidos") para não revelar se o e-mail existe.
- `DTOs/ProductDto.cs`, `DTOs/AuthDtos.cs`: objetos de transporte de dados que cruzam a fronteira do hexágono, evitando expor a entidade de domínio diretamente para fora.

### `App.Infrastructure` — adaptadores de saída (driven)

Implementações concretas das portas de saída. Referencia `App.Application` (para conhecer as interfaces que precisa implementar). Usa **EF Core** para persistência.

- `Persistence/AppDbContext.cs`: `DbContext` do EF Core, com `DbSet<Product>` e `DbSet<User>`.
- `Persistence/Configurations/ProductConfiguration.cs`, `UserConfiguration.cs`: mapeamento (Fluent API) das entidades — tabela, chave, tamanhos de coluna (`User.Email` tem índice único). Nem `Product` nem `User` têm construtor vazio ou setters públicos; o EF Core faz *constructor binding* automaticamente (casa os parâmetros do construtor com as propriedades), então o `Domain` não precisa de nenhum ajuste para ser persistido.
- `Persistence/Repositories/ProductRepository.cs`, `UserRepository.cs`: implementações de `IProductRepository`/`IUserRepository` usando `AppDbContext`. Poderiam ser substituídas por Dapper, MongoDB, um client HTTP para outro microsserviço, etc. — **sem alterar uma linha sequer** de `Domain` ou `Application`.
- `Security/PasswordHasher.cs`: implementação de `IPasswordHasher` usando `PasswordHasher<T>` do ASP.NET Core Identity (PBKDF2).
- `Security/JwtTokenGenerator.cs`: implementação de `IJwtTokenGenerator`, gera o JWT (claims `sub`, `email`, `jti`) assinado com HMAC-SHA256, usando as configurações de `JwtSettings`.
- `Security/JwtSettings.cs`: opções (`Issuer`, `Audience`, `SecretKey`, `ExpirationMinutes`) — bind da seção `Jwt` do `appsettings.json`.
- `DependencyInjection.cs`: `AddPersistence` — escolhe o provider de banco e registra `IProductRepository`/`IUserRepository`. `AddSecurity` — registra `IPasswordHasher`, `IJwtTokenGenerator` e faz o bind de `JwtSettings`.

#### Trocando de banco de dados

O provider é configurado via `appsettings.json` (`Database:Provider` + `ConnectionStrings:Default`) e resolvido em `DependencyInjection.AddPersistence`. Hoje só SQLite está implementado. Para usar outro banco (SQL Server, PostgreSQL, MySQL etc.):

1. Instale o pacote do provider desejado em `App.Infrastructure` (ex.: `dotnet add src/App.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer` ou `Npgsql.EntityFrameworkCore.PostgreSQL`).
2. Adicione um novo `case` no `switch` de `DependencyInjection.AddPersistence` chamando o `Use<Provider>` correspondente (ex.: `options.UseSqlServer(connectionString)`).
3. Ajuste `Database:Provider` e `ConnectionStrings:Default` no `appsettings.json`.

Nenhuma outra camada (`Domain`, `Application`, `WebApi`) precisa mudar.

> **Nota sobre criação de schema**: este projeto usa `Database.EnsureCreated()` (chamado em `Program.cs`) em vez de Migrations, propositalmente — assim o schema é gerado automaticamente a partir do modelo para qualquer provider, sem depender de migrations específicas de um banco. Para um cenário de produção real, o recomendado é usar `dotnet ef migrations` em vez de `EnsureCreated`.

### `App.WebApi` — adaptador de entrada (driving) + composição

Ponto de entrada da aplicação. Referencia `App.Application` (para consumir a porta de entrada) e `App.Infrastructure` (apenas para fazer o *wiring* de DI em `Program.cs` — o controller nunca referencia `Infrastructure` diretamente).

- `Controllers/ProductsController.cs`: adaptador driving. Traduz requisições HTTP em chamadas a `IProductService`. Não conhece a entidade `Product` do domínio nem o repositório concreto. Protegido com `[Authorize]` — exige um Bearer token válido.
- `Controllers/AuthController.cs`: adaptador driving. Expõe `POST /api/auth/register` e `POST /api/auth/login`, traduzindo requisições HTTP em chamadas a `IAuthService`. Marcado com `[AllowAnonymous]` (não exige token, por razões óbvias).
- `Program.cs`: é onde a "mágica" da inversão de dependência acontece — o *composition root* chama `AddPersistence`/`AddSecurity` para registrar os adaptadores concretos, garante a criação do banco (`EnsureCreated`) na inicialização, e configura o middleware de autenticação JWT Bearer (`AddAuthentication().AddJwtBearer(...)`).

## Autenticação (JWT Bearer)

- `POST /api/auth/register`: cria um usuário (`Email`, `Password`). A senha nunca é persistida em texto puro — é hasheada com `PasswordHasher<T>` do ASP.NET Core Identity antes de ir para o banco. E-mail duplicado retorna `400`.
- `POST /api/auth/login`: valida e-mail/senha e devolve um JWT (`{ "token": "..." }`). Credenciais inválidas ou usuário inativo retornam `401` com mensagem genérica (não revela se o e-mail existe).
- Todas as rotas de `ProductsController` exigem `Authorization: Bearer {token}`; sem token válido, a resposta é `401`.
- As configurações do token ficam em `appsettings.json`, seção `Jwt` (`Issuer`, `Audience`, `SecretKey`, `ExpirationMinutes`).

> **Importante**: a `SecretKey` no `appsettings.json` é apenas um valor de exemplo para rodar o projeto localmente. Em qualquer ambiente real, substitua-a por variável de ambiente, User Secrets (`dotnet user-secrets`) ou um cofre de segredos (Azure Key Vault, AWS Secrets Manager etc.) — nunca deixe uma secret real versionada no repositório.

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
POST /api/auth/register
Content-Type: application/json

{
  "email": "joao@teste.com",
  "password": "Senha123!"
}
```

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "joao@teste.com",
  "password": "Senha123!"
}
```

A resposta do login traz o token: `{ "token": "eyJhbGci..." }`. Use-o no header `Authorization` das chamadas a `/api/products`:

```http
POST /api/products
Authorization: Bearer eyJhbGci...
Content-Type: application/json

{
  "name": "Teclado Mecânico",
  "price": 350.00
}
```

```http
GET /api/products
Authorization: Bearer eyJhbGci...

GET /api/products/{id}
Authorization: Bearer eyJhbGci...

PATCH /api/products/{id}/discount
Authorization: Bearer eyJhbGci...
Content-Type: application/json

{
  "percentage": 10
}
```
