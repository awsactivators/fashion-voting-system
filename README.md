# Fashion Voting System

The **Fashion Voting System** is a back-end web application that allows participants to register for fashion shows, vote for their favorite designers, and manage their registered shows. Administrators have complete control over managing shows, designers, and votes, ensuring a seamless experience for both participants and administrators.

---

## Features

### Public
- **Browse Upcoming Shows**: Discover details about upcoming fashion shows and view schedules.
- **Register/Login**: Participants can create an account and log in to access personalized features.

### Participants
- **Register for Shows**: Enroll in one or more fashion shows with conflict-free scheduling.
- **Vote for Designers**: Cast votes for favorite designers during the show.
- **Unvote Option**: Update or withdraw votes before the show concludes.
- **View Registered Shows**: Keep track of registered shows and their statuses.

### Admin
- **Show Management**:
  - Create, edit, and delete fashion shows.
  - View votes and participant details for specific shows.
- **Designer Management**:
  - Add and assign designers to multiple shows.
  - Update or remove designer profiles.
  - Organize designers by show assignments.
- **Vote Management**:
  - Monitor voting trends and details for all shows.
- **Manage Participants**:
  - View all registered participants.
  - Edit participant details, including updating their show registrations.
  - Delete participants from the system.
- **View Votes:**
  - View vote details across all shows or for a specific show.

### Additional Features
- **Real-time Feedback**: Display success or error messages for user actions.
- **Responsive Design**: Fully optimized for mobile, tablet, and desktop devices.
- **Database Relationships**: Utilizes robust `1-M` and `M-M` relationships for efficient data management.

---

## Technologies Used

- **ASP.NET Core MVC**: Backend framework for building the application.
- **Entity Framework Core**: ORM for database management.
- **SQLite**: Database for lightweight and efficient data storage.
- **Bootstrap**: Frontend framework for responsive design.
- **C#**: Programming language for business logic and API implementation.

---

## Database Structure

The database includes three primary tables with `1-M` and `M-M` relationships:
1. **Participants**: Stores participant data.
2. **Designers**: Stores designer data and their show assignments.
3. **Shows**: Stores information about fashion shows.

### Relationships
- **1-M**: `Participant` to `ParticipantShow` (a participant can register for multiple shows).
- **M-M**: `Show` to `Designer` (a designer can participate in multiple shows).

---

## API Endpoints

### Participant APIs
- **Register for a Show**: `POST /api/ShowsApi/register/{showId}`
- **View Registered Shows**: `GET /api/ShowsApi/myshows`
- **Vote for Designers**: `POST /api/VotesApi/SubmitVote`
- **Unvote a Designer**: `POST /api/VotesApi/Unvote`

### Admin APIs
- **Manage Shows**:
  - `GET /api/ShowsApi/admin`: View all shows.
  - `POST /api/ShowsApi/create`: Create a new show.
  - `PUT /api/ShowsApi/edit/{id}`: Edit an existing show.
  - `DELETE /api/ShowsApi/delete/{id}`: Delete a show.
  - `GET /api/VotesApi/Vote/{showId}`: View votes for a specific show.
- **Manage Designers**:
  - `GET /api/designers`: View all designers.
  - `POST /api/designers`: Add a new designer.
  - `PUT /api/designers/{id}`: Edit designer details.
  - `DELETE /api/designers/{id}`: Delete a designer.
- **Manage Participants**:
  - `GET /Participants`: View all participants.
  - `POST /Participants/Edit/{id}`: Edit participant details, including assigned shows.
  - `POST /Participants/Delete/{id}`: Delete a participant.

---

## How to Run the Project

1. **Clone the repository**:
   ```bash
   git clone https://github.com/awsactivators/fashion-voting-system.git
   cd fashion-voting-system/FashionVote


2. **Restore dependencies:**
   ```bash
   dotnet restore


3. **Apply database migrations:**
    ```bash
    dotnet ef database update


4. **Run the application:**
    ```bash
    dotnet run


5. **Access the application in your browser:**
    ```bash
    http://localhost:<port>>


## Project Highlights

- Seamless Voting Experience: Participants can register for shows, vote for their favorite designers, and update their choices.

- Admin Functionality: Comprehensive tools for managing shows, designers, and votes.

- User-Friendly Design: Intuitive and responsive interface for all users.

- Secure and Efficient: Built with robust authentication and optimized database handling.

<!-- scaffolding to show the account for login, register, logout: dotnet aspnet-codegenerator identity -dc FashionVote.Data.ApplicationDbContext -->

<!-- curl -X "DELETE" -H "cookie: .AspNetCore.Identity.Application={token}" "https://localhost:xx/api/category/delete/2" -->