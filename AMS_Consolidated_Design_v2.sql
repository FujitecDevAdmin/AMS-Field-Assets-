/*
    ============================================================================
    AssetManagement (AMS) - CONSOLIDATED MODULE-WISE DATABASE DESIGN
    REVISION 2
    ============================================================================
    One script for the whole target database: everything the application has
    today, plus every table and column needed to close the gaps found against
    the Asset Management and Ticket Raising functional handbook (v1.0,
    05 August 2026).

    REVISION 2 (12 August 2026) - fixes applied after design review
    ---------------------------------------------------------------------------
    BLOCKER
      R2-1  SQL Server forbids rowversion columns on system-versioned tables.
            [RowVersion] is REMOVED from the five temporal tables (Employee,
            Asset, Contract, SlaPolicy, LocationOperationalHour). Those tables
            use [SysStartTime] as the optimistic-concurrency token instead -
            it is regenerated on every UPDATE. See design rule 7.
    BUSINESS CORRECTNESS
      R2-2  ContractReminderLog: added [ExpiryDateSnapshot] and widened the
            once-only unique key to (ContractId, DaysBeforeExpiry,
            ExpiryDateSnapshot) - a renewed contract gets its reminders again.
      R2-3  ContractReminderLog + SlaEscalationLog: the once-only unique
            indexes now EXCLUDE Outcome = 'Failed', so a failed send can be
            retried instead of being blocked forever.
      R2-4  RequestEmail: [SentByUserId] is now nullable with a CHECK - an
            Inbound e-mail has no sending user. For Inbound rows,
            [QueuedOnUtc] records the time the message was received.
      R2-5  Section 17.1 now seeds the FULL baseline AssetStatus set. The
            previous revision seeded only the two standby statuses, so a
            fresh database could not create its first asset.
    CONSTRAINT / FK COMPLETENESS
      R2-6  Intra-schema FKs added: RoleCapability -> Capability,
            UserCapabilityOverride -> Capability, ServiceRequest ->
            RequestCategory / RequestSubCategory, RequestHistory ->
            RequestEmail (RequestEmail is now created BEFORE RequestHistory).
      R2-7  AssetMovement: CHECKs added for [MovementType] and [Status]
            (they were comments only).
      R2-8  AssetHandover: CHECKs added tying Status = 'Cancelled' to
            [CancelledOnUtc] and Status = 'ReceivedAtHo' to [IsReceivedByHo],
            so the filtered unique indexes cannot be dodged by partial writes.
      R2-9  LocationOperationalHour: window and break CHECKs are relaxed when
            [IsRoundTheClock] = 1 (a 24h branch no longer needs fake times).
      R2-10 HolidayCalendar: CHECK that [HolidayYear] = YEAR([HolidayDate]),
            and the recurrence day is validated per month (no 30 February).
            A 29 February recurrence is observed on 28 February in non-leap
            years - an application rule, documented at the table.
      R2-11 Approval extension: CK_RequestApprovalStep_Activation now permits
            Cancelled/Skipped steps that were never activated (an instance
            cancelled at stage 1 must be able to cancel its Waiting steps).
      R2-12 Approval extension: ApprovalNotificationLog's FK to
            RequestApprovalInstance changed CASCADE -> NO ACTION. Evidence
            rows (notification log, decisions) deliberately BLOCK deletion of
            an approval run: approval history is never deleted.
      R2-13 Approval extension: one-active-default filtered unique index on
            ApprovalWorkflowDefinition (two live defaults meant the submission
            path picked whichever sorted first).
    REVISION 2 ADDENDUM (12 August 2026) - second design review
      R2-18 ServiceRequest.RequestKind had no CHECK - the allowed values lived
            in a trailing comment only, exactly the defect R2-7 fixed on
            AssetMovement. CK_ServiceRequest_Kind now enforces
            SupportTicket|AssetIssue|NewService, matching the CHECK that
            ServiceTemplate.RequestKind already carried. This is the column
            the whole approval extension keys off (RequestKind='NewService'),
            so an unconstrained typo silently routed a request past approval.
      R2-19 Section 18's table count ran BEFORE the approval extension and
            reported 79/84 for a script that goes on to create 8 more tables.
            Section 18 now states it measures the base design, and the
            extension carries its own count of the finished database.
      R2-20 PhysicalVerification.WorkingCondition had no CHECK, the same
            defect as R2-18. It now reuses the AssetHandover.ReturnCondition
            vocabulary (Good|MinorDamage|Damaged|NotWorking|Missing) rather
            than inventing a second one: an asset judged 'Damaged' on a
            return and 'Damaged' on an audit should read as the same word,
            and 'Missing' is exactly what an audit needs to be able to say.
            If the business wants a distinct audit vocabulary, change it
            here first - the mobile app mirrors this list, it does not
            define it.
      R2-22 The five system-versioned tables gain [ConcurrencyStamp]
            uniqueidentifier NOT NULL DEFAULT NEWID(), and THAT is the
            optimistic-concurrency token. R2-1 nominated [SysStartTime] on the
            premise that it is regenerated on every UPDATE. Measured against
            SQL Server 2022, it is not: the period start is stamped from the
            TRANSACTION start time and the system clock advances in ~1-15ms
            ticks, so two updates inside one tick receive the same value.
            20 of 20 insert-then-update pairs left it unchanged; a 50ms delay
            changed it every time.
            The consequence was a SILENT lost update - the second writer's
            stale token still matched, their UPDATE affected one row, and no
            error was raised anywhere. Zero-duration versions are not kept
            either, so those edits left no history row to appeal to.
            [SysStartTime] goes back to doing only what it is for: history.
            The stamp is re-generated by the audit interceptor on every update,
            so the token changes because the application changed the row, not
            because the clock happened to move.
            Evidence: src/Backend/tests/AMS.PersistenceGates.Tests/GateA_TemporalConcurrency.cs
      R2-26 CustomFieldDefinition.FieldType had no CHECK - the allowed values
            lived in a trailing comment only, the third instance of the defect
            R2-7 first fixed (see also R2-18, R2-20). The column decides how
            every value of that field is read: which of [Value], [ValueNumber],
            [ValueDate] and [OptionId] carries the answer. A typo would produce
            a field the write path stores in one column and the read path looks
            for in another, and the asset would simply appear to have no value.
            CK_CustomFieldDefinition_Type now enforces
            Text|Number|Percentage|Date|Boolean|Dropdown.
      R2-21 PhysicalVerification gains [ClientCaptureId] and a filtered
            unique index. The audit is captured offline and retried, so the
            server must be able to tell a phone resending its own capture
            from a second technician verifying the same asset. Previously
            only UX_PhysicalVerification_OnePerAssetPerCycle existed, which
            reports both as the same conflict. See Sec 10 and doc 05.

    HYGIENE
      R2-14 All remaining [RowVersion] columns are declared NOT NULL (the
            value is always generated; NULL stated a falsehood).
      R2-15 IX_UserRecoveryCode_UserUnused is now a filtered index.
      R2-16 ApprovalStageApproverRule gains [RowVersion] (it is editable).
      R2-17 Doc corrections: module map now shows Audit = 2 tables and
            TOTAL = 79 (+5 temporal history tables = 84 rows in sys.tables);
            Audit.ScheduledFieldChange added to the WHAT-IS-NEW list and the
            Section 18 missing-table check; "Sec 18.7" -> "Sec 17.7";
            ScheduledFieldChange's comment now points at the inline temporal
            declarations; the ticket-number comment states that the sequence
            is global and does not reset each year.

    REVISION 3 - THE ASSET MODEL IS WIDER THAN OFFICE IT   *** APPLIED ***
    ---------------------------------------------------------------------------
    Why: the live fixed asset register holds 7,413 assets and IT is 1,834 of
    them - 24%. Furniture & Fixtures alone is 2,181. Revision 2 had no
    representation of depreciation, book value, asset quantity, disposal,
    chart-of-accounts codes or insurance policies, all of which that register
    carries; and it had one taxonomy where the business runs three (Asset
    Category 9, Asset Class 13, TechnicalGroup 342).

    Design: docs/07ASSETREGISTERDESIGN.md. Decisions: docs/00DESIGNDECISIONS.md.

      R3-1  [Assets] 10 -> 18 tables. AssetCategory renamed AssetType and given
            seven behaviour flags, so what a type CAN DO is data rather than a
            hardcoded list of IT categories. New: AssetClass and ChartOfAccount
            (the finance taxonomy), AssetFinance and AssetDepreciationEntry (a
            read-only MIRROR of SAP - see section 8 of the design doc),
            AssetHolding (per-place balances for bulk lines), AssetDisposal,
            AssetVehicleDetail, AssetInstrumentDetail.
            [Asset] gains AssetClassId, Make, Model, IsBulk, Quantity,
            UnitOfMeasure, CapitalisedFromAssetId, SplitFromAssetId; loses
            Hostname (moved to AssetHardwareDetail) and the calibration window
            (moved to AssetInstrumentDetail, which 221 rows use and 7,192
            do not).
            CK_Asset_UnitQuantityIsOne is the load-bearing one: it makes
            "every allocatable asset has Quantity = 1" a database PROOF, so
            allocation, handover and verification keep working unchanged.

      R3-2  [Asset].[ImportBatchId]. FieldAsset carried row-level import
            lineage and the unified register had none, so folding the module
            in without this column would have LOST the only per-row link to
            [DataImport] the design had. A deviation from section 7 of the
            design doc, which mapped 13 of FieldAsset's 14 columns.

      R3-3  Section 16 [FieldAssets] DELETED; the module folds into the one
            register. The reviewed design's own argument against a separate
            FieldAssetAdmins login table - "a second identity store is how the
            old app ended up with two password policies" - applies verbatim to
            a second register. The field-asset.* capabilities survive as a
            scoped VIEW of the one register instead of a gate on a second one.
            Fold-ins: CustomerSite.CustomerName, AssetSiteMapping.
            CommissionedDate, AssetMovement.Quantity, PhysicalVerification's
            bulk-count columns and its split unique indexes, Insurance in the
            contract-type vocabulary, AssetEvent.QuantityDelta/DisposalId.

    Section 16 is left VACANT rather than renumbering 17 and 18: every "Sec 17
    seeds it" reference in this file and in the docs would otherwise have to
    move, and a stale section number is a worse defect than a numbering gap.

    Sections 1, 2 and 4-15 are otherwise unchanged.

    HOW TO RUN
        sqlcmd -S <server> -d AssetManagement -i AMS_Consolidated_Design.sql

        Run it against an empty database. Every column a table needs is in that
        table's own CREATE TABLE - there is not one ALTER TABLE in this file,
        because this application has not shipped and there is no database in the
        field to migrate. A design that describes itself half in a definition
        and half in a later patch is a design nobody can read in one pass.

        Statements are still guarded on existence, so the script is safe to
        re-run while the schema is being iterated during development.

    RELATIONSHIP TO THE OTHER SCHEMA FILES
        AssetManagement_Schema_v2.sql  the older column/constraint reference
        AMS_Consolidated_Design.sql    THIS FILE - the agreed TARGET design

        The EF model stays the source of truth. Nothing here is live until the
        matching entity configuration and migration exist; this file is the
        design those migrations must produce.

    DESIGN RULES HELD THROUGHOUT (docs/ARCHITECTURE.md, ams-standards)
        1.  One schema per code module. A table lives in exactly one module.
        2.  NO foreign keys across schemas. Cross-module links hold the id only,
            with no FK and no navigation property, so a module can be deployed,
            tested and reasoned about alone. Every FK below is intra-schema.
        3.  Singular PascalCase table names, PK is always [Id], FK is [<Entity>Id].
        4.  Every datetime2 is UTC and named <Event>OnUtc. Wall-clock times that
            are genuinely local (a branch opening at 09:00) use time(0) plus the
            location's [TimeZoneId] - never a UTC timestamp.
        5.  Money and percentages are decimal, never float.
        6.  A business rule that must survive two concurrent users lives in the
            database as a filtered unique index, not a read-then-write check.
        7.  rowversion on every table a user can edit concurrently - EXCEPT the
            system-versioned tables, where SQL Server forbids rowversion
            columns. Those tables use [SysStartTime] as the concurrency token:
            it changes on every UPDATE, and EF Core can map it as a
            concurrency token even though it is HIDDEN.                  -- R2-1
        8.  Booleans are Is/Has prefixed; soft delete is [IsDeleted].

    MODULE MAP
    ---------------------------------------------------------------------------
    Sec  Schema          Module responsibility                    Tables   New
    ---------------------------------------------------------------------------
     1   Identity        Login, roles, capabilities, MFA             8       -
     2   Organization    Locations, regions, departments,            7       1
                         vendors, employees, applications
     3   Assets          Register, classes, finance mirror,         18       8   -- R3
                         depreciation schedule, holdings, disposal,
                         detail tables, custom fields, timeline
     4   Allocations     Employee allocation, acknowledgement,       8       2
                         branch handover, return reversal
     5   Movements       Shipments, despatch batches, GRN            2       1
     6   Transfers       Employee/dept/branch/cost-centre moves      1       -
     7   ServiceDesk     Tickets, categories, teams, templates,     12       4
                         notes, e-mail, attachments, SLA state
     8   ServiceLevel    SLA policies, escalation, operational       8       8   (new module)
                         calendar, holidays
     9   Contracts       Contracts, covered assets, documents,       5       1
                         reminder configuration and evidence
    10   Verification    Physical verification cycles (mobile)       2       -
    11   Discovery       Agent inventory, health, software           6       -
    12   SapSync         S/4HANA sync logs and watermarks            2       -
    13   Notifications   In-app notifications, SMTP, outbox          3       -
    14   Audit           Field-level change audit, scheduled         2       1   -- R2-17
                         future-dated changes
    15   DataImport      Excel import batches and row errors         2       2   (new module)
    ---------------------------------------------------------------------------
                                                           TOTAL     86      28  -- R3
    (System versioning adds 5 history tables - EmployeeHistory, AssetHistory,
     ContractHistory, SlaPolicyHistory, LocationOperationalHourHistory - so
     sys.tables shows 91 rows for the base design. The approval-workflow
     extension at the end of this file adds 8 more ServiceDesk tables.)

    WHAT IS NEW, AND WHICH HANDBOOK GAP IT CLOSES
        Sec 2   Organization.Region + Location.RegionId/TimeZoneId ....... gap 11
        Sec 4   Allocations.AssetHandover ............................ gaps 18,20
                Allocations.AllocationReturnReversal ..................... gap 25
                AssetReturnImage.HandoverId + upload metadata ............ gap 21
        Sec 5   Movements.MovementBatch .............................. gaps 22,23
                AssetMovement.ReceiptRemarks (GRN) ....................... gap 24
        Sec 7   ServiceDesk.SupportTeam / SupportTeamMember .............. gap 15
                ServiceDesk.ServiceTemplate .............................. gap 17
                ServiceDesk.RequestEmail ................................. gap 13
                RequestHistory.EntryKind (notes on one timeline) ......... gap 14
                RequestAttachment upload metadata ........................ gap 12
                ServiceRequest SLA + intake-scheduling columns ....... gaps 2,9,16
        Sec 8   ServiceLevel.SlaPolicy / SlaEscalation ................. gaps 1,5
                ServiceLevel.SlaEscalationLog ............................. gap 5
                ServiceLevel.LocationOperationalHour / Day / SaturdayRule . gap 7
                ServiceLevel.HolidayCalendar / HolidayLocation ............ gap 8
                (clock arithmetic, pause/resume, overdue: gaps 3,4,6,10 are
                 computed by the SLA service over these tables + Sec 7 columns)
        Sec 9   Contracts.ContractReminderSetting ........................ gap 29
                ContractDocument upload metadata ......................... gap 30
                ContractReminderLog.EmailOutboxId (evidence) ............. gap 29
        Sec 14  Audit.ScheduledFieldChange (future-dated changes) . design addition  -- R2-17
        Sec 15  DataImport.ImportBatch / ImportError ................. gaps 26-28

    Three handbook constructs are deliberately NOT reproduced:
        FieldAssetAdmins as a separate login table - a second identity store is
        how the old app ended up with two password policies. Field asset access
        is a capability set on Identity.User instead (Sec 17 seeds it).
        A separate field asset register - the same argument, one level up. R3
        folds it into [Assets], and field-asset.* became a scoped view of the
        one register rather than a gate on a second one.
        Per-entity import tables - one DataImport module serves assets,
        employees, field assets and the fixed asset register.
*/
/*  Filtered unique indexes are the backbone of this design and SQL Server
    refuses to create one unless QUOTED_IDENTIFIER is ON. sqlcmd connects with
    it OFF, so the script sets it itself rather than depending on the caller
    remembering the -I switch. */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* ===========================================================================
   SECTION 0 - SCHEMAS AND SEQUENCES
   =========================================================================== */
IF SCHEMA_ID(N'Identity')      IS NULL EXEC(N'CREATE SCHEMA [Identity];');
IF SCHEMA_ID(N'Organization')  IS NULL EXEC(N'CREATE SCHEMA [Organization];');
IF SCHEMA_ID(N'Assets')        IS NULL EXEC(N'CREATE SCHEMA [Assets];');
IF SCHEMA_ID(N'Allocations')   IS NULL EXEC(N'CREATE SCHEMA [Allocations];');
IF SCHEMA_ID(N'Movements')     IS NULL EXEC(N'CREATE SCHEMA [Movements];');
IF SCHEMA_ID(N'Transfers')     IS NULL EXEC(N'CREATE SCHEMA [Transfers];');
IF SCHEMA_ID(N'ServiceDesk')   IS NULL EXEC(N'CREATE SCHEMA [ServiceDesk];');
IF SCHEMA_ID(N'ServiceLevel')  IS NULL EXEC(N'CREATE SCHEMA [ServiceLevel];');   -- NEW
IF SCHEMA_ID(N'Contracts')     IS NULL EXEC(N'CREATE SCHEMA [Contracts];');
IF SCHEMA_ID(N'Verification')  IS NULL EXEC(N'CREATE SCHEMA [Verification];');
IF SCHEMA_ID(N'Discovery')     IS NULL EXEC(N'CREATE SCHEMA [Discovery];');
IF SCHEMA_ID(N'SapSync')       IS NULL EXEC(N'CREATE SCHEMA [SapSync];');
IF SCHEMA_ID(N'Notifications') IS NULL EXEC(N'CREATE SCHEMA [Notifications];');
IF SCHEMA_ID(N'Audit')         IS NULL EXEC(N'CREATE SCHEMA [Audit];');
IF SCHEMA_ID(N'DataImport')    IS NULL EXEC(N'CREATE SCHEMA [DataImport];');     -- NEW
GO
/*  Numbers people quote on the phone come from a sequence, never from
    COUNT(*) + 1 - two intakes in the same second must not share a number.
    R2-17: the sequence is GLOBAL and never resets. A ticket number like
    TKT-2027-000481 simply continues where 2026 left off; the year in the
    printed number is the intake year, not a numbering partition. */
IF OBJECT_ID(N'[ServiceDesk].[RequestNumberSequence]', N'SO') IS NULL
    CREATE SEQUENCE [ServiceDesk].[RequestNumberSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
IF OBJECT_ID(N'[Movements].[MovementBatchNumberSequence]', N'SO') IS NULL          -- NEW
    CREATE SEQUENCE [Movements].[MovementBatchNumberSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
IF OBJECT_ID(N'[DataImport].[ImportBatchNumberSequence]', N'SO') IS NULL           -- NEW
    CREATE SEQUENCE [DataImport].[ImportBatchNumberSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
GO
/* ===========================================================================
   SECTION 1 - [Identity]  Login, roles, capabilities, MFA
   ---------------------------------------------------------------------------
   Authorisation is by CAPABILITY, never by role name. A user holds many roles
   and an administrator can move a capability between roles at runtime; code
   that tests role = 'BranchAdmin' silently ignores both facts.
   =========================================================================== */
IF OBJECT_ID(N'[Identity].[Capability]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[Capability] (
        [Name]        nvarchar(80)  NOT NULL,
        [Module]      nvarchar(60)  NOT NULL,
        [Description] nvarchar(300) NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_Capability] PRIMARY KEY ([Name])
    );
END
GO
IF OBJECT_ID(N'[Identity].[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[Role] (
        [Id]            int           NOT NULL IDENTITY,
        [RoleName]      nvarchar(80)  NOT NULL,
        [Description]   nvarchar(300) NULL,
        [IsSystemRole]  bit           NOT NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_Role] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Identity].[User]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[User] (
        [Id]                    int            NOT NULL IDENTITY,
        [Username]              nvarchar(100)  NOT NULL,
        [DisplayName]           nvarchar(150)  NOT NULL,
        [PasswordHash]          nvarchar(500)  NOT NULL,
        [Email]                 nvarchar(256)  NULL,
        [EmployeeId]            int            NULL,   -- Organization.Employee, id only
        [MustChangePassword]    bit            NOT NULL,
        [IsLocked]              bit            NOT NULL,
        [FailedLoginAttempts]   int            NOT NULL,
        [LastLoginOnUtc]        datetime2      NULL,
        [HasAllBranches]        bit            NOT NULL,
        [IsActive]              bit            NOT NULL,
        [MfaEnabled]            bit            NOT NULL,
        [MfaSecretEncrypted]    varbinary(max) NULL,    -- data protection, purpose AMS.Identity.MfaSecret
        [MfaEnrolledOnUtc]      datetime2      NULL,
        [MfaEnrollmentRequired] bit            NOT NULL,
        [CreatedOnUtc]          datetime2      NOT NULL,
        [CreatedBy]             nvarchar(100)  NULL,
        [ModifiedOnUtc]         datetime2      NULL,
        [ModifiedBy]            nvarchar(100)  NULL,
        [RowVersion]            rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_User] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Identity].[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[UserRole] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        [GrantedOnUtc]          datetime2     NOT NULL,   -- A
        [GrantedBy]             nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRole_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRole_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Identity].[RoleCapability]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[RoleCapability] (
        [RoleId]         int          NOT NULL,
        [CapabilityName] nvarchar(80) NOT NULL,
        [GrantedOnUtc]          datetime2     NOT NULL,   -- A
        [GrantedBy]             nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_RoleCapability] PRIMARY KEY ([RoleId], [CapabilityName]),
        CONSTRAINT [FK_RoleCapability_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE CASCADE,
        /*  R2-6: intra-schema link, so it gets a real FK per design rule 2.
            Deleting a capability removes its grants - retiring a capability
            is a code-level decision and the grants are meaningless without it. */
        CONSTRAINT [FK_RoleCapability_Capability_CapabilityName] FOREIGN KEY ([CapabilityName]) REFERENCES [Identity].[Capability] ([Name]) ON DELETE CASCADE
    );
END
GO
/*  A per-user grant or deny that beats the role union in both directions.
    IsGranted = 0 is a deny and must win, so one capability can be taken away
    without unpicking somebody's roles. */
IF OBJECT_ID(N'[Identity].[UserCapabilityOverride]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[UserCapabilityOverride] (
        [UserId]         int           NOT NULL,
        [CapabilityName] nvarchar(80)  NOT NULL,
        [IsGranted]      bit           NOT NULL,
        [Reason]         nvarchar(300) NULL,
        [GrantedOnUtc]          datetime2     NOT NULL,   -- A
        [GrantedBy]             nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_UserCapabilityOverride] PRIMARY KEY ([UserId], [CapabilityName]),
        CONSTRAINT [FK_UserCapabilityOverride_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserCapabilityOverride_Capability_CapabilityName] FOREIGN KEY ([CapabilityName]) REFERENCES [Identity].[Capability] ([Name]) ON DELETE CASCADE   -- R2-6
    );
END
GO
IF OBJECT_ID(N'[Identity].[UserBranch]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[UserBranch] (
        [UserId]     int NOT NULL,
        [BranchId]   int NOT NULL,   -- Organization.Branch, id only
        [IsPrimary]  bit NOT NULL,
        [GrantedOnUtc]          datetime2     NOT NULL,   -- A
        [GrantedBy]             nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_UserBranch] PRIMARY KEY ([UserId], [BranchId]),
        CONSTRAINT [FK_UserBranch_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Identity].[UserRecoveryCode]', N'U') IS NULL
BEGIN
    CREATE TABLE [Identity].[UserRecoveryCode] (
        [Id]           bigint        NOT NULL IDENTITY,
        [UserId]       int           NOT NULL,
        [CodeHash]     nvarchar(500) NOT NULL,   -- hashed like a password; never stored in clear
        [CreatedOnUtc] datetime2     NOT NULL,
        [UsedOnUtc]    datetime2     NULL,       -- single use
        CONSTRAINT [PK_UserRecoveryCode] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRecoveryCode_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[Role]'), N'UX_Role_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Role_Name] ON [Identity].[Role] ([RoleName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[User]'), N'UX_User_Username', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_User_Username] ON [Identity].[User] ([Username]);
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[User]'), N'UX_User_Employee', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_User_Employee] ON [Identity].[User] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[UserRole]'), N'IX_UserRole_RoleId', N'IndexID') IS NULL
    CREATE INDEX [IX_UserRole_RoleId] ON [Identity].[UserRole] ([RoleId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[RoleCapability]'), N'IX_RoleCapability_CapabilityName', N'IndexID') IS NULL
    CREATE INDEX [IX_RoleCapability_CapabilityName] ON [Identity].[RoleCapability] ([CapabilityName]);   -- R2-6 FK support
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[UserCapabilityOverride]'), N'IX_UserCapabilityOverride_CapabilityName', N'IndexID') IS NULL
    CREATE INDEX [IX_UserCapabilityOverride_CapabilityName] ON [Identity].[UserCapabilityOverride] ([CapabilityName]);   -- R2-6 FK support
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[UserBranch]'), N'UX_UserBranch_OnePrimary', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_UserBranch_OnePrimary] ON [Identity].[UserBranch] ([UserId]) WHERE [IsPrimary] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[Identity].[UserRecoveryCode]'), N'IX_UserRecoveryCode_UserUnused', N'IndexID') IS NULL
    CREATE INDEX [IX_UserRecoveryCode_UserUnused] ON [Identity].[UserRecoveryCode] ([UserId]) WHERE [UsedOnUtc] IS NULL;   -- R2-15
GO
/* ===========================================================================
   SECTION 2 - [Organization]  Locations, regions, departments, vendors,
                               employees, application access
   ---------------------------------------------------------------------------
   NEW: [Region], and [Location].[RegionId] / [TimeZoneId].
   The handbook routes tickets by matching location NAMES against a hard-coded
   southern list, with "everything else is North" as the fallback - so a new
   branch silently lands in the wrong queue on the day it opens. Region is a
   master row and a foreign key instead.
   TimeZoneId exists because operational hours are local wall-clock times: a
   branch opens at 09:00 where it stands, not at 09:00 UTC.
   =========================================================================== */
IF OBJECT_ID(N'[Organization].[Region]', N'U') IS NULL                              -- NEW
BEGIN
    CREATE TABLE [Organization].[Region] (
        [Id]            int           NOT NULL IDENTITY,
        [RegionName]    nvarchar(60)  NOT NULL,
        [Description]   nvarchar(300) NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_Region] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Organization].[Branch]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Branch] (
        [Id]            int           NOT NULL IDENTITY,
        [BranchCode]    nvarchar(20)  NOT NULL,
        [BranchName]    nvarchar(100) NOT NULL,
        [RegionId]      int           NULL,                                         -- NEW
        [Latitude]      decimal(9,6)  NULL,
        [Longitude]     decimal(9,6)  NULL,
        [TimeZoneId]    nvarchar(64)  NOT NULL CONSTRAINT [DF_Branch_TimeZoneId]  -- NEW
                                      DEFAULT (N'India Standard Time'),
        [IsHeadOffice]  bit           NOT NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_Branch] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Branch_Latitude] CHECK ([Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)),
        CONSTRAINT [CK_Branch_Longitude] CHECK ([Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)),
        CONSTRAINT [FK_Branch_Region_RegionId] FOREIGN KEY ([RegionId])
            REFERENCES [Organization].[Region] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[Organization].[Department]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Department] (
        [Id]             int           NOT NULL IDENTITY,
        [DepartmentName] nvarchar(100) NOT NULL,
        [IsActive]       bit           NOT NULL,
        [CreatedOnUtc]   datetime2     NOT NULL,
        [CreatedBy]      nvarchar(100) NULL,
        [ModifiedOnUtc]  datetime2     NULL,
        [ModifiedBy]     nvarchar(100) NULL,
        CONSTRAINT [PK_Department] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Organization].[Vendor]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Vendor] (
        [Id]            int           NOT NULL IDENTITY,
        [VendorName]    nvarchar(150) NOT NULL,
        [ContactPerson] nvarchar(120) NULL,
        [Phone]         nvarchar(40)  NULL,
        [Email]         nvarchar(256) NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_Vendor] PRIMARY KEY ([Id])
    );
END
GO
/*  ReportingManagerId is a self-reference - it replaces the handbook's separate
    BranchManagers master, which could not express a manager who is also
    somebody's report. */
