# Domain Layer Skeleton — v2 (complete first pass)

This is the `Domain` layer for the Project module, built from the decisions in
the "Project Domain Model — DDD Reference (v2)" document plus everything
refined since (the PostAssignment/Specialization/ProjectNumber correction,
the generic `Entity<TId>` with `IsTransient()`, and the `BatchScopedEntity`
carry-forward mechanism for creating follow-up batches). It is **not wired
into EF Core, MVC, or the database yet** — this is purely the business-rule
layer, so you can review the shape before we connect it to anything.

## Folder layout

```
DomainSkeleton/
├─ Common/                       <- shared building blocks, used by everything below
│   ├─ Entity.cs                   base class for anything with its own identity
│   ├─ BatchScopedEntity.cs        adds a reassignable BatchId — every "content" entity uses this, NOT Batch/Revision themselves
│   ├─ ValueObject.cs              base class for anything that's just "a value"
│   ├─ AggregateRoot.cs            marks Project as the one "front door" class
│   └─ ValidationResult.cs         the Error/Warning validation result
│
└─ Project/                      <- everything that lives inside the Project aggregate
    ├─ Project.cs                  THE aggregate root — start reading here
    ├─ Batch.cs                    one edit/review/approval cycle
    ├─ BatchStatus.cs              the 4 states a Batch moves through
    ├─ BatchFollowUpCreated.cs     the ONE summary event raised when a follow-up batch is created
    ├─ Revision.cs                 a "bookmark" into an Approved batch, for other divisions
    ├─ ProjectNumber.cs            the running-serial project number; OWNS its PostAssignments
    ├─ PostAssignment.cs           a Post declared under a ProjectNumber (+ optional SubOrg), OWNS its Specializations
    ├─ PoolSnapshots.cs            frozen copies of things picked from master data (incl. SubOrganizationSnapshot)
    ├─ ExamPhaseDetail.cs          Exam Phase combinations — Post/Spec Ids drawn freely from any ProjectNumber
    ├─ PenaltyAndAnswerOption.cs   extends an ExamPhaseDetail with Penalty/Answer Choices/Exam Date(s)
    ├─ DateWiseSession.cs          extends a PenaltyAndAnswerOption with Session names per date
    ├─ EducationQualification.cs   qualification/experience text per Post-Spec combination
    ├─ MultiPostCandidate.cs       which Posts a candidate may apply to together
    ├─ CandidateCount.cs           counts at Phase / Post / Specialization level (whichever the user gave)
    ├─ CoOrdinatorDetail.cs        contact person(s) for the Project
    ├─ UploadDocument.cs           uploaded document metadata, with versioning
    ├─ ExplanationNote.cs          the single free-text note (only ONE per batch, not a list)
    └─ ExamStructure/
        ├─ NodeLevel.cs              Subject / Section / SubSection
        ├─ TestStructureNode.cs      the recursive tree (replaces per-type tables + XML)
        └─ NodeConsistencyRules.cs   the cross-level Penalty conflict checker
```

**Suggested reading order**: `Common/Entity.cs` and `Common/BatchScopedEntity.cs`
first (the identity + batch-carrying mechanism everything else relies on),
then `Project/Project.cs` (the aggregate root — creation helpers, validation,
and `CreateFollowUpBatch` all live here), then whichever child file you're
most curious about. `ExamPhaseDetail.cs` is a good "template" file to read
before the others, since `PenaltyAndAnswerOption`, `EducationQualification`,
`MultiPostCandidate`, and `CandidateCount` all follow its exact shape.

## Quick glossary (plain English, matches earlier conversation)

| Term | In one sentence |
|---|---|
| **Entity** | Something with its own ID — we care *which one*, not just its values (a Batch, a Subject). `Entity<TId>` is generic (`TId` is `long` everywhere in this system); `Equals`/`GetHashCode`/`==` handle the "not-yet-saved" edge case (`IsTransient()`) so two new, unsaved entities never look falsely equal. |
| **BatchScopedEntity** | An `Entity<TId>` that also carries a reassignable `BatchId`. Every entity that holds actual Project *data* (ProjectNumber, PostAssignment, ExamPhaseDetail...) is one of these — `Batch` and `Revision` themselves are NOT, since they ARE the fixed ledger and must never be repointed. |
| **Value Object** | Something with *no* ID — two with the same values are the same thing (a Penalty amount, a frozen master-data snapshot). |
| **Aggregate Root** | The one class (`Project`) the rest of the app is allowed to load/save directly; everything inside it is reached only through it. |
| **Domain Event** | A record of "something meaningful happened" (e.g. a follow-up batch being created), that other code can react to later without `Project` knowing about emails/audit tables. |
| **Invariant** | A rule that must always be true (e.g. "if a Penalty exists at two levels, it must match" — enforced in code, not just on a form). |
| **Severity split (Error vs Warning)** | Some problems block the workflow (Error), some are just advisory prompts (Warning). |
| **Carry-forward** | How a new Batch inherits an Approved batch's content: existing rows get their `BatchId` REPOINTED to the new batch (their own Id never changes), instead of being cloned with new Ids. See `Project.CreateFollowUpBatch()`. |

## What's in this pass

Every tab from the original 12-tab wizard now has a matching Domain entity,
all following the same `ExamPhaseDetail`-style pattern: a `BatchScopedEntity<long>`,
a private constructor + static `Create(...)` factory, a `ValidateReferencesExistIn(...)`
method feeding into the Option 3 referential-integrity check, and — for the ones
that reference PostAssignment/Specialization/ExamPhaseDetail Ids — the same
"free mix, unscoped by ProjectNumber" rule confirmed earlier in the conversation.

`Project.cs` wires all of them together: one `AddX(...)` creation helper per
entity (each automatically calls `AssignBatch(LatestBatch.Id)` so the caller
never has to think about BatchId), a `ValidateCurrentState()` that runs every
entity's referential check plus the Exam Structure's Penalty consistency
check, and `CreateFollowUpBatch(...)` which walks every collection and
carries non-deleted rows forward onto a new Batch.

## Still not included

- `IProjectRepository` (the interface an Infrastructure project would
  implement with actual EF Core/SQL code) and the Application Service layer
  that would sit between a Controller and `Project`. Both come once you've
  reviewed this and we're ready to move into Infrastructure/DB schema.
- Unit tests exercising the scenarios discussed (SubOrgA/PostB vs SubOrgB/PostB,
  orphaned references, the Penalty Warning, the full Batch state machine,
  and the carry-forward mechanism itself) — worth doing before or alongside
  schema design, per the earlier discussion on sequencing.

## Questions for you while reviewing

1. Does the shape of the eight new entities match what you expected, or does
   anything (e.g. `PenaltyAndAnswerOption` pointing at `ExamPhaseDetail`
   rather than holding its own Post/Spec Ids) need correcting?
2. Ready to move to unit tests next, or straight into the DB schema now that
   every entity exists?