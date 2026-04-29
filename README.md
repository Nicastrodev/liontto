# ðŸª‘ LÃ­ontto MÃ³veis â€” ASP.NET Core + MySQL (XAMPP)

Sistema de gestÃ£o de materiais, produtos, clientes e pedidos.
VersÃ£o migrada de **MongoDB â†’ MySQL** usando **Entity Framework Core + Pomelo**.

---

## Deploy com Railway + Vercel

### Arquitetura recomendada

- Backend ASP.NET: Railway (ou outro host com suporte .NET)
- Banco MySQL: Railway
- Frontend/dominio publico: Vercel (proxy para o backend)

### 1) Variaveis no backend (Railway)

Com base nas variaveis que voce enviou, mantenha no servico do backend:

- `MYSQL_URL`
- `MYSQL_PUBLIC_URL`
- `MYSQLHOST`
- `MYSQLPORT`
- `MYSQLUSER`
- `MYSQLPASSWORD`
- `MYSQL_ROOT_PASSWORD`
- `MYSQL_DATABASE`
- `MYSQLDATABASE`

Tambem configure:

- `ASPNETCORE_ENVIRONMENT=Production`
- `SEED_ON_STARTUP=false`
- `APPLY_MIGRATIONS_ON_STARTUP=false`
- `CORS_ALLOWED_ORIGINS=https://SEU-PROJETO.vercel.app`

Observacao: o backend agora resolve conexao automaticamente usando a prioridade:

1. `ConnectionStrings__MySQL`
2. `DB_CONNECTION`
3. `MYSQL_URL`
4. `MYSQL_PUBLIC_URL`
5. `MYSQLHOST` + `MYSQLPORT` + `MYSQLUSER` + `MYSQLPASSWORD` + `MYSQLDATABASE`

### 2) Variavel no frontend (Vercel)

No projeto da Vercel, configure:

- `BACKEND_ORIGIN=https://URL-PUBLICA-DO-BACKEND`

Exemplo:

```bash
BACKEND_ORIGIN=https://liontto-backend.up.railway.app
```

### 3) Arquivos de deploy usados

- `vercel.json` (rewrites e headers)
- `api/proxy.js` (proxy para backend)
- `.vercelignore` (evita upload de arquivos .NET no deploy da Vercel)
- `package.json` (runtime Node para a function)

### 4) Proximo passo (ordem correta)

1. Suba/valide o backend no Railway.
2. Confirme que a rota publica do backend responde (ex: `https://SEU_BACKEND/`).
3. Configure `CORS_ALLOWED_ORIGINS` no backend com o dominio da Vercel.
4. Configure `BACKEND_ORIGIN` na Vercel apontando para o backend.
5. Rode deploy da Vercel.
6. Teste criar/editar/excluir no frontend publicado.

### 5) Seguranca imediata

Como a senha root do MySQL foi compartilhada em texto, gere uma nova senha no Railway e atualize as variaveis dependentes antes do go-live.

---
## ðŸ› ï¸ PrÃ©-requisitos

| Software | VersÃ£o mÃ­nima | Link |
|---|---|---|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| XAMPP | qualquer | https://www.apachefriends.org |

---

## âš™ï¸ ConfiguraÃ§Ã£o do XAMPP

### 1. Inicie o MySQL no XAMPP
Abra o **XAMPP Control Panel** e clique em **Start** ao lado de **MySQL**.

### 2. Crie o banco de dados

Abra o **phpMyAdmin** (`http://localhost/phpmyadmin`) e execute:

```sql
CREATE DATABASE liontto CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Ou via linha de comando:
```bash
# Windows
"C:\xampp\mysql\bin\mysql.exe" -u root -e "CREATE DATABASE liontto CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# Linux/Mac
/opt/lampp/bin/mysql -u root -e "CREATE DATABASE liontto CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

> **As tabelas sÃ£o criadas automaticamente** pelo EF Core (`EnsureCreated`) na primeira execuÃ§Ã£o da aplicaÃ§Ã£o.

---

## ðŸ”Œ Connection String

