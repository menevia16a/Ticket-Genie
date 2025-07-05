# TicketGenie

TicketGenie is an open-source tool designed to make the lives of Game Masters (GMs) easier by providing a dedicated application for handling, reading, and responding to multiple tickets, as well as viewing ticket histories. It is intended for use with World of Warcraft servers that support SQL and SOAP connections.

## Features
- View, respond to, and close multiple tickets efficiently
- View ticket history for players
- Dedicated character manager and account tools
- Secure login with GM username and a custom PIN
- Modern WPF interface

## Installation
1. Visit [https://voidmaster.xyz/TicketGenie/publish.htm](https://voidmaster.xyz/TicketGenie/publish.htm)
2. Click **Install** to download the setup file and follow the prompts to install TicketGenie on your system.

## First-Time Setup
1. On first launch, you will be prompted to enter your SQL and SOAP connection settings. These must be filled in completely for the application to function.
2. The software requires access to your `auth`, `characters`, and `world` databases.
3. The SOAP login uses your GM username and password.
4. On your first login with a GM account, you will be prompted to set a 4-digit PIN. This PIN will be required for future logins.

> **Privacy Note:** All connection settings and credentials are stored locally on your computer in a JSON file. This information is never sent anywhere outside your machine or shared with any third party.

### Database Requirement
For the PIN functionality to work, you must add a column to your `auth.account_access` table. Run the following SQL script on your `auth` database:
ALTER TABLE `account_access`
ADD COLUMN `TicketGeniePin` SMALLINT(5) NULL DEFAULT NULL;
This column can be null; it will be set the first time a GM logs in with TicketGenie.

## Usage
- **Login:** Use your GM username. On first login, set your PIN. On subsequent logins, enter your PIN.
- **Tickets:** View, respond to, and close tickets from the main window.
- **History:** View a player's ticket history.
- **Account Tools:** Manage player accounts and characters.

## License & Disclaimer
This software is open source and provided as-is, without warranty of any kind. Use at your own risk. TicketGenie is intended to help GMs manage tickets more efficiently and is not affiliated with any official game server or company.
