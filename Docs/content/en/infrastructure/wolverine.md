# Wolverine

Wolverine handles commands and queries inside the host. It runs as an in-process mediator: instead of calling each other directly, code sends messages and Wolverine routes them to handlers with validation, durability, and logging applied uniformly around every execution.

## Mediator, Not Broker

There is no external transport or queue to configure: messages never leave the process. Wolverine is an in-memory call director with a uniform pipeline (middleware) around every handler. You get architectural order (commands/queries instead of tangled calls) without a broker's operational price.

## Discovery

Handlers are discovered, not registered. At startup the host includes exactly two sources:

- the **Core assembly** (anchored at `JwtKeyStore`) domain commands and queries
- **each loaded plugin assembly** plugins ship handlers with their own code, and the host never references plugin assemblies directly.

Consequence: adding handler means adding a class in the right assembly. There is no central registry file to update and forget.

## Writing Handlers

Command/query types and handlers land in Core (domain behavior) or in the plugin assembly (plugin behavior). Pair every command with a validator DevTokens keeps them in `UseCase/Commands/Validations/` next to `UseCase/Commands/Requests/`.

## Validation

FluentValidation runs as Wolverine middleware, so invalid messages fail before handlers execute. Validation is a pipeline property, never a manual call inside each handler it cannot be forgotten.

## Durability and Outbox

```json
{
  "AuthKit": {
    "SkipStorageMigrationOnStartup": false
  }
}
```

- Default keeps full durability: message storage is built on startup and Marten integrates as the outbox (`IntegrateWithWolverine`) handlers and persistence share the transactional outbox.
- With `SkipStorageMigrationOnStartup: true`, Wolverine drops to `MediatorOnly` mode (in-memory, no message storage) and skips storage migration. Use it for fast local iterations where losing in-flight messages on restart doesn't matter.

## Plugins Bring Handlers

This is the key property of the model: plugin logic doesn't end at endpoints. A plugin can define its own commands, queries, and handlers in its own assembly the host includes them in discovery just like Core. A plugin endpoint typically just translates HTTP/gRPC into a message and sends it to Wolverine all logic lives in handlers.

## Logging and Observability

The pipeline is silent by default: message execution/success levels and the `Wolverine`, `Marten`, and `Npgsql` categories are set to `None`.

:::tip[Debugging the pipeline]
Raise these categories temporarily when handler misbehaves silent pipeline hides both the message flow and the SQL behind it.
:::

## Configuration

| Option | Default | Meaning |
|--------|---------|---------|
| `AuthKit:SkipStorageMigrationOnStartup` | `false` | `true` → `MediatorOnly` mode, no storage migration |

See [ADR-016](/adr/016-marten-and-wolverine-infrastructure/).
