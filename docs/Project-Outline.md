# CSV/Excel Automation - project outline
Created October 14, 2026 | Owner: Robert Walkama | Language: C#

## Purpose
Build reusable, client-specific tools that replace repeated spreadsheet copying, cleanup and import preparation. First demonstration: three supplier exports become a validated master product list, an Excel workbook and an exception report. Sell narrowly scoped Fiverr workflows while continuing larger Photoshop production and .NET integration work on Upwork.

## Delivery status
Windows verification passed October 14, 2026 using .NET SDK 10.0.401: Release build succeeded; CSV acceptance passed (8 rows, 4 accepted, 4 rejected); Excel round-trip passed (4 accepted, 0 rejected). Edge-case testing remains pending. API integrations, scheduling, graphical interface and dashboards are roadmap items, not implemented features. No Fiverr listing or Upwork proposal has been submitted by this project.

## Starter features
- Read UTF-8 comma-delimited CSV with quoted fields, escaped quotes and embedded newlines.
- Read first worksheet of .xlsx; row 1 is the header. Formula cells fail with instructions to export values.
- Map supplier header aliases to a standard schema using JSON configuration.
- Trim values, validate required fields and nonnegative decimal prices using invariant culture.
- Preserve text identifiers such as 001; identifiers in XLSX must be stored as text at source.
- Merge files in ordinal filename order. Deduplicate case-insensitive SKU; first valid record wins.
- Produce clean.csv, clean.xlsx, exceptions.csv with original rejected values, and summary.json.
- Separate, uniquely named output folders keep sources unchanged and previous runs available.
- Prefix formula-like CSV values when exporting for spreadsheet use; XLSX outputs use text cells.
- Return nonzero on file/configuration failure. Outputs are created after input validation; output I/O failures can leave a partial run folder. Only regard exit code 0 as successful.

## Roadmap and scope boundaries
| Stage | Features | Release condition |
|---|---|---|
| MVP hardening | Explicit worksheet selection, delimiter/encoding options, typed dates, integer/currency rules, blank-row policy, maximum input sizes, configuration validation, malformed-record quarantine, staged atomic output, dry-run | Edge-case tests and Windows executable verified |
| Client workflow | Multiple schemas, joins/lookups, computed columns, grouping, reconciliation, formatted multi-sheet reports, import-specific output profiles | Approved sample inputs and expected outputs |
| Repeat execution | Windows Task Scheduler wrapper, stable-file checks, run locks, retry policy, archive/retention, notifications | Document recovery and duplicate-run behavior |
| Connected workflow | REST APIs, OAuth/token storage, pagination, webhooks, durable queue, checkpoint/retry, idempotency, audit logs | Sandbox contract tests, approved scopes and destination-write review |
| Operator interface | Small Windows UI or Blazor dashboard: file selection, mapping preview, run history and failed-job replay | A client need justifies additional development |

Legacy .xls, macros, password-protected files, preservation of existing workbook formatting, arbitrary formula recalculation, accounting decisions and real-time two-way sync are outside starter scope. Convert older workbooks to .xlsx values first. Starter holds files in memory; it is not a large-data streaming engine. Define and measure row/file limits before a client quote.

## What owners may want products to communicate
These are discovery hypotheses and service opportunities, not a measured demand ranking. Export/import is an inexpensive first step; live synchronization needs separate API scope and ongoing maintenance.

| Business task | Connection | Data and result | Discovery questions / risks |
|---|---|---|---|
| Supplier catalog updates | Supplier CSV/XLSX -> Shopify/WooCommerce import | SKU, price, stock, images -> validated catalog update file | Who owns price? Match variants? Preview changes before import |
| Weekly sales report | Etsy/Shopify exports -> Excel | Orders, fees, refunds -> reconciled summary | Date/timezone, refund timing, gross vs net, tax columns |
| Bookkeeping preparation | Store exports -> accountant/QuickBooks/Xero import | Transactions -> approved import layout | Accountant specifies mapping; no automatic tax classification |
| Customer follow-up | CRM/HubSpot exports -> email platform | Contacts and segments -> approved import | Consent, duplicates, unsubscribe suppression |
| Fulfillment handoff | Store -> shipping/inventory system | Order and shipment status | Unique order IDs, retries, partial shipments, source of truth |
| Creative production | CSV/Airtable/database -> Photoshop UXP workflow | Product ID, text, artwork path -> mockups/proofs and completion status | PSD contract, missing assets, export naming, desktop session |
| Management dashboard | SQL/MongoDB/REST -> Blazor/Excel | Operational data -> KPI view | Ownership, access, refresh cadence, metric definitions |
| Team notification | Processing job -> email/Teams/Slack | Success summary or failures | Recipient authorization, sensitive details and notification limits |
| Shared file intake | Google Drive/OneDrive/SFTP -> automation | Approved folder inputs -> output folder | Permissions, file completion detection, retention |

