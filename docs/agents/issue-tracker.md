# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues on
[`rcodesmith/grid-runner`](https://github.com/rcodesmith/grid-runner/issues).
Use the `gh` CLI for all tracker operations (`gh issue create`, `gh issue view`,
`gh issue comment`, `gh issue edit`).

Until 2026-09-23 the tracker was local markdown under `.scratch/`. Those issues
were migrated to GitHub (#1–#4) and `.scratch/` was removed — don't recreate it.

## Relationship to `stories/`

`stories/` predates the tracker and holds long-form planning documents written
by the `story-workflow` skill. It is **not** the issue tracker. Keep the two
separate:

- `stories/` — planning and brainstorming, one document per feature
- GitHub issues — issues and specs consumed by `to-tickets`, `triage`,
  `to-spec`, and `wayfinder`

Don't file tickets into `stories/`, and don't write story documents as issues.

## Conventions

- One feature = one **parent issue** holding the spec (decisions, scope, out of
  scope). Implementation work is filed as **sub-issues** of that parent.
- Size sub-issues so each one ends in something visible or testable on its own
  — a vertical slice, not a layer. Don't split so fine that a ticket can't be
  checked without its sibling.
- Triage state is a GitHub **label** (see `triage-labels.md`). Every open issue
  carries exactly one triage label.
- Blocking is recorded with GitHub's issue dependencies ("blocked by") and
  repeated as a `Blocked by: #N` line at the top of the body.
- Comments and conversation history go in issue comments.
- Images can't be attached through `gh`. Commit reference images under
  `docs/reference/` and link them from the issue.

## When a skill says "publish to the issue tracker"

Create a GitHub issue with `gh issue create -R rcodesmith/grid-runner`, applying
a triage label.

## When a skill says "fetch the relevant ticket"

`gh issue view <number> -R rcodesmith/grid-runner --comments`. The user will
normally pass the issue number or URL directly.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a markdown file in the repo; its **child**
tickets are GitHub issues.

- **Map**: `docs/<effort>/map.md` — the Notes / Decisions-so-far / Fog body. It
  stays in the repo so every session can read it without the network (e.g.
  `docs/world-format/map.md`, which CLAUDE.md points to).
- **Child ticket**: a GitHub issue with the question in the body, labelled
  `type:research` / `type:prototype` / `type:grilling` / `type:task` (create the
  label on first use), and linked from the map.
- **Blocking**: GitHub "blocked by" dependencies. A ticket is unblocked when
  every issue blocking it is closed.
- **Frontier**: open, unblocked, unassigned child issues; lowest number wins.
- **Claim**: assign the issue to yourself (`gh issue edit <n> --add-assignee @me`)
  before any work.
- **Resolve**: comment the answer under an `## Answer` heading, close the issue,
  then append a context pointer (gist + issue link) to the map's
  Decisions-so-far.
