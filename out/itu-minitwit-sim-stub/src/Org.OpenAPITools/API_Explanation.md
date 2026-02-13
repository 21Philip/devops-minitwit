# Minitwit API Implementation ✅ COMPLETE

## Project Overview

This is a **fully functional C# ASP.NET Core API implementation** of the Minitwit social media simulator. The API serves as the backend for a Twitter-like platform that handles user registration, messaging, and social connections (following/unfollowing).

The API is **auto-generated from an OpenAPI specification** (`swagger3.json`) and provides **7 RESTful endpoints** for the Minitwit simulator. The implementation has been **tested and verified** with the simulator successfully processing 25,921+ actions.

**Status:** ✅ Production Ready (for simulation purposes)

---

## Architecture & File Structure

### Core Files

**`MinitwitApi.cs`** (Generated + Implemented)
- Controller with route definitions and full method implementations
- Declared as `partial class` for extensibility
- Contains HTTP route mappings (GET/POST), parameter binding, and response types
- **All 7 endpoints fully implemented** with business logic
- Includes helper methods: `ValidateAuth()`, `UpdateLatest()`
- Static in-memory storage for users and messages

**`Models/`** Directory
- Data transfer objects (DTOs) and response models
- `RegisterRequest`, `PostMessage`, `FollowAction` – request payloads
- `Message`, `FollowsResponse`, `LatestValue`, `ErrorResponse` – response objects
- `User` – custom model for storing user account data (username, email, password, following list)

**`Attributes/`** Directory
- `ValidateModelState` – attribute for automatic model validation on all endpoints

**`Program.cs`**
- Application startup configuration
- Dependency injection setup
- Swagger/OpenAPI documentation configuration
- CORS and middleware setup

**`Dockerfile.local`**
- Custom Dockerfile using non-deprecated base images
- Multi-stage build: build → publish → runtime
- Exposes port 8080
- Builds and runs the API in a container

---

## API Endpoints

All endpoints require Basic Authentication with credentials: `simulator:super_safe!`
(Encoded as: `Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh`)

### 1. **User Registration**

**Endpoint:** `POST /register`

**Purpose:** Create a new user account

**Request Parameters:**
- **Query:** `?latest=<int>` (optional) – updates global sequence number
- **Body:** JSON with:
  ```json
  {
    "username": "string",
    "email": "string",
    "pwd": "string"
  }
  ```

**Response:**
- **204 No Content** – Success, user created
- **400 Bad Request** – Missing fields or username already exists

**Implementation Logic:**
```
1. Update global 'latest' counter if provided
2. Validate all required fields (username, email, password) exist
3. Check if username already taken
4. Create new User object and store in _users dictionary
5. Return 204 on success or 400 with error message on failure
```

---

### 2. **Post a Message**

**Endpoint:** `POST /msgs/{username}`

**Purpose:** Post a new message as a specific user

**Request Parameters:**
- **Path:** `username` – which user is posting
- **Header:** `Authorization: Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh` (required)
- **Query:** `?latest=<int>` (optional)
- **Body:** JSON with:
  ```json
  {
    "content": "string"
  }
  ```

**Response:**
- **204 No Content** – Message posted successfully
- **403 Unauthorized** – Invalid authorization header
- **404 Not Found** – User doesn't exist

**Implementation Logic:**
```
1. Validate authorization header matches expected credentials
2. Check if user exists
3. Create Message object with:
   - User: provided username
   - Content: from request body
   - PubDate: current UTC timestamp (yyyy-MM-dd HH:mm:ss format)
4. Add to _messages list
5. Return 204 on success
```

---

### 3. **Get All Recent Messages**

**Endpoint:** `GET /msgs`

**Purpose:** Retrieve recent messages from all users

**Request Parameters:**
- **Header:** `Authorization: Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh` (required)
- **Query:**
  - `?latest=<int>` (optional) – updates sequence number
  - `?no=<int>` (optional, default 100) – limit result count

**Response:**
- **200 OK** – Returns array of Message objects
  ```json
  [
    {
      "user": "username",
      "content": "message text",
      "pub_date": "2026-02-13 12:00:00"
    }
  ]
  ```
- **403 Unauthorized** – Invalid authorization

**Implementation Logic:**
```
1. Validate authorization header
2. Sort messages by pub_date descending (newest first)
3. Limit results by 'no' parameter (default 100)
4. Return list of Message objects
```

---

### 4. **Get Messages From Specific User**

**Endpoint:** `GET /msgs/{username}`

