# Contributing to Sarab

First off, thanks for taking the time to contribute.

The following is a set of guidelines for contributing to Sarab. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

## Getting Started

### Prerequisites

*   **.NET 10 SDK**: This project targets .NET 10. Ensure you have the preview/latest version installed.
*   **Git**: For version control.

### Setting Up the Development Environment

1.  **Fork the repository** on GitHub.
2.  **Clone your fork** locally:
    ```bash
    git clone https://github.com/your-username/sarab.git
    cd sarab
    ```
3.  **Restore dependencies**:
    ```bash
    dotnet restore
    ```

## Building and Running

### Build from Source

To build the project locally:

```bash
dotnet build
```

### Run the CLI

You can run the CLI directly using `dotnet run`:

```bash
# Example: Check version
dotnet run --project Sarab.Cli/Sarab.Cli.csproj -- --version

# Example: Expose a port
dotnet run --project Sarab.Cli/Sarab.Cli.csproj -- expose 8000
```

## Running Tests

(If tests are added in the future, include instructions here. e.g., `dotnet test`)

## Pull Request Process

1.  Ensure any install or build dependencies are removed before the end of the layer when doing a build.
2.  Update the README.md with details of changes to the interface, this includes new environment variables, exposed ports, useful file locations and container parameters.
3.  Increase the version numbers in any examples files and the README.md to the new version that this Pull Request would represent.
4.  You may merge the Pull Request in once you have the sign-off of other developers, or if you do not have permission to do that, you may request the second reviewer to merge it for you.

## Code Style

*   Use standard C# coding conventions.
*   Run `dotnet format` before committing to ensure consistent code style.
*   Keep code clean and commented where necessary.
