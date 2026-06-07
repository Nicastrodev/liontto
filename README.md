# 🪑 Líontto Móveis

Sistema web de gestão para marcenaria e móveis planejados desenvolvido com **ASP.NET Core MVC**, **Entity Framework Core** e **MySQL**.

> Projeto acadêmico desenvolvido durante o 3º semestre de Análise e Desenvolvimento de Sistemas, com foco em arquitetura de software, persistência de dados, controle de estoque e desenvolvimento full stack utilizando ASP.NET Core MVC e MySQL.

A aplicação permite o gerenciamento completo de materiais, produtos, clientes, pedidos e controle de estoque em um único sistema.

---

# 🚀 Tecnologias Utilizadas

## 🔹 Back-end

* C#
* .NET 8
* ASP.NET Core MVC
* Entity Framework Core
* Pomelo.EntityFrameworkCore.MySql

## 🔹 Front-end

* Razor Views
* HTML5
* CSS3
* JavaScript

## 🔹 Banco de Dados

* MySQL
* XAMPP

## 🔹 Deploy

* Railway
* Docker

---

# 📌 Sobre o Projeto

O projeto foi originalmente desenvolvido utilizando **MongoDB** e posteriormente migrado para **MySQL**, adotando uma arquitetura relacional mais organizada e escalável.

A aplicação utiliza:

* Entity Framework Core
* Repository Pattern
* Service Layer
* Dependency Injection
* MVC Architecture

Frontend e backend funcionam no mesmo serviço utilizando **ASP.NET Core MVC + Razor Views**.

---

# 🧠 Funcionalidades

✅ Gestão de materiais
✅ Controle de estoque
✅ Cadastro de clientes
✅ Gestão de produtos
✅ Controle de pedidos
✅ Movimentações de entrada e saída
✅ Dashboard administrativo
✅ Persistência em MySQL
✅ Seed automático de dados

---

# 🗂️ Estrutura do Projeto

```bash id="y0ql38"
LionttoMoveis/
│
├── Controllers/
├── Data/
├── Models/
├── Repository/
├── Services/
├── Views/
├── wwwroot/
├── appsettings.json
└── Program.cs
```

---

# 🛢️ Banco de Dados

O sistema utiliza MySQL com Entity Framework Core.

### Principais tabelas

* materiais
* clientes
* produtos
* pedidos
* itens_do_pedido
* materiais_do_produto
* movimentacoes

---

# 🔄 Migração MongoDB → MySQL

A aplicação foi migrada de MongoDB para MySQL visando:

* Melhor organização relacional
* Integridade de dados
* Estrutura escalável
* Melhor controle de consultas
* Relacionamentos SQL

| MongoDB      | MySQL               |
| ------------ | ------------------- |
| ObjectId     | int AUTO_INCREMENT  |
| Sem schema   | Schema estruturado  |
| Documentos   | Tabelas relacionais |
| Mongo Driver | EF Core + Pomelo    |

---

# 🧩 Conceitos Aplicados

* Programação Orientada a Objetos
* Repository Pattern
* Dependency Injection
* Service Layer
* MVC Pattern
* Entity Framework Core

---

# 🚂 Deploy

Aplicação preparada para deploy utilizando:

* Railway
* Docker
* ASP.NET Core
* MySQL

---

# 📸 Screenshots

<img width="1900" height="942" alt="image" src="https://github.com/user-attachments/assets/417d9973-5fa3-484b-95e8-7cc6246c14ab" />

---

# 👨‍💻 Autor

## Matheus Nicastro

Desenvolvedor Full Stack focado em:

* C#
* .NET
* React
* SQL
* Sistemas Web

---

# 📄 Licença

Projeto desenvolvido para fins acadêmicos, portfólio e desenvolvimento profissional.
