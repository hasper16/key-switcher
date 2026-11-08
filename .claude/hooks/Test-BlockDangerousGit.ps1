# Regression cases for block-dangerous-git.ps1. Run: pwsh -File .claude/hooks/Test-BlockDangerousGit.ps1
# Commands are fed to the hook as JSON, exactly as Claude Code does; exit 2 means blocked.
$hook = Join-Path $PSScriptRoot 'block-dangerous-git.ps1'
$nl = "`n"
$cases = @(
  @{ c = 'gh pr merge 7 --merge'; want = 0 },
  @{ c = 'gh pr merge 7 --admin --merge'; want = 2 },
  @{ c = "gh pr merge 7 '--admin' --merge"; want = 2 },
  @{ c = 'gh pr merge 7 "--admin" --merge'; want = 2 },
  @{ c = "gh pr merge 7 ``$nl  --admin --merge"; want = 2 },
  @{ c = "gh pr merge 7 \$nl  --admin"; want = 2 },
  @{ c = 'gh pr checks 7 --required'; want = 0 },
  @{ c = 'git push --force origin main'; want = 2 },
  @{ c = "git push 'origin' --force"; want = 2 },
  @{ c = 'git commit -m "feat: x" --no-verify'; want = 2 },
  @{ c = 'git config core.hooksPath .githooks'; want = 0 },
  @{ c = 'git config core.hooksPath /dev/null'; want = 2 },
  @{ c = 'git commit -m "feat: x"'; want = 0 },
  @{ c = 'git status'; want = 0 }
)
$failures = 0
foreach ($t in $cases) {
  $json = @{ tool_input = @{ command = $t.c } } | ConvertTo-Json -Compress
  $null = $json | pwsh -NoProfile -File $hook 2>$null
  $code = $LASTEXITCODE
  if ($code -ne $t.want) {
    $failures++
    Write-Host ("FAIL exit {0}, want {1}: {2}" -f $code, $t.want, ($t.c -replace $nl, '\n'))
  }
}
Write-Host "$($cases.Count - $failures)/$($cases.Count) passed"
exit [int]($failures -gt 0)
