# Judging generated levels without a human: prior art

Research for [issue #9](https://github.com/rcodesmith/grid-runner/issues/9),
part of the World Evaluation map. Researched 2026-09-23.

**Question.** How do existing systems judge procedurally generated or
AI-authored levels for playability without a human playing? What can we borrow
for winnability, difficulty, "skill matters", and reporting Findings back to
an LLM author?

Sources are primary (papers, official repos) unless marked. Numbers in square
brackets refer to the [References](#references) at the end. Terms (World,
Evaluation, Finding) are as in `CONTEXT.md`.

## Summary

- **The field splits evaluation into three kinds**: *direct* (compute
  something from the content), *simulation-based* (an agent plays it), and
  *interactive* (a human plays it) [1]. Almost every system in this note runs a
  cheap direct/structural layer first and saves simulation for content that
  passes it. GAVEL makes this explicit as a tiered fitness [11].
- **Winnability is almost always "a strong planner can finish it"**: A* on the
  game's own forward model in Mario [3, 8], A* in Sokoban and Word2World [9,
  10], BFS reachability plus minimum path length in PCGRL's Zelda [13]. This
  gives a *lower bound* on solvability: a timeout counts as unsolvable [9].
- **Difficulty is measured as a success rate (or survival curve) of a
  deliberately imperfect agent.** Isaksen et al. make a bot human-like by
  adding Gaussian timing noise and a reaction delay, then fit a survival curve
  [14]. Industry work uses AI pass rate as the difficulty proxy [15], and finds
  that the *best* runs of an agent predict human difficulty better than
  average runs [15].
- **"Skill matters" has a name: Relative Algorithm Performance Profiles
  (RAPP).** Play the content with a ladder of agents from DoNothing to MCTS;
  well-designed games show a larger performance gap between strong and weak
  agents [5]. GVGAI's level and rule generators, and GAVEL's "strategic depth",
  all optimise this gap [4, 11].
- **Play style is modelled separately from skill** as *procedural personas*:
  the same planner with different utility functions (Runner, Monster Killer,
  Treasure Collector, Completionist) [6]. EA frames the same split as *skill*
  vs *style* [16].
- **Reporting to a generator** takes three shapes in the literature: a
  *graded fitness* whose tiers say which gate failed [11], a *constraint
  distance* that tells an infeasible candidate how far it is from feasible
  [4, 17], and — in LLM loops — *the previous level plus its evaluation
  scores* fed into the next round [10], or text summaries of agent episodes
  [18]. Both LLM-loop papers show that closing the loop with an evaluator
  sharply increases playable output (Word2World: about 60% playable with one
  round versus 9 in 10 with rounds [10]).

## 1. Winnability: solvability checks and planning agents

### Structural / direct checks

- **Connectivity of the special tiles.** Sentient Sketchbook deems a map
  playable only if all special tiles (bases, resources) are connected by a
  passable path; unplayable maps are kept in a separate infeasible population
  rather than discarded [17].
- **Entity-count and placement rules.** PCGRL's Zelda problem requires exactly
  one player, one key and one door, the player must be able to reach the key
  and then the door in *at least* X steps (X = 16), and enemies may not spawn
  within 3 tiles of the player [13]. GVGAI's constructive generators enforce
  "only one avatar" and "all non-solid areas connected" [4, §VII-A].
- **Minimum survival window.** GVGAI's sample genetic generator treats "the
  avatar must not die in the first 40 steps" as a feasibility constraint, next
  to "at least one avatar" [4, §VII-B].
- **A spatial "safety" measure.** Liapis et al. compute, for each tile, how
  much closer it is to one reference point (e.g. a base) than to all others,
  and derive *safety* of resources and *exploration* (how much of the map a
  flood fill from one reference point covers before reaching another) [17].
  These are cheap graph metrics, no agent needed.

### Planning agents as the solvability oracle