IF OBJECT_ID(N'[Organization].[Employee]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Employee] (
        [Id]                 int           NOT NULL IDENTITY,
        [EmployeeCode]       nvarchar(30)  NOT NULL,
        [FullName]           nvarchar(150) NOT NULL,
        [Email]              nvarchar(256) NULL,
        [Phone]              nvarchar(40)  NULL,
        [DepartmentId]       int           NULL,
        [BranchId]           int           NULL,
        [ReportingManagerId] int           NULL,
        [IsActive]           bit           NOT NULL,
        [CreatedOnUtc]       datetime2     NOT NULL,
        [CreatedBy]          nvarchar(100) NULL,
        [ModifiedOnUtc]      datetime2     NULL,
        [ModifiedBy]         nvarchar(100) NULL,
        /*  R2-1: no [RowVersion] here. SQL Server forbids rowversion columns
            on system-versioned tables (the history table must mirror the
            schema and a rowversion column cannot accept copied values).
            R2-22: [ConcurrencyStamp] below is the EF concurrency token. The
            previous revision used [SysStartTime], on the premise that it is
            regenerated on every UPDATE. It is not: SQL Server stamps it from
            the TRANSACTION start time and the system clock moves in ~1-15ms
            ticks, so two updates in one tick share a value, a stale token
            still matches, and one writer's change is lost in silence. */
        /*  System-versioned. SQL Server keeps every prior version of this row in
            [Organization].[EmployeeHistory], so what the record said on any past date can be read
            directly, without replaying a change log:
                SELECT * FROM [Organization].[Employee] FOR SYSTEM_TIME AS OF '2026-03-31T18:30:00'
            The period columns are HIDDEN, so SELECT * and EF's queries never see
            them. Declare the table .IsTemporal() so the model agrees. */
        [ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_Employee_ConcurrencyStamp] DEFAULT (NEWID()),   -- R2-22
        [SysStartTime] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [SysEndTime]   datetime2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
        PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]),
        CONSTRAINT [PK_Employee] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employee_Department_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Organization].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employee_Employee_ReportingManagerId] FOREIGN KEY ([ReportingManagerId]) REFERENCES [Organization].[Employee] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employee_Branch_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Organization].[Branch] ([Id]) ON DELETE NO ACTION
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Organization].[EmployeeHistory]));
END
GO
IF OBJECT_ID(N'[Organization].[Application]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Application] (
        [Id]              int           NOT NULL IDENTITY,
        [ApplicationName] nvarchar(100) NOT NULL,
        [IsActive]        bit           NOT NULL,
        [CreatedOnUtc]    datetime2     NOT NULL,
        [CreatedBy]       nvarchar(100) NULL,
        [ModifiedOnUtc]   datetime2     NULL,
        [ModifiedBy]      nvarchar(100) NULL,
        CONSTRAINT [PK_Application] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Organization].[EmployeeApplication]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[EmployeeApplication] (
        [Id]                 int           NOT NULL IDENTITY,
        [EmployeeId]         int           NOT NULL,
        [ApplicationId]      int           NOT NULL,
        [ApplicationLoginId] nvarchar(100) NULL,
        [GrantedOnUtc]       datetime2     NOT NULL,
        [RevokedOnUtc]       datetime2     NULL,
        [CreatedOnUtc]       datetime2     NOT NULL,
        [CreatedBy]          nvarchar(100) NULL,
        [ModifiedOnUtc]      datetime2     NULL,
        [ModifiedBy]         nvarchar(100) NULL,
        CONSTRAINT [PK_EmployeeApplication] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeApplication_Application_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Organization].[Application] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeApplication_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Organization].[Employee] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Region]'), N'UX_Region_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Region_Name] ON [Organization].[Region] ([RegionName]);              -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Branch]'), N'UX_Branch_Code', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Branch_Code] ON [Organization].[Branch] ([BranchCode]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Branch]'), N'UX_Branch_OneHeadOffice', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Branch_OneHeadOffice] ON [Organization].[Branch] ([IsHeadOffice]) WHERE [IsHeadOffice] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Branch]'), N'IX_Branch_RegionId', N'IndexID') IS NULL
    CREATE INDEX [IX_Branch_RegionId] ON [Organization].[Branch] ([RegionId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Department]'), N'UX_Department_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Department_Name] ON [Organization].[Department] ([DepartmentName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Vendor]'), N'UX_Vendor_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Vendor_Name] ON [Organization].[Vendor] ([VendorName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Employee]'), N'UX_Employee_Code', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Employee_Code] ON [Organization].[Employee] ([EmployeeCode]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Employee]'), N'IX_Employee_Branch', N'IndexID') IS NULL
    CREATE INDEX [IX_Employee_Branch] ON [Organization].[Employee] ([BranchId], [FullName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Employee]'), N'IX_Employee_DepartmentId', N'IndexID') IS NULL
    CREATE INDEX [IX_Employee_DepartmentId] ON [Organization].[Employee] ([DepartmentId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Employee]'), N'IX_Employee_ReportingManagerId', N'IndexID') IS NULL
    CREATE INDEX [IX_Employee_ReportingManagerId] ON [Organization].[Employee] ([ReportingManagerId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[Application]'), N'UX_Application_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Application_Name] ON [Organization].[Application] ([ApplicationName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[EmployeeApplication]'), N'IX_EmployeeApplication_ApplicationId', N'IndexID') IS NULL
    CREATE INDEX [IX_EmployeeApplication_ApplicationId] ON [Organization].[EmployeeApplication] ([ApplicationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Organization].[EmployeeApplication]'), N'UX_EmployeeApplication_OneActive', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_EmployeeApplication_OneActive] ON [Organization].[EmployeeApplication] ([EmployeeId], [ApplicationId]) WHERE [RevokedOnUtc] IS NULL;
GO
/* ===========================================================================
   SECTION 3 - [Assets]  Register, 1:1 detail tables, custom fields, timeline
   ---------------------------------------------------------------------------
   [Asset] is the centre of the model. Every state change that moves an asset
   also appends an [AssetEvent] in the SAME transaction - a timeline that can
   disagree with the row it describes is worse than no timeline, because it is
   believed.
   Assets are soft-deleted. History that points at a hard-deleted asset is a
   report that quietly loses rows.
   =========================================================================== */
/*  R3: the FINANCE axis, and it is genuinely independent of [AssetType].
    Measured on the live register: 49 technical groups appear under more than
    one accounting category and 86 under more than one class - "Storage Rack"
    is Furniture & Fixtures AND Plant & Machinery AND Office Equipments. A
    class/type tree would silently misclassify hundreds of rows on import, so
    the two are separate columns on [Asset] and their cross-product is
    deliberately unconstrained.

    [ReportingCategory] is a COLUMN and not a table because the register's
    9 categories are a pure function of its 13 classes - the cross-tab has
    exactly 13 rows.

    No depreciation or chart-of-account DEFAULTS here: SAP owns the arithmetic
    (see [Assets].[AssetFinance]) and a default nothing computes from is
    documentation pretending to be schema. They arrive only if that ownership
    ever flips. */
IF OBJECT_ID(N'[Assets].[AssetClass]', N'U') IS NULL                                -- R3
BEGIN
    CREATE TABLE [Assets].[AssetClass] (
        [Id]                int           NOT NULL IDENTITY,
        [ClassCode]         nvarchar(20)  NOT NULL,
        [ClassName]         nvarchar(100) NOT NULL,
        [ReportingCategory] nvarchar(100) NOT NULL,
        [IsDepreciable]     bit           NOT NULL,   -- Leasehold Land is not
        [IsIntangible]      bit           NOT NULL,
        [IsAuc]             bit           NOT NULL,   -- exactly one row: assets under construction
        [IsActive]          bit           NOT NULL,
        [CreatedOnUtc]      datetime2     NOT NULL,
        [CreatedBy]         nvarchar(100) NULL,
        [ModifiedOnUtc]     datetime2     NULL,
        [ModifiedBy]        nvarchar(100) NULL,
        [RowVersion]        rowversion    NOT NULL,
        CONSTRAINT [PK_AssetClass] PRIMARY KEY ([Id])
    );
END
GO
/*  R3: the register carries three code/description PAIRS per asset. Storing
    the pair inline would keep 7,000 copies of one description, which drift
    apart the first time somebody corrects a typo in the ledger. */
IF OBJECT_ID(N'[Assets].[ChartOfAccount]', N'U') IS NULL                            -- R3
BEGIN
    CREATE TABLE [Assets].[ChartOfAccount] (
        [Id]            int           NOT NULL IDENTITY,
        [CoaCode]       nvarchar(30)  NOT NULL,
        [Description]   nvarchar(200) NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        [RowVersion]    rowversion    NOT NULL,
        CONSTRAINT [PK_ChartOfAccount] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Assets].[AssetType]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetType] (
        [Id]               int           NOT NULL IDENTITY,
        [TypeName]          nvarchar(100) NOT NULL,
        [ParentAssetTypeId] int           NULL,
        /*  R3: behaviour flags. These decide what the APPLICATION does with an
            asset of this type - whether it can be issued to a person, whether
            it is counted or scanned, which detail table applies. They live here
            and not on [AssetClass] because "can a barricade be issued to a
            supervisor" is an operational judgement, not an accounting one, and
            because custom fields already hang off this table. */
        [IsAllocatable]     bit NOT NULL CONSTRAINT [DF_AssetType_IsAllocatable]     DEFAULT (1),
        [IsPhysical]        bit NOT NULL CONSTRAINT [DF_AssetType_IsPhysical]        DEFAULT (1),   -- 0 = software/licence: no serial, no location, no verification
        [IsBulkDefault]     bit NOT NULL CONSTRAINT [DF_AssetType_IsBulkDefault]     DEFAULT (0),
        [TracksHardware]    bit NOT NULL CONSTRAINT [DF_AssetType_TracksHardware]    DEFAULT (0),
        [TracksSoftware]    bit NOT NULL CONSTRAINT [DF_AssetType_TracksSoftware]    DEFAULT (0),
        [TracksVehicle]     bit NOT NULL CONSTRAINT [DF_AssetType_TracksVehicle]     DEFAULT (0),
        [TracksCalibration] bit NOT NULL CONSTRAINT [DF_AssetType_TracksCalibration] DEFAULT (0),
        [IsActive]          bit           NOT NULL,
        [CreatedOnUtc]      datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_AssetType] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssetType_AssetType_ParentAssetTypeId] FOREIGN KEY ([ParentAssetTypeId]) REFERENCES [Assets].[AssetType] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Status is a lookup row, not an enum: "In Standby" and "In Standby-IT" were
    added for the branch/HO store split, and an administrator adding
    "Awaiting Disposal" should not need a release.
    R2-5: Sec 17.1 now seeds the FULL baseline set, not just the two standby
    rows - this script claims to stand up an empty database, and an asset
    register with no statuses cannot create its first asset. */
IF OBJECT_ID(N'[Assets].[AssetStatus]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetStatus] (
        [Id]           int          NOT NULL IDENTITY,
        [StatusName]   nvarchar(50) NOT NULL,
        [IsTerminal]   bit          NOT NULL,
        [DisplayOrder] int          NOT NULL,
        [IsActive]     bit          NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_AssetStatus] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Assets].[Asset]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[Asset] (
        [Id]                     int           NOT NULL IDENTITY,
        [AssetNumber]            nvarchar(40)  NOT NULL,
        [AssetName]              nvarchar(200) NOT NULL,
        [SerialNumber]           nvarchar(100) NULL,
        [AssetTypeId]            int           NOT NULL,   -- what the thing IS      (operational axis)
        [AssetClassId]           int           NULL,       -- how finance sees it    (NULL until classified)  -- R3
        [Make]                   nvarchar(100) NULL,       -- R3: promoted from AssetHardwareDetail; a chair has a make
        [Model]                  nvarchar(100) NULL,       -- R3
        [AssetStatusId]          int           NOT NULL,
        [CurrentLocationId]      int           NULL,   -- Organization.Branch, id only
        [CurrentEmployeeId]      int           NULL,   -- Organization.Employee, id only
        [DepartmentId]           int           NULL,
        [CostCenter]             nvarchar(40)  NULL,
        [AcquisitionDate]        date          NULL,
        [QrCodeValue]            nvarchar(100) NULL,
        [BarcodeValue]           nvarchar(100) NULL,
        [ErpAssetNumber]         nvarchar(50)  NULL,
        [SapAssetNumber]         nvarchar(50)  NULL,
        [SapAssetClass]          nvarchar(50)  NULL,
        [SapPlant]               nvarchar(20)  NULL,
        [LastSapSyncOnUtc]       datetime2     NULL,
        [LastPhysicalCheckOnUtc] datetime2     NULL,
        [Remarks]                nvarchar(1000) NULL,
        [ImportedDataJson]       nvarchar(max)  NULL, -- original 70-column FAR row for drill-down
        /*  R3: bulk mode. Measured on the live register, itemisation is already
            the norm - the 1,163 chairs are 1,163 rows - and only 463 of 7,413
            rows carry a quantity above one: scaffolding, barricades, bins,
            crates, pallets. Pooled site material, never issued to a person.
            CK_Asset_UnitQuantityIsOne below turns that into a PROOF, so
            allocation, handover and unit verification never reason about
            quantity at all. */
        [IsBulk]                 bit           NOT NULL CONSTRAINT [DF_Asset_IsBulk] DEFAULT (0),        -- R3
        [Quantity]               decimal(18,3) NOT NULL CONSTRAINT [DF_Asset_Quantity] DEFAULT (1),      -- R3
        [UnitOfMeasure]          nvarchar(20)  NULL,       -- Nos, Set, Metre. Required when IsBulk      -- R3
        [CapitalisedFromAssetId] int           NULL,       -- the AUC this settled from                  -- R3
        [SplitFromAssetId]       int           NULL,       -- the bulk line this was carved out of       -- R3
        [ImportBatchId]          int           NULL,       -- DataImport.ImportBatch, id only            -- R3-2
        [IsDeleted]              bit           NOT NULL,
        [CreatedOnUtc]           datetime2     NOT NULL,
        [CreatedBy]              nvarchar(100) NULL,
        [ModifiedOnUtc]          datetime2     NULL,
        [ModifiedBy]             nvarchar(100) NULL,
        /*  R2-1: no [RowVersion] - forbidden on system-versioned tables.
            R2-22: [ConcurrencyStamp] is the concurrency token, not
            [SysStartTime] - see the note on [Organization].[Employee]. */
        /*  System-versioned. SQL Server keeps every prior version of this row in
            [Assets].[AssetHistory], so what the record said on any past date can be read
            directly, without replaying a change log:
                SELECT * FROM [Assets].[Asset] FOR SYSTEM_TIME AS OF '2026-03-31T18:30:00'
            The period columns are HIDDEN, so SELECT * and EF's queries never see
            them. Declare the table .IsTemporal() so the model agrees. */
        [ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_Asset_ConcurrencyStamp] DEFAULT (NEWID()),   -- R2-22
        [SysStartTime] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [SysEndTime]   datetime2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
        PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]),
        CONSTRAINT [PK_Asset] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Asset_QuantityPositive]  CHECK ([Quantity] > 0),                                       -- R3
        /*  The load-bearing one. Every allocatable asset provably has
            Quantity = 1, so UX_AssetAllocation_OneActivePerAsset and every
            unit flow stay exactly as reviewed. */
        CONSTRAINT [CK_Asset_UnitQuantityIsOne] CHECK ([IsBulk] = 1 OR [Quantity] = 1),                       -- R3
        CONSTRAINT [CK_Asset_BulkHasUom]        CHECK ([IsBulk] = 0 OR [UnitOfMeasure] IS NOT NULL),          -- R3
        /*  A bulk line is in four places at once, so it has no single current
            location and nobody holds it. Its custody lives in [AssetHolding]. */
        CONSTRAINT [CK_Asset_BulkNotHeld]       CHECK ([IsBulk] = 0 OR ([CurrentEmployeeId] IS NULL AND [CurrentLocationId] IS NULL)),   -- R3
        CONSTRAINT [FK_Asset_AssetType_AssetTypeId] FOREIGN KEY ([AssetTypeId]) REFERENCES [Assets].[AssetType] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Asset_AssetClass_AssetClassId] FOREIGN KEY ([AssetClassId]) REFERENCES [Assets].[AssetClass] ([Id]) ON DELETE NO ACTION,   -- R3
        CONSTRAINT [FK_Asset_Asset_CapitalisedFromAssetId] FOREIGN KEY ([CapitalisedFromAssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION,   -- R3
        CONSTRAINT [FK_Asset_Asset_SplitFromAssetId] FOREIGN KEY ([SplitFromAssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION,               -- R3
        CONSTRAINT [FK_Asset_AssetStatus_AssetStatusId] FOREIGN KEY ([AssetStatusId]) REFERENCES [Assets].[AssetStatus] ([Id]) ON DELETE NO ACTION
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[AssetHistory]));
END
GO
IF OBJECT_ID(N'[Assets].[AssetHardwareDetail]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetHardwareDetail] (
        [AssetId]             int           NOT NULL,
        [Hostname]            nvarchar(100) NULL,   -- R3: moved off [Asset]; a chair has no hostname
        [ChassisType]         nvarchar(50)  NULL,
        [Processor]           nvarchar(150) NULL,   -- replaces the handbook ProcessorMasters lookup
        [MemoryGb]            int           NULL,
        [StorageGb]           int           NULL,
        [MonitorModel]        nvarchar(100) NULL,
        [MonitorSerialNumber] nvarchar(100) NULL,
        [MacAddress]          nvarchar(50)  NULL,
        [IpAddress]           nvarchar(45)  NULL,
        [CreatedOnUtc]        datetime2     NOT NULL,
        [CreatedBy]           nvarchar(100) NULL,
        [ModifiedOnUtc]       datetime2     NULL,
        [ModifiedBy]          nvarchar(100) NULL,
        CONSTRAINT [PK_AssetHardwareDetail] PRIMARY KEY ([AssetId]),
        CONSTRAINT [FK_AssetHardwareDetail_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Assets].[AssetSoftwareDetail]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetSoftwareDetail] (
        [AssetId]              int            NOT NULL,
        [OperatingSystem]      nvarchar(120)  NULL,
        [OperatingSystemBuild] nvarchar(60)   NULL,
        [Architecture]         nvarchar(20)   NULL,
        [OfficeVersion]        nvarchar(80)   NULL,
        [Antivirus]            nvarchar(120)  NULL,
        [OsKeyEncrypted]       varbinary(max) NULL,   -- licence keys are never stored in clear
        [CreatedOnUtc]         datetime2      NOT NULL,
        [CreatedBy]            nvarchar(100)  NULL,
        [ModifiedOnUtc]        datetime2      NULL,
        [ModifiedBy]           nvarchar(100)  NULL,
        CONSTRAINT [PK_AssetSoftwareDetail] PRIMARY KEY ([AssetId]),
        CONSTRAINT [FK_AssetSoftwareDetail_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Assets].[AssetPurchaseDetail]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetPurchaseDetail] (
        [AssetId]             int            NOT NULL,
        [VendorId]            int            NULL,   -- Organization.Vendor, id only
        [PurchaseOrderNumber] nvarchar(50)   NULL,
        [InvoiceNumber]       nvarchar(50)   NULL,
        [PurchaseDate]        date           NULL,
        [PurchaseCost]        decimal(18,2)  NULL,   -- decimal: this feeds SAP postings
        [WarrantyStartDate]   date           NULL,
        [WarrantyEndDate]     date           NULL,
        [CreatedOnUtc]        datetime2      NOT NULL,
        [CreatedBy]           nvarchar(100)  NULL,
        [ModifiedOnUtc]       datetime2      NULL,
        [ModifiedBy]          nvarchar(100)  NULL,
        CONSTRAINT [PK_AssetPurchaseDetail] PRIMARY KEY ([AssetId]),
        CONSTRAINT [CK_AssetPurchaseDetail_WarrantyWindow] CHECK ([WarrantyEndDate] IS NULL OR [WarrantyStartDate] IS NULL OR [WarrantyEndDate] >= [WarrantyStartDate]),
        CONSTRAINT [FK_AssetPurchaseDetail_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Assets].[CustomFieldDefinition]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[CustomFieldDefinition] (
        [Id]              int            NOT NULL IDENTITY,
        [AssetTypeId] int            NOT NULL,
        [FieldName]       nvarchar(80)   NOT NULL,
        [DisplayLabel]    nvarchar(150)  NOT NULL,
        [FieldType]       nvarchar(20)   NOT NULL,   -- enforced by CK_CustomFieldDefinition_Type (R2-26)
        [IsRequired]      bit            NOT NULL,
        [MinValue]        decimal(18,4)  NULL,
        [MaxValue]        decimal(18,4)  NULL,
        [ValidationRegex] nvarchar(300)  NULL,
        [DefaultValue]    nvarchar(300)  NULL,
        [DisplayOrder]    int            NOT NULL,
        [IsActive]        bit            NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_CustomFieldDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomFieldDefinition_Type] CHECK ([FieldType] IN (N'Text', N'Number', N'Percentage', N'Date', N'Boolean', N'Dropdown')),   -- R2-26
        CONSTRAINT [CK_CustomFieldDefinition_Range] CHECK ([MinValue] IS NULL OR [MaxValue] IS NULL OR [MaxValue] >= [MinValue]),
        CONSTRAINT [FK_CustomFieldDefinition_AssetType_AssetTypeId] FOREIGN KEY ([AssetTypeId]) REFERENCES [Assets].[AssetType] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[Assets].[CustomFieldOption]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[CustomFieldOption] (
        [Id]                      int           NOT NULL IDENTITY,
        [CustomFieldDefinitionId] int           NOT NULL,
        [OptionValue]             nvarchar(150) NOT NULL,
        [DisplayOrder]            int           NOT NULL,
        [IsActive]                bit           NOT NULL,   -- retire, never delete an in-use option
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_CustomFieldOption] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFieldOption_CustomFieldDefinition_CustomFieldDefinitionId] FOREIGN KEY ([CustomFieldDefinitionId]) REFERENCES [Assets].[CustomFieldDefinition] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  Typed columns alongside the canonical text so a numeric custom field can be
    filtered and summed without CAST over every row. */
IF OBJECT_ID(N'[Assets].[AssetCustomValue]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetCustomValue] (
        [Id]                      int            NOT NULL IDENTITY,
        [AssetId]                 int            NOT NULL,
        [CustomFieldDefinitionId] int            NOT NULL,
        [Value]                   nvarchar(1000) NULL,
        [ValueNumber]             decimal(18,4)  NULL,
        [ValueDate]               date           NULL,
        [OptionId]                int            NULL,
        [UpdatedOnUtc]            datetime2      NOT NULL,
        [UpdatedBy]               nvarchar(100)  NULL,
        CONSTRAINT [PK_AssetCustomValue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssetCustomValue_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssetCustomValue_CustomFieldDefinition_CustomFieldDefinitionId] FOREIGN KEY ([CustomFieldDefinitionId]) REFERENCES [Assets].[CustomFieldDefinition] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetCustomValue_CustomFieldOption_OptionId] FOREIGN KEY ([OptionId]) REFERENCES [Assets].[CustomFieldOption] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  R3 - FINANCE. SAP S/4HANA owns the arithmetic; this is a MIRROR.
    The register is an export FROM the accounting system, every row uses one
    method, and its Status column carries SAP's own vocabulary. Building a
    statutory depreciation engine beside a live ERP is how two numbers for one
    asset reach two reports.

    Written only by [SapSync] and [DataImport], through the Assets PublicApi
    write contract (architecture rule 4a). Read-only in the UI behind a
    finance.view capability. [LastSyncedOnUtc] NULL means the row was keyed or
    imported rather than synced.

    If ownership ever flips, this table barely changes - a run header and a
    posting flag arrive - which is exactly why depreciation is a SCHEDULE
    below and not three columns here. */
IF OBJECT_ID(N'[Assets].[AssetFinance]', N'U') IS NULL                              -- R3
BEGIN
    CREATE TABLE [Assets].[AssetFinance] (
        [AssetId]                      int           NOT NULL,
        [OriginalValue]                decimal(18,2) NULL,
        [MigratedBookValue]            decimal(18,2) NULL,
        [AdditionalValue]              decimal(18,2) NULL,
        [GrossValue]                   decimal(18,2) NULL,
        [DisposalGrossValue]           decimal(18,2) NULL,
        [AccumulatedDepreciation]      decimal(18,2) NULL,   -- as at the last sync
        [NetBookValue]                 decimal(18,2) NULL,   -- as at the last sync
        [DepreciationMethod]           nvarchar(30)  NULL,
        [DepreciationPercent]          decimal(9,4)  NULL,
        [UsefulLifeMonths]             int           NULL,
        [CapitalisedQuantity]          decimal(18,3) NULL,   -- original; [Asset].[Quantity] is current
        [FirstAcquisitionDate]         date          NULL,
        [PostingDate]                  date          NULL,
        [SapPostingStatus]             nvarchar(20)  NULL,   -- SAP vocabulary: New | Post
        /*  Present on 6,488 rows of the live register: capitalisation from an
            asset under construction is how most of it came into existence, not
            an edge case. Kept even where the AUC predates AMS and
            [Asset].[CapitalisedFromAssetId] has no row to point at. */
        [AucReference]                 nvarchar(50)  NULL,
        [OpportunityName]              nvarchar(200) NULL,   -- the project it was bought for
        [VoucherNo]                    nvarchar(60)  NULL,
        [ApVoucherNo]                  nvarchar(60)  NULL,
        [GrossValueCoaId]              int           NULL,
        [AccumulatedDepreciationCoaId] int           NULL,
        [DepreciationChargeCoaId]      int           NULL,
        [LastSyncedOnUtc]              datetime2     NULL,
        [CreatedOnUtc]                 datetime2     NOT NULL,
        [CreatedBy]                    nvarchar(100) NULL,
        [ModifiedOnUtc]                datetime2     NULL,
        [ModifiedBy]                   nvarchar(100) NULL,
        [RowVersion]                   rowversion    NOT NULL,
        CONSTRAINT [PK_AssetFinance] PRIMARY KEY ([AssetId]),
        CONSTRAINT [CK_AssetFinance_Method] CHECK ([DepreciationMethod] IS NULL OR [DepreciationMethod] IN (N'StraightLine', N'WrittenDownValue', N'None')),
        CONSTRAINT [FK_AssetFinance_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssetFinance_ChartOfAccount_GrossValueCoaId] FOREIGN KEY ([GrossValueCoaId]) REFERENCES [Assets].[ChartOfAccount] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetFinance_ChartOfAccount_AccumulatedDepreciationCoaId] FOREIGN KEY ([AccumulatedDepreciationCoaId]) REFERENCES [Assets].[ChartOfAccount] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetFinance_ChartOfAccount_DepreciationChargeCoaId] FOREIGN KEY ([DepreciationChargeCoaId]) REFERENCES [Assets].[ChartOfAccount] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  R3 - the year-by-year story. The register's opening accumulated, charged
    for the year and closing accumulated are a SCHEDULE, not three columns, and
    storing it as one is what lets any prior year be reproduced.
    NO ACTION and no [IsPosted]: financial evidence blocks deletion (the same
    reasoning as R2-12), and AMS does not post to the ledger. */
IF OBJECT_ID(N'[Assets].[AssetDepreciationEntry]', N'U') IS NULL                    -- R3
BEGIN
    CREATE TABLE [Assets].[AssetDepreciationEntry] (
        [Id]                  bigint        NOT NULL IDENTITY,
        [AssetId]             int           NOT NULL,
        [FinancialYear]       smallint      NOT NULL,   -- 2026 = FY 2026-27
        [OpeningAccumulated]  decimal(18,2) NOT NULL,
        [Additions]           decimal(18,2) NOT NULL,
        [ChargedForPeriod]    decimal(18,2) NOT NULL,
        [ClosingAccumulated]  decimal(18,2) NOT NULL,
        [NetBookValueAtClose] decimal(18,2) NOT NULL,
        [SourceSystem]        nvarchar(20)  NOT NULL,
        [SyncedOnUtc]         datetime2     NOT NULL,
        CONSTRAINT [PK_AssetDepreciationEntry] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetDepreciationEntry_Source] CHECK ([SourceSystem] IN (N'Sap', N'Import')),
        CONSTRAINT [FK_AssetDepreciationEntry_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  R3 - where bulk quantity actually lives. A bulk line has no single current
    location, so its custody is a set of per-place balances. The two filtered
    unique indexes make one balance row per asset per place a database fact, so
    two concurrent receipts collide on 2601 and one retries as an increment -
    design rule 6, applied to stock.

    [CK_AssetHolding_NonNegative] is the over-issue backstop: a set-based
    decrement that would go below zero dies INSIDE the database rather than in
    a read-then-write check. It is the one 547 the API translates to a 409
    instead of treating as a coding bug, because insufficient stock is a
    user-facing race and not a defect. */
IF OBJECT_ID(N'[Assets].[AssetHolding]', N'U') IS NULL                              -- R3
BEGIN
    CREATE TABLE [Assets].[AssetHolding] (
        [Id]             int           NOT NULL IDENTITY,
        [AssetId]        int           NOT NULL,
        [LocationId]     int           NULL,   -- Organization.Branch, id only
        [CustomerSiteId] int           NULL,   -- Allocations.CustomerSite, id only
        [OnHandQuantity] decimal(18,3) NOT NULL,
        [CreatedOnUtc]   datetime2     NOT NULL,
        [CreatedBy]      nvarchar(100) NULL,
        [ModifiedOnUtc]  datetime2     NULL,
        [ModifiedBy]     nvarchar(100) NULL,
        [RowVersion]     rowversion    NOT NULL,
        CONSTRAINT [PK_AssetHolding] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetHolding_NonNegative]  CHECK ([OnHandQuantity] >= 0),
        CONSTRAINT [CK_AssetHolding_OnePlaceKind] CHECK (([LocationId] IS NOT NULL AND [CustomerSiteId] IS NULL) OR ([LocationId] IS NULL AND [CustomerSiteId] IS NOT NULL)),
        CONSTRAINT [FK_AssetHolding_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  R3 - disposal is an EVENT, not a date column: a bulk line is disposed of in
    parts and each part carries its own approval. */
IF OBJECT_ID(N'[Assets].[AssetDisposal]', N'U') IS NULL                             -- R3
BEGIN
    CREATE TABLE [Assets].[AssetDisposal] (
        [Id]                 int           NOT NULL IDENTITY,
        [AssetId]            int           NOT NULL,
        [DisposalDate]       date          NOT NULL,
        [DisposalQuantity]   decimal(18,3) NOT NULL,
        [DisposalGrossValue] decimal(18,2) NULL,
        [SaleProceeds]       decimal(18,2) NULL,
        [DisposalReason]     nvarchar(300) NOT NULL,
        [ApprovedByUserId]   int           NULL,   -- Identity.User, id only
        [CreatedOnUtc]       datetime2     NOT NULL,
        [CreatedBy]          nvarchar(100) NULL,
        [ModifiedOnUtc]      datetime2     NULL,
        [ModifiedBy]         nvarchar(100) NULL,
        [RowVersion]         rowversion    NOT NULL,
        CONSTRAINT [PK_AssetDisposal] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetDisposal_QuantityPositive] CHECK ([DisposalQuantity] > 0),
        CONSTRAINT [FK_AssetDisposal_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  R3 - vehicles. Applies where [AssetType].[TracksVehicle] = 1. */
IF OBJECT_ID(N'[Assets].[AssetVehicleDetail]', N'U') IS NULL                        -- R3
BEGIN
    CREATE TABLE [Assets].[AssetVehicleDetail] (
        [AssetId]             int           NOT NULL,
        [RegistrationNumber]  nvarchar(20)  NOT NULL,
        [ChassisNumber]       nvarchar(50)  NULL,
        [EngineNumber]        nvarchar(50)  NULL,
        [FuelType]            nvarchar(20)  NULL,
        [FitnessExpiryDate]   date          NULL,
        [PucExpiryDate]       date          NULL,
        [InsuranceExpiryDate] date          NULL,   -- a vehicle policy is not the blanket fire policy
        [OdometerKm]          int           NULL,
        [CreatedOnUtc]        datetime2     NOT NULL,
        [CreatedBy]           nvarchar(100) NULL,
        [ModifiedOnUtc]       datetime2     NULL,
        [ModifiedBy]          nvarchar(100) NULL,
        [RowVersion]          rowversion    NOT NULL,
        CONSTRAINT [PK_AssetVehicleDetail] PRIMARY KEY ([AssetId]),
        CONSTRAINT [FK_AssetVehicleDetail_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  R3 - calibrated instruments. The window moved off [Asset]: 221 rows of the
    live register carry calibration dates, so the due-report reads one narrow
    index instead of scanning 7,413 rows for a column 97% of them never use. */
IF OBJECT_ID(N'[Assets].[AssetInstrumentDetail]', N'U') IS NULL                     -- R3
BEGIN
    CREATE TABLE [Assets].[AssetInstrumentDetail] (
        [AssetId]                    int           NOT NULL,
        [CalibrationStartDate]       date          NULL,
        [CalibrationEndDate]         date          NULL,
        [CalibrationFrequencyMonths] int           NULL,
        [CalibrationAgency]          nvarchar(200) NULL,
        [CertificateNumber]          nvarchar(80)  NULL,
        [MeasurementRange]           nvarchar(100) NULL,
        [AccuracyClass]              nvarchar(50)  NULL,
        [CreatedOnUtc]               datetime2     NOT NULL,
        [CreatedBy]                  nvarchar(100) NULL,
        [ModifiedOnUtc]              datetime2     NULL,
        [ModifiedBy]                 nvarchar(100) NULL,
        [RowVersion]                 rowversion    NOT NULL,
        CONSTRAINT [PK_AssetInstrumentDetail] PRIMARY KEY ([AssetId]),
        CONSTRAINT [CK_AssetInstrumentDetail_Window] CHECK ([CalibrationEndDate] IS NULL OR [CalibrationStartDate] IS NULL OR [CalibrationEndDate] >= [CalibrationStartDate]),
        CONSTRAINT [FK_AssetInstrumentDetail_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  The one business timeline. Name snapshots are deliberate: an event must
    still read correctly after the employee leaves or the branch is renamed.
    [HandoverId] is NEW so the branch-store step appears on the same timeline
    as allocation and shipment, instead of in a second history nobody reads. */
IF OBJECT_ID(N'[Assets].[AssetEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [Assets].[AssetEvent] (
        [Id]                    bigint        NOT NULL IDENTITY,
        [AssetId]               int           NOT NULL,
        [EventType]             nvarchar(50)  NOT NULL,
        [Description]           nvarchar(500) NOT NULL,
        [EventOnUtc]            datetime2     NOT NULL,
        [PerformedBy]           nvarchar(100) NOT NULL,
        [EmployeeId]            int           NULL,
        [EmployeeNameSnapshot]  nvarchar(150) NULL,
        [LocationId]            int           NULL,
        [LocationNameSnapshot]  nvarchar(100) NULL,
        [AllocationId]          int           NULL,
        [MovementId]            int           NULL,
        [ServiceRequestId]      int           NULL,
        [ContractId]            int           NULL,
        [HandoverId]            int           NULL,   -- NEW  Allocations.AssetHandover, id only
        [VerificationId]        int           NULL,   -- NEW  Verification.PhysicalVerification, id only
        [DisposalId]            int           NULL,   -- R3  Assets.AssetDisposal, same schema, id only
        /*  R3: signed. A receipt of 200 is +200, an issue of 5 is -5, and a
            unit asset's events leave it NULL. This is what makes the timeline
            reconcile against [AssetHolding] instead of merely narrating it. */
        [QuantityDelta]         decimal(18,3) NULL,
        CONSTRAINT [PK_AssetEvent] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssetEvent_Asset_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetType]'), N'UX_AssetType_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetType_Name] ON [Assets].[AssetType] ([TypeName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetType]'), N'IX_AssetType_ParentAssetTypeId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetType_ParentAssetTypeId] ON [Assets].[AssetType] ([ParentAssetTypeId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetStatus]'), N'UX_AssetStatus_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetStatus_Name] ON [Assets].[AssetStatus] ([StatusName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'UX_Asset_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Asset_Number] ON [Assets].[Asset] ([AssetNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'UX_Asset_QrCode', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Asset_QrCode] ON [Assets].[Asset] ([QrCodeValue]) WHERE [QrCodeValue] IS NOT NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'UX_Asset_SapNumber', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Asset_SapNumber] ON [Assets].[Asset] ([SapAssetNumber]) WHERE [SapAssetNumber] IS NOT NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_LocationStatus', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_LocationStatus] ON [Assets].[Asset] ([CurrentLocationId], [AssetStatusId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_Serial', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_Serial] ON [Assets].[Asset] ([SerialNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_AssetTypeId', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_AssetTypeId] ON [Assets].[Asset] ([AssetTypeId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_AssetStatusId', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_AssetStatusId] ON [Assets].[Asset] ([AssetStatusId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[CustomFieldDefinition]'), N'UX_CustomFieldDefinition_TypeField', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_CustomFieldDefinition_TypeField] ON [Assets].[CustomFieldDefinition] ([AssetTypeId], [FieldName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[CustomFieldOption]'), N'UX_CustomFieldOption_Value', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_CustomFieldOption_Value] ON [Assets].[CustomFieldOption] ([CustomFieldDefinitionId], [OptionValue]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetCustomValue]'), N'UX_AssetCustomValue_AssetField', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetCustomValue_AssetField] ON [Assets].[AssetCustomValue] ([AssetId], [CustomFieldDefinitionId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetCustomValue]'), N'IX_AssetCustomValue_NumericLookup', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetCustomValue_NumericLookup] ON [Assets].[AssetCustomValue] ([CustomFieldDefinitionId], [ValueNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetCustomValue]'), N'IX_AssetCustomValue_OptionId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetCustomValue_OptionId] ON [Assets].[AssetCustomValue] ([OptionId]);
/*  R3: exactly one AUC class. The capitalisation step finds the source
    class by [IsAuc] = 1, so a second one would make it ambiguous - and a
    filtered unique index says that once, in the only place that cannot be
    bypassed. */
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetClass]'), N'UX_AssetClass_OneAuc', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetClass_OneAuc] ON [Assets].[AssetClass] ([IsAuc]) WHERE [IsAuc] = 1;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetClass]'), N'UX_AssetClass_Code', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetClass_Code] ON [Assets].[AssetClass] ([ClassCode]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetClass]'), N'UX_AssetClass_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetClass_Name] ON [Assets].[AssetClass] ([ClassName]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[ChartOfAccount]'), N'UX_ChartOfAccount_Code', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ChartOfAccount_Code] ON [Assets].[ChartOfAccount] ([CoaCode]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_ImportBatchId', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_ImportBatchId] ON [Assets].[Asset] ([ImportBatchId]) WHERE [ImportBatchId] IS NOT NULL;   -- R3-2
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_AssetClassId', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_AssetClassId] ON [Assets].[Asset] ([AssetClassId]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[Asset]'), N'IX_Asset_CapitalisedFromAssetId', N'IndexID') IS NULL
    CREATE INDEX [IX_Asset_CapitalisedFromAssetId] ON [Assets].[Asset] ([CapitalisedFromAssetId]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetDepreciationEntry]'), N'UX_AssetDepreciationEntry_AssetYear', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetDepreciationEntry_AssetYear] ON [Assets].[AssetDepreciationEntry] ([AssetId], [FinancialYear]);   -- R3
/*  One balance row per asset per place. Filtered because a holding is either
    at a branch or at a customer site, never both, and the NULL half must not
    collide with itself. */
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetHolding]'), N'UX_AssetHolding_AssetLocation', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetHolding_AssetLocation] ON [Assets].[AssetHolding] ([AssetId], [LocationId]) WHERE [LocationId] IS NOT NULL;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetHolding]'), N'UX_AssetHolding_AssetSite', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetHolding_AssetSite] ON [Assets].[AssetHolding] ([AssetId], [CustomerSiteId]) WHERE [CustomerSiteId] IS NOT NULL;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetHolding]'), N'IX_AssetHolding_Location', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHolding_Location] ON [Assets].[AssetHolding] ([LocationId]) WHERE [OnHandQuantity] > 0;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetDisposal]'), N'IX_AssetDisposal_AssetId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetDisposal_AssetId] ON [Assets].[AssetDisposal] ([AssetId]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetVehicleDetail]'), N'UX_AssetVehicleDetail_Registration', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetVehicleDetail_Registration] ON [Assets].[AssetVehicleDetail] ([RegistrationNumber]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetInstrumentDetail]'), N'IX_AssetInstrumentDetail_CalibrationDue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetInstrumentDetail_CalibrationDue] ON [Assets].[AssetInstrumentDetail] ([CalibrationEndDate]);   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Assets].[AssetEvent]'), N'IX_AssetEvent_Asset', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetEvent_Asset] ON [Assets].[AssetEvent] ([AssetId], [EventOnUtc] DESC);
GO
/* ===========================================================================
   SECTION 4 - [Allocations]  Allocation, acknowledgement, branch handover,
                              return evidence, return reversal
   ---------------------------------------------------------------------------
   NEW: [AssetHandover], [AllocationReturnReversal], and upload metadata plus
   [HandoverId] on [AssetReturnImage].
   The handbook's return runs Employee -> Branch IT store -> transit -> HO GRN.
   AMS had only allocation and shipment, so the branch-store stage - where the
   condition is judged and the photographs are taken - had nowhere to live.
   [AssetHandover] is that stage. It closes the allocation, holds the condition
   and remarks, and is what the dispatch and GRN screens select from.
   =========================================================================== */