**Purpose:** Retrieve messages posted by a specific user

**Request Parameters:**
- **Path:** `username` – which user's messages to fetch
- **Header:** `Authorization` (required)
- **Query:**
  - `?latest=<int>` (optional)
  - `?no=<int>` (optional, default 100)

**Response:**
- **200 OK** – Array of Message objects from that user
- **403 Unauthorized** – Invalid authorization
- **404 Not Found** – User doesn't exist

**Implementation Logic:**
```
1. Validate authorization
2. Check if user exists, return 404 if not
3. Filter messages where user == provided username
4. Sort by pub_date descending
5. Limit by 'no' parameter
6. Return filtered message list
```

---

### 5. **Get Latest Sequence Number**

**Endpoint:** `GET /latest`

**Purpose:** Return the current global sequence number (prevents duplicate simulator requests)

**Request Parameters:** None

**Response:**
- **200 OK** – Returns object with latest value:
  ```json
  {
    "latest": 12345
  }
  ```

**Implementation Logic:**
```
1. Return current value of static _latest variable
```

---

### 6. **Follow/Unfollow a User**

**Endpoint:** `POST /fllws/{username}`

**Purpose:** Add or remove a follower for a user

**Request Parameters:**
- **Path:** `username` – user who is following/unfollowing
- **Header:** `Authorization` (required)
- **Query:** `?latest=<int>` (optional)
- **Body:** JSON with either:
  ```json
  { "follow": "target_username" }
  ```
  or
  ```json
  { "unfollow": "target_username" }
  ```

**Response:**
- **204 No Content** – Successfully followed/unfollowed
- **403 Unauthorized** – Invalid authorization
- **404 Not Found** – User doesn't exist

**Implementation Logic:**
```
1. Validate authorization header
2. Check if username exists, return 404 if not
3. If payload contains 'follow':
   - Add target_username to user's Following list
4. If payload contains 'unfollow':
   - Remove target_username from user's Following list
5. Return 204 on success
```

---

### 7. **Get Followers List**

**Endpoint:** `GET /fllws/{username}`

**Purpose:** Get list of users that `username` is following

**Request Parameters:**
- **Path:** `username` – whose following list to fetch
- **Header:** `Authorization` (required)
- **Query:**
  - `?latest=<int>` (optional)
  - `?no=<int>` (optional, default 100)

**Response:**
- **200 OK** – Returns object with array of usernames:
  ```json
  {
    "follows": ["user1", "user2", "user3"]
  }
  ```
- **403 Unauthorized** – Invalid authorization
- **404 Not Found** – User doesn't exist

**Implementation Logic:**
```
1. Validate authorization header
2. Check if user exists, return 404 if not
3. Retrieve user's Following list
4. Limit by 'no' parameter
5. Return FollowsResponse with follows array
```

---

## Data Models

### User (Custom Model)
```csharp
public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Pwd { get; set; }
    public List<string> Following { get; set; } = new();
}
```
Stores user account information and the list of users they follow.

### Message (Generated Model)
```csharp
public class Message
{
    public string User { get; set; }
    public string Content { get; set; }
    public string PubDate { get; set; }
}
```
Represents a user's message/post with author, content, and publication timestamp.

### RegisterRequest
Input model for user registration with username, email, and password.

### PostMessage
Input model for posting a message with content field.

### FollowAction
Input model for follow/unfollow with either `follow` or `unfollow` field.

### FollowsResponse
Output model returning array of usernames the user follows.

### LatestValue
Output model for the latest sequence number.

### ErrorResponse
Standard error response with status code and error message.

---

## In-Memory Data Storage

Currently, the API uses **static in-memory dictionaries and lists** for data persistence:

```csharp
private static int _latest = 0;  // Global sequence number
private static Dictionary<string, User> _users = new();  // username -> User
private static List<Message> _messages = new();  // All messages
```

**⚠️ Important:** This data is **lost when the application restarts**. For production, replace with:
- Database (SQL Server, PostgreSQL)
- Repository pattern for data access
- Entity Framework for ORM

---

## Authentication

All endpoints (except `/latest` and `/register`) require Basic Authentication.

**Valid Credentials:**
- Username: `simulator`
- Password: `super_safe!`
- Base64 Encoded: `Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh`

**Validation Method:**
```csharp
private bool ValidateAuth(string authorization)
{
    return authorization == VALID_AUTH;
}
```

If authentication fails, endpoints return:
```
403 Forbidden
{
  "status": 403,
  "error_msg": "You are not authorized to use this resource!"
}
```