- **Mario: Baumgarten's A\*.** Winner of the 2009 Mario AI competition. It
  (a) re-implements the game physics so future states can be simulated, (b)
  runs A\* over combinations of button presses with "time to reach the right
  edge" as the heuristic [3]. The modern Mario AI Framework ships it as the
  default agent, and the level-generation track's `GenerateLevel.java` runs it
  on each generated level [8]. Framework levels run "5–20 full levels per
  second", thousands of times faster than real time [3].
- **MarioGPT uses that agent as its playability test**: 88.4% of 250 generated
  levels were completed by Baumgarten's A\* and "therefore considered
  playable" [7]. It also compares the path the model *intended* (it generates
  one) with the path the agent actually took: mean error about 1 tile for
  playable levels vs 4.56 for unplayable ones [7] — a cheap signal of *where*
  the author's mental model and the level diverge.
- **Sokoban via LLM:** a level is "playable" if it is well-formed *and* an A\*
  agent finds a solution within 150,000 steps. The authors state this is only
  a lower bound on true playability [9].
- **Word2World:** an A\* agent checks that all objectives can be completed
  (playability) and reports the shortest path length [10].
- **Perfect-player variant for real-time games.** Isaksen et al. first check
  whether a game variant is possible at all by running their bot with timing
  noise σ = 0 ms, i.e. a perfect player; if even that player fails, the
  variant is impossible (difficulty d = 1) [14].

**Takeaway:** the literature separates *can it be won at all* (structural
reachability plus a strong or perfect agent) from *how hard is it* (a noisy
agent). A timeout of the strong agent is reported as "not shown winnable",
not "proven unwinnable" [9].

## 2. Difficulty: win rates, survival, damage; scaling bot skill

### What is measured

- **Pass/win rate of an agent** as the difficulty proxy. Roohi et al. (Angry
  Birds Dream Blast, 168 levels, ~95k human players) operationalise difficulty
  as pass rate and predict human pass rates from AI playtests; they note pass
  rate "is sensitive to all these challenge types and agnostic with respect to
  the game having a score" [15].
- **Best runs, not average runs.** Kristensen et al.'s hypothesis, tested by
  Roohi et al.: AI agents' performance distributions are long-tailed, so
  features from the agent's *best* attempts (e.g. the top 5%) correlate better
  with human pass rates than averages [15].
- **Survival curves.** Isaksen et al. play thousands of simulated games, fit an
  exponential distribution to the score histogram, and define difficulty d as
  the probability of dying before the first obstacle — d = 0 trivial, d = 1
  impossible [14]. This turns "how long did the bot survive" into a single
  comparable number and uses losses as data, not just wins.
- **Event telemetry per run.** The Mario AI Framework's `MarioResult` exposes
  game status (win/lose/timeout), completion percentage, remaining time, times
  hurt, kills by cause, jumps, lives, etc. [8]. Useful as a checklist of what a
  run record should hold.
- **Static difficulty proxies.** Smith & Whitehead's *leniency* (weighted count
  of hazards and gaps where an action is required) and *linearity* are used to
  plot a generator's expressive range [2, 12]. They describe content, not
  play, and are best used to explain simulated results.

### How bot skill is scaled

| Knob | Source | How |
|---|---|---|
| Timing precision (aim/press noise) | Isaksen et al. [14] | Add N(0, σ) to the ideal action time; human σ measured at 35.9–61.1 ms. σ = 0 is the perfect player. |
| Reaction time | Isaksen et al. [14] | Bot may only use information visible for at least τ ms; τ = 288 ms from their user study. |
| Actions per second | Isaksen et al. [14] | Caps how often the bot can act. |
| Search budget | Zook et al. [19]; GAVEL [11] | Number of MCTS rollouts as a proxy for player skill; GAVEL fixes 0.25 s thinking time per move. |
| Algorithm ladder | GVGAI [4], RAPP [5] | DoNothing < Random < OneStep lookahead < GA/MCTS < OLETS. |
| Style (not skill) | Holmgård et al. [6]; Zhao et al. [16] | Same planner, different utility function; EA separates *skill* (efficiency at the task) from *style*. |