IF OBJECT_ID(N'[Allocations].[AssetAllocation]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[AssetAllocation] (
        [Id]                   int           NOT NULL IDENTITY,
        [AssetId]              int           NOT NULL,   -- Assets.Asset, id only
        [EmployeeId]           int           NOT NULL,   -- Organization.Employee, id only
        [LocationId]           int           NULL,
        [AllocatedOnUtc]       datetime2     NOT NULL,
        [ExpectedReturnDate]   date          NULL,
        [ReturnRequestedOnUtc] datetime2     NULL,
        [ReturnedOnUtc]        datetime2     NULL,
        [ReceivedByUserId]     int           NULL,
        [Remarks]              nvarchar(500) NULL,
        [CreatedOnUtc]         datetime2     NOT NULL,
        [CreatedBy]            nvarchar(100) NULL,
        [ModifiedOnUtc]        datetime2     NULL,
        [ModifiedBy]           nvarchar(100) NULL,
        [RowVersion]           rowversion    NOT NULL,   -- R2-14
        CONSTRAINT [PK_AssetAllocation] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Allocations].[AssetAllocationApproval]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[AssetAllocationApproval] (
        [Id]                int           NOT NULL IDENTITY,
        [AssetId]           int           NOT NULL,
        [EmployeeId]        int           NOT NULL,
        [LocationId]        int           NULL,
        [Status]            nvarchar(30)  NOT NULL,   -- Pending|BranchApproved|Approved|Rejected|Cancelled
        [RequestedByUserId] int           NOT NULL,
        [RequestedOnUtc]    datetime2     NOT NULL,
        [DecidedByUserId]   int           NULL,
        [DecidedOnUtc]      datetime2     NULL,
        [DecisionRemarks]   nvarchar(500) NULL,
        [AllocationId]      int           NULL,
        [CreatedOnUtc]      datetime2     NOT NULL,
        [CreatedBy]         nvarchar(100) NULL,
        [ModifiedOnUtc]     datetime2     NULL,
        [ModifiedBy]        nvarchar(100) NULL,
        CONSTRAINT [PK_AssetAllocationApproval] PRIMARY KEY ([Id]),
        /*  R3-7: the vocabulary, in the only place that can enforce it.
            R2-7 gave the Movements and Handover status columns a CHECK and
            missed this one and the next. The application's smart-enum
            constants claim to spell what the database allows, and until now
            the database allowed anything at all. */
        CONSTRAINT [CK_AssetAllocationApproval_Status] CHECK ([Status] IN (N'Pending', N'BranchApproved', N'Approved', N'Rejected', N'Cancelled'))
    );
END
GO
IF OBJECT_ID(N'[Allocations].[AssetAcknowledgement]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[AssetAcknowledgement] (
        [Id]                   int           NOT NULL IDENTITY,
        [AllocationId]         int           NOT NULL,
        [Status]               nvarchar(30)  NOT NULL,
        [DocumentPath]         nvarchar(400) NULL,
        [SignatureImagePath]   nvarchar(400) NULL,
        [SignedOnUtc]          datetime2     NULL,
        [ManagerUserId]        int           NULL,
        [ManagerApprovedOnUtc] datetime2     NULL,
        [CreatedOnUtc]         datetime2     NOT NULL,
        [CreatedBy]            nvarchar(100) NULL,
        [ModifiedOnUtc]        datetime2     NULL,
        [ModifiedBy]           nvarchar(100) NULL,
        CONSTRAINT [PK_AssetAcknowledgement] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetAcknowledgement_Status] CHECK ([Status] IN (N'Pending', N'Signed', N'Approved')),   -- R3-7
        CONSTRAINT [FK_AssetAcknowledgement_AssetAllocation_AllocationId] FOREIGN KEY ([AllocationId]) REFERENCES [Allocations].[AssetAllocation] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  NEW - the employee-to-branch-store step.
    Status runs HandedOver -> InTransitToHo -> ReceivedAtHo, with Cancelled for
    a handover reversed before dispatch. [IsReceivedByHo] is kept as its own
    flag because the GRN queue reads it on every poll and a status string
    comparison is not what an index wants.
    Condition and remarks are MANDATORY: "returned" without a condition is the
    row that starts the argument six months later about who broke the hinge.
    R2-8: the CHECKs below tie Status to its companion columns, so the filtered
    unique indexes (which read CancelledOnUtc and Status) cannot be dodged by a
    row that sets one but not the other. */
IF OBJECT_ID(N'[Allocations].[AssetHandover]', N'U') IS NULL                        -- NEW
BEGIN
    CREATE TABLE [Allocations].[AssetHandover] (
        [Id]                  int           NOT NULL IDENTITY,
        [AllocationId]        int           NOT NULL,
        [AssetId]             int           NOT NULL,   -- Assets.Asset, id only
        [FromEmployeeId]      int           NOT NULL,   -- Organization.Employee, id only
        [BranchLocationId]    int           NOT NULL,   -- the branch IT store holding it
        [Status]              nvarchar(30)  NOT NULL,
        [ReturnCondition]     nvarchar(20)  NOT NULL,
        [Remarks]             nvarchar(500) NOT NULL,
        [HandedOverOnUtc]     datetime2     NOT NULL,
        [ReceivedByUserId]    int           NOT NULL,   -- branch admin who accepted it
        [MovementId]          int           NULL,       -- Movements.AssetMovement, id only
        [DispatchedOnUtc]     datetime2     NULL,
        [IsReceivedByHo]      bit           NOT NULL CONSTRAINT [DF_AssetHandover_IsReceivedByHo] DEFAULT (0),
        [ReceivedAtHoOnUtc]   datetime2     NULL,
        [ReceivedAtHoByUserId] int          NULL,
        [ReceiptRemarks]      nvarchar(500) NULL,       -- what GRN recorded
        [CancelledOnUtc]      datetime2     NULL,
        [CreatedOnUtc]        datetime2     NOT NULL,
        [CreatedBy]           nvarchar(100) NULL,
        [ModifiedOnUtc]       datetime2     NULL,
        [ModifiedBy]          nvarchar(100) NULL,
        [RowVersion]          rowversion    NOT NULL,   -- R2-14
        CONSTRAINT [PK_AssetHandover] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetHandover_Status] CHECK ([Status] IN (N'HandedOver', N'InTransitToHo', N'ReceivedAtHo', N'Cancelled')),
        CONSTRAINT [CK_AssetHandover_Condition] CHECK ([ReturnCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing')),
        CONSTRAINT [CK_AssetHandover_ReceiptPair] CHECK (([IsReceivedByHo] = 0 AND [ReceivedAtHoOnUtc] IS NULL) OR ([IsReceivedByHo] = 1 AND [ReceivedAtHoOnUtc] IS NOT NULL)),
        CONSTRAINT [CK_AssetHandover_ReceiptStatus] CHECK (([Status] = N'ReceivedAtHo' AND [IsReceivedByHo] = 1) OR ([Status] <> N'ReceivedAtHo' AND [IsReceivedByHo] = 0)),   -- R2-8
        CONSTRAINT [CK_AssetHandover_CancelPair] CHECK (([Status] = N'Cancelled' AND [CancelledOnUtc] IS NOT NULL) OR ([Status] <> N'Cancelled' AND [CancelledOnUtc] IS NULL)),   -- R2-8
        CONSTRAINT [FK_AssetHandover_AssetAllocation_AllocationId] FOREIGN KEY ([AllocationId]) REFERENCES [Allocations].[AssetAllocation] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Photographic evidence. [AllocationId] stays for the direct-return case;
    [HandoverId] is NEW and is what the branch screen writes.
    The five-image cap is an application rule - a CHECK cannot count siblings
    and a trigger to do it would be worse than the rule it enforces. */
IF OBJECT_ID(N'[Allocations].[AssetReturnImage]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[AssetReturnImage] (
        [Id]               int           NOT NULL IDENTITY,
        [AllocationId]     int           NOT NULL,
        [HandoverId]       int           NULL,           -- NEW
        [ImagePath]        nvarchar(400) NOT NULL,
        [Caption]          nvarchar(200) NULL,
        [ContentType]      nvarchar(120) NULL,           -- NEW
        [SizeBytes]        bigint        NULL,           -- NEW
        [UploadedByUserId] int           NULL,           -- NEW
        [CapturedOnUtc]    datetime2     NOT NULL,
        CONSTRAINT [PK_AssetReturnImage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssetReturnImage_AssetAllocation_AllocationId] FOREIGN KEY ([AllocationId]) REFERENCES [Allocations].[AssetAllocation] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssetReturnImage_AssetHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [Allocations].[AssetHandover] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  NEW - undoing a return that should not have happened.
    The reversal is a record, not an UPDATE that quietly clears ReturnedOnUtc:
    somebody has to answer for it, so who, when and why are columns. Clearing
    ReturnedOnUtc re-arms UX_AssetAllocation_OneActivePerAsset, so reversing a
    return onto an asset that has since been allocated to somebody else fails
    with 2601 rather than producing two live holders. */
IF OBJECT_ID(N'[Allocations].[AllocationReturnReversal]', N'U') IS NULL             -- NEW
BEGIN
    CREATE TABLE [Allocations].[AllocationReturnReversal] (
        [Id]                    int           NOT NULL IDENTITY,
        [AllocationId]          int           NOT NULL,
        [HandoverId]            int           NULL,
        [Reason]                nvarchar(500) NOT NULL,
        [PreviousReturnedOnUtc] datetime2     NOT NULL,
        [PreviousAssetStatusId] int           NULL,
        [RestoredEmployeeId]    int           NOT NULL,
        [ReversedByUserId]      int           NOT NULL,
        [ReversedOnUtc]         datetime2     NOT NULL,
        CONSTRAINT [PK_AllocationReturnReversal] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AllocationReturnReversal_AssetAllocation_AllocationId] FOREIGN KEY ([AllocationId]) REFERENCES [Allocations].[AssetAllocation] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AllocationReturnReversal_AssetHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [Allocations].[AssetHandover] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[Allocations].[CustomerSite]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[CustomerSite] (
        [Id]            int            NOT NULL IDENTITY,
        [CustomerName]  nvarchar(200)  NULL,       -- R3: from FieldAsset.CustomerName
        [SiteName]      nvarchar(200)  NOT NULL,
        [City]          nvarchar(100)  NULL,
        [Address]       nvarchar(500)  NULL,
        [Latitude]      decimal(9,6)   NULL,
        [Longitude]     decimal(9,6)   NULL,
        [IsActive]      bit            NOT NULL,
        [CreatedOnUtc]  datetime2      NOT NULL,
        [CreatedBy]     nvarchar(100)  NULL,
        [ModifiedOnUtc] datetime2      NULL,
        [ModifiedBy]    nvarchar(100)  NULL,
        CONSTRAINT [PK_CustomerSite] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Allocations].[AssetSiteMapping]', N'U') IS NULL
BEGIN
    CREATE TABLE [Allocations].[AssetSiteMapping] (
        [Id]             int           NOT NULL IDENTITY,
        [AssetId]        int           NOT NULL,
        [CustomerSiteId] int           NOT NULL,
        [CommissionedDate] date         NULL,       -- R3: from FieldAsset.CommissionedDate
        [MappedOnUtc]    datetime2     NOT NULL,
        [RemovedOnUtc]   datetime2     NULL,
        [CreatedOnUtc]   datetime2     NOT NULL,
        [CreatedBy]      nvarchar(100) NULL,
        [ModifiedOnUtc]  datetime2     NULL,
        [ModifiedBy]     nvarchar(100) NULL,
        CONSTRAINT [PK_AssetSiteMapping] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssetSiteMapping_CustomerSite_CustomerSiteId] FOREIGN KEY ([CustomerSiteId]) REFERENCES [Allocations].[CustomerSite] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  The index that makes double allocation impossible rather than unlikely.
    Filtered on ReturnedOnUtc IS NULL - unfiltered, every returned row would
    collide. Catch error 2601/2627 and translate to a readable 409. */
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetAllocation]'), N'UX_AssetAllocation_OneActivePerAsset', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetAllocation_OneActivePerAsset] ON [Allocations].[AssetAllocation] ([AssetId]) WHERE [ReturnedOnUtc] IS NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetAllocation]'), N'IX_AssetAllocation_LocationEmployee', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetAllocation_LocationEmployee] ON [Allocations].[AssetAllocation] ([LocationId], [EmployeeId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetAllocation]'), N'IX_AssetAllocation_Overdue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetAllocation_Overdue] ON [Allocations].[AssetAllocation] ([ExpectedReturnDate]) WHERE [ReturnedOnUtc] IS NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetAllocationApproval]'), N'IX_AssetAllocationApproval_Queue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetAllocationApproval_Queue] ON [Allocations].[AssetAllocationApproval] ([Status], [LocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetAcknowledgement]'), N'UX_AssetAcknowledgement_Allocation', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetAcknowledgement_Allocation] ON [Allocations].[AssetAcknowledgement] ([AllocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetHandover]'), N'UX_AssetHandover_OneOpenPerAsset', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetHandover_OneOpenPerAsset] ON [Allocations].[AssetHandover] ([AssetId]) WHERE [Status] = N'HandedOver';   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetHandover]'), N'UX_AssetHandover_OnePerAllocation', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetHandover_OnePerAllocation] ON [Allocations].[AssetHandover] ([AllocationId]) WHERE [CancelledOnUtc] IS NULL;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetHandover]'), N'IX_AssetHandover_BranchQueue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHandover_BranchQueue] ON [Allocations].[AssetHandover] ([BranchLocationId], [Status]);   -- NEW  dispatch picker
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetHandover]'), N'IX_AssetHandover_GrnQueue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHandover_GrnQueue] ON [Allocations].[AssetHandover] ([Status], [DispatchedOnUtc]) WHERE [IsReceivedByHo] = 0;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetReturnImage]'), N'IX_AssetReturnImage_AllocationId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetReturnImage_AllocationId] ON [Allocations].[AssetReturnImage] ([AllocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetReturnImage]'), N'IX_AssetReturnImage_HandoverId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetReturnImage_HandoverId] ON [Allocations].[AssetReturnImage] ([HandoverId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AllocationReturnReversal]'), N'IX_AllocationReturnReversal_AllocationId', N'IndexID') IS NULL
    CREATE INDEX [IX_AllocationReturnReversal_AllocationId] ON [Allocations].[AllocationReturnReversal] ([AllocationId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetSiteMapping]'), N'UX_AssetSiteMapping_OneActivePerAsset', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetSiteMapping_OneActivePerAsset] ON [Allocations].[AssetSiteMapping] ([AssetId]) WHERE [RemovedOnUtc] IS NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Allocations].[AssetSiteMapping]'), N'IX_AssetSiteMapping_CustomerSiteId', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetSiteMapping_CustomerSiteId] ON [Allocations].[AssetSiteMapping] ([CustomerSiteId]);
GO
/* ===========================================================================
   SECTION 5 - [Movements]  Shipments, dispatch batches, receipt (GRN)
   ---------------------------------------------------------------------------
   NEW: [MovementBatch], and [MovementBatchId] / [InvoiceDate] /
        [ReceiptRemarks] / [HandoverId] on [AssetMovement].
   The handbook dispatches five standby laptops under ONE invoice in ONE action
   and expects five traceable movements. Invoice, courier and challan belong to
   the consignment, not to each laptop, so they move up to [MovementBatch] and
   each [AssetMovement] points at it. Without that, five rows repeat the same
   invoice number and nothing stops the third one being edited.
   An asset in transit belongs to NEITHER branch. CurrentLocationId is changed
   on receipt, never on dispatch - marking it as arrived on dispatch makes it
   findable somewhere it is not.
   =========================================================================== */
IF OBJECT_ID(N'[Movements].[MovementBatch]', N'U') IS NULL                          -- NEW
BEGIN
    CREATE TABLE [Movements].[MovementBatch] (
        [Id]                 int           NOT NULL IDENTITY,
        [BatchNumber]        nvarchar(30)  NOT NULL,   -- from MovementBatchNumberSequence
        [FromLocationId]     int           NOT NULL,
        [ToLocationId]       int           NOT NULL,
        [MovementType]       nvarchar(20)  NOT NULL,   -- Transfer | HandoverToHO
        [InvoiceNumber]      nvarchar(80)  NOT NULL,
        [InvoiceDate]        date          NOT NULL,
        [CourierName]        nvarchar(100) NOT NULL,
        [TrackingNumber]     nvarchar(80)  NULL,
        [ChallanNumber]      nvarchar(80)  NULL,
        [DocumentPath]       nvarchar(400) NULL,
        [Remarks]            nvarchar(500) NOT NULL,
        [ItemCount]          int           NOT NULL,
        [DispatchedByUserId] int           NOT NULL,
        [ShippedOnUtc]       datetime2     NOT NULL,
        [ReceivedOnUtc]      datetime2     NULL,       -- set when the LAST item is received
        [CreatedOnUtc]       datetime2     NOT NULL,
        [CreatedBy]          nvarchar(100) NULL,
        [ModifiedOnUtc]      datetime2     NULL,
        [ModifiedBy]         nvarchar(100) NULL,
        [RowVersion]         rowversion    NOT NULL,   -- R2-14
        CONSTRAINT [PK_MovementBatch] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MovementBatch_DifferentBranches] CHECK ([FromLocationId] <> [ToLocationId]),
        CONSTRAINT [CK_MovementBatch_PositiveCount] CHECK ([ItemCount] > 0),
        CONSTRAINT [CK_MovementBatch_Type] CHECK ([MovementType] IN (N'Transfer', N'HandoverToHO'))
    );
