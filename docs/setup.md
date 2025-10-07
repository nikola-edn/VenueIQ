**VenueIQ — Developer Setup**

- Prerequisites
  - .NET 9 SDK
  - Visual Studio 2022 17.10+ with .NET MAUI workload (Windows) or VS for Mac alternatives
  - Android/iOS platform SDKs per target

- Install workloads
  - `dotnet workload install maui`

- Build
  - `dotnet restore VenueIQ.sln`
  - `dotnet build VenueIQ.sln -c Debug`

- Test
  - `dotnet test tests/VenueIQ.Tests/VenueIQ.Tests.csproj`

- Notes
  - This repo may be edited under WSL where MAUI build/run/test is not supported. Agents should implement code and documentation only. Build and run on a proper MAUI host.