Notes:

- Isaksen's bot deliberately avoids an A\* planner and uses a simple AI "which
  performs well but is easier" to perturb with noise [14]. The skill knobs are
  motor ones (timing, reaction), which matches an action game with a
  guaranteed escape route better than search depth does.
- Zhao et al. warn that high-skill agents often reach it through superhuman
  reaction time or compute, giving an unrealistic "style" even when the
  win rate looks right [16]. Baumgarten's A\* is the canonical example: always
  running, superhuman precision [12].

## 3. Challenge: does skill matter?

- **RAPP (Nielsen, Barros, Togelius, Nelson 2015).** Seven controllers
  (MCTS, GA, OneStep-Heuristic, OneStep-Score, Random, DoNothing, Explorer),
  50 ms per tick, played hand-designed, mutated and random VGDL games.
  Well-designed games show on average a larger performance difference between
  better and worse algorithms [5]. Hypothesis in their words: "good games
  allow good players to play better than bad players".
- **GVGAI generators optimise the gap directly.** The sample genetic level
  generator's feasible population maximises the difference between OLETS and
  One-Step Lookahead; another entry uses MCTS minus One-Step Lookahead,
  normalised to [0, 1] so it does not swamp the constraint terms; the ASP
  generator uses MCTS minus random [4, §VII].
- **Three-tier gap.** For rule generation, Kunanusont et al. use a weak
  (One-Step Lookahead), mediocre (deterministic) and strong (MCTS) agent and
  score a game by the *minimum* of (strong − mediocre) and (mediocre − weak),
  so the game must separate every adjacent pair, not just the extremes
  [4, §VIII].
- **GAVEL's strategic depth**: the proportion of games an MCTS agent wins
  against a random agent (10 playouts), combined with balance, decisiveness,
  completion, agency and coverage by harmonic mean [11]. Harmonic mean means
  one bad dimension drags the whole score down.
- **Degenerate-agency filter.** GAVEL rejects games where fewer than half of
  states offer more than one legal move ("lack of player agency") before any
  expensive evaluation [11]. The analogous question for us: is there ever a
  meaningful choice, or is there one forced corridor?
- **Suspense as a skill-adjacent signal.** One GVGAI generator fits a
  suspense curve computed as the number of actions that lead to death or tie
  at each point, as judged by OLETS [4, §VII-B].

## 4. Reporting: telling a generator *why* a level failed

- **Tiered fitness codes.** GAVEL returns −3 (does not compile), −2 (compiles
  but not playable), −1 (random playouts show gross imbalance or no agency),
  otherwise a score in [0.01, 1] [11]. The sign and tier alone tell the
  generator which gate failed; cheap gates run first.
- **Feasible/infeasible populations (FI-2Pop).** Infeasible candidates are
  scored by *how many constraints they violate* (GVGAI [4]) or distance to
  feasibility, not just rejected; Sentient Sketchbook uses the same two-
  population scheme [17]. The actionable content is "which constraint, by how
  much".
- **Previous level + evaluation → next round.** Word2World runs *rounds*: every
  round after the first receives the previous world and its evaluation scores
  (LLM judgements plus A\* playability and path length) as feedback [10].
  Ablation: one round gives about 60% playable worlds; with rounds, 9 out of
  10 [10].
- **Summaries of agent play to an LLM designer.** Fly, Fail, Fix (NVIDIA,
  2025) has an RL agent play 5 episodes per configuration, then gives GPT-4.1
  the configuration, a description of each parameter, and either text
  summaries (score, flight time) or image strips of the last 8 s of play. Text
  and image feedback both reached the target score within ≤10 iterations,
  often by the 5th; config-only did not [18]. The LLM referenced only the
  metrics relevant to the goal and ignored the rest [18] — a hint to keep
  Findings short and goal-related.
