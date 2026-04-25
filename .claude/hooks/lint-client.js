// PostToolUse hook: runs ESLint --fix on TypeScript files in src/Client after AI edits.
// Runs on both Windows and macOS via: node .claude/hooks/lint-client.js
// Uses spawnSync (array args) to avoid any shell-injection risk on file paths.
const { spawnSync } = require('child_process');
const input = JSON.parse(process.env.CLAUDE_TOOL_INPUT || '{}');
const fp = (input.file_path || '').replace(/\\/g, '/');

if (fp.includes('src/Client') && /\.(ts|tsx)$/.test(fp) && !fp.endsWith('.d.ts')) {
  console.log(`ESLint: fixing ${fp}`);
  const result = spawnSync('npx', ['eslint', '--fix', fp], {
    cwd: 'src/Client',
    encoding: 'utf8',
    shell: false,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  // Fail-closed: if spawn itself failed (e.g., npx not found, EPERM), result.error
  // is populated and result.status is null — surface it and exit non-zero so the
  // hook never silently stops linting.
  if (result.error) {
    console.error(`ESLint hook failed to spawn npx: ${result.error.message}`);
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
