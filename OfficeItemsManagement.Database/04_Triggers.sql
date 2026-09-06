-- ============================================================
-- 办公物品管理系统 — 触发器
-- ============================================================
USE OfficeItemsDB;

DELIMITER //

-- -----------------------------------------------------------
-- 1. 入库后自动更新库存（备选：与 sp_StockIn 配合使用）
--    如果直接 INSERT StockIn，触发器自动累加库存
-- -----------------------------------------------------------
CREATE TRIGGER trg_StockIn_AfterInsert
AFTER INSERT ON StockIn
FOR EACH ROW
BEGIN
    UPDATE Items SET Quantity = Quantity + NEW.Quantity
    WHERE ItemCode = NEW.ItemCode;
END //

-- -----------------------------------------------------------
-- 2. 入库删除时回退库存
-- -----------------------------------------------------------
CREATE TRIGGER trg_StockIn_AfterDelete
AFTER DELETE ON StockIn
FOR EACH ROW
BEGIN
    UPDATE Items SET Quantity = GREATEST(0, Quantity - OLD.Quantity)
    WHERE ItemCode = OLD.ItemCode;
END //

-- -----------------------------------------------------------
-- 3. 领用确认时自动扣减库存
-- -----------------------------------------------------------
CREATE TRIGGER trg_StockOut_AfterUpdate
AFTER UPDATE ON StockOut
FOR EACH ROW
BEGIN
    IF OLD.Status = 0 AND NEW.Status = 1 THEN
        UPDATE Items SET Quantity = GREATEST(0, Quantity - NEW.Quantity)
        WHERE ItemCode = NEW.ItemCode;
    END IF;
END //

-- -----------------------------------------------------------
-- 4. 领用记录删除时恢复库存（仅已确认的）
-- -----------------------------------------------------------
CREATE TRIGGER trg_StockOut_AfterDelete
AFTER DELETE ON StockOut
FOR EACH ROW
BEGIN
    IF OLD.Status = 1 THEN
        UPDATE Items SET Quantity = Quantity + OLD.Quantity
        WHERE ItemCode = OLD.ItemCode;
    END IF;
END //

DELIMITER ;
