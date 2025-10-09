# Sentiment Analyzer App

## Overview
The Sentiment Analyzer App is a web application that allows users to input text and receive sentiment analysis results using Azure OpenAI. The application is built with Angular for the frontend and C# for the backend, both of which are containerized for deployment to Azure Container Apps (ACA).

## Project Structure
The project is organized into two main directories: `frontend` and `backend`.

### Frontend
- **Angular Application**: The frontend is developed using Angular and includes components for user interaction and services for API communication.
- **Components**: The main component for sentiment analysis is located in `src/app/components/sentiment-analyzer/`.
- **Services**: The sentiment analysis service is defined in `src/app/services/sentiment.service.ts`.
- **Models**: The response model for sentiment analysis is defined in `src/app/models/sentiment-response.model.ts`.
- **Docker**: The frontend is containerized using a Dockerfile located in the `frontend` directory.

### Backend
- **C# Web API**: The backend is developed using C# and provides an API for sentiment analysis.
- **Controllers**: The main controller for handling sentiment analysis requests is located in `Controllers/SentimentController.cs`.
- **Models**: Request and response models are defined in `Models/SentimentRequest.cs` and `Models/SentimentResponse.cs`.
- **Services**: The sentiment analysis logic is implemented in `Services/AzureOpenAISentimentService.cs`.
- **Docker**: The backend is also containerized using a Dockerfile located in the `backend` directory.

## Setup Instructions
1. **Clone the Repository**: 
   ```
   git clone <repository-url>
   cd sentiment-analyzer-app
   ```

2. **Frontend Setup**:
   - Navigate to the `frontend` directory.
   - Install dependencies:
     ```
     npm install
     ```
   - Run the Angular application:
     ```
     ng serve
     ```

3. **Backend Setup**:
   - Navigate to the `backend` directory.
   - Restore dependencies:
     ```
     dotnet restore
     ```
   - Run the backend application:
     ```
     dotnet run
     ```

4. **Docker Setup**:
   - Ensure Docker is installed and running.
   - Build and run the containers using Docker Compose:
     ```
     docker-compose up --build
     ```

## Usage
- Open your browser and navigate to `http://localhost:4200` to access the frontend application.
- Input text into the provided field and submit to receive sentiment analysis results.

## Deployment
- The application can be deployed to Azure Container Apps using the provided Dockerfiles and `docker-compose.yml` configuration.

## Contributing
Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

## License
This project is licensed under the MIT License.