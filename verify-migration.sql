-- Verify data migration
SELECT 'Users' as TableName, COUNT(*) as Count FROM Users UNION ALL
SELECT 'PerformanceMetrics', COUNT(*) FROM PerformanceMetrics UNION ALL
SELECT 'ProductReturns', COUNT(*) FROM ProductReturns UNION ALL
SELECT 'Objectives', COUNT(*) FROM Objectives UNION ALL
SELECT 'Risks', COUNT(*) FROM Risks UNION ALL
SELECT 'Issues', COUNT(*) FROM Issues UNION ALL
SELECT 'Milestones', COUNT(*) FROM Milestones UNION ALL
SELECT 'Actions', COUNT(*) FROM Actions UNION ALL
SELECT 'RiskTypes', COUNT(*) FROM RiskTypes UNION ALL
SELECT 'RiskTiers', COUNT(*) FROM RiskTiers UNION ALL
SELECT 'ActionSources', COUNT(*) FROM ActionSources;
