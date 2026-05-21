# Data Model: Phase 1 — Global Exception Handling & Pagination Infrastructure

**Status**: Complete

## Overview

This phase introduces no database entities or schema changes. Two infrastructure types are created:

## Types

### Error Response (implicit — no dedicated class)

The error response is serialized from an anonymous object in the middleware. Its shape is:

| Field     | Type   | Description                                |
|-----------|--------|--------------------------------------------|
| `code`    | string | Machine-readable error code (e.g. `NOT_FOUND`, `CONFLICT`, `BUSINESS_RULE`, `SERVER_ERROR`) |
| `message` | string | Human-readable error description           |

**Mapping rules:**

| Domain Exception          | HTTP Status | Error Code       |
|---------------------------|-------------|------------------|
| `NotFoundException`       | 404         | `NOT_FOUND`      |
| `ConflictException`       | 409         | `CONFLICT`       |
| `BusinessRuleException`   | 422         | `BUSINESS_RULE`  |
| Any other `Exception`     | 500         | `SERVER_ERROR`   |

### PagedList&lt;T&gt; (HR.Shared/Pagination/)

A generic wrapper for paginated query results.

| Property       | Type                | Description                                   |
|----------------|---------------------|-----------------------------------------------|
| `Items`        | `IReadOnlyList<T>`  | The subset of items for the current page       |
| `TotalCount`   | `int`               | Total number of items across all pages         |
| `Page`         | `int`               | Current page number (1-based)                  |
| `PageSize`     | `int`               | Number of items per page                       |
| `TotalPages`   | `int` (computed)    | `Ceiling(TotalCount / PageSize)`               |
| `HasNext`      | `bool` (computed)   | `Page < TotalPages`                            |
| `HasPrevious`  | `bool` (computed)   | `Page > 1`                                     |

**Factory method:** `static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int page, int pageSize, CancellationToken ct)`

**Behavior:**
1. Counts total items via `CountAsync`
2. Skips `(page - 1) * pageSize` items
3. Takes `pageSize` items
4. Returns a new `PagedList<T>` with the results and metadata

## Relationships

- `PagedList<T>` has no relationships — it is a standalone utility type.
- The error response is ephemeral (produced per-request in the middleware, never persisted).
