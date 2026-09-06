-- ============================================================
-- 办公物品管理系统 — 生成100万条入库记录（性能测试用）
-- 使用 MySQL 存储过程循环插入
-- 运行前请确保基础数据已导入
-- ============================================================
USE OfficeItemsDB;

DELIMITER //

CREATE PROCEDURE sp_SeedMillionStockIn()
BEGIN
    DECLARE i INT DEFAULT 1;
    DECLARE v_ItemCode VARCHAR(20);
    DECLARE v_ItemCodes VARCHAR(2000) DEFAULT 'P001,P002,S001,S002,S003,S004,T001,D001,G001,O001';
    DECLARE v_Idx INT;
    DECLARE v_Price DECIMAL(18,2);

    -- 关闭自动提交以加速
    SET autocommit = 0;

    WHILE i <= 1000000 DO
        -- 随机选一个物品
        SET v_Idx = 1 + FLOOR(RAND() * 10);
        SET v_ItemCode = SUBSTRING_INDEX(SUBSTRING_INDEX(v_ItemCodes, ',', v_Idx), ',', -1);

        -- 随机单价 1~500
        SET v_Price = 1 + ROUND(RAND() * 499, 2);

        INSERT INTO StockIn (StockInNo, ItemCode, PurchaseDate, Quantity, UnitPrice, TotalPrice)
        VALUES (
            CONCAT('SI', DATE_FORMAT(DATE_ADD('2024-01-01', INTERVAL FLOOR(RAND() * 730) DAY), '%Y%m%d'),
                   LPAD(i, 6, '0')),
            v_ItemCode,
            DATE_ADD('2024-01-01', INTERVAL FLOOR(RAND() * 730) DAY),
            1 + FLOOR(RAND() * 100),
            v_Price,
            (1 + FLOOR(RAND() * 100)) * v_Price
        );

        IF i % 10000 = 0 THEN
            COMMIT;
        END IF;

        SET i = i + 1;
    END WHILE;

    COMMIT;
    SET autocommit = 1;
END //

DELIMITER ;

-- 执行（慎用，生产环境需约 5-15 分钟）:
-- CALL sp_SeedMillionStockIn();
