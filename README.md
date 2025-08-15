<img width="800" height="500" alt="image" src="https://github.com/user-attachments/assets/aea431a8-8503-4712-9681-e375721b4e91" />
<img width="800" height="500" alt="Ekran görüntüsü 2025-08-14 201415" src="https://github.com/user-attachments/assets/cf3d582d-0e73-42f2-8fec-4a92b52c8f97" />
<img width="800" height="500" alt="Ekran görüntüsü 2025-08-14 201437" src="https://github.com/user-attachments/assets/97ddcc7b-06a9-4cfb-9b1d-945d2e183dc8" />

# Blog Application

This project is a blog platform that allows users to share posts, manage their profiles, interact with other users, and comment, all while incorporating an admin approval process.

---

## 📌 Features

Your application boasts the following core features, providing a rich user experience and administrative control:

### **Post Management**
- **Create New Posts:** Users can share new posts.
- **Post Approval:** Newly shared posts must be approved by an administrator before they become visible on the site.
- **Post Editing:**
  - **Normal Users:** Can only view and edit their own posts.
  - **Admin Users:** Can view all users' posts on the "Manage Posts" page and change their active status (visible/hidden).
- **Like Posts:** Users can like posts and see the number of likes and views.
- **Post Detail Page:** From a post's detail page, users can navigate to:
  - The profile of the user who shared the post.
  - The profile pages of users who commented.

### **User Profile and Interactions**
- **Account Confirmation:** A confirmation link is sent via email to newly created accounts.
- **Password Reset:** Users can reset their passwords via a link sent to their email.
- **Follow Notifications:** When a user follows another, a notification is sent to the followed user.
- **Profile Update:** Users can update their profiles.
- **View Other Profiles:** Users can browse other users' profiles.
- **Follow/Unfollow:** Users can follow or unfollow others.
- **Mutual Following & Messaging:** If two users follow each other, they can send messages.
- **View User's Posts & Comments:** A profile shows a user’s posts and comments.

### **Comments and Interactions**
- **Comment on Posts:** Users can comment under posts.
- **Reply to Comments:** Users can reply to existing comments.

### **Administrator (Admin) Panel**
- **Define User Roles:** Admins can assign or revoke admin roles.
- **View Users:** Admin panel lists all users.

---

## 🛠 Technologies

### **Backend**
- [.NET 9.0](https://dotnet.microsoft.com/) – Powerful and scalable backend.
- Microsoft.AspNetCore.Identity.EntityFrameworkCore – Authentication & authorization.
- Microsoft.AspNetCore.Identity.UI – Ready-to-use UI components.
- Microsoft.AspNetCore.Mvc – MVC architecture.
- Microsoft.EntityFrameworkCore.Sqlite – Lightweight database.
- Microsoft.EntityFrameworkCore.Design – EF Core migrations & design tools.
- Microsoft.Extensions.Logging – Logging operations.
- Microsoft.AspNetCore.Mvc.NewtonsoftJson – JSON serialization/deserialization.

### **Frontend**
- JavaScript
- HTML/CSS

---

## ⚙️ Setup and Running

### **Prerequisites**
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) installed.
- A code editor (Visual Studio Code or Visual Studio).

### **Steps**
1. **Clone the Repository**
   ```bash
   git clone https://github.com/melisaaydin/Blog-App.git
   cd Blog-App
````

2. **Install Dependencies**

   ```bash
   dotnet restore
   ```

3. **Apply Database Migrations**

   ```bash
   dotnet ef database update
   ```

   *(If running for the first time, you may need to add a new migration with:)*

   ```bash
   dotnet ef migrations add InitialCreate
   ```

4. **Run the Application**

   * **HTTP**

     ```bash
     dotnet run --launch-profile http
     ```

     ➜ Runs at `http://localhost:5001`

   * **HTTPS**

     ```bash
     dotnet run --launch-profile https
     ```

     ➜ Runs at `https://localhost:7058` and `http://localhost:5001`

   * **IIS Express**

     ```bash
     dotnet run --launch-profile "IIS Express"
     ```

---

## ⚙️ Configuration

`launchSettings.json` defines environment settings:

```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:9120",
      "sslPort": 5001
    }
  },
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "applicationUrl": "https://localhost:7058;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## 👥 User Roles and Access

**Normal User**

* Share posts.
* View & edit own posts.
* Comment & reply to posts.
* Follow/unfollow users.
* Message users with mutual follow.
* Update own profile.
* View other users’ posts & comments.

**Admin User**

* All features of a normal user.
* Access **Admin Panel**.
* View all posts & change their visibility.
* Manage user roles.

---

## 🤝 Contributing

1. Fork the repository.
2. Create a new branch for your feature/bug fix.
3. Make and test changes.
4. Write descriptive commit messages.
5. Submit a pull request.

---