END
GO
IF OBJECT_ID(N'[Movements].[AssetMovement]', N'U') IS NULL
BEGIN
    CREATE TABLE [Movements].[AssetMovement] (
        [Id]               int           NOT NULL IDENTITY,
        [AssetId]          int           NOT NULL,   -- Assets.Asset, id only
        [MovementBatchId]  int           NULL,       -- NEW  null = single-asset despatch
        [HandoverId]       int           NULL,       -- NEW  Allocations.AssetHandover, id only
        /*  R3: how much moved. A unit asset always moves as 1 (the default),
            so every existing row and every existing caller stays correct. */
        [Quantity]         decimal(18,3) NOT NULL CONSTRAINT [DF_AssetMovement_Quantity] DEFAULT (1),
        [MovementType]     nvarchar(20)  NOT NULL,   -- Transfer | HandoverToHO
        [FromLocationId]   int           NOT NULL,
        [ToLocationId]     int           NOT NULL,
        [Status]           nvarchar(20)  NOT NULL,   -- InTransit | Received | Cancelled
        [CourierName]      nvarchar(100) NULL,
        [TrackingNumber]   nvarchar(80)  NULL,
        [ChallanNumber]    nvarchar(80)  NULL,
        [InvoiceNumber]    nvarchar(80)  NULL,
        [InvoiceDate]      date          NULL,       -- NEW
        [DocumentPath]     nvarchar(400) NULL,
        [ShippedOnUtc]     datetime2     NOT NULL,
        [ReceivedOnUtc]    datetime2     NULL,
        [ReceivedByUserId] int           NULL,
        [ReceiptRemarks]   nvarchar(500) NULL,       -- NEW  what GRN recorded on receipt
        [Remarks]          nvarchar(500) NULL,
        [CreatedOnUtc]     datetime2     NOT NULL,
        [CreatedBy]        nvarchar(100) NULL,
        [ModifiedOnUtc]    datetime2     NULL,
        [ModifiedBy]       nvarchar(100) NULL,
        [RowVersion]       rowversion    NOT NULL,   -- R2-14
        CONSTRAINT [PK_AssetMovement] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssetMovement_QuantityPositive] CHECK ([Quantity] > 0),   -- R3
        CONSTRAINT [CK_AssetMovement_DifferentBranches] CHECK ([FromLocationId] <> [ToLocationId]),
        CONSTRAINT [CK_AssetMovement_Type] CHECK ([MovementType] IN (N'Transfer', N'HandoverToHO')),   -- R2-7
        CONSTRAINT [CK_AssetMovement_Status] CHECK ([Status] IN (N'InTransit', N'Received', N'Cancelled')),   -- R2-7
        CONSTRAINT [CK_AssetMovement_ReceiptPair] CHECK (([Status] = N'Received' AND [ReceivedOnUtc] IS NOT NULL) OR ([Status] <> N'Received' AND [ReceivedOnUtc] IS NULL)),   -- R2-7
        CONSTRAINT [FK_AssetMovement_MovementBatch_MovementBatchId] FOREIGN KEY ([MovementBatchId]) REFERENCES [Movements].[MovementBatch] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Movements].[MovementBatch]'), N'UX_MovementBatch_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_MovementBatch_Number] ON [Movements].[MovementBatch] ([BatchNumber]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Movements].[MovementBatch]'), N'IX_MovementBatch_Open', N'IndexID') IS NULL
    CREATE INDEX [IX_MovementBatch_Open] ON [Movements].[MovementBatch] ([ToLocationId], [ShippedOnUtc]) WHERE [ReceivedOnUtc] IS NULL;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Movements].[AssetMovement]'), N'IX_AssetMovement_Incoming', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetMovement_Incoming] ON [Movements].[AssetMovement] ([Status], [ToLocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Movements].[AssetMovement]'), N'IX_AssetMovement_Batch', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetMovement_Batch] ON [Movements].[AssetMovement] ([MovementBatchId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Movements].[AssetMovement]'), N'IX_AssetMovement_Handover', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetMovement_Handover] ON [Movements].[AssetMovement] ([HandoverId]);   -- NEW
GO
/* ===========================================================================
   SECTION 6 - [Transfers]  Employee / department / branch / cost-centre moves
   ---------------------------------------------------------------------------
   A transfer is the APPROVAL and the accounting consequence; the physical
   shipment it may create is a [Movements].[AssetMovement] linked by id.
   =========================================================================== */
IF OBJECT_ID(N'[Transfers].[AssetTransferRequest]', N'U') IS NULL
BEGIN
    CREATE TABLE [Transfers].[AssetTransferRequest] (
        [Id]                int           NOT NULL IDENTITY,
        [AssetId]           int           NOT NULL,
        [TransferType]      nvarchar(20)  NOT NULL,   -- Employee|Department|Branch|CostCenter
        [Status]            nvarchar(20)  NOT NULL,
        [FromEmployeeId]    int           NULL,
        [ToEmployeeId]      int           NULL,
        [FromDepartmentId]  int           NULL,
        [ToDepartmentId]    int           NULL,
        [FromLocationId]    int           NULL,
        [ToLocationId]      int           NULL,
        [FromCostCenter]    nvarchar(40)  NULL,
        [ToCostCenter]      nvarchar(40)  NULL,
        [RequestedByUserId] int           NOT NULL,
        [RequestedOnUtc]    datetime2     NOT NULL,
        [ApprovedByUserId]  int           NULL,
        [ApprovedOnUtc]     datetime2     NULL,
        [CompletedOnUtc]    datetime2     NULL,
        [Remarks]           nvarchar(500) NULL,
        [MovementId]        int           NULL,
        [SapSyncStatus]     nvarchar(20)  NOT NULL,
        [CreatedOnUtc]      datetime2     NOT NULL,
        [CreatedBy]         nvarchar(100) NULL,
        [ModifiedOnUtc]     datetime2     NULL,
        [ModifiedBy]        nvarchar(100) NULL,
        [RowVersion]        rowversion    NOT NULL,   -- R2-14
        CONSTRAINT [PK_AssetTransferRequest] PRIMARY KEY ([Id]),
        /*  R3-7. [TransferType] needs no list of its own: the TypePair CHECK
            below already refuses anything outside the four, because none of
            its branches can match another value. */
        CONSTRAINT [CK_AssetTransferRequest_Status] CHECK ([Status] IN (N'Pending', N'Approved', N'Rejected', N'Completed', N'Cancelled')),
        CONSTRAINT [CK_AssetTransferRequest_SapSyncStatus] CHECK ([SapSyncStatus] IN (N'NotRequired', N'Pending', N'Sent', N'Failed')),
        /*  The CHECK also guards the SAP inbound path, which does not come
            through the API and so cannot rely on endpoint validation. */
        CONSTRAINT [CK_AssetTransferRequest_TypePair] CHECK (([TransferType] = 'Employee'   AND [ToEmployeeId]   IS NOT NULL) OR
([TransferType] = 'Department' AND [ToDepartmentId] IS NOT NULL) OR
([TransferType] = 'Branch'     AND [ToLocationId]   IS NOT NULL) OR
([TransferType] = 'CostCenter' AND [ToCostCenter]   IS NOT NULL))
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Transfers].[AssetTransferRequest]'), N'IX_AssetTransferRequest_Queue', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetTransferRequest_Queue] ON [Transfers].[AssetTransferRequest] ([Status], [FromLocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Transfers].[AssetTransferRequest]'), N'IX_AssetTransferRequest_SapPending', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetTransferRequest_SapPending] ON [Transfers].[AssetTransferRequest] ([SapSyncStatus]) WHERE [SapSyncStatus] = 'Pending';
GO
/* ===========================================================================
   SECTION 7 - [ServiceDesk]  Tickets, classification, teams, templates,
                              conversation, attachments, SLA state
   ---------------------------------------------------------------------------
   NEW tables:  [SupportTeam], [SupportTeamMember], [ServiceTemplate],
                [RequestEmail]
   NEW columns: 17 SLA / intake-scheduling columns on [ServiceRequest],
                [EntryKind] / [IsInternal] / [Body] on [RequestHistory],
                upload metadata on [RequestAttachment]
   One pipeline, three shapes (SupportTicket, AssetIssue, NewService). The SLA
   COLUMNS live here, on the ticket, because they are ticket state; the SLA
   RULES live in [ServiceLevel] because they are policy. Keeping consumed
   minutes on the ticket is what lets the queue sort by overdue without
   recomputing every row on every page load.
   R2-6: [RequestEmail] is created BEFORE [RequestHistory] in this revision,
   because RequestHistory now carries a real FK to it.
   =========================================================================== */
IF OBJECT_ID(N'[ServiceDesk].[RequestStatus]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestStatus] (
        [Id]            int          NOT NULL IDENTITY,
        [StatusName]    nvarchar(50) NOT NULL,
        [IsClosedState] bit          NOT NULL,   -- what every open-queue filter tests
        [DisplayOrder]  int          NOT NULL,
        [IsActive]      bit          NOT NULL,
        /*  NEW - how this status treats the resolution clock.
            Running: Open, Assigned, In Progress, Standby Provided
            Paused:  On Hold, Waiting for User, Waiting for Spare
            Stopped: Resolved, Closed, Rejected
            It is a column and not a hard-coded list because "Awaiting Vendor"
            must be addable without a release, and the clock has to know. */
        [SlaClockBehaviour] nvarchar(10) NOT NULL
            CONSTRAINT [DF_RequestStatus_SlaClockBehaviour] DEFAULT (N'Running'),
        [CountsTechnicianTime] bit NOT NULL
            CONSTRAINT [DF_RequestStatus_CountsTechnicianTime] DEFAULT (0),
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_RequestStatus] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestStatus_SlaClockBehaviour] CHECK ([SlaClockBehaviour] IN (N'Running', N'Paused', N'Stopped'))
    );
END
GO
IF OBJECT_ID(N'[ServiceDesk].[RequestCategory]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestCategory] (
        [Id]           int           NOT NULL IDENTITY,
        [CategoryName] nvarchar(100) NOT NULL,
        [IsActive]     bit           NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_RequestCategory] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[ServiceDesk].[RequestSubCategory]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestSubCategory] (
        [Id]                int           NOT NULL IDENTITY,
        [RequestCategoryId] int           NOT NULL,
        [SubCategoryName]   nvarchar(100) NOT NULL,
        [IsActive]          bit           NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_RequestSubCategory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestSubCategory_RequestCategory_RequestCategoryId] FOREIGN KEY ([RequestCategoryId]) REFERENCES [ServiceDesk].[RequestCategory] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  NEW - the routing target. [RegionId] replaces name matching: a ticket's
    site resolves to a region, and the region resolves to a team. Adding a
    branch is then a master-data change, not a code change. */
IF OBJECT_ID(N'[ServiceDesk].[SupportTeam]', N'U') IS NULL                          -- NEW
BEGIN
    CREATE TABLE [ServiceDesk].[SupportTeam] (
        [Id]              int           NOT NULL IDENTITY,
        [TeamName]        nvarchar(100) NOT NULL,
        [RegionId]        int           NULL,   -- Organization.Region, id only
        [MailboxAddress]  nvarchar(256) NULL,
        [IsDefaultTeam]   bit           NOT NULL CONSTRAINT [DF_SupportTeam_IsDefaultTeam] DEFAULT (0),
        [IsActive]        bit           NOT NULL,
        [CreatedOnUtc]    datetime2     NOT NULL,
        [CreatedBy]       nvarchar(100) NULL,
        [ModifiedOnUtc]   datetime2     NULL,
        [ModifiedBy]      nvarchar(100) NULL,
        CONSTRAINT [PK_SupportTeam] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[ServiceDesk].[SupportTeamMember]', N'U') IS NULL                    -- NEW
BEGIN
    CREATE TABLE [ServiceDesk].[SupportTeamMember] (
        [SupportTeamId] int       NOT NULL,
        [UserId]        int       NOT NULL,   -- Identity.User, id only
        [IsLead]        bit       NOT NULL,
        [AddedOnUtc]    datetime2 NOT NULL,
        [AddedByUserId]         int           NULL,   -- A
        CONSTRAINT [PK_SupportTeamMember] PRIMARY KEY ([SupportTeamId], [UserId]),
        CONSTRAINT [FK_SupportTeamMember_SupportTeam_SupportTeamId] FOREIGN KEY ([SupportTeamId]) REFERENCES [ServiceDesk].[SupportTeam] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  NEW - the handbook's ServiceTemplates: a pre-written request an admin picks
    instead of retyping. Defaults only; the raised ticket owns its own values
    from the moment it is created, so editing a template never rewrites
    history. */
IF OBJECT_ID(N'[ServiceDesk].[ServiceTemplate]', N'U') IS NULL                      -- NEW
BEGIN
    CREATE TABLE [ServiceDesk].[ServiceTemplate] (
        [Id]                    int            NOT NULL IDENTITY,
        [TemplateName]          nvarchar(150)  NOT NULL,
        [RequestKind]           nvarchar(20)   NOT NULL,   -- SupportTicket|AssetIssue|NewService
        [RequestCategoryId]     int            NULL,
        [RequestSubCategoryId]  int            NULL,
        [DefaultPriority]       nvarchar(20)   NOT NULL,
        [DefaultSupportTeamId]  int            NULL,
        [SubjectTemplate]       nvarchar(300)  NOT NULL,
        [DescriptionTemplate]   nvarchar(4000) NULL,
        [RequiresAsset]         bit            NOT NULL CONSTRAINT [DF_ServiceTemplate_RequiresAsset] DEFAULT (0),
        [DisplayOrder]          int            NOT NULL,
        [IsActive]              bit            NOT NULL,
        [CreatedOnUtc]          datetime2      NOT NULL,
        [CreatedBy]             nvarchar(100)  NULL,
        [ModifiedOnUtc]         datetime2      NULL,
        [ModifiedBy]            nvarchar(100)  NULL,
        CONSTRAINT [PK_ServiceTemplate] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServiceTemplate_Kind] CHECK ([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService')),
        CONSTRAINT [CK_ServiceTemplate_Priority] CHECK ([DefaultPriority] IN (N'Low', N'Medium', N'High', N'Critical')),
        CONSTRAINT [FK_ServiceTemplate_RequestCategory_RequestCategoryId] FOREIGN KEY ([RequestCategoryId]) REFERENCES [ServiceDesk].[RequestCategory] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceTemplate_RequestSubCategory_RequestSubCategoryId] FOREIGN KEY ([RequestSubCategoryId]) REFERENCES [ServiceDesk].[RequestSubCategory] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceTemplate_SupportTeam_DefaultSupportTeamId] FOREIGN KEY ([DefaultSupportTeamId]) REFERENCES [ServiceDesk].[SupportTeam] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  The ticket.
    SLA block, all NEW:
      SlaPolicyId ....... which policy was in force AT INTAKE. Copied, not
                          looked up later - repricing history when somebody
                          edits a policy is how SLA reports stop being trusted.
      SlaStartOnUtc ..... when the clock actually started (after any hold)
      IsScheduledHold ... raised outside operating hours and waiting
      NextOperationalStartUtc / ScheduleHoldReason ... the exact minute it will
                          open, and the sentence shown to the requester
      Response/Resolution DueOnUtc ... recomputed on every resume
      *Minutes .......... OPERATIONAL minutes, not wall clock. A ticket held
                          over a weekend consumes nothing.
      IsSlaOverdue ...... persisted, not derived, so the queue can sort on it */
IF OBJECT_ID(N'[ServiceDesk].[ServiceRequest]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[ServiceRequest] (
        [Id]                        int            NOT NULL IDENTITY,
        [RequestNumber]             nvarchar(30)   NOT NULL,   -- TKT-2026-000123, from the sequence (global, no yearly reset - R2-17)
        [RequestKind]               nvarchar(20)   NOT NULL,   -- SupportTicket|AssetIssue|NewService, enforced by CK_ServiceRequest_Kind (R2-18)
        [Subject]                   nvarchar(300)  NOT NULL,
        [Description]               nvarchar(4000) NULL,
        [Priority]                  nvarchar(20)   NOT NULL,
        [RequestStatusId]           int            NOT NULL,
        [RequestCategoryId]         int            NULL,
        [RequestSubCategoryId]      int            NULL,
        [ServiceTemplateId]         int            NULL,       -- NEW
        [AssetId]                   int            NULL,       -- Assets.Asset, id only
        [ManualAssetText]           nvarchar(200)  NULL,
        [RequestedByEmployeeId]     int            NOT NULL,
        [OnBehalfOfEmployeeId]      int            NULL,
        [LocationId]                int            NULL,       -- the Site the handbook prints
        [AssignedToUserId]          int            NULL,
        [AssignedTeamId]            int            NULL,       -- NEW
        [AssignedOnUtc]             datetime2      NULL,
        [ResolvedOnUtc]             datetime2      NULL,
        [ClosedOnUtc]               datetime2      NULL,
        [Resolution]                nvarchar(4000) NULL,
        -- SLA and intake scheduling ------------------------------------ NEW
        [SlaPolicyId]               int            NULL,
        [SlaStartOnUtc]             datetime2      NULL,
        [IsScheduledHold]           bit            NOT NULL CONSTRAINT [DF_ServiceRequest_IsScheduledHold] DEFAULT (0),
        [NextOperationalStartUtc]   datetime2      NULL,
        [ScheduleHoldReason]        nvarchar(300)  NULL,
        [ResponseDueOnUtc]          datetime2      NULL,
        [ResolutionDueOnUtc]        datetime2      NULL,
        [FirstResponseOnUtc]        datetime2      NULL,
        [ResponseElapsedMinutes]    int            NULL,
        [ResolutionConsumedMinutes] int            NOT NULL CONSTRAINT [DF_ServiceRequest_ResolutionConsumedMinutes] DEFAULT (0),
        [TechnicianWorkingMinutes]  int            NOT NULL CONSTRAINT [DF_ServiceRequest_TechnicianWorkingMinutes] DEFAULT (0),
        [SlaPausedMinutes]          int            NOT NULL CONSTRAINT [DF_ServiceRequest_SlaPausedMinutes] DEFAULT (0),
        [SlaLastCalculatedOnUtc]    datetime2      NULL,
        [IsSlaPaused]               bit            NOT NULL CONSTRAINT [DF_ServiceRequest_IsSlaPaused] DEFAULT (0),
        [IsSlaOverdue]              bit            NOT NULL CONSTRAINT [DF_ServiceRequest_IsSlaOverdue] DEFAULT (0),
        -- ---------------------------------------------------------------
        [CreatedOnUtc]              datetime2      NOT NULL,
        [CreatedBy]                 nvarchar(100)  NULL,
        [ModifiedOnUtc]             datetime2      NULL,
        [ModifiedBy]                nvarchar(100)  NULL,
        [RowVersion]                rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_ServiceRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServiceRequest_Kind] CHECK ([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService')),   -- R2-18
        CONSTRAINT [CK_ServiceRequest_Priority] CHECK ([Priority] IN (N'Low', N'Medium', N'High', N'Critical')),
        CONSTRAINT [CK_ServiceRequest_SlaMinutes] CHECK ([ResolutionConsumedMinutes] >= 0 AND [TechnicianWorkingMinutes] >= 0 AND [SlaPausedMinutes] >= 0 AND ([ResponseElapsedMinutes] IS NULL OR [ResponseElapsedMinutes] >= 0)),
        CONSTRAINT [CK_ServiceRequest_ScheduledHold] CHECK ([IsScheduledHold] = 0 OR [NextOperationalStartUtc] IS NOT NULL),
        CONSTRAINT [FK_ServiceRequest_RequestStatus_RequestStatusId] FOREIGN KEY ([RequestStatusId]) REFERENCES [ServiceDesk].[RequestStatus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceRequest_RequestCategory_RequestCategoryId] FOREIGN KEY ([RequestCategoryId]) REFERENCES [ServiceDesk].[RequestCategory] ([Id]) ON DELETE NO ACTION,   -- R2-6
        CONSTRAINT [FK_ServiceRequest_RequestSubCategory_RequestSubCategoryId] FOREIGN KEY ([RequestSubCategoryId]) REFERENCES [ServiceDesk].[RequestSubCategory] ([Id]) ON DELETE NO ACTION,   -- R2-6
        CONSTRAINT [FK_ServiceRequest_SupportTeam_AssignedTeamId] FOREIGN KEY ([AssignedTeamId]) REFERENCES [ServiceDesk].[SupportTeam] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceRequest_ServiceTemplate_ServiceTemplateId] FOREIGN KEY ([ServiceTemplateId]) REFERENCES [ServiceDesk].[ServiceTemplate] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[ServiceDesk].[NewServiceRequestDetail]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[NewServiceRequestDetail] (
        [ServiceRequestId] int            NOT NULL,
        [NeedsEmail]       bit            NOT NULL,
        [NeedsErp]         bit            NOT NULL,
        [NeedsDms]         bit            NOT NULL,
        [NeedsVpn]         bit            NOT NULL,
        [RequiredByDate]   date           NULL,
        [Notes]            nvarchar(1000) NULL,
        CONSTRAINT [PK_NewServiceRequestDetail] PRIMARY KEY ([ServiceRequestId]),
        CONSTRAINT [FK_NewServiceRequestDetail_ServiceRequest_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[ServiceDesk].[NewServiceRequestItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[NewServiceRequestItem] (
        [Id]               int           NOT NULL IDENTITY,
        [ServiceRequestId] int           NOT NULL,
        [AssetTypeId]      int           NOT NULL,   -- Assets.AssetType, id only
        [Quantity]         int           NOT NULL,
        [Specification]    nvarchar(500) NULL,
        CONSTRAINT [PK_NewServiceRequestItem] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NewServiceRequestItem_PositiveQuantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_NewServiceRequestItem_ServiceRequest_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  NEW - e-mail sent FROM a ticket, or received INTO one.
    R2-6: created BEFORE [RequestHistory] so RequestHistory can hold a real FK.
    R2-4: [SentByUserId] is nullable - an Inbound message has no sending user;
    the CHECK requires it for Outbound. For Inbound rows [QueuedOnUtc] records
    the time the message was RECEIVED, and [Status] is 'Sent' on arrival.
    The Outbound row is written when the technician sends, and [SentOnUtc] only
    when the SMTP server has accepted it. Delivery goes through
    [Notifications].[EmailOutbox] so a dead SMTP host retries instead of losing
    the message, and [EmailOutboxId] is the link back to that attempt.
    SMTP acceptance is not inbox placement; [Status] records what we actually
    know rather than what we hope. */
IF OBJECT_ID(N'[ServiceDesk].[RequestEmail]', N'U') IS NULL                         -- NEW
BEGIN
    CREATE TABLE [ServiceDesk].[RequestEmail] (
        [Id]               int            NOT NULL IDENTITY,
        [ServiceRequestId] int            NOT NULL,
        [Direction]        nvarchar(10)   NOT NULL CONSTRAINT [DF_RequestEmail_Direction] DEFAULT (N'Outbound'),
        [ToAddresses]      nvarchar(1000) NOT NULL,   -- comma or semicolon separated
        [CcAddresses]      nvarchar(1000) NULL,
        [Subject]          nvarchar(300)  NOT NULL,
        [Body]             nvarchar(max)  NOT NULL,
        [IsHtml]           bit            NOT NULL CONSTRAINT [DF_RequestEmail_IsHtml] DEFAULT (1),
        [Status]           nvarchar(20)   NOT NULL,   -- Queued | Sent | Failed
        [LastError]        nvarchar(500)  NULL,
        [EmailOutboxId]    bigint         NULL,       -- Notifications.EmailOutbox, id only
        [SentByUserId]     int            NULL,       -- R2-4: NULL for Inbound
        [QueuedOnUtc]      datetime2      NOT NULL,   -- Inbound: the received time
        [SentOnUtc]        datetime2      NULL,
        CONSTRAINT [PK_RequestEmail] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestEmail_Status] CHECK ([Status] IN (N'Queued', N'Sent', N'Failed')),
        CONSTRAINT [CK_RequestEmail_Direction] CHECK ([Direction] IN (N'Outbound', N'Inbound')),
        CONSTRAINT [CK_RequestEmail_SentBy] CHECK ([Direction] = N'Inbound' OR [SentByUserId] IS NOT NULL),   -- R2-4
        CONSTRAINT [FK_RequestEmail_ServiceRequest_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  ONE timeline for the ticket. [EntryKind] is NEW so a technician's private
    note, an e-mail and an automatic SLA activation all land in the same
    chronological list the handbook calls Conversations and History - rather
    than a second notes table nobody joins.
    [IsInternal] hides a note from the requester without hiding it from audit.
    R2-6: [RequestEmailId] now carries a real FK (intra-schema, rule 2).
    NOTE on deletion: tickets are CLOSED, never deleted, and individual e-mail
    rows are never deleted - the NO ACTION FK enforces exactly that. */
IF OBJECT_ID(N'[ServiceDesk].[RequestHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestHistory] (
        [Id]                bigint         NOT NULL IDENTITY,
        [ServiceRequestId]  int            NOT NULL,
        [EntryKind]         nvarchar(20)   NOT NULL   -- NEW
                            CONSTRAINT [DF_RequestHistory_EntryKind] DEFAULT (N'Transition'),
        [EntryText]         nvarchar(500)  NOT NULL,
        [Body]              nvarchar(max)  NULL,      -- NEW  long note / e-mail body
        [IsInternal]        bit            NOT NULL   -- NEW
                            CONSTRAINT [DF_RequestHistory_IsInternal] DEFAULT (0),
        [FromStatusId]      int            NULL,
        [ToStatusId]        int            NULL,
        [AssignedToUserId]  int            NULL,
        [RequestEmailId]    int            NULL,      -- NEW
        [OccurredOnUtc]     datetime2      NOT NULL,
        [PerformedBy]       nvarchar(100)  NOT NULL,  -- 'SLA Automation' for machine entries
        CONSTRAINT [PK_RequestHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestHistory_EntryKind] CHECK ([EntryKind] IN (N'Transition', N'Note', N'Email', N'Automation', N'Sla', N'Escalation')),
        CONSTRAINT [FK_RequestHistory_ServiceRequest_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestHistory_RequestEmail_RequestEmailId] FOREIGN KEY ([RequestEmailId]) REFERENCES [ServiceDesk].[RequestEmail] ([Id]) ON DELETE NO ACTION   -- R2-6
    );
END
GO
/*  Files. [RequestEmailId] is NEW so an attachment sent with an e-mail is the
    same row as the attachment listed on the ticket, not a copy. */
IF OBJECT_ID(N'[ServiceDesk].[RequestAttachment]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestAttachment] (
        [Id]               int           NOT NULL IDENTITY,
        [ServiceRequestId] int           NOT NULL,
        [RequestEmailId]   int           NULL,           -- NEW
        [AttachmentType]   nvarchar(30)  NOT NULL,       -- Requester | Resolution | Email
        [FilePath]         nvarchar(400) NOT NULL,
        [FileName]         nvarchar(260) NULL,
        [ContentType]      nvarchar(120) NULL,           -- NEW
        [SizeBytes]        bigint        NULL,           -- NEW
        [UploadedByUserId] int           NULL,           -- NEW
        [UploadedOnUtc]    datetime2     NOT NULL,
        CONSTRAINT [PK_RequestAttachment] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestAttachment_Type] CHECK ([AttachmentType] IN (N'Requester', N'Resolution', N'Email')),
        CONSTRAINT [FK_RequestAttachment_ServiceRequest_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestAttachment_RequestEmail_RequestEmailId] FOREIGN KEY ([RequestEmailId]) REFERENCES [ServiceDesk].[RequestEmail] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestStatus]'), N'UX_RequestStatus_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestStatus_Name] ON [ServiceDesk].[RequestStatus] ([StatusName]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestCategory]'), N'UX_RequestCategory_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestCategory_Name] ON [ServiceDesk].[RequestCategory] ([CategoryName]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestSubCategory]'), N'UX_RequestSubCategory_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestSubCategory_Name] ON [ServiceDesk].[RequestSubCategory] ([RequestCategoryId], [SubCategoryName]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[SupportTeam]'), N'UX_SupportTeam_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SupportTeam_Name] ON [ServiceDesk].[SupportTeam] ([TeamName]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[SupportTeam]'), N'UX_SupportTeam_OneDefault', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SupportTeam_OneDefault] ON [ServiceDesk].[SupportTeam] ([IsDefaultTeam]) WHERE [IsDefaultTeam] = 1;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[SupportTeam]'), N'IX_SupportTeam_RegionId', N'IndexID') IS NULL
    CREATE INDEX [IX_SupportTeam_RegionId] ON [ServiceDesk].[SupportTeam] ([RegionId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceTemplate]'), N'UX_ServiceTemplate_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ServiceTemplate_Name] ON [ServiceDesk].[ServiceTemplate] ([TemplateName]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'UX_ServiceRequest_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ServiceRequest_Number] ON [ServiceDesk].[ServiceRequest] ([RequestNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'IX_ServiceRequest_Queue', N'IndexID') IS NULL
    CREATE INDEX [IX_ServiceRequest_Queue] ON [ServiceDesk].[ServiceRequest] ([RequestStatusId], [LocationId], [Priority]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'IX_ServiceRequest_Requester', N'IndexID') IS NULL
    CREATE INDEX [IX_ServiceRequest_Requester] ON [ServiceDesk].[ServiceRequest] ([RequestedByEmployeeId]);
/*  The technician queue: overdue first, then nearest due. Filtered to open
    tickets so the index stays small as closed ones accumulate. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'IX_ServiceRequest_SlaQueue', N'IndexID') IS NULL
    CREATE INDEX [IX_ServiceRequest_SlaQueue] ON [ServiceDesk].[ServiceRequest] ([IsSlaOverdue] DESC, [ResolutionDueOnUtc]) WHERE [ClosedOnUtc] IS NULL;   -- NEW
/*  The monitor job runs every minute and asks exactly this question. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'IX_ServiceRequest_ScheduledIntake', N'IndexID') IS NULL
    CREATE INDEX [IX_ServiceRequest_ScheduledIntake] ON [ServiceDesk].[ServiceRequest] ([NextOperationalStartUtc]) WHERE [IsScheduledHold] = 1;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ServiceRequest]'), N'IX_ServiceRequest_AssignedTeam', N'IndexID') IS NULL
    CREATE INDEX [IX_ServiceRequest_AssignedTeam] ON [ServiceDesk].[ServiceRequest] ([AssignedTeamId], [RequestStatusId]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestHistory]'), N'IX_RequestHistory_Request', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestHistory_Request] ON [ServiceDesk].[RequestHistory] ([ServiceRequestId], [OccurredOnUtc]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestHistory]'), N'IX_RequestHistory_RequestEmailId', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestHistory_RequestEmailId] ON [ServiceDesk].[RequestHistory] ([RequestEmailId]);   -- R2-6 FK support
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestEmail]'), N'IX_RequestEmail_Request', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestEmail_Request] ON [ServiceDesk].[RequestEmail] ([ServiceRequestId], [QueuedOnUtc]);   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestAttachment]'), N'IX_RequestAttachment_ServiceRequestId', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestAttachment_ServiceRequestId] ON [ServiceDesk].[RequestAttachment] ([ServiceRequestId]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[NewServiceRequestItem]'), N'IX_NewServiceRequestItem_ServiceRequestId', N'IndexID') IS NULL
    CREATE INDEX [IX_NewServiceRequestItem_ServiceRequestId] ON [ServiceDesk].[NewServiceRequestItem] ([ServiceRequestId]);
GO
/* ===========================================================================
   SECTION 8 - [ServiceLevel]   *** ENTIRELY NEW MODULE ***
               SLA policies, escalation, operational calendar, holidays
   ---------------------------------------------------------------------------
   This module answers two questions and nothing else:
       "Is this minute operational for this branch?"   (calendar tables)
       "Is this ticket late?"                          (policy tables)
   It is separate from ServiceDesk because the calendar is a property of the
   BRANCH, not of the ticket, and because a second consumer is already visible:
   allocation return-by dates want the same working-day arithmetic.
   TIME HANDLING - the rule that makes or breaks this module.
     Every column here that is a wall-clock time is time(0) and is LOCAL to the
     location, resolved through [Organization].[Branch].[TimeZoneId]. Every
     column that is an instant is datetime2 UTC and ends OnUtc. A branch opens
     at 09:00 where it stands; storing that as UTC breaks twice a year in any
     country with daylight saving and permanently in any second country.
     The SLA service converts once, at the edge, and reasons in local minutes.
   =========================================================================== */
/*  What "on time" means, per priority.
    Targets are stored in MINUTES: the handbook's days/hours/minutes editor is
    a presentation concern, and three columns to store one duration is three
    chances to disagree.
    Respect* flags exist because a Critical policy often ignores the calendar
    entirely - a production outage does not wait for Monday. */
IF OBJECT_ID(N'[ServiceLevel].[SlaPolicy]', N'U') IS NULL                           -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[SlaPolicy] (
        [Id]                      int           NOT NULL IDENTITY,
        [PolicyName]              nvarchar(150) NOT NULL,
        [Description]             nvarchar(500) NULL,
        [Priority]                nvarchar(20)  NOT NULL,   -- Low|Medium|High|Critical
        [ResponseTargetMinutes]   int           NOT NULL,
        [ResolutionTargetMinutes] int           NOT NULL,
        [RespectOperationalHours] bit           NOT NULL CONSTRAINT [DF_SlaPolicy_RespectOperationalHours] DEFAULT (1),
        [RespectHolidays]         bit           NOT NULL CONSTRAINT [DF_SlaPolicy_RespectHolidays]         DEFAULT (1),
        [RespectWeekends]         bit           NOT NULL CONSTRAINT [DF_SlaPolicy_RespectWeekends]         DEFAULT (1),
        [NearDueWarningMinutes]   int           NOT NULL CONSTRAINT [DF_SlaPolicy_NearDueWarningMinutes]   DEFAULT (30),
        [IsActive]                bit           NOT NULL,
        [CreatedOnUtc]            datetime2     NOT NULL,
        [CreatedBy]               nvarchar(100) NULL,
        [ModifiedOnUtc]           datetime2     NULL,
        [ModifiedBy]              nvarchar(100) NULL,
        /*  R2-1: no [RowVersion] - forbidden on system-versioned tables.
            R2-22: [ConcurrencyStamp] is the concurrency token, not
            [SysStartTime] - see the note on [Organization].[Employee]. */
        /*  System-versioned. SQL Server keeps every prior version of this row in
            [ServiceLevel].[SlaPolicyHistory], so what the record said on any past date can be read
            directly, without replaying a change log:
                SELECT * FROM [ServiceLevel].[SlaPolicy] FOR SYSTEM_TIME AS OF '2026-03-31T18:30:00'
            The period columns are HIDDEN, so SELECT * and EF's queries never see
            them. Declare the table .IsTemporal() so the model agrees. */
        [ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_SlaPolicy_ConcurrencyStamp] DEFAULT (NEWID()),   -- R2-22
        [SysStartTime] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [SysEndTime]   datetime2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
        PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]),
        CONSTRAINT [PK_SlaPolicy] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SlaPolicy_Priority] CHECK ([Priority] IN (N'Low', N'Medium', N'High', N'Critical')),
        CONSTRAINT [CK_SlaPolicy_Targets] CHECK ([ResponseTargetMinutes] > 0 AND [ResolutionTargetMinutes] > 0),
        /*  A response target longer than the resolution target is always a
            typo, and it silently makes every ticket look compliant. */
        CONSTRAINT [CK_SlaPolicy_ResponseWithinResolution] CHECK ([ResponseTargetMinutes] <= [ResolutionTargetMinutes]),
        CONSTRAINT [CK_SlaPolicy_NearDue] CHECK ([NearDueWarningMinutes] >= 0)
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ServiceLevel].[SlaPolicyHistory]));
END
GO
/*  Who is told, and when, if a target is missed.
    Up to four levels per type, per the handbook. [ThresholdPercent] is
    ADDITIVE to the target: 100 means at the due time, 150 means half the
    target again past it. Percent rather than absolute minutes so one
    escalation ladder can serve policies with different targets. */
