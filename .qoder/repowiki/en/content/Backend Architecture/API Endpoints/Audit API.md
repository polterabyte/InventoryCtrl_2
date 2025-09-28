# Audit API

<cite>
**Referenced Files in This Document**   
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs) - *Updated with comprehensive documentation*
- [AuditLogDto.cs](file://src/Inventory.Shared/DTOs/AuditLogDto.cs) - *Updated response schema*
- [AuditService.cs](file://src/Inventory.API/Services/AuditService.cs) - *Updated service implementation*
- [AuditLog.cs](file://src/Inventory.API/Models/AuditLog.cs) - *Updated model with enhanced fields*
- [ActionType.cs](file://src/Inventory.Shared/Enums/ActionType.cs) - *Action type enumeration*
- [ApiEndpoints.cs](file://src/Inventory.Shared/Constants/ApiEndpoints.cs) - *API endpoint definitions*
</cite>

## Update Summary
**Changes Made**   
- Updated documentation to reflect comprehensive audit API functionality
- Added detailed information about filtering parameters and response structure
- Enhanced description of specialized endpoints and their use cases
- Updated response schema to include all available fields
- Added comprehensive usage examples for various audit scenarios

## Table of Contents
1. [Introduction](#introduction)
2. [Authentication and Authorization](#authentication-and-authorization)
3. [Core Endpoints](#core-endpoints)
4. [Specialized Endpoints](#specialized-endpoints)
5. [Export Functionality](#export-functionality)
6. [Cleanup Endpoint](#cleanup-endpoint)
7. [Response Schema](#response-schema)
8. [Rate Limiting](#rate-limiting)
9. [Usage Examples](#usage-examples)
10. [Error Handling](#error-handling)

## Introduction

The Audit API provides comprehensive audit trail functionality for tracking system activities, user actions, and entity changes within the inventory management system. This API enables administrators and managers to monitor, analyze, and maintain a complete history of operations performed on various entities such as products, users, categories, and other system components.

The audit system captures detailed information about each operation including timestamps, user information, IP addresses, user agents, HTTP methods, URLs, status codes, and duration. It also records changes made to entities with before and after values, enabling complete change tracking and rollback analysis.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L12-L450)

## Authentication and Authorization

All endpoints in the Audit API require authentication via JWT (JSON Web Token). The API implements role-based access control with different authorization requirements for various endpoints.

The primary authorization roles are:
- **Admin**: Full access to all audit endpoints including cleanup functionality
- **Manager**: Access to all audit viewing and export endpoints, but cannot perform cleanup operations

The `[Authorize(Roles = "Admin,Manager")]` attribute is applied at the controller level, granting access to both Admin and Manager roles for most endpoints. The cleanup endpoint has an additional `[Authorize(Roles = "Admin")]` attribute that restricts access to Admin users only.

Requests must include a valid JWT token in the Authorization header using the Bearer scheme:
```
Authorization: Bearer <your-jwt-token>
```

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L12-L15)

## Core Endpoints

### GET /api/audit

Retrieves audit logs with comprehensive filtering and pagination capabilities. This is the primary endpoint for querying audit data with multiple filter options.

#### Parameters

| Parameter | Type | Description | Constraints |
|---------|------|-------------|------------|
| entityName | string | Filter by entity name (e.g., "Product", "User") | Case-insensitive partial match |
| action | string | Filter by action performed | Case-insensitive partial match |
| actionType | ActionType | Filter by action type enum | See ActionType enum below |
| entityType | string | Filter by entity type | Case-insensitive partial match |
| userId | string | Filter by user ID | Exact match |
| userName | string | Filter by username | Case-insensitive partial match |
| startDate | DateTime | Filter by start date (inclusive) | ISO 8601 format |
| endDate | DateTime | Filter by end date (inclusive) | ISO 8601 format |
| dateFrom | DateTime | Alternative to startDate | ISO 8601 format |
| dateTo | DateTime | Alternative to endDate | ISO 8601 format |
| severity | string | Filter by severity level | INFO, WARNING, ERROR, CRITICAL |
| requestId | string | Filter by request ID for tracing | Exact match |
| isSuccess | boolean | Filter by success status | true or false |
| ipAddress | string | Filter by client IP address | Exact match |
| page | integer | Page number for pagination | Minimum: 1, Default: 1 |
| pageSize | integer | Number of records per page | Maximum: 100, Default: 50 |

#### Response

Returns a paginated response with the following structure:
```json
{
  "logs": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 50,
  "totalPages": 3
}
```

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L33-L125)

## Specialized Endpoints

### GET /api/audit/entity/{entityName}/{entityId}

Retrieves audit logs for a specific entity identified by its type and ID. This endpoint is optimized for viewing the complete history of changes to a particular entity.

**Parameters:**
- entityName (path): Type of entity (e.g., "Product", "User")
- entityId (path): Unique identifier of the entity

**Example:**
```
GET /api/audit/entity/Product/P12345
```

This returns all audit logs related to the product with ID P12345.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L127-L164)

### GET /api/audit/user/{userId}

Retrieves audit logs for a specific user within a specified time period. This endpoint helps analyze user activity patterns and track actions performed by individual users.

**Parameters:**
- userId (path): User ID to retrieve logs for
- days (query): Number of days to look back (default: 30, maximum: 365)

**Example:**
```
GET /api/audit/user/U98765?days=7
```

This returns audit logs for user U98765 from the past 7 days.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L127-L164)

### GET /api/audit/trace/{requestId}

Retrieves audit logs by request ID for distributed tracing. This endpoint is essential for debugging and tracking operations that span multiple services or components.

**Parameters:**
- requestId (path): Request ID to trace across operations

**Example:**
```
GET /api/audit/trace/req-7a8b9c1d2e3f
```

This returns all audit logs associated with the request ID req-7a8b9c1d2e3f, allowing complete traceability of a specific operation.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L255-L275)

### GET /api/audit/statistics

Retrieves audit log statistics for analytical purposes. This endpoint provides aggregated data about system activity over a specified period.

**Parameters:**
- days (query): Number of days to analyze (default: 30, maximum: 365)

**Response includes:**
- Total logs count
- Successful vs failed logs
- Logs grouped by action, entity, severity, and user
- Average response time
- Top errors by frequency

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L164-L190)

## Export Functionality

### GET /api/audit/export

Exports audit logs to CSV format for offline analysis, reporting, or archival purposes. This endpoint returns the complete set of filtered logs in a comma-separated values format that can be opened in spreadsheet applications.

The CSV includes all available audit log fields with proper escaping for values containing commas, quotes, or newlines.

#### Parameters

All filtering parameters from the main GET /api/audit endpoint are supported, allowing you to export only the specific logs you need.

#### CSV Format

The exported CSV includes the following columns:
- Id
- Timestamp
- Action
- ActionType
- EntityType
- EntityId
- EntityName
- Username
- UserId
- IpAddress
- UserAgent
- HttpMethod
- Url
- StatusCode
- Duration
- IsSuccess
- ErrorMessage
- Severity
- RequestId
- Changes
- OldValues
- NewValues
- Description
- Metadata

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L277-L335)

## Cleanup Endpoint

### DELETE /api/audit/cleanup

Removes old audit logs to manage storage and maintain optimal performance. This endpoint is restricted to Admin users only.

**Parameters:**
- daysToKeep (query): Number of days of logs to retain (default: 90, minimum: 30)

**Example:**
```
DELETE /api/audit/cleanup?daysToKeep=180
```

This removes all audit logs older than 180 days, keeping only the most recent 6 months of data.

**Response:**
```json
{
  "deletedCount": 1500,
  "daysToKeep": 180,
  "cleanupDate": "2025-01-15T10:30:00Z"
}
```

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L337-L370)

## Response Schema

### AuditLogDto

The primary response model for audit log entries contains comprehensive information about each audited action.

| Field | Type | Description |
|------|------|-------------|
| Id | integer | Unique identifier for the audit log |
| EntityName | string | Name of the entity being audited |
| EntityId | string | ID of the entity being audited |
| Action | string | Action performed (CREATE, UPDATE, etc.) |
| ActionType | ActionType | Enum representing the type of action |
| EntityType | string | Type of entity being audited |
| Changes | string | JSON containing detailed changes |
| RequestId | string | Request ID for tracing operations |
| UserId | string | ID of the user who performed the action |
| Username | string | Username of the user who performed the action |
| OldValues | string | JSON of values before the change |
| NewValues | string | JSON of values after the change |
| Description | string | Additional description of the action |
| Timestamp | DateTime | When the action occurred |
| IpAddress | string | Client IP address |
| UserAgent | string | Client user agent string |
| HttpMethod | string | HTTP method used |
| Url | string | Requested URL/endpoint |
| StatusCode | integer | HTTP status code returned |
| Duration | long | Operation duration in milliseconds |
| Metadata | string | Additional metadata in JSON format |
| Severity | string | Severity level (INFO, WARNING, ERROR) |
| IsSuccess | boolean | Whether the action was successful |
| ErrorMessage | string | Error message if action failed |

**Section sources**
- [AuditLogDto.cs](file://src/Inventory.Shared/DTOs/AuditLogDto.cs#L7-L35)

### ActionType Enum

The ActionType enum provides standardized categorization of actions for better filtering and analysis.

| Value | Description |
|------|-------------|
| Create (1) | New entity created |
| Read (2) | Entity accessed/viewed |
| Update (3) | Entity modified |
| Delete (4) | Entity removed |
| Login (5) | User authentication |
| Logout (6) | User session ended |
| Refresh (7) | Token refresh |
| Export (8) | Data exported |
| Import (9) | Data imported |
| Search (10) | Search performed |
| Other (99) | Custom or unspecified action |

**Section sources**
- [ActionType.cs](file://src/Inventory.Shared/Enums/ActionType.cs#L1-L61)

## Rate Limiting

The Audit API implements rate limiting to prevent abuse and ensure system stability. The rate limiting policy varies by user role:

- **Admin**: 200 requests per minute
- **Manager**: 100 requests per minute
- **User**: 50 requests per minute
- **Anonymous**: 20 requests per minute

The rate limiting is configured using the "ApiPolicy" token bucket algorithm with auto-replenishment. When the rate limit is exceeded, the API returns HTTP status code 429 (Too Many Requests) with appropriate retry-after headers.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L13-L14)
- [Program.cs](file://src/Inventory.API/Program.cs#L315-L350)

## Usage Examples

### Querying Product Update Logs

To retrieve audit logs for product updates in the last 7 days with pagination:

```
GET /api/audit?entityName=Product&action=UPDATE&startDate=2025-01-08T00:00:00&endDate=2025-01-15T23:59:59&page=1&pageSize=25
```

This returns up to 25 audit logs for product updates from the past week, showing who made changes, what was changed, and when.

### Analyzing User Activity Patterns

To analyze a specific user's activity over the past month:

```
GET /api/audit/user/U12345?days=30
```

This helps identify patterns in user behavior, such as frequent actions, peak usage times, and common error patterns.

### Investigating System Issues

To investigate errors in the system over the past 3 days:

```
GET /api/audit?isSuccess=false&severity=ERROR&startDate=2025-01-12T00:00:00&pageSize=100
```

This retrieves up to 100 failed operations with error severity from the last 3 days, helping identify and troubleshoot system issues.

### Compliance Reporting

To export all audit logs for compliance reporting:

```
GET /api/audit/export?startDate=2024-10-01T00:00:00&endDate=2024-12-31T23:59:59
```

This exports a complete CSV file of all audit logs from the fourth quarter of 2024 for regulatory compliance purposes.

**Section sources**
- [AuditController.cs](file://src/Inventory.API/Controllers/AuditController.cs#L33-L335)