- **Where things went wrong, spatially.** Holmgård et al. present persona
  behaviour as per-map heatmaps and suggest these play traces as designer
  feedback [6]. MarioGPT's intended-path vs agent-path error localises
  divergence [7]. Sentient Sketchbook draws navigable paths and metric scores
  in real time [17]. PathOS (CHI PLAY 2020) is a designer tool built on agent
  navigation traces; participants liked it but said agents moved unlike
  players [20].
- **Balancing loops with adaptive sampling.** RuleSmith (2026) uses LLM
  self-play to measure win-rate disparities and gives more evaluation games to
  promising candidates, fewer to exploratory ones [21] — relevant to our
  2-minute budget.
- **Humans still judge "fun".** Both the GVGAI PCG tracks and the Mario
  level-generation track were judged by humans choosing between pairs of
  generated levels [4, 12]. No system in this survey claims to measure fun
  automatically; they measure proxies.

## 5. Ludii and general game systems

Ludii [22] is a general *board* game system; its lineage (Browne's Ludi,
Browne & Maire 2010 [23]) evaluates games by self-play with measures such as
balance, completion and decisiveness. The most usable modern statement of that
approach is GAVEL [11], which runs on Ludii and is summarised above. Board-game
measures like balance and drawishness assume two players and don't carry over
directly; *completion* (does play reach an end state within a move cap),
*agency*, *coverage* (share of the board ever occupied) and *strategic depth*
(strong vs random win rate) do carry over.

## Implications for us

*Recommendations for the grilling tickets (#13, #14, #15, #11), not
decisions.*

Our rules have a property that shapes everything: `EnemySpeed` (3.5) is below
`PlayerSpeed` (6) by design (`GameConfig.cs`). A perfect runner can outpace any
single chaser, so difficulty comes from imprecision, reaction delay, spawn
pressure over time and being cut off at doorways — much like Isaksen's Flappy
Bird, where the perfect player always survives and noise creates difficulty.

### Bot personalities

1. **Perfect Runner (winnability oracle).** Plans the doorway route
   (`Dungeon.RouteTo`) to the treasure with zero noise and zero reaction
   delay; one run per seed. Losing means "not shown winnable" (a lower bound,
   as in [9]), not "proven impossible". Mirrors Isaksen's σ = 0 check [14] and
   the A\* oracles [3, 7, 9, 10].
2. **Reference bot = Runner with human-like motor noise.** Same route logic,
   plus a reaction delay (only reacts to enemies seen at least τ ago) and
   Gaussian steering/aim noise [14]. Its win rate is the headline difficulty.
   Keep τ and σ in `GameConfig` so they are tunable and not hidden in bot code.
3. **Skill ladder for "skill matters".** Three rungs, following RAPP and
   Kunanusont's weak/mediocre/strong scheme [4, 5]: a *Weak* bot (large τ and
   σ, no shooting), the *Reference* bot, and a *Strong* bot (small τ/σ, shoots
   spawn points that lie on its route). Skill is the noise level, not the
   planner, so the ladder isolates execution skill.
4. **Optional style personas later** [6]: *Runner* (straight to treasure)
   vs *Spawner Killer* (destroys spawn points first). If both win at similar
   rates, the spawn points don't create a real choice. Treat as v2.

### Difficulty and skill-gap metrics

- **Headline:** reference bot win rate over N seeded runs, with a confidence
  interval, compared to the target band. Report *below / within / above band*
  plus the interval, because N will be small under a 2-minute budget.
- **Explanatory metrics per run** (the `MarioResult` checklist [8] adapted):
  outcome (win / death / timeout), time to win or death, HP at the end,
  damage events with location and the room they occurred in, closest enemy
  distance over time, spawn points destroyed.
- **Survival view:** for losses, time of death; a Kaplan-Meier or
  exponential fit [14] turns losses into information when win rate is 0% or
  100% and the headline number saturates.
- **Skill gap:** `gap = min(win(Strong) − win(Reference), win(Reference) −
  win(Weak))` [4]. The `min` makes sure every step up in skill pays off. Low
  gap with a mid win rate means luck, not skill, decides the World.
- **Consider "best runs" features** [15] once human calibration data exists;
  not needed for v1.
