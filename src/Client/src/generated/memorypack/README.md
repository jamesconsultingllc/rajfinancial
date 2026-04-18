# Auto-Generated MemoryPack TypeScript DTOs

> **Do not edit files in this directory by hand. They are regenerated on every `dotnet build`.**

## What

TypeScript type definitions and MemoryPack (de)serializers generated from the C# DTOs in
[`src/Shared/`](../../../../Shared/) by the [MemoryPack](https://github.com/Cysharp/MemoryPack)
Roslyn source generator.

Configuration lives in [`src/Shared/RajFinancial.Shared.csproj`](../../../../Shared/RajFinancial.Shared.csproj)
under the `MemoryPackGenerator_TypeScript*` properties.

## Why this folder is git-ignored

The output is deterministic and 100% derived from the C# source. Committing it causes noisy
diffs on every build and merge conflicts that add zero information. See `.gitignore` at the
repository root.

## How to regenerate

Any of these will repopulate the folder:

```powershell
# Build just the Shared project (fastest)
dotnet build src/Shared/RajFinancial.Shared.csproj

# Or build the full solution (runs automatically in CI)
dotnet build src/RajFinancial.sln
```

After regeneration the TS DTOs are immediately consumable by the React client.

## If files are missing

If `npm run dev` / `npm test` fails with `Cannot find module '@/generated/memorypack/...'`,
you need to run the `dotnet build` command above. This is the expected one-time setup after
a fresh clone. CI pipelines always build `.NET` first, so PRs are unaffected.