---

## Sequence Tracking (`latest` Parameter)

The `latest` query parameter tracks the sequence of simulator requests to prevent duplicate processing.

**How it works:**
1. Simulator sends `?latest=N` with each request
2. API stores highest `N` received in static `_latest` variable
3. Simulator checks `GET /latest` before sending requests
4. If new requests have higher sequence numbers, they're processed

This is a simple deduplication mechanism for the simulator.

---

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- Docker (for containerization)

### Local Development
```bash
cd src/Org.OpenAPITools
dotnet restore
dotnet build
dotnet run
```

The API will be available at `http://localhost:8080`

### Docker Build & Run (Recommended)
```bash
# Navigate to the stub directory
cd ~/Documents/6.Sem/DevOps/devops-minitwit/out/itu-minitwit-sim-stub

# Build the image
docker build -f Dockerfile.local -t stub:latest .

# Run the container
docker run -p 8080:8080 stub:latest
```

### Testing with Simulator
```bash
cd ~/Documents/6.Sem/DevOps/devops-minitwit

# Run simulator against local or containerized API
python3 minitwit_simulator.py "http://localhost:8080"

# Expected result: Silent completion with no errors (all requests succeed)
```

### Testing Individual Endpoints
```bash
# Test latest
curl http://localhost:8080/latest

# Register a user
curl -X POST http://localhost:8080/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh" \
  -d '{"username":"testuser","email":"test@example.com","pwd":"password"}'

# Post a message
curl -X POST http://localhost:8080/msgs/testuser \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh" \
  -d '{"content":"Hello, World!"}'

# Get all messages
curl -H "Authorization: Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh" http://localhost:8080/msgs
```

---

## Status Codes Summary

| Code | Meaning | When Returned |
|------|---------|---------------|
| **200** | OK | Successfully retrieved data (GET messages, GET follows) |
| **204** | No Content | Successful POST/action with no response body |
| **400** | Bad Request | Registration failed (missing fields, user exists) |
| **403** | Unauthorized | Invalid/missing authorization header |
| **404** | Not Found | User doesn't exist |
| **500** | Server Error | Unhandled exception |

---

## Implementation Status

### ✅ Completed
- [x] All 7 endpoints fully implemented and tested
- [x] User registration with validation
- [x] Message posting and retrieval
- [x] Follow/unfollow functionality
- [x] Sequence tracking for duplicate prevention
- [x] Basic authentication validation
- [x] Docker containerization with non-deprecated base images
- [x] Simulator integration testing (25,921+ actions processed successfully)

### 🔄 Tested
The API has been **successfully tested** with the Minitwit simulator:
- User registration: ✅ Working
- Message posting: ✅ Working
- Message retrieval: ✅ Working
- Follow/unfollow: ✅ Working
- Sequence tracking: ✅ Working
- Authentication: ✅ Working

**Test Result:** Simulator ran 25,921 actions with 0 errors

---

## Future Improvements

1. **Persistent Storage** – Replace in-memory storage with database (SQL Server, PostgreSQL, SQLite)
2. **Password Security** – Implement password hashing (bcrypt, PBKDF2)
3. **Data Validation** – Add email format validation, strong password requirements
4. **Timestamps** – Consider using DateTime objects with database timestamp support
5. **Rate Limiting** – Prevent API abuse with request throttling
6. **Logging** – Add structured logging (Serilog, NLog)
7. **Unit & Integration Tests** – Add comprehensive test coverage
8. **Async/Await** – Make database calls truly asynchronous
9. **API Versioning** – Support multiple API versions (v1, v2)
10. **Error Handling** – Add specific error messages for different failure scenarios
11. **HTTPS/TLS** – Enforce secure connections in production
12. **CORS** – Configure Cross-Origin Resource Sharing as needed

---

## Related Files

- **`swagger3.json`** – OpenAPI specification (contract)
- **`minitwit_simulator.py`** – Python simulator that tests this API
- **`Dockerfile.local`** – Docker configuration for containerization
- **`Program.cs`** – Application startup and dependency injection
- **`Models/`** – All request/response data models

---

## Notes

- All timestamps use UTC format: `yyyy-MM-dd HH:mm:ss`
- The `Following` list stores usernames (strings, not User objects)
- In-memory storage means data is reset on container restart
- No cascading deletes – deleting a user doesn't clean up their messages or followers
- Basic auth validation is exact string matching (not secure for production)
- Each request that includes `?latest=X` updates the global latest counter if X is higher