- **Budget:** cheap structural gates first, then the Perfect Runner, and only
  then the N-run reference/ladder sweep (GAVEL's ordering [11]). If the budget
  bites, spend extra runs on Worlds whose interval straddles a band edge
  (RuleSmith's adaptive sampling [21]).

### Finding shape

Borrowing from GAVEL tiers [11], FI-2Pop constraint distance [4, 17],
Word2World rounds [10] and Fly, Fail, Fix [18]:

```json
{
  "tier": "structural | winnability | difficulty | challenge",
  "code": "TREASURE_UNREACHABLE",
  "severity": "blocking | warning",
  "message": "No doorway route from the player start to the treasure room.",
  "where": { "room": "r3", "doorway": null, "position": [41.0, 12.5] },
  "measured": 0.18,
  "target": [0.4, 0.7],
  "evidence": { "runs": 20, "bot": "reference", "deaths_by_room": { "r2": 11 } },
  "suggestion": "Move spawn point sp4 away from doorway d2, or widen d2."
}
```

- **`tier` + `severity`** say which gate failed, so the author fixes blocking
  structural problems before tuning difficulty — GAVEL's ordering [11].
- **`measured` vs `target`** gives distance to feasibility, not just
  pass/fail [4, 17].
- **`where`** uses World vocabulary (room, doorway, spawn point IDs), the
  textual equivalent of heatmaps and path overlays [6, 7, 17], so an LLM can
  edit the right element.
- **Keep the set short and relevant to the goal**: the LLM designer in [18]
  ignored metrics unrelated to its target. Return the current World with its
  Findings each round, as Word2World does [10].
- `suggestion` is optional and advisory; the literature feeds scores, not
  fixes, and lets the generator decide [10, 18].

## References

1. J. Togelius, G. N. Yannakakis, K. O. Stanley, C. Browne. "Search-based
   Procedural Content Generation: A Taxonomy and Survey." *IEEE TCIAIG* 3(3),
   2011. Defines direct, simulation-based and interactive evaluation.
   (Cited from memory of the paper; not re-fetched for this note.)
2. N. Shaker, G. Smith, G. N. Yannakakis. "Evaluating content generators."
   Ch. 12 of *Procedural Content Generation in Games*, 2016.
   http://pcgbook.com/chapter12.pdf — expressive range, leniency, linearity.
3. J. Togelius, S. Karakovskiy, R. Baumgarten. "The 2009 Mario AI
   Competition." *IEEE CEC*, 2010. http://julian.togelius.com/Togelius2010The.pdf
   — Baumgarten's A\* agent; simulation speed.
4. D. Perez-Liebana et al. "General Video Game AI: a Multi-Track Framework for
   Evaluating Agents, Games and Content Generation Algorithms." *IEEE ToG*,
   2019. https://arxiv.org/abs/1802.10363 — §IV sample agents, §VII level
   generators and their fitness, §VIII rule generation, human judging of PCG
   tracks.
5. T. S. Nielsen, G. A. B. Barros, J. Togelius, M. J. Nelson. "General Video
   Game Evaluation Using Relative Algorithm Performance Profiles."
   *EvoApplications*, 2015. https://www.kmjn.org/publications/GVGprofiles_Evostar15.pdf
6. C. Holmgård, M. C. Green, A. Liapis, J. Togelius. "Automated Playtesting
   with Procedural Personas through MCTS with Evolved Heuristics." *IEEE ToG*,
   2018. https://arxiv.org/abs/1802.06881 — MiniDungeons 2 personas and their
   utility functions; heatmaps.
7. S. Sudhakaran et al. "MarioGPT: Open-Ended Text2Level Generation through
   Large Language Models." *NeurIPS*, 2023. https://arxiv.org/abs/2302.05981
8. A. Khalifa et al. Mario AI Framework (v0.8.0), README and
   `MarioResult.java`. https://github.com/amidos2006/Mario-AI-Framework
9. G. Todd, S. Earle, M. U. Nasir, M. C. Green, J. Togelius. "Level
   Generation Through Large Language Models." *FDG*, 2023.
   https://arxiv.org/abs/2302.05817
10. M. U. Nasir, S. James, J. Togelius. "Word2World: Generating Stories and
    Worlds through Large Language Models." 2024.
    https://arxiv.org/abs/2405.06686
11. G. Todd, A. Padula, M. Stephenson, É. Piette, D. J. N. J. Soemers,
    J. Togelius. "GAVEL: Generating Games Via Evolution and Language Models."
    *NeurIPS*, 2024. https://arxiv.org/abs/2407.09388 — Algorithm 1 (tiered
    evaluation).
12. J. Togelius, N. Shaker, S. Karakovskiy, G. N. Yannakakis. "The Mario AI
    Championship 2009–2012." *AI Magazine* 34(3), 2013.
    http://julian.togelius.com/Togelius2013The.pdf — level-generation track
    judged by human players; un-humanlike A\*. Also B. Horn et al. "A
    Comparative Evaluation of Procedural Level Generators in the Mario AI
    Framework." *FDG*, 2014. http://julian.togelius.com/Horn2014Comparative.pdf
    — leniency, linearity, density, pattern metrics.
13. A. Khalifa, P. Bontrager, S. Earle, J. Togelius. "PCGRL: Procedural
    Content Generation via Reinforcement Learning." *AIIDE*, 2020.
    https://arxiv.org/abs/2001.09212 — Zelda constraints.
14. A. Isaksen, D. Gopstein, A. Nealen. "Exploring Game Space Using Survival
    Analysis." *FDG*, 2015.
    http://www.nealen.net/papers/exploring-game-space-FDG2015.pdf
15. S. Roohi et al. "Predicting Game Difficulty and Engagement Using AI
    Players." *Proc. ACM HCI* 5 (CHI PLAY), 2021.
    https://arxiv.org/abs/2107.12061 — pass rate as difficulty; best-runs
    hypothesis (Kristensen et al.).
16. Y. Zhao et al. (Electronic Arts). "Winning Isn't Everything: Enhancing
    Game Development with Intelligent Agents." *IEEE ToG*, 2020.
    https://arxiv.org/abs/1903.10545 — skill vs style.
17. A. Liapis, G. N. Yannakakis, J. Togelius. "Towards a Generic Method of
    Evaluating Game Levels." *AIIDE*, 2013.
    http://antoniosliapis.com/papers/towards_a_generic_method_of_evaluating_game_levels.pdf
    — safety, exploration, balance; playability as connectivity; FI-2Pop.
    Tool: Sentient Sketchbook,
    https://antoniosliapis.com/projects/project_sentient_sketchbook.php
18. A. Zook, J. Spjut, J. Tremblay. "Fly, Fail, Fix: Iterative Game Repair with
    Reinforcement Learning and Large Multimodal Models." 2025.
    https://arxiv.org/abs/2507.12666
19. A. Zook, B. Harrison, M. O. Riedl. "Monte-Carlo Tree Search for
    Simulation-based Strategy Analysis." *FDG*, 2015.
    https://arxiv.org/abs/1908.01423 — MCTS rollouts as a skill proxy.
20. S. Stahlke, A. Nova, P. Mirza-Babaei. "Artificial Players in the Design
    Process: Developing an Automated Testing Tool for Game Level and World
    Design." *CHI PLAY*, 2020. https://doi.org/10.1145/3410404.3414249
    (abstract and summary only; full text not read).
21. Z. Zeng et al. "RuleSmith: Multi-Agent LLMs for Automated Game
    Balancing." 2026. https://arxiv.org/abs/2602.06232
22. É. Piette et al. "Ludii — The Ludemic General Game System." *ECAI*, 2020.
    https://arxiv.org/abs/1905.05013
23. C. Browne, F. Maire. "Evolutionary Game Design." *IEEE TCIAIG* 2(1),
    2010. https://eprints.qut.edu.au/31909/ (abstract only; full text not read).
