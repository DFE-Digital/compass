# View "Work" as a User

## Description
Add a "View as" feature to the Work/Projects section that allows users to see what work items another user would see in their "Your work" view. This is useful for troubleshooting, support, or understanding what projects are visible to a specific user.

**Important:** This feature does not change any permissions or grant access to modify data. It only displays what that person would see - you cannot perform actions on behalf of the viewed user.

## Requirements
1. Add a "View as" option to the project navigation navbar (available to all users)
2. Create a user selection page that allows users to search and select a user using the existing user picker component
3. Display the selected user's "Your work" view as if they were logged in
4. Show a clear banner indicating when viewing as another user with option to return to normal view
5. Preserve all filters and navigation state when viewing as another user
6. All existing filters (search, RAG status, business area, phase, flagship, priority, status) must work in "View as" mode

## Acceptance Criteria
- [ ] "View as" link appears in navbar (available to all users)
- [ ] User selection page with explanatory content
- [ ] Projects displayed match what the selected user would see
- [ ] Banner shows "Viewing as: [User Name] ([Email])" with return button
- [ ] All filters work correctly in "View as" mode
- [ ] Feature is read-only (no data modification)

## Technical Implementation
- New actions: `ProjectController.ViewAs()` and `ProjectController.ViewAsUser()`
- Updated `GetProjectsView()` to accept optional `targetUserEmail` parameter
- New view: `Views/Project/ViewAs.cshtml`
- Updated `Views/Project/Index.cshtml` for navbar and filter preservation
