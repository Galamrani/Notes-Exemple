#!/usr/bin/env bash
# Runs the whole pipeline in a throwaway repo against a fake herdr and fake gh.
# Proves the orchestration logic (waves, loops, gates, merge, PR); it does not
# prove anything about real agents.   Usage: .team/test/selftest.sh
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; SRC="$(dirname "$HERE")"
T="${SELFTEST_DIR:-$(mktemp -d)}"; [ -n "${SELFTEST_KEEP:-}" ] || trap 'rm -rf "$T"' EXIT
export FAKE_STATE="$T/state" PATH="$HERE/fake-bin:$PATH" HERDR_ENV=1
export GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@t GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@t
chmod +x "$HERE/fake-bin/"*
fail=0; ok() { echo "  ok   $1"; }; bad() { echo "  FAIL $1"; fail=1; }
check() { local d="$1"; shift; if "$@" >/dev/null 2>&1; then ok "$d"; else bad "$d"; fi; }

git init -q --bare "$T/origin.git"
git init -q -b main "$T/repo"; cd "$T/repo" || exit 1
echo readme >README.md; echo base >shared.txt
mkdir .team; cp -R "$SRC/roles" "$SRC/bin" "$SRC/GUIDELINES.md" .team/
cat >.team/config.sh <<'EOF'
TEST_CMD='touch test-output.tmp; test ! -f FAIL_GATE'
PLANNER_ARGS="--model x"; DEV_ARGS=""; MAX_PARALLEL=2; BLOCK_POLL_S=0
EOF
cat >.team/tasks.json <<'EOF'
{"tasks":[{"id":"T1","title":"demo task","description":"d","acceptance":["works"],"priority":1,"status":"todo","subtasks":[],"branch":null,"pr":null}]}
EOF
git add -A; git commit -qm init; git remote add origin "$T/origin.git"; git push -q -u origin main
team=.team/bin/team; st() { jq -r "$1" .team/tasks.json; }

echo "0. doctor"
check "doctor passes"                        bash -c "$team doctor >'$T/out0' 2>&1"
check "one check per distinct agent setup"   test "$(grep -c 'took a file assignment' "$T/out0")" = 2
check "doctor cleans up after itself"        bash -c "! ls .team/io/doctor-* .team/runs/doctor 2>/dev/null | grep -q ."
FAKE_NO_STATUS_FIELD=1 $team doctor >"$T/out0b" 2>&1
check "unreadable status field is reported, not fatal" bash -c "grep -q 'status field was not recognised' '$T/out0b' && grep -q 'all good' '$T/out0b'"

echo "1. plan, then stop for approval (no terminal attached)"
$team run </dev/null >"$T/out1" 2>&1
check "task waits as 'planned'"              test "$(st '.tasks[0].status')" = planned
check "three subtasks on the board"          test "$(st '.tasks[0].subtasks|length')" = 3
check "planner was nudged after stopping early" grep -q "plan-t1 stopped early" "$T/out1"
check "planner's stray edit was discarded"   test "$(cat ../repo-worktrees/t1/README.md)" = readme

echo "2. approve and run to the end"
$team run T1 --yes </dev/null >"$T/out2" 2>&1 || bad "run exited non-zero"
W=../repo-worktrees/t1
check "status pr_open"                       test "$(st '.tasks[0].status')" = pr_open
check "PR url on the board"                  test "$(st '.tasks[0].pr')" = "https://github.com/example/repo/pull/1"
check "all subtasks merged"                  test "$(st '[.tasks[0].subtasks[].status]|unique|join(",")')" = merged
check "s1 and s2 ran in the same wave"       grep -q "wave: s1 s2" "$T/out2"
check "s3 waited for its dependencies"       grep -q "wave: s3" "$T/out2"
check "QA failure looped back to dev"        grep -q "QA FAIL (round 1)" "$T/out2"
check "QA's edits to the code were discarded" test ! -e ../repo-worktrees/t1-s1/stray.txt
check "uncommitted dev work was caught"      grep -q "uncommitted changes" "$T/out2"
check "blocked agent raised a human alert"   grep -q "NEEDS YOU: qa-t1-s2" "$T/out2"
check "merge conflict went back to the dev"  grep -q "merge conflict" "$T/out2"
check "both sides survived the conflict"     test "$(cat $W/shared.txt | tr '\n' ,)" = "from s1,from s2,"
check "review changes triggered a fix round" grep -q "reviewer asked for changes" "$T/out2"
check "orchestrator's own test gate caught a break" grep -q "test gate FAILED" "$T/out2"
check "decorated verdict line was understood" bash -c "grep -q 't1/s3. QA PASS' '$T/out2' && ! grep -q 'qa-t1-s3 stopped early' '$T/out2'"
check "test command's leftovers were discarded" bash -c "! ls ../repo-worktrees/*/test-output.tmp 2>/dev/null | grep -q ."
check "gate-breaking file is gone"           test ! -e $W/FAIL_GATE
check "fix landed on the branch"             test -f $W/src/fix.txt
check "branch pushed to origin"              git -C "$T/origin.git" rev-parse -q --verify refs/heads/team/t1
check "pushed branch == local branch"        test "$(git -C "$T/origin.git" rev-parse team/t1)" = "$(git rev-parse team/t1)"
check "gh got base, head and title"          bash -c "grep -qx main '$FAKE_STATE/gh.args' && grep -qx team/t1 '$FAKE_STATE/gh.args' && grep -qx 'Add the thing' '$FAKE_STATE/gh.args'"
check "PR body has no title line"            test "$(grep -c 'Add the thing' .team/runs/t1/reports/pr-body.md)" = 0
check "agent args passed through"            grep -q -- "plan-t1 .* -- --model x" "$FAKE_STATE/starts"
check "no '--' when a role has no args"      grep -q -- "dev-t1-s1 --kind claude --pane [^ ]* --timeout [0-9]*$" "$FAKE_STATE/starts"
check "every agent got its own labelled tab" bash -c "grep -q -- 'tab create .*--label dev-t1-s1 ' '$FAKE_STATE/herdr.log' && grep -q -- '--label qa-t1-s1 ' '$FAKE_STATE/herdr.log' && grep -q -- '--label rev-t1 ' '$FAKE_STATE/herdr.log'"
check "no split panes"                       bash -c "! grep -q '^pane split' '$FAKE_STATE/herdr.log'"
check "one workspace for the whole task"     test "$(grep -c 'workspace create .*--label team t1' "$FAKE_STATE/herdr.log")" = 1
check "tabs open in the agent's own worktree" grep -q -- "tab create .*--cwd .*repo-worktrees/t1-s2 --label qa-t1-s2" "$FAKE_STATE/herdr.log"
check "reports collected"                    test -f .team/runs/t1/reports/s1-qa-r1.md -a -f .team/runs/t1/reports/review-r2.md
check "main checkout untouched"              test -z "$(git status --porcelain -- . ':!.team')"
check "main branch untouched"                test "$(git rev-parse main)" = "$(git rev-parse origin/main)"

