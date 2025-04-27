# Document Management System (DMS)

A secure document management system built with ASP.NET Core that implements all requirements from the Smart Issuance test scenario. Designed with enterprise-grade security, maintainability, and scalability in mind.


## ✅ Functional Requirements Coverage

### 1. User Authentication & Authorization
- **ASP.NET Core Identity** with JWT authentication
- Role-based access control (Admin/Regular User)
- Admin Portal for user management (CRUD operations, account status control)
- Policy-based authorization for document access

### 2. Document Management
- File upload with validation (type, size)
- Secure storage with GUID filenames
- Download with original filename preservation

### 3. Search & Filtering
- Filter by:
  - Categories/Tags
  - Full-text/Title/Description search
  

### 4. Document Categorization
- Multi-layer taxonomy system:
  - **Types**: Invoice, Report, Contract, etc.
  - **Departments**: HR, Finance, Engineering

### 5. Version Control
- Immutable version history
- Semantic version tracking (v1.0, v1.1, etc.)
- Diff visualization for text-based formats
- Rollback functionality with audit trails

### 6. Notification System
- In-app notifications stored in database
- Flexible Controller for integration of multiple channels, including: Email Push Notification
- Event types:
  - Document approval/rejection
  - Version updates
  - Admin actions (document deletions, access changes)

### 7. Reporting (Admin)
Admin-only dashboard for document statistics and system usage
- Overview of document categories
- Insights into recent upload activity
- Summary of total storage usage
- Trend chart showing storage usage over time

## 🛠 Technical Implementation

### Technology Stack
| Component               | Technology Choices                                                                 |
|-------------------------|------------------------------------------------------------------------------------|
| **Backend**             | ASP.NET Core 8 Web API                                                             |
| **Database**            | PostgreSQL 16 with EF Core 8                                                       |
| **File Storage**        | Local file system                                                                  |
| **Search**              | PostgreSQL Full-Text Search                                                        |
| **Security**            | JWT, ASP.NET Core Identity, OWASP safeguards                                       |
| **Frontend**            | React JS - Vite - Talewind                                                         |

### Security Measures
- Anti-CSRF tokens
- Content Security Policy (CSP) headers
- File type whitelisting
- Regular expression sanitization for search inputs
- Role-based access control (RBAC)
- Audit logging for sensitive operations

### Architecture
```plaintextDMS.API (Presentation Layer)
├── Controllers
├── Storage
├── Properties
├── Program.cs
└── SeedData.cs

DMS.Core (Domain Layer)
├── Interfaces
└── Models

DMS.Infrastructure (Data Layer)
├── Data
├── Mappers
├── Migrations
├── Repositories
└── Services

DMS.Shared (Shared Layer)
└── DTOs

```

## 🚀 Deployment

### Prerequisites
- .NET 8 SDK
- PostgreSQL 16+
- React JS - Vite 

## 🔍 Testing Credentials
| Role        | Username         | Password  |
|-------------|------------------|-----------|
| Admin       | admin            | Admin123! |
| Regular User| user             | User123!  |

Access Swagger UI: `https://localhost:5000/swagger`

## 📜 Documentation
- **API Docs**: Swagger/OpenAPI 3.0 specification
- **Code Comments**: XML documentation enabled
- **Architecture Decision Records**: `/docs/adr`
- **Security Guidelines**: `SECURITY.md`

## 🔮 Future Improvements
- [ ] Implement More Notification Integration
- [ ] Add two-factor authentication
- [ ] Migrate to cloud blob storage
- [ ] Introduce message queue for notifications

