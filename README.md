This project is a web application called MasterServicePlatform, developed using ASP.NET Core MVC, Entity Framework Core, SQL Server, and ASP.NET Identity. The system connects users with professional masters who offer household and technical services. It provides functionality for users to register, create profiles, upload avatars, place orders, and leave reviews. Masters can register as service providers, upload portfolios, manage their orders, and receive ratings. An administrative panel is also included, offering tools for managing users and masters, verifying accounts, monitoring violations, and viewing system statistics.

The application demonstrates the full software development lifecycle, including requirements analysis, design, implementation, testing, and deployment. The system includes various important components such as profile management, file uploads, review logic with fraud detection, master verification, and a statistics dashboard. All functionality is fully implemented and tested.

Automated testing was performed using xUnit, Moq, and the EF Core InMemory provider. A total of 38 unit tests were created to verify the correct behavior of controllers, validation logic, administrative actions, order creation, review restrictions, and violation detection. All tests pass successfully. Manual testing was also carried out to ensure correct system behavior from a user perspective.

To run the project, clone the repository, open the solution in Visual Studio, apply database migrations, and launch the application. The project requires .NET 8 SDK.

This project was created as part of a course assignment and represents a complete, functional, and well-tested example of a modern ASP.NET Core MVC application.
