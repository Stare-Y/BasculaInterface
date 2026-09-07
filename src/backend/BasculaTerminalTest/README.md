# BasculaTerminalTest

The backend test suite. Organised by folder, one project so `dotnet test` stays a single command.

| Folder          | What's in it                                                                                     | Needs                     |
| --------------- | ----------------------------------------------------------------------------------------------- | ------------------------- |
| `Unit/`         | Fast, isolated tests of business logic (`PedidoService`, `WeightService` gate, `WeightLogisticService`, `PasswordHasher`). NSubstitute for collaborators. | nothing                   |
| `Integration/`  | The real `BasculaTerminalApi` host in-memory (`WebApplicationFactory`) against a throwaway PostgreSQL container. Exercises HTTP + SignalR + EF end to end. | a Docker-compatible engine |
| `Live/`         | `BasculaClient` — smoke check against a *running* API with a scale attached. Never run automatically. | running API + hardware    |
| `TestDoubles/`  | Shared fakes (`FakeBasculaService`, the ContpaqiSQL repo fakes) and `TestData` builders.        | —                         |

## Running

```bash
# Everything that doesn't need hardware (unit + integration). This is what CI runs.
dotnet test --filter "Category!=Live"

# Unit tests only — no Docker required.
dotnet test --filter "Category!=Live&Category!=Integration"

# The live hardware check, on the terminal PC with the scale connected.
dotnet test --filter "Category=Live"
```

### Integration tests locally

They use [Testcontainers](https://dotnet.testcontainers.org/) to start `postgres:16-alpine`.

- **Docker Desktop / Docker Engine**: works out of the box.
- **Podman (rootless, e.g. Fedora)**:
  ```bash
  systemctl --user start podman.socket
  export DOCKER_HOST="unix://$XDG_RUNTIME_DIR/podman/podman.sock"
  dotnet test --filter "Category=Integration"
  ```
  Ryuk (the Testcontainers reaper) is disabled automatically for Podman compatibility; containers
  are still torn down by the test fixture.
