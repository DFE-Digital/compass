# Staff Role Return Implementation

## Overview

Staff Role Return is a new annual reporting feature under Enterprise Reporting that allows staff members to submit their primary GDD role and up to 5 secondary skills from the Government Digital and Data Capability Framework.

## Database Schema

### Models

1. **StaffRoleReturn** - Annual submission record
   - Links to User (who submitted)
   - Links to GddRole (primary role)
   - Includes Grade (EO, HEO, SEO, G7, G6, SCS1, SCS2, SCS3)
   - Status (Draft, Submitted)
   - Dates (DueDate: 31 March, SubmittedDate, LastModifiedDate)

2. **GddRole** - GDD Framework roles
   - RoleFamily (e.g., Architecture, User-centred design)
   - RoleName
   - RoleLevel
   - DisplayName (combination of level + name)

3. **Skill** - GDD Framework skills
   - SkillName
   - Description
   - Category (auto-categorised from name)

4. **StaffRoleReturnSkill** - Junction table
   - Links StaffRoleReturn to Skills
   - DisplayOrder (1-5)

### Migration

The migration `AddStaffRoleReturn` has been created and applied to the database.

## Admin Management

### GDD Roles Management

Accessed via: **Admin → Settings → GDD Roles tab**

Features:
- View all GDD roles grouped by family
- Create new roles
- Edit existing roles
- Delete roles (blocked if in use)
- Activate/deactivate roles

### Skills Management

Accessed via: **Admin → Settings → Skills tab**

Features:
- View all skills
- Create new skills
- Edit existing skills
- Delete skills (blocked if in use)
- Activate/deactivate skills

## User Submission

### Staff Role Return Form

Accessed via: **Enterprise Reporting → Staff role return**

Features:
- Select primary GDD role from autocomplete dropdown
- Select civil service grade
- Select up to 5 secondary skills from autocomplete dropdown
- Save as Draft or Submit
- View submitted return with read-only details

### Overdue Monitoring

Accessed via: **StaffRoleReturn/Overdue** (Admin only)

Features:
- List all staff who haven't submitted
- Show days overdue or days remaining
- Identify missing submissions for follow-up

## CSV Data Seeding

### Seed GDD Framework Data

The GDD Framework data (roles and skills) can be seeded from the official CSV export.

**Command:**
```bash
./seed-gdd-development.sh
```

Or manually:
```bash
dotnet run --seed-gdd-framework --environment Development --csv-file "https://cf-production-data-exports.s3.eu-west-2.amazonaws.com/exports/Role%20and%20skill%20content%20-%20Capability%20Framework%20-%20Government%20Digital%20and%20Data%20profession2025-10-28_13-40-49.csv"
```

**What gets seeded:**
- All unique GDD roles from the CSV (33 roles)
- All unique skills from the CSV (44 skills)
- Display names automatically formatted
- Categories auto-assigned based on skill name patterns

## Annual Cycle

### Submission Deadline

- **Due date:** 31 March annually
- **Audit trail:** Last submitted date tracked
- **Status:** Draft or Submitted

### Notification & Reminders

**Current implementation:**
- Overdue view for admins to identify missing submissions
- Days remaining/overdue calculated automatically
- Visible in the Staff Role Return interface

**Future enhancements:**
- Email notifications to staff approaching deadline
- Automated reminders
- Bulk email to non-submitters

## File Locations

### Controllers
- `Controllers/StaffRoleReturnController.cs` - User submission and admin monitoring

### Views
- `Views/StaffRoleReturn/Index.cshtml` - Submission form
- `Views/StaffRoleReturn/Overdue.cshtml` - Admin overdue report

### Admin Views
- `Views/Admin/Settings/_GddRolesTab.cshtml` - GDD Roles management tab
- `Views/Admin/Settings/_SkillsTab.cshtml` - Skills management tab
- `Views/Admin/Settings/GddRoles.cshtml` - GDD Roles list
- `Views/Admin/Settings/CreateGddRole.cshtml` - Create role form
- `Views/Admin/Settings/EditGddRole.cshtml` - Edit role form
- `Views/Admin/Settings/Skills.cshtml` - Skills list
- `Views/Admin/Settings/CreateSkill.cshtml` - Create skill form
- `Views/Admin/Settings/EditSkill.cshtml` - Edit skill form

### Models
- `Models/StaffRoleReturn.cs` - All four models

### Data
- `Data/CompassDbContext.cs` - DbContext configuration
- `Data/CompassDbSeeder.cs` - CSV seeding logic

### Seeding
- `SeedGddFramework.cs` - Seeding utility
- `seed-gdd-development.sh` - Shell script

### Navigation
- `Views/Shared/_Layout.cshtml` - Added "Staff role return" to Enterprise Reporting menu

## Usage

### For Staff Members

1. Navigate to Enterprise Reporting → Staff role return
2. Select your primary GDD role from the dropdown
3. Select your civil service grade
4. Optionally select up to 5 secondary skills
5. Choose "Save as Draft" or "Submit"
6. View your submitted return

### For Administrators

**Manage Roles and Skills:**
1. Navigate to Admin → Settings
2. Select GDD Roles or Skills tab
3. Create/edit/delete as needed

**Monitor Submissions:**
1. Navigate to Enterprise Reporting → Staff role return
2. Access the Overdue report to see non-submitters
3. Follow up with staff as needed

## Technical Notes

- Uses Select2 for improved dropdown UX
- Prevents duplicate submissions per user per year
- Enforces 5 secondary skills limit
- Read-only after submission
- History tracking via last modified dates
- Grade validation against allowed values
- Unique constraints prevent duplicate roles/skills

