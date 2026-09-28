# Configuracao de producao

## Segredos obrigatorios

Nao grave valores secretos em `appsettings*.json`, no repositorio ou em logs. Configure no gerenciador de segredos do host:

| Chave | Variavel de ambiente | Requisito |
| --- | --- | --- |
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | Npgsql para Supabase, com host, database, username, password e SSL obrigatorio |
| `Admin:Username` | `Admin__Username` | Usuario administrativo |
| `Admin:Password` | `Admin__Password` | Senha longa, minimo de 16 caracteres |
| `Registration:BootstrapToken` | `Registration__BootstrapToken` | Segredo aleatorio com pelo menos 32 caracteres |
| `DataProtection:KeysPath` | `DataProtection__KeysPath` | Diretorio gravavel e persistente para chaves de cookies e recuperacao |
| `AllowedHosts` | `AllowedHosts` | Hosts publicos separados por ponto e virgula; curingas nao sao aceitos |

A inicializacao fora de Development falha se uma dessas configuracoes estiver ausente ou invalida. Restrinja o acesso ao diretorio de Data Protection ao usuario do servico. Em ambiente com replicas, use o mesmo diretorio/volume compartilhado.

Em desenvolvimento, o projeto tem `UserSecretsId`. Configure usuario, senha e banco sem editar arquivos:

```powershell
dotnet user-secrets set "Admin:Username" "admin" --project .\BlakBox.Api.csproj
dotnet user-secrets set "Admin:Password" "SUA-SENHA-FORTE" --project .\BlakBox.Api.csproj
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require" --project .\BlakBox.Api.csproj
```

O cliente da API e o **BlakBox.Torque**, instalado e executado localmente na oficina. Ele chama a API Central pela internet; nao existe um site OficinaWeb separado neste fluxo. A configuracao feita no Torque informa somente a URL da Central. Portanto, o Torque nao envia `X-Registration-Token` por essa configuracao.

**Incompatibilidade que precisa ser resolvida antes da liberacao:** em producao, a API exige `X-Registration-Token` no cadastro de uma instalacao nova, mas o Torque atualmente envia esse header somente se `Licenciamento:RegistrationToken` estiver configurado fora da tela do link. Com apenas o link, o primeiro cadastro de uma instalacao nova sera recusado. Instalacoes ja cadastradas continuam podendo sincronizar usando `X-Installation-Key`. Nao distribua o token global de bootstrap dentro do aplicativo local como correcao: ele seria compartilhado com todas as oficinas e poderia ser extraido. O fluxo de primeiro cadastro precisa de um mecanismo individual de provisionamento/autorizacao.

## Registro e rotacao de credenciais

O endpoint de registro exige `X-Registration-Token` para uma instalacao nova quando o bootstrap esta configurado, como ocorre em producao. Para sincronizar uma instalacao existente, o BlakBox.Torque envia `X-Installation-Key` com a credencial ativa. O token de bootstrap tambem autoriza recuperacao/rotacao administrativa. Resolva a incompatibilidade de onboarding acima antes de liberar novas instalacoes.

Proteja o token e limite quem pode usa-lo. O endpoint aplica limite por IP. Configure `ForwardedHeaders:KnownProxies` com os IPs reais do proxy confiavel; nao confie em `X-Forwarded-For` de outros remetentes.

Na rotacao, a substituta fica cifrada no banco por ate dez minutos, para permitir recuperacao entre replicas/reinicios. A chave de Data Protection deve permanecer persistente e compartilhada. Uma credencial revogada sem uma substituta valida nao pode emitir outra; nesse caso, sincronize com o token de bootstrap.

## TLS, proxy e cookies

Sirva o painel somente por HTTPS. Cookies administrativos sao `HttpOnly`, `SameSite=Strict`, `Secure` fora de Development e expiram em oito horas. Formularios Razor usam antiforgery. Quando TLS terminar em proxy, liste seus IPs em `ForwardedHeaders:KnownProxies`. Swagger fica habilitado somente em Development.

## Supabase e migrations

As migrations em `Migrations/` foram geradas originalmente pelo provider SQL Server e nao devem ser aplicadas ao Supabase com `dotnet ef database update`. A API nao executa migrations automaticamente. `Database/PostgreSQL/001_schema.sql` e o schema atual para banco novo e vazio. Em banco existente, faca backup e revise `Database/PostgreSQL/002_credential_recovery.sql` para adicionar as colunas de recuperacao. Compare as demais colunas e indices com `LicencaDbContext` antes de cada release.

## Datas

Datas de negocio usam horario de Sao Paulo, gravado sem fuso (`timestamp without time zone`), independente do fuso do host. O monitoramento da API permanece em UTC no mesmo tipo PostgreSQL. Nao converta dados existentes sem confirmar o fuso em que foram gravados.

## Checklist de liberacao

- Configure os segredos acima e valide inicializacao fora de Development sem expor connection strings.
- Compare/aplique o schema PostgreSQL em janela controlada e confirme indices.
- Atualize o BlakBox.Torque instalado nas oficinas com a URL HTTPS da API e confirme a sincronizacao de instalacoes provisionadas. Teste o primeiro cadastro somente depois de implementar o mecanismo individual de autorizacao.
- Confirme HTTPS, proxy confiavel, login/logout, validacao e rotacao de licenca.
- Tenha backup do Supabase, monitoramento de 429/5xx e teste de restauracao.
