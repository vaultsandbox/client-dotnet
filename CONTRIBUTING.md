# Contributing to VaultSandbox Client - .NET

First off, thank you for considering contributing! This project and its community appreciate your time and effort.

Please take a moment to review this document in order to make the contribution process easy and effective for everyone involved.

## Code of Conduct

This project and everyone participating in it is governed by the [Code of Conduct](./CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to hello@vaultsandbox.com.

## How You Can Contribute

There are many ways to contribute, from writing tutorials or blog posts, improving the documentation, submitting bug reports and feature requests or writing code which can be incorporated into the main project.

### Reporting Bugs

If you find a bug, please ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/vaultsandbox/client-dotnet/issues). If you're unable to find an open issue addressing the problem, [open a new one](https://github.com/vaultsandbox/client-dotnet/issues/new). Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample** or an **executable test case** demonstrating the expected behavior that is not occurring.

### Suggesting Enhancements

If you have an idea for an enhancement, please open an issue with a clear title and description. Describe the enhancement, its potential benefits, and any implementation ideas you might have.

### Pull Requests

We love pull requests. Here's a quick guide:

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix: `git checkout -b feat/my-awesome-feature` or `git checkout -b fix/that-annoying-bug`.
3.  Make your changes, adhering to the coding style.
4.  Add or update tests for your changes.
5.  Ensure all tests pass (`dotnet test`).
6.  Ensure your code is formatted (`dotnet format`).
7.  Commit your changes with a descriptive commit message.
8.  Push your branch to your fork.
9.  Open a pull request to the `main` branch of the upstream repository.

## Development Setup

This project is a .NET library written in C#.

1.  Restore dependencies: `dotnet restore`
2.  Build the project: `dotnet build`
3.  Configuration: If you are running tests that require environment variables, you can create a `.env` file from the `.env.example`.

## Running Tests

- **Run all tests**:
  ```bash
  dotnet test
  ```
- **Run tests with detailed output**:
  ```bash
  dotnet test --logger "console;verbosity=detailed"
  ```
- **Generate a coverage report**:
  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  ```

## Coding Style

- **Formatting**: We use `dotnet format` for automated code formatting. Please run `dotnet format` before committing your changes.
- **Analyzers**: The project includes Roslyn analyzers for identifying code quality issues. Build warnings should be addressed before submitting a PR.
- **Comments**: For new features or complex logic, please add XML documentation comments to explain the _why_ behind your code.

Thank you for your contribution!