IF OBJECT_ID(N'[ServiceLevel].[SlaEscalation]', N'U') IS NULL                       -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[SlaEscalation] (
        [Id]               int           NOT NULL IDENTITY,
        [SlaPolicyId]      int           NOT NULL,
        [EscalationType]   nvarchar(20)  NOT NULL,   -- Response | Resolution
        [Level]            int           NOT NULL,   -- 1..4
        [ThresholdPercent] int           NOT NULL,
        [RecipientType]    nvarchar(30)  NOT NULL,   -- AssignedTechnician|TeamLead|BranchAdmin|Manager|Custom
        [RecipientAddress] nvarchar(400) NULL,       -- required when RecipientType = Custom
        [Channel]          nvarchar(20)  NOT NULL,   -- Email | InApp | Both
        [IsEnabled]        bit           NOT NULL,
        [CreatedOnUtc]     datetime2     NOT NULL,
        [CreatedBy]        nvarchar(100) NULL,
        [ModifiedOnUtc]    datetime2     NULL,
        [ModifiedBy]       nvarchar(100) NULL,
        CONSTRAINT [PK_SlaEscalation] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SlaEscalation_Type] CHECK ([EscalationType] IN (N'Response', N'Resolution')),
        CONSTRAINT [CK_SlaEscalation_Level] CHECK ([Level] BETWEEN 1 AND 4),
        CONSTRAINT [CK_SlaEscalation_Threshold] CHECK ([ThresholdPercent] BETWEEN 1 AND 1000),
        CONSTRAINT [CK_SlaEscalation_Channel] CHECK ([Channel] IN (N'Email', N'InApp', N'Both')),
        CONSTRAINT [CK_SlaEscalation_RecipientType] CHECK ([RecipientType] IN (N'AssignedTechnician', N'TeamLead', N'BranchAdmin', N'Manager', N'Custom')),
        CONSTRAINT [CK_SlaEscalation_CustomAddress] CHECK ([RecipientType] <> N'Custom' OR [RecipientAddress] IS NOT NULL),
        CONSTRAINT [FK_SlaEscalation_SlaPolicy_SlaPolicyId] FOREIGN KEY ([SlaPolicyId]) REFERENCES [ServiceLevel].[SlaPolicy] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  Evidence that an escalation actually fired.
    The unique index is the whole point: the monitor runs every minute, and
    without it a ticket that stays overdue for a day sends 1,440 e-mails and
    everybody filters the address. Fire once per ticket per level - UNLESS the
    attempt failed. R2-3: the index excludes Outcome = 'Failed', so a failed
    queue attempt can be retried; a Skipped or Sent row still blocks a repeat. */
IF OBJECT_ID(N'[ServiceLevel].[SlaEscalationLog]', N'U') IS NULL                    -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[SlaEscalationLog] (
        [Id]               bigint        NOT NULL IDENTITY,
        [ServiceRequestId] int           NOT NULL,   -- ServiceDesk.ServiceRequest, id only
        [SlaEscalationId]  int           NOT NULL,
        [EscalationType]   nvarchar(20)  NOT NULL,
        [Level]            int           NOT NULL,
        [SentTo]           nvarchar(400) NOT NULL,
        [Channel]          nvarchar(20)  NOT NULL,
        [EmailOutboxId]    bigint        NULL,       -- Notifications.EmailOutbox, id only
        [Outcome]          nvarchar(20)  NOT NULL,   -- Queued | Sent | Failed | Skipped
        [FailureReason]    nvarchar(500) NULL,
        [FiredOnUtc]       datetime2     NOT NULL,
        CONSTRAINT [PK_SlaEscalationLog] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SlaEscalationLog_Outcome] CHECK ([Outcome] IN (N'Queued', N'Sent', N'Failed', N'Skipped')),
        CONSTRAINT [FK_SlaEscalationLog_SlaEscalation_SlaEscalationId] FOREIGN KEY ([SlaEscalationId]) REFERENCES [ServiceLevel].[SlaEscalation] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  One service calendar per location.
    [DeferFinalMinutes] and [DeferNewTicketsOnFriday] are the handbook's two
    oddest intake rules - "raised in the last thirty minutes" and "raised on a
    Friday goes to Monday". They are configuration, not code: a rule that is
    hard-coded is a rule the branch manager cannot turn off when it stops
    matching how the branch actually works.
    A location with no row falls back to Monday-Friday 09:00-18:00.
    R2-9: when [IsRoundTheClock] = 1 the window and break CHECKs are relaxed -
    a 24-hour branch no longer has to invent fake opening times. Store
    00:00/00:00 by convention; the SLA service ignores the times entirely when
    the flag is set. */
IF OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]', N'U') IS NULL             -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[LocationOperationalHour] (
        [Id]                      int           NOT NULL IDENTITY,
        [LocationId]              int           NOT NULL,   -- Organization.Branch, id only
        [IsRoundTheClock]         bit           NOT NULL CONSTRAINT [DF_LocationOperationalHour_IsRoundTheClock] DEFAULT (0),
        [StandardStartTime]       time(0)       NOT NULL,
        [StandardEndTime]         time(0)       NOT NULL,
        [BreakStartTime]          time(0)       NULL,
        [BreakEndTime]            time(0)       NULL,
        [DeferFinalMinutes]       int           NOT NULL CONSTRAINT [DF_LocationOperationalHour_DeferFinalMinutes] DEFAULT (30),
        [DeferNewTicketsOnFriday] bit           NOT NULL CONSTRAINT [DF_LocationOperationalHour_DeferOnFriday]    DEFAULT (0),
        [IsActive]                bit           NOT NULL,
        [CreatedOnUtc]            datetime2     NOT NULL,
        [CreatedBy]               nvarchar(100) NULL,
        [ModifiedOnUtc]           datetime2     NULL,
        [ModifiedBy]              nvarchar(100) NULL,
        /*  R2-1: no [RowVersion] - forbidden on system-versioned tables.
            R2-22: [ConcurrencyStamp] is the concurrency token, not
            [SysStartTime] - see the note on [Organization].[Employee]. */
        /*  System-versioned. SQL Server keeps every prior version of this row in
            [ServiceLevel].[LocationOperationalHourHistory], so what the record said on any past date can be read
            directly, without replaying a change log:
                SELECT * FROM [ServiceLevel].[LocationOperationalHour] FOR SYSTEM_TIME AS OF '2026-03-31T18:30:00'
            The period columns are HIDDEN, so SELECT * and EF's queries never see
            them. Declare the table .IsTemporal() so the model agrees. */
        [ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_LocationOperationalHour_ConcurrencyStamp] DEFAULT (NEWID()),   -- R2-22
        [SysStartTime] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [SysEndTime]   datetime2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
        PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]),
        CONSTRAINT [PK_LocationOperationalHour] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LocationOperationalHour_Window] CHECK ([IsRoundTheClock] = 1 OR [StandardEndTime] > [StandardStartTime]),   -- R2-9
        CONSTRAINT [CK_LocationOperationalHour_BreakPair] CHECK (([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime])),
        /*  A break outside the working window silently removes nothing, which
            looks like the configuration worked. */
        CONSTRAINT [CK_LocationOperationalHour_BreakInside] CHECK ([IsRoundTheClock] = 1 OR [BreakStartTime] IS NULL OR ([BreakStartTime] >= [StandardStartTime] AND [BreakEndTime] <= [StandardEndTime])),   -- R2-9
        CONSTRAINT [CK_LocationOperationalHour_DeferMinutes] CHECK ([DeferFinalMinutes] BETWEEN 0 AND 480)
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ServiceLevel].[LocationOperationalHourHistory]));
END
GO
/*  Seven rows per calendar, one per weekday.
    DayType = Standard means "inherit whatever the standard hours are NOW".
    That is the handbook's own rule and it matters: copying the standard times
    into each day at save time leaves seven stale copies the first time
    somebody edits the standard window. */
IF OBJECT_ID(N'[ServiceLevel].[LocationOperationalDay]', N'U') IS NULL              -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[LocationOperationalDay] (
        [Id]                        int          NOT NULL IDENTITY,
        [LocationOperationalHourId] int          NOT NULL,
        [DayOfWeek]                 tinyint      NOT NULL,   -- 0 = Sunday .. 6 = Saturday
        [IsWorkingDay]              bit          NOT NULL,
        [DayType]                   nvarchar(20) NOT NULL,   -- Standard|Custom|TwentyFourHour
        [StartTime]                 time(0)      NULL,       -- Custom only
        [EndTime]                   time(0)      NULL,       -- Custom only
        [BreakStartTime]            time(0)      NULL,
        [BreakEndTime]              time(0)      NULL,
        CONSTRAINT [PK_LocationOperationalDay] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LocationOperationalDay_DayOfWeek] CHECK ([DayOfWeek] BETWEEN 0 AND 6),
        CONSTRAINT [CK_LocationOperationalDay_DayType] CHECK ([DayType] IN (N'Standard', N'Custom', N'TwentyFourHour')),
        CONSTRAINT [CK_LocationOperationalDay_CustomTimes] CHECK ([DayType] <> N'Custom' OR ([StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [EndTime] > [StartTime])),
        CONSTRAINT [CK_LocationOperationalDay_CustomBreak] CHECK (([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime])),
        CONSTRAINT [FK_LocationOperationalDay_LocationOperationalHour_LocationOperationalHourId] FOREIGN KEY ([LocationOperationalHourId]) REFERENCES [ServiceLevel].[LocationOperationalHour] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  "First and third Saturday are working." Five occurrence rows per calendar.
    A Saturday must satisfy BOTH the weekday row and its occurrence row, which
    is why this cannot collapse into LocationOperationalDay. */
IF OBJECT_ID(N'[ServiceLevel].[LocationSaturdayRule]', N'U') IS NULL                -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[LocationSaturdayRule] (
        [Id]                        int     NOT NULL IDENTITY,
        [LocationOperationalHourId] int     NOT NULL,
        [Occurrence]                tinyint NOT NULL,   -- 1st .. 5th Saturday of the month
        [IsWorking]                 bit     NOT NULL,
        CONSTRAINT [PK_LocationSaturdayRule] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LocationSaturdayRule_Occurrence] CHECK ([Occurrence] BETWEEN 1 AND 5),
        CONSTRAINT [FK_LocationSaturdayRule_LocationOperationalHour_LocationOperationalHourId] FOREIGN KEY ([LocationOperationalHourId]) REFERENCES [ServiceLevel].[LocationOperationalHour] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  Holidays.
    [AppliesToAllLocations] is stored rather than inferred from "no rows in
    HolidayLocation", because an all-location holiday and a regional holiday
    somebody forgot to attach locations to are different mistakes and must not
    look identical.
    A recurring holiday keeps [HolidayDate] for the year it was created and is
    matched on month/day thereafter - Republic Day does not need re-entering
    every January.
    R2-10: [HolidayYear] must agree with [HolidayDate], and a recurrence day
    must exist in its month (no 30 February). A 29 February recurrence is
    OBSERVED ON 28 FEBRUARY in non-leap years - that is an application rule in
    the calendar service, stated here so nobody hunts for a missing row. */
IF OBJECT_ID(N'[ServiceLevel].[HolidayCalendar]', N'U') IS NULL                     -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[HolidayCalendar] (
        [Id]                    int           NOT NULL IDENTITY,
        [HolidayName]           nvarchar(150) NOT NULL,
        [HolidayDate]           date          NOT NULL,
        [HolidayYear]           int           NOT NULL,
        [HolidayType]           nvarchar(20)  NOT NULL,   -- Government|Festival|Regional|Optional
        [AppliesToAllLocations] bit           NOT NULL,
        [IsRecurringAnnually]   bit           NOT NULL CONSTRAINT [DF_HolidayCalendar_IsRecurring] DEFAULT (0),
        [RecurrenceMonth]       tinyint       NULL,
        [RecurrenceDay]         tinyint       NULL,
        [Remarks]               nvarchar(300) NULL,
        [IsActive]              bit           NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,
        [CreatedBy]             nvarchar(100) NULL,
        [ModifiedOnUtc]         datetime2     NULL,
        [ModifiedBy]            nvarchar(100) NULL,
        CONSTRAINT [PK_HolidayCalendar] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HolidayCalendar_Type] CHECK ([HolidayType] IN (N'Government', N'Festival', N'Regional', N'Optional')),
        CONSTRAINT [CK_HolidayCalendar_Recurrence] CHECK ([IsRecurringAnnually] = 0 OR ([RecurrenceMonth] BETWEEN 1 AND 12 AND [RecurrenceDay] >= 1 AND [RecurrenceDay] <= CASE WHEN [RecurrenceMonth] IN (4, 6, 9, 11) THEN 30 WHEN [RecurrenceMonth] = 2 THEN 29 ELSE 31 END)),   -- R2-10
        CONSTRAINT [CK_HolidayCalendar_YearMatchesDate] CHECK ([HolidayYear] = YEAR([HolidayDate])),   -- R2-10
        CONSTRAINT [CK_HolidayCalendar_Year] CHECK ([HolidayYear] BETWEEN 2000 AND 2100)
    );
END
GO
IF OBJECT_ID(N'[ServiceLevel].[HolidayLocation]', N'U') IS NULL                     -- NEW
BEGIN
    CREATE TABLE [ServiceLevel].[HolidayLocation] (
        [HolidayCalendarId] int NOT NULL,
        [LocationId]        int NOT NULL,   -- Organization.Branch, id only
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_HolidayLocation] PRIMARY KEY ([HolidayCalendarId], [LocationId]),
        CONSTRAINT [FK_HolidayLocation_HolidayCalendar_HolidayCalendarId] FOREIGN KEY ([HolidayCalendarId]) REFERENCES [ServiceLevel].[HolidayCalendar] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  One active policy per priority - two live "High" policies means the ticket
    gets whichever the query happened to order first. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[SlaPolicy]'), N'UX_SlaPolicy_ActivePriority', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SlaPolicy_ActivePriority] ON [ServiceLevel].[SlaPolicy] ([Priority]) WHERE [IsActive] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[SlaPolicy]'), N'UX_SlaPolicy_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SlaPolicy_Name] ON [ServiceLevel].[SlaPolicy] ([PolicyName]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[SlaEscalation]'), N'UX_SlaEscalation_PolicyTypeLevel', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SlaEscalation_PolicyTypeLevel] ON [ServiceLevel].[SlaEscalation] ([SlaPolicyId], [EscalationType], [Level]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[SlaEscalationLog]'), N'UX_SlaEscalationLog_OncePerLevel', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SlaEscalationLog_OncePerLevel] ON [ServiceLevel].[SlaEscalationLog] ([ServiceRequestId], [SlaEscalationId]) WHERE [Outcome] <> N'Failed';   -- R2-3
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[SlaEscalationLog]'), N'IX_SlaEscalationLog_Request', N'IndexID') IS NULL
    CREATE INDEX [IX_SlaEscalationLog_Request] ON [ServiceLevel].[SlaEscalationLog] ([ServiceRequestId], [FiredOnUtc]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]'), N'UX_LocationOperationalHour_Location', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_LocationOperationalHour_Location] ON [ServiceLevel].[LocationOperationalHour] ([LocationId]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[LocationOperationalDay]'), N'UX_LocationOperationalDay_Day', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_LocationOperationalDay_Day] ON [ServiceLevel].[LocationOperationalDay] ([LocationOperationalHourId], [DayOfWeek]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[LocationSaturdayRule]'), N'UX_LocationSaturdayRule_Occurrence', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_LocationSaturdayRule_Occurrence] ON [ServiceLevel].[LocationSaturdayRule] ([LocationOperationalHourId], [Occurrence]);
/*  The calendar lookup the SLA service makes for every span it measures. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[HolidayCalendar]'), N'IX_HolidayCalendar_YearDate', N'IndexID') IS NULL
    CREATE INDEX [IX_HolidayCalendar_YearDate] ON [ServiceLevel].[HolidayCalendar] ([HolidayYear], [HolidayDate]) WHERE [IsActive] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[HolidayCalendar]'), N'IX_HolidayCalendar_Recurring', N'IndexID') IS NULL
    CREATE INDEX [IX_HolidayCalendar_Recurring] ON [ServiceLevel].[HolidayCalendar] ([RecurrenceMonth], [RecurrenceDay]) WHERE [IsRecurringAnnually] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceLevel].[HolidayLocation]'), N'IX_HolidayLocation_LocationId', N'IndexID') IS NULL
    CREATE INDEX [IX_HolidayLocation_LocationId] ON [ServiceLevel].[HolidayLocation] ([LocationId]);
GO
/* ===========================================================================
   SECTION 9 - [Contracts]  Contracts, covered assets, documents, reminders
   ---------------------------------------------------------------------------
   NEW: [ContractReminderSetting]; upload metadata on [ContractDocument];
        [EmailOutboxId] / [Outcome] on [ContractReminderLog].
   Reminder windows were 60/30/15/7 days compiled into the job. One AMC that
   needs 90 days' notice meant a release. They are rows now: a NULL ContractId
   is the organisation default, a non-NULL one overrides it for that contract.
   =========================================================================== */
IF OBJECT_ID(N'[Contracts].[Contract]', N'U') IS NULL
BEGIN
    CREATE TABLE [Contracts].[Contract] (
        [Id]                  int            NOT NULL IDENTITY,
        [ContractNumber]      nvarchar(40)   NOT NULL,
        [ContractName]        nvarchar(200)  NOT NULL,
        [ContractType]        nvarchar(20)   NOT NULL,   -- Amc|Warranty|Lease|Licence|Service|Insurance  -- R3
        [VendorId]            int            NULL,       -- Organization.Vendor, id only
        [StartDate]           date           NOT NULL,
        [EndDate]             date           NOT NULL,
        [ContractValue]       decimal(18,2)  NULL,
        [LicensedSeats]       int            NULL,
        [LicenseKeyEncrypted] varbinary(max) NULL,
        [AutoRenew]           bit            NOT NULL,
        [RenewalCount]        int            NOT NULL,
        [Remarks]             nvarchar(1000) NULL,
        [IsDeleted]           bit            NOT NULL,
        [CreatedOnUtc]        datetime2      NOT NULL,
        [CreatedBy]           nvarchar(100)  NULL,
        [ModifiedOnUtc]       datetime2      NULL,
        [ModifiedBy]          nvarchar(100)  NULL,
        /*  R2-1: no [RowVersion] - forbidden on system-versioned tables.
            R2-22: [ConcurrencyStamp] is the concurrency token, not
            [SysStartTime] - see the note on [Organization].[Employee]. */
        /*  System-versioned. SQL Server keeps every prior version of this row in
            [Contracts].[ContractHistory], so what the record said on any past date can be read
            directly, without replaying a change log:
                SELECT * FROM [Contracts].[Contract] FOR SYSTEM_TIME AS OF '2026-03-31T18:30:00'
            The period columns are HIDDEN, so SELECT * and EF's queries never see
            them. Declare the table .IsTemporal() so the model agrees. */
        [ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_Contract_ConcurrencyStamp] DEFAULT (NEWID()),   -- R2-22
        [SysStartTime] datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [SysEndTime]   datetime2(7) GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL,
        PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]),
        CONSTRAINT [PK_Contract] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Contract_Window] CHECK ([EndDate] >= [StartDate])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Contracts].[ContractHistory]));
END
GO
IF OBJECT_ID(N'[Contracts].[ContractAsset]', N'U') IS NULL
BEGIN
    CREATE TABLE [Contracts].[ContractAsset] (
        [Id]           int       NOT NULL IDENTITY,
        [ContractId]   int       NOT NULL,
        [AssetId]      int       NOT NULL,   -- Assets.Asset, id only
        [LinkedOnUtc]  datetime2 NOT NULL,
        [LinkedByUserId]        int           NULL,   -- A
        CONSTRAINT [PK_ContractAsset] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractAsset_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts].[Contract] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Contracts].[ContractDocument]', N'U') IS NULL
BEGIN
    CREATE TABLE [Contracts].[ContractDocument] (
        [Id]               int           NOT NULL IDENTITY,
        [ContractId]       int           NOT NULL,
        [FilePath]         nvarchar(400) NOT NULL,
        [FileName]         nvarchar(260) NULL,
        [ContentType]      nvarchar(120) NULL,   -- NEW
        [SizeBytes]        bigint        NULL,   -- NEW
        [UploadedByUserId] int           NULL,   -- NEW
        [UploadedOnUtc]    datetime2     NOT NULL,
        CONSTRAINT [PK_ContractDocument] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractDocument_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts].[Contract] ([Id]) ON DELETE CASCADE
    );
END
GO
IF OBJECT_ID(N'[Contracts].[ContractReminderSetting]', N'U') IS NULL                -- NEW
BEGIN
    CREATE TABLE [Contracts].[ContractReminderSetting] (
        [Id]                int            NOT NULL IDENTITY,
        [ContractId]        int            NULL,   -- NULL = organisation default
        [DaysBeforeExpiry]  int            NOT NULL,
        [Recipients]        nvarchar(1000) NULL,   -- blank = the contract owner and vendor contact
        [Channel]           nvarchar(20)   NOT NULL CONSTRAINT [DF_ContractReminderSetting_Channel] DEFAULT (N'Email'),
        [IsActive]          bit            NOT NULL,
        [CreatedOnUtc]      datetime2      NOT NULL,
        [CreatedBy]         nvarchar(100)  NULL,
        [ModifiedOnUtc]     datetime2      NULL,
        [ModifiedBy]        nvarchar(100)  NULL,
        CONSTRAINT [PK_ContractReminderSetting] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ContractReminderSetting_Days] CHECK ([DaysBeforeExpiry] BETWEEN 1 AND 365),
        CONSTRAINT [CK_ContractReminderSetting_Channel] CHECK ([Channel] IN (N'Email', N'InApp', N'Both')),
        CONSTRAINT [FK_ContractReminderSetting_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts].[Contract] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  Proof a reminder went out, and the thing that stops it going out twice.
    The daily job is idempotent because of UX_ContractReminderLog_OncePerThreshold,
    not because it remembers having run.
    R2-2: [ExpiryDateSnapshot] is the [EndDate] the reminder was measured
    against, and it is part of the unique key - a renewed contract (same row,
    new EndDate) earns its 60/30/15/7 reminders again for the NEW expiry.
    R2-3: the unique index excludes Outcome = 'Failed', so a send that failed
    to queue can be retried tomorrow instead of being blocked forever. */
IF OBJECT_ID(N'[Contracts].[ContractReminderLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [Contracts].[ContractReminderLog] (
        [Id]                 bigint        NOT NULL IDENTITY,
        [ContractId]         int           NOT NULL,
        [DaysBeforeExpiry]   int           NOT NULL,
        [ExpiryDateSnapshot] date          NOT NULL,   -- R2-2
        [SentOnDate]         date          NOT NULL,
        [SentTo]             nvarchar(400) NULL,
        [EmailOutboxId]      bigint        NULL,   -- NEW  Notifications.EmailOutbox, id only
        [Outcome]            nvarchar(20)  NOT NULL   -- NEW
                             CONSTRAINT [DF_ContractReminderLog_Outcome] DEFAULT (N'Queued'),
        CONSTRAINT [PK_ContractReminderLog] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ContractReminderLog_Outcome] CHECK ([Outcome] IN (N'Queued', N'Sent', N'Failed')),
        CONSTRAINT [FK_ContractReminderLog_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts].[Contract] ([Id]) ON DELETE CASCADE
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[Contract]'), N'UX_Contract_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_Contract_Number] ON [Contracts].[Contract] ([ContractNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[Contract]'), N'IX_Contract_EndDate', N'IndexID') IS NULL
    CREATE INDEX [IX_Contract_EndDate] ON [Contracts].[Contract] ([EndDate]);
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[ContractAsset]'), N'UX_ContractAsset_NoDuplicates', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ContractAsset_NoDuplicates] ON [Contracts].[ContractAsset] ([ContractId], [AssetId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[ContractDocument]'), N'IX_ContractDocument_ContractId', N'IndexID') IS NULL
    CREATE INDEX [IX_ContractDocument_ContractId] ON [Contracts].[ContractDocument] ([ContractId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[ContractReminderLog]'), N'UX_ContractReminderLog_OncePerThreshold', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ContractReminderLog_OncePerThreshold] ON [Contracts].[ContractReminderLog] ([ContractId], [DaysBeforeExpiry], [ExpiryDateSnapshot]) WHERE [Outcome] <> N'Failed';   -- R2-2, R2-3
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[ContractReminderSetting]'), N'UX_ContractReminderSetting_Default', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ContractReminderSetting_Default] ON [Contracts].[ContractReminderSetting] ([DaysBeforeExpiry]) WHERE [ContractId] IS NULL;   -- NEW
IF INDEXPROPERTY(OBJECT_ID(N'[Contracts].[ContractReminderSetting]'), N'UX_ContractReminderSetting_PerContract', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ContractReminderSetting_PerContract] ON [Contracts].[ContractReminderSetting] ([ContractId], [DaysBeforeExpiry]) WHERE [ContractId] IS NOT NULL;   -- NEW
GO
/* ===========================================================================
   SECTION 10 - [Verification]  Physical verification cycles (mobile)
   ---------------------------------------------------------------------------
   This is the handbook's "asset audit", done properly: QR scan, GPS, photo and
   a working-condition judgement, captured offline on a phone and synced.
   =========================================================================== */
IF OBJECT_ID(N'[Verification].[PhysicalVerificationCycle]', N'U') IS NULL
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationCycle] (
        [Id]            int           NOT NULL IDENTITY,
        [CycleName]     nvarchar(120) NOT NULL,
        [StartDate]     date          NOT NULL,
        [EndDate]       date          NULL,
        [BranchId]      int           NOT NULL,   -- Organization.Branch, id only
        [TotalAssetCount] int         NOT NULL,   -- frozen when the cycle opens
        [IsActive]      bit           NOT NULL,
        [ClosedOnUtc]   datetime2     NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerificationCycle] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Verification].[PhysicalVerificationAssignment]', N'U') IS NULL
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationAssignment] (
        [PhysicalVerificationCycleId] int           NOT NULL,
        [AuditorUserId]              int           NOT NULL,   -- Identity.User, id only
        [AssignedOnUtc]               datetime2     NOT NULL,
        [AssignedBy]                  nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerificationAssignment] PRIMARY KEY ([PhysicalVerificationCycleId], [AuditorUserId]),
        CONSTRAINT [FK_PhysicalVerificationAssignment_Cycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[Verification].[PhysicalVerificationCycleLocation]', N'U') IS NULL
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationCycleLocation] (
        [PhysicalVerificationCycleId] int NOT NULL,
        [BranchId]                    int NOT NULL,   -- Organization.Branch, id only
        CONSTRAINT [PK_PhysicalVerificationCycleLocation] PRIMARY KEY ([PhysicalVerificationCycleId], [BranchId]),
        CONSTRAINT [FK_PhysicalVerificationCycleLocation_Cycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF OBJECT_ID(N'[Verification].[PhysicalVerification]', N'U') IS NULL
BEGIN
    CREATE TABLE [Verification].[PhysicalVerification] (
        [Id]                          int           NOT NULL IDENTITY,
        [PhysicalVerificationCycleId] int           NOT NULL,
        [AssetId]                     int           NOT NULL,
        [ClientCaptureId]             uniqueidentifier NULL,    -- R2-21: generated on the phone at capture
        /*  R3: counting versus sighting. A unit asset is sighted once per
            cycle; a bulk line is counted wherever it is held, so the two
            kinds of row obey different uniqueness rules (see the split
            indexes below) and are told apart by this flag. */
        [IsBulkCount]                 bit           NOT NULL CONSTRAINT [DF_PhysicalVerification_IsBulkCount] DEFAULT (0),
        [CountedQuantity]             decimal(18,3) NULL,       -- R3: what was on the floor
        [ExpectedQuantitySnapshot]    decimal(18,3) NULL,       -- R3: the holding when the sheet was issued
        [ScannedQrValue]              nvarchar(200) NULL,
        [HasQrMismatch]               bit           NOT NULL,   -- the tag did not match the asset
        [WorkingCondition]            nvarchar(20)  NOT NULL,   -- R2-20
        [SerialVerified]              bit           NOT NULL,
        [GpsLatitude]                 decimal(9,6)  NULL,
        [GpsLongitude]                decimal(9,6)  NULL,
        [GpsAccuracyMetres]           decimal(9,2)  NULL,
        [ReferenceLatitude]           decimal(9,6)  NULL,
        [ReferenceLongitude]          decimal(9,6)  NULL,
        [DistanceFromLocationMetres]  decimal(12,2) NULL,
        [AllowedRadiusMetres]         decimal(12,2) NULL,
        [GpsValidationStatus]         nvarchar(20)  NULL,
        [HasLocationMismatch]         bit           NOT NULL CONSTRAINT [DF_PhysicalVerification_HasLocationMismatch] DEFAULT (0),
        [IsMockLocation]              bit           NULL,
        [GpsValidationMessage]        nvarchar(500) NULL,
        [PhotoPath]                   nvarchar(400) NULL,
        [LocationId]                  int           NULL,
        [HolderEmployeeId]            int           NULL,
        [StatusUpdatedToId]           int           NULL,
        [VerifiedByUserId]            int           NOT NULL,
        [VerifiedOnUtc]               datetime2     NOT NULL,
        [Remarks]                     nvarchar(500) NULL,
        [CreatedOnUtc]                datetime2     NOT NULL,
        [CreatedBy]                   nvarchar(100) NULL,
        [ModifiedOnUtc]               datetime2     NULL,
        [ModifiedBy]                  nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerification] PRIMARY KEY ([Id]),
        /*  R3: a bulk count without a number is not a count. */
        CONSTRAINT [CK_PhysicalVerification_BulkHasCount] CHECK ([IsBulkCount] = 0 OR [CountedQuantity] IS NOT NULL),
        CONSTRAINT [CK_PhysicalVerification_CountNonNegative] CHECK ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0),
        CONSTRAINT [CK_PhysicalVerification_Condition] CHECK ([WorkingCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing')),   -- R2-20
        CONSTRAINT [CK_PhysicalVerification_GpsAccuracy] CHECK ([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0),
        CONSTRAINT [CK_PhysicalVerification_ReferenceLatitude] CHECK ([ReferenceLatitude] IS NULL OR ([ReferenceLatitude] >= -90 AND [ReferenceLatitude] <= 90)),
        CONSTRAINT [CK_PhysicalVerification_ReferenceLongitude] CHECK ([ReferenceLongitude] IS NULL OR ([ReferenceLongitude] >= -180 AND [ReferenceLongitude] <= 180)),
        CONSTRAINT [CK_PhysicalVerification_Distance] CHECK ([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0),
        CONSTRAINT [CK_PhysicalVerification_AllowedRadius] CHECK ([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0),
        CONSTRAINT [CK_PhysicalVerification_GpsValidationStatus] CHECK ([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN (N'NotValidated', N'InsideGeofence', N'OutsideGeofence', N'ReferenceUnavailable')),
        CONSTRAINT [FK_PhysicalVerification_PhysicalVerificationCycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id]) ON DELETE NO ACTION
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerificationCycle]'), N'UX_PhysicalVerificationCycle_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_PhysicalVerificationCycle_Name] ON [Verification].[PhysicalVerificationCycle] ([CycleName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerificationCycle]'), N'UX_PhysicalVerificationCycle_OneActivePerBranch', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_PhysicalVerificationCycle_OneActivePerBranch] ON [Verification].[PhysicalVerificationCycle] ([BranchId], [IsActive]) WHERE [IsActive] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerificationAssignment]'), N'IX_PhysicalVerificationAssignment_AuditorUserId', N'IndexID') IS NULL
    CREATE INDEX [IX_PhysicalVerificationAssignment_AuditorUserId] ON [Verification].[PhysicalVerificationAssignment] ([AuditorUserId]);
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerificationCycleLocation]'), N'IX_PhysicalVerificationCycleLocation_BranchId', N'IndexID') IS NULL
    CREATE INDEX [IX_PhysicalVerificationCycleLocation_BranchId] ON [Verification].[PhysicalVerificationCycleLocation] ([BranchId]);
/*  R3: this WAS one index over ([CycleId], [AssetId]). It splits, because
    the two kinds of row mean different things:
      unit rows - one sighting per asset per cycle, exactly as before.
      bulk rows - one count per asset per PLACE per cycle. Counting the same
                  bulk line at four branches is the correct answer, not a
                  duplicate, and the old single index called it a conflict.
    Both stay UNIQUE, so design rule 6 still holds: a second technician
    submitting the same sighting collides on 2601 and gets a 409, and no
    read-then-write check is involved. */
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerification]'), N'UX_PhysicalVerification_OnePerUnitAssetPerCycle', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_PhysicalVerification_OnePerUnitAssetPerCycle] ON [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId], [AssetId]) WHERE [IsBulkCount] = 0;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerification]'), N'UX_PhysicalVerification_OneBulkCountPerPlacePerCycle', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_PhysicalVerification_OneBulkCountPerPlacePerCycle] ON [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId], [AssetId], [LocationId]) WHERE [IsBulkCount] = 1;   -- R3
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerification]'), N'IX_PhysicalVerification_Exceptions', N'IndexID') IS NULL
    CREATE INDEX [IX_PhysicalVerification_Exceptions] ON [Verification].[PhysicalVerification] ([LocationId], [WorkingCondition]);
