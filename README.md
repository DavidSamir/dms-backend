# Document Management System

A secure document management system built with ASP.NET Core that allows users to upload, categorize, and version-control their documents.

## Features

- **User Authentication and Authorization**
  - Secure authentication using ASP.NET Core Identity
  - Two user roles: Admin and Regular User
  - Regular Users can upload, view, categorize, and manage their own documents
  - Admins have full access to all documents and users

- **Document Management**
  - Upload documents (PDF, DOCX, TXT, etc.)
  - File validation and safe storage
  - Download and preview uploaded documents

- **Document Version Control**
  - Maintain historical versions of uploaded documents
  - View version history
  - Revert to previous versions

## Technology Stack

- **Backend**: ASP.NET Core (.NET 6)
- **Database**: SQLite
- **Authentication**: JWT (JSON Web Tokens)
- **Architecture**: Layered architecture with SOLID principles

## Project Structure

- **DMS.API**: API Layer
- **DMS.Core**: Core business logic and interfaces
- **DMS.Infrastructure**: Data access, file storage
- **DMS.Shared**: DTOs, shared utilities

## Setup and Installation

### Prerequisites

- .NET 6 SDK or later
- Visual Studio 2022, VS Code, or your preferred IDE

### Steps to Run

1. Clone the repository
   ```
   git clone https://github.com/yourusername/document-management-system.git
   cd document-management-system
   ```

2. Install required NuGet packages
dotnet ef migrations add InitialCreate --project DMS.Infrastructure --startup-project DMS.API
dotnet ef database update --project DMS.Infrastructure --startup-project DMS.API
