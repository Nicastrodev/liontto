# 🪑 Líontto Móveis — ASP.NET Core + MySQL (XAMPP)

Sistema de gestão de materiais, produtos, clientes e pedidos.
Versão migrada de **MongoDB → MySQL** usando **Entity Framework Core + Pomelo**.

---

## 🛠️ Pré-requisitos

| Software | Versão mínima | Link |
|---|---|---|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| XAMPP | qualquer | https://www.apachefriends.org |

---

## ⚙️ Configuração do XAMPP

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

> **As tabelas são criadas automaticamente** pelo EF Core (`EnsureCreated`) na primeira execução da aplicação.

---

## 🔌 Connection String

Edite `appsettings.json` se necessário:

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=liontto;User=root;Password=;CharSet=utf8mb4;"
  }
}
```

- **Password vazio** = configuração padrão do XAMPP (sem senha para root)
- Se você definiu uma senha para o root do MySQL, coloque-a no campo `Password=`
- Se o MySQL estiver em outra porta, altere `Port=3306`

---

## ▶️ Executar a aplicação

```bash
# Na raiz do projeto (onde está o .csproj)
dotnet restore
dotnet run
```

Acesse: **http://localhost:5000**

Na primeira execução, o sistema:
1. Cria automaticamente todas as tabelas no MySQL
2. Insere dados de exemplo (materiais, produtos, clientes)

---

## 🗂️ Estrutura do Projeto

```
LionttoMoveis/
├── Data/
│   └── AppDbContext.cs         # DbContext EF Core (substitui IMongoDatabase)
├── Models/
│   ├── EntidadeBase.cs         # Classe base com Id (int) e CriadoEm
│   ├── Material.cs             # Tabela: materiais
│   ├── Cliente.cs              # Tabela: clientes
│   ├── Produto.cs              # Tabelas: produtos + materiais_do_produto
│   ├── Pedido.cs               # Tabelas: pedidos + itens_do_pedido
│   └── Movimentacao.cs         # Tabela: movimentacoes
├── Repository/
│   ├── IRepository.cs          # Contrato CRUD genérico
│   ├── MySqlRepository.cs      # Implementação base com EF Core
│   └── Repositorios.cs         # Repositórios específicos por entidade
├── Services/
│   ├── EstoqueService.cs       # Regras de negócio de estoque
│   └── SeedService.cs          # Dados de exemplo na primeira execução
├── Controllers/                # Controllers MVC
├── Views/                      # Views Razor (.cshtml)
├── appsettings.json            # Connection string MySQL
└── Program.cs                  # Startup: EF Core + DI + Seed
```

---

## 🔄 Diferenças MongoDB → MySQL

| Aspecto | MongoDB (original) | MySQL (esta versão) |
|---|---|---|
| **ID** | `string` (ObjectId 24 chars) | `int` (AUTO_INCREMENT) |
| **Driver** | `MongoDB.Driver` | `Pomelo.EntityFrameworkCore.MySql` |
| **ORM** | Nenhum (driver direto) | Entity Framework Core 8 |
| **Schema** | Sem schema (NoSQL) | Tabelas com FK e índices |
| **Embedded docs** | `MaterialDoProduto` dentro de `Produto` | Tabela separada `materiais_do_produto` |
| **Itens do pedido** | Embedded em `Pedido` | Tabela separada `itens_do_pedido` |
| **Contexto** | `IMongoDatabase` | `AppDbContext : DbContext` |
| **Repositório base** | `MongoRepository<T>` | `MySqlRepository<T>` |
| **Conexão** | `appsettings.json` → `MongoDB.ConnectionString` | `appsettings.json` → `ConnectionStrings.MySQL` |

---

## 🗄️ Tabelas criadas no MySQL

```sql
materiais           -- Material (nome, unidade, quantidade, preco_unitario...)
clientes            -- Cliente (nome, telefone, email, endereco)
produtos            -- Produto (nome, descricao, preco_base, tempo_producao_dias)
materiais_do_produto-- Relação N:N Produto ↔ Material (quantidade_necessaria)
pedidos             -- Pedido (cliente_id, status, valor_total, datas...)
itens_do_pedido     -- Itens de cada pedido (produto_id, quantidade, preco_unitario)
movimentacoes       -- Histórico de entrada/saída de materiais
```

---

## 🧪 Migrations (opcional, para produção)

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

## 💡 Conceitos POO mantidos

- **Herança**: `EntidadeBase` → `Material`, `Cliente`, `Produto`, `Pedido`, `Movimentacao`
- **Encapsulamento**: `StatusEstoque`, `RecalcularTotal()`, `AvancarStatus()` no model
- **Polimorfismo**: `Descricao()` sobrescrito em cada entidade
- **Abstração**: `IRepository<T>` — controllers não conhecem EF Core diretamente
- **Injeção de Dependência**: repositórios e services injetados pelo ASP.NET Core