/*  R2-21: the phone generates [ClientCaptureId] when the technician records the
    capture, and sends the same value on every retry. Filtered, because a
    verification recorded from the web has no capture id and many rows may
    legitimately hold NULL.
    This is what lets the two duplicate cases be told apart, which matters
    because they deserve different words on a technician's screen:
      2601 on THIS index                     - the same phone sent it twice.
                                               The row already exists; answer
                                               with it and let the device tick
                                               the capture off.
      2601 on UX_..._OnePerAssetPerCycle     - somebody else verified this
                                               asset first. A real conflict.
    Without this index the server sees only the second case and has to call
    every retry a conflict, which teaches technicians to ignore conflicts. */
IF INDEXPROPERTY(OBJECT_ID(N'[Verification].[PhysicalVerification]'), N'UX_PhysicalVerification_ClientCapture', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_PhysicalVerification_ClientCapture] ON [Verification].[PhysicalVerification] ([ClientCaptureId]) WHERE [ClientCaptureId] IS NOT NULL;   -- R2-21
GO
/* ===========================================================================
   SECTION 11 - [Discovery]  Agent inventory, health, installed software
   =========================================================================== */
IF OBJECT_ID(N'[Discovery].[AgentApiKey]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[AgentApiKey] (
        [Id]            int           NOT NULL IDENTITY,
        [KeyName]       nvarchar(100) NOT NULL,
        [KeyPrefix]     nvarchar(12)  NOT NULL,   -- lookup handle; the key itself is never stored
        [KeyHash]       nvarchar(500) NOT NULL,
        [LastUsedOnUtc] datetime2     NULL,
        [RevokedOnUtc]  datetime2     NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]  datetime2     NOT NULL,
        [CreatedBy]     nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2     NULL,
        [ModifiedBy]    nvarchar(100) NULL,
        CONSTRAINT [PK_AgentApiKey] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Discovery].[DiscoveredDevice]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[DiscoveredDevice] (
        [Id]              int            NOT NULL IDENTITY,
        [Hostname]        nvarchar(100)  NOT NULL,
        [SerialNumber]    nvarchar(100)  NULL,
        [Manufacturer]    nvarchar(150)  NULL,
        [Model]           nvarchar(150)  NULL,
        [OperatingSystem] nvarchar(150)  NULL,
        [MacAddress]      nvarchar(50)   NULL,
        [RawPayloadJson]  nvarchar(max)  NULL,
        [Status]          nvarchar(20)   NOT NULL,   -- New|Linked|Registered|Ignored
        [LinkedAssetId]   int            NULL,
        [FirstSeenOnUtc]  datetime2      NOT NULL,
        [LastSeenOnUtc]   datetime2      NOT NULL,
        [CreatedOnUtc]    datetime2      NOT NULL,
        [CreatedBy]       nvarchar(100)  NULL,
        [ModifiedOnUtc]   datetime2      NULL,
        [ModifiedBy]      nvarchar(100)  NULL,
        CONSTRAINT [PK_DiscoveredDevice] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Discovery].[AssetHealth]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[AssetHealth] (
        [AssetId]              int           NOT NULL,
        [Hostname]             nvarchar(100) NOT NULL,
        [CpuPercent]           decimal(5,2)  NOT NULL,   -- decimal: compared against alert thresholds
        [MemoryPercent]        decimal(5,2)  NOT NULL,
        [SystemDrivePercent]   decimal(5,2)  NOT NULL,
        [BatteryHealthPercent] decimal(5,2)  NULL,
        [UptimeHours]          int           NOT NULL,
        [LoggedInUser]         nvarchar(150) NULL,
        [LastSeenOnUtc]        datetime2     NOT NULL,
        CONSTRAINT [PK_AssetHealth] PRIMARY KEY ([AssetId])
    );
END
GO
IF OBJECT_ID(N'[Discovery].[AssetHealthHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[AssetHealthHistory] (
        [Id]                 bigint       NOT NULL IDENTITY,
        [AssetId]            int          NOT NULL,
        [CpuPercent]         decimal(5,2) NOT NULL,
        [MemoryPercent]      decimal(5,2) NOT NULL,
        [SystemDrivePercent] decimal(5,2) NOT NULL,
        [CapturedOnUtc]      datetime2    NOT NULL,
        CONSTRAINT [PK_AssetHealthHistory] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Discovery].[AssetInstalledSoftware]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[AssetInstalledSoftware] (
        [Id]             bigint        NOT NULL IDENTITY,
        [AssetId]        int           NOT NULL,
        [SoftwareName]   nvarchar(300) NOT NULL,
        [Version]        nvarchar(80)  NULL,
        [Publisher]      nvarchar(200) NULL,
        [FirstSeenOnUtc] datetime2     NOT NULL,
        [LastSeenOnUtc]  datetime2     NOT NULL,
        [IsRemoved]      bit           NOT NULL,
        CONSTRAINT [PK_AssetInstalledSoftware] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Discovery].[SoftwareCatalog]', N'U') IS NULL
