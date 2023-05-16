# Projeto

Esse é um projeto que realiza um consulta em um repositório github trazendo informações sobre issues e contabilizando a quantidade de commits por usuário.
Essa consulta é realizada de tempos em tempos com um intervalo em horas informado no startup da aplicação.

# Stack de desenvolvimento

O projeto foi desenvolvido na linguagem F# (F Sharp) da plataforma Microsoft .NET, e segue nesse caso o paradigma funcional, já que a linguagem é multi-paradigma.

# Arquitetura

O projeto segue uma organização em camadas seguindo um padrão de arquitetura chamado Onion Architecture.
Referência: https://marcoatschaefer.medium.com/onion-architecture-explained-building-maintainable-software-54996ff8e464

# Requisitos de instalação

- SDK do .NET, versão 6 ou maior
  https://dotnet.microsoft.com/en-us/download

# Requisitos de execução

- É necessário a criação de um API Token no github: 
  https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token

- Setar variáveis de ambiente:
  "GITHUB_USERNAME" -> Usuário do Github.
  "GITHUB_REPO" -> Repositório para consulta.
  "GITHUB_API_TOKEN" -> API Token gerado no Github.
  "WEBHOOK_URL" -> Webhook de destino.
  "CHECKING_INTERVAL" - Intervalo em horas que a consulta será realizada.

# Para executar
  dotnet restore
  dotnet run




