# View "Work" as a User

## Epic
**Admin Tools & Support Features**

## Feature
**View Work Items as Another User**

## Description
Add a "View as" feature to the Work/Projects section that allows administrators to see what work items another user would see in their "Your work" view. This is useful for troubleshooting, support, or understanding what projects are visible to a specific user.

**Important:** This feature does not change any permissions or grant access to modify data. It only displays what that person would see - administrators cannot perform actions on behalf of the viewed user.

## Requirements

### Functional Requirements
1. Add a "View as" option to the project navigation navbar (available to all users)
2. Create a user selection page that allows users to search and select a user
3. Display the selected user's "Your work" view as if they were logged in
4. Show a clear indicator when viewing as another user
5. Preserve all filters and navigation state when viewing as another user
6. Allow easy return to the user's own view

### Non-Functional Requirements
1. Available to all authenticated users
2. No permission changes occur - read-only view
3. All existing filters (search, RAG status, business area, phase, flagship, priority, status) work in "View as" mode
4. User selection uses the existing user picker component

## Acceptance Criteria

### AC1: Navigation Access
- [ ] "View as" link appears in the project navbar after "All work" and before "Create work entry"
- [ ] "View as" link is available to all authenticated users
- [ ] Clicking "View as" navigates to the user selection page

### AC2: User Selection
- [ ] User selection page displays explanatory content about the feature
- [ ] User picker component allows searching for users by name or email
- [ ] Form validation requires a user to be selected before submission
- [ ] Error message displayed if user is not found
- [ ] Cancel button returns to "Your work" view

### AC3: Viewing as Another User
- [ ] After selecting a user, the page displays their "Your work" view
- [ ] Banner at top shows "Viewing as: [User Name] ([Email])" with "Return to your view" button
- [ ] Projects displayed match what the selected user would see (based on their contacts, SRO, Service Owner, PMO roles)
- [ ] All filters work correctly (search, RAG status, business area, phase, flagship, priority, status)
- [ ] Filter links preserve the userId parameter
- [ ] Status navigation links preserve the userId parameter

### AC4: Permissions & Security
- [ ] All authenticated users can access ViewAs and ViewAsUser actions
- [ ] No data modification capabilities are granted
- [ ] Feature is read-only - no actions can be performed on behalf of the viewed user

### AC5: User Experience
- [ ] Clear explanation of feature purpose and limitations
- [ ] Easy way to return to normal view
- [ ] All existing functionality (filters, sorting, pagination) works in "View as" mode
- [ ] User picker provides clear feedback during search

## Technical Notes

### Implementation Details

#### Controller Changes
- **ProjectController.ViewAs()**: GET action to display user selection page
- **ProjectController.ViewAsUser()**: GET action to display projects as selected user
- **ProjectController.GetProjectsView()**: Updated to accept optional `targetUserEmail` parameter
  - When `targetUserEmail` is provided, filters projects for that user instead of logged-in user
  - Updates user lookup to use target user when in "View as" mode

#### View Changes
- **Views/Project/ViewAs.cshtml**: New view for user selection
  - Uses existing user picker component
  - Includes explanatory content about feature purpose and limitations
  - Left-aligned layout
- **Views/Project/Index.cshtml**: Updated to support "View as" mode
  - Added "View as" to navbar (available to all users)
  - Added banner showing "Viewing as" status
  - Updated all filter links to preserve userId parameter
  - Updated filter form to include userId parameter

#### Data Flow
1. User clicks "View as" → ViewAs action
2. User selects user → ViewAsUser action (user validation)
3. ViewAsUser calls GetProjectsView with targetUserEmail
4. GetProjectsView filters projects using target user's email instead of logged-in user
5. Projects displayed match what target user would see

### Files Modified
- `Controllers/ProjectController.cs`
  - Added `ViewAs()` action
  - Added `ViewAsUser()` action
  - Updated `GetProjectsView()` method signature and logic
- `Views/Project/ViewAs.cshtml` (new file)
- `Views/Project/Index.cshtml`
  - Updated navbar to include "View as" option
  - Added "Viewing as" banner
  - Updated filter links to preserve userId

### Database Impact
- None - read-only feature using existing data

### API Impact
- None - uses existing user picker API endpoints

## Related Work
- User picker component (existing)
- Project filtering logic (existing)
- Permission service (existing)

## Dependencies
- Existing user picker JavaScript component
- Existing user search API (`/api/users/search`)
- Permission service for admin checks

## Testing Checklist

### Manual Testing
- [ ] Verify "View as" link appears for all authenticated users
- [ ] Verify all users can access ViewAs/ViewAsUser URLs
- [ ] Test user selection with valid user
- [ ] Test user selection with invalid/non-existent user
- [ ] Verify projects displayed match selected user's actual "Your work" view
- [ ] Test all filters work in "View as" mode (search, RAG, business area, phase, flagship, priority, status)
- [ ] Verify filter links preserve userId parameter
- [ ] Test "Return to your view" button functionality
- [ ] Verify banner displays correct user name and email
- [ ] Test with users who have different project assignments
- [ ] Verify watched projects are not shown (only "Your work" view)
- [ ] Test pagination if applicable
- [ ] Verify no permission errors occur

### Security Testing
- [ ] Verify all authenticated users can access feature
- [ ] Verify no data modification is possible
- [ ] Verify user email is properly sanitized/validated

### Edge Cases
- [ ] User with no assigned projects
- [ ] User with many assigned projects
- [ ] User selection cancelled
- [ ] Network errors during user search

## Notes
- This is a read-only feature for support and troubleshooting purposes
- Does not grant any additional permissions or capabilities
- All existing project filtering and display logic is reused
- Available to all authenticated users
