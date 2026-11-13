# Agent team for herdr

A task board, a planner, parallel developers, an independent tester and a
reviewer, run as real agents in herdr tabs by one bash script.

```
tasks.json ─► PLANNER ─► you approve ─► ┌─ DEV ⇄ QA ─┐ (one pair per subtask,   ─► REVIEWER ─► PR
  (board)      plan.json                 └─ DEV ⇄ QA ─┘  each in its own worktree)     ⇅
                                              merge into team/<task>                DEV ⇄ QA (fixes)
```

## Set up (once per repository)

1. Copy the `.team/` folder into the root of your repository.
2. Edit `.team/config.sh`: set `TEST_CMD` and `SETUP_CMD` for your project and
   pick which agent plays each role.
3. Replace the examples in `.team/GUIDELINES.md` with your real coding rules
   and strategy. This is what the reviewer enforces.
4. Needed on your machine: `herdr` (running), `git`, `jq`, and `gh` logged in
   (`gh auth login`) if you want PRs opened for you.
5. From a herdr pane at the repository root, run `.team/bin/team doctor`. It
   starts each agent you configured, gives it a one-line file assignment and
   checks the answer, so flag or login problems show up in a minute instead
   of halfway through a task.

`.team/test/selftest.sh` checks the script's own logic against a fake herdr
(temp directory only); run it after you change `bin/team`.

## Use

Run these from a pane inside herdr, at the repository root.

```
.team/bin/team add "Add rate limiting to the API" "Why, and any pointers"
.team/bin/team run            # next todo task; stops to show you the plan
.team/bin/team run T2 --yes   # a specific task, no plan approval stop
.team/bin/team run --all      # every todo task, one after another
.team/bin/team board          # where everything stands
.team/bin/team clean T2       # close its workspace, remove its worktrees
.team/bin/team doctor         # check herdr and your agents
```

Write acceptance criteria into `tasks.json` for each task. They are what the
planner splits up and what QA tests against.

## What happens in a run

| Stage | Agent | Where | Produces | Passes when |
|---|---|---|---|---|
| Plan | `plan-<task>` | `team/<task>` worktree | `plan.json`: 1–5 subtasks with acceptance criteria and dependencies | JSON is valid; you approve it |
| Build | `dev-<task>-s1`… | own worktree, branch `team/<task>-s1` | commits + handoff note | note says DONE, tree clean, new commits |
| Gate | the script itself | same worktree | output of `TEST_CMD` | exit code 0 |
| Test | `qa-<task>-s1`… | same worktree, read-only | report starting `VERDICT: PASS/FAIL` | PASS; FAIL sends the report back to the dev |
| Merge | the script itself | `team/<task>` | merge commit | conflict goes back to that dev, then through QA again |
| Review | `rev-<task>` | `team/<task>`, read-only | review starting `VERDICT: APPROVE/CHANGES` + PR text | APPROVE; CHANGES goes to a fix dev, then QA, then back to review |
| PR | the script itself | — | `git push` + `gh pr create` | — |

Subtasks with no dependency on each other run at the same time, up to
`MAX_PARALLEL`. Loops are capped (`MAX_QA_ROUNDS`, `MAX_REVIEW_ROUNDS`); when a
cap is hit the task is marked `blocked` and you are called.

## Where the agents live in herdr

Each task gets one herdr workspace, named `team <task>`. Every agent runs in
its own tab there, labelled with its name (`plan-t1`, `dev-t1-s1`, `qa-t1-s1`,
`rev-t1`, ...) and opened in that agent's worktree. The workspace's first tab
is a plain shell on the task branch, left free for you. `team clean` closes
the whole workspace.

## How agents get their identity and their work

Each turn, the script writes one markdown file into the agent's worktree
(`.team/io/<name>-task.md`) containing the role from `.team/roles/` plus the
specific assignment, then sends the agent a one-line prompt: read that file and
follow it. Agents answer by writing files (plan, handoff note, QA report,
review). The script decides a turn is finished from those files and from git
state, never from what the agent says or from herdr's idle/working status.

To change how a role behaves, edit its file in `.team/roles/`.

## When it needs you

- **An agent shows `blocked` in herdr** (permission prompt, trust-this-folder
  screen, a question). The script prints `!! NEEDS YOU`, rings the bell, and
  waits. Answer in that agent's tab and the run continues. Choosing "always
  allow" for routine commands (tests, git) makes later runs quieter; so do
  allow rules in your agent's settings, or looser `*_ARGS` in `config.sh`.
- **A task ends `blocked`.** Read `.team/runs/<task>/reports/` and
  `.team/runs/<task>/run.log`, fix or clarify, then `team run <ID> --yes`
  again: finished subtasks are kept and planning is not repeated.
- **A task ends `approved` with no PR** (push or `gh` failed). Fix the cause
  and run `team run <ID>`: only the push and PR are retried.
- **You want a different plan.** Edit `.team/runs/<task>/plan.json` at the
  approval stop, then `team run <ID> --yes`.

## Files

```
.team/
  tasks.json        the board (task and subtask status, branch, PR link)
  config.sh         commands, agents per role, limits, human gates
  GUIDELINES.md     your rules; copied to every agent
  roles/*.md        planner, dev, qa, reviewer
  bin/team          the orchestrator
  test/             self-test with a fake herdr
  runs/<task>/      plan.json, run.log, reports/ (git-ignored locally)
```

Worktrees go to `../<repo>-worktrees/`. `main` is never touched; everything
lands on `team/<task>` and reaches `main` only through the PR you merge.