BEGIN
    CREATE TABLE [Discovery].[SoftwareCatalog] (
        [Id]            int           NOT NULL IDENTITY,
        [SoftwareName]  nvarchar(300) NOT NULL,
        [Publisher]     nvarchar(200) NULL,
        [LicensedSeats] int           NULL,
        [ContractId]    int           NULL,   -- Contracts.Contract, id only
        [IsBlacklisted] bit           NOT NULL,
        [IsActive]      bit           NOT NULL,
        [CreatedOnUtc]          datetime2     NOT NULL,   -- A
        [CreatedBy]             nvarchar(100) NULL,   -- A
        [ModifiedOnUtc]         datetime2     NULL,   -- A
        [ModifiedBy]            nvarchar(100) NULL,   -- A
        CONSTRAINT [PK_SoftwareCatalog] PRIMARY KEY ([Id])
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AgentApiKey]'), N'IX_AgentApiKey_Prefix', N'IndexID') IS NULL
    CREATE INDEX [IX_AgentApiKey_Prefix] ON [Discovery].[AgentApiKey] ([KeyPrefix]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[DiscoveredDevice]'), N'IX_DiscoveredDevice_Status', N'IndexID') IS NULL
    CREATE INDEX [IX_DiscoveredDevice_Status] ON [Discovery].[DiscoveredDevice] ([Status]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[DiscoveredDevice]'), N'UX_DiscoveredDevice_Machine', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_DiscoveredDevice_Machine] ON [Discovery].[DiscoveredDevice] ([Hostname], [SerialNumber]) WHERE [SerialNumber] IS NOT NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AssetHealth]'), N'IX_AssetHealth_LastSeen', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHealth_LastSeen] ON [Discovery].[AssetHealth] ([LastSeenOnUtc]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AssetHealthHistory]'), N'IX_AssetHealthHistory_AssetTrend', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHealthHistory_AssetTrend] ON [Discovery].[AssetHealthHistory] ([AssetId], [CapturedOnUtc]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AssetHealthHistory]'), N'IX_AssetHealthHistory_Captured', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetHealthHistory_Captured] ON [Discovery].[AssetHealthHistory] ([CapturedOnUtc]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AssetInstalledSoftware]'), N'IX_AssetInstalledSoftware_Name', N'IndexID') IS NULL
    CREATE INDEX [IX_AssetInstalledSoftware_Name] ON [Discovery].[AssetInstalledSoftware] ([SoftwareName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[AssetInstalledSoftware]'), N'UX_AssetInstalledSoftware_Install', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_AssetInstalledSoftware_Install] ON [Discovery].[AssetInstalledSoftware] ([AssetId], [SoftwareName], [Version]) WHERE [Version] IS NOT NULL;
IF INDEXPROPERTY(OBJECT_ID(N'[Discovery].[SoftwareCatalog]'), N'UX_SoftwareCatalog_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SoftwareCatalog_Name] ON [Discovery].[SoftwareCatalog] ([SoftwareName]);
GO
/* ===========================================================================
   SECTION 12 - [SapSync]  S/4HANA synchronisation
   =========================================================================== */
IF OBJECT_ID(N'[SapSync].[SapSyncLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [SapSync].[SapSyncLog] (
        [Id]               bigint         NOT NULL IDENTITY,
        [Direction]        nvarchar(20)   NOT NULL,   -- Inbound | Outbound
        [SyncType]         nvarchar(100)  NOT NULL,
        [Outcome]          nvarchar(20)   NOT NULL,
        [Message]          nvarchar(2000) NOT NULL,
        [RecordsProcessed] int            NOT NULL,
        [RecordsFailed]    int            NOT NULL,
        [SourceReference]  nvarchar(100)  NULL,
        [StartedOnUtc]     datetime2      NOT NULL,
        [CompletedOnUtc]   datetime2      NULL,
        [AttemptCount]     int            NOT NULL,
        CONSTRAINT [PK_SapSyncLog] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[SapSync].[SapSyncWatermark]', N'U') IS NULL
BEGIN
    CREATE TABLE [SapSync].[SapSyncWatermark] (
        [Id]                int           NOT NULL IDENTITY,
        [SyncType]          nvarchar(100) NOT NULL,
        [LastChangedOnUtc]  datetime2     NOT NULL,   -- resume point for the next delta pull
        [UpdatedOnUtc]      datetime2     NOT NULL,
        CONSTRAINT [PK_SapSyncWatermark] PRIMARY KEY ([Id])
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[SapSync].[SapSyncLog]'), N'IX_SapSyncLog_Recent', N'IndexID') IS NULL
    CREATE INDEX [IX_SapSyncLog_Recent] ON [SapSync].[SapSyncLog] ([StartedOnUtc] DESC);
IF INDEXPROPERTY(OBJECT_ID(N'[SapSync].[SapSyncLog]'), N'IX_SapSyncLog_Failures', N'IndexID') IS NULL
    CREATE INDEX [IX_SapSyncLog_Failures] ON [SapSync].[SapSyncLog] ([Outcome], [StartedOnUtc] DESC);
IF INDEXPROPERTY(OBJECT_ID(N'[SapSync].[SapSyncWatermark]'), N'UX_SapSyncWatermark_Type', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_SapSyncWatermark_Type] ON [SapSync].[SapSyncWatermark] ([SyncType]);
GO
/* ===========================================================================
   SECTION 13 - [Notifications]  In-app messages, SMTP profiles, outbox
   ---------------------------------------------------------------------------
   Every e-mail in the system goes through [EmailOutbox] - contract reminders,
   ticket replies and SLA escalations alike. Sending inline from a request
   thread loses the message when SMTP is down, and nobody finds out.
   =========================================================================== */
IF OBJECT_ID(N'[Notifications].[Notification]', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications].[Notification] (
        [Id]           bigint        NOT NULL IDENTITY,
        [UserId]       int           NOT NULL,   -- Identity.User, id only
        [Text]         nvarchar(500) NOT NULL,
        [DeepLink]     nvarchar(200) NULL,
        [IsRead]       bit           NOT NULL,
        [CreatedOnUtc] datetime2     NOT NULL,
        [ReadOnUtc]    datetime2     NULL,
        CONSTRAINT [PK_Notification] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Notifications].[EmailSetting]', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications].[EmailSetting] (
        [Id]                    int            NOT NULL IDENTITY,
        [ProfileName]           nvarchar(100)  NOT NULL,
        [Host]                  nvarchar(200)  NOT NULL,
        [Port]                  int            NOT NULL,
        [UseSsl]                bit            NOT NULL,
        [FromAddress]           nvarchar(256)  NOT NULL,
        [Username]              nvarchar(200)  NULL,
        [SmtpPasswordEncrypted] varbinary(max) NULL,   -- never logged, never in AssetFieldAudit
        [IsDefault]             bit            NOT NULL,
        [IsActive]              bit            NOT NULL,
        [CreatedOnUtc]          datetime2      NOT NULL,
        [CreatedBy]             nvarchar(100)  NULL,
        [ModifiedOnUtc]         datetime2      NULL,
        [ModifiedBy]            nvarchar(100)  NULL,
        CONSTRAINT [PK_EmailSetting] PRIMARY KEY ([Id])
    );
END
GO
IF OBJECT_ID(N'[Notifications].[EmailOutbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications].[EmailOutbox] (
        [Id]           bigint         NOT NULL IDENTITY,
        [ToAddress]    nvarchar(256)  NOT NULL,
        [CcAddress]    nvarchar(1000) NULL,   -- NEW  ticket replies copy the branch admin
        [Subject]      nvarchar(300)  NOT NULL,
        [Body]         nvarchar(max)  NOT NULL,
        [IsHtml]       bit            NOT NULL,
        [Status]       nvarchar(20)   NOT NULL,   -- Pending | Sent | Failed
        [AttemptCount] int            NOT NULL,
        [LastError]    nvarchar(500)  NULL,
        [SourceType]   nvarchar(40)   NULL,   -- NEW  ServiceRequest | Contract | SlaEscalation
        [SourceId]     bigint         NULL,   -- NEW  so a failed send can be traced back
        [CreatedOnUtc] datetime2      NOT NULL,
        [SentOnUtc]    datetime2      NULL,
        CONSTRAINT [PK_EmailOutbox] PRIMARY KEY ([Id])
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Notifications].[Notification]'), N'IX_Notification_UserUnread', N'IndexID') IS NULL
    CREATE INDEX [IX_Notification_UserUnread] ON [Notifications].[Notification] ([UserId], [CreatedOnUtc] DESC) WHERE [IsRead] = 0;
IF INDEXPROPERTY(OBJECT_ID(N'[Notifications].[EmailSetting]'), N'UX_EmailSetting_Name', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_EmailSetting_Name] ON [Notifications].[EmailSetting] ([ProfileName]);
IF INDEXPROPERTY(OBJECT_ID(N'[Notifications].[EmailSetting]'), N'UX_EmailSetting_OneDefault', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_EmailSetting_OneDefault] ON [Notifications].[EmailSetting] ([IsDefault]) WHERE [IsDefault] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[Notifications].[EmailOutbox]'), N'IX_EmailOutbox_PendingOldest', N'IndexID') IS NULL
    CREATE INDEX [IX_EmailOutbox_PendingOldest] ON [Notifications].[EmailOutbox] ([Status], [CreatedOnUtc]) WHERE [Status] = 'Pending';
IF INDEXPROPERTY(OBJECT_ID(N'[Notifications].[EmailOutbox]'), N'IX_EmailOutbox_Source', N'IndexID') IS NULL
    CREATE INDEX [IX_EmailOutbox_Source] ON [Notifications].[EmailOutbox] ([SourceType], [SourceId]);   -- NEW
GO
/* ===========================================================================
   SECTION 14 - [Audit]  Field-level change audit + scheduled future changes
   ---------------------------------------------------------------------------
   Written by a SaveChanges interceptor. Secret columns are excluded by the
   interceptor's ExcludedFields set - add to it when you add a secret column,
   or the audit trail becomes the place the password leaks.
   =========================================================================== */
IF OBJECT_ID(N'[Audit].[AssetFieldAudit]', N'U') IS NULL
BEGIN
    CREATE TABLE [Audit].[AssetFieldAudit] (
        [Id]           bigint         NOT NULL IDENTITY,
        [EntityName]   nvarchar(100)  NOT NULL,
        [EntityId]     nvarchar(64)   NOT NULL,
        [AssetId]      int            NULL,
        [FieldName]    nvarchar(128)  NOT NULL,
        [OldValue]     nvarchar(1024) NULL,
        [NewValue]     nvarchar(1024) NULL,
        [ChangedOnUtc] datetime2      NOT NULL,
        [ChangedBy]    nvarchar(100)  NOT NULL,
        CONSTRAINT [PK_AssetFieldAudit] PRIMARY KEY ([Id])
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[Audit].[AssetFieldAudit]'), N'IX_AFA_Asset', N'IndexID') IS NULL
    CREATE INDEX [IX_AFA_Asset] ON [Audit].[AssetFieldAudit] ([AssetId], [ChangedOnUtc]);
GO
/*  NEW - a change that has been decided but is not true yet.
    System versioning (the temporal tables declared inline in Sections 2, 3,
    8 and 9 - R2-17) records what a row USED to be. It cannot record what a
    row is going to become: SQL Server writes history when the update happens,
    so a cost centre moving on 1 April cannot be entered in March.
    This table is that missing half. Finance enters the change with the date it
    takes effect, a background job applies it on the morning of that date, and
    the temporal history then carries it like any other edit.
    The row is a scheduling instruction, never a source of truth - the entity
    and its history remain authoritative. That is why the value is held as
    text: this table must not become a second, competing copy of the record. */
IF OBJECT_ID(N'[Audit].[ScheduledFieldChange]', N'U') IS NULL
BEGIN
    CREATE TABLE [Audit].[ScheduledFieldChange] (
        [Id]                 int            NOT NULL IDENTITY,
        [SchemaName]         nvarchar(60)   NOT NULL,
        [EntityName]         nvarchar(100)  NOT NULL,
        [EntityId]           nvarchar(64)   NOT NULL,
        [FieldName]          nvarchar(128)  NOT NULL,
        [CurrentValue]       nvarchar(1024) NULL,       -- what it was when the change was scheduled
        [NewValue]           nvarchar(1024) NULL,
        [EffectiveFromDate]  date           NOT NULL,
        [EffectiveToDate]    date           NULL,       -- set only when the change is a temporary override
        [Status]             nvarchar(20)   NOT NULL,   -- Pending|Applied|Cancelled|Failed|Superseded
        [Reason]             nvarchar(500)  NOT NULL,
        [RequestedByUserId]  int            NOT NULL,
        [RequestedOnUtc]     datetime2      NOT NULL,
        [AppliedOnUtc]       datetime2      NULL,
        [AppliedBy]          nvarchar(100)  NULL,
        [CancelledOnUtc]     datetime2      NULL,
        [CancelledByUserId]  int            NULL,
        [FailureReason]      nvarchar(500)  NULL,
        [CreatedOnUtc]       datetime2      NOT NULL,
        [CreatedBy]          nvarchar(100)  NULL,
        [ModifiedOnUtc]      datetime2      NULL,
        [ModifiedBy]         nvarchar(100)  NULL,
        [RowVersion]         rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_ScheduledFieldChange] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ScheduledFieldChange_Status] CHECK ([Status] IN (N'Pending', N'Applied', N'Cancelled', N'Failed', N'Superseded')),
        CONSTRAINT [CK_ScheduledFieldChange_Window] CHECK ([EffectiveToDate] IS NULL OR [EffectiveToDate] >= [EffectiveFromDate]),
        CONSTRAINT [CK_ScheduledFieldChange_Applied] CHECK ([Status] <> N'Applied' OR [AppliedOnUtc] IS NOT NULL)
    );
END
GO
/*  Two people scheduling different values for the same field on the same day
    is not a merge to resolve at run time - it is a mistake, and the second one
    must be refused while it can still be argued about. */
IF INDEXPROPERTY(OBJECT_ID(N'[Audit].[ScheduledFieldChange]'), N'UX_ScheduledFieldChange_OnePendingPerFieldPerDate', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ScheduledFieldChange_OnePendingPerFieldPerDate]
        ON [Audit].[ScheduledFieldChange] ([SchemaName], [EntityName], [EntityId], [FieldName], [EffectiveFromDate])
        WHERE [Status] = N'Pending';
GO
/*  The query the apply job runs every morning. */
IF INDEXPROPERTY(OBJECT_ID(N'[Audit].[ScheduledFieldChange]'), N'IX_ScheduledFieldChange_Due', N'IndexID') IS NULL
    CREATE INDEX [IX_ScheduledFieldChange_Due] ON [Audit].[ScheduledFieldChange] ([EffectiveFromDate]) WHERE [Status] = N'Pending';
GO
/*  "What is scheduled against this asset?", asked from the record itself. */
IF INDEXPROPERTY(OBJECT_ID(N'[Audit].[ScheduledFieldChange]'), N'IX_ScheduledFieldChange_Entity', N'IndexID') IS NULL
    CREATE INDEX [IX_ScheduledFieldChange_Entity] ON [Audit].[ScheduledFieldChange] ([SchemaName], [EntityName], [EntityId], [EffectiveFromDate]);
GO
/* ===========================================================================
   SECTION 15 - [DataImport]   *** ENTIRELY NEW MODULE ***
                Excel import batches and row-level errors
   ---------------------------------------------------------------------------
   One import module for every entity, not one set of tables per entity as the
   handbook had for field assets. [ImportType] says what was loaded.
   [IsDryRun] is the point of the design. An import of seven thousand rows must
   be rehearsed before anybody commits it: the dry run writes the batch and
   every rejection, changes no business data, and the operator fixes the file
   and runs it again. An import whose failure mode is a wrong asset register
   that looks right should never run unattended.
   =========================================================================== */
IF OBJECT_ID(N'[DataImport].[ImportBatch]', N'U') IS NULL                           -- NEW
BEGIN
    CREATE TABLE [DataImport].[ImportBatch] (
        [Id]               int           NOT NULL IDENTITY,
        [BatchNumber]      nvarchar(30)  NOT NULL,   -- from ImportBatchNumberSequence
        [ImportType]       nvarchar(40)  NOT NULL,   -- Asset|Employee|FieldAsset|FixedAssetRegister|Contract
        [FileName]         nvarchar(260) NOT NULL,
        [FilePath]         nvarchar(400) NULL,
        [FileHash]         nvarchar(128) NULL,       -- catches the same file loaded twice
        [IsDryRun]         bit           NOT NULL,
        [Status]           nvarchar(20)  NOT NULL,   -- Running|Rehearsed|Committed|Failed|Cancelled
        [TotalRows]        int           NOT NULL CONSTRAINT [DF_ImportBatch_TotalRows]     DEFAULT (0),
        [SucceededRows]    int           NOT NULL CONSTRAINT [DF_ImportBatch_SucceededRows] DEFAULT (0),
        [FailedRows]       int           NOT NULL CONSTRAINT [DF_ImportBatch_FailedRows]    DEFAULT (0),
        [ImportedByUserId] int           NOT NULL,   -- Identity.User, id only
        [StartedOnUtc]     datetime2     NOT NULL,
        [CompletedOnUtc]   datetime2     NULL,
        [Remarks]          nvarchar(500) NULL,
        [CreatedOnUtc]     datetime2     NOT NULL,
        [CreatedBy]        nvarchar(100) NULL,
        [ModifiedOnUtc]    datetime2     NULL,
        [ModifiedBy]       nvarchar(100) NULL,
        CONSTRAINT [PK_ImportBatch] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ImportBatch_Status] CHECK ([Status] IN (N'Running', N'Rehearsed', N'Committed', N'Failed', N'Cancelled')),
        CONSTRAINT [CK_ImportBatch_Counts] CHECK ([TotalRows] >= 0 AND [SucceededRows] >= 0 AND [FailedRows] >= 0 AND [SucceededRows] + [FailedRows] <= [TotalRows])
    );
END
GO
/*  One row per rejected spreadsheet row. RowNumber and ColumnName are what the
    operator needs to find it in Excel; RawValue is kept so the rejection
    report can show what was actually in the cell rather than a rephrasing. */
IF OBJECT_ID(N'[DataImport].[ImportError]', N'U') IS NULL                           -- NEW
BEGIN
    CREATE TABLE [DataImport].[ImportError] (
        [Id]            bigint        NOT NULL IDENTITY,
        [ImportBatchId] int           NOT NULL,
        [RowNumber]     int           NOT NULL,
        [ColumnName]    nvarchar(128) NULL,
        [RawValue]      nvarchar(500) NULL,
        [ErrorCode]     nvarchar(60)  NOT NULL,   -- DuplicateAssetNumber, UnknownLocation, ...
        [ErrorMessage]  nvarchar(500) NOT NULL,
        [IsResolved]    bit           NOT NULL CONSTRAINT [DF_ImportError_IsResolved] DEFAULT (0),
        [RecordedOnUtc] datetime2     NOT NULL,
        CONSTRAINT [PK_ImportError] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ImportError_RowNumber] CHECK ([RowNumber] > 0),
        CONSTRAINT [FK_ImportError_ImportBatch_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [DataImport].[ImportBatch] ([Id]) ON DELETE CASCADE
    );
END
GO
IF INDEXPROPERTY(OBJECT_ID(N'[DataImport].[ImportBatch]'), N'UX_ImportBatch_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ImportBatch_Number] ON [DataImport].[ImportBatch] ([BatchNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[DataImport].[ImportBatch]'), N'IX_ImportBatch_TypeRecent', N'IndexID') IS NULL
    CREATE INDEX [IX_ImportBatch_TypeRecent] ON [DataImport].[ImportBatch] ([ImportType], [StartedOnUtc] DESC);
IF INDEXPROPERTY(OBJECT_ID(N'[DataImport].[ImportError]'), N'IX_ImportError_Batch', N'IndexID') IS NULL
    CREATE INDEX [IX_ImportError_Batch] ON [DataImport].[ImportError] ([ImportBatchId], [RowNumber]);
GO
/* ===========================================================================
   SECTION 17 - REFERENCE DATA
   ---------------------------------------------------------------------------
   Only the lookup rows the application cannot work without. Every insert is
   guarded on the natural key, so re-running adds nothing and an administrator's
   later edits are never overwritten.
   Nothing here is transactional data and nothing here is environment-specific:
   locations, employees and users are still loaded per environment.
   =========================================================================== */
-- 17.1  Asset statuses --------------------------------------------------------
/*  R2-5: the FULL baseline set. The previous revision seeded only the two
    standby rows, on the assumption that statuses 1-7 already existed - but
    this script claims to stand up an EMPTY database, and Asset.AssetStatusId
    is NOT NULL, so a register with no statuses cannot create its first asset.
    DisplayOrder leaves gaps (5-7) for statuses an administrator adds later.
    The handbook distinguishes stock sitting in a BRANCH IT store ('In
    Standby') from stock that has reached the HO store ('In Standby-IT');
    without both rows the GRN step has nothing to move the asset into. */
INSERT INTO [Assets].[AssetStatus] ([StatusName], [IsTerminal], [DisplayOrder], [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT v.[StatusName], v.[IsTerminal], v.[DisplayOrder], 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'In Stock',      CAST(0 AS bit),  1),   -- registered, not yet issued
        (N'Allocated',     CAST(0 AS bit),  2),   -- in an employee's hands
        (N'In Transit',    CAST(0 AS bit),  3),   -- on a courier between branches
        (N'Under Repair',  CAST(0 AS bit),  4),   -- with a vendor or the IT bench
        (N'In Standby',    CAST(0 AS bit),  8),   -- held in a branch IT store
        (N'In Standby-IT', CAST(0 AS bit),  9),   -- held in the head-office IT store
        /*  R3: the capitalisation lifecycle. An asset under construction is
            not 'In Stock' - it does not exist as a usable thing yet, cannot
            be allocated and must not appear on a verification sheet. 77 rows
            of the live register sit in this state. */
        (N'Under Construction', CAST(0 AS bit), 10),   -- R3  an AUC line
        (N'Capitalised',        CAST(0 AS bit), 11),   -- R3  AUC settled into a real asset
        (N'Scrapped',      CAST(1 AS bit), 20),   -- terminal
        (N'Lost',          CAST(1 AS bit), 21),   -- terminal
        /*  R3: distinct from Scrapped. Scrapped is thrown away; Disposed was
            SOLD, carries proceeds and an approval, and its evidence lives in
            [Assets].[AssetDisposal]. */
        (N'Disposed',      CAST(1 AS bit), 22)    -- R3  terminal
     ) AS v([StatusName], [IsTerminal], [DisplayOrder])
WHERE NOT EXISTS (SELECT 1 FROM [Assets].[AssetStatus] s WHERE s.[StatusName] = v.[StatusName]);
GO
-- 17.1b  Asset classes - the finance axis ------------------------------------   R3
/*  All 13, taken from the live fixed asset register (7,413 rows as at
    18-07-2026) with their real codes. This is not a guess at a taxonomy: it is
    the taxonomy the accounts already run on, and an import that invents a
    fourteenth class is an import that has misread its file.

    [ReportingCategory] is a column rather than a table because it is a pure
    FUNCTION of the class - the class/category cross-tab has exactly 13 rows,
    one per class, collapsing to 9 distinct categories. Five classes report as
    Plant & Machinery, which is precisely why the register keeps both.

    [ClassName] is spelled as the register spells it, typos included
    ('Maintainence eqpt', 'Plant.& Machinery', the double space in
    'Lease Hold  land'), because the importer matches on it. The reporting
    categories ARE normalised - they are a display grouping and match nothing,
    so 'Leashold Land' is corrected here. */
INSERT INTO [Assets].[AssetClass] ([ClassCode], [ClassName], [ReportingCategory], [IsDepreciable], [IsIntangible], [IsAuc], [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT v.[ClassCode], v.[ClassName], v.[ReportingCategory], v.[IsDepreciable], v.[IsIntangible], v.[IsAuc], 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        --          code               name                        reporting category      depr  intan  auc      live rows
        (N'F & F',          N'Furniture & Fixtures',  N'Furniture & Fixtures', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   -- 2,181
        (N'Comp.h/w & s/w', N'Comp.h/w & s/w',        N'Computers',            CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   -- 1,774
        (N'Office eqpt',    N'Office eqpt',           N'Office Equipments',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   -- 1,140
        (N'Ins eqpt',       N'Installation eqpt',     N'Plant & Machinery',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --   983
        (N'Fty eqpt',       N'Factory eqpt',          N'Plant & Machinery',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --   758
        (N'P&M',            N'Plant.& Machinery',     N'Plant & Machinery',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --   313
        /*  The one AUC row. [IsAuc] = 1 is what the capitalisation step looks
            for, and [CK_AssetClass_OneAuc] below keeps it the only one. */
        (N'AUC',            N'AUC',                   N'AUC',                  CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit)),   --    77
        (N'Intangible',     N'Intangible Asset',      N'Software',             CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit)),   --    60
        (N'Mtn eqpt',       N'Maintainence eqpt',     N'Plant & Machinery',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --    55
        (N'Canteen eqpt',   N'Canteen eqpt',          N'Plant & Machinery',    CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --    38
        (N'LH bldg',        N'Lease Hold bldg',       N'Building',             CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --    22
        (N'Vehicles',       N'Vehicles',              N'Vehicles',             CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),   --    10
        /*  Land is not depreciated. The flag exists for this row. */
        (N'LH Land',        N'Lease Hold  land',      N'Leasehold Land',       CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit))    --     2
     ) AS v([ClassCode], [ClassName], [ReportingCategory], [IsDepreciable], [IsIntangible], [IsAuc])
WHERE NOT EXISTS (SELECT 1 FROM [Assets].[AssetClass] c WHERE c.[ClassCode] = v.[ClassCode]);
GO
-- 17.2  Ticket statuses and how each one treats the clock --------------------
INSERT INTO [ServiceDesk].[RequestStatus] ([StatusName], [IsClosedState], [DisplayOrder], [IsActive], [SlaClockBehaviour], [CountsTechnicianTime], [CreatedOnUtc], [CreatedBy])
SELECT v.[StatusName], v.[IsClosedState], v.[DisplayOrder], 1, v.[SlaClockBehaviour], v.[CountsTechnicianTime], SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'Open',              CAST(0 AS bit),  1, N'Running', CAST(0 AS bit)),
        (N'Assigned',          CAST(0 AS bit),  2, N'Running', CAST(0 AS bit)),
        (N'In Progress',       CAST(0 AS bit),  3, N'Running', CAST(1 AS bit)),
        (N'On Hold',           CAST(0 AS bit),  4, N'Paused',  CAST(0 AS bit)),
        (N'Waiting for User',  CAST(0 AS bit),  5, N'Paused',  CAST(0 AS bit)),
        (N'Waiting for Spare', CAST(0 AS bit),  6, N'Paused',  CAST(0 AS bit)),
        (N'Standby Provided',  CAST(0 AS bit),  7, N'Running', CAST(0 AS bit)),
        (N'Resolved',          CAST(0 AS bit),  8, N'Stopped', CAST(0 AS bit)),
        (N'Closed',            CAST(1 AS bit),  9, N'Stopped', CAST(0 AS bit)),
        (N'Rejected',          CAST(1 AS bit), 10, N'Stopped', CAST(0 AS bit))
     ) AS v([StatusName], [IsClosedState], [DisplayOrder], [SlaClockBehaviour], [CountsTechnicianTime])
WHERE NOT EXISTS (SELECT 1 FROM [ServiceDesk].[RequestStatus] s WHERE s.[StatusName] = v.[StatusName]);
GO
/*  Statuses that already existed before this design were created with the
    'Running' default. Correct the ones that must freeze the clock - but only
    while they still hold the default, so a deliberate change is never undone. */
UPDATE s
   SET s.[SlaClockBehaviour] = N'Paused'
  FROM [ServiceDesk].[RequestStatus] s
 WHERE s.[SlaClockBehaviour] = N'Running'
   AND s.[StatusName] IN (N'On Hold', N'Waiting for User', N'Waiting for Spare');
GO
UPDATE s
   SET s.[SlaClockBehaviour] = N'Stopped'
  FROM [ServiceDesk].[RequestStatus] s
 WHERE s.[SlaClockBehaviour] = N'Running'
   AND s.[StatusName] IN (N'Resolved', N'Closed', N'Rejected');
GO
UPDATE s
   SET s.[CountsTechnicianTime] = 1
  FROM [ServiceDesk].[RequestStatus] s
 WHERE s.[StatusName] = N'In Progress'
   AND s.[CountsTechnicianTime] = 0;
GO
-- 17.3  Support regions and the teams that serve them ------------------------
INSERT INTO [Organization].[Region] ([RegionName], [Description], [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT v.[RegionName], v.[Description], 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'North', N'Delhi, Noida, Ahmedabad, Mumbai and northern branches'),
        (N'South', N'Chennai, Bangalore, Kerala and Andhra Pradesh branches')
     ) AS v([RegionName], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Organization].[Region] r WHERE r.[RegionName] = v.[RegionName]);
GO
INSERT INTO [ServiceDesk].[SupportTeam] ([TeamName], [RegionId], [IsDefaultTeam], [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT v.[TeamName],
       (SELECT r.[Id] FROM [Organization].[Region] r WHERE r.[RegionName] = v.[RegionName]),
       v.[IsDefaultTeam], 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'IT Support North', N'North', CAST(1 AS bit)),   -- default: unmapped branches land here
        (N'IT Support South', N'South', CAST(0 AS bit))
     ) AS v([TeamName], [RegionName], [IsDefaultTeam])
WHERE NOT EXISTS (SELECT 1 FROM [ServiceDesk].[SupportTeam] t WHERE t.[TeamName] = v.[TeamName]);
GO
-- 17.4  Default SLA policies -------------------------------------------------
/*  The handbook's published targets, plus Critical, which the priority list
    has always had and the policy table must therefore answer for. Minutes
    counted inside the branch calendar, not on the wall clock. */
INSERT INTO [ServiceLevel].[SlaPolicy]
       ([PolicyName], [Description], [Priority], [ResponseTargetMinutes], [ResolutionTargetMinutes],
        [RespectOperationalHours], [RespectHolidays], [RespectWeekends], [NearDueWarningMinutes],
        [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT v.[PolicyName], v.[Description], v.[Priority], v.[ResponseMinutes], v.[ResolutionMinutes],
       v.[RespectHours], v.[RespectHolidays], v.[RespectWeekends], 30, 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'Critical Priority SLA', N'Production down. Counted around the clock.',
            N'Critical',   60,  120, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
        (N'High Priority SLA',     N'2 hours to respond, 4 to resolve, inside branch hours.',
            N'High',      120,  240, CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
        (N'Medium Priority SLA',   N'4 hours to respond, 8 to resolve, inside branch hours.',
            N'Medium',    240,  480, CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
        (N'Low Priority SLA',      N'8 hours to respond, 24 to resolve, inside branch hours.',
            N'Low',       480, 1440, CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit))
     ) AS v([PolicyName], [Description], [Priority], [ResponseMinutes], [ResolutionMinutes],
            [RespectHours], [RespectHolidays], [RespectWeekends])
WHERE NOT EXISTS (SELECT 1 FROM [ServiceLevel].[SlaPolicy] p WHERE p.[Priority] = v.[Priority] AND p.[IsActive] = 1);
GO
-- 17.5  Default contract reminder windows ------------------------------------
/*  The four thresholds the reminder job used to carry in code. */
INSERT INTO [Contracts].[ContractReminderSetting] ([ContractId], [DaysBeforeExpiry], [Channel], [IsActive], [CreatedOnUtc], [CreatedBy])
SELECT NULL, v.[Days], N'Both', 1, SYSUTCDATETIME(), N'schema-design'
FROM (VALUES (60), (30), (15), (7)) AS v([Days])
WHERE NOT EXISTS (SELECT 1 FROM [Contracts].[ContractReminderSetting] s
                   WHERE s.[ContractId] IS NULL AND s.[DaysBeforeExpiry] = v.[Days]);
GO
-- 17.6  Capabilities for the new features ------------------------------------
/*  Registering the capability does not grant it. Assign to roles deliberately;
    Sec 17.7 shows the intended mapping but does not apply it, because which
    role may reverse a return is a decision for the business, not for a
    script.                                                            -- R2-17 */
INSERT INTO [Identity].[Capability] ([Name], [Module], [Description], [CreatedOnUtc], [CreatedBy])
SELECT v.[Name], v.[Module], v.[Description], SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        /*  R2-23: the Identity capabilities the existing screens have always
            needed. The previous revision seeded only capabilities for NEW
            features, so a fresh database could register a handover but had no
            name for "may create a user" - and an endpoint declaring a
            capability that is not seeded can never be granted to anybody. */
        (N'user.manage',            N'Identity',    N'Create, edit, lock, unlock and reset users.'),   -- R2-23
        (N'user.view',              N'Identity',    N'Read users and their effective capabilities.'),  -- R2-23
        (N'role.manage',            N'Identity',    N'Create, rename and retire roles, and set what each grants.'),  -- R2-23
        /*  R2-24: Organization, for the same reason as R2-23. Branch scoping
            decides WHICH employees a holder sees; the capability decides
            whether they see the directory at all.
            Seeded ahead of the Organization slices, which are built after
            Identity is complete. A seeded capability nobody holds is inert. */
        (N'employee.manage',        N'Organization', N'Create and edit employees, and deactivate leavers.'),   -- R2-24
        (N'employee.view',          N'Organization', N'Read the employee directory.'),                          -- R2-24
        /*  R2-25: the Organization master data one screen each maintains.
            One capability covers branches, regions, departments, vendors and
            the application master: they are the same job, done by the same
            person, and splitting them would produce five grants nobody ever
            sets differently. */
        (N'organization.manage',    N'Organization', N'Maintain branches, regions, departments, vendors and applications.'),  -- R2-25
        (N'organization.view',      N'Organization', N'Read the organisation master data.'),                                  -- R2-25
        (N'application-access.manage', N'Organization', N'Grant and revoke an employee''s access to an application.'),        -- R2-25
        /*  R3-5: the allocation lifecycle's own capabilities. Revision 2 seeded
            handover.record and allocation.revert-return and nothing else, so
            every Allocations screen would have declared a capability no
            administrator could grant - the same gap R3-4 found in [Assets].

            Split by AUDIENCE, as the catalogue splits it:
              allocation.view / .manage   Branch Admin runs allocation day to day.
              allocation.approve          Super Admin decides a request. Separate
                                          because the point of raising a request
                                          rather than allocating directly is that
                                          somebody ELSE decides it - one person
                                          holding both makes the approval a
                                          formality.
              acknowledgement.approve     The reporting manager countersigns. Not
                                          an administrator's job at all.
              customer-site.manage        Site master data. A different screen and
                                          a different kind of change from issuing
                                          an asset to a person. */
        (N'allocation.view',        N'Allocations', N'Read allocations, expected returns and the overdue list.'),        -- R3-5
        (N'allocation.manage',      N'Allocations', N'Allocate an asset to an employee and receive it back.'),           -- R3-5
        (N'allocation.request',     N'Allocations', N'Raise a request for an asset to be allocated to an employee.'),    -- R3-5
        (N'allocation.approve',     N'Allocations', N'Approve or reject an allocation request.'),                        -- R3-5
        (N'acknowledgement.approve', N'Allocations', N'Countersign an employee''s acknowledgement of an asset.'),        -- R3-5
        (N'customer-site.manage',   N'Allocations', N'Maintain customer sites and map assets to them.'),                 -- R3-5
        (N'handover.record',        N'Allocations', N'Accept an asset back from an employee into the branch store.'),
        /*  R3-6: Movements' own capabilities. Revision 2 seeded handover.dispatch
            and handover.receive - the two HANDOVER steps - and nothing for the
            ordinary branch-to-branch despatch the module is mostly used for, so
            every Despatch screen would have declared a capability nobody could
            be granted. The third time this gap has appeared; R3-4 and R3-5 were
            the same in [Assets] and [Allocations].

            movement.receive is separate from movement.manage because receiving
            is done by the DESTINATION branch, not the one that sent it. The
            person who despatched cannot also confirm arrival, which is the only
            thing that makes a goods receipt worth reading. */
        /*  R3-8: Transfers had NO capabilities seeded at all - the fourth
            module in a row. A transfer is the approval and the accounting
            consequence, so raise / decide / complete are three different jobs:
            a branch administrator raises one, head office decides it, and
            completing it applies a change to the register and queues it to SAP.
            One capability covering all three would let the person who wants the
            transfer grant it to themselves. */
        (N'transfer.view',          N'Transfers',   N'Read transfer requests and their SAP status.'),          -- R3-8
        (N'transfer.request',       N'Transfers',   N'Raise a transfer of an asset.'),                         -- R3-8
        (N'transfer.approve',       N'Transfers',   N'Approve, reject or cancel a transfer request.'),         -- R3-8
        (N'transfer.complete',      N'Transfers',   N'Apply an approved transfer and queue it to SAP.'),       -- R3-8
        (N'movement.view',          N'Movements',   N'Read shipments and the pending-receipt queue.'),           -- R3-6
        (N'movement.manage',        N'Movements',   N'Despatch assets to another branch or to head office.'),    -- R3-6
        (N'movement.receive',       N'Movements',   N'Confirm arrival at the destination branch.'),              -- R3-6
        (N'handover.dispatch',      N'Movements',   N'Despatch branch standby stock to head office.'),
        (N'handover.receive',       N'Movements',   N'Record GRN receipt of assets arriving at head office.'),
        (N'allocation.revert-return', N'Allocations', N'Reverse a return recorded in error.'),
        (N'sla.manage',             N'ServiceLevel', N'Create and edit SLA policies and escalation levels.'),
        (N'calendar.manage',        N'ServiceLevel', N'Configure per-location operational hours.'),
        (N'holiday.manage',         N'ServiceLevel', N'Maintain the holiday calendar.'),
        /*  R3-9: the ticket lifecycle's own capabilities. Revision 2 seeded the
            three side-actions - note, e-mail, attach - and the two master-data
            ones, and nothing for raising, reading, assigning or working a
            ticket, which is the module. Fifth time; R3-4 through R3-8 were the
            same in Assets, Allocations, Movements and Transfers.

            request.raise is separate from request.manage because EVERY employee
            raises tickets and almost none of them work one. A single capability
            would mean either giving the whole company the technician queue or
            nobody a way to ask for help.

            request.assign is separate again: a technician may pick up work,
            but handing it to somebody else is a decision about their day. */
        (N'request.raise',          N'ServiceDesk', N'Raise a ticket, a fault or a New Service request.'),   -- R3-9
        (N'request.view',           N'ServiceDesk', N'Read the ticket queue and ticket detail.'),            -- R3-9
        (N'request.manage',         N'ServiceDesk', N'Work a ticket: start, hold, resolve, close, reopen.'), -- R3-9
        (N'request.assign',         N'ServiceDesk', N'Assign a ticket to a technician or a support team.'),  -- R3-9
        (N'request-category.manage', N'ServiceDesk', N'Maintain request categories and sub-categories.'),    -- R3-9
        (N'approval-workflow.manage', N'ServiceDesk', N'Define and publish approval workflows.'),            -- R3-9
        (N'approval.decide',        N'ServiceDesk', N'Approve or reject an assigned approval step.'),        -- R3-9
        (N'approval.cancel',        N'ServiceDesk', N'Cancel an approval run, with a recorded reason.'),     -- R3-9
        (N'request.note',           N'ServiceDesk', N'Add a note to a ticket.'),
        (N'request.email',          N'ServiceDesk', N'Send e-mail from a ticket.'),
        (N'request.attach',         N'ServiceDesk', N'Upload and download ticket attachments.'),
        (N'support-team.manage',    N'ServiceDesk', N'Maintain support teams and their members.'),
        (N'service-template.manage', N'ServiceDesk', N'Maintain reusable service request templates.'),
        (N'import.run',             N'DataImport',  N'Rehearse and commit spreadsheet imports.'),
        /*  R3-10: Notifications had NO capabilities at all. Every e-mail in
            the system goes through this module's outbox, and until now nothing
            could be granted to look at it - so a message that failed to send
            was invisible to everybody, which defeats the point of having an
            outbox rather than sending inline.

            Sixth module in a row. The pattern is settled and written down in
            docs/00DESIGNDECISIONS.md: the seed is written when the SCREENS
            are, not when the tables are, because until a screen exists nobody
            knows what it needs permission to do.

            There is no capability for reading your own notifications. Every
            signed-in user reads their own, and a capability would be a lie:
            withdrawing it would stop somebody being told things about their
            own work. */
        (N'email-setting.manage',   N'Notifications', N'Configure SMTP profiles and the sending address.'),   -- R3-10
        (N'outbox.manage',          N'Notifications', N'Read the e-mail queue and requeue a failed message.'), -- R3-10
        /*  R3-11: Contracts had none either. Seventh module in a row, and the
            last one whose screens exist - the pattern is settled and written
            down in docs/00DESIGNDECISIONS.md.

            contract.view is separate from contract.manage because an AMC's
            expiry is something a branch administrator needs to SEE - it decides
            whether a repair is chargeable - while editing the contract itself
            belongs with whoever negotiates it. */
        (N'contract.view',          N'Contracts',   N'Read contracts, their covered assets and their documents.'),  -- R3-11
        (N'contract.manage',        N'Contracts',   N'Create, edit, renew and retire contracts.'),                   -- R3-11
        (N'contract-reminder.manage', N'Contracts', N'Configure expiry reminder windows and recipients.'),           -- R3-11
        /*  R3-12: Verification had none. Eighth and last module whose
            screens exist.

            run is separate from manage because they are different people in
            different places: a technician walks a branch with a phone, and an
            administrator opens and closes the cycle from a desk. Giving the
            technician the power to close a cycle mid-count is how a count
            ends early. */
        (N'verification.run',       N'Verification', N'Record a sighting or a bulk count against the open cycle.'), -- R3-12
        (N'verification.view',      N'Verification', N'Read verification results and the exception report.'),       -- R3-12
        (N'verification.manage',    N'Verification', N'Open and close verification cycles.'),                       -- R3-12
        /*  R3-13: Discovery had none. Note what is NOT here: the agent's own
            endpoint has no capability at all. An agent is not a user - it has
            no session, no branches and nobody to grant anything to - so it
            authenticates with an API key and is authorised by holding one.
            Inventing a capability for it would mean creating a user account
            per machine, which is how service accounts multiply. */
        (N'discovery.view',         N'Discovery',   N'Read discovered devices, health and installed software.'),  -- R3-13
        (N'discovery.manage',       N'Discovery',   N'Link a discovered device to an asset, or ignore it.'),      -- R3-13
        (N'agent-key.manage',       N'Discovery',   N'Issue and revoke agent API keys.'),                         -- R3-13
        (N'software-catalog.manage', N'Discovery',  N'Maintain the software catalogue and licence counts.'),      -- R3-13
        /*  R3-4: the register's own capabilities. Revision 2 seeded field-asset.*
            and nothing else for [Assets], so every Asset screen would have
            declared a capability no administrator could grant - the failure
            Identity found the hard way and R2-24 was written to stop.

            Split three ways because the catalogue splits the AUDIENCE three
            ways, not for symmetry:
              asset.view / asset.manage   Super Admin AND Branch Admin. Running
                                          the register is a branch job.
              asset-taxonomy.manage       Super Admin only. Types, classes,
                                          statuses, custom fields and COA codes
                                          are one job done by one person - the
                                          same argument as R2-25 - and a branch
                                          administrator inventing an asset class
                                          would corrupt the finance roll-up for
                                          everybody.
              asset-finance.view          Book values are payroll-adjacent. They
                                          are read-only everywhere in AMS (SAP
                                          owns the arithmetic), so there is no
                                          matching .manage and there should
                                          never be one. */
        (N'asset.view',             N'Assets', N'Read the asset register.'),                                        -- R3-4
        (N'asset.manage',           N'Assets', N'Register and edit assets, and record disposals.'),                 -- R3-4
        (N'asset-taxonomy.manage',  N'Assets', N'Maintain asset types, classes, statuses, custom fields and chart-of-account codes.'),  -- R3-4
        (N'asset-finance.view',     N'Assets', N'Read the book values and depreciation mirrored from SAP.'),        -- R3-4
        (N'field-asset.view',       N'Assets', N'View the register filtered to field assets.'),   -- R3
        (N'field-asset.manage',     N'Assets', N'Create, edit and import field assets in the register.'),   -- R3
        (N'change.schedule',        N'Audit',       N'Schedule a field change to take effect on a future date.'),
        (N'change.cancel',          N'Audit',       N'Cancel a scheduled change before it is applied.'),
        (N'history.view',           N'Audit',       N'Read a record as it stood at a past date.')
     ) AS v([Name], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Identity].[Capability] c WHERE c.[Name] = v.[Name]);
GO
/*  17.7  Intended role mapping - REFERENCE ONLY, deliberately not executed.
        SuperAdmin      every capability above
        BranchAdmin     handover.record, handover.dispatch, request.note,
                        request.attach, import.run
        Technician      request.note, request.email, request.attach
        FieldAssetAdmin field-asset.view, field-asset.manage, import.run
                        (a normal Identity.User role - there is no second
                         login table)
        SLA and calendar administration stays with SuperAdmin: an operational
        calendar edited by the wrong person silently changes every SLA
        measurement taken afterwards.
*/
/* ===========================================================================
   SECTION 18 - VERIFICATION (BASE DESIGN)
   ---------------------------------------------------------------------------
   What the script has built AT THIS POINT. Expect 15 schemas and 86 module
   tables; sys.tables shows 91 rows because system versioning adds five
   history tables (EmployeeHistory, AssetHistory, ContractHistory,
   SlaPolicyHistory, LocationOperationalHourHistory) in their owning
   schemas.                                                            -- R2-17

   R2-19: this counts the BASE design only. The approval-workflow extension
   below adds 8 more ServiceDesk tables, so a completed run of this file ends
   at 94 module tables / 99 rows in sys.tables.   -- R3 (was 87/92) The extension's own Section 6
   counts the finished database; do not read the numbers below as the total.
   =========================================================================== */
SELECT  s.[name]                          AS [Schema],
        COUNT(t.[object_id])              AS [TablesInclHistory]
FROM    sys.schemas s
        LEFT JOIN sys.tables t ON t.[schema_id] = s.[schema_id]
WHERE   s.[name] IN (N'Identity', N'Organization', N'Assets', N'Allocations', N'Movements',
                     N'Transfers', N'ServiceDesk', N'ServiceLevel', N'Contracts', N'Verification',
                     N'Discovery', N'SapSync', N'Notifications', N'Audit', N'DataImport')
GROUP BY s.[name]
ORDER BY s.[name];
GO
/*  Anything still missing shows up here rather than at run time. */
SELECT  v.[Expected] AS [MissingTable]
FROM    (VALUES
            (N'[Organization].[Region]'),
            (N'[Allocations].[AssetHandover]'),
            (N'[Allocations].[AllocationReturnReversal]'),
            (N'[Movements].[MovementBatch]'),
            (N'[ServiceDesk].[SupportTeam]'),
            (N'[ServiceDesk].[SupportTeamMember]'),
            (N'[ServiceDesk].[ServiceTemplate]'),
            (N'[ServiceDesk].[RequestEmail]'),
            (N'[ServiceLevel].[SlaPolicy]'),
            (N'[ServiceLevel].[SlaEscalation]'),
            (N'[ServiceLevel].[SlaEscalationLog]'),
            (N'[ServiceLevel].[LocationOperationalHour]'),
            (N'[ServiceLevel].[LocationOperationalDay]'),
            (N'[ServiceLevel].[LocationSaturdayRule]'),
            (N'[ServiceLevel].[HolidayCalendar]'),
            (N'[ServiceLevel].[HolidayLocation]'),
            (N'[Contracts].[ContractReminderSetting]'),
            (N'[Audit].[ScheduledFieldChange]'),
            (N'[DataImport].[ImportBatch]'),
            (N'[DataImport].[ImportError]'),
            (N'[Assets].[AssetFinance]'),
            (N'[Assets].[AssetHolding]')
        ) AS v([Expected])
WHERE   OBJECT_ID(v.[Expected], N'U') IS NULL;
GO
PRINT N'AMS consolidated design applied.';
GO
/* ===========================================================================
   APPENDIX - WHAT THIS SCRIPT DOES NOT DO
   ---------------------------------------------------------------------------
   1.  It does not create __EFMigrationsHistory. A database built from this
       file alone is not tracked by EF Migrations. The intended sequence is:
       write the entity configurations to match this design, add a migration,
       and deploy with an idempotent migration script. This file is the design
       those migrations must produce, and the reference to review them against.
   2.  It does not compute anything. The SLA clock - operational minutes,
       pause and resume, next operational start, overdue - is application code
       reading these tables. The schema records state and enforces the rules
       that must survive two users at once; it does not iterate a calendar.
   3.  It does not enforce four rules that belong in the application layer,
       listed here so they are not mistaken for oversights:
         - at most five return images per handover (a CHECK cannot count
           siblings, and a trigger would be worse than the rule)
         - custom field validation: required, min/max, regex (the definitions
           are stored here, the enforcement is in the write path)
         - branch scoping (deliberately NOT a global query filter: reach
           depends on the caller, and a model-level filter reading request
           state behaves differently in the background jobs, where there is no
           caller at all)
         - a 29 February recurring holiday is observed on 28 February in
           non-leap years (R2-10; the calendar service owns this rule)
   4.  It seeds no locations, employees or users, and no operational-hour rows.
       A location with no calendar falls back to Monday-Friday 09:00-18:00, so
       the ticket system works on day one and each branch is configured when
       somebody who knows its hours sits down to do it.
   5.  Optimistic concurrency on the five temporal tables uses [SysStartTime]
       (R2-1). In EF, map the period-start column and mark it as a concurrency
       token; on every other editable table [RowVersion] remains the token.
   =========================================================================== */
/* ============================================================================
   ADDITIVE EXTENSION - NEW SERVICE MULTI-LEVEL APPROVAL WORKFLOW
   Original consolidated schema above remains unchanged.
   ============================================================================ */
/*
    ============================================================================
    AMS - NEW SERVICE MULTI-LEVEL APPROVAL WORKFLOW EXTENSION  (REVISION 2)
    ============================================================================
    PURPOSE
        Adds the missing sequential approval workflow for Service Desk
        RequestKind = 'NewService'. This is an ADDITIVE companion to the existing
        consolidated design. It does not alter or replace any existing table,
        column, constraint, index, seed, or workflow.
    REVISION 2 FIXES IN THIS EXTENSION
        R2-11  CK_RequestApprovalStep_Activation permits Cancelled/Skipped
               steps that never activated.
        R2-12  ApprovalNotificationLog no longer cascades from the instance.
               Approval history - instances, decisions, notification evidence -
               is NEVER deleted; the NO ACTION FKs enforce that deliberately.
        R2-13  One active default workflow, enforced by a filtered unique index.
        R2-14  rowversion columns declared NOT NULL.
        R2-16  ApprovalStageApproverRule gains [RowVersion] (it is editable).
    EXPECTED APPLICATION FLOW
        1. A Branch Admin creates a NewService ServiceRequest on behalf of an
           employee, including NewServiceRequestDetail/items.
        2. The application selects one active ApprovalWorkflowDefinition and
           creates one RequestApprovalInstance.
        3. All enabled definition stages and approver rules are SNAPSHOTTED into
           RequestApprovalStep and RequestApprovalParticipant. Later changes to
           a definition therefore never rewrite an in-flight request's history.
        4. Only the first step is activated. Its notification is written to the
           existing Notifications.EmailOutbox in the SAME transaction; its id is
           recorded in ApprovalNotificationLog.
        5. An approver records a RequestApprovalDecision. The application locks
           the step/instance rows, evaluates Any/All approval mode, completes the
           step, activates the next step, and queues the next e-mail atomically.
        6. Approval of the final step marks the instance Approved. A rejection
           marks the step and instance Rejected and does not activate later steps.
        7. Each material event is also added to the existing RequestHistory as
           EntryKind = 'Automation' or 'Transition', so the request keeps one
           chronological timeline.
    MODULE RULES PRESERVED
        - All new tables live in ServiceDesk.
        - All declared foreign keys remain inside ServiceDesk.
        - Identity/Organization/Notifications references store ids only, matching
          the consolidated design's no-cross-schema-FK rule.
        - User-editable configuration and runtime rows carry rowversion.
        - E-mail delivery uses the existing Notifications.EmailOutbox; this
          extension records evidence and idempotency, not a second mail queue.
    IMPORTANT
        The database stores state and protects concurrency/idempotency. The
        application workflow service performs approver resolution, transactions,
        mail rendering, and sequential advancement.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* ===========================================================================
   1. WORKFLOW DEFINITIONS
   =========================================================================== */
/*  A versioned approval route. A template may select a workflow explicitly;
    otherwise the application chooses the active default matching location and
    priority. LocationId is Organization.Branch, id only.
    Do not edit a published definition in place. Retire it and create a new
    VersionNumber. Runtime steps are snapshots, but immutable published versions
    also make configuration review and audit much clearer. */
IF OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[ApprovalWorkflowDefinition] (
        [Id]                    int            NOT NULL IDENTITY,
        [WorkflowName]          nvarchar(150)  NOT NULL,
        [VersionNumber]         int            NOT NULL,
        [Description]           nvarchar(500)  NULL,
        [ServiceTemplateId]     int            NULL,
        [LocationId]            int            NULL,  -- Organization.Branch, id only
        [Priority]              nvarchar(20)   NULL,  -- NULL means every priority
        [IsDefault]             bit            NOT NULL CONSTRAINT [DF_ApprovalWorkflowDefinition_IsDefault] DEFAULT (0),
        [IsPublished]           bit            NOT NULL CONSTRAINT [DF_ApprovalWorkflowDefinition_IsPublished] DEFAULT (0),
        [IsActive]              bit            NOT NULL,
        [EffectiveFromUtc]      datetime2      NULL,
        [EffectiveToUtc]        datetime2      NULL,
        [CreatedOnUtc]          datetime2      NOT NULL,
        [CreatedBy]             nvarchar(100)  NULL,
        [ModifiedOnUtc]         datetime2      NULL,
        [ModifiedBy]            nvarchar(100)  NULL,
        [RowVersion]            rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_ApprovalWorkflowDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalWorkflowDefinition_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [CK_ApprovalWorkflowDefinition_Priority] CHECK ([Priority] IS NULL OR [Priority] IN (N'Low', N'Medium', N'High', N'Critical')),
        CONSTRAINT [CK_ApprovalWorkflowDefinition_EffectiveRange] CHECK ([EffectiveToUtc] IS NULL OR [EffectiveFromUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc]),
        CONSTRAINT [FK_ApprovalWorkflowDefinition_ServiceTemplate_ServiceTemplateId]
            FOREIGN KEY ([ServiceTemplateId]) REFERENCES [ServiceDesk].[ServiceTemplate] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Ordered levels in a workflow. ApprovalMode:
      Any - one approval completes the level; a rejection rejects the level.
      All - every resolved participant must approve; one rejection rejects it.
    DueAfterMinutes supports reminder/escalation jobs without embedding timers in
    UI code. EscalateAfterMinutes is measured after the step becomes due. */
IF OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowStage]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[ApprovalWorkflowStage] (
        [Id]                       int            NOT NULL IDENTITY,
        [ApprovalWorkflowId]       int            NOT NULL,
        [StageNumber]              int            NOT NULL,
        [StageName]                nvarchar(150)  NOT NULL,
        [ApprovalMode]             nvarchar(10)   NOT NULL,
        [DueAfterMinutes]          int            NULL,
        [ReminderAfterMinutes]     int            NULL,
        [ReminderRepeatMinutes]    int            NULL,
        [EscalateAfterMinutes]     int            NULL,
        [AllowDelegation]          bit            NOT NULL CONSTRAINT [DF_ApprovalWorkflowStage_AllowDelegation] DEFAULT (0),
        [IsEnabled]                bit            NOT NULL,
        [CreatedOnUtc]             datetime2      NOT NULL,
        [CreatedBy]                nvarchar(100)  NULL,
        [ModifiedOnUtc]            datetime2      NULL,
        [ModifiedBy]               nvarchar(100)  NULL,
        [RowVersion]               rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_ApprovalWorkflowStage] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalWorkflowStage_Number] CHECK ([StageNumber] > 0),
        CONSTRAINT [CK_ApprovalWorkflowStage_Mode] CHECK ([ApprovalMode] IN (N'Any', N'All')),
        CONSTRAINT [CK_ApprovalWorkflowStage_Timers] CHECK (
            ([DueAfterMinutes] IS NULL OR [DueAfterMinutes] > 0) AND
            ([ReminderAfterMinutes] IS NULL OR [ReminderAfterMinutes] > 0) AND
            ([ReminderRepeatMinutes] IS NULL OR [ReminderRepeatMinutes] > 0) AND
            ([EscalateAfterMinutes] IS NULL OR [EscalateAfterMinutes] >= 0)
        ),
        CONSTRAINT [FK_ApprovalWorkflowStage_ApprovalWorkflowDefinition_ApprovalWorkflowId]
            FOREIGN KEY ([ApprovalWorkflowId]) REFERENCES [ServiceDesk].[ApprovalWorkflowDefinition] ([Id]) ON DELETE CASCADE
    );
END
GO
/*  Describes how the workflow service finds the approvers for a stage.
    ResolverType meanings:
      User                 ResolverUserId (Identity.User, id only)
      Role                 ResolverRoleId (Identity.Role, id only)
      Capability           ResolverCapabilityName (Identity.Capability, name only)
      EmployeeManager      manager of OnBehalfOfEmployeeId, resolved by app/org data
      RequesterManager     manager of RequestedByEmployeeId, resolved by app/org data
      LocationBranchAdmin  branch-scoped users holding the configured capability
      CustomEmail          ResolverEmail (external/non-login approver)
    Resolution occurs once at submission. Resolved people are snapshotted in
    RequestApprovalParticipant, preventing staff/role changes from rewriting an
    in-flight approval. */
IF OBJECT_ID(N'[ServiceDesk].[ApprovalStageApproverRule]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[ApprovalStageApproverRule] (
        [Id]                       int            NOT NULL IDENTITY,
        [ApprovalWorkflowStageId]  int            NOT NULL,
        [ResolverType]             nvarchar(30)   NOT NULL,
        [ResolverUserId]           int            NULL,  -- Identity.User, id only
        [ResolverRoleId]           int            NULL,  -- Identity.Role, id only
        [ResolverCapabilityName]   nvarchar(80)   NULL,  -- Identity.Capability, name only
        [ResolverEmail]            nvarchar(256)  NULL,
        [DisplayName]              nvarchar(150)  NULL,
        [IsRequired]               bit            NOT NULL CONSTRAINT [DF_ApprovalStageApproverRule_IsRequired] DEFAULT (1),
        [IsEnabled]                bit            NOT NULL,
        [CreatedOnUtc]             datetime2      NOT NULL,
        [CreatedBy]                nvarchar(100)  NULL,
        [ModifiedOnUtc]            datetime2      NULL,
        [ModifiedBy]               nvarchar(100)  NULL,
        [RowVersion]               rowversion     NOT NULL,   -- R2-16
        CONSTRAINT [PK_ApprovalStageApproverRule] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalStageApproverRule_ResolverType] CHECK ([ResolverType] IN (
            N'User', N'Role', N'Capability', N'EmployeeManager', N'RequesterManager',
            N'LocationBranchAdmin', N'CustomEmail'
        )),
        CONSTRAINT [CK_ApprovalStageApproverRule_Value] CHECK (
            ([ResolverType] = N'User'                AND [ResolverUserId] IS NOT NULL) OR
            ([ResolverType] = N'Role'                AND [ResolverRoleId] IS NOT NULL) OR
            ([ResolverType] = N'Capability'          AND [ResolverCapabilityName] IS NOT NULL) OR
            ([ResolverType] = N'EmployeeManager') OR
            ([ResolverType] = N'RequesterManager') OR
            ([ResolverType] = N'LocationBranchAdmin' AND [ResolverCapabilityName] IS NOT NULL) OR
            ([ResolverType] = N'CustomEmail'         AND [ResolverEmail] IS NOT NULL)
        ),
        CONSTRAINT [FK_ApprovalStageApproverRule_ApprovalWorkflowStage_ApprovalWorkflowStageId]
            FOREIGN KEY ([ApprovalWorkflowStageId]) REFERENCES [ServiceDesk].[ApprovalWorkflowStage] ([Id]) ON DELETE CASCADE
    );
END
GO
/* ===========================================================================
   2. REQUEST RUNTIME AND IMMUTABLE DECISIONS
   ===========================================================================
   R2-12 DELETION POLICY, stated once for the whole runtime block: an approval
   run is EVIDENCE. Instances, steps, participants, decisions and notification
   log rows are never deleted by the application. The cascades below
   (instance -> step -> participant) exist only so a development database can
   be reset; the NO ACTION FKs from the evidence tables (decision, notification
   log) deliberately BLOCK deletion of any run that has evidence attached.
   =========================================================================== */
/*  One approval run for a NewService request. WorkflowName/Version are copied
    for audit readability. CurrentStageNumber is a queue aid; the authoritative
    history remains in RequestApprovalStep and RequestApprovalDecision. */
IF OBJECT_ID(N'[ServiceDesk].[RequestApprovalInstance]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestApprovalInstance] (
        [Id]                    bigint         NOT NULL IDENTITY,
        [ServiceRequestId]      int            NOT NULL,
        [ApprovalWorkflowId]    int            NOT NULL,
        [WorkflowNameSnapshot]  nvarchar(150)  NOT NULL,
        [WorkflowVersion]       int            NOT NULL,
        [Status]                nvarchar(20)   NOT NULL,
        [CurrentStageNumber]    int            NULL,
        [SubmittedByUserId]     int            NOT NULL,  -- Identity.User, id only
        [SubmittedOnUtc]        datetime2      NOT NULL,
        [CompletedOnUtc]        datetime2      NULL,
        [CancelledOnUtc]        datetime2      NULL,
        [CancelledByUserId]     int            NULL,      -- Identity.User, id only
        [CancellationReason]    nvarchar(500)  NULL,
        [CreatedOnUtc]          datetime2      NOT NULL,
        [CreatedBy]             nvarchar(100)  NULL,
        [ModifiedOnUtc]         datetime2      NULL,
        [ModifiedBy]            nvarchar(100)  NULL,
        [RowVersion]            rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_RequestApprovalInstance] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestApprovalInstance_Status] CHECK ([Status] IN (N'Pending', N'Approved', N'Rejected', N'Cancelled')),
        CONSTRAINT [CK_RequestApprovalInstance_Version] CHECK ([WorkflowVersion] > 0),
        CONSTRAINT [CK_RequestApprovalInstance_CurrentStage] CHECK ([CurrentStageNumber] IS NULL OR [CurrentStageNumber] > 0),
        CONSTRAINT [FK_RequestApprovalInstance_ServiceRequest_ServiceRequestId]
            FOREIGN KEY ([ServiceRequestId]) REFERENCES [ServiceDesk].[ServiceRequest] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestApprovalInstance_ApprovalWorkflowDefinition_ApprovalWorkflowId]
            FOREIGN KEY ([ApprovalWorkflowId]) REFERENCES [ServiceDesk].[ApprovalWorkflowDefinition] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Snapshot of one workflow level for one request. A worker activates exactly
    one pending step at a time. Skipped is available for a deliberate application
    rule or administrator action and must always be recorded in history.
    R2-11: a step may be Cancelled (instance cancelled at an earlier stage) or
    Skipped without ever having been activated, so the activation CHECK admits
    those two states alongside Waiting. */
