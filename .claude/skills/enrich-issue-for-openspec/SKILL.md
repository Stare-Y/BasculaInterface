---
name: enrich-issue-for-openspec
description: Read a GitHub issue plus repository context, then produce an enriched issue or issue draft that is ready to feed into OpenSpec proposal generation.
license: MIT
metadata:
  author: BasculaInterface
  version: "1.0"
---

Enrich an existing issue so it becomes a reliable handoff into `openspec-propose`.

Use this when a GitHub issue already exists but still needs repository-aware requirements, clearer scope boundaries, or explicit unknowns before OpenSpec proposal generation.

This skill is for planning only. It may inspect code, issues, and change artifacts, but it must NOT implement application code.

---

## Input

The user may provide:

- an issue number or URL
- issue text copied into chat
- a request like "enrich this requirement before OpenSpec"

If the issue target is ambiguous, ask the user to identify which issue to refine.

---

## Steps

1. **Load the issue context**

   Read the issue if issue tooling is available. Otherwise, use the issue text supplied by the user.

   Extract:
   - current problem statement
   - desired outcome
   - explicit constraints
   - missing business decisions
   - any suggested affected areas

2. **Inspect repository context**

   Read only the code and planning artifacts needed to understand the issue's likely backend impact.

   Good targets include:
   - controllers
   - services
   - interfaces
   - related OpenSpec artifacts
   - nearby domain or policy code

   The goal is to identify relevant backend surfaces, assumptions, integration points, and likely constraints.

3. **Separate confirmed requirements from inferred findings**

   Build two mental buckets:
   - **Confirmed requirements**: explicitly stated by the user or issue
   - **Inferred assumptions / recommendations**: likely conclusions derived from repository context

   Never blend them together.

4. **Ask about critical business decisions only**

   Use the **AskUserQuestion tool** only when an unresolved decision materially changes scope, policy, acceptance criteria, or the proposal boundary.

   Examples:
   - auth rule differences
   - audit expectations
   - scope boundaries across services or endpoints
   - rollout target for the first controller or service

5. **Produce enriched issue content**

   Build an enriched issue body or structured issue comment with these sections:
   - **Summary**
   - **Problem**
   - **Desired Outcome**
   - **Confirmed Requirements**
   - **Affected Backend Areas**
   - **Integration Points / Dependencies**
   - **Constraints**
   - **Inferred Assumptions / Recommendations**
   - **Out of Scope**
   - **Open Questions**
   - **Initial Acceptance Criteria**
   - **Suggested OpenSpec Change Name**

   Rules:
   - Keep confirmed requirements separate from inferred findings.
   - Make scope boundaries explicit.
   - Preserve unresolved business decisions instead of guessing.
   - Prefer concrete repository references when they materially improve clarity.

6. **Update the issue or produce an enrichment draft**

   If GitHub issue write tooling is available, update the issue body or add a structured comment with the enriched content.

   If issue write tooling is not available, produce:
   - a replacement issue body or structured comment draft
   - a short note describing how it should be applied manually

   Make it explicit whether the issue was updated or drafted only.

7. **Hand off to OpenSpec**

   Stop once the issue is proposal-ready.

   Recommend:
   - `openspec-propose` when the issue now defines a bounded change
   - further exploration only if major uncertainty remains

---

## Output

Your result should clearly show:

- which issue was enriched
- whether it was updated or drafted
- the enriched issue content
- remaining open questions, if any
- the suggested OpenSpec change name
- whether the next step is `openspec-propose`

---

## Guardrails

- Do not implement code.
- Do not present inferred repository findings as confirmed business requirements.
- Do not do deep implementation design that belongs in proposal or design artifacts.
- Keep the workflow backend-focused unless the user explicitly expands scope.
- Stop once the issue is mature enough to hand off into OpenSpec.
