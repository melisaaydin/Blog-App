<img width="400" height="280" alt="image" src="https://github.com/user-attachments/assets/5914ee3a-3054-4dca-b918-ce182dcd3c3f" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/640a9755-7918-4c11-a3c7-d8b0d33415e7" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/f543bfc5-3eea-482d-97b3-c46c8a4a3d92" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/170d825b-9dcb-468f-9d09-52fa3dc92582" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/8a96e060-390c-466d-a6b8-490a4ad5746d" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/73eb1cff-5d13-4957-aa33-3936787e16bc" />




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

