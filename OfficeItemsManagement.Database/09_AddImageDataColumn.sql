-- ============================================================
-- 图片存储迁移：文件系统 → 数据库
-- 为 Items 表添加 ImageData LONGBLOB 列
-- ============================================================
USE OfficeItemsDB;

-- 添加新列（保留原 ImagePath 作兼容）
ALTER TABLE Items ADD COLUMN ImageData LONGBLOB NULL AFTER ImagePath;
