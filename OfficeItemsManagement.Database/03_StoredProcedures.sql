-- ============================================================
-- 办公物品管理系统 — 存储过程
-- ============================================================
USE OfficeItemsDB;

DELIMITER //

-- -----------------------------------------------------------
-- 1. 生成流水号 (格式: SIyyyyMMddNNN / SOyyyyMMddNNN)
-- -----------------------------------------------------------
CREATE PROCEDURE sp_GenerateStockInNo(OUT outNo VARCHAR(20))
BEGIN
    DECLARE prefix VARCHAR(10);
    DECLARE seq INT;
    SET prefix = CONCAT('SI', DATE_FORMAT(NOW(), '%Y%m%d'));
    SELECT COUNT(*) + 1 INTO seq FROM StockIn WHERE StockInNo LIKE CONCAT(prefix, '%');
    SET outNo = CONCAT(prefix, LPAD(seq, 3, '0'));
END //

CREATE PROCEDURE sp_GenerateStockOutNo(OUT outNo VARCHAR(20))
BEGIN
    DECLARE prefix VARCHAR(10);
    DECLARE seq INT;
    SET prefix = CONCAT('SO', DATE_FORMAT(NOW(), '%Y%m%d'));
    SELECT COUNT(*) + 1 INTO seq FROM StockOut WHERE StockOutNo LIKE CONCAT(prefix, '%');
    SET outNo = CONCAT(prefix, LPAD(seq, 3, '0'));
END //

-- -----------------------------------------------------------
-- 2. 物品入库（事务：写入库记录 + 更新库存）
-- -----------------------------------------------------------
CREATE PROCEDURE sp_StockIn(
    IN p_ItemCode     VARCHAR(20),
    IN p_PurchaseDate DATETIME,
    IN p_Quantity     INT,
    IN p_UnitPrice    DECIMAL(18,2)
)
BEGIN
    DECLARE v_StockInNo VARCHAR(20);
    DECLARE v_TotalPrice DECIMAL(18,2);
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;
        CALL sp_GenerateStockInNo(v_StockInNo);
        SET v_TotalPrice = p_Quantity * p_UnitPrice;

        INSERT INTO StockIn (StockInNo, ItemCode, PurchaseDate, Quantity, UnitPrice, TotalPrice)
        VALUES (v_StockInNo, p_ItemCode, p_PurchaseDate, p_Quantity, p_UnitPrice, v_TotalPrice);

        UPDATE Items SET Quantity = Quantity + p_Quantity WHERE ItemCode = p_ItemCode;

        SELECT v_StockInNo AS StockInNo;
    COMMIT;
END //

-- -----------------------------------------------------------
-- 3. 领用确认（事务：更新状态 + 扣减库存）
-- -----------------------------------------------------------
CREATE PROCEDURE sp_ApproveStockOut(
    IN p_StockOutNo VARCHAR(20),
    IN p_Approver   VARCHAR(50),
    IN p_Remark     VARCHAR(500)
)
BEGIN
    DECLARE v_ItemCode VARCHAR(20);
    DECLARE v_Quantity INT;
    DECLARE v_Status INT;

    SELECT ItemCode, Quantity, Status INTO v_ItemCode, v_Quantity, v_Status
    FROM StockOut WHERE StockOutNo = p_StockOutNo;

    IF v_Status != 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '该申请已处理，不能重复确认';
    END IF;

    START TRANSACTION;
        UPDATE StockOut SET Status = 1, ApproveDate = NOW(), Remark = p_Remark
        WHERE StockOutNo = p_StockOutNo;

        UPDATE Items SET Quantity = Quantity - v_Quantity WHERE ItemCode = v_ItemCode;
    COMMIT;
END //

