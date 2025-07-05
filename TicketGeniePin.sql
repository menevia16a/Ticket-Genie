-- Run this script on your auth database to enable PIN login for TicketGenie
ALTER TABLE `account_access`
ADD COLUMN `TicketGeniePin` SMALLINT(5) NULL DEFAULT NULL;
