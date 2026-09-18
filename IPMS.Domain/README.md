# Domain Layer Skeleton — v1 (for review)

This is a first-pass skeleton of the `Domain` layer for the Project module,
built from the decisions in the "Project Domain Model — DDD Reference (v2)"
document. It is **not wired into EF Core, MVC, or the database yet** — this is
purely the business-rule layer, so you can review the shape before we connect
it to anything.

## Folder layout

```
DomainSkeleton/
├─ Common/                     <- shared building blocks, used by everything below
│   ├─ Entity.cs                 base class for anything with its own identity
│   ├─ ValueObject.cs             base class for anything that's just "a value"
│   ├─ AggregateRoot.cs           marks Project as the one "front door" class
│   └─ ValidationResult.cs        the Error/Warning validation result
│
└─ Project/                    <- everything that lives inside the Project aggregate
    ├─ Project.cs                 THE aggregate root — start reading here
    ├─ Batch.cs                   one edit/review/approval cycle
    ├─ BatchStatus.cs             the 4 states a Batch moves through
    ├─ Revision.cs                a "bookmark" into an Approved batch, for other divisions
    ├─ PostSpecialization.cs      real-ID version of the old combo-text field
    ├─ PoolSnapshots.cs           frozen copies of things picked from master data
    ├─ ProjectNumber.cs           the running-serial project number
    └─ ExamStructure/
        ├─ NodeLevel.cs           Subject / Section / SubSection
        ├─ TestStructureNode.cs   the recursive tree (replaces per-type tables + XML)
        └─ NodeConsistencyRules.cs   the cross-level Penalty conflict checker
```

**Suggested reading order**: `Common/Entity.cs` and `Common/ValueObject.cs` first
(the two core ideas everything else builds on), then `Project/Project.cs`
(the aggregate root, ties everything together), then whichever child file
you're most curious about.

## Quick glossary (plain English, matches earlier conversation)

| Term | In one sentence |
|---|---|
| **Entity** | Something with its own ID — we care *which one*, not just its values (a Batch, a Subject). |
| **Value Object** | Something with *no* ID — two with the same values are the same thing (a Penalty amount, a frozen master-data snapshot). |
| **Aggregate Root** | The one class (`Project`) the rest of the app is allowed to load/save directly; everything inside it is reached only through it. |
| **Domain Event** | A record of "something meaningful happened" (e.g. Approved), that other code can react to later without `Project` knowing about emails/audit tables. |
| **Invariant** | A rule that must always be true (e.g. "if a Penalty exists at two levels, it must match" — enforced in code, not just on a form). |
| **Value/Severity split (Error vs Warning)** | New in this design — some problems block the workflow (Error), some are just advisory prompts (Warning). |

## What's intentionally NOT in this first pass

To keep this reviewable, the following are described in the reference doc but
not yet written as code — they all follow the *exact same simple pattern* as
`PostSpecialization.cs` (a plain `Entity<long>` holding a list of
`PostSpecializationIds` it refers to), so there's no new pattern to review,
just more of the same:

- ExamPhaseDetail, PenaltyAndAnswerOption, DateWiseSession,
  MultiPostCandidate, CandidateCount, CoOrdinatorDetail, UploadDocument,
  EducationQualification, ExplanationNote

Also not yet included: `IProjectRepository` (the interface an Infrastructure
project would implement with actual EF Core/SQL code), and the Application
Service layer that would sit between a Controller and `Project`. Both come
after you've had a chance to look at this and we agree the shape is right.

## Questions for you while reviewing

1. Does the split between `Common/` (shared base classes) and `Project/`
   (everything specific to this one aggregate) make sense as a starting
   folder structure, or would you rather see it organized differently?
2. Is the amount of comment-per-class about right, or too much/too little
   once you're looking at real code instead of chat messages?
3. Once you've looked this over, should I fill in the remaining "same
   pattern" entities (ExamPhaseDetail etc.) before we move to the DB schema,
   or move to the DB schema now and treat those as a mechanical follow-up?
