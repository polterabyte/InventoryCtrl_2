# User-Warehouse Assignment API Documentation

## Overview

The User-Warehouse Assignment feature provides comprehensive API endpoints for managing many-to-many relationships between Users and Warehouses with access control levels. This documentation outlines all the new and updated API endpoints.

## Controllers

### UserWarehouseController (`/api/UserWarehouse`)

Primary controller for warehouse assignment management operations.

#### Core Assignment Operations

| Endpoint | Method | Description | Authorization | Response Type |
|----------|--------|-------------|---------------|---------------|
| `/users/{userId}/warehouses` | POST | Assign warehouse to user | Admin, Manager | UserWarehouseDto |
| `/users/{userId}/warehouses/{warehouseId}` | DELETE | Remove warehouse assignment | Admin, Manager | Success message |
| `/users/{userId}/warehouses/{warehouseId}` | PUT | Update assignment details | Admin, Manager | UserWarehouseDto |
| `/users/{userId}/warehouses/{warehouseId}/default` | PUT | Set default warehouse | Admin, Manager | Success message |

#### Query Operations

| Endpoint | Method | Description | Authorization | Response Type |
|----------|--------|-------------|---------------|---------------|
| `/users/{userId}/warehouses` | GET | Get user's warehouses | Self/Admin/Manager | List<UserWarehouseDto> |
| `/warehouses/{warehouseId}/users` | GET | Get warehouse's users | Admin, Manager | List<UserWarehouseDto> |
| `/users/{userId}/warehouses/{warehouseId}/access` | GET | Check warehouse access | Self/Admin/Manager | Access status |
| `/users/{userId}/accessible-warehouses` | GET | Get accessible warehouse IDs | Self/Admin/Manager | List<int> |

#### Bulk Operations

| Endpoint | Method | Description | Authorization | Response Type |
|----------|--------|-------------|---------------|---------------|
| `/warehouses/bulk-assign` | POST | Bulk assign users to warehouse | Admin | Bulk result |

### UserController (`/api/User`)

Alternative endpoints for user-centric warehouse operations.

| Endpoint | Method | Description | Authorization |
|----------|--------|-------------|---------------|
| `/{id}/warehouses` | GET | Get user's warehouses | Admin, Manager |
| `/{id}/warehouses` | POST | Assign warehouse | Admin, Manager |
| `/{id}/warehouses/{warehouseId}` | DELETE | Remove assignment | Admin, Manager |
| `/{id}/warehouses/{warehouseId}` | PUT | Update assignment | Admin, Manager |
| `/{id}/warehouses/{warehouseId}/default` | PUT | Set default warehouse | Admin, Manager |

### WarehouseController (`/api/Warehouse`)

Alternative endpoints for warehouse-centric user operations.

| Endpoint | Method | Description | Authorization |
|----------|--------|-------------|---------------|
| `/{id}/users` | GET | Get warehouse's users | Admin, Manager |
| `/bulk-assign-users` | POST | Bulk assign users | Admin |

## Data Transfer Objects (DTOs)

### AssignWarehouseDto
```json
{
  "warehouseId": 123,
  "accessLevel": "Full|ReadOnly|Limited",
  "isDefault": true
}
```

### UpdateWarehouseAssignmentDto
```json
{
  "accessLevel": "Full|ReadOnly|Limited",
  "isDefault": true
}
```

### BulkAssignWarehousesDto
```json
{
  "warehouseId": 123,
  "userIds": ["user1", "user2", "user3"],
  "accessLevel": "Full|ReadOnly|Limited",
  "setAsDefault": false
}
```

### UserWarehouseDto
```json
{
  "userId": "user123",
  "userName": "john.doe",
  "userEmail": "john@example.com",
  "warehouseId": 123,
  "warehouseName": "Main Warehouse",
  "warehouseLocation": "New York",
  "accessLevel": "Full",
  "isDefault": true,
  "assignedAt": "2024-01-01T00:00:00Z"
}
```

## Access Levels

| Level | Description | Capabilities |
|-------|-------------|--------------|
| **Full** | Complete access | All read/write operations |
| **ReadOnly** | View-only access | Read operations, reports |
| **Limited** | Restricted access | Specific operation subset |

## Business Rules

### Assignment Rules
- Users can have multiple warehouse assignments
- Only one warehouse per user can be marked as default
- Users cannot be assigned to the same warehouse twice
- Setting a new default automatically unsets the previous one

### Access Control Rules
- **Admin users**: Access to all warehouses regardless of assignments
- **Manager users**: Access to all warehouses regardless of assignments  
- **Regular users**: Access only to explicitly assigned warehouses
- Self-access: Users can view their own assignments and access status

### Default Warehouse Logic
- Used for automatic warehouse selection in transactions
- Only users with warehouse assignments can have a default
- Removing a default assignment clears the user's default warehouse

## HTTP Status Codes

| Code | Description | When Returned |
|------|-------------|---------------|
| 200 | Success | Operation completed successfully |
| 400 | Bad Request | Invalid data, business rule violation |
| 401 | Unauthorized | User not authenticated |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | User, warehouse, or assignment not found |
| 500 | Internal Server Error | Unexpected server error |

## Error Response Format

```json
{
  "success": false,
  "errorMessage": "Description of the error",
  "errors": ["Detailed error 1", "Detailed error 2"],
  "data": null
}
```

## Success Response Format

```json
{
  "success": true,
  "data": { /* Response data */ },
  "errorMessage": null,
  "errors": null
}
```

## Swagger Documentation

All endpoints are fully documented with:
- Comprehensive XML documentation comments
- Request/response examples
- HTTP status code descriptions
- Parameter validation details
- Business rule explanations
- Authorization requirements

Access the interactive API documentation at `/swagger` when the application is running.

## Examples

### Assign Full Access to Warehouse
```bash
POST /api/UserWarehouse/users/user123/warehouses
{
  "warehouseId": 1,
  "accessLevel": "Full",
  "isDefault": true
}
```

### Check User Access to Warehouse
```bash
GET /api/UserWarehouse/users/user123/warehouses/1/access?requiredAccessLevel=ReadOnly
```

### Bulk Assign Users to Warehouse
```bash
POST /api/UserWarehouse/warehouses/bulk-assign
{
  "warehouseId": 1,
  "userIds": ["user1", "user2", "user3"],
  "accessLevel": "ReadOnly",
  "setAsDefault": false
}
```

### Get User's Accessible Warehouses
```bash
GET /api/UserWarehouse/users/user123/accessible-warehouses
```

## Integration Notes

- All endpoints support JSON request/response format
- Authentication required via JWT Bearer token
- CORS configured for cross-origin requests
- Full integration with existing User and Warehouse entities
- Database transactions ensure data consistency
- Comprehensive logging for audit trails