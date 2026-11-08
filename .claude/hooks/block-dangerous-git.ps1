# PreToolUse hook: blocks destructive git commands from Bash and PowerShell tool calls.
# Exit code 2 = block; the message on stderr is shown to the agent.

$raw = [Console]::In.ReadToEnd()
try { $command = ($raw | ConvertFrom-Json).tool_input.command } catch { exit 0 }
if (-not $command) { exit 0 }

$patterns = @(
    'git\s+push\b.*(\s--force\b|\s--force-with-lease\b|\s-f\b|\s--delete\b|\s--mirror\b|\s:\S)',
    'git\s+reset\s+.*--hard\b',
    'git\s+clean\s+.*-\w*f',
    'git\s+branch\s+.*-D\b',
    'git\s+checkout\s+(--\s+)?\.(\s|$)',
    'git\s+checkout\s+--\s',
    'git\s+restore\s+(\S+\s+)*\.(\s|$)',
    'git\s+stash\s+(drop|clear)\b',
    'git\s+filter-(branch|repo)\b',
    'git\s+update-ref\s+-d\b',
    '--no-verify\b',
    'core\.hooksPath\s+(?!\.githooks(\s|;|$))[^\s;]+',
    '--unset(-all)?\s+core\.hooksPath',
    'gh\s+pr\s+merge\b.*\s--admin\b'
)

# Quotes and line breaks can hide a flag from the patterns ('--admin', `-continued lines), so
# each command is also checked with quotes removed and all whitespace collapsed.
$normalized = (($command -replace "[`"'``]", '') -replace '\s+', ' ').Trim()

foreach ($p in $patterns) {
    if ($command -match $p -or $normalized -match $p) {
        [Console]::Error.WriteLine("BLOCKED: '$command' matches protected pattern '$p'. Ask the user to run this command themselves.")
        exit 2
    }
}
exit 0
