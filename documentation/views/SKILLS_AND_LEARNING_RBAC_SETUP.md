# Skills and Learning - RBAC Setup Guide

## Overview

This guide explains how to set up Role-Based Access Control (RBAC) for the Skills and Learning module.

## RBAC System

Compass uses a **Group/Feature/Permission** system:
- **Groups** - Role groups (e.g., "Learning and Skills", "HOP", "Central Operations Admin")
- **Features** - Functional areas (e.g., "skills_and_learning")
- **Permissions** - View, Create, Update, Delete

## Feature Created

The `skills_and_learning` feature has been added to the seeding process in `Program.cs` and will be automatically created when the application starts.

## Required Groups

You need to create the following groups (or use existing ones):

### 1. Learning and Skills Group
- **Name**: "Learning and Skills"
- **Description**: "Users who manage training requests and learning across DfE"
- **Permissions needed**: View, Create, Update for `skills_and_learning` feature

### 2. HOP Group (or use existing)
- **Name**: "Head of Profession" or use existing "Central Operations Admin"
- **Description**: "Heads of Profession who oversee profession-scoped training"
- **Permissions needed**: View for `skills_and_learning` feature

### 3. Central Operations Admin (already exists)
- Already has all permissions for all features including `skills_and_learning`

## Setup Steps

### Option 1: Using Admin UI (Recommended)

1. Navigate to `/Admin/GroupManagement`
2. Create new groups:
   - "Learning and Skills"
   - "Head of Profession" (if not using Central Operations Admin)
3. Assign permissions:
   - For "Learning and Skills" group: Grant View, Create, Update permissions for `skills_and_learning` feature
   - For "Head of Profession" group: Grant View permission for `skills_and_learning` feature
4. Assign users to groups via User Management

### Option 2: Using Database Directly

```sql
-- Create Learning and Skills group
INSERT INTO Groups (Name, Description, IsActive, IsSystemGroup, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
VALUES ('Learning and Skills', 'Users who manage training requests and learning across DfE', 1, 0, GETUTCDATE(), GETUTCDATE(), 'System', 'System');

-- Get the group ID (replace with actual ID)
DECLARE @LearningSkillsGroupId INT = (SELECT Id FROM Groups WHERE Name = 'Learning and Skills');

-- Get the feature ID
DECLARE @SkillsLearningFeatureId INT = (SELECT Id FROM Features WHERE Code = 'skills_and_learning');

-- Grant View, Create, Update permissions
INSERT INTO GroupFeaturePermissions (GroupId, FeatureId, Permission, CreatedAt, CreatedBy)
VALUES 
    (@LearningSkillsGroupId, @SkillsLearningFeatureId, 1, GETUTCDATE(), 'System'), -- View
    (@LearningSkillsGroupId, @SkillsLearningFeatureId, 2, GETUTCDATE(), 'System'), -- Create
    (@LearningSkillsGroupId, @SkillsLearningFeatureId, 3, GETUTCDATE(), 'System'); -- Update

-- Assign user to group (replace email with actual user email)
DECLARE @UserId INT = (SELECT Id FROM Users WHERE Email = 'user@example.com');
INSERT INTO UserGroups (UserId, GroupId, AssignedAt, AssignedBy)
VALUES (@UserId, @LearningSkillsGroupId, GETUTCDATE(), 'System');
```

## Permission Mapping

### Individual Users
- **Access**: All authenticated users can access `SkillsAndLearningController`
- **Scope**: Can only see their own data
- **No special group required** - uses `[Authorize]` attribute only

### Learning and Skills Role
- **Group**: "Learning and Skills"
- **Permissions**: View, Create, Update for `skills_and_learning`
- **Access**: `LearningAndSkillsController` - all training activity

### HOP/Central Ops Admin
- **Group**: "Head of Profession" or "Central Operations Admin"
- **Permissions**: View for `skills_and_learning` (or all permissions if Central Ops Admin)
- **Access**: `HOPController` - profession-scoped views

### Admin/DesignOps
- **Group**: "Central Operations Admin" (already has all permissions)
- **Access**: `Admin/TrainingCourseController` - course library CRUD

## Current Implementation

Currently, all controllers use `[Authorize]` which means:
- ✅ Any authenticated user can access Skills and Learning features
- ✅ Individual users can only see their own data (enforced in controller logic)
- ⚠️ Role-based filtering is implemented in controller logic but not enforced via RBAC attributes

## Future Enhancement

To add RBAC enforcement, add permission checks to controllers:

```csharp
[RequirePermission("skills_and_learning", PermissionType.View)]
public async Task<IActionResult> Index()
{
    // ...
}
```

Or use group-based checks:

```csharp
if (!await _permissionService.IsInGroupAsync(userEmail, "Learning and Skills"))
{
    return Forbid();
}
```

## Testing

After setting up groups and permissions:

1. Assign a test user to "Learning and Skills" group
2. Verify they can access `/LearningAndSkills/Index`
3. Verify they can approve/reject requests
4. Assign a test user to "Head of Profession" group
5. Verify they can access `/HOP/Index`
6. Verify they only see profession-scoped data

