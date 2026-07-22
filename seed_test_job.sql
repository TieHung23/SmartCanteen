-- Tạo 1 ServingJob Queued MỚI (dùng lại TEST SESSION) để test next-job / pull.
-- Chạy lại bao nhiêu lần cũng được → mỗi lần thêm 1 job Queued.
-- Chạy: docker exec -i smartcanteen-postgres psql -U smartcanteen -d smartcanteen < seed_test_job.sql
DO $$
DECLARE oid uuid := gen_random_uuid();
        z   uuid := '00000000-0000-0000-0000-000000000000';
        sid uuid;
BEGIN
  SELECT "Id" INTO sid FROM "Sessions" WHERE "Name" = 'TEST SESSION' LIMIT 1;
  IF sid IS NULL THEN
    sid := gen_random_uuid();
    INSERT INTO "Sessions"("Id","Name","Description","IsActive","CreatedAtUtc","CreatedBy","UpdatedBy","IsDeleted")
    VALUES (sid,'TEST SESSION','hybrid test',true,now(),z,z,false);
  END IF;
  INSERT INTO "Orders"("Id","SessionId","Status","CreatedAtUtc","CreatedBy","UpdatedBy","IsDeleted")
  VALUES (oid,sid,4,now(),z,z,false);
  INSERT INTO "ServingJobs"("Id","OrderId","Status","CreatedAtUtc","CreatedBy","UpdatedBy","IsDeleted")
  VALUES (gen_random_uuid(),oid,0,now(),z,z,false);
  RAISE NOTICE '+1 Queued job cho order %', oid;
END $$;
SELECT count(*) AS queued_jobs FROM "ServingJobs" WHERE "Status" = 0;
