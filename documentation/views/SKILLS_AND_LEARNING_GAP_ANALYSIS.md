# Skills and Learning Module - Gap Analysis

## Overview

This document compares the implemented features against the specification requirements to identify gaps and missing functionality.

## ✅ Completed Requirements

### Core Features Implemented
- ✅ Data models (5 models) with relationships
- ✅ Database migration applied
- ✅ Course library browsing with filters
- ✅ Training request form (approved or custom)
- ✅ Learning history tracking
- ✅ Feedback submission
- ✅ Professional profile management
- ✅ Admin CRUD for courses
- ✅ Approval workflow (basic)
- ✅ Role-based dashboards
- ✅ Navigation section added
- ✅ RBAC feature seeded

## ⚠️ Partially Implemented

### 1. Browse Courses (Issue 2)
**Status**: ✅ Basic implementation, ⚠️ Missing features
- ✅ Filter by profession, capability area, provider, mode, cost
- ✅ Course detail pages
- ✅ Empty states
- ⚠️ **Missing**: "Recommended for you" section (code exists but not fully implemented)
- ⚠️ **Missing**: "Suggested based on capability gaps" section

### 2. Training Request Form (Issue 4)
**Status**: ✅ Basic implementation, ⚠️ Missing features
- ✅ Form supports approved courses OR custom entry
- ✅ Required fields enforced (justification)
- ✅ Saves as Draft or Submitted
- ⚠️ **Missing**: Users can update draft requests (no edit action)
- ⚠️ **Missing**: Users can withdraw draft requests (no withdraw action)
- ⚠️ **Missing**: Optional Decision entity creation/linking UI

### 3. Request Approval Workflow (Issue 5)
**Status**: ✅ Basic implementation, ⚠️ Missing features
- ✅ Approvers can see pending requests
- ✅ Approve/reject functionality
- ✅ Status changes recorded
- ⚠️ **Missing**: Approvers can add comments (field exists but no UI)
- ⚠️ **Missing**: Optional Decision entity creation/linking UI
- ⚠️ **Missing**: Line manager approval (only L&S role implemented)

### 4. Learning History (Issue 6)
**Status**: ✅ Implemented
- ✅ Shows completed courses with dates and feedback
- ✅ Shows upcoming bookings
- ⚠️ **Missing**: Shows training awaiting feedback (no filter/view for this)

### 5. Feedback Submission (Issue 7)
**Status**: ✅ Basic implementation, ⚠️ Missing features
- ✅ Rating (1–5) required
- ✅ Text feedback optional
- ✅ Feedback updates TrainingRecord
- ⚠️ **Missing**: Evidence file upload (field exists but no upload UI)
- ⚠️ **Missing**: File upload validation (10 MB limit, virus scanning)

### 6. L&D Admin Dashboard (Issue 8)
**Status**: ⚠️ Partially implemented
- ✅ Basic dashboard views
- ⚠️ **Missing**: Filters: grade, directorate (only profession, capability, status, cost)
- ⚠️ **Missing**: Shows people with no training in last 12 months (exists in TrainingGaps but not main dashboard)
- ⚠️ **Missing**: Export functionality (no export buttons/actions)

### 7. Requests Dashboard (Issue 9)
**Status**: ✅ Basic implementation, ⚠️ Missing features
- ✅ Tabs: Pending, Approved, Rejected (via filters)
- ✅ Search by user or profession
- ✅ Bulk approval not allowed (correctly implemented)
- ⚠️ **Missing**: "On-hold" tab/filter
- ⚠️ **Missing**: Approval events recorded in audit logs (no explicit audit logging)

## ❌ Missing Requirements

### 1. Nudging Engine (Issue 10) - **NOT IMPLEMENTED**
**Requirements**:
- System suggests training based on capability gaps
- Recommendations appear in user dashboard
- Users can dismiss or accept recommendations
- Audit captures when nudges applied

**Status**: ❌ Not implemented
- No recommendation engine
- No nudge display in dashboard
- No dismiss/accept functionality
- No audit tracking for nudges

### 2. RBAC Permissions (Issue 11) - **PARTIALLY IMPLEMENTED**
**Requirements**:
- Users can only see their data ✅
- Managers see requests for their team ❌
- Heads of Profession see profession-wide data ✅
- DesignOps sees entire department ✅
- Admin-only access to configuration screens ✅

**Status**: ⚠️ Partially implemented
- Basic RBAC structure exists
- Feature seeded
- Missing: Manager role implementation
- Missing: Granular permission enforcement (uses [Authorize] only)

### 3. Reporting & Analytics (Issue 12) - **NOT IMPLEMENTED**
**Requirements**:
- Reports available for spend, requests, outcomes, satisfaction
- Year-to-date and trend reporting
- Compatible with existing Compass reporting patterns

