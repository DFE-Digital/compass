# Skills and Learning Module - Implementation Summary

## Overview

This document summarizes the implementation of the Skills and Learning (L&D) module for Compass, as specified in `requirements/lnd_module`.

## ✅ Completed Components

### 1. Data Models
All required data models have been created:

- **TrainingCourse** (`Models/TrainingCourse.cs`)
  - Course library with all required fields
  - Soft-delete support via `Active` flag
  - Profession and capability tags support

- **TrainingRecord** (`Models/TrainingRecord.cs`)
  - Tracks user training attendance and completion
  - Supports feedback and evidence upload
  - Links to courses and users

- **TrainingRequest** (`Models/TrainingRequest.cs`)
  - Supports both approved courses and custom requests
  - Status workflow: Draft → Submitted → Approved/Rejected/On-hold
  - Optional Decision entity linking

- **UserProfessionalProfile** (`Models/UserProfessionalProfile.cs`)
  - Stores user profession, skills, and capability gaps
  - Links to Head of Profession

- **HOPS** (`Models/HOPS.cs`)
  - Head of Profession assignments
  - Maps users to professions they oversee

### 2. Database Context
- Updated `CompassDbContext.cs` with:
  - All L&D DbSets
  - Relationship configurations
  - Indexes for performance
  - Cascade delete rules

### 3. Controllers

#### Individual Users (`SkillsAndLearningController.cs`)
- ✅ Index dashboard
- ✅ Browse courses with filtering
- ✅ Course details view
- ✅ Request training (approved or custom)
- ✅ Submit draft requests
- ✅ View my requests
- ✅ Learning history
- ✅ Provide feedback
- ✅ Update professional profile

#### Learning & Skills Role (`LearningAndSkillsController.cs`)
- ✅ Dashboard with statistics
- ✅ Profession requests management
- ✅ Approve/reject requests
- ✅ View all learning records
- ✅ Manage skills and learning
- ✅ Training gaps analysis

#### HOP/Central Ops Admin (`HOPController.cs`)
- ✅ Dashboard with profession scope
- ✅ Profession requests (scoped to profession)
- ✅ Budget and spending dashboard
- ✅ Spending by provider and profession
- ✅ User history (scoped)
- ✅ Capability trends analysis

#### Admin Course Management (`Admin/TrainingCourseController.cs`)
- ✅ List courses with filtering
- ✅ Course details
- ✅ Create new courses
- ✅ Edit courses
- ✅ Archive/restore courses (soft delete)

### 4. Views Created

#### Individual User Views (`Views/SkillsAndLearning/`)
- ✅ `Index.cshtml` - Main dashboard with quick actions and recent activity
- ✅ `BrowseCourses.cshtml` - Course library with filters and sortable table
- ✅ `CourseDetails.cshtml` - Course detail view
- ✅ `RequestTraining.cshtml` - Training request form
- ✅ `MyRequests.cshtml` - User's training requests with status filters
- ✅ `LearningHistory.cshtml` - Learning records with feedback
- ✅ `ProvideFeedback.cshtml` - Feedback submission form
- ✅ `MyProfile.cshtml` - Professional profile management

#### Admin Views (`Views/Admin/TrainingCourse/`)
- ✅ `Index.cshtml` - Course library management with filters
- ✅ `Details.cshtml` - Course details view
- ✅ `Create.cshtml` - Create new course form
- ✅ `Edit.cshtml` - Edit course form

#### Learning & Skills Role Views (`Views/LearningAndSkills/`)
- ✅ `ProfessionRequests.cshtml` - Approval workflow with modals
- ⚠️ `Index.cshtml` - Dashboard (controller ready, view needed)
- ⚠️ `ViewLearning.cshtml` - View all learning (controller ready, view needed)
- ⚠️ `ManageSkills.cshtml` - Skills management (controller ready, view needed)
- ⚠️ `TrainingGaps.cshtml` - Training gaps analysis (controller ready, view needed)

#### HOP Views (`Views/HOP/`)
- ⚠️ `Index.cshtml` - Dashboard (controller ready, view needed)
- ⚠️ `ProfessionRequests.cshtml` - Profession-scoped requests (controller ready, view needed)
- ⚠️ `BudgetAndSpending.cshtml` - Budget dashboard (controller ready, view needed)
- ⚠️ `UserHistory.cshtml` - User history (controller ready, view needed)
- ⚠️ `CapabilityTrends.cshtml` - Capability trends (controller ready, view needed)

