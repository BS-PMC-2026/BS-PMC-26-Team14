# BS-PMC-26-Team14
# CityFix
Link to the Website: https://cityfix-app.azurewebsites.net/
Git Hub : https://github.com/BS-PMC-2026/BS-PMC-26-Team14.git
Jira: https://sce-ac.atlassian.net/jira/software/projects/BSPMT14/boards/2907/backlog?atlOrigin=eyJpIjoiNjk5Y2ZlMzM4YjcxNDNiNjhjMTFlNWE2MGQwMTYyMDkiLCJwIjoiaiJ9

## Overview

CityFix is a web-based municipal reporting system that enables citizens to report infrastructure and public service issues directly to the municipality. The system provides a centralized platform for reporting, assigning, tracking, and resolving city-related problems.

Examples of issues that can be reported:
- Potholes and road damage
- Broken streetlights
- Waste and sanitation problems
- Public infrastructure damage
- Safety hazards

---

## Project Goals

The main goals of the system are:

- Simplify the process of reporting municipal issues.
- Improve communication between citizens and municipal workers.
- Reduce response and resolution times.
- Provide transparency and status tracking for citizens.
- Support efficient management of reports by municipal employees and administrators.

---

## User Roles

### Citizen

Customers can:
- Register and log in.
- Create reports.
- Upload images.
- Select a report location on a map.
- View and track their reports.
- Receive notifications about report updates.

### Worker

Workers can:
- View assigned reports.
- Accept reports for treatment.
- Update report status.
- Upload images after treatment.
- View reports on a map.
- Close reports after resolution.

### Admin

Administrators can:
- View all reports.
- Assign reports to workers.
- Monitor system activity.
- Manage users.
- Track report progress.

---

## Main Features

### Authentication
- Login
- Registration
- Forgot Password
- Logout
- Profile Management

### Report Management
- Create Reports
- Upload Images
- Select Report Location
- Automatic Location Detection
- Report Tracking

### Map Integration
- Google Maps Integration
- Report Visualization
- Location Selection

### Notification System
- Real-Time Notifications
- Notification Center
- Read/Unread Status
- Notification Counter

### Report Lifecycle Management
- Report Creation
- Assignment
- Status Updates
- Resolution
- Closure

---

## Technologies Used

### Frontend
- HTML
- CSS
- JavaScript

### Backend
- ASP.NET Core Web API
- C#

### Database
- SQL Server

### External Services
-leaflet OpenStreetMap Maps API

### Project Management
- Jira
- GitHub

---

## Testing

### Unit Tests
- Authentication tests
- Report management tests
- Notification tests
- User validation tests

### Integration Tests
- Frontend ↔ Backend
- Backend ↔ Database
- leaflet OpenStreetMap Maps API integration
- End-to-end report workflow testing

---

## Quality Assurance

Prerequisites

Before running the system, make sure the following software is installed:

- Visual Studio 2022
- .NET 8 SDK
- SQL Server
- SQL Server Management Studio (SSMS)
- Git
- Google Chrome

Clone the Project

Open Command Prompt or Terminal and run:

git clone https://github.com/your-repository/CityFix.git

Move into the project directory:

cd CityFix


Database Setup

Open SQL Server Management Studio (SSMS).

Create a new database:

CREATE DATABASE CityFixDB;
GO

If migrations exist, run:

dotnet ef database update

If no migrations exist:

dotnet ef migrations add InitialCreate
dotnet ef database update
Configure Database Connection


Open:

appsettings.json

Update the connection string:

"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=CityFixDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
Restore Dependencies

Open terminal inside the Backend project folder:

dotnet restore
Build the Project
dotnet build

Expected result:

Build succeeded.
Run the Backend

Navigate to the API project folder:

cd CityFix.API

Run:

dotnet run

Expected output:

Now listening on:
https://localhost:5001

http://localhost:5000
Run the Frontend

If the project uses static HTML pages:

Open:

index.html

or

login.html

using Live Server in Visual Studio Code.

Alternative:

npx serve .
leaflet OpenStreetMap Maps Configuration


Navigate to the test project folder:

cd CityFix.Tests

Run:

dotnet test

Expected output:

Passed! 100% tests passed.
Running Integration Tests
dotnet test --filter Category=Integration

## Team Members

- Ahmad Akhras
- Ismail Badran
- Bashar Abadi
- Muhammed Mahmameed


