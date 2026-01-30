# Testing Guide for SatchSDKCore

This document provides comprehensive instructions for running tests, viewing coverage, and understanding the CI/CD pipeline for SatchSDKCore.

## Table of Contents
- [Quick Start](#quick-start)
- [Running Tests](#running-tests)
- [Code Coverage](#code-coverage)
- [CI/CD Pipeline](#cicd-pipeline)
- [Coverage Requirements](#coverage-requirements)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~Base32Tests"
```

## Running Tests

### Basic Test Execution

```bash
# Run all tests in the solution
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests in a specific project
dotnet test tests/SatchSDKCore.Tests/SatchSDKCore.Tests.csproj
```

### Filtering Tests

```bash
# Run tests by namespace
dotnet test --filter "FullyQualifiedName~Net.HttpEx"

# Run tests by class name
dotnet test --filter "ClassName=HttpServerExCoreTests"

# Run tests by method name
dotnet test --filter "Name~Should"
```

### Test Organization

Tests are organized by component:
```
tests/SatchSDKCore.Tests/
├── Misc/
│   ├── Base32Tests.cs
│   ├── FastTextEncodingTests.cs
│   ├── TimeTests.cs
│   └── Hookable/
│       └── HookableTests.cs
└── Net/
    ├── HttpEx/
    │   ├── HttpClientExPayloadTests.cs
    │   ├── HttpClientExRateLimitInfoTests.cs
    │   ├── HttpClientExResponseTests.cs
    │   ├── HttpServerExCoreTests.cs
    │   ├── HttpServerExRequestContextTests.cs
    │   └── HttpServerExResponseTests.cs
    └── JsonRpc/
        └── JsonRpcClientResultTests.cs
```

## Code Coverage

### Generating Coverage Reports

#### Option 1: Simple Coverage (XML)

```bash
# Generate coverage in Cobertura format
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Coverage files will be in: ./coverage/{guid}/coverage.cobertura.xml
```

#### Option 2: HTML Coverage Report (Recommended)

```bash
# Install ReportGenerator (one-time setup)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:Html

# Open the report in your browser
# Linux/Mac:
open ./coverage-report/index.html
# Windows:
start ./coverage-report/index.html
```

The HTML report includes:
- ✅ **Line-by-line coverage** with color coding (green = covered, red = not covered)
- ✅ **Branch coverage** details
- ✅ **Class and method-level** statistics
- ✅ **Risk hotspots** highlighting areas needing tests
- ✅ **Coverage history** and trends

#### Option 3: VS Code Tasks (Recommended for VS Code Users)

The project includes pre-configured VS Code tasks for easy test and coverage execution:

**Run Tasks:** Press `Ctrl+Shift+P` → "Tasks: Run Task" → Select a task

**Available Tasks:**
- **test** - Run all tests (default test task: `Ctrl+Shift+B` → Test)
- **test with coverage** - Run tests and generate coverage data
- **test with coverage (verbose)** - Same as above with detailed output
- **test with HTML coverage (auto-open)** ⭐ - Run tests, generate HTML report, and auto-open in VS Code
- **test with coverage and report** - Run tests and generate HTML report (no auto-open)
- **generate coverage report** - Generate HTML report from existing coverage data
- **generate and open coverage report** - Generate HTML report and open in VS Code
- **open coverage report** - Open existing HTML report in VS Code
- **open coverage report (VS Code)** - Open existing HTML report in VS Code
- **open coverage report (browser)** - Open existing HTML report in your default browser
- **clean coverage** - Remove coverage and report directories

**Quick Workflow (Recommended):**
1. Press `Ctrl+Shift+P`
2. Type "Tasks: Run Task"
3. Select **"test with HTML coverage (auto-open)"** ⭐
4. Wait for completion
5. HTML report opens automatically in VS Code

**Note:** By default, reports open in VS Code for convenient viewing without leaving your editor. If you prefer to open reports in your browser, use the "(browser)" variant of the task.

#### Option 4: VS Code Coverage Gutters Extension

1. Install the **Coverage Gutters** extension by ryanluker
2. Run tests with coverage (use VS Code task or command):
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
3. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
4. Select "Coverage Gutters: Watch"
5. See coverage indicators directly in your code:
   - 🟢 Green line numbers = covered
   - 🔴 Red line numbers = not covered

### Understanding Coverage Metrics

- **Line Coverage**: Percentage of code lines executed by tests
- **Branch Coverage**: Percentage of decision branches (if/else, switch) tested
- **Complexity**: Cyclomatic complexity score (lower is better)

**Example:**
```
Package      | Line Rate | Branch Rate | Complexity | Health
------------ | --------- | ----------- | ---------- | ------
SatchSDKCore | 24%       | 24%         | 1118       | ❌
```

## CI/CD Pipeline

### Workflow Overview

Our GitHub Actions workflow runs automatically on:
- **Pull Requests**: To any branch
- **Pushes**: To `stable` and `dev` branches only

### Pipeline Stages

#### Stage 1: Test & Coverage (All Events)
```yaml
Triggers: All PRs and pushes to stable/dev
Steps:
  1. Setup .NET 9.0
  2. Restore dependencies
  3. Build project
  4. Run tests with coverage
  5. Generate coverage report
  6. Post coverage comment (PRs only)
  7. Upload test results & coverage artifacts
```

**Coverage Requirements:**
- ⚠️ **Minimum**: 60% (workflow fails below this)
- ✅ **Target**: 80% (ideal coverage)

#### Stage 2: Build (stable/dev only)
```yaml
Triggers: Pushes to stable/dev (only if tests pass)
Depends on: test-and-coverage
Steps:
  1. Build Release configuration
  2. Create NuGet package with branch suffix
     - stable → SatchSDKCore.{version}-stable.nupkg
     - dev → SatchSDKCore.{version}-dev.nupkg
  3. Upload build artifacts
```

#### Stage 3: Publish (stable/dev only)
```yaml
Triggers: Pushes to stable/dev (only if build passes)
Depends on: build
Steps:
  1. Download NuGet packages
  2. Publish to GitHub Packages
```

### Viewing CI/CD Results

#### On Pull Requests
1. Go to your PR page
2. See **Checks** tab for workflow status
3. **Coverage report** automatically posted as a comment
4. Click "Details" next to check to see full workflow run

#### On GitHub Actions
1. Go to **Actions** tab in GitHub
2. Click on a workflow run
3. View **Summary** for coverage table
4. Download artifacts:
   - `test-results` - Test execution results
   - `coverage-reports` - Coverage XML files
   - `build-artifacts` - Compiled binaries (stable/dev only)
   - `nuget-packages` - NuGet packages (stable/dev only)

### Workflow Files
- `.github/workflows/dotnet.yml` - Main CI/CD workflow

## Coverage Requirements

### Minimum Thresholds

Our CI/CD enforces the following coverage requirements:

| Metric | Minimum | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 60% | 80% | ❌ Fails below 60% |
| Branch Coverage | 60% | 80% | ⚠️ Warning 60-80% |

### What Happens When Coverage is Low?

- **< 60% Coverage**:
  - ❌ CI/CD fails
  - ❌ PR cannot be merged
  - ❌ Build and publish stages don't run

- **60-80% Coverage**:
  - ⚠️ CI/CD passes with warning
  - ✅ PR can be merged
  - ✅ Build and publish run normally

- **> 80% Coverage**:
  - ✅ CI/CD passes
  - ✅ Ideal code quality

### Adjusting Thresholds

To modify coverage thresholds, edit `.github/workflows/dotnet.yml`:

```yaml
- name: Code Coverage Report
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
      thresholds: "60 80"  # Change these values
      #           ^  ^
      #           |  Target threshold
      #           Minimum threshold (fails below)
```

## Best Practices

### Writing Tests

1. **Follow AAA Pattern**:
   ```csharp
   [Fact]
   public void MethodName_Scenario_ExpectedBehavior()
   {
       // Arrange
       var sut = new SystemUnderTest();

       // Act
       var result = sut.DoSomething();

       // Assert
       Assert.Equal(expected, result);
   }
   ```

2. **Use Descriptive Test Names**:
   ```csharp
   // Good ✅
   public void ParseBase32_WithValidInput_ReturnsDecodedBytes()

   // Bad ❌
   public void Test1()
   ```

3. **Test Edge Cases**:
   - Null/empty inputs
   - Boundary values
   - Exception scenarios
   - Concurrent operations

4. **Keep Tests Independent**:
   - Each test should run in isolation
   - Don't rely on test execution order
   - Clean up resources after tests

### Before Committing

```bash
# 1. Run all tests
dotnet test

# 2. Check coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
open ./coverage-report/index.html

# 3. Ensure coverage meets minimum (60%)
# 4. Commit and push
```

### Working with Pull Requests

1. **Create PR** against appropriate branch
2. **Wait for CI/CD** to complete
3. **Review coverage report** in PR comment
4. **Address failing tests** or low coverage
5. **Request review** once all checks pass

## Troubleshooting

### Tests Fail Locally But Pass in CI/CD (or vice versa)

**Cause**: Environment differences

**Solution**:
```bash
# Ensure you're using the same .NET version
dotnet --version  # Should be 9.0.x

# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

### Coverage Report Not Generated

**Cause**: Missing coverage collector or wrong format

**Solution**:
```bash
# Use the correct collector format
dotnet test --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

### "Resource not accessible" Error in CI/CD

**Cause**: Missing permissions in workflow

**Solution**: Ensure workflow has proper permissions (already configured):
```yaml
permissions:
    contents: read
    pull-requests: write
```

### Duplicate CI/CD Runs on PR Commits

**Cause**: This is expected behavior (one workflow run per commit)

**Not a Problem**: Each commit in a PR triggers the workflow to ensure code quality at every step.

### Coverage Showing 0% or Incorrect Values

**Cause**: Wrong coverage file path or multiple test runs

**Solution**:
```bash
# Clean previous coverage results
rm -rf coverage/

# Run tests again
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### ReportGenerator Command Not Found

**Cause**: Tool not installed

**Solution**:
```bash
# Install globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Or install locally in project
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
```

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Code Coverage in .NET](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

## Questions or Issues?

If you encounter any testing-related issues:
1. Check this guide first
2. Review existing test examples in the codebase
3. Ask the team in pull request discussions
4. Create an issue with detailed error messages

---

**Last Updated**: January 2026