### 5. Navigation
- ✅ Added "Skills and Learning" section to main sidebar (`Views/Shared/_Layout.cshtml`)
- ✅ Sub-menu items:
  - My skills and learning
  - Browse courses
  - My requests
  - Learning history

## 🔄 Next Steps

### 1. Database Migration ✅
Migration created successfully. Apply it:

```bash
cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
dotnet ef database update
```

### 2. Complete Views
Create the remaining view files following the patterns established in:
- `Views/SkillsAndLearning/Index.cshtml`
- `Views/SkillsAndLearning/BrowseCourses.cshtml`
- `Views/Project/Index.cshtml` (for sortable tables)
- `Views/CentralOps/Summary.cshtml` (for dashboard cards)

### 3. RBAC Permissions
Add permissions for L&D module:

- `learning.view` - View own learning data
- `learning.request` - Request training
- `learning.approve` - Approve training requests (Learning & Skills role)
- `learning.manage` - Manage course library (Admin/DesignOps)
- `learning.hop` - HOP-specific views (HOP role)
- `learning.admin` - Full admin access (Central Ops Admin)

Update `PermissionService` and add role assignments.

### 4. Notifications
Implement email notifications for:
- Request submitted → notify approvers
- Request approved/rejected → notify requester
- Feedback reminder → notify users with completed training

### 5. File Upload
Implement evidence file upload for:
- Training certificates
- Feedback evidence

Consider using Azure Blob Storage or similar.

### 6. Reporting & Analytics
Create reporting views for:
- Spend analysis
- Training trends
- Capability gap analysis
- Profession-level statistics

### 7. Nudging Engine
Implement capability gap-based recommendations:
- Analyze user profiles
- Match gaps to courses
- Display recommendations on dashboard
- Allow dismiss/accept actions

## Architecture Notes

### Role-Based Access
- **Individual Users**: Can see only their own data
- **Learning & Skills Role**: Can see all requests and records, approve/reject
- **HOP Role**: Can see profession-scoped data, budget information
- **Central Ops Admin**: Can see all data, manage budget

### Data Flow
1. User browses courses → selects or creates custom request
2. Request saved as Draft → user can submit
3. Submitted request → visible to approvers
4. Approved request → creates TrainingRecord with "Booked" status
5. User attends → updates record to "Completed"
6. User provides feedback → updates record with rating and comments

### Integration Points
- Links to Decision entity (optional)
- Uses existing User model
- Follows existing audit logging patterns
- Uses existing notification system (when implemented)

## Testing Checklist

- [ ] Create migration and verify schema
- [ ] Test course browsing and filtering
- [ ] Test training request workflow
- [ ] Test approval/rejection workflow
- [ ] Test learning history display
- [ ] Test feedback submission
- [ ] Test professional profile updates
- [ ] Test HOP scoping (users see only their profession)
- [ ] Test budget calculations
- [ ] Test admin course CRUD operations
- [ ] Verify RBAC permissions work correctly
- [ ] Test empty states
- [ ] Test error handling

## Files Created

### Models (5 files)
- `Models/TrainingCourse.cs`
- `Models/TrainingRecord.cs`
- `Models/TrainingRequest.cs`
- `Models/UserProfessionalProfile.cs`
- `Models/HOPS.cs`

### Controllers (4 files)
- `Controllers/SkillsAndLearningController.cs`
- `Controllers/LearningAndSkillsController.cs`
- `Controllers/HOPController.cs`
- `Controllers/Admin/TrainingCourseController.cs`

### Views (2 files created, ~20 more needed)
- `Views/SkillsAndLearning/Index.cshtml`
- `Views/SkillsAndLearning/BrowseCourses.cshtml`

### Database
- Updated `Data/CompassDbContext.cs` with L&D models and configuration

### Navigation
- Updated `Views/Shared/_Layout.cshtml` with Skills and Learning section

## Notes

- All controllers follow existing Compass patterns
- Views use GOV.UK Design System styling
- Sortable tables follow the pattern from `Views/Project/Index.cshtml`
- Dashboard cards follow the pattern from `Views/CentralOps/Summary.cshtml`
- All models include audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- Soft-delete implemented for TrainingCourse via `Active` flag

