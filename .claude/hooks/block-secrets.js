// PreToolUse hook: blocks AI edits to local.settings.json (contains Azure secrets).
// Runs on both Windows and macOS via: node .claude/hooks/block-secrets.js
const input = JSON.parse(process.env.CLAUDE_TOOL_INPUT || '{}');
const fp = (input.file_path || '').replace(/\\/g, '/');
if (fp.includes('local.settings.json')) {
  console.log('BLOCKED: local.settings.json contains Azure connection strings and secrets.');
  console.log('Edit this file manually — never allow AI to modify it.');
  process.exit(2);
}