**Status**: ❌ Not implemented
- No dedicated reporting views
- No export functionality
- No trend analysis views
- Basic statistics exist but not comprehensive reporting

### 4. Notifications and Reminders (Issue 13) - **NOT IMPLEMENTED**
**Requirements**:
- Notify user when request approved/rejected
- Notify approver of new pending request
- Reminder email when feedback overdue
- Uses Notify templates

**Status**: ❌ Not implemented
- No email notification system
- No reminder functionality
- No integration with Notify

### 5. API Endpoints (Issue 14) - **NOT IMPLEMENTED**
**Requirements**:
- Follows Compass API versioning (/api/v1/...)
- Read/Write endpoints secured by role
- API returns consistent shape with Compass standards
- Audit logging enabled

**Status**: ❌ Not implemented
- No API controllers
- No API endpoints
- No API documentation

### 6. Budget Management (HOP/Central Ops Admin) - **PARTIALLY IMPLEMENTED**
**Requirements**:
- Forecast cost and L&D demand ✅
- Support financial planning (available, spent, remaining, actual, estimated) ✅
- Costs per provider ✅
- Identify department-wide capability trends ✅
- View user's history scoped to profession ✅
- **Missing**: Manage total L&D budget amount for FY (Central ops admin only)

**Status**: ⚠️ Partially implemented
- Budget dashboard exists
- Missing: Budget amount management UI

### 7. Integration Requirements - **NOT IMPLEMENTED**
**Requirements**:
- Integrates with RAID and milestones
- Supports analytics and KPIs

**Status**: ❌ Not implemented
- No RAID integration
- No milestone linking
- Basic analytics exist but not comprehensive

## ⚠️ Validation Gaps

### Model Validation
- ✅ Basic validation exists ([Required], [StringLength], [Range])
- ⚠️ **Missing**: More comprehensive validation rules:
  - URL validation for TrainingCourse.Url
  - Email validation where needed
  - Date range validation (date_attended >= date_approved)
  - Cost validation (must be positive)
  - Status enum validation (should use enums instead of strings)

### Controller Validation
- ✅ Basic validation exists (ModelState checks)
- ⚠️ **Missing**: 
  - Input sanitization (XSS prevention)
  - More comprehensive business rule validation
  - File upload validation (size, type, virus scanning)

## ⚠️ Non-Functional Requirements Gaps

### Accessibility (WCAG 2.2 AA)
- ✅ Basic ARIA labels exist
- ✅ Keyboard navigation (bootstrap default)
- ⚠️ **Needs Review**:
  - Colour contrast ratios
  - Focus states visibility
  - Error message association
  - Screen reader testing

### Usability
- ✅ GOV.UK Design System styling used
- ✅ Form validation patterns
- ✅ Empty states provided
- ⚠️ **Missing**: 
  - Save progress on long forms (draft functionality exists but could be enhanced)
  - More comprehensive error messages

### Performance
- ✅ Database indexes configured
- ⚠️ **Needs Verification**:
  - Query optimization (needs testing)
  - Page load times (needs measurement)
  - Concurrent user support (needs load testing)

### Security
- ✅ [Authorize] attributes on controllers
- ✅ [ValidateAntiForgeryToken] on POST actions
- ⚠️ **Missing**:
  - Explicit audit logging (relies on EF Core change tracking)
  - Input sanitization (needs XSS prevention)
  - File upload security (virus scanning, type validation)
  - RBAC enforcement at view level (currently controller level only)

## 📋 Summary

### Implementation Status
- **Core Features**: ~75% complete
- **Advanced Features**: ~30% complete
- **Non-Functional**: ~60% complete

### Priority Gaps to Address

**High Priority**:
1. ❌ Nudging engine (Issue 10)
2. ❌ Export functionality (Issue 8)
3. ⚠️ File upload for evidence (Issue 7)
4. ⚠️ Draft request edit/withdraw (Issue 4)
5. ⚠️ Approval comments (Issue 5)

**Medium Priority**:
6. ❌ Notifications (Issue 13)
7. ❌ Reporting & Analytics (Issue 12)
8. ⚠️ Budget amount management (HOP requirement)
9. ⚠️ Manager role support (RBAC)

**Low Priority**:
10. ❌ API endpoints (Issue 14)
11. ❌ RAID/milestone integration
12. ⚠️ Enhanced validation rules
13. ⚠️ Comprehensive audit logging

## 🎯 Recommendations

1. **Immediate**: Add missing basic features (file upload, draft edit/withdraw, approval comments)
2. **Short-term**: Implement nudging engine and export functionality
3. **Medium-term**: Add notifications and comprehensive reporting
4. **Long-term**: API endpoints and advanced integrations