Specific connectors are not included merely because they appear here. Verify API availability, account plan, permissions, fees, current version and rate limits before quoting. Shopify bulk import uses GraphQL mutations and uploaded JSONL; CSV-to-Shopify API work needs a translation/validation adapter.

## Requirements
Developer: supported .NET 10 SDK, NuGet access, Windows for final delivery verification, editor (Visual Studio/VS Code), version control. Starter dependency: ClosedXML 0.105.1; review package/license/security notices at release. CSV parsing uses Microsoft's TextFieldParser. Excel desktop is not needed for file processing; visual output review in Excel is recommended.
Client runtime: agreed Windows environment, read/write folder permissions and disk space; .NET runtime for framework-dependent delivery or tested self-contained executable. Scheduled tasks must use the intended account and accessible paths.
Client inputs: sanitized representative samples, exact header/worksheet layout, delimiter/encoding, identifier types, date/currency formats, duplicate/conflict policy, desired output, volumes, frequency, execution environment and success examples.
Integration requirements: supported endpoint, sandbox, least-privilege authentication, secret storage outside source/config, paging and throttling rules, stable IDs, destination-write approval, conflict policy and monitoring owner.
Quality requirements: input/output row counts reconcile; invalid rows are explained; quoted values and leading-zero IDs survive; repeated runs are predictable; no source overwrite; failure is visible; no credentials or full sensitive records in operational logs. Exception reports contain input data and need agreed access/retention. Do not upload real customer records for a public demo.

## Architecture
Start with one CLI project to prove the workflow. Extract adapters and transformation services only when a second real workflow needs them. Pipeline: file adapter -> header mapping -> normalization -> validation -> deduplication -> export -> run summary. Future layers: Core rules, CSV/XLSX adapters, connector adapters, CLI/desktop/API host and meaningful regression tests.

## Acceptance and demo
Three CSV fixtures contain 8 rows: expect 4 accepted and 4 rejected (duplicate SKU, bad price, missing SKU, negative price). Accepted SKU sequence: 001, 002, 003, 005. Preserve "Mug, blue". Compare CSV and XLSX outputs, then rerun to prove independent outputs.
Before gig launch test: multiline/escaped quotes, UTF-8 text, malformed CSV, missing/duplicate headers, empty files, text SKU 001 in XLSX, rejected formula cells, wrong decimal format, extra/missing fields, output permission failure and missing config. Do not claim large-file performance without measurements.
Demo video: manual problem (5 sec), three sample files (10 sec), run utility (10 sec), clean workbook and exception reasons (20 sec), repeatable workflow/value (10 sec). Use synthetic data only. Gallery: â€œStop repeating spreadsheet cleanupâ€; before/after columns; clean data + exception report. No unmeasured time-saving percentage.

## Commercial sequence
Week 1: compile, run acceptance checks, harden failure handling and record demo.
Week 2: review gig scope, prepare three gallery images and publish after verification.
Weeks 3-4: monitor impressions, clicks, qualified inquiries, orders, effective hourly return; review after 14 days and change one variable at a time. Keep existing Photoshop/.NET gigs active.
Reserve two focused sessions weekly for Upwork fit review and proposals, even during CSV development. Prioritize Photoshop production pipelines, C# API integrations, automation backends and Blazor internal tools. Keep the established $55/hour profile positioning. This small gig becomes an additional proof of delivery, rather than replacing the higher-value positioning.

## Sources checked October 14, 2026
- .NET support: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- ClosedXML package: https://www.nuget.org/packages/ClosedXML/0.105.1
- CSV parser: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.textfieldparser.readfields?view=net-10.0
- Shopify import architecture: https://shopify.dev/docs/apps/build/apis/graphql-admin/bulk-operations/imports
- Existing Drive assessment: Custom Software Gig Opportunities - Fiverr and Upwork - 2026-10-14.
