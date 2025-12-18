# Raj Financial - Blazor WebAssembly Application

A modern financial application built with Blazor WebAssembly and deployed to Azure Static Web Apps.

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Git](https://git-scm.com/)

### Local Development
```powershell
# Clone the repository
git clone https://github.com/your-org/rajfinancial.git
cd rajfinancial

# Restore dependencies
dotnet restore src/RajFinancial.sln

# Build the solution
dotnet build src/RajFinancial.sln

# Run the application locally
cd src/Client
dotnet run
```

## 📁 Project Structure

```
rajfinancial/
├── .github/workflows/          # GitHub Actions workflows
├── src/
│   ├── Client/                 # Blazor WebAssembly project
│   ├── Server/                 # Azure Functions API
│   ├── Shared/                 # Shared models and DTOs
│   └── RajFinancial.sln        # Solution file
├── tests/
│   ├── UnitTests/              # Unit tests (xUnit)
│   ├── IntegrationTests/       # Integration tests
│   └── AcceptanceTests/        # E2E tests (Playwright)
└── docs/                       # Documentation
```

## 🔧 GitHub Actions Workflow

This project uses a comprehensive CI/CD pipeline with:

- ✅ **Unit Tests** - Run before deployment
- ✅ **Environment-based Deployment** - Production, Development, and Preview environments
- ✅ **Settings Synchronization** - Automatic config copying
- ✅ **E2E Tests** - Playwright tests after deployment
- ✅ **Automatic Cleanup** - Preview environments cleaned up on PR merge

### Setup Instructions

1. **Azure Configuration**: Follow [QUICK_SETUP_GUIDE.md](./QUICK_SETUP_GUIDE.md)
2. **Workflow Details**: See [WORKFLOW_UPDATE_SUMMARY.md](./WORKFLOW_UPDATE_SUMMARY.md)
3. **Visual Guide**: See [WORKFLOW_VISUAL_GUIDE.md](./WORKFLOW_VISUAL_GUIDE.md)

## 🧪 Testing

```powershell
# Run unit tests
dotnet test tests/UnitTests/UnitTests.csproj

# Run integration tests
dotnet test tests/IntegrationTests/IntegrationTests.csproj

# Run E2E tests (requires app to be running)
$env:BASE_URL = "http://localhost:5000"
dotnet test tests/AcceptanceTests/AcceptanceTests.csproj
```

For more details, see [tests/README.md](./tests/README.md)

## 🌍 Environments

- **Production**: https://gray-cliff-072f3b510.azurestaticapps.net (main branch)
- **Development**: https://development.gray-cliff-072f3b510.azurestaticapps.net (develop branch)
- **Preview**: Auto-generated for feature/hotfix/release branches

## 📚 Documentation

- [Quick Setup Guide](./QUICK_SETUP_GUIDE.md) - Get started with Azure and GitHub setup
- [Azure Federated Credentials](./AZURE_FEDERATED_CREDENTIALS_SETUP.md) - Detailed Azure configuration
- [Workflow Comparison](./WORKFLOW_COMPARISON.md) - Compare with texas-build-pros
- [Workflow Visual Guide](./WORKFLOW_VISUAL_GUIDE.md) - Visual workflow documentation
- [Implementation Complete](./IMPLEMENTATION_COMPLETE.md) - Setup completion checklist
- [Test Guide](./tests/README.md) - Testing best practices

## 🔐 Security

This project uses:
- OIDC federated credentials (no long-lived secrets)
- Pinned GitHub Actions versions
- Environment-based approvals for production
- Minimal workflow permissions

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes and commit: `git commit -am "Add new feature"`
3. Push to the branch: `git push origin feature/my-feature`
4. Create a Pull Request to `develop`

The workflow will automatically:
- Run unit tests
- Deploy to a preview environment
- Run E2E tests
- Clean up the preview when the PR is merged

## 📝 License

[Your License Here]

## 🆘 Support

For issues or questions:
1. Check the [troubleshooting guide](./IMPLEMENTATION_COMPLETE.md#troubleshooting-guide)
2. Review [workflow documentation](./WORKFLOW_VISUAL_GUIDE.md)
3. Open an issue in GitHub

## 🔗 Resources

- [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [xUnit Documentation](https://xunit.net/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
