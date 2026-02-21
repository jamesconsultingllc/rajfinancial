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
git clone https://github.com/jamesconsultingllc/rajfinancial.git
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
├── .github/
│   ├── workflows/              # GitHub Actions CI/CD workflows
│   └── actions/                # Custom reusable actions
├── src/
│   ├── Client/                 # Blazor WebAssembly project
│   ├── Api/                    # Azure Functions API
│   ├── Shared/                 # Shared models and DTOs
│   └── RajFinancial.sln        # Solution file
├── tests/
│   ├── UnitTests/              # Unit tests (xUnit)
│   └── AcceptanceTests/        # E2E tests (Playwright + Reqnroll)
├── scripts/                    # Automation scripts
│   ├── infra/                  # Infrastructure provisioning
│   └── *.ps1                   # CI/CD and operational scripts
├── docs/                       # Documentation
└── themes/                     # Brand themes (Loveable)
```

## 🔧 GitHub Actions Workflow

This project uses a comprehensive CI/CD pipeline with:

- ✅ **Unit Tests** - Run before deployment
- ✅ **Environment-based Deployment** - Production, Development, and Preview environments
- ✅ **Settings Synchronization** - Automatic config copying between environments
- ✅ **Entra Redirect URI Management** - Automatic redirect URI setup for preview environments
- ✅ **E2E Tests** - Playwright tests after deployment (Chromium, Firefox, WebKit, Edge)
- ✅ **Automatic Cleanup** - Preview environments cleaned up on PR merge

### Branch Strategy

| Branch Pattern | Environment | URL |
|----------------|-------------|-----|
| `main` | Production | `https://gray-cliff-072f3b510.azurestaticapps.net` |
| `develop` | Development | `https://gray-cliff-072f3b510-develop.centralus.azurestaticapps.net` |
| `feature/*` | Preview | Auto-generated preview URL |
| `hotfix/*` | Preview | Auto-generated preview URL |
| `release/*` | Preview | Auto-generated preview URL |

## 🛠️ Scripts

All automation scripts are in the [`scripts/`](./scripts/) directory:

| Category | Scripts | Purpose |
|----------|---------|---------|
| **Infrastructure** | `infra/register-entra-apps.ps1` | Register SPA & API apps in Entra External ID |
| **CI/CD Setup** | `setup-entra-oidc.ps1`, `add-entra-federated-credentials.ps1` | Configure GitHub Actions OIDC authentication |
| **Configuration** | `configure-entra-app-roles.ps1`, `configure-user-flows.ps1` | App roles and MFA configuration |

See [`scripts/README.md`](./scripts/README.md) for detailed documentation.

## 🧪 Testing

```powershell
# Run unit tests
dotnet test tests/UnitTests/RajFinancial.UnitTests.csproj

# Run E2E tests (requires app to be running)
$env:BASE_URL = "http://localhost:5000"
$env:BROWSER = "chromium"
dotnet test tests/AcceptanceTests/RajFinancial.AcceptanceTests.csproj
```

## 🌍 Environments

### Azure Static Web Apps

- **Production**: https://gray-cliff-072f3b510.azurestaticapps.net (main branch)
- **Development**: https://gray-cliff-072f3b510-develop.centralus.azurestaticapps.net (develop branch)
- **Preview**: Auto-generated for feature/hotfix/release branches

### Entra External ID Tenants

| Environment | Tenant Domain | MFA |
|-------------|---------------|-----|
| Development | `rajfinancialdev.onmicrosoft.com` | Disabled |
| Production | `rajfinancialprod.onmicrosoft.com` | Enabled |

## 📚 Documentation

- [Scripts Documentation](./scripts/README.md) - Automation scripts guide
- [UI Design](./docs/RAJ_FINANCIAL_UI.md) - UI component specifications
- [Execution Plan](./docs/RAJ_FINANCIAL_EXECUTION_PLAN.md) - Development roadmap
- [Copilot Instructions](./.github/copilot-instructions.md) - AI coding guidelines

## 🔐 Security

This project uses:
- **OIDC federated credentials** - No long-lived secrets for Azure/Entra authentication
- **Pinned GitHub Actions versions** - Prevent supply chain attacks
- **Environment-based approvals** - Required for production deployments
- **Minimal workflow permissions** - Least privilege principle
- **OWASP compliance** - Following Top 10:2025 security guidelines

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes and commit: `git commit -am "Add new feature"`
3. Push to the branch: `git push origin feature/my-feature`
4. Create a Pull Request to `develop`

The workflow will automatically:
- Run unit tests
- Deploy to a preview environment
- Configure Entra redirect URIs
- Run E2E tests across multiple browsers
- Clean up the preview when the PR is merged

## 📝 License

Proprietary - RAJ Financial Software

## 🆘 Support

For issues or questions:
1. Check the [scripts documentation](./scripts/README.md)
2. Review GitHub Actions workflow logs
3. Open an issue in GitHub

## 🔗 Resources

- [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [Entra External ID](https://learn.microsoft.com/entra/external-id/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
