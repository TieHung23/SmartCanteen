-- Seed POOL khay + ô kệ để test luồng hybrid (next-job cần khay Available, không có thì luôn 204).
-- Idempotent: chạy lại không tạo trùng (guard theo Code). Status 0 = Available (Tray) / Empty (PickupSlot).
-- Chạy:  docker exec -i smartcanteen-postgres psql -U smartcanteen -d smartcanteen < seed_pool.sql

-- 30 khay: TRAY001..TRAY030, Status=Available(0)
INSERT INTO "Trays" ("Id", "Code", "Status", "CreatedAtUtc", "CreatedBy", "UpdatedBy")
SELECT gen_random_uuid(),
       'TRAY' || lpad(g::text, 3, '0'),
       0,
       now(),
       '00000000-0000-0000-0000-000000000000',
       '00000000-0000-0000-0000-000000000000'
FROM generate_series(1, 30) AS g
WHERE NOT EXISTS (SELECT 1 FROM "Trays" WHERE "Code" = 'TRAY' || lpad(g::text, 3, '0'));

-- 12 ô kệ: SLOT01..SLOT12, Status=Empty(0)
INSERT INTO "PickupSlots" ("Id", "Code", "Status", "CreatedAtUtc", "CreatedBy", "UpdatedBy")
SELECT gen_random_uuid(),
       'SLOT' || lpad(g::text, 2, '0'),
       0,
       now(),
       '00000000-0000-0000-0000-000000000000',
       '00000000-0000-0000-0000-000000000000'
FROM generate_series(1, 12) AS g
WHERE NOT EXISTS (SELECT 1 FROM "PickupSlots" WHERE "Code" = 'SLOT' || lpad(g::text, 2, '0'));

-- Kiểm tra
SELECT 'Trays Available'    AS pool, count(*) FROM "Trays"       WHERE "Status" = 0 AND "IsDeleted" = false
UNION ALL
SELECT 'PickupSlots Empty'  AS pool, count(*) FROM "PickupSlots" WHERE "Status" = 0 AND "IsDeleted" = false;
