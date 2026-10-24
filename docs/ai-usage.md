# AI Usage

This document records, per development step, which parts were generated with Claude Code and which were reviewed or decided manually.

## Step B – Test data (T1.4)

### Generated with Claude Code

- Seeder: `DatabaseSeeder` (delete + `SqlBulkCopy` insert in one transaction, identity reseed) and the `--seed [--count N]` command in `Program.cs`.
- Generator: `RequestGenerator` (deterministic data with status, priority and organization distributions and consistent status history) and the `SeedData` source lists.
- Tests: `RequestGeneratorTests` (determinism, status distribution, history consistency, `New` requests).

### Reviewed / decided manually

- TODO: fill in by the developer
