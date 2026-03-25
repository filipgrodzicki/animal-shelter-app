#  Animal Shelter App

A web application for managing an animal shelter, developed as an engineering thesis project. The system supports animal management, adoptions, volunteer coordination, and includes an AI module based on RAG (Retrieval-Augmented Generation).

## Tech Stack

**Backend**
- ASP.NET Core
- Clean Architecture + CQRS
- Entity Framework Core
- PostgreSQL
- JWT / RBAC

**Frontend**
- React
- TypeScript

**AI**
- Groq API (Llama 3.3 70B)
- RAG (Retrieval-Augmented Generation)

**Tools**
- Docker
- Git / Azure DevOps

## Features

- Animal management (add, edit, adoption status tracking)
- Adoption request workflow with status management
- Volunteer management
- AI chatbot answering questions about shelter animals (RAG)
- Authentication and authorization with role-based access control (admin, employee, user)

## Getting Started
```bash
# Backend
cd ShelterApp
dotnet restore
dotnet run

# Frontend
cd shelter-frontend
npm install
npm run dev
```

## Author

Filip Grodzicki — [linkedin.com/in/filip-grodzicki](https://linkedin.com/in/filip-grodzicki)