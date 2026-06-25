# Smart Parking System

AI-powered parking utilization system with real-time occupancy tracking, JWT authentication, and ML-based prediction.

---

## Project Overview
- **Problem:** Drivers waste time searching for empty slots; no real-time visibility; no prediction of future occupancy.
- **Solution:** Real-time slot tracking, AI prediction (MLR), alerts, and utilization reports.
- **Users:** Admin, Operator, Viewer (role‑based access).

---

## Tech Stack
| Layer | Technology |
|-------|------------|
| Backend | .NET 10, EF Core, SQLite, JWT, BCrypt |
| Frontend | Angular 17, TypeScript, Tailwind CSS, DaisyUI |
| AI Model | Weighted Multiple Linear Regression (MLR) |

---

## Features
- Login with JWT (Admin/Operator/Viewer)
- Real-time dashboard (total/occupied/available, utilisation %, AI prediction)
- Parking slots grid (color‑coded)
- Vehicle entry/exit with transaction history
- Alerts at 80% (warning) and 90% (critical)
- Reports page explaining MLR model + static insights
- Comprehensive exception handling and logging

---

## Repository Structure
SmartParkingSystem/
├── api/ # .NET 10 backend
│ ├── Controllers/
│ ├── Services/
│ ├── Models/
│ ├── Data/
│ └── Program.cs
├── ui/ # Angular  frontend
│ ├── src/app/
│ └── ...
└── README.md

text

---

## Setup & Run

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v18+)
- Angular CLI: `npm install -g @angular/cli`

### Backend (API)
cd api
dotnet restore
dotnet run   # listens on http://localhost:5042
Frontend (UI)
cd ui
npm install
ng serve --open   # opens http://localhost:4200
Login Credentials

Role	Username	Password
Admin	admin	Admin@123
Operator	operator	Admin@123
Viewer	viewer	Admin@123

After login, click Initialize Slots (if empty) to create 30 slots (10 per zone A/B/C).

Machine Learning part used is Multiple Linear Regression considering it's simplicity and has provided detailed contents on its prediction capabilities

Formula:
Predicted = 0.5×Current + 0.3×Last3hAvg + 0.2×SameHourHistAvg

Current = real‑time occupancy (50%)
Last3hAvg = recent trend (30%)
SameHourHistAvg = daily pattern (20%)
When history is empty, prediction falls back to current occupancy.

Assumptions & Limitations

Manual entry/exit (no hardware sensors)
Simple linear model; upgradeable to ML.NET
History accumulates over time → predictions improve after a few days
Future Improvements

SignalR for real‑time updates
ML.NET for automated training
Payment integration
Docker deployment
