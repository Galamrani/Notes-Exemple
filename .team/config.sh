# Team configuration for the Notes example: .NET Web API in backend/, Angular app in frontend/.
# Sourced by .team/bin/team (plain bash).

# --- Project -----------------------------------------------------------------
BASE_BRANCH="main"      # branch PRs target
# Run once in every new worktree. The guards make it a no-op until task T1 has created the projects.
SETUP_CMD='if ls backend/*.sln >/dev/null 2>&1; then dotnet restore backend; fi; if [ -f frontend/package-lock.json ]; then (cd frontend && npm ci); fi'
# The gate the script runs itself after every dev turn. Same guards: nothing to test before T1.
TEST_CMD='if ls backend/*.sln >/dev/null 2>&1; then dotnet test backend --nologo || exit 1; fi; if [ -f frontend/package.json ]; then (cd frontend && npm run test:ci) || exit 1; fi'
WORKTREE_ROOT=""        # empty = ../<repo-name>-worktrees
GUIDELINES="AGENTS.md CLAUDE.md CONTRIBUTING.md"

# --- Who plays each role -----------------------------------------------------
PLANNER_KIND="claude";  PLANNER_ARGS="--permission-mode acceptEdits"
DEV_KIND="claude";      DEV_ARGS="--permission-mode acceptEdits"
QA_KIND="claude";       QA_ARGS="--permission-mode acceptEdits"
REVIEWER_KIND="claude"; REVIEWER_ARGS="--permission-mode acceptEdits"

# --- Limits ------------------------------------------------------------------
MAX_SUBTASKS=4          # these tasks are small; keep plans small
MAX_PARALLEL=2          # one backend dev and one frontend dev at a time
MAX_QA_ROUNDS=3
MAX_REVIEW_ROUNDS=3
TURN_TIMEOUT_MIN=45
NUDGES=2

# --- Human gates -------------------------------------------------------------
APPROVE_PLAN=1          # show the plan and wait for your OK before devs start
OPEN_PR=1               # push the branch and open a PR with gh when the reviewer approves
PR_DRAFT=0
REMOTE="origin"
NOTIFY_CMD=""
