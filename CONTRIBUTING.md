# Contributing to Akamai CCM Orchestrator

Thank you for your interest in contributing to the Akamai CCM Orchestrator!

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/bjfahey/akamai-ccm-orchestrator/issues)
2. If not, create a new issue using the bug report template
3. Include as much detail as possible:
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details (Keyfactor version, .NET version, OS)
   - Relevant log excerpts

### Suggesting Enhancements

1. Check if the enhancement has already been suggested
2. Create a new issue using the feature request template
3. Clearly describe the use case and expected benefit

### Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes:
   - Follow existing code style and patterns
   - Add tests for new functionality
   - Update documentation as needed
4. Commit your changes (`git commit -m 'Add some amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git

### Building

```bash
dotnet restore
dotnet build --configuration Release
```

### Testing

```bash
dotnet test
```

## Code Style

- Follow existing patterns in the codebase
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose

## Project Structure

- `akamai-ccm-orchestrator/` - Main orchestrator implementation
- `http-interface/` - Shared HTTP client with EdgeGrid auth
- `akamai-ccm-orchestrator.Tests/` - Unit tests

## Questions?

Feel free to open an issue with the question label or reach out to the maintainers.

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0.
