// PreToolUse hook: blocks AI edits to local.settings.json (which contains
// Azure connection strings / secrets). This is a path-based block — it does
// NOT scan tool input content for arbitrary secret patterns. Content-pattern
// scanning is delegated to the advanced-security Copilot plugin and to
// pre-commit secret scanners.
// Runs on both Windows and macOS via: node .claude/hooks/block-secrets.js
const input = JSON.parse(process.env.CLAUDE_TOOL_INPUT || '{}');
const fp = (input.file_path || '').replace(/\\/g, '/').toLowerCase();
if (fp.includes('local.settings.json')) {
  console.log('BLOCKED: local.settings.json contains Azure connection strings and secrets.');
  console.log('Edit this file manually — never allow AI to modify it.');
  process.exit(2);
}
