# Fashion Voting System

The **Fashion Voting System** is a web application that enables participants to register for fashion shows, vote for their favorite designers during the show, and view their registered shows. Administrators can manage shows, designers, and votes, while participants enjoy a seamless voting experience.

---

## Features

### Public
- **Browse Upcoming Shows**: View details of upcoming fashion shows.
- **Register/Login**: Participants can register for the system to access further functionalities.

### Participants
- **Register for Shows**: Participants can register for one or more fashion shows (conflict-free scheduling).
- **Vote for Designers**: During the show, participants can vote for their favorite designers.
- **Unvote Option**: Participants can change their votes before the show ends.
- **My Registered Shows**: View all shows a participant has registered for, along with voting statuses.

### Admin
- **Manage Shows**:
  - Create, edit, and delete fashion shows.
  - View votes for a specific show.
- **Manage Designers**:
  - Add designers and assign them to multiple shows.
  - Edit or delete designers.
  - Group designers by shows.
- **View Votes**:
  - View vote details across all shows or for a specific show.

### Additional Features
- **Real-time Feedback**: Success/error messages for user actions.
- **Responsive Design**: Optimized for different devices and screen sizes.
- **Relationship Management**: Effective `1-M` and `M-M` database relationships.

---

## Technologies Used

- **ASP.NET Core MVC**: Backend framework for building the application.
- **Entity Framework Core**: ORM for database management.
- **SQLite**: Lightweight database for data storage.
- **Bootstrap**: Frontend framework for responsive design.
- **C#**: Programming language for business logic and APIs.

---

## Database Structure

The database consists of three main tables with `1-M` and `M-M` relationships:
1. **Participants**: Stores participant details.
2. **Designers**: Stores designer details and their assigned shows.
3. **Shows**: Stores details of upcoming and past shows.

Relationships:
- **1-M**: `Participant` to `ParticipantShow` (a participant can register for multiple shows).
- **M-M**: `Show` to `Designer` (a designer can participate in multiple shows).

---

## API Endpoints

### Participant APIs
- **Register for a Show**: `POST /Shows/Register`
- **View Registered Shows**: `GET /Shows/MyShows`
- **Vote for Designers**: `POST /Votes/SubmitVote`
- **Unvote a Designer**: `POST /Votes/Unvote`

### Admin APIs
- **Manage Shows**:
  - `GET /Shows/AdminIndex`: View all shows.
  - `POST /Shows/Create`: Create a new show.
  - `POST /Shows/Edit`: Edit an existing show.
  - `POST /Shows/Delete`: Delete a show.
  - `GET /Votes/ShowVotes/{showId}`: View votes for a specific show.
- **Manage Designers**:
  - `GET /Designers/Index`: View all designers.
  - `POST /Designers/Create`: Add a new designer.
  - `POST /Designers/Edit`: Edit designer details.
  - `POST /Designers/Delete`: Delete a designer.

---

## How to Run the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo-name/fashion-voting-system.git
   cd fashion-voting-system

2. Restore dependencies:
   ```bash
   dotnet restore


3. Apply database migrations:
    ```bash
    dotnet ef database update


4. Run the application:
    ```bash
    dotnet run


5. Access the application in your browser:
    ```bash
    http://localhost:<port>>


<!-- scaffolding to show the account for login, register, logout: dotnet aspnet-codegenerator identity -dc FashionVote.Data.ApplicationDbContext -->