echo "3. a subtask that never passes QA stops the task and calls a human"
$team add "second task" "d" >/dev/null
FAKE_QA_ALWAYS_FAIL=1 $team run T2 --yes </dev/null >"$T/out3" 2>&1 && bad "run should exit non-zero"
check "task T2 is blocked"                   test "$(st '.tasks[1].status')" = blocked
check "stopped after MAX_QA_ROUNDS"          grep -q "did not pass after 3 dev/QA rounds" "$T/out3"
check "healthy sibling subtask still merged" test "$(st '.tasks[1].subtasks[]|select(.id=="s2").status')" = merged
check "dependent subtask never started"      test "$(st '.tasks[1].subtasks[]|select(.id=="s3").status')" = todo
check "no PR for a blocked task"             test "$(st '.tasks[1].pr')" = null
$team run T2 --yes </dev/null >"$T/out4" 2>&1 || bad "resume exited non-zero"
check "re-running resumes and finishes"      test "$(st '.tasks[1].status')" = pr_open
check "resume skipped planning and s2"       bash -c "! grep -q -e 'planning →' -e 'wave: s2' '$T/out4'"

echo "4. a failed PR can be retried without redoing the work"
$team add "third task" "d" >/dev/null
FAKE_GH_FAIL=1 $team run T3 --yes </dev/null >"$T/out5" 2>&1 && bad "run should exit non-zero when the PR fails"
check "task waits as 'approved'"             test "$(st '.tasks[2].status')" = approved
$team clean T3 >/dev/null 2>&1
$team run T3 --yes </dev/null >"$T/out6" 2>&1 || bad "retry exited non-zero"
check "retry opened the PR"                  test "$(st '.tasks[2].status')" = pr_open
check "retry did not redo plan, dev or review" bash -c "! grep -q -e 'planning' -e 'wave:' -e 'review round' '$T/out6'"
check "a task with a PR is not run again"    bash -c "$team run T3 --yes </dev/null | grep -q 'already has a PR'"

echo "5. a workspace you closed by hand is recreated"
$team add "fourth task" "d" >/dev/null
$team run T4 </dev/null >/dev/null 2>&1                      # plans, then stops for approval
cat .team/runs/t4/panes/task.cur >>"$FAKE_STATE/closed"       # you close the workspace in herdr
$team run T4 --yes </dev/null >"$T/out7" 2>&1 || bad "run after closing the workspace exited non-zero"
check "task still finished"                  test "$(st '.tasks[3].status')" = pr_open
check "exactly one replacement workspace"    test "$(grep -c 'workspace create .*--label team t4' "$FAKE_STATE/herdr.log")" = 2

echo "6. board and clean"
check "board prints"                         bash -c "$team board | grep -q 'T1 .*pr_open'"
$team clean T1 >/dev/null 2>&1
check "worktrees removed"                    test ! -d ../repo-worktrees/t1 -a ! -d ../repo-worktrees/t1-s1
check "herdr workspace closed"               test -s "$FAKE_STATE/closed"
check "subtask branches deleted, PR branch kept" bash -c "! git rev-parse -q --verify team/t1-s1 && git rev-parse -q --verify team/t1"

if [ $fail -ne 0 ]; then echo; echo "---- run output ----"; cat "$T"/out*; echo "SELFTEST FAILED"; exit 1; fi
echo "SELFTEST PASSED"