Edite `appsettings.json` se necessÃ¡rio:

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=liontto;User=root;Password=;CharSet=utf8mb4;"
  }
}
```

- **Password vazio** = configuraÃ§Ã£o padrÃ£o do XAMPP (sem senha para root)
- Se vocÃª definiu uma senha para o root do MySQL, coloque-a no campo `Password=`
- Se o MySQL estiver em outra porta, altere `Port=3306`

---

## â–¶ï¸ Executar a aplicaÃ§Ã£o

```bash
# Na raiz do projeto (onde estÃ¡ o .csproj)
dotnet restore
dotnet run
```

Acesse: **http://localhost:5000**

Na primeira execuÃ§Ã£o, o sistema:
1. Cria automaticamente todas as tabelas no MySQL
2. Insere dados de exemplo (materiais, produtos, clientes)

---

## ðŸ—‚ï¸ Estrutura do Projeto

```
LionttoMoveis/
â”œâ”€â”€ Data/
â”‚   â””â”€â”€ AppDbContext.cs         # DbContext EF Core (substitui IMongoDatabase)
â”œâ”€â”€ Models/
â”‚   â”œâ”€â”€ EntidadeBase.cs         # Classe base com Id (int) e CriadoEm
â”‚   â”œâ”€â”€ Material.cs             # Tabela: materiais
â”‚   â”œâ”€â”€ Cliente.cs              # Tabela: clientes
â”‚   â”œâ”€â”€ Produto.cs              # Tabelas: produtos + materiais_do_produto
â”‚   â”œâ”€â”€ Pedido.cs               # Tabelas: pedidos + itens_do_pedido
â”‚   â””â”€â”€ Movimentacao.cs         # Tabela: movimentacoes
â”œâ”€â”€ Repository/
â”‚   â”œâ”€â”€ IRepository.cs          # Contrato CRUD genÃ©rico
â”‚   â”œâ”€â”€ MySqlRepository.cs      # ImplementaÃ§Ã£o base com EF Core
â”‚   â””â”€â”€ Repositorios.cs         # RepositÃ³rios especÃ­ficos por entidade
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ EstoqueService.cs       # Regras de negÃ³cio de estoque
â”‚   â””â”€â”€ SeedService.cs          # Dados de exemplo na primeira execuÃ§Ã£o
â”œâ”€â”€ Controllers/                # Controllers MVC
â”œâ”€â”€ Views/                      # Views Razor (.cshtml)
â”œâ”€â”€ appsettings.json            # Connection string MySQL
â””â”€â”€ Program.cs                  # Startup: EF Core + DI + Seed
```

---

## ðŸ”„ DiferenÃ§as MongoDB â†’ MySQL

| Aspecto | MongoDB (original) | MySQL (esta versÃ£o) |
|---|---|---|
| **ID** | `string` (ObjectId 24 chars) | `int` (AUTO_INCREMENT) |
| **Driver** | `MongoDB.Driver` | `Pomelo.EntityFrameworkCore.MySql` |
| **ORM** | Nenhum (driver direto) | Entity Framework Core 8 |
| **Schema** | Sem schema (NoSQL) | Tabelas com FK e Ã­ndices |
| **Embedded docs** | `MaterialDoProduto` dentro de `Produto` | Tabela separada `materiais_do_produto` |
| **Itens do pedido** | Embedded em `Pedido` | Tabela separada `itens_do_pedido` |
| **Contexto** | `IMongoDatabase` | `AppDbContext : DbContext` |
| **RepositÃ³rio base** | `MongoRepository<T>` | `MySqlRepository<T>` |
| **ConexÃ£o** | `appsettings.json` â†’ `MongoDB.ConnectionString` | `appsettings.json` â†’ `ConnectionStrings.MySQL` |

---

## ðŸ—„ï¸ Tabelas criadas no MySQL

```sql
materiais           -- Material (nome, unidade, quantidade, preco_unitario...)
clientes            -- Cliente (nome, telefone, email, endereco)
produtos            -- Produto (nome, descricao, preco_base, tempo_producao_dias)
materiais_do_produto-- RelaÃ§Ã£o N:N Produto â†” Material (quantidade_necessaria)
pedidos             -- Pedido (cliente_id, status, valor_total, datas...)
itens_do_pedido     -- Itens de cada pedido (produto_id, quantidade, preco_unitario)
movimentacoes       -- HistÃ³rico de entrada/saÃ­da de materiais
```

---

## ðŸ§ª Migrations (opcional, para produÃ§Ã£o)

Para usar Migrations em vez de `EnsureCreated`:

```bash
# Instalar ferramenta EF (uma vez)
dotnet tool install --global dotnet-ef

# Criar migration inicial
dotnet ef migrations add Inicial

# Aplicar ao banco
dotnet ef database update
```

---

## ðŸ’¡ Conceitos POO mantidos

- **HeranÃ§a**: `EntidadeBase` â†’ `Material`, `Cliente`, `Produto`, `Pedido`, `Movimentacao`
- **Encapsulamento**: `StatusEstoque`, `RecalcularTotal()`, `AvancarStatus()` no model
- **Polimorfismo**: `Descricao()` sobrescrito em cada entidade
- **AbstraÃ§Ã£o**: `IRepository<T>` â€” controllers nÃ£o conhecem EF Core diretamente
- **InjeÃ§Ã£o de DependÃªncia**: repositÃ³rios e services injetados pelo ASP.NET Core


