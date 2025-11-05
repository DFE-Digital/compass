-- Clear all operational reporting returns
-- This will cascade delete ProductMetricValues due to foreign key constraints

BEGIN TRANSACTION;

-- Show counts before deletion
SELECT 'Before deletion:' as Status;
SELECT COUNT(*) as ProductReturns_Count FROM ProductReturns;
SELECT COUNT(*) as ProductMetricValues_Count FROM ProductMetricValues;

-- Delete all product metric values first (to be safe)
DELETE FROM ProductMetricValues;
PRINT 'Deleted all ProductMetricValues';

-- Delete all product returns
DELETE FROM ProductReturns;
PRINT 'Deleted all ProductReturns';

-- Show counts after deletion
SELECT 'After deletion:' as Status;
SELECT COUNT(*) as ProductReturns_Count FROM ProductReturns;
SELECT COUNT(*) as ProductMetricValues_Count FROM ProductMetricValues;

COMMIT TRANSACTION;

SELECT 'Cleanup completed successfully' as Status;