IF OBJECT_ID(N'[ServiceDesk].[RequestApprovalStep]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestApprovalStep] (
        [Id]                         bigint         NOT NULL IDENTITY,
        [RequestApprovalInstanceId]  bigint         NOT NULL,
        [ApprovalWorkflowStageId]    int            NOT NULL,
        [StageNumber]                int            NOT NULL,
        [StageNameSnapshot]          nvarchar(150)  NOT NULL,
        [ApprovalModeSnapshot]       nvarchar(10)   NOT NULL,
        [Status]                     nvarchar(20)   NOT NULL,
        [ActivatedOnUtc]             datetime2      NULL,
        [DueOnUtc]                   datetime2      NULL,
        [CompletedOnUtc]             datetime2      NULL,
        [OutcomeRemarks]             nvarchar(1000) NULL,
        [CreatedOnUtc]               datetime2      NOT NULL,
        [CreatedBy]                  nvarchar(100)  NULL,
        [ModifiedOnUtc]              datetime2      NULL,
        [ModifiedBy]                 nvarchar(100)  NULL,
        [RowVersion]                 rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_RequestApprovalStep] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestApprovalStep_Number] CHECK ([StageNumber] > 0),
        CONSTRAINT [CK_RequestApprovalStep_Mode] CHECK ([ApprovalModeSnapshot] IN (N'Any', N'All')),
        CONSTRAINT [CK_RequestApprovalStep_Status] CHECK ([Status] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Skipped', N'Cancelled')),
        CONSTRAINT [CK_RequestApprovalStep_Activation] CHECK ([Status] IN (N'Waiting', N'Cancelled', N'Skipped') OR [ActivatedOnUtc] IS NOT NULL),   -- R2-11
        CONSTRAINT [FK_RequestApprovalStep_RequestApprovalInstance_RequestApprovalInstanceId]
            FOREIGN KEY ([RequestApprovalInstanceId]) REFERENCES [ServiceDesk].[RequestApprovalInstance] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestApprovalStep_ApprovalWorkflowStage_ApprovalWorkflowStageId]
            FOREIGN KEY ([ApprovalWorkflowStageId]) REFERENCES [ServiceDesk].[ApprovalWorkflowStage] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Concrete approvers resolved when the request is submitted. Address and name
    are snapshots so the notification/decision record remains understandable if
    a user later changes name/e-mail or leaves the business. */
IF OBJECT_ID(N'[ServiceDesk].[RequestApprovalParticipant]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestApprovalParticipant] (
        [Id]                       bigint         NOT NULL IDENTITY,
        [RequestApprovalStepId]    bigint         NOT NULL,
        [ApproverRuleId]           int            NOT NULL,
        [ApproverUserId]           int            NULL,  -- Identity.User, id only
        [ApproverEmployeeId]       int            NULL,  -- Organization.Employee, id only
        [ApproverNameSnapshot]     nvarchar(150)  NOT NULL,
        [ApproverEmailSnapshot]    nvarchar(256)  NOT NULL,
        [IsRequired]               bit            NOT NULL,
        [ParticipantStatus]        nvarchar(20)   NOT NULL,
        [DelegatedToUserId]        int            NULL,  -- Identity.User, id only
        [DelegatedOnUtc]           datetime2      NULL,
        [CreatedOnUtc]             datetime2      NOT NULL,
        [CreatedBy]                nvarchar(100)  NULL,
        [RowVersion]               rowversion     NOT NULL,   -- R2-14
        CONSTRAINT [PK_RequestApprovalParticipant] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestApprovalParticipant_Identity] CHECK ([ApproverUserId] IS NOT NULL OR [ApproverEmployeeId] IS NOT NULL OR [ApproverEmailSnapshot] <> N''),
        CONSTRAINT [CK_RequestApprovalParticipant_Status] CHECK ([ParticipantStatus] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Delegated', N'Cancelled')),
        CONSTRAINT [FK_RequestApprovalParticipant_RequestApprovalStep_RequestApprovalStepId]
            FOREIGN KEY ([RequestApprovalStepId]) REFERENCES [ServiceDesk].[RequestApprovalStep] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RequestApprovalParticipant_ApprovalStageApproverRule_ApproverRuleId]
            FOREIGN KEY ([ApproverRuleId]) REFERENCES [ServiceDesk].[ApprovalStageApproverRule] ([Id]) ON DELETE NO ACTION
    );
END
GO
/*  Append-only decision audit. ClientDecisionId is generated by the client or
    API and makes a retried approve/reject command idempotent. ParticipantId is
    unique because one participant gets one final decision in an approval run.
    R2-12: the NO ACTION FK is part of the deletion policy - a run with a
    recorded decision can never be deleted. */
IF OBJECT_ID(N'[ServiceDesk].[RequestApprovalDecision]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[RequestApprovalDecision] (
        [Id]                              bigint           NOT NULL IDENTITY,
        [RequestApprovalParticipantId]    bigint           NOT NULL,
        [ClientDecisionId]                uniqueidentifier NOT NULL,
        [Decision]                        nvarchar(20)     NOT NULL,
        [Remarks]                         nvarchar(1000)   NULL,
        [ActedByUserId]                   int              NULL,  -- Identity.User, id only
        [ActedByEmailSnapshot]            nvarchar(256)    NOT NULL,
        [Source]                          nvarchar(20)     NOT NULL,
        [DecidedOnUtc]                    datetime2        NOT NULL,
        [SourceIpAddress]                 nvarchar(64)     NULL,
        [UserAgent]                       nvarchar(500)    NULL,
        CONSTRAINT [PK_RequestApprovalDecision] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestApprovalDecision_Decision] CHECK ([Decision] IN (N'Approved', N'Rejected')),
        CONSTRAINT [CK_RequestApprovalDecision_Source] CHECK ([Source] IN (N'Application', N'EmailLink', N'Api')),
        CONSTRAINT [FK_RequestApprovalDecision_RequestApprovalParticipant_RequestApprovalParticipantId]
            FOREIGN KEY ([RequestApprovalParticipantId]) REFERENCES [ServiceDesk].[RequestApprovalParticipant] ([Id]) ON DELETE NO ACTION
    );
END
GO
/* ===========================================================================
   3. NOTIFICATION EVIDENCE AND IDEMPOTENCY
   =========================================================================== */
/*  The existing Notifications.EmailOutbox sends the message. This table records
    why it was queued and prevents a retrying worker from queuing the same logical
    e-mail more than once. EmailOutboxId is cross-schema, id only by design.
    R2-12: EVERY FK here is NO ACTION. The previous revision cascaded from the
    instance while holding NO ACTION FKs to step and participant - a mixed
    cascade/no-action pattern whose delete either fails at run time or depends
    on internal ordering. Evidence rows block deletion instead, uniformly. */
IF OBJECT_ID(N'[ServiceDesk].[ApprovalNotificationLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [ServiceDesk].[ApprovalNotificationLog] (
        [Id]                              bigint           NOT NULL IDENTITY,
        [RequestApprovalInstanceId]       bigint           NOT NULL,
        [RequestApprovalStepId]           bigint           NULL,
        [RequestApprovalParticipantId]    bigint           NULL,
        [NotificationType]                nvarchar(30)     NOT NULL,
        [IdempotencyKey]                  uniqueidentifier NOT NULL,
        [RecipientAddress]                nvarchar(256)    NOT NULL,
        [SubjectSnapshot]                 nvarchar(300)    NOT NULL,
        [EmailOutboxId]                   bigint           NULL,  -- Notifications.EmailOutbox, id only
        [Status]                          nvarchar(20)     NOT NULL,
        [AttemptCount]                    int              NOT NULL CONSTRAINT [DF_ApprovalNotificationLog_AttemptCount] DEFAULT (0),
        [LastError]                       nvarchar(500)    NULL,
        [QueuedOnUtc]                     datetime2        NOT NULL,
        [SentOnUtc]                       datetime2        NULL,
        CONSTRAINT [PK_ApprovalNotificationLog] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalNotificationLog_Type] CHECK ([NotificationType] IN (
            N'ApprovalRequired', N'Reminder', N'Escalation', N'StepApproved',
            N'RequestApproved', N'RequestRejected', N'RequestCancelled'
        )),
        CONSTRAINT [CK_ApprovalNotificationLog_Status] CHECK ([Status] IN (N'Queued', N'Sent', N'Failed', N'Skipped')),
        CONSTRAINT [CK_ApprovalNotificationLog_Attempts] CHECK ([AttemptCount] >= 0),
        CONSTRAINT [FK_ApprovalNotificationLog_RequestApprovalInstance_RequestApprovalInstanceId]
            FOREIGN KEY ([RequestApprovalInstanceId]) REFERENCES [ServiceDesk].[RequestApprovalInstance] ([Id]) ON DELETE NO ACTION,   -- R2-12 (was CASCADE)
        CONSTRAINT [FK_ApprovalNotificationLog_RequestApprovalStep_RequestApprovalStepId]
            FOREIGN KEY ([RequestApprovalStepId]) REFERENCES [ServiceDesk].[RequestApprovalStep] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalNotificationLog_RequestApprovalParticipant_RequestApprovalParticipantId]
            FOREIGN KEY ([RequestApprovalParticipantId]) REFERENCES [ServiceDesk].[RequestApprovalParticipant] ([Id]) ON DELETE NO ACTION
    );
END
GO
/* ===========================================================================
   4. INDEXES AND CONCURRENCY GUARANTEES
   =========================================================================== */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]'), N'UX_ApprovalWorkflowDefinition_NameVersion', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ApprovalWorkflowDefinition_NameVersion]
        ON [ServiceDesk].[ApprovalWorkflowDefinition] ([WorkflowName], [VersionNumber]);
/* R2-13: one live default. Two active defaults meant the submission path
   picked whichever sorted first - the exact failure mode this design exists
   to prevent. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]'), N'UX_ApprovalWorkflowDefinition_OneActiveDefault', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ApprovalWorkflowDefinition_OneActiveDefault]
        ON [ServiceDesk].[ApprovalWorkflowDefinition] ([IsDefault])
        WHERE [IsDefault] = 1 AND [IsActive] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]'), N'IX_ApprovalWorkflowDefinition_Match', N'IndexID') IS NULL
    CREATE INDEX [IX_ApprovalWorkflowDefinition_Match]
        ON [ServiceDesk].[ApprovalWorkflowDefinition] ([ServiceTemplateId], [LocationId], [Priority], [EffectiveFromUtc])
        WHERE [IsActive] = 1 AND [IsPublished] = 1;
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowStage]'), N'UX_ApprovalWorkflowStage_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ApprovalWorkflowStage_Number]
        ON [ServiceDesk].[ApprovalWorkflowStage] ([ApprovalWorkflowId], [StageNumber]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalStageApproverRule]'), N'IX_ApprovalStageApproverRule_Stage', N'IndexID') IS NULL
    CREATE INDEX [IX_ApprovalStageApproverRule_Stage]
        ON [ServiceDesk].[ApprovalStageApproverRule] ([ApprovalWorkflowStageId], [IsEnabled]);
/* One unfinished approval run per request prevents two submission retries from
   creating two parallel approval chains and duplicate mail. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalInstance]'), N'UX_RequestApprovalInstance_OnePending', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalInstance_OnePending]
        ON [ServiceDesk].[RequestApprovalInstance] ([ServiceRequestId])
        WHERE [Status] = N'Pending';
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalInstance]'), N'IX_RequestApprovalInstance_Request', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestApprovalInstance_Request]
        ON [ServiceDesk].[RequestApprovalInstance] ([ServiceRequestId], [SubmittedOnUtc] DESC);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalStep]'), N'UX_RequestApprovalStep_Number', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalStep_Number]
        ON [ServiceDesk].[RequestApprovalStep] ([RequestApprovalInstanceId], [StageNumber]);
/* Exactly one active level in an instance enforces sequential approval even if
   two workers try to advance the same request concurrently. */
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalStep]'), N'UX_RequestApprovalStep_OnePending', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalStep_OnePending]
        ON [ServiceDesk].[RequestApprovalStep] ([RequestApprovalInstanceId])
        WHERE [Status] = N'Pending';
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalStep]'), N'IX_RequestApprovalStep_Due', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestApprovalStep_Due]
        ON [ServiceDesk].[RequestApprovalStep] ([DueOnUtc])
        WHERE [Status] = N'Pending';
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalParticipant]'), N'UX_RequestApprovalParticipant_Resolved', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalParticipant_Resolved]
        ON [ServiceDesk].[RequestApprovalParticipant] ([RequestApprovalStepId], [ApproverRuleId], [ApproverEmailSnapshot]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalParticipant]'), N'IX_RequestApprovalParticipant_Inbox', N'IndexID') IS NULL
    CREATE INDEX [IX_RequestApprovalParticipant_Inbox]
        ON [ServiceDesk].[RequestApprovalParticipant] ([ApproverUserId], [ParticipantStatus], [RequestApprovalStepId]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalDecision]'), N'UX_RequestApprovalDecision_Participant', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalDecision_Participant]
        ON [ServiceDesk].[RequestApprovalDecision] ([RequestApprovalParticipantId]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[RequestApprovalDecision]'), N'UX_RequestApprovalDecision_ClientId', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_RequestApprovalDecision_ClientId]
        ON [ServiceDesk].[RequestApprovalDecision] ([ClientDecisionId]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalNotificationLog]'), N'UX_ApprovalNotificationLog_Idempotency', N'IndexID') IS NULL
    CREATE UNIQUE INDEX [UX_ApprovalNotificationLog_Idempotency]
        ON [ServiceDesk].[ApprovalNotificationLog] ([IdempotencyKey]);
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalNotificationLog]'), N'IX_ApprovalNotificationLog_Instance', N'IndexID') IS NULL
    CREATE INDEX [IX_ApprovalNotificationLog_Instance]
        ON [ServiceDesk].[ApprovalNotificationLog] ([RequestApprovalInstanceId], [QueuedOnUtc]);   -- R2-12 FK support
IF INDEXPROPERTY(OBJECT_ID(N'[ServiceDesk].[ApprovalNotificationLog]'), N'IX_ApprovalNotificationLog_Outbox', N'IndexID') IS NULL
    CREATE INDEX [IX_ApprovalNotificationLog_Outbox]
        ON [ServiceDesk].[ApprovalNotificationLog] ([EmailOutboxId])
        WHERE [EmailOutboxId] IS NOT NULL;
GO
/* ===========================================================================
   5. CAPABILITIES
   ===========================================================================
   Registration does not grant access. Map these to roles/users deliberately.
   A Branch Admin normally receives new-service.raise, while approvers receive
   new-service.approve through the appropriate role/capability arrangement.
*/
INSERT INTO [Identity].[Capability]
       ([Name], [Module], [Description], [CreatedOnUtc], [CreatedBy])
SELECT v.[Name], N'ServiceDesk', v.[Description], SYSUTCDATETIME(), N'schema-design'
FROM (VALUES
        (N'new-service.raise',
         N'Raise a New Service request for an employee and submit it for approval.'),
        (N'new-service.approve',
         N'Approve or reject an assigned New Service approval step.'),
        (N'new-service.approval-workflow.manage',
         N'Create, publish, retire and inspect New Service approval workflows.'),
        (N'new-service.approval-workflow.view',
         N'View approval progress and decision history for permitted requests.'),
        (N'new-service.approval.cancel',
         N'Cancel an in-progress New Service approval with a recorded reason.')
     ) AS v([Name], [Description])
WHERE NOT EXISTS (
    SELECT 1 FROM [Identity].[Capability] c WHERE c.[Name] = v.[Name]
);
GO
/* ===========================================================================
   6. VERIFICATION
   =========================================================================== */
SELECT v.[Expected] AS [MissingApprovalWorkflowTable]
FROM (VALUES
        (N'[ServiceDesk].[ApprovalWorkflowDefinition]'),
        (N'[ServiceDesk].[ApprovalWorkflowStage]'),
        (N'[ServiceDesk].[ApprovalStageApproverRule]'),
        (N'[ServiceDesk].[RequestApprovalInstance]'),
        (N'[ServiceDesk].[RequestApprovalStep]'),
        (N'[ServiceDesk].[RequestApprovalParticipant]'),
        (N'[ServiceDesk].[RequestApprovalDecision]'),
        (N'[ServiceDesk].[ApprovalNotificationLog]')
     ) AS v([Expected])
WHERE OBJECT_ID(v.[Expected], N'U') IS NULL;
GO
/*  R2-19: the count of the FINISHED database, base design plus this
    extension. Expect 87 module tables across 16 schemas, and 92 rows in
    sys.tables once the five temporal history tables are included.
    ServiceDesk is 20: 12 from Section 7 and 8 from this extension. */
SELECT  s.[name]                                                  AS [Schema],
        COUNT(t.[object_id])                                      AS [TablesInclHistory],
        SUM(CASE WHEN t.[temporal_type] = 1 THEN 1 ELSE 0 END)    AS [HistoryTables]
FROM    sys.schemas s
        LEFT JOIN sys.tables t ON t.[schema_id] = s.[schema_id]
WHERE   s.[name] IN (N'Identity', N'Organization', N'Assets', N'Allocations', N'Movements',
                     N'Transfers', N'ServiceDesk', N'ServiceLevel', N'Contracts', N'Verification',
                     N'Discovery', N'SapSync', N'Notifications', N'Audit', N'DataImport')
GROUP BY ROLLUP (s.[name])
ORDER BY GROUPING(s.[name]), s.[name];
GO
PRINT N'AMS New Service approval workflow extension applied.';
GO
/* ===========================================================================
   7. REQUIRED APPLICATION TRANSACTION RULES (REFERENCE, NOT SQL EXECUTION)
   ===========================================================================
   SUBMIT REQUEST TRANSACTION
     - Confirm ServiceRequest.RequestKind = 'NewService'.
     - Confirm it has no pending RequestApprovalInstance.
     - Select exactly one matching active, published workflow.
     - Create the instance, all step snapshots, and resolved participants.
     - Mark stage 1 Pending; leave later stages Waiting.
     - Queue stage-1 messages in Notifications.EmailOutbox.
     - Add ApprovalNotificationLog and RequestHistory rows.
     - Commit all operations together.
   APPROVE/REJECT TRANSACTION
     - Authorize the caller against the resolved participant and capability.
     - Lock/re-read instance, current step, and participant rowversions.
     - Insert the immutable decision using ClientDecisionId for idempotency.
     - Update participant state.
     - Evaluate the step's snapshotted Any/All mode.
     - On rejection: reject the step and instance; notify requester/creator.
       Mark later Waiting steps Cancelled (permitted un-activated - R2-11).
     - On step approval: complete it and activate exactly the next stage.
     - If no next stage: approve the instance and continue service fulfilment.
     - Queue all e-mails through Notifications.EmailOutbox and record their ids.
     - Add RequestHistory entries and commit all operations together.
   BACKGROUND WORKER
     - Find pending steps by DueOnUtc.
     - Queue reminders/escalations using deterministic IdempotencyKey values.
     - Retry through Notifications.EmailOutbox; never send SMTP mail directly
       from the web request or Angular application.
   DELETION (R2-12)
     - Approval runs are never deleted. Cancel them; the record stays. The
       NO ACTION FKs on decisions and notification logs make deletion of a
       run with evidence fail by design.
   SECURITY
     - Approval links must contain a short-lived, single-use signed token; store
       only its hash if token persistence is required.
     - E-mail is notification, not proof of authorization. Revalidate identity,
       participant assignment, current step, status, and rowversion on action.
     - Angular only displays approval state. The API owns every transition.
*/
