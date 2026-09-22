# Marten + PostgreSQL

Marten is the PostgreSQL document/event store behind AuthKit persistence: the keystore, token bindings, plugin documents, and the message outbox. One Postgres, many roles configured in one place.

## What Lives There

- **Keystore** the encrypted singleton holding signing key material (see [ADR-011](/adr/011-keystore-persisted-as-singleton-marten-document/))
- **Token bindings** developer token key bindings (see [ADR-012](/adr/012-token-key-bindings-persisted-in-marten/))
- **Plugin documents** every plugin implementing `IMartenPlugin` brings its own documents
- **Outbox** the Wolverine message store shared with handlers (`IntegrateWithWolverine`).

All of it is plain JSONB documents in one cluster plain Postgres client is enough for inspection, no Marten tooling required.

## Connection and Schema

```json
{
  "ConnectionStrings": {
    "Marten": "Host=authdev-db;Port=5432;Database=AuthDev;Username=postgres;Password=postgres"
  }
}
```

Schema objects auto-create on startup (`AutoCreate.All`) in dev you never write migrations by hand.

:::warning[AutoCreate in production]
Automatic schema creation is a developer convenience. For production, generate migration scripts ahead of time and apply them deliberately instead of letting the app modify the schema on startup.
:::

## Plugin Documents

A plugin contributes documents with a single hook:

```csharp
public interface IMartenPlugin
{
    void ConfigureMarten(StoreOptions options);
}
```

The rules are strict and few:

- the hook runs **once**, while the host builds its single document store, before the store is opened
- plugins are processed in ascending plugin-ID order ordering is deterministic and independent of discovery order
- the hook configures **only** `StoreOptions` it must not reach for `IDocumentStore` or `IDocumentSession`, which exist only after the provider is built
- the plugin's `ConfigureServices` services are already registered when the hook runs you may assume their presence, but not sessions.

## Sessions and Outbox

Marten works on lightweight sessions (`UseLightweightSessions`) integrated with Wolverine: a handler writes documents and publishes messages in the same unit of work. If the handler fails, both the write and the send roll back there are no "saved but not sent" states.

It works the other way too: a message in the outbox without a matching write doesn't exist. Consistency between "what the system remembered" and "what the system sent" is a transactional property, not a gentlemen's agreement.

## Operations

- **Backup** back up the `AuthDev` database (keys + tokens + outbox are one consistent set) losing the keystore invalidates every issued token.
- **Fresh start** `docker compose down -v` wipes everything including the schema the next start rebuilds from zero.
- **Inspection** plain Postgres client is enough: documents are JSONB, readable without Marten tooling.

## Diagnostics

<Expansion title="No connection">

Check the `Marten` connection string and that `authdev-db` is `healthy` (`docker compose ps`) the host only starts after healthy database.

</Expansion>

<Expansion title="Schema error on startup">

Someone changed document without migrating in dev drop the volume (`down -v`), in production apply the generated script.

</Expansion>

<Expansion title="Suspected session leak">

Sessions are lightweight and short lived long transaction usually means handler doing too much split it into messages.

</Expansion>

<Expansion title="Messages lost on restart">

In `MediatorOnly` mode (`SkipStorageMigrationOnStartup: true`) there is no message storage everything in flight dies on restart. If you lose calls locally, check that flag before debugging handlers.

</Expansion>

<Expansion title="Tokens fail after database restore">

A fresh volume means a new keystore previously issued tokens have nothing to verify against. That's key rotation semantics, not a bug: reissue tokens or restore from backup.

</Expansion>

<Expansion title="Slow document queries">

Documents are JSONB filtering unindexed fields hurts as data grows. Add indexes in the same `ConfigureMarten` where you define documents measure on real data, not an empty dev database.

</Expansion>

See [ADR-016](/adr/016-marten-and-wolverine-infrastructure/).
