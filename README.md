# DataNath.ApiMetadatos - API GraphQL de Metadatos de Bases de Datos

Proyecto .NET 8 que expone un endpoint GraphQL para consultar metadatos de múltiples tipos de bases de datos. Se conecta a Azure Cosmos DB para almacenar conexiones y proporciona autenticación JWT, cifrado de contraseñas, y consultas de metadatos en tiempo real.

## Características

- GraphQL API con HotChocolate
- Conexión a Azure Cosmos DB (NoSQL API)
- Autenticación JWT
- Cifrado AES-256 de contraseñas con IV aleatorio
- CRUD completo de conexiones de bases de datos
- **Consulta de metadatos de bases de datos** (tablas, columnas, relaciones)
- **Soporte multi-base de datos**: SQL Server, MongoDB, Azure Cosmos DB, PostgreSQL
- Dos modos de consulta: usando conexiones guardadas o credenciales directas
- Filtro exacto por clientName (case-insensitive)
- Paginación con skip/take
- Generación automática de UUIDs
- Validación de duplicados en crear/actualizar conexiones
- Logging estructurado con ILogger
- Queries optimizadas de Cosmos DB (evita N+1)

## Tabla de Contenidos

- [Estructura del Proyecto](#estructura-del-proyecto)
- [Dependencias](#dependencias)
- [Configuración](#configuración)
- [Instalación y Ejecución](#instalación-y-ejecución)
- [Autenticación](#autenticación)
- [Queries de Conexiones](#queries-disponibles)
- [Mutations CRUD](#mutations-crear-actualizar-eliminar)
- [Consultas de Metadatos](#consultas-de-metadatos-de-bases-de-datos)
  - [Adaptadores Soportados](#adaptadores-soportados)
  - [Modo 1: Conexiones Guardadas](#modo-1-usando-conexiones-guardadas-en-cosmos-db)
  - [Modo 2: Credenciales Directas](#modo-2-pasando-credenciales-directamente)
  - [Ejemplos por Base de Datos](#ejemplos-por-tipo-de-base-de-datos)
- [Seguridad](#seguridad)
- [Modelos de Datos](#modelo-de-datos)
- [Optimizaciones de Rendimiento](#optimizaciones-de-rendimiento)
- [Solución de Problemas](#solución-de-problemas)
- [Características Implementadas](#características-implementadas)

## Estructura del Proyecto

```
datanath_apimetadatos/
├── Configuration/
│   ├── CosmosDbSettings.cs              # Configuración de Cosmos DB
│   ├── EncryptionSettings.cs            # Configuración de cifrado
│   └── JwtSettings.cs                   # Configuración de JWT
├── Models/
│   ├── Connection.cs                    # Modelo Connection
│   ├── User.cs                          # Modelo User
│   ├── DatabaseConnectionInfo.cs        # Info de conexión a bases de datos
│   ├── ColumnInfo.cs                    # Información de columnas
│   └── RelationInfo.cs                  # Información de relaciones (FK)
├── Repositories/
│   ├── IConnectionRepository.cs         # Interfaz del repositorio de conexiones
│   ├── ConnectionRepository.cs          # Implementación con CRUD y cifrado
│   ├── IUserRepository.cs               # Interfaz del repositorio de usuarios
│   └── UserRepository.cs                # Implementación para autenticación
├── Services/
│   ├── IEncryptionService.cs            # Interfaz del servicio de cifrado
│   ├── EncryptionService.cs             # Cifrado AES-256
│   ├── IJwtService.cs                   # Interfaz del servicio JWT
│   ├── JwtService.cs                    # Generación y validación de tokens
│   ├── IDatabaseMetadataService.cs      # Interfaz de metadatos
│   ├── DatabaseMetadataServiceFactory.cs # Factory para servicios de metadata
│   ├── SqlServerMetadataService.cs      # Servicio de metadata SQL Server
│   ├── MongoDbMetadataService.cs        # Servicio de metadata MongoDB
│   ├── CosmosDbMetadataService.cs       # Servicio de metadata Cosmos DB
│   └── PostgreSqlMetadataService.cs     # Servicio de metadata PostgreSQL
├── GraphQL/
│   ├── Query.cs                         # Queries de conexiones y metadatos
│   └── Mutation.cs                      # Mutations (Login y CRUD)
├── Program.cs                           # Configuración principal con DI
├── appsettings.json                     # Configuración de la aplicación
└── DataNath.ApiMetadatos.csproj         # Archivo del proyecto
```

## Dependencias

- .NET 8.0
- HotChocolate.AspNetCore 13.9.11
- HotChocolate.AspNetCore.Authorization 13.9.11
- Microsoft.Azure.Cosmos 3.41.0
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- System.IdentityModel.Tokens.Jwt 8.0.0
- Microsoft.Data.SqlClient 5.1.5
- MongoDB.Driver 2.24.0
- Npgsql 8.0.5

## Configuración

### 1. Configurar Cosmos DB

El archivo `appsettings.json` ya está configurado para Cosmos DB Emulator local:

```json
{
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseId": "requestdb",
    "ContainerId": "connections",
    "UsersContainerId": "users"
  },
  "Encryption": {
    "Key": "webdeveloptmentkfcteam"
  },
  "Jwt": {
    "Issuer": "DataNathAPI",
    "Audience": "DataNathClients",
    "SigningKey": "nats-web-application-development-environtment-developt-by-software-engineers",
    "ExpirationMinutes": 60
  }
}
```

Si usas Azure Cosmos DB en la nube, reemplaza con:
- `Endpoint`: Tu endpoint de Cosmos DB
- `Key`: Tu clave primaria de Cosmos DB
- `DatabaseId`: Nombre de tu base de datos
- `ContainerId`: Nombre del contenedor de conexiones
- `UsersContainerId`: Nombre del contenedor de usuarios

### 2. Configurar el Puerto del Servidor

El servidor está configurado para ejecutarse en el puerto **5223**. Si necesitas cambiarlo, edita la sección `Kestrel` en `appsettings.json`:

```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://*:5223"
    }
  }
}
```

Cambia `5223` por el puerto que desees usar.

### 3. Crear la Base de Datos y Contenedores

Asegúrate de tener:
- Una base de datos llamada `requestdb` (o el nombre que especifiques)
- Un contenedor llamado `connections` con `/id` como partition key
- Un contenedor llamado `users` con `/id` como partition key

### 4. Crear un Usuario de Prueba

Inserta este documento en el contenedor `users`:

```json
{
  "id": "1",
  "name": "admin",
  "password": "R0RleFpuZz09"
}
```

Nota: La contraseña está cifrada con AES-256. El texto plano es "password123".

## Instalación y Ejecución

### 1. Restaurar paquetes y compilar

```bash
dotnet restore
dotnet build
```

### 2. Ejecutar el proyecto

```bash
dotnet run
```

El servidor se iniciará en:
- HTTP: `http://localhost:5223`

## Autenticación

### Obtener un Token JWT

**IMPORTANTE**: Todas las operaciones (excepto login) requieren autenticación.

Primero debes hacer login para obtener un token:

```graphql
mutation {
  login(username: "admin", password: "password123") {
    success
    message
    token
  }
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "login": {
      "success": true,
      "message": "Login exitoso",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
  }
}
```

### Usar el Token en las Peticiones

#### Opción 1: Banana Cake Pop (Interfaz Gráfica)

1. Abre http://localhost:5223/graphql
2. Haz clic en "Headers" (abajo a la izquierda)
3. Agrega este header:
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

#### Opción 2: cURL

```bash
curl -X POST http://localhost:5223/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d '{"query":"{ getConnections { items { id clientName } } }"}'
```

#### Opción 3: Postman

1. En la pestaña "Authorization", selecciona "Bearer Token"
2. Pega el token obtenido del login

## Queries Disponibles

### 1. Obtener todas las conexiones (con paginación)

```graphql
query {
  getConnections(skip: 0, take: 10) {
    items {
      id
      clientName
      servidor
      puerto
      user
      password
      repository
      adapter
    }
    totalCount
    skip
    take
    pageCount
  }
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "getConnections": {
      "items": [
        {
          "id": "a7b3c456-1234-5678-90ab-cdef12345678",
          "clientName": "Maxpoint",
          "servidor": "192.168.101.42\\sqlexpress",
          "puerto": "1433",
          "user": "sa",
          "password": "xxxx",
          "repository": "MAXPOINT_K003",
          "adapter": "SqlServerSP"
        }
      ],
      "totalCount": 1,
      "skip": 0,
      "take": 10,
      "pageCount": 1
    }
  }
}
```

### 2. Filtrar conexiones por clientName (búsqueda exacta)

```graphql
query {
  getConnections(clientName: "MAXPOINT_LEGACY", skip: 0, take: 10) {
    items {
      id
      clientName
      repository
      adapter
    }
    totalCount
  }
}
```

**Nota:** Este filtro busca conexiones donde `clientName` sea exactamente "MAXPOINT_LEGACY" (no distingue mayúsculas/minúsculas). No traerá "MAXPOINT_LEGACY_1" u otros nombres similares.

### 3. Combinar filtro con paginación

```graphql
query {
  getConnections(clientName: "MAXPOINT_LEGACY", skip: 0, take: 5) {
    items {
      id
      clientName
      repository
      adapter
    }
    totalCount
    pageCount
  }
}
```

**Importante:** El filtro usa búsqueda **exacta** (Equals), case-insensitive. Si necesitas buscar por coincidencia parcial, omite el filtro y filtra los resultados en tu aplicación cliente.

### 4. Obtener una conexión por UUID

```graphql
query {
  getConnectionById(id: "a7b3c456-1234-5678-90ab-cdef12345678") {
    id
    clientName
    servidor
    puerto
    user
    password
    repository
    adapter
  }
}
```

## Mutations (Crear, Actualizar, Eliminar)

### 1. Crear una nueva conexión

El **ID se genera automáticamente** usando UUID. La **contraseña se cifra automáticamente** con AES-256.

**Validación de Duplicados:** El sistema valida que no exista una conexión duplicada antes de crearla. Se considera duplicada si ya existe una conexión con la misma combinación de:
- `clientName` + `servidor` + `repository` + `adapter`

Esto permite tener:
- ✅ Mismo cliente (clientName), diferentes bases de datos (repository)
- ✅ Mismo cliente (clientName), diferentes servidores
- ✅ Misma conexión física (servidor + repository), diferentes adaptadores
- ❌ Exactamente la misma combinación clientName + servidor + repository + adapter (se rechaza el duplicado)

```graphql
mutation {
  createConnection(input: {
    clientName: "Otro Cliente"
    servidor: "192.168.1.50\\sqlexpress"
    puerto: "1433"
    user: "admin"
    password: "mipassword"
    repository: "MYDB"
    adapter: "SqlServerSP"
  }) {
    id
    clientName
    servidor
    repository
    adapter
  }
}
```

**Respuesta exitosa (con UUID generado automáticamente):**
```json
{
  "data": {
    "createConnection": {
      "id": "b8c4d567-2345-6789-01bc-def123456789",
      "clientName": "Otro Cliente",
      "servidor": "192.168.1.50\\sqlexpress",
      "repository": "MYDB",
      "adapter": "SqlServerSP"
    }
  }
}
```

**Respuesta si ya existe (duplicado):**
```json
{
  "errors": [
    {
      "message": "Ya existe una conexión con clientName='Otro Cliente', servidor='192.168.1.50\\sqlexpress', repository='MYDB' y adapter='SqlServerSP'"
    }
  ],
  "data": null
}
```

**Nota:** La contraseña se devuelve descifrada en la respuesta, pero se almacena cifrada en Cosmos DB.

### 2. Actualizar una conexión existente

Usa el **UUID** que recibiste al crear la conexión.

**Validación de Duplicados:** Al igual que en el create, el update valida que la actualización no genere un duplicado. Se compara contra todas las conexiones existentes **excepto** el registro que se está actualizando.

```graphql
mutation {
  updateConnection(
    id: "b8c4d567-2345-6789-01bc-def123456789"
    input: {
      clientName: "Otro Cliente Actualizado"
      servidor: "192.168.1.50\\sqlexpress"
      puerto: "1433"
      user: "admin"
      password: "newpassword"
      repository: "MYDB"
      adapter: "SqlServerSP"
    }
  ) {
    id
    clientName
    repository
    adapter
  }
}
```

**Respuesta exitosa:**
```json
{
  "data": {
    "updateConnection": {
      "id": "b8c4d567-2345-6789-01bc-def123456789",
      "clientName": "Otro Cliente Actualizado",
      "repository": "MYDB",
      "adapter": "SqlServerSP"
    }
  }
}
```

**Si el ID no existe:**
```json
{
  "data": {
    "updateConnection": null
  }
}
```

**Si la actualización crea un duplicado:**
```json
{
  "errors": [
    {
      "message": "Ya existe una conexión con clientName='Otro Cliente', servidor='192.168.1.50\\sqlexpress', repository='MYDB' y adapter='SqlServerSP'"
    }
  ],
  "data": null
}
```

### 3. Eliminar una conexión

Usa el **UUID** de la conexión que quieres eliminar:

```graphql
mutation {
  deleteConnection(id: "b8c4d567-2345-6789-01bc-def123456789")
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "deleteConnection": true
  }
}
```

**Si el ID no existe:**
```json
{
  "data": {
    "deleteConnection": false
  }
}
```

## Parámetros de Paginación

- `skip`: Número de registros a saltar (default: 0)
- `take`: Número de registros a retornar (default: 10)

**Ejemplo de paginación:**
- Página 1: `skip: 0, take: 10` (registros 1-10)
- Página 2: `skip: 10, take: 10` (registros 11-20)
- Página 3: `skip: 20, take: 10` (registros 21-30)

El campo `pageCount` en la respuesta te indica el número total de páginas.

---

## Consultas de Metadatos de Bases de Datos

La API proporciona dos modos para consultar metadatos de bases de datos:

1. **Modo con conexiones guardadas**: Usa el `clientName` para buscar la conexión en Cosmos DB
2. **Modo directo**: Pasa las credenciales directamente en la query

### Adaptadores Soportados

El campo `adapter` acepta los siguientes valores:

| Adaptador | Alias Aceptados | Base de Datos |
|-----------|----------------|---------------|
| SQL Server | `sqlserver`, `sqlserversp`, `sql` | Microsoft SQL Server |
| MongoDB | `mongodb`, `mongo` | MongoDB |
| Cosmos DB | `cosmosdb`, `cosmos` | Azure Cosmos DB |
| PostgreSQL | `postgresql`, `postgres`, `pgsql` | PostgreSQL |

---

### Modo 1: Usando Conexiones Guardadas en Cosmos DB

#### 1. Obtener Tablas/Colecciones

```graphql
query {
  getTables(
    clientName: "ClienteDemo"
    repository: "MAXPOINT_K043"
    adapter: "SqlServer"
  )
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "getTables": [
      "Usuarios",
      "Productos",
      "Pedidos",
      "Cabecera_Factura",
      "Detalle_Factura"
    ]
  }
}
```

**Parámetros:**
- `clientName` (requerido): Nombre del cliente según está guardado en Cosmos DB
- `repository` (opcional): Nombre de la base de datos/repositorio
- `adapter` (opcional): Tipo de adaptador a usar. **Importante:** Si tienes la misma conexión con diferentes adaptadores (ej: "SqlServer" y "SqlServerSP"), debes especificar cuál usar

---

#### 2. Obtener Columnas de una Tabla

```graphql
query {
  getTableColumns(
    clientName: "ClienteDemo"
    tableName: "Cabecera_Factura"
    repository: "MAXPOINT_K043"
    adapter: "SqlServer"
  ) {
    columnName
    dataType
    maxLength
    isNullable
    isPrimaryKey
    defaultValue
  }
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "getTableColumns": [
      {
        "columnName": "id",
        "dataType": "int",
        "maxLength": null,
        "isNullable": false,
        "isPrimaryKey": true,
        "defaultValue": null
      },
      {
        "columnName": "numero_factura",
        "dataType": "varchar",
        "maxLength": 50,
        "isNullable": false,
        "isPrimaryKey": false,
        "defaultValue": null
      },
      {
        "columnName": "fecha",
        "dataType": "datetime",
        "maxLength": null,
        "isNullable": false,
        "isPrimaryKey": false,
        "defaultValue": "getdate()"
      },
      {
        "columnName": "cliente_id",
        "dataType": "int",
        "maxLength": null,
        "isNullable": true,
        "isPrimaryKey": false,
        "defaultValue": null
      }
    ]
  }
}
```

**Campos del modelo ColumnInfo:**
- `columnName`: Nombre de la columna/campo
- `dataType`: Tipo de dato (int, varchar, datetime, etc.)
- `maxLength`: Longitud máxima (para tipos de texto)
- `isNullable`: Si acepta valores NULL
- `isPrimaryKey`: Si es clave primaria
- `defaultValue`: Valor por defecto

---

#### 3. Obtener Relaciones (Foreign Keys)

```graphql
query {
  getTableRelations(
    clientName: "ClienteDemo"
    tableName: "Cabecera_Factura"
    repository: "MAXPOINT_K043"
    adapter: "SqlServer"
  ) {
    relationName
    fromTable
    fromColumn
    toTable
    toColumn
    relationType
  }
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "getTableRelations": [
      {
        "relationName": "FK_Factura_Cliente",
        "fromTable": "Cabecera_Factura",
        "fromColumn": "cliente_id",
        "toTable": "Usuarios",
        "toColumn": "id",
        "relationType": "ForeignKey"
      },
      {
        "relationName": "FK_Detalle_Factura",
        "fromTable": "Detalle_Factura",
        "fromColumn": "factura_id",
        "toTable": "Cabecera_Factura",
        "toColumn": "id",
        "relationType": "ReferencedBy"
      }
    ]
  }
}
```

**Campos del modelo RelationInfo:**
- `relationName`: Nombre de la constraint/relación
- `fromTable`: Tabla origen (la que tiene la FK)
- `fromColumn`: Columna en la tabla origen
- `toTable`: Tabla referenciada (a donde apunta la FK)
- `toColumn`: Columna en la tabla referenciada
- `relationType`: Tipo de relación
  - `"ForeignKey"`: Esta tabla tiene una FK hacia otra tabla
  - `"ReferencedBy"`: Otra tabla tiene una FK hacia esta tabla

---

### Caso de Uso: Múltiples Adaptadores para la Misma Conexión

Puedes tener la misma conexión física registrada con diferentes adaptadores. Esto es útil cuando necesitas diferentes formas de acceder a la base de datos:

**Ejemplo: SQL Server con dos adaptadores**

```json
// Registro 1: Para queries directas
{
  "clientName": "Otro Cliente",  // MISMO
  "servidor": "192.168.1.50\\sqlexpress",  // MISMO
  "repository": "MYDB",  // MISMO
  "adapter": "SqlServer"  // DIFERENTE - Ejecuta SELECT, INSERT, UPDATE, etc.
}

// Registro 2: Solo stored procedures
{
  "clientName": "Otro Cliente",  // MISMO
  "servidor": "192.168.1.50\\sqlexpress",  // MISMO
  "repository": "MYDB",  // MISMO
  "adapter": "SqlServerSP"  // DIFERENTE - Solo ejecuta SPs
}
```

**Clave única:** `clientName + servidor + repository + adapter`

**Para consultar, especifica el adapter:**

```graphql
# Usar el adapter de queries directas
query {
  getTables(
    clientName: "Otro Cliente"
    repository: "MYDB"
    adapter: "SqlServer"
  )
}

# Usar el adapter de stored procedures
query {
  getTables(
    clientName: "Otro Cliente"
    repository: "MYDB"
    adapter: "SqlServerSP"
  )
}
```

**Si no especificas `adapter`:**
- Se tomará la primera conexión que encuentre
- Si tienes múltiples adaptadores, no hay garantía de cuál se usará

---

### Modo 2: Pasando Credenciales Directamente

Si no quieres guardar la conexión en Cosmos DB o necesitas hacer una consulta temporal, puedes pasar las credenciales directamente.

#### 1. Obtener Tablas (Conexión Directa)

```graphql
query {
  getTablesFromConnection(connection: {
    servidor: "192.168.1.100\\SQLEXPRESS"
    puerto: "1433"
    user: "sa"
    password: "MiPassword123"
    repository: "MAXPOINT_K043"
    adapter: "sqlserver"
  })
}
```

**Respuesta:** Lista de tablas/colecciones

---

#### 2. Obtener Columnas (Conexión Directa)

```graphql
query {
  getTableColumnsFromConnection(
    connection: {
      servidor: "192.168.1.100\\SQLEXPRESS"
      puerto: "1433"
      user: "sa"
      password: "MiPassword123"
      repository: "MAXPOINT_K043"
      adapter: "sqlserver"
    }
    tableName: "Cabecera_Factura"
  ) {
    columnName
    dataType
    maxLength
    isNullable
    isPrimaryKey
    defaultValue
  }
}
```

---

#### 3. Obtener Relaciones (Conexión Directa)

```graphql
query {
  getTableRelationsFromConnection(
    connection: {
      servidor: "192.168.1.100\\SQLEXPRESS"
      puerto: "1433"
      user: "sa"
      password: "MiPassword123"
      repository: "MAXPOINT_K043"
      adapter: "sqlserver"
    }
    tableName: "Cabecera_Factura"
  ) {
    relationName
    fromTable
    fromColumn
    toTable
    toColumn
    relationType
  }
}
```

---

### Ejemplos por Tipo de Base de Datos

#### SQL Server

```graphql
query {
  getTablesFromConnection(connection: {
    servidor: "localhost\\SQLEXPRESS"
    puerto: "1433"
    user: "sa"
    password: "password123"
    repository: "MiBaseDatos"
    adapter: "sqlserver"
  })
}
```

#### MongoDB

```graphql
query {
  getTablesFromConnection(connection: {
    servidor: "localhost"
    puerto: "27017"
    user: "admin"
    password: "password123"
    repository: "midb"
    adapter: "mongodb"
  })
}
```

#### PostgreSQL

```graphql
query {
  getTablesFromConnection(connection: {
    servidor: "localhost"
    puerto: "5432"
    user: "postgres"
    password: "password123"
    repository: "midb"
    adapter: "postgresql"
  })
}
```

#### Azure Cosmos DB

```graphql
query {
  getTablesFromConnection(connection: {
    servidor: "https://mi-cuenta.documents.azure.com:443/"
    puerto: ""
    user: ""
    password: "tu-primary-key-aqui"
    repository: "mi-database"
    adapter: "cosmosdb"
  })
}
```

---

## Seguridad

### Cifrado de Contraseñas

- Las contraseñas se cifran automáticamente con **AES-256** antes de guardarse en Cosmos DB
- **IV Aleatorio**: Cada encriptación genera un IV (Initialization Vector) único y aleatorio para máxima seguridad
- El IV se almacena junto al texto cifrado (primeros 16 bytes del Base64)
- La clave de cifrado se configura en `appsettings.json` → `Encryption.Key`
- Las contraseñas se descifran automáticamente al leer desde la base de datos
- Compatible con contraseñas antiguas (formato con IV estático) y texto plano (backward compatibility)

### Autenticación JWT

- Todas las queries y mutations (excepto login) requieren un token JWT válido
- Los tokens expiran después de 60 minutos (configurable en `appsettings.json`)
- Los usuarios se almacenan en el contenedor `users` de Cosmos DB
- Las contraseñas de usuarios también están cifradas con AES-256

### Headers Requeridos

Para operaciones autenticadas, incluye este header:

```
Authorization: Bearer <tu-token-jwt>
```

**Sin token:**
```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

## Modelo de Datos

### Connection

```json
{
  "id": "a7b3c456-1234-5678-90ab-cdef12345678",
  "clientName": "Maxpoint",
  "servidor": "192.168.101.42\\sqlexpress",
  "puerto": "1433",
  "user": "sa",
  "password": "R0RleFpuZz09",
  "repository": "MAXPOINT_K003",
  "adapter": "SqlServerSP"
}
```

**Campos:**
- `id`: Identificador único (UUID generado automáticamente)
- `clientName`: Nombre del cliente (usado en queries de metadatos)
- `servidor`: Servidor de base de datos
- `puerto`: Puerto de conexión
- `user`: Usuario de la base de datos
- `password`: Contraseña cifrada con AES-256
- `repository`: Nombre de la base de datos
- `adapter`: Tipo de adaptador (SqlServer, MongoDB, CosmosDB, PostgreSQL)

### User

```json
{
  "id": "1",
  "name": "admin",
  "password": "R0RleFpuZz09"
}
```

Nota: El campo `password` está cifrado con AES-256.

### Validación de Duplicados

El sistema valida que no se creen conexiones duplicadas usando la siguiente clave única:
- `clientName` + `servidor` + `repository` + `adapter`

**Comportamiento:**
- Al crear una nueva conexión, se verifica que no exista otra con la misma combinación
- Al actualizar una conexión, se valida que los nuevos datos no choquen con otra conexión existente
- El registro que se está actualizando queda excluido de la validación
- Si se intenta crear/actualizar con datos duplicados, se lanza un error con mensaje descriptivo

**Ejemplo de error de duplicado:**
```json
{
  "errors": [{
    "message": "Ya existe una conexión con clientName='Maxpoint', servidor='192.168.101.42', repository='MAXPOINT_K003' y adapter='SqlServer'"
  }]
}
```

## Optimizaciones de Rendimiento

El proyecto implementa varias optimizaciones para garantizar buen rendimiento:

### 1. Queries Optimizadas de Cosmos DB

**Problema resuelto:** Evitar N+1 queries y carga innecesaria de datos

**Solución implementada:**
- `ExistsConnectionAsync`: Usa `COUNT(1)` en lugar de cargar todos los registros para validar duplicados
- `GetConnectionByClientAndRepositoryAsync`: Query parametrizado que retorna solo el registro necesario
- Queries con parámetros para prevenir inyección SQL y mejorar plan de ejecución

**Antes (ineficiente):**
```csharp
var allConnections = await GetAllConnectionsAsync(); // Carga TODA la tabla
var isDuplicate = allConnections.Any(c => ...);     // Filtra en memoria
```

**Después (optimizado):**
```csharp
var exists = await ExistsConnectionAsync(           // Query COUNT optimizado
    clientName, servidor, repository, adapter);
```

### 2. Logging Estructurado

Implementado con `ILogger<T>` en todos los repositorios:
- Logs de información para operaciones exitosas
- Logs de advertencia para casos no encontrados o validaciones fallidas
- Logs de error con stack traces para excepciones
- Parámetros estructurados para mejor análisis y filtrado

**Ejemplo de logs:**
```
[Information] Creando nueva conexión para cliente: Maxpoint
[Warning] Intento de crear conexión duplicada: Maxpoint/192.168.101.42/MAXPOINT_K003/SqlServer
[Information] Conexión creada exitosamente con ID: a7b3c456-1234-5678-90ab-cdef12345678
```

### 3. Refactorización de Código Duplicado

**Query.cs - Método helper reutilizable:**
```csharp
private async Task<DatabaseConnectionInfo> GetDatabaseConnectionInfoAsync(
    string clientName, IConnectionRepository connectionRepo,
    string? repository = null, string? adapter = null)
```

Este método es usado por:
- `getTables`
- `getTableColumns`
- `getTableRelations`

Reduce duplicación de ~30 líneas x 3 métodos = ~90 líneas eliminadas

### 4. Filtrado Exacto

**Cambio:** Filtro de `clientName` usa `Equals` en lugar de `Contains`
- Previene resultados parciales inesperados
- Mejor rendimiento en queries grandes
- Case-insensitive para flexibilidad

**Antes:** `clientName: "Maxpoint"` retornaba: MAXPOINT, MAXPOINT_LEGACY, MAXPOINT_LEGACY_1
**Ahora:** `clientName: "Maxpoint"` retorna solo: Maxpoint

### Recomendaciones Futuras

Para mejorar aún más el rendimiento:

1. **Índices Compuestos en Cosmos DB**
   ```json
   {
     "compositeIndexes": [
       [
         {"path": "/clientName", "order": "ascending"},
         {"path": "/servidor", "order": "ascending"},
         {"path": "/repository", "order": "ascending"},
         {"path": "/adapter", "order": "ascending"}
       ]
     ]
   }
   ```

2. **Caching con MemoryCache o Redis** para conexiones frecuentemente usadas

3. **DataLoader pattern** para batch queries en GraphQL

## Explorar el Schema de GraphQL

En Banana Cake Pop, haz clic en el ícono de documentación (📖) para ver:
- Todos los tipos disponibles
- Campos de cada tipo
- Argumentos de cada query/mutation
- Descripción completa del schema

## Solución de Problemas

### Error de autenticación

Si obtienes "The current user is not authorized":
- Verifica que incluiste el header `Authorization: Bearer <token>`
- Verifica que el token no haya expirado (60 minutos)
- Verifica que el usuario existe en el contenedor `users`

### Error de conexión a Cosmos DB

Verifica que:
- Cosmos DB Emulator esté ejecutándose (si usas emulator local)
- El endpoint y la key sean correctos
- La base de datos `requestdb` y los contenedores `connections` y `users` existan
- Los contenedores tengan `/id` como partition key

### Puerto en uso

Si el puerto 5223 está en uso, puedes cambiarlo editando el `appsettings.json`:

```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://*:5200"
    }
  }
}
```

### Ver logs detallados

Edita `appsettings.json` y cambia el nivel de log:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Características Implementadas

### Gestión de Conexiones
- ✅ CRUD completo de conexiones (create, read, update, delete)
- ✅ Almacenamiento de conexiones en Azure Cosmos DB
- ✅ Generación automática de UUIDs
- ✅ Filtro por clientName (búsqueda exacta)
- ✅ Paginación con skip/take
- ✅ Cifrado AES-256 de contraseñas
- ✅ Validación de duplicados (clientName + servidor + repository + adapter)

### Seguridad
- ✅ Autenticación JWT
- ✅ Autorización en todas las queries y mutations
- ✅ Login mutation
- ✅ Validación de usuarios desde Cosmos DB
- ✅ Tokens con expiración configurable

### Consultas de Metadatos
- ✅ Obtener lista de tablas/colecciones
- ✅ Obtener columnas/campos de tablas
- ✅ Obtener relaciones (foreign keys)
- ✅ Soporte para SQL Server
- ✅ Soporte para MongoDB
- ✅ Soporte para Azure Cosmos DB
- ✅ Soporte para PostgreSQL
- ✅ Dos modos de consulta: con conexiones guardadas o credenciales directas
- ✅ Factory pattern para servicios de metadata
- ✅ Desencriptación automática de contraseñas guardadas

### API GraphQL
- ✅ Interfaz Banana Cake Pop integrada
- ✅ Schema documentation automática
- ✅ Queries con filtros y paginación
- ✅ Mutations con validación
