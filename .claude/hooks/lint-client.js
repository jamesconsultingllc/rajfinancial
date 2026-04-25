// PostToolUse hook: runs ESLint --fix on TypeScript files in src/Client after AI edits.
// Runs on both Windows and macOS via: node .claude/hooks/lint-client.js
// Uses spawnSync (array args) to avoid any shell-injection risk on file paths.
const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');
// Claude Code pipes the hook payload to stdin as JSON; fd 0 is portable across platforms.
const payload = JSON.parse(fs.readFileSync(0, 'utf-8') || '{}');
const input = payload.tool_input || {};
const fp = (input.file_path || '').replace(/\\/g, '/');

// We run ESLint with cwd=src/Client, so the path we hand it must be resolved
// relative to that cwd (otherwise a repo-relative path like "src/Client/foo.ts"
// becomes "src/Client/src/Client/foo.ts"). Compute the path relative to the
// Client root and only proceed when the file actually lives under it.
const clientRoot = path.resolve('src/Client');
const resolvedFp = fp ? path.resolve(fp) : '';
const eslintTarget = resolvedFp ? path.relative(clientRoot, resolvedFp).replace(/\\/g, '/') : '';
const isInClient =
  !!eslintTarget &&
  eslintTarget !== '..' &&
  !eslintTarget.startsWith('../') &&
  !path.isAbsolute(eslintTarget);

if (isInClient && /\.(ts|tsx)$/.test(eslintTarget) && !eslintTarget.endsWith('.d.ts')) {
  console.log(`ESLint: fixing ${fp}`);
  // Invoke ESLint's JS entry point via the running Node binary. This avoids the
  // npx/.cmd shim, which Node 20+ refuses to spawn without shell=true on Windows
  // (CVE-2024-27980). Using process.execPath + an absolute script path keeps
  // shell=false, which preserves the no-shell-injection guarantee on file paths.
  const eslintScript = path.join(clientRoot, 'node_modules', 'eslint', 'bin', 'eslint.js');
  if (!fs.existsSync(eslintScript)) {
    console.error('ESLint is not installed under src/Client. Run `npm ci` in src/Client to install dependencies, then retry.');
    process.exit(1);
  }
  const result = spawnSync(process.execPath, [eslintScript, '--fix', eslintTarget], {
    cwd: clientRoot,
    encoding: 'utf8',
    shell: false,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  // Fail-closed: if spawn itself failed (e.g., EPERM), result.error is populated
  // and result.status is null — surface it and exit non-zero so the hook never
  // silently stops linting.
  if (result.error) {
    console.error(`ESLint hook failed to spawn node: ${result.error.message}`);
    process.exit(1);
  }
  if (result.status === null) {
    console.error('ESLint hook: process terminated without an exit code (likely killed by signal).');
    process.exit(1);
  }
  // Propagate ESLint's exit code: 1 = unfixable violations, 2 = config error.
  // Surfaces issues to the user immediately instead of waiting for the pre-commit hook.
  if (result.status !== 0) {
    process.exit(result.status);
  }
}