-- -----------------------------------------------------------
-- 4. 驳回领用
-- -----------------------------------------------------------
CREATE PROCEDURE sp_RejectStockOut(
    IN p_StockOutNo VARCHAR(20),
    IN p_Remark     VARCHAR(500)
)
BEGIN
    UPDATE StockOut SET Status = 2, ApproveDate = NOW(), Remark = p_Remark
    WHERE StockOutNo = p_StockOutNo AND Status = 0;
END //

-- -----------------------------------------------------------
-- 5. 库存统计
-- -----------------------------------------------------------
CREATE PROCEDURE sp_StockStatistics()
BEGIN
    SELECT
        c.Name AS CategoryName,
        COUNT(i.ItemCode) AS ItemCount,
        COALESCE(SUM(i.Quantity), 0) AS TotalQuantity
    FROM Categories c
    LEFT JOIN Items i ON c.Id = i.Category
    GROUP BY c.Id, c.Name
    ORDER BY TotalQuantity DESC;
END //

-- -----------------------------------------------------------
-- 6. 入库明细查询（带分页，用于百万级数据优化）
-- -----------------------------------------------------------
CREATE PROCEDURE sp_StockInDetail(
    IN p_ItemCode VARCHAR(20),
    IN p_PageNo   INT,
    IN p_PageSize INT
)
BEGIN
    DECLARE v_Offset INT;
    SET v_Offset = (p_PageNo - 1) * p_PageSize;

    SELECT si.StockInNo, si.ItemCode, i.ItemName, si.PurchaseDate,
           si.Quantity, si.UnitPrice, si.TotalPrice
    FROM StockIn si
    JOIN Items i ON si.ItemCode = i.ItemCode
    WHERE (p_ItemCode = '' OR si.ItemCode = p_ItemCode)
    ORDER BY si.PurchaseDate DESC
    LIMIT p_PageSize OFFSET v_Offset;
END //

-- -----------------------------------------------------------
-- 7. 领用明细查询（带分页）
-- -----------------------------------------------------------
CREATE PROCEDURE sp_StockOutDetail(
    IN p_ItemCode VARCHAR(20),
    IN p_PageNo   INT,
    IN p_PageSize INT
)
BEGIN
    DECLARE v_Offset INT;
    SET v_Offset = (p_PageNo - 1) * p_PageSize;

    SELECT so.StockOutNo, so.ItemCode, i.ItemName, so.Quantity,
           so.ApplicantId, so.ApplyDate, so.Status
    FROM StockOut so
    JOIN Items i ON so.ItemCode = i.ItemCode
    WHERE (p_ItemCode = '' OR so.ItemCode = p_ItemCode)
    ORDER BY so.ApplyDate DESC
    LIMIT p_PageSize OFFSET v_Offset;
END //

-- -----------------------------------------------------------
-- 8. 游标 — 批量更新库存盘点（将库存为负的置零）
-- -----------------------------------------------------------
CREATE PROCEDURE sp_FixNegativeStock()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE v_ItemCode VARCHAR(20);
    DECLARE cur CURSOR FOR SELECT ItemCode FROM Items WHERE Quantity < 0;
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO v_ItemCode;
        IF done THEN LEAVE read_loop; END IF;
        UPDATE Items SET Quantity = 0 WHERE ItemCode = v_ItemCode;
    END LOOP;
    CLOSE cur;
END //

-- -----------------------------------------------------------
-- 9. 复杂查询：按类别统计入库金额TOP N
-- -----------------------------------------------------------
CREATE PROCEDURE sp_TopCategoryByStockIn(
    IN p_TopN INT
)
BEGIN
    SELECT
        c.Name AS CategoryName,
        COALESCE(SUM(si.TotalPrice), 0) AS TotalAmount,
        COUNT(DISTINCT si.StockInNo) AS InCount
    FROM Categories c
    JOIN Items i ON c.Id = i.Category
    JOIN StockIn si ON i.ItemCode = si.ItemCode
    GROUP BY c.Id, c.Name
    ORDER BY TotalAmount DESC
    LIMIT p_TopN;
END //

DELIMITER ;
