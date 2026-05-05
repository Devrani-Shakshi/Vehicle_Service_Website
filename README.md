# Service Platform EV - Vehicle Service Website

Service Platform EV is a robust, enterprise-grade ASP.NET Core application designed to streamline the management of electric vehicle (EV) service requests. The platform connects vehicle owners with service providers and shopkeepers, providing a seamless experience for tracking services, managing commissions, and monitoring performance through interactive dashboards.

 Features

- **Multi-Role Dashboards**: Specialized interfaces for Administrators, Service Providers, and Shopkeepers.
- **Service Request Management**: Complete lifecycle tracking of vehicle service requests from initiation to completion.
- **Commission Tracking**: Automated calculation and tracking of commissions for service providers.
- **Real-time Notifications**: Integrated email and hub-based notifications for status updates.
- **Payment Integration**: Support for Stripe and Razorpay for secure service payments.
- **Service Reminders**: Automated reminders for upcoming vehicle maintenance.
- **Data Seeding**: Robust database initialization with comprehensive seed data.

 Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server / Entity Framework Core
- **Frontend**: Razor Pages, Vanilla CSS, Javascript
- **Logging**: NLog
- **Authentication**: ASP.NET Core Identity

Project Structure

- `/Controllers`: Application logic and request handling.
- `/Models`: Database entities and domain models.
- `/Services`: Business logic layer.
- `/Views`: UI components and page layouts.
- `/Data`: Database context and migration history.
- `/wwwroot`: Static assets (CSS, JS, Images).

Getting Started

Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or Express)
- Visual Studio 2022 or VS Code

Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/Devrani-Shakshi/Vehicle_Service_Website.git
   ```
2. Navigate to the project directory:
   ```bash
   cd Vehicle_Service_Website
   ```
3. Update the connection string in `appsettings.json`.
4. Run the database migrations:
   ```bash
   dotnet ef database update
   ```
5. Build and run the application:
   ```bash
   dotnet run
   ```

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---
*Developed by [Devrani Shakshi](https://github.com/Devrani-Shakshi)*
