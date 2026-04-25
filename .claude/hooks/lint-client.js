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
  // Propagate ESLint's exit code: 1 = unfixable violations, 2 = config error.
  // Surfaces issues to the user immediately instead of waiting for the pre-commit hook.
  if (typeof result.status === 'number' && result.status !== 0) {
    process.exit(result.status);
  }
}